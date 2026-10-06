using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Configures the first scene entry using a scene-owned lifecycle root.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyLab/Bootstrap System")]
    public sealed class BootstrapSystem : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _sceneRoot;
        [SerializeField] private string _firstScenePath = "";
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private BootstrapCallbacks _callbacks;
        private UniTaskCompletionSource _entry;
        private UniTaskCompletionSource _shutdown;
        private CancellationTokenSource _lifetime;
        private Scene _ownedGameScene;
        private Scene _previousActiveScene;
        private bool _stopping;
        private bool _gameWasMadeActive;
        /// <summary>Successfully prepared game scene, or an invalid scene before completion.</summary>
        public Scene GameScene { get; private set; }

        /// <summary>Explicit lifecycle host; Bootstrap never searches for global services.</summary>
        public MonoBehaviour SceneRoot => _sceneRoot;
        /// <summary>Full Assets scene path selected by the project.</summary>
        public string FirstScenePath => _firstScenePath;
        /// <summary>Starts preparation from Start when enabled; false permits explicit execution.</summary>
        public bool AutoStart => _autoStart;

        /// <summary>Configures entry before execution. Inspector and code use the same validation.</summary>
        public void Configure(MonoBehaviour sceneRoot, string firstScenePath, bool autoStart = true, BootstrapCallbacks callbacks = null)
        {
            if (_entry != null || _stopping)
            {
                throw new InvalidOperationException("Configure Bootstrap before its first entry attempt.");
            }
            _sceneRoot = sceneRoot;
            _firstScenePath = firstScenePath;
            _autoStart = autoStart;
            _callbacks = callbacks;
        }

        /// <summary>Rejects invalid root ownership or entry settings before initialization.</summary>
        public void ValidateConfiguration()
        {
            ValidateSceneRoot(_sceneRoot, gameObject.scene);
            ValidateScenePath(_firstScenePath);
            if (_firstScenePath == gameObject.scene.path)
                throw new InvalidOperationException("Bootstrap cannot load itself as the game scene.");
            if (_callbacks != null && _callbacks.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Callbacks must belong to the retained Bootstrap scene.");
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("Bootstrap must be active and enabled.");
        }

        /// <summary>Checks a standard, active, nonpersistent, top-level lifecycle host in its owning scene.</summary>
        public static void ValidateSceneRoot(MonoBehaviour host, Scene scene)
        {
            if (!(host is SceneOwnedRoot) && !(host is SingletonSceneRoot))
                throw new InvalidOperationException("Select a SceneOwnedRoot or SingletonSceneRoot.");
            if (host == null || !scene.IsValid() || host.gameObject.scene != scene ||
                host.transform.parent != null || !host.isActiveAndEnabled)
                throw new InvalidOperationException("The lifecycle host must be an active root in its owning scene.");
            if (host.GetComponents<SceneOwnedRoot>().Length + host.GetComponents<SingletonSceneRoot>().Length != 1)
                throw new InvalidOperationException("A root must have exactly one lifecycle host.");
            if ((host is SceneOwnedRoot owned && owned.PersistsAcrossScenes) ||
                (host is SingletonSceneRoot singleton && singleton.PersistsAcrossScenes))
                throw new InvalidOperationException("Additive scenes own their roots; disable DontDestroyOnLoad.");
        }

        /// <summary>Checks a normalized Assets scene path; existence and build inclusion are checked separately.</summary>
        public static void ValidateScenePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".unity", StringComparison.Ordinal) || path.Contains("//") ||
                path.IndexOfAny(new[] { '\\', ':', '\r', '\n', '\t' }) >= 0)
                throw new InvalidOperationException("Select a full Assets/.../*.unity scene path.");
            foreach (var part in path.Split('/'))
            {
                if (part == "." || part == "..")
                    throw new InvalidOperationException("Relative path segments are not allowed.");
            }
        }

        /// <summary>Shares one entry attempt; caller cancellation stops only its wait. Failure keeps the cover.</summary>
        public UniTask BootstrapAsync(CancellationToken cancellationToken = default)
        {
            EnsureMainThread();
            if (_stopping) throw new ObjectDisposedException(nameof(BootstrapSystem));
            cancellationToken.ThrowIfCancellationRequested();
            if (_entry != null) return _entry.Task.AttachExternalCancellation(cancellationToken);
            ValidateConfiguration();
            if (!Application.isPlaying || !Application.CanStreamedLevelBeLoaded(_firstScenePath))
                throw new InvalidOperationException("The first game scene must be enabled in the Player build scene list.");
            if (SceneManager.GetSceneByPath(_firstScenePath).isLoaded)
                throw new InvalidOperationException("The first game scene is already loaded; Bootstrap must own its load.");
            _entry = new UniTaskCompletionSource();
            _lifetime = new CancellationTokenSource();
            RunOwnedAsync().Forget();
            return _entry.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>Stops entry and awaits game root release/unload. The root owner releases shared systems separately.</summary>
        public UniTask ShutdownAsync()
        {
            EnsureMainThread();
            if (_shutdown != null) return _shutdown.Task;
            _stopping = true;
            _shutdown = new UniTaskCompletionSource();
            ShutdownOwnedAsync().Forget();
            return _shutdown.Task;
        }

        private void Start()
        {
            if (_autoStart)
            {
                try
                {
                    BootstrapAsync().Forget(Debug.LogException);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void OnDestroy()
        {
            // Graceful release requires awaiting ShutdownAsync before destroying this component.
            try
            {
                ShutdownAsync().Forget(Debug.LogException);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Reset()
        {
            _sceneRoot = (MonoBehaviour)GetComponent<SceneOwnedRoot>() ?? GetComponent<SingletonSceneRoot>();
        }

        private async UniTask RunOwnedAsync()
        {
            try
            {
                var flow = new SceneRootFlow((ISceneRoot)_sceneRoot,
                    token => _callbacks != null ? _callbacks.ShowCoverAsync(token) : UniTask.CompletedTask,
                    token => _callbacks != null ? _callbacks.HideCoverAsync(token) : UniTask.CompletedTask);
                await flow.PrepareAndProceedAsync(LoadAndPrepareGameAsync, _lifetime.Token);
                GameScene = _ownedGameScene;
                _entry.TrySetResult();
            }
            catch (Exception failure)
            {
                Exception reported = failure;
                try
                {
                    await ReleaseGameAsync();
                }
                catch (Exception cleanupFailure)
                {
                    reported = new AggregateException(reported, cleanupFailure);
                }
                try
                {
                    if (_callbacks != null) _callbacks.OnFailure(reported);
                }
                catch (Exception callbackFailure)
                {
                    reported = new AggregateException(reported, callbackFailure);
                }
                if (reported is OperationCanceledException cancelled)
                    _entry.TrySetCanceled(cancelled.CancellationToken);
                else
                    _entry.TrySetException(reported);
            }
        }

        private async UniTask LoadAndPrepareGameAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _previousActiveScene = SceneManager.GetActiveScene();
            var operation = SceneManager.LoadSceneAsync(_firstScenePath, LoadSceneMode.Additive);
            if (operation == null) throw new InvalidOperationException("Unity did not start the game scene load.");
            // Native scene loading is not cancellable. Retain ownership until late completion and cleanup.
            await operation.ToUniTask();
            _ownedGameScene = SceneManager.GetSceneByPath(_firstScenePath);
            token.ThrowIfCancellationRequested();
            if (!_ownedGameScene.IsValid() || !_ownedGameScene.isLoaded || !SceneManager.SetActiveScene(_ownedGameScene))
                throw new InvalidOperationException("The loaded game scene could not become active.");
            _gameWasMadeActive = true;
            var hosts = GetRootHosts(_ownedGameScene);
            if (hosts.Length != 1) throw new InvalidOperationException("Game scene requires exactly one lifecycle root.");
            ValidateSceneRoot(hosts[0], _ownedGameScene);
            var root = (ISceneRoot)hosts[0];
            if (_callbacks != null) await _callbacks.ConfigureGameAsync(_ownedGameScene, root, token);
            token.ThrowIfCancellationRequested();
            await root.PrepareAsync(token);
            if (!root.IsPrepared) throw new InvalidOperationException("Game root no longer owns prepared systems.");
            if (_callbacks != null) await _callbacks.PreparePresentationAsync(_ownedGameScene, root, token);
            token.ThrowIfCancellationRequested();
            if (_sceneRoot == null || !((ISceneRoot)_sceneRoot).IsPrepared || !root.IsPrepared ||
                !_ownedGameScene.IsValid() || !_ownedGameScene.isLoaded)
                throw new InvalidOperationException("Both owning roots must remain prepared until game entry completes.");
        }

        private async UniTask ReleaseGameAsync()
        {
            GameScene = default;
            if (!_ownedGameScene.IsValid() || !_ownedGameScene.isLoaded) return;
            var failures = new List<Exception>();
            foreach (var host in GetRootHosts(_ownedGameScene))
            {
                try
                {
                    await ((ISceneRoot)host).ShutdownAsync();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (_gameWasMadeActive && _previousActiveScene.IsValid() && _previousActiveScene.isLoaded &&
                _previousActiveScene != SceneManager.GetActiveScene())
            {
                if (!SceneManager.SetActiveScene(_previousActiveScene))
                    failures.Add(new InvalidOperationException("Could not restore the previous active scene."));
            }
            try
            {
                var unload = SceneManager.UnloadSceneAsync(_ownedGameScene);
                if (unload == null) throw new InvalidOperationException("Unity did not start the owned game scene unload.");
                await unload.ToUniTask();
                _ownedGameScene = default;
                _gameWasMadeActive = false;
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (failures.Count != 0) throw new AggregateException(failures);
        }

        private async UniTask ShutdownOwnedAsync()
        {
            var failures = new List<Exception>();
            try
            {
                _lifetime?.Cancel();
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
                    /* Entry failure belongs to entry awaiters; cleanup still runs. */
                }
            }
            if (GameScene.IsValid() && _callbacks != null)
            {
                try
                {
                    await _callbacks.ShowCoverAsync(CancellationToken.None);
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
            if (failures.Count == 0) _shutdown.TrySetResult();
            else _shutdown.TrySetException(new AggregateException(failures));
        }

        private static MonoBehaviour[] GetRootHosts(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(component => component is ISceneRoot).ToArray();

        private static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("Bootstrap must be used on Unity's main thread.");
        }
    }
}
