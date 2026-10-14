# UIContext 특성화 안전망 B — 2026-10-14

상태: T2 테스트 전용, 원본 정상본·함수 범위 변형 검증·전체 회귀 및 반복 완료. PR 제공 후 사용자 병합 승인 대기이며 main 병합 권한으로 승계하지 않는다.

## 범위·검증 source

기준 main/source는 `eed386a6b07d3f04aba25786949cc5c67f3cb686`(PR #23 병합)이다. 검증 대상은 이 runtime 원본과 아래 동결된 신규 cs/meta 4개 조합이며, 실행 후 commit metadata와 구별한다. 기존 UI EditMode/PlayMode assembly를 사용한다. runtime·기존 테스트·assembly/참조·packages·UPM·XML·사람/AI API 변경은0이다. public 계약을 바꾸지 않는 특성화이므로 API 문서 갱신은 비적용이다.

| mode·case(각1) | 신규 파일 | 실제 보호 범위 |
|---|---|---|
| EditMode · `CleanupFailureSharesOriginalAggregateAndLeavesOwnerUsable` | [source:80](../../Assets/TPLab/UI/Tests/EditMode/UIContextSafetyNetBEditModeTests.cs#L80) | close·cleanup·Closed 실패의 원본 객체/타입/집계 순서, close/Closed/repeated 공유 예외 정체성, Fault null·owner 사용 가능, 이후 정상 종료 |
| PlayMode · `NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes` | [source:121](../../Assets/TPLab/UI/Tests/PlayMode/UIContextSafetyNetBPlayModeTests.cs#L121) | Open hook gate 미완료 상태에서 caller 취소, Opened는 native retirement·Closed 이후 취소 완료, owner 유지·정상 후속 display 사용 |
| PlayMode · `OwnerShutdownRetiresWhileNonCooperativeCloseHookRemainsPending` | [source:191](../../Assets/TPLab/UI/Tests/PlayMode/UIContextSafetyNetBPlayModeTests.cs#L191) | visible close hook을 먼저 시작한 뒤 owner shutdown; 취소로 core wait가 분리되어 hook gate 미완료여도 Close·shutdown 성공, disposed·잔여 상태 |
| PlayMode · `ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement` | [source:254](../../Assets/TPLab/UI/Tests/PlayMode/UIContextSafetyNetBPlayModeTests.cs#L254) | 독립 display 2개의 cleanup·Closed 실패 4개를 원본 객체로 보존; owner는 수락 순서의 display별 AggregateException 2개를 공유·모든 native retirement 완료 |

확정: EditMode1+PlayMode3=4case, 제외0. 결정적 재현 불가 항목0, 정상본에서 버그 의심으로 중단한 항목0. hook 자기 대기 순환, 외부 UI 파괴, runtime Timeout 정책·변경 제안, 메시지 통일은 고정하지 않았다.

예외는 `Task.Exception`의 자동 wrapper가 아니라 완료된 Task를 `GetAwaiter().GetResult()`로 await할 때 호출자에게 전달되는 객체를 관찰한다. EditMode close의 정확한 타입은 AggregateException이며, InnerExceptions는 원본 ApplicationException(close)·InvalidOperationException(cleanup)·NotSupportedException(Closed) 객체 순이다. repeated close·Closed는 같은 AggregateException 객체를 공유한다. callback 오류는 native Fault가 아니며 context는 disposed가 아니고 계속 사용 가능하다. 정상 후속 display를 닫고 shutdown하면 disposed=true, Fault=null로 성공하며 이미 retired된 오류를 다시 집계하지 않는다. 다중 owner shutdown의 실패는 각 display의 AggregateException 2개를 수락 순서로 감싼 AggregateException이며 두 계층의 원본 객체 정체성을 검사한다.

## 환경·명령·정상본·전체 회귀

Windows / Unity6000.3.18f1 / UTF1.6.0 / UniTask2.5.11 / Connector0.4.1 / 기존 Editor PID27224 / C:\Python314\python.exe. CLI에 항상 절대 TPLab project를 지정하고 컴파일 idle·실제 UTF IsRunActive=false를 reflection으로 확인한 뒤 source를 변경했다. 새 Editor·다른 프로젝트·stale 결과로 대체하지 않았다.

실제 runner 명령 형식은 다음과 같다. 각 label의 고유 output·동일 CLI PID/fresh timestamp/nonzero 결과로 실행을 식별했다. Temp wrapper는 해당 영속 runner를 호출해 driver wall/원문만 기록하며 영속 문서는 일회성 wrapper에 의존하지 않는다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/ui-safetynet-b-20261014/baseline-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/ui-safetynet-b-20261014/baseline-play.json
```

Targeted는 같은 명령에 `--filter <fixture 또는 아래 full case>`와 고유 label output을 지정했다. `normal-edit` filter는 TPLab.UI.Tests.UIContextSafetyNetBEditModeTests, `normal-play`/`normal-play-v2`는 TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests다. 전체 final/repeat filter는 비어 있다.

| label | mode | total/pass/fail/skip | CLI PID · source(runId) | driver wall(초) |
|---|---|---|---|---|
| `baseline-edit` | EditMode | 397/397/0/0 | 46920 · cli (-) | 미측정 |
| `baseline-play` | PlayMode | 369/369/0/0 | 44024 · cli (-) | 미측정 |
| `normal-play` | PlayMode | 3/3/0/0 | 46256 · cli (-) | 미측정 |
| `play-file-alone-full` | PlayMode | 372/372/0/0 | 48780 · connector-file (48780-1791966264033245700) | 미측정 |
| `normal-edit` | EditMode | 1/1/0/0 | 50044 · cli (-) | 3.0662 |
| `normal-play-v2` | PlayMode | 3/3/0/0 | 52540 · cli (-) | 15.5385 |
| `final-edit` | EditMode | 398/398/0/0 | 54724 · cli (-) | 31.5913 |
| `final-play` | PlayMode | 372/372/0/0 | 22060 · cli (-) | 31.4984 |
| `repeat-edit` | EditMode | 398/398/0/0 | 55532 · cli (-) | 26.3338 |
| `repeat-play` | PlayMode | 372/372/0/0 | 53580 · cli (-) | 31.5423 |

Guard-A 교훈 적용: PlayMode 신규 파일만 먼저 포함하여 targeted3과 전체372를 통과한 뒤 EditMode 파일을 추가했다. 이후 opening 취소 결과가 Closed·native retirement 이후 공개되는 순서 단언을 보강하고 정상본3을 재통과한 뒤 모든 신규 cs/meta를 동결했다. fixture는 inactive plain GameObject source와 명시적 owner만 만들고 EventSystem·입력 서비스·modal lease를 연결하지 않는다; InputSystem 전역 상태를 조작하지 않아 InputTestFixture는 비적용이며 기존 UI assembly 참조를 늘리지 않았다. TearDown은 lazy outer IEnumerator 안에서 gate·hook 작업·shutdown·native 파괴를 완료한 뒤 같은 LogAssert.NoUnexpectedReceived를 수행한다. 기대값 약화·정상본 실패 후 fixture 정정·case 제외는0이다.

기존 이름 집합 보존: final/repeat 모두 EditMode397개+신규1개, PlayMode369개+신규3개와 정확히 일치했다(집합 비교). 최초 Play 단독 전체도369+3과 일치했다. 정상본 전체 실패0이라 신규 제외/그룹별 재격리를 실행할 조건은 발생하지 않았다.

실행 전 infrastructure 한계: Play 단독 전체의 최초 시도는 Console preflight에서 `RuntimeError: Resolve compilation errors before testing; old Connector results are not evidence: Error: no Unity instances running`으로 거부되어 테스트가 시작되지 않았다. PID27224 ready·compile/update/play/test inactive·Console error=[]를 다시 확인하고 실행했다. 이 미시작을 test pass/fail로 세지 않았다. Play 단독 전체 CLI PID48780, M2 green CLI PID51236은 domain reload 후 nonzero CLI exit였지만 영속 runner가 그 PID의 fresh Connector runId를 회수했다; 다른 실행 결과 재사용이 아니다. 첫 Editor 상태 probe는 internal IsRunActive를 public으로 호출한 동적 exec 컴파일 오류를 반환했다; 설치 UTF 소스의 internal static 정의를 확인하고 reflection으로 정정했으며 자산·runtime·테스트 소스를 수정한 것이 아니다.

## 임시 변형·원복 검증

변형은 UIContext.cs의 RunOpenAsync/RunCloseAsync/RunShutdownAsync 함수 body를 좁힌 unique pattern만 변경했다. 원본 일치를 확인한 후 한 변형씩 적용했고 compile·tests idle을 확인한 뒤 원본 bytes로 복구했다. 범위 밖 source/기존 테스트는 변경하지 않았다. 6개 패턴의 unique preflight와 duplicate input 거부6/6을 쓰기 전에 확인했다. 신규 cs/meta hash는 변형 전후 동일하며 민감도 미확인 case0(4개 case 모두 최소1변형에서 실제 실패)이다. mutant 실패는 의도한 assertion이며 [Timeout] 중단은0; gate는 finally와 TearDown에서 해제하고 hook/core cleanup을 끝낸 뒤 다음 실행을 시작했다.

| ID·함수 | 변형 내용 | 실패 case | Red total/pass/fail/skip · CLI | 실제 실패 요점 | 원복 Green · CLI |
|---|---|---|---|---|---|
| M1 · `RunCloseAsync` | Close failure wrapper gains an extra AggregateException layer | `CleanupFailureSharesOriginalAggregateAndLeavesOwnerUsable` | 1/0/1/0 · 51396 | 집계3개가 추가 wrapper1개로 바뀜 | 1/1/0/0 · 38108 |
| M2 · `RunOpenAsync` | Opening ignores external cancellation while project hook is pending | `NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes` | 1/0/1/0 · 49588 | core completion이 gate 미완료에 묶여 helper 상한 실패 | 1/1/0/0 · 51236 |
| M3 · `RunCloseAsync` | Closing ignores owner cancellation while project hook is pending | `OwnerShutdownRetiresWhileNonCooperativeCloseHookRemainsPending` | 1/0/1/0 · 52332 | shutdown이 close hook gate에 묶여 helper 상한 실패 | 1/1/0/0 · 53000 |
| M4 · `RunShutdownAsync` | Owner error collection uses reverse acceptance order | `ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement` | 1/0/1/0 · 44044 | first display 오류 객체 대신 second 객체 | 1/1/0/0 · 53692 |
| M5 · `RunShutdownAsync` | Owner error collection omits the second display error | `ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement` | 1/0/1/0 · 51144 | owner 집계2개 중1개 누락 | 1/1/0/0 · 1404 |
| M6 · `RunOpenAsync` | Opening cancellation is published before native retirement | `NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes` | 1/0/1/0 · 53124 | Closed 전에 Opened 취소가 공개됨 | 1/1/0/0 · 48904 |

### 실패 원문

runner failures 원문 문자열 전체를 JSON으로 가역 기록했다. CRLF·trailing space도 escape로 보존하며 실패 문장·stack을 삭제하거나 의역하지 않는다.

M1:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBEditModeTests.CleanupFailureSharesOriginalAggregateAndLeavesOwnerUsable:   Expected: 3\r\n  But was:  1\r\n"
]
```

M2:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes:   Completion must settle within 100 yields and 10 seconds.\r\n  Expected: True\r\n  But was:  False\r\n"
]
```

M3:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.OwnerShutdownRetiresWhileNonCooperativeCloseHookRemainsPending:   Completion must settle within 100 yields and 10 seconds.\r\n  Expected: True\r\n  But was:  False\r\n"
]
```

M4:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement:   Expected: same as <System.AggregateException: One or more errors occurred. (first-cleanup) (first-closed) ---> System.InvalidOperationException: first-cleanup\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__0 () [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:275 \r\n  at TPLab.UI.UIContext.DispatchCleanup (System.Action cleanup) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:647 \r\n  at TPLab.UI.UIHandle+CleanupRegistration.Invoke () [0x0003b] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:331 \r\n  at TPLab.UI.UIHandle.RunCleanup () [0x00011] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:266 \r\n   --- End of inner exception stack trace ---\r\n  at Cysharp.Threading.Tasks.UniTaskCompletionSource.GetResult (System.Int16 token) [0x00026] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTaskCompletionSource.cs:649 \r\n  at Cysharp.Threading.Tasks.UniTaskExtensions+<>c.<AsTask>b__35_0 (System.Object state) [0x00016] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTaskExtensions.cs:164 \r\n--- End of stack trace from previous location where exception was thrown ---\r\n\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.Error (System.Threading.Tasks.Task task) [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:111 \r\n---> (Inner Exception #0) System.InvalidOperationException: first-cleanup\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__0 () [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:275 \r\n  at TPLab.UI.UIContext.DispatchCleanup (System.Action cleanup) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:647 \r\n  at TPLab.UI.UIHandle+CleanupRegistration.Invoke () [0x0003b] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:331 \r\n  at TPLab.UI.UIHandle.RunCleanup () [0x00011] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:266 <---\r\n\r\n---> (Inner Exception #1) System.ApplicationException: first-closed\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__2 (TPLab.UI.UIHandle _) [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:269 \r\n  at TPLab.UI.UIContext.DispatchClosed (TPLab.UI.UIHandle handle, System.Action`1[T] callback) [0x00012] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:634 \r\n  at TPLab.UI.UIContext.RunCloseAsync (TPLab.UI.UIHandle handle, System.Boolean wasVisible, System.Collections.Generic.List`1[T] children) [0x00801] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:1103 <---\r\n>\r\n  But was:  <System.AggregateException: One or more errors occurred. (second-cleanup) (second-closed) ---> System.InvalidOperationException: second-cleanup\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__1 () [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:276 \r\n  at TPLab.UI.UIContext.DispatchCleanup (System.Action cleanup) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:647 \r\n  at TPLab.UI.UIHandle+CleanupRegistration.Invoke () [0x0003b] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:331 \r\n  at TPLab.UI.UIHandle.RunCleanup () [0x00011] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:266 \r\n   --- End of inner exception stack trace ---\r\n  at Cysharp.Threading.Tasks.UniTaskCompletionSource.GetResult (System.Int16 token) [0x00026] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTaskCompletionSource.cs:649 \r\n  at Cysharp.Threading.Tasks.UniTaskExtensions+<>c.<AsTask>b__35_0 (System.Object state) [0x00016] in .\\Library\\PackageCache\\com.cysharp.unitask@d648f5692cf2\\Runtime\\UniTaskExtensions.cs:164 \r\n--- End of stack trace from previous location where exception was thrown ---\r\n\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.Error (System.Threading.Tasks.Task task) [0x00000] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:111 \r\n---> (Inner Exception #0) System.InvalidOperationException: second-cleanup\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__1 () [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:276 \r\n  at TPLab.UI.UIContext.DispatchCleanup (System.Action cleanup) [0x0000e] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:647 \r\n  at TPLab.UI.UIHandle+CleanupRegistration.Invoke () [0x0003b] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:331 \r\n  at TPLab.UI.UIHandle.RunCleanup () [0x00011] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIHandle.cs:266 <---\r\n\r\n---> (Inner Exception #1) System.ApplicationException: second-closed\r\n  at TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests+<>c__DisplayClass17_0.<CheckMultipleFailures>b__3 (TPLab.UI.UIHandle _) [0x00010] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Tests\\PlayMode\\UIContextSafetyNetBPlayModeTests.cs:273 \r\n  at TPLab.UI.UIContext.DispatchClosed (TPLab.UI.UIHandle handle, System.Action`1[T] callback) [0x00012] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:634 \r\n  at TPLab.UI.UIContext.RunCloseAsync (TPLab.UI.UIHandle handle, System.Boolean wasVisible, System.Collections.Generic.List`1[T] children) [0x00801] in C:\\Users\\PC\\Projects\\TPLab\\Assets\\TPLab\\UI\\Runtime\\UIContext.cs:1103 <---\r\n>\r\n"
]
```

M5:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement:   Expected: 2\r\n  But was:  1\r\n"
]
```

M6:

```json
[
  "TPLab.UI.Tests.UIContextSafetyNetBPlayModeTests.NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes:   Opened cancellation is published after display cleanup completes.\r\n  Expected: True\r\n  But was:  False\r\n"
]
```

## 대기 상한·H6 경계

모든 신규 case는 [Timeout(30000)]를 사용한다. async completion helper는100 yield와 Stopwatch10초 중 먼저 오는 상한으로 완료를 단언하며, PlayMode는 NextFrame, EditMode는 Yield를 사용한다. 네 동작은 메인 스레드의 async/native 수명만 관찰하므로 worker 생성·Thread.Join은 비적용이다. external IO 없이 gate/Unity destruction 몇 frame이 정상 경로이며 원본 targeted driver wall은 EditMode1건3.0662초, PlayMode3건15.5385초였다; 이 값은 CLI/Editor/Connector overhead를 포함한 driver 시간이고 case 자체 시간으로 해석하지 않는다. helper100yield/10초와 outer30초는 테스트 hang 실패 상한이며 runtime deadline 정책을 새로 정하거나 정상 완료 시간을 계약으로 고정하지 않는다. UTF의 UnityTest Timeout 강제 적용은 이번에 재실험하지 않았고 helper 완료 단언을 실질 오라클로 사용했다.

H6: UIContext는 취소 후 core wait/owned native state를 끝내도 프로젝트 hook 작업을 강제 중단하지 않는다. 테스트는 아직 pending인 hook task를 확인한 뒤 finally에서 자신의 gate를 해제하고 task를 await하며, 취소 후 clone·서비스에 접근하지 않는다. hook 작성자가 취소·release 수명을 책임진다는 [현행 결정](../guidelines/03-async-lifecycle.md)의 의미를 정규화하거나 변경하지 않는다.

## 보존·미실행·검사

문서 작성 전 기준선 추적1195파일 raw SHA256과 dirty6 bytes가 모두 동일했다. runtime/UIContext 원본 SHA256은 `c0caf7871b1ad30a8aeac3e3f97b3d2b78f3d527cbd3afd8f3e121f3ea26d753`이며 변형 후 동일 bytes로 원복했다. 신규 GUID2개는 Assets meta 전수 대조에서 각각 자기 파일1개만 일치했다. 변경 allowlist는 신규 cs2+meta2+본 회고+회고 INDEX이며 다른 파일 stage/commit은 금지한다.

| 동결 파일 | SHA256 |
|---|---|
| `Assets/TPLab/UI/Tests/EditMode/UIContextSafetyNetBEditModeTests.cs` | `d07d32b7971bcdc68475f4351ecc002a1bbbdef3db0233ff0ae978e9f18f0891` |
| `Assets/TPLab/UI/Tests/EditMode/UIContextSafetyNetBEditModeTests.cs.meta` | `15a763a64062cdef2b4ef5929b602b110a5e3ced099c1ae252fff08c87b170ff` |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextSafetyNetBPlayModeTests.cs` | `5da27f2a0978f9f4993dfe3c67d33543345d30447e3499159450d6662ffecf2c` |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextSafetyNetBPlayModeTests.cs.meta` | `0a7189127c89ee0660241567b4bb49d997a5df2c6b6ee31bab8ee3fd974c61ca` |

의심 동작: 승인된 네 범위의 원본 실행에서 버그로 판단할 결과0. 비협조 hook이 core 종료 뒤에도 남을 수 있다는 점은 H6와 XML에 따른 관찰이며 강제 중단·late fault 보관의 신규 정책으로 고정하지 않는다. callback 자기 대기·외부 UI 파괴·입력 전역 상태·runtime Timeout 변경은 별도 검증/결정 대상으로 남긴다.

미실행: Player/IL2CPP, 신규 GC 계측, 실제 제품 시각/입력 UX 수동 확인, artifact 재생성·consumer 빌드/배포, Unity 테스트 Actions 실행, main 병합·브랜치 삭제. Actions는 PR 규칙 검사이며 로컬 Editor 실행을 대체하지 않는다. 정상본 fixture 실패/정정0이므로 최초 실패·정정 이력으로 기재할 test failure는 없다; infrastructure 미시작과 모든 의도한 mutant 실패는 위에 구분해 보존했다.

이번 실행 결과·변형 원본 backup·일회성 helper는 Temp/ui-safetynet-b-20261014의 작업 소유 파일이다. 최종 요약·HEAD Actions 확인 후 진행 중 참조/실행이 없는 것을 확인하고 명시 목록으로 제거한다. 재사용 영속 도구와 기존 기록은 수정·삭제하지 않는다.

Console 별도 관찰: 최종 전체 suite 뒤 error/warning14개 중10개는 기존 Singleton/Resource/SceneRoot 테스트의 예상 exception 로그이며4개는 기존 InputSystemUiScopeGuardSafetyNetTests의 UI/Point(Value)·UI/Click(Button) 설정 warning이다. 이번 신규 UI fixture 기원 로그0, compile CS 오류0이다. 전체 제품 Console이 비었다고 주장하지 않는다. 해당 기존 warning과 설정은 범위 밖 발견으로 보존하고 수정하지 않는다. 전체 working-tree 공백 검사는 기존 dirty InitScene.unity:164·276·398의 m_Name 뒤 공백3건을 보고했다; 기준선 raw hash와 같음을 확인했으며 작업 diff 공백 검사와 구분하고 수정하지 않는다.

최종 링크·공백·규칙 검사와 exact-SHA CI metadata는 아래 후속 기록으로 추가한다.
