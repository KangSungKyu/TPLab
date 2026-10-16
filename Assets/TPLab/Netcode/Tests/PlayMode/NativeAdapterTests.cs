using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Netcode.Tests
{
    public class NativeAdapterTests
    {
        private sealed class Verifier : IAdmissionVerifier
        {
            private readonly Func<AdmissionRequest, AdmissionDecision> _verify;
            internal int Calls { get; private set; }

            internal Verifier(Func<AdmissionRequest, AdmissionDecision> verify)
            {
                _verify = verify;
            }

            public AdmissionDecision Verify(AdmissionRequest request)
            {
                Calls++;
                return _verify(request);
            }
        }

        private static ushort _nextPort = 28340;
        private readonly List<NetworkManager> _managers = new List<NetworkManager>();
        private readonly List<SessionStartCoordinator> _coordinators = new List<SessionStartCoordinator>();

        private static NetworkSessionIdentity Identity() => new NetworkSessionIdentity("native-session", "native-attempt", 1, 1, "fixed-arena");
        private static NetworkSessionOptions Options() => new NetworkSessionOptions(4096, 15, 5, 5);
        private static double Now => Time.realtimeSinceStartupAsDouble;

        private NetworkManager Manager(ushort port, bool sceneManagement)
        {
            var go = new GameObject("TPLab-P3-NativeAdapter-owned");
            go.SetActive(false);
            var manager = go.AddComponent<NetworkManager>();
            _managers.Add(manager);
            var transport = go.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = sceneManagement,
                AutoSpawnPlayerPrefabClientSide = false,
                ProtocolVersion = 219
            };
            manager.RunInBackground = false;
            go.SetActive(true);

            // Use only public callbacks to exclude the transient TestRunner/native shared scene.
            // This smoke fixture does not establish real project scene-object synchronization.
            if (sceneManagement)
            {
                manager.OnServerStarted += () => ConfigureSafeScenes(manager);
                manager.OnClientStarted += () => ConfigureSafeScenes(manager);
            }
            return manager;
        }

        private static void ConfigureSafeScenes(NetworkManager manager)
        {
            manager.SceneManager.VerifySceneBeforeLoading = (index, name, mode) => false;
            manager.SceneManager.VerifySceneBeforeUnloading = scene => false;
            manager.SceneManager.DisableValidationWarnings(true);
        }

        private SessionStartCoordinator Coordinator(NetworkManager manager, bool host, string[] participants,
            IAdmissionVerifier verifier = null)
        {
            var identity = Identity();
            var request = host
                ? NetworkSessionRequest.Host(identity, participants, new byte[] { 2, 4, 6 }, LoadSceneMode.Additive)
                : NetworkSessionRequest.Client(identity, new byte[] { 2, 4, 6 });
            var coordinator = new SessionStartCoordinator(manager, request, Options(), verifier, Now);
            _coordinators.Add(coordinator);
            return coordinator;
        }

        private static IEnumerator WaitFor(Func<bool> condition, string label, params SessionStartCoordinator[] coordinators)
        {
            var watch = Stopwatch.StartNew();
            while (!condition() && watch.Elapsed.TotalSeconds < 8)
            {
                foreach (var coordinator in coordinators) coordinator.Tick();
                yield return null;
            }
            Assert.That(condition(), Is.True, label);
        }

        [Test]
        public void LocalReadyBeforeNativeReadyIsRejectedWithoutStartingAnEndpoint()
        {
            var manager = Manager(_nextPort++, false);
            var coordinator = Coordinator(manager, true, new[] { "host" }, new Verifier(_ => AdmissionDecision.Accept("host")));
            Assert.That(coordinator.NativeStopped, Is.True);
            Assert.Throws<InvalidOperationException>(() => coordinator.MarkLocalReady());
            Assert.That(coordinator.NativeReady, Is.False);
            Assert.That(manager.IsListening, Is.False);
        }

        [Test]
        public void HostVerifierRejectionPreventsNativeStartAndPreservesRejectionCode()
        {
            var manager = Manager(_nextPort++, false);
            var verifier = new Verifier(_ => AdmissionDecision.Reject(AdmissionRejectionCode.VerifierRejected));
            var coordinator = Coordinator(manager, true, new[] { "host" }, verifier);
            Assert.Catch(() => coordinator.Start());
            Assert.That(verifier.Calls, Is.EqualTo(1));
            Assert.That(coordinator.LastAdmissionRejection.Code, Is.EqualTo(AdmissionRejectionCode.VerifierRejected));
            Assert.That(manager.IsListening || manager.IsServer || manager.IsClient, Is.False);
            Assert.That(coordinator.NativeStopped, Is.True);
        }

        [Test]
        public void HostVerifierExceptionRemainsLocalAndPreventsNativeStart()
        {
            var manager = Manager(_nextPort++, false);
            var cause = new InvalidOperationException("local verifier sentinel");
            var coordinator = Coordinator(manager, true, new[] { "host" }, new Verifier(_ => throw cause));
            Assert.Catch(() => coordinator.Start());
            Assert.That(coordinator.LastAdmissionRejection.Code, Is.EqualTo(AdmissionRejectionCode.VerifierFailed));
            Assert.That(coordinator.LastAdmissionRejection.Cause, Is.SameAs(cause));
            Assert.That(manager.IsListening, Is.False);
            Assert.That(coordinator.NativeStopped, Is.True);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator FalseHostVerifiesOnceAndRequiresLocalReadyBeforeAuthorization()
        {
            var manager = Manager(_nextPort++, false);
            var verifier = new Verifier(_ => AdmissionDecision.Accept("host"));
            var coordinator = Coordinator(manager, true, new[] { "host" }, verifier);
            coordinator.Start();
            Assert.That(manager.IsHost && manager.IsListening, Is.True, "Red stub must fail before waiting.");
            Assert.That(coordinator.NativeStopped, Is.False);
            yield return WaitFor(() => coordinator.NativeReady, "host native connected", coordinator);
            Assert.That(verifier.Calls, Is.EqualTo(1));
            Assert.That(coordinator.StartAuthorized, Is.False);
            coordinator.MarkLocalReady();
            yield return WaitFor(() => coordinator.StartAuthorized, "host start authorization", coordinator);
            Assert.That(verifier.Calls, Is.EqualTo(1));
            Assert.That(coordinator.Failure, Is.Null);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator TrueHostBecomesReadyWithoutWaitingForItsOwnSynchronizeComplete()
        {
            var manager = Manager(_nextPort++, true);
            int selfSync = 0;
            manager.OnServerStarted += () => manager.SceneManager.OnSynchronizeComplete += id =>
            {
                if (id == NetworkManager.ServerClientId) selfSync++;
            };
            var coordinator = Coordinator(manager, true, new[] { "host" }, new Verifier(_ => AdmissionDecision.Accept("host")));
            coordinator.Start();
            Assert.That(manager.IsHost, Is.True, "Red stub must fail before waiting.");
            yield return WaitFor(() => coordinator.NativeReady, "true host native ready", coordinator);
            Assert.That(selfSync, Is.Zero);
            Assert.That(manager.NetworkConfig.EnableSceneManagement, Is.True);
            coordinator.MarkLocalReady();
            yield return WaitFor(() => coordinator.StartAuthorized, "true host local barrier", coordinator);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator FalsePeersRequireBothLocalPreparationsAfterNativeConnection()
        {
            ushort port = _nextPort++;
            var hostManager = Manager(port, false);
            var clientManager = Manager(port, false);
            var host = Coordinator(hostManager, true, new[] { "host", "guest" },
                new Verifier(r => AdmissionDecision.Accept(r.ClientId == NetworkManager.ServerClientId ? "host" : "guest")));
            var client = Coordinator(clientManager, false, null);
            host.Start();
            Assert.That(hostManager.IsListening, Is.True, "Red stub must fail before waiting.");
            client.Start();
            Assert.That(clientManager.IsListening, Is.True);
            yield return WaitFor(() => host.NativeReady && client.NativeReady, "false peers native ready", host, client);
            host.MarkLocalReady();
            host.Tick(); client.Tick();
            Assert.That(host.StartAuthorized || client.StartAuthorized, Is.False);
            client.MarkLocalReady();
            yield return WaitFor(() => host.StartAuthorized && client.StartAuthorized, "false peer barrier", host, client);
            Assert.That(host.Failure, Is.Null);
            Assert.That(client.Failure, Is.Null);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator TrueAdditiveClientRequiresConnectedAndLocalSynchronizationFacts()
        {
            ushort port = _nextPort++;
            var hostManager = Manager(port, true);
            var clientManager = Manager(port, true);
            bool localConnected = false, localSync = false;
            clientManager.OnClientConnectedCallback += _ => localConnected = true;
            clientManager.OnClientStarted += () => clientManager.SceneManager.OnSynchronizeComplete += _ => localSync = true;
            var host = Coordinator(hostManager, true, new[] { "host", "guest" },
                new Verifier(r => AdmissionDecision.Accept(r.ClientId == NetworkManager.ServerClientId ? "host" : "guest")));
            var client = Coordinator(clientManager, false, null);
            host.Start();
            Assert.That(hostManager.IsListening, Is.True, "Red stub must fail before waiting.");
            client.Start();
            Assert.That(clientManager.IsListening, Is.True);
            yield return WaitFor(() => host.NativeReady && client.NativeReady, "safe true peers native ready", host, client);
            Assert.That(localConnected && localSync, Is.True);
            Assert.That(clientManager.SceneManager.ClientSynchronizationMode, Is.EqualTo(LoadSceneMode.Additive));
            Assert.That(clientManager.NetworkConfig.EnableSceneManagement, Is.True);
            host.MarkLocalReady(); client.MarkLocalReady();
            yield return WaitFor(() => host.StartAuthorized && client.StartAuthorized, "true peer barrier", host, client);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RejectedRemoteAdmissionPreservesHealthyHostAndFrozenBarrier()
        {
            ushort port = _nextPort++;
            var hostManager = Manager(port, false);
            var clientManager = Manager(port, false);
            var host = Coordinator(hostManager, true, new[] { "host", "guest" }, new Verifier(r =>
                r.ClientId == NetworkManager.ServerClientId ? AdmissionDecision.Accept("host")
                    : AdmissionDecision.Reject(AdmissionRejectionCode.VerifierRejected)));
            var client = Coordinator(clientManager, false, null);
            host.Start();
            Assert.That(hostManager.IsListening, Is.True, "Red stub must fail before waiting.");
            yield return WaitFor(() => host.NativeReady, "host native ready", host);
            host.MarkLocalReady();
            client.Start();
            yield return WaitFor(() => host.LastAdmissionRejection != null, "remote rejection observed", host, client);
            Assert.That(host.LastAdmissionRejection.Code, Is.EqualTo(AdmissionRejectionCode.VerifierRejected));
            Assert.That(hostManager.IsListening && host.NativeReady, Is.True);
            Assert.That(host.StartAuthorized, Is.False);
            Assert.That(host.Failure, Is.Null);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DuplicateVerifiedParticipantCannotReplaceHostMapping()
        {
            ushort port = _nextPort++;
            var hostManager = Manager(port, false);
            var clientManager = Manager(port, false);
            var host = Coordinator(hostManager, true, new[] { "host", "guest" }, new Verifier(_ => AdmissionDecision.Accept("host")));
            var client = Coordinator(clientManager, false, null);
            host.Start();
            Assert.That(hostManager.IsListening, Is.True, "Red stub must fail before waiting.");
            yield return WaitFor(() => host.NativeReady, "host native ready", host);
            host.MarkLocalReady();
            client.Start();
            yield return WaitFor(() => host.LastAdmissionRejection != null, "duplicate identity rejection", host, client);
            Assert.That(host.LastAdmissionRejection.Code, Is.EqualTo(AdmissionRejectionCode.DuplicateParticipant));
            Assert.That(hostManager.IsHost && host.NativeReady, Is.True);
            Assert.That(host.StartAuthorized, Is.False);
            Assert.That(host.Failure, Is.Null);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownWaitsForBothHostStopEventsThenRestoresBorrowedFields()
        {
            var manager = Manager(_nextPort++, false);
            var originalData = new byte[] { 9, 8, 7 };
            manager.NetworkConfig.ConnectionData = originalData;
            int clientStops = 0, serverStops = 0;
            manager.OnClientStopped += _ => clientStops++;
            manager.OnServerStopped += _ => serverStops++;
            var coordinator = Coordinator(manager, true, new[] { "host" }, new Verifier(_ => AdmissionDecision.Accept("host")));
            coordinator.Start();
            Assert.That(manager.IsHost, Is.True, "Red stub must fail before waiting.");
            yield return WaitFor(() => coordinator.NativeReady, "host native ready", coordinator);
            coordinator.MarkLocalReady();
            yield return WaitFor(() => coordinator.StartAuthorized, "start before normal stop", coordinator);
            coordinator.BeginShutdown();
            Assert.That(coordinator.NativeStopped, Is.False);
            Assert.That(manager.ShutdownInProgress, Is.True);
            coordinator.BeginShutdown();
            yield return WaitFor(() => coordinator.NativeStopped, "host stop observed", coordinator);
            Assert.That(clientStops, Is.EqualTo(1));
            Assert.That(serverStops, Is.EqualTo(1));
            Assert.That(IsStopped(manager), Is.True);
            coordinator.Dispose();
            _coordinators.Remove(coordinator);
            Assert.That(manager.ConnectionApprovalCallback, Is.Null);
            Assert.That(manager.NetworkConfig.ConnectionData, Is.SameAs(originalData));
            Assert.That(coordinator.Failure, Is.Null, "Normal running stop is not an attempt abort.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ActiveDisposeIsRefusedAndStoppedManagerCanBeBorrowedByANewCoordinator()
        {
            var manager = Manager(_nextPort++, false);
            var verifier = new Verifier(_ => AdmissionDecision.Accept("host"));
            var first = Coordinator(manager, true, new[] { "host" }, verifier);
            first.Start();
            Assert.That(manager.IsHost, Is.True, "Red stub must fail before waiting.");
            Assert.Throws<InvalidOperationException>(() => first.Dispose());
            Assert.That(manager.IsListening, Is.True);
            first.BeginShutdown();
            yield return WaitFor(() => first.NativeStopped, "first coordinator stopped", first);
            first.Dispose();
            _coordinators.Remove(first);
            var second = Coordinator(manager, true, new[] { "host" }, verifier);
            second.Start();
            Assert.That(manager.IsHost, Is.True);
            yield return WaitFor(() => second.NativeReady, "new coordinator native ready", second);
            second.MarkLocalReady();
            yield return WaitFor(() => second.StartAuthorized, "new coordinator authorized", second);
            Assert.That(first.NativeStopped, Is.True);
            Assert.That(first.StartAuthorized, Is.False);
            Assert.That(first.Failure, Is.Null);
            Assert.That(verifier.Calls, Is.EqualTo(2));
        }

        private static bool IsStopped(NetworkManager manager)
        {
            return manager == null || (!manager.IsListening && !manager.ShutdownInProgress && !manager.IsServer && !manager.IsClient);
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            var errors = new List<Exception>();
            foreach (var coordinator in _coordinators)
            {
                try { coordinator.BeginShutdown(); }
                catch (Exception e) { errors.Add(e); }
            }
            foreach (var manager in _managers)
            {
                if (manager != null && !IsStopped(manager)) manager.Shutdown();
            }
            var watch = Stopwatch.StartNew();
            bool stopped;
            do
            {
                stopped = true;
                foreach (var coordinator in _coordinators)
                {
                    try { coordinator.Tick(); }
                    catch (Exception e) { errors.Add(e); }
                    stopped &= coordinator.NativeStopped;
                }
                foreach (var manager in _managers) stopped &= IsStopped(manager);
                if (!stopped) yield return null;
            } while (!stopped && watch.Elapsed.TotalSeconds < 8);

            foreach (var coordinator in _coordinators)
            {
                try { coordinator.Dispose(); }
                catch (Exception e) { errors.Add(e); }
            }
            foreach (var manager in _managers)
            {
                if (manager != null) Object.Destroy(manager.gameObject);
            }
            yield return null;
            foreach (var manager in _managers) Assert.That(manager == null, Is.True, "Owned manager was not destroyed.");
            _coordinators.Clear();
            _managers.Clear();
            Assert.That(stopped, Is.True, "Native cleanup did not reach a verified stop before destroying owned objects.");
            Assert.That(errors, Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
