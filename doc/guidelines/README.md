# 프로그래머 가이드라인 초안

- [현행] 기준 SourceRevision은 main **35508bc1fb8ed7749a292d1f9ae983b45387afd4**이며 PR #11(IMP-03)이 병합된 상태다. 근거: 2026-10-13 git fetch/rev-parse/log와 [PR #11](https://github.com/KangSungKyu/TPLab/pull/11), [IMP-03 요약:3](../validation/imp-03/README.md#L3).
- [제안] 이 초안은 현재 관행과 그 예외를 서술하며, D-A/B/C·D-E~D-G·D-H1~D-H12는 사용자 결정이며 나머지 제안은 검토 후 선택한다. 근거: 아래 [사용자 결정](#사용자-결정), 기존 [문서 상태 구분:21](../DOCUMENTATION_GUIDE.md#L21).

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
| [현행] | [06 문서](06-documentation.md) | 4분류·회고 증거 소유·레퍼런스·날짜 이력 |
| [현행] | [07 검증 증거](07-validation-evidence.md) | 최소 기계 입력 예외·permalink·삭제 순서·미결정 위치 |
| [현행] | [08 변경 절차](08-change-procedure.md) | T0~T3·공통 gate·14묶음 적용 초안·첫 시범 |

## 사용자 결정

- [결정됨] 이번 초안의 설계·앞으로의 규칙 선택은 사용자가 맡고, Codex는 현행 관행의 작성·근거 검토를 맡는다. 근거: 사용자 2026-10-13 Phase1 요청의 작성 역할과 아래 확정 결정 기록; 단위1b는 선택한 기준을 문서에 반영하며 코드·XML 전체에 구현됐다는 뜻은 아니다.

| 상태 | 결정 ID | 확정 내용과 이번 적용 범위 |
|---|---|---|
| [결정됨] | D-A | 원시 실행 산출물은 저장소에 보관하지 않고 요약·판정 근거는 남긴다. 실제 삭제·링크 정리는 삭제 목록을 먼저 제시하는 2단계 별도 PR이다; 이번 PR은 정책 서술만이며 Git 이력 재작성은 하지 않는다. 근거: 사용자 2026-10-13 Phase 1 요청 D-A·삭제 제약, [07 장](07-validation-evidence.md). |
| [결정됨] | D-B | 기존 공개 CLR 시그니처를 optional 인자 추가로 바꾸지 않고, 새 옵션은 옵션 객체 또는 새 오버로드로 확장한다. 기존 IMP-01/03 API를 이번 PR에서 고치지는 않는다. 근거: 사용자 같은 요청 D-B, [현행 두 사례](02-public-api.md#호환성-호출-가능과-기존-바이너리-결합). |
| [결정됨] | D-C | 2026-10-15 변경: 3단계에서는 대형 클래스를 분리하지 않고 안전망 57 case(UI-A 18·Scene-A 17·Guard-A 11·UI-B 4·Scene-B 7)를 산출물로 삼는다. 분리 후보의 작은 이득과 미보호 흐름을 고려했으며 크기만으로 분리하지 않는다. 특정 부분의 반복 수정으로 읽는 비용이 커지거나 새 기능이 한 책임에 크게 붙을 때 다시 검토한다. 기존 실제 분리 계획을 이 결정으로 대체한다. [종료 결정·안전망 지도](../retrospectives/2026-10-15-02-phase3-close.md), [D-H1](01-code-conventions.md#크기와-책임-분리-판단). |
| [결정됨] | D-E | 변경 하나의 증거 요약은 회고 한 곳, PR은 회고 링크, 새 validation README는 생성하지 않음, 백로그·INDEX는 상태 한 줄과 링크만 유지. 근거: 사용자 2026-10-13 Phase1b D-E, [06](06-documentation.md#문서-4분류와-증거-소유), [07](07-validation-evidence.md#증거-소유와-최소-기계-입력). |
| [결정됨] | D-F | T0~T3와 상향 규칙을 적용하고 모든 등급에 exact-SHA CI·사용자 병합 승인·dirty 보존·병합 전 사용자 확인을 요구한다. T1은 그 PR에서 새 Red·API 문서 갱신을 하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-F, [08](08-change-procedure.md#위험-등급과-공통-항목). |
| [결정됨] | D-G | Unity 테스트 CI(IMP-07)는 범위 밖이며 별도 진행한다; 이번 문서에서 설계하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-G, [08](08-change-procedure.md#위험-등급과-공통-항목). |
| [결정됨] | D-H1 | 책임·소유권 경계로 분리 판단,1,000행 초과는 설명 신호,500행 신호 불채택. 근거: 사용자 2026-10-13 일괄 승인, [01](01-code-conventions.md#크기와-책임-분리-판단). |
| [결정됨] | D-H2 | 작은 값 snapshot은 readonly struct, null/mutable authoring이 필요하면 class; default·검증 위치는 옵션별 XML에 명시. 근거: 사용자 2026-10-13 일괄 승인, [02](02-public-api.md#옵션-객체와-default-검증-형태를-계약에-맞게-선택). |
| [결정됨] | D-H3 | 새 옵션 overload는 기존 overload를 통해 같은 실행 경로로 위임하며 새 이름의 API로 구분하는 안은 불채택. 근거: 사용자 2026-10-13 일괄 승인, [02](02-public-api.md#호환성-호출-가능과-기존-바이너리-결합). |
| [결정됨] | D-H4 | 제거 전 대체·이행 예·소비 호출·제거 버전 기록, 버전 미정 adapter 유지. 근거: 사용자 2026-10-13 일괄 승인, [02](02-public-api.md#obsolete와-xml). |
| [결정됨] | D-H5 | 현행 늦은 실패 소비 계약을 문서/XML에 명시; callback·코어 원인 보관은 필요 확인 후 별도 IMP. 근거: 사용자 2026-10-13 일괄 승인, [03](03-async-lifecycle.md). |
| [결정됨] | D-H6 | 프로젝트 hook 작성자가 취소·해제 수명을 준수하며 timeout의 강제 중단 불가를 계약에 명시. 근거: 사용자 2026-10-13 일괄 승인, [03](03-async-lifecycle.md). |
| [결정됨] | D-H7 | 종료 deadline은 현재 root 단계만 유지하고 다른 native owner로 확대하지 않음. 근거: 사용자 2026-10-13 일괄 승인, [03](03-async-lifecycle.md). |
| [결정됨] | D-H8 | 신규 API만 조건부0events, 기존 snapshot 예외 분리, 현행 helper 유지·소량 대조는4단계, 오라클 위험 테스트에만 양성 대조. 근거: 사용자 2026-10-13 일괄 승인, [04](04-allocation-gc.md), [05](05-testing.md). |
| [결정됨] | D-H9 | 문서4분류·레퍼런스 현재 계약/제한·긴 실행 이력 분리·과거 운영 날짜 이력 구분. 근거: 사용자 2026-10-13 일괄 승인, [06](06-documentation.md#문서-4분류와-증거-소유). |
| [결정됨] | D-H10 | 현행 필수 기계 입력만 예외 보존, 도구 변경 없음, 삭제된 원시 링크는 고정 commit의 읽기용 permalink. 근거: 사용자 2026-10-13 일괄 승인, [07](07-validation-evidence.md#증거-소유와-최소-기계-입력). |
| [결정됨] | D-H11 | leaf14개 먼저 검토 후 실행 단위별 매핑, 실제 삭제는 목록 제시·사용자 승인 뒤2단계 별도 PR. 근거: 사용자 2026-10-13 일괄 승인, [07](07-validation-evidence.md#삭제와-현행-도구의-충돌). |
| [결정됨] | D-H12 | 2026-10-15 변경: EnsureMainThread 5곳 통합 T1 시범은 실행하지 않음. 메시지·검사 순서·assembly 경계·보호 공백 조사로 T2로 정정한 뒤 통합하지 않기로 결정했다. 2026-10-13의 시범 채택을 대체하며 통합 완료를 뜻하지 않는다. [종료 회고](../retrospectives/2026-10-15-02-phase3-close.md), [08](08-change-procedure.md#오래-가는-인용과-첫-적용-시범). |

## 0.5단계 사실 확인

- [현행] 0.5단계에서 위 main의 git ls-tree/cat-file/count-objects/verify-pack로 validation **1,310파일·54,121,607bytes(51.614MiB)**, 원시 후보 **1,216파일·52,534,371bytes**, Markdown **51파일·309,565bytes**를 확인했다; .git 일반 파일 합은 약7.5MB, size-pack은4.95MiB였다. 이는 Git 전송량 또는 재작성 후 회수량 측정이 아니다. 근거: 2026-10-13 0.5단계 조회 보고와 [검증 자료 요약 문서 위치](../validation/imp-03/README.md).
- [현행] 같은 조회의 scroll-benchmark.json6개는 모두 다른 blob이고, 원시만 제거해도 내부 요약50개, 전체 제거면 외부 Markdown112개에서 링크가 깨지는 것으로 집계했다; 직접 경로 참조가 없는 leaf14개(4,102,323bytes)는 내용 불필요 판정이 아니다. 근거: 같은 조회 보고, 대표 [P7 참조:38](../validation/ui-system/p7/README.md#L38), [정정 후보 INDEX:135](../INDEX.md#L135).
- [현행] 생성기 projection 비교에서 Core만 코드9·문서6의 **15파일**이 다르고 Input/Editor는 일치했다; 갱신 절차는 존재한다. 근거: 같은 조회 보고, [생성기:146,154,232](../../tools/build_distribution.py#L146), [절차:123](../DISTRIBUTION_PIPELINE.md#L123).
- [현행] manifest 템플릿은 validation의 manifest와 같은 blob이지만, consumer는 독립 경로 tools/core-consumer/templates/manifest.json.in을 읽는다; 동일 내용 공유만으로 validation 경로 의존이나 대체 필요성이 성립하지 않는다. 근거: 같은 조회의 공유 blob75b2248a45451dbd0f77840c80d3ccdfe00227a4와 [직접 입력 경로:222,230](../../tools/run_core_consumer.py#L222).
- [제안] 2단계의 “템플릿 대체” 선행 조건은 실제 입력 경로·blob 보존 확인으로 정정하는 안이다; 대안은 사용자 판단으로 별도 대체 입력을 설계하는 안이며 이번 단계에서는 템플릿을 바꾸지 않는다. 근거: 위 사용 경로와 [D-A](#사용자-결정).
- [현행] INDEX에는 IMP-14/05구현/03 완료 행135/137/139가 있으며, 27·30·31·32·47행은 오래된 범위·GC 수치 안내·조사 중단·UI 통합 표기를 남긴다. 근거: [현재 INDEX:27](../INDEX.md#L27), [후속 완료 행:135](../INDEX.md#L135).
- [현행] 최상위 DRAFT/TRACK/ACCEPTANCE는16개·249,660bytes이며 상태 표기는 존재한다; Input track47, Scene acceptance33 이후, UI acceptance3에 과거 운영 문장이 남는다. 근거: git ls-tree 집계, [Input:47](../INPUT_SYSTEM_TRACK.md#L47), [Scene:33](../SCENE_TRANSITION_ACCEPTANCE.md#L33), [UI:3](../UI_ACCEPTANCE.md#L3).

## 제안 목록

- [제안] 05의 Red 실패 이유 검토·동결 확장과 IMP-04b 이후 Timeout 적용 방식, 06의 문서 머리말 형식, 07의 과거 재현 입력 확보와 CI 판정 문서 위치, 08의14묶음 적용표와 장기 인용 방식은 사용자 선택 전이다. 기존 템플릿 선행 조건 정정과 [회고의 AGENTS 이관안](../retrospectives/2026-10-13-06-programmer-guidelines-draft.md#agents-이관-제안-적용하지-않음)도 제안으로 유지한다. 근거: [05](05-testing.md), [06](06-documentation.md), [07](07-validation-evidence.md), [08](08-change-procedure.md).
- [현행] AGENTS:119~121의 자동 병합 문구와 증거/색인의 과거 규칙은 새 D-E/D-F와 대조할 후속 정리 대상이다. 이번 요청에서는 사용자의 결정·제외 범위를 우선하고 AGENTS·INDEX는 수정하지 않는다. 근거: [AGENTS:119](../../AGENTS.md#L119), [07의 충돌 목록](07-validation-evidence.md#삭제와-현행-도구의-충돌).
- [현행] 이 PR은 AGENTS·기존 GC/문서 지침·모든 기존 INDEX·runtime/tests/UPM을 수정하지 않고 검증 자료도 삭제하지 않는다; 회고는 [이번 회고](../retrospectives/2026-10-13-06-programmer-guidelines-draft.md)에 기존 문서에1b 후속 기록을 추가한다. 근거: 이번 Phase1b 제외 범위, [회고 지침:51](../../AGENTS.md#L51).


## 라벨 개수

[현행] 아래는 항목·표 행의 첫 상태 라벨 기준이다. 미채택 판단 기록도 해당 라벨에 포함한다. 08의 적용 칸56개는 모두 [제안]이고 행14개와 별도로 세며, 중복 칸 표식을 장별 항목 수에 다시 더하지 않는다. 근거: 각 장의 실제 문장·표와 이번 정적 집계.

<!-- LABEL_COUNTS_START -->
| 상태 | 문서 | 현행 | 결정됨 | 제안 |
|---|---|---:|---:|---:|
| [현행] | [01-code-conventions.md](01-code-conventions.md) | 14 | 3 | 0 |
| [현행] | [02-public-api.md](02-public-api.md) | 12 | 7 | 0 |
| [현행] | [03-async-lifecycle.md](03-async-lifecycle.md) | 14 | 7 | 0 |
| [현행] | [04-allocation-gc.md](04-allocation-gc.md) | 14 | 4 | 0 |
| [현행] | [05-testing.md](05-testing.md) | 15 | 2 | 2 |
| [현행] | [06-documentation.md](06-documentation.md) | 12 | 11 | 2 |
| [현행] | [07-validation-evidence.md](07-validation-evidence.md) | 12 | 8 | 2 |
| [현행] | [08-change-procedure.md](08-change-procedure.md) | 8 | 12 | 16 |
| [현행] | [README.md](README.md) | 30 | 19 | 3 |
<!-- LABEL_COUNTS_END -->
