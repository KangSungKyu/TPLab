# GameSceneManager 최초 진입과 BootstrapSystem

2026-10-06. 최초 진입과 Phase 3A 주 씬 교체의 MyLab 내부 자동 검증 완료. GameSceneManager가 씬 진입·교체·취소·해제를 소유하고 BootstrapSystem은 Inspector 설정과 자동 시작을 담당한다. 파생 구역·조건/정의는 [후속 Phase](GAME_SCENE_MANAGER_DRAFT.md)다. 게임별 씬·서비스·UI를 자동 생성하지 않는다.

## Inspector 구성

1. 첫 Bootstrap 씬에 top-level GameObject와 SceneOwnedRoot 또는 SingletonSceneRoot를 둔다. 권장 기본값은 Persist Across Scenes=false + First Load Mode=Additive다. 이 경우 Bootstrap 씬을 유지한다.
2. 공용 root 영속화를 선택하면 Persist Across Scenes=true로 두고 BootstrapSystem과 SceneTransitionCallbacks를 해당 root 또는 그 자식에 둔다. 두 로드 모드를 사용할 수 있다. callback이 사용하는 UI·EventSystem·서비스도 프로젝트가 영속 수명을 보장해야 한다. 일반 씬의 객체 참조를 자동 영속화하지 않는다.
3. BootstrapSystem의 Scene Root, First Game Scene, Load Mode, Auto Start, Callbacks를 지정한다. Auto Start=true면 Start에서 실행한다. false면 BootstrapAsync를 호출한다. Inspector와 코드가 같은 설정 검사를 사용한다.
4. 목적지에는 유일한 활성 top-level root를 두고 persistence를 끈다. 공용 root가 SingletonSceneRoot면 게임 root는 SceneOwnedRoot를 사용한다. 같은 SingletonSceneRoot.Instance를 양쪽에서 중복 소유하지 않는다.
5. 실제 Build Profiles 씬 목록의 첫 순서에 Bootstrap을 둔다. Source=BuildScene이면 enabled 목록에 목적지를 포함한다. Source=Addressable이면 선택한 SceneAsset의 등록과 고유 Address 또는 Scene GUID Reference를 지정한다. 자동 loader 추정·fallback은 하지 않는다. [로더 계약](SCENE_LOADING.md)을 따른다. Editor에서는 Bootstrap을 열고 목적지는 닫는다. 다른 Play Mode Start Scene은 기존 gate에서 거부한다.

공용 수명은 root의 기존 persistence 설정이 소유한다. 별도의 설정값·DI container·전역 GameSceneManager singleton은 추가하지 않았다. 영속 root는 Awake/동기 Install 뒤 DontDestroyOnLoad로 이동하므로 저장된 씬 검사와 runtime 실제 소속 검사를 구분한다. Bootstrap이 영속 root 밖에 있거나 callback이 다른 일반 씬에 있으면 거부한다. 네이티브 Single로 Bootstrap 씬은 해제되지만 영속 실행자는 살아남는다.

## 실행과 준비 신호

`사전 검사 → cover → 공용 root.PrepareAsync → native Single/Additive → active scene → ConfigureSceneAsync → 게임 root.PrepareAsync → PreparePresentationAsync → 최종 소유권 검사/reveal → Ready/GameScene 공개`

Bootstrap은 native load/unload를 호출하지 않는다. BootstrapAsync가 명시적 manager를 한 번 생성하고 공용 준비 전에 Manager로 공개한다. manager는 순수 C# 객체이며 기본 MonoSingleton 상속을 요구하지 않는다. 프로젝트는 공용 root의 installer 목록으로 서비스 준비를 지정한다. 최초 진입을 공용 installer의 PrepareAsync 안에서 await하면 자기 준비를 기다리는 순환이 된다. 최초 진입은 동기 설치 이후 Start/외부 controller에서 요청한다.

