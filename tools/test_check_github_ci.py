"""Offline CLI contract tests; Git credentials and every HTTP read are mocked."""
import copy
from contextlib import redirect_stdout
import io
import json
from pathlib import Path
from types import SimpleNamespace
import tempfile
import unittest
from unittest.mock import patch
from urllib.parse import parse_qs, urlparse

import check_github_ci as gate


SHA = "a" * 40
OTHER_SHA = "b" * 40
SECRET = "offline-credential-do-not-report"


def record(kind, identity=1):
    if kind == "workflows":
        return {"id": identity, "name": "rules", "path": ".github/workflows/rules.yml", "state": "active"}
    if kind == "statuses":
        return {"id": identity, "context": "rules", "state": "success", "description": "observed"}
    return {"id": identity, "name": "rules", "head_sha": SHA, "status": "completed", "conclusion": "success"}


def fixture():
    return {
        "commit": {"sha": SHA},
        "branch": {"commit": {"sha": OTHER_SHA}, "protected": False},
        "workflows": [record("workflows")],
        "checks": [record("checks")],
        "statuses": [],
        "runs": [record("runs")],
    }


class GitHubGateTests(unittest.TestCase):
    def invoke(self, data=None, page_changes=None, read_errors=(), ref=SHA):
        data = copy.deepcopy(data if data is not None else fixture())
        requested = []
        list_keys = {"workflows": "workflows", "checks": "check_runs", "statuses": "statuses", "runs": "workflow_runs"}

        def read(request, timeout):
            self.assertEqual(timeout, 30)
            parsed = urlparse(request.full_url)
            path = parsed.path.removeprefix("/repos/owner/repo")
            if path == "/commits/" + ref.lower():
                result = data["commit"]
                key = "commit"
            elif path == "/branches/main":
                result = data["branch"]
                key = "branch"
            else:
                key = {"check-runs": "checks", "status": "statuses", "workflows": "workflows", "runs": "runs"}[path.rsplit("/", 1)[-1]]
                page = int(parse_qs(parsed.query).get("page", ["1"])[0])
                requested.append((key, page))
                if key in read_errors:
                    raise RuntimeError("offline read unavailable")
                rows = data[key]
                result = {"total_count": len(rows), list_keys[key]: rows[(page - 1) * 100:page * 100]}
                if key == "statuses":
                    result["sha"] = data.get("status_sha", SHA)
                    result["state"] = "success"
                if page_changes:
                    result = page_changes(key, page, result)
            if key in read_errors:
                raise RuntimeError("offline read unavailable")
            return io.StringIO(json.dumps(result))

        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "report.json"
            argv = ["check_github_ci.py", "--repository", "owner/repo", "--ref", ref, "--output", str(output)]
            stdout = io.StringIO()
            exit_code = 0
            with patch.object(gate.subprocess, "run", return_value=SimpleNamespace(stdout="password=" + SECRET + "\n")) as credentials, patch.object(gate, "urlopen", side_effect=read), patch("sys.argv", argv), redirect_stdout(stdout):
                try:
                    gate.main()
                except SystemExit as error:
                    exit_code = error.code
            credentials.assert_called_once()
            content = output.read_text(encoding="utf-8")
            self.assertNotIn(SECRET, content + stdout.getvalue() + str(exit_code))
            return json.loads(content), exit_code, stdout.getvalue(), requested

    def assert_blocked(self, data=None, **kwargs):
        report, exit_code, _, requested = self.invoke(data, **kwargs)
        self.assertNotEqual(exit_code, 0)
        self.assertEqual(report.get("gateStatus"), "blocked")
        self.assertTrue(report.get("blockingReasons"))
        return report, requested

    def test_successful_exact_sha_checks_do_not_stop_for_workflow_presence(self):
        report, exit_code, _, _ = self.invoke()
        self.assertEqual(exit_code, 0)
        self.assertEqual(report.get("gateStatus"), "passed")

    def test_no_ci_is_unconfigured_and_not_ci_success(self):
        data = fixture()
        for key in ("workflows", "checks", "statuses", "runs"):
            data[key] = []
        report, exit_code, stdout, _ = self.invoke(data)
        self.assertEqual(exit_code, 0)
        self.assertEqual(report.get("gateStatus"), "unconfigured")
        self.assertIn("not CI success", stdout)

    def test_configured_ci_without_target_results_is_blocked(self):
        data = fixture()
        data["checks"] = data["runs"] = []
        self.assert_blocked(data)

    def test_protected_main_is_blocked_even_with_success(self):
        data = fixture()
        data["branch"]["protected"] = True
        self.assert_blocked(data)

    def test_failure_check_is_blocked(self):
        data = fixture()
        data["checks"][0]["conclusion"] = "failure"
        self.assert_blocked(data)

    def test_in_progress_check_is_blocked(self):
        data = fixture()
        data["checks"][0].update(status="in_progress", conclusion=None)
        self.assert_blocked(data)

    def test_cancelled_run_is_blocked(self):
        data = fixture()
        data["runs"][0]["conclusion"] = "cancelled"
        self.assert_blocked(data)

    def test_timed_out_run_is_blocked(self):
        data = fixture()
        data["runs"][0]["conclusion"] = "timed_out"
        self.assert_blocked(data)

    def test_skipped_check_is_blocked(self):
        data = fixture()
        data["checks"][0]["conclusion"] = "skipped"
        self.assert_blocked(data)

    def test_neutral_check_is_blocked(self):
        data = fixture()
        data["checks"][0]["conclusion"] = "neutral"
        self.assert_blocked(data)

    def test_unknown_result_is_blocked(self):
        data = fixture()
        data["checks"][0]["conclusion"] = "future-result"
        self.assert_blocked(data)

    def test_completed_without_conclusion_is_blocked(self):
        data = fixture()
        data["checks"][0]["conclusion"] = None
        self.assert_blocked(data)

    def test_commit_status_error_is_blocked(self):
        data = fixture()
        data["statuses"] = [record("statuses")]
        data["statuses"][0]["state"] = "error"
        self.assert_blocked(data)

    def test_commit_status_pending_is_blocked(self):
        data = fixture()
        data["statuses"] = [record("statuses")]
        data["statuses"][0]["state"] = "pending"
        self.assert_blocked(data)

    def test_commit_sha_mismatch_is_blocked(self):
        data = fixture()
        data["commit"]["sha"] = OTHER_SHA
        self.assert_blocked(data)

    def test_check_sha_mismatch_is_blocked(self):
        data = fixture()
        data["checks"][0]["head_sha"] = OTHER_SHA
        self.assert_blocked(data)

    def test_run_sha_mismatch_is_blocked(self):
        data = fixture()
        data["runs"][0]["head_sha"] = OTHER_SHA
        self.assert_blocked(data)

    def test_combined_status_sha_mismatch_is_blocked(self):
        data = fixture()
        data["statuses"] = [record("statuses")]
        data["status_sha"] = OTHER_SHA
        self.assert_blocked(data)

    def test_raw_workflow_check_status_and_run_identities_are_preserved(self):
        data = fixture()
        data["statuses"] = [record("statuses")]
        report, _, _, _ = self.invoke(data)
        for key in ("workflows", "checks", "statuses", "runs"):
            self.assertEqual(report.get(key), data[key])

    def test_all_four_collections_are_paginated_without_dropping_records(self):
        data = fixture()
        for key in ("workflows", "checks", "statuses", "runs"):
            data[key] = [record(key, index) for index in range(101)]
        report, exit_code, _, requested = self.invoke(data)
        self.assertEqual(exit_code, 0)
        for key in ("workflows", "checks", "statuses", "runs"):
            self.assertEqual(report.get(key), data[key])
            self.assertIn((key, 2), requested)

    def test_failure_beyond_first_hundred_checks_is_blocked(self):
        data = fixture()
        data["checks"] = [record("checks", index) for index in range(101)]
        data["checks"][-1]["conclusion"] = "failure"
        report, requested = self.assert_blocked(data)
        self.assertEqual(len(report["checks"]), 101)
        self.assertIn(("checks", 2), requested)

    def test_truncated_second_page_is_blocked(self):
        data = fixture()
        data["checks"] = [record("checks", index) for index in range(101)]
        def truncate(key, page, result):
            if key == "checks" and page == 2:
                result["check_runs"] = []
            return result
        self.assert_blocked(data, page_changes=truncate)

    def test_duplicate_page_does_not_hide_missing_record(self):
        data = fixture()
        data["checks"] = [record("checks", index) for index in range(101)]
        def duplicate(key, page, result):
            if key == "checks" and page == 2:
                result["check_runs"] = [data["checks"][0]]
            return result
        self.assert_blocked(data, page_changes=duplicate)

    def test_changing_total_count_is_incomplete_and_blocked(self):
        data = fixture()
        data["checks"] = [record("checks", index) for index in range(101)]
        def change_count(key, page, result):
            if key == "checks" and page == 2:
                result["total_count"] = 102
            return result
        self.assert_blocked(data, page_changes=change_count)

    def test_collection_read_error_is_reported_and_blocked(self):
        self.assert_blocked(read_errors=("checks",))

    def test_missing_collection_field_is_incomplete_and_blocked(self):
        def omit(key, page, result):
            if key == "workflows":
                result.pop("workflows")
            return result
        self.assert_blocked(page_changes=omit)

    def test_old_failure_and_new_success_remain_blocked_conservatively(self):
        data = fixture()
        data["runs"] = [record("runs", 1), record("runs", 2)]
        data["runs"][0].update(conclusion="failure", run_attempt=1)
        data["runs"][1].update(run_attempt=2)
        self.assert_blocked(data)

    def test_unknown_main_protection_does_not_auto_clear(self):
        data = fixture()
        data["branch"]["protected"] = None
        self.assert_blocked(data)


if __name__ == "__main__":
    unittest.main()
