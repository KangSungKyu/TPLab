# GameSceneManager 특성화 안전망 B — 2026-10-15

상태: T2 테스트 전용·승인 정정 후7case, 임시 변형10건, 전체 회귀와 반복 완료. PR 제공·최종 HEAD CI 확인 단계이며 main 병합은 사용자 명시 승인을 기다린다.

## 범위·source·확정 case

기준 main/source `9062f72b0bca5db75b5e61f5ffd0be817d28fd02`(UI-B PR #24 병합)에서 `codex/scene-safetynet-b`를 만들었다. 검증 source는 위 runtime 원본과 아래 동결 신규 cs/meta 조합이다. source commit과 문서·CI metadata를 구분한다. 새 [GameSceneManagerSafetyNetBTests.cs](../../Assets/TPLab/Tests/PlayMode/GameSceneManagerSafetyNetBTests.cs)와 .meta, 이 회고와 [회고 INDEX](INDEX.md)만 이번 변경이다. 기존 Scene PlayMode assembly 사용; runtime·기존 테스트·assembly/참조·package·자산·UPM·XML·사람/AI API 변경0. public 계약을 바꾸지 않는 특성화라 API 문서 갱신은 비적용이다.

| 이름 | case | 보호하는 현재 동작 |
|---|---:|---|
| `ExplicitRequestRejectsInvalidInputWithoutEffects(Kind/Mode/RequiredIds/Destination)` | 4 | kind→mode→required IDs→load destination→target의 지정 invalid 쌍; 정확한 예외 타입·Message·ParamName, 즉시/다음 yield 효과 불변, 이력 미소비, 이후 실제 entry/shutdown 정상 사용 |
| `PolicyEvaluatesEveryConditionInAttachmentOrderAfterFalse` | 1 | false/true/false 조건이 부착 순서로 모두 각1회 평가·false 반환; Validate 정확히1회·load/unload0회, 나머지 효과 불변; 다음 yield 추가 평가0회·허용 후 정상 전환 |
| `FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory` | 1 | presentation·loading release·failure callback의 겹친 실패; phase·원본 객체/2계층 집계 순서, LastFailure·await·두 history 공유 정체성, candidate 정리·공용 root 잔존 |
| `RegisteredReadFailureClearsPartialContentsWithoutRestoringExpandedCapacity` | 1 | 초기 capacity0에서 primary 추가 후 derived 읽기 실패: 원본 예외 SameAs, 부분 count1→0, 확장 capacity 그대로, 상태 불변·후속 읽기 성공 |

확정 PlayMode7case·EditMode0, 제외0. 기존 capacity 테스트는 수정하지 않았다. 새 case가 초기 capacity0에서 실제 확장을 만든 뒤 실패 직전 용량과 정확히 같은지 검사하며 .NET 성장 배수는 고정하지 않는다. GUID `328bacd594584c748c495edeab589a11`의 새 .meta를 포함한다.

invalid 입력 상세: Kind99+mode99 → ArgumentOutOfRangeException/ParamName kind; FirstEntry+mode99+중복 required IDs → ArgumentException/ParamName mode; 중복 required IDs+기존 destination → ArgumentException/ParamName null; 기존 destination+default target → ArgumentException/ParamName null. 정확한 메시지는 신규 테스트의 상수 및 동일 환경의 ArgumentException/ArgumentOutOfRangeException 생성 메시지와 전체 문자열 일치로 검사한다. 특정 invalid 쌍의 현재 우선순위이며 전체 invalid 조합 검증을 뜻하지 않는다.

Scene-A private fixture/helper는 복사하지 않았다. 기존 공개 test-only GameSceneSafetyNetCallbacks·SceneTransitionCallbacksProbe·SceneConditionProbe·SceneReadinessFaultProbe·BootstrapSceneTestSetup만 참조했다. 로더 wrapper·23항목 효과 snapshot·bounded await·정리는 새 파일에 필요한 최소를 자체 정의했다. snapshot은 manager 상태/phase/failure/progress/소유/등록, 공용root·host, native inventory/active scene/events, loader/callback/progress/failure/installer 기록이다. 정상 entry·load/unload·callback·native 사건을 양성 대조해 관찰기 작동도 확인했다.

## 최초 정상본 실패와 승인 정정

최초7건 중6통과·1실패였다. false 결과·조건 부착 순서·각1회 호출 단언은 통과했으나 신규 작성자의 “loader 검증0회” snapshot 가정이 틀렸다. [GameSceneManager.cs:359](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L359)의 loader.Validate(request.Target)가 [382행](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L382)의 EvaluatePolicy보다 먼저 실행된다. fixture timing·runtime 결함·의도한 변형 Red로 처리하지 않았다. 최초 cs SHA256 `38ea61b9a6c39fbcb241632b97994de08782d8c18ef432bfe0f113bd4fa54f39`.

최초 실패 원문(JSON은 CRLF·trailing 문자를 가역 보존):

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.PolicyEvaluatesEveryConditionInAttachmentOrderAfterFalse:   Expected and actual are both <System.Object[23]>\r\n  Values differ at index [17]\r\n    Expected is <System.String[0]>, actual is <System.String[1]>\r\n    Values differ at index [0]\r\n  Extra:    < \"build:validate\" >"
]
```

실패 직후 후속 변형·회귀·commit을 멈추고 원인을 보고했다. 사용자2026-10-15의 정정 승인 뒤 CheckPolicy만 수정했다: backend 예상 snapshot을 정확히 new[] { "build:validate" }로 하고 실제 backend 전체도 그 배열과 일치함을 단언했다. **Validate1회·load0회·unload0회**이며 범위/이상 단언으로 약화하지 않았다. false·순서·각1회·나머지22항목 및 다음yield 불변 단언은 유지했다. invalid4case는 여전히 loader 검증도0회다. 정정 후7/7·신규 파일 단독 전체379/379 통과 뒤 동결했고, 아래10변형은 모두 정정 후 fixture/test 기준으로 처음부터 수행했다. 정정 전 실제 변형 실행0건이다.

현재 대상/로더 사전 검증이 프로젝트 조건보다 앞선 흐름에서 설계 결함으로 단정할 근거는 찾지 못했다. 비단락 평가의 바람직함·단락 평가 필요성은 이 테스트로 결정하지 않으며 현재 관찰값만 고정한다.

## 환경·명령·정상본·회귀

Windows / Unity6000.3.18f1 / UTF1.6.0 / UniTask2.5.11 / Connector0.4.1 / 기존 Editor PID3428 / C:\Python314\python.exe. 절대 project·실제 compile/test idle을 확인하고 동일 Editor에서 source를 동결해 실행했다. 새 Editor·다른 프로젝트·stale 결과로 대체하지 않았다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/scene-safetynet-b-20261015/baseline-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/scene-safetynet-b-20261015/baseline-play.json
```

