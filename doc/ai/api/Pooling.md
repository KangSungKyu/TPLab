# Pooling

**Module / Namespace / Assembly:** Pooling / `TPLab.Core.Pooling` / `TPLab.Core`

**SourceRevision / SourcePath / HumanContract:** `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`; [`ObjectPool.cs`](../../../Assets/TPLab/Core/Pooling/ObjectPool.cs), [`PrefabPool.cs`](../../../Assets/TPLab/Core/Pooling/PrefabPool.cs); [`Pooling.md`](../../api/Pooling.md).

**ImplementationStatus / ValidationStatus / Evidence:** Implemented / Partial. 현행 source revision의 Unity `6000.3.18f1` Windows Mono 전체 회귀: [EditMode 258/258](../../validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](../../validation/input-system/p4/full-PlayMode.json); [P4 기록](../../validation/input-system/p4/README.md). 회귀에는 `ObjectPool<T>`·`PrefabPool` 검사가 포함됨. [소비 Player](../../validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json)의 pool 재사용은 generic `ObjectPool<T>` 경로. 과거 focused `object-pool/green-*.json`은 현행 결과가 아님. 다른 Unity 버전·backend·플랫폼 범용성은 검증하지 않음.

**Symbol / Signature / Constraints:**

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

**Inputs / Outputs / Errors:** `capacity`는 양수여야 함. `ObjectPool<T>` factory는 null이 아니며 새 고유 참조를 반환해야 함. `TryRent`가 false면 용량 소진이며 출력은 null. `Return`은 같은 풀에서 현재 대여된 참조만 받음. 잘못된 factory 결과·재진입·중복 반환은 `InvalidOperationException`, 다른 풀 소유 객체는 `ArgumentException`, null 인수는 `ArgumentNullException`, 종료 후 사용은 `ObjectDisposedException`. Factory 오류는 전파됨. 대여 준비/반환 재설정 오류는 해당 인스턴스를 폐기하고 전파하며, destroy cleanup도 실패하면 두 오류가 `AggregateException`에 포함됨. `PrefabPool`은 null/파괴된 prefab과 양수가 아닌 용량을 거부함. PrefabPool.Return의 null/파괴된 인수는 ArgumentNullException, TryRent에서 파괴된 inactive clone 또는 유효하지 않은 source/parent/storage context는 InvalidOperationException. 두 pool의 nonpositive capacity는 ArgumentOutOfRangeException.

**Ownership / Lifecycle / Threading:** 두 풀은 생성 객체와 대여 중인 객체까지 독점 소유함. 소비자는 빌려 씀. `ObjectPool<T>` 소유 판정은 참조 동일성. 용량은 대여+대기 수. `onReturn`은 재사용 대기열 저장 전에 실행됨. 준비/재설정 실패는 해당 항목만 제거. `ObjectPool.Dispose`는 정리 callback 전 소유 목록을 비우고 모든 항목에 `onDestroy` 시도. `onReturn`을 호출하지 않고 `T.Dispose`도 자동 호출하지 않음. `PrefabPool`은 clone과 비활성 저장 root를 소유하며 source prefab/외부 parent는 소유하지 않음. 대여는 parent 재설정→저장한 local transform 복원→`onRent`→활성화. 반환은 비활성화→`onReturn`→저장 root로 이동→transform 복원. `Dispose`는 대여/대기 clone과 저장 root를 파괴하며 source/외부 parent는 보존. Play Mode 파괴는 프레임 끝에 완료. `ObjectPool<T>` 호출은 단일 스레드 순차 호출이며 내부 lock 없음. factory/callback은 같은 풀을 재진입 변경할 수 없음. `PrefabPool`은 Unity 메인 스레드 전용.

**Concurrency / Cancellation / FailureCleanup:** 취소 API 없음. 내부 동기화 없음. callback 중 mutation 재진입은 거부됨. Rent/return callback 실행 중 `Dispose`는 거부됨; 반복 Dispose는 무시됨. `onDestroy`는 모든 항목을 시도하고 오류를 집계함. Prefab callback 오류는 해당 clone만 폐기하며 callback은 재진입하지 않아야 함.

**Configuration / ExtensionPoints:** 호출자가 capacity와 callback을 정함. `ObjectPool<T>` 외부 자원 정리는 `onDestroy`에서 명시하고 자동 `IDisposable` 호출을 기대하지 말 것. `PrefabPool`은 destroy callback 없음. `onRent`는 대여/활성화 전 구성, `onReturn`은 재사용 전 상태 초기화. callback은 같은 풀을 변경하면 안 됨. source prefab/parent는 소비자 소유이며 사용 중 유지.

**RequiredSequence / ForbiddenUsage:** 생성 → `TryRent` → 대여 객체 사용 → 정확히 한 번 `Return`; 소유 수명 종료 시 `Dispose`. 종료 시 아직 대여 중인 객체도 풀이 정리할 수 있음. 다른 풀에 반환, 중복 반환, 종료 후 사용, 대여 clone 직접 파괴 금지. 풀 소유 정리로 source prefab이나 외부 parent를 파괴하지 말 것. 외부 자원을 가진 generic 인스턴스에는 필요한 `onDestroy` 설정.

**Example / Compatibility / Limitations:** 사람용 계약의 예제는 설명 발췌이며 NotRun. 이 source revision의 API migration 기록 없음. 검증은 명시된 Unity/Windows Mono 실행 범위로 한정되며 Player·IL2CPP·다른 플랫폼 지원을 주장하지 않음.
