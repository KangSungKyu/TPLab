using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
