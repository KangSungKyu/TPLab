# Lifecycle API

씬의 서비스 생성·주입·비동기 준비·종료를 하나의 명시적 root 수명으로 묶는다. `SceneOwnedRoot`는 참조로 접근하고 `SingletonSceneRoot`는 같은 계약에 전역 접근을 추가한다. Singleton 선택과 씬 간 영속화는 독립적이다. 게임 진행·입력·씬 로드는 프로젝트 또는 [SceneManagement](SceneManagement.md)가 담당한다.

| 항목 | 값 |
|---|---|
| Namespace / Assembly | `TPLab.Core.Lifecycle` / `TPLab.Core` |
| 의존성 | Unity, UniTask 2.5.11. assembly 전체에는 Addressables 참조도 있으므로 [asmdef](../../Assets/TPLab/Core/TPLab.Core.asmdef)를 함께 확인한다. |
| SourceRevision | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0` (별도 배포 버전 미지정) |
| ImplementationStatus / ValidationStatus | `Implemented` / `Partial` |
| 계약 원문 | [Singleton](../SINGLETON.md), [root](../SCENE_ROOT.md), [비동기 준비·해제](../ASYNC_SCENE_LIFECYCLE.md) |
| AI 참조 | [Lifecycle](../ai/api/Lifecycle.md) |

이 문서는 현재 소스 선언·XML과 구현을 대조한 사용 안내다. 기존 명세의 과거 단계 표현보다 아래 현재 API 목록을 기준으로 사용한다.

## 소유권과 호출 순서

1. 실제 씬의 최상위 GameObject에 host 하나만 둔다. runtime 코드 구성은 비활성 객체에서 한다.
2. root 또는 자식의 installer를 의존 순서대로 지정하고 root를 활성화한다. 동기 `Install` 전체 성공 후 `IsReady=true`다.
3. `PrepareAsync`를 await한다. installer별 비동기 준비 성공 후 `IsPrepared=true`다. `Instance` 존재 또는 `IsInitialized`만으로 준비 완료를 판단하지 않는다.
4. 프로젝트가 화면·게임 진행을 허용한다. 코어는 다른 스크립트의 Awake/Start/Update를 정지시키지 않는다.
5. 소유자가 `ShutdownAsync`를 await한 뒤 root 파괴/씬 unload를 수행한다. 준비 중 작업 종료 대기 → 역순 `ReleaseAsync` → 역순 `Uninstall`이다.

installer는 자신이 생성한 서비스를 정리하고 대여한 서비스를 종료하지 않는다. host보다 installer를 먼저 별도로 파괴하지 않는다. 비활성화는 서비스 해제가 아니다. shutdown 시작 즉시 `IsPrepared=false`가 되고 root는 terminal 상태가 되어 신규 준비를 거부한다. singleton 등록은 객체 파괴까지 유지될 수 있으므로 종료된 root의 `Instance`로 서비스를 다시 사용하지 않는다.

## root와 installer

[ISceneRoot](../../Assets/TPLab/Core/Lifecycle/ISceneRoot.cs)의 정확한 선언이다.

```csharp
public interface ISceneRoot
{
    GameObject RootObject { get; }
    bool IsReady { get; }
    bool IsPrepared { get; }
    UniTask PrepareAsync(CancellationToken cancellationToken = default);
    UniTask ShutdownAsync();
}
```

[SceneOwnedRoot](../../Assets/TPLab/Core/Lifecycle/SceneOwnedRoot.cs)와 [SingletonSceneRoot](../../Assets/TPLab/Core/Lifecycle/SingletonSceneRoot.cs)는 각각 아래 public 멤버를 제공한다. 후자는 `MonoSingleton<SingletonSceneRoot>`의 public 멤버도 상속한다.

```csharp
public sealed class SceneOwnedRoot : MonoBehaviour, ISceneRoot
public sealed class SingletonSceneRoot : MonoSingleton<SingletonSceneRoot>, ISceneRoot

