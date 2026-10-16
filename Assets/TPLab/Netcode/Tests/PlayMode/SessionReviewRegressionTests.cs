using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Netcode.Tests
{
    // False scene management isolates admission and failure propagation regressions.
    public sealed class SessionReviewRegressionTests
    {
        private const string ReentryMessage = "Session commands cannot reenter their project callbacks.";
        private const string Channel = "TPLab.Netcode.Session.v1";
        private readonly List<NetworkManager> _managers = new List<NetworkManager>();
        private readonly List<NetworkSessionContext> _contexts = new List<NetworkSessionContext>();
        private readonly List<SessionStartCoordinator> _coordinators = new List<SessionStartCoordinator>();
        private readonly List<Hooks> _hooks = new List<Hooks>();

        [UnityTest, Timeout(30000)]
        public IEnumerator HostVerifierExceptionPreservesOriginalResultAndAdmissionCause() => UniTask.ToCoroutine(async () =>
        {
            var manager = Manager(FreePort());
            var hooks = HooksFor();
            var cause = new InvalidOperationException("local admission regression sentinel");
            var context = Context(manager, hooks, new Verifier(_ => throw cause));
            try
            {
                var result = await context.StartAsync(HostRequest(Identity(), false));
                Assert.That(result.Failure, Is.Not.Null);
                Assert.That(result.Failure.Reason, Is.EqualTo(NetworkSessionFailureReason.AdmissionRejected));
                Assert.That(result.Failure.Cause, Is.SameAs(cause));
                Assert.That(context.LastAdmissionRejection, Is.Not.Null);
                Assert.That(context.LastAdmissionRejection.Code, Is.EqualTo(AdmissionRejectionCode.VerifierFailed));
                Assert.That(context.LastAdmissionRejection.Cause, Is.SameAs(cause));
                Assert.That(result.State, Is.EqualTo(NetworkSessionState.Lobby));
                Assert.That(IsStopped(manager), Is.True);
                Assert.That(hooks.GameplayCalls, Is.Zero);
            }
            finally { hooks.Open(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator HostVerifierCannotReenterOwnerShutdownAndAdmissionCanStillStart() => UniTask.ToCoroutine(async () =>
        {
            var manager = Manager(FreePort());
            var hooks = HooksFor();
            NetworkSessionContext captured = null;
            Exception observed = null;
            NetworkSessionState before = default, after = default;
            int calls = 0;
            captured = Context(manager, hooks, new Verifier(request =>
            {
                calls++;
                before = captured.State;
                try { _ = captured.ShutdownAsync(); }
                catch (Exception error) { observed = error; }
                after = captured.State;
                return AdmissionDecision.Accept("host");
            }));
            try
            {
                var start = captured.StartAsync(HostRequest(Identity(), false)).Preserve();
                await Until(() => calls == 1, "host verifier invoked");
                AssertReentry(observed, before, after);
                Assert.That(captured.LastAdmissionRejection, Is.Null);
                hooks.Open();
                var result = await start;
                Assert.That(result.Started, Is.True);
                Assert.That(result.Failure, Is.Null);
                Assert.That(hooks.GameplayCalls, Is.EqualTo(1));
                Assert.That(calls, Is.EqualTo(1));
            }
            finally { hooks.Open(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteVerifierCannotReenterOwnerShutdownAndBothPeersCanStart() => UniTask.ToCoroutine(async () =>
        {
            ushort port = FreePort();
            var hostManager = Manager(port);
            var clientManager = Manager(port);
            var hostHooks = HooksFor();
            var clientHooks = HooksFor();
            var identity = Identity();
            NetworkSessionContext captured = null;
            Exception observed = null;
            NetworkSessionState before = default, after = default;
            int remoteCalls = 0;
            captured = Context(hostManager, hostHooks, new Verifier(request =>
            {
                if (request.ClientId == NetworkManager.ServerClientId) return AdmissionDecision.Accept("host");
                remoteCalls++;
                before = captured.State;
                try { _ = captured.ShutdownAsync(); }
                catch (Exception error) { observed = error; }
                after = captured.State;
                return AdmissionDecision.Accept("client");
            }));
            var client = Context(clientManager, clientHooks);
            try
            {
                var hostStart = captured.StartAsync(HostRequest(identity, true)).Preserve();
                await Until(() => hostManager.IsListening, "host listening before remote start");
                var clientStart = client.StartAsync(NetworkSessionRequest.Client(identity, new byte[] { 1 })).Preserve();
                await Until(() => remoteCalls == 1, "remote verifier invoked");
                AssertReentry(observed, before, after);
                Assert.That(captured.LastAdmissionRejection, Is.Null);
                hostHooks.Open(); clientHooks.Open();
                var hostResult = await hostStart;
                var clientResult = await clientStart;
                Assert.That(hostResult.Started && clientResult.Started, Is.True);
                Assert.That(hostResult.Failure, Is.Null);
                Assert.That(clientResult.Failure, Is.Null);
                Assert.That(hostHooks.GameplayCalls, Is.EqualTo(1));
                Assert.That(clientHooks.GameplayCalls, Is.EqualTo(1));
                Assert.That(remoteCalls, Is.EqualTo(1));
            }
            finally { hostHooks.Open(); clientHooks.Open(); }
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator ClientOwnerCancelledAbortPreservesRemoteReasonOnHost() =>
            RemoteAbort(SessionFailureCode.OwnerCancelled, NetworkSessionFailureReason.OwnerCancelled);

        [UnityTest, Timeout(30000)]
        public IEnumerator ClientDeadlineExceededAbortPreservesRemoteReasonOnHost() =>
            RemoteAbort(SessionFailureCode.DeadlineExceeded, NetworkSessionFailureReason.DeadlineExceeded);

        [UnityTest, Timeout(30000)]
        public IEnumerator ClientEndpointFailedAbortPreservesRemoteReasonOnHost() =>
            RemoteAbort(SessionFailureCode.EndpointFailed, NetworkSessionFailureReason.EndpointFailed);

        private IEnumerator RemoteAbort(SessionFailureCode code, NetworkSessionFailureReason expected) => UniTask.ToCoroutine(async () =>
        {
            ushort port = FreePort();
            var hostManager = Manager(port);
            var clientManager = Manager(port);
            var identity = Identity();
            ulong generation = 0;
            var host = Coordinator(hostManager, HostRequest(identity, true), new Verifier(request =>
            {
                if (request.ClientId == NetworkManager.ServerClientId) return AdmissionDecision.Accept("host");
                generation = request.ConnectionGeneration;
                return AdmissionDecision.Accept("client");
            }));
            var client = Coordinator(clientManager, NetworkSessionRequest.Client(identity, new byte[] { 1 }));
            try
            {
                host.Start(); client.Start();
                await Until(() => host.NativeReady && client.NativeReady, "both native peers ready", host, client);
                Assert.That(generation, Is.GreaterThan(0UL));
                Assert.That(host.StartAuthorized || client.StartAuthorized, Is.False);
                byte[] bytes = SessionWireCodec.EncodeControl(new SessionControlMessage(
                    identity.Protocol, SessionControlKind.Abort, generation, 0, code, 0));
                using (var writer = new FastBufferWriter(bytes.Length, Allocator.Temp))
                {
                    writer.WriteBytesSafe(bytes);
                    clientManager.CustomMessagingManager.SendNamedMessage(Channel, NetworkManager.ServerClientId,
                        writer, NetworkDelivery.ReliableFragmentedSequenced);
                }
                // Keep the sender alive until the authenticated message wins over any subsequent disconnect.
                await Until(() => host.Failure != null, "host observed remote abort", host, client);
                Assert.That(host.Failure.Reason, Is.EqualTo(expected));
                Assert.That(host.Failure.RemoteReasonObserved, Is.True);
                Assert.That(host.Failure.ParticipantId, Is.EqualTo("client"));
                Assert.That(host.Failure.Cause, Is.Null);
                Assert.That(host.StartAuthorized, Is.False);
            }
            finally { client.BeginShutdown(); host.BeginShutdown(); }
        });

        private static void AssertReentry(Exception error, NetworkSessionState before, NetworkSessionState after)
        {
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.Message, Is.EqualTo(ReentryMessage));
            Assert.That(after, Is.EqualTo(before), "Rejected synchronous command must not change owner state.");
        }

        private static NetworkSessionIdentity Identity() => new NetworkSessionIdentity("review", "attempt", 7, 2, "fixed");
        // Explicit fixture budgets, not proposed runtime defaults.
        private static NetworkSessionOptions Options() => new NetworkSessionOptions(2048, 15, 5, 5);
        private static NetworkSessionRequest HostRequest(NetworkSessionIdentity identity, bool remote) => NetworkSessionRequest.Host(
            identity, remote ? new[] { "host", "client" } : new[] { "host" }, new byte[] { 1 }, LoadSceneMode.Additive);
        private static ushort FreePort()
        {
            using (var reservation = new UdpClient(0)) return (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
        }
        private NetworkManager Manager(ushort port)
        {
            var go = new GameObject("TPLab-P3-review-owned");
            go.SetActive(false);
            var manager = go.AddComponent<NetworkManager>();
            _managers.Add(manager);
            var transport = go.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false,
                AutoSpawnPlayerPrefabClientSide = false, ProtocolVersion = 219
            };
            manager.RunInBackground = false;
            go.SetActive(true);
            return manager;
        }
        private Hooks HooksFor() { var hooks = new Hooks(); _hooks.Add(hooks); return hooks; }
        private NetworkSessionContext Context(NetworkManager manager, Hooks hooks, IAdmissionVerifier verifier = null)
        {
            var context = new NetworkSessionContext(manager, hooks, Options(), verifier);
            _contexts.Add(context);
            return context;
        }
        private SessionStartCoordinator Coordinator(NetworkManager manager, NetworkSessionRequest request, IAdmissionVerifier verifier = null)
        {
            var coordinator = new SessionStartCoordinator(manager, request, Options(), verifier, Time.realtimeSinceStartupAsDouble);
            _coordinators.Add(coordinator);
            return coordinator;
        }
        private static async UniTask Until(Func<bool> condition, string label, params SessionStartCoordinator[] coordinators)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
            {
                foreach (var coordinator in coordinators) coordinator.Tick();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, label);
        }
        private static bool IsStopped(NetworkManager manager) => manager == null ||
            (!manager.IsListening && !manager.ShutdownInProgress && !manager.IsServer && !manager.IsClient);

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            var errors = new List<Exception>();
            bool stopped = false;
            yield return UniTask.ToCoroutine(async () =>
            {
                foreach (var hooks in _hooks) hooks.Open();
                foreach (var context in _contexts)
                {
                    try { await context.ShutdownAsync(); }
                    catch (Exception error) { errors.Add(error); }
                }
                foreach (var coordinator in _coordinators)
                {
                    try { coordinator.BeginShutdown(); }
                    catch (Exception error) { errors.Add(error); }
                }
                foreach (var manager in _managers)
                {
                    try { if (!IsStopped(manager)) manager.Shutdown(); }
                    catch (Exception error) { errors.Add(error); }
                }
                double deadline = Time.realtimeSinceStartupAsDouble + 8;
                do
                {
                    stopped = true;
                    foreach (var coordinator in _coordinators)
                    {
                        try { coordinator.Tick(); }
                        catch (Exception error) { errors.Add(error); }
                        stopped &= coordinator.NativeStopped;
                    }
                    foreach (var manager in _managers) stopped &= IsStopped(manager);
                    if (!stopped) await UniTask.Yield();
                } while (!stopped && Time.realtimeSinceStartupAsDouble < deadline);
                foreach (var coordinator in _coordinators)
                {
                    try { coordinator.Dispose(); }
                    catch (Exception error) { errors.Add(error); }
                }
                foreach (var manager in _managers) if (manager != null) Object.Destroy(manager.gameObject);
                await UniTask.Yield();
            });
            foreach (var manager in _managers) Assert.That(manager == null, Is.True, "Owned manager was not destroyed.");
            _contexts.Clear(); _coordinators.Clear(); _managers.Clear(); _hooks.Clear();
            Assert.That(stopped, Is.True, "Native stop must be observed before destroying owned managers.");
            Assert.That(errors, Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        private sealed class Verifier : IAdmissionVerifier
        {
            private readonly Func<AdmissionRequest, AdmissionDecision> _verify;
            internal Verifier(Func<AdmissionRequest, AdmissionDecision> verify) { _verify = verify; }
            public AdmissionDecision Verify(AdmissionRequest request) => _verify(request);
        }
        private sealed class Hooks : INetworkSessionCallbacks
        {
            private readonly UniTaskCompletionSource _prepare = new UniTaskCompletionSource();
            internal int GameplayCalls;
            internal void Open() => _prepare.TrySetResult();
            public UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken token) => UniTask.CompletedTask;
            public UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken token) => _prepare.Task.AttachExternalCancellation(token);
            public void StartGameplay(NetworkSessionAttempt attempt) { GameplayCalls++; }
            public UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken token) => UniTask.CompletedTask;
            public UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken token) => UniTask.CompletedTask;
        }
    }
}
