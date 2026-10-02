# uint idx 생성·추출·PK/FK 탐색 초안

2026-10-02. 기준 MyLab `main / a7804a8`, Cashier `total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0`. 상태는 미구현 초안이다. 기본 제공 테이블의 uint index key, 생성·추출 클래스 등록, PK/FK 탐색 요구는 사용자 지시로 확정이다. 아래 API·인코딩·오류 세부 정책은 제안이다. [DTO·테이블 매핑 초안](DATA_TABLE_MAPPING_DRAFT.md)과 연결하며 [현재 구현 계약](DATA_TABLE_MANAGER.md)을 구현 완료로 바꾸지 않는다.

## 역할과 Cashier 참조

idx는 대상 행의 PK이며 다른 행에 저장하면 그 대상에 대한 FK가 된다. FK에는 대상의 완전한 idx를 저장한다. 내부 번호만 저장하면 테이블 종류를 복원할 수 없다.

Cashier `Utils/Util.cs`의 CreateDataIdx/GetDataTableType/GetDataInnerId는 `idx = (uint)DataTableType * 1000 + innerId`, `type = idx / 1000`, `innerId = idx % 1000`을 사용한다. `Manager/DataTableManager.cs`의 GetDB<T>(uint)는 추출 종류로 등록 테이블을 찾는다. `Commons/Data/ProductData.cs`의 NameIdx·ImageResourceIdx는 대상 PK를 저장하며 `Customer/Data/CustomerCatalog.cs`는 Text·Resource 등의 실제 대상 행 존재를 검증한다.

MyLab은 양방향 규약과 종류별 탐색을 개선 후 채택할 것을 제안한다. 게임 enum·1000 구간은 소비 프로젝트가 선택한다. Cashier의 idx=0일 때 첫 T 구현을 찾는 fallback은 적용하지 않고 범위·overflow·미등록 종류·잘못된 대상·참조 누락을 구분한다. Cashier 파일·코드는 변경하거나 복사하지 않았다.

## 생성·추출 클래스 등록

```csharp
public interface IIdxGenerator
{
    uint Generate(uint dataType, uint localId);
}

public interface IIdxExtractor
{
    bool TryExtract(uint idx, out uint dataType, out uint localId);
}

public interface IIdxCodec : IIdxGenerator, IIdxExtractor
{
}
```

dataType은 CLR Type 자체가 아니라 프로젝트가 정한 안정적인 uint 종류 코드다. enum을 사용하면 `(uint)GameDataType.Text`처럼 전달한다. 종류의 의미는 테이블 등록이 제공하며 게임 enum을 코어에 추가하지 않는다.

생성·추출의 소비 계약은 분리하고, 두 계약을 만족하는 같은 규약 객체를 `manager.RegisterIdxCodec(IIdxCodec codec)`으로 등록한다. 한 manager의 표준 테이블은 규약 하나를 공유한다. null/중복 등록·첫 로드 이후 변경은 거부한다. 표준 테이블이 있는데 codec이 없으면 I/O 시작 전에 구성 오류로 거부하고 구성을 동결하지 않는다. 수동 Register만 쓰는 경우에는 codec을 강제하지 않는다.

- Generate는 유효한 종류·내부 번호에 완전한 uint idx를 반환한다. 입력 범위 위반은 ArgumentOutOfRangeException, 표현 범위 초과는 OverflowException이다. 0이나 wrap된 값을 실패 대용으로 반환하지 않는다.
- TryExtract는 형식상 유효한 idx에서 종류·내부 번호를 추출한다. 무효 값은 false, out 값은 0이다. 추출 성공은 등록된 종류나 실제 행 존재의 보장이 아니다. 구현 결함의 예외는 false로 숨기지 않는다.
- 역함수 조건: `Extract(Generate(type, local)) == (type, local)` 및 유효 idx의 `Generate(Extract(idx)) == idx`. 같은 입력은 같은 결과이며 유효한 서로 다른 쌍의 idx는 충돌하지 않아야 한다.
- 규약은 I/O·부수 효과가 없는 관리 객체다. 설정은 생성자에서 확정하고 등록 후 외부에서 바꾸지 않는다. manager와 이전 snapshot은 그 불변 객체를 참조하며 manager가 임의로 Dispose하지 않는다.
- Generate는 조합 함수다. 다음 빈 번호 발급·예약·CSV 수정·행 삽입·중복 자동 보정은 수행하지 않는다. 클래스명/assembly 탐색으로 구현을 자동 생성하지 않는다.

## 기본 인코딩 후보

