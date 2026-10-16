using System;
using System.IO;
using System.Text;

namespace TPLab.Netcode
{
    internal enum SessionControlKind : byte { Welcome = 1, Ready = 2, Start = 3, Abort = 4 }

    internal readonly struct SessionControlMessage
    {
        internal SessionProtocolContext Context { get; }
        internal SessionControlKind Kind { get; }
        internal ulong Generation { get; }
        internal ulong Sequence { get; }
        internal SessionFailureCode FailureCode { get; }
        internal double RemainingSeconds { get; }

        internal SessionControlMessage(SessionProtocolContext context, SessionControlKind kind,
            ulong generation, ulong sequence, SessionFailureCode failureCode, double remainingSeconds)
        {
            Context = context;
            Kind = kind;
            Generation = generation;
            Sequence = sequence;
            FailureCode = failureCode;
            RemainingSeconds = remainingSeconds;
        }
    }

    internal static class SessionWireCodec
    {
        private const uint Magic = 0x534C5054;
        private const byte FormatVersion = 1;
        private const int HeaderSize = 6;
        private const int ControlFieldsSize = 28;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static byte[] EncodeAdmission(SessionProtocolContext context, byte[] proof, int maxPayloadBytes)
        {
            ValidateLimit(maxPayloadBytes);
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            int prefixSize = checked(HeaderSize + GetIdentityPayloadSize(context) + 4);
            if (prefixSize > maxPayloadBytes || proof.Length > maxPayloadBytes - prefixSize)
                throw new ArgumentException("The admission payload exceeds the configured limit.", nameof(proof));

            var payload = new byte[prefixSize + proof.Length];
            using (var stream = new MemoryStream(payload, true))
            using (var writer = new BinaryWriter(stream, Utf8))
            {
                WritePrefix(writer, context, 0);
                writer.Write(proof.Length);
                writer.Write(proof);
            }
            return payload;
        }

        internal static bool TryDecodeAdmission(byte[] payload, int maxPayloadBytes,
            out SessionProtocolContext context, out byte[] proof)
        {
            ValidateLimit(maxPayloadBytes);
            context = null;
            proof = null;
            if (payload == null || payload.Length > maxPayloadBytes
                || !TryReadPrefix(payload, 0, out var decoded, out int offset)
                || payload.Length - offset < 4)
                return false;

            int proofLength = unchecked((int)ReadUInt32(payload, ref offset));
            if (proofLength != payload.Length - offset)
                return false;

            var snapshot = new byte[proofLength];
            Buffer.BlockCopy(payload, offset, snapshot, 0, proofLength);
            context = decoded;
            proof = snapshot;
            return true;
        }

        /// <summary>Uses trusted expected identity; invalid UTF-16 and checked size overflow are configuration errors.</summary>
        internal static int GetControlPayloadSize(SessionProtocolContext context)
        {
            return checked(HeaderSize + GetIdentityPayloadSize(context) + ControlFieldsSize);
        }

        internal static byte[] EncodeControl(SessionControlMessage message)
        {
            if (message.Context == null) throw new ArgumentNullException(nameof(message.Context));
            if (!IsValidControl(message))
                throw new ArgumentException("The control message has invalid or noncanonical fields.", nameof(message));

            var payload = new byte[GetControlPayloadSize(message.Context)];
            using (var stream = new MemoryStream(payload, true))
            using (var writer = new BinaryWriter(stream, Utf8))
            {
                WritePrefix(writer, message.Context, (byte)message.Kind);
                writer.Write(message.Generation);
                writer.Write(message.Sequence);
                writer.Write((int)message.FailureCode);
                writer.Write(message.RemainingSeconds);
            }
            return payload;
        }

