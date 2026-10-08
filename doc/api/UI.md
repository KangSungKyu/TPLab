# UIContext API

TPLab.UI provides a root-owned lifecycle for HUD and Popup displays from direct or provider-keyed prefab sources. ImplementationStatus: Implemented through P3. ValidationStatus: Partial. SourceRevision: `18666acae25b04c1c63ca49d8c00ca2ee67da308`.

On original Editor PID 24376, the P3-focused UI suite passed EditMode 18/18 and PlayMode 30/30 (failed 0, skipped 0), with compile errors 0 and product Console errors 0. Edit used connector-exec-file runId `303ed314e92e4c3cb12d9f935257816f`; Play used CLI PID 29644. The 48 focused tests include 18 P3 additions; not every added test had a separate Red run. Evidence: [P3 EditMode](../validation/ui-system/p3/final-edit.json), [native EditMode result](../validation/ui-system/p3/final-edit.native.json), [P3 PlayMode](../validation/ui-system/p3/final-play.json), [Console check](../validation/ui-system/p3/final-console.json), and [P3 validation record](../validation/ui-system/p3/README.md). The source commit contains 375 source files; six protected inputs remained unchanged. Historical P1/P2 evidence remains [P2/P1 EditMode](../validation/ui-system/p2/final-edit.json) and [P2/P1 PlayMode](../validation/ui-system/p2/final-play.json). Consumer installation, Player, Profiler, broad UX, and user acceptance remain **NotRun** (P7). UI remains development source, outside the released 0.0.1 Core/Input/Editor packages and tag; no UI package version is set.

## Responsibility and ownership

`UIContext` owns accepted display generations, instantiated clones, one optional reusable clone per definition, shared provider-key preparation coordination, cleanup registrations, and root-linked lifetime. The root, direct prefab source, provider, and external services are borrowed; context shutdown never destroys or disposes them. Connect the context to the actual persistent or scene root and await `ShutdownAsync()` before unloading it. `Dispose()` begins the same fallback but cannot await asynchronous hooks.

All Unity object operations and callbacks run on Unity's main thread. P3 supports HUD selection, logical parent trees for Popup displays, registered borrowed hosts, managed fixed Canvas/sibling order, DeactivateView and constrained DisableCanvasRendering, and DestroyOnClose or Reuse. Modal/Input is deferred to P4; Virtual ScrollRect is deferred to P5.

## Public declarations

P3 adds host registration and HUD selection to the P1/P2 public signatures:

