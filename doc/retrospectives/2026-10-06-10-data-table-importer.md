# 2026-10-06 · CSV Editor importer 구현

## 작업과 기준

- 요청: 설정 자산과 생성/수정 단계의 공용 필수 validator를 포함한 초안에서 구현 단계로 진행.
- 기준: main/19b577038ca77d02d252d676cacf0f63e23c74c2 → codex/data-table-importer. 기존 InitScene 변경과 미추적 SceneTemplateSettings를 bytes 그대로 보존했다.
- 상태: CSV 1차 구현·MyLab 내부 검증 완료. [현재 계약](../DATA_TABLE_IMPORTER_DRAFT.md), [선행 회고](2026-10-06-09-importer-settings-validation.md), [실행 증거](../validation/data-table-importer/README.md).

## 결정과 변경

- DataTableCsvValidator에 기존 manager CSV reader/표준 PK 검사를 추출해 runtime과 Editor가 재사용한다. 수동 arbitrary PK·기존 snapshot/수명 의미를 보존했다.
- Editor 설정 Disabled/ValidateOnly/GenerateValidated, strict JSON 정의·결정적 partial 생성, 소유 manifest/hash/GUID·stage/복구, 명시적 project profile, 임시 manager의 실제 ClassMap/hook/FK 검증을 연결했다. 새 의존성·기본 구체 테이블·runtime 자동 등록은 추가하지 않았다.
- 기존 스키마의 모든 계약 변경은 자동으로 쓰지 않고 수동 검토/적용으로 제한했다. 타입 rename은 별도 migration 전까지 수동에서도 거부한다. 추가 preview/restore framework를 만들지 않았다.
- 처음 typed 검사에서는 기존 DTO namespace에 생성 전용 core 보호 규칙을 적용하는 오류가 있었다. generated/existing 경계에서 나눠 해결했다.
- native CSV 이동/삭제/재생성 중 삭제 GUID의 캐시 경로를 따라가 실패했다. 실제 파일 존재·현재 GUID 일치를 확인하고 검증 성공 후 새 GUID를 기록했다. 파일 위치 캐시를 현재 존재 증거로 사용하지 않는 점을 배웠다.
- profile 변경과 worker 호출을 Red로 확인하고 revision/fingerprint 무효화·Unity API 전 main-thread 거부를 추가했다. 임시 manager 해제는 취소 재개 스레드와 무관하게 main thread로 복귀한다.
- Git 수행: 기준 main에서 작업 브랜치 생성. 최종 diff·원격·검증을 대조하고 승인된 자동 병합 정책으로 커밋·푸시·main 통합한다. 해당 실제 최종 commit/통합 결과는 Git 이력과 최종 보고에서 확인한다. 사용자 변경은 커밋하지 않는다.

## 검증과 한계

- 실제 Red/Green: 20건(19실패→0), 27건(7→0), 32건(5→0), 37건(4→0), 39건(2→0). 초기 Red stderr는 파일로 보존하지 못해 관찰 결과를 요약 JSON으로 남겼다.
- 최종 집중 40/40, full EditMode 154/154, PlayMode 87/87; 실패0/skip0. native 확인11개·typed6회·컴파일 오류 차단/복구·자기 fixture 제거 성공. same Editor PID23176, Unity6000.3.18f1/Connector0.4.1.
- 컴파일·예상 실패 Console 보관·clear 후 오류/경고0 확인. 최종 source/config hash·문서/범위·GUID 검사 자료는 위 증거에 있다. 신규 코드만 Roslyn 공백 정리했으며 task-owned formatter Temp는 제거했다.
- 수동 시각 UX·소비 프로젝트·Player/IL2CPP 미실행. 새 사용자의 직접 확인이 필요한 gate는 이번 필수 범위에 없다. 다중 테이블 전체 파일 반영의 원자성은 보장하지 않으며 잠정 소스와 성공 데이터 검증 상태를 구분한다.

## 다음 작업

- 다음 공용 시스템은 GameSceneManager: 기존 AsyncSceneLifecycle의 준비·해제·가림막 계약에 실제 씬 전환의 취소/활성화 경계를 정의한 뒤 독립 단위로 구현한다.
- importer 후속은 JSON 행 형식/로더, rename migration, 소비 프로젝트 적용·Player 검증이다. 현재 테스트 통과를 이 영역 완료로 확대하지 않는다.
