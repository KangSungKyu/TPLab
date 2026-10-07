# DataTables

CSV를 프로젝트 DTO로 읽고 모든 테이블·FK 검증이 성공한 snapshot만 공개한다. 기본 경로는 `uint idx`이며, 별도 PK를 쓰는 수동 경로도 제공한다. Text/Resource는 코어 기본 테이블이 아닌 [프로젝트 템플릿](../templates/data-tables/README.md)이다.

- Namespace: `MyLab.Core.DataTables`; Assembly: `MyLab.Core`
- SourceRevision: `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`; [소스](../../Assets/MyLab/Core/DataTables)
- ImplementationStatus: Implemented; ValidationStatus: Partial. Unity 6000.3.18f1 Windows Mono 소비 프로젝트와 전체 회귀는 [검증 자료](../validation/input-system/p4/README.md)에 있다. 다른 버전·IL2CPP는 미실행이다.
- 의존성: CsvHelper 33.1.0, UniTask. 상세 계약: [기본 로딩](../DATA_TABLE_MANAGER.md), [idx](../DATA_TABLE_IDX_DRAFT.md), [제네릭 조회](../DATA_TABLE_GENERIC_IMPLEMENTATION.md).

`IDataRow.Id`는 getter이며 `DataRow.Id`는 `[Name("idx")] uint` getter/setter다. CSV의 idx는 이미 생성된 최종 PK다. 로드 시 생성하지 않는다. 공개 후 DTO를 읽기 전용으로 취급한다. `CsvDataTable<TRow>`의 제약은 `where TRow : class, IDataRow`; `IDataTable<TRow>`는 `Rows`, `Count`, `TryGet(uint key, out TRow row)`를 제공한다. 테이블 상속 시 `ConfigureMapping(CsvContext)`, `ValidateRow(TRow)`, `ValidateTable(IReadOnlyDictionary<uint, TRow>)`를 override할 수 있다. 외부 부작용 없이 검증하며 factory는 실패한 재시도에도 새 인스턴스를 반환해야 한다.

manager가 데이터를 attach하기 전 `Rows/Count/TryGet` 조회는 `InvalidOperationException`이다. 성공한 snapshot에서 table binding을 받아 사용한다.

등록·조회 API는 다음과 같다. 아래는 선언 발췌이며 메서드 본문은 생략했다.

```csharp
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
```

`DataTableManager`는 인수 없이 생성한다. `Snapshot`은 첫 성공 전과 종료 후 null, `IsLoading`은 공유 로드 중, `IsDisposed`는 종료 후 true다. 등록명은 공백 불가·ordinal 기준 고유, 표준 dataType은 nonzero·고유하다. router는 하나이며 표준 로드에 필수다. 첫 로드 시작 시 등록을 고정한다. 추가 binding은 등록 table이 구현하는 interface로 제한된다. FK는 먼저 등록된 정확한 source/target DTO, 완전한 target idx와 nullable selector를 사용한다. optional null은 참조 없음이고 0은 무효다.

`Get<TRow>`는 router로 종류를 확인하고 정확한 DTO 타입을 대조한다. 무효 idx는 `ArgumentException`, 미등록 종류/PK는 `KeyNotFoundException`, 타입 불일치/미준비/router 누락은 `InvalidOperationException`이다. `TryGet`은 키·타입 불일치에 false/null을 반환하지만 준비·종료 상태와 router 구현 오류는 숨기지 않는다. manager 종료 후에는 `ObjectDisposedException`이다.

수동 `Register<TKey, TRow>`는 `IDataRow` 제약 없이 프로젝트가 key selector를 제공한다. 필수 header는 비어 있지 않은 중복 없는 이름 목록이며 대소문자를 구분한다. key는 null·중복 금지, 0은 프로젝트 validator가 금지하지 않는 한 유효하다. 반환되는 테이블은 `snapshot.GetTable<TKey, TRow>(string name)`으로 조회한다. 이 경로를 idx 기반 `Get<TRow>`에 혼합하지 않는다.

Snapshot API:

