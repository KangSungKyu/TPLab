# ResourceManager 계약

2026-10-02. `MyLab.Core.ResourceManagement.ResourceManager`는 상속이 필요 없는 일반 C# 소유자다. Addressables 2.9.1·UniTask 2.5.11을 사용하며 게임별 key, CSV schema, UI를 참조하지 않는다.

## API와 소유권

| API | 계약 |
|---|---|
| `InitializeAsync(token)` | Addressables 초기화 완료를 공유한다. 필수 자산 로드 완료와는 다르다. 실패한 시도는 재요청할 수 있으나 Addressables 전역 설정을 자동 복구하지 않는다. |
| `LoadAssetAsync<T>(key, token)` | 초기화를 기다리고 같은 ordinal string key·정확히 같은 T의 진행 중 요청과 성공 결과를 공유한다. 다른 T는 명시적으로 실패한다. |
| `ShutdownAsync()` | 영구 종료하고 모든 진행 중 네이티브 작업과 handle 해제가 끝날 때까지 기다린다. 여러 호출자가 함께 기다릴 수 있다. |
| `Dispose()` | 즉시 신규 요청을 거부하고 대기자를 취소한다. 완료된 자산은 즉시 해제하고 진행 중 자산은 완료 시 해제한다. 반복 호출은 무해하다. |
| `IsInitialized` / `IsDisposed` | 초기화 완료·영구 종료 상태다. 종료 시 초기화 상태는 false다. |

- 메서드는 Unity 메인 스레드에서 호출한다. 다른 스레드는 상태 변경 전에 `InvalidOperationException`으로 거부한다.
- key는 null·빈 문자열·공백만 있는 문자열을 허용하지 않는다. 고유 asset address를 사용한다. label 다중 자산 조회는 이번 API에 포함하지 않는다.
- 소비자는 **빌린 자산**을 받는다. 자산에 `Destroy`·`Addressables.Release`를 호출하지 않는다. manager가 성공·실패·늦은 완료의 handle을 소유하고 해제한다.
- 호출자 token은 그 호출의 대기만 취소한다. 모든 호출자가 취소해도 이미 시작한 공유 로드는 소유자가 유지한다. 작업 시작 전 취소된 요청은 dispatch하지 않는다.
- 소유자 종료는 모든 대기자를 취소하고 늦은 결과를 공개하지 않는다. 종료 후 재사용하지 않고 새 scope를 생성한다.
- 로드 오류를 null 성공값으로 숨기지 않는다. 실패한 entry는 제거하며 재요청할 수 있다. 타입 충돌·초기화·provider 오류가 호출자에게 전달된다.
- 네이티브 작업의 강제 중단은 제공하지 않는다. provider가 완료하지 않으면 `ShutdownAsync`도 완료하지 않는다. 무조건 해제 완료로 처리하는 timeout은 넣지 않았다.

초기화 handle은 `InitializeAsync()`의 auto-release 참조와 별도로 `Acquire`하여 manager가 자기 참조만 해제한다. Addressables가 이미 진행 중인 초기화 handle을 그대로 반환하는 경우에도 다른 소유자의 참조를 해제하지 않는다. 자산 load에는 추가 Acquire를 하지 않는다.

load/release의 대응과 bundle 메모리의 실제 회수 시점은 [Unity Addressables 메모리 계약](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/memory-assets.html)을 따른다. handle 해제 완료가 bundle의 즉시 메모리 회수를 의미하지 않는다.

## SceneRoot에서 선택하여 사용

