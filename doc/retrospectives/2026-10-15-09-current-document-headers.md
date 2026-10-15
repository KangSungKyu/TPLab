# D-I3 현행 문서 공통 머리말 적용

위험 등급: **T0 · 문서만**. 사용자 2026-10-15 요청의 5개 필드 머리말·줄 번호 링크 보정과 허용한 가이드 두 곳의 후속 완료/라벨 갱신만 수행한다. Unity·런타임·테스트·assembly·tools·workflow·upm·AGENTS·CLAUDE 변경은 없다. 본문의 상태·검증 의미를 새로 판단하거나 source를 최신화하지 않는다.

## 선행 병합과 작업 기준

PR #32의 승인 HEAD `876ce9aa331a5ab4bc0fa2e9f7f3def170220749`·최종 exact-SHA Actions 성공·base main `af084e0d0027c4098fad08c336ccb0753e1c7862`를 다시 확인한 뒤 사용자 승인으로 일반 merge했다. 병합 SHA와 이 작업 base는 `d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f`다. 로컬/원격 main 일치, 해당 tip의 main 포함, 다른 worktree 사용 없음, 삭제 직전 remote tip 일치를 확인한 뒤 `codex/guidelines-decisions-i`의 원격을 SHA lease로 삭제하고 로컬은 `git branch -d`로 정리했다. 원본 커밋은 main에서 도달 가능하며 이력 재작성은 없다.

GitHub 조회에서 열린 PR0을 확인하고 최신 main에서 `codex/docs-current-headers`를 만들었다. 기존 dirty10개(기존6개와 Netcode 관련4개)는 raw SHA256을 보존했다. PR #32는 closed/merged=true이며 이번 후속 PR은 사용자 승인 전 병합하지 않는다.

## 적용 전 분류표

범위는 `doc/*.md`, `doc/api/*.md`, `doc/ai/*.md`, `doc/ai/api/*.md`의 실제56개다. 전체 본문을 읽어 상태/source/계약 링크·제목·과거 표기를 추출하고 관련 문장을 직접 대조했다. 적용31·제외24·판단 보류1이다. 회고·validation 디렉터리는 애초 대상 밖이며 아래의 제외는 이56개 안에서만 센다.

