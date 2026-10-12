# Pooling

**Module / Namespace / Assembly:** Pooling / `TPLab.Core.Pooling` / `TPLab.Core`

**SourceRevision / SourcePath / HumanContract:** `2c8c0768c53878dcc9c2d4ed090582092e9dc5e0`; [`ObjectPool.cs`](../../../Assets/TPLab/Core/Pooling/ObjectPool.cs), [`PrefabPool.cs`](../../../Assets/TPLab/Core/Pooling/PrefabPool.cs); [`Pooling.md`](../../api/Pooling.md).

**ImplementationStatus / ValidationStatus / Evidence:** Implemented / Verified for IMP-02 in Windows Mono Editor; Red20 API부재 → Green62/62·전체Edit348/348·Play316/316, fail/skip0, 사용자 수락 대기; [IMP-02 검증 기록](../../validation/imp-02/README.md). 아래는 IMP-01의 역사적 Windows Mono Editor 결과로 IMP-02 Green을 입증하지 않음: 2026-10-12 사용자 수락 완료(PR #7). 실제 Red17pass/1fail → Green18/24/15 전부pass, 초기 전체Edit334/Play307 후 경계3개 추가, 최종 전체Edit335/335·Play309/309, fail/skip0; [IMP-01 검증 기록](../../validation/imp-01/README.md). 새 소비 프로젝트/Player는 이번 미실행. 다음은 이전 source revision `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`의 역사적 증거로 현재 Green을 입증하지 않음: Unity `6000.3.18f1` Windows Mono 전체 회귀: [EditMode 258/258](../../validation/input-system/p4/full-EditMode.json), [PlayMode 217/217](../../validation/input-system/p4/full-PlayMode.json); [P4 기록](../../validation/input-system/p4/README.md). 회귀에는 `ObjectPool<T>`·`PrefabPool` 검사가 포함됨. [소비 Player](../../validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json)의 pool 재사용은 generic `ObjectPool<T>` 경로. 과거 focused `object-pool/green-*.json`은 현행 결과가 아님. 다른 Unity 버전·backend·플랫폼 범용성은 검증하지 않음.

**Symbol / Signature / Constraints:**

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

**Inputs / Outputs / Errors:** `capacity`는 양수여야 함. `ObjectPool<T>` factory는 null이 아니며 새 고유 참조를 반환해야 함. `TryRent`가 false면 용량 소진이며 출력은 null. `Return`은 같은 풀에서 현재 대여된 참조만 받음. 잘못된 factory 결과·재진입·중복 반환은 `InvalidOperationException`, 다른 풀 소유 객체는 `ArgumentException`, null 인수는 `ArgumentNullException`, 종료 후 TryRent/Return은 `ObjectDisposedException`; TryReturn은 입력보다 IsDisposed를 먼저 검사하여 false. Factory 오류는 전파됨. 대여 준비/반환 재설정 오류는 해당 인스턴스를 폐기하고 전파하며, destroy cleanup도 실패하면 두 오류가 `AggregateException`에 포함됨. `PrefabPool`은 null/파괴된 prefab과 양수가 아닌 용량을 거부함. 열린 PrefabPool.Return의 null/파괴된 인수는 ArgumentNullException, TryRent의 유효하지 않은 source/parent/storage context와 hook 중 clone 파괴는 InvalidOperationException. 외부에서 이미 파괴된 inactive clone은 건너뜀. 두 pool의 nonpositive capacity는 ArgumentOutOfRangeException.

**Ownership / Lifecycle / Threading:** 두 풀은 생성 객체와 대여 중인 객체까지 독점 소유함. 소비자는 빌려 씀. `ObjectPool<T>` 소유 판정은 참조 동일성. 용량은 대여+대기 수. `onReturn`은 재사용 대기열 저장 전에 실행됨. 준비/재설정 실패는 해당 항목만 제거. `ObjectPool.Dispose`는 정리 callback 전 소유 목록을 비우고 모든 항목에 `onDestroy` 시도. `onReturn`을 호출하지 않고 `T.Dispose`도 자동 호출하지 않음. `PrefabPool`은 clone과 비활성 저장 root를 소유하며 source prefab/외부 parent는 소유하지 않음. 대여는 parent 재설정→저장한 local transform 복원→`onRent`→활성화. 반환은 비활성화→`onReturn`→저장 root로 이동→transform 복원. `Dispose`는 대여/대기 clone과 저장 root를 파괴하며 source/외부 parent는 보존. Play Mode 파괴는 프레임 끝에 완료. `ObjectPool<T>` 호출은 단일 스레드 순차 호출이며 내부 lock 없음. factory/callback은 같은 풀을 재진입 변경할 수 없음. `PrefabPool`은 Unity 메인 스레드 전용.

**Concurrency / Cancellation / FailureCleanup:** 취소 API 없음. 내부 동기화 없음. callback 중 mutation 재진입은 거부됨. Rent/return callback 실행 중 `Dispose`는 거부됨; 반복 Dispose는 무시됨. `onDestroy`는 모든 항목을 시도하고 오류를 집계함. Prefab callback 오류는 해당 clone만 폐기하며 callback은 재진입하지 않아야 함.

**LazyLiveness / Allocation / Exceptions:** 마지막 선택 `Func<T, bool> isAlive = null`. null은 기존 unchecked 정책이며 scratch도 생성하지 않음. 설정 시 생성자에서 capacity 크기 List scratch를 준비. 모든 predicate는 _isBusy guard 안에서 실행. inactive Peek 판정 후 죽은 항목 제거, survivor LIFO 보존. 정상 reuse는 owned loans를 순회하지 않음. reuse가 없고 capacity가 full일 때만 concrete Dictionary 순회로 죽은 loan을 scratch에 수집한 뒤 제거. predicate 예외는 Peek 후보 또는 sweep ownership을 보존; 이미 제거한 inactive는 복구하지 않음. 검출·제거된 죽은 항목의 onDestroy를 각각 한 번 시도, 모두 시도한 후 cleanup 오류 집계. slots는 cleanup 실패에도 회수되고 Dispose에서 재시도하지 않음. inactive cleanup 오류 뒤 predicate 오류가 발생하면 함께 집계. factory null/duplicate 검증은 ownership 등록 전에 유지; 등록 후 dead/predicate 오류이면 기존 실패 정리 경로로 discard, onRent 미호출, cleanup도 실패하면 원래 오류와 AggregateException. Return/Dispose 검증·예외 우선순위 유지. Count getters는 판정·mutation·allocation 없음, external death는 관련 TryRent까지 stale 가능. 공개 Prune은 보류/별도 사용자 결정. alive loan·omitted policy는 baseline PASS→현재 PASS, destroyed inactive의 동일 호출 교체는 사후 baseline FAIL→현재 PASS; 사전 Red와 구분. Warmed reuse/alive exhausted-capacity sweep/count getters/dead-loan bookkeeping recovery는 각1,000회 측정의 두 GC 테스트에서 풀 내부 allocation 0 확인; caller predicate/callback/factory allocation 별도, CPU 0 보장 없음. PrefabPool은 Unity fake-null 정책 사용; Destroy 완료 전은 alive로 판정 가능. source/parent/storage 수명 오류와 hook 내부 파괴 예외는 유지.

**TryReturn / DisposalPriority / Allocation:** ObjectPool<T>.TryReturn(T instance), PrefabPool.TryReturn(GameObject instance). 정확한 순서: if(IsDisposed) return false; Return(instance); return true. false는 disposed에만 해당; null/foreign/destroyed/context 검사보다 우선하고 callback·mutation·cleanup 없음. Dispose cleanup 중 호출도 false. Dispose가 owned loans cleanup을 소유하며 TryReturn은 이를 재시도하거나 foreign 객체를 destroy하지 않음. open이면 기존 Return 검증/오류 순서·failure cleanup 유지; null/foreign/duplicate/context/callback 오류를 catch하여 false로 숨기지 않음. open mutation callback reentry는 기존 예외 전파. Prefab active destroyed 인수는 기존 ArgumentNullException. synchronous·single-thread sequential API, Prefab은 Unity main-thread 사용 전제이며 실제 thread guard·동시 Dispose synchronization 추가 없음. P0 wrapper 정상 allocation 0, warm storage/caller callbacks 조건은 Return과 같음. IMP-02 WarmedTryReturnReuseAllocatesZero의 1000회strict0바이트를 집중/전체Edit에서 확인(바인딩·warmup·caller/factory/Unity/Dispose비용 제외). 기존 Return/Dispose API 시그니처·body 유지, TryReturn만 추가.

**Configuration / ExtensionPoints:** 호출자가 capacity와 callback을 정함. `ObjectPool<T>` 외부 자원 정리는 `onDestroy`에서 명시하고 자동 `IDisposable` 호출을 기대하지 말 것. `PrefabPool`은 destroy callback 없음. `onRent`는 대여/활성화 전 구성, `onReturn`은 재사용 전 상태 초기화. callback은 같은 풀을 변경하면 안 됨. source prefab/parent는 소비자 소유이며 사용 중 유지.

**RequiredSequence / ForbiddenUsage:** 생성 → `TryRent` → 대여 객체 사용 → 정확히 한 번 `Return`(종료 경합이 있는 정리에서는 TryReturn); 소유 수명 종료 시 `Dispose`. 종료 시 아직 대여 중인 객체도 풀이 정리할 수 있음. 열린 풀에서 다른 풀에 반환·중복 반환 금지, 종료 후 TryRent/Return 금지(TryReturn은 false), 정상 사용에서는 대여 clone을 반환할 것. 외부 파괴의 슬롯은 지연 회수되며 열린 풀의 Return(destroyed)는 계속 ArgumentNullException. 풀 소유 정리로 source prefab이나 외부 parent를 파괴하지 말 것. 외부 자원을 가진 generic 인스턴스에는 필요한 `onDestroy` 설정.

**Example / Compatibility / Limitations:** 사람용 계약의 예제는 설명 발췌이며 NotRun. ObjectPool 생성자 마지막 선택 isAlive 추가: 기존 소스 호출은 호환되나 미리 컴파일한 소비 어셈블리는 재빌드 필요. IMP-01은 PrefabPool 공개 시그니처 유지; IMP-02는 양쪽 TryReturn API만 추가. NotRun 발췌: `bool returned = pool.TryReturn(instance);` true=Return 성공, false=disposed만 해당. 검증은 명시된 Unity/Windows Mono 실행 범위로 한정되며 Player·IL2CPP·다른 플랫폼 지원을 주장하지 않음.