Unity native activation은 비동기 시스템 준비와 다르다. Awake/Install은 ConfigureSceneAsync보다 먼저 실행될 수 있다. ConfigureSceneAsync는 설치된 소비자에게 빌린 참조를 연결하며 활성 root를 다시 Configure하지 않는다. PreparePresentationAsync는 준비 완료한 root를 받으며 화면 준비까지 await한다. 임의 Awake/Start/Update를 코어가 정지시키지 않는다. 게임 controller는 성공한 진입과 CanProceed를 확인해 진행을 허용한다. 별도 activation framework나 UI 자산은 없다.

```csharp
bootstrap.Configure(commonRoot, "Assets/Scenes/HubScene.unity",
    autoStart: false, callbacks: presentation, loadMode: LoadSceneMode.Single);
await bootstrap.BootstrapAsync(cancellationToken);
GameSceneManager scenes = bootstrap.Manager;
if (scenes.CanProceed)
{
    // 프로젝트 controller가 게임 진행을 허용한다.
}
```

공용 root는 위 호출 전에 persistence와 installers를 설정하고 활성화해야 한다. 코드에서 Bootstrap 없이 직접 사용할 수도 있다.

```csharp
var scenes = new GameSceneManager(commonRoot, presentation);
await scenes.EnterFirstSceneAsync("Assets/Scenes/HubScene.unity", LoadSceneMode.Additive, cancellationToken);
// 다른 소비자는 기존 작업만 기다린다. 새 entry 명령은 거부한다.
await scenes.WaitForEntryAsync(otherWaitToken);
await scenes.ShutdownAsync();
await ((ISceneRoot)commonRoot).ShutdownAsync();
```

manager 생성은 설치된 root를 요구하며 자체적으로 설치/준비하지 않는다. 생성·주입과 최초 진입을 분리하고 프로젝트 공용 owner가 manager 하나를 소유한다. Bootstrap의 반복 호출은 기존 진입을 공유한다. manager의 EnterFirstSceneAsync 반복 명령은 성공/실패 이후에도 거부한다. WaitForEntryAsync는 진입 이력의 완료 대기이며 현재 진행 권한을 뜻하지 않는다.

## 소유권·취소·종료

첫 Single은 기존 일반 씬이 Bootstrap 하나이며 다른 미등록 lifecycle root가 없을 때만 허용한다. 기존 일반 씬을 유지하면서 Single을 지정하면 부작용 전에 거부한다. Additive로 Single을 흉내 내지 않는다. Additive 최초 진입은 시작 시 일반 씬 집합을 유지한다. 공용 root는 최초 소유 씬에 머물고 게임 root는 로드한 씬의 top-level을 유지해야 한다. 이동된 게임 root의 이미 소유한 서비스도 정리한다. 후보 안의 빌린 공용 root를 언로드로 파괴하지 않는다. 로드 전과 준비/reveal 경계에서 실제 집합을 비교하고 외부 변경을 진단한다. 로드된 목적지는 sceneLoaded에서 실제 Scene 인스턴스로 기록하며 경로만으로 언로드하지 않는다. 동일 경로의 외부 동시 native 로드는 지원 계약 밖이다.

호출자 token은 그 await만 취소한다. CancelTransition은 실제 manager 작업의 취소를 요청하고 ShutdownAsync는 취소와 게임 정리를 기다린다. native 로드는 강제 취소되지 않으므로 늦은 완료까지 소유하고 후보 root를 ShutdownAsync한 뒤 해당 씬을 해제한다. cleanup에는 취소 토큰을 전달하지 않는다. 공용 root는 게임 종료 중 준비된 상태를 유지하며 최종 종료는 외부 owner가 담당한다.

```csharp
scenes.CancelTransition();             // 현재 작업에 취소 요청; cleanup 완료 대기는 기존 entry task
await bootstrap.ShutdownAsync();       // 진입 중단/성공한 게임 cover → root 종료 → 해당 씬 해제
await ((ISceneRoot)commonRoot).ShutdownAsync();
```

