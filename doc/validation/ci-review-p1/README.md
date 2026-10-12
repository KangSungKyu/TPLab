# 2026-10-12 · GitHub review automation P1

User authorized file application and a PR only. Base main `0641fb57e4d6fae03fe17b88801c6762d78de84f`, branch `codex/github-review-p1`. Main merge, other branch pushes/deletion, force push, repository settings, app installation and real secrets are outside this unit.

The seven supplied draft files are copied byte-for-byte in commit `b194ebbf090108f4fae1ea0f23b2e64d1ee1d91d`. [Baseline](baseline.json) records their SHA256 and the six preserved user changes. AGENTS gains documentation and CI interpretation paragraphs; all preexisting rule lines remain unchanged. The attachment's CODEX_APPLY_PROMPT.md is not installed in the repository.

| Verification | Executed | Failures | Errors | Skip | Evidence |
|---|---:|---:|---:|---:|---|
| Draft PR-rule tests, initial Windows CP949 run |13|0|1|0|[initial](draft-tests.txt)|
| Same draft tests, per-process PYTHONUTF8=1 |13|0|0|0|[UTF-8](draft-tests-utf8.txt)|
| CI helper Red against old implementation |28|27|1|0|[Red](gate-red.txt), [summary](gate-red.json)|
| CI helper Green, same unchanged test source |28|0|0|0|[Green](gate-green.txt), [summary](gate-green.json)|
| Parent combined PR-rule + CI-helper regression |41|0|0|0|[combined](combined-tests.txt)|

Commands run from `tools`: `python -m unittest test_check_pr_rules -v`, `python -m unittest test_check_github_ci -v`, and `python -m unittest test_check_pr_rules test_check_github_ci -v`. UTF-8 is a process environment setting, not a draft edit. Fixing the initial encoding error is not new authored Red/Green implementation. Mock fixtures intentionally print rule errors; the unittest summary is the execution result.

The helper implementation and test hashes are frozen in [gate policy](gate-policy.json). The helper change is a separate commit from the draft. [Before/after proposal](CI_GATE_PROPOSAL.md) defines outcomes, completeness/SHA gates and remaining policy decisions. This is an observation gate; even a pass does not authorize merging this PR.

Original draft issues remain for user review: advisory GitHub workflow versus stricter local helper policy, Windows stdout encoding, [rename leaving an old .meta undetected](draft-rename-probe.json), an absent GC guide and optional absent IMP backlog, and unverified Claude write restrictions/permissions/usage. Original drafts are not corrected under the preservation instruction. Unity compile/EditMode/PlayMode are not run for this Python/workflow-only change; no Unity source, package or project setting is staged.

Committed implementation diff `ef23b9347fab87a847a43dc81af853d622958e34` against base main: 26 changed files, errors0/warnings7, exit0. The seven warnings are expected review notices for protected rule/workflow files; they are retained for human review. [Rule output](pr-rules.txt), [structured result](pr-rules.json). Subsequent documentation-only evidence additions are also checked before pushing; exact final-HEAD results are recorded in the PR body. Live GitHub results must be read after PR creation and reported in the PR body; they are not established by local tests. Claude and guide S1–S5 remain unexecuted until user setup and authorization. App installation, secrets, repository configuration, comments triggering Claude and main integration were not performed.

Independent read-only policy review found no implementation/table mismatch or blocker. It does not prove live API behavior or Claude permissions.
