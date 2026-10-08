# UIContext API

TPLab.UI provides a root-owned lifecycle for HUD and Popup displays from direct or provider-keyed prefab sources. ImplementationStatus: Implemented through P6. ValidationStatus: Automated regression and consumer verification complete for recorded scopes; physical/visual acceptance remains pending. SourceRevision: `99df25dd6b89fd7f3e322c8033b61eb1364f495e` for final regression inputs; runtime UI implementation is unchanged from P6 source `21f89e2580f08be3903724efa0a42e5ad0567c83`.

On original Editor PID 24376, the P3-focused UI suite passed EditMode 18/18 and PlayMode 30/30 (failed 0, skipped 0), with compile errors 0 and product Console errors 0. Edit used connector-exec-file runId `303ed314e92e4c3cb12d9f935257816f`; Play used CLI PID 29644. The 48 focused tests include 18 P3 additions; not every added test had a separate Red run. Evidence: [P3 EditMode](../validation/ui-system/p3/final-edit.json), [native EditMode result](../validation/ui-system/p3/final-edit.native.json), [P3 PlayMode](../validation/ui-system/p3/final-play.json), [Console check](../validation/ui-system/p3/final-console.json), and [P3 validation record](../validation/ui-system/p3/README.md). The source commit contains 375 source files; six protected inputs remained unchanged. Historical P1/P2 evidence remains [P2/P1 EditMode](../validation/ui-system/p2/final-edit.json) and [P2/P1 PlayMode](../validation/ui-system/p2/final-play.json). P4 final focused original-Editor validation passed EditMode 24/24 and PlayMode 41/41 (failed 0, skipped 0); Edit runId c4c9dd94c6b74ec9be545306d0bcc897, Play CLI PID 34520, Editor PID 24376, product Console 0. Evidence: [P4 Edit](../validation/ui-system/p4/final-edit.json), [native Edit](../validation/ui-system/p4/final-edit.native.json), [P4 Play](../validation/ui-system/p4/final-play.json), and [source match](../validation/ui-system/p4/source-commit-match.json). SourceRevision deb222cf6fda752d3e0dd6d22bda67f9d60e9e16. This paragraph records historical P3 validation; current P7 evidence and remaining gates are summarized below. UI remains development source outside released 0.0.1 Core/Input/Editor packages/tag; no UI package version is set.

## Responsibility and ownership

`UIContext` owns accepted display generations, instantiated clones, one optional reusable clone per definition, shared provider-key preparation coordination, cleanup registrations, and root-linked lifetime. The root, direct prefab source, provider, and external services are borrowed; context shutdown never destroys or disposes them. Connect the context to the actual persistent or scene root and await `ShutdownAsync()` before unloading it. `Dispose()` begins the same fallback but cannot await asynchronous hooks.

P7 current validation: final original regression passed Edit318/318 and Play306/306, failed0/skip0. [P7 Edit](../validation/ui-system/p7/final-regression/edit.json) · [P7 Play](../validation/ui-system/p7/final-regression/play.json) · [source match](../validation/ui-system/p7/final-source-check/source-commit-match.json). Base isolated consumer build/smoke/scroll/Canvas passed on a Windows Mono graphics Player; [report](../validation/ui-system/p7/consumer-base-c/consumer-report.json). Exploratory scroll/Canvas measurements are summarized in [performance record](../UI_PERFORMANCE.md) with raw evidence links; one device/run only. The base consumer passed its stated build/smoke/scroll/Canvas scope. A fresh optional-Input Single consumer run on candidate `8310a94bcb674f28bee2c246c4e5eae9d6c83804` passed one Editor build (two artifacts) and four fresh Windows graphics Players (smoke, scroll, Canvas, sample), exit 0 with no timeout ([consumer report](../validation/ui-system/p7/consumer-input-single-final/consumer-report.json)). Its sample passed 80 checks, 30 continuous fresh positive-draw frames (minimum 11), seven inventory snapshots, four manual Continue checks, and two ordered cleanup checks ([sample validation](../validation/ui-system/p7/consumer-input-single-final/sample-validated.json)). This candidate changes the consumer driver only; the validated UI regression snapshot remains `99df25dd6b89fd7f3e322c8033b61eb1364f495e`. The final optional-Input Additive consumer on candidate `8310a94bcb674f28bee2c246c4e5eae9d6c83804` also passed one Editor build (two artifacts) and four fresh Windows graphics Players (smoke, scroll, Canvas, sample), exit 0 with no timeout ([consumer report](../validation/ui-system/p7/consumer-input-additive-final/consumer-report.json)). Its sample passed 78 checks, 30 continuous fresh positive-draw frames (minimum 11), seven inventory snapshots, four manual Continue checks, and two cleanup checks; it ended with ManagerStopped and zero owned/registered scopes across two normal scenes ([sample validation](../validation/ui-system/p7/consumer-input-additive-final/sample-validated.json)). Both final consumer candidates preserve the same original Assets/Packages/ProjectVersion inputs; only the consumer tools driver differs. Candidate integrity captured 476 inputs: 475 Git matches, six protected raw files unchanged, and the only changed input from the regression snapshot was the consumer sample driver; original Assets/Packages/ProjectVersion inputs are identical ([candidate source check](../validation/ui-system/p7/candidate-source-check/source-commit-match.json)). All six regular benchmark arms recorded 600 fresh frames. Physical/visual acceptance remains pending and main acceptance pending. UI is not included in released 0.0.1 packages/tag.

