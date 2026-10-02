using System;
using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Owns explicit installers without global registration. Disabling does not release services.</summary>
    public sealed class SceneOwnedRoot : MonoBehaviour, ISceneRoot
    {
        [SerializeField] private SceneRootInstaller[] _installers = Array.Empty<SceneRootInstaller>();
        [SerializeField] private bool _persistAcrossScenes;
        [NonSerialized] private RootInstallation _installation;
        [NonSerialized] private bool _attempted;
        [NonSerialized] private bool _isQuitting;

        /// <inheritdoc />
        public GameObject RootObject => gameObject;
        /// <inheritdoc />
        public bool IsReady => _installation != null && _installation.IsReady;

        /// <summary>
        /// Copies installers in initialization order before activation. Null means an empty list.
        /// Persistent roots and their children survive scene unload. Throws for invalid ownership or late configuration.
        /// </summary>
        public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false)
        {
            if (_attempted || (Application.isPlaying && gameObject.activeInHierarchy))
            {
                throw new InvalidOperationException("Configure the root before activating it.");
            }
            RootInstallation.ValidateRoot(gameObject);
            _installers = RootInstallation.CopyInstallers(gameObject, installers);
            _persistAcrossScenes = persistAcrossScenes;
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
                _installation = new RootInstallation(this, _installers);
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
