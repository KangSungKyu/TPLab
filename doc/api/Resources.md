# 리소스 관리 API

`MyLab.Core.ResourceManagement` 네임스페이스는 `MyLab.Core` 어셈블리에 있습니다. 이 모듈은 서로 다른 두 소유권 경로를 제공합니다.

- `ResourceManager`는 프로젝트 또는 씬 범위의 Addressables **에셋 캐시**입니다. 반환된 에셋은 manager가 종료될 때까지 빌려 쓰는 객체입니다.
- `ISceneLoader`는 실제 로드된 씬 인스턴스 하나와 해당 인스턴스를 언로드할 백엔드 핸들을 독점하는 `LoadedScene`을 반환합니다. 씬 로드는 에셋 캐시를 사용하지 않습니다.

Addressables 설정, catalog/runtime data, 에셋 주소는 소비 프로젝트가 제공합니다. Native 씬은 Player build scene 목록에 활성화되어 있어야 합니다. 모듈에는 로딩 진행률이나 로딩 화면 UI API가 없습니다.

**SourceRevision:** `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`
**ImplementationStatus:** Implemented
**ValidationStatus:** Partial — Unity `6000.3.18f1` / Windows Mono에서 전체 회귀와 소비 Player를 확인했습니다. 소비 프로젝트의 실제 Addressables catalog/content 및 원격 다운로드는 검증하지 않았습니다. IL2CPP와 다른 플랫폼도 미실행입니다.

확인한 프로젝트 의존성은 UniTask `2.5.11`, Addressables `2.9.1`이며 어셈블리 참조는 `UniTask`, `UniTask.Addressables`, `Unity.Addressables`, `Unity.ResourceManager`입니다. 이는 조사한 프로젝트 버전이며 일반적인 호환성 보장을 뜻하지 않습니다.

## Addressables 에셋 캐시: `ResourceManager`

```csharp
public sealed class ResourceManager : IDisposable
public UniTask InitializeAsync(CancellationToken cancellationToken = default)
public UniTask<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default)
    where T : UnityEngine.Object
public UniTask ShutdownAsync()
public void Dispose()
public bool IsInitialized { get; }
public bool IsDisposed { get; }
```

public parameterless constructor는 암시적으로 제공됩니다. 호출은 Unity 메인 스레드에서 해야 합니다. `InitializeAsync`는 진행 중인 Addressables 초기화 작업을 공유합니다. 실패하면 다시 시도할 수 있습니다. caller token 취소는 해당 호출자의 대기만 취소하며, `Dispose`는 모든 대기자를 취소하고 manager를 영구 종료합니다. 초기화는 catalog update나 프로젝트 전용 다운로드를 요청하지 않습니다. Addressables runtime data는 소비 프로젝트가 설정해야 합니다.

`LoadAssetAsync<T>`는 null·빈 문자열·공백 key에 `ArgumentException`을 던집니다. key는 ordinal 비교를 하며 첫 요청의 정확한 `T` 타입에 묶입니다. 같은 key에 다른 타입을 요청하면 `InvalidOperationException`이 발생합니다. label 대신 고유한 에셋 address를 사용하세요. 동일 key·타입의 동시 요청은 native load 한 건을 공유하고, 성공 결과는 종료할 때까지 캐시됩니다. 실패한 요청은 캐시에서 제거되어 재시도할 수 있습니다. Addressables 오류는 전파되고, 종료 뒤 호출은 `ObjectDisposedException`, caller/owner 취소는 `OperationCanceledException`을 던집니다. 대기자가 모두 취소되어도 owner가 native load를 계속 소유하며 이후 성공 결과를 캐시합니다.

반환된 Unity 객체는 대여 상태입니다. 직접 `Destroy`하거나 Addressables 핸들을 release하지 마세요. manager를 닫기 전에 소비자를 중지하고 프리팹 복제본을 정리해야 합니다. `Dispose`는 새 작업을 즉시 거부하고 대기자를 취소하며 완료된 핸들을 release합니다. 아직 진행 중인 핸들은 작업이 완료될 때 release합니다. `ShutdownAsync`는 종료를 시작하고 모든 native 작업과 핸들 정리가 끝날 때까지 기다리는 공유 완료 작업을 반환합니다. native 작업이 멈추면 종료도 계속 대기합니다. 전역 catalog를 언로드하지 않으며 bundle 메모리가 즉시 회수된다고 보장하지 않습니다. 반복 호출은 안전합니다.

