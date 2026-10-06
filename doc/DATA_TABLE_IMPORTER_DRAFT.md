# 데이터 테이블 Editor importer 초안

2026-10-06. 상태: 검토 초안·미구현. 기준 `main/f51e2d3`. 요청 범위는 지정 폴더의 CSV/JSON 추가·변경 감지와 DTO·테이블 생성/수정 설계다. 자동 생성과 데이터 로드는 별개의 작업이며 현재 runtime 계약은 변경하지 않는다.

선행 계약: [매핑](DATA_TABLE_MAPPING_DRAFT.md), [uint idx](DATA_TABLE_IDX_DRAFT.md), [manager](DATA_TABLE_MANAGER.md), [예시 템플릿](templates/data-tables/README.md), [최신 회고](retrospectives/2026-10-06-07-data-table-templates.md).

## 권장 방향과 현재 구현의 경계

지정 폴더의 변경을 감지하는 공용 Editor 도구를 제공하고, 프로젝트가 명시한 스키마를 기준으로 프로젝트 namespace에 DTO와 테이블을 생성한다. 처음에는 CSV와 스키마 JSON을 지원하고, JSON 행 데이터는 로더 계약을 확정한 뒤 추가하는 안을 권장한다. 타입 추론은 최초 스키마 작성의 제안으로만 사용한다.

- 현재 core 기반은 `IDataRow`, `DataRow`, `IDataTable<TRow>`, `CsvDataTable<TRow>`다. Text/Resource는 테스트 assembly의 복사 가능한 예시이며 예약된 테이블 이름·종류가 아니다.
- `RegisterTable<TRow, TTable>`은 CSV 문자열 공급자와 새 테이블 factory를 받는다. JSON 행 데이터 파서·자동 폴더 탐색·자동 등록·임의 PK 코드 생성은 현재 구현에 없다.
- importer는 Editor assembly에 둔다. 생성한 DTO·테이블은 프로젝트 runtime assembly에 두며 UnityEditor나 importer를 참조하지 않는다. core와 테스트 fixture·읽기 전용 package 경로에 생성하지 않는다.
- 기존 manager의 명시적 등록, 같은 snapshot의 FK 검증, 전체 후보 성공 후 공개, 재로드 실패 시 이전 세대 유지 계약을 보존한다. Editor에서 파일이 바뀌었다고 실행 중 manager의 구성·snapshot을 자동 변경하지 않는다.

## 공용 규격과 프로젝트 규격의 구분

별도의 기본 manager/프로젝트 manager를 만들기보다 생성 모드와 코드 소유권을 구분한다.

| 구분 | 사용 방법 | importer의 권한 |
|---|---|---|
| 코어 기반 | 표준 uint idx 행·CSV 테이블 기반을 상속 | 기반 타입과 core 소스 수정 금지 |
| 예시 템플릿 | Text/Resource 쌍을 복사해 프로젝트 규격으로 변경 | 명시적으로 선택했을 때만 시작점으로 사용; 자동 설치/등록 금지 |
| 프로젝트 생성 타입 | 스키마에서 partial DTO·테이블 생성 | 생성기가 소유하는 `.g.cs`만 갱신 |
| 프로젝트 기존 타입 | 기존 DTO·ClassMap·table·interface 지정 | 입력 검증만 수행; 사용자 소스 재작성 금지 |
| 프로젝트 임의 PK | 수동 `Register<TKey, TRow>` 사용 | 1차 생성 대상에서 제외; 별도 키/매핑 계약 후 확장 |

같은 `TextRow` 이름이어도 namespace가 다르면 다른 C# 타입이다. manager의 이름·dataType 중복 등록은 여전히 오류이며, 상속의 `new`나 후등록으로 교체하지 않는다. 프로젝트 root가 최초 등록에서 사용할 구현을 선택한다. 같은 DTO를 여러 종류에 등록하는 기존 계약도 유지하며, 여러 입력이 같은 생성 파일을 갱신하는 구성은 명시적인 단일 스키마 소유자 없이는 거부한다.

기존 DTO가 `IDataRow`를 직접 구현하거나 사용자 ClassMap으로 idx를 매핑하는 경우도 검증 모드에서 허용한다. 기존 테이블의 interface binding·converter·검증 hook은 사용자가 소유하며 열 목록만 보고 새 interface를 자동 설계하지 않는다.

## 입력과 스키마의 기준

프로젝트가 입력 폴더, 생성 출력 폴더, namespace, 자동 적용 여부를 설정한다. 1차는 Unity Asset Database가 관리하는 `Assets` 아래 폴더를 대상으로 하고 출력은 입력 바깥에 둔다. 외부 폴더 감시·FileSystemWatcher는 후속 요구가 있을 때 검토한다.

