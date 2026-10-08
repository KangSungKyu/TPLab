# UIContext 계약

2026-10-08 P0. 구현 승인된 첫 범위의 계약이다. 동작 구현/검증 상태는 [track](GAME_UI_SYSTEM_TRACK.md)에서 확인한다. 0.0.1 배포 API에 포함하지 않는다. 설계 근거는 [초안](GAME_UI_SYSTEM_DRAFT.md)이다.

## 모듈과 수명

- namespace/assembly는 TPLab.UI, source는 Assets/TPLab/UI다. UIContext는 해당 root의 상태를 소유하며 별도 필수 UIManager/Singleton/공개 Factory를 제공하지 않는다.
- 공용 Context는 실제 영속 root, scene Context는 해당 scene root에 연결한다. 논리 부모가 없는 표시의 owner는 Context이고 특정 HUD/Popup 자식으로 요청한 표시는 그 표시의 수명을 따른다. Canvas/Transform 배치와 논리 parent를 혼동하지 않는다.
- Context는 UI clone/표시 작업·자신의 구독/입력 lease를 소유하고 root, prefab 자산, ResourceManager/InputManager/카메라/Canvas/EventSystem을 빌린다. root의 종료는 graceful ShutdownAsync로 기다리고 Unity 파괴는 최종 fallback으로 준비 취소·소유 정리를 시도한다.
- 동작은 Unity 메인 스레드에 한정한다. 현재 표시 handle은 Context+표시 세대로 식별하며 재사용 clone의 이전 handle은 새 표시를 변경하지 않는다.

## 등록·준비·표시 진입

정의는 안정적인 ID·HUD/Popup 역할·직접 prefab 또는 명시적 자산 key·대상 host·보관/숨김 기본 정책을 담는다. 동시에 두 자산 source를 지정하거나 살아 있는 인스턴스를 설정 asset에 저장하지 않는다. 같은 owner·정의의 동시 열기는 기본 거부다.

- Register: 정의·host·정책 검증/등록. 모든 자산 preload/instance 생성을 자동 수행하지 않는다.
- PrepareAsync: 자산의 선택적 선행 준비. 직접 prefab은 이미 준비된 자산이다. 인스턴스 warm-up/표시/modal 차단을 의미하지 않는다.
- BeginOpen: 접수 시 순번을 예약하고 즉시 표시 handle을 반환한다. handle.Opened로 준비·열기 연출의 완료를 기다리며 Opening 중에도 같은 handle의 CloseAsync를 호출할 수 있다.
- OpenAsync: BeginOpen과 동일 경로를 사용하고 열기 완료 후 handle을 반환하는 편의 진입이다. 별도의 생성/취소 경로를 만들지 않는다.
- SelectHudAsync: 다음 HUD의 자산/데이터 준비 후 기존 HUD/그 자식 표시를 교체한다. 준비 실패 시 기존 HUD를 유지하고 종료 실패를 성공으로 숨기지 않는다.
- CloseAsync/ShutdownAsync: 표시/owner의 종료 결과를 공유하고, caller 대기 취소가 시작된 정리를 중단하지 않게 한다. Shutdown은 Context에 신규 요청을 거부시키고 자식부터 종료한다.

메서드별 실제 signature는 구현 source/XML과 사람/AI API 문서에 기록한다. Open caller 취소는 해당 표시 요청과 부분 정리를 대상으로 한다. Prepare caller 취소는 해당 대기를 취소하며 borrowed ResourceManager/native owner를 종료하지 않는다. 이미 종료된 표시의 Close는 그 표시의 결과를 관찰하고 새 표시를 닫지 않는다.

## 표시 수명과 실패

표시 준비/열기/닫기 hook은 프로젝트의 데이터 연결·연출을 기다리는 경계다. RegisterCleanup(Action)에는 이번 표시의 정확한 listener 해지/소유 subscription 정리만 등록한다. 표시는 새 token/세대이며 종료 시 역순·한 번 정리한다. 비동기 종료는 await hook에서 수행하고 async void cleanup을 등록하지 않는다.

