# Scene Transitions sample

이 sample은 Bootstrap에서 Hub/Main 씬을 전환하고 Area/Nested 구역을 관리하는 **프로젝트 연결 예제**다. 코어 runtime에는 UI/입력 의존성을 추가하지 않는다. 로딩 presentation도 sample callback이 소유하며 별도 Unity scene을 만들지 않는다.

## 실행

현재 TPLab Editor에서 `TPLab > Scene Transitions > Open Additive` 또는 `Open Single`을 선택한 뒤 Play한다. sample assets가 아직 없을 때만 `Build Sample Assets`를 사용한다. Play를 멈추면 builder가 열기 전 scene setup과 Editor Build Settings를 복원한다. 복원이 필요하면 `Restore Original Setup`을 실행한다.

Hierarchy에서 `CommonSceneRoot`를 선택하고 `SceneTransitionSampleController` Inspector를 연다. 두 opt-in 설정의 기본값은 false이며 Tips는 아래 sample 문구를 제공한다.

- **Use Loading Presentation**: 켜면 최초 진입과 sample load/replace/add에 loading UI callback을 사용한다.
- **Manual Proceed**: `Use Loading Presentation`을 켠 경우에만 의미가 있다. 끄면 준비 완료 후 자동 진행하고, 켜면 Continue 입력을 기다린다.
- **Loading Tips**: sample이 표시할 문자열 배열이다. 기본값으로 sample tip 두 개가 들어 있으며, 비우면 기본 안내 문구를 사용한다.

Inspector 대신 transition이 시작되기 전에 callback에서 `ConfigureLoadingPresentation(true, manualProceed: true)`를 호출할 수 있다. 자동 진행은 `ConfigureLoadingPresentation(true, manualProceed: false)` 또는 기본 두 번째 인수 `false`다. 실행 중 정책 변경은 거부된다.

sample은 `SceneLoadingContext.OperationId`와 일치하는 진행 snapshot만 반영한다. bar는 **현재 stage 비율**을 표시하며, 비율이 없는 준비 단계에는 `working...` 문구를 보여준다. 이는 전체 전환 퍼센트나 예상 남은 시간이 아니다. 준비가 끝나면 Ready 문구와 완료 bar를 표시하고, 수동 mode는 입력 release 경계 이후 Continue를 활성화한다. Tips는 sample이 소유하고 순환 표시한다.

Single sample에서는 `CommonSceneRoot`가 persistent다. callback, EventSystem/Input module과 런타임 생성 `LoadingPresentation` UI도 그 root 하위에 남아 있어야 한다. manager는 로딩 presentation 전/후에 새로운 scene을 추가하지 않는다. 전환 lease는 첫 가림막부터 두 번째 가림막과 cleanup을 지나 최종 reveal까지 유지된다.

## Player smoke

`SceneTransitionSampleBuilder.BuildWindowsMono(bool single, string outputDirectory, string evidencePath)`는 선택된 sample scenes로 Windows x64 Mono Player를 만든다. 새 output은 `Temp/GameScenesTrack/ScenePlayers` 아래, 새 JSON evidence는 `doc/validation/scene-integration`, `doc/validation/input-system`, 또는 `doc/validation/scene-loading` 아래에 둔다. Builder는 실행 중 scripting backend, Editor scene setup, Build Settings와 원본 Build Settings bytes를 복원한다.

Player smoke에서 `-tplab-loading-presentation`을 지정하면 automatic mode로 opt-in한다. `run_scene_player.py`의 `--loading-presentation --expected-checks 12`는 12개의 관찰과 loading preparation/reveal/progress/proceed/release/two-cover counter를 검사한다. 이 batch smoke는 수동 진행 UI나 실제 키보드·게임패드·pointer 입력을 확인하지 않는다. 사람의 최종 UX 확인은 Play에서 Continue 버튼 및 실제 키보드·게임패드·pointer 입력을 확인하는 수락 절차를 따른다.

실행 기록과 현재 검증 범위는 개발 저장소의 scene-loading 검증 기록에 둔다. 이 예제의 현재 P4 통합 상태를 자동 green이나 사용자 승인으로 확대하지 않는다.

## Import and preparation

Requires TPLab Core, TPLab Input, Unity Input System and uGUI. Import **Scene Transitions** from the Input package's Samples tab. Run **TPLab > Scene Transitions > Build Sample Assets** explicitly before opening imported template scenes. The builder resolves its own MonoScript GUID and writes only its sample folder under Assets; it rejects package storage. This regenerates the saved settings' scene paths for the actual imported folder and uses the bundled `Settings/SampleInput.inputactions`. Existing scenes and Build Settings are restored on success or failure. Unsaved or dirty user scenes must be saved first.

Use **Open Additive** or **Open Single**, enter Play, then stop Play; the builder restores the original scene and Build Settings setup. **Restore Original Setup** is also available explicitly. The runtime controller derives its root from its owning saved Bootstrap scene before that root becomes persistent; Player paths do not depend on Editor static state. This sample owns one active session at a time.