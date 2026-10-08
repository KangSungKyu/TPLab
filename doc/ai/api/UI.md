# UI API Reference

## Module / Namespace / Assembly

`TPLab.UI` / `TPLab.UI` / [runtime source](../../../Assets/TPLab/UI/Runtime). Runtime assembly references `TPLab.Core`, `UniTask`, and `UnityEngine.UI`. Confirmed environment: Unity 6000.3.18f1, UniTask 2.5.11, uGUI 2.0.0.

## SourceRevision / SourcePath / HumanContract

SourceRevision: 8f8abd290d540dc0b6ba30c5acc8b36b271bc302. Public declarations and XML links identify the P5 source revision.

## ImplementationStatus / ValidationStatus / Evidence

ImplementationStatus: Implemented through P5. ValidationStatus: Partial (P5 focused tests/source match verified; P6/P7 remain open). SourceRevision: 8f8abd290d540dc0b6ba30c5acc8b36b271bc302.

## Symbol / Signature / Constraints

P3 adds host registration and HUD selection to the P1/P2 public signatures:

- UIContext P3 additions: void RegisterHost(string id, Transform container); UIHandle CurrentHud { get; }; UniTask<UIHandle> SelectHudAsync(UIOpenRequest request, CancellationToken cancellationToken = default).
- `UIDefinition(string id, GameObject prefab = null, string assetKey = null, UIRole role = UIRole.Popup, string hostId = "default", UIInputMode inputMode = UIInputMode.Modeless, UIRetention retention = UIRetention.DestroyOnClose, UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)`; properties `Id`, `Prefab`, `AssetKey`, `Role`, `HostId`, `InputMode`, `Retention`, `HideStrategy`.
- `UIOpenRequest(string definitionId, UIHandle parent = null, UIInputMode? inputMode = null, UIHooks hooks = null)`; properties `DefinitionId`, `Parent`, `InputMode`, `Hooks`.
- `UIContext(GameObject rootObject, Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null, Func<IDisposable> acquireModalBlock = null, EventSystem eventSystem = null)`; properties `RootObject`, `LifetimeToken`, `IsDisposed`, `Fault`, `Displays`.
- `void UIContext.Register(UIDefinition definition)`; `UniTask UIContext.PrepareAsync(string definitionId, CancellationToken cancellationToken = default)`.
- `UIHandle UIContext.BeginOpen(UIOpenRequest request, CancellationToken cancellationToken = default)`; `UniTask<UIHandle> UIContext.OpenAsync(UIOpenRequest request, CancellationToken cancellationToken = default)`.
- `UniTask UIContext.ShutdownAsync()`; `void UIContext.Dispose()`; `UIContext : IDisposable`.
- `UIHandle` properties `Context`, `Id`, `DefinitionId`, `Parent`, `State`, `ViewObject`, `LifetimeToken`, `Opened`, `Closed`; methods `IDisposable RegisterCleanup(Action cleanup)` and `UniTask CloseAsync(CancellationToken cancellationToken = default)`.
- `UIHooks` callbacks: `Func<UIHandle, CancellationToken, UniTask> PrepareAsync`, `OpenAsync`, `CloseAsync`; `Action<UIHandle> Closed`.
- Enums: `UIState { Opening, Visible, Closing, Closed }`; `UIRole { Hud, Popup }`; `UIInputMode { Modeless, Modal }`; `UIRetention { DestroyOnClose, Reuse }`; `UIHideStrategy { DeactivateView, DisableCanvasRendering }`.

## Inputs / Outputs / Errors

UIContext construction with a null/destroyed root throws ArgumentNullException. Register(null) throws ArgumentNullException; blank/duplicate definition ID, invalid metadata, or selecting zero/two asset sources throws ArgumentException. A key without a configured provider throws InvalidOperationException. RegisterHost blank/duplicate ID throws ArgumentException; null/destroyed container throws ArgumentNullException; invalid thread or cleanup/native mutation boundary throws InvalidOperationException; termination throws ObjectDisposedException.

PrepareAsync unknown ID throws ArgumentException. Caller or owner cancellation throws OperationCanceledException. A destroyed direct source, provider result without a live prefab, or dead cached source throws InvalidOperationException; provider exceptions propagate. BeginOpen null request throws ArgumentNullException, unknown ID throws ArgumentException, Modal input is supported, invalid/non-visible/cross-context parent or non-HUD/parented HUD request throws InvalidOperationException, invalid mode throws ArgumentException, pre-cancelled request throws OperationCanceledException, and duplicate active/closing owner-definition pair or concurrent HUD selection throws InvalidOperationException.