```csharp
public sealed class UIContext : IDisposable
public UIContext(GameObject rootObject,
    Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
    Func<IDisposable> acquireModalBlock = null)
public void RegisterHost(string id, Transform container);
public UIHandle CurrentHud { get; }
public UniTask<UIHandle> SelectHudAsync(UIOpenRequest request, CancellationToken cancellationToken = default);
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

UIContext construction rejects a null/destroyed root with ArgumentNullException. Register(null) throws ArgumentNullException; empty/duplicate IDs, invalid metadata, or definitions that do not select exactly one source throw ArgumentException. A key without a configured provider throws InvalidOperationException. Unsupported Modal input throws NotSupportedException until P4; invalid presentation topology, unsupported renderer-only setup, or invalid logical parent relationships throw InvalidOperationException. Operations after owner termination throw ObjectDisposedException where declared.

BeginOpen rejects a null request with ArgumentNullException, unknown ID with ArgumentException, Modal input with NotSupportedException, invalid or non-visible parent relationships with InvalidOperationException, invalid mode with ArgumentException, pre-cancelled token with OperationCanceledException, and duplicate active/closing owner-definition pair with InvalidOperationException. Opening failure is reported through Opened after cleanup; cleanup failure is preserved with the opening error in AggregateException. Close/Shutdown cleanup errors are aggregated after remaining cleanup is attempted.

## Opening, display preparation, and reuse

`BeginOpen` validates the definition and request, reserves a new context-local handle ID, rejects a duplicate active/closing display of the same definition, snapshots hooks, and starts the lifecycle. It returns the handle before hooks finish. `Opened` reports the opening outcome. `OpenAsync` calls the same path and returns that handle after `Opened` succeeds.

The opening path resolves the prefab, takes a cached clone or instantiates under storage, then invokes request UIHooks.PrepareAsync for display data/subscriptions. For fresh DeactivateView instances, storage and the clone are inactive during this hook. For fresh DisableCanvasRendering instances, the clone is constructed under inactive storage, then its owned presentation wrapper is prepared and shown; after reuse, the renderer-only clone GameObject remains active in active rendering storage while its Canvas, captured GraphicRaycasters, and wrapper visibility mask are disabled. UIHandle.ViewObject is the inner clone; renderer-only mode places it under the owned wrapper. This hook is distinct from UIContext.PrepareAsync asset preparation. After display preparation, the presentation root is parented under the registered host and activated/shown, then UIHooks.OpenAsync runs. Opened succeeds only after the opening hooks complete.

DestroyOnClose destroys the owned clone/presentation. Reuse retains at most one candidate per definition after successful visible close, cleanup, and Closed observer. DeactivateView candidates are inactive clones; DisableCanvasRendering candidates remain active GameObjects in active rendering storage with the dedicated Canvas/raycasters disabled and the owned visibility CanvasGroup masking rendering/raycasts. Every generation gets a fresh handle, token, and cleanup set. Opening, cleanup, native, or Closed observer failures discard the candidate.

## Cleanup, close, and owner shutdown

`RegisterCleanup(Action)` accepts synchronous, display-owned cleanup. Actions run once in reverse registration order; disposing a registration early runs it immediately once. Remaining actions are attempted after a failure and errors remain observable. Do not register destruction/disposal of borrowed roots, prefab sources, providers, ResourceManagers, InputManagers, or services.

CloseAsync starts non-vetoable close and shares the generation completion; its cancellation token cancels only that caller wait. A visible close awaits UIHooks.CloseAsync unless owner shutdown cancels that phase, runs cleanup, hides/deactivates according to the configured presentation strategy, then retires or caches the candidate. Renderer-only hide keeps the clone GameObject active while closing the owned wrapper mask and disabling its dedicated Canvas and captured GraphicRaycasters. The handle becomes Closed and ViewObject is cleared before UIHooks.Closed runs. Closed completes after the observer and cache/discard decision. Hook, cleanup, native, observer, and candidate disposal failures are preserved after remaining cleanup is attempted. Opening failure is reported through Opened; Closed does not fault for that opening error alone when cleanup succeeds.

ShutdownAsync is idempotent and shared. It rejects new work, cancels owner lifetime and asset waiters, closes active/opening/closing displays, destroys cached clones and owned storage, and clears definitions/provider references. Cached renderer-only clones may be active GameObjects in rendering storage. Shutdown waits for display cleanup/native retirement and cached-clone destruction; a borrowed provider may finish later, but its result is ignored.

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

Modal/Input remains deferred to P4 and Virtual ScrollRect to P5. Consumer installation, Player, Profiler, broad UX, and user acceptance remain NotRun under P7.

## Registered hosts, HUD selection, and presentation

The added declarations are RegisterHost(string id, Transform container), CurrentHud { get; }, and SelectHudAsync(UIOpenRequest request, CancellationToken cancellationToken = default). RegisterHost reserves the implicit default host ID; blank or duplicate IDs throw ArgumentException, null/destroyed containers throw ArgumentNullException, cleanup/native mutation or wrong-thread boundaries throw InvalidOperationException, and a terminating owner throws ObjectDisposedException. It borrows the Transform and does not create a Canvas.

CurrentHud is null when no selected HUD remains and stays observable after context termination. SelectHudAsync accepts a registered parentless HUD request; a non-HUD or parented request throws InvalidOperationException. Concurrent selection throws InvalidOperationException. A candidate is resolved/instantiated and its display Prepare hook runs before the old HUD subtree starts closing; fresh DeactivateView clones are inactive, while reused renderer-only clones are active but hidden by their wrapper mask. Failure/cancellation during that preparation preserves the old HUD and children. Once old close begins, it is not rolled back; children close first. Close failure remains observable and the new candidate is cleaned/discarded with errors preserved. CurrentHud changes only after the new HUD reaches Visible. Independent context-owned Popups are not part of the HUD subtree.

Logical Parent is immutable and independent of the physical registered host. Popup parent must belong to this context and be Visible with Visible ancestors; invalid parent ownership/state throws InvalidOperationException. Duplicate active/closing owner+definition also throws InvalidOperationException. Child displays close before parents. Accepted handle ID/order determines managed HUD/Popup order, not provider or hook completion order.

Physical host Transforms, Canvas, sorting configuration, and borrowed sibling objects remain project-owned. Managed displays on one Canvas use managed sibling slots; sorting may exchange only those managed slots, preserving borrowed siblings' absolute indices. Fixed Canvas/host topology must express parent below child in ascending sibling/sorting order; child-first applies only to closure. The topology must represent that logical order. Overlapping/ambiguous fixed hosts, incomparable host trees, equal separate-Canvas sorting, or incompatible render mode, camera, or target display throw InvalidOperationException. Every prefab Canvas is rejected when overrideSorting is enabled. Graphics require an active host and a supported effective Canvas. WorldSpace roots and ScreenSpaceCamera roots without a camera are rejected; managed planes must share render mode, camera, and target display.

DisableCanvasRendering requires a dedicated Canvas on the owned clone root. Any descendant CanvasGroup with ignoreParentGroups=true is rejected because it can bypass the owned mask. The implementation creates an owned RectTransform wrapper with its own CanvasGroup and preserves existing project CanvasGroup values. UIHandle.ViewObject refers to the inner clone, whose Transform parent is the wrapper; the wrapper is the presentation root placed in the host. On hide, the owned group sets alpha=0, blocksRaycasts=false, interactable=false, and disables the owned Canvas and captured GraphicRaycasters. It does not disable a shared Canvas.

For DeactivateView, fresh instances are inactive during inactive display preparation and close deactivates the view. For DisableCanvasRendering, a fresh clone is constructed under inactive storage, then shown as an active GameObject with Canvas and raycasters enabled. A successfully reused renderer-only clone is retained under active rendering storage: its GameObject remains active while its Canvas/raycasters are disabled and the owned visibility group masks rendering and raycasts. Re-show restores captured Canvas/raycaster state and opens the mask. Thus Reuse is at most one clone per definition but renderer-only cached clones are active GameObjects, not inactive clones. Reuse is published only after successful visible close, cleanup, and Closed observer; failure discards the candidate. Before each generation, authored Transform and RectTransform anchors, pivot, size, and position are restored from the borrowed prefab. Moving the presentation root to a host preserves its anchored position.

Explanatory examples remain NotRun. P4 Modal/Input and P5 Virtual ScrollRect remain deferred; P7 consumer, Player, Profiler, broad UX, and user-acceptance gates remain NotRun.
