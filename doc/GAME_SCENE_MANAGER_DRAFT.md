# GameSceneManager 계약과 단계별 구현

2026-10-06. 사용자 검토를 반영한 설계 계약. Phase 1 callback 공용화, Phase 2 최초 진입·Bootstrap 위임, P0 명시적 로더 및 Phase 3A 주 씬 교체를 구현했다. **파생 구역 수명 tree·전환 정의·조건 판단의 runtime 구현은 후속 Phase**다. 최초 진입과 주 씬 교체의 Single/Additive 및 공용 영속 수명 선택을 제공하며 기본 권장은 Bootstrap 유지 + Additive다. 선행 계약은 [SceneRoot](SCENE_ROOT.md), [비동기 수명](ASYNC_SCENE_LIFECYCLE.md), [자산 소유권](RESOURCE_MANAGER.md)이다.

## 공용 수명과 로드 모드

Bootstrap을 첫 씬으로 실행한다. 첫 진입과 이후 전환 모두 프로젝트가 로드 모드를 선택하며, 권장 기본값은 Bootstrap 씬 유지 + Additive다. Singleton 접근과 영속 수명은 독립적인 선택이다.

| 공용 수명 설정 | 유지 대상 | 허용 모드 |
|---|---|---|
| Bootstrap 씬 유지 | 씬과 공용 root | Additive 로드 후 선택적 해제 |
| 공용 root 영속화 | DontDestroyOnLoad root와 필요한 객체; Bootstrap 씬은 해제 가능 | Single·Additive |

네이티브 Single은 모든 기존 일반 씬을 해제한다. Bootstrap 또는 다른 일반 씬 유지와 Single을 동시에 지정하면 부작용 전에 거부한다. Additive로 대신 실행하면서 Single인 것처럼 표시하지 않는다. [Unity Single](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Single.html)

공용 root는 ResourceManager·DataTableManager·GameSceneManager를 한 번 생성·준비한다. 게임 전환은 공용 root를 Shutdown하지 않는다. Single에서는 전환 실행자·callback·가림막·필요한 EventSystem과 UI 참조도 살아남아야 한다. 파괴되는 Bootstrap 객체를 영속 manager가 참조하는 구성은 거부한다. 영속 객체의 실제 씬 소속은 원래 Bootstrap 씬과 다를 수 있으므로 저장된 씬 사전 검사와 runtime 영속 소유권 검사를 구분한다. [Unity 영속 객체](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)

Bootstrap 공용 수명 최종 종료는 외부 소유자가 manager의 게임 정리 완료 뒤 공용 root.ShutdownAsync를 기다린다. manager 설치/준비 중 최초 진입을 기다리고 최초 진입이 공용 root 준비를 기다리는 순환은 금지한다. 두 root에서 SingletonSceneRoot.Instance를 중복 소유하지 않고 게임·구역 root는 SceneOwnedRoot를 권장한다.

## 전환 경로와 실행 중 수명

설정은 이동 가능한 전환 경로 graph를 표현한다. Hub → Main → Hub처럼 순환을 허용한다. 실행 중 등록은 현재 로드된 씬의 **순환 없는 수명 tree**다. 파생 인스턴스는 부모 하나를 가지며, 공용 세션 root는 유지되는 Bootstrap 씬 또는 영속 공용 객체를 의미한다.

```text
공용 세션
└─ Main [주 흐름]
   ├─ Dungeon A [파생, Main에 수명 종속]
   └─ Dungeon B [파생, Main에 수명 종속]
```

씬 경로는 정의를 식별하고 실제 로드된 인스턴스는 Scene handle 등 별도 runtime 식별로 추적한다. 경로/이름만 보고 다른 인스턴스를 해제하지 않는다. 첫 구역 구현은 동일 경로의 동시 중복 로드를 거부하며, 반복 진입은 새 인스턴스로 등록한다. 같은 asset의 여러 동시 인스턴스는 로드 결과 식별·지원 backend 검증 후 별도 확장한다.

등록 정보는 인스턴스·부모·주/파생 역할·준비/종료 상태를 가진다. 주 게임 씬, Unity active scene, 프로젝트 입력/포커스 대상은 구분한다. depth는 자식 우선 해제에 사용하며, 프로젝트 priority는 포커스 비교에 사용할 수 있다. priority로 다른 씬을 암묵적으로 해제하거나 UI sorting order를 결정하지 않는다. 포커스는 명시적 선택을 기본으로 하고 자동 priority 선택은 별도 정책으로 둔다.

