# 2026-10-06 · 제네릭 데이터 조회 구현 준비

## 작업과 기준

- 목표: 최종 uint PK를 입력하는 Get<T>/TryGet<T> 기준으로 다음 구현의 계약·단계·완료 조건을 준비한다. 이번 요청은 준비이며 runtime 구현 실행이 아니다.
- 기준: `main / 9d79dacb3eb3347dee29d7a6241700de1f7d3e8b`, 작업 branch `docs/generic-data-table-ready`.
- 상태: 준비 완료·미구현. 제네릭 형식 우선은 사용자 지시, 기본 codec·0/null·오류 세부 정책은 이번 준비의 설계 선택이다.
- 관련: [구현 준비](../DATA_TABLE_GENERIC_IMPLEMENTATION.md), [idx 계약](../DATA_TABLE_IDX_DRAFT.md), [DTO 매핑](../DATA_TABLE_MAPPING_DRAFT.md), [선행 Parts/Stride 회고](2026-10-06-01-idx-parts-stride.md).

## 결정과 변경

- 변경: GetRow 명칭을 Get/TryGet으로 정리하고 manager/runtime 등록을 IIdxRouter로 줄였다. 기존 초안·예제·색인을 맞추고 단계별 수정 경계·TDD·후속 확인 항목을 작성했다.
- 이유: CSV의 idx는 작성 시 최종 생성된 PK다. 조회는 종류 추출·등록 DTO 검증·완전한 PK 조회로 충분하고 생성/전체 추출은 별도 codec 테스트로 검증한다. dynamic은 공용 runtime에 추가하지 않는다.
- 배운 점: 기대 T와 실제 테이블 선택을 구분하면 호출자가 타입을 지정해도 idx 자동 탐색을 유지할 수 있다. generator 등록·왕복 재생성·새 전역 색인까지 조회에 강제하지 않는다.
- Git: 원격 main 확인 후 문서 branch를 생성했다. 검사 후 이번 문서 6개만 커밋·푸시·main 통합하고 최종 결과는 Git 이력과 작업 보고에서 확인한다.

## 검증과 한계

- 문서 검사: Markdown 21개, 상대 링크 212개, anchor 5개, 검사 오류 0건. 문서 6개 변경 범위·diff 공백도 확인했다. 산술 예제 6개와 overflow 경계 1개를 확인했고 기존 런타임 검증 입력 44개의 canonical SHA-256이 일치한다. 로컬 검사·증거는 `Temp/GenericDataTableReady/Validate.py`와 `validation.json`이다. 산술 확인은 새 API의 실행 증거가 아니다.
- 기존 Editor의 CLI ready만 확인했다(PID 23176). Unity 테스트 실행 0건, 새 API 컴파일·제품 Console·Player 실행은 미실행이다. 과거 테스트 결과는 현재 구현 준비/Green 증거로 사용하지 않는다.
- 기존 사용자 수정 InitScene·미추적 SceneTemplateSettings의 SHA-256이 변경 전과 일치하며 이번 Git 반영에서 제외한다. 소스·패키지·assembly·씬·설정은 수정하지 않았다.

## 다음 작업

- 후속 구현 요청 시 구현 준비 문서 단계 1의 codec/DTO 계약 Red부터 진행한다. 기본 정책을 바꾸는 사용자 지시가 생기면 관련 단위의 계약·테스트를 함께 갱신한다.
- 새 API의 컴파일·정상/경계/실패·전체 회귀·소비 프로젝트/Player 검증은 후속 실행 사항이다. 이번 단위는 코드를 생성하거나 다른 세션에 구현을 시작시키지 않는다.
