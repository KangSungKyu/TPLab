using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class SceneTransitionPolicyTests
    {
        private const string Hub = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/TPLab/Tests/Fixtures/ReplacementMain.unity";
        private const string Area = "Assets/TPLab/Tests/Fixtures/DerivedArea.unity";
        private const string Nested = "Assets/TPLab/Tests/Fixtures/NestedArea.unity";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneConditionProbe _commonCondition;
        private SceneTransitionCallbacksProbe _callbacks;
        private SceneAreaRevealGateCallbacks _revealCallbacks;
        private GameSceneManager _manager;
        private BootstrapSystem _bootstrap;
        private TrackingLoader _loader;
        private readonly List<SceneTransitionSettings> _settings = new List<SceneTransitionSettings>();

        [SetUp]
        public void SetUp()
        {
            _host = null;
            _common = null;
            _commonCondition = null;
            _callbacks = null;
            _revealCallbacks = null;
            _manager = null;
            _bootstrap = null;
            _loader = null;
            _settings.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            _callbacks?.PresentationGate?.TrySetResult();
            _callbacks?.CoverGate?.TrySetResult();
            _revealCallbacks?.RevealGate?.TrySetResult();
            if (SceneManager.sceneCount == 1 && IsFixture(SceneManager.GetSceneAt(0))) SceneManager.CreateScene("ScenePolicyTestRecovery");
            if (_manager != null) { try { await _manager.ShutdownAsync(); } catch (Exception) { } }
            if (_bootstrap != null) { try { await _bootstrap.ShutdownAsync(); } catch (Exception) { } }
            if (_common != null) await _common.ShutdownAsync();
            foreach (string path in new[] { Nested, Area, Main, Hub })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (SceneManager.sceneCount <= 1) SceneManager.CreateScene("ScenePolicyTestRecovery");
                foreach (var root in scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Where(root => root != null))
                { try { await root.ShutdownAsync(); } catch (Exception) { } }
                await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
            if (_host != null) UnityEngine.Object.Destroy(_host);
            foreach (var settings in _settings) UnityEngine.Object.Destroy(settings);
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator FirstCommonFalseRejectsBeforeAnyOperationOrEffects() => UniTask.ToCoroutine(async () =>
        {
            CreateManager();
            _commonCondition.Allowed = false;
            Assert.Throws<SceneTransitionRejectedException>(() => _manager.EnterFirstSceneAsync(Hub));
            Assert.That(_commonCondition.Calls, Is.EqualTo(1));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Idle));
            Assert.That(_manager.LastFailure, Is.Null);
            Assert.That(_callbacks.CoverCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_loader.LoadCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => _manager.WaitForEntryAsync());
            Assert.Throws<InvalidOperationException>(() => _manager.WaitForTransitionAsync());
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator FirstEntryTryFalseDoesNotConsumeHistoricalEntry() => UniTask.ToCoroutine(async () =>
        {
            CreateManager();
            _commonCondition.Allowed = false;
            var request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, SceneTarget.BuildScene(Hub));
            Assert.That(await _manager.TryTransitionAsync(request), Is.False);
            Assert.That(_callbacks.CoverCount, Is.Zero);
            _commonCondition.Allowed = true;
            Assert.That(await _manager.TryTransitionAsync(request), Is.True);
            Assert.That(_manager.CanProceed, Is.True);
            await _manager.WaitForEntryAsync();
        });

        [UnityTest]
        public IEnumerator DirectAndDefinitionRequestsEvaluateTheSameAttachedParentPolicy() => UniTask.ToCoroutine(async () =>
        {
            var settings = Settings(new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, Hub, SceneTarget.BuildScene(Area)));
            CreateManager(settings: settings);
            await _manager.EnterFirstSceneAsync(Hub);
            var parent = _manager.GameScene;
            var condition = Condition(parent, "permission", false);
            var direct = new SceneTransitionRequest(SceneTransitionKind.AddDerived, SceneTarget.BuildScene(Area), parent);
            Assert.That(await _manager.TryTransitionAsync("add"), Is.False);
            Assert.That(await _manager.TryTransitionAsync(direct), Is.False);
            Assert.Throws<SceneTransitionRejectedException>(() => _manager.AddDerivedAsync(Area, parent));
            Assert.That(condition.Calls, Is.EqualTo(3));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            condition.Allowed = true;
            Assert.That(await _manager.TryTransitionAsync("add"), Is.True);
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.True);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DefinitionRemoveAndReplaceResolveActualRegisteredInstances()
            => UniTask.ToCoroutine(async () =>
        {
            var settings = Settings(
                new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, Hub, SceneTarget.BuildScene(Area)),
                new SceneTransitionDefinition("remove", SceneTransitionKind.RemoveDerived, Hub, SceneTarget.BuildScene(Area)),
                new SceneTransitionDefinition("replace", SceneTransitionKind.ReplacePrimary, Hub, SceneTarget.BuildScene(Main)));
            CreateManager(settings: settings);
            await _manager.EnterFirstSceneAsync(Hub);
            var primary = _manager.GameScene;
            var policy = Condition(primary, "permission", true);
            Assert.That(await _manager.TryTransitionAsync("add"), Is.True);
            var child = SceneManager.GetSceneByPath(Area);
            Assert.That(await _manager.TryTransitionAsync("remove"), Is.True);
            Assert.That(child.isLoaded, Is.False);
            Assert.That(await _manager.TryTransitionAsync("replace"), Is.True);
            Assert.That(_manager.GameScene.path, Is.EqualTo(Main));
            Assert.That(policy.Calls, Is.EqualTo(7));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DisabledAttachedConditionIsStillPolicyWhenRequiredIdsAreEmpty() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var condition = Condition(_manager.GameScene, "disabled", false);
            condition.enabled = false;
            Assert.Throws<SceneTransitionRejectedException>(() => _manager.ReplacePrimaryAsync(Main));
            Assert.That(condition.Calls, Is.EqualTo(1));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator OneFalseDescendantRejectsPrimaryReplacementBeforeAnyRelease() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            Condition(primary, "permission", true);
            Condition(child, "permission", true);
            Condition(nested, "permission", false);
            Assert.Throws<SceneTransitionRejectedException>(() => _manager.ReplacePrimaryAsync(Main));
            Assert.That(Installer(primary).ReleaseCount + Installer(child).ReleaseCount + Installer(nested).ReleaseCount, Is.Zero);
            Assert.That(_manager.RegisteredScenes.Count, Is.EqualTo(3));
            Assert.That(_manager.CanProceed, Is.True);
            Assert.That(_callbacks.FailureCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator RequiredIdCanBeProvidedByAnyAffectedRootAndDuplicateAcrossRootsIsLegal() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var primaryPolicy = Condition(primary, "permission", true);
            var childPolicy = Condition(child, "permission", true);
            var request = new SceneTransitionRequest(SceneTransitionKind.RemoveDerived, sourceScene: primary, destinationScene: child,
                requiredConditionIds: new[] { "permission" });
            Assert.That(await _manager.TryTransitionAsync(request), Is.True);
            Assert.That(primaryPolicy.Calls, Is.EqualTo(2));
            Assert.That(childPolicy.Calls, Is.EqualTo(2));
            Assert.That(primaryPolicy.LastContext.Request.Kind, Is.EqualTo(SceneTransitionKind.RemoveDerived));
            Assert.That(primaryPolicy.LastContext.SourceScene, Is.EqualTo(primary));
            Assert.That(primaryPolicy.LastContext.DestinationScene, Is.EqualTo(child));
            Assert.That(primaryPolicy.LastContext.AffectedScenes, Is.EquivalentTo(new[] { primary, child }));
        });

        [UnityTest]
        public IEnumerator MissingRequiredPolicyIsConfigurationErrorWithoutOperation() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var request = new SceneTransitionRequest(SceneTransitionKind.AddDerived, SceneTarget.BuildScene(Area), _manager.GameScene,
                requiredConditionIds: new[] { "missing" });
            Exception failure = null;
            try { await _manager.TryTransitionAsync(request); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.ToString(), Does.Contain("missing"));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DirectRequiredIdsRejectEmptyAndDuplicateConfiguration() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            Condition(_manager.GameScene, "permission", true);
            foreach (var required in new[] { new[] { "" }, new[] { "permission", "permission" } })
            {
                var request = new SceneTransitionRequest(SceneTransitionKind.AddDerived, SceneTarget.BuildScene(Area), _manager.GameScene,
                    requiredConditionIds: required);
                Exception failure = null;
                try { await _manager.TryTransitionAsync(request); } catch (Exception exception) { failure = exception; }
                Assert.That(failure, Is.InstanceOf<ArgumentException>());
            }
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator DuplicatePolicyIdsWithinOneRootAreConfigurationError() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            Condition(_manager.GameScene, "duplicate", true);
            Condition(_manager.GameScene, "duplicate", true);
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, _manager.GameScene));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator EmptyAttachedConditionIdIsConfigurationError() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            Condition(_manager.GameScene, "", true);
            Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, _manager.GameScene));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator ConditionExceptionRemainsVisibleBeforeEffects() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var policy = Condition(_manager.GameScene, "throwing", true);
            policy.Failure = new ApplicationException("policy-evaluate");
            Exception failure = null;
            try { await _manager.TryTransitionAsync(new SceneTransitionRequest(SceneTransitionKind.AddDerived, SceneTarget.BuildScene(Area), _manager.GameScene)); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.SameAs(policy.Failure));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_manager.LastFailure, Is.Null);
        });

        [UnityTest]
        public IEnumerator ConditionsCannotReenterEntryCancelShutdownWaitOrNewCommands() => UniTask.ToCoroutine(async () =>
        {
            CreateManager();
            _commonCondition.Evaluating = context =>
            {
                Assert.Throws<InvalidOperationException>(() => _manager.EnterFirstSceneAsync(Hub));
                Assert.Throws<InvalidOperationException>(() => _manager.CancelTransition());
                Assert.Throws<InvalidOperationException>(() => _manager.ShutdownAsync());
                Assert.Throws<InvalidOperationException>(() => _manager.WaitForEntryAsync());
                Assert.Throws<InvalidOperationException>(() => _manager.WaitForTransitionAsync());
                Assert.Throws<InvalidOperationException>(() => _manager.ReplacePrimaryAsync(Main));
                Assert.Throws<InvalidOperationException>(() => _manager.AddDerivedAsync(Area, default));
                Assert.Throws<InvalidOperationException>(() => _manager.RemoveDerivedAsync(default, default));
                Assert.Throws<InvalidOperationException>(() => _manager.TryTransitionAsync("anything"));
                Assert.Throws<InvalidOperationException>(() => _manager.TryTransitionAsync(context.Request));
            };
            await _manager.EnterFirstSceneAsync(Hub);
            Assert.That(_commonCondition.Calls, Is.EqualTo(3));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator SettingsSnapshotIsUnaffectedByAssetChangeAfterManagerConstruction() => UniTask.ToCoroutine(async () =>
        {
            var settings = Settings(new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, Hub, SceneTarget.BuildScene(Area)));
            CreateManager(settings: settings);
            settings.Configure(new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, Hub, SceneTarget.BuildScene(Main)));
            await _manager.EnterFirstSceneAsync(Hub);
            Assert.That(await _manager.TryTransitionAsync("add"), Is.True);
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
        });

        [UnityTest]
        public IEnumerator MissingSettingsOrUnknownIdThrowsWithoutCover() => UniTask.ToCoroutine(async () =>
        {
            CreateManager();
            Exception failure = null;
            try { await _manager.TryTransitionAsync("missing"); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(_callbacks.CoverCount, Is.Zero);
            Assert.That(_loader.LoadCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator UnknownDefinitionIdIsConfigurationErrorWithoutEntryEffects() => UniTask.ToCoroutine(async () =>
        {
            CreateManager(settings: Settings(new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, "", SceneTarget.BuildScene(Hub))));
            Exception failure = null;
            try { await _manager.TryTransitionAsync("unknown"); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(_callbacks.CoverCount, Is.Zero);
            Assert.That(_loader.LoadCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator CallbackRejectedExceptionIsExecutionFailureAndNeverTryFalse() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var rejected = new SceneTransitionRejectedException("callback-refusal");
            _callbacks.Configuring = () => throw rejected;
            Exception failure = null;
            try { await _manager.TryTransitionAsync(new SceneTransitionRequest(SceneTransitionKind.ReplacePrimary, SceneTarget.BuildScene(Main), _manager.GameScene)); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.SameAs(rejected));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_manager.LastFailure, Is.SameAs(rejected));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
        });

        [UnityTest]
        public IEnumerator ChangedConditionIdAfterApprovalIsConfigurationErrorWithCandidateCleanup() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var policy = Condition(primary, "stable", true);
            _callbacks.Presenting = () => policy.Id = "changed";
            Exception failure = null;
            try { await _manager.AddDerivedAsync(Area, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.False);
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
        });

        [UnityTest]
        public IEnumerator AddedAttachedConditionAfterApprovalIsConfigurationError() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            _callbacks.Presenting = () => Condition(primary, "late-attached", true);
            Exception failure = null;
            try { await _manager.AddDerivedAsync(Area, primary); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.False);
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
        });

        [UnityTest]
        public IEnumerator LateFalseDuringAddCleansCandidateBeforeSuccessPublication() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var policy = Condition(primary, "business", true);
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var add = _manager.AddDerivedAsync(Area, primary).AsTask();
            await WaitForPresentationAsync(Area);
            policy.Allowed = false;
            _callbacks.PresentationGate.TrySetResult();
            Exception failure = null;
            try { await add; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(((OperationCanceledException)failure).CancellationToken.IsCancellationRequested, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Single().Scene, Is.EqualTo(primary));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
        });

        [UnityTest]
        public IEnumerator LateFalseDuringAsyncRevealCleansAddedCandidateAndRestoresCover() => UniTask.ToCoroutine(async () =>
        {
            CreateManager(revealGate: true);
            await _manager.EnterFirstSceneAsync(Hub);
            var primary = _manager.GameScene;
            var policy = Condition(primary, "business", true);
            _revealCallbacks.RevealGate = new UniTaskCompletionSource();
            var add = _manager.AddDerivedAsync(Area, primary).AsTask();
            await UniTask.WaitUntil(() => _manager.State == SceneTransitionState.Revealing && _revealCallbacks.RevealCount == 2)
                .Timeout(TimeSpan.FromSeconds(10));
            policy.Allowed = false;
            _revealCallbacks.RevealGate.TrySetResult();
            Exception failure = null;
            try { await add; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(((OperationCanceledException)failure).CancellationToken.IsCancellationRequested, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Area).isLoaded, Is.False);
            Assert.That(_manager.RegisteredScenes.Single().Scene, Is.EqualTo(primary));
            Assert.That(Root(primary).IsPrepared, Is.True);
            Assert.That(_manager.GameScene, Is.EqualTo(primary));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_revealCallbacks.CoverCount, Is.EqualTo(3));
            Assert.That(_revealCallbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LateFalseBeforeAdditiveReleaseRetainsPreparedOldSubtree() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var policy = Condition(child, "business", true);
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var replace = _manager.ReplacePrimaryAsync(Main).AsTask();
            await WaitForPresentationAsync(Main);
            policy.Allowed = false;
            _callbacks.PresentationGate.TrySetResult();
            Exception failure = null;
            try { await replace; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(Installer(primary).ReleaseCount + Installer(child).ReleaseCount, Is.Zero);
            Assert.That(Root(primary).IsPrepared && Root(child).IsPrepared, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Main).isLoaded, Is.False);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary, child }));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LateFalseBeforeSingleReleaseDoesNotShutdownOrLoad() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync(single: true);
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var policy = Condition(child, "business", true);
            _callbacks.CoverGate = new UniTaskCompletionSource();
            var replace = _manager.ReplacePrimaryAsync(Main, LoadSceneMode.Single).AsTask();
            policy.Allowed = false;
            _callbacks.CoverGate.TrySetResult();
            Exception failure = null;
            try { await replace; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_loader.LoadCount, Is.EqualTo(2));
            Assert.That(Installer(primary).ReleaseCount + Installer(child).ReleaseCount, Is.Zero);
            Assert.That(Root(primary).IsPrepared && Root(child).IsPrepared, Is.True);
            Assert.That(_manager.OwnedScenes, Is.EquivalentTo(new[] { primary, child }));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LateFalseBeforeRemovalRetainsEntirePreparedSubtree() => UniTask.ToCoroutine(async () =>
        {
            await EnterAsync();
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var nested = await _manager.AddDerivedAsync(Nested, child);
            var policy = Condition(nested, "business", true);
            _callbacks.CoverGate = new UniTaskCompletionSource();
            var remove = _manager.RemoveDerivedAsync(child, primary).AsTask();
            policy.Allowed = false;
            _callbacks.CoverGate.TrySetResult();
            Exception failure = null;
            try { await remove; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(Installer(child).ReleaseCount + Installer(nested).ReleaseCount, Is.Zero);
            Assert.That(_manager.RegisteredScenes.Count, Is.EqualTo(3));
            Assert.That(Root(child).IsPrepared && Root(nested).IsPrepared, Is.True);
            Assert.That(_callbacks.RevealCount, Is.EqualTo(3));
        });

        [UnityTest]
        public IEnumerator PendingRemovalRevealJoinDoesNotReevaluateDestroyedConditions() => UniTask.ToCoroutine(async () =>
        {
            CreateManager(revealGate: true);
            await _manager.EnterFirstSceneAsync(Hub);
            var primary = _manager.GameScene;
            var child = await _manager.AddDerivedAsync(Area, primary);
            var policy = Condition(child, "removed", true);
            var parentPolicy = Condition(primary, "parent", true);
            _revealCallbacks.RevealGate = new UniTaskCompletionSource();
            var first = _manager.RemoveDerivedAsync(child, primary).AsTask();
            await UniTask.WaitUntil(() => !child.isLoaded && _manager.State == SceneTransitionState.Revealing).Timeout(TimeSpan.FromSeconds(10));
            Assert.That(policy.Calls, Is.EqualTo(2));
            parentPolicy.Allowed = false;
            var self = _manager.RemoveDerivedAsync(child, child).AsTask();
            var ancestor = _manager.RemoveDerivedAsync(child, primary).AsTask();
            Assert.That(parentPolicy.Calls, Is.EqualTo(2));
            _revealCallbacks.RevealGate.TrySetResult();
            await first;
            await self;
            await ancestor;
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator BootstrapUsesSelectedFirstEntryDefinition() => UniTask.ToCoroutine(async () =>
        {
            var settings = Settings(new SceneTransitionDefinition("first", SceneTransitionKind.FirstEntry, "", SceneTarget.BuildScene(Hub)));
            CreateCommonHost(false);
            _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _bootstrap = _host.AddComponent<BootstrapSystem>();
            _bootstrap.Configure(_common, settings, "first", false, _callbacks);
            _host.SetActive(true);
            await _bootstrap.BootstrapAsync();
            _manager = _bootstrap.Manager;
            Assert.That(_bootstrap.Settings, Is.SameAs(settings));
            Assert.That(_bootstrap.FirstTransitionId, Is.EqualTo("first"));
            Assert.That(_manager.GameScene.path, Is.EqualTo(Hub));
            Assert.That(_manager.CanProceed, Is.True);
        });

        private void CreateCommonHost(bool persistent)
        {
            _host = new GameObject("PolicyCommon");
            _host.SetActive(false);
            var installer = _host.AddComponent<SceneRootInstallerProbe>();
            installer.Id = "policy-common";
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(new SceneRootInstaller[] { installer }, persistent);
            _commonCondition = _host.AddComponent<SceneConditionProbe>();
            _commonCondition.Id = "common";
        }

        private void CreateManager(bool single = false, SceneTransitionSettings settings = null, bool revealGate = false)
        {
            CreateCommonHost(single);
            SceneTransitionCallbacks callbacks;
            if (revealGate) { _revealCallbacks = _host.AddComponent<SceneAreaRevealGateCallbacks>(); callbacks = _revealCallbacks; }
            else { _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>(); callbacks = _callbacks; }
            _host.SetActive(true);
            _loader = new TrackingLoader();
            _manager = new GameSceneManager(_common, callbacks, _loader, settings: settings);
        }

        private async UniTask EnterAsync(bool single = false)
        {
            CreateManager(single);
            await _manager.EnterFirstSceneAsync(Hub, single ? LoadSceneMode.Single : LoadSceneMode.Additive);
        }

        private SceneTransitionSettings Settings(params SceneTransitionDefinition[] definitions)
        {
            var settings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
            _settings.Add(settings);
            settings.Configure(definitions);
            return settings;
        }

        private static SceneConditionProbe Condition(Scene scene, string id, bool allowed)
        {
            var condition = Root(scene).gameObject.AddComponent<SceneConditionProbe>();
            condition.Id = id;
            condition.Allowed = allowed;
            return condition;
        }

        private UniTask WaitForPresentationAsync(string path) => UniTask.WaitUntil(() => _callbacks.PresentationRoot != null &&
            _callbacks.PresentationRoot.RootObject != null && _callbacks.PresentationRoot.RootObject.scene.path == path).Timeout(TimeSpan.FromSeconds(10));
        private static bool IsFixture(Scene scene) => new[] { Hub, Main, Area, Nested }.Contains(scene.path);
        private static SceneOwnedRoot Root(Scene scene) => scene.GetRootGameObjects().Select(go => go.GetComponent<SceneOwnedRoot>()).Single(root => root != null);
        private static SceneRootInstallerProbe Installer(Scene scene) => Root(scene).GetComponent<SceneRootInstallerProbe>();

        private sealed class TrackingLoader : ISceneLoader
        {
            private readonly NativeSceneLoader _native = new NativeSceneLoader();
            internal int LoadCount;
            public void Validate(SceneTarget target) => _native.Validate(target);
            public UniTask<TPLab.Core.ResourceManagement.LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)
            { ++LoadCount; return _native.LoadAsync(target, mode); }
        }
    }

}
