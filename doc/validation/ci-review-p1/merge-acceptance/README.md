# 2026-10-12 · PR #1 accepted for integration

User explicitly approved "승인, 병합" for [PR #1](https://github.com/KangSungKyu/TPLab/pull/1). This supersedes the earlier PR-only instruction for this integration. Existing post-merge branch-cleanup authorization applies after local/remote main match and source commits are reachable. App, OAuth secrets, repository settings and Claude comments remain outside the action.

Approved source candidate `0162f7eb39b5d8fc505d05323206bbe4c75ecfc8`, unchanged base main `0641fb57e4d6fae03fe17b88801c6762d78de84f`. Actual API inspection showed open/non-draft PR, mergeable/clean, main unprotected. [Observation](approved-pr.json). Existing user dirty6 match the original baseline and remain unstaged. No Unity/runtime/API/package/source change is added during acceptance.

Fresh Windows regression from `tools`: `python -m unittest test_check_pr_rules test_check_github_ci -v`, no PYTHONUTF8/PYTHONIOENCODING. **45/45, failures0/errors0/skip0**. [Result/source identities](merge-tests.json), [UTF-8 transcript](merge-tests.txt), raw CP949 log alongside. The following acceptance commit adds only this evidence and retrospective; implementation/test hashes remain the same. Final source-tip Actions and merge metadata are reported in PR #1.

Normal GitHub merge preserves original commits; squash/rebase are excluded. Source-tip CI inspection does not exempt the merged tree/ancestry and dirty-file checks. After merge, fetch/switch main normally; verify merged tree equals the inspected candidate tree, local/remote main identity, original commit ancestry and protected files before any branch deletion. Remote deletion must compare the exact source tip; delete local branch only with `git branch -d` after main is checked out. If state moves, preserve the branch and resolve it.

This is the pre-integration acceptance record. The actual merge SHA, final Actions ID and branch-cleanup outcome are recorded in the final PR body and Git history after they occur. Claude review/App/token/permission/use and guide S1–S5 remain unexecuted. Unity and PC are not controlled by this request.
