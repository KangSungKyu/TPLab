using System;
using System.Collections.Generic;
using NUnit.Framework;
using TPLab.Core.Pooling;

namespace TPLab.Core.Tests
{
    public class ObjectPoolLivenessTests
    {
        private sealed class Item
        {
            public bool Alive = true;
        }

        [Test]
        public void DeadLoanIsReclaimedWhileLiveLoanRemainsOwned()
        {
            var cleaned = new List<Item>();
            using (var pool = new ObjectPool<Item>(() => new Item(), 2,
                onDestroy: cleaned.Add, isAlive: item => item.Alive))
            {
                pool.TryRent(out var dead);
                pool.TryRent(out var survivor);
                dead.Alive = false;
                Assert.That(pool.CountOwned, Is.EqualTo(2));
                Assert.That(pool.CountRented, Is.EqualTo(2));
                Assert.That(pool.TryRent(out var replacement), Is.True);
                Assert.That(replacement, Is.Not.SameAs(dead));
                Assert.That(cleaned, Is.EqualTo(new[] { dead }));
                pool.Return(survivor);
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(survivor));
                Assert.That(pool.CountOwned, Is.EqualTo(2));
            }
        }

        [Test]
        public void InactiveSkipPreservesSurvivorLifoAndDoesNotSweepLoans()
        {
            var checkedItems = new List<Item>();
            using (var pool = new ObjectPool<Item>(() => new Item(), 4,
                isAlive: item => { checkedItems.Add(item); return item.Alive; }))
            {
                pool.TryRent(out var oldest);
                pool.TryRent(out var newest);
                pool.TryRent(out var deadInactive);
                pool.TryRent(out var deadLoan);
                pool.Return(oldest);
                pool.Return(newest);
                pool.Return(deadInactive);
                deadInactive.Alive = false;
                deadLoan.Alive = false;
                checkedItems.Clear();
                Assert.That(pool.TryRent(out var first), Is.True);
                Assert.That(first, Is.SameAs(newest));
                Assert.That(checkedItems, Is.EqualTo(new[] { deadInactive, newest }));
                Assert.That(pool.CountOwned, Is.EqualTo(3));
                Assert.That(pool.TryRent(out var second), Is.True);
                Assert.That(second, Is.SameAs(oldest));
            }
        }

