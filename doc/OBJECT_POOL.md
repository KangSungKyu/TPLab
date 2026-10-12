# 로컬 PrefabPool 계약

로컬 GameObject 프리팹 또는 생성 원본의 동기 풀 어댑터다. 공통 정원·소유권·실패 정리는 [제네릭 ObjectPool<T>](GENERIC_POOL.md)에 위임한다. namespace는 TPLab.Core.Pooling, runtime assembly는 TPLab.Core다. 현재 지원·검증 기준은 Unity 6000.3이며, Assets/TPLab/Core 폴더와 .meta를 소비 프로젝트에 함께 가져오는 방식을 사용한다. UPM 배포는 별도 소비 프로젝트 검증 단계에서 결정한다.

- PrefabPool(prefab, capacity, parent, onRent, onReturn)은 원본과 부모를 빌려 사용한다. 원본은 새 객체를 생성하는 동안, 부모는 풀 수명 동안 살아 있어야 한다. 풀은 생성한 모든 복제본과 비활성 보관 루트를 소유한다.
- Capacity는 대여 중 객체와 대기 객체를 합친 총 소유 정원이다. TryRent(out instance)는 대기 객체를 우선 재사용하고 정원 소진에만 false/null을 반환한다. 입력·수명·콜백 오류는 예외다.
- 생성은 비활성 보관 루트 아래에서 수행한다. 대여 시 비활성 복제본을 지정 부모로 옮기고 원본의 생성 시점 localPosition/localRotation/localScale을 복원한 뒤 onRent → SetActive(true) 순서로 처리한다. 활성 원본도 복제본의 OnEnable 전에 준비된다. 부모가 비활성이면 activeInHierarchy를 강제로 바꾸지 않는다.
- Return은 원래 풀에서 대여 중인 객체만 받는다. null/비소유 객체는 ArgumentException 계열, 중복 반환은 InvalidOperationException으로 거부한다. 반환은 비활성화 → onReturn → 보관 루트 이동·transform 복원 순서다. 이벤트·속도·드래그·UI·연출 등 소비자 상태는 onReturn에서 정리한다.
- 콜백 실패 또는 콜백의 즉시 객체 파괴는 해당 객체를 폐기하고 정원을 회복한 뒤 예외를 전달한다. 다른 객체와 원본은 보존한다. 메서드·Unity 수명 콜백에서 풀 변경 재진입은 금지하며 InvalidOperationException으로 거부한다.
- Dispose는 대여 중·대기 중 객체와 보관 루트를 전부 정리하고 영구 종료한다. 반복 Dispose는 무시한다. 이후 대여·반환은 ObjectDisposedException이다. PlayMode의 Destroy는 프레임 종료 시 완료되며 객체는 먼저 비활성화한다. 종료 시 반환 콜백은 호출하지 않는다. 소비자는 반환하지 못하는 종료 경로의 외부 구독도 직접 정리해야 한다.
- Unity 메인 스레드 전용이다. 소비자는 복제본을 정상적으로 반환해야 한다. 외부에서 파괴된 복제본은 다음 TryRent의 지연 확인으로 슬롯을 회수하며 파괴된 객체의 Return은 계속 ArgumentNullException이다. 보관 루트 수정은 지원하지 않는다. Addressables·비동기 준비·Singleton·전역 풀 registry·prewarm은 이번 단계에 포함하지 않는다.

Cashier에서 총 소유 정원·수명 소유권·실패 정리의 필요성을 확인했다. 초기 구현은 Unity 기본 풀의 보관 기능을 활용했으나, 일반 C# 클래스 지원과 정책 일관성을 위해 System 컬렉션 기반 제네릭 풀로 전환했다. 현재 runtime은 UnityEngine.Pool에 의존하지 않는다. Cashier 코드와 게임별 상태는 복사하지 않는다. 전환 근거와 역사적 검증은 [GENERIC_POOL.md](GENERIC_POOL.md)를 따른다.

```csharp
using TPLab.Core.Pooling;

var pool = new PrefabPool(prefab, capacity: 16, parent: transform,
    onReturn: instance =>
    {
        var body = instance.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = UnityEngine.Vector3.zero;
            body.angularVelocity = UnityEngine.Vector3.zero;
        }
    });

if (pool.TryRent(out var instance))
{
    // 사용이 끝나면 같은 풀에 반환한다. 실패 경로에도 반환을 보장한다.
    pool.Return(instance);
}

// 소유자가 종료할 때 호출한다.
pool.Dispose();
```

