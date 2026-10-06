# 2026-10-06 · GameSceneManager Phase 2

- 목표/범위: 승인된 다음 단위로 최초 진입의 manager 소유권·Bootstrap 위임, Single/Additive·공용 수명 선택, 취소/실패/종료 상태를 구현한다. 연속 교체와 구역/조건은 확장하지 않는다.
- 기준: main/ab8d3ff3c3ccc78f3636260f2def7f177a978763 → codex/game-scene-entry. [선행 Phase 1](2026-10-06-13-scene-transition-contracts.md)의 계약과 실제 checkout을 대조했다. 기존 사용자 변경 4개와 build list를 raw bytes로 보존한다.
- 결정/변경: 순수 C# GameSceneManager를 Bootstrap이 동기 설치 후 최초 명령에서 생성한다. 준비/로드/unload 소유권은 manager에 모으고 기존 SceneRootFlow와 root 공유 종료를 재사용한다. 공용 persistence는 기존 root 설정을 사용한다. 새 `_loadMode`의 기본값은 Additive이며 기존 callback/serialized 필드/GUID를 보존한다.
- 계약: 실제 Single은 영속 root·Bootstrap/callback 생존과 기존 Bootstrap 하나의 씬 집합을 요구한다. caller 대기와 owner 취소를 분리한다. 준비·reveal·active/씬 집합 검사 이후 진행을 허용한다. 마지막 Single 씬 해제 실패는 잔여 씬/준비 상태/원인으로 보고하며 코어가 빈 씬을 생성하지 않는다. 실패 뒤 외부 씬이 사용 가능하면 명시적 종료로 잔여 정리를 완료할 수 있다. [단일 runtime 본문](../BOOTSTRAP_SYSTEM.md).
- 문제/해결: Scene handle 기본 정렬 불가를 HashSet<Scene> 집합 비교로 수정했다. Single이 이미 active로 지정한 씬은 다시 설정하지 않는다. OnDestroy가 이미 요청한 failed shutdown을 재관찰해 중복 로그를 내던 경로를 차단했다. 최종 리뷰에서 active scene 단독 외부 변경의 검사를 추가했다. 공용 flow의 root 호출 adapter와 게임 root 준비/해제 경계에서 동기 재진입을 검사해 비동기 cover 뒤 누락을 해결했다. 소유 씬 밖으로 이동한 게임 root도 준비로 처리되던 Red를 확인하고 원래 root의 서비스 정리까지 포함했다. 빌린 공용 root가 후보 씬으로 이동하면 언로드로 파괴하지 않고 오류를 보고한다.
- 경계/학습: 새 entry 명령과 동기 hook/common-installer 재진입을 거부한다. await 이후 자기 Bootstrap 공유 대기의 순환은 자동 검출하지 못해 명시적 금지 계약을 남겼다. 전체 외부 await 공유를 막거나 task-context framework를 추가하지 않았다. common installer는 최초 진입을 기다리지 않는다.
- 검증: 실제 Red Edit 1/0/1/0·Play 1/0/1/0·비동기 cover 경계 Play 1/0/1/0·이동 root Play 1/0/1/0. focused 설정 28/28/0/0·Bootstrap 21/21/0/0·entry 7/7/0/0. 최종 Edit 182/182/0/0·Play 115/115/0/0. 기존 동일 PID의 컴파일/ready, 예상 테스트 로그 10개 보존 후 새 Console 오류/경고 0, 실제 invalid Single BuildPipeline/Play 차단과 fixture 제거. [증거](../validation/game-scene-entry/README.md).
- 미실행/위험: 성공 Player·소비 프로젝트·실제 cover/input UX·Reload 비활성 반복 Play, 연속 교체/구역/조건. native gate의 Unknown은 실패 전 검사 중단이며 Player 성공이 아니다. CI 미구성은 별도 기록한다.
- 도구/임시 파일: 반복 CLI domain reload 단절을 tools/run_unity_tests.py에서 이번 CLI PID·실행 시간으로만 복원하게 했다. 전체 Edit/Play와 parser self-check를 실행했다. 기존 BootstrapEditorCheck는 현재 evidence 경로·invalid Single 입력을 받게 했다. Temp/GameSceneEntry의 script/transcript/중간 JSON은 증거를 보존하고 명시 목록으로 제거한다.
- Git: 원격 main 기준 작업 브랜치 생성, 사용자 변경과 분리한 이번 파일만 commit/push한다. 필수 자동 검사·원격 gate·diff 확인 후 승인 조건에 따라 main을 병합·푸시한다. 최종 commit/원격 일치는 Git 이력과 최종 보고에서 확인한다.
- 다음: Phase 3A에서 Single/Additive 연속 주 흐름 교체와 이전 게임 root graceful release, 실패 시 보존/복귀 가능 경계, 늦은 완료와 active 선택을 구현한다. 현재 첫 진입 source·검증과 ownership API를 선행 입력으로 사용한다.
