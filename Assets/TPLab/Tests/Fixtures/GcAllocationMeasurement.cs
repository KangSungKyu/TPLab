using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Profiling;

[assembly: InternalsVisibleTo("TPLab.Core.EditModeTests")]
[assembly: InternalsVisibleTo("TPLab.Core.PlayModeTests")]

namespace TPLab.Core.Tests
{
    /// <summary>Test-only, synchronous allocation event counts and calibration results.</summary>
    public readonly struct GcAllocationMeasurementResult
    {
        /// <summary>Sum of current-thread GC.Alloc sample counts, not allocation bytes.</summary>
        public long EventCount { get; }
        public long PositiveControlEventCount { get; }
        public long NegativeControlEventCount { get; }
        /// <summary>Whether the global GC Allocated In Frame diagnostic recorder is supported.</summary>
        public bool FrameBytesAvailable { get; }
        /// <summary>Global frame counter CurrentValue delta around the workload; never a pass/fail substitute.</summary>
        public long FrameByteDelta { get; }

        internal GcAllocationMeasurementResult(long eventCount, long positiveControlEventCount,
            long negativeControlEventCount, bool frameBytesAvailable, long frameByteDelta)
        {
            EventCount = eventCount;
            PositiveControlEventCount = positiveControlEventCount;
            NegativeControlEventCount = negativeControlEventCount;
            FrameBytesAvailable = frameBytesAvailable;
            FrameByteDelta = frameByteDelta;
        }
    }

    /// <summary>Test-only allocation measurement; invalid calibration must fail, never report zero.</summary>
    public static class GcAllocationMeasurement
    {
        private static byte[] _positiveControl;
        private static readonly Action PositiveControl = AllocatePositiveControl;
        private static readonly Action NegativeControl = ClearPositiveControl;

        /// <summary>Measures one warmed synchronous workload, with positive and negative controls.</summary>
        /// <param name="workload">Work to invoke exactly once; caller owns warm-up and resource cleanup.</param>
        /// <returns>Workload events and separate calibration counts; frame bytes are diagnostic only.</returns>
        /// <exception cref="ArgumentNullException">The workload is null.</exception>
        /// <exception cref="AssertionException">The event recorder is unavailable, controls are invalid, or capacity is exhausted.</exception>
        /// <remarks>
        /// Caller workload warm-up is never performed here. Only controls and recorder instrumentation are warmed.
        /// Each call calibrates separate current-thread event ranges before invoking workload once.
        /// Workload exceptions propagate unchanged. Recorders and the retained control payload are released on all exits.
        /// Synchronous test use only; nested or concurrent calls are not supported because calibration retains a static payload.
        /// </remarks>
        public static GcAllocationMeasurementResult Measure(Action workload)
        {
            return Measure(workload, "GC.Alloc", 4096);
        }

        // Small test seam for unavailable markers and recorder capacity exhaustion.
        internal static GcAllocationMeasurementResult Measure(Action workload, string markerName, int capacity)
        {
            if (workload == null)
            {
                throw new ArgumentNullException(nameof(workload));
            }
            if (capacity <= 0)
            {
                throw new AssertionException("Allocation recorder capacity must be positive.");
            }

            ProfilerRecorder events = default;
            ProfilerRecorder frameBytes = default;
            bool failed = false;
            try
            {
                try
                {
                    events = new ProfilerRecorder(ProfilerCategory.Memory, markerName, capacity,
                        ProfilerRecorderOptions.CollectOnlyOnCurrentThread |
                        ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                }
                catch (Exception error)
                {
                    throw new AssertionException("Allocation recorder unavailable: " + error.Message);
                }
                if (!events.Valid)
                {
                    throw new AssertionException("Allocation recorder unavailable.");
                }
                frameBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1,
                    ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                bool frameBytesAvailable = frameBytes.Valid;
                if (frameBytesAvailable)
                {
                    _ = frameBytes.CurrentValue;
                }

                // Warm only the controls and the exact recorder instrumentation, never the caller workload.
                Capture(ref events, PositiveControl);
                Capture(ref events, NegativeControl);
                long negative = Capture(ref events, NegativeControl);
                long positive = Capture(ref events, PositiveControl);
                if (negative != 0)
                {
                    throw new AssertionException("Negative control allocation was detected.");
                }
                if (positive == 0)
                {
                    throw new AssertionException("Positive control allocation was not detected.");
                }

                long before = frameBytesAvailable ? frameBytes.CurrentValue : 0;
                long eventCount = Capture(ref events, workload);
                long frameByteDelta = frameBytesAvailable ? frameBytes.CurrentValue - before : 0;
                return new GcAllocationMeasurementResult(eventCount, positive, negative,
                    frameBytesAvailable, frameByteDelta);
            }
            catch
            {
                failed = true;
                throw;
            }
            finally
            {
                _positiveControl = null;
                // Native cleanup errors must not replace the original workload/calibration exception.
                try
                {
                    Release(ref events, failed);
                }
                finally
                {
                    Release(ref frameBytes, failed);
                }
            }
        }

        private static long Capture(ref ProfilerRecorder recorder, Action workload)
        {
            recorder.Reset();
            recorder.Start();
            bool failed = false;
            try
            {
                workload();
            }
            catch
            {
                failed = true;
                throw;
            }
            finally
            {
                try
                {
                    recorder.Stop();
                }
                catch when (failed)
                {
                }
            }
            if (recorder.WrappedAround || recorder.Count >= recorder.Capacity)
            {
                throw new AssertionException("Allocation recorder capacity was exhausted.");
            }
            long eventCount = 0;
            for (int i = 0; i < recorder.Count; i++)
            {
                eventCount += recorder.GetSample(i).Count;
            }
            return eventCount;
        }

        private static void Release(ref ProfilerRecorder recorder, bool preserveFailure)
        {
            try
            {
                try
                {
                    if (recorder.Valid)
                    {
                        recorder.Stop();
                    }
                }
                finally
                {
                    recorder.Dispose();
                }
            }
            catch when (preserveFailure)
            {
            }
        }

        private static void AllocatePositiveControl()
        {
            _positiveControl = new byte[4096];
            _positiveControl[0] = 0x5a;
        }

        private static void ClearPositiveControl()
        {
            _positiveControl = null;
        }
    }
}
