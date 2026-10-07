using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CsvHelper;
using Cysharp.Threading.Tasks;

namespace TPLab.Core.DataTables
{
    /// <summary>Registers project CSV schemas and atomically publishes all tables after validation. Methods require Unity's main thread.</summary>
    /// <remarks>Source delegates own I/O and resource handles. This owner stores only managed snapshots, without game IDs or Singleton access.</remarks>
    public sealed class DataTableManager : IDisposable
    {
        private sealed class Registration
        {
            internal string Name;
            internal Func<CancellationToken, UniTask<object>> ReadAsync;
            internal uint DataType;
            internal Type RowType;
            internal Type TableType;
            internal HashSet<Type> Bindings;
        }

        private readonly List<Registration> _registrations = new List<Registration>();
        private readonly List<Action<DataTableSnapshot>> _validators = new List<Action<DataTableSnapshot>>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private UniTaskCompletionSource<DataTableSnapshot> _loading;
        private bool _frozen;
        private IIdxRouter _router;

        /// <summary>Registers the immutable project router before the first valid load. A second router is rejected.</summary>
        /// <exception cref="ArgumentNullException">Router is null.</exception>
        /// <exception cref="InvalidOperationException">Already registered, frozen, or called on a worker.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        public void RegisterIdxRouter(IIdxRouter router)
        {
            EnsureConfigurable();
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }
            if (_router != null)
            {
                throw new InvalidOperationException("An idx router is already registered.");
            }
            _router = router;
        }

