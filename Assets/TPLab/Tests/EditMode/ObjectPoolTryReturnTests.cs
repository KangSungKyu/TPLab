using System;
using System.Collections.Generic;
using NUnit.Framework;
using TPLab.Core.Pooling;

namespace TPLab.Core.Tests
{
    public class ObjectPoolTryReturnTests
    {
        private sealed class Item
        {
        }

        [Test]
        public void SuccessfulReturnResetsOnceAndReusesLoan()
        {
            int resets = 0;
            using (var pool = new ObjectPool<Item>(() => new Item(), 1, onReturn: item => resets++))
            {
                var tryReturn = TryReturnTestApi.Bind<Item>(pool);
                Assert.That(pool.TryRent(out var loan), Is.True);
                Assert.That(tryReturn(loan), Is.True);
                Assert.That(resets, Is.EqualTo(1));
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(loan));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DisposedPoolReturnsFalseBeforeInputValidation(int input)
        {
            int cleaned = 0;
            int resets = 0;
            using (var pool = new ObjectPool<Item>(() => new Item(), 1,
                onReturn: item => resets++, onDestroy: item => cleaned++))
            {
                var tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out var loan);
                pool.Dispose();
                var argument = input == 0 ? loan : input == 1 ? null : new Item();
                Assert.That(tryReturn(argument), Is.False);
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.That(resets, Is.Zero);
                Assert.That(cleaned, Is.EqualTo(1));
                Assert.Throws<ObjectDisposedException>(() => pool.Return(argument));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InvalidActiveReturnsKeepExistingValidation(int input)
        {
            using (var pool = new ObjectPool<Item>(() => new Item(), 1))
            {
                var tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out var loan);
                if (input == 0)
                {
                    Assert.Throws<ArgumentNullException>(() => tryReturn(null));
                }
                else if (input == 1)
                {
                    Assert.Throws<ArgumentException>(() => tryReturn(new Item()));
                }
                else
                {
                    pool.Return(loan);
                    Assert.Throws<InvalidOperationException>(() => tryReturn(loan));
                }
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.EqualTo(input == 2 ? 1 : 0));
                Assert.That(pool.CountRented, Is.EqualTo(input == 2 ? 0 : 1));
            }
        }

        [Test]
        public void DisposeCleanupMayTryReturnWithoutInterruptingOtherCleanup()
        {
            Func<Item, bool> tryReturn = null;
            var outcomes = new List<bool>();
            using (var pool = new ObjectPool<Item>(() => new Item(), 3,
                onDestroy: item => outcomes.Add(tryReturn(item))))
            {
                tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out _);
                pool.TryRent(out _);
                pool.TryRent(out var inactive);
                pool.Return(inactive);
                pool.Dispose();
                Assert.That(outcomes, Is.EqualTo(new[] { false, false, false }));
                Assert.That(pool.CountOwned, Is.Zero);
            }
        }

        [Test]
        public void ResetAndCleanupFailuresStillAggregateAndRecoverCapacity()
        {
            var resetFailure = new InvalidOperationException("try-return-reset");
            var cleanupFailure = new ApplicationException("try-return-cleanup");
            using (var pool = new ObjectPool<Item>(() => new Item(), 1,
                onReturn: item => throw resetFailure, onDestroy: item => throw cleanupFailure))
            {
                var tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out var loan);
                var failure = Assert.Throws<AggregateException>(() => tryReturn(loan));
                Assert.That(failure.InnerExceptions, Is.EqualTo(new Exception[] { resetFailure, cleanupFailure }));
                Assert.That(pool.CountOwned, Is.Zero);
            }
        }

        [Test]
        public void ReturnCallbackReentryStillThrowsAndRecoversCapacity()
        {
            Func<Item, bool> tryReturn = null;
            using (var pool = new ObjectPool<Item>(() => new Item(), 1,
                onReturn: item => tryReturn(item)))
            {
                tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out var loan);
                Assert.Throws<InvalidOperationException>(() => tryReturn(loan));
                Assert.That(pool.CountOwned, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RentOrPredicateReentryKeepsTheExistingGuard(bool predicate)
        {
            Func<Item, bool> tryReturn = null;
            Action<Item> reenter = item => Assert.Throws<InvalidOperationException>(() => tryReturn(item));
            using (var pool = new ObjectPool<Item>(() => new Item(), 1,
                onRent: predicate ? null : reenter,
                isAlive: predicate ? item => { reenter(item); return true; } : null))
            {
                tryReturn = TryReturnTestApi.Bind<Item>(pool);
                Assert.That(pool.TryRent(out var loan), Is.True);
                Assert.That(pool.CountRented, Is.EqualTo(1));
                Assert.That(tryReturn(loan), Is.True);
            }
        }

        [Test]
        public void WarmedTryReturnReuseAllocatesZero()
        {
            using (var pool = new ObjectPool<Item>(() => new Item(), 1))
            {
                var tryReturn = TryReturnTestApi.Bind<Item>(pool);
                pool.TryRent(out var warm);
                tryReturn(warm);
                bool returned = true;
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    pool.TryRent(out var loan);
                    returned &= tryReturn(loan);
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero);
                Assert.That(returned, Is.True);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
            }
        }
    }
}
