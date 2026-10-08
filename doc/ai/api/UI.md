# UI API Reference

## Module / Namespace / Assembly

`TPLab.UI` / `TPLab.UI` / runtime source [Assets/TPLab/UI/Runtime](../../../Assets/TPLab/UI/Runtime). Runtime assembly references `TPLab.Core`, `UniTask`, and `UnityEngine.UI`. Confirmed project environment: Unity 6000.3.18f1, UniTask 2.5.11, uGUI 2.0.0.

## SourceRevision / SourcePath / HumanContract

SourceRevision: `545c342f80061c66ede2fc7f168cfc1c2cb13cee`. Source paths: [UIContext](../../../Assets/TPLab/UI/Runtime/UIContext.cs), [UIHandle](../../../Assets/TPLab/UI/Runtime/UIHandle.cs), [definition](../../../Assets/TPLab/UI/Runtime/UIDefinition.cs), [request](../../../Assets/TPLab/UI/Runtime/UIOpenRequest.cs), [hooks](../../../Assets/TPLab/UI/Runtime/UIHooks.cs), [enums](../../../Assets/TPLab/UI/Runtime/UIEnums.cs). Human contract: [doc/api/UI.md](../../api/UI.md); design contract: [GAME_UI_SYSTEM.md](../../GAME_UI_SYSTEM.md).

## ImplementationStatus / ValidationStatus / Evidence

ImplementationStatus: **Implemented (P1 scope)**. ValidationStatus: **Partial**. Original Unity Editor PID 24376, Unity 6000.3.18f1: UI EditMode 9/9 and PlayMode 9/9, skip 0; existing `SceneRootTests` PlayMode regression 10/10, skip 0; compile errors 0. The two expected SceneRoot fixture Console errors were asserted by `LogAssert.Expect`; unexpected errors 0. Evidence: [Edit](../../validation/ui-system/p1/refactor-edit.json), [Play](../../validation/ui-system/p1/refactor-play.json), [root regression](../../validation/ui-system/p1/root-regression-play.json), [expected Console errors](../../validation/ui-system/p1/console-expected-errors.json). Consumer install, Player, Profiler, broad UX, and user acceptance are P7 **NotRun**.

## Symbol / Signature / Constraints

- `UIDefinition(string id, GameObject prefab = null, string assetKey = null, UIRole role = UIRole.Popup, string hostId = "default", UIInputMode inputMode = UIInputMode.Modeless, UIRetention retention = UIRetention.DestroyOnClose, UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)`. Properties: `Id`, `Prefab`, `AssetKey`, `Role`, `HostId`, `InputMode`, `Retention`, `HideStrategy`.
- `UIOpenRequest(string definitionId, UIHandle parent = null, UIInputMode? inputMode = null, UIHooks hooks = null)`. Properties: `DefinitionId`, `Parent`, `InputMode`, `Hooks`.
- `UIContext(GameObject rootObject, Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null, Func<IDisposable> acquireModalBlock = null)`; properties `RootObject`, `LifetimeToken`, `IsDisposed`, `Fault`, `Displays`.
- `void UIContext.Register(UIDefinition definition)`; `UniTask UIContext.PrepareAsync(string definitionId, CancellationToken cancellationToken = default)`.
- `UIHandle UIContext.BeginOpen(UIOpenRequest request, CancellationToken cancellationToken = default)`; `UniTask<UIHandle> UIContext.OpenAsync(UIOpenRequest request, CancellationToken cancellationToken = default)`.
- `UniTask UIContext.ShutdownAsync()`; `void UIContext.Dispose()`; `UIContext : IDisposable`.
- `UIHandle` properties: `Context`, `Id`, `DefinitionId`, `Parent`, `State`, `ViewObject`, `LifetimeToken`, `Opened`, `Closed`; methods `IDisposable RegisterCleanup(Action cleanup)` and `UniTask CloseAsync(CancellationToken cancellationToken = default)`.
- `UIHooks`: settable `Func<UIHandle, CancellationToken, UniTask> PrepareAsync`, `OpenAsync`, `CloseAsync`; `Action<UIHandle> Closed`.
- Enums: `UIState { Opening, Visible, Closing, Closed }`; `UIRole { Hud, Popup }`; `UIInputMode { Modeless, Modal }`; `UIRetention { DestroyOnClose, Reuse }`; `UIHideStrategy { DeactivateView, DisableCanvasRendering }`.

## Inputs / Outputs / Errors

