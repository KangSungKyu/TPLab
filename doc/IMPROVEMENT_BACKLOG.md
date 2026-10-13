# TPLab 개선 백로그

**개정 2026-10-12**: PR #4 검토 결과를 반영했다(IMP-07 전제 갱신, 위치 표기 2곳 정정, IMP-13 추가). 코드 위치는 Codex 대조(main `c1804e17`, 지정 Core/Input 파일은 기준 `de879b9` 이후 변경 없음)에 근거한다.

작업자(AI 에이전트)가 개선 작업을 수행할 때 참고하는 문서다. 코드 리뷰에서 도출한 항목을 **한 번에 하나씩** 처리할 수 있게 증거·테스트 스펙·제약을 정리했다.

## 먼저 읽을 것

1. [AGENTS.md](../AGENTS.md)를 먼저 따른다. TDD(Red→Green→Refactor), 완료 보고, 문서 갱신, 회고, Git 작업 규칙은 이 문서가 아니라 AGENTS.md가 소유한다. 이 문서는 그 규칙을 바꾸지 않는다.
2. 요청받은 항목(IMP-NN) 하나만 읽고 수행한다. 다른 항목으로 범위를 넓히지 않는다.
3. 항목의 상태가 **확정 대기**이면(현재 해당 항목 없음) 구현하지 않는다. 선택지별 영향 분석만 보고하고 사용자의 결정을 기다린다. 결정은 아래 "결정 기록"에 사용자가 적는다.
4. 항목은 위 "진행 운영"의 승인 게이트에 따라 한 번에 하나씩 진행한다. 사용자 승인 없이 다음 항목을 시작하지 않는다.
5. 할당(GC) 관련 코드를 쓰거나 고치는 항목은 [GC_ALLOCATION_GUIDELINE.md](GC_ALLOCATION_GUIDELINE.md)(초안)를 함께 따른다.

## 이 문서의 신뢰 범위

- 근거 source: `de879b9396ddae0523bd3ab86939b679b383dd92` (v0.0.1). 줄 번호는 이 revision 기준이므로 **작업 전 현재 HEAD와 대조**한다.
- 항목은 코드를 읽어 도출했고 Unity에서 실행해 확인하지 않았다. "수정 전 예상 결과"는 추정이다. 실제 Red는 실행해서 확인하고, 예상과 다르면 구현 전에 보고한다.
- 테스트를 실행하지 못했으면 Red·Green 확인으로 기록하지 않는다(AGENTS.md).

## 추가 규칙 (AGENTS.md 보완)

- 수정 단계에서는 Red 테스트 파일을 변경하지 않는다. 어설션 삭제, 기대값 변경, `Ignore` 추가, 타임아웃 완화로 통과시키지 않는다.
- 테스트가 무한 대기할 수 있으면 `[Timeout]`을 먼저 건다(IMP-04에서 `[UnityTest]`에도 실제로 적용되는지 확인). 비동기는 기존 방식(UniTaskCompletionSource 게이트, `SceneRootInstallerProbe`의 실패 주입 플래그)을 쓰고 `Delay`/`Sleep`으로 시간을 기다리지 않는다. **유일한 예외**: IMP-03의 타임아웃 동작 자체를 검증하는 테스트는 실제 경과 시간이 필요하므로 짧은 타임아웃(약 0.1~0.2초)과 `[Timeout]` 보호 아래에서 허용한다.
- 새 헬퍼를 만들기 전에 기존 헬퍼(`CreatePool`, `SceneRootInstallerProbe`, `Wait`)를 재사용할 수 있는지 확인한다.
- public 계약이 바뀌면 같은 단위에서 XML 주석과 사람/AI 문서(`doc/api/*.md`, `doc/ai/api/*.md`)를 함께 갱신한다.
- 새 패키지(Performance Testing 등)는 추가하지 않는다(AGENTS.md: 승인 없는 패키지 추가 금지).
- Unity 테스트는 `tools/run_unity_tests.py`로 실행한다(기존 ready Editor 필요). 예:
  `python tools/run_unity_tests.py --project <절대경로> --mode PlayMode --filter TPLab.Core.Tests.PrefabPoolTests --output Temp/<작업>/play.json`

## 진행 운영 (항목 승인 게이트)

항목은 **한 번에 하나씩** 진행한다. 한 항목이 끝나면 사용자가 확인하고, 사용자의 **명시적 승인 이후에만** 다음 항목을 자동으로 시작한다. 승인 뒤에는 별도의 "시작" 지시를 기다리지 않는다.

### 진행 큐 (이 순서로 자동 진행)

| 순서 | ID | 진행 상태 | 비고 |
|---|---|---|---|
| 1 | IMP-04 | 승인·완료 | 승인일 2026-10-12, PR #6 병합 완료; [적용 검증](validation/imp-04-apply/README.md) |
| 2 | IMP-01 | 승인·완료 | D-01 = A1; 승인일 2026-10-12, PR #7 사용자 병합 승인; [검증 기록](validation/imp-01/README.md), 실제 통합 결과는 PR 참조 |
| 3 | IMP-02 | 승인·완료 | 승인일 2026-10-13, PR #8 사용자 병합 승인; [검증 기록](validation/imp-02/README.md), 실제 통합 결과는 PR 참조 |
| 4 | IMP-05 | 진행 | 예상 Red가 pass1/1; GC 양성 대조0 → 계측 조사 사용자 결정 대기; [중간 보고](validation/imp-05/README.md). 버퍼 API 확인·구현은 이후 |
| 5 | IMP-03 | 대기 | 비동기 수명 위험 high. IMP-04의 타임아웃 보호가 적용된 뒤 Red → 구현 (D-03) |

