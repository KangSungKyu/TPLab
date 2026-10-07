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

        private void Create(SceneRootMode mode = SceneRootMode.SceneOwned)
        {
            _object = new GameObject("AsyncSceneTestRoot");
            _object.SetActive(false);
            _first = _object.AddComponent<SceneRootInstallerProbe>();
            _first.Id = "first";
            _second = _object.AddComponent<SceneRootInstallerProbe>();
            _second.Id = "second";
            _root = SceneRootSetup.Attach(_object, mode, new[] { _first, _second });
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
    }
}
