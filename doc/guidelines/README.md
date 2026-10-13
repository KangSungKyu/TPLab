# 프로그래머 가이드라인 초안

- [현행] 기준 SourceRevision은 main **35508bc1fb8ed7749a292d1f9ae983b45387afd4**이며 PR #11(IMP-03)이 병합된 상태다. 근거: 2026-10-13 git fetch/rev-parse/log와 [PR #11](https://github.com/KangSungKyu/TPLab/pull/11), [IMP-03 요약:3](../validation/imp-03/README.md#L3).
- [제안] 이 초안은 현재 관행과 그 예외를 서술하며, D-A/B/C 이외의 앞으로의 규칙은 사용자 검토 후 선택한다. 근거: 아래 [사용자 결정](#사용자-결정), 기존 [문서 상태 구분:21](../DOCUMENTATION_GUIDE.md#L21).

## 근거와 검증 범위

- [현행] 각 항목의 상태는 코드·현재 문서의 사실인 **[현행]**, 2026-10-13 사용자가 확정한 **[결정됨]**, 대안이 남은 **[제안]**으로 구분한다. 근거: 이번 작성 요청의 상태 라벨 조건과 [기존 상태 구분:21](../DOCUMENTATION_GUIDE.md#L21).
- [현행] 이번 증거 수준은 **코드 읽기·Git 조회/집계**이며 Unity·테스트·Player 실행은 **0회**다; 장에서 인용한 실행 수치는 해당 source의 기존 요약 기록이다. 근거: [문서 전용 검증:74](../DOCUMENTATION_GUIDE.md#L74), [IMP-03 실행 source/한계:3,74](../validation/imp-03/README.md#L3).
- [현행] 코드의 파일:줄은 위 SourceRevision을 기준으로 쓰고, 외부에 제공된 UPM 0.0.1 사본과 개발본의 일치를 가정하지 않는다. 근거: [배포 입력·사본 검증:232](../../tools/build_distribution.py#L232), 아래 [0.5단계 사실 확인](#05단계-사실-확인).

| 상태 | 장 | 읽는 목적 |
|---|---|---|
| [현행] | [01 코드 규약](01-code-conventions.md) | 이름·assembly·타입 배치·로그·thread·크기 현황 |
| [현행] | [02 공개 API](02-public-api.md) | source/CLR 호환성·옵션 선례·snapshot/buffer |
| [현행] | [03 비동기·수명](03-async-lifecycle.md) | 취소·timeout·Forget·늦은 실패·소유권 |
| [현행] | [04 할당·GC](04-allocation-gc.md) | 현행 helper와 GC 초안의 충돌 |
| [현행] | [05 테스트](05-testing.md) | 현행 분모·대기·오라클·Red와 교훈 |
| [현행] | [06 문서](06-documentation.md) | 기존 문서 지침과 분류·이력 제안 |
| [현행] | [07 검증 증거](07-validation-evidence.md) | D-A와 현행 도구의 입력·보고 경계 |

## 사용자 결정

- [결정됨] 이번 초안의 설계·앞으로의 규칙 선택은 사용자가 맡고, Codex는 현행 관행의 작성·근거 검토를 맡는다. 근거: 사용자 2026-10-13 Phase1 요청의 작성 역할과 아래 D-A/B/C 결정 기록.

| 상태 | 결정 ID | 확정 내용과 이번 적용 범위 |
|---|---|---|
| [결정됨] | D-A | 원시 실행 산출물은 저장소에 보관하지 않고 요약·판정 근거는 남긴다. 실제 삭제·링크 정리는 삭제 목록을 먼저 제시하는 2단계 별도 PR이다; 이번 PR은 정책 서술만이며 Git 이력 재작성은 하지 않는다. 근거: 사용자 2026-10-13 Phase 1 요청 D-A·삭제 제약, [07 장](07-validation-evidence.md). |
| [결정됨] | D-B | 기존 공개 CLR 시그니처를 optional 인자 추가로 바꾸지 않고, 새 옵션은 옵션 객체 또는 새 오버로드로 확장한다. 기존 IMP-01/03 API를 이번 PR에서 고치지는 않는다. 근거: 사용자 같은 요청 D-B, [현행 두 사례](02-public-api.md#호환성-호출-가능과-기존-바이너리-결합). |
| [결정됨] | D-C | 필요하면 GameSceneManager 같은 큰 클래스를 3단계에서 실제 분리한다. 크기 수치·분리 경계는 아직 제안이다. 근거: 사용자 같은 요청 D-C, [현재 분포와 제안](01-code-conventions.md#크기와-책임-분리-판단). |

## 0.5단계 사실 확인

- [현행] 0.5단계에서 위 main의 git ls-tree/cat-file/count-objects/verify-pack로 validation **1,310파일·54,121,607bytes(51.614MiB)**, 원시 후보 **1,216파일·52,534,371bytes**, Markdown **51파일·309,565bytes**를 확인했다; .git 일반 파일 합은 약7.5MB, size-pack은4.95MiB였다. 이는 Git 전송량 또는 재작성 후 회수량 측정이 아니다. 근거: 2026-10-13 0.5단계 조회 보고와 [검증 자료 요약 문서 위치](../validation/imp-03/README.md).
- [현행] 같은 조회의 scroll-benchmark.json6개는 모두 다른 blob이고, 원시만 제거해도 내부 요약50개, 전체 제거면 외부 Markdown112개에서 링크가 깨지는 것으로 집계했다; 직접 경로 참조가 없는 leaf14개(4,102,323bytes)는 내용 불필요 판정이 아니다. 근거: 같은 조회 보고, 대표 [P7 참조:38](../validation/ui-system/p7/README.md#L38), [정정 후보 INDEX:135](../INDEX.md#L135).
- [현행] 생성기 projection 비교에서 Core만 코드9·문서6의 **15파일**이 다르고 Input/Editor는 일치했다; 갱신 절차는 존재한다. 근거: 같은 조회 보고, [생성기:146,154,232](../../tools/build_distribution.py#L146), [절차:123](../DISTRIBUTION_PIPELINE.md#L123).
- [현행] manifest 템플릿은 validation의 manifest와 같은 blob이지만, consumer는 독립 경로 tools/core-consumer/templates/manifest.json.in을 읽는다; 동일 내용 공유만으로 validation 경로 의존이나 대체 필요성이 성립하지 않는다. 근거: 같은 조회의 공유 blob75b2248a45451dbd0f77840c80d3ccdfe00227a4와 [직접 입력 경로:222,230](../../tools/run_core_consumer.py#L222).
- [제안] 2단계의 “템플릿 대체” 선행 조건은 실제 입력 경로·blob 보존 확인으로 정정하는 안이다; 대안은 사용자 판단으로 별도 대체 입력을 설계하는 안이며 이번 단계에서는 템플릿을 바꾸지 않는다. 근거: 위 사용 경로와 [D-A](#사용자-결정).
- [현행] INDEX에는 IMP-14/05구현/03 완료 행135/137/139가 있으며, 27·30·31·32·47행은 오래된 범위·GC 수치 안내·조사 중단·UI 통합 표기를 남긴다. 근거: [현재 INDEX:27](../INDEX.md#L27), [후속 완료 행:135](../INDEX.md#L135).
- [현행] 최상위 DRAFT/TRACK/ACCEPTANCE는16개·249,660bytes이며 상태 표기는 존재한다; Input track47, Scene acceptance33 이후, UI acceptance3에 과거 운영 문장이 남는다. 근거: git ls-tree 집계, [Input:47](../INPUT_SYSTEM_TRACK.md#L47), [Scene:33](../SCENE_TRANSITION_ACCEPTANCE.md#L33), [UI:3](../UI_ACCEPTANCE.md#L3).

## 제안 목록

- [제안] 코드 크기 신호, 옵션의 class/readonly struct·default, API 제거 유예, GC 분류 예외, 테스트 교훈의 확장, 문서4분류와 permalink·최소 증거 보존의 대안은 각 장에서 사용자 선택 대상으로 유지한다. 근거: [01](01-code-conventions.md), [02](02-public-api.md), [04](04-allocation-gc.md), [05](05-testing.md), [06](06-documentation.md), [07](07-validation-evidence.md).
- [현행] 이 PR은 AGENTS·기존 GC/문서 지침·모든 기존 INDEX·runtime/tests/UPM을 수정하지 않고 검증 자료도 삭제하지 않는다; 회고는 [이번 회고](../retrospectives/2026-10-13-06-programmer-guidelines-draft.md)에 별도 새 문서로만 남긴다. 근거: 이번 Phase1 제외 범위, [회고 지침:51](../../AGENTS.md#L51).