        internal static bool TryDecodeControl(byte[] payload, SessionProtocolContext expected,
            out SessionControlMessage message)
        {
            int expectedSize = GetControlPayloadSize(expected);
            message = default;
            if (payload == null || payload.Length != expectedSize
                || !TryReadPrefix(payload, payload[5], out var context, out int offset)
                || !expected.Matches(context) || payload.Length - offset != ControlFieldsSize)
                return false;

            var kind = (SessionControlKind)payload[5];
            ulong generation = ReadUInt64(payload, ref offset);
            ulong sequence = ReadUInt64(payload, ref offset);
            var code = (SessionFailureCode)unchecked((int)ReadUInt32(payload, ref offset));
            double remaining = BitConverter.Int64BitsToDouble(unchecked((long)ReadUInt64(payload, ref offset)));
            var decoded = new SessionControlMessage(context, kind, generation, sequence, code, remaining);
            if (!IsValidControl(decoded))
                return false;
            message = decoded;
            return true;
        }

        private static bool IsValidControl(SessionControlMessage message)
        {
            if (message.Generation == 0)
                return false;
            switch (message.Kind)
            {
                case SessionControlKind.Welcome:
                    return message.Sequence == 0 && (int)message.FailureCode == 0
                        && message.RemainingSeconds > 0 && !double.IsInfinity(message.RemainingSeconds);
                case SessionControlKind.Ready:
                    return message.Sequence == 0 && (int)message.FailureCode == 0 && message.RemainingSeconds == 0;
                case SessionControlKind.Start:
                    return message.Sequence > 0 && (int)message.FailureCode == 0 && message.RemainingSeconds == 0;
                case SessionControlKind.Abort:
                    return message.Sequence == 0 && message.RemainingSeconds == 0
                        && Enum.IsDefined(typeof(SessionFailureCode), message.FailureCode);
                default:
                    return false;
            }
        }

        private static void ValidateLimit(int maxPayloadBytes)
        {
            if (maxPayloadBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));
        }

        private static int GetIdentityPayloadSize(SessionProtocolContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return checked(24 + Utf8.GetByteCount(context.SessionId)
                + Utf8.GetByteCount(context.AttemptId) + Utf8.GetByteCount(context.SceneKey));
        }

        private static void WritePrefix(BinaryWriter writer, SessionProtocolContext context, byte kind)
        {
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(kind);
            WriteString(writer, context.SessionId);
            WriteString(writer, context.AttemptId);
            WriteString(writer, context.SceneKey);
            writer.Write(context.RosterRevision);
            writer.Write(context.ProtocolVersion);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static bool TryReadPrefix(byte[] payload, byte expectedKind,
            out SessionProtocolContext context, out int offset)
        {
            context = null;
            offset = 0;
            if (payload.Length < HeaderSize || ReadUInt32(payload, ref offset) != Magic
                || payload[offset++] != FormatVersion || payload[offset++] != expectedKind)
                return false;
            try
            {
                if (!TryReadString(payload, ref offset, out string sessionId)
                    || !TryReadString(payload, ref offset, out string attemptId)
                    || !TryReadString(payload, ref offset, out string sceneKey)
                    || payload.Length - offset < 12)
                    return false;
                ulong revision = ReadUInt64(payload, ref offset);
                uint protocolVersion = ReadUInt32(payload, ref offset);
                context = new SessionProtocolContext(sessionId, attemptId, revision, protocolVersion, sceneKey);
                return true;
            }
            catch (ArgumentException)
            {
                // Invalid UTF-8 and blank identities are malformed input, not protocol events.
                return false;
            }
        }

        private static bool TryReadString(byte[] payload, ref int offset, out string value)
        {
            value = null;
            if (payload.Length - offset < 4)
                return false;
            int length = unchecked((int)ReadUInt32(payload, ref offset));
            if (length < 0 || length > payload.Length - offset)
                return false;
            value = Utf8.GetString(payload, offset, length);
            offset += length;
            return true;
        }

        private static uint ReadUInt32(byte[] payload, ref int offset)
        {
            uint value = (uint)(payload[offset] | payload[offset + 1] << 8
                | payload[offset + 2] << 16 | payload[offset + 3] << 24);
            offset += 4;
            return value;
        }

        private static ulong ReadUInt64(byte[] payload, ref int offset)
        {
            ulong low = ReadUInt32(payload, ref offset);
            return low | (ulong)ReadUInt32(payload, ref offset) << 32;
        }
    }
}