선택 가능한 `DecimalIdxCodec(uint stride)`를 기본 구현 후보로 둔다. stride는 1보다 커야 한다. Cashier 호환이 필요하면 프로젝트가 1000을 전달한다.

```text
Generate(type, local) = checked(type * stride + local)
Extract(idx)          = (idx / stride, idx % stride)
valid                 = type > 0, 0 < local < stride
```

| 입력 | stride=1000 결과 |
|---|---|
| Text 종류 8, 내부 번호 248 | 8248 → 종류 8, 내부 번호 248 |
| Resource 종류 4, 내부 번호 1 | 4001 → 종류 4, 내부 번호 1 |
| 내부 번호 0 또는 1000 | 거부; 다음 종류 구간으로 넘기지 않음 |
| 종류 4,294,967, 내부 번호 295 | uint.MaxValue로 표현 가능 |
| 종류 4,294,967, 내부 번호 296 | overflow로 거부 |

0을 표준 PK/FK의 무효값으로 예약하고 선택 FK 부재는 nullable의 null로 나타낼 것을 제안한다. 0을 유효 키로 쓰는 현재 수동 Register는 변경하지 않는다. 비트 조합 등 다른 프로젝트 codec도 같은 역함수·범위 계약을 만족하면 사용할 수 있다.

stride·종류 코드·추출 규칙을 바꾸면 CSV와 저장된 FK의 의미가 바뀐다. enum 이름 변경과 숫자 재배정은 구분하며 숫자 재배정·규약 변경은 데이터 migration을 포함한 별도 작업이다. 구형 값을 이름이나 새 규약으로 추정·보정하지 않는다.

## 종류와 테이블 등록

표준 경로는 uint 키로 고정하므로 새 RegisterTable의 TKey를 제거한다. 이름·interface 매핑은 기존 초안의 계약을 유지한다. 아래는 현재 구현에 없는 제안 API다.

```csharp
var codec = new DecimalIdxCodec(1000);
manager.RegisterIdxCodec(codec);
manager.RegisterTable<TextRow, TextDataTable>(
    (uint)GameDataType.Text, "texts", readCsvAsync, () => new TextDataTable());
uint idx = codec.Generate((uint)GameDataType.Text, 248);
```

registry는 `종류 코드 → 테이블 이름 → DTO 타입·구현·공개 interface`를 연결한다. 한 snapshot의 종류 코드는 표준 테이블 하나만 가리킨다. 중복 종류·이름·잘못된 binding은 등록 시 거부하며 실패한 입력의 일부를 남기지 않는다. 종류 0은 예약값으로 거부한다.

같은 DTO·구체 타입의 여러 CSV는 서로 다른 종류 코드로 등록한다. 하나의 종류에 여러 파일이 속하면 프로젝트 소스가 통합하거나 선택한 문자열을 테이블 하나에 제공한다. 이름만 다르게 같은 종류를 중복 등록하면 idx로 목적지를 결정할 수 없으므로 허용하지 않는다. 종류 번호는 로드·의존 순서를 의미하지 않는다.

표준 PK는 파싱 후 0 아님·추출 성공·등록 종류 일치·생성/추출 왕복 일치·중복 없음으로 검사한다. 다른 종류의 CSV에 들어간 PK는 자동 이동하지 않는다. header-only 테이블 종류도 등록 메타데이터로 알 수 있으며 첫 행에서 추정하지 않는다.

## PK/FK 탐색

조회는 모든 테이블이 같은 세대로 묶인 DataTableSnapshot을 기준으로 한다. manager는 로드·현재 snapshot 공개를 소유한다. 소비자는 manager.Snapshot을 한 번 확보하고 관련 조회를 그 객체에서 수행한다. 여러 호출마다 최신 snapshot을 다시 읽어 세대를 섞지 않는다.

```csharp
var snapshot = await manager.LoadAsync(cancellationToken);
var table = snapshot.GetTable<IDataTable<TextRow>>(nameIdx);
var text = snapshot.GetRow<TextRow>(nameIdx);
bool found = snapshot.TryGetRow<ResourceKeyRow>(imageIdx, out var resource);
```

GetTable<TService>(uint idx)는 추출 → 종류 registry → 같은 테이블의 명시적 계약 binding으로 조회한다. 테이블 검색이므로 idx 행 존재는 보장하지 않으며 실제 참조는 GetRow/TryGetRow로 해결한다. 이름 기반 API도 같은 테이블·dictionary를 사용한다.

