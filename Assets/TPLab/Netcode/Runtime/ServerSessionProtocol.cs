using System;
using System.Collections.Generic;

namespace TPLab.Netcode
{
    /// <summary>
    /// Serially checks one frozen roster using already verified participant identities.
    /// Admission, native connection and readiness are distinct facts; native effects belong to P3.
    /// </summary>
    internal sealed class ServerSessionProtocol
    {
        private sealed class ParticipantState
        {
            internal readonly string Id;
            internal SessionConnection Connection;
            internal bool Admitted;
            internal bool Connected;
            internal bool Ready;

            internal ParticipantState(string id)
            {
                Id = id;
            }
        }

        private readonly SessionAttemptState _attempt;
        private readonly Dictionary<string, ParticipantState> _byParticipant =
            new Dictionary<string, ParticipantState>(StringComparer.Ordinal);
        private readonly Dictionary<ulong, ParticipantState> _byClient =
            new Dictionary<ulong, ParticipantState>();

        internal SessionProtocolContext Context { get; }
        internal SessionAttemptPhase Phase => _attempt.Phase;
        internal SessionProtocolFailure Failure => _attempt.Failure;
        internal IReadOnlyList<string> Participants { get; }
        internal int AdmittedCount => _byClient.Count;
        internal int ConnectedCount { get; private set; }
        internal int ReadyCount { get; private set; }
        internal bool LocalReady => _attempt.LocalReady;
        internal ulong AuthorizationSequence => _attempt.AuthorizationSequence;

        internal ServerSessionProtocol(SessionProtocolContext context, string[] participants,
            double startedAt, double budget)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            if (participants == null || participants.Length == 0)
                throw new ArgumentException("A nonempty frozen roster is required.", nameof(participants));

            var snapshot = (string[])participants.Clone();
            foreach (string id in snapshot)
            {
                if (string.IsNullOrWhiteSpace(id) || _byParticipant.ContainsKey(id))
                    throw new ArgumentException("Roster identities must be nonblank and unique.", nameof(participants));
                _byParticipant.Add(id, new ParticipantState(id));
            }
            Participants = Array.AsReadOnly(snapshot);
            _attempt = new SessionAttemptState(startedAt, budget);
        }

        internal SessionAdmissionResult TryAdmit(SessionProtocolContext context, string participantId,
            SessionConnection connection, double now)
        {
            if (!_attempt.TryContinue(now))
                return SessionAdmissionResult.InactiveAttempt;
            if (!Context.Matches(context))
                return SessionAdmissionResult.ContextMismatch;
            if (connection.Generation == 0)
                return SessionAdmissionResult.InvalidConnection;
            if (string.IsNullOrWhiteSpace(participantId) || !_byParticipant.TryGetValue(participantId, out var participant))
                return SessionAdmissionResult.UnknownParticipant;
            if (participant.Admitted)
                return SessionAdmissionResult.DuplicateParticipant;
            if (_byClient.ContainsKey(connection.ClientId))
                return SessionAdmissionResult.DuplicateClient;

            participant.Connection = connection;
            participant.Admitted = true;
            _byClient.Add(connection.ClientId, participant);
            return SessionAdmissionResult.Accepted;
        }

        internal bool NotifyConnected(SessionConnection connection, double now)
        {
            if (!_attempt.TryContinue(now) || !TryFindParticipant(connection, out var participant) || participant.Connected)
                return false;
            participant.Connected = true;
            ConnectedCount++;
            return true;
        }

        internal bool ReceiveReady(SessionProtocolContext context, SessionConnection connection, double now)
        {
            if (!_attempt.TryContinue(now) || !Context.Matches(context)
                || !TryFindParticipant(connection, out var participant) || !participant.Connected || participant.Ready)
                return false;
            participant.Ready = true;
            ReadyCount++;
            return true;
        }

        internal bool CompleteLocalPreparation(double now) => _attempt.CompleteLocalPreparation(now);

        internal bool TryAuthorizeStart(double now, out SessionStartSignal signal)
        {
            signal = default;
            if (!_attempt.TryContinue(now) || !LocalReady
                || AdmittedCount != Participants.Count || ConnectedCount != Participants.Count || ReadyCount != Participants.Count)
                return false;

            _attempt.Authorize(1);
            signal = new SessionStartSignal(Context, AuthorizationSequence);
            return true;
        }

        internal bool ReportParticipantFailure(SessionProtocolContext context, SessionConnection connection,
            Exception cause, double now)
        {
            if (!_attempt.TryContinue(now) || !Context.Matches(context)
                || !TryFindParticipant(connection, out var participant) || !participant.Connected)
                return false;
            return _attempt.Abort(SessionFailureCode.ParticipantPreparationFailed, participant.Id, cause);
        }

        internal bool ReportLocalFailure(Exception cause, double now)
        {
            return _attempt.TryContinue(now) && _attempt.Abort(SessionFailureCode.LocalPreparationFailed, cause: cause);
        }

        internal bool FailEndpoint(Exception cause, double now)
        {
            return _attempt.TryContinue(now) && _attempt.Abort(SessionFailureCode.EndpointFailed, cause: cause);
        }

        internal bool Disconnect(SessionConnection connection, double now)
        {
            if (!_attempt.TryContinue(now) || !TryFindParticipant(connection, out var participant))
                return false;
            return _attempt.Abort(SessionFailureCode.ParticipantDisconnected, participant.Id);
        }

        internal bool Cancel(double now)
        {
            return _attempt.TryContinue(now) && _attempt.Abort(SessionFailureCode.OwnerCancelled);
        }

        internal void Poll(double now)
        {
            _attempt.TryContinue(now);
        }

        private bool TryFindParticipant(SessionConnection connection, out ParticipantState participant)
        {
            if (connection.Generation != 0 && _byClient.TryGetValue(connection.ClientId, out participant)
                && participant.Connection.Generation == connection.Generation)
                return true;
            participant = null;
            return false;
        }
    }
}
