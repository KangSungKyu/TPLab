# DataTables

Module: DataTables
Namespace: MyLab.Core.DataTables
Assembly: MyLab.Core
SourceRevision: 9305b5dd0f730636f431fd5d19a1c9102fdc3bed
SourcePath: [Core/DataTables](../../../Assets/MyLab/Core/DataTables)
HumanContract: [DataTables](../../api/DataTables.md), [상세 계약](../../DATA_TABLE_MANAGER.md)
ImplementationStatus: Implemented
ValidationStatus: Partial
Evidence: [Unity6000.3.18f1/Windows Mono 회귀·consumer](../../validation/input-system/p4/README.md)

Symbol / Signature / Constraints: public 선언 발췌. public constructor 없는 결과/controller는 owner로부터 받는다.

```csharp
// IDataRow / DataRow
uint Id { get; } // IDataRow
uint Id { get; set; } // DataRow, [Name("idx")]
// IDataTable<TRow>, CsvDataTable<TRow>: where TRow : class, IDataRow
IReadOnlyDictionary<uint, TRow> Rows { get; }
int Count { get; }
bool TryGet(uint key, out TRow row);
// DataTableManager (public implicit parameterless constructor)
DataTableSnapshot Snapshot { get; } // private setter
bool IsLoading { get; }
bool IsDisposed { get; } // private setter
void RegisterIdxRouter(IIdxRouter router);
void RegisterTable<TRow, TTable>(uint dataType, string name,
    Func<CancellationToken, UniTask<string>> readCsvAsync, Func<TTable> createTable)
    where TRow : class, IDataRow where TTable : CsvDataTable<TRow>;
void BindTable<TService>(string name);
void RegisterForeignKey<TSource, TTarget>(string sourceTable, string column,
    Func<TSource, uint?> getForeignKey, uint targetDataType, bool required)
    where TSource : class, IDataRow where TTarget : class, IDataRow;
void Register<TKey, TRow>(string name, string[] requiredHeaders,
    Func<CancellationToken, UniTask<string>> readCsvAsync, Func<CsvReader, TRow> readRow,
    Func<TRow, TKey> keySelector, Action<TRow> validateRow = null);
void AddValidator(Action<DataTableSnapshot> validate);
UniTask<DataTableSnapshot> LoadAsync(CancellationToken cancellationToken = default);
TRow Get<TRow>(uint idx) where TRow : class, IDataRow;
bool TryGet<TRow>(uint idx, out TRow row) where TRow : class, IDataRow;
void Dispose();
// DataTableSnapshot (constructor internal)
int Count { get; }
TRow Get<TRow>(uint idx) where TRow : class, IDataRow;
bool TryGet<TRow>(uint idx, out TRow row) where TRow : class, IDataRow;
TService GetTable<TService>(string name);
TService GetTable<TService>(uint idx);
IReadOnlyDictionary<TKey, TRow> GetTable<TKey, TRow>(string name);
// IIdxRouter / IIdxCodec<TParts> : IIdxRouter
bool TryGetDataType(uint idx, out uint dataType);
uint Generate(TParts parts);
bool TryExtract(uint idx, out TParts parts);
// IdxParts
IdxParts(uint dataType, uint localIdx);
uint DataType { get; }
uint LocalIdx { get; }
// DecimalIdxCodec : IIdxCodec<IdxParts>
DecimalIdxCodec(uint stride);
uint Stride { get; }
uint Generate(IdxParts parts);
bool TryExtract(uint idx, out IdxParts parts);
bool TryGetDataType(uint idx, out uint dataType);
// DataTableCsvValidator
static void ValidateIdx(uint idx, uint dataType, IIdxRouter router);
static ReadOnlyDictionary<TKey, TRow> Read<TKey, TRow>(string text, string[] headers,
    Func<CsvReader, TRow> readRow, Func<TRow, TKey> keySelector,
    Action<TRow> validateRow, CancellationToken token,
    Action<CsvContext> configure = null, Action<CsvReader> validateHeader = null);
```

Inputs: 표준 CSV idx는 최종 PK, nonzero kind/idx, exact DTO class. 수동 PK는 null/중복 금지이고 zero는 유효. 필수 manual headers는 nonempty/unique/ordinal. 기본 codec은 stride>1, kind/local>0, local<stride, checked uint 연산. router는 immutable/side-effect-free.
Outputs: 전체 검증 성공의 read-only container snapshot; rows/table은 borrowed class references. 이전 snapshot은 reload/disposal 이후 유지된다.
Errors: Get invalid format=ArgumentException; missing kind/PK=KeyNotFoundException; wrong exact DTO/unready/router missing=InvalidOperationException. TryGet key/type miss=false/null, state/router error propagates. closed manager=ObjectDisposedException. CSV/source 실패=InvalidDataException(inner 보존); whole validator 오류 그대로. codec invalid parts=ArgumentOutOfRangeException, overflow=OverflowException.
Ownership: manager는 managed publication; source delegate는 I/O/handle. table factory는 attempt마다 fresh. consumers는 DTO를 수정하거나 factory instance를 재사용하지 않는다.
Lifecycle: register before first load; first valid load start freezes registry. Snapshot null before success/after Dispose. Reload failure preserves last snapshot. CsvDataTable Rows/Count/TryGet before attach throws InvalidOperationException; use published binding.
Threading: manager Unity main thread. snapshot은 불변 DTO/router를 공급하지 않으면 worker 사용 안전을 일반화하지 않는다.
Concurrency: overlapping loads share attempt; later call reload. validation callbacks must not await owner load or introduce external side effects.
Cancellation: caller token cancels only its waiter; Dispose cancels owner and prohibits late publication. source independently drains native I/O.
FailureCleanup: failed candidate unpublished; source owns external cleanup; retained old snapshot is not invalidated.
Configuration: names ordinal unique, dataType unique/nonzero, one router. binding concrete/default IDataTable plus explicit implemented interface only. FK nullable absence allowed iff !required, zero invalid, same candidate exact target checked.
ExtensionPoints: project custom parts/codec (optional localType); protected ConfigureMapping(CsvContext), ValidateRow(TRow), ValidateTable(IReadOnlyDictionary<uint,TRow>); AddValidator for complex relations.
RequiredSequence: new manager → router/tables/bindings/FK/validators → await LoadAsync → Get<T>/TryGet<T> or snapshot.GetTable → Dispose.
ForbiddenUsage: dynamic Get; infer return type from uint; invent builtin Text/Resource; regenerate CSV idx at load; use internal StandardTable/object fields; retain/advance/dispose CsvReader in readRow; treat manual table as idx-routed.
Example: [human excerpt](../../api/DataTables.md) is NotRun with project placeholders. [ConsumerSmoke.cs](../../../tools/core-consumer/templates/ConsumerSmoke.cs) has recorded consumer execution.
Compatibility: existing arbitrary-PK Register/GetTable path retained; concrete Text/Resource moved to templates, not core defaults.
Limitations: no file storage, ID reservation, row mutation system, automatic project type discovery, JSON-row loader or dynamic lookup.