1. 기존 root에 `SceneOwnedRoot` 또는 `SingletonSceneRoot`를 선택한다. [SceneRoot 설정](SCENE_ROOT.md)을 따른다.
2. `ResourceManagerInstaller` 컴포넌트를 추가하고 root의 Installers 첫 부분에 등록한다.
3. 자산을 사용하는 프로젝트 installer를 그 뒤에 등록하고 ResourceManagerInstaller 참조를 Inspector 또는 script로 명시적으로 연결한다. `Install`에서 `source.Resources`를 소비자에게 주입한다.
4. 소비자의 `PrepareAsync`에서 필요한 자산 로드를 모두 await한다. `ResourceManagerInstaller`는 Addressables 초기화만 기다린다.
5. [SceneRootFlow](ASYNC_SCENE_LIFECYCLE.md)의 cover → root 준비 → 목적지 준비 → reveal 흐름을 사용한다. 실패·취소 시 가림막을 유지하고 씬 진행을 중단한다.
6. 정상 종료는 root의 `ShutdownAsync`를 await한 뒤 씬 unload·root Destroy를 진행한다. 자산 소비자부터 역순으로 해제된다.

```csharp
// 프로젝트의 SceneRootInstaller 내부. source는 명시적으로 연결한 컴포넌트다.
public override void Install(ISceneRoot root)
{
    _resources = source.Resources;
    view.Inject(_resources);
}

public override async UniTask PrepareAsync(ISceneRoot root, CancellationToken token)
{
    var prefab = await _resources.LoadAssetAsync<GameObject>("UI/Item", token);
    _pool = new PrefabPool(prefab, 20, root.RootObject.transform);
}

public override async UniTask ReleaseAsync(ISceneRoot root)
{
    _pool?.Dispose();
    await UniTask.NextFrame(); // PlayMode Destroy 완료 후 prefab handle 해제
}

public override void Uninstall(ISceneRoot root)
{
    view.Inject(null);
    _pool?.Dispose();
    _pool = null;
    _resources = null;
}
```

manager를 직접 생성하고 주입해도 된다. Singleton host 선택이 ResourceManager의 상속·전역 접근을 강제하지 않는다. 호출자의 `PrepareAndProceedAsync` 대기 취소는 root 소유자 종료를 자동으로 시작하지 않는다. 포기한 root를 정리할 책임은 프로젝트의 전환 소유자에게 있으며, `root.ShutdownAsync()`로 명시적으로 종료한다.

Unity `OnDestroy`에서는 await할 수 없으므로 `Uninstall`의 동기 fallback이 사용된다. 프리팹 clone을 비활성화·파괴 예약한 뒤 자산을 해제하며, 프레임 종료까지의 정리 순서는 정상적인 async shutdown 경로로 보장한다. 가림막 소유자는 종료될 root보다 오래 살아야 한다.

## Cashier 참조 판단과 범위

읽기 전용으로 확인한 Cashier ResourceManager·ResourcePoolTests의 공유 요청, 개별 대기 취소, 타입 충돌, 실패 후 재요청, 늦은 결과 해제 원칙을 개선 후 채택했다. MyLab에 별도 구현했으며 Cashier 코드·plugin·프로젝트 설정을 복사하거나 수정하지 않았다.

- 개선: Singleton 의존 제거, UniTask 공유 완료 소스, 영구 종료·drain 계약, SceneRoot installer 연결, null 성공값 대신 오류 전달.
- 이번 범위: Addressables 초기화와 공유 asset cache, 기존 PrefabPool을 통한 clone 생성·정리, 양쪽 root host의 준비·해제 연결.
- 보류: Datas label 다운로드, 명시적 catalog 갱신, atlas 전역 구독, 소비자별 lease·key별 Release, Addressables InstantiateInstance API, 별도 resource backend. 실제 사용 요구가 생기면 확장한다.
- 소비 프로젝트는 Addressables 설정·runtime catalog·asset address를 제공해야 한다. 이번 테스트는 임시 native catalog와 provider를 사용하며 MyLab의 Addressables 프로젝트 설정을 새로 만들지 않았다.

## 검증

실행 범위, Red/Green, 최종 회귀 결과와 한계는 [검증 기록](validation/resource-manager/README.md)을 따른다. 별도 소비 프로젝트 가져오기·Player/IL2CPP·원격 bundle 다운로드·최종 가림막 연출은 후속 통합 단계다.
