# Scene loading presentation P4 validation

2026-10-07. SourceRevision `18d479bf07fe8479a22187ec7357024e9979d096` (runtime/sample commit `91d6f16`; following commit only trims new documentation metadata whitespace). Core source starts from P2 `07273a4ed4a8e0656e49d7b8fdbe897b32bc5f5e`. [Snapshot](test-inputs.json) records the final normalized source/tool inputs; [protected inputs](preserved-inputs.json) use original raw hashes. Metadata/document-only edits after tests do not change runtime behavior.

| Gate | Actual result | Evidence |
|---|---|---|
| Full EditMode, final runtime source | 271/271; failed0/skip0 | [final-edit](final-edit.json) |
| Full PlayMode, Core/Input/sample | 253/253; failed0/skip0 | [full-play](full-play.json) |
| Reload, same Editor; loading UI | 12/12; failed0/skip0 | [reload-ui](reload-ui.json) |
| Additive Windows x64 Mono build | Succeeded; errors0/warnings0; all restoration flags true | [build](build-additive.json) |
| Single Windows x64 Mono build | Succeeded; errors0/warnings0; all restoration flags true | [build](build-single.json) |
| Additive Player | 12/12 structural observations, fresh process/exit0 | [result](player/scene-player-additive-20261007T054303Z-36760.json) |
| Single Player | 12/12 structural observations, fresh process/exit0 | [result](player/scene-player-single-20261007T054350Z-21564.json) |
| Separate Core+Input consumer | Editor build + Player: actual2 runs, exit0, every required observation true | [result](consumer/core-consumer-20261007T054058Z-35276.json) |
| Original Editor, idle after tests/build/settings restore | PID23120; compile/update/play false; InitScene restored; current errors/warnings empty | [status](final-status.txt), [state](final-state.txt), [Console](final-console.json) |

Unity6000.3.18f1, UniTask2.5.11, Addressables2.9.1, InputSystem1.19.0, Connector0.4.1. Original MyLab Editor executed all named/full tests and two scene builds. The independent consumer is explicitly an export/import verification project, not a substitute Editor for MyLab tests. Its ResourceManager smoke verifies empty shutdown rather than actual external catalogs/content; loading presentation uses project callbacks and does not ship sample UI in Core. [Consumer build log](consumer/logs/editor.log) and [Player log](consumer/logs/player.log) are preserved; temporary paths in original reports are historical output locations.

Play CLI returned1 while the Connector produced a fresh nonzero exact-process run result. The report records exact CLI PID, Connector runId, original editorPid and individual tests. No alternate instance or suite was substituted. The reload-request CLI response timed out; it is not success evidence. Later original-Editor readiness and fresh12 tests are the evidence. Deferred build scheduling produced no build output, so it is not counted; the actual direct build results above establish the two builds.

TDD failures preserved: [initial Red8](red-ui.json) 0/8; [invalid fixture Green11](green-ui.json) 0/11; [module boundary Green11](green-ui-fixed-target.json) 0/11; [bound-module Green11](green-ui-bound-module.json) 11/11. [Ready display Red1](review-red-ready-ui.json) 0/1 reproduced the missing ready label/full bar; final full Play includes its fix and new12th UI check. Earlier [full-edit](full-edit.json) is before this display fix; final-edit supersedes it. Historical P1/P2 results remain in their folders.

[Known generated setting restoration](build-generated-restoration.json) restored only build-produced changes; original dirty4 and protected raw7 were preserved. [Preclear Console](preclear-test-console.txt) preserves expected negative-test messages before clearing them. Final Console was inspected after tests, builds and restored-setting import with original scene idle; it does not establish interactive product UX. Native Player logs contain no Exception/error-CS entries.

2026-10-07 user PlayMode feedback completed the final acceptance gate; individual mode/device/resolution results were not supplied. See [integration record](../integration/README.md) and [human checklist](../../../SCENE_LOADING_ACCEPTANCE.md). Native Player runs use automatic presentation and synthetic tests cover native Input module paths. Physical keyboard/gamepad/pointer, visual layer order, resolutions, manual UX, remote Addressables content, IL2CPP and other Unity/platform combinations are unverified. Descriptive API snippets are NotRun. [GitHub inspection](source-github-ci.json) confirms the pushed source commit has workflow/check/status/run counts0 and main is unprotected. This is CI unconfigured, not CI success; no test count0 or absent CI is called success. main integration and owned branch cleanup follow the confirmed gate.

Only this unit's transient consumer/build/run directories and Python cache are removed after evidence preservation. Source-generated assets/meta and preexisting Temp outputs are retained. Re-run tools with fresh owned paths to reproduce.

Raw Unity logs retain their generated trailing whitespace; only these preserved `.log` files are excluded from source/document whitespace checks. Their text is not rewritten to manufacture a clean result.
