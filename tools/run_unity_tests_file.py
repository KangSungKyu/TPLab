"""Run EditMode tests through the selected Editor and persist results from a background continuation."""

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import uuid

from run_unity_tests import has_compilation_error, result_from_text, wait_for_editor


def csharp_literal(value):
    """Encode text as a C# string literal without passing it through a shell."""
    return json.dumps(value, ensure_ascii=True)


def project_identity(path):
    return os.path.normcase(os.path.normpath(os.path.abspath(os.fspath(path))))


def output_inside_project(project, output):
    project = Path(project)
    output = Path(output)
    if not project.is_absolute() or not output.is_absolute():
        raise ValueError("Project and output paths must be absolute")
    project = project.resolve()
    output = output.resolve()
    try:
        output.relative_to(project)
    except ValueError as error:
        raise ValueError("The native output must be inside the selected project") from error
    if output.suffixes[-2:] != [".native", ".json"]:
        raise ValueError("Output filename must end with .native.json")
    return project, output


def json_objects(text):
    decoder = json.JSONDecoder()
    for index, character in enumerate(text):
        if character != "{":
            continue
        try:
            value, _ = decoder.raw_decode(text[index:])
        except ValueError:
            continue
        if isinstance(value, dict):
            yield value


def exec_started_response(text, expected_run_id):
    pending = list(json_objects(text))
    while pending:
        value = pending.pop()
        if value.get("started") is True and value.get("runId") == expected_run_id:
            return value
        nested = value.get("data")
        if isinstance(nested, dict):
            pending.append(nested)
        elif isinstance(nested, str):
            pending.extend(json_objects(nested))
    return None


def build_csharp_body(run_id, project, filter_value, output):
    run_literal = csharp_literal(run_id)
    project_literal = csharp_literal(str(project))
    filter_literal = csharp_literal(filter_value)
    output_literal = csharp_literal(str(output))
    parameter_json = csharp_literal(json.dumps(
        {"mode": "EditMode", "filter": filter_value, "runId": run_id},
        ensure_ascii=True,
        separators=(",", ":"),
    ))
    return f'''var runId = {run_literal};
var project = {project_literal};
var filter = {filter_literal};
var outputPath = {output_literal};
var expectedAssetsPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(project, "Assets"));
var actualAssetsPath = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath);
if (!string.Equals(actualAssetsPath, expectedAssetsPath, System.StringComparison.OrdinalIgnoreCase))
    throw new System.InvalidOperationException("The connected Editor project path does not match the requested project.");
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
    throw new System.InvalidOperationException("The connected Editor is compiling or updating.");
var activeMethod = typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi).GetMethod(
    "IsRunActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
if (activeMethod == null) throw new System.InvalidOperationException("TestRunnerApi.IsRunActive was not found.");
if ((bool)activeMethod.Invoke(null, null)) throw new System.InvalidOperationException("The selected Editor already has an active TestRunner run.");
var parameters = Newtonsoft.Json.Linq.JObject.Parse({parameter_json});
var testTask = UnityCliConnector.TestRunner.RunTests.HandleCommand(parameters);
testTask.ContinueWith(completed =>
{{
    object response = null;
    string error = null;
    try {{ response = completed.GetAwaiter().GetResult(); }}
    catch (System.Exception exception) {{ error = exception.ToString(); }}
    Newtonsoft.Json.Linq.JToken summary = null;
    if (response != null)
    {{
        try
        {{
            var dataField = response.GetType().GetField("data");
            var data = dataField == null ? null : dataField.GetValue(response);
            if (data != null) summary = Newtonsoft.Json.Linq.JToken.FromObject(data);
        }}
        catch (System.Exception exception) {{ error = error ?? exception.ToString(); }}
    }}
    var record = new Newtonsoft.Json.Linq.JObject
    {{
        ["runId"] = runId,
        ["project"] = project,
        ["filter"] = filter,
        ["mode"] = "EditMode",
        ["summary"] = summary,
        ["error"] = error,
        ["completedUtc"] = System.DateTime.UtcNow.ToString("O")
    }};
    var temporaryPath = outputPath + "." + runId + ".tmp";
    System.IO.File.WriteAllText(temporaryPath, record.ToString(Newtonsoft.Json.Formatting.Indented));
    System.IO.File.Move(temporaryPath, outputPath);
}}, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.None,
    System.Threading.Tasks.TaskScheduler.Default);
return Newtonsoft.Json.JsonConvert.SerializeObject(new {{ started = true, runId, project, filter, mode = "EditMode" }});'''


