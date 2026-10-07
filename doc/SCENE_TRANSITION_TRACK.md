# GameSceneManager 통합 track 운영

작성일: 2026-10-06. GameSceneManager 후속 구현을 하나의 순차 통합 track에서 운영한다. API·행동 계약은 [GAME_SCENE_MANAGER_DRAFT.md](GAME_SCENE_MANAGER_DRAFT.md)가 소유한다. 이 문서는 작업 배정, 단계 상태·증거, 통합과 종료 gate만 관리한다. 상태는 실제 변경과 현재 checkout의 검증 증거에 맞춰 갱신한다.

## 브랜치와 순서

`codex/game-scenes-track`은 main `e9fa4e46f1dc8fe19800800a2229668cb8b4a432`에서 생성됐다. Editor 검사 commit `de4bb691aed3124443c4bfe3cfd085e3275002a8`까지 track에 fast-forward 통합·push했고 마지막 `codex/game-scenes-p6-validation` 단위의 자동 검증을 완료했다. P6 커밋의 정확한 SHA·push 및 track 통합 결과는 Git 이력과 최종 보고에서 확인한다. 2026-10-07 최종 Unity 확인 답변을 받았다. main 통합·브랜치 정리 결과는 아래 최종 기록과 Git 이력에서 확인한다. 각 단계를 직전 track tip에 순차 통합한다.

```text
main@e9fa4e46 → codex/game-scenes-track
             → p0-guidelines → p0-loaders → p3a-flow → p3b-areas
     → p4-definitions → p5-editor → p6-validation
     → 최종 자동 gate → 명시적 사용자 확인 → main
```

Phase 단위 브랜치는 track에서 분기하고 원본 commit을 보존하는 fast-forward 또는 일반 merge로 통합한다. 다음 Phase는 이전 단계의 실제 코드·데이터·검증 증거가 존재할 때 시작한다. main 병합 시 최신 main을 track에 통합하고 충돌을 해결한 뒤 영향을 받는 검증을 다시 실행한다.

## 소유권과 병렬 작업

부모 에이전트는 요구사항·공용 계약·범위 결정, 배정, 구현 diff와 증거 리뷰, 회고, Git 통합, Unity 조작을 책임진다. 하위 에이전트는 배정된 제한 경로의 구현 또는 독립 조사만 수행하고 Git 통합·Unity 조작·다른 에이전트 재위임을 하지 않는다. 새 사용자 소유 세션을 만들지 않는다.

기본 하위 에이전트 수는 1개이며, 파일과 공용 계약이 겹치지 않고 독립 검증 가능한 작업에만 최대 2개를 쓴다. 같은 파일·공용 API 동시 수정은 금지한다. Unity 테스트 실행 동안 소스를 동결하고, 기존 TPLab Editor는 부모 한 명만 조작한다. 현재 도구의 실제 모델 지원을 확인한 뒤 역할에 맞게 luna/low(검색), luna/medium(작고 확정된 구현), 6.1-sol/medium(통합 구현)을 우선한다. 비동기 수명·소유권 위험은 high로 지정하고 필요하면 추론 수준을 높인다. 별칭이나 모델명으로 성능을 추측하지 않고 선택 근거를 기록한다.

## 단계 상태와 증거

상태 어휘는 `계획(사용자 실행 승인됨)`, `진행`, `선행 검증 기록 있음`, `자동 검증 완료`, `사용자 확인 대기`, `완료`, `차단`이다. 계획이나 과거 검증 기록을 현재 구현·검증 완료로 승격하지 않는다. `선행 검증 기록 있음`은 과거 단계에 증거 문서가 있다는 뜻이며, 현재 track tip에서 재검증했다는 뜻이 아니다. 자동 검증 완료는 해당 Phase의 코드 리뷰·필수 자동 검사·compile/Console 확인 및 회고가 모두 현재 작업에서 실제로 끝났다는 뜻이다. 해당 상태만으로 최종 사용자 확인이나 main 통합을 의미하지 않는다.

