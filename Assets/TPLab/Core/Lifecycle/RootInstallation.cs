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
        private readonly float _shutdownTimeoutSeconds;
        private int _begun;
        private bool _disposed;
        private bool _stopping;
        private bool _preparationRunning;
        private SceneRootInstaller _preparingInstaller;
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
                _preparationRunning = true;
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
                    _preparingInstaller = installer;
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
                _preparingInstaller = null;
                _preparationRunning = false;
                if (_disposed)
                {
                    DisposePreparationCancellation();
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
            string preparingInstallerName = _preparingInstaller?.GetType().Name ?? nameof(RootInstallation);
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
                var outcome = await WaitForShutdownPhaseAsync(_preparation.Task);
                if (outcome.TimedOut)
                {
                    failures.Add(CreateShutdownTimeout("Prepare", preparingInstallerName));
                }
                // Original preparation failures belong to its awaiters. Cleanup must still run.
            }
            for (int index = _begun - 1; index >= 0 && !_disposed; --index)
            {
                try
                {
                    var installer = _installers[index];
                    string installerName = installer.GetType().Name;
                    var outcome = await WaitForShutdownPhaseAsync(installer.ReleaseAsync(_root));
                    if (outcome.TimedOut)
                    {
                        failures.Add(CreateShutdownTimeout("Release", installerName));
                    }
                    else if (outcome.Failure != null)
                    {
                        failures.Add(outcome.Failure);
                    }
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

        private async UniTask<(bool TimedOut, Exception Failure)> WaitForShutdownPhaseAsync(UniTask operation)
        {
            // Capturing the actual operation separates an installer TimeoutException from our deadline.
            // The capture also observes a non-cooperative operation's completion after the deadline wins.
            var captured = CaptureFailureAsync(operation);
            if (_shutdownTimeoutSeconds <= 0)
            {
                return (false, await captured);
            }
            try
            {
                var failure = await captured.Timeout(TimeSpan.FromSeconds(_shutdownTimeoutSeconds),
                    DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update);
                return (false, failure);
            }
            catch (TimeoutException)
            {
                return (true, null);
            }
        }

        private static async UniTask<Exception> CaptureFailureAsync(UniTask operation)
        {
            try
            {
                await operation;
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private TimeoutException CreateShutdownTimeout(string phase, string installerName) =>
            new TimeoutException($"{phase} shutdown timeout after {_shutdownTimeoutSeconds} seconds for installer {installerName}.");

        private void DisposePreparationCancellation()
        {
            var cancellation = _preparationCancellation;
            _preparationCancellation = null;
            cancellation?.Dispose();
        }

        internal RootInstallation(ISceneRoot root, SceneRootInstaller[] installers, float shutdownTimeoutSeconds = 60f)
        {
            _root = root;
            ValidateRoot(root.RootObject);
            if (root.RootObject.GetComponents<SceneOwnedRoot>().Length +
                root.RootObject.GetComponents<SingletonSceneRoot>().Length != 1)
            {
                throw new InvalidOperationException("A root must have exactly one scene root host.");
            }
            _installers = CopyInstallers(root.RootObject, installers);
            ValidateShutdownTimeout(shutdownTimeoutSeconds);
            _shutdownTimeoutSeconds = shutdownTimeoutSeconds;
        }

        internal static void ValidateShutdownTimeout(float shutdownTimeoutSeconds)
        {
            if (float.IsNaN(shutdownTimeoutSeconds) || float.IsInfinity(shutdownTimeoutSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeoutSeconds), shutdownTimeoutSeconds,
                    "The shutdown timeout must be finite; zero or negative values disable the deadline.");
            }
            if (shutdownTimeoutSeconds > 0)
            {
                try
                {
                    _ = TimeSpan.FromSeconds(shutdownTimeoutSeconds);
                }
                catch (OverflowException)
                {
                    throw new ArgumentOutOfRangeException(nameof(shutdownTimeoutSeconds), shutdownTimeoutSeconds,
                        "A positive shutdown timeout must fit within TimeSpan.");
                }
            }
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
            // Cancelling the shared promise does not mean the installer stopped running.
            if (!_preparationRunning)
            {
                DisposePreparationCancellation();
            }
            if (failures != null)
            {
                throw new AggregateException("Scene root cleanup failed.", failures);
            }
        }
    }
}