// 두 host에 각각 선언된 멤버
public GameObject RootObject { get; }
public bool PersistsAcrossScenes { get; }
public bool IsReady { get; }
public bool IsPrepared { get; }
public UniTask PrepareAsync(CancellationToken cancellationToken = default);
public UniTask ShutdownAsync();
public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false);
```

`Configure`는 activation/설치 전에 호출한다. `installers=null`은 빈 목록이며 배열은 복사된다. 각 항목은 null이 아니고 중복 없이 해당 root 또는 자식에 속해야 한다. Inspector의 `Installers`와 `Persist Across Scenes`도 같은 구성 책임을 갖는다. 영속화하면 root와 자식이 `DontDestroyOnLoad`로 함께 유지된다. 일반 씬 별 root의 영속화는 사용하는 씬 관리 계약과 함께 결정한다.

[SceneRootSetup](../../Assets/TPLab/Core/Lifecycle/SceneRootSetup.cs)은 기존 GameObject에 host를 추가하며 GameObject를 생성하지 않는다.

```csharp
public enum SceneRootMode { SceneOwned, Singleton }
public static class SceneRootSetup
public static ISceneRoot Attach(GameObject root, SceneRootMode mode,
    SceneRootInstaller[] installers = null, bool persistAcrossScenes = false);
