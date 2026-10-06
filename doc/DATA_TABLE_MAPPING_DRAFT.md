# DTO·테이블·인터페이스 매핑 계약

2026-10-02. 최초 상태: 검토 초안. 2026-10-06 generic 범위 구현 완료. 기준은 MyLab `main / b8c2c91`과 Cashier `total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0`이다. 아래 API와 예제는 현재 구현 계약이며 [DataTableManager](DATA_TABLE_MANAGER.md)와 [검증 증거](validation/generic-data-tables/README.md)를 함께 확인한다.

후속 요청으로 기본 제공 테이블의 uint 키는 확정했다. 생성·추출 클래스 등록, 종류와 테이블 연결, PK/FK 탐색의 세부 계약은 [uint idx 계약](DATA_TABLE_IDX_DRAFT.md)이 소유한다. 아래 표준 인터페이스·등록 예제도 이 요구에 맞춰 갱신했다.

2026-10-06 보완으로 codec은 Parts 값 타입과 불변 Stride 설정을 함께 지원한다. 프로젝트의 선택적 localType은 같은 테이블 내부의 구분이며 표준 테이블 등록은 dataType만 사용한다. 세부 계약은 위 idx 초안에서 확인한다.

2026-10-06 후속 변경: Text/Resource DTO·테이블은 코어에서 제거하고 [예시 템플릿](templates/data-tables/README.md)으로 분리했다. 예시 소스는 테스트 assembly에서만 컴파일하며 프로젝트에서 복사·수정해 사용한다. 현재 [분리 검증](validation/data-table-templates/README.md)을 따른다.

제네릭 조회 구현 준비 기준은 [구현 순서·완료 조건](DATA_TABLE_GENERIC_IMPLEMENTATION.md)으로 연결한다. 아래 표준 DTO/기반·기본 테이블/명시적 binding은 그 준비 범위이며 runtime에 생성기를 강제하지 않는다.

사용자 요청은 Lab이 기본 DTO 규격과 인터페이스를 제공하고 이를 구현한 테이블 단위를 매핑하는 것이다. 여기서 구현 단위는 CSV 한 종류의 DTO 매핑·행 검증·조회 기능을 가진 테이블 클래스로 해석했다. DTO를 GameObject·게임 서비스로 생성하는 기능은 별도 범위다.

## Cashier에서 확인한 구조

| 실제 구조 | MyLab 채택 판단 |
|---|---|
| `TextData`, `ResourceData`, `ProductData`의 `[Name]`·`[TypeConverter]` 속성 | 개선 후 채택: 기본 DTO와 CsvHelper의 DTO 자동 매핑 경로 제공 |
| 테이블의 `ReadHeader` → `ValidateHeader<T>` → `GetRecord<T>` → PK/행 검증 | 개선 후 채택: 공용 CSV 파싱·header/중복 키 검사는 manager에 두고 테이블은 매핑·게임 검증을 제공 |
| `IDataLoad`의 `LoadData`, `GetDataCount`, `Release`와 테이블별 구현 | 책임 분리: 소비자 조회에는 읽기 전용 인터페이스를 제공하고 로드·공개·종료는 manager가 소유 |
| `Dictionary<DataTableType, IDataLoad>`와 `GetDB<T>` | 개선 후 채택: 명시적 테이블 이름과 계약 타입을 구현 테이블에 연결하며 잘못된 매핑은 등록 시 거부 |
| `idx / 1000`·게임별 enum·필수 테이블 목록·Singleton | 등록한 router와 별도 codec으로 생성·역추출 지원. 게임 enum·1000 구간·필수 목록·Singleton은 프로젝트 선택 |
| DTO의 public setter, 일부 테이블의 PendingRows/Commit, ResourceDataTable의 독립 갱신 | setter의 변경 가능성을 명시하고 전체 snapshot 공개·이전 세대 보존 계약 유지 |

읽은 근거는 Cashier `Assets/Scripts/Commons/Commons.cs`, `Commons/Data/{TextData,ResourceData,ProductData,TextDataTable,ProductDataTable,ResourceDataTable}.cs`, `Customer/Data/CustomerDispositionDataTable.cs`, `Manager/DataTableManager.cs`, `Utils/Util.cs`다. Cashier 파일·코드·plugin은 복사하거나 수정하지 않았다.

