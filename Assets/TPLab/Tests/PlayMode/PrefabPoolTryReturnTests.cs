using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TPLab.Core.Pooling;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class PrefabPoolTryReturnTests
    {
        private GameObject _source;
        private GameObject _parent;
        private readonly List<PrefabPool> _pools = new List<PrefabPool>();

        [SetUp]
        public void SetUp()
        {
            _source = new GameObject("TryReturnSource");
            _source.SetActive(false);
            _parent = new GameObject("TryReturnParent");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var pool in _pools)
            {
                pool.Dispose();
            }
            _pools.Clear();
            UnityEngine.Object.Destroy(_source);
            UnityEngine.Object.Destroy(_parent);
            yield return null;
        }

        private PrefabPool CreatePool(int capacity = 1, Action<GameObject> onRent = null,
            Action<GameObject> onReturn = null)
        {
            var pool = new PrefabPool(_source, capacity, _parent.transform, onRent, onReturn);
            _pools.Add(pool);
            return pool;
        }

        [Test]
        public void SuccessfulReturnDeactivatesResetsAndReusesLoan()
        {
            int resets = 0;
            var pool = CreatePool(onReturn: clone => resets++);
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            pool.TryRent(out var loan);
            Assert.That(tryReturn(loan), Is.True);
            Assert.That(loan.activeSelf, Is.False);
            Assert.That(resets, Is.EqualTo(1));
            Assert.That(pool.CountOwned, Is.EqualTo(1));
            Assert.That(pool.CountRented, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            pool.TryRent(out var reused);
            Assert.That(reused, Is.SameAs(loan));
        }

        [Test]
        public void ActiveInvalidReturnsKeepExistingValidation()
        {
            var pool = CreatePool();
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            pool.TryRent(out var loan);
            Assert.Throws<ArgumentNullException>(() => tryReturn(null));
            Assert.Throws<ArgumentException>(() => tryReturn(_source));
            Assert.That(_source != null, Is.True);
            Assert.That(tryReturn(loan), Is.True);
            Assert.Throws<InvalidOperationException>(() => tryReturn(loan));
            Assert.That(pool.CountOwned, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator LateDestroyedLoanReturnsFalseAfterDisposalWhileReturnStillThrows()
        {
            int resets = 0;
            var pool = CreatePool(onReturn: clone => resets++);
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            pool.TryRent(out var loan);
            pool.Dispose();
            yield return null;
            Assert.That(loan == null, Is.True);
            Assert.That(tryReturn(loan), Is.False);
            Assert.That(tryReturn(null), Is.False);
            Assert.That(tryReturn(_source), Is.False);
            Assert.That(resets, Is.Zero);
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => pool.Return(loan));
            Assert.That(_source != null && _parent != null, Is.True);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DisableAndDestroyCallbacksTryReturnWithoutInterruptingDisposal()
        {
            var pool = CreatePool(2);
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            var disabled = new List<bool>();
            var destroyed = new List<bool>();
            for (int i = 0; i < 2; i++)
            {
                pool.TryRent(out var loan);
                var probe = loan.AddComponent<PoolReturnLifecycleProbe>();
                probe.Disabled = clone => disabled.Add(tryReturn(clone));
                probe.Destroyed = clone => destroyed.Add(tryReturn(clone));
            }
            pool.Dispose();
            Assert.That(disabled, Is.EqualTo(new[] { false, false }));
            Assert.That(pool.CountOwned, Is.Zero);
            yield return null;
            Assert.That(destroyed, Is.EqualTo(new[] { false, false }));
            Assert.That(_source != null && _parent != null, Is.True);
        }

        [Test]
        public void RentCallbackReentryStillThrows()
        {
            Func<GameObject, bool> tryReturn = null;
            var pool = CreatePool(onRent: clone =>
                Assert.Throws<InvalidOperationException>(() => tryReturn(clone)));
            tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            Assert.That(pool.TryRent(out var loan), Is.True);
            Assert.That(pool.CountRented, Is.EqualTo(1));
            Assert.That(tryReturn(loan), Is.True);
        }

        [Test]
        public void ResetFailureStillThrowsAndDiscardsTheLoan()
        {
            var expected = new ApplicationException("try-return-prefab-reset");
            var pool = CreatePool(onReturn: clone => throw expected);
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            pool.TryRent(out var loan);
            var failure = Assert.Throws<ApplicationException>(() => tryReturn(loan));
            Assert.That(failure, Is.SameAs(expected));
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.TryRent(out _), Is.True);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DestroyedParentKeepsContextErrorBeforeInputValidation()
        {
            var pool = CreatePool();
            var tryReturn = TryReturnTestApi.Bind<GameObject>(pool);
            pool.TryRent(out var loan);
            UnityEngine.Object.Destroy(_parent);
            yield return null;
            Assert.Throws<InvalidOperationException>(() => tryReturn(null));
            Assert.Throws<InvalidOperationException>(() => tryReturn(loan));
            Assert.That(pool.CountOwned, Is.EqualTo(1));
        }
    }
}
