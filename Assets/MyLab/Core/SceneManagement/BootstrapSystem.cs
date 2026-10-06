using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Configures first entry and delegates scene ownership to an explicit GameSceneManager.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyLab/Bootstrap System")]
    public sealed class BootstrapSystem : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _sceneRoot;
        [SerializeField] private string _firstScenePath = "";
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private SceneTransitionCallbacks _callbacks;
        [SerializeField] private LoadSceneMode _loadMode = LoadSceneMode.Additive;
        private bool _stopping;
        /// <summary>Explicit manager created before common preparation; null before the first entry command.</summary>
        public GameSceneManager Manager { get; private set; }
        /// <summary>Successfully prepared/revealed game scene, or invalid before completion and during shutdown.</summary>
        public Scene GameScene => Manager?.GameScene ?? default;
        /// <summary>First game load mode. Single requires a persistent common root.</summary>
        public LoadSceneMode LoadMode => _loadMode;

        /// <summary>Explicit lifecycle host; Bootstrap never searches for global services.</summary>
        public MonoBehaviour SceneRoot => _sceneRoot;
        /// <summary>Full Assets scene path selected by the project.</summary>
        public string FirstScenePath => _firstScenePath;
        /// <summary>Starts preparation from Start when enabled; false permits explicit execution.</summary>
        public bool AutoStart => _autoStart;

        /// <summary>Configures entry before execution. Inspector and code use the same validation.</summary>
        public void Configure(MonoBehaviour sceneRoot, string firstScenePath, bool autoStart = true, SceneTransitionCallbacks callbacks = null,
            LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            if (Manager != null || _stopping)
            {
                throw new InvalidOperationException("Configure Bootstrap before its first entry attempt.");
            }
            _sceneRoot = sceneRoot;
            _firstScenePath = firstScenePath;
            _autoStart = autoStart;
            _callbacks = callbacks;
            _loadMode = loadMode;
        }

        /// <summary>Rejects invalid root ownership or entry settings before initialization.</summary>
        public void ValidateConfiguration()
        {
            ValidateSceneRoot(_sceneRoot, gameObject.scene, true);
            if (_loadMode != LoadSceneMode.Additive && _loadMode != LoadSceneMode.Single)
                throw new InvalidOperationException("Select Single or Additive as the first load mode.");
            if (_loadMode == LoadSceneMode.Single && !IsPersistent(_sceneRoot))
                throw new InvalidOperationException("Single requires a persistent common root; it cannot retain Bootstrap.");
            if (IsPersistent(_sceneRoot) && (!transform.IsChildOf(_sceneRoot.transform) ||
                (_callbacks != null && !_callbacks.transform.IsChildOf(_sceneRoot.transform))))
                throw new InvalidOperationException("Bootstrap and callbacks must survive under the persistent common root.");
            ValidateScenePath(_firstScenePath);
            if (_firstScenePath == gameObject.scene.path)
                throw new InvalidOperationException("Bootstrap cannot load itself as the game scene.");
            if (_callbacks != null && _callbacks.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Callbacks must belong to the retained Bootstrap scene.");
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("Bootstrap must be active and enabled.");
        }

        /// <summary>Checks a standard, active, nonpersistent, top-level lifecycle host in its owning scene; common hosts may opt into persistence.</summary>
        public static void ValidateSceneRoot(MonoBehaviour host, Scene scene, bool allowPersistence = false)
        {
            if (!(host is SceneOwnedRoot) && !(host is SingletonSceneRoot))
                throw new InvalidOperationException("Select a SceneOwnedRoot or SingletonSceneRoot.");
            if (host == null || !scene.IsValid() || host.gameObject.scene != scene ||
                host.transform.parent != null || !host.isActiveAndEnabled)
                throw new InvalidOperationException("The lifecycle host must be an active root in its owning scene.");
            if (host.GetComponents<SceneOwnedRoot>().Length + host.GetComponents<SingletonSceneRoot>().Length != 1)
                throw new InvalidOperationException("A root must have exactly one lifecycle host.");
            if (!allowPersistence && IsPersistent(host))
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
            GameSceneManager.EnsureMainThread();
            if (_stopping) throw new ObjectDisposedException(nameof(BootstrapSystem));
            cancellationToken.ThrowIfCancellationRequested();
            if (Manager != null) return Manager.WaitForEntryAsync(cancellationToken);
            ValidateConfiguration();
            Manager = new GameSceneManager(_sceneRoot, _callbacks);
            try
            {
                return Manager.EnterFirstSceneAsync(_firstScenePath, _loadMode, cancellationToken);
            }
            catch
            {
                Manager = null; throw;
            }
        }

        /// <summary>Delegates game cleanup to the manager; the external root owner releases common systems separately.</summary>
        public UniTask ShutdownAsync()
        {
            GameSceneManager.EnsureMainThread();
            var shutdown = Manager?.ShutdownAsync() ?? UniTask.CompletedTask;
            _stopping = true;
            return shutdown;
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
            if (_stopping) return;
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

        internal static bool IsPersistent(MonoBehaviour host) =>
            (host is SceneOwnedRoot owned && owned.PersistsAcrossScenes) ||
            (host is SingletonSceneRoot singleton && singleton.PersistsAcrossScenes);
    }
}
