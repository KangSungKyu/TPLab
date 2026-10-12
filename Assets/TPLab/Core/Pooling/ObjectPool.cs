using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TPLab.Core.Pooling
{
    /// <summary>
    /// Owns a bounded set of reference-type instances independently of Unity.
    /// Calls are single-threaded; factories and callbacks must not mutate this pool recursively.
    /// </summary>
    /// <typeparam name="T">Reference type. Ownership uses reference identity, ignoring value equality and mutable hashes.</typeparam>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Func<T> _createInstance;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDestroy;
        private readonly Func<T, bool> _isAlive;
        private readonly List<T> _deadInstances;
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly Dictionary<T, bool> _owned = new Dictionary<T, bool>(new ReferenceComparer());
        private bool _isBusy;

        /// <summary>Maximum total owned instances, including outstanding loans.</summary>
        public int Capacity { get; }

        /// <summary>Current tracked owned count; does not check liveness or allocate.</summary>
        public int CountOwned => _owned.Count;

        /// <summary>Current tracked loan count; does not check liveness or allocate. External death remains counted until lazy rent checks.</summary>
        public int CountRented => _owned.Count - _inactive.Count;

        /// <summary>Current tracked inactive count; does not check liveness or allocate.</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>Whether this pool has permanently ended its lifetime.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Creates an empty pool with caller-provided creation, reset, and cleanup policies.</summary>
        /// <param name="createInstance">Produces a new non-null instance exclusively owned by this pool; cleans up partial creation on failure.</param>
        /// <param name="capacity">Positive upper bound on total owned instances.</param>
        /// <param name="onRent">Optional preparation before handing the instance to a consumer.</param>
        /// <param name="onReturn">Optional reset before placing the instance in inactive storage.</param>
        /// <param name="onDestroy">Optional cleanup for discarded or disposed instances; provide it for owned external resources.</param>
        /// <param name="isAlive">Optional liveness policy checked during rent under the mutation guard; null preserves unchecked reuse. Must not mutate this pool.</param>
        /// <exception cref="ArgumentNullException">The factory is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Capacity is not positive.</exception>
        /// <remarks>No implicit IDisposable call is made; the caller explicitly supplies the appropriate cleanup policy.</remarks>
        public ObjectPool(Func<T> createInstance, int capacity, Action<T> onRent = null,
            Action<T> onReturn = null, Action<T> onDestroy = null, Func<T, bool> isAlive = null)
        {
            if (createInstance == null)
            {
                throw new ArgumentNullException(nameof(createInstance));
            }
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _createInstance = createInstance;
            _onRent = onRent;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _isAlive = isAlive;
            _deadInstances = isAlive == null ? null : new List<T>(capacity);
            Capacity = capacity;
        }

        /// <summary>Reuses or creates an owned instance, invokes preparation, then lends it to the caller.</summary>
        /// <param name="instance">Borrowed instance on success; null when capacity is exhausted or an exception occurs.</param>
        /// <returns>False only when all available capacity is rented; otherwise true.</returns>
        /// <exception cref="ObjectDisposedException">The pool is closed.</exception>
        /// <exception cref="InvalidOperationException">Mutation reentry or an invalid factory result.</exception>
        /// <exception cref="AggregateException">Preparation and cleanup both fail, or dead-instance cleanup fails; all detected dead cleanup is attempted.</exception>
        /// <remarks>Factory errors propagate. Dead inactive entries are skipped in LIFO order. Owned loans are checked only when reuse is unavailable and capacity is full. Inactive predicate failure preserves the Peek candidate; sweep predicate failure preserves unswept ownership. Dead or predicate-failing factory results use owned failure cleanup. Preparation failure restores capacity. Warmed reuse and capacity checks add no allocation; creation and caller callbacks may allocate.</remarks>
        public bool TryRent(out T instance)
        {
            instance = null;
            BeginOperation();
            T candidate = null;
            bool ownsCandidate = false;
            try
            {
                RemoveDeadInactive();
                if (_inactive.Count > 0)
                {
                    candidate = _inactive.Pop();
                    _owned[candidate] = true;
                }
                else
                {
                    if (_owned.Count >= Capacity)
                    {
                        RemoveDeadLoans();
                        if (_owned.Count >= Capacity)
                        {
                            return false;
                        }
                    }
                    candidate = _createInstance();
                    if (candidate == null || _owned.ContainsKey(candidate))
                    {
                        throw new InvalidOperationException("Factory must return a new non-null instance owned exclusively by this pool.");
                    }
                    _owned.Add(candidate, true);
                    ownsCandidate = true;
                    if (_isAlive != null && !_isAlive(candidate))
                    {
                        throw new InvalidOperationException("Factory must return a live instance.");
                    }
                }
                ownsCandidate = true;
                _onRent?.Invoke(candidate);
                instance = candidate;
                return true;
            }
            catch (Exception failure)
            {
                if (ownsCandidate)
                {
                    DiscardAfterFailure(candidate, failure);
                }
                throw;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>Resets an instance borrowed from this pool, then stores it for reuse.</summary>
        /// <param name="instance">Non-null instance currently borrowed from this pool.</param>
        /// <exception cref="ArgumentNullException">The instance is null.</exception>
        /// <exception cref="ArgumentException">The instance is not owned by this pool.</exception>
        /// <exception cref="InvalidOperationException">Duplicate return or mutation reentry.</exception>
        /// <exception cref="ObjectDisposedException">The pool is closed.</exception>
        /// <exception cref="AggregateException">Both reset and failed-instance cleanup throw.</exception>
        /// <remarks>Reset errors discard only the affected instance and propagate; foreign instances remain unchanged.</remarks>
        public void Return(T instance)
        {
            BeginOperation();
            try
            {
                if (instance == null)
                {
                    throw new ArgumentNullException(nameof(instance));
                }
                if (!_owned.TryGetValue(instance, out bool isRented))
                {
                    throw new ArgumentException("Instance is not owned by this pool.", nameof(instance));
                }
                if (!isRented)
                {
                    throw new InvalidOperationException("Instance has already been returned.");
                }

                try
                {
                    _onReturn?.Invoke(instance);
                    _inactive.Push(instance);
                    _owned[instance] = false;
                }
                catch (Exception failure)
                {
                    DiscardAfterFailure(instance, failure);
                    throw;
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>Permanently closes this pool and attempts cleanup of all borrowed and inactive instances.</summary>
        /// <exception cref="InvalidOperationException">Called during factory, preparation, reset, or discard cleanup.</exception>
        /// <exception cref="AggregateException">One or more cleanup callbacks failed; all callbacks were attempted.</exception>
        /// <remarks>Counts become zero before cleanup. Repeated disposal is ignored. Return callbacks are not invoked.</remarks>
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
            var instances = new List<T>(_owned.Keys);
            _owned.Clear();
            _inactive.Clear();
            List<Exception> failures = null;
            foreach (var instance in instances)
            {
                try
                {
                    _onDestroy?.Invoke(instance);
                }
                catch (Exception failure)
                {
                    if (failures == null)
                    {
                        failures = new List<Exception>();
                    }
                    failures.Add(failure);
                }
            }
            if (failures != null)
            {
                throw new AggregateException("Pool cleanup failed for one or more instances.", failures);
            }
        }

        private void RemoveDeadInactive()
        {
            if (_isAlive == null)
            {
                return;
            }
            List<Exception> failures = null;
            try
            {
                while (_inactive.Count > 0)
                {
                    // Evaluate before popping: a throwing or reentrant predicate preserves this candidate.
                    var candidate = _inactive.Peek();
                    if (_isAlive(candidate))
                    {
                        break;
                    }
                    _inactive.Pop();
                    _owned.Remove(candidate);
                    CleanupDeadInstance(candidate, ref failures);
                }
            }
            catch (Exception failure)
            {
                if (failures == null)
                {
                    throw;
                }
                failures.Add(failure);
                throw new AggregateException("Pool liveness check and cleanup failed.", failures);
            }
            if (failures != null)
            {
                throw new AggregateException("Pool dead-instance cleanup failed.", failures);
            }
        }

        private void RemoveDeadLoans()
        {
            if (_isAlive == null)
            {
                return;
            }
            _deadInstances.Clear();
            try
            {
                // Collect first so predicate failure leaves ownership unchanged. Concrete enumeration does not box.
                foreach (var entry in _owned)
                {
                    if (!_isAlive(entry.Key))
                    {
                        _deadInstances.Add(entry.Key);
                    }
                }
                for (int i = 0; i < _deadInstances.Count; i++)
                {
                    _owned.Remove(_deadInstances[i]);
                }
                List<Exception> failures = null;
                for (int i = 0; i < _deadInstances.Count; i++)
                {
                    CleanupDeadInstance(_deadInstances[i], ref failures);
                }
                if (failures != null)
                {
                    throw new AggregateException("Pool dead-instance cleanup failed.", failures);
                }
            }
            finally
            {
                _deadInstances.Clear();
            }
        }

        private void CleanupDeadInstance(T instance, ref List<Exception> failures)
        {
            try
            {
                _onDestroy?.Invoke(instance);
            }
            catch (Exception failure)
            {
                if (failures == null)
                {
                    failures = new List<Exception>();
                }
                failures.Add(failure);
            }
        }

        private void BeginOperation()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
            if (_isBusy)
            {
                throw new InvalidOperationException("Pool mutation is already running.");
            }
            _isBusy = true;
        }

        private void DiscardAfterFailure(T instance, Exception failure)
        {
            _owned.Remove(instance);
            try
            {
                _onDestroy?.Invoke(instance);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Pool callback and cleanup both failed.", failure, cleanupFailure);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public bool Equals(T left, T right) => ReferenceEquals(left, right);
            public int GetHashCode(T instance) => RuntimeHelpers.GetHashCode(instance);
        }
    }
}
