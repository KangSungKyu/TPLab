# 2026-10-07 · Input root·UI·transition

- 기준: track `16daccd`, 작업 `codex/input-system-p3-integration`. [이전 회고](2026-10-07-05-input-rebinding.md) 이후 명시적 초기화·native UI 경계를 연결했다.
- 변경: InputManagerInstaller는 두 root host에서 scope/준비 BlockAll을 소유하고 root.IsPrepared 이후 프로젝트의 CompletePreparation 호출만 허용한다. game lease는 자동 획득하지 않는다. graceful Release와 즉시 Uninstall/OnDestroy를 지원한다.
- UI: 프로젝트 Samples의 InputSystemUiScope는 clone 참조만 빌리고 module lifecycle 후 현재 layer를 재적용한다. 전체 차단 시 즉시 disable/focus 정리; 복원은 다음 frame·button release 이후다. inactive host에서 ConfigureView하는 기존 계약을 보존했다. 구독·module만 정리하며 원본·manager·caller refs는 소유하지 않는다.
- 예제: 기존 controller의 직접 Enable/Disable을 독립 gameplay/ui/modal/transition lease로 치환했다. 프로젝트 UI Map은 blockers보다 위에 두고 가림막 실패 시 transition 차단을 유지한다. Inspector Input context menu로 native keyboard binding/표시·메모리 JSON·reset을 확인한다. 로딩 UI/progress 확장은 제외했다.
- 실제 검증: [Red Play3](../validation/input-system/p3/red-play.json) 모두 미구현 계약 실패. [입력 Edit18](../validation/input-system/p3/green-edit.json)/[Play14](../validation/input-system/p3/green-play-final.json) + [기존 sample Play2](../validation/input-system/p3/sample-regression-play.json), 실패0/skip0. 두 root 준비 전 차단·명시적 공개·graceful 해제, native module 외부 Enable 차단 재적용, held Submit release, 팝업 focus 변경 시 같은 submit 중복0·새 submit1을 관찰했다. 추가 Submit은 보완 회귀이며 별도 Red가 아니다.
- 경계: 실제 mouse/touch UI·물리 gamepad·화면 UX는 최종 사용자 gate다. GUI 렌더링 없이 실행된 입력 테스트를 시각 수락으로 기록하지 않는다. 전체 기존 회귀/reload·소비 프로젝트/Player는 단계4다.
- Git: 검증된 단계3를 원본 commit 보존해 track에 통합한다. 단계4 도구/독립 consumer 자료는 이 commit에 섞지 않는다. main은 최종 확인 대기.
- 다음: 전체 회귀, 네 reload 조합, include/exclude consumer/Windows Player, 최종 수락 문서·증거 hash·Console·임시 파일 정리.
