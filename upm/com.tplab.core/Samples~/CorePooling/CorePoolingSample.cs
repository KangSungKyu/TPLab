using System;
using TPLab.Core.Pooling;
using UnityEngine;

namespace TPLab.Samples.CorePooling
{
    /// <summary>Core-only rent, bounded capacity, reset, reuse and owner cleanup example.</summary>
    public sealed class CorePoolingSample : MonoBehaviour
    {
        private sealed class Item
        {
            public int Value;
        }

        private void Start()
        {
            Run();
            Debug.Log("TPLab Core pooling sample passed.");
        }

        /// <summary>Runs synchronously, throws if an observable pool contract fails, and always disposes its owner.</summary>
        public static void Run()
        {
            int destroyed = 0;
            using (var pool = new ObjectPool<Item>(() => new Item(), 1,
                onReturn: item => item.Value = 0, onDestroy: item => destroyed++))
            {
                if (!pool.TryRent(out var first)) throw new InvalidOperationException("Initial rent failed.");
                first.Value = 42;
                if (pool.TryRent(out _)) throw new InvalidOperationException("Capacity was exceeded.");
                pool.Return(first);
                if (!pool.TryRent(out var second) || !ReferenceEquals(first, second) || second.Value != 0)
                    throw new InvalidOperationException("Return must reset and next rent must reuse the owned instance.");
                pool.Return(second);
                if (pool.CountOwned != 1 || pool.CountInactive != 1 || pool.CountRented != 0)
                    throw new InvalidOperationException("Loan counts did not return to idle.");
            }
            if (destroyed != 1) throw new InvalidOperationException("The owner must clean up its instance once.");
        }
    }
}
