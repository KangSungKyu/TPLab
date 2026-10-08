# 데이터 테이블 Editor importer 계약

2026-10-06. 상태: CSV 1차 구현·TPLab 검증. 구현 기준 `main/19b5770`, 작업 `codex/data-table-importer`. 기존 초안의 경로를 유지하며 현재 동작을 이 문서에서 관리한다. [실행 증거](validation/data-table-importer/README.md), [회고](retrospectives/2026-10-06-10-data-table-importer.md)를 함께 확인한다.

선행 계약: [manager](DATA_TABLE_MANAGER.md), [DTO 매핑](DATA_TABLE_MAPPING_DRAFT.md), [uint idx](DATA_TABLE_IDX_DRAFT.md), [예시 템플릿](templates/data-tables/README.md). Text/Resource는 예약된 타입이 아니며 생성 이름·필드·종류는 프로젝트가 소유한다.

## 설정과 시작 방법

`TPLab.Core.Editor.DataTables.DataTableImportSettings`는 Editor 전용 ScriptableObject다. `Tools/TPLab/Data Tables/Create or Select Active Settings`로 **Assets/Editor/TPLab/setting.asset**을 만들고 입력·스키마 폴더를 준비한다. 설정과 meta를 프로젝트 Git에 포함한다. 설정이 없으면 자동화는 꺼지며 자동 생성하지 않는다. 다른 위치의 설정 자산은 Inspector 수동 명령만 사용할 수 있다.

| 설정 | 현재 계약 |
|---|---|
| AutomationMode | Disabled 기본값 / ValidateOnly / GenerateValidated |
| InputFolder | CSV 입력 root, 기본 Assets/Game/Data |
| SchemaFolder | 테이블별 JSON 정의 root, 기본 Assets/Game/DataSchemas |
| OutputFolder | 프로젝트 runtime 생성 소스 root, 기본 Assets/Game/Generated/Data |
| DefaultNamespace | 스키마 namespace 생략 시 적용, 기본 Game.Data |
| ValidationProfileId | 프로젝트가 명시적으로 등록한 Editor 검증 구성 식별자 |

- Disabled는 자동 실행만 막는다. Inspector의 **Validate All Tables**, **Generate and Validate**는 모든 모드에서 동일한 필수 검증을 수행한다.
- ValidateOnly는 소스를 쓰지 않고 입력·소유권·실제 타입을 검증한다. 스키마에 맞는 생성 소스/타입이 없으면 컴파일 또는 구성 대기로 남는다.
- GenerateValidated는 유효한 신규 스키마를 잠정 생성하고 컴파일 뒤 전체 검증한다. **기존 스키마 계약의 모든 변경은 자동 반영을 거부**한다. 필드 추가도 검토 후 수동 Generate and Validate로 적용한다. 데이터·header 순서 변경은 타입을 다시 쓰지 않는다.
- 생성 타입/파일 이름 변경은 수동 명령에서도 거부한다. 참조·GUID·소유권을 확인하는 별도 migration 작업이 필요하다. 삭제된 입력·스키마에 대응하는 소스를 자동 삭제하지 않는다.

## 입력 형식

SchemaFolder의 `.json`은 정의이며 InputFolder의 `.csv`는 행 데이터다. 입력 CSV와 스키마는 명시적으로 연결하고 미등록 CSV는 생성/검증 대상에 넣지 않는다. 스키마를 하위 폴더까지 읽지만 CSV 값에서 타입을 추론하지 않는다. JSON 행 로더는 제공하지 않는다.

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

`mode`는 generated 또는 existing이다. generated는 DataRow를 상속하는 partial DTO와 CsvDataTable을 상속하는 partial 테이블의 `.g.cs` 한 쌍을 만든다. existing은 정확한 기존 DTO/table과 스키마의 프로퍼티 타입을 확인하고 사용자 소스를 쓰지 않는다. 기존 ClassMap·hook·interface binding은 프로젝트가 소유한다.

