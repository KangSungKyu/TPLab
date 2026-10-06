# GameSceneManager 통합 track 운영

작성일: 2026-10-06. GameSceneManager 후속 구현을 하나의 순차 통합 track에서 운영한다. API·행동 계약은 [GAME_SCENE_MANAGER_DRAFT.md](GAME_SCENE_MANAGER_DRAFT.md)가 소유한다. 이 문서는 작업 배정, 단계 상태·증거, 통합과 종료 gate만 관리한다. 상태는 실제 변경과 현재 checkout의 검증 증거에 맞춰 갱신한다.

## 브랜치와 순서

현재 `codex/game-scenes-p0-guidelines`는 이 운영 지침 작성 단위다. `codex/game-scenes-track`은 main `e9fa4e46f1dc8fe19800800a2229668cb8b4a432`에서 이미 생성됐다. 이 지침 단위의 원본 commit을 track에 보존해 통합한 뒤, 각 단계를 직전 track tip에 순차 통합한다.

```text
main@e9fa4e46 → codex/game-scenes-track
             → p0-guidelines → p0-loaders → p3a-flow → p3b-areas
     → p4-definitions → p5-editor → p6-validation
     → 최종 자동 gate → 명시적 사용자 확인 → main
```

Phase 단위 브랜치는 track에서 분기하고 원본 commit을 보존하는 fast-forward 또는 일반 merge로 통합한다. 다음 Phase는 이전 단계의 실제 코드·데이터·검증 증거가 존재할 때 시작한다. main 병합 시 최신 main을 track에 통합하고 충돌을 해결한 뒤 영향을 받는 검증을 다시 실행한다.

## 소유권과 병렬 작업

부모 에이전트는 요구사항·공용 계약·범위 결정, 배정, 구현 diff와 증거 리뷰, 회고, Git 통합, Unity 조작을 책임진다. 하위 에이전트는 배정된 제한 경로의 구현 또는 독립 조사만 수행하고 Git 통합·Unity 조작·다른 에이전트 재위임을 하지 않는다. 새 사용자 소유 세션을 만들지 않는다.

기본 하위 에이전트 수는 1개이며, 파일과 공용 계약이 겹치지 않고 독립 검증 가능한 작업에만 최대 2개를 쓴다. 같은 파일·공용 API 동시 수정은 금지한다. Unity 테스트 실행 동안 소스를 동결하고, 기존 MyLab Editor는 부모 한 명만 조작한다. 현재 도구의 실제 모델 지원을 확인한 뒤 역할에 맞게 luna/low(검색), luna/medium(작고 확정된 구현), 6.1-sol/medium(통합 구현)을 우선한다. 비동기 수명·소유권 위험은 high로 지정하고 필요하면 추론 수준을 높인다. 별칭이나 모델명으로 성능을 추측하지 않고 선택 근거를 기록한다.

## 단계 상태와 증거

상태 어휘는 `계획(사용자 실행 승인됨)`, `진행`, `선행 검증 기록 있음`, `자동 검증 완료`, `사용자 확인 대기`, `완료`, `차단`이다. 계획이나 과거 검증 기록을 현재 구현·검증 완료로 승격하지 않는다. `선행 검증 기록 있음`은 과거 단계에 증거 문서가 있다는 뜻이며, 현재 track tip에서 재검증했다는 뜻이 아니다. 자동 검증 완료는 해당 Phase의 코드 리뷰·필수 자동 검사·compile/Console 확인 및 회고가 모두 현재 작업에서 실제로 끝났다는 뜻이다. 해당 상태만으로 최종 사용자 확인이나 main 통합을 의미하지 않는다.