아래는 설명용 발췌이며 **실행하지 않았습니다**.

```csharp
var resources = new ResourceManager();
try
{
    Sprite icon = await resources.LoadAssetAsync<Sprite>("ui.main-icon", cancellationToken);
    // manager가 살아 있는 동안 icon을 빌려 사용합니다.
}
finally
{
    await resources.ShutdownAsync();
}
```

key, Addressable 항목, catalog, runtime data와 `Sprite` 사용은 소비 프로젝트가 제공합니다. `LoadAssetAsync`가 먼저 초기화하므로 그 전에 `InitializeAsync`를 호출할 필요는 없습니다.

## 씬 root 구성: `ResourceManagerInstaller`

```csharp
public sealed class ResourceManagerInstaller : SceneRootInstaller
public ResourceManager Resources { get; private set; }
public override void Install(ISceneRoot root)
public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
public override UniTask ReleaseAsync(ISceneRoot root)
public override void Uninstall(ISceneRoot root)
```

소비 에셋을 사용하는 installer보다 앞에 이 컴포넌트를 root의 installer 목록에 둡니다. Root 설치 시 manager를 만들고, root 준비에서는 Addressables 초기화 완료까지만 기다립니다. 이후 에셋을 사용하는 각 installer가 필요한 `LoadAssetAsync`를 기다린 뒤 root가 공개될 수 있습니다. 정상 종료 때 소비자가 pool과 대여 에셋을 먼저 해제하고 manager가 마지막에 drain합니다. Installer의 release 순서는 역순입니다. `Uninstall`은 `Resources`를 비우고 즉시 idempotent 정리를 수행하며 Unity 파괴 경로의 fallback도 제공합니다. 설치 전과 uninstall 후 `Resources`는 null입니다. 전역 singleton 대신 소비자에게 참조를 명시적으로 주입하세요.

uninstall 전에 `Install`을 다시 호출하면 `InvalidOperationException`이 발생합니다. `PrepareAsync`는 Addressables 오류와 root/caller 취소를 전파합니다. `ReleaseAsync`에는 취소 인수가 없으며 manager drain을 기다립니다. `Uninstall`은 즉시 fallback 정리이며 native 작업의 drain 완료를 기다리지 않습니다.

## 씬 대상과 독점 씬 수명

