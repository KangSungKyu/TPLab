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

        internal DataTableSnapshot(Dictionary<string, object> tables)
            => _tables = new ReadOnlyDictionary<string, object>(tables);

        /// <summary>Number of registered tables in this snapshot, including header-only tables.</summary>
        public int Count => _tables.Count;

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
            if (!(table is ReadOnlyDictionary<TKey, TRow> rows))
            {
                throw new InvalidOperationException($"Table '{name}' does not match the requested key and row types.");
            }
            return rows;
        }
    }
}
