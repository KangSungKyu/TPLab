using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TPLab.Core.ResourceManagement;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public sealed class SceneLoadProgressTests
    {
        [TestCase(SceneLoadStage.ResolvingTarget, 0f)]
        [TestCase(SceneLoadStage.LoadingScene, 1f)]
        public void SnapshotPreservesStageAndBoundaryRatio(SceneLoadStage stage, float ratio)
        {
            var progress = new SceneLoadProgress(stage, ratio);
            Assert.That(progress.Stage, Is.EqualTo(stage));
            Assert.That(progress.Ratio, Is.EqualTo(ratio));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SnapshotRejectsInvalidRatio(float ratio)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SceneLoadProgress(SceneLoadStage.LoadingScene, ratio));
        }

        [Test]
        public void SnapshotRejectsUnknownStage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SceneLoadProgress((SceneLoadStage)99, 0.5f));
        }

        [Test]
        public void ObserverRequiresACallback()
        {
            Assert.Throws<ArgumentNullException>(() => new SceneLoadProgressObserver(null));
        }

        [Test]
        public void ObserverReportsSynchronouslyOnTheCallingMainThread()
        {
            int thread = Thread.CurrentThread.ManagedThreadId;
            int observedThread = 0;
            var reported = new List<SceneLoadProgress>();
            using (var observer = new SceneLoadProgressObserver(progress =>
            {
                observedThread = Thread.CurrentThread.ManagedThreadId;
                reported.Add(progress);
            }))
            {
                observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0.5f));
                Assert.That(reported.Count, Is.EqualTo(1));
                Assert.That(reported[0].Ratio, Is.EqualTo(0.5f));
                Assert.That(observedThread, Is.EqualTo(thread));
                Assert.That(observer.Failure, Is.Null);
            }
        }

        [Test]
        public void ObserverRetainsFirstCallbackFailureAndSuppressesLaterReports()
        {
            var expected = new InvalidOperationException("expected progress observer failure");
            int calls = 0;
            using (var observer = new SceneLoadProgressObserver(_ =>
            {
                ++calls;
                throw expected;
            }))
            {
                Assert.DoesNotThrow(() => observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0f)));
                observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 1f));
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(observer.Failure, Is.SameAs(expected));
                observer.Dispose();
                Assert.That(observer.Failure, Is.SameAs(expected));
            }
        }

        [Test]
        public void ObserverDisposeFromCallbackSuppressesSubsequentNotifications()
        {
            int calls = 0;
            SceneLoadProgressObserver observer = null;
            observer = new SceneLoadProgressObserver(_ =>
            {
                ++calls;
                observer.Dispose();
            });
            using (observer)
            {
                observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0f));
                observer.Dispose();
                observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 1f));
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(observer.Failure, Is.Null);
            }
        }

        [Test]
        public void ObserverRejectsWorkerReportsWithoutCallingTheCallback()
        {
            int calls = 0;
            using (var observer = new SceneLoadProgressObserver(_ => ++calls))
            {
                Exception failure = Task.Run(() =>
                {
                    try
                    {
                        observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0.5f));
                        return (Exception)null;
                    }
                    catch (Exception error)
                    {
                        return error;
                    }
                }).GetAwaiter().GetResult();
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                Assert.That(calls, Is.Zero);
                Assert.That(observer.Failure, Is.Null);
            }
        }
    }
}
