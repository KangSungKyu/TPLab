using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class GameSceneEntryTests
    {
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private GameObject _host;
        private ISceneRoot _root;
        private SceneRootInstallerProbe _installer;
        private SceneTransitionCallbacksProbe _callbacks;
        private BootstrapSystem _bootstrap;
        private GameSceneManager _directManager;

        private void CreateBootstrap(LoadSceneMode mode, bool singleton = false)
        {
            _host = new GameObject("PersistentEntryTest");
            _host.SetActive(false);
            _installer = _host.AddComponent<SceneRootInstallerProbe>();
            _installer.Id = "common";
            if (singleton)
            {
                var root = _host.AddComponent<SingletonSceneRoot>();
                root.Configure(new SceneRootInstaller[] { _installer }, true);
                _root = root;
            }
            else
            {
                var root = _host.AddComponent<SceneOwnedRoot>();
                root.Configure(new SceneRootInstaller[] { _installer }, true);
                _root = root;
            }
            _callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _bootstrap = _host.AddComponent<BootstrapSystem>();
            _bootstrap.Configure((MonoBehaviour)_root, Hub, false, _callbacks, mode);
            _host.SetActive(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            // This belongs to the test harness, not the core's failure policy. Keep one normal scene for later tests.
            if (SceneManager.sceneCount == 1 && SceneManager.GetSceneAt(0).path == Hub)
                SceneManager.CreateScene("SceneEntryTestRecovery");
            if (_bootstrap != null)
            {
                try
                {
                    await _bootstrap.ShutdownAsync();
                }
                catch (Exception) { }
            }
            if (_directManager != null) await _directManager.ShutdownAsync();
            if (_root != null && _root.RootObject != null) await _root.ShutdownAsync();
            var remaining = SceneManager.GetSceneByPath(Hub);
            if (remaining.IsValid() && remaining.isLoaded) await SceneManager.UnloadSceneAsync(remaining).ToUniTask();
            if (_host != null) UnityEngine.Object.Destroy(_host);
            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator DirectManagerSharesWaitWithoutCancellingItsOwner() => UniTask.ToCoroutine(async () =>
        {
            CreateBootstrap(LoadSceneMode.Additive);
            _installer.PrepareGate = new UniTaskCompletionSource();
            _directManager = new GameSceneManager((MonoBehaviour)_root, _callbacks);
            Assert.Throws<InvalidOperationException>(() => _directManager.WaitForEntryAsync());
            using (var cancellation = new CancellationTokenSource())
            {
                var entry = _directManager.EnterFirstSceneAsync(Hub, cancellationToken: cancellation.Token);
                cancellation.Cancel();
                bool cancelled = false;
                try
                {
                    await entry;
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                Assert.That(cancelled, Is.True);
                Assert.That(_directManager.LastFailure, Is.Null);
                _installer.PrepareGate.TrySetResult();
                await _directManager.WaitForEntryAsync();
                Assert.That(_directManager.CanProceed, Is.True);
                Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            }
            await _directManager.ShutdownAsync();
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator ExplicitShutdownCanFinishFailedSingleCleanupAfterExternalSceneIsAvailable() => UniTask.ToCoroutine(async () =>
        {
            CreateBootstrap(LoadSceneMode.Single);
            var entry = _bootstrap.BootstrapAsync();
            _bootstrap.Manager.CancelTransition();
            Exception failure = null;
            try
            {
                await entry;
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            var remaining = _bootstrap.Manager.LoadedScene;
            var installer = remaining.GetRootGameObjects()[0].GetComponent<SceneRootInstallerProbe>();
            Assert.That(installer.ReleaseCount, Is.EqualTo(1));
            var external = SceneManager.CreateScene("ExplicitCleanupDestinationTest");
            await _bootstrap.ShutdownAsync();
            Assert.That(remaining.isLoaded, Is.False);
            Assert.That(external.isLoaded, Is.True);
            Assert.That(installer.ReleaseCount, Is.EqualTo(1));
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Stopped));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator PersistentAdditiveEntryRetainsBootstrapAndCommonServices() => UniTask.ToCoroutine(async () =>
        {
            var original = SceneManager.GetActiveScene();
            CreateBootstrap(LoadSceneMode.Additive);
            await _bootstrap.BootstrapAsync();
            Assert.That(original.isLoaded, Is.True);
            Assert.That(_root.IsPrepared, Is.True);
            Assert.That(_callbacks.PresentationReceivedPreparedRoot, Is.True);
            Assert.That(_host.scene, Is.Not.EqualTo(original));
            await _bootstrap.ShutdownAsync();
            Assert.That(original.isLoaded, Is.True);
            Assert.That(_installer.ReleaseCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator NativeSingleEntryRetainsOnlyPersistentCommonOwner() => UniTask.ToCoroutine(async () =>
        {
            var original = SceneManager.GetActiveScene();
            CreateBootstrap(LoadSceneMode.Single, true);
            await _bootstrap.BootstrapAsync();
            Assert.That(original.isLoaded, Is.False, "This must execute native Single, not simulate it with Additive.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_bootstrap.GameScene));
            Assert.That(_root.IsPrepared, Is.True);
            Assert.That(_bootstrap.Manager.CanProceed, Is.True);
            Assert.That(SingletonSceneRoot.Instance, Is.SameAs(_root));
            Assert.That(_installer.InstallCount, Is.EqualTo(1));
            Assert.That(_installer.PrepareCount, Is.EqualTo(1));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_installer.ReleaseCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator SingleShutdownReportsLastSceneWithoutCreatingFallback() => UniTask.ToCoroutine(async () =>
        {
            CreateBootstrap(LoadSceneMode.Single);
            await _bootstrap.BootstrapAsync();
            var scene = _bootstrap.GameScene;
            Exception failure = null;
            try
            {
                await _bootstrap.ShutdownAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("last normal scene"));
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_bootstrap.Manager.LoadedScene, Is.EqualTo(scene));
            Assert.That(scene.isLoaded, Is.True);
            Assert.That(_bootstrap.Manager.IsGamePrepared, Is.False);
            Assert.That(_bootstrap.Manager.GameScene.IsValid(), Is.False);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator CancellationDuringNativeSingleOwnsLateLoadedScene() => UniTask.ToCoroutine(async () =>
        {
            CreateBootstrap(LoadSceneMode.Single);
            var entry = _bootstrap.BootstrapAsync();
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Loading));
            _bootstrap.Manager.CancelTransition();
            Exception failure = null;
            try
            {
                await entry;
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)failure).Flatten().InnerExceptions.Any(e => e is OperationCanceledException), Is.True);
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.Loading));
            Assert.That(_bootstrap.Manager.LoadedScene.isLoaded, Is.True);
            Assert.That(_bootstrap.Manager.IsGamePrepared, Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator SingleRejectsUnknownLoadedScenesBeforeCover() => UniTask.ToCoroutine(async () =>
        {
            CreateBootstrap(LoadSceneMode.Single);
            var foreign = SceneManager.CreateScene("UnregisteredEntryTest");
            try
            {
                Assert.Throws<InvalidOperationException>(() => _bootstrap.BootstrapAsync());
                Assert.That(_bootstrap.Manager, Is.Null);
                Assert.That(_callbacks.CoverCount, Is.Zero);
                Assert.That(_installer.PrepareCount, Is.Zero);
                Assert.That(foreign.isLoaded, Is.True);
            }
            finally
            {
                await SceneManager.UnloadSceneAsync(foreign).ToUniTask();
            }
        });
    }
}