정상본은 같은 명령에 --filter TPLab.Core.Tests.GameSceneManagerSafetyNetBTests와 label별 고유 output을 사용했다. 변형은 각 method full name filter; final/repeat는 filter 없음. Temp wrapper는 영속 runner만 호출해 driver wall·원문을 기록했으며 문서 재사용이 그 일회성 도구에 의존하지 않는다.

| label | mode | total/pass/fail/skip | CLI PID · source(runId) | driver wall(초) |
|---|---|---|---|---|
| `baseline-edit` | EditMode | 398/398/0/0 | 22464 · cli (-) | 미측정 |
| `baseline-play` | PlayMode | 372/372/0/0 | 25172 · cli (-) | 미측정 |
| `normal` | PlayMode | 7/6/1/0 | 20872 · cli (-) | 미측정 |
| `normal-corrected` | PlayMode | 7/7/0/0 | 16424 · cli (-) | 6.4502 |
| `play-file-alone-full` | PlayMode | 379/379/0/0 | 28532 · cli (-) | 13.7073 |
| `final-edit` | EditMode | 398/398/0/0 | 13432 · cli (-) | 21.0711 |
| `final-play` | PlayMode | 379/379/0/0 | 6528 · cli (-) | 14.5373 |
| `repeat-edit` | EditMode | 398/398/0/0 | 12204 · cli (-) | 21.1147 |
| `repeat-play` | PlayMode | 379/379/0/0 | 28564 · cli (-) | 14.0075 |

측정: final/repeat 모두 기존 Edit398 이름 집합 그대로, Play372+신규7과 정확히 일치했다. 선언4메서드와 실행7case를 구분한다. 기존 안전망50case도 전체 기준선/최종 실행에 포함된다.

