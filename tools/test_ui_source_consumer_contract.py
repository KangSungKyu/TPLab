"""Source-only UI consumer contract tests using isolated files and Git fixtures.
No test launches Unity or changes original source/Git.
"""

from contextlib import contextmanager
import copy
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest

from run_ui_source_consumer import (
    resolve_ui_paths,
    source_files,
    source_identity,
    validate_result,
)


TASK_TEMP_NAME = "UIConsumerContractTests"
UNITY_VERSION = "6000.3.18f1"
BASE_FILES = (
    "Assets/TPLab.meta",
    "Assets/TPLab/Core.meta",
    "Assets/TPLab/Core/TPLab.Core.asmdef",
    "Assets/TPLab/Core/TPLab.Core.asmdef.meta",
    "Assets/TPLab/Core/SceneRoot.cs",
    "Assets/TPLab/Core/SceneRoot.cs.meta",
    "Assets/TPLab/UI.meta",
    "Assets/TPLab/UI/Runtime.meta",
    "Assets/TPLab/UI/Runtime/TPLab.UI.asmdef",
    "Assets/TPLab/UI/Runtime/TPLab.UI.asmdef.meta",
    "Assets/TPLab/UI/Runtime/UIContext.cs",
    "Assets/TPLab/UI/Runtime/UIContext.cs.meta",
    "Assets/Plugins.meta",
    "Assets/Plugins/CsvHelper.meta",
    "Assets/Plugins/CsvHelper/CsvHelper.dll",
    "Assets/Plugins/CsvHelper/CsvHelper.dll.meta",
    "Assets/Plugins/CsvHelper/LICENSE.txt",
    "Assets/Plugins/CsvHelper/LICENSE.txt.meta",
    "doc/licenses/UniTask-LICENSE.txt",
)
INPUT_FILES = (
    "Assets/TPLab/Input.meta",
    "Assets/TPLab/Input/Runtime.meta",
    "Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef",
    "Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef.meta",
    "Assets/TPLab/Input/Runtime/InputManager.cs",
    "Assets/TPLab/Input/Runtime/InputManager.cs.meta",
    "Assets/TPLab/UI/InputSystem.meta",
    "Assets/TPLab/UI/InputSystem/Runtime.meta",
    "Assets/TPLab/UI/InputSystem/Runtime/TPLab.UI.InputSystem.asmdef",
    "Assets/TPLab/UI/InputSystem/Runtime/TPLab.UI.InputSystem.asmdef.meta",
    "Assets/TPLab/UI/InputSystem/Runtime/UIInputSystemAdapter.cs",
    "Assets/TPLab/UI/InputSystem/Runtime/UIInputSystemAdapter.cs.meta",
)
EXCLUDED_FILES = (
    "Assets/TPLab/UI/Tests/PlayMode/Example.cs",
    "Assets/TPLab/UI/Editor/Example.cs",
    "Assets/TPLab/UI/Scenes/Example.unity",
    "Assets/TPLab/UI/Settings/Example.asset",
    "Assets/TPLab/Input/Tests/Example.cs",
    "Assets/TPLab/Samples/Input/SceneTransitions/Runtime/Example.cs",
    "ProtectedUnrelated.txt",
)


def write_fixture_file(project, relative, content=None):
    path = project / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content if content is not None else ("fixture:" + relative + "\n").encode())
    return path


def make_project(directory):
    project = Path(directory) / "project"
    project.mkdir()
    for relative in BASE_FILES + INPUT_FILES + EXCLUDED_FILES:
        write_fixture_file(project, relative)
    write_fixture_file(project, "Assets/Plugins/CsvHelper/CsvHelper.dll", b"MZ-P7-fixture-only")
    write_fixture_file(project, "ProjectSettings/ProjectVersion.txt",
                       ("m_EditorVersion: " + UNITY_VERSION + "\n").encode())
    (project / "Temp").mkdir()
    (project / "doc/validation/ui-system/p7").mkdir(parents=True)
    return project


def is_link_path(path):
    if path.is_symlink():
        return True
    try:
        return bool(getattr(path.lstat(), "st_file_attributes", 0) & 0x400)
    except FileNotFoundError:
        return False


def fixture_root():
    # Promotion to tools changes __file__, never fixture ownership: locate the checkout and pin its task Temp child.
    project = next((parent for parent in Path(__file__).absolute().parents
                    if (parent / "Assets/TPLab/Core/TPLab.Core.asmdef").is_file()), None)
    if project is None:
        raise AssertionError("Cannot locate the TPLab checkout for task-owned Temp fixtures.")
    root = project / "Temp" / TASK_TEMP_NAME / "fixtures"
    for candidate in (root, *root.parents):
        if is_link_path(candidate):
            raise AssertionError("Task fixture root must not traverse a symlink/junction/reparse point.")
    root.mkdir(parents=True, exist_ok=True)
    return root


