using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
    public sealed class GameSceneManagerSafetyNetATests
    {
        public enum CommandOverload
        {
            EnterPath, EnterTarget, WaitEntry, ReplacePath, ReplaceTarget,
            AddPath, AddTarget, Remove, TryId, TryRequest, WaitTransition, Cancel, Shutdown
        }

        public enum BufferKind
        {
            Owned, Registered
        }

        private static readonly CommandOverload[] Commands =
            (CommandOverload[])Enum.GetValues(typeof(CommandOverload));
        private static readonly BufferKind[] Buffers = (BufferKind[])Enum.GetValues(typeof(BufferKind));
        private const string Hub = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/TPLab/Tests/Fixtures/ReplacementMain.unity";
        private const string ThreadMessage = "Scene management must be used on Unity's main thread.";
        private const string EntryHistoryMessage = "Start first entry before waiting for it.";
        private const string TransitionHistoryMessage = "Start a transition before waiting for it.";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneRootInstallerProbe _installer;
        private SceneConditionProbe _condition;
        private GameSceneSafetyNetCallbacks _callbacks;
        private TrackingLoader _loader;
        private SceneTransitionSettings _settings;
        private GameSceneManager _manager;
        private readonly List<string> _nativeEvents = new List<string>();
        private bool _eventsSubscribed;

        private sealed class TrackingLoader : ISceneLoader
        {
            private readonly NativeSceneLoader _native = new NativeSceneLoader();
            public int ValidateCount;
            public int LoadCount;
            public int UnloadCount;

            public void Validate(SceneTarget target)
            {
                ++ValidateCount;
                _native.Validate(target);
            }

            public async UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)
            {
                ++LoadCount;
                LoadedScene owned = await _native.LoadAsync(target, mode);
                return new LoadedScene(target, owned.Scene, async () =>
                {
                    ++UnloadCount;
                    await owned.UnloadAsync();
                });
            }
        }

        private sealed class WorkerObservation
        {
            public Exception Error;
            public bool Joined;
            public bool Pending;
        }

        private sealed class EffectSnapshot
        {
            public SceneTransitionState State;
            public SceneTransitionState FailurePhase;
            public Exception LastFailure;
            public SceneTransitionProgress? Progress;
            public Scene GameScene;
            public Scene LoadedScene;
            public Scene ActiveScene;
            public Scene CommonScene;
            public Scene[] Inventory;
            public Scene[] Owned;
            public SceneRegistration[] Registered;
            public int OwnedCount;
            public bool CanProceed;
            public bool IsGamePrepared;
            public bool CommonReady;
            public bool CommonPrepared;
            public bool CoverCancelled;
            public int[] Counts;
            public string[] Callbacks;
            public SceneTransitionProgress[] ProgressReports;
            public Exception[] Failures;
            public string[] NativeEvents;
            public string[] InstallerTrace;
            public string EntryHistory;
            public string TransitionHistory;
        }

        [SetUp]
        public void SetUp()
        {
            _nativeEvents.Clear();
            _host = new GameObject("SceneSafetyNetCommon");
            _host.SetActive(false);
            _installer = _host.AddComponent<SceneRootInstallerProbe>();
            _installer.Id = "scene-safety-net-common";
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(new SceneRootInstaller[] { _installer }, false);
            _condition = _host.AddComponent<SceneConditionProbe>();
            _condition.Id = "scene-safety-net-condition";
            _callbacks = _host.AddComponent<GameSceneSafetyNetCallbacks>();
            _host.SetActive(true);
            _loader = new TrackingLoader();
            _settings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
            _settings.Configure(new[]
            {
                new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, "", SceneTarget.BuildScene(Hub))
            });
            _manager = new GameSceneManager(_common, _callbacks, _loader, _loader, _settings);
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.sceneUnloaded += OnUnloaded;
            SceneManager.activeSceneChanged += OnActiveChanged;
            _eventsSubscribed = true;
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown() => CleanupAsync().ToCoroutine();

        private async UniTask CleanupAsync()
        {
            _callbacks?.CoverGate?.TrySetResult();
            UnsubscribeNativeEvents();
            try
            {
                if (_manager != null)
                {
                    try
                    {
                        await Complete(_manager.ShutdownAsync().AsTask());
                    }
                    catch (Exception)
                    {
                        // The test body owns rejection/failure assertions; continue native fixture cleanup.
                    }
                }
                if (_common != null)
                {
                    await Complete(_common.ShutdownAsync().AsTask());
                }
                Scene remaining = SceneManager.GetSceneByPath(Hub);
                if (remaining.IsValid() && remaining.isLoaded)
                {
                    await Complete(SceneManager.UnloadSceneAsync(remaining).ToUniTask().AsTask());
                }
            }
            finally
            {
                if (_host != null)
                {
                    UnityEngine.Object.Destroy(_host);
                }
                if (_settings != null)
                {
                    UnityEngine.Object.Destroy(_settings);
                }
                _manager = null;
                _host = null;
                _common = null;
                _callbacks = null;
                _settings = null;
            }
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        }

        private void OnLoaded(Scene scene, LoadSceneMode mode) => _nativeEvents.Add("loaded:" + scene.handle + ":" + mode);
        private void OnUnloaded(Scene scene) => _nativeEvents.Add("unloaded:" + scene.handle);
        private void OnActiveChanged(Scene before, Scene after) => _nativeEvents.Add("active:" + before.handle + ":" + after.handle);

        private void UnsubscribeNativeEvents()
        {
            if (!_eventsSubscribed)
            {
                return;
            }
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneUnloaded -= OnUnloaded;
            SceneManager.activeSceneChanged -= OnActiveChanged;
            _eventsSubscribed = false;
        }

        private static async UniTask Wait(Task task)
        {
            var clock = Stopwatch.StartNew();
            for (int frame = 0; frame < 100 && !task.IsCompleted && clock.ElapsedMilliseconds < 10000; ++frame)
            {
                await UniTask.Yield();
            }
            Assert.That(task.IsCompleted, Is.True, "Completion exceeded 100 yields or 10 seconds.");
        }

        private static async UniTask Complete(Task task)
        {
            await Wait(task);
            task.GetAwaiter().GetResult();
        }

        private static WorkerObservation OnWorker(Action<WorkerObservation> action)
        {
            var observation = new WorkerObservation();
            var thread = new Thread(() =>
            {
                try
                {
                    action(observation);
                }
                catch (Exception error)
                {
                    observation.Error = error;
                }
            }) { IsBackground = true };
            thread.Start();
            observation.Joined = thread.Join(3000);
            return observation;
        }

        private static void Observe(UniTask task, WorkerObservation observation)
        {
            var awaiter = task.GetAwaiter();
            observation.Pending = !awaiter.IsCompleted;
            if (!observation.Pending)
            {
                awaiter.GetResult();
            }
        }

        private static void Observe<T>(UniTask<T> task, WorkerObservation observation)
        {
            var awaiter = task.GetAwaiter();
            observation.Pending = !awaiter.IsCompleted;
            if (!observation.Pending)
            {
                awaiter.GetResult();
            }
        }

        private static void AssertThreadRejection(WorkerObservation observation)
        {
            Assert.That(observation.Joined, Is.True, "Worker did not join within 3000ms.");
            Assert.That(observation.Pending, Is.False);
            Assert.That(observation.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(observation.Error.Message, Is.EqualTo(ThreadMessage));
        }

        private static string DescribeWait(Func<UniTask> wait)
        {
            try
            {
                return wait().Status.ToString();
            }
            catch (Exception error)
            {
                return error.GetType().FullName + ":" + error.Message;
            }
        }

        private EffectSnapshot CaptureEffects()
        {
            var inventory = new Scene[SceneManager.sceneCount];
            for (int index = 0; index < inventory.Length; ++index)
            {
                inventory[index] = SceneManager.GetSceneAt(index);
            }
            var owned = new List<Scene>();
            var registered = new List<SceneRegistration>();
            _manager.GetOwnedScenes(owned);
            _manager.GetRegisteredScenes(registered);
            return new EffectSnapshot
            {
                State = _manager.State,
                FailurePhase = _manager.FailurePhase,
                LastFailure = _manager.LastFailure,
                Progress = _manager.Progress,
                GameScene = _manager.GameScene,
                LoadedScene = _manager.LoadedScene,
                ActiveScene = SceneManager.GetActiveScene(),
                CommonScene = _host.scene,
                Inventory = inventory,
                Owned = owned.ToArray(),
                Registered = registered.ToArray(),
                OwnedCount = _manager.OwnedSceneCount,
                CanProceed = _manager.CanProceed,
                IsGamePrepared = _manager.IsGamePrepared,
                CommonReady = _common.IsReady,
                CommonPrepared = _common.IsPrepared,
                CoverCancelled = _callbacks.CoverToken.IsCancellationRequested,
                Counts = new[] { _loader.ValidateCount, _loader.LoadCount, _loader.UnloadCount,
                    _installer.InstallCount, _installer.PrepareCount, _installer.ReleaseCount,
                    _installer.UninstallCount, _condition.Calls },
                Callbacks = _callbacks.Calls.ToArray(),
                ProgressReports = _callbacks.Progress.ToArray(),
                Failures = _callbacks.Failures.ToArray(),
                NativeEvents = _nativeEvents.ToArray(),
                InstallerTrace = SceneRootInstallerProbe.Trace.ToArray(),
                EntryHistory = DescribeWait(() => _manager.WaitForEntryAsync()),
                TransitionHistory = DescribeWait(() => _manager.WaitForTransitionAsync())
            };
        }

        private static void AssertEffectsUnchanged(EffectSnapshot before, EffectSnapshot after)
        {
            Assert.That(after.State, Is.EqualTo(before.State));
            Assert.That(after.FailurePhase, Is.EqualTo(before.FailurePhase));
            Assert.That(after.LastFailure, Is.SameAs(before.LastFailure));
            Assert.That(after.Progress, Is.EqualTo(before.Progress));
            Assert.That(after.GameScene, Is.EqualTo(before.GameScene));
            Assert.That(after.LoadedScene, Is.EqualTo(before.LoadedScene));
            Assert.That(after.ActiveScene, Is.EqualTo(before.ActiveScene));
            Assert.That(after.CommonScene, Is.EqualTo(before.CommonScene));
            Assert.That(after.Inventory, Is.EqualTo(before.Inventory));
            Assert.That(after.Owned, Is.EqualTo(before.Owned));
            Assert.That(after.Registered, Is.EqualTo(before.Registered));
            Assert.That(after.OwnedCount, Is.EqualTo(before.OwnedCount));
            Assert.That(after.CanProceed, Is.EqualTo(before.CanProceed));
            Assert.That(after.IsGamePrepared, Is.EqualTo(before.IsGamePrepared));
            Assert.That(after.CommonReady, Is.EqualTo(before.CommonReady));
            Assert.That(after.CommonPrepared, Is.EqualTo(before.CommonPrepared));
            Assert.That(after.CoverCancelled, Is.EqualTo(before.CoverCancelled));
            Assert.That(after.Counts, Is.EqualTo(before.Counts));
            Assert.That(after.Callbacks, Is.EqualTo(before.Callbacks));
            Assert.That(after.ProgressReports, Is.EqualTo(before.ProgressReports));
            Assert.That(after.Failures, Is.EqualTo(before.Failures));
            Assert.That(after.NativeEvents, Is.EqualTo(before.NativeEvents));
            Assert.That(after.InstallerTrace, Is.EqualTo(before.InstallerTrace));
            Assert.That(after.EntryHistory, Is.EqualTo(before.EntryHistory));
            Assert.That(after.TransitionHistory, Is.EqualTo(before.TransitionHistory));
        }

        private async UniTask FollowWithMainEntry(Task entry = null)
        {
            await Complete(entry ?? _manager.EnterFirstSceneAsync(Hub, LoadSceneMode.Additive).AsTask());
            await Complete(_manager.WaitForEntryAsync().AsTask());
            await Complete(_manager.WaitForTransitionAsync().AsTask());
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Ready));
            Assert.That(_manager.CanProceed, Is.True);
            Assert.That(_manager.GameScene.path, Is.EqualTo(Hub));
            Assert.That(_manager.OwnedSceneCount, Is.EqualTo(1));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(_installer.PrepareCount, Is.EqualTo(1));
            Assert.That(_callbacks.Failures, Is.Empty);
            Assert.That(_callbacks.Calls, Does.Contain("uses-loading"));
            Assert.That(_callbacks.Calls, Does.Contain("cover"));
            Assert.That(_callbacks.Calls, Does.Contain("configure"));
            Assert.That(_callbacks.Calls, Does.Contain("presentation"));
            Assert.That(_callbacks.Calls, Does.Contain("reveal"));
            Assert.That(_callbacks.Progress, Is.Not.Empty);
            Assert.That(_nativeEvents.Exists(value => value.StartsWith("loaded:", StringComparison.Ordinal)), Is.True);
            await Complete(_manager.ShutdownAsync().AsTask());
            Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Stopped));
            Assert.That(_manager.OwnedScenes, Is.Empty);
            Assert.That(_manager.RegisteredScenes, Is.Empty);
            Assert.That(_loader.UnloadCount, Is.EqualTo(1));
            Assert.That(_nativeEvents.Exists(value => value.StartsWith("unloaded:", StringComparison.Ordinal)), Is.True);
            Assert.That(_common.IsPrepared, Is.True, "The manager borrows the common owner.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator CommandOverloads_WorkerRejectsBeforeEffects(
            [ValueSource(nameof(Commands))] CommandOverload command) => CheckCommand(command).ToCoroutine();

        private async UniTask CheckCommand(CommandOverload command)
        {
            // Construct passive target/request arguments on main; the worker invokes only the manager member.
            SceneTarget hub = SceneTarget.BuildScene(Hub);
            SceneTarget main = SceneTarget.BuildScene(Main);
            Scene parent = _manager.GameScene;
            var request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, hub);
            Task entry = null;
            try
            {
                if (command == CommandOverload.Cancel)
                {
                    _callbacks.CoverGate = new UniTaskCompletionSource();
                    entry = _manager.EnterFirstSceneAsync(hub).AsTask();
                    Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Covering));
                    Assert.That(entry.IsCompleted, Is.False);
                    Assert.That(_callbacks.CoverToken.IsCancellationRequested, Is.False);
                }
                else
                {
                    Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Idle));
                }
                EffectSnapshot before = CaptureEffects();
                var observation = OnWorker(result =>
                {
                    switch (command)
                    {
                        case CommandOverload.EnterPath: Observe(_manager.EnterFirstSceneAsync(Hub), result); break;
                        case CommandOverload.EnterTarget: Observe(_manager.EnterFirstSceneAsync(hub), result); break;
                        case CommandOverload.WaitEntry: Observe(_manager.WaitForEntryAsync(), result); break;
                        case CommandOverload.ReplacePath: Observe(_manager.ReplacePrimaryAsync(Main), result); break;
                        case CommandOverload.ReplaceTarget: Observe(_manager.ReplacePrimaryAsync(main), result); break;
                        case CommandOverload.AddPath: Observe(_manager.AddDerivedAsync(Main, parent), result); break;
                        case CommandOverload.AddTarget: Observe(_manager.AddDerivedAsync(main, parent), result); break;
                        case CommandOverload.Remove: Observe(_manager.RemoveDerivedAsync(parent, parent), result); break;
                        case CommandOverload.TryId: Observe(_manager.TryTransitionAsync("entry"), result); break;
                        case CommandOverload.TryRequest: Observe(_manager.TryTransitionAsync(request), result); break;
                        case CommandOverload.WaitTransition: Observe(_manager.WaitForTransitionAsync(), result); break;
                        case CommandOverload.Cancel: _manager.CancelTransition(); break;
                        case CommandOverload.Shutdown: Observe(_manager.ShutdownAsync(), result); break;
                        default: throw new ArgumentOutOfRangeException(nameof(command));
                    }
                });
                AssertThreadRejection(observation);
                AssertEffectsUnchanged(before, CaptureEffects());
                await UniTask.Yield();
                AssertEffectsUnchanged(before, CaptureEffects());
                if (entry != null)
                {
                    Assert.That(entry.IsCompleted, Is.False);
                    Assert.That(_callbacks.CoverToken.IsCancellationRequested, Is.False);
                    _callbacks.CoverGate.TrySetResult();
                }
                await FollowWithMainEntry(entry);
            }
            finally
            {
                _callbacks.CoverGate?.TrySetResult();
                UnsubscribeNativeEvents();
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator Constructor_WorkerRejectsBeforeHostValidation() => CheckConstructor().ToCoroutine();

        private async UniTask CheckConstructor()
        {
            try
            {
                EffectSnapshot before = CaptureEffects();
                var observation = OnWorker(_ => new GameSceneManager(null, _callbacks, _loader, _loader, _settings));
                AssertThreadRejection(observation);
                AssertEffectsUnchanged(before, CaptureEffects());
                await UniTask.Yield();
                AssertEffectsUnchanged(before, CaptureEffects());
                await FollowWithMainEntry();
            }
            finally
            {
                UnsubscribeNativeEvents();
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator NullBuffers_WorkerUsesArgumentNullPriority(
            [ValueSource(nameof(Buffers))] BufferKind buffer) => CheckNullBuffer(buffer).ToCoroutine();

        private async UniTask CheckNullBuffer(BufferKind buffer)
        {
            try
            {
                EffectSnapshot before = CaptureEffects();
                var observation = OnWorker(_ =>
                {
                    if (buffer == BufferKind.Owned)
                    {
                        _manager.GetOwnedScenes(null);
                    }
                    else
                    {
                        _manager.GetRegisteredScenes(null);
                    }
                });
                Assert.That(observation.Joined, Is.True, "Worker did not join within 3000ms.");
                Assert.That(observation.Error, Is.TypeOf<ArgumentNullException>());
                Assert.That(((ArgumentNullException)observation.Error).ParamName, Is.EqualTo("buffer"));
                Assert.That(observation.Error.Message, Is.EqualTo(new ArgumentNullException("buffer").Message));
                AssertEffectsUnchanged(before, CaptureEffects());
                await UniTask.Yield();
                AssertEffectsUnchanged(before, CaptureEffects());
                await FollowWithMainEntry();
            }
            finally
            {
                UnsubscribeNativeEvents();
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator WaitWithoutHistory_PreservesExactRejectionAndIdleState() => CheckNoHistory().ToCoroutine();

        private async UniTask CheckNoHistory()
        {
            try
            {
                Assert.That(_manager.State, Is.EqualTo(SceneTransitionState.Idle));
                EffectSnapshot before = CaptureEffects();
                AssertHistoryErrors(default);
                using (var cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    AssertHistoryErrors(cancelled.Token);
                }
                AssertEffectsUnchanged(before, CaptureEffects());
                await UniTask.Yield();
                AssertEffectsUnchanged(before, CaptureEffects());
                await FollowWithMainEntry();
            }
            finally
            {
                UnsubscribeNativeEvents();
            }
        }

        private void AssertHistoryErrors(CancellationToken token)
        {
            Exception entry = Assert.Throws<InvalidOperationException>(() => _manager.WaitForEntryAsync(token));
            Exception transition = Assert.Throws<InvalidOperationException>(() => _manager.WaitForTransitionAsync(token));
            Assert.That(entry.Message, Is.EqualTo(EntryHistoryMessage));
            Assert.That(transition.Message, Is.EqualTo(TransitionHistoryMessage));
        }
    }

    // This test-only component records every callback without making assertions in dispatch.
    public sealed class GameSceneSafetyNetCallbacks : SceneTransitionCallbacks
    {
        public readonly List<string> Calls = new List<string>();
        public readonly List<SceneTransitionProgress> Progress = new List<SceneTransitionProgress>();
        public readonly List<Exception> Failures = new List<Exception>();
        public UniTaskCompletionSource CoverGate;
        public CancellationToken CoverToken;

        public override async UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            Calls.Add("cover");
            CoverToken = cancellationToken;
            if (CoverGate != null)
            {
                await CoverGate.Task.AttachExternalCancellation(cancellationToken);
            }
        }

        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            Calls.Add("configure");
            return UniTask.CompletedTask;
        }

        public override UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            Calls.Add("presentation");
            return UniTask.CompletedTask;
        }

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            Calls.Add("reveal");
            return UniTask.CompletedTask;
        }

        public override bool UsesLoadingPresentation(SceneLoadingContext context)
        {
            Calls.Add("uses-loading");
            return false;
        }

        public override UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            Calls.Add("prepare-loading");
            return UniTask.CompletedTask;
        }

        public override UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            Calls.Add("reveal-loading");
            return UniTask.CompletedTask;
        }

        public override void ReportLoadingProgress(SceneTransitionProgress progress)
        {
            Calls.Add("progress");
            Progress.Add(progress);
        }

        public override UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            Calls.Add("proceed");
            return UniTask.CompletedTask;
        }

        public override UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context)
        {
            Calls.Add("release-loading");
            return UniTask.CompletedTask;
        }

        public override void OnFailure(Exception exception)
        {
            Calls.Add("failure");
            Failures.Add(exception);
        }
    }

}