| 문서 | 분류 | 유형 | 이유 |
|---|---|---|---|
| [doc/ai/api/DataTables.md](../ai/api/DataTables.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/Editor.md](../ai/api/Editor.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/Input.md](../ai/api/Input.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/Lifecycle.md](../ai/api/Lifecycle.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/Pooling.md](../ai/api/Pooling.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/README.md](../ai/api/README.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/Resources.md](../ai/api/Resources.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/SceneManagement.md](../ai/api/SceneManagement.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/ai/api/UI.md](../ai/api/UI.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/ai/README.md](../ai/README.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/api/DataTables.md](../api/DataTables.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/api/Editor.md](../api/Editor.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/api/Input.md](../api/Input.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/api/Lifecycle.md](../api/Lifecycle.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/api/Pooling.md](../api/Pooling.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/api/README.md](../api/README.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/api/Resources.md](../api/Resources.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/api/SceneManagement.md](../api/SceneManagement.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/api/UI.md](../api/UI.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/ASYNC_SCENE_LIFECYCLE.md](../ASYNC_SCENE_LIFECYCLE.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/BOOTSTRAP_SYSTEM.md](../BOOTSTRAP_SYSTEM.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/CI_GATE_PROPOSAL.md](../CI_GATE_PROPOSAL.md) | 판단 보류 | 미적용 | AGENTS의 현행 판정 참조와 본문의 과거 PR 제안·follow-up이 혼재하여 유형 판단 보류 |
| [doc/CI_SETUP_GUIDE.md](../CI_SETUP_GUIDE.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/CORE_PLAN.md](../CORE_PLAN.md) | 적용 | 결정 기록 | 사용자 목표·채택·후속 범위 결정 기록; 상태는 본문의 확정/제안 구분 위치로 연결 |
| [doc/DATA_TABLE_GENERIC_IMPLEMENTATION.md](../DATA_TABLE_GENERIC_IMPLEMENTATION.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/DATA_TABLE_IDX_DRAFT.md](../DATA_TABLE_IDX_DRAFT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/DATA_TABLE_IMPORTER_DRAFT.md](../DATA_TABLE_IMPORTER_DRAFT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/DATA_TABLE_MANAGER.md](../DATA_TABLE_MANAGER.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/DATA_TABLE_MAPPING_DRAFT.md](../DATA_TABLE_MAPPING_DRAFT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/DISTRIBUTION_ACCEPTANCE.md](../DISTRIBUTION_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/DISTRIBUTION_PIPELINE.md](../DISTRIBUTION_PIPELINE.md) | 적용 | 가이드 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/DOCUMENTATION_GUIDE.md](../DOCUMENTATION_GUIDE.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/GAME_SCENE_MANAGER_DRAFT.md](../GAME_SCENE_MANAGER_DRAFT.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/GAME_UI_SYSTEM.md](../GAME_UI_SYSTEM.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/GAME_UI_SYSTEM_DRAFT.md](../GAME_UI_SYSTEM_DRAFT.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/GAME_UI_SYSTEM_TRACK.md](../GAME_UI_SYSTEM_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/GC_ALLOCATION_GUIDELINE.md](../GC_ALLOCATION_GUIDELINE.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/GENERIC_POOL.md](../GENERIC_POOL.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/IMPROVEMENT_BACKLOG.md](../IMPROVEMENT_BACKLOG.md) | 제외 | 미적용 | 수정 금지 가이드/과거 회고의 상대 #L 인용 보존; 해당 참조 수정 없이 머리말 추가 불가 |
| [doc/INDEX.md](../INDEX.md) | 적용 | 레퍼런스 | 현재 문서·계약 탐색 색인이므로 레퍼런스로 적용; 별도 검증 상태는 만들지 않음 |
| [doc/INPUT_SYSTEM_ACCEPTANCE.md](../INPUT_SYSTEM_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/INPUT_SYSTEM_DRAFT.md](../INPUT_SYSTEM_DRAFT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/INPUT_SYSTEM_TRACK.md](../INPUT_SYSTEM_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/OBJECT_POOL.md](../OBJECT_POOL.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/RESOURCE_MANAGER.md](../RESOURCE_MANAGER.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/SCENE_LOADING.md](../SCENE_LOADING.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/SCENE_LOADING_ACCEPTANCE.md](../SCENE_LOADING_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/SCENE_LOADING_PRESENTATION_DRAFT.md](../SCENE_LOADING_PRESENTATION_DRAFT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/SCENE_LOADING_TRACK.md](../SCENE_LOADING_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/SCENE_ROOT.md](../SCENE_ROOT.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/SCENE_TRANSITION_ACCEPTANCE.md](../SCENE_TRANSITION_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/SCENE_TRANSITION_TRACK.md](../SCENE_TRANSITION_TRACK.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/SINGLETON.md](../SINGLETON.md) | 적용 | 레퍼런스 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/TPLAB_NAMING.md](../TPLAB_NAMING.md) | 적용 | 가이드 | 현재 계약·API 탐색 또는 현행 결정·운영 기준 문서 |
| [doc/UI_ACCEPTANCE.md](../UI_ACCEPTANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |
| [doc/UI_PERFORMANCE.md](../UI_PERFORMANCE.md) | 제외 | 미적용 | 날짜가 붙은 과거 기록: 원문 보존 |

제외24는 날짜가 붙은 과거 기록12개와 수정 금지 문서의 상대 #L 인용을 보존해야 하는 현행 문서12개다. hash 보호 때문에 제외된 문서는0이다. `CI_GATE_PROPOSAL.md`는 AGENTS가 현재 판정 기준으로 링크하는 한편 본문은 이전 PR의 제안 표·follow-up을 보존한다. 이력/현행 유형을 이번 PR에서 억지로 결정하지 않고 그대로 보류했다.

### 수정 금지 인용 때문에 제외한 12개

과거 회고의 fixed-SHA permalink는 당시 파일을 가리키므로 변경하지 않는다. 아래는 그와 달리 현재 checkout을 가리키는 상대 #L 링크다. 원문 보존·가이드 수정 제한을 지키면서 이 참조까지 고칠 수 없어 대상 문서에 머리말을 넣지 않았다.

| 제외 대상 | 수정 금지 참조 위치 |
|---|---|
| `doc/ai/api/UI.md` | `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:18`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:22`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` |
| `doc/ai/README.md` | `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:23`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:24`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` |
| `doc/api/Lifecycle.md` | `doc/guidelines/02-public-api.md:8`, `doc/guidelines/02-public-api.md:10` |
| `doc/api/Pooling.md` | `doc/guidelines/02-public-api.md:7` |
| `doc/api/SceneManagement.md` | `doc/guidelines/02-public-api.md:23`, `doc/guidelines/02-public-api.md:24`, `doc/guidelines/02-public-api.md:25` |
| `doc/api/UI.md` | `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:17`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:19`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:34` |
| `doc/CI_SETUP_GUIDE.md` | `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:179` |
| `doc/DOCUMENTATION_GUIDE.md` | `doc/guidelines/02-public-api.md:32`, `doc/guidelines/02-public-api.md:33`, `doc/guidelines/08-change-procedure.md:11`, `doc/guidelines/08-change-procedure.md:12`, `doc/guidelines/08-change-procedure.md:42`, `doc/guidelines/08-change-procedure.md:43`, `doc/guidelines/08-change-procedure.md:44`, `doc/guidelines/08-change-procedure.md:50` |
| `doc/GAME_SCENE_MANAGER_DRAFT.md` | `doc/retrospectives/2026-10-13-07-docs-governance-slim.md:178` |
| `doc/GAME_UI_SYSTEM.md` | `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:20`, `doc/retrospectives/2026-10-15-06-ui-docs-status-alignment.md:21` |
| `doc/GC_ALLOCATION_GUIDELINE.md` | `doc/guidelines/04-allocation-gc.md:23`, `doc/guidelines/04-allocation-gc.md:24`, `doc/guidelines/04-allocation-gc.md:25`, `doc/guidelines/04-allocation-gc.md:26`, `doc/guidelines/04-allocation-gc.md:29` |
| `doc/IMPROVEMENT_BACKLOG.md` | `doc/guidelines/04-allocation-gc.md:17`, `doc/guidelines/04-allocation-gc.md:32`, `doc/guidelines/07-validation-evidence.md:7`, `doc/guidelines/08-change-procedure.md:13`, `doc/guidelines/08-change-procedure.md:18`, `doc/guidelines/08-change-procedure.md:19`, `doc/guidelines/08-change-procedure.md:39`, `doc/guidelines/08-change-procedure.md:40`, `doc/guidelines/08-change-procedure.md:41`, `doc/guidelines/08-change-procedure.md:46`, `doc/guidelines/08-change-procedure.md:47`, `doc/guidelines/08-change-procedure.md:54` |

## 머리말 값과 원문 보존

형식은 제목 바로 다음에 유형·현재 상태·기준 source·갱신일·계약 링크의 정확한5줄이다. 기존 빈 줄은 그대로 두었으며 원래2행 이후의 본문은5행만 이동한다. 코드 fence·추가 필드·별도 검증 상태는 넣지 않았다.

- 유형은06의 가이드/레퍼런스/결정 기록 정의로 분류했다. API·현재 계약·색인은 레퍼런스, 배포/이름 운영 기준은 가이드, CORE_PLAN의 채택·후속 범위는 결정 기록이다. 애매한 CI 판정 문서는 보류했다.
- 현재 상태는 기존 상태 문장이 있는 원문 위치로 self #L 링크했다. 그 문장을 요약해 새 완료·검증 판단을 만들지 않았다. 문서 자체의 상태 문장이 없으면 “본문 참조”다.
- 기준 source는 본문에 명시한 revision/tag를 그대로 옮겼다. API 색인의 original seven modules/P6/P7, idx의 TPLab/Cashier, pool의 IMP-01은 본문의 서로 다른 source 범위를 구분해 표기했다. 문서 자체의 근거 source가 없고 개별 기능/의존성/제3자 조회 commit만 있는 경우는 그 값을 문서 전체 source로 승격하지 않고 “미표기(본문 근거 링크 참조)”로 뒀다. 이 PR base를 문서 검증 source로 쓰지 않았다.
- 갱신일은 적용 직전 base에서 `git log -1 --format=%cs <base> -- <문서>`로 확인한 마지막 내용 변경 날짜다. 오늘 날짜를 일괄 대입하지 않았고 머리말 커밋은 포함하지 않는다.
- 계약 링크는 본문에 이미 있는 계약 문서/소스 링크 대상만 재사용했다. 없으면 “본문 참조”다.

| 값이 없는 항목 | 수 | 문서 |
|---|---:|---|
| 기준 source | 12 | `doc/ASYNC_SCENE_LIFECYCLE.md`, `doc/BOOTSTRAP_SYSTEM.md`, `doc/CORE_PLAN.md`, `doc/DATA_TABLE_MANAGER.md`, `doc/INDEX.md`, `doc/INPUT_SYSTEM_DRAFT.md`, `doc/RESOURCE_MANAGER.md`, `doc/SCENE_LOADING.md`, `doc/SCENE_LOADING_PRESENTATION_DRAFT.md`, `doc/SCENE_ROOT.md`, `doc/SINGLETON.md`, `doc/TPLAB_NAMING.md` |
| 현재 상태 | 8 | `doc/ai/api/README.md`, `doc/api/README.md`, `doc/DATA_TABLE_MANAGER.md`, `doc/INDEX.md`, `doc/RESOURCE_MANAGER.md`, `doc/SCENE_ROOT.md`, `doc/SINGLETON.md`, `doc/TPLAB_NAMING.md` |
| 계약 링크 | 5 | `doc/DISTRIBUTION_PIPELINE.md`, `doc/GENERIC_POOL.md`, `doc/SCENE_LOADING.md`, `doc/SINGLETON.md`, `doc/TPLAB_NAMING.md` |

“미표기”12개, “본문 참조”는 상태8·계약5의13개 필드다(중복 문서 포함 필드 수). 원문에 없는 사실을 보충하지 않았다. 다음 표는 적용한 값의 출처 추적용이며 새 실행 검증을 뜻하지 않는다.

| 적용 문서 | 상태 원문 줄 | 본문에서 옮긴 source | 마지막 내용 날짜 | 계약 원문 대상 |
|---|---:|---|---|---|
| `doc/ai/api/DataTables.md` | 9 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../../DATA_TABLE_MANAGER.md` |
| `doc/ai/api/Editor.md` | 9 | 32a74d5a5f201bd582a8cbe5ff9d4f40dc5298ed | 2026-10-08 | `../../DATA_TABLE_IMPORTER_DRAFT.md` |
| `doc/ai/api/Input.md` | 9 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../../INPUT_SYSTEM_DRAFT.md` |
| `doc/ai/api/Lifecycle.md` | 9 | 5fe5bf5a41386409775d836e5b3a50a2f516a787 | 2026-10-13 | `../../api/Lifecycle.md` |
| `doc/ai/api/Pooling.md` | 11 | 2c8c0768c53878dcc9c2d4ed090582092e9dc5e0 | 2026-10-14 | `../../api/Pooling.md` |
| `doc/ai/api/README.md` | 본문 참조 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 (original seven modules); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 UI runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (P7 regression snapshot) | 2026-10-14 | `UI.md` |
| `doc/ai/api/Resources.md` | 7 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs` |
| `doc/ai/api/SceneManagement.md` | 9 | 0cc919b6c497f348deae3e6ae4dffb33f21c8482 | 2026-10-13 | `../../GAME_SCENE_MANAGER_DRAFT.md` |
| `doc/api/DataTables.md` | 7 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../DATA_TABLE_MANAGER.md` |
| `doc/api/Editor.md` | 7 | 32a74d5a5f201bd582a8cbe5ff9d4f40dc5298ed | 2026-10-08 | `../DATA_TABLE_IMPORTER_DRAFT.md` |
| `doc/api/Input.md` | 7 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../INPUT_SYSTEM_DRAFT.md` |
| `doc/api/README.md` | 본문 참조 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 (original seven modules); 21f89e2580f08be3903724efa0a42e5ad0567c83 (P6 UI runtime); 99df25dd6b89fd7f3e322c8033b61eb1364f495e (P7 regression snapshot) | 2026-10-14 | `UI.md` |
| `doc/api/Resources.md` | 11 | 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 | 2026-10-07 | `../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs` |
| `doc/ASYNC_SCENE_LIFECYCLE.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-14 | `api/SceneManagement.md` |
| `doc/BOOTSTRAP_SYSTEM.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-07 | `GAME_SCENE_MANAGER_DRAFT.md` |
| `doc/CORE_PLAN.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-15 | `GENERIC_POOL.md` |
| `doc/DATA_TABLE_IDX_DRAFT.md` | 3 | TPLab main / a7804a8; Cashier total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0 | 2026-10-07 | `DATA_TABLE_MAPPING_DRAFT.md` |
| `doc/DATA_TABLE_IMPORTER_DRAFT.md` | 3 | main/19b5770 | 2026-10-08 | `DATA_TABLE_MANAGER.md` |
| `doc/DATA_TABLE_MANAGER.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-07 | `DATA_TABLE_MAPPING_DRAFT.md` |
| `doc/DATA_TABLE_MAPPING_DRAFT.md` | 3 | TPLab main / b8c2c91; Cashier total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0 | 2026-10-07 | `DATA_TABLE_MANAGER.md` |
| `doc/DISTRIBUTION_PIPELINE.md` | 3 | de879b9396ddae0523bd3ab86939b679b383dd92 (v0.0.1) | 2026-10-14 | 본문 참조 |
| `doc/GENERIC_POOL.md` | 72 | 901d56c582691d55f4aa9698e91be57231328635 (IMP-01 source) | 2026-10-14 | 본문 참조 |
| `doc/INDEX.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-15 | `api/README.md` |
| `doc/INPUT_SYSTEM_DRAFT.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-07 | `api/Input.md` |
| `doc/OBJECT_POOL.md` | 67 | 901d56c582691d55f4aa9698e91be57231328635 (IMP-01 source) | 2026-10-14 | `GENERIC_POOL.md` |
| `doc/RESOURCE_MANAGER.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-07 | `SCENE_ROOT.md` |
| `doc/SCENE_LOADING.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-07 | 본문 참조 |
| `doc/SCENE_LOADING_PRESENTATION_DRAFT.md` | 3 | 미표기(본문 근거 링크 참조) | 2026-10-14 | `api/SceneManagement.md` |
| `doc/SCENE_ROOT.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-14 | `ASYNC_SCENE_LIFECYCLE.md` |
| `doc/SINGLETON.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-14 | 본문 참조 |
| `doc/TPLAB_NAMING.md` | 본문 참조 | 미표기(본문 근거 링크 참조) | 2026-10-07 | 본문 참조 |

## 줄 번호 링크 보정

전 저장소 추적 Markdown의 상대 #L 링크를 검색했다. 이번56개 대상으로 향하는 기존 링크68개 중 적용 대상에 영향을 주는 것은4개다. URI/commit SHA permalink와 코드 fence·인라인 코드의 과거 인용 문자열은 바꾸지 않았다. 보정 위치는 허용된 가이드06/README뿐이다.

| 참조 문서·원래 줄 | 대상 | 전 → 후 | 라벨 |
|---|---|---|---|
| `doc/guidelines/06-documentation.md:41` | `doc/INPUT_SYSTEM_DRAFT.md` | `L3` → `L8` | `Input:3` → `Input:8` |
| `doc/guidelines/06-documentation.md:41` | `doc/DATA_TABLE_MAPPING_DRAFT.md` | `L3` → `L8` | `Mapping:3` → `Mapping:8` |
| `doc/guidelines/06-documentation.md:50` | `doc/DISTRIBUTION_PIPELINE.md` | `L123` → `L128` | `절차:123` → `절차:128` |
| `doc/guidelines/README.md:62` | `doc/DISTRIBUTION_PIPELINE.md` | `L123` → `L128` | `절차:123` → `절차:128` |

본문 위치를 가리키는 새 머리말 self link23개도 원래 줄+5로 계산했다. 각 원문 행과 삽입 뒤 실제 행이 같은지 확인했다. Markdown 제목은 변경하지 않아 heading anchor는 동일하다. 기존 본문의 #L 링크는 위4곳 외 변경0이다.

## 안전 검사와 전후 비교

적용 전에 `tools/check_pr_rules.py` 전체 판정 경로(97–136), `tools/verify_validation.py`(23–48), `tools/build_distribution.py`의 convert_links/validate_tree/package_tree(96–189), 관련 배포 테스트·입력 수집 도구를 읽었다. 제목 첫 줄/제목 아래 머리말의 기존 형태를 강제하는 검사는 없다. 규칙 검사는 diff의 테스트/메타/보호 경로, 검증기는 hash/결과/링크, 배포 도구는 링크 경로·payload를 확인한다. tools·workflow를 수정하거나 실제 패키징·배포를 수행하지 않았다.

checks16개 폴더의 test-inputs.sha256/preserved-inputs와 Bootstrap의 test-inputs/preserved-user-inputs 선언을 읽었다. 고유 hash 경로836개와 대상56문서 교집합0이며 각 경로의 현재 raw hash/존재 상태가 전후 같다. 과거 입력·기대 hash는 바꾸지 않았다.

verifier 분모는 `tools/verify_validation.py --evidence <checks.json 폴더>`16회 + `doc/validation/bootstrap-system/verify.py`1회다. 변경 전17실패 → 변경 후17실패, 새로운 실패0이다. 종료 코드·stdout·stderr 원문이 전부 같다. 첫 실패는 옛 Assets/MyLab 경로 누락11건과 ISceneRoot source hash 불일치6건이며, 이 결과를 현재 검증 성공으로 해석하지 않는다. 각 verifier는 첫 실패에서 멈추므로 미도달 검사가 모두 정상이라는 뜻도 아니다.

원문/메타 검사에서 적용31개는 머리말5줄만 제거하면 시작 시점 working-tree 본문 raw SHA256이 같고, Git blob도 CRLF→LF 정규화 후 원래 본문과 같다. Bootstrap 문서의 기존 working-tree CRLF와 Git LF 차이는 보존하고 실제 본문 변경과 구분했다. 제외·보류25문서는 raw bytes 동일하다. 가이드 두 파일은4개 링크·D-I3 한 항목씩·README 라벨 표를 역변환하면 원본과 같다. 다른 가이드 파일과 기존 회고/validation 본문은 수정하지 않았다.

06의 D-I3 [제안] 한 항목과 README의 대응 항목은 “분류한 적용 범위 완료” [현행]으로 바꾸고 제외·보류는 이 회고로 연결했다. 전체 문서 통일을 완료했다고 선언하지 않는다. 라벨 재계산:06 **12/21/1 → 13/21/0**, README **24/28/2 → 25/28/1**(현행/결정됨/제안). 다른8개 가이드 행과56칸은 그대로다. 남은 제안은 Timeout 세부 실험/값 결정이다.

## 명령·환경·미실행과 후속

Windows PowerShell·Git·Python3.14를 사용했다. 원시 출력은 작업 소유 `Temp/docs-header-rollout-20261015/`에만 둔다. 대표 명령은 다음과 같다.

```text
git log -1 --format=%cs <base> -- <문서>
C:/Python314/python.exe -B Temp/docs-header-rollout-20261015/verify_doc_links.py --label before/after
C:/Python314/python.exe -B Temp/docs-header-rollout-20261015/audit_headers.py
C:/Python314/python.exe -B tools/verify_validation.py --evidence <16개 폴더>
C:/Python314/python.exe -B doc/validation/bootstrap-system/verify.py
C:/Python314/python.exe -B tools/check_pr_rules.py --base d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f --head HEAD
git diff --check d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f HEAD
C:/Python314/python.exe -B tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <40자리 tip> --output Temp/docs-header-rollout-20261015/ci-final.json
```

링크 검사 기준선은 Markdown254개·상대 링크2016·anchor485·오류0이다. 후속 실제 수치는 아래 검사 확정에 남긴다. 임시 검사기의 이전 실험에서 새 회고가 아직 작성되지 않아 나타난 missing-path는 회고 생성 후 최종 검사에서 없어져야 한다. 정식 검사기/도구를 수정하지 않는다.

미실행: Unity 컴파일·EditMode/PlayMode·제품 Console·Player/IL2CPP·GC·설치/패키징/배포·과거 source 환경 재현. 문서만 추가한 T0이며 실행0을 통과로 쓰지 않는다. main 후속 병합·브랜치 삭제·PC 제어는 하지 않는다. PR 수락 후 임시 명시 목록이 더 이상 필요하지 않을 때 이 작업 소유 파일만 정리하며 다른 Temp·사용자 파일은 보존한다.

사용자 결정 항목: CI_GATE_PROPOSAL의 현행 판정/과거 이력 분류, 수정 금지 가이드·과거 상대 줄 인용 때문에 제외한12문서의 후속 적용 범위다. 기존 역사적 문서12개와 원문 보존 규칙은 유지하며 이번 PR에서 판단을 확대하지 않는다.

## 검사 확정

변경 후 Markdown255개·상대 링크2124개·anchor508개·오류0이다. 기존 heading anchor 변경0·새 링크 오류0, 기존 줄 링크 보정4곳과 새 상태 link23개 원문 행 일치를 확인했다. Label 표의 독립 재계산이 전부 일치했다. 적용31문서의 본문 bytes·제외/보류25문서·보호 hash836경로·기존 dirty10개를 보존했다. 허용 문서는 머리말31개+가이드2개+회고/색인2개의35개이며 그 밖의 추적 파일 bytes 변경0이다. 가이드 예외를 제외한 각 적용 diff는 정확히5줄 추가다.

verifier는 전후17실패·새 실패0이며 full exit/stdout/stderr가 동일하다. 규칙/공백은 실제 커밋으로 확인하고 최종 tip의 exact-SHA Actions를 [작업 브랜치 Actions](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Fdocs-current-headers)에서 대조한다. 결과는 PR과 최종 보고로 연결하며 PR은 사용자 승인 전 open으로 유지한다.

문서 source commit은 `45b82a988be73c41f90d7e59a07150cd98304b1e`다. 실제 커밋35파일의 규칙 검사 오류0·경고0, 공백 검사 오류0, 링크/anchor 오류0을 확인했다. [source Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38700195094)는 completed/success이고 check1/run1, workflows/checks/statuses/runs 조회 완전성 및 SHA 일치를 확인했다. 이 검사 요약만 추가한 최종 tip도 exact-SHA로 다시 확인하고 PR #33·최종 보고에 연결한다. 문서 머리말·본문·검증 입력은 이 증거 기록 단계에서 바꾸지 않았다. 후속 main 병합은 하지 않는다.
