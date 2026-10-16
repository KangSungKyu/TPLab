using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Netcode.Tests
{
    public sealed class NetworkSessionContextTests
    {
        private readonly List<NetworkSessionContext> _contexts = new List<NetworkSessionContext>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<Hooks> _hooks = new List<Hooks>();
        private NetworkManager _manager;
        private Hooks _hook;
        private NetworkSessionContext _context;
        private readonly NetworkSessionIdentity _identity = new NetworkSessionIdentity("session", "attempt", 7, 2, "fixed");

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("P3 context test");
            go.SetActive(false);
            _objects.Add(go);
            _manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            ushort port;
            using (var reservation = new UdpClient(0)) port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            _manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false };
            go.SetActive(true);
            _hook = new Hooks(_manager);
            _hooks.Add(_hook);
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                foreach (var hook in _hooks) hook.OpenAll();
                foreach (var context in _contexts) await context.ShutdownAsync();
                foreach (var go in _objects)
                {
                    if (go == null) continue;
                    var manager = go.GetComponent<NetworkManager>();
                    if (manager != null && (manager.IsListening || manager.IsClient || manager.IsServer)) manager.Shutdown();
                    await Until(() => manager == null || (!manager.IsListening && !manager.ShutdownInProgress && !manager.IsClient && !manager.IsServer));
                    Object.Destroy(go);
                }
                await UniTask.Yield();
                _contexts.Clear(); _objects.Clear(); _hooks.Clear();
            });
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ProtectionAndPreparationAreAwaitedBeforeGameplay() => UniTask.ToCoroutine(async () =>
        {
            Create(); _hook.ProtectGate = new UniTaskCompletionSource(); _hook.PrepareGate = new UniTaskCompletionSource();
            var task = _context.StartAsync(Request()).Preserve();
            await Until(() => _hook.ProtectCalls == 1);
            Assert.That(_manager.IsListening, Is.False);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Pending));
            _hook.ProtectGate.TrySetResult();
            await Until(() => _hook.PrepareCalls == 1);
            Assert.That(_manager.IsListening, Is.True); Assert.That(_hook.GameplayCalls, Is.Zero);
            Assert.That(_context.State, Is.EqualTo(NetworkSessionState.Preparing));
            _hook.PrepareGate.TrySetResult();
            var result = await task;
            Assert.That(result.Started, Is.True); Assert.That(_hook.GameplayCalls, Is.EqualTo(1));
            var stopped = await _context.ShutdownAsync();
            Assert.That(stopped.State, Is.EqualTo(NetworkSessionState.Closed));
            Assert.That(_hook.ReleaseCalls, Is.EqualTo(1)); Assert.That(_hook.RestoreCalls, Is.Zero);
            Assert.That(_manager.IsListening, Is.False); Assert.That(_manager, Is.Not.Null);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator CallerCancellationDoesNotCancelTheOwnedAttempt() => UniTask.ToCoroutine(async () =>
        {
            Create(); _hook.PrepareGate = new UniTaskCompletionSource();
            using (var caller = new CancellationTokenSource())
            {
                var wait = _context.StartAsync(Request(), caller.Token).Preserve();
                await Until(() => _hook.PrepareCalls == 1); caller.Cancel();
                Exception error = null; try { await wait; } catch (Exception ex) { error = ex; }
                Assert.That(error, Is.InstanceOf<OperationCanceledException>());
                Assert.That(_context.State, Is.EqualTo(NetworkSessionState.Preparing)); Assert.That(_manager.IsListening, Is.True);
                _hook.PrepareGate.TrySetResult(); await Until(() => _context.LastResult != null);
                Assert.That(_context.LastResult.Started, Is.True); Assert.That(_hook.GameplayCalls, Is.EqualTo(1));
            }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator PreparationFailureKeepsCauseAndRestoresOnlyAfterCleanup() => UniTask.ToCoroutine(async () =>
        {
            Create(); var cause = new InvalidOperationException("project preparation"); _hook.PrepareError = cause;
            var result = await _context.StartAsync(Request());
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.Lobby));
            Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.LocalPreparationFailed));
            Assert.That(result.Failure.Cause, Is.SameAs(cause)); Assert.That(result.RecoveryErrors, Is.Empty);
            Assert.That(_hook.Trace, Is.EqualTo(new[] { "protect", "prepare", "protect", "release", "restore" }));
            Assert.That(_hook.RestoreObservedStopped, Is.True); Assert.That(_hook.GameplayCalls, Is.Zero);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ReleaseFailureCannotOverwritePreparationCause() => UniTask.ToCoroutine(async () =>
        {
            Create(); var first = new Exception("first"); var release = new Exception("release");
            _hook.PrepareError = first; _hook.ReleaseError = release;
            var result = await _context.StartAsync(Request());
            Assert.That(result.Failure.Cause, Is.SameAs(first));
            Assert.That(result.RecoveryErrors, Does.Contain(release));
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(_hook.RestoreCalls, Is.Zero); Assert.That(_manager.IsListening, Is.False);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ExplicitRestoreRetryKeepsOriginalFailureAndAttemptKey() => UniTask.ToCoroutine(async () =>
        {
            Create(); _hook.PrepareError = new Exception("prepare"); var restore = new Exception("restore"); _hook.RestoreError = restore;
            var failed = await _context.StartAsync(Request());
            Assert.That(failed.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(failed.RecoveryErrors, Does.Contain(restore)); Assert.That(_context.CanRetryLobbyRestore, Is.True);
            var originalAttempt = _hook.LastRecovery.Attempt; _hook.RestoreError = null;
            var retried = await _context.RetryLobbyRestoreAsync();
            Assert.That(retried.State, Is.EqualTo(NetworkSessionState.Lobby)); Assert.That(retried.Failure, Is.SameAs(failed.Failure));
            Assert.That(_hook.LastRecovery.Attempt, Is.SameAs(originalAttempt)); Assert.That(_hook.RestoreCalls, Is.EqualTo(2));
            Assert.That(_hook.GameplayCalls, Is.Zero);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator NonCooperativePreparationCannotRestoreOrReviveGameplay() => UniTask.ToCoroutine(async () =>
        {
            Create(new NetworkSessionOptions(2048, 0.15, 0.15, 2));
            _hook.PrepareGate = new UniTaskCompletionSource(); _hook.IgnorePrepareCancellation = true;
            try
            {
                var result = await _context.StartAsync(Request());
                Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
                Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.DeadlineExceeded));
                Assert.That(_context.CanRetryLobbyRestore, Is.False); Assert.That(_hook.ReleaseCalls, Is.Zero); Assert.That(_hook.RestoreCalls, Is.Zero);
                Assert.Throws<InvalidOperationException>(() => _context.RetryLobbyRestoreAsync());
                _hook.PrepareGate.TrySetResult(); await Until(() => _context.CanRetryLobbyRestore);
                Assert.That(_hook.GameplayCalls, Is.Zero); Assert.That(_hook.ReleaseCalls, Is.EqualTo(1));
                var restored = await _context.RetryLobbyRestoreAsync();
                Assert.That(restored.State, Is.EqualTo(NetworkSessionState.Lobby)); Assert.That(restored.Failure, Is.SameAs(result.Failure));
            }
            finally { _hook.OpenAll(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator NonCooperativeRestoreTimeoutRequiresExplicitRetryAfterCompletion() => UniTask.ToCoroutine(async () =>
        {
            Create(new NetworkSessionOptions(2048, 5, 3, 0.15));
            _hook.PrepareError = new Exception("prepare"); _hook.RestoreGate = new UniTaskCompletionSource();
            try
            {
                var result = await _context.StartAsync(Request());
                Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault)); Assert.That(_context.CanRetryLobbyRestore, Is.False);
                Assert.That(result.RecoveryErrors.Count, Is.EqualTo(1)); Assert.That(result.RecoveryErrors[0], Is.TypeOf<TimeoutException>());
                Assert.Throws<InvalidOperationException>(() => _context.RetryLobbyRestoreAsync());
                _hook.RestoreGate.TrySetResult(); await Until(() => _context.CanRetryLobbyRestore);
                Assert.That(_context.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
                var retried = await _context.RetryLobbyRestoreAsync();
                Assert.That(retried.State, Is.EqualTo(NetworkSessionState.Lobby)); Assert.That(_hook.RestoreCalls, Is.EqualTo(2));
            }
            finally { _hook.OpenAll(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator OwnerShutdownCancelsPreparationAndSharesCleanup() => UniTask.ToCoroutine(async () =>
        {
            Create(); _hook.PrepareGate = new UniTaskCompletionSource();
            var started = _context.StartAsync(Request()).Preserve(); await Until(() => _hook.PrepareCalls == 1);
            var stop1 = _context.ShutdownAsync().Preserve(); var stop2 = _context.ShutdownAsync().Preserve();
            var result = await started;
            Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.OwnerCancelled));
            Assert.That((await stop1).State, Is.EqualTo(NetworkSessionState.Lobby)); Assert.That(await stop2, Is.SameAs(await stop1));
            Assert.That(_hook.ReleaseCalls, Is.EqualTo(1)); Assert.That(_hook.RestoreCalls, Is.EqualTo(1)); Assert.That(_hook.GameplayCalls, Is.Zero);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator GameplayGateFailureDoesNotClaimAutomaticLobbyRecovery() => UniTask.ToCoroutine(async () =>
        {
            Create(); var error = new Exception("gate"); _hook.GameplayError = error;
            var result = await _context.StartAsync(Request());
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault)); Assert.That(result.Failure.Cause, Is.SameAs(error));
            Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.GameplayGateFailed));
            Assert.That(_hook.ProtectCalls, Is.EqualTo(2)); Assert.That(_hook.RestoreCalls, Is.Zero); Assert.That(_hook.ReleaseCalls, Is.EqualTo(1));
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ContextLeasePreventsTwoOwnersAndIsReturnedAfterShutdown() => UniTask.ToCoroutine(async () =>
        {
            Create(); var first = await _context.StartAsync(Request()); Assert.That(first.Started, Is.True);
            var other = new NetworkSessionContext(_manager, _hook, Options(), new Verifier()); _contexts.Add(other);
            Assert.Throws<InvalidOperationException>(() => other.StartAsync(Request()));
            Assert.That(_manager.IsListening, Is.True);
            await _context.ShutdownAsync();
            var next = await other.StartAsync(Request("next")); Assert.That(next.Started, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator WorkerStartIsRejectedBeforeNativeEffectsAndContextRemainsUsable() => UniTask.ToCoroutine(async () =>
        {
            Create(); var request = Request(); Exception observed = null;
            var worker = new Thread(() => { try { _context.StartAsync(request); } catch (Exception ex) { observed = ex; } }) { IsBackground = true };
            worker.Start(); Assert.That(worker.Join(2000), Is.True, "Worker guard did not finish within 2 seconds.");
            Assert.That(observed, Is.TypeOf<InvalidOperationException>()); Assert.That(_manager.IsListening, Is.False);
            Assert.That(_hook.ProtectCalls, Is.Zero); Assert.That(_context.State, Is.EqualTo(NetworkSessionState.Created));
            Assert.That((await _context.StartAsync(request)).Started, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ProtectionFailureStillShutsDownAndNeverStartsNative() => UniTask.ToCoroutine(async () =>
        {
            Create(); var error = new Exception("cover"); _hook.ProtectError = error;
            var result = await _context.StartAsync(Request());
            Assert.That(result.Failure.Cause, Is.SameAs(error)); Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.ProtectionFailed));
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(_manager.IsListening, Is.False); Assert.That(_hook.PrepareCalls, Is.Zero); Assert.That(_hook.GameplayCalls, Is.Zero);
        });

        private void Create(NetworkSessionOptions options = null)
        {
            _context = new NetworkSessionContext(_manager, _hook, options ?? Options(), new Verifier()); _contexts.Add(_context);
        }
        private NetworkSessionRequest Request(string attempt = "attempt") => NetworkSessionRequest.Host(
            attempt == "attempt" ? _identity : new NetworkSessionIdentity("session", attempt, 7, 2, "fixed"), new[] { "host" }, new byte[] { 1 }, LoadSceneMode.Additive);
        private static NetworkSessionOptions Options() => new NetworkSessionOptions(2048, 5, 3, 2);
        private static async UniTask Until(Func<bool> condition)
        {
            double until = Time.realtimeSinceStartupAsDouble + 5;
            while (!condition() && Time.realtimeSinceStartupAsDouble < until) await UniTask.Yield();
            Assert.That(condition(), Is.True, "Condition was not reached within 5 seconds.");
        }
        private sealed class Verifier : IAdmissionVerifier
        {
            public AdmissionDecision Verify(AdmissionRequest request) => AdmissionDecision.Accept("host");
        }
        private sealed class Hooks : INetworkSessionCallbacks
        {
            private readonly NetworkManager _manager;
            internal readonly List<string> Trace = new List<string>();
            internal UniTaskCompletionSource ProtectGate, PrepareGate, RestoreGate;
            internal Exception ProtectError, PrepareError, ReleaseError, RestoreError, GameplayError;
            internal bool IgnorePrepareCancellation, RestoreObservedStopped;
            internal int ProtectCalls, PrepareCalls, GameplayCalls, ReleaseCalls, RestoreCalls;
            internal NetworkSessionRecovery LastRecovery;
            internal Hooks(NetworkManager manager) { _manager = manager; }
            internal void OpenAll() { ProtectGate?.TrySetResult(); PrepareGate?.TrySetResult(); RestoreGate?.TrySetResult(); }
            public async UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken token)
            {
                Trace.Add("protect"); ProtectCalls++;
                if (ProtectGate != null) await ProtectGate.Task.AttachExternalCancellation(token);
                if (ProtectError != null) throw ProtectError;
            }
            public async UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken token)
            {
                Trace.Add("prepare"); PrepareCalls++;
                if (PrepareGate != null)
                {
                    if (IgnorePrepareCancellation) await PrepareGate.Task;
                    else await PrepareGate.Task.AttachExternalCancellation(token);
                }
                if (PrepareError != null) throw PrepareError;
            }
            public void StartGameplay(NetworkSessionAttempt attempt) { Trace.Add("gameplay"); GameplayCalls++; if (GameplayError != null) throw GameplayError; }
            public UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken token)
            { Trace.Add("release"); ReleaseCalls++; if (ReleaseError != null) throw ReleaseError; return UniTask.CompletedTask; }
            public async UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken token)
            {
                Trace.Add("restore"); RestoreCalls++; LastRecovery = recovery;
                RestoreObservedStopped = !_manager.IsListening && !_manager.IsServer && !_manager.IsClient && !_manager.ShutdownInProgress && ReleaseCalls == 1;
                if (RestoreGate != null) await RestoreGate.Task;
                if (RestoreError != null) throw RestoreError;
            }
        }
    }
}
