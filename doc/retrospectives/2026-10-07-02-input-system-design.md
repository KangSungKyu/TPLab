# 2026-10-07 · Input System 전용 정책·wrapper 설계

- 목표: 사용자가 확정한 Input System 전용 지원을 지침에 기록하고, 공용 wrapper 설계를 작성한다. 구현 요청으로 확대하지 않는다.
- 기준: main `8cf3f226ae5e57f037c199fc49f48a14fd299835`; 원격 fetch 후 main/origin/main ahead0·behind0 확인. 신규 `codex/input-system-design`에서 문서만 변경한다. 기존 사용자 변경4개는 범위 밖으로 보존한다.
- 선행: [씬 통합 회고](2026-10-07-01-scene-track-integration.md), [SceneRoot](../SCENE_ROOT.md), [전환 callback](../../Assets/MyLab/Core/SceneManagement/SceneTransitionCallbacks.cs), 기존 Samples의 clone·modal/transition 분리 입력 처리를 대조했다.
- 결정: [INPUT_SYSTEM_DRAFT.md](../INPUT_SYSTEM_DRAFT.md)에 native API 재사용, 별도 입력 assembly, clone의 단일 활성화 소유자, lease 현재 상태 재계산, candidate 검증 후 binding commit, 프로젝트 저장/UI/충돌 정책 경계를 제안했다. Legacy backend는 지원하지 않는 확정 정책이며 API·Phase·기본 정책은 초안이다.
- 배운 점: Action Map과 UI 이벤트 우선순위는 다른 책임이다. UI module의 자동 Enable과 직접 장치 읽기를 무시하면 layer가 입력 차단을 보장하지 못한다. UI adapter와 관리 범위의 한계를 명시했다.
- 변경: AGENTS.md 정책, README·CORE_PLAN·문서/회고 색인, 설계 초안과 이 회고. runtime/asset/의존성/ProjectSettings 변경 없음.
- 검증: 문서7개 diff/공백·상대 링크168개·색인 등록·변경 allowlist를 검사해 통과했다. 사용자 변경4개의 SHA-256 일치, 예상 외 변경0개, runtime 변경0개를 확인했다. 새 Unity 테스트 실행0건, 컴파일·Console·PlayMode·Player 미실행이다. 실행 동작 변경이 없는 문서 작업이므로 Red/Green이나 과거 테스트를 이번 통과로 기록하지 않는다.
- Git: 이 단위의 문서 검사 뒤 승인된 일반 Git 관리·문서 병합 규칙에 따라 commit/push/main 반영·작업 브랜치 정리를 진행한다. 최종 SHA/실제 수행은 Git 이력과 최종 보고를 따른다.
- 상태: 정책 문서 반영·설계 초안 작성 완료, 입력 wrapper 미구현. 다음은 사용자의 설계 검토·구현 요청 후 단계1부터 실제 TDD로 진행한다. 키 충돌 기본 정책·UI adapter 범위·지원 버전 확대는 승인된 구현 범위에서 확인한다.
