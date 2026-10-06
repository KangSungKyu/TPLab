# GameSceneManager 기본 사양과 Bootstrap 권장안

2026-10-06. 상태: 권장 구조 확정·Bootstrap 최초 진입 구현·일반 전환 설계 초안. 기준 `main/0d22ed3`, 작업 `codex/bootstrap-additive-design`. 사용자는 기본 씬 로드·활성화·해제, 중복 전환 방지, 실패·취소 처리를 기본 사양으로 하고 **Bootstrap을 첫 씬으로 실행한 뒤 게임 씬을 Additive로 로드**하는 방식을 공용 코어의 권장안으로 선택했다.

선행 계약: [SceneRoot 주입](SCENE_ROOT.md), [비동기 준비·해제·가림막](ASYNC_SCENE_LIFECYCLE.md), [자산 수명](RESOURCE_MANAGER.md), [데이터 수명](DATA_TABLE_MANAGER.md). 초기 설계 단위는 문서만 변경했다. 이후 [BootstrapSystem](BOOTSTRAP_SYSTEM.md)의 최초 진입·설정·Editor/build 사전 검사를 구현했다. 실제 프로젝트 Bootstrap 씬·Build Profile과 일반 GameSceneManager 전환은 후속 범위다.

## 권장 씬 구성과 소유권

프로젝트의 실행 시작 씬을 Bootstrap으로 지정한다. Bootstrap이 공용 서비스를 생성·주입·준비하고 이후 게임 씬을 `LoadSceneMode.Additive`로 로드한다. Bootstrap은 게임 씬을 교체하는 동안 계속 로드된 상태를 유지한다. Additive는 기존 씬을 언로드하지 않는 Unity 기능이다. [Unity Additive](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html)

| 소유자 | 책임 | 해제 시점 |
|---|---|---|
| Bootstrap root | 공용 ResourceManager·DataTableManager·GameSceneManager 생성/주입/준비 | 앱 또는 공용 세션의 명시적 최종 종료 |
| 게임 씬 root | 씬별 controller·pool·UI와 필요한 자산 준비; Bootstrap 서비스 참조 사용 | 해당 게임 씬 교체·언로드 전 |
| Bootstrap보다 짧지 않은 연출 소유자 | 가림막 표시/해제, 입력 차단, 실패 통지 callback 제공 | 전환 종료 후 또는 최종 종료 |

Bootstrap root는 **SceneOwnedRoot를 우선 권장**한다. Bootstrap 씬을 유지하므로 root의 `Persist Across Scenes`는 기본 false로 두고, 이 경로에서 DontDestroyOnLoad를 추가하지 않는다. 전역 접근이 필요한 프로젝트는 기존 SingletonSceneRoot 선택을 사용할 수 있다. manager마다 Singleton 상속을 강제하지 않는다.

씬 이름·경로·첫 게임 씬·서비스 구성을 프로젝트가 명시한다. 코어에 `Bootstrap`, `MainScene` 문자열이나 게임별 enum·전역 manager 목록을 고정하지 않는다. Bootstrap과 게임 씬에 같은 공용 서비스를 이중 생성하지 않고 게임 씬은 받은 참조의 소유자를 종료하지 않는다. 가림막·EventSystem·카메라·AudioListener 배치는 프로젝트가 정하고, 두 씬이 함께 로드된 동안 중복 동작하지 않게 구성한다.

## 최초 진입

`Bootstrap 시작 → 가림막/입력 차단 → 공용 root 준비 → 게임 씬 Additive 로드 → 게임 root 주입·준비/표시 준비 → 게임 진행 허용 → 가림막 해제`

- 최초 가림막은 Bootstrap에 기본 표시한다. 공용 root의 Install 성공이나 Singleton Instance 존재만으로 진행하지 않고 `PrepareAsync` 완료와 `IsPrepared`를 확인한다.
- 게임 씬 로드 완료 이후 실제 게임 root에 공용 서비스를 명시적으로 연결하고 씬별 `PrepareAsync`와 화면 준비를 기다린다. 모든 준비 성공 전에는 입력·게임 시작을 허용하지 않는다.
- 게임 씬을 Unity의 active scene으로 명시적으로 설정하고 반환 결과를 확인한다. 이는 새 GameObject의 기본 소속과 lighting을 정하는 기능이며 렌더링/게임 진행 허용과 다르다. 기본 생성 위치가 필요한 installer 실행 전에 active scene을 정하거나 생성 객체를 목적지 씬에 명시적으로 배치한다. [Unity SetActiveScene](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html)
- 네이티브 scene activation, SetActiveScene, 프로젝트 게임 시작을 구분한다. 로드한 객체의 Awake/Start는 시스템 준비 전에 실행될 수 있으므로 프로젝트 controller는 명시적 시작 신호를 기다린다. 가림막이 임의 게임 로직 실행까지 막지는 않는다.
- Bootstrap 준비 실패·취소 또는 게임 씬 준비 실패·취소 시 진행을 중단하고 가림막을 유지한다. 자동 재시도·자동 복귀는 기존 사용자 정책대로 제공하지 않는다. 실제 생성한 부분 상태의 정리는 각 소유자가 수행한다.

Bootstrap은 한 번만 설치·준비한다. 첫 게임 씬 진입과 이후 게임 씬 전환에서 공용 manager를 다시 초기화하거나 Shutdown하지 않는다.

## 게임 씬 전환의 기본 사양

