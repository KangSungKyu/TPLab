import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("package_consumer", Path(__file__).with_name("run_package_consumer.py"))
c = importlib.util.module_from_spec(spec)
spec.loader.exec_module(c)

ASSEMBLIES = {"core": "TPLab.Core", "input": "TPLab.Core.Input", "editor": "TPLab.Core.Editor", "ui": "TPLab.UI", "ui.inputsystem": "TPLab.UI.InputSystem", "netcode": "TPLab.Netcode"}
UNITASK = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"

class PackageConsumerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="tplab-package-consumer-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.artifacts = self.root / "artifacts"
        self.artifacts.mkdir()
        self.revision = "e" * 40
        self.files = {}
        self.records = {}
        dependencies = {
            "core": {"com.cysharp.unitask": "2.5.11", "com.unity.addressables": "2.9.1"},
            "input": {"com.tplab.core": "0.0.2", "com.cysharp.unitask": "2.5.11", "com.unity.inputsystem": "1.19.0"},
            "editor": {"com.tplab.core": "0.0.2", "com.cysharp.unitask": "2.5.11", "com.unity.addressables": "2.9.1", "com.unity.nuget.newtonsoft-json": "3.2.2"},
            "ui": {"com.tplab.core": "0.0.2", "com.cysharp.unitask": "2.5.11", "com.unity.ugui": "2.0.0"},
            "ui.inputsystem": {"com.tplab.ui": "0.0.2", "com.tplab.input": "0.0.2", "com.cysharp.unitask": "2.5.11", "com.unity.inputsystem": "1.19.0"},
            "netcode": {"com.cysharp.unitask": "2.5.11", "com.unity.netcode.gameobjects": "2.13.3", "com.unity.transport": "2.7.3"}}
        for short in ASSEMBLIES:
            name = "com.tplab." + short
            manifest = {"name": name, "version": "0.0.2", "dependencies": dependencies[short]}
            files = {"Runtime/Marker.cs": ("// " + short + "\n").encode(), "Runtime/Marker.cs.meta": ("fileFormatVersion: 2\nguid: " + hashlib.md5(short.encode()).hexdigest() + "\n").encode()}
            if short == "core":
                files["Runtime/ThirdParty/CsvHelper.dll"] = b"fixture-dll"
            if short in ("ui.inputsystem", "netcode"):
                manifest["samples"] = [{"displayName": "Demo", "description": "Contract fixture", "path": "Samples~/Demo"}]
                files["Samples~/Demo/Runtime/Demo.cs"] = b"// imported demo\n"
                files["Samples~/Demo/Runtime/Demo.cs.meta"] = ("fileFormatVersion: 2\nguid: " + hashlib.md5((short + "-sample").encode()).hexdigest() + "\n").encode()
                files["Samples~/Demo/Runtime/Demo.asmdef"] = json.dumps({"name": "Sample." + short}).encode()
            files["package.json"] = json.dumps(manifest).encode()
            self.files[name] = files
            self.repack(name)
        self.write_manifest()

    def repack(self, name, extras=()):
        filename = name + "-0.0.2.tgz"
        with tarfile.open(self.artifacts / filename, "w:gz") as archive:
            for relative, data in self.files[name].items():
                item = tarfile.TarInfo("package/" + relative)
                item.size = len(data)
                archive.addfile(item, io.BytesIO(data))
            for item, data in extras:
                archive.addfile(item, io.BytesIO(data) if data is not None else None)
        payload = {relative: hashlib.sha256(data).hexdigest() for relative, data in self.files[name].items()}
        self.records[name] = {"id": name, "version": "0.0.2", "filename": filename, "sha256": hashlib.sha256((self.artifacts / filename).read_bytes()).hexdigest(), "payloadSha256": hashlib.sha256((json.dumps(payload, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode()).hexdigest()}

    def write_manifest(self, **changes):
        manifest = {"schemaVersion": 1, "sourceRevision": self.revision, "version": "0.0.2", "publicationSnapshot": "Matched", "packages": list(self.records.values())}
        manifest.update(changes)
        (self.artifacts / "distribution-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")

    def plan(self, modules=("netcode",), mode="tarball", samples=()):
        return c.build_plan(self.artifacts, self.revision, mode, modules, samples)

    def install(self, plan):
        project = self.root / "consumer"
        project.mkdir()
        (project / "Packages").mkdir()
        lock = {}
        infos = []
        for name, provider in plan["dependencies"].items():
            if name.startswith("com.tplab."):
                fingerprint = self.revision if plan["mode"] == "git" else "a" * 32
                resolved = project / "Library/PackageCache" / (name + "@" + fingerprint[:12])
                for relative, data in self.files[name].items():
                    target = resolved / relative
                    target.parent.mkdir(parents=True, exist_ok=True)
                    if relative == "package.json":
                        manifest = json.loads(data)
                        manifest["_fingerprint"] = fingerprint
                        data = json.dumps(manifest).encode()
                    target.write_bytes(data)
                item = {"version": provider, "source": "git" if plan["mode"] == "git" else "local-tarball", "dependencies": {}}
                if plan["mode"] == "git":
                    item["hash"] = self.revision
                infos.append({"name": name, "version": "0.0.2", "resolvedPath": str(resolved), "source": item["source"]})
            else:
                item = {"version": provider, "source": "git" if name == "com.cysharp.unitask" else "registry"}
            lock[name] = item
        (project / "Packages/packages-lock.json").write_text(json.dumps({"dependencies": lock}))
        (project / "Packages/manifest.json").write_text(json.dumps({"dependencies": plan["dependencies"]}))
        assemblies = []
        for name in plan["expectedAssemblies"]:
            location = project / "Library/ScriptAssemblies" / (name + ".dll")
            location.parent.mkdir(parents=True, exist_ok=True)
            location.write_bytes(b"fixture-compiled")
            assemblies.append({"name": name, "location": str(location), "references": []})
        report = {"success": True, "phase": "verify", "runId": plan["runId"], "sourceRevision": self.revision, "projectPath": str(project), "installedPackages": infos, "assemblies": assemblies, "imports": []}
        return project, report

    def test_netcode_only_does_not_install_core_ui_input_or_csv(self):
        plan = self.plan()
        self.assertEqual(["com.tplab.netcode"], plan["selectedPackages"])
        self.assertEqual(["TPLab.Netcode"], plan["expectedAssemblies"])
        self.assertNotIn("com.unity.addressables", plan["dependencies"])
        self.assertNotIn("com.unity.inputsystem", plan["dependencies"])
        self.assertNotIn("com.unity.ugui", plan["dependencies"])
        self.assertEqual(UNITASK, plan["dependencies"]["com.cysharp.unitask"])

    def test_ui_base_dependency_closure_has_no_input_or_ngo(self):
        plan = self.plan(("ui",))
        self.assertEqual({"com.tplab.core", "com.tplab.ui"}, set(plan["selectedPackages"]))
        self.assertNotIn("com.unity.inputsystem", plan["dependencies"])
        self.assertNotIn("com.unity.netcode.gameobjects", plan["dependencies"])

    def test_adapter_closure_installs_only_its_required_modules(self):
        plan = self.plan(("ui.inputsystem",))
        self.assertEqual({"com.tplab.core", "com.tplab.input", "com.tplab.ui", "com.tplab.ui.inputsystem"}, set(plan["selectedPackages"]))
        self.assertNotIn("com.tplab.editor", plan["selectedPackages"])
        self.assertNotIn("com.tplab.netcode", plan["selectedPackages"])

    def test_sample_extras_are_opt_in_and_do_not_force_tplab_input(self):
        plan = self.plan(samples=("netcode",))
        self.assertEqual(["com.tplab.netcode"], plan["selectedPackages"])
        self.assertEqual("1.19.0", plan["dependencies"]["com.unity.inputsystem"])
        self.assertEqual("2.0.0", plan["dependencies"]["com.unity.ugui"])
        self.assertIn("Sample.netcode", plan["expectedAssemblies"])

    def test_git_providers_use_exact_sha_for_entire_closure(self):
        plan = self.plan(("ui",), mode="git")
        for name in plan["selectedPackages"]:
            self.assertEqual("https://github.com/KangSungKyu/TPLab.git?path=/upm/" + name + "#" + self.revision, plan["dependencies"][name])

    def test_wrong_version_or_incomplete_six_package_manifest_is_rejected(self):
        for changes in ({"version": "0.0.1"}, {"packages": list(self.records.values())[:-1]}, {"publicationSnapshot": "Unchecked"}):
            self.write_manifest(**changes)
            with self.assertRaises(ValueError):
                self.plan()

    def test_unknown_module_and_source_installation_are_rejected(self):
        with self.assertRaises(ValueError):
            self.plan(("unknown",))
        with self.assertRaises(ValueError):
            self.plan(mode="source")

    def test_wrong_source_sha_is_rejected(self):
        with self.assertRaises(ValueError):
            c.build_plan(self.artifacts, "a" * 40, "git", ["netcode"])

    def test_archive_tampering_is_rejected_before_launch(self):
        archive = self.artifacts / self.records["com.tplab.netcode"]["filename"]
        archive.write_bytes(archive.read_bytes() + b"tamper")
        with self.assertRaises(ValueError):
            self.plan()

    def test_path_traversal_and_symlink_members_are_rejected(self):
        for relative, kind in [("package/../escape.cs", tarfile.REGTYPE), ("package/linked", tarfile.SYMTYPE)]:
            item = tarfile.TarInfo(relative)
            item.type = kind
            item.size = 0
            item.linkname = "elsewhere"
            self.repack("com.tplab.netcode", [(item, b"" if kind == tarfile.REGTYPE else None)])
            self.write_manifest()
            with self.assertRaises(ValueError):
                self.plan()

    def test_duplicate_asset_guid_is_rejected(self):
        self.files["com.tplab.netcode"]["Runtime/Marker.cs.meta"] = self.files["com.tplab.core"]["Runtime/Marker.cs.meta"]
        self.repack("com.tplab.netcode")
        self.write_manifest()
        with self.assertRaises(ValueError):
            self.plan()

    def test_netcode_package_cannot_silently_reintroduce_core(self):
        data = json.loads(self.files["com.tplab.netcode"]["package.json"])
        data["dependencies"]["com.tplab.core"] = "0.0.2"
        self.files["com.tplab.netcode"]["package.json"] = json.dumps(data).encode()
        self.repack("com.tplab.netcode")
        self.write_manifest()
        with self.assertRaises(ValueError):
            self.plan()

    def test_samples_must_belong_to_selected_package(self):
        with self.assertRaises(ValueError):
            self.plan(samples=("ui.inputsystem",))

    def test_consumers_and_evidence_are_fresh_temp_paths(self):
        source = self.root / "source"
        (source / "Temp").mkdir(parents=True)
        out, evidence = c.resolve_paths(source, "Temp/consumer", "Temp/evidence")
        self.assertEqual(source / "Temp/consumer", out)
        self.assertEqual(source / "Temp/evidence", evidence)
        out.mkdir()
        with self.assertRaises(FileExistsError):
            c.resolve_paths(source, "Temp/consumer", "Temp/evidence")
        with self.assertRaises(ValueError):
            c.resolve_paths(source, self.root / "outside", "Temp/evidence")

    def test_scaffolding_copies_no_product_sources_and_only_selected_manifest(self):
        plan = self.plan()
        output = self.root / "new-consumer"
        c.create_project(plan, output, "6000.3.18f1")
        manifest = json.loads((output / "Packages/manifest.json").read_text())
        self.assertEqual(plan["dependencies"], manifest["dependencies"])
        self.assertFalse((output / "Assets/TPLab").exists())
        self.assertEqual(1, len(list((output / "Assets").rglob("*.cs"))))

    def test_matching_tarball_and_git_installations_pass_payload_checks(self):
        for mode in ("tarball", "git"):
            with self.subTest(mode=mode):
                plan = self.plan(mode=mode)
                project, report = self.install(plan)
                verified = c.verify_installation(project, plan, report)
                self.assertEqual(["com.tplab.netcode"], [x["name"] for x in verified])
                project.rename(self.root / ("done-" + mode))

    def test_installed_payload_tampering_is_rejected(self):
        plan = self.plan()
        project, report = self.install(plan)
        (Path(report["installedPackages"][0]["resolvedPath"]) / "Runtime/Marker.cs").write_text("changed")
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)

    def test_wrong_git_hash_and_extra_tplab_module_are_rejected(self):
        plan = self.plan(mode="git")
        project, report = self.install(plan)
        path = project / "Packages/packages-lock.json"
        lock = json.loads(path.read_text())
        lock["dependencies"]["com.tplab.netcode"]["hash"] = "a" * 40
        path.write_text(json.dumps(lock))
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)
        lock["dependencies"]["com.tplab.netcode"]["hash"] = self.revision
        lock["dependencies"]["com.tplab.core"] = {"version": "0.0.2", "source": "registry"}
        path.write_text(json.dumps(lock))
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)

    def test_no_core_dependency_may_hide_in_loaded_netcode_assembly(self):
        plan = self.plan()
        project, report = self.install(plan)
        report["assemblies"][0]["references"] = ["TPLab.Core"]
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)

    def test_missing_assembly_and_foreign_report_identity_are_rejected(self):
        plan = self.plan()
        project, report = self.install(plan)
        for field, value in [("assemblies", []), ("runId", "another-run"), ("projectPath", str(self.root)), ("sourceRevision", "0" * 40)]:
            changed = dict(report)
            changed[field] = value
            with self.assertRaises(ValueError):
                c.verify_installation(project, plan, changed)

    def test_forbidden_source_tree_is_rejected_even_with_matching_packages(self):
        plan = self.plan()
        project, report = self.install(plan)
        (project / "Assets/TPLab/Netcode/Runtime").mkdir(parents=True)
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)

    def test_netcode_no_samples_rejects_unexpected_input_or_addressables(self):
        plan = self.plan()
        project, report = self.install(plan)
        path = project / "Packages/packages-lock.json"
        original = json.loads(path.read_text())
        for name in ("com.unity.inputsystem", "com.unity.addressables", "com.unity.ugui"):
            lock = json.loads(json.dumps(original))
            lock["dependencies"][name] = {"version": "1.0.0", "source": "registry"}
            path.write_text(json.dumps(lock))
            with self.assertRaises(ValueError):
                c.verify_installation(project, plan, report)

