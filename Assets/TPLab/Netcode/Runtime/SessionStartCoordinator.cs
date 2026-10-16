using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TPLab.Netcode
{
    /// <summary>
    /// Borrows one idle manager for one attempt. Native callbacks only update protocol facts;
    /// the context owns project hooks, operation lifetimes and the manager lease.
    /// </summary>
    internal sealed class SessionStartCoordinator : IDisposable
    {
        private const string Channel = "TPLab.Netcode.Session.v1";
        private const string RejectionPrefix = "TPLab.Netcode.Admission.";
        private readonly NetworkManager _manager;
        private readonly NetworkSessionRequest _request;
        private readonly NetworkSessionOptions _options;
        private readonly IAdmissionVerifier _verifier;
        private readonly NetworkConfig _config;
        private readonly double _startedAt, _initialDeadline;
        private readonly int _thread;
        private readonly int _controlSize;
        private readonly bool _sceneManagement;
        private readonly Dictionary<ulong, SessionConnection> _connections = new Dictionary<ulong, SessionConnection>();
        private ServerSessionProtocol _server;
        private ClientSessionProtocol _client;
        private CustomMessagingManager _messaging;
        private NetworkSceneManager _scenes;
        private SessionConnection _serverConnection;
        private ulong _generation;
        private byte[] _originalData, _ownedData;
        private Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> _approval;
        private bool _startCalled, _attemptedNative, _eventsBound, _disposed, _shutdown;
        private bool _connected, _synchronized, _clientStopped, _serverStopped;

        internal SessionStartCoordinator(NetworkManager manager, NetworkSessionRequest request,
            NetworkSessionOptions options, IAdmissionVerifier verifier, double startedAt)
        {
            _manager = manager != null ? manager : throw new ArgumentNullException(nameof(manager));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _verifier = verifier;
            _thread = Thread.CurrentThread.ManagedThreadId;
            _startedAt = startedAt;
            _initialDeadline = startedAt + options.PreparationTimeoutSeconds;
            if (double.IsNaN(startedAt) || double.IsInfinity(startedAt)
                || double.IsInfinity(_initialDeadline) || !(_initialDeadline > startedAt))
                throw new ArgumentOutOfRangeException(nameof(startedAt));
            Preflight();
            _config = manager.NetworkConfig;
            _sceneManagement = _config.EnableSceneManagement;
            _ownedData = SessionWireCodec.EncodeAdmission(request.Identity.Protocol, request.ProofBytes, options.MaxAdmissionPayloadBytes);
            _controlSize = SessionWireCodec.GetControlPayloadSize(request.Identity.Protocol);
        }

        internal bool NativeReady => _attemptedNative && !_shutdown && !_disposed && Failure == null
            && _connected && (_request.IsHost || (!_sceneManagement || _synchronized) && _client != null);
        internal bool StartAuthorized { get; private set; }
        internal bool NativeStopped { get; private set; } = true;
        internal NetworkSessionFailure Failure { get; private set; }
        internal AdmissionRejection LastAdmissionRejection { get; private set; }
        private double Now => Time.realtimeSinceStartupAsDouble;

        internal void Start()
        {
            CheckThread();
            if (_disposed || _startCalled || Failure != null) throw new InvalidOperationException("An adapter can start only one attempt.");
            Preflight();
            if (!ReferenceEquals(_manager.NetworkConfig, _config) || _config.EnableSceneManagement != _sceneManagement)
                throw new InvalidOperationException("The borrowed native configuration changed before start.");
            _startCalled = true;
            if (_request.IsHost)
            {
                var roster = new string[_request.Participants.Count];
                for (int i = 0; i < roster.Length; i++) roster[i] = _request.Participants[i];
                _server = new ServerSessionProtocol(_request.Identity.Protocol, roster, _startedAt, _options.PreparationTimeoutSeconds);
                if (!VerifyAdmission(NetworkManager.ServerClientId, _request.ProofBytes, out _))
                    throw new InvalidOperationException("Host admission was rejected before native start.", LastAdmissionRejection.Cause);
            }
            // Project verification must not have acquired or reconfigured the borrowed endpoint.
            Preflight();
            if (!ReferenceEquals(_manager.NetworkConfig, _config) || _config.EnableSceneManagement != _sceneManagement)
                throw new InvalidOperationException("Project verification changed the borrowed configuration.");
            _originalData = _config.ConnectionData;
            _config.ConnectionData = _ownedData;
            BindManagerEvents();
            if (_request.IsHost)
            {
                _approval = OnApproval;
                _manager.ConnectionApprovalCallback = _approval;
            }
            _attemptedNative = true;
            NativeStopped = false;
            bool started;
            try { started = _request.IsHost ? _manager.StartHost() : _manager.StartClient(); }
            catch (Exception e) { Capture(e); ObserveStop(); throw; }
            ObserveStop();
            if (!started)
            {
                var error = new InvalidOperationException("The native endpoint did not start.");
                Capture(error);
                throw error;
            }
            BindNativeManagers();
        }

        internal void Tick()
        {
            CheckThread();
            if (_disposed || !_attemptedNative) return;
            ObserveStop();
            if (NativeStopped || _shutdown || Failure != null) return;
            if (_manager == null) { Capture(new InvalidOperationException("The borrowed manager was destroyed.")); return; }
            if (!ReferenceEquals(_manager.NetworkConfig, _config) || _config.EnableSceneManagement != _sceneManagement)
            { Capture(new InvalidOperationException("The borrowed scene configuration changed during the attempt.")); return; }
            if (_server != null) _server.Poll(Now);
            else if (_client != null) _client.Poll(Now);
            else if (Now >= _initialDeadline) Failure = new NetworkSessionFailure(NetworkSessionFailureReason.DeadlineExceeded);
            ReadProtocolFailure();
            try { TryAuthorize(); } catch (Exception e) { Capture(e); }
        }

        internal void MarkLocalReady()
        {
            CheckThread();
            if (!NativeReady) throw new InvalidOperationException("Native connection and synchronization must finish before local readiness.");
            if (_server != null)
            {
                if (_server.CompleteLocalPreparation(Now))
                    _server.ReceiveReady(_request.Identity.Protocol, _connections[NetworkManager.ServerClientId], Now);
                ReadProtocolFailure();
                try { TryAuthorize(); } catch (Exception e) { Capture(e); }
            }
            else if (_client.CompleteLocalPreparation(Now))
            {
                try { Send(NetworkManager.ServerClientId, SessionControlKind.Ready, _serverConnection.Generation); }
                catch (Exception e) { Capture(e); }
            }
            ReadProtocolFailure();
        }

        internal void Abort(SessionFailureCode code, Exception cause = null)
        {
            CheckThread();
            if (_disposed || Failure != null || StartAuthorized) return;
            if (!Enum.IsDefined(typeof(SessionFailureCode), code)) throw new ArgumentOutOfRangeException(nameof(code));
            double now = Now;
            _server?.Poll(now);
            _client?.Poll(now);
            ReadProtocolFailure();
            if (Failure != null) return;
            if (_server != null)
            {
                if (code == SessionFailureCode.OwnerCancelled) _server.Cancel(now);
                else if (code == SessionFailureCode.LocalPreparationFailed) _server.ReportLocalFailure(cause, now);
                else if (code == SessionFailureCode.EndpointFailed) _server.FailEndpoint(cause, now);
            }
            else if (_client != null)
            {
                if (code == SessionFailureCode.OwnerCancelled) _client.Cancel(now);
                else if (code == SessionFailureCode.LocalPreparationFailed) _client.ReportLocalFailure(cause, now);
            }
            ReadProtocolFailure();
            if (Failure == null) Failure = new NetworkSessionFailure((NetworkSessionFailureReason)code, cause);
            // The adapter invalidates immediately; native/hook cleanup remains a separate owned operation.
        }

        internal void BeginShutdown()
        {
            CheckThread();
            if (_disposed || NativeStopped || _shutdown) return;
            _shutdown = true;
            if (Failure != null && !StartAuthorized)
            {
                var code = (SessionFailureCode)Failure.Reason;
                if (Enum.IsDefined(typeof(SessionFailureCode), code))
                {
                    if (_request.IsHost)
                    {
                        foreach (var pair in _connections)
                            if (pair.Key != NetworkManager.ServerClientId) TrySendAbort(pair.Key, pair.Value.Generation, code);
                    }
                    else if (_client != null) TrySendAbort(NetworkManager.ServerClientId, _serverConnection.Generation, code);
                }
            }
            if (_manager != null) _manager.Shutdown();
            else Capture(new InvalidOperationException("The borrowed manager was destroyed before shutdown."));
            ObserveStop();
        }

        public void Dispose()
        {
            CheckThread();
            if (_disposed) return;
            ObserveStop();
            if (!NativeStopped) throw new InvalidOperationException("Native stop must be observed before releasing adapter handlers.");
            if (_messaging != null) _messaging.UnregisterNamedMessageHandler(Channel);
            if (_scenes != null) _scenes.OnSynchronizeComplete -= OnSynchronized;
            if (_eventsBound && _manager != null)
            {
                _manager.OnServerStarted -= OnStarted;
                _manager.OnClientStarted -= OnStarted;
                _manager.OnClientConnectedCallback -= OnConnected;
                _manager.OnClientDisconnectCallback -= OnDisconnected;
                _manager.OnClientStopped -= OnClientStopped;
                _manager.OnServerStopped -= OnServerStopped;
                _manager.OnTransportFailure -= OnTransportFailure;
                if (_approval != null && _manager.ConnectionApprovalCallback == _approval) _manager.ConnectionApprovalCallback = null;
                if (ReferenceEquals(_manager.NetworkConfig, _config) && ReferenceEquals(_config.ConnectionData, _ownedData))
                    _config.ConnectionData = _originalData;
            }
            _disposed = true;
        }

        private void Preflight()
        {
            if (_manager == null || !_manager.isActiveAndEnabled || _manager.transform.parent != null
                || _manager.IsListening || _manager.IsClient || _manager.IsServer || _manager.ShutdownInProgress)
                throw new InvalidOperationException("An active, top-level, idle NetworkManager is required.");
            if (_manager.NetworkConfig == null || _manager.NetworkConfig.NetworkTransport == null
                || !_manager.NetworkConfig.ConnectionApproval || _manager.ConnectionApprovalCallback != null
                || _manager.DistributedAuthorityMode || _manager.NetworkConfig.NetworkTopology != NetworkTopologyTypes.ClientServer)
                throw new InvalidOperationException("A configured client/server transport and unclaimed approval callback are required.");
            if (_request.IsHost && _verifier == null) throw new InvalidOperationException("Host admission requires a project verifier.");
        }

        private void BindManagerEvents()
        {
            _eventsBound = true;
            _manager.OnServerStarted += OnStarted;
            _manager.OnClientStarted += OnStarted;
            _manager.OnClientConnectedCallback += OnConnected;
            _manager.OnClientDisconnectCallback += OnDisconnected;
            _manager.OnClientStopped += OnClientStopped;
            _manager.OnServerStopped += OnServerStopped;
            _manager.OnTransportFailure += OnTransportFailure;
        }

        private void OnStarted() => Handle(BindNativeManagers);
        private void BindNativeManagers()
        {
            if (_messaging != null) return;
            _messaging = _manager.CustomMessagingManager;
            _scenes = _manager.SceneManager;
            if (_messaging == null || _scenes == null) throw new InvalidOperationException("Native session services are unavailable after start.");
            _messaging.RegisterNamedMessageHandler(Channel, OnMessage);
            if (_sceneManagement) _scenes.OnSynchronizeComplete += OnSynchronized;
            if (_request.IsHost && _sceneManagement) _scenes.SetClientSynchronizationMode(_request.InitialSynchronizationMode);
        }

        private void OnApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Approved = false;
            try
            {
                CheckThread();
                if (request.ClientNetworkId == NetworkManager.ServerClientId)
                { response.Approved = _connections.ContainsKey(NetworkManager.ServerClientId); return; }
                AdmissionRejectionCode code;
                if (_shutdown || Failure != null || StartAuthorized) code = AdmissionRejectionCode.AttemptClosed;
                else if (!SessionWireCodec.TryDecodeAdmission(request.Payload, _options.MaxAdmissionPayloadBytes, out var context, out var proof))
                    code = AdmissionRejectionCode.InvalidPayload;
                else if (!_request.Identity.Protocol.Matches(context)) code = AdmissionRejectionCode.ContextMismatch;
                else
                {
                    response.Approved = VerifyAdmission(request.ClientNetworkId, proof, out code);
                    if (!response.Approved) response.Reason = RejectionPrefix + code;
                    return;
                }
                Reject(request.ClientNetworkId, code);
                response.Reason = RejectionPrefix + code;
            }
            catch (Exception e)
            {
                Reject(request.ClientNetworkId, AdmissionRejectionCode.VerifierFailed, e);
                response.Reason = RejectionPrefix + AdmissionRejectionCode.VerifierFailed;
            }
        }

        private bool VerifyAdmission(ulong clientId, byte[] proof, out AdmissionRejectionCode code)
        {
            code = AdmissionRejectionCode.VerificationDeadlineExceeded;
            if (Now >= _initialDeadline) { Reject(clientId, code); return false; }
            var connection = new SessionConnection(clientId, checked(++_generation));
            AdmissionDecision decision;
            try { decision = _verifier.Verify(new AdmissionRequest(_request.Identity, proof, clientId, connection.Generation, DateTimeOffset.UtcNow, _initialDeadline - Now)); }
            catch (Exception e) { code = AdmissionRejectionCode.VerifierFailed; Reject(clientId, code, e); return false; }
            if (Now >= _initialDeadline) { Reject(clientId, code); return false; }
            if (decision == null || !Enum.IsDefined(typeof(AdmissionRejectionCode), decision.Code)
                || decision.Code == AdmissionRejectionCode.None && string.IsNullOrWhiteSpace(decision.ParticipantId))
                code = AdmissionRejectionCode.InvalidVerifierResult;
            else if (decision.Code != AdmissionRejectionCode.None) code = decision.Code;
            else
            {
                var result = _server.TryAdmit(_request.Identity.Protocol, decision.ParticipantId, connection, Now);
                if (result == SessionAdmissionResult.Accepted) { _connections.Add(clientId, connection); code = AdmissionRejectionCode.None; return true; }
                code = result == SessionAdmissionResult.UnknownParticipant ? AdmissionRejectionCode.UnknownParticipant
                    : result == SessionAdmissionResult.DuplicateParticipant ? AdmissionRejectionCode.DuplicateParticipant
                    : result == SessionAdmissionResult.DuplicateClient ? AdmissionRejectionCode.DuplicateClient : AdmissionRejectionCode.AttemptClosed;
            }
            Reject(clientId, code);
            return false;
        }

        private void Reject(ulong clientId, AdmissionRejectionCode code, Exception cause = null)
        { LastAdmissionRejection = new AdmissionRejection(clientId, code, cause); }

        private void OnConnected(ulong clientId) => Handle(() =>
        {
            if (_request.IsHost)
            {
                if (!_connections.TryGetValue(clientId, out var connection)) return;
                _server.NotifyConnected(connection, Now);
                if (clientId == NetworkManager.ServerClientId) _connected = true;
                else if (_initialDeadline > Now)
                    Send(clientId, SessionControlKind.Welcome, connection.Generation, remaining: _initialDeadline - Now);
            }
            else _connected = true;
            ReadProtocolFailure();
        });

        private void OnSynchronized(ulong clientId) => Handle(() =>
        { if (!_request.IsHost && clientId == _manager.LocalClientId) _synchronized = true; });

        private void OnMessage(ulong sender, FastBufferReader reader)
        {
            try
            {
                CheckThread();
                if (_disposed || _shutdown || Failure != null || StartAuthorized) return;
                if (reader.Length - reader.Position != _controlSize) return;
                var bytes = new byte[_controlSize];
                reader.ReadBytesSafe(ref bytes, _controlSize);
                if (!SessionWireCodec.TryDecodeControl(bytes, _request.Identity.Protocol, out var message)) return;
                if (_request.IsHost)
                {
                    if (!_connections.TryGetValue(sender, out var connection) || connection.Generation != message.Generation) return;
                    if (message.Kind == SessionControlKind.Ready) _server.ReceiveReady(message.Context, connection, Now);
                    else if (message.Kind == SessionControlKind.Abort) _server.ReportParticipantFailure(message.Context, connection, null, Now);
                }
                else if (sender == NetworkManager.ServerClientId)
                {
                    if (message.Kind == SessionControlKind.Welcome && _client == null)
                    {
                        double now = Now, deadline = Math.Min(_initialDeadline, now + message.RemainingSeconds);
                        if (deadline <= now) { Failure = new NetworkSessionFailure(NetworkSessionFailureReason.DeadlineExceeded); return; }
                        _serverConnection = new SessionConnection(sender, message.Generation);
                        _client = new ClientSessionProtocol(message.Context, _serverConnection, now, deadline - now);
                    }
                    else if (_client != null && _serverConnection.Generation == message.Generation)
                    {
                        if (message.Kind == SessionControlKind.Start)
                            StartAuthorized = _client.ReceiveStart(_serverConnection, new SessionStartSignal(message.Context, message.Sequence), Now);
                        else if (message.Kind == SessionControlKind.Abort) _client.ReceiveAbort(_serverConnection, message.Context, message.FailureCode, Now);
                    }
                }
                ReadProtocolFailure();
            }
            catch (Exception e) { Capture(e); }
        }

        private void TryAuthorize()
        {
            if (_server == null || _shutdown || Failure != null || StartAuthorized || !_server.TryAuthorizeStart(Now, out var signal))
            { ReadProtocolFailure(); return; }
            StartAuthorized = true;
            foreach (var pair in _connections)
                if (pair.Key != NetworkManager.ServerClientId) Send(pair.Key, SessionControlKind.Start, pair.Value.Generation, signal.Sequence);
        }

        private void Send(ulong recipient, SessionControlKind kind, ulong generation, ulong sequence = 0,
            SessionFailureCode code = default, double remaining = 0)
        {
            byte[] bytes = SessionWireCodec.EncodeControl(new SessionControlMessage(_request.Identity.Protocol, kind, generation, sequence, code, remaining));
            using (var writer = new FastBufferWriter(bytes.Length, Allocator.Temp))
            {
                writer.WriteBytesSafe(bytes);
                _messaging.SendNamedMessage(Channel, recipient, writer, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private void TrySendAbort(ulong recipient, ulong generation, SessionFailureCode code)
        { try { if (_messaging != null && _manager != null && _manager.IsListening) Send(recipient, SessionControlKind.Abort, generation, code: code); } catch { /* Best effort; preserve the first failure. */ } }

        private void OnDisconnected(ulong clientId) => Handle(() =>
        {
            if (_request.IsHost)
            {
                if (_connections.TryGetValue(clientId, out var connection)) _server.Disconnect(connection, Now);
            }
            else if (_manager.DisconnectReason != null && _manager.DisconnectReason.StartsWith(RejectionPrefix, StringComparison.Ordinal)
                && Enum.TryParse(_manager.DisconnectReason.Substring(RejectionPrefix.Length), out AdmissionRejectionCode code)
                && code != AdmissionRejectionCode.None && Enum.IsDefined(typeof(AdmissionRejectionCode), code))
                Failure = new NetworkSessionFailure(NetworkSessionFailureReason.AdmissionRejected, remoteReasonObserved: true);
            else if (_client != null) _client.DisconnectServer(_serverConnection, Now);
            else Failure = new NetworkSessionFailure(NetworkSessionFailureReason.ParticipantDisconnected);
            ReadProtocolFailure();
        });

        private void OnTransportFailure() => Capture(new InvalidOperationException("The native transport failed."));
        private void OnClientStopped(bool host) { _clientStopped = true; ObserveStop(); }
        private void OnServerStopped(bool host) { _serverStopped = true; ObserveStop(); }
        private void ObserveStop()
        {
            if (_attemptedNative && _manager != null && _clientStopped && (!_request.IsHost || _serverStopped)
                && !_manager.IsListening && !_manager.ShutdownInProgress && !_manager.IsClient && !_manager.IsServer)
                NativeStopped = true;
        }
        private void ReadProtocolFailure()
        {
            var failure = _server != null ? _server.Failure : _client?.Failure;
            if (Failure == null && failure != null)
                Failure = new NetworkSessionFailure((NetworkSessionFailureReason)failure.Code, failure.Cause, failure.ParticipantId, failure.RemoteReasonObserved);
        }
        private void Capture(Exception cause)
        { if (Failure == null) Failure = new NetworkSessionFailure(NetworkSessionFailureReason.EndpointFailed, cause); }
        private void Handle(Action action)
        { try { CheckThread(); if (!_disposed && !_shutdown && Failure == null && !StartAuthorized) action(); } catch (Exception e) { Capture(e); } }
        private void CheckThread()
        { if (Thread.CurrentThread.ManagedThreadId != _thread) throw new InvalidOperationException("Session adapter commands require their owning Unity thread."); }
    }
}
