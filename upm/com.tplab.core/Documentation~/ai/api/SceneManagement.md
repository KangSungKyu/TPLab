PackageVersion: 0.0.1. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.

# SceneManagement: AI API reference

| Field | Value |
|---|---|
| Module / Namespace / Assembly | `SceneManagement` / `TPLab.Core.SceneManagement` / `TPLab.Core` |
| SourceRevision | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` |
| SourcePath | [소스](../../../Runtime/SceneManagement), [asmdef](../../../Runtime/TPLab.Core.asmdef) |
| HumanContract | [사람용 API](../../api/SceneManagement.md), [manager](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/GAME_SCENE_MANAGER_DRAFT.md), [Bootstrap](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/BOOTSTRAP_SYSTEM.md), [로더](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/SCENE_LOADING.md), [Lifecycle](Lifecycle.md) |
| ImplementationStatus / ValidationStatus | `Implemented` / `Partial` |
| Evidence | 현재 P4 전체 Edit271/271·Play253/253(실패0/skip0), Additive/Single Windows Mono build 및 Player 각12/12, Input 포함 consumer Editor build 1회와 Player run 1회. [P4 증거](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/scene-loading/p4/README.md). UserAcceptance: user PlayMode confirmation 2026-10-07; per-mode/device/resolution results unspecified. |

## Symbol / Signature / Constraints

public 선언 참조이며 body는 생략한다. ResourceManagement의 SceneTarget/ISceneLoader가 의존 타입이다. internal helper·테스트 API는 노출하지 않는다.

```csharp
public enum SceneTransitionState
{
    Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene,
    PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted,
    PreparingLoadingPresentation, RevealingLoadingPresentation, ResolvingTarget,
    AwaitingProceed, Finalizing
}
public sealed class GameSceneManager
public GameSceneManager(MonoBehaviour commonHost, SceneTransitionCallbacks callbacks = null,
    ISceneLoader buildLoader = null, ISceneLoader addressableLoader = null,
    SceneTransitionSettings settings = null);
public SceneTransitionState State { get; private set; }
public SceneTransitionState FailurePhase { get; private set; }
public Exception LastFailure { get; private set; }
public Scene GameScene { get; private set; }
public Scene LoadedScene { get; }
public IReadOnlyList<Scene> OwnedScenes { get; }
public IReadOnlyList<SceneRegistration> RegisteredScenes { get; }
public bool IsGamePrepared { get; }
public bool CanProceed { get; }
public UniTask EnterFirstSceneAsync(string scenePath, LoadSceneMode mode = LoadSceneMode.Additive,
    CancellationToken cancellationToken = default);
public UniTask EnterFirstSceneAsync(SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive,
    CancellationToken cancellationToken = default);
public UniTask WaitForEntryAsync(CancellationToken cancellationToken = default);
public UniTask ReplacePrimaryAsync(SceneTarget target, LoadSceneMode mode = LoadSceneMode.Additive,
    CancellationToken cancellationToken = default);
public UniTask ReplacePrimaryAsync(string scenePath, LoadSceneMode mode = LoadSceneMode.Additive,
    CancellationToken cancellationToken = default);
public UniTask<Scene> AddDerivedAsync(SceneTarget target, Scene parent, bool activate = false,
    int priority = 0, CancellationToken cancellationToken = default);
public UniTask<Scene> AddDerivedAsync(string scenePath, Scene parent, bool activate = false,
    int priority = 0, CancellationToken cancellationToken = default);
public UniTask RemoveDerivedAsync(Scene scene, Scene requester, CancellationToken cancellationToken = default);
public UniTask<bool> TryTransitionAsync(string definitionId, CancellationToken cancellationToken = default);
public UniTask<bool> TryTransitionAsync(SceneTransitionRequest request, CancellationToken cancellationToken = default);
public UniTask WaitForTransitionAsync(CancellationToken cancellationToken = default);
public void CancelTransition();
public UniTask ShutdownAsync();