| 단계 | 범위 | 현재 상태 | 증거 / gate |
|---|---|---|---|
| P0 guidelines | 운영 규칙과 추적 표 | 진행 | 본 문서 및 `AGENTS.md`; 문서 diff 검사 후 회고 기록 |
| P0 loaders | Addressables/Build loader 명시 선택, 로드 결과와 씬 instance 소유권 | 계획(사용자 실행 승인됨) | 구현 전 현재 계약·사용처 확인. 두 loader 경로와 경계/실패 검증 증거를 단계 완료 시 연결 |
| Phase 1 | SceneTransitionCallbacks 공용화 | 선행 검증 기록 있음 | [검증 기록](validation/scene-transition-contracts/README.md); track 통합본의 현재 검증과 최종 사용자 확인은 별도 |
| Phase 2 | manager 최초 진입과 Bootstrap 위임 | 선행 검증 기록 있음 | [검증 기록](validation/game-scene-entry/README.md); track 통합본의 현재 검증과 최종 사용자 확인은 별도 |
| Phase 3A | Single/Additive 주 흐름 교체 | 계획(사용자 실행 승인됨) | [계약](GAME_SCENE_MANAGER_DRAFT.md)의 Phase 표에 따른 자동 검증·실패 경계 증거 연결 |
| Phase 3B | 수명 tree와 구역 추가/제거 | 계획(사용자 실행 승인됨) | 부모/자식 수명 및 중복 종료 검증 증거 연결 |
| Phase 4 | 정의 asset, ID/직접 요청, root 조건 | 계획(사용자 실행 승인됨) | 공통 경로·무부작용 거부·권한/영향 root 검증 증거 연결 |
| Phase 5 | Inspector, compile 후 Editor, Play/build gate | 계획(사용자 실행 승인됨) | 실제 Build scene 목록 및 설정 오류 차단 증거 연결 |
| Phase 6 | 통합 예제·회귀·소비 프로젝트·Player | 계획(사용자 실행 승인됨) | Bootstrap→주 흐름→구역, 구성·반복 Play·화면/입력 확인 증거 연결 |
| 최종 사용자 gate | 완성된 track의 시각·사용성·실행 확인 | 사용자 확인 대기 | 자동 검증과 분리 기록. 명시적인 인간 확인만 gate를 해소함 |

Phase별 증거에는 기준 commit, 변경 파일, 실제 실행한 검사와 건수·결과, Console/compile 상태, 미실행 항목, 리뷰 및 회고 링크를 남긴다. 사용자 확인 항목은 재현 가능한 실행 절차와 기대 결과를 적는다. 마지막 단계에서 시각·사용성 검사를 모아 수행하되 미확인·미검증 항목을 완료로 표시하지 않는다.

## 에이전트 선택 기록

| 단계/작업 | 역할·모델 / 추론 | 선택 근거 | 허용 경로 | 상태 |
|---|---|---|---|---|
| P0 guidelines | `/root/track_guidelines`, gpt-6-luna / medium | 문서 범위가 확정된 track 운영 규칙 작성 | `AGENTS.md`, `doc/SCENE_TRANSITION_TRACK.md`, `doc/INDEX.md`, `doc/CORE_PLAN.md` | 작성·부모 리뷰 완료 |
| P0 loaders 조사 | `/root/scene_runtime`, gpt-6.1-sol / high | loader 경로·씬 instance 소유권 위험의 읽기 전용 조사 | P0 관련 코드·문서 읽기 전용; 수정 금지 | 조사·계약 검토 완료 |

새 배정은 에이전트가 실제 생성·지원된 뒤 이 표에 기록한다. 기록 필드는 역할/실제 모델·추론, 작업과 선택 이유, 허용 경로, 상태를 포함한다. 비밀값이나 전체 실행 로그는 남기지 않는다.

## 통합 및 최종 종료 gate

Phase 완료 시 부모가 계약과 diff를 리뷰하고, 요구된 자동 검사·컴파일·제품 Console 검증을 확인하고, 해당 단위 회고를 작성한 뒤에만 track에 통합한다. 테스트 0건·미실행은 통과로 기록하지 않는다. 테스트 결과는 통합할 변경을 포함한 commit에 대응해야 한다. 통합 뒤 영향을 받은 필수 검증을 다시 실행한다.

최종 자동 gate와 최종 사용자 확인은 별개다. 사용자의 명시적 확인이 바로 이어지지 않으면 `사용자 확인 대기`로 둔다. 무응답이나 시간 경과로 승인된 것으로 간주하지 않는다. 이 대기 중에는 main 병합·푸시와 브랜치 삭제를 하지 않는다. `main`은 필수 자동 gate와 사용자의 명시적 최종 확인이 모두 충족된 뒤에만 통합한다.

작업이 끝난 뒤 브랜치는 최종 main push와 SHA 보존, 작업 tip의 main 포함 여부를 확인한 다음에만 삭제할 수 있다. 다른 worktree가 쓰는 브랜치는 보류한다. 이번 요청에 한정된 종료 절차는 다음과 같다.

- 구현·자동 검증이 모두 끝나고 최종 사용자 확인만 대기 중이면 track 결과를 보존하고 Unity를 저장한 뒤 절전할 수 있다. 이 경우 Unity Editor는 종료하지 않아 다음 Git diff와 Unity 확인이 가능하게 둔다. main 병합·브랜치 삭제는 보류한다.
- 작업 또는 테스트가 진행 중이면 절전하지 않는다.
- 최종 사용자 확인, main 통합과 정리까지 모두 끝났으면 Unity 저장 후 정상 종료를 확인하고 PC를 종료한다. Unity 종료가 실패하면 오프라인 전환 후 절전한다.