@contextmanager
def link_fixture(link, target):
    if not target.is_dir():
        raise AssertionError("Link fixtures require an actual owned target directory.")
    created = False
    try:
        try:
            link.symlink_to(target, target_is_directory=True)
        except OSError as error:
            if os.name != "nt" or getattr(error, "winerror", None) != 1314:
                raise
            # Actual junction creation needs no symlink privilege; reject command metacharacters in owned fixture paths.
            if any(character in str(link) + str(target) for character in '\"\n\r%&|<>^!()'):
                raise AssertionError("Unsupported characters in native junction fixture paths.") from error
            subprocess.run([os.environ.get("COMSPEC", "cmd.exe"), "/d", "/c", "mklink", "/J", str(link), str(target)],
                           check=True, capture_output=True, text=True)
        created = True
        if not is_link_path(link):
            raise AssertionError("Native link fixture was not a real symlink/junction/reparse point.")
        yield link
    finally:
        if created:
            if not is_link_path(link):
                raise AssertionError("Refusing to remove a fixture path that is no longer a link.")
            if link.is_symlink():
                link.unlink()
            else:
                os.rmdir(link)  # Nonrecursive: remove only the junction, never its target contents.


def fixture_git(project, *arguments):
    # Future test execution only: the synthetic repository is under this draft's TemporaryDirectory.
    # Explicit fixture identities also override inherited author/committer environment variables.
    environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    environment.update({
        "GIT_AUTHOR_NAME": "TPLabTestFixture",
        "GIT_AUTHOR_EMAIL": "fixture@example.invalid",
        "GIT_COMMITTER_NAME": "TPLabTestFixture",
        "GIT_COMMITTER_EMAIL": "fixture@example.invalid",
        "GIT_TERMINAL_PROMPT": "0",
    })
    completed = subprocess.run(
        ["git", "-c", "core.autocrlf=false", "-c", "commit.gpgsign=false",
         "-c", "core.hooksPath=" + str(project / ".git/fixture-empty-hooks"),
         "-c", "user.name=TPLabTestFixture", "-c", "user.email=fixture@example.invalid", *arguments],
        cwd=project, env=environment, check=True, capture_output=True, text=True,
    )
    return completed.stdout.strip()


