using System;
using System.Collections;
using System.Text.RegularExpressions;
using TPLab.Core.Lifecycle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class SceneRootTests
    {
        [SetUp]
        public void SetUp() => SceneRootInstallerProbe.Trace.Clear();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var instance in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (instance.scene.IsValid() && instance.name.StartsWith("SceneRootTest", StringComparison.Ordinal))
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
            yield return null;
            Assert.That(SingletonSceneRoot.Instance, Is.Null);
        }

        private static GameObject CreateRoot() => new GameObject("SceneRootTestOwner");

        private static SceneRootInstallerProbe AddInstaller(GameObject owner, string id)
        {
            var installer = owner.AddComponent<SceneRootInstallerProbe>();
            installer.Id = id;
            return installer;
        }

        [UnityTest]
        public IEnumerator ScriptSelectionInstallsInOrderAndCleansInReverseOrder()
        {
            var owner = CreateRoot();
            owner.SetActive(false);
            var first = AddInstaller(owner, "first");
            var second = AddInstaller(owner, "second");
            var source = new SceneRootInstaller[] { first, second };
            var host = SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned, source);
            source[0] = null;
            Assert.That(host.IsReady, Is.False);
            owner.SetActive(true);
            Assert.That(host.IsReady, Is.True);
            Assert.That(first.InjectedRoot, Is.SameAs(host));
            Assert.That(first.WasReadyDuringInstall, Is.False);
            Assert.That(host.RootObject, Is.SameAs(owner));
            owner.SetActive(false);
            owner.SetActive(true);
            Assert.That(first.InstallCount, Is.EqualTo(1));
            UnityEngine.Object.Destroy(owner);
            yield return null;
            CollectionAssert.AreEqual(new[] { "install:first", "install:second", "uninstall:second", "uninstall:first" }, SceneRootInstallerProbe.Trace);
            Assert.That(first.UninstallCount, Is.EqualTo(1));
            Assert.That(second.UninstallCount, Is.EqualTo(1));
        }

        [Test]
        public void SceneOwnedRootsHaveIndependentInstallations()
        {
            var first = CreateRoot();
            first.SetActive(false);
            var second = CreateRoot();
            second.SetActive(false);
            var firstInstaller = AddInstaller(first, "first");
            var secondInstaller = AddInstaller(second, "second");
            var firstHost = SceneRootSetup.Attach(first, SceneRootMode.SceneOwned, new[] { firstInstaller });
            var secondHost = SceneRootSetup.Attach(second, SceneRootMode.SceneOwned, new[] { secondInstaller });
            first.SetActive(true);
            second.SetActive(true);
            Assert.That(firstHost.IsReady && secondHost.IsReady, Is.True);
            Assert.That(firstInstaller.InjectedRoot, Is.SameAs(firstHost));
            Assert.That(secondInstaller.InjectedRoot, Is.SameAs(secondHost));
            Assert.That(SingletonSceneRoot.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator SingletonSelectionRejectsDuplicateWithoutInstallingOrDestroyingItsObject()
        {
            var first = CreateRoot();
            first.SetActive(false);
            var second = CreateRoot();
            second.SetActive(false);
            var installer = AddInstaller(first, "first");
            var duplicate = AddInstaller(second, "duplicate");
            var owner = SceneRootSetup.Attach(first, SceneRootMode.Singleton, new[] { installer });
            SceneRootSetup.Attach(second, SceneRootMode.Singleton, new[] { duplicate });
            first.SetActive(true);
            second.SetActive(true);
            yield return null;
            Assert.That(SingletonSceneRoot.Instance, Is.SameAs(owner));
            Assert.That(owner.IsReady, Is.True);
            Assert.That(duplicate.InstallCount, Is.Zero);
            Assert.That(second != null && duplicate != null, Is.True);
            Assert.That(second.GetComponent<SingletonSceneRoot>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator PartialFailureCleansFailingAndEarlierInstallersButSkipsLaterOnes()
        {
            var owner = CreateRoot();
            owner.SetActive(false);
            var first = AddInstaller(owner, "first");
            var failing = AddInstaller(owner, "failing");
            failing.FailInstall = true;
            var later = AddInstaller(owner, "later");
            var host = SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned, new[] { first, failing, later });
            LogAssert.Expect(LogType.Exception, new Regex("root-install:failing"));
            owner.SetActive(true);
            Assert.That(host.IsReady, Is.False);
            CollectionAssert.AreEqual(new[] { "install:first", "install:failing", "uninstall:failing", "uninstall:first" }, SceneRootInstallerProbe.Trace);
            Assert.That(first.InjectedRoot, Is.Null);
            Assert.That(later.InstallCount, Is.Zero);
            UnityEngine.Object.Destroy(owner);
            yield return null;
            Assert.That(first.UninstallCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CleanupFailureStillAttemptsEveryInstaller()
        {
            var owner = CreateRoot();
            owner.SetActive(false);
            var first = AddInstaller(owner, "first");
            var second = AddInstaller(owner, "second");
            second.FailUninstall = true;
            SceneRootSetup.Attach(owner, SceneRootMode.Singleton, new[] { first, second });
            owner.SetActive(true);
            LogAssert.Expect(LogType.Exception, new Regex("root-uninstall:second"));
            UnityEngine.Object.Destroy(owner);
            yield return null;
            Assert.That(first.UninstallCount, Is.EqualTo(1));
            Assert.That(second.UninstallCount, Is.EqualTo(1));
            Assert.That(SingletonSceneRoot.Instance, Is.Null);
        }

        [Test]
        public void RuntimeSetupRequiresInactiveObjectWithoutDisablingExistingComponents()
        {
            var owner = CreateRoot();
            Assert.Throws<InvalidOperationException>(() => SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned));
            Assert.That(owner.activeSelf, Is.True);
            Assert.That(owner.GetComponent<SceneOwnedRoot>(), Is.Null);
        }

        [Test]
        public void ConfigurationCannotChangeAfterActivation()
        {
            var owner = CreateRoot();
            owner.SetActive(false);
            var host = (SceneOwnedRoot)SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned);
            owner.SetActive(true);
            Assert.Throws<InvalidOperationException>(() => host.Configure(Array.Empty<SceneRootInstaller>()));
        }

        [Test]
        public void SetupRejectsMixedHostsAndInvalidInstallerOwnershipBeforeMutation()
        {
            var owner = CreateRoot();
            owner.SetActive(false);
            var foreign = CreateRoot();
            foreign.SetActive(false);
            var installer = AddInstaller(foreign, "foreign");
            Assert.Throws<ArgumentException>(() => SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned, new[] { installer }));
            Assert.That(owner.GetComponent<SceneOwnedRoot>(), Is.Null);
            SceneRootSetup.Attach(owner, SceneRootMode.SceneOwned);
            Assert.Throws<InvalidOperationException>(() => SceneRootSetup.Attach(owner, SceneRootMode.Singleton));
            Assert.That(owner.GetComponent<SingletonSceneRoot>(), Is.Null);
        }

        [Test]
        public void SetupRejectsNullChildUnknownModeAndRepeatedInstaller()
        {
            Assert.Throws<ArgumentNullException>(() => SceneRootSetup.Attach(null, SceneRootMode.SceneOwned));
            var parent = CreateRoot();
            parent.SetActive(false);
            var child = CreateRoot();
            child.transform.SetParent(parent.transform);
            Assert.Throws<ArgumentException>(() => SceneRootSetup.Attach(child, SceneRootMode.SceneOwned));
            Assert.Throws<ArgumentOutOfRangeException>(() => SceneRootSetup.Attach(parent, (SceneRootMode)99));
            var installer = AddInstaller(parent, "first");
            Assert.Throws<ArgumentException>(() => SceneRootSetup.Attach(parent, SceneRootMode.SceneOwned, new[] { installer, installer }));
            Assert.That(parent.GetComponent<SceneOwnedRoot>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator SceneUnloadCleansSceneOwnedRootWhilePersistentSingletonSurvives()
        {
            var scene = SceneManager.CreateScene("SceneRootTestScene");
            var ownedObject = CreateRoot();
            ownedObject.SetActive(false);
            var globalObject = CreateRoot();
            globalObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(ownedObject, scene);
            SceneManager.MoveGameObjectToScene(globalObject, scene);
            var ownedInstaller = AddInstaller(ownedObject, "owned");
            var globalInstaller = AddInstaller(globalObject, "global");
            SceneRootSetup.Attach(ownedObject, SceneRootMode.SceneOwned, new[] { ownedInstaller });
            var global = SceneRootSetup.Attach(globalObject, SceneRootMode.Singleton, new[] { globalInstaller }, true);
            ownedObject.SetActive(true);
            globalObject.SetActive(true);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(ownedInstaller.UninstallCount, Is.EqualTo(1));
            Assert.That(global.IsReady, Is.True);
            Assert.That(globalInstaller.UninstallCount, Is.Zero);
            Assert.That(global.RootObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }
    }
}
