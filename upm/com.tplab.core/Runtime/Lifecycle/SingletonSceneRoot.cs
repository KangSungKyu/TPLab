using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    /// <summary>Owns explicit installers and publishes Instance only after successful installation.</summary>
    public sealed class SingletonSceneRoot : MonoSingleton<SingletonSceneRoot>, ISceneRoot
    {
        [SerializeField] private SceneRootInstaller[] _installers = Array.Empty<SceneRootInstaller>();
        [SerializeField] private bool _persistAcrossScenes;
        [SerializeField] private float _shutdownTimeoutSeconds = 60f;
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
        /// <param name="installers">Ordered owned installers, or null for none.</param>
        /// <param name="persistAcrossScenes">Whether the root survives scene changes.</param>
        /// <param name="shutdownTimeoutSeconds">Independent unscaled deadlines for preparation cancellation and each release; finite zero/negative values wait without a deadline.</param>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is non-finite or a positive value cannot be represented by TimeSpan. Configuration is unchanged.</exception>
        public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false,
            float shutdownTimeoutSeconds = 60f)
        {
            if (_installation != null || IsInitialized || (Application.isPlaying && gameObject.activeInHierarchy))
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

        protected override void OnSingletonInitialize()
        {
            _installation = new RootInstallation(this, _installers, _shutdownTimeoutSeconds);
            _installation.Install();
        }

        protected override void OnSingletonShutdown() => _installation?.Dispose();
    }
}
