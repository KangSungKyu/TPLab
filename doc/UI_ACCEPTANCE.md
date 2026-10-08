# UIContext sample: final manual acceptance guide

Status: **Manual physical-input/visual acceptance pending.** Original regression and automated consumer/Player evidence are recorded in [P7 verification](validation/ui-system/p7/README.md). This guide covers the remaining project-owned sample confirmation; automated commands and raycasts do not establish physical input or visual usability. The sample source and current controls are in [Samples/UI README](../Assets/TPLab/Samples/UI/README.md), [controller](../Assets/TPLab/Samples/UI/Runtime/UIContextSampleController.cs), and [builder](../Assets/TPLab/Samples/UI/Editor/UIContextSampleBuilder.cs). Use the checked-in runner/harnesses under `tools/ui-consumer`; generated consumer projects and Players are review artifacts.

## Prepare the sample

1. Use the TPLab project after the UI installer/settings and optional Input adapter are present. The sample requires the project's supported Input System backend, uGUI, UniTask, Core, Input, and UI. Base `TPLab.UI` does not require Input System; the demo's `TPLab.UISamples.OptionalInput` assembly explicitly references `TPLab.Core.Input` and `Unity.InputSystem`.
2. The sample-owned assets are already present in this project. Do **not** run **TPLab > UI > Create Sample** here. That builder is for a fresh destination only: it creates the named sample assets when none of the planned outputs or `.meta` files exist, and refuses to overwrite or clean existing paths. The sample borrows the built-in `LegacyRuntime.ttf`; it does not edit the existing Input sample actions or settings.
3. Select one entry scene, `UIContextBootstrapSingle` or `UIContextBootstrapAdditive`. Add the selected entry and these four scenes to Build Settings in this order: `UIContextHub`, `UIContextMain`, `UIContextArea`, `UIContextNested`. The builder intentionally does not edit Build Settings. Open only the selected Bootstrap scene for Play. At first startup, wait for preparation to finish and press the initial **Continue** once (defaults are loading presentation and manual Continue on); only then begin the toolbar walkthrough. All UI definitions use direct prefabs, so this walkthrough does not need a `ResourceManagerInstaller`, Addressables catalog, or global service lookup.
4. Keep the serialized owner order **Input -> sample scope -> UI**. The scope provides the runtime UI factory/hooks and registers separate Player/UI maps and mapless modal/transition layers before leases are acquired. The sample's common UI/EventSystem/toolbar/cover/loading canvas stays owned by the Bootstrap root. Do not run both Bootstrap variants together.

## HUD, popup, focus, and close behavior

Use the diagnostic toolbar (commands are shown literally on the buttons):

- Press `hud-a`, then `hud-b`, then `hud-a`. Confirm the HUD changes and reusable display ownership remains with the common context.
- Press `abc`: the harness opens A modeless, B modal, then C modeless. Press `close-b` while B is between A and C. Confirm B closes, C remains visible and eligible/focused, and input held during closure does not trigger newly exposed A/C. Repeat with mouse button, keyboard/gamepad submit, cancel, and movement controls held through the close, then released. Wait for the following EventSystem frame before expecting the lower UI/game layer to receive a new action. A click or submit already in progress must not be replayed to A.
- With B visible in the `abc` setup, press `mode-b` to toggle modal/modeless. Verify the UI map remains available while a modal blocks the game layer; releasing B must not release separate game, preparation, or transition blocks. Press `abc` again before the next A/B/C test, because `close-b` retired B.
- With A visible, press `front-a` and confirm A's subtree is brought forward without rearranging borrowed scene siblings. Then press `child`, `close-child`, then `child` and `close-parent`. C as a child closes before its parent; closing the parent removes its child subtree. Since `close-parent` also retires A, press `abc` again before testing C close approval.
- Approval defaults to veto. With a fresh top-level C from `abc`, press `user-c` and confirm C stays open while veto is enabled. Press `veto` to toggle approval and try again. `outside-c` is a harness command that requests `OutsidePointer` approval; it is not an automatic backdrop detector. Native Cancel requests approval through the adapter. `force-c` must close C regardless of veto. Confirm the view close button and child close path leave no stale focus or subscription.

The toolbar deliberately sits outside managed displays so it can address a middle or blocked handle; its ability to invoke a demonstration command does not mean the blocked managed view should receive input.

## Virtual inventory

The inventory is opened during sample startup and uses a separate batching Canvas that inherits the managed host plane. Check its visible range and status (`Count`, active/inactive/owned cells, cumulative created/destroyed, visible range) while performing this sequence:

