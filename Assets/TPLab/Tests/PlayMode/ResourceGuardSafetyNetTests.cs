using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public sealed class ResourceGuardSafetyNetTests
    {
        public enum OwnerState
        {
            Open, Disposed
        }
        private static readonly OwnerState[] States = { OwnerState.Open, OwnerState.Disposed };
        private const string ResourceMessage = "ResourceManager requires Unity's main thread.";
        private const string SceneMessage = "Scene resources must be used on Unity's main thread.";
        private const string ScenePath = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private readonly List<Scene> _scenes = new List<Scene>();
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();

        private sealed class WorkerObservation
        {
            public Exception Error;
            public bool Joined;
            public bool Pending;
            public bool GateCompleted;
            public int ThreadId;
        }

        private sealed class NativeSnapshot
        {
            public Scene Active;
            public Scene[] Inventory;
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown()
        {
            yield return CleanupAsync().ToCoroutine();
            LogAssert.NoUnexpectedReceived();
        }

        private async UniTask CleanupAsync()
        {
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            _gates.Clear();
            foreach (Scene scene in _scenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    await Complete(SceneManager.UnloadSceneAsync(scene).ToUniTask().AsTask());
                }
            }
            _scenes.Clear();
        }

        private Scene CreateScene()
        {
            Scene scene = SceneManager.CreateScene("ResourceGuard-" + Guid.NewGuid().ToString("N"));
            _scenes.Add(scene);
            return scene;
        }

        private static NativeSnapshot CaptureNative()
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (int index = 0; index < scenes.Length; ++index)
            {
                scenes[index] = SceneManager.GetSceneAt(index);
            }
            return new NativeSnapshot { Active = SceneManager.GetActiveScene(), Inventory = scenes };
        }

        private static void AssertNativeUnchanged(NativeSnapshot before, NativeSnapshot after)
        {
            Assert.That(after.Active, Is.EqualTo(before.Active));
            Assert.That(after.Inventory, Is.EqualTo(before.Inventory));
        }

        private static WorkerObservation OnWorker(Action<WorkerObservation> action)
        {
            var observation = new WorkerObservation();
            var worker = new Thread(() =>
            {
                observation.ThreadId = Thread.CurrentThread.ManagedThreadId;
                try
                {
                    action(observation);
                }
                catch (Exception error)
                {
                    observation.Error = error;
                }
            }) { IsBackground = true };
            worker.Start();
            observation.Joined = worker.Join(3000);
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

        private static void AssertThreadRejection(WorkerObservation observation, string message)
        {
            Assert.That(observation.Joined, Is.True, "Worker did not join within 3000ms.");
            Assert.That(observation.Pending, Is.False);
            Assert.That(observation.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(observation.Error.Message, Is.EqualTo(message));
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

        private static async UniTask<Exception> ReadFailure(Task task)
        {
            await Wait(task);
            try
            {
                task.GetAwaiter().GetResult();
                return null;
            }
            catch (Exception error)
            {
                return error;
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(
            [ValueSource(nameof(States))] OwnerState state) => CheckResource(state).ToCoroutine();

        private async UniTask CheckResource(OwnerState state)
        {
            var manager = new TPLab.Core.ResourceManagement.ResourceManager();
            try
            {
                if (state == OwnerState.Disposed)
                {
                    await Complete(manager.ShutdownAsync().AsTask());
                }
                bool disposedBefore = manager.IsDisposed;
                bool initializedBefore = manager.IsInitialized;
                NativeSnapshot before = CaptureNative();
                var observation = OnWorker(result => Observe(manager.LoadAssetAsync<TextAsset>(string.Empty), result));
                AssertThreadRejection(observation, ResourceMessage);
                Assert.That(manager.IsDisposed, Is.EqualTo(disposedBefore));
                Assert.That(manager.IsInitialized, Is.EqualTo(initializedBefore));
                AssertNativeUnchanged(before, CaptureNative());
                await UniTask.Yield();
                Assert.That(manager.IsDisposed, Is.EqualTo(disposedBefore));
                Assert.That(manager.IsInitialized, Is.EqualTo(initializedBefore));
                AssertNativeUnchanged(before, CaptureNative());
                Exception mainFailure = await ReadFailure(manager.LoadAssetAsync<TextAsset>(string.Empty).AsTask());
                if (state == OwnerState.Open)
                {
                    Assert.That(mainFailure, Is.TypeOf<ArgumentException>());
                    Assert.That(((ArgumentException)mainFailure).ParamName, Is.EqualTo("key"));
                    Assert.That(mainFailure.Message, Is.EqualTo(new ArgumentException("An Addressables key is required.", "key").Message));
                }
                else
                {
                    Assert.That(mainFailure, Is.TypeOf<ObjectDisposedException>());
                    Assert.That(((ObjectDisposedException)mainFailure).ObjectName,
                        Is.EqualTo(nameof(TPLab.Core.ResourceManagement.ResourceManager)));
                }
                await Complete(manager.ShutdownAsync().AsTask());
                await Complete(manager.ShutdownAsync().AsTask());
                Assert.That(manager.IsDisposed, Is.True);
                Assert.That(manager.IsInitialized, Is.False);
                AssertNativeUnchanged(before, CaptureNative());
            }
            finally
            {
                manager.Dispose();
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadedScene_ConstructorWorkerPrecedesArgumentValidation() => CheckConstructor().ToCoroutine();

        private async UniTask CheckConstructor()
        {
            NativeSnapshot before = CaptureNative();
            var observation = OnWorker(_ => new LoadedScene(default, default, null));
            AssertThreadRejection(observation, SceneMessage);
            AssertNativeUnchanged(before, CaptureNative());
            await UniTask.Yield();
            AssertNativeUnchanged(before, CaptureNative());
            await MainSuccessControl();
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadedScene_UnloadWorkerRejectsBeforeDelegateAndCachedResult() => CheckUnloadStart().ToCoroutine();

        private async UniTask CheckUnloadStart()
        {
            Scene scene = CreateScene();
            var target = SceneTarget.BuildScene(ScenePath);
            int calls = 0;
            int delegateThread = 0;
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            var owner = new LoadedScene(target, scene, async () =>
            {
                ++calls;
                delegateThread = Thread.CurrentThread.ManagedThreadId;
                await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            });
            NativeSnapshot before = CaptureNative();
            AssertThreadRejection(OnWorker(result => Observe(owner.UnloadAsync(), result)), SceneMessage);
            Assert.That(calls, Is.Zero);
            Assert.That(owner.IsUnloaded, Is.False);
            Assert.That(owner.Scene, Is.EqualTo(scene));
            Assert.That(owner.Target, Is.EqualTo(target));
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            AssertNativeUnchanged(before, CaptureNative());
            await UniTask.Yield();
            Assert.That(calls, Is.Zero);
            Assert.That(owner.IsUnloaded, Is.False);
            AssertNativeUnchanged(before, CaptureNative());
            await Complete(owner.UnloadAsync().AsTask());
            Assert.That(owner.IsUnloaded, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(delegateThread, Is.EqualTo(mainThread));
            NativeSnapshot unloaded = CaptureNative();
            AssertThreadRejection(OnWorker(result => Observe(owner.UnloadAsync(), result)), SceneMessage);
            Assert.That(owner.IsUnloaded, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            AssertNativeUnchanged(unloaded, CaptureNative());
            await Complete(owner.UnloadAsync().AsTask());
            Assert.That(calls, Is.EqualTo(1));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry() => CheckUnloadContinuation().ToCoroutine();

        private async UniTask CheckUnloadContinuation()
        {
            Scene scene = CreateScene();
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            int calls = 0;
            int enteredThread = 0;
            int resumedThread = 0;
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            var owner = new LoadedScene(SceneTarget.BuildScene(ScenePath), scene, async () =>
            {
                ++calls;
                enteredThread = Thread.CurrentThread.ManagedThreadId;
                await gate.Task;
                // Observe the continuation inside the public backend delegate, without marshalling it.
                resumedThread = Thread.CurrentThread.ManagedThreadId;
            });
            try
            {
                Task first = owner.UnloadAsync().AsTask();
                Task second = owner.UnloadAsync().AsTask();
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(enteredThread, Is.EqualTo(mainThread));
                Assert.That(resumedThread, Is.Zero);
                Assert.That(first.IsCompleted, Is.False);
                Assert.That(second.IsCompleted, Is.False);
                // The harness controls this backend's native work on main before completing its gate.
                await Complete(SceneManager.UnloadSceneAsync(scene).ToUniTask().AsTask());
                Assert.That(scene.IsValid() && scene.isLoaded, Is.False);
                Assert.That(owner.IsUnloaded, Is.False);
                NativeSnapshot before = CaptureNative();
                var observation = OnWorker(result => result.GateCompleted = gate.TrySetResult());
                Assert.That(observation.Joined, Is.True, "Worker did not join within 3000ms.");
                Assert.That(observation.Error, Is.Null, "Backend completion captures failure in the shared unload result.");
                Assert.That(observation.GateCompleted, Is.True);
                Assert.That(observation.ThreadId, Is.Not.EqualTo(mainThread));
                Assert.That(resumedThread, Is.EqualTo(observation.ThreadId));
                Exception firstFailure = await ReadFailure(first);
                Exception secondFailure = await ReadFailure(second);
                Assert.That(firstFailure, Is.TypeOf<InvalidOperationException>());
                Assert.That(firstFailure.Message, Is.EqualTo(SceneMessage));
                Assert.That(secondFailure, Is.SameAs(firstFailure));
                Assert.That(owner.IsUnloaded, Is.False);
                Assert.That(calls, Is.EqualTo(1));
                AssertNativeUnchanged(before, CaptureNative());
                await UniTask.Yield();
                AssertNativeUnchanged(before, CaptureNative());
                Exception laterFailure = await ReadFailure(owner.UnloadAsync().AsTask());
                Assert.That(laterFailure, Is.SameAs(firstFailure));
                Assert.That(owner.IsUnloaded, Is.False);
                Assert.That(calls, Is.EqualTo(1));
                await MainSuccessControl();
            }
            finally
            {
                gate.TrySetResult();
            }
        }

        private async UniTask MainSuccessControl()
        {
            Scene scene = CreateScene();
            var target = SceneTarget.BuildScene(ScenePath);
            int calls = 0;
            int enteredThread = 0;
            int completedThread = 0;
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            var owner = new LoadedScene(target, scene, async () =>
            {
                ++calls;
                enteredThread = Thread.CurrentThread.ManagedThreadId;
                await SceneManager.UnloadSceneAsync(scene).ToUniTask();
                completedThread = Thread.CurrentThread.ManagedThreadId;
            });
            await Complete(owner.UnloadAsync().AsTask());
            await Complete(owner.UnloadAsync().AsTask());
            Assert.That(owner.IsUnloaded, Is.True);
            Assert.That(scene.IsValid() && scene.isLoaded, Is.False);
            Assert.That(owner.Scene, Is.EqualTo(scene));
            Assert.That(owner.Target, Is.EqualTo(target));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(enteredThread, Is.EqualTo(mainThread));
            Assert.That(completedThread, Is.EqualTo(mainThread));
        }
    }
}