All Unity object operations and callbacks run on Unity's main thread. P3 supports HUD selection, logical parent trees for Popup displays, registered borrowed hosts, managed fixed Canvas/sibling order, DeactivateView and constrained DisableCanvasRendering, and DestroyOnClose or Reuse. P4 Modal/Input eligibility and the optional Input System adapter are implemented; P5 fixed-height virtual list and P6 settings/root installer are implemented in development source. P6 focused Edit33/33 and Play53/53 passed, failed0/skip0; see [P6 validation](../validation/ui-system/p6/README.md), [Edit](../validation/ui-system/p6/final-edit.json), and [Play](../validation/ui-system/p6/final-play.json). P6 source match and sample authoring/compile are historical prerequisites. Current P7 partial evidence and open gates are summarized above; see also [manual acceptance guide](../UI_ACCEPTANCE.md).

## Public declarations

P3 adds host registration and HUD selection to the P1/P2 public signatures:

```csharp
public sealed class UIContext : IDisposable
public UIContext(GameObject rootObject,
    Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
    Func<IDisposable> acquireModalBlock = null, EventSystem eventSystem = null)
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

## P4 modal eligibility, focus, and user-close requests

P4 adds a borrowed, explicit EventSystem and per-generation input commands:

~~~csharp
public EventSystem EventSystem { get; }
public event Action<UIHandle> DisplayChanged;
public UIInputMode InputMode { get; }
public bool CanReceiveInput { get; }
public void SetInputMode(UIInputMode inputMode);
public void BringToFront();
public void SetFocus(GameObject target);
public UniTask<bool> RequestCloseAsync(UIUserCloseReason reason,
    CancellationToken cancellationToken = default);
public Func<UIHandle, UIUserCloseReason, CancellationToken, UniTask<bool>> CanCloseAsync { get; set; }

// Optional TPLab.UI.InputSystem assembly
public void Bind(UIContext context, InputManager input,
    InputSystemUIInputModule module, Guid uiMapId);
public IDisposable AcquireModalBlock(string layerId);
public void Unbind();
~~~

SetInputMode requires a Visible generation. Modal mode gates lower UI and, when configured, acquires an independent lease from the supplied modal factory. Without that factory, Modal still blocks lower UI but makes no gameplay-input guarantee. CanReceiveInput reports logical eligibility of a Visible generation, not borrowed map/module activation. BringToFront reorders the owned logical subtree without changing its generation or borrowed host/sorting state. SetFocus requires an explicit borrowed EventSystem and a live target inside the handle's view. An Opening hook may set focus; the context remembers it and applies it when the generation becomes Visible and eligible.

User close is opt-in through CanCloseAsync. Missing handler or veto returns false and leaves the display open; accepted close returns true only after shared close completes as Closed. Caller cancellation cancels that request/approval wait, not owner cleanup. Forced CloseAsync and owner shutdown bypass veto and cancel pending approval. Reasons are Cancel, OutsidePointer, and Button; no outside detector or backdrop is installed.

Base TPLab.UI has no Core.Input or Unity Input System dependency; UI-only Modal works without the optional adapter. Native integration uses the separate TPLab.UI.InputSystem assembly. Bind requires the same explicit EventSystem as the module and configured action references from the runtime-cloned UI map. The project pre-registers a compatible mapless BlockLower layer and supplies adapter.AcquireModalBlock as the context's modal factory. EventSystem, InputManager, module, actions and external leases are borrowed. Adapter owns only its subscriptions/recovery leases; it never disposes those borrowed owners.

Modal retirement keeps lower UI blocked through hide and cleanup, raw pointer/touch/submit/cancel/move release, and a later EventSystem frame, preventing the closing event from reaching newly exposed UI. Closing middle modal B in A(modeless)-B(modal)-C(modeless) preserves C focus and independent game/transition leases. Unbind disables the borrowed module and refreshes existing layer policy; it does not return an active display's modal lease. On native state-application failure, the adapter's BlockAll lease transfers to UIContext and remains owned there through owner shutdown, including after Unbind.

DisplayChanged dispatch is synchronous and read-only: all listeners are attempted, and listener failures surface through the affected lifecycle completion after remaining cleanup and lease retirement. SetInputMode, BringToFront and SetFocus command failures throw from that command and are removed from its error batch, so they do not poison a later close. Ordinary listener failures do not set Fault; native adapter application failures do. Arbitrary raw Input System action subscribers bypass the supported native UI replay boundary. See the [P4 UI/input contract](../GAME_UI_SYSTEM.md).

## Errors

UIContext construction rejects a null/destroyed root with ArgumentNullException. Register(null) throws ArgumentNullException; empty/duplicate IDs, invalid metadata, or definitions that do not select exactly one source throw ArgumentException. A key without a configured provider throws InvalidOperationException. Modal input is supported through P4; invalid presentation topology, unsupported renderer-only setup, or invalid logical parent relationships throw InvalidOperationException. Operations after owner termination throw ObjectDisposedException where declared.

BeginOpen rejects a null request with ArgumentNullException, unknown ID with ArgumentException, invalid or non-visible parent relationships with InvalidOperationException, invalid mode with ArgumentException, pre-cancelled token with OperationCanceledException, and duplicate active/closing owner-definition pair with InvalidOperationException. Modal input is supported through P4. Opening failure is reported through Opened after cleanup; cleanup failure is preserved with the opening error in AggregateException. Close/Shutdown cleanup errors are aggregated after remaining cleanup is attempted.

## Opening, display preparation, and reuse

`BeginOpen` validates the definition and request, reserves a new context-local handle ID, rejects a duplicate active/closing display of the same definition, snapshots hooks, and starts the lifecycle. It returns the handle before hooks finish. `Opened` reports the opening outcome. `OpenAsync` calls the same path and returns that handle after `Opened` succeeds.

The opening path resolves the prefab, takes a cached clone or instantiates under storage, then invokes request UIHooks.PrepareAsync for display data/subscriptions. For fresh DeactivateView instances, storage and the clone are inactive during this hook. For fresh DisableCanvasRendering instances, the clone is constructed under inactive storage, then its owned presentation wrapper is prepared and shown; after reuse, the renderer-only clone GameObject remains active in active rendering storage while its Canvas, captured GraphicRaycasters, and wrapper visibility mask are disabled. Every managed display places the inner clone under its owned input/visibility wrapper; the host parents that wrapper and UIHandle.ViewObject returns the inner clone. This hook is distinct from UIContext.PrepareAsync asset preparation. After display preparation, the presentation root is parented under the registered host and activated/shown, then UIHooks.OpenAsync runs. Opened succeeds only after the opening hooks complete.

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

P4 Modal/Input and P5 VirtualScrollRect are implemented in development source. P5 first Green passed Edit31/31 and Play48/48; final UI79 passed Edit31/31 and Play48/48, failed0/skip0, product Console empty. [Final Edit](../validation/ui-system/p5/final-edit.json) · [Final Play](../validation/ui-system/p5/final-play.json). Edit runId `7984bff4b98d43e0b8aba050ba85b45e`; Play connector run `34780-1791455587583642700` recovered its same completed result after CLI timeout, with no rerun. SourceRevision: 8f8abd290d540dc0b6ba30c5acc8b36b271bc302. Snapshot verified 405 non-protected source matches and six protected files unchanged ([match record](../validation/ui-system/p5/source-commit-match.json)). P6 settings/root installation is implemented and focused-verified. P7 evidence is summarized at the top of this document; it does not establish final visual/user acceptance.

## Registered hosts, HUD selection, and presentation

The added declarations are RegisterHost(string id, Transform container), CurrentHud { get; }, and SelectHudAsync(UIOpenRequest request, CancellationToken cancellationToken = default). RegisterHost reserves the implicit default host ID; blank or duplicate IDs throw ArgumentException, null/destroyed containers throw ArgumentNullException, cleanup/native mutation or wrong-thread boundaries throw InvalidOperationException, and a terminating owner throws ObjectDisposedException. It borrows the Transform and does not create a Canvas.

CurrentHud is null when no selected HUD remains and stays observable after context termination. SelectHudAsync accepts a registered parentless HUD request; a non-HUD or parented request throws InvalidOperationException. Concurrent selection throws InvalidOperationException. A candidate is resolved/instantiated and its display Prepare hook runs before the old HUD subtree starts closing; fresh DeactivateView clones are inactive, while reused renderer-only clones are active but hidden by their wrapper mask. Failure/cancellation during that preparation preserves the old HUD and children. Once old close begins, it is not rolled back; children close first. Close failure remains observable and the new candidate is cleaned/discarded with errors preserved. CurrentHud changes only after the new HUD reaches Visible. Independent context-owned Popups are not part of the HUD subtree.

Logical Parent is immutable and independent of the physical registered host. Popup parent must belong to this context and be Visible with Visible ancestors; invalid parent ownership/state throws InvalidOperationException. Duplicate active/closing owner+definition also throws InvalidOperationException. Child displays close before parents. Accepted handle ID/order determines managed HUD/Popup order, not provider or hook completion order.

Physical host Transforms, Canvas, sorting configuration, and borrowed sibling objects remain project-owned. Managed displays on one Canvas use managed sibling slots; sorting may exchange only those managed slots, preserving borrowed siblings' absolute indices. Fixed Canvas/host topology must express parent below child in ascending sibling/sorting order; child-first applies only to closure. The topology must represent that logical order. Overlapping/ambiguous fixed hosts, incomparable host trees, equal separate-Canvas sorting, or incompatible render mode, camera, or target display throw InvalidOperationException. Every prefab Canvas is rejected when overrideSorting is enabled. Graphics require an active host and a supported effective Canvas. WorldSpace roots and ScreenSpaceCamera roots without a camera are rejected; managed planes must share render mode, camera, and target display.

DisableCanvasRendering requires a dedicated Canvas on the owned clone root. Any descendant CanvasGroup with ignoreParentGroups=true is rejected because it can bypass the owned mask. The implementation creates an owned RectTransform wrapper with its own CanvasGroup and preserves existing project CanvasGroup values. UIHandle.ViewObject refers to the inner clone, whose Transform parent is the wrapper; the wrapper is the presentation root placed in the host. On hide, the owned group sets alpha=0, blocksRaycasts=false, interactable=false, and disables the owned Canvas and captured GraphicRaycasters. It does not disable a shared Canvas.

For DeactivateView, fresh instances are inactive during inactive display preparation and close deactivates the view. For DisableCanvasRendering, a fresh clone is constructed under inactive storage, then shown as an active GameObject with Canvas and raycasters enabled. A successfully reused renderer-only clone is retained under active rendering storage: its GameObject remains active while its Canvas/raycasters are disabled and the owned visibility group masks rendering and raycasts. Re-show restores captured Canvas/raycaster state and opens the mask. Thus Reuse is at most one clone per definition but renderer-only cached clones are active GameObjects, not inactive clones. Reuse is published only after successful visible close, cleanup, and Closed observer; failure discards the candidate. Before each generation, authored Transform and RectTransform anchors, pivot, size, and position are restored from the borrowed prefab. Moving the presentation root to a host preserves its anchored position.

Explanatory examples remain NotRun. P4 Modal/Input, P5 fixed-height virtual list, and P6 settings/root installation are implemented in development source. P6 focused tests passed Edit33/33 and Play53/53, failed0/skip0; [P6 validation](../validation/ui-system/p6/README.md). P6 source match and sample authoring/compile are historical prerequisites; current P7 evidence is recorded above.

## P5: fixed-height virtual list

`VirtualScrollRect` virtualizes one borrowed native vertical `ScrollRect` and one borrowed fixed-height `RectTransform` prefab. It owns only its row clones, per-binding cancellation tokens, inactive staging object, and its own ScrollRect listener. The supported layout is one column, fixed row height, finite non-negative spacing, no horizontal padding, explicit viewport and direct-child content, top-stretch content/cell roots with pivot `(0.5, 1)` and identity local transform, and no `LayoutGroup` or `ContentSizeFitter` on those roots. Horizontal scrolling and unsupported anchors/pivots are rejected.

```csharp
public int Count { get; }
public int CountActive { get; }
public int CountInactive { get; }
public int CountOwned { get; }
public int TotalCreated { get; }
public int TotalDestroyed { get; }

