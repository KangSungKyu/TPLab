using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class AsyncSceneRootTests
    {
        private GameObject _object;
        private ISceneRoot _root;
        private SceneRootInstallerProbe _first;
        private SceneRootInstallerProbe _second;

        private void Create(SceneRootMode mode = SceneRootMode.SceneOwned, float? shutdownTimeoutSeconds = null)
        {
            _object = new GameObject("AsyncSceneTestRoot");
            _object.SetActive(false);
            _first = _object.AddComponent<SceneRootInstallerProbe>();
            _first.Id = "first";
            _second = _object.AddComponent<SceneRootInstallerProbe>();
            _second.Id = "second";
            _root = SceneRootSetup.Attach(_object, mode, new[] { _first, _second });
            if (shutdownTimeoutSeconds.HasValue)
            {
                // A missing serialized field is ignored on the legacy implementation, allowing a behavioral Red.
                string seconds = shutdownTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                JsonUtility.FromJsonOverwrite("{\"_shutdownTimeoutSeconds\":" + seconds + "}", (UnityEngine.Object)_root);
            }
            _object.SetActive(true);
            SceneRootInstallerProbe.Trace.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _first?.PrepareGate?.TrySetCanceled();
            _second?.PrepareGate?.TrySetCanceled();
            _first?.ReleaseGate?.TrySetResult();
            _second?.ReleaseGate?.TrySetResult();
            if (_object != null)
            {
                UnityEngine.Object.Destroy(_object);
            }
            yield return null;
        }

        private static UniTask Mark(string value)
        {
            SceneRootInstallerProbe.Trace.Add(value);
            return UniTask.CompletedTask;
        }

        private static async UniTask Wait(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True, "Operation did not settle.");
        }

        [UnityTest]
        public IEnumerator RevealFailureRestoresCoverBeforeReporting() => CheckRevealFailure(false).ToCoroutine();

        [UnityTest]
        public IEnumerator RevealCancellationRestoresCoverWithoutCancelledToken() => CheckRevealFailure(true).ToCoroutine();

        private async UniTask CheckRevealFailure(bool cancel)
        {
            Create();
            float opacity = 0;
            int failureCount = 0;
            using (var cancellation = new CancellationTokenSource())
            {
                var flow = new SceneRootFlow(_root,
                    token =>
                    {
                        token.ThrowIfCancellationRequested();
                        opacity = 1;
                        return Mark("cover");
                    },
                    token =>
                    {
                        opacity = 0.5f;
                        if (cancel)
                        {
                            cancellation.Cancel();
                            token.ThrowIfCancellationRequested();
                        }
                        throw new InvalidOperationException("reveal-failure");
                    },
                    error =>
                    {
                        Assert.That(opacity, Is.EqualTo(1));
                        ++failureCount;
                    });
                var operation = flow.PrepareAndProceedAsync(token => Mark("proceed"), cancellation.Token).AsTask();
                await Wait(operation);
                Assert.That(operation.IsFaulted, Is.True);
                Assert.That(failureCount, Is.EqualTo(1));
                Assert.That(opacity, Is.EqualTo(1));
                Assert.That(SceneRootInstallerProbe.Trace.FindAll(entry => entry == "cover").Count, Is.EqualTo(2));
                if (cancel)
                {
                    Assert.Throws<OperationCanceledException>(() => operation.GetAwaiter().GetResult());
                }
            }
        }

        [UnityTest]
        public IEnumerator PreparationAndRollbackFailuresAreBothPreserved() => CheckCombinedFailure().ToCoroutine();

        private async UniTask CheckCombinedFailure()
        {
            Create();
            _first.FailPrepare = true;
            _second.FailRelease = true;
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"));
            var operation = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            await Wait(operation);
            Assert.That(operation.Exception.ToString(), Does.Contain("root-prepare:first").And.Contain("root-release:second"));
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CoverAndAllPreparationAndScenePresentationAreAwaited() => CheckPreparation().ToCoroutine();

        private async UniTask CheckPreparation()
        {
            Create();
            var cover = new UniTaskCompletionSource();
            var scene = new UniTaskCompletionSource();
            _first.PrepareGate = new UniTaskCompletionSource();
            _second.PrepareGate = new UniTaskCompletionSource();
            var flow = new SceneRootFlow(_root, token => { Mark("cover"); return cover.Task; }, token => Mark("reveal"));
            var operation = flow.PrepareAndProceedAsync(token => { Mark("proceed"); return scene.Task; }).AsTask();
            Assert.That(_first.PrepareCount, Is.Zero);
            Assert.That(flow.IsTransitioning, Is.True);
            cover.TrySetResult();
            Assert.That(_first.PrepareCount, Is.EqualTo(1));
            Assert.That(_second.PrepareCount, Is.Zero);
            Assert.That(_root.IsPrepared, Is.False);
            _first.PrepareGate.TrySetResult();
            Assert.That(_second.PrepareCount, Is.EqualTo(1));
            _second.PrepareGate.TrySetResult();
            Assert.That(_root.IsPrepared, Is.True);
            Assert.That(operation.IsCompleted, Is.False);
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("reveal"));
            scene.TrySetResult();
            await operation;
            CollectionAssert.AreEqual(new[] { "cover", "prepare:first", "prepare:second", "proceed", "reveal" }, SceneRootInstallerProbe.Trace);
            Assert.That(flow.IsTransitioning, Is.False);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ReleaseWaitsUnderCoverAndCleansInReverseOrder() => CheckRelease().ToCoroutine();

        private async UniTask CheckRelease()
        {
            Create();
            await _root.PrepareAsync();
            SceneRootInstallerProbe.Trace.Clear();
            _second.ReleaseGate = new UniTaskCompletionSource();
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"));
            var operation = flow.ReleaseAndProceedAsync(token => Mark("proceed")).AsTask();
            Assert.That(_root.IsPrepared, Is.False);
            Assert.That(_first.ReleaseCount, Is.Zero);
            Assert.That(operation.IsCompleted, Is.False);
            _second.ReleaseGate.TrySetResult();
            await operation;
            CollectionAssert.AreEqual(new[] { "cover", "release:second", "release:first", "uninstall:second", "uninstall:first", "proceed", "reveal" }, SceneRootInstallerProbe.Trace);
            Assert.That(_root.IsReady, Is.False);
            Assert.Throws<ObjectDisposedException>(() => _root.PrepareAsync());
        }

        [UnityTest]
        public IEnumerator PreparationFailureRollsBackAndKeepsCover() => CheckPreparationFailure().ToCoroutine();

        private async UniTask CheckPreparationFailure()
        {
            Create();
            _first.FailPrepare = true;
            int failures = 0;
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"), error => ++failures);
            var operation = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            await Wait(operation);
            Assert.That(operation.IsFaulted, Is.True);
            Assert.That(operation.Exception.ToString(), Does.Contain("root-prepare:first"));
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(_second.PrepareCount, Is.Zero);
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
            Assert.That(_root.IsReady || _root.IsPrepared, Is.False);
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
        }

        [UnityTest]
        public IEnumerator OneWaiterCancellationDoesNotCancelSharedPreparation() => CheckSharedPreparation().ToCoroutine();

        private async UniTask CheckSharedPreparation()
        {
            Create();
            _first.PrepareGate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                var first = _root.PrepareAsync(cancellation.Token).AsTask();
                var second = _root.PrepareAsync().AsTask();
                cancellation.Cancel();
                await Wait(first);
                Assert.Throws<OperationCanceledException>(() => first.GetAwaiter().GetResult());
                Assert.That(second.IsCompleted, Is.False);
                _first.PrepareGate.TrySetResult();
                await second;
                Assert.That(_root.IsPrepared, Is.True);
                Assert.That(_first.PrepareCount, Is.EqualTo(1));
                Assert.That(_second.PrepareCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator CancelledFlowCannotProceedOrRevealAfterLatePreparation() => CheckFlowCancellation().ToCoroutine();

        private async UniTask CheckFlowCancellation()
        {
            Create();
            _first.PrepareGate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                int failures = 0;
                var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"), error => ++failures);
                var operation = flow.PrepareAndProceedAsync(token => Mark("proceed"), cancellation.Token).AsTask();
                cancellation.Cancel();
                await Wait(operation);
                Assert.Throws<OperationCanceledException>(() => operation.GetAwaiter().GetResult());
                _first.PrepareGate.TrySetResult();
                await _root.PrepareAsync();
                Assert.That(failures, Is.EqualTo(1));
                Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
            }
        }

        [UnityTest]
        public IEnumerator DestructionCancelsWaitersAndLateResultsCannotProceed() => CheckDestruction().ToCoroutine();

        private async UniTask CheckDestruction()
        {
            Create();
            _first.PrepareGate = new UniTaskCompletionSource();
            _first.IgnorePrepareCancellation = true;
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"));
            var operation = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            UnityEngine.Object.Destroy(_object);
            await UniTask.NextFrame();
            await Wait(operation);
            Assert.Throws<OperationCanceledException>(() => operation.GetAwaiter().GetResult());
            _first.PrepareGate.TrySetResult();
            await UniTask.NextFrame();
            Assert.That(_root.IsPrepared, Is.False);
            Assert.That(_second.PrepareCount, Is.Zero);
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
        }

        [UnityTest]
        public IEnumerator SameFlowRejectsOverlappingOperations() => CheckOverlap().ToCoroutine();

        private async UniTask CheckOverlap()
        {
            Create();
            _first.PrepareGate = new UniTaskCompletionSource();
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"));
            var first = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            var duplicate = flow.ReleaseAndProceedAsync(token => Mark("duplicate")).AsTask();
            await Wait(duplicate);
            Assert.That(duplicate.IsFaulted, Is.True);
            Assert.That(duplicate.Exception.ToString(), Does.Contain("InvalidOperationException"));
            _first.PrepareGate.TrySetResult();
            await first;
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("duplicate"));
        }

        [UnityTest]
        public IEnumerator ReleaseFailureStillCleansOtherSystemsAndKeepsCover() => CheckReleaseFailure().ToCoroutine();

        private async UniTask CheckReleaseFailure()
        {
            Create();
            _second.FailRelease = true;
            int failures = 0;
            var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"), error => ++failures);
            var operation = flow.ReleaseAndProceedAsync(token => Mark("proceed")).AsTask();
            await Wait(operation);
            Assert.That(operation.IsFaulted, Is.True);
            Assert.That(operation.Exception.ToString(), Does.Contain("root-release:second"));
            Assert.That(_first.ReleaseCount, Is.EqualTo(1));
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
        }

        [UnityTest]
        public IEnumerator CoverFailureDoesNotStartSystemsOrScene() => CheckCoverFailure().ToCoroutine();

        private async UniTask CheckCoverFailure()
        {
            Create();
            var flow = new SceneRootFlow(_root, token => throw new InvalidOperationException("cover-failure"), token => Mark("reveal"));
            var operation = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            await Wait(operation);
            Assert.That(operation.IsFaulted, Is.True);
            Assert.That(_first.PrepareCount, Is.Zero);
            Assert.That(_root.IsReady, Is.True);
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator SingletonRegistrationDoesNotAllowEarlySceneProgress() => CheckSingleton().ToCoroutine();

        private async UniTask CheckSingleton()
        {
            Create(SceneRootMode.Singleton);
            _first.PrepareGate = new UniTaskCompletionSource();
            Assert.That(SingletonSceneRoot.Instance, Is.SameAs(_root));
            Assert.That(_root.IsPrepared, Is.False);
            var flow = new SceneRootFlow(_root);
            var operation = flow.PrepareAndProceedAsync(token => Mark("proceed")).AsTask();
            Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed"));
            _first.PrepareGate.TrySetResult();
            await operation;
            Assert.That(_root.IsPrepared, Is.True);
            Assert.That(SceneRootInstallerProbe.Trace, Does.Contain("proceed"));
        }

        [UnityTest]
        public IEnumerator CancellationDuringReleaseFinishesCleanupBeforeStoppingScene() => CheckReleaseCancellation().ToCoroutine();

        private async UniTask CheckReleaseCancellation()
        {
            Create();
            _second.ReleaseGate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                var flow = new SceneRootFlow(_root, token => Mark("cover"), token => Mark("reveal"));
                var operation = flow.ReleaseAndProceedAsync(token => Mark("proceed"), cancellation.Token).AsTask();
                cancellation.Cancel();
                Assert.That(operation.IsCompleted, Is.False);
                _second.ReleaseGate.TrySetResult();
                await Wait(operation);
                Assert.Throws<OperationCanceledException>(() => operation.GetAwaiter().GetResult());
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
                Assert.That(SceneRootInstallerProbe.Trace, Does.Not.Contain("proceed").And.Not.Contain("reveal"));
            }
        }

        [UnityTest]
        public IEnumerator ShutdownCancelsPreparationAndCannotRunItAgain() => CheckShutdownDuringPreparation().ToCoroutine();

        private async UniTask CheckShutdownDuringPreparation()
        {
            Create();
            _first.PrepareGate = new UniTaskCompletionSource();
            var preparation = _root.PrepareAsync().AsTask();
            await _root.ShutdownAsync();
            await Wait(preparation);
            Assert.Throws<OperationCanceledException>(() => preparation.GetAwaiter().GetResult());
            Assert.That(_second.PrepareCount, Is.Zero);
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => _root.PrepareAsync());
            await _root.ShutdownAsync();
            Assert.That(_first.ReleaseCount, Is.EqualTo(1));
        }

        private static async UniTask WaitForShutdownDeadline(Task operation)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 1d;
            while (!operation.IsCompleted && Time.realtimeSinceStartupAsDouble < deadline)
            {
                await UniTask.NextFrame();
            }
            Assert.That(operation.IsCompleted, Is.True,
                "Shutdown did not settle within 1 second with a configured 0.1-second timeout.");
        }

        private static void AssertTimeouts(Task operation, int count, string phase)
        {
            Assert.That(operation.IsFaulted, Is.True);
            int found = 0;
            foreach (var failure in operation.Exception.Flatten().InnerExceptions)
            {
                if (failure is TimeoutException)
                {
                    ++found;
                    Assert.That(failure.Message, Does.Contain(nameof(SceneRootInstallerProbe)));
                    Assert.That(failure.Message, Does.Contain(phase));
                }
            }
            Assert.That(found, Is.EqualTo(count));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownWithUncooperativePreparationFailsWithTimeoutAndStillCleansUp() =>
            CheckPreparationDeadline(SceneRootMode.SceneOwned).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator SingletonShutdownWithUncooperativePreparationFailsWithTimeoutAndStillCleansUp() =>
            CheckPreparationDeadline(SceneRootMode.Singleton).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownTimeoutUsesUnscaledTimeWhenTimeScaleIsZero() =>
            CheckPreparationDeadline(SceneRootMode.SceneOwned, pause: true).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator ConcurrentShutdownWaitersShareTheSameTimeoutFailure() =>
            CheckPreparationDeadline(SceneRootMode.SceneOwned, shared: true).ToCoroutine();

        private async UniTask CheckPreparationDeadline(SceneRootMode mode, bool pause = false, bool shared = false)
        {
            Create(mode, 0.1f);
            _first.PrepareGate = new UniTaskCompletionSource();
            _first.IgnorePrepareCancellation = true;
            float previousTimeScale = Time.timeScale;
            var preparation = _root.PrepareAsync().AsTask();
            Task shutdown = null;
            try
            {
                if (pause)
                {
                    Time.timeScale = 0;
                }
                shutdown = _root.ShutdownAsync().AsTask();
                Task secondWaiter = shared ? _root.ShutdownAsync().AsTask() : null;
                await WaitForShutdownDeadline(shutdown);
                AssertTimeouts(shutdown, 1, "Prepare");
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
                Assert.That(_first.ReleaseCount, Is.EqualTo(1));
                Assert.That(_second.ReleaseCount, Is.EqualTo(1));
                Assert.That(_root.IsReady || _root.IsPrepared, Is.False);
                Assert.That(_second.PrepareCount, Is.Zero);
                Assert.Throws<ObjectDisposedException>(() => _root.PrepareAsync());
                if (shared)
                {
                    await WaitForShutdownDeadline(secondWaiter);
                    Assert.That(secondWaiter.Exception.InnerException, Is.SameAs(shutdown.Exception.InnerException));
                }
                _first.PrepareGate.TrySetResult();
                await Wait(preparation);
                Assert.Throws<OperationCanceledException>(() => preparation.GetAwaiter().GetResult());
                await UniTask.NextFrame();
                Assert.That(_root.IsPrepared, Is.False);
                Assert.That(_second.PrepareCount, Is.Zero);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                _first.PrepareGate.TrySetResult();
                if (shutdown != null && shutdown.IsFaulted)
                {
                    _ = shutdown.Exception;
                }
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReleaseTimeoutStillReleasesAndUninstallsOtherSystems() =>
            CheckReleaseDeadline(SceneRootMode.SceneOwned).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator SingletonReleaseTimeoutStillReleasesAndUninstallsOtherSystems() =>
            CheckReleaseDeadline(SceneRootMode.Singleton).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator LateReleaseFailureAfterTimeoutIsObserved() =>
            CheckReleaseDeadline(SceneRootMode.SceneOwned, lateFailure: true).ToCoroutine();

        private async UniTask CheckReleaseDeadline(SceneRootMode mode, bool lateFailure = false)
        {
            Create(mode, 0.1f);
            _second.ReleaseGate = new UniTaskCompletionSource();
            _second.FailRelease = lateFailure;
            await _root.PrepareAsync();
            var shutdown = _root.ShutdownAsync().AsTask();
            try
            {
                await WaitForShutdownDeadline(shutdown);
                AssertTimeouts(shutdown, 1, "Release");
                Assert.That(_first.ReleaseCount, Is.EqualTo(1));
                Assert.That(_second.ReleaseCount, Is.EqualTo(1));
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
                Assert.That(SceneRootInstallerProbe.Trace.IndexOf("release:second"),
                    Is.LessThan(SceneRootInstallerProbe.Trace.IndexOf("release:first")));
                Assert.That(_root.IsReady || _root.IsPrepared, Is.False);
                _second.ReleaseGate.TrySetResult();
                await UniTask.NextFrame();
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
            }
            finally
            {
                _second.ReleaseGate.TrySetResult();
                if (shutdown.IsFaulted)
                {
                    _ = shutdown.Exception;
                }
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator EachReleaseReceivesAnIndependentTimeoutBudget() => CheckReleaseBudgets().ToCoroutine();

        private async UniTask CheckReleaseBudgets()
        {
            Create(shutdownTimeoutSeconds: 0.1f);
            _first.ReleaseGate = new UniTaskCompletionSource();
            _second.ReleaseGate = new UniTaskCompletionSource();
            double firstReleaseStarted = 0;
            _first.Releasing = () => firstReleaseStarted = Time.realtimeSinceStartupAsDouble;
            var shutdown = _root.ShutdownAsync().AsTask();
            try
            {
                await WaitForShutdownDeadline(shutdown);
                AssertTimeouts(shutdown, 2, "Release");
                Assert.That(Time.realtimeSinceStartupAsDouble - firstReleaseStarted, Is.GreaterThanOrEqualTo(0.05d));
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
            }
            finally
            {
                _first.ReleaseGate.TrySetResult();
                _second.ReleaseGate.TrySetResult();
                if (shutdown.IsFaulted)
                {
                    _ = shutdown.Exception;
                }
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PreparationAndReleaseReceiveSeparateTimeoutBudgets() => CheckPhaseBudgets().ToCoroutine();

        private async UniTask CheckPhaseBudgets()
        {
            Create(shutdownTimeoutSeconds: 0.1f);
            _first.PrepareGate = new UniTaskCompletionSource();
            _first.IgnorePrepareCancellation = true;
            _second.ReleaseGate = new UniTaskCompletionSource();
            double releaseStarted = 0;
            _second.Releasing = () => releaseStarted = Time.realtimeSinceStartupAsDouble;
            var preparation = _root.PrepareAsync().AsTask();
            var shutdown = _root.ShutdownAsync().AsTask();
            try
            {
                await WaitForShutdownDeadline(shutdown);
                AssertTimeouts(shutdown, 2, "");
                Assert.That(shutdown.Exception.ToString(), Does.Contain("Prepare").And.Contain("Release"));
                Assert.That(Time.realtimeSinceStartupAsDouble - releaseStarted, Is.GreaterThanOrEqualTo(0.05d));
                Assert.That(_first.UninstallCount, Is.EqualTo(1));
                Assert.That(_second.UninstallCount, Is.EqualTo(1));
            }
            finally
            {
                _first.PrepareGate.TrySetResult();
                _second.ReleaseGate.TrySetResult();
                if (shutdown.IsFaulted)
                {
                    _ = shutdown.Exception;
                }
            }
            await Wait(preparation);
            Assert.Throws<OperationCanceledException>(() => preparation.GetAwaiter().GetResult());
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator OriginalPreparationTimeoutFailureIsNotAShutdownDeadlineFailure() =>
            CheckOriginalPreparationTimeout().ToCoroutine();

        private async UniTask CheckOriginalPreparationTimeout()
        {
            Create(shutdownTimeoutSeconds: 0.1f);
            var expected = new TimeoutException("installer-owned-prepare-timeout");
            _first.Preparing = () => throw expected;
            var preparation = _root.PrepareAsync().AsTask();
            await Wait(preparation);
            Assert.That(preparation.Exception.InnerException, Is.SameAs(expected));
            await _root.ShutdownAsync();
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator OriginalReleaseTimeoutFailureRetainsItsIdentity() => CheckOriginalReleaseTimeout().ToCoroutine();

        private async UniTask CheckOriginalReleaseTimeout()
        {
            Create(shutdownTimeoutSeconds: 0.1f);
            var expected = new TimeoutException("installer-owned-release-timeout");
            _second.Releasing = () => throw expected;
            var shutdown = _root.ShutdownAsync().AsTask();
            await WaitForShutdownDeadline(shutdown);
            Assert.That(shutdown.Exception.Flatten().InnerExceptions.Count, Is.EqualTo(1));
            Assert.That(shutdown.Exception.Flatten().InnerExceptions[0], Is.SameAs(expected));
            Assert.That(_first.ReleaseCount, Is.EqualTo(1));
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownWithUncooperativePreparationNeverCompletesWithoutTimeout() =>
            CheckUnlimitedShutdown(preparation: true).ToCoroutine();

        [UnityTest, Timeout(30000)]
        public IEnumerator NegativeReleaseTimeoutWaitsUntilItsGateCompletes() =>
            CheckUnlimitedShutdown(preparation: false).ToCoroutine();

        private async UniTask CheckUnlimitedShutdown(bool preparation)
        {
            Create(shutdownTimeoutSeconds: preparation ? 0f : -1f);
            Task preparing = null;
            if (preparation)
            {
                _first.PrepareGate = new UniTaskCompletionSource();
                _first.IgnorePrepareCancellation = true;
                preparing = _root.PrepareAsync().AsTask();
            }
            else
            {
                _second.ReleaseGate = new UniTaskCompletionSource();
            }
            var shutdown = _root.ShutdownAsync().AsTask();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.2), DelayType.UnscaledDeltaTime);
                Assert.That(shutdown.IsCompleted, Is.False);
                Assert.That(_first.UninstallCount, Is.Zero);
                Assert.That(_second.UninstallCount, Is.Zero);
            }
            finally
            {
                _first.PrepareGate?.TrySetResult();
                _second.ReleaseGate?.TrySetResult();
            }
            await Wait(shutdown);
            Assert.That(shutdown.IsCompletedSuccessfully, Is.True);
            Assert.That(_first.UninstallCount, Is.EqualTo(1));
            Assert.That(_second.UninstallCount, Is.EqualTo(1));
            if (preparing != null)
            {
                await Wait(preparing);
                Assert.Throws<OperationCanceledException>(() => preparing.GetAwaiter().GetResult());
            }
        }
    }
}
