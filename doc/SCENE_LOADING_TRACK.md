# Scene loading presentation track

2026-10-07. 기준 main `32e6cac1e80c95ec38033f95310f7ac2459e095f`. 사용자 구현 요청에 따라 `codex/scene-loading-track`에서 단계별 통합한다. 최종 화면 확인 전 main 병합은 보류한다.

계약: [설계](SCENE_LOADING_PRESENTATION_DRAFT.md). 기존 가림막 경로와 Single/Additive 소유권을 보존한다. UI는 프로젝트 callback 소유다.

| 단계 | 브랜치 | 상태 | 증거 |
|---|---|---|---|
| 1 진행률 | codex/scene-loading-p1-progress | 자동 검증 완료 | validation/scene-loading/p1 |
| 2 표시 순서·3 진행 대기 | codex/scene-loading-p2-flow | 구현·targeted 검증 완료 | validation/scene-loading/p2; Phase 4 통합 검증 대기 |
| 4 예제·통합 검증 | 후속 phase | 진행 중 | sample UI callback·Single/Additive Player·최종 UX 확인 |

| 담당 | 모델/추론 | 범위·이유 | 상태 |
|---|---|---|---|
| 부모 | 현재 모델 | 계약·manager·테스트 실행·문서·Git | 진행 |
| /root/input_validation_plan | gpt-6-luna/low | 기존 계약의 사람/AI 문서 갱신; 확정 계약 반복 작업 | 진행 |
| /root/input_layers | gpt-6.1-sol/high | ResourceManagement progress 및 테스트; 비동기 결과 소유권 위험 | P1 완료 / P4 sample Red 준비 |

사용자 변경 4개와 보호 입력 7개는 [hash](validation/scene-loading/p1/preserved-inputs.json)로 대조한다. 원본 Editor PID23120을 사용한다. 자동 검사와 실제 최종 UX 확인은 별도 gate다. PC 종료/절전은 이번 요청에 포함되지 않는다.

P1 Red Edit13(2pass/11fail), Play209(201pass/8fail). Green Edit13/13, Play210/210, skip0. short filter 0건은 통과로 기록하지 않음. 표시context 이름 충돌은 SceneLoadingContext로 분리. 테스트의 build GUID 정규화는 원본 bytes로 복원.

P2/3는 UI lifetime/진행대기/old release 순서가 하나의 전환 흐름이므로 한 phase로 통합했다. Red Play 21/28 (7 failures), Green Play 28/28, failed0/skipped0: [Red](validation/scene-loading/p2/red-flow.json), [Green](validation/scene-loading/p2/green-flow.json). 이는 `GameSceneReplacementTests` targeted filter만 확인한다. 전체 회귀·Player·sample UX는 Phase 4에서 검증한다. 최종 source revision은 부모가 검증 tip 확정 후 기록한다.

P2 최종 targeted36/36, failed0/skip0 ([green-final-flow](validation/scene-loading/p2/green-final-flow.json)); Core252/224는 이후 cover보정1건 전이며 최종 전체 suite는P4에서실행한다. P4sample Red8/8fail 확인 ([red-ui](validation/scene-loading/p4/red-ui.json)).
