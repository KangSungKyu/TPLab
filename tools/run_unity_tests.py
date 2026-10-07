"""Run tests in an explicitly selected existing Editor; preserve results across CLI domain-reload disconnects."""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys
import time


def result_from_text(text):
    decoder = json.JSONDecoder()
    for position, character in enumerate(text):
        if character != "{":
            continue
        try:
            value, _ = decoder.raw_decode(text[position:])
        except ValueError:
            continue
        if isinstance(value, dict):
            value = value.get("data", value)
            if isinstance(value, dict) and all(key in value for key in ("total", "passed", "failed", "skipped")):
                return value
    return None


def is_prestart_discovery_gap(transcript, result):
    return result is None and transcript.strip() == "Error: no Unity instances running"


def has_compilation_error(text):
    return re.search(r"\berror CS\d{4}\b", text) is not None


def wait_for_editor(project, expected_pid=None):
    deadline = time.monotonic() + 30
    while True:
        probe = subprocess.run(["unity-cli", "status", "--project", project.as_posix()],
                               capture_output=True, encoding="utf-8")
        status = probe.stdout + probe.stderr
        if probe.returncode == 0 and status.startswith("Unity: ready"):
            break
        if time.monotonic() >= deadline:
            raise RuntimeError("The selected Editor did not become ready: " + status)
        time.sleep(0.5)
    pid = re.search(r"^\s*PID:\s*(\d+)\s*$", status, re.M)
    if project.as_posix().lower() not in status.replace("\\", "/").lower() or pid is None:
        raise RuntimeError("The selected Editor must be ready: " + status)
    actual_pid = int(pid.group(1))
    if expected_pid is not None and actual_pid != expected_pid:
        raise RuntimeError("The selected Editor changed between discovery and test execution")
    return actual_pid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--mode", choices=("EditMode", "PlayMode"), required=True)
    parser.add_argument("--filter", default="")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not args.project.is_absolute() or not (args.project / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("Select an existing Unity project by absolute path")
    project = args.project.resolve()
    editor_pid = wait_for_editor(project)
    console = subprocess.run(["unity-cli", "console", "--project", project.as_posix(),
                              "--type", "error", "--lines", "500"],
                             capture_output=True, encoding="utf-8")
    if console.returncode != 0 or has_compilation_error(console.stdout + console.stderr):
        raise RuntimeError("Resolve compilation errors before testing; old Connector results are not evidence: "
                           + console.stdout + console.stderr)
    command = ["unity-cli", "test", "--project", project.as_posix(), "--mode", args.mode]
    if args.filter:
        command += ["--filter", args.filter]
    attempts = []
    for attempt in range(2):
        started = time.time()
        with subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding="utf-8") as process:
            transcript, _ = process.communicate(timeout=180)
            return_code, cli_pid = process.returncode, process.pid
        result = result_from_text(transcript)
        rejected = return_code != 0 and is_prestart_discovery_gap(transcript, result)
        attempts.append({"cliPid": cli_pid, "cliReturnCode": return_code, "prestartDiscoveryRejected": rejected})
        if not rejected or attempt != 0:
            break
        # No test started. Rediscover only the same Editor once; never rerun a failed or ambiguous test result.
        wait_for_editor(project, editor_pid)
    if rejected:
        raise RuntimeError("No test started after one same-Editor rediscovery: " + transcript)
    source, run_id = "cli", None
    if result is None:
        # The Connector uses this exact CLI process PID in runId. Never substitute another process/run's result.
        folder = Path.home() / ".unity-cli/status"
        deadline = time.monotonic() + 120
        while result is None and time.monotonic() < deadline:
            candidates = [path for path in folder.glob(f"test-results-{cli_pid}-*.json") if path.stat().st_mtime >= started]
            if len(candidates) > 1:
                raise RuntimeError("Ambiguous results for this CLI process")
            if candidates:
                path = candidates[0]
                result = result_from_text(path.read_text(encoding="utf-8-sig"))
                run_id, source = path.stem.removeprefix("test-results-"), "connector-file"
            else:
                time.sleep(0.5)
    if result is None or result["total"] <= 0:
        raise RuntimeError("No nonzero result for this invocation: " + transcript)
    result["execution"] = {"project": str(project), "mode": args.mode, "filter": args.filter,
                           "source": source, "runId": run_id, "cliPid": cli_pid, "cliReturnCode": return_code,
                           "editorPid": editor_pid, "attempts": attempts}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: result[key] for key in ("total", "passed", "failed", "skipped", "execution")}))
    return 0 if result["failed"] == 0 and result["skipped"] == 0 else 1


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-check"]:
        counts = {"total": 1, "passed": 1, "failed": 0, "skipped": 0}
        assert result_from_text("running\n" + json.dumps(counts)) == counts
        assert result_from_text("Error: Details: " + json.dumps({"success": False, "data": counts})) == counts
        assert result_from_text("No result {invalid") is None
        assert is_prestart_discovery_gap("Error: no Unity instances running\n", None)
        assert not is_prestart_discovery_gap("Error: no Unity instances running", counts)
        assert not is_prestart_discovery_gap("Error: test run failed", None)
        assert has_compilation_error("Input.cs(1,2): error CS0122: inaccessible")
        assert not has_compilation_error("Expected test exception: ArgumentException")
        print("PASS: extraction, prestart-discovery classification, and compiler-error rejection")
    else:
        raise SystemExit(main())
