# 로딩 UI 예제와 최종 자동 검증

2026-10-07. 상태: 자동 검증 완료 / 최종 사용자 UX 확인 대기.

- 목표: project callback 소유의 진행률·팁·자동/수동 Continue UI를 기존 전환 예제에 연결하고 전체 코어와 실제 Player를 검증한다.
- 기준: `codex/scene-loading-p4-sample`, HEAD `07273a4ed4a8e0656e49d7b8fdbe897b32bc5f5e`. P1/P2 트랙을 재사용하며 main은 변경하지 않는다.
- 구현: runtime 생성 UI를 persistent common controller 아래에 두고 operation GUID/TCS/listener를 단일 소유한다. 수동 진행은 입력 release와 새 입력을 기다리고 자동 진행은 준비 완료 후 계속한다. 두 cover 사이에도 transition lease는 유지한다. 삭제·취소·오래된 통지는 현재 operation에 영향을 주지 않는다. Core에는 UI/Input 의존성을 추가하지 않았다.
- 문제와 수정: 테스트의 잘못된 BuildScene short path를 올바른 fixture path로 고쳤다. ConfigureView의 누락된 `_module` 저장을 공유 경계에서 수정했다. 준비 완료 후 표시가 working/빈 bar로 남는 누락을 Red1로 재현하고 Ready 문구와 완료 막대를 표시했다. 표시 완료가 게임 진행 허용을 뜻하지 않는다.
- TDD: 최초 UI Red8(0pass/8fail), fixture 문제 Green11(0pass/11fail), module 미등록 Green11(0pass/11fail), 수정 후11/11. Ready Red1(0pass/1fail) 뒤 전체 Play253/253, final Edit271/271, reload UI12/12. 모두 skip0; 실패 실행을 Green으로 기록하지 않았다.
- 실행: 동일 MyLab Editor PID23120. 두 Windows Mono 빌드(errors0/warnings0)와 Additive/Single Player 각12/12. Input 포함 별도 소비 프로젝트 Editor build와 Player 실제2회 성공. CLI timeout 뒤 fresh Connector exact run ID로 확정한 Play 결과를 보존했다. 예약 build callback은 결과가 생성되지 않아 실행 증거로 쓰지 않았고 직접 build를 실행했다.
- 보호: build가 생성한 URP/settings 변화만 복원했다. 보호7 raw hash와 기존 dirty4를 보존했다. 소비 로그와 Player 로그를 영속 증거로 복사하고 이번 작업의 재생성 가능한 Temp 출력만 정리한다.
- 리뷰: 부모 diff/실행 증거 확인과 `/root/input_layers` 읽기 전용 최종 정적 리뷰에서 추가 계약·소유권 결함 없음. `/root/input_validation_plan`은 사람/AI 문서와 사용자 확인 절차를 갱신한다.
- 검증 자료: [P4 증거](../validation/scene-loading/p4/README.md), [트랙](../SCENE_LOADING_TRACK.md), [사용자 확인](../SCENE_LOADING_ACCEPTANCE.md).
- 한계: 실제 장치·해상도·화면 순서 UX, 원격 Addressables content, IL2CPP/다른 Unity·플랫폼 미실행. native Player는 자동 흐름 구조 확인이며 수동 화면 확인을 대체하지 않는다. Unity 재로드 요청 CLI 응답 timeout 자체는 reload 성공 증거로 삼지 않고 이후 fresh12 결과와 원본Editor readiness를 사용했다.
- Git: P4를 track에 fast-forward 통합하고 phase/track을 push한다. main 병합·branch 삭제는 최종 사용자 확인까지 보류한다. 사용자 소유 새 세션이나 PC 종료/절전은 이번 요청에 포함하지 않는다.
- 다음: 사용자가 최종 화면 확인 항목을 확인하면 최신 main 동기화·영향 검증 후 승인된 main 병합과 작업 branch 정리를 진행한다.
