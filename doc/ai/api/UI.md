# UI API Reference

## Module / Namespace / Assembly

`TPLab.UI` / `TPLab.UI` / [runtime source](../../../Assets/TPLab/UI/Runtime). Runtime assembly references `TPLab.Core`, `UniTask`, and `UnityEngine.UI`. Confirmed environment: Unity 6000.3.18f1, UniTask 2.5.11, uGUI 2.0.0.

## SourceRevision / SourcePath / HumanContract

SourceRevision: `cea4268b82e0f119e0d4dbc8c788b7632d21f3ee`. Sources: [UIContext](../../../Assets/TPLab/UI/Runtime/UIContext.cs), [UIHandle](../../../Assets/TPLab/UI/Runtime/UIHandle.cs), [UIDefinition](../../../Assets/TPLab/UI/Runtime/UIDefinition.cs), [UIOpenRequest](../../../Assets/TPLab/UI/Runtime/UIOpenRequest.cs), [UIHooks](../../../Assets/TPLab/UI/Runtime/UIHooks.cs), [enums](../../../Assets/TPLab/UI/Runtime/UIEnums.cs). Human contract: [doc/api/UI.md](../../api/UI.md). Design contract: [GAME_UI_SYSTEM.md](../../GAME_UI_SYSTEM.md).

## ImplementationStatus / ValidationStatus / Evidence

ImplementationStatus: **Implemented (P1/P2 scope)**. ValidationStatus: **Partial**. On original Unity Editor PID 24376, combined P2 and P1 lifecycle suite passed: EditMode 14/14 and PlayMode 16/16, failed 0, skipped 0; compile errors 0 and product Console errors 0. These totals include 12 P2 tests and 18 P1 regression tests. Evidence: [EditMode](../../validation/ui-system/p2/final-edit.json), [PlayMode](../../validation/ui-system/p2/final-play.json). An earlier CLI invocation timed out with no result; it is not counted as a pass or test failure: [transport record](../../validation/ui-system/p2/transport-rejected.json). Consumer installation, Player, Profiler, broad UX, and user acceptance remain P7 **NotRun**. UI is development source, separate from released 0.0.1 Core/Input/Editor packages/tag; no UI release version is set.

## Symbol / Signature / Constraints

P2 changes no public signature:

- `UIDefinition(string id, GameObject prefab = null, string assetKey = null, UIRole role = UIRole.Popup, string hostId = "default", UIInputMode inputMode = UIInputMode.Modeless, UIRetention retention = UIRetention.DestroyOnClose, UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)`; properties `Id`, `Prefab`, `AssetKey`, `Role`, `HostId`, `InputMode`, `Retention`, `HideStrategy`.
- `UIOpenRequest(string definitionId, UIHandle parent = null, UIInputMode? inputMode = null, UIHooks hooks = null)`; properties `DefinitionId`, `Parent`, `InputMode`, `Hooks`.
- `UIContext(GameObject rootObject, Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null, Func<IDisposable> acquireModalBlock = null)`; properties `RootObject`, `LifetimeToken`, `IsDisposed`, `Fault`, `Displays`.
- `void UIContext.Register(UIDefinition definition)`; `UniTask UIContext.PrepareAsync(string definitionId, CancellationToken cancellationToken = default)`.
- `UIHandle UIContext.BeginOpen(UIOpenRequest request, CancellationToken cancellationToken = default)`; `UniTask<UIHandle> UIContext.OpenAsync(UIOpenRequest request, CancellationToken cancellationToken = default)`.
- `UniTask UIContext.ShutdownAsync()`; `void UIContext.Dispose()`; `UIContext : IDisposable`.
- `UIHandle` properties `Context`, `Id`, `DefinitionId`, `Parent`, `State`, `ViewObject`, `LifetimeToken`, `Opened`, `Closed`; methods `IDisposable RegisterCleanup(Action cleanup)` and `UniTask CloseAsync(CancellationToken cancellationToken = default)`.
- `UIHooks` callbacks: `Func<UIHandle, CancellationToken, UniTask> PrepareAsync`, `OpenAsync`, `CloseAsync`; `Action<UIHandle> Closed`.
- Enums: `UIState { Opening, Visible, Closing, Closed }`; `UIRole { Hud, Popup }`; `UIInputMode { Modeless, Modal }`; `UIRetention { DestroyOnClose, Reuse }`; `UIHideStrategy { DeactivateView, DisableCanvasRendering }`.

## Inputs / Outputs / Errors

`UIContext` construction with null/destroyed root throws `ArgumentNullException`. `Register(null)` throws `ArgumentNullException`; empty/duplicate ID, invalid metadata, or not exactly one live direct prefab/nonempty key throws `ArgumentException`; key without configured provider throws `InvalidOperationException`; unsupported HUD/host/renderer-only policy (P3) or Modal policy (P4) throws `NotSupportedException`. `PrepareAsync` unknown ID throws `ArgumentException`; caller/owner cancellation throws `OperationCanceledException`; destroyed direct source, provider returning no live prefab, or no-longer-live resolved source throws `InvalidOperationException`; provider exceptions propagate. `BeginOpen` null request throws `ArgumentNullException`, unknown ID throws `ArgumentException`, non-null parent and Modal mode throw `NotSupportedException`, invalid mode throws `ArgumentException`, pre-cancelled token throws `OperationCanceledException`, and duplicate active/closing definition throws `InvalidOperationException`. Calls after owner shutdown throw `ObjectDisposedException` where declared. Opening failure is reported through `Opened` after partial cleanup; if cleanup fails too, both errors are aggregated. Close/Shutdown cleanup errors are aggregated after remaining cleanup is attempted.

