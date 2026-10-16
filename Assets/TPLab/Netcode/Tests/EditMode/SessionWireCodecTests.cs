using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace TPLab.Netcode.Tests
{
    public class SessionWireCodecTests
    {
        private const uint Magic = 0x534C5054;
        private const int PayloadLimit = 4096;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        private static SessionProtocolContext Context(int changed = -1)
        {
            return new SessionProtocolContext(changed == 0 ? "other" : "session-세션😀",
                changed == 1 ? "other" : "attempt", changed == 2 ? 9ul : 8ul,
                changed == 3 ? 2u : 1u, changed == 4 ? "other" : "arena");
        }

        private static SessionControlMessage Control(int kind)
        {
            return new SessionControlMessage(Context(), (SessionControlKind)kind, 7,
                kind == 3 ? 3ul : 0ul, kind == 4 ? SessionFailureCode.EndpointFailed : 0,
                kind == 1 ? 12.5 : 0);
        }

        // This independent writer fixes the wire format, including field order and byte order.
        private static void WritePrefix(BinaryWriter writer, int kind, int invalidIdentity = -1,
            byte[] replacement = null)
        {
            var context = Context();
            writer.Write(Magic);
            writer.Write((byte)1);
            writer.Write((byte)kind);
            var fields = new[] { context.SessionId, context.AttemptId, context.SceneKey };
            for (var index = 0; index < fields.Length; index++)
            {
                var bytes = index == invalidIdentity ? replacement : Utf8.GetBytes(fields[index]);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            writer.Write(context.RosterRevision);
            writer.Write(context.ProtocolVersion);
        }

        private static byte[] RawAdmission(byte[] proof = null, int invalidIdentity = -1, byte[] replacement = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Utf8))
            {
                WritePrefix(writer, 0, invalidIdentity, replacement);
                proof = proof ?? new byte[] { 0, 255, 128, 1 };
                writer.Write(proof.Length);
                writer.Write(proof);
                return stream.ToArray();
            }
        }

        private static byte[] RawControl(SessionControlMessage message)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Utf8))
            {
                WritePrefix(writer, (int)message.Kind);
                writer.Write(message.Generation);
                writer.Write(message.Sequence);
                writer.Write((int)message.FailureCode);
                writer.Write(message.RemainingSeconds);
                return stream.ToArray();
            }
        }

        private static void WriteInt32(byte[] bytes, int offset, int value)
        {
            for (var index = 0; index < 4; index++) bytes[offset + index] = (byte)(value >> (8 * index));
        }

        private static byte[] WithTrailingByte(byte[] source)
        {
            var result = new byte[source.Length + 1];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static void RejectAdmission(byte[] payload, int limit = PayloadLimit)
        {
            Assert.That(SessionWireCodec.TryDecodeAdmission(payload, limit, out var context, out var proof), Is.False);
            Assert.That(context, Is.Null);
            Assert.That(proof, Is.Null);
        }

        private static void RejectControl(byte[] payload)
        {
            Assert.That(SessionWireCodec.TryDecodeControl(payload, Context(), out var message), Is.False);
            Assert.That(message.Context, Is.Null);
            Assert.That(message.Generation, Is.Zero);
        }

        [Test]
        public void AdmissionMatchesFixedWireFormatAndCopiesOpaqueProof()
        {
            var proof = new byte[] { 0, 255, 128, 1 };
            var expected = RawAdmission(proof);
            var encoded = SessionWireCodec.EncodeAdmission(Context(), proof, expected.Length);
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(SessionWireCodec.TryDecodeAdmission(expected, expected.Length, out var context, out var decoded), Is.True);
            Assert.That(context.Matches(Context()), Is.True);
            Assert.That(decoded, Is.EqualTo(proof));
            decoded[0] = 88;
            Assert.That(expected[expected.Length - proof.Length], Is.Zero);
            proof[0] = 77;
            Assert.That(encoded[encoded.Length - proof.Length], Is.Zero);
        }

        [Test]
        public void EmptyProofRemainsAnExplicitVerifierInput()
        {
            var encoded = SessionWireCodec.EncodeAdmission(Context(), Array.Empty<byte>(), PayloadLimit);
            Assert.That(SessionWireCodec.TryDecodeAdmission(encoded, PayloadLimit, out var context, out var proof), Is.True);
            Assert.That(context.Matches(Context()), Is.True);
            Assert.That(proof, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void AdmissionRequiresAnExplicitPositivePayloadLimit(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SessionWireCodec.EncodeAdmission(Context(), Array.Empty<byte>(), limit));
            Assert.Throws<ArgumentOutOfRangeException>(() => SessionWireCodec.TryDecodeAdmission(null, limit, out _, out _));
        }

        [Test]
        public void AdmissionRejectsOneByteOverTheCallerLimit()
        {
            var bytes = RawAdmission();
            RejectAdmission(bytes, bytes.Length - 1);
            Assert.Throws<ArgumentException>(() => SessionWireCodec.EncodeAdmission(Context(), new byte[] { 0, 255, 128, 1 }, bytes.Length - 1));
            Assert.That(SessionWireCodec.TryDecodeAdmission(bytes, bytes.Length, out _, out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void AdmissionRejectsMalformedFramingAndResetsOutputs(int invalid)
        {
            var bytes = RawAdmission();
            if (invalid == 0) bytes[0] ^= 1;
            else if (invalid == 1) bytes[4] = 2;
            else if (invalid == 2) bytes[5] = 1;
            else if (invalid == 3) WriteInt32(bytes, 6, -1);
            else if (invalid == 4) WriteInt32(bytes, 6, int.MaxValue);
            else if (invalid == 5) WriteInt32(bytes, bytes.Length - 8, -1);
            else if (invalid == 6) WriteInt32(bytes, bytes.Length - 8, int.MaxValue);
            else if (invalid == 7) bytes = WithTrailingByte(bytes);
            else Array.Resize(ref bytes, bytes.Length - 1);
            RejectAdmission(bytes);
            Assert.That(SessionWireCodec.TryDecodeAdmission(RawAdmission(), PayloadLimit, out _, out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AdmissionRejectsMalformedUtf8InEveryIdentityField(int field)
        {
            RejectAdmission(RawAdmission(invalidIdentity: field, replacement: new byte[] { 0xC3, 0x28 }));
            Assert.That(SessionWireCodec.TryDecodeAdmission(RawAdmission(), PayloadLimit, out _, out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AdmissionRejectsBlankIdentityFields(int field)
        {
            RejectAdmission(RawAdmission(invalidIdentity: field, replacement: Utf8.GetBytes(" ")));
            Assert.That(SessionWireCodec.TryDecodeAdmission(RawAdmission(), PayloadLimit, out _, out _), Is.True);
        }

        [Test]
        public void NullAndTruncatedPayloadsAreRejectedWithoutExceptions()
        {
            RejectAdmission(null);
            RejectControl(null);
            var admission = RawAdmission();
            var control = RawControl(Control(1));
            for (var size = 0; size < admission.Length; size++)
            {
                var truncated = new byte[size];
                Array.Copy(admission, truncated, size);
                RejectAdmission(truncated);
            }
            for (var size = 0; size < control.Length; size++)
            {
                var truncated = new byte[size];
                Array.Copy(control, truncated, size);
                RejectControl(truncated);
            }
            Assert.That(SessionWireCodec.TryDecodeControl(control, Context(), out _), Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EachControlKindMatchesTheFixedFormatAndComputedReadBound(int kind)
        {
            var expected = Control(kind);
            var bytes = SessionWireCodec.EncodeControl(expected);
            Assert.That(bytes, Is.EqualTo(RawControl(expected)));
            Assert.That(bytes.Length, Is.EqualTo(SessionWireCodec.GetControlPayloadSize(Context())));
            Assert.That(SessionWireCodec.TryDecodeControl(bytes, Context(), out var decoded), Is.True);
            Assert.That(decoded.Context.Matches(Context()), Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(expected.Kind));
            Assert.That(decoded.Generation, Is.EqualTo(expected.Generation));
            Assert.That(decoded.Sequence, Is.EqualTo(expected.Sequence));
            Assert.That(decoded.FailureCode, Is.EqualTo(expected.FailureCode));
            Assert.That(decoded.RemainingSeconds, Is.EqualTo(expected.RemainingSeconds));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ControlsRejectEveryMismatchedIdentityDimension(int changed)
        {
            var bytes = RawControl(Control(1));
            Assert.That(SessionWireCodec.TryDecodeControl(bytes, Context(changed), out var decoded), Is.False);
            Assert.That(decoded.Context, Is.Null);
            Assert.That(SessionWireCodec.TryDecodeControl(bytes, Context(), out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ControlRejectsWrongMagicVersionKindTrailingAndLength(int invalid)
        {
            var bytes = RawControl(Control(1));
            if (invalid == 0) bytes[0] ^= 1;
            else if (invalid == 1) bytes[4] = 2;
            else if (invalid == 2) bytes[5] = 255;
            else if (invalid == 3) bytes = WithTrailingByte(bytes);
            else WriteInt32(bytes, 6, int.MaxValue);
            RejectControl(bytes);
            Assert.That(SessionWireCodec.TryDecodeControl(RawControl(Control(1)), Context(), out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        public void InvalidControlValuesAreRejectedByBothDirections(int invalid)
        {
            var kind = invalid <= 7 ? SessionControlKind.Welcome : invalid <= 10 ? SessionControlKind.Ready
                : invalid <= 13 ? SessionControlKind.Start : SessionControlKind.Abort;
            ulong generation = invalid == 0 ? 0ul : 7ul;
            ulong sequence = kind == SessionControlKind.Start ? 3ul : 0ul;
            var code = kind == SessionControlKind.Abort ? SessionFailureCode.EndpointFailed : 0;
            double remaining = kind == SessionControlKind.Welcome ? 12.5 : 0;
            if (invalid == 1) remaining = 0;
            else if (invalid == 2) remaining = -1;
            else if (invalid == 3) remaining = double.NaN;
            else if (invalid == 4) remaining = double.PositiveInfinity;
            else if (invalid == 5 || invalid == 8) sequence = 1;
            else if (invalid == 6 || invalid == 9 || invalid == 12) code = SessionFailureCode.EndpointFailed;
            else if (invalid == 7 || invalid == 10 || invalid == 13) remaining = invalid == 7 ? double.NegativeInfinity : 1;
            else if (invalid == 11) sequence = 0;
            else if (invalid == 14) code = (SessionFailureCode)999;
            var message = new SessionControlMessage(Context(), kind, generation, sequence, code, remaining);
            Assert.Throws<ArgumentException>(() => SessionWireCodec.EncodeControl(message));
            RejectControl(RawControl(message));
            Assert.That(SessionWireCodec.TryDecodeControl(RawControl(Control(1)), Context(), out _), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void AbortAcceptsEveryDefinedReasonIncludingZero(int code)
        {
            var message = new SessionControlMessage(Context(), SessionControlKind.Abort, 7, 0,
                (SessionFailureCode)code, 0);
            var bytes = SessionWireCodec.EncodeControl(message);
            Assert.That(SessionWireCodec.TryDecodeControl(bytes, Context(), out var decoded), Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(SessionControlKind.Abort));
            Assert.That(decoded.FailureCode, Is.EqualTo((SessionFailureCode)code));
        }

        [TestCase(0)]
        [TestCase(255)]
        public void UnknownControlKindsAreRejectedInBothDirections(int kind)
        {
            var message = new SessionControlMessage(Context(), (SessionControlKind)kind, 7, 0, 0, 0);
            Assert.Throws<ArgumentException>(() => SessionWireCodec.EncodeControl(message));
            RejectControl(RawControl(message));
        }
        [Test]
        public void NullConfigurationAndInvalidOutgoingUtf16FailExplicitly()
        {
            Assert.Throws<ArgumentNullException>(() => SessionWireCodec.EncodeAdmission(null, Array.Empty<byte>(), PayloadLimit));
            Assert.Throws<ArgumentNullException>(() => SessionWireCodec.EncodeAdmission(Context(), null, PayloadLimit));
            Assert.Throws<ArgumentNullException>(() => SessionWireCodec.GetControlPayloadSize(null));
            Assert.Throws<ArgumentNullException>(() => SessionWireCodec.TryDecodeControl(Array.Empty<byte>(), null, out _));
            Assert.That(() => SessionWireCodec.EncodeControl(default), Throws.InstanceOf<ArgumentException>());
            var malformed = new SessionProtocolContext("bad\uD800", "attempt", 0, 1, "scene");
            Assert.That(() => SessionWireCodec.GetControlPayloadSize(malformed), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => SessionWireCodec.EncodeAdmission(malformed, Array.Empty<byte>(), PayloadLimit), Throws.InstanceOf<ArgumentException>());
        }
    }
}