## 작업과 소유권

| 작업 | 동작 |
|---|---|
| 주 흐름 교체 | 목적지 로드 모드는 Single/Additive 선택, 지정된 이전 흐름과 종속 구역 정리 |
| 파생 구역 추가 | 부모 유지 + Additive 로드, active scene 유지/목적지 선택을 명시 |
| 파생 구역 제거 | 해당 root ShutdownAsync 대기 후 해당 인스턴스 언로드 |

부모는 파생 씬의 유지 여부와 수명 책임을 가진다. 파생 씬은 용도 완료 시 자기 종료를 요청할 수 있고 부모도 명시적으로 자식을 종료할 수 있다. 실제 조건 검사·해제·언로드·등록 갱신은 공용 GameSceneManager 한 곳에서 수행한다. root가 직접 Unity 언로드로 등록을 우회하지 않는다.

부모 교체/제거는 종속 자식부터 정리한다. 독립적으로 유지할 씬은 부모 종속 관계로 등록하지 않는다. 자신의 subtree 밖을 해제하는 요청은 권한/정의를 검사한다. 같은 인스턴스의 진행 중 제거 요청은 기존 정리를 공유하고, 그 외 겹친 상태 변경은 거부한다. 임의 큐·자동 interruption은 첫 범위에 없다.

전환 전 실제 로드 목록과 명시적 소유 기록을 대조하고, 유지·해제 대상과 남길 의존 관계를 계산한다. 미등록 일반 씬을 자동 삭제하지 않는다. Single은 미등록 씬도 해제하므로 사전 등록 없이 실행하지 않는다. 성공 공개/reveal 전에 예상 로드 집합·실제 집합·root 준비와 영속 공용 소유자의 생존을 확인한다. 외부 Single/직접 unload로 소유권이 깨지면 진단하고 신규 진행을 중단한다.

## 정의와 조건 판단

프로젝트 설정 asset은 전환 ID·작업 종류·출발/목적지·로드 모드·부모/해제 범위·active scene 선택·선택적 조건 식별자를 표현한다. first entry도 같은 요청/정의 모델로 연결한다. 코어에 씬 문자열·게임 enum·CSV 규격을 고정하지 않는다. 최초에는 Inspector 목록으로 제공하며 Animator graph editor나 매 프레임 자동 조건 검색은 추가하지 않는다.

조건은 root 객체에 연결된 작은 프로젝트 조건 판단 컴포넌트가 담당한다. ISceneRoot의 수명 API에 게임별 필수 메서드를 추가하지 않는다. 정의 ID 요청과 직접 코드 요청은 같은 검사 경로를 거쳐 조건을 우회하지 않는다. 조건이 없는 정의는 허용하지만, 조건이 필요한 정의에서 담당 컴포넌트가 없으면 설정 오류다.

검사는 읽기 전용으로 하고 저장/자원 소비 같은 작업을 숨기지 않는다. 기본 검사는 동기식이며 네트워크 승인 등 비동기 판단은 필요가 확인될 때 확장한다. 파생 구역 추가/제거의 출발 root와 부모 해제로 영향받는 모든 root의 조건을 명시하고, 전부 통과하기 전에 어느 root도 해제하지 않는다. 조건 거부는 정상적인 거부 결과이며 cover·로드·OnFailure를 호출하지 않는다. 검사 예외/잘못된 설정은 오류로 전달한다.

승인 후 상태 변경을 프로젝트가 차단하는 것을 기본 계약으로 한다. 최종 해제 시작 직전 필요한 조건을 다시 확인한다. 목적지를 이미 로드한 뒤 재검사에서 거부되면 작업 취소 경로로 후보를 정리하고 cover를 유지한다. 검사 내부에서 현재 전환이나 같은 root의 종료를 await하지 않는다.

## 준비 신호와 callback

Phase 1의 실제 공용 컴포넌트는 SceneTransitionCallbacks다. ShowCoverAsync → ConfigureSceneAsync → PreparePresentationAsync → HideCoverAsync 및 OnFailure를 제공한다. [Bootstrap 현재 구현](BOOTSTRAP_SYSTEM.md)이 이를 사용한다. 기존 BootstrapCallbacks는 폐기 예정 호환 어댑터로 유지한다.