Opening→Close는 작업 취소와 부분 정리, Closing→Close는 같은 종료 결과 공유다. Closing 중 재Open/표시 변경과 자기 hook의 동기 호출 중 자기 작업 재대기/종료 재진입은 거부한다. hook이 await 이후 자기 Opened/Closed를 다시 기다리는 순환 대기도 금지 사용이다. 현행 구현에서는 await 이후의 자기 순환 대기를 자동 검출한다고 보장하지 않는다. hook의 비동기 대기 기간 전체를 전역 재진입 상태로 잠가 외부의 정상 Close를 거부하지 않는다. owner 종료는 사용자 Cancel/바깥 클릭 닫기 거부보다 우선한다. 실패한 hook/정리 뒤에도 남은 자원을 정리하고 오류를 보존한다.

UIContext의 준비 완료와 root 전체의 준비 완료는 구분한다. SceneRoot installer는 필수 자산/첫 HUD만 준비하고 정상 해제에서 Context Shutdown을 await한 뒤 외부 ResourceManager를 종료한다. UIContext가 scene 로드·업무 규칙·Time.timeScale을 소유하지 않는다.

## 순서·입력·Canvas

한 Context의 기본 HUD는 하나이며 Popup은 같은 표시 영역의 요청 접수 순번으로 정렬한다. 아직 준비 중인 표시는 실제 입력 경계에 참여하지 않는다. 논리 자식은 부모보다 앞에 있어야 하고 실제 host/sibling/sorting으로 표현할 수 없는 순서는 요청 단계에서 거부한다. native UI를 batch 개선 목적으로 임의 재배치하지 않는다.

가장 위 유효 Modal 아래의 UI는 막고 그 Modal 이상은 포인터 후보가 될 수 있다. navigation/submit/cancel focus는 한 곳이며 유효한 C focus를 B 닫기 후 과거 A로 무조건 바꾸지 않는다. Closing Modal 차단은 실제 숨김/정리 완료까지 유지한다. 독립 A-B-C에서 B만 닫으면 A/C 유지, C가 B 자식이면 C부터 종료한다.

숨김 기본은 DeactivateView다. DisableCanvasRendering은 해당 UI 전용 Canvas/whole-host만 허용하고 단일 Popup 때문에 공유 Canvas를 끄지 않는다. rendering을 끄는 것과 입력/focus/표시 작업 정리를 동일하게 안내하지 않는다.

Input System 연동은 별도 선택 경계이며 Core/Input에서 UI를 역참조하지 않는다. 기존 Input layer의 등록 동결과 독립 lease를 유지한다. 같은 닫기 click/submit/cancel이 뒤 UI에 재전달되지 않도록 해제+frame 경계를 검사하고 다른 Modal/전환 차단을 해제하지 않는다.

## Virtual ScrollRect

독립 세로/고정 높이/한 열·한 prefab cell 컴포넌트다. 기존 ScrollRect 동작을 사용하며 data count·bind/unbind는 프로젝트가 제공한다. cell의 이전 binding은 반환 시 정리하고 늦은 비동기 결과는 token/세대로 거부한다.

필수 N=1,000/10,000 기능 gate와 기본 ScrollRect1,000↔virtual1,000의 성능 비교, virtual10,000의 확장성은 [대규모 검증 조건](GAME_UI_SYSTEM_DRAFT.md)에 따른다. 활성/보관/총소유·누적 생성 수를 구분해 안정된 스크롤의 생성 증가를 검사한다.

## 의존성과 구현 경계

기본 UI runtime 참조는 TPLab.Core·UniTask·UnityEngine.UI다. uGUI2.0.0을 제공해야 한다. 현행 단일 Core 때문에 UniTask/Addressables/CsvHelper의 설치 부담이 전이되며 이를 숨기지 않는다. Pool 코드의 직접 외부 plugin 의존과 Core 전체 설치 의존은 다르다.

Input adapter는 UI runtime에서 분리하고 TPLab.Core.Input·Unity.InputSystem·UnityEngine.UI를 참조하는 선택 경계로 구현한다. passive 목록의 UI base는 Input assembly를 참조하지 않는다. 기존 Core/Input 재편·새 배포 package/version은 이번 UI 기능의 암묵적 선행 조건으로 추가하지 않는다.

