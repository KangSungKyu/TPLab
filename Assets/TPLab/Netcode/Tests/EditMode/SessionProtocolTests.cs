using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TPLab.Netcode.Tests
{
    public class SessionProtocolTests
    {
        private static SessionProtocolContext Context(int changed = -1)
        {
            return new SessionProtocolContext(changed == 0 ? "other" : "session",
                changed == 1 ? "older" : "attempt", changed == 2 ? 9ul : 8ul,
                changed == 3 ? 2u : 1u, changed == 4 ? "other-scene" : "arena");
        }

        private static SessionConnection Peer(ulong id = 1, ulong generation = 1)
        {
            return new SessionConnection(id, generation);
        }

        private static ServerSessionProtocol Server(string[] roster = null)
        {
            return new ServerSessionProtocol(Context(), roster ?? new[] { "a", "b" }, 100, 20);
        }

        private static void Admit(ServerSessionProtocol server, string id, SessionConnection peer,
            double now = 100)
        {
            Assert.That(server.TryAdmit(Context(), id, peer, now), Is.EqualTo(SessionAdmissionResult.Accepted));
        }

        private static ServerSessionProtocol ReadyServer()
        {
            var server = Server();
            Admit(server, "a", Peer(0));
            Admit(server, "b", Peer(1));
            Assert.That(server.NotifyConnected(Peer(0), 100), Is.True);
            Assert.That(server.NotifyConnected(Peer(1), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(0), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(1), 100), Is.True);
            Assert.That(server.CompleteLocalPreparation(100), Is.True);
            return server;
        }

        private static ClientSessionProtocol Client()
        {
            return new ClientSessionProtocol(Context(), Peer(0), 100, 20);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ContextRejectsMissingIdentity(int missing)
        {
            Assert.Throws<ArgumentException>(() => new SessionProtocolContext(
                missing == 0 ? null : "s", missing == 1 ? "" : "a", 0, 1,
                missing == 2 ? " " : "arena"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ContextComparesEveryDimensionByValue(int changed)
        {
            Assert.That(Context().Matches(Context(changed)), Is.False);
            Assert.That(Context().Matches(null), Is.False);
            Assert.That(Context().Matches(Context()), Is.True);
        }

        [Test]
        public void RosterIsAnImmutableOrderedSnapshot()
        {
            var source = new[] { "a", "b" };
            var server = Server(source);
            source[0] = "intruder";
            Assert.That(server.Participants, Is.EqualTo(new[] { "a", "b" }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)server.Participants)[0] = "intruder");
            Admit(server, "a", Peer());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RosterRejectsNullEmptyDuplicateOrBlankMembers(int invalid)
        {
            string[][] rosters = { null, Array.Empty<string>(), new[] { "a", "a" }, new[] { "a", " " } };
            Assert.That(() => new ServerSessionProtocol(Context(), rosters[invalid], 100, 20),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void BothRolesRejectInvalidBudgets(double budget)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ServerSessionProtocol(Context(), new[] { "a" }, 100, budget));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClientSessionProtocol(Context(), Peer(0), 100, budget));
        }

        [Test]
        public void ConstructionRejectsInvalidContextConnectionOrClockRange()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerSessionProtocol(null, new[] { "a" }, 100, 20));
            Assert.Throws<ArgumentNullException>(() => new ClientSessionProtocol(null, Peer(0), 100, 20));
            Assert.Throws<ArgumentException>(() => new ClientSessionProtocol(Context(), default, 100, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ServerSessionProtocol(Context(), new[] { "a" }, double.NaN, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClientSessionProtocol(Context(), Peer(0), double.MaxValue, double.MaxValue));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(99d)]
        public void InvalidOrBackwardTimeDoesNotMutateEitherRole(double now)
        {
            var server = Server();
            var client = Client();
            Assert.Throws<ArgumentOutOfRangeException>(() => server.Poll(now));
            Assert.Throws<ArgumentOutOfRangeException>(() => client.Poll(now));
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.Preparing));
            Assert.That(client.Phase, Is.EqualTo(SessionAttemptPhase.Preparing));
            Assert.That(server.AdmittedCount, Is.Zero);
            Assert.That(server.Failure, Is.Null);
            Assert.That(client.Failure, Is.Null);
            Assert.That(server.CompleteLocalPreparation(100), Is.True);
            Assert.That(client.CompleteLocalPreparation(100), Is.True);
        }

        [Test]
        public void FourFrozenParticipantsAndServerPreparationAreRequiredForOneAuthorization()
        {
            var server = Server(new[] { "a", "b", "c", "d" });
            for (var i = 0; i < 4; i++)
            {
                var peer = Peer((ulong)i);
                Admit(server, server.Participants[i], peer);
                Assert.That(server.NotifyConnected(peer, 100), Is.True);
                Assert.That(server.ReceiveReady(Context(), peer, 100), Is.True);
                Assert.That(server.TryAuthorizeStart(100, out _), Is.False);
            }
            Assert.That(server.ReadyCount, Is.EqualTo(4));
            Assert.That(server.CompleteLocalPreparation(100), Is.True);
            Assert.That(server.TryAuthorizeStart(100, out var signal), Is.True);
            Assert.That(signal.Context.Matches(Context()), Is.True);
            Assert.That(signal.Sequence, Is.EqualTo(1ul));
            Assert.That(server.AuthorizationSequence, Is.EqualTo(1ul));
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.StartAuthorized));
            Assert.That(server.TryAuthorizeStart(101, out var duplicate), Is.False);
            Assert.That(duplicate.Sequence, Is.Zero);
        }

        [TestCase(0ul)]
        [TestCase(25ul)]
        public void ParticipantIdsDoNotImplyHostOrDedicatedServerReadiness(ulong clientId)
        {
            var server = Server(new[] { "a" });
            Admit(server, "a", Peer(clientId));
            Assert.That(server.NotifyConnected(Peer(clientId), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(clientId), 100), Is.True);
            Assert.That(server.TryAuthorizeStart(100, out _), Is.False);
            Assert.That(server.CompleteLocalPreparation(100), Is.True);
            Assert.That(server.TryAuthorizeStart(100, out _), Is.True);
            Assert.That(server.Participants.Count, Is.EqualTo(1));
        }

        [Test]
        public void AdmissionConnectionAndReadyAreIndependentFacts()
        {
            var server = Server(new[] { "a" });
            Admit(server, "a", Peer());
            Assert.That(server.AdmittedCount, Is.EqualTo(1));
            Assert.That(server.ConnectedCount, Is.Zero);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.False);
            Assert.That(server.ReadyCount, Is.Zero);
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(server.NotifyConnected(Peer(), 100), Is.False);
            Assert.That(server.ConnectedCount, Is.EqualTo(1));
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.True);
        }

        [TestCase(0, (int)SessionAdmissionResult.UnknownParticipant)]
        [TestCase(1, (int)SessionAdmissionResult.ContextMismatch)]
        [TestCase(2, (int)SessionAdmissionResult.DuplicateParticipant)]
        [TestCase(3, (int)SessionAdmissionResult.DuplicateClient)]
        [TestCase(4, (int)SessionAdmissionResult.InvalidConnection)]
        public void AdmissionRejectionPreservesExistingMappingAndReady(int invalid, int expected)
        {
            var server = Server();
            Admit(server, "a", Peer());
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.True);
            var id = invalid == 0 ? "intruder" : invalid == 2 ? "a" : "b";
            var context = invalid == 1 ? Context(1) : Context();
            var peer = invalid == 3 ? Peer(1, 2) : invalid == 4 ? Peer(2, 0) : Peer(2);
            Assert.That(server.TryAdmit(context, id, peer, 101), Is.EqualTo((SessionAdmissionResult)expected));
            Assert.That(server.AdmittedCount, Is.EqualTo(1));
            Assert.That(server.ConnectedCount, Is.EqualTo(1));
            Assert.That(server.ReadyCount, Is.EqualTo(1));
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.Preparing));
            Admit(server, "b", Peer(2), 101);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ReadyAndFailureRejectEveryMismatchedContext(int changed)
        {
            var server = Server();
            Admit(server, "a", Peer());
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(changed), Peer(), 100), Is.False);
            Assert.That(server.ReportParticipantFailure(Context(changed), Peer(), new Exception("stale"), 100), Is.False);
            Assert.That(server.ReadyCount, Is.Zero);
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.True);
        }

        [TestCase(99ul, 1ul)]
        [TestCase(1ul, 2ul)]
        [TestCase(1ul, 0ul)]
        public void UnknownOrOldConnectionCannotChangeCurrentParticipant(ulong id, ulong generation)
        {
            var server = Server();
            Admit(server, "a", Peer());
            Assert.That(server.NotifyConnected(Peer(id, generation), 100), Is.False);
            Assert.That(server.Disconnect(Peer(id, generation), 100), Is.False);
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(id, generation), 100), Is.False);
            Assert.That(server.ReportParticipantFailure(Context(), Peer(id, generation), new Exception("wrong-peer"), 100), Is.False);
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.True);
        }

        [Test]
        public void DuplicateReadyCannotFillAnotherSeat()
        {
            var server = Server();
            Admit(server, "a", Peer());
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(), 100), Is.False);
            Assert.That(server.ReadyCount, Is.EqualTo(1));
            Assert.That(server.CompleteLocalPreparation(100), Is.True);
            Assert.That(server.CompleteLocalPreparation(100), Is.False);
            Assert.That(server.TryAuthorizeStart(100, out _), Is.False);
            Admit(server, "b", Peer(2));
            Assert.That(server.NotifyConnected(Peer(2), 100), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(2), 100), Is.True);
            Assert.That(server.TryAuthorizeStart(100, out _), Is.True);
        }

        [Test]
        public void DisconnectBeforeAuthorizationInvalidatesReadyAndPreservesRoster()
        {
            var server = ReadyServer();
            Assert.That(server.Disconnect(Peer(1), 101), Is.True);
            Assert.That(server.Failure.Code, Is.EqualTo(SessionFailureCode.ParticipantDisconnected));
            Assert.That(server.Failure.ParticipantId, Is.EqualTo("b"));
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.Aborted));
            Assert.That(server.TryAuthorizeStart(101, out _), Is.False);
            Assert.That(server.ReceiveReady(Context(), Peer(1), 101), Is.False);
            Assert.That(server.TryAdmit(Context(), "b", Peer(2), 101), Is.EqualTo(SessionAdmissionResult.InactiveAttempt));
            Assert.That(server.Participants, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void AuthorizationBeforeDisconnectLeavesGameplayPolicyToTheOwner()
        {
            var server = ReadyServer();
            Assert.That(server.TryAuthorizeStart(101, out _), Is.True);
            Assert.That(server.Disconnect(Peer(1), 101), Is.False);
            Assert.That(server.Cancel(101), Is.False);
            server.Poll(130);
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.StartAuthorized));
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.AuthorizationSequence, Is.EqualTo(1ul));
        }

        [TestCase(0, (int)SessionFailureCode.OwnerCancelled)]
        [TestCase(1, (int)SessionFailureCode.LocalPreparationFailed)]
        [TestCase(2, (int)SessionFailureCode.EndpointFailed)]
        public void FirstLocalFailureWinsAndLateInputCannotReviveTheAttempt(int source, int code)
        {
            var server = ReadyServer();
            var cause = new InvalidOperationException("first");
            var accepted = source == 0 ? server.Cancel(101) : source == 1
                ? server.ReportLocalFailure(cause, 101) : server.FailEndpoint(cause, 101);
            Assert.That(accepted, Is.True);
            var first = server.Failure;
            Assert.That(first.Code, Is.EqualTo((SessionFailureCode)code));
            Assert.That(first.Cause, Is.SameAs(source == 0 ? null : cause));
            Assert.That(server.FailEndpoint(new Exception("late"), 102), Is.False);
            Assert.That(server.NotifyConnected(Peer(1), 102), Is.False);
            Assert.That(server.ReceiveReady(Context(), Peer(1), 102), Is.False);
            Assert.That(server.CompleteLocalPreparation(102), Is.False);
            Assert.That(server.TryAuthorizeStart(102, out _), Is.False);
            server.Poll(130);
            Assert.That(server.Failure, Is.SameAs(first));
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.Aborted));
        }

        [Test]
        public void AuthenticatedParticipantFailurePreservesExactCause()
        {
            var server = Server();
            Admit(server, "a", Peer());
            Assert.That(server.NotifyConnected(Peer(), 100), Is.True);
            var cause = new InvalidOperationException("prepare");
            Assert.That(server.ReportParticipantFailure(Context(), Peer(), cause, 101), Is.True);
            Assert.That(server.Failure.Cause, Is.SameAs(cause));
            Assert.That(server.Failure.ParticipantId, Is.EqualTo("a"));
            Assert.That(server.Failure.Code, Is.EqualTo(SessionFailureCode.ParticipantPreparationFailed));
            Assert.That(server.Failure.RemoteReasonObserved, Is.False);
        }

        [TestCase(119.999d, true)]
        [TestCase(120d, false)]
        public void DeadlineIsRecheckedAtTheAuthorizationBoundary(double now, bool expected)
        {
            var server = ReadyServer();
            Assert.That(server.TryAuthorizeStart(now, out _), Is.EqualTo(expected));
            Assert.That(server.Phase, Is.EqualTo(expected ? SessionAttemptPhase.StartAuthorized : SessionAttemptPhase.Aborted));
            if (!expected) Assert.That(server.Failure.Code, Is.EqualTo(SessionFailureCode.DeadlineExceeded));
        }

        [Test]
        public void AdmissionAndPartialReadinessDoNotExtendTheFrozenBudget()
        {
            var server = Server();
            Admit(server, "a", Peer(), 110);
            Assert.That(server.NotifyConnected(Peer(), 111), Is.True);
            Assert.That(server.ReceiveReady(Context(), Peer(), 119), Is.True);
            server.Poll(120);
            Assert.That(server.Phase, Is.EqualTo(SessionAttemptPhase.Aborted));
            Assert.That(server.Failure.Code, Is.EqualTo(SessionFailureCode.DeadlineExceeded));
            Assert.That(server.Participants.Count, Is.EqualTo(2));
        }

        [TestCase(99ul, 1ul)]
        [TestCase(0ul, 2ul)]
        public void ClientAcceptsOnlyItsPinnedServerConnection(ulong id, ulong generation)
        {
            var client = Client();
            Assert.That(client.CompleteLocalPreparation(100), Is.True);
            Assert.That(client.ReceiveStart(Peer(id, generation), new SessionStartSignal(Context(), 1), 101), Is.False);
            Assert.That(client.ReceiveAbort(Peer(id, generation), Context(), SessionFailureCode.OwnerCancelled, 101), Is.False);
            Assert.That(client.DisconnectServer(Peer(id, generation), 101), Is.False);
            Assert.That(client.Phase, Is.EqualTo(SessionAttemptPhase.Preparing));
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(), 1), 101), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ClientRejectsEveryMismatchedStartAndAbortContext(int changed)
        {
            var client = Client();
            Assert.That(client.CompleteLocalPreparation(100), Is.True);
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(changed), 1), 101), Is.False);
            Assert.That(client.ReceiveAbort(Peer(0), Context(changed), SessionFailureCode.OwnerCancelled, 101), Is.False);
            Assert.That(client.Phase, Is.EqualTo(SessionAttemptPhase.Preparing));
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(), 1), 101), Is.True);
        }

        [Test]
        public void ClientRequiresLocalReadinessAndAcceptsOneSequenceOnly()
        {
            var client = Client();
            var signal = new SessionStartSignal(Context(), 1);
            Assert.That(client.ReceiveStart(Peer(0), signal, 100), Is.False);
            Assert.That(client.CompleteLocalPreparation(100), Is.True);
            Assert.That(client.CompleteLocalPreparation(100), Is.False);
            Assert.That(client.ReceiveStart(Peer(0), default, 100), Is.False);
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(), 0), 100), Is.False);
            Assert.That(client.ReceiveStart(Peer(0), signal, 100), Is.True);
            Assert.That(client.ReceiveStart(Peer(0), signal, 101), Is.False);
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(), 2), 101), Is.False);
            Assert.That(client.ReceiveAbort(Peer(0), Context(), SessionFailureCode.OwnerCancelled, 101), Is.False);
            Assert.That(client.DisconnectServer(Peer(0), 101), Is.False);
            Assert.That(client.AuthorizationSequence, Is.EqualTo(1ul));
            Assert.That(client.Phase, Is.EqualTo(SessionAttemptPhase.StartAuthorized));
        }

        [TestCase(0, (int)SessionFailureCode.OwnerCancelled)]
        [TestCase(1, (int)SessionFailureCode.LocalPreparationFailed)]
        [TestCase(2, (int)SessionFailureCode.DeadlineExceeded)]
        [TestCase(3, (int)SessionFailureCode.ParticipantDisconnected)]
        public void ClientFailureKeepsGameplayClosedAndPreservesFirstReason(int source, int expected)
        {
            var client = Client();
            var cause = new Exception("local");
            Assert.That(client.CompleteLocalPreparation(100), Is.True);
            if (source == 0) Assert.That(client.Cancel(101), Is.True);
            else if (source == 1) Assert.That(client.ReportLocalFailure(cause, 101), Is.True);
            else if (source == 2) client.Poll(120);
            else Assert.That(client.DisconnectServer(Peer(0), 101), Is.True);
            var first = client.Failure;
            Assert.That(first, Is.Not.Null);
            Assert.That(first.Code, Is.EqualTo((SessionFailureCode)expected));
            Assert.That(first.RemoteReasonObserved, Is.False);
            Assert.That(first.Cause, Is.SameAs(source == 1 ? cause : null));
            Assert.That(client.ReceiveStart(Peer(0), new SessionStartSignal(Context(), 1), 120), Is.False);
            Assert.That(client.ReceiveAbort(Peer(0), Context(), SessionFailureCode.EndpointFailed, 120), Is.False);
            Assert.That(client.Failure, Is.SameAs(first));
            Assert.That(client.Phase, Is.EqualTo(SessionAttemptPhase.Aborted));
        }

        [Test]
        public void ClientAcceptsNormalizedServerAbortWithoutRemoteExceptionText()
        {
            var client = Client();
            Assert.That(client.ReceiveAbort(Peer(0), Context(), (SessionFailureCode)999, 100), Is.False);
            Assert.That(client.Failure, Is.Null);
            Assert.That(client.ReceiveAbort(Peer(0), Context(), SessionFailureCode.ParticipantPreparationFailed, 100), Is.True);
            Assert.That(client.Failure.RemoteReasonObserved, Is.True);
            Assert.That(client.Failure.Cause, Is.Null);
            Assert.That(client.Failure.ParticipantId, Is.Null);
            Assert.That(client.Failure.Code, Is.EqualTo(SessionFailureCode.ParticipantPreparationFailed));
        }

        [Test]
        public void NewAttemptCannotAcceptTheOldAttemptMessages()
        {
            var next = new ServerSessionProtocol(Context(1), new[] { "a" }, 100, 20);
            Assert.That(next.TryAdmit(Context(), "a", Peer(), 100), Is.EqualTo(SessionAdmissionResult.ContextMismatch));
            Assert.That(next.TryAdmit(Context(1), "a", Peer(), 100), Is.EqualTo(SessionAdmissionResult.Accepted));
            Assert.That(next.NotifyConnected(Peer(), 100), Is.True);
            Assert.That(next.ReceiveReady(Context(), Peer(), 100), Is.False);
            Assert.That(next.ReportParticipantFailure(Context(), Peer(), new Exception("old"), 100), Is.False);
            Assert.That(next.ReceiveReady(Context(1), Peer(), 100), Is.True);
            Assert.That(next.CompleteLocalPreparation(100), Is.True);
            Assert.That(next.TryAuthorizeStart(100, out var signal), Is.True);
            Assert.That(signal.Context.Matches(Context(1)), Is.True);
        }
    }
}