ConfigureSceneAsync는 native Awake/동기 Install 이후 설치된 소비자에게 빌린 공용 참조를 연결한다. 활성 root를 Configure하거나 공용 서비스를 종료하지 않는다. root.PrepareAsync 완료/IsPrepared 확인 후 PreparePresentationAsync에 scene/root를 전달한다. 이것이 코드에서 시스템 준비 완료를 받는 지점이며 UI 준비·reveal·게임 시작 허용과 구분한다. 전환 API의 성공 완료는 모든 필요한 준비·reveal·최종 상태 검사를 포함한다. 해제될 이전 controller가 전체 결과 처리의 소유자가 되지 않게 한다.

가림막만으로 임의 Awake/Start/Update를 정지시키지 않는다. 게임 controller는 명시적인 진행 허용을 기다린다. 진행 허용은 필요한 준비와 reveal 완료 이후에 적용하고, 이미 허용한 게임을 보호하는 실패 경로에서는 다시 차단한다. 명시적 진행 hook의 구체 API와 실행 순서는 Phase 2/3의 최소 테스트로 확정하며 이번에 새 activation framework를 만들지 않는다.

callback·installer 안에서 자신이 참여한 전환 완료를 await하거나 재진입하지 않는다. manager는 상태 변경 전체에 대한 중복 요청을 부작용 전에 거부한다. OnFailure는 실행 실패·소유자 취소를 한 번 통지하고 예외는 호출자에게 남긴다. 종료 실패 통지와 상태 조회는 Phase 2에서 연결한다.

## 모드별 실행과 실패 경계

Additive 교체: 검사/조건 → cover → 목적지 로드·주입·root/화면 준비 → 해제 직전 조건 재검사 → 이전 subtree 종료·unload → 최종 검사·reveal·게임 허용. 목적지 준비 실패 시 후보만 정리하고 이전 root는 유지할 수 있다. 이전 root ShutdownAsync가 시작된 뒤에는 복귀를 보장하지 않는다. 두 씬이 함께 존재하는 동안 메모리·카메라·AudioListener·EventSystem의 프로젝트 구성을 확인한다.

Single 교체: 유지/소유권 검사·조건 → cover → 해제 직전 재검사 → 해제되는 root들의 자식 우선 graceful shutdown → native Single → 목적지 주입·root/화면 준비 → 최종 검사·reveal·게임 허용. 기존 씬은 destination 준비 이전에 종료된다. 마지막 일반 씬을 먼저 별도 unload하지 않고 native Single에 맡긴다. 공용 영속 root는 종료하지 않는다. [Unity 언로드](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html)

native 로드는 토큰으로 강제 중단하지 않는다. 취소 시 늦은 완료까지 소유하고 self-owned 후보를 정리한다. Single 실패/취소에서 마지막 일반 씬을 unload할 수 없으면 실패한 씬의 root 정리를 시도하고 loaded-but-unprepared 상태를 진단한다. 자동 빈 씬 생성/복귀는 하지 않는다. allowSceneActivation=false를 root 준비 대기의 수단으로 쓰지 않는다. [Unity activation 대기](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html)

호출자 토큰은 해당 await만 취소한다. 실제 전환 중단은 manager의 명시적 취소/종료 요청이 담당하며, 취소되거나 파괴되는 이전/부모 객체의 토큰을 실제 정리 작업의 수명으로 사용하지 않는다. 부모 수명은 정리 대상 결정에 쓰고 실행 수명은 공용 manager가 소유한다. root별 내용 준비는 해당 root의 수명 토큰을 존중한다. cleanup은 취소되지 않으며 이미 시작한 shutdown을 끝까지 기다린다.

실패·작업 취소 시 cover 유지, reveal 실패 시 cover 복구 요청, 정리/callback 오류는 원인과 함께 집계한다. 메시지 확인은 cover 해제를 허용하지 않는다. 실패 상태는 단계·원인·실제 남은 인스턴스·root 준비 상태·정리 오류를 제공한다. 정상 성공과 구분하고 Faulted에서는 상태 조회/명시적 종료를 허용하며 자동 재시도/복구는 제공하지 않는다. 취소된 caller의 대기와 owner 실패 통지는 구분한다.

## 화면·입력·수명