P0의 확인 환경은 Unity6000.3.18f1, UniTask2.5.11, uGUI2.0.0, InputSystem1.19.0, Addressables2.9.1이다. P0 당시 실제 원본 테스트·소비/Player·성능·최종 수락은 미실행이며 다른 Unity/플랫폼 지원을 주장하지 않는다.

## P1 구현 범위와 실제 검증

2026-10-08 source545c342f80061c66ede2fc7f168cfc1c2cb13cee에서 root scope·direct prefab·modeless Popup·default host·DestroyOnClose/DeactivateView의 표시 수명을 구현했다. 나머지 정책을 조용히 무시하지 않고 NotSupported로 거부한다. 정확한 현재 선언과 제한은 [사람 API](api/UI.md), [AI API](ai/api/UI.md), [실제 결과](validation/ui-system/p1/README.md)를 따른다. 현재 UI18/18·기존 root10/10 실패0/skip0이며 외부 소비/Player/성능/최종 UX는 P7 미실행이다.

표시 hook에서 다른 표시를 열거나 닫는 조합은 허용한다. 자기 handle의 동기 Close/Opened/Closed, 현재 hook을 포함한 Context Shutdown/Dispose와 cleanup/native 적용 중 lifecycle 변경은 거부한다. 종료 observer는 native 정리 및 StateClosed/ViewObject null 이후에 호출하며 observer 실패도 공유 Closed 결과에 보존한다. 다른 세대의 표시를 종료 observer가 시작해도 이전 표시의 정리가 이를 변경하지 않는다.

## P2 구현 범위와 실제 검증

2026-10-08 sourcecea4268b82e0f119e0d4dbc8c788b7632d21f3ee에서 explicit key provider와 direct prefab의 같은 표시 준비, source-only Prepare, definition별 clone 최대1개 Reuse를 구현했다. native 자산/provider는 borrowed이며 실패 entry는 다음 명시 요청에서만 재시도한다. caller 준비 취소는 공유 자산 작업을 종료하지 않고 owner 종료 후 late result는 무시한다. [실제 UI30/30](validation/ui-system/p2/README.md)·실패0/skip0·compile/제품 Console 오류0이다. HUD/Canvas/graph/input/scroll 및 최종 외부/성능/UX gate는 남아 있다.

Reuse 후보는 cleanup 및 StateClosed/ViewObject null 이후 Closed observer가 성공해야 cache에 공개한다. 실패/초과 후보를 폐기하며 callback이 만든 새 표시의 수명을 변경하지 않는다. owner Shutdown은 active/retiring/cached clone의 실제 파괴를 기다린다. public PrepareAsync는 인스턴스를 만들지 않고 UIHooks.PrepareAsync는 표시 중 inactive clone의 데이터/구독을 준비한다.

## P3 확정 범위와 선행 테스트 준비

2026-10-08. P2 검증 tip에서 진행한다. 신규 public 진입은 `RegisterHost(string id, Transform container)`, `CurrentHud`, `SelectHudAsync(UIOpenRequest, CancellationToken)`다. 아래 계약은 P3에서 구현했다. 최종 실행 근거는 아래 검증 절을 따른다.

