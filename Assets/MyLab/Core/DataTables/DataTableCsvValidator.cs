using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyLab.Core.DataTables
{
    /// <summary>Shared runtime/Editor CSV and standard PK checks. No I/O or snapshot publication.</summary>
    public static class DataTableCsvValidator
    {
        /// <summary>Checks one full standard PK using the project router. Manual arbitrary keys need not use this check.</summary>
        /// <exception cref="ArgumentNullException">Router is absent.</exception>
        /// <exception cref="InvalidDataException">PK is zero, invalid or belongs to another kind.</exception>
        public static void ValidateIdx(uint idx, uint dataType, IIdxRouter router)
        {
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            if (dataType == 0)
                throw new ArgumentOutOfRangeException(nameof(dataType));
            if (idx == 0 || !router.TryGetDataType(idx, out uint kind) || kind != dataType)
            {
                throw new InvalidDataException($"PK '{idx}' does not match kind '{dataType}'.");
            }
        }

        /// <summary>Reads and validates one candidate with invariant comma-separated CSV and unique non-null keys.</summary>
        /// <param name="text">Complete CSV, including a required header. Header-only tables are valid.</param>
        /// <param name="headers">Required ordinal column names. DTO callers use validateHeader instead.</param>
        /// <param name="readRow">Reads current fields; must not advance, retain or dispose the reader.</param>
        /// <param name="keySelector">Returns a non-null key. Zero is allowed unless validateRow rejects it.</param>
        /// <param name="validateRow">Optional side-effect-free row validation.</param>
        /// <param name="token">Cancels parsing; cancellation propagates.</param>
        /// <param name="configure">Optional mapping configuration before the header is read.</param>
        /// <param name="validateHeader">Optional DTO mapping header check.</param>
        /// <returns>A new read-only dictionary. Rows are borrowed, not cloned.</returns>
        /// <exception cref="InvalidDataException">Malformed CSV, missing column, failed conversion/validation or duplicate key.</exception>
        public static ReadOnlyDictionary<TKey, TRow> Read<TKey, TRow>(string text, string[] headers, Func<CsvReader, TRow> readRow,
            Func<TRow, TKey> keySelector, Action<TRow> validateRow, CancellationToken token,
            Action<CsvContext> configure = null, Action<CsvReader> validateHeader = null)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (readRow == null)
                throw new ArgumentNullException(nameof(readRow));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (text == null)
            {
                throw new InvalidDataException("Source returned null CSV text.");
            }
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                DetectColumnCountChanges = true
            };
            using (var reader = new StringReader(text))
            using (var csv = new CsvReader(reader, configuration))
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    configure?.Invoke(csv.Context);
                    if (!csv.Read())
                    {
                        throw new InvalidDataException("CSV header is required.");
                    }
                    csv.ReadHeader();
                    var actual = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var header in csv.HeaderRecord)
                    {
                        if (string.IsNullOrWhiteSpace(header) || !actual.Add(header))
                        {
                            throw new InvalidDataException("CSV headers must be named and unique.");
                        }
                    }
                    foreach (var header in headers)
                    {
                        if (!actual.Contains(header))
                        {
                            throw new InvalidDataException($"Required header '{header}' is missing.");
                        }
                    }
                    validateHeader?.Invoke(csv);
                    var rows = new Dictionary<TKey, TRow>();
                    while (csv.Read())
                    {
                        token.ThrowIfCancellationRequested();
                        // CsvHelper validates quoted fields lazily; validate even columns the project parser ignores.
                        for (int column = 0; column < csv.Parser.Count; ++column)
                        {
                            csv.GetField(column);
                        }
                        var row = readRow(csv);
                        if (row is null)
                        {
                            throw new InvalidDataException("Null rows are not supported.");
                        }
                        validateRow?.Invoke(row);
                        var key = keySelector(row);
                        if (key is null || !rows.TryAdd(key, row))
                        {
                            throw new InvalidDataException($"Null or duplicate key '{key}'.");
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    return new ReadOnlyDictionary<TKey, TRow>(rows);
                }
                catch (Exception error) when (!(error is OperationCanceledException))
                {
                    throw new InvalidDataException($"CSV row {csv.Parser.Row}: {error.Message}", error);
                }
            }
        }

    }
}