public sealed class BootstrapSystem : MonoBehaviour
public GameSceneManager Manager { get; private set; }
public Scene GameScene { get; }
public LoadSceneMode LoadMode { get; }
public MonoBehaviour SceneRoot { get; }
public string FirstScenePath { get; }
public SceneSource Source { get; }
public string AddressableKey { get; }
public AssetReference SceneReference { get; }
public SceneTarget FirstSceneTarget { get; }
public bool AutoStart { get; }
public SceneTransitionSettings Settings { get; }
public string FirstTransitionId { get; }
public void Configure(MonoBehaviour sceneRoot, SceneTransitionSettings settings, string firstTransitionId,
    bool autoStart = true, SceneTransitionCallbacks callbacks = null);
public void Configure(MonoBehaviour sceneRoot, string firstScenePath, bool autoStart = true,
    SceneTransitionCallbacks callbacks = null, LoadSceneMode loadMode = LoadSceneMode.Additive);
public void Configure(MonoBehaviour sceneRoot, SceneTarget target, bool autoStart = true,
    SceneTransitionCallbacks callbacks = null, LoadSceneMode loadMode = LoadSceneMode.Additive);
public void ValidateConfiguration();
public static void ValidateSceneRoot(MonoBehaviour host, Scene scene, bool allowPersistence = false);
public static void ValidateScenePath(string path);
public UniTask BootstrapAsync(CancellationToken cancellationToken = default);
public UniTask ShutdownAsync();

public abstract class SceneTransitionCallbacks : MonoBehaviour
public virtual UniTask ShowCoverAsync(CancellationToken cancellationToken);
public virtual UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask HideCoverAsync(CancellationToken cancellationToken);
public virtual bool UsesLoadingPresentation(SceneLoadingContext context); // default false
public virtual UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken);
public virtual UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken);
public virtual void ReportLoadingProgress(SceneTransitionProgress progress);
public virtual UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken); // default completed
public virtual UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context);
public virtual void OnFailure(Exception exception);
// Deprecated/Obsolete 호환 타입
public abstract class BootstrapCallbacks : SceneTransitionCallbacks
public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask ConfigureGameAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);

public abstract class SceneTransitionCondition : MonoBehaviour
public abstract string ConditionId { get; }
public abstract bool Evaluate(SceneTransitionContext context);
public enum SceneTransitionKind { FirstEntry, ReplacePrimary, AddDerived, RemoveDerived }
public enum SceneRegistrationRole { Primary, Derived }

public sealed class SceneTransitionDefinition
public string Id { get; }
public SceneTransitionKind Kind { get; }
public string SourceScenePath { get; }
public SceneTarget Target { get; }
public LoadSceneMode Mode { get; }
public bool Activate { get; }
public int Priority { get; }
public IReadOnlyList<string> RequiredConditionIds { get; }
public SceneTransitionDefinition(string id, SceneTransitionKind kind, string sourceScenePath, SceneTarget target,
    LoadSceneMode mode = LoadSceneMode.Additive, bool activate = false, int priority = 0,
    string[] requiredConditionIds = null);
public sealed class SceneTransitionSettings : ScriptableObject
public void Configure(params SceneTransitionDefinition[] definitions);
public IReadOnlyList<SceneTransitionDefinition> CreateSnapshot();

public readonly struct SceneTransitionRequest
public SceneTransitionKind Kind { get; }
public SceneTarget Target { get; }
public Scene SourceScene { get; }
public Scene DestinationScene { get; }
public LoadSceneMode Mode { get; }
public bool Activate { get; }
public int Priority { get; }
public IReadOnlyList<string> RequiredConditionIds { get; }
public SceneTransitionRequest(SceneTransitionKind kind, SceneTarget target = default, Scene sourceScene = default,
    Scene destinationScene = default, LoadSceneMode mode = LoadSceneMode.Additive, bool activate = false,
    int priority = 0, string[] requiredConditionIds = null);

public readonly struct SceneTransitionContext
public SceneTransitionRequest Request { get; }
public Scene SourceScene { get; }
public Scene DestinationScene { get; }
public IReadOnlyList<Scene> AffectedScenes { get; }
public SceneTransitionContext(SceneTransitionRequest request, Scene sourceScene, Scene destinationScene,
    IEnumerable<Scene> affectedScenes);