- default host는 borrowed root Transform이다. 명시 host는 ID별 한 번 등록하며 borrowed host/Canvas를 파괴하거나 재설정하지 않는다. 표시 직전 살아 있는 host를 다시 검사한다.
- logical parent는 같은 Context의 Visible handle만 가능하며 중복 범위는 `(Parent 또는 Context, DefinitionId)`다. parent 종료는 subtree 신규 요청을 막고 준비 중 자식을 취소하며 자식부터 완료한다. callback이 종료할 subtree에 자기 표시를 포함하면 동기 재진입을 거부한다. 독립 표시 조합은 유지한다.
- HUD는 SelectHudAsync로 선택한다. parent 없는 Hud 역할만 허용하고 동시 선택/현재 정의 재선택은 거부한다. 다음 asset/clone/Prepare 성공 전 기존 HUD와 자식을 유지한다. 기존 종료가 시작된 뒤 rollback을 약속하지 않는다. 기존 종료 실패 시 candidate를 정리하고 오류를 보존하며 일반 callback 실패만으로 Context를 native fault로 만들지 않는다.
- 실제 순서는 HUD/Popup 역할과 고정 host 영역, 접수 Id, 부모보다 앞선 자식 조건을 만족해야 한다. 같은 effective Canvas에서는 관리 표시의 sibling 순서만 적용한다. 독립 Canvas의 실제 sorting layer 값/order를 비교하고 renderMode/camera/targetDisplay 불일치·순서 동률·우회 override 등 표현 불가능한 배치는 기존 표시를 건드리기 전 거부한다. source-only 수명 fixture에는 Canvas를 강제하지 않으며 실제 Graphic 구성에는 Canvas 검증을 적용한다.
- DisableCanvasRendering은 owned clone의 전용 Canvas에 한정한다. Canvas 및 owned raycaster/로컬 상호작용을 차단하며 공유 host Canvas를 끄지 않는다. 설치 uGUI의 실제 native 관찰에서 child Canvas 비활성화 후 Graphic이 active 상위 Canvas로 fallback했다. 따라서 RendererOnly에만 host 전체 크기의 owned RectTransform wrapper와 CanvasGroup alpha=0을 사용한다. public ViewObject는 원래 prefab clone이고 실제 parent는 wrapper다. CanvasGroup은 같은 GameObject에 두 개를 추가할 수 없으므로 이 구조로 프로젝트 prefab의 기존 CanvasGroup 값을 보존한다. DeactivateView는 wrapper 없이 직접 host에 배치한다. prefab의 overrideSorting 및 ignoreParentGroups로 관리 순서/가림을 우회하는 구성은 명시적으로 거부한다. 새 clone의 Prepare는 inactive이고 이 정책의 Reuse는 GO 활성/Canvas와 raycaster 차단 상태에서 Prepare한다. 표시 구독·업무 update 정리는 프로젝트 hook의 책임이고 실제 EventSystem focus/게임 입력 차단은 P4에서 검증한다.
- Reuse는 Transform뿐 아니라 RectTransform의 anchor/pivot/size/anchoredPosition도 borrowed prefab 규격으로 복원한다. 자산·host·owner 수명과 source-only Prepare 의미는 P2 계약을 유지한다.

## P3 구현과 검증

source `18666acae25b04c1c63ca49d8c00ca2ee67da308`에서 HUD 교체·논리 자식 종료·borrowed host의 실제 Canvas/sibling 정렬·명시 숨김을 구현했다. [최종 자동 검증](validation/ui-system/p3/README.md)은 UI Edit18/18·Play30/30 실패0/skip0 및 compile/제품 Console0이다. Runtime과 실제 native 관찰을 대조했으며 Canvas 렌더 비활성 fallback을 owned wrapper mask로 차단한다. 사람/AI API와 [회고07](retrospectives/2026-10-08-07-ui-presentation.md)를 갱신했다. P4 입력, P5 virtual, P6 통합과 P7 소비/Player/성능/사용자 수락은 남아 있다.

## P4 확정 계약과 Red 준비

P3 최신 검증 track3725ad6에서 codex/game-ui-p4-input을 생성했다. 아래는 구현할 계약이며 P4 실행·완료 결과가 아니다.

