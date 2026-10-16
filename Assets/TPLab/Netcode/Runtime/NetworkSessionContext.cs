using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace TPLab.Netcode
{
    /// <summary>Owns one preparation attempt and its cleanup while borrowing a project-configured NetworkManager.</summary>
    /// <remarks>Main-thread only. Create a fresh context for a fresh attempt. Await ShutdownAsync before destroying the borrowed manager.
    /// Caller cancellation only cancels that wait. Hooks must own their late work; a deadline cannot forcibly terminate a hook.</remarks>
    public sealed class NetworkSessionContext
    {
        private static readonly ConditionalWeakTable<NetworkManager, NetworkSessionContext> Leases = new ConditionalWeakTable<NetworkManager, NetworkSessionContext>();
        private readonly NetworkManager _manager;
        private readonly INetworkSessionCallbacks _callbacks;
        private readonly NetworkSessionOptions _options;
        private readonly IAdmissionVerifier _verifier;
        private readonly List<Exception> _recoveryErrors = new List<Exception>();
        private NetworkSessionState _state;
        private NetworkSessionResult _lastResult;
        private NetworkSessionFailure _failure;
        private NetworkSessionAttempt _attempt;
        private SessionStartCoordinator _endpoint;
        private CancellationTokenSource _attemptCancellation;
        private CancellationTokenSource _cleanupCancellation;
        private UniTaskCompletionSource<NetworkSessionResult> _start;
        private UniTaskCompletionSource<NetworkSessionResult> _stop;
        private HookOperation _protection;
        private HookOperation _preparation;
        private HookOperation _restore;
        private bool _leaseHeld;
        private bool _cleanupStarted;
        private bool _cleanupFinished;
        private bool _cleanupSucceeded;
        private bool _cleanupTimeoutRecorded;
        private bool _insideCallback;
        private bool _retryActive;
        private double _preparationDeadline;

        /// <summary>Gets the current local lifecycle state. A timed-out hook may still be pending in RecoveryFault.</summary>
        public NetworkSessionState State { get { EnsureMainThread(); return _state; } }
        /// <summary>Gets the latest detached result; later recovery errors do not mutate earlier results.</summary>
        public NetworkSessionResult LastResult { get { EnsureMainThread(); return _lastResult; } }
        /// <summary>Gets the latest local admission diagnostic. Never forward Cause to remote clients.</summary>
        public AdmissionRejection LastAdmissionRejection { get { EnsureMainThread(); return _endpoint?.LastAdmissionRejection; } }
        /// <summary>Gets whether actual native/hook cleanup is complete and no previous restore is still pending.</summary>
        public bool CanRetryLobbyRestore
        {
            get
            {
                EnsureMainThread();
                return _state == NetworkSessionState.RecoveryFault && _cleanupSucceeded && !_retryActive
                    && (_restore == null || _restore.Completed) && (_stop == null || _stop.Task.Status != UniTaskStatus.Pending);
            }
        }

        /// <summary>Borrows an explicit manager and project callbacks. No native startup or configuration mutation occurs here.</summary>
        /// <exception cref="ArgumentNullException">A manager, callbacks or options is null.</exception>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public NetworkSessionContext(NetworkManager manager, INetworkSessionCallbacks callbacks,
            NetworkSessionOptions options, IAdmissionVerifier verifier = null)
        {
            EnsureMainThread();
            _manager = manager != null ? manager : throw new ArgumentNullException(nameof(manager));
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _verifier = verifier == null ? null : new GuardedVerifier(this, verifier);
        }

        /// <summary>Starts this context's single attempt. Protect, native synchronization, local prepare and server authorization precede gameplay.</summary>
        /// <remarks>Cancellation cancels only this caller's wait. ShutdownAsync cancels the owned attempt. Project failures are returned in the result.</remarks>
        /// <exception cref="ArgumentNullException">Request is null.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, synchronous callback reentry, reused context, or unavailable/invalid native endpoint.</exception>
        public UniTask<NetworkSessionResult> StartAsync(NetworkSessionRequest request, CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (_state != NetworkSessionState.Created) throw new InvalidOperationException("A session context owns one attempt only.");
            cancellationToken.ThrowIfCancellationRequested();
            if (Leases.TryGetValue(_manager, out _)) throw new InvalidOperationException("The NetworkManager already has a session owner.");
            double startedAt = Now;
            var endpoint = new SessionStartCoordinator(_manager, request, _options, _verifier, startedAt);
            Leases.Add(_manager, this);
            _leaseHeld = true;
            _endpoint = endpoint;
            _attempt = new NetworkSessionAttempt(request.Identity, request.IsHost);
            _attemptCancellation = new CancellationTokenSource();
            _preparationDeadline = startedAt + _options.PreparationTimeoutSeconds;
            _start = new UniTaskCompletionSource<NetworkSessionResult>();
            _state = NetworkSessionState.Starting;
            RunStartAsync().Forget(UnexpectedFailure);
            return Wait(_start.Task, cancellationToken);
        }

        /// <summary>Cancels pre-start work or closes running gameplay, sharing cleanup with other callers. Running close does not restore the lobby automatically.</summary>
        /// <remarks>A RecoveryFault result means cleanup was not proven complete within its observation budget. It is unsafe to destroy pending hooks.</remarks>
        public UniTask<NetworkSessionResult> ShutdownAsync(CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            cancellationToken.ThrowIfCancellationRequested();
            if (_stop != null && (_stop.Task.Status == UniTaskStatus.Pending || _state == NetworkSessionState.Closed))
                return Wait(_stop.Task, cancellationToken);
            if (_state == NetworkSessionState.Created) return UniTask.FromResult(Complete(NetworkSessionState.Closed));
            if (_state == NetworkSessionState.Closed || _state == NetworkSessionState.Lobby) return UniTask.FromResult(_lastResult);
            if (_start != null && _start.Task.Status == UniTaskStatus.Pending)
            {
                Fail(new NetworkSessionFailure(NetworkSessionFailureReason.OwnerCancelled));
                Cancel(_attemptCancellation);
                return Wait(_start.Task, cancellationToken);
            }
            if (_retryActive) throw new InvalidOperationException("Lobby restoration is already running.");
            _stop = new UniTaskCompletionSource<NetworkSessionResult>();
            RunStopAsync().Forget(UnexpectedFailure);
            return Wait(_stop.Task, cancellationToken);
        }

        /// <summary>Explicitly retries lobby restoration after verified cleanup. Uses the original SessionId/AttemptId key and original failure.</summary>
        /// <exception cref="InvalidOperationException">Recovery is unavailable, a previous hook is pending, or synchronous callback reentry occurs.</exception>
        public UniTask<NetworkSessionResult> RetryLobbyRestoreAsync(CancellationToken cancellationToken = default)
        {
            EnsureCommand();
            if (!CanRetryLobbyRestore) throw new InvalidOperationException("Lobby restoration requires completed native and hook cleanup.");
            cancellationToken.ThrowIfCancellationRequested();
            _retryActive = true;
            var result = RetryAsync().Preserve();
            return Wait(result, cancellationToken);
        }

        private async UniTask RunStartAsync()
        {
            _protection = Invoke(() => _callbacks.ProtectAsync(_attempt, _attemptCancellation.Token));
            await ObservePreparationAsync(_protection, NetworkSessionFailureReason.ProtectionFailed);
            if (_failure == null)
            {
                try { _endpoint.Start(); }
                catch (Exception error)
                {
                    Fail(_endpoint.Failure ?? new NetworkSessionFailure(
                        _endpoint.LastAdmissionRejection == null ? NetworkSessionFailureReason.EndpointFailed : NetworkSessionFailureReason.AdmissionRejected,
                        _endpoint.LastAdmissionRejection?.Cause ?? error));
                }
            }
            while (_failure == null && !_endpoint.NativeReady)
            {
                CheckAttempt();
                if (_failure == null) await UniTask.Yield();
            }
            if (_failure == null)
            {
                _state = NetworkSessionState.Preparing;
                _preparation = Invoke(() => _callbacks.PrepareAsync(_attempt, _attemptCancellation.Token));
                await ObservePreparationAsync(_preparation, NetworkSessionFailureReason.LocalPreparationFailed);
            }
            if (_failure == null)
            {
                try { _endpoint.MarkLocalReady(); }
                catch (Exception error) { Fail(new NetworkSessionFailure(NetworkSessionFailureReason.EndpointFailed, error)); }
            }
            while (_failure == null && !_endpoint.StartAuthorized)
            {
                CheckAttempt();
                if (_failure == null) await UniTask.Yield();
            }
            CheckAttempt();
            if (_failure == null)
            {
                try
                {
                    _insideCallback = true;
                    _callbacks.StartGameplay(_attempt);
                }
                catch (Exception error) { Fail(new NetworkSessionFailure(NetworkSessionFailureReason.GameplayGateFailed, error)); }
                finally { _insideCallback = false; }
            }
            if (_failure == null)
            {
                _start.TrySetResult(Complete(NetworkSessionState.Running));
                return;
            }
            bool cleaned = await CleanupWithinBudgetAsync();
            bool restoreAllowed = cleaned && !_endpoint.StartAuthorized && _failure.Reason != NetworkSessionFailureReason.GameplayGateFailed
                && _failure.Reason != NetworkSessionFailureReason.ProtectionFailed;
            var outcome = restoreAllowed ? await RestoreAsync() : Complete(NetworkSessionState.RecoveryFault);
            _start.TrySetResult(outcome);
        }

        private async UniTask ObservePreparationAsync(HookOperation hook, NetworkSessionFailureReason failureReason)
        {
            while (!hook.Completed && _failure == null)
            {
                CheckAttempt();
                if (_failure == null) await UniTask.Yield();
            }
            CheckAttempt();
            if (_failure == null && hook.Error != null) Fail(new NetworkSessionFailure(failureReason, hook.Error));
        }

        private void CheckAttempt()
        {
            if (_failure != null) return;
            try
            {
                _endpoint.Tick();
                if (_endpoint.Failure != null) Fail(_endpoint.Failure);
                else if (Now >= _preparationDeadline)
                    Fail(new NetworkSessionFailure(NetworkSessionFailureReason.DeadlineExceeded));
            }
            catch (Exception error) { Fail(new NetworkSessionFailure(NetworkSessionFailureReason.EndpointFailed, error)); }
        }

        private async UniTask RunStopAsync()
        {
            bool cleaned = await CleanupWithinBudgetAsync();
            double deadline = Now + _options.ShutdownTimeoutSeconds;
            while (_restore != null && !_restore.Completed && Now < deadline) await UniTask.Yield();
            bool restoreFinished = _restore == null || _restore.Completed;
            _stop.TrySetResult(Complete(cleaned && restoreFinished ? NetworkSessionState.Closed : NetworkSessionState.RecoveryFault));
        }

        private async UniTask<bool> CleanupWithinBudgetAsync()
        {
            bool firstObservation = !_cleanupStarted;
            double deadline = Now + _options.ShutdownTimeoutSeconds;
            if (!_cleanupStarted)
            {
                _cleanupStarted = true;
                _state = NetworkSessionState.Aborting;
                _cleanupCancellation = new CancellationTokenSource();
                Cancel(_attemptCancellation);
                CleanupAsync().Forget(error =>
                {
                    _recoveryErrors.Add(error);
                    _cleanupFinished = true;
                });
            }
            while (!_cleanupFinished && Now < deadline) await UniTask.Yield();
            bool expired = !_cleanupFinished || firstObservation && Now >= deadline;
            if (expired)
            {
                if (!_cleanupTimeoutRecorded)
                {
                    _cleanupTimeoutRecorded = true;
                    _recoveryErrors.Add(new TimeoutException("Session cleanup did not complete within the project budget."));
                }
                if (!_cleanupFinished) Cancel(_cleanupCancellation);
            }
            return !expired && _cleanupFinished && _cleanupSucceeded;
        }

        private async UniTask CleanupAsync()
        {
            bool clean = true;
            try
            {
                if (_failure != null) _endpoint.Abort(ToProtocolFailure(_failure.Reason), _failure.Cause);
            }
            catch (Exception error) { _recoveryErrors.Add(error); clean = false; }
            // Never overlap a second protect/release with a hook that ignored cancellation.
            await FinishAsync(_protection);
            var cover = Invoke(() => _callbacks.ProtectAsync(_attempt, _cleanupCancellation.Token));
            await FinishAsync(cover);
            if (cover.Error != null) { _recoveryErrors.Add(cover.Error); clean = false; }
            try { _endpoint.BeginShutdown(); }
            catch (Exception error) { _recoveryErrors.Add(error); clean = false; }
            await FinishAsync(_preparation);
            if (_preparation?.Error != null && !ReferenceEquals(_preparation.Error, _failure?.Cause)
                && !(_preparation.Error is OperationCanceledException)) _recoveryErrors.Add(_preparation.Error);
            var release = Invoke(() => _callbacks.ReleaseAsync(_attempt, _cleanupCancellation.Token));
            await FinishAsync(release);
            if (release.Error != null) { _recoveryErrors.Add(release.Error); clean = false; }
            while (!_endpoint.NativeStopped)
            {
                _endpoint.Tick();
                await UniTask.Yield();
            }
            _endpoint.Dispose();
            _attemptCancellation.Dispose();
            _cleanupCancellation.Dispose();
            _cleanupSucceeded = clean;
            _cleanupFinished = true;
        }

        private async UniTask<NetworkSessionResult> RetryAsync()
        {
            try { return await RestoreAsync(); }
            finally { _retryActive = false; }
        }

        private async UniTask<NetworkSessionResult> RestoreAsync()
        {
            _state = NetworkSessionState.RestoringLobby;
            var cancellation = new CancellationTokenSource();
            double deadline = Now + _options.RestoreTimeoutSeconds;
            _restore = Invoke(() => _callbacks.RestoreLobbyAsync(
                new NetworkSessionRecovery(_attempt, _failure, _recoveryErrors), cancellation.Token));
            var restore = _restore;
            while (!restore.Completed && Now < deadline) await UniTask.Yield();
            bool expired = !restore.Completed || Now >= deadline;
            if (expired)
            {
                _recoveryErrors.Add(new TimeoutException("Lobby restoration did not complete within the project budget."));
                if (!restore.Completed) Cancel(cancellation);
            }
            if (!restore.Completed) ObserveLateRestoreAsync(restore, cancellation).Forget(UnexpectedFailure);
            else
            {
                cancellation.Dispose();
                if (restore.Error != null) _recoveryErrors.Add(restore.Error);
            }
            return Complete(!expired && restore.Error == null ? NetworkSessionState.Lobby : NetworkSessionState.RecoveryFault);
        }

        private async UniTask ObserveLateRestoreAsync(HookOperation restore, CancellationTokenSource cancellation)
        {
            await FinishAsync(restore);
            if (restore.Error != null) _recoveryErrors.Add(restore.Error);
            cancellation.Dispose();
        }

        private HookOperation Invoke(Func<UniTask> callback)
        {
            var operation = new HookOperation();
            try
            {
                _insideCallback = true;
                ObserveHookAsync(callback(), operation).Forget(UnexpectedFailure);
            }
            catch (Exception error) { operation.Error = error; operation.Completed = true; }
            finally { _insideCallback = false; }
            return operation;
        }

        private static async UniTask ObserveHookAsync(UniTask task, HookOperation operation)
        {
            Exception failure = null;
            try { await task; }
            catch (Exception error) { failure = error; }
            // A project hook may complete on a worker. All owner state is published on Unity's thread.
            await UniTask.SwitchToMainThread();
            operation.Error = failure;
            operation.Completed = true;
        }

        private static async UniTask FinishAsync(HookOperation operation)
        {
            if (operation == null) return;
            while (!operation.Completed) await UniTask.Yield();
        }
        private static UniTask<NetworkSessionResult> Wait(UniTask<NetworkSessionResult> task, CancellationToken token)
            => token.CanBeCanceled ? task.AttachExternalCancellation(token) : task;
        private void Fail(NetworkSessionFailure failure) { if (_failure == null) _failure = failure; }
        private void Cancel(CancellationTokenSource source)
        {
            try { source?.Cancel(); }
            catch (Exception error) { _recoveryErrors.Add(error); }
        }
        private NetworkSessionResult Complete(NetworkSessionState state)
        {
            _state = state;
            if (_leaseHeld && (state == NetworkSessionState.Lobby || state == NetworkSessionState.Closed))
            {
                Leases.Remove(_manager);
                _leaseHeld = false;
            }
            _lastResult = new NetworkSessionResult(state, _failure, _recoveryErrors);
            return _lastResult;
        }
        private void UnexpectedFailure(Exception error)
        {
            Fail(new NetworkSessionFailure(NetworkSessionFailureReason.EndpointFailed, error));
            var result = Complete(NetworkSessionState.RecoveryFault);
            _start?.TrySetResult(result);
            _stop?.TrySetResult(result);
        }
        private void EnsureCommand()
        {
            EnsureMainThread();
            if (_insideCallback) throw new InvalidOperationException("Session commands cannot reenter their project callbacks.");
        }
        private static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Network sessions require Unity's main thread.");
        }
        private static double Now => Time.realtimeSinceStartupAsDouble;
        private static SessionFailureCode ToProtocolFailure(NetworkSessionFailureReason reason)
        {
            switch (reason)
            {
                case NetworkSessionFailureReason.OwnerCancelled: return SessionFailureCode.OwnerCancelled;
                case NetworkSessionFailureReason.DeadlineExceeded: return SessionFailureCode.DeadlineExceeded;
                default: return SessionFailureCode.LocalPreparationFailed;
            }
        }
        private sealed class GuardedVerifier : IAdmissionVerifier
        {
            private readonly NetworkSessionContext _owner;
            private readonly IAdmissionVerifier _verifier;

            internal GuardedVerifier(NetworkSessionContext owner, IAdmissionVerifier verifier)
            { _owner = owner; _verifier = verifier; }

            public AdmissionDecision Verify(AdmissionRequest request)
            {
                bool previous = _owner._insideCallback;
                try
                {
                    _owner._insideCallback = true;
                    return _verifier.Verify(request);
                }
                finally { _owner._insideCallback = previous; }
            }
        }
        private sealed class HookOperation
        {
            internal bool Completed;
            internal Exception Error;
        }
    }
}
