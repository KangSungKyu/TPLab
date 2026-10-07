using System;
using System.Collections;
using System.Collections.Generic;
using TPLab.Core.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class PrefabPoolTests
    {
        private GameObject _source;
        private GameObject _parent;
        private readonly List<PrefabPool> _pools = new List<PrefabPool>();

        [SetUp]
        public void SetUp()
        {
            _source = new GameObject("PoolTestSource");
            _source.SetActive(false);
            _source.transform.localPosition = new Vector3(1, 2, 3);
            _source.transform.localRotation = Quaternion.Euler(10, 20, 30);
            _source.transform.localScale = new Vector3(2, 3, 4);
            _parent = new GameObject("PoolTestParent");
            PoolLifecycleProbe.EnableCount = 0;
            PoolLifecycleProbe.OnEnabled = null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PoolLifecycleProbe.OnEnabled = null;
            foreach (var pool in _pools)
            {
                pool.Dispose();
            }
            _pools.Clear();
            UnityEngine.Object.Destroy(_source);
            UnityEngine.Object.Destroy(_parent);
            yield return null;
            foreach (var instance in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                Assert.That(instance.name != "PrefabPool" || !instance.scene.IsValid(), Is.True,
                    "An inactive pool storage root leaked from a test.");
            }
        }

        private PrefabPool CreatePool(int capacity = 1, Action<GameObject> onRent = null,
            Action<GameObject> onReturn = null)
        {
            var pool = new PrefabPool(_source, capacity, _parent.transform, onRent, onReturn);
            _pools.Add(pool);
            return pool;
        }

        [Test]
        public void ReturnedInstanceIsReusedWithSourceTransform()
        {
            var pool = CreatePool();
            Assert.That(pool.TryRent(out var first), Is.True);
            Assert.That(first.activeInHierarchy, Is.True);
            Assert.That(first.transform.parent, Is.EqualTo(_parent.transform));
            first.transform.SetParent(null);
            first.transform.localPosition = Vector3.zero;
            first.transform.localRotation = Quaternion.identity;
            first.transform.localScale = Vector3.one;
            pool.Return(first);
            Assert.That(first.activeSelf, Is.False);
            Assert.That(pool.CountRented, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(pool.TryRent(out var second), Is.True);
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.transform.parent, Is.EqualTo(_parent.transform));
            Assert.That(second.transform.localPosition, Is.EqualTo(_source.transform.localPosition));
            Assert.That(Quaternion.Angle(second.transform.localRotation, _source.transform.localRotation), Is.LessThan(0.001f));
            Assert.That(second.transform.localScale, Is.EqualTo(_source.transform.localScale));
            Assert.That(pool.CountOwned, Is.EqualTo(1));
            Assert.That(pool.CountRented, Is.EqualTo(1));
        }

        [Test]
        public void CapacityIncludesRentedInstances()
        {
            var pool = CreatePool(2);
            Assert.That(pool.TryRent(out var first), Is.True);
            Assert.That(pool.TryRent(out var second), Is.True);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(pool.TryRent(out var exhausted), Is.False);
            Assert.That(exhausted, Is.Null);
            Assert.That(pool.CountOwned, Is.EqualTo(2));
            pool.Return(first);
            Assert.That(pool.TryRent(out var reused), Is.True);
            Assert.That(reused, Is.SameAs(first));
            Assert.That(pool.CountOwned, Is.EqualTo(2));
        }

        [Test]
        public void ReturnRejectsAnotherPoolsInstanceWithoutChangingIt()
        {
            var owner = CreatePool();
            var other = CreatePool();
            Assert.That(owner.TryRent(out var instance), Is.True);
            Assert.Throws<ArgumentException>(() => other.Return(instance));
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(owner.CountRented, Is.EqualTo(1));
            Assert.That(other.CountOwned, Is.Zero);
            owner.Return(instance);
        }

        [Test]
        public void ReturnRejectsDuplicateWithoutAddingAnotherSlot()
        {
            var pool = CreatePool();
            Assert.That(pool.TryRent(out var instance), Is.True);
            pool.Return(instance);
            Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(pool.TryRent(out var reused), Is.True);
            Assert.That(reused, Is.SameAs(instance));
            Assert.That(pool.TryRent(out _), Is.False);
        }

        [Test]
        public void ReturnRejectsNull()
        {
            var pool = CreatePool();
            Assert.Throws<ArgumentNullException>(() => pool.Return(null));
        }

        [UnityTest]
        public IEnumerator RentHookFailureDiscardsInstanceAndAllowsRetry()
        {
            bool fail = true;
            GameObject failed = null;
            var pool = CreatePool(onRent: instance =>
            {
                if (fail)
                {
                    failed = instance;
                    throw new InvalidOperationException("Expected rent failure");
                }
            });
            Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountRented, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(failed.activeSelf, Is.False);
            yield return null;
            Assert.That(failed == null, Is.True);
            fail = false;
            Assert.That(pool.TryRent(out _), Is.True);
        }

        [UnityTest]
        public IEnumerator ReturnHookFailureDiscardsInstanceAndAllowsRetry()
        {
            var pool = CreatePool(onReturn: _ => throw new InvalidOperationException("Expected return failure"));
            Assert.That(pool.TryRent(out var instance), Is.True);
            Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(pool.CountRented, Is.Zero);
            Assert.That(instance.activeSelf, Is.False);
            yield return null;
            Assert.That(instance == null, Is.True);
            Assert.That(pool.TryRent(out _), Is.True);
        }

        [Test]
        public void ReturnHookResetsConsumerStateBeforeReuse()
        {
            int consumerState = 0;
            int returns = 0;
            var pool = CreatePool(onReturn: instance =>
            {
                Assert.That(instance.activeSelf, Is.False);
                consumerState = 0;
                returns++;
            });
            Assert.That(pool.TryRent(out var first), Is.True);
            consumerState = 42;
            pool.Return(first);
            Assert.That(pool.TryRent(out _), Is.True);
            Assert.That(consumerState, Is.Zero);
            Assert.That(returns, Is.EqualTo(1));
        }

        [Test]
        public void MutatingReentryFromRentHookIsRejected()
        {
            PrefabPool pool = null;
            pool = CreatePool(onRent: instance =>
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
                Assert.Throws<InvalidOperationException>(() => pool.Dispose());
            });
            Assert.That(pool.TryRent(out _), Is.True);
            Assert.That(pool.IsDisposed, Is.False);
            Assert.That(pool.CountRented, Is.EqualTo(1));
        }

        [Test]
        public void MutatingReentryFromReturnHookIsRejected()
        {
            PrefabPool pool = null;
            pool = CreatePool(onReturn: instance =>
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
                Assert.Throws<InvalidOperationException>(() => pool.Dispose());
            });
            Assert.That(pool.TryRent(out var instance), Is.True);
            pool.Return(instance);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisposeDestroysRentedAndInactiveInstancesAndIsIdempotent()
        {
            var pool = CreatePool(2);
            Assert.That(pool.TryRent(out var rented), Is.True);
            Assert.That(pool.TryRent(out var idle), Is.True);
            pool.Return(idle);
            // Rented instances remain pool-owned even after a consumer reparents them.
            rented.transform.SetParent(null);
            pool.Dispose();
            pool.Dispose();
            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountRented, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(rented.activeSelf, Is.False);
            yield return null;
            Assert.That(rented == null, Is.True);
            Assert.That(idle == null, Is.True);
            Assert.That(_source != null, Is.True);
        }

        [Test]
        public void DisposedPoolRejectsRentAndReturn()
        {
            var pool = CreatePool();
            Assert.That(pool.TryRent(out var instance), Is.True);
            pool.Dispose();
            Assert.Throws<ObjectDisposedException>(() => pool.TryRent(out _));
            Assert.Throws<ObjectDisposedException>(() => pool.Return(instance));
        }

        [UnityTest]
        public IEnumerator DestroyedParentRejectsOperationsButStillAllowsDisposal()
        {
            var pool = CreatePool();
            Assert.That(pool.TryRent(out var instance), Is.True);
            instance.transform.SetParent(null);
            UnityEngine.Object.Destroy(_parent);
            yield return null;
            Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
            Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
            pool.Dispose();
            yield return null;
            Assert.That(instance == null, Is.True);
        }

        [Test]
        public void ActiveSourceIsConfiguredBeforeCloneOnEnable()
        {
            _source.AddComponent<PoolLifecycleProbe>();
            _source.SetActive(true);
            PoolLifecycleProbe.EnableCount = 0;
            bool configured = false;
            var pool = CreatePool(onRent: instance =>
            {
                Assert.That(instance.activeSelf, Is.False);
                Assert.That(PoolLifecycleProbe.EnableCount, Is.Zero);
                configured = true;
            });
            PoolLifecycleProbe.OnEnabled = () => Assert.That(configured, Is.True);
            Assert.That(pool.TryRent(out _), Is.True);
            Assert.That(PoolLifecycleProbe.EnableCount, Is.EqualTo(1));
            Assert.That(_source.activeSelf, Is.True);
        }

        [Test]
        public void OnEnableCannotReenterThePool()
        {
            _source.AddComponent<PoolLifecycleProbe>();
            var pool = CreatePool();
            PoolLifecycleProbe.OnEnabled = () =>
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.Throws<InvalidOperationException>(() => pool.Dispose());
            };
            Assert.That(pool.TryRent(out _), Is.True);
            Assert.That(PoolLifecycleProbe.EnableCount, Is.EqualTo(1));
        }

        [Test]
        public void RentHookDestroyingInstanceIsReportedAndCapacityRecovered()
        {
            var pool = CreatePool(onRent: instance => UnityEngine.Object.DestroyImmediate(instance));
            Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountRented, Is.Zero);
        }

        [Test]
        public void ReturnHookDestroyingInstanceIsReportedAndCapacityRecovered()
        {
            var pool = CreatePool(onReturn: instance => UnityEngine.Object.DestroyImmediate(instance));
            Assert.That(pool.TryRent(out var instance), Is.True);
            Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
        }
    }
}