```csharp
int Count { get; }
TRow Get<TRow>(uint idx) where TRow : class, IDataRow;
bool TryGet<TRow>(uint idx, out TRow row) where TRow : class, IDataRow;
TService GetTable<TService>(string name);
TService GetTable<TService>(uint idx);
IReadOnlyDictionary<TKey, TRow> GetTable<TKey, TRow>(string name);
```

`GetTable<TService>`는 concrete table/default `IDataTable<TRow>`/명시적 추가 binding만 반환한다. idx overload는 해당 행의 존재를 요구하지 않는다. snapshot에는 public constructor가 없다. container는 읽기 전용이고 행은 복제되지 않는다. 보관한 이전 snapshot은 reload·manager 종료 뒤에도 유효하다.

ID 계약:

```csharp
// IIdxRouter
bool TryGetDataType(uint idx, out uint dataType);
// IIdxCodec<TParts> : IIdxRouter
uint Generate(TParts parts);
bool TryExtract(uint idx, out TParts parts);
// IdxParts / DecimalIdxCodec
IdxParts(uint dataType, uint localIdx);
DecimalIdxCodec(uint stride);
```

`IdxParts`는 `DataType`, `LocalIdx` getter를 제공한다. 기본 codec은 `dataType * Stride + localIdx`이고 stride > 1, 두 parts > 0, localIdx < stride다. invalid parts는 `ArgumentOutOfRangeException`, uint 초과는 `OverflowException`이다. 추출 실패는 false/default; 분류 성공도 테이블/행 존재를 보장하지 않는다. optional localType 등은 프로젝트 parts/codec으로 확장한다. router는 불변·무부작용이어야 한다.

공용 validator:

```csharp
static void ValidateIdx(uint idx, uint dataType, IIdxRouter router);
static ReadOnlyDictionary<TKey, TRow> Read<TKey, TRow>(string text, string[] headers,
    Func<CsvReader, TRow> readRow, Func<TRow, TKey> keySelector,
    Action<TRow> validateRow, CancellationToken token,
    Action<CsvContext> configure = null, Action<CsvReader> validateHeader = null);
```

`DataTableCsvValidator`는 I/O 없이 invariant comma CSV의 header·모든 field·변환·PK를 검증한다. 빈 이름/중복 header, 누락, 변환 실패, malformed CSV, 중복/null key는 `InvalidDataException`; header-only CSV는 유효하다. row reader는 현재 field만 읽고 reader를 advance/dispose/보관하지 않는다.

호출 순서는 생성 → router/테이블/binding/FK/전체 validator 등록 → `await LoadAsync` → 조회 → 소유자 `Dispose`다. 모든 manager API는 Unity 메인 스레드에서 호출한다. 중첩 Load는 같은 시도를 공유하며 caller token은 그 caller 대기만 취소한다. 나중의 Load는 명시적 reload다. owner 종료는 publication을 막고 대기를 취소한다. CSV source는 외부 I/O·handle 정리를 소유하며, 종료 후 늦게 완료된 native 작업도 별도로 정리해야 한다. CSV 실패는 테이블 문맥과 inner exception을 보존하고, 전체 validator 오류는 그대로 전달한다. 실패/취소는 기존 snapshot을 유지한다.

설명용 발췌(NotRun): `GameRow`, `GameTable`, CSV source와 idx 규격은 프로젝트가 제공한다.

```csharp
using (var tables = new DataTableManager())
{
    tables.RegisterIdxRouter(new DecimalIdxCodec(10000));
    tables.RegisterTable<GameRow, GameTable>(1, "Game", ReadProjectCsvAsync,
        () => new GameTable());
    await tables.LoadAsync(cancellationToken);
    GameRow row = tables.Get<GameRow>(10001);
}
```

실행 확인된 가져오기 예제는 [소비 프로젝트 smoke](../../tools/core-consumer/templates/ConsumerSmoke.cs)와 해당 [검증](../validation/input-system/p4/README.md)을 사용한다. dynamic 반환·자동 도메인 테이블·파일 저장·수정 가능한 공개 snapshot은 제공하지 않는다.
