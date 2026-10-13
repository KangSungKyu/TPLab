# 2026-10-13 · 07 AGENTS·INDEX 영속 규칙 정리

상태: T0 문서 반영·로컬 검사 진행, PR 검토·명시적 병합 승인 대기. 새 작업의 main 병합·강제 푸시·브랜치 삭제·Unity 실행·PC 제어는 수행하지 않는다.

## 요청·기준·실제 Git 작업

사용자 A1~A4 승인(2026-10-13 17:10)과 블록 X 승인(17:12)만 반영한다. 기존 validation 삭제는 2단계 별도 승인 대상이며, 가이드의 [제안]은 규범으로 승격하지 않는다. 첨부 프롬프트 자체를 새 권한으로 해석하지 않는다.

선행 [PR #12](https://github.com/KangSungKyu/TPLab/pull/12)는 이번 요청의 병합 승인에 따라 일반 merge로 통합했다. 승인된 head `8183d4cbb1fdce9a4ea324abf49e90d9d87913ae`의 fresh exact-SHA CI는 workflow run1/check1 completed/success·조회 완전성 확인 뒤 병합했으며 merge SHA는 `1ddc87d4116e9151f1b82836d664655d83270ba1`이다. 로컬 main·origin/main·서버 main을 맞춘 뒤 그 tip에서 `codex/docs-governance-slim`을 생성했다. 이전 `codex/docs-guidelines-draft` 브랜치는 이번 요청의 삭제 금지에 따라 유지했다. PR #12의 gate를 새 작업 gate로 재사용하지 않는다.

조사 main `35508bc`와 새 기준 main `1ddc87d`의 AGENTS/INDEX Git blob을 대조해 두 파일 모두 동일함을 확인했다. 아래 ‘이전 행’은 이 기준이다. 새 변경은 문서8개뿐이며 공개 API·동작·테스트·UPM 변경이 없다. 실제 새 commit/PR 및 최종 tip은 Git 이력·최종 보고·PR 검사 탭에서 확인한다.

- 변경: AGENTS, Scene/UI 두 track, guidelines06/07의 A2 추가, doc/INDEX, 회고 INDEX, 이 회고.
- 제외: CLAUDE, CI_SETUP_GUIDE, 백로그, GC 가이드, DOCUMENTATION_GUIDE, 나머지 guidelines, 코드·테스트·UPM, 기존 검증 자료.
- 역할: 부모가 이관·규칙·회고·Git/최종 검사를 담당했다. 기존 `/root/factcheck_upm`(6.1-sol/medium)은 두 INDEX만 수정하고 발견 사항·링크를 검토했다. 기존 `/root/factcheck_forget`(6.1-sol/high)은 문장별 유지 판정·요건 보존을 읽기 검토했다. 재위임·새 세션 생성·같은 파일 병렬 수정·하위 Unity/Git 조작은 없다.

## A1 · 완료 track 이관과 영속 규칙 판정

AGENTS의 두 track 본문을 [Scene 역사 절](../SCENE_TRANSITION_TRACK.md#당시-운영-지침역사적-기록-2026-10-062026-10-07)과 [UI 역사 절](../GAME_UI_SYSTEM_TRACK.md#당시-운영-지침역사적-기록-2026-10-082026-10-12)에 추가했다. **본문13개 항목·45문장 전체**를 기준 blob과 대조해 원문 일치를 확인했다. Markdown 원문 블록을 사용해 당시 상대 URL까지 보존했고, 블록 밖에 현재 이동 가능한 링크를 제공했다. 완료 상태·당시 날짜·이관 날짜·‘현재 요청의 권한으로 승계하지 않는다’를 명시했다. 기존 track 본문은 변경하지 않았다.

문장 판정은 영속 유지21·애매하여 유지9·당시 운영 이관15다. 모든 원문은 역사 절에 보존하며, 아래 ‘유지’는 AGENTS에도 남기는 의미다. 혼합 문장의 지원 모델 확인·미검증 표시·승인 gate는 별도 영속 규칙으로 남기고 구체 모델·Phase 일정·당시 증거 위치만 역사로 구분했다.

| 기준 행.문장 | 판정 | 원문 문장 | AGENTS 유지 위치 또는 이관 이유 |
|---|---|---|---|
| 82.1 | 당시 운영 이관 | `` GameSceneManager Phase 통합은 [SCENE_TRANSITION_TRACK.md](doc/SCENE_TRANSITION_TRACK.md)를 기준으로 한다. `` | Scene track 역사 절 — 완료 track의 특정 SHA·Phase·탐색 경로와 당시 증거 소유 방식. 현재 요약 소유는 D-E를 따른다. |
| 82.2 | 당시 운영 이관 | `` main `e9fa4e46f1dc8fe19800800a2229668cb8b4a432`에서 생성한 `codex/game-scenes-track`의 최신 검증 tip에서 Phase 브랜치를 생성하고 `p0-guidelines` → `p0-loaders` → `p3a-flow` → `p3b-areas` → `p4-definitions` → `p5-editor` → `p6-validation`을 순서대로 통합한다. `` | Scene track 역사 절 — 완료 track의 특정 SHA·Phase·탐색 경로와 당시 증거 소유 방식. 현재 요약 소유는 D-E를 따른다. |
| 82.3 | 당시 운영 이관 | `` 현재 단위·commit·실제 증거는 track 문서 한 곳에서 갱신한다. `` | Scene track 역사 절 — 완료 track의 특정 SHA·Phase·탐색 경로와 당시 증거 소유 방식. 현재 요약 소유는 D-E를 따른다. |
| 82.4 | 당시 운영 이관 | `` Phase 0은 Addressables/Build loader 명시 선택과 씬 instance 소유권을 다룬다. `` | Scene track 역사 절 — 완료 track의 특정 SHA·Phase·탐색 경로와 당시 증거 소유 방식. 현재 요약 소유는 D-E를 따른다. |
| 82.5 | 당시 운영 이관 | `` Phase 3A/3B/4/5/6 계약은 [GAME_SCENE_MANAGER_DRAFT.md](doc/GAME_SCENE_MANAGER_DRAFT.md), 최종 한 번의 사용자 확인 기준은 [SCENE_TRANSITION_ACCEPTANCE.md](doc/SCENE_TRANSITION_ACCEPTANCE.md)를 참조한다. `` | Scene track 역사 절 — 완료 track의 특정 SHA·Phase·탐색 경로와 당시 증거 소유 방식. 현재 요약 소유는 D-E를 따른다. |
| 83.1 | 애매하여 유지 | `` 부모 에이전트가 공용 계약·작업 배정·결과 리뷰·회고·Git 통합·Unity 조작을 소유한다. `` | [AGENTS.md:85](../../AGENTS.md#L85) — 담당·위임 한도·위험 표시·미확정 설계 보호를 완화하지 않는다. track 표 형식만 당시 운영이다. |
| 83.2 | 당시 운영 이관 | `` 하위 에이전트는 현재 지원 모델을 확인하고 검색은 luna/low, 작고 확정된 구현은 luna/medium, 통합 구현은 6.1-sol/medium을 우선 선택한다. `` | Scene track 역사 절 / AGENTS 모델 확인 원칙 — 선호 모델 조합은 역사로 옮기고 실제 지원 모델 확인은 AGENTS에 남긴다. |
| 83.3 | 애매하여 유지 | `` 비동기 수명·소유권 위험은 high로 기록하고 필요하면 추론을 올린다. `` | [AGENTS.md:85](../../AGENTS.md#L85) — 담당·위임 한도·위험 표시·미확정 설계 보호를 완화하지 않는다. track 표 형식만 당시 운영이다. |
| 83.4 | 애매하여 유지 | `` 역할·선택 근거·허용 경로·상태를 track 표에 남기며, 미확정 설계는 구현 담당에게 넘기지 않는다. `` | [AGENTS.md:85](../../AGENTS.md#L85) — 담당·위임 한도·위험 표시·미확정 설계 보호를 완화하지 않는다. track 표 형식만 당시 운영이다. |
| 84.1 | 애매하여 유지 | `` 기본 하위 에이전트는 1개, 독립 작업이 필요한 경우 최대 2개다. `` | [AGENTS.md:85](../../AGENTS.md#L85) — 담당·위임 한도·위험 표시·미확정 설계 보호를 완화하지 않는다. track 표 형식만 당시 운영이다. |
| 84.2 | 영속 유지 | `` 같은 파일 또는 공용 계약을 동시에 수정하지 않는다. `` | [AGENTS.md:86](../../AGENTS.md#L86) — 동시 수정·테스트 입력 동결·Editor 담당·새 세션 권한 보호. |
| 84.3 | 영속 유지 | `` Unity 테스트 중 소스 변경을 동결하고, 기존 TPLab Editor는 부모만 조작한다. `` | [AGENTS.md:86](../../AGENTS.md#L86) — 동시 수정·테스트 입력 동결·Editor 담당·새 세션 권한 보호. |
| 84.4 | 영속 유지 | `` 새 사용자 소유 세션을 만들지 않는다. `` | [AGENTS.md:86](../../AGENTS.md#L86) — 동시 수정·테스트 입력 동결·Editor 담당·새 세션 권한 보호. |
| 85.1 | 당시 운영 이관 | `` 각 Phase의 자동 검증·리뷰·회고가 완료되면 해당 Phase를 track에 통합한다. `` | Scene track 역사 절 / AGENTS 등급별 통합 — 특정 Phase 절차와 최종 확인 일정은 역사로 옮긴다. 검증·미검증 구분은 유지한다. |
| 85.2 | 영속 유지 | `` 제안·진행·자동 검증 완료·사용자 확인 대기·완료 상태를 구분하고 증거가 있기 전 완료로 바꾸지 않는다. `` | [AGENTS.md:87](../../AGENTS.md#L87) — 증거와 상태를 구분하고 실제 검증 전 완료로 표시하지 않는다. |
| 85.3 | 당시 운영 이관 | `` 최종 시각·사용성 확인은 마지막에 모으되 미검증 영역을 명시한다. `` | Scene track 역사 절 / AGENTS 등급별 통합 — 특정 Phase 절차와 최종 확인 일정은 역사로 옮긴다. 검증·미검증 구분은 유지한다. |
| 86.1 | 영속 유지 | `` 자동 검증 완료와 최종 사용자 확인은 별도 gate다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — 자동 검증·사용자 수락·승인 분리, 대기 중 병합·삭제 금지, 무응답≠승인. 86.5는 X의 명시적 승인으로 강화한다. |
| 86.2 | 영속 유지 | `` 사용자의 명시적인 확인이 바로 이어지지 않으면 최종 확인 대기 상태로 둔다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — 자동 검증·사용자 수락·승인 분리, 대기 중 병합·삭제 금지, 무응답≠승인. 86.5는 X의 명시적 승인으로 강화한다. |
| 86.3 | 영속 유지 | `` 대기 중에는 `main` 병합과 브랜치 삭제를 하지 않는다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — 자동 검증·사용자 수락·승인 분리, 대기 중 병합·삭제 금지, 무응답≠승인. 86.5는 X의 명시적 승인으로 강화한다. |
| 86.4 | 영속 유지 | `` 시간 경과·무응답은 확인이나 승인으로 보지 않는다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — 자동 검증·사용자 수락·승인 분리, 대기 중 병합·삭제 금지, 무응답≠승인. 86.5는 X의 명시적 승인으로 강화한다. |
| 86.5 | 영속 유지 | `` `main`은 필요한 자동 검증과 사용자의 명시적 최종 확인이 모두 끝난 뒤에만 병합·푸시한다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — 자동 검증·사용자 수락·승인 분리, 대기 중 병합·삭제 금지, 무응답≠승인. 86.5는 X의 명시적 승인으로 강화한다. |
| 86.6 | 애매하여 유지 | `` main 진행 시 최신 main을 track에 통합하고 영향을 받는 검증을 다시 실행한다. `` | [AGENTS.md:88](../../AGENTS.md#L88) — track 방식은 역사이나 최신 main 변경에 따른 영향 검증은 유지한다. |
| 87.1 | 영속 유지 | `` 통합은 원본 commit을 보존하는 fast-forward 또는 일반 merge를 사용한다. `` | [AGENTS.md:89](../../AGENTS.md#L89) — 원본 SHA 보존, 최종 push·도달성·다른 worktree 보호. 삭제 권한을 새로 부여하지 않는다. |
| 87.2 | 영속 유지 | `` track/Phase 브랜치는 최종 main push와 SHA 보존을 확인한 뒤에만 삭제하며, 다른 worktree가 사용 중이면 보류한다. `` | [AGENTS.md:89](../../AGENTS.md#L89) — 원본 SHA 보존, 최종 push·도달성·다른 worktree 보호. 삭제 권한을 새로 부여하지 않는다. |
| 88.1 | 영속 유지 | `` PC 종료 지시는 이번 요청에 한정한다. `` | [AGENTS.md:90](../../AGENTS.md#L90) — 전원 지시의 요청 한정과 진행 작업 보호. 이번 작업은 전원 조작 없음. |
| 88.2 | 당시 운영 이관 | `` 구현·자동 검증까지 끝나고 최종 사용자 확인만 대기 중이면 track 결과를 보존하고 Unity를 저장하되 종료하지 않은 채 절전할 수 있다. `` | Scene track 역사 절 — 당시 요청의 구체 종료·절전·실패 대응이며 현재 권한으로 승계하지 않는다. |
| 88.3 | 영속 유지 | `` 작업 또는 테스트가 진행 중이면 절전하지 않는다. `` | [AGENTS.md:90](../../AGENTS.md#L90) — 전원 지시의 요청 한정과 진행 작업 보호. 이번 작업은 전원 조작 없음. |
| 88.4 | 당시 운영 이관 | `` 최종 확인·main 통합·정리까지 완료한 경우 Unity 저장 후 정상 종료를 확인하고 PC를 종료한다. `` | Scene track 역사 절 — 당시 요청의 구체 종료·절전·실패 대응이며 현재 권한으로 승계하지 않는다. |
| 88.5 | 당시 운영 이관 | `` Unity 정상 종료가 실패하면 오프라인 전환 후 절전한다. `` | Scene track 역사 절 — 당시 요청의 구체 종료·절전·실패 대응이며 현재 권한으로 승계하지 않는다. |
| 92.1 | 당시 운영 이관 | `` 사용자 승인된 P0~P7은 [UIContext track](doc/GAME_UI_SYSTEM_TRACK.md), 공용 계약은 [GAME_UI_SYSTEM.md](doc/GAME_UI_SYSTEM.md)를 따른다. `` | UI track 역사 절 — 완료 UI P0~P7·P7 일정과 당시 종료 권한. 확인 전 병합 금지는 영속 gate에 남긴다. |
| 92.2 | 영속 유지 | `` 명칭/namespace/assembly는 UIContext/TPLab.UI다. `` | [AGENTS.md:22](../../AGENTS.md#L22) — UI 명칭·소스 경계·의존 방향·선택 Input 계약과 자동 범위 확대 금지. |
| 92.3 | 영속 유지 | `` 새 UI source는 Assets/TPLab/UI이고 기존 Core/Input 소스 이동·배포 사본·버전/tag는 자동 포함하지 않는다. `` | [AGENTS.md:22](../../AGENTS.md#L22) — UI 명칭·소스 경계·의존 방향·선택 Input 계약과 자동 범위 확대 금지. |
| 93.1 | 영속 유지 | `` UI Runtime은 TPLab.Core·UniTask·uGUI를 사용하되 UnityEditor와 게임 업무 로직을 참조하지 않는다. `` | [AGENTS.md:22](../../AGENTS.md#L22) — UI 명칭·소스 경계·의존 방향·선택 Input 계약과 자동 범위 확대 금지. |
| 93.2 | 영속 유지 | `` Core runtime에서 UI 역참조는 금지한다. `` | [AGENTS.md:22](../../AGENTS.md#L22) — UI 명칭·소스 경계·의존 방향·선택 Input 계약과 자동 범위 확대 금지. |
| 93.3 | 영속 유지 | `` Input System 연결은 선택 assembly/adapter 경계로 두며 passive Virtual ScrollRect에 Input wrapper를 강제하지 않는다. `` | [AGENTS.md:22](../../AGENTS.md#L22) — UI 명칭·소스 경계·의존 방향·선택 Input 계약과 자동 범위 확대 금지. |
| 94.1 | 영속 유지 | `` root별 Context 수명, borrowed ResourceManager/InputManager와 소유 clone/표시 작업/구독·lease를 구분한다. `` | [AGENTS.md:35](../../AGENTS.md#L35) — borrowed/owned 구분, 준비·표시의 공통 경로, await 종료와 파괴 fallback. |
| 94.2 | 영속 유지 | `` Scene Prepare의 등록/선행 자산 준비와 런타임 표시가 같은 준비·생성 경로를 사용한다. `` | [AGENTS.md:35](../../AGENTS.md#L35) — borrowed/owned 구분, 준비·표시의 공통 경로, await 종료와 파괴 fallback. |
| 94.3 | 영속 유지 | `` 정상 root 해제에서 UI Shutdown을 await하고 Unity 파괴 fallback도 검증한다. `` | [AGENTS.md:35](../../AGENTS.md#L35) — borrowed/owned 구분, 준비·표시의 공통 경로, await 종료와 파괴 fallback. |
| 95.1 | 당시 운영 이관 | `` 각 Phase에서 실제 Red→Green·관련 회귀·XML/사람/AI 문서·회고를 수행한 결과만 최신 검증 track에 통합한다. `` | UI track 역사 절 — 완료 UI P0~P7·P7 일정과 당시 종료 권한. 확인 전 병합 금지는 영속 gate에 남긴다. |
| 95.2 | 애매하여 유지 | `` 부모만 Unity/Git/최종 리뷰를 맡으며 테스트 중 source를 동결한다. `` | [AGENTS.md:86](../../AGENTS.md#L86) — 당시 분담 문장이지만 Editor/Git 통제와 테스트 입력 보호를 완화하지 않는다. |
| 95.3 | 애매하여 유지 | `` 기본 하위 agent1개·독립 작업 시 최대2개를 사용하고 모델/추론/허용 파일/상태를 track에 기록한다. `` | [AGENTS.md:85](../../AGENTS.md#L85) — 담당·위임 한도·위험 표시·미확정 설계 보호를 완화하지 않는다. track 표 형식만 당시 운영이다. |
| 96.1 | 애매하여 유지 | `` Virtual ScrollRect는 필수1,000/10,000 항목에서 기능·총소유/누적 생성 상한을 확인한다. `` | [AGENTS.md:44](../../AGENTS.md#L44) — 기능별 영속 검증인지 당시 P7만의 조건인지 애매하므로 1,000/10,000과 비교군·실제 실행 구분을 유지한다. 원시 보관은 D-E/H10을 따른다. |
| 96.2 | 애매하여 유지 | `` 기본 ScrollRect1,000↔virtual1,000 성능비교와 virtual10,000 확장성 결과를 구분하고 실제 Profiler/Player 결과를 보존한다. `` | [AGENTS.md:44](../../AGENTS.md#L44) — 기능별 영속 검증인지 당시 P7만의 조건인지 애매하므로 1,000/10,000과 비교군·실제 실행 구분을 유지한다. 원시 보관은 D-E/H10을 따른다. |
| 97.1 | 당시 운영 이관 | `` 최종 사용자 시각/실제 입력 확인은 P7에 모으며 미확인 시 main 병합·작업 branch 삭제를 보류한다. `` | UI track 역사 절 — 완료 UI P0~P7·P7 일정과 당시 종료 권한. 확인 전 병합 금지는 영속 gate에 남긴다. |
| 97.2 | 당시 운영 이관 | `` 이 UI 구현 요청에는 Unity/PC 종료 권한을 추가하지 않는다. `` | UI track 역사 절 — 완료 UI P0~P7·P7 일정과 당시 종료 권한. 확인 전 병합 금지는 영속 gate에 남긴다. |

애매한9문장은 부모 역할·위임 한도·high 표시·허용 범위 기록·최신 main 영향 검증·Editor/Git 분담·Virtual ScrollRect 대규모 기준이다. 이를 삭제해 통제나 검증을 완화하지 않았다. 역할 도입부의 3자 분업 정정은 발견 사항으로만 남긴다. 영속 절전 규칙은 요청 한정·자동 승계 없음·진행 작업 보호만 남겼으며 과거 구체 전원 절차는 역사다.

## A2 · 기존 문장 → 새 위치 대조

회고6항목의18문장은 [06장의 회고 상세](../guidelines/06-documentation.md#단위-작업-회고-상세--2026-10-13-이관)에 그대로 전사했다. 색인의 상대 URL과 [결정됨] 표시만 현재 위치에 맞게 조정했다. 증거2항목은 07에 원문을 보존하고 현재 D-E·삭제 전 보존 경계를 추가했다. 아래는 각 문장 단위의 대응이다.

| 기존 AGENTS 문장 | 핵심 요건 | 새 가이드 위치 | 보존 확인 |
|---|---|---|---|
| 51.1–3 | 단위 정의·완료/중단/차단·최종 보고/인계 전·단순 도구 호출마다 생성하지 않음 | [06-documentation.md:46](../guidelines/06-documentation.md#L46) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 52.1 | 날짜-NN-topic 경로·색인 날짜/주제/상태/링크 | [06-documentation.md:47](../guidelines/06-documentation.md#L47) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 52.2–3 | 같은 날짜 순번·같은 단위는 날짜 붙여 보완·별개 단위는 새 기록 | [06-documentation.md:47](../guidelines/06-documentation.md#L47) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 53.1 | 범위·기준 branch/HEAD·변경/결정/이유·문제/원인/해결/교훈·검증·NotRun·위험/다음 작업/선행 조건 | [06-documentation.md:48](../guidelines/06-documentation.md#L48) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 53.2–3 | 수행 Git/남은 통합 구별·회고 포함 최종 ID는 이력/최종 보고, 자기 파일 기재 필수 아님 | [06-documentation.md:48](../guidelines/06-documentation.md#L48) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 54.1–2 | 확정/제안/미확정·구현/초안/검증 대기·실행 수/fail/skip·미실행 | [06-documentation.md:49](../guidelines/06-documentation.md#L49) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 54.3–4 | 기존 명세/코드/증거 링크·전문/전체 로그/비밀값 복제 금지·영속 계약/규칙 본문 소유 | [06-documentation.md:49](../guidelines/06-documentation.md#L49) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 55.1 | 다음 단위에서 최신/선행 회고와 실제 checkout/diff/코드/증거 대조 | [06-documentation.md:50](../guidelines/06-documentation.md#L50) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 55.2–3 | 과거 통과/승인≠현재 증거/권한·기록 없으면 현재 확인 기반, 과거 회고 임의 복원 금지 | [06-documentation.md:50](../guidelines/06-documentation.md#L50) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 56.1–2 | 차단/중단 재개 정보/구체 행동·개선은 제안, 자동 구현 범위 확대 금지 | [06-documentation.md:51](../guidelines/06-documentation.md#L51) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 60.1 | 일회성 출력은 작업별 임시 폴더 | [07-validation-evidence.md:47](../guidelines/07-validation-evidence.md#L47) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 60.2 | 필요 증거 처리·진행 실행/참조 없음 확인·이번 작업 소유 임시 파일만 제거 | [07-validation-evidence.md:47](../guidelines/07-validation-evidence.md#L47) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 60.3 | 재개 파일 위치·용도·제거 조건 | [07-validation-evidence.md:47](../guidelines/07-validation-evidence.md#L47) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 70.1 | 과거 원문 의미 보존: 별도 삭제 단계 승인 전까지 보존 | [07-validation-evidence.md:48](../guidelines/07-validation-evidence.md#L48) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |
| 70.2 | 역사 영역 이름 검사 구분·과거 절대 경로를 현재 예제로 복사 금지 | [07-validation-evidence.md:48](../guidelines/07-validation-evidence.md#L48) | 보존 / 승인된 증거 소유 조정 외 누락 없음 |


기계 대조: 06의6항목은 기준51–56행과 상대 링크·상태 접두사 정규화 후 동일하다. 07의 원문55–56행은 기준60·70행과 동일하다. 60.2의 validation/회고 선택은 **요약=회고 한 곳(D-E), 최소 기계 입력만 예외**로 조정했다. 70.1은 **삭제 단계 별도 승인 전까지 보존**으로 조정했다. 나머지 요건 누락0이다. 현재 요청은 임시 파일을 포함해 삭제하지 않는다. 두 가이드의 기존 본문은 prefix 일치, 추가만 수행했다.

AGENTS에는 완료/중단/차단 시 회고·경로/색인·D-E·확정/제안/미확정·실행/미실행 구분을 유지했다. 기준61–64행의 재사용 도구·Temp 의존 금지·절대 경로/소유권·GUID/.meta/사용자 자산·importer 생성 소스 보호는 그대로 남겼다.

## A3 · 영속 규칙과 규모

AGENTS는 **134행 → 127행(7행 감소)**이다. 이관 항목 분모는 기존 bullet21개(완료 track13 + 회고6 + 증거2)다. track의 영속/애매 규칙은 중복해서 역사에도 보존하므로 ‘이관21개’가 ‘규칙21개 삭제’를 의미하지 않는다. 애매한 규칙을 보수적으로 유지해 축소 폭보다 권한·검증 보존을 우선했다.

맨 위에 가이드 입구·[결정됨]만 규범/[제안] 미확정·T0~T3 절차 링크를 추가했다. 등급표를 복제하지 않았다. 역할·scope·dirty, C#/assembly/소유권, SOLID/TDD/실행0 구분, 무승인 패키지/재직렬화/이력 변경 금지, Git 시작 전 확인, exact-SHA CI, 사용자 수락, 승인 후 브랜치 정리, 배포 절은 유지했다. 목적/역할·외부 제공 문서·Git 시작 전·배포·정리의 기존 절 본문 일치를 검사했다. X 외 영속 보호 변경 없음, 가이드의 [제안] 상태 변경 없음.

## A4 · INDEX 정리 행

| 이전 → 현재 행 | 변경 |
|---|---|
| 27 → 28 | 백로그 범위 IMP-01~13 → IMP-01~14. 백로그 자체는 변경하지 않음. |
| 28/29 → 29/30 | IMP-04 조사/적용의 현재 완료 상태·기존 요약 링크, 수치/SHA 제거. |
| 30/31 → 31/32 | IMP-01/02 현재 완료 상태와 IMP-14 계측 정정 링크, 과거 GC 0바이트·수치/SHA 제거. |
| 32 → 33 | 초기 IMP-05 중단 조사를 역사로 표시하고 후속 구현 승인·완료 링크. |
| 33 → 34 | GC 가이드의 초안·UI 제외/IMP-12·13 별도 결정 유지, IMP-14 정정과 새 GC 결정 링크. |
| 43 → 45 | CI 초안 미반영 표현을 workflow·판정 도구 반영 완료로 정정. Unity 테스트 CI와 구분. |
| 47 → 49 | UI ‘main 통합 진행 중’ → 완료·기존 통합 요약 링크. |
| 135/137/139 → 137/139/141 | IMP-14/05/03 현재 상태 한 줄·기존 요약 링크, 실행 수치/SHA 제거. |
| 신규27/37 | guidelines 색인과 이번 회고 입구. |
| 회고 INDEX 신규151 | 이번07회고 상태·링크만 추가. 기존 행은 그대로. |

최근 대상의 숫자 IMP 번호·조사 단계·API 명칭은 상태 식별자이며 실행 수치가 아니다. 이전 모듈 행과 회고 색인의 과거 수치는 이번 범위 밖으로 남겼다. 이미 연결된 과거 요약을 새 회고로 다시 복제하지 않았다.

## 블록 X · 승인된 변경 전후 전체 문장

D-F는 모든 등급에서 사용자 병합 승인·병합 전 사용자 확인을 생략 불가로 정한다. 사용자17:12 승인으로 다음3개 bullet만 대체했다. 필요한 검사·대상 commit·실패/누락 해결·로컬/원격 일치·dirty·브랜치 정리 요건은 그대로 유지했다.

### 기준 119행 → 현재 112행

변경 전:

```markdown
- 2026-10-02 사용자는 TPLab에서 아래 조건을 충족하면 추가 확인 없이 main 병합·푸시까지 진행하도록 승인했다.
```

변경 후:

```markdown
- 모든 변경 등급의 main 병합은 사용자의 명시적 승인 뒤에만 진행한다. 2026-10-13 사용자 승인(블록 X)에 따라 2026-10-02의 추가 확인 없는 병합 승인을 대체하며, [D-F 변경 절차](doc/guidelines/08-change-procedure.md)의 병합 전 사용자 확인을 생략하지 않는다.
```

### 기준 120행 → 현재 113행

변경 전:

```markdown
- 변경 범위에 필요한 기존 자동 테스트·컴파일·CI 검사가 통과하고, 사용자가 직접 실행하여 확인해야 할 항목이 남아 있지 않으면 병합한다. 직접 확인이 필요한 항목은 구체적인 실행 방법과 기대 결과를 전달하고, 해당 확인이 완료될 때까지 병합을 보류한다.
```

변경 후:

```markdown
- 변경 범위에 필요한 기존 자동 테스트·컴파일·CI 검사가 통과하고, 사용자가 직접 실행하여 확인해야 할 항목이 모두 확인된 뒤 사용자의 병합 승인을 기다린다. 직접 확인이 필요한 항목은 구체적인 실행 방법과 기대 결과를 전달하고, 해당 확인과 병합 승인이 완료될 때까지 병합을 보류한다.
```

### 기준 121행 → 현재 114행

변경 전:

```markdown
- 문서 변경처럼 실행 테스트 대상이 없으면 그 이유를 기록하고 diff·공백·변경 범위 등 해당 변경에 필요한 검사를 통과한 뒤 병합한다. 테스트 0건이나 테스트 미실행을 테스트 통과로 기록하지 않는다.
```

변경 후:

```markdown
- 문서 변경처럼 실행 테스트 대상이 없으면 그 이유를 기록하고 diff·공백·변경 범위 등 해당 변경에 필요한 검사를 통과한 뒤 사용자의 명시적 병합 승인을 기다린다. 테스트 0건이나 테스트 미실행을 테스트 통과로 기록하지 않는다.
```

PR 본문에도 사용자 요구대로 이3개 bullet의 변경 전후 전문을 보여 준다. 이는 X 승인 검토를 위한 예외이며 검증 수치·source SHA 요약을 PR에 복제하는 허용으로 확대하지 않는다.

## 범위 밖 발견 사항 · 수정하지 않음

아래4묶음의 제안은 미확정이다. 해당 원본 파일과 역할 도입부는 그대로 두었다.

| 묶음 | 현재 위치·원문/문제 | 제안 문구 |
|---|---|---|
| 1 | [CLAUDE 검토 항목5:33](../../CLAUDE.md#L33): “XML 주석과 doc/api, doc/ai/api가 같은 PR에서 갱신됐는가.” | “변경 등급을 확인한다. T2/T3 계약 변경은 XML·사람/AI API 동시 갱신, T1은 계약 불변과 이동으로 어긋난 인용만 확인한다.” |
| 2 | [GAME_SCENE_MANAGER_DRAFT:162](../GAME_SCENE_MANAGER_DRAFT.md#L162): “사용자 승인 대기다.” | “2026-10-13 사용자 수락·PR #10 병합 완료. 당시 실행·NotRun은 기존 요약을 따른다.” |
| 2 | [CI_SETUP_GUIDE:3](../CI_SETUP_GUIDE.md#L3): “초안. 아직 저장소에 반영되지 않았다.” | “PR 규칙 검사·요청 시 Claude 리뷰와 CI 판정 도구는 main 반영 완료. 도입·첫 실행 절차는 당시 기록, Unity 테스트 CI는 별도다.” |
| 3 | [UI_ACCEPTANCE:3](../UI_ACCEPTANCE.md#L3): “Main integration is in progress and is not complete.” | “Main integration completed after user acceptance; see the integration record.” |
| 3 | [INPUT_SYSTEM_TRACK:47](../INPUT_SYSTEM_TRACK.md#L47): 통합 전 대기·병합/삭제 보류 운영 문장 | “통합 전 운영 기록”으로 날짜·시점을 표시하고 같은 문서43행의 완료 상태를 연결한다. |
| 3 | [SCENE_TRANSITION_ACCEPTANCE:33](../SCENE_TRANSITION_ACCEPTANCE.md#L33) 이후: 수락·통합 대기와 당시 전원 절차 | “수락·통합 전 운영 기록”으로 날짜를 표시하고 [track 완료 기록](../SCENE_TRANSITION_TRACK.md#L93)을 연결한다. |
| 4 | [AGENTS 역할:8](../../AGENTS.md#L8)(기준6행): “이 세션은 Unity 공용 코어 아키텍처 프로그래머다.” | “사용자가 설계·범위를 결정하고 Codex는 확정된 범위의 구현·검증·문서화를 수행한다. 독립 검토자는 결과와 증거를 별도로 검토한다. 설계 변경이 필요하면 사용자에게 차이·대안을 보고한다.” |

추가 발견: doc/INDEX의 기존74/123행(현재76/125)의 계약/기능 문서가 실행 설명을 소유한다는 문구는 이번 ‘최근 행’ 정리 밖이라 유지했다. 회고 색인에06가 아직 등록되지 않은 것도 이번07행 추가 범위 밖으로 남겼다. 06/07 앞부분의 기존 AGENTS 행 인용은 SourceRevision35508bc의 역사적 인용이며 새 행 번호로 바꾸지 않았다. 이 항목들은 현재 요청의 수정·삭제 권한으로 확대하지 않는다.

## 검증 증거 · 이 회고가 단일 요약 소유

승인된 문서8개 diff를 대상으로 로컬 정적 검사와 규칙 검사, 그 뒤 새 PR의 최종 exact-SHA Actions를 확인한다. 아래 로컬 정적 결과는 이번 단위 실제 실행이며, PR 규칙·최종 Actions는 실행 후 갱신한다. 원문 대조·링크 검사는 Python 표준 라이브러리로 git show 기준 blob·Path 읽기·SHA256 집계·Markdown의 실제 링크/heading을 검사했다. 원문 코드 블록/inline code는 렌더링 링크가 아니므로 제외한다.

| 검사 | 실행/근거 | 현재 결과 |
|---|---|---|
| 선행 PR12 gate·merge | tools/check_github_ci.py --repository KangSungKyu/TPLab --ref 8183d4cbb1fdce9a4ea324abf49e90d9d87913ae / get_pr_info·merge·git fetch·ff | completed/success·조회 완전성 확인; merge1ddc87d. 새 단위 gate와 별도. |
| 링크·heading·공백 | 문서8개의 fenced/inline 원문을 제외한 상대 파일·heading/#L 검사; git diff --check <base> -- <문서8개> | 상대 링크406개·heading/#L125개 오류0, 전체 링크414개; 승인 문서 diff 공백 exit0 |
| 이관·범위 보존 | git show 기준 blob 대조, 06/07·track·회고색인 prefix, A2 문장 비교, 제외 추적 파일과 dirty raw SHA256 | track13항목·회고6항목·증거2항목 일치; prefix5개·영속 절·제외2242파일·dirty6 raw hash 보존 |
| PR 규칙 | tools/check_pr_rules.py --base 1ddc87d4116e9151f1b82836d664655d83270ba1 --head HEAD | 실제 실행 대기. AGENTS 보호 파일 warning은 A1~A3/X 명시 승인에 따른 예상 항목이며 오류와 구분한다. |
| 최종 Actions | tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <최종 tip> --output Temp/docs-governance-20261013/final-ci.json | 새 PR·최종 SHA gate 대기. 조회 출력은 ignored Temp이며 커밋하지 않는다. |

보존 기준: 허용 기존7개 문서를 제외한 추적2242개 raw SHA256 aggregate `d6dc7abe76b00f384028cb9fc08ab7e3ccfe73f2d362a8df5591e51d2f5f87a2`. 기존 사용자 변경6개(추적5/미추적1)의 raw hash 기준도 확보했다. 코드/테스트/UPM/가이드 제안/기존 validation을 수정·삭제하지 않았음을 이 기준과 최종 diff로 확인한다. 이번 작업과 과거 작업의 Temp 출력은 삭제 금지에 따라 보존한다; 경로는 실행 명령에 기록하며 영속 문서가 Temp 스크립트에 의존하지 않는다.

전체 작업 트리의 공백 검사는 기존 사용자 InitScene 변경의 trailing whitespace3개로 exit2였다. 사용자 자산을 수정하지 않고 승인 문서8개만 검사하며 새 회고는 별도 행 공백 검사에 포함한다.

NotRun: Unity EditMode·PlayMode·컴파일·제품 Console·Player/IL2CPP·새 시각 UX·독립 Claude workflow. T0 문서만 변경해 Unity 실행 대상이 없고 실행0은 테스트 통과가 아니다. 이 단위의 main 병합·강제 푸시·브랜치 삭제·Unity 종료·PC 제어도 수행하지 않는다.

## 배운 점·남은 행동

세부 운영을 이관할 때 문단 단위 삭제만 하면 UI source·대규모 검증 같은 영속 요건이 빠질 수 있다. 문장별 판정과 기준 blob 대조로 보완했고, 증거 소유 변경은 원문과 현재 승인 규칙을 함께 기록해 역사 왜곡을 피했다. 추가 발견은 제안으로만 남기고 실행 범위를 늘리지 않았다.

다음 행동은 새 PR의 사용자 diff 검토와 명시적 병합 승인이다. 그전에는 main1ddc87d와 두 작업 브랜치를 유지한다. 원시 자료 삭제·CLAUDE/상태 문구/역할 변경·누락된 과거 색인 정리는 별도 요청과 범위 결정이 필요하다.
