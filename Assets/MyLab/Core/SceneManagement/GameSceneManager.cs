using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Observable primary/derived transition phases. Faulted permits inspection and explicit shutdown only.</summary>
    public enum SceneTransitionState
    {
        Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene,
        PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted,
        PreparingLoadingPresentation, RevealingLoadingPresentation, ResolvingTarget, AwaitingProceed, Finalizing
    }

    /// <summary>
    /// Main-thread owner of explicit Build/Addressables scene results and graceful primary/derived root release.
    /// Borrows the common root and presentation; their external owner shuts them down after this manager.
    /// First entry has one historical completion; each later primary replacement has its own operation lifetime.
    /// </summary>
    public sealed class GameSceneManager
    {
        private readonly MonoBehaviour _commonHost;
        private readonly ISceneRoot _commonRoot;
        private readonly Scene _commonScene;
        private readonly SceneTransitionCallbacks _callbacks;
        private readonly bool _hasCallbacks;
        private UniTaskCompletionSource _entry;
        private UniTaskCompletionSource _transition;
        private UniTaskCompletionSource _shutdown;
        private CancellationTokenSource _transitionCancellation;
        private Scene[] _initialScenes;
        private Scene[] _retainedScenes = Array.Empty<Scene>();
        private Scene _previousActiveScene;
        private readonly ISceneLoader _buildLoader;
        private readonly ISceneLoader _addressableLoader;
        private ISceneLoader _loader;
        private SceneTarget _target;
        private OwnedPrimary _primary;
        private OwnedPrimary _candidate;
        private readonly List<OwnedPrimary> _derived = new List<OwnedPrimary>();
        private Operation _operation;
        private Scene _expectedActiveScene;
        private LoadSceneMode _mode;
        private bool _stopping;
        private bool _dispatching;
        private bool _evaluating;
        private bool _failureNotified;
        private readonly Dictionary<string, SceneTransitionDefinition> _definitions;

        /// <summary>Current owner phase; Ready is published only after preparation and reveal finish.</summary>
        public SceneTransitionState State { get; private set; }
        /// <summary>Phase of the latest execution failure, retained after explicit shutdown.</summary>
        public SceneTransitionState FailurePhase { get; private set; }
        /// <summary>Execution/cleanup/callback failure, or null before failure. Caller wait cancellation is excluded.</summary>
        public Exception LastFailure { get; private set; }
        /// <summary>Latest operation stage snapshot, or null before any accepted command.</summary>
        public SceneTransitionProgress? Progress { get; private set; }
        /// <summary>Successfully revealed primary scene, distinct from an explicitly activated derived scene; invalid before entry and after primary shutdown starts.</summary>
        public Scene GameScene { get; private set; }
        /// <summary>Native candidate still owned, including a last Single scene that Unity cannot unload.</summary>
        public Scene LoadedScene => (_candidate ?? _primary)?.Result.Scene ?? default;
        /// <summary>Read-only snapshot of actual remaining owned scenes, including previous/candidate instances after failure.</summary>
        public IReadOnlyList<Scene> OwnedScenes => GetOwners().Select(owner => owner.Result.Scene)
            .Where(scene => scene.IsValid() && scene.isLoaded).ToArray();
        /// <summary>Snapshot of successful primary/derived registration, parent metadata and actual root lifetime.</summary>
        public IReadOnlyList<SceneRegistration> RegisteredScenes => GetRegisteredOwners().Select(owner =>
            new SceneRegistration(owner.Result.Scene, owner.Parent != null ? owner.Parent.Result.Scene : default,
                owner.Role, owner.Priority, IsOwnerPrepared(owner), owner.ShutdownStarted)).ToArray();
        /// <summary>Actual remaining candidate root readiness, including failure diagnostics.</summary>
        public bool IsGamePrepared => IsOwnerPrepared(_candidate ?? _primary);
        /// <summary>Gameplay may proceed only while common and all registered roots are prepared and active-scene/inventory ownership still matches.</summary>
        public bool CanProceed => State == SceneTransitionState.Ready && _commonHost != null &&
            _commonHost.gameObject.scene == _commonScene && _commonRoot.IsPrepared && IsGamePrepared &&
            GameScene.IsValid() && GameScene.isLoaded &&
            GetRegisteredOwners().All(IsOwnerPrepared) &&
            SceneManager.GetActiveScene() == _expectedActiveScene && (!_hasCallbacks || _callbacks != null) &&
            InventoryMatches(ExpectedGameScenes());

        /// <summary>
        /// Binds an explicitly installed common host; does not prepare or enter from an installer.
        /// Persistent common roots require callback components under the same persistent hierarchy.
        /// Callback UI/service dependencies are borrowed and must share that lifetime too.
        /// </summary>
        public GameSceneManager(MonoBehaviour commonHost, SceneTransitionCallbacks callbacks = null,
            ISceneLoader buildLoader = null, ISceneLoader addressableLoader = null, SceneTransitionSettings settings = null)
        {
            EnsureMainThread();
            BootstrapSystem.ValidateSceneRoot(commonHost, commonHost != null ? commonHost.gameObject.scene : default, true);
            if (!Application.isPlaying || !((ISceneRoot)commonHost).IsReady)
                throw new InvalidOperationException("Install the common root before constructing its scene manager.");
            _commonHost = commonHost;
            _commonRoot = (ISceneRoot)commonHost;
            _commonScene = commonHost.gameObject.scene;
            _callbacks = callbacks;
            _hasCallbacks = callbacks != null;
            _buildLoader = buildLoader ?? new NativeSceneLoader();
            _addressableLoader = addressableLoader ?? new AddressableSceneLoader();
            _definitions = settings != null ? settings.CreateSnapshot().ToDictionary(definition => definition.Id, StringComparer.Ordinal) : null;
            ValidateCommonLifetime();
        }

        /// <summary>
        /// Covers, prepares common systems, loads Single/Additive, injects, prepares and reveals the first game.
        /// Rejects repeated entry commands and invalid ownership before side effects. Caller token cancels only its wait.
        /// Never call/await entry from a common installer or from a transition hook; start after synchronous installation.
        /// </summary>
        public UniTask EnterFirstSceneAsync(string scenePath, LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            RejectHookReentry();
            cancellationToken.ThrowIfCancellationRequested();
            return EnterFirstSceneAsync(SceneTarget.BuildScene(scenePath), mode, cancellationToken);
        }

        /// <summary>Enters one explicit Build/Addressables target with the same first-entry ownership and caller-wait contract.</summary>
        public UniTask EnterFirstSceneAsync(SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            var operation = RequireAccepted(BeginRequest(new SceneTransitionRequest(SceneTransitionKind.FirstEntry, target,
                sourceScene: _commonScene, mode: mode), cancellationToken));
            return operation.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>
        /// Waits for the existing entry attempt without starting another command. Caller cancellation affects only this wait.
        /// Completion records entry history; check CanProceed for current permission. Hooks must not await their own entry.
        /// </summary>
        public UniTask WaitForEntryAsync(CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            RejectHookReentry();
            if (_entry == null) throw new InvalidOperationException("Start first entry before waiting for it.");
            return _entry.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>
        /// Replaces a Ready primary through its explicit loader, preserving common systems.
        /// Additive prepares the candidate before releasing the old primary; Single gracefully releases old roots before native load.
        /// Caller cancellation affects only this wait. Overlapping commands and Faulted owners are rejected before side effects.
        /// </summary>
        public UniTask ReplacePrimaryAsync(SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            var operation = RequireAccepted(BeginRequest(new SceneTransitionRequest(SceneTransitionKind.ReplacePrimary, target,
                sourceScene: GameScene, mode: mode), cancellationToken));
            return operation.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>Replaces the primary with a Build Scene path under the explicit-target replacement contract.</summary>
        public UniTask ReplacePrimaryAsync(string scenePath, LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            return ReplacePrimaryAsync(SceneTarget.BuildScene(scenePath), mode, cancellationToken);
        }

        /// <summary>
        /// Adds one Additive child under a prepared registered parent, retaining existing roots.
        /// Returns this command's actual Scene. Priority is metadata; activation is explicit and caller cancellation only stops waiting.
        /// Rejects overlaps, duplicate assets and invalid parents before side effects. Failed candidates remain owned until cleanup succeeds.
        /// </summary>
        public UniTask<Scene> AddDerivedAsync(SceneTarget target, Scene parent, bool activate = false, int priority = 0,
            CancellationToken cancellationToken = default)
        {
            var operation = RequireAccepted(BeginRequest(new SceneTransitionRequest(SceneTransitionKind.AddDerived, target,
                sourceScene: parent, activate: activate, priority: priority), cancellationToken));
            return WaitForAddedSceneAsync(operation, cancellationToken);
        }

        /// <summary>Adds a Build Scene child under the explicit-target ownership and caller-wait contract.</summary>
        public UniTask<Scene> AddDerivedAsync(string scenePath, Scene parent, bool activate = false, int priority = 0,
            CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            return AddDerivedAsync(SceneTarget.BuildScene(scenePath), parent, activate, priority, cancellationToken);
        }

        /// <summary>
        /// Gracefully removes a derived subtree child-first when requested by itself or a registered ancestor.
        /// The same pending target shares completion after requester authorization; caller cancellation only stops its wait.
        /// Other overlapping commands are rejected before effects; unload failures remain observable through explicit shutdown.
        /// </summary>
        public UniTask RemoveDerivedAsync(Scene scene, Scene requester, CancellationToken cancellationToken = default)
        {
            var operation = RequireAccepted(BeginRequest(new SceneTransitionRequest(SceneTransitionKind.RemoveDerived,
                sourceScene: requester, destinationScene: scene), cancellationToken));
            return operation.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private static Operation RequireAccepted(Operation operation) => operation ??
            throw new SceneTransitionRejectedException("An attached scene transition condition rejected the request.");

        private Operation BeginRequest(SceneTransitionRequest request, CancellationToken callerToken)
        {
            EnsureMainThread();
            RejectHookReentry();
            if (_stopping) throw new ObjectDisposedException(nameof(GameSceneManager));
            callerToken.ThrowIfCancellationRequested();
            SceneTransitionSettings.ValidateKindAndMode(request.Kind, request.Mode);
            OwnedPrimary parent = null;
            OwnedPrimary removalTarget = null;
            OwnedPrimary requester = null;
            if (request.Kind == SceneTransitionKind.RemoveDerived)
            {
                bool samePendingRemoval = _transition != null && _transition.Task.Status == UniTaskStatus.Pending &&
                    _operation.Kind == OperationKind.Remove && _operation.RemovalTarget.Result.Scene == request.DestinationScene;
                removalTarget = samePendingRemoval ? _operation.RemovalTarget : FindRegistered(request.DestinationScene);
                requester = samePendingRemoval && request.SourceScene == request.DestinationScene ? removalTarget : FindRegistered(request.SourceScene);
                if (removalTarget == null || removalTarget.Role != SceneRegistrationRole.Derived || requester == null ||
                    !IsAncestorOrSelf(requester, removalTarget))
                    throw new InvalidOperationException("Only the derived scene itself or a registered ancestor may remove it.");
                // This is observation of the accepted operation, including after unload, not another policy request.
                if (samePendingRemoval) return _operation;
            }
            RejectPendingOperation();
            SceneTransitionSettings.ValidateRequiredIds(request.RequiredConditionIds);
            if (request.Kind == SceneTransitionKind.FirstEntry)
            {
                if (_entry != null) throw new InvalidOperationException("First entry already has an owner; do not reenter it.");
                ValidateCommonLifetime();
                if (request.SourceScene != default && request.SourceScene != _commonScene)
                    throw new InvalidOperationException("First entry must start from the injected common scene.");
            }
            else
            {
                RequireReady();
                if (request.Kind == SceneTransitionKind.ReplacePrimary && request.SourceScene != GameScene)
                    throw new InvalidOperationException("Replace requires the exact current primary instance.");
                if (request.Kind == SceneTransitionKind.AddDerived)
                {
                    parent = FindRegistered(request.SourceScene);
                    if (parent == null || !IsOwnerPrepared(parent))
                        throw new InvalidOperationException("A derived scene requires a prepared registered parent.");
                }
            }
            ISceneLoader loader = null;
            var initialScenes = GetLoadedScenes();
            if (request.Kind != SceneTransitionKind.RemoveDerived)
            {
                if (request.DestinationScene != default)
                    throw new ArgumentException("Load destinations are acquired by the owner, not supplied as an existing instance.");
                request.Target.Validate();
                loader = request.Target.Source == SceneSource.BuildScene ? _buildLoader : _addressableLoader;
                loader.Validate(request.Target);
                if (SceneManager.GetSceneByPath(request.Target.ScenePath).isLoaded)
                    throw new InvalidOperationException("The destination scene is already loaded.");
                if (request.Mode == LoadSceneMode.Single)
                {
                    if (!BootstrapSystem.IsPersistent(_commonHost) || _retainedScenes.Length != 0)
                        throw new InvalidOperationException("Single requires a persistent common owner and no retained normal scenes.");
                    if (request.Kind == SceneTransitionKind.FirstEntry && (initialScenes.Length != 1 || GetRootHosts(initialScenes[0]).Length != 0))
                        throw new InvalidOperationException("First Single entry requires only Bootstrap, without unregistered scene roots.");
                }
            }
            MonoBehaviour[] affected;
            switch (request.Kind)
            {
                case SceneTransitionKind.FirstEntry: affected = new[] { _commonHost }; break;
                case SceneTransitionKind.ReplacePrimary: affected = GetSubtreeChildFirst(_primary).Select(owner => (MonoBehaviour)owner.Root).ToArray(); break;
                case SceneTransitionKind.AddDerived: affected = new[] { (MonoBehaviour)parent.Root }; break;
                default:
                    affected = GetSubtreeChildFirst(removalTarget).Select(owner => (MonoBehaviour)owner.Root)
                        .Concat(new[] { (MonoBehaviour)requester.Root, (MonoBehaviour)removalTarget.Parent.Root }).Distinct().ToArray();
                    break;
            }
            var policy = CapturePolicy(request, affected);
            if (!EvaluatePolicy(policy, default)) return null;
            _initialScenes = initialScenes;
            _previousActiveScene = SceneManager.GetActiveScene();
            if (request.Kind == SceneTransitionKind.FirstEntry)
            {
                _expectedActiveScene = _previousActiveScene;
                _retainedScenes = request.Mode == LoadSceneMode.Single ? Array.Empty<Scene>() : initialScenes;
            }
            _loader = loader;
            _target = request.Target;
            _mode = request.Mode;
            var kind = request.Kind == SceneTransitionKind.FirstEntry ? OperationKind.Entry :
                request.Kind == SceneTransitionKind.ReplacePrimary ? OperationKind.Replace :
                request.Kind == SceneTransitionKind.AddDerived ? OperationKind.Add : OperationKind.Remove;
            return StartOperation(kind, parent, request.Activate, request.Priority, removalTarget, policy);
        }

        private PolicySnapshot CapturePolicy(SceneTransitionRequest request, MonoBehaviour[] affected)
        {
            _evaluating = true;
            try
            {
                var roots = new List<RootPolicy>();
                var suppliedIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var host in affected.Distinct())
                {
                    if (host == null) throw new InvalidOperationException("An affected condition root was destroyed.");
                    var conditions = host.GetComponents<SceneTransitionCondition>();
                    var ids = new HashSet<string>(StringComparer.Ordinal);
                    var orderedIds = new List<string>();
                    foreach (var condition in conditions)
                    {
                        string id = condition.ConditionId;
                        if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                            throw new InvalidOperationException("Attached condition IDs must be nonempty and unique within their root.");
                        orderedIds.Add(id);
                        suppliedIds.Add(id);
                    }
                    roots.Add(new RootPolicy(host, conditions, orderedIds.ToArray()));
                }
                foreach (var id in request.RequiredConditionIds)
                    if (!suppliedIds.Contains(id)) throw new InvalidOperationException("An affected root must provide required condition: " + id);
                return new PolicySnapshot(request, roots.ToArray(), request.Kind == SceneTransitionKind.FirstEntry ? _commonScene : request.SourceScene);
            }
            finally { _evaluating = false; }
        }

        private bool EvaluatePolicy(PolicySnapshot policy, Scene actualDestination)
        {
            _evaluating = true;
            try
            {
                var context = new SceneTransitionContext(policy.Request, policy.SourceScene,
                    policy.Request.Kind == SceneTransitionKind.RemoveDerived ? policy.Request.DestinationScene : actualDestination,
                    policy.Roots.Select(root => root.Scene));
                bool allowed = true;
                // Validate the complete fixed composition before evaluating any business state.
                foreach (var root in policy.Roots)
                {
                    if (root.Host == null || root.Host.gameObject.scene != root.Scene || root.Host.transform.parent != null ||
                        !root.Host.GetComponents<SceneTransitionCondition>().SequenceEqual(root.Conditions))
                        throw new InvalidOperationException("Condition root configuration changed during the transition.");
                    for (int index = 0; index < root.Conditions.Length; ++index)
                        if (root.Conditions[index] == null || root.Conditions[index].ConditionId != root.Ids[index])
                            throw new InvalidOperationException("Condition ID configuration changed during the transition.");
                }
                foreach (var root in policy.Roots)
                    foreach (var condition in root.Conditions) allowed &= condition.Evaluate(context);
                return allowed;
            }
            finally { _evaluating = false; }
        }

        private void RecheckPolicy(Operation operation, Scene actualDestination)
        {
            if (!EvaluatePolicy(operation.Policy, actualDestination))
            {
                operation.Cancellation.Cancel();
                throw new OperationCanceledException("Scene transition conditions no longer permit the accepted request.", operation.Cancellation.Token);
            }
        }

        private static async UniTask<Scene> WaitForAddedSceneAsync(Operation operation, CancellationToken token)
        {
            await operation.Completion.Task.AttachExternalCancellation(token);
            return operation.AddedScene;
        }

        private void RejectPendingOperation()
        {
            if (_stopping) throw new ObjectDisposedException(nameof(GameSceneManager));
            if (_transition != null && _transition.Task.Status == UniTaskStatus.Pending)
                throw new InvalidOperationException("A scene transition already has an owner.");
        }

        private void RequireReady()
        {
            if (_entry == null || _entry.Task.Status != UniTaskStatus.Succeeded || !CanProceed)
                throw new InvalidOperationException("Change only a prepared registered tree in Ready state.");
            ValidateCommonLifetime();
        }

        /// <summary>Resolves an injected definition snapshot; false means synchronous preflight refusal only, while configuration/execution/cancellation throws.</summary>
        public UniTask<bool> TryTransitionAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            RejectHookReentry();
            cancellationToken.ThrowIfCancellationRequested();
            if (_definitions == null || string.IsNullOrWhiteSpace(definitionId) || !_definitions.TryGetValue(definitionId, out var definition))
                throw new InvalidOperationException("Select an existing ID from the injected transition settings.");
            var source = definition.Kind == SceneTransitionKind.FirstEntry ? _commonScene : ResolveDefinedScene(definition.SourceScenePath);
            var destination = definition.Kind == SceneTransitionKind.RemoveDerived ? ResolveDefinedScene(definition.Target.ScenePath) : default;
            return TryTransitionAsync(new SceneTransitionRequest(definition.Kind, definition.Target, source, destination,
                definition.Mode, definition.Activate, definition.Priority, definition.RequiredConditionIds.ToArray()), cancellationToken);
        }

        /// <summary>Executes an explicit instance request under the same root policy as convenience/ID commands; false is preflight refusal only.</summary>
        public UniTask<bool> TryTransitionAsync(SceneTransitionRequest request, CancellationToken cancellationToken = default)
        {
            var operation = BeginRequest(request, cancellationToken);
            return operation == null ? UniTask.FromResult(false) : WaitForSuccessAsync(operation, cancellationToken);
        }

        private static async UniTask<bool> WaitForSuccessAsync(Operation operation, CancellationToken token)
        {
            await operation.Completion.Task.AttachExternalCancellation(token);
            return true;
        }

        private Scene ResolveDefinedScene(string path)
        {
            var registered = GetRegisteredOwners().SingleOrDefault(owner => owner.Result.Target.ScenePath == path);
            if (registered != null) return registered.Result.Scene;
            if (_transition != null && _transition.Task.Status == UniTaskStatus.Pending && _operation.Kind == OperationKind.Remove &&
                _operation.RemovalTarget.Result.Target.ScenePath == path) return _operation.RemovalTarget.Result.Scene;
            throw new InvalidOperationException("The definition requires a registered scene instance: " + path);
        }

        /// <summary>Waits for the last accepted transition without issuing a command; entry history remains available separately.</summary>
        public UniTask WaitForTransitionAsync(CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            RejectHookReentry();
            if (_transition == null) throw new InvalidOperationException("Start a transition before waiting for it.");
            return _transition.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>Requests actual owner cancellation. Native loads finish before owned candidate cleanup.</summary>
        public void CancelTransition()
        {
            EnsureMainThread();
            if (_evaluating) throw new InvalidOperationException("A condition cannot cancel the transition it is evaluating.");
            if (_transition != null && _transition.Task.Status == UniTaskStatus.Pending) _transitionCancellation?.Cancel();
        }

        /// <summary>
        /// Shares uncancelled shutdown, waits for late transition work, releases every owned scene and retains common systems.
        /// Throws and records a loaded-but-unprepared last Single scene when Unity cannot unload it. No fallback scene is created.
        /// Await before destroying the common owner; never await from a hook participating in this operation.
        /// </summary>
        public UniTask ShutdownAsync()
        {
            EnsureMainThread();
            RejectHookReentry();
            if (_shutdown != null) return _shutdown.Task;
            _stopping = true;
            _shutdown = new UniTaskCompletionSource();
            RunShutdownAsync().Forget();
            return _shutdown.Task;
        }

        private Operation StartOperation(OperationKind kind, OwnedPrimary parent = null, bool activate = false,
            int priority = 0, OwnedPrimary removalTarget = null, PolicySnapshot policy = null)
        {
            var operation = new Operation(kind, parent, activate, priority, removalTarget, policy);
            _operation = operation;
            _transition = operation.Completion;
            _transitionCancellation = operation.Cancellation;
            if (kind == OperationKind.Entry) _entry = operation.Completion;
            _failureNotified = false;
            State = SceneTransitionState.Covering;
            RunOperationAsync(operation).Forget();
            return operation;
        }

        private async UniTask RunOperationAsync(Operation operation)
        {
            var completion = operation.Completion;
            var cancellation = operation.Cancellation;
            Exception reported = null;
            try
            {
                if (operation.Kind != OperationKind.Remove && _callbacks != null)
                {
                    bool previous = _dispatching;
                    _dispatching = true;
                    try { operation.UsesLoadingPresentation = _callbacks.UsesLoadingPresentation(operation.Context); }
                    finally { _dispatching = previous; }
                }
                var flow = new SceneRootFlow(new GuardedCommonRoot(this), CoverAsync, RevealAsync);
                await flow.PrepareAndProceedAsync(token => operation.Kind == OperationKind.Remove
                    ? RemoveSubtreeAsync(operation, token) : operation.UsesLoadingPresentation
                    ? LoadWithPresentationAsync(operation, token) : LoadAndPrepareAsync(operation, token), cancellation.Token);
                if (operation.Kind == OperationKind.Add)
                {
                    operation.AddedScene = _candidate.Result.Scene;
                    _derived.Add(_candidate);
                    _candidate = null;
                }
                else if (operation.Kind != OperationKind.Remove)
                {
                    _primary = _candidate;
                    _candidate = null;
                    GameScene = _primary.Result.Scene;
                }
                PublishPhase(SceneTransitionState.Ready, notify: false);
            }
            catch (Exception failure)
            {
                FailurePhase = State;
                reported = failure;
                if (operation.LoadingUiExposed)
                {
                    try
                    {
                        await InvokeAsync(() => RequireCallbacks().ShowCoverAsync(CancellationToken.None));
                        operation.LoadingUiExposed = false;
                    }
                    catch (Exception coverFailure) { reported = new AggregateException(reported, coverFailure); }
                }
                try
                {
                    await CleanupCandidateAsync();
                }
                catch (Exception cleanupFailure)
                {
                    reported = new AggregateException(reported, cleanupFailure);
                }
                try { await ReleaseLoadingUiAsync(operation); }
                catch (Exception uiFailure) { reported = new AggregateException(reported, uiFailure); }
                reported = RecordFailure(reported);
            }
            finally
            {
                // Completion resumes callers synchronously; dispose this operation before they can start another one.
                if (ReferenceEquals(_transitionCancellation, cancellation)) _transitionCancellation = null;
                cancellation.Dispose();
            }
            if (reported == null) completion.TrySetResult();
            else if (reported is OperationCanceledException cancelled) completion.TrySetCanceled(cancelled.CancellationToken);
            else completion.TrySetException(reported);
        }

        private async UniTask CoverAsync(CancellationToken token)
        {
            bool restoring = State == SceneTransitionState.Revealing;
            if (!restoring) PublishPhase(SceneTransitionState.Covering, notify: false);
            await InvokeAsync(() => _callbacks != null ? _callbacks.ShowCoverAsync(token) : UniTask.CompletedTask);
            if (_operation != null) _operation.LoadingUiExposed = false;
            if (!restoring)
            {
                // Progress/UI errors must not prevent the first protective cover from being attempted.
                PublishPhase(SceneTransitionState.Covering);
                PublishPhase(SceneTransitionState.PreparingCommon);
            }
        }

        private async UniTask RevealAsync(CancellationToken token)
        {
            var operation = _operation;
            ValidatePreparedOwnership();
            PublishPhase(SceneTransitionState.Revealing);
            await InvokeAsync(() => _callbacks != null ? _callbacks.HideCoverAsync(token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            // Entry/add keep their affected roots alive through reveal; releasing commands cannot reevaluate retired roots.
            if (operation.Kind == OperationKind.Entry || operation.Kind == OperationKind.Add)
                RecheckPolicy(operation, LoadedScene);
        }

        private async UniTask LoadAndPrepareAsync(Operation operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidateCommonLifetime();
            ValidateInventory(_initialScenes);
            if (_primary != null && !IsOwnerPrepared(_primary))
                throw new InvalidOperationException("The previous primary lost its prepared ownership.");
            if (SceneManager.GetActiveScene() != _previousActiveScene)
                throw new InvalidOperationException("The active scene changed before loading.");
            if (_mode == LoadSceneMode.Single && _primary != null)
            {
                PublishPhase(SceneTransitionState.Stopping);
                token.ThrowIfCancellationRequested();
                RecheckPolicy(operation, default);
                ValidateCommonLifetime();
                GameScene = default;
                await ShutdownSubtreeRootsAsync(_primary);
                token.ThrowIfCancellationRequested();
                ValidateCommonLifetime();
                ValidateInventory(_initialScenes);
                ValidateActiveScene();
            }
            PublishPhase(SceneTransitionState.Loading);
            // The loader cannot abandon native work. Retain its actual result before observing owner cancellation.
            using var observer = new SceneLoadProgressObserver(progress =>
            {
                if (!ReferenceEquals(_operation, operation) || operation.Cancellation.IsCancellationRequested) return;
                PublishPhase(progress.Stage == SceneLoadStage.ResolvingTarget
                    ? SceneTransitionState.ResolvingTarget : SceneTransitionState.Loading, progress.Ratio);
            });
            ResourceManagement.LoadedScene result;
            try
            {
                result = _loader is ISceneProgressLoader progressLoader
                    ? await progressLoader.LoadAsync(_target, _mode, observer)
                    : await _loader.LoadAsync(_target, _mode);
            }
            catch (Exception backendFailure)
            {
                if (observer.Failure != null) throw new AggregateException(backendFailure, observer.Failure);
                throw;
            }
            if (result == null) throw new InvalidOperationException("The scene loader returned no owned result.");
            _candidate = new OwnedPrimary(result, operation.Kind == OperationKind.Add ? operation.Parent : null,
                operation.Kind == OperationKind.Add ? SceneRegistrationRole.Derived : SceneRegistrationRole.Primary, operation.Priority);
            if (_mode == LoadSceneMode.Single && _primary != null)
            {
                // Single has unloaded the old scene. Its backend result still owns completion/release observation.
                await DrainSingleResultsAsync(_primary);
            }
            if (observer.Failure != null) throw observer.Failure;
            if (result.Target.Source != _target.Source || result.Target.ScenePath != _target.ScenePath ||
                result.Target.AddressableKey != _target.AddressableKey || LoadedScene.path != _target.ScenePath)
                throw new InvalidOperationException("The scene loader returned a different target or scene asset.");
            token.ThrowIfCancellationRequested();
            if (_mode == LoadSceneMode.Additive) ValidateActiveScene();
            var activeScene = operation.Kind == OperationKind.Add && !operation.Activate ? _previousActiveScene : LoadedScene;
            if (!LoadedScene.IsValid() || !LoadedScene.isLoaded || !activeScene.IsValid() || !activeScene.isLoaded ||
                (SceneManager.GetActiveScene() != activeScene && !SceneManager.SetActiveScene(activeScene)))
                throw new InvalidOperationException("The loaded game scene could not become active: " +
                    LoadedScene.path + " (valid=" + LoadedScene.IsValid() + ", loaded=" + LoadedScene.isLoaded + ").");
            _expectedActiveScene = activeScene;
            var hosts = GetRootHosts(LoadedScene);
            if (hosts.Length != 1) throw new InvalidOperationException("Game scene requires exactly one lifecycle root.");
            BootstrapSystem.ValidateSceneRoot(hosts[0], LoadedScene);
            _candidate.Root = (ISceneRoot)hosts[0];
            PublishPhase(SceneTransitionState.Configuring);
            await InvokeAsync(() => _callbacks != null ? _callbacks.ConfigureSceneAsync(LoadedScene, _candidate.Root, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            PublishPhase(SceneTransitionState.PreparingScene);
            await InvokeAsync(() => _candidate.Root.PrepareAsync(token));
            ValidatePreparedOwnership();
            PublishPhase(SceneTransitionState.PreparingPresentation);
            await InvokeAsync(() => _callbacks != null ? _callbacks.PreparePresentationAsync(LoadedScene, _candidate.Root, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            operation.DestinationPrepared = true;
            if (!operation.UsesLoadingPresentation)
                await FinalizePreviousAsync(operation, token);
        }

        private SceneTransitionCallbacks RequireCallbacks() => _callbacks != null ? _callbacks :
            throw new InvalidOperationException("Loading presentation owner was destroyed.");

        private async UniTask LoadWithPresentationAsync(Operation operation, CancellationToken token)
        {
            ValidateCommonLifetime();
            PublishPhase(SceneTransitionState.PreparingLoadingPresentation);
            operation.LoadingUiStarted = true;
            await InvokeAsync(() => RequireCallbacks().PrepareLoadingPresentationAsync(operation.Context, token));
            token.ThrowIfCancellationRequested();
            ValidateCommonLifetime();
            PublishPhase(SceneTransitionState.RevealingLoadingPresentation);
            // A hook can expose the UI before throwing; recovery must cover that partial reveal too.
            operation.LoadingUiExposed = true;
            await InvokeAsync(() => RequireCallbacks().RevealLoadingPresentationAsync(operation.Context, token));
            token.ThrowIfCancellationRequested();
            await LoadAndPrepareAsync(operation, token);
            PublishPhase(SceneTransitionState.AwaitingProceed);
            await InvokeAsync(() => RequireCallbacks().WaitForProceedAsync(operation.Context, token))
                .AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            RecheckLivePolicy(operation);
            PublishPhase(SceneTransitionState.Finalizing);
            await InvokeAsync(() => RequireCallbacks().ShowCoverAsync(token));
            operation.LoadingUiExposed = false;
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            await FinalizePreviousAsync(operation, token);
            await ReleaseLoadingUiAsync(operation);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
        }

        private void RecheckLivePolicy(Operation operation)
        {
            // Single replacement has already retired the old affected roots; never evaluate their dead policy.
            if (operation.Kind == OperationKind.Entry || operation.Kind == OperationKind.Add ||
                (_mode == LoadSceneMode.Additive && _primary != null))
                RecheckPolicy(operation, LoadedScene);
        }

        private async UniTask FinalizePreviousAsync(Operation operation, CancellationToken token)
        {
            if (_primary != null && operation.Kind != OperationKind.Add)
            {
                PublishPhase(SceneTransitionState.Stopping);
                token.ThrowIfCancellationRequested();
                ValidatePreparedOwnership();
                RecheckPolicy(operation, LoadedScene);
                GameScene = default;
                await ReleaseSubtreeAsync(_primary, false, ValidatePreparedOwnership);
                token.ThrowIfCancellationRequested();
                ValidatePreparedOwnership();
            }
            if (operation.Kind == OperationKind.Entry || operation.Kind == OperationKind.Add)
                RecheckPolicy(operation, LoadedScene);
        }

        private async UniTask ReleaseLoadingUiAsync(Operation operation)
        {
            if (!operation.LoadingUiStarted || operation.LoadingUiReleaseStarted) return;
            operation.LoadingUiReleaseStarted = true;
            await InvokeAsync(() => RequireCallbacks().ReleaseLoadingPresentationAsync(operation.Context));
        }

        private async UniTask RemoveSubtreeAsync(Operation operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            ValidateInventory(_initialScenes);
            RecheckPolicy(operation, operation.RemovalTarget.Result.Scene);
            var target = operation.RemovalTarget;
            // Select the nearest surviving parent before shutting down any active descendant.
            var activeOwner = FindRegistered(_expectedActiveScene);
            if (activeOwner != null && IsAncestorOrSelf(target, activeOwner))
            {
                var survivingParent = target.Parent;
                if (survivingParent == null || !IsOwnerPrepared(survivingParent) ||
                    !SceneManager.SetActiveScene(survivingParent.Result.Scene))
                    throw new InvalidOperationException("Cannot activate the surviving registered parent.");
                _expectedActiveScene = survivingParent.Result.Scene;
            }
            PublishPhase(SceneTransitionState.Stopping);
            // Cancellation cannot abandon a begun subtree release. Report it only after all cleanup completes.
            await ReleaseSubtreeAsync(target, true, ValidatePreparedOwnership);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
        }

        private OwnedPrimary[] GetSubtreeChildFirst(OwnedPrimary root)
        {
            var ordered = new List<OwnedPrimary>();
            AddSubtreeChildFirst(root, ordered);
            return ordered.ToArray();
        }

        private void AddSubtreeChildFirst(OwnedPrimary owner, List<OwnedPrimary> ordered)
        {
            foreach (var child in _derived.Where(child => ReferenceEquals(child.Parent, owner)))
                AddSubtreeChildFirst(child, ordered);
            ordered.Add(owner);
        }

        private async UniTask ShutdownSubtreeRootsAsync(OwnedPrimary root)
        {
            var failures = new List<Exception>();
            foreach (var owner in GetSubtreeChildFirst(root))
            {
                try { await ShutdownOwnerRootsAsync(owner); }
                catch (Exception failure) { failures.Add(failure); }
            }
            if (failures.Count != 0) throw new AggregateException("Scene subtree shutdown failed.", failures);
        }

        private async UniTask DrainSingleResultsAsync(OwnedPrimary root)
        {
            var failures = new List<Exception>();
            foreach (var owner in GetSubtreeChildFirst(root))
            {
                try { await owner.Result.UnloadAsync(); }
                catch (Exception failure) { failures.Add(failure); }
                finally { ForgetUnloadedOwner(owner); }
            }
            if (failures.Count != 0) throw new AggregateException("Single backend release failed.", failures);
        }

        private async UniTask ReleaseSubtreeAsync(OwnedPrimary root, bool unloadAfterRootFailure, Action validate = null)
        {
            var failures = new List<Exception>();
            foreach (var owner in GetSubtreeChildFirst(root))
            {
                bool rootSucceeded = false;
                try
                {
                    await ShutdownOwnerRootsAsync(owner);
                    rootSucceeded = true;
                    validate?.Invoke();
                }
                catch (Exception failure) { failures.Add(failure); }
                if (!rootSucceeded && !unloadAfterRootFailure) continue;
                try
                {
                    await UnloadOwnerAsync(owner);
                    ForgetUnloadedOwner(owner);
                    validate?.Invoke();
                }
                catch (Exception failure) { failures.Add(failure); }
            }
            if (failures.Count != 0) throw new AggregateException("Scene subtree cleanup failed.", failures);
        }

        private void ForgetUnloadedOwner(OwnedPrimary owner)
        {
            if (!owner.Result.IsUnloaded) return;
            if (ReferenceEquals(owner, _primary)) _primary = null;
            else _derived.Remove(owner);
        }

        private async UniTask ShutdownOwnerRootsAsync(OwnedPrimary owner)
        {
            owner.ShutdownStarted = true;
            var failures = new List<Exception>();
            var scene = owner.Result.Scene;
            var roots = scene.IsValid() && scene.isLoaded ? GetRootHosts(scene).Cast<ISceneRoot>().ToList() : new List<ISceneRoot>();
            if (owner.Root is MonoBehaviour host && host != null && !roots.Contains(owner.Root)) roots.Add(owner.Root);
            foreach (var root in roots.Where(root => !ReferenceEquals(root, _commonRoot)))
            {
                try
                {
                    await InvokeAsync(root.ShutdownAsync);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0) throw new AggregateException("Game root shutdown failed.", failures);
        }

        private async UniTask UnloadOwnerAsync(OwnedPrimary owner)
        {
            var scene = owner.Result.Scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                if (_commonHost != null && _commonHost.gameObject.scene == scene)
                    throw new InvalidOperationException("Cannot unload a candidate containing the borrowed common owner.");
                if (ReferenceEquals(owner, _candidate) && SceneManager.GetActiveScene() == scene && _previousActiveScene != scene &&
                    _previousActiveScene.IsValid() && _previousActiveScene.isLoaded &&
                    (_primary == null || !_primary.ShutdownStarted))
                {
                    if (!SceneManager.SetActiveScene(_previousActiveScene))
                        throw new InvalidOperationException("Could not restore the previous active scene.");
                    _expectedActiveScene = _previousActiveScene;
                }
                if (GetLoadedScenes().Length <= 1)
                    throw new InvalidOperationException("Unity cannot unload the last normal scene; it remains loaded but unprepared.");
            }
            await owner.Result.UnloadAsync();
        }

        private async UniTask CleanupOwnerAsync(OwnedPrimary owner)
        {
            var failures = new List<Exception>();
            try
            {
                await ShutdownOwnerRootsAsync(owner);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                await UnloadOwnerAsync(owner);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (failures.Count != 0) throw new AggregateException("Game scene cleanup failed.", failures);
        }

        private async UniTask CleanupCandidateAsync()
        {
            if (_candidate == null) return;
            var candidate = _candidate;
            try
            {
                await CleanupOwnerAsync(candidate);
            }
            finally
            {
                if (candidate.Result.IsUnloaded) _candidate = null;
            }
        }

        private async UniTask ReleaseGameAsync()
        {
            GameScene = default;
            var failures = new List<Exception>();
            try
            {
                await CleanupCandidateAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            foreach (var owner in GetRegisteredOwnersChildFirst())
            {
                try
                {
                    await CleanupOwnerAsync(owner);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
                finally
                {
                    ForgetUnloadedOwner(owner);
                }
            }
            if (failures.Count != 0) throw new AggregateException("Game scene cleanup failed.", failures);
        }

        private async UniTask RunShutdownAsync()
        {
            var failures = new List<Exception>();
            try
            {
                CancelTransition();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (_transition != null)
            {
                try
                {
                    await _transition.Task;
                }
                catch (Exception)
                {
                    /* Transition failures belong to their original awaiters. */
                }
            }
            bool wasReady = GameScene.IsValid();
            GameScene = default;
            State = SceneTransitionState.Stopping;
            if (wasReady)
            {
                try
                {
                    await InvokeAsync(() => _callbacks != null ? _callbacks.ShowCoverAsync(CancellationToken.None) : UniTask.CompletedTask);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            try
            {
                await ReleaseGameAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (failures.Count == 0)
            {
                State = SceneTransitionState.Stopped;
                _shutdown.TrySetResult();
            }
            else
            {
                FailurePhase = SceneTransitionState.Stopping;
                _shutdown.TrySetException(RecordFailure(new AggregateException("Scene manager shutdown failed.", failures)));
            }
        }

        private void ValidateCommonLifetime()
        {
            if (_commonHost == null || !_commonRoot.IsReady)
                throw new InvalidOperationException("The common owner is no longer installed.");
            if (_commonHost.gameObject.scene != _commonScene)
                throw new InvalidOperationException("The common root changed its owning scene.");
            if (_hasCallbacks && _callbacks == null)
                throw new InvalidOperationException("The configured callbacks were destroyed during their owner's lifetime.");
            if (BootstrapSystem.IsPersistent(_commonHost))
            {
                if (GetLoadedScenes().Contains(_commonHost.gameObject.scene))
                    throw new InvalidOperationException("The common root has not entered its persistent lifetime.");
                if (_callbacks != null && !_callbacks.transform.IsChildOf(_commonHost.transform))
                    throw new InvalidOperationException("Callbacks must survive under the persistent common root.");
            }
            else if (_callbacks != null && _callbacks.gameObject.scene != _commonHost.gameObject.scene)
                throw new InvalidOperationException("Callbacks must belong to the retained common scene.");
        }

        private void ValidatePreparedOwnership()
        {
            ValidateCommonLifetime();
            if (!_commonRoot.IsPrepared || !IsGamePrepared || !LoadedScene.IsValid() || !LoadedScene.isLoaded)
                throw new InvalidOperationException("Both owning roots and the game scene must remain prepared.");
            ValidateActiveScene();
            if (GetRegisteredOwners().Any(owner => !owner.ShutdownStarted && !IsOwnerPrepared(owner)))
                throw new InvalidOperationException("A registered scene lost its prepared ownership.");
            ValidateInventory(ExpectedGameScenes());
        }

        private void ValidateActiveScene()
        {
            if (SceneManager.GetActiveScene() != _expectedActiveScene)
                throw new InvalidOperationException("The active scene changed outside the scene manager's ownership.");
        }

        private Scene[] ExpectedGameScenes() => _retainedScenes.Concat(GetOwners().Select(owner => owner.Result.Scene)).ToArray();

        private IEnumerable<OwnedPrimary> GetOwners()
        {
            foreach (var owner in GetRegisteredOwners()) yield return owner;
            if (_candidate != null) yield return _candidate;
        }

        private IEnumerable<OwnedPrimary> GetRegisteredOwners()
        {
            if (_primary != null) yield return _primary;
            foreach (var owner in _derived) yield return owner;
        }

        private OwnedPrimary[] GetRegisteredOwnersChildFirst()
        {
            // A failed parent unload may leave a residual child whose retired parent is no longer registered.
            return GetRegisteredOwners().OrderByDescending(GetDepth).ToArray();
        }

        private static int GetDepth(OwnedPrimary owner)
        {
            int depth = 0;
            for (var parent = owner.Parent; parent != null; parent = parent.Parent) ++depth;
            return depth;
        }

        private OwnedPrimary FindRegistered(Scene scene) => GetRegisteredOwners().FirstOrDefault(owner => owner.Result.Scene == scene);

        private static bool IsAncestorOrSelf(OwnedPrimary ancestor, OwnedPrimary owner)
        {
            for (var current = owner; current != null; current = current.Parent)
                if (ReferenceEquals(current, ancestor)) return true;
            return false;
        }

        private static bool IsOwnerPrepared(OwnedPrimary owner) => owner != null && owner.Root is MonoBehaviour host &&
            host != null && host.gameObject.scene == owner.Result.Scene && host.transform.parent == null &&
            owner.Result.Scene.IsValid() && owner.Result.Scene.isLoaded && owner.Root.IsPrepared;

        private sealed class OwnedPrimary
        {
            internal readonly MyLab.Core.ResourceManagement.LoadedScene Result;
            internal readonly OwnedPrimary Parent;
            internal readonly SceneRegistrationRole Role;
            internal readonly int Priority;
            internal ISceneRoot Root;
            internal bool ShutdownStarted;
            internal OwnedPrimary(MyLab.Core.ResourceManagement.LoadedScene result, OwnedPrimary parent,
                SceneRegistrationRole role, int priority)
            {
                Result = result;
                Parent = parent;
                Role = role;
                Priority = priority;
            }
        }

        private enum OperationKind { Entry, Replace, Add, Remove }

        private sealed class Operation
        {
            internal readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();
            internal readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            internal readonly OperationKind Kind;
            internal readonly SceneLoadingContext Context;
            internal bool DestinationPrepared;
            internal bool UsesLoadingPresentation;
            internal bool LoadingUiStarted;
            internal bool LoadingUiExposed;
            internal bool LoadingUiReleaseStarted;
            internal readonly OwnedPrimary Parent;
            internal readonly bool Activate;
            internal readonly int Priority;
            internal readonly OwnedPrimary RemovalTarget;
            internal readonly PolicySnapshot Policy;
            internal Scene AddedScene;

            internal Operation(OperationKind kind, OwnedPrimary parent, bool activate, int priority, OwnedPrimary removalTarget, PolicySnapshot policy)
            {
                Kind = kind;
                Context = new SceneLoadingContext(policy.Request);
                Parent = parent;
                Activate = activate;
                Priority = priority;
                RemovalTarget = removalTarget;
                Policy = policy;
            }
        }

        private sealed class RootPolicy
        {
            internal readonly MonoBehaviour Host;
            internal readonly Scene Scene;
            internal readonly SceneTransitionCondition[] Conditions;
            internal readonly string[] Ids;
            internal RootPolicy(MonoBehaviour host, SceneTransitionCondition[] conditions, string[] ids)
            {
                Host = host;
                Scene = host.gameObject.scene;
                Conditions = conditions;
                Ids = ids;
            }
        }

        private sealed class PolicySnapshot
        {
            internal readonly SceneTransitionRequest Request;
            internal readonly RootPolicy[] Roots;
            internal readonly Scene SourceScene;
            internal PolicySnapshot(SceneTransitionRequest request, RootPolicy[] roots, Scene sourceScene)
            {
                Request = request;
                Roots = roots;
                SourceScene = sourceScene;
            }
        }

        private static void ValidateInventory(Scene[] expected)
        {
            if (!InventoryMatches(expected))
                throw new InvalidOperationException("Loaded scenes changed outside the scene manager's ownership.");
        }

        private static bool InventoryMatches(Scene[] expected) => new HashSet<Scene>(GetLoadedScenes()).SetEquals(expected);

        private void PublishPhase(SceneTransitionState phase, float? ratio = null, bool notify = true)
        {
            State = phase;
            var operation = _operation;
            if (operation == null) return;
            var snapshot = new SceneTransitionProgress(operation.Context, phase, ratio,
                phase != SceneTransitionState.Faulted && operation.DestinationPrepared);
            Progress = snapshot;
            if (!notify || _callbacks == null) return;
            bool previous = _dispatching;
            _dispatching = true;
            try { _callbacks.ReportLoadingProgress(snapshot); }
            finally { _dispatching = previous; }
        }

        private Exception RecordFailure(Exception failure)
        {
            PublishPhase(SceneTransitionState.Faulted, notify: false);
            LastFailure = failure;
            if (!_failureNotified)
            {
                _failureNotified = true;
                try
                {
                    _dispatching = true;
                    if (_callbacks != null) _callbacks.OnFailure(failure);
                }
                catch (Exception callbackFailure)
                {
                    LastFailure = new AggregateException(failure, callbackFailure);
                }
                finally
                {
                    _dispatching = false;
                }
            }
            return LastFailure;
        }

        private UniTask InvokeAsync(Func<UniTask> hook)
        {
            // ponytail: guards synchronous dispatch; detecting self-waits after await needs task-context cycle tracking.
            bool previous = _dispatching;
            _dispatching = true;
            try
            {
                return hook();
            }
            finally
            {
                _dispatching = previous;
            }
        }

        private void RejectHookReentry()
        {
            if (_dispatching || _evaluating) throw new InvalidOperationException("A transition hook or condition cannot start, wait for or stop its own transition.");
        }

        // Guard the actual root invocation, including when SceneRootFlow resumes after an asynchronous cover.
        // Installers still receive their original host; this adapter only forwards flow calls.
        private sealed class GuardedCommonRoot : ISceneRoot
        {
            private readonly GameSceneManager _owner;

            internal GuardedCommonRoot(GameSceneManager owner) => _owner = owner;
            public GameObject RootObject => _owner._commonRoot.RootObject;
            public bool IsReady => _owner._commonRoot.IsReady;
            public bool IsPrepared => _owner._commonRoot.IsPrepared;
            public UniTask PrepareAsync(CancellationToken cancellationToken = default)
                => _owner.InvokeAsync(() => _owner._commonRoot.PrepareAsync(cancellationToken));
            public UniTask ShutdownAsync() => _owner.InvokeAsync(_owner._commonRoot.ShutdownAsync);
        }

        internal static MonoBehaviour[] GetRootHosts(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(component => component is ISceneRoot).ToArray();

        private static Scene[] GetLoadedScenes() => Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt).Where(scene => scene.isLoaded).ToArray();

        internal static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("Scene management must be used on Unity's main thread.");
        }
    }
}
