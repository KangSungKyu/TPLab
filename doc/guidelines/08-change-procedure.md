# 08 · 변경 절차 초안

[현행] 코드·기존 규칙의 SourceRevision은 main `35508bc1fb8ed7749a292d1f9ae983b45387afd4`다. PR #12는 미병합이므로 문서 초안 브랜치에서 후속 작성하며 이번 Unity·테스트·Player 실행은0회다. 근거: [공통 범위](README.md#근거와-검증-범위), [PR #12](https://github.com/KangSungKyu/TPLab/pull/12), [문서 전용 검증:74](../DOCUMENTATION_GUIDE.md#L74).

## 위험 등급과 공통 항목

[결정됨] **D-F — 위험 등급별 절차.** 아래4등급을 사용한다. T1의 새 Red·API 문서 비갱신은 그 PR에서 하지 않는다는 뜻이며 나중에 몰아 갱신한다는 뜻이 아니다. 근거: 사용자 2026-10-13 Phase1b D-F, [사용자 결정](README.md#사용자-결정).

| 상태 | 등급·조건 | 수행 절차 |
|---|---|---|
| [결정됨] | T0 · 문서만 | 링크·공백·규칙 검사. Unity 테스트 없음. 근거: D-F, [기존 문서 전용 조건:74](../DOCUMENTATION_GUIDE.md#L74). |
| [결정됨] | T1 · 동작·공개 시그니처 불변 리팩터링 | 기존 전체 테스트의 변경 전·후 재실행과 같은 통과 수 확인, 테스트 수 순감소 금지, 공개 시그니처 diff 없음 확인, 회고 한 단락. 새 Red·XML·사람/AI API 갱신은 하지 않으며 이동으로 틀어진 파일:줄·경로 인용만 고친다. 근거: D-F, [기존 계약 불변 예외:73](../DOCUMENTATION_GUIDE.md#L73). |
| [결정됨] | T2 · 공개 API·동작 변경 | 현행 전체 절차: 실제 Red·구현 전 동결, Green·전체 회귀, XML+사람/AI API 동시 갱신, 회고 증거 요약. 근거: D-F, [TDD:35](../../AGENTS.md#L35), [동결:23](../IMPROVEMENT_BACKLOG.md#L23), [API:26](../IMPROVEMENT_BACKLOG.md#L26). |
| [결정됨] | T3 · 동시성·수명·GC 위험 | T2 + 오라클 위험에 대한 양성 대조·대기/Timeout 검토·독립 검토. 양성 대조는 D-H8에 따라 계측·순서 등 위험이 확인된 테스트에 적용한다. 근거: D-F·D-H8, [양성 대조](05-testing.md#양성-대조-범위와-미확정-사항). |

- [결정됨] **D-F — 공통 생략 불가.** 모든 등급에서 exact-SHA CI, 사용자 병합 승인, dirty 변경 보존, 병합 전 사용자 확인을 수행한다. T1 표의 생략이 이 공통 항목을 생략하는 근거가 되지 않는다. 근거: 사용자 2026-10-13 Phase1b D-F, [현행 CI:125](../../AGENTS.md#L125), [dirty 보호:74](../../AGENTS.md#L74).
- [결정됨] **D-F — 등급 선택.** Codex가 PR 본문에 등급·근거를 적고 사용자가 확인한다. 애매하면 높은 등급을 택한다. 근거: 사용자 2026-10-13 Phase1b D-F.
- [결정됨] **D-F — 상향.** 옮길 동작을 기존 테스트가 지키지 않으면 옮기기 전에 그 동작을 고정하는 테스트를 추가하고 해당 부분은 T2 절차를 따른다. 리팩터링 중 동작 변화가 확인되면 그 PR을 T2 이상으로 올린다. 근거: 사용자 2026-10-13 Phase1b D-F, [예상 밖 결과 중단:75](../IMPROVEMENT_BACKLOG.md#L75).
- [결정됨] **D-G — Unity CI 범위.** Unity 테스트 CI(IMP-07)는 이후 별도 작업이다. 이번 문서는 범위 밖이라는 사실만 기록하고 라이선스·runner·cache·workflow를 설계하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-G, [현행 보류:277](../IMPROVEMENT_BACKLOG.md#L277).

## 1.5단계 측정 근거

- [현행] 분모는 병합 PR10개(#1,3,4,5,6,7,8,9,10,11)이며 #2는 미병합 종료, #12는 미병합이다. 내부 리팩터링 단독 PR·배포 PR은 분모에 없다. 근거: **1.5단계 측정 보고(2026-10-13)**의 GitHub 조회·main merge log, [PR #2](https://github.com/KangSungKyu/TPLab/pull/2), [PR #10의 혼합 범위](https://github.com/KangSungKyu/TPLab/pull/10).
- [현행] validation은 연 변경 파일137/262, 추가 줄23,977/28,999였다. GitHub per-file diff로 줄 수를, Git cat-file로 blob bytes를 측정한 보고이며 Git 전송·압축 크기로 해석하지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**, 대표 [CI 원시·요약 위치](../validation/ci-review-p1/README.md), [IMP-03 위치](../validation/imp-03/README.md).
- [현행] 개설 후 시각의 커밋17개 중15개는 doc/만 변경했다. 이는 PR 개설 시각과 Git committer 시각 비교의 근사값이며 실제 push·PR 추가 시각이나 사용자 작업 시간을 측정한 값이 아니다. 근거: **1.5단계 측정 보고(2026-10-13)**의 git log/diff-tree, 대표 [수락 기록](../retrospectives/2026-10-13-05-root-shutdown-timeout.md#L17).
- [현행] 최종 회귀 수치는 IMP-03/05/14의 해당 PR 변경 Markdown7/7/4파일에 반복됐다. 과거 tip의 실제 수치가 들어 있는 파일을 세었으며 링크만 있는 파일은 제외했다. 근거: **1.5단계 측정 보고(2026-10-13)**, [IMP-03:65](../validation/imp-03/README.md#L65), [IMP-05:33](../validation/imp-05/implementation/README.md#L33), [IMP-14:59](../validation/imp-14/README.md#L59).
- [현행] PR 개설→병합 중앙값은16분45.5초였다. 승인 대기와 실제 작업 시간이 분리된 값은 아니다. 근거: **1.5단계 측정 보고(2026-10-13)**의 created_at/merged_at 계산, [대상 PR #11의 시각](https://github.com/KangSungKyu/TPLab/pull/11).
- [현행] 관측 Actions25회는 모두 PR 규칙 검사이며 Unity 실행이 아니었다. commit별 pull_request 첫 페이지 조회의 관측 범위여서 모든 이벤트·재실행 attempt의 총비용을 확정하지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**, [workflow:21,33](../../.github/workflows/rule-check.yml#L21), [IMP-03 CI 분리](../validation/imp-03/README.md#L84).
- [현행] #10 병합 뒤에도 GAME_SCENE_MANAGER_DRAFT:162에는 “사용자 승인 대기”, CI_SETUP_GUIDE:3에는 “아직 저장소에 반영되지 않았다”가 남아 있었다. 이번에 해당 파일을 고치지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**, [Scene 명세:162](../GAME_SCENE_MANAGER_DRAFT.md#L162), [CI 가이드:3](../CI_SETUP_GUIDE.md#L3), [PR #10](https://github.com/KangSungKyu/TPLab/pull/10).

## 현행 절차 14묶음의 등급 적용 초안

[제안] 아래는 1.5단계 보고가 현행 요구를14묶음으로 나눈 뒤 각 등급에 연결한 세부 적용 초안이다. 모든 적용 칸은 사용자 결정 전이며 상단 D-F의 확정 등급·공통 항목을 이 표로 완화하지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**의 단계 표, 아래 규칙 원문. 대안은 세부 적용을 기존 규칙의 개별 조건대로 판단하고 별도 매핑표를 채택하지 않는 안이다.

| 상태·현행 묶음 / 근거 | T0 | T1 | T2 | T3 |
|---|---|---|---|---|
| [제안] 01 checkout·HEAD·dirty·branch — [AGENTS:111](../../AGENTS.md#L111) | [제안] 공통 | [제안] 공통 | [제안] 공통 | [제안] 공통 |
| [제안] 02 Red→Green→Refactor — [AGENTS:35](../../AGENTS.md#L35) | [제안] 비적용 | [제안] 기존 전체 전후 | [제안] 적용 | [제안] 적용 |
| [제안] 03 Red 동결·예상 밖 중단 — [백로그:23,75](../IMPROVEMENT_BACKLOG.md#L23) | [제안] Red 비적용 | [제안] 상향 여부 확인 | [제안] 적용 | [제안] 적용 |
| [제안] 04 gate·대기·Timeout — [백로그:24](../IMPROVEMENT_BACKLOG.md#L24) | [제안] 비적용 | [제안] 기존 대기 검토 | [제안] 대기 위험 시 | [제안] 위험 검토 |
| [제안] 05 ready Editor·runner — [백로그:28](../IMPROVEMENT_BACKLOG.md#L28) | [제안] Unity 없음 | [제안] 전후 실행 | [제안] 실행 | [제안] 실행 |
| [제안] 06 XML·사람/AI API — [문서지침:73](../DOCUMENTATION_GUIDE.md#L73) | [제안] 문서 근거 대조 | [제안] 비갱신·이동 인용만 | [제안] 관련 계약 갱신 | [제안] 관련 계약 갱신 |
| [제안] 07 source·상태·예제 한계 — [문서지침:18](../DOCUMENTATION_GUIDE.md#L18) | [제안] 변경 문서 | [제안] 회고·이동 인용 | [제안] 계약·증거 | [제안] 계약·증거 |
| [제안] 08 링크·공백·diff 리뷰 — [문서지침:74](../DOCUMENTATION_GUIDE.md#L74) | [제안] 적용 | [제안] 적용 | [제안] 적용 | [제안] 적용 |
| [제안] 09 회고·상태 링크 — [AGENTS:51](../../AGENTS.md#L51) | [제안] 요약 | [제안] 한 단락 | [제안] 증거 요약 | [제안] 증거 요약 |
| [제안] 10 결과·원문·NotRun 보고 — [백로그:318](../IMPROVEMENT_BACKLOG.md#L318) | [제안] 문서 검사 | [제안] 전후 결과 | [제안] Red/Green·전체 | [제안] 대조·위험도 |
| [제안] 11 사용자 확인·승인 — [백로그:58](../IMPROVEMENT_BACKLOG.md#L58) | [제안] 공통필수 | [제안] 공통필수 | [제안] 공통필수 | [제안] 공통필수 |
| [제안] 12 규칙·exact-SHA CI — [AGENTS:125](../../AGENTS.md#L125) | [제안] 공통필수 | [제안] 공통필수 | [제안] 공통필수 | [제안] 공통필수 |
| [제안] 13 병합 후 refs·dirty·branch — [AGENTS:123,129](../../AGENTS.md#L123) | [제안] 승인 후 | [제안] 승인 후 | [제안] 승인 후 | [제안] 승인 후 |
| [제안] 14 외부 설치·예제 검증 — [문서지침:75](../DOCUMENTATION_GUIDE.md#L75) | [제안] 외부 제공 시 | [제안] 외부 제공 시 | [제안] 외부 제공 시 | [제안] 외부 제공 시 |

## 회고에 남길 증거 요약

[결정됨] **D-E — 요약 항목.** 검증 source SHA, 실행 명령, 환경, 대상/전체 실행 수·실패·skip, 한계·미실행, 실패 메시지 원문 요약, Actions 링크를 회고 한 곳에 남긴다. 판정·source와 최종 commit metadata를 구별하며 새 validation README·PR 본문·색인에 같은 요약을 복제하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-E, [현행 보고:323](../IMPROVEMENT_BACKLOG.md#L323), [회고 source/commit 구분:53](../../AGENTS.md#L53).

## 오래 가는 인용과 첫 적용 시범

- [제안] 코드 이동은 파일:줄·경로 인용을 틀리게 할 수 있으므로 (a) 심볼 이름 중심, (b) commit SHA 고정 permalink, (c) 현행 인용을 유지하고 T1에서 이동 인용만 갱신하는 대안을 비교한다. 어느 방식을 채택할지는 사용자 결정 전이다. 근거: [기존 인용:18](../DOCUMENTATION_GUIDE.md#L18), [분리 대상](01-code-conventions.md#크기와-책임-분리-판단).
- [결정됨] **D-H12 — 첫 시범.** EnsureMainThread 통합(5곳 구현)을3단계 T1 절차의 첫 적용 시범으로 삼는다. 이 문서 PR은 그 코드의 설계·통합·테스트를 시작하지 않는다. 근거: 사용자 2026-10-13 일괄 승인, [5곳 근거](01-code-conventions.md#로그와-메인-스레드-검사).
- [결정됨] 검토했으나 채택하지 않은 대안: 다른3단계 항목을 T1 첫 시범으로 선택하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H12).
