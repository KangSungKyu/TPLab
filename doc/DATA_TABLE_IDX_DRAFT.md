# uint idx 생성·추출·PK/FK 계약

2026-10-02. 기준 MyLab `main / a7804a8`, Cashier `total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0`. 최초 상태는 미구현 초안이었다. 2026-10-06 준비한 generic 범위를 구현했으며 [실행 증거](validation/generic-data-tables/README.md)로 확인한다. 기본 제공 테이블의 uint index key, 생성·추출 클래스 등록, PK/FK 탐색 요구는 사용자 지시로 확정이다. 아래 API·인코딩·오류 정책은 이번 구현의 계약이다. [DTO·테이블 매핑 계약](DATA_TABLE_MAPPING_DRAFT.md)과 연결하며 [manager 계약](DATA_TABLE_MANAGER.md)과 함께 적용한다.

2026-10-06 보완: 조합 값 Parts와 구간 설정 Stride를 함께 지원한다. localType은 여러 작업자의 키 충돌 방지·테이블 내부 분류를 위한 선택적 요소이며 테이블 탐색 종류가 아니다. 프로젝트 세 요소 codec은 테스트 예제로 검증하며 코어 기본 타입으로 추가하지 않는다.

2026-10-06 구현 준비 기준: 소비 조회는 Get<TRow>/TryGet<TRow>로 통일하고 런타임 등록은 IIdxRouter만 요구한다. 아래 기준을 현재 runtime에 구현했다. 작업 순서·수정 경계·완료 조건은 [제네릭 구현 준비](DATA_TABLE_GENERIC_IMPLEMENTATION.md)를 따른다.

## 역할과 Cashier 참조

idx는 대상 행의 PK이며 다른 행에 저장하면 그 대상에 대한 FK가 된다. FK에는 대상의 완전한 idx를 저장한다. 내부 번호만 저장하면 테이블 종류를 복원할 수 없다.

Cashier `Utils/Util.cs`의 CreateDataIdx/GetDataTableType/GetDataInnerId는 `idx = (uint)DataTableType * 1000 + innerId`, `type = idx / 1000`, `innerId = idx % 1000`을 사용한다. `Manager/DataTableManager.cs`의 GetDB<T>(uint)는 추출 종류로 등록 테이블을 찾는다. `Commons/Data/ProductData.cs`의 NameIdx·ImageResourceIdx는 대상 PK를 저장하며 `Customer/Data/CustomerCatalog.cs`는 Text·Resource 등의 실제 대상 행 존재를 검증한다.

MyLab은 양방향 규약과 종류별 탐색을 개선 후 채택했다. 게임 enum·1000 구간은 소비 프로젝트가 선택한다. Cashier의 idx=0일 때 첫 T 구현을 찾는 fallback은 적용하지 않고 범위·overflow·미등록 종류·잘못된 대상·참조 누락을 구분한다. Cashier 파일·코드는 변경하거나 복사하지 않았다.

## 생성·추출 클래스 등록

```csharp
public interface IIdxRouter
{
    bool TryGetDataType(uint idx, out uint dataType);
}

public interface IIdxCodec<TParts> : IIdxRouter
{
    uint Generate(TParts parts);
    bool TryExtract(uint idx, out TParts parts);
}
```

dataType은 CLR Type 자체가 아니라 프로젝트가 정한 안정적인 uint 종류 코드다. enum을 사용하면 `(uint)GameDataType.Text`처럼 전달한다. 종류의 의미는 테이블 등록이 제공하며 게임 enum을 코어에 추가하지 않는다.

생성·전체 추출은 프로젝트의 TParts를 사용하고 manager의 테이블 탐색은 IIdxRouter를 사용한다. 초기의 두 uint 매개변수 및 비제네릭 IIdxGenerator/IIdxExtractor/IIdxCodec 제안은 위 계약으로 대체한다. 값 묶음의 요소 개수에 맞춰 manager를 제네릭 타입으로 바꾸지 않는다.

