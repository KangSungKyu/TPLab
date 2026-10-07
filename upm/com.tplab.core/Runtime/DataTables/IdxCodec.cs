using System;

namespace TPLab.Core.DataTables
{
    /// <summary>Validates a complete project idx and extracts its table kind. Implementations must be immutable and side-effect free.</summary>
    public interface IIdxRouter
    {
        /// <summary>Returns false and zero for invalid formats. Success does not imply a registered kind or existing row.</summary>
        bool TryGetDataType(uint idx, out uint dataType);
    }

    /// <summary>Project-owned reversible encoding. Does not reserve numbers or insert rows.</summary>
    public interface IIdxCodec<TParts> : IIdxRouter
    {
        /// <summary>Encodes valid parts; throws ArgumentOutOfRangeException for invalid parts and OverflowException beyond uint.</summary>
        uint Generate(TParts parts);
        /// <summary>Restores all parts, or returns false and default for an invalid complete idx.</summary>
        bool TryExtract(uint idx, out TParts parts);
    }

    /// <summary>Default two-part values. Projects may supply their own parts and codec for additional components.</summary>
    public readonly struct IdxParts
    {
        /// <summary>Stores values; the selected codec validates their ranges when encoding.</summary>
        public IdxParts(uint dataType, uint localIdx)
        {
            DataType = dataType;
            LocalIdx = localIdx;
        }

        /// <summary>Project-assigned table kind.</summary>
        public uint DataType { get; }
        /// <summary>Number within the kind.</summary>
        public uint LocalIdx { get; }
    }

    /// <summary>Immutable encoding: kind * stride + local. Kind and local must be positive, local must be less than stride.</summary>
    public sealed class DecimalIdxCodec : IIdxCodec<IdxParts>
    {
        /// <summary>Creates a codec with a lower-part radix greater than one.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Stride is zero or one.</exception>
        public DecimalIdxCodec(uint stride)
        {
            if (stride <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stride));
            }
            Stride = stride;
        }

        /// <summary>Immutable lower-part range; does not constrain the display width of the kind.</summary>
        public uint Stride { get; }

        /// <inheritdoc />
        public uint Generate(IdxParts parts)
        {
            if (parts.DataType == 0 || parts.LocalIdx == 0 || parts.LocalIdx >= Stride)
            {
                throw new ArgumentOutOfRangeException(nameof(parts));
            }
            return checked(parts.DataType * Stride + parts.LocalIdx);
        }
        /// <inheritdoc />
        public bool TryExtract(uint idx, out IdxParts parts)
        {
            parts = default;
            uint dataType = idx / Stride;
            uint localIdx = idx % Stride;
            if (dataType == 0 || localIdx == 0)
            {
                return false;
            }
            parts = new IdxParts(dataType, localIdx);
            return true;
        }
        /// <inheritdoc />
        public bool TryGetDataType(uint idx, out uint dataType)
        {
            bool valid = TryExtract(idx, out var parts);
            dataType = parts.DataType;
            return valid;
        }
    }
}
