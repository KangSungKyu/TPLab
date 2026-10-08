#!/usr/bin/env python3
"""Source-only UI consumer. Import is inert; execute only with a reviewed exact SHA."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import re
import shutil
import statistics
import subprocess
import sys
import time
import uuid

SHA = re.compile(r"[0-9a-f]{40}")
DEPENDENCIES = (
    "Assets/Plugins/CsvHelper/CsvHelper.dll", "Assets/Plugins/CsvHelper/CsvHelper.dll.meta",
    "Assets/Plugins/CsvHelper/LICENSE.txt", "Assets/Plugins/CsvHelper/LICENSE.txt.meta",
    "doc/licenses/UniTask-LICENSE.txt",
)


def _refuse_links(path: Path) -> None:
    for candidate in (path, *path.parents):
        try:
            linked = candidate.is_symlink() or bool(getattr(candidate.lstat(), "st_file_attributes", 0) & 0x400)
        except FileNotFoundError:
            linked = False
        if linked:
            raise ValueError("Symlink/junction/reparse point refused: " + str(candidate))


def source_files(project: Path, include_input: bool = False) -> list[Path]:
    """Exact runtime trees, required metas and licensed dependencies; never a whole UI copy."""
    project = Path(project).absolute()
    _refuse_links(project)
    roots = ["Assets/TPLab/Core", "Assets/TPLab/UI/Runtime"]
    if include_input:
        roots += ["Assets/TPLab/Input/Runtime", "Assets/TPLab/UI/InputSystem/Runtime"]
    selected = set()
    for relative in roots:
        root = project / relative
        _refuse_links(root)
        if not root.is_dir():
            raise FileNotFoundError(root)
        for directory, subdirectories, files in os.walk(root, followlinks=False):
            current = Path(directory)
            _refuse_links(current)
            selected.add(Path(str(current) + ".meta"))
            for child in subdirectories:
                _refuse_links(current / child)
                if child.lower() in {"tests", "editor", "scenes", "settings", "samples"}:
                    raise ValueError("Non-runtime directory inside selected runtime tree: " + str(current / child))
            for name in files:
                source = current / name
                _refuse_links(source)
                if source.suffix not in {".cs", ".asmdef", ".meta"}:
                    raise ValueError("Unreviewed runtime extension: " + str(source))
                selected.add(source)
                if source.suffix != ".meta":
                    selected.add(Path(str(source) + ".meta"))
                elif not Path(str(source)[:-5]).exists():
                    raise ValueError("Orphan runtime meta: " + str(source))
        parent = root.parent
        while parent != project / "Assets":
            selected.add(Path(str(parent) + ".meta"))
            parent = parent.parent
    selected.update(project / name for name in DEPENDENCIES)
    selected.update(project / name for name in ("Assets/Plugins.meta", "Assets/Plugins/CsvHelper.meta"))
    for source in selected:
        _refuse_links(source)
        if not source.is_file():
            raise FileNotFoundError(source)
    # A dependency directory can never be replaced by a link, even with valid-looking children.
    _refuse_links(project / "Assets/Plugins/CsvHelper")
    return sorted(selected, key=lambda path: path.relative_to(project).as_posix())


def resolve_ui_paths(project: Path, output_arg: str, evidence_arg: str) -> tuple[Path, Path]:
    """Fresh immediate Temp child and fresh immediate UI p7 evidence child. No mutation."""
    project = Path(project).absolute()
    _refuse_links(project)
    def scoped(argument, parent):
        raw = Path(argument)
        if any(part in {".", ".."} for part in raw.parts):
            raise ValueError("Path traversal refused.")
        candidate = raw if raw.is_absolute() else project / raw
        _refuse_links(candidate)
        candidate = candidate.absolute()
        if candidate.parent != parent or candidate == parent or not candidate.name:
            raise ValueError("Expected a new direct child of " + str(parent))
        if candidate.exists():
            raise FileExistsError(candidate)
        return candidate
    return scoped(output_arg, project / "Temp"), scoped(evidence_arg, project / "doc/validation/ui-system/p7")


def _finite(value):
    return type(value) in (int, float) and math.isfinite(value)


def validate_result(result: dict, expected_run_id: str, expected_revision: str,
                    expected_project: Path, started_unix: float) -> bool:
    """Validate actual identity/render proof; unavailable counters retain empty data and null stats."""
    try:
        version = next(line.split(":", 1)[1].strip() for line in
                       (Path(expected_project) / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()
                       if line.startswith("m_EditorVersion:"))
        if not SHA.fullmatch(expected_revision or "") or not isinstance(result, dict):
            return False
        if (result.get("runId") != expected_run_id or result.get("sourceRevision") != expected_revision or
            Path(result.get("projectPath", "")).absolute() != Path(expected_project).absolute() or
            result.get("unityVersion") != version or result.get("backend") != "Mono2x" or
            result.get("target") != "StandaloneWindows64" or
            not isinstance(result.get("graphicsDeviceType"), str) or
            result["graphicsDeviceType"].lower() in {"null", "none", "", "na", "n/a"}):
            return False
        begin, end = result.get("startedUnix"), result.get("completedUnix")
        frames, draws = result.get("actualFrames"), result.get("drawCalls")
        if (not all(_finite(value) for value in (begin, end, started_unix)) or
            begin < started_unix or end < begin or type(frames) is not int or frames <= 0 or
            not _finite(draws) or draws <= 0):
            return False
        counters = result.get("profiler")
        if not isinstance(counters, dict) or not counters:
            return False
        for counter in counters.values():
            if not isinstance(counter, dict) or not counter.get("unit"):
                return False
            samples = counter.get("samples")
            stats = [counter.get(key) for key in ("median", "p95", "max")]
            if counter.get("status") == "N/A":
                if samples != [] or any(value is not None for value in stats) or not counter.get("reason"):
                    return False
            elif counter.get("status") in {"Available", "Partial"}:
                fresh = counter.get("freshSamples")
                if (not isinstance(samples, list) or not samples or type(fresh) is not int or
                    fresh != len(samples) or fresh > frames or
                    any(not _finite(value) or value < 0 for value in samples) or
                    any(not _finite(value) for value in stats)):
                    return False
                if counter["status"] == "Available" and fresh != frames:
                    return False
                if counter["status"] == "Partial" and (fresh >= frames or not counter.get("reason")):
                    return False
                ordered = sorted(samples)
                expected = [statistics.median(ordered), ordered[math.ceil(len(ordered) * .95) - 1], ordered[-1]]
                if stats != expected:
                    return False
            else:
                return False
        return True
    except (OSError, ValueError, TypeError, StopIteration, KeyError):
        return False


def source_identity(project: Path, expected_revision: str, include_input: bool = False) -> dict:
    """Exact HEAD and Git-clean blob identity; canonical and untouched working hashes remain separate."""
    project = Path(project).absolute()
    _refuse_links(project)
    if not SHA.fullmatch(expected_revision or ""):
        raise ValueError("An exact lowercase 40-digit revision is required.")
    environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    environment["GIT_TERMINAL_PROMPT"] = "0"
    def git(*arguments):
        process = subprocess.run(["git", "--literal-pathspecs", *arguments], cwd=project,
                                 env=environment, capture_output=True, check=False)
        if process.returncode:
            raise ValueError("Read-only Git identity check failed: " + process.stderr.decode(errors="replace"))
        return process.stdout
    if Path(os.fsdecode(git("rev-parse", "--show-toplevel")).strip()).resolve() != project.resolve():
        raise ValueError("The selected project must be its repository root.")
    if git("rev-parse", "HEAD").decode().strip() != expected_revision:
        raise ValueError("HEAD does not equal the selected development revision.")
    try:
        paths = source_files(project, include_input)
    except FileNotFoundError as error:
        raise ValueError("Missing tracked allowlist content/meta: " + str(error)) from error
    hashes, working_hashes = {}, {}
    for source in paths:
        relative = source.relative_to(project).as_posix()
        entry = git("ls-tree", expected_revision, "--", relative).decode().strip()
        if not entry or entry.split()[0] not in {"100644", "100755"}:
            raise ValueError("Allowlisted path is not a tracked regular blob: " + relative)
        blob = git("cat-file", "blob", expected_revision + ":" + relative)
        canonical_oid = entry.split()[2]
        working_oid = git("hash-object", "--path=" + relative, str(source)).decode().strip()
        if working_oid != canonical_oid:
            raise ValueError("Allowlisted Git-clean working content differs from selected Git blob: " + relative)
        hashes[relative] = hashlib.sha256(blob).hexdigest()
        working_hashes[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
    if git("rev-parse", "HEAD").decode().strip() != expected_revision:
        raise ValueError("HEAD changed during source verification.")
    return {"revision": expected_revision, "paths": list(hashes), "sha256": hashes, "workingRawSha256": working_hashes, "includeInput": include_input}


def _load_core(path):
    _refuse_links(path)
    spec = importlib.util.spec_from_file_location("ui_consumer_core_helpers", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _write(path, value):
    if path.exists():
        raise FileExistsError(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False, allow_nan=False) + "\n", encoding="utf-8")


def _phase_counters(counters, phase_name):
    result = {}
    for counter in counters:
        phase = next((item for item in counter["phases"] if item["name"] == phase_name), None)
        if phase is None or counter["name"] in result:
            raise ValueError("Missing/duplicate measured profiler marker.")
        status = {"Measured": "Available", "Partial": "Partial", "N/A": "N/A"}.get(phase["status"])
        summary = phase.get("summaryRaw", [])
        samples = phase.get("rawValues", [])
        if status == "N/A" and (summary or samples):
            raise ValueError("Unavailable marker fabricated measurements.")
        if status != "N/A" and len(summary) != 3:
            raise ValueError("Missing measured statistics.")
        result[counter["name"]] = {
            "status": status, "reason": phase.get("reason") or counter.get("reason") or "Recorder unavailable",
            "unit": {"TimeNanoseconds": "ns", "Bytes": "bytes", "Count": "count"}.get(counter["rawUnit"], counter["rawUnit"]),
            "freshSamples": phase["freshSamples"], "samples": samples, "graphicsEligibleFrames": phase["graphicsEligibleFrames"],
            "median": summary[0] if summary else None, "p95": summary[1] if summary else None,
            "max": summary[2] if summary else None,
        }
        frames = phase.get("unityFrames", [])
        if len(frames) != len(samples) or any(b != a + 1 for a, b in zip(frames, frames[1:])):
            # Partial coverage may have gaps; the raw status must preserve the missing interval explicitly.
            if status == "Available":
                raise ValueError("Available marker frame sequence was discontinuous.")
    return result


def _extra_identity(project, revision, include_sample):
    """Additional licensing and the explicitly selected sample are separate from the frozen helper scope."""
    names = {"LICENSE", "THIRD_PARTY_NOTICES.md"}
    if include_sample:
        root = project / "Assets/TPLab/Samples/UI"
        for relative in ("Runtime", "Editor"):
            directory = root / relative
            _refuse_links(directory)
            if not directory.is_dir():
                raise FileNotFoundError(directory)
            for source in directory.rglob("*"):
                _refuse_links(source)
                if source.is_dir():
                    raise ValueError("Unexpected nested sample code directory: " + str(source))
                if source.suffix not in {".cs", ".asmdef", ".meta"}:
                    raise ValueError("Unreviewed sample code file: " + str(source))
                names.add(source.relative_to(project).as_posix())
                if source.suffix != ".meta":
                    names.add(source.relative_to(project).as_posix() + ".meta")
        assets = ["Prefabs/" + name + ".prefab" for name in
                  ("HudA", "HudB", "PopupA", "PopupB", "PopupC", "Inventory", "Cell", "SceneHud")]
        assets += ["Settings/" + name + ".asset" for name in
                   ("CommonUI", "SceneUI", "TransitionsSingle", "TransitionsAdditive")]
        assets += ["Settings/UIInput.inputactions"]
        assets += ["Scenes/UIContext" + name + ".unity" for name in
                   ("Hub", "Main", "Area", "Nested", "BootstrapSingle", "BootstrapAdditive")]
        for relative in assets:
            names.add("Assets/TPLab/Samples/UI/" + relative)
            names.add("Assets/TPLab/Samples/UI/" + relative + ".meta")
        for directory in ("Prefabs", "Settings", "Scenes"):
            path = root / directory
            _refuse_links(path)
            if not path.is_dir():
                raise FileNotFoundError(path)
            for item in path.iterdir():
                _refuse_links(item)
                if item.relative_to(project).as_posix() not in names:
                    raise ValueError("Unreviewed sample asset: " + str(item))
        names.update("Assets/TPLab/Samples/UI/" + name + ".meta" for name in
                     ("Runtime", "Editor", "Prefabs", "Settings", "Scenes"))
        names.update(("Assets/TPLab/Samples.meta", "Assets/TPLab/Samples/UI.meta"))
    env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    env["GIT_TERMINAL_PROMPT"] = "0"
    def git(*arguments):
        done = subprocess.run(["git", "--literal-pathspecs", *arguments], cwd=project, env=env, capture_output=True)
        if done.returncode:
            raise ValueError("Additional tracked source check failed: " + done.stderr.decode(errors="replace"))
        return done.stdout
    if git("rev-parse", "HEAD").decode().strip() != revision:
        raise ValueError("Source revision changed before additional license/sample verification.")
    hashes, working_hashes = {}, {}
    for relative in sorted(names):
        source = project / relative
        _refuse_links(source)
        entry = git("ls-tree", revision, "--", relative).decode().strip()
        if not source.is_file() or not entry or entry.split()[0] not in {"100644", "100755"}:
            raise ValueError("Missing tracked regular license/sample path: " + relative)
        blob = git("cat-file", "blob", revision + ":" + relative)
        if git("hash-object", "--path=" + relative, str(source)).decode().strip() != entry.split()[2]:
            raise ValueError("License/sample Git-clean working content differs from exact revision: " + relative)
        hashes[relative] = hashlib.sha256(blob).hexdigest()
        working_hashes[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
    return {"sha256": hashes, "workingRawSha256": working_hashes}


def _copy_canonical(project, revision, relative, target, canonical_sha256, working_sha256):
    """Write the exact selected blob to the new consumer, preserving original working bytes."""
    source = project / relative
    _refuse_links(source)
    if hashlib.sha256(source.read_bytes()).hexdigest() != working_sha256:
        raise ValueError("Source working bytes changed before canonical copy: " + relative)
    env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    env["GIT_TERMINAL_PROMPT"] = "0"
    done = subprocess.run(["git", "--literal-pathspecs", "cat-file", "blob", revision + ":" + relative],
                          cwd=project, env=env, capture_output=True)
    if done.returncode or hashlib.sha256(done.stdout).hexdigest() != canonical_sha256:
        raise ValueError("Selected canonical blob verification failed: " + relative)
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("xb") as stream:
        stream.write(done.stdout)


def _run_graphics_process(command, cwd, env, process_log, stdout_log, timeout):
    """Bounded visible non-activating Player; hidden Windows windows can produce zero draws."""
    for path in (process_log, stdout_log):
        if path.exists():
            raise FileExistsError(path)
    started = time.time()
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 4  # SW_SHOWNOACTIVATE: render without requesting keyboard focus.
    timed_out = False
    with stdout_log.open("xb") as log:
        process = subprocess.Popen(command, cwd=str(cwd), env=env, stdout=log,
                                   stderr=subprocess.STDOUT, startupinfo=startup)
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
    ended = time.time()
    return {"pid": process.pid, "startedUnix": started, "completedUnix": ended,
            "elapsedSeconds": round(ended - started, 3), "returnCode": return_code,
            "timedOut": timed_out, "windowPolicy": "SW_SHOWNOACTIVATE",
            "processLog": str(process_log), "stdoutLog": str(stdout_log),
            "processLogFresh": process_log.is_file() and process_log.stat().st_size > 0 and
            process_log.stat().st_mtime >= started - 1,
            "stdoutLogFresh": stdout_log.is_file() and stdout_log.stat().st_mtime >= started - 1}


def execute(args):
    if os.name != "nt":
        raise ValueError("Actual consumer requires a graphics-capable Windows host.")
    if not Path(args.project).is_absolute() or args.timeout <= 0:
        raise ValueError("An absolute project and positive process timeout are required.")
    project = Path(args.project).absolute()
    core_path = Path(args.core_runner).absolute()
    if core_path != project / "tools/run_core_consumer.py":
        raise ValueError("Use this checkout's explicit existing Core consumer helper path.")
    core = _load_core(core_path)
    core.refuse_links(project)
    output, evidence = resolve_ui_paths(project, args.output, args.evidence)
    identity = source_identity(project, args.revision, args.include_input)
    if args.sample and (not args.include_input or not args.sample_entry):
        raise ValueError("--sample requires --include-input and an explicit --sample-entry.")
    if args.sample_validation_script and not args.sample:
        raise ValueError("--sample-validation-script requires the explicit optional sample scope.")
    if args.sample_entry and not args.sample:
        raise ValueError("--sample-entry requires --sample.")
    additional = _extra_identity(project, args.revision, args.sample)
    template_root = Path(__file__).absolute().parent / "ui-consumer/templates"
    for path in (Path(args.unity), template_root, Path(args.scroll_benchmark), Path(args.canvas_benchmark)):
        _refuse_links(path)
        if not path.exists():
            raise FileNotFoundError(path)
    output.mkdir(parents=True)
    evidence.mkdir(parents=True)
    consumer = output / "Project"
    consumer.mkdir()
    for relative, expected_hash in {**identity["sha256"], **additional["sha256"]}.items():
        target = consumer / relative
        raw_hashes = {**identity["workingRawSha256"], **additional["workingRawSha256"]}
        _copy_canonical(project, args.revision, relative, target, expected_hash, raw_hashes[relative])
        if core.sha256(target) != expected_hash:
            raise ValueError("Copied source hash mismatch: " + relative)
    manifest = json.loads((Path(args.core_runner).parent / "core-consumer/templates/manifest.json.in").read_text())
    manifest["dependencies"].update({"com.unity.ugui": "2.0.0", "com.unity.modules.ui": "1.0.0"})
    if args.include_input:
        manifest["dependencies"].update({"com.unity.inputsystem": "1.19.0", "com.unity.modules.uielements": "1.0.0"})
    _write(consumer / "Packages/manifest.json", manifest)
    settings = consumer / "ProjectSettings"
    settings.mkdir()
    (settings / "ProjectVersion.txt").write_text("m_EditorVersion: " + core.unity_version(project) + "\n", encoding="utf-8")
    if args.include_input:
        (settings / "ProjectSettings.asset").write_text(core.input_project_settings(project), encoding="utf-8")
    runtime = consumer / "Assets/UIConsumer/Runtime"
    editor = consumer / "Assets/UIConsumer/Editor"
    runtime.mkdir(parents=True); editor.mkdir()
    references = ["TPLab.Core", "TPLab.UI", "UnityEngine.UI", "UniTask"]
    if args.include_input:
        references += ["TPLab.Core.Input", "TPLab.UI.InputSystem", "Unity.InputSystem"]
    if args.sample:
        references.append("TPLab.UISamples.OptionalInput")
    runtime_definition = {"name": "UIConsumer.Runtime", "references": references}
    if args.include_input:
        runtime_definition["versionDefines"] = [{"name": "com.unity.inputsystem", "expression": "[1.19.0,1.20.0)", "define": "TPLAB_UI_INPUT"}]
    _write(runtime / "UIConsumer.Runtime.asmdef", runtime_definition)
    _write(editor / "UIConsumer.Editor.asmdef", {"name": "UIConsumer.Editor", "references": ["UIConsumer.Runtime", "UniTask"], "includePlatforms": ["Editor"]})
    harness = {}
    for name, directory in (("UIConsumerSmoke.cs", runtime), ("UIConsumerBuild.cs", editor)):
        source = template_root / name
        _refuse_links(source)
        shutil.copyfile(source, directory / name)
        harness[name] = {"source": str(source), "sha256": core.sha256(source)}
    if args.sample_validation_script:
        source = Path(args.sample_validation_script)
        _refuse_links(source)
        if not source.is_absolute() or source.suffix != ".cs" or not source.is_file():
            raise ValueError("An explicit reviewed absolute sample validation .cs is required.")
        target = runtime / "UIConsumerSampleSmoke.cs"
        shutil.copyfile(source, target)
        harness[target.name] = {"source": str(source), "sha256": core.sha256(source)}
    for argument, name in ((args.scroll_benchmark, "UIVirtualScrollBenchmark.cs"), (args.canvas_benchmark, "UICanvasBenchmark.cs")):
        source = Path(argument).absolute()
        text = source.read_text(encoding="utf-8-sig")
        old = "private async UniTask RunAllAsync(CancellationToken token)"
        if text.count(old) != 1:
            raise ValueError("Frozen benchmark entry signature changed: " + str(source))
        target = runtime / name
        target.write_text(text.replace(old, "public async UniTask RunAllAsync(CancellationToken token)"), encoding="utf-8")
        harness[name] = {"source": str(source), "originalSha256": core.sha256(source), "consumerSha256": core.sha256(target),
                         "adaptation": "Private harness entry becomes public for typed await; measurement body unchanged; original untouched."}
    run_id = uuid.uuid4().hex
    report = {"runId": run_id, "source": identity, "consumerProject": str(consumer), "harness": harness, "additionalSourceIdentity": additional,
              "buildOnly": args.build_only, "sampleEntry": args.sample_entry,
              "coreRunnerSha256": core.sha256(core_path),
              "actualExecutions": [], "success": False, "performanceStatus": "Unmeasured"}
    _write(evidence / "source-manifest.json", report)
    env = os.environ.copy()
    env.update({"TPLAB_UI_RUN_ID": run_id, "TPLAB_UI_SOURCE_REVISION": args.revision,
                "TPLAB_UI_PROJECT": str(consumer), "TPLAB_UI_BENCHMARK_REVISION": args.revision,
                "TPLAB_UI_BENCHMARK_BACKEND": "Mono2x", "TPLAB_UI_INCLUDE_INPUT": "1" if args.include_input else "0"})
    if not args.sample:
        env["TPLAB_UI_SAMPLE_ENTRY"] = ""
        env["TPLAB_UI_SAMPLE_PLAYER_PATH"] = ""
    player = output / "Build/UIConsumer.exe"
    player.parent.mkdir()
    if args.sample:
        env.update({"TPLAB_UI_SAMPLE_ENTRY": args.sample_entry,
                    "TPLAB_UI_SAMPLE_PLAYER_PATH": str(output / "SampleBuild/UIContextSample.exe")})
        (output / "SampleBuild").mkdir()
    editor_result = evidence / "editor-result.json"
    env.update({"TPLAB_UI_EDITOR_RESULT": str(editor_result), "TPLAB_UI_PLAYER_PATH": str(player)})
    try:
        build = core.run_process([str(Path(args.unity).absolute()), "-batchmode", "-nographics", "-projectPath", str(consumer),
                                  "-executeMethod", "UIConsumer.Editor.UIConsumerBuild.Perform", "-logFile", str(evidence / "editor.log")],
                                 consumer, env, evidence / "editor.log", evidence / "editor-stdout.log", args.timeout)
        report["actualExecutions"].append({"kind": "build", **build})
        actual_build = core.read_json(editor_result, build["startedUnix"])
        if (build["returnCode"] != 0 or build["timedOut"] or not build["processLogFresh"] or not build["stdoutLogFresh"] or not actual_build.get("success") or
            actual_build.get("errors") != 0 or actual_build.get("runId") != run_id or
            actual_build.get("sourceRevision") != args.revision or actual_build.get("projectPath") != str(consumer) or
            actual_build.get("unityVersion") != core.unity_version(project) or actual_build.get("backend") != "Mono2x" or
            actual_build.get("target") != "StandaloneWindows64" or actual_build.get("playerPath") != str(player) or not player.is_file()):
            raise ValueError("Fresh isolated Mono consumer build proof failed.")
        if args.sample:
            sample_player = output / "SampleBuild/UIContextSample.exe"
            if not actual_build.get("sampleSuccess") or actual_build.get("sampleErrors") != 0 or not sample_player.is_file() or actual_build.get("samplePlayerPath") != str(sample_player):
                raise ValueError("Explicit isolated sample Player build failed.")
            report["sample"] = {"buildSuccess": True, "playerPath": str(sample_player), "actualPlayerExecutions": 0,
                                "acceptance": "Unexecuted: manual/Core+UI automated acceptance is separate from consumer smoke and benchmarks."}
        report["manifestSha256"] = core.sha256(consumer / "Packages/manifest.json")
        report["lockSha256"] = core.sha256(consumer / "Packages/packages-lock.json")
        report["corePackageVersions"] = core.package_versions(consumer)
        if report["corePackageVersions"] != core.package_versions_from_manifest(consumer):
            raise ValueError("Resolved Core/Input package versions differ from the reused manifest contract.")
        report["packages"] = json.loads((consumer / "Packages/packages-lock.json").read_text())["dependencies"]
        for package, requested in manifest["dependencies"].items():
            locked = report["packages"].get(package, {})
            if locked.get("version") != requested:
                raise ValueError("Consumer package lock does not match requested version: " + package)
        for mode in (() if args.build_only else ("smoke", "scroll", "canvas")):
            result_path = evidence / (mode + "-result.json")
            benchmark_path = evidence / (mode + "-benchmark.json")
            env.update({"TPLAB_UI_MODE": mode, "TPLAB_UI_RUN_MODE": mode, "TPLAB_UI_PLAYER_RESULT": str(result_path),
                        "TPLAB_UI_BENCHMARK_OUTPUT": str(benchmark_path)})
            # Graphics Player: visible without activation; rendering still requires fresh positive draws.
            process = _run_graphics_process([str(player), "-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0",
                                        "-logFile", str(evidence / (mode + "-player.log"))],
                                       consumer, env, evidence / (mode + "-player.log"), evidence / (mode + "-stdout.log"), args.timeout)
            report["actualExecutions"].append({"kind": mode, **process})
            raw = core.read_json(result_path, process["startedUnix"])
            if process["returnCode"] != 0 or process["timedOut"] or not process["processLogFresh"] or not process["stdoutLogFresh"] or not raw.get("success"):
                raise ValueError("Player lifecycle smoke/benchmark failed: " + mode)
            if mode == "smoke":
                if raw.get("includeInputCompiled") is not args.include_input:
                    raise ValueError("Actual compiled optional/base smoke profile differs from selected scope.")
                raw["profiler"] = _phase_counters(raw.pop("counters"), "measure")
                candidates = [raw]
            else:
                benchmark = core.read_json(benchmark_path, process["startedUnix"])
                report[mode + "BenchmarkSha256"] = core.sha256(benchmark_path)
                environment = benchmark["environment"]
                if (environment["sourceRevision"] != args.revision or environment["actualBackend"] != "Mono2x" or
                    environment["suppliedBackend"] != "Mono2x" or environment["isEditor"] or environment["platform"] != "WindowsPlayer" or
                    environment["screenWidth"] != 1280 or environment["screenHeight"] != 720):
                    raise ValueError("Benchmark hardware/identity gate failed.")
                arms = benchmark.get("scenarios", benchmark.get("arms", []))
                if len(arms) != 3 or benchmark.get("warmupFrames") != 120 or benchmark.get("measuredFrames") != 600:
                    raise ValueError("Benchmark interval/arm count failed.")
                candidates = []
                expected_names = {"Native1000", "Virtual1000", "Virtual10000"} if mode == "scroll" else {"SharedCanvas", "FrequencySeparatedCanvases", "PopupPerCanvas"}
                if {arm["name"] for arm in arms} != expected_names:
                    raise ValueError("Exact benchmark arms do not match the reviewed fixture.")
                for arm in arms:
                    if mode == "scroll":
                        geometry = arm["geometry"]
                        if geometry["duplicateIndices"] != 0 or geometry["missingVisibleRows"] != 0:
                            raise ValueError("Virtual/native benchmark endpoint geometry failed.")
                    elif arm["scheduleMismatchFrames"] != 0 or arm["measuredFrameSpan"] != 600:
                        raise ValueError("Canvas lifecycle/visibility schedule deviated during measurement.")
                    candidate = dict(raw)
                    candidate["actualFrames"] = 600
                    candidate["profiler"] = _phase_counters(arm["counters"], "MeasuredScroll" if mode == "scroll" else "MeasuredSchedule")
                    draws = candidate["profiler"].get("Draw Calls Count", {})
                    candidate["drawCalls"] = min(draws.get("samples") or [0])
                    candidates.append(candidate)
            for candidate in candidates:
                draws = candidate["profiler"].get("Draw Calls Count", {})
                if (not validate_result(candidate, run_id, args.revision, consumer, process["startedUnix"]) or
                    candidate.get("actualFrames") != 600 or draws.get("status") != "Available" or draws.get("freshSamples") != 600 or draws.get("graphicsEligibleFrames") != 600 or
                    any(value <= 0 for value in draws.get("samples", [])) or candidate.get("width") != 1280 or candidate.get("height") != 720):
                    raise ValueError("Actual fresh 600-frame graphics result gate failed: " + mode)
            _write(evidence / (mode + "-validated.json"), candidates)
            report[mode] = {"success": True, "arms": len(candidates), "runtimeChecks": raw.get("checks", [])}
            if any(counter["status"] != "Available" for candidate in candidates for counter in candidate["profiler"].values()):
                report["performanceStatus"] = "Partial: unavailable/incomplete markers retained as N/A/Partial"
        if not args.build_only and report["performanceStatus"] == "Unmeasured":
            report["performanceStatus"] = "Measured: all requested markers have complete fresh coverage"
        if args.sample_validation_script and not args.build_only:
            sample_result = evidence / "sample-result.json"
            env.update({"TPLAB_UI_MODE": "sample", "TPLAB_UI_RUN_MODE": "sample", "TPLAB_UI_PLAYER_RESULT": str(sample_result)})
            sample_process = _run_graphics_process([str(sample_player), "-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0",
                                               "-logFile", str(evidence / "sample-player.log")],
                                              consumer, env, evidence / "sample-player.log", evidence / "sample-stdout.log", args.timeout)
            report["actualExecutions"].append({"kind": "sample", **sample_process})
            report["sample"].update({"actualPlayerExecutions": 1, "acceptance": "Executed: sample result validation pending or failed."})
            sample = core.read_json(sample_result, sample_process["startedUnix"])
            begin, end = sample.get("startedUnix"), sample.get("completedUnix")
            if (sample_process["returnCode"] != 0 or sample_process["timedOut"] or not sample_process["processLogFresh"] or
                not sample_process["stdoutLogFresh"] or not sample.get("success") or sample.get("runId") != run_id or
                sample.get("sourceRevision") != args.revision or sample.get("projectPath") != str(consumer) or
                sample.get("unityVersion") != core.unity_version(project) or sample.get("backend") != "Mono2x" or
                sample.get("target") != "StandaloneWindows64" or sample.get("graphicsDeviceType") in (None, "", "Null") or
                sample.get("width") != 1280 or sample.get("height") != 720 or not _finite(begin) or not _finite(end) or
                begin < sample_process["startedUnix"] or end < begin):
                raise ValueError("Distinct sample Player process/identity/lifecycle result failed.")
            report["sample"].update({"actualPlayerExecutions": 1, "identityValidated": True,
                                    "rawResultSha256": core.sha256(sample_result),
                                    "acceptance": "Executed: distinct sample functional/fresh graphics schema still requires root review."})
        if source_identity(project, args.revision, args.include_input) != identity:
            raise ValueError("Original allowlisted source identity changed during consumer run.")
        if _extra_identity(project, args.revision, args.sample) != additional:
            raise ValueError("Additional license/sample identity changed during the consumer run.")
        for relative, expected in {**identity["sha256"], **additional["sha256"]}.items():
            if core.sha256(consumer / relative) != expected:
                raise ValueError("Copied source changed during build/run: " + relative)
        for name, metadata in harness.items():
            directory = editor if name == "UIConsumerBuild.cs" else runtime
            expected = metadata.get("consumerSha256", metadata.get("sha256"))
            if core.sha256(directory / name) != expected:
                raise ValueError("Consumer harness changed during build/run: " + name)
            original = Path(metadata["source"])
            if core.sha256(original) != metadata.get("originalSha256", metadata.get("sha256")):
                raise ValueError("Original harness/benchmark changed during build/run: " + name)
        if core.sha256(core_path) != report["coreRunnerSha256"]:
            raise ValueError("Core pure helper source changed during execution.")
        report["success"] = True
        if not args.build_only and report["performanceStatus"] == "Unmeasured":
            report["performanceStatus"] = "Measured: all requested markers have complete fresh coverage"
    except Exception as error:
        report["error"] = str(error)
    finally:
        _write(evidence / "consumer-report.json", report)
    print(json.dumps({"success": report["success"], "evidence": str(evidence), "performanceStatus": report["performanceStatus"]}))
    return 0 if report["success"] else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("project", "revision", "unity", "output", "evidence", "core-runner", "scroll-benchmark", "canvas-benchmark"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--include-input", action="store_true")
    parser.add_argument("--sample", action="store_true")
    parser.add_argument("--sample-entry", choices=("UIContextBootstrapSingle", "UIContextBootstrapAdditive"))
    parser.add_argument("--sample-validation-script", help="Explicit reviewed RuntimeInitialize sample validation .cs; distinct result schema.")
    parser.add_argument("--build-only", action="store_true", help="Build only; all smoke/performance/visual acceptance remains unexecuted.")
    parser.add_argument("--timeout", type=int, default=1200)
    return execute(parser.parse_args())


if __name__ == "__main__":
    sys.exit(main())