        [Test]
        public void AllDeadInactiveAreDiscardedBeforeCreatingReplacement()
        {
            var cleaned = new List<Item>();
            using (var pool = new ObjectPool<Item>(() => new Item(), 2,
                onDestroy: cleaned.Add, isAlive: item => item.Alive))
            {
                pool.TryRent(out var first);
                pool.TryRent(out var second);
                pool.Return(first);
                pool.Return(second);
                first.Alive = second.Alive = false;
                Assert.That(pool.TryRent(out var replacement), Is.True);
                Assert.That(cleaned, Is.EqualTo(new[] { second, first }));
                Assert.That(replacement.Alive, Is.True);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.Zero);
            }
        }

        [Test]
        public void NullPredicatePreservesUncheckedLegacyPolicy()
        {
            using (var pool = new ObjectPool<Item>(() => new Item(), 1))
            {
                pool.TryRent(out var item);
                item.Alive = false;
                Assert.That(pool.TryRent(out _), Is.False);
                pool.Return(item);
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(item));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InactivePredicateFailureOrReentryPreservesCandidate(bool reenter)
        {
            ObjectPool<Item> pool = null;
            bool fail = false;
            pool = new ObjectPool<Item>(() => new Item(), 1, isAlive: item =>
            {
                if (fail)
                {
                    if (reenter) pool.Dispose();
                    throw new InvalidOperationException("predicate");
                }
                return item.Alive;
            });
            using (pool)
            {
                pool.TryRent(out var item);
                pool.Return(item);
                fail = true;
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.IsDisposed, Is.False);
                fail = false;
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(item));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SweepPredicateFailureOrReentryPreservesAllOwnership(bool reenter)
        {
            ObjectPool<Item> pool = null;
            bool fail = false;
            int visits = 0;
            var cleaned = new List<Item>();
            pool = new ObjectPool<Item>(() => new Item(), 2, onDestroy: cleaned.Add, isAlive: item =>
            {
                if (fail && ++visits == 2)
                {
                    if (reenter) pool.TryRent(out _);
                    throw new InvalidOperationException("predicate");
                }
                return item.Alive;
            });
            using (pool)
            {
                pool.TryRent(out var first);
                pool.TryRent(out var second);
                first.Alive = second.Alive = false;
                fail = true;
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(pool.CountOwned, Is.EqualTo(2));
                Assert.That(pool.CountRented, Is.EqualTo(2));
                Assert.That(pool.CountInactive, Is.Zero);
                Assert.That(cleaned, Is.Empty);
                fail = false;
                Assert.That(pool.TryRent(out _), Is.True);
                Assert.That(cleaned.Count, Is.EqualTo(2));
                Assert.That(pool.CountOwned, Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EveryDeadCleanupIsAttemptedAndFailureLeavesCapacityRecovered(bool inactive)
        {
            var cleaned = new List<Item>();
            using (var pool = new ObjectPool<Item>(() => new Item(), 2,
                onDestroy: item =>
                {
                    cleaned.Add(item);
                    if (!item.Alive) throw new InvalidOperationException("cleanup");
                }, isAlive: item => item.Alive))
            {
                pool.TryRent(out var first);
                pool.TryRent(out var second);
                if (inactive)
                {
                    pool.Return(first);
                    pool.Return(second);
                }
                first.Alive = second.Alive = false;
                var failure = Assert.Throws<AggregateException>(() => pool.TryRent(out _));
                Assert.That(failure.InnerExceptions.Count, Is.EqualTo(2));
                Assert.That(cleaned.Count, Is.EqualTo(2));
                Assert.That(cleaned, Does.Contain(first));
                Assert.That(cleaned, Does.Contain(second));
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.CountInactive, Is.Zero);
                Assert.That(pool.TryRent(out _), Is.True);
                Assert.That(pool.TryRent(out _), Is.True);
                Assert.That(cleaned.Count, Is.EqualTo(2));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeadFactoryResultIsRejectedAndCleanedOnce(bool cleanupFails)
        {
            var dead = new Item { Alive = false };
            bool createDead = true;
            int cleanups = 0;
            int rentHooks = 0;
            using (var pool = new ObjectPool<Item>(() => createDead ? dead : new Item(), 1,
                onRent: item => rentHooks++,
                onDestroy: item =>
                {
                    cleanups++;
                    if (cleanupFails && !item.Alive) throw new Exception("cleanup");
                }, isAlive: item => item.Alive))
            {
                if (cleanupFails)
                {
                    var failure = Assert.Throws<AggregateException>(() => pool.TryRent(out _));
                    Assert.That(failure.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
                    Assert.That(failure.InnerExceptions.Count, Is.EqualTo(2));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                }
                Assert.That(cleanups, Is.EqualTo(1));
                Assert.That(rentHooks, Is.Zero);
                Assert.That(pool.CountOwned, Is.Zero);
                createDead = false;
                Assert.That(pool.TryRent(out _), Is.True);
            }
        }

        [Test]
        public void FreshFactoryPredicateFailureDiscardsOwnedResultWithoutRentHook()
        {
            var item = new Item();
            int cleanups = 0;
            int rentHooks = 0;
            bool fail = true;
            using (var pool = new ObjectPool<Item>(() => item, 1,
                onRent: value => rentHooks++, onDestroy: value => cleanups++,
                isAlive: value =>
                {
                    if (fail) throw new InvalidOperationException("predicate");
                    return value.Alive;
                }))
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.That(cleanups, Is.EqualTo(1));
                Assert.That(rentHooks, Is.Zero);
                fail = false;
                Assert.That(pool.TryRent(out var rented), Is.True);
                Assert.That(rented, Is.SameAs(item));
            }
        }

        [Test]
        public void WarmedDeadLoanRecoveryAddsNoPoolAllocation()
        {
            var first = new Item();
            var second = new Item();
            Item next = first;
            using (var pool = new ObjectPool<Item>(() => next, 1, isAlive: item => item.Alive))
            {
                pool.TryRent(out var current);
                current.Alive = false;
                next = second;
                pool.TryRent(out current);
                bool rented = true;
                var measurement = GcAllocationMeasurement.Measure(() =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        current.Alive = false;
                        next = ReferenceEquals(current, first) ? second : first;
                        next.Alive = true;
                        rented &= pool.TryRent(out current);
                    }
                });
                Assert.That(measurement.EventCount, Is.Zero);
                Assert.That(rented, Is.True);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
            }
        }

        [Test]
        public void WarmedReuseCapacitySweepAndCountGettersAllocateZero()
        {
            using (var pool = new ObjectPool<Item>(() => new Item(), 2, isAlive: item => item.Alive))
            {
                pool.TryRent(out var first);
                pool.TryRent(out var second);
                pool.Return(first);
                pool.TryRent(out first);
                pool.TryRent(out _);
                pool.Return(first);
                long checksum = 0;
                bool rented = true;
                bool exhausted = true;
                var measurement = GcAllocationMeasurement.Measure(() =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        rented &= pool.TryRent(out first);
                        exhausted &= !pool.TryRent(out _);
                        checksum += pool.CountOwned + pool.CountRented + pool.CountInactive;
                        pool.Return(first);
                    }
                });
                Assert.That(measurement.EventCount, Is.Zero);
                Assert.That(rented && exhausted, Is.True);
                Assert.That(checksum, Is.EqualTo(4000));
                Assert.That(second.Alive, Is.True);
            }
        }
    }
}
