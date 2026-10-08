# Game UI System 설계 검토 초안

2026-10-08. 설계 검토 기록이다. 사용자가 후속으로 P0~P7 구현 진행을 승인했다. 현재 적용 계약은 [UIContext 계약](GAME_UI_SYSTEM.md), 실제 구현/검증·통합 상태는 [track](GAME_UI_SYSTEM_TRACK.md)에서 확인한다. 이 설계 기록의 제안과 0.0.1 배포 API를 혼동하지 않는다.

사용자가 적은 `tplap.uisystem`은 저장소 이름에 맞춰 TPLab UI 모듈로 해석했다. P0에서 namespace/assembly `TPLab.UI`를 채택했다. 향후 설치 단위 `com.tplab.ui`와 배포 버전은 별도 범위다. 다음 버전 번호도 미정이다. 선행 계획은 [CORE_PLAN](CORE_PLAN.md#다음-버전-계획-2026-10-07)이다.

## 현재 구현에서 확인한 경계

- [ResourceManager](RESOURCE_MANAGER.md)는 자산을 빌려 주고 실제 Addressables handle을 소유한다. 호출자 취소는 대기만 취소하며 성공 자산은 manager 종료까지 유지한다. 개별 key 해제 API는 없다.
- [PrefabPool](../Assets/TPLab/Core/Pooling/PrefabPool.cs)은 비활성 clone의 onRent 뒤 활성화, 비활성화 뒤 onReturn을 제공한다. 기본 reset은 local transform이고 UI 상태 reset은 소비자가 맡는다. Dispose는 onReturn을 호출하지 않는다.
- [Input layer](../Assets/TPLab/Input/Runtime/InputLayerController.cs)는 ActionMap의 활성 상태를 관리한다. uGUI raycast·선택 focus·중첩 Panel의 상호작용은 별도다.
- [InputSystemUiScope](../Assets/TPLab/Samples/Input/SceneTransitions/Runtime/InputSystemUiScope.cs)는 현재 Samples의 프로젝트 소유 연결 예제이며 공용 runtime API가 아니다. runtime clone UI map을 연결하고 차단 경계에서 focus·누르고 있던 버튼의 재전달을 처리한다.
- [SceneTransitionCallbacks](../Assets/TPLab/Core/SceneManagement/SceneTransitionCallbacks.cs)는 프로젝트 UI를 빌려 사용하는 현행 계약이다. 가림막·로딩 표시 시점은 GameSceneManager, UI·연출은 프로젝트 callback이 소유한다. [로딩 표시 계약](SCENE_LOADING_PRESENTATION_DRAFT.md)을 유지한다.

## UIContext 명칭과 수명 (2026-10-08 사용자 선택)

사용자는 관리 진입점의 이름으로 UIManager보다 **UIContext**를 선택했다. UIContext와 UIManager를 별개의 필수 시스템으로 제공하지 않는다. UIContext는 연결한 root/화면 영역의 등록 정의, 선택된 HUD, 표시 목록과 owner tree, depth/입력/focus, 표시·종료 상태를 소유한다. Singleton 상속이나 전역 접근을 필수로 하지 않는다.

씬 사이에도 필요한 가림막·로딩·공용 안내는 영속 root의 UIContext에, 씬 전용 HUD·popup은 scene root의 UIContext에 연결한다. HUD 소유 popup은 그 HUD 종료에 따라 정리한다. 공용 Canvas에 표시해도 scene/HUD owner의 수명이 늘어나지 않으며, root 종료는 진행 중 표시 요청과 보관 인스턴스를 함께 정리한다. 정상 종료는 await하고 Unity 객체 파괴는 마지막 정리 경계로 검증한다.

Prepare/런타임은 실행 시점의 구분이고 Context/생성부는 책임의 구분이다. 기존 자산 공급자(ResourceManager 또는 직접 prefab 참조), 인스턴스 생성·재사용 경계, UIContext의 표시·입력·수명 소유권을 구분한다. 첫 구현은 생성 경계를 내부에 두고 별도 public UIFactory·DI container를 추가하지 않는다. 프로젝트별 생성 방식 교체 요구가 확인될 때만 public 경계를 검토한다. 생성부는 HUD 선택·depth·modal·focus를 소유하지 않으며 빌린 ResourceManager/InputManager를 종료하지 않는다.

현행 PrefabPool은 onRent 준비 뒤 활성화된 인스턴스를 반환한다. 생성 경계가 항상 비활성 결과를 반환한다고 가정하지 않고, 준비·표시 허가·graph/입력 반영과 실제 활성화 시점이 일치하도록 계약·PlayMode 검증을 먼저 둔다.

## 공용 모듈과 프로젝트의 책임

| 대상 | TPLab 제공 제안 | 프로젝트 선택 |
|---|---|---|
| Canvas / Panel | 지정 host 검증, 표시 수명·순서·최상단 관리, 종료 | Canvas/카메라·CanvasScaler·safe area·디자인·설정 연결 |
| Popup | 준비/열기/닫기/종료, 중복 정책, modal 입력·focus 정리 | 화면 종류·데이터·열기 조건·연출·결과 처리 |
| 자산 | 지정된 공급 경계에서 받은 prefab 사용, clone/pool 소유 | 직접 prefab 참조 또는 ResourceManager 연결, 자산 소유자의 범위 |
| 입력 | modal 기간의 차단 획득/반환과 uGUI 하위 상호작용 제어 | Input System action map·layer·우선순위·EventSystem 구성 |
| Virtual ScrollRect | 표시 범위 계산, cell 재사용, bind/unbind | 데이터·cell prefab·항목 선택·이미지/텍스트 공급 |
| 씬 전환 | callback 구현에 사용할 UI 표시 API | 가림막·로딩·게임 팁, SceneTransitionCallbacks 연결 |

한 GameUIManager가 ScrollRect의 데이터나 모든 화면을 관리하지 않는다. Popup 관리와 virtual scroll 컴포넌트는 독립적으로 사용할 수 있게 한다. 가시성·입력만 관리하고 게임 도메인, Time.timeScale 변경, 씬 로드 권한은 프로젝트에 둔다.

사용자는 HUD 선택과 modal/modeless popup 관리를 분리하는 방향을 지정했다. Canvas 구성은 별도로 선택하며 모든 popup마다 Canvas를 강제하지 않는다. 기본 표시 영역 예시는 HUD → Popup → Loading → Cover 순서다. cover 위의 오류/시스템 메시지가 필요하면 프로젝트가 별도 영역을 명시한다. 숫자 sortingOrder와 게임 입력 layer priority를 동일 값으로 취급하지 않는다. 아래 HUD·graph·Canvas 정책은 이 요구를 구현하기 위한 검토 제안이다.

## 활성화 API와 표시 수명의 명시 (2026-10-08 보완)

사용자 요구: SetActive와 enabled를 사용하는 공용 API는 용도를 구별할 수 있도록 이름·문서에 동작 범위를 명시한다. 아래 이름은 설명용 제안이며 아직 구현/API가 아니다. generic enabled라는 표현 대신 Canvas rendering과 GameObject activation을 구분한다.

관리 호출은 표시 준비/열기, 표시 종료/닫기, owner 최종 종료를 나눈다. 숨김 전략은 별도로 지정하여 같은 수명/입력 계약에 적용한다. renderer 토글과 managed Close의 의미를 같게 안내하지 않는다.

| 설명용 숨김 전략 이름 | 내부 동작 | 명시할 효과/제약 |
|---|---|---|
| DeactivateView | 해당 UI 인스턴스 GameObject.SetActive(false) | Unity OnDisable/Update/Unity coroutine 영향, 재활성화 시 OnEnable; 인스턴스/자산 해제 또는 모든 async 취소는 아님 |
| DisableCanvasRendering | 해당 UI 전용 Canvas.enabled=false | 그리기 정지; hierarchy/script를 유지하므로 입력/focus·표시 update/animation·구독은 표시 수명에서 별도 처리 |

기본 전략은 DeactivateView로 제안하고, 사용자가 독립 Canvas의 빈번한 교체를 위해 rendering 전략을 선택하게 한다. 전용 Canvas를 가진 UI/whole-host 요청에만 DisableCanvasRendering을 허용하며, 여러 UI가 공유하는 Canvas를 단일 popup 숨김으로 끄는 요청은 거부한다. 시스템/SceneRoot/InputManager/EventSystem을 함께 끄는 shared root를 표시 인스턴스의 대상으로 사용하지 않도록 검증한다.

CanvasGroup은 fade와 interactable/blocksRaycasts 표현에 사용하며 인스턴스 보관 전략과 분리한다. 예: 입력/focus 정리 → fade-out → 표시 수명 정리 → 선택 전략으로 숨김 → 보관 정책에 따른 반환/파괴. fade 실패/취소도 listener와 부분 표시 자원을 정리하고 실패를 전달해야 한다. 다시 표시할 때 데이터·부모·alpha와 새 표시 수명을 준비한 후 활성화/그리기·focus·입력 복원을 수행한다.

Popup의 managed Close는 이번 표시의 callback/listener/표시 작업·focus·자식 UI·입력 lease를 정리한 뒤 인스턴스 보관 정책을 적용한다. root의 Shutdown은 진행 중 요청과 보관 인스턴스까지 끝낸다. 직접 GameObject/Canvas 토글을 호출한 것만으로 이 정리가 완료됐다고 보고하지 않는다. 다른 modal/transition의 입력 차단은 계속 유지한다.

사용 문서/XML에는 대상 범위, Unity 활성 상태 영향, 표시·입력 효과, cleanup/token 취소, 보관/파괴·자산 handle 구분, 반복 호출·오류/취소·재표시 계약을 적는다. 사람/AI 문서에는 동일한 기본값과 범위를 제공한다.

## Popup의 프로젝트 연결과 구독 정리

TPLab이 소유할 범위는 표시/닫기 상태, 자산 공급 경계의 결과와 UI 인스턴스, 등록 host/depth/owner graph, modal/modeless·하위 입력/focus, UI 수명 callback, 표시 단위 cleanup과 취소다. game domain의 데이터 구조·통신 protocol·판매/저장/보상/권한·성공 조건을 소유하지 않는다.

프로젝트의 presenter/controller가 데이터를 조회·검증하고 view에 바인딩한다. 프로젝트는 클릭을 받아 사업 규칙을 수행하고 결과를 UI에 반영한다. TPLab은 presenter를 특정 framework로 강제하거나 UI button click을 게임 요청으로 자동 해석하지 않는다. 데이터 테이블·ResourceManager·network 서비스 참조는 프로젝트가 주입한다.

구독은 native C# event 또는 UnityEvent를 계속 사용한다. 공용 기능은 이번 표시 수명에 해지 작업을 등록하는 작은 경계면 충분하다. 설명용 `RegisterCleanup(Action)`에 RemoveListener/ -= /IDisposable.Dispose를 등록하고, 반환된 registration을 통해 조기 해지를 요청할 수 있는 안을 제안한다. 표시 종료 시 등록 역순으로 정리하고 해당 등록마다 cleanup은 한 번만 실행하며 이후 Close/Shutdown에서 다시 호출하지 않는다. IDisposable 등록 대상은 이번 표시가 소유한 구독/자원에 한정한다. 빌린 ResourceManager/InputManager/프로젝트 서비스 자체의 Dispose를 등록하지 않는다. 반환값의 소유권과 만료 scope에서 등록 실패 시 호출자 책임을 명시한다. 범용 event bus나 reflection을 통한 구독 자동 탐색은 만들지 않는다.

설명용 흐름은 listener 연결 → 동일 표시 수명에 해당 listener의 정확한 해지 작업 등록 → UI 사용 → 조기 해지 또는 표시 종료에서 해지다. 프로젝트/다른 화면이 연결한 listener까지 RemoveAllListeners로 지우지 않는다. 필요한 비동기 종료는 await 가능한 수명 hook에서 처리하고 동기 cleanup에 async void를 등록하지 않는다.

표시 token은 표시 종료/열기 실패/owner 종료에서 취소하고 재표시할 때 새로 만든다. owner token은 최종 종료까지 유지한다. 호출자 대기 취소와 실제 표시 요청 취소는 해당 API 계약에서 구분하고, 호출자 token 취소를 빌린 owner/service의 종료로 전파하지 않는다. 숨긴 view의 GetCancellationTokenOnDestroy에만 의존하지 않는다. callback에 token을 전달해도 임의 통신/UniTask가 자동 종료되는 것은 아니므로 프로젝트가 취소를 관찰하고 늦은 결과의 표시 세대를 확인한다. UI가 닫혀도 계속해야 하는 저장/구매 등 domain 작업은 별도 프로젝트 소유자/token을 사용한다. UI 종료가 이미 처리된 업무의 rollback을 의미하지 않는다.

수명 hook은 데이터 바인딩/표시 준비, 표시 완료, 닫기 시작, 표시 종료, owner 최종 종료에 연결할 수 있게 한다. Unity OnEnable/OnDisable과 관리된 Open/Close 완료를 같은 event로 취급하지 않는다. hook/callback 실패 시 아직 남은 cleanup·lease 반환·인스턴스 정리를 계속 시도하고 오류를 보존해 호출자에게 전달한다. 반복 종료는 같은 완료/실패를 공유하고 종료 callback의 자기 작업 재대기/재진입을 검증한다. 프로젝트의 business 성공/실패를 TPLab이 재정의하지 않는다.

## 팝업의 로드 시점과 보관 정책

로드·생성·표시·닫기·자산 해제는 서로 다른 단계다. 준비 요청은 표시하거나 modal 입력을 차단하지 않는다.

| 설정 축 | 선택지 제안 | 기본 제안 |
|---|---|---|
| UI 자산 준비 시점 | 최초 열기 / owner 준비 단계 / 씬에 배치한 참조 | 최초 열기 |
| 인스턴스 보관 | 닫은 뒤 파괴 / owner 종료까지 비활성 재사용 | 닫은 뒤 파괴 |
| 자산 소유 범위 | 공용 root / scene root / 해당 화면·기능의 소유자 | UI 용도와 일치하는 명시적 소유자 |
| 중복 열기 | 동일 owner·정의의 중복 거부 / 여러 개 허용 | 중복 거부 |

팝업의 표시 범위와 자산 캐시 범위를 따로 지정한다. 공용 Canvas에 표시하는 UI도 특정 scene이 소유할 수 있고, 공용 자산을 빌린 scene popup을 닫는다고 공용 ResourceManager를 종료해서는 안 된다.

- 가림막·로딩·필수 실패 안내: 공용 root에 배치하거나 준비 단계에서 로드한다. 기본 실패 표현은 실패할 수 있는 동일 로드에 의존하지 않게 한다.
- 인벤토리·설정: 최초 사용 로드 후 해당 owner 내 재사용을 프로젝트가 선택할 수 있다. 사용자가 펼치기 직전의 예상 로드는 프로젝트 코드가 명시적으로 요청한다.
- 상점·특정 dungeon 화면: 관련 scene/기능 owner에 등록하고 종료 시 그 owner가 정리한다.
- 드물게 쓰는 확인창: 최초 열기, 닫을 때 인스턴스 정리를 기본으로 한다. 준비 화면을 표시하지 않은 상태에서 로드가 오래 걸리면 프로젝트가 대기 표시와 중복 입력 제한을 선택한다.

자산 준비와 인스턴스 미리 생성은 동일 설정으로 묶지 않는다. 첫 단계는 자산 준비만 제공하고, Instantiate/layout 비용으로 문제가 확인될 때 제한된 warm-up을 추가한다. 모든 UI 자동 preload, 전체 프로젝트 prefab 검색, 화면 사용 빈도 자동 추정은 넣지 않는다.

## ResourceManager와의 수명 계약

현행 ResourceManager를 사용하면 Close 후 clone을 Destroy해도 prefab handle은 manager 종료까지 남는다. 따라서 이 기본값은 인스턴스 수를 줄이는 정책이며 즉시 자산 메모리 회수 보장은 아니다. Addressables handle 해제도 공유 bundle 메모리의 즉시 회수와 같지 않다.

기본 통합은 기존 root의 ResourceManager를 빌려 사용한다. 화면 단위 자산 해제가 실제 요구인 프로젝트는 다른 곳과 공유하지 않는 전용 ResourceManager를 해당 owner가 만들고, UI/pool 종료 → PlayMode clone 파괴 완료 → ResourceManager.ShutdownAsync 순서로 종료할 수 있다. UI가 빌린 manager를 임의 종료하지 않는다.

빈번한 popup마다 manager를 새로 만들거나 UI에서 Addressables.Release를 직접 호출하지 않는다. 개별 자산 eviction이 필요한 경우, 소비자별 asset lease를 ResourceManager에 추가하는 별도 설계로 다룬다. 이번 Popup API 안에 숨겨 추가하지 않는다.

## 프로젝트별 적용 범위와 설정

설치·등록·표시 소유자를 각각 선택한다. UI를 쓰는 프로젝트/root에만 installer 또는 script로 연결하며 Singleton을 필수로 만들지 않는다. 공용 root에는 공통 UI만 등록하고 scene root에는 해당 scene UI를 등록한다. Script와 Inspector는 같은 검증·호출 경로를 사용한다.

선택적 설정 asset에는 정의 ID, prefab 직접 참조 또는 명시적 provider용 key, 대상 host/layer, 준비 시점, 인스턴스 보관, modal 여부, 중복 정책을 둔다. 자동 source 추정은 하지 않는다. 설정은 등록·기본 정책만 담고 root·runtime 자원·살아 있는 인스턴스 상태를 담지 않는다. 사용 데이터와 개별 표시 인자는 열기 요청으로 전달한다.

등록·준비·표시는 서로 다른 단계이며 호출자에게 매번 세 단계를 직접 수행하도록 강제하지 않는다. 설명용 Register는 정의 ID·prefab 참조/provider key·host·정책을 검증·등록한다. PrepareAsync는 선택적인 자산 선행 준비이며 인스턴스 warm-up이나 표시·입력 차단을 의미하지 않는다. OpenAsync/SelectHudAsync는 필요한 준비를 내부에서 보장하고 동일한 생성·표시 경로를 사용한다. 직접 연결한 prefab은 이미 준비된 자산으로 취급하며 ResourceManager 호출을 강제하지 않는다. 이 이름은 아직 구현되지 않은 API 제안이다.

권장 연결은 Bootstrap 준비에서 공용 정의/host 등록, Scene Prepare에서 씬 정의/host/owner 등록과 첫 HUD·필수 자산 준비, 런타임에서 상황별 HUD 교체·popup 열기 요청이다. 등록만으로 모든 UI를 preload하지 않는다. 새 HUD 준비가 실패하면 기존 HUD를 유지하며 늦은 완료는 종료된 owner에 UI를 다시 표시하지 않는다. runtime의 공통 검증은 Inspector 지원 전에 구현하고 Inspector·script는 그 검증/호출 경로를 공유한다.

재사용 popup은 닫을 때 데이터 구독·버튼 listener·scroll 위치/속도·focus·animation과 비동기 작업을 정리한다. 프로젝트는 의도적으로 보존할 화면 상태를 별도로 관리하고 다음 열기에서 다시 주입한다. Pool.Dispose 시 onReturn이 호출되지 않는 점 때문에 종료 cleanup은 반환 callback에만 의존하지 않는다.

Single을 사용하는 공용 UI host는 실제 영속 root에 속해야 한다. 공용 Canvas 아래에 놓더라도 scene owner의 종료가 popup의 종료로 이어지게 명시한다. 매 frame scene 탐색으로 owner를 추정하지 않는다. Additive에서도 씬 간 Unity 객체 참조가 남지 않도록 owner 종료를 먼저 await한다.

## 비동기 Popup과 모달

제안 흐름: 요청 검증 → 자산 준비 → 비활성 인스턴스/데이터 준비 → 표시·입력 소유권 획득 → 열기 연출 → 표시 완료. 닫기 흐름: 상호작용 중지 → 닫기 연출 → listener/비동기 표시 작업 정리 → 비활성화·반환 또는 파괴 → 이번 popup의 입력 lease 반환 → 유효한 하위 focus 복원이다.

- 인스턴스별 식별 결과를 반환하고 닫기는 해당 인스턴스에만 적용한다. 다른 owner가 같은 정의를 열었다고 일괄 종료하지 않는다.
- 최초 단계에서는 같은 owner·정의의 중복 열기를 명시적으로 거부한다. 진행 중 작업과 표시 완료 결과를 혼동하거나 모든 호출자에게 하나의 close 권한을 자동 공유하지 않는다.
- 자산 로드가 늦게 완료돼도 이미 취소·종료된 UI 요청을 표시하지 않는다. scope 종료는 새 요청 거부, 진행 중 UI 요청 취소와 부분 생성 정리, 모든 표시/캐시 인스턴스 종료를 담당한다.
- 취소는 UniTask의 취소, 오류는 예외로 전달한다. 종료는 반복 호출에 같은 완료/실패를 전달하고 이미 시작한 cleanup을 외부 대기 취소로 중단하지 않는다. 열기/닫기 도중 반대 요청과 owner 파괴는 별도 상태 전이 테스트가 필요하다.
- 가장 위의 유효한 modal을 하위 UI 입력 경계로 삼는다. 그 경계 이상의 popup은 pointer 입력을 받을 수 있고 navigation·submit·cancel focus는 그중 한 대상으로 제한한다. modal 위 modeless도 허용하는 제안이며 아래 화면의 CanvasGroup/선택 경계를 따로 관리한다. 다른 modal이나 전환의 차단 lease가 있으면 하나를 닫아도 게임 입력을 복구하지 않는다.
- Input System을 사용하는 프로젝트 UI 모듈을 연결한다. Legacy 입력 fallback은 제공하지 않는다. uGUI 클릭 소비만으로 게임 InputAction이 차단되지 않으므로 입력 layer 연결이 필요하다.
- 기존 Input wrapper를 사용할 때 clone action map과 UI 모듈 연결 및 버튼 해제/focus 복원 계약을 유지한다. Samples의 InputSystemUiScope를 구현 완료한 공용 API로 안내하지 않는다.
- GameSceneManager의 cover/전환 lease와 popup lease는 별도 소유권이다. callback의 HideCover는 전환 소유 차단만 해제하며 UI에서 전환 성공이나 CanProceed를 임의 변경하지 않는다.

첫 UI assembly는 Core를 사용하되 Core가 UI를 역참조하지 않게 한다. TPLab.Input 연결은 주입 가능한 작은 경계로 두고, 상호작용하는 프로젝트는 Input System UI 모듈과 modal 차단 연결을 제공해야 한다. TPLab.Input 통합 assembly/package가 필요한지는 선행 의존성 분리 검토에서 확정한다. passive cell 가상화까지 입력 wrapper 설치를 강제하지 않는 방향을 제안한다.

## HUD 선택과 popup 관리 (2026-10-08 보완)

사용자 요구: 상황에 맞는 HUD를 선택하고, 원하는 시점에 modal/modeless popup을 표시·종료하며 depth/graph로 관리한다. 사용자 UI 배치 재량을 유지하면서 공용 Canvas 수준에서 가능한 갱신·렌더링·입력 정책을 제공한다.

각 UI context(root·화면 영역)에 선택된 HUD와 popup 목록을 둔다. 첫 제안은 context당 기본 HUD 하나이며 프로젝트가 HUD 정의를 등록하고 직접 전환한다. 합성 HUD는 선택된 HUD 안의 구성으로 다룰 수 있다. 분할 화면·복수 기본 HUD와 일반 그래프의 임의 다중 부모는 이번 첫 범위에 자동 추가하지 않는다.

HUD 선택은 가시성/데이터 준비와 자산/인스턴스 보관 정책을 조합한다. 새 HUD 준비 실패로 기존 HUD를 먼저 없애지 않는다. 기존 HUD 소유 자식 popup은 HUD 해제 전에 자식부터 닫는다. HUD 변경과 관계없는 공용 popup은 context 소유로 등록해 유지한다. 이전 HUD 해제 자체가 실패한 경우 새 HUD 선택을 성공으로 숨기지 않고 실패를 보고한다. 게임 상태로 HUD를 자동 추정하지 않는다.

Popup은 동일한 표시 단위에 modal/modeless 입력 모드를 설정하는 방식으로 제안한다. 화면 종류에 따라 서로 다른 prefab을 강제하지 않는다. 열 때 mode를 선택하고 Visible 상태에서 mode 변경을 허용한다. Opening에는 요청에서 고정한 mode를 사용하고 Closing/Closed에서 변경은 거부하는 첫 계약을 제안한다. 이것은 API 확정 전의 상태 제약이다.

modal 전환은 사전 검증 → 차단 lease 획득 → 하위 UI 상호작용/focus 재계산 → mode 확정 순서로 적용한다. 획득 실패는 기존 modeless mode를 유지하며, 입력 적용 실패는 성공으로 숨기지 않는다. 기존 Input controller가 faulted이면 scope 오류로 취급한다. modeless로 바꾸거나 닫을 때는 자신의 lease만 반환하고 남아 있는 modal/전환 경계로 입력을 다시 계산한다.

## 소유권 graph·표시 depth·Canvas의 분리

| 정보 | 용도 | 첫 제안 |
|---|---|---|
| 논리 parent | 부모 종료 시 함께 정리할 관계 | context 내의 단일 부모, 순환/다른 context 연결 거부 |
| 표시 순서 | UI 겹침, 입력 경계와 focus 후보 계산 | 표시 영역 + 같은 영역의 순번 |
| Canvas host | 실제 렌더링·rebuild 분리 범위 | 변경 빈도가 유사한 화면을 묶는 등록 host |

graph의 소유권 간선은 첫 단계에서 tree 형태로 제한한다. 표시 순서를 parent depth만으로 정하지 않는다. 자식 popup을 부모보다 앞에 두는 기본 규칙을 제공하되 서로 다른 branch의 순서는 context에서 정한다. 표시 영역 정책이 owner 관계보다 우선한다. 재정렬은 자식이 부모 뒤로 내려가거나 입력 허용 영역과 실제 렌더링 순서가 어긋나지 않도록 검증한다.

공용 Canvas에 표시한 popup도 scene/HUD parent를 논리 소유자로 가질 수 있다. 부모 종료는 leaf부터 정리하며, 현재 Canvas hierarchy를 바꾸지 않고도 graph 관계를 관리한다. 기존 SceneManager 수명 tree를 UI tree로 대신 사용하지 않고 root 종료와 UI owner 종료만 연결한다.

Canvas/Transform 구조는 사용자 배치를 따른다. graph parent 변경 때문에 SetParent를 자동 실행하지 않는다. SetSiblingIndex/sortingOrder는 open/close/명시적 순서 변경으로 실제 순서가 달라질 때만 조정하고 매 frame 전체를 재정렬하지 않는다. 서로 다른 Canvas host에 UI가 걸칠 경우 host의 고정 표시 영역과 depth mapping을 검증하고 임의의 전역 교차 순서를 보장하지 않는다.

가장 위 유효 modal을 입력 경계로 삼아 그 아래 UI를 막는다. 위에 놓인 modeless popup/자식은 pointer 입력을 받을 수 있으나 keyboard/gamepad focus는 유효한 한 대상에만 준다. depth 변경·mode 전환·닫기·HUD 선택마다 경계를 재계산한다. 저장된 focus가 파괴/숨김/차단 상태면 복원하지 않고 현재 경계의 유효 기본 대상을 선택한다.

입력 차단은 raycast, navigation/focus, gameplay ActionMap을 각각 다룬다. Canvas.enabled/alpha/sortingOrder만 바꾸는 것은 입력 차단 완료가 아니다. modeless라도 클릭한 UI와 gameplay에 같은 클릭이 동시에 전달되는 것을 허용한다는 뜻은 아니다. 해당 pointer의 UI hit와 게임 입력 구분 정책은 프로젝트 입력 연결에서 정한다.

InputLayerController는 첫 lease 뒤 등록이 동결되고 각 map은 한 layer에만 소속된다. 매 popup마다 map/layer를 동적으로 등록하거나 depth를 layer priority로 변환하지 않는다. 사전 등록된 modal 차단 layer의 독립 lease와 UI 내부 입력 경계 계산을 조합한다. 같은 UI map을 여러 popup layer에 중복 등록하지 않는다.

## TPLab이 제공할 Canvas host의 관리 범위

제안 컴포넌트 이름 `UiCanvasHost`는 아직 구현/API가 아니다. 사용자가 연결한 Canvas 또는 프로젝트 template로 구성한 Canvas의 등록 화면을 관리한다. 첫 구현은 기존 Canvas 연결 경로를 기본으로 하고 Canvas 자동 생성 필요성은 예제로 판단한다.

2026-10-08 사용자 명확화: 공통 HUD/Canvas host를 사용하고 프로젝트가 변경 빈도에 맞게 영역을 나눌 수 있도록 한다. StaticHud/DynamicHud 전용 타입, static/dynamic 필수 슬롯이나 자동 분류를 제공하는 규격은 두지 않는다. HUD/Popup이라는 역할과 Canvas의 batching/rebuild 경계는 독립적으로 연결한다. 프로젝트가 공유 Canvas 하나 또는 여러 Canvas를 등록해 사용하는 같은 경로를 제공한다.

- 공용 제어: 등록 화면의 표시 순서·활성/입력 상태, host별 표시 영역, whole-host Canvas 표시/숨김, 명시적 rebuild 분리, 변경 없는 setter와 반복 정렬 억제, 소유 종료 시 cleanup.
- 프로젝트 결정: prefab 내부 계층, Graphic/Material/font/atlas, 카메라·render mode·CanvasScaler·safe area, 화면 데이터·애니메이션.
- 설정/검사: host의 표시 영역 중복/순서 역전, child Canvas overrideSorting 범위, 관리되지 않는 nested Canvas·raycaster, ignoreParentGroups 등 입력 정책 우회 구성을 확인한다. 수정 가능한 관리 범위와 경고 범위를 구분한다. Unity 기본 Mask/스크롤 구성을 무조건 오류로 판정하지 않는다.
- 외부 영역: 프로젝트가 공용 host 밖에 만든 Canvas는 자동 이동·비활성화·material 변경하지 않는다. 등록 host 밖의 UI와 전체 화면 draw call 상한을 보장하지 않는다.

관리 host의 순서는 logical depth를 실제 UI 순서에 일관되게 반영해야 한다. 같은 Canvas에서는 sibling order, 독립 host에서는 host sorting 정책을 사용한다. 동적 정렬 요구가 고정 host 구성으로 표현되지 않으면 등록/요청에서 거부하거나 프로젝트가 표시 영역을 명시적으로 변경한다. batch 개선을 위해 alpha UI의 앞뒤 순서를 바꾸지 않는다.

## Rebuild와 batching의 최적화 제안

목표는 draw call 숫자의 최소화만이 아니라 목표 기기에서 UI CPU/GPU 시간과 열기/스크롤 spike를 줄이는 것이다. Canvas 분리는 rebuild 범위를 좁히지만 Canvas 간 batching도 분리하므로 양쪽 비용을 함께 측정해야 한다. Canvas 개수와 draw call 개수가 일대일이라는 보장도 없다.

먼저 구분할 비용은 layout 계산, Graphic mesh/material 갱신, Canvas batch 구성, GPU draw/overdraw다. Canvas가 geometry/batching을 분리해도 상위 LayoutGroup·ContentSizeFitter의 연결까지 끊는 것은 아니다. graph 분리 자체가 render 최적화를 만들지 않는다.

공통 Canvas host의 사용 예는 프로젝트의 변경 빈도에 따라 선택한다. 아래 네 영역은 사용자가 구성할 수 있는 예시이며 시스템이 제공하는 별도 HUD 타입·필수 host 종류·기본 생성 목록이 아니다.

| 프로젝트 구성 예 | 예시 | 분리 이유 |
|---|---|---|
| 변경이 드문 HUD 영역 | 프레임·아이콘·고정 라벨 | 자주 바뀌는 내용 때문에 함께 rebuild되지 않게 함 |
| 자주 함께 갱신되는 HUD 영역 | HP·재화·진행 타이머 | 동시에/비슷한 빈도로 바뀌는 값끼리 묶음 |
| Popup | 인벤토리·확인창 | HUD와 다른 open/close 수명 |
| Loading/Cover | 진행률·가림막 | 독립 표시 수명과 전환 연출 |

단순 popup은 Popup host에서 함께 batch하고, 큰 scroll/상시 animation 영역이 다른 화면까지 자주 dirty시키는 것이 측정되면 해당 subtree에 독립 Canvas를 선택한다. popup마다 독립 Canvas를 기본 생성하거나 화면 tree의 모든 node를 Canvas로 만들지 않는다. Canvas 경계를 넘어 LayoutGroup이 끊임없이 size를 재계산하지 않도록 anchors·고정 영역·layout 독립성을 같이 확인한다.

인벤토리는 화면 이름에 따른 독립 Canvas 규칙을 두지 않고 조건부 분리를 권장한다. 항목 수/가상화 cell 수·갱신 빈도·animation·같은 host의 다른 UI 비용을 보고 프로젝트가 연결한다.

| 인벤토리 구성 선택 | 적합한 조건 | 고려할 점 |
|---|---|---|
| 공유 Popup Canvas | 작은 목록, 변경이 드문 화면 | 별도 Canvas 관리 없이 기존 host 사용 |
| 인벤토리 전체의 독립 Canvas | 화면 전반이 크고 자주 갱신됨 | 다른 popup/HUD의 batch 구성과 분리, Canvas 간 batching 비용 측정 |
| 목록 subtree의 child Canvas | 프레임은 안정적이고 scroll/아이템 영역만 자주 바뀜 | 갱신 범위를 좁혀 격리, layout 연결·clip·raycaster 구성도 확인 |

어느 선택이든 같은 popup API·owner·depth·modal 정책을 사용한다. 창을 열 때 TPLab이 인벤토리 이름으로 Canvas를 생성/이동하지 않으며 사용자가 지정한 host와 prefab의 child Canvas를 존중한다. 독립 Canvas에 필요한 입력 경로는 프로젝트 설정/검사에서 확인한다. 이 권장은 CPU rebuild 격리 후보이며 최소 draw call이나 성능 개선을 사전 보장하지 않는다.


갱신 규칙:

1. 값이 달라진 필드만 표시한다. UI의 표시 정밀도에 맞춰 타이머 문자열 등을 바꾸고 모든 Graphic/RectTransform을 매 frame 다시 대입하지 않는다. 고빈도 데이터가 같은 view에 여러 번 도착하면 최종 표시값으로 모으는 작은 dirty 처리만 필요 시 둔다.
2. 기본 uGUI delayed rebuild를 사용한다. 현재 설치한 CanvasUpdateRegistry는 layout/graphic queue를 AddUnique로 등록하고 render 전 갱신한다. TPLab에 같은 종류의 전역 rebuild scheduler를 만들지 않는다. view 데이터 합치기와 Unity layout queue의 중복 제거는 서로 다른 역할이다.
3. ForceRebuildLayoutImmediate/Canvas.ForceUpdateCanvases를 cell bind나 개별 setter마다 호출하지 않는다. 즉시 layout 결과를 반드시 읽어야 하는 좁은 경우에만 해당 subtree를 한 번 계산하며 강제 갱신은 표시 준비 계약에서 드러낸다. layout callback 안에서 다시 구조/크기를 바꾸는 순환을 피한다.
4. 고정 cell 크기의 Virtual ScrollRect는 index로 content 크기·cell 위치를 계산하고 보이는 cell만 bind한다. 스크롤마다 전체 목록의 LayoutGroup/ContentSizeFitter 계산·재부모화·sibling 정렬을 반복하지 않는다. 첫 범위는 데이터 수 변화/viewport resize 시 content를 재계산한다.
5. Pool 반환은 비활성화 → reset → 저장 위치 이동, 대여는 대상 parent/위치·데이터 설정 → 활성화 순서다. 비교 가능한 동일 parent의 안정된 cell 재사용은 불필요한 reparent를 생략할 수 있다. 기존 Pool이 제공하는 onRent/onReturn 경계를 재사용한다.
6. 재사용 whole Canvas를 숨길 때 Canvas.enabled로 drawing을 멈출 수 있으나 input/raycaster/focus와 해당 view의 표시용 update/animation/구독도 함께 관리한다. 같은 host의 일부 popup을 숨기려고 공유 Canvas 전체를 끄지 않는다. alpha=0만으로 모든 표시 갱신 비용이 사라진다고 보장하지 않는다.
7. shared material·호환 texture/atlas를 권장하고 인스턴스 material 생성, 서로 다른 mask/clip·겹침 순서에 따른 batch 분리는 UI Profiler의 실제 이유로 검사한다. TMP 폰트 atlas·fallback, Mask 재질 등 사용자 content는 공용 manager가 강제로 교체하지 않는다.
8. 입력이 없는 Canvas의 GraphicRaycaster와 장식용 Graphic의 Raycast Target을 줄이는 것은 raycast CPU 최적화다. draw call 감소와 혼동하지 않는다. 불필요한 상시 Animator/보이지 않는 animation을 정지하는 정책도 프로젝트 callback과 협의한다.

Canvas 분할·표시 정책은 후보 기본값을 제공하고 Inspector/script로 프로젝트가 결정한다. 실행 중 자동 Canvas 재분할이나 batch 개수에 따른 UI 계층 변경은 하지 않는다. 기본적인 등록/설정 검사는 제공하고 실제 최적화 판단은 측정 결과로 한다.

## 구현 후 측정할 항목

구성 A(공유 Canvas), B(변경 빈도별 Canvas), C(popup별 Canvas)를 동일 asset·해상도·UI 데이터·장치 조건에서 비교한다. idle, 타이머/HP 갱신, popup open/close, 겹친 modal, 큰 목록 스크롤, cover/loading을 각각 측정한다. 전체 UI 기본값을 C로 정하지 않는다.

- CPU: UI Layout/Graphic 갱신, Canvas batch 구성 시간, open/scroll frame spike와 frame time 분포.
- GPU/렌더링: batch/draw call·vertices·overdraw, UI Profiler Batch Breaking Reason과 Frame Debugger의 실제 draw.
- 할당/수명: GC Alloc, 활성/보관 cell/instance 수, reuse/close/HUD 교체 뒤 listener·입력 lease·참조 잔존.
- 동작: 순서/입력 경계 일치, modal 위 modeless, runtime mode/depth 변경, focus 대상 파괴, HUD 교체의 자식 정리, Canvas 숨김 중 입력/업데이트 누출.

정확한 draw call 예산이나 성능 개선 수치는 target과 실제 UI가 정해진 후에 결정한다. 현재는 문서·코드의 정적 설계이며 Profiler/실제 HUD·popup·virtual scroll 실행 증거는 없다.

## Virtual ScrollRect의 첫 범위

기존 ScrollRect의 drag·inertia·scrollbar를 사용하고, viewport에 필요한 cell과 여유분만 생성/재사용한다. 가상화는 기본 ScrollRect의 content/scroll 동작에 추가할 표시 범위·recycle 기능이다.

첫 범위는 세로·고정 높이의 한 열, cell prefab 한 종류로 제한하는 안을 제안한다. data count·index별 bind/unbind는 프로젝트가 제공한다. 데이터 목록 전체의 복사와 N개 GameObject 생성을 강제하지 않는다. 가변 높이·grid·무한 pagination은 별도 요구가 확인되면 설계한다.

필수 계약은 빈 목록, 데이터 수 변경·viewport 크기 변경, 스크롤 위치 재설정, 큰 index 이동, 비활성화/재활성화, 파괴/취소다. cell 재사용은 old binding 정리 → 위치·새 데이터 설정 → 공개 순서로 한다. 비동기 이미지 로드에는 binding 세대/취소를 사용하여 이전 row의 늦은 결과가 새 row를 덮어쓰지 않게 한다.

핵심 검증은 전체 데이터 수 N을 크게 늘려도 활성 cell 수가 viewport·여유분에 의해 제한되는지, 스크롤/새 데이터에서 다른 행을 표시하지 않는지다. item별 permanent 구독과 ContentSizeFitter/LayoutGroup의 전체 N 항목 계산을 반복하지 않는다. 실제 Canvas rebuild·GC·frame time 수치는 구현 후 Profiler로 측정한다.

### 대규모 기능·성능 검증 (2026-10-08 사용자 추가)

사용자는 1,000 단위 등 대규모 cell 환경도 기능/성능 검증에 포함하도록 요청했다. 필수 전체 항목 수 N은 1,000과 10,000이며 작은 경계 사례 0·1·100도 유지한다. N은 표시할 데이터 항목 수이고 실제 GameObject cell 수와 별도로 기록한다. 전체 N개 cell 생성을 가상화 경로에 강제하지 않는다.

| 규모/구성 | 검증 목적 |
|---|---|
| 0·1·100 항목 | 빈 목록, 작은 목록, viewport보다 적거나 많은 데이터의 기본 계약 |
| 가상화 1,000 항목 | 대규모 행 정확성·cell 재사용, 기본 ScrollRect와 같은 N의 직접 비교 |
| 가상화 10,000 항목 | 전체 N 증가에 대한 확장성·큰 index/끝 경계·메모리/구독 잔존 |
| 기본 ScrollRect 1,000 항목 전체 cell 생성 | 같은 prefab/data·viewport·Canvas·입력/스크롤 시나리오의 비교 기준 |

P5 기능 gate는 실제 ScrollRect에서 처음/중간/마지막 행, 빠른 왕복·drag/inertia·큰 index 점프·scrollbar 이동, 데이터 수 10,000→1,000→0→1,000 변화와 위치 보정, viewport resize, 표시/종료·비활성 재활성, 파괴/취소와 이전 행의 늦은 이미지 결과를 검증한다. 노출 행/index/데이터가 정확하고 이전 binding·listener·표시 참조가 남지 않아야 한다.

cell 수는 활성 수, 보관 수, 현재 총소유 수(활성+보관), 누적 Instantiate/Destroy 수를 구분한다. 현재 총소유 상한은 viewport·여유분·resize로 관찰한 최대 필요 수와 명시적 보관 정책을 기준으로 정하고 N 증가만으로 확대되지 않아야 한다. 고정 viewport와 준비된 pool의 안정된 스크롤에서는 누적 Instantiate가 계속 증가하지 않는지 검사한다. 종료 후 파괴/다음 열기 재생성은 선택한 보관 정책에 따른 정상 동작과 누출을 구분한다.

P7 성능 비교는 기본 ScrollRect 1,000 ↔ 가상화 1,000을 같은 조건으로 측정한다. 가상화 10,000은 별도의 확장성 결과이며 이를 기본 1,000과 비교해 같은 N의 개선율로 표현하지 않는다. 동일 prefab/data·이미 준비된 자산·해상도·viewport·Canvas 구성·장치/Unity/backend·스크롤 동작·warm-up·측정 시간을 기록하고 준비/첫 생성, 안정된 스크롤, count/resize, 닫기/idle 구간을 나눈다. 전체 데이터/이미지 준비 비용은 UI cell 생성/표시 비용과 분리한다.

측정은 frame time median/p95/max와 open/scroll spike, UI Layout/Graphic/Canvas CPU, GC Alloc·메모리, batch/draw/vertices, 활성/보관/누적 생성 수와 종료 후 구독/참조 잔존을 기록한다. 같은 구성·같은 구간의 실제 결과만 비교하고 측정 기기 없이 고정 FPS·GC 0·성능 개선율을 약속하지 않는다. TDD 기능 결과와 Profiler/Player 성능 결과는 별도로 보존하며 실행0건을 대규모 검증 통과로 표시하지 않는다.

## 구현 전 확정할 결과 계약 (2026-10-08 검토 보완)

현재 사용 범위는 HUD 선택·Popup·Virtual ScrollRect다. 기능을 늘리기 전에 아래 결과 계약을 확정하는 것을 권장한다. 이 절의 구체 정책은 구현 전 검토용 제안이다.

### 비동기 열기의 순서와 명령 충돌

같은 표시 영역의 기본 순번은 유효한 Open 요청이 접수된 시점에 예약한다. asset load 완료 순서가 표시 depth를 바꾸지 않게 한다. 아직 준비 중인 항목은 실제 표시/입력 경계에 포함하지 않고 취소된 예약은 제거한다. 서로 다른 UI의 준비는 병렬로 할 수 있지만 graph/depth/입력 상태 적용은 Unity 메인 스레드에서 재진입을 제어한다. 같은 owner·정의의 중복 요청은 기존 제안대로 거부하며 큐/대기 공유를 자동 추가하지 않는다.

| 상태/요청 | 권장 결과 |
|---|---|
| Opening 중 Close | 표시 요청 취소, 늦은 결과 공개 금지, 부분 생성/lease/listener 정리 후 Close 완료 |
| Closing 중 동일 표시의 Close | 진행 중인 종료 완료/실패 공유 |
| Closing 중 같은 owner·정의 재Open | 종료 완료까지 거부; 암묵적 reopen 예약 없음 |
| mode/depth 변경 | Visible에서만 허용, Closing 중 변경 거부 |
| 수명 callback에서 자기 작업을 다시 await | 재진입/자기 대기로 명시적으로 거부 |
| owner 종료 | 사용자 닫기 정책보다 우선, 신규 요청 거부, 진행 중 표시 요청 취소, 자식부터 종료 |

호출자 token의 의미는 method별로 명시한다. Open의 취소는 해당 표시 요청과 부분 정리를 대상으로 하고 빌린 자산 manager를 종료하지 않는다. 이미 시작한 Close/Shutdown cleanup은 caller의 대기 취소로 중단하지 않는다. view callback이 재진입해 shared graph 상태를 바꾸려는 요청은 적용 중인 상태를 훼손하기 전에 검증/거부한다.

### 중간 Popup 종료와 소유권 기본값

새 popup의 기본 논리 parent는 명시적으로 받은 owner/context로 한다. 단순히 마지막에 열린 popup을 자동 parent로 지정하지 않는다. 프로젝트가 특정 popup의 자식으로 요청한 경우에만 해당 소유 간선을 만든다. 생성 순번과 owner tree는 독립이다.

필수 예제는 A(modeless) → B(modal) → C(modeless)다. 세 창이 같은 owner에 독립 등록됐다면 B 종료 후 A → C를 유지하고 입력 경계를 재계산한다. C가 B의 논리 자식이면 C부터 종료한다. Closing 중인 B의 차단은 실제 숨김/표시 정리가 끝날 때까지 유지한다. 종료 후 이미 유효 focus를 가진 C가 있으면 유지하고 B를 열기 전 저장했던 A로 무조건 되돌리지 않는다. 실제 sortingOrder 번호를 연속 값으로 재할당할 필요는 없다.

### 표시 handle의 유효기간

표시 handle은 `(context, 표시 ID/세대)`를 식별한다. Pool/cache에서 같은 GameObject를 재사용해도 새 표시마다 새 handle을 발급한다. 과거 handle의 Close는 과거 표시의 완료/실패만 관찰하고 새 표시를 닫지 않는다. 만료 handle의 mode/depth/데이터 변경은 거부한다. 외부 호출자에게 Unity 객체를 노출하는 경우에도 그 객체만으로 현재 표시 권한을 판정하지 않는다. UIContext가 종료된 표시의 전체 기록을 영구 보관할 필요는 없다.

### 닫기 요청과 입력 재전달

사용자 Cancel/바깥 클릭은 프로젝트가 허용하는 닫기 요청이다. 기본 제안은 Cancel을 현재 유효 focus popup 한 곳으로 전달하고 바깥 클릭 닫기는 명시적 opt-in으로 둔다. 사용자 요청을 거부/처리할 계약과 owner가 수명을 끝내기 위한 최종 종료를 구분하며, owner 종료는 사용자 닫기 거부로 막지 않는다. 프로젝트가 필요한 저장/구매 등 business 처리 자체를 공용 Close에 맡기지 않는다.

닫기를 일으킨 click/submit/cancel은 아래 UI에 같은 입력으로 재전달하지 않는다. 새로 드러난 대상은 해당 입력의 해제와 최소 다음 frame 경계를 확인한 뒤 새 activation을 허용하는 정책을 제안한다. 전체 UI map이 계속 활성화된 popup 교체에도 적용해야 하며 Samples adapter의 map 차단/복원만으로 해결됐다고 간주하지 않는다. 같은 Escape로 창 둘이 닫히는 경우, 눌린 submit이 뒤의 구매 버튼을 실행하는 경우, pointer press/drag 중 owner 종료를 검증한다.

### 실제 표시 순서와 실패 처리

TPLab에 등록한 host의 관리 sibling/sorting 범위는 system이 관리하고 prefab 내부 배치는 프로젝트가 결정한다. logical depth와 actual Canvas 순서가 맞지 않는 요청, modal 경계를 우회하는 관리 대상 overrideSorting/입력 설정은 표시 전에 오류로 거부한다. batch 증가 가능성 같은 성능 권고는 경고로 구분한다. 알 수 없는 외부 Canvas까지 이동하거나 관리했다고 주장하지 않는다.

순서/입력 적용 실패는 가능한 범위에서 이전의 일관된 상태를 유지한다. 복구를 확인할 수 없으면 context 오류로 신규 표시/순서 변경을 거부하고 안전한 입력 차단을 유지한 채 오류를 프로젝트에 전달한다. callback/cleanup 오류로 다른 listener와 자원을 정리하는 시도를 중단하지 않고 성공으로 숨기지 않는다. 자동 reload/retry나 실패 popup 재귀 생성은 넣지 않는다. 최종 owner 종료는 계속 시도할 수 있어야 한다.

### 최소 검증과 확인 수단

구현 TDD는 접수/완료 순서가 다른 Open, 정상/중간/자식 종료, runtime mode/depth 변경, 반복 Close와 자기 재진입, 만료 handle, 같은 입력 재전달, 실패 callback/cleanup, owner 종료와 pending load, 공유/독립 Canvas·실제 순서 일치를 포함한다. 모달은 등록된 UI/입력 scope를 차단하는 계약이며 게임 simulation·Time.timeScale 정지를 자동 의미하지 않는다.

표시 목록·owner/parent·현재 상태·실제 표시 순서·최상위 modal과 focus/차단 원인은 runtime 상태 조회 또는 예제 Inspector에서 확인할 수 있게 한다. 별도 전역 event bus·runtime profiler·Canvas 자동 분할 시스템을 추가하지 않는다. 활성 cell 수와 UI batch/layout 비용의 실제 성능 측정은 기존 후속 계획을 따른다.

## 구현 Phase 계획 (2026-10-08)

사용자는 UIContext 명칭을 채택하고 구현 가능한 Phase 분할을 요청했다. 후속 구현 요청으로 P0를 시작했다. 아래 표는 단계별 완료 조건이며 실제 상태와 증거는 track에 기록한다. 기존 합의로 첫 범위를 나눌 수 있고 추가 사용자 결정이 필수인 사항은 현재 발견하지 못했다. P0의 실측에서 합의와 다른 지원 범위/의존성 변경이 필요하면 해당 차이만 재검토한다.

| Phase | 작업 범위 | 완료 조건 |
|---|---|---|
| P0 계약·의존성 | UIContext의 public 결과/취소·owner/handle 계약, namespace/assembly와 실제 Core·UniTask·uGUI·Input 참조, 지원 Canvas/host 순서 범위, 첫 TDD 항목 확정 | 등록/선행 준비/런타임 표시·수명 종료를 구현자가 별도 기획 없이 작성할 수 있다. 입력 연결의 선택 설치와 기존 패키지 migration 필요 여부를 조사해 기록한다. |
| P1 Context 수명 | root 범위, 표시 세대/handle, 표시 token·정리 등록·반복 Close/Shutdown·callback 실패/재진입·owner 종료 | EditMode 상태 계약과 작은 실제 GameObject의 root 종료/파괴 fallback을 Red→Green으로 검증한다. 정리는 해당 소유 자원마다 한 번 실행하고 오류에도 나머지 정리를 시도한다. |
| P2 등록·준비·생성 | 정의 검증/등록, 직접 prefab·ResourceManager 공급, 선택적 Prepare와 runtime lazy 준비, 같은 생성 경로와 선택적 인스턴스 보관 | 실제 prefab으로 준비 실패/취소·owner 종료 중 늦은 로드·부분 생성 정리·재사용 후 만료 handle을 검증한다. Pool 활성화와 Context의 표시 허가를 맞추며 빌린 자산 handle은 임의 해제하지 않는다. |
| P3 HUD·Popup·Canvas | HUD 준비 후 교체, owner tree·요청 접수 순서의 depth, 관리 host의 실제 sibling/sorting, 숨김 전략과 자식 우선 종료 | 실제 Canvas/CanvasGroup과 A-B-C fixture로 중간 종료·독립 C 유지·자식 C 종료·기존 HUD 보존·공유 Canvas 오용 거부를 검증한다. 이 Phase에서는 실제 modal 입력 차단 완료를 주장하지 않는다. |
| P4 Modal·입력·연출 | 가장 위 modal 경계, runtime mode/depth 변경, pointer/navigation focus·Input System 차단 lease, 열기/닫기 hook·입력 재전달 방지 | P3 fixture에 실제 EventSystem/Input System을 연결해 C focus 유지·B Closing 차단·다른 modal/전환 차단 보존·같은 click/submit/cancel 재전달 방지·연출 실패 정리를 검증한다. |
| P5 Virtual ScrollRect | Popup과 독립인 세로 고정 높이/한 열·단일 cell 타입, 표시 범위·cell 재사용, 프로젝트 bind/unbind·늦은 이미지 세대 검사 | N=1,000/10,000에서 정확한 행·활성/보관 총소유 상한·안정된 스크롤의 누적 생성 증가를 검증한다. 빈 목록·count/resize·큰 index 이동·drag/inertia·비활성 재활성·파괴/늦은 binding을 실제 ScrollRect에서 확인한다. |
| P6 설정·root·예제 | Inspector 설정·host 검사, script와 공통 검증, Bootstrap/Scene Prepare/정상 종료, 기존 cover/loading callback, HUD·인벤토리·A-B-C 예제 | 새 생성/종료 경로 없이 앞 단계 API로 구성한다. scene owner UI 종료와 영속 UI 유지, Single/Additive·중첩 씬 종료, 전환 lease 보존, 유효/잘못된 설정의 같은 결과를 확인한다. |
| P7 통합·소비·성능·수락 | 영향 범위 회귀·반복 Play, 외부 소비 설치/컴파일·예제 Player, Canvas 비교·ScrollRect 1,000 동수 비교와 가상화10,000 확장성, 사람/AI 문서·최종 확인 | exact source·실행 수/실패/skip·컴파일/제품 Console·소비 환경·Profiler 결과와 대규모 cell/생성 수를 구분해 보존한다. 사용자의 최종 시각/실제 입력 확인 후에만 main 통합 gate를 완료한다. 버전/tag/Release는 별도 배포 범위다. |

선행 순서는 P0 → P1 → P2 → P3 → P4, P5는 P0 계약과 P2 재사용/cleanup 경계가 실제 존재한 뒤 독립 진행 가능, P6은 P4/P5 이후, P7은 P6 이후다. 기본은 순차 진행하며 독립 파일·계약의 작업만 제한적으로 병렬화한다. P1에서 root 종료의 최소 연결을 만들고 P6까지 수명 검증을 미루지 않는다. runtime 설정 검증은 P2~P4에 포함하고 P6은 동일 검증의 Inspector/installer 입구를 제공한다.

각 동작 Phase는 실제 Red → Green → 필요한 Refactor, 관련 기존 회귀와 부모 리뷰, XML·사람/AI 문서의 구현 상태 갱신, 단위 회고를 포함한다. 모든 자동 검증을 P7에 몰아두지 않는다. P0처럼 문서만 바꾸는 단위는 링크·범위·계약 일관성 검사를 수행하며 실행0건을 통과로 세지 않는다. P7에 모으는 것은 최종 소비/성능과 사람의 시각·사용성 확인이다.

첫 범위는 공용/scene UIContext, HUD 선택·modal/modeless Popup, 명시적 host/owner, 선택적 준비/재사용, 세로 고정 높이 Virtual ScrollRect다. StaticHud/DynamicHud 타입·Canvas 자동 분할·전역 rebuild scheduler·공개 Factory 계층·게임 업무 로직은 추가하지 않는다. 기존 Core/Input 소스 이동과 package 분리 구현은 P0 조사 결과에 따른 별도 범위로 기록하며 UI 기능 구현에 자동 포함하지 않는다.

### 구현 track과 최종 확인

현재 설계 branch는 codex/game-ui-design이며 기준 HEAD는 8ce768d96f79ed8328143bebc31e77f060f51205, upstream은 없다. 이 계획 작성에서는 branch 생성/commit/push/merge와 runtime 구현을 수행하지 않는다.

구현 요청 후 actual main/dirty·선행 소스를 확인해 통합 track과 각 Phase branch를 준비한다. 제안 이름은 codex/game-ui-track, codex/game-ui-p0-contracts ~ p7-validation이며 아직 생성되지 않았다. 각 Phase는 최신 검증 track tip에서 시작하고 필요한 자동 검증·리뷰·회고를 마친 결과만 원본 commit을 보존해 track에 통합한다. 기존 사용자 Unity 변경은 수정/stage에서 제외하고 보호한다. Unity 조작·테스트·Git·최종 리뷰는 부모만 수행한다.

기본 하위 agent는 기존 ui_lifecycle_review를 재사용하고 별도의 독립 구현/조사가 있을 때만 최대2개를 배정한다. 지원 모델/추론·허용 파일·완료 조건은 실제 배정 시 위험과 난이도에 맞춰 기록하고 같은 파일/공용 계약을 동시 수정하거나 테스트 중 소스를 바꾸지 않는다.

최종 사용자 확인은 HUD 교체, A-B-C·자식 종료, 실제 pointer/keyboard/gamepad와 입력 재전달, 큰 목록 스크롤, 가림막·로딩, Single/Additive·화면비를 한 번에 확인할 수 있도록 P7에서 묶는다. 사용자가 확인하지 못하면 확인 대기를 유지하고 main 병합·branch 삭제를 하지 않는다. 이 요청에는 Unity/PC 종료가 없다.

## 근거와 현재 검증

- 저장소 현재 기준: main `8ce768d96f79ed8328143bebc31e77f060f51205`. 코드·설정 변경 없이 현재 public API/소유권과 대조했다. 관련 하위 에이전트 검토는 읽기 전용이다.
- Unity [ScrollRect 2.0 문서](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-ScrollRect.html): content·viewport·scroll/inertia 동작.
- Unity [UI 최적화 가이드](https://unity.com/how-to/unity-ui-optimization-tips): Canvas 변경 빈도별 분리, 대규모 목록의 작은 UI pool, 비활성화/재부모화/데이터/활성화 순서. 이를 TPLab scope와 조합한 정책은 이 초안의 설계 제안이다.
- Unity [Input System 1.19 UI 지원](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html#distinguishing-between-ui-and-game-input): UI 소비와 gameplay 입력은 자동으로 분리되지 않는다.
- Unity [UI Profiler 2.0](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/ProfilerUI.html): Canvas별 batch·vertices·Batch Breaking Reason과 overdraw 검사.
- Unity [LayoutRebuilder 2.0](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.LayoutRebuilder.html): 기본 delayed layout과 즉시 rebuild의 예외적 사용. 설치한 uGUI 2.0.0 소스 CanvasUpdateRegistry/LayoutRebuilder도 읽기 전용 대조했다. Library 경로는 구현/배포 dependency가 아니다.
- 이번 검증은 문서·소스의 정적 대조다. Unity 테스트·컴파일·Player·Profiler 실행 0건. 성능/시각/사용성 검증 또는 구현 완료를 의미하지 않는다.
