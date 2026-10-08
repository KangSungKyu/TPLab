# UIContext API

`TPLab.UI` exposes a root-owned display lifecycle for direct prefab Popup views. **ImplementationStatus: Implemented (P1 scope). ValidationStatus: Partial.** SourceRevision: `545c342f80061c66ede2fc7f168cfc1c2cb13cee`. Source is in [Assets/TPLab/UI/Runtime](../../Assets/TPLab/UI/Runtime). Reference environment: Unity 6000.3.18f1, UniTask 2.5.11, uGUI 2.0.0.

The exact P1 lifecycle tests passed in the original Editor: EditMode 9/9, PlayMode 9/9, and existing SceneRoot PlayMode regression 10/10; all had skip 0. Compile errors were 0. The two expected SceneRoot fixture Console errors were asserted with `LogAssert.Expect`; unexpected errors were 0. Evidence: [EditMode](../validation/ui-system/p1/refactor-edit.json), [PlayMode](../validation/ui-system/p1/refactor-play.json), [SceneRoot regression](../validation/ui-system/p1/root-regression-play.json), [expected Console errors](../validation/ui-system/p1/console-expected-errors.json). Consumer installation, Player, Profiler, broader UX, and user acceptance remain **NotRun**; those are P7 gates. UI is development source and is not part of the released 0.0.1 Core/Input/Editor packages or tag.

## Responsibility and ownership

`UIContext` owns accepted display generations, their instantiated clones, callback snapshots, registered cleanup, and root-linked lifetime. The root GameObject, source prefab, optional provider, input services, and lease factory are borrowed. Shutdown destroys only context-owned clones/storage and never destroys or disposes borrowed objects. Connect it to the real persistent or scene owner and await `ShutdownAsync()` before unloading that root; `Dispose()` is a synchronous fallback and cannot await project callbacks.

All operations and callbacks are Unity-main-thread work. P1 supports a direct live prefab, `Popup`, host `"default"`, `Modeless`, `DestroyOnClose`, and `DeactivateView`. Provider keys, `Reuse`, HUD role, explicit hosts, parent trees, Modal input, and `DisableCanvasRendering` are declared but currently rejected with `NotSupportedException` for their later phases.

## Public declarations

```csharp
public sealed class UIContext : IDisposable
public UIContext(GameObject rootObject,
    Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
    Func<IDisposable> acquireModalBlock = null)
public void Register(UIDefinition definition)
public UniTask PrepareAsync(string definitionId, CancellationToken cancellationToken = default)
public UIHandle BeginOpen(UIOpenRequest request, CancellationToken cancellationToken = default)
public UniTask<UIHandle> OpenAsync(UIOpenRequest request, CancellationToken cancellationToken = default)
public UniTask ShutdownAsync()
public void Dispose()

public sealed class UIDefinition
public UIDefinition(string id, GameObject prefab = null, string assetKey = null,
    UIRole role = UIRole.Popup, string hostId = "default",
    UIInputMode inputMode = UIInputMode.Modeless,
    UIRetention retention = UIRetention.DestroyOnClose,
    UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)

public sealed class UIOpenRequest
public UIOpenRequest(string definitionId, UIHandle parent = null,
    UIInputMode? inputMode = null, UIHooks hooks = null)

public sealed class UIHandle
public UniTask Opened { get; }
public UniTask Closed { get; }
public IDisposable RegisterCleanup(Action cleanup)
public UniTask CloseAsync(CancellationToken cancellationToken = default)
```

`UIContext` also exposes `RootObject`, `LifetimeToken`, `IsDisposed`, `Fault`, and a detached read-only `Displays` snapshot. `UIHandle` exposes `Context`, `Id`, `DefinitionId`, `Parent`, `State`, `ViewObject`, and cached `LifetimeToken`. `UIHooks` has settable `PrepareAsync`, `OpenAsync`, `CloseAsync` delegates of `Func<UIHandle, CancellationToken, UniTask>` and a `Closed` delegate of `Action<UIHandle>`. Request hooks are copied when the request is accepted.

Enums: `UIState` (`Opening`, `Visible`, `Closing`, `Closed`), `UIRole` (`Hud`, `Popup`), `UIInputMode` (`Modeless`, `Modal`), `UIRetention` (`DestroyOnClose`, `Reuse`), `UIHideStrategy` (`DeactivateView`, `DisableCanvasRendering`). Enum values describe future policies too; their presence does not mean P1 implements them.

## Registration and opening

