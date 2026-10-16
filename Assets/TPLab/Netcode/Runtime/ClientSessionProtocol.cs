using System;

namespace TPLab.Netcode
{
    /// <summary>
    /// Accepts one start or abort from the pinned server connection for one attempt.
    /// The owner serializes calls and turns accepted protocol results into project effects.
    /// </summary>
    internal sealed class ClientSessionProtocol
    {
        private readonly SessionProtocolContext _context;
        private readonly SessionConnection _server;
        private readonly SessionAttemptState _attempt;

        internal SessionAttemptPhase Phase => _attempt.Phase;
        internal SessionProtocolFailure Failure => _attempt.Failure;
        internal bool LocalReady => _attempt.LocalReady;
        internal ulong AuthorizationSequence => _attempt.AuthorizationSequence;

        internal ClientSessionProtocol(SessionProtocolContext context, SessionConnection server,
            double startedAt, double budget)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (server.Generation == 0)
                throw new ArgumentException("The server connection must have a positive generation.", nameof(server));
            _server = server;
            _attempt = new SessionAttemptState(startedAt, budget);
        }

        internal bool CompleteLocalPreparation(double now) => _attempt.CompleteLocalPreparation(now);

        internal bool ReceiveStart(SessionConnection sender, SessionStartSignal signal, double now)
        {
            if (!_attempt.TryContinue(now) || !IsServer(sender) || !_context.Matches(signal.Context)
                || !LocalReady || signal.Sequence == 0)
                return false;
            return _attempt.Authorize(signal.Sequence);
        }

        internal bool ReceiveAbort(SessionConnection sender, SessionProtocolContext context,
            SessionFailureCode code, double now)
        {
            if (!_attempt.TryContinue(now) || !IsServer(sender) || !_context.Matches(context)
                || !Enum.IsDefined(typeof(SessionFailureCode), code))
                return false;
            return _attempt.Abort(code, remoteReasonObserved: true);
        }

        internal bool DisconnectServer(SessionConnection sender, double now)
        {
            return _attempt.TryContinue(now) && IsServer(sender)
                && _attempt.Abort(SessionFailureCode.ParticipantDisconnected);
        }

        internal bool ReportLocalFailure(Exception cause, double now)
        {
            return _attempt.TryContinue(now) && _attempt.Abort(SessionFailureCode.LocalPreparationFailed, cause: cause);
        }

        internal bool Cancel(double now)
        {
            return _attempt.TryContinue(now) && _attempt.Abort(SessionFailureCode.OwnerCancelled);
        }

        internal void Poll(double now)
        {
            _attempt.TryContinue(now);
        }

        private bool IsServer(SessionConnection sender)
        {
            return sender.ClientId == _server.ClientId && sender.Generation == _server.Generation;
        }
    }
}
