# 제네릭 ObjectPool 계약과 Unity 기본 풀 검토

`TPLab.Core.Pooling.ObjectPool<T>`는 `where T : class`인 일반 C# 클래스와 Unity 참조 타입에 사용할 수 있다. 구현 파일은 System API만 사용한다. 동일 assembly의 `PrefabPool`은 GameObject 생성·활성화·Transform·파괴를 맡는 어댑터다. 프리팹 호출자는 기존 API를 유지하고 일반 클래스는 ObjectPool<T>를 직접 사용한다.

## 사용 계약

- 생성자: `ObjectPool<T>(Func<T> createInstance, int capacity, Action<T> onRent = null, Action<T> onReturn = null, Action<T> onDestroy = null)`. 정원은 양수, factory는 필수다.
- factory는 null이 아닌 새로운 객체를 이 풀 전용으로 제공한다. 동일 풀의 이미 소유한 객체를 다시 제공하면 예외이며 기존 객체는 폐기하지 않는다. 서로 다른 풀에 동일 객체를 공유해서는 안 된다. factory 내부 부분 생성물이 생긴 뒤 실패하면 factory가 정리한다.
- `TryRent(out T)`는 대기 객체를 먼저 재사용하고 총 소유 정원 소진에만 false/null을 반환한다. 총 정원에는 대여 중 객체가 포함된다. factory·준비 콜백 오류는 예외로 전달한다.
- `Return(T)`은 해당 풀에서 대여 중인 객체만 허용한다. null·비소유·중복 반환을 거부한다. 값 Equals/GetHashCode를 재정의하거나 값이 바뀌어도 참조 동일성으로 소유권을 판별한다. 같은 값을 가진 서로 다른 객체는 각각의 슬롯이다.
- 준비·반환 콜백이 실패하면 해당 객체를 소유 목록에서 제거하고 onDestroy를 호출한다. 다른 객체는 보존하고 정원을 회복한다. onDestroy도 실패하면 원래 오류와 정리 오류를 AggregateException으로 함께 전달한다. 정리 완료를 보장할 수 없는 외부 자원은 정리 콜백이 재시도·보고 정책을 소유한다.
- `Dispose()`는 먼저 영구 종료와 카운트 0을 확정하고 대여·대기 객체의 정리를 모두 시도한다. 일부 정리 콜백이 실패해도 나머지를 수행한 뒤 AggregateException으로 모은다. 반복 Dispose는 무시하며 실패한 정리를 다시 호출하지 않는다. 종료 후 대여·반환은 ObjectDisposedException이다. 종료 시 반환 콜백은 호출하지 않는다.
- 단일 스레드에서 사용한다. factory·준비·반환·실패 정리 도중 풀 변경 재진입은 거부한다. 동시 접근의 lock·자동 반환 lease·prewarm·전역 registry는 추가하지 않았다.
- 일반 클래스의 자원 정리는 onDestroy에서 명시한다. IDisposable을 자동 호출하지 않으며, managed 객체만 사용하면 콜백 없이 소유 참조를 해제한다. 제네릭 풀 자체는 Unity의 파괴된 객체 판정·SetActive·Transform을 처리하지 않는다. Unity 객체에는 적절한 콜백을 제공하거나 PrefabPool을 사용한다.
- 대기 보관은 Stack<T>, 소유 상태는 참조 비교 Dictionary<T, bool> 하나로 관리한다. Unity 기본 풀의 내부 카운터와 별도 소유 카운터를 이중 관리하지 않는다.

```csharp
using System.Collections.Generic;
using TPLab.Core.Pooling;

using (var pool = new ObjectPool<List<int>>(
    createInstance: () => new List<int>(),
    capacity: 16,
    onReturn: list => list.Clear()))
{
    if (pool.TryRent(out var list))
    {
        try
        {
            list.Add(42);
        }
        finally
        {
            pool.Return(list);
        }
    }
}
```

## Unity 기본 풀 검토 결과

Unity `ObjectPool<T>`도 일반 C# 클래스를 지원한다. 기본 풀의 정상 반환이 상시 실패하는 현상은 이번 검증에서 확인하지 않았다. 아래는 Unity 6000.3.18f1의 실제 API 실행 결과이며, 모든 버전·Player 동작을 보장하는 결과는 아니다. [공식 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html), [공식 참조 소스](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/ObjectPool/ObjectPools.cs). 참조 소스 master와 설치 버전은 다를 수 있어 실제 설치 버전의 테스트를 근거로 삼았다.

