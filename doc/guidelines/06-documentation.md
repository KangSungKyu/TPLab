# 06 · 문서 작성 초안

[현행] SourceRevision은 main35508bc이며 기존 문서와 코드의 읽기 결과를 서술한다; 이번 문서 예제의 compile/run은0회다. 근거: [공통 범위](README.md#근거와-검증-범위), [문서 예제 조건:46](../DOCUMENTATION_GUIDE.md#L46).

## 기존 지침과 역할

- [현행] DOCUMENTATION_GUIDE는 사람 README/API와 AI README/API의 경로·역할, public 선언/XML의 근거, source·상태·검증 한계를 소유한다; 이 초안은 이를 중복된 새 규범으로 교체하지 않는다. 근거: [기존 지침:7,18,20](../DOCUMENTATION_GUIDE.md#L7), [AGENTS:43](../../AGENTS.md#L43).
- [현행] 구현 상태 Implemented/Proposed/Deprecated와 검증 상태 Verified/Partial/NotRun, 자동 검증과 사용자 수락은 구분하도록 적혀 있다; 과거 실행과 순수 문서 변경의 실행0을 최신 성공으로 표현하지 않는다. 근거: [기존 지침:21,22,74](../DOCUMENTATION_GUIDE.md#L21).
- [현행] 공개 계약이 바뀌면 XML·관련 사람/AI API를 같은 단위에서 갱신하고, 기능명세는 장문의 계약·결정 근거를 소유한다. 근거: [기존 지침:19,73](../DOCUMENTATION_GUIDE.md#L19), [현행 API 관찰](02-public-api.md#obsolete와-xml).
- [현행] 회고는 목표·base/branch·변경·결정·검증·미실행·후속 작업을 기록하며 일반 규칙은 AGENTS가 소유한다; 이번에는 새 회고만 추가하고 모든 기존 INDEX를 그대로 둔다. 근거: [AGENTS:51,53,54](../../AGENTS.md#L51), [현재 작성 범위](README.md#제안-목록).

## 문서 유형4분류는 제안

| 상태 | 유형과 대표 현행 위치 | 제안과 대안 |
|---|---|---|
| [제안] | 가이드: [DOCUMENTATION_GUIDE](../DOCUMENTATION_GUIDE.md) | 판단·작업 기준을 안내하고 API 계약을 링크하는 안; 대안은 현재 AGENTS에 짧은 규칙을 두고 상세만 가이드로 연결한다. 근거: 기존 지침:18,69. |
| [제안] | 레퍼런스: [사람 API](../api/README.md), [AI API](../ai/api/README.md) | 현재 계약·호환성·알려진 제한을 본문에 두고 긴 실행 이력을 검증 기록으로 연결하는 안; 대안은 짧은 변경 안내를 현재 API에 유지한다. 근거: 기존 지침:19,40,44. |
| [제안] | 검증 기록: [IMP-03 요약](../validation/imp-03/README.md) | source·명령·환경·결과·판정·한계를 요약하고 원시는 D-A에 따라 커밋하지 않는 안; 대안은 [07 장](07-validation-evidence.md)의 최소 gate 입력을 명시적으로 허용한다. 근거: D-A·현행 verify_validation.py:31. |
| [제안] | 결정 기록: [백로그 결정 표](../IMPROVEMENT_BACKLOG.md#L103), [회고](../retrospectives/INDEX.md) | 확정·대안·이유를 한 소유 문서에 두고 다른 문서가 링크하는 안; 대안은 별도 결정 문서 없이 현행 백로그/회고를 유지한다. 근거: AGENTS:54·기존 지침:19. |

- [현행] DRAFT라는 파일명에도 현재 구현 계약을 담은 Input/DataTable 문서가 있고, GAME_UI_SYSTEM_DRAFT는 설계 기록으로 현재 계약을 안내한다; 이름만으로 미구현이라고 판정할 수 없다. 근거: [Input:3](../INPUT_SYSTEM_DRAFT.md#L3), [Mapping:3](../DATA_TABLE_MAPPING_DRAFT.md#L3), [UI 설계:3](../GAME_UI_SYSTEM_DRAFT.md#L3).
- [현행] INDEX의 IMP-14/05구현/03 행은 존재하지만, 구형 조사 중단/GC 수치·UI 통합 문장과 공존한다; track/acceptance에도 과거 운영 문장이 남는다. 근거: [INDEX:30,32,47,135](../INDEX.md#L30), [Input track:47](../INPUT_SYSTEM_TRACK.md#L47), [UI acceptance:3](../UI_ACCEPTANCE.md#L3).
- [제안] 문서 첫 부분에 유형·현재 상태·source·마지막 갱신·현재 계약 링크를 두고 과거 운영 문장을 날짜가 붙은 기록으로 구분하는 안이다; 대안은 완료 track을 별도 보관 위치로 옮기고 현 위치에 안내를 남기는 안으로 실제 이동은2단계에서 결정한다. 근거: [기존 상태 기준:20](../DOCUMENTATION_GUIDE.md#L20), 위 현재 예외.
- [제안] INDEX는 주제·현재 상태·본문 링크만 우선하고 상세 test count/하나의 SHA는 요약 소유 문서로 보내는 안이다; 대안은 탐색에 필요한 짧은 최신 수치만 유지한다. 근거: [INDEX 자신의 역할:13](../INDEX.md#L13), [현재 실행 수치 행:30](../INDEX.md#L30).

## 증거 링크와 배포 사본

- [결정됨] D-A는 원시 산출물 보관 중단·삭제 방향을 확정했지만 실제 목록 제시·삭제·링크 정리는 별도2단계 PR이다; 요약·판정 문서와 CI_GATE_PROPOSAL은 보존한다. 근거: [사용자 결정 D-A](README.md#사용자-결정), [판정 기준 링크:125](../../AGENTS.md#L125).
- [제안] 삭제할 과거 원시 링크는 그 자료가 존재하는 고정 commit의 permalink로 대체하고 요약에는 판정·한계를 남기는 안이다; 대안은 원시 링크를 없애고 source/명령/해시만 남기거나 별도 제공 위치를 사용자 선택으로 정한다. permalink는 입력 파일을 되살리거나 과거 실행을 다시 검증한 결과가 아니다. 근거: [현행 파일 읽기:23,31](../../tools/verify_validation.py#L23), [D-A와 이력 경계](07-validation-evidence.md#삭제와-현행-도구의-충돌).
- [현행] UPM 갱신은 clean source의 prepare 출력 검토→사본 반영/commit→새 SHA 기본 mode 검증 절차가 있고 Core15파일이 현재 개발본과 다르다; 문서 raw bytes가 다른 이유 중 링크/prefix 변환은 정상이다. 근거: [절차:123](../DISTRIBUTION_PIPELINE.md#L123), [변환:154](../../tools/build_distribution.py#L154), [0.5단계 비교](README.md#05단계-사실-확인).