등록 API는 `manager.RegisterIdxRouter(IIdxRouter router)`다. 기존 RegisterIdxCodec<TParts> 제안은 대체하며 호환 alias를 새로 만들지 않는다. 기본 또는 프로젝트 codec을 installer/scene root에서 직접 new로 생성해 전달할 수 있고, runtime에 생성 기능이 필요 없으면 프로젝트의 router 구현만 전달할 수도 있다. codec의 Generate/TryExtract는 데이터 작성·분류·해당 규약의 왕복 검증에 사용한다. manager는 TParts·생성기·전체 추출에 의존하지 않으며 모든 로드 행을 재생성하지 않는다.

한 manager의 표준 테이블은 불변 router 하나를 공유한다. null/중복 등록·첫 유효한 로드 시작 이후 변경은 거부한다. 표준 테이블이 있는데 router가 없으면 I/O 시작 전에 InvalidOperationException으로 거부하고 구성을 동결하지 않는다. 수동 Register만 쓰는 경우에는 router를 강제하지 않는다. router와 테이블은 첫 로드 전 어느 순서로 등록해도 되며 전체 구성 유효성을 로드 전에 확인한다.

- Generate는 유효한 Parts에 완전한 uint idx를 반환한다. 입력 범위 위반은 ArgumentOutOfRangeException, 표현 범위 초과는 OverflowException이다. 0이나 wrap된 값을 실패 대용으로 반환하지 않는다.
- TryExtract는 전체 형식이 유효한 idx를 TParts로 복원한다. 무효 값은 false, out 값은 default(TParts)다. TryGetDataType도 전체 idx를 검증하며 무효 값은 false, out 종류는 0이다. 종류 비트/자리수만 읽고 나머지 오류를 무시하지 않는다. 둘의 성공 여부와 추출 종류는 일치해야 한다. 추출 성공은 등록된 종류나 실제 행 존재의 보장이 아니다. 구현 결함의 예외는 false로 숨기지 않는다.
- 역함수 조건: `Extract(Generate(parts))`의 모든 요소가 원래 parts와 같고 유효 idx의 `Generate(Extract(idx)) == idx`다. 같은 입력은 같은 결과이며 유효한 서로 다른 조합의 idx는 충돌하지 않아야 한다.
- router·codec은 I/O·부수 효과가 없는 관리 객체다. 설정은 생성자에서 확정하고 등록 후 외부에서 바꾸지 않는다. manager와 이전 snapshot은 그 불변 router를 참조하며 manager가 임의로 Dispose하지 않는다.
- Generate는 조합 함수다. 다음 빈 번호 발급·예약·CSV 수정·행 삽입·중복 자동 보정은 수행하지 않는다. 클래스명/assembly 탐색으로 구현을 자동 생성하지 않는다.

## 기본 인코딩

Parts는 실제 조합 값이며 숫자 구간 크기가 아니다. 기본 제공 `IdxParts`는 DataType/LocalIdx만 갖는 불변 값 타입이다. 프로젝트는 LocalType 등 필요한 요소를 가진 다른 불변 TParts를 정의한다.

```csharp
public readonly struct IdxParts
{
    public IdxParts(uint dataType, uint localIdx)
    {
        DataType = dataType;
        LocalIdx = localIdx;
    }

    public uint DataType { get; }
    public uint LocalIdx { get; }
}
```

`DecimalIdxCodec(uint stride) : IIdxCodec<IdxParts>`를 기본 구현으로 제공한다. Stride는 생성자에서 검증·확정하고 읽기 전용 uint 프로퍼티로 제공한다. 1보다 커야 하며 Cashier 호환이 필요하면 프로젝트가 1000을 전달한다. Parts는 생성 호출마다 전달하고 Stride는 codec의 불변 설정으로 유지한다.

```text
Generate(parts) = checked(parts.DataType * Stride + parts.LocalIdx)
Extract(idx)    = IdxParts(idx / Stride, idx % Stride)
valid           = DataType > 0, 0 < LocalIdx < Stride
```

| 입력 | stride=1000 결과 |
|---|---|
| Text 종류 8, 내부 번호 248 | 8248 → 종류 8, 내부 번호 248 |
| Resource 종류 4, 내부 번호 1 | 4001 → 종류 4, 내부 번호 1 |
| 내부 번호 0 또는 1000 | 거부; 다음 종류 구간으로 넘기지 않음 |
| 종류 4,294,967, 내부 번호 295 | uint.MaxValue로 표현 가능 |
| 종류 4,294,967, 내부 번호 296 | overflow로 거부 |