GameSceneManager는 전환 cover 시점과 실패 정책을 소유한다. 프로젝트 UI 컴포넌트가 실제 표시·애니메이션·입력을 처리하고, 시스템 메시지는 발신 시스템과 공용 UI 표현자가 담당한다. 일반 UI/알림 → cover/로딩 → 필수 오류 메시지 순서를 프로젝트가 지정한다. 전환과 모달은 각자 입력 차단을 소유하며 한쪽 완료로 다른 차단을 해제하지 않는다. 포인터 차단과 선택/키보드·게임패드 입력도 구분한다.

일반 화면의 기본 권장 구성은 top-level Screen Space - Overlay Canvas + 전체 Stretch Image(anchor 0..1, offset 0)다. UI root는 공용 owner가 명시적으로 관리하고 Single에서는 별도로 영속화한다. game camera viewport/FOV나 reference resolution을 고정할 필요는 없다. cover는 Safe Area 밖까지 Player 렌더 영역을 덮고 문구/버튼만 Safe Area를 따른다. 다중 display는 targetDisplay별 구성이 필요하며 RenderTexture/XR 출력은 별도 어댑터/검증 범위다. 현재 코어는 Canvas 자산이나 기본 UI 구현을 제공하지 않는다. [Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/class-Canvas.html), [Safe Area](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-safeArea.html)

## Phase와 완료 조건

| Phase | 범위 | 완료 조건 |
|---|---|---|
| 1 | 계약 문서·SceneTransitionCallbacks 공용화·호환 어댑터 | 기존 Bootstrap 순서/오류 보존, 새 타입 주입·준비/reveal 대기·기존 callback 회귀 |
| 2 | manager 최초 진입과 Bootstrap 위임, 첫 진입 모드/공용 수명 선택 | 유일한 씬 소유자, 두 모드 진입·취소·종료·공용 준비 순환 방지·상태 공개 |
| 3A | Single/Additive 주 흐름 교체 | 연속 교체·조건 경계·late load·모드별 준비/해제 실패 |
| 3B | 수명 tree·구역 추가/제거 | 부모 유지·자식 자발 요청·부모 명시 종료·자식 우선 정리·중복 해제 없음 |
| 4 | 정의 asset·ID/직접 요청·root 조건 컴포넌트 | 공통 실행 경로·거부 무부작용·누락/권한/영향 root 검사 |
| 5 | Inspector·compile 후 Editor·Play/build gate | 실제 build scene 목록, 모드/수명 모순·root/조건/중복 ID·정의 오류 차단 |
| 6 | 통합 예제·회귀·소비 프로젝트·Player | Bootstrap→주 흐름→구역 출입, 실제 구성/반복 Play/가림막·입력 검증 |

Phase 3은 먼저 코드 요청으로 씬 전환을 검증하고, Phase 4에서 프로젝트 조건 판단을 실제 연결한다. 조건 관련 검사는 Phase 4에서 실행한다. 임의 graph editor·자동 polling·DI container·자동 Bootstrap 생성은 추가하지 않는다. Editor의 게임 씬 직접 Play 자동 Bootstrap 도우미도 후속 별도 범위다.

### Phase 3A 확정 API와 선행 조건

2026-10-06 구현 및 [MyLab 내부 자동 검증](validation/scene-replacement/README.md)을 완료한 계약이다. 파생 구역과 조건은 다음 단위다.

- `ReplacePrimaryAsync(SceneTarget, LoadSceneMode, CancellationToken)`와 기존 사용성을 유지하는 Build path overload를 제공한다. 최초 진입이 성공했고 현재 주 씬/공용 root/실제 씬 집합이 준비된 Ready 상태에서만 실행한다.
- `WaitForEntryAsync`는 최초 진입 이력이다. `WaitForTransitionAsync`는 마지막으로 승인된 상태 변경 작업의 공유 완료를 기다린다. 호출자 token은 기다림만 취소하며 `CancelTransition`과 terminal `ShutdownAsync`는 현재 소유 작업·늦은 native 완료까지 기다린다.
- 첫 진입 이력과 현재 작업의 완료 소스·취소 수명을 분리한다. 상태 변경은 한 번에 하나이고 전환마다 OnFailure를 한 번 통지한다. 부작용 전의 설정/겹친 명령 거부는 실행 실패 통지가 아니다.
- 이전 primary와 후보는 각 실제 LoadedScene 결과와 root를 보유한다. 후보를 얻은 순간 먼저 소유하고 검증한다. `OwnedScenes`는 실패 시에도 실제 남은 소유 씬을 읽기 전용으로 제공한다. 성공 primary, 전환 후보, Unity active scene을 혼동하지 않는다.
- Additive에서는 후보 root·화면 준비 뒤에 이전 root Shutdown과 결과 Unload를 기다린다. 그 이전 후보 실패는 후보만 정리하고 이전 root를 유지하되 Faulted/가림막 유지/진행 중단으로 남긴다. 이전 종료가 시작된 뒤의 복귀는 보장하지 않는다.
- Single에서는 영속 공용 수명과 전체 해제 집합을 먼저 검사한다. 이전 root graceful Shutdown을 기다린 뒤 native Single을 실행한다. 마지막 일반 씬을 먼저 별도 unload하지 않는다. Single이 이전 씬을 해제한 뒤에도 이전 결과의 backend 해제를 끝까지 관찰한다.
- 명령별 실제 집합과 최초의 유지 대상 집합을 구분하고, 후보 추가·이전 해제 단계에 맞는 예상 집합을 검사한다. 미등록/유지되어야 하는 일반 씬이 있는 Single은 거부한다. 공용 root는 게임 전환에서 종료하지 않는다.
- Phase 3B의 부모/자식/priority·registry와 Phase 4 조건 실행은 이 단계에 넣지 않는다. 실패 후보와 이전 씬의 잔여 소유권을 먼저 검증한 뒤 다음 단위로 확장한다.

