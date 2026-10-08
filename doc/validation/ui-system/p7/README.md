# UIContext P7 verification

2026-10-08. Status: automated implementation/regression/consumer/performance and both final samples passed; final user acceptance pending. Physical-input and visual acceptance are pending. This is development source validation, not a UI UPM release or main approval.

## Source and preservation

- Original final regression source: `99df25dd6b89fd7f3e322c8033b61eb1364f495e`. [476 captured inputs / 475 Git matches](final-source-check/source-commit-match.json); protected Input sample is excluded from Git matching and all six protected files match their original raw bytes. Source was unchanged before/after the final regression.
- Final source-consumer candidate: `8310a94bcb674f28bee2c246c4e5eae9d6c83804`. [Candidate relationship](candidate-source-check/source-commit-match.json) records the one changed captured input: the consumer-only teardown driver. Original Assets/Packages/ProjectVersion regression inputs are unchanged. [Final captured inputs](test-inputs.json) match the before-consumer snapshot.
- UI/Core/Input Runtime is unchanged from P6 `21f89e2580f08be3903724efa0a42e5ad0567c83`. P7 adds checked-in validation tools and changes one optional UI test fixture to seed a fixture-owned Addressables locator only when the already-initialized global locator list is empty. Existing global state is not reset; original locators/preferences are preserved.
- Original Editor PID24376, Unity6000.3.18f1 / Connector0.4.1. New isolated batch Editors import consumer sources, not a substitute for original development tests. Native compiler registry omission was temporarily corrected for compilation; permanent registry repair is not claimed.

## Completed gates

| Gate | Actual result | Evidence |
|---|---|---|
| Original EditMode, filter TPLab | 318/318; failed0, skipped0 | [normalized](final-regression/edit.json), [native](final-regression/edit.native.json), [runner](final-regression/edit-runner.txt) |
| Original PlayMode, filter TPLab | 306/306; failed0, skipped0 | [result](final-regression/play.json), [runner](final-regression/play-runner.txt) |
| UI subset of those results | Edit33 + Play53 = 86/86 | Exact names in the two results above; remaining Core/Input538 passed |
| Python source-consumer contract fixtures | 5/5; failed0, skipped0 | [promoted Green](promoted-helper-green.txt), [corrected direct Green](corrected-helper-green-direct.txt), [final Green5](final-helper-green.txt) |
| Base source consumer, no Input | 1 actual Editor build + 3 actual graphics Players; all passed | [base-c report](consumer-base-c/consumer-report.json) |
| Optional Input consumer | Build2 artifacts, regular smoke/scroll/canvas Players passed; full invocation failed at sample inventory fixture | [original report](consumer-input-additive-a/consumer-report.json), [diagnosis](sample-first-diagnosis.json); not an overall passed invocation |
| Final optional Input + Single consumer | Editor1 builds2 artifacts + actual4 graphics Players(smoke/scroll/canvas/sample); all exit0, no timeout, complete fresh markers | [report](consumer-input-single-final/consumer-report.json), [sample deep validation](consumer-input-single-final/sample-validated.json) |
| Final optional Input + Additive consumer | Editor1 builds2 artifacts + actual4 graphics Players; all exit0, no timeout, complete fresh markers | [report](consumer-input-additive-final/consumer-report.json), [sample deep validation](consumer-input-additive-final/sample-validated.json) |
| Final samples, same source8310a94 | Single80/Additive78 checks; each inventory7, manualContinue4, ordered cleanup2 and30 consecutive positive-draw frames(min11) | [Single raw](consumer-input-single-final/sample-result.json), [Additive raw](consumer-input-additive-final/sample-result.json); physical/visual verified=false |
| Product Console | Unexpected errors0; expected negative-test logs preserved before clearing | [empty product Console](final-product-console.json), [expected test errors](final-test-expected-console.json), [Editor observation](final-editor-observation.json) |

PlayMode CLI transport returned1, but the same actual run `27988-1791461694519407600` completed in Connector's result file: nonzero306/306, exact project/filter/Editor. This is recovery of the completed run, not another test execution. The UI fixture's original Setup failure and source-specific regression remain in [first diagnosis](full-regression-first-diagnosis.json) and [original regression](original-regression/play.json).

## Graphics and performance

Source `ee5e233744ac8ac4285a9d1340c974675b233400`, Windows64 Development Mono, Unity6000.3.18f1, Direct3D12, NVIDIA RTX4060 Laptop GPU, 1280x720. Every measured arm has 120 warm frames then 600 consecutive fresh positive-draw frames. Each invocation has a unique runId; distinct modes/PIDs and execution times identify its serial graphics processes. Exact copied source/harness/manifest/lock hashes, fresh logs/result, normal exit and before/after source equality are required. Players use a bounded visible window without requesting focus (`SW_SHOWNOACTIVATE`).

