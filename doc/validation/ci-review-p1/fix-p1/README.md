# 2026-10-12 · PR #1 review correction

Status: implementation and local verification complete; same branch `codex/github-review-p1`, starting HEAD `f6b5fdca61ae72151262e0a50e153bd53c6bc9b0`. User explicitly approved correcting the originally immutable draft and updating/pushing PR #1; main merge, other branches, forced push, branch deletion, settings, App and secrets remain excluded.

The original `../baseline.json` is unchanged. `tools/check_pr_rules.py` and `tools/test_check_pr_rules.py` now intentionally differ from its original draft hashes. [Post-correction hashes](fix-policy.json) record the new state. The remaining five draft files keep the original hashes. The correction is committed separately and identified as a draft correction in the commit/PR description.

Four tests are added with only `contextlib`/`io` imports. Original13 test method source text and all other test-module behavior are preserved. Checker implementation changes are exactly the attachment diff: ASCII report hyphen; leftover old .meta error after rename; pure rename included in changed-file count; UTF-8 stream configuration only in direct CLI entry. Both tool files match the attached source bytes. No external dependency is added.

| Windows local execution, no PYTHONUTF8/PYTHONIOENCODING | Total | Failures | Errors | Skip | Evidence |
|---|---:|---:|---:|---:|---|
| Red, old checker + four new tests |17|2|2|0|[Red](red-windows.txt), [environment/result](red-windows.json)|
| Green, identical test source |17|0|0|0|[Green](green-windows.txt), [result](green-windows.json)|
| Combined existing CI28 + PR-rule17 |45|0|0|0|[combined](combined-windows.txt), [result](combined-windows.json)|

Commands from `tools`: `python -m unittest test_check_pr_rules -v`, then `python -m unittest test_check_pr_rules test_check_github_ci -v`. Python3.14.6/win32, utf8_mode0, stdout/stderr/preferred encoding cp949, both environment settings absent. Raw CP949 `.log` outputs and UTF-8 `.txt` transcriptions are preserved separately. Windows Red has an extra `test_main_exit_codes` encoding error compared with the [reviewer's Linux Red](reviewer/red.txt) (fail2/error1); reviewer files are reference material, not executed local evidence. Fixture-generated error annotations are expected test data.

AGENTS changes only the two supplied paragraphs: 2 added/2 deleted versus starting HEAD. Versus base main, final additions are4/deletions0; every preexisting main rule line is preserved. Paragraph3's task-specific sentence is moved to the parent validation README and PR body. The committed `main...HEAD` diff is verified before push.

The SHA sentence is used for PR-result lookup against the source candidate branch tip, not a requirement that candidate SHA equal current main. It does not waive the existing rule to validate merged changes. GitHub's `pull_request` GITHUB_SHA/default checkout can refer to a synthetic merge commit; the [official event contract](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request) distinguishes that SHA from `github.event.pull_request.head.sha`. A statement that merge commits have no results is limited to lookup behavior of the current P1 PR check, not a promise about all future workflow types. New main changes or conflict resolutions require relevant validation again before eventual integration. No merge is performed in this unit.

Independent read-only final review found no blocker or attachment difference. Runtime SDK/Unity/compiler/Console/EditMode/PlayMode/Player validation is not run because this unit changes Python tooling and operational guidance only. Claude execution/App/token/permission verification and guide S1–S5 remain unexecuted. Final entire PR rules and actual final-HEAD GitHub run are recorded in the PR body after committing/pushing.

The copied reviewer diff retains its context-space lines byte-for-byte. A path-specific evidence attribute disables only end-of-line-space checks for that diff artifact; implementation/docs whitespace checks remain enabled.
