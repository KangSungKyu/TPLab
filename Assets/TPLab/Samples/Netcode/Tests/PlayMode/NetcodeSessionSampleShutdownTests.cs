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
    public sealed class NetcodeSessionSampleShutdownTests
    {
        private GameObject _managerObject;
        private GameObject _sampleObject;
        private NetworkManager _manager;
        private NetcodeSessionSample _sample;
        private bool NativeStopped => _manager == null || (!_manager.IsListening && !_manager.ShutdownInProgress && !_manager.IsClient && !_manager.IsServer);

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("P5 shared shutdown borrowed manager");
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
            _sampleObject = new GameObject("P5 shared shutdown controller");
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
        public IEnumerator ConcurrentPendingShutdownCallersShareTheExactResult() => UniTask.ToCoroutine(async () =>
        {
            await StartRunningAsync("concurrent-stop");
            var first = _sample.ShutdownAsync().AsTask();
            Assert.That(first.IsCompleted, Is.False, "The first shutdown must still be observing native cleanup.");
            // Register both waiters before yielding; this must not depend on frame timing.
            var second = _sample.ShutdownAsync().AsTask();
            await Until(() => first.IsCompleted && second.IsCompleted, "both shutdown waiters");
            var firstResult = await first;
            var secondResult = await second;
            Assert.That(secondResult, Is.SameAs(firstResult));
            Assert.That(firstResult, Is.SameAs(_sample.LastResult));
            Assert.That(firstResult.State, Is.EqualTo(NetworkSessionState.Closed));
            Assert.That(NativeStopped, Is.True);
            Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator CancelledFirstShutdownWaiterDoesNotPreventASecondFromJoiningCleanup() => UniTask.ToCoroutine(async () =>
        {
            await StartRunningAsync("cancelled-stop-wait");
            using (var caller = new CancellationTokenSource())
            {
                var first = _sample.ShutdownAsync(caller.Token).AsTask();
                Assert.That(first.IsCompleted, Is.False, "Cancel the wait while native cleanup is still pending.");
                caller.Cancel();
                // The cancelled wait's internal observer is still attached; join again before any yield.
                var second = _sample.ShutdownAsync().AsTask();
                await Until(() => first.IsCompleted && second.IsCompleted, "cancelled and continuing shutdown waiters");
                Exception firstError = null;
                try { await first; }
                catch (Exception error) { firstError = error; }
                Assert.That(firstError, Is.InstanceOf<OperationCanceledException>());
                var result = await second;
                Assert.That(result, Is.SameAs(_sample.LastResult));
                Assert.That(result.State, Is.EqualTo(NetworkSessionState.Closed));
                Assert.That(NativeStopped, Is.True);
                Assert.That(_sample.ReleaseCalls, Is.EqualTo(1));
            }
        });

        private async UniTask StartRunningAsync(string attemptId)
        {
            var start = _sample.StartHostAsync("shared-shutdown", attemptId, new[] { "host" }, Encoding.UTF8.GetBytes("demo-host")).Preserve();
            await Until(() => _sample.IsPreparing, "local preparation");
            _sample.CompletePreparation();
            await Until(() => start.Status != UniTaskStatus.Pending, "server-authorized running result");
            Assert.That((await start).Started, Is.True);
            Assert.That(_sample.Context.State, Is.EqualTo(NetworkSessionState.Running));
            Assert.That(_manager.IsListening && _manager.IsConnectedClient, Is.True);
        }

        private static async UniTask Until(Func<bool> condition, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 7;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) await UniTask.Yield();
            Assert.That(condition(), Is.True, "Condition did not complete within 7 seconds: " + label);
        }
    }
}