## 공용 기반 규격

공용 namespace는 `MyLab.Core.DataTables`다. 기본 CSV 규격은 `idx` 열과 uint 키로 고정한다. ID 구간·0/null 정책은 uint idx 계약을 따르며 기존 수동 Register는 임의 TKey/TRow를 계속 지원한다. 표준 종류 registry와 idx 조회는 새 표준 경로에 적용하고 수동 등록을 자동 변환하지 않는다.

```csharp
public interface IDataRow
{
    uint Id { get; }
}

public abstract class DataRow : IDataRow
{
    [Name("idx")]
    public uint Id { get; set; }
}

public interface IDataTable<TRow>
{
    IReadOnlyDictionary<uint, TRow> Rows { get; }
    int Count { get; }
    bool TryGet(uint key, out TRow row);
}
```

위 DTO·인터페이스 예제에는 `CsvHelper.Configuration.Attributes`와 `System.Collections.Generic`의 using이 필요하다.

- `DataRow`는 식별 키만 가진 최소 DTO다. TextRow/ResourceKeyRow와 두 구체 테이블은 코어 규격이 아닌 프로젝트용 예시다. 필드 구성은 프로젝트가 정하며 manager는 등록하지 않은 CSV를 탐색하거나 테이블을 자동 생성하지 않는다.
- 예시 TextRow는 빈 문자열을 허용하고 ResourceKeyDataTable은 공백 키를 거부한다. 이 규칙은 예시의 검증이며 공용 manager의 필수 정책이 아니다. 실제 Addressables 키의 존재를 CSV 파싱으로 보장하지 않는다.
- DTO의 public setter는 CsvHelper 표준 매핑을 쓰기 위한 선택이며 공개 후 불변성을 컴파일러로 보장하지 않는다. 공개된 DTO는 수정하지 않고 재로드에서 새 행을 생성한다. 깊은 복사·가변 DTO 동결 기능은 초기 범위에서 제외한다.
- C#의 Id와 CSV의 idx를 명시적으로 연결한다. Cashier의 `Idx`를 이름만으로 추정하지 않고 기존 DTO를 사용할 때는 속성 또는 ClassMap으로 대응시킨다.

## 테이블 구현 단위

`CsvDataTable<TRow> : IDataTable<TRow>`를 공통 기반으로 제공한다. 표준 DTO 경로는 `where TRow : class, IDataRow`를 만족한다. `TextDataTable : CsvDataTable<TextRow>`, `ResourceKeyDataTable : CsvDataTable<ResourceKeyRow>`는 예시 템플릿이며 runtime core에서 제공하지 않는다. Text/Resource를 포함한 구체 스키마·테이블은 소비 프로젝트에 둔다.

| 기반 클래스가 제공하는 기능 | 구체 테이블이 보완하는 기능 |
|---|---|
| Rows/Count/TryGet, DTO.Id를 키로 사용 | `ConfigureMapping(CsvContext)`에서 필요한 ClassMap·converter 설정 |
| manager 내부에서 후보의 읽기 전용 행을 한 번 연결 | `ValidateRow(TRow)`에서 행의 독립적인 게임 규칙 검증 |
| 연결된 행 재설정 금지, Load/Commit/Release는 미공개 | `ValidateTable(IReadOnlyDictionary<uint, TRow>)`에서 후보 내 행 수·일관성 검증 |

각 hook은 protected virtual이며 기본 구현은 빈 동작이다. GetRecord·header 검사·dictionary 구성은 manager가 실행한다. FK는 기존 AddValidator에 전체 후보 snapshot을 전달해 검증한다. 구체 테이블의 생성자·hook·factory는 외부 상태 변경·자산 로드·후보의 외부 공개를 수행하지 않는다.

