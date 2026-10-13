using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public sealed class GcAllocationMeasurementPlayModeTests
    {
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator RepeatedControlsAreStableAcross100Measurements()
        {
            yield return null;
            GcAllocationControls.Repeat(100);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator WorkerAllocationsAreExcludedFromCurrentThreadEvents()
        {
            yield return null;
            GcAllocationControls.CheckWorkerIsolation();
        }
    }
}
