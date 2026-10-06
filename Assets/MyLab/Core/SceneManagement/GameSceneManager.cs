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
    /// <summary>Observable first-entry phases. Faulted permits inspection and explicit shutdown only.</summary>
    public enum SceneTransitionState
    {
        Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene,
        PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted
    }

    /// <summary>
    /// Main-thread owner of native scene loading and graceful game-root release.
    /// Borrows the common root and presentation; their external owner shuts them down after this manager.
    /// First entry is one attempt. Later primary replacement and derived scenes are separate phases.
    /// </summary>
    public sealed class GameSceneManager
    {
        private readonly MonoBehaviour _commonHost;
        private readonly ISceneRoot _commonRoot;
        private readonly Scene _commonScene;
        private readonly SceneTransitionCallbacks _callbacks;
        private readonly bool _hasCallbacks;
        private UniTaskCompletionSource _entry;
        private UniTaskCompletionSource _shutdown;
        private CancellationTokenSource _lifetime;
        private Scene[] _initialScenes;
        private Scene _previousActiveScene;
        private ISceneRoot _gameRoot;
        private readonly ISceneLoader _buildLoader;
        private readonly ISceneLoader _addressableLoader;
        private ISceneLoader _loader;
        private SceneTarget _target;
        private MyLab.Core.ResourceManagement.LoadedScene _ownedScene;
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
        public Scene LoadedScene { get; private set; }
        /// <summary>Actual remaining candidate root readiness, including failure diagnostics.</summary>
        public bool IsGamePrepared => _gameRoot != null && _gameRoot.RootObject != null &&
            _gameRoot.RootObject.scene == LoadedScene && _gameRoot.RootObject.transform.parent == null && _gameRoot.IsPrepared;
        /// <summary>Project gameplay may proceed only while the revealed scene and both roots remain prepared.</summary>
        public bool CanProceed => State == SceneTransitionState.Ready && _commonHost != null &&
            _commonHost.gameObject.scene == _commonScene && _commonRoot.IsPrepared && IsGamePrepared &&
            GameScene.IsValid() && GameScene.isLoaded &&
            SceneManager.GetActiveScene() == GameScene && (!_hasCallbacks || _callbacks != null) &&
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
            _target = target;
            _mode = mode;
            _entry = new UniTaskCompletionSource();
            _lifetime = new CancellationTokenSource();
            RunEntryAsync().Forget();
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

        /// <summary>Requests actual owner cancellation. Native loads finish before owned candidate cleanup.</summary>
        public void CancelTransition()
        {
            EnsureMainThread();
            if (_entry != null && _entry.Task.Status == UniTaskStatus.Pending) _lifetime.Cancel();
        }

        /// <summary>
        /// Shares uncancelled shutdown, waits for late entry work, releases/unloads the owned game and retains common systems.
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

        private async UniTask RunEntryAsync()
        {
            try
            {
                var flow = new SceneRootFlow(new GuardedCommonRoot(this), CoverAsync, RevealAsync);
                await flow.PrepareAndProceedAsync(LoadAndPrepareAsync, _lifetime.Token);
                GameScene = LoadedScene;
                State = SceneTransitionState.Ready;
                _entry.TrySetResult();
            }
            catch (Exception failure)
            {
                FailurePhase = State;
                Exception reported = failure;
                try
                {
                    await ReleaseGameAsync();
                }
                catch (Exception cleanupFailure)
                {
                    reported = new AggregateException(reported, cleanupFailure);
                }
                reported = RecordFailure(reported);
                if (reported is OperationCanceledException cancelled) _entry.TrySetCanceled(cancelled.CancellationToken);
                else _entry.TrySetException(reported);
            }
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

        private async UniTask LoadAndPrepareAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidateCommonLifetime();
            ValidateInventory(_initialScenes);
            State = SceneTransitionState.Loading;
            // The loader cannot abandon native work. Retain its actual result before observing owner cancellation.
            _ownedScene = await _loader.LoadAsync(_target, _mode);
            if (_ownedScene == null) throw new InvalidOperationException("The scene loader returned no owned result.");
            LoadedScene = _ownedScene.Scene;
            if (_ownedScene.Target.Source != _target.Source || _ownedScene.Target.ScenePath != _target.ScenePath ||
                _ownedScene.Target.AddressableKey != _target.AddressableKey || LoadedScene.path != _target.ScenePath)
                throw new InvalidOperationException("The scene loader returned a different target or scene asset.");
            token.ThrowIfCancellationRequested();
            if (!LoadedScene.IsValid() || !LoadedScene.isLoaded ||
                (SceneManager.GetActiveScene() != LoadedScene && !SceneManager.SetActiveScene(LoadedScene)))
                throw new InvalidOperationException("The loaded game scene could not become active: " +
                    LoadedScene.path + " (valid=" + LoadedScene.IsValid() + ", loaded=" + LoadedScene.isLoaded + ").");
            var hosts = GetRootHosts(LoadedScene);
            if (hosts.Length != 1) throw new InvalidOperationException("Game scene requires exactly one lifecycle root.");
            BootstrapSystem.ValidateSceneRoot(hosts[0], LoadedScene);
            _gameRoot = (ISceneRoot)hosts[0];
            State = SceneTransitionState.Configuring;
            await InvokeAsync(() => _callbacks != null ? _callbacks.ConfigureSceneAsync(LoadedScene, _gameRoot, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            State = SceneTransitionState.PreparingScene;
            await InvokeAsync(() => _gameRoot.PrepareAsync(token));
            ValidatePreparedOwnership();
            State = SceneTransitionState.PreparingPresentation;
            await InvokeAsync(() => _callbacks != null ? _callbacks.PreparePresentationAsync(LoadedScene, _gameRoot, token) : UniTask.CompletedTask);
            token.ThrowIfCancellationRequested();
            ValidatePreparedOwnership();
        }

        private async UniTask ReleaseGameAsync()
        {
            GameScene = default;
            var failures = new List<Exception>();
            if (LoadedScene.IsValid() && LoadedScene.isLoaded)
            {
                var roots = GetRootHosts(LoadedScene).Cast<ISceneRoot>().ToList();
                if (_gameRoot != null && _gameRoot.RootObject != null && !roots.Contains(_gameRoot)) roots.Add(_gameRoot);
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
                try
                {
                    if (_commonHost != null && _commonHost.gameObject.scene == LoadedScene)
                        throw new InvalidOperationException("Cannot unload a candidate containing the borrowed common owner.");
                    if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded &&
                        SceneManager.GetActiveScene() == LoadedScene && !SceneManager.SetActiveScene(_previousActiveScene))
                        throw new InvalidOperationException("Could not restore the previous active scene.");
                    if (GetLoadedScenes().Length <= 1)
                        throw new InvalidOperationException("Unity cannot unload the last normal scene; it remains loaded but unprepared.");
                    await _ownedScene.UnloadAsync();
                    LoadedScene = default;
                    _ownedScene = null;
                    _gameRoot = null;
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            else if (_ownedScene != null)
            {
                try
                {
                    await _ownedScene.UnloadAsync();
                    _ownedScene = null;
                    LoadedScene = default;
                    _gameRoot = null;
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
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
            if (_entry != null)
            {
                try
                {
                    await _entry.Task;
                }
                catch (Exception)
                {
                    /* Entry failure belongs to entry awaiters. */
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
            _lifetime?.Dispose();
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
            if (SceneManager.GetActiveScene() != LoadedScene)
                throw new InvalidOperationException("The active scene changed outside the scene manager's ownership.");
            ValidateInventory(ExpectedGameScenes());
        }

        private Scene[] ExpectedGameScenes() => _mode == LoadSceneMode.Single
            ? new[] { LoadedScene } : _initialScenes.Concat(new[] { LoadedScene }).ToArray();

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
