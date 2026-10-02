using System;
using System.Collections.Generic;
using MyLab.Core.Pooling;
using NUnit.Framework;

namespace MyLab.Core.Tests
{
    public class ObjectPoolTests
    {
        private sealed class Data : IDisposable
        {
            public int Value;
            public bool Disposed;

            public override bool Equals(object other) => other is Data data && Value == data.Value;
            public override int GetHashCode() => Value;
            public void Dispose() => Disposed = true;
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ConstructorRejectsNonPositiveCapacity(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectPool<Data>(() => new Data(), capacity));
        }

        [Test]
        public void ConstructorRejectsNullFactory()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Data>(null, 1));
        }

        [Test]
        public void ReturnResetsPlainClassAndNextRentReusesIt()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1, onReturn: data => data.Value = 0))
            {
                Assert.That(pool.TryRent(out var first), Is.True);
                first.Value = 42;
                pool.Return(first);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.TryRent(out var second), Is.True);
                Assert.That(second, Is.SameAs(first));
                Assert.That(second.Value, Is.Zero);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
            }
        }

        [Test]
        public void CapacityIncludesBorrowedObjects()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1))
            {
                Assert.That(pool.TryRent(out var first), Is.True);
                Assert.That(pool.TryRent(out var exhausted), Is.False);
                Assert.That(exhausted, Is.Null);
                pool.Return(first);
                Assert.That(pool.TryRent(out var second), Is.True);
                Assert.That(second, Is.SameAs(first));
                Assert.That(pool.CountOwned, Is.EqualTo(1));
            }
        }

        [Test]
        public void ReturnRejectsNull()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1))
            {
                Assert.Throws<ArgumentNullException>(() => pool.Return(null));
            }
        }

        [Test]
        public void ReturnRejectsForeignInstanceWithoutDestroyingIt()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1, onDestroy: data => data.Dispose()))
            {
                var foreign = new Data();
                Assert.Throws<ArgumentException>(() => pool.Return(foreign));
                Assert.That(foreign.Disposed, Is.False);
                Assert.That(pool.CountOwned, Is.Zero);
            }
        }

        [Test]
        public void DuplicateReturnIsRejectedAndCannotCreateTwoLoansOfTheSameInstance()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1))
            {
                Assert.That(pool.TryRent(out var instance), Is.True);
                pool.Return(instance);
                Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.TryRent(out _), Is.True);
                Assert.That(pool.TryRent(out _), Is.False);
            }
        }

        [Test]
        public void EqualValuesStillRepresentDistinctOwnedInstances()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 2))
            {
                Assert.That(pool.TryRent(out var first), Is.True);
                Assert.That(pool.TryRent(out var second), Is.True);
                Assert.That(first.Equals(second), Is.True);
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(pool.CountOwned, Is.EqualTo(2));
                pool.Return(first);
                pool.Return(second);
                Assert.That(pool.CountInactive, Is.EqualTo(2));
            }
        }

        [Test]
        public void ChangingValueHashDoesNotLoseOwnershipOrAdmitEqualForeignObjects()
        {
            using (var pool = new ObjectPool<Data>(() => new Data(), 1))
            {
                Assert.That(pool.TryRent(out var instance), Is.True);
                instance.Value = 100;
                Assert.Throws<ArgumentException>(() => pool.Return(new Data { Value = 100 }));
                pool.Return(instance);
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(instance));
            }
        }

        [Test]
        public void FactoryFailureLeavesCapacityAvailableForRetry()
        {
            bool fail = true;
            using (var pool = new ObjectPool<Data>(() => fail ? throw new InvalidOperationException("factory") : new Data(), 1))
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(pool.CountOwned, Is.Zero);
                fail = false;
                Assert.That(pool.TryRent(out _), Is.True);
            }
        }

        [Test]
        public void NullFactoryResultIsRejectedWithoutConsumingCapacity()
        {
            using (var pool = new ObjectPool<Data>(() => null, 1))
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(pool.CountOwned, Is.Zero);
            }
        }

        [Test]
        public void RepeatedFactoryReferenceIsRejectedWithoutDestroyingTheOriginalLoan()
        {
            var shared = new Data();
            using (var pool = new ObjectPool<Data>(() => shared, 2, onDestroy: data => data.Dispose()))
            {
                Assert.That(pool.TryRent(out var first), Is.True);
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.That(shared.Disposed, Is.False);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
                pool.Return(first);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HookFailureDiscardsOnlyAffectedInstanceAndAllowsRetry(bool failOnRent)
        {
            bool fail = false;
            var discarded = new List<Data>();
            Action<Data> hook = _ => { if (fail) throw new InvalidOperationException("hook"); };
            using (var pool = new ObjectPool<Data>(() => new Data(), 2,
                onRent: failOnRent ? hook : null, onReturn: failOnRent ? null : hook,
                onDestroy: data => { discarded.Add(data); data.Dispose(); }))
            {
                Assert.That(pool.TryRent(out var preserved), Is.True);
                Data failed = null;
                if (!failOnRent) Assert.That(pool.TryRent(out failed), Is.True);
                fail = true;
                if (failOnRent) Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                else Assert.Throws<InvalidOperationException>(() => pool.Return(failed));
                Assert.That(discarded.Count, Is.EqualTo(1));
                Assert.That(discarded[0].Disposed, Is.True);
                Assert.That(preserved.Disposed, Is.False);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.Zero);
                fail = false;
                Assert.That(pool.TryRent(out _), Is.True);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HookAndCleanupFailuresAreBothReported(bool failOnRent)
        {
            var hookError = new InvalidOperationException("hook");
            var cleanupError = new ArgumentException("cleanup");
            Action<Data> hook = _ => throw hookError;
            var pool = new ObjectPool<Data>(() => new Data(), 1,
                onRent: failOnRent ? hook : null, onReturn: failOnRent ? null : hook,
                onDestroy: _ => throw cleanupError);
            try
            {
                Data instance = null;
                if (!failOnRent) Assert.That(pool.TryRent(out instance), Is.True);
                var error = failOnRent
                    ? Assert.Throws<AggregateException>(() => pool.TryRent(out _))
                    : Assert.Throws<AggregateException>(() => pool.Return(instance));
                Assert.That(error.InnerExceptions, Does.Contain(hookError));
                Assert.That(error.InnerExceptions, Does.Contain(cleanupError));
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.That(pool.CountInactive, Is.Zero);
            }
            finally
            {
                pool.Dispose();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MutationFromCallbacksIsRejected(bool duringRent)
        {
            ObjectPool<Data> pool = null;
            Action<Data> hook = instance =>
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
                Assert.Throws<InvalidOperationException>(() => pool.Dispose());
            };
            pool = new ObjectPool<Data>(() => new Data(), 1,
                onRent: duringRent ? hook : null, onReturn: duringRent ? null : hook);
            using (pool)
            {
                Assert.That(pool.TryRent(out var instance), Is.True);
                pool.Return(instance);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
            }
        }

        [Test]
        public void MutationFromFactoryIsRejected()
        {
            ObjectPool<Data> pool = null;
            pool = new ObjectPool<Data>(() =>
            {
                Assert.Throws<InvalidOperationException>(() => pool.TryRent(out _));
                Assert.Throws<InvalidOperationException>(() => pool.Dispose());
                return new Data();
            }, 1);
            using (pool) Assert.That(pool.TryRent(out _), Is.True);
        }

        [Test]
        public void DisposeCleansUpBorrowedAndIdleObjectsOnceAndRejectsReuse()
        {
            int destroyed = 0;
            var pool = new ObjectPool<Data>(() => new Data(), 2, onDestroy: data => { destroyed++; data.Dispose(); });
            Assert.That(pool.TryRent(out var borrowed), Is.True);
            Assert.That(pool.TryRent(out var idle), Is.True);
            pool.Return(idle);
            pool.Dispose();
            pool.Dispose();
            Assert.That(destroyed, Is.EqualTo(2));
            Assert.That(borrowed.Disposed && idle.Disposed, Is.True);
            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(pool.CountRented, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => pool.TryRent(out _));
            Assert.Throws<ObjectDisposedException>(() => pool.Return(borrowed));
        }

        [Test]
        public void DisposeAttemptsAllCleanupEvenWhenEveryDestroyCallbackThrows()
        {
            int attempts = 0;
            var pool = new ObjectPool<Data>(() => new Data(), 2, onDestroy: _ =>
            {
                attempts++;
                throw new InvalidOperationException("destroy");
            });
            Assert.That(pool.TryRent(out _), Is.True);
            Assert.That(pool.TryRent(out var idle), Is.True);
            pool.Return(idle);
            var error = Assert.Throws<AggregateException>(() => pool.Dispose());
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(error.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountOwned, Is.Zero);
            Assert.DoesNotThrow(() => pool.Dispose());
        }

        [Test]
        public void CleanupCallbackSeesClosedEmptyPoolAndCannotRentAgain()
        {
            ObjectPool<Data> pool = null;
            pool = new ObjectPool<Data>(() => new Data(), 1, onDestroy: data =>
            {
                Assert.That(pool.IsDisposed, Is.True);
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.Throws<ObjectDisposedException>(() => pool.TryRent(out _));
                Assert.DoesNotThrow(() => pool.Dispose());
            });
            Assert.That(pool.TryRent(out _), Is.True);
            pool.Dispose();
        }

        [Test]
        public void ResourceDisposalIsExplicitThroughDestroyCallback()
        {
            var pool = new ObjectPool<Data>(() => new Data(), 1);
            Assert.That(pool.TryRent(out var instance), Is.True);
            pool.Dispose();
            Assert.That(instance.Disposed, Is.False, "IDisposable is not implicitly invoked.");
        }
    }
}