public readonly struct SceneRegistration
public Scene Scene { get; }
public Scene Parent { get; }
public SceneRegistrationRole Role { get; }
public int Priority { get; }
public bool IsPrepared { get; }
public bool IsShuttingDown { get; }
public SceneRegistration(Scene scene, Scene parent, SceneRegistrationRole role, int priority,
    bool isPrepared, bool isShuttingDown);
public sealed class SceneTransitionRejectedException : InvalidOperationException
public SceneTransitionRejectedException(string message);
```

## Inputs / Outputs / Errors

- commonHost: Play active/enabled, unique top-level SceneOwnedRoot/SingletonSceneRoot, IsReady=true. null/부적합 host→InvalidOperationException. callback 및 UI는 common lifetime 안에 있어야 함. constructor는 root를 prepare하지 않음.
- loaders null→각 native 기본 loader. settings null→ID 미구성. settings runtime snapshot은 constructor에서 detached 고정. callbacks null→no-op presentation.
- target Source는 explicit BuildScene/Addressable; path overload는 BuildScene. 정규 Assets/.../*.unity 및 backend 매핑/실제 build 등록 검사를 통과해야 함. 동일 asset 중복 instance 거부.
- entry는 역사적 시도 하나. manager 반복 entry 거부; Bootstrap 반복 entry는 shared await. WaitForEntry/WaitForTransition은 시작 전 InvalidOperationException.
- add parent=prepared registered Scene; return=해당 command의 actual Scene. remove target=derived actual instance; requester=self/registered ancestor. 권한 오류→InvalidOperationException. 동일 pending remove 완료만 공유. priority 정수 제한 없음/metadata only. activate default false.
- TryTransitionAsync false=동기 preflight condition refusal only; true=accepted execution 완료. convenience refusal→SceneTransitionRejectedException. 구성/실행/취소는 false로 숨기지 않음.
- 잘못된 kind/source→ArgumentOutOfRangeException; mode/required IDs/target key 조합→ArgumentException; malformed scene path→InvalidOperationException; invalid lifetime/instance/overlap/후킹 재진입→InvalidOperationException; stop 뒤 새 command→ObjectDisposedException; cancel→OperationCanceledException. backend/observer/cleanup 오류 전달, 겹친 오류 AggregateException.
- GameScene=성공 primary, derived 활성화와 별개. LoadedScene=remaining candidate/primary. OwnedScenes=remaining actual loaded owners. RegisteredScenes=성공 등록 snapshot. CanProceed=Ready+common/all registered roots prepared+active/inventory/callback lifetime 일치. entry 완료 이력/IsGamePrepared만으로 gameplay permission 판단 금지.
- LastFailure=execution/cleanup/callback 및 owner cancel; caller wait cancel 제외. FailurePhase는 shutdown 뒤 보존.

## Ownership / Lifecycle / Threading

프로젝트가 manager를 소유한다. manager는 성공 load results와 primary/derived roots cleanup을 독점 소유한다. common root/callback/loader 경계 의존성은 대여하며 common 최종 shutdown은 외부 owner 담당. snapshot 읽기는 unload/reparent 권한 없음. 모든 명령·대기·종료·Unity state 읽기·hooks는 main thread. manager command guards는 worker 호출을 InvalidOperationException으로 거부한다.

## Concurrency / Cancellation / FailureCleanup

accepted 작업 하나, queue/자동재시도 없음. 동일 pending derived removal만 재권한 확인 후 shared completion. caller token은 해당 wait만 취소, owner 작업 지속. CancelTransition은 actual owner cancel 요청; completion은 원래 task/WaitForTransition으로 관찰. condition에서 CancelTransition 금지. Shutdown은 shared uncancelled terminal completion, owner cancel→late native 결과 확보→root shutdown→result unload.

Additive replace는 후보 준비 뒤 old subtree release. Single replace는 old roots shutdown 뒤 native load. native load 강제 cancel 없음. 실패 후보는 cleanup 시도하고 해제 실패 잔여 owner는 진단에 유지. Faulted에서는 진단과 explicit shutdown만 허용. 실패 cover 유지/reveal중 실패 재cover 시도, OnFailure도 오류를 성공으로 바꾸지 않음. last ordinary Single scene unload 제한은 Faulted+loaded/unprepared+예외로 보고, fallback 생성/자동 복귀 없음. 시작된 Shutdown은 같은 결과 공유.

## Configuration / ExtensionPoints

- Bootstrap Inspector: SceneRoot, first path/source/key/reference, load mode(default Additive), autoStart(default true), callbacks, optional settings/FirstTransitionId. Configure는 첫 시도 전. settings+ID는 함께, existing FirstEntry만. key/reference mutually exclusive. ValidateConfiguration은 active Bootstrap/common lifetime 검사.
- common persistence false+retained Bootstrap→Additive. persistence true→Bootstrap/callback 및 참조 UI가 root hierarchy 안에서 유지되어 Single/ Additive 가능. Single과 retained ordinary scenes 함께 사용 금지. destination은 exactly one active nonpersistent top-level host.
- settings Configure(null)=empty; 목록 복사, null/empty/duplicate ordinal IDs 거부. CreateSnapshot이 kind/mode/target/source/required IDs까지 검증하고 detached readonly 반환. FirstEntry source path empty, others normalized scene path. derived Additive only. Definition/Request required IDs 배열 복사, null empty. Context affectedScenes distinct copy/null empty. Registration constructor는 수동 snapshot, Unity lifetime 검증 없음.
- attached root Conditions는 disabled도 평가, IDs nonempty/unique per root, 다른 roots same ID 허용. required IDs nonempty/unique 및 affected roots 최소 하나 제공. 목록 empty도 attached policy 우회 안 됨. Evaluate는 synchronous business read only. composition/IDs accepted lifetime 동안 고정.
- affected: FirstEntry common; ReplacePrimary old primary subtree; AddDerived parent; RemoveDerived requester/parent/removed subtree. preflight 모든 조건 후 시작, release/reveal 경계 재검사. accepted 뒤 refusal은 owner cancellation failure cleanup.
- SceneTransitionCallbacks default no-op. ShowCover=cover+input block complete; ConfigureScene=installed consumer injection before prepare; PreparePresentation=prepared root presentation ready; HideCover=final reveal+자신의 transition block release only; OnFailure=failure/owner cancel 보고. borrowed root Configure/Dispose 금지. callbacks는 released scenes보다 긴 수명.
- BootstrapCallbacks는 Obsolete 호환 ConfigureGameAsync adapter. 새 구현 SceneTransitionCallbacks.ConfigureSceneAsync 사용. event/JSON/CSV 없음.

## RequiredSequence

1. common inactive root/ordered installers/persistence 및 callback hierarchy 설정 → activation/IsReady.
2. manager 생성 또는 Bootstrap.Configure(autoStart:false) 후 BootstrapAsync.
3. entry await → 현재 CanProceed 및 프로젝트 입력 permission 확인.
4. Ready에서 actual registered instances로 replace/add/remove 또는 injected ID 요청.
5. manager/Bootstrap.ShutdownAsync await → commonRoot.ShutdownAsync await → common owner Destroy. cleanup 오류가 있어도 나머지 cleanup 시도하고 원래 오류 보존.

## ForbiddenUsage

common installer에서 entry/self-prepare await, hook에서 같은 manager command/wait/shutdown, condition에서 start/stop/cancel/self-await, 외부 직접 owned scene load/unload/SetActiveScene/reparent, path만으로 removal authorization, Instance/entry completion만으로 readiness 판정, callback 의존 객체 선파괴, Single로 retained scene 보존, worker thread Unity 접근을 금지한다.

## Example / Compatibility / Limitations

[사람용 발췌](../../api/SceneManagement.md#사용-발췌)는 actual signatures에 맞춘 normal/add/remove/caller cancel/owner shutdown 순서이며 이번 compile/run0. placeholders는 프로젝트 제공. cleanup 동시 오류 aggregate reporting은 프로젝트 책임임을 예제에 명시한다.

`SceneLoadingContext`/`SceneTransitionProgress`:

- `SceneLoadingContext`: `OperationId`, `Kind`, `Target`, `Mode`; 변경 불가한 작업 식별자이며 씬 소유권은 없다.
- `SceneTransitionProgress`: `OperationId`, `Stage: SceneTransitionState`, `StageRatio: float?`, `IsPrepared`. 기존 enum 값 0..11은 유지하고 새 값을 뒤에 추가했다: `PreparingLoadingPresentation`, `RevealingLoadingPresentation`, `ResolvingTarget`, `AwaitingProceed`, `Finalizing`.
- `GameSceneManager.Progress`는 nullable이다. `ReportLoadingProgress`는 main thread 동기 callback이므로 manager를 재진입하지 않는다. `Ready`/`Faulted` terminal은 property로만 관찰하며 callback report는 없다. 단계 비율은 전체 진행률이 아니고 미지원 loader는 null을 보고한다. Addressables 해석은 UniTask progress의 bytes ratio 대신 operation `PercentComplete`를 사용한다.
- `ISceneProgressLoader`는 선택 구현이며 기존 `ISceneLoader`도 유효하다. observer 예외를 보관한 뒤 successful load result를 먼저 보유하고 candidate를 정리한 다음 실패를 보고한다. caller token은 caller의 await만 취소하고 실제 작업은 manager owner token이 제어한다.

opt-in load에서 manager는 `UsesLoadingPresentation`을 한 번 평가한다. 흐름: 기존 `ShowCoverAsync` → `PrepareLoadingPresentationAsync` → `RevealLoadingPresentationAsync`(가림막만 숨기고 transition input lease 유지) → load/configure/prepare → `AwaitingProceed` → `WaitForProceedAsync` → live policy 재검사 → `ShowCoverAsync` 재호출 → 이전 root 마무리 → 가림막 아래 `ReleaseLoadingPresentationAsync` 1회 → 기존 최종 `HideCoverAsync`(게임 공개 및 transition lease 해제). 기본값은 `UsesLoadingPresentation=false`, 완료된 `WaitForProceedAsync`다.

`Single` 교체는 native load 전 기존 root를 종료하므로 종료된 root의 condition을 재평가하지 않는다. `Additive`는 수동 대기 중 이전 root를 유지하고 중복 명령을 거부하며, 두 번째 가림막 이후 해제 전에 이전 root condition을 재검사한다. UI 공개 뒤 실패/취소 시 가림막 복구, candidate 정리, presentation release를 한 번씩 시도하고 오류를 aggregate한다. rollback은 보장하지 않는다. callback/UI hierarchy는 영향받는 씬보다 오래 살아야 하며 `Single`은 persistent common root 아래에 둔다. 프로젝트 sample UI 통합은 구현됐으며 2026-10-07 사용자가 로딩 UI PlayMode 확인을 전달했다.

확인 Unity6000.3.18f1/UniTask2.5.11/Addressables2.9.1/Windows Mono. 전체 p4 및 sample smoke가 다른 Unity/IL2CPP/플랫폼·원격 bundle download·임의 presentation/물리 입력 UX를 증명하지 않는다. core runtime UI/Input dependency 없음. 기존 Bootstrap path/source overload 및 Deprecated adapter 호환을 유지한다.

## ValidationStatus

현재 P4 전체 Edit271/271·Play253/253, 실패0/skip0이며 Windows Mono Additive/Single builds와 Players는 각12/12, Input 포함 consumer Editor build 1회와 Player run 1회가 성공했다([증거](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/scene-loading/p4/README.md)). 이 smoke는 최종 사용자 화면 확인, 물리 장치 UX, 원격 Addressables content/download, IL2CPP 또는 다른 플랫폼을 검증하지 않는다. 과거 P2 targeted 결과는 현재 전체 회귀 대신 사용하지 않는다.
