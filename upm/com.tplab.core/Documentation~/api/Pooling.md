PackageVersion: 0.0.1. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.

# 풀링 API

`TPLab.Core.Pooling` 네임스페이스는 `TPLab.Core` 어셈블리에 있습니다. 일반 참조 객체용 제한 풀 `ObjectPool<T>`와 Unity `GameObject` 프리팹 복제본용 메인 스레드 풀 `PrefabPool`을 제공합니다. 풀은 생성한 객체를 소유하고, 대여 중인 객체도 용량과 종료 정리에 포함합니다. 소비자는 객체를 빌려 쓰고 정확히 한 번 반환합니다. 풀 소유자가 용량과 초기화·재설정·정리 콜백을 선택합니다.

**SourceRevision:** `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`
**ImplementationStatus:** Implemented
**ValidationStatus:** Partial — Unity `6000.3.18f1` / Windows Mono에서 확인했습니다. 기록된 테스트 범위만 근거로 삼으며 모든 Unity 버전, 백엔드, 플랫폼의 지원을 뜻하지 않습니다.

## `ObjectPool<T>`

```csharp
public sealed class ObjectPool<T> : IDisposable where T : class
public ObjectPool(Func<T> createInstance, int capacity, Action<T> onRent = null,
    Action<T> onReturn = null, Action<T> onDestroy = null)
public bool TryRent(out T instance)
public void Return(T instance)
public void Dispose()
public int Capacity { get; }
public int CountOwned { get; }
public int CountRented { get; }
public int CountInactive { get; }
public bool IsDisposed { get; }
```

`Capacity`는 대여 중인 객체와 재사용 대기 객체를 모두 포함하는 최대 소유 수입니다. 팩토리는 null이 아닌 새 객체를 반환해야 하며, 그 객체는 이 풀만 소유해야 합니다. 소유 판정은 값 비교나 변경 가능한 해시 코드가 아니라 객체 참조로 합니다. `TryRent`는 모든 용량이 대여 중일 때만 `false`를 반환하고 `instance`는 null입니다. 그 외에는 새 객체를 만들거나 대기 객체를 꺼내 `onRent`를 호출한 뒤 대여합니다.

`Return`은 이 풀에서 현재 대여 중인 객체만 받습니다. `onReturn`을 호출한 다음 재사용 대기열에 보관합니다. 대여 준비나 반환 재설정이 실패하면 해당 객체만 풀 소유에서 제외하고 `onDestroy`를 시도한 뒤 원래 오류를 다시 던집니다. 정리 콜백도 실패하면 두 오류를 `AggregateException`으로 보고합니다. 팩토리와 콜백 안에서 같은 풀을 재진입해 변경하면 안 됩니다. 호출은 단일 스레드에서 순서대로 수행해야 하며 내부 동기화는 없습니다.

`Dispose`는 풀을 영구 종료하고 모든 대여·대기 객체에 `onDestroy`를 호출합니다. 정리를 시작하기 전에 소유 수를 0으로 만들고, 한 콜백이 실패해도 나머지 정리를 모두 시도한 뒤 실패를 집계합니다. `onReturn`을 호출하거나 `IDisposable`을 자동 호출하지 않습니다. 외부 자원 정리는 `onDestroy`에 직접 지정해야 합니다. 반복 `Dispose`는 무시됩니다. 종료 후 대여·반환은 `ObjectDisposedException`을 던집니다.

아래는 설명용 발췌이며 **실행하지 않았습니다**.

```csharp
var pool = new ObjectPool<Worker>(
    () => new Worker(), capacity: 8,
    onRent: worker => worker.Prepare(),
    onReturn: worker => worker.Reset(),
    onDestroy: worker => worker.Dispose());

if (pool.TryRent(out var worker))
{
    try
    {
        worker.Run();
    }
    finally
    {
        pool.Return(worker);
    }
}

pool.Dispose(); // 대여 중인 객체도 정리합니다.
```

