using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Netcode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPLab.Samples.Netcode
{
    /// <summary>Demonstrates a fixed-scene Host and two clients with explicit preparation and recovery controls.</summary>
    /// <remarks>
    /// Main-thread only. Demo proof strings are loopback examples, not authentication.
    /// The project owns the configured manager and must await ShutdownAsync before destroying this component or its manager.
    /// Native scene management is borrowed unchanged; this sample uses Additive initial synchronization only.
    /// </remarks>
    public sealed class NetcodeSessionSample : MonoBehaviour, INetworkSessionCallbacks
    {
        private const string SceneKey = "TPLab.Netcode.P5.Fixed";
        private readonly HashSet<Tuple<string, string>> _attemptHistory = new HashSet<Tuple<string, string>>();
        private NetworkManager _manager;
        private NetworkSessionOptions _options;
        private bool _preparationCompleted;
        private Exception _preparationError;
        private bool _failNextRestore;
        private bool _destroyed;
        private bool _starting;
        private bool _stopping;
        private bool _retrying;
        private bool _host;
        private int _participantCount;
        private UniTask<NetworkSessionResult> _ownedStart;
        private UniTask<NetworkSessionResult> _ownedStop;
        private UniTask<NetworkSessionResult> _ownedRetry;

        /// <summary>Gets whether the sample protection cover blocks gameplay visibility and input.</summary>
        public bool IsProtected { get; private set; } = true;
        /// <summary>Gets whether the local preparation callback is waiting or completing.</summary>
        public bool IsPreparing { get; private set; }
        /// <summary>Gets whether the sample gameplay gate has opened; RecordGameplayInput also checks the current connection.</summary>
        public bool GameplayEnabled { get; private set; }
        /// <summary>Gets concise local lobby, preparation, gameplay or recovery status for the project view.</summary>
        public string StatusText { get; private set; } = "Not configured";
        /// <summary>Gets the latest owned context. A fresh context is created for every accepted attempt.</summary>
        public NetworkSessionContext Context { get; private set; }
        /// <summary>Gets the latest detached result from the current context, including a result completed after caller cancellation.</summary>
        public NetworkSessionResult LastResult => Context?.LastResult;
        /// <summary>Gets the number of local preparation callbacks during this component's lifetime.</summary>
        public int PrepareCalls { get; private set; }
        /// <summary>Gets the number of release callbacks during this component's lifetime.</summary>
        public int ReleaseCalls { get; private set; }
        /// <summary>Gets the number of lobby restoration callbacks, including explicit retries.</summary>
        public int RestoreCalls { get; private set; }
        /// <summary>Gets the lifetime count of accepted sample gameplay button presses.</summary>
        public int GameplayInputCount { get; private set; }
        /// <summary>Injects one local restore failure. Set only before starting an attempt or after a safe terminal result.</summary>
        /// <exception cref="InvalidOperationException">A context or owned operation is still active.</exception>
        public bool FailNextRestore
        {
            get => _failNextRestore;
            set
            {
                EnsureUsable();
                EnsureIdle();
                _failNextRestore = value;
            }
        }

        /// <summary>Borrows an active, top-level, idle manager and explicit limits without changing native configuration.</summary>
        /// <remarks>All peers must already be in the fixed sample scene. Counters and the small demo attempt history last for this component's lifetime.</remarks>
        /// <exception cref="ArgumentNullException">The manager or options is null.</exception>
        /// <exception cref="InvalidOperationException">Called off the main thread, during an attempt, or with an unavailable native manager.</exception>
        public void Configure(NetworkManager manager, NetworkSessionOptions options)
        {
            EnsureUsable();
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (options == null) throw new ArgumentNullException(nameof(options));
            EnsureIdle();
            ValidateManager(manager);
            _manager = manager;
            _options = options;
            Context = null;
            IsProtected = false;
            GameplayEnabled = false;
            IsPreparing = false;
            StatusText = "Lobby";
        }

        /// <summary>Starts a fresh Host attempt using a unique session/attempt pair and a subset of host, c1 and c2 that includes host.</summary>
        /// <remarks>Use demo-host proof for the Host. Caller cancellation stops only this wait; ShutdownAsync stops owned work.</remarks>
        /// <exception cref="ArgumentException">Identity, roster or proof input is invalid.</exception>
        /// <exception cref="InvalidOperationException">The sample is unavailable, an attempt is active, or this attempt key was already used.</exception>
        public UniTask<NetworkSessionResult> StartHostAsync(string sessionId, string attemptId,
            string[] roster, byte[] proof, CancellationToken caller = default)
        {
            EnsureCanStart(caller);
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            var members = new HashSet<string>(StringComparer.Ordinal);
            if (roster.Length == 0 || roster.Length > 3) throw new ArgumentException("Use one to three demo members.", nameof(roster));
            foreach (string member in roster)
            {
                if ((member != "host" && member != "c1" && member != "c2") || !members.Add(member))
                    throw new ArgumentException("Demo members must be unique host, c1 or c2 identities.", nameof(roster));
            }
            if (!members.Contains("host")) throw new ArgumentException("The demo Host roster must include host.", nameof(roster));
            var identity = new NetworkSessionIdentity(sessionId, attemptId, 1, 1, SceneKey);
            return StartOwned(NetworkSessionRequest.Host(identity, roster, proof, LoadSceneMode.Additive), caller);
        }

        /// <summary>Starts a fresh client attempt; use demo-c1 or demo-c2 proof for the loopback demonstration.</summary>
        /// <remarks>Caller cancellation stops only this wait. This method does not configure a transport address or load a scene.</remarks>
        /// <exception cref="ArgumentException">Identity or proof is invalid.</exception>
        /// <exception cref="InvalidOperationException">An attempt is active, the manager is unavailable, or this attempt key was already used.</exception>
        public UniTask<NetworkSessionResult> StartClientAsync(string sessionId, string attemptId,
            byte[] proof, CancellationToken caller = default)
        {
            EnsureCanStart(caller);
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            var identity = new NetworkSessionIdentity(sessionId, attemptId, 1, 1, SceneKey);
            return StartOwned(NetworkSessionRequest.Client(identity, proof), caller);
        }

        /// <summary>Accepts local preparation completion. Server authorization still controls the gameplay gate.</summary>
        /// <exception cref="InvalidOperationException">No unresolved local preparation callback is active.</exception>
        public void CompletePreparation()
        {
            EnsurePreparationPending();
            _preparationCompleted = true;
        }

        /// <summary>Injects a local preparation failure that enters the ordinary wrapper cleanup and recovery path.</summary>
        /// <exception cref="InvalidOperationException">No unresolved local preparation callback is active.</exception>
        public void FailPreparation()
        {
            EnsurePreparationPending();
            _preparationError = new InvalidOperationException("Sample preparation failed.");
        }

        /// <summary>Records one gameplay button press only while the sample, native connection and authorized context are running.</summary>
        /// <returns>True when accepted; false in lobby, preparation, protection, disconnect or recovery.</returns>
        public bool RecordGameplayInput()
        {
            EnsureUsable();
            if (IsProtected || !GameplayEnabled || Context == null || Context.State != NetworkSessionState.Running
                || !isActiveAndEnabled || !ConnectionHealthy())
                return false;
            GameplayInputCount++;
            return true;
        }

        /// <summary>Explicitly retries lobby restoration only after the wrapper confirms actual native and callback cleanup.</summary>
        /// <remarks>The original failure and session/attempt identity are preserved. Caller cancellation affects only this wait.</remarks>
        /// <exception cref="InvalidOperationException">No safe restoration retry is available or another owned operation is active.</exception>
        public UniTask<NetworkSessionResult> RetryLobbyRestoreAsync(CancellationToken caller = default)
        {
            EnsureUsable();
            caller.ThrowIfCancellationRequested();
            if (Context == null || _starting || _stopping || _retrying || !Context.CanRetryLobbyRestore)
                throw new InvalidOperationException("Wait for cleanup before retrying lobby restoration.");
            _retrying = true;
            _ownedRetry = RetryOwnedAsync(Context).Preserve();
            return Wait(_ownedRetry, caller);
        }

        /// <summary>Stops the owned attempt and awaits real wrapper cleanup. A normal Running close shows the sample lobby after Closed.</summary>
        /// <remarks>Returns null before the first attempt. RecoveryFault keeps protection and does not claim safe completion. The borrowed manager is never destroyed.</remarks>
        /// <exception cref="InvalidOperationException">An explicit restoration retry is already running.</exception>
        public UniTask<NetworkSessionResult> ShutdownAsync(CancellationToken caller = default)
        {
            EnsureUsable();
            caller.ThrowIfCancellationRequested();
            if (Context == null) return UniTask.FromResult<NetworkSessionResult>(null);
            if (_retrying) throw new InvalidOperationException("Lobby restoration is already running.");
            IsProtected = true;
            GameplayEnabled = false;
            if (!_stopping)
            {
                var completion = new UniTaskCompletionSource<NetworkSessionResult>();
                _ownedStop = completion.Task;
                _stopping = true;
                StopOwnedAsync(Context, completion).Forget();
            }
            return Wait(_ownedStop, caller);
        }

        /// <summary>Closes the sample gameplay gate before preparation or cleanup; performs no Unity object access.</summary>
        public UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken token)
        {
            IsProtected = true;
            GameplayEnabled = false;
            StatusText = _destroyed ? "Destroyed" : "Protected";
            return UniTask.CompletedTask;
        }

        /// <summary>Cooperatively waits for CompletePreparation or FailPreparation using the wrapper's owner cancellation token.</summary>
        public async UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken token)
        {
            PrepareCalls++;
            IsPreparing = true;
            StatusText = "Preparing";
            try
            {
                while (!_preparationCompleted && _preparationError == null)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                token.ThrowIfCancellationRequested();
                if (_preparationError != null) throw _preparationError;
                StatusText = "Waiting for all participants";
            }
            finally
            {
                IsPreparing = false;
            }
        }

        /// <summary>Opens the local sample gameplay gate only when invoked by the wrapper after server authorization.</summary>
        public void StartGameplay(NetworkSessionAttempt attempt)
        {
            if (_destroyed) throw new InvalidOperationException("The destroyed sample cannot open gameplay.");
            GameplayEnabled = true;
            IsProtected = false;
            StatusText = "Running";
        }

        /// <summary>Clears local gameplay state after preparation has actually ended, without destroying the borrowed manager.</summary>
        public UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken token)
        {
            ReleaseCalls++;
            GameplayEnabled = false;
            IsProtected = true;
            StatusText = _destroyed ? "Destroyed" : "Released";
            return UniTask.CompletedTask;
        }

        /// <summary>Restores the sample lobby view, or injects the one requested failure while retaining protection.</summary>
        /// <remarks>Cleanup callbacks use managed fields only, so owner shutdown can drain after Unity destroys this component.</remarks>
        public UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken token)
        {
            RestoreCalls++;
            GameplayEnabled = false;
            IsProtected = true;
            if (_destroyed) throw new InvalidOperationException("A destroyed sample cannot restore its lobby view.");
            token.ThrowIfCancellationRequested();
            if (_failNextRestore)
            {
                _failNextRestore = false;
                throw new InvalidOperationException("Sample lobby restoration failed once.");
            }
            IsProtected = false;
            StatusText = "Lobby";
            return UniTask.CompletedTask;
        }

        private UniTask<NetworkSessionResult> StartOwned(NetworkSessionRequest request, CancellationToken caller)
        {
            var key = Tuple.Create(request.Identity.SessionId, request.Identity.AttemptId);
            if (_attemptHistory.Contains(key)) throw new InvalidOperationException("Use a fresh sample session/attempt pair.");
            _preparationCompleted = false;
            _preparationError = null;
            _host = request.IsHost;
            _participantCount = request.Participants.Count;
            var context = new NetworkSessionContext(_manager, this, _options, request.IsHost ? new DemoVerifier() : null);
            Context = context;
            _attemptHistory.Add(key);
            _starting = true;
            _ownedStart = StartOwnedAsync(context, request).Preserve();
            return Wait(_ownedStart, caller);
        }

        private async UniTask<NetworkSessionResult> StartOwnedAsync(NetworkSessionContext context, NetworkSessionRequest request)
        {
            try
            {
                var result = await context.StartAsync(request);
                ApplyResult(context, result);
                return result;
            }
            catch
            {
                IsProtected = true;
                GameplayEnabled = false;
                StatusText = "Start failed; stop before retrying";
                throw;
            }
            finally { _starting = false; }
        }

        private async UniTask StopOwnedAsync(NetworkSessionContext context,
            UniTaskCompletionSource<NetworkSessionResult> completion)
        {
            NetworkSessionResult result = null;
            Exception failure = null;
            try
            {
                result = await context.ShutdownAsync();
                ApplyResult(context, result);
            }
            catch (Exception error)
            {
                failure = error;
            }
            finally
            {
                _stopping = false;
            }
            if (failure == null) completion.TrySetResult(result);
            else completion.TrySetException(failure);
        }

        private async UniTask<NetworkSessionResult> RetryOwnedAsync(NetworkSessionContext context)
        {
            try
            {
                var result = await context.RetryLobbyRestoreAsync();
                ApplyResult(context, result);
                return result;
            }
            finally { _retrying = false; }
        }

        private void ApplyResult(NetworkSessionContext context, NetworkSessionResult result)
        {
            if (!ReferenceEquals(Context, context)) return;
            if (_destroyed)
            {
                IsProtected = true;
                GameplayEnabled = false;
                StatusText = "Destroyed";
                return;
            }
            if (result.State == NetworkSessionState.Lobby || result.State == NetworkSessionState.Closed)
            {
                GameplayEnabled = false;
                IsProtected = false;
                StatusText = result.Failure == null ? "Lobby" : "Lobby: " + result.Failure.Reason;
            }
            else if (result.State == NetworkSessionState.RecoveryFault)
            {
                GameplayEnabled = false;
                IsProtected = true;
                StatusText = "RecoveryFault: " + result.Failure?.Reason;
            }
        }

        private void Update()
        {
            if (_destroyed || Context == null || Context.State != NetworkSessionState.Running) return;
            if (!ConnectionHealthy())
            {
                IsProtected = true;
                GameplayEnabled = false;
                StatusText = "Connection lost. Stop to return to the lobby.";
            }
        }

        private bool ConnectionHealthy()
        {
            return _manager != null && _manager.IsListening && _manager.IsConnectedClient
                && (!_host || _manager.ConnectedClientsIds.Count == _participantCount);
        }

        private void EnsureCanStart(CancellationToken caller)
        {
            EnsureUsable();
            caller.ThrowIfCancellationRequested();
            EnsureIdle();
            if (_manager == null || _options == null) throw new InvalidOperationException("Configure the sample first.");
            if (!isActiveAndEnabled) throw new InvalidOperationException("Start an active sample component.");
            ValidateManager(_manager);
        }

        private void EnsurePreparationPending()
        {
            EnsureUsable();
            if (!IsPreparing || _preparationCompleted || _preparationError != null)
                throw new InvalidOperationException("No unresolved sample preparation is active.");
        }

        private void EnsureIdle()
        {
            if (_starting || _stopping || _retrying || Context != null
                && Context.State != NetworkSessionState.Lobby && Context.State != NetworkSessionState.Closed)
                throw new InvalidOperationException("Stop or recover the current sample attempt first.");
        }

        private void EnsureUsable()
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Use Unity's main thread.");
            if (_destroyed) throw new ObjectDisposedException(nameof(NetcodeSessionSample));
        }

        private static void ValidateManager(NetworkManager manager)
        {
            if (!manager.isActiveAndEnabled || manager.transform.parent != null || manager.IsListening
                || manager.ShutdownInProgress || manager.IsClient || manager.IsServer)
                throw new InvalidOperationException("Configure an active, top-level, idle manager.");
            if (manager.NetworkConfig == null || manager.NetworkConfig.NetworkTransport == null
                || !manager.NetworkConfig.ConnectionApproval || manager.ConnectionApprovalCallback != null)
                throw new InvalidOperationException("Configure a transport and unclaimed connection approval before using the sample.");
        }

        private static UniTask<NetworkSessionResult> Wait(UniTask<NetworkSessionResult> owned, CancellationToken caller)
        {
            return caller.CanBeCanceled ? owned.AttachExternalCancellation(caller) : owned;
        }

        private void OnDestroy()
        {
            _destroyed = true;
            IsProtected = true;
            GameplayEnabled = false;
            StatusText = "Destroyed";
            if (Context != null && Context.State != NetworkSessionState.Closed && Context.State != NetworkSessionState.Lobby)
                DrainDestroyedAsync(Context).Forget(Debug.LogException);
        }

        // Destruction cannot await. This fallback requests owner shutdown; it does not promise normal recovery or manager destruction.
        private async UniTask DrainDestroyedAsync(NetworkSessionContext context)
        {
            await context.ShutdownAsync();
        }

        private sealed class DemoVerifier : IAdmissionVerifier
        {
            private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

            public AdmissionDecision Verify(AdmissionRequest request)
            {
                string proof;
                try { proof = Utf8.GetString(request.Proof.ToArray()); }
                catch (DecoderFallbackException) { return AdmissionDecision.Reject(AdmissionRejectionCode.VerifierRejected); }
                switch (proof)
                {
                    case "demo-host": return AdmissionDecision.Accept("host");
                    case "demo-c1": return AdmissionDecision.Accept("c1");
                    case "demo-c2": return AdmissionDecision.Accept("c2");
                    default: return AdmissionDecision.Reject(AdmissionRejectionCode.VerifierRejected);
                }
            }
        }
    }
}