`Register` requires nonempty unique ID and exactly one live direct prefab or nonempty key; metadata must use a defined policy. P1 rejects key/provider and `Reuse` with `NotSupportedException` (P2), HUD/explicit host with `NotSupportedException` (P3), Modal/non-default hide strategy with `NotSupportedException` (P4). `BeginOpen` rejects non-null parent (`NotSupportedException`, P3), invalid input enum (`ArgumentException`), Modal (`NotSupportedException`, P4), pre-canceled token (`OperationCanceledException`), and same-owner duplicate definition (`InvalidOperationException`). Unknown definition is `ArgumentException`; destroyed borrowed prefab is `InvalidOperationException`.

`PrepareAsync` validates direct prefab readiness only; no instantiate/warm-up. `BeginOpen` returns a generation immediately and publishes `Opened` after lifecycle hooks. `OpenAsync` awaits and returns the same handle. Opening failure/cancellation is observable through `Opened` after partial cleanup. `Closed` represents cleanup/observer/native failures; open failure alone does not fault it. Cleanup failures are aggregated after remaining cleanup attempts. Root null/destroyed at construction throws `ArgumentNullException`; wrong thread, prohibited self-reentry, or lifecycle mutation during cleanup/native application throws `InvalidOperationException`; operations after termination throw `ObjectDisposedException` where declared.

## Ownership / Lifecycle / Threading

Connect context → register → optional prepare → begin/open → await `Opened` → close → await `ShutdownAsync` before root/scene unload. Context owns display clones/storage, per-display hook snapshot, cleanup registrations, and lifetime. Root, direct prefab source, provider, input services, and lease factory are borrowed and are never destroyed/disposed by Context. Instantiation is under inactive storage; `PrepareAsync` hook runs inactive; clone is then parented to borrowed root and activated before `OpenAsync` hook. Hooks must observe cancellation and stop touching their clone; cancellation cannot forcibly stop project code. `Closed` is invoked after native clone destruction, state becomes Closed and `ViewObject` clears, then the shared `Closed` task completes. All APIs/callbacks require Unity main thread.

## Concurrency / Cancellation / FailureCleanup

`BeginOpen`/`OpenAsync` caller cancellation stops opening and initiates that display's cleanup; registration detaches once Visible. `UIHandle.LifetimeToken` cancels at termination start. Prepare caller cancellation affects that request only. `CloseAsync` cancellation affects only that caller's wait; close cleanup continues. `ShutdownAsync` shares completion and cancels owner/pending work; it does not own cancellation of external asset/input services. `Dispose` starts synchronous fallback without awaiting async hooks. Cleanup callbacks run once in reverse registration order; early registration Dispose runs its action immediately once. `Closed` observer errors join cleanup errors. Synchronous self-close/self-await/shutdown is rejected; after a hook genuinely yields, self-await cycles and attempts to shut down its own context are forbidden and not reliably detected. Independent display composition is allowed. Avoid assuming `UniTask.AsTask()` maps an `OperationCanceledException` to `TaskStatus.Canceled`; this bridge may report `Faulted` with the cancellation exception.

## Configuration / ExtensionPoints

Supported P1 configuration: one borrowed direct prefab, role `Popup`, host ID `default`, `Modeless`, `DestroyOnClose`, `DeactivateView`. Constructor `loadPrefab` and `acquireModalBlock` are reserved but never invoked by P1. `UIHooks` values are copied at request acceptance and callbacks remain project-owned. Provider implementation/cache, reuse, HUD, parent tree, host/Canvas registration/order, Modal/Input adapter, renderer-only hide, and Virtual ScrollRect remain unimplemented.

## RequiredSequence / ForbiddenUsage

1. Construct against the actual persistent/scene root.
2. `Register` definitions using the supported direct-Prefab P1 policy.
3. Optionally call `PrepareAsync`; it does not instantiate.
4. Call `BeginOpen` and observe `handle.Opened`, or call `OpenAsync` for the same path.
5. Register precise synchronous cleanup; close the handle and await `ShutdownAsync` before root destruction.

Do not dispose borrowed root/prefab/provider/services, destroy `ViewObject`, treat enum availability as feature support, register async-void cleanup, self-await after yield, or use `Dispose` as graceful async shutdown. P1 is development source only and is not in released 0.0.1 packages/tag.

## Example / Compatibility / Limitations

```csharp
var context = new UIContext(ownerRoot);
context.Register(new UIDefinition("settings", prefab: settingsPrefab));
var handle = context.BeginOpen(new UIOpenRequest("settings"));
await handle.Opened;
await handle.CloseAsync();
await context.ShutdownAsync();
```

Explanatory only; **NotRun** as a standalone example. P1 lifecycle tests do not verify this exact snippet, consumer installation, Player, Profiler, broad UX, or user acceptance. No migration compatibility, new package version, or support beyond the stated Unity/package environment is claimed.
