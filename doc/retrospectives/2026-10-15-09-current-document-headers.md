# D-I3 현행 문서 공통 머리말 적용

위험 등급: **T0 · 문서 전용**. 사용자 2026-10-15 후속 보완을 같은 `codex/docs-current-headers` 브랜치와 PR #33에 반영한다. 새 PR·회고 파일은 만들지 않고 이 회고와 기존 INDEX 행을 갱신한다. 상태·검증 사실을 새로 판단하지 않으며 머리말과 인용 대상, 명시적으로 승인한 D-I3 적용 규칙만 수정한다.

## 작업 기준과 이전 단계

PR #32는 사용자 승인 HEAD `876ce9aa331a5ab4bc0fa2e9f7f3def170220749`의 Actions 성공과 당시 main `af084e0d0027c4098fad08c336ccb0753e1c7862`를 확인한 뒤 일반 merge했다. 병합 SHA와 PR #33의 base는 `d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f`다. PR #32 브랜치는 tip 도달 가능성·worktree 사용·원격 tip 일치를 확인한 뒤 정리했다.

PR #33의 최초 적용은 31개 적용·24개 제외·1개 보류였다. 문서 source `45b82a988be73c41f90d7e59a07150cd98304b1e`와 요약 추가 HEAD `2904394e1399bcbabdd647668a449c9f176b2540`의 Actions는 성공했다. [이전 HEAD Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38700317088)는 그 단계의 기록이며 이번 보완의 검증을 대신하지 않는다.

이번 시작 시 PR #33은 open·merged=false, HEAD는 `2904394e1399bcbabdd647668a449c9f176b2540`였다. 로컬/원격 main은 위 base와 같고 main이 branch의 조상이며 2커밋 앞선 상태였다. 같은 작업이므로 새 브랜치를 만들지 않았다. 기존 dirty10개(추적7·미추적3)를 raw SHA256으로 보존한다. main 병합은 사용자 승인 전 진행하지 않는다.

## 최종 분류표

분모는 `doc/*.md`, `doc/api/*.md`, `doc/ai/*.md`, `doc/ai/api/*.md`의 56개다. **적용44 / 제외12 / 보류0**이다. 이전에 상대 줄 인용 때문에 제외한12개와 보류한 CI 판정 문서를 추가했다. 제외12개는 이미 날짜가 있는 역사적 기록이며 머리말을 추가하지 않았다. 회고·validation은 이56개 밖이다.