대기: 각 UnityTest [Timeout(30000)]; async 완료는 NextFrame 최대1000회와 Stopwatch10초의 이중 상한. 정상 경로를 지연하지 않는 Scene-A 방식이며30초 외곽은 내부 실패·정리에 여유를 준 테스트 상한이다. runtime Timeout 정책 변경0. worker가 없어 Thread.Join 비적용. 새 미해제 gate는 만들지 않았고 기존 probe의 gate는 정리 시작에서 해제한다. lazy UnityTearDown이 manager/common shutdown·host native 파괴 및 다음frame을 기다린 뒤 LogAssert.NoUnexpectedReceived를 수행한다. static installer Trace·active scene·이벤트 구독을 복원한다. 입력/EventSystem/전역 input 상태를 건드리지 않아 InputTestFixture 비적용이다. 단독 전체/최종 회귀 실패0이라 신규 제외·그룹별 원인 격리 조건은 발생하지 않았다.

인프라: final-edit 최초 시도는 Console preflight에서 `Error: no Unity instances running`으로 거부돼 테스트가 시작되지 않았다. 그 미시작 driver를 별도 보존하고 동일 PID3428의 ready·compile/update/play/test inactive를 확인한 뒤 실행했다; 실패 테스트 재시도가 아니다. 상태 조회 중 동일 Editor의 잠시 Hold on/busy·heartbeat 지연은 강제 종료/새 Editor/소스 변경 없이 idle로 복구한 뒤 진행했다. 변형 도구 첫 preflight의 UniTaskVoid marker 오기는 실제 UniTask와 달라 쓰기 전에 거부됐고 임시 도구만 정정했다. 최초 M1 enum full case 필터는 total0으로 runner가 거부해 미실행으로 구분했고 원본을 finally에서 복구했다; 이후 method4case 필터 사용. attempts의 prestartDiscoveryRejected=true는 테스트가 시작되지 않은 동일 Editor 재연결이다. connector-file은 해당 CLI PID의 fresh runId만 사용한다.

## 정정 후 임시 변형과 원복

함수 body의 unique pattern에 변형 하나씩 적용·실패 확인·원본 bytes 복구·재통과했다. 쓰기 전 unique preflight10/10·중복 거부10/10. 신규 cs/meta hash 동일, 두 runtime 원본 복구 확인. 실제7case가 모두 최소1변형에서 의도한 이유로 실패했으며 민감도 미확인0이다. 변형을 commit하지 않는다.

| ID·함수 | 변형·실패 의미 | 실패 case | Red total/pass/fail/skip · CLI | 원복 Green total/pass/fail/skip · CLI |
|---|---|---|---|---|
| M1 · `internal static void ValidateKindAndMode(` | kind/mode 검사 순서 반전 → Kind 예외 타입 변경 | `ExplicitRequestRejectsInvalidInputWithoutEffects(Kind)` | 4/3/1/0 · 8356 | 4/4/0/0 · 23680 |
| M2 · `private Operation BeginRequest(` | RequiredIds 우선 검사 → Mode 메시지 변경 | `ExplicitRequestRejectsInvalidInputWithoutEffects(Mode)` | 4/3/1/0 · 13884 | 4/4/0/0 · 18200 |
| M3 · `private Operation BeginRequest(` | destination 우선 검사 → RequiredIds 메시지 변경 | `ExplicitRequestRejectsInvalidInputWithoutEffects(RequiredIds)` | 4/3/1/0 · 9248 | 4/4/0/0 · 5576 |
| M4 · `private Operation BeginRequest(` | target 우선 검사 → Destination 예외 타입 변경 | `ExplicitRequestRejectsInvalidInputWithoutEffects(Destination)` | 4/3/1/0 · 7996 | 4/4/0/0 · 26892 |
| M5 · `private bool EvaluatePolicy(` | 단락 &&로 변경 → 뒤 조건 호출 누락 | `PolicyEvaluatesEveryConditionInAttachmentOrderAfterFalse` | 1/0/1/0 · 20392 | 1/1/0/0 · 15896 |
| M6 · `private async UniTask RunOperationAsync(` | UI 해제 실패 누락 → 원본 집계 구조 소실 | `FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory` | 1/0/1/0 · 6460 | 1/1/0/0 · 25488 |
| M7 · `private async UniTask RunOperationAsync(` | 전환/UI 해제 집계 순서 반전 → SameAs 순서 변경 | `FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory` | 1/0/1/0 · 13448 | 1/1/0/0 · 15576 |
| M8 · `private Exception RecordFailure(` | OnFailure 집계 순서 반전 → 공유 원본 집계 정체성 변경 | `FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory` | 1/0/1/0 · 24212 | 1/1/0/0 · 23184 |
| M9 · `private async UniTask RunOperationAsync(` | FailurePhase를 Idle로 변경 → phase 불일치 | `FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory` | 1/0/1/0 · 5388 | 1/1/0/0 · 28520 |
| M10 · `public void GetRegisteredScenes(` | Clear 뒤 TrimExcess → 확장 capacity 보존 실패 | `RegisteredReadFailureClearsPartialContentsWithoutRestoringExpandedCapacity` | 1/0/1/0 · 27512 | 1/1/0/0 · 24164 |

