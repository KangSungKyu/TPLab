# 씬 루트 선택과 명시적 주입

InitScene 같은 씬의 root GameObject에 소유 방식을 선택하고, 프로젝트별 installer가 일반 C# 서비스 생성·참조 주입·해제를 수행한다. [ResourceManagerInstaller](RESOURCE_MANAGER.md#sceneroot에서-선택하여-사용)로 자산 소유자를 연결할 수 있다. manager마다 Singleton을 상속하거나 자동 검색하는 규칙은 없다.

| 선택 | 접근 | 기본 수명 |
|---|---|---|
| SceneOwnedRoot | Inspector 참조 또는 Attach 반환값 | 해당 root 파괴까지. 여러 root가 독립적으로 동작 |
| SingletonSceneRoot | 위 방식 + SingletonSceneRoot.Instance | 해당 root 파괴까지. 성공한 root 하나만 등록 |

두 방식 모두 `Persist Across Scenes`를 별도로 선택하면 root와 자식이 씬 전환 후에도 유지된다. Singleton 선택이 자동 영속화를 의미하지 않는다. 하나의 GameObject에는 host 하나만 지정한다.

공용 코어의 [Bootstrap 권장안](GAME_SCENE_MANAGER_DRAFT.md)은 Bootstrap을 첫 씬으로 실행하고 유지한 채 게임 씬을 Additive로 로드하는 방식이다. 이 경로는 Bootstrap의 SceneOwnedRoot·Persist Across Scenes=false를 우선 권장하며, 씬을 유지하므로 root에 별도의 영속화를 추가하지 않는다. Singleton 접근 선택은 유지한다. 공용 root와 게임 씬별 root의 종료 책임은 나누고 게임 씬 언로드 때 Bootstrap 서비스를 종료하지 않는다. [현재 최초 진입](BOOTSTRAP_SYSTEM.md)은 GameSceneManager에 위임한다. 영속 공용 root를 선택하면 Bootstrap/callback을 그 하위에 두고 첫 씬의 Single/Additive를 선택할 수 있다. 이 선택은 기본 권장 경로이며 Single과 영속 공용 root 선택·구역 수명 tree는 [Phase별 계약](GAME_SCENE_MANAGER_DRAFT.md)을 따른다.

## Editor에서 선택

1. Hierarchy에서 최상위 GameObject를 선택한다. 기존 InitScene controller가 붙은 객체도 선택할 수 있다.
2. `GameObject > TPLab > Scene Root > Scene Owned` 또는 `Singleton`을 실행한다.
3. Inspector의 `Installers`에 SceneRootInstaller 구현 컴포넌트를 필요한 순서로 지정한다. 메뉴는 같은 객체에 붙은 installer를 기본 등록한다. 자식 installer는 직접 지정한다.
4. 필요하면 `Persist Across Scenes`를 켠다. Play 중에는 설정 Inspector를 잠근다.

메뉴는 기존 객체에 host를 추가하며 Undo/Redo를 지원한다. 씬을 자동 저장하거나 기존 controller를 변경하지 않는다. 선택한 객체가 자식·asset이거나 이미 host가 있으면 메뉴가 비활성화된다. 설정 변경은 Play 전에 완료한다. [Unity Undo.AddComponent](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Undo.AddComponent.html)

## 스크립트에서 선택

```csharp
var rootObject = new GameObject("InitSceneRoot");
rootObject.SetActive(false);
var installer = rootObject.AddComponent<MyProjectInstaller>();
ISceneRoot root = SceneRootSetup.Attach(
    rootObject, SceneRootMode.SceneOwned,
    new SceneRootInstaller[] { installer }, persistAcrossScenes: false);
rootObject.SetActive(true);
```

`MyProjectInstaller`는 소비 프로젝트가 구현한다. runtime에서는 비활성 root에 설정한 뒤 활성화한다. 활성 객체에 Attach를 호출하면 객체를 변경하기 전에 예외를 반환한다. 이미 host를 Inspector에 붙인 경우 해당 객체가 비활성인 동안 `Configure`로 installer 목록을 지정할 수 있다. [Unity 활성화 콜백](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.SetActive.html)

