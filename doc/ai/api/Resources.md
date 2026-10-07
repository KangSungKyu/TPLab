# Resource Management

**Module / Namespace / Assembly:** Resources / `MyLab.Core.ResourceManagement` / `MyLab.Core`

**SourceRevision / SourcePath / HumanContract:** `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`; [`ResourceManager.cs`](../../../Assets/MyLab/Core/ResourceManagement/ResourceManager.cs), [`ResourceManagerInstaller.cs`](../../../Assets/MyLab/Core/ResourceManagement/ResourceManagerInstaller.cs), [`SceneTarget.cs`](../../../Assets/MyLab/Core/ResourceManagement/SceneTarget.cs), [`ISceneLoader.cs`](../../../Assets/MyLab/Core/ResourceManagement/ISceneLoader.cs), [`NativeSceneLoader.cs`](../../../Assets/MyLab/Core/ResourceManagement/NativeSceneLoader.cs), [`AddressableSceneLoader.cs`](../../../Assets/MyLab/Core/ResourceManagement/AddressableSceneLoader.cs), [`LoadedScene.cs`](../../../Assets/MyLab/Core/ResourceManagement/LoadedScene.cs); [`Resources.md`](../../api/Resources.md).

**ImplementationStatus / ValidationStatus / Evidence:** Implemented / Partial. 현행 source revision의 Unity `6000.3.18f1` Windows Mono 전체 회귀는 [EditMode 258/258](../../validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](../../validation/input-system/p4/full-PlayMode.json); 실행 범위는 [P4 기록](../../validation/input-system/p4/README.md). Windows Mono 소비 Editor build와 Player도 성공했으나 ResourceManager smoke는 빈 manager 종료만 확인: [최종 소비 결과](../../validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json). 실제 소비 프로젝트 Addressables catalog/content 및 원격 다운로드는 검증하지 않음. IL2CPP·다른 플랫폼 미실행.

**Symbol / Signature / Constraints:**

```csharp
public sealed class ResourceManager : IDisposable
public ResourceManager() // 암시적 public parameterless constructor
public UniTask InitializeAsync(CancellationToken cancellationToken = default)
public UniTask<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default)
    where T : UnityEngine.Object
public UniTask ShutdownAsync()
public void Dispose()
public bool IsInitialized { get; }
public bool IsDisposed { get; }

public sealed class ResourceManagerInstaller : SceneRootInstaller
public ResourceManager Resources { get; private set; }
public override void Install(ISceneRoot root)
public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
public override UniTask ReleaseAsync(ISceneRoot root)
public override void Uninstall(ISceneRoot root)

public enum SceneSource { BuildScene, Addressable }
public readonly struct SceneTarget
public SceneTarget(SceneSource source, string scenePath, string addressableKey = null)
public SceneSource Source { get; }
public string ScenePath { get; }
public string AddressableKey { get; }
public static SceneTarget BuildScene(string scenePath)
public static SceneTarget Addressable(string key, string scenePath)
public static SceneTarget Addressable(AssetReference reference, string scenePath)
public void Validate()
public static void ValidateScenePath(string scenePath)

public interface ISceneLoader
{
    void Validate(SceneTarget target);
    UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode);
}

public sealed class NativeSceneLoader : ISceneLoader
public void Validate(SceneTarget target)
public UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)

public sealed class AddressableSceneLoader : ISceneLoader
public void Validate(SceneTarget target)
public UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)

public sealed class LoadedScene
public LoadedScene(SceneTarget target, Scene scene, Func<UniTask> unloadAsync)
public UniTask UnloadAsync()
public SceneTarget Target { get; }
public Scene Scene { get; }
public bool IsUnloaded { get; }
```

확인한 의존성: UniTask `2.5.11`, Addressables `2.9.1`; assembly refs `UniTask`, `UniTask.Addressables`, `Unity.Addressables`, `Unity.ResourceManager`.

**Inputs / Outputs / Errors:** `LoadAssetAsync<T>` key는 공백이 아니어야 하고 T는 `UnityEngine.Object` 파생. ordinal key 하나는 정확한 요청 타입 하나에 고정; 다른 타입 요청은 `InvalidOperationException`. 빈 key는 `ArgumentException`; 종료된 owner는 `ObjectDisposedException`; caller/owner 취소는 `OperationCanceledException`; native Addressables 실패는 전파. 실패 요청은 제거되어 retry 가능. `SceneTarget` 잘못된 enum은 `ArgumentOutOfRangeException`, source/key 불일치는 `ArgumentException`, 경로 위반은 `InvalidOperationException`; path 검사만으로 자산 존재 여부는 확인하지 않음. `Addressable(AssetReference, ...)`은 null/무효 reference를 거부. loader mode는 Single/Additive만 허용하고 동일 경로가 이미 로드됐으면 거부; Native는 enabled Player build scene도 요구. Addressables scene key는 `SceneInstance` 위치 1개로 해석되어야 함. 실패 로드의 backend 부분 자원은 반환 전에 정리. `LoadedScene`은 invalid target, invalid/unloaded Scene, null delegate 거부. 언로드 실패는 모든 awaiter에 공유되며 `IsUnloaded=false`; 씬이 로드된 상태에서 delegate가 완료되면 오류.

