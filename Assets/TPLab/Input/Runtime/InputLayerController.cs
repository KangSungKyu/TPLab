using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace TPLab.Core.Input
{
    /// <summary>Defines whether a layer preserves lower layers or blocks them.</summary>
    public enum InputLayerMode
    {
        Overlay,
        BlockLower
    }

    /// <summary>A detached observation of the layer state after a completed activation calculation.</summary>
    public sealed class InputLayerSnapshot
    {
        /// <summary>Creates an empty observation without holding any input ownership.</summary>
        public InputLayerSnapshot() : this(Array.Empty<Guid>(), Array.Empty<string>(), false)
        {
        }

        internal InputLayerSnapshot(IEnumerable<Guid> mapIds, IEnumerable<string> layerIds, bool allInputBlocked)
        {
            ActiveMapIds = Array.AsReadOnly(mapIds.ToArray());
            ActiveLayerIds = Array.AsReadOnly(layerIds.ToArray());
            AllInputBlocked = allInputBlocked;
        }

        /// <summary>Active map IDs owned by this scope.</summary>
        public IReadOnlyList<Guid> ActiveMapIds { get; } = Array.Empty<Guid>();

        /// <summary>Active layer IDs in highest-priority/latest-acquisition order.</summary>
        public IReadOnlyList<string> ActiveLayerIds { get; } = Array.Empty<string>();

        /// <summary>Whether any whole-scope blocking lease remains held.</summary>
        public bool AllInputBlocked { get; }
    }

    /// <summary>Owns activation policy for one scope; project UI retains its own focus and event ownership.</summary>
    public sealed class InputLayerController
    {
        private readonly Action _ensureActive;
        private readonly InputActionMap[] _maps;
        private readonly Dictionary<Guid, InputActionMap> _mapsById;
        private readonly Dictionary<string, Layer> _layers = new Dictionary<string, Layer>(StringComparer.Ordinal);
        private readonly HashSet<Guid> _registeredMaps = new HashSet<Guid>();
        private readonly List<Lease> _leases = new List<Lease>();
        private InputLayerSnapshot _snapshot = new InputLayerSnapshot(Array.Empty<Guid>(), Array.Empty<string>(), false);
        private long _sequence;
        private bool _registrationFrozen;
        private bool _applying;
        private bool _dirty;
        private bool _stopped;
        private Exception _fault;

        internal InputLayerController(InputActionAsset actions, Action ensureActive)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            _ensureActive = ensureActive ?? throw new ArgumentNullException(nameof(ensureActive));
            _maps = actions.actionMaps.ToArray();
            _mapsById = _maps.ToDictionary(map => map.id);
        }

        /// <summary>Registers immutable, disjoint map ownership before the first layer acquisition.</summary>
        public void RegisterLayer(string id, IEnumerable<Guid> mapIds, int priority, InputLayerMode mode)
        {
            EnsureActive();
            if (_registrationFrozen)
            {
                throw new InvalidOperationException("Layer registration is frozen after the first acquisition.");
            }
            ValidateId(id);
            if (mapIds == null)
            {
                throw new ArgumentNullException(nameof(mapIds));
            }
            if (mode != InputLayerMode.Overlay && mode != InputLayerMode.BlockLower)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var ids = mapIds.ToArray();
            var uniqueIds = new HashSet<Guid>();
            foreach (var mapId in ids)
            {
                if (mapId == Guid.Empty || !_mapsById.ContainsKey(mapId) || !uniqueIds.Add(mapId))
                {
                    throw new ArgumentException("Map IDs must be distinct and belong to this scope.", nameof(mapIds));
                }
            }

            // An enumerable may call back into the controller; commit only after all validation.
            EnsureActive();
            if (_registrationFrozen)
            {
                throw new InvalidOperationException("Layer registration is frozen after the first acquisition.");
            }
            if (_layers.ContainsKey(id))
            {
                throw new ArgumentException("The layer ID is already registered.", nameof(id));
            }
            if (ids.Any(mapId => _registeredMaps.Contains(mapId)))
            {
                throw new ArgumentException("Maps cannot be shared by different layers.", nameof(mapIds));
            }
            _layers.Add(id, new Layer(id, ids, priority, mode));
            _registeredMaps.UnionWith(ids);
        }

        /// <summary>Acquires an independent lease; dispose it on the Unity main thread when its owner exits.</summary>
        public IDisposable AcquireLayer(string id)
        {
            EnsureActive();
            ValidateId(id);
            if (!_layers.TryGetValue(id, out var layer))
            {
                throw new ArgumentException("The layer ID is not registered.", nameof(id));
            }
            var sequence = checked(_sequence + 1);
            var lease = new Lease(this, layer, sequence);
            _sequence = sequence;
            _registrationFrozen = true;
            _leases.Add(lease);
            Recalculate();
            EnsureActive();
            return lease;
        }

        /// <summary>Blocks every managed map until this independent lease is disposed.</summary>
        public IDisposable BlockAll()
        {
            EnsureActive();
            var lease = new Lease(this, null, 0);
            _leases.Add(lease);
            Recalculate();
            EnsureActive();
            return lease;
        }

        /// <summary>Returns a detached snapshot; no returned collection mutates controller state.</summary>
        public InputLayerSnapshot Snapshot
        {
            get
            {
                EnsureActive();
                return _snapshot;
            }
        }

        /// <summary>Reconciles map state after a project UI adapter has changed its native module lifecycle.</summary>
        public void Refresh()
        {
            EnsureActive();
            Recalculate();
        }

        /// <summary>Reports a failed activation/observer boundary that permanently blocks new work.</summary>
        public bool IsFaulted => _fault != null;

        /// <summary>The activation or observer failure, including any subsequent cleanup failures; null while healthy.</summary>
        public Exception Fault => _fault;

        /// <summary>Notifies completed state changes synchronously; subscribers own their unsubscription.</summary>
        public event Action<InputLayerSnapshot> Changed;

        internal void Stop()
        {
            if (_stopped)
            {
                return;
            }
            _stopped = true;
            InvalidateLeases();
            var failure = DisableAllMaps();
            if (failure == null)
            {
                return;
            }
            _fault = _fault == null ? failure : new AggregateException(_fault, failure);
            throw failure;
        }

        internal void MarkFault(Exception failure)
        {
            if (failure == null)
            {
                throw new ArgumentNullException(nameof(failure));
            }
            if (_fault != null)
            {
                return;
            }
            _fault = failure;
            InvalidateLeases();
            var cleanupFailure = DisableAllMaps();
            if (cleanupFailure == null)
            {
                return;
            }
            _fault = new AggregateException(failure, cleanupFailure);
            throw _fault;
        }

        private void EnsureActive()
        {
            _ensureActive();
            if (_stopped)
            {
                throw new ObjectDisposedException(nameof(InputLayerController));
            }
            if (_fault != null)
            {
                throw new InvalidOperationException("Input activation has faulted.", _fault);
            }
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A nonempty layer ID is required.", nameof(id));
            }
        }

        private void Release(Lease lease)
        {
            EnsureActive();
            lease.Owner = null;
            _leases.Remove(lease);
            Recalculate();
        }

        private void Recalculate()
        {
            _dirty = true;
            if (_applying)
            {
                return;
            }
            _applying = true;
            try
            {
                // ponytail: synchronous consumers must converge; bound the drain if hostile callbacks are supported.
                while (_dirty && !_stopped && _fault == null)
                {
                    _dirty = false;
                    var next = CalculateSnapshot();
                    var desiredMaps = new HashSet<Guid>(next.ActiveMapIds);
                    foreach (var map in _maps)
                    {
                        if (!desiredMaps.Contains(map.id))
                        {
                            map.Disable();
                        }
                        if (_stopped || _fault != null)
                        {
                            return;
                        }
                    }

                    // Disable can synchronously cancel actions whose consumers change leases.
                    if (_dirty)
                    {
                        continue;
                    }
                    foreach (var map in _maps)
                    {
                        if (desiredMaps.Contains(map.id))
                        {
                            map.Enable();
                        }
                        if (_stopped || _fault != null)
                        {
                            return;
                        }
                        if (_dirty)
                        {
                            break;
                        }
                    }
                    if (_dirty)
                    {
                        continue;
                    }
                    if (SameState(_snapshot, next))
                    {
                        continue;
                    }
                    _snapshot = next;
                    Changed?.Invoke(next);
                }
            }
            catch (Exception failure)
            {
                MarkFault(failure);
                throw;
            }
            finally
            {
                _applying = false;
            }
        }

        private InputLayerSnapshot CalculateSnapshot()
        {
            if (_leases.Any(lease => lease.Layer == null))
            {
                return new InputLayerSnapshot(Array.Empty<Guid>(), Array.Empty<string>(), true);
            }

            var ordered = _leases.OrderByDescending(lease => lease.Layer.Priority)
                .ThenByDescending(lease => lease.Sequence).ToArray();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var mapIds = new List<Guid>();
            var layerIds = new List<string>();
            foreach (var lease in ordered)
            {
                var layer = lease.Layer;
                if (!seen.Add(layer.Id))
                {
                    continue;
                }
                layerIds.Add(layer.Id);
                mapIds.AddRange(layer.MapIds);
                if (layer.Mode == InputLayerMode.BlockLower)
                {
                    break;
                }
            }
            return new InputLayerSnapshot(mapIds, layerIds, false);
        }

        private static bool SameState(InputLayerSnapshot left, InputLayerSnapshot right)
        {
            return left.AllInputBlocked == right.AllInputBlocked
                && left.ActiveMapIds.SequenceEqual(right.ActiveMapIds)
                && left.ActiveLayerIds.SequenceEqual(right.ActiveLayerIds);
        }

        private void InvalidateLeases()
        {
            foreach (var lease in _leases)
            {
                lease.Owner = null;
            }
            _leases.Clear();
            _dirty = false;
            Changed = null;
        }

        private Exception DisableAllMaps()
        {
            List<Exception> failures = null;
            foreach (var map in _maps)
            {
                try
                {
                    map.Disable();
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
            if (failures == null)
            {
                return null;
            }
            return failures.Count == 1 ? failures[0] : new AggregateException(failures);
        }

        private sealed class Layer
        {
            public readonly string Id;
            public readonly Guid[] MapIds;
            public readonly int Priority;
            public readonly InputLayerMode Mode;

            public Layer(string id, Guid[] mapIds, int priority, InputLayerMode mode)
            {
                Id = id;
                MapIds = mapIds;
                Priority = priority;
                Mode = mode;
            }
        }

        private sealed class Lease : IDisposable
        {
            public InputLayerController Owner;
            public readonly Layer Layer;
            public readonly long Sequence;

            public Lease(InputLayerController owner, Layer layer, long sequence)
            {
                Owner = owner;
                Layer = layer;
                Sequence = sequence;
            }

            public void Dispose()
            {
                Owner?.Release(this);
            }
        }
    }
}
