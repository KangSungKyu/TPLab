# Unity Input System wrapper 설계 초안

작성일: 2026-10-07. **Input System 전용 지원은 사용자 확정 정책**이다. 아래 타입·API·기본 동작·Phase는 검토 제안이며 미구현이다. 설계 문서의 병합은 runtime 구현이나 계약 승인 완료를 의미하지 않는다.

## 지원 정책과 현재 근거

- Unity Input System만 지원한다. Legacy Input Manager(`UnityEngine.Input`)·backend 선택·자동 fallback은 제공하지 않는다. Unity의 기본 입력 API도 신형 패키지의 `Keyboard`/`Mouse` 등 직접 장치 접근과 구분해서 표현한다.
- 확인 기준은 Unity 6000.3.18f1 + Input System 1.19.0이다. manifest와 설치 package가 일치하며 현재 `activeInputHandler: 1`(New)을 사용한다. 이번 설계에서 package·ProjectSettings·기존 씬을 변경하지 않는다. 최소 지원 버전·다른 플랫폼 호환성은 아직 미확정이다.
- 신규 입력 모듈은 `MyLab.Core.Input` namespace, `MyLab.Core.Input` 별도 assembly로 `MyLab.Core`·`UniTask`·`Unity.InputSystem`을 참조한다. 기존 `MyLab.Core.asmdef`는 입력 패키지를 참조하지 않는다. UI 연결은 별도 Samples/프로젝트 assembly에 둔다.
- 최초 범위는 입력 asset 한 개의 단일 사용자 scope다. 로컬 멀티플레이·InputUser pairing·플레이어별 장치 격리는 후속 요구에 따른다. 신형 Input System의 장치/Action/interaction/processor 기능은 그대로 사용하고 재구현하지 않는다.

