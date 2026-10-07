PackageVersion: 0.0.1. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.

# SceneManagement API

`GameSceneManager`는 명시적으로 선택한 Build Scene/Addressable 씬의 로드 결과와 root 수명을 소유한다. 공용 root·표시 callback은 대여하고, 최초 진입·주 씬 교체·파생 씬 tree의 준비와 종료를 관리한다. `BootstrapSystem`은 첫 진입 설정과 자동 시작을 연결하는 선택적 MonoBehaviour다. 게임 진행은 성공한 await와 현재 `CanProceed`, 프로젝트의 입력 차단 정책을 함께 확인해 허용한다.

| 항목 | 값 |
|---|---|
| Namespace / Assembly | `TPLab.Core.SceneManagement` / `TPLab.Core` |
| 의존성 | Unity, UniTask 2.5.11, Addressables 2.9.1. [asmdef](../../Runtime/TPLab.Core.asmdef) 참조. Core runtime에는 UI/Input 의존성이 없다. |
| SourceRevision | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` |
| ImplementationStatus / ValidationStatus | `Implemented` / `Partial` |
| 계약 원문 | [씬 관리](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/GAME_SCENE_MANAGER_DRAFT.md), [Bootstrap](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/BOOTSTRAP_SYSTEM.md), [로더](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/SCENE_LOADING.md), [Lifecycle](Lifecycle.md) |
| AI 참조 | [SceneManagement](../ai/api/SceneManagement.md) |

현재 소스에는 파생 tree·정의·조건 runtime, progress snapshot, opt-in 표시/진행 대기와 프로젝트 소유 sample UI가 구현되어 있다. P4 Edit271/271·Play253/253, Windows Mono Additive/Single build·Player(각 12/12), Input 포함 consumer Editor build 1회와 Player run 1회는 [P4 증거](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/scene-loading/p4/README.md)에 기록한다. 2026-10-07 사용자의 로딩 UI PlayMode 확인으로 사용자 gate를 완료했다.

## 구성·소유권·준비

공용 host는 Play에서 활성화·동기 설치가 성공한 SceneOwnedRoot 또는 SingletonSceneRoot여야 한다. manager를 공용 installer 안에서 생성·진입·자기 준비 await하는 순환을 만들지 않는다. 생성은 공용 root 준비를 시작하지 않는다. 공용 root와 callback/UI 의존성의 최종 종료는 외부 세션 소유자가 담당한다.

기본 권장은 Bootstrap 씬 유지 + Additive다. Single은 공용 root를 영속화하고 Bootstrap/callback 및 참조하는 UI·서비스도 그 영속 hierarchy에 두어야 한다. retained 일반 씬을 유지하면서 Single을 요청하면 거부한다. 첫 Single은 Bootstrap 일반 씬 하나이며 미등록 다른 root가 없어야 한다. Additive로 Single을 흉내 내거나 실패 시 backend를 바꾸지 않는다.

로드 목적지마다 유일한 활성 최상위 lifecycle host가 필요하며 영속화는 꺼야 한다. 공용 singleton과 목적지 singleton을 중복 소유하지 않는다. Unity activation으로 Awake/Install은 이미 실행될 수 있으므로 ConfigureSceneAsync에서는 설치된 소비자에 대여 참조를 연결하고 활성 root를 다시 Configure하지 않는다.

첫 진입 순서는 사전 검사 → ShowCover → 공용 준비 → native load → active 선택 → ConfigureScene → 목적지 root 준비 → PreparePresentation → 소유권/조건 검사 → HideCover → Ready다. Additive 교체는 후보 준비 후 이전 subtree를 해제하고, Single 교체는 이전 root를 먼저 graceful 종료한 뒤 native Single을 실행한다. 실패 시 이전 게임으로 자동 복귀하거나 빈 fallback 씬을 생성하지 않는다.

## GameSceneManager 호출 API

[소스](../../Runtime/SceneManagement/GameSceneManager.cs)의 public 호출 선언이다. `SceneTarget`/`ISceneLoader`는 `TPLab.Core.ResourceManagement` 소유이며 [로더 계약](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/SCENE_LOADING.md)을 따른다.

```csharp
public sealed class GameSceneManager
public GameSceneManager(MonoBehaviour commonHost, SceneTransitionCallbacks callbacks = null,
    ISceneLoader buildLoader = null, ISceneLoader addressableLoader = null,
    SceneTransitionSettings settings = null);
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
public UniTask RemoveDerivedAsync(Scene scene, Scene requester,
    CancellationToken cancellationToken = default);