검증: EditMode는 생성 입력과 실제 prefab asset 보존, PlayMode는 재사용·정원·반환 검증·콜백 실패·재진입·OnEnable 순서·파괴·부모 수명을 확인한다. 시각 UX나 사용자 직접 조작이 필요한 기능은 없다. 다른 Unity 버전·소비 프로젝트·Player 빌드는 이후 단계의 검증 대상이다.

## 외부 파괴와 지연 카운트 (IMP-01)

내부 ObjectPool 생성자의 마지막 선택 매개변수 `Func<GameObject, bool> isAlive`에 Unity fake-null 판정(`instance => instance != null`)을 제공한다. 죽은 대기 객체는 건너뛰고 살아 있는 객체의 LIFO 재사용 순서를 유지한다. 재사용 대상이 없고 정원이 찼을 때만 대여 목록을 확인하여 외부 파괴된 복제본의 슬롯을 회수한다. Unity Destroy 완료 전에는 살아 있다고 판정할 수 있으므로 PlayMode에서는 프레임 종료 이후의 상태가 기준이다. source/parent/storage context 실패는 계속 기존 예외 경로를 따른다.

CountOwned/CountRented/CountInactive는 추적 수를 읽는 비할당 getter이며 외부 파괴를 즉시 반영하지 않는다. 판정·정리 중 변경 재진입은 거부한다. 제거된 복제본은 기존 cleanup을 한 번 시도하며, 검출된 죽은 객체의 cleanup 실패는 나머지를 모두 시도한 뒤 집계한다. 이미 파괴된 Unity 참조의 cleanup은 안전하게 종료한다. Return(destroyed)는 슬롯 회수 API가 아니며 여전히 ArgumentNullException이다. 소비자 구독·외부 자원 정리는 소비자가 소유한다.

PrefabPool 공개 시그니처는 유지된다. ObjectPool 생성자에 마지막 선택 매개변수가 추가되어 기존 소스는 호환되지만 미리 컴파일한 소비 어셈블리는 재빌드해야 한다. [IMP-01 검증 기록](validation/imp-01/README.md)은 source `901d56c582691d55f4aa9698e91be57231328635`의 초기Green18/24/15·후속경계3/3·최종전체Edit335/Play309 자동 검증 완료·사용자 수락 대기이며 아래 결과는 이전 구현의 역사적 증거다.

## 최초 Unity 기본 풀 기반 구현의 실행 증거 (2026-10-02)

- 정확한 TPLab Editor: Unity 6000.3.18f1, Connector 0.4.1, PID 42616. 새 Editor를 실행하지 않았다.
- Red: 최소 API stub에서 EditMode 6건 중 통과 1·실패 5·skip 0, PlayMode 17건 중 통과 0·실패 17·skip 0을 실제 실행했다. [EditMode 결과](validation/object-pool/red-edit.json), [PlayMode 결과](validation/object-pool/red-play.json).
- Green: 최종 구현에서 전체 EditMode 통과 7/7(공용 풀 6건 + Addressables 패키지의 기존 테스트 1건), 전체 PlayMode 통과 17/17. 두 실행 모두 실패 0·skip 0이다. [EditMode 결과](validation/object-pool/green-edit.json), [PlayMode 결과](validation/object-pool/green-play.json).
- 테스트에 사용한 최종 C#·asmdef·manifest/lock의 [SHA-256 목록](validation/object-pool/test-input-sha256.json)을 남겼다. 이 파일과 테스트 결과는 동일한 작업 커밋에 포함한다.
- Editor 컴파일 완료, 최종 상태 ready, Console error 조회 0건을 확인했다. [Console 결과](validation/object-pool/console-errors.json). 원본 CLI 출력은 버전 관리에서 제외한 Temp/ObjectPool에 있다.
- EditMode 종료 직후 PlayMode 호출 1건은 "no Unity instances running"으로 실행 전에 종료됐다. 동일 Editor의 ready를 다시 확인한 뒤 PlayMode를 실행했으며, 위 17/17은 실제 실행 결과다.
- 자동 테스트 종료 후 생성된 ProjectSettings/SceneTemplateSettings.json은 변경 범위 밖의 미추적 파일로 보존했다. 프로젝트 설정·의존성·기존 씬·Cashier는 수정하지 않았다.
- 직접 확인이 필요한 항목은 없으며, 원격 CI 구성은 아직 별도 구축하지 않았다. 다른 Unity 버전·별도 소비 프로젝트·Player 빌드는 미실행이며 전체 배포 호환성 완료를 주장하지 않는다.