## InitScene·manager 연결 계약

- `SceneRootInstaller.Install(ISceneRoot root)`에서 서비스를 생성하고 InitScene controller 등 소비자의 명시적 초기화 메서드로 참조를 전달한다. controller의 Awake/Start와 서비스 준비 순서를 암묵적으로 연결하지 않는다.
- 소비자가 다른 installer에서 생성한 서비스에 의존하면 그 installer를 먼저 배치한다. 게임별 API·서비스 필드·소비자 목록은 프로젝트 installer가 소유한다.
- `Uninstall`에서 주입된 참조를 해제하고 자신이 만든 서비스를 종료한다. 일부만 만들어진 상태에서도 정리할 수 있어야 한다. 게임 로직·자산 검색·reflection DI container를 코어에 추가하지 않았다.
- host는 설정 배열을 복사하고 전체 입력을 검사한 다음 순서대로 Install을 호출한다. 비활성화·재활성화는 재설치나 해제를 유발하지 않는다.
- installer는 root 또는 자식에 있어야 하며 null·중복은 거부한다. 초기화 실패 시 실패한 installer와 앞선 installer를 역순으로 Uninstall하고 뒤의 installer는 실행하지 않는다.
- 파괴 시에도 역순으로 정리한다. 정리 예외가 있어도 나머지를 시도하고 예외를 기록한다. installer별 정리는 설치 시도당 한 번이며, installer를 host보다 먼저 별도로 삭제하지 않는다.
- `IsReady`는 동기 Install 전체 성공만 의미한다. 비동기 준비는 `PrepareAsync`를 await하고 `IsPrepared`로 확인한다. 씬 진행과 가림막 callback은 [비동기 씬 계약](ASYNC_SCENE_LIFECYCLE.md)을 따른다. async void Install은 사용하지 않는다.
- Singleton 중복은 host 컴포넌트만 제거한다. 중복 객체·다른 컴포넌트는 보존하며 installer를 실행하지 않는다.

실행 예제와 계약 검사는 `SceneRootTests`와 `SceneRootInstallerProbe`에 있다. 비동기 준비·해제 hook과 씬 진행 callback도 제공한다. 실제 자산 소유자 생성·참조 주입과 pool 수명 연결은 [ResourceManager 계약](RESOURCE_MANAGER.md)과 `ResourceManagerTests`를 따른다.

## 검증 (2026-10-02)

- 동일 TPLab Editor: Unity 6000.3.18f1, Connector 0.4.1, PID 42616.
- 실제 Red: PlayMode 신규 10개 모두 실패, Editor 신규 3개 모두 실패. [runtime](validation/scene-root/red-play.json), [Editor](validation/scene-root/red-editor.json)
- 전체 Green: EditMode 41/41, PlayMode 41/41, 실패·skip 0. [EditMode](validation/scene-root/green-edit.json), [PlayMode](validation/scene-root/green-play.json)
- Editor의 두 메뉴·기존 컴포넌트 보존·순서/영속 설정 직렬화·Undo/Redo와 runtime의 순서·부분 실패·중복 소유·설정 제한·씬 해제/영속성을 자동 검증했다.
- 반복 Play 자동 검사 10/10: 두 root의 주입 복구·정리 횟수를 네 가지 Domain/Scene Reload 조합에서 확인했다. 원래 씬·옵션을 복원했다. [결과](validation/scene-root/reload-check.json)
- Editor 컴파일 완료, 상태 ready. LogAssert로 확인한 예상 예외 8건을 [보존](validation/scene-root/expected-test-exceptions.json)했고 최종 Console 오류는 [0건](validation/scene-root/console-errors.json)이다.
- 반복 Play·컴파일·Console의 최종 근거는 [실행 기록](validation/scene-root/execution.json)에 남긴다. NUnit 테스트 건수와 반복 Play 검사는 별도 집계한다.
- 사용자 직접 실행 확인이 필요한 항목은 없다. 소비 프로젝트 가져오기·Player 빌드·다른 Unity 버전은 미검증이다. 기존 `ProjectSettings/SceneTemplateSettings.json`은 보존하고 포함하지 않는다.
