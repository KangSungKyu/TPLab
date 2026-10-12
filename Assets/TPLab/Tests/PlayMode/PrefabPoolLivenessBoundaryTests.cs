using System.Collections;
using NUnit.Framework;
using TPLab.Core.Pooling;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class PrefabPoolLivenessBoundaryTests
    {
        private GameObject _source;

        [SetUp]
        public void SetUp()
        {
            _source = new GameObject("PoolLivenessBoundarySource");
            _source.SetActive(false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(_source);
            yield return null;
        }

        [Test]
        public void LiveLoanIsPreservedWhenCapacityIsExhausted()
        {
            using (var pool = new PrefabPool(_source, 1))
            {
                Assert.That(pool.TryRent(out var clone), Is.True);
                Assert.That(pool.TryRent(out var exhausted), Is.False);
                Assert.That(exhausted, Is.Null);
                Assert.That(clone != null, Is.True);
                Assert.That(clone.activeSelf, Is.True);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.Zero);
                pool.Return(clone);
                Assert.That(pool.TryRent(out var reused), Is.True);
                Assert.That(reused, Is.SameAs(clone));
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DestroyedInactiveCloneIsSkippedAndReplacedInSameRent()
        {
            using (var pool = new PrefabPool(_source, 1))
            {
                Assert.That(pool.TryRent(out var clone), Is.True);
                pool.Return(clone);
                UnityEngine.Object.Destroy(clone);
                yield return null;
                Assert.That(clone == null, Is.True);
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.CountInactive, Is.EqualTo(1));
                Assert.That(pool.TryRent(out var replacement), Is.True);
                Assert.That(replacement != null, Is.True);
                Assert.That(replacement, Is.Not.SameAs(clone));
                Assert.That(pool.CountOwned, Is.EqualTo(1));
                Assert.That(pool.CountRented, Is.EqualTo(1));
                Assert.That(pool.CountInactive, Is.Zero);
            }
        }
    }
}
