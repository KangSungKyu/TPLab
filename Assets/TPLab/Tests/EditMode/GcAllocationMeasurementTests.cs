using System;
using NUnit.Framework;
using Unity.Profiling;

namespace TPLab.Core.Tests
{
    public sealed class GcAllocationMeasurementTests
    {
        private static byte[] _payload;

        [Test]
        public void NegativeWorkloadHasZeroEventsWithValidControls()
        {
            GcAllocationControls.DoNotAllocate();
            var result = GcAllocationMeasurement.Measure(GcAllocationControls.DoNotAllocate);
            Assert.That(result.EventCount, Is.Zero);
            Assert.That(result.PositiveControlEventCount, Is.GreaterThan(0));
            Assert.That(result.NegativeControlEventCount, Is.Zero);
        }

        [Test]
        public void RetainedPositiveWorkloadIsDetected()
        {
            var result = GcAllocationMeasurement.Measure(GcAllocationControls.Allocate);
            Assert.That(result.EventCount, Is.GreaterThan(0));
        }

        [Test]
        [Timeout(30000)]
        public void RepeatedControlsAreStableAcross100Measurements()
        {
            GcAllocationControls.Repeat(100);
        }

        [Test]
        public void UnavailableRecorderFailsBeforeWorkloadRuns()
        {
            bool ran = false;
            var error = Assert.Throws<AssertionException>(() =>
                GcAllocationMeasurement.Measure(() => ran = true, "TPLab.Nonexistent.GC.Marker", 4096));
            Assert.That(error.Message, Does.Contain("unavailable"));
            Assert.That(ran, Is.False);
        }

        [Test]
        public void MissingPositiveControlFailsBeforeWorkloadRuns()
        {
            var marker = new ProfilerMarker(ProfilerCategory.Memory, "TPLab.Calibration.NoAlloc");
            marker.Begin();
            marker.End();
            bool ran = false;
            var error = Assert.Throws<AssertionException>(() =>
                GcAllocationMeasurement.Measure(() => ran = true, "TPLab.Calibration.NoAlloc", 4096));
            Assert.That(error.Message, Does.Contain("Positive control allocation was not detected"));
            Assert.That(ran, Is.False);
        }

        [Test]
        public void WorkloadExceptionIsPreservedAndNextMeasurementWorks()
        {
            var expected = new InvalidOperationException("workload");
            var actual = Assert.Throws<InvalidOperationException>(() =>
                GcAllocationMeasurement.Measure(() => throw expected));
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(GcAllocationMeasurement.Measure(GcAllocationControls.DoNotAllocate).EventCount, Is.Zero);
        }

        [Test]
        public void RecorderCapacityExhaustionFailsAndNextMeasurementWorks()
        {
            var error = Assert.Throws<AssertionException>(() => GcAllocationMeasurement.Measure(() =>
            {
                for (int i = 0; i < 16; i++) _payload = new byte[4096];
            }, "GC.Alloc", 2));
            Assert.That(error.Message, Does.Contain("capacity"));
            Assert.That(GcAllocationMeasurement.Measure(GcAllocationControls.DoNotAllocate).EventCount, Is.Zero);
            _payload = null;
        }

        [Test]
        [Timeout(30000)]
        public void WorkerAllocationsAreExcludedFromCurrentThreadEvents()
        {
            GcAllocationControls.CheckWorkerIsolation();
        }

        [Test]
        public void NullWorkloadIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => GcAllocationMeasurement.Measure(null));
        }
    }
}