GameSceneManager는 Bootstrap이 소유하는 전환 조정자로 설계한다. 한 조정자에서 겹친 전환은 거부하고, Bootstrap 자신을 일반 게임 씬 교체·언로드 대상으로 지정하는 요청도 거부한다. 1차 범위는 유지되는 Bootstrap과 현재 게임 씬 하나의 교체이며 다중 월드 streaming은 후속 범위다.

- 가림막과 준비/해제 callback은 기존 [비동기 계약](ASYNC_SCENE_LIFECYCLE.md)을 재사용한다. 최상위 전환 소유자 하나가 연출을 제어하며 중첩 flow로 가림막을 중복 호출하지 않는다.
- 목적지 경로/등록 여부, 이미 로드된 목적지, 중복 Bootstrap과 잘못된 요청을 상태 변경 전에 검사한다. 네이티브 동작이 반환한 Scene을 식별하여 같은 이름의 다른 씬을 잘못 언로드하지 않는다.
- 전환 단계는 가림막 → 목적지 Additive 로드 → 목적지 주입/준비 → 기존 게임 root의 graceful shutdown/씬 해제 → 새 게임 진행/가림막 해제로 제안한다. active scene 설정 시점과 게임 객체 소속을 함께 관리한다. 이 정확한 전환 API·소유 상태·단계별 실패 정리는 구현 전에 확정한다.
- 목적지 준비가 성공하기 전 기존 root를 유지할 수 있으나 기존 root.ShutdownAsync가 시작된 후 정상 복귀를 보장하지 않는다. 기존 서비스를 종료한 뒤의 실패를 이전 게임 복구 성공으로 숨기지 않는다. 실패/취소는 준비한 목적지의 정리 여부와 현재 남은 씬을 진단하고 가림막을 유지한다.
- 게임 씬 root의 ShutdownAsync를 **await한 다음** UnloadSceneAsync를 수행한다. Bootstrap root에 ReleaseAndProceedAsync를 호출해 공용 서비스를 종료하는 방식으로 게임 씬을 교체하지 않는다. Bootstrap의 Shutdown은 최종 종료에서만 한다.
- Unity 네이티브 로드는 취소 토큰만으로 중단된다고 가정하지 않는다. 시작 후 취소는 늦은 로드 완료까지 소유권을 유지하고 자기 시도가 로드한 목적지를 안전하게 정리해야 한다. cleanup은 이미 취소된 토큰에 의존하지 않는다.
- `allowSceneActivation=false`를 게임 root.PrepareAsync 대기의 수단으로 사용하지 않는다. 0.9 정지는 로드 완료와 다르고 다른 async scene 작업도 지연시킬 수 있다. 첫 구현은 가림막 아래 activation을 완료한 뒤 주입/준비로 진행 허용을 제어하는 방향이다. [Unity allowSceneActivation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html)
- 씬 unload가 자산 handle 해제까지 보장하지 않는다. 씬별 owner는 pool·자산 참조를 수명 계약대로 정리하고 Bootstrap의 공유 자산은 Bootstrap 소유 규칙을 따른다. [Unity UnloadSceneAsync](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html)

이 권장안은 managed 게임 전환에서 Single 로드를 사용하지 않는다. 프로젝트가 외부에서 Single 로드로 Bootstrap을 없애면 유지되는 공용 수명 전제를 벗어난다. 종료된 root의 재사용이나 자동 재부팅은 코어가 암묵적으로 수행하지 않는다.

## Editor에서 게임 씬 직접 실행

1차 기본 흐름은 프로젝트의 Bootstrap에서 Play하는 방식이다. 게임 씬을 직접 Play했을 때 자동으로 Bootstrap을 추가하는 Editor 도우미·RuntimeInitializeOnLoadMethod·전역 auto-create는 이번 선택에 포함하지 않는다. 후속 필요가 확인되면 명시적 시작 설정과 중복 로드 방지·씬/Editor 설정 복원 계약을 정한다.

## 구현과 완료 조건

권장 구조는 이번 요청으로 선택됐지만 GameSceneManager의 public API·scene backend·정확한 commit/취소 경계는 구현 전 설계 대상이다. 기존 root/flow의 입력과 수명 계약을 바꾸지 않고 최소 외부 경계로 연결한다. 새 DI container·scene setting framework·게임별 Bootstrap 자산을 선행해서 만들지 않는다.

구현 단위는 전환 계약/실패하는 최소 검사 → Additive 로드/활성/해제와 실제 root 연결 → 같은 MyLab Editor의 실제 씬 회귀 순서다. 최소 검증은 Bootstrap 유지·한 번만 설치, 공용/게임 준비 후 진행, active scene/생성 객체 소속, 연속 교체·중복/잘못된 요청, 로드 전후 취소·늦은 완료 정리, 준비/해제 실패·가림막 유지, 최종 종료와 반복 Play다. 사용자 연출의 실제 시각 UX와 Player/소비 프로젝트 검증은 실행 증거를 따로 구분한다.

초기 설계 단위의 검증: 문서 링크·색인·diff/공백·기존 자산 bytes 보존 검사만 수행했다. runtime/테스트/씬/설정은 변경하지 않아 Unity 테스트 실행 대상이 없고 **실행 0건·미실행**이다. 문서 검사 결과는 [정적 검증](validation/bootstrap-additive-design.json), 결정과 다음 작업은 [회고](retrospectives/2026-10-06-11-bootstrap-additive-design.md)에 기록한다.

Bootstrap 최초 진입 구현과 현재 검증은 [BOOTSTRAP_SYSTEM.md](BOOTSTRAP_SYSTEM.md)를 따른다. 위 문서만 변경한 기록을 현재 runtime 검증으로 대신하지 않는다.