- UIContext 생성자 네 번째 optional `EventSystem eventSystem = null`은 프로젝트의 native EventSystem을 명시적으로 빌리고 read-only `EventSystem`으로 관측한다. 미연결 Context도 CanvasGroup UI-only modal 차단은 지원하되 SetFocus는 거부한다. global EventSystem.current 자동 탐색은 하지 않는다. 게임 조작 차단은 별도의 명시 acquireModalBlock delegate/선택 adapter가 필요하다.
- UIHandle의 현재 InputMode/CanReceiveInput, Visible에서 SetInputMode·BringToFront(owned subtree 전체), Opening/Visible에서 live clone descendant에 SetFocus를 제공한다. 표시 ID는 불변이고 순서는 별도다. fixed Canvas에 표현 불가능한 front 이동은 native mutation 전에 거부한다. 종료 handle은 이후 rental을 제어하지 못한다.
- UIUserCloseReason은 Cancel/OutsidePointer/Button. optional UIHooks.CanCloseAsync가 opt-in/veto를 소유한다. RequestCloseAsync는 veto/미참여 false, 허용한 shared Closed 완료 후 true이고 stale/state/concurrent 요청과 caller 취소는 명시 오류다. force Close/root 종료는 veto를 기다리지 않고 남은 veto를 취소한다. outside click/button은 프로젝트 callback이며 자동 fullscreen blocker를 만들지 않는다.
- 모든 관리 view는 owned RectTransform wrapper+CanvasGroup에 놓여 source CanvasGroup/Selectable 값을 보존한다. ViewObject는 원 clone이다. P3 DeactivateView의 직접 parent 관측은 managed wrapper.parent로 바뀐다. prefab overrideSorting와 ignoreParentGroups 우회는 거부한다. 가장 위 presented Modal 아래 UI는 차단되며 Closing modal lease는 실제 hide/cleanup까지 유지한다. Opening은 Visible 게시 전까지 native 입력 후보/모달 경계에 참여하지 않는다.
- DisplayChanged는 각 project listener를 독립 호출하는 read-only 동기 알림이다. listener의 synchronous lifecycle mutation을 거부한다. 일반 listener 오류는 남은 listener/cleanup/lease retirement 뒤 해당 lifecycle 결과에 남기고 Context.Fault로 바꾸지 않는다. 선택 adapter의 실제 native 적용 오류는 friend assembly의 단일 내부 callback으로 구분해 Context를 fault시키고 안전 차단을 owner 종료까지 유지한다.
- TPLab.UI.InputSystem 선택 assembly만 TPLab.Core.Input/Unity.InputSystem을 참조한다. UIInputSystemAdapter.Bind는 같은 명시 EventSystem/module/runtime-clone UI map을 검증한다. AcquireModalBlock은 미리 등록된 mapless BlockLower layer의 독립 lease 하나를 빌린다. Context와 InputManager/EventSystem/module은 borrowed이며 adapter가 service를 Dispose하지 않는다.
- native retirement/Cancel opt-in은 module pointer/navigation state를 reset하고 raw pointer buttons/touch/submit/cancel/move 해제와 다음 EventSystem frame까지 native 재입력을 격리한다. 퇴장 modal lease만 이 경계까지 보관하며 다른 modal/transition lease를 해제하지 않는다. module의 action toggles 뒤 Layers.Refresh로 기존 입력 정책을 재적용한다. 외부 block이 UI map을 막은 경우에도 raw control 상태를 확인한다. UI map은 native module/adapter 전용이며 임의 raw action 구독의 게임 로직은 이 격리 계약 밖이다.
- 유효한 상위 C focus는 B 종료 뒤 유지한다. focus history는 display generation+target이며 cached GO의 새 rental을 옛 handle이 복원하지 못한다. module 하나에 경쟁하는 interactive Context 여러 개를 bind하지 않는다. passive persistent overlay와 scene interactive Context는 함께 사용할 수 있다. Unbind/owner 종료는 adapter-owned pending lease만 강제 해제하고 borrowed module을 비활성화한 뒤 유효한 input layer 정책을 재적용한다.

P4 예정 사례17개(Edit6/native UI Play6/InputSystem Play5)의 실제 Red부터 실행한다. raw held 입력 사례는 pointer3/submit/cancel/move/touch7종을 포함한다. 아직 Assets stub/테스트 준비 단계이며 실행0건이다.

## P5 구현 계약