        /// <summary>Registers a standard DTO table with a unique nonzero kind, name, source and fresh-per-attempt factory.</summary>
        /// <param name="dataType">Project-assigned nonzero kind, unique in this manager.</param>
        /// <param name="name">Unique ordinal table name, also used for manual dictionary lookup.</param>
        /// <param name="readCsvAsync">Owner-token CSV source. Owns external I/O and handles; DTO parsing runs on the main thread.</param>
        /// <param name="createTable">Returns a fresh managed table for every attempt, including retries after failure.</param>
        /// <exception cref="ArgumentException">Name or kind is invalid/duplicate.</exception>
        /// <exception cref="ArgumentNullException">Source or factory is null.</exception>
        /// <exception cref="InvalidOperationException">Configuration is frozen or called on a worker.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        public void RegisterTable<TRow, TTable>(uint dataType, string name,
            Func<CancellationToken, UniTask<string>> readCsvAsync, Func<TTable> createTable)
            where TRow : class, IDataRow where TTable : CsvDataTable<TRow>
        {
            EnsureConfigurable();
            ValidateName(name);
            if (dataType == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dataType));
            }
            if (readCsvAsync == null)
            {
                throw new ArgumentNullException(nameof(readCsvAsync));
            }
            if (createTable == null)
            {
                throw new ArgumentNullException(nameof(createTable));
            }
            foreach (var existing in _registrations)
            {
                if (existing.DataType == dataType)
                {
                    throw new ArgumentException($"Kind '{dataType}' is already registered.", nameof(dataType));
                }
            }
            var registration = new Registration
            {
                Name = name,
                DataType = dataType,
                RowType = typeof(TRow),
                TableType = typeof(TTable),
                Bindings = new HashSet<Type> { typeof(TTable), typeof(IDataTable<TRow>) }
            };
            registration.ReadAsync = async token =>
            {
                var table = createTable();
                if (table == null)
                {
                    throw new InvalidDataException("Table factory returned null.");
                }
                table.Claim();
                string text = await ReadSourceAsync(readCsvAsync, token);
                var rows = DataTableCsvValidator.Read<uint, TRow>(text, Array.Empty<string>(), csv => csv.GetRecord<TRow>(), row => row.Id,
                    row =>
                    {
                        DataTableCsvValidator.ValidateIdx(row.Id, dataType, _router);
                        table.Validate(row);
                    }, token, table.Configure, csv => csv.ValidateHeader<TRow>());
                table.Attach(rows);
                return new DataTableSnapshot.StandardTable(dataType, typeof(TRow), table, rows, registration.Bindings);
            };
            _registrations.Add(registration);
        }

        /// <summary>Gets a borrowed row from the current snapshot using full idx routing and exact DTO type validation.</summary>
        /// <exception cref="InvalidOperationException">Not ready, no router, DTO mismatch, or worker call.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        /// <exception cref="ArgumentException">Idx format is invalid.</exception>
        /// <exception cref="KeyNotFoundException">Kind or row is missing.</exception>
        public TRow Get<TRow>(uint idx) where TRow : class, IDataRow => GetSnapshot().Get<TRow>(idx);

        /// <summary>Returns false/null for a key/type miss. State errors and router implementation errors propagate.</summary>
        /// <exception cref="InvalidOperationException">Not ready, no router, or worker call.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        public bool TryGet<TRow>(uint idx, out TRow row) where TRow : class, IDataRow => GetSnapshot().TryGet(idx, out row);

        private DataTableSnapshot GetSnapshot()
        {
            EnsureOpen();
            return Snapshot ?? throw new InvalidOperationException("Load a complete snapshot before querying rows.");
        }

        /// <summary>Explicitly exposes an additional interface implemented by the declared table type before loading.</summary>
        /// <param name="name">Already registered standard table name.</param>
        /// <exception cref="ArgumentException">Service is not an implemented interface, binding is duplicate, or name is blank.</exception>
        /// <exception cref="KeyNotFoundException">Name is not registered.</exception>
        /// <exception cref="InvalidOperationException">Not a standard table, frozen, or worker call.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        public void BindTable<TService>(string name)
        {
            EnsureConfigurable();
            var registration = GetStandardRegistration(name);
            var service = typeof(TService);
            if (!service.IsInterface || !service.IsAssignableFrom(registration.TableType))
            {
                throw new ArgumentException("Binding must be an interface implemented by the declared table type.");
            }
            if (!registration.Bindings.Add(service))
            {
                throw new ArgumentException("This table contract is already bound.");
            }
        }

        /// <summary>Registers a single nullable FK checked against the same complete candidate. Source and target must already be registered.</summary>
        /// <param name="sourceTable">Standard source table name whose exact DTO is TSource.</param>
        /// <param name="column">Diagnostic CSV column name; fields are not discovered automatically.</param>
        /// <param name="getForeignKey">Side-effect-free selector of the full target idx. Null represents absence; zero is invalid.</param>
        /// <param name="targetDataType">Registered standard target kind whose exact DTO is TTarget.</param>
        /// <param name="required">Rejects null when true.</param>
        /// <exception cref="ArgumentException">Column/name/kind is invalid.</exception>
        /// <exception cref="ArgumentNullException">Selector is null.</exception>
        /// <exception cref="KeyNotFoundException">Source or target is not registered.</exception>
        /// <exception cref="InvalidOperationException">Exact DTO differs, manual table, frozen, or worker call.</exception>
        /// <exception cref="ObjectDisposedException">Owner is closed.</exception>
        /// <remarks>Load failures include source PK, column, FK, expected kind and reason. Complex relations use AddValidator.</remarks>
        public void RegisterForeignKey<TSource, TTarget>(string sourceTable, string column,
            Func<TSource, uint?> getForeignKey, uint targetDataType, bool required)
            where TSource : class, IDataRow where TTarget : class, IDataRow
        {
            EnsureConfigurable();
            if (string.IsNullOrWhiteSpace(column))
            {
                throw new ArgumentException("An FK column name is required.", nameof(column));
            }
            if (getForeignKey == null)
            {
                throw new ArgumentNullException(nameof(getForeignKey));
            }
            if (targetDataType == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDataType));
            }
            var source = GetStandardRegistration(sourceTable);
            var target = _registrations.Find(registration => registration.DataType == targetDataType);
            if (target == null)
            {
                throw new KeyNotFoundException($"Unknown target kind '{targetDataType}'.");
            }
            if (source.RowType != typeof(TSource) || target.RowType != typeof(TTarget))
            {
                throw new InvalidOperationException("FK source and target require exact registered DTO types.");
            }
            _validators.Add(candidate =>
            {
                foreach (var row in candidate.GetTable<uint, TSource>(sourceTable).Values)
                {
                    uint? fk = getForeignKey(row);
                    string reason = fk.HasValue ? candidate.GetReferenceError<TTarget>(fk.Value, targetDataType) :
                        required ? "Required reference is null." : null;
                    if (reason != null)
                    {
                        throw new InvalidDataException($"Table '{sourceTable}' PK '{row.Id}', column '{column}', FK '{fk?.ToString() ?? "null"}', expected kind '{targetDataType}': {reason}");
                    }
                }
            });
        }

        private Registration GetStandardRegistration(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A table name is required.", nameof(name));
            }
            var registration = _registrations.Find(table => table.Name == name);
            if (registration == null)
            {
                throw new KeyNotFoundException($"Unknown table '{name}'.");
            }
            if (registration.DataType == 0)
            {
                throw new InvalidOperationException("A standard table is required.");
            }
            return registration;
        }

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
            ValidateName(name);
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
            _registrations.Add(new Registration
            {
                Name = name,
                ReadAsync = async token =>
                {
                    string text = await ReadSourceAsync(readCsvAsync, token);
                    return DataTableCsvValidator.Read(text, headers, readRow, keySelector, validateRow, token);
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
                if (_router == null && _registrations.Exists(registration => registration.DataType != 0))
                {
                    throw new InvalidOperationException("Standard tables require an idx router before loading.");
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
                _router = null;
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
                var candidate = new DataTableSnapshot(tables, _router);
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

        private static async UniTask<string> ReadSourceAsync(Func<CancellationToken, UniTask<string>> source, CancellationToken token)
        {
            string text;
            try
            {
                text = await source(token).AttachExternalCancellation(token);
            }
            finally
            {
                // Sources may finish on a worker; both CSV paths and publication require Unity's main thread.
                await UniTask.SwitchToMainThread();
            }
            token.ThrowIfCancellationRequested();
            return text;
        }

        private void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A table name is required.", nameof(name));
            }
            if (_registrations.Exists(registration => registration.Name == name))
            {
                throw new ArgumentException($"Table '{name}' is already registered.", nameof(name));
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