def validate_native_result(record, run_id, project, filter_value):
    if record.get("runId") != run_id or project_identity(record.get("project", "")) != project_identity(project):
        raise RuntimeError("Native result runId/project does not match this invocation")
    if record.get("filter") != filter_value or record.get("mode") != "EditMode":
        raise RuntimeError("Native result filter/mode does not match this invocation")
    if record.get("error"):
        raise RuntimeError("Native TestRunner task failed: " + record["error"])
    native_summary = record.get("summary")
    summary = result_from_text(json.dumps(native_summary)) if isinstance(native_summary, dict) else None
    if summary is None or not all(
        isinstance(summary.get(key), int) for key in ("total", "passed", "failed", "skipped")
    ):
        raise RuntimeError("Native result has no valid TestRunner summary")
    if summary["total"] <= 0 or summary["total"] != sum(summary[key] for key in ("passed", "failed", "skipped")):
        raise RuntimeError("Native result has no nonzero, internally consistent test count")
    return summary


def self_check():
    hostile = 'filter "x"; System.IO.File.Delete("no");\\line\nnext'
    literal = csharp_literal(hostile)
    assert literal.startswith('"') and literal.endswith('"')
    assert json.loads(literal) == hostile
    sample = {"runId": "r1", "project": str(Path("C:/Project").resolve()), "filter": hostile, "mode": "EditMode",
              "summary": {"total": 1, "passed": 1, "failed": 0, "skipped": 0}, "error": None}
    assert validate_native_result(sample, "r1", Path("C:/Project"), hostile)["total"] == 1
    assert exec_started_response('{"success":true,"data":"{\\"started\\":true,\\"runId\\":\\"r1\\"}"}', "r1")
    root, output = output_inside_project(Path("C:/Project"), Path("C:/Project/Temp/check.native.json"))
    assert output.is_relative_to(root)
    for unsafe in (Path("C:/Elsewhere/result.native.json"), Path("C:/Project/check.json")):
        try:
            output_inside_project(Path("C:/Project"), unsafe)
        except ValueError:
            pass
        else:
            raise AssertionError("Unsafe output path was accepted")
    try:
        validate_native_result({**sample, "runId": "other"}, "r1", Path("C:/Project"), hostile)
    except RuntimeError:
        pass
    else:
        raise AssertionError("Mismatched run result was accepted")
    assert result_from_text('{"total":1,"passed":1,"failed":0,"skipped":0}') is not None
    print("PASS: C# literal, result extraction/identity, and output path constraints")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--editor-pid", type=int, required=True)
    parser.add_argument("--filter", default="")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--timeout", type=float, default=300)
    args = parser.parse_args()

    if not args.project.is_absolute() or not (args.project / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("Select an existing Unity project by absolute path")
    project, output = output_inside_project(args.project, args.output)
    if output.exists():
        parser.error("Refusing to overwrite an existing native result file")
    editor_pid = wait_for_editor(project, args.editor_pid)
    console = subprocess.run(
        ["unity-cli", "console", "--project", project.as_posix(), "--type", "error", "--lines", "500"],
        capture_output=True, encoding="utf-8", shell=False,
    )
    console_text = console.stdout + console.stderr
    if console.returncode != 0 or has_compilation_error(console_text):
        raise RuntimeError("Resolve compile errors before testing: " + console_text)
    wait_for_editor(project, editor_pid)

    run_id = uuid.uuid4().hex
    output.parent.mkdir(parents=True, exist_ok=True)
    body = build_csharp_body(run_id, project, args.filter, output)
    started = subprocess.run(
        ["unity-cli", "exec", body, "--project", project.as_posix(), "--timeout", "10000", "--allow-async"],
        capture_output=True, encoding="utf-8", shell=False,
    )
    if started.returncode != 0:
        raise RuntimeError("Unity exec did not start the native EditMode run: " + started.stdout + started.stderr)
    start_record = exec_started_response(started.stdout + started.stderr, run_id)
    if start_record is None or project_identity(start_record.get("project", "")) != project_identity(project) \
            or start_record.get("filter") != args.filter:
        raise RuntimeError("Unity exec returned no matching started record: " + started.stdout + started.stderr)

    print(json.dumps({"started": True, "runId": run_id, "editorPid": editor_pid, "project": str(project), "filter": args.filter, "mode": "EditMode", "nativeOutput": str(output)}, ensure_ascii=False), flush=True)
    deadline = time.monotonic() + args.timeout
    while time.monotonic() < deadline:
        if output.is_file():
            record = json.loads(output.read_text(encoding="utf-8"))
            summary = validate_native_result(record, run_id, project, args.filter)
            print(json.dumps({"runId": run_id, "project": str(project), "filter": args.filter,
                              "mode": "EditMode", "summary": summary}, ensure_ascii=False))
            return 0 if summary["failed"] == 0 and summary["skipped"] == 0 else 1
        time.sleep(0.25)
    raise TimeoutError("Native result file was not written before timeout; no test retry was attempted")


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-check"]:
        self_check()
    else:
        raise SystemExit(main())