| 단계 | 범위 | 현재 상태 | 증거 / gate |
|---|---|---|---|
| P0 guidelines | 운영 규칙과 추적 표 | 완료 | 문서 diff 검사·[회고](retrospectives/2026-10-06-16-game-scenes-track-guidelines.md), track push 완료 |
| P0 loaders | Addressables/Build loader 명시 선택, 로드 결과와 씬 instance 소유권 | 자동 검증 완료 | [계약](SCENE_LOADING.md), [검증](validation/scene-loaders/README.md), [회고](retrospectives/2026-10-06-17-scene-loaders.md). 전체 Edit 213/213·Play 128/128, 26f725d track FF/push 완료. [정확한 commit CI 조회](validation/scene-loaders/ci-policy.json): 미구성, CI 성공 아님 |
| Phase 1 | SceneTransitionCallbacks 공용화 | 선행 검증 기록 있음 | [검증 기록](validation/scene-transition-contracts/README.md); track 통합본의 현재 검증과 최종 사용자 확인은 별도 |
| Phase 2 | manager 최초 진입과 Bootstrap 위임 | 선행 검증 기록 있음 | [검증 기록](validation/game-scene-entry/README.md); track 통합본의 현재 검증과 최종 사용자 확인은 별도 |
| Phase 3A | Single/Additive 주 흐름 교체 | 자동 검증 완료 | [검증](validation/scene-replacement/README.md), [회고](retrospectives/2026-10-06-18-scene-replacement.md). Edit 213/213·Play 146/146, bd7698a track FF/push 완료. [정확한 commit CI 조회](validation/scene-replacement/ci-policy.json): 미구성 |
| Phase 3B | 수명 tree와 구역 추가/제거 | 자동 검증 완료 | [검증](validation/scene-areas/README.md), [회고](retrospectives/2026-10-06-19-scene-areas.md). Edit 213/213·Play 173/173, 5410789 track FF/push 완료. [정확한 CI 조회](validation/scene-areas/ci-policy.json): 미구성 |
| Phase 4 | 정의 asset, ID/직접 요청, root 조건 | 자동 검증 완료 | [검증](validation/scene-definitions/README.md), [회고](retrospectives/2026-10-06-20-scene-definitions.md). Green Edit15/15·Play26/26, 전체 Edit228/228·Play199/199; 5eda919 track FF/push 완료. [정확한 CI 조회](validation/scene-definitions/ci-policy.json): 미구성 |
| Phase 5 | Inspector, compile 후 Editor, Play/build gate | 자동 검증 완료 | [검증](validation/scene-transition-editor/README.md), [회고](retrospectives/2026-10-06-21-scene-transition-editor.md). Green12/12·전체 Edit240/240·Play199/199, actual build/Play 거부; de4bb69 track FF/push 완료. [정확한 CI 조회](validation/scene-transition-editor/ci-policy.json): 미구성 |
| Phase 6 | 통합 예제·회귀·소비 프로젝트·Player | 자동 검증 완료 | [검증](validation/scene-integration/README.md), [회고](retrospectives/2026-10-06-22-scene-integration.md). 전체 Edit240/240·Play201/201, 원래 Editor Windows Player 각10관찰, Reload8/8·소비 Player11관찰 |
| 최종 사용자 gate | 완성된 track의 시각·사용성·실행 확인 | 완료 | 2026-10-07 사용자가 Unity 실행 항목까지 확인 완료. [수락 기록](validation/scene-integration/final-integration/acceptance.json), [실행 절차](SCENE_TRANSITION_ACCEPTANCE.md) |

Phase별 증거에는 기준 commit, 변경 파일, 실제 실행한 검사와 건수·결과, Console/compile 상태, 미실행 항목, 리뷰 및 회고 링크를 남긴다. 사용자 확인 항목은 재현 가능한 실행 절차와 기대 결과를 적는다. 마지막 단계에서 시각·사용성 검사를 모아 수행하되 미확인·미검증 항목을 완료로 표시하지 않는다.

