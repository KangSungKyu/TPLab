# 2026-10-06 · GameSceneManager 작업 트랙 운영 지침

- 목표/기준: 사용자 승인한 남은 씬 작업을 하위 에이전트와 순차 track으로 진행한다. main e9fa4e46f1dc8fe19800800a2229668cb8b4a432에서 codex/game-scenes-track과 codex/game-scenes-p0-guidelines를 생성했다.
- 변경: [AGENTS.md](../../AGENTS.md)와 [track](../SCENE_TRANSITION_TRACK.md)에 Phase 자동 gate, 최종 명시적 사용자 확인, 모델 선택·배정·Unity 단일 실행·소스 동결·최종 브랜치 정리 규칙을 추가했다. 관련 색인/계획도 연결했다.
- 배정/결정: /root/track_guidelines는 gpt-6-luna/medium으로 허용 문서만 작성했고 부모가 리뷰했다. /root/scene_runtime은 gpt-6.1-sol/high로 비동기 소유권 위험의 P0 로더를 읽기 전용 조사했다. 선행 구현 없이 의존 Phase를 병렬 실행하지 않는다.
- 최신 사용자 지시: 자동 구현·검증 완료 후 사용자 확인만 남으면 main 병합·브랜치 삭제를 보류하고 track에 증거를 보존한다. Unity 저장 후 열린 상태로 절전한다. 최종 확인과 main 통합/정리까지 끝나면 Unity 저장·정상 종료를 확인하고 PC 종료, Unity 종료 실패면 오프라인·절전으로 전환한다. 진행 중 작업에는 적용하지 않는다.
- 검증: 허용 문서 diff/공백/범위 확인 완료. Unity 실행 테스트 0건, 문서 지침에는 Red/Green·컴파일·Player 검증 대상 없음. 기존 Unity PID 23176, CLI 0.4.1, 6000.3.18f1, Play/compile/update idle을 실제 확인했다. 사용자 dirty 3개·미추적 설정 1개는 보존하고 baseline SHA256은 Temp/GameScenesTrack/preserved-inputs.json에 기록했다.
- Git: 지침 단위만 commit/push하고 원본 commit을 보존해 track에 통합한다. main은 변경하지 않으며 모든 track/Phase 브랜치는 최종 확인까지 유지한다.
- 다음: 최신 track에서 p0-loaders를 생성하고 SceneTarget/두 loader/개별 LoadedScene 소유권과 Bootstrap 기존 API 호환을 TDD로 구현한다. 회고·증거는 각 Phase 완료 시 추가한다. 최종 사용자 확인은 아직 후보 미완성으로 미도달이다.
