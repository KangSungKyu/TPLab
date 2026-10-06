# 2026-10-06 · 임시 파일 지침·Editor importer 검토

## 작업과 기준

- 요청: 임시 파일 사용 후 정리/반복 도구 관리 기준을 프로젝트 지침에 명시하고, 지정 폴더 CSV/JSON 감지와 테이블 타입 생성/수정 시스템을 검토한다.
- 기준 `main/f51e2d330a981b3bf7fcf3d3b5c2e4300a27a54f`, 작업 `codex/data-table-importer-draft`. fetch 후 main/origin/main ahead/behind 0/0에서 분기했다.
- 상태: 지침 완료·설계 초안·미구현. [선행 회고](2026-10-06-07-data-table-templates.md), [매핑 계약](../DATA_TABLE_MAPPING_DRAFT.md).

## 결정과 변경

- [AGENTS.md](../../AGENTS.md)에 작업 소유 임시 파일의 증거 보존 후 정리, 반복 도구의 필요 기반 tools 승격, 기존/사용자 파일 보호, 생성 소스와 임시 출력 구분을 추가했다. 기존 Temp 파일 삭제·도구 승격은 수행하지 않았다.
- [importer 초안](../DATA_TABLE_IMPORTER_DRAFT.md)에 명시적 스키마·CSV 우선, 생성/기존 타입 검증 모드, partial/.g.cs 소유권, 폴더 감지·컴파일/reimport·이동/삭제·hash 충돌, 프로젝트 idx/router/FK/등록 경계와 JSON 행 후속 결정을 정리했다.
- INDEX·CORE_PLAN·매핑 계약에 초안 입구를 연결했다. runtime·테스트·자산·package·settings는 변경하지 않았다.
- 현재 core에는 Text/Resource 기본 타입과 JSON 행 로더가 없다. 구체 스키마를 공용 기본 테이블로 재도입하지 않고, 생성 코드도 프로젝트 소유로 두는 것이 이전 결정과 일치한다.
- CSV/JSON은 내장 TextAsset이며 AssetPostprocessor 감지가 적합하다는 판단은 초안의 Unity 6.3 공식 문서 링크를 근거로 했다. 코드 생성은 refresh/컴파일 재진입을 유발할 수 있어 지연 처리뿐 아니라 출력 제외·내용 비교·소유권 검사가 필요하다.
- Git: 작업 범위의 문서 7개만 commit/push하고 원격 검사 확인 후 main fast-forward 병합·push한다. 최종 결과는 Git 이력과 최종 보고에서 확인한다.

## 검증과 한계

- 문서 검사 결과는 아래 완료 보완에 기록한다. 링크/anchor·색인 등록·공백·문서 한정 diff·사용자 파일 hash 보존을 검사한다.
- 실행 테스트 0건, 실패/skip 해당 없음. 문서만 변경하므로 Unity 테스트 재실행 대상이 없으며 이전 EditMode/PlayMode 통과를 이번 작업의 실행 결과로 세지 않는다. 컴파일·제품 Console·Editor 감지/생성·Player·소비 프로젝트 실행은 미실행이다.
- 기존 `Assets/Scenes/InitScene.unity` 수정과 미추적 `ProjectSettings/SceneTemplateSettings.json`은 그대로 보존한다. Git stage/commit에 포함하지 않는다.

### 완료 보완 (2026-10-06)

- 작업 문서 7개를 대상으로 변경 범위·후행 공백·git diff --check를 확인했다. Markdown 31개, 상대 링크 318개, anchor 5개, 색인 누락/검사 오류 0건이다. 검사 코드는 표준 Python으로 직접 실행했고 새 임시 파일을 만들지 않았다.
- 사용자 파일 2개의 원본 SHA-256이 작업 전과 일치했다. 테스트는 실행 0건이며 문서 검사 통과와 구분한다.

## 다음 작업

- 구현 전 지원 필드 타입·null/default, 설정/소유권 기록 형식, JSON 행 처리 경로를 확정한다. CSV 수동 생성 → 감지/반영 → 프로젝트 연결 → 필요 시 JSON 행 순서의 완료 조건은 초안이 소유한다.
- 초안 병합은 importer 구현/패키지 추가/자동 생성 활성화 승인이 아니다. 기존 임시 스크립트의 승격/정리도 이번 범위 밖이다.
