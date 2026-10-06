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
    public sealed class GameSceneAreaTests
    {
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/MyLab/Tests/Fixtures/ReplacementMain.unity";
        private const string Area = "Assets/MyLab/Tests/Fixtures/DerivedArea.unity";
        private const string Nested = "Assets/MyLab/Tests/Fixtures/NestedArea.unity";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneRootInstallerProbe _commonInstaller;
        private SceneTransitionCallbacksProbe _callbacks;
        private SceneAreaRevealGateCallbacks _revealCallbacks;
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
            _revealCallbacks = null;
            _manager = null;
            _loader = null;
            _releaseGate = null;
            _foreign = default;
            SceneRootInstallerProbe.Trace.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            _releaseGate?.TrySetResult();
            _loader?.Gate?.TrySetResult();
            _callbacks?.PresentationGate?.TrySetResult();
            _revealCallbacks?.RevealGate?.TrySetResult();
            if (SceneManager.sceneCount == 1 && IsFixture(SceneManager.GetSceneAt(0)))
                SceneManager.CreateScene("SceneAreaTestRecovery");
            if (_manager != null)
            {
                try { await _manager.ShutdownAsync(); } catch (Exception) { }
            }
            if (_common != null) await _common.ShutdownAsync();
            foreach (string path in new[] { Nested, Area, Main, Hub })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (SceneManager.sceneCount <= 1) SceneManager.CreateScene("SceneAreaTestRecovery");
                foreach (var root in scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Where(root => root != null))
                {
                    try { await root.ShutdownAsync(); } catch (Exception) { }
                }
                await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
            if (_foreign.IsValid() && _foreign.isLoaded)
            {
                if (SceneManager.sceneCount <= 1) SceneManager.CreateScene("SceneAreaTestRecovery");
                await SceneManager.UnloadSceneAsync(_foreign).ToUniTask();
            }
            if (_host != null) UnityEngine.Object.Destroy(_host);
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator PrimaryRegistrationHasSessionBoundaryAndPreparedSnapshot() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var registration = _manager.RegisteredScenes.Single();
            Assert.That(registration.Scene, Is.EqualTo(_manager.GameScene));
            Assert.That(registration.Parent, Is.EqualTo(default(Scene)));
            Assert.That(registration.Role, Is.EqualTo(SceneRegistrationRole.Primary));
            Assert.That(registration.Priority, Is.Zero);
            Assert.That(registration.IsPrepared, Is.True);
            Assert.That(registration.IsShuttingDown, Is.False);
        });

        [UnityTest]
        public IEnumerator AdditiveChildWaitsForPresentationAndRetainsPreparedParent() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var parent = _manager.GameScene;
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var add = _manager.AddDerivedAsync(SceneTarget.BuildScene(Area), parent).AsTask();
            await WaitForPresentationAsync(Area);
            Assert.That(Root(parent).IsPrepared, Is.True);
            Assert.That(Installer(parent).ReleaseCount, Is.Zero);
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(add.IsCompleted, Is.False);
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { parent }));
            Assert.That(_manager.OwnedScenes.Count, Is.EqualTo(2));
            _callbacks.PresentationGate.TrySetResult();
            var child = await add;
            Assert.That(child.path, Is.EqualTo(Area));
            Assert.That(_manager.GameScene, Is.EqualTo(parent));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(parent));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator NestedRegistrationsCopyParentAndPriorityWithoutAutomaticActivation() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary, priority: -20);
            var snapshot = _manager.RegisteredScenes;
            var nested = await _manager.AddDerivedAsync(Nested, child, priority: 900);
            var childInfo = _manager.RegisteredScenes.Single(item => item.Scene == child);
            var nestedInfo = _manager.RegisteredScenes.Single(item => item.Scene == nested);
            Assert.That(childInfo.Parent, Is.EqualTo(primary));
            Assert.That(childInfo.Role, Is.EqualTo(SceneRegistrationRole.Derived));
            Assert.That(childInfo.Priority, Is.EqualTo(-20));
            Assert.That(nestedInfo.Parent, Is.EqualTo(child));
            Assert.That(nestedInfo.Priority, Is.EqualTo(900));
            Assert.That(nestedInfo.IsPrepared, Is.True);
            Assert.That(nestedInfo.IsShuttingDown, Is.False);
            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(primary));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator ExplicitActivationChangesActiveSceneWithoutReplacingPrimary() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary, activate: true);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(child));
            var nested = await _manager.AddDerivedAsync(Nested, child);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(child));
            Assert.That(_manager.GameScene, Is.EqualTo(primary));
            Assert.That(_manager.RegisteredScenes.Count, Is.EqualTo(3));
            Assert.That(Root(nested).IsPrepared, Is.True);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DerivedCanExitItselfWithoutReleasingParent() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary, activate: true);
            var installer = Installer(child);
            await _manager.RemoveDerivedAsync(child, child);
            Assert.That(installer.ReleaseCount, Is.EqualTo(1));
            Assert.That(child.isLoaded, Is.False);
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(Installer(primary).ReleaseCount, Is.Zero);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary }));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(primary));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator AncestorRemovesSubtreeChildFirst() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            SceneRootInstallerProbe.Trace.Clear();
            await _manager.RemoveDerivedAsync(child, primary);
            Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea" }));
            Assert.That(child.isLoaded, Is.False);
            Assert.That(nested.isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Count, Is.EqualTo(1));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DerivedAncestorRemovesItsChildWhileKeepingOwnBranchPrepared() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child, activate: true);
            await _manager.RemoveDerivedAsync(nested, child);
            Assert.That(nested.isLoaded, Is.False);
            Assert.That(Root(child).IsPrepared, Is.True);
            Assert.That(Installer(child).ReleaseCount, Is.Zero);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(child));
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, child }));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator RegisteredSiblingCannotRemoveAnotherSubtree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var sibling = await _manager.AddDerivedAsync(Nested, primary);
            int covers = _callbacks.CoverCount;
            Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(child, sibling));
            Assert.That(Installer(child).ReleaseCount, Is.Zero);
            Assert.That(_callbacks.CoverCount, Is.EqualTo(covers));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_manager.RegisteredScenes.Count, Is.EqualTo(3));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator PendingSameRemovalSharesResultAfterAuthorityValidationAndCallerCancellation() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var sibling = await _manager.AddDerivedAsync(Nested, primary);
            var installer = Installer(child);
            _releaseGate = installer.ReleaseGate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                var first = _manager.RemoveDerivedAsync(child, child, cancellation.Token).AsTask();
                var second = _manager.RemoveDerivedAsync(child, primary).AsTask();
                var snapshot = _manager.RegisteredScenes.Single(item => item.Scene == child);
                Assert.That(snapshot.IsShuttingDown, Is.True);
                Assert.That(snapshot.IsPrepared, Is.False);
                Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(child, sibling));
                Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(sibling, primary));
                Assert.That(installer.ReleaseCount, Is.EqualTo(1));
                Assert.That(second.IsCompleted, Is.False);
                cancellation.Cancel();
                Exception failure = null;
                try { await first; } catch (Exception exception) { failure = exception; }
                Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(child.isLoaded, Is.True);
                _releaseGate.TrySetResult();
                await second;
                Assert.That(child.isLoaded, Is.False);
                Assert.That(installer.ReleaseCount, Is.EqualTo(1));
                Assert.That(_callbacks.FailureCount, Is.Zero);
                Assert.That(snapshot.IsShuttingDown, Is.True);
            }
        });

        [UnityTest]
        public IEnumerator OtherCommandsAreRejectedDuringRemovalWithoutEffects() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _releaseGate = Installer(child).ReleaseGate = new UniTaskCompletionSource();
            var remove = _manager.RemoveDerivedAsync(child, primary).AsTask();
            int covers = _callbacks.CoverCount;
            int loads = _loader.LoadCount;
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Nested, primary));
            Assert.Throws<InvalidOperationException>(() => _manager.ReplacePrimaryAsync(Main));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(covers));
            Assert.That(_loader.LoadCount, Is.EqualTo(loads));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            _releaseGate.TrySetResult();
            await remove;
        });

        [UnityTest]
        public IEnumerator InvalidParentDuplicateAssetAndPrimaryRemovalAreRejectedBeforeEffects() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, default));
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Hub, primary));
            var child = await _manager.AddDerivedAsync(Area, primary);
            int covers = _callbacks.CoverCount;
            int loads = _loader.LoadCount;
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, primary));
            Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(primary, primary));
            Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(child, default));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(covers));
            Assert.That(_loader.LoadCount, Is.EqualTo(loads));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator PrimaryAdditiveReplacementReleasesSubtreeChildFirst() => UniTask.ToCoroutine(() => ReplaceTreeAsync(LoadSceneMode.Additive));

        [UnityTest]
        public IEnumerator PrimarySingleReplacementReleasesWholeTreeBeforeNativeLoad() => UniTask.ToCoroutine(() => ReplaceTreeAsync(LoadSceneMode.Single));

        [UnityTest]
        public IEnumerator OwnerCancellationWaitsForLateAddedCandidateAndPreservesRegisteredTree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _loader.Gate = new UniTaskCompletionSource();
            var add = _manager.AddDerivedAsync(Nested, child).AsTask();
            _manager.CancelTransition();
            Assert.That(add.IsCompleted, Is.False);
            _loader.Gate.TrySetResult();
            Exception failure = null;
            try { await add; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(SceneManager.GetSceneByPath(Nested).isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, child }));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary, child }));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(Root(child).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator FailedCandidatePreparationNeverRegistersOrReleasesExistingTree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _callbacks.Configuring = () => Installer(SceneManager.GetSceneByPath(Nested)).FailPrepare = true;
            Exception failure = null;
            try { await _manager.AddDerivedAsync(Nested, child); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("root-prepare:NestedArea"));
            Assert.That(SceneManager.GetSceneByPath(Nested).isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, child }));
            Assert.That(Installer(primary).ReleaseCount, Is.Zero);
            Assert.That(Installer(child).ReleaseCount, Is.Zero);
            Assert.That(Root(child).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator ShutdownCleansEntireTreeChildFirst() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            SceneRootInstallerProbe.Trace.Clear();
            var first = _manager.ShutdownAsync().AsTask();
            var second = _manager.ShutdownAsync().AsTask();
            await first;
            await second;
            Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea", "release:game" }));
            Assert.That(nested.isLoaded || child.isLoaded || primary.isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes, Is.Empty);
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_commonInstaller.ReleaseCount, Is.Zero);
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Stopped));
        });

        [UnityTest]
        public IEnumerator ShutdownWaitsForLateAddedCandidateAndThenReleasesRegisteredTree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _loader.Gate = new UniTaskCompletionSource();
            var add = _manager.AddDerivedAsync(Nested, child).AsTask();
            SceneRootInstallerProbe.Trace.Clear();
            var shutdown = _manager.ShutdownAsync().AsTask();
            Assert.That(shutdown.IsCompleted, Is.False);
            _loader.Gate.TrySetResult();
            Exception failure = null;
            try { await add; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            await shutdown;
            Assert.That(SceneManager.GetSceneByPath(Nested).isLoaded, Is.False);
            Assert.That(child.isLoaded || primary.isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes, Is.Empty);
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea", "release:game" }));
            Assert.That(_common.IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Stopped));
        });

        [UnityTest]
        public IEnumerator AddCompletionAwaiterCanStartNextAddWithoutChangingFirstCommandScene() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var first = await _manager.AddDerivedAsync(Area, _manager.GameScene);
            var next = _manager.AddDerivedAsync(Nested, first).AsTask();
            Assert.That(first.path, Is.EqualTo(Area));
            var second = await next;
            Assert.That(second.path, Is.EqualTo(Nested));
            Assert.That(_manager.RegisteredScenes.Single(item => item.Scene == second).Parent, Is.EqualTo(first));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator RemovalReleaseFailureCleansActualSubtreeAndBlocksFurtherWork() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            Installer(nested).FailRelease = true;
            Exception failure = null;
            try { await _manager.RemoveDerivedAsync(child, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("root-release:NestedArea"));
            Assert.That(child.isLoaded || nested.isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary }));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary }));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            int covers = _callbacks.CoverCount;
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, primary));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(covers));
        });

        [UnityTest]
        public IEnumerator RemovalUnloadFailureKeepsActualRegistrationAndStickyShutdownError() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _loader.FailUnloadPath = Area;
            Exception failure = null;
            try { await _manager.RemoveDerivedAsync(child, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("area-unload"));
            Assert.That(child.isLoaded, Is.True);
            var remaining = _manager.RegisteredScenes.Single(item => item.Scene == child);
            Assert.That(remaining.Parent, Is.EqualTo(primary));
            Assert.That(remaining.IsPrepared, Is.False);
            Assert.That(remaining.IsShuttingDown, Is.True);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary, child }));
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Nested, primary));
            Exception shutdownFailure = null;
            try { await _manager.ShutdownAsync(); } catch (Exception exception) { shutdownFailure = exception; }
            Assert.That(shutdownFailure, Is.TypeOf<AggregateException>());
            Assert.That(shutdownFailure.ToString(), Does.Contain("area-unload"));
            Assert.That(_loader.UnloadCount, Is.EqualTo(2));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { child }));
            Assert.That(_manager.RegisteredScenes.Single().Scene, Is.EqualTo(child));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator OwnerCancellationDuringRemovalWaitsForEntireBegunSubtreeCleanup() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            _releaseGate = Installer(nested).ReleaseGate = new UniTaskCompletionSource();
            var remove = _manager.RemoveDerivedAsync(child, primary).AsTask();
            _manager.CancelTransition();
            Assert.That(remove.IsCompleted, Is.False);
            Assert.That(nested.isLoaded && child.isLoaded, Is.True);
            _releaseGate.TrySetResult();
            Exception failure = null;
            try { await remove; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(nested.isLoaded || child.isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary }));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator SingleCancellationDuringDescendantReleaseFinishesGracefulTreeWithoutNativeLoad() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(true);
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            _releaseGate = Installer(nested).ReleaseGate = new UniTaskCompletionSource();
            SceneRootInstallerProbe.Trace.Clear();
            var replace = _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single).AsTask();
            _manager.CancelTransition();
            Assert.That(replace.IsCompleted, Is.False);
            Assert.That(_loader.LoadCount, Is.EqualTo(3));
            _releaseGate.TrySetResult();
            Exception failure = null;
            try { await replace; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_loader.LoadCount, Is.EqualTo(3));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea", "release:game" }));
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary, child, nested }));
            Assert.That(_manager.RegisteredScenes.All(item => !item.IsPrepared && item.IsShuttingDown), Is.True);
            Assert.That(_manager.GameScene.IsValid(), Is.False);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(3));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator ExternalActiveChangeDuringAddPresentationFaultsAndRetainsExistingTree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            _callbacks.Presenting = () => SceneManager.SetActiveScene(child);
            Exception failure = null;
            try { await _manager.AddDerivedAsync(Nested, child); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("active scene changed"));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(child));
            Assert.That(SceneManager.GetSceneByPath(Nested).isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, child }));
            Assert.That(Root(primary).IsPrepared && Root(child).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator RemovalPreservesActiveSiblingOutsideTargetSubtree() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var sibling = await _manager.AddDerivedAsync(Nested, primary, activate: true);
            await _manager.RemoveDerivedAsync(child, primary);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(sibling));
            Assert.That(Root(sibling).IsPrepared, Is.True);
            Assert.That(_manager.GameScene, Is.EqualTo(primary));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator ExternalActiveChangeDuringRemovalIsReportedAfterCleanup() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var sibling = await _manager.AddDerivedAsync(Nested, primary, activate: true);
            Installer(child).Releasing = () => SceneManager.SetActiveScene(primary);
            Exception failure = null;
            try { await _manager.RemoveDerivedAsync(child, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("active scene changed"));
            Assert.That(child.isLoaded, Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(primary));
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, sibling }));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
        });

        [UnityTest]
        public IEnumerator ForeignInventoryDuringRemovalIsReportedWithoutDeletingForeignScene() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            Installer(child).Releasing = () => _foreign = SceneManager.CreateScene("ForeignDuringAreaRemoval");
            Exception failure = null;
            try { await _manager.RemoveDerivedAsync(child, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("Loaded scenes changed"));
            Assert.That(child.isLoaded, Is.False);
            Assert.That(_foreign.isLoaded, Is.True);
            Assert.That(_manager.RegisteredScenes.Single().Scene, Is.EqualTo(primary));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator SameRemovalSharesCompletionAfterUnloadWhileRevealIsPending() => UniTask.ToCoroutine(async () =>
        {
            await CreateEnteredManagerAsync(revealGate: true);
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var sibling = await _manager.AddDerivedAsync(Nested, primary);
            var installer = Installer(child);
            _revealCallbacks.RevealGate = new UniTaskCompletionSource();
            var first = _manager.RemoveDerivedAsync(child, primary).AsTask();
            await UniTask.WaitUntil(() => !child.isLoaded && _revealCallbacks.RevealCount == 4)
                .Timeout(TimeSpan.FromSeconds(10));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Revealing));
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(_manager.RegisteredScenes.Select(item => item.Scene), Is.EquivalentTo(new[] { primary, sibling }));
            Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(child, sibling));
            Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(child, default));
            var self = _manager.RemoveDerivedAsync(child, child).AsTask();
            var ancestor = _manager.RemoveDerivedAsync(child, primary).AsTask();
            Assert.That(self.IsCompleted || ancestor.IsCompleted, Is.False);
            Assert.That(installer.ReleaseCount, Is.EqualTo(1));
            Assert.That(_revealCallbacks.RevealCount, Is.EqualTo(4));
            _revealCallbacks.RevealGate.TrySetResult();
            await first;
            await self;
            await ancestor;
            Assert.That(_manager.CanProceed, Is.True);
            Assert.That(_revealCallbacks.FailureCount, Is.Zero);
        });

        private async UniTask ReplaceTreeAsync(LoadSceneMode mode)
        {
            await CreateEnteredManagerAsync(mode == LoadSceneMode.Single);
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            SceneRootInstallerProbe.Trace.Clear();
            if (mode == LoadSceneMode.Single)
                _loader.BeforeLoad = () => Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea", "release:game" }));
            await _manager.ReplacePrimaryAsync(Main, mode);
            Assert.That(ReleaseTrace(), Is.EqualTo(new[] { "release:NestedArea", "release:DerivedArea", "release:game" }));
            Assert.That(nested.isLoaded || child.isLoaded || primary.isLoaded, Is.False);
            var registration = _manager.RegisteredScenes.Single();
            Assert.That(registration.Scene, Is.EqualTo(_manager.GameScene));
            Assert.That(registration.Scene.path, Is.EqualTo(Main));
            Assert.That(registration.Role, Is.EqualTo(SceneRegistrationRole.Primary));
            Assert.That(registration.Parent, Is.EqualTo(default(Scene)));
            Assert.That(_commonInstaller.ReleaseCount, Is.Zero);
            Assert.That(_manager.CanProceed, Is.True);
        }

        private async UniTask CreateEnteredManagerAsync(bool single = false, bool revealGate = false)
        {
            _host = new GameObject("AreaCommon");
            _host.SetActive(false);
            _commonInstaller = _host.AddComponent<SceneRootInstallerProbe>();
            _commonInstaller.Id = "area-common";
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(new SceneRootInstaller[] { _commonInstaller }, single);
            SceneTransitionCallbacks callbacks;
            if (revealGate)
            {
                _revealCallbacks = _host.AddComponent<SceneAreaRevealGateCallbacks>();
                callbacks = _revealCallbacks;
            }
            else
            {
                _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
                callbacks = _callbacks;
            }
            _host.SetActive(true);
            _loader = new TrackingLoader();
            _manager = new GameSceneManager(_common, callbacks, _loader);
            await _manager.EnterFirstSceneAsync(Hub, single ? LoadSceneMode.Single : LoadSceneMode.Additive);
        }

        private UniTask WaitForPresentationAsync(string path) => UniTask.WaitUntil(() => _callbacks.PresentationRoot != null &&
            _callbacks.PresentationRoot.RootObject != null && _callbacks.PresentationRoot.RootObject.scene.path == path).Timeout(TimeSpan.FromSeconds(10));
        private static bool IsFixture(Scene scene) => new[] { Hub, Main, Area, Nested }.Contains(scene.path);
        private static SceneOwnedRoot Root(Scene scene) => scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Single(root => root != null);
        private static SceneRootInstallerProbe Installer(Scene scene) => Root(scene).GetComponent<SceneRootInstallerProbe>();
        private static string[] ReleaseTrace() => SceneRootInstallerProbe.Trace.Where(item => item.StartsWith("release:", StringComparison.Ordinal)).ToArray();

        private sealed class TrackingLoader : ISceneLoader
        {
            private readonly NativeSceneLoader _native = new NativeSceneLoader();
            internal int LoadCount;
            internal int UnloadCount;
            internal string FailUnloadPath;
            internal UniTaskCompletionSource Gate;
            internal Action BeforeLoad;
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
                    if (target.ScenePath == FailUnloadPath) throw new InvalidOperationException("area-unload");
                    await result.UnloadAsync();
                });
            }
        }
    }

    public sealed class SceneAreaRevealGateCallbacks : SceneTransitionCallbacks
    {
        [NonSerialized] public UniTaskCompletionSource RevealGate;
        public int RevealCount;
        public int FailureCount;

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            ++RevealCount;
            return RevealGate == null ? UniTask.CompletedTask : RevealGate.Task.AttachExternalCancellation(cancellationToken);
        }

        public override void OnFailure(Exception exception) => ++FailureCount;
    }
}