1. `1000`; scroll from start through the middle and end using native content drag and wheel input. This sample does not create a scrollbar. Press `last` and confirm the final logical item is visible without overscrolling.
2. `10000`; repeat start/middle/end and `last`. The bound rows should track the viewport; scrolling should not grow the owned cell count beyond the viewport budget. This is functional observation only, not a performance claim.
3. Press `0`, verify no active rows and a valid empty range; then press `1000` and verify the list recovers. Press `refresh` and confirm visible labels change to `Updated item …` without stale values replacing a recycled row.
4. Press `resize` repeatedly to alternate viewport heights 240 and 360. Verify visible coverage and correct row/index labels after each resize. Return to start/middle/end after resizing; confirm no duplicate or missing visible indices and stable warmed create/destroy counters during repeated scrolling.

Changing the list data is explicit: `refresh` updates prepared data and calls `Refresh`. Ordinary scrolling and viewport changes should preserve unchanged bindings. The status display allocates diagnostic strings, so do not treat it as a benchmark.

## Scene lifetime and loading paths

The common HUD, inventory, Input owner, and toolbar should remain alive while the owned game scene changes. `area` adds an Area under the current Hub/Main scene; `nested` adds Nested under Area. `remove-nested` removes just Nested; `remove-area` removes Area and its Nested subtree. Run these in both entry modes, inspect the scene-context counts, and confirm each retired scene context is disposed while common UI/Input remains. Repeat primary scene replacement with `replace`; Single replaces the primary scene, while Additive keeps the common owner and swaps the primary scene. Do not interpret the scene-local passive HUD as sharing the common EventSystem or modal/focus behavior.

For presentation modes, `loading` toggles loading UI and `manual` toggles Continue; both default on. Press `replace` to switch Hub/Main:

- With `loading` enabled and `manual` enabled, confirm the cover/loading UI is visible, progress text updates by stage, and the external **Continue** button appears only after controls are released and a later frame has elapsed. Continue with a fresh pointer click or keyboard/gamepad Submit. Verify the operation completes once and the next transition can run.
- Toggle `manual` off and repeat. The same cover/loading lifecycle should continue automatically without waiting for Continue.
- Toggle `loading` off and repeat. Confirm the cover-only Core transition path blocks the underlying A/B/C until scene finalization, then retires its independent transition shield/lease.
- Exercise both `UIContextBootstrapSingle` and `UIContextBootstrapAdditive`; also add/remove Area and Nested under each primary. Try a cancelled/failed transition only when a safe reproducible fixture is available, and confirm the failure remains covered until Core recovery completes.

## Physical-device and visual acceptance record

Run in a foreground graphics Player for the supported Windows Mono configuration, then repeat in the Editor if desired. Use an actual keyboard and mouse; verify pointer click/drag/wheel, navigation, Submit, Cancel, focus return, and the held-control close quarantine above. If a physical gamepad is available, repeat navigation/Submit/Cancel with it; if none is available, record gamepad as N/A (unavailable), not passed. Repeat at the target aspect ratio and at least one narrow/wide window size. Minimize or move the window out of focus during a transition, restore it, and verify focus, input maps, Continue, and the next scene operation recover without a duplicate action. Capture the exact Player backend, graphics device, display/resolution/aspect, peripherals, entry mode, step, result, and visible failure/error text.

Automated evidence and manual acceptance answer different questions. UI installer/settings Edit/Play tests prove their tested lifecycle and cleanup cases. Sample source import/compile and asset authoring prove only those authoring gates. Consumer assembly compile, isolated install smoke, Player startup, and command-level scenarios must be recorded separately. Manual hardware, visuals, aspect-ratio behavior, foreground restoration, and user acceptance remain pending until observed and recorded; no profiler or performance result is implied by this guide.


## Verified Player builds for review

The completed source-consumer builds are retained locally under `Temp/UIConsumer-input-additive-p7-final/SampleBuild/UIContextSample.exe` and `Temp/UIConsumer-input-single-p7-final/SampleBuild/UIContextSample.exe`. These are generated outputs, not source-tool dependencies or released UI packages. Launch either normally without `TPLAB_UI_MODE`/`TPLAB_UI_RUN_MODE=sample`; the validation hook remains inactive, so the interactive sample stays open. A fresh checkout can build them with the checked-in [consumer runner](../tools/ui-consumer/README.md). In the retained consumer Project, the matching bootstrap and four target scenes already have an isolated build list.

Record the result for both entry modes, physical devices (gamepad N/A when unavailable), screen sizes, focus restoration, and unexpected Console/log errors. The main merge and branch deletion remain pending explicit user confirmation.
