using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using MyLab.Core.ResourceManagement;
using MyLab.Core.Lifecycle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using CoreResourceManager = MyLab.Core.ResourceManagement.ResourceManager;

namespace MyLab.Core.Tests
{
    public sealed class ResourceManagerTests
    {
        private CoreResourceManager _resources;
        private DelayedProvider _provider;
        private ResourceLocationMap _locator;
        private TextAsset _asset;
        private string _key;
        private string _bootstrapPath;
        private IResourceLocator[] _bootstrapLocators;
        private GameObject _rootObject;
        private GameObject _prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (_bootstrapPath == null)
            {
                _bootstrapPath = Path.Combine(Application.temporaryCachePath, "MyLabResourceTests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_bootstrapPath);
                var catalog = new ContentCatalogData(new List<ContentCatalogDataEntry>(), "MyLabTestCatalog")
                {
                    InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>("instance"),
                    SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>("scene")
                };
                string catalogPath = Path.Combine(_bootstrapPath, "catalog.bin");
                File.WriteAllBytes(catalogPath, catalog.SerializeToByteArray());
                var runtimeData = new ResourceManagerRuntimeData
                {
                    DisableCatalogUpdateOnStartup = true
                };
#if UNITY_EDITOR
                runtimeData.BuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#endif
                runtimeData.CatalogLocations.Add(new ResourceLocationData(
                    new[] { ResourceManagerRuntimeData.kCatalogAddress }, catalogPath,
                    typeof(ContentCatalogProvider), typeof(ContentCatalogData)));
                string settingsPath = Path.Combine(_bootstrapPath, "settings.json");
                File.WriteAllText(settingsPath, JsonUtility.ToJson(runtimeData));
                var previousLocators = Addressables.ResourceLocators.ToArray();
                bool hadPath = PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);
                string previousPath = PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath);
                var bootstrap = new CoreResourceManager();
                try
                {
                    PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, settingsPath);
                    var initialization = bootstrap.InitializeAsync().AsTask();
                    float deadline = Time.realtimeSinceStartup + 10;
                    while (!initialization.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(initialization.IsCompleted, Is.True, "Native catalog initialization did not complete.");
                    initialization.GetAwaiter().GetResult();
                    Assert.That(bootstrap.IsInitialized, Is.True);
                    _bootstrapLocators = Addressables.ResourceLocators.Except(previousLocators).ToArray();
                }
                finally
                {
                    bootstrap.Dispose();
                    if (hadPath) PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, previousPath);
                    else PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
                }
            }
            _key = "MyLabTests/" + Guid.NewGuid().ToString("N");
            _asset = new TextAsset("resource-test");
            _provider = new DelayedProvider { Asset = _asset };
            _provider.Initialize(_key, null);
            _locator = new ResourceLocationMap(_key);
            _locator.Add(_key, new ResourceLocationBase(_key, _key, _provider.ProviderId, typeof(TextAsset)));
            Addressables.ResourceManager.ResourceProviders.Add(_provider);
            Addressables.AddResourceLocator(_locator);
            _resources = new CoreResourceManager();
        }

        [OneTimeTearDown]
        public void RemoveBootstrap()
        {
            if (_bootstrapLocators != null)
            {
                foreach (var locator in _bootstrapLocators) Addressables.RemoveResourceLocator(locator);
            }
            if (_bootstrapPath != null)
            {
                File.Delete(Path.Combine(_bootstrapPath, "catalog.bin"));
                File.Delete(Path.Combine(_bootstrapPath, "settings.json"));
                Directory.Delete(_bootstrapPath);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_rootObject != null) UnityEngine.Object.Destroy(_rootObject);
            yield return null;
            _resources?.Dispose();
            _provider?.CompletePending();
            yield return null;
            yield return null;
            Addressables.RemoveResourceLocator(_locator);
            Addressables.ResourceManager.ResourceProviders.Remove(_provider);
            UnityEngine.Object.Destroy(_asset);
            if (_prefab != null) UnityEngine.Object.Destroy(_prefab);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SharedLoadCachesOneOwnedHandle() => CheckSharedLoad().ToCoroutine();

        private async UniTask CheckSharedLoad()
        {
            var first = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            var second = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Complete(first);
            await Settle(second);
            Assert.That(first.Result, Is.SameAs(_asset));
            Assert.That(second.Result, Is.SameAs(_asset));
            Assert.That(await _resources.LoadAssetAsync<TextAsset>(_key), Is.SameAs(_asset));
            Assert.That(_provider.Loads, Is.EqualTo(1));
            Assert.That(_provider.Releases, Is.Zero);
            _resources.Dispose();
            _resources.Dispose();
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CallerCancellationDoesNotCancelSharedLoad() => CheckCancellation(false).ToCoroutine();

        [UnityTest]
        public IEnumerator AllCancelledWaitersStillLeaveOwnerCache() => CheckCancellation(true).ToCoroutine();

        private async UniTask CheckCancellation(bool cancelAll)
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var cancelled = _resources.LoadAssetAsync<TextAsset>(_key, cancellation.Token).AsTask();
                var survivor = cancelAll ? null : _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
                await Pending();
                cancellation.Cancel();
                await Settle(cancelled);
                Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
                _provider.CompletePending();
                if (survivor != null)
                {
                    await Settle(survivor);
                    Assert.That(survivor.Result, Is.SameAs(_asset));
                }
                Assert.That(await _resources.LoadAssetAsync<TextAsset>(_key), Is.SameAs(_asset));
                Assert.That(_provider.Loads, Is.EqualTo(1));
                Assert.That(_provider.Releases, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeysTypesAndPreCancellationDoNotDispatch() => CheckValidation().ToCoroutine();

        private async UniTask CheckValidation()
        {
            Assert.Throws<ArgumentException>(() => _resources.LoadAssetAsync<TextAsset>(" ").GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => _resources.LoadAssetAsync<TextAsset>(null).GetAwaiter().GetResult());
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                var cancelled = _resources.LoadAssetAsync<TextAsset>(_key, cancellation.Token).AsTask();
                await Settle(cancelled);
                Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
                Assert.That(_resources.IsInitialized, Is.False);
                Assert.That(_provider.Loads, Is.Zero);
            }
            var pending = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Pending();
            var conflict = _resources.LoadAssetAsync<GameObject>(_key).AsTask();
            await Settle(conflict);
            Assert.Throws<InvalidOperationException>(() => conflict.GetAwaiter().GetResult());
            await Complete(pending);
            var cachedConflict = _resources.LoadAssetAsync<UnityEngine.Object>(_key).AsTask();
            await Settle(cachedConflict);
            Assert.Throws<InvalidOperationException>(() => cachedConflict.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator FailedLoadIsRemovedAndCanRetry() => CheckFailure().ToCoroutine();

        private async UniTask CheckFailure()
        {
            _provider.FailNext = true;
            LogAssert.Expect(LogType.Error, new Regex("^System.InvalidOperationException: MyLab expected provider failure"));
            var failure = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Complete(failure);
            Assert.That(Assert.Catch(() => failure.GetAwaiter().GetResult()).ToString(),
                Does.Contain("MyLab expected provider failure"));
            var retry = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Complete(retry);
            Assert.That(retry.Result, Is.SameAs(_asset));
            Assert.That(_provider.Loads, Is.EqualTo(2));
            await _resources.ShutdownAsync();
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShutdownCancelsWaitersAndDrainsLateHandle() => CheckShutdown().ToCoroutine();

        private async UniTask CheckShutdown()
        {
            var pending = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Pending();
            var shutdown = _resources.ShutdownAsync().AsTask();
            var sharedShutdown = _resources.ShutdownAsync().AsTask();
            await Settle(pending);
            Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
            Assert.That(_resources.IsDisposed, Is.True);
            Assert.That(_resources.IsInitialized, Is.False);
            Assert.That(shutdown.IsCompleted, Is.False);
            _provider.CompletePending();
            await Settle(shutdown);
            await Settle(sharedShutdown);
            shutdown.GetAwaiter().GetResult();
            sharedShutdown.GetAwaiter().GetResult();
            Assert.That(_provider.Releases, Is.EqualTo(1));
            _resources.Dispose();
            Assert.That(_provider.Releases, Is.EqualTo(1));
            var rejected = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
            await Settle(rejected);
            Assert.Throws<ObjectDisposedException>(() => rejected.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator SeparateOwnersKeepIndependentNativeReferences() => CheckOwners().ToCoroutine();

        private async UniTask CheckOwners()
        {
            using (var other = new CoreResourceManager())
            {
                var first = _resources.LoadAssetAsync<TextAsset>(_key).AsTask();
                var second = other.LoadAssetAsync<TextAsset>(_key).AsTask();
                await Complete(first);
                await Settle(second);
                Assert.That(second.Result, Is.SameAs(_asset));
                _resources.Dispose();
                Assert.That(_provider.Releases, Is.Zero);
                Assert.That(await other.LoadAssetAsync<TextAsset>(_key), Is.SameAs(_asset));
            }
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InitializationIsSharedAndOwnerTerminationIsPermanent() => CheckInitialization().ToCoroutine();

        private async UniTask CheckInitialization()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                var cancelled = _resources.InitializeAsync(cancellation.Token).AsTask();
                await Settle(cancelled);
                Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
                Assert.That(_resources.IsInitialized, Is.False);
            }
            var first = _resources.InitializeAsync().AsTask();
            var second = _resources.InitializeAsync().AsTask();
            await Settle(first);
            await Settle(second);
            first.GetAwaiter().GetResult();
            second.GetAwaiter().GetResult();
            Assert.That(_resources.IsInitialized, Is.True);
            await _resources.ShutdownAsync();
            var rejected = _resources.InitializeAsync().AsTask();
            await Settle(rejected);
            Assert.Throws<ObjectDisposedException>(() => rejected.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator InitializationDispatchFailureCanRetry() => CheckInitializationRetry().ToCoroutine();

        private async UniTask CheckInitializationRetry()
        {
            // Native Addressables throws when initialization is complete but its locator list is empty.
            // Restore the isolated test catalogs synchronously, before yielding or starting any loads.
            var locators = Addressables.ResourceLocators.ToArray();
            Addressables.ClearResourceLocators();
            try
            {
                Assert.Catch<ArgumentOutOfRangeException>(() => _resources.InitializeAsync().GetAwaiter().GetResult());
                Assert.That(_resources.IsInitialized, Is.False);
            }
            finally
            {
                foreach (var locator in locators) Addressables.AddResourceLocator(locator);
            }
            await _resources.InitializeAsync();
            Assert.That(_resources.IsInitialized, Is.True);
        }

        [UnityTest]
        public IEnumerator ShutdownDuringInitializationCannotPublishReadiness() => CheckInitializationShutdown().ToCoroutine();

        private async UniTask CheckInitializationShutdown()
        {
            var initialization = _resources.InitializeAsync().AsTask();
            var shutdown = _resources.ShutdownAsync().AsTask();
            await Settle(initialization);
            Assert.Throws<OperationCanceledException>(() => initialization.GetAwaiter().GetResult());
            await Settle(shutdown);
            shutdown.GetAwaiter().GetResult();
            Assert.That(_resources.IsInitialized, Is.False);
        }

        [UnityTest]
        public IEnumerator BackgroundThreadCallsDoNotMutateOwner() => CheckThreadBoundary().ToCoroutine();

        private async UniTask CheckThreadBoundary()
        {
            var dispose = Task.Run(() => _resources.Dispose());
            await Settle(dispose);
            Assert.That(Assert.Throws<InvalidOperationException>(() => dispose.GetAwaiter().GetResult()).Message,
                Does.Contain("main thread"));
            Assert.That(_resources.IsDisposed, Is.False);
            var initialize = Task.Run(() => _resources.InitializeAsync().GetAwaiter().GetResult());
            await Settle(initialize);
            Assert.Throws<InvalidOperationException>(() => initialize.GetAwaiter().GetResult());
            var load = Task.Run(() => _resources.LoadAssetAsync<TextAsset>(_key).GetAwaiter().GetResult());
            await Settle(load);
            Assert.Throws<InvalidOperationException>(() => load.GetAwaiter().GetResult());
            var shutdown = Task.Run(() => _resources.ShutdownAsync().GetAwaiter().GetResult());
            await Settle(shutdown);
            Assert.Throws<InvalidOperationException>(() => shutdown.GetAwaiter().GetResult());
            Assert.That(_resources.IsInitialized, Is.False);
            Assert.That(_provider.Loads, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SceneOwnedRootWaitsForRequiredAssetBeforeRevealing() => CheckRoot(SceneRootMode.SceneOwned, false).ToCoroutine();

        [UnityTest]
        public IEnumerator SingletonRootDisposesPrefabPoolBeforeAssetHandle() => CheckRoot(SceneRootMode.Singleton, true).ToCoroutine();

        [UnityTest]
        public IEnumerator PreparationFailureKeepsCoverAndReleasesOwnedResources() => CheckRootFailure().ToCoroutine();

        [UnityTest]
        public IEnumerator PreparationCallerCancellationKeepsCoverUntilOwnerShutdown() => CheckRootCancellation().ToCoroutine();

        [UnityTest]
        public IEnumerator RootDestructionCancelsLoadAndReleasesLateCompletion() => CheckRootDestruction().ToCoroutine();

        private async UniTask CheckRootCancellation()
        {
            var root = CreateRoot(SceneRootMode.SceneOwned, false, out var installer, out _);
            var owned = installer.Resources;
            bool covered = false;
            int proceeded = 0;
            using (var cancellation = new CancellationTokenSource())
            {
                var flow = new SceneRootFlow(root,
                    token => { covered = true; return UniTask.CompletedTask; },
                    token => { covered = false; return UniTask.CompletedTask; });
                var preparation = flow.PrepareAndProceedAsync(token =>
                {
                    ++proceeded;
                    return UniTask.CompletedTask;
                }, cancellation.Token).AsTask();
                await Pending();
                cancellation.Cancel();
                await Settle(preparation);
                Assert.Throws<OperationCanceledException>(() => preparation.GetAwaiter().GetResult());
                Assert.That(owned.IsDisposed, Is.False, "Caller cancellation stops only this flow's wait.");
                Assert.That(covered, Is.True);
                Assert.That(proceeded, Is.Zero);
                var shutdown = root.ShutdownAsync().AsTask();
                for (int frame = 0; frame < 100 && !owned.IsDisposed; ++frame) await UniTask.NextFrame();
                Assert.That(owned.IsDisposed, Is.True);
                Assert.That(shutdown.IsCompleted, Is.False, "Owner shutdown must drain native work.");
                await Complete(shutdown);
                shutdown.GetAwaiter().GetResult();
                Assert.That(root.IsPrepared, Is.False);
                Assert.That(_provider.Releases, Is.EqualTo(1));
            }
        }

        private async UniTask CheckRootDestruction()
        {
            var root = CreateRoot(SceneRootMode.SceneOwned, false, out var installer, out _);
            var owned = installer.Resources;
            var preparation = root.PrepareAsync().AsTask();
            await Pending();
            UnityEngine.Object.Destroy(_rootObject);
            await UniTask.NextFrame();
            await Settle(preparation);
            Assert.Throws<OperationCanceledException>(() => preparation.GetAwaiter().GetResult());
            Assert.That(owned.IsDisposed, Is.True);
            _provider.CompletePending();
            await owned.ShutdownAsync();
            Assert.That(root.IsPrepared, Is.False);
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        private ISceneRoot CreateRoot(SceneRootMode mode, bool prefab, out ResourceManagerInstaller installer,
            out ResourceConsumerProbe consumer)
        {
            _rootObject = new GameObject("ResourceTestRoot");
            _rootObject.SetActive(false);
            installer = _rootObject.AddComponent<ResourceManagerInstaller>();
            consumer = _rootObject.AddComponent<ResourceConsumerProbe>();
            consumer.Source = installer;
            consumer.Key = _key;
            consumer.LoadPrefab = prefab;
            if (prefab)
            {
                _prefab = new GameObject("ResourceTestPrefab");
                _prefab.SetActive(false);
                _provider.Asset = _prefab;
                _locator.Locations[_key].Clear();
                _locator.Add(_key, new ResourceLocationBase(_key, _key, _provider.ProviderId, typeof(GameObject)));
            }
            var root = SceneRootSetup.Attach(_rootObject, mode, new SceneRootInstaller[] { installer, consumer });
            _rootObject.SetActive(true);
            return root;
        }

        private async UniTask CheckRoot(SceneRootMode mode, bool prefab)
        {
            var root = CreateRoot(mode, prefab, out var installer, out var consumer);
            Assert.That(consumer.Injected, Is.SameAs(installer.Resources).And.Not.Null);
            bool covered = false;
            int proceeded = 0;
            var flow = new SceneRootFlow(root,
                token => { covered = true; return UniTask.CompletedTask; },
                token => { covered = false; return UniTask.CompletedTask; });
            var preparation = flow.PrepareAndProceedAsync(token =>
            {
                Assert.That(root.IsPrepared, Is.True);
                if (prefab) Assert.That(consumer.Clone != null, Is.True);
                else Assert.That(consumer.Text, Is.SameAs(_asset));
                ++proceeded;
                return UniTask.CompletedTask;
            }).AsTask();
            await Pending();
            Assert.That(root.IsPrepared, Is.False);
            Assert.That(covered, Is.True);
            Assert.That(proceeded, Is.Zero);
            await Complete(preparation);
            preparation.GetAwaiter().GetResult();
            Assert.That(proceeded, Is.EqualTo(1));
            Assert.That(covered, Is.False);
            var owned = installer.Resources;
            var clone = consumer.Clone;
            _provider.BeforeRelease = () =>
            {
                if (prefab) Assert.That(clone == null, Is.True, "Prefab clones must be destroyed before releasing the asset.");
            };
            await flow.ReleaseAndProceedAsync(token => UniTask.CompletedTask);
            Assert.That(owned.IsDisposed, Is.True);
            Assert.That(installer.Resources, Is.Null);
            Assert.That(consumer.Injected, Is.Null);
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        private async UniTask CheckRootFailure()
        {
            var root = CreateRoot(SceneRootMode.SceneOwned, false, out var installer, out var consumer);
            var owned = installer.Resources;
            bool covered = false;
            int proceeded = 0;
            _provider.FailNext = true;
            var flow = new SceneRootFlow(root,
                token => { covered = true; return UniTask.CompletedTask; },
                token => { covered = false; return UniTask.CompletedTask; });
            var preparation = flow.PrepareAndProceedAsync(token =>
            {
                ++proceeded;
                return UniTask.CompletedTask;
            }).AsTask();
            await Pending();
            LogAssert.Expect(LogType.Error, new Regex("^System.InvalidOperationException: MyLab expected provider failure"));
            await Complete(preparation);
            Assert.That(Assert.Catch(() => preparation.GetAwaiter().GetResult()).ToString(),
                Does.Contain("MyLab expected provider failure"));
            Assert.That(covered, Is.True);
            Assert.That(proceeded, Is.Zero);
            Assert.That(root.IsPrepared, Is.False);
            Assert.That(owned.IsDisposed, Is.True);
            Assert.That(installer.Resources, Is.Null);
            Assert.That(consumer.Injected, Is.Null);
        }

        [UnityTest]
        public IEnumerator DataTablesArePublishedBeforeSceneProceedAndDisposedBeforeResource() => CheckDataTableRoot(false).ToCoroutine();

        [UnityTest]
        public IEnumerator InvalidCsvStopsSceneKeepsCoverAndReleasesResource() => CheckDataTableRoot(true).ToCoroutine();

        [UnityTest]
        public IEnumerator StandardDataTablesReadyBeforeSingletonSceneProceed() => CheckDataTableRoot(false, true).ToCoroutine();

        [UnityTest]
        public IEnumerator InvalidStandardIdxKeepsCoverAndReleasesResource() => CheckDataTableRoot(true, true).ToCoroutine();

        private async UniTask CheckDataTableRoot(bool invalid, bool standard = false)
        {
            UnityEngine.Object.Destroy(_asset);
            _asset = new TextAsset(standard ?
                invalid ? "idx,text\n2001,wrong-kind" : "idx,text\n1001,ready" :
                invalid ? "Id,Name\ninvalid,row" : "Id,Name\n1,ready");
            _provider.Asset = _asset;
            _rootObject = new GameObject("DataTableTestRoot");
            _rootObject.SetActive(false);
            var resources = _rootObject.AddComponent<ResourceManagerInstaller>();
            var consumer = _rootObject.AddComponent<DataTableConsumerProbe>();
            consumer.Source = resources;
            consumer.Key = _key;
            consumer.UseStandardIdx = standard;
            var root = SceneRootSetup.Attach(_rootObject, standard ? SceneRootMode.Singleton : SceneRootMode.SceneOwned,
                new SceneRootInstaller[] { resources, consumer });
            _rootObject.SetActive(true);
            var ownedResources = resources.Resources;
            var ownedTables = consumer.Tables;
            bool covered = false;
            int proceeded = 0;
            _provider.BeforeRelease = () => Assert.That(ownedTables.IsDisposed, Is.True, "Data owner must close before its asset source.");
            var flow = new SceneRootFlow(root,
                token => { covered = true; return UniTask.CompletedTask; },
                token => { covered = false; return UniTask.CompletedTask; });
            var preparation = flow.PrepareAndProceedAsync(token =>
            {
                Assert.That(root.IsPrepared, Is.True);
                if (standard)
                    Assert.That(ownedTables.Get<TextRow>(1001).Text, Is.EqualTo("ready"));
                else
                    Assert.That(ownedTables.Snapshot.GetTable<int, (int Id, string Name)>("rows")[1].Name, Is.EqualTo("ready"));
                ++proceeded;
                return UniTask.CompletedTask;
            }).AsTask();
            await Pending();
            Assert.That(ownedTables.Snapshot, Is.Null);
            Assert.That(covered, Is.True);
            Assert.That(proceeded, Is.Zero);
            await Complete(preparation);
            if (invalid)
            {
                Assert.Throws<InvalidDataException>(() => preparation.GetAwaiter().GetResult());
                Assert.That(covered, Is.True);
                Assert.That(proceeded, Is.Zero);
                Assert.That(root.IsPrepared, Is.False);
            }
            else
            {
                preparation.GetAwaiter().GetResult();
                Assert.That(covered, Is.False);
                Assert.That(proceeded, Is.EqualTo(1));
                await flow.ReleaseAndProceedAsync(token => UniTask.CompletedTask);
            }
            Assert.That(ownedTables.IsDisposed, Is.True);
            Assert.That(ownedTables.Snapshot, Is.Null);
            Assert.That(consumer.Tables, Is.Null);
            Assert.That(ownedResources.IsDisposed, Is.True);
            Assert.That(_provider.Releases, Is.EqualTo(1));
        }

        private async UniTask Pending()
        {
            for (int frame = 0; frame < 100 && _provider.PendingCount == 0; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(_provider.PendingCount, Is.GreaterThan(0), "Native provider was not reached.");
        }

        private async UniTask Complete(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame)
            {
                _provider.CompletePending();
                await UniTask.NextFrame();
            }
            await Settle(task);
        }

        private static async UniTask Settle(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True, "Operation did not settle within 100 frames.");
        }

        private sealed class DelayedProvider : ResourceProviderBase
        {
            private readonly List<ProvideHandle> _pending = new List<ProvideHandle>();
            public UnityEngine.Object Asset;
            public bool FailNext;
            public int Loads;
            public int Releases;
            public int PendingCount => _pending.Count;
            public Action BeforeRelease;

            public override void Provide(ProvideHandle handle)
            {
                ++Loads;
                _pending.Add(handle);
            }

            public override void Release(IResourceLocation location, object asset)
            {
                BeforeRelease?.Invoke();
                ++Releases;
            }

            public void CompletePending()
            {
                var batch = _pending.ToArray();
                _pending.Clear();
                foreach (var handle in batch)
                {
                    if (FailNext)
                    {
                        FailNext = false;
                        handle.Complete<UnityEngine.Object>(null, false,
                            new InvalidOperationException("MyLab expected provider failure"));
                    }
                    else
                    {
                        handle.Complete(Asset, true, (Exception)null);
                    }
                }
            }
        }
    }
}