class PackageConsumerReviewTests(unittest.TestCase):
    setUp = PackageConsumerTests.setUp
    repack = PackageConsumerTests.repack
    write_manifest = PackageConsumerTests.write_manifest
    plan = PackageConsumerTests.plan
    install = PackageConsumerTests.install

    def test_actual_netcode_collections_dependency_is_installed_at_pinned_version(self):
        data = json.loads(self.files["com.tplab.netcode"]["package.json"])
        data["dependencies"]["com.unity.collections"] = "2.6.6"
        self.files["com.tplab.netcode"]["package.json"] = json.dumps(data).encode()
        self.repack("com.tplab.netcode")
        self.write_manifest()
        plan = self.plan()
        self.assertEqual("2.6.6", plan["dependencies"]["com.unity.collections"])
        self.assertEqual(["com.tplab.netcode"], plan["selectedPackages"])

    def test_git_dll_bytes_must_not_receive_text_line_ending_exception(self):
        self.files["com.tplab.netcode"]["Runtime/Backend.dll"] = b"binary\r\npayload"
        self.repack("com.tplab.netcode")
        self.write_manifest()
        plan = self.plan(mode="git")
        project, report = self.install(plan)
        installed = Path(report["installedPackages"][0]["resolvedPath"]) / "Runtime/Backend.dll"
        installed.write_bytes(b"binary\npayload")
        with self.assertRaises(ValueError):
            c.verify_installation(project, plan, report)