**Ownership / Lifecycle / Threading:** `ResourceManager`는 ordinal key+요청 타입별 성공한 **에셋**을 캐시. 에셋은 borrowed이고 manager가 handle을 소유/release. 모든 manager 메서드는 Unity main thread만 허용. `InitializeAsync`는 초기화 작업을 공유하고 실패 후 retry 허용. `LoadAssetAsync`가 초기화부터 수행. caller 취소는 해당 대기만 취소; owner `Dispose`는 대기자 취소, 영구 종료, 완료 handle release, 미완료 handle은 완료 시 release. 모든 caller가 취소해도 manager는 진행 load를 보유하고 성공 결과 캐시. `ShutdownAsync`는 즉시 종료 시작 후 native 작업/handle release drain을 기다리는 취소 불가능 공유 task; provider 정체 시 대기 유지. global catalog release나 bundle 메모리 즉시 회수 보장은 없음. `LoadedScene`은 asset cache와 별개로 실제 씬 인스턴스 하나와 backend unload 자원을 독점 소유하며 public 메서드는 main-thread만. `UnloadAsync`는 한 번 시작해 성공/실패를 공유하고 취소·retry 없음. `ResourceManagerInstaller.Install`에서 scope 생성, `PrepareAsync`에서 Addressables readiness만 대기, `ReleaseAsync`에서 drain, `Uninstall`에서 참조 초기화와 즉시 fallback dispose.

**Concurrency / Cancellation / FailureCleanup:** ResourceManager는 초기화와 key/type별 native load를 공유. caller 취소는 대기 전용, owner 종료는 영구적이며 대기자 취소. `ShutdownAsync`는 취소 불가이며 모든 native operation/handle release까지 대기. Background/thread-safe API 아님. Scene loader native 작업은 취소 불가; manager/caller는 늦게 도착한 `LoadedScene` 결과의 소유권을 보관하고 정리해야 함. `LoadedScene.UnloadAsync` 동시/후속 호출은 동일 operation과 실패를 공유하며 retry 안 함. loader는 `LoadedScene`에 소유권을 넘기기 전 부분 backend 자원을 정리.

**Configuration / ExtensionPoints:** 소비 프로젝트가 Addressables runtime data/catalog와 고유 에셋 address를 설정. catalog update/download는 요청하지 않음. Native 씬은 enabled Player build scene에 등록. Addressables 씬은 정확히 하나의 scene location으로 해석되는 address/reference와 정규화된 `Assets/.../*.unity` path 필요. `SceneTarget.ValidateScenePath`는 `//`, `\\`, `:`, CR/LF/TAB, `.`/`..` 구간 거부; 모든 제어 문자를 검사하지는 않음. `ResourceManagerInstaller`는 consumer installer보다 먼저 root 순서에 배치하고 `Resources`를 명시 주입. 정상 종료에서 consumer pool을 먼저 정리하고 manager를 마지막 drain. Custom `ISceneLoader`는 side effect 전에 target 검증, 정확한 씬 인스턴스만 언로드하는 uncancelled delegate를 가진 `LoadedScene` 반환, 자기 backend 자원만 release. 진행률/로딩 화면 UI API 없음.

**RequiredSequence / ForbiddenUsage:** 에셋: Addressables 구성 → manager 설치/주입 → `LoadAssetAsync<T>` await → borrower 중지·clone 정리 → `ShutdownAsync` await. 최초 에셋 로드 전에 `InitializeAsync`는 선택. 씬: 명시 `SceneTarget` 생성 → 검증 → 로드 → 반환 `LoadedScene` 보관 → 해당 owner로 `UnloadAsync` await. 에셋 캐시와 씬 handle 혼용, borrowed asset destroy/release, 같은 key에 복수 타입 요청, background thread에서 manager 사용, 실제 owner scene 대신 path unload, private handle 별도 release, cancellation이 native 작업까지 중단한다고 가정, 진행 UI API를 있다고 주장 금지.

**Example / Compatibility / Limitations:** 사람용 계약 예제는 설명 발췌이며 NotRun. 이 source revision에서 확인한 migration 변경 없음. 검증은 현재 명시한 Unity/Windows Mono 범위로 제한. test provider와 빈 manager shutdown은 실제 Addressables content/원격 다운로드 검증이 아님.

## 진행률 확장 (2026-10-07 P1)

두 기본 loader는 ISceneProgressLoader를 구현하며 기존 LoadAsync(target, mode)는 유지한다. 추가 overload는 SceneLoadProgressObserver를 받는다. SceneLoadProgress.Stage는 ResolvingTarget/LoadingScene이고 Ratio는 finite0..1이며 전체 준비/다운로드 bytes 비율이 아니다. observer의 callback 예외는 Failure에 첫1개 보존하고 native 완료를 중단하지 않는다. 반환 LoadedScene을 먼저 소유한 뒤 Failure를 확인하여 결과를 정리한다. null/Dispose는 통지만 억제하며 native 취소가 아니다. 통지는 동기 메인 스레드다. Native AsyncOperation.progress/Addressables PercentComplete를 읽는다. Green Edit13/13, core Play210/210, failed0/skip0. 원격 다운로드·다른 플랫폼은 미검증이다.