큐 밖 항목(IMP-06~13)은 자동 진행하지 않는다. 사용자가 항목을 지정해 요청할 때만 시작한다. IMP-11은 측정 결과에 따른 사용자 결정, IMP-12는 사용자 검토가 필요하고, IMP-13은 IMP-12 확정 뒤에 시작한다.

진행 상태 값은 AGENTS.md의 구분을 따른다: **대기 → 진행 → 자동 검증 완료·사용자 확인 대기 → 승인·완료**. 증거가 없으면 상태를 올리지 않는다. 상태 갱신(이 표, 승인일, 보고 링크)은 해당 항목 브랜치의 커밋에 포함한다.

### 항목 하나의 흐름

1. 큐에서 첫 번째 "대기" 항목을 고른다. 이미 "진행" 또는 "사용자 확인 대기"인 항목이 있으면 새 항목을 시작하지 않는다.
2. AGENTS.md의 Git 작업 시작 전 확인, TDD(Red → Green → Refactor), 문서 갱신, 회고 규칙을 그대로 따른다. 이 문서는 그 규칙을 바꾸지 않는다.
3. 자동 검증(해당 테스트 + 전체 EditMode/PlayMode)을 마친 뒤 **완료 보고**(아래 형식)를 하고 상태를 "사용자 확인 대기"로 바꾼다.
4. **대기한다.** 시간이 지나거나 응답이 없는 것은 승인이 아니다. main 병합과 다음 항목 시작은 승인 전까지 하지 않는다(AGENTS.md의 최종 확인 gate와 동일).
5. 사용자가 승인하면: AGENTS.md "검증 후 병합" 절차로 병합하고, 상태를 "승인·완료"로 바꾼 뒤, 큐의 다음 항목을 시작하며 시작 보고(현재 브랜치, 새 브랜치 여부와 이유)를 남긴다.

### 승인 방법

- 승인: `IMP-NN 승인` 또는 같은 의미의 명시적 문장. 승인 대상 항목 ID가 불분명하면 되묻고 진행하지 않는다.
- 수정 요청: `IMP-NN 수정: <내용>` → 같은 항목을 이어서 수정하고 다시 완료 보고한다. 다음 항목으로 넘어가지 않는다.
- 보류/중단: `IMP-NN 보류` 또는 `중단` → 다음 항목을 시작하지 않고 현재 상태를 회고에 남긴다.
- "좋아 보이네요" 같은 모호한 반응은 승인으로 보지 않는다.

### 자동 진행을 멈추고 보고하는 경우 (다음 항목으로 넘어가지 않는다)

- Red가 예상한 이유로 실패하지 않았거나 이미 통과한 경우
- 테스트를 실행하지 못했거나(Editor 연결 불가 등) 실행 결과가 0건인 경우
- 전체 회귀 테스트에 실패나 새 skip이 생긴 경우
- 스펙이 모호하거나 이 문서와 현재 코드가 다른 경우(줄 번호 불일치가 아니라 동작·구조가 다를 때)
- 항목 범위를 넘는 변경이 필요해 보이는 경우, 기존 dirty·무관한 변경을 안전하게 분리할 수 없는 경우
- 항목에 명시된 중간 확인 지점: IMP-04 적용 범위, IMP-05 버퍼 API 이름

이 경우 상태를 "진행"으로 두고 원인·증거와 필요한 결정을 보고한다.

## 상태 요약

