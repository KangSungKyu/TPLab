# 2026-10-12 · GitHub review automation P1

User authorized copying the supplied ci-draft files and creating a PR only. Base main `0641fb57e4d6fae03fe17b88801c6762d78de84f`, branch `codex/github-review-p1`. Main merge, other branch pushes/deletion, force push, repository settings, app installation and real secrets are outside this unit.

Seven supplied draft files are copied byte-for-byte; [baseline](baseline.json) records their SHA256 and the six preserved user changes. AGENTS only gains document guidance at this stage. Existing behavior rules are preserved. The attachment's CODEX_APPLY_PROMPT.md is not installed in the repository.

PR-rule tests: [initial Windows run](draft-tests.txt):13 executed,12 passed/1 error/skip0 because CP949 stdout cannot encode the report em dash. [UTF-8 environment run](draft-tests-utf8.txt):13/13,fail0/skip0 using the same unmodified files and command with per-process `PYTHONUTF8=1`. This is environment repair, not a new authored Red/Green implementation. Ubuntu GitHub execution and Claude review have not run at this preparation point.

Draft issues retained for user review: the guide calls checks advisory while its proposed merge tool blocks failures; CLAUDE.md refers to an absent GC_ALLOCATION_GUIDELINE.md and an optional absent IMPROVEMENT_BACKLOG.md. Read-only Claude tools/token permissions and subscription usage are not established by local Python tests. No nonexistent guideline or permission guarantee is invented. The proposed CI guard will be a separate commit with offline Red/Green evidence and a before/after table.
