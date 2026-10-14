# 풀링 API

## Editor 계측 한계에 의한 정정 (2026-10-13)

[IMP-14 검증](../validation/imp-14/README.md)에서 기존 GC 테스트3개를 양성·음성 대조 내장 ProfilerRecorder GC.Alloc 현재 스레드 이벤트 측정으로 교체했다. warmed pool 반복 각1000회에서 workload0 events를 확인했다. 기존 Editor counter0B 기록은 양성 대조가 감지되지 않는 계측의 역사적 관측이며 무할당 입증으로 사용하지 않는다. 이벤트 수를 byte 수로 해석하지 않는다. pool runtime·공개 API·D-01 지연 회수·D-02 TryReturn 로직 결론은 유지한다. Player/IL2CPP는 미검증이다.

`TPLab.Core.Pooling` 네임스페이스는 `TPLab.Core` 어셈블리에 있습니다. 일반 참조 객체용 제한 풀 `ObjectPool<T>`와 Unity `GameObject` 프리팹 복제본용 메인 스레드 풀 `PrefabPool`을 제공합니다. 풀은 생성한 객체를 소유하고, 대여 중인 객체도 용량과 종료 정리에 포함합니다. 소비자는 객체를 빌려 쓰고 정확히 한 번 반환합니다. 풀 소유자가 용량과 초기화·재설정·정리 콜백을 선택합니다.

