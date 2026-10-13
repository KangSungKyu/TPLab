using NUnit.Framework;
using Unity.Profiling;

namespace TPLab.Core.Tests
{
    public sealed class GcAllocationDiagnosticTests
    {
        [Test]
        public void FrameBytesAvailabilityMatchesNativeRecorderAndRetainedAllocation()
        {
            GcAllocationControls.Allocate();
            using (var frameBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            {
                var result = GcAllocationMeasurement.Measure(GcAllocationControls.Allocate);
                Assert.That(result.FrameBytesAvailable, Is.EqualTo(frameBytes.Valid));
                if (frameBytes.Valid) Assert.That(result.FrameByteDelta, Is.GreaterThan(0));
            }
        }

        [Test]
        public void WorkloadIsInvokedExactlyOnce()
        {
            int calls = 0;
            GcAllocationMeasurement.Measure(() => calls++);
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}
