# Lifecycle: AI API reference

| Field | Value |
|---|---|
| Module / Namespace / Assembly | `Lifecycle` / `TPLab.Core.Lifecycle` / `TPLab.Core` |
| SourceRevision | `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`; 배포 버전 미지정 |
| SourcePath | [Lifecycle 소스](../../../Assets/TPLab/Core/Lifecycle), [assembly](../../../Assets/TPLab/Core/TPLab.Core.asmdef) |
| HumanContract | [사람용 API](../../api/Lifecycle.md), [root](../../SCENE_ROOT.md), [비동기 계약](../../ASYNC_SCENE_LIFECYCLE.md), [singleton](../../SINGLETON.md) |
| ImplementationStatus / ValidationStatus | `Implemented` / `Partial` |
| Evidence | [p4](../../validation/input-system/p4/README.md): 전체 Edit258/Play217 실패0·skip0, reload8/8, Windows Mono sample/Core consumer build·Player. 모듈 전용 건수가 아니다. 이 문서 작업 새 실행0. |

## Symbol / Signature / Constraints

아래는 public 선언 참조다. body는 생략했으며 host 공통 멤버는 두 concrete 타입 각각에 선언되어 있다. private/internal 구현과 테스트 API는 사용 대상이 아니다.

```csharp
public interface ISceneRoot
{
    GameObject RootObject { get; }
    bool IsReady { get; }
    bool IsPrepared { get; }
    UniTask PrepareAsync(CancellationToken cancellationToken = default);
    UniTask ShutdownAsync();
}
public sealed class SceneOwnedRoot : MonoBehaviour, ISceneRoot
public sealed class SingletonSceneRoot : MonoSingleton<SingletonSceneRoot>, ISceneRoot
// 각각의 host public 멤버
public GameObject RootObject { get; }
public bool PersistsAcrossScenes { get; }
public bool IsReady { get; }
public bool IsPrepared { get; }
public UniTask PrepareAsync(CancellationToken cancellationToken = default);
public UniTask ShutdownAsync();
public void Configure(SceneRootInstaller[] installers, bool persistAcrossScenes = false);

public enum SceneRootMode { SceneOwned, Singleton }
public static class SceneRootSetup
public static ISceneRoot Attach(GameObject root, SceneRootMode mode,
    SceneRootInstaller[] installers = null, bool persistAcrossScenes = false);

public abstract class SceneRootInstaller : MonoBehaviour
public abstract void Install(ISceneRoot root);
public abstract void Uninstall(ISceneRoot root);
public virtual UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken);
public virtual UniTask ReleaseAsync(ISceneRoot root);

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
public static T Instance { get; }
public bool IsInitialized { get; }

public sealed class SceneRootFlow
public bool IsTransitioning { get; private set; }
public SceneRootFlow(ISceneRoot root, Func<CancellationToken, UniTask> coverAsync = null,
    Func<CancellationToken, UniTask> revealAsync = null, Action<Exception> onFailure = null);
public UniTask PrepareAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
    CancellationToken cancellationToken = default);
public UniTask ReleaseAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
    CancellationToken cancellationToken = default);
```

## Inputs / Outputs / Errors

- Attach: scene 최상위 GameObject, host 없음, Play 중 inactive. root null→ArgumentNullException; scene/parent 오류→ArgumentException; enum 오류→ArgumentOutOfRangeException; active/기존 host→InvalidOperationException. mutation 전 검사.
- Configure: activation/설치 전. installers null→empty, 배열 복사, nonnull·unique·root/child 컴포넌트만. 목록 오류→ArgumentException; 늦은 구성→InvalidOperationException. persistence 기본 false.
- IsReady=동기 Install 전체 성공; IsPrepared=비동기 준비 전체 성공 및 아직 종료하지 않음. RootObject는 host GameObject이며 획득한 신규 자원이 아니다.
- PrepareAsync: 설치 전→InvalidOperationException; 종료 시작/완료 뒤→ObjectDisposedException; 취소→OperationCanceledException; installer 오류 그대로 전달. 준비 실패 결과 캐시.
- ShutdownAsync: 미설치→즉시 완료, 종료 오류→AggregateException. 준비 실패는 준비 awaiter에게 남음. lifecycle 즉시 fallback 오류는 로그에 기록.
- MonoSingleton Instance: Play 밖/종료/소유자 없음→null. 검색·생성 없음. 성공 초기화만 공개, disable 후에도 등록 유지. 중복은 컴포넌트만 제거하며 GameObject 보존. 초기화 오류→로그, 부분 cleanup, 컴포넌트 제거.
- Flow root/proceed null→ArgumentNullException; overlap→InvalidOperationException. callback/준비/진행 오류 전달. recovery/cleanup/실패 callback 오류 동반 시 AggregateException.

