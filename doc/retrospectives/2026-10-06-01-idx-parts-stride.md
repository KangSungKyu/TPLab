# 2026-10-06 · Parts와 Stride 계약 보완

## 작업과 기준

- 목표: 조합 값 Parts와 구간 설정 Stride를 함께 제공하고 localType을 선택적 프로젝트 구분으로 정의한다. 요청 범위는 앞선 idx 초안의 보완이다.
- 기준: `main / 356396fa2403d33fb93d4070ca0008cb60be548b`, 작업 branch `docs/idx-parts-stride`.
- 상태: 초안. Parts/Stride 지원과 localType의 목적·선택성은 사용자 요구이며 API·구간·0/null 세부 정책은 미구현 제안이다.
- 관련 명세: [idx 초안](../DATA_TABLE_IDX_DRAFT.md), [DTO 매핑 초안](../DATA_TABLE_MAPPING_DRAFT.md). 선행 기록은 [uint PK/FK 초안](2026-10-02-02-data-table-idx-draft.md)이다.

## 결정과 변경

- 변경: idx 계약의 고정된 두 uint 입력을 IIdxCodec<TParts>로 대체하고 manager 탐색은 IIdxRouter로 한정했다. 기본 IdxParts와 불변 Stride, 프로젝트 세 요소·복수 구간 예시를 정의하고 문서·회고 색인을 갱신했다.
- 이유: 조합 요소 수는 프로젝트마다 다르지만 테이블 탐색은 dataType만 필요하다. localType은 동일 테이블의 PK 구분이며 선택한 codec 안에서 범위·왕복·충돌 부재를 검증한다. 작업자 구분 배정은 프로젝트가 소유한다.
- 문제와 배운 점: Parts가 숫자 구간 크기로 오해될 수 있었다. 실제 값 묶음과 설정을 분리하고 Stride=10000의 표시·숫자 저장 차이를 예시로 명시했다. localType은 테이블 라우팅·Git 텍스트 충돌 해결 기능으로 확장하지 않는다.
- Git: 원격 main을 확인하고 문서 작업 branch를 생성했다. 검사 후 이번 문서 5개만 커밋·푸시·main 통합하며 최종 결과는 Git 이력과 작업 보고에서 확인한다.

## 검증과 한계

- 문서 검사: Markdown 19개, 상대 링크 189개, anchor 4개, 검사 오류 0건. 문서 5개 변경 범위·diff 공백도 확인했다. 산술 예제 6개 및 overflow 경계 1개를 검사했고 기존 런타임 검증 입력 44개의 canonical SHA-256이 일치한다. 로컬 검사·증거는 `Temp/IdxPartsStride/Validate.py`와 `validation.json`이다. 산술 확인은 미구현 codec의 컴파일·실행 증거가 아니다.
- Unity 테스트 실행 0건. 문서만 변경하여 runtime·자산·패키지·설정 변경의 테스트 대상이 없다. 컴파일·제품 Console·Player·새 codec 런타임 실행은 미실행이다.
- 기존 사용자 수정 InitScene과 미추적 SceneTemplateSettings의 SHA-256이 변경 전과 일치하며 이번 stage에서 제외한다. 과거 회고의 로컬 Temp 증거는 현재 없으므로 현재 검사 증거로 사용하지 않는다.

## 다음 작업

- 미확정: 기본 codec 제공·개별 범위·0/null·API 오류 정책. 프로젝트 LocalType 배정과 사용한 조합 규약의 migration은 소비 프로젝트가 결정한다.
- 구현 요청 시 기본 Parts/Stride와 전체 추출·종류 라우팅의 일치부터 TDD로 확인하고 DTO 매핑·snapshot/FK 계약을 연결한다. 이번 문서 작업에서 runtime 구현이나 다음 시스템을 시작하지 않는다.