- Native ScrollRect1000 vs virtual1000 and virtual10000: both virtual arms owned41 cells, cumulatively created41/destroyed0 during scrolling; measured bound cells38–41. Demand ceiling42. Empty/restore/resize/late binding/cleanup are separate functional tests.
- Canvas1(shared),3(frequency-separated),4(popup Canvas): same128 HUD Graphics and three25-Graphic popups; same120-frame schedule repeated five times in the600 measured frames, schedule mismatch0.
- [Performance report](../../../UI_PERFORMANCE.md) preserves median/P95/max, marker availability and raw data. Virtual1000 had lower median Main Thread than native1000 in this run, but higher GC allocation. Draw calls were equal3. No zero-allocation guarantee or universal Canvas recommendation follows. Large Main Thread maxima remain unexplained.
- Independent overdraw and batch-breaking-reason capture: N/A. Endpoint startup/resize/close samples are not full operation-cost measurements.

[Scroll raw](consumer-base-c/scroll-benchmark.json), [scroll validated](consumer-base-c/scroll-validated.json), [Canvas raw](consumer-base-c/canvas-benchmark.json), [Canvas validated](consumer-base-c/canvas-validated.json), [smoke validated](consumer-base-c/smoke-validated.json). Optional Input smoke also tests runtime-clone action references, synthetic owned Keyboard/Mouse events, independent leases, held Cancel quarantine and actual EventSystem processing; [25 checks](consumer-input-additive-a/smoke-result.json). Synthetic input does not prove physical devices.

## Preserved failures and corrections

Helper actual Red4→Green4 and Git-attributes Red5→Green5 remain in [Red4](../p6/p7-consumer-red.txt), [Green4](../p6/p7-consumer-first-green.txt), [newline Red5](../p6/p7-newline-red.txt), [newline Green5](../p6/p7-newline-green.txt).

1. Base-a smoke failed at an empty-list fixture expecting zero owned cells. Inactive cached cells may remain within viewport budget; corrected assertions retain active0/owned bound and exact unbind counts. [Diagnosis](first-consumer-diagnosis.json).
2. Base-b produced600 fresh frames with Draw Calls0 because the window was hidden. Running the **same binary** visible without activation produced600 positive frames, minimum3: [controlled diagnostic](visible-smoke-diagnostic/validated.json). Only the validation launcher changed; UI/Camera product sources did not.
3. Optional Additive sample counted every child under Content as active. Inactive cache children are owned but not displayed; corrected active-hierarchy assertions and inventory snapshots retain binding/index/budget checks. The original invocation launched one failing sample Player although its old bookkeeping reported0; [diagnosis](sample-first-diagnosis.json) preserves that discrepancy. The runner now counts execution before validation and records completed regular performance stages before sample execution.
4. Single sample functional checks and30 positive draw frames passed, but shutdown attempted to unload Unity's last normal scene. [Original failed result](sample-single-a/result.json), [process](sample-single-a/process.json). Final Single fixture correction passed: an empty, uniquely named, fixture-owned parking scene is created only after functional/render checks, excluded from Manager registration, and retained through Quit. Actual Manager Stop/registered0/owned0, game unload and UI→Input shutdown are required. Additive needs no parking (two normal scenes before shutdown). Core's documented last-scene failure contract remains unchanged.
5. Real CSV license CRLF/LF mismatch is handled with canonical Git blobs plus separate untouched working hashes. Helper fixtures include a genuine Git attributes Red/Green case. Zero-test ImportError attempts remain in [failed invocation](corrected-helper-green.txt); direct5-test execution is the Green proof.

Both final combined optional invocations execute regular smoke and the six benchmark arms again on source8310a94, then the separate sample. Their full reports pass. The primary performance table preserves the earlier base-c exploratory measurement unchanged; the final raw repeats are supplemental, not an averaged performance claim.

Original failed results are preserved with their actual source SHA and counts. The scoped `.gitattributes` preserves JSON/log/text payload bytes so recorded raw hashes survive Git checkout; source text normalization is recorded separately. Build-only reports legitimately say Player executions0/performanceUnmeasured; separately launched sample Players have separate process/results and do not rewrite their build report.

## Reproduction and remaining acceptance

The checked-in [runner/harness guide](../../../../tools/ui-consumer/README.md) accepts an absolute checkout, exact current40-character HEAD, explicit installed Unity executable, and new output/evidence directories. It refuses reuse/path escapes/links/dirty selected sources and validates actual source/clone identity. It does not merge Git, edit the original settings, or provide a released package.

Follow [UI acceptance](../../../UI_ACCEPTANCE.md) for both Bootstrap entries, actual keyboard/mouse and available gamepad, HUD/ABC/child/veto,1,000/10,000 list, nested scenes, cover/loading/manual Continue, aspect ratios and focus restoration. Physical input/visual usability are pending; the final main merge and branch deletion wait explicit user confirmation. IL2CPP, other Unity versions/platforms and long-duration soak remain outside this validated scope.

[Final static verification](final-static-check.json) covers18documents/599relative links,476source hashes,58owned asset GUIDs and protected6raw bytes. [Temporary cleanup](temporary-cleanup.json) removed18consumed owned paths; final two source8310a94 review consumers remain until manual acceptance.