Stride=10000이고 Parts=(2, 37)이면 idx=20037이며 네 자리씩 표시하면 `0002 | 0037`이다. uint에는 앞쪽 0·구분자·자리수 정보가 저장되지 않는다. 정확히 네 자리 DataType을 원하면 프로젝트가 DataType<=9999도 제한한다. Stride는 하위 구간 크기를 제한하며 상위 종류의 표시 폭까지 자동 제한하지 않는다.

초기 구현 기준은 표준 PK/FK의 0을 무효값으로 예약하고 선택 FK 부재는 nullable의 null로 표현하는 것이다. 이는 이번 준비에서 선택한 기본 정책이며 사용자 지시로 확정된 값 범위와 구분한다. 0을 유효 키로 쓰는 현재 수동 Register는 변경하지 않는다. 기본 codec의 LocalIdx는 양수이며 다른 프로젝트 codec의 내부 요소 범위는 프로젝트가 정한다. 비트 조합 등 다른 codec도 전체 idx 유효성·왕복·범위 계약을 만족하면 사용할 수 있다.

stride·종류 코드·추출 규칙을 바꾸면 CSV와 저장된 FK의 의미가 바뀐다. enum 이름 변경과 숫자 재배정은 구분하며 숫자 재배정·규약 변경은 데이터 migration을 포함한 별도 작업이다. 구형 값을 이름이나 새 규약으로 추정·보정하지 않는다.

## 선택적 localType과 프로젝트 구간

localType은 같은 테이블 안의 작업자·팀·분류를 구별하는 프로젝트 선택 요소다. 코어의 IdxParts/DTO/테이블 등록에 필수 필드로 추가하지 않으며 TryGetDataType은 dataType만 반환한다. FK는 localType을 포함한 완전한 idx를 저장한다.

세 요소를 선택한 프로젝트 예시는 다음과 같다. ProjectIdxParts/ProjectIdxCodec은 소비 프로젝트의 타입이며 코어 기본 구현 추가를 의미하지 않는다.

```csharp
var codec = new ProjectIdxCodec(localTypeCount: 100, localIdxStride: 10000);
manager.RegisterIdxRouter(codec);
uint idx = codec.Generate(new ProjectIdxParts(dataType: 2, localType: 7, localIdx: 37));
```

```text
Generate(parts) = checked((DataType * LocalTypeCount + LocalType) * LocalIdxStride + LocalIdx)
LocalIdx        = idx % LocalIdxStride
LocalType       = (idx / LocalIdxStride) % LocalTypeCount
DataType        = (idx / LocalIdxStride) / LocalTypeCount
```

예시 설정은 `LocalTypeCount=100`, `LocalIdxStride=10000`이며 둘 다 불변 구간 설정이다. 범위는 DataType>0, 0<=LocalType<100, 0<LocalIdx<10000과 최종 uint 표현 가능 범위다. (2, 7, 37)은 2070037, (2, 8, 37)은 2080037로 같은 테이블의 서로 다른 PK가 된다. 각 곱셈·덧셈과 최종 변환은 overflow를 검사하며 자동 잘림·다음 구간 넘김을 하지 않는다.

LocalType 배정의 중복 방지와 해당 구분 내 LocalIdx 유일성은 프로젝트 작업 규칙이 소유한다. 동일한 LocalType/LocalIdx를 두 작업자가 사용하면 여전히 충돌한다. 이 구분은 PK 충돌을 줄이며 같은 CSV 파일의 Git 텍스트 병합 충돌을 없애지는 않는다. 자동 작업자 ID 배정·잠금 서버·번호 예약은 이번 계약에 포함하지 않는다.

기본 두 요소 규약과 세 요소 규약을 같은 manager에 섞지 않는다. localType은 프로젝트가 세 요소 codec을 선택할지의 자유이며, 선택한 뒤 행마다 인코딩 형식을 임의로 생략하는 뜻은 아니다. 예시의 LocalType=0도 명시적인 값이다. 요소 수·구간 설정 변경은 기존 PK/FK migration이 필요하고 이전 snapshot의 codec/설정은 그대로 보존한다.

## 종류와 테이블 등록

표준 경로는 uint 키로 고정하므로 새 RegisterTable의 TKey를 제거한다. 이름·interface 매핑은 매핑 계약을 따른다. 아래는 현재 지원하는 API다.