Presentation validation throws InvalidOperationException for unknown/destroyed host, missing active/effective Canvas, any prefab Canvas with overrideSorting, renderer-only mode without a dedicated clone-root Canvas, descendant CanvasGroup.ignoreParentGroups, unsupported WorldSpace or camera-less ScreenSpaceCamera layout, incompatible render mode/camera/target display, equal separate-Canvas ordering, overlapping/incomparable hosts, or an ordering relation that cannot be represented.

Opening failure is reported through Opened after partial cleanup. If cleanup also fails, both are preserved in AggregateException. Close and Shutdown aggregate cleanup/native/observer errors after attempting remaining cleanup. Methods called after owner termination throw ObjectDisposedException where declared.

## Ownership / Lifecycle / Threading

Context owns clones/storage, handles, callback snapshots, per-generation cleanup, provider-key preparation coordination, current HUD selection, and at most one reusable candidate per definition. DeactivateView cached clones are inactive; renderer-only cached clones are active GameObjects under active rendering storage with Canvas/raycasters disabled and the owned wrapper mask closed. Root, source prefab, provider, hosts, and external services are borrowed. All Unity object operations/callbacks run on the main thread.

Opening resolves the source and takes a cached clone or instantiates under storage, then runs display-specific UIHooks.PrepareAsync. Fresh DeactivateView clones prepare inactive. Fresh DisableCanvasRendering clones are instantiated under inactive storage; for reuse, the renderer-only clone GameObject stays active under active rendering storage while its dedicated Canvas/raycasters and wrapper mask are disabled. Every managed display places the inner view under its owned input/visibility wrapper; the host parents that wrapper and UIHandle.ViewObject returns the inner view.

DestroyOnClose destroys the clone/presentation. Reuse retains at most one candidate per definition after successful visible close, cleanup, and Closed observer. Renderer-only cached candidates remain active GameObjects, masked by the owned CanvasGroup with Canvas/raycasters disabled; DeactivateView cached clones are inactive. Failed/cancelled opening, close/cleanup/native/observer failure discards the candidate.

## Concurrency / Cancellation / FailureCleanup

- Same-key provider requests share in-flight asset preparation. `PrepareAsync` cancellation is waiter-only; shared work follows context lifetime.
- Failed preparation entry is removed; retry requires a later explicit Prepare/Open call.
- Owner shutdown cancels waiters and prevents late provider publication without disposing the provider/source. The provider operation may still finish after shutdown.
- Opening request cancellation starts partial cleanup and is detached after Visible. `UIHandle.LifetimeToken` cancels when that generation begins termination.
- Renderer-only cached clone GameObjects remain active; storage activity and Canvas/raycaster/wrapper-mask state differ from DeactivateView cached clones.
- `CloseAsync` cancellation affects only the caller's wait; started cleanup continues. Cleanup runs once in reverse registration order; early registration disposal invokes that cleanup once immediately.
- Opening error is surfaced by `Opened`; if cleanup succeeds, it does not by itself fault `Closed`. If cleanup also fails, opening result preserves both errors. Closing/cleanup/native/observer failures are preserved after remaining cleanup attempts.
- Shutdown is idempotent, rejects new work, cancels owner/preparation waiters, closes accepted displays, destroys cached clones and storage, and shares completion. Shutdown waits for display retirement and cached clone destruction, not a borrowed provider's arbitrary late completion.
- `Dispose` starts fallback without awaiting asynchronous hooks. Root destruction also starts owner fallback. Await `ShutdownAsync` before unloading the root.

## Configuration / ExtensionPoints

Supported through P5: direct prefab or provider key; Hud or Popup role; registered host; Modeless or Modal input eligibility; accepted fixed Canvas topology; DeactivateView or constrained DisableCanvasRendering; DestroyOnClose or Reuse; fixed-height vertical one-column VirtualScrollRect. acquireModalBlock optionally supplies a UI modal lease factory. P6 installer/settings and P7 consumer/Player/Profiler/Canvas/UX gates remain open.

## RequiredSequence / ForbiddenUsage

1. Construct `UIContext` against a live persistent/scene root; supply `loadPrefab` when definitions use keys.
2. Register each definition with exactly one source.
3. Optionally call `UIContext.PrepareAsync` to prepare/resolve the asset. It does not create a clone.
4. Call BeginOpen and await Opened, or use OpenAsync. The display Prepare hook runs on the clone; fresh DeactivateView clones are inactive, while reused renderer-only clones are active but hidden by their owned wrapper.
5. Close the handle and await its shared result. Await `ShutdownAsync` before root/scene unload.

