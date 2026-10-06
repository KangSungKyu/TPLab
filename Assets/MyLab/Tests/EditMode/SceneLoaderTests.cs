using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyLab.Core.Tests
{
    public sealed class SceneLoaderTests
    {
        private const string ScenePath = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private Scene _scene;

        [SetUp]
        public void SetUp() => _scene = EditorSceneManager.NewPreviewScene();

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void FactoriesPreserveExplicitBackendAndSceneIdentity()
        {
            var build = SceneTarget.BuildScene(ScenePath);
            var addressable = SceneTarget.Addressable("hub", ScenePath);
            Assert.That(build.Source, Is.EqualTo(SceneSource.BuildScene));
            Assert.That(build.ScenePath, Is.EqualTo(ScenePath));
            Assert.That(build.AddressableKey, Is.Null);
            Assert.That(addressable.Source, Is.EqualTo(SceneSource.Addressable));
            Assert.That(addressable.ScenePath, Is.EqualTo(ScenePath));
            Assert.That(addressable.AddressableKey, Is.EqualTo("hub"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Scenes/Hub.unity")]
        [TestCase("Assets/../Hub.unity")]
        [TestCase("Assets//Hub.unity")]
        [TestCase("Assets\\Hub.unity")]
        [TestCase("Assets/Hub.prefab")]
        public void FactoriesRejectMalformedScenePaths(string path)
        {
            Assert.Throws<InvalidOperationException>(() => SceneTarget.BuildScene(path));
            Assert.Throws<InvalidOperationException>(() => SceneTarget.Addressable("hub", path));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t")]
        public void AddressableTargetRequiresAnExplicitKey(string key)
            => Assert.Throws<ArgumentException>(() => SceneTarget.Addressable(key, ScenePath));

        [Test]
        public void ReferenceTargetUsesItsRuntimeKeyAndRejectsMissingReferences()
        {
            var reference = new AssetReference("0123456789abcdef0123456789abcdef");
            var target = SceneTarget.Addressable(reference, ScenePath);
            Assert.That(target.Source, Is.EqualTo(SceneSource.Addressable));
            Assert.That(target.AddressableKey, Is.EqualTo(reference.RuntimeKey.ToString()));
            Assert.That(target.ScenePath, Is.EqualTo(ScenePath));
            Assert.Throws<ArgumentNullException>(() => SceneTarget.Addressable((AssetReference)null, ScenePath));
            Assert.Throws<ArgumentException>(() => SceneTarget.Addressable(new AssetReference(""), ScenePath));
        }

        [Test]
        public void InvalidSourceAndBuildAddressKeyAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SceneTarget((SceneSource)99, ScenePath));
            Assert.Throws<ArgumentException>(() => new SceneTarget(SceneSource.BuildScene, ScenePath, "hub"));
            Assert.Throws<InvalidOperationException>(() => default(SceneTarget).Validate());
        }

        [UnityTest]
        public IEnumerator UnloadStartsOnceAndAllCallersWaitForItsCompletion() => UniTask.ToCoroutine(async () =>
        {
            int calls = 0;
            var gate = new UniTaskCompletionSource();
            var loaded = new LoadedScene(SceneTarget.BuildScene(ScenePath), _scene, () =>
            {
                ++calls;
                return gate.Task;
            });
            var first = loaded.UnloadAsync().AsTask();
            var second = loaded.UnloadAsync().AsTask();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.IsCompleted, Is.False);
            Assert.That(loaded.IsUnloaded, Is.False);
            EditorSceneManager.ClosePreviewScene(_scene);
            gate.TrySetResult();
            await first;
            await second;
            await loaded.UnloadAsync();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(loaded.IsUnloaded, Is.True);
            Assert.That(loaded.Scene, Is.EqualTo(_scene));
        });

        [UnityTest]
        public IEnumerator UnloadFailureIsSharedWithoutRetryOrSuccessPublication() => UniTask.ToCoroutine(async () =>
        {
            int calls = 0;
            var gate = new UniTaskCompletionSource();
            var expected = new InvalidOperationException("expected unload failure");
            var loaded = new LoadedScene(SceneTarget.BuildScene(ScenePath), _scene, () =>
            {
                ++calls;
                return gate.Task;
            });
            var first = loaded.UnloadAsync().AsTask();
            var second = loaded.UnloadAsync().AsTask();
            gate.TrySetException(expected);
            Exception firstFailure = null;
            Exception secondFailure = null;
            try { await first; } catch (Exception failure) { firstFailure = failure; }
            try { await second; } catch (Exception failure) { secondFailure = failure; }
            Assert.That(firstFailure, Is.SameAs(expected));
            Assert.That(secondFailure, Is.SameAs(expected));
            Exception laterFailure = null;
            try { await loaded.UnloadAsync(); } catch (Exception failure) { laterFailure = failure; }
            Assert.That(laterFailure, Is.SameAs(expected));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(loaded.IsUnloaded, Is.False);
            Assert.That(_scene.isLoaded, Is.True);
        });

        [Test]
        public void OwnershipRequiresARealLoadedSceneAndUnloadDelegate()
        {
            var target = SceneTarget.BuildScene(ScenePath);
            Assert.Throws<ArgumentException>(() => new LoadedScene(target, default, () => UniTask.CompletedTask));
            Assert.Throws<ArgumentNullException>(() => new LoadedScene(target, _scene, null));
        }

        [UnityTest]
        public IEnumerator BackendCannotPublishUnloadSuccessWhileSceneRemainsLoaded() => UniTask.ToCoroutine(async () =>
        {
            var loaded = new LoadedScene(SceneTarget.BuildScene(ScenePath), _scene, () => UniTask.CompletedTask);
            Exception failure = null;
            try { await loaded.UnloadAsync(); } catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(loaded.IsUnloaded, Is.False);
            Assert.That(_scene.isLoaded, Is.True);
        });

        [Test]
        public void BootstrapTargetConfigurationPreservesLegacyBuildSelection()
        {
            var host = new GameObject("SceneTargetConfiguration");
            host.SetActive(false);
            try
            {
                var bootstrap = host.AddComponent<BootstrapSystem>();
                bootstrap.Configure(null, SceneTarget.Addressable("hub", ScenePath), false);
                Assert.That(bootstrap.Source, Is.EqualTo(SceneSource.Addressable));
                Assert.That(bootstrap.FirstSceneTarget.AddressableKey, Is.EqualTo("hub"));
                bootstrap.Configure(null, ScenePath, false);
                Assert.That(bootstrap.Source, Is.EqualTo(SceneSource.BuildScene));
                Assert.That(bootstrap.AddressableKey, Is.Empty);
                Assert.That(bootstrap.SceneReference, Is.Null);
                Assert.That(bootstrap.FirstSceneTarget.ScenePath, Is.EqualTo(ScenePath));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
