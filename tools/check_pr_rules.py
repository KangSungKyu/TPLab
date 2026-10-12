"""Check a pull request diff against TPLab review rules (read-only, no Unity needed).

Usage: python tools/check_pr_rules.py --base BASE_SHA --head HEAD_SHA [--repo PATH] [--warn-only]

Reads only `git diff` and `git cat-file`. Does not push, merge, change files, or use credentials.
Results are heuristics over the diff, not Unity test results: a clean report is not a test pass.
Exit code 1 when an error-level finding exists (unless --warn-only), otherwise 0.
"""
import argparse
import os
import re
import subprocess
import sys

TEST_PATH = re.compile(r"(Tests|TestFixtures)")
PROTECTED_PATHS = (".github/", "CLAUDE.md", "AGENTS.md", "tools/check_pr_rules.py", "tools/check_github_ci.py")
META_ROOTS = ("Assets", "upm")

IGNORE_RE = re.compile(r"\[\s*(?:[A-Za-z]+\s*,\s*)*(?:Ignore|Explicit)\b|\bAssert\.(?:Ignore|Inconclusive)\b")
TEST_ATTR_RE = re.compile(r"^\s*\[.*[\[,]?\s*(?:Test|UnityTest)\s*[\],(]|^\s*\[\s*TestCase\s*\(")
ASSERT_RE = re.compile(r"\b(?:Assert|LogAssert|CollectionAssert|StringAssert)\.\w+")
WAIT_RE = re.compile(r"\bThread\.Sleep\s*\(|\bTask\.Delay\s*\(|\bUniTask\.Delay\s*\(|\bUniTask\.DelayFrame\s*\(")
LINQ_USING_RE = re.compile(r"^\s*using\s+System\.Linq\s*;")