### 실패 원문

runner failures 문자열 전체를 가역 JSON으로 보존했다. 정상본 최초 실패와 구분한다.

M1:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.ExplicitRequestRejectsInvalidInputWithoutEffects(Kind):   Expected: <System.ArgumentOutOfRangeException>\r\n  But was:  <System.ArgumentException: Select a valid load mode; derived commands require Additive.\r\nParameter name: mode\r\n  at TPLab.Core.SceneManagement.SceneTransitionSettings.ValidateKindAndMode (TPLab.Core.SceneManagement.SceneTransitionKind kind, UnityEngine.SceneManagement.LoadSceneMode mode) [0x00036] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\SceneTransitionSettings.cs:69 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.BeginRequest (TPLab.Core.SceneManagement.SceneTransitionRequest request, System.Threading.CancellationToken callerToken) [0x00025] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:314 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.TryTransitionAsync (TPLab.Core.SceneManagement.SceneTransitionRequest request, System.Threading.CancellationToken cancellationToken) [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:501 \r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass29_0.<CheckInvalid>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:248 \r\n  at NUnit.Framework.Assert.Throws (NUnit.Framework.Constraints.IResolveConstraint expression, NUnit.Framework.TestDelegate code, System.String message, System.Object[] args) [0x0002f] in <f577cd9686154d6ab10efd7e65e64366>:0 >\r\n"
]
```

M2:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.ExplicitRequestRejectsInvalidInputWithoutEffects(Mode):   Expected string length 82 but was 51. Strings differ at index 0.\r\n  Expected: \"Select a valid load mode; derived commands require Additive.\\r...\"\r\n  But was:  \"Required condition IDs must be nonempty and unique.\"\r\n  -----------^\r\n"
]
```

M3:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.ExplicitRequestRejectsInvalidInputWithoutEffects(RequiredIds):   Expected string length 51 but was 82. Strings differ at index 0.\r\n  Expected: \"Required condition IDs must be nonempty and unique.\"\r\n  But was:  \"Load destinations are acquired by the owner, not supplied as ...\"\r\n  -----------^\r\n"
]
```

M4:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.ExplicitRequestRejectsInvalidInputWithoutEffects(Destination):   Expected: <System.ArgumentException>\r\n  But was:  <System.InvalidOperationException: Select a full Assets/.../*.unity scene path.\r\n  at TPLab.Core.ResourceManagement.SceneTarget.ValidateScenePath (System.String scenePath) [0x00097] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\ResourceManagement\\SceneTarget.cs:70 \r\n  at TPLab.Core.ResourceManagement.SceneTarget.Validate () [0x0001c] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\ResourceManagement\\SceneTarget.cs:51 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.BeginRequest (TPLab.Core.SceneManagement.SceneTransitionRequest request, System.Threading.CancellationToken callerToken) [0x001ec] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:355 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.TryTransitionAsync (TPLab.Core.SceneManagement.SceneTransitionRequest request, System.Threading.CancellationToken cancellationToken) [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:501 \r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass29_0.<CheckInvalid>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:248 \r\n  at NUnit.Framework.Assert.Throws (NUnit.Framework.Constraints.IResolveConstraint expression, NUnit.Framework.TestDelegate code, System.String message, System.Object[] args) [0x0002f] in <f577cd9686154d6ab10efd7e65e64366>:0 >\r\n"
]
```

