using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class GameSceneManagerSafetyNetBTests
    {
        public enum InvalidInput { Kind, Mode, RequiredIds, Destination }
        private static readonly InvalidInput[] InvalidInputs = (InvalidInput[])Enum.GetValues(typeof(InvalidInput));
        private const string Hub = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Area = "Assets/TPLab/Tests/Fixtures/DerivedArea.unity";
        private const string ModeMessage = "Select a valid load mode; derived commands require Additive.";
        private const string IdMessage = "Required condition IDs must be nonempty and unique.";
        private const string DestinationMessage = "Load destinations are acquired by the owner, not supplied as an existing instance.";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private GameSceneManager _manager;
        private GameSceneSafetyNetCallbacks _recorder;
        private SceneTransitionCallbacksProbe _failureCallbacks;
        private Scene _initialActive;
        private string[] _initialTrace;
        private readonly List<string> _backend = new List<string>();
        private readonly List<string> _native = new List<string>();

        private sealed class ObservedLoader : ISceneLoader
        {
            private readonly ISceneLoader _inner;
            private readonly List<string> _calls;
            private readonly string _id;

            internal ObservedLoader(ISceneLoader inner, List<string> calls, string id)
            {
                _inner = inner;
                _calls = calls;
                _id = id;
            }

            public void Validate(SceneTarget target)
            {
                _calls.Add(_id + ":validate");
                _inner.Validate(target);
            }

            public async UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)
            {
                _calls.Add(_id + ":load");
                var loaded = await _inner.LoadAsync(target, mode);
                return new LoadedScene(loaded.Target, loaded.Scene, async () =>
                {
                    _calls.Add(_id + ":unload");
                    await loaded.UnloadAsync();
                });
            }
        }

        [SetUp]
        public void SetUp()
        {
            _initialActive = SceneManager.GetActiveScene();
            _initialTrace = SceneRootInstallerProbe.Trace.ToArray();
            _backend.Clear();
            _native.Clear();
            SceneManager.sceneLoaded += Loaded;
            SceneManager.sceneUnloaded += Unloaded;
            SceneManager.activeSceneChanged += ActiveChanged;
        }

        private void Create(bool failureCallbacks = false)
        {
            _host = new GameObject("SceneSafetyNetBCommon");
            _host.SetActive(false);
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(Array.Empty<SceneRootInstaller>());
            SceneTransitionCallbacks callbacks;
            if (failureCallbacks)
            {
                _failureCallbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
                callbacks = _failureCallbacks;
            }
            else
            {
                _recorder = _host.AddComponent<GameSceneSafetyNetCallbacks>();
                callbacks = _recorder;
            }
            _host.SetActive(true);
            _manager = new GameSceneManager(_common, callbacks,
                new ObservedLoader(new NativeSceneLoader(), _backend, "build"),
                new ObservedLoader(new AddressableSceneLoader(), _backend, "addressable"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return CleanupAsync().ToCoroutine();
            LogAssert.NoUnexpectedReceived();
        }

        private async UniTask CleanupAsync()
        {
            _recorder?.CoverGate?.TrySetResult();
            _failureCallbacks?.CoverGate?.TrySetResult();
            _failureCallbacks?.PresentationGate?.TrySetResult();
            _failureCallbacks?.ProceedGate?.TrySetResult();
            try
            {
                if (_manager != null)
                {
                    var shutdown = _manager.ShutdownAsync().AsTask();
                    await Wait(shutdown);
                    await shutdown;
                }
                if (_common != null)
                {
                    var shutdown = _common.ShutdownAsync().AsTask();
                    await Wait(shutdown);
                    await shutdown;
                }
            }
            finally
            {
                SceneManager.sceneLoaded -= Loaded;
                SceneManager.sceneUnloaded -= Unloaded;
                SceneManager.activeSceneChanged -= ActiveChanged;
                if (_initialActive.IsValid() && _initialActive.isLoaded)
                {
                    SceneManager.SetActiveScene(_initialActive);
                }
                var host = _host;
                UnityEngine.Object.Destroy(host);
                await UniTask.NextFrame();
                Assert.That(host == null, Is.True);
                SceneRootInstallerProbe.Trace.Clear();
                SceneRootInstallerProbe.Trace.AddRange(_initialTrace);
                _host = null;
                _common = null;
                _manager = null;
                _recorder = null;
                _failureCallbacks = null;
            }
        }

        private void Loaded(Scene scene, LoadSceneMode mode) => _native.Add("load:" + scene.handle + ":" + mode);
        private void Unloaded(Scene scene) => _native.Add("unload:" + scene.handle);
        private void ActiveChanged(Scene before, Scene after) => _native.Add("active:" + before.handle + ":" + after.handle);

        private static async UniTask Wait(Task task)
        {
            var timer = Stopwatch.StartNew();
            for (int frame = 0; frame < 1000 && !task.IsCompleted && timer.ElapsedMilliseconds < 10000; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True, "Completion must settle within 1000 yields and 10 seconds.");
        }

        private static Exception Error(Task task)
        {
            try { task.GetAwaiter().GetResult(); return null; }
            catch (Exception error) { return error; }
        }

        private object[] Effects()
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < scenes.Length; ++i) scenes[i] = SceneManager.GetSceneAt(i);
            return new object[]
            {
                _manager.State, _manager.FailurePhase, _manager.LastFailure, _manager.Progress,
                _manager.GameScene, _manager.LoadedScene, _manager.OwnedSceneCount,
                _manager.OwnedScenes.ToArray(), _manager.RegisteredScenes.ToArray(),
                _manager.CanProceed, _manager.IsGamePrepared, _common.IsReady, _common.IsPrepared,
                _host.scene, _host.activeSelf, scenes, SceneManager.GetActiveScene(),
                _backend.ToArray(), _native.ToArray(), _recorder.Calls.ToArray(),
                _recorder.Progress.ToArray(), _recorder.Failures.ToArray(), SceneRootInstallerProbe.Trace.ToArray()
            };
        }

        private async UniTask EnterAndVerifyObservation()
        {
            var entry = _manager.EnterFirstSceneAsync(Hub).AsTask();
            await Wait(entry);
            await entry;
            Assert.That(_manager.CanProceed, Is.True);
            Assert.That(_manager.GameScene.path, Is.EqualTo(Hub));
            Assert.That(_manager.OwnedSceneCount, Is.EqualTo(1));
            Assert.That(_backend, Does.Contain("build:validate").And.Contain("build:load"));
            Assert.That(_recorder.Calls, Does.Contain("cover").And.Contain("configure").And.Contain("presentation").And.Contain("reveal"));
            Assert.That(_recorder.Progress, Is.Not.Empty);
            Assert.That(_native.Any(value => value.StartsWith("load:", StringComparison.Ordinal)), Is.True);
            var shutdown = _manager.ShutdownAsync().AsTask();
            await Wait(shutdown);
            await shutdown;
            Assert.That(_backend, Does.Contain("build:unload"));
            Assert.That(_native.Any(value => value.StartsWith("unload:", StringComparison.Ordinal)), Is.True);
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_manager.RegisteredScenes, Is.Empty);
            Assert.That(_common.IsPrepared, Is.True, "The manager borrows the common lifecycle.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ExplicitRequestRejectsInvalidInputWithoutEffects([ValueSource(nameof(InvalidInputs))] InvalidInput input)
        {
            return CheckInvalid(input).ToCoroutine();
        }

        private async UniTask CheckInvalid(InvalidInput input)
        {
            Create();
            SceneTransitionRequest request;
            ArgumentException expected;
            switch (input)
            {
                case InvalidInput.Kind:
                    request = new SceneTransitionRequest((SceneTransitionKind)99, mode: (LoadSceneMode)99);
                    expected = new ArgumentOutOfRangeException("kind");
                    break;
                case InvalidInput.Mode:
                    request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, mode: (LoadSceneMode)99,
                        requiredConditionIds: new[] { "duplicate", "duplicate" });
                    expected = new ArgumentException(ModeMessage, "mode");
                    break;
                case InvalidInput.RequiredIds:
                    request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, destinationScene: _host.scene,
                        requiredConditionIds: new[] { "duplicate", "duplicate" });
                    expected = new ArgumentException(IdMessage);
                    break;
                default:
                    request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, destinationScene: _host.scene);
                    expected = new ArgumentException(DestinationMessage);
                    break;
            }
            var before = Effects();
            var actual = (ArgumentException)Assert.Throws(expected.GetType(), () => _manager.TryTransitionAsync(request));
            Assert.That(actual.Message, Is.EqualTo(expected.Message));
            Assert.That(actual.ParamName, Is.EqualTo(expected.ParamName));
            Assert.That(Effects(), Is.EqualTo(before));
            await UniTask.NextFrame();
            Assert.That(Effects(), Is.EqualTo(before));
            Assert.That(Assert.Throws<InvalidOperationException>(() => _manager.WaitForEntryAsync()).Message,
                Is.EqualTo("Start first entry before waiting for it."));
            Assert.That(Assert.Throws<InvalidOperationException>(() => _manager.WaitForTransitionAsync()).Message,
                Is.EqualTo("Start a transition before waiting for it."));
            await EnterAndVerifyObservation();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PolicyEvaluatesEveryConditionInAttachmentOrderAfterFalse()
        {
            return CheckPolicy().ToCoroutine();
        }

        private async UniTask CheckPolicy()
        {
            Create();
            var trace = new List<string>();
            var conditions = new SceneConditionProbe[3];
            for (int i = 0; i < conditions.Length; ++i)
            {
                var condition = _host.AddComponent<SceneConditionProbe>();
                condition.Id = "condition-" + i;
                condition.Allowed = i == 1;
                condition.Evaluating = _ => trace.Add(condition.Id);
                conditions[i] = condition;
            }
            var before = Effects();
            // Backend validation precedes business policy; load/unload and all other effects stay forbidden.
            before[17] = new[] { "build:validate" };
            var request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, SceneTarget.BuildScene(Hub));
            var evaluation = _manager.TryTransitionAsync(request).AsTask();
            await Wait(evaluation);
            Assert.That(await evaluation, Is.False);
            Assert.That(_backend, Is.EqualTo(new[] { "build:validate" }));
            Assert.That(trace, Is.EqualTo(new[] { "condition-0", "condition-1", "condition-2" }));
            Assert.That(conditions.Select(condition => condition.Calls), Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(Effects(), Is.EqualTo(before));
            await UniTask.NextFrame();
            Assert.That(conditions.Select(condition => condition.Calls), Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(Effects(), Is.EqualTo(before));
            foreach (var condition in conditions) condition.Allowed = true;
            await EnterAndVerifyObservation();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory()
        {
            return CheckFailures().ToCoroutine();
        }

        private async UniTask CheckFailures()
        {
            Create(failureCallbacks: true);
            var transitionError = new InvalidOperationException("scene-b-transition");
            var releaseError = new ApplicationException("scene-b-loading-release");
            _failureCallbacks.LoadingEnabled = true;
            _failureCallbacks.Presenting = () => throw transitionError;
            _failureCallbacks.LoadingReleasing = () => throw releaseError;
            _failureCallbacks.FailFailureCallback = true;
            var transition = _manager.TryTransitionAsync(new SceneTransitionRequest(
                SceneTransitionKind.FirstEntry, SceneTarget.BuildScene(Hub))).AsTask();
            await Wait(transition);
            var actual = Error(transition);
            Assert.That(actual, Is.TypeOf<AggregateException>());
            var ownerError = (AggregateException)actual;
            Assert.That(_manager.LastFailure, Is.SameAs(ownerError));
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_manager.FailurePhase, Is.EqualTo(SceneTransitionState.PreparingPresentation));
            Assert.That(ownerError.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(ownerError.InnerExceptions[0], Is.SameAs(_failureCallbacks.Failure));
            Assert.That(_failureCallbacks.Failure, Is.TypeOf<AggregateException>());
            var originalFailures = (AggregateException)_failureCallbacks.Failure;
            Assert.That(originalFailures.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(originalFailures.InnerExceptions[0], Is.SameAs(transitionError));
            Assert.That(originalFailures.InnerExceptions[1], Is.SameAs(releaseError));
            Assert.That(ownerError.InnerExceptions[1], Is.TypeOf<InvalidOperationException>());
            Assert.That(ownerError.InnerExceptions[1].Message, Is.EqualTo("failure-callback"));
            var entryHistory = _manager.WaitForEntryAsync().AsTask();
            var transitionHistory = _manager.WaitForTransitionAsync().AsTask();
            await Wait(entryHistory);
            await Wait(transitionHistory);
            Assert.That(Error(entryHistory), Is.SameAs(ownerError));
            Assert.That(Error(transitionHistory), Is.SameAs(ownerError));
            Assert.That(_failureCallbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_failureCallbacks.LoadingReleaseCount, Is.EqualTo(1));
            Assert.That(_failureCallbacks.CoverCount, Is.EqualTo(2));
            Assert.That(_failureCallbacks.Covered, Is.True);
            Assert.That(_backend.Count(value => value == "build:load"), Is.EqualTo(1));
            Assert.That(_backend.Count(value => value == "build:unload"), Is.EqualTo(1));
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_manager.RegisteredScenes, Is.Empty);
            Assert.That(_manager.GameScene, Is.EqualTo(default(Scene)));
            Assert.That(_manager.LoadedScene, Is.EqualTo(default(Scene)));
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(_common.IsReady && _common.IsPrepared, Is.True);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RegisteredReadFailureClearsPartialContentsWithoutRestoringExpandedCapacity()
        {
            return CheckCapacity().ToCoroutine();
        }

        private async UniTask CheckCapacity()
        {
            Create();
            var entry = _manager.EnterFirstSceneAsync(Hub).AsTask();
            await Wait(entry);
            await entry;
            var adding = _manager.AddDerivedAsync(Area, _manager.GameScene).AsTask();
            await Wait(adding);
            await adding;
            var before = Effects();
            var owners = (IList)typeof(GameSceneManager).GetField("_derived", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
            var owner = owners[0];
            var rootField = owner.GetType().GetField("Root", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = (ISceneRoot)rootField.GetValue(owner);
            var probe = original.RootObject.AddComponent<SceneReadinessFaultProbe>();
            probe.Wrapped = original;
            var expected = new InvalidOperationException("scene-b-readiness");
            probe.Failure = expected;
            var buffer = new List<SceneRegistration>(0);
            int partialCount = -1;
            int expandedCapacity = -1;
            probe.Reading = () => { partialCount = buffer.Count; expandedCapacity = buffer.Capacity; };
            rootField.SetValue(owner, probe);
            try
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => _manager.GetRegisteredScenes(buffer)), Is.SameAs(expected));
                Assert.That(partialCount, Is.EqualTo(1));
                Assert.That(expandedCapacity, Is.GreaterThan(0), "The primary row must have expanded the initially empty buffer.");
                Assert.That(buffer, Is.Empty);
                Assert.That(buffer.Capacity, Is.EqualTo(expandedCapacity), "Failure clears contents without restoring old capacity.");
            }
            finally
            {
                rootField.SetValue(owner, original);
                UnityEngine.Object.Destroy(probe);
                await UniTask.NextFrame();
            }
            Assert.That(Effects(), Is.EqualTo(before));
            _manager.GetRegisteredScenes(buffer);
            Assert.That(buffer, Is.EqualTo(_manager.RegisteredScenes));
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.Capacity, Is.EqualTo(expandedCapacity));
        }
    }
}
