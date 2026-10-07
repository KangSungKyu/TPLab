# 2026-10-07 · 진행률·로딩 화면·진행 대기 설계

- 목표: 가림막-only 전환에 진행률·팁 UI 및 자동/버튼 대기 단계를 추가하는 설계다. 사용자 정정에 따라 별도 Unity 로딩 씬은 범위에서 제외한다.
- 기준: main `f8aae132179610103db882c7fcfc01374bbfb157`; fetch 후 main/origin/main ahead0·behind0. 신규 `codex/scene-loading-presentation-design`에서 문서만 작성하고 기존 사용자 변경4개를 보존한다.
- 선행: [씬 최종 통합](2026-10-07-01-scene-track-integration.md), [입력 wrapper 초안](2026-10-07-02-input-system-design.md), 현재 GameSceneManager의 inventory·후보 준비/이전 root 해제·SceneRootFlow cover 복구와 두 loader를 대조했다.
- 변경·결정: [설계 초안](../SCENE_LOADING_PRESENTATION_DRAFT.md)에 기존 callback 확장·로딩 화면 공개와 최종 reveal 분리·준비/진행 신호 분리·OperationId·선택적 progress 경계·Additive 지연 release와 Single 기존 규칙을 기록했다. CORE_PLAN·GameSceneManager/loader 명세·문서/회고 색인으로 연결한다.
- 문제·배운 점: 기존 HideCoverAsync는 입력 차단을 해제하므로 중간 reveal에 쓸 수 없다. 로딩 화면이 열린 상태의 실패는 기존 최종 reveal 복구만으로 보호되지 않는다. 진행률100%·씬 activation·root 준비·게임 허용을 분리해야 한다.
- 검증: 문서7개의 diff/공백·상대 링크163개·색인 등록·변경 allowlist를 검사해 통과했다. 사용자 변경4개 SHA-256 일치와 예상 외 변경0개를 확인했다. runtime 변경0개·새 Unity 테스트 실행0건이다. 컴파일·Console·PlayMode·Player 미실행이며 과거 통과를 새 동작 검증으로 쓰지 않는다.
- Git: 문서 검사 뒤 승인된 일반 문서 Git 관리·병합 규칙으로 commit/push/main 반영·이번 브랜치만 정리한다. 실제 SHA와 수행 결과는 Git 이력·최종 보고를 따른다.
- 상태: 설계 초안 작성 완료, 진행률/로딩 UI/대기 및 입력 wrapper 미구현. 다음은 설계 검토·사용자 구현 요청 후 단계별 TDD다. 두 설계의 통합 runtime 완료를 이번 문서 병합으로 주장하지 않는다.
