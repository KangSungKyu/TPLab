using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.UI
{
    /// <summary>
    /// Owns root-scoped display generations, clones, and registered cleanup on Unity's main thread.
    /// The root, prefab, provider, and input services are borrowed and are never released here.
    /// </summary>
    /// <remarks>
    /// Supports direct/keyed borrowed prefabs, optional shared asset preparation, modeless Popup,
    /// the default host, DestroyOnClose or one inactive Reuse clone per definition, and DeactivateView.
    /// Deferred HUD, parent, host, rendering-only, and modal policies fail explicitly.
    /// Hooks may compose independent displays. Synchronous self-close/self-await and owner shutdown
    /// inside a hook are rejected; native application and cleanup reject all lifecycle mutation.
    /// Self-await after a callback's await remains forbidden usage outside the synchronous dispatch guard.
    /// Closed observers run after native retirement and handle termination, before Closed completes.
    /// A successful Reuse candidate is published only after its observer succeeds; failure discards it.
    /// </remarks>
    public sealed class UIContext : IDisposable
    {
        private readonly Dictionary<string, UIDefinition> _definitions = new Dictionary<string, UIDefinition>();
        private readonly List<UIHandle> _displays = new List<UIHandle>();
        private readonly Dictionary<string, AssetPreparation> _preparations =
            new Dictionary<string, AssetPreparation>(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> _cachedClones =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private Func<string, CancellationToken, UniTask<GameObject>> _loadPrefab;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly CancellationToken _lifetimeToken;
        private readonly UniTaskCompletionSource _shutdown = new UniTaskCompletionSource();
        private CancellationTokenRegistration _rootRegistration;
        private GameObject _storage;
        private long _nextId;
        private UIHandle _dispatching;
        private int _cleanupDepth;
        private int _nativeDepth;

        /// <summary>Connects a borrowed root and optional keyed prefab provider; input integration remains reserved.</summary>
        /// <param name="rootObject">Live owner root; await ShutdownAsync before destroying it.</param>
        /// <param name="loadPrefab">
        /// Returns a borrowed live prefab for an explicit key. The owner token cancels UI waits; the provider
        /// retains native asset/service ownership and may finish after UI shutdown. Late results are ignored.
        /// </param>
        /// <param name="acquireModalBlock">Reserved independent lease factory; Modal integration requires P4.</param>
        /// <exception cref="ArgumentNullException">The root is null or destroyed.</exception>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public UIContext(GameObject rootObject,
            Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
            Func<IDisposable> acquireModalBlock = null)
        {
            EnsureMainThread();
            if (rootObject == null)
            {
                throw new ArgumentNullException(nameof(rootObject));
            }
            RootObject = rootObject;
            _loadPrefab = loadPrefab;
            _lifetimeToken = _lifetime.Token;
            // Own only this registration. UniTask owns the native destruction trigger and its token.
            _rootRegistration = rootObject.GetCancellationTokenOnDestroy().Register(OnRootDestroyed);
        }

        /// <summary>Gets the borrowed root; graceful termination never destroys it.</summary>
        public GameObject RootObject
        {
            get;
        }
        /// <summary>Gets the cached owner token, also observable after termination.</summary>
        public CancellationToken LifetimeToken => _lifetimeToken;
        /// <summary>Gets whether permanent termination has begun.</summary>
        public bool IsDisposed
        {
            get; private set;
        }
        /// <summary>Gets a native state-application fault; ordinary callback errors belong to the handle.</summary>
        public Exception Fault
        {
            get; private set;
        }
        /// <summary>Gets a detached read-only snapshot of Opening, Visible, and Closing handles.</summary>
        public IReadOnlyList<UIHandle> Displays
        {
            get
            {
                EnsureMainThread();
                var current = new List<UIHandle>();
                foreach (UIHandle handle in _displays)
                {
                    if (handle.State != UIState.Closed)
                    {
                        current.Add(handle);
                    }
                }
                return current.AsReadOnly();
            }
        }

        /// <summary>Records metadata without loading or creating an instance, including from a display hook.</summary>
        /// <exception cref="ArgumentException">ID/source/policy metadata is invalid or duplicated.</exception>
        /// <exception cref="NotSupportedException">The definition requires a deferred presentation/input phase.</exception>
        /// <exception cref="InvalidOperationException">A key needs a configured provider, or the thread/mutation boundary is invalid.</exception>
        /// <exception cref="ObjectDisposedException">The owner is terminating.</exception>
        public void Register(UIDefinition definition)
        {
            EnsureCommand();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException("A definition needs a nonempty ID.", nameof(definition));
            }
            bool hasPrefab = definition.Prefab != null;
            bool hasKey = !string.IsNullOrWhiteSpace(definition.AssetKey);
            if (hasPrefab == hasKey)
            {
                throw new ArgumentException("Exactly one live prefab or asset key is required.", nameof(definition));
            }
            if (!Enum.IsDefined(typeof(UIRole), definition.Role)
                || !Enum.IsDefined(typeof(UIInputMode), definition.InputMode)
                || !Enum.IsDefined(typeof(UIRetention), definition.Retention)
                || !Enum.IsDefined(typeof(UIHideStrategy), definition.HideStrategy)
                || string.IsNullOrWhiteSpace(definition.HostId))
            {
                throw new ArgumentException("Invalid definition policy.", nameof(definition));
            }
            if (_definitions.ContainsKey(definition.Id))
            {
                throw new ArgumentException("The definition ID is already registered.", nameof(definition));
            }
            if (hasKey && _loadPrefab == null)
            {
                throw new InvalidOperationException("A key definition requires a configured prefab provider.");
            }
            if (definition.Role != UIRole.Popup || definition.HostId != "default")
            {
                throw new NotSupportedException("HUD and explicit Canvas hosts require P3.");
            }
            if (definition.InputMode != UIInputMode.Modeless
                || definition.HideStrategy != UIHideStrategy.DeactivateView)
            {
                throw new NotSupportedException("Modal input and renderer-only policies require P4.");
            }
            _definitions.Add(definition.Id, definition);
        }

        /// <summary>Optionally prepares an asset without clone creation, warm-up, or display.</summary>
        /// <param name="definitionId">Registered definition with a live direct prefab or an explicit provider key.</param>
        /// <param name="cancellationToken">Cancels only this caller's wait; another waiter and the shared owner load continue.</param>
        /// <returns>Borrowed asset readiness, shared by ordinal key until owner shutdown.</returns>
        /// <remarks>
        /// Failure is removed from the shared key cache; only a later explicit request retries it.
        /// Owner termination cancels waiters and prevents late publication without disposing the asset/provider.
        /// Display hooks may prepare independent definitions; cleanup/native reentry is rejected.
        /// </remarks>
        /// <exception cref="OperationCanceledException">The caller or owner ended its wait.</exception>
        /// <exception cref="InvalidOperationException">The provider returns no live prefab or the mutation boundary is invalid.</exception>
        public UniTask PrepareAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            cancellationToken.ThrowIfCancellationRequested();
            UIDefinition definition = GetDefinition(definitionId);
            return WaitForPrefabAsync(PreparePrefabAsync(definition), cancellationToken).AsUniTask();
        }

        /// <summary>
        /// Accepts and returns a generation immediately. Incomplete hooks keep it Opening; no frame delay is required.
        /// The request token cancels opening and its cleanup and is detached once Visible.
        /// </summary>
        /// <exception cref="ArgumentException">The definition is unknown or the request is invalid.</exception>
        /// <exception cref="InvalidOperationException">Duplicate owner/definition, thread violation, or cleanup/native reentry.</exception>
        /// <exception cref="OperationCanceledException">The request was cancelled before acceptance.</exception>
        /// <exception cref="ObjectDisposedException">The owner is terminating.</exception>
        public UIHandle BeginOpen(UIOpenRequest request, CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            UIDefinition definition = GetDefinition(request.DefinitionId);
            if (request.Parent != null)
            {
                throw new NotSupportedException("Logical parent trees require P3.");
            }
            UIInputMode inputMode = request.InputMode ?? definition.InputMode;
            if (!Enum.IsDefined(typeof(UIInputMode), inputMode))
            {
                throw new ArgumentException("Invalid input policy.", nameof(request));
            }
            if (inputMode != UIInputMode.Modeless)
            {
                throw new NotSupportedException("Modal input requires P4, including when a block factory is supplied.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            foreach (UIHandle current in _displays)
            {
                if (current.State != UIState.Closed && current.DefinitionId == request.DefinitionId)
                {
                    throw new InvalidOperationException("This owner already has this definition open or closing.");
                }
            }

            UIHandle handle = new UIHandle(this, ++_nextId, request, definition);
            _displays.Add(handle);
            handle.ConnectCallerCancellation(cancellationToken);
            RunOpenAsync(handle).Forget();
            return handle;
        }

        /// <summary>Calls BeginOpen and returns the same handle once its shared Opened result succeeds.</summary>
        public async UniTask<UIHandle> OpenAsync(UIOpenRequest request, CancellationToken cancellationToken = default)
        {
            UIHandle handle = BeginOpen(request, cancellationToken);
            await handle.Opened;
            return handle;
        }

        /// <summary>
        /// Permanently ends the owner, cancels asset waiters, and shares completion of display cleanup
        /// and native destruction of active, retiring, and cached owned clones. Borrowed providers may finish later.
        /// Cleanup continues after callback errors. Borrowed roots, sources, providers, and input services survive.
        /// </summary>
        /// <exception cref="InvalidOperationException">Called during a display callback, cleanup, or native state application.</exception>
        /// <exception cref="AggregateException">Cleanup failed after all remaining cleanup was attempted.</exception>
        public UniTask ShutdownAsync()
        {
            EnsureMainThread();
            EnsureNoReentry();
            BeginShutdown();
            return _shutdown.Task;
        }

        /// <summary>Starts the same idempotent owner fallback without awaiting asynchronous work.</summary>
        /// <remarks>Use ShutdownAsync before scene unload. Observe its shared result for deferred cleanup errors.</remarks>
        /// <exception cref="InvalidOperationException">Called during a display callback, cleanup, or native state application.</exception>
        /// <exception cref="AggregateException">Already-completed synchronous cleanup failed.</exception>
        public void Dispose()
        {
            EnsureMainThread();
            EnsureNoReentry();
            BeginShutdown();
            if (_shutdown.Task.Status == UniTaskStatus.Faulted)
            {
                _shutdown.Task.GetAwaiter().GetResult();
            }
        }

        private sealed class AssetPreparation
        {
            internal readonly UniTaskCompletionSource<GameObject> Completion =
                new UniTaskCompletionSource<GameObject>();
        }

        private async UniTask<GameObject> PreparePrefabAsync(UIDefinition definition)
        {
            EnsureMainThread();
            _lifetimeToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(definition.AssetKey))
            {
                if (definition.Prefab == null)
                {
                    throw new InvalidOperationException("The borrowed prefab was destroyed by its owner.");
                }
                return definition.Prefab;
            }

            string key = definition.AssetKey;
            if (!_preparations.TryGetValue(key, out AssetPreparation preparation))
            {
                preparation = new AssetPreparation();
                _preparations.Add(key, preparation);
                // Own and observe shared failures even when every caller has cancelled its wait.
                preparation.Completion.Task.Forget(
                    _ =>
                    {
                    }, false);
                RunPreparationAsync(key, preparation).Forget();
            }
            GameObject prefab = await preparation.Completion.Task;
            await UniTask.SwitchToMainThread();
            _lifetimeToken.ThrowIfCancellationRequested();
            if (prefab == null)
            {
                RemovePreparation(key, preparation);
                throw new InvalidOperationException("The prepared borrowed prefab is no longer alive.");
            }
            return prefab;
        }

        private async UniTask RunPreparationAsync(string key, AssetPreparation preparation)
        {
            try
            {
                EnsureMainThread();
                GameObject prefab = await _loadPrefab(key, _lifetimeToken);
                await UniTask.SwitchToMainThread();
                if (IsDisposed)
                {
                    // The native asset still belongs to its provider; a late result must never be published here.
                    preparation.Completion.TrySetCanceled(_lifetimeToken);
                    return;
                }
                if (prefab == null)
                {
                    throw new InvalidOperationException("The provider returned no live prefab for '" + key + "'.");
                }
                preparation.Completion.TrySetResult(prefab);
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                if (IsDisposed)
                {
                    preparation.Completion.TrySetCanceled(_lifetimeToken);
                }
                else
                {
                    RemovePreparation(key, preparation);
                    preparation.Completion.TrySetException(error);
                }
            }
        }

        private void RemovePreparation(string key, AssetPreparation preparation)
        {
            if (_preparations.TryGetValue(key, out AssetPreparation current)
                && ReferenceEquals(current, preparation))
            {
                _preparations.Remove(key);
            }
        }

        private static UniTask<GameObject> WaitForPrefabAsync(UniTask<GameObject> preparation,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return preparation;
            }
            var completion = new UniTaskCompletionSource<GameObject>();
            var registration = cancellationToken.Register(() =>
                completion.TrySetCanceled(cancellationToken));
            ForwardPreparationAsync(preparation, completion, registration).Forget();
            return completion.Task;
        }

        private static async UniTask ForwardPreparationAsync(UniTask<GameObject> preparation,
            UniTaskCompletionSource<GameObject> completion, CancellationTokenRegistration registration)
        {
            try
            {
                completion.TrySetResult(await preparation);
            }
            catch (Exception error)
            {
                // Late failure is consumed even if cancellation already completed this caller's wait.
                completion.TrySetException(error);
            }
            finally
            {
                registration.Dispose();
            }
        }

        private UIDefinition GetDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_definitions.TryGetValue(id, out UIDefinition definition))
            {
                throw new ArgumentException("Unknown UI definition.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(definition.AssetKey) && definition.Prefab == null)
            {
                throw new InvalidOperationException("The borrowed prefab was destroyed by its owner.");
            }
            return definition;
        }

        internal static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                throw new InvalidOperationException("UI operations require Unity's main thread.");
            }
        }

        private void EnsureCommand()
        {
            EnsureMainThread();
            EnsureMutationAllowed();
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(UIContext));
            }
            if (Fault != null)
            {
                throw new InvalidOperationException("The context has a native application fault.", Fault);
            }
            if (RootObject == null)
            {
                BeginShutdown();
                throw new ObjectDisposedException(nameof(UIContext));
            }
        }

        internal void EnsureMutationAllowed()
        {
            if (_cleanupDepth != 0 || _nativeDepth != 0)
            {
                throw new InvalidOperationException("Cleanup or native application cannot reenter lifecycle commands.");
            }
        }

        internal void EnsureNoReentry()
        {
            EnsureMutationAllowed();
            if (_dispatching != null)
            {
                throw new InvalidOperationException("A display callback cannot shut down its own context.");
            }
        }

        internal void EnsureObservation(UIHandle handle)
        {
            EnsureMainThread();
            if (_dispatching == handle || _cleanupDepth != 0)
            {
                throw new InvalidOperationException("A callback cannot await its own lifecycle operation.");
            }
        }

        internal void EnsureCleanupRegistration()
        {
            EnsureMainThread();
            if (_cleanupDepth != 0 || _nativeDepth != 0)
            {
                throw new InvalidOperationException("Cleanup or native application cannot register more cleanup.");
            }
        }

        internal UniTask DispatchAsync(UIHandle handle, Func<UIHandle, CancellationToken, UniTask> hook,
            CancellationToken token)
        {
            if (hook == null)
            {
                return UniTask.CompletedTask;
            }
            UIHandle previous = _dispatching;
            _dispatching = handle;
            try
            {
                return hook(handle, token);
            }
            finally
            {
                _dispatching = previous;
            }
        }

        internal void DispatchClosed(UIHandle handle, Action<UIHandle> callback)
        {
            if (callback == null)
            {
                return;
            }
            UIHandle previous = _dispatching;
            _dispatching = handle;
            try
            {
                callback(handle);
            }
            finally
            {
                _dispatching = previous;
            }
        }

        internal void DispatchCleanup(Action cleanup)
        {
            ++_cleanupDepth;
            try
            {
                cleanup();
            }
            finally
            {
                --_cleanupDepth;
            }
        }

        internal void Close(UIHandle handle)
        {
            EnsureMainThread();
            EnsureMutationAllowed();
            if (_dispatching == handle)
            {
                throw new InvalidOperationException("A callback cannot close its own display.");
            }
            StartClose(handle);
        }

        internal void CallerCancelled(UIHandle handle)
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, () => CallerCancelled(handle));
                return;
            }
            if (handle.State == UIState.Opening)
            {
                StartClose(handle);
            }
        }

        private async UniTask RunOpenAsync(UIHandle handle)
        {
            Exception openingError = null;
            try
            {
                handle.ThrowIfOpeningCancelled();
                GameObject prefab = await WaitForPrefabAsync(PreparePrefabAsync(handle.Definition), handle.LifetimeToken);
                await UniTask.SwitchToMainThread();
                handle.ThrowIfOpeningCancelled();
                ++_nativeDepth;
                try
                {
                    handle.View = TakeOrCreateClone(handle, prefab);
                }
                finally
                {
                    --_nativeDepth;
                }
                await DispatchAsync(handle, handle.Hooks.PrepareAsync, handle.LifetimeToken)
                    .AttachExternalCancellation(handle.LifetimeToken);
                await UniTask.SwitchToMainThread();
                handle.ThrowIfOpeningCancelled();
                ++_nativeDepth;
                try
                {
                    handle.View.transform.SetParent(RootObject.transform, false);
                    handle.View.SetActive(true);
                }
                finally
                {
                    --_nativeDepth;
                }
                await DispatchAsync(handle, handle.Hooks.OpenAsync, handle.LifetimeToken)
                    .AttachExternalCancellation(handle.LifetimeToken);
                await UniTask.SwitchToMainThread();
                handle.ThrowIfOpeningCancelled();
                handle.DetachCaller();
                handle.CurrentState = UIState.Visible;
                handle.OpenCompletion.TrySetResult();
            }
            catch (Exception error)
            {
                openingError = error;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                handle.OpeningDone.TrySetResult();
            }

            if (openingError == null)
            {
                return;
            }
            StartClose(handle);
            Exception cleanupError = null;
            try
            {
                await handle.CloseCompletion.Task;
            }
            catch (Exception error)
            {
                cleanupError = error;
            }
            if (cleanupError != null)
            {
                handle.OpenCompletion.TrySetException(new AggregateException(openingError, cleanupError));
            }
            else if (openingError is OperationCanceledException cancelled)
            {
                handle.OpenCompletion.TrySetCanceled(cancelled.CancellationToken);
            }
            else
            {
                handle.OpenCompletion.TrySetException(openingError);
            }
        }

        private GameObject TakeOrCreateClone(UIHandle handle, GameObject prefab)
        {
            if (_storage == null)
            {
                _storage = new GameObject("UIContext Inactive Storage");
                _storage.SetActive(false);
                _storage.transform.SetParent(RootObject.transform, false);
            }

            GameObject clone;
            if (_cachedClones.TryGetValue(handle.DefinitionId, out clone))
            {
                _cachedClones.Remove(handle.DefinitionId);
            }
            else
            {
                clone = null;
            }
            if (clone == null)
            {
                clone = UnityEngine.Object.Instantiate(prefab, _storage.transform, false);
            }
            // Track the rental before native configuration so any failure reaches normal close/discard.
            handle.View = clone;
            clone.SetActive(false);
            clone.transform.SetParent(_storage.transform, false);
            clone.transform.localPosition = prefab.transform.localPosition;
            clone.transform.localRotation = prefab.transform.localRotation;
            clone.transform.localScale = prefab.transform.localScale;
            return clone;
        }

        private bool TryCacheClone(string definitionId, GameObject clone)
        {
            if (IsDisposed || clone == null || _storage == null)
            {
                return false;
            }
            if (_cachedClones.TryGetValue(definitionId, out GameObject current) && current != null)
            {
                return false;
            }
            _cachedClones[definitionId] = clone;
            return true;
        }

        private void StartClose(UIHandle handle)
        {
            if (handle.CloseStarted)
            {
                return;
            }
            handle.CloseStarted = true;
            bool wasVisible = handle.CurrentState == UIState.Visible;
            handle.CurrentState = UIState.Closing;
            handle.DetachCaller();
            handle.CancelLifetime();
            RunCloseAsync(handle, wasVisible).Forget();
        }

        private async UniTask RunCloseAsync(UIHandle handle, bool wasVisible)
        {
            Action<UIHandle> closedCallback = handle.Hooks.Closed;
            bool requestedReuse = handle.Definition.Retention == UIRetention.Reuse;
            GameObject candidate = null;
            try
            {
                try
                {
                    await handle.OpeningDone.Task.AttachExternalCancellation(_lifetimeToken);
                }
                catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
                {
                }
                if (wasVisible && !_lifetimeToken.IsCancellationRequested)
                {
                    try
                    {
                        await DispatchAsync(handle, handle.Hooks.CloseAsync, _lifetimeToken)
                            .AttachExternalCancellation(_lifetimeToken);
                    }
                    catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
                    {
                    }
                    catch (Exception error)
                    {
                        await UniTask.SwitchToMainThread();
                        handle.Errors.Add(error);
                    }
                }
                await UniTask.SwitchToMainThread();
                handle.RunCleanup();
                GameObject view = handle.View;
                if (view != null)
                {
                    ++_nativeDepth;
                    try
                    {
                        view.SetActive(false);
                        if (requestedReuse && wasVisible && handle.Errors.Count == 0
                            && !IsDisposed && _storage != null)
                        {
                            view.transform.SetParent(_storage.transform, false);
                            candidate = view;
                        }
                    }
                    catch (Exception error)
                    {
                        RecordNativeFailure(handle, error);
                    }
                    finally
                    {
                        --_nativeDepth;
                    }
                    if (candidate == null)
                    {
                        try
                        {
                            await DestroyOwnedAsync(view);
                        }
                        catch (Exception error)
                        {
                            RecordNativeFailure(handle, error);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                handle.Errors.Add(error);
                handle.RunCleanup();
            }
            finally
            {
                handle.Finish();
                try
                {
                    DispatchClosed(handle, closedCallback);
                }
                catch (Exception error)
                {
                    handle.Errors.Add(error);
                }
                // A retired candidate is private until its observer succeeds. Nested displays cannot rent it.
                if (candidate != null
                    && (handle.Errors.Count != 0 || !TryCacheClone(handle.DefinitionId, candidate)))
                {
                    try
                    {
                        await DestroyOwnedAsync(candidate);
                    }
                    catch (Exception error)
                    {
                        RecordNativeFailure(handle, error);
                    }
                }
                // Keep this closing generation tracked through observer/discard so owner shutdown awaits it.
                _displays.Remove(handle);
                if (handle.Errors.Count == 0)
                {
                    handle.CloseCompletion.TrySetResult();
                }
                else
                {
                    handle.CloseCompletion.TrySetException(new AggregateException(handle.Errors));
                }
            }
        }

        private void RecordNativeFailure(UIHandle handle, Exception error)
        {
            Fault = Fault ?? error;
            handle.Errors.Add(error);
        }

        private async UniTask DestroyOwnedAsync(GameObject owned)
        {
            if (owned == null)
            {
                return;
            }
            ++_nativeDepth;
            try
            {
                if (!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(owned);
                }
                else
                {
                    UnityEngine.Object.Destroy(owned);
                }
            }
            finally
            {
                --_nativeDepth;
            }
            while (owned != null)
            {
                await UniTask.NextFrame();
            }
        }

        private void OnRootDestroyed()
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, OnRootDestroyed);
                return;
            }
            BeginShutdown();
        }

        private void BeginShutdown()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            UIHandle[] owned = _displays.ToArray();
            var cached = new List<GameObject>(_cachedClones.Values);
            _cachedClones.Clear();
            var preparations = new List<AssetPreparation>(_preparations.Values);
            _preparations.Clear();
            _rootRegistration.Dispose();
            List<Exception> errors = new List<Exception>();
            try
            {
                _lifetime.Cancel();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            foreach (AssetPreparation preparation in preparations)
            {
                preparation.Completion.TrySetCanceled(_lifetimeToken);
            }
            foreach (UIHandle handle in owned)
            {
                StartClose(handle);
            }
            RunShutdownAsync(owned, cached, errors).Forget();
        }

        private async UniTask RunShutdownAsync(UIHandle[] owned, List<GameObject> cached, List<Exception> errors)
        {
            foreach (UIHandle handle in owned)
            {
                try
                {
                    await handle.CloseCompletion.Task;
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            foreach (GameObject clone in cached)
            {
                try
                {
                    await DestroyOwnedAsync(clone);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                    Fault = Fault ?? error;
                }
            }
            try
            {
                await DestroyOwnedAsync(_storage);
            }
            catch (Exception error)
            {
                errors.Add(error);
                Fault = Fault ?? error;
            }
            _storage = null;
            _loadPrefab = null;
            _definitions.Clear();
            _lifetime.Dispose();
            if (errors.Count == 0)
            {
                _shutdown.TrySetResult();
            }
            else
            {
                _shutdown.TrySetException(new AggregateException(errors));
            }
        }
    }
}