public void Configure(ScrollRect scrollRect, RectTransform cellPrefab, float cellHeight,
    Action<VirtualCellBinding> bind, Action<VirtualCellBinding> unbind = null,
    int overscan = 2, float spacing = 0);
public void SetCount(int count);
public void Refresh();
public void ScrollToIndex(int index);
```

`Count` is logical data length. `CountActive` is bound rows; `CountInactive` is retained unbound clones; `CountOwned` is their sum. `TotalCreated` and `TotalDestroyed` are cumulative clone counters, not row counts; native destruction can complete at frame end. Extent is `count * cellHeight + max(0, count - 1) * spacing`, zero for an empty list. `SetCount` updates extent and clamps content position while retaining unchanged visible indexes. Successful `Configure` retires previous bindings and clones and starts at count zero; explicitly set the new count. Invalid configuration is rejected before replacing valid current state. `ScrollToIndex(0)` on empty data resets to top; other indexes must be in range. Explicit jumps stop inertia; normal ScrollRect drag, elasticity, scrollbar, and inertia remain native.

`Refresh()` explicitly replaces visible binding generations after project data changes. Automatic scroll/viewport reconciliation preserves unchanged in-range bindings and does not rebind idle rows. The viewport cell budget is `ceil(viewportHeight / (cellHeight + spacing)) + 1 + 2 * overscan`. Reconciliation pre-creates at most `min(Count, budget)` cells, but released cells may remain retained up to the viewport budget even when Count is smaller or zero. Count zero means CountActive is zero; CountInactive and CountOwned may still be nonzero. Viewport shrink trims excess clones. Warmed fixed-viewport counter stability demonstrates bounded ownership/reuse, not performance. A bind/native reconciliation failure cleans partial bindings and surfaces an `AggregateException`; automatic retry stops until an explicit `SetCount`, `Refresh`, or `ScrollToIndex`. Disable and destroy invalidate bindings and attempt remaining cleanup. The borrowed ScrollRect, viewport/content, prefab, and unrelated listeners remain caller-owned.

`VirtualCellBinding` is a readonly snapshot for one row generation:

```csharp
public readonly struct VirtualCellBinding
{
    public int Index { get; }
    public long Generation { get; }
    public RectTransform View { get; }
    public CancellationToken LifetimeToken { get; }
    public bool IsCurrent { get; }
}
```

`View` is borrowed from the virtual-list owner. Project async work must return to Unity's main thread and check both captured `LifetimeToken` and `IsCurrent` before applying a result. Index, generation, and cached token remain readable after cancellation-source disposal; `View` and `IsCurrent` require main-thread access. Unbind, bind failure, disable, and destruction cancel the generation before optional unbind cleanup and continue cleanup across rows when callbacks fail. Project data, async work, and subscriptions remain project-owned.

Commands and Unity view checks run on the main thread. Bind/unbind callbacks must not synchronously reenter `Configure`, `SetCount`, `Refresh`, or `ScrollToIndex` on this component; unrelated UI composition is allowed. Invalid numeric values/count/index use `ArgumentOutOfRangeException`; invalid borrowed topology/layout uses `ArgumentException` (null bind uses `ArgumentNullException`); wrong thread, unconfigured calls, or structural callback reentry use `InvalidOperationException`. Cleanup/native errors are aggregated after remaining cleanup is attempted. Do not force layout/canvas rebuilds per bound row.

P5 first focused Green passed Edit31/31 and Play48/48. Final focused UI79 passed Edit31/31 and Play48/48, failed0/skip0, product Console empty. [Final Edit](../validation/ui-system/p5/final-edit.json) | [Final Play](../validation/ui-system/p5/final-play.json). Edit runId `7984bff4b98d43e0b8aba050ba85b45e`; Play connector run `34780-1791455587583642700` recovered the completed result after CLI timeout without rerunning. SourceRevision: `8f8abd290d540dc0b6ba30c5acc8b36b271bc302`. The final snapshot verified 405 non-protected source matches and six protected files unchanged. P6 settings/root installation is implemented and focused-verified. This P5 section is historical; current P7 status is summarized at the top. UI remains development source outside released 0.0.1 packages/tag.

## P6: settings and root installation

Namespace `TPLab.UI.Installation` remains in the base `TPLab.UI` assembly; it does not require Input System. `UIContextDefinitionData` is serialized registration metadata with `Id`, `Prefab`, `AssetKey`, `Role`, `HostId`, `InputMode`, `Retention`, and `HideStrategy`. Exactly one source is required. `UIContextSettings` is a borrowed ScriptableObject asset; Inspector values and script configuration use the same validator. IDs are unique/nonblank, enums must be declared values, the first HUD must refer to a HUD definition, and preload IDs must be registered and unique.

```csharp
public void UIContextSettings.Configure(UIContextDefinitionData[] definitions,
    string firstHudDefinitionId = null, string[] preloadDefinitionIds = null);