CSV header는 이름과 순서만 제공하므로 실제 데이터 값에서 필드 타입을 확정하지 않는다. 빈 테이블·선행 0 문자열·큰 정수·null·enum·문화권별 숫자가 추론을 흔들 수 있다. CSV 추가 시 등록되지 않은 파일은 검토 목록에 올리고, 승인된 스키마가 있을 때 생성/검증한다. `string`이라는 자동 기본값도 확인 없이 계약으로 확정하지 않는다.

아래 JSON은 제안 스키마 예시이며 아직 구현된 파일 형식/API가 아니다. `dataType=1`도 예시일 뿐 프로젝트가 결정한다.

```json
{
  "schemaVersion": 1,
  "tableId": "texts",
  "input": "Assets/Game/Data/texts.csv",
  "format": "csv",
  "mode": "generated",
  "namespace": "Game.Data",
  "rowType": "TextRow",
  "tableType": "TextDataTable",
  "dataType": 1,
  "columns": [
    { "name": "idx", "member": "Id", "type": "uint", "required": true },
    { "name": "text", "member": "Text", "type": "string", "required": true }
  ]
}
```

- Id는 `DataRow`에서 상속하며 생성 코드에서 중복 선언하지 않는다. idx는 프로젝트 생성기로 이미 만든 완전한 PK다. importer는 idx 생성·재번호 부여·Stride/localType 자동 결정·데이터 값 수정을 수행하지 않는다.
- 필드 타입은 1차에 지원 목록을 제한한다. 권장 초기 목록은 string/bool/int/uint/long/ulong/float/double/decimal과 지원하는 nullable 값 타입이다. enum·배열·중첩 객체·사용자 converter는 기존 타입 검증 모드 또는 후속 확장으로 다룬다. 지원하지 않는 타입은 string으로 묵시 변환하지 않고 진단한다.
- `required`는 열 존재 요구다. 빈 문자열·null 허용, 기본값, 범위·게임 규칙은 별도 매핑/검증 계약으로 다룬다. `[Optional]`을 임의로 추가해 누락을 숨기지 않는다.
- header 대소문자·중복/공백·필드 수·인용·InvariantCulture·쉼표 구분은 현재 CSV 계약을 따른다. 이름은 `[Name]` 또는 ClassMap으로 연결하고 C# 식별자를 검증한다. 예약어·잘못된 이름·정규화 충돌은 명시적 member 설정을 요구하며 코드 문자열과 식별자는 안전하게 출력한다.
- namespace·동일 assembly의 타입 이름·테이블 이름·dataType·출력 경로 중복을 적용 전에 검사한다. 경로는 정규화하고 지정 root 밖으로 나가는 `..`·심볼릭 링크/재분석 지점 경로와 읽기 전용 입력/출력을 거부한다.
- source GUID로 입력 이동을 추적하고 `tableId`로 계약을 식별하는 안을 권장한다. 파일명 변경만으로 타입·테이블 이름을 바꾸지 않는다. 내용 hash는 중복 처리를 막는 용도이며 식별자나 승인 근거가 아니다.
- 스키마 JSON의 버전·필수 필드·중복 키·알 수 없는 옵션을 검사한다. JSON 파서 선택은 설치된 기능의 실제 지원 범위 확인 후 확정하며 새 의존성을 초안 단계에서 추가하지 않는다.

## 변경 감지와 생성 흐름

