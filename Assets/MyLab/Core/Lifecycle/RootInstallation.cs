using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    internal sealed class RootInstallation : IDisposable
    {
        private readonly ISceneRoot _root;
        private readonly SceneRootInstaller[] _installers;
        private int _begun;
        private bool _disposed;

        internal bool IsReady { get; private set; }

        internal RootInstallation(ISceneRoot root, SceneRootInstaller[] installers)
        {
            _root = root;
            ValidateRoot(root.RootObject);
            if (root.RootObject.GetComponents<SceneOwnedRoot>().Length +
                root.RootObject.GetComponents<SingletonSceneRoot>().Length != 1)
            {
                throw new InvalidOperationException("A root must have exactly one scene root host.");
            }
            _installers = CopyInstallers(root.RootObject, installers);
        }

        internal static void ValidateRoot(GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (!root.scene.IsValid() || root.transform.parent != null)
            {
                throw new ArgumentException("Choose a root GameObject in a scene.", nameof(root));
            }
        }

        internal static SceneRootInstaller[] CopyInstallers(GameObject root, SceneRootInstaller[] installers)
        {
            var copy = installers == null ? Array.Empty<SceneRootInstaller>() : (SceneRootInstaller[])installers.Clone();
            var seen = new HashSet<SceneRootInstaller>();
            foreach (var installer in copy)
            {
                if (installer == null || !installer.transform.IsChildOf(root.transform) || !seen.Add(installer))
                {
                    throw new ArgumentException("Installers must be unique, non-null components on the root or its children.", nameof(installers));
                }
            }
            return copy;
        }

        internal void Install()
        {
            try
            {
                foreach (var installer in _installers)
                {
                    ++_begun;
                    installer.Install(_root);
                    if (_disposed || _root.RootObject == null)
                    {
                        throw new InvalidOperationException("Root was destroyed during installation.");
                    }
                }
                IsReady = true;
            }
            catch (Exception installFailure)
            {
                try
                {
                    Dispose();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(installFailure, cleanupFailure);
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            IsReady = false;
            List<Exception> failures = null;
            while (_begun > 0)
            {
                try
                {
                    // Keep the managed adapter reference during Unity object destruction.
                    _installers[--_begun].Uninstall(_root);
                }
                catch (Exception exception)
                {
                    if (failures == null)
                    {
                        failures = new List<Exception>();
                    }
                    failures.Add(exception);
                }
            }
            if (failures != null)
            {
                throw new AggregateException("Scene root cleanup failed.", failures);
            }
        }
    }
}