`UIDefinition` requires exactly one live direct `prefab` or non-empty explicit `assetKey`. Its `id` must be non-empty and unique within a context. Registration validates metadata but neither loads nor instantiates anything. For P1 use `prefab`, `Popup`, `hostId: "default"`, `Modeless`, `DestroyOnClose`, and `DeactivateView`. Unsupported future policies fail explicitly instead of silently falling back.

`PrepareAsync` currently verifies that the registered direct prefab remains alive; it does not instantiate or warm a clone. The caller token cancels this request. The constructor's `loadPrefab` and `acquireModalBlock` delegates are reserved and are not invoked by supported P1 operations.

`BeginOpen` validates the definition, reserves a new context-local handle ID, rejects an already opening/visible/closing display with the same definition, snapshots hooks, connects the opening cancellation token, and starts the lifecycle. It returns the generation handle before its hooks finish. The clone is instantiated under an inactive context-owned storage object, `PrepareAsync` hook runs while inactive, then the clone is parented to the borrowed root and activated before the `OpenAsync` hook. `Opened` completes only after both hooks complete. `OpenAsync` is a convenience wrapper over `BeginOpen` and returns that same handle after `Opened` succeeds. A callback may compose an independent Popup operation; it must not await or synchronously reenter its own display operation.

`UIOpenRequest.parent` is reserved for P3 and non-null values are rejected in P1. P1 also rejects Modal requests even when an `acquireModalBlock` delegate was supplied. Same-context independent Popup hooks may open/close another Popup. A callback cannot synchronously close or await its own handle, shut down its own context, or mutate lifecycle state during cleanup/native state application. The dispatch guard is synchronous: after a hook genuinely yields, self-await cycles and attempts to shut down that hook's own context are not reliably detected and remain forbidden. These restrictions do not lock out ordinary operations for the full duration of an awaited hook.

## Cancellation, cleanup, and shutdown

The `BeginOpen`/`OpenAsync` cancellation token cancels an opening display and starts partial cleanup; it is detached after the display becomes `Visible`. `UIHandle.LifetimeToken` is cached and canceled when that display starts closing. Hooks must observe their token and stop touching the clone after cancellation; cancellation does not forcibly stop arbitrary project code. `RegisterCleanup(Action)` registers synchronous, display-owned subscription/resource cleanup; remaining registrations run once in reverse order. Disposing its returned registration executes it immediately and exactly once. If registration is rejected after termination starts, the caller remains responsible for the resource it had not registered. Never register disposal of the borrowed root, prefab asset, ResourceManager, InputManager, or other borrowed service.

`CloseAsync` starts non-vetoable close and returns the generation's shared cleanup completion; its token cancels only this caller's wait. The sequence awaits a visible display's `CloseAsync` hook (unless owner shutdown has canceled that phase), runs cleanup, deactivates and destroys the clone, then sets `State` to `Closed` and clears `ViewObject`. The `Closed` observer then runs; only after it returns does `Closed` complete. Hook, cleanup, native destruction, and observer errors are preserved; remaining cleanup is attempted. Opening failure is reported by `Opened`; it does not by itself fault `Closed` when cleanup succeeds.

`ShutdownAsync` rejects new operations, cancels the owner lifetime and pending opens, closes all displays, destroys context storage, and shares one completion across repeated calls. It does not cancel cleanup because an awaiting caller was canceled. Native root destruction triggers this owner fallback. `Dispose` starts the same fallback without awaiting callbacks; callers that need graceful completion or deferred errors should await `ShutdownAsync`.

When adapting a `UniTask` to `Task`, note that cancellation thrown through `UniTask.AsTask()` may be represented as `TaskStatus.Faulted` with an `OperationCanceledException`; inspect the exception when bridging APIs rather than assuming `TaskStatus.Canceled`.

## Example and limits

The following is an explanatory example, **NotRun**; the P1 tests verify the underlying lifecycle contracts, not this exact snippet.

```csharp
var context = new UIContext(ownerRoot);
context.Register(new UIDefinition("settings", prefab: settingsPrefab));

var handle = context.BeginOpen(new UIOpenRequest("settings"));
try
{
    await handle.Opened;
    // Project-owned interaction.
}
finally
{
    try
    {
        await handle.CloseAsync();
    }
    finally
    {
        await context.ShutdownAsync();
    }
}
```

Provider loading/cache, reuse, HUD selection, parent trees, host/Canvas ordering, Modal/input behavior, and Virtual ScrollRect are not implemented in P1. No consumer project, Player, performance, broad UX, or user acceptance claim is made. See [UIContext contract](../GAME_UI_SYSTEM.md) and [UI implementation track](../GAME_UI_SYSTEM_TRACK.md) for later phases.