## 에이전트 선택 기록

| 단계/작업 | 역할·모델 / 추론 | 선택 근거 | 허용 경로 | 상태 |
|---|---|---|---|---|
| P0 guidelines | `/root/track_guidelines`, gpt-6-luna / medium | 문서 범위가 확정된 track 운영 규칙 작성 | `AGENTS.md`, `doc/SCENE_TRANSITION_TRACK.md`, `doc/INDEX.md`, `doc/CORE_PLAN.md` | 작성·부모 리뷰 완료 |
| P0 loaders 조사 | `/root/scene_runtime`, gpt-6.1-sol / high | loader 경로·씬 instance 소유권 위험의 읽기 전용 조사 | P0 관련 코드·문서 읽기 전용; 수정 금지 | 조사·계약 검토 완료 |
| P0 loaders 구현 | `/root/scene_runtime`, gpt-6.1-sol / high | 네이티브 비동기 완료·개별 씬 해제와 기존 Bootstrap 호환 | Core ResourceManagement/SceneManagement, 관련 Tests; Editor·Git·Unity 제외 | Green·최종 runtime 읽기 리뷰 완료 |
| P0 Editor 구현 | `/root/track_guidelines`, gpt-6-luna / medium | source/SceneAsset Inspector와 등록·catalog 사전 검사 | Editor Bootstrap/asmdef 및 BootstrapSceneSourceTests; runtime·Git·Unity 제외 | Green 13/13·부모 리뷰 완료 |
| Phase 3A 구현 | `/root/scene_runtime`, gpt-6.1-sol / high | 교체 중 두 씬 소유권·Single graceful 종료·공유 취소의 비동기 위험 | GameSceneManager 및 관련 테스트·fixture; Editor·docs·Git·Unity 제외 | Red 11/0/11·Green 18/18, 최종 읽기 리뷰 완료 |
| Phase 3B 구현 | `/root/scene_runtime`, gpt-6.1-sol / high | tree 수명·active 선택·권한·공유 제거 및 중복 종료의 비동기 위험 | GameSceneManager/SceneRegistration 및 관련 테스트·fixture; Editor·docs·Git·Unity 제외 | Red 18/0/18·보완 Red 1/0/1·Green 27/27, 최종 리뷰 완료 |
| Phase 4 구현 | `/root/scene_runtime`, gpt-6.1-sol / high | 모든 요청의 조건 우회 방지·해제 전 재검사·비동기 취소 경계 | Core SceneManagement 및 관련 Tests/meta; Editor·docs·tools·Git·Unity 제외 | Red·getter/reveal 보완 Red 확인, Green15/15·26/26, readonly 최종 리뷰 완료 |
| Phase 4/5 사전 조사 | `/root/track_guidelines`, gpt-6-luna / medium | 기존 root/validator 재사용과 최소 정의·조건 API 검토 | 관련 코드·명세 읽기 전용; 수정 없음 | 조사 완료; 실행·수정 0건 |
| 최종 수락 기준 문서 | `/root/track_guidelines`, gpt-6-luna / medium | 자동 gate와 마지막 사용자 확인 묶음의 독립 문서 | `doc/SCENE_TRANSITION_ACCEPTANCE.md`만 수정; Git·Unity 제외 | 실제 P6 메뉴/버튼/복구 경로 반영·부모 리뷰 완료; 인간 확인 대기 |
| P0 Editor API 조사 | `/root/track_guidelines`, gpt-6-luna / medium | 설치 Addressables의 등록 API·preview gate·Player 모듈 확인 | 설치 패키지·Editor·Tests 읽기 전용 | 조사 완료; 실행 테스트 0건 |
| Phase 5 경로 조사 | `/root/track_guidelines`, gpt-6-luna / medium | 정의/조건 설정을 기존 Inspector·compile/Play/build 검사에 연결하는 최소 경로 확인 | 관련 코드·확정 계약 읽기 전용; 수정·Unity·Git 제외 | 조사 완료; 수정·실행 0건 |
| Phase 6 소비 프로젝트 조사 | `/root/track_guidelines`, gpt-6-luna / medium | 최소 dependency·파일 allowlist와 원래 Editor·반복 Play 경로 조사 | Core/의존성/Validation 관련 읽기 전용; 수정·복사·Unity·Git 제외 | 조사 완료; 최소 Core/CsvHelper + UniTask/Addressables, 실행 0건 |

