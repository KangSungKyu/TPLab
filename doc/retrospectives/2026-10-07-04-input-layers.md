# 2026-10-07 · Input layer·소유권

- 요청: 입력 wrapper 구현 중 단계1. 기준 track `80d657a`, 작업 `codex/input-system-p1-layers`. 기존 사용자 변경4개와 입력 asset/설정은 보존한다.
- 결정: 별도 Input assembly, InputManager 소유 clone·GUID 조회·종료, 독립 layer/전체 차단 lease와 immutable snapshot. 하위 에이전트 gpt-6.1-sol/high가 controller 한 파일을 구현하고 부모가 Manager·테스트·리뷰·Unity를 담당했다.
- 문제/배움: 기존 Editor가 응답하지 않아 실행을 보류했고 사용자 복구 후 동일 PID23120에서 재개했다. EditMode에서는 가상 키 입력이 Action을 시작하지 않아 native canceled 검증을 PlayMode로 옮겼다. 내부 Update overload는 public API가 아니므로 사용하지 않는다.
- 실제 검증: [Red](../validation/input-system/p1/red-edit.json)9실행/9실패/0skip. 첫 Green13 중12통과/1실패는 위 테스트 전제 실패다. 최종 [Edit](../validation/input-system/p1/green-edit-final.json)12/12, [Play](../validation/input-system/p1/green-play.json)1/1, 실패0·skip0. 추가4개는 보완 회귀이며 별도 Red로 주장하지 않는다. 컴파일 통과; Console·전체 회귀·Player/소비 프로젝트·실제 사용자 UX는 최종 단계에서 확인한다.
- Git: 단계1 commit 후 원본 commit을 보존해 track으로 통합한다. main 반영은 전체 자동 검증과 최종 사용자 확인 뒤 수행한다.
- 다음: 리바인딩/native operation·취소/timeout·버튼 release·JSON/reset. 준비 block은 등록을 동결하지 않으며 게임 입력 자동 활성화는 하지 않는다.
