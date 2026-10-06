using System;
using System.IO;
using MyLab.Core.Editor.Bootstrap;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    public sealed class BootstrapSystemTests
    {
        private const string Hub = "Assets/Game/Hub.unity";

        [Test]
        public void DefaultsRequireExplicitRootAndDestination()
        {
            var go = new GameObject("UnconfiguredBootstrap");
            try
            {
                var bootstrap = go.AddComponent<BootstrapSystem>();
                Assert.That(bootstrap.AutoStart, Is.True);
                Assert.That(bootstrap.SceneRoot, Is.Null);
                Assert.That(bootstrap.FirstScenePath, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ValidSceneOwnedOrSingletonRootCanBeSelected(bool singleton)
        {
            WithRoot((bootstrap, root) => bootstrap.ValidateConfiguration(), singleton);
        }

        [Test]
        public void MissingRootIsRejected()
        {
            WithRoot((bootstrap, root) =>
            {
                bootstrap.Configure(null, Hub, false);
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
            });
        }

        [Test]
        public void ArbitraryComponentCannotActAsSceneRoot()
        {
            WithRoot((bootstrap, root) =>
            {
                bootstrap.Configure(bootstrap, Hub, false);
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
            });
        }

        [Test]
        public void RootMustRemainTopLevelAndInTheBootstrapScene()
        {
            WithRoot((bootstrap, root) =>
            {
                var parent = new GameObject("Parent");
                try
                {
                    root.transform.SetParent(parent.transform);
                    Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
                    root.transform.SetParent(null);
                    var otherScene = EditorSceneManager.NewPreviewScene();
                    try
                    {
                        var foreign = new GameObject("Foreign");
                        SceneManager.MoveGameObjectToScene(foreign, otherScene);
                        bootstrap.Configure(foreign.AddComponent<SceneOwnedRoot>(), Hub, false);
                        Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
                    }
                    finally
                    {
                        EditorSceneManager.ClosePreviewScene(otherScene);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(parent);
                }
            });
        }

        [Test]
        public void DuplicateOrPersistentHostsCannotOwnBootstrap()
        {
            WithRoot((bootstrap, root) =>
            {
                var duplicate = root.gameObject.AddComponent<SingletonSceneRoot>();
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
                UnityEngine.Object.DestroyImmediate(duplicate);
                ((SceneOwnedRoot)root).Configure(Array.Empty<SceneRootInstaller>(), true);
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
            });
        }

        [TestCase("")]
        [TestCase("Hub")]
        [TestCase("Packages/Hub.unity")]
        [TestCase("Assets/../Hub.unity")]
        [TestCase("Assets/Game/Hub.prefab")]
        public void InvalidScenePathsAreRejected(string path)
        {
            WithRoot((bootstrap, root) =>
            {
                bootstrap.Configure(root, path, false);
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
            });
        }

        [Test]
        public void BootstrapCannotTargetItsOwnScene()
        {
            WithRoot((bootstrap, root) =>
            {
                bootstrap.Configure(root, root.gameObject.scene.path, false);
                Assert.Throws<InvalidOperationException>(() => bootstrap.ValidateConfiguration());
            });
        }

        [Test]
        public void ValidSavedBootstrapAndGameScenesPassBuildPreflight()
        {
            WithScenes((folder, boot, game) => Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Empty));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavedSharedAndLegacyCallbackReferencesSurviveReload(bool legacy)
        {
            WithScenes((folder, boot, game) =>
            {
                SaveScene(boot, scene =>
                {
                    var go = new GameObject("BootstrapWithCallbacks");
                    SceneTransitionCallbacks callbacks = legacy
                        ? (SceneTransitionCallbacks)go.AddComponent<BootstrapCallbacksProbe>()
                        : go.AddComponent<SceneTransitionCallbacksProbe>();
                    go.AddComponent<BootstrapSystem>().Configure(go.AddComponent<SceneOwnedRoot>(), game, false, callbacks);
                });
                var preview = EditorSceneManager.OpenPreviewScene(boot);
                try
                {
                    var bootstrap = preview.GetRootGameObjects()[0].GetComponent<BootstrapSystem>();
                    var callbacks = new SerializedObject(bootstrap).FindProperty("_callbacks").objectReferenceValue;
                    Type expectedType = legacy ? typeof(BootstrapCallbacksProbe) : typeof(SceneTransitionCallbacksProbe);
                    Assert.That(callbacks, Is.Not.Null);
                    Assert.That(callbacks.GetType(), Is.EqualTo(expectedType));
                    Assert.DoesNotThrow(() => bootstrap.ValidateConfiguration());
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(preview);
                }
            });
        }

        [Test]
        public void BootstrapMustBeFirstAndDestinationMustBeIncluded()
        {
            WithScenes((folder, boot, game) =>
            {
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { game, boot }), Is.Not.Empty);
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot }), Is.Not.Empty);
            });
        }

        [Test]
        public void SavedInvalidRootStopsBuildPreflight()
        {
            WithScenes((folder, boot, game) =>
            {
                SaveScene(boot, scene =>
                {
                    var go = new GameObject("InvalidBootstrap");
                    go.AddComponent<BootstrapSystem>().Configure(null, game, true);
                });
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Not.Empty);
            });
        }

        [Test]
        public void DestinationRequiresItsOwnValidSceneRoot()
        {
            WithScenes((folder, boot, game) =>
            {
                SaveScene(game, scene => { });
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Not.Empty);
            });
        }

        [Test]
        public void InvalidSerializedInstallerStopsBuildPreflight()
        {
            WithScenes((folder, boot, game) =>
            {
                SaveScene(boot, scene =>
                {
                    var go = new GameObject("Bootstrap");
                    var root = go.AddComponent<SceneOwnedRoot>();
                    go.AddComponent<BootstrapSystem>().Configure(root, game, true);
                    var serialized = new SerializedObject(root);
                    serialized.FindProperty("_installers").arraySize = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                });
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Not.Empty);
            });
        }

        [Test]
        public void ProjectsWithoutBootstrapKeepTheirExistingBuildFlow()
        {
            WithScenes((folder, boot, game) => Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { game }), Is.Empty));
        }

        [Test]
        public void LegacyMissingBuildScenesAreLeftToUnityWhenBootstrapIsAbsent()
        {
            Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { "Assets/Removed.unity" }), Is.Empty);
        }

        [Test]
        public void SavedSceneValidationPreservesActiveSceneAndClosesPreviews()
        {
            WithScenes((folder, boot, game) =>
            {
                var active = SceneManager.GetActiveScene();
                int count = EditorSceneManager.previewSceneCount;
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Empty);
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
                Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(count));
            });
        }

        [Test]
        public void BootstrapAndGameCannotBothClaimSingletonSceneRoot()
        {
            WithScenes((folder, boot, game) =>
            {
                SaveScene(boot, scene =>
                {
                    var go = new GameObject("BootstrapSingleton");
                    go.AddComponent<BootstrapSystem>().Configure(go.AddComponent<SingletonSceneRoot>(), game);
                });
                SaveScene(game, scene => new GameObject("GameSingleton").AddComponent<SingletonSceneRoot>());
                Assert.That(BootstrapValidator.ValidateBuildScenes(new[] { boot, game }), Is.Not.Empty);
            });
        }

        private static void WithRoot(Action<BootstrapSystem, MonoBehaviour> test, bool singleton = false)
        {
            var go = new GameObject("BootstrapTest");
            try
            {
                MonoBehaviour root = singleton ? (MonoBehaviour)go.AddComponent<SingletonSceneRoot>() : go.AddComponent<SceneOwnedRoot>();
                var bootstrap = go.AddComponent<BootstrapSystem>();
                bootstrap.Configure(root, Hub, false);
                test(bootstrap, root);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void WithScenes(Action<string, string, string> test)
        {
            string folder = "Assets/BootstrapTest_" + Guid.NewGuid().ToString("N");
            string fullFolder = Path.GetFullPath(folder);
            if (!fullFolder.StartsWith(Path.GetFullPath("Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                Directory.Exists(fullFolder) || File.Exists(fullFolder + ".meta"))
            {
                throw new InvalidOperationException("Fixture path must be unused and remain inside Assets.");
            }
            Directory.CreateDirectory(fullFolder);
            try
            {
                string boot = folder + "/Bootstrap.unity", game = folder + "/Hub.unity";
                SaveScene(game, scene => new GameObject("GameRoot").AddComponent<SceneOwnedRoot>());
                SaveScene(boot, scene =>
                {
                    var go = new GameObject("Bootstrap");
                    go.AddComponent<BootstrapSystem>().Configure(go.AddComponent<SceneOwnedRoot>(), game);
                });
                test(folder, boot, game);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                if (Directory.Exists(fullFolder)) Directory.Delete(fullFolder, true);
            }
        }

        private static void SaveScene(string path, Action<Scene> setup)
        {
            var previous = SceneManager.GetActiveScene();
            File.WriteAllText(path, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
            AssetDatabase.ImportAsset(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                setup(scene);
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