### Phase 3B 예정 API와 실행 경계

- `UniTask<Scene> AddDerivedAsync(SceneTarget target, Scene parent, bool activate = false, int priority = 0, CancellationToken cancellationToken = default)`는 준비된 등록 부모를 유지하며 Additive로만 로드한다. 성공 결과는 이 명령이 실제로 추가한 Scene 인스턴스다. Build path adapter도 같은 경로를 사용한다. 부모는 현재 primary 또는 준비된 derived 인스턴스여야 한다.
- `RemoveDerivedAsync(Scene scene, Scene requester, CancellationToken cancellationToken = default)`는 대상 자신의 요청 또는 등록 ancestor의 요청만 허용한다. requester/scene은 실제 Scene 인스턴스이며 경로를 권한으로 사용하지 않는다. caller 코드는 그 인스턴스를 소유한 controller에서 호출하는 계약이다.
- `RegisteredScenes`의 읽기 전용 snapshot은 SceneRegistration(실제 Scene, 부모 Scene, Primary/Derived 역할, 프로젝트 priority, root 준비/종료 상태)을 제공한다. Primary의 부모 Scene은 default로 공용 세션 경계를 표시한다. 실패한 후보는 OwnedScenes 진단에 남길 수 있지만 성공 등록과 구분한다.
- Additive 추가의 active 유지/선택은 명시한다. primary GameScene과 active scene은 별개이며 manager가 기록한 예상 active scene을 검증한다. priority는 정보만 제공하고 자동 포커스·UI 정렬·해제를 유발하지 않는다.
- primary 교체 또는 derived 제거는 대상 subtree를 자식 우선으로 정리한다. 같은 대상의 진행 중 제거는 기존 완료를 공유하고 caller token은 기다림만 취소한다. 다른 상태 변경은 부작용 전에 거부한다. terminal Shutdown은 현재 작업의 늦은 완료와 모든 남은 owner를 기다린다.
- 후보 준비 실패는 해당 후보만 정리하고 기존 tree를 유지하되 Faulted로 진행을 중단한다. 해제 실패는 실제 잔여 인스턴스·오류를 기록한다. 외부 직접 load/unload/active 변경은 진단하며 자동 삭제·복구하지 않는다.

각 단위는 Red→Green→정리, 관련 회귀·컴파일/Console·증거·회고로 종료한다. Phase의 필수 자동 검사가 통과하면 승인된 track 통합을 진행한다. 이번 전체 작업의 main 통합에는 최종 명시적 사용자 확인도 필요하며, 사용자 부재 시 [track의 대기·절전 정책](SCENE_TRANSITION_TRACK.md)을 따른다. 역사적 [Bootstrap 설계 회고](retrospectives/2026-10-06-11-bootstrap-additive-design.md)와 [최초 진입 회고](retrospectives/2026-10-06-12-bootstrap-system.md)는 이전 범위의 기록이며 현재 계약과 구현 상태를 대신하지 않는다. Phase 1 검증은 [callback 증거](validation/scene-transition-contracts/README.md), Phase 2 검증은 [최초 진입 증거](validation/game-scene-entry/README.md)를 따른다.
