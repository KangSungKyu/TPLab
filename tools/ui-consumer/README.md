# P7 source consumer preparation

The reusable runner and harnesses are committed source tools. Their Python contract fixtures passed 5/5 after actual Red and Green execution, including Git newline normalization without original-file mutation. Consumer compilation, Player behavior, measurements and visual acceptance are separate gates recorded per invocation.

The runner resolves its harnesses under `tools/ui-consumer/templates`. Use an exact committed revision for each invocation; preparation commits and historical fixture results are never a default execution revision.

The driver imports only pure helpers from the explicit existing `tools/run_core_consumer.py`. It never calls that runner's headless execution path or replaces the original Editor. It creates a new direct checkout Temp child with a Project subdirectory and a fresh direct `doc/validation/ui-system/p7` evidence child. Existing paths, path traversal, symlinks/junctions/reparse points, and unknown runtime extensions are refused. No cleanup/reset/reuse of a prior project is automatic.

Use an exact final committed development SHA including the reviewed P6 sample when selected. `source_identity` verifies exact HEAD and each selected regular blob OID against `git hash-object --path` (read-only, without -w), honoring the actual Git text/binary attributes. Evidence preserves the canonical blob SHA256 and original workingRawSha256 separately. The new consumer receives canonical Git blob bytes, and original working bytes remain untouched. Substantive content changes still fail; binary DLLs remain exact. The base profile copies Core and UI Runtime only, required folder/file metas, CsvHelper DLL/license/metas, and the UniTask license. Optional Input adds only Input Runtime and UI InputSystem Runtime. Root LICENSE and THIRD_PARTY_NOTICES.md are verified/copied by a separate exact-blob gate. Tests, original scenes/settings, unrelated samples, original ProjectSettings, and Core Editor tools are excluded. Generated optional PlayerSettings reads only the original serializedVersion and sets activeInputHandler 1; base gets fresh Unity defaults.

Invoke from the checkout, substituting a real fresh name, exact SHA and installed Unity path:

```powershell
python tools/run_ui_source_consumer.py `
  --project . --revision <exact-40-digit-final-dev-SHA> `
  --unity "C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Unity.exe" `
  --core-runner tools/run_core_consumer.py `
  --scroll-benchmark tools/ui-consumer/templates/UIVirtualScrollBenchmark.cs `
  --canvas-benchmark tools/ui-consumer/templates/UICanvasBenchmark.cs `
  --output Temp/UIConsumer-base-<fresh-id> `
  --evidence doc/validation/ui-system/p7/consumer-base-<fresh-id>