| 문서 | 최종 분류 | 유형 | 근거 |
|---|---|---|---|
| [doc/ASYNC_SCENE_LIFECYCLE.md](../ASYNC_SCENE_LIFECYCLE.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/BOOTSTRAP_SYSTEM.md](../BOOTSTRAP_SYSTEM.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/CI_GATE_PROPOSAL.md](../CI_GATE_PROPOSAL.md) | 적용 | 가이드 | 사용자 지정 가이드; AGENTS의 현행 CI 판정 기준 연결 확인 |
| [doc/CI_SETUP_GUIDE.md](../CI_SETUP_GUIDE.md) | 적용 | 가이드 | 줄 인용 보정 후 추가 적용 |
| [doc/CORE_PLAN.md](../CORE_PLAN.md) | 적용 | 결정 기록 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DATA_TABLE_GENERIC_IMPLEMENTATION.md](../DATA_TABLE_GENERIC_IMPLEMENTATION.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/DATA_TABLE_IDX_DRAFT.md](../DATA_TABLE_IDX_DRAFT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DATA_TABLE_IMPORTER_DRAFT.md](../DATA_TABLE_IMPORTER_DRAFT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DATA_TABLE_MANAGER.md](../DATA_TABLE_MANAGER.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DATA_TABLE_MAPPING_DRAFT.md](../DATA_TABLE_MAPPING_DRAFT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DISTRIBUTION_ACCEPTANCE.md](../DISTRIBUTION_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/DISTRIBUTION_PIPELINE.md](../DISTRIBUTION_PIPELINE.md) | 적용 | 가이드 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/DOCUMENTATION_GUIDE.md](../DOCUMENTATION_GUIDE.md) | 적용 | 가이드 | 줄 인용 보정 후 추가 적용 |
| [doc/GAME_SCENE_MANAGER_DRAFT.md](../GAME_SCENE_MANAGER_DRAFT.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/GAME_UI_SYSTEM.md](../GAME_UI_SYSTEM.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/GAME_UI_SYSTEM_DRAFT.md](../GAME_UI_SYSTEM_DRAFT.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/GAME_UI_SYSTEM_TRACK.md](../GAME_UI_SYSTEM_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/GC_ALLOCATION_GUIDELINE.md](../GC_ALLOCATION_GUIDELINE.md) | 적용 | 가이드 | 줄 인용 보정 후 추가 적용 |
| [doc/GENERIC_POOL.md](../GENERIC_POOL.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/IMPROVEMENT_BACKLOG.md](../IMPROVEMENT_BACKLOG.md) | 적용 | 결정 기록 | 줄 인용 보정 후 추가 적용 |
| [doc/INDEX.md](../INDEX.md) | 적용 | 레퍼런스 | 현행 문서 탐색 색인; 새 검증 상태를 만들지 않고 본문 참조 |
| [doc/INPUT_SYSTEM_ACCEPTANCE.md](../INPUT_SYSTEM_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/INPUT_SYSTEM_DRAFT.md](../INPUT_SYSTEM_DRAFT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/INPUT_SYSTEM_TRACK.md](../INPUT_SYSTEM_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/OBJECT_POOL.md](../OBJECT_POOL.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/RESOURCE_MANAGER.md](../RESOURCE_MANAGER.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/SCENE_LOADING.md](../SCENE_LOADING.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/SCENE_LOADING_ACCEPTANCE.md](../SCENE_LOADING_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/SCENE_LOADING_PRESENTATION_DRAFT.md](../SCENE_LOADING_PRESENTATION_DRAFT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/SCENE_LOADING_TRACK.md](../SCENE_LOADING_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/SCENE_ROOT.md](../SCENE_ROOT.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/SCENE_TRANSITION_ACCEPTANCE.md](../SCENE_TRANSITION_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/SCENE_TRANSITION_TRACK.md](../SCENE_TRANSITION_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/SINGLETON.md](../SINGLETON.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/TPLAB_NAMING.md](../TPLAB_NAMING.md) | 적용 | 가이드 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/UI_ACCEPTANCE.md](../UI_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/UI_PERFORMANCE.md](../UI_PERFORMANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록; 원문 보존 |
| [doc/ai/README.md](../ai/README.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/ai/api/DataTables.md](../ai/api/DataTables.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/Editor.md](../ai/api/Editor.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/Input.md](../ai/api/Input.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/Lifecycle.md](../ai/api/Lifecycle.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/Pooling.md](../ai/api/Pooling.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/README.md](../ai/api/README.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/Resources.md](../ai/api/Resources.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/SceneManagement.md](../ai/api/SceneManagement.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/ai/api/UI.md](../ai/api/UI.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/api/DataTables.md](../api/DataTables.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/api/Editor.md](../api/Editor.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/api/Input.md](../api/Input.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/api/Lifecycle.md](../api/Lifecycle.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/api/Pooling.md](../api/Pooling.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/api/README.md](../api/README.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/api/Resources.md](../api/Resources.md) | 적용 | 레퍼런스 | 기존 적용 유지; 본문 근거와 계약 분류 재점검 |
| [doc/api/SceneManagement.md](../api/SceneManagement.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |
| [doc/api/UI.md](../api/UI.md) | 적용 | 레퍼런스 | 줄 인용 보정 후 추가 적용 |

보호 경로 `AGENTS.md`, `CLAUDE.md`, `.github`, `tools`에서 이전 제외12문서를 상대 `.md#L숫자`로 가리키는 참조는0이다. 이 경로들은 수정하지 않았다. CI_GATE_PROPOSAL은 본문 제안 이력과 현행 판정 안내가 혼재하지만 사용자 지정 유형=가이드로 적용한다. 현재 상태 문구는 AGENTS의 “CI 판정 기준” 연결 근거만 사용했다. 본문의 “Main keeps its current behavior until this PR is explicitly accepted.”와 제안 이력 문장은 그대로이며, 본문 정리는 이번 범위가 아니다.

## 값 산정 규칙

1. 제목 바로 다음에 유형·현재 상태·기준 source·갱신일·계약 링크의 5줄만 둔다. 유형은 가이드/레퍼런스/결정 기록 중 하나이며 본문의 역할을 따른다.
2. 현재 상태는 기존 본문 상태 문장 일부 또는 해당 소제목 앵커다. 본문에 상태가 없으면 “본문 참조”로 둔다. 머리말 어디에도 줄 번호 링크를 쓰지 않는다. CI 판정 문서만 사용자가 허용한 AGENTS 연결 근거를 쓴다.
3. 기준 source는 본문에 이미 명시된 SHA/tag만 가져온다. 없으면 “미표기(본문 근거 링크 참조)”다. 이번 base SHA를 새 검증 source로 쓰지 않는다.
4. 갱신일은 PR #33 이전 main까지의 마지막 실제 내용 변경일이다. `git log --follow --format=COMMIT:%H:%cs --name-status -M <base> -- <문서>`로 rename을 따라가며 각 commit/첫 parent의 blob을 비교한다. 머리말5줄과 Markdown 링크 목적지만 정규화하고, 표시 문구의 줄 번호 접미사만 정규화해 머리말·링크 보정·rename뿐인 커밋은 건너뛴다. 나머지 본문이 처음 달라지는 commit을 고르고 `git show -s --format=%cs <commit>`의 날짜를 쓴다. 아래 표에 선택 commit을 남긴다. 이번 PR의 머리말/링크 보정 커밋은 계산에서 제외한다.
5. 계약 링크는 (a) 문서 자체의 계약이면 “본 문서”, (b) 레퍼런스가 계약 원문으로 명시한 다른 문서·코드면 그 링크, (c) 설정·설치·관련 문서 안내뿐이면 “본문 참조”다. (b)는 본문에 같은 대상이 실제 존재하고 파일이 실존하는지 검사했다.

현재 상태 “본문 참조”는9개, 기준 source 미표기는15개다. 계약 분류는 (a)22개·(b)15개·(c)7개다. 근거가 없는 상태/source를 채우지 않았다. 기존31개 중 갱신일10개는 이전의 마지막 touch 날짜에서 링크 보정 커밋을 제외한 내용 변경일로 정정했다. 예를 들어 CI_GATE는 2026-10-15 이동과 2026-10-14 링크 보정을 제외하고 2026-10-12 내용 변경을 선택했다.

## 문서별 머리말 값과 근거 — 전수44개

표의 링크 문법은 각 대상 문서 기준 값을 그대로 인용한 일반 코드 텍스트다. 실제 탐색 링크는 위 분류표를 사용한다. 날짜 괄호의 SHA는 내용 변경일 근거이며 기준 source와 별개다.

| 문서 | 유형 | 현재 상태 | 기준 source | 내용 갱신일·근거 commit | 계약 분류·값 | 본문 근거 한 줄 |
|---|---|---|---|---|---|---|
| `doc/ASYNC_SCENE_LIFECYCLE.md` | 레퍼런스 | `GameSceneManager의 최초 Single/Additive 진입·연속 교체·중첩 구역·조건은 구현되어 있으며 [현재 API](api/SceneManagement.md)를 따른다.` | `미표기(본문 근거 링크 참조)` | 2026-10-13 (`2cf57533b4773248b04a53a212979d70ca98c3c1`) | a: `본 문서` | `GameSceneManager의 최초 Single/Additive 진입·연속 교체·중첩 구역·조건은 구현되어 있으며 [현재 API](api/SceneManagement.md)를 따른다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/BOOTSTRAP_SYSTEM.md` | 레퍼런스 | `최초 진입·주 씬 교체·파생 구역·전환 정의/조건 runtime의 TPLab 내부 자동 검증 기록이 있다.` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | `최초 진입·주 씬 교체·파생 구역·전환 정의/조건 runtime의 TPLab 내부 자동 검증 기록이 있다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/CI_GATE_PROPOSAL.md` | 가이드 | `[현행 CI 판정 기준](../AGENTS.md#검증-후-병합)` | `미표기(본문 근거 링크 참조)` | 2026-10-12 (`b68bf1f0eaaa3ef777464e5ee996c5aec7ac9203`) | a: `본 문서` | `CI 판정 기준`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/CI_SETUP_GUIDE.md` | 가이드 | `저장소 반영 완료.` | `미표기(본문 근거 링크 참조)` | 2026-10-15 (`91524bb3412e27526996265d6fceb753350de472`) | a: `본 문서` | `저장소 반영 완료.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/CORE_PLAN.md` | 결정 기록 | `사용자 지정 목표·작업 원칙은 확정이다.` | `미표기(본문 근거 링크 참조)` | 2026-10-15 (`1626450c9940518cc41473aaeac48d1982aee7f2`) | c: `본문 참조` | `사용자 지정 목표·작업 원칙은 확정이다.`; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/DATA_TABLE_IDX_DRAFT.md` | 레퍼런스 | `아래 API·인코딩·오류 정책은 이번 구현의 계약이다.` | `TPLab main / a7804a8; Cashier total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | `아래 API·인코딩·오류 정책은 이번 구현의 계약이다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_IMPORTER_DRAFT.md` | 레퍼런스 | `상태: CSV 1차 구현·TPLab 검증.` | `main/19b5770` | 2026-10-08 (`49278a169e03bbfe56279fc30b00a89f576e5a4c`) | a: `본 문서` | `상태: CSV 1차 구현·TPLab 검증.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_MANAGER.md` | 레퍼런스 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | 본문에 명시적 상태 문장 없음; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_MAPPING_DRAFT.md` | 레퍼런스 | `2026-10-06 generic 범위 구현 완료.` | `TPLab main / b8c2c91; Cashier total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | `2026-10-06 generic 범위 구현 완료.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DISTRIBUTION_PIPELINE.md` | 가이드 | `P4 / 0.0.1 정식 공개·발행 검증 완료` | `de879b9396ddae0523bd3ab86939b679b383dd92 (v0.0.1)` | 2026-10-08 (`8ce768d96f79ed8328143bebc31e77f060f51205`) | a: `본 문서` | `P4 / 0.0.1 정식 공개·발행 검증 완료`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DOCUMENTATION_GUIDE.md` | 가이드 | `이 문서는 **작성 지침**이다.` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`58fc7a15ae772b08afb8d6e450a2a3d0065480c7`) | a: `본 문서` | `이 문서는 **작성 지침**이다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/GAME_SCENE_MANAGER_DRAFT.md` | 레퍼런스 | `최초 진입·Bootstrap 위임·명시적 로더·주 씬 교체·파생 구역 수명 tree·전환 정의·root 조건·Editor 사전 검사를 구현했다.` | `0cc919b6c497f348deae3e6ae4dffb33f21c8482 (IMP-05 source)` | 2026-10-15 (`91524bb3412e27526996265d6fceb753350de472`) | a: `본 문서` | `최초 진입·Bootstrap 위임·명시적 로더·주 씬 교체·파생 구역 수명 tree·전환 정의·root 조건·Editor 사전 검사를 구현했다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/GAME_UI_SYSTEM.md` | 레퍼런스 | `[본문 상태](#p7-source별-증거-및-현재-통합-상태)` | `21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (final regression snapshot)` | 2026-10-15 (`1626450c9940518cc41473aaeac48d1982aee7f2`) | a: `본 문서` | `P7 source별 증거 및 현재 통합 상태`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/GC_ALLOCATION_GUIDELINE.md` | 가이드 | `초안. 사용자 검토 전이다.` | `de879b9 (v0.0.1); c1804e17 (PR #4 대조)` | 2026-10-12 (`c30a1e9e158bc60aa640e19e3f04314ce2e0e0b0`) | a: `본 문서` | `초안. 사용자 검토 전이다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/GENERIC_POOL.md` | 레퍼런스 | `[본문 상태](#지연-생존-확인-imp-01--d-01a1)` | `901d56c582691d55f4aa9698e91be57231328635 (IMP-01 source)` | 2026-10-13 (`e8f45411c5efc9227d7047e564e6976e6db145ef`) | a: `본 문서` | `지연 생존 확인 (IMP-01 / D-01=A1)`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/IMPROVEMENT_BACKLOG.md` | 결정 기록 | `[본문 상태](#상태-요약)` | `de879b9396ddae0523bd3ab86939b679b383dd92 (v0.0.1)` | 2026-10-13 (`f89fe70115d46321cd4a229a2b91a28fc380d708`) | c: `본문 참조` | `상태 요약`; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/INDEX.md` | 레퍼런스 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-15 (`50cfbbbb7f1e5e72cfb7fb3c4284bfbacb210b12`) | c: `본문 참조` | 본문에 명시적 상태 문장 없음; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/INPUT_SYSTEM_DRAFT.md` | 레퍼런스 | `아래 API·기본 동작을 구현했다.` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | `아래 API·기본 동작을 구현했다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/OBJECT_POOL.md` | 레퍼런스 | `[본문 상태](#외부-파괴와-지연-카운트-imp-01)` | `901d56c582691d55f4aa9698e91be57231328635 (IMP-01 source)` | 2026-10-13 (`e8f45411c5efc9227d7047e564e6976e6db145ef`) | a: `본 문서` | `외부 파괴와 지연 카운트 (IMP-01)`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/RESOURCE_MANAGER.md` | 레퍼런스 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | a: `본 문서` | 본문에 명시적 상태 문장 없음; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_LOADING.md` | 레퍼런스 | `작업 트랙 P0 확정 구현 계약` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`930ddeccc35d3fe5ecb8e4c81b91a20f70fca356`) | a: `본 문서` | `작업 트랙 P0 확정 구현 계약`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_LOADING_PRESENTATION_DRAFT.md` | 레퍼런스 | `progress snapshot, 선택적 UI callback 흐름, 자동/수동 진행 대기와 프로젝트 소유 sample UI가 구현됐다.` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`930ddeccc35d3fe5ecb8e4c81b91a20f70fca356`) | a: `본 문서` | `progress snapshot, 선택적 UI callback 흐름, 자동/수동 진행 대기와 프로젝트 소유 sample UI가 구현됐다.`; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_ROOT.md` | 레퍼런스 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-13 (`2cf57533b4773248b04a53a212979d70ca98c3c1`) | a: `본 문서` | 본문에 명시적 상태 문장 없음; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SINGLETON.md` | 레퍼런스 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`23907324d0cf7df97c0d4a3f6d354959fab667cf`) | a: `본 문서` | 본문에 명시적 상태 문장 없음; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/TPLAB_NAMING.md` | 가이드 | `본문 참조` | `미표기(본문 근거 링크 참조)` | 2026-10-07 (`23907324d0cf7df97c0d4a3f6d354959fab667cf`) | a: `본 문서` | 본문에 명시적 상태 문장 없음; 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/ai/README.md` | 레퍼런스 | `ImplementationStatus: Existing Core/Input/Editor modules Implemented.` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0 (original seven modules); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 UI runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (P7 regression snapshot); de879b9396ddae0523bd3ab86939b679b383dd92 (InstallationSourceRevision)` | 2026-10-15 (`626716da37f0a2e5a8294c72a57adf4ef9e8212b`) | c: `본문 참조` | `ImplementationStatus: Existing Core/Input/Editor modules Implemented.`; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/ai/api/DataTables.md` | 레퍼런스 | `ImplementationStatus: Implemented` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../../DATA_TABLE_MANAGER.md)` | `ImplementationStatus: Implemented`; HumanContract 상세 계약 |
| `doc/ai/api/Editor.md` | 레퍼런스 | `ImplementationStatus: Implemented` | `32a74d5a5f201bd582a8cbe5ff9d4f40dc5298ed` | 2026-10-08 (`4c7b3076f99eb16dec1f77d435975a3ba90064fc`) | b: `[계약 원문](../../api/Editor.md)` | `ImplementationStatus: Implemented`; HumanContract 사람용 API |
| `doc/ai/api/Input.md` | 레퍼런스 | `ImplementationStatus: Implemented` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../../INPUT_SYSTEM_DRAFT.md)` | `ImplementationStatus: Implemented`; HumanContract 상세 계약 |
| `doc/ai/api/Lifecycle.md` | 레퍼런스 | `Implemented / Partial` | `5fe5bf5a41386409775d836e5b3a50a2f516a787` | 2026-10-13 (`f89fe70115d46321cd4a229a2b91a28fc380d708`) | b: `[계약 원문](../../api/Lifecycle.md)` | `Implemented / Partial`; HumanContract 사람용 API |
| `doc/ai/api/Pooling.md` | 레퍼런스 | `[본문 상태](#editor-계측-한계에-의한-정정-2026-10-13)` | `2c8c0768c53878dcc9c2d4ed090582092e9dc5e0` | 2026-10-13 (`e8f45411c5efc9227d7047e564e6976e6db145ef`) | b: `[계약 원문](../../api/Pooling.md)` | `Editor 계측 한계에 의한 정정 (2026-10-13)`; HumanContract Pooling.md |
| `doc/ai/api/README.md` | 레퍼런스 | `본문 참조` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0 (original seven modules); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 UI runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (P7 regression snapshot)` | 2026-10-08 (`e4a3df6b2bf472f36c09991bf9073b4f727a1bcc`) | c: `본문 참조` | 본문에 명시적 상태 문장 없음; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/ai/api/Resources.md` | 레퍼런스 | `Implemented / Partial.` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs)` | `Implemented / Partial.`; SourcePath 코드 파일 |
| `doc/ai/api/SceneManagement.md` | 레퍼런스 | `Implemented / Partial` | `0cc919b6c497f348deae3e6ae4dffb33f21c8482` | 2026-10-13 (`56c101fb282e7f2723ad6a06a253a741ccf47c4e`) | b: `[계약 원문](../../GAME_SCENE_MANAGER_DRAFT.md)` | `Implemented / Partial`; HumanContract manager |
| `doc/ai/api/UI.md` | 레퍼런스 | `[본문 상태](#implementationstatus--validationstatus--evidence)` | `99df25dd6b89fd7f3e322c8033b61eb1364f495e (UI regression); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 runtime)` | 2026-10-15 (`626716da37f0a2e5a8294c72a57adf4ef9e8212b`) | b: `[계약 원문](../../GAME_UI_SYSTEM.md)` | `ImplementationStatus / ValidationStatus / Evidence`; UIContext design 계약 원문 |
| `doc/api/DataTables.md` | 레퍼런스 | `ImplementationStatus: Implemented; ValidationStatus: Partial.` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../DATA_TABLE_MANAGER.md)` | `ImplementationStatus: Implemented; ValidationStatus: Partial.`; 기본 로딩 계약 |
| `doc/api/Editor.md` | 레퍼런스 | `ImplementationStatus: Implemented; ValidationStatus: Partial.` | `32a74d5a5f201bd582a8cbe5ff9d4f40dc5298ed` | 2026-10-08 (`4c7b3076f99eb16dec1f77d435975a3ba90064fc`) | c: `본문 참조` | `ImplementationStatus: Implemented; ValidationStatus: Partial.`; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/api/Input.md` | 레퍼런스 | `ImplementationStatus: Implemented; ValidationStatus: Partial.` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../INPUT_SYSTEM_DRAFT.md)` | `ImplementationStatus: Implemented; ValidationStatus: Partial.`; 상세 계약 |
| `doc/api/Lifecycle.md` | 레퍼런스 | `Implemented / Partial` | `5fe5bf5a41386409775d836e5b3a50a2f516a787` | 2026-10-13 (`f89fe70115d46321cd4a229a2b91a28fc380d708`) | b: `[계약 원문](../SCENE_ROOT.md)` | `Implemented / Partial`; 계약 원문 root |
| `doc/api/Pooling.md` | 레퍼런스 | `본문 참조` | `2c8c0768c53878dcc9c2d4ed090582092e9dc5e0` | 2026-10-13 (`e8f45411c5efc9227d7047e564e6976e6db145ef`) | b: `[계약 원문](../../Assets/TPLab/Core/Pooling/ObjectPool.cs)` | 본문에 명시적 상태 문장 없음; SourcePath 코드 파일 |
| `doc/api/README.md` | 레퍼런스 | `본문 참조` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0 (original seven modules); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 UI runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (P7 regression snapshot)` | 2026-10-08 (`e4a3df6b2bf472f36c09991bf9073b4f727a1bcc`) | c: `본문 참조` | 본문에 명시적 상태 문장 없음; 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/api/Resources.md` | 레퍼런스 | `Implemented` | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` | 2026-10-07 (`9ae5c15355132b80384541e9371b53e109439805`) | b: `[계약 원문](../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs)` | `Implemented`; SourcePath 코드 파일 |
| `doc/api/SceneManagement.md` | 레퍼런스 | `Implemented / Partial` | `0cc919b6c497f348deae3e6ae4dffb33f21c8482` | 2026-10-13 (`56c101fb282e7f2723ad6a06a253a741ccf47c4e`) | b: `[계약 원문](../GAME_SCENE_MANAGER_DRAFT.md)` | `Implemented / Partial`; 계약 원문 씬 관리 |
| `doc/api/UI.md` | 레퍼런스 | `ImplementationStatus: Implemented through P6.` | `99df25dd6b89fd7f3e322c8033b61eb1364f495e (final regression inputs); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 runtime)` | 2026-10-15 (`626716da37f0a2e5a8294c72a57adf4ef9e8212b`) | b: `[계약 원문](../GAME_UI_SYSTEM.md)` | `ImplementationStatus: Implemented through P6.`; P4 UI/input contract |

