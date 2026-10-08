# UIContext API

`TPLab.UI` provides a root-owned lifecycle for direct- or provider-keyed Popup views. **ImplementationStatus: Implemented (P1/P2 scope). ValidationStatus: Partial.** SourceRevision: `cea4268b82e0f119e0d4dbc8c788b7632d21f3ee`. Runtime source: [Assets/TPLab/UI/Runtime](../../Assets/TPLab/UI/Runtime). Confirmed environment: Unity 6000.3.18f1, UniTask 2.5.11, uGUI 2.0.0.

The original Editor (PID 24376) completed the combined P2 and P1 lifecycle suite: EditMode 14/14 and PlayMode 16/16, failed 0, skipped 0, compile errors 0, product Console errors 0. The totals include 12 P2 tests and 18 P1 regression tests. Evidence: [EditMode](../validation/ui-system/p2/final-edit.json), [PlayMode](../validation/ui-system/p2/final-play.json). One earlier CLI invocation timed out without a test result; it is recorded separately and is not counted as a pass or test failure ([transport record](../validation/ui-system/p2/transport-rejected.json)). Consumer installation, Player, Profiler, broad UX and user acceptance remain **NotRun** (P7). UI remains development source and is not part of released 0.0.1 Core/Input/Editor packages or tag; no new UI package version is set.

## Responsibility and ownership

`UIContext` owns accepted display generations, instantiated clones, one optional reusable clone per definition, shared provider-key preparation coordination, cleanup registrations, and root-linked lifetime. The root, direct prefab source, provider, and external services are borrowed; context shutdown never destroys or disposes them. Connect the context to the actual persistent or scene root and await `ShutdownAsync()` before unloading it. `Dispose()` begins the same fallback but cannot await asynchronous hooks.

All Unity object operations and callbacks run on Unity's main thread. The supported P2 display policy is `Popup`, host `"default"`, `Modeless`, and `DeactivateView`, with either `DestroyOnClose` or `Reuse`. HUD selection, logical parents, explicit hosts/Canvas ordering, Modal/Input integration, and renderer-only hiding are not supported yet and are rejected. Virtual ScrollRect is also deferred to P5.

## Public declarations

P2 does not change the P1 public signatures:

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

`UIContext` also exposes `RootObject`, `LifetimeToken`, `IsDisposed`, `Fault`, and a detached `Displays` snapshot. `UIHandle` exposes `Context`, `Id`, `DefinitionId`, `Parent`, `State`, `ViewObject`, and `LifetimeToken`. `UIHooks` has settable `PrepareAsync`, `OpenAsync`, and `CloseAsync` callbacks of `Func<UIHandle, CancellationToken, UniTask>` plus `Closed` of `Action<UIHandle>`. Hooks are snapshotted when the request is accepted. Enums are `UIState` (`Opening`, `Visible`, `Closing`, `Closed`), `UIRole` (`Hud`, `Popup`), `UIInputMode` (`Modeless`, `Modal`), `UIRetention` (`DestroyOnClose`, `Reuse`), and `UIHideStrategy` (`DeactivateView`, `DisableCanvasRendering`); declared values do not imply supported policy.

## Registration and asset preparation

A `UIDefinition` requires one live direct prefab or one non-empty explicit `assetKey`, a non-empty unique ID, and defined enum values. A key requires a configured `loadPrefab` delegate. Registration stores metadata only; it does not load assets or create clones. Direct prefabs and provider-returned prefabs remain borrowed.

`UIContext.PrepareAsync` is optional asset preparation only: it verifies that a direct prefab is alive or resolves a key through the provider. It never instantiates or warms a clone. Concurrent requests for the same key share one provider request; a successful borrowed prefab result stays cached per key until owner shutdown. A caller token cancels only that caller's wait; shared preparation uses the context lifetime. Failure removes the key entry, so another explicit Prepare/Open request may retry; there is no automatic retry. If the context ends during a provider call, waiters are cancelled and a late result is ignored rather than displayed or cached. The provider and returned source remain untouched.

## Errors

`UIContext` construction rejects a null/destroyed root with `ArgumentNullException`. `Register(null)` is `ArgumentNullException`; empty/duplicate IDs, invalid metadata, or definitions that do not select exactly one source are `ArgumentException`. A key without a configured provider is `InvalidOperationException`; unsupported HUD/explicit-host and Modal/renderer-only policies are `NotSupportedException`. Operations after owner termination throw `ObjectDisposedException` where declared.

