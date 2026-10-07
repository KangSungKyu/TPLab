"""Run one owned scene sample Player and preserve fresh evidence; does not launch an Editor."""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil

from run_core_consumer import read_json, run_process, unity_version


def validate_smoke(result, mode, expected_version, expected_checks, loading_presentation=False):
    if not isinstance(result, dict) or result.get("success") is not True or result.get("error") != "":
        raise ValueError("Sample smoke reported failure or an invalid result")
    if result.get("mode", "").lower() != mode or result.get("unityVersion") != expected_version:
        raise ValueError("Wrong scene mode or Unity version")
    if type(result.get("completedChecks")) is not int or result["completedChecks"] != expected_checks or expected_checks <= 0:
        raise ValueError("Wrong check count")
    observations = result.get("observations")
    if (not isinstance(observations, list) or len(observations) != expected_checks or
            not all(isinstance(item, str) and item.strip() for item in observations)):
        raise ValueError("Missing observations")
    if loading_presentation:
        if expected_checks != 12 or result.get("LoadingFlow") is not True or result.get("LoadingFailureCleanup") is not True:
            raise ValueError("Missing loading presentation checks")
        counters = ("LoadingPreparations", "LoadingReveals", "LoadingProgressReports", "Proceeds", "LoadingReleases", "LoadingTwoCovers")
        if not all(type(result.get(name)) is int and result[name] > 0 for name in counters):
            raise ValueError("Missing loading progress/ownership observations")
        if (result["LoadingPreparations"] != result["LoadingReleases"] or
                result["LoadingTwoCovers"] != result["Proceeds"] or
                result["LoadingReveals"] != result["LoadingPreparations"]):
            raise ValueError("Loading operation ownership counts disagree")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--mode", choices=("additive", "single"), required=True)
    parser.add_argument("--expected-checks", type=int, required=True)
    parser.add_argument("--loading-presentation", action="store_true", help="Opt into automatic loading UI and its 12-check smoke")
    parser.add_argument("--timeout-seconds", type=int, default=180)
    args = parser.parse_args()
    if args.loading_presentation and args.expected_checks != 12:
        parser.error("Loading presentation requires --expected-checks 12")
    project = args.project.resolve()
    if not args.project.is_absolute() or not (project / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("Select this project by absolute path")
    workspace = project / "Temp/GameScenesTrack"
    player, output, evidence = args.player.resolve(), args.output.resolve(), args.evidence.resolve()
    if not player.is_relative_to(workspace) or not player.is_file():
        parser.error("Player must be an existing owned build under Temp/GameScenesTrack")
    if not output.is_relative_to(workspace) or output == workspace or output.exists():
        parser.error("Choose an unused owned output directory under Temp/GameScenesTrack")
    if not evidence.is_relative_to(project / "doc/validation") or args.expected_checks <= 0 or args.timeout_seconds <= 0:
        parser.error("Choose local validation evidence and positive check count/timeout")
    output.mkdir(parents=True)
    evidence.mkdir(parents=True, exist_ok=True)
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + str(os.getpid())
    report_path = evidence / ("scene-player-" + args.mode + "-" + run_id + ".json")
    log = output / "player.log"
    report = {"success": False, "project": str(project), "player": str(player), "mode": args.mode,
              "expectedChecks": args.expected_checks, "expectedUnityVersion": unity_version(project),
              "loadingPresentation": args.loading_presentation,
              "process": {}, "result": {}, "error": ""}
    try:
        command = [str(player), "-batchmode", "-nographics", "-logFile", str(log),
                   "-tplab-scene-smoke", "-tplab-scene-result", str(output / "result.json")]
        if args.loading_presentation:
            command.append("-tplab-loading-presentation")
        process = run_process(command,
                              player.parent, os.environ.copy(), log, output / "stdout.log", args.timeout_seconds)
        report["process"] = process
        result = read_json(output / "result.json", process["startedUnix"])
        report["result"] = result
        if process["returnCode"] != 0 or process["timedOut"] or not process["processLogFresh"]:
            raise RuntimeError("Player process gate failed")
        validate_smoke(result, args.mode, report["expectedUnityVersion"], args.expected_checks, args.loading_presentation)
        report["success"] = True
    except Exception as exception:
        report["error"] = str(exception)
    if log.is_file():
        preserved_log = evidence / ("scene-player-" + args.mode + "-" + run_id + ".log")
        shutil.copyfile(log, preserved_log)
        report["preservedLog"] = str(preserved_log)
    with report_path.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    print(json.dumps({"success": report["success"], "pid": report["process"].get("pid"),
                      "evidence": str(report_path), "error": report["error"]}))
    return 0 if report["success"] else 1


if __name__ == "__main__":
    import sys
    if sys.argv[1:] == ["--self-check"]:
        good = {"success": True, "error": "", "mode": "Additive", "unityVersion": "test",
                "completedChecks": 1, "observations": ["actual check"]}
        validate_smoke(good, "additive", "test", 1)
        for change in ({"success": False}, {"completedChecks": 0}, {"observations": []}, {"observations": [""]},
                       {"observations": "a"}, {"completedChecks": True},
                       {"mode": "Single"}, {"unityVersion": "wrong"}, {"error": "failure"}):
            try:
                validate_smoke(dict(good, **change), "additive", "test", 1)
            except ValueError:
                pass
            else:
                raise AssertionError("Invalid smoke was accepted")
        loading = dict(good, completedChecks=12, observations=["actual check"] * 12,
                       LoadingFlow=True, LoadingFailureCleanup=True, LoadingPreparations=2, LoadingReveals=2,
                       LoadingProgressReports=5, Proceeds=1, LoadingReleases=2, LoadingTwoCovers=1)
        validate_smoke(loading, "additive", "test", 12, True)
        for change in ({"LoadingFlow": False}, {"LoadingFailureCleanup": False}, {"LoadingProgressReports": 0},
                       {"Proceeds": True}, {"LoadingReleases": 1}, {"LoadingTwoCovers": 2}, {"LoadingReveals": 1}):
            try:
                validate_smoke(dict(loading, **change), "additive", "test", 12, True)
            except ValueError:
                pass
            else:
                raise AssertionError("Invalid loading smoke was accepted")
        print("PASS: nonzero exact scene smoke count, mode, version, observations and failure gates")
    else:
        raise SystemExit(main())
