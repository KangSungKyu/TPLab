using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.Input;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.UI;
using TPLab.UI.Installation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests.Installation
{
    public sealed class UIContextInstallerInputSystemPlayModeTests
    {
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private string _catalogDirectory;
        private IResourceLocator[] _catalogLocators;

        [UnitySetUp]
        public IEnumerator SetUp() => InitializeTestCatalogAsync().ToCoroutine();

        private async UniTask InitializeTestCatalogAsync()
        {
            _catalogDirectory = Path.Combine(Application.temporaryCachePath, "TPLabUIInstallerTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_catalogDirectory);
            var catalog = new ContentCatalogData(new List<ContentCatalogDataEntry>(), "TPLabUIInstallerCatalog")
            {
                InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>("instance"),
                SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>("scene")
            };
            string catalogPath = Path.Combine(_catalogDirectory, "catalog.bin");
            File.WriteAllBytes(catalogPath, catalog.SerializeToByteArray());
            var runtimeData = new ResourceManagerRuntimeData { DisableCatalogUpdateOnStartup = true };
#if UNITY_EDITOR
            runtimeData.BuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#endif
            runtimeData.CatalogLocations.Add(new ResourceLocationData(
                new[] { ResourceManagerRuntimeData.kCatalogAddress }, catalogPath,
                typeof(ContentCatalogProvider), typeof(ContentCatalogData)));
            string settingsPath = Path.Combine(_catalogDirectory, "settings.json");
            File.WriteAllText(settingsPath, JsonUtility.ToJson(runtimeData));
            IResourceLocator[] previousLocators = Addressables.ResourceLocators.ToArray();
            bool hadPath = PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);
            string previousPath = PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath);
            var bootstrap = new TPLab.Core.ResourceManagement.ResourceManager();
            try
            {
                PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, settingsPath);
                await bootstrap.InitializeAsync().Timeout(TimeSpan.FromSeconds(10));
            }
            finally
            {
                _catalogLocators = Addressables.ResourceLocators.Except(previousLocators).ToArray();
                bootstrap.Dispose();
                if (hadPath) PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, previousPath);
                else PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; --i)
            {
                if (_owned[i] != null) UnityEngine.Object.Destroy(_owned[i]);
            }
            _owned.Clear();
            yield return null;
            if (_catalogLocators != null)
            {
                foreach (var locator in _catalogLocators) Addressables.RemoveResourceLocator(locator);
                _catalogLocators = null;
            }
            if (_catalogDirectory != null)
            {
                // Only this test's GUID-named direct files are removed; no recursive cleanup.
                foreach (string file in Directory.GetFiles(_catalogDirectory)) File.Delete(file);
                Directory.Delete(_catalogDirectory);
                _catalogDirectory = null;
            }
        }

        [UnityTest]
        public IEnumerator RootReverseReleaseClosesUiBeforeBorrowedInputAndResources()
            => ReverseReleaseOrderAsync().ToCoroutine();

        private async UniTask ReverseReleaseOrderAsync()
        {
            GameObject prefab = Own(new GameObject("HUD-source"));
            InputActionAsset inputSource = Own(ScriptableObject.CreateInstance<InputActionAsset>());
            inputSource.AddActionMap("Game").AddAction("Fire");

            GameObject host = Own(new GameObject("Root"));
            host.SetActive(false);
            var root = host.AddComponent<SceneOwnedRoot>();
            var serviceHost = Child(host, "Services");
            var resources = serviceHost.AddComponent<ResourceManagerInstaller>();
            var input = serviceHost.AddComponent<InputManagerInstaller>();
            input.Configure(inputSource);
            var ui = serviceHost.AddComponent<UIContextInstaller>();
            UIContextSettings settings = Own(ScriptableObject.CreateInstance<UIContextSettings>());
            settings.Configure(new[]
            {
                new UIContextDefinitionData("hud", prefab, role: UIRole.Hud)
            }, "hud");
            bool uiClosedBeforeServices = false;
            var hooks = new UIHooks
            {
                Closed = _ => uiClosedBeforeServices = resources.Resources != null &&
                    !resources.Resources.IsDisposed && input.Input != null && !input.Input.IsDisposed
            };
            ui.Configure(settings, resources: resources, firstHudHooks: hooks);
            root.Configure(new SceneRootInstaller[] { resources, input, ui });
            host.SetActive(true);
            await root.PrepareAsync();
            Assert.That(input.Input, Is.Not.Null);
            Assert.That(resources.Resources, Is.Not.Null);

            await root.ShutdownAsync();
            Assert.That(uiClosedBeforeServices, Is.True);
            Assert.That(ui.Context, Is.Null);
            Assert.That(input.Input, Is.Null);
            Assert.That(resources.Resources, Is.Null);
            Assert.That(prefab == null, Is.False);
            Assert.That(inputSource == null, Is.False);
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            _owned.Add(value);
            return value;
        }
    }
}
