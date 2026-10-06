# Text·Resource 예시 템플릿 분리 검증

2026-10-06. 기준 main/8e44803, 작업 codex/data-table-templates. 사용자 요청으로 TextRow/ResourceKeyRow/TextDataTable/ResourceKeyDataTable을 MyLab.Core에서 제거했다. 공용 IDataRow/DataRow/IDataTable/CsvDataTable 및 manager/snapshot 계약은 유지했다. [템플릿과 복사 방법](../../templates/data-tables/README.md), [매핑 계약](../../DATA_TABLE_MAPPING_DRAFT.md)을 따른다.

## 실제 실행

| 검사 | 실행 / 통과 / 실패 / skip | 증거 |
|---|---|---|
| Core 타입 제외 Red와 기존 표준 계약 | 22 / 18 / 4 / 0 | [JSON](red.json) · [터미널](red.txt) |
| 분리 후 표준 계약 Green | 22 / 22 / 0 / 0 | [JSON](green.json) · [터미널](green.txt) |
| 최종 전체 EditMode | 114 / 114 / 0 / 0 | [JSON](full-EditMode.json) |
| 최종 전체 PlayMode | 87 / 87 / 0 / 0 | [JSON](full-PlayMode.json) |

추가한 4건은 core assembly에 네 가지 구체 스키마/테이블 타입이 존재하지 않아야 한다는 공개 경계 검사다. 변경 전 실제 실패를 실행하고 분리 후 통과했다. 전체 EditMode에는 외부 Addressables DocExample RequiredTest 1건이 포함된다. Player/소비 프로젝트 결과로 확대하지 않는다.

첫 refresh에서 컴파일/domain reload 도중 CLI instance discovery 오류가 있었고 뒤이은 실행은 신규 검사 없이 기존 18건만 발견했다. 이 결과는 Red로 사용하지 않았다. 정확한 MyLab 상태·refresh 완료를 다시 확인한 뒤 22건/4실패를 확보했다. 컴파일 오류·0건·이전 assembly 검사를 Red/Green으로 세지 않는다.

## 검증 대상과 증거

- 예시 소스는 `Assets/MyLab/Tests/Fixtures/DataTableTemplates.cs`, namespace는 MyLab.Examples.DataTables다. 기존 TestFixtures assembly에서 컴파일하고 표준 CSV 매핑/조회·FK·binding·factory·비동기·native CSV/root 테스트가 실제 예시를 사용한다.
- manager/snapshot/codec/CsvDataTable 구현과 assembly 설정·패키지는 바꾸지 않았다. Core의 DataRow.cs에서 구체 4타입을 제거하고 기존 테스트 5파일에는 예시 namespace 참조를 추가했다. 기존 DataRow.cs meta/GUID는 보존하고 새 템플릿 meta만 생성했다.
- Text 빈 문자열·Resource 공백 key 정책은 예시 검증으로 유지했다. runtime manager의 필수 스키마/정책이 아니다.
- exact MyLab Editor PID 23176, Unity 6000.3.18f1/Connector 0.4.1. [컴파일](compile-after.txt), [Editor 상태](status-after.txt), [예상 실패 경로 로그](console-before-clear.json), Console을 비운 뒤 정상 [오류·경고 0](console-after.json)을 기록했다.
- [입력 hash](test-inputs.json)는 최종 C#·asmdef·manifest/lock·Unity 버전을 UTF-8 BOM 제외·CRLF→LF로 정규화한 SHA-256이다. 최종 실행 뒤 코드 입력은 변경하지 않았다. 결과 JSON은 같은 실행의 터미널 출력에서 추출했다.
- [정적 검사](static-checks.json)는 문서·상대 링크·색인, asset/meta·GUID, 범위·공백·기존 사용자 파일 hash와 변경하지 않은 baseline 입력을 확인한다. 기존 InitScene·SceneTemplateSettings 변경은 보존했다.

## 재현과 완료 경계

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab editor refresh --compile
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode
unity-cli --project C:\Users\PC\Projects\MyLab test --mode PlayMode
```

같은 Editor에서 순차 수행한다. 집중 검사는 `--filter MyLab.Core.Tests.StandardDataTableTests`다. 새로운 수동 UI/scene 확인 대상은 없다. 승인된 자동 검증 후 통합 정책을 적용하며 GitHub workflow/check가 없으면 CI 통과라고 기록하지 않는다. Git 수행은 [회고](../../retrospectives/2026-10-06-07-data-table-templates.md)와 최종 보고를 따른다.

소비 프로젝트로 예시 복사·필드 변경·Player/IL2CPP·최종 시각 UX 검증은 미실행이다. 테스트 assembly에 있는 예시는 일반 Player 제품 의존성으로 가져오지 않고 필요한 소스를 프로젝트 runtime namespace에 복사한다. 이미 제거 전 Core 구체 타입을 쓰던 소비 프로젝트는 예시/자체 타입으로 namespace와 등록을 변경해야 한다.
