# Scene loading presentation: final user acceptance

이 절차는 자동 테스트 이후 **마지막 한 번의 실제 UI 확인**에 사용한다. 코어의 자동 검증 결과나 [synthetic InputSystem/sample tests](../Assets/MyLab/Tests/PlayMode/LoadingSampleTests.cs)는 사람의 화면 확인과 실제 키보드·게임패드·pointer 조작을 대신하지 않는다. 아직 완료되지 않은 항목은 PASS로 적지 않는다. 실행 결과 수와 source revision은 [scene-loading track](SCENE_LOADING_TRACK.md)에서 관리한다.

## 준비와 설정

1. 기존 MyLab Editor에서 `MyLab > Scene Transitions > Open Additive`를 선택한다. Additive 확인 후 `Open Single`도 별도로 실행한다. sample assets가 없을 때만 `Build Sample Assets`를 선택한다.
2. 각 열린 sample에서 Hierarchy의 `CommonSceneRoot`를 선택하고 `SceneTransitionSampleController`를 Inspector에서 찾는다. **Use Loading Presentation**은 기본 꺼짐, **Manual Proceed**는 기본 꺼짐이다. `Loading Tips`는 project-owned sample 문구 배열이다. 변경한 뒤 Play한다. 코드에서 전환 전 `ConfigureLoadingPresentation(true, manualProceed: true)` 또는 자동 진행용 `ConfigureLoadingPresentation(true, manualProceed: false)`를 호출해도 된다.
3. 로딩 화면은 callback이 runtime에 생성하는 전체 화면 Canvas다. 새 Unity scene을 로드하지 않는다. 특히 Single에서는 `CommonSceneRoot`와 그 아래 controller, EventSystem/Input module, loading UI가 persistent hierarchy에 남는지 확인한다.

## 자동·수동 진행 확인

Additive와 Single 각각에서 자동 진행과 수동 진행을 확인한다. 각 설정 변경은 Play 진입 전에 한다.

| 설정 | 확인할 동작 |
|---|---|
| Automatic: Use Loading Presentation on, Manual Proceed off | 최초 Hub 진입 때 loading UI가 cover 아래 준비된 뒤 공개된다. 준비 후 자동으로 final cover/reveal까지 진행한다. 이후 `Hub / Main` 조작으로 Hub→Main→Hub 교체를 확인한다. |
| Manual: Use Loading Presentation on, Manual Proceed on | 준비 중 Continue는 비활성이다. `AwaitingProceed`가 되면 Ready 문구와 완료 bar가 표시되고 loading 화면이 유지된다. 입력 release 경계를 지난 fresh Continue 입력만 한 번 진행시킨다. Hub↔Main 왕복에서도 이전 operation의 click이 다음 operation을 완료하지 않아야 한다. |
| Opt-out: Use Loading Presentation off | 기존 cover-only 흐름이 남아 있고 loading UI/proceed 대기가 나타나지 않는다. |

화면에서 stage별 퍼센트 막대와 stage label, 진행률을 모르는 단계의 `working...` 안내, tips 배열 문구를 확인한다. 막대는 **현재 stage**의 값이므로 전체 전환율로 해석하지 않는다. 1280×720 reference 외에 실제 배포 대상 해상도와 다른 화면 비율에서도 전체 화면 가림, 글자·막대·Continue 배치를 확인한다.

## 입력 release 경계와 modal

수동 mode에서는 preparation 중 입력을 누른 채 `AwaitingProceed`로 넘어가도 Continue가 자동 실행되지 않아야 한다. 입력을 release한 뒤에도 진행하지 않고, 다시 새로 누른 입력 한 번에만 현재 operation이 진행되는지 확인한다. 아래 각 입력은 실제 장치로 따로 확인한다.

- Keyboard: UI Submit을 누르고 계속 누른 상태에서 대기 화면이 된 뒤 release하고 fresh Submit을 누른다.
- Gamepad: 실제 연결된 controller의 Submit을 같은 방식으로 확인한다. 장치가 없으면 미실행으로 기록한다.
- Pointer: preparation 중 버튼 입력을 누른 채 대기 경계를 통과하고 release해도 진행되지 않고, 이후 새 Continue click으로만 진행되는지 확인한다. 빠른 press/release와 중복 click도 확인한다.

`System modal` 샘플 동작으로 modal이 gameplay 차단을 독립적으로 유지하고 final reveal 뒤에도 해당 차단이 해제되지 않는지 확인한다. 현재 sample HUD는 전환 중 modal을 여는 trigger를 제공하지 않는다. 따라서 modal과 loading panel을 동시에 띄워 순서와 fullscreen raycast를 확인할 수 있는 project-owned trigger가 실제 구성돼 있지 않으면 그 동시 표시 항목은 미검증으로 기록한다. 구성이 있다면 Canvas 표시 순서는 modal이 loading UI보다 위, 두 번째 cover가 loading UI를 가리고, modal은 cover 위여야 한다.

실패 보호도 확인한다. sample의 `Fail next prepare`는 destination 준비 실패 뒤 cover와 gameplay blocking이 유지되는지 보여준다. Manual wait 중 owner cancel은 public `Manager.CancelTransition()`을 호출할 수 있는 project/test trigger가 있을 때 실행한다. 같은 trigger로 Continue UI를 파괴하거나 UI callback을 실패시켜 cover 복구, candidate cleanup, UI/listener 단일 해제를 관찰한다. 현행 HUD에 해당 cancel/UI-failure control이 없다면 자동 PlayMode 결과와 분리해 **interactive 미실행**으로 남긴다.

## 결과 기록

자동 Player smoke의 12개 structural observation은 실제 장치 입력·시각 UX의 PASS가 아니다. Synthetic/native input tests도 실제 gamepad/pointer 상호작용 증거가 아니다. Additive/Single 및 Automatic/Manual 각 조합, stage progress/tips, release 후 fresh input, modal overlap, 해상도별 fullscreen을 실제 확인한 항목별로 기록한다. 실패·미실행은 각각 그대로 남기고, 확인이 끝날 때까지 acceptance 완료로 처리하지 않는다.