```

Quote the Unity path in PowerShell because it contains spaces. Run a second fresh invocation with `--include-input`, distinct output/evidence names, to establish the optional native profile. The driver does not infer that running one profile proves the other. Add `--timeout 1200` or another positive explicit bound if needed.

Each full profile starts one isolated batch Editor build and three separate graphics-capable Windows64 Development Mono Players: smoke, scroll benchmark, Canvas benchmark. Player commands contain neither `-batchmode` nor `-nographics`; no original Editor is selected. Hidden process launch does not establish rendering: all measured arms require 600 fresh positive Draw Calls samples, actual non-Null device, exact version/backend/revision/project, 1280x720, ordered process/result times, fresh process logs, and normal exit. All arms use vsync 0, target frame rate -1, and runInBackground true.

Smoke independently checks public source usage: explicit SceneOwnedRoot install/prepare; borrowed Canvas/source/provider/EventSystem; direct/keyed HUD replacement and child cleanup; veto/forced close; retained clone generation; order/mode commands; virtual rows 1k/10k/0 bounded ownership/binding cleanup. The optional profile additionally configures all module actions from one runtime UI map, queues actual native owned Keyboard states, checks modal gameplay blocking, independent transition ownership, intercepted held Cancel/quarantine, external module enable under BlockAll, and UI-before-input/root shutdown preserving borrowed Input/EventSystem. Owned synthetic input devices are removed; existing hardware devices are not reset/removed. This consumer proof does not replace original-project native tests or the broader Core consumer's own CSV/transition smoke.

Benchmarks reuse the two explicit frozen source files. A single declared adaptation in each consumer copy changes `private async UniTask RunAllAsync` to `public async UniTask RunAllAsync`; the driver then awaits the typed harness entry and its cleanup. Product APIs and original files are untouched. This avoids measured-loop file polling and unobserved benchmark completion. Original and adapted hashes, harness template hashes, copied product hashes, manifest/lock hashes and package versions are retained. Bench raw files remain unchanged; validated files normalize the JsonUtility marker array to dictionaries and units without inventing measurements. Missing or incomplete counters retain N/A/Partial and reasons; summaries for N/A remain null and samples empty. A missing/partial Draw Calls marker fails the graphics gate. Other unavailable markers yield Partial performance evidence. One endpoint frame does not establish total startup/resize/close cost. Overdraw/BatchBreakingReason remain N/A absent independent captures.

Optional sample build:

```powershell
# Add to an optional-input invocation:
--sample --sample-entry UIContextBootstrapAdditive
# Or explicitly UIContextBootstrapSingle. Add --build-only to omit all three smoke/benchmark runs.
```

Sample selection copies only `Assets/TPLab/Samples/UI/Runtime` and `Editor` code/asmdefs/metas, parent folder metas, exactly HudA/HudB/PopupA/PopupB/PopupC/Inventory/Cell/SceneHud prefabs, CommonUI/SceneUI/TransitionsSingle/TransitionsAdditive settings, UIInput.inputactions, and UIContext Hub/Main/Area/Nested/BootstrapSingle/BootstrapAdditive scenes, each with its meta. Every copied path must match the exact committed Git blob after its Git clean policy; additional files in the named asset folders are refused. The existing sample builder is never invoked. The fresh consumer Editor builds the explicit bootstrap plus four targets into `SampleBuild/UIContextSample.exe`; only this new consumer's build list changes.

Sample build success records actual sample Player executions **0** and acceptance **Unexecuted**. The sample Player has an interactive project UI and does not auto-quit; launch it separately for root-authorized automation/manual acceptance. It is never treated as the automated smoke or benchmark result. `--build-only` likewise reports build scope and Unmeasured performance; it does not claim smoke/runtime/render success.

Evidence: source-manifest.json; editor-result.json; fresh Editor/Player logs and captured stdout; raw smoke/scroll/canvas result files; raw two benchmark files; derived validated files preserving N/A; final consumer-report.json with exact process counts, failures, source/harness/package identities, and Partial/Measured status. The generated projects/builds are retained for review; no automatic deletion occurs.

Required gates: exact selected source commit; actual fresh consumer import/compilation/build for base and optional Input; hardware/marker/600-frame graphics proof; separate sample Player sequence acceptance; product Console and final visual/user acceptance. All actual consumer compilation/runtime/measurement counts from this preparation are **0**.


For the separately reviewed automated sample driver, add `--sample-validation-script <absolute-checkout>/tools/ui-consumer/templates/UIConsumerSampleSmoke.cs` together with --sample/--include-input/--sample-entry. Its exact source hash is recorded and it is copied into UIConsumer.Runtime, whose explicit sample reference exists only when --sample is selected. After the three regular Players, the runner starts the separate SampleBuild Player in env mode sample, collects its fresh distinct result, and validates process/identity/time/backend/device/resolution/lifecycle success. Its deeper sample functional and fresh-draw schema remains a distinct root review gate; this driver never labels it the regular smoke or benchmark evidence. --build-only suppresses every Player including this one. Without an explicit validation script, the sample remains build-only with actual sample Player executions 0.

The optional runtime harness uses an Input System package versionDefine in its own asmdef, and checks the actual compiled profile against the selected environment. Base cannot silently count an optional-only smoke as executed, and optional cannot silently omit native checks.


The fifth contract fixture covers working CRLF versus committed LF under actual Git attributes. Its Red run failed the original raw-byte gate and its Green run passed the corrected read-only identity/canonical-copy contract. The original four fixtures remain unchanged. All five promoted fixtures passed with failure 0 and skip 0; evidence is in `doc/validation/ui-system/p7/promoted-helper-green.txt`.
