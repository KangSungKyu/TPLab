# Input System wrapper 구현 track

2026-10-07. 사용자 요청: 입력 wrapper부터 구현한다. [입력 계약](INPUT_SYSTEM_DRAFT.md)의 layer/소유권 → 리바인딩 → root/UI 연결 → 통합 순서이며 [로딩 UI 확장](SCENE_LOADING_PRESENTATION_DRAFT.md)은 제외한다.

## 기준과 보호

- main 기준 `ce290878da51c653eaa219aecc45e6bf61ac755a`, track `codex/input-system-track`. Phase 브랜치는 track의 검증 tip에서 생성하고 원본 commit을 유지해 통합한다.
- 기존 사용자 변경: InitScene.unity, DefaultVolumeProfile.asset, Mobile_RPAsset.asset, SceneTemplateSettings.json. 본 작업의 stage/수정에서 제외하고 raw hash를 보존한다. 기본 입력 asset도 변경하지 않는다.
- 입력 모듈은 `Assets/MyLab/Input/Runtime`의 별도 assembly다. 기존 `Assets/MyLab/Core`만 가져가는 소비 프로젝트에 입력 의존성을 강제하지 않는다. 테스트는 Input/Tests의 독립 assembly로 둔다.
- Unity 조작·테스트·Git·최종 리뷰는 부모 에이전트가 맡는다. 테스트 중 소스 변경은 동결한다. 독립적 위임이 필요할 때만 담당·지원 모델·허용 경로를 아래에 기록한다.

## 단계 상태

| 단계 | 브랜치 | 상태 | 완료 조건·증거 |
|---|---|---|---|
| 0 운영 | codex/input-system-track | 준비 완료 | 운영 commit `80d657a`, 보호 hash와 동일 Editor 확인 |
| 1 layer·소유권 | codex/input-system-p1-layers | 자동 검증 완료 | Red9/실패9; Edit12/12·Play1/1, skip0. [회고](retrospectives/2026-10-07-04-input-layers.md) |
| 2 rebind | codex/input-system-p2-rebinding | 자동 검증 완료 | 기준 `119dde0`; Red Edit5/실패5·Play5/실패5; Green Edit18·Play10, 실패0/skip0. [회고](retrospectives/2026-10-07-05-input-rebinding.md) |
| 3 root·UI | codex/input-system-p3-integration | 자동 검증 완료 | 기준 `16daccd`; Red Play3/실패3; Green 입력 Edit18/Play14 + 예제Play2, 실패0/skip0. [회고](retrospectives/2026-10-07-06-input-integration.md) |
| 4 최종 검증 | codex/input-system-p4-validation | 자동 검증 완료·사용자 확인 대기 | Edit258/258·Play217/217, reload8/8, Windows 두 mode 각10, consumer 제외/포함 통과. [증거](validation/input-system/p4/README.md), [회고](retrospectives/2026-10-07-07-input-validation.md) |

## 담당

| 담당 | 범위 | 상태 |
|---|---|---|
| 부모 | 계약·실패 테스트·Unity·리뷰·통합·회고, InputManager | 단계1~4 자동 검증 완료·최종 사용자 확인 대기 |
| /root/input_layers, gpt-6.1-sol / high (2026-10-07) | 재진입·lease 수명 위험에 따른 선택. InputLayerController.cs만 수정; Unity·Git 제외 | 단계1 구현·부모 검증 완료 |
| /root/input_layers 재사용, gpt-6.1-sol / high (2026-10-07) | 비동기 native 수명·원자적 override 위험. InputRebindingController.cs만 수정; Unity·Git 제외 | 단계2 구현·부모 검증 완료 |
| /root/input_layers 재사용, gpt-6.1-sol / high (2026-10-07) | 준비/해제·UI lifecycle 수명 위험. InputManagerInstaller.cs·Samples/InputSystemUiScope.cs만; Unity·Git 제외 | 단계3 구현·부모 검증 완료 |
| /root/input_validation_plan, gpt-6-luna / low (2026-10-07) | 조사 후 확정된 consumer 포함/제외 경로 도구 보완. tools/run_core_consumer.py·ConsumerSmoke.cs·tools/README.md만; Unity·Git 제외 | 도구 구현·self-check와 부모 실제 consumer 검증 완료 |
| /root/input_layers 재사용, gpt-6.1-sol / high (2026-10-07) | Input runtime·UI/예제 읽기 전용 최종 리뷰, 소스·Unity·Git 변경 금지 | 리뷰 완료; 초기 owner 종료 취소 예외 결함1건을 부모가 Red/Green으로 보완 |

## 최초 Editor 상태

기존 MyLab Editor는 Unity6000.3.18f1/Connector0.4.1 PID23120이다. discovery는 ready지만 실제 PID Responding=false이고 health 및 C# exec 요청은 timeout이다. MCP 연결 instance는0이다. 다른 프로젝트/새 Editor로 검증을 대체하지 않는다. 사용자가 기존 Editor를 복구하도록 요청했고 계약·실패 테스트 준비를 진행한다. 실행0건은 Red 또는 통과가 아니다.

사용자 재확인 후 같은 PID23120의 Responding=true와 C# exec/compile idle을 확인했다. 실제 EditMode Red 9개 실행·9개 실패·skip0을 기록했다([증거](validation/input-system/p1/red-edit.json)). 실패 이유는 미구현 계약이며 컴파일 오류는0이다.

## 통합·수락

Phase 자동 검증·부모 리뷰·회고 뒤 track에 통합한다. 단계1 `119dde0`, 단계2 `16daccd`, 단계3 `3b0adaa`를 fast-forward로 통합했다. 단계4 최종 commit은 Git 이력/최종 보고에서 확인한다. [최종 실제 키 설정·팝업·장치 확인](INPUT_SYSTEM_ACCEPTANCE.md)은 한 번에 모으며 사용자 최종 확인 전 main 병합과 track/Phase 삭제는 보류한다. 문서/준비 commit을 runtime 완료 증거로 쓰지 않는다. PC 종료·절전은 이번 요청에 없으며 수행하지 않는다.
