using System;
using UnityEngine;

namespace TPLab.Core.Pooling
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
        private readonly ObjectPool<GameObject> _pool;

        /// <summary>Maximum number of owned clones, both rented and inactive.</summary>
        public int Capacity => _pool.Capacity;

        /// <summary>Tracked owned clone count; external destruction remains counted until lazy rent checks.</summary>
        public int CountOwned => _pool.CountOwned;

        /// <summary>Tracked borrowed clone count; getter does not check Unity liveness or allocate. External destruction remains counted until lazy rent checks.</summary>
        public int CountRented => _pool.CountRented;

        /// <summary>Tracked inactive clone count; getter does not check Unity liveness or allocate.</summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>Whether the pool has permanently ended its lifetime.</summary>
        public bool IsDisposed => _pool.IsDisposed;

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
            _storageRoot = new GameObject("PrefabPool");
            _storageRoot.SetActive(false);
            _storageRoot.transform.SetParent(parent, false);
            _pool = new ObjectPool<GameObject>(CreateInstance, capacity, RentInstance, ReturnInstance, DestroyInstance, instance => instance != null);
        }

        /// <summary>Reuses or creates a clone, configures it, then sets it active.</summary>
        /// <param name="instance">Borrowed clone on success; null when the total capacity is exhausted.</param>
        /// <returns>False only when all owned slots are rented; otherwise true.</returns>
        /// <exception cref="ObjectDisposedException">The pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Reentry, missing source/parent/root, or a destroyed clone.</exception>
        /// <remarks>Dead inactive clones are skipped; when capacity is full without reusable clones, externally destroyed loans free their slots. Counts are lazy. Callback exceptions propagate after discard. An inactive parent stays inactive.</remarks>
        public bool TryRent(out GameObject instance)
        {
            instance = null;
            EnsureOpenContext();
            return _pool.TryRent(out instance);
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
            EnsureOpenContext();
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            _pool.Return(instance);
        }

        /// <summary>Returns a borrowed instance through Return, or ignores the request if the pool is already disposed.</summary>
        /// <param name="instance">Live clone currently borrowed from this pool while open; ignored after disposal.</param>
        /// <returns>True after successful Return; false only when already disposed, including during disposal cleanup.</returns>
        /// <exception cref="ArgumentNullException">The open pool receives a null or destroyed clone.</exception>
        /// <exception cref="ArgumentException">The open pool does not own the instance.</exception>
        /// <exception cref="InvalidOperationException">Duplicate return, mutation reentry, lost context, or callback destruction while open.</exception>
        /// <exception cref="AggregateException">Both reset and failed-instance cleanup throw while open.</exception>
        /// <remarks>
        /// Disposal takes precedence over input validation: false invokes no callbacks and changes no state.
        /// Dispose owns cleanup of outstanding loans; this method does not retry that cleanup or destroy foreign instances.
        /// While open, Return validation, callback errors, failure cleanup, and mutation reentry rules are preserved.
        /// Calls are synchronous and restricted to the Unity main thread; there is no concurrent-disposal synchronization.
        /// The wrapper adds no allocation; caller callbacks and cold inactive storage may allocate.
        /// </remarks>
        public bool TryReturn(GameObject instance)
        {
            if (IsDisposed)
            {
                return false;
            }
            Return(instance);
            return true;
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
            try
            {
                _pool.Dispose();
            }
            finally
            {
                if (_pool.IsDisposed)
                {
                    DestroyInstance(_storageRoot);
                }
            }
        }

        private GameObject CreateInstance()
        {
            if (_prefab == null)
            {
                throw new InvalidOperationException("The source prefab has been destroyed.");
            }

            var instance = UnityEngine.Object.Instantiate(_prefab, _storageRoot.transform, false);
            try
            {
                // The inactive root prevents OnEnable even when the source itself is active.
                instance.SetActive(false);
                return instance;
            }
            catch
            {
                DestroyInstance(instance);
                throw;
            }
        }

        private void RentInstance(GameObject instance)
        {
            EnsureInstanceAlive(instance);
            instance.transform.SetParent(_parent, false);
            ResetTransform(instance);
            _onRent?.Invoke(instance);
            EnsureInstanceAlive(instance);
            EnsureContextAlive();
            instance.SetActive(true);
            EnsureInstanceAlive(instance);
            EnsureContextAlive();
        }

        private void ReturnInstance(GameObject instance)
        {
            instance.SetActive(false);
            EnsureInstanceAlive(instance);
            _onReturn?.Invoke(instance);
            EnsureInstanceAlive(instance);
            EnsureContextAlive();
            instance.transform.SetParent(_storageRoot.transform, false);
            ResetTransform(instance);
        }

        private void ResetTransform(GameObject instance)
        {
            instance.transform.localPosition = _localPosition;
            instance.transform.localRotation = _localRotation;
            instance.transform.localScale = _localScale;
        }

        private void EnsureOpenContext()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(PrefabPool));
            }
            EnsureContextAlive();
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
