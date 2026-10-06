# 2026-10-06 · SceneTransition Phase 1

- 목표/범위: 합의한 전환 계약과 Phase를 문서화하고 Bootstrap callback을 공용화한다. GameSceneManager runtime은 Phase 2부터 순차 구현한다.
- 기준: main/0610ff596f11c23ec0dd96741728b349337ec605 → codex/scene-transition-contracts. 기존 사용자 변경 4개와 build list는 [raw hash](../validation/scene-transition-contracts/preserved-inputs.json)로 보존한다.
- 결정: SceneTransitionCallbacks/ConfigureSceneAsync를 새 기본 계약으로 사용하고 기존 BootstrapCallbacks/ConfigureGameAsync는 Obsolete adapter로 연결한다. 기존 meta/GUID와 serialized 필드 이름을 유지하며 소비 assembly 재컴파일이 필요하다.
- 계약: Single/Additive 선택·Bootstrap 씬/영속 root 수명·경로 graph/실행 수명 tree·부모의 파생 씬 책임·manager 실행 소유권·조건/준비 신호·대기/작업 취소 분리·실패 상태/UI 계약을 [단일 본문](../GAME_SCENE_MANAGER_DRAFT.md)에 반영한다. 구체 manager API/구역/조건 runtime은 아직 없다.
- 변경: 공용 callback과 compatibility adapter, Bootstrap 주입/호출 타입, 새 공유 callback probe 및 준비 gate PlayMode 1건, 씬 참조 저장/재로드 EditMode 2건, 관련 문서·색인·증거·재사용 Python 검증 도구.
- 문제/교훈: 기존 MyLab PID는 살아 있었지만 최초 CLI discovery가 실패했다. heartbeat/path를 읽기 전용으로 대조한 뒤 같은 Editor의 CLI ready를 확인했다. 새 Editor나 다른 프로젝트로 대체하지 않았다. 공유 타입 주입의 Red는 실제 ArgumentException이며 컴파일 실패를 Red로 대신하지 않았다.
- 검증: 실제 Red 1/0/1/0, Bootstrap Green 10/10/0/0, 최종 Edit 178/178/0/0·Play 97/97/0/0. 컴파일 완료/Editor ready, 예상 테스트 로그 보존 뒤 새 Console 오류·경고 0. [증거](../validation/scene-transition-contracts/README.md).
- 미실행: Player·소비 프로젝트·새 UI 시각 검증·Reload 비활성 반복 Play·Single/graph/조건 runtime. CI는 미구성이다. Phase 1의 통과를 이후 시스템 완료로 확대하지 않는다.
- Git: 시작 전 checkout/dirty/upstream 확인, origin/main 기준 작업 브랜치 생성. 이번 파일만 commit/push하고 자동 검증·원격 gate·범위 검사 후 승인 조건에 따라 main 통합한다. 실제 최종 commit/원격 일치는 Git 이력과 최종 보고가 소유한다.
- 임시 파일: Temp/SceneTransitionContracts의 문서 갱신/기록 스크립트와 CLI transcript를 증거 보존 후 명시 목록으로 제거한다. 반복 검증은 tools/verify_validation.py, 원격 gate 확인은 tools/check_github_ci.py로 옮기고 입력·출력을 인수로 분리했다. baseline API/정적 검증의 실제 실행으로 최소 확인한다.
- 다음: Phase 2에서 GameSceneManager의 최초 진입 소유권, Bootstrap 위임, Single/Additive·공용 수명 선택·실패 조회/종료를 최소 테스트부터 구현한다. 현재 Bootstrap persistence/모드 검사는 그대로이며 그 변경이 Phase 2의 선행 과제다.
