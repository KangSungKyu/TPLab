using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace TPLab.Netcode
{
    /// <summary>Immutable logical identity. A fresh attempt requires a fresh AttemptId; scene loading remains project owned.</summary>
    public sealed class NetworkSessionIdentity
    {
        internal SessionProtocolContext Protocol { get; }
        /// <summary>Gets the project session identity.</summary>
        public string SessionId => Protocol.SessionId;
        /// <summary>Gets the unique preparation attempt identity.</summary>
        public string AttemptId => Protocol.AttemptId;
        /// <summary>Gets the frozen roster revision.</summary>
        public ulong RosterRevision => Protocol.RosterRevision;
        /// <summary>Gets the project protocol version, independent of the wrapper wire format.</summary>
        public uint ProtocolVersion => Protocol.ProtocolVersion;
        /// <summary>Gets the logical fixed-scene key. The project preparation callback verifies its actual scene.</summary>
        public string SceneKey => Protocol.SceneKey;
        /// <summary>Creates an identity. Blank string identities throw ArgumentException.</summary>
        public NetworkSessionIdentity(string sessionId, string attemptId, ulong rosterRevision, uint protocolVersion, string sceneKey)
        {
            Protocol = new SessionProtocolContext(sessionId, attemptId, rosterRevision, protocolVersion, sceneKey);
        }
    }

    /// <summary>Explicit project limits; there are no runtime timeout or payload defaults.</summary>
    public sealed class NetworkSessionOptions
    {
        /// <summary>Gets the entire admission envelope byte limit, including identity and proof. Does not bound NGO's earlier allocation.</summary>
        public int MaxAdmissionPayloadBytes { get; }
        /// <summary>Gets the finite positive protect/connect/prepare budget in unscaled seconds.</summary>
        public double PreparationTimeoutSeconds { get; }
        /// <summary>Gets the finite positive cleanup observation budget; it cannot forcibly stop hooks or NGO.</summary>
        public double ShutdownTimeoutSeconds { get; }
        /// <summary>Gets the finite positive lobby restore observation budget.</summary>
        public double RestoreTimeoutSeconds { get; }
        /// <summary>Creates explicit limits. Nonpositive sizes or nonfinite/nonpositive durations are rejected.</summary>
        public NetworkSessionOptions(int maxAdmissionPayloadBytes, double preparationTimeoutSeconds,
            double shutdownTimeoutSeconds, double restoreTimeoutSeconds)
        {
            if (maxAdmissionPayloadBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxAdmissionPayloadBytes));
            ValidateDuration(preparationTimeoutSeconds, nameof(preparationTimeoutSeconds));
            ValidateDuration(shutdownTimeoutSeconds, nameof(shutdownTimeoutSeconds));
            ValidateDuration(restoreTimeoutSeconds, nameof(restoreTimeoutSeconds));
            MaxAdmissionPayloadBytes = maxAdmissionPayloadBytes;
            PreparationTimeoutSeconds = preparationTimeoutSeconds;
            ShutdownTimeoutSeconds = shutdownTimeoutSeconds;
            RestoreTimeoutSeconds = restoreTimeoutSeconds;
        }
        private static void ValidateDuration(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameter);
        }
    }

    /// <summary>Copied request for one Host or client attempt. Transport addresses, prefabs and native scene policy are configured by the project.</summary>
    public sealed class NetworkSessionRequest
    {
        /// <summary>Gets the immutable logical attempt identity.</summary>
        public NetworkSessionIdentity Identity { get; }
        /// <summary>Gets whether this request starts a Host. Dedicated-server execution is outside this API.</summary>
        public bool IsHost { get; }
        /// <summary>Gets the frozen Host roster, including its local player. Client requests contain an empty roster.</summary>
        public IReadOnlyList<string> Participants { get; }
        /// <summary>Gets the copied opaque proof. Proof interpretation and replay prevention belong to the project verifier.</summary>
        public ReadOnlyMemory<byte> Proof => ProofBytes;
        internal byte[] ProofBytes { get; }
        /// <summary>Gets the Host initial synchronization mode; ignored when native scene management is disabled or on a client.</summary>
        public LoadSceneMode InitialSynchronizationMode { get; }
        private NetworkSessionRequest(NetworkSessionIdentity identity, bool host, string[] participants, byte[] proof, LoadSceneMode mode)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            if (host)
            {
                if (mode != LoadSceneMode.Single && mode != LoadSceneMode.Additive) throw new ArgumentOutOfRangeException(nameof(mode));
                var unique = new HashSet<string>(StringComparer.Ordinal);
                if (participants.Length == 0) throw new ArgumentException("A frozen roster is required.", nameof(participants));
                foreach (string participant in participants)
                {
                    if (string.IsNullOrWhiteSpace(participant) || !unique.Add(participant))
                        throw new ArgumentException("Roster identities must be nonblank and unique.", nameof(participants));
                }
            }
            Identity = identity;
            IsHost = host;
            Participants = Array.AsReadOnly((string[])participants.Clone());
            ProofBytes = (byte[])proof.Clone();
            InitialSynchronizationMode = mode;
        }
        /// <summary>Copies a Host request. Identity, proof and a nonempty unique roster are required; mode must be Single or Additive.</summary>
        public static NetworkSessionRequest Host(NetworkSessionIdentity identity, string[] participants, byte[] proof, LoadSceneMode mode)
        {
            return new NetworkSessionRequest(identity, true, participants, proof, mode);
        }
        /// <summary>Copies a client request. No participant identity claimed by the client is treated as authenticated.</summary>
        public static NetworkSessionRequest Client(NetworkSessionIdentity identity, byte[] proof)
        {
            return new NetworkSessionRequest(identity, false, Array.Empty<string>(), proof, LoadSceneMode.Single);
        }
    }

    /// <summary>Normalized admission codes. Arbitrary verifier text and exceptions are never transmitted.</summary>
    public enum AdmissionRejectionCode
    {
        None, InvalidPayload, ContextMismatch, VerifierRejected, VerifierFailed, VerificationDeadlineExceeded,
        InvalidVerifierResult, UnknownParticipant, DuplicateParticipant, DuplicateClient, AttemptClosed
    }
    /// <summary>Project-supplied synchronous, nonblocking authentication and proof replay verification.</summary>
    public interface IAdmissionVerifier
    {
        /// <summary>Returns an authenticated roster identity or a normalized rejection. Called on the Unity main thread; do not reenter session commands.</summary>
        AdmissionDecision Verify(AdmissionRequest request);
    }
    /// <summary>Trusted server context and copied opaque proof for one actual native connection.</summary>
    public sealed class AdmissionRequest
    {
        /// <summary>Gets the server's current identity, not a trusted replacement from the client.</summary>
        public NetworkSessionIdentity Identity { get; }
        /// <summary>Gets proof bytes whose envelope has already passed size and structure validation.</summary>
        public ReadOnlyMemory<byte> Proof { get; }
        /// <summary>Gets the actual native client ID, not an account identity.</summary>
        public ulong ClientId { get; }
        /// <summary>Gets the wrapper's connection generation.</summary>
        public ulong ConnectionGeneration { get; }
        /// <summary>Gets the server wall-clock sample for project authentication; session budgets use a separate monotonic clock.</summary>
        public DateTimeOffset ServerUtcNow { get; }
        /// <summary>Gets the remaining preparation budget when verification begins.</summary>
        public double RemainingSeconds { get; }
        internal AdmissionRequest(NetworkSessionIdentity identity, ReadOnlyMemory<byte> proof, ulong clientId,
            ulong connectionGeneration, DateTimeOffset serverUtcNow, double remainingSeconds)
        {
            Identity = identity; Proof = proof; ClientId = clientId; ConnectionGeneration = connectionGeneration;
            ServerUtcNow = serverUtcNow; RemainingSeconds = remainingSeconds;
        }
    }
    /// <summary>Verifier result. The wrapper validates membership and duplicate connections after this decision.</summary>
    public sealed class AdmissionDecision
    {
        /// <summary>Gets the authenticated participant ID for an acceptance.</summary>
        public string ParticipantId { get; }
        /// <summary>Gets the normalized result; None denotes an acceptance requiring further wrapper validation.</summary>
        public AdmissionRejectionCode Code { get; }
        private AdmissionDecision(string participantId, AdmissionRejectionCode code) { ParticipantId = participantId; Code = code; }
        /// <summary>Returns an acceptance candidate; the wrapper rejects blank or unknown IDs.</summary>
        public static AdmissionDecision Accept(string participantId) => new AdmissionDecision(participantId, AdmissionRejectionCode.None);
        /// <summary>Returns a rejection candidate; undefined codes and None are normalized to InvalidVerifierResult.</summary>
        public static AdmissionDecision Reject(AdmissionRejectionCode code) => new AdmissionDecision(null, code);
    }
    /// <summary>Latest server-local admission diagnostic. Cause may contain sensitive data and is never sent to peers.</summary>
    public sealed class AdmissionRejection
    {
        /// <summary>Gets the rejected native connection ID.</summary>
        public ulong ClientId { get; }
        /// <summary>Gets the normalized rejection code.</summary>
        public AdmissionRejectionCode Code { get; }
        /// <summary>Gets the local cause, when available.</summary>
        public Exception Cause { get; }
        internal AdmissionRejection(ulong clientId, AdmissionRejectionCode code, Exception cause) { ClientId = clientId; Code = code; Cause = cause; }
    }
    /// <summary>Local session and recovery state; Running requires a server authorization and successful local gameplay gate.</summary>
    public enum NetworkSessionState { Created, Starting, Preparing, Running, Aborting, RestoringLobby, Lobby, RecoveryFault, Closed }
    /// <summary>First local attempt failure reason. Remote reasons are accepted only as normalized codes.</summary>
    public enum NetworkSessionFailureReason
    {
        LocalPreparationFailed, ParticipantPreparationFailed, ParticipantDisconnected, OwnerCancelled,
        DeadlineExceeded, EndpointFailed, AdmissionRejected, ProtectionFailed, GameplayGateFailed
    }
    /// <summary>Immutable first failure; later recovery errors never replace its cause.</summary>
    public sealed class NetworkSessionFailure
    {
        /// <summary>Gets the first failure category.</summary>
        public NetworkSessionFailureReason Reason { get; }
        /// <summary>Gets the original local exception object, if known. Remote exception text is not exposed here.</summary>
        public Exception Cause { get; }
        /// <summary>Gets the affected authenticated participant ID when known.</summary>
        public string ParticipantId { get; }
        /// <summary>Gets whether a normalized remote reason was received, rather than inferred from local disconnect or expiry.</summary>
        public bool RemoteReasonObserved { get; }
        internal NetworkSessionFailure(NetworkSessionFailureReason reason, Exception cause = null, string participantId = null, bool remoteReasonObserved = false)
        { Reason = reason; Cause = cause; ParticipantId = participantId; RemoteReasonObserved = remoteReasonObserved; }
    }
    /// <summary>Detached outcome of one start, stop or explicit recovery wait.</summary>
    public sealed class NetworkSessionResult
    {
        /// <summary>Gets the local state at completion. It is not a distributed atomic-start guarantee.</summary>
        public NetworkSessionState State { get; }
        /// <summary>Gets whether local gameplay was opened.</summary>
        public bool Started => State == NetworkSessionState.Running;
        /// <summary>Gets the immutable first attempt failure, or null on success/normal close.</summary>
        public NetworkSessionFailure Failure { get; }
        /// <summary>Gets a detached ordered snapshot of protection, cleanup and restore errors.</summary>
        public IReadOnlyList<Exception> RecoveryErrors { get; }
        internal NetworkSessionResult(NetworkSessionState state, NetworkSessionFailure failure, IEnumerable<Exception> errors)
        { State = state; Failure = failure; RecoveryErrors = new List<Exception>(errors).AsReadOnly(); }
    }
    /// <summary>Context passed to project callbacks; it does not transfer native manager ownership.</summary>
    public sealed class NetworkSessionAttempt
    {
        /// <summary>Gets the attempt identity.</summary>
        public NetworkSessionIdentity Identity { get; }
        /// <summary>Gets whether the local peer is the Host.</summary>
        public bool IsHost { get; }
        internal NetworkSessionAttempt(NetworkSessionIdentity identity, bool isHost) { Identity = identity; IsHost = isHost; }
    }
    /// <summary>Project restore input. The SessionId/AttemptId pair is stable across explicit retries and is the project's idempotency key.</summary>
    public sealed class NetworkSessionRecovery
    {
        /// <summary>Gets the original attempt.</summary>
        public NetworkSessionAttempt Attempt { get; }
        /// <summary>Gets the original first failure.</summary>
        public NetworkSessionFailure Failure { get; }
        /// <summary>Gets errors already observed before this restore call.</summary>
        public IReadOnlyList<Exception> RecoveryErrors { get; }
        internal NetworkSessionRecovery(NetworkSessionAttempt attempt, NetworkSessionFailure failure, IEnumerable<Exception> errors)
        { Attempt = attempt; Failure = failure; RecoveryErrors = new List<Exception>(errors).AsReadOnly(); }
    }
    /// <summary>Project-owned UI, root preparation/release and lobby restoration. Hooks must honor cancellation and own any late work.</summary>
    public interface INetworkSessionCallbacks
    {
        /// <summary>Closes gameplay visibility/input before starting and before failure cleanup.</summary>
        UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken);
        /// <summary>Awaits local scene/root/business readiness after native connection/synchronization. Do not wait on this same session from its root installer.</summary>
        UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken);
        /// <summary>Opens the local gameplay gate once after authorization. Must finish synchronously; do fallible preparation in PrepareAsync.</summary>
        void StartGameplay(NetworkSessionAttempt attempt);
        /// <summary>Releases owned project resources after preparation has actually stopped. Must not destroy the borrowed NetworkManager.</summary>
        UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken);
        /// <summary>Restores the lobby after native and hook cleanup. Retry must be idempotent for the same SessionId/AttemptId.</summary>
        UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken cancellationToken);
    }
}