Unity는 CSV/JSON을 기본 TextAsset으로 처리한다. [Text assets](https://docs.unity3d.com/6000.3/Documentation/Manual/class-TextAsset.html), [importer 안내](https://docs.unity3d.com/6000.3/Documentation/Manual/ScriptedImporters.html)를 확인했다. 내장 importer 재정의는 1차에서 사용하지 않고 `AssetPostprocessor.OnPostprocessAllAssets`와 수동 검사/생성 메뉴를 공유하는 안을 권장한다. Unity 6.3 문서에 있는 `overrideExts` 가능성을 일반적인 확장자 사용 금지로 설명하지 않는다.

1. [OnPostprocessAllAssets](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html)에서 입력·스키마의 추가/변경/이동/삭제만 수집한다. 출력 폴더·소유권 기록·stage 파일은 감지 대상에서 제외한다.
2. import callback 밖의 지연 Editor 처리로 요청을 합치고, compile/import/Play 전환 중에는 쓰기를 보류한다. 프로젝트 설정·스키마·입력의 최신 hash를 확인하고 처리 도중 변경됐으면 새 요청으로 다시 검사한다.
3. 같은 순수 생성 함수로 입력·스키마 검증 → 생성 결과/diff → 진단을 만든다. Inspector/메뉴와 자동 감지는 이 경로를 공유한다. 중복 이벤트·Editor 재시작·domain reload 후에도 설정을 기준으로 재검사할 수 있어야 한다.
4. 데이터 값만 변경되면 재검증하고 `.cs`는 쓰지 않는다. header/스키마 변경은 차이를 표시한다. 기본은 검토 후 적용이며 프로젝트가 자동 적용을 켰을 때도 승인된 스키마와 소유권 검사를 통과한 생성 파일에만 적용한다. 삭제·이름/타입 변경 같은 계약 변경은 자동 적용에서 제외한다.
5. 전체 생성 대상의 경로·소유권·진단을 먼저 확인하고 임시 위치에 stage한다. 문제 없는 후보만 main Editor에서 반영하고 바뀐 파일만 import한다. [refresh 과정](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetDatabaseRefreshing.html)의 재시작·C# 컴파일을 고려해 중복 쓰기·무한 refresh를 막는다.
6. 소스 생성 성공과 컴파일 성공을 별도 상태로 표시한다. 새 타입을 쓰는 검증/등록은 컴파일 완료 후 수행한다. 생성 실패는 기존 소스를 유지하며 컴파일 실패는 이전 실행 snapshot을 건드리지 않고 실패한 파일/진단을 남긴다.

지연 실행만으로 재진입 문제가 해결됐다고 간주하지 않는다. 동일 내용 쓰기 생략, 생성 출력 무시, 처리 중복 방지, domain reload 후 일치 확인이 함께 필요하다. 여러 소스 파일의 반영을 DB 트랜잭션처럼 원자적이라고 주장하지 않고, 반영 중 I/O 실패의 원본 복구와 부분 반영 진단을 구현·시험한다.

## 생성 코드와 사용자 코드의 소유권

- 프로젝트 runtime의 지정 출력에 `TextRow.g.cs`, `TextDataTable.g.cs` 같은 partial 타입을 생성한다. 생성 DTO는 DataRow를 상속하고 필드 속성/프로퍼티를 제공하며 생성 테이블은 `CsvDataTable<TextRow>`를 상속한다. 구체 이름은 스키마가 소유한다.
- 사용자는 별도 `TextRow.cs`/`TextDataTable.cs` partial에서 편의 기능·interface·행/테이블 검증·추가 ClassMap을 작성한다. 생성기가 validation hook을 함께 override하지 않아 사용자 정의와 충돌하지 않게 한다. 생성 후 스키마 필수 열을 바꾸는 사용자 ClassMap은 실제 매핑 검증에서 드러내야 한다.
- `.g.cs` 내용과 생성 소유권 기록은 Git에 포함한다. 안정적인 schema/table/source GUID·generator version·마지막 생성 내용 hash만 기록하고 timestamp·사용자 절대 경로는 출력하지 않는다. schema와 manifest의 구체 저장 형태는 구현 전에 확정한다.
- 기존 `.g.cs`가 마지막 생성 hash와 다르면 사용자 수정으로 보고 중단한다. 헤더 주석/확장자만으로 소유권을 인정하지 않으며 알 수 없는 기존 파일은 덮어쓰지 않는다. 사용자는 변경을 partial로 이동하거나 명시적으로 소유권을 재설정할 수 있다.
- 갱신은 기존 파일의 `.meta`·GUID를 보존한다. 입력 삭제 시 해당 테이블을 누락 상태로 표시하고 원본·생성 코드·등록을 자동 삭제하지 않는다. 이동 시 GUID로 연결을 유지한다. 코드 경로/타입 이름 변경과 폐기는 참조 확인 후 별도 적용한다.
- 생성 순서·UTF-8·줄바꿈·타입/컬럼 표현을 결정적으로 출력한다. CSV 열 순서/데이터만 달라진 재import가 불필요한 소스 변경·Git conflict를 만들지 않게 한다. 스키마 파일도 테이블별로 분리해 다수 작업자의 충돌 범위를 줄인다. localType 분할은 PK 작업 범위이며 코드 생성 충돌을 해결하는 기능으로 혼동하지 않는다.

## manager 연결과 PK/FK 검증

1차 생성 결과는 DTO·빈 구체 테이블과 등록 안내까지다. 프로젝트 installer는 router, dataType/name, CSV 공급자, 매 로드 새 테이블 factory를 기존 API로 명시적으로 등록한다. 자동 assembly 탐색·전역 singleton·등록 코드 덮어쓰기는 추가하지 않는다.

기존 타입 검증 모드는 컴파일된 DTO/ClassMap·table hook을 이용해 입력을 확인한다. 단일 파일 구문/매핑 검사와 프로젝트 router가 필요한 종류 검사, 모든 대상 테이블이 필요한 FK 검사를 구분한다. 전체 검증은 프로젝트가 구성한 임시 manager로 같은 후보 snapshot에서 수행하고 기존 runtime manager/snapshot에 공개하지 않는다. importer가 게임 router를 추측하거나 생성하지 않는다.

FK 필드의 uint라는 사실만으로 관계를 추정하지 않는다. 대상 종류·DTO·선택/필수 정책을 프로젝트가 명시하고 기존 `RegisterForeignKey`/`AddValidator`로 연결한다. 자체 interface binding도 프로젝트 소유다. Editor 검증을 통과해도 runtime LoadAsync의 입력·종류·PK/FK·전체 snapshot 검증은 생략하지 않는다.

임의 PK 모드는 이후 추가한다면 TKey·selector·매핑/공급자 등록까지 별도 정의해야 한다. 이를 표준 `Get<TRow>(uint idx)`에 자동 라우팅하거나 표준 uint DTO로 억지 변환하지 않는다.

## JSON 행 데이터는 별도 결정

스키마 JSON과 테이블 행 JSON은 역할이 다르다. `format`과 스키마의 명시적 연결로 구분하며, 폴더 내 모든 JSON을 행 데이터로 처리하지 않는다.

JSON 행을 지원할 때는 배열/객체 envelope, idx의 숫자 표현·uint 범위, 누락/null/빈 문자열, 중복 속성, 숫자 정밀도, 중첩 객체/배열, enum·converter 정책을 먼저 확정한다. 동일한 IDataRow·테이블 조회 계약을 제공할 수 있어도 현재 CsvDataTable 기반의 파싱을 그대로 지원한다고 볼 수 없다.

권장 후속 검토는 JSON을 Editor에서 검증 후 결정적인 CSV 산출물로 변환해 기존 runtime 공급자를 사용하는 안이다. 이 경우 원본 JSON과 산출 CSV의 소유권·배포 갱신/누락 검사·숫자/문자열 변환·추가 산출물 비용이 필요하다. 런타임 직접 JSON 로드는 별도 파서/테이블 기반 계약 변경이다. 어느 경로도 구현 없이 지원 완료로 표시하지 않는다.

## 구현 단위와 완료 조건

| 단계 | 구현 범위 | 최소 검증/완료 조건 |
|---|---|---|
| 1. 계약·수동 생성 | 설정/스키마 확정, CSV parser와 결정적 DTO/table 생성, diff·진단 | 빈/header-only·인용/줄바꿈·큰 uint·열 누락/중복·식별자/경로 충돌·타입 오류의 EditMode Red/Green; 같은 입력은 같은 출력 |
| 2. 감지·반영 | 지정 폴더 감지·요청 병합·소유권/manifest·stage·수동/선택적 자동 적용 | 추가/수정/이동/삭제·중복 알림·domain reload·compile·부분 저장·I/O 실패·사용자 수정 보호·무한 refresh 방지 |
| 3. 프로젝트 연결 | 생성 타입 컴파일·기존 타입 검증·installer 등록·전체 후보 검증 | generic 조회·interface/검증 hook 유지·PK/종류/FK 실패·이전 snapshot 보존·씬 준비 연결; 기존 관련 테스트 통과 |
| 4. JSON 행 | 데이터 형태/변환 또는 runtime 로더 계약 확정 후 별도 구현 | JSON 특유 경계·CSV와 타입/조회 의미 일치·배포 산출물 검증; 필요한 소비 프로젝트/Player 검사 |

2단계에는 실제 기존 MyLab Editor에서 파일 변경→감지→한 번의 생성→컴파일 완료와 재import 무변경을 자동 실행으로 확인한다. 사용자가 직접 확인할 필요가 있는 Inspector diff/진단 UX가 남으면 구체적인 절차와 기대 결과를 전달하고 구현 병합을 보류한다. 현재 문서 초안의 병합은 구현 채택 승인이나 이러한 실행 검증을 의미하지 않는다.

실제 테스트/컴파일/Console/Player 실행: 이번 문서 작업에서는 모두 미실행(테스트 0건). 문서 링크·색인·diff/공백·변경 범위·기존 사용자 변경 보존만 확인한다.

## 구현 전 확정할 선택

제안 기본값은 CSV 우선, 명시적 스키마, 생성/기존 타입 검증의 두 모드, 감지는 자동·계약 변경 적용은 검토 후, DTO/table만 생성, 프로젝트 installer 수동 등록이다. 지원 필드 타입과 null/default 계약, 설정/소유권 기록의 저장 형태, JSON 행 지원 경로는 구현 단계 전에 확정한다. Text/Resource 재도입·전체 테이블 자동 등록·새 패키지 설치로 초안 범위를 넓히지 않는다.
