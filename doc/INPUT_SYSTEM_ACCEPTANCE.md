# Input wrapper 최종 사용자 확인

2026-10-07. 입력 wrapper의 자동 검증과 실제 화면·물리 장치 확인을 구분한다. 사용자가 "input은 확인했고, 문서화도 진행하면 되겠어"라고 확인하여 **최종 사용자 수락 완료**로 기록했다. 아래는 수락 때 사용한 절차이며 개별 물리 게임패드/touch 실행을 추가로 주장하지 않는다. [구현 계약](INPUT_SYSTEM_DRAFT.md), [track](INPUT_SYSTEM_TRACK.md), [자동 검증](validation/input-system/p4/README.md)을 기준으로 한다. 새 로딩 진행률 UI는 이번 범위가 아니다.

## 실행 준비

현재 MyLab Editor에서 `MyLab > Scene Transitions > Open Additive`를 선택하고 Play한다. 기존 sample 자산을 재생성할 필요는 없다. Hierarchy의 공용 Bootstrap 오브젝트에서 `SceneTransitionSampleController`를 선택한다. 해당 component의 context menu에 `Input/…` 네 항목이 있다. 아래 리바인딩은 Inspector에서 시작한 뒤 Game view에 포커스를 옮긴다.

## 키 설정과 저장

1. `Input/Rebind Attack Keyboard`를 실행한다. K를 누른 채 잠깐 유지하면 아직 적용되지 않고, 뗀 뒤 `Applied`와 K 표시가 나타나야 한다. 이후 K를 누르면 `Gameplay input` counter가 증가한다. 변경된 keyboard Attack binding의 이전 Enter는 Attack을 발생시키지 않는다. Enter는 여전히 UI Submit이므로 UI 동작과 gameplay counter를 구분한다.
2. 다시 실행하고 Escape로 취소한다. K 설정이 유지되고 입력이 복원되어야 한다. 다시 실행한 뒤 아무 키도 누르지 않으면 15초 후 `TimedOut`이 나오고 기존 설정이 유지되어야 한다.
3. K 설정에서 `Input/Save Overrides In Memory` → `Input/Reset Overrides` → `Input/Restore Overrides From Memory`를 실행한다. reset은 원본 기본 키, restore는 저장한 K로 돌아와야 한다. 입력 원본 asset은 변경되지 않아야 한다. 저장은 이 Play session의 메모리 예제이며 다음 실행까지 보존하는 파일 저장 기능이 아니다.
4. 원한다면 리바인딩에서 같은 Player Map의 다른 동작에 이미 쓰는 W를 선택한다. 기본 충돌 정책에 따라 `Rejected`이고 기존 키가 유지되어야 한다. 프로젝트별 alias/composite 의미상 충돌 검사는 별도 validator 책임이다.

## 팝업·전환·장치

- 포인터로 `System modal`을 열고 `Dismiss modal`로 닫는다. modal 중 gameplay counter가 증가하지 않고 UI는 조작 가능해야 한다. 키보드로 modal을 열 때 같은 Enter 한 번으로 새 modal이 바로 닫히거나 두 번 처리되지 않는지도 확인한다. 재바인딩 후 Enter의 gameplay 중복과 UI Submit을 혼동하지 않는다.
- `Hub / Main` 왕복과 `Add Area` → `Add Nested` → `Parent removes Area`를 실행한다. 전환 중 gameplay가 막히고 전환 뒤 복원되어야 한다. modal을 유지한 코드 전환과 실패 cover 검사는 [기존 절차](SCENE_TRANSITION_ACCEPTANCE.md#additive-확인)를 사용한다. modal 해제와 transition 차단이 서로 독립이어야 한다.
- Play를 멈추고 `Open Single`에서 다시 확인한다. 공용 root가 유지되고 입력·UI가 새 게임 씬에서 동작해야 한다. Play를 다시 시작하면 새 runtime clone에 원본 기본 설정이 적용된다.
- 실제 게임패드가 있다면 sample asset의 UI navigation/Submit과 Player Attack binding을 확인한다. 포인터 click·키보드 Submit·게임패드 Submit 각각에서 팝업 열기/닫기와 gameplay 차단을 확인한다. 장치가 없으면 게임패드 항목은 미실행으로 기록한다. 이번 자동 rebind 검증은 가상 Keyboard를 사용했으며 물리 게임패드·touch UX의 증거가 아니다.

Play 종료 후 `MyLab > Scene Transitions > Restore Original Setup`을 실행한다. 자동 복구가 이미 끝났으면 추가 변경이 없다. 원래 씬과 Build Settings, 기존 사용자 변경이 보존되는지 확인한다.

## 수락과 통합

입력 수락과 문서화 요청은 확인되었다. 자동 결과의 source hash·보호 파일·문서·원격 gate를 확인한 뒤 기존 승인에 따라 main 통합과 해당 작업 브랜치 정리를 진행한다. 개별 물리 장치별 통과 목록은 제공되지 않았으므로 그 범위는 미확인으로 유지한다. 새로운 PC 종료·절전 지시는 이번 입력 작업에 없다.
