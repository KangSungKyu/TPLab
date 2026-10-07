using System;
using System.Collections.Generic;
using CsvHelper;

namespace TPLab.Core.DataTables
{
    /// <summary>Borrowed read-only standard rows. Published DTO objects must not be mutated.</summary>
    public interface IDataTable<TRow> where TRow : class, IDataRow
    {
        /// <summary>Rows in this generation, indexed by their complete encoded PK.</summary>
        IReadOnlyDictionary<uint, TRow> Rows { get; }
        /// <summary>Number of rows, including zero for header-only CSV.</summary>
        int Count { get; }
        /// <summary>Looks up the complete PK inside this table; false returns null.</summary>
        bool TryGet(uint key, out TRow row);
    }

    /// <summary>Project table base. Manager owns parsing and attaches one candidate; hooks must avoid external side effects.</summary>
    public class CsvDataTable<TRow> : IDataTable<TRow> where TRow : class, IDataRow
    {
        private bool _claimed;
        private IReadOnlyDictionary<uint, TRow> _rows;

        /// <summary>Borrowed rows. Throws InvalidOperationException before the manager attaches a complete candidate.</summary>
        public IReadOnlyDictionary<uint, TRow> Rows => _rows ?? throw new InvalidOperationException("Table rows are not prepared.");
        /// <inheritdoc />
        public int Count => Rows.Count;
        /// <inheritdoc />
        public bool TryGet(uint key, out TRow row) => Rows.TryGetValue(key, out row);

        /// <summary>Configures DTO ClassMap/converters before header reading. Reader lifetime belongs to the manager.</summary>
        protected virtual void ConfigureMapping(CsvContext context)
        {
        }
        /// <summary>Validates one row; throw to reject this candidate.</summary>
        protected virtual void ValidateRow(TRow row)
        {
        }
        /// <summary>Validates the complete local candidate before attachment. Cross-table rules belong to manager validators.</summary>
        protected virtual void ValidateTable(IReadOnlyDictionary<uint, TRow> rows)
        {
        }

        internal void Claim()
        {
            if (_claimed)
            {
                throw new InvalidOperationException("Each load attempt requires a fresh table instance, including failed attempts.");
            }
            _claimed = true;
        }

        internal void Configure(CsvContext context) => ConfigureMapping(context);
        internal void Validate(TRow row) => ValidateRow(row);
        internal void Attach(IReadOnlyDictionary<uint, TRow> rows)
        {
            ValidateTable(rows);
            _rows = rows;
        }
    }
}