| API | 실패 계약 후보 |
|---|---|
| GetTable<TService>(idx) | 무효 idx: ArgumentException; 미등록 종류: KeyNotFoundException; 계약 불일치: InvalidOperationException |
| GetRow<TRow>(idx) | 위 형식/종류 오류; 정확한 DTO 타입 불일치: InvalidOperationException; 대상 PK 누락: KeyNotFoundException |
| TryGetRow<TRow>(idx, out row) | 무효 idx·미등록 종류·DTO 타입 불일치·대상 PK 누락: false, row=null. codec 구현 예외는 전파 |

GetRow/TryGetRow는 등록된 구체 DTO와 정확히 일치하는 TRow를 요구한다. 공통 base·관계없는 DTO로 성공시키지 않는다. 반환값은 snapshot의 관리 행 참조이며 자산 handle·새 게임 객체가 아니다. 이전 snapshot·테이블은 재로드·manager Dispose 후에도 기존 데이터를 유지하고 codec·registry를 새 규약으로 교체하지 않는다.

## FK 선언과 검증

종류 추출만으로 FK가 유효해지지는 않는다. Product.NameIdx에는 존재하는 Text 행이 필요하며 존재하는 Resource 행이 들어가도 오류다. 예상 목적지는 소비 프로젝트가 선언한다.

기존 AddValidator를 사용하는 helper 후보:

```csharp
manager.RegisterForeignKey<ProductRow, TextRow>(
    sourceTable: "products",
    column: "nameidx",
    getForeignKey: row => (uint?)row.NameIdx,
    targetDataType: (uint)GameDataType.Text,
    required: true);
```

getForeignKey는 `Func<TSource, uint?>`이며 source/target은 표준 테이블, TSource/TTarget은 IDataRow를 만족하는 구체 DTO다. source·target 테이블 등록 후 규칙을 등록한다. 구성 시 source 이름·DTO와 target 종류·DTO 일치를 확인하고 행 참조는 전체 후보 구성 후 검증한다.

1. 필수 FK의 null은 오류, 선택 FK의 null은 참조 없음이다. 0을 자동으로 null로 바꾸지 않는다.
2. 값이 있으면 추출한다. 형식 오류·미등록 종류·선언한 대상 종류 불일치는 오류다.
3. 같은 후보 snapshot의 대상 dictionary에 완전한 idx가 있는지 검사한다. 이전 공개 데이터로 후보 FK를 검증하지 않는다.
4. 실패는 source 이름·행 PK·column·FK·예상 종류·원인을 포함하는 InvalidDataException이다. 전체 후보 공개를 중단하고 이전 정상 snapshot을 유지한다.

단일 nullable FK가 helper 범위다. 배열·복합 관계는 AddValidator에서 같은 조회 경로로 검증한다. FK 컬럼을 이름이나 모든 uint 필드에서 자동 추정하지 않으며 필수 관계를 등록·검증할 책임은 소비 프로젝트에 있다. 별도 FK 엔진·재귀 객체 생성·cascade는 추가하지 않는다.

자기·순환 FK도 모든 후보 dictionary 구성 후 존재를 검사할 수 있다. 순환 금지·게임 상태 조건은 프로젝트 검증이다. ResourceKeyRow 참조 성공은 실제 Addressables 자산 준비 완료가 아니며 ResourceManager 준비 단계에서 별도로 기다린다.

## 구현 순서와 완료 조건

1. codec의 경계·역함수 테스트를 Red부터 실행한다. stride 0/1, 종류/local 0, local=stride-1/stride, uint.MaxValue 경계와 유효 쌍 왕복·충돌 부재를 확인한다.
2. uint 테이블 등록: codec 누락/null/중복·종류 중복·설정 동결·종류가 다른 PK·header-only·잘못된 등록의 잔여 상태를 확인한다. 수동 Register 혼합은 기존 동작을 유지한다.
3. idx 기반 interface/DTO 조회: 실패 계약, 같은 DTO의 다른 종류, 이름/idx 조회의 동일 인스턴스·행을 확인한다.
4. FK: 필수/선택·0/null·잘못된 종류·누락 PK·자기/순환 참조·배열 프로젝트 검증을 확인한다. 재로드 후보의 대상 제거가 실패하고 기존 행/테이블/codec이 보존되는지도 확인한다.
5. 공유 로드·취소·Dispose·씬 준비·전체 회귀를 확인한다. 새 DTO/codec의 Player·소비 프로젝트 경로는 후속 단계에서 검증한다.

uint 표준 키와 생성/추출 등록·PK/FK 탐색 요구는 사용자 지시다. DecimalIdxCodec 기본 제공 여부, stride 선택, 0/null 정책과 API 세부형은 초안이며 구현·자동 migration을 시작하지 않았다. 이번 검사와 다음 행동은 [단위 회고](retrospectives/2026-10-02-02-data-table-idx-draft.md)에 남긴다.
