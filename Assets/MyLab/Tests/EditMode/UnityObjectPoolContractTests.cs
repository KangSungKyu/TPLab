using System;
using NUnit.Framework;

namespace MyLab.Core.Tests
{
    public class UnityObjectPoolContractTests
    {
        private sealed class Data
        {
            public bool Destroyed;
        }

        [Test]
        public void NormalReturnReusesPlainClass()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data()))
            {
                var first = pool.Get();
                pool.Release(first);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.Get(), Is.SameAs(first));
            }
        }

        [Test]
        public void ThrowingReturnCallbackLeavesInstanceOutsideInactiveStorage()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data(),
                actionOnRelease: _ => throw new InvalidOperationException("release")))
            {
                var first = pool.Get();
                Assert.Throws<InvalidOperationException>(() => pool.Release(first));
                Assert.That(pool.CountInactive, Is.Zero);
                Assert.That(pool.CountActive, Is.EqualTo(1));
                Assert.That(pool.Get(), Is.Not.SameAs(first));
            }
        }

        [Test]
        public void ThrowingRentCallbackDoesNotRollBackTheCreatedSlot()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data(),
                actionOnGet: _ => throw new InvalidOperationException("get")))
            {
                Assert.Throws<InvalidOperationException>(() => pool.Get());
                Assert.That(pool.CountAll, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.Zero);
            }
        }

        [Test]
        public void DisabledCollectionCheckAllowsTwoLoansOfOneDuplicateReturn()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data(), collectionCheck: false, maxSize: 2))
            {
                var instance = pool.Get();
                pool.Release(instance);
                pool.Release(instance);
                Assert.That(pool.Get(), Is.SameAs(instance));
                Assert.That(pool.Get(), Is.SameAs(instance));
            }
        }

        [Test]
        public void ForeignReturnIsNotAnOwnershipCheck()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data()))
            {
                var foreign = new Data();
                pool.Release(foreign);
                Assert.That(pool.CountAll, Is.Zero);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.Get(), Is.SameAs(foreign));
            }
        }

        [Test]
        public void ClearDestroysOnlyInactiveObjectsAndAllowsFurtherRent()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data(), actionOnDestroy: data => data.Destroyed = true))
            {
                var borrowed = pool.Get();
                var idle = pool.Get();
                pool.Release(idle);
                pool.Clear();
                Assert.That(idle.Destroyed, Is.True);
                Assert.That(borrowed.Destroyed, Is.False);
                Assert.That(pool.Get(), Is.Not.Null);
            }
        }

        [Test]
        public void FullInactiveStorageIntentionallyDestroysOverflowReturn()
        {
            using (var pool = new UnityEngine.Pool.ObjectPool<Data>(() => new Data(),
                actionOnDestroy: data => data.Destroyed = true, maxSize: 1))
            {
                var first = pool.Get();
                var overflow = pool.Get();
                pool.Release(first);
                pool.Release(overflow);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(overflow.Destroyed, Is.True);
            }
        }
    }
}
