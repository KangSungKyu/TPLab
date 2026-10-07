using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SceneResult = MyLab.Core.ResourceManagement.LoadedScene;

namespace MyLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class GameSceneReplacementTests
    {
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/MyLab/Tests/Fixtures/ReplacementMain.unity";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneRootInstallerProbe _commonInstaller;
        private SceneTransitionCallbacksProbe _callbacks;
        private GameSceneManager _manager;
        private TrackingLoader _loader;
        private UniTaskCompletionSource _releaseGate;
        private Scene _foreign;

        [SetUp]
        public void SetUp()
        {
            _host = null;
            _common = null;
            _commonInstaller = null;
            _callbacks = null;
            _manager = null;
            _loader = null;
            _releaseGate = null;
            _foreign = default;
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            // Complete test gates even when a Red command throws before it can consume them.
            _releaseGate?.TrySetResult();
            _loader?.Gate?.TrySetResult();
            _callbacks?.PresentationGate?.TrySetResult();
            if (SceneManager.sceneCount == 1 && IsFixture(SceneManager.GetSceneAt(0)))
                SceneManager.CreateScene("SceneReplacementTestRecovery");
            if (_manager != null)
            {
                try { await _manager.ShutdownAsync(); } catch (Exception) { }
            }
            if (_common != null) await _common.ShutdownAsync();
            foreach (string path in new[] { Hub, Main })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (SceneManager.sceneCount <= 1) SceneManager.CreateScene("SceneReplacementTestRecovery");
                foreach (var root in scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Where(root => root != null))
                {
                    try { await root.ShutdownAsync(); } catch (Exception) { }
                }
                await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
            if (_foreign.IsValid() && _foreign.isLoaded)
            {
                if (SceneManager.sceneCount <= 1) SceneManager.CreateScene("SceneReplacementTestRecovery");
                await SceneManager.UnloadSceneAsync(_foreign).ToUniTask();
            }
            if (_host != null) UnityEngine.Object.Destroy(_host);
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator AdditivePresentationFinishesBeforeOldRootShutdown() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            var oldRoot = Root(oldScene);
            var oldInstaller = oldRoot.GetComponent<SceneRootInstallerProbe>();
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var replacement = _manager.ReplacePrimaryAsync(SceneTarget.BuildScene(Main)).AsTask();
            await WaitForMainPresentationAsync();
            Assert.That(oldInstaller.ReleaseCount, Is.Zero);
            Assert.That(oldRoot.IsPrepared, Is.True);
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(_manager.OwnedScenes, Does.Contain(oldScene));
            Assert.That(_manager.OwnedScenes, Does.Contain(_callbacks.PresentationRoot.RootObject.scene));
            Assert.That(replacement.IsCompleted, Is.False);
            _callbacks.PresentationGate.TrySetResult();
            await replacement;
            Assert.That(oldInstaller.ReleaseCount, Is.EqualTo(1));
            Assert.That(oldScene.isLoaded, Is.False);
            Assert.That(_manager.GameScene.path, Is.EqualTo(Main));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator CandidateFailurePreservesPreparedOldRootAndKeepsCover() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            var oldRoot = Root(oldScene);
            var oldInstaller = oldRoot.GetComponent<SceneRootInstallerProbe>();
            _callbacks.Configuring = () => Root(SceneManager.GetSceneByPath(Main)).GetComponent<SceneRootInstallerProbe>().FailPrepare = true;
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("root-prepare"));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(oldRoot.IsPrepared, Is.True);
            Assert.That(oldInstaller.ReleaseCount, Is.Zero);
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(oldScene));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator SingleWaitsForOldGracefulShutdownBeforeNativeLoad() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true, LoadSceneMode.Single);
            var oldScene = _manager.GameScene;
            var oldRoot = Root(oldScene);
            var oldInstaller = oldRoot.GetComponent<SceneRootInstallerProbe>();
            _releaseGate = oldInstaller.ReleaseGate = new UniTaskCompletionSource();
            _loader.BeforeLoad = () =>
            {
                Assert.That(oldInstaller.ReleaseCount, Is.EqualTo(1));
                Assert.That(oldInstaller.UninstallCount, Is.EqualTo(1));
                Assert.That(oldRoot.IsPrepared, Is.False);
                Assert.That(oldScene.isLoaded, Is.True);
            };
            var replacement = _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single).AsTask();
            Assert.That(oldInstaller.ReleaseCount, Is.EqualTo(1));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(replacement.IsCompleted, Is.False);
            _releaseGate.TrySetResult();
            await replacement;
            Assert.That(_loader.LoadCount, Is.EqualTo(2));
            Assert.That(oldScene.isLoaded, Is.False);
            Assert.That(_manager.GameScene.path, Is.EqualTo(Main));
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(_commonInstaller.ReleaseCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator TwoReplacementsKeepCommonAndSeparateEntryFromTransitionHistory() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var replacement = _manager.ReplacePrimaryAsync(Main).AsTask();
            await WaitForMainPresentationAsync();
            Assert.That(_manager.WaitForEntryAsync().AsTask().IsCompleted, Is.True);
            var transition = _manager.WaitForTransitionAsync().AsTask();
            Assert.That(transition.IsCompleted, Is.False);
            _callbacks.PresentationGate.TrySetResult();
            await replacement;
            await transition;
            _callbacks.PresentationGate = null;
            await _manager.ReplacePrimaryAsync(SceneTarget.BuildScene(Hub));
            await _manager.WaitForEntryAsync();
            await _manager.WaitForTransitionAsync();
            Assert.That(_manager.GameScene.path, Is.EqualTo(Hub));
            Assert.That(_manager.OwnedScenes.Count, Is.EqualTo(1));
            Assert.That(_commonInstaller.InstallCount, Is.EqualTo(1));
            Assert.That(_commonInstaller.PrepareCount, Is.EqualTo(1));
            Assert.That(_commonInstaller.ReleaseCount, Is.Zero);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator OverlappingReplacementIsRejectedBeforeEffects() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var replacement = _manager.ReplacePrimaryAsync(Main).AsTask();
            await WaitForMainPresentationAsync();
            int covers = _callbacks.CoverCount;
            int loads = _loader.LoadCount;
            Assert.Throws<InvalidOperationException>(() => _manager.ReplacePrimaryAsync(Hub));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(covers));
            Assert.That(_loader.LoadCount, Is.EqualTo(loads));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            _callbacks.PresentationGate.TrySetResult();
            await replacement;
        });

        [UnityTest]
        public IEnumerator OwnerCancellationWaitsForLateCandidateThenPreservesOldRoot() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            var oldRoot = Root(oldScene);
            _loader.Gate = new UniTaskCompletionSource();
            var replacement = _manager.ReplacePrimaryAsync(Main).AsTask();
            _manager.CancelTransition();
            Assert.That(replacement.IsCompleted, Is.False);
            _loader.Gate.TrySetResult();
            Exception failure = null;
            try { await replacement; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(oldRoot.IsPrepared, Is.True);
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_loader.UnloadCount, Is.EqualTo(1));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator ShutdownSharesWaitForLateCandidateAndCleansEveryOwnedScene() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            _loader.Gate = new UniTaskCompletionSource();
            var replacement = _manager.ReplacePrimaryAsync(Main).AsTask();
            var first = _manager.ShutdownAsync().AsTask();
            var second = _manager.ShutdownAsync().AsTask();
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.IsCompleted, Is.False);
            _loader.Gate.TrySetResult();
            Exception failure = null;
            try { await replacement; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            await first;
            await second;
            Assert.That(_loader.UnloadCount, Is.EqualTo(2));
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Stopped));
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(_commonInstaller.ReleaseCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator CallerCancellationStopsOnlyItsReplacementWait() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            _loader.Gate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                var replacement = _manager.ReplacePrimaryAsync(Main, cancellationToken: cancellation.Token).AsTask();
                cancellation.Cancel();
                Exception failure = null;
                try { await replacement; } catch (Exception exception) { failure = exception; }
                Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                _loader.Gate.TrySetResult();
                await _manager.WaitForTransitionAsync();
                Assert.That(_manager.CanProceed, Is.True);
                Assert.That(_manager.GameScene.path, Is.EqualTo(Main));
                Assert.That(_callbacks.FailureCount, Is.Zero);
                Assert.That(_common.IsPrepared, Is.True);
            }
        });

        [UnityTest]
        public IEnumerator SingleRejectsRetainedNormalSceneBeforeCover() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true);
            Assert.Throws<InvalidOperationException>(() => _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(Root(_manager.GameScene).IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator SingleRejectsUnregisteredSceneBeforeCover() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true, LoadSceneMode.Single);
            _foreign = SceneManager.CreateScene("UnregisteredReplacementScene");
            Assert.Throws<InvalidOperationException>(() => _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(_foreign.isLoaded, Is.True);
        });

        [UnityTest]
        public IEnumerator SingleCandidateFailureKeepsTheLastActualSceneOwnedAndUnprepared() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true, LoadSceneMode.Single);
            _callbacks.Configuring = () => Root(SceneManager.GetSceneByPath(Main)).GetComponent<SceneRootInstallerProbe>().FailPrepare = true;
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("last normal scene"));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            var remaining = SceneManager.GetSceneByPath(Main);
            Assert.That(remaining.isLoaded, Is.True);
            Assert.That(_manager.LoadedScene, Is.EqualTo(remaining));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { remaining }));
            Assert.That(_manager.IsGamePrepared, Is.False);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator OldReleaseFailureCleansCandidateAndRetainsActualOldOwner() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            var oldInstaller = Root(oldScene).GetComponent<SceneRootInstallerProbe>();
            oldInstaller.FailRelease = true;
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("root-release:game"));
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(Root(oldScene).IsPrepared, Is.False);
            Assert.That(oldInstaller.ReleaseCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.GameScene.IsValid(), Is.False);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator SingleOldShutdownFailurePreventsNativeLoad() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true, LoadSceneMode.Single);
            var oldScene = _manager.GameScene;
            Root(oldScene).GetComponent<SceneRootInstallerProbe>().FailRelease = true;
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.IsGamePrepared, Is.False);
            Assert.That(_manager.GameScene.IsValid(), Is.False);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator RevealFailureRestoresCoverAndCleansReplacement() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            _callbacks.Revealing = () => throw new InvalidOperationException("replacement-reveal");
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("replacement-reveal"));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(3));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(2));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_manager.FailurePhase, Is.EqualTo(SceneTransitionState.Revealing));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(oldScene.isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator MovedCandidateIsReleasedWithoutShuttingDownPreparedOldRoot() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            var oldRoot = Root(oldScene);
            SceneRootInstallerProbe candidateInstaller = null;
            _callbacks.Presenting = () =>
            {
                var candidate = _callbacks.PresentationRoot.RootObject;
                candidateInstaller = candidate.GetComponent<SceneRootInstallerProbe>();
                SceneManager.MoveGameObjectToScene(candidate, oldScene);
            };
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(candidateInstaller.ReleaseCount, Is.EqualTo(1));
            Assert.That(oldRoot.IsPrepared, Is.True);
            Assert.That(oldRoot.GetComponent<SceneRootInstallerProbe>().ReleaseCount, Is.Zero);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(oldScene));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
        });

        [UnityTest]
        public IEnumerator ForeignSceneDuringCandidatePresentationIsRetainedAndFaultsBeforeOldShutdown() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            _callbacks.Presenting = () => _foreign = SceneManager.CreateScene("ForeignDuringReplacement");
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(_foreign.isLoaded, Is.True);
            Assert.That(Root(oldScene).IsPrepared, Is.True);
            Assert.That(Root(oldScene).GetComponent<SceneRootInstallerProbe>().ReleaseCount, Is.Zero);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator FailedOldUnloadRemainsObservableAfterCandidateCleanupAndExplicitShutdown() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var oldScene = _manager.GameScene;
            _loader.FailUnloadPath = Hub;
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("replacement-unload"));
            Assert.That(oldScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.IsGamePrepared, Is.False);
            Exception shutdownFailure = null;
            try { await _manager.ShutdownAsync(); } catch (Exception exception) { shutdownFailure = exception; }
            Assert.That(shutdownFailure, Is.TypeOf<AggregateException>());
            Assert.That(shutdownFailure.ToString(), Does.Contain("replacement-unload"));
            Assert.That(_loader.UnloadCount, Is.EqualTo(2));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator CompletedOperationAwaiterCanImmediatelyStartAndCancelTheNextOperation() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            // UniTask resumes this awaiter inside the previous operation's completion dispatch.
            await _manager.ReplacePrimaryAsync(Main);
            var oldScene = _manager.GameScene;
            _loader.Gate = new UniTaskCompletionSource();
            var next = _manager.ReplacePrimaryAsync(Hub).AsTask();
            _manager.CancelTransition();
            Assert.That(next.IsCompleted, Is.False);
            _loader.Gate.TrySetResult();
            Exception failure = null;
            try { await next; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(Root(oldScene).IsPrepared, Is.True);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { oldScene }));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator UnsupportedProgressKeepsPreparationAndNativeCompletionSeparate() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            bool sawUnknownLoad = false;
            _callbacks.ProgressReported = snapshot =>
            {
                if (snapshot.Stage == SceneTransitionState.Loading)
                {
                    sawUnknownLoad = true;
                    Assert.That(snapshot.StageRatio, Is.Null);
                    Assert.That(snapshot.IsPrepared, Is.False);
                }
            };
            var replacement = _manager.ReplacePrimaryAsync(Main).AsTask();
            await WaitForMainPresentationAsync();
            Assert.That(sawUnknownLoad, Is.True);
            Assert.That(_manager.Progress.HasValue, Is.True);
            Assert.That(_manager.Progress.Value.Stage, Is.EqualTo(SceneTransitionState.PreparingPresentation));
            Assert.That(_manager.Progress.Value.IsPrepared, Is.False);
            Assert.That(_manager.CanProceed, Is.False);
            var id = _manager.Progress.Value.OperationId;
            _callbacks.PresentationGate.TrySetResult();
            await replacement;
            Assert.That(_manager.Progress.Value.Stage, Is.EqualTo(SceneTransitionState.Ready));
            Assert.That(_manager.Progress.Value.IsPrepared, Is.True);
            Assert.That(_manager.Progress.Value.OperationId, Is.EqualTo(id));
        });

        [UnityTest]
        public IEnumerator ProgressHookFailureCleansCandidateBeforeReportingFailure() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var old = _manager.GameScene;
            _callbacks.ProgressReported = snapshot =>
            {
                if (snapshot.Stage == SceneTransitionState.PreparingScene)
                    throw new InvalidOperationException("progress-ui");
            };
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("progress-ui"));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(Root(old).IsPrepared, Is.True);
            Assert.That(_manager.CanProceed, Is.False);
        });

        [UnityTest]
        public IEnumerator NativeProgressFailureStillRetainsAndUnloadsLateResult() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(progress: true);
            var old = _manager.GameScene;
            _callbacks.ProgressReported = snapshot =>
            {
                if (snapshot.Stage == SceneTransitionState.Loading && snapshot.StageRatio.HasValue)
                    throw new InvalidOperationException("native-progress-ui");
            };
            Exception failure = null;
            try { await _manager.ReplacePrimaryAsync(Main); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("native-progress-ui"));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(Root(old).IsPrepared, Is.True);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { old }));
        });

        private async UniTask CreateEnteredManagerAsync(bool persistent = false, LoadSceneMode firstMode = LoadSceneMode.Additive, bool progress = false)
        {
            _host = new GameObject("ReplacementCommon");
            _host.SetActive(false);
            _commonInstaller = _host.AddComponent<SceneRootInstallerProbe>();
            _commonInstaller.Id = "common-replacement";
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(new SceneRootInstaller[] { _commonInstaller }, persistent);
            _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _host.SetActive(true);
            _loader = new TrackingLoader();
            _manager = new GameSceneManager(_common, _callbacks, progress ? new NativeSceneLoader() : (ISceneLoader)_loader);
            await _manager.EnterFirstSceneAsync(Hub, firstMode);
        }

        private UniTask WaitForMainPresentationAsync() => UniTask.WaitUntil(() =>
            _callbacks.PresentationRoot != null && _callbacks.PresentationRoot.RootObject != null &&
            _callbacks.PresentationRoot.RootObject.scene.path == Main).Timeout(TimeSpan.FromSeconds(10));

        private static bool IsFixture(Scene scene) => scene.path == Hub || scene.path == Main;
        private static SceneOwnedRoot Root(Scene scene) => scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Single(root => root != null);

        private sealed class TrackingLoader : ISceneLoader
        {
            private readonly NativeSceneLoader _native = new NativeSceneLoader();
            internal int LoadCount;
            internal int UnloadCount;
            internal UniTaskCompletionSource Gate;
            internal Action BeforeLoad;
            internal string FailUnloadPath;

            public void Validate(SceneTarget target) => _native.Validate(target);

            public async UniTask<SceneResult> LoadAsync(SceneTarget target, LoadSceneMode mode)
            {
                ++LoadCount;
                BeforeLoad?.Invoke();
                if (Gate != null) await Gate.Task;
                var result = await _native.LoadAsync(target, mode);
                return new SceneResult(target, result.Scene, async () =>
                {
                    ++UnloadCount;
                    if (target.ScenePath == FailUnloadPath) throw new InvalidOperationException("replacement-unload");
                    await result.UnloadAsync();
                });
            }
        }
    }
}
