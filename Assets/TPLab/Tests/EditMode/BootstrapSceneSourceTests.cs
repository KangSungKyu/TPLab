using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TPLab.Core.Editor.Bootstrap;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace TPLab.Core.Tests
{
    public sealed class BootstrapSceneSourceTests
    {
        private const string SceneHeader = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";
        private string _folder;
        private AddressableAssetSettings _settings;
        private Scene _activeScene;
        private int _previewCount;

        [SetUp]
        public void SetUp()
        {
            _activeScene = SceneManager.GetActiveScene();
            _previewCount = EditorSceneManager.previewSceneCount;

            _folder = "Assets/BootstrapSceneSourceTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            AssetDatabase.CreateFolder(_folder, "Config");
            _settings = AddressableAssetSettings.Create(_folder + "/Config", "TestSettings", true, false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_activeScene.IsValid() && _activeScene.isLoaded) SceneManager.SetActiveScene(_activeScene);
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            if (_settings != null)
            {
                foreach (var group in _settings.groups.Where(group => group != null))
                {
                    foreach (var schema in group.Schemas) UnityEngine.Object.DestroyImmediate(schema);
                    UnityEngine.Object.DestroyImmediate(group);
                }
                UnityEngine.Object.DestroyImmediate(_settings);
            }
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previewCount));
        }

        [Test]
        public void RegisteredAddressableSceneOutsideBuildListPassesAndPreservesEditorScenes()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", gamePath));
            var active = SceneManager.GetActiveScene();
            int previewCount = EditorSceneManager.previewSceneCount;

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings);

            Assert.That(errors, Is.Empty);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previewCount));
        }

        [Test]
        public void AddressableSceneMustHaveAnExplicitEntry()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", gamePath));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void AddressableKeyMustResolveToTheSelectedScene()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("other", gamePath));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void SceneReferenceGuidMustMatchItsSavedScenePath()
        {
            string selectedPath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            string otherPath = CreateScene("Other.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(selectedPath, "hub");
            AddAddressableScene(otherPath, "other");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", selectedPath));
            SetSerializedAddressableReference(bootstrapPath, AssetDatabase.AssetPathToGUID(otherPath), false);

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, selectedPath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void AddressableKeyAndSceneReferenceCannotBothBeSet()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", gamePath));
            SetSerializedAddressableReference(bootstrapPath, AssetDatabase.AssetPathToGUID(gamePath), true);

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void AddressablePathMustExistAndContainOneValidLifecycleRoot()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub");
            string missingPath = _folder + "/Missing.unity";
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", missingPath));
            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Not.Empty);

            string emptyPath = CreateScene("Empty.unity", null);
            AddAddressableScene(emptyPath, "empty");
            bootstrapPath = ReplaceBootstrap(bootstrapPath, SceneTarget.Addressable("empty", emptyPath));
            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void BootstrapAndAddressableSceneCannotBothOwnSingletonRoot()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SingletonSceneRoot));
            AddAddressableScene(gamePath, "hub");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", gamePath), typeof(SingletonSceneRoot));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void AddressableValidationRejectsExplicitNullSettings()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub", gamePath));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, null), Is.Not.Empty);
        }

        [Test]
        public void MatchingReferenceWithAddressAliasOutsideBuildListPasses()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub-alias");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub-alias", gamePath));
            SetSerializedAddressableReference(bootstrapPath, AssetDatabase.AssetPathToGUID(gamePath), false);

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Empty);
        }

        [Test]
        public void DuplicateAddressEntriesAreRejected()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            string duplicatePath = CreateScene("Duplicate.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "shared");
            AddAddressableScene(duplicatePath, "shared");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("shared", gamePath));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void SceneReferenceRequiresGuidInCatalog()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub-alias");
            var entry = _settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(gamePath));
            entry.parentGroup.GetSchema<BundledAssetGroupSchema>().IncludeGUIDInCatalog = false;
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub-alias", gamePath));
            SetSerializedAddressableReference(bootstrapPath, AssetDatabase.AssetPathToGUID(gamePath), false);

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void SceneReferenceRejectsSubObjectRuntimeKeys()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub-alias");
            string bootstrapPath = CreateBootstrap(SceneTarget.Addressable("hub-alias", gamePath));
            var reference = new AssetReference(AssetDatabase.AssetPathToGUID(gamePath)) { SubObjectName = "sub" };
            SetSerializedAddressableReference(bootstrapPath, reference, false);

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath, gamePath }, _settings), Is.Not.Empty);
        }

        [Test]
        public void NativeBuildSceneStillRequiresBuildListInclusionWhenRegisteredAddressable()
        {
            string gamePath = CreateScene("Hub.unity", typeof(SceneOwnedRoot));
            AddAddressableScene(gamePath, "hub-alias");
            string bootstrapPath = CreateBootstrap(SceneTarget.BuildScene(gamePath));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrapPath }, _settings), Is.Not.Empty);
        }

        private string CreateScene(string name, Type rootType)
        {
            string path = _folder + "/" + name;
            File.WriteAllText(path, SceneHeader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                if (rootType != null) new GameObject("GameRoot").AddComponent(rootType);
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            return path;
        }

        private string CreateBootstrap(SceneTarget target, Type rootType = null)
        {
            string path = _folder + "/Bootstrap.unity";
            File.WriteAllText(path, SceneHeader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var rootObject = new GameObject("BootstrapRoot");
                var root = (MonoBehaviour)rootObject.AddComponent(rootType ?? typeof(SceneOwnedRoot));
                var bootstrap = rootObject.AddComponent<BootstrapSystem>();
                bootstrap.Configure(root, target, false);
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            return path;
        }

        private string ReplaceBootstrap(string oldPath, SceneTarget target)
        {
            AssetDatabase.DeleteAsset(oldPath);
            return CreateBootstrap(target);
        }

        private void AddAddressableScene(string path, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = _settings.CreateOrMoveEntry(guid, _settings.DefaultGroup);
            if (entry.parentGroup.GetSchema<BundledAssetGroupSchema>() == null)
                entry.parentGroup.AddSchema<BundledAssetGroupSchema>();
            entry.address = address;
        }

        private static void SetSerializedAddressableReference(string bootstrapPath, string guid, bool keepKey)
            => SetSerializedAddressableReference(bootstrapPath, new AssetReference(guid), keepKey);

        private static void SetSerializedAddressableReference(string bootstrapPath, AssetReference reference, bool keepKey)
        {
            var preview = EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Additive);
            try
            {
                var bootstrap = preview.GetRootGameObjects()[0].GetComponent<BootstrapSystem>();
                SetField(bootstrap, "_sceneReference", reference);
                if (!keepKey) SetField(bootstrap, "_addressableKey", "");
                EditorUtility.SetDirty(bootstrap);
                Assert.That(EditorSceneManager.SaveScene(preview, bootstrapPath), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(preview, true);
            }
        }

        private static void SetField<T>(BootstrapSystem bootstrap, string name, T value)
        {
            var field = typeof(BootstrapSystem).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(bootstrap, value);
        }
    }
}
