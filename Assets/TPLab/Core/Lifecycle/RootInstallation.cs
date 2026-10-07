using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    internal sealed class RootInstallation : IDisposable
    {
        private readonly ISceneRoot _root;
        private readonly SceneRootInstaller[] _installers;
        private int _begun;
        private bool _disposed;
        private bool _stopping;
        private CancellationTokenSource _preparationCancellation;
        private UniTaskCompletionSource _preparation;
        private UniTaskCompletionSource _shutdown;

        internal bool IsReady { get; private set; }
        internal bool IsPrepared { get; private set; }

        internal UniTask PrepareAsync(CancellationToken cancellationToken)
        {
            if (_disposed || _stopping)
            {
                throw new ObjectDisposedException(nameof(RootInstallation));
            }
            if (!IsReady)
            {
                throw new InvalidOperationException("Installation must succeed before preparation.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (_preparation == null)
            {
                _preparation = new UniTaskCompletionSource();
                _preparationCancellation = new CancellationTokenSource();
                PrepareOwnedAsync().Forget();
            }
            return _preparation.Task.AttachExternalCancellation(cancellationToken);
        }

        private async UniTask PrepareOwnedAsync()
        {
            var token = _preparationCancellation.Token;
            try
            {
                foreach (var installer in _installers)
                {
                    token.ThrowIfCancellationRequested();
                    await installer.PrepareAsync(_root, token);
                    token.ThrowIfCancellationRequested();
                }
                if (_disposed || _stopping)
                {
                    throw new OperationCanceledException(token);
                }
                IsPrepared = true;
                _preparation.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                _preparation.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                if (!_preparation.TrySetException(exception))
                {
                    Debug.LogException(exception);
                }
            }
            finally
            {
                if (_disposed)
                {
                    _preparationCancellation.Dispose();
                }
            }
        }

        internal UniTask ShutdownAsync()
        {
            if (_shutdown != null)
            {
                return _shutdown.Task;
            }
            if (_disposed)
            {
                return UniTask.CompletedTask;
            }
            _stopping = true;
            IsPrepared = false;
            _shutdown = new UniTaskCompletionSource();
            ShutdownOwnedAsync().Forget();
            return _shutdown.Task;
        }

        private async UniTask ShutdownOwnedAsync()
        {
            var failures = new List<Exception>();
            try
            {
                _preparationCancellation?.Cancel();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (_preparation != null)
            {
                try
                {
                    await _preparation.Task;
                }
                catch (Exception)
                {
                    // Preparation failure belongs to its awaiters. Cleanup must still run.
                }
            }
            for (int index = _begun - 1; index >= 0 && !_disposed; --index)
            {
                try
                {
                    await _installers[index].ReleaseAsync(_root);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            try
            {
                Dispose();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (failures.Count == 0)
            {
                _shutdown.TrySetResult();
            }
            else
            {
                _shutdown.TrySetException(new AggregateException("Asynchronous root shutdown failed.", failures));
            }
        }

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
            _stopping = true;
            IsReady = false;
            IsPrepared = false;
            List<Exception> failures = null;
            try
            {
                _preparationCancellation?.Cancel();
            }
            catch (Exception exception)
            {
                failures = new List<Exception> { exception };
            }
            // Destruction cannot await a non-cooperative preparation operation.
            _preparation?.TrySetCanceled();
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
            if (_preparation == null || _preparation.Task.Status != UniTaskStatus.Pending)
            {
                _preparationCancellation?.Dispose();
            }
            if (failures != null)
            {
                throw new AggregateException("Scene root cleanup failed.", failures);
            }
        }
    }
}