`UIContext.PrepareAsync` is optional **asset preparation only**. For a direct prefab it verifies the borrowed source is alive; for a key it resolves a borrowed prefab through the provider. It does not instantiate/warm a clone. Same-key provider loads share one context preparation, and a successful borrowed prefab remains cached per key until owner shutdown. Caller cancellation cancels only that caller's wait; owner shutdown cancels waiters. Failed shared preparation is removed and retries only on a later explicit request. A late provider result after owner shutdown is ignored, not displayed/cached; neither provider nor borrowed source is destroyed/released.

`BeginOpen` returns a generation immediately. It rejects a non-null parent (P3), Modal input (P4), a pre-cancelled request (`OperationCanceledException`), and duplicate active/closing definition (`InvalidOperationException`). `Opened` reports opening success/failure after partial cleanup. `OpenAsync` is the same lifecycle path and returns the same handle after `Opened` succeeds.

## Ownership / Lifecycle / Threading

Context owns clones/storage, handles, callback snapshots, per-generation cleanup, provider-key preparation coordination, and at most one inactive reused clone per definition. Root, source prefab, provider, and external services are borrowed. All Unity object operations/callbacks run on the main thread.

Opening resolves the source, takes a cached clone or instantiates under inactive context storage, runs request `UIHooks.PrepareAsync` while the clone is inactive, reparents to the borrowed root and activates it, then runs `UIHooks.OpenAsync`. The display `PrepareAsync` hook is not `UIContext.PrepareAsync`: the latter only prepares/resolves an asset and never creates a clone.

`DestroyOnClose` destroys the clone. `Reuse` retains a clone only after a successful visible close, cleanup, and `Closed` observer. Every display gets a fresh handle/token/cleanup generation. Opening failure/cancellation and any close/cleanup/native/observer failure discard the clone. `Closed` runs after native retirement and `State == Closed`/`ViewObject == null`; its result completes only after observer and cache/discard processing. A new generation opened by the observer is independent of retirement for the previous generation.

## Concurrency / Cancellation / FailureCleanup

- Same-key provider requests share in-flight asset preparation. `PrepareAsync` cancellation is waiter-only; shared work follows context lifetime.
- Failed preparation entry is removed; retry requires a later explicit Prepare/Open call.
- Owner shutdown cancels waiters and prevents late provider publication without disposing the provider/source. The provider operation may still finish after shutdown.
- Opening request cancellation starts partial cleanup and is detached after Visible. `UIHandle.LifetimeToken` cancels when that generation begins termination.
- `CloseAsync` cancellation affects only the caller's wait; started cleanup continues. Cleanup runs once in reverse registration order; early registration disposal invokes that cleanup once immediately.
- Opening error is surfaced by `Opened`; if cleanup succeeds, it does not by itself fault `Closed`. If cleanup also fails, opening result preserves both errors. Closing/cleanup/native/observer failures are preserved after remaining cleanup attempts.
- Shutdown is idempotent, rejects new work, cancels owner/preparation waiters, closes accepted displays, destroys cached clones and storage, and shares completion. Shutdown waits for display retirement and cached clone destruction, not a borrowed provider's arbitrary late completion.
- `Dispose` starts fallback without awaiting asynchronous hooks. Root destruction also starts owner fallback. Await `ShutdownAsync` before unloading the root.

## Configuration / ExtensionPoints

Supported P2 display configuration: direct prefab or provider key; role `Popup`; host `default`; `Modeless`; `DeactivateView`; `DestroyOnClose` or `Reuse`. Provider is supplied through the existing constructor delegate. `acquireModalBlock` remains reserved for P4. HUD selection, parent tree, explicit host/Canvas order (P3), renderer-only hide (P3) and Modal/Input behavior (P4), and Virtual ScrollRect (P5) remain deferred and rejected where applicable. P2 does not add factory, provider, installation, or package APIs.

## RequiredSequence / ForbiddenUsage

1. Construct `UIContext` against a live persistent/scene root; supply `loadPrefab` when definitions use keys.
2. Register each definition with exactly one source.
3. Optionally call `UIContext.PrepareAsync` to prepare/resolve the asset. It does not create a clone.
4. Call `BeginOpen` and await `Opened`, or use `OpenAsync`. Display-specific inactive clone/data preparation runs in request `UIHooks.PrepareAsync`.
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

P2 does not change the released 0.0.1 packages/tag. Consumer installation, Player, Profiler, broad UX, and user acceptance are not established by the focused suite. See [UIContext design](../../GAME_UI_SYSTEM.md), [P2 EditMode](../../validation/ui-system/p2/final-edit.json), and [P2 PlayMode](../../validation/ui-system/p2/final-play.json).
