# 2026-10-14 · 09 · Guard 특성화 안전망 A

현재 상태: 원인 분리·신규 UI fixture 격리 정정·전체 회귀 후 사용자 승인으로 **Play369/369 반복 및 정정 후 fixture 기준11변형 재검증**을 완료했다. 자동 검증 완료·commit/push/PR 제공 단계이며 main 병합은 하지 않는다. source/최종 HEAD metadata는 말미의 PR 제공 단계 기록을 따른다. 최신 증거는 아래 [후속 원인 분리와 fixture 격리](#후속-원인-분리와-fixture-격리2026-10-14)에 둔다. 최초 실패·중단·정정 원문은 과거 실행 증거로 보존하며 통과값으로 덮지 않는다.

## 최초 작업과 중단 기록(2026-10-14)


T2 테스트 전용이다. PR #22 조건부 병합 후 최신 main에서 `codex/guard-safetynet-a`를 만들고 **전체 기준선 → 정상본11 case → 단일 임시 변형·원복 재통과 → 동결 후 전체 회귀**를 수행했다. 새 cs5개/.meta5개, 이 회고와 회고 INDEX만 이번 변경이다. **전체 PlayMode6건 실패로 중단**했다. commit/push/신규 PR/exact-SHA Actions는 진행하지 않았으며 기대값·runtime·기존 테스트를 고치지 않았다.

## 선행 병합과 source

PR #22 HEAD `03b19f7598415f10edc4ebd965bac40841af923b`, 당시 main `f54554a6c7ff1b06e73dcc3258d8b4170d26ddfa`, HEAD의 [Actions success](https://github.com/KangSungKyu/TPLab/actions/runs/38518156820), runtime diff0을 모두 재확인했다. 일반 merge commit·이번 runtime source는 `1dffd1e7ce286ced9dac6823ef8864dc3ee3f029`이다. local main·origin/main·서버 main 일치와 source tree 보존을 확인했다. `codex/gamescene-safetynet-a`는 tip 일치·main 도달성·다른 worktree 미사용 확인 뒤 remote expected-tip 조건부 삭제 및 local `git branch -d`로 정리했다. 원본 tip은 main 이력에 남는다.

이번 Unity 검증 source는 위 runtime base + 동결 신규 테스트 working tree다. **검증 source commit과 최종 문서/CI metadata는 말미에서 구분**한다. 테스트 후 문서만 수정한 것을 새 runtime 검증으로 주장하지 않는다.

## 확정 case·관찰 경계

기존 Core/Input EditMode·PlayMode assembly를 사용해11 case(Edit4·Play7)를 확정했다. `TPLab.Input.PlayModeTests.asmdef:6`이 이미 `TPLab.SceneTransitionSamples`를 참조하고 기존 InputIntegrationTests:97–121도 adapter를 사용하므로 sample2 case를 포함했다. 제외0, 새 assembly·참조·package·fixture asset0이다.

| 분류·새 파일 | 테스트 이름 | case | 현재 동작·보존 단언 |
|---|---|---:|---|
| [DataTable](../../Assets/TPLab/Tests/EditMode/DataTableManagerGuardSafetyNetTests.cs#L19) | LoadAsync_WorkerThreadPrecedesOwnerState(Open/Disposed) | 2 | exact IOE 메시지·thread→disposed, snapshot/loading/disposed 불변, 이후 main 인자 검증·등록·해제 |
| [Observer](../../Assets/TPLab/Tests/EditMode/SceneLoadProgressObserverGuardSafetyNetTests.cs#L16) | Report_WorkerThreadPrecedesNotificationSuppression | 1 | worker→callback/suppression 이전 거부, 보고 기록·최초 Failure 불변, 이후 main 보고; disposed/failed 경계 포함 |
| [Resource](../../Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs#L170) | LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Open/Disposed) | 2 | exact IOE 메시지·thread→disposed/key, initialized/disposed/native inventory 불변, main 인자/owner 검증·중복 종료 |
| [LoadedScene ctor](../../Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs#L220) | LoadedScene_ConstructorWorkerPrecedesArgumentValidation | 1 | thread→target/native 검증, native 불변, fresh main 성공 대조 |
| [LoadedScene start](../../Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs#L234) | LoadedScene_UnloadWorkerRejectsBeforeDelegateAndCachedResult | 1 | backend0·owner/native 불변, main unload 성공, 완료 뒤 worker도 cached 반환 전에 거부 |
| [LoadedScene await](../../Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs#L275) | LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry | 1 | post-await exact 거부, 동일 공유 예외·IsUnloaded=false·retry0, fresh main 성공 대조 |
| [InputManager](../../Assets/TPLab/Input/Tests/EditMode/InputManagerGuardSafetyNetTests.cs#L13) | WorkerLeaseDisposePreservesCreatingThreadMessageAndLease | 1 | creating-ID exact 거부, snapshot/clone/map/event 불변, 동일 lease 미소비·main release·중복 release |
| [UI bound](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs#L136) | BoundWorkerCallsPreserveBindingThreadMessageAndLease | 1 | binding-ID exact Refresh/Unbind 거부, native/owner/lease/event 불변, main unbind/rebind/release |
| [UI unbound](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs#L175) | UnboundWorkerCallsReturnBeforeBindingThreadValidation | 1 | Bind 이전 Refresh/Unbind의 thread 검사 전 반환, 오류·변이 없음, 이후 main bind/release |

Core는 UniTask `PlayerLoopHelper.IsMainThread`, InputManager는 생성 시 ID, InputSystemUiScope는 Bind 시 ID를 그대로 관찰한다. 메시지/predicate를 동일 계약으로 정규화하지 않았다. 실제 guard 근거는 [DataTable:435/444](../../Assets/TPLab/Core/DataTables/DataTableManager.cs#L435), [Resource:227/236](../../Assets/TPLab/Core/ResourceManagement/ResourceManager.cs#L227), [LoadedScene:26/40/54/66](../../Assets/TPLab/Core/ResourceManagement/LoadedScene.cs#L26), [Observer:55](../../Assets/TPLab/Core/ResourceManagement/SceneLoadProgress.cs#L55), [Input:171](../../Assets/TPLab/Input/Runtime/InputManager.cs#L171), [UI:265](../../Assets/TPLab/Samples/Input/SceneTransitions/Runtime/InputSystemUiScope.cs#L265)이다.

LoadedScene post-await은 native scene 생성/해제를 main에서 한 뒤 공개 UniTaskCompletionSource를 worker에서 완료하는 backend로 결정적으로 재현했다. backend 재개 ID=worker ID≠main ID를 단언한다. gate.TrySetResult는 성공하고 오류는 gate throw가 아닌 **공유 Unload result의 fault**다. 최초/동시/이후 caller가 같은 예외 객체를 보고 backend1회·IsUnloaded=false·native inventory 불변을 유지하며 fresh main owner의 정상 unload도 확인한다. private 상태 변경이나 race 추정으로 대체하지 않았다.

Data/Resource main 재사용 대조는 인자/owner 검증·등록/종료까지다. 실제 CSV publication 또는 Addressables asset 재로드를 새 case가 증명하지 않는다. Resource는 invalid key로 mutant에서도 native Addressables 초기화를 시작하지 않는다. UI host inactive/module disabled는 native OnEnable/LateUpdate가 snapshot을 바꾸지 않게 한다. native는 main에서 읽고 callback은 기록만 한다. callback에서 NUnit 단언을 삼키는 oracle을 만들지 않았다.

runtime·기존 테스트·asmdef·package·설정·UPM diff0. 공개 runtime 계약 불변이므로 XML·doc/api·doc/ai/api 갱신은 비적용이다. 새 GUID5개는 Assets 전수 대조에서 각각 새 .meta 한 곳뿐이다.

## 환경·실행 명령·정상본

Windows / Unity6000.3.18f1 / UTF1.6.0 / UniTask2.5.11 / Connector0.4.1 / 기존 Editor PID27224 / C:\Python314\python.exe를 사용했다. CLI에는 항상 절대 TPLab 경로를 지정했다. 새 Editor·이전/stale 결과·다른 프로젝트로 대체하지 않았다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/guard-safetynet-a-20261014/baseline-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/guard-safetynet-a-20261014/baseline-play.json
```

Targeted는 같은 명령에 `--filter <아래 fixture>`와 고유 output을 지정했고 final은 mode별 `final-edit.json`/`final-play.json`이다. runner는 이번 CLI PID·fresh timestamp·nonzero 결과를 확인한다. 시작 전 discovery 거부만 같은 Editor를 재조회하며 실패 결과를 숨기기 위한 재실행은 하지 않는다.

| 정상 fixture filter | mode | 통과/실패/skip | CLI PID·source |
|---|---|---|---|
| `TPLab.Core.Tests.DataTableManagerGuardSafetyNetTests` | EditMode | 2/0/0 | 35104 · cli |
| `TPLab.Core.Tests.SceneLoadProgressObserverGuardSafetyNetTests` | EditMode | 1/0/0 | 27916 · cli |
| `TPLab.Core.Tests.ResourceGuardSafetyNetTests` | PlayMode | 5/0/0 | 41816 · cli |
| `TPLab.Core.Input.Tests.InputManagerGuardSafetyNetTests` | EditMode | 1/0/0 | 42420 · cli |
| `TPLab.Core.Input.Tests.InputSystemUiScopeGuardSafetyNetTests` | PlayMode | 2/0/0 | 19668 · connector-file |

Sample 정상 CLI는 domain reload 뒤 nonzero exit였지만 공식 runner가 **해당 PID19668의 fresh Connector result(runId19668-1791958837570001400), 2pass**를 회수했다. 다른 실행/stale 결과 대체가 아니다.

## 정상본 실패·조건부 승인 정정 이력

실행 전 Input fixture AddAction named argument `expectedControlType` 두 곳이 설치 API와 달라 CS1739가 발생했다. 원문: `Assets\TPLab\Input\Tests\PlayMode\InputSystemUiScopeGuardSafetyNetTests.cs(35,65): error CS1739: The best overload for 'AddAction' does not have a parameter named 'expectedControlType'`, 동일 오류 `(37,68)`. 기존 InputRebindingTests:277/설치 API의 `expectedControlLayout`으로 인자 이름 두 곳만 정정해 컴파일했다. 단언·기대값·runtime 불변이다.

Data2/observer1 정상 통과 뒤 Resource targeted는0pass/5fail/0skip(CLI PID34372)여서 요청대로 즉시 중단했다. 다음5개가 모두 동일 TearDown 원문으로 실패했다:

- `TPLab.Core.Tests.ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Open)` — `TearDown : System.InvalidOperationException : No log scope is available`
- `TPLab.Core.Tests.ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Disposed)` — `TearDown : System.InvalidOperationException : No log scope is available`
- `TPLab.Core.Tests.ResourceGuardSafetyNetTests.LoadedScene_ConstructorWorkerPrecedesArgumentValidation` — `TearDown : System.InvalidOperationException : No log scope is available`
- `TPLab.Core.Tests.ResourceGuardSafetyNetTests.LoadedScene_UnloadWorkerRejectsBeforeDelegateAndCachedResult` — `TearDown : System.InvalidOperationException : No log scope is available`
- `TPLab.Core.Tests.ResourceGuardSafetyNetTests.LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry` — `TearDown : System.InvalidOperationException : No log scope is available`

원본 XML stack은 `LogScope.get_Current():32 → LogAssert.NoUnexpectedReceived():95 → CleanupAsync():62 → UniTask.ExceptionResultSource.GetResult():326 → ToCoroutineEnumerator.RunTask():807/MoveNext():838 → BeforeAfterTestCommandBase.ExecuteEnumerable():248`이었다. XML에 비정상 Unity 로그는 없고 output은 결과 저장 안내뿐이다. Editor ready, test active/compiling/updating/playing=false, Console error0·전체 Console은 connector 시작/결과 저장/IPostBuildCleanup 안내만 있었다. 의도하지 않은 runtime 로그를 숨긴 실패가 아니다.

사용자 2026-10-14 승인: 실패 로그 원문을 먼저 확인하고 **fixture timing인 경우만 정정**, LogAssert 약화 금지. 설치 UTF의 BeforeAfterTestCommandBase:232는 InvokeAfter로 iterator를 만든 뒤 :242에서 LogScope를 연다. 기존 expression-bodied TearDown은 CleanupAsync를 즉시 시작해 scene 이미 해제된 동기 경로에서 scope 전에 LogAssert까지 실행했다. 설치 UniTask builder의 즉시 실행과 ToCoroutine이 이미 만든 task를 wrap한다는 코드도 확인했다. 수정은 동일 단언의 실행 위치 두 곳뿐이다:

```csharp
// 이전
public IEnumerator TearDown() => CleanupAsync().ToCoroutine();
// CleanupAsync 끝의 LogAssert.NoUnexpectedReceived();

// 이후
public IEnumerator TearDown()
{
    yield return CleanupAsync().ToCoroutine();
    LogAssert.NoUnexpectedReceived();
}
// CleanupAsync 내부의 같은 LogAssert 호출만 위 위치로 이동
```

정정 Resource5pass/0fail/0skip(PID41816) 후 Input3도 통과했다. worker/native/owner/예외 단언과 상한은 그대로다. 최초0/5를 Green으로 고쳐 기록하지 않는다. 이후 cs/.meta10개 raw SHA-256를 동결했고 변형 중 수정0이다.

## 임시 단일 변형·원복

함수 signature1회 + body 내 교체구간1회를 preflight했다.11개 모두 in-memory 중복 구간은 쓰기 전 거부(negative writes0). 변형 하나씩 적용하고 매번 original raw bytes 원복→refresh/compile→같은 targeted 재통과 후 다음으로 진행했다. source 쓰기/원복 전 실제 IsRunActive=false·compiling/updating/playing=false·동일 Editor PID를 확인했다. runner에 임의 outer timeout을 추가해 실행 중 Editor를 남기지 않았다. idle 판정 불가 시 source를 동결하고 자동 원복도 보류한다.

U2 첫 시도는 source 쓰기 전 idle probe에서 앞선 실행 active=true/일시적 CLI discovery 공백으로 차단됐다. 원본 hash 일치, mutated source/테스트 실행0을 확인했고 그 사전 기록을 별도로 보존했다. 같은 PID27224가 실제 active=false로 복귀한 뒤 고유 fresh 결과로 처음 U2 테스트를 실행했다. 실패한 테스트를 재실행해 덮은 일이 아니다.

독립 검토에서 임시 도구 PID 부분 문자열 판정을 발견해 숫자 parse/==27224로 보완했다. 앞 D1/D2/R1/R2의 write/restore8관측도 exact27224였음을 재대조했고 negative272240은 거부했다. 테스트·runtime 계약 또는 과거 결과를 바꾼 정정은 아니다.

| ID·변형 | targeted 총수 | 실패 | 실패 이유 원문 요약 | 원복 후 통과/실패/skip |
|---|---:|---:|---|---|
| D1 · DataTable guard exact message | 2 | 2 | Expected string length 46 but was 30. Strings differ at index 17.   Expected: "DataTableManager requires Unity's main thread."   But was:  "DataTableManager guard mutant."   ----------------------------^ | 2/0/0 |
| D2 · DataTable disposed before worker | 2 | 1 | Expected: <System.InvalidOperationException>   But was:  <System.ObjectDisposedException> | 2/0/0 |
| R1 · Resource guard exact message | 5 | 2 | Expected string length 45 but was 29. Strings differ at index 16.   Expected: "ResourceManager requires Unity's main thread."   But was:  "ResourceManager guard mutant."   ---------------------------^ | 5/0/0 |
| R2 · Resource disposed before worker | 5 | 1 | Expected: <System.InvalidOperationException>   But was:  <System.ObjectDisposedException> | 5/0/0 |
| L1 · LoadedScene guard exact message | 5 | 3 | Expected string length 52 but was 28. Strings differ at index 14.   Expected: "Scene resources must be used on Unity's main thread."   But was:  "Scene resource guard mutant."   -------------------------^ | 5/0/0 |
| L2 · LoadedScene post-await guard removed | 5 | 1 | Expected: <System.InvalidOperationException>   But was:  <UnityEngine.UnityException> | 5/0/0 |
| O1 · Observer entry guard removed | 1 | 1 | Expected: <System.InvalidOperationException>   But was:  null | 1/0/0 |
| I1 · Input creating-thread exact message | 1 | 1 | Expected string length 57 but was 35. Strings differ at index 6.   Expected: "Input scope operations require the creating Unity thread."   But was:  "Input creating-thread guard mutant."   -----------------^ | 1/0/0 |
| I2 · Lease consumed before worker guard | 1 | 1 | Expected: False   But was:  True | 1/0/0 |
| U1 · Bound UI binding-thread exact message | 2 | 1 | Expected string length 53 but was 31. Strings differ at index 3.   Expected: "UI scope operations require the binding Unity thread."   But was:  "UI binding-thread guard mutant."   --------------^ | 2/0/0 |
| U2 · UI Refresh binding guard before unbound early return | 2 | 1 | Expected and actual are both <System.Exception[2]>   Values differ at index [0]   Expected: null   But was:  <System.InvalidOperationException: UI scope operations require the binding Unity thread.   at TPLab.Samples.SceneTransitions.InputSystemUiScope.EnsureThread () [0x0001d] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Samples\Input\SceneTransitions\Runtime\InputSystemUiScope.cs:271    at TPLab.Samples.SceneTransitions.InputSystemUiScope.Refresh () [0x00000] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Samples\Input\SceneTransitions\Runtime\InputSystemUiScope.cs:63    at TPLab.Core.Input.Tests.InputSystemUiScopeGuardSafetyNetTests+<>c__DisplayClass12_0.<OnWorker>b__0 () [0x00000] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Input\Tests\PlayMode\InputSystemUiScopeGuardSafetyNetTests.cs:102 > | 2/0/0 |

변형 실패 case 원문 이름(namespace만 생략):

- D1: `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Open)`, `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Disposed)`.
- D2: `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Disposed)`.
- R1: `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Open)`, `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Disposed)`.
- R2: `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Disposed)`.
- L1: `ResourceGuardSafetyNetTests.LoadedScene_ConstructorWorkerPrecedesArgumentValidation`, `ResourceGuardSafetyNetTests.LoadedScene_UnloadWorkerRejectsBeforeDelegateAndCachedResult`, `ResourceGuardSafetyNetTests.LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry`.
- L2: `ResourceGuardSafetyNetTests.LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry`.
- O1: `SceneLoadProgressObserverGuardSafetyNetTests.Report_WorkerThreadPrecedesNotificationSuppression`.
- I1: `InputManagerGuardSafetyNetTests.WorkerLeaseDisposePreservesCreatingThreadMessageAndLease`.
- I2: `InputManagerGuardSafetyNetTests.WorkerLeaseDisposePreservesCreatingThreadMessageAndLease`.
- U1: `InputSystemUiScopeGuardSafetyNetTests.BoundWorkerCallsPreserveBindingThreadMessageAndLease`.
- U2: `InputSystemUiScopeGuardSafetyNetTests.UnboundWorkerCallsReturnBeforeBindingThreadValidation`.

정상 신규11개 exact 이름 집합이 변형 실패 이름 합집합에 모두 포함됐다. 미감지0·예상 밖 실패0·원복 미완료0. 실패 수뿐 아니라 메시지/예외 타입/lease 상태/early-return 원문을 검토했다. post-await guard 제거도 메시지 변경과 별도로 감지했다.

## 기준선·동결 후 전체 회귀

| 실행 | 총/통과/실패/skip | CLI PID·source | 이름 집합 |
|---|---|---|---|
| 기준 Edit | 393/393/0/0 | 37864 · cli | 기준 |
| 기준 Play | 362/362/0/0 | 7876 · cli | 기준 |
| 최종 Edit | 397/397/0/0 | 19944 · cli | 기존393 pass·신규4 pass |
| 최종 Play | 369/363/6/0 | 40908 · cli | 기존 이름362 유지(356pass/6fail)·신규7 pass |

기존 이름 누락0, 추가 집합은 targeted 신규11개와 정확히 같다. full run 뒤 동결 해시10개·기존 runtime/테스트/asmdef/UPM·dirty6개의 rawhash를 대조했다. runtime 공개 선언 diff0. 기준 Console의 예상 singleton6/provider2/root2 오류10건 외 최종 Play에서는 실제 비정상 Input System/UI 로그가 추가되어 회귀 통과로 기록하지 않는다.

## 대기 상한·협업·한계

worker는 background Thread.Join3000ms+성공 단언, 각 test는 [Timeout(30000)]이다. Resource async는 최대100 yield와 Stopwatch10,000ms를 검사하고 IsCompleted 단언 후 GetResult한다. 나머지는 pending worker/gate를 남기지 않고 main 후속 사용과 필요 fixed yield 상태를 확인한다. UI-A/Scene-A와 같은 방식이다. 정상 targeted wall time은 observer1.10s, Resource 정정8.32s, Input1.87s, sample8.42s(Play 진입/CLI 비용 포함)였다. 속성만으로 UnityTest 실제 cutoff를 보증하지 않으며 명시적 Join·시간/yield 상한을 같이 둔다. Timeout으로 끊긴 실행0이며 공용 Timeout 정책을 바꾸지 않았다.

부모가 Unity·Git·회고·배정을 소유했다. 기존 하위 agent2개를 재사용: `/root/imp01_pool_impl`은 Core 새 파일/입력 교차 리뷰·임시 도구 검토, `/root/factcheck_upm`은 Input 새 파일/Core 교차 리뷰·fixture 수명 원인 확인. 신규 cs/.meta만 허용, 기존 파일/Git/Unity 금지, 같은 파일 동시 수정0. 기존 model/추론을 유지·상속했고 현재 도구에 실제 model명이 노출되지 않아 특정 버전으로 확정하지 않는다. 비동기 수명·상태 보존 위험평가는 high다. 두 agent의 정적 검토와 부모 실제 실행 검토를 구분하며 둘 다 완료다. 초기 리뷰가 synchronous TearDown 경로를 놓친 한계도 위 실패/정정에 남겼다.

전체 회귀에서 실제 Input System/UI 오류와 포커스 불일치가 발생했다. fixture 전역 상태 격리/정리 영향과 production 버그를 아직 구분하지 못했다. 신규 정상 targeted11·변형 원복은 통과했지만 이로 전체 회귀를 통과로 간주하지 않는다. creating/Bind ID 차이·unbound early return·공유 fault의 no retry는 서로 다른 현행 동작으로만 고정한다. 순환 감지 한계·외부 UI 파괴·Timeout 정책·메시지 통일·runtime refactor는 범위 밖이다. Player/IL2CPP, guard의 별도 신규 GC 계측/Profiler, artifact consumer, 최종 시각 UX는 **미실행**. 전체 기존 GC tests 실행과 별도 신규 GC 계측을 구분한다.

일회성 원시 결과/XML·변형 도구·동결/hash 자료는 이번 작업 소유 `Temp/guard-safetynet-a-20261014`에만 두었다. 현재 중단 상태이므로 재개에 필요한 raw 결과·XML·mutator·동결 자료를 보존한다. 재개/원인 판정·전체 회귀 및 증거 요약 완료 후 실행/참조 종료·실제 경로·명시 파일 목록을 확인해 이 작업 소유 파일만 정리하며 다른 Temp/기존 검증 기록/사용자 자산을 건드리지 않는다. 영속 문서·도구는 이 Temp에 의존하지 않는다.

## 전체 회귀 실패 원문과 재개 조건

Edit397/397/0fail/0skip과 Play369/363pass/6fail/0skip을 원문 그대로 보존한다. 신규 Play7은 모두pass, 실패6은 기존 UI case다. 정상 코드의 실제 비정상 로그가 있으므로 사용자 조건에 따라 테스트/runtime/기존 fixture를 고치거나 회귀를 재실행하지 않고 중단했다. 실패6 원문:

- `TPLab.UI.Tests.UIInputSystemAdapterTests.CancelIsOptInRequestsOnceAndNeverReachesLowerUiAfterAcceptedClose` — Unhandled log message: '[Exception] ArgumentNullException: Value cannot be null. Parameter name: statePtr'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIInputSystemAdapterTests.HeldControlsRequireRawReleaseAndNextEventSystemFrameWithoutReplay` — Unhandled log message: '[Assert] Assertion failed'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIInputSystemAdapterTests.IndependentModalAndTransitionLeasesSurviveMiddleRetirement` — Unhandled log message: '[Exception] NullReferenceException: Object reference not set to an instance of an object'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIInputSystemAdapterTests.NativeAdapterApplicationFailureFaultsUiAndKeepsSafeBlockUntilOwnerShutdown` — Unhandled log message: '[Assert] Assertion failed'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIContextInputPresentationTests.DynamicModeAndBringToFrontMoveWholeSubtreeWithoutChangingGeneration` — Unhandled log message: '[Exception] NullReferenceException: Object reference not set to an instance of an object'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIContextInputPresentationTests.MiddleModalBlocksAThroughClosingAndPreservesIndependentCFocus` — Expected: same as <second (UnityEngine.UI.Button)>   But was:  <P4 adapter source(Clone) (UnityEngine.UI.Button)>

첫4건은 UIInputSystemAdapterTests, 후2건은 UIContextInputPresentationTests다. XML 실행 순서는 새 InputSystemUiScope2 case 뒤 기존 UI suite이며 최초 statePtr 오류는 InputActionState.OnBeforeInitialUpdate/CheckStateIsAtDefault에서, 이어 RemoveDevice assertion·EnableControls NRE·포커스 불일치가 발생했다. 같은 실행 순서라는 사실만으로 인과관계를 확정하지 않는다. HeldControls 실패 stack은 자기 case가 아닌 앞 Cancel의 Scope.Dispose(:458, Cancel b__4_0:180)에서 RemoveDevice가 호출된 기록이며, NativeAdapterFailure 실패도 앞 IndependentModal b__3_0:135의 Dispose stack이다. XML에는 `There can be only one active Event System.` 안내도 있다. 따라서 최초 오류 뒤 이전 async cleanup이 다음 case에 늦게 재개된 연쇄 영향은 관찰되지만 최초 원인은 아직 확정하지 않았다. 읽기 전용 조사: 새 InputSystemUiScopeGuard fixture:17은 InputTestFixture를 상속하지 않으나 기존 InputIntegrationTests:19/UIInputSystemAdapterTests:23은 상속해 입력 전역 상태 SaveAndReset/Restore를 사용한다. 새 TearDown:73의 Dispose→Object.Destroy→UniTask.Yield1과 InputManager:178의 Play 지연 파괴는 다음 fixture reset과 겹칠 가능성이 있다. clone InputActionState 해제는 설치 InputActionAsset.cs:942 OnDestroy에서 일어난다. 기존 UI Scope.Dispose:436은 Context.Dispose를 호출하고 Shutdown을 await하지 않으며 UIContext owned destruction:1139는 NextFrame 대기를 사용한다. 이전 cleanup의 다음 case 재개는 stack 사실이지만 최초 state 객체 identity·실제 teardown/reset 프레임 순서는 미확인이다. 기존 InputIntegration도 지연 Destroy를 사용하므로 위 차이만으로 새 fixture를 원인으로 확정하지 않았다. Resource의 새 scene은 빈 scene이고 InputSystem을 조작하지 않아 직접 입력 오염 근거가 약하다. 읽기 전용 조사 후 수정·재실행을 하지 않았다. 추가 fixture 정정/원인 재현 범위와 재검증은 사용자의 후속 결정이 필요하다.

보존 Temp: baseline-state.json/원래 dirty·tracked 해시, baseline/normal/final 결과, normal-resource-original.xml 및 final-play-original.xml, final-console-errors.json, final-play-order.json, frozen-tests.json, 11개 driver/mutant/restored 결과, U2-prestart-blocked.json·pid-audit.json, mutate.py·회고 생성기. 이 자료는 재개 전 해당 동결 소스와 대조하며 이전 결과를 새 source의 통과로 재사용하지 않는다.

## Commit·규칙·CI metadata

Source/최종 문서 metadata는 구분한다. Actions는 PR 규칙 검사이며 Unity는 위 기존 Editor 실행 결과다.

- 현재 branch/HEAD: `codex/guard-safetynet-a` / `1dffd1e7ce286ced9dac6823ef8864dc3ee3f029`; 신규 소스는 미커밋 working tree다.
- 링크: Markdown244개·local links1930·anchors466 검사, 기존 오류6 유지·새 문서 오류0. 새 작업12파일의 trailing whitespace0, 회고 INDEX 범위 git diff --check0. 전체 git diff --check의 InitScene.unity:164/276/398 공백3건은 작업 전과 동일한 dirty raw bytes이며 수정하지 않았다.
- check_pr_rules: 미실행(회귀 실패로 신규 commit/PR 중단; HEAD끼리의 빈 diff 검사를 현재 변경 검사로 주장하지 않음).
- 신규 exact-SHA Actions: 미실행(신규 변경 push/PR 없음). PR22 gate와 신규 작업의 gate를 구분한다.

## 후속 원인 분리와 fixture 격리(2026-10-14)

사용자 승인 범위는 병합·PR 없이 원인 분리, 신규 fixture 안의 격리 정정만, 기존 테스트·runtime·대기시간 불변, 미해결 신규 case는 제외·사유 기록, 정상 runtime 동작 문제로 보이면 수정하지 않고 보고다. 같은 branch/HEAD `codex/guard-safetynet-a` / `1dffd1e7ce286ced9dac6823ef8864dc3ee3f029`를 재사용했다. 이번 Git stage/commit/push/PR/merge0이다.

### 신규 파일 제외와 단독 포함 실험

신규 cs5개와 .meta5개만 명시 목록으로 Assets 밖 작업 소유 Temp에 이동·보존했다. Git tracked 파일이 아닌 것, 기존/사용자 파일과 겹치지 않는 것, 각 절대 이동 경로가 TPLab Assets·해당 Temp 안인 것, rawhash10개와 GUID5개를 이동 전에 대조했다. 실제 같은 Editor가 idle임을 확인한 뒤 import 중 orphan .meta 재생성을 막도록 DisallowAutoRefresh → 이동 → finally AllowAutoRefresh → compile을 수행했다. 다른 asset/폴더·기존 meta를 이동하지 않았다. 단독 그룹은 기존 파일 위치·assembly를 그대로 사용했다.

그룹은 새 fixture 파일 단위5개다: DataTable2, observer1, ResourceManager2+LoadedScene3(같은 새 파일5), InputManager1, UI scope2. 각각 **다른 신규4파일은 포함하지 않은 전체 EditMode와 PlayMode**를 실행했다. filter로 기존 suite를 빼지 않았다.

환경은 앞선 실행과 같은 Windows/Unity6000.3.18f1/UTF1.6.0/Connector0.4.1/Editor PID27224다. 새 Editor·기존 결과·stale file 대체0이다. 아래는 실제 측정이며 wall time은 CLI/Play 진입 포함이고 test body만의 실행 시간으로 해석하지 않는다.

| 포함 상태 | Edit 통과/전체 · 실패/skip | Play 통과/전체 · 실패/skip | 실제 CLI PID(Edit/Play) | wall seconds(Edit/Play) | 판정 |
|---|---|---|---|---|---|
| 신규 전부 제외 | 393/393 · 0/0 | 362/362 · 0/0 | 41752/35272 | 24.37/22.50 | 통과 |
| DataTable만 | 395/395 · 0/0 | 362/362 · 0/0 | 9484/45956 | 24.70/19.57 | 통과 |
| observer만 | 394/394 · 0/0 | 362/362 · 0/0 | 47092/45228 | 24.44/19.59 | 통과 |
| Resource/LoadedScene만 | 393/393 · 0/0 | 367/367 · 0/0 | 41032/41172 | 25.24/20.06 | 통과 |
| InputManager만 | 394/394 · 0/0 | 362/362 · 0/0 | 40288/46492 | 25.34/19.99 | 통과 |
| UI scope만·정정 전 | 393/393 · 0/0 | 360/364 · 4/0 | 37384/41752 | 25.39/19.99 | 실패 재현 |
| UI scope만·격리 정정 후 | 393/393 · 0/0 | 364/364 · 0/0 | 34784/43152 | 25.31/20.53 | 통과 |
| 신규11 전부·격리 정정 후 | 397/397 · 0/0 | 369/369 · 0/0 | 43496/45520 | 25.70/20.49 | 통과 |

실행 명령: 위 공식 runner에 absolute project·mode·고유 output을 사용했다. 최초 제외 기준선의 신규 미포함393/362가 모두 통과했으므로 다음으로 진행했다. 실제 helper는 실험마다 같은 Editor idle→source 선택/compile→mode별 idle 확인→full suite를 수행한다. 실험 label별 JSON/XML/console은 실행 전 없는 새 경로로만 만들고 같은 CLI PID/launch timestamp/nonzero total·기대 case 수·skip0을 확인했다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/guard-safetynet-a-isolation-20261014/baseline-excluded-EditMode.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/guard-safetynet-a-isolation-20261014/baseline-excluded-PlayMode.json
# 그룹별 고유 output: data-only/observer-only/resource-only/input-only/scope-only/scope-isolated/final-all-isolated-{mode}.json
```

### 재현 실패 원문·영향 판정

UI scope 원본을 단독 포함하면 신규2는 pass지만 기존4가 실패했다. 최초 전체의6건과 정확히 같은 실패 개수/후속 이름을 요구하지 않고, 최초 statePtr 오류와 늦은 cleanup 연쇄를 원문으로 비교했다. SDK input state·async cleanup의 뒤늦은 재개가 후속 test를 오염시키므로 후속 failure가 어느 case에서 포착되는지는 달랐다. 재현4건의 실패 원문:

- `TPLab.UI.Tests.UIInputSystemAdapterTests.CancelIsOptInRequestsOnceAndNeverReachesLowerUiAfterAcceptedClose` — Unhandled log message: '[Exception] ArgumentNullException: Value cannot be null. Parameter name: statePtr'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIInputSystemAdapterTests.HeldControlsRequireRawReleaseAndNextEventSystemFrameWithoutReplay` — Unhandled log message: '[Assert] Assertion failed'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.PlayMode.UIContextPresentationTests.ThreeAcceptedRootsKeepInterleavedBorrowedSiblingSlots` — Unhandled log message: '[Exception] NullReferenceException: Object reference not set to an instance of an object'. Use UnityEngine.TestTools.LogAssert.Expect
- `TPLab.UI.Tests.UIContextInputPresentationTests.MiddleModalBlocksAThroughClosingAndPreservesIndependentCFocus` — Expected: same as <second (UnityEngine.UI.Button)>   But was:  <P4 adapter source(Clone) (UnityEngine.UI.Button)>

XML에서 첫 Cancel의 statePtr stack은 InputActionState.OnBeforeInitialUpdate/CheckStateIsAtDefault였으며 Held의 RemoveDevice stack은 이전 Cancel Scope.Dispose다. ThreeAcceptedRoots 실패 stack도 이전 Cancel의 Open/UniTask unobserved exception 재개다. 원본 최초 전체6건과 이번 재현4건은 별도 실행이며 둘 다 보존했다. 이를 서로 독립적인6/4 runtime 결함으로 세지 않았다.

신규 미포함·나머지 단독4그룹에서는 통과, UI fixture 포함에서는 최초 오류 재현, **그 fixture의 입력 격리만 바꾸면** 단독364 및 신규 전체369에서 모두 통과했다. 이번 회귀의 영향을 새 UI fixture의 Input System 전역 상태 격리 경계로 분리했다. 실제 statePtr가 가리킨 객체 identity와 지연 파괴의 정확한 프레임을 측정했다고 주장하지 않으며, 제품 native lifetime 전체에 결함이 없음을 증명한 결과도 아니다. supported public flow의 독립 production 결함은 이번 실험에서 확정되지 않았고 runtime은 수정하지 않았다.

### 신규 UI fixture 격리 정정만

바뀐 파일은 [InputSystemUiScopeGuardSafetyNetTests.cs](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs) 한 곳이다.

- [19](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs#L19): private InputTestFixture 구성 객체를 추가했다.
- [33](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs#L33): 기존 SetUp 첫 단계에 Setup을 호출해 isolated Input System state를 만들었다.
- [99–101](../../Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs#L99): 기존 UnityTearDown cleanup을 try로 감싸고 finally에서 TearDown으로 이전 입력 상태를 복원했다. 기존 LogAssert.NoUnexpectedReceived는 복원 뒤 그대로 검사하므로 weak expectation/로그 허용 추가0이다.

단순 InputTestFixture 상속은 inherited normal TearDown의 Restore가 기존 UnityTearDown 정리보다 먼저 실행되고 같은 이름의 IEnumerator TearDown이 void TearDown과 충돌할 수 있어 채택하지 않았다. 설치 SDK 문서가 지원하는 구성 방식으로 lifetime만 명시했다. Setup 이후 구성/cleanup 예외 시 Restore를 시도하고 오류를 삼키지 않는다. SDK Setup 자체의 부분 실패·cleanup과 Restore 동시 실패의 예외 우선순위를 새 보장으로 확장하지 않는다.

private OnWorker부터 파일 끝의 모든 helper·두 test body/단언은 **원본 byte와 동일**하다. Join3000ms·[Timeout(30000)] 개수/값·UniTask.Yield 횟수·async 대기 상한 불변이며 NextFrame/Delay를 추가하지 않았다. native 권한/state/map/lease/메시지의 기대값을 바꾸지 않았다. Bind/creating ID를 공통화하지 않았다. 새 패키지/assembly/기존 테스트/runtime/UPM/사용자 asset 수정0이다.

앞선 freeze 이후 새 source 변경은 이 파일 한 곳뿐이며 meta5개·다른 새 cs4개는 원본 hash다. 테스트 본문은 그대로이고 최신 지시가 runtime 변경을 금지하므로 **이번 원인 분리에서 runtime 임시 변형도 수행하지 않았다**. 앞11변형 원문은 당시 source의 기록으로 유지하며 이번 source에서 모든 변형을 재실행한 것으로 바꾸지 않는다.

### 최종 회귀와 보존

최종 case name 집합은 재실행 제외 기준선 Edit393/Play362를 전부 포함하고, 추가는 이전 정상 targeted의 정확한 신규11 이름(Edit4/Play7)과 같다. 기존 이름 누락0·삭제0·Ignore0·제외0. 새 UI2도 유지했다. 최종397/369는 전부pass/실패0/skip0이고 실제 raw source·원본 meta hash를 검사했다.

Play 종료 Console error10건은 기준선의 예상 singleton6/provider2/root2 첫 줄 집합과 같다. 신규 statePtr/NullReference/assertion/focus 오류0이며 앞선 실패/재현값을 지우지 않았다. runtime/기존 테스트/asmdef/UPM 및 dirty6의 rawhash는 모두 원래 값, 신규10파일은 Assets의 원래 위치와 GUID로 복원됐다. UI source hash는 `7cd6442a70ad1195c2ab6906e21d150b73e0f2048b3c3e527c4421cafb22c10b`이다.

기존 독립 reviewer agent `/root/imp01_pool_impl`을 같은 설정으로 재사용해 SDK 수명·구성 후보와 패치를 읽기 전용 검토했다. 부모만 파일 이동/정정·Unity·회고를 맡았다. 기존 파일/Git/Unity 수정은 reviewer에게 금지했고 추가 모델/사용자 task를 만들지 않았다. model/추론 상속의 실제 버전은 현재 도구에 노출되지 않아 특정 값으로 단정하지 않는다. lifetime/전역 상태 위험은 high다. 최초 state identity/EndOfFrame 확정 미측정과 SDK partial Setup 실패의 한계도 남긴다.

Player/IL2CPP·새 GC 계측/Profiler·visual UX·artifact consumer는 여전히 미실행이다. 이 단계는 사용자 요청대로 Git commit/push/PR/병합·신규 exact-SHA Actions를 하지 않는다. 기존 HEAD의 CI를 새 working tree의 CI로 주장하지 않는다. check_pr_rules는 미커밋 diff 대상 SHA가 없어 미실행이며 링크/공백/source integrity는 아래에서 확인한다.

### 후속 문서·임시 파일 처리 상태

동일 회고에 후속 증거를 추가했고 회고 INDEX 상태 한 줄만 갱신한다. 최초 중단·실패 원문을 삭제/전환하지 않는다. 문서/source 링크·새 작업 파일 공백 검사는 최종 단계에서 확인한다. 앞선 일회성 Temp와 이번 원인 분리 Temp에는 현재 미커밋 테스트와 관련된 원문/해시·이동 manifest가 있다. 이후 사용자 검토·Git 단계에서 필요한 요약은 이 회고 한 곳을 사용하며, 일회성 실험은 실행/참조 종료와 실제 경로·명시 파일 목록 확인 뒤 작업 소유 파일만 정리한다. 기존 Unity Temp나 다른 작업 데이터는 삭제하지 않는다.

최종 미커밋 검증 source의 raw SHA-256(실행 뒤 byte 불변 확인):

| source/meta | SHA-256 |
|---|---|
| `Assets/TPLab/Tests/EditMode/DataTableManagerGuardSafetyNetTests.cs` | `0507a4f8f447bf762b40d17aad2a9c9c9078a1bdef07788df33599dfff3c5744` |
| `Assets/TPLab/Tests/EditMode/DataTableManagerGuardSafetyNetTests.cs.meta` | `72fd3e2ae95d58cd4bc19b8847097b8a1abdee5b617a1eed4f0027569e298b94` |
| `Assets/TPLab/Tests/EditMode/SceneLoadProgressObserverGuardSafetyNetTests.cs` | `99ca41689cef324753f3ecf7dae2036f41275a3dcd8feb34ae1abeaa3f758b99` |
| `Assets/TPLab/Tests/EditMode/SceneLoadProgressObserverGuardSafetyNetTests.cs.meta` | `7198308fa5d7fcd0d6eeecf477380bc178b2f2ce6d6452319c15adfffe772a9f` |
| `Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs` | `a8382ca318a7a4ed34473cf99f3786a51795a50981f7e3cea2c08a82729f16d6` |
| `Assets/TPLab/Tests/PlayMode/ResourceGuardSafetyNetTests.cs.meta` | `020fe2f850f9b66706257a1917bfd1699cb20596aa8afa3feebba03532536fc4` |
| `Assets/TPLab/Input/Tests/EditMode/InputManagerGuardSafetyNetTests.cs` | `56d78bf116945092a31b7855e0b0a189c286dd99cfe591863ef8974920e55e37` |
| `Assets/TPLab/Input/Tests/EditMode/InputManagerGuardSafetyNetTests.cs.meta` | `63a6eea040f5539edf0e72888a1b9cd82184e8e8f9f8e62e9f3b33ac9e8a0bfe` |
| `Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs` | `7cd6442a70ad1195c2ab6906e21d150b73e0f2048b3c3e527c4421cafb22c10b` |
| `Assets/TPLab/Input/Tests/PlayMode/InputSystemUiScopeGuardSafetyNetTests.cs.meta` | `9f04942c636b6a3ab76f2e4f3b73d2ef8b62a694d1d33f1ae8d862f5c2e7d4cb` |

최종 링크/공백 검사: Markdown244개·local link1935·anchor470, 기존 오류6 유지·새 문서 오류0. 새 작업12파일 trailing whitespace0·회고 INDEX 범위 git diff --check0. 기존 InitScene dirty의 공백3건은 수정하지 않았다. 작업 소유 일회성 파일 정리는 아래 실제 완료 여부를 기록한다.

정리 완료: 같은 Editor PID27224의 test active/compiling/updating/playing=false 및 관련 helper 실행0을 확인한 뒤, 실제 절대 경로·소유 파일 목록을 검증한 두 작업 Temp 폴더만 정리했다. 작업 소유149파일·7,838,433bytes 제거, Assets/source/meta 이동·삭제0·다른 Temp 변경0. 이 회고의 source 해시·명령·수치·실패 원문 요약은 보존했고 작업별 raw 결과/일회성 스크립트는 남기지 않았다.

## PR 제공 단계: 반복 회귀와 정정 후 fixture 기준 변형(2026-10-14)

사용자는 전체 PlayMode369 반복 확인, 변형 확인이 정정 후 fixture 기준임을 명시, commit/push/PR·규칙 검사·최종 HEAD Actions 후 멈춤을 승인했다. 이전 원인 분리 단계의 runtime 무수정과 당시 변형 미실행 기록은 과거 실행 그대로 유지했다. 이 단계에서는 원래 특성화 절차의 **일회성 runtime 변형·완전 원복**을 다시 수행했고 permanent runtime diff0이다. 앞선 기록의 source를 소급해 정정 후 source로 바꾸지 않는다.

### 전체 Play 반복과 source 동결

같은 Editor PID27224 / Unity6000.3.18f1 / UTF1.6.0 / Connector0.4.1에서 정정 후 cs5개/.meta5개 hash가 위 source 표와 같은지 확인하고 동결했다. 최신 UI fixture hash는 `7cd6442a70ad1195c2ab6906e21d150b73e0f2048b3c3e527c4421cafb22c10b`이며 다음 정상본·변형·원복 모두 이 fixture를 사용했다. 테스트 body/worker/대기시간은 바꾸지 않았다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/guard-safetynet-a-publish-20261014/repeat-play.json
```

반복 결과 **369total/369pass/0fail/0skip**, CLI PID49644·source=connector-file·runId=49644-1791963688292907100, Editor PID27224다. 이전 정정 후 전체 Play369/369(PID45520)와 같은 통과 수가 반복됐다. 이번 exact 이름369개 중 신규Play7 전부 포함·중복0을 확인했고, 이전 검증 source10 hash와 byte 동일성을 대조했다. CLI의 domain reload disconnect 뒤 그 실행 PID의 fresh Connector 결과만 공식 runner가 회수했으며 stale/다른 suite 대체0이다.

정정 후 정상 targeted는 같은 runner+각 fixture filter로 Data2/observer1/Resource5/Input1/UI2, 총11pass/0fail/0skip이었다. preflight는 함수 signature/body 구간 각각1회만 허용하고 in-memory 중복11개 모두 쓰기 전 거부했다. 같은 Editor의 실제 runner inactive/compile idle/PID exact27224 확인 뒤 한 번에 source1변형만 적용하고 original raw byte 복구·compile·같은 targeted 재통과 뒤 다음으로 진행했다. test10파일 hash·dirty6·기존 tracked bytes도 매번 검사했다.

### 정정 후 fixture 기준11변형 결과

아래 표는 **격리 정정 후 fixture의 새로운 실제 실행**이다. 첫 변형 표의 과거 결과를 복사하거나 새 source 결과로 해석하지 않았다.

| ID·단일 변형 | targeted 총수 | 실제 실패 | 실제 실패 이유 요약 | 원복 후 통과/실패/skip |
|---|---:|---:|---|---|
| D1 · DataTable guard exact message | 2 | 2 | Expected string length 46 but was 30. Strings differ at index 17.   Expected: "DataTableManager requires Unity's main thread."   But was:  "DataTableManager guard mutant."   ----------------------------^ | 2/0/0 |
| D2 · DataTable disposed before worker | 2 | 1 | Expected: <System.InvalidOperationException>   But was:  <System.ObjectDisposedException> | 2/0/0 |
| R1 · Resource guard exact message | 5 | 2 | Expected string length 45 but was 29. Strings differ at index 16.   Expected: "ResourceManager requires Unity's main thread."   But was:  "ResourceManager guard mutant."   ---------------------------^ | 5/0/0 |
| R2 · Resource disposed before worker | 5 | 1 | Expected: <System.InvalidOperationException>   But was:  <System.ObjectDisposedException> | 5/0/0 |
| L1 · LoadedScene guard exact message | 5 | 3 | Expected string length 52 but was 28. Strings differ at index 14.   Expected: "Scene resources must be used on Unity's main thread."   But was:  "Scene resource guard mutant."   -------------------------^ | 5/0/0 |
| L2 · LoadedScene post-await guard removed | 5 | 1 | Expected: <System.InvalidOperationException>   But was:  <UnityEngine.UnityException> | 5/0/0 |
| O1 · Observer entry guard removed | 1 | 1 | Expected: <System.InvalidOperationException>   But was:  null | 1/0/0 |
| I1 · Input creating-thread exact message | 1 | 1 | Expected string length 57 but was 35. Strings differ at index 6.   Expected: "Input scope operations require the creating Unity thread."   But was:  "Input creating-thread guard mutant."   -----------------^ | 1/0/0 |
| I2 · Lease consumed before worker guard | 1 | 1 | Expected: False   But was:  True | 1/0/0 |
| U1 · Bound UI binding-thread exact message | 2 | 1 | Expected string length 53 but was 31. Strings differ at index 3.   Expected: "UI scope operations require the binding Unity thread."   But was:  "UI binding-thread guard mutant."   --------------^ | 2/0/0 |
| U2 · UI Refresh binding guard before unbound early return | 2 | 1 | Expected and actual are both <System.Exception[2]>   Values differ at index [0]   Expected: null   But was:  <System.InvalidOperationException: UI scope operations require the binding Unity thread.   at TPLab.Samples.SceneTransitions.InputSystemUiScope.EnsureThread () [0x0001d] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Samples\Input\SceneTransitions\Runtime\InputSystemUiScope.cs:271    at TPLab.Samples.SceneTransitions.InputSystemUiScope.Refresh () [0x00000] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Samples\Input\SceneTransitions\Runtime\InputSystemUiScope.cs:63    at TPLab.Core.Input.Tests.InputSystemUiScopeGuardSafetyNetTests+<>c__DisplayClass13_0.<OnWorker>b__0 () [0x00000] in C:\Users\PC\Projects\TPLab\Assets\TPLab\Input\Tests\PlayMode\InputSystemUiScopeGuardSafetyNetTests.cs:111 > | 2/0/0 |

실패 exact case 이름(namespace만 생략):

- D1: `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Open)`, `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Disposed)`.
- D2: `DataTableManagerGuardSafetyNetTests.LoadAsync_WorkerThreadPrecedesOwnerState(Disposed)`.
- R1: `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Open)`, `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Disposed)`.
- R2: `ResourceGuardSafetyNetTests.LoadAssetAsync_WorkerThreadPrecedesOwnerAndKeyValidation(Disposed)`.
- L1: `ResourceGuardSafetyNetTests.LoadedScene_ConstructorWorkerPrecedesArgumentValidation`, `ResourceGuardSafetyNetTests.LoadedScene_UnloadWorkerRejectsBeforeDelegateAndCachedResult`, `ResourceGuardSafetyNetTests.LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry`.
- L2: `ResourceGuardSafetyNetTests.LoadedScene_WorkerBackendCompletionFaultsSharedUnloadWithoutRetry`.
- O1: `SceneLoadProgressObserverGuardSafetyNetTests.Report_WorkerThreadPrecedesNotificationSuppression`.
- I1: `InputManagerGuardSafetyNetTests.WorkerLeaseDisposePreservesCreatingThreadMessageAndLease`.
- I2: `InputManagerGuardSafetyNetTests.WorkerLeaseDisposePreservesCreatingThreadMessageAndLease`.
- U1: `InputSystemUiScopeGuardSafetyNetTests.BoundWorkerCallsPreserveBindingThreadMessageAndLease`.
- U2: `InputSystemUiScopeGuardSafetyNetTests.UnboundWorkerCallsReturnBeforeBindingThreadValidation`.

최신 정상11개 이름 집합이 이11변형의 실패 이름 합집합에 전부 포함돼 미감지0이며, 예상 밖 실패0·원복 미완료0이다. 예외 타입/정확한 메시지/lease 상태/early-return 단언 실패 원문을 검토했다. L2의 post-await guard 제거는 실제 UnityException과 기대 InvalidOperationException 차이를 감지했다. UI U1/U2는 InputTestFixture 구성 격리·Setup/Restore가 적용된 상태에서 각각 bound 메시지/unbound 조기 반환 위반을 감지하고 원복2/2 pass를 확인했다.

변형 후 frozen cs/meta10개·기존 runtime/테스트/asmdef/UPM raw bytes·dirty6는 original이고 모든 변형을 permanent 변경에서 제외했다. 이전 전체 Edit397/397(PID43496)의 source10 hash와도 일치한다. 이번 전체 Edit를 재실행했다고 주장하지 않는다. 부모가 Unity·원복·Git·회고를 수행하고 기존 reviewer agent는 최신 도구·source·원문 보존을 읽기 전용 확인했다. 추가 model/task 생성0이다.

### Commit·규칙·CI metadata(최신)

T2 테스트 전용이며 public runtime 계약·XML·사람/AI API 갱신은 비적용이다. 최초 실패/정정·원인 분리와 이번 repeat/변형을 이 회고 한 곳에 보존한다. PR 본문과 색인은 링크/상태만 둔다.

- 검증 source commit: commit 후 기록.
- check_pr_rules·공백·링크: commit 전/후 확인.
- exact-SHA Actions: PR 개설 후 source와 최종 tip을 구분해 확인.
- Player/IL2CPP·추가 GC 계측·visual UX·consumer는 미실행이다.
- 이 단계의 일회성 raw/도구는 작업 소유 Temp에만 두고 필요한 요약을 이 회고에 남긴 뒤 실행/참조 종료·실제 경로·명시 목록 확인 후 정리한다.