```

`root=null`은 `ArgumentNullException`, 씬에 속하지 않거나 parent가 있으면 `ArgumentException`, 잘못된 enum은 `ArgumentOutOfRangeException`이다. 활성 runtime root 또는 기존 host가 있으면 `InvalidOperationException`이며 Attach는 이 입력 검사를 host 추가 전에 수행한다. `Configure`의 늦은 호출도 `InvalidOperationException`이다. installer null/중복/외부 소속은 `ArgumentException`이다. 설치 실패는 Unity lifecycle에서 기록되며 host를 준비 완료로 공개하지 않는다. Singleton 중복은 host 컴포넌트만 제거하며 그 installer를 실행하지 않는다.

[SceneRootInstaller](../../Assets/TPLab/Core/Lifecycle/SceneRootInstaller.cs)는 프로젝트 서비스 연결 경계다.

```csharp
public abstract class SceneRootInstaller : MonoBehaviour
public abstract void Install(ISceneRoot root);
public abstract void Uninstall(ISceneRoot root);
public virtual UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask ReleaseAsync(ISceneRoot root);
```

`Install`/`Uninstall`은 동기다. `async void Install`을 사용하지 않는다. 준비·해제의 기본 구현은 즉시 완료다. Install 중에는 root.IsReady가 아직 false다. Install 실패 시 실패한 installer를 포함해 시작된 installer를 역순 Uninstall하고 이후 installer는 실행하지 않는다. 부분 생성 상태에서도 Uninstall이 가능해야 한다. ReleaseAsync는 호출자 취소를 받지 않으며 같은 root.ShutdownAsync를 await하면 자신을 기다리므로 금지한다. 즉시 OnDestroy에는 async ReleaseAsync 완료를 기다릴 수 없어 Uninstall fallback을 사용한다.

## 공유 대기·취소·오류·스레드

모든 구성, Unity 객체 접근, 준비·종료 및 callback은 Unity 메인 스레드에서 수행한다. worker thread로 이 API를 옮기지 않는다. 이 조건은 모든 Lifecycle API에 런타임 thread 검사기가 있다는 보장이 아니다.

`PrepareAsync` 호출들은 한 시도를 공유하며 여러 대기자를 지원한다. 호출자 token은 그 대기만 취소하고 root의 준비를 취소하지 않는다. 실패 결과도 공유·보존되므로 새 시도로 재구성하려면 새 root를 만든다. 설치 전 준비는 `InvalidOperationException`, 종료 시작/완료 후 준비는 `ObjectDisposedException`, 호출자/준비 수명 취소는 `OperationCanceledException`이다. installer 오류는 준비 대기자에게 전달된다. 준비 실패를 직접 await한 소유자는 별도로 종료해야 한다. 아래 flow는 비취소 준비 실패에 대해 종료를 수행한다.

`ShutdownAsync`는 호출자 token이 없는 공유 완료이며 반복 호출로 정리를 중복 수행하지 않는다. 준비 소유 token을 취소하고 협조적 종료를 기다린다. token을 무시하는 installer는 종료를 지연시킬 수 있다. Release/Uninstall이 실패해도 나머지 정리를 시도하고 `AggregateException`으로 보고한다. 준비 오류는 원래 준비 대기자에게 남으며 shutdown 자체가 그 오류를 재보고하는 계약은 아니다. 설치되지 않은 host의 ShutdownAsync는 즉시 완료다. 강제 파괴 시 늦은 준비가 IsPrepared를 다시 올리지는 않지만, installer 자신의 늦은 작업 보호도 필요하다.

## MonoSingleton 확장

[MonoSingleton](../../Assets/TPLab/Core/Lifecycle/MonoSingleton.cs)은 명시적으로 생성된 컴포넌트 하나를 등록한다. Instance는 검색·자동 생성하지 않는다.

```csharp
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
public static T Instance { get; }
public bool IsInitialized { get; }
```

Play 밖, 종료 중, 소유자 부재 시 Instance는 null이다. 비활성 소유자는 파괴까지 등록된다. 초기화 성공 뒤에만 Instance가 공개된다. 중복 컴포넌트는 제거하지만 GameObject와 다른 컴포넌트는 보존한다. 초기화 실패는 로그를 남기고 부분 상태 cleanup 후 해당 컴포넌트를 제거한다. 종료 hook 예외도 기록하며 등록을 남기지 않는다.

상속 구현용 **protected 확장점**은 아래와 같다. public 호출 API가 아니다.

```csharp
protected virtual bool PersistAcrossScenes { get; } // 기본 false
protected virtual void OnSingletonInitialize();
protected virtual void OnSingletonShutdown();
```

초기화는 동기로 끝내고 shutdown은 부분 초기화도 정리한다. Unity의 Awake/OnEnable/OnDestroy/OnApplicationQuit을 숨기지 말고 hook을 override한다. 영속 singleton은 최상위 GameObject여야 한다. Domain/Scene Reload 없이 다시 Play에 들어가는 경우의 등록 복구도 제공하지만 모든 abrupt stop/늦은 작업 조합의 실행 증거를 뜻하지는 않는다.

## SceneRootFlow

[SceneRootFlow](../../Assets/TPLab/Core/Lifecycle/SceneRootFlow.cs)는 root를 대여해 cover → 준비 또는 종료 → 프로젝트 proceed → reveal을 await한다. UI·입력 정책·씬 로드를 제공하지 않는다.

```csharp
public sealed class SceneRootFlow
public bool IsTransitioning { get; private set; }
public SceneRootFlow(ISceneRoot root,
    Func<CancellationToken, UniTask> coverAsync = null,
    Func<CancellationToken, UniTask> revealAsync = null,
    Action<Exception> onFailure = null);
public UniTask PrepareAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
    CancellationToken cancellationToken = default);
public UniTask ReleaseAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
    CancellationToken cancellationToken = default);
