# 2026-10-12 · GitHub review automation P1

Current follow-up: the originally preserved checker/test drafts are now corrected with explicit user approval. See [PR #1 correction](fix-p1/README.md): Windows no-UTF8 Red17 failures2/errors2 -> Green17/17, combined45/45. Earlier sections below are historical import evidence; Windows output and old-meta rename issues are now fixed. Original baseline hashes are retained; post-correction hashes are recorded separately below.

User authorized file application and a PR only. Base main `0641fb57e4d6fae03fe17b88801c6762d78de84f`, branch `codex/github-review-p1`. Main merge, other branch pushes/deletion, force push, repository settings, app installation and real secrets are outside this unit.

The seven supplied draft files are copied byte-for-byte in commit `b194ebbf090108f4fae1ea0f23b2e64d1ee1d91d`. [Baseline](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/baseline.json) records their SHA256 and the six preserved user changes. AGENTS gains documentation and CI interpretation paragraphs; all preexisting rule lines remain unchanged. The attachment's CODEX_APPLY_PROMPT.md is not installed in the repository.

| Verification | Executed | Failures | Errors | Skip | Evidence |
|---|---:|---:|---:|---:|---|
| Draft PR-rule tests, initial Windows CP949 run |13|0|1|0|[initial](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/draft-tests.txt)|
| Same draft tests, per-process PYTHONUTF8=1 |13|0|0|0|[UTF-8](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/draft-tests-utf8.txt)|
| CI helper Red against old implementation |28|27|1|0|[Red](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/gate-red.txt), [summary](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/gate-red.json)|
| CI helper Green, same unchanged test source |28|0|0|0|[Green](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/gate-green.txt), [summary](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/gate-green.json)|
| Parent combined PR-rule + CI-helper regression |41|0|0|0|[combined](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/combined-tests.txt)|

Commands run from `tools`: `python -m unittest test_check_pr_rules -v`, `python -m unittest test_check_github_ci -v`, and `python -m unittest test_check_pr_rules test_check_github_ci -v`. UTF-8 is a process environment setting, not a draft edit. Fixing the initial encoding error is not new authored Red/Green implementation. Mock fixtures intentionally print rule errors; the unittest summary is the execution result.

The helper implementation and test hashes are frozen in [gate policy](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/gate-policy.json). The helper change is a separate commit from the draft. [Before/after proposal](CI_GATE_PROPOSAL.md) defines outcomes, completeness/SHA gates and remaining policy decisions. This is an observation gate; even a pass does not authorize merging this PR.

Original draft issues remain for user review: advisory GitHub workflow versus stricter local helper policy, Windows stdout encoding, [rename leaving an old .meta undetected](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/draft-rename-probe.json), an absent GC guide and optional absent IMP backlog, and unverified Claude write restrictions/permissions/usage. Original drafts are not corrected under the preservation instruction. Unity compile/EditMode/PlayMode are not run for this Python/workflow-only change; no Unity source, package or project setting is staged.

Committed implementation diff `ef23b9347fab87a847a43dc81af853d622958e34` against base main: 26 changed files, errors0/warnings7, exit0. The seven warnings are expected review notices for protected rule/workflow files; they are retained for human review. [Rule output](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/pr-rules.txt), [structured result](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/pr-rules.json). Subsequent documentation-only evidence additions are also checked before pushing; exact final-HEAD results are recorded in the PR body. Live GitHub results must be read after PR creation and reported in the PR body; they are not established by local tests. Claude and guide S1–S5 remain unexecuted until user setup and authorization. App installation, secrets, repository configuration, comments triggering Claude and main integration were not performed.

Independent read-only policy review found no implementation/table mismatch or blocker. It does not prove live API behavior or Claude permissions.

## PR created and live CI evidence

[PR #1](https://github.com/KangSungKyu/TPLab/pull/1) is open, non-draft, branch `codex/github-review-p1` -> main; main remains `0641fb57e4d6fae03fe17b88801c6762d78de84f`. File application and PR creation are complete; user review/setup remain pending. Original drafts and user dirty changes are preserved.

Actual [PR rule workflow run](https://github.com/KangSungKyu/TPLab/actions/runs/38194948859) for exact head `9383e4d6ea6e89e777f883049c3ab4e96fa634c7` completed successfully. The newly proposed helper was also executed against the real GitHub API: workflow1/check1/status0/run1, complete queries, no issues, main unprotected, gate PASSED/exit0. [Raw observation](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/github-ci-9383e4d.json). Seven protected-file warnings and one GitHub runner-image migration notice appear in [annotations](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/github-annotations-9383e4d.json); no error annotation. The runner notice is independent of the PR rule warning count.

This evidence is bound to the stated SHA. This documentation-only follow-up does not change either tool or test source; final HEAD is rechecked before push and its actual Actions state is reported in the PR body. No CI snapshot is presented as proof for later code changes. Claude review and S1–S5 remain unexecuted. No comments invoking Claude, main merge, branch deletion, App/secret/configuration changes or Unity/power operations occurred.

## Original draft correction follow-up · 2026-10-12

이 PR은 사용자 확인 전 main에 병합하지 않는다. 이 단위는 PR 생성까지 진행하며, 판정 도구 변경과 리뷰 권한 설정은 사용자 확인 후에만 반영한다.

The user separately approved implementing and verifying the draft correction and updating this same PR. That authorization does not authorize main integration or reviewer permission setup.

| File | Original baseline SHA256 | After draft correction SHA256 |
|---|---|---|
| `tools/check_pr_rules.py` | `be732fc2f51b85ae306fa73d2095d234c9d1b50b8ffb873c6eab7e71f5a40b82` | `ff26fea4ba35cc60c8c6d4fa3c81067b3b9655e3b05f410c2f82f7fd7334941a` |
| `tools/test_check_pr_rules.py` | `b356d6765e123544eb41a08f60c235e8bbab316760ae9a33a0f094642cb5aa58` | `a390c856638c5c952b5afb7e2b48eec609bac7dd6429a613c8dc49d2ccbe893d` |

The baseline file is not rewritten. [Correction result/policy](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/fix-p1/fix-policy.json) and [actual Windows evidence](fix-p1/README.md) identify the new source and remaining limits. The supplied AGENTS replacements are exact; original main rules remain intact. PR-tip CI lookup is separate from checking an eventual merged result. Other original draft issues (absent GC guide/backlog, advisory versus stricter local helper, Claude permission/usage) remain for user review.