## Ownership / Lifecycle / Threading

프로젝트가 root GameObject와 installer 생성 서비스의 소유자다. host가 installer 수명을 순서대로 실행하고 flow는 root/presentation을 대여한다. singleton 전역 접근은 소유권 이전이 아니다. 전 API와 Unity callback은 main thread에서 사용하며 Lifecycle 전 API가 thread guard로 검사된다고 가정하지 않는다.

## Concurrency / Cancellation / FailureCleanup

- Prepare는 shared attempt/completion, 여러 awaiter 가능. caller token은 해당 대기만 취소. Shutdown은 owner preparation token 취소→협조적 종료 대기→reverse Release→reverse Uninstall, shared uncancelled completion.
- shutdown 시작 시 IsPrepared=false, 신규 prepare 금지, terminal. 중복 shutdown 무해. root는 새 서비스 시도에 재사용하지 않음. token을 무시하는 Prepare hook은 cleanup을 지연시킬 수 있음.
- Install 실패는 실패한 항목 포함 begun installers 역순 Uninstall; release/uninstall 실패도 남은 cleanup 시도. Destroy fallback은 async Release를 await하지 않음. installer 자체 late-work guard 필요.
- Flow 하나를 owner가 보관, overlap 거부. Prepare의 비OCE 실패는 shutdown; caller 취소는 root 공유준비 지속. Release 경로 cleanup 시작 뒤 caller 취소는 cleanup 완료 후 proceed 차단.
- Flow 진행 오류 시 reveal 생략; reveal 오류/취소는 None token으로 cover 복구; onFailure 통지 뒤 재throw. 실행 전 validation/overlap/already-cancelled에는 callback 없음.

## Configuration / ExtensionPoints

Inspector: Installers 순서, Persist Across Scenes. ID/GUID/JSON/CSV/event 없음. installer Install/Uninstall 및 async hooks는 프로젝트 구현. MonoSingleton의 아래 **protected** hooks만 override하며 public로 노출하지 않는다.

```csharp
protected virtual bool PersistAcrossScenes { get; } // false
protected virtual void OnSingletonInitialize();
protected virtual void OnSingletonShutdown();
```

Unity lifecycle 메서드를 숨기지 않음. flow cover/reveal=`Func<CancellationToken, UniTask>`, failure=`Action<Exception>`; optional null은 생략. presentation은 해제 root보다 긴 수명.

## RequiredSequence

1. inactive scene root + 유일 host + root/child installers 구성.
2. activation → 동기 Install 성공/IsReady 확인.
3. PrepareAsync await → IsPrepared 확인 → 프로젝트 진행.
4. ShutdownAsync await → GameObject Destroy 또는 scene unload.

## ForbiddenUsage

async void Install, active root Configure, installer 선파괴, Release hook에서 자기 Shutdown await, 여러 flow가 같은 presentation 동시 제어, Instance 존재만으로 준비 완료 판정, shutdown 후 서비스 재사용, worker thread Unity API 접근을 금지한다. disable을 cleanup으로 취급하지 않는다.

## Example / Compatibility / Limitations

[사람용 설명 발췌](../../api/Lifecycle.md#사용-발췌)는 실제 선언과 대조했지만 문서 작업 중 compile/run하지 않았다. 정상·caller cancel·owner shutdown·Destroy 순서를 포함한다. 기존 동기 installer는 default async hooks로 호환된다. singleton과 persistence는 별도 선택이다.

확인 환경 Unity6000.3.18f1/UniTask2.5.11/Addressables2.9.1, Windows Mono. 다른 Unity/IL2CPP/플랫폼, 임의 installer의 취소·종료, 모든 abrupt stop 조합은 미검증. Core asmdef 의존성은 [소스](../../../Assets/TPLab/Core/TPLab.Core.asmdef)가 소유한다. 로딩 진행률·팁·버튼 대기는 이 모듈 기능이 아니다.