M5:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.PolicyEvaluatesEveryConditionInAttachmentOrderAfterFalse:   Expected is <System.String[3]>, actual is <System.Collections.Generic.List`1[System.String]> with 1 elements\r\n  Values differ at index [1]\r\n  Missing:  < \"condition-1\", \"condition-2\" >"
]
```

M6:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory:   Expected: <System.AggregateException>\r\n  But was:  <System.InvalidOperationException>\r\n"
]
```

M7:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory:   Expected: same as <System.InvalidOperationException: scene-b-transition\r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass33_0.<CheckFailures>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:310 \r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.PreparePresentationAsync (UnityEngine.SceneManagement.Scene scene, TPLab.Core.Lifecycle.ISceneRoot root, System.Threading.CancellationToken cancellationToken) [0x00029] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:51 \r\n  at TPLab.Core.SceneManagement.GameSceneManager+<>c__DisplayClass92_0.<LoadAndPrepareAsync>b__3 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at (wrapper delegate-invoke) System.Func`1[Cysharp.Threading.Tasks.UniTask].invoke_TResult()\r\n  at TPLab.Core.SceneManagement.GameSceneManager.InvokeAsync (System.Func`1[TResult] hook) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1387 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadAndPrepareAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x00775] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadWithPresentationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x001e6] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:759 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0032d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:97 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0053f] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:133 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RunOperationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x0014e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:582 >\r\n  But was:  <System.ApplicationException: scene-b-loading-release\r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass33_0.<CheckFailures>b__1 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:311 \r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.ReleaseLoadingPresentationAsync (TPLab.Core.SceneManagement.SceneLoadingContext context) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:87 \r\n  at TPLab.Core.SceneManagement.GameSceneManager+<>c__DisplayClass97_0.<ReleaseLoadingUiAsync>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:806 \r\n  at (wrapper delegate-invoke) System.Func`1[Cysharp.Threading.Tasks.UniTask].invoke_TResult()\r\n  at TPLab.Core.SceneManagement.GameSceneManager.InvokeAsync (System.Func`1[TResult] hook) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1387 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.ReleaseLoadingUiAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x0005d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:806 \r\n  at Cysharp.Threading.Tasks.UniTask+ExceptionResultSource.GetResult (System.Int16 token) [0x00015] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTask.Factory.cs:326 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RunOperationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x003e7] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:620 >\r\n"
]
```

M8:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory:   Expected: same as <System.AggregateException: One or more errors occurred. (scene-b-transition) (scene-b-loading-release) ---> System.InvalidOperationException: scene-b-transition\r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass33_0.<CheckFailures>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:310 \r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.PreparePresentationAsync (UnityEngine.SceneManagement.Scene scene, TPLab.Core.Lifecycle.ISceneRoot root, System.Threading.CancellationToken cancellationToken) [0x00029] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:51 \r\n  at TPLab.Core.SceneManagement.GameSceneManager+<>c__DisplayClass92_0.<LoadAndPrepareAsync>b__3 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at (wrapper delegate-invoke) System.Func`1[Cysharp.Threading.Tasks.UniTask].invoke_TResult()\r\n  at TPLab.Core.SceneManagement.GameSceneManager.InvokeAsync (System.Func`1[TResult] hook) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1387 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadAndPrepareAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x00775] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadWithPresentationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x001e6] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:759 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0032d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:97 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0053f] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:133 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RunOperationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x0014e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:582 \r\n   --- End of inner exception stack trace ---\r\n---> (Inner Exception #0) System.InvalidOperationException: scene-b-transition\r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass33_0.<CheckFailures>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:310 \r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.PreparePresentationAsync (UnityEngine.SceneManagement.Scene scene, TPLab.Core.Lifecycle.ISceneRoot root, System.Threading.CancellationToken cancellationToken) [0x00029] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:51 \r\n  at TPLab.Core.SceneManagement.GameSceneManager+<>c__DisplayClass92_0.<LoadAndPrepareAsync>b__3 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at (wrapper delegate-invoke) System.Func`1[Cysharp.Threading.Tasks.UniTask].invoke_TResult()\r\n  at TPLab.Core.SceneManagement.GameSceneManager.InvokeAsync (System.Func`1[TResult] hook) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1387 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadAndPrepareAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x00775] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:735 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.LoadWithPresentationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation, System.Threading.CancellationToken token) [0x001e6] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:759 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0032d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:97 \r\n  at TPLab.Core.Lifecycle.SceneRootFlow.RunAsync (System.Func`2[T,TResult] proceedAsync, System.Boolean release, System.Threading.CancellationToken cancellationToken) [0x0053f] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\Lifecycle\\SceneRootFlow.cs:133 \r\n  at Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1[TStateMachine].GetResult (System.Int16 token) [0x00000] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\CompilerServices\\StateMachineRunner.cs:218 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RunOperationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x0014e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:582 <---\r\n\r\n---> (Inner Exception #1) System.ApplicationException: scene-b-loading-release\r\n  at TPLab.Core.Tests.GameSceneManagerSafetyNetBTests+<>c__DisplayClass33_0.<CheckFailures>b__1 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\PlayMode\\GameSceneManagerSafetyNetBTests.cs:311 \r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.ReleaseLoadingPresentationAsync (TPLab.Core.SceneManagement.SceneLoadingContext context) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:87 \r\n  at TPLab.Core.SceneManagement.GameSceneManager+<>c__DisplayClass97_0.<ReleaseLoadingUiAsync>b__0 () [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:806 \r\n  at (wrapper delegate-invoke) System.Func`1[Cysharp.Threading.Tasks.UniTask].invoke_TResult()\r\n  at TPLab.Core.SceneManagement.GameSceneManager.InvokeAsync (System.Func`1[TResult] hook) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1387 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.ReleaseLoadingUiAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x0005d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:806 \r\n  at Cysharp.Threading.Tasks.UniTask+ExceptionResultSource.GetResult (System.Int16 token) [0x00015] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTask.Factory.cs:326 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RunOperationAsync (TPLab.Core.SceneManagement.GameSceneManager+Operation operation) [0x003e7] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:620 <---\r\n>\r\n  But was:  <System.InvalidOperationException: failure-callback\r\n  at TPLab.Core.Tests.SceneTransitionCallbacksProbe.OnFailure (System.Exception exception) [0x00028] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Tests\\Fixtures\\SceneTransitionCallbacksProbe.cs:98 \r\n  at TPLab.Core.SceneManagement.GameSceneManager.RecordFailure (System.Exception failure) [0x0003d] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\Core\\SceneManagement\\GameSceneManager.cs:1366 >\r\n"
]
```

M9:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.FailureAggregationPreservesOriginalObjectsPhaseAndSharedHistory:   Expected: PreparingPresentation\r\n  But was:  Idle\r\n"
]
```