**SourceRevision:** `2c8c0768c53878dcc9c2d4ed090582092e9dc5e0`
**ImplementationStatus:** Implemented
**ValidationStatus:** Verified for IMP-02 in Windows Mono Editor — Red20 signature 부재 → Green62/62·전체Edit348/348·Play316/316, fail/skip0; 2026-10-13 사용자 수락 완료(PR #8). [현재 증거](../validation/imp-02/README.md). 다음은 IMP-01의 역사적 결과입니다: 자동 검증 완료·2026-10-12 사용자 수락 완료(PR #7). 이번 소비 프로젝트/Player는 미실행입니다. 이전 실행은 역사적 증거입니다. Unity `6000.3.18f1` / Windows Mono에서 확인했습니다. 기록된 테스트 범위만 근거로 삼으며 모든 Unity 버전, 백엔드, 플랫폼의 지원을 뜻하지 않습니다.

## `ObjectPool<T>`

```csharp
public sealed class ObjectPool<T> : IDisposable where T : class
public ObjectPool(Func<T> createInstance, int capacity, Action<T> onRent = null,
    Action<T> onReturn = null, Action<T> onDestroy = null, Func<T, bool> isAlive = null)
public bool TryRent(out T instance)
public void Return(T instance)
public bool TryReturn(T instance)
public void Dispose()
public int Capacity { get; }
public int CountOwned { get; }
public int CountRented { get; }
public int CountInactive { get; }
public bool IsDisposed { get; }
```

`Capacity`는 대여 중인 객체와 재사용 대기 객체를 모두 포함하는 최대 소유 수입니다. 팩토리는 null이 아닌 새 객체를 반환해야 하며, 그 객체는 이 풀만 소유해야 합니다. 소유 판정은 값 비교나 변경 가능한 해시 코드가 아니라 객체 참조로 합니다. `TryRent`는 모든 용량이 대여 중일 때만 `false`를 반환하고 `instance`는 null입니다. 그 외에는 새 객체를 만들거나 대기 객체를 꺼내 `onRent`를 호출한 뒤 대여합니다.

`Return`은 이 풀에서 현재 대여 중인 객체만 받습니다. `onReturn`을 호출한 다음 재사용 대기열에 보관합니다. 대여 준비나 반환 재설정이 실패하면 해당 객체만 풀 소유에서 제외하고 `onDestroy`를 시도한 뒤 원래 오류를 다시 던집니다. 정리 콜백도 실패하면 두 오류를 `AggregateException`으로 보고합니다. 팩토리와 콜백 안에서 같은 풀을 재진입해 변경하면 안 됩니다. 호출은 단일 스레드에서 순서대로 수행해야 하며 내부 동기화는 없습니다.

`Dispose`는 풀을 영구 종료하고 모든 대여·대기 객체에 `onDestroy`를 호출합니다. 정리를 시작하기 전에 소유 수를 0으로 만들고, 한 콜백이 실패해도 나머지 정리를 모두 시도한 뒤 실패를 집계합니다. `onReturn`을 호출하거나 `IDisposable`을 자동 호출하지 않습니다. 외부 자원 정리는 `onDestroy`에 직접 지정해야 합니다. 반복 `Dispose`는 무시됩니다. 종료 후 TryRent와 Return은 `ObjectDisposedException`을 던집니다. TryReturn은 false를 반환합니다.

`TryReturn(instance)`은 이미 종료된 풀이면 입력 검사를 생략하고 false를 반환합니다. null·foreign 객체도 종료 후에는 false가 우선이며 콜백·정리·상태 변경을 수행하지 않습니다. Dispose가 소유 객체 정리를 맡고 TryReturn은 실패한 Dispose cleanup을 재시도하거나 foreign 객체를 정리하지 않습니다. Dispose cleanup 콜백 중 호출도 false입니다.

열린 풀에서는 Return을 그대로 호출하여 성공한 뒤 true를 반환합니다. null·foreign·duplicate·재진입·콜백 실패와 기존 실패 정리 예외는 전파되며 false로 숨기지 않습니다. 활성 콜백에서 TryReturn을 통한 변경 재진입은 기존 InvalidOperationException입니다. 이 동기 API도 단일 스레드 순차 호출 전제이며 동시 종료 동기화는 없습니다. wrapper는 정상 경로에서 할당을 추가하지 않고 워밍업·콜백 할당 조건은 Return과 같습니다. 기존 API 시그니처는 유지되며 TryReturn만 추가됩니다.

아래는 종료 경합이 있는 소비자 정리의 설명용 발췌이며 **실행하지 않았습니다**.

```csharp
bool returned = pool.TryReturn(worker);
// false: 이미 종료됨. true: Return 성공. 열린 풀의 잘못된 호출은 예외.
```

선택 `isAlive`가 null이면 기존 판정 없는 정책을 유지합니다. 설정하면 대기 객체를 Peek에서 확인하고 죽은 항목을 건너뛰어 살아 있는 객체의 LIFO 순서를 유지합니다. 재사용 대상이 없고 정원이 찼을 때만 대여 목록 전체를 판정하고 죽은 슬롯을 회수합니다. CountOwned/CountRented/CountInactive getter는 판정·변경·할당을 하지 않으므로 외부 사망은 관련 TryRent 확인 전까지 추적 수에 남을 수 있습니다. 설정한 정책의 scratch는 생성자에서 준비하며 워밍업 후 재사용·살아 있는 정원 소진 확인은 풀 내부 할당을 추가하지 않습니다. factory/콜백의 할당과 판정 CPU 비용은 별도입니다.

predicate는 변경 재진입 가드 안에서 실행됩니다. 대기 predicate 예외는 현재 Peek 후보를 보존하고, 정원 순회 predicate 예외는 순회 중 소유권을 그대로 유지합니다. 이미 제거한 죽은 대기 항목은 복구하지 않습니다. 죽은 항목마다 onDestroy를 한 번 시도하며, 정리 실패가 있어도 검출된 모든 죽은 항목을 정리 시도한 뒤 AggregateException으로 모읍니다. 실패한 cleanup의 슬롯도 회수되며 Dispose에서 다시 시도하지 않습니다. 대기 cleanup 오류와 후속 predicate 오류는 함께 집계합니다. factory의 null/중복 검증 후 소유 등록한 새 결과가 죽었거나 판정이 실패하면 기존 실패 정리로 폐기하며 onRent를 호출하지 않습니다.

마지막 선택 매개변수 추가는 기존 소스 호출과 호환되지만 CLR 생성자 시그니처가 바뀌므로 미리 컴파일한 소비 어셈블리는 재빌드해야 합니다. Return/Dispose 계약은 그대로 유지됩니다.

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
public bool TryReturn(GameObject instance)
public void Dispose()
public int Capacity { get; }
public int CountOwned { get; }
public int CountRented { get; }
public int CountInactive { get; }
public bool IsDisposed { get; }
```

생성·사용·정리는 Unity 메인 스레드에서 해야 합니다. 호출자는 원본 프리팹과 선택한 부모 `Transform`을 계속 소유합니다. 새 복제본을 만들 동안 둘 다 살아 있어야 합니다. 생성자는 비활성 저장 루트를 만듭니다. 대여 시 복제본을 만들거나 재사용하고, `parent` 아래로 옮겨 원본 프리팹의 로컬 위치·회전·크기를 복원한 뒤 `onRent`를 호출하고 활성화합니다. 반환 시 먼저 비활성화하고 `onReturn`을 호출한 뒤 저장 루트로 옮기고 변환을 복원합니다. 부모가 비활성이면 복제본도 계층에서 활성 상태가 되지 않습니다.

용량에는 대여 중인 복제본도 포함됩니다. 전부 대여 중일 때만 `TryRent`가 null과 `false`를 반환합니다. `Return`은 null·파괴된 객체, 다른 풀의 객체, 이미 반환한 객체를 거부합니다. 콜백 실패는 해당 복제본을 폐기하고 오류를 전파합니다. 콜백 안에서 풀을 재진입해 변경하지 마세요. 별도 destroy 콜백은 없습니다. `Dispose`는 대여·대기 복제본과 저장 루트를 파괴하며 원본 프리팹과 외부 부모는 보존합니다. Play Mode에서 Unity의 파괴 처리는 프레임 끝에 완료됩니다. 정상적으로 반환하거나 풀 종료에 맡기세요. 외부 파괴된 복제본은 Unity fake-null 정책으로 죽은 대기 항목을 건너뛰고, 재사용 대상 없이 정원이 찼을 때 대여 슬롯을 회수합니다. Destroy 완료 전에는 살아 있다고 판정할 수 있습니다. Count getter는 지연 추적 수입니다. 열린 풀의 Return(destroyed)는 계속 ArgumentNullException입니다.

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

PrefabPool.TryReturn(clone)도 종료되었으면 null/destroyed/foreign/context 검사 전에 false를 반환합니다. Dispose cleanup의 OnDisable/OnDestroy에서도 false이며 콜백·파괴·상태 변경을 수행하지 않습니다. 열린 풀은 Return과 같은 null/destroyed·foreign·duplicate·context·callback 예외와 실패 정리를 유지합니다. 활성 콜백 재진입을 false로 숨기지 않습니다. Unity 메인 스레드에서 동기·순차 호출해야 하며 새 thread guard는 없습니다. Dispose가 소유 clone 정리를 담당하고 소비자 외부 구독은 소비자가 정리합니다. wrapper의 정상 경로는 할당하지 않습니다.

아래 발췌는 설명용이며 **실행하지 않았습니다**.

```csharp
bool returned = pool.TryReturn(clone);
// 종료 후 false; 열린 풀은 정상 반환에만 true.
```

`prefab`, `parent`, 복제본 사용 방식은 소비 프로젝트가 제공합니다. 소비자가 복제본을 반환하거나 소유권을 넘긴 뒤에 풀을 종료하세요.

## 검증 근거와 한계

- IMP-02: 실제 Red20(public signature 부재) → Green62/62, 전체 Edit348/348·Play316/316, fail/skip0. 당시 WarmedTryReturnReuseAllocatesZero는 1000회 counter0B를 보고했습니다(위 Editor 계측 정정 적용). 현재 유효 대조 기반 측정은 workload0 events입니다. [현재 검증 기록](../validation/imp-02/README.md)의 실제 결과·범위·미실행을 확인하세요. 2026-10-13 사용자 수락 완료(PR #8)이며 아래 IMP-01·이전 실행은 이번 Green 증거가 아닙니다.

- IMP-01: 실제 Red 18개 중 예상 실패1 → Green Prefab18/18·기존generic24/24·보충15/15, 초기 전체 Edit334/334·Play307/307 이후 추가 경계3개를 검증했고, 최종 전체 Edit335/335·Play309/309, fail/skip0. 워밍업 뒤 재사용/정원 점검/죽은 슬롯 회복 두 GC 테스트는 당시 각1,000회 counter0B를 보고했습니다(위 Editor 계측 정정 적용). 현재 유효 대조 기반 측정은 workload0 events입니다. [IMP-01 검증 기록](../validation/imp-01/README.md)의 범위·raw 결과·한계를 확인하세요. 살아 있는 대여·isAlive 생략은 baseline/현재 모두 PASS, dead inactive는 같은 호출 교체를 baseline FAIL/현재 PASS로 사후 비교했습니다. 공개 Prune은 보류(소비 프로젝트의 즉시 정리 필요에 따라 별도 결정)입니다. 2026-10-12 사용자 수락을 완료했으며 새 소비 프로젝트/Player 검증은 별도입니다. 아래 과거 결과는 현재 변경의 Green 증거가 아닙니다.

- 이전 source revision `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`의 역사적 전체 회귀: [EditMode 258/258](../validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](../validation/input-system/p4/full-PlayMode.json). 전체 실행에는 `ObjectPool<T>`와 `PrefabPool` 검사가 포함됩니다. 실행 환경·범위는 [P4 검증 기록](../validation/input-system/p4/README.md)을 참조하세요.
- 이전 Input 포함 소비 프로젝트는 별도 Windows Mono Editor/Player에서 성공했습니다. Player의 pool 재사용 관찰은 **generic `ObjectPool<T>`** 경로입니다. [`최종 소비 결과`](https://github.com/KangSungKyu/TPLab/blob/4704dc0387d5e22ad8eae745386dcc8a03e78680/doc/validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json).
- 이전 focused `object-pool/green-*.json` 자료는 과거 증거이며 현행 `PrefabPool` 결과로 사용하지 않습니다.
- 이 근거는 Unity `6000.3.18f1` / Windows Mono 범위이며, 다른 버전·backend·플랫폼의 호환성을 입증하지 않습니다.

구현: [`ObjectPool.cs`](../../Assets/TPLab/Core/Pooling/ObjectPool.cs), [`PrefabPool.cs`](../../Assets/TPLab/Core/Pooling/PrefabPool.cs).