기본 API 근거: [바인딩 변경·JSON·표시](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/ActionBindings.html), [Action/Map 활성화](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Actions.html#enable-actions), [UI와 게임 입력 구분](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html#distinguishing-between-ui-and-game-input). 1.19.0의 설치 소스에서도 `OnApplyBinding`, enabled Action의 리바인딩 거부, operation Dispose와 UI 클릭의 비독점 처리를 확인했다. 개발 중인 다른 버전의 기능을 현재 제공 API로 가정하지 않는다.

## 책임과 최소 구성

| 구성 제안 | 책임 | 맡지 않는 것 |
|---|---|---|
| `InputManager` | 원본 asset의 runtime clone 소유, Layers/Rebinding 구성·종료, runtime Action 참조 해석 | Singleton 강제, 게임 명령 dispatch, 장치 API 대체 |
| `InputLayerController` | layer 정의·lease, 현재 활성 Map 계산, 전체 입력 차단 lease | UI 표시 순서·팝업 포커스·입력 이벤트 소비 |
| `InputRebindingController` | 후보 캡처·검증·commit, 취소/종료/operation 해제, override JSON 입출력 | 설정 화면, 파일/PlayerPrefs 자동 저장, 자동 키 교환 |
| `InputManagerInstaller` | 기존 SceneRootInstaller 경로의 생성·주입·준비·역순 종료 | 새 DI container, 별도 부팅/씬 전환 구현 |

layer/rebind 요청과 결과는 필요한 작은 값 타입으로 정의하고 lease는 `IDisposable`로 반환한다. 모든 클래스에 interface를 만들거나 일반 입력 backend 추상화를 추가하지 않는다. 프로젝트 정책 교체에는 작은 동기 delegate만 사용한다.

게임별 Action 이름·Map 구성·Control Scheme·기본 키·우선순위·키 충돌 범위·설정 UI는 프로젝트가 소유한다. 공용 코어는 `Attack`, `Inventory`, `Pause` 같은 게임 enum이나 Action을 기본 등록하지 않는다. 1차 layer 정의는 script 등록으로 제공하며 독립 settings.asset/Editor 도구는 실제 반복 필요를 확인한 뒤 검토한다.

## asset·참조·수명

1. 생성 시 프로젝트의 `InputActionAsset`을 명시적으로 받고 clone한다. 원본 asset은 빌린 설정이며 바인딩 변경/Enable/Disable/파괴하지 않는다. runtime clone은 즉시 Disable하고 layer가 없는 초기 상태는 입력 비활성이다.
2. Map/Action/binding은 GUID로 식별한다. 설정 편의용 이름은 등록 시 GUID로 해석하며, 요청 시 배열 index만 저장하지 않는다. `InputActionReference`가 원본을 가리키면 같은 Action GUID를 clone에서 해석한다. 다른 asset이나 존재하지 않는 ID는 즉시 거부한다.
3. `GetAction`은 clone의 native `InputAction`을 빌려준다. 소비자는 typed ReadValue와 callback을 사용할 수 있고 자신의 구독을 해제한다. Enable/Disable, binding 변경, Dispose는 manager 전용이다. wrapper 밖에서 직접 읽는 장치·다른 asset·project-wide Actions는 layer로 차단되지 않는다.
4. 등록 Map은 해당 controller가 단독으로 활성화를 관리한다. 같은 clone을 두 controller나 PlayerInput과 공동 소유하지 않는다. 자동 활성화하는 project-wide Actions/PlayerInput을 관리 Map에 중복 연결하지 않는다. PlayerInput 지원 어댑터는 1차 범위 밖이다.
5. SceneOwnedRoot/SingletonSceneRoot 모두 기존 installer로 지원하고 singleton은 필수가 아니다. 공용 root scope를 빌리는 게임 씬은 종료 시 자기 layer lease·구독만 해제한다. manager 자체를 종료하지 않는다.
6. 시작 시 준비 차단 lease를 잡고 설정·저장 override 검증/복원·소비자 주입 후 준비를 완료한다. gameplay layer는 최종 진행을 허용하는 프로젝트 지점에서 획득한다. 준비 완료만으로 gameplay를 자동 활성화하지 않는다.
7. 종료는 신규 요청 거부 → layer 전체 차단 → 진행 중 rebind 취소·완료 대기 → 구독/operation 정리 → UI 참조 분리 → clone Disable/Destroy 순서다. Uninstall/OnDestroy에도 즉시 idempotent 정리를 지원하고 늦은 후보를 commit하지 않는다. 원본 clone 구분과 반복 Play/Domain Reload도 검증한다.

## layer 계산과 해제 계약

- `RegisterLayer(id, mapIds, priority, mode)`로 immutable 정의를 등록한다. 빈 Map 목록도 차단 전용 layer로 허용한다. 다른 layer 정의끼리는 Map을 공유하지 않는다. 동일 UI Map을 쓰는 중첩 팝업은 같은 layer의 lease를 여러 개 획득하고 포커스는 UI가 관리한다.
- layer ID/Map GUID 중복·누락·다른 asset의 참조는 등록 시 거부한다. 정의는 첫 lease 획득 전에 등록을 완료하고 이후 변경하지 않는다. 모든 관리 API와 lease Dispose는 Unity 메인 스레드에서 호출한다.
- `AcquireLayer(id)`는 고유 lease를 반환한다. 같은 layer의 여러 lease는 독립 소유이며 하나를 닫아도 나머지는 유지된다. 우선순위가 높을수록 앞이고 동순위는 나중에 획득한 lease가 앞이다. 같은 layer의 대표 순서는 남은 lease 중 가장 최근 것으로 계산한다.
- `Overlay`는 하위 layer도 유지한다. `BlockLower`는 자신을 포함해 위에 있는 layer만 허용하고 아래를 차단한다. 순서대로 내려가며 첫 `BlockLower`까지의 Map 합집합만 활성화한다. 활성 layer가 없으면 모두 Disable한다.
- `BlockAll()`은 독립 전체 차단 lease다. 하나라도 남으면 모든 관리 Map을 Disable한다. UI 포함 모든 입력 차단이 필요한 리바인딩에 사용하며, 오류 UI를 유지하는 씬 전환은 전용 `BlockLower` layer를 사용한다.
- lease Dispose는 중복 호출에 안전하다. 중간 layer를 먼저 해제해도 전체 현재 상태를 재계산하며 과거 Enable 상태를 그대로 복원하지 않는다. manager 종료 뒤 Dispose는 no-op이고 새 획득·등록은 거부한다.
- 비활성 Map을 먼저 Disable한 뒤 새 Map을 Enable한다. Disable의 canceled callback에서 lease 변경이 발생할 수 있으므로 변경 요청을 모아 재계산한다. 종료·재진입 중 중간 상태를 확정 상태로 통지하지 않는다.
- 상태 통지는 재계산 완료 후 현재 활성 layer/Map snapshot으로 제공하며 구독 해제는 소비자 책임이다. native 상태 적용이나 정리 실패는 입력을 허용하는 성공으로 숨기지 않는다. 가능한 Map을 모두 Disable하고 신규 요청을 거부하는 fault 상태 및 원래 예외를 보고한다.
- layer에는 입력 event의 `Handled`/전파 중단을 흉내 내는 기능이 없다. Overlay로 둘 다 활성화한 Map의 같은 키는 둘 다 반응할 수 있다. Map 수준 차단보다 세밀한 제어는 프로젝트가 Action/Map을 나누어 정의한다.
- Disable 시 진행 중 Action이 canceled될 수 있고 Enable 후 Value Action은 누른 키/스틱의 현재 상태를 읽을 수 있다. gameplay의 이동 상태는 canceled에서 정리한다. 모든 입력을 자동으로 neutral까지 기다리는 기능은 1차에 넣지 않으며 복원 직후 동작은 테스트·예제에서 명시한다.

| 현재 lease 예시 | 활성 Map 예시 |
|---|---|
| Gameplay(0, Overlay) | Gameplay |
| + Inventory(100, BlockLower) | Inventory |
| + ConfirmPopup(200, BlockLower) | ConfirmPopup |
| Inventory를 먼저 해제 | ConfirmPopup |
| ConfirmPopup 해제 | Gameplay |
| + Transition(1000, BlockLower, 오류 UI Map) | 오류 UI Map |
| + BlockAll lease | 없음 |

위 이름·숫자는 예시다. 같은 UI layer를 중복 획득하는 팝업 구성에서는 controller가 팝업 객체의 포커스를 결정하지 않는다. UI 최상위 객체만 반응하도록 프로젝트 어댑터가 관리해야 한다.

## 동적 리바인딩과 저장

제안 API는 `RebindAsync(RebindRequest, CancellationToken)`이며 UniTask 결과를 사용한다. 요청은 Action/binding GUID, Control Scheme/허용 control 제한, 취소 키, 선택적 timeout, candidate 검증 delegate를 가진다.

1. 한 scope에 동시에 한 rebind만 허용한다. 중복 요청·존재하지 않는 ID·composite 본체 선택은 실행 전에 거부하고 입력 상태를 바꾸지 않는다. composite part는 하나씩 변경할 수 있다. WASD 전체 변경의 원자적 wizard는 별도 프로젝트 UI 범위다.
2. controller가 BlockAll lease를 획득하고 대상 Action을 Disable 상태로 확인한 뒤 `PerformInteractiveRebinding`을 구성한다. cancel·timeout·장치 제한은 native operation 기능을 사용한다. 외부 Enable로 전제 조건이 깨지면 명시적으로 실패한다.
3. `OnApplyBinding`을 이용해 자동 적용을 대체하고 후보 path만 캡처한다. 프로젝트 validator는 commit 전에 Allow/Reject를 반환한다. delegate 예외는 실패로 전달하며 후보를 적용하지 않는다. UI 표시와 게임 동작은 callback 내부에서 실행하지 않는다.
4. 기본 충돌 정책 제안은 같은 Map·교차하는 binding group에서 동일 effectivePath를 가진 다른 binding을 거부하는 것이다. group이 비어 있으면 전체와 교차하는 것으로 본다. 다른 Map에 같은 키를 쓰는 것은 허용한다. 정확히 같은 path 비교는 wildcard·modifier/composite·장치 alias의 의미상 충돌 검출을 보장하지 않는다. 프로젝트가 validator로 범위/허용 정책을 명시적으로 바꿀 수 있으며 자동 교환은 하지 않는다.
5. 선택한 버튼 control은 해제를 기다린 뒤 생존·취소·binding ID를 다시 확인하고 단일 override를 commit한다. 후보 선택 이후의 해제 대기도 요청 timeout·취소 범위다. 선택 장치 제거는 취소하며 기본 설정을 유지한다. 버튼 외 연속 입력의 neutral 대기 정책은 프로젝트 요청에 명시한다.
6. 완료/거부/취소/실패 모든 경로에서 operation Dispose와 rebind 소유 BlockAll 해제를 보장한다. 이때 현재 layer를 재계산하므로 rebind 도중 열린 다른 팝업이나 씬 전환 차단은 유지한다. operation callback 내 예외는 완료 소스에 전달하고 정리 누락을 막는다.
7. 결과는 Applied/Rejected/Cancelled/TimedOut을 구분한다. 취소 키·timeout은 해당 결과이고 caller/owner CancellationToken 취소는 OperationCanceledException이다. 설정/ID/정책 예외는 예외로 전달한다. commit 직전까지 기존 override를 보존하며 commit 후의 늦은 취소는 이미 완료된 적용을 되돌리지 않는다.

`ExportOverridesJson`/`ImportOverridesJson`/`ResetBinding`/`ResetAll`은 Unity override API를 사용한다. Export는 현재 확정값만 읽는다. Import/Reset은 rebind 진행 중 거부하고 mutation 동안 전체 차단 lease를 사용한다. JSON 검증은 임시 clone에서 먼저 수행하고, 적용 실패 시 기존 override snapshot으로 복원한다. 복원까지 실패하면 입력 차단을 유지하는 fault 상태로 전환하고 양쪽 예외를 보고한다. 초기 버전은 존재하지 않는 Action/binding GUID를 조용히 무시하지 않고 오류로 보고한다.

파일 I/O·PlayerPrefs·프로필 선택·자동 저장은 프로젝트가 맡는다. 저장 envelope의 프로젝트 schema 버전과 GUID migration 역시 프로젝트 책임이다. 입력 asset 재생성으로 GUID가 바뀐 설정을 자동 추측해 매핑하지 않는다. UI의 키 이름은 native `GetBindingDisplayString`을 사용하고 아이콘·로컬라이징은 프로젝트에 둔다.

## 씬 전환과 UI 연결

- 기존 [SceneTransitionCallbacks](../Assets/MyLab/Core/SceneManagement/SceneTransitionCallbacks.cs)의 계약을 재사용한다. ShowCoverAsync는 자기 Transition layer lease를 확보하고 가림막 표시를 기다린다. HideCoverAsync는 reveal 성공 후 자기 lease만 해제한다. 가림막 실패·취소 시 layer를 유지하고 정책상 전환을 중단한다. bool 하나를 false로 바꾸어 다른 팝업 차단까지 해제하지 않는다.
- callback/입력 scope/가림막은 해제되는 게임 씬보다 오래 살아 있어야 한다. Single 전환의 persistent 공용 root 규칙은 [기존 계약](BOOTSTRAP_SYSTEM.md)을 따른다. layer priority로 scene graph/active scene의 우선순위를 대체하지 않는다.
- 기본 UI 입력과 gameplay 입력은 자동으로 상호 배제되지 않는다. `InputSystemUIInputModule`과 CanvasGroup/포커스를 연결하는 프로젝트 소유 어댑터를 예제로 제공한다. 코어는 EventSystem/uGUI/UI Toolkit에 의존하지 않는다.
- 어댑터는 원본 asset의 ActionReference를 그대로 연결하지 않고 manager clone의 Action을 사용한다. UI module 재활성화가 Map을 자동 Enable하는 문제를 포함해 adapter가 module lifecycle을 조정하고 controller 상태를 다시 적용한다. module이 임의로 다른/default asset을 만들지 않게 한다.
- UI Map 비활성/전체 차단 중에는 module도 비활성화하고 포인터 click/submit 상태·포커스를 정리한다. 복원은 module 설정/활성화 뒤 layer 상태를 다시 반영한다. 새 팝업을 연 submit/click이 같은 프레임에 닫기 동작으로 전달되는지 PlayMode에서 확인한다. Map 차단만으로 이미 처리 중인 UI 이벤트나 IMGUI/직접 장치 읽기까지 취소한다고 주장하지 않는다.
- 기존 `SceneTransitionSampleController`에는 clone·transition/modal 독립 차단의 작은 예시가 있다. wrapper가 준비된 후 해당 예시를 연결하여 중복 bool 기반 처리를 줄이되 이번 설계 작업에서 수정하지 않는다.

## 사용 흐름 제안

아래는 검토용 의사 코드이며 현재 컴파일 가능한 API가 아니다. 생성·등록은 공용 installer가, lease와 구독은 해당 소비자가 소유한다.

```csharp
// input은 공용 root에서 주입받은 InputManager다.
input.Layers.RegisterLayer("gameplay", gameplayMapIds, 0, InputLayerMode.Overlay);
input.Layers.RegisterLayer("inventory", inventoryMapIds, 100, InputLayerMode.BlockLower);
input.Layers.RegisterLayer("transition", errorUiMapIds, 1000, InputLayerMode.BlockLower);

_gameplayLease = input.Layers.AcquireLayer("gameplay");
_inventoryLease = input.Layers.AcquireLayer("inventory");
_inventoryLease.Dispose(); // 현재 남은 lease를 기준으로 복원한다.

var result = await input.Rebinding.RebindAsync(request, cancellationToken);
if (result.Status == RebindStatus.Applied)
{
    SaveProjectSettings(input.Rebinding.ExportOverridesJson());
}
```

## 구현 단계와 완료 조건 제안

| 단계 | 범위 | 최소 관찰 가능한 검증 |
|---|---|---|
| 0 설계 | 전용 정책 확정, 이 초안 검토 | 문서 링크·scope·기존 입력/씬 계약 대조. runtime 실행 대상 없음 |
| 1 layer·소유권 | clone, GUID, layer/전체 차단 lease, 상태·종료 | 원본 보존, 우선순위/동순위, 중첩/중간/중복 해제, cancelled 재진입, 종료 후 획득 거부, 실제 Map 차단·복원 |
| 2 rebind·override | 단일 binding/part, 후보 정책, cancel/timeout, JSON·reset | 새 키 적용·이전 키 비활성, 거부/취소 원본 유지, 버튼 해제/장치 제거, 동시 요청, ID 안정성, 잘못된 JSON 무변경·복구 실패 차단 |
| 3 root·전환·UI | installer, clone UI adapter, 기존 예제 연결 | 준비 전 입력0, Single/Additive·파생 씬 lease 정리, cover 실패 차단 유지, 팝업 중첩/독립 해제, UI module 재활성·같은 프레임 클릭 누출 |
| 4 통합 | 전체 회귀·반복 Play·consumer·Player | Domain/Scene Reload 네 조합, 기존 결과와 입력 hash 대조, 입력 모듈 포함/제외 소비 compile, 실제 입력 가능한 최소 Player |

동작 구현은 각 단위의 실패 테스트 실행 → 최소 구현 → 정리 순서로 진행한다. native Input System은 가상 Keyboard/Gamepad를 사용하는 Unity Test Framework 검증을 우선 사용하고 실제 UI/frame 순서는 PlayMode로 확인한다. 테스트 fixture는 생성 장치를 제거하고 전역 Input System 설정/기존 장치를 보존한다. 제품 Console·Player·최종 사람이 확인하는 키 설정/팝업 UX는 자동 테스트와 구분한다.

현재 완료는 전용 정책 기록과 설계 초안 작성뿐이다. wrapper 소스·assembly·installer·UI adapter·Editor gate·새 테스트·소비 프로젝트/Player 검증은 미구현/미실행이다. 구현 요청 전 타입/API/기본 충돌 정책은 검토 제안으로 유지한다.
