using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SceneResult = MyLab.Core.ResourceManagement.LoadedScene;

namespace MyLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class SceneLoaderPlayModeTests
    {
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private string _runtimePath;
        private IResourceLocator[] _runtimeLocators;
        private ResourceLocationMap _locator;
        private string _key;
        private SceneResult _loaded;
        private GameObject _host;
        private SceneOwnedRoot _common;
        private SceneTransitionCallbacksProbe _callbacks;
        private GameSceneManager _manager;

        [UnitySetUp]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
            _loaded = null;
            _host = null;
            _common = null;
            _callbacks = null;
            _manager = null;
            // An initialized Addressables instance may have no remaining global catalog locator after another fixture cleans up.
            _key = "MyLabSceneTests/" + Guid.NewGuid().ToString("N");
            _locator = new ResourceLocationMap(_key);
            _locator.Add(_key, new ResourceLocationBase(_key, Hub, typeof(SceneProvider).FullName, typeof(SceneInstance)));
            Addressables.AddResourceLocator(_locator);
            // Same temporary runtime-data bootstrap used by ResourceManagerTests; no project catalogs/settings are saved.
            if (_runtimePath == null)
            {
                _runtimePath = Path.Combine(Application.temporaryCachePath, "MyLabSceneLoaderTests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_runtimePath);
                var catalog = new ContentCatalogData(new List<ContentCatalogDataEntry>(), "MyLabSceneTestCatalog")
                {
                    InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>("instance"),
                    SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>("scene")
                };
                string catalogPath = Path.Combine(_runtimePath, "catalog.bin");
                File.WriteAllBytes(catalogPath, catalog.SerializeToByteArray());
                var runtimeData = new ResourceManagerRuntimeData { DisableCatalogUpdateOnStartup = true };
#if UNITY_EDITOR
                runtimeData.BuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#endif
                runtimeData.CatalogLocations.Add(new ResourceLocationData(
                    new[] { ResourceManagerRuntimeData.kCatalogAddress }, catalogPath, typeof(ContentCatalogProvider), typeof(ContentCatalogData)));
                string settingsPath = Path.Combine(_runtimePath, "settings.json");
                File.WriteAllText(settingsPath, JsonUtility.ToJson(runtimeData));
                var previousLocators = Addressables.ResourceLocators.ToArray();
                bool hadPath = PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);
                string previousPath = PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath);
                var resources = new MyLab.Core.ResourceManagement.ResourceManager();
                try
                {
                    PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, settingsPath);
                    await resources.InitializeAsync();
                    _runtimeLocators = Addressables.ResourceLocators.Except(previousLocators).ToArray();
                }
                finally
                {
                    await resources.ShutdownAsync();
                    if (hadPath) PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, previousPath);
                    else PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
                }
            }
        });

        [OneTimeTearDown]
        public void RemoveRuntimeData()
        {
            if (_runtimeLocators != null)
                foreach (var locator in _runtimeLocators) Addressables.RemoveResourceLocator(locator);
            if (_runtimePath == null) return;
            File.Delete(Path.Combine(_runtimePath, "catalog.bin"));
            File.Delete(Path.Combine(_runtimePath, "settings.json"));
            Directory.Delete(_runtimePath);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            // Native Single tests deliberately replace the runner scene; recovery belongs only to the test harness.
            if (SceneManager.sceneCount == 1 && SceneManager.GetSceneAt(0).path == Hub)
                SceneManager.CreateScene("SceneLoaderTestRecovery");
            if (_manager != null) await _manager.ShutdownAsync();
            if (_common != null) await _common.ShutdownAsync();
            if (_loaded != null)
            {
                await ReleaseRootsAsync(_loaded.Scene);
                await _loaded.UnloadAsync();
            }
            var remaining = SceneManager.GetSceneByPath(Hub);
            if (remaining.IsValid() && remaining.isLoaded)
            {
                await ReleaseRootsAsync(remaining);
                await SceneManager.UnloadSceneAsync(remaining).ToUniTask();
            }
            Addressables.RemoveResourceLocator(_locator);
            if (_host != null) UnityEngine.Object.Destroy(_host);
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator NativeResultOwnsTheActualSceneAndSharedUnload() => UniTask.ToCoroutine(async () =>
        {
            var loader = new NativeSceneLoader();
            _loaded = await loader.LoadAsync(SceneTarget.BuildScene(Hub), LoadSceneMode.Additive);
            Assert.That(_loaded.Scene, Is.EqualTo(SceneManager.GetSceneByPath(Hub)));
            Assert.That(_loaded.Scene.isLoaded, Is.True);
            await ReleaseRootsAsync(_loaded.Scene);
            var first = _loaded.UnloadAsync().AsTask();
            var second = _loaded.UnloadAsync().AsTask();
            await first;
            await second;
            Assert.That(_loaded.IsUnloaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
        });

        [UnityTest]
        public IEnumerator NativeProgressReportsLoadingWithoutPreparingTheRoot() => CheckProgressAsync(false).ToCoroutine();

        [UnityTest]
        public IEnumerator AddressablesProgressReportsResolutionThenLoadingWithoutPreparingTheRoot() => CheckProgressAsync(true).ToCoroutine();

        [UnityTest]
        public IEnumerator NativeObserverFailureStillReturnsAnOwnedResult() => CheckProgressFailureAsync(false).ToCoroutine();

        [UnityTest]
        public IEnumerator AddressablesObserverFailureStillReturnsAnOwnedResult() => CheckProgressFailureAsync(true).ToCoroutine();

        [UnityTest]
        public IEnumerator NativeObserverDisposalDuringLoadKeepsTheResultOwned() => CheckProgressDisposalAsync(false).ToCoroutine();

        [UnityTest]
        public IEnumerator AddressablesObserverDisposalDuringLoadKeepsTheResultOwned() => CheckProgressDisposalAsync(true).ToCoroutine();

        private async UniTask CheckProgressAsync(bool addressable)
        {
            var reports = new List<SceneLoadProgress>();
            using (var observer = new SceneLoadProgressObserver(progress => reports.Add(progress)))
            {
                _loaded = await ProgressLoader(addressable).LoadAsync(ProgressTarget(addressable), LoadSceneMode.Additive, observer);
                Assert.That(_loaded.Scene.isLoaded, Is.True);
                Assert.That(observer.Failure, Is.Null);
                Assert.That(reports, Is.Not.Empty);
                Assert.That(reports.All(progress => progress.Ratio >= 0f && progress.Ratio <= 1f && !float.IsNaN(progress.Ratio)), Is.True);
                var loading = reports.Where(progress => progress.Stage == SceneLoadStage.LoadingScene).ToArray();
                Assert.That(loading, Is.Not.Empty);
                Assert.That(loading.First().Ratio, Is.Zero);
                Assert.That(loading.Last().Ratio, Is.EqualTo(1f));
                if (addressable)
                {
                    var resolving = reports.TakeWhile(progress => progress.Stage == SceneLoadStage.ResolvingTarget).ToArray();
                    Assert.That(resolving, Is.Not.Empty);
                    Assert.That(resolving.First().Ratio, Is.Zero);
                    Assert.That(resolving.Last().Ratio, Is.EqualTo(1f));
                    Assert.That(reports.Skip(resolving.Length).All(progress => progress.Stage == SceneLoadStage.LoadingScene), Is.True);
                }
                else
                {
                    Assert.That(reports.All(progress => progress.Stage == SceneLoadStage.LoadingScene), Is.True);
                }
                var roots = _loaded.Scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true)).OfType<ISceneRoot>().ToArray();
                Assert.That(roots, Has.Length.EqualTo(1));
                Assert.That(roots[0].IsReady, Is.True);
                Assert.That(roots[0].IsPrepared, Is.False);
                await ReleaseRootsAsync(_loaded.Scene);
                await _loaded.UnloadAsync();
                Assert.That(_loaded.IsUnloaded, Is.True);
            }
        }

        private async UniTask CheckProgressFailureAsync(bool addressable)
        {
            var expected = new InvalidOperationException("expected scene progress callback failure");
            int calls = 0;
            using (var observer = new SceneLoadProgressObserver(_ =>
            {
                ++calls;
                throw expected;
            }))
            {
                _loaded = await ProgressLoader(addressable).LoadAsync(ProgressTarget(addressable), LoadSceneMode.Additive, observer);
                Assert.That(_loaded.Scene.isLoaded, Is.True);
                Assert.That(observer.Failure, Is.SameAs(expected));
                Assert.That(calls, Is.EqualTo(1));
                await ReleaseRootsAsync(_loaded.Scene);
                await _loaded.UnloadAsync();
                await _loaded.UnloadAsync();
                Assert.That(_loaded.IsUnloaded, Is.True);
                Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            }
        }

        private async UniTask CheckProgressDisposalAsync(bool addressable)
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
                _loaded = await ProgressLoader(addressable).LoadAsync(ProgressTarget(addressable), LoadSceneMode.Additive, observer);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(observer.Failure, Is.Null);
                Assert.That(_loaded.Scene.isLoaded, Is.True);
                await ReleaseRootsAsync(_loaded.Scene);
                await _loaded.UnloadAsync();
                Assert.That(_loaded.IsUnloaded, Is.True);
            }
        }

        private static ISceneProgressLoader ProgressLoader(bool addressable)
        {
            return addressable ? (ISceneProgressLoader)new AddressableSceneLoader() : new NativeSceneLoader();
        }

        private SceneTarget ProgressTarget(bool addressable)
        {
            return addressable ? SceneTarget.Addressable(_key, Hub) : SceneTarget.BuildScene(Hub);
        }

        [UnityTest]
        public IEnumerator AddressablesResultOwnsARealNativeScene() => UniTask.ToCoroutine(async () =>
        {
            _loaded = await new AddressableSceneLoader().LoadAsync(SceneTarget.Addressable(_key, Hub), LoadSceneMode.Additive);
            Assert.That(_loaded.Scene.path, Is.EqualTo(Hub));
            Assert.That(_loaded.Scene.isLoaded, Is.True);
            Assert.That(_loaded.Target.Source, Is.EqualTo(SceneSource.Addressable));
            await ReleaseRootsAsync(_loaded.Scene);
            await _loaded.UnloadAsync();
            await _loaded.UnloadAsync();
            Assert.That(_loaded.IsUnloaded, Is.True);
        });

        [UnityTest]
        public IEnumerator AddressablesExternalUnloadDoesNotReleaseAnInvalidHandleTwice() => UniTask.ToCoroutine(async () =>
        {
            _loaded = await new AddressableSceneLoader().LoadAsync(SceneTarget.Addressable(_key, Hub), LoadSceneMode.Additive);
            await ReleaseRootsAsync(_loaded.Scene);
            await SceneManager.UnloadSceneAsync(_loaded.Scene).ToUniTask();
            await UniTask.Yield();
            await _loaded.UnloadAsync();
            await _loaded.UnloadAsync();
            Assert.That(_loaded.IsUnloaded, Is.True);
        });

        [UnityTest]
        public IEnumerator AddressablesExternalSingleReleasesOnlyThePreviousInstance() => UniTask.ToCoroutine(async () =>
        {
            _loaded = await new AddressableSceneLoader().LoadAsync(SceneTarget.Addressable(_key, Hub), LoadSceneMode.Additive);
            var oldScene = _loaded.Scene;
            await ReleaseRootsAsync(oldScene);
            await SceneManager.LoadSceneAsync(Hub, LoadSceneMode.Single).ToUniTask();
            await UniTask.Yield();
            var replacement = SceneManager.GetSceneByPath(Hub);
            Assert.That(replacement, Is.Not.EqualTo(oldScene));
            await _loaded.UnloadAsync();
            await _loaded.UnloadAsync();
            Assert.That(_loaded.IsUnloaded, Is.True);
            Assert.That(replacement.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(replacement));
        });

        [UnityTest]
        public IEnumerator AddressablesRejectsMultipleLocationsBeforeNativeLoad() => UniTask.ToCoroutine(async () =>
        {
            _locator.Add(_key, new ResourceLocationBase(_key + "/second", Hub, typeof(SceneProvider).FullName, typeof(SceneInstance)));
            Exception failure = null;
            try { await new AddressableSceneLoader().LoadAsync(SceneTarget.Addressable(_key, Hub), LoadSceneMode.Additive); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
        });

        [UnityTest]
        public IEnumerator AddressablesMissingKeyFailsThenAValidKeyStillLoads() => UniTask.ToCoroutine(async () =>
        {
            var loader = new AddressableSceneLoader();
            Exception failure = null;
            try { await loader.LoadAsync(SceneTarget.Addressable(_key + "/missing", Hub), LoadSceneMode.Additive); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("exactly one scene location"));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            _loaded = await loader.LoadAsync(SceneTarget.Addressable(_key, Hub), LoadSceneMode.Additive);
            Assert.That(_loaded.Scene.path, Is.EqualTo(Hub));
            await ReleaseRootsAsync(_loaded.Scene);
            await _loaded.UnloadAsync();
            Assert.That(_loaded.IsUnloaded, Is.True);
        });

        [UnityTest]
        public IEnumerator AddressablesSceneDependencyFailurePropagatesThenTheSameKeyRecovers() => UniTask.ToCoroutine(async () =>
        {
            const string expectedError = "MyLab expected scene dependency failure";
            string key = _key + "/failure-recovery";
            var asset = new EmptyBundleResource();
            var provider = new FailOnceSceneDependencyProvider { Asset = asset, ExpectedError = expectedError };
            provider.Initialize(key + "/provider", null);
            var dependency = new ResourceLocationBase(key + "/dependency", key + "/dependency", provider.ProviderId, typeof(IAssetBundleResource));
            _locator.Add(key, new ResourceLocationBase(key, Hub, typeof(SceneProvider).FullName, typeof(SceneInstance), dependency));
            Addressables.ResourceManager.ResourceProviders.Add(provider);
            try
            {
                var loader = new AddressableSceneLoader();
                var expectedDiagnostics = new List<Exception>();
                var previousHandler = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
                Exception failure = null;
                try
                {
                    UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (operation, exception) =>
                    {
                        if (exception.ToString().IndexOf(expectedError, StringComparison.Ordinal) >= 0)
                            expectedDiagnostics.Add(exception);
                        else previousHandler?.Invoke(operation, exception);
                    };
                    try { await loader.LoadAsync(SceneTarget.Addressable(key, Hub), LoadSceneMode.Additive); }
                    catch (Exception exception) { failure = exception; }
                }
                finally
                {
                    UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previousHandler;
                }
                Assert.That(failure, Is.Not.Null);
                Assert.That(failure.ToString(), Does.Contain(expectedError));
                Assert.That(expectedDiagnostics, Is.Not.Empty);
                Assert.That(provider.Loads, Is.EqualTo(1));
                Assert.That(_loaded, Is.Null);
                Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
                _loaded = await loader.LoadAsync(SceneTarget.Addressable(key, Hub), LoadSceneMode.Additive);
                Assert.That(_loaded.Scene.path, Is.EqualTo(Hub));
                Assert.That(provider.Loads, Is.EqualTo(2));
                await ReleaseRootsAsync(_loaded.Scene);
                await _loaded.UnloadAsync();
                Assert.That(_loaded.IsUnloaded, Is.True);
                Assert.That(provider.Releases, Is.EqualTo(1));
            }
            finally
            {
                try
                {
                    if (_loaded != null && !_loaded.IsUnloaded)
                    {
                        await ReleaseRootsAsync(_loaded.Scene);
                        await _loaded.UnloadAsync();
                    }
                }
                finally
                {
                    Addressables.ResourceManager.ResourceProviders.Remove(provider);
                }
            }
        });

        [UnityTest]
        public IEnumerator ExplicitSourceChoosesOnlyItsInjectedLoader() => UniTask.ToCoroutine(async () =>
        {
            var build = new DelayedLoader();
            var addressable = new DelayedLoader();
            CreateManager(build, addressable);
            var entry = _manager.EnterFirstSceneAsync(SceneTarget.Addressable(_key, Hub)).AsTask();
            Assert.That(build.LoadCount, Is.Zero);
            Assert.That(build.ValidationCount, Is.Zero);
            Assert.That(addressable.ValidationCount, Is.EqualTo(1));
            Assert.That(addressable.LoadCount, Is.EqualTo(1));
            addressable.Gate.TrySetResult();
            await entry;
            Assert.That(_manager.CanProceed, Is.True);
            await _manager.ShutdownAsync();
            Assert.That(addressable.UnloadCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator OwnerCancellationWaitsForLateNativeResultAndCleansIt() => UniTask.ToCoroutine(async () =>
        {
            var loader = new DelayedLoader();
            CreateManager(loader, loader);
            var entry = _manager.EnterFirstSceneAsync(SceneTarget.BuildScene(Hub)).AsTask();
            _manager.CancelTransition();
            Assert.That(entry.IsCompleted, Is.False);
            loader.Gate.TrySetResult();
            Exception failure = null;
            try { await entry; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(loader.UnloadCount, Is.EqualTo(1));
            Assert.That(_manager.LoadedScene.IsValid(), Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_common.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator CallerCancellationLeavesNativeOwnerAndResultAlive() => UniTask.ToCoroutine(async () =>
        {
            var loader = new DelayedLoader();
            CreateManager(loader, loader);
            using (var cancellation = new CancellationTokenSource())
            {
                var entry = _manager.EnterFirstSceneAsync(SceneTarget.BuildScene(Hub), cancellationToken: cancellation.Token).AsTask();
                cancellation.Cancel();
                Exception failure = null;
                try { await entry; } catch (Exception exception) { failure = exception; }
                Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                loader.Gate.TrySetResult();
                await _manager.WaitForEntryAsync();
                Assert.That(_manager.CanProceed, Is.True);
                Assert.That(loader.UnloadCount, Is.Zero);
                Assert.That(_callbacks.FailureCount, Is.Zero);
            }
        });

        [UnityTest]
        public IEnumerator WrongSceneResultIsRetainedThenCleanedWithoutReveal() => CheckWrongResult(false).ToCoroutine();

        [UnityTest]
        public IEnumerator WrongTargetResultIsRetainedThenCleanedWithoutReveal() => CheckWrongResult(true).ToCoroutine();

        private async UniTask CheckWrongResult(bool wrongTarget)
        {
            var loader = new DelayedLoader { ReturnNativeTarget = wrongTarget };
            CreateManager(loader, loader);
            var target = SceneTarget.Addressable(_key, wrongTarget ? Hub : "Assets/MyLab/Tests/Fixtures/WrongScene.unity");
            var entry = _manager.EnterFirstSceneAsync(target).AsTask();
            loader.Gate.TrySetResult();
            Exception failure = null;
            try { await entry; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(loader.UnloadCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RealAddressablesOwnerCancellationOwnsLateCompletion() => UniTask.ToCoroutine(async () =>
        {
            CreateManager();
            var entry = _manager.EnterFirstSceneAsync(SceneTarget.Addressable(_key, Hub)).AsTask();
            _manager.CancelTransition();
            Exception failure = null;
            try { await entry; } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
        });

        private void CreateManager(ISceneLoader buildLoader = null, ISceneLoader addressableLoader = null)
        {
            _host = new GameObject("SceneLoaderCommon");
            _host.SetActive(false);
            _common = _host.AddComponent<SceneOwnedRoot>();
            _common.Configure(Array.Empty<SceneRootInstaller>());
            _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _host.SetActive(true);
            _manager = new GameSceneManager(_common, _callbacks, buildLoader, addressableLoader);
        }

        private static async UniTask ReleaseRootsAsync(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true)).OfType<ISceneRoot>())
                await root.ShutdownAsync();
        }

        private sealed class FailOnceSceneDependencyProvider : ResourceProviderBase
        {
            internal IAssetBundleResource Asset;
            internal string ExpectedError;
            internal int Loads;
            internal int Releases;

            public override void Provide(ProvideHandle handle)
            {
                ++Loads;
                if (Loads == 1) handle.Complete<IAssetBundleResource>(null, false, new InvalidOperationException(ExpectedError));
                else handle.Complete(Asset, true, (Exception)null);
            }

            public override void Release(IResourceLocation location, object asset) => ++Releases;
        }

        private sealed class EmptyBundleResource : IAssetBundleResource
        {
            public AssetBundle GetAssetBundle() => null;
        }

        private sealed class DelayedLoader : ISceneLoader
        {
            internal readonly UniTaskCompletionSource Gate = new UniTaskCompletionSource();
            internal int ValidationCount;
            internal int LoadCount;
            internal int UnloadCount;
            internal bool ReturnNativeTarget;
            public void Validate(SceneTarget target) { ++ValidationCount; target.Validate(); }
            public async UniTask<SceneResult> LoadAsync(SceneTarget target, LoadSceneMode mode)
            {
                ++LoadCount;
                await Gate.Task;
                var native = await new NativeSceneLoader().LoadAsync(SceneTarget.BuildScene(Hub), mode);
                return new SceneResult(ReturnNativeTarget ? native.Target : target, native.Scene, async () =>
                {
                    ++UnloadCount;
                    await native.UnloadAsync();
                });
            }
        }
    }
}
