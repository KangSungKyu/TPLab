using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    /// <summary>Owns explicit installers without global registration. Disabling does not release services.</summary>
    public sealed class SceneOwnedRoot : MonoBehaviour, ISceneRoot
    {
        [SerializeField] private SceneRootInstaller[] _installers = Array.Empty<SceneRootInstaller>();
        [SerializeField] private bool _persistAcrossScenes;
        [SerializeField] private float _shutdownTimeoutSeconds = 60f;
        [NonSerialized] private RootInstallation _installation;
        [NonSerialized] private bool _attempted;
        [NonSerialized] private bool _isQuitting;

        /// <inheritdoc />
        public GameObject RootObject => gameObject;
        /// <summary>Gets whether the host moves into DontDestroyOnLoad on installation.</summary>
        public bool PersistsAcrossScenes => _persistAcrossScenes;
        /// <inheritdoc />
        public bool IsReady => _installation != null && _installation.IsReady;
        /// <inheritdoc />
        public bool IsPrepared => _installation != null && _installation.IsPrepared;
        /// <inheritdoc />
        public UniTask PrepareAsync(CancellationToken cancellationToken = default)
        {
            if (_installation == null)
            {
                throw new InvalidOperationException("Activate and install the root before preparation.");
            }
            return _installation.PrepareAsync(cancellationToken);
        }
        /// <inheritdoc />
        public UniTask ShutdownAsync() => _installation?.ShutdownAsync() ?? UniTask.CompletedTask;

        /// <summary>
        /// Copies installers in initialization order before activation. Null means an empty list.
        /// Persistent roots and their children survive scene unload. Throws for invalid ownership or late configuration.
        /// </summary>
        /// <param name="installers">Ordered owned installers, or null for none.</param>
        /// <param name="persistAcrossScenes">Whether the root survives scene changes.</param>
        /// <param name="shutdownTimeoutSeconds">Independent unscaled deadlines for preparation cancellation and each release; finite zero/negative values wait without a deadline.</param>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is non-finite or a positive value cannot be represented by TimeSpan. Configuration is unchanged.</exception>
        public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false,
            float shutdownTimeoutSeconds = 60f)
        {
            if (_attempted || (Application.isPlaying && gameObject.activeInHierarchy))
            {
                throw new InvalidOperationException("Configure the root before activating it.");
            }
            RootInstallation.ValidateRoot(gameObject);
            var copy = RootInstallation.CopyInstallers(gameObject, installers);
            RootInstallation.ValidateShutdownTimeout(shutdownTimeoutSeconds);
            _installers = copy;
            _persistAcrossScenes = persistAcrossScenes;
            _shutdownTimeoutSeconds = shutdownTimeoutSeconds;
        }

        private void Awake() => InitializeIfNeeded();
        private void OnEnable() => InitializeIfNeeded();
        private void OnDestroy() => Shutdown();

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            Shutdown();
            _attempted = false;
        }

        private void InitializeIfNeeded()
        {
            if (!Application.isPlaying || _isQuitting || _attempted)
            {
                return;
            }
            _attempted = true;
            try
            {
                _installation = new RootInstallation(this, _installers, _shutdownTimeoutSeconds);
                _installation.Install();
                if (_persistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Shutdown()
        {
            try
            {
                _installation?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreSceneRoots()
        {
            foreach (var root in FindObjectsByType<SceneOwnedRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (root.gameObject.scene.IsValid() && root.gameObject.activeInHierarchy)
                {
                    root._isQuitting = false;
                    root.InitializeIfNeeded();
                }
            }
        }
    }
}
