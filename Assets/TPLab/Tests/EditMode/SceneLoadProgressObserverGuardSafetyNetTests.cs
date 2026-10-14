using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TPLab.Core.ResourceManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public sealed class SceneLoadProgressObserverGuardSafetyNetTests
    {
        private const string ThreadMessage = "Scene resources must be used on Unity's main thread.";

        [UnityTest, Timeout(30000)]
        public IEnumerator Report_WorkerThreadPrecedesNotificationSuppression()
        {
            var first = new SceneLoadProgress(SceneLoadStage.LoadingScene, 0.25f);
            var second = new SceneLoadProgress(SceneLoadStage.LoadingScene, 0.5f);
            var reports = new List<SceneLoadProgress>();
            using (var observer = new SceneLoadProgressObserver(progress => reports.Add(progress)))
            {
                observer.Report(first);
                AssertWorkerRejected(observer, second);
                Assert.That(reports, Is.EqualTo(new[] { first }));
                Assert.That(observer.Failure, Is.Null);
                yield return null;
                Assert.That(reports, Is.EqualTo(new[] { first }));
                observer.Report(second);
                Assert.That(reports, Is.EqualTo(new[] { first, second }));
                observer.Dispose();
                AssertWorkerRejected(observer, second);
                Assert.That(observer.Failure, Is.Null);
                observer.Report(second);
                Assert.That(reports, Is.EqualTo(new[] { first, second }));
            }
            int failureCalls = 0;
            var sentinel = new InvalidOperationException("Observer guard sentinel");
            using (var failed = new SceneLoadProgressObserver(_ =>
            {
                ++failureCalls;
                throw sentinel;
            }))
            {
                failed.Report(first);
                Assert.That(failed.Failure, Is.SameAs(sentinel));
                AssertWorkerRejected(failed, second);
                Assert.That(failed.Failure, Is.SameAs(sentinel));
                Assert.That(failureCalls, Is.EqualTo(1));
                failed.Report(second);
                Assert.That(failureCalls, Is.EqualTo(1));
                failed.Dispose();
                AssertWorkerRejected(failed, second);
                Assert.That(failed.Failure, Is.SameAs(sentinel));
                Assert.That(failureCalls, Is.EqualTo(1));
            }
        }

        private static void AssertWorkerRejected(SceneLoadProgressObserver observer, SceneLoadProgress progress)
        {
            Exception failure = null;
            var worker = new Thread(() =>
            {
                try
                {
                    observer.Report(progress);
                }
                catch (Exception error)
                {
                    failure = error;
                }
            }) { IsBackground = true };
            worker.Start();
            Assert.That(worker.Join(3000), Is.True, "Worker did not join within 3000ms.");
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Is.EqualTo(ThreadMessage));
        }
    }
}