각 로드 시도에서 factory로 새 구체 테이블을 생성하고 새 행을 연결한 뒤 전체 후보 검증이 성공하면 snapshot으로 공개한다. manager가 이미 사용한 테이블 인스턴스의 재사용은 거부하고 실패한 후보도 재사용하지 않는다. 기존 테이블 참조와 행은 이전 세대를 유지한다. 테이블은 관리 데이터만 소유하며 독자적인 Dispose·네이티브 자원 수명을 추가하지 않는다.

## CSV에서 DTO로 매핑

기본 경로는 DTO의 `[Name]`·`[Optional]`·converter 속성과 `GetRecord<TRow>()`를 사용하는 매핑이다. 열 이름이 다른 기존 CSV·외부 DTO는 소비 프로젝트의 `ClassMap<TRow>`를 ConfigureMapping에서 등록한다. [CsvHelper 속성](https://joshclose.github.io/CsvHelper/examples/configuration/attributes/)과 [이름 기반 ClassMap](https://joshclose.github.io/CsvHelper/examples/configuration/class-maps/mapping-by-name/)을 사용하며 별도 반사 매핑 라이브러리를 만들지 않는다.

표준 경로는 reader 생성 후 header를 읽기 전에 ConfigureMapping을 실행한다. ReadHeader 뒤 `ValidateHeader<TRow>()`로 매핑의 필수 열을 검사하며 header만 있는 CSV도 검사한다. 표준 경로에 별도 RequiredHeaders 목록을 중복 관리하지 않고 속성/ClassMap의 필수·선택 열 지정을 사용한다. 기존 수동 Register는 RequiredHeaders를 유지한다.

공통 header 이름의 중복·공백, 열 수와 모든 필드의 잘못된 인용 검사는 기존 manager가 유지한다. 현재 대소문자 구분·InvariantCulture·쉼표 구분 규칙도 유지하며 header를 자동으로 소문자로 바꾸거나 DTO 타입의 delimiter/culture 속성을 자동 적용하지 않는다. Cashier의 `path` 열을 프로젝트 예시 ResourceKeyRow.Key로 읽으려면 ClassMap에 지정한다. 타입 변환·독자 converter·상속된 Id·Optional 열 동작은 구현 시 테스트한다.

## 계약 타입과 구현 테이블 매핑

이름·계약 매핑 API는 다음과 같다. idx 생성·추출 등록과 idx 기반 조회는 uint idx 계약에 정의한다.

```csharp
// TTable : CsvDataTable<TRow>
manager.RegisterTable<TRow, TTable>(dataType, name, readCsvAsync, createTable);
manager.BindTable<TService>(name);
snapshot.GetTable<TService>(name);
```

`dataType`은 uint 종류 코드, `readCsvAsync`는 기존 `Func<CancellationToken, UniTask<string>>`, `createTable`은 `Func<TTable>`이다. 등록한 TTable과 `IDataTable<TRow>`는 기본 공개 계약이다. BindTable은 추가 프로젝트 인터페이스를 선언된 TTable에 명시적으로 연결한다. 매핑 키는 `(테이블 이름, 계약 Type)`이며 종류 코드는 한 표준 테이블만 가리킨다. 같은 DTO·구체 타입의 여러 CSV도 서로 다른 이름·종류 코드로 등록할 수 있다. 추가 binding은 인터페이스만 허용하며 별도 일반 서비스 등록 기능으로 확장하지 않는다.

동명·동일 종류 등록, 같은 이름·계약의 중복 binding, 미등록 이름, TTable이 구현하지 않는 계약은 등록 시 예외로 거부한다. 구성은 첫 유효한 LoadAsync 시작 때 동결된다. 등록과 기본 binding의 입력을 함께 검증하며 실패한 등록의 일부만 남기지 않는다. factory의 null·재사용·예외는 로드 실패로 처리하고 이전 snapshot을 유지한다.

snapshot.GetTable은 등록된 계약만 반환하며 미등록 이름은 KeyNotFoundException, 연결되지 않은 계약은 InvalidOperationException이다. 같은 snapshot의 구체 타입·추가 interface·기본 IDataTable 조회는 같은 테이블 인스턴스를 반환한다. 기존 `GetTable<TKey, TRow>(name)`은 해당 테이블과 동일한 읽기 전용 dictionary를 반환한다.

다음은 소비 프로젝트의 조회 interface 예제다. ITextLookup은 코어 필수 계약으로 추가하지 않는다. TextRow는 프로젝트로 복사한 예시 타입이다. 추가 프로젝트 interface 예시다. 실행 검증은 동일한 패턴의 DataTableBindingTests가 담당한다.

```csharp
public interface ITextLookup
{
    string GetText(uint id);
}

public sealed class ProjectTextTable : CsvDataTable<TextRow>, ITextLookup
{
    public string GetText(uint id) => Rows[id].Text;
}

manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
manager.RegisterTable<TextRow, ProjectTextTable>(
    (uint)GameDataType.Text, "texts", readTextCsvAsync, () => new ProjectTextTable());
manager.BindTable<ITextLookup>("texts");
var snapshot = await manager.LoadAsync(cancellationToken);
var texts = snapshot.GetTable<ITextLookup>("texts");
string text = texts.GetText(nameIdx); // 대상 Text PK. 없는 키는 KeyNotFoundException.
```

소비자에게 interface를 주입할 때는 root 준비 후 확보한 같은 snapshot을 사용한다. 재로드로 새 snapshot을 공개하면 프로젝트가 새 데이터 채택·재주입을 명시적으로 수행한다. 최신 테이블을 자동 추적하는 proxy·전역 조회·assembly 탐색·Activator/DI container 자동 생성은 초기 범위에 포함하지 않는다.

## 구현 순서와 완료 조건

1. DTO·IDataRow·IDataTable과 기반/프로젝트 예시 테이블의 책임을 확정한다. 표준 uint 키 요구를 적용하고 생성·추출 규약의 경계·역함수를 확인한다. 기존 수동 API를 제한하지 않는다.
2. RegisterTable의 DTO 매핑을 TDD로 추가한다. 상속 Id·속성·ClassMap·Optional·타입 변환·header-only 필수 열 누락·미사용 열 오류·converter 오류·빈 테이블을 확인한다.
3. 이름·계약 binding을 TDD로 추가한다. interface/구체 타입의 동일 인스턴스, 다른 이름의 동일 타입 독립성, 잘못된 binding·중복·등록 동결, factory null/예외/재사용을 확인한다.
4. FK·재로드 실패 시 기존 dictionary/테이블 보존, 공유 로드·호출자 취소·Dispose 후 늦은 공개 금지를 확인한다. 기존 수동 Register와 혼합 등록·전체 회귀도 확인한다.
5. 프로젝트 installer가 새 API로 필수 테이블을 준비하고 성공 후에만 씬을 진행하는 경로를 확인한다. root/ResourceManager의 소유권·해제 순서는 유지한다.

DTO 매핑의 Player/IL2CPP 경로는 미검증이다. 구현 후 소비 프로젝트·Player 단계에서 확인하고 필요한 코드 보존 설정은 실제 빌드 결과를 근거로 정한다. 기존 수동 GetField 경로의 테스트를 새 DTO 매핑의 실행 증거로 사용하지 않는다.

## 이번 검증과 변경 경계

최초 요청은 초안 작성이었다. 2026-10-06 구현 요청으로 runtime·관련 테스트·문서를 변경했으며 scene·settings·package는 보존했다. 참조 코드 대조, 초안/현행 계약 구분, 상대 링크·색인·diff·공백·변경 범위·사용자 변경 hash를 확인한다. 현재 Red/Green·컴파일·Console·전체 회귀 결과와 Player/소비 프로젝트 미실행 경계는 [검증 설명](validation/generic-data-tables/README.md)을 따른다.

최초 초안 `e2cc242`의 과거 문서 검사 결과는 Markdown 14개, 상대 링크 165개, anchor 2개, 검사 오류 0건이다. 당시 로컬 증거는 `Temp/DataTableMappingDraft/Validate.ps1`과 `validation.json`이다. 후속 uint 요구 반영 당시의 검사·변경 경계는 [단위 회고](retrospectives/2026-10-02-02-data-table-idx-draft.md)에 기록한다. 당시 문서 작업의 테스트 0건은 이번 구현의 실행 증거로 사용하지 않는다.
