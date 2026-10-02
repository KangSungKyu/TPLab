using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MyLab.Core.Pooling
{
    /// <summary>
    /// Owns a bounded set of local prefab clones, including rented instances.
    /// All operations must run on the Unity main thread; consumers return clones instead of destroying them.
    /// </summary>
    public sealed class PrefabPool : IDisposable
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly bool _hasParent;
        private readonly GameObject _storageRoot;
        private readonly Vector3 _localPosition;
        private readonly Quaternion _localRotation;
        private readonly Vector3 _localScale;
        private readonly Action<GameObject> _onRent;
        private readonly Action<GameObject> _onReturn;
        private readonly ObjectPool<GameObject> _storage;
        private readonly HashSet<GameObject> _owned = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _rented = new HashSet<GameObject>();
        private bool _isBusy;

        /// <summary>Maximum number of owned clones, both rented and inactive.</summary>
        public int Capacity { get; }

        /// <summary>Number of clones currently owned by this pool.</summary>
        public int CountOwned => _owned.Count;

        /// <summary>Number of clones currently borrowed by consumers.</summary>
        public int CountRented => _rented.Count;

        /// <summary>Number of clones available for reuse.</summary>
        public int CountInactive => _owned.Count - _rented.Count;

        /// <summary>Whether the pool has permanently ended its lifetime.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Creates an empty pool. The caller retains ownership of the prefab and parent.</summary>
        /// <param name="prefab">Live prefab or source GameObject; keep it alive while new clones are needed.</param>
        /// <param name="capacity">Positive upper bound on the total owned clone count.</param>
        /// <param name="parent">Optional parent for rented clones; keep it alive until disposal.</param>
        /// <param name="onRent">Configures an inactive clone before activation. Must not mutate this pool.</param>
        /// <param name="onReturn">Resets consumer state after deactivation. Must not mutate this pool.</param>
        /// <exception cref="ArgumentNullException">The prefab is null or destroyed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Capacity is not positive.</exception>
        public PrefabPool(GameObject prefab, int capacity, Transform parent = null,
            Action<GameObject> onRent = null, Action<GameObject> onReturn = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _prefab = prefab;
            _parent = parent;
            _hasParent = parent != null;
            _localPosition = prefab.transform.localPosition;
            _localRotation = prefab.transform.localRotation;
            _localScale = prefab.transform.localScale;
            _onRent = onRent;
            _onReturn = onReturn;
            Capacity = capacity;

            _storageRoot = new GameObject("PrefabPool");
            _storageRoot.SetActive(false);
            _storageRoot.transform.SetParent(parent, false);
            // Ownership checks remain enabled in every build through the sets above.
            // Native CountAll is not used: a failed callback discards a checked-out clone.
            _storage = new ObjectPool<GameObject>(CreateInstance, collectionCheck: false,
                defaultCapacity: 0, maxSize: capacity);
        }

        /// <summary>Reuses or creates a clone, configures it, then sets it active.</summary>
        /// <param name="instance">Borrowed clone on success; null when the total capacity is exhausted.</param>
        /// <returns>False only when all owned slots are rented; otherwise true.</returns>
        /// <exception cref="ObjectDisposedException">The pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Reentry, missing source/parent/root, or a destroyed clone.</exception>
        /// <remarks>Callback exceptions propagate after the affected clone is discarded. An inactive parent stays inactive.</remarks>
        public bool TryRent(out GameObject instance)
        {
            instance = null;
            BeginOperation();
            GameObject candidate = null;
            try
            {
                if (CountInactive == 0 && CountOwned >= Capacity)
                {
                    return false;
                }

                candidate = _storage.Get();
                EnsureInstanceAlive(candidate);
                _rented.Add(candidate);
                candidate.transform.SetParent(_parent, false);
                ResetTransform(candidate);
                _onRent?.Invoke(candidate);
                EnsureInstanceAlive(candidate);
                EnsureContextAlive();
                candidate.SetActive(true);
                EnsureInstanceAlive(candidate);
                EnsureContextAlive();
                instance = candidate;
                return true;
            }
            catch
            {
                Discard(candidate);
                throw;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>Deactivates a borrowed clone, resets consumer state, then stores it for reuse.</summary>
        /// <param name="instance">Live clone currently borrowed from this pool.</param>
        /// <exception cref="ArgumentNullException">The instance is null or destroyed.</exception>
        /// <exception cref="ArgumentException">The instance belongs to another owner.</exception>
        /// <exception cref="InvalidOperationException">Duplicate return, reentry, lost context, or a destroyed clone.</exception>
        /// <exception cref="ObjectDisposedException">The pool has been disposed.</exception>
        /// <remarks>Callback failures discard only this clone and propagate; foreign instances remain unchanged.</remarks>
        public void Return(GameObject instance)
        {
            BeginOperation();
            try
            {
                if (instance == null)
                {
                    throw new ArgumentNullException(nameof(instance));
                }
                if (!_owned.Contains(instance))
                {
                    throw new ArgumentException("Instance is not owned by this pool.", nameof(instance));
                }
                if (!_rented.Contains(instance))
                {
                    throw new InvalidOperationException("Instance has already been returned.");
                }

                try
                {
                    instance.SetActive(false);
                    EnsureInstanceAlive(instance);
                    _onReturn?.Invoke(instance);
                    EnsureInstanceAlive(instance);
                    EnsureContextAlive();
                    instance.transform.SetParent(_storageRoot.transform, false);
                    ResetTransform(instance);
                    _storage.Release(instance);
                    _rented.Remove(instance);
                }
                catch
                {
                    Discard(instance);
                    throw;
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>Destroys all owned clones and the storage root, permanently closing this pool.</summary>
        /// <remarks>
        /// Repeated calls are ignored. Clones are deactivated immediately; PlayMode destruction ends this frame.
        /// Return callbacks are not invoked. The prefab and external parent are preserved.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Called from an in-progress rent or return.</exception>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            if (_isBusy)
            {
                throw new InvalidOperationException("Pool mutation is already running.");
            }

            IsDisposed = true;
            var instances = new GameObject[_owned.Count];
            _owned.CopyTo(instances);
            _owned.Clear();
            _rented.Clear();
            _storage.Clear();
            foreach (var instance in instances)
            {
                DestroyInstance(instance);
            }
            DestroyInstance(_storageRoot);
        }

        private GameObject CreateInstance()
        {
            if (_prefab == null)
            {
                throw new InvalidOperationException("The source prefab has been destroyed.");
            }

            var instance = UnityEngine.Object.Instantiate(_prefab, _storageRoot.transform, false);
            _owned.Add(instance);
            try
            {
                // The inactive root prevents OnEnable even when the source itself is active.
                instance.SetActive(false);
                return instance;
            }
            catch
            {
                Discard(instance);
                throw;
            }
        }

        private void ResetTransform(GameObject instance)
        {
            instance.transform.localPosition = _localPosition;
            instance.transform.localRotation = _localRotation;
            instance.transform.localScale = _localScale;
        }

        private void BeginOperation()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(PrefabPool));
            }
            if (_isBusy)
            {
                throw new InvalidOperationException("Pool mutation is already running.");
            }
            EnsureContextAlive();
            _isBusy = true;
        }

        private void EnsureContextAlive()
        {
            if (_storageRoot == null || (_hasParent && _parent == null))
            {
                throw new InvalidOperationException("The pool storage root or parent has been destroyed.");
            }
        }

        private static void EnsureInstanceAlive(GameObject instance)
        {
            if (instance == null)
            {
                throw new InvalidOperationException("A pooled clone has been destroyed.");
            }
        }

        private void Discard(GameObject instance)
        {
            // A destroyed Unity object still has a managed reference that must leave the ownership sets.
            if (ReferenceEquals(instance, null))
            {
                return;
            }
            _rented.Remove(instance);
            _owned.Remove(instance);
            DestroyInstance(instance);
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }
            instance.SetActive(false);
            if (instance == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
