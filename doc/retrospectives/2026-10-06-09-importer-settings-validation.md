# 2026-10-06 · Importer 설정 자산·공용 validator 보완

## 작업과 기준

- 요청: 생성/수정 시 공용 validator 통과 확인과 setting.asset 기반 선택적 자동화를 importer 초안에 반영한다.
- 기준 `main/2942c9840d58a281fb69e40d7f7dc160747e9077`, 작업 `codex/data-table-importer-settings-validation`. 원격 fetch 후 main/origin/main ahead/behind 0/0에서 분기했다.
- 상태: 설계 초안 보완·미구현. [선행 회고](2026-10-06-08-data-table-importer-draft.md), [설계 본문](../DATA_TABLE_IMPORTER_DRAFT.md), [runtime 계약](../DATA_TABLE_MANAGER.md).

## 결정과 변경

- 설계 본문에 DataTableImportSettings ScriptableObject/setting.asset과 자동화 3모드를 명시했다. 설정은 선택적 실행을 제어하며 공용 validator 생략 옵션은 제공하지 않는다. 설정 부재/오류/변경·domain reload·수동 실행과 프로젝트 검증 연결 책임을 정리했다.
- manager의 실제 공통 CSV reader·표준 idx 검사·table hook·FK/AddValidator를 대조했다. 독립 공개 Validator가 있다고 가정하지 않고 실제 타입은 임시 manager의 기존 경로를 재사용하도록 했다.
- 새 타입은 컴파일 전 실제 ClassMap/hook을 실행할 수 없다. 공용 reader/PK 검사의 최소 공유 추출을 제안한 사전검증과, 컴파일 후 실제 타입/전체 후보 검증을 구분하고 잠정 쓰기·검증 성공 기록도 분리했다.
- router/등록/참조 입력이 누락되면 검증 대기이며 성공으로 표시하지 않는다. 모든 입력은 현재 후보에 고정하고 실제 runtime snapshot·게임 상태를 변경하지 않는다.
- INDEX·CORE_PLAN·회고 색인을 갱신했다. 코드·테스트·자산·package·settings 및 기존 임시 파일은 변경하지 않았다.
- 조사 중 PowerShell의 따옴표 없는 upstream 표현이 Git 인수로 잘못 전달됐다. 따옴표를 적용한 읽기 전용 재확인으로 ahead/behind 0/0을 확인했고 변경은 없었다.
- Git 수행: 작업 브랜치 생성. 검증한 문서 5개만 commit/push하고 원격 검사·범위 확인 후 main fast-forward 병합/push한다. 최종 통합 결과는 Git 이력과 최종 보고에서 확인한다.

## 검증과 한계

- 문서 상대 링크/anchor·색인 등록·diff/공백·변경 범위·기존 사용자 파일 hash를 검사하고 결과를 완료 보완에 기록한다.
- 실행 테스트 0건, 실패/skip 해당 없음. 문서 작업으로 Unity 테스트 재실행 대상이 없다. 컴파일·제품 Console·실제 생성/validator·Player·소비 프로젝트 실행은 미실행이며 이전 통과 기록을 재사용하지 않는다.
- 기존 InitScene.unity 수정과 미추적 SceneTemplateSettings.json은 보존하고 Git stage에서 제외한다.

### 완료 보완 (2026-10-06)

- Markdown 32개, 상대 링크 322개, anchor 5개, 색인 누락/검사 오류 0건을 확인했다. 작업 문서 5개의 범위·후행 공백·diff --check와 기존 사용자 파일 2개의 SHA-256 보존을 확인했다.
- 검사 코드는 표준 Python으로 직접 실행해 임시 파일을 만들지 않았다. 실행 테스트는 0건이며 위 문서 검사 결과와 구분한다.

## 다음 작업

- 공용 사전검사 접근 API·프로젝트 검증 연결 함수·설정 활성 경로·소유권 기록 저장 형태와 지원 필드 타입/null을 구현 전에 확정한다. JSON 행은 여전히 후속 계약이다.
- 구현 요청 전에는 설정 자산·validator·importer 코드를 만들지 않는다. 완료 조건과 Editor/runtime 결과 일치 검증은 설계 본문을 따른다.
