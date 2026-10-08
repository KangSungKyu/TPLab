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
    /// P1 supports direct prefabs, modeless Popup, the default host, DestroyOnClose, and DeactivateView.
    /// Deferred provider, retention, HUD, parent, host, rendering-only, and modal policies fail explicitly.
    /// Hooks may compose independent displays. Synchronous self-close/self-await and owner shutdown
    /// inside a hook are rejected; native application and cleanup reject all lifecycle mutation.
    /// Self-await after a callback's await remains forbidden usage outside the synchronous dispatch guard.
    /// Closed observers run after native destruction and handle termination, before Closed completes.
    /// </remarks>
    public sealed class UIContext : IDisposable
    {
        private readonly Dictionary<string, UIDefinition> _definitions = new Dictionary<string, UIDefinition>();
        private readonly List<UIHandle> _displays = new List<UIHandle>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly CancellationToken _lifetimeToken;
        private readonly UniTaskCompletionSource _shutdown = new UniTaskCompletionSource();
        private CancellationTokenRegistration _rootRegistration;
        private GameObject _storage;
        private long _nextId;
        private UIHandle _dispatching;
        private int _cleanupDepth;
        private int _nativeDepth;

        /// <summary>Connects borrowed services. Optional provider/input boundaries are reserved for later phases.</summary>
        /// <param name="rootObject">Live owner root; await ShutdownAsync before destroying it.</param>
        /// <param name="loadPrefab">Reserved borrowed provider; P1 rejects key definitions and does not invoke it.</param>
        /// <param name="acquireModalBlock">Reserved independent lease factory; P1 rejects Modal requests.</param>
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
                return Array.AsReadOnly(_displays.ToArray());
            }
        }

        /// <summary>Records metadata without loading or creating an instance, including from a display hook.</summary>
        /// <exception cref="ArgumentException">ID/source/policy metadata is invalid or duplicated.</exception>
        /// <exception cref="NotSupportedException">The definition requires a deferred phase.</exception>
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
            if (hasKey || definition.Retention != UIRetention.DestroyOnClose)
            {
                throw new NotSupportedException("Asset providers and reuse require P2.");
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

        /// <summary>Checks direct prefab readiness without instantiation or warm-up.</summary>
        /// <remarks>Cancellation affects this request. Display hooks may prepare independent definitions; cleanup/native reentry is rejected.</remarks>
        public UniTask PrepareAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            cancellationToken.ThrowIfCancellationRequested();
            GetDefinition(definitionId);
            return UniTask.CompletedTask;
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
                if (current.DefinitionId == request.DefinitionId)
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
        /// Permanently ends the owner and shares completion of every display cleanup and owned native destruction.
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

        private UIDefinition GetDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_definitions.TryGetValue(id, out UIDefinition definition))
            {
                throw new ArgumentException("Unknown UI definition.", nameof(id));
            }
            if (definition.Prefab == null)
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
                if (_storage == null)
                {
                    _storage = new GameObject("UIContext Inactive Storage");
                    _storage.SetActive(false);
                    _storage.transform.SetParent(RootObject.transform, false);
                }
                ++_nativeDepth;
                try
                {
                    handle.View = UnityEngine.Object.Instantiate(handle.Definition.Prefab, _storage.transform, false);
                    handle.View.SetActive(false);
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
                    }
                    catch (Exception error)
                    {
                        RecordNativeFailure(handle, error);
                    }
                    finally
                    {
                        --_nativeDepth;
                    }
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
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                handle.Errors.Add(error);
                // Managed cleanup remains obligatory even if a native boundary unexpectedly fails.
                handle.RunCleanup();
            }
            finally
            {
                handle.Finish();
                _displays.Remove(handle);
                try
                {
                    DispatchClosed(handle, closedCallback);
                }
                catch (Exception error)
                {
                    handle.Errors.Add(error);
                }
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

        private static async UniTask DestroyOwnedAsync(GameObject owned)
        {
            if (owned == null)
            {
                return;
            }
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(owned);
                return;
            }
            UnityEngine.Object.Destroy(owned);
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
            foreach (UIHandle handle in owned)
            {
                StartClose(handle);
            }
            RunShutdownAsync(owned, errors).Forget();
        }

        private async UniTask RunShutdownAsync(UIHandle[] owned, List<Exception> errors)
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
