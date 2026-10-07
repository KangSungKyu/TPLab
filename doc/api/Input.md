# Input

Unity Input System 전용 wrapper다. 원본 action asset을 복제하고, layer lease로 map 활성 상태를 관리하며 native interactive rebinding과 override JSON을 제공한다. 프로젝트는 action asset, layer 구성, 저장 위치와 설정/UI를 소유한다. legacy `UnityEngine.Input` adapter는 제공하지 않는다.

- Namespace: `TPLab.Core.Input`; Assembly: `TPLab.Core.Input`
- SourceRevision: `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`; [소스](../../Assets/TPLab/Input/Runtime)
- ImplementationStatus: Implemented; ValidationStatus: Partial. Input System 1.19.0 / Unity6000.3.18f1 / Windows Mono [자동 검증](../validation/input-system/p4/README.md)과 사용자 입력 수락 완료를 구분한다. 물리 게임패드·touch의 개별 실행 증거는 없다.
- 의존성: Core, UniTask, Unity.InputSystem. [상세 계약](../INPUT_SYSTEM_DRAFT.md).

선언 발췌:

```csharp
// InputManager : IDisposable
InputManager(InputActionAsset source);
InputActionAsset Actions { get; }
InputLayerController Layers { get; }
InputRebindingController Rebinding { get; }
bool IsDisposed { get; }
InputAction GetAction(Guid actionId);
InputAction GetAction(InputActionReference reference);
UniTask ShutdownAsync();
void Dispose();
// InputLayerController (manager에서 제공)
void RegisterLayer(string id, IEnumerable<Guid> mapIds, int priority, InputLayerMode mode);
IDisposable AcquireLayer(string id);
IDisposable BlockAll();
InputLayerSnapshot Snapshot { get; }
void Refresh();
bool IsFaulted { get; }
Exception Fault { get; }
event Action<InputLayerSnapshot> Changed;
```

source는 null 불가이고 borrowed이며, manager가 disabled runtime clone을 소유한다. action/reference GUID는 clone의 action으로 해석한다. 소비자는 Actions/action을 빌리며 Enable/Disable/override/native Destroy를 직접 호출하지 않는다. API는 생성한 메인 스레드에서 사용한다. `ShutdownAsync`는 진행 중 rebind 정리를 기다리고 clone을 파괴한다. `Dispose`는 즉시 종료 경로다. 소비자는 구독과 자기 lease를 해제하며 manager 소유자는 종료를 담당한다.

layer ID는 nonempty·unique, map GUID는 존재하고 중복 소속이 없어야 한다. 빈 map blocker는 허용된다. `Overlay`는 하위 layer를 계속 허용하고 `BlockLower`는 하위를 차단한다. 높은 priority, 같은 priority의 최근 획득 순서로 평가한다. 첫 Acquire 이후 정의를 고정한다. `BlockAll`은 독립적인 최상위 차단 lease이며 정의를 고정하지 않는다. 중첩 차단은 마지막 lease 해제까지 유지된다. lease Dispose는 반복/owner 종료 뒤에도 안전하다.

`InputLayerSnapshot`의 public 빈 constructor는 빈 관찰값이다. `ActiveMapIds : IReadOnlyList<Guid>`, `ActiveLayerIds : IReadOnlyList<string>`, `AllInputBlocked : bool`은 getter다. snapshot/Changed는 구조적으로 안정된 상태를 제공한다. native map 이벤트 재진입 중 변경은 이후 정리하며, fault면 gameplay를 계속 활성화하지 않는다. 잘못된 등록/재진입/상태는 argument 또는 invalid-operation 예외, 종료 후 owner API는 disposed 예외를 전달한다.

리바인딩·저장 선언:

```csharp
// RebindRequest
RebindRequest(Guid actionId, Guid bindingId);
Guid ActionId { get; }
Guid BindingId { get; }
string ControlPath { get; set; }
string BindingGroup { get; set; }
string CancelPath { get; set; } // default "<Keyboard>/escape"
float TimeoutSeconds { get; set; } // default 10, 0 = deadline 없음
Func<RebindCandidate, bool> Validator { get; set; }
Func<InputControl, bool> IsReleased { get; set; }
// InputRebindingController (manager에서 제공)
bool IsRebinding { get; }
UniTask<RebindResult> RebindAsync(RebindRequest request, CancellationToken cancellationToken = default);
string ExportOverridesJson();
void ImportOverridesJson(string json);
void ResetBinding(Guid actionId, Guid bindingId);
void ResetAll();
```

