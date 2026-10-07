# GameSceneManager 최종 사용자 확인

P6 자동 검증은 완료했다. 원래 MyLab Editor의 전체 EditMode240/240·PlayMode201/201(실패0·skip0), Windows Player Additive/Single 각10관찰, Reload4×2와 동일 Unity 소비 프로젝트 빌드/11관찰을 확인했다. 정확한 범위·실패 기록·Console·입력 hash는 [P6 검증 기록](validation/scene-integration/README.md)에 있다. 2026-10-07 사용자가 아래 Unity 실행 항목까지 확인 완료했다고 명시했다. 현재 상태는 **사용자 확인 완료**다. [수락 기록](validation/scene-integration/final-integration/acceptance.json)을 보존하고 기존 승인에 따라 main 통합·작업 브랜치 정리를 진행한다.

## 실행 준비

원래 MyLab Editor에서만 확인한다. sample 자산은 `Assets/MyLab/Samples/SceneTransitions/Scenes/`의 `BootstrapAdditive.unity`, `BootstrapSingle.unity`, `Hub.unity`, `Main.unity`, `Area.unity`, `Nested.unity`다. 생성 산출물이 없을 때만 `MyLab > Scene Transitions > Build Sample Assets`를 실행한다. 이 메뉴는 sample 소유 표식과 자산 충돌을 확인하고 기존 Editor 설정을 복구한다. 완료된 산출물을 재생성할 필요는 없다.

각 mode는 `MyLab > Scene Transitions > Open Additive` 또는 `Open Single`로 열고 Play를 눌러 확인한다. 확인 전에 Game view를 16:9(1280×720 기준)와 4:3으로 바꿔 HUD, 버튼 라벨, cover, modal이 화면 안에 유지되고 의도한 버튼이 눌리는지 확인한다. 키보드 방향키/WASD로 메뉴를 이동하고 Enter로 제출한다. 기본 입력 자산에서 Enter는 UI Submit과 Player Attack 양쪽에 연결되어 있으므로, gameplay counter만 따로 확인할 때는 버튼이 없는 게임 화면을 클릭한다.

## Additive 확인

초기 진입에서 비동기 cover가 준비 중 입력을 막고 완료 후 사라지는지 본다. `Hub / Main`을 두 번 눌러 Hub→Main→Hub 왕복을 확인한다. Bootstrap 공용 root는 유지되고 기존 primary가 새 primary로 교체되는지 Hierarchy와 화면 상태를 확인한다. 화면의 `Gameplay input` counter가 준비된 상태에서 증가하고 전환 준비 중에는 증가하지 않는지도 확인한다.

`Toggle policy`를 눌러 policy를 false로 만든 뒤 `Hub / Main`을 누른다. 상태 메시지가 거부를 알리고 primary·파생 scene·cover·입력 상태가 바뀌지 않는지 확인한다. policy를 다시 true로 전환한다.

`Add Area`와 `Add Nested`를 차례로 누른 뒤 `Nested self exit`를 눌러 Area가 남는지 확인한다. Nested를 다시 추가하고 `Parent removes Area`를 눌러 Area와 그 아래 Nested가 함께 사라지는지 확인한다. 가능한 경우 Area만 추가한 상태에서 `Area self exit`도 확인한다.

`System modal`을 열면 아래 게임 명령 버튼은 차단되며, modal 안의 `Dismiss modal`은 키보드·포인터로 사용할 수 있어야 한다. modal을 유지한 채 코드 전환을 확인하려면 아래 명령을 실행한다. 전환 cover가 사라진 뒤에도 modal은 열린 채 gameplay 입력을 막고 UI 입력은 유지해야 한다. `Dismiss modal`로 닫은 뒤 gameplay 입력이 복원되는지 확인한다. 자동 smoke도 이 코드 전환을 검증한다.

```powershell
unity-cli exec 'var c=UnityEngine.Object.FindFirstObjectByType<MyLab.Samples.SceneTransitions.SceneTransitionSampleController>(); var id=c.Manager.GameScene.path==MyLab.Samples.SceneTransitions.SceneTransitionSamplePaths.Main ? "to-hub" : "to-main"; c.Manager.TryTransitionAsync(id).Forget(); return "requested transition under modal";' --project C:/Users/PC/Projects/MyLab --allow-async --usings Cysharp.Threading.Tasks
```

`Fail next prepare`를 누르고 `Hub / Main`으로 교체를 요청한다. 오류 modal과 cover가 보이고 gameplay 입력이 꺼지는지 확인한다. `Dismiss modal`을 눌러도 cover가 남아 scene 입력이 계속 차단되어야 한다. 이 실패 상태에서는 추가 명령을 시도하지 않는다. Play를 멈추고 `Open Additive`를 다시 선택해 새 Play session을 시작한다.

## Single 확인 및 복구

Play를 멈춘 뒤 `MyLab > Scene Transitions > Open Single`을 선택하고 다시 Play한다. 초기 entry 후 공용 Singleton root와 Bootstrap callback/cover가 살아 있고 Hub↔Main 교체 때 공용 root가 유지되는지 확인한다. `Add Area`, `Add Nested`, `Nested self exit`, 재추가, `Parent removes Area`를 실행해 파생 씬 트리가 정리되는지 본다. gameplay 입력 counter, 전환 중 입력 차단, 키보드 UI 조작, 두 화면비에서의 HUD/cover/modal을 확인한다.

Play를 멈추고 `MyLab > Scene Transitions > Restore Original Setup`을 실행한다. 자동 복구가 이미 끝났으면 이 명령은 추가 변경 없이 반환한다. 원래 scene setup과 Build Settings가 돌아왔는지 확인하고 작업 전 사용자의 Unity 변경 4개가 보존된 상태인지 확인한다.

## 기록과 통합 대기

한 번의 최종 확인에서 Additive, Single, 조건 거부, 중첩 제거, 실패 cover, 입력, 화면비, 원래 설정 복구를 각각 `절차 / 기대 결과 / 실제 결과 / 통과·실패·미실행`으로 기록한다. 자동 smoke·Player build·Console 결과를 시각 확인으로 대신하지 않는다. UI를 실제 확인하지 못한 항목은 미검증으로 남긴다.

통합 diff는 `main...codex/game-scenes-track` 기준으로 검토하고 사용자의 Unity 변경 4개는 별도로 보존한다. 전체 자동 gate와 위 인간 확인이 완료되고 명시적 사용자 답변이 있기 전에는 main 병합·푸시나 track/Phase 브랜치 삭제를 하지 않는다. 답변을 기다리는 동안에는 결과를 보존하고 Unity를 저장한 뒤 Editor를 열어 절전할 수 있다. 구현·검증 중에는 절전하지 않는다. 확인과 통합·보존 정리가 모두 끝난 뒤에만 Unity 정상 종료 및 PC 종료 절차를 진행한다.
