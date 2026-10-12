"""Read GitHub gates for an exact commit using the configured Git credential helper.

Usage: python tools/check_github_ci.py --repository owner/repo --ref COMMIT --output PATH
Does not push, merge, change credentials, or expose credential material.
Workflow presence alone is not a failure. Only complete, exact-SHA observed results
with success clear a configured gate. No CI is unconfigured, not CI success.
Every returned workflow run is considered; an older failed run still blocks after
another run succeeds. No required-check catalog or optional-check exceptions exist.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
from urllib.error import HTTPError
from urllib.request import Request, urlopen


SHA_PATTERN = r"[0-9a-fA-F]{40}"


def read_collection(get, endpoint, key):
    """Collect every declared page, retaining raw rows and incomplete-query evidence."""
    rows, pages, errors = [], [], []
    total = None
    page = 1
    ids = set()
    while True:
        separator = "&" if "?" in endpoint else "?"
        page_endpoint = endpoint + separator + f"per_page=100&page={page}"
        try:
            payload = get(page_endpoint)
        except Exception as error:
            errors.append(f"{key}: page {page} query failed ({type(error).__name__})")
            break
        if not isinstance(payload, dict):
            errors.append(f"{key}: page {page} response is not an object")
            break
        declared = payload.get("total_count")
        batch = payload.get(key)
        pages.append({"page": page, "totalCount": declared, "sha": payload.get("sha")})
        if type(declared) is not int or declared < 0 or not isinstance(batch, list):
            errors.append(f"{key}: page {page} count/list metadata is missing or invalid")
            break
        if total is None:
            total = declared
        elif declared != total:
            errors.append(f"{key}: total_count changed while paging")
        rows.extend(batch)
        for row in batch:
            if not isinstance(row, dict) or type(row.get("id")) is not int:
                errors.append(f"{key}: record identity is missing or invalid")
                continue
            if row["id"] in ids:
                errors.append(f"{key}: duplicate record identity {row['id']}")
            ids.add(row["id"])
        if len(batch) > 100 or len(rows) > total:
            errors.append(f"{key}: received rows exceed declared page/count limits")
        if errors or len(rows) == total:
            break
        if not batch:
            errors.append(f"{key}: page {page} ended before total_count was received")
            break
        page += 1
    return {"items": rows, "totalCount": total, "receivedCount": len(rows),
            "pages": pages, "complete": not errors and total == len(rows), "errors": errors}


def evaluate_gate(report):
    """Judge one saved observation; unknown/incomplete results fail closed."""
    reasons = list(report["queryIssues"])
    target = report["targetSha"]
    if str(report.get("inspectedCommit", "")).lower() != target:
        reasons.append("Returned commit SHA does not match the requested target")
    if report["mainProtected"] is True:
        reasons.append("main is protected; automatic integration is blocked")
    elif report["mainProtected"] is not False:
        reasons.append("main protection could not be established")
    if not isinstance(report["remoteMain"], str) or not re.fullmatch(SHA_PATTERN, report["remoteMain"]):
        reasons.append("Remote main identity is missing or invalid")
    for name, collection in report["collections"].items():
        reasons.extend(collection["errors"])
        if not collection["complete"] and not collection["errors"]:
            reasons.append(f"{name}: collection is incomplete")
    for page in report["collections"]["statuses"]["pages"]:
        if str(page["sha"]).lower() != target:
            reasons.append("Combined commit-status SHA does not match the target")
    for name in ("checks", "runs"):
        for row in report[name]:
            if not isinstance(row, dict):
                continue
            identity = row.get("id", "unknown")
            if str(row.get("head_sha", "")).lower() != target:
                reasons.append(f"{name} {identity}: head SHA does not match the target")
            if row.get("status") != "completed" or row.get("conclusion") != "success":
                reasons.append(f"{name} {identity}: result is not completed/success")
    for row in report["statuses"]:
        if isinstance(row, dict) and row.get("state") != "success":
            reasons.append(f"status {row.get('id', 'unknown')}: state is not success")
    observed = sum(len(report[name]) for name in ("checks", "statuses", "runs"))
    if report["ciConfigured"] and observed == 0:
        reasons.append("CI is configured but the target has no checks, statuses or runs")
    if reasons:
        return "blocked", reasons
    return ("passed" if report["ciConfigured"] else "unconfigured"), []


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", args.repository):
        parser.error("Expected owner/repository")
    if not re.fullmatch(SHA_PATTERN, args.ref):
        parser.error("Expected exact 40-character commit SHA")
    target = args.ref.lower()
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

    query_issues = []

    def identity(endpoint, name):
        try:
            value = get(endpoint)
            if not isinstance(value, dict):
                raise ValueError("Identity response is not an object")
            return value
        except Exception as error:
            query_issues.append(f"{name}: query failed ({type(error).__name__})")
            return {}

    commit = identity("/commits/" + target, "commit")
    branch = identity("/branches/main", "main")
    collections = {
        "workflows": read_collection(get, "/actions/workflows", "workflows"),
        "checks": read_collection(get, "/commits/" + target + "/check-runs", "check_runs"),
        "statuses": read_collection(get, "/commits/" + target + "/status", "statuses"),
        "runs": read_collection(get, "/actions/runs?head_sha=" + target, "workflow_runs"),
    }
    main_commit = branch.get("commit")
    report = {
        "repository": args.repository, "requestedCommit": args.ref, "targetSha": target,
        "inspectedCommit": commit.get("sha"),
        "remoteMain": main_commit.get("sha") if isinstance(main_commit, dict) else None,
        "mainProtected": branch.get("protected"),
        "workflowCount": collections["workflows"]["totalCount"],
        "checkRunCount": collections["checks"]["totalCount"],
        "commitStatusCount": collections["statuses"]["totalCount"],
        "workflowRunCount": collections["runs"]["totalCount"],
        "queryIssues": query_issues,
        "collections": {name: {key: value for key, value in collection.items() if key != "items"}
                        for name, collection in collections.items()},
        **{name: collection["items"] for name, collection in collections.items()},
    }
    report["ciConfigured"] = any(collection["totalCount"] or collection["items"] for collection in collections.values())
    report["gateStatus"], report["blockingReasons"] = evaluate_gate(report)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if report["gateStatus"] == "blocked":
        raise SystemExit("BLOCKED: inspect the saved exact-commit results, query completeness and main protection")
    if report["gateStatus"] == "unconfigured":
        print("UNCONFIGURED: exact commit readable; main unprotected; no CI observed (not CI success)")
    else:
        print("PASS: exact commit observed results completed successfully; main unprotected")


if __name__ == "__main__":
    main()
