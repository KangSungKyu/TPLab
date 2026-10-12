# IMP-04 적용 · 선택한 PlayMode 테스트의 30초 보호

- BaseRevision: main `d6163084bb2d7dc0420941ad9e35555ef900cab5`. PR #5 merged 및 최신 main을 확인하고 `codex/imp-04-apply`를 생성했다.
- Scope / Decision: 첨부 요청의 D-05a~c, 정확히24개 PlayMode 게이트 메서드에 `[Timeout(30000)]` 추가. 기존 Wait/Settle/Pending/Complete·조건 대기·본문·assertion은 유지한다.
- ImplementationStatus: 속성24개 추가 완료. runtime/public API·패키지·설정·runner 변경 없음. UI·Input·EditMode 소스는 변경하지 않았다.
- ValidationStatus: 전후 같은151개 case 통과, 전체 Edit319/319·Play306/306, 실패/skip0. 최종 PR 사용자 확인 대기; main 병합·다음 IMP 항목은 시작하지 않는다.
- Source evidence: [적용된7개 파일 SHA256·원본 보존](source-preservation.json), [환경](environment.json). 이 보고서의 base SHA를 변경 후 테스트 source SHA로 혼동하지 않는다. 최종 commit/정확한 HEAD Actions는 PR 본문·완료 보고에 기록한다.
- 선행 근거: [1단계 조사](../imp-04-survey/README.md), [백로그 D-05a~c](../../IMPROVEMENT_BACKLOG.md#결정-기록-사용자가-작성), [문서 지침](../../DOCUMENTATION_GUIDE.md).

## 메서드 종류·적용 목록

24개 모두 공개 `IEnumerator [UnityTest]`이며 메서드 선언 자체의 async, `[Test]`, `[TestCase]`, 기존 `[Timeout]`은 없다. async lambda를 coroutine으로 반환하는19개, async helper를 coroutine으로 반환하는5개를 구분했다. lambda/helper의 async와 공개 테스트 메서드의 반환형을 혼동하지 않는다. **제외 목록0개**. [정확한 심볼·전후 행·속성](target-inventory.json).

아래 파일은 `Assets/TPLab/Tests/PlayMode/<Class>.cs`이다. 선언 행은 기준main → 적용 source 순서, 별도 Timeout 속성은 적용 선언 행의 바로 앞이다. 종류/기존 특수 속성을 모두 대조했다. 시간은 변경 후 해당 필터의 실제 UTF case duration이다.

| 클래스.메서드 | 선언 행 전→후 | 종류 | 기존 TestCase/Timeout | after case 초 |
|---|---:|---|---|---:|
| AsyncSceneRootTests.CoverAndAllPreparationAndScenePresentationAreAwaited | 128 → 129 | IEnumerator / UnityTest | 없음 | 0.0081364 |
| AsyncSceneRootTests.ReleaseWaitsUnderCoverAndCleansInReverseOrder | 158 → 160 | IEnumerator / UnityTest | 없음 | 0.0087263 |
| AsyncSceneRootTests.SingletonRegistrationDoesNotAllowEarlySceneProgress | 320 → 323 | IEnumerator / UnityTest | 없음 | 0.0070932 |
| GameSceneReplacementTests.SingleWaitsForOldGracefulShutdownBeforeNativeLoad | 131 → 132 | IEnumerator / UnityTest | 없음 | 0.0756941 |
| GameSceneReplacementTests.OwnerCancellationWaitsForLateCandidateThenPreservesOldRoot | 201 → 203 | IEnumerator / UnityTest | 없음 | 0.0189960 |
| GameSceneReplacementTests.ShutdownSharesWaitForLateCandidateAndCleansEveryOwnedScene | 224 → 227 | IEnumerator / UnityTest | 없음 | 0.0198718 |
| GameSceneReplacementTests.CallerCancellationStopsOnlyItsReplacementWait | 247 → 251 | IEnumerator / UnityTest | 없음 | 0.0403758 |
| GameSceneReplacementTests.CompletedOperationAwaiterCanImmediatelyStartAndCancelTheNextOperation | 442 → 447 | IEnumerator / UnityTest | 없음 | 0.0446839 |
| GameSceneAreaTests.PendingSameRemovalSharesResultAfterAuthorityValidationAndCallerCancellation | 226 → 227 | IEnumerator / UnityTest | 없음 | 0.0255448 |
| GameSceneAreaTests.OtherCommandsAreRejectedDuringRemovalWithoutEffects | 260 → 262 | IEnumerator / UnityTest | 없음 | 0.0279298 |
| GameSceneAreaTests.OwnerCancellationWaitsForLateAddedCandidateAndPreservesRegisteredTree | 304 → 307 | IEnumerator / UnityTest | 없음 | 0.0215001 |
| GameSceneAreaTests.ShutdownWaitsForLateAddedCandidateAndThenReleasesRegisteredTree | 369 → 373 | IEnumerator / UnityTest | 없음 | 0.0218144 |
| GameSceneAreaTests.OwnerCancellationDuringRemovalWaitsForEntireBegunSubtreeCleanup | 459 → 464 | IEnumerator / UnityTest | 없음 | 0.0343604 |
| GameSceneAreaTests.SingleCancellationDuringDescendantReleaseFinishesGracefulTreeWithoutNativeLoad | 483 → 489 | IEnumerator / UnityTest | 없음 | 0.0480443 |
| BootstrapPlayModeTests.CommonInstallerReentryIsRejectedAfterAsynchronousCover | 112 → 113 | IEnumerator / UnityTest | 없음 | 0.0136274 |
| BootstrapPlayModeTests.CallerCancellationDoesNotCancelSharedEntry | 404 → 406 | IEnumerator / UnityTest | 없음 | 0.0159000 |
| GameSceneEntryTests.DirectManagerSharesWaitWithoutCancellingItsOwner | 74 → 75 | IEnumerator / UnityTest | 없음 | 0.0386770 |
| SceneLoaderPlayModeTests.ExplicitSourceChoosesOnlyItsInjectedLoader | 386 → 387 | IEnumerator / UnityTest | 없음 | 0.5173133 |
| SceneLoaderPlayModeTests.OwnerCancellationWaitsForLateNativeResultAndCleansIt | 404 → 406 | IEnumerator / UnityTest | 없음 | 0.0115193 |
| SceneLoaderPlayModeTests.CallerCancellationLeavesNativeOwnerAndResultAlive | 423 → 426 | IEnumerator / UnityTest | 없음 | 0.0568574 |
| SceneLoaderPlayModeTests.WrongSceneResultIsRetainedThenCleanedWithoutReveal | 443 → 447 | IEnumerator / UnityTest | 없음 | 0.0121595 |
| SceneLoaderPlayModeTests.WrongTargetResultIsRetainedThenCleanedWithoutReveal | 446 → 451 | IEnumerator / UnityTest | 없음 | 0.0121746 |
| SceneTransitionPolicyTests.LateFalseBeforeSingleReleaseDoesNotShutdownOrLoad | 436 → 437 | IEnumerator / UnityTest | 없음 | 0.0634107 |
| SceneTransitionPolicyTests.LateFalseBeforeRemovalRetainsEntirePreparedSubtree | 457 → 459 | IEnumerator / UnityTest | 없음 | 0.0257012 |

## 전후 실행·시간 근거

사용자 요청에 따라 이번 테스트 보호 추가의 Red/Green은 **변경 전후 동일 필터 결과 비교**로 대체했다. 실패 재현/동작 수정 TDD Red→Green으로 표시하지 않는다. `tools/run_unity_tests.py --project <실제 TPLab 절대경로> --mode PlayMode --filter TPLab.Core.Tests.<Class> --output Temp/<이번 작업>/<결과>.json`을 동일한 기존 Editor PID24376에서 실행했다. [원시 전후 결과·타임스탬프·invocation](comparison-runs.json).

| 클래스 필터 | before total/pass/fail/skip | after total/pass/fail/skip | before driver 초 | after driver 초 |
|---|---|---|---:|---:|
| AsyncSceneRootTests | 15/15/0/0 | 15/15/0/0 | 29.198565 | 28.706187 |
| GameSceneReplacementTests | 36/36/0/0 | 36/36/0/0 | 13.356970 | 13.093530 |
| GameSceneAreaTests | 27/27/0/0 | 27/27/0/0 | 12.690708 | 13.092161 |
| BootstrapPlayModeTests | 21/21/0/0 | 21/21/0/0 | 12.759339 | 12.100233 |
| GameSceneEntryTests | 7/7/0/0 | 7/7/0/0 | 11.807478 | 11.606293 |
| SceneLoaderPlayModeTests | 19/19/0/0 | 19/19/0/0 | 12.739081 | 12.572038 |
| SceneTransitionPolicyTests | 26/26/0/0 | 26/26/0/0 | 13.219751 | 13.104406 |
| 합계 | 151/151/0/0 | 151/151/0/0 | 필터별 전체 시간 | 필터별 전체 시간 |

7개 필터의 case 이름 집합도 모두 동일하고 승인한24개가 각각 pass 목록에 있다. CLI domain reload로 returnCode1인 invocation도 해당 CLI PID·runId의 새 Connector 결과를 지정 runner가 회수했으며 stale 결과로 대체하지 않았다. 각 invocation의 실제 실행 source·CLI PID·Editor PID·접수 시도는 JSON에 보존했다.

개별 시간은 CLI 출력에 없으므로 **기존 Test Runner GUI가 보존한 최신 ITestResultAdaptor**를 각 after 필터 직후 읽었다. callback 등록·runner 소스·설정 변경이나 측정용 테스트 추가는 하지 않았다. 전체 case 이름/Passed 상태가 해당 CLI 결과와 같고 모든 case UTC 시작·종료가 해당 driver window 안에 있음을 확인한 뒤24개 선택 행을 사용했다. 과거 GUI 결과·필터 wall을 case 시간으로 표시하지 않는다. duration은 UTF가 보고한 정상 test timing이며 await별/cleanup별 분해 측정은 아니다.

관측한 대상 최대값은 `SceneLoaderPlayModeTests.ExplicitSourceChoosesOnlyItsInjectedLoader` **0.5173133초**이다. 30초는 약58배이며 **30초 근접 목록0개**다. AsyncSceneRoot 필터 전체 driver는 약29초지만 해당3개 case는 각각 약0.007~0.009초다. 이 차이에는 ready·Console·IPC·domain reload·결과 회수가 포함되므로 필터29초를 개별 테스트의 제한 근접으로 판단하지 않는다. 다른 기기/플랫폼의 최악값은 측정하지 않았다. 60초로 변경하거나 대상 외 속성을 추가하지 않았다.

[실제 compiled 속성 확인](compiled-attributes.json): 파일 수정 직후 기존 assembly의 count0을 확인했고 표준 AssetDatabase.Refresh 뒤 compile/import idle에서24개 모두 ctor argument30000으로 로드된 것을 확인했다. 소스만 편집한 상태로 변경 후 결과를 수집하지 않았다. timeout 만료 재실험은 이번 요청대로 수행하지 않았으며 [1단계 실제250ms PlayMode 실험](../imp-04-survey/timeout-experiment.json)과 구분한다.

## 전체 회귀·Editor/Console

백로그의 전체 회귀 gate에 따라 source를 동결한 뒤 같은 도구에서 필터 없이 실행했다. UI 소스 적용은 범위 밖이며 전체 회귀 실행은 소스 변경으로 취급하지 않는다. [원시 전체 결과](regression-runs.json).

| 실제 전체 실행 | total/pass/fail/skip | driver wall 초 |
|---|---|---:|
| EditMode | 319/319/0/0 | 68.534399 |
| PlayMode | 306/306/0/0 | 21.050150 |

[최종 Editor/Console](post-editor-state.json): Play/compile/import/testRun false, compiler error0. Console에는 기존 singleton 실패 주입6개·provider 실패2개·root 실패2개, **합계10개의 기대 로그**가 남았다. MonoSingletonTests:119/135/147–148/169/207, ResourceManagerTests:212/499, SceneRootTests:116/137의 기존 LogAssert와 대조했다. Console을 clear하지 않았고 Console 전체0이라고 보고하지 않는다.

## Timeout 시 기존 정리 경로 · 정적 검토

아래 teardown 행은 대상 속성보다 앞이어서 전후 위치가 같다. 추가 참고 행 중 ‘base’는 위 main 기준이다. 실제 timeout을 유발하지 않았다. 정상 회귀 통과를 timeout 이후 자원 정리 보장으로 확대하지 않는다.

| fixture / teardown | gate 정리·owner 경로 | 보장하지 못하는 부분 |
|---|---|---|
| AsyncSceneRootTests:33–45 | PrepareGate 취소36–37·ReleaseGate 완료38–39 → root object Destroy42 → frame44 | graceful Shutdown await 없음. base133–134 로컬 cover/scene gate에 teardown 접근 없음. 정상 helper는151까지 gate를 동기 해제하고152에서 await하므로 이 누락만으로 현재 timeout 잔류를 입증하지 않음 |
| Replacement:45–79 | release/loader/presentation/proceed gate49–52 → manager57·common59 → roots67·씬 unload69 → host Destroy76 | 선택5개 fake gate 정리 경로 존재. shutdown/native unload의 유한 완료 보장 없음, 일부 예외는 catch |
| Area:50–83 | release/loader/presentation/reveal gate53–56 → manager61·common63 → roots71·씬 unload73 → host Destroy80 | 선택6개 fake gate 정리 경로 존재. native/owner drain 완료를 timeout 속성만으로 보장하지 않음 |
| Bootstrap:43–62 | bootstrap50·common root55 shutdown → finally host Destroy59 | gate 직접 해제 없음. callback/prepare token 취소에 의존. shutdown이 끝나지 않으면 finally 도달 보장 없음 |
| Entry:51–71 | bootstrap61·direct manager65·root66 shutdown → remaining 씬 unload68 → host Destroy69 | PrepareGate 직접 해제 없음. owner 취소에 의존하며 pending/예외 뒤 unload/Destroy 도달 보장 없음 |
| SceneLoader:104–127 | manager110·common111 → root/unload114–121 → locator 제거123 → host Destroy124 | **로컬 DelayedLoader gate 직접 해제 없음**. 닫힌 gate와 manager drain이 함께 대기할 가능성 |
| Policy:49–72 | presentation/cover/reveal gate52–54 → manager/bootstrap/common56–58 → root65/unload66 → host/settings Destroy68–69 | 선택2개 CoverGate 해제 경로 존재. TrackingLoader는 native loader 사용; native 완료 한도 보장 없음 |

구체적인 조건부 위험: 적용 source `SceneLoaderPlayModeTests.CallerCancellationLeavesNativeOwnerAndResultAlive:426`은 취소된 entry를435에서 await한 뒤437에서 gate를 연다(base423/432/434). 취소 회귀로 이 await가 멈추면 timeout 때 로컬 gate가 닫힌 채 남을 수 있다. DelayedLoader의 raw `Gate.Task:533`(base528)와 teardown110의 manager shutdown이 기다릴 수 있다. 나머지4개 loader 대상은 정상 흐름에서 final await 전에 gate를 동기 해제한다.

`GameSceneManager.cs:909/919`는 취소 후 transition drain, `RootInstallation.cs:103/113/124/133`은 preparation 취소·drain·release·dispose 순서를 사용한다. Probe의 gate/token 대기와 함께 읽었다. `[Timeout]`은 cooperative 실행 실패 보호이고 underlying UniTask 취소·gate 해제·owner cleanup 완료를 자동 제공하지 않는다. Editor 메인 스레드 blocking 강제 중단도 확인하지 않았다. **정리 코드 수정은 승인 범위 밖이므로 하지 않았다.** 이 경로를 안전하게 보완할지는 별도 사용자 결정이며 이번 정상 측정으로 보장된다고 표시하지 않는다.

## 변경 경계·문서 원문·승인 상태

- 테스트 변경은7개 기존 `.cs`에24개 속성 줄만 추가했다. 이24줄을 제거하면 기존 파일 raw SHA256과 같으므로 모든 본문·helper·assertion·흐름·using을 보존했다. 기존 `.meta`/GUID를 포함한497개 다른 tracked 입력과 사용자 dirty6 raw hash도 보존했다. 테스트가 만든 임시 InitTestScene은 framework의 정상 cleanup 후 남아 있지 않다.
- `doc/IMPROVEMENT_BACKLOG.md`는 사용자 첨부 `IMPROVEMENT_BACKLOG_2.md` **32,772 bytes 그대로** 복사했다. SHA256·Git blob은 source-preservation.json에 있다. Codex용 지시문은 저장소에 넣지 않았다.
- 첨부의 큐 ‘대기’/IMP-04 절 ‘준비됨’ 등 과거 상태 문구는 원문 보존 지시에 따라 다듬지 않았다. 이번 적용 승인 근거는 D-05a~c와 직접 전달한 적용 프롬프트이며, 실제 작업 상태는 이 검증 문서·색인·[회고](../../retrospectives/2026-10-12-09-imp-04-apply.md)로 보완한다. 1단계 PR #5 수락 기록은 Git/이전 회고에 보존되어 있다.
- public API/의존성/설정/지원 계약 변화가 없어 XML·사람/AI API 문서는 갱신 대상이 아니다. 배포 사본·버전·tag·runtime 소스는 수정하지 않았다.
- 미실행: 실제30초 timeout 유발·timeout 후 teardown 동적 검증·EditMode Timeout 적용/실험·CPU blocking 복구·다른 기기·Player/IL2CPP·제품 UX. 최종 정확한 HEAD 규칙 검사/Actions/PR 링크는 PR 본문·완료 보고에 기록한다.
- 사용자 확인: 적용 결과의 PR 최종 수락/병합 승인. 범위·혼합 방식·30초는 이미 확정되어 다시 승인받지 않는다. 정리 위험의 별도 수정 요청·추가 범위/60초 적용은 이번 PR에 포함하지 않는다. main 병합과 다음 IMP 착수는 최종 승인 전 보류한다.
