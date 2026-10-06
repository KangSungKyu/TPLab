# BootstrapSystem 최초 진입

2026-10-06. 구현·MyLab 내부 자동 검증 완료. 최초 진입만 소유하며 일반 게임 씬 교체 API는 [GameSceneManager 후속 범위](GAME_SCENE_MANAGER_DRAFT.md)다. 코어는 특정 Init/Hub 씬 자산이나 프로젝트 서비스를 자동 생성하지 않는다.

## Inspector 구성

1. 실행 첫 씬에 top-level GameObject를 만들고 기존 `SceneOwnedRoot` 또는 `SingletonSceneRoot`를 연결한다. `Persist Across Scenes`는 false로 둔다. Bootstrap 씬 자체가 유지되므로 DontDestroyOnLoad를 추가하지 않는다.
2. `BootstrapSystem`을 추가하고 `Scene Root`에 해당 host, `First Game Scene`에 목적지 SceneAsset을 지정한다. `Auto Start`가 true이면 Start에서 실행하고 false이면 프로젝트가 `BootstrapAsync`를 호출한다.
3. 목적지에도 유일한 top-level 활성 root를 둔다. 게임 씬은 SceneOwnedRoot를 권장한다. Bootstrap과 게임 양쪽에 SingletonSceneRoot를 사용하면 같은 Instance를 중복 소유하므로 사전 검사에서 거부한다.
4. Build Profiles의 실제 포함 씬 목록에서 Bootstrap을 첫 순서로, 목적지를 enabled 상태로 포함한다. Editor에서는 Bootstrap을 열고 목적지는 닫은 상태로 Play한다. 다른 Play Mode Start Scene을 지정하면 검사에서 차단한다.
5. 공용/게임 root의 installer 목록에 필요한 프로젝트 시스템을 명시한다. 코어는 전역 manager를 검색하거나 프로젝트 서비스를 암묵적으로 주입하지 않는다.

## 실행·소유권

`설정 검사 → 가림막 표시 → Bootstrap root.PrepareAsync → 목적지 Additive 로드 → active scene 설정 → ConfigureSceneAsync → 게임 root.PrepareAsync → PreparePresentationAsync → 가림막 해제 → GameScene 공개`

설정 검사는 준비·연출·로드의 부작용 전에 실행한다. root의 타입, top-level 여부, 활성/enabled 상태, Bootstrap과의 동일 씬 소속, 중복 host, persistence 및 목적지의 정규화된 Assets 경로를 검사한다. runtime은 실제 Player 목록 포함 여부와 이미 로드된 목적지도 거부한다. 잘못된 설정으로 인한 실행 전 오류는 호출자에게 직접 반환된다.

Unity의 native activation은 시스템 준비와 다르다. 목적지의 Awake/Install은 SetActiveScene과 ConfigureSceneAsync보다 먼저 실행될 수 있다. Install에서 만드는 GameObject는 root의 자식으로 만들거나 목적지 씬에 명시적으로 배치한다. ConfigureSceneAsync는 이미 설치된 소비자에게 빌린 공용 참조를 연결하는 지점이며, 활성 root의 Configure를 다시 호출하는 지점이 아니다. PrepareAsync에서 해당 참조와 비동기 콘텐츠의 준비를 기다린다. 게임 controller는 명시적 준비 신호를 기다려야 하며 가림막이 임의 Awake/Start 게임 로직을 막지는 않는다. [Unity SetActiveScene](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html)

SceneTransitionCallbacks를 상속한 프로젝트 컴포넌트를 Bootstrap 씬에 두면 `ShowCoverAsync`, `ConfigureSceneAsync`, `PreparePresentationAsync`, `HideCoverAsync`, `OnFailure`를 Inspector에서 연결할 수 있다. callback은 완료될 때까지 await하며 코어 기본 UI는 없다. 첫 프레임 가림막과 입력 차단은 프로젝트가 초기 표시 상태로 준비한다. 실패·소유자 취소는 reveal을 건너뛰며, reveal 자체가 실패하면 기존 SceneRootFlow가 가림막을 다시 요청한다.

```csharp
bootstrap.Configure(bootstrapRoot, "Assets/Scenes/HubScene.unity", autoStart: false, callbacks: presentation);
await bootstrap.BootstrapAsync(cancellationToken);
// GameScene은 성공적으로 준비·표시된 씬이다.
```

동시 호출은 한 번의 entry 작업을 공유한다. 전달한 cancellationToken은 해당 호출자의 대기만 취소한다. 실제 진입을 멈추려면 소유자가 ShutdownAsync를 호출한다. 실패 후 자동 재시도하지 않으며 Configure도 실행 시도 뒤에는 거부한다. 종료한 컴포넌트는 terminal이다.