class PackageConsumerPrefabEolTests(unittest.TestCase):
    setUp = PackageConsumerTests.setUp
    repack = PackageConsumerTests.repack
    write_manifest = PackageConsumerTests.write_manifest
    plan = PackageConsumerTests.plan
    install = PackageConsumerTests.install

    def test_git_installed_prefab_accepts_only_crlf_checkout_difference(self):
        name = "com.tplab.ui.inputsystem"
        relative = "Samples~/Demo/Cell.prefab"
        self.files[name][relative] = b"%YAML 1.1\n--- !u!1 &1\nGameObject:\n  m_Name: Cell\n"
        self.repack(name)
        self.write_manifest()
        plan = self.plan(("ui.inputsystem",), mode="git")
        project, report = self.install(plan)
        resolved = Path(next(item for item in report["installedPackages"] if item["name"] == name)["resolvedPath"])
        (resolved / relative).write_bytes(self.files[name][relative].replace(b"\n", b"\r\n"))
        self.assertEqual(4, len(c.verify_installation(project, plan, report)))

    def test_git_unity_yaml_line_endings_are_symmetric_for_all_three_extensions(self):
        lf = b"%YAML 1.1\n--- !u!1 &1\nGameObject:\n  m_Name: Cell\n"
        crlf = lf.replace(b"\n", b"\r\n")
        for extension in (".prefab", ".asset", ".unity"):
            with self.subTest(extension=extension):
                self.assertTrue(c._equal_payload(lf, crlf, "Samples~/Demo/Cell" + extension, self.root, "git"))
                self.assertTrue(c._equal_payload(crlf, lf, "Samples~/Demo/Cell" + extension, self.root, "git"))

    def test_binary_unity_serialization_never_receives_eol_exception(self):
        for extension in (".prefab", ".asset", ".unity"):
            for binary in (b"binary\x00\r\npayload", b"%YAML 1.1\r\nvalue: \x00\r\n"):
                with self.subTest(extension=extension, binary=binary):
                    self.assertFalse(c._equal_payload(binary, binary.replace(b"\r\n", b"\n"), "Cell" + extension, self.root, "git"))

    def test_invalid_utf8_unity_yaml_never_receives_eol_exception(self):
        binary = b"%YAML 1.1\r\nvalue: \xff\r\n"
        for extension in (".prefab", ".asset", ".unity"):
            with self.subTest(extension=extension):
                self.assertFalse(c._equal_payload(binary, binary.replace(b"\r\n", b"\n"), "Cell" + extension, self.root, "git"))

    def test_prefab_content_mutation_or_lone_cr_is_rejected(self):
        original = b"%YAML 1.1\nGameObject:\n  m_Name: Cell\n"
        for changed in (original.replace(b"Cell", b"Tampered").replace(b"\n", b"\r\n"), original.replace(b"\n", b"\r")):
            with self.subTest(changed=changed):
                self.assertFalse(c._equal_payload(original, changed, "Cell.prefab", self.root, "git"))

    def test_tarball_unity_yaml_keeps_exact_byte_requirement(self):
        original = b"%YAML 1.1\nGameObject:\n"
        for extension in (".prefab", ".asset", ".unity"):
            with self.subTest(extension=extension):
                self.assertFalse(c._equal_payload(original, original.replace(b"\n", b"\r\n"), "Cell" + extension, self.root, "tarball"))

    def test_exact_binary_unity_payload_is_still_accepted(self):
        binary = b"binary\x00\r\n\xff"
        for extension in (".prefab", ".asset", ".unity", ".dll"):
            with self.subTest(extension=extension):
                self.assertTrue(c._equal_payload(binary, binary, "Cell" + extension, self.root, "git"))

if __name__ == "__main__":
    unittest.main()
