# CI gate proposal · PR review required

The gate is a separate change from the seven unchanged supplied drafts. Main keeps its current behavior until this PR is explicitly accepted. A zero exit code clears only this CI inspection boundary, not local tests, human acceptance, PR permission or authorization to merge.

| Exact target state | Current main helper | Proposed helper |
|---|---|---|
| Unprotected main, no workflows/checks/statuses/runs | exit0; says CI not configured | exit0, UNCONFIGURED; CI not executed |
| Workflows exist and target CI executed successfully | always stops on CI presence | exit0 only with complete exact-SHA evidence and success outcomes |
| Workflow exists but target has no execution evidence | stops | stops as unexecuted/missing evidence |
| Failed/error/pending/in-progress/cancelled/timed-out CI | stops on presence | stops on outcome/state |
| Completed skipped/neutral/unknown/null outcome | stops on presence | stops; no unapproved success exception |
| Protected main | stops | stops; no branch-protection bypass |
| Wrong SHA, incomplete/unstable pages, read failure | incomplete inspection or request failure | stops with incomplete/identity evidence preserved |
| Multiple Actions runs for same SHA, older failed and later succeeded | stops on CI presence | conservatively stops on any returned failed run; latest-run selection is deferred |

The proposed source retains complete workflow/check/status/run objects and inspected identities. Every100-item page is collected and count/truncation/duplicate-ID/read-error conditions are handled. An unknown/absent expected-check policy is not replaced by a claim that all intended jobs ran. There is no required/optional check catalog in this unit.

The supplied guide calls the Phase1 PR rule workflow advisory. GitHub can leave it non-required while this local inspection tool still stops on its failed result. That is a policy difference for user review, not resolved by changing the immutable draft or silently ignoring failures. A guard allowance does not override this task's PR-only instruction.

Known unchanged draft issues: Windows CLI reporting needs UTF-8 environment; a pure source rename can leave old `.meta` undetected ([reproduction](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/ci-review-p1/draft-rename-probe.json)); CLAUDE.md's GC guide is absent and its optional backlog/IMP numbering has no current source. These are reported rather than fixed under the draft-preservation instruction. Claude tool restrictions, app token permissions and subscription use await user setup and S3/S4 verification.

Primary references: [GitHub check run state/conclusion](https://docs.github.com/en/rest/checks/runs#get-a-check-run), [workflow runs and pagination](https://docs.github.com/en/rest/actions/workflow-runs#list-workflow-runs-for-a-repository), [events/default-branch requirements](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#issue_comment), [Claude Action documented inputs](https://github.com/anthropics/claude-code-action/blob/main/docs/usage.md). These references establish API/event contracts, not a successful execution of this repository's workflows.

## 2026-10-12 follow-up

The earlier known draft issues describe the initial import. With explicit user approval, Windows report encoding and old-meta rename detection are corrected in [the review follow-up](validation/ci-review-p1/fix-p1/README.md). The original baseline hashes remain intact. CI-helper behavior/table are unchanged. PR result lookup targets the candidate source branch tip; validating an eventual merge result remains a separate obligation under existing AGENTS rules.
