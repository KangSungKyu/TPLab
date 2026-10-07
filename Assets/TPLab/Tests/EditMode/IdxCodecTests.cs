using System;
using TPLab.Core.DataTables;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public sealed class IdxCodecTests
    {
        [TestCase(0u)]
        [TestCase(1u)]
        public void RejectsInvalidStride(uint stride)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalIdxCodec(stride));

        [TestCase(0u, 1u)]
        [TestCase(1u, 0u)]
        [TestCase(1u, 1000u)]
        public void RejectsInvalidParts(uint kind, uint local)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalIdxCodec(1000).Generate(new IdxParts(kind, local)));

        [TestCase(8u, 248u, 8248u)]
        [TestCase(1u, 999u, 1999u)]
        [TestCase(4294967u, 295u, uint.MaxValue)]
        public void RoundTripsAndRoutesCompleteIdx(uint kind, uint local, uint idx)
        {
            var codec = new DecimalIdxCodec(1000);
            Assert.That(codec.Stride, Is.EqualTo(1000));
            Assert.That(codec.Generate(new IdxParts(kind, local)), Is.EqualTo(idx));
            Assert.That(codec.TryExtract(idx, out var parts), Is.True);
            Assert.That(parts.DataType, Is.EqualTo(kind));
            Assert.That(parts.LocalIdx, Is.EqualTo(local));
            Assert.That(codec.TryGetDataType(idx, out uint extracted), Is.True);
            Assert.That(extracted, Is.EqualTo(kind));
            Assert.That(codec.Generate(parts), Is.EqualTo(idx));
        }

        [TestCase(0u)]
        [TestCase(999u)]
        [TestCase(1000u)]
        public void InvalidLowerOrUpperPartResetsOutputs(uint idx)
        {
            var codec = new DecimalIdxCodec(1000);
            Assert.That(codec.TryExtract(idx, out var parts), Is.False);
            Assert.That(parts.DataType, Is.Zero);
            Assert.That(parts.LocalIdx, Is.Zero);
            Assert.That(codec.TryGetDataType(idx, out uint kind), Is.False);
            Assert.That(kind, Is.Zero);
        }

        [Test]
        public void OverflowNeverWrapsAndStrideIsProjectSelected()
        {
            Assert.Throws<OverflowException>(() => new DecimalIdxCodec(1000).Generate(new IdxParts(4294967, 296)));
            Assert.Throws<OverflowException>(() => new DecimalIdxCodec(1000).Generate(new IdxParts(uint.MaxValue, 1)));
            Assert.That(new DecimalIdxCodec(10000).Generate(new IdxParts(2, 37)), Is.EqualTo(20037));
        }

        // Test-only project format: optional worker/category component stays inside one table kind.
        public readonly struct ProjectParts
        {
            public ProjectParts(uint kind, uint category, uint local)
            {
                Kind = kind;
                Category = category;
                Local = local;
            }
            public uint Kind { get; }
            public uint Category { get; }
            public uint Local { get; }
        }

        public sealed class ProjectCodec : IIdxCodec<ProjectParts>
        {
            public uint Generate(ProjectParts parts)
            {
                if (parts.Kind == 0 || parts.Category >= 100 || parts.Local == 0 || parts.Local >= 10000)
                    throw new ArgumentOutOfRangeException(nameof(parts));
                return checked((parts.Kind * 100 + parts.Category) * 10000 + parts.Local);
            }
            public bool TryExtract(uint idx, out ProjectParts parts)
            {
                parts = default;
                if (idx / 1000000 == 0 || idx % 10000 == 0) return false;
                parts = new ProjectParts(idx / 1000000, idx / 10000 % 100, idx % 10000);
                return true;
            }
            public bool TryGetDataType(uint idx, out uint dataType)
            {
                bool valid = TryExtract(idx, out var parts);
                dataType = parts.Kind;
                return valid;
            }
        }

        [Test]
        public void ProjectCodecSupportsOptionalThirdPartWithoutChangingKind()
        {
            var codec = new ProjectCodec();
            foreach (uint category in new[] { 0u, 7u, 8u, 99u })
            {
                var original = new ProjectParts(2, category, 37);
                uint idx = codec.Generate(original);
                Assert.That(codec.TryExtract(idx, out var parts), Is.True);
                Assert.That(parts.Category, Is.EqualTo(category));
                Assert.That(parts.Local, Is.EqualTo(37));
                Assert.That(codec.Generate(parts), Is.EqualTo(idx));
                Assert.That(codec.TryGetDataType(idx, out uint kind), Is.True);
                Assert.That(kind, Is.EqualTo(2));
            }
            Assert.That(codec.Generate(new ProjectParts(2, 7, 37)), Is.EqualTo(2070037));
            Assert.That(codec.Generate(new ProjectParts(2, 8, 37)), Is.EqualTo(2080037));
            Assert.That(codec.TryGetDataType(2070000, out uint invalid), Is.False);
            Assert.That(invalid, Is.Zero);
            Assert.That(codec.TryExtract(999999, out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => codec.Generate(new ProjectParts(2, 100, 1)));
            Assert.Throws<OverflowException>(() => codec.Generate(new ProjectParts(uint.MaxValue, 0, 1)));
        }
    }
}
