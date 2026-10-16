using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Netcode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Samples.Netcode.Tests
{
    public sealed class NetcodeSessionSampleTests
    {
        private GameObject _managerObject;
        private GameObject _sampleObject;
        private NetworkManager _manager;
        private NetcodeSessionSample _sample;
        private readonly List<NetworkSessionContext> _capturedContexts = new List<NetworkSessionContext>();
        private static byte[] HostProof => Encoding.UTF8.GetBytes("demo-host");
        private static NetworkSessionOptions Options() => new NetworkSessionOptions(2048, 5, 3, 2);
        private bool NativeStopped => _manager == null || (!_manager.IsListening && !_manager.ShutdownInProgress && !_manager.IsClient && !_manager.IsServer);

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("P5 sample test borrowed manager");
            _managerObject.SetActive(false);
            _manager = _managerObject.AddComponent<NetworkManager>();
            var transport = _managerObject.AddComponent<UnityTransport>();
            ushort port;
            using (var reservation = new UdpClient(0)) port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            _manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false,
                AutoSpawnPlayerPrefabClientSide = false
            };
            _managerObject.SetActive(true);
            _sampleObject = new GameObject("P5 sample test controller");
            _sample = _sampleObject.AddComponent<NetcodeSessionSample>();
            _sample.Configure(_manager, Options());
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                if (_sample != null && _sample.Context != null)
                {
                    var stopped = await _sample.ShutdownAsync();
                    Assert.That(stopped.State == NetworkSessionState.Closed || stopped.State == NetworkSessionState.Lobby, Is.True);
                }
                foreach (var context in _capturedContexts)
                {
                    var stopped = await context.ShutdownAsync();
                    Assert.That(stopped.State == NetworkSessionState.Closed || stopped.State == NetworkSessionState.Lobby, Is.True);
                }
                await Until(() => NativeStopped, "actual native stop before destroying owned fixture objects");
                if (_sample != null) Assert.That(_sample.IsPreparing, Is.False, "Preparation callback did not drain.");
                if (_sampleObject != null) Object.Destroy(_sampleObject);
                if (_managerObject != null) Object.Destroy(_managerObject);
                await UniTask.Yield();
                Assert.That(_sampleObject == null && _managerObject == null, Is.True);
                _capturedContexts.Clear();
            });
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ConfigureBorrowsAnIdleManagerWithoutStartingGameplay() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(_sample.Context, Is.Null);
            Assert.That(_sample.LastResult, Is.Null);
            Assert.That(_sample.StatusText, Is.EqualTo("Lobby"));
            Assert.That(_sample.IsProtected || _sample.IsPreparing || _sample.GameplayEnabled, Is.False);
            Assert.That(_sample.RecordGameplayInput(), Is.False);
            Assert.That(_sample.PrepareCalls + _sample.ReleaseCalls + _sample.RestoreCalls + _sample.GameplayInputCount, Is.Zero);
            Assert.That(NativeStopped, Is.True);
            Assert.That(_manager.ConnectionApprovalCallback, Is.Null);
            Assert.That(_manager.NetworkConfig.EnableSceneManagement, Is.False);
            await UniTask.Yield();
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator LocalPreparationGateBlocksInputUntilServerAuthorization() => UniTask.ToCoroutine(async () =>
        {
            var started = Start("gate");
            await Until(() => _sample.IsPreparing, "local preparation gate");
            Assert.That(started.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_sample.IsProtected, Is.True);
            Assert.That(_sample.GameplayEnabled, Is.False);
            Assert.That(_sample.RecordGameplayInput(), Is.False);
            _sample.CompletePreparation();
            var running = await started;
            Assert.That(running.Started, Is.True);
            Assert.That(_sample.Context.State, Is.EqualTo(NetworkSessionState.Running));
            Assert.That(_sample.IsProtected || _sample.IsPreparing, Is.False);
            Assert.That(_sample.GameplayEnabled, Is.True);
            Assert.That(_sample.RecordGameplayInput(), Is.True);
            Assert.That(_sample.GameplayInputCount, Is.EqualTo(1));
            var closed = await _sample.ShutdownAsync();
            Assert.That(closed.State, Is.EqualTo(NetworkSessionState.Closed));
            Assert.That(_sample.StatusText, Does.StartWith("Lobby"));
            Assert.That(_sample.GameplayEnabled || _sample.IsProtected, Is.False);
            Assert.That(_sample.RecordGameplayInput(), Is.False);
            Assert.That(_sample.PrepareCalls, Is.EqualTo(1));
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.Zero, "Running close must not claim wrapper automatic restoration.");
            Assert.That(_manager != null && NativeStopped, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator PreparationFailureRestoresLobbyAndKeepsGameplayClosed() => UniTask.ToCoroutine(async () =>
        {
            var started = Start("prepare-failure");
            await Until(() => _sample.IsPreparing, "preparation gate");
            _sample.FailPreparation();
            var failed = await started;
            Assert.That(failed.State, Is.EqualTo(NetworkSessionState.Lobby));
            Assert.That(failed.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.LocalPreparationFailed));
            Assert.That(failed.Failure.Cause, Is.TypeOf<InvalidOperationException>());
            Assert.That(_sample.LastResult, Is.SameAs(failed));
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.EqualTo(1));
            Assert.That(_sample.IsPreparing || _sample.GameplayEnabled || _sample.IsProtected, Is.False);
            Assert.That(_sample.StatusText, Does.Contain("LocalPreparationFailed"));
            Assert.That(_sample.RecordGameplayInput(), Is.False);
            Assert.That(NativeStopped, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator FailedRestoreKeepsProtectionUntilExplicitRetry() => UniTask.ToCoroutine(async () =>
        {
            _sample.FailNextRestore = true;
            var started = Start("restore-failure");
            await Until(() => _sample.IsPreparing, "preparation gate");
            _sample.FailPreparation();
            var first = await started;
            Assert.That(first.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(_sample.Context.CanRetryLobbyRestore, Is.True);
            Assert.That(_sample.IsProtected, Is.True);
            Assert.That(_sample.GameplayEnabled, Is.False);
            Assert.That(_sample.RecordGameplayInput(), Is.False);
            Assert.That(first.RecoveryErrors.Count, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.EqualTo(1));
            var retried = await _sample.RetryLobbyRestoreAsync();
            Assert.That(retried.State, Is.EqualTo(NetworkSessionState.Lobby));
            Assert.That(retried.Failure, Is.SameAs(first.Failure));
            Assert.That(_sample.LastResult, Is.SameAs(retried));
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.EqualTo(2));
            Assert.That(_sample.IsProtected || _sample.GameplayEnabled, Is.False);
            Assert.That(NativeStopped, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator OwnerShutdownCooperativelyDrainsPendingPreparation() => UniTask.ToCoroutine(async () =>
        {
            var started = Start("owner-cancel");
            await Until(() => _sample.IsPreparing, "preparation gate");
            var stopping = _sample.ShutdownAsync().Preserve();
            var result = await started;
            Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.OwnerCancelled));
            Assert.That(await stopping, Is.SameAs(result));
            Assert.That(result.State, Is.EqualTo(NetworkSessionState.Lobby));
            Assert.That(_sample.IsPreparing || _sample.GameplayEnabled, Is.False);
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.EqualTo(1));
            Assert.That(NativeStopped, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ANewAttemptUsesANewContextAndCannotReuseAnOldAttemptKey() => UniTask.ToCoroutine(async () =>
        {
            var firstTask = Start("first");
            await Until(() => _sample.IsPreparing, "first preparation");
            var firstContext = _sample.Context;
            _sample.CompletePreparation();
            Assert.That((await firstTask).Started, Is.True);
            Assert.That((await _sample.ShutdownAsync()).State, Is.EqualTo(NetworkSessionState.Closed));
            var nextTask = Start("second");
            await Until(() => _sample.IsPreparing, "second preparation");
            Assert.That(_sample.Context, Is.Not.SameAs(firstContext));
            Assert.That(firstContext.State, Is.EqualTo(NetworkSessionState.Closed));
            _sample.CompletePreparation();
            Assert.That((await nextTask).Started, Is.True);
            await _sample.ShutdownAsync();
            Assert.Throws<InvalidOperationException>(() => _sample.StartHostAsync("session", "first", new[] { "host" }, HostProof));
            Assert.That(_sample.PrepareCalls, Is.EqualTo(2));
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(2));
            Assert.That(NativeStopped, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator InvalidOrConcurrentCommandsDoNotReplaceTheOwnedAttempt() => UniTask.ToCoroutine(async () =>
        {
            Assert.Throws<ArgumentNullException>(() => _sample.Configure(null, Options()));
            Assert.Throws<ArgumentNullException>(() => _sample.Configure(_manager, null));
            Assert.Throws<InvalidOperationException>(() => _sample.CompletePreparation());
            Assert.Throws<InvalidOperationException>(() => _sample.FailPreparation());
            Assert.Throws<ArgumentException>(() => _sample.StartHostAsync(" ", "invalid", new[] { "host" }, HostProof));
            Assert.Throws<ArgumentException>(() => _sample.StartHostAsync("session", "invalid", new[] { "outside" }, HostProof));
            var started = Start("active");
            await Until(() => _sample.IsPreparing, "owned preparation");
            var context = _sample.Context;
            Assert.Throws<InvalidOperationException>(() => _sample.Configure(_manager, Options()));
            Assert.Throws<InvalidOperationException>(() => _sample.StartHostAsync("session", "other", new[] { "host" }, HostProof));
            Assert.Throws<InvalidOperationException>(() => _sample.FailNextRestore = true);
            Assert.That(_sample.Context, Is.SameAs(context));
            _sample.CompletePreparation();
            Assert.That((await started).Started, Is.True);
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator CallerCancellationOnlyStopsThatWait() => UniTask.ToCoroutine(async () =>
        {
            using (var caller = new CancellationTokenSource())
            {
                var wait = _sample.StartHostAsync("session", "caller-cancel", new[] { "host" }, HostProof, caller.Token).Preserve();
                await Until(() => _sample.IsPreparing, "preparation before caller cancellation");
                caller.Cancel();
                Exception observed = null;
                try { await wait; } catch (Exception error) { observed = error; }
                Assert.That(observed, Is.InstanceOf<OperationCanceledException>());
                Assert.That(_sample.Context.State, Is.EqualTo(NetworkSessionState.Preparing));
                Assert.That(_sample.IsPreparing, Is.True);
                Assert.That(_sample.RecordGameplayInput(), Is.False);
                _sample.CompletePreparation();
                await Until(() => _sample.Context.State == NetworkSessionState.Running, "owned attempt continues after caller cancellation");
                Assert.That(_sample.LastResult.Started && _sample.GameplayEnabled, Is.True);
                Assert.That(_sample.RecordGameplayInput(), Is.True);
            }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator InvalidClientAndHostInputsLeaveTheBorrowedManagerIdle() => UniTask.ToCoroutine(async () =>
        {
            Assert.Throws<ArgumentException>(() => _sample.StartClientAsync(null, "attempt", Encoding.UTF8.GetBytes("demo-c1")));
            Assert.Throws<ArgumentException>(() => _sample.StartClientAsync("session", " ", Encoding.UTF8.GetBytes("demo-c1")));
            Assert.Throws<ArgumentNullException>(() => _sample.StartClientAsync("session", "attempt", null));
            Assert.Throws<ArgumentNullException>(() => _sample.StartHostAsync("session", "attempt", null, HostProof));
            Assert.Throws<ArgumentException>(() => _sample.StartHostAsync("session", "attempt", Array.Empty<string>(), HostProof));
            Assert.Throws<ArgumentException>(() => _sample.StartHostAsync("session", "attempt", new[] { "host", "host" }, HostProof));
            Assert.Throws<InvalidOperationException>(() => _sample.RetryLobbyRestoreAsync());
            Assert.That(_sample.Context, Is.Null);
            Assert.That(_sample.PrepareCalls + _sample.ReleaseCalls + _sample.RestoreCalls, Is.Zero);
            Assert.That(NativeStopped, Is.True);
            await UniTask.Yield();
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator DestroyFallbackRequestsOwnerShutdownAndDoesNotDestroyTheBorrowedManager() => UniTask.ToCoroutine(async () =>
        {
            var started = Start("destroy-fallback");
            await Until(() => _sample.IsPreparing, "preparation before destruction");
            var context = _sample.Context;
            _capturedContexts.Add(context);
            Object.Destroy(_sampleObject);
            await UniTask.Yield();
            await Until(() => NativeStopped && started.Status != UniTaskStatus.Pending, "fallback owner shutdown drains callback and native endpoint");
            var result = await started;
            Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.OwnerCancelled));
            Assert.That(_manager != null, Is.True, "The sample borrows the manager and must never destroy it.");
            var stopped = await context.ShutdownAsync();
            Assert.That(stopped.State, Is.EqualTo(NetworkSessionState.Closed).Or.EqualTo(NetworkSessionState.Lobby));
            Assert.That(NativeStopped, Is.True);
        });

        private UniTask<NetworkSessionResult> Start(string attempt)
            => _sample.StartHostAsync("session", attempt, new[] { "host" }, HostProof).Preserve();

        private static async UniTask Until(Func<bool> condition, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 7;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) await UniTask.Yield();
            Assert.That(condition(), Is.True, "Condition did not complete within 7 seconds: " + label);
        }
    }
}
