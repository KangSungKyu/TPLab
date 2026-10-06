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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyLab.Core.Tests
{
    [PrebuildSetup(typeof(BootstrapSceneTestSetup))]
    [PostBuildCleanup(typeof(BootstrapSceneTestSetup))]
    public sealed class BootstrapPlayModeTests
    {
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private GameObject _host;
        private SceneOwnedRoot _root;
        private BootstrapSystem _bootstrap;
        private BootstrapCallbacksProbe _callbacks;
        private SceneRootInstallerProbe _installer;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("BootstrapPlayTest");
            _host.SetActive(false);
            _root = _host.AddComponent<SceneOwnedRoot>();
            _installer = _host.AddComponent<SceneRootInstallerProbe>();
            _installer.Id = "bootstrap";
            _root.Configure(new SceneRootInstaller[] { _installer });
            _callbacks = _host.AddComponent<BootstrapCallbacksProbe>();
            _bootstrap = _host.AddComponent<BootstrapSystem>();
            _bootstrap.Configure(_root, Hub, false, _callbacks);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                try
                {
                    await _bootstrap.ShutdownAsync();
                }
                catch (Exception)
                {
                }
                await _root.ShutdownAsync();
            }
            finally
            {
                UnityEngine.Object.Destroy(_host);
            }
            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator MovedGameRootIsRejectedAndItsOwnedServicesAreReleased() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            callbacks.Presenting = () => SceneManager.MoveGameObjectToScene(callbacks.PresentationRoot.RootObject, _host.scene);
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            try
            {
                Exception failure = null;
                try
                {
                    await _bootstrap.BootstrapAsync();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                Assert.That(callbacks.RevealCount, Is.Zero);
                Assert.That(callbacks.PresentationRoot.IsReady, Is.False);
                Assert.That(callbacks.PresentationRoot.RootObject.GetComponent<SceneRootInstallerProbe>().ReleaseCount, Is.EqualTo(1));
                Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
                Assert.That(_root.IsPrepared, Is.True);
            }
            finally
            {
                if (callbacks.PresentationRoot != null && callbacks.PresentationRoot.RootObject != null)
                    UnityEngine.Object.Destroy(callbacks.PresentationRoot.RootObject);
            }
        });

        [UnityTest]
        public IEnumerator GameReleaseCannotSynchronouslyWaitForItsManagerShutdown() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            await _bootstrap.BootstrapAsync();
            var installer = callbacks.PresentationRoot.RootObject.GetComponent<SceneRootInstallerProbe>();
            installer.Releasing = () => Assert.Throws<InvalidOperationException>(() => _bootstrap.ShutdownAsync());
            await _bootstrap.ShutdownAsync();
            Assert.That(installer.ReleaseCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator CommonInstallerReentryIsRejectedAfterAsynchronousCover() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            callbacks.CoverGate = new UniTaskCompletionSource();
            _installer.Preparing = () => Assert.Throws<InvalidOperationException>(() => _bootstrap.BootstrapAsync());
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            var entry = _bootstrap.BootstrapAsync();
            callbacks.CoverGate.TrySetResult();
            await entry;
            Assert.That(_bootstrap.Manager.CanProceed, Is.True);
        });

        [UnityTest]
        public IEnumerator ExternalActiveSceneChangeBeforeRevealRejectsProgress() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            callbacks.Presenting = () => SceneManager.SetActiveScene(_host.scene);
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("active scene changed"));
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.PreparingPresentation));
            Assert.That(callbacks.RevealCount, Is.Zero);
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator SuccessfulEntryStopsPermissionWhenForeignScenesAppear() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            await _bootstrap.BootstrapAsync();
            var foreign = SceneManager.CreateScene("PostEntryForeignTest");
            try
            {
                Assert.That(_bootstrap.Manager.CanProceed, Is.False);
                await _bootstrap.ShutdownAsync();
                Assert.That(foreign.isLoaded, Is.True);
            }
            finally
            {
                await SceneManager.UnloadSceneAsync(foreign).ToUniTask();
            }
        });

        [UnityTest]
        public IEnumerator OwnerCancellationKeepsFailurePhaseAndCommonServices() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            callbacks.PresentationGate = new UniTaskCompletionSource();
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            var entry = _bootstrap.BootstrapAsync();
            await UniTask.WaitUntil(() => callbacks.PresentationRoot != null).Timeout(TimeSpan.FromSeconds(10));
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.PreparingPresentation));
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
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.PreparingPresentation));
            Assert.That(_bootstrap.Manager.LastFailure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_bootstrap.Manager.LoadedScene.IsValid(), Is.False);
            Assert.That(callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(callbacks.RevealCount, Is.Zero);
            Assert.That(_root.IsPrepared, Is.True);
            Assert.Throws<InvalidOperationException>(() => _bootstrap.Manager.EnterFirstSceneAsync(Hub));
            await _bootstrap.ShutdownAsync();
            Assert.That(callbacks.FailureCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator CommonInstallerCannotSynchronouslyAwaitItsOwnEntry() => UniTask.ToCoroutine(async () =>
        {
            _installer.Preparing = () => _bootstrap.BootstrapAsync().Forget();
            _host.SetActive(true);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("own transition"));
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.PreparingCommon));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator HookReentryAndFailureCallbackExceptionsRemainVisible() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            callbacks.Configuring = () => _bootstrap.ShutdownAsync().Forget();
            callbacks.FailFailureCallback = true;
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)failure).Flatten().InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.Configuring));
            Assert.That(callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(callbacks.RevealCount, Is.Zero);
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator ExternalSceneChangesDuringRevealRestoreCoverAndKeepForeignScene() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            Scene foreign = default;
            callbacks.Revealing = () => foreign = SceneManager.CreateScene("ForeignSceneTest");
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            try
            {
                Exception failure = null;
                try
                {
                    await _bootstrap.BootstrapAsync();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.Revealing));
                Assert.That(callbacks.CoverCount, Is.EqualTo(2));
                Assert.That(foreign.isLoaded, Is.True);
                Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
                Assert.That(_bootstrap.Manager.CanProceed, Is.False);
            }
            finally
            {
                if (foreign.IsValid() && foreign.isLoaded) await SceneManager.UnloadSceneAsync(foreign).ToUniTask();
            }
        });

        [UnityTest]
        public IEnumerator ShutdownFailureIsReportedOnceAfterGameCleanup() => UniTask.ToCoroutine(async () =>
        {
            var callbacks = _host.AddComponent<SceneTransitionCallbacksProbe>();
            _bootstrap.Configure(_root, Hub, false, callbacks);
            _host.SetActive(true);
            await _bootstrap.BootstrapAsync();
            var installer = callbacks.PresentationRoot.RootObject.GetComponent<SceneRootInstallerProbe>();
            installer.FailRelease = true;
            var first = _bootstrap.ShutdownAsync();
            var second = _bootstrap.ShutdownAsync();
            foreach (var task in new[] { first, second })
            {
                Exception failure = null;
                try
                {
                    await task;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                Assert.That(failure, Is.TypeOf<AggregateException>());
            }
            Assert.That(installer == null, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Faulted));
            Assert.That(_bootstrap.Manager.FailurePhase, Is.EqualTo(SceneTransitionState.Stopping));
            Assert.That(callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator BootstrapPublishesTheOnlySceneManager() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            await _bootstrap.BootstrapAsync();
            Assert.That(_bootstrap.Manager, Is.Not.Null);
            Assert.That(_bootstrap.Manager.GameScene, Is.EqualTo(_bootstrap.GameScene));
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Ready));
            Assert.That(_bootstrap.Manager.CanProceed, Is.True);
            Assert.Throws<InvalidOperationException>(() => _bootstrap.Manager.EnterFirstSceneAsync(Hub));
            await _bootstrap.ShutdownAsync();
            Assert.That(_bootstrap.Manager.State, Is.EqualTo(SceneTransitionState.Stopped));
            Assert.That(_bootstrap.Manager.CanProceed, Is.False);
        });

        [UnityTest]
        public IEnumerator SharedCallbacksWaitForPreparedPresentationBeforeReveal() => UniTask.ToCoroutine(async () =>
        {
            var presentation = _host.AddComponent<SceneTransitionCallbacksProbe>();
            presentation.PresentationGate = new UniTaskCompletionSource();
            _bootstrap.Configure(_root, Hub, false, presentation);
            _host.SetActive(true);
            var entry = _bootstrap.BootstrapAsync();
            await UniTask.WaitUntil(() => presentation.PresentationRoot != null).Timeout(TimeSpan.FromSeconds(10));
            Assert.That(presentation.ConfigurationWasBeforePreparation, Is.True);
            Assert.That(presentation.PresentationReceivedPreparedRoot, Is.True);
            Assert.That(presentation.CoverCount, Is.EqualTo(1));
            Assert.That(presentation.RevealCount, Is.Zero);
            Assert.That(_bootstrap.GameScene.IsValid(), Is.False);
            presentation.PresentationGate.TrySetResult();
            await entry;
            Assert.That(presentation.RevealCount, Is.EqualTo(1));
            Assert.That(_bootstrap.GameScene, Is.EqualTo(presentation.PresentationRoot.RootObject.scene));
            await _bootstrap.ShutdownAsync();
            Assert.That(presentation.CoverCount, Is.EqualTo(2));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator BothRootsMustPrepareBeforeRevealAndShutdownKeepsSharedRoot() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            _installer.PrepareGate = new UniTaskCompletionSource();
            _callbacks.GamePreparation = new UniTaskCompletionSource();
            var entry = _bootstrap.BootstrapAsync();
            var shared = _bootstrap.BootstrapAsync();
            await UniTask.Yield();
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            _installer.PrepareGate.TrySetResult();
            await UniTask.WaitUntil(() => _callbacks.GameInstaller != null).Timeout(TimeSpan.FromSeconds(10));
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Hub));
            _callbacks.GamePreparation.TrySetResult();
            await entry;
            await shared;
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_installer.InstallCount, Is.EqualTo(1));
            Assert.That(_installer.PrepareCount, Is.EqualTo(1));
            Assert.That(_callbacks.GameInstaller.PrepareCount, Is.EqualTo(1));
            Assert.That(_bootstrap.GameScene.isLoaded, Is.True);
            await _bootstrap.ShutdownAsync();
            Assert.That(_callbacks.CoverCount, Is.EqualTo(2));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
            Assert.That(_installer.ReleaseCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator ConfigurationFailureKeepsCoverAndUnloadsCandidate() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            _callbacks.FailConfigure = true;
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("bootstrap-configure"));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_bootstrap.GameScene.IsValid(), Is.False);
        });

        [UnityTest]
        public IEnumerator CallerCancellationDoesNotCancelSharedEntry() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            _installer.PrepareGate = new UniTaskCompletionSource();
            using (var cancellation = new CancellationTokenSource())
            {
                var cancelledWait = _bootstrap.BootstrapAsync(cancellation.Token);
                cancellation.Cancel();
                bool cancelled = false;
                try
                {
                    await cancelledWait;
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                Assert.That(cancelled, Is.True);
                _installer.PrepareGate.TrySetResult();
                await _bootstrap.BootstrapAsync();
                Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            }
        });

        [UnityTest]
        public IEnumerator OwnerShutdownCancelsGamePreparationAndCleansItsScene() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            _callbacks.GamePreparation = new UniTaskCompletionSource();
            var entry = _bootstrap.BootstrapAsync();
            await UniTask.WaitUntil(() => _callbacks.GameInstaller != null).Timeout(TimeSpan.FromSeconds(10));
            await _bootstrap.ShutdownAsync();
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
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator AutoStartRunsExactlyOnce() => UniTask.ToCoroutine(async () =>
        {
            _bootstrap.Configure(_root, Hub, true, _callbacks);
            _host.SetActive(true);
            await UniTask.WaitUntil(() => _bootstrap.GameScene.isLoaded).Timeout(TimeSpan.FromSeconds(10));
            await _bootstrap.BootstrapAsync();
            Assert.That(_callbacks.RevealCount, Is.EqualTo(1));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator MissingBuildDestinationFailsBeforePreparation() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            _bootstrap.Configure(_root, "Assets/NotIncluded.unity", false, _callbacks);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(_installer.PrepareCount, Is.Zero);
            Assert.That(_callbacks.CoverCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator ShutdownDuringNativeLoadWaitsForLateCompletionAndUnloads() => UniTask.ToCoroutine(async () =>
        {
            _host.SetActive(true);
            var entry = _bootstrap.BootstrapAsync();
            await _bootstrap.ShutdownAsync();
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
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => _bootstrap.BootstrapAsync());
        });

        [UnityTest]
        public IEnumerator RevealFailureRestoresCoverAndUnloadsPreparedGame() => UniTask.ToCoroutine(async () =>
        {
            _callbacks.FailReveal = true;
            _host.SetActive(true);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("bootstrap-reveal"));
            Assert.That(_callbacks.CoverCount, Is.EqualTo(2));
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });

        [UnityTest]
        public IEnumerator GamePreparationFailureNeverRevealsOrShutsDownBorrowedSharedRoot() => UniTask.ToCoroutine(async () =>
        {
            _callbacks.FailGamePrepare = true;
            _host.SetActive(true);
            Exception failure = null;
            try
            {
                await _bootstrap.BootstrapAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(_callbacks.RevealCount, Is.Zero);
            Assert.That(_callbacks.FailureCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetSceneByPath(Hub).isLoaded, Is.False);
            Assert.That(_root.IsPrepared, Is.True);
        });
    }

    public sealed class BootstrapSceneTestSetup : IPrebuildSetup, IPostBuildCleanup
    {
#if UNITY_EDITOR
        private const string Key = "MyLab.BootstrapTest.BuildScenes";
        [Serializable] private sealed class Entry { public string Path; public string Guid; public bool Enabled; }
        [Serializable] private sealed class Snapshot { public Entry[] Scenes; }
#endif
        public void Setup()
        {
#if UNITY_EDITOR
            const string hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
            const string replacement = "Assets/MyLab/Tests/Fixtures/ReplacementMain.unity";
            if (!string.IsNullOrEmpty(SessionState.GetString(Key, "")))
                throw new InvalidOperationException("Restore the previous Bootstrap test build scene snapshot before running again.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(hub) == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(replacement) == null)
                throw new InvalidOperationException("Bootstrap/Replacement test fixture is missing.");
            var original = EditorBuildSettings.scenes;
            SessionState.SetString(Key + ".Bytes", Convert.ToBase64String(System.IO.File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset")));
            SessionState.SetString(Key, JsonUtility.ToJson(new Snapshot
            {
                Scenes = original.Select(scene => new Entry { Path = scene.path, Guid = scene.guid.ToString(), Enabled = scene.enabled }).ToArray()
            }));
            EditorBuildSettings.scenes = original.Where(scene => scene.path != hub && scene.path != replacement)
                .Concat(new[] { new EditorBuildSettingsScene(hub, true), new EditorBuildSettingsScene(replacement, true) }).ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            string json = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return;
            EditorBuildSettings.scenes = JsonUtility.FromJson<Snapshot>(json).Scenes
                .Select(scene => new EditorBuildSettingsScene(scene.Path, scene.Enabled) { guid = new GUID(scene.Guid) }).ToArray();
            // Unity recalculates missing-scene GUIDs when assigning the list; retain the original serialized bytes too.
            System.IO.File.WriteAllBytes("ProjectSettings/EditorBuildSettings.asset", Convert.FromBase64String(SessionState.GetString(Key + ".Bytes", "")));
            SessionState.EraseString(Key);
            SessionState.EraseString(Key + ".Bytes");
#endif
        }
    }
}