```csharp
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
public void Validate(SceneTarget target)
public UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)

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

`SceneTarget`는 백엔드를 명시하며 loader는 다른 백엔드로 자동 대체하지 않습니다. `ScenePath`는 `/`를 쓰는 정규화된 전체 `Assets/.../*.unity` 경로여야 합니다. `//`, 역슬래시, 콜론, CR/LF/TAB, `.` 또는 `..` 경로 구간은 허용되지 않습니다. 다른 제어 문자를 모두 검사하는 것은 아닙니다. 경로 유효성 검사만으로 에셋 존재 여부를 확인하지는 않습니다. `BuildScene`은 Addressables key가 null이어야 하고, `Addressable`은 공백이 아닌 key가 필요합니다. `AssetReference` 오버로드는 null이 아니고 유효한 runtime key를 가진 참조를 요구한 뒤 그 key를 문자열로 변환합니다. 잘못된 enum은 `ArgumentOutOfRangeException`, source/key 조합 오류는 `ArgumentException`, 형식이 잘못된 경로는 `InvalidOperationException`을 던집니다.

두 loader 모두 Unity 메인 스레드에서 검증하고 `LoadSceneMode.Single` 또는 `LoadSceneMode.Additive`만 허용합니다. `NativeSceneLoader`는 Player build scene 목록에서 활성화된 씬도 요구합니다. `AddressableSceneLoader`는 로드 시 key를 해석하고 `SceneInstance` 위치가 정확히 하나인지 확인합니다. 소비 프로젝트에서 고유한 Addressables 씬 address를 구성해야 합니다. 두 loader 모두 요청한 경로의 씬이 이미 로드된 경우 거부합니다. 같은 에셋을 중복 인스턴스화하는 흐름은 지원하지 않습니다. Native 완료 작업은 취소할 수 없습니다. 로드가 실패하면 부분 backend 자원을 정리하고 오류를 던집니다. 잘못된 씬을 성공으로 반환하지 않습니다.

`LoadedScene`은 실제 로드된 `Scene` 인스턴스와 비공개 언로드 작업을 소유합니다. custom loader가 생성할 수 있도록 생성자가 공개되어 있으며 유효한 target, 실제로 로드된 Scene, null이 아닌 delegate가 필요합니다. 잘못된/언로드된 Scene에는 `ArgumentException`, null delegate에는 `ArgumentNullException`이 발생합니다. delegate는 해당 씬 인스턴스만 언로드하고 자기 backend 자원만 해제해야 합니다. backend handle을 공유하거나 별도로 해제하지 마세요. `UnloadAsync`는 메인 스레드에서 한 번 시작하고, 동시에 또는 나중에 호출된 모든 대기자에게 같은 완료/실패를 전달합니다. 취소와 자동 재시도는 없습니다. 실패하면 `IsUnloaded == false`를 유지합니다. 성공은 Unity에서 실제로 씬이 언로드된 뒤에만 보고됩니다. delegate가 완료됐는데 씬이 여전히 로드되어 있으면 실패합니다.

아래는 설명용 발췌이며 **실행하지 않았습니다**.

```csharp
var target = SceneTarget.BuildScene("Assets/Scenes/Gameplay.unity");
var loader = new NativeSceneLoader();
loader.Validate(target);
LoadedScene ownedScene = await loader.LoadAsync(target, LoadSceneMode.Additive);
try
{
    // 이 씬 인스턴스가 사용되는 동안 결과를 보관합니다.
}
finally
{
    await ownedScene.UnloadAsync();
}
```

Addressables 씬은 소비 프로젝트 catalog에 설정한 뒤 `SceneTarget.Addressable("unique-scene-address", "Assets/Scenes/Gameplay.unity")`처럼 만듭니다. `GameSceneManager`는 명시된 target에 맞는 loader를 사용합니다. 반환된 씬 인스턴스 대신 경로 검색을 사용하지 마세요.

## 검증 근거와 한계

- 현행 source revision 전체 회귀: [EditMode 258/258](../validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](../validation/input-system/p4/full-PlayMode.json). ResourceManager native test-provider 검증도 포함합니다. 실행 환경과 범위는 [P4 검증 기록](../validation/input-system/p4/README.md)을 참조하세요.
- 현행 Input 포함 소비 프로젝트에서 Windows Mono Editor build와 Player가 성공했습니다. Player smoke의 ResourceManager 검사는 빈 manager 종료입니다. [최종 소비 결과](../validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json).
- 이 근거는 Unity `6000.3.18f1` / Windows Mono만 입증합니다. 실제 소비 프로젝트 Addressables catalog/content 또는 원격 다운로드는 검증하지 않았고, IL2CPP·다른 플랫폼도 미실행입니다.

구현: [`ResourceManager.cs`](../../Assets/MyLab/Core/ResourceManagement/ResourceManager.cs), [`ResourceManagerInstaller.cs`](../../Assets/MyLab/Core/ResourceManagement/ResourceManagerInstaller.cs), [`SceneTarget.cs`](../../Assets/MyLab/Core/ResourceManagement/SceneTarget.cs), [`ISceneLoader.cs`](../../Assets/MyLab/Core/ResourceManagement/ISceneLoader.cs), [`NativeSceneLoader.cs`](../../Assets/MyLab/Core/ResourceManagement/NativeSceneLoader.cs), [`AddressableSceneLoader.cs`](../../Assets/MyLab/Core/ResourceManagement/AddressableSceneLoader.cs), [`LoadedScene.cs`](../../Assets/MyLab/Core/ResourceManagement/LoadedScene.cs).
