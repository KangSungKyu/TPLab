# 2026-10-07 · Input rebind·override

- 요청/기준: 입력 wrapper 단계2. track `119dde0`, 작업 `codex/input-system-p2-rebinding`. [이전 회고](2026-10-07-04-input-layers.md) 후 native 선택·release·JSON 경계를 구현했다.
- 결정: 한 scope에 한 mutation, native OnApplyBinding 후보 캡처, default 같은 Map/교차 group 정확한 path 충돌 거부와 선택적 validator 대체. 선택 버튼 release 후 ID/생존 재확인·commit, 전체 realtime timeout·장치/키 취소·caller/owner OCE 구분. owner shutdown은 신규 요청 차단·native 취소/해제·공유 drain·clone 파괴를 수행한다.
- JSON: native override envelope/Action 경로/서로 다른 known binding ID 검사와 임시 clone Load를 먼저 수행한다. null 거부, native empty string roundtrip 지원. commit/rollback 실패는 fault 차단한다. 파일 저장/프로필 migration은 프로젝트 책임이다.
- 실제 문제: 기본 native event suppression은 후보 버튼 state update를 막아 아직 누른 버튼을 released로 읽었다. 관리 Map은 BlockAll로 비활성하므로 suppression을 해제해 native state를 관찰한다. removed 이후 native candidate list는 장치 ID를 역조회해 null이 될 수 있어 OnPotentialMatch에서 장치를 미리 저장하고 즉시 Cancel한다. 두 결함은 실제 실패 후 수정했다.
- 검증: Red [Edit5](../validation/input-system/p2/red-edit.json)/[Play5](../validation/input-system/p2/red-play.json) 모두 실패/skip0. 첫 Green Edit6통과·Play8중2실패, 다음 Play9중1실패. 최종 [Edit18](../validation/input-system/p2/green-edit-corrected.json)/[Play10](../validation/input-system/p2/green-play-corrected.json) 모두 통과·실패0·skip0. 실제 새 키 반응/이전 키 비활성, composite 본체 거부와 part GUID 보존 포함. 추가 경계는 보완 회귀로 기록하며 별도 Red라고 주장하지 않는다.
- 한계: native rollback 자체의 이중 실패는 자동으로 강제 재현하지 않았다. exact path 충돌은 wildcard/alias 의미상 충돌을 보장하지 않는다. 전체 회귀·root/UI·소비 프로젝트/Player·사용자 UX는 후속이다.
- Git/보존: 단계 commit을 track에 통합하며 main은 최종 gate 대기. 단계1 잘못된 컴파일 호출 후 결과는 stale이라 명시하고 통과 증거에서 제외했다. 본 입력 모듈의 생성 meta 공백만 정리하며 GUID를 보존했다. 기존 사용자4개/기본 asset/ProjectSettings7개 hash를 확인한다. 검증 도구3개는 병렬 준비했지만 단계4 변경으로 분리한다.
- 다음: InputManagerInstaller와 프로젝트 UI adapter·전환/modal lease 예제·준비/해제 gate 검증.