`PrepareAsync` rejects an unknown ID with `ArgumentException`; caller or owner cancellation is `OperationCanceledException`; a destroyed direct source, dead cached key result, or provider result without a live prefab is `InvalidOperationException`. Provider exceptions are propagated through the request. `BeginOpen` rejects a null request with `ArgumentNullException`, unknown ID with `ArgumentException`, non-null parent or Modal mode with `NotSupportedException`, invalid mode with `ArgumentException`, pre-cancelled token with `OperationCanceledException`, and duplicate active/closing definition with `InvalidOperationException`. Opening failure is reported through `Opened` after cleanup; if cleanup also fails, both are preserved in an `AggregateException`. Close/shutdown cleanup failures are aggregated after remaining cleanup attempts.

## Opening, display preparation, and reuse

`BeginOpen` validates the definition and request, reserves a new context-local handle ID, rejects a duplicate active/closing display of the same definition, snapshots hooks, and starts the lifecycle. It returns the handle before hooks finish. `Opened` reports the opening outcome. `OpenAsync` calls the same path and returns that handle after `Opened` succeeds.

The opening path resolves the prefab, rents the definition's cached clone or instantiates a new clone under inactive context storage, then invokes the request's `UIHooks.PrepareAsync` while the clone is inactive. This hook prepares display data/subscriptions and is distinct from `UIContext.PrepareAsync` asset preparation. After it completes, the clone is parented under the borrowed root and activated, then `UIHooks.OpenAsync` runs. `Opened` succeeds only after both hooks complete.

`DestroyOnClose` destroys the clone. `Reuse` keeps at most one inactive clone per definition and publishes it to the cache only after successful close cleanup and the `Closed` observer. Every display generation gets a fresh handle, token, and cleanup set, even when using the same native clone. Failed/cancelled Opening, close-hook failure, cleanup failure, native failure, or `Closed` observer failure discards the candidate clone instead of caching it. A `Closed` observer may start an independent new generation; old-generation retirement must not affect it.

## Cleanup, close, and owner shutdown

`RegisterCleanup(Action)` accepts synchronous, display-owned cleanup. Actions run once in reverse registration order; disposing a registration early runs it immediately once. Remaining actions are attempted after a failure and errors remain observable. Do not register destruction/disposal of borrowed roots, prefab sources, providers, ResourceManagers, InputManagers, or services.

`CloseAsync` starts non-vetoable close and shares the generation's completion; its cancellation token cancels only that caller's wait. A visible close awaits `UIHooks.CloseAsync` unless owner shutdown has cancelled that phase, runs cleanup, deactivates the clone, then either retires it as a reuse candidate or destroys it. The handle is set to `Closed` and `ViewObject` is cleared before `UIHooks.Closed` runs. The shared `Closed` task completes only after the observer and candidate cache/discard decision. Hook, cleanup, native, observer, and candidate disposal failures are preserved; remaining cleanup is attempted. Opening failure is reported through `Opened`; `Closed` does not fault for that opening error alone when cleanup succeeds.

`ShutdownAsync` is idempotent and shares completion. It rejects new work, cancels owner lifetime and asset waiters, closes active/opening/closing displays, destroys cached inactive clones and context storage, and clears definitions/provider references. It waits for display cleanup/native retirement and cached-clone destruction; an already-running borrowed provider may finish later, but its result is ignored. Cleanup continues after callback errors, with failures reported by shutdown completion. Root destruction triggers this fallback. `Dispose()` starts the fallback without awaiting asynchronous work; await `ShutdownAsync()` when completion and deferred errors matter.

Synchronous self-close/self-await and owner shutdown from a hook, plus lifecycle mutation during cleanup/native state application, are rejected. After a hook yields, self-await cycles and shutting down its own context remain forbidden but are not reliably detected by the synchronous guard. Independent display composition is supported. A `UniTask.AsTask()` bridge may represent `OperationCanceledException` as `TaskStatus.Faulted`; inspect the exception.

## Example and limits

This explanatory example is **NotRun**; the tests cover contracts, not this exact snippet:

```csharp
var context = new UIContext(ownerRoot, loadPrefab: LoadPrefabAsync);
context.Register(new UIDefinition("settings", assetKey: "ui/settings",
    retention: UIRetention.Reuse));
await context.PrepareAsync("settings"); // asset only; no clone is created
var handle = context.BeginOpen(new UIOpenRequest("settings",
    hooks: new UIHooks { PrepareAsync = PrepareViewAsync }));
await handle.Opened;
await handle.CloseAsync();
await context.ShutdownAsync();
```

P3 HUD/parent/host/Canvas policy, P3 renderer-only hiding and P4 Modal/Input, and P5 Virtual ScrollRect remain deferred. No consumer installation, Player, Profiler, broad UX, or user-acceptance result is claimed. See the [UIContext design contract](../GAME_UI_SYSTEM.md) and the final P2 test records linked above.