Single 이후 마지막 일반 씬은 Unity에서 별도로 unload할 수 없다. 게임 root 해제를 시도하고 LoadedScene에 남은 씬, IsGamePrepared=false, Faulted와 cleanup 예외를 기록한다. 자동 빈 씬 생성·복귀·재진입은 하지 않는다. 실패한 entry의 잔여 씬은 외부에서 다른 씬이 사용 가능해진 뒤 명시적 ShutdownAsync로 정리를 끝낼 수 있다. root의 실제 release는 자체 공유 종료로 중복 실행하지 않는다. 이미 시작한 manager ShutdownAsync는 같은 결과를 공유하는 terminal 작업이다. [Unity 언로드 제한](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html)

## 상태와 실패

State는 Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene, PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted다. FailurePhase와 LastFailure는 실제 실행/cleanup/callback 오류를 공개한다. GameScene은 성공 공개, LoadedScene은 남은 후보, IsGamePrepared는 후보 root 준비 상태다. CanProceed는 Ready와 두 root 준비·씬 생존·callback 생존·active scene·씬 집합을 대조한다. 수명 상태는 자동 polling하지 않는다. 외부 변경 뒤 State가 마지막 완료 단계에 남더라도 CanProceed는 false이며 전환 중 외부 변경은 Faulted로 보고한다.

실행 실패와 owner 취소는 reveal을 생략한다. reveal 중 오류가 나면 SceneRootFlow가 cover를 복구 요청한다. 원인과 정리/OnFailure 오류를 집계하며 OnFailure는 첫 실행/종료 실패를 한 번 통지한다. caller의 대기 취소는 owner 실패 통지가 아니다. 성공한 게임의 종료 cover 오류도 자원 정리를 계속 시도하고 오류를 반환한다.

hook 안에서 자기 진입/종료 완료를 await하지 않는다. 새 entry 명령은 항상 거부하고 동기 hook 재진입과 최초 common-installer 재진입을 검사한다. **await 이후 Bootstrap의 공유 대기를 자기 hook에서 호출하는 순환은 자동 검출하지 못한다.** task-context 추적을 추가하지 않았으므로 프로젝트 hook의 계약으로 금지한다. 외부 await 공유를 전부 차단하는 방식은 사용하지 않는다. graceful 종료는 객체 파괴 전에 ShutdownAsync를 await한다. OnDestroy는 이미 요청한 종료를 다시 관찰해 같은 실패를 Console에 중복 출력하지 않으며 미요청 종료만 최선으로 요청한다.

## 호환과 사전 검사

기존 Bootstrap script GUID와 `_sceneRoot`, `_firstScenePath`, `_autoStart`, `_callbacks` 이름을 유지한다. 추가된 `_loadMode`는 Additive를 기본으로 초기화한다. BootstrapCallbacks는 SceneTransitionCallbacks의 Obsolete adapter로 기존 ConfigureGameAsync를 지원한다. 소비 assembly 재컴파일은 필요하며 precompiled binary 호환성은 보장하지 않는다.

기존 Inspector·컴파일 후 진단·live 미저장 Play gate·BuildPlayerOptions.scenes 검사에 새 설정 검사를 연결한다. Single/공용 수명 모순과 영속 실행자/callback의 잘못된 배치를 runtime 이전에 진단한다. C# 컴파일러가 scene 참조를 검사하는 것은 아니다. 조건 ID·구역 graph·전환 정의의 Editor 검사는 Phase 5다.

## 검증과 후속 범위

[Phase 2 검증](validation/game-scene-entry/README.md)은 실제 Red/Green, 최종 EditMode 182/182·PlayMode 115/115, 실패·skip 0, native Single/Additive·취소/정리·빌드/Play invalid-mode 차단을 기록한다. [최초 Bootstrap 증거](validation/bootstrap-system/README.md)와 [callback Phase 1 증거](validation/scene-transition-contracts/README.md)는 이전 범위의 기록이다.

Phase 3A는 연속 주 흐름 교체, 3B는 파생 구역 수명이다. 조건/정의는 Phase 4다. 소비 프로젝트·성공 Player 빌드/실행·가림막 시각 UX·Reload 비활성 반복 Play는 이번 단계에서 확인하지 않았다. 내부 테스트 통과를 전체 코어 배포 완료로 확대하지 않는다.