public UniTask<bool> TryTransitionAsync(string definitionId, CancellationToken cancellationToken = default);
public UniTask<bool> TryTransitionAsync(SceneTransitionRequest request, CancellationToken cancellationToken = default);
public UniTask WaitForTransitionAsync(CancellationToken cancellationToken = default);
public void CancelTransition();
public UniTask ShutdownAsync();
```

null loader는 각각 NativeSceneLoader/AddressableSceneLoader를 사용한다. null callback은 표시 hook을 생략하고 null settings는 ID 호출을 구성하지 않는다. settings는 생성 시 검증한 detached snapshot으로 고정된다. path overload는 BuildScene만 뜻한다. Build Scene은 정규화된 `Assets/.../*.unity` 전체 경로와 실제 build 목록 포함을 요구하고, Addressable은 명시된 path와 고유 key/reference 매핑을 요구한다. `SceneTarget.BuildScene(path)`/`SceneTarget.Addressable(key, path)` 등으로 대상을 만든다. 코어가 GUID/address를 등록하지 않는다.

| 호출 | 입력·결과·공유 규칙 |
|---|---|
| EnterFirstSceneAsync | 한 역사적 최초 시도만 허용한다. 성공/실패 후 반복 명령도 거부한다. |
| WaitForEntryAsync | 이미 시작된 entry 완료만 기다린다. 완료 이력은 현재 진행 권한이 아니다. |
| ReplacePrimaryAsync | Ready의 실제 primary를 교체한다. 이전 primary 아래 파생 subtree도 종료한다. |
| AddDerivedAsync | 준비된 등록 parent 아래 Additive child를 추가하고 해당 명령의 실제 Scene을 반환한다. activate=false는 현재 active 유지, true는 새 child 활성화다. priority는 메타데이터이며 입력/focus/정렬 규칙이 아니다. 동일 asset의 동시 중복 인스턴스는 거부한다. |
| RemoveDerivedAsync | 실제 등록 derived 인스턴스를 자기 자신 또는 등록 ancestor가 요청해야 한다. subtree를 child-first로 해제한다. 같은 pending 대상은 requester 권한 검사 후 완료를 공유한다. 경로만으로 제거 권한을 얻지 않는다. |
| TryTransitionAsync | 실행 전 조건 false만 `false`이며 accepted 완료는 `true`다. 구성 오류·실행 실패·취소는 예외다. |
| WaitForTransitionAsync | 마지막 accepted 작업을 관찰한다. 새 명령을 만들지 않는다. 시작 전 호출은 오류다. |
| CancelTransition | 현재 작업의 실제 owner 취소를 요청한다. 완료 대기는 별도 Wait API/원래 작업을 사용한다. pending이 없으면 무해하다. |
| ShutdownAsync | 공유·호출자 취소 없는 terminal 종료. late native 결과까지 기다려 owned roots/results를 해제하며 공용 root는 유지한다. |

상태와 관찰 선언은 다음과 같다. snapshot 읽기는 해제·재부모화 권한을 이전하지 않는다.

```csharp
public enum SceneTransitionState
{
    Idle, Covering, PreparingCommon, Loading, Configuring, PreparingScene,
    PreparingPresentation, Revealing, Ready, Stopping, Stopped, Faulted
}
public SceneTransitionState State { get; private set; }
public SceneTransitionState FailurePhase { get; private set; }
public Exception LastFailure { get; private set; }
public Scene GameScene { get; private set; }
public Scene LoadedScene { get; }
public IReadOnlyList<Scene> OwnedScenes { get; }
public IReadOnlyList<SceneRegistration> RegisteredScenes { get; }
public bool IsGamePrepared { get; }
public bool CanProceed { get; }
```

`GameScene`은 성공 reveal된 primary이며 derived 활성화로 바뀌지 않는다. `LoadedScene`은 후보 또는 primary의 실제 잔여 씬이고 실패 진단에도 남을 수 있다. OwnedScenes는 잔여 후보/이전 씬도 포함하고 RegisteredScenes는 성공 등록의 snapshot이다. IsGamePrepared는 후보/primary root의 현재 준비 상태다. CanProceed는 Ready뿐 아니라 공용/등록 root 준비, active scene, 씬 inventory, callback 수명의 일치까지 확인한다. LastFailure는 실행/cleanup/callback 오류와 owner 취소를 기록하며 caller 대기 취소는 제외한다. FailurePhase는 실패 후 explicit shutdown 뒤에도 남는다.

## BootstrapSystem 설정과 호환 API

[소스](../../Runtime/SceneManagement/BootstrapSystem.cs). Inspector에 Scene Root, First Scene Path, Auto Start, Callbacks, Load Mode, Scene Source, Addressable Key/Scene Reference 또는 Transition Settings/First Transition Id를 지정한다. default는 AutoStart=true, Additive, BuildScene이며 씬 경로는 프로젝트가 제공한다. definition 구성을 선택하면 기존 target/mode 필드 대신 FirstEntry 정의를 사용한다.

```csharp
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
```

Configure는 첫 실행 전에만 허용한다. settings와 ID는 함께 지정하고 기존 FirstEntry를 선택한다. key/reference는 동시에 지정하지 않는다. Bootstrap은 active/enabled이며 callback은 유지되는 공용 측의 같은 scene에 속해야 한다. SceneRoot validation의 allowPersistence=false는 일반 게임 root, true는 공용 host용이다.

BootstrapAsync는 처음 manager를 공개하고 entry를 시작하며 반복 호출은 그 결과를 공유한다. AutoStart=true의 Start는 실패를 로그에 기록하므로 명시적 await가 필요한 프로젝트는 false를 지정한다. Shutdown은 manager만 종료한다. OnDestroy fallback은 비동기 처리를 await할 수 없으므로 파괴 전에 ShutdownAsync를 await한다.

## callback·조건·데이터 타입

[SceneTransitionCallbacks](../../Runtime/SceneManagement/SceneTransitionCallbacks.cs)는 프로젝트가 구현한다. 모든 hook의 기본값은 no-op/즉시 완료이며 root/scene을 소유하지 않는다.

```csharp
public abstract class SceneTransitionCallbacks : MonoBehaviour
public virtual UniTask ShowCoverAsync(CancellationToken cancellationToken);
public virtual UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask HideCoverAsync(CancellationToken cancellationToken);
public virtual void OnFailure(Exception exception);

// 기존 코드 호환. 타입은 Obsolete. 신규 구현은 SceneTransitionCallbacks를 상속한다.
public abstract class BootstrapCallbacks : SceneTransitionCallbacks
public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask ConfigureGameAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken);
```

ShowCover는 표시와 gameplay 입력 차단 완료, ConfigureScene은 준비 전 참조 접속, PreparePresentation은 준비된 root의 화면 준비 완료, HideCover는 최종 표시와 **전환이 소유한 차단만**의 반환을 의미한다. 다른 modal의 차단은 유지한다. OnFailure는 실행 실패/owner 취소를 보고하고 성공으로 변환하지 않는다. hook에서 같은 manager의 command/wait/shutdown을 호출하지 않는다. 오류 표시를 닫아도 reveal이 허용됐다고 취급하지 않는다. reveal 도중 실패는 cover 재표시를 시도한다.

조건은 root GameObject에 직접 붙인다. [SceneTransitionCondition](../../Runtime/SceneManagement/SceneTransitionCondition.cs)은 disabled component도 평가 대상이며 붙어 있는 모든 조건을 평가한다. 필수ID 목록이 비어도 policy를 우회하지 않는다.

```csharp
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

정의/settings [소스](../../Runtime/SceneManagement/SceneTransitionSettings.cs)는 ID를 ordinal 비교하고 null/empty/duplicate를 거부한다. Configure(null)은 빈 목록이고 Configure는 목록/ID를 검사한다. CreateSnapshot은 kind/mode/source/target/필수ID까지 검증하고 detached readonly 결과를 만든다. FirstEntry source path는 빈 값이며 나머지는 정규 scene path다. derived는 Additive만 허용한다. 정의와 request는 필수ID 배열을 복사하고 null을 empty로 취급한다. Request 생성 자체는 작업을 시작하거나 manager validation을 대신하지 않는다. `SceneTransitionContext`는 affected 씬을 distinct 복사하며 null은 empty다. `SceneLoadingContext`는 progress/presentation 전환별 OperationId, Kind, Target, Mode를 제공하며 scene/root 소유권을 주지 않는다. Registration은 수동 값 snapshot이며 현재 Unity 수명 검사나 소유권 획득을 하지 않는다. JSON/CSV 포맷은 없다.

ConditionId는 root 안에서 nonempty/unique, 다른 root와 같은 ID는 허용된다. required ID는 영향 root 중 하나 이상이 제공해야 한다. 최초는 common, 교체는 이전 primary subtree, 추가는 parent, 제거는 requester/parent/제거 subtree를 검사한다. ID와 컴포넌트 구성은 작업 중 고정하고 Evaluate는 동기로 business 상태만 읽는다. 조건에서 전환 시작·종료·취소·자기 대기를 금지한다. preflight false는 무변경 거부다. accepted 후 해제/reveal 경계의 재검사 거부는 취소 실패 경로로 정리한다.

## 취소·동시성·실패·종료

manager 명령·대기·종료와 Unity 상태 읽기는 메인 스레드에서 사용한다. 명령은 main-thread guard가 있고 worker 호출은 `InvalidOperationException`이다. 하나의 accepted 작업만 실행하며 별도 큐/자동 재시도는 없다. 같은 pending derived 제거 이외의 overlap, callback/condition 재진입, 잘못된 instance/ownership/configuration, Faulted 상태 신규 명령은 `InvalidOperationException` 계열 오류다. 종료 뒤 새 명령은 `ObjectDisposedException`이다. 잘못된 kind/source는 ArgumentOutOfRangeException, mode/required IDs/target key 조합은 ArgumentException, 잘못된 scene path는 InvalidOperationException이다. loader의 검사·로드 오류도 그대로 전달된다.

caller token은 해당 await만 취소하며 작업은 계속된다. `CancelTransition`/`ShutdownAsync`가 실제 owner 취소를 요청한다. native load는 강제 중단하지 않고 완료 결과를 먼저 소유한 뒤 candidate root를 종료하고 해당 result를 unload한다. cleanup에는 caller token을 전달하지 않는다. callback·cleanup 추가 실패는 원래 오류와 합쳐 전달되며 Faulted에서 관찰과 explicit shutdown만 허용한다. caller wait 취소는 OnFailure/LastFailure를 발생시키지 않는다.

외부에서 manager 소유 씬을 직접 load/unload/reparent/SetActiveScene하지 않는다. snapshot 경로나 key로 unrelated 씬을 해제하지 않는다. 소유 inventory 불일치도 실행 오류다. 씬 제거 전에 root shutdown을 await하고 child-first로 해제한다. 마지막 일반 Single 씬은 Unity unload 제한으로 loaded-but-unprepared 상태와 Faulted/예외가 남을 수 있다. 자동 fallback은 없다. 이미 시작된 ShutdownAsync는 같은 terminal 결과를 공유한다.

종료는 `await manager.ShutdownAsync()` → `await commonRoot.ShutdownAsync()` → 공용 host 파괴다. manager 오류가 있어도 공용 cleanup을 빠뜨리지 않도록 프로젝트가 예외를 보존하며 시도한다. callback은 manager cleanup까지 살아 있어야 한다.

## 사용 발췌

아래 **설명용 발췌는 이 문서 작업에서 compile/run하지 않았다**. async UniTask 메서드 본문이며 commonHost는 활성·설치된 비영속 SceneOwnedRoot, presentation은 같은 retained 공용 scene의 프로젝트 callback이다. destination들은 실제 build 목록과 유일한 비영속 root를 갖춰야 한다. callerToken은 프로젝트가 제공한다.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

var scenes = new GameSceneManager(commonHost, presentation);
try
{
    await scenes.EnterFirstSceneAsync("Assets/Scenes/Hub.unity", cancellationToken: callerToken);
    if (scenes.CanProceed)
    {
        Scene child = await scenes.AddDerivedAsync("Assets/Scenes/Room.unity", scenes.GameScene,
            activate: false, cancellationToken: callerToken);
        await scenes.RemoveDerivedAsync(child, requester: scenes.GameScene, cancellationToken: callerToken);
    }
}
catch (OperationCanceledException)
{
    // caller 대기 취소만으로 소유 작업은 취소되지 않는다.
    scenes.CancelTransition();
    throw;
}
finally
{
    try
    {
        await scenes.ShutdownAsync();
    }
    finally
    {
        try
        {
            await ((ISceneRoot)commonHost).ShutdownAsync();
        }
        finally
        {
            UnityEngine.Object.Destroy(commonHost.gameObject);
        }
    }
}
```

발췌의 finally 구조는 모든 cleanup 시도를 보여주지만 cleanup이 함께 실패하면 예외를 합쳐 보고하는 코드는 프로젝트가 추가해야 한다. ID 정의 거부와 실패를 구분하는 발췌다. settings는 manager 생성자에 먼저 주입한다.

```csharp
bool completed = await scenes.TryTransitionAsync("hub-to-main", callerToken);
if (!completed)
{
    // 동기 preflight policy 거부: cover/load/OnFailure는 시작되지 않았다.
}
// true여도 현재 gameplay 허용은 scenes.CanProceed와 프로젝트 입력 정책을 확인한다.
```

## 검증·호환성·한계

[역사적 입력 P4](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/README.md)는 이전 source의 Edit258/258·Play217/217과 reload/consumer 범위를 기록한다. 현재 SceneManagement P4 결과는 Edit271/271·Play253/253(실패0·skip0), Windows Mono Additive/Single build와 Player 각 12/12, Input 포함 consumer Editor build 1회와 Player run 1회 성공이다([현재 증거](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/scene-loading/p4/README.md)). P2 targeted 36/36과 이전 252/224 수치는 역사적 결과다. 사용자의 로딩 UI PlayMode 확인은 완료됐으며 개별 모드·해상도·물리 장치 결과는 기록되지 않았다. 문서 예제 발췌는 계속 NotRun이다.

확인 환경은 Unity6000.3.18f1/UniTask2.5.11/Addressables2.9.1/Windows Mono다. 다른 Unity·IL2CPP·플랫폼, 원격 Addressables download와 개별 물리 장치·해상도 coverage는 미검증이다. `BootstrapCallbacks`는 Deprecated 호환 타입이며 새 코드는 `SceneTransitionCallbacks.ConfigureSceneAsync`를 사용한다. SceneRootFlow/Lifecycle의 기존 준비·종료 계약은 유지한다. UI·bar·tips·button input 구현은 소비 프로젝트가 소유한다.

## Progress와 선택적 로딩 표시/대기

검증 tip의 API와 순서는 아래와 같다. 예제는 계약 발췌(NotRun)이며 이 문서에서는 compile/run하지 않았다.

```csharp
public readonly struct SceneLoadingContext
{
    public Guid OperationId { get; }
    public SceneTransitionKind Kind { get; }
    public SceneTarget Target { get; }
    public LoadSceneMode Mode { get; }
}

public readonly struct SceneTransitionProgress
{
    public Guid OperationId { get; }
    public SceneTransitionState Stage { get; }
    public float? StageRatio { get; }
    public bool IsPrepared { get; }
}

public sealed class GameSceneManager
{
    public SceneTransitionProgress? Progress { get; }
}

public abstract class SceneTransitionCallbacks : MonoBehaviour
{
    public virtual bool UsesLoadingPresentation(SceneLoadingContext context) => false;
    public virtual UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;
    public virtual UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;
    public virtual void ReportLoadingProgress(SceneTransitionProgress progress) { }
    public virtual UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;
    public virtual UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context) => UniTask.CompletedTask;
}
```

로드 작업이 승인되면 `UsesLoadingPresentation`을 한 번 평가하며 기본값은 false다. 기본 prepare/reveal/release callback은 no-op 완료 task이고 `WaitForProceedAsync`도 즉시 완료한다. `SceneTransitionProgress.Stage`는 기존 `SceneTransitionState`를 사용한다. 직렬화된 기존 enum 값 0..11을 보존하기 위해 새 값은 뒤에 추가했다: `PreparingLoadingPresentation`, `RevealingLoadingPresentation`, `ResolvingTarget`, `AwaitingProceed`, `Finalizing`. `StageRatio`는 해당 단계 비율이거나 null이며 전체 진행률이 아니다. Addressables 해석 단계는 operation `PercentComplete`를 보고한다. UniTask가 해석 callback에 주는 byte 비율은 사용하지 않는다. root와 project presentation 준비 후 `IsPrepared`가 true가 되며, 이전 root 해제와 최종 reveal은 아직 남을 수 있다. `Ready`/`Faulted`는 property에서만 관찰하며 `ReportLoadingProgress`를 호출하지 않는다.

`ReportLoadingProgress`는 Unity main thread에서 동기로 실행되며 manager 명령을 재진입하면 안 된다. callback의 첫 예외는 보관하고 native/Addressables 로드 결과의 소유권 확보를 계속한다. 성공 결과를 manager에 넘긴 뒤 candidate cleanup을 수행하고 실패를 보고한다. progress를 지원하지 않는 기존 `ISceneLoader`도 그대로 사용할 수 있으며 해당 backend 단계 비율은 null이다. caller 취소는 caller의 await만 취소하고 실제 작업은 manager owner token으로 취소한다. 수동 callback은 전달받은 owner token으로 operation별 완료 소스를 기다리고, 오래된 `OperationId`의 버튼 이벤트를 폐기한다.

Opt-in 순서는 다음과 같다. 첫 `ShowCoverAsync`가 전환 입력 차단을 획득한다. `PrepareLoadingPresentationAsync`는 가림막 뒤에서 project UI를 준비한다. `RevealLoadingPresentationAsync`는 UI를 보이고 가림막만 숨기며 기존 전환 차단을 유지한다. manager는 씬을 load/inject하고 root와 project presentation을 준비한 다음 `AwaitingProceed`를 공개한다(`CanProceed`는 false). 이어 `WaitForProceedAsync`를 기다리고 현재 조건을 재검사한 뒤 가림막을 다시 보인다. 가림막 뒤에서 이전 root를 마무리하고 `ReleaseLoadingPresentationAsync`를 한 번 호출한다. 마지막 기존 `HideCoverAsync`가 게임 화면을 공개하고 전환 차단을 해제한다.

```csharp
// 설명용 발췌(NotRun): 자동 진행은 기본 완료 구현을 그대로 쓴다.
public override bool UsesLoadingPresentation(SceneLoadingContext context) => true;

// 설명용 발췌(NotRun): 수동 진행은 프로젝트 callback이 per-operation TCS를 만들고
// 버튼 handler에서 matching OperationId만 TrySetResult 한 뒤, token으로 취소 가능한 await를 반환한다.
public override async UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken token)
{
    var gate = CreateGate(context.OperationId);
    await gate.Task.AttachExternalCancellation(token);
}
```

`Single` 교체는 native load 전에 기존 root를 종료하므로 종료된 이전 root의 condition을 다시 평가하지 않는다. `Additive` 수동 대기 중에는 이전 root를 계속 소유하고 다른 전환을 거부한다. 진행 허용 후 두 번째 가림막 아래에서 이전 root를 해제하기 전에 condition을 재검사한다. 버튼은 preparation 완료 뒤에만 활성화하고 미리 누른 입력은 승인으로 저장하지 않는다. 표시 후 실패나 owner 취소가 발생하면 먼저 가림막 복구, candidate 정리, loading UI release를 각각 시도하고 오류를 합쳐 보고한다. 이전 씬 rollback은 보장하지 않는다. 특히 `Single`에서는 callback/UI/EventSystem이 persistent common hierarchy 안에 있어야 한다.