- 필드는 string/bool/int/uint/long/ulong/float/double/decimal과 nullable 값 타입을 지원한다. string?·enum·배열·중첩 객체는 이 importer의 스키마에 넣을 수 없다. 기존 타입도 명시한 열은 같은 제한과 사전 변환 검사를 받는다. 복잡한 converter/임의 PK 테이블은 기존 runtime 수동 등록을 사용한다.
- `idx`는 이미 프로젝트 생성기로 완성한 PK다. 필수 uint Id로 연결하며 DataRow에서 상속한다. importer는 idx 생성·재번호·Stride/localType 결정을 하지 않는다.
- `required`는 열 존재 여부다. 빈 문자열·nullable 값의 의미·범위·게임 정책은 실제 table hook/프로젝트 validator가 결정한다. optional 열은 생성 DTO에 [Optional]로 표시한다.
- 중복/공백 header, 행 필드 수, 미사용 필드의 잘못된 인용, invariant 쉼표 구분, 중복 PK·uint 범위·0·router 종류 불일치를 검사한다. 올바른 header-only CSV는 허용한다.
- JSON 버전·필수 필드·중복 키·알 수 없는 속성·지원하지 않는 타입을 거부한다. C# 이름은 ASCII 식별자와 유효 namespace만 허용하며 생성 프로젝트 타입의 TPLab.Core namespace 사용은 거부한다.
- Assets 밖, `..`, 재분석 지점, Editor 출력, core/tests/validation 출력, 입력/스키마와 겹치는 출력 root는 거부한다. 경로·파일명·종류·테이블 이름 충돌은 쓰기 전에 검사한다.

JSON 파서는 이미 설치된 Unity Newtonsoft.Json **3.2.2**을 Editor에서 사용한다. 패키지 추가/변경은 없으며 runtime CSV 파서는 CsvHelper **33.1.0**을 유지한다.

## 프로젝트 검증 연결

설정은 delegate·Type·서비스를 직렬화하지 않는다. 프로젝트 Editor 초기화 함수에서 **DataTableImportProfiles.Register**로 router factory와 typed 등록 함수를 명시적으로 연결한다. 프로젝트 Editor asmdef는 TPLab.Core와 TPLab.Core.Editor를 참조하고 생성 runtime 타입을 볼 수 있어야 한다.

```csharp
using TPLab.Core.DataTables;
using TPLab.Core.Editor.DataTables;
using UnityEditor;
using Game.Data;

internal static class ProjectTableImportProfile
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        DataTableImportProfiles.Register("Game.Tables", () => new DecimalIdxCodec(1000), context =>
        {
            context.RegisterTable<TextRow, TextDataTable>("texts", () => new TextDataTable());
            // context.Manager.BindTable / RegisterForeignKey / AddValidator:
            // runtime과 같은 프로젝트 규칙을 이 임시 manager에 연결한다.
        });
    }
}
```

예시의 dataType=1·DecimalIdxCodec(1000)은 프로젝트 선택이며 공용 고정 규격이 아니다. ValidationProfileId를 Game.Tables로 지정한다. 테이블 factory는 매번 새 인스턴스를 만들고 모든 스키마를 정확한 DTO/table로 등록해야 한다. importer가 고정한 CSV 공급자를 context.RegisterTable이 연결한다. 추가 수동 테이블을 임시 Manager에 등록하는 구성은 입력 집합 불일치로 실패한다.

**최초 생성**은 아직 DTO가 없으므로 router와 빈 configure(`context => { }`)로 profile을 등록한 뒤 Generate and Validate를 실행한다. 사전검사 후 소스가 컴파일되지만 구성 대기로 남는다. 생성한 타입으로 위 typed 등록을 연결하고 재검증해야 완료된다. production importer는 assembly 탐색으로 자동 등록하거나 Activator로 타입을 생성하지 않는다. 타입 이름 충돌·컴파일 fingerprint 검사는 등록과 별개다.

FK 대상 DTO·종류·필수/null 정책은 프로젝트가 RegisterForeignKey로 명시한다. uint 필드라는 이유로 관계를 추정하지 않는다. 전체 후보의 동일 snapshot에서 table.ValidateRow/ValidateTable, ClassMap, FK, AddValidator를 실행한다. interface binding 역시 임시 manager에 명시적으로 연결한다. 검증 hook은 외부 상태 변경·자산 로드 없이 동작해야 한다.

## 필수 검증과 수명

Runtime의 기존 CSV reader와 표준 idx 검사를 **DataTableCsvValidator.Read / ValidateIdx**로 추출했다. manager와 Editor 사전검사가 같은 구조·키 검사를 사용한다. 수동 Register<TKey,TRow>의 임의 PK 규칙과 uint0 허용은 유지한다. 공용 필수 검사를 끄는 설정은 없다.

