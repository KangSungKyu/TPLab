PackageVersion: 0.0.1. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.

# Input

Module: Input
Namespace: TPLab.Core.Input
Assembly: TPLab.Core.Input
SourceRevision: 3062716f2d494bc61bf515f3fa30b1ee8aada9f0
SourcePath: [Input/Runtime](../../../Runtime)
HumanContract: [Input](../../api/Input.md), [상세 계약](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/INPUT_SYSTEM_DRAFT.md)
ImplementationStatus: Implemented
ValidationStatus: Partial
Evidence: [자동 회귀/consumer/Player](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/README.md); 2026-10-07 사용자 입력 확인 완료. 물리 게임패드/touch 개별 증거 없음.

Symbol / Signature / Constraints:

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
// InputLayerController: internal constructor
void RegisterLayer(string id, IEnumerable<Guid> mapIds, int priority, InputLayerMode mode);
IDisposable AcquireLayer(string id);
IDisposable BlockAll();
InputLayerSnapshot Snapshot { get; }
void Refresh();
bool IsFaulted { get; }
Exception Fault { get; }
event Action<InputLayerSnapshot> Changed;
// enum InputLayerMode: Overlay, BlockLower
// InputLayerSnapshot
InputLayerSnapshot(); // empty
IReadOnlyList<Guid> ActiveMapIds { get; }
IReadOnlyList<string> ActiveLayerIds { get; }
bool AllInputBlocked { get; }
// RebindRequest
RebindRequest(Guid actionId, Guid bindingId);
Guid ActionId { get; }
Guid BindingId { get; }
string ControlPath { get; set; }
string BindingGroup { get; set; }
string CancelPath { get; set; } // "<Keyboard>/escape"
float TimeoutSeconds { get; set; } // 10
Func<RebindCandidate, bool> Validator { get; set; }
Func<InputControl, bool> IsReleased { get; set; }
// RebindCandidate: internal constructor, borrowed values
InputAction Action { get; }
Guid BindingId { get; }
string Path { get; }
InputControl Control { get; }
// RebindResult: readonly struct, internal constructor
RebindStatus Status { get; }
string Path { get; }
// enum RebindStatus: Applied, Rejected, Cancelled, TimedOut
// InputRebindingController: internal constructor
bool IsRebinding { get; }
UniTask<RebindResult> RebindAsync(RebindRequest request, CancellationToken cancellationToken = default);
string ExportOverridesJson();
void ImportOverridesJson(string json);
void ResetBinding(Guid actionId, Guid bindingId);
void ResetAll();
// InputManagerInstaller : SceneRootInstaller
InputManager Input { get; } // private setter
void Configure(InputActionAsset source);
void CompletePreparation();
override void Install(ISceneRoot root);
override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken);
override UniTask ReleaseAsync(ISceneRoot root);
override void Uninstall(ISceneRoot root);
```

Inputs: source nonnull borrowed; GUID resolves runtime clone, reference is not activated itself. layers unique nonempty IDs, actual map GUIDs, no map shared across definitions, empty blocker allowed. first Acquire freezes definitions; BlockAll does not freeze. rebind single binding/composite part, no composite root. timeout finite >=0 (0=infinite). CancelPath null/empty=no key cancellation.
Outputs: runtime clone actions, disposable independent leases, stable layer snapshot, RebindResult statuses or committed override JSON. validator 승인 후 버튼은 실제 release를 기다린다. 비버튼은 IsReleased가 지정된 경우만 해당 조건을 기다리고 null이면 즉시 통과한다.
Errors: invalid arguments/IDs throw; closed owner ObjectDisposedException; wrong thread/concurrent mutation/fault InvalidOperationException; caller/owner cancellation OperationCanceledException. validator/native/cleanup errors propagate. Escape/device removal returns Cancelled, deadline TimedOut, validator false Rejected. No failed operation treated as Applied.
Ownership: manager owns cloned InputActionAsset/controllers; project owns source/profile storage/UI. actions/controllers borrowed. project releases its leases/subscriptions; root installer alone owns installed manager.
Lifecycle: new disabled clone → RegisterLayer → AcquireLayer → use → release/unsubscribe → await ShutdownAsync; Dispose is immediate shutdown. late lease.Dispose is safe.
Threading: creating main thread only.
Concurrency: layer ordering priority descending/latest lease first, Overlay permits lower, BlockLower stops lower; BlockAll reference-counted. native event reentry settles stable state. rebind single mutation, snapshot old overrides until commit. export during selection allowed, commit/rollback export rejected.
Cancellation: rebind owns independent BlockAll; caller/owner linked token cancels and cleans native selection/release, then releases own block. does not clear other owners' blocks. device observation is not globally suppressed.
FailureCleanup: preserve/rollback prior overrides; failed rollback faults scope. owner cancellation ODE is converted only if linked token actually cancelled, with original inner exception. native clone shutdown drained gracefully when awaited.
Configuration: optional control path/group; JSON action/binding schema/known GUIDs validated on temporary clone, null rejected, empty clears. no persistence backend.
ExtensionPoints: Validator replaces default same-map/group identical path conflict rule; IsReleased for continuous controls, buttons always release-wait. UI adapter belongs to Samples/project, not core.
RequiredSequence: installer Configure before install → root-owned clone/preparation BlockAll → project registers/acquires desired layers → await root Prepare → CompletePreparation → use → root shutdown. standalone owner follows Lifecycle above.
ForbiddenUsage: use legacy input API; Enable/Disable/override/destroy borrowed Actions directly; use binding index for persistence; assume BlockAll blocks another asset/direct polling; release all modal/transition blocks after rebind; claim memory sample JSON saves files; Dispose borrowed installer.Input; add speculative InputUser multiplayer.
Example: [human excerpt](../../api/Input.md) NotRun/project placeholders; [sample UI](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/Assets/TPLab/Samples/SceneTransitions/Runtime/InputSystemUiScope.cs) and [consumer](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/tools/core-consumer/templates/ConsumerSmoke.cs) have recorded execution.
Compatibility: optional separate assembly, Core-only consumer has no Unity.InputSystem dependency.
Limitations: native Input System only; no device-specific settings UX/file storage/InputUser routing/semantic alias resolver. forced commit+rollback failure and physical gamepad/touch were not executed.
