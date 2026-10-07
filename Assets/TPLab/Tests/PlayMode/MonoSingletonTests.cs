using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class MonoSingletonTests
    {
        [SetUp]
        public void SetUp()
        {
            SingletonProbe.TotalInitializeCount = 0;
            SingletonProbe.TotalShutdownCount = 0;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var instance in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (instance.scene.IsValid() && instance.name.StartsWith("SingletonTest", StringComparison.Ordinal))
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(OtherSingletonProbe.Instance, Is.Null);
        }

        private static SingletonProbe CreateProbe(Action<SingletonProbe> configure = null, Transform parent = null)
        {
            var instance = new GameObject("SingletonTestOwner");
            instance.SetActive(false);
            instance.transform.SetParent(parent, false);
            var probe = instance.AddComponent<SingletonProbe>();
            configure?.Invoke(probe);
            instance.SetActive(true);
            return probe;
        }

        [Test]
        public void InstanceQueryDoesNotCreateAnObject()
        {
            int before = Resources.FindObjectsOfTypeAll<SingletonProbe>().Length;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(Resources.FindObjectsOfTypeAll<SingletonProbe>().Length, Is.EqualTo(before));
        }

        [Test]
        public void AwakePublishesOnlyAfterInitializationSucceeds()
        {
            var owner = CreateProbe();
            Assert.That(owner.InitializeCount, Is.EqualTo(1));
            Assert.That(owner.InstanceDuringInitialize, Is.Null);
            Assert.That(owner.IsInitialized, Is.True);
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
        }

        [UnityTest]
        public IEnumerator DuplicateRemovesOnlyItsComponentAndPreservesSiblingsAndChildren()
        {
            var owner = CreateProbe();
            var duplicate = CreateProbe();
            var duplicateObject = duplicate.gameObject;
            var sibling = duplicateObject.AddComponent<BoxCollider>();
            var child = new GameObject("SingletonTestDuplicateChild");
            child.transform.SetParent(duplicateObject.transform, false);
            yield return null;
            Assert.That(duplicate == null, Is.True);
            Assert.That(duplicateObject != null && sibling != null && child != null, Is.True);
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            Assert.That(SingletonProbe.TotalInitializeCount, Is.EqualTo(1));
            Assert.That(SingletonProbe.TotalShutdownCount, Is.Zero);
        }

        [Test]
        public void DifferentClosedTypesHaveIndependentOwners()
        {
            var owner = CreateProbe();
            var other = new GameObject("SingletonTestOther").AddComponent<OtherSingletonProbe>();
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            Assert.That(OtherSingletonProbe.Instance, Is.SameAs(other));
        }

        [Test]
        public void DisableAndEnablePreserveOwnershipWithoutReinitializing()
        {
            var owner = CreateProbe();
            owner.enabled = false;
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            owner.enabled = true;
            owner.gameObject.SetActive(false);
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            owner.gameObject.SetActive(true);
            Assert.That(owner.InitializeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyedOwnerUnregistersAndAllowsExplicitReplacement()
        {
            var owner = CreateProbe();
            UnityEngine.Object.Destroy(owner.gameObject);
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.EqualTo(1));
            var replacement = CreateProbe();
            Assert.That(SingletonProbe.Instance, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator InitializationFailureUnpublishesAndCleansPartialStateOnce()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: singleton-init"));
            var failed = CreateProbe(probe => probe.FailInitialize = true);
            var source = failed.gameObject;
            yield return null;
            Assert.That(failed == null, Is.True);
            Assert.That(source != null, Is.True);
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.EqualTo(1));
            var replacement = CreateProbe();
            Assert.That(SingletonProbe.Instance, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator ShutdownFailureStillClearsOwnershipAndAllowsReplacement()
        {
            var owner = CreateProbe(probe => probe.FailShutdown = true);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: singleton-shutdown"));
            UnityEngine.Object.Destroy(owner.gameObject);
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.EqualTo(1));
            var replacement = CreateProbe();
            Assert.That(SingletonProbe.Instance, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator InitializationAndCleanupFailuresAreBothReported()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: singleton-init"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: singleton-shutdown"));
            CreateProbe(probe => { probe.FailInitialize = true; probe.FailShutdown = true; });
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReentrantCreationCannotPublishASecondOwnerDuringInitialization()
        {
            var owner = CreateProbe(probe => probe.SpawnDuplicate = true);
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            Assert.That(owner.InstanceDuringInitialize, Is.Null);
            Assert.That(SingletonProbe.TotalInitializeCount, Is.EqualTo(1));
            Assert.That(SingletonProbe.TotalShutdownCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DestroyDuringInitializationCannotPublishADeadOwner()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Singleton was destroyed during initialization"));
            CreateProbe(probe => probe.DestroyDuringInitialize = true);
            yield return null;
            Assert.That(SingletonProbe.Instance, Is.Null);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SceneLifetimeOwnerIsDestroyedWhenItsSceneUnloads()
        {
            var scene = SceneManager.CreateScene("SingletonTestScene-" + Guid.NewGuid().ToString("N"));
            var owner = CreateProbe();
            SceneManager.MoveGameObjectToScene(owner.gameObject, scene);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(owner == null, Is.True);
            Assert.That(SingletonProbe.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator PersistentOwnerSurvivesSourceSceneUnload()
        {
            var scene = SceneManager.CreateScene("SingletonTestPersistentScene-" + Guid.NewGuid().ToString("N"));
            var instance = new GameObject("SingletonTestPersistentOwner");
            instance.SetActive(false);
            SceneManager.MoveGameObjectToScene(instance, scene);
            var owner = instance.AddComponent<SingletonProbe>();
            owner.Persistent = true;
            instance.SetActive(true);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(owner != null, Is.True);
            Assert.That(SingletonProbe.Instance, Is.SameAs(owner));
            Assert.That(owner.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator PersistentChildIsRejectedWithoutChangingTheParentHierarchy()
        {
            var parent = new GameObject("SingletonTestParent");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Persistent singletons require a root GameObject"));
            var child = CreateProbe(probe => probe.Persistent = true, parent.transform);
            var childObject = child.gameObject;
            yield return null;
            Assert.That(child == null, Is.True);
            Assert.That(childObject.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(SingletonProbe.TotalInitializeCount, Is.Zero);
            Assert.That(SingletonProbe.TotalShutdownCount, Is.Zero);
        }
    }
}
