using System;
using System.Threading;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    /// <summary>Shared EditMode/PlayMode controls; payloads escape through static fields.</summary>
    public static class GcAllocationControls
    {
        private static readonly byte[] NegativePayload = new byte[4096];
        private static byte[] _payload;
        private static byte[] _workerPayload;
        public static int PositiveDetections { get; private set; }
        public static int NegativeDetections { get; private set; }
        public static int CalibrationDetections { get; private set; }
        public static int Iterations { get; private set; }
        public static long WorkerEventCount { get; private set; }
        public static bool WorkerPayloadRetained { get; private set; }

        public static void Allocate()
        {
            _payload = new byte[4096];
            _payload[0] = 0x5a;
        }

        public static void DoNotAllocate()
        {
            _payload = NegativePayload;
        }

        /// <summary>Repeats retained positive and negative workloads with per-call calibration.</summary>
        public static void Repeat(int iterations)
        {
            PositiveDetections = NegativeDetections = CalibrationDetections = Iterations = 0;
            Allocate();
            DoNotAllocate();
            for (int i = 0; i < iterations; i++)
            {
                var positive = GcAllocationMeasurement.Measure(Allocate);
                var negative = GcAllocationMeasurement.Measure(DoNotAllocate);
                if (positive.EventCount > 0) PositiveDetections++;
                if (negative.EventCount != 0) NegativeDetections++;
                if (positive.PositiveControlEventCount > 0 && negative.PositiveControlEventCount > 0 &&
                    positive.NegativeControlEventCount == 0 && negative.NegativeControlEventCount == 0)
                    CalibrationDetections++;
                Iterations++;
            }
            Assert.That(PositiveDetections, Is.EqualTo(iterations));
            Assert.That(NegativeDetections, Is.Zero);
            Assert.That(CalibrationDetections, Is.EqualTo(iterations));
            _payload = null;
        }

        /// <summary>Checks that a retained worker-thread allocation is excluded from main-thread events.</summary>
        public static void CheckWorkerIsolation()
        {
            using (var request = new AutoResetEvent(false))
            using (var complete = new AutoResetEvent(false))
            {
                var worker = new Thread(() =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (!request.WaitOne(5000)) return;
                        _workerPayload = new byte[4096];
                        _workerPayload[0] = 0x5a;
                        complete.Set();
                    }
                });
                Action workload = () =>
                {
                    request.Set();
                    if (!complete.WaitOne(5000)) throw new TimeoutException("Worker allocation did not finish.");
                };
                worker.Start();
                try
                {
                    workload();
                    _workerPayload = null;
                    var result = GcAllocationMeasurement.Measure(workload);
                    WorkerEventCount = result.EventCount;
                    WorkerPayloadRetained = _workerPayload != null && _workerPayload.Length == 4096 &&
                        _workerPayload[0] == 0x5a;
                    Assert.That(WorkerPayloadRetained, Is.True);
                    Assert.That(WorkerEventCount, Is.Zero);
                }
                finally
                {
                    request.Set();
                    Assert.That(worker.Join(6000), Is.True, "Worker thread did not stop.");
                    _workerPayload = null;
                }
            }
        }
    }
}