| ID | 항목 | 상태 | 우선 작업 |
|---|---|---|---|
| IMP-01 | PrefabPool 대여 클론 파괴 시 슬롯 누수 | **승인·완료**(D-01 = A1, 승인일 2026-10-12, PR #7) | 사용자 수락·병합 승인 완료; [검증 기록](validation/imp-01/README.md) |
| IMP-02 | Dispose 후 Return 계약 | **승인·완료**(D-02 = `TryReturn` 추가, 승인일 2026-10-13, PR #8) | 사용자 수락·병합 승인 완료; [검증 기록](validation/imp-02/README.md) |
| IMP-03 | 비협조 installer + ShutdownAsync 무한 대기 | **설계 확정**(D-03a~c 모두 확정) | Red 재현 테스트(타임아웃 필수) → 구현 |
| IMP-04 | 테스트 타임아웃 도입 | **승인·완료**(D-05a~c, 승인일 2026-10-12, PR #6) | 선택24개 메서드·30초 적용 병합 완료; [검증 기록](validation/imp-04-apply/README.md) |
| IMP-05 | GameSceneManager 폴링 속성 할당 | **진행·계측 확인 대기**(D-04, D-04b) | 예상 Red 미확립; [중간 보고](validation/imp-05/README.md), runtime 변경 없음 |
| IMP-06 | `Thread.Sleep` 시간 의존 테스트 | 제안(낮음) | - |
| IMP-07 | CI 구성 | 일부 완료(PR 규칙 검사, `@claude` 리뷰). Unity 테스트 CI는 보류 | 범위 재정의 |
| IMP-08~10 | 후속 리팩터링 | 스펙 없음 | IMP-01~05 이후 |
| IMP-11 | 할당 감사 후속(P1 전환·이벤트 경로) | 제안 | IMP-05 이후, 측정 먼저 |
| IMP-12 | GC 가이드라인 확정 및 AGENTS.md 반영 | 초안 작성됨, 사용자 검토 대기 | 검토 |
| IMP-13 | UI(`Assets/TPLab/UI`) 구조·할당·테스트 점검 | 제안 | IMP-12 확정 뒤, 읽기 전용 점검 먼저 |

## 결정 기록 (사용자가 작성)

| ID | 질문 | 결정 |
|---|---|---|
| D-01 | IMP-01: 파괴된 대여 클론의 슬롯 회수 방식 | **확정: A1(지연 점검)** |
| D-02 | IMP-02: Dispose 후 Return 처리 | **확정: (b) `TryReturn` 추가, `Return`은 현행 유지** |
| D-03 | IMP-03: 종료 대기 초과 시 동작 | **확정: 예외 발생 + 타임아웃 값 설정 가능** |
| D-03a | 타임아웃 기본값 | **확정: 60초**(0 이하 = 무제한, 현행 동작) |
| D-03b | 타임아웃 적용 범위 | **확정: Prepare 취소 대기 + 각 `ReleaseAsync` 대기 모두** |
| D-03c | 설정 위치 | **확정: `SceneOwnedRoot`/`SingletonSceneRoot` 호스트**(`Configure`, `SceneRootSetup.Attach`, 직렬화 필드) |
| D-04 | IMP-05/11: GC 최적화 방향 | **확정: 추가 후보 탐색 + 로직 개선 + 가이드라인 초안. LINQ는 "반드시 써야 하는 곳인지" 판단해서 쓴다(가이드라인 6항에 판단 절차 반영)** |
| D-04b | `OwnedScenes`/`RegisteredScenes`를 캐시할 것인가 | **확정: 캐시하지 않고 버퍼 채우기 API 추가, 기존 프로퍼티는 유지** |
| D-05a | IMP-04: 적용 범위 | **확정: 게이트 테스트 중 테스트 자체 보호가 없는 PlayMode 24개 메서드**(EditMode `SceneLoaderTests` 2개는 EditMode 실험 전까지 제외, 이미 보호가 있는 테스트는 변경하지 않음) |
| D-05b | IMP-04: 적용 방식 | **확정: 혼합** - 기존 헬퍼·조건 대기는 그대로 두고 선택한 메서드에만 `[Timeout]` 추가(헬퍼를 `.Timeout()`으로 치환하지 않음) |
| D-05c | IMP-04: 값 | **확정: 30,000ms**. 60초는 네이티브 다단계 메서드의 실제 소요 시간 측정으로 필요성이 확인될 때만 별도 사용자 결정으로 추가 |

---

## IMP-01. PrefabPool: 대여 중 클론이 외부에서 파괴되면 슬롯이 회수되지 않음

**상태**: **승인·완료(D-01 = A1)**. 승인일 **2026-10-12**, **PR #7 사용자 병합 승인**. 구현·실제 Red/Green과 경계·회귀 검증은 [검증 기록](validation/imp-01/README.md)을 따른다. 실제 main 통합은 PR/Git 이력을 확인하며 A2/B/C는 채택하지 않는다.

**증거**
- `Assets/TPLab/Core/Pooling/PrefabPool.cs:92-100` `Return`: `instance == null`(파괴된 Unity 객체 포함)이면 `_pool.Return` 전에 `ArgumentNullException`을 던진다.
- `Assets/TPLab/Core/Pooling/ObjectPool.cs:19` `_owned`는 파괴된 참조를 계속 보유하고, `:29` `CountRented = _owned.Count - _inactive.Count`, `:86` 용량 검사는 `_owned.Count >= Capacity`.
- 결과(추정): 대여 중 클론이 소비자 코드나 클론 자신의 `Destroy(gameObject)`로 파괴되면 반납 경로가 없고, `Dispose` 전까지 용량이 줄어든 채 유지된다.

**유지해야 할 기존 테스트** (`Assets/TPLab/Tests/PlayMode/PrefabPoolTests.cs`)
`RentHookDestroyingInstanceIsReportedAndCapacityRecovered`, `ReturnHookDestroyingInstanceIsReportedAndCapacityRecovered`, `DestroyedParentRejectsOperationsButStillAllowsDisposal`, `ReturnRejectsNull`, `DisposedPoolRejectsRentAndReturn`

**Red 테스트 스펙** — `PrefabPoolTests`에 추가, `[UnityTest]`
- 이름: `RentedCloneDestroyedByConsumerFreesItsSlot`
- 준비: `var pool = CreatePool(1);` (`CreatePool(int capacity = 1, Action<GameObject> onRent = null, Action<GameObject> onReturn = null)` 재사용)
- 행동: `pool.TryRent(out var clone)` → `UnityEngine.Object.Destroy(clone)` → `yield return null`
- 기대: `pool.TryRent(out _)`가 `true`
- 수정 전 예상: 실패(`TryRent`가 `false`). 실제 실패 메시지를 보고한다.

**배경**: Unity에서는 외부 코드의 `Destroy` 호출을 막을 방법이 없다. 풀은 "정상 경로(`Return`)를 제공하고, 예외 경로(외부 파괴)를 감지·회수"할 수 있을 뿐이다. 감지에 별도 MonoBehaviour가 반드시 필요하지는 않다. 파괴된 Unity 객체는 `== null`이 `true`이므로 생존 판정만 있으면 된다.

**선택지**
- **A1 (채택) 지연 점검**: `ObjectPool<T>`에 선택 인자 `Func<T, bool> isAlive`를 추가한다. 용량이 소진되어 `TryRent`가 `false`를 돌려주기 직전에 대여·비활성 항목 중 죽은 것을 정리하고 한 번 더 시도한다. `PrefabPool`은 `go => go != null`을 넘긴다. 추가 컴포넌트 없음, 정상 경로 비용 0, `ObjectPool<T>`는 Unity 비의존 유지. 한계: 정리 전까지 `CountRented`/`CountOwned`가 실제보다 클 수 있다(필요하면 공개 `Prune()` 또는 getter 정리를 별도 결정). 구현 주의: `Dictionary` 열거 중 수정 불가이므로 재사용 scratch 리스트를 쓰고(할당 가이드라인), `Stack`에 죽은 항목이 있으면 `Pop` 후 재검사한다.
- **A2 마커 컴포넌트**: 클론마다 작은 MonoBehaviour를 자동 부착하고 `OnDestroy`에서 풀에 통지한다. 즉시 정확하지만 모든 클론에 컴포넌트가 늘고, 풀 자신의 `Destroy`(Dispose 중) 때 재진입 억제와 테스트가 추가로 필요하다. 복잡도가 높아 A1로 부족할 때만 검토한다.
- **B 명시적 API**: 소비자가 "파괴했다"고 풀에 알린다. 잊으면 누수라서 "시스템이 책임진다"는 목표와 반대다. 비권장.
- **C 문서화만**: "대여 클론을 Destroy하지 말 것". 회복 수단이 없다. 비권장.

**제약**: `ObjectPool<T>`가 UnityEngine에 의존하게 만들지 않는다. 공개 API가 바뀌면 `doc/api/Pooling.md`, `doc/ai/api/Pooling.md`와 XML 주석을 같은 단위에서 갱신한다.

---

## IMP-02. Dispose 후 Return이 예외를 던지는 계약

**상태**: **승인·완료(D-02 = (b))**. 승인일 **2026-10-13**, **PR #8 사용자 병합 승인**. `Return`과 아래 두 기존 테스트(`DisposedPoolRejectsRentAndReturn`, `ObjectPoolTests.cs:274-275`)는 수정하지 않고, `TryReturn`만 추가한다. 실제 Red/Green·자동 검증과 사용자 확인을 [검증 기록](validation/imp-02/README.md)에서 구분한다. (a), (c)는 채택하지 않는다.

**증거**
- `ObjectPool.cs:201-212` `BeginOperation`: 폐기된 풀이면 `ObjectDisposedException`. `Return`(`:124`)도 같은 경로를 쓴다. `PrefabPool.cs:179-186` `EnsureOpenContext`도 폐기 여부를 먼저 검사한다.
- 현행 계약을 고정하는 테스트: `PrefabPoolTests.DisposedPoolRejectsRentAndReturn`(`:242`), `ObjectPoolTests.cs:274-275`.

**문제가 되는 상황 (모두 추정, 실행 미확인)**
1. **풀 해제 중 소비자 정리**: `PrefabPool.Dispose`는 `IsDisposed`를 먼저 `true`로 만든 뒤 대여 중인 클론까지 파괴한다. `DestroyInstance`(`:204-223`)가 `SetActive(false)`를 즉시 호출하므로, 소비자 스크립트가 `OnDisable`/`OnDestroy`에서 `pool.Return`을 호출하는 구조라면 이미 폐기된 풀에 호출되어 `ObjectDisposedException`이 난다. Unity 콜백 안이라 앱이 죽지는 않고 Console 에러와 해당 콜백의 나머지 정리 코드 중단이 생긴다.
2. **늦은 비동기 반납**: `await UniTask.Delay(...)` 뒤 `pool.Return(bullet)`처럼, 씬 전환이나 풀 소유자 종료 후에 완료되는 작업이 반납하는 경우. 예외가 연속 구간에서 발생해 로그로만 남는다.
3. **진짜 버그**: 잘못된 풀에 반납, 이중 반납. 이 경우에는 예외가 오히려 유용하며 어떤 선택지에서도 유지해야 한다.

폐기된 풀에 대한 반납을 무시해도 자원 누수는 없다. `Dispose`가 이미 모든 인스턴스에 `_onDestroy`를 호출했기 때문이다. 위험은 버그가 가려지는 것뿐이다.

**선택지**
- (a) 현행 유지 + 가이드("풀 소유자가 소비자를 먼저 멈춘 뒤 Dispose", 콜백에서 `IsDisposed` 확인). 계약이 단순하고 테스트도 그대로지만, 소비자마다 방어 코드가 필요하다.
- **(b) 채택**: `TryReturn(instance)` 추가. 풀이 폐기되었으면 `false`를 반환하고, null·타 풀 인스턴스·이중 반납은 `Return`과 동일하게 예외를 던진다. `Return`은 그대로이므로 기존 테스트 변경이 없다. 종료 경합이 있는 소비자만 `TryReturn`을 쓴다.
- (c) `Return` 자체를 폐기 후 무시: 위 3번 버그까지 가려지고 기존 테스트 2개를 고쳐야 한다. 비권장.

---

## IMP-03. 토큰을 무시하는 installer가 있으면 ShutdownAsync가 끝나지 않을 수 있음

**상태**: **설계 확정(D-03, D-03a~c)**: 예외 발생 + 설정 가능한 타임아웃(기본 60초), Prepare 대기와 각 `ReleaseAsync` 대기에 모두 적용, 설정은 root 호스트. 구현 가능. 비동기 수명·소유권 위험: **high**.

**증거**
- `Assets/TPLab/Core/Lifecycle/RootInstallation.cs:81-96` `ShutdownAsync`, `:98-147` `ShutdownOwnedAsync`. `:103`에서 취소를 요청한 뒤 `:113`에서 `await _preparation.Task`를 타임아웃 없이 기다린다. 이어지는 `:120-130`의 `installer.ReleaseAsync(_root)` 대기에도 타임아웃이 없다.
- 파괴 경로 `Dispose`(`:216-261`)는 `:236`에서 `_preparation?.TrySetCanceled()`로 이 문제를 피한다. 정상 종료 경로에는 같은 보호가 없다(추정).
- 호스트: `SceneOwnedRoot.cs`, `SingletonSceneRoot.cs`는 둘 다 `[SerializeField] _installers`, `_persistAcrossScenes`와 `Configure(SceneRootInstaller[], bool)`를 가지며 `new RootInstallation(this, _installers)`로 만든다. `SceneRootSetup.Attach`가 두 호스트를 구성한다.
- 기존 테스트 `AsyncSceneRootTests.ShutdownCancelsPreparationAndCannotRunItAgain`(`:360`)은 협조적 installer만 다룬다. 비협조 설정(`SceneRootInstallerProbe.IgnorePrepareCancellation`)은 파괴 경로 테스트에서만 쓰인다.

**설계 (확정)**
- 설정(D-03c): 호스트에 `[SerializeField] float _shutdownTimeoutSeconds`, `Configure(..., float shutdownTimeoutSeconds = 60f)`, `SceneRootSetup.Attach(..., shutdownTimeoutSeconds)`. `RootInstallation` 생성자로 전달한다. `ISceneRoot` 인터페이스는 바꾸지 않는다. 0 이하는 무제한(이전 동작).
- 기본값(D-03a): **60초**. 직렬화 필드의 기존 씬·프리팹 값은 0(무제한)으로 역직렬화될 수 있으므로, 필드 기본 초기화(`= 60f`)가 기존 자산에 적용되는지 구현 시 확인하고 결과를 보고한다(AGENTS.md: 직렬화 호환성 확인).
- 범위(D-03b): Prepare 취소 대기(`:113`)와 각 `ReleaseAsync` 대기(`:124`) 모두. 단계마다 같은 값을 따로 적용한다.
- 시간 기준: unscaled time(`Time.timeScale == 0`에서도 흐름). UniTask의 Timeout 계열 API 사용 가능 여부와 시그니처는 구현 시 설치된 UniTask 2.5.11에서 확인한다.
- 초과 시 동작: `TimeoutException`을 `failures`에 추가하고, **남은 해제(역순 `ReleaseAsync`)와 `Dispose`는 계속 수행**한 뒤 마지막에 `AggregateException`으로 `ShutdownAsync`를 실패시킨다. 정리를 건너뛰면 자원이 누수되고, 이 저장소의 기존 원칙(실패해도 나머지 정리를 계속)과 어긋난다. 예외 메시지에 시간 초과한 installer 타입 이름과 단계(Prepare/Release)를 넣는다.
- 상위 영향: `SceneRootFlow`는 `ShutdownAsync` 실패를 기존 해제 실패와 같은 경로(cover 유지, 실패 보고)로 처리하므로 `GameSceneManager.LastFailure`에 기록된다. 기존 테스트 `ReleaseFailureStillCleansOtherSystemsAndKeepsCover`와 같은 동작 계열이다.
- 알려진 위험: 시간 초과 시점에 비협조 `PrepareAsync`가 아직 실행 중일 수 있다. 늦은 완료는 `PrepareOwnedAsync`의 `_disposed || _stopping` 검사로 `IsPrepared`를 바꾸지 못하지만, installer 자신의 코드가 자원을 계속 만질 수 있다. 계약 위반이므로 예외로 보고하고 XML 주석과 문서에 "PrepareAsync는 토큰을 따라야 한다"를 명시한다.

**재현/검증 테스트 스펙** — `AsyncSceneRootTests`에 추가, `[UnityTest]` + `[Timeout(...)]` 필수
- (1) Red 재현: `ShutdownWithUncooperativePreparationNeverCompletesWithoutTimeout` — 기본값 무제한 상태로는 완료되지 않음을 짧은 관찰 시간 후 확인하고 게이트를 열어 정리한다. 구현 후에는 (2)로 대체한다.
- (2) 구현 후: `ShutdownWithUncooperativePreparationFailsWithTimeoutAndStillCleansUp` — `Create()`를 확장해 짧은 타임아웃(약 0.1초)을 구성한다. 준비: `_first.PrepareGate = new UniTaskCompletionSource(); _first.IgnorePrepareCancellation = true;` 행동: `_root.PrepareAsync()` 시작 후 `await Wait(_root.ShutdownAsync().AsTask())`. 기대: 종료가 실패하고 예외 트리에 `TimeoutException`(installer 이름 포함)이 있으며 `_first.UninstallCount == 1`, `_first.ReleaseCount == 1`, `_root.IsPrepared == false`, `_second.PrepareCount == 0`. 마지막에 게이트를 반드시 연다.
- (3) (D-03b 확정으로 필수) `ReleaseGate`를 열지 않은 installer에서 같은 방식으로 `TimeoutException`이 나오고 다음 installer의 해제와 `Dispose`가 계속되는지 확인한다.

**주의**: 이 테스트는 Editor를 멈출 수 있다. `[Timeout]` 없이 실행하지 않는다. 테스트 실행 중에는 소스를 변경하지 않는다(AGENTS.md).

---

## IMP-04. 테스트 타임아웃 도입

**상태**: **승인·완료**. 승인일 **2026-10-12**, **PR #6 병합 완료**. 선택한 PlayMode24개에 `[Timeout(30000)]` 적용을 완료했다. [조사](validation/imp-04-survey/README.md)와 [적용 검증](validation/imp-04-apply/README.md)을 구분한다.

**증거**
- `Assets/TPLab/Tests`, `Assets/TPLab/Input/Tests`에서 `[Timeout]`은 0건이다. 다만 **1단계 조사에서 확인: Test Framework 1.6.0은 속성이 없어도 기본 180초 제한을 둔다**(설치 소스 기준, 180초 만료 실험은 미실행). 무제한이 아니다. 실제 틈은 `tools/run_unity_tests.py`의 응답 대기 한도(180초)가 프레임워크 결과보다 먼저 닿을 수 있다는 점이고, `[Timeout]`의 목적은 이보다 짧게 빨리 실패시키는 것이다. 조사 보고서: `doc/validation/imp-04-survey/README.md`.
- 대기 헬퍼 `Wait(Task)`는 `AsyncSceneRootTests.cs:53`에 `private static async UniTask Wait(Task task)`로 정의되어 있다. 다른 파일의 유사 대기 패턴은 조사가 필요하다.

**조사 지시**: (1) 게이트(`UniTaskCompletionSource`)를 기다리는 테스트 목록, (2) 대기 헬퍼의 정의 위치와 타임아웃 유무, (3) Test Framework 1.6.0에서 `[Timeout]`이 `[UnityTest]`에도 실제로 적용되는지(작은 실험으로 확인, 결과를 증거로 남김), (4) 적용 방식 후보(헬퍼 vs 개별 속성)를 정리해 보고한다. 범위는 사용자가 정한 뒤 변경한다.

**제약**: 테스트의 검증 의미를 바꾸지 않는다. 타임아웃은 정상 실행 시간의 충분한 배수로 잡는다.

---

## IMP-05. GameSceneManager 폴링 속성의 반복 할당

**상태**: **진행·계측 확인 대기(D-04, D-04b)**. 2026-10-13 예상 Red가 pass1/1이며 유휴 Editor의 4KB 양성 대조도0이라 자동 구현을 중단했다. [중간 보고](validation/imp-05/README.md). 버퍼 API의 이름·시그니처는 계측/Red를 해소한 뒤 구현 전에 사용자에게 한 번 보고하고 확인받는다.

**증거** (`Assets/TPLab/Core/SceneManagement/GameSceneManager.cs`)
- `:69` `OwnedScenes`(LINQ `Select/Where/ToArray`), `:72` `RegisteredScenes`(LINQ `Select/ToArray`)
- `:78-83` `CanProceed`: `GetRegisteredOwners().All(IsOwnerPrepared)`, `InventoryMatches(ExpectedGameScenes())`
- `:998-1007` `GetOwners`/`GetRegisteredOwners`는 `yield return` 이터레이터라 호출마다 객체가 생긴다. `:996` `ExpectedGameScenes`(`Concat/Select/ToArray`), `:1120` `InventoryMatches`(`new HashSet<Scene>`), `:1200` `GetLoadedScenes`(`Enumerable.Range` 기반 LINQ). `All(IsOwnerPrepared)`의 메서드 그룹은 C# 9에서 호출마다 델리게이트가 생긴다.
- 사실 확인: Core 내부에서 `CanProceed`는 전환 요청당 한 번(`:373`)만 읽는다. 프레임당 비용은 **소비자가 `Update` 등에서 폴링할 때** 생긴다. 공개 프로퍼티는 폴링될 수 있으므로 할당이 없어야 한다(가이드라인 3항).

**Red 테스트 스펙** — `GameSceneEntryTests`(PlayMode)에 추가
- 이름: `CanProceedDoesNotAllocateInReadyState`
- 준비: 기존 테스트의 진입 방식을 재사용해 `State == Ready`까지 진행 (`GameSceneEntryTests.cs:78`의 `new GameSceneManager((MonoBehaviour)_root, _callbacks)` 패턴)
- 행동: `_ = manager.CanProceed;` 1회 워밍업 후 `GC.GetAllocatedBytesForCurrentThread()` 전후 100회 호출 차이 측정
- 기대: 차이 0
- 수정 전 예상: 실패. Editor 측정 노이즈로 0이 안정적이지 않으면 임계값을 임의로 정하지 말고 보고한다.

**수정 방향** (의미를 바꾸지 않는다)
- `CanProceed`: LINQ·이터레이터·`HashSet` 대신 `_primary`/`_derived`(`List<OwnedPrimary>`) 인덱스 루프로 직접 순회한다. `InventoryMatches`는 `SceneManager.sceneCount`와 `GetSceneAt(i)`를 순회하며 기대 집합(`_retainedScenes` + 등록·후보 owner의 씬)에 속하는지 중첩 루프로 확인한다(원래 `SetEquals`와 같이 양방향 포함 검사, `isLoaded`인 씬만 대상). 씬 수가 작아 O(n·m)이 문제되지 않는다. `ExpectedGameScenes()` 배열 생성은 제거한다.
- `OwnedScenes`/`RegisteredScenes`: **기존 프로퍼티는 유지**(스냅샷 의미, 진단용으로 XML에 "할당 발생" 명시). 할당 없는 대안으로 `int OwnedSceneCount`, `void GetOwnedScenes(List<Scene> buffer)`, `void GetRegisteredScenes(List<SceneRegistration> buffer)` 같은 버퍼 채우기 API를 추가한다(D-04b 확정). 캐시는 하지 않는다. `OwnedScenes`의 `isLoaded` 필터가 코드 밖(Unity)에서 바뀔 수 있어 무효화 조건을 정할 수 없다. 버퍼 메서드는 내부에서 `Clear()` 후 채우고, 채워진 내용은 호출 시점의 스냅샷임을 XML 주석에 명시한다. 이름과 시그니처는 위 제안을 기본으로 하되 구현 전에 사용자에게 보고한다.
- 같은 변경에 기존 `CanProceed` 관련 PlayMode 테스트(`GameSceneEntryTests` 등) 재실행으로 의미 보존을 확인한다.

**제약**: `GameSceneManager`는 1209줄의 큰 클래스다. 이 항목에서 분리·구조 변경을 하지 않는다(IMP-10).

---

## IMP-06. 시간 의존 테스트 제거

**상태**: 제안(낮은 우선순위).

**증거**: `Assets/TPLab/Tests/EditMode/DataTableImporterTests.cs:180` `System.Threading.Thread.Sleep(75)`. 파일 핸들 해제 타이밍을 시뮬레이션하는 유일한 시간 의존 테스트다.

**지시**: `DataTableGeneratedFiles.Apply`의 재시도 대기 구간을 먼저 읽고, 프로덕션 재시도 로직은 바꾸지 않은 채 테스트만 결정적으로 만들 수 있는지 검토해 보고한다.

---

## IMP-07. CI 구성

**상태**: 일부 완료. 남은 범위는 사용자 승인 필요(프로젝트 설정·인프라 변경).

**현재 상태**(개정 시점): `.github/workflows/rule-check.yml`(PR diff 규칙 검사)과 `claude-review.yml`(`@claude` 호출 시 리뷰)이 main에 있고, `tools/check_github_ci.py`는 정확한 SHA의 결과를 판정하도록 바뀌었다([CI_SETUP_GUIDE.md](CI_SETUP_GUIDE.md)). 이 PR 검사는 Unity 테스트를 실행하지 않는다.

**남은 범위**: Unity 테스트 실행 CI(GameCI 등)는 보류 상태다. 라이선스 방식(Personal 활성화 가능 여부)과 InputSystem·UGUI 테스트 포함 여부를 정해야 한다. 이 항목을 시작하면 `check_github_ci.py`의 해석과 병합 정책(AGENTS.md "검증 후 병합")을 함께 검토한다. 승인 전에는 아무것도 추가하지 않는다.

**참고**: `tools/README.md` 14행에 "CI 미구성" 전제의 과거 설명이 남아 있다. 정정은 별도 문서 정리에서 한다.

---

## 후속 항목 (아직 스펙 없음, IMP-01~05 이후)

- **IMP-08**: 중복 보일러플레이트 정리. `EnsureMainThread`(`PlayerLoopHelper.IsMainThread` 검사)가 `ResourceManager.cs:236-242`, `DataTableManager.cs:444-450`, `GameSceneManager.cs:1203`, `LoadedScene.cs:66` **4곳**에 중복되어 있고, 취소 토큰 + `IsDisposed` + 드레인 대기 패턴이 반복된다. 공통 헬퍼 도입 여부는 사용자 결정이 필요하다.
- **IMP-09**: DataTable 파싱의 프레임 분산. `DataTableManager.cs:362-373` 로드 루프와 `:398-412` `ReadSourceAsync`. 소스가 이미 메인 스레드에서 완료되면 `UniTask.SwitchToMainThread()`가 양보 없이 반환되어 여러 테이블이 한 프레임에 파싱될 수 있다(UniTask 동작에 대한 추정, 프로파일링 미수행). 실제 히치를 측정한 뒤 결정한다.
- **IMP-10**: `GameSceneManager` 분리. 후보: 씬 소유 트리(`OwnedPrimary` 관리) / 정책 평가(`PolicySnapshot`) / 오퍼레이션 실행기. 관련 테스트(약 100개)가 안전망이다. 위 항목들이 끝나고 별도 설계 검토를 거친 뒤에 진행한다.

---

## IMP-11. 할당 감사 후속 (전환·이벤트 경로, P1)

**상태**: 제안. IMP-05 이후, **측정 먼저**. 아래는 코드 읽기 기준 후보이며 프로파일링은 하지 않았다. 빈도가 낮은 경로라 현재는 허용 범위이고, 측정에서 문제가 확인될 때만 고친다.

| 위치 | 내용 | 빈도 | 메모 |
|---|---|---|---|
| `GameSceneManager.cs:960` `ValidateCommonLifetime` | persistent일 때 호출마다 `GetLoadedScenes()` 배열 생성. 한 전환에서 약 15곳이 호출 | 전환당 다수 | 단계 사이 한 번만 계산해 재사용 가능한지 검토 |
| `GameSceneManager.cs:979` `ValidatePreparedOwnership` | `Any(람다)`, `ExpectedGameScenes()`, `HashSet` | 전환당 다수 | IMP-05의 비할당 `InventoryMatches` 재사용 |
| `GameSceneManager.cs:1196` `GetRootHosts` | `GetRootGameObjects()` + 씬 전체 `GetComponentsInChildren<MonoBehaviour>(true)` | 로드당 1회(`:618`) + 정리 시(`:801`) | 할당보다 CPU(씬 크기에 비례). 중첩 root 검출 의미 때문에 바꾸기 전 측정과 사용자 확인 필요 |
| `Input/Runtime/InputLayerController.cs:308` `CalculateSnapshot`, `:337` `SameState` | 정렬 LINQ, `HashSet`, `List` 2개, `SequenceEqual`(인터페이스 열거) | 레이어 획득·해제마다 | UI 열기/닫기 빈도. 수정한다면 가이드라인 적용 |
| `ResourceManager.cs:101` | 캐시 적중 경로에서도 `AttachExternalCancellation`을 두 번 거침 | 에셋 요청마다 | 취소 등록 객체 할당 여부는 추정. 측정 필요 |

**확인해서 깨끗하다고 본 곳** (코드 읽기 기준, 측정 아님)
- `ObjectPool`/`PrefabPool`의 정상 상태 `TryRent`/`Return`(콜백과 용량 확장 제외)
- `DataTableSnapshot.Get`/`TryGet`과 `DecimalIdxCodec`(구조체, 예외 문자열은 실패 경로뿐)
- `MonoSingleton<T>.Instance`

**이번에 정밀 확인하지 않은 곳**: `InputManager.GetAction`, `InputRebindingController`의 프레임 경로, `SceneTransitionCallbacks` 호출 경로.

---

## IMP-12. GC 가이드라인 확정

**상태**: 초안 작성됨([GC_ALLOCATION_GUIDELINE.md](GC_ALLOCATION_GUIDELINE.md)). 사용자 검토 후 확정하고, AGENTS.md에는 요약 규칙(초안 11항)만 반영한다. 에이전트는 사용자 확정 전에 AGENTS.md를 수정하지 않는다.

---

## 항목별 완료 보고 형식

AGENTS.md의 완료 보고 규칙에 더해 다음을 포함한다.

- 수행한 항목 ID와 상태 변화(제안 → 진행 → 자동 검증 완료 등)
- Red 확인: 실행 명령, 실패한 테스트 이름과 **실패 메시지 원문**(수정 전 예상과의 일치 여부)
- Green 확인: 대상 테스트와 전체 EditMode/PlayMode의 실행 수·실패·skip
- 변경 파일 목록과, 테스트 파일을 변경했는지 여부(수정 단계에서는 없어야 함)
- 갱신한 XML·사람/AI 문서
- 미실행 영역과 남은 위험

---

## IMP-13. UI 구조·할당·테스트 점검

**상태**: 제안. **IMP-12(GC 가이드라인 확정) 이후**, 읽기 전용 점검부터 시작한다.

**범위**: `Assets/TPLab/UI`(Runtime, InputSystem, Tests)와 `Samples/UI`. 기존 [UI_PERFORMANCE.md](UI_PERFORMANCE.md) 측정 기록을 기준선으로 삼고 새 측정은 하지 않는다.

**점검 후보**(코드 읽기 기준 추정, 측정 아님): `UIContext`의 호출마다 생성되는 `List`와 `ToArray()`, `GetInvocationList()`, 포커스 계산의 `GetComponentsInChildren<Selectable>()`, 매 프레임 경로인 `VirtualScrollRect.LateUpdate`와 `UIInputSystemAdapter.Update`. `UIContext`(약 1,900줄 규모의 partial 클래스) 분할 필요성. UI 테스트의 타임아웃 보호(IMP-04에서 제외된 범위).

**지시**: 수정하지 않고 위 후보를 호출 빈도와 함께 보고한다. 수정은 보고 후 사용자가 항목을 정해 요청할 때만 한다.
