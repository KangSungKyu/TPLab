# 씬 로딩 표시·진행 대기 P2/3

2026-10-07. 기준 track8f3bfde, codex/scene-loading-p2-flow. 표시와 진행 대기는 old root/UI 수명 순서가 하나의 계약이라 한 단위로 구현했다.

- opt-in 정책은 accepted operation별 고정. common 준비 뒤 프로젝트 UI 준비/중간 cover OFF, native/root/presentation 준비, AwaitingProceed, 자동/수동 wait, 두 번째 cover, Additive old subtree 해제/UI release, 최종 reveal이다. 기존 cover-only와 derived remove 순서는 유지한다.
- Additive 대기는 이전 root를 유지한다. Single은 기존 old root 종료/native Single을 유지하고 이미 종료된 root 조건을 다시 평가하지 않는다. 진행 신호와 두 번째 cover await 이후 해제 직전에 live조건/소유권/token을 다시 확인한다.
- UI부분reveal 실패·owner취소는 progress 통지 없이 실제 cover를 먼저 복구하고 candidate/UI cleanup을 시도해 실패를 aggregate한다. UI cleanup은 begun once. 최종 reveal 실패는 기존 SceneRootFlow 복구를 재사용한다.
- 최초 progress UI예외도 보호 cover 시도를 막지 않도록 실제 ShowCover 이후에 관찰 callback을 통지한다. root준비와 gameplay허용은 계속 별개다. caller token은 wait-only.
- 실제 Red targeted28 중 신규7fail. Green28/28, 추가 core Play224/224·Edit252/252, 이후 최초cover보정 포함 targeted36/36, failed0/skip0. [증거](../validation/scene-loading/p2/). 전체 Input/sample/Player/consumer·최종 UX는 P4에서 새로 확인한다. P4 UI테스트는별도namespace이며 Red8/8fail 확인, 아직stub단계이다.
- 보호7입력 hash 유지. Test Framework build GUID 정규화만 원본 bytes 복원. P2commit/track통합 후 P4진행; main 최종확인대기. Runtime Core의 UI/Input참조 추가 없음. 새 UI테스트 Inputassembly 참조는 P4파일로 분리한다.