M10:

```json
[
  "TPLab.Core.Tests.GameSceneManagerSafetyNetBTests.RegisteredReadFailureClearsPartialContentsWithoutRestoringExpandedCapacity:   Failure clears contents without restoring old capacity.\r\n  Expected: 4\r\n  But was:  0\r\n"
]
```

## 보존·한계·미실행

동결 신규 파일 SHA256:

- `Assets/TPLab/Tests/PlayMode/GameSceneManagerSafetyNetBTests.cs`: `7b2728b62df9d556b39845875931b7051be76f25674de125d4ac4aa7c7512588`
- `Assets/TPLab/Tests/PlayMode/GameSceneManagerSafetyNetBTests.cs.meta`: `1fc648d6032e3739136bcc1c0feb72f8801e14c0c8fc0109272cdd2677934c14`

문서 작성 전 기존 추적1200파일 raw SHA256과 dirty6 bytes가 작업 시작 snapshot과 같음을 확인했다. 문서 작성 후 허용된 기존 파일 변경은 회고 INDEX 하나뿐이어야 하며 commit 전에 재확인한다. runtime·기존 테스트·assembly/UPM diff0. compile·제품 Console·규칙/공백/CI는 아래 제공 단계 기록 및 최종 채팅으로 구분한다.

미실행: Player/IL2CPP, 새 GC 계측·할당 보증, 외부 소비 프로젝트, 실제 Addressables load, 최종 시각 UX. 이번 native scene 검증은 BuildScene backend다. hook 자기 대기 순환·외부 UI 파괴·runtime Timeout 변경·메시지 통일은 고정하지 않았다. 버그 의심으로 제외한 case0. capacity fault 주입은 private _derived/owner Root reflection 위치에 의존한다; 책임 분리 시 주입 위치 조정이 필요할 수 있으나 public 관찰 단언 약화 근거가 되지 않는다.

