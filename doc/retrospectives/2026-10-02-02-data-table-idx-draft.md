# 2026-10-02 · uint idx와 PK/FK 계약 초안

## 작업과 기준

- 목표: 기본 테이블의 uint 키, idx 생성·추출 클래스 등록, 종류별 PK/FK 탐색 계약 초안 작성. 구현 요청은 아니다.
- 기준: `main / a7804a83e1ade7ff7ec1058f19b65de7f0784b07`, 작업 branch `docs/data-table-idx-draft`. Cashier 참조는 `total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0`이다.
- 상태: 초안. uint 표준 키·생성/추출 등록·PK/FK 탐색 요구는 확정이며 API·인코딩·0/null 세부 정책은 제안이다.
- 관련 명세: [uint idx 초안](../DATA_TABLE_IDX_DRAFT.md), [DTO 매핑 초안](../DATA_TABLE_MAPPING_DRAFT.md), [현행 구현](../DATA_TABLE_MANAGER.md). 선행 기록은 [회고 지침 추가](2026-10-02-01-unit-retrospectives.md)다.

## 결정과 변경

- 변경: idx 초안을 추가하고 기존 매핑 초안의 표준 IDataRow/IDataTable/등록 API를 uint로 통일했다. 문서·회고 색인에 입구를 연결했다.
- 결정: 같은 불변 codec 객체가 생성·추출 계약을 제공하고 종류 코드 하나를 테이블 하나에 연결한다. 종류 추출과 실제 행 존재는 별도 단계다. FK는 예상 종류와 실제 대상 PK를 같은 후보 snapshot에서 검증한다. 기존 수동 제네릭 등록은 유지한다.
- 이유와 배운 점: 이름만 다른 동일 종류 테이블은 idx로 구별할 수 없다. DTO 타입만 확인하면 같은 DTO의 다른 종류를 잘못 참조할 수 있다. 명시적인 종류 등록·FK 목적지 선언을 사용하며 후보를 이전 snapshot으로 검증하지 않는다.
- Git: main에서 문서 작업 branch를 생성했다. 문서 검증 후 승인 정책에 따라 이번 문서 5개만 커밋·푸시·main 통합하며 최종 결과는 Git 이력과 작업 보고에서 확인한다.

## 검증과 한계

- 문서 검사: Markdown 18개, 상대 링크 184개, anchor 4개, 색인 누락·검사 오류 0건. diff 공백·문서 5개 변경 범위도 확인했다. 로컬 검사·증거는 `Temp/DataTableIdxDraft/Validate.ps1`, `validation.json`이다. 기존 검증 입력 44개의 canonical SHA-256이 일치한다(`runtime-input-check.json`). 이는 런타임 입력 보존 확인이며 테스트 재실행 증거가 아니다.
- Unity 테스트 실행 0건. 문서만 변경하여 실행 테스트 대상이 없다. 이번 단위에서 컴파일·제품 Console·Player 검증은 수행하지 않으며 기존 실행 결과를 이번 테스트 통과로 사용하지 않는다.
- 사용자 수정 InitScene과 미추적 SceneTemplateSettings는 이번 변경·stage에서 제외했으며 SHA-256이 변경 전과 일치한다. Cashier는 읽기 전용이며 참조 색인의 SHA-256도 일치한다.

## 다음 작업

- 미확정: DecimalIdxCodec 제공 여부·stride 선택·0 예약·선택 FK null·API 오류 정책. 문서 반영은 API 구현 완료를 의미하지 않는다.
- 구현을 요청받으면 관련 초안과 현재 코드를 대조하고 codec 경계/역함수 TDD부터 시작한다. 다음으로 uint DTO 매핑·종류 registry·조회·FK 후보 검증을 연결한다. 소비 프로젝트/Player 실행은 별도 검증이 필요하다.
- 다음 시스템의 구현이나 GameSceneManager 단계 진행은 이번 초안 요청으로 시작하지 않는다.
