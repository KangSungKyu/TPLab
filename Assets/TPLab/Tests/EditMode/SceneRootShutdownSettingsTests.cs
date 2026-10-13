using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TPLab.Core.Lifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPLab.Core.Tests
{
    public class SceneRootShutdownSettingsTests
    {
        private Scene _scene;
        private GameObject _object;
        private string _assetPath;

        [SetUp]
        public void SetUp()
        {
            // UTF creates and later restores its bootstrap scene around the EditMode run.
            Assert.That(TestContext.CurrentContext.Test.FullName,
                Does.StartWith(GetType().FullName + "."), "Only execute this fixture through the test runner.");
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(_scene);
            _object = new GameObject("ShutdownSettingsTestRoot");
            _object.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_object != null)
            {
                UnityEngine.Object.DestroyImmediate(_object);
            }
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            if (_assetPath != null)
            {
                AssetDatabase.DeleteAsset(_assetPath);
            }
        }

        private static MethodInfo FindAttach()
        {
            var method = typeof(SceneRootSetup).GetMethod(nameof(SceneRootSetup.Attach),
                new[] { typeof(GameObject), typeof(SceneRootMode), typeof(SceneRootInstaller[]), typeof(bool), typeof(float) });
            Assert.That(method, Is.Not.Null, "Attach must expose the confirmed optional shutdownTimeoutSeconds argument.");
            var parameter = method.GetParameters()[4];
            Assert.That(parameter.Name, Is.EqualTo("shutdownTimeoutSeconds"));
            Assert.That(parameter.IsOptional, Is.True);
            Assert.That(parameter.DefaultValue, Is.EqualTo(60f));
            return method;
        }

        private static MethodInfo FindConfigure(ISceneRoot root)
        {
            var method = root.GetType().GetMethod("Configure",
                new[] { typeof(SceneRootInstaller[]), typeof(bool), typeof(float) });
            Assert.That(method, Is.Not.Null, "Configure must expose the confirmed optional shutdownTimeoutSeconds argument.");
            var parameter = method.GetParameters()[2];
            Assert.That(parameter.Name, Is.EqualTo("shutdownTimeoutSeconds"));
            Assert.That(parameter.IsOptional, Is.True);
            Assert.That(parameter.DefaultValue, Is.EqualTo(60f));
            return method;
        }

        private static float ReadTimeout(ISceneRoot root)
        {
            using (var serialized = new SerializedObject((UnityEngine.Object)root))
            {
                var property = serialized.FindProperty("_shutdownTimeoutSeconds");
                Assert.That(property, Is.Not.Null, "The host must expose the serialized timeout setting.");
                return property.floatValue;
            }
        }

        [TestCase(SceneRootMode.SceneOwned)]
        [TestCase(SceneRootMode.Singleton)]
        public void ConfigureHasOptionalDefaultAndForwardsFiniteValues(SceneRootMode mode)
        {
            var root = SceneRootSetup.Attach(_object, mode);
            Assert.That(ReadTimeout(root), Is.EqualTo(60f));
            var configure = FindConfigure(root);
            foreach (float value in new[] { 0.25f, 0f, -1f })
            {
                configure.Invoke(root, new object[] { null, false, value });
                Assert.That(ReadTimeout(root), Is.EqualTo(value));
            }
        }

        [TestCase(SceneRootMode.SceneOwned)]
        [TestCase(SceneRootMode.Singleton)]
        public void AttachHasOptionalDefaultAndForwardsTimeout(SceneRootMode mode)
        {
            var root = (ISceneRoot)FindAttach().Invoke(null, new object[] { _object, mode, null, false, 0.25f });
            Assert.That(ReadTimeout(root), Is.EqualTo(0.25f));
        }

        [TestCase(SceneRootMode.SceneOwned)]
        [TestCase(SceneRootMode.Singleton)]
        public void InvalidTimeoutRejectsAttachBeforeAddingAHost(SceneRootMode mode)
        {
            var attach = FindAttach();
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var error = Assert.Throws<TargetInvocationException>(() =>
                    attach.Invoke(null, new object[] { _object, mode, null, false, value }));
                Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(((ArgumentOutOfRangeException)error.InnerException).ParamName, Is.EqualTo("shutdownTimeoutSeconds"));
                Assert.That(_object.GetComponent<SceneOwnedRoot>(), Is.Null);
                Assert.That(_object.GetComponent<SingletonSceneRoot>(), Is.Null);
            }
        }

        [TestCase(SceneRootMode.SceneOwned)]
        [TestCase(SceneRootMode.Singleton)]
        public void InvalidTimeoutRejectsConfigureBeforeChangingExistingSettings(SceneRootMode mode)
        {
            var root = (ISceneRoot)FindAttach().Invoke(null, new object[] { _object, mode, null, false, 0.25f });
            var configure = FindConfigure(root);
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var error = Assert.Throws<TargetInvocationException>(() =>
                    configure.Invoke(root, new object[] { null, true, value }));
                Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(ReadTimeout(root), Is.EqualTo(0.25f));
                using (var serialized = new SerializedObject((UnityEngine.Object)root))
                {
                    Assert.That(serialized.FindProperty("_persistAcrossScenes").boolValue, Is.False);
                }
            }
        }

        [TestCase(SceneRootMode.SceneOwned, false, true)]
        [TestCase(SceneRootMode.Singleton, false, true)]
        [TestCase(SceneRootMode.SceneOwned, true, true)]
        [TestCase(SceneRootMode.Singleton, true, true)]
        [TestCase(SceneRootMode.SceneOwned, false, false)]
        [TestCase(SceneRootMode.Singleton, false, false)]
        [TestCase(SceneRootMode.SceneOwned, true, false)]
        [TestCase(SceneRootMode.Singleton, true, false)]
        public void LegacyAssetsUseDefaultForMissingFieldAndPreserveExplicitZero(SceneRootMode mode, bool prefab, bool missing)
        {
            var root = SceneRootSetup.Attach(_object, mode);
            if (!missing)
            {
                JsonUtility.FromJsonOverwrite("{\"_shutdownTimeoutSeconds\":0}", (UnityEngine.Object)root);
            }
            string suffix = prefab ? ".prefab" : ".unity";
            _assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/TPLab/Tests/Fixtures/Imp03LegacyRoot" + suffix);
            if (prefab)
            {
                PrefabUtility.SaveAsPrefabAsset(_object, _assetPath);
                UnityEngine.Object.DestroyImmediate(_object);
            }
            else
            {
                Assert.That(EditorSceneManager.SaveScene(_scene, _assetPath), Is.True);
                _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            if (missing)
            {
                string yaml = File.ReadAllText(_assetPath);
                const string fieldPattern = @"^\s*_shutdownTimeoutSeconds:.*\r?\n";
                Assert.That(Regex.Matches(yaml, fieldPattern, RegexOptions.Multiline).Count, Is.EqualTo(1),
                    "The newly saved host must contain exactly one timeout field before creating the legacy fixture.");
                yaml = Regex.Replace(yaml, fieldPattern, "", RegexOptions.Multiline);
                Assert.That(Regex.IsMatch(yaml, fieldPattern, RegexOptions.Multiline), Is.False);
                File.WriteAllText(_assetPath, yaml);
            }
            AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
            if (prefab)
            {
                var contents = PrefabUtility.LoadPrefabContents(_assetPath);
                try
                {
                    var loaded = (ISceneRoot)contents.GetComponent(mode == SceneRootMode.SceneOwned ? typeof(SceneOwnedRoot) : typeof(SingletonSceneRoot));
                    Assert.That(ReadTimeout(loaded), Is.EqualTo(missing ? 60f : 0f));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            else
            {
                _scene = EditorSceneManager.OpenScene(_assetPath, OpenSceneMode.Single);
                _object = _scene.GetRootGameObjects()[0];
                var loaded = (ISceneRoot)_object.GetComponent(mode == SceneRootMode.SceneOwned ? typeof(SceneOwnedRoot) : typeof(SingletonSceneRoot));
                Assert.That(ReadTimeout(loaded), Is.EqualTo(missing ? 60f : 0f));
            }
        }
    }
}
