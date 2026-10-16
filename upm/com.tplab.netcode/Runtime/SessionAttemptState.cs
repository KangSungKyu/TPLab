using System;

namespace TPLab.Netcode
{
    /// <summary>
    /// Owns one attempt's local state and fixed deadline. The owner serializes all calls;
    /// this internal protocol performs no native cleanup or project callbacks.
    /// </summary>
    internal sealed class SessionAttemptState
    {
        private readonly double _deadline;
        private double _lastObservedAt;

        internal SessionAttemptPhase Phase { get; private set; } = SessionAttemptPhase.Preparing;
        internal SessionProtocolFailure Failure { get; private set; }
        internal bool LocalReady { get; private set; }
        internal ulong AuthorizationSequence { get; private set; }

        internal SessionAttemptState(double startedAt, double budget)
        {
            if (!IsFinite(startedAt))
                throw new ArgumentOutOfRangeException(nameof(startedAt));
            if (!IsFinite(budget) || budget <= 0)
                throw new ArgumentOutOfRangeException(nameof(budget));

            double deadline = startedAt + budget;
            if (!IsFinite(deadline) || deadline <= startedAt)
                throw new ArgumentOutOfRangeException(nameof(budget), "The deadline must be finite and later than the start.");

            _deadline = deadline;
            _lastObservedAt = startedAt;
        }

        /// <summary>
        /// Checks time before any pending input changes state. Terminal attempts ignore late
        /// inputs, including their time samples; expiry wins over an input at the deadline.
        /// </summary>
        internal bool TryContinue(double now)
        {
            if (Phase != SessionAttemptPhase.Preparing)
                return false;
            if (!IsFinite(now) || now < _lastObservedAt)
                throw new ArgumentOutOfRangeException(nameof(now));

            _lastObservedAt = now;
            if (now >= _deadline)
            {
                Abort(SessionFailureCode.DeadlineExceeded);
                return false;
            }
            return true;
        }

        internal bool CompleteLocalPreparation(double now)
        {
            if (!TryContinue(now) || LocalReady)
                return false;
            LocalReady = true;
            return true;
        }

        internal bool Authorize(ulong sequence)
        {
            if (Phase != SessionAttemptPhase.Preparing)
                return false;
            AuthorizationSequence = sequence;
            Phase = SessionAttemptPhase.StartAuthorized;
            return true;
        }

        internal bool Abort(SessionFailureCode code, string participantId = null,
            Exception cause = null, bool remoteReasonObserved = false)
        {
            if (Phase != SessionAttemptPhase.Preparing)
                return false;
            Failure = new SessionProtocolFailure(code, participantId, cause, remoteReasonObserved);
            Phase = SessionAttemptPhase.Aborted;
            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