Do not destroy/dispose borrowed roots, source prefabs, provider or services; do not stop shared preparation when one waiter cancels; do not expect automatic retry; do not destroy `ViewObject`; do not retain failed/dirty clones; do not use a stale handle to affect a later generation; do not use `Dispose` as graceful asynchronous shutdown. Hooks must observe cancellation and stop touching a terminated clone. Synchronous self-close/self-await/shutdown is rejected; self-await after yielding remains forbidden even though the synchronous guard cannot reliably detect it. `UniTask.AsTask()` may report an `OperationCanceledException` as `TaskStatus.Faulted`; inspect the exception.

## Example / Compatibility / Limitations

Explanatory only; **NotRun** as a standalone example:

```csharp
var context = new UIContext(ownerRoot, loadPrefab: LoadPrefabAsync);
context.Register(new UIDefinition("settings", assetKey: "ui/settings",
    retention: UIRetention.Reuse));
await context.PrepareAsync("settings"); // asset only
var handle = context.BeginOpen(new UIOpenRequest("settings",
    hooks: new UIHooks { PrepareAsync = PrepareViewAsync }));
await handle.Opened;
await handle.CloseAsync();
await context.ShutdownAsync();
```

P3-focused tests passed EditMode 18/18 and PlayMode 30/30 on original Editor PID24376, failed0/skip0; compile and product Console errors0. Edit runId `303ed314e92e4c3cb12d9f935257816f`; Play CLI PID29644. The 48 focused tests include 18 P3 additions; not every added test had a separate Red run. Evidence: [P3 Edit](../../validation/ui-system/p3/final-edit.json), [native Edit result](../../validation/ui-system/p3/final-edit.native.json), [P3 Play](../../validation/ui-system/p3/final-play.json), [Console](../../validation/ui-system/p3/final-console.json), [validation record](../../validation/ui-system/p3/README.md). SourceRevision is `18666acae25b04c1c63ca49d8c00ca2ee67da308` (375 source files; protected six unchanged). UI remains development source outside released 0.0.1 packages/tag. P4 Modal/Input and P5 VirtualScrollRect are implemented in development source. P4 focused original-Editor validation: EditMode 24/24, PlayMode 41/41, failed 0/skip 0; [Edit](../../validation/ui-system/p4/final-edit.json) · [Play](../../validation/ui-system/p4/final-play.json). SourceRevision deb222cf6fda752d3e0dd6d22bda67f9d60e9e16. Consumer install, Player, Profiler, broad UX, and user acceptance remain P7 NotRun. See [UIContext design](../../GAME_UI_SYSTEM.md).


## P4 modal/input API and constraints

P4 public additions (source: ../../../Assets/TPLab/UI/Runtime; optional adapter source: ../../../Assets/TPLab/UI/InputSystem/Runtime):

- UIContext(GameObject rootObject, Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null, Func<IDisposable> acquireModalBlock = null, EventSystem eventSystem = null); EventSystem is an explicit borrowed reference.
- UIHandle.InputMode, CanReceiveInput; SetInputMode(UIInputMode), BringToFront(), SetFocus(GameObject), RequestCloseAsync(UIUserCloseReason reason, CancellationToken cancellationToken = default).
- UIHooks.CanCloseAsync: Func<UIHandle, UIUserCloseReason, CancellationToken, UniTask<bool>>. Reasons: Cancel, OutsidePointer, Button.
- Optional TPLab.UI.InputSystem.UIInputSystemAdapter: Bind(UIContext, InputManager, InputSystemUIInputModule, Guid uiMapId), AcquireModalBlock(string layerId), Unbind().

Behavior and ownership:

