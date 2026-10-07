using System;
using TPLab.Core.Pooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Tests
{
    public class PrefabPoolValidationTests
    {
        private GameObject _source;

        [SetUp]
        public void SetUp()
        {
            _source = new GameObject("PoolValidationSource");
            _source.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_source);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ConstructorRejectsNonPositiveCapacity(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrefabPool(_source, capacity));
        }

        [Test]
        public void ConstructorRejectsNullPrefab()
        {
            Assert.Throws<ArgumentNullException>(() => new PrefabPool(null, 1));
        }

        [Test]
        public void ConstructorRejectsDestroyedPrefab()
        {
            UnityEngine.Object.DestroyImmediate(_source);
            Assert.Throws<ArgumentNullException>(() => new PrefabPool(_source, 1));
        }

        [Test]
        public void ConstructorStartsEmptyAndPreservesSource()
        {
            using (var pool = new PrefabPool(_source, 2))
            {
                Assert.That(pool.Capacity, Is.EqualTo(2));
                Assert.That(pool.CountOwned, Is.Zero);
                Assert.That(pool.CountRented, Is.Zero);
                Assert.That(pool.CountInactive, Is.Zero);
                Assert.That(pool.IsDisposed, Is.False);
            }
            Assert.That(_source != null, Is.True);
            Assert.That(_source.activeSelf, Is.False);
        }

        [Test]
        public void InstantiatesPrefabAssetWithoutChangingIt()
        {
            string path = "Assets/TPLab/Tests/EditMode/PoolFixture-" + Guid.NewGuid().ToString("N") + ".prefab";
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(_source, path);
                using (var pool = new PrefabPool(prefab, 1))
                {
                    Assert.That(pool.TryRent(out var instance), Is.True);
                    Assert.That(instance, Is.Not.SameAs(prefab));
                    Assert.That(instance.activeSelf, Is.True);
                    pool.Return(instance);
                    Assert.That(instance.activeSelf, Is.False);
                }
                Assert.That(prefab != null, Is.True);
                Assert.That(prefab.activeSelf, Is.False);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