```csharp
var codec = new DecimalIdxCodec(1000);
manager.RegisterIdxRouter(codec);
manager.RegisterTable<TextRow, TextDataTable>(
    (uint)GameDataType.Text, "texts", readCsvAsync, () => new TextDataTable());
uint idx = codec.Generate(new IdxParts((uint)GameDataType.Text, 248));
```

registry는 `종류 코드 → 테이블 이름 → DTO 타입·구현·공개 interface`를 연결한다. 한 snapshot의 종류 코드는 표준 테이블 하나만 가리킨다. 중복 종류·이름·잘못된 binding은 등록 시 거부하며 실패한 입력의 일부를 남기지 않는다. 종류 0은 예약값으로 거부한다.

같은 DTO·구체 타입의 여러 CSV는 서로 다른 종류 코드로 등록한다. 하나의 종류에 여러 파일이 속하면 프로젝트 소스가 통합하거나 선택한 문자열을 테이블 하나에 제공한다. 이름만 다르게 같은 종류를 중복 등록하면 idx로 목적지를 결정할 수 없으므로 허용하지 않는다. 종류 번호는 로드·의존 순서를 의미하지 않는다.

표준 PK는 데이터 작성 단계에서 최종 생성된 값으로 그대로 읽는다. 로드 검사는 0 아님·TryGetDataType에 의한 전체 형식 유효성·등록 종류 일치·중복 없음이다. manager는 TParts의 멤버 이름이나 localType 등 프로젝트 요소를 직접 해석하지 않는다. 생성/추출 왕복과 라우팅이 생성 입력의 DataType과 일치하는지는 codec 자체의 테스트에서 확인한다. 다른 종류의 CSV에 들어간 PK는 자동 이동·재생성하지 않는다. header-only 테이블 종류도 등록 메타데이터로 알 수 있으며 첫 행에서 추정하지 않는다.

## PK/FK 탐색

조회는 모든 테이블이 같은 세대로 묶인 DataTableSnapshot을 기준으로 한다. manager는 로드·현재 snapshot 공개를 소유한다. 소비자는 manager.Snapshot을 한 번 확보하고 관련 조회를 그 객체에서 수행한다. 여러 호출마다 최신 snapshot을 다시 읽어 세대를 섞지 않는다.

```csharp
var snapshot = await manager.LoadAsync(cancellationToken);
var table = snapshot.GetTable<IDataTable<TextRow>>(nameIdx);
var text = manager.Get<TextRow>(nameIdx);
bool found = manager.TryGet(imageIdx, out ResourceKeyRow resource);
var sameGenerationText = snapshot.Get<TextRow>(nameIdx);
```

DataTableManager와 DataTableSnapshot은 `TRow Get<TRow>(uint idx)`와 `bool TryGet<TRow>(uint idx, out TRow row)`를 제공하며 `where TRow : class, IDataRow`를 적용한다. GetRow/TryGetRow 초안 명칭을 대체하며 중복 alias를 만들지 않는다. dynamic 반환·비제네릭 행 Get·암시적 변환·typed ID wrapper는 이번 초기 구현에 포함하지 않는다. 제네릭 타입은 기대 DTO 검증에 쓰며 테이블 선택은 idx의 종류 코드가 담당한다.

공통 조회 흐름은 전체 idx 형식 검증 → 종류 registry → 등록된 DTO 타입과 typeof(TRow)의 정확한 일치 확인 → 동일 테이블에서 완전한 idx로 PK 조회다. 실패 시 다른 테이블을 스캔하거나 첫 T 구현을 반환하지 않는다. 같은 DTO가 서로 다른 종류로 등록되어 있어도 idx로 정확한 테이블을 선택한다. Base/interface로 조회하는 다형성 변환은 제공하지 않는다.

manager 조회는 기존 메인 스레드·종료 검사를 적용하고 현재 Snapshot을 한 번 확보해 위임한다. 최초 성공 전 Get/TryGet 모두 InvalidOperationException, Dispose 후 모두 ObjectDisposedException이다. router가 없는 수동 전용 snapshot의 idx API 호출도 InvalidOperationException이다. 재로드 중·실패 후에는 이전 정상 snapshot을 조회한다. 자동 LoadAsync·동기 비동기 대기·자산 로드는 수행하지 않는다. snapshot 조회는 manager의 종료 상태를 검사하지 않으며 보관한 이전 데이터·router를 계속 사용한다.

