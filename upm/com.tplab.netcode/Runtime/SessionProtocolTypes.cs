using System;

namespace TPLab.Netcode
{
    internal enum SessionAttemptPhase { Preparing, StartAuthorized, Aborted }
    internal enum SessionAdmissionResult
    {
        Accepted, InactiveAttempt, ContextMismatch, UnknownParticipant,
        DuplicateParticipant, DuplicateClient, InvalidConnection
    }
    internal enum SessionFailureCode
    {
        LocalPreparationFailed, ParticipantPreparationFailed, ParticipantDisconnected,
        OwnerCancelled, DeadlineExceeded, EndpointFailed
    }

    /// <summary>Immutable identity for one attempt; these internal types are not a public API.</summary>
    internal sealed class SessionProtocolContext
    {
        internal string SessionId { get; }
        internal string AttemptId { get; }
        internal ulong RosterRevision { get; }
        internal uint ProtocolVersion { get; }
        internal string SceneKey { get; }
        internal SessionProtocolContext(string sessionId, string attemptId, ulong rosterRevision,
            uint protocolVersion, string sceneKey)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("A session identity is required.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(attemptId))
                throw new ArgumentException("An attempt identity is required.", nameof(attemptId));
            if (string.IsNullOrWhiteSpace(sceneKey))
                throw new ArgumentException("A scene identity is required.", nameof(sceneKey));
            SessionId = sessionId;
            AttemptId = attemptId;
            RosterRevision = rosterRevision;
            ProtocolVersion = protocolVersion;
            SceneKey = sceneKey;
        }
        internal bool Matches(SessionProtocolContext other)
        {
            return other != null
                && string.Equals(SessionId, other.SessionId, StringComparison.Ordinal)
                && string.Equals(AttemptId, other.AttemptId, StringComparison.Ordinal)
                && RosterRevision == other.RosterRevision
                && ProtocolVersion == other.ProtocolVersion
                && string.Equals(SceneKey, other.SceneKey, StringComparison.Ordinal);
        }
    }

    internal readonly struct SessionConnection
    {
        internal ulong ClientId { get; }
        internal ulong Generation { get; }
        internal SessionConnection(ulong clientId, ulong generation)
        {
            ClientId = clientId;
            Generation = generation;
        }
    }

    internal readonly struct SessionStartSignal
    {
        internal SessionProtocolContext Context { get; }
        internal ulong Sequence { get; }
        internal SessionStartSignal(SessionProtocolContext context, ulong sequence)
        {
            Context = context;
            Sequence = sequence;
        }
    }

    /// <summary>Retains the first accepted failure; a local cause is never a remote reason payload.</summary>
    internal sealed class SessionProtocolFailure
    {
        internal SessionFailureCode Code { get; }
        internal string ParticipantId { get; }
        internal Exception Cause { get; }
        internal bool RemoteReasonObserved { get; }
        internal SessionProtocolFailure(SessionFailureCode code, string participantId,
            Exception cause, bool remoteReasonObserved = false)
        {
            Code = code;
            ParticipantId = participantId;
            Cause = cause;
            RemoteReasonObserved = remoteReasonObserved;
        }
    }
}