| 조건 | 실제 관찰 | TPLab에서 필요한 계약 |
|---|---|---|
| 일반 클래스 정상 Get → Release → Get | 동일 객체 재사용 | 그대로 유지 |
| actionOnRelease 예외 | 대기 보관 전에 종료; 객체는 대기 풀에 들어가지 않음 | 실패 객체 폐기·정원 회복 |
| actionOnGet 예외 | 생성 슬롯이 롤백되지 않고 CountAll에 남음 | 실패 생성·준비 정리 |
| collectionCheck=false에서 중복 Release | 같은 참조를 두 번 대여할 수 있음 | 빌드 종류와 무관하게 중복 반환 거부 |
| 비소유 객체 Release | 받아들임; 소유 검사가 없음 | 비소유 반환 거부·객체 보존 |
| Clear | 대기 객체만 파괴; 대여 객체는 보존; 이후 Get 가능 | 대여 객체 포함 영구 종료 |
| maxSize 초과 Release | 초과 반환 객체를 의도적으로 폐기 | 총 소유 정원과 보관 한도 구분 |

콜백 예외 처리 부재, 오용 검사 범위, 수명 정책의 차이이며 위 결과를 Unity 엔진의 일반적인 반환 결함으로 분류하지 않는다. `PrefabPool`을 완전히 제거하면 생성·활성화 순서와 보관 루트·Transform·파괴 정책을 호출자마다 작성해야 한다. 따라서 공통 풀 구현을 제네릭으로 전환하고 프리팹 정책만 어댑터에 남겼다.

## 검증 증거 (2026-10-02)

- TPLab Editor Unity 6000.3.18f1, Connector 0.4.1, PID 42616을 사용했다. 기존 미추적 ProjectSettings/SceneTemplateSettings.json은 보존했고 패키지·프로젝트 설정·기존 씬·Cashier는 수정하지 않았다.
- Unity 기본 API 재현: [7/7 통과](validation/generic-pool/unity-contract.json). 관찰 자체를 검증하는 테스트이며 문제가 수정됐다는 의미가 아니다.
- TDD Red: API stub에서 새 일반 클래스 테스트 [24건 모두 실패](validation/generic-pool/red-generic.json), skip 0.
- Green 전체 EditMode: [38/38 통과](validation/generic-pool/green-edit.json) = 제네릭 24 + Unity API 재현 7 + 기존 프리팹 6 + Addressables 기존 테스트 1. 실패·skip 0.
- 기존 프리팹 회귀 전체 PlayMode: [17/17 통과](validation/generic-pool/green-play.json), 실패·skip 0. CLI는 domain reload 구간에 editor stopped를 보고했지만 동일 Editor는 살아 있었고 실행은 완료됐다. run ID `28588-1790911711368444900`의 저장 결과를 복구했으며 테스트를 재실행하지 않았다. [복구 경로·시각](validation/generic-pool/play-recovery.json).
- Editor 컴파일 완료, 최종 상태 ready, [Console 오류 0건](validation/generic-pool/console-errors.json).
- Unity 참조가 없는 별도 .NET 10 프로그램: 실제 ObjectPool.cs를 링크하여 컴파일하고 [검사 6건 통과](validation/generic-pool/dotnet-probe.txt). 임시 프로젝트와 저장된 검증 프로젝트에서 각각 1회 실행하여 재현성을 확인했다. [검증 코드](validation/generic-pool/dotnet-probe/Program.cs), [프로젝트](validation/generic-pool/dotnet-probe/PoolProbe.csproj). .NET SDK는 설치된 10.0.401을 사용했다. 다른 대상 프레임워크·Unity 버전·Unity Player 빌드는 미실행이다.
- [검증 입력 SHA-256](validation/generic-pool/test-input-sha256.json)을 남겼다. runtime과 테스트 소스에 대한 검증 후 변경은 없으며 위 결과와 같은 작업 커밋에 포함한다.
- 사용자 직접 조작이 필요한 항목은 없다. 별도 Unity 소비 프로젝트의 가져오기·컴파일·Player 검증은 후속 단계이며 .NET 확인으로 대체하지 않는다. 원본 CLI 출력과 .NET 빌드 생성물은 버전 관리에서 제외한 Temp/GenericPooling에 있다.

저장된 .NET 검증은 저장소 루트에서 `dotnet run --project doc/validation/generic-pool/dotnet-probe/PoolProbe.csproj --artifacts-path Temp/GenericPooling/SavedProbeBuild`로 재실행한다. 빌드 생성물도 Temp에 둔다.