class UiSourceConsumerContractTests(unittest.TestCase):
    def test_allowlist_runtime_metas_dependencies_optional_input_and_link_rejection(self):
        with tempfile.TemporaryDirectory(prefix="ui-p7-allowlist-", dir=fixture_root()) as directory:
            project = make_project(directory)
            base = source_files(project, include_input=False)
            self.assertEqual({path.relative_to(project).as_posix() for path in base}, set(BASE_FILES))
            self.assertEqual(len(base), len(set(base)), "Duplicate source destinations are unsupported.")
            optional = source_files(project, include_input=True)
            self.assertEqual({path.relative_to(project).as_posix() for path in optional}, set(BASE_FILES + INPUT_FILES))
            for relative in ("Assets/TPLab/Core/unreviewed.json", "Assets/TPLab/UI/Runtime/unreviewed.asset"):
                with self.subTest(unexpected=relative):
                    unexpected = write_fixture_file(project, relative)
                    with self.assertRaises(ValueError):
                        source_files(project, include_input=False)
                    unexpected.unlink()
            required = project / "Assets/Plugins/CsvHelper/LICENSE.txt.meta"
            original = required.read_bytes()
            required.unlink()
            with self.assertRaises(FileNotFoundError):
                source_files(project)
            required.write_bytes(original)
            target = Path(directory) / "external-runtime"
            write_fixture_file(target, "External.cs")
            with link_fixture(project / "Assets/TPLab/UI/Runtime/Borrowed", target):
                with self.assertRaises(ValueError):
                    source_files(project)
            dependency = project / "Assets/Plugins/CsvHelper"
            target = Path(directory) / "external-csv"
            for name in ("CsvHelper.dll", "CsvHelper.dll.meta", "LICENSE.txt", "LICENSE.txt.meta"):
                write_fixture_file(target, name, (dependency / name).read_bytes())
                (dependency / name).unlink()
            dependency.rmdir()
            with link_fixture(dependency, target):
                with self.assertRaises(ValueError):
                    source_files(project)

    def test_paths_are_fresh_direct_temp_child_and_ui_evidence_without_links_or_escape(self):
        with tempfile.TemporaryDirectory(prefix="ui-p7-paths-", dir=fixture_root()) as directory:
            project = make_project(directory)
            output_arg = "Temp/UIConsumer-unique-01"
            evidence_arg = "doc/validation/ui-system/p7/consumer-unique-01"
            output, evidence = resolve_ui_paths(project, output_arg, evidence_arg)
            self.assertEqual(output, project / output_arg)
            self.assertEqual(output.parent, project / "Temp")
            self.assertEqual(evidence, project / evidence_arg)
            self.assertFalse(output.exists(), "Path resolution must not create or overwrite output.")
            self.assertFalse(evidence.exists())
            for out, proof in (
                ("Temp", evidence_arg), ("Temp/nested/consumer", evidence_arg),
                ("Temp/../escape", evidence_arg), (str(Path(directory) / "outside-output"), evidence_arg),
                (output_arg, "doc/validation/distribution-consumer/consumer"),
                (output_arg, "doc/validation/ui-system/../../../escape"),
                (output_arg, str(Path(directory) / "outside-evidence")),
            ):
                with self.subTest(output=out, evidence=proof):
                    with self.assertRaises(ValueError):
                        resolve_ui_paths(project, out, proof)
            for existing in (output, evidence):
                existing.mkdir(parents=True)
                with self.assertRaises((ValueError, FileExistsError)):
                    resolve_ui_paths(project, output_arg, evidence_arg)
                existing.rmdir()
            with link_fixture(project / "Temp/linked", Path(directory)):
                with self.assertRaises(ValueError):
                    resolve_ui_paths(project, "Temp/linked/consumer", evidence_arg)
            with link_fixture(project / "doc/validation/ui-system/p7/linked", Path(directory)):
                with self.assertRaises(ValueError):
                    resolve_ui_paths(project, output_arg, "doc/validation/ui-system/p7/linked/consumer")

    def test_actual_result_exact_fresh_graphics_and_honest_profiler_availability(self):
        with tempfile.TemporaryDirectory(prefix="ui-p7-result-", dir=fixture_root()) as directory:
            project = make_project(directory)
            revision = "a" * 40
            started = time.time() - 2
            result = {
                "runId": "fixture-run", "sourceRevision": revision, "projectPath": str(project),
                "startedUnix": started + 0.25, "completedUnix": started + 1,
                "actualFrames": 600, "graphicsDeviceType": "Direct3D11", "drawCalls": 1,
                "unityVersion": UNITY_VERSION, "backend": "Mono2x", "target": "StandaloneWindows64",
                "profiler": {
                    "Main Thread": {"status": "Available", "freshSamples": 600, "unit": "ns",
                                    "samples": [1000000] * 600, "median": 1000000, "p95": 1000000, "max": 1000000},
                    "Layout": {"status": "N/A", "reason": "Recorder unavailable in this fixture",
                               "unit": "ns", "samples": [], "median": None, "p95": None, "max": None},
                },
            }
            original = copy.deepcopy(result)
            arguments = dict(expected_run_id="fixture-run", expected_revision=revision,
                             expected_project=project, started_unix=started)
            self.assertTrue(validate_result(result, **arguments))
            self.assertEqual(result, original, "Validation must preserve N/A rather than substitute numeric zero.")
            for key, value in (
                ("runId", "old-run"), ("sourceRevision", "b" * 40), ("sourceRevision", "short"),
                ("projectPath", str(Path(directory) / "wrong-project")),
                ("startedUnix", started - 10), ("completedUnix", started - 10),
                ("completedUnix", float("nan")), ("actualFrames", 0), ("graphicsDeviceType", "Null"),
                ("graphicsDeviceType", None), ("drawCalls", 0), ("backend", "IL2CPP"),
                ("target", "StandaloneLinux64"), ("unityVersion", "wrong-version"),
            ):
                with self.subTest(field=key, value=value):
                    invalid = copy.deepcopy(result)
                    invalid[key] = value
                    self.assertFalse(validate_result(invalid, **arguments))
            for key in ("sourceRevision", "projectPath", "completedUnix", "graphicsDeviceType", "unityVersion", "backend"):
                with self.subTest(missing=key):
                    invalid = copy.deepcopy(result)
                    del invalid[key]
                    self.assertFalse(validate_result(invalid, **arguments))
            invalid = copy.deepcopy(result)
            invalid["profiler"]["Layout"].update(status="Available", freshSamples=0, median=0, p95=0, max=0)
            self.assertFalse(validate_result(invalid, **arguments), "Unavailable/stale counters cannot become measured zero.")
            invalid = copy.deepcopy(result)
            invalid["profiler"]["Layout"].update(median=0, p95=0, max=0)
            self.assertFalse(validate_result(invalid, **arguments), "N/A statistics must remain unavailable, not numeric zero.")

    def test_source_identity_exact_head_and_allowlisted_git_blob_bytes_only(self):
        with tempfile.TemporaryDirectory(prefix="ui-p7-git-", dir=fixture_root()) as directory:
            project = make_project(directory)
            fixture_git(project, "init", "--quiet")
            fixture_git(project, "add", "--", ".")
            fixture_git(project, "commit", "--quiet", "-m", "Synthetic P7 contract fixture")
            revision = fixture_git(project, "rev-parse", "HEAD")
            self.assertRegex(revision, r"^[0-9a-f]{40}$")
            identity = source_identity(project, expected_revision=revision)
            self.assertEqual(identity["revision"], revision)
            self.assertEqual(set(identity["paths"]), set(BASE_FILES))
            for wrong in ("main", revision[:39], revision + "0", "0" * 40):
                with self.subTest(revision=wrong):
                    with self.assertRaises(ValueError):
                        source_identity(project, expected_revision=wrong)
            write_fixture_file(project, "ProtectedUnrelated.txt", b"unrelated human fixture change\n")
            write_fixture_file(project, "Assets/TPLab/UI/Tests/PlayMode/Example.cs", b"excluded fixture change\n")
            self.assertEqual(source_identity(project, revision), identity,
                             "Excluded/protected dirty content must not block an allowlist-only identity check.")
            optional_identity = source_identity(project, revision, include_input=True)
            self.assertEqual(set(optional_identity["paths"]), set(BASE_FILES + INPUT_FILES))
            input_source = project / "Assets/TPLab/Input/Runtime/InputManager.cs"
            input_original = input_source.read_bytes()
            input_source.write_bytes(b"changed optional input fixture\n")
            self.assertEqual(source_identity(project, revision), identity)
            with self.assertRaises(ValueError):
                source_identity(project, revision, include_input=True)
            input_source.write_bytes(input_original)
            for relative in ("Assets/TPLab/UI/Runtime/UIContext.cs", "Assets/TPLab/UI/Runtime/UIContext.cs.meta",
                             "Assets/Plugins/CsvHelper/CsvHelper.dll", "doc/licenses/UniTask-LICENSE.txt"):
                with self.subTest(changed_allowlist=relative):
                    path = project / relative
                    original = path.read_bytes()
                    path.write_bytes(original + b"uncommitted fixture change\n")
                    with self.assertRaises(ValueError):
                        source_identity(project, revision)
                    path.write_bytes(original)
            untracked = write_fixture_file(project, "Assets/TPLab/UI/Runtime/Untracked.cs")
            with self.assertRaises(ValueError):
                source_identity(project, revision)
            untracked.unlink()


    def test_git_text_attributes_allow_crlf_working_license_without_changing_canonical_blob(self):
        import hashlib
        with tempfile.TemporaryDirectory(prefix="ui-p7-eol-", dir=fixture_root()) as directory:
            project = make_project(directory)
            license_path = "Assets/Plugins/CsvHelper/LICENSE.txt"
            write_fixture_file(project, ".gitattributes", b"Assets/Plugins/CsvHelper/LICENSE.txt text eol=crlf\n*.dll binary\n")
            license_file = write_fixture_file(project, license_path, b"fixture license\n")
            fixture_git(project, "init", "--quiet")
            fixture_git(project, "add", "--", ".")
            fixture_git(project, "commit", "--quiet", "-m", "Synthetic Git newline policy fixture")
            revision = fixture_git(project, "rev-parse", "HEAD")
            license_file.write_bytes(b"fixture license\r\n")
            canonical = subprocess.check_output(["git", "show", revision + ":" + license_path], cwd=project)
            self.assertEqual(canonical, b"fixture license\n")
            self.assertNotEqual(license_file.read_bytes(), canonical)
            identity = source_identity(project, revision)
            self.assertEqual(identity["sha256"][license_path], hashlib.sha256(canonical).hexdigest())
            self.assertEqual(license_file.read_bytes(), b"fixture license\r\n", "Source bytes must stay untouched.")
            license_file.write_bytes(b"different license\r\n")
            with self.assertRaises(ValueError):
                source_identity(project, revision)


if __name__ == "__main__":
    unittest.main()