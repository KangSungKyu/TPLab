# 2026-10-06 · Phase 3A 주 씬 교체

- 목표/기준: codex/game-scenes-p3a-flow, track 26f725ddf77c2a27e632f48d1f19b7b678315d13. Additive/Single 선택, 이전/후보 소유권·실패 경계·연속 전환을 구현한다.
- 변경/결정: ReplacePrimaryAsync target/path, WaitForTransitionAsync 및 OwnedScenes를 제공한다. 최초 entry 이력과 현재 작업 수명은 분리한다. Additive는 후보 준비 뒤 이전 종료, Single은 이전 graceful 종료 뒤 native load를 수행한다. [계약](../GAME_SCENE_MANAGER_DRAFT.md#phase-3a-확정-api와-선행-조건)을 구현했고 조건/tree는 섞지 않았다.
- 배정/리뷰: scene_runtime(6.1-sol/high)이 제한 runtime/테스트/fixture를 담당했다. 부모만 Unity를 조작하여 실제 Red 후 Green을 진행하고 최종 diff·소유권·오류 경계를 리뷰했다. track_guidelines(luna/medium)의 독립 최종 수락 문서는 부모가 사용자 변경 소유권 문구를 보완했다.
- 핵심 주의: UniTask 완료는 동기 awaiter를 재개하므로 작업별 local CTS를 완료 통지 전에 정리한다. 실패한 LoadedScene unload의 공유 결과는 terminal Shutdown에서도 실패로 남긴다. 이전 종료가 시작된 뒤의 복귀를 보장하지 않는다.
- 검증: 실제 Red Play 11/0/11/0, focused Green 18/18/0/0, 최종 전체 Edit 213/213/0/0·Play 146/146/0/0. [증거](../validation/scene-replacement/README.md). 추가 경계 7건의 별도 사전 Red는 미실행이다. compile 완료, 예상 로그 보존 후 Console 오류·경고 0, Editor ready, 사용자 dirty 4개·build settings raw bytes 유지. 성공 Player·소비 프로젝트·시각/input UX는 미실행이다.
- Git/다음: 이번 소스·계약·증거·회고만 commit/push 후 원본 commit을 보존해 track에 통합한다. main과 브랜치 삭제는 사용자 최종 확인까지 보류한다. 다음 Phase 3B는 primary/derived 수명 tree, 자식 자기/부모 해제 및 active 정책을 TDD로 확장한다. 진행 중 공통 보호 hash를 Temp/GameScenesTrack에 유지한다.

2026-10-06 통합 보완: bd7698ab0d341dd990efb894804d19963a638693을 track에 FF 통합·push했다. [정확한 원격 CI 조회](../validation/scene-replacement/ci-policy.json)는 미구성이다. main e9fa4e46과 사용자 기존 변경은 유지했다.