기존 ScrollRect를 빌리는 독립 세로·고정높이·한열 VirtualScrollRect와 readonly VirtualCellBinding을 제공한다. Configure/SetCount/Refresh/ScrollToIndex는 Unity main thread에서만 호출한다. content/cell은 가로 stretch·위쪽 anchor/pivot, content에는 LayoutGroup/ContentSizeFitter를 두지 않는 첫 범위다. 잘못된 숫자·배치·count·callback 재진입은 작업중 상태 변경 전에 거부한다. Configure는 이전 정상 목록을 invalid 입력 때문에 해제하지 않는다.

bind/unbind callback 안의 같은 목록 Configure/SetCount/Refresh/ScrollToIndex 구조 변경은 InvalidOperationException이다. 다른 독립 UI 작업은 전역적으로 차단하지 않는다. native onValueChanged와 자체 layout 갱신은 private coalescing으로 구분한다. cell 반환/disable/destroy/실패는 이전 binding token을 취소하고 IsCurrent를 false로 만든 뒤 남은 정리를 시도한다. 프로젝트 async 결과는 main thread 복귀 후 token과 IsCurrent 확인을 모두 수행한다. source prefab/ScrollRect와 다른 listener를 정리하지 않는다.

총 extent는 count*height+max(0,count-1)*spacing이며 0건은0이다. count/viewport 변경과 logical jump는 native 위치를 clamp하고 visible coverage를 유지한다. active+inactive는 viewport와 overscan의 창 예산으로 제한하며 고정 viewport warm 이후 생성/파괴 수가 scrolling으로 누적 증가하지 않는다. 자동 scroll reconcile는 같은 index의 binding을매frame 교체하지 않는다. 공개 Refresh의 프로젝트 데이터 재binding 정책은 실제 구현/XML에서 명시한다. 기존 Core ObjectPool은 고정capacity이며 trim API가 없어 viewport 변화에 맞는 작은 private cell holder를 사용하고 공용 Pool API는 확장하지 않는다.

P5 source 반영 전 Temp 테스트14개는 계획이며 실제 Red/Green, consumer/Player/성능 성공이 아니다. 실제 상태는 track/검증 근거를 따른다.

P5 구현 source `8f8abd290d540dc0b6ba30c5acc8b36b271bc302`와 [실제 UI79 검증](validation/ui-system/p5/README.md)을 기록했다. Configure의 성공 교체는 이전 binding/cell을 정리하고 Count0부터 시작한다. 자동 native 이동은 물리 elastic/inertia를 보존하고 계산 범위만 clamp하며 count·viewport/jump는 물리 위치를 clamp한다. bind 실패는 모든 부분 세대를 취소·해제하고 자동 재시도를 중단하며 명시 command로 재시도한다. 성능·소비·사용자 수락은 P7이다.

## P6 설정과 root 계약

UIContextSettings는 private SerializeField DTO/readonly 속성으로 정의·선택 preload ID·선택 첫 HUD ID를 보관한다. Inspector와 Configure/CreateSnapshot이 같은 검증을 거치며 Install에서 정의·preload·첫 HUD를 owner-local로 복사한다. 같은 Settings가 나중에 바뀌어도 설치된 root의 준비를 바꾸지 않는다. Host/EventSystem/ResourceManager와 callback은 installer에 명시적으로 빌린다.

Install은 Context와 등록만 만들며 표시 clone을 생성하지 않는다. Prepare는 선택 자산만 source로 준비한 뒤 선택 첫 HUD를 연다. Keyed 정의는 설치된 비폐기 ResourceManager 또는 custom provider 중 하나가 필요하며 Resource 초기화는 상위 root Prepare 순서를 따른다. Resource provider는 설치 시 manager 객체를 빌려 다른 owner로 바뀐 installer를 다시 탐색하지 않는다. 정상 순서는 Resource→Input→UI이며 역해제에서 UI Shutdown을 await한 뒤 상위 서비스를 종료한다. Uninstall은 자신의 Context 참조를 먼저 지우고 fallback Dispose를 수행한다. 다른 root의 호출은 거부하지만 파괴 중 같은 root의 fake-null은 정리를 막지 않는다. 별도 Factory/전역 Context/자동 Input 게시를 추가하지 않는다.

[P6 Red 근거](validation/ui-system/p6/README.md)를 기록했고 구현과 최종 Green은 진행 중이다.
