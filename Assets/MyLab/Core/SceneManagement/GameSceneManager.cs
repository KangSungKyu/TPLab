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
    /// <summary>Observable primary-transition phases. Faulted permits inspection and explicit shutdown only.</summary>
    public enum SceneTransitionState
    {
        Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene,
        PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted
    }

    /// <summary>
    /// Main-thread owner of native scene loading and graceful game-root release.
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
        private bool _failureNotified;

        /// <summary>Current owner phase; Ready is published only after preparation and reveal finish.</summary>
        public SceneTransitionState State { get; private set; }
        /// <summary>Phase of the latest execution failure, retained after explicit shutdown.</summary>
        public SceneTransitionState FailurePhase { get; private set; }
        /// <summary>Execution/cleanup/callback failure, or null before failure. Caller wait cancellation is excluded.</summary>
        public Exception LastFailure { get; private set; }
        /// <summary>Successfully revealed game scene, invalid before success and after shutdown starts.</summary>
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
        /// <summary>Project gameplay may proceed only while the revealed scene and both roots remain prepared.</summary>
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
            ISceneLoader buildLoader = null, ISceneLoader addressableLoader = null)
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
            cancellationToken.ThrowIfCancellationRequested();
            return EnterFirstSceneAsync(SceneTarget.BuildScene(scenePath), mode, cancellationToken);
        }

        /// <summary>Enters one explicit Build/Addressables target with the same first-entry ownership and caller-wait contract.</summary>
        public UniTask EnterFirstSceneAsync(SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            if (_stopping) throw new ObjectDisposedException(nameof(GameSceneManager));
            if (_entry != null) throw new InvalidOperationException("First entry already has an owner; do not reenter it.");
            cancellationToken.ThrowIfCancellationRequested();
            target.Validate();
            ValidateCommonLifetime();
            if (mode != LoadSceneMode.Additive && mode != LoadSceneMode.Single)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == LoadSceneMode.Single && !BootstrapSystem.IsPersistent(_commonHost))
                throw new InvalidOperationException("Single requires a persistent common root; it cannot retain Bootstrap.");
            _loader = target.Source == SceneSource.BuildScene ? _buildLoader : _addressableLoader;
            _loader.Validate(target);
            if (SceneManager.GetSceneByPath(target.ScenePath).isLoaded)
                throw new InvalidOperationException("The game scene is already loaded; the manager must own its load.");
            _initialScenes = GetLoadedScenes();
            if (mode == LoadSceneMode.Single && (_initialScenes.Length != 1 || GetRootHosts(_initialScenes[0]).Length != 0))
                throw new InvalidOperationException("First Single entry requires only Bootstrap, without unregistered scene roots.");
            _previousActiveScene = SceneManager.GetActiveScene();
            _expectedActiveScene = _previousActiveScene;
            _retainedScenes = mode == LoadSceneMode.Single ? Array.Empty<Scene>() : _initialScenes;
            _target = target;
            _mode = mode;
            StartOperation(OperationKind.Entry);
            return _entry.Task.AttachExternalCancellation(cancellationToken);
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
            EnsureMainThread();
            RejectHookReentry();
            if (_stopping) throw new ObjectDisposedException(nameof(GameSceneManager));
            if (_transition != null && _transition.Task.Status == UniTaskStatus.Pending)
                throw new InvalidOperationException("A scene transition already has an owner.");
            cancellationToken.ThrowIfCancellationRequested();
            if (_entry == null || _entry.Task.Status != UniTaskStatus.Succeeded || !CanProceed)
                throw new InvalidOperationException("Replace only a prepared, owned primary in Ready state.");
            target.Validate();
            if (mode != LoadSceneMode.Single && mode != LoadSceneMode.Additive)
                throw new ArgumentOutOfRangeException(nameof(mode));
            ValidateCommonLifetime();
            var loader = target.Source == SceneSource.BuildScene ? _buildLoader : _addressableLoader;
            loader.Validate(target);
            if (SceneManager.GetSceneByPath(target.ScenePath).isLoaded)
                throw new InvalidOperationException("The destination scene is already loaded.");
            if (mode == LoadSceneMode.Single && (!BootstrapSystem.IsPersistent(_commonHost) || _retainedScenes.Length != 0))
                throw new InvalidOperationException("Single requires a persistent common owner and no retained normal scenes.");
            _initialScenes = GetLoadedScenes();
            _previousActiveScene = SceneManager.GetActiveScene();
            _loader = loader;
            _target = target;
            _mode = mode;
            return StartOperation(OperationKind.Replace).Completion.Task.AttachExternalCancellation(cancellationToken);
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
            EnsureMainThread();
            RejectHookReentry();
            RejectPendingOperation();
            cancellationToken.ThrowIfCancellationRequested();
            RequireReady();
            var parentOwner = FindRegistered(parent);
            if (parentOwner == null || !IsOwnerPrepared(parentOwner))
                throw new InvalidOperationException("A derived scene requires a prepared registered parent.");
            target.Validate();
            var loader = target.Source == SceneSource.BuildScene ? _buildLoader : _addressableLoader;
            loader.Validate(target);
            if (SceneManager.GetSceneByPath(target.ScenePath).isLoaded)
                throw new InvalidOperationException("The destination scene is already loaded.");
            _initialScenes = GetLoadedScenes();
            _previousActiveScene = SceneManager.GetActiveScene();
            _loader = loader;
            _target = target;
            _mode = LoadSceneMode.Additive;
            var operation = StartOperation(OperationKind.Add, parentOwner, activate, priority);
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
            EnsureMainThread();
            RejectHookReentry();
            if (_stopping) throw new ObjectDisposedException(nameof(GameSceneManager));
            cancellationToken.ThrowIfCancellationRequested();
            // Unload retires registration before asynchronous reveal finishes; the accepted removal still owns completion.
            bool samePendingRemoval = _transition != null && _transition.Task.Status == UniTaskStatus.Pending &&
                _operation.Kind == OperationKind.Remove && _operation.RemovalTarget.Result.Scene == scene;
            var target = samePendingRemoval ? _operation.RemovalTarget : FindRegistered(scene);
            var requesterOwner = samePendingRemoval && requester == scene ? target : FindRegistered(requester);
            if (target == null || target.Role != SceneRegistrationRole.Derived || requesterOwner == null ||
                !IsAncestorOrSelf(requesterOwner, target))
                throw new InvalidOperationException("Only the derived scene itself or a registered ancestor may remove it.");
            if (_transition != null && _transition.Task.Status == UniTaskStatus.Pending)
            {
                if (_operation.Kind == OperationKind.Remove && ReferenceEquals(_operation.RemovalTarget, target))
                    return _transition.Task.AttachExternalCancellation(cancellationToken);
                throw new InvalidOperationException("A different scene transition already has an owner.");
            }
            RequireReady();
            _initialScenes = GetLoadedScenes();
            _previousActiveScene = SceneManager.GetActiveScene();
            return StartOperation(OperationKind.Remove, removalTarget: target).Completion.Task.AttachExternalCancellation(cancellationToken);
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
            int priority = 0, OwnedPrimary removalTarget = null)
        {
            var operation = new Operation(kind, parent, activate, priority, removalTarget);
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
                var flow = new SceneRootFlow(new GuardedCommonRoot(this), CoverAsync, RevealAsync);
                await flow.PrepareAndProceedAsync(token => operation.Kind == OperationKind.Remove
                    ? RemoveSubtreeAsync(operation, token) : LoadAndPrepareAsync(operation, token), cancellation.Token);
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
                State = SceneTransitionState.Ready;
            }
            catch (Exception failure)
            {
                FailurePhase = State;
                reported = failure;
                try
                {
                    await CleanupCandidateAsync();
                }
                catch (Exception cleanupFailure)
                {
                    reported = new AggregateException(reported, cleanupFailure);
                }
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
            if (!restoring) State = SceneTransitionState.Covering;
            await InvokeAsync(() => _callbacks != null ? _callbacks.ShowCoverAsync(token) : UniTask.CompletedTask);
            if (!restoring) State = SceneTransitionState.PreparingCommon;
        }

        private async UniTask RevealAsync(CancellationToken token)
        {
            ValidatePreparedOwnership();
            State = SceneTransitionState.Revealing;
            await InvokeAsync(() => _callbacks != null ? _callbacks.HideCoverAsync(token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
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
                State = SceneTransitionState.Stopping;
                GameScene = default;
                await ShutdownSubtreeRootsAsync(_primary);
                token.ThrowIfCancellationRequested();
                ValidateCommonLifetime();
                ValidateInventory(_initialScenes);
                ValidateActiveScene();
            }
            State = SceneTransitionState.Loading;
            // The loader cannot abandon native work. Retain its actual result before observing owner cancellation.
            var result = await _loader.LoadAsync(_target, _mode);
            if (result == null) throw new InvalidOperationException("The scene loader returned no owned result.");
            _candidate = new OwnedPrimary(result, operation.Kind == OperationKind.Add ? operation.Parent : null,
                operation.Kind == OperationKind.Add ? SceneRegistrationRole.Derived : SceneRegistrationRole.Primary, operation.Priority);
            if (_mode == LoadSceneMode.Single && _primary != null)
            {
                // Single has unloaded the old scene. Its backend result still owns completion/release observation.
                await DrainSingleResultsAsync(_primary);
            }
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
            State = SceneTransitionState.Configuring;
            await InvokeAsync(() => _callbacks != null ? _callbacks.ConfigureSceneAsync(LoadedScene, _candidate.Root, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            State = SceneTransitionState.PreparingScene;
            await InvokeAsync(() => _candidate.Root.PrepareAsync(token));
            ValidatePreparedOwnership();
            State = SceneTransitionState.PreparingPresentation;
            await InvokeAsync(() => _callbacks != null ? _callbacks.PreparePresentationAsync(LoadedScene, _candidate.Root, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            if (_primary != null && operation.Kind != OperationKind.Add)
            {
                State = SceneTransitionState.Stopping;
                GameScene = default;
                await ReleaseSubtreeAsync(_primary, false, ValidatePreparedOwnership);
                token.ThrowIfCancellationRequested();
                ValidatePreparedOwnership();
            }
        }

        private async UniTask RemoveSubtreeAsync(Operation operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
            ValidateInventory(_initialScenes);
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
            State = SceneTransitionState.Stopping;
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
            internal readonly OwnedPrimary Parent;
            internal readonly bool Activate;
            internal readonly int Priority;
            internal readonly OwnedPrimary RemovalTarget;
            internal Scene AddedScene;

            internal Operation(OperationKind kind, OwnedPrimary parent, bool activate, int priority, OwnedPrimary removalTarget)
            {
                Kind = kind;
                Parent = parent;
                Activate = activate;
                Priority = priority;
                RemovalTarget = removalTarget;
            }
        }

        private static void ValidateInventory(Scene[] expected)
        {
            if (!InventoryMatches(expected))
                throw new InvalidOperationException("Loaded scenes changed outside the scene manager's ownership.");
        }

        private static bool InventoryMatches(Scene[] expected) => new HashSet<Scene>(GetLoadedScenes()).SetEquals(expected);

        private Exception RecordFailure(Exception failure)
        {
            State = SceneTransitionState.Faulted;
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
            if (_dispatching) throw new InvalidOperationException("A transition hook cannot wait for or stop its own transition.");
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