`Worker`와 해당 메서드는 소비 프로젝트가 제공하는 예시 타입입니다. `ObjectPool<T>`로 `IDisposable` 객체를 관리하면 `onDestroy`에서 직접 Dispose 하세요.

## `PrefabPool`

```csharp
public sealed class PrefabPool : IDisposable
public PrefabPool(GameObject prefab, int capacity, Transform parent = null,
    Action<GameObject> onRent = null, Action<GameObject> onReturn = null)
public bool TryRent(out GameObject instance)
public void Return(GameObject instance)
public void Dispose()
public int Capacity { get; }
public int CountOwned { get; }
public int CountRented { get; }
public int CountInactive { get; }
public bool IsDisposed { get; }
```

생성·사용·정리는 Unity 메인 스레드에서 해야 합니다. 호출자는 원본 프리팹과 선택한 부모 `Transform`을 계속 소유합니다. 새 복제본을 만들 동안 둘 다 살아 있어야 합니다. 생성자는 비활성 저장 루트를 만듭니다. 대여 시 복제본을 만들거나 재사용하고, `parent` 아래로 옮겨 원본 프리팹의 로컬 위치·회전·크기를 복원한 뒤 `onRent`를 호출하고 활성화합니다. 반환 시 먼저 비활성화하고 `onReturn`을 호출한 뒤 저장 루트로 옮기고 변환을 복원합니다. 부모가 비활성이면 복제본도 계층에서 활성 상태가 되지 않습니다.

용량에는 대여 중인 복제본도 포함됩니다. 전부 대여 중일 때만 `TryRent`가 null과 `false`를 반환합니다. `Return`은 null·파괴된 객체, 다른 풀의 객체, 이미 반환한 객체를 거부합니다. 콜백 실패는 해당 복제본을 폐기하고 오류를 전파합니다. 콜백 안에서 풀을 재진입해 변경하지 마세요. 별도 destroy 콜백은 없습니다. `Dispose`는 대여·대기 복제본과 저장 루트를 파괴하며 원본 프리팹과 외부 부모는 보존합니다. Play Mode에서 Unity의 파괴 처리는 프레임 끝에 완료됩니다. 대여한 복제본을 직접 파괴하지 말고 반환하거나 풀 종료에 맡기세요.

아래는 설명용 발췌이며 **실행하지 않았습니다**.

```csharp
var pool = new PrefabPool(prefab, capacity: 8, parent: parent);
if (pool.TryRent(out var clone))
{
    try
    {
        // 대여한 복제본을 사용합니다.
    }
    finally
    {
        pool.Return(clone);
    }
}
pool.Dispose();
```

`prefab`, `parent`, 복제본 사용 방식은 소비 프로젝트가 제공합니다. 소비자가 복제본을 반환하거나 소유권을 넘긴 뒤에 풀을 종료하세요.

## 검증 근거와 한계

- 현행 source revision 전체 회귀: [EditMode 258/258](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/full-PlayMode.json). 전체 실행에는 `ObjectPool<T>`와 `PrefabPool` 검사가 포함됩니다. 실행 환경·범위는 [P4 검증 기록](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/README.md)을 참조하세요.
- 현행 Input 포함 소비 프로젝트는 별도 Windows Mono Editor/Player에서 성공했습니다. Player의 pool 재사용 관찰은 **generic `ObjectPool<T>`** 경로입니다. [`최종 소비 결과`](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json).
- 이전 focused `object-pool/green-*.json` 자료는 과거 증거이며 현행 `PrefabPool` 결과로 사용하지 않습니다.
- 이 근거는 Unity `6000.3.18f1` / Windows Mono 범위이며, 다른 버전·backend·플랫폼의 호환성을 입증하지 않습니다.

구현: [`ObjectPool.cs`](../../Runtime/Pooling/ObjectPool.cs), [`PrefabPool.cs`](../../Runtime/Pooling/PrefabPool.cs).