native LoadSceneAsync는 취소되지 않는다. 소유자가 취소해도 늦은 로드 완료까지 기다리고 자기 작업이 로드한 목적지의 root를 ShutdownAsync한 뒤 씬을 해제한다. 원래 root나 외부에서 로드한 씬을 정리 대상으로 삼지 않는다. [Unity Additive](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html)

```csharp
await bootstrap.ShutdownAsync();       // 진입 중단 또는 게임 가림막 → root 해제 → 게임 씬 unload
await bootstrapRoot.ShutdownAsync();   // 외부 소유자가 공용 시스템을 최종 종료
```

성공한 게임의 종료는 가림막 callback 완료 뒤 진행하고 Bootstrap root는 유지한다. cleanup은 취소되지 않으며 해제 오류를 모아 호출자에게 반환한다. 종료 가림막 오류가 나도 자원 정리를 시도하며 성공으로 숨기지 않는다. OnDestroy는 최선의 비동기 정리만 요청할 수 있으므로 graceful 종료는 파괴 전에 ShutdownAsync를 명시적으로 await한다. 임의 Single 로드로 Bootstrap을 제거하는 흐름은 권장 수명 계약 밖이다.

## Callback 이전 코드 호환

BootstrapSystem의 `_callbacks` 필드 이름과 기존 script GUID는 유지한다. Configure의 callback 매개변수는 SceneTransitionCallbacks다. 기존 BootstrapCallbacks는 이를 상속하는 Obsolete 어댑터로 남고 ConfigureSceneAsync를 기존 ConfigureGameAsync override에 연결한다. 새 코드는 SceneTransitionCallbacks/ConfigureSceneAsync를 사용한다. 기존 상속 소스와 Inspector 객체 참조는 유지되며 소비 assembly는 다시 컴파일해야 한다. precompiled binary 호환성은 보장하지 않는다.

Phase 1은 이 callback 교체만 runtime에 적용한다. Single/영속 공용 root·구역 수명 graph·조건/전환 정의는 [후속 계약](GAME_SCENE_MANAGER_DRAFT.md)이며 현재 Bootstrap의 Additive 고정과 persistence 거부를 해제하지 않았다. 제품 UI·연출 자산도 추가하지 않았다. 새 callback/기존 어댑터의 현재 검증은 [Phase 1 증거](validation/scene-transition-contracts/README.md)를 따른다.

## Runtime 이전 검증

| 시점 | 검사 및 실패 동작 |
|---|---|
| Inspector 변경 | 현재 root 참조·씬 자산·installer 오류를 HelpBox로 표시 |
| script reload/컴파일 완료 뒤 Editor, 씬 저장/변경 | 지연 검사로 live 미저장 객체와 저장된 build scene 데이터를 확인하고 중복 Console 진단 억제 |
| Play 진입 전 | 동일 검사와 Bootstrap 시작 씬/이미 로드된 목적지/Play Mode Start Scene을 확인하고 잘못된 진입을 취소 |
| Player build PrepareForBuild | BuildPlayerContext의 실제 BuildPlayerOptions.scenes 순서·포함 목록으로 검사하고 BuildFailedException으로 중단 |

C# 컴파일러는 serialized scene 참조를 검사하지 않는다. 여기서 컴파일 단계 검증은 **컴파일 성공 뒤 Editor 진단**이고, C# 컴파일 오류로 위장하지 않는다. 빌드 검사는 저장된 씬을 OpenPreviewScene으로 읽고 finally에서 모두 닫는다. 사용자 씬을 저장·교체하지 않는다. [Unity build processor](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.BuildPlayerProcessor.PrepareForBuild.html)

Bootstrap을 사용하는 빌드에는 정확히 하나의 Bootstrap, 첫 순서, 포함된 목적지, 목적지의 유일한 유효 root, 두 root의 installer 항목 null 여부·중복·소유권을 검사한다. Bootstrap이 없는 프로젝트의 빌드 오류 정책은 기존 Unity 흐름을 유지한다. 별도 setting.asset이나 자동 Bootstrap 생성 기능은 추가하지 않았다.

## 검증과 후속 범위

[실행 증거](validation/bootstrap-system/README.md): EditMode 176/176, PlayMode 96/96, 실패·skip 0. Bootstrap 전용 설정 22건, 실제 씬 진입 9건을 포함한다. 실제 BuildPipeline·Play invalid-root 차단과 임시 fixture 제거를 확인했다.

일반 GameSceneManager의 연속 씬 교체·이전 게임 보존/해제 계약은 후속 구현이다. 소비 프로젝트 가져오기, 성공적인 Player 실행, 가림막 시각 UX와 Domain/Scene Reload 비활성 옵션의 반복 Play는 미검증이다. 이 기록은 해당 항목의 완료를 뜻하지 않는다. 기존 사용자 InitScene·render 설정·SceneTemplateSettings는 수정하지 않고 보존했다.
