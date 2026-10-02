using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;
using Cysharp.Threading.Tasks;

namespace MyLab.Core.DataTables
{
    /// <summary>Registers project CSV schemas and atomically publishes all tables after validation. Methods require Unity's main thread.</summary>
    /// <remarks>Source delegates own I/O and resource handles. This owner stores only managed snapshots, without game IDs or Singleton access.</remarks>
    public sealed class DataTableManager : IDisposable
    {
        private sealed class Registration
        {
            internal string Name;
            internal Func<CancellationToken, UniTask<object>> ReadAsync;
        }

        private readonly List<Registration> _registrations = new List<Registration>();
        private readonly List<Action<DataTableSnapshot>> _validators = new List<Action<DataTableSnapshot>>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private UniTaskCompletionSource<DataTableSnapshot> _loading;
        private bool _frozen;

        /// <summary>Last successful complete snapshot, or null before first success and after disposal. Reload failure preserves it.</summary>
        public DataTableSnapshot Snapshot { get; private set; }
        /// <summary>True while a shared load attempt is preparing or validating its candidate.</summary>
        public bool IsLoading => _loading != null;
        /// <summary>True after permanent termination. No further registration, load or publication is permitted.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Registers a typed CSV source, required headers and project row/key rules before the first load.</summary>
        /// <param name="name">Unique nonempty ordinal name, independent of filename and IDs.</param>
        /// <param name="requiredHeaders">Nonempty, unique, case-sensitive column names; copied. Extra uniquely named columns are allowed.</param>
        /// <param name="readCsvAsync">Returns CSV text and owns any external handles. Receives the owner's cancellation, not an individual waiter's.</param>
        /// <param name="readRow">Reads the current CsvReader record. Must not advance, dispose or retain the reader.</param>
        /// <param name="keySelector">Returns a non-null unique key. Default key equality is used; zero has no special meaning.</param>
        /// <param name="validateRow">Optional project validation; throw to reject. Parsing and validation callbacks must avoid external side effects.</param>
        /// <exception cref="ArgumentException">Name/headers are invalid or the name is already registered.</exception>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        /// <exception cref="InvalidOperationException">Registration is frozen or the thread is invalid.</exception>
        /// <exception cref="ObjectDisposedException">Owner is terminated.</exception>
        public void Register<TKey, TRow>(string name, string[] requiredHeaders,
            Func<CancellationToken, UniTask<string>> readCsvAsync, Func<CsvReader, TRow> readRow,
            Func<TRow, TKey> keySelector, Action<TRow> validateRow = null)
        {
            EnsureConfigurable();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A table name is required.", nameof(name));
            }
            if (requiredHeaders == null)
            {
                throw new ArgumentNullException(nameof(requiredHeaders));
            }
            if (readCsvAsync == null)
            {
                throw new ArgumentNullException(nameof(readCsvAsync));
            }
            if (readRow == null)
            {
                throw new ArgumentNullException(nameof(readRow));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            var headers = (string[])requiredHeaders.Clone();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (headers.Length == 0)
            {
                throw new ArgumentException("Required headers cannot be empty.", nameof(requiredHeaders));
            }
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header) || !unique.Add(header))
                {
                    throw new ArgumentException("Required headers must be named and unique.", nameof(requiredHeaders));
                }
            }
            foreach (var existing in _registrations)
            {
                if (existing.Name == name)
                {
                    throw new ArgumentException($"Table '{name}' is already registered.", nameof(name));
                }
            }
            _registrations.Add(new Registration
            {
                Name = name,
                ReadAsync = async token =>
                {
                    string text;
                    try
                    {
                        text = await readCsvAsync(token).AttachExternalCancellation(token);
                    }
                    finally
                    {
                        // Sources may finish on a worker; all parsing, validation and publication belong to Unity's main thread.
                        await UniTask.SwitchToMainThread();
                    }
                    token.ThrowIfCancellationRequested();
                    return ReadTable(text, headers, readRow, keySelector, validateRow, token);
                }
            });
        }

        /// <summary>Adds a project validator for the complete candidate, such as cross-table foreign keys. Registration freezes at first load.</summary>
        /// <param name="validate">Synchronous, side-effect-free validation. Throw to prevent publication. Do not await this owner's load from here.</param>
        /// <exception cref="ArgumentNullException">Validator is null.</exception>
        /// <exception cref="InvalidOperationException">Registration is frozen or called outside Unity's main thread.</exception>
        /// <exception cref="ObjectDisposedException">Owner is terminated.</exception>
        public void AddValidator(Action<DataTableSnapshot> validate)
        {
            EnsureConfigurable();
            _validators.Add(validate ?? throw new ArgumentNullException(nameof(validate)));
        }

        /// <summary>Loads all registrations in order and publishes one snapshot only after every row and cross-table validator succeeds.</summary>
        /// <param name="cancellationToken">Cancels only this caller's wait; other waiters and owner work continue.</param>
        /// <returns>The new complete snapshot. Overlapping requests share the attempt; a later call starts an explicit reload.</returns>
        /// <exception cref="InvalidOperationException">No tables registered or called on a background thread.</exception>
        /// <exception cref="InvalidDataException">CSV/source/key/row error, with table context and the original error as InnerException.</exception>
        /// <exception cref="OperationCanceledException">Caller or owner cancellation, or cancellation reported by a source.</exception>
        /// <exception cref="ObjectDisposedException">Owner is terminated.</exception>
        /// <remarks>Project cross-table validator errors propagate. Any failure preserves the previous snapshot and permits retry.</remarks>
        public UniTask<DataTableSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            EnsureOpen();
            cancellationToken.ThrowIfCancellationRequested();
            var operation = _loading;
            if (operation == null)
            {
                if (_registrations.Count == 0)
                {
                    throw new InvalidOperationException("Register at least one table before loading.");
                }
                _frozen = true;
                operation = new UniTaskCompletionSource<DataTableSnapshot>();
                _loading = operation;
                LoadAndPublishAsync(operation, _lifetime.Token).Forget();
            }
            return operation.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>Permanently closes the owner, cancels its managed wait and clears its snapshot. Repeated calls are ignored.</summary>
        /// <remarks>External I/O may continue but cannot publish. Its source/resource owner must drain/release native work separately.</remarks>
        public void Dispose()
        {
            EnsureMainThread();
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            Snapshot = null;
            var operation = _loading;
            var token = _lifetime.Token;
            try
            {
                _lifetime.Cancel();
            }
            finally
            {
                _loading = null;
                operation?.TrySetCanceled(token);
                _registrations.Clear();
                _validators.Clear();
                _lifetime.Dispose();
            }
        }

        private async UniTask LoadAndPublishAsync(UniTaskCompletionSource<DataTableSnapshot> operation, CancellationToken token)
        {
            try
            {
                var tables = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var registration in _registrations)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        tables.Add(registration.Name, await registration.ReadAsync(token));
                    }
                    catch (Exception error) when (!(error is OperationCanceledException))
                    {
                        throw new InvalidDataException($"Table '{registration.Name}' failed to load: {error.Message}", error);
                    }
                }
                var candidate = new DataTableSnapshot(tables);
                foreach (var validate in _validators)
                {
                    token.ThrowIfCancellationRequested();
                    validate(candidate);
                    token.ThrowIfCancellationRequested();
                }
                token.ThrowIfCancellationRequested();
                Snapshot = candidate;
                _loading = null;
                operation.TrySetResult(candidate);
            }
            catch (OperationCanceledException error)
            {
                _loading = null;
                operation.TrySetCanceled(error.CancellationToken);
            }
            catch (Exception error)
            {
                _loading = null;
                operation.TrySetException(error);
            }
        }

        private static object ReadTable<TKey, TRow>(string text, string[] headers, Func<CsvReader, TRow> readRow,
            Func<TRow, TKey> keySelector, Action<TRow> validateRow, CancellationToken token)
        {
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

        private void EnsureConfigurable()
        {
            EnsureOpen();
            if (_frozen)
            {
                throw new InvalidOperationException("Registration is frozen after the first load starts.");
            }
        }

        private void EnsureOpen()
        {
            EnsureMainThread();
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(DataTableManager));
            }
        }

        private static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                throw new InvalidOperationException("DataTableManager requires Unity's main thread.");
            }
        }
    }
}