- SetInputMode requires a Visible handle. An Opening hook may set focus to a live descendant; UIContext remembers it and applies it when the generation becomes Visible and eligible. Visible focus requires the explicitly connected EventSystem and an active/interactable target owned by that view.
- Modal mode gates lower UI without Input System. Gameplay blocking occurs only when the project supplies the modal lease factory. CanReceiveInput reports logical Visible-generation eligibility, not native module/map state.
- User-close approval is opt-in. No handler or veto returns false; accepted close returns true after the shared close reaches Closed. Caller cancellation throws OperationCanceledException for that caller's approval/wait only. Forced CloseAsync and owner shutdown bypass veto and cancel pending approval. No backdrop or outside-click detector is created.
- Adapter preconditions: matching explicit EventSystem on the module; runtime-clone action asset and references from the selected UI map; pre-registered compatible mapless BlockLower layer. Keep Input references out of base TPLab.UI. EventSystem, InputManager, module, actions, and external leases are borrowed; the adapter owns only subscriptions and its recovery wrappers.
- Keep the lower UI blocked during modal hide/cleanup, raw pointer/touch/submit/cancel/move release, and through a later EventSystem frame. Closing middle modal B in A(modeless)-B(modal)-C(modeless) preserves C focus and does not release independent gameplay/transition leases.
- Unbind disables the borrowed module, refreshes layer policy, and detaches active modal wrappers; it does not return a live display's lease. Native application failure causes UIContext to retain the adapter-provided BlockAll lease through owner shutdown, even after Unbind. DisplayChanged listeners run synchronously in read-only dispatch; all are attempted. Listener failures propagate through the affected lifecycle result after cleanup/lease retirement. SetInputMode, BringToFront or SetFocus command failures throw from the command and do not poison later close. Ordinary listener errors do not set Fault. Arbitrary raw action subscribers are outside the native UI replay guarantee.
- Invalid mode/reason uses ArgumentException; invalid state/thread/reentry/competing requests use declared InvalidOperationException boundaries. Read XML on each public symbol for exact exception scope.

Final focused P4 original-Editor results: EditMode 24/24 and PlayMode 41/41, failed 0, skipped 0; product Console 0. Edit runId c4c9dd94c6b74ec9be545306d0bcc897; Play CLI PID 34520; Editor PID 24376. [Edit](../../validation/ui-system/p4/final-edit.json); [native Edit](../../validation/ui-system/p4/final-edit.native.json); [Play](../../validation/ui-system/p4/final-play.json); [source match](../../validation/ui-system/p4/source-commit-match.json). SourceRevision deb222cf6fda752d3e0dd6d22bda67f9d60e9e16. Consumer installation, Player, Profiler, broad UX, and user acceptance remain NotRun (P7). P5 final focused Edit31/31 and Play48/48 passed; P6 installer/settings remain subsequent work. [P5 Edit](../../validation/ui-system/p5/final-edit.json) | [P5 Play](../../validation/ui-system/p5/final-play.json).

## P3 hosts, selection, presentation, and constraints

P3 adds UIContext.RegisterHost(string id, Transform container), UIContext.CurrentHud, and UIContext.SelectHudAsync(UIOpenRequest request, CancellationToken cancellationToken = default). RegisterHost borrows a live Transform and adds a unique host ID; it does not create or own Canvas state. Blank/duplicate IDs throw ArgumentException; null/destroyed container throws ArgumentNullException; invalid mutation/thread boundary throws InvalidOperationException; terminated owner throws ObjectDisposedException.

CurrentHud is the selected live HUD, or null after retirement and after context shutdown. SelectHudAsync requires a parentless HUD request and rejects non-HUD/parented input or concurrent selection with InvalidOperationException. It prepares the candidate clone and invokes its display Prepare hook before closing the old HUD subtree; fresh DeactivateView clones are inactive, while reused renderer-only clones are active but hidden by their wrapper mask. Candidate preparation failure/cancellation leaves old HUD and children untouched. Once old close starts, it is not rolled back; child displays close first, errors remain observable, and failed replacement candidate cleanup errors are preserved. CurrentHud is assigned only after the candidate reaches Visible. Independent context-owned Popup displays are not closed by HUD replacement.

Popup Parent is an immutable logical relationship, separate from physical host placement. Parent and ancestors must be Visible and belong to the same context; invalid relationship/state throws InvalidOperationException. One owner/definition pair cannot be active or closing twice. Descendants close before parent. Request acceptance order determines managed order, independent of provider/hook completion. Host Transforms, Canvas, sorting setup, and borrowed siblings are borrowed. Same-Canvas managed ordering exchanges only managed sibling slots and preserves borrowed siblings' absolute indices. Fixed host/sorting arrangements place parent below child in ascending sibling/sorting order; child-first is the close order only. An unrepresentable accepted visual order throws InvalidOperationException.

All prefab Canvas components with overrideSorting=true are rejected. Graphics need an active host and effective Canvas. WorldSpace roots and ScreenSpaceCamera roots without an assigned camera are rejected. Managed planes must share render mode, camera, and target display. Separate Canvas planes need unambiguous sorting layer/order; overlapping or incomparable host trees are rejected.