Temp는 source/정정 전 cs/patch·runtime backup·실행 JSON/driver·임시 도구를 이 회고에 요약한 뒤 CI·진행 참조가 끝나면 이번 작업 명시 목록만 정리한다. 중단 시 Temp/scene-safetynet-b-20261015를 재개에 쓰며 그 조건 완료 전 보존한다. 다른 Temp·사용자 자료를 정리하지 않는다.

## 전체 안전망·D-C 시작 판단 자료

기존50case+이번7case=57case의 추가 안전망 범위다. 전체777case의 모든 보호 범위를 뜻하지 않으며 “이 묶음이 보호하지 않음”이 다른 기존 기능 테스트 부재를 의미하지 않는다.

| 묶음·case | 보호하는 경계 | 이 묶음으로 보호하지 못한 경계 |
|---|---|---|
| [UI-A](2026-10-14-07-uicontext-safetynet-a.md) ·18 | worker11개 정확한 거부·무효과·재사용; 동시 검증 우선순위6쌍; Displays의 수락 순서 분리 snapshot | 나머지 우선순위 쌍·비guard 구독/getter·전체 async hook 경로·Player |
| [Scene-A](2026-10-14-08-gamescene-safetynet-a.md) ·17 | 명령 overload13 worker 거부; 생성자 thread 우선1; null buffer 우선2; 이력 없는 Wait1 | 전체 graph/모드별 실패·재진입·늦은 완료 조합, Scene-B invalid/집계/확장 capacity 경계 |
| [Guard-A](2026-10-14-09-guard-safetynet-a.md) ·11 | DataTable/Resource 우선순위; LoadedScene 생성/시작/await후; observer 기록; Input lease 및 bound/unbound; 서로 다른 thread 판별 계약 | 모든 public API의 thread 조합·실제 병렬 IO 전체·thread 계약 통합 적합성·Player |
| [UI-B](2026-10-14-10-uicontext-safetynet-b.md) ·4 | cleanup/다중 display 집계 정체성·수락 순서/잔여 상태; 비협조 opening 취소/native retirement·close hook pending 중 owner 종료 | H6 hook 자체 취소/release 책임·자기 대기 순환·외부 UI 파괴·새 Timeout·새 GC |
| Scene-B ·7 | invalid4쌍 정확한 오류/무효과; false 뒤 모든 조건 순서/횟수; 겹친 실패 phase·SameAs 집계/history·정리; 부분 읽기 실패 확장 capacity | 나머지 invalid 조합·전체 graph/모드/늦은 완료·실제 Addressables·hook 순환/외부 파괴/Timeout·새 GC·Player |

관찰: 이번 gate는 선택된 경계의 분리 검토 근거를 보탠다. D-C 시작·분리 대상 결정은 사용자에게 남긴다. [D-H1](../guidelines/01-code-conventions.md)에 따라 책임·소유권 경계를 정하고, 옮길 동작이 위 미보호 영역이면 D-F에 따라 필요한 테스트부터 추가한다. 이 PR은 runtime 분리·공개 API 변경·정책 정규화 권한을 부여하지 않는다. 사용자 수락·병합 승인 및 실제 옮길 경계 결정이 남아 있다.

## 정적 검사·제품 Console

전체 PlayMode 뒤 Console error 조회10건은 모두 기존 테스트의 기대 로그와 정확한 수가 일치했다. MonoSingletonTests:119/135/147/148/169/207의 init2·shutdown2·초기화 중 파괴1·root 제약1, ResourceManagerTests:212/499의 provider failure2, SceneRootTests:116/137의 install/uninstall failure 각1이다. 신규 Scene-B 로그는0이고 모든 case의 LogAssert.NoUnexpectedReceived가 유지됐다. Console을 지우거나 error=[]로 보고하지 않았다. compile error CS 로그0.
