# 제네릭 데이터 조회 구현 준비

2026-10-06. 기준 `main / 9d79dacb3eb3347dee29d7a6241700de1f7d3e8b`, 문서 branch `docs/generic-data-table-ready`. 상태는 구현 준비이며 런타임 미구현이다. 사용자는 우선 generic 형식을 기준으로 구현 준비를 요청했다. 이 문서는 작업 순서·수정 경계·완료 조건을 소유한다. 조회/idx 계약은 [idx 명세](DATA_TABLE_IDX_DRAFT.md), DTO/CSV/테이블 binding 계약은 [매핑 명세](DATA_TABLE_MAPPING_DRAFT.md)가 소유하며 중복 정의하지 않는다.

## 목표와 준비 기준

소비자는 최종 생성된 uint PK를 입력하고 테이블 이름·구간·조합 요소를 전달하지 않는다. 아래는 구현 후의 목표 사용법이며 현재 컴파일되는 예제가 아니다.

```csharp
TextRow text = manager.Get<TextRow>(nameIdx);
bool found = manager.TryGet(imageIdx, out ResourceKeyRow resource);

// 여러 참조를 같은 데이터 세대로 사용
var snapshot = manager.Snapshot;
TextRow sameGenerationText = snapshot.Get<TextRow>(nameIdx);
```

- 사용자 요구: uint 표준 PK/FK, 프로젝트 규약 등록, Parts/Stride 동시 지원, 선택적 localType, idx 자동 탐색과 제네릭 DTO 검증.
- 초기 구현 선택: 기본 IdxParts/DecimalIdxCodec, 표준 0 PK/FK 거부·선택 FK null, 정확한 DTO 타입 일치, Get/TryGet의 명시적 실패 정책. 이는 준비 단계의 설계 선택이며 모든 세부 정책을 사용자가 직접 지정했다는 뜻은 아니다.
- generator/전체 extractor는 작성·분류·규약 검증 도구로 제공하며 manager는 IIdxRouter만 등록받는다. 완성된 CSV PK를 읽을 때 Generate를 다시 호출하지 않는다.
- manager는 기존 dictionary·snapshot을 재사용한다. 전역 PK 중복 색인·dynamic 반환·비제네릭 행 Get·GetRow alias·typed ID wrapper·전체 테이블 검색을 추가하지 않는다.
- 동적 테이블/DTO 탐색과 C# dynamic 반환을 구분한다. IL2CPP는 dynamic을 지원하지 않으므로 runtime API에서 제외한다. [Unity 6.3 제한](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-restrictions.html)

## 확인한 선행 코드와 영향 경계

| 현재 위치 | 구현 시 연결할 지점 |
|---|---|
| [DataTableManager](../Assets/MyLab/Core/DataTables/DataTableManager.cs) | Registration·ReadTable·LoadAndPublishAsync 확장; 공통 CSV 검사·단일 공개·EnsureOpen 유지 |
| [DataTableSnapshot](../Assets/MyLab/Core/DataTables/DataTableSnapshot.cs) | 같은 세대의 종류/DTO/계약 메타데이터와 router 보관; 제네릭 조회·테이블 binding 추가 |
| [EditMode 계약 테스트](../Assets/MyLab/Tests/EditMode/DataTableManagerTests.cs) | 기존 수동 파싱·header·FK·실패 보존 회귀 유지; 표준 경로 테스트 추가 |
| [비동기 테스트](../Assets/MyLab/Tests/PlayMode/DataTableManagerAsyncTests.cs) | 공유 로드·취소·등록 동결·Dispose·worker 완료 회귀 유지 |
| [소비 fixture](../Assets/MyLab/Tests/Fixtures/DataTableConsumerProbe.cs) · [ResourceManagerTests](../Assets/MyLab/Tests/PlayMode/ResourceManagerTests.cs) | 기존 수동 연결 보존; 표준 DTO/router 준비 후 씬 진행·실패 rollback 경로 검증 |
| [Core assembly](../Assets/MyLab/Core/MyLab.Core.asmdef) | 기존 경계·참조 재사용; 새 assembly/패키지/DLL을 추가하지 않음 |

현재 Registration은 이름·ReadAsync만, snapshot은 이름별 dictionary만 보관한다. 표준 registry·DTO·codec·Get/TryGet은 아직 없다. LoadAndPublishAsync에서 모든 후보 구성 뒤 AddValidator 실행, 성공 뒤 한 번 공개하는 순서를 유지한다. 기존 테스트 fixture는 임의 int/string key와 수동 GetTable을 사용하므로 자동 전환하지 않는다.

수정 허용 범위는 `Assets/MyLab/Core/DataTables/`, 관련 EditMode/PlayMode 테스트·fixtures와 해당 새 파일의 meta, 관련 명세·검증 자료·회고다. 실제 구현 단위마다 더 좁은 allowlist를 정한다. 공용 namespace는 MyLab.Core.DataTables, 코드·TDD·Git 규칙은 [AGENTS.md](../AGENTS.md)를 따른다.

제외: Cashier 변경/코드 복사, Unity scene·prefab·settings, 패키지·기존 assembly 설정, 다른 코어 시스템, 게임 enum·상품 DTO·UI·CSV migration. 기존 InitScene 및 SceneTemplateSettings 사용자 변경을 보존한다. 추가 수정이 실제로 필요하면 영향·이유를 먼저 검토하며 이번 준비를 자동 확장하지 않는다.