| Phase 5 Editor 구현 | `/root/track_guidelines`, gpt-6-luna / medium | 확정 설정·조건 메타데이터를 기존 사전 검사에 연결 | Editor/Bootstrap 및 SceneTransitionEditorTests/EditorConditionProbe/meta; Core·Unity·Git 제외 | 초기·보완/corrected Red, 최종 Green12/12·회귀240/240·199/199·부모 리뷰 완료 |
| Phase 6 반복 Play/샘플 조사 | `/root/scene_runtime`, gpt-6.1-sol / high | 비동기 manager의 reload 수명과 UI 입력 소유권 검토 | 관련 코드 읽기 전용; 수정·Unity·Git 제외 | 조사 완료; 실행0 |

새 배정은 에이전트가 실제 생성·지원된 뒤 이 표에 기록한다. 기록 필드는 역할/실제 모델·추론, 작업과 선택 이유, 허용 경로, 상태를 포함한다. 비밀값이나 전체 실행 로그는 남기지 않는다.

## 통합 및 최종 종료 gate

Phase 완료 시 부모가 계약과 diff를 리뷰하고, 요구된 자동 검사·컴파일·제품 Console 검증을 확인하고, 해당 단위 회고를 작성한 뒤에만 track에 통합한다. 테스트 0건·미실행은 통과로 기록하지 않는다. 테스트 결과는 통합할 변경을 포함한 commit에 대응해야 한다. 통합 뒤 영향을 받은 필수 검증을 다시 실행한다.

최종 자동 gate와 최종 사용자 확인은 별개다. 사용자의 명시적 확인이 바로 이어지지 않으면 `사용자 확인 대기`로 둔다. 무응답이나 시간 경과로 승인된 것으로 간주하지 않는다. 이 대기 중에는 main 병합·푸시와 브랜치 삭제를 하지 않는다. `main`은 필수 자동 gate와 사용자의 명시적 최종 확인이 모두 충족된 뒤에만 통합한다.

작업이 끝난 뒤 브랜치는 최종 main push와 SHA 보존, 작업 tip의 main 포함 여부를 확인한 다음에만 삭제할 수 있다. 다른 worktree가 쓰는 브랜치는 보류한다. 이번 요청에 한정된 종료 절차는 다음과 같다.

- 구현·자동 검증이 모두 끝나고 최종 사용자 확인만 대기 중이면 track 결과를 보존하고 Unity를 저장한 뒤 절전할 수 있다. 이 경우 Unity Editor는 종료하지 않아 다음 Git diff와 Unity 확인이 가능하게 둔다. main 병합·브랜치 삭제는 보류한다.
- 작업 또는 테스트가 진행 중이면 절전하지 않는다.
- 최종 사용자 확인, main 통합과 정리까지 모두 끝났으면 Unity 저장 후 정상 종료를 확인하고 PC를 종료한다. Unity 종료가 실패하면 오프라인 전환 후 절전한다.

Phase 5 최종 읽기 리뷰: scene_runtime(6.1-sol/high)이 exact default config·live root·legacy Inspector 보존 수정 후 테스트 callback 누수를 발견했다. 테스트 소유 callback만 해제·원래 callback 유지 보완을 부모가 리뷰하고 전체 회귀를 다시 통과했다.

