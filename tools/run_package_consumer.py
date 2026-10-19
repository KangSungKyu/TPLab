#!/usr/bin/env python3
"""Resolve and compile selected TPLab 0.0.2 Git/tarball packages in a fresh consumer.

Only verification scaffolding is copied. Samples use Unity's public Sample.Import;
installation/import success is not a Player, test-suite, or visual acceptance result.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import tarfile
import uuid

_CORE_SPEC = importlib.util.spec_from_file_location("tplab_existing_core_consumer", Path(__file__).with_name("run_core_consumer.py"))
core = importlib.util.module_from_spec(_CORE_SPEC)
_CORE_SPEC.loader.exec_module(core)
VERSION = "0.0.2"
ASSEMBLIES = {"core": "TPLab.Core", "input": "TPLab.Core.Input", "editor": "TPLab.Core.Editor", "ui": "TPLab.UI", "ui.inputsystem": "TPLab.UI.InputSystem", "netcode": "TPLab.Netcode"}
PACKAGE_IDS = {"com.tplab." + name for name in ASSEMBLIES}
UNITASK_PROVIDER = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
EXTERNAL_VERSIONS = {"com.cysharp.unitask": "2.5.11", "com.unity.addressables": "2.9.1", "com.unity.inputsystem": "1.19.0", "com.unity.ugui": "2.0.0", "com.unity.netcode.gameobjects": "2.13.3", "com.unity.transport": "2.7.3", "com.unity.collections": "2.6.6", "com.unity.nuget.newtonsoft-json": "3.2.2"}
SAMPLE_EXTRAS = {name: {"com.unity.ugui": "2.0.0", "com.unity.modules.ui": "1.0.0", "com.unity.modules.imgui": "1.0.0"} for name in ("com.tplab.input", "com.tplab.ui.inputsystem", "com.tplab.netcode")}
SAMPLE_EXTRAS["com.tplab.netcode"]["com.unity.inputsystem"] = "1.19.0"
TEMPLATES = Path(__file__).with_name("package-consumer") / "templates"


def _load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def _write_new(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n")


def _package_id(name):
    result = name if name.startswith("com.tplab.") else "com.tplab." + name
    if result not in PACKAGE_IDS:
        raise ValueError("Unknown TPLab module: " + name)
    return result


def _relative(name):
    path = PurePosixPath(name)
    if not name or path.is_absolute() or "\\" in name or ":" in name or any(part in ("", ".", "..") for part in name.split("/")):
        raise ValueError("Unsafe package path: " + name)
    return path


def _payload(archive):
    core.refuse_links(archive)
    result = {}
    with tarfile.open(archive, "r:gz") as tar:
        for member in tar.getmembers():
            _relative(member.name)
            if not member.isfile() or not member.name.startswith("package/"):
                raise ValueError("Only regular package/ members are accepted.")
            relative = member.name[len("package/"):]
            _relative(relative)
            if relative in result:
                raise ValueError("Duplicate package member: " + relative)
            result[relative] = tar.extractfile(member).read()
    if "package.json" not in result:
        raise ValueError("Missing package.json.")
    return result


def _external(name, version):
    expected = "1.0.0" if name.startswith("com.unity.modules.") else EXTERNAL_VERSIONS.get(name)
    if expected is None or version != expected:
        raise ValueError("Unapproved external dependency/version: " + name + "=" + str(version))
    return UNITASK_PROVIDER if name == "com.cysharp.unitask" else version


def build_plan(artifacts, revision, mode, modules, samples=()):
    if mode not in ("git", "tarball") or not re.fullmatch("[0-9a-f]{40}", revision or ""):
        raise ValueError("Use git/tarball and an exact 40-character revision.")
    artifacts = Path(artifacts).absolute()
    core.refuse_links(artifacts)
    manifest_path = artifacts / "distribution-manifest.json"
    manifest = _load(manifest_path)
    if (manifest.get("schemaVersion") != 1 or manifest.get("sourceRevision") != revision or manifest.get("version") != VERSION or manifest.get("publicationSnapshot") != "Matched"):
        raise ValueError("Artifact source/version/publication snapshot mismatch.")
    records = manifest.get("packages", [])
    if len(records) != 6 or {item.get("id") for item in records} != PACKAGE_IDS:
        raise ValueError("Distribution must contain exactly the six approved package IDs.")
    packages, guid_owners = {}, {}
    for record in records:
        name = record["id"]
        filename = name + "-" + VERSION + ".tgz"
        archive = artifacts / filename
        if record.get("version") != VERSION or record.get("filename") != filename or core.sha256(archive) != record.get("sha256"):
            raise ValueError("Artifact filename/version/hash mismatch: " + name)
        files = _payload(archive)
        hashes = {key: hashlib.sha256(value).hexdigest() for key, value in files.items()}
        canonical = (json.dumps(hashes, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode()
        if hashlib.sha256(canonical).hexdigest() != record.get("payloadSha256"):
            raise ValueError("Artifact payload hash mismatch: " + name)
        package = json.loads(files["package.json"])
        if package.get("name") != name or package.get("version") != VERSION:
            raise ValueError("Package manifest identity mismatch: " + name)
        for relative, data in files.items():
            if relative.endswith(".meta"):
                match = re.search(rb"^guid:\s*([0-9a-f]{32})\s*$", data, re.M)
                if not match:
                    raise ValueError("Invalid asset GUID: " + relative)
                guid = match.group(1).decode()
                if guid in guid_owners:
                    raise ValueError("Duplicate asset GUID: " + guid)
                guid_owners[guid] = name + "/" + relative
        packages[name] = {**record, "manifest": package, "files": hashes}
    requested = {_package_id(name) for name in modules}
    if not requested:
        raise ValueError("Select at least one module.")
    selected = set()
    def visit(name):
        if name in selected:
            return
        selected.add(name)
        for dependency, version in packages[name]["manifest"].get("dependencies", {}).items():
            if dependency.startswith("com.tplab."):
                if dependency not in PACKAGE_IDS or version != VERSION:
                    raise ValueError("Unapproved TPLab dependency: " + dependency)
                visit(dependency)
    for name in requested:
        visit(name)
    sample_ids = sorted({_package_id(name) for name in samples})
    if not set(sample_ids) <= selected:
        raise ValueError("Samples must belong to selected package dependencies.")
    if requested == {"com.tplab.netcode"} and selected != requested:
        raise ValueError("Netcode-only must not acquire another TPLab module.")
    dependencies = {}
    for name in sorted(selected):
        dependencies[name] = ("https://github.com/KangSungKyu/TPLab.git?path=/upm/" + name + "#" + revision if mode == "git" else "file:" + (artifacts / packages[name]["filename"]).as_posix())
        for dependency, version in packages[name]["manifest"].get("dependencies", {}).items():
            if not dependency.startswith("com.tplab."):
                provider = _external(dependency, version)
                if dependency in dependencies and dependencies[dependency] != provider:
                    raise ValueError("Conflicting dependency: " + dependency)
                dependencies[dependency] = provider
    expected_assemblies = [ASSEMBLIES[name[len("com.tplab."):]] for name in sorted(selected)]
    sample_plans = []
    for name in sample_ids:
        definitions = packages[name]["manifest"].get("samples", [])
        if len(definitions) != 1:
            raise ValueError("Expected one explicitly selected sample: " + name)
        definition = definitions[0]
        folder = str(_relative(definition["path"]))
        if not folder.startswith("Samples~/"):
            raise ValueError("Samples must be beneath Samples~/.")
        prefix = folder + "/"
        sample_files = [key for key in packages[name]["files"] if key.startswith(prefix)]
        if not sample_files:
            raise ValueError("Selected sample is empty.")
        raw = _payload(artifacts / packages[name]["filename"])
        sample_assemblies = [json.loads(raw[key])["name"] for key in sample_files if key.endswith(".asmdef")]
        expected_assemblies.extend(sample_assemblies)
        sample_plans.append({"package": name, "path": folder, "displayName": definition["displayName"], "files": sample_files})
        for dependency, version in SAMPLE_EXTRAS.get(name, {}).items():
            provider = _external(dependency, version)
            if dependency in dependencies and dependencies[dependency] != provider:
                raise ValueError("Conflicting sample dependency: " + dependency)
            dependencies[dependency] = provider
    # JsonUtility is used by the harness, rather than being a product dependency.
    dependencies["com.unity.modules.jsonserialize"] = "1.0.0"
    if len(expected_assemblies) != len(set(expected_assemblies)):
        raise ValueError("Duplicate selected assembly names.")
    return {"runId": uuid.uuid4().hex, "version": VERSION, "mode": mode, "sourceRevision": revision, "artifacts": str(artifacts), "distributionManifestSha256": core.sha256(manifest_path), "requestedPackages": sorted(requested), "selectedPackages": sorted(selected), "dependencies": dependencies, "packages": packages, "samples": sample_plans, "expectedAssemblies": sorted(expected_assemblies), "harnessDependencies": {"com.unity.modules.jsonserialize": "1.0.0"}, "playerStatus": "NotRun", "testSuiteStatus": "NotRun"}


def resolve_paths(project, output, evidence):
    project = Path(project).absolute()
    core.refuse_links(project)
    project = project.resolve()
    temp = project / "Temp"
    paths = []
    for argument in (output, evidence):
        raw = Path(argument)
        if ".." in raw.parts:
            raise ValueError("Path traversal is refused.")
        candidate = raw if raw.is_absolute() else project / raw
        core.refuse_links(candidate)
        candidate = candidate.resolve()
        if candidate == temp or not core.inside(candidate, temp):
            raise ValueError("Consumer and evidence paths must be below source-project Temp.")
        if candidate.exists():
            raise FileExistsError("Fresh output required: " + str(candidate))
        paths.append(candidate)
    if core.inside(paths[0], paths[1]) or core.inside(paths[1], paths[0]):
        raise ValueError("Consumer and evidence paths must not overlap.")
    return tuple(paths)


def create_project(plan, output, unity_version):
    output = Path(output)
    core.refuse_links(output)
    output.mkdir(parents=True, exist_ok=False)
    _write_new(output / "Packages/manifest.json", {"dependencies": plan["dependencies"]})
    settings = output / "ProjectSettings"
    settings.mkdir()
    (settings / "ProjectVersion.txt").write_text("m_EditorVersion: " + unity_version + "\n", encoding="utf-8")
    if "com.unity.inputsystem" in plan["dependencies"]:
        (settings / "ProjectSettings.asset").write_text(core.input_project_settings(Path(plan["sourceProject"])), encoding="utf-8")
    editor = output / "Assets/PackageConsumer/Editor"
    editor.mkdir(parents=True)
    _write_new(editor / "TPLab.PackageConsumer.Editor.asmdef", {"name": "TPLab.PackageConsumer.Editor", "includePlatforms": ["Editor"]})
    (editor / "PackageConsumerProbe.cs").write_bytes((TEMPLATES / "PackageConsumerProbe.cs").read_bytes())


def _equal_payload(expected, actual, relative, resolved, mode):
    suffix = PurePosixPath(relative).suffix
    text = suffix in {".cs", ".asmdef", ".md", ".meta", ".asset", ".unity", ".prefab", ".txt", ".json", ".inputactions"}
    if text and suffix in {".asset", ".unity", ".prefab"}:
        # Unity also supports binary serialization; only verified YAML receives Git's EOL exception.
        try:
            text = all(b"\0" not in raw and raw.decode("utf-8-sig").startswith("%YAML ") for raw in (expected, actual))
        except UnicodeDecodeError:
            text = False
    git_eol_match = mode == "git" and text and expected.replace(b"\r\n", b"\n") == actual.replace(b"\r\n", b"\n")
    metadata_match = relative == "package.json" and core.installed_manifest_matches(expected, actual, resolved.name, mode)
    return expected == actual or git_eol_match or metadata_match



def _equal_imported_sample(expected, actual, relative, resolved, mode, input_system_version):
    if PurePosixPath(relative).suffix != ".inputactions":
        return _equal_payload(expected, actual, relative, resolved, mode)

    def unique_object(pairs):
        value = {}
        for key, item in pairs:
            if key in value:
                raise ValueError("Duplicate JSON key")
            value[key] = item
        return value

    try:
        before = json.loads(expected.decode("utf-8-sig"), object_pairs_hook=unique_object)
        after = json.loads(actual.decode("utf-8-sig"), object_pairs_hook=unique_object)
        # Canonical JSON retains bool/number distinctions and rejects NaN/Infinity.
        json.dumps(before, allow_nan=False)
        after_json = json.dumps(after, sort_keys=True, allow_nan=False)
    except (ValueError, UnicodeError):
        return False
    if not isinstance(before, dict) or not isinstance(after, dict):
        return False
    if _equal_payload(expected, actual, relative, resolved, mode):
        return True
    stem = PurePosixPath(relative).stem
    if (input_system_version != "1.19.0" or set(before) != {"name", "maps", "controlSchemes"}
            or not isinstance(before["name"], str) or not before["name"] or before["name"] == stem
            or after.get("name") != stem or type(after.get("version")) is not int or after["version"] != 1):
        return False
    # Input System 1.19 renames a mismatched legacy asset to its filename and writes version 1.
    normalized = {**before, "name": stem, "version": 1}
    return json.dumps(normalized, sort_keys=True, allow_nan=False) == after_json


def verify_installation(project, plan, report):
    project = Path(project).absolute()
    if (report.get("success") is not True or report.get("phase") != "verify" or report.get("runId") != plan["runId"] or report.get("sourceRevision") != plan["sourceRevision"] or report.get("projectPath") != str(project)):
        raise ValueError("Consumer report identity/phase/success mismatch.")
    if _load(project / "Packages/manifest.json").get("dependencies") != plan["dependencies"]:
        raise ValueError("Consumer manifest changed.")
    lock = _load(project / "Packages/packages-lock.json")["dependencies"]
    if {name for name in lock if name.startswith("com.tplab.")} != set(plan["selectedPackages"]):
        raise ValueError("Resolved TPLab dependency scope mismatch.")
    for name, provider in plan["dependencies"].items():
        if lock.get(name, {}).get("version") != provider:
            raise ValueError("Resolved provider mismatch: " + name)
    if "com.tplab.input" not in plan["selectedPackages"] and "com.unity.inputsystem" not in plan["dependencies"] and "com.unity.inputsystem" in lock:
        raise ValueError("Unselected Input System resolved.")
    if "com.tplab.netcode" not in plan["selectedPackages"] and "com.unity.netcode.gameobjects" in lock:
        raise ValueError("Unselected NGO resolved.")
    standalone = plan["selectedPackages"] == ["com.tplab.netcode"]
    if standalone and "com.unity.addressables" in lock:
        raise ValueError("Netcode-only resolved Addressables.")
    if standalone and not plan["samples"] and any(name in lock for name in ("com.unity.inputsystem", "com.unity.ugui")):
        raise ValueError("Netcode without samples resolved sample extras.")
    if (project / "Assets/TPLab").exists():
        raise ValueError("Forbidden source-tree copy in package consumer.")
    for asmdef in (project / "Assets").rglob("*.asmdef"):
        if _load(asmdef).get("name") in ASSEMBLIES.values():
            raise ValueError("Product assembly was copied into Assets.")
    infos = report.get("installedPackages", [])
    if len(infos) != len(plan["selectedPackages"]) or {item["name"] for item in infos} != set(plan["selectedPackages"]):
        raise ValueError("PackageInfo scope mismatch.")
    assemblies = report.get("assemblies", [])
    if len(assemblies) != len(plan["expectedAssemblies"]) or {item["name"] for item in assemblies} != set(plan["expectedAssemblies"]):
        raise ValueError("Expected compiled assemblies were not actually loaded.")
    for item in assemblies:
        location = Path(item.get("location", ""))
        core.refuse_links(location)
        if not location.is_absolute() or not core.inside(location.resolve(), project.resolve()) or not location.is_file():
            raise ValueError("Loaded assembly is outside this consumer: " + item["name"])
        if item["name"] == "TPLab.Netcode" and any(name.startswith("TPLab.") for name in item.get("references", [])):
            raise ValueError("Netcode runtime references another TPLab assembly.")
    if core.sha256(Path(plan["artifacts"]) / "distribution-manifest.json") != plan["distributionManifestSha256"]:
        raise ValueError("Distribution manifest changed after planning.")
    validated = []
    for info in infos:
        name = info["name"]
        entry = lock[name]
        source = "git" if plan["mode"] == "git" else "local-tarball"
        if info.get("version") != VERSION or entry.get("source") != source or plan["mode"] == "git" and entry.get("hash") != plan["sourceRevision"]:
            raise ValueError("Installed package source/version/SHA mismatch: " + name)
        resolved = Path(info.get("resolvedPath", ""))
        core.refuse_links(resolved)
        if not resolved.is_absolute() or not core.inside(resolved.resolve(), (project / "Library/PackageCache").resolve()) or not resolved.is_dir():
            raise ValueError("Expected this consumer's installed package cache: " + name)
        record = plan["packages"][name]
        archive = Path(plan["artifacts"]) / record["filename"]
        if core.sha256(archive) != record["sha256"]:
            raise ValueError("Archive changed after planning: " + name)
        raw = _payload(archive)
        for relative, expected in raw.items():
            target = resolved / relative
            core.refuse_links(target)
            if not target.is_file() or not _equal_payload(expected, target.read_bytes(), relative, resolved, plan["mode"]):
                raise ValueError("Installed payload differs: " + name + "/" + relative)
        validated.append({**info, "verifiedPayloadFiles": len(raw), "source": source})
    imports = report.get("imports", [])
    if len(imports) != len(plan["samples"]) or {x["package"] for x in imports} != {x["package"] for x in plan["samples"]}:
        raise ValueError("Sample import evidence scope mismatch.")
    for sample in plan["samples"]:
        observed = next(x for x in imports if x["package"] == sample["package"])
        target = Path(observed.get("importPath", ""))
        core.refuse_links(target)
        if not observed.get("imported") or not target.is_absolute() or not core.inside(target.resolve(), (project / "Assets/Samples").resolve()) or not target.is_dir():
            raise ValueError("Expected public Sample.Import output in fresh Assets/Samples.")
        record = plan["packages"][sample["package"]]
        raw = _payload(Path(plan["artifacts"]) / record["filename"])
        prefix = sample["path"] + "/"
        for relative in sample["files"]:
            imported = target / relative[len(prefix):]
            core.refuse_links(imported)
            if not imported.is_file() or not _equal_imported_sample(raw[relative], imported.read_bytes(), relative, target, plan["mode"], plan["dependencies"].get("com.unity.inputsystem")):
                raise ValueError("Imported sample bytes/GUID differ: " + relative)
    if standalone and any((project / "Library/PackageCache").rglob("CsvHelper.dll")):
        raise ValueError("Netcode-only unexpectedly contains CsvHelper.")
    return sorted(validated, key=lambda item: item["name"])


def execute(plan, unity, output, evidence, timeout=900):
    evidence.mkdir(parents=True, exist_ok=False)
    report = {"runId": plan["runId"], "sourceRevision": plan["sourceRevision"], "consumerProject": str(output), "success": False, "installationStatus": "NotRun", "compileStatus": "NotRun", "sampleImportStatus": "NotRequested" if not plan["samples"] else "NotRun", "testSuiteStatus": "NotRun", "playerStatus": "NotRun", "visualAcceptance": "NotRun", "phases": [], "errors": []}
    try:
        create_project(plan, output, core.unity_version(Path(plan["sourceProject"])))
        plan_path = evidence / "plan.json"
        _write_new(plan_path, {**plan, "projectPath": str(output)})
        env = os.environ.copy()
        env["TPLAB_PACKAGE_PLAN"] = str(plan_path)
        phases = ["import", "verify"] if plan["samples"] else ["verify"]
        for phase in phases:
            result_path = evidence / (phase + ".json")
            log = evidence / (phase + "-unity.log")
            env.update({"TPLAB_PACKAGE_PHASE": phase, "TPLAB_PACKAGE_RESULT": str(result_path)})
            command = [str(unity), "-batchmode", "-nographics", "-projectPath", str(output), "-executeMethod", "TPLabPackageConsumer.PackageConsumerProbe.Perform", "-logFile", str(log)]
            process = core.run_process(command, output, env, log, evidence / (phase + "-stdout.log"), timeout)
            report["phases"].append({"phase": phase, "command": command, **process})
            observed = core.read_json(result_path, process["startedUnix"])
            if process["returnCode"] != 0 or process["timedOut"] or not process["processLogFresh"] or observed.get("success") is not True or observed.get("unityVersion") != core.unity_version(Path(plan["sourceProject"])):
                raise ValueError("Actual Unity consumer phase failed: " + phase)
            if phase == "verify":
                report["installedPackages"] = verify_installation(output, plan, observed)
        report.update(success=True, installationStatus="Passed", compileStatus="Passed", sampleImportStatus="Passed" if plan["samples"] else "NotRequested")
    except Exception as error:
        report["errors"].append(str(error))
    _write_new(evidence / "summary.json", report)
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--unity", required=True, type=Path)
    parser.add_argument("--artifacts", required=True, type=Path)
    parser.add_argument("--revision", required=True)
    parser.add_argument("--installation", required=True, choices=("git", "tarball"))
    parser.add_argument("--modules", required=True, nargs="+", help="Selected short names or exact com.tplab IDs; dependencies are closed explicitly.")
    parser.add_argument("--samples", nargs="*", default=[], help="Opt-in sample owner package IDs; imported via Sample.Import in a separate phase.")
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence", required=True)
    parser.add_argument("--timeout-seconds", type=int, default=900)
    args = parser.parse_args(argv)
    try:
        if not args.project.is_absolute() or not args.unity.is_absolute() or not args.unity.is_file() or args.timeout_seconds <= 0:
            raise ValueError("Absolute source project/Unity executable and positive timeout required.")
        core.refuse_links(args.unity)
        output, evidence = resolve_paths(args.project, args.output, args.evidence)
        plan = build_plan(args.artifacts, args.revision, args.installation, args.modules, args.samples)
        plan["sourceProject"] = str(args.project)
        report = execute(plan, args.unity, output, evidence, args.timeout_seconds)
        print(json.dumps({"success": report["success"], "evidence": str(evidence), "errors": report["errors"]}))
        return 0 if report["success"] else 1
    except Exception as error:
        print(str(error))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