## 기존31개 계약 링크 재판정 변경 목록

기존31개 중22개를 바꿨고9개는 같은 계약 링크를 유지했다. 새13개의 판단은 전수 표에 포함한다. RESOURCE_MANAGER의 SCENE_ROOT 링크는 설정 안내이므로 원문 계약 링크로 쓰지 않고 이 문서 자신의 계약으로 판정했다.

| 문서 | 변경 전 | 변경 후 | 판정 근거 |
|---|---|---|---|
| `doc/ASYNC_SCENE_LIFECYCLE.md` | `[계약 원문](api/SceneManagement.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/BOOTSTRAP_SYSTEM.md` | `[계약 원문](GAME_SCENE_MANAGER_DRAFT.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/CORE_PLAN.md` | `[계약 원문](GENERIC_POOL.md)` | c: `본문 참조` | 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/DATA_TABLE_IDX_DRAFT.md` | `[계약 원문](DATA_TABLE_MAPPING_DRAFT.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_IMPORTER_DRAFT.md` | `[계약 원문](DATA_TABLE_MANAGER.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_MANAGER.md` | `[계약 원문](DATA_TABLE_MAPPING_DRAFT.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DATA_TABLE_MAPPING_DRAFT.md` | `[계약 원문](DATA_TABLE_MANAGER.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/DISTRIBUTION_PIPELINE.md` | `본문 참조` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/GENERIC_POOL.md` | `본문 참조` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/INDEX.md` | `[계약 원문](api/README.md)` | c: `본문 참조` | 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/INPUT_SYSTEM_DRAFT.md` | `[계약 원문](api/Input.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/OBJECT_POOL.md` | `[계약 원문](GENERIC_POOL.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/RESOURCE_MANAGER.md` | `[계약 원문](SCENE_ROOT.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_LOADING.md` | `본문 참조` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_LOADING_PRESENTATION_DRAFT.md` | `[계약 원문](api/SceneManagement.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SCENE_ROOT.md` | `[계약 원문](ASYNC_SCENE_LIFECYCLE.md)` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/SINGLETON.md` | `본문 참조` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/TPLAB_NAMING.md` | `본문 참조` | a: `본 문서` | 이 문서가 해당 계약·판단 기준을 직접 정의한다. |
| `doc/ai/api/Editor.md` | `[계약 원문](../../DATA_TABLE_IMPORTER_DRAFT.md)` | b: `[계약 원문](../../api/Editor.md)` | HumanContract 사람용 API |
| `doc/ai/api/README.md` | `[계약 원문](UI.md)` | c: `본문 참조` | 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/api/Editor.md` | `[계약 원문](../DATA_TABLE_IMPORTER_DRAFT.md)` | c: `본문 참조` | 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |
| `doc/api/README.md` | `[계약 원문](UI.md)` | c: `본문 참조` | 색인·결정 모음 또는 설정/관련 문서 안내이며 단일 계약 원문을 명시하지 않는다. |

## 줄 번호 링크 전후

분모는 추적된 `doc/` Markdown의 현재 checkout 상대 `.md` 목적지(본문 링크·참조 정의·self link 포함)다. fence·인라인 코드의 원문 인용과 원격 URI/fixed-SHA permalink는 제외한다. **변경 전171 → 변경 후0**: 본문148개(소제목133·링크 제거 후 원래 표시 텍스트 유지15)와 기존 머리말23개다. 보호 경로의 참조0, 남은 상대 줄 번호 링크0이다. 과거 회고는 목적지/링크 문법만 바꾸고 표시 문장·수치·인라인 코드 원문은 그대로 두었다. 아래의 전/후 URL은 코드 인용이며 새 상대 링크가 아니다.

### 기존 머리말23개

| 문서 | 이전 목적지 | 새 현재 상태 값 |
|---|---|---|
| `doc/ASYNC_SCENE_LIFECYCLE.md` | `#L8` | `GameSceneManager의 최초 Single/Additive 진입·연속 교체·중첩 구역·조건은 구현되어 있으며 [현재 API](api/SceneManagement.md)를 따른다.` |
| `doc/BOOTSTRAP_SYSTEM.md` | `#L8` | `최초 진입·주 씬 교체·파생 구역·전환 정의/조건 runtime의 TPLab 내부 자동 검증 기록이 있다.` |
| `doc/CORE_PLAN.md` | `#L8` | `사용자 지정 목표·작업 원칙은 확정이다.` |
| `doc/DATA_TABLE_IDX_DRAFT.md` | `#L8` | `아래 API·인코딩·오류 정책은 이번 구현의 계약이다.` |
| `doc/DATA_TABLE_IMPORTER_DRAFT.md` | `#L8` | `상태: CSV 1차 구현·TPLab 검증.` |
| `doc/DATA_TABLE_MAPPING_DRAFT.md` | `#L8` | `2026-10-06 generic 범위 구현 완료.` |
| `doc/DISTRIBUTION_PIPELINE.md` | `#L8` | `P4 / 0.0.1 정식 공개·발행 검증 완료` |
| `doc/GENERIC_POOL.md` | `#L77` | `[본문 상태](#지연-생존-확인-imp-01--d-01a1)` |
| `doc/INPUT_SYSTEM_DRAFT.md` | `#L8` | `아래 API·기본 동작을 구현했다.` |
| `doc/OBJECT_POOL.md` | `#L72` | `[본문 상태](#외부-파괴와-지연-카운트-imp-01)` |
| `doc/SCENE_LOADING.md` | `#L8` | `작업 트랙 P0 확정 구현 계약` |
| `doc/SCENE_LOADING_PRESENTATION_DRAFT.md` | `#L8` | `progress snapshot, 선택적 UI callback 흐름, 자동/수동 진행 대기와 프로젝트 소유 sample UI가 구현됐다.` |
| `doc/ai/api/DataTables.md` | `#L14` | `ImplementationStatus: Implemented` |
| `doc/ai/api/Editor.md` | `#L14` | `ImplementationStatus: Implemented` |
| `doc/ai/api/Input.md` | `#L14` | `ImplementationStatus: Implemented` |
| `doc/ai/api/Lifecycle.md` | `#L14` | `Implemented / Partial` |
| `doc/ai/api/Pooling.md` | `#L16` | `[본문 상태](#editor-계측-한계에-의한-정정-2026-10-13)` |
| `doc/ai/api/Resources.md` | `#L12` | `Implemented / Partial.` |
| `doc/ai/api/SceneManagement.md` | `#L14` | `Implemented / Partial` |
| `doc/api/DataTables.md` | `#L12` | `ImplementationStatus: Implemented; ValidationStatus: Partial.` |
| `doc/api/Editor.md` | `#L12` | `ImplementationStatus: Implemented; ValidationStatus: Partial.` |
| `doc/api/Input.md` | `#L12` | `ImplementationStatus: Implemented; ValidationStatus: Partial.` |
| `doc/api/Resources.md` | `#L16` | `Implemented` |

### 본문 링크148개 — 시작 HEAD의 위치

| 참조 위치 | 대상 | 이전 | 이후 |
|---|---|---|---|
| `doc/guidelines/01-code-conventions.md:7` | `AGENTS.md` | `../../AGENTS.md#L16` | `../../AGENTS.md#c과-unity-기준` |
| `doc/guidelines/02-public-api.md:7` | `doc/api/Pooling.md` | `../api/Pooling.md#L51` | `../api/Pooling.md#objectpoolt` |
| `doc/guidelines/02-public-api.md:8` | `doc/api/Lifecycle.md` | `../api/Lifecycle.md#L100` | `../api/Lifecycle.md#종료-시간-제한` |
| `doc/guidelines/02-public-api.md:10` | `doc/api/Lifecycle.md` | `../api/Lifecycle.md#L100` | `../api/Lifecycle.md#종료-시간-제한` |
| `doc/guidelines/02-public-api.md:23` | `doc/api/SceneManagement.md` | `../api/SceneManagement.md#L350` | `../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot` |
| `doc/guidelines/02-public-api.md:24` | `doc/api/SceneManagement.md` | `../api/SceneManagement.md#L360` | `../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot` |
| `doc/guidelines/02-public-api.md:24` | `doc/api/SceneManagement.md` | `../api/SceneManagement.md#L362` | `../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot` |
| `doc/guidelines/02-public-api.md:25` | `doc/api/SceneManagement.md` | `../api/SceneManagement.md#L364` | `../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot` |
| `doc/guidelines/02-public-api.md:25` | `doc/api/SceneManagement.md` | `../api/SceneManagement.md#L368` | `../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot` |
| `doc/guidelines/02-public-api.md:32` | `AGENTS.md` | `../../AGENTS.md#L19` | `../../AGENTS.md#c과-unity-기준` |
| `doc/guidelines/02-public-api.md:32` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L18` | `../DOCUMENTATION_GUIDE.md#공통-사실과-상태` |
| `doc/guidelines/02-public-api.md:33` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L73` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/04-allocation-gc.md:17` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L293` | `../IMPROVEMENT_BACKLOG.md#imp-11-할당-감사-후속-전환이벤트-경로-p1` |
| `doc/guidelines/04-allocation-gc.md:17` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L333` | `../IMPROVEMENT_BACKLOG.md#imp-13-ui-구조할당테스트-점검` |
| `doc/guidelines/04-allocation-gc.md:23` | `doc/GC_ALLOCATION_GUIDELINE.md` | `../GC_ALLOCATION_GUIDELINE.md#L72` | `../GC_ALLOCATION_GUIDELINE.md#8-측정과-테스트` |
| `doc/guidelines/04-allocation-gc.md:24` | `doc/GC_ALLOCATION_GUIDELINE.md` | `../GC_ALLOCATION_GUIDELINE.md#L73` | `../GC_ALLOCATION_GUIDELINE.md#8-측정과-테스트` |
| `doc/guidelines/04-allocation-gc.md:25` | `doc/GC_ALLOCATION_GUIDELINE.md` | `../GC_ALLOCATION_GUIDELINE.md#L89` | `../GC_ALLOCATION_GUIDELINE.md#10-알려진-위반-이-revision-기준` |
| `doc/guidelines/04-allocation-gc.md:26` | `doc/GC_ALLOCATION_GUIDELINE.md` | `../GC_ALLOCATION_GUIDELINE.md#L5` | 링크 제거; 원래 표시 `근거 revision:5` 유지 |
| `doc/guidelines/04-allocation-gc.md:29` | `doc/GC_ALLOCATION_GUIDELINE.md` | `../GC_ALLOCATION_GUIDELINE.md#L25` | `../GC_ALLOCATION_GUIDELINE.md#3-공개-api-규칙` |
| `doc/guidelines/04-allocation-gc.md:32` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L314` | `../IMPROVEMENT_BACKLOG.md#imp-12-gc-가이드라인-확정` |
| `doc/guidelines/06-documentation.md:7` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L7` | `../DOCUMENTATION_GUIDE.md#문서의-역할과-경로` |
| `doc/guidelines/06-documentation.md:7` | `AGENTS.md` | `../../AGENTS.md#L48` | `../../AGENTS.md#사람ai용-외부-제공-문서` |
| `doc/guidelines/06-documentation.md:8` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L21` | `../DOCUMENTATION_GUIDE.md#공통-사실과-상태` |
| `doc/guidelines/06-documentation.md:9` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L73` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/06-documentation.md:10` | `AGENTS.md` | `../../AGENTS.md#L56` | `../../AGENTS.md#단위-작업-회고` |
| `doc/guidelines/06-documentation.md:19` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L18` | `../DOCUMENTATION_GUIDE.md#공통-사실과-상태` |
| `doc/guidelines/06-documentation.md:22` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L103` | `../IMPROVEMENT_BACKLOG.md#결정-기록-사용자가-작성` |
| `doc/guidelines/06-documentation.md:22` | `AGENTS.md` | `../../AGENTS.md#L56` | `../../AGENTS.md#단위-작업-회고` |
| `doc/guidelines/06-documentation.md:24` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L19` | `../DOCUMENTATION_GUIDE.md#공통-사실과-상태` |
| `doc/guidelines/06-documentation.md:41` | `doc/INPUT_SYSTEM_DRAFT.md` | `../INPUT_SYSTEM_DRAFT.md#L8` | 링크 제거; 원래 표시 `Input:8` 유지 |
| `doc/guidelines/06-documentation.md:41` | `doc/DATA_TABLE_MAPPING_DRAFT.md` | `../DATA_TABLE_MAPPING_DRAFT.md#L8` | 링크 제거; 원래 표시 `Mapping:8` 유지 |
| `doc/guidelines/06-documentation.md:41` | `doc/GAME_UI_SYSTEM_DRAFT.md` | `../GAME_UI_SYSTEM_DRAFT.md#L5` | 링크 제거; 원래 표시 `UI 설계 기록:5` 유지 |
| `doc/guidelines/06-documentation.md:46` | `AGENTS.md` | `../../AGENTS.md#L118` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/06-documentation.md:50` | `doc/DISTRIBUTION_PIPELINE.md` | `../DISTRIBUTION_PIPELINE.md#L128` | `../DISTRIBUTION_PIPELINE.md#최소-실행-도구와-기록` |
| `doc/guidelines/07-validation-evidence.md:7` | `AGENTS.md` | `../../AGENTS.md#L42` | `../../AGENTS.md#tdd와-완료-기준` |
| `doc/guidelines/07-validation-evidence.md:7` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L323` | `../IMPROVEMENT_BACKLOG.md#항목별-완료-보고-형식` |
| `doc/guidelines/07-validation-evidence.md:9` | `AGENTS.md` | `../../AGENTS.md#L118` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/07-validation-evidence.md:23` | `AGENTS.md` | `../../AGENTS.md#L118` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/07-validation-evidence.md:30` | `AGENTS.md` | `../../AGENTS.md#L61` | `../../AGENTS.md#임시-파일과-재사용-도구` |
| `doc/guidelines/07-validation-evidence.md:31` | `AGENTS.md` | `../../AGENTS.md#L71` | `../../AGENTS.md#이름경로-유지` |
| `doc/guidelines/08-change-procedure.md:11` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L74` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/08-change-procedure.md:12` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L73` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/08-change-procedure.md:13` | `AGENTS.md` | `../../AGENTS.md#L39` | `../../AGENTS.md#tdd와-완료-기준` |
| `doc/guidelines/08-change-procedure.md:13` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L23` | `../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완` |
| `doc/guidelines/08-change-procedure.md:13` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L26` | `../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완` |
| `doc/guidelines/08-change-procedure.md:16` | `AGENTS.md` | `../../AGENTS.md#L118` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/08-change-procedure.md:16` | `AGENTS.md` | `../../AGENTS.md#L75` | `../../AGENTS.md#참조와-변경-경계` |
| `doc/guidelines/08-change-procedure.md:18` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L75` | `../IMPROVEMENT_BACKLOG.md#자동-진행을-멈추고-보고하는-경우-다음-항목으로-넘어가지-않는다` |
| `doc/guidelines/08-change-procedure.md:19` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L277` | `../IMPROVEMENT_BACKLOG.md#imp-07-ci-구성` |
| `doc/guidelines/08-change-procedure.md:37` | `AGENTS.md` | `../../AGENTS.md#L104` | `../../AGENTS.md#git-작업-시작-전-확인` |
| `doc/guidelines/08-change-procedure.md:38` | `AGENTS.md` | `../../AGENTS.md#L39` | `../../AGENTS.md#tdd와-완료-기준` |
| `doc/guidelines/08-change-procedure.md:39` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L23` | `../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완` |
| `doc/guidelines/08-change-procedure.md:40` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L24` | `../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완` |
| `doc/guidelines/08-change-procedure.md:41` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L28` | `../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완` |
| `doc/guidelines/08-change-procedure.md:42` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L73` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/08-change-procedure.md:43` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L18` | `../DOCUMENTATION_GUIDE.md#공통-사실과-상태` |
| `doc/guidelines/08-change-procedure.md:44` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L74` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/08-change-procedure.md:45` | `AGENTS.md` | `../../AGENTS.md#L56` | `../../AGENTS.md#단위-작업-회고` |
| `doc/guidelines/08-change-procedure.md:46` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L318` | `../IMPROVEMENT_BACKLOG.md#항목별-완료-보고-형식` |
| `doc/guidelines/08-change-procedure.md:47` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L58` | `../IMPROVEMENT_BACKLOG.md#항목-하나의-흐름` |
| `doc/guidelines/08-change-procedure.md:48` | `AGENTS.md` | `../../AGENTS.md#L118` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/08-change-procedure.md:49` | `AGENTS.md` | `../../AGENTS.md#L116` | `../../AGENTS.md#검증-후-병합` |
| `doc/guidelines/08-change-procedure.md:50` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L75` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/08-change-procedure.md:54` | `doc/IMPROVEMENT_BACKLOG.md` | `../IMPROVEMENT_BACKLOG.md#L323` | `../IMPROVEMENT_BACKLOG.md#항목별-완료-보고-형식` |
| `doc/guidelines/README.md:9` | `doc/DOCUMENTATION_GUIDE.md` | `../DOCUMENTATION_GUIDE.md#L74` | `../DOCUMENTATION_GUIDE.md#갱신과-검증` |
| `doc/guidelines/README.md:62` | `doc/DISTRIBUTION_PIPELINE.md` | `../DISTRIBUTION_PIPELINE.md#L128` | `../DISTRIBUTION_PIPELINE.md#최소-실행-도구와-기록` |
| `doc/guidelines/README.md:73` | `AGENTS.md` | `../../AGENTS.md#L112` | `../../AGENTS.md#검증-후-병합` |
| `doc/retrospectives/2026-10-13-06-programmer-guidelines-draft.md:17` | `AGENTS.md` | `../../AGENTS.md#L80` | `../../AGENTS.md#참조와-변경-경계` |
| `doc/retrospectives/2026-10-13-06-programmer-guidelines-draft.md:18` | `AGENTS.md` | `../../AGENTS.md#L49` | `../../AGENTS.md#사람ai용-외부-제공-문서` |
| `doc/retrospectives/2026-10-13-06-programmer-guidelines-draft.md:19` | `AGENTS.md` | `../../AGENTS.md#L5` | `../../AGENTS.md#목적과-역할` |
| `doc/retrospectives/2026-10-13-06-programmer-guidelines-draft.md:36` | `AGENTS.md` | `../../AGENTS.md#L53` | `../../AGENTS.md#사람ai용-외부-제공-문서` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:30` | `AGENTS.md` | `../../AGENTS.md#L85` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:32` | `AGENTS.md` | `../../AGENTS.md#L85` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:33` | `AGENTS.md` | `../../AGENTS.md#L85` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:34` | `AGENTS.md` | `../../AGENTS.md#L85` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:35` | `AGENTS.md` | `../../AGENTS.md#L86` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:36` | `AGENTS.md` | `../../AGENTS.md#L86` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:37` | `AGENTS.md` | `../../AGENTS.md#L86` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:39` | `AGENTS.md` | `../../AGENTS.md#L87` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:41` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:42` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:43` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:44` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:45` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:46` | `AGENTS.md` | `../../AGENTS.md#L88` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:47` | `AGENTS.md` | `../../AGENTS.md#L89` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:48` | `AGENTS.md` | `../../AGENTS.md#L89` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:49` | `AGENTS.md` | `../../AGENTS.md#L90` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:51` | `AGENTS.md` | `../../AGENTS.md#L90` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:55` | `AGENTS.md` | `../../AGENTS.md#L22` | `../../AGENTS.md#c과-unity-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:56` | `AGENTS.md` | `../../AGENTS.md#L22` | `../../AGENTS.md#c과-unity-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:57` | `AGENTS.md` | `../../AGENTS.md#L22` | `../../AGENTS.md#c과-unity-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:58` | `AGENTS.md` | `../../AGENTS.md#L22` | `../../AGENTS.md#c과-unity-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:59` | `AGENTS.md` | `../../AGENTS.md#L22` | `../../AGENTS.md#c과-unity-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:60` | `AGENTS.md` | `../../AGENTS.md#L35` | `../../AGENTS.md#solid와-최소-구현` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:61` | `AGENTS.md` | `../../AGENTS.md#L35` | `../../AGENTS.md#solid와-최소-구현` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:62` | `AGENTS.md` | `../../AGENTS.md#L35` | `../../AGENTS.md#solid와-최소-구현` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:64` | `AGENTS.md` | `../../AGENTS.md#L86` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:65` | `AGENTS.md` | `../../AGENTS.md#L85` | `../../AGENTS.md#협업통합과-완료된-track` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:66` | `AGENTS.md` | `../../AGENTS.md#L44` | `../../AGENTS.md#tdd와-완료-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:67` | `AGENTS.md` | `../../AGENTS.md#L44` | `../../AGENTS.md#tdd와-완료-기준` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:79` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L46` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:80` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L47` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:81` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L47` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:82` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L48` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:83` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L48` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:84` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L49` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:85` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L49` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:86` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L50` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:87` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L50` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:88` | `doc/guidelines/06-documentation.md` | `../guidelines/06-documentation.md#L51` | `../guidelines/06-documentation.md#기존-자료와-배포-사본` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:89` | `doc/guidelines/07-validation-evidence.md` | `../guidelines/07-validation-evidence.md#L47` | `../guidelines/07-validation-evidence.md#증거-분류와-원문-보존-상세--2026-10-13-이관` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:90` | `doc/guidelines/07-validation-evidence.md` | `../guidelines/07-validation-evidence.md#L47` | `../guidelines/07-validation-evidence.md#증거-분류와-원문-보존-상세--2026-10-13-이관` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:91` | `doc/guidelines/07-validation-evidence.md` | `../guidelines/07-validation-evidence.md#L47` | `../guidelines/07-validation-evidence.md#증거-분류와-원문-보존-상세--2026-10-13-이관` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:92` | `doc/guidelines/07-validation-evidence.md` | `../guidelines/07-validation-evidence.md#L48` | `../guidelines/07-validation-evidence.md#증거-분류와-원문-보존-상세--2026-10-13-이관` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:93` | `doc/guidelines/07-validation-evidence.md` | `../guidelines/07-validation-evidence.md#L48` | `../guidelines/07-validation-evidence.md#증거-분류와-원문-보존-상세--2026-10-13-이관` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:177` | `CLAUDE.md` | `../../CLAUDE.md#L33` | `../../CLAUDE.md#검토-항목` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:178` | `doc/GAME_SCENE_MANAGER_DRAFT.md` | `../GAME_SCENE_MANAGER_DRAFT.md#L162` | `../GAME_SCENE_MANAGER_DRAFT.md#imp-05-후속-구현-2026-10-13` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:179` | `doc/CI_SETUP_GUIDE.md` | `../CI_SETUP_GUIDE.md#L3` | 링크 제거; 원래 표시 `CI_SETUP_GUIDE:3` 유지 |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:180` | `doc/UI_ACCEPTANCE.md` | `../UI_ACCEPTANCE.md#L3` | 링크 제거; 원래 표시 `UI_ACCEPTANCE:3` 유지 |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:181` | `doc/INPUT_SYSTEM_TRACK.md` | `../INPUT_SYSTEM_TRACK.md#L47` | `../INPUT_SYSTEM_TRACK.md#통합수락` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:182` | `doc/SCENE_TRANSITION_ACCEPTANCE.md` | `../SCENE_TRANSITION_ACCEPTANCE.md#L33` | `../SCENE_TRANSITION_ACCEPTANCE.md#single-확인-및-복구` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:182` | `doc/SCENE_TRANSITION_TRACK.md` | `../SCENE_TRANSITION_TRACK.md#L93` | `../SCENE_TRANSITION_TRACK.md#통합-및-최종-종료-gate` |
| `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:183` | `AGENTS.md` | `../../AGENTS.md#L8` | `../../AGENTS.md#목적과-역할` |
| `doc/retrospectives/2026-10-15-02-phase3-close.md:41` | `doc/retrospectives/2026-10-15-01-gamescene-safetynet-b.md` | `2026-10-15-01-gamescene-safetynet-b.md#L49` | `2026-10-15-01-gamescene-safetynet-b.md#환경명령정상본회귀` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:9` | `doc/UI_ACCEPTANCE.md` | `../UI_ACCEPTANCE.md#L5` | 링크 제거; 원래 표시 `수락 기록` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:9` | `doc/retrospectives/2026-10-12-03-ui-main-integration.md` | `2026-10-12-03-ui-main-integration.md#L3` | 링크 제거; 원래 표시 `2026-10-12 통합 회고` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:9` | `doc/retrospectives/2026-10-12-03-ui-main-integration.md` | `2026-10-12-03-ui-main-integration.md#L11` | 링크 제거; 원래 표시 `실제 Git 마감` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:17` | `doc/api/UI.md` | `../../doc/api/UI.md#L11` | `../../doc/api/UI.md#responsibility-and-ownership` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:18` | `doc/ai/api/UI.md` | `../../doc/ai/api/UI.md#L128` | `../../doc/ai/api/UI.md#p3-hosts-selection-presentation-and-constraints` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:19` | `doc/api/UI.md` | `../../doc/api/UI.md#L13` | `../../doc/api/UI.md#responsibility-and-ownership` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:20` | `doc/GAME_UI_SYSTEM.md` | `../../doc/GAME_UI_SYSTEM.md#L124` | `../../doc/GAME_UI_SYSTEM.md#p7-source별-증거-및-현재-통합-상태` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:21` | `doc/GAME_UI_SYSTEM.md` | `../../doc/GAME_UI_SYSTEM.md#L124` | `../../doc/GAME_UI_SYSTEM.md#p7-source별-증거-및-현재-통합-상태` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:22` | `doc/ai/api/UI.md` | `../../doc/ai/api/UI.md#L62` | `../../doc/ai/api/UI.md#configuration--extensionpoints` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:23` | `doc/ai/README.md` | `../../doc/ai/README.md#L26` | 링크 제거; 원래 표시 `doc/ai/README.md:26` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:24` | `doc/ai/README.md` | `../../doc/ai/README.md#L37` | 링크 제거; 원래 표시 `doc/ai/README.md:37` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` | `doc/api/UI.md` | `../api/UI.md#L3` | 링크 제거; 원래 표시 `사람 API:3` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` | `doc/ai/api/UI.md` | `../ai/api/UI.md#L13` | `../ai/api/UI.md#implementationstatus--validationstatus--evidence` |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` | `doc/ai/README.md` | `../ai/README.md#L28` | 링크 제거; 원래 표시 `AI README:28` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` | `doc/ai/README.md` | `../ai/README.md#L29` | 링크 제거; 원래 표시 `29` 유지 |
| `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` | `doc/ai/api/UI.md` | `../ai/api/UI.md#L112` | `../ai/api/UI.md#p4-modalinput-api-and-constraints` |
| `doc/retrospectives/2026-10-15-07-ui-source-close.md:9` | `doc/UI_ACCEPTANCE.md` | `../UI_ACCEPTANCE.md#L5` | 링크 제거; 원래 표시 `2026-10-12 UI 수락` 유지 |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:32` | `doc/retrospectives/2026-10-14-07-uicontext-safetynet-a.md` | `2026-10-14-07-uicontext-safetynet-a.md#L79` | `2026-10-14-07-uicontext-safetynet-a.md#환경실행-명령기준선` |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:33` | `doc/retrospectives/2026-10-14-09-guard-safetynet-a.md` | `2026-10-14-09-guard-safetynet-a.md#L267` | `2026-10-14-09-guard-safetynet-a.md#pr-제공-단계-반복-회귀와-정정-후-fixture-기준-변형2026-10-14` |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:34` | `doc/retrospectives/2026-10-14-09-guard-safetynet-a.md` | `2026-10-14-09-guard-safetynet-a.md#L220` | `2026-10-14-09-guard-safetynet-a.md#신규-ui-fixture-격리-정정만` |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:35` | `doc/retrospectives/2026-10-14-09-guard-safetynet-a.md` | `2026-10-14-09-guard-safetynet-a.md#L67` | `2026-10-14-09-guard-safetynet-a.md#정상본-실패조건부-승인-정정-이력` |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:35` | `doc/retrospectives/2026-10-15-05-test-teardown-hardening.md` | `2026-10-15-05-test-teardown-hardening.md#L25` | `2026-10-15-05-test-teardown-hardening.md#변경과-이유` |
| `doc/retrospectives/2026-10-15-08-guidelines-decisions-i.md:36` | `doc/retrospectives/2026-10-14-09-guard-safetynet-a.md` | `2026-10-14-09-guard-safetynet-a.md#L180` | `2026-10-14-09-guard-safetynet-a.md#신규-파일-제외와-단독-포함-실험` |

다른 가이드의 규칙·표 내용은 그대로이며 필요한 인용 대상만 보정했다. 06의 형식 아래에는 “사용자 결정 2026-10-15 후속 보완” 근거로 D-I3 적용 규칙2개를 [결정됨]으로 추가했다. 새 결정 ID는 없다. 06/README의 D-I3 후속 완료와 기존 회고 INDEX 행만 현행화했다.

## 기계 검사와 보호 확인

작업 소유 Temp 스크립트가 추적 Markdown에서 머리말 보유44문서를 독립 수집해 전수 검사했다. 다음 결과는 실제 실행한 검사이며 Unity 실행은 아니다.

<!-- MECHANICAL_RESULTS -->

| 검사 | 분모·실측 결과 | 검사 방식 |
|---|---|---|
| a 제목 다음 정확한 필드 순서 | 44/44 통과 | 유형/현재 상태/기준 source/갱신일/계약 링크의 5줄 |
| b 머리말 줄 번호 링크 금지 | 44/44 통과; 0개 | 5줄 전체에서 #L + 숫자 검색 |
| c 신규 머리말 삽입 diff | 신규13/13 통과; 각 +5/-0 | 시작 HEAD2904394 대비; PR base 대비는 전체44/44 각 +5/-0 |
| d 유형 열거값 | 44/44 통과 | 가이드/레퍼런스/결정 기록 |
| e 내용 변경일 | 44/44 통과 | 위 산정식과 선택 commit 날짜 일치 |
| f 계약 링크 분류·존재 | 44/44 통과; a22/b15/c7 | a/c 리터럴 또는 b의 본문 명시·대상 파일 실존 |
| 상대 .md 줄 번호 링크 | 171 → 0 | tracked doc 전체 목적지 독립 검색 |
| 본문/이력 보호 | 44문서 본문 동일; 과거 회고6개 표시 문장 동일 | base 대비 머리말5줄만; 링크 목적지 정규화 후 과거 문장 비교 |
| 기존 dirty | 10/10 raw SHA256 동일 | 시작 snapshot과 비교 |
| verifier 보호 입력 | 836경로 존재·hash 상태 동일; 문서 대상 교집합0 | 선언 파일 읽기 전용 수집·전후 raw hash 비교 |
| verifier 결과 | 17실패 → 17실패; 새 실패0 | 16 checks 폴더 + Bootstrap 1; 종료 코드/stdout/stderr 원문 전체 동일 |

checks/input/hash/도구/기대값은 수정하지 않았다. 17건은 사용자 결정에 따른 역사적 기준선이며 현재 source 성공이 아니다. 기존 첫 실패 원문은 옛 Assets/MyLab 입력 경로 누락11건과 ISceneRoot source hash 불일치6건이다. 검증기마다 첫 실패에서 멈추므로 미도달 검사가 통과했다는 뜻도 아니다.

검사기는 Temp에만 두었다. 문서 heading anchor를 검사할 때 inline code 안의 `<T>`를 HTML로 잘못 지우지 않도록 Temp 검사기의 slug 계산만 바로잡았다. 저장소 tools·workflow 변경은0이다.

라벨은 실제 문장·표에서 다시 셌다. 이번 보완 전06은13/21/0, 후는**13/23/0**, README는**25/28/1 → 25/28/1**(현행/결정됨/제안)이다. README LABEL_COUNTS와 독립 재계산이 일치한다. 다른 장의 규범·상태 라벨과56칸 내용은 바뀌지 않았다. 남은 제안은 Timeout 세부 실험/값 결정이며 이번 머리말 범위 밖이다.

## 명령·환경·실행하지 않은 것

Windows PowerShell·Git·Python3.14를 사용했다. 원시 JSON/검사기는 작업 소유 `Temp/docs-header-revision-20261015/`에만 둔다. 영속 문서의 사실·판정은 이 회고에 남기며 Temp 도구를 저장소에 추가하지 않는다.

```text
git log --follow --format=COMMIT:%H:%cs --name-status -M d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f -- <문서>
C:/Python314/python.exe -B Temp/docs-header-revision-20261015/check_revision.py
C:/Python314/python.exe -B Temp/docs-header-revision-20261015/verify_doc_links.py --label final
C:/Python314/python.exe -B tools/verify_validation.py --evidence <16개 checks 폴더>
C:/Python314/python.exe -B doc/validation/bootstrap-system/verify.py
C:/Python314/python.exe -B tools/check_pr_rules.py --base d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f --head HEAD
git diff --check d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f HEAD
C:/Python314/python.exe -B tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <40자리 tip> --output Temp/docs-header-revision-20261015/ci-final.json
```

미실행: Unity 컴파일·EditMode/PlayMode·제품 Console·Player/IL2CPP·GC·설치/패키징/배포·과거 source 환경 재현. C#·테스트·asmdef·tools·workflow·upm·AGENTS·CLAUDE의 이 작업 diff는0이다. main 병합·브랜치 삭제·PC 제어도 하지 않는다.

현재 사용자 결정이 필요한 보류 문서는0개다. 역사적 문서12개와 CI 판정 문서의 과거 제안 본문은 그대로 보존하며 이후 정리가 필요하면 별도 범위를 정한다. 이 기록만으로 후속 정리 권한을 만들지 않는다.

## 최종 커밋 검증

문서/anchor 검사는 Markdown255개·상대 링크2082개·anchor476개·오류0이다. 신규 링크 오류0, 기존 제목 변경0이다. 범위 감사에서 이번 후속 변경은 허용된 Markdown59개뿐이며44개 머리말의 본문·과거 회고6개의 표시 문장·다른 가이드의 규칙 내용을 보존했다. Temp 라벨 감사의 첫 정규식이 CHECKLIST의 두 번째 열 라벨을 놓친 것은 검사기만 보정해 재실행했으며, 원본 문서·라벨 표는 추가로 바꾸지 않았다. 공백·규칙·exact-SHA Actions는 실제 커밋 뒤 다시 조회해 아래에 확정한다. PR #33은 open으로 유지하며 사용자 승인 전 병합하지 않는다.

문서 보완 source commit은 `b6bb49d8c8f098b62c4e5e66f9b10417fd0caf45`다. 실제 커밋의 규칙 검사 **오류0·경고0**(보호 파일 경고 없음), 공백 검사0, 링크/anchor0을 확인했다. [source exact-SHA Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38703638370)는 completed/success이며 check1/run1·SHA 일치와 workflows/checks/statuses/runs 조회 완전성을 확인했다. 실행 중 첫 조회는 in_progress였으므로 성공으로 처리하지 않았고 완료 후 다시 확인했다. 이 검사 요약만 추가한 최종 HEAD도 exact-SHA로 재확인해 PR/채팅 보고에 연결한다.

임시 원시 출력·검사기·before/after 목록은 PR #33 사용자 검토의 추적용으로 위 Temp 작업 폴더에 유지한다. 사용자가 수락하고 더 이상 실행·조사 참조가 없으면 이 작업 소유 명시 목록만 제거하며 다른 작업 Temp와 사용자 파일은 건드리지 않는다.
