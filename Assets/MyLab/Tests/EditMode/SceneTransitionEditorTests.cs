using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyLab.Core.Editor.Bootstrap;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    public sealed class SceneTransitionEditorTests
    {
        private const string SceneHeader = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";
        private string _folder;
        private SceneTransitionSettings _settings;
        private AddressableAssetSettings _addressables;
        private AddressableAssetSettings _defaultConfigAddressables;
        private EditorBuildSettingsScene[] _originalBuildScenes;
        private bool _hadCurrentAddressablesConfig;
        private AddressableAssetSettingsDefaultObject _originalCurrentAddressablesConfig;
        private bool _hadLegacyAddressablesConfig;
        private AddressableAssetSettings _originalLegacyAddressablesConfig;
        private AddressableAssetSettings _originalCachedAddressables;
        private byte[] _originalEditorBuildSettingsBytes;
        private AddressableAssetSettings _loadedDefaultAddressables;
        private List<Delegate> _preexistingAddressablesPostprocessDelegates;

        [SetUp]
        public void SetUp()
        {
            _originalEditorBuildSettingsBytes = System.IO.File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");
            _originalBuildScenes = EditorBuildSettings.scenes.ToArray();
            _hadCurrentAddressablesConfig = EditorBuildSettings.TryGetConfigObject(
                AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName, out _originalCurrentAddressablesConfig);
            _hadLegacyAddressablesConfig = EditorBuildSettings.TryGetConfigObject(
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, out _originalLegacyAddressablesConfig);
            _originalCachedAddressables = GetCachedAddressables();
            _folder = "Assets/SceneTransitionEditorTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(_folder));
            AssetDatabase.CreateFolder(_folder, "Config");
            AssetDatabase.CreateFolder(_folder, "Addressables");
            _settings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
            AssetDatabase.CreateAsset(_settings, _folder + "/Config/Transitions.asset");
            _addressables = AddressableAssetSettings.Create(_folder + "/Addressables", "TestAddressables", true, false);
            EditorConditionProbe.EvaluationCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Exception callbackCleanupFailure = null;
            try
            {
                UnregisterOwnedAddressablesPostprocessCallback();
            }
            catch (Exception exception)
            {
                callbackCleanupFailure = exception;
            }
            try
            {
                RestoreConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                    _hadCurrentAddressablesConfig, _originalCurrentAddressablesConfig);
                RestoreConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    _hadLegacyAddressablesConfig, _originalLegacyAddressablesConfig);
                EditorBuildSettings.scenes = _originalBuildScenes;
                SetCachedAddressables(_originalCachedAddressables);
                if (_addressables != null)
                {
                    foreach (var group in _addressables.groups.Where(group => group != null))
                    {
                        foreach (var schema in group.Schemas) UnityEngine.Object.DestroyImmediate(schema);
                        UnityEngine.Object.DestroyImmediate(group);
                    }
                    UnityEngine.Object.DestroyImmediate(_addressables);
                }
                if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            }
            finally
            {
                if (_originalEditorBuildSettingsBytes != null)
                    System.IO.File.WriteAllBytes("ProjectSettings/EditorBuildSettings.asset", _originalEditorBuildSettingsBytes);
            }
            if (callbackCleanupFailure != null) throw callbackCleanupFailure;
        }

        [Test]
        public void AddressableDefinitionUsesInstalledDefaultObjectFromCurrentEditorBuildConfig()
        {
            string target = CreateScene("Main.unity", false);
            _defaultConfigAddressables = AddressableAssetSettings.Create(_folder + "/Addressables", "CurrentDefault", false, false);
            typeof(AddressableAssetSettings).GetField("m_IsTemporary", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_defaultConfigAddressables, false);
            AssetDatabase.CreateAsset(_defaultConfigAddressables, _folder + "/Addressables/CurrentDefault.asset");
            _defaultConfigAddressables.CreateGroup("Scenes", true, false, false, null, typeof(BundledAssetGroupSchema));
            AddAddressable(_defaultConfigAddressables, target, "main");
            _settings.Configure(First("entry", SceneTarget.Addressable("main", target)));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            var defaultObject = ScriptableObject.CreateInstance<AddressableAssetSettingsDefaultObject>();
            string settingsPath = AssetDatabase.GetAssetPath(_defaultConfigAddressables);
            Assert.That(settingsPath, Is.Not.Empty);
            string settingsGuid = AssetDatabase.AssetPathToGUID(settingsPath);
            Assert.That(settingsGuid, Is.Not.Empty);
            var serialized = new SerializedObject(defaultObject);
            serialized.FindProperty("m_AddressableAssetSettingsGuid").stringValue = settingsGuid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(defaultObject, _folder + "/Addressables/DefaultObject.asset");
            EditorBuildSettings.AddConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                defaultObject, true);
            SetCachedAddressables(null);
            _preexistingAddressablesPostprocessDelegates = GetAddressablesPostprocessDelegates();
            Assert.That(EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                out AddressableAssetSettingsDefaultObject currentDefaultObject), Is.True);
            Assert.That(currentDefaultObject, Is.SameAs(defaultObject));
            _loadedDefaultAddressables = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(_loadedDefaultAddressables, Is.SameAs(_defaultConfigAddressables));
            Assert.That(GetAddressablesPostprocessDelegates().Any(callback =>
                ReferenceEquals(callback.Target, _loadedDefaultAddressables)), Is.True);

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap });

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void LiveCommonRootConditionsOverrideSavedBootstrapPreviewMetadata()
        {
            string target = CreateScene("Main.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(target)));
            string bootstrapPath = CreateBootstrap("Bootstrap.unity", false, "entry", "policy", "policy");
            var liveScene = EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Additive);
            try
            {
                var bootstrap = liveScene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<BootstrapSystem>(true)).Single();
                var root = bootstrap.SceneRoot;
                var conditions = root.GetComponents<SceneTransitionCondition>();
                Assert.That(conditions.Length, Is.EqualTo(2));
                UnityEngine.Object.DestroyImmediate(conditions[1]);

                var errors = BootstrapValidator.ValidateBootstrap(bootstrap, _addressables);

                Assert.That(errors.Any(error => error.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(liveScene, true);
            }
        }

        [TestCase(LoadSceneMode.Additive, false)]
        [TestCase(LoadSceneMode.Single, true)]
        public void ValidFirstEntryModesPassWithCorrectCommonLifetime(LoadSceneMode mode, bool persistent)
        {
            string target = CreateScene("Main.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(target), mode));
            string bootstrap = CreateBootstrap("Bootstrap.unity", persistent, "entry");

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, target }, _addressables);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void BuildDestinationMustExistInActualPlayerSceneList()
        {
            string source = CreateScene("Main.unity", false);
            string omittedTarget = CreateScene("Next.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(source)),
                new SceneTransitionDefinition("replace", SceneTransitionKind.ReplacePrimary, source,
                    SceneTarget.BuildScene(omittedTarget)));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, source }, _addressables), Is.Not.Empty);
        }

        [Test]
        public void AddressableDestinationMustExistAndHaveExplicitMapping()
        {
            string target = CreateScene("Main.unity", false);
            _settings.Configure(First("entry", SceneTarget.Addressable("main", target)));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap }, _addressables);
            Assert.That(errors, Is.Not.Empty);

            AddAddressable(target, "main");
            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrap }, _addressables), Is.Empty);
        }

        [Test]
        public void MalformedSettingsAndDuplicateDefinitionIdsAreRejected()
        {
            string target = CreateScene("Main.unity", false);
            var definition = First("entry", SceneTarget.BuildScene(target));
            _settings.Configure(definition);
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            SetDefinitions(First("entry", SceneTarget.BuildScene(target)), First("entry", SceneTarget.BuildScene(target)));

            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, target }, _addressables), Is.Not.Empty);
        }

        [Test]
        public void RequiredConditionMustExistOnPotentiallyAffectedRoots()
        {
            string source = CreateScene("Hub.unity", false);
            string target = CreateScene("Area.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(target)),
                new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, source,
                    SceneTarget.BuildScene(target), requiredConditionIds: new[] { "missing" }));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, target, source }, _addressables);
            Assert.That(errors.Any(error => error.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
        }

        [Test]
        public void DuplicateConditionIdsOnOneRootAreRejectedButNeverEvaluated()
        {
            string source = CreateScene("Hub.unity", false, "policy", "policy");
            string target = CreateScene("Area.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(target)),
                new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, source, SceneTarget.BuildScene(target)));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            AssertConditionPresent(source, "policy");

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, target, source }, _addressables);

            Assert.That(errors.Any(error => error.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Assert.That(EditorConditionProbe.EvaluationCount, Is.Zero);
        }

        [Test]
        public void NestedDerivedConditionUnionIsVisitedSafelyAndNeverEvaluated()
        {
            string hub = CreateScene("Hub.unity", false);
            string area = CreateScene("Area.unity", false);
            string nested = CreateScene("Nested.unity", false, "deep-policy");
            string main = CreateScene("Main.unity", false);
            _settings.Configure(
                First("entry", SceneTarget.BuildScene(main)),
                new SceneTransitionDefinition("area", SceneTransitionKind.AddDerived, hub, SceneTarget.BuildScene(area)),
                new SceneTransitionDefinition("nested", SceneTransitionKind.AddDerived, area, SceneTarget.BuildScene(nested)),
                new SceneTransitionDefinition("cycle", SceneTransitionKind.AddDerived, nested, SceneTarget.BuildScene(hub)),
            new SceneTransitionDefinition("remove", SceneTransitionKind.RemoveDerived, hub,
                    SceneTarget.BuildScene(area), requiredConditionIds: new[] { "deep-policy" }));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            AssertConditionPresent(nested, "deep-policy");

            var errors = BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, main, hub, area, nested }, _addressables);

            Assert.That(errors, Is.Empty);
            Assert.That(EditorConditionProbe.EvaluationCount, Is.Zero);
        }

        [Test]
        public void MalformedBootstrapDefinitionReturnsDiagnosticsBeforeEffectiveGetters()
        {
            string target = CreateScene("Main.unity", false);
            var first = First("same", SceneTarget.BuildScene(target));
            SetDefinitions(first, First("same", SceneTarget.BuildScene(target)));
            var host = new GameObject("MalformedDefinitionBootstrap");
            try
            {
                var root = host.AddComponent<SceneOwnedRoot>();
                root.Configure(null, false);
                var bootstrap = host.AddComponent<BootstrapSystem>();
                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("_transitionSettings").objectReferenceValue = _settings;
                serialized.FindProperty("_firstTransitionId").stringValue = "same";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                IReadOnlyList<string> errors = null;

                Assert.DoesNotThrow(() => errors = BootstrapValidator.ValidateBootstrap(bootstrap, _addressables));
                Assert.That(errors, Is.Not.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void ValidationPreservesLoadedActivePreviewAndBuildSettings()
        {
            string target = CreateScene("Main.unity", false);
            _settings.Configure(First("entry", SceneTarget.BuildScene(target)));
            string bootstrap = CreateBootstrap("Bootstrap.unity", false, "entry");
            var active = SceneManager.GetActiveScene();
            var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(scene => scene.path).ToArray();
            var buildSettings = EditorBuildSettings.scenes.Select(scene => scene.path + "|" + scene.enabled).ToArray();
            int previews = EditorSceneManager.previewSceneCount;

            BootstrapValidator.ValidateBuildScenes(new[] { bootstrap, target }, _addressables);

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(scene => scene.path), Is.EqualTo(loaded));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path + "|" + scene.enabled), Is.EqualTo(buildSettings));
        }

        private SceneTransitionDefinition First(string id, SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive)
            => new SceneTransitionDefinition(id, SceneTransitionKind.FirstEntry, "", target, mode);

        private string CreateScene(string name, bool persistent, params string[] conditionIds)
        {
            string path = _folder + "/" + name;
            System.IO.File.WriteAllText(path, SceneHeader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("SceneRoot").AddComponent<SceneOwnedRoot>();
                root.Configure(null, persistent);
                foreach (string id in conditionIds) root.gameObject.AddComponent<EditorConditionProbe>().Id = id;
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
            }
            return path;
        }

        private string CreateBootstrap(string name, bool persistent, string firstId, params string[] conditionIds)
        {
            string path = _folder + "/" + name;
            System.IO.File.WriteAllText(path, SceneHeader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("CommonRoot").AddComponent<SceneOwnedRoot>();
                root.Configure(null, persistent);
                foreach (string id in conditionIds) root.gameObject.AddComponent<EditorConditionProbe>().Id = id;
                var bootstrap = root.gameObject.AddComponent<BootstrapSystem>();
                bootstrap.Configure(root, _settings, firstId, false);
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
            }
            return path;
        }

        private void AddAddressable(string path, string address)
            => AddAddressable(_addressables, path, address);

        private static void AddAddressable(AddressableAssetSettings settings, string path, string address)
        {
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), settings.DefaultGroup);
            if (entry.parentGroup.GetSchema<BundledAssetGroupSchema>() == null)
                entry.parentGroup.AddSchema<BundledAssetGroupSchema>();
            entry.address = address;
        }

        private void SetDefinitions(params SceneTransitionDefinition[] definitions)
        {
            typeof(SceneTransitionSettings).GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_settings, definitions);
            EditorUtility.SetDirty(_settings);
        }

        private static void AssertConditionPresent(string scenePath, string id)
        {
            var preview = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var conditions = preview.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<SceneTransitionCondition>(true)).ToArray();
                Assert.That(conditions.Any(condition => condition is EditorConditionProbe probe && probe.Id == id), Is.True,
                    "Saved test scene must contain the intended condition MonoBehaviour.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static AddressableAssetSettings GetCachedAddressables()
        {
            var field = typeof(AddressableAssetSettingsDefaultObject).GetField("s_DefaultSettingsObject",
                BindingFlags.Static | BindingFlags.NonPublic);
            return (AddressableAssetSettings)field.GetValue(null);
        }

        private static void SetCachedAddressables(AddressableAssetSettings settings)
        {
            var field = typeof(AddressableAssetSettingsDefaultObject).GetField("s_DefaultSettingsObject",
                BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, settings);
        }

        private static void RestoreConfigObject<T>(string key, bool existed, T value) where T : UnityEngine.Object
        {
            if (existed) EditorBuildSettings.AddConfigObject(key, value, true);
            else EditorBuildSettings.RemoveConfigObject(key);
        }

        private void UnregisterOwnedAddressablesPostprocessCallback()
        {
            if (_loadedDefaultAddressables == null) return;
            object handler = GetAddressablesPostprocessHandler();
            Type delegateType = handler.GetType().GetNestedType("Delegate", BindingFlags.Public);
            if (delegateType.ContainsGenericParameters)
                delegateType = delegateType.MakeGenericType(handler.GetType().GetGenericArguments());
            var method = typeof(AddressableAssetSettings).GetMethod("OnPostprocessAllAssets", BindingFlags.Instance | BindingFlags.NonPublic);
            var callback = Delegate.CreateDelegate(delegateType, _loadedDefaultAddressables, method);
            handler.GetType().GetMethod("Unregister").Invoke(handler, new object[] { callback });
            var callbacksAfterCleanup = GetAddressablesPostprocessDelegates();
            Assert.That(callbacksAfterCleanup.Any(item => item.Equals(callback)), Is.False);
            foreach (var existing in _preexistingAddressablesPostprocessDelegates ?? new List<Delegate>())
                Assert.That(callbacksAfterCleanup.Contains(existing), Is.True, "Existing Addressables postprocess callbacks must remain registered.");
        }

        private static List<Delegate> GetAddressablesPostprocessDelegates()
        {
            object handler = GetAddressablesPostprocessHandler();
            var invocationField = handler.GetType().GetField("m_SortedInvocationList", BindingFlags.Instance | BindingFlags.NonPublic);
            var invocationList = (IDictionary)invocationField.GetValue(handler);
            var callbacks = new List<Delegate>();
            foreach (DictionaryEntry entry in invocationList)
                callbacks.AddRange(((Delegate)entry.Value).GetInvocationList());
            return callbacks;
        }

        private static object GetAddressablesPostprocessHandler()
        {
            var type = typeof(AddressableAssetSettings).Assembly.GetType(
                "UnityEditor.AddressableAssets.Settings.AddressablesAssetPostProcessor");
            return type.GetProperty("OnPostProcess", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }
    }
}
