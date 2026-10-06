# 2026-10-06 · GameSceneManager Bootstrap Additive 권장안

## 작업과 기준

- 요청: 기본 씬 사양을 유지하고 Bootstrap/Additive 시작을 코어 권장안으로 제공.
- 선택 확인: 사용자는 Bootstrap을 첫 씬으로 실행하고 게임 씬을 Additive로 추가 로드하는 방식을 선택했다. 시작 게임 씬에서 Bootstrap을 나중에 붙이는 방식은 이번 기본 진입안에 포함하지 않았다.
- 기준 main/0d22ed343ce1e58a235ccb4a61fb7ef3541402fa → codex/bootstrap-additive-design. 상태: 권장 구조·문서 완료, 전환 상세 설계/runtime 미구현.
- 선행: [importer 회고](2026-10-06-10-data-table-importer.md), [새 설계](../GAME_SCENE_MANAGER_DRAFT.md), [기존 root](../SCENE_ROOT.md)/[flow](../ASYNC_SCENE_LIFECYCLE.md).

## 결정과 이유

- Bootstrap은 공용 서비스와 전환 수명을 소유하고 게임 씬은 씬별 자산/서비스를 소유한다. 유지되는 Bootstrap의 SceneOwnedRoot를 우선 권장하되 Singleton 접근 선택은 보존한다. 별도 DontDestroyOnLoad는 이 경로의 기본값으로 추가하지 않는다.
- Bootstrap 공용 준비 → 게임 씬 Additive → 목적지 주입/준비 → 진행/가림막 해제로 권장 시작을 정했다. 게임 전환에서 Bootstrap.Shutdown/ReleaseAndProceed를 호출하면 기존 API상 공용 manager가 종료되므로 명시적으로 제외했다.
- Unity activation·SetActiveScene·게임 시작을 분리하고 active scene의 기본 생성 객체 소속, 취소한 네이티브 로드의 늦은 완료 정리를 구현 전 경계로 남겼다. Single 전환·자동 Bootstrap 생성·streaming 시스템을 함께 구현하지 않았다.
- 기본 사양/권장안만 문서화했다. 씬·Build Profile·runtime·패키지·Editor UI는 변경하지 않았다. 정확한 public API·취소/commit 경계를 확정하기 전 구현으로 확대하지 않았다.
- Git: 문서 작업 브랜치 생성. 문서 전용 정적 검증 후 승인 정책으로 커밋·푸시·main 통합한다. 실제 최종 커밋/통합 결과는 Git 이력과 최종 보고에서 확인한다.

## 검증과 한계

- [정적 검사](../validation/bootstrap-additive-design.json): 문서 상대 링크/anchor·색인·diff/공백·허용 범위와 작업 전 사용자 자산 hash 보존.
- 테스트 실행 0건, 실패/skip 집계 해당 없음. 문서만 변경하여 Unity 테스트·컴파일·Console·실행·Player 검증은 미실행이며 과거 테스트 통과를 이번 실행으로 쓰지 않았다.
- InitScene, DefaultVolumeProfile, Mobile_RPAsset의 기존 변경과 미추적 SceneTemplateSettings는 보존하고 커밋에서 제외했다.

## 다음 작업

- GameSceneManager의 실제 입력·결과·씬 소유권, 재진입·native 취소/늦은 완료·부분 실패 경계를 확정한다. Bootstrap이 유지되는 실제 additive 씬 최소 fixture로 TDD 구현한다.
- Editor에서 게임 씬을 직접 Play하는 선택적 도우미·소비 프로젝트 통합·Player/시각 UX는 후속 요구와 실행 증거가 필요하다.