public string UIContextSettings.FirstHudDefinitionId { get; }
public IReadOnlyList<string> UIContextSettings.PreloadDefinitionIds { get; }
public IReadOnlyList<UIDefinition> UIContextSettings.CreateSnapshot();

public UIContext UIContextInstaller.Context { get; }
public void UIContextInstaller.Configure(UIContextSettings settings,
    UIContextInstaller.HostBinding[] hosts = null,
    ResourceManagerInstaller resources = null,
    Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
    EventSystem eventSystem = null, Func<IDisposable> acquireModalBlock = null,
    UIHooks firstHudHooks = null);
public override void UIContextInstaller.Install(ISceneRoot root);
public override UniTask UIContextInstaller.PrepareAsync(ISceneRoot root, CancellationToken cancellationToken);
public override UniTask UIContextInstaller.ReleaseAsync(ISceneRoot root);
public override void UIContextInstaller.Uninstall(ISceneRoot root);
```

These qualified declarations identify the symbols; they are explanatory signatures, not a compilable class listing. `HostBinding(string id, Transform container)` borrows a live scene container and exposes `Id`/`Container`; the implicit `default` host is the owner root and cannot be registered again. Install rejects an unrelated/non-root owner or missing host. Resource and custom provider are mutually exclusive; keyed definitions require one. A borrowed resource installer must already be installed before UI. Direct prefab definitions need neither provider nor ResourceManager.

Configure validates before replacing script configuration. Install snapshots the definitions and first-HUD/preload lists, creates one context, and registers metadata without cloning/loading. Later edits to the shared Settings asset do not change that installation. Prepare shares one attempt: source preloads create no displays, then first-HUD selection uses the ordinary context pipeline. The first preparation token controls the attempt; later tokens cancel only their own wait. Failure stays observable until Uninstall. Thread, owner identity, root/context cancellation, and late completion checks follow the Runtime XML contract.

Order installers as required resource/input owners, project wiring, UI. Normal reverse Release awaits UI shutdown before earlier services terminate; Uninstall clears `Context` and provides idempotent destruction fallback. Release leaves the terminating context observable until Uninstall. Hosts, settings, prefab assets, provider, EventSystem, ResourceManager, and project hooks remain borrowed. Partial installation attempts cleanup and preserves both installation/cleanup failures. Neither UI installation nor a modal lease calls InputManagerInstaller.CompletePreparation: the project publishes input only after its complete root is prepared.

The optional [project walkthrough](../../Assets/TPLab/Samples/UI/README.md) uses the existing Core Bootstrap/scene callbacks and optional Input System adapter. Scene ownership, visibility, gameplay permission, callback-owned loading UI, and retained clones remain separate lifetimes. Its import, Player, performance and user-acceptance evidence is recorded separately from installer unit tests.
