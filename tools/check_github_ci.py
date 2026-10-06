"""Read GitHub gates for an exact commit using the configured Git credential helper.

Usage: python tools/check_github_ci.py --repository owner/repo --ref COMMIT --output PATH
Does not push, merge, change credentials, or expose credential material.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
from urllib.error import HTTPError
from urllib.request import Request, urlopen


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", args.repository):
        parser.error("Expected owner/repository")
    if not re.fullmatch(r"[0-9a-fA-F]{40}", args.ref):
        parser.error("Expected exact 40-character commit SHA")
    credential = subprocess.run(
        ["git", "credential", "fill"], input="protocol=https\nhost=github.com\n\n",
        text=True, capture_output=True, check=True,
    )
    fields = dict(line.split("=", 1) for line in credential.stdout.splitlines() if "=" in line)
    token = fields.get("password")
    if not token:
        raise RuntimeError("Git credential helper returned no GitHub credential")

    def get(endpoint):
        request = Request("https://api.github.com/repos/" + args.repository + endpoint, headers={
            "Authorization": "Bearer " + token,
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "TPLab-validation",
        })
        try:
            with urlopen(request, timeout=30) as response:
                return json.load(response)
        except HTTPError as error:
            raise RuntimeError(f"GitHub read failed: HTTP {error.code}, endpoint {endpoint}") from None

    commit = get("/commits/" + args.ref)
    branch = get("/branches/main")
    workflows = get("/actions/workflows?per_page=100")
    checks = get("/commits/" + args.ref + "/check-runs?per_page=100")
    statuses = get("/commits/" + args.ref + "/status?per_page=100")
    runs = get("/actions/runs?head_sha=" + args.ref + "&per_page=100")
    report = {
        "repository": args.repository, "inspectedCommit": commit["sha"],
        "remoteMain": branch["commit"]["sha"], "mainProtected": branch["protected"],
        "workflowCount": workflows["total_count"], "checkRunCount": checks["total_count"],
        "commitStatusCount": statuses["total_count"], "workflowRunCount": runs["total_count"],
        "checks": [{k: check.get(k) for k in ("name", "status", "conclusion")} for check in checks["check_runs"]],
        "statuses": [{k: status.get(k) for k in ("context", "state")} for status in statuses["statuses"]],
        "runs": [{k: run.get(k) for k in ("name", "head_sha", "status", "conclusion")} for run in runs["workflow_runs"]],
    }
    report["ciConfigured"] = any(report[key] for key in ("workflowCount", "checkRunCount", "commitStatusCount", "workflowRunCount"))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    # ponytail: only auto-clear an unprotected repo without CI; inspect configured required gates before extending automation.
    if report["mainProtected"] or report["ciConfigured"]:
        raise SystemExit("CI/protection exists: inspect the saved exact-commit report and required gates before integration")
    print("PASS: exact commit readable; main unprotected and CI unconfigured (not CI success)")


if __name__ == "__main__":
    main()