## 단계와 완료 조건

| 단계 | 응집된 구현 단위 | 실제 실행할 최소 검증 |
|---|---|---|
| 1 | 기본 IdxParts·DecimalIdxCodec·IIdxRouter/IIdxCodec<TParts>, IDataRow/DataRow 계약 | Parts/Stride 경계·전체 형식·왕복·uint.MaxValue/overflow. 테스트 전용 세 요소 codec으로 LocalType 선택성·종류 추출 확인 |
| 2 | 표준 RegisterTable·CsvHelper DTO 매핑·종류 registry·manager/snapshot Get/TryGet | 최종 PK 조회의 정상/무효/미등록/잘못된 T/누락, 동일 DTO 다른 종류, header-only 메타데이터, 기존 수동 등록 혼합·0 key 보존 |
| 3 | 기반/기본 테이블 조회·name/interface binding·FK helper를 표준 공개 경로에 연결 | 동일 행·테이블 참조, 추가 interface mapping, factory null/예외/재사용, 필수/선택 FK·잘못된 종류·누락·자기/순환·후보 실패 후 이전 세대 보존 |
| 4 | root/native CSV 소비 검증과 전체 회귀·실행 문서 | 준비 완료 후 씬 진행·오류 시 중단/가림막 유지·정리, 기존 비동기/자산/root 회귀, MyLab 컴파일/Console·전체 EditMode/PlayMode |

단계 2의 RegisterTable에는 매핑 명세의 CsvDataTable 기반·fresh factory·단일 행 연결 등 등록에 필요한 최소 부분도 포함한다. 단계 3에서 그 계약을 바꾸는 별도 테이블 생성 경로를 만들지 않는다. FK가 없는 테스트의 후보 공개도 단계 2부터 원자적으로 확인한다. 앞 단계 코드·실행 증거가 존재해야 다음 단위를 시작한다.

각 동작은 실행 가능한 최소 실패 테스트 Red → 최소 구현 Green → 보존 정리 순서다. 미구현 API 때문에 assembly가 컴파일되지 않으면 Red 실행으로 기록하지 않는다. 필요한 최소 선언/미구현 본문 또는 reflection 기반 검사로 테스트가 실제로 실행되고 관찰 가능한 계약에서 실패하도록 준비한다. 현재 과거 결과 [61 EditMode/82 PlayMode 기록](validation/data-tables/README.md)은 회귀 항목의 참고이며 새 기능의 Green 증거가 아니다.

## 집중 검증할 불변 조건

1. router는 전체 형식을 검사하고 종류 코드만 반환한다. manager는 등록 DTO 타입과 typeof(TRow)를 비교한다. LocalType은 테이블 선택을 바꾸지 않으며 PK 조회에는 전체 idx를 쓴다.
2. registry·기본 binding 등록 실패는 일부 상태를 남기지 않는다. router 누락/잘못된 구성은 I/O·구성 동결 전에 거부하며 사전 취소·빈 등록 규칙도 유지한다.
3. 조회 시 자동 Load/Wait/자산 로드가 없다. manager 미준비·종료·worker 호출은 상태 오류, TryGet은 예상 가능한 키/타입 불일치만 false로 처리한다. router의 구현 예외를 false로 숨기지 않는다.
4. FK는 예상 종류와 실제 대상 PK를 같은 후보 snapshot에서 확인한다. 이전 공개 snapshot이나 다음 로드 결과로 보완하지 않는다.
5. 모든 로드/검증 성공 후 dictionary·테이블·registry를 한 snapshot으로 공개한다. 실패·취소·Dispose 후 늦은 완료는 새 데이터를 공개하지 않는다. 이전 snapshot의 router·행·binding은 유지된다.
6. 기존 수동 Register/GetTable과 표준 경로는 함께 사용할 수 있다. 수동 테이블이 uint 키를 쓰더라도 명시적인 표준 등록 없이 idx registry에 자동 편입하지 않는다.

## 검증 환경과 다음 시작

준비 당시 MyLab 기존 Editor는 Unity 6000.3.18f1/Connector 0.4.1, PID 23176, CLI ready 상태였다. 이 상태는 테스트 실행 증거가 아니며 구현 시작 시 절대 project 경로와 기존 Editor의 compile/Play/준비 상태를 다시 확인한다. 임의로 다른 프로젝트나 새 Editor로 대체하지 않는다.

이번 준비는 문서만 변경하므로 Unity 테스트 실행 0건, 컴파일·Console·Player 검증은 미실행이다. 현재 문서 검사·보존 증거는 [준비 회고](retrospectives/2026-10-06-02-generic-data-table-ready.md)에 기록한다.

다음 구현 요청을 받으면 관련 계약·현재 diff를 확인하고 단계 1의 실제 Red부터 시작할 수 있다. 준비 완료는 단계 1 자동 실행을 뜻하지 않는다. 구현 완료 시 소비 프로젝트 가져오기·최소 예제·Player/IL2CPP 검증이 남으면 그 경계를 별도 기록하고 공용 코어 전체 완료로 확대하지 않는다. 병합은 해당 변경에 필요한 자동 검사와 사용자 직접 확인 여부를 [프로젝트 병합 정책](../AGENTS.md#검증-후-병합)으로 판단한다.