binding index가 아닌 GUID를 사용한다. 단일 binding 또는 composite part 하나가 대상이며 composite root는 거부한다. timeout은 유한 nonnegative 값, selection과 release 전체에 적용되는 realtime deadline이다. CancelPath null/빈 문자열은 키 취소를 끈다. control/group은 선택적인 native 필터다. 기본 validator는 같은 map/group의 동일 path 충돌을 거부하며 custom Validator는 이를 **대체**한다. wildcard/alias/게임별 의미상 충돌은 프로젝트 책임이다.

`RebindCandidate`는 borrowed `InputAction Action`, `Guid BindingId`, `string Path`, `InputControl Control` getter를 제공한다. 버튼은 release를 기다린 뒤 commit한다. 비버튼 control은 `IsReleased`를 지정했을 때만 해당 중립 조건을 기다리며 null이면 즉시 통과한다. false validator는 기존 override를 유지하며 결과는 `Rejected`다. `RebindResult`는 `RebindStatus Status`와 `string Path` getter; status는 `Applied`, `Rejected`, `Cancelled`, `TimedOut`다. 후보 path는 적용/거부에 포함될 수 있고 미선택 취소는 null이다. Escape/장치 제거는 Cancelled 결과, caller/owner token 취소는 `OperationCanceledException`이다. invalid GUID/요청은 argument 오류, 동시 mutation은 `InvalidOperationException`으로 거부한다. validator/native 오류를 성공 status로 숨기지 않는다.

rebind는 한 번에 하나이며 별도 BlockAll lease를 획득한다. native 이벤트를 관찰하므로 다른 action asset이나 직접 device polling까지 차단하지 않는다. 선택 중 Export는 마지막 commit 값을 내보낸다. commit/rollback 중 Export와 동시 Import/reset은 거부한다. Import는 알려진 action/binding ID·형식만 허용하고 임시 clone으로 검증한다. null은 거부하고 빈 문자열은 override를 지운다. mutation 실패는 rollback하며 rollback도 실패하면 fault로 막는다. 파일·프로필 저장은 JSON 문자열을 받은 프로젝트가 담당한다.

root 연결:

```csharp
// InputManagerInstaller : SceneRootInstaller
InputManager Input { get; } // private setter, 설치 전/해제 후 null
void Configure(InputActionAsset source);
void CompletePreparation();
void Install(ISceneRoot root);
UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken);
UniTask ReleaseAsync(ISceneRoot root);
void Uninstall(ISceneRoot root);
```

installer는 root 또는 그 자식에 두며 source를 설치 전에 지정한다. Install이 준비 BlockAll을 만들고, root 준비 완료 후 프로젝트가 `CompletePreparation()`을 호출해 이 차단만 해제한다. 다른 전환/modal/rebind lease는 유지된다. gameplay layer를 암묵적으로 획득하지 않는다. ReleaseAsync는 graceful 종료를 기다리고, 파괴 시 Uninstall은 즉시 정리한다. borrowed `Input`을 소비자가 Dispose하지 않는다.

설명용 발췌(NotRun): source와 actionMap/binding GUID는 프로젝트가 공급한다.

```csharp
var input = new InputManager(source);
try
{
    input.Layers.RegisterLayer("Gameplay", new[] { gameplayMapId }, 0, InputLayerMode.Overlay);
    using (input.Layers.AcquireLayer("Gameplay"))
    {
        RebindResult result = await input.Rebinding.RebindAsync(
            new RebindRequest(attackId, keyboardBindingId), cancellationToken);
        if (result.Status == RebindStatus.Applied)
            SaveProjectOverrides(input.Rebinding.ExportOverridesJson());
    }
}
finally
{
    await input.ShutdownAsync();
}
```

취소 token 예외는 프로젝트 호출자에게 전달된다. `Rejected/Cancelled/TimedOut`은 정상 결과로 UI에 표시할 수 있다. 실행 확인된 [sample UI adapter](../../Assets/TPLab/Samples/Input/SceneTransitions/Runtime/InputSystemUiScope.cs)와 [소비 smoke](../../tools/core-consumer/templates/ConsumerSmoke.cs)는 별도 예제다. UI module을 runtime clone에 연결하고 lease/구독을 UI 수명에 맞춰 해제한다. UI는 코어 소유가 아니다. InputUser 멀티플레이, 파일 저장, 설정 화면, 물리 장치별 UX는 제공 범위에 포함하지 않는다.