1. 설정·프로젝트 router·전체 스키마·입력을 고정하고 공용 CSV/변환/표준 PK 검사를 실행한다. 구성이 없거나 실패하면 쓰지 않는다.
2. 모든 생성 대상의 소유권과 충돌을 사전 확인하고 변경된 `.g.cs`만 잠정 반영한다. 컴파일 대기와 전체 검증 완료를 구분한다.
3. 현재 코드가 컴파일되면 매 시도 새 임시 DataTableManager에 typed 구성을 연결하고 LoadAsync로 전체 후보를 검증한다. 실제 매핑/hook/FK를 사전검사가 대체하지 않는다.
4. 입력·설정·스키마·profile revision·컴파일된 core/Editor/profile/DTO/table 식별자·출력 소스 fingerprint가 현재 시도와 일치해야 Validated가 된다. 변경/취소/컴파일 오류는 이전 결과를 성공으로 표시하지 않는다.
5. 취소/실패/완료 모두 임시 manager를 main thread에서 Dispose한다. runtime manager·씬·자산 서비스에 공개하지 않는다. RunAsync는 main Editor 호출만 허용한다.

상태는 Disabled, AwaitingConfiguration, AwaitingCompilation, Validated, Failed, Cancelled다. Inspector에 최근 상태와 진단을 표시하며 입력/설정 변경 시 이전 성공을 즉시 대기로 무효화한다. worker-thread 호출은 Unity API 접근 전에 거부한다. profile 교체도 결과를 무효화한다.

## 감지·소유권·실패 처리

[OnPostprocessAllAssets](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html)는 경로 이벤트만 전달한다. import callback 밖 EditorApplication.update에서 요청을 합치고 import/compile/Play 전환 중에는 실행하지 않는다. 출력 변경 이벤트는 입력으로 취급하지 않으며 domain reload 후 다시 검증한다. 같은 내용의 재import는 소스를 재작성하지 않는다.

- 스키마 GUID가 `<owner>.tableimport.json` 소유자를 결정한다. manifest는 소스 hash·입력 GUID·마지막 성공 fingerprint·마지막 검증 성공 소스를 보관한다. 타임스탬프/사용자 절대 경로는 넣지 않는다. `.g.cs`, manifest와 meta를 Git에 포함한다.
- 생성 파일의 현재 hash가 소유 기록과 다르면 사용자 수정으로 거부한다. 알 수 없는 `.g.cs`/meta·기존 컴파일 타입을 덮어쓰지 않는다. 기존 meta/GUID를 유지하고 편의 기능·interface·validation override는 별도 partial에 작성한다.
- CSV 이동은 현재 존재하고 GUID가 일치하는 경로로 따라간다. 삭제된 GUID의 Unity 캐시 경로를 읽지 않는다. 이동 후 삭제된 CSV를 다시 만들면 스키마 input 경로로 재연결하고 성공 검증 뒤 새 GUID를 기록한다.
- 소스 한 쌍과 manifest를 같은 폴더에 stage하고 I/O 실패 시 기존 bytes를 복구하고 stage를 정리한다. Windows의 일시적 sharing/lock/교체 대상 제거 오류에만 같은 원자적 교체를 최대5회·50ms 간격으로 시도한다. 영구 오류·소진은 실패를 전파하며 임의 덮어쓰기로 우회하지 않는다. 여러 테이블·파일 전체를 DB처럼 원자적으로 쓰는 계약은 아니다. 모든 테이블 사전검사 이후에도 I/O 실패로 이전 쌍의 잠정 반영이 남을 수 있으며 Failed로 진단한다.
- 사후 컴파일/hook/FK 실패는 잠정 소스와 진단을 남긴다. 임의 사용자 변경을 자동 rollback하거나 runtime 데이터를 공개하지 않는다. manifest 성공 기록은 전체 검증 후에만 갱신한다. 복원/소유권 재설정 UI는 제공하지 않는다.

## 검증과 후속 범위

집중 테스트는 `TPLab.Core.Tests.DataTableImporterTests`다. 실제 asset 감지→생성→컴파일/domain reload→typed 검증, 데이터 수정·이동·삭제·모드 전환·컴파일 오류/복구는 `DataTableImportEditorCheck.Run()`으로 같은 Editor에서 확인한다. 이 검사는 자신의 임시 자산/setting.asset만 만들고 정리하며 기존 활성 설정이 있으면 시작을 거부한다. [현재 실행 수·소스 hash·Console](validation/data-table-importer/README.md)을 확인한다.

JSON 행, 자동 타입 추론·등록, 임의 PK 생성, 외부 폴더 감시, rename migration, 소유권 복원 UI는 후속 요청 범위다. GameSceneManager와 소비 프로젝트 가져오기·Player/IL2CPP 검증은 기존 다음 단계이며 TPLab 내부 검증을 배포 호환성으로 확대하지 않는다.
