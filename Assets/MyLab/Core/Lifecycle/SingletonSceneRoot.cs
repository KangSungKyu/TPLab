using System;
using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Owns explicit installers and publishes Instance only after successful installation.</summary>
    public sealed class SingletonSceneRoot : MonoSingleton<SingletonSceneRoot>, ISceneRoot
    {
        [SerializeField] private SceneRootInstaller[] _installers = Array.Empty<SceneRootInstaller>();
        [SerializeField] private bool _persistAcrossScenes;
        [NonSerialized] private RootInstallation _installation;

        /// <inheritdoc />
        public GameObject RootObject => gameObject;
        /// <inheritdoc />
        public bool IsReady => _installation != null && _installation.IsReady;

        protected override bool PersistAcrossScenes => _persistAcrossScenes;

        /// <summary>
        /// Copies installers before activation. Null means an empty list. Persistence is independent of global access.
        /// Throws for invalid ownership or late configuration; duplicate singleton hosts never install.
        /// </summary>
        public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false)
        {
            if (_installation != null || IsInitialized || (Application.isPlaying && gameObject.activeInHierarchy))
            {
                throw new InvalidOperationException("Configure the root before activating it.");
            }
            RootInstallation.ValidateRoot(gameObject);
            _installers = RootInstallation.CopyInstallers(gameObject, installers);
            _persistAcrossScenes = persistAcrossScenes;
        }

        protected override void OnSingletonInitialize()
        {
            _installation = new RootInstallation(this, _installers);
            _installation.Install();
        }

        protected override void OnSingletonShutdown() => _installation?.Dispose();
    }
}
