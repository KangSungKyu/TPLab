using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyLab.Core.DataTables
{
    /// <summary>A complete, validated set of tables. Dictionary containers cannot be changed by consumers.</summary>
    /// <remarks>Rows are not cloned. Supply immutable rows, or treat mutable DTOs as read-only. Retained snapshots survive reload and owner disposal.</remarks>
    public sealed class DataTableSnapshot
    {
        private readonly IReadOnlyDictionary<string, object> _tables;
        private readonly Dictionary<uint, StandardTable> _kinds = new Dictionary<uint, StandardTable>();
        private readonly IIdxRouter _router;

        internal sealed class StandardTable
        {
            internal readonly uint DataType;
            internal readonly Type RowType;
            internal readonly object Table;
            internal readonly object Rows;
            internal readonly HashSet<Type> Bindings;

            internal StandardTable(uint dataType, Type rowType, object table, object rows, HashSet<Type> bindings)
            {
                DataType = dataType;
                RowType = rowType;
                Table = table;
                Rows = rows;
                Bindings = new HashSet<Type>(bindings);
            }
        }

        internal DataTableSnapshot(Dictionary<string, object> tables, IIdxRouter router)
        {
            _tables = new ReadOnlyDictionary<string, object>(tables);
            _router = router;
            foreach (var value in tables.Values)
            {
                if (value is StandardTable table)
                {
                    _kinds.Add(table.DataType, table);
                }
            }
        }

        /// <summary>Number of registered tables in this snapshot, including header-only tables.</summary>
        public int Count => _tables.Count;

        /// <summary>Routes a complete idx and validates the exact registered DTO. Returns a borrowed row.</summary>
        /// <exception cref="ArgumentException">Idx format is invalid.</exception>
        /// <exception cref="KeyNotFoundException">Kind or row is missing.</exception>
        /// <exception cref="InvalidOperationException">Router is missing or exact DTO type differs.</exception>
        public TRow Get<TRow>(uint idx) where TRow : class, IDataRow
        {
            var table = Resolve(idx);
            if (table.RowType != typeof(TRow))
            {
                throw new InvalidOperationException("Idx table does not match the exact requested DTO type.");
            }
            return ((IReadOnlyDictionary<uint, TRow>)table.Rows)[idx];
        }

        /// <summary>Returns false/null for invalid idx, unknown kind, DTO mismatch or missing PK. Configuration errors propagate.</summary>
        /// <exception cref="InvalidOperationException">No idx router is configured.</exception>
        public bool TryGet<TRow>(uint idx, out TRow row) where TRow : class, IDataRow
        {
            row = null;
            EnsureRouter();
            return idx != 0 && _router.TryGetDataType(idx, out uint kind) && kind != 0 &&
                _kinds.TryGetValue(kind, out var table) && table.RowType == typeof(TRow) &&
                ((IReadOnlyDictionary<uint, TRow>)table.Rows).TryGetValue(idx, out row);
        }

        private StandardTable Resolve(uint idx)
        {
            EnsureRouter();
            if (idx == 0 || !_router.TryGetDataType(idx, out uint kind) || kind == 0)
            {
                throw new ArgumentException("Invalid complete idx format.", nameof(idx));
            }
            if (!_kinds.TryGetValue(kind, out var table))
            {
                throw new KeyNotFoundException($"Unknown table kind '{kind}'.");
            }
            return table;
        }

        private void EnsureRouter()
        {
            if (_router == null)
            {
                throw new InvalidOperationException("This snapshot has no idx router.");
            }
        }

        /// <summary>Gets the borrowed standard table via a registered concrete/default/additional interface binding.</summary>
        /// <exception cref="ArgumentException">Name is blank.</exception>
        /// <exception cref="KeyNotFoundException">Name is not registered.</exception>
        /// <exception cref="InvalidOperationException">Not a standard table or contract is not bound.</exception>
        public TService GetTable<TService>(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A table name is required.", nameof(name));
            }
            if (!_tables.TryGetValue(name, out var value))
            {
                throw new KeyNotFoundException($"Unknown table '{name}'.");
            }
            if (!(value is StandardTable table))
            {
                throw new InvalidOperationException("A standard table is required.");
            }
            return GetBinding<TService>(table);
        }

        /// <summary>Routes idx to the table binding. Does not require that the particular row exists.</summary>
        /// <exception cref="ArgumentException">Idx format is invalid.</exception>
        /// <exception cref="KeyNotFoundException">Kind is not registered.</exception>
        /// <exception cref="InvalidOperationException">No router or contract is not bound.</exception>
        public TService GetTable<TService>(uint idx) => GetBinding<TService>(Resolve(idx));

        private static TService GetBinding<TService>(StandardTable table)
        {
            if (!table.Bindings.Contains(typeof(TService)))
            {
                throw new InvalidOperationException("Table contract is not explicitly bound.");
            }
            return (TService)table.Table;
        }

        internal string GetReferenceError<TRow>(uint idx, uint expectedKind) where TRow : class, IDataRow
        {
            EnsureRouter();
            if (idx == 0 || !_router.TryGetDataType(idx, out uint kind) || kind == 0)
            {
                return "Invalid full idx format.";
            }
            if (!_kinds.TryGetValue(kind, out var table))
            {
                return $"Unregistered kind '{kind}'.";
            }
            if (kind != expectedKind)
            {
                return $"Wrong kind '{kind}'.";
            }
            if (table.RowType != typeof(TRow))
            {
                return "Wrong exact DTO type.";
            }
            return ((IReadOnlyDictionary<uint, TRow>)table.Rows).ContainsKey(idx) ? null : "Target PK is missing in this candidate.";
        }

        /// <summary>Gets the complete borrowed table using the exact registered key and row types.</summary>
        /// <param name="name">Ordinal registration name, independent of CSV filename and game IDs.</param>
        /// <returns>Read-only rows indexed by the registered key selector.</returns>
        /// <exception cref="ArgumentException">Name is empty or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">No such table is registered.</exception>
        /// <exception cref="InvalidOperationException">The requested generic types do not match.</exception>
        public IReadOnlyDictionary<TKey, TRow> GetTable<TKey, TRow>(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A table name is required.", nameof(name));
            }
            if (!_tables.TryGetValue(name, out var table))
            {
                throw new KeyNotFoundException($"Unknown table '{name}'.");
            }
            if (table is StandardTable standard)
            {
                table = standard.Rows;
            }
            if (!(table is ReadOnlyDictionary<TKey, TRow> rows))
            {
                throw new InvalidOperationException($"Table '{name}' does not match the requested key and row types.");
            }
            return rows;
        }
    }
}