def git(repo, *args):
    result = subprocess.run(["git", "-c", "core.quotepath=off", *args], cwd=repo, capture_output=True,
                            text=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise RuntimeError("git " + " ".join(args) + " failed: " + result.stderr.strip())
    return result.stdout


def exists_at(repo, rev, path):
    result = subprocess.run(["git", "cat-file", "-e", rev + ":" + path], cwd=repo, capture_output=True)
    return result.returncode == 0


def parse_patch(text):
    """Return {path: {"added": [...], "removed": [...]}} from a --unified=0 --no-prefix patch."""
    files, old, current, in_hunk = {}, None, None, False
    for line in text.splitlines():
        if line.startswith("diff --git "):
            in_hunk, current, old = False, None, None
        elif line.startswith("@@"):
            in_hunk = True
        elif not in_hunk and line.startswith("--- "):
            old = None if line[4:] == "/dev/null" else line[4:].rstrip("\t")
        elif not in_hunk and line.startswith("+++ "):
            new = None if line[4:] == "/dev/null" else line[4:].rstrip("\t")
            current = files.setdefault(new or old, {"added": [], "removed": []})
        elif in_hunk and current is not None and line.startswith("+"):
            current["added"].append(line[1:])
        elif in_hunk and current is not None and line.startswith("-"):
            current["removed"].append(line[1:])
    return files


def parse_name_status(text):
    entries = []
    for line in text.splitlines():
        parts = line.split("\t")
        if len(parts) >= 2:
            entries.append((parts[0][0], parts[-1], parts[1] if parts[0][0] in "RC" else None))
    return entries


def is_test(path):
    return path.endswith(".cs") and TEST_PATH.search(path) is not None


def count_tests(lines):
    return sum(1 for line in lines if TEST_ATTR_RE.search(line))


def count_asserts(lines):
    return sum(len(ASSERT_RE.findall(line)) for line in lines)


def skipped_for_meta(path):
    parts = path.split("/")
    return path.endswith(".meta") or any(part.endswith("~") or part.startswith(".") for part in parts)


def active_meta_roots(repo, base):
    roots = []
    for root in META_ROOTS:
        try:
            listing = git(repo, "ls-tree", "-r", "--name-only", base, "--", root)
        except RuntimeError:
            continue
        if any(name.endswith(".meta") for name in listing.splitlines()):
            roots.append(root)
    return roots


def analyze(repo, base, head):
    errors, warnings = [], []
    patch = parse_patch(git(repo, "diff", "--unified=0", "--no-color", "--no-ext-diff", "--no-prefix", "-M", base + "..." + head))
    statuses = parse_name_status(git(repo, "diff", "--name-status", "-M", base + "..." + head))

    test_added = test_removed = assert_added = assert_removed = 0
    for path, change in patch.items():
        if is_test(path):
            for line in change["added"]:
                if IGNORE_RE.search(line):
                    errors.append((path, "테스트를 끄는 구문이 추가됨: " + line.strip()))
                elif WAIT_RE.search(line):
                    warnings.append((path, "시간 대기가 추가됨(게이트로 제어할 수 있는지 확인, 백로그 예외 항목 제외): " + line.strip()))
            test_added += count_tests(change["added"])
            test_removed += count_tests(change["removed"])
            assert_added += count_asserts(change["added"])
            assert_removed += count_asserts(change["removed"])
        elif path.endswith(".cs") and any(LINQ_USING_RE.search(line) for line in change["added"]):
            warnings.append((path, "using System.Linq 추가됨. P0 경로(폴링·프레임 단위)면 GC 가이드라인 위반인지 확인"))
    if test_added - test_removed < 0:
        errors.append(("(테스트 전체)", "테스트 수 순감소(근사치): 추가 %d, 제거 %d. 의도한 삭제인지 확인" % (test_added, test_removed)))
    if assert_added - assert_removed < 0:
        warnings.append(("(테스트 전체)", "단언 수 순감소(근사치): 추가 %d, 제거 %d. 기대값 약화가 없는지 확인" % (assert_added, assert_removed)))

    roots = active_meta_roots(repo, base)
    for status, path, _old in statuses:
        if status in "ACR" and path.startswith(tuple(r + "/" for r in roots)) and not skipped_for_meta(path):
            if not exists_at(repo, head, path + ".meta"):
                errors.append((path, ".meta 파일이 없음(Unity 패키지 소비 시 참조가 깨질 수 있음)"))
        if status == "D" and path.startswith(tuple(r + "/" for r in roots)) and not skipped_for_meta(path):
            if exists_at(repo, head, path + ".meta"):
                errors.append((path, "파일은 삭제됐는데 .meta가 남아 있음"))
    for _status, path, _old in statuses:
        if path.startswith(PROTECTED_PATHS):
            warnings.append((path, "검사·리뷰·작업 규칙 파일이 변경됨. 사용자가 직접 확인"))
    return errors, warnings, len(patch)


def render(errors, warnings, files_changed):
    lines = ["## PR 규칙 검사", "",
             "변경 파일 %d개를 diff 기준으로 검사했습니다. 이 결과는 Unity 테스트 실행 결과가 아닙니다." % files_changed, ""]
    for title, items in (("오류", errors), ("경고", warnings)):
        lines.append("### %s (%d)" % (title, len(items)))
        lines += ["- `%s` — %s" % item for item in items] if items else ["- 없음"]
        lines.append("")
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True)
    parser.add_argument("--head", required=True)
    parser.add_argument("--repo", default=".")
    parser.add_argument("--warn-only", action="store_true")
    args = parser.parse_args(argv)
    for name in (args.base, args.head):
        if not re.fullmatch(r"[0-9A-Za-z_./-]+", name):
            parser.error("Unexpected revision: " + name)
    errors, warnings, files_changed = analyze(args.repo, args.base, args.head)
    report = render(errors, warnings, files_changed)
    print(report)
    for path, message in errors:
        print("::error title=PR 규칙 검사::%s: %s" % (path, message))
    for path, message in warnings:
        print("::warning title=PR 규칙 검사::%s: %s" % (path, message))
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(report + "\n")
    return 1 if errors and not args.warn_only else 0


if __name__ == "__main__":
    sys.exit(main())
