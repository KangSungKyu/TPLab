# 2026-10-06 · Phase 5 전환 정의 Editor 검사

- 목표/기준: `codex/game-scenes-p5-editor`, track `5eda919b89a59cea3021c37c14d176f6b3719ff0`. 명시적 settings/씬 asset/condition ID를 Inspector·compile/Play/build 검사에 연결한다.
- 상태: MyLab 내부 자동 검증 완료. track 통합 준비, main·사용자 gate 미도달.
- 결정: 기존 Bootstrap 검사 queue/preview/build hook을 재사용한다. 조건 Evaluate는 실행하지 않고 root metadata만 읽는다. settings 범위의 잠재 derived graph를 visited 집합으로 검사하며 실제 runtime tree 권한과 구분한다. 명시적인 Inspector 변경 전에는 legacy key/GUID를 보존한다.
- 배정: track_guidelines(luna/medium)이 Editor·관련 테스트를 구현하고, scene_runtime(6.1-sol/high)이 읽기 리뷰를 했다. 부모가 Unity·증거·Git을 소유한다.
- 문제: 최초 fixture는 untitled 씬과 Additive NewScene 제약으로 10건 실패했다. saved owned scene을 열도록 보완한 뒤 의미 있는 초기 Red를 실행했다. build scene 배열 참조 동일성 assertion을 값 비교로 수정했다. 추가 default-settings fixture는 비영속 메모리 settings를 GUID로 참조하여 잘못된 상태였고 실제 저장된 owned settings로 보완했다. fixture 실패를 생산 코드 결함의 Red 증거로 확대하지 않는다.
- 검증/증거: [검증 기록](../validation/scene-transition-editor/README.md)에 실제 실행 수·Red/Green·native build/Play 결과를 최종 연결한다. 전체 Player 성공·소비 프로젝트·반복 Play·시각/input UX는 Phase 6이다.
- 다음: P5 실제 자동 gate와 보호 bytes 확인 후 track 통합. 이후 P6에서 Bootstrap 반복 진입 결함을 먼저 실제 Red로 관찰하고 최소 수명 수정·통합 예제·Player·소비 검증을 진행한다. 최종 인간 확인 전 main 병합·브랜치 삭제·절전은 하지 않는다.

최종 보완/검증: corrected default fixture로 actual resolver Red1/0/1/0를 확인했다. 전역 postprocessor callback은 정확한 테스트 소유 target만 해제하고 기존 callback 유지 여부를 assertion으로 검증했다. 최종 Green12/12/0/0, 전체 Edit240/240/0/0·Play199/199/0/0. actual build는 missing-required-policy로 실패하고 live 진단과 pre-Play 차단을 확인했다. Play에는 원래 build list/첫 씬 오류도 있으므로 단일 원인이라고 주장하지 않는다. compile 완료, 예상 로그 보존 후 Console0·원래 Editor ready·보호 raw5 유지. 테스트 callback 보완의 별도 사전 Red는 미실행이다.
