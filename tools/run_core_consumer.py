#!/usr/bin/env python3
"""Build an isolated Unity consumer and run its Windows Mono smoke Player."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone


CORE_FILES = ("Assets/MyLab/Core",)
INPUT_FILES = ("Assets/MyLab/Input/Runtime",)
PARENT_METAS = (
    "Assets/MyLab.meta",
    "Assets/MyLab/Core.meta",
    "Assets/MyLab/Input.meta",
    "Assets/MyLab/Input/Runtime.meta",
    "Assets/Plugins.meta",
    "Assets/Plugins/CsvHelper.meta",
)
CSV_FILES = (
    "Assets/Plugins/CsvHelper/CsvHelper.dll",
    "Assets/Plugins/CsvHelper/CsvHelper.dll.meta",
    "Assets/Plugins/CsvHelper/LICENSE.txt",
    "Assets/Plugins/CsvHelper/LICENSE.txt.meta",
)
PLAYER_RESULT_ENV = "MYLAB_CONSUMER_PLAYER_RESULT"
EDITOR_RESULT_ENV = "MYLAB_CONSUMER_EDITOR_RESULT"
PLAYER_PATH_ENV = "MYLAB_CONSUMER_PLAYER_PATH"


def inside(path: Path, parent: Path) -> bool:
    try:
        path.relative_to(parent)
        return True
    except ValueError:
        return False


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def unity_version(project: Path) -> str:
    text = (project / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
    for line in text.splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    raise ValueError("ProjectVersion.txt has no m_EditorVersion.")


def input_project_settings(project: Path) -> str:
    lines = (project / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8").splitlines()
    try:
        player_settings = lines.index("PlayerSettings:")
        serialized_version = next(line.strip().split(":", 1)[1].strip()
                                  for line in lines[player_settings + 1:]
                                  if line.startswith("  serializedVersion:"))
    except (ValueError, StopIteration, IndexError) as error:
        raise ValueError("Could not read PlayerSettings serializedVersion from the source project.") from error
    if not serialized_version.isdigit():
        raise ValueError("PlayerSettings serializedVersion must be an integer.")
    return ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!129 &1\nPlayerSettings:\n"
            "  m_ObjectHideFlags: 0\n"
            f"  serializedVersion: {serialized_version}\n"
            "  activeInputHandler: 1\n")


def resolve_paths(project_arg: str, output_arg: str, evidence_arg: str) -> tuple[Path, Path, Path]:
    project = Path(project_arg)
    if not project.is_absolute():
        raise ValueError("--project must be an absolute path.")
    project = project.resolve(strict=True)
    if not (project / "Assets/MyLab/Core/MyLab.Core.asmdef").is_file():
        raise ValueError("--project is not a MyLab checkout with Assets/MyLab/Core.")
    output = Path(output_arg)
    if not output.is_absolute():
        output = project / output
    output = output.resolve()
    temp_root = (project / "Temp").resolve()
    if output == temp_root or not inside(output, temp_root):
        raise ValueError("--output must be a new directory below this project's Temp.")
    if output.exists():
        raise FileExistsError("Output already exists; it will not be removed or reused: " + str(output))
    evidence = Path(evidence_arg)
    if not evidence.is_absolute():
        evidence = project / evidence
    evidence = evidence.resolve()
    evidence_roots = ((project / "doc/validation/scene-integration").resolve(),
                      (project / "doc/validation/input-system").resolve())
    if not any(inside(evidence, root) for root in evidence_roots):
        raise ValueError("--evidence must be inside doc/validation/scene-integration or doc/validation/input-system.")
    return project, output, evidence


def source_files(project: Path, include_input: bool = False) -> list[Path]:
    files = []
    for root in CORE_FILES:
        base = project / root
        if not base.is_dir():
            raise FileNotFoundError("Missing allowlisted source folder: " + str(base))
        for path in sorted(base.rglob("*")):
            if path.is_symlink():
                raise ValueError("Symlinks are not accepted in the Core allowlist: " + str(path))
            if path.is_file():
                if path.suffix not in (".cs", ".asmdef", ".meta"):
                    raise ValueError("Unexpected Core file type requires explicit allowlist review: " + str(path))
                files.append(path)
    if include_input:
        for root in INPUT_FILES:
            base = project / root
            if not base.is_dir():
                raise FileNotFoundError("Missing allowlisted source folder: " + str(base))
            for path in sorted(base.rglob("*")):
                if path.is_symlink():
                    raise ValueError("Symlinks are not accepted in the Input allowlist: " + str(path))
                if path.is_file():
                    if path.suffix not in (".cs", ".asmdef", ".meta"):
                        raise ValueError("Unexpected Input file type requires explicit allowlist review: " + str(path))
                    files.append(path)
    parent_metas = PARENT_METAS if include_input else tuple(
        item for item in PARENT_METAS if item not in ("Assets/MyLab/Input.meta", "Assets/MyLab/Input/Runtime.meta"))
    files.extend(project / item for item in parent_metas + CSV_FILES)
    files.extend((project / "doc/licenses/UniTask-LICENSE.txt",))
    missing = [str(path) for path in files if not path.is_file()]
    if missing:
        raise FileNotFoundError("Missing allowlisted file(s): " + ", ".join(missing))
    return files


def copy_allowlist(project: Path, output: Path, tool_root: Path, version: str,
                   include_input: bool = False) -> list[dict[str, str]]:
    destinations = []
    for source in source_files(project, include_input):
        relative = source.relative_to(project)
        if relative.as_posix() == "doc/licenses/UniTask-LICENSE.txt":
            destination = output / "Licenses/UniTask-LICENSE.txt"
        else:
            destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        destinations.append({"source": relative.as_posix(), "destination": destination.relative_to(output).as_posix(),
                             "sha256": sha256(source)})

    templates = tool_root / "core-consumer/templates"
    (output / "Assets/Editor").mkdir(parents=True, exist_ok=True)
    (output / "Assets/MyLabConsumer/Runtime").mkdir(parents=True, exist_ok=True)
    (output / "Assets/MyLabConsumer/Scenes").mkdir(parents=True, exist_ok=True)
    (output / "Packages").mkdir(parents=True, exist_ok=True)
    (output / "ProjectSettings").mkdir(parents=True, exist_ok=True)
    (output / "Assets/Editor/MyLabConsumerBuild.cs").write_bytes((templates / "ConsumerBuild.cs").read_bytes())
    (output / "Assets/MyLabConsumer/Runtime/ConsumerSmoke.cs").write_bytes((templates / "ConsumerSmoke.cs").read_bytes())
    manifest = json.loads((templates / "manifest.json.in").read_text(encoding="utf-8"))
    if include_input:
        manifest["dependencies"]["com.unity.inputsystem"] = "1.19.0"
        manifest["dependencies"]["com.unity.modules.uielements"] = "1.0.0"
        (output / "Assets/csc.rsp").write_text("-define:MYLAB_INPUT_CONSUMER\n", encoding="utf-8")
        (output / "ProjectSettings/ProjectSettings.asset").write_text(
            input_project_settings(project), encoding="utf-8")
    (output / "Packages/manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    project_template = (templates / "ProjectVersion.txt.in").read_text(encoding="utf-8")
    (output / "ProjectSettings/ProjectVersion.txt").write_text(
        project_template.replace("@UNITY_VERSION@", version), encoding="utf-8")
    return destinations


def run_process(command: list[str], cwd: Path, env: dict[str, str], process_log: Path,
                stdout_log: Path, timeout: int) -> dict:
    for path in (process_log, stdout_log):
        if path.exists():
            raise FileExistsError("Refusing to overwrite process log: " + str(path))
    started = datetime.now(timezone.utc)
    started_epoch = time.time()
    creation_flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
    startup_info = None
    if os.name == "nt":
        startup_info = subprocess.STARTUPINFO()
        startup_info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup_info.wShowWindow = subprocess.SW_HIDE
    with stdout_log.open("xb") as log:
        process = subprocess.Popen(command, cwd=str(cwd), env=env, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=creation_flags, startupinfo=startup_info)
        pid = process.pid
        timed_out = False
        try:
            return_code = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            timed_out = True
            process.terminate()
            try:
                return_code = process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                return_code = process.wait()
    ended = datetime.now(timezone.utc)
    return {"pid": pid, "startedUtc": started.isoformat(), "startedUnix": started_epoch,
            "endedUtc": ended.isoformat(),
            "elapsedSeconds": round(time.time() - started_epoch, 3), "returnCode": return_code,
            "timedOut": timed_out, "processLog": str(process_log), "stdoutLog": str(stdout_log),
            "processLogFresh": process_log.is_file() and process_log.stat().st_size > 0 and
            process_log.stat().st_mtime >= started_epoch - 1,
            "stdoutLogFresh": stdout_log.is_file() and stdout_log.stat().st_mtime >= started_epoch - 1}


def read_json(path: Path, started_unix: float) -> dict:
    if not path.is_file():
        raise FileNotFoundError("Expected fresh result JSON was not written: " + str(path))
    if path.stat().st_mtime < started_unix - 1:
        raise RuntimeError("Result JSON predates this process: " + str(path))
    return json.loads(path.read_text(encoding="utf-8"))


def all_true(observations: dict, include_input: bool = False) -> bool:
    required = ("commonRootReady", "gameSceneLoaded", "commonRootPrepared", "activeSceneOwned",
                "canProceedAfterEntry", "derivedAdded", "derivedRemoved", "poolReused", "csvTypedLookup",
                "emptyResourceManagerShutdown", "gracefulShutdown")
    return all(observations.get(name) is True for name in required) and (
        not include_input or observations.get("inputScopeVerified") is True)


def normalize_package_version(version):
    return version.rsplit("#", 1)[-1] if isinstance(version, str) and "#" in version else version


def package_versions(project: Path) -> dict:
    lock = project / "Packages/packages-lock.json"
    if not lock.is_file():
        return {}
    dependencies = json.loads(lock.read_text(encoding="utf-8")).get("dependencies", {})
    names = ("com.cysharp.unitask", "com.unity.addressables", "com.unity.modules.assetbundle",
             "com.unity.modules.imageconversion", "com.unity.modules.jsonserialize",
             "com.unity.modules.unitywebrequest", "com.unity.modules.unitywebrequestassetbundle")
    if "com.unity.inputsystem" in dependencies:
        names += ("com.unity.inputsystem", "com.unity.modules.uielements")
    versions = {name: dependencies.get(name, {}).get("version") for name in names}
    return {name: normalize_package_version(version) for name, version in versions.items()}


def execute(project: Path, unity: Path, output: Path, evidence_dir: Path, timeout: int,
            include_input: bool = False) -> tuple[dict, Path]:
    version = unity_version(project)
    tool_root = Path(__file__).resolve().parent
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + str(os.getpid())
    evidence_dir.mkdir(parents=True, exist_ok=True)
    evidence_path = evidence_dir / ("core-consumer-" + run_id + ".json")
    if evidence_path.exists():
        raise FileExistsError("Evidence already exists: " + str(evidence_path))
    output.mkdir(parents=True)
    report = {"schemaVersion": 1, "runId": run_id, "sourceProject": str(project), "unityExecutable": str(unity),
              "expectedUnityVersion": version, "outputProject": str(output), "createdUtc": datetime.now(timezone.utc).isoformat(),
              "includeInput": include_input, "allowlist": [], "harnessTemplates": {}, "manifest": {},
              "editor": {}, "player": {},
              "packageVersions": {}, "observations": {},
              "success": False, "actualRuns": 0, "errors": []}
    try:
        report["allowlist"] = copy_allowlist(project, output, tool_root, version, include_input)
        input_asmdef = output / "Assets/MyLab/Input/Runtime/MyLab.Core.Input.asmdef"
        input_package = "com.unity.inputsystem" in json.loads(
            (output / "Packages/manifest.json").read_text(encoding="utf-8"))["dependencies"]
        input_tree = output / "Assets/MyLab/Input"
        if (input_asmdef.is_file() != include_input or input_package != include_input or
                input_tree.exists() != include_input):
            raise RuntimeError("Copied Input module/package do not match the requested consumer scope.")
        template_paths = (tool_root / "core-consumer/templates/ConsumerBuild.cs",
                          tool_root / "core-consumer/templates/ConsumerSmoke.cs",
                          tool_root / "core-consumer/templates/manifest.json.in",
                          tool_root / "core-consumer/templates/ProjectVersion.txt.in")
        report["harnessTemplates"] = {path.name: sha256(path) for path in template_paths}
        report["manifest"] = {"requested": package_versions_from_manifest(output),
                              "sha256": sha256(output / "Packages/manifest.json")}
        consumer_player = output / "Build/ConsumerSmoke.exe"
        editor_result = output / "Results/editor.json"
        player_result = output / "Results/player.json"
        editor_log = output / "Logs/editor.log"
        editor_log.parent.mkdir(parents=True, exist_ok=True)
        env = os.environ.copy()
        env[EDITOR_RESULT_ENV] = str(editor_result)
        env[PLAYER_RESULT_ENV] = str(player_result)
        env[PLAYER_PATH_ENV] = str(consumer_player)
        editor_command = [str(unity), "-batchmode", "-nographics", "-projectPath", str(output),
                          "-executeMethod", "MyLabConsumer.ConsumerBuild.Perform", "-logFile", str(editor_log), "-quit"]
        report["editor"] = run_process(editor_command, output, env, editor_log,
                                       output / "Logs/editor-stdout.log", timeout)
        report["actualRuns"] = 1
        editor_result_data = read_json(editor_result, report["editor"]["startedUnix"])
        report["editor"]["result"] = editor_result_data
        if not report["editor"]["processLogFresh"] or report["editor"]["returnCode"] != 0 or not editor_result_data.get("success"):
            raise RuntimeError("Consumer Editor batch build did not pass its result and log gates.")
        if editor_result_data.get("unityVersion") != version:
            raise RuntimeError("Consumer Editor version does not match source ProjectVersion.txt.")
        if editor_result_data.get("backend") != "Mono2x" or editor_result_data.get("target") != "StandaloneWindows64":
            raise RuntimeError("Consumer build did not use Windows Standalone Mono.")
        if package_versions(output) != package_versions_from_manifest(output):
            raise RuntimeError("Resolved package versions differ from the approved manifest versions.")
        if include_input and package_versions(output).get("com.unity.inputsystem") != "1.19.0":
            raise RuntimeError("Consumer Input System package did not resolve to 1.19.0.")
        if not consumer_player.is_file():
            raise FileNotFoundError("Windows Mono Player was not produced: " + str(consumer_player))

        player_env = env.copy()
        player_result.parent.mkdir(parents=True, exist_ok=True)
        player_process_log = output / "Logs/player.log"
        player_command = [str(consumer_player), "-batchmode", "-nographics", "-logFile", str(player_process_log)]
        player = run_process(player_command, output, player_env, player_process_log,
                             output / "Logs/player-stdout.log", timeout)
        report["player"] = player
        report["actualRuns"] = 2
        observations = read_json(player_result, player["startedUnix"])
        report["observations"] = observations
        report["player"]["result"] = observations
        if not player["processLogFresh"] or player["returnCode"] != 0 or not observations.get("success") or not all_true(observations, include_input):
            raise RuntimeError("Consumer Player smoke did not pass every observed runtime-path gate.")
        report["packageVersions"] = package_versions(output)
        report["success"] = True
    except Exception as exception:
        report["errors"].append(str(exception))
        report["packageVersions"] = package_versions(output)
    evidence_path.write_text(json.dumps(report, indent=2, sort_keys=True), encoding="utf-8")
    return report, evidence_path


def package_versions_from_manifest(project: Path) -> dict:
    manifest = json.loads((project / "Packages/manifest.json").read_text(encoding="utf-8"))
    deps = manifest.get("dependencies", {})
    versions = {"com.cysharp.unitask": deps.get("com.cysharp.unitask").split("#")[-1],
            **{name: deps.get(name) for name in (
                "com.unity.addressables", "com.unity.modules.assetbundle", "com.unity.modules.imageconversion",
                "com.unity.modules.jsonserialize", "com.unity.modules.unitywebrequest",
                "com.unity.modules.unitywebrequestassetbundle")}}
    if "com.unity.inputsystem" in deps:
        versions.update({"com.unity.inputsystem": deps["com.unity.inputsystem"],
                         "com.unity.modules.uielements": deps["com.unity.modules.uielements"]})
    return versions


def self_check() -> int:
    assert all_true({key: True for key in ("commonRootReady", "gameSceneLoaded", "commonRootPrepared", "activeSceneOwned",
               "canProceedAfterEntry", "derivedAdded", "derivedRemoved", "poolReused", "csvTypedLookup",
               "emptyResourceManagerShutdown", "gracefulShutdown")})
    assert all_true({key: True for key in ("commonRootReady", "gameSceneLoaded", "commonRootPrepared", "activeSceneOwned",
               "canProceedAfterEntry", "derivedAdded", "derivedRemoved", "poolReused", "csvTypedLookup",
               "emptyResourceManagerShutdown", "gracefulShutdown", "inputScopeVerified")}, True)
    assert not all_true({"inputScopeVerified": False}, True)
    assert not all_true({"commonRootReady": True})
    project = Path("C:/MyLab")
    temp_root = project / "Temp"
    output = temp_root / "consumer-new"
    assert inside(output, temp_root)
    assert not inside(project / "Assets/Consumer", temp_root)
    assert normalize_package_version("https://example.invalid/repo#2.5.11") == "2.5.11"
    assert normalize_package_version("2.9.1") == "2.9.1"
    settings = input_project_settings(Path(__file__).resolve().parents[1])
    assert "serializedVersion: 28\n" in settings and "activeInputHandler: 1\n" in settings
    print("self-check: PASS")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", help="Absolute MyLab project path.")
    parser.add_argument("--unity", help="Exact Unity Editor executable path.")
    parser.add_argument("--output", help="New output directory below project Temp.")
    parser.add_argument("--evidence", help="Evidence directory below doc/validation/scene-integration or doc/validation/input-system.")
    parser.add_argument("--timeout-seconds", type=int, default=900)
    parser.add_argument("--include-input", action="store_true",
                        help="Include MyLab.Core.Input and Input System 1.19.0 in the isolated consumer.")
    parser.add_argument("--self-check", action="store_true")
    args = parser.parse_args()
    if args.self_check:
        return self_check()
    if not args.project or not args.unity or not args.output or not args.evidence:
        parser.error("--project, --unity, --output, and --evidence are required unless --self-check is used")
    try:
        project, output, evidence = resolve_paths(args.project, args.output, args.evidence)
        unity = Path(args.unity)
        if not unity.is_absolute() or not unity.is_file() or unity.suffix.lower() != ".exe":
            raise ValueError("--unity must be the exact existing absolute Unity .exe path.")
        if args.timeout_seconds < 60:
            raise ValueError("--timeout-seconds must be at least 60.")
        report, evidence_path = execute(project, unity.resolve(), output, evidence, args.timeout_seconds,
                                        args.include_input)
        print(json.dumps({"success": report["success"], "actualRuns": report["actualRuns"],
                          "evidence": str(evidence_path), "output": str(output), "errors": report["errors"]}, indent=2))
        return 0 if report["success"] else 1
    except Exception as exception:
        print(str(exception), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
