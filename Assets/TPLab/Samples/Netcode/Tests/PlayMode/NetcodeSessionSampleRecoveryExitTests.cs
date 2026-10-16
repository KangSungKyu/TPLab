using System;
using System.Collections;
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
    public sealed class NetcodeSessionSampleRecoveryExitTests
    {
        private GameObject _managerObject;
        private GameObject _sampleObject;
        private NetworkManager _manager;
        private NetcodeSessionSample _sample;
        private bool NativeStopped => _manager == null || (!_manager.IsListening && !_manager.ShutdownInProgress && !_manager.IsClient && !_manager.IsServer);

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("P5 recovery exit borrowed manager");
            _managerObject.SetActive(false);
            _manager = _managerObject.AddComponent<NetworkManager>();
            var transport = _managerObject.AddComponent<UnityTransport>();
            ushort port;
            using (var reservation = new UdpClient(0)) port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            _manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = false,
                AutoSpawnPlayerPrefabClientSide = false
            };
            _managerObject.SetActive(true);
            _sampleObject = new GameObject("P5 recovery exit controller");
            _sample = _sampleObject.AddComponent<NetcodeSessionSample>();
            _sample.Configure(_manager, new NetworkSessionOptions(2048, 5, 3, 2));
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                // Join the public core operation directly so a broken sample sharing adapter cannot break fixture cleanup.
                if (_sample != null && _sample.Context != null)
                {
                    var stopped = await _sample.Context.ShutdownAsync();
                    Assert.That(stopped.State == NetworkSessionState.Closed || stopped.State == NetworkSessionState.Lobby, Is.True);
                }
                await Until(() => NativeStopped, "native cleanup before fixture destruction");
                if (_sample != null) Assert.That(_sample.IsPreparing, Is.False);
                if (_sampleObject != null) Object.Destroy(_sampleObject);
                if (_managerObject != null) Object.Destroy(_managerObject);
                await UniTask.Yield();
                Assert.That(_sampleObject == null && _managerObject == null, Is.True);
            });
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator SafeExitAfterRestoreFailureKeepsProtectionAndDiagnostics() => UniTask.ToCoroutine(async () =>
        {
            var failed = await FailRestoreAsync();
            for (int repeat = 0; repeat < 2; repeat++)
            {
                var closed = await _sample.ShutdownAsync();
                Assert.That(closed.State, Is.EqualTo(NetworkSessionState.Closed));
                Assert.That(NativeStopped, Is.True);
                Assert.That(closed.Failure, Is.SameAs(failed.Failure));
                Assert.That(closed.RecoveryErrors[0], Is.SameAs(failed.RecoveryErrors[0]));
                Assert.That(_sample.RestoreCalls, Is.EqualTo(1));
                Assert.That(_sample.IsProtected, Is.True, "Safe exit must not claim the failed lobby was restored.");
                Assert.That(_sample.GameplayEnabled || _sample.RecordGameplayInput(), Is.False);
                Assert.That(_sample.StatusText, Does.StartWith("Closed without lobby restoration"));
            }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator SafelyClosedRecoveryCannotBeReconfiguredOrStartAnotherAttempt() => UniTask.ToCoroutine(async () =>
        {
            await FailRestoreAsync();
            var context = _sample.Context;
            Assert.That((await _sample.ShutdownAsync()).State, Is.EqualTo(NetworkSessionState.Closed));
            Assert.Throws<InvalidOperationException>(() => _sample.Configure(_manager, new NetworkSessionOptions(2048, 5, 3, 2)));
            Assert.Throws<InvalidOperationException>(() => _sample.StartHostAsync("recovery", "new-host", new[] { "host" }, Encoding.UTF8.GetBytes("demo-host")));
            Assert.Throws<InvalidOperationException>(() => _sample.StartClientAsync("recovery", "new-client", Encoding.UTF8.GetBytes("demo-c1")));
            Assert.Throws<InvalidOperationException>(() => _sample.FailNextRestore = false);
            Assert.That(_sample.Context, Is.SameAs(context));
            Assert.That(_sample.PrepareCalls, Is.EqualTo(1));
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            Assert.That(_sample.RestoreCalls, Is.EqualTo(1));
            Assert.That(NativeStopped, Is.True);
        });

        private async UniTask<NetworkSessionResult> FailRestoreAsync()
        {
            _sample.FailNextRestore = true;
            var start = _sample.StartHostAsync("recovery", "failed-restore", new[] { "host" }, Encoding.UTF8.GetBytes("demo-host")).Preserve();
            await Until(() => _sample.IsPreparing, "local preparation");
            _sample.FailPreparation();
            await Until(() => start.Status != UniTaskStatus.Pending, "failed restoration");
            var failed = await start;
            Assert.That(failed.State, Is.EqualTo(NetworkSessionState.RecoveryFault));
            Assert.That(_sample.Context.CanRetryLobbyRestore, Is.True);
            Assert.That(failed.RecoveryErrors.Count, Is.EqualTo(1));
            Assert.That(NativeStopped, Is.True);
            return failed;
        }

        private static async UniTask Until(Func<bool> condition, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 7;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) await UniTask.Yield();
            Assert.That(condition(), Is.True, "Condition did not complete within 7 seconds: " + label);
        }
    }
}
