using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Owns an Addressables asset cache for one project or scene scope. Use only on Unity's main thread.</summary>
    /// <remarks>Consumers borrow assets and must not destroy them or release their Addressables references. Methods reject background threads.</remarks>
    public sealed class ResourceManager : IDisposable
    {
        private sealed class AssetLoad
        {
            internal readonly Type Type;
            internal readonly UniTaskCompletionSource<UnityEngine.Object> Completion = new UniTaskCompletionSource<UnityEngine.Object>();
            internal AsyncOperationHandle Handle;
            internal bool Completed;

            internal AssetLoad(Type type) => Type = type;
        }

        private readonly Dictionary<string, AssetLoad> _loads = new Dictionary<string, AssetLoad>(StringComparer.Ordinal);
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly UniTaskCompletionSource _drained = new UniTaskCompletionSource();
        private UniTaskCompletionSource _initialization;
        private int _pendingCount;

        /// <summary>True after Addressables initialization succeeds, until this owner terminates. Does not imply assets are loaded.</summary>
        public bool IsInitialized { get; private set; }
        /// <summary>True after permanent termination. Pending native work may still be draining.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Shares Addressables initialization. Failure permits another attempt; caller cancellation only cancels its wait.</summary>
        /// <param name="cancellationToken">Cancellation of this caller's wait, not the shared native operation.</param>
        /// <returns>Initialization completion; no catalog updates or project-specific downloads are requested.</returns>
        /// <exception cref="ObjectDisposedException">The owner is terminated.</exception>
        /// <exception cref="OperationCanceledException">The caller or owner cancels the wait.</exception>
        /// <remarks>Native initialization errors propagate. Addressables runtime data must be configured by the consuming project.</remarks>
        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            EnsureOpen();
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsInitialized)
            {
                var initialization = _initialization ?? StartInitialization();
                await initialization.Task.AttachExternalCancellation(_lifetime.Token)
                    .AttachExternalCancellation(cancellationToken);
            }
            EnsureOpen();
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>Initializes and shares one load per ordinal string key and exact requested type, caching successes until termination.</summary>
        /// <typeparam name="T">Unity asset type. Another type for an already registered key is rejected.</typeparam>
        /// <param name="key">Nonempty, non-whitespace Addressables key. Use a unique asset address rather than a label.</param>
        /// <param name="cancellationToken">Cancels only this caller's wait. Even if all waiters cancel, the owner retains the load.</param>
        /// <returns>A borrowed asset; the owner holds its handle. Failed requests are removed and may be retried.</returns>
        /// <exception cref="ArgumentException">The key is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">The same key has another type, or a provider succeeds without a live asset.</exception>
        /// <exception cref="ObjectDisposedException">The owner is terminated.</exception>
        /// <exception cref="OperationCanceledException">The caller or owner cancels the wait.</exception>
        /// <remarks>Addressables errors propagate. Stop all consumers and destroy prefab clones before shutting down this owner.</remarks>
        public async UniTask<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            EnsureOpen();
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("An Addressables key is required.", nameof(key));
            }
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken);
            if (_loads.TryGetValue(key, out var load))
            {
                if (load.Type != typeof(T))
                {
                    throw new InvalidOperationException($"Key '{key}' is already requested as {load.Type.Name}.");
                }
            }
            else
            {
                load = new AssetLoad(typeof(T));
                _loads.Add(key, load);
                ++_pendingCount;
                try
                {
                    var handle = Addressables.LoadAssetAsync<T>(key);
                    load.Handle = handle;
                    handle.Completed += operation => CompleteLoad(key, load, operation);
                }
                catch (Exception error)
                {
                    _loads.Remove(key);
                    Release(load);
                    load.Completion.TrySetException(error);
                    CompletePending();
                }
            }
            var asset = await load.Completion.Task.AttachExternalCancellation(_lifetime.Token)
                .AttachExternalCancellation(cancellationToken);
            EnsureOpen();
            cancellationToken.ThrowIfCancellationRequested();
            return (T)asset;
        }

        /// <summary>Permanently closes the owner and waits for all native operations and handle releases. Concurrent calls share completion.</summary>
        /// <returns>Cleanup completion, without caller cancellation. A stalled native operation keeps cleanup pending.</returns>
        /// <remarks>Use before scene unload. This does not unload Addressables global catalogs or guarantee immediate bundle memory reclamation.</remarks>
        public UniTask ShutdownAsync()
        {
            Dispose();
            return _drained.Task;
        }

        /// <summary>Immediately rejects new work, cancels waiters and releases completed handles. Safe to call repeatedly.</summary>
        /// <remarks>Incomplete native handles are released when they finish; use ShutdownAsync to await that drain. No asset can be republished.</remarks>
        public void Dispose()
        {
            EnsureMainThread();
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            IsInitialized = false;
            var loads = new List<AssetLoad>(_loads.Values);
            _loads.Clear();
            _lifetime.Cancel();
            foreach (var load in loads)
            {
                if (load.Completed)
                {
                    Release(load);
                }
            }
            if (_pendingCount == 0)
            {
                _drained.TrySetResult();
            }
            _lifetime.Dispose();
        }

        private UniTaskCompletionSource StartInitialization()
        {
            var completion = new UniTaskCompletionSource();
            _initialization = completion;
            ++_pendingCount;
            try
            {
                // InitializeAsync can return an existing handle without acquiring another reference.
                // Acquire our own reference while Addressables releases its default auto-release reference.
                var handle = Addressables.ResourceManager.Acquire(Addressables.InitializeAsync());
                handle.Completed += operation =>
                {
                    var error = operation.OperationException;
                    bool succeeded = operation.Status == AsyncOperationStatus.Succeeded;
                    Addressables.Release(operation);
                    if (!succeeded)
                    {
                        _initialization = null;
                        completion.TrySetException(error ?? new InvalidOperationException("Addressables initialization failed."));
                    }
                    else if (IsDisposed)
                    {
                        completion.TrySetCanceled();
                    }
                    else
                    {
                        IsInitialized = true;
                        completion.TrySetResult();
                    }
                    CompletePending();
                };
            }
            catch (Exception error)
            {
                _initialization = null;
                completion.TrySetException(error);
                CompletePending();
            }
            return completion;
        }

        private void CompleteLoad<T>(string key, AssetLoad load, AsyncOperationHandle<T> operation) where T : UnityEngine.Object
        {
            load.Completed = true;
            var asset = operation.Result;
            var error = operation.OperationException;
            if (IsDisposed)
            {
                Release(load);
                load.Completion.TrySetCanceled();
            }
            else if (operation.Status != AsyncOperationStatus.Succeeded || asset == null)
            {
                _loads.Remove(key);
                Release(load);
                load.Completion.TrySetException(error ?? new InvalidOperationException($"No live asset was loaded for '{key}'."));
            }
            else
            {
                load.Completion.TrySetResult(asset);
            }
            CompletePending();
        }

        private static void Release(AssetLoad load)
        {
            if (load.Handle.IsValid())
            {
                Addressables.Release(load.Handle);
                load.Handle = default;
            }
        }

        private void CompletePending()
        {
            --_pendingCount;
            if (IsDisposed && _pendingCount == 0)
            {
                _drained.TrySetResult();
            }
        }

        private void EnsureOpen()
        {
            EnsureMainThread();
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ResourceManager));
            }
        }

        private static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                throw new InvalidOperationException("ResourceManager requires Unity's main thread.");
            }
        }
    }
}
