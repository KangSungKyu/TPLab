using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.Lifecycle;
using TPLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class GameScenePollingTests
    {
        private const string Hub = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/TPLab/Tests/Fixtures/ReplacementMain.unity";
        private const string Area = "Assets/TPLab/Tests/Fixtures/DerivedArea.unity";
        private const string Nested = "Assets/TPLab/Tests/Fixtures/NestedArea.unity";
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneTransitionCallbacksProbe _callbacks;
        private GameSceneManager _manager;
        private Scene _foreign;
        private Scene _initialActive;
        private static IReadOnlyList<Scene> _ownedSink;
        private static IReadOnlyList<SceneRegistration> _registeredSink;

        [UnitySetUp]
        [Timeout(30000)]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
            _initialActive = SceneManager.GetActiveScene();
            _foreign = default;
            _host = new GameObject("PollingCommon");
            _host.SetActive(false);
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(Array.Empty<SceneRootInstaller>(), true);
            _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _host.SetActive(true);
            _manager = new GameSceneManager(_common, _callbacks);
            await _manager.EnterFirstSceneAsync(Hub);
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTearDown]
        [Timeout(30000)]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            _callbacks?.PresentationGate?.TrySetResult();
            if (_manager != null)
            {
                try { await _manager.ShutdownAsync(); } catch (Exception) { }
            }
            if (_common != null) await _common.ShutdownAsync();
            if (_foreign.IsValid() && _foreign.isLoaded) await SceneManager.UnloadSceneAsync(_foreign).ToUniTask();
            if (_host != null) UnityEngine.Object.Destroy(_host);
            _ownedSink = null;
            _registeredSink = null;
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        private Action<List<T>> BindBuffer<T>(string name)
        {
            var method = typeof(GameSceneManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(List<T>) }, null);
            Assert.That(method, Is.Not.Null, "Required public API: void GameSceneManager." + name + "(List<" + typeof(T).Name + ">)");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            return (Action<List<T>>)Delegate.CreateDelegate(typeof(Action<List<T>>), _manager, method);
        }

        private Func<int> BindCount()
        {
            var getter = typeof(GameSceneManager).GetProperty("OwnedSceneCount")?.GetGetMethod();
            Assert.That(getter, Is.Not.Null, "Required public API: int GameSceneManager.OwnedSceneCount { get; }");
            Assert.That(getter.ReturnType, Is.EqualTo(typeof(int)));
            return (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), _manager, getter);
        }

        private async UniTask AddNestedAsync()
        {
            var area = await _manager.AddDerivedAsync(Area, _manager.GameScene, priority: 20);
            await _manager.AddDerivedAsync(Nested, area, priority: 10);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator BufferedSnapshotsMatchPropertiesClearAndRemainSnapshots() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var count = BindCount();
            await AddNestedAsync();
            var owned = new List<Scene>(8) { default, default };
            var registered = new List<SceneRegistration>(8) { default, default };
            getOwned(owned);
            getRegistered(registered);
            Assert.That(owned, Is.EqualTo(_manager.OwnedScenes));
            Assert.That(registered, Is.EqualTo(_manager.RegisteredScenes));
            Assert.That(count(), Is.EqualTo(owned.Count));
            Assert.That(owned.Count, Is.EqualTo(3));
            Assert.That(registered[0].Role, Is.EqualTo(SceneRegistrationRole.Primary));
            Assert.That(registered[1].Priority, Is.EqualTo(20));
            Assert.That(registered[2].Parent, Is.EqualTo(owned[1]));
            Assert.That(registered[2].Priority, Is.EqualTo(10));
            var snapshot = _manager.OwnedScenes;
            await _manager.RemoveDerivedAsync(owned[2], owned[1]);
            Assert.That(snapshot.Count, Is.EqualTo(3));
            Assert.That(owned.Count, Is.EqualTo(3));
            Assert.That(registered[2].IsPrepared, Is.True);
            getOwned(owned);
            getRegistered(registered);
            Assert.That(owned, Is.EqualTo(_manager.OwnedScenes));
            Assert.That(registered, Is.EqualTo(_manager.RegisteredScenes));
            Assert.That(owned.Count, Is.EqualTo(2));
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator WarmedBuffersAndCountAllocateZeroWithSufficientCapacity() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var count = BindCount();
            await AddNestedAsync();
            var owned = new List<Scene>(8);
            var registered = new List<SceneRegistration>(8);
            getOwned(owned);
            getRegistered(registered);
            count();
            long checksum = 0;
            var result = GcAllocationMeasurement.Measure(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    getOwned(owned);
                    getRegistered(registered);
                    checksum += count() + owned.Count + registered.Count;
                }
            });
            Assert.That(result.EventCount, Is.Zero);
            Assert.That(checksum, Is.EqualTo(9000));
            Assert.That(owned.Capacity, Is.EqualTo(8));
            Assert.That(registered.Capacity, Is.EqualTo(8));
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator SnapshotPropertiesContinueToAllocate() => UniTask.ToCoroutine(async () =>
        {
            await AddNestedAsync();
            _ownedSink = _manager.OwnedScenes;
            _registeredSink = _manager.RegisteredScenes;
            var owned = GcAllocationMeasurement.Measure(() =>
            {
                for (int i = 0; i < 100; i++) _ownedSink = _manager.OwnedScenes;
            });
            var registered = GcAllocationMeasurement.Measure(() =>
            {
                for (int i = 0; i < 100; i++) _registeredSink = _manager.RegisteredScenes;
            });
            Assert.That(owned.EventCount, Is.GreaterThan(0));
            Assert.That(registered.EventCount, Is.GreaterThan(0));
            Assert.That(_ownedSink.Count, Is.EqualTo(3));
            Assert.That(_registeredSink.Count, Is.EqualTo(3));
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator NullBuffersThrowArgumentNullException() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            Assert.Throws<ArgumentNullException>(() => getOwned(null));
            Assert.Throws<ArgumentNullException>(() => getRegistered(null));
            await UniTask.Yield();
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator WorkerCallsFailBeforeChangingBuffers() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var count = BindCount();
            var owned = new List<Scene> { default, default };
            var registered = new List<SceneRegistration> { default, default };
            Exception ownedError = null, registeredError = null, countError = null;
            await Task.Run(() =>
            {
                try { getOwned(owned); } catch (Exception error) { ownedError = error; }
                try { getRegistered(registered); } catch (Exception error) { registeredError = error; }
                try { count(); } catch (Exception error) { countError = error; }
            }).AsUniTask();
            Assert.That(ownedError, Is.TypeOf<InvalidOperationException>());
            Assert.That(registeredError, Is.TypeOf<InvalidOperationException>());
            Assert.That(countError, Is.TypeOf<InvalidOperationException>());
            Assert.That(owned, Is.EqualTo(new Scene[] { default, default }));
            Assert.That(registered, Is.EqualTo(new SceneRegistration[] { default, default }));
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator RegisteredReadFailureClearsPartialBufferAndPreservesException() => UniTask.ToCoroutine(async () =>
        {
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var area = await _manager.AddDerivedAsync(Area, _manager.GameScene);
            var owners = (IList)typeof(GameSceneManager).GetField("_derived", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
            object owner = owners[0];
            var rootField = owner.GetType().GetField("Root", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = (ISceneRoot)rootField.GetValue(owner);
            var probe = original.RootObject.AddComponent<SceneReadinessFaultProbe>();
            probe.Wrapped = original;
            var expected = new InvalidOperationException("project-root-readiness");
            probe.Failure = expected;
            var buffer = new List<SceneRegistration>(8) { default, default };
            int partialCount = -1;
            probe.Reading = () => partialCount = buffer.Count;
            rootField.SetValue(owner, probe);
            try
            {
                var actual = Assert.Throws<InvalidOperationException>(() => getRegistered(buffer));
                Assert.That(actual, Is.SameAs(expected));
                Assert.That(partialCount, Is.EqualTo(1));
                Assert.That(buffer, Is.Empty);
            }
            finally
            {
                rootField.SetValue(owner, original);
                UnityEngine.Object.Destroy(probe);
            }
            await UniTask.Yield();
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CandidateIsOwnedButNotRegisteredDuringPresentation() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var count = BindCount();
            _callbacks.PresentationGate = new UniTaskCompletionSource();
            var transition = _manager.ReplacePrimaryAsync(Main).AsTask();
            await UniTask.WaitUntil(() => _callbacks.PresentationRoot != null &&
                _callbacks.PresentationRoot.RootObject.scene.path == Main);
            var owned = new List<Scene>(8);
            var registered = new List<SceneRegistration>(8);
            getOwned(owned);
            getRegistered(registered);
            Assert.That(owned, Is.EqualTo(_manager.OwnedScenes));
            Assert.That(registered, Is.EqualTo(_manager.RegisteredScenes));
            Assert.That(count(), Is.EqualTo(2));
            Assert.That(owned.Count, Is.EqualTo(2));
            Assert.That(registered.Count, Is.EqualTo(1));
            _callbacks.PresentationGate.TrySetResult();
            await transition;
            getOwned(owned);
            getRegistered(registered);
            Assert.That(owned.Count, Is.EqualTo(1));
            Assert.That(registered[0].Scene.path, Is.EqualTo(Main));
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ExternalUnloadFiltersOwnedButPreservesRegisteredSnapshot() => UniTask.ToCoroutine(async () =>
        {
            var getOwned = BindBuffer<Scene>("GetOwnedScenes");
            var getRegistered = BindBuffer<SceneRegistration>("GetRegisteredScenes");
            var count = BindCount();
            var area = await _manager.AddDerivedAsync(Area, _manager.GameScene);
            await SceneManager.UnloadSceneAsync(area).ToUniTask();
            var owned = new List<Scene>(8);
            var registered = new List<SceneRegistration>(8);
            getOwned(owned);
            getRegistered(registered);
            Assert.That(owned, Is.EqualTo(_manager.OwnedScenes));
            Assert.That(registered, Is.EqualTo(_manager.RegisteredScenes));
            Assert.That(count(), Is.EqualTo(1));
            Assert.That(registered.Count, Is.EqualTo(2));
            Assert.That(registered[1].IsPrepared, Is.False);
            Assert.That(_manager.CanProceed, Is.False);
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ForeignLoadedScenePreventsProceeding() => UniTask.ToCoroutine(async () =>
        {
            _foreign = SceneManager.CreateScene("PollingForeign");
            Assert.That(_manager.CanProceed, Is.False);
            await SceneManager.UnloadSceneAsync(_foreign).ToUniTask();
            Assert.That(_manager.CanProceed, Is.True);
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator MissingRetainedScenePreventsProceeding() => UniTask.ToCoroutine(async () =>
        {
            _foreign = SceneManager.CreateScene("PollingRetained");
            await _manager.ShutdownAsync();
            _manager = new GameSceneManager(_common, _callbacks);
            await _manager.EnterFirstSceneAsync(Hub);
            Assert.That(_manager.CanProceed, Is.True);
            await SceneManager.UnloadSceneAsync(_foreign).ToUniTask();
            Assert.That(_manager.CanProceed, Is.False);
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ExternalActiveSceneChangePreventsProceeding() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(SceneManager.SetActiveScene(_initialActive), Is.True);
            Assert.That(_manager.CanProceed, Is.False);
            Assert.That(SceneManager.SetActiveScene(_manager.GameScene), Is.True);
            Assert.That(_manager.CanProceed, Is.True);
            await UniTask.Yield();
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DerivedReadinessLossPreventsProceeding() => UniTask.ToCoroutine(async () =>
        {
            var area = await _manager.AddDerivedAsync(Area, _manager.GameScene);
            var owners = (IList)typeof(GameSceneManager).GetField("_derived", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
            var root = (ISceneRoot)owners[0].GetType().GetField("Root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owners[0]);
            await root.ShutdownAsync();
            Assert.That(_manager.CanProceed, Is.False);
        });

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DuplicateExpectedScenesPreserveSetEquality() => UniTask.ToCoroutine(async () =>
        {
            var field = typeof(GameSceneManager).GetField("_retainedScenes", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = (Scene[])field.GetValue(_manager);
            Assert.That(original.Length, Is.GreaterThan(0));
            var duplicates = new Scene[original.Length + 1];
            Array.Copy(original, duplicates, original.Length);
            duplicates[original.Length] = original[0];
            field.SetValue(_manager, duplicates);
            try { Assert.That(_manager.CanProceed, Is.True); }
            finally { field.SetValue(_manager, original); }
            await UniTask.Yield();
        });
    }
}
