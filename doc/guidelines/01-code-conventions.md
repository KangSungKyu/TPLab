# 01 · 코드 규약

[현행] SourceRevision: main `af084e0d0027c4098fad08c336ccb0753e1c7862`. 갱신일: 2026-10-15. 현재 지침·인용을 읽기/Git 조회로 대조했으며 Unity·테스트·Player 실행은 0회다. 기존 실행·집계 수치는 각 기록의 당시 source 기준이며 이번 재실행·재집계로 보지 않는다. 선언·행수 집계 기준은 `35508bc1fb8ed7749a292d1f9ae983b45387afd4`(2026-10-13)이다. [공통 근거와 검증 범위](README.md#근거와-검증-범위).

## 이름과 assembly 경계

- [현행] PascalCase 타입/메서드, camelCase 인자, `_camelCase` private 필드, `I` 인터페이스, `Async` 비동기 이름은 지침과 대표 구현에 나타난다; 모든 심볼의 자동 적합성 검사 결과는 아니다. [AGENTS:16](../../AGENTS.md#c과-unity-기준), [ObjectPool:18](../../Assets/TPLab/Core/Pooling/ObjectPool.cs#L18), [ISceneRoot](../../Assets/TPLab/Core/Lifecycle/ISceneRoot.cs#L8), [RebindAsync:109](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L109).
- [현행] assembly는 Core, Core.Input, UI, UI.InputSystem, Core.Editor로 분리된다. Core는 UI/InputSystem을 참조하지 않고, Input은 InputSystem, UI는 Core/UniTask/uGUI, UI.InputSystem은 UI/Core.Input/InputSystem을 참조하며 Editor는 Editor 플랫폼 한정이다. [Core asmdef](../../Assets/TPLab/Core/TPLab.Core.asmdef#L1), [Input asmdef](../../Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef#L1), [UI asmdef](../../Assets/TPLab/UI/Runtime/TPLab.UI.asmdef#L1), [adapter asmdef](../../Assets/TPLab/UI/InputSystem/Runtime/TPLab.UI.InputSystem.asmdef#L1), [Editor asmdef](../../Assets/TPLab/Editor/TPLab.Core.Editor.asmdef#L1).

## 한 파일의 공개 타입: 현재 예외

[현행] 아래 목록은 Core, Input/Runtime, UI/Runtime, UI/InputSystem/Runtime의 추적 `.cs` **56파일**에서 `public (abstract|sealed|static|readonly|partial)* (class|struct|interface|enum)` 선언을 세어 얻었다. 여러 공개 타입은 실제 관행이며 “한 파일 한 공개 타입”을 현행 규칙으로 쓰지 않는다; 중첩 타입도 따로 표시한다. [Input:14](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L14), [중첩 HostBinding:24](../../Assets/TPLab/UI/Runtime/UIContextInstaller.cs#L24).

| 상태 | 파일과 선언 수 | 근거 |
|---|---|---|
| [현행] | CsvDataTable 2, DataRow 2, IdxCodec 4 | [CsvDataTable:8](../../Assets/TPLab/Core/DataTables/CsvDataTable.cs#L8), [DataRow:6](../../Assets/TPLab/Core/DataTables/DataRow.cs#L6), [IdxCodec:6](../../Assets/TPLab/Core/DataTables/IdxCodec.cs#L6) |
| [현행] | SceneRootSetup 2, SceneLoadProgress 3, SceneTarget 2 | [Setup:7](../../Assets/TPLab/Core/Lifecycle/SceneRootSetup.cs#L7), [Progress:6](../../Assets/TPLab/Core/ResourceManagement/SceneLoadProgress.cs#L6), [Target:7](../../Assets/TPLab/Core/ResourceManagement/SceneTarget.cs#L7) |
| [현행] | GameSceneManager 2, SceneRegistration 2, SceneTransitionDefinition 2, SceneTransitionProgress 2 | [Manager:14](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L14), [Registration:6](../../Assets/TPLab/Core/SceneManagement/SceneRegistration.cs#L6), [Definition:10](../../Assets/TPLab/Core/SceneManagement/SceneTransitionDefinition.cs#L10), [Progress:8](../../Assets/TPLab/Core/SceneManagement/SceneTransitionProgress.cs#L8) |
| [현행] | InputLayerController 3, InputRebindingController **5** | [Layer:9](../../Assets/TPLab/Input/Runtime/InputLayerController.cs#L9), [Rebinding:14](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L14): RebindCandidate / RebindRequest / RebindStatus / RebindResult / InputRebindingController |
| [현행] | UIContextSettings 2, UIEnums 6; UIContextInstaller 1 + 중첩 HostBinding 1 | [Settings:10](../../Assets/TPLab/UI/Runtime/UIContextSettings.cs#L10), [Enums:4](../../Assets/TPLab/UI/Runtime/UIEnums.cs#L4), [Installer:20](../../Assets/TPLab/UI/Runtime/UIContextInstaller.cs#L20) |

## 로그와 메인 스레드 검사

- [현행] 같은 56파일에서 **직접 `Debug.LogException(...)` 호출12곳**이다: Core7(MonoSingleton117/165, RootInstallation74, SceneOwnedRoot90/102, BootstrapSystem212/227), UI Runtime3(VirtualScrollRect702/720/741), optional adapter2(UIInputSystemAdapter322/427). [Singleton](../../Assets/TPLab/Core/Lifecycle/MonoSingleton.cs#L117), [Installation](../../Assets/TPLab/Core/Lifecycle/RootInstallation.cs#L74), [Root](../../Assets/TPLab/Core/Lifecycle/SceneOwnedRoot.cs#L90), [Bootstrap](../../Assets/TPLab/Core/SceneManagement/BootstrapSystem.cs#L212), [Scroll](../../Assets/TPLab/UI/Runtime/VirtualScrollRect.cs#L702), [adapter](../../Assets/TPLab/UI/InputSystem/Runtime/UIInputSystemAdapter.cs#L322).
- [현행] `.Forget(Debug.LogException)`는 직접 호출 분모에 넣지 않은 method-group2곳이며 Samples/Tests/Editor/UPM도 제외했다; “저장소 전체 로그12곳”이라는 뜻은 아니다. [Bootstrap:208/223](../../Assets/TPLab/Core/SceneManagement/BootstrapSystem.cs#L208).
- [현행] `EnsureMainThread` **구현5곳**은 PlayerLoopHelper.IsMainThread 검사와 InvalidOperationException을 각 경계에 둔다; 호출 횟수5가 아니다. [DataTableManager:444](../../Assets/TPLab/Core/DataTables/DataTableManager.cs#L444), [ResourceManager:236](../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs#L236), [LoadedScene:66](../../Assets/TPLab/Core/ResourceManagement/LoadedScene.cs#L66), [GameSceneManager:1422](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L1422), [UIContext:546](../../Assets/TPLab/UI/Runtime/UIContext.cs#L546).

## 크기와 책임 분리 판단

- [현행] 같은56파일의 Git blob `splitlines()` 중앙값은 **75.5행**이며 큰 파일은 GameSceneManager1428, UIContext1297, VirtualScrollRect768, InputRebindingController731행이다; 주석/빈 행 포함 파일 행수이며 클래스별 실 코드행 수는 아니다. UIContext는 별도 partial 파일613행도 있다. [Manager](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L26), [UIContext](../../Assets/TPLab/UI/Runtime/UIContext.cs#L1), [Scroll](../../Assets/TPLab/UI/Runtime/VirtualScrollRect.cs#L1), [Rebinding](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L84), [partial](../../Assets/TPLab/UI/Runtime/UIContext.Input.cs#L1).
- [결정됨] D-C — 2026-10-15 변경: 3단계에서는 대형 클래스를 실제 분리하지 않고 안전망 57 case를 산출물로 확정한다. 조건 평가 약91줄·Core 스레드 검사 본문 합계 약24줄의 통합 이득이 작고 큰 전환·해제 흐름의 조합 보호는 충분하지 않아 기존 분리 계획을 대체한다. 반복 수정으로 특정 부분의 읽는 비용이 커지거나 새 기능이 한 책임에 크게 붙을 때 다시 검토한다. 크기만으로 분리하지 않는 D-H1은 유지한다. [사용자 결정](README.md#사용자-결정), [안전망과 한계](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/retrospectives/2026-10-15-02-phase3-close.md#안전망-지도).
- [결정됨] D-H1: 클래스 분리는 책임·소유권 경계를 기준으로 판단한다. **1,000행 초과는 분리 규칙이 아니라 설명이 필요한 신호**로만 사용한다. 근거: 사용자 2026-10-13 일괄 승인, [사용자 결정](README.md#사용자-결정), [Manager 실행 경계:310](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L310), [정리 경계:855](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L855).
- [결정됨] 검토했으나 채택하지 않은 대안: 500행 초과를 검토 신호로 사용하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H1), [사용자 결정](README.md#사용자-결정).

[현행] 당시 집계의 재측정은 `git ls-tree -r --name-only <집계Revision>`로 위 네 경계의 `.cs`를 고른 뒤 `git show <집계Revision>:<path>`의 `splitlines()`와 선언 정규식, `Debug\.LogException\s*\(`, `(?:internal|private) static void EnsureMainThread`를 센다; 실행 성능·컴파일·전체 스타일 준수는 이 측정의 범위 밖이다. [대표 직접 호출](../../Assets/TPLab/UI/Runtime/VirtualScrollRect.cs#L702), [대표 guard 선언](../../Assets/TPLab/UI/Runtime/UIContext.cs#L546).
