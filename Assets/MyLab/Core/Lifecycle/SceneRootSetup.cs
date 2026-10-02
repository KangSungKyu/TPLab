using System;
using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Chooses root access independently of scene persistence.</summary>
    public enum SceneRootMode
    {
        SceneOwned,
        Singleton
    }

    /// <summary>Attaches one explicitly selected lifecycle owner on Unity's main thread.</summary>
    public static class SceneRootSetup
    {
        /// <summary>
        /// Adds and configures a host on a scene root. Runtime roots must be inactive before this call.
        /// Copies ordered installers belonging to the root or its children; null means none.
        /// Returns the owner, which installs on activation and releases on destruction.
        /// Throws before mutation for invalid inputs, active runtime roots, or an existing host.
        /// </summary>
        public static ISceneRoot Attach(GameObject root, SceneRootMode mode, SceneRootInstaller[] installers = null,
            bool persistAcrossScenes = false)
        {
            RootInstallation.ValidateRoot(root);
            if (mode != SceneRootMode.SceneOwned && mode != SceneRootMode.Singleton)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }
            if (Application.isPlaying && root.activeInHierarchy)
            {
                throw new InvalidOperationException("Runtime setup requires an inactive root GameObject.");
            }
            if (root.GetComponent<SceneOwnedRoot>() != null || root.GetComponent<SingletonSceneRoot>() != null)
            {
                throw new InvalidOperationException("The GameObject already has a scene root host.");
            }
            var copy = RootInstallation.CopyInstallers(root, installers);
            if (mode == SceneRootMode.Singleton)
            {
                var host = root.AddComponent<SingletonSceneRoot>();
                host.Configure(copy, persistAcrossScenes);
                return host;
            }
            var sceneHost = root.AddComponent<SceneOwnedRoot>();
            sceneHost.Configure(copy, persistAcrossScenes);
            return sceneHost;
        }
    }
}
