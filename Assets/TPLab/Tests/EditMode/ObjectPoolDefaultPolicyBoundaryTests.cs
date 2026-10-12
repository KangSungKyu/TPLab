using NUnit.Framework;
using TPLab.Core.Pooling;

namespace TPLab.Core.Tests
{
    public class ObjectPoolDefaultPolicyBoundaryTests
    {
        private sealed class Item
        {
            public bool Alive = true;
        }

        [Test]
        public void OmittedLivenessPolicyPreservesUncheckedLoansAndReuse()
        {
            int created = 0;
            int cleaned = 0;
            using (var pool = new ObjectPool<Item>(() => { created++; return new Item(); },
                1, onDestroy: item => cleaned++))
            {
                Assert.That(pool.TryRent(out var loan), Is.True);
                loan.Alive = false;
                Assert.That(pool.TryRent(out var exhausted), Is.False);
                Assert.That(exhausted, Is.Null);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
                Assert.That(cleaned, Is.Zero);
                pool.Return(loan);
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(loan));
                Assert.That(reused.Alive, Is.False);
                Assert.That(created, Is.EqualTo(1));
                Assert.That(cleaned, Is.Zero);
            }
            Assert.That(cleaned, Is.EqualTo(1));
        }
    }
}
