# 08 · 변경 절차

[현행] SourceRevision: main `af084e0d0027c4098fad08c336ccb0753e1c7862`. 갱신일: 2026-10-15. 현재 지침·인용을 읽기/Git 조회로 대조했으며 Unity·테스트·Player 실행은 0회다. 기존 실행·집계 수치는 각 기록의 당시 source 기준이며 이번 재실행·재집계로 보지 않는다. [공통 근거와 검증 범위](README.md#근거와-검증-범위).

## 위험 등급과 공통 항목

[결정됨] **D-F — 위험 등급별 절차.** 아래4등급을 사용한다. T1의 새 Red·API 문서 비갱신은 그 PR에서 하지 않는다는 뜻이며 나중에 몰아 갱신한다는 뜻이 아니다. 근거: 사용자 2026-10-13 Phase1b D-F, [사용자 결정](README.md#사용자-결정).

| 상태 | 등급·조건 | 수행 절차 |
|---|---|---|
| [결정됨] | T0 · 문서만 | 링크·공백·규칙 검사. Unity 테스트 없음. 근거: D-F, [기존 문서 전용 조건:74](../DOCUMENTATION_GUIDE.md#L74). |
| [결정됨] | T1 · 동작·공개 시그니처 불변 리팩터링 | 기존 전체 테스트의 변경 전·후 재실행과 같은 통과 수 확인, 테스트 수 순감소 금지, 공개 시그니처 diff 없음 확인, 회고 한 단락. 새 Red·XML·사람/AI API 갱신은 하지 않으며 이동으로 틀어진 파일:줄·경로 인용만 고친다. 근거: D-F, [기존 계약 불변 예외:73](../DOCUMENTATION_GUIDE.md#L73). |
| [결정됨] | T2 · 공개 API·동작 변경 | 현행 전체 절차: 실제 Red·구현 전 동결, Green·전체 회귀, XML+사람/AI API 동시 갱신, 회고 증거 요약. 근거: D-F, [TDD:39](../../AGENTS.md#L39), [동결:23](../IMPROVEMENT_BACKLOG.md#L23), [API:26](../IMPROVEMENT_BACKLOG.md#L26). |
| [결정됨] | T3 · 동시성·수명·GC 위험 | T2 + 오라클 위험에 대한 양성 대조·대기/Timeout 검토·독립 검토. 양성 대조는 D-H8에 따라 계측·순서 등 위험이 확인된 테스트에 적용한다. 근거: D-F·D-H8, [양성 대조](05-testing.md#양성-대조-범위와-미확정-사항). |

- [결정됨] **D-F — 공통 생략 불가.** 모든 등급에서 exact-SHA CI, 사용자 병합 승인, dirty 변경 보존, 병합 전 사용자 확인을 수행한다. T1 표의 생략이 이 공통 항목을 생략하는 근거가 되지 않는다. 근거: 사용자 2026-10-13 Phase1b D-F, [현행 CI:118](../../AGENTS.md#L118), [dirty 보호:75](../../AGENTS.md#L75).
- [결정됨] **D-F — 등급 선택.** Codex가 PR 본문에 등급·근거를 적고 사용자가 확인한다. 애매하면 높은 등급을 택한다. 근거: 사용자 2026-10-13 Phase1b D-F.
- [결정됨] **D-F — 상향.** 옮길 동작을 기존 테스트가 지키지 않으면 옮기기 전에 그 동작을 고정하는 테스트를 추가하고 해당 부분은 T2 절차를 따른다. 리팩터링 중 동작 변화가 확인되면 그 PR을 T2 이상으로 올린다. 근거: 사용자 2026-10-13 Phase1b D-F, [예상 밖 결과 중단:75](../IMPROVEMENT_BACKLOG.md#L75).
- [결정됨] **D-G — Unity CI 범위.** Unity 테스트 CI(IMP-07)는 이후 별도 작업이다. 이번 문서는 범위 밖이라는 사실만 기록하고 라이선스·runner·cache·workflow를 설계하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-G, [현행 보류:277](../IMPROVEMENT_BACKLOG.md#L277).

## 1.5단계 측정 근거

- [현행] 2026-10-13 조사 당시 분모는 병합 PR10개(#1,3,4,5,6,7,8,9,10,11)이며 #2는 미병합 종료, #12는 미병합이다. 내부 리팩터링 단독 PR·배포 PR은 분모에 없다. 근거: **1.5단계 측정 보고(2026-10-13)**의 GitHub 조회·main merge log, [PR #2](https://github.com/KangSungKyu/TPLab/pull/2), [PR #10의 혼합 범위](https://github.com/KangSungKyu/TPLab/pull/10).
- [현행] validation은 연 변경 파일137/262, 추가 줄23,977/28,999였다. GitHub per-file diff로 줄 수를, Git cat-file로 blob bytes를 측정한 보고이며 Git 전송·압축 크기로 해석하지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**, 대표 [CI 원시·요약 위치](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/ci-review-p1/README.md), [IMP-03 위치](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md).
- [현행] 개설 후 시각의 커밋17개 중15개는 doc/만 변경했다. 이는 PR 개설 시각과 Git committer 시각 비교의 근사값이며 실제 push·PR 추가 시각이나 사용자 작업 시간을 측정한 값이 아니다. 근거: **1.5단계 측정 보고(2026-10-13)**의 git log/diff-tree, 대표 [수락 기록](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/retrospectives/2026-10-13-05-root-shutdown-timeout.md#L17).
- [현행] 최종 회귀 수치는 IMP-03/05/14의 해당 PR 변경 Markdown7/7/4파일에 반복됐다. 과거 tip의 실제 수치가 들어 있는 파일을 세었으며 링크만 있는 파일은 제외했다. 근거: **1.5단계 측정 보고(2026-10-13)**, [IMP-03:65](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md#L65), [IMP-05:33](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L33), [IMP-14:59](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L59).
- [현행] PR 개설→병합 중앙값은16분45.5초였다. 승인 대기와 실제 작업 시간이 분리된 값은 아니다. 근거: **1.5단계 측정 보고(2026-10-13)**의 created_at/merged_at 계산, [대상 PR #11의 시각](https://github.com/KangSungKyu/TPLab/pull/11).
- [현행] 관측 Actions25회는 모두 PR 규칙 검사이며 Unity 실행이 아니었다. commit별 pull_request 첫 페이지 조회의 관측 범위여서 모든 이벤트·재실행 attempt의 총비용을 확정하지 않는다. 근거: **1.5단계 측정 보고(2026-10-13)**, [workflow:21,33](../../.github/workflows/rule-check.yml#L21), [IMP-03 CI 분리](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md#L84).
- [현행] 2026-10-13 1.5단계 조사 당시 GAME_SCENE_MANAGER_DRAFT와 CI_SETUP_GUIDE에는 이전 상태 문장이 남아 있었다. 현재 상태와 구분하며 당시 [Scene 명세](https://github.com/KangSungKyu/TPLab/blob/35508bc1fb8ed7749a292d1f9ae983b45387afd4/doc/GAME_SCENE_MANAGER_DRAFT.md), [CI 가이드:3](https://github.com/KangSungKyu/TPLab/blob/35508bc1fb8ed7749a292d1f9ae983b45387afd4/doc/CI_SETUP_GUIDE.md#L3)를 인용한다. 근거: 1.5단계 측정 보고(2026-10-13).

## 현행 절차 14묶음의 등급 적용

[결정됨] **D-I6 — 14묶음 × 4등급 적용.** 현행 관행의 56칸을 그대로 채택한다. 아래 칸의 내용은 변경하지 않고 [제안] 라벨만 [결정됨]으로 바꿨다. 사용자 병합 승인·exact-SHA CI·dirty 보존 등 D-F의 공통 필수는 완화하지 않는다. 근거: 사용자 결정 2026-10-15(D-I6), 1.5단계 측정 보고(2026-10-13).

| 상태·현행 묶음 / 근거 | T0 | T1 | T2 | T3 |
|---|---|---|---|---|
| [결정됨] 01 checkout·HEAD·dirty·branch — [AGENTS:104](../../AGENTS.md#L104) | [결정됨] 공통 | [결정됨] 공통 | [결정됨] 공통 | [결정됨] 공통 |
| [결정됨] 02 Red→Green→Refactor — [AGENTS:39](../../AGENTS.md#L39) | [결정됨] 비적용 | [결정됨] 기존 전체 전후 | [결정됨] 적용 | [결정됨] 적용 |
| [결정됨] 03 Red 동결·예상 밖 중단 — [백로그:23,75](../IMPROVEMENT_BACKLOG.md#L23) | [결정됨] Red 비적용 | [결정됨] 상향 여부 확인 | [결정됨] 적용 | [결정됨] 적용 |
| [결정됨] 04 gate·대기·Timeout — [백로그:24](../IMPROVEMENT_BACKLOG.md#L24) | [결정됨] 비적용 | [결정됨] 기존 대기 검토 | [결정됨] 대기 위험 시 | [결정됨] 위험 검토 |
| [결정됨] 05 ready Editor·runner — [백로그:28](../IMPROVEMENT_BACKLOG.md#L28) | [결정됨] Unity 없음 | [결정됨] 전후 실행 | [결정됨] 실행 | [결정됨] 실행 |
| [결정됨] 06 XML·사람/AI API — [문서지침:73](../DOCUMENTATION_GUIDE.md#L73) | [결정됨] 문서 근거 대조 | [결정됨] 비갱신·이동 인용만 | [결정됨] 관련 계약 갱신 | [결정됨] 관련 계약 갱신 |
| [결정됨] 07 source·상태·예제 한계 — [문서지침:18](../DOCUMENTATION_GUIDE.md#L18) | [결정됨] 변경 문서 | [결정됨] 회고·이동 인용 | [결정됨] 계약·증거 | [결정됨] 계약·증거 |
| [결정됨] 08 링크·공백·diff 리뷰 — [문서지침:74](../DOCUMENTATION_GUIDE.md#L74) | [결정됨] 적용 | [결정됨] 적용 | [결정됨] 적용 | [결정됨] 적용 |
| [결정됨] 09 회고·상태 링크 — [AGENTS:56](../../AGENTS.md#L56) | [결정됨] 요약 | [결정됨] 한 단락 | [결정됨] 증거 요약 | [결정됨] 증거 요약 |
| [결정됨] 10 결과·원문·NotRun 보고 — [백로그:318](../IMPROVEMENT_BACKLOG.md#L318) | [결정됨] 문서 검사 | [결정됨] 전후 결과 | [결정됨] Red/Green·전체 | [결정됨] 대조·위험도 |
| [결정됨] 11 사용자 확인·승인 — [백로그:58](../IMPROVEMENT_BACKLOG.md#L58) | [결정됨] 공통필수 | [결정됨] 공통필수 | [결정됨] 공통필수 | [결정됨] 공통필수 |
| [결정됨] 12 규칙·exact-SHA CI — [AGENTS:118](../../AGENTS.md#L118) | [결정됨] 공통필수 | [결정됨] 공통필수 | [결정됨] 공통필수 | [결정됨] 공통필수 |
| [결정됨] 13 병합 후 refs·dirty·branch — [AGENTS:116,122](../../AGENTS.md#L116) | [결정됨] 승인 후 | [결정됨] 승인 후 | [결정됨] 승인 후 | [결정됨] 승인 후 |
| [결정됨] 14 외부 설치·예제 검증 — [문서지침:75](../DOCUMENTATION_GUIDE.md#L75) | [결정됨] 외부 제공 시 | [결정됨] 외부 제공 시 | [결정됨] 외부 제공 시 | [결정됨] 외부 제공 시 |

## 회고에 남길 증거 요약

[결정됨] **D-E — 요약 항목.** 검증 source SHA, 실행 명령, 환경, 대상/전체 실행 수·실패·skip, 한계·미실행, 실패 메시지 원문 요약, Actions 링크를 회고 한 곳에 남긴다. 판정·source와 최종 commit metadata를 구별하며 새 validation README·PR 본문·색인에 같은 요약을 복제하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-E, [현행 보고:323](../IMPROVEMENT_BACKLOG.md#L323), [회고 상세](06-documentation.md#단위-작업-회고-상세--2026-10-13-이관).

## 오래 가는 인용과 첫 적용 시범

- [결정됨] **D-I7 — 오래 가는 인용.** 코드 인용은 심볼 이름 + 경로를 기본으로 하고 줄 번호는 보조로 사용한다. 증거 문서는 commit SHA permalink를 사용한다. 근거: 사용자 결정 2026-10-15(D-I7), [문서 근거 기준](../DOCUMENTATION_GUIDE.md#공통-사실과-상태).
- [결정됨] **D-H12 — 시범 미실행과 종료.** 2026-10-13에는 EnsureMainThread 5곳 통합을 T1 첫 시범으로 채택했으나 실제 실행하지 않았다. 이후 메시지·검사 순서·assembly 경계·테스트 공백 조사에서 T2로 정정했고, 2026-10-15 사용자는 통합하지 않기로 결정했다. 5곳 통합이나 T1 시범이 완료됐다는 뜻이 아니다. 3단계 산출물은 D-C에 따라 안전망 57 case이며 실제 클래스 분리도 하지 않는다. [사용자 결정](README.md#사용자-결정), [종료 회고](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/retrospectives/2026-10-15-02-phase3-close.md).
- [결정됨] 2026-10-13 당시 검토했으나 채택하지 않은 대안은 다른3단계 항목을 T1 첫 시범으로 선택하는 안이었다. 이 과거 기록은 위 2026-10-15 시범 미실행·통합하지 않음 결정을 대신하지 않는다. 근거: 사용자 두 결정(D-H12).
