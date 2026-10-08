# UIContext P0 계약·의존성

- 날짜/상태: 2026-10-08 / P0 완료 / 계약·정적 검증, runtime 미구현.
- 기준: main8ce768d, 승인된 설계852ee960, codex/game-ui-track → codex/game-ui-p0-contracts. 원격 main도8ce768d로 조회했으며 사용자dirty6개 raw hash를 보존한다.
- 범위/변경: [계약](../GAME_UI_SYSTEM.md), [track](../GAME_UI_SYSTEM_TRACK.md), [P0 자료](../validation/ui-system/p0/README.md). 새 UI source는 아직 추가하지 않고 기존 Core/Input/자산/설정을 이동·재직렬화하지 않는다.
- 결정: UIContext의 root 수명·borrowed services와 소유 clone/표시 cleanup 분리, BeginOpen 즉시 handle와 Opened await/OpenAsync 동일경로, root Shutdown/파괴 fallback, Input 선택 경계, 기본 UI→Core/UniTask/uGUI 참조.
- 분업: 기존 ui_lifecycle_review는 계약/async 위험을 읽기 전용 검토; 신규 ui_dependency_audit(gpt-6-luna/low)는 실제 asmdef/설치 의존성 조사 완료, 파일/Unity/Git 수정·재위임 없음.
- Editor: 원본PID24376 준비·compile/updating/play false·InitScene dirty false·Console0. CLI project discovery는 확장 권한에서 같은 경로의 실제 ready 상태를 확인했으며 원본 Editor를 바꾸지 않았다.
- 검증: 문서9개·로컬 링크230개 누락0·공백0, 보호hash6개 일치, 제품 Console0을 확인했다. 동작 테스트/Player/Profiler 실행0건. P0를 UI 기능 구현 완료로 보고하지 않는다.
- Git/다음: 설계문서5개만852ee960로 기록하고 track/P0 branch를 생성했다. P0 확인/commit·track 통합 후 P1 계약stub+실패 테스트를 실행하고 Green 구현으로 진행한다. 사용자 최종 확인 전 main/branch 정리를 보류하며 PC 종료 지시는 없다.