DisableCanvasRendering requires a dedicated Canvas on the clone root and rejects any descendant CanvasGroup with ignoreParentGroups=true. An owned RectTransform wrapper with an owned CanvasGroup masks rendering/raycasts and preserves all project CanvasGroup values. The physical host parents the wrapper, while UIHandle.ViewObject returns the inner view, whose Transform parent is that wrapper. Hiding sets wrapper alpha=0, blocksRaycasts=false, interactable=false and disables the owned Canvas/captured GraphicRaycasters; it never disables a shared host Canvas.

Fresh clones are created under inactive storage before display preparation. DeactivateView close deactivates the clone. Renderer-only presentation uses an active GameObject with its Canvas/raycasters and wrapper mask controlling visibility. A reused renderer-only clone remains active under active rendering storage while its Canvas and captured GraphicRaycasters are disabled and wrapper mask is closed. Re-show restores captured native enablement and opens the mask. Therefore a cached renderer-only clone is not an inactive GameObject. Before each generation, Transform and RectTransform anchors, pivot, size, and position are restored from the prefab; moving the presentation root preserves anchored position. Reuse remains one clone per definition and is published only after successful close, cleanup, and observer; failures discard it.

P4 Modal/Input and P5 VirtualScrollRect are implemented in development source. Explanatory examples remain NotRun; P7 consumer, Player, Profiler, broad UX, and user-acceptance gates remain NotRun.

## P5 fixed-height virtual list

`VirtualScrollRect` uses a borrowed vertical `ScrollRect`, its direct viewport/content, and one borrowed fixed-height prefab. It owns its clones, per-binding tokens, inactive staging storage, and its own native listener. The supported topology is one column, finite positive height, finite non-negative spacing, top-stretch content/cell roots with pivot `(0.5, 1)` and identity transforms, no horizontal padding, and no `LayoutGroup` or `ContentSizeFitter` on those roots.

- `Configure(ScrollRect, RectTransform, float, Action<VirtualCellBinding>, Action<VirtualCellBinding> = null, int overscan = 2, float spacing = 0)` validates before replacing state. A successful Configure retires old cells and resets Count to zero; explicitly set the new count. Borrowed ScrollRect/prefab and unrelated listeners are preserved.
- `SetCount(int)` updates logical length and extent, then clamps content. Extent is `count * height + max(0, count - 1) * spacing`, zero when empty. `ScrollToIndex(0)` resets an empty list; other indexes must be valid and jumps stop inertia. Native drag, elasticity, scrollbar, and inertia remain ScrollRect-owned.
- `Refresh()` explicitly replaces visible binding generations after data changes. Automatic scroll/viewport reconciliation preserves unchanged in-range bindings and avoids idle rebinding.
- `CountActive`, `CountInactive`, `CountOwned`, `TotalCreated`, and `TotalDestroyed` report settled state. The viewport cell budget is `ceil(viewportHeight / (height + spacing)) + 1 + 2 * overscan`. Reconciliation pre-creates at most `min(Count, budget)` cells, but retained cells may remain up to the viewport budget after Count shrinks, including Count zero. Count zero gives CountActive zero; CountInactive and CountOwned may still be nonzero. Viewport shrink trims excess clones. Warmed viewport stability is functional bounded-ownership evidence, not a performance measurement.
- Bind/native reconciliation failures clean partial generations and surface an aggregate error. Automatic retry stops until explicit `SetCount`, `Refresh`, or `ScrollToIndex`. Unbind, failure, disable, and destruction invalidate/cancel generations and attempt remaining cleanup. Do not structurally reenter this component from bind/unbind.
- `VirtualCellBinding.Index`, `Generation`, and cached `LifetimeToken` remain readable after cancellation-source disposal. `View` and `IsCurrent` require main-thread access. Async project code must return to main thread and check both token and `IsCurrent` before touching a recycled view. Data and subscriptions remain project-owned.

P5 first Green passed Edit31/31 and Play48/48. Final UI79 passed Edit31/31 and Play48/48, failed0/skip0, with product Console empty. [Final Edit](../../validation/ui-system/p5/final-edit.json) | [Final Play](../../validation/ui-system/p5/final-play.json). The Play connector result was recovered after a CLI timeout without rerunning the test. SourceRevision `8f8abd290d540dc0b6ba30c5acc8b36b271bc302`; final snapshot verified 405 non-protected source matches and six protected files unchanged. Fourteen virtual-list tests include synthetic PointerEventData dispatch, not physical-device/Input System UX. P7 consumer, Player, Profiler/performance, Canvas comparison, broad UX, and user acceptance remain NotRun.
