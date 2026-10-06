# Phase 5 전환 정의 Editor 검증

2026-10-06. 기준 track `5eda919b89a59cea3021c37c14d176f6b3719ff0`, 작업 `codex/game-scenes-p5-editor`. 기존 MyLab Unity 6000.3.18f1 / Connector 0.4.1 / PID 23176만 사용한다. 현재 보완 검증 중이며 최종 자동 gate 통과를 아직 선언하지 않는다. [계약](../../GAME_SCENE_MANAGER_DRAFT.md), [회고](../../retrospectives/2026-10-06-21-scene-transition-editor.md).

첫 fixture 실패 10/0/10/0는 동작 Red가 아니다. 초기 Red 10/5/5/0 중 1건은 배열 참조 동일성 assertion 오류였다. 초기 Green10/10 및 전체 Edit238/238은 후속 수정 전의 결과다. 보완 Red12/10/2/0와 첫 보완 Green 실패12/11/1/0를 보존하며 Addressables default test의 당시 fixture는 실제 저장된 settings가 아니었다. 해당 실패를 resolver 결함의 독립 증거로 취급하지 않는다. 최종 counts와 실제 native gate·Console·보호 파일 검증은 실행 후 아래에 기록한다.

| 실제 기록 | total / pass / fail / skip | 증거 |
|---|---|---|
| fixture-failure-edit.json | 10 / 0 / 10 / 0 | [fixture-failure-edit.json](fixture-failure-edit.json) |
| red-edit.json | 10 / 5 / 5 / 0 | [red-edit.json](red-edit.json) |
| initial-green-edit.json | 10 / 10 / 0 / 0 | [initial-green-edit.json](initial-green-edit.json) |
| initial-full-EditMode.json | 238 / 238 / 0 / 0 | [initial-full-EditMode.json](initial-full-EditMode.json) |
| supplemental-red-edit.json | 12 / 10 / 2 / 0 | [supplemental-red-edit.json](supplemental-red-edit.json) |
| supplemental-green-failure-edit.json | 12 / 11 / 1 / 0 | [supplemental-green-failure-edit.json](supplemental-green-failure-edit.json) |
| default-corrected-red-edit.json | 1 / 0 / 1 / 0 | [default-corrected-red-edit.json](default-corrected-red-edit.json) |
| green-edit.json | 12 / 12 / 0 / 0 | [green-edit.json](green-edit.json) |
| pre-cleanup-full-EditMode.json | 240 / 240 / 0 / 0 | [pre-cleanup-full-EditMode.json](pre-cleanup-full-EditMode.json) |
| pre-cleanup-full-PlayMode.json | 199 / 199 / 0 / 0 | [pre-cleanup-full-PlayMode.json](pre-cleanup-full-PlayMode.json) |

올바른 저장 fixture를 만든 뒤 잘못된 config object 타입의 resolver를 일시 복원하여 `default-corrected-red-edit.json`의 실제 Red 1/0/1/0를 확인했다. proper wrapper 타입으로 복원한 뒤 Green을 실행했다. reflection은 설치 Addressables 내부 cache/정확한 테스트 소유 postprocessor delegate를 복원하는 테스트에만 사용하며 제품 코드의 설정 migration/전역 검색으로 확장하지 않는다. 기존 callback 보존 assertion은 후속 보완으로 별도 사전 Red는 미실행이다.

[native-editor.json](native-editor.json)은 실제 BuildPipeline에서 missing-required-policy로 거부하고 live metadata 진단·Play 진입 차단·owned asset 제거를 확인한 기록이다. 원래 build list를 유지했으므로 Play에는 build list/첫 씬 경고도 있었으며 condition만이 유일한 Play 차단 원인이라고 주장하지 않는다. [실행 경계](native-execution.json)에 원래 PID·지연 finisher 처리와 한계를 남겼다. successful Player build/run은 Phase 6이며 이 실패 build를 성공 증거로 사용하지 않는다.

최종 기록: Green12/12/0/0, 전체 Edit240/240/0/0 및 Play199/199/0/0. [최종 Edit](full-EditMode.json), [최종 Play](full-PlayMode.json). 전역 테스트 callback 해제·기존 callback 보존 assertion을 포함한다. 실제 source 변경 후 전체 회귀를 다시 실행했다. native helper의 지연 callback 제거도 보완한 최종 소스로 actual build/Play 검사를 다시 실행했다. native 지연 finisher는 Play=false를 관찰한 뒤 한 번만 실행했고 본래 예약을 해제했다. 예상 테스트/빌드 로그를 보존하고 최종 Console 오류·경고0 및 원래 Editor ready를 확인했다. 성공 Player·소비 프로젝트·최종 시각 UX는 P6이며 main·브랜치 삭제는 사용자 확인까지 보류한다.

[최종 입력](test-inputs.json) 223개와 [보호 파일](preserved-inputs.json) 5개의 raw bytes를 확인했다. [정적 verifier](../../../tools/verify_validation.py)는 saved counts/source hashes/meta GUID/document links/fixture 제거 및 최종 Console/Editor 증거를 검사하며 Unity 테스트 실행을 대체하지 않는다.
