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
    public class SessionRecoveryExitTests
    {
        private readonly List<NetworkSessionContext> _contexts = new List<NetworkSessionContext>();
        private readonly List<Hooks> _hooks = new List<Hooks>();
        private GameObject _object;
        private NetworkManager _manager;
        private Hooks _hook;
        private NetworkSessionContext _context;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("P3 recovery exit test");
            _object.SetActive(false);
            _manager = _object.AddComponent<NetworkManager>();
            var transport = _object.AddComponent<UnityTransport>();
            ushort port;
            using (var reservation = new UdpClient(0))
                port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            _manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = false
            };
            _object.SetActive(true);
            _hook = new Hooks
            {
                PreparationError = new InvalidOperationException("prepare before restore"),
                RestoreGate = new UniTaskCompletionSource()
            };
            _context = Create(_hook);
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                foreach (var hook in _hooks)
                {
                    hook.RestoreGate?.TrySetResult();
                    hook.SecondProtectionGate?.TrySetResult();
                }
                foreach (var hook in _hooks)
                    await Until(() => !hook.RestoreEntered || hook.RestoreCompleted);
                foreach (var context in _contexts) await context.ShutdownAsync();
                if (_manager != null)
                {
                    if (_manager.IsListening || _manager.IsClient || _manager.IsServer) _manager.Shutdown();
                    await Until(() => !_manager.IsListening && !_manager.ShutdownInProgress
                        && !_manager.IsClient && !_manager.IsServer);
                }
                Object.Destroy(_object);
                await UniTask.Yield();
                _contexts.Clear();
                _hooks.Clear();
            });
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownCannotClaimClosedWhileAnOldRestoreHookIsPending()
            => UniTask.ToCoroutine(async () =>
        {
            try
            {
                var first = await _context.StartAsync(Request("first"));
                Assert.That(first.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
                Assert.That(_hook.RestoreEntered, Is.True);
                Assert.That(_hook.RestoreCompleted, Is.False);
                Assert.That(_context.CanRetryLobbyRestore, Is.False);
                var pending = await _context.ShutdownAsync();
                Assert.That(pending.State, Is.EqualTo(NetworkSessionState.RecoveryFault),
                    "Closed must not hide an in-flight restore hook.");
                Assert.That(_hook.RestoreCompleted, Is.False);
                Assert.That(pending.Failure, Is.SameAs(first.Failure));
                _hook.RestoreGate.TrySetResult();
                await Until(() => _hook.RestoreCompleted && _context.CanRetryLobbyRestore);
                var closed = await _context.ShutdownAsync();
                Assert.That(closed.State, Is.EqualTo(NetworkSessionState.Closed));
                Assert.That(closed.Failure, Is.SameAs(first.Failure));
                Assert.That(_hook.RestoreCalls, Is.EqualTo(1));
            }
            finally { _hook.RestoreGate.TrySetResult(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator AManagerCannotBeLeasedAgainUntilPendingRecoveryHasSafelyClosed()
            => UniTask.ToCoroutine(async () =>
        {
            var otherHook = new Hooks();
            var other = Create(otherHook);
            try
            {
                var failed = await _context.StartAsync(Request("first"));
                Assert.That(failed.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
                Assert.That(_hook.RestoreCompleted, Is.False);
                Assert.That(_manager.IsListening, Is.False);
                Assert.Throws<InvalidOperationException>(() => other.StartAsync(Request("second")),
                    "An old pending recovery still owns this manager lease.");
                Assert.That(other.State, Is.EqualTo(NetworkSessionState.Created));
                Assert.That(otherHook.ProtectCalls, Is.Zero);
                _hook.RestoreGate.TrySetResult();
                await Until(() => _hook.RestoreCompleted && _context.CanRetryLobbyRestore);
                Assert.That((await _context.ShutdownAsync()).State, Is.EqualTo(NetworkSessionState.Closed));
                var next = await other.StartAsync(Request("second"));
                Assert.That(next.Started, Is.True);
                Assert.That(otherHook.GameplayCalls, Is.EqualTo(1));
            }
            finally { _hook.RestoreGate.TrySetResult(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator SynchronousRestoreOverBudgetCannotReportLobbySuccess()
            => UniTask.ToCoroutine(async () =>
        {
            _hook.RestoreGate = null;
            _hook.RestoreDelayMilliseconds = 100;
            _context = Create(_hook, new NetworkSessionOptions(2048, 5, 3, 0.01));
            var result = await _context.StartAsync(Request("slow-restore"));
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(result.RecoveryErrors, Has.Some.TypeOf<TimeoutException>());
            Assert.That(_hook.RestoreCompleted, Is.True);
            Assert.That(result.Failure.Cause, Is.SameAs(_hook.PreparationError));
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator SynchronousReleaseOverBudgetMustRecordCleanupTimeout()
            => UniTask.ToCoroutine(async () =>
        {
            _hook.RestoreGate = null;
            _hook.FirstProtectionError = new InvalidOperationException("protect before native start");
            _hook.ReleaseDelayMilliseconds = 100;
            _context = Create(_hook, new NetworkSessionOptions(2048, 5, 0.01, 3));
            var result = await _context.StartAsync(Request("slow-release"));
            Assert.That(result.Failure.Cause, Is.SameAs(_hook.FirstProtectionError));
            Assert.That(_hook.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_manager.IsListening, Is.False);
            Assert.That(result.RecoveryErrors, Has.Some.TypeOf<TimeoutException>(),
                "A synchronous release that exceeds the budget must not be reported as timely cleanup.");
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
        });
        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownProtectsGameplayBeforeNativeShutdownOrRelease()
            => UniTask.ToCoroutine(async () =>
        {
            _hook.PreparationError = null;
            _hook.RestoreGate = null;
            _hook.SecondProtectionGate = new UniTaskCompletionSource();
            _context = Create(_hook, new NetworkSessionOptions(2048, 5, 3, 2));
            try
            {
                Assert.That((await _context.StartAsync(Request("protect-before-stop"))).Started, Is.True);
                var stopped = _context.ShutdownAsync().Preserve();
                await Until(() => _hook.ProtectCalls == 2);
                await UniTask.Yield();
                await UniTask.Yield();
                Assert.That(stopped.Status, Is.EqualTo(UniTaskStatus.Pending));
                Assert.That(_manager.IsListening, Is.True,
                    "Native shutdown must wait for the project's protection gate.");
                Assert.That(_hook.ReleaseCalls, Is.Zero);
                _hook.SecondProtectionGate.TrySetResult();
                Assert.That((await stopped).State, Is.EqualTo(NetworkSessionState.Closed));
                Assert.That(_hook.ReleaseCalls, Is.EqualTo(1));
                Assert.That(_manager.IsListening, Is.False);
                Assert.That(_manager.ShutdownInProgress, Is.False);
                Assert.That(_manager.IsClient || _manager.IsServer, Is.False);
            }
            finally { _hook.SecondProtectionGate.TrySetResult(); }
        });
        private NetworkSessionContext Create(Hooks hook, NetworkSessionOptions options = null)
        {
            if (!_hooks.Contains(hook)) _hooks.Add(hook);
            var context = new NetworkSessionContext(_manager, hook,
                options ?? new NetworkSessionOptions(2048, 5, 0.15, 0.15), new Verifier());
            _contexts.Add(context);
            return context;
        }

        private static NetworkSessionRequest Request(string attempt)
        {
            return NetworkSessionRequest.Host(new NetworkSessionIdentity("session", attempt, 7, 2, "fixed"),
                new[] { "host" }, new byte[] { 1 }, LoadSceneMode.Additive);
        }

        private static async UniTask Until(Func<bool> condition)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) await UniTask.Yield();
            Assert.That(condition(), Is.True, "Condition did not complete within 5 seconds.");
        }

        private sealed class Verifier : IAdmissionVerifier
        {
            public AdmissionDecision Verify(AdmissionRequest request) => AdmissionDecision.Accept("host");
        }

        private sealed class Hooks : INetworkSessionCallbacks
        {
            internal Exception PreparationError;
            internal Exception FirstProtectionError;
            internal int ReleaseDelayMilliseconds, RestoreDelayMilliseconds, ReleaseCalls;
            internal UniTaskCompletionSource RestoreGate, SecondProtectionGate;
            internal bool RestoreEntered;
            internal bool RestoreCompleted;
            internal int RestoreCalls;
            internal int ProtectCalls;
            internal int GameplayCalls;
            public UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken token)
            {
                ProtectCalls++;
                if (ProtectCalls == 1 && FirstProtectionError != null) throw FirstProtectionError;
                return ProtectCalls == 2 && SecondProtectionGate != null
                    ? SecondProtectionGate.Task.AttachExternalCancellation(token) : UniTask.CompletedTask;
            }
            public UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken token)
            {
                if (PreparationError != null) throw PreparationError;
                return UniTask.CompletedTask;
            }
            public void StartGameplay(NetworkSessionAttempt attempt) { GameplayCalls++; }
            public UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken token)
            {
                ReleaseCalls++;
                // Elapsed time is the tested contract; blocking cannot be preempted, but overrun must be detected.
                if (ReleaseDelayMilliseconds > 0) Thread.Sleep(ReleaseDelayMilliseconds);
                return UniTask.CompletedTask;
            }
            public async UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken token)
            {
                RestoreCalls++;
                RestoreEntered = true;
                if (RestoreDelayMilliseconds > 0) Thread.Sleep(RestoreDelayMilliseconds);
                if (RestoreGate != null) await RestoreGate.Task;
                RestoreCompleted = true;
            }
        }
    }
}