| Phase 6 reload Red harness | `/root/scene_runtime`, gpt-6.1-sol / high | Domain/Scene Reload 4×2 actual entry/종료와 manager 재사용 결함 관찰 | Validation/Editor/BootstrapReloadCheck 및 새 dedicated callback fixture/meta만; Core·Unity·Git 제외 | observed8/8 성공·원래 설정/보호5 복원. 잘못된 fixture 가정 2회 기록; 제품 결함 Red 아님, Core reset 미추가 |
| Phase 6 소비 프로젝트 도구 | `/root/track_guidelines`, gpt-6-luna / medium | 확정 allowlist·별도 소비 batch import/build/실행의 독립 도구 | tools/run_core_consumer.py 및 tools/core-consumer/**만; Assets·복사·Unity·Git 제외 | 도구 selfcheck·실제 두 번째 소비 build/Player 성공(2actual runs/11observables), 첫 driver 로그 gate 실패·Player0 별도 보존; 원본/복사86hash+로그 읽기 리뷰 완료 |

Phase 6 통합 sample: scene_runtime(6.1-sol/high)이 별도 Samples Runtime/Editor·own 검증 helper 및 sample PlayMode tests/필요 assembly 참조를 맡는다. 준비·조건·UI/입력 소유권의 영향으로 high를 유지한다. Core·패키지·기존 사용자 자산·Unity·Git 변경은 제외한다. NEW UI의 observable Red를 부모가 확인한 뒤 Green/scene builder/player smoke로 진행한다.

Phase 6 sample 최소 입력 policy는 실제 Red2/0/2→Green2/2를 확인했다. 설치 NUnit API 및 NEW meta 형식 오류는 compile/discovery 실패로 별도 기록한다. 샘플/빌드 helper와 자동 gate를 완료했다. main gate는 사용자의 최종 확인을 기다린다.

P6 최종 결과: sample native Editor smoke Additive/Single 각10관찰, 원래 Editor Windows Mono build 두 모드 성공 및 Player 각10관찰을 확인했다. actual asset 생성 실패는 NewScene 후 파괴된 settings wrapper를 경로로 재로드하여 보완했다. background Editor delayCall 복구 대신 idle update 1회 복구를 사용하고 실제 정상 Play stop의 원래 InitScene 복구를 확인했다. 최초 투명 modal의 텍스트 겹침은 완전 불투명 cover/modal로 수정하고 실제 최종 화면을 재확인했다. 원래 사용자의 raw5와 Player 설정을 보존하고 빌드 소유 cache 변경을 복원했다. final source hash·GUID·Console/ready 확인과 회고는 통합 증거에 연결한다. 인간 화면비·물리 입력·사용성은 미확인이다.

| P6 sample 구현·최종 읽기 리뷰 | `/root/scene_runtime`, gpt-6.1-sol / high | 입력/비동기 root·UI 수명과 native smoke 검토 | 별도 Samples/관련 dedicated tests·fixture/Validation helper; Core·Unity·Git 제외 | 구현 완료·소스 동결, 부모 actual Unity/Player/전체 회귀 검증 완료 |

P6 소스·테스트·영속 증거 commit `76939ba678d217f7e4787122a14139e73e4c3a51`을 Phase 브랜치에 push했다. [정확한 commit CI 조회](validation/scene-integration/ci-policy.json)는 main unprotected/CI 미구성이며 CI 성공으로 기록하지 않는다. 이후 CI·기록만 추가한 tip은 같은 source hash로 verifier를 통과한 뒤 track에 FF 통합한다. main `e9fa4e46f1dc8fe19800800a2229668cb8b4a432`와 사용자 변경은 유지한다.


2026-10-07 최종 통합: 사용자 Unity 확인 후 7fae48093253b7710d9447b91322c22de2a1e1e1로 main FF/push를 실제 완료하고 local main=origin/main을 확인했다. 원래 코드/예제/테스트 입력283개와 보호 bytes는 동일하다. 제품 Console0, 현재 Editor PID52320 ready/저장 확인. 현재 단위는 최종 문서 기록과 승인된 track/Phase8개의 exact-tip 정리·정상 종료다. 기타 작업 브랜치는 보존한다. 최종 기록 commit SHA와 실제 삭제 결과는 Git 이력·최종 보고를 따른다.