```

root/proceedAsync null은 `ArgumentNullException`, 같은 flow 실행 중 재요청은 `InvalidOperationException`이다. flow 하나를 전환 소유자가 보관한다. 서로 다른 flow로 같은 화면을 동시에 제어하지 않는다. cover/reveal과 실패 표시 객체는 해제 대상보다 오래 살아 있어야 한다.

비취소 준비 실패는 root shutdown을 시도하고 두 실패가 겹치면 합쳐 전달한다. 호출자 대기 취소는 공유 root 준비를 계속 두며 소유자가 shutdown을 결정한다. Release 경로는 shutdown을 시작하면 취소가 와도 cleanup을 await하고 다음 진행은 중단한다. proceed 실패·취소 시 reveal을 건너뛰며 reveal 중 실패·취소는 `CancellationToken.None`으로 cover를 다시 await한다. 실행 오류/취소는 onFailure로 통지하고 호출자에게도 전달한다. 실행 전 입력·재진입·이미 취소된 token 거부에는 callback이 없다. cover 복구/오류 callback 실패는 원래 오류와 `AggregateException`으로 전달된다. 표시 callback 자체 실패 시 시각 보호까지 보장하지 않는다.

## 사용 발췌

아래는 **설명용 발췌이며 이 문서 작업에서 컴파일·실행하지 않았다**. `MyProjectInstaller`는 프로젝트가 구현하는 SceneRootInstaller다. Unity 메인 스레드 async 메서드에서 실행하며 실제 서비스와 씬 설정은 프로젝트가 제공한다.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;

// async UniTask 메서드 본문 발췌; callerToken은 프로젝트 제공 CancellationToken.
var rootObject = new GameObject("SessionRoot");
rootObject.SetActive(false);
var installer = rootObject.AddComponent<MyProjectInstaller>();
ISceneRoot root = SceneRootSetup.Attach(rootObject, SceneRootMode.SceneOwned,
    new SceneRootInstaller[] { installer });
rootObject.SetActive(true);
try
{
    await root.PrepareAsync(callerToken);
    if (!root.IsPrepared)
    {
        throw new InvalidOperationException("Root is no longer prepared.");
    }
    // 프로젝트가 root 수명 안에서 서비스를 사용한다.
}
catch (OperationCanceledException)
{
    // 이 대기만 취소됐다. 아래 소유자 종료가 실제 준비를 중단한다.
    throw;
}
finally
{
    try
    {
        await root.ShutdownAsync();
    }
    finally
    {
        UnityEngine.Object.Destroy(rootObject);
    }
}
```

발췌는 cleanup 시도를 보여주며 준비 실패와 cleanup 실패가 함께 발생하면 원래 오류까지 합쳐 보고하는 코드는 프로젝트가 추가해야 한다. 준비와 게임 진행을 분리하는 다음 발췌의 표시/진행 delegate는 프로젝트가 제공하며 종료 시점에도 같은 flow의 중복 실행을 피한다.

```csharp
var flow = new SceneRootFlow(root, ShowCoverAsync, HideCoverAsync, ReportFailure);
await flow.PrepareAndProceedAsync(EnterContentAsync, callerToken);
// 소유자의 후속 전환: root는 terminal이 되므로 새 content의 root는 별도로 소유한다.
await flow.ReleaseAndProceedAsync(EnterNextContentAsync, callerToken);
```

## 검증과 호환성 한계

[최종 p4 증거](../validation/input-system/p4/README.md)는 source `9305b5d...`를 포함한 전체 EditMode 258/258·PlayMode 217/217(실패0·skip0), reload 8/8, Additive/Single sample 및 별도 Core 소비 프로젝트 Windows Mono build/Player를 기록한다. 환경은 Unity 6000.3.18f1, UniTask 2.5.11, Addressables 2.9.1이며 문서 작성 중 새 실행은 0건이다. 전체 테스트 수는 Lifecycle 전용 테스트 수가 아니다.

다른 Unity 버전·IL2CPP·다른 플랫폼 및 임의 소비 프로젝트의 서비스 shutdown은 미검증이다. 자동 smoke는 최종 시각·사용성 검증을 대신하지 않는다. 현재 기본 false인 persistence, 동기 Install과 별도 Prepare, 호출자 대기 취소/소유자 종료 분리는 통합 시 유지해야 할 계약이다. 이벤트·JSON/CSV·ID 설정은 이 모듈에 없다.