GetTable<TService>(uint idx)는 TryGetDataType → 종류 registry → 같은 테이블의 명시적 계약 binding으로 조회한다. 테이블 검색이므로 idx 행 존재는 보장하지 않으며 실제 참조는 Get/TryGet으로 해결한다. 이름 기반 API도 같은 테이블·dictionary를 사용한다.

| API | 실패 계약 |
|---|---|
| GetTable<TService>(idx) | 무효 idx: ArgumentException; 미등록 종류: KeyNotFoundException; 계약 불일치: InvalidOperationException |
| Get<TRow>(idx) | 무효 idx: ArgumentException; 미등록 종류: KeyNotFoundException; 정확한 DTO 타입 불일치: InvalidOperationException; 대상 PK 누락: KeyNotFoundException |
| TryGet<TRow>(idx, out row) | 무효 idx·미등록 종류·DTO 타입 불일치·대상 PK 누락: false, row=null. 상태/구성 오류·router 구현 예외는 전파 |

반환값은 snapshot의 관리 행 참조이며 복사본·자산 handle·새 게임 객체가 아니다. 같은 snapshot의 Get/TryGet/기존 dictionary 조회는 같은 행 객체를 반환한다. 이전 snapshot·테이블은 재로드·manager Dispose 후에도 기존 데이터를 유지하고 router·registry를 새 규약으로 교체하지 않는다.

## FK 선언과 검증

종류 추출만으로 FK가 유효해지지는 않는다. Product.NameIdx에는 존재하는 Text 행이 필요하며 존재하는 Resource 행이 들어가도 오류다. 예상 목적지는 소비 프로젝트가 선언한다.

기존 AddValidator 목록에 연결되는 FK helper:

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

1. codec의 경계·역함수 테스트를 Red부터 실행한다. 기본 IdxParts/Stride의 0/1·종류/local 0·local=stride-1/stride·uint.MaxValue를 확인한다. 프로젝트 세 요소 예제의 모든 요소 왕복·경계·동일 LocalIdx의 서로 다른 LocalType·TryGetDataType과 전체 추출 일치·무효 하위 요소·overflow도 확인한다.
2. uint 테이블 등록: router 누락/null/중복·종류 중복·설정 동결·종류가 다른 PK·header-only·잘못된 등록의 잔여 상태를 확인한다. 수동 Register 혼합은 기존 동작을 유지한다.
3. idx 기반 interface/DTO 조회: 실패 계약, 같은 DTO의 다른 종류, 이름/idx 조회의 동일 인스턴스·행을 확인한다.
4. FK: 필수/선택·0/null·잘못된 종류·누락 PK·자기/순환 참조·배열 프로젝트 검증을 확인한다. 재로드 후보의 대상 제거가 실패하고 기존 행/테이블/router가 보존되는지도 확인한다.
5. 공유 로드·취소·Dispose·씬 준비·전체 회귀를 확인한다. 새 DTO/codec의 Player·소비 프로젝트 경로는 후속 단계에서 검증한다.

uint 표준 키·생성/추출 규약·PK/FK 탐색·Parts/Stride·선택적 localType·제네릭 조회 기준 요구는 사용자 지시다. 기본 DecimalIdxCodec·0/null·오류 세부 정책은 구현 준비를 위한 선택이며 [구현 준비 문서](DATA_TABLE_GENERIC_IMPLEMENTATION.md)에 실행 범위와 완료 조건을 연결한다. 최초 기록은 [2026-10-02 회고](retrospectives/2026-10-02-02-data-table-idx-draft.md), Parts/Stride 보완은 [선행 회고](retrospectives/2026-10-06-01-idx-parts-stride.md), 이번 준비는 [현재 회고](retrospectives/2026-10-06-02-generic-data-table-ready.md)에 남긴다. 런타임 구현은 완료했다. 자동 migration은 제공하지 않는다. [구현 회고](retrospectives/2026-10-06-06-generic-data-table-validation.md)에서 현재 검증과 미실행 경계를 확인한다.
