# IMP-04 1단계 · 테스트 대기·타임아웃 조사

- SourceRevision: `7d4ff2e388124218761363cdf31762230cd7e8b3` (PR #4 일반 merge 완료). 코드·행 번호는 이 main 기준이다.
- Scope: `Assets/TPLab/Tests`, `Assets/TPLab/Input/Tests`. UI 테스트는 아래 위치/개수 참고만 포함한다.
- ImplementationStatus: 조사·작은 실험 완료, 기존 테스트 적용 없음. IMP-04 전체는 진행 중이며 적용 범위 지정 대기다.
- ValidationStatus: 현재 Editor의 임시 PlayMode 실험 2개와 기존 테스트 46개 실행 확인. 전체 회귀·EditMode 실험·Player·타 플랫폼은 NotRun.
- Environment: Unity 6000.3.18f1, Test Framework 1.6.0, resolved NUnit package 2.0.5 / DLL assembly 3.5.0.0, CLI/Connector 0.4.1, Windows Editor PID 24376. [실행 환경/소스 hash](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/environment.json).
- 근거: [백로그 IMP-04](../../IMPROVEMENT_BACKLOG.md#imp-04-테스트-타임아웃-도입), [작업 규칙](../../../AGENTS.md), [문서화 지침](../../DOCUMENTATION_GUIDE.md).

## 1. 게이트 대기 목록과 집계

명시적인 테스트/fixture `UniTaskCompletionSource`를 사용하는 공개 테스트 메서드는 **62개**, 생성 소스 지점은 **68개/10파일**이다. Input 테스트의 TCS 생성은 0개이고, `System.Threading.Tasks.TaskCompletionSource` 생성은 지정 범위에서 0개다. 메서드 수, 생성식 수, 실제 실행 case 수를 구분한다. 공유 helper/fixture 한 생성식을 여러 테스트가 사용하므로 이 숫자는 runtime 객체 수가 아니다. [기계 판독 가능한 명단/위치](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/inventory.json).

| 그룹 | 공개 게이트 테스트 메서드 | 생성 소스 지점 |
|---|---:|---:|
| AsyncSceneRootTests | 9 | 12 |
| GameSceneReplacementTests | 16 | 17 |
| GameSceneAreaTests | 8 | 8 |
| DataTableManagerAsyncTests | 6 | 9 |
| BootstrapPlayModeTests | 6 | 7 |
| GameSceneEntryTests | 1 | 1 |
| SceneLoaderTests | 2 | 2 |
| SceneLoaderPlayModeTests | 5 | 1 |
| SceneTransitionPolicyTests | 6 | 6 |
| StandardDataTableAsyncTests | 3 | 5 |
| 합계 | 62 | 68 |

게이트 68개 생성 지점 자체에는 deadline이 없다. owner/caller 취소, 테스트가 명시하는 결과 설정, teardown 정리가 일부 대기를 끝낸다. ‘자체 deadline 없음’은 모든 테스트가 무제한이라는 뜻이 아니다. 아래 표의 F100은 기존 최대100프레임 settle+assert, C3/C10은 조건 도달만 제한하는 3/10초 Timeout이다. 후속 direct await까지 보호하는 것으로 세지 않는다. **표의 `없음`/`별도 제한 없음`은 테스트 코드가 정의한 추가 제한이 없다는 뜻**이다. 설치 UTF의 framework 기본 180초 cooperative 제한과 구분한다(2·3절).

### AsyncSceneRootTests — 9개

파일: `Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `CoverAndAllPreparationAndScenePresentationAreAwaited:128` | 133–136 | 141/145/147/151 → 152 | 없음 |
| `ReleaseWaitsUnderCoverAndCleansInReverseOrder:158` | 165 | 171 → 172 | 없음 |
| `OneWaiterCancellationDoesNotCancelSharedPreparation:200` | 205 | 210 취소 → 211; 214 → 215 | 취소 F100; survivor 없음 |
| `CancelledFlowCannotProceedOrRevealAfterLatePreparation:223` | 228 | 234 취소 → 235; 237 → 238 | 취소 F100; 늦은 prepare 없음 |
| `DestructionCancelsWaitersAndLateResultsCannotProceed:245` | 250 | 254 Destroy → 256; 258 늦은 결과 | F100 |
| `SameFlowRejectsOverlappingOperations:267` | 272 | 276 duplicate; 279 → 280 | duplicate F100; first 없음 |
| `SingletonRegistrationDoesNotAllowEarlySceneProgress:320` | 325 | 331 → 332 | 없음 |
| `CancellationDuringReleaseFinishesCleanupBeforeStoppingScene:338` | 343 | 348 취소; 350 → 351 | F100 |
| `ShutdownCancelsPreparationAndCannotRunItAgain:360` | 365 | 367 shutdown → 368 | preparation F100; shutdown 없음 |

### GameSceneReplacementTests — 16개

파일: `Assets/TPLab/Tests/PlayMode/GameSceneReplacementTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `AdditivePresentationFinishesBeforeOldRootShutdown:82` | 88 | 98 → 99 | C10 |
| `SingleWaitsForOldGracefulShutdownBeforeNativeLoad:131` | 137 | 149 → 150 | 없음 |
| `TwoReplacementsKeepCommonAndSeparateEntryFromTransitionHistory:159` | 162 | 168 → 169–170 | C10 |
| `OverlappingReplacementIsRejectedBeforeEffects:184` | 187 | 196 → 197 | C10 |
| `OwnerCancellationWaitsForLateCandidateThenPreservesOldRoot:201` | 206 | owner 취소; 210 → 212 | 없음 |
| `ShutdownSharesWaitForLateCandidateAndCleansEveryOwnedScene:224` | 227 | 233 → 235/237–238 | 없음 |
| `CallerCancellationStopsOnlyItsReplacementWait:247` | 250 | 254 취소 → 256; 258 → 259 | 없음 |
| `CompletedOperationAwaiterCanImmediatelyStartAndCancelTheNextOperation:442` | 448 | next 취소; 452 → 454 | 없음 |
| `UnsupportedProgressKeepsPreparationAndNativeCompletionSeparate:463` | 466 | 485 → 486 | C10 |
| `ManualWaitKeepsOldAdditiveRootAndRejectsAnotherCommand:555` | 560 | 569 → 571 | C3 |
| `ManualWaitCancellationRestoresCoverAndCleansOnlyCandidate:577` | 582 | owner 취소 → 587 | C3 |
| `ManualWaitRechecksOldRootConditionBeforeShutdown:597` | 604 | 608 → 610 | C3 |
| `SingleLoadingWaitUsesPersistentCallbacksAfterOldRootRelease:618` | 624 | 630 → 631 | C3 |
| `SecondCoverAwaitRechecksConditionBeforeOldRelease:670` | 677/680 | 681/684 → 686 | C3 |
| `CallerWaitCancellationDoesNotAuthorizeOrCancelProceed:725` | 729 | 733 취소 → 735; 739 → 740 | C3 |
| `SingleWaitCancellationRetainsLastSceneUnpreparedUntilExplicitRecovery:772` | 776 | owner 취소 → 781; 792 shutdown | C3 |

### GameSceneAreaTests — 8개

파일: `Assets/TPLab/Tests/PlayMode/GameSceneAreaTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `AdditiveChildWaitsForPresentationAndRetainsPreparedParent:99` | 103 | 113 → add await | C10 |
| `PendingSameRemovalSharesResultAfterAuthorityValidationAndCallerCancellation:226` | 233 | 245 취소 → 247; 250 → 251 | 없음 |
| `OtherCommandsAreRejectedDuringRemovalWithoutEffects:260` | 265 | 274 → removal | 없음 |
| `OwnerCancellationWaitsForLateAddedCandidateAndPreservesRegisteredTree:304` | 309 | 311 취소; 313 → 315 | 없음 |
| `ShutdownWaitsForLateAddedCandidateAndThenReleasesRegisteredTree:369` | 374 | 379 → 381/383 | 없음 |
| `OwnerCancellationDuringRemovalWaitsForEntireBegunSubtreeCleanup:459` | 465 | 467 취소; 470 → 472 | 없음 |
| `SingleCancellationDuringDescendantReleaseFinishesGracefulTreeWithoutNativeLoad:483` | 489 | 492 취소; 495 → 497 | 없음 |
| `SameRemovalSharesCompletionAfterUnloadWhileRevealIsPending:581` | 588 | 602 → shared removal | C10(590–591) |

### DataTableManagerAsyncTests — 6개

파일: `Assets/TPLab/Tests/PlayMode/DataTableManagerAsyncTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `ConcurrentRequestsPublishOnlyAfterAllSourcesAndValidation:28` | 32 | 48 → 49–50 | F100 |
| `CallerCancellationLeavesSharedWork:56` | 63(helper) | 71 취소; 75 → 76 | F100 |
| `AllWaitersCancelledStillPublishOwnerSnapshot:59` | 63(helper) | 71 취소; 75 → 77 | F100 + 최대100프레임 IsLoading |
| `SourceFailureAndCancellationPreserveSnapshotAndAllowRetry:102` | 106/112/119/125 | 109 결과/115 오류/121 취소/127 retry | F100 |
| `DisposalCancelsWaitersAndRejectsLatePublication:134` | 138/145 | 142; 148–149 Dispose → 150–151; 155 늦은 결과 | F100 |
| `WorkerSourceReturnsToMainThreadBeforeProjectCallbacks:164` | 168 | 176 worker → 177/179 | F100 |

### BootstrapPlayModeTests — 6개

파일: `Assets/TPLab/Tests/PlayMode/BootstrapPlayModeTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `CommonInstallerReentryIsRejectedAfterAsynchronousCover:112` | 115 | 120 → 121 | 없음 |
| `OwnerCancellationKeepsFailurePhaseAndCommonServices:168` | 171 | 177 취소 → 181; 196 shutdown | C10(175) |
| `SharedCallbacksWaitForPreparedPresentationBeforeReveal:327` | 330 | 340 → 341 | C10(334) |
| `BothRootsMustPrepareBeforeRevealAndShutdownKeepsSharedRoot:351` | 354/355 | 361/365 → 366–367 | C10(362) |
| `CallerCancellationDoesNotCancelSharedEntry:404` | 407 | 411 취소 → 415; 422 → 423 | 없음 |
| `OwnerShutdownCancelsGamePreparationAndCleansItsScene:429` | 432 | 435 shutdown → 439 | C10(434) |

### GameSceneEntryTests — 1개

파일: `Assets/TPLab/Tests/PlayMode/GameSceneEntryTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `DirectManagerSharesWaitWithoutCancellingItsOwner:74` | 77 | 83 취소 → 87; 95 → 96 | 없음 |

### SceneLoaderTests — 2개

파일: `Assets/TPLab/Tests/EditMode/SceneLoaderTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `UnloadStartsOnceAndAllCallersWaitForItsCompletion:82` | 85 | 98 → 99–101 | 없음 |
| `UnloadFailureIsSharedWithoutRetryOrSuccessPublication:108` | 111 | 120 오류 → 123–124/128 | 없음 |

### SceneLoaderPlayModeTests — 5개

파일: `Assets/TPLab/Tests/PlayMode/SceneLoaderPlayModeTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `ExplicitSourceChoosesOnlyItsInjectedLoader:386` | 519(fixture) | 396 → 397 | 없음 |
| `OwnerCancellationWaitsForLateNativeResultAndCleansIt:404` | 519(fixture) | 409 owner 취소; 411 → 413 | 없음 |
| `CallerCancellationLeavesNativeOwnerAndResultAlive:423` | 519(fixture) | 430 취소 → 432; 434 → 435 | 없음 |
| `WrongSceneResultIsRetainedThenCleanedWithoutReveal:443` | 519(fixture/helper448) | 454 → 456 | 없음 |
| `WrongTargetResultIsRetainedThenCleanedWithoutReveal:446` | 519(fixture/helper448) | 454 → 456 | 없음 |

### SceneTransitionPolicyTests — 6개

파일: `Assets/TPLab/Tests/PlayMode/SceneTransitionPolicyTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `LateFalseDuringAddCleansCandidateBeforeSuccessPublication:366` | 371 | 375 → add | C10 |
| `LateFalseDuringAsyncRevealCleansAddedCandidateAndRestoresCover:388` | 394 | 399 → add | C10(396–397) |
| `LateFalseBeforeAdditiveReleaseRetainsPreparedOldSubtree:414` | 420 | 424 → replacement | C10 |
| `LateFalseBeforeSingleReleaseDoesNotShutdownOrLoad:436` | 442 | 445 → replacement | 없음 |
| `LateFalseBeforeRemovalRetainsEntirePreparedSubtree:457` | 464 | 467 → removal | 없음 |
| `PendingRemovalRevealJoinDoesNotReevaluateDestroyedConditions:478` | 486 | 494 → shared removal | C10(488) |

### StandardDataTableAsyncTests — 3개

파일: `Assets/TPLab/Tests/PlayMode/StandardDataTableAsyncTests.cs`.

| 공개 테스트(선언 행) | 생성 행 | 해제/취소 → 완료 await | 기존 테스트 보호 |
|---|---|---|---|
| `ReloadQueriesOldGenerationAndCallerCancellationLeavesSharedWork:17` | 24/32 | 29; 39 취소; 42 → 43 | F100 |
| `WorkerSourceParsesOnMainAndWorkerQueriesAreRejected:56` | 63 | 71 → 72/74; worker77/80 | F100 |
| `DisposalRejectsLateStandardPublicationAndRetainedSnapshotStaysReadable:86` | 93/99 | 96; Dispose → 102; 104 늦은 결과 | F100 |

### 게이트와 구분한 provider·runtime·native 대기

- `ResourceManagerTests`에는 테스트 TCS 생성이 없다. `DelayedProvider:614/622–625`가 `ProvideHandle`을 보류하고 `CompletePending:634–649`가 성공/실패를 완료한다. provider-held 테스트 16개와 native/background 전용 4개를 구분한다.
- `LoadingSampleTests`에는 테스트 TCS 생성이 없지만 runtime continuation을 기다리는 테스트 10개가 있다. `SceneTransitionSampleController.Loading.cs:38/184/211–217/303/355`가 runtime completion/await/성공/취소를 소유한다. 작업 자체 deadline은 없다. 아래 표는 테스트 위치만 대조하며 샘플 코드는 수정하지 않았다.
- `InputRebindingTests`의 테스트 TCS 생성은 0개지만 native 입력 결과를 await한다. `Request:46–48`의 작업 deadline은 2초, `TimeoutIncludesReleaseWait:99–108`은 0.5초다. `selected` 상태 대기 `:68/:106`의 1초 조건 timeout과 별개다.

| ResourceManagerTests 공개 선언 | 대기 유형/주요 경로 | 기존 보호 |
|---|---|---|
| SharedLoadCachesOneOwnedHandle:133 | provider → Complete139/Settle140 | Complete/F100 |
| CallerCancellationDoesNotCancelSharedLoad:152 | provider, Pending163/취소164/Settle165/완료167 | F100 |
| AllCancelledWaitersStillLeaveOwnerCache:155 | 공유 helper, 완료167/직접 await173 | F100; 직접 await 별도 제한 없음 |
| InvalidKeysTypesAndPreCancellationDoNotDispatch:180 | provider Pending196/Complete200 | F100 |
| FailedLoadIsRemovedAndCanRetry:207 | provider 실패/retry Complete214/218 | Complete |
| ShutdownCancelsWaitersAndDrainsLateHandle:226 | provider Pending231/Shutdown/완료239/Settle240–241 | F100 |
| SeparateOwnersKeepIndependentNativeReferences:253 | provider Complete261/Settle262 | F100 |
| InitializationIsSharedAndOwnerTerminationIsPermanent:272 | native init Settle280/286–287/293, shutdown291 | F100; shutdown 제한 없음 |
| InitializationDispatchFailureCanRetry:298 | native init direct await315 | 없음 |
| ShutdownDuringInitializationCannotPublishReadiness:320 | native init, shutdown325/Settle326/328 | F100 |
| BackgroundThreadCallsDoNotMutateOwner:334 | Task.Run + Settle339/344/347/350 | F100 |
| SceneOwnedRootWaitsForRequiredAssetBeforeRevealing:357 | provider, 공유 Pending462/Complete466/release476 | F100; release 제한 없음 |
| SingletonRootDisposesPrefabPoolBeforeAssetHandle:360 | 동일 helper | 동일 |
| PreparationFailureKeepsCoverAndReleasesOwnedResources:363 | provider 실패 Pending498/Complete500 | F100 |
| PreparationCallerCancellationKeepsCoverUntilOwnerShutdown:366 | provider Pending387/Settle389/Complete398 | F100; disposed100프레임 |
| RootDestructionCancelsLoadAndReleasesLateCompletion:369 | provider Destroy411/Settle413/완료416/shutdown417 | F100; shutdown 제한 없음 |
| DataTablesArePublishedBeforeSceneProceedAndDisposedBeforeResource:512 | provider Pending558/Complete562/release575 | F100; release 제한 없음 |
| InvalidCsvStopsSceneKeepsCoverAndReleasesResource:515 | 동일 helper, CSV 실패/rollback | F100 |
| StandardDataTablesReadyBeforeSingletonSceneProceed:518 | 동일 helper, release575 | F100; release 제한 없음 |
| InvalidStandardIdxKeepsCoverAndReleasesResource:521 | 동일 helper, idx 실패/rollback | F100 |

| LoadingSampleTests 공개 선언 | 생성/완료 await | 종료 경로 |
|---|---|---|
| HeldKeyboardSubmitRequiresReleaseThenFreshSubmit:111 | 118 / 130 | release 후 fresh submit |
| HeldGamepadSubmitRequiresReleaseThenFreshSubmit:135 | 142 / 154 | release 후 fresh submit |
| HeldPointerReleaseDoesNotProceedAndFreshClickDoes:158 | 167 / 179 | release 후 fresh click |
| EarlyAndDuplicateClicksDoNotCompleteLaterOperation:183 | 189/214 / 194/219 | 클릭192/218 |
| CancelledWaitReleasesListenersWithoutReleasingTransition:224 | 229 / 236 | 취소231 |
| DestroyedContinueButtonFailsPendingWait:250 | 254 / 261 | button Destroy256 |
| DestroyedUiOwnerCancelsPendingWait:272 | 276 / 283 | host Destroy278 |
| SecondaryPointerButtonsAlsoRequireNeutralBeforeArming:292 | 298 / 309 | neutral 후 click308 |
| AutomaticContinuationDoesNotWaitForHeldSubmitOrPointerRelease:331 | 343 / 345 | 이미 완료 확인344 |
| PreparedManualUiShowsReadyAndFullStageBarWhileStillWaiting:351 | 355 / 362 | 클릭361 |

native-only `SceneLoaderPlayModeTests` 14개는 explicit TCS 62개에 포함하지 않는다: 공개 선언 130,146,149,152,155,158,161,253,266,278,295,306,323,464. 특히 `RealAddressablesOwnerCancellationOwnsLateCompletion:464`는 `CreateManager():466`의 기본 native loader를 쓰며 `DelayedLoader.Gate`를 사용하는 테스트가 아니다. EditMode `SceneLoaderTests.BackendCannotPublishUnloadSuccessWhileSceneRemainsLoaded:144`의 unload await148도 별도 native/backend 범주다.

## 2. 대기 helper와 현재 보호

| 파일/정의 | 현재 구현의 제한 | 실제 보호 범위/한계 |
|---|---|---|
| AsyncSceneRootTests.cs:53–60 `Wait(Task)` | 최대100회 NextFrame → IsCompleted assertion | fault/cancel을 await해서 전파하지 않음. 이후 기존 GetResult/Exception assertion 계약을 유지해야 함 |
| DataTableManagerAsyncTests.cs:201–205 `Settle(Task)` | 최대100프레임 + 완료 assertion | 전체 load/source await의 일반 deadline 아님 |
| StandardDataTableAsyncTests.cs:112–116 `Settle` | 최대100프레임 + 완료 assertion | 위와 같은 계약 |
| ResourceManagerTests.cs:603–610 `Settle(Task)` | 최대100프레임 + 완료 assertion | wall-clock 한도 아님 |
| ResourceManagerTests.cs:584–591 `Pending` | 최대100프레임 + provider 도달 assertion | 후속 native completion까지 보호하지 않음 |
| ResourceManagerTests.cs:593–601 `Complete(Task)` | 완료시키는 루프 최대100프레임, 뒤에 Settle 최대100프레임 | 단일100프레임으로 합치지 않음 |
| ResourceManagerTests.cs:74–78 SetUp init | realtime 최대10초 + 완료 assertion | catalog setup 전용; method 전체 보호 아님 |
| Replacement:814–815 `WaitForProceedAsync` | 조건만3초 | 뒤 transition await 별도 제한 없음 |
| Replacement:833–835 `WaitForMainPresentationAsync` | 조건만10초 | 뒤 replacement 완료 별도 제한 없음 |
| Area:656–657 / Policy:563–564 presentation helper | 조건만10초 | cleanup/reveal 결과 전체 제한 아님 |
| Bootstrap:175/334/362/434/456 상태 대기 | 조건만10초 | gate/entry 완료에 자동 적용되지 않음 |
| InputRebindingTests.cs:51–55 `KeyAsync` | NextFrame 두 번 | settle assertion이나 작업 deadline을 추가하지 않음 |
| LoadingSampleTests.cs:392–397 `FramesAsync` | 고정6프레임 | 뒤 continuation await 제한 없음 |
| InputIntegrationTests.cs:22/25 → helper58/63 | root Prepare/Shutdown direct await; frame wait66 | fixture TCS 없음, 완료 deadline 없음 |
| SceneLoadProgressTests.cs:109–120 | Task.Run + GetAwaiter().GetResult | gate 생성 없음; 동기 blocking 유형 |

**설치 소스 확인:** UTF1.6.0은 `UnityWorkItem.cs:18/63–65`, `DefaultTestWorkItem.cs:46–48`, `TimeoutCommand.cs:14/27–29`에서 TestCaseTimeout이 0일 때 **180,000ms** 기본값을 둔다. 따라서 속성 미지정을 무제한 실행으로 판단하지 않는다. 이 기본값은 소스로 확인했으며 180초 만료 실험은 실행하지 않았다. `tools/run_unity_tests.py:79`의 CLI 응답 대기 한도도 180초이므로 준비·reload 시간을 포함하면 framework 결과가 회수되기 전에 driver 한도에 도달할 가능성이 있다. 관찰에 따른 운영 주의이며 이번 작업에서 runner를 수정하지 않는다.

지정 범위 NUnit `[Timeout]` 속성은 현재 0개다. `.Timeout(...)` 조건 대기와 runtime rebind deadline은 이미 있으므로 전부 ‘timeout 없음’으로 보고하지 않는다. 100프레임 제한은 프레임이 계속 진행할 때 유한하며, Editor 메인 스레드 자체 정지에 대한 wall-clock 복구 보장은 아니다.

추가 helper 호출 메서드(명시 TCS62개 밖): Async의 공개 선언63/66/111/179/285/305는 Wait/F100, DataTableAsync:184의 background 호출은 Settle/F100, :84의 pre-cancel/immediate source는 별도 await 한도가 없다. InputRebinding 공개 선언59/89/99/113/134/158/183/213/223/251/274는 runtime 2초(99는0.5초)를 사용하며 일부 shutdown await는 별도 한도가 없다.

## 3. UTF1.6.0 작은 실행 실험

임시 PlayMode fixture의 두 `[UnityTest]`는 같은 ‘최대1초 동안 프레임을 양보하고 정상 종료’ 코루틴을 반환했다. 대조군에는 Timeout이 없고 비교군에만 `[Timeout(250)]`을 붙였다. 영구 테스트는 변경하지 않았다. 무한 대기나 Sleep으로 Editor를 막지 않았다. 실험 source/.meta는 실제 실행 후 제거했고 커밋 대상에 포함하지 않는다. 실험 code 원문도 이 저장소에 남기지 않는다.

| 항목 | 실제 결과 |
|---|---|
| 실행 경로 | `tools/run_unity_tests.py`, 기존 Editor PID24376, PlayMode |
| 대조군 | `AControlCompletesAfterBoundedCoroutine` 통과 |
| Timeout 비교군 | `BTimeoutStopsBeforeBoundedCoroutineCompletes` 실패 |
| 실패 원문 | `Timeout value of 250 ms was exceeded. If this is intended, increase the timeout value using a TimeoutAttribute.` |
| 실제 실행 수 | total2 / passed1 / failed1 / skipped0 — 실패1은 의도한 timeout 관측 |
| 정확한 invocation | runId `43892-1791785196399221700`, CLI PID43892, 접수 재시도0 |
| 증거 | [실제 결과](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/timeout-experiment.json), [프로토콜·임시 제거 metadata](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/experiment-meta.json) |

이 결과는 현재 **PlayMode에서 프레임을 양보하는 IEnumerator**에 `[Timeout]`이 적용된다는 확인이다. EditMode, async Task `[Test]`, 클래스/assembly 수준 속성, CPU 무한 루프·메인 스레드 blocking 강제 중단·타 플랫폼에는 확대하지 않는다. Timeout은 underlying UniTask/owner의 취소나 cleanup 완료를 자동 보장하지 않는다.

설치 source 해석: `TestCommandBuilder.cs:113–117`의 IApplyToContext 적용 후 `EnumerableTestMethodCommand.cs:79/91–95`는 MoveNext 반환 뒤 HasTimedOut을 검사한다. 동기 `UnityTestMethodCommand.cs:19–23`도 실행 반환 뒤 검사한다. 이는 cooperative 검사 경로이며 blocking 도중 중단을 보장하는 근거로 쓰지 않는다. NUnit DLL metadata/IL의 TimeoutAttribute는 context의 TestCaseTimeout에 정수 값을 전달한다. 설치 source 경로/hash는 environment.json에 기록했으며 제3자 source를 복사하지 않았다. 단위 ms와 TestFinished에서만 유효한 메타데이터는 [UTF1.6 공식 ITestAdaptor](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/api/UnityEditor.TestTools.TestRunner.Api.ITestAdaptor.html#UnityEditor_TestTools_TestRunner_Api_ITestAdaptor_TestCaseTimeout)를 참고한다. 실험 결과를 문서 설명으로 대신하지 않았다.

실험 전에 CLI ready 표시와 실제 응답이 달랐고 Windows 응답 없음/실제 exec 연결 timeout을 확인했다. 이후 실제 idle 응답과 class 로드·컴파일 오류0을 확인한 뒤만 실행했다. 초기 지연의 원인은 확인하지 않았다.

## 4. 정상 실행시간과 적용 후보

현재 unchanged 테스트 3개 필터를 같은 Editor에서 순차 실행했다. [원시 결과/시간 metadata](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/normal-runs.json). 아래 시간은 **각 필터 전체 driver wall-clock**이며 ready 확인, Console, IPC, domain reload, 실행·결과 회수를 포함한다. 개별 test duration, 평균 또는 case 최대값으로 나누어 해석하지 않는다. CLI0.4.1 RunTests.cs:110–119는 이름/실패 메시지를 모으고 duration을 JSON에 보존하지 않는다.

| 현재 필터 | total/passed/failed/skipped | driver wall seconds |
|---|---|---:|
| `TPLab.Core.Tests.AsyncSceneRootTests` | 15/15/0/0 | 6.843792 |
| `TPLab.Core.Input.Tests.InputRebindingTests` | 11/11/0/0 | 7.131526 |
| `TPLab.Core.Tests.ResourceManagerTests` | 20/20/0/0 | 7.785239 |
| 합계 | 46/46/0/0 | 각 필터별 측정이며 suite 합산 성능 점수 아님 |

보조 과거 자료(현재 실행 확인의 대체 아님): de879b9/2026-10-08/Unity6000.3.18f1 Windows Mono의 [Edit286 XML](https://github.com/KangSungKyu/TPLab/blob/73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67/doc/validation/distribution-candidate/final2-EditMode.xml)은 최대 case `NestedDerivedConditionUnionIsVisitedSafelyAndNeverEvaluated` 0.643471초, suite14.5581651초. [Play253 XML](https://github.com/KangSungKyu/TPLab/blob/73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67/doc/validation/distribution-candidate/final2-PlayMode.xml)은 최대 case `TimeoutIncludesReleaseWait` 0.505106초, suite2.8085409초. 현재 case별 시간은 미수집이며 Editor/기기/플랫폼에 일반화하지 않는다.

| 적용 방식 | 장점 | 제약/검증할 부분 |
|---|---|---|
| helper만 보완 | 중복이 적고 대기 실패 위치가 명확함 | helper 밖 direct/native/continuation await는 보호되지 않음. Wait가 IsCompleted만 확인하는 현재 fault/cancel 관찰 순서를 바꾸면 안 됨 |
| 메서드별 `[Timeout]` | 변경 의도가 명확하고 helper 밖 cooperative 대기도 포함 | 중복 속성, 값 갱신 비용. underlying 작업 정리/메인 스레드 blocking은 별도 문제. 이번 실험은 PlayMode UnityTest만 확인 |
| **혼합(권장 후보)** | 기존 frame/조건 보호와 진단을 유지하면서 선택 메서드에 더 짧고 명시적인 outer guard를 지정 | 적용 메서드와 값 명시, timeout 후 기존 teardown/finally의 gate 해제·owner 정리 확인 필요. blanket 적용하지 않음 |

| 값 후보(확정 아님) | 근거 | 결정 조건 |
|---|---|---|
| 선택한 PlayMode cooperative 메서드 outer30,000ms | 현재 최대 필터 wall7.785초의 약3.85배이며 해당 필터의 한 case보다 보수적인 여유. 과거 case 최대0.643초보다 충분히 큼. 기존 Core의 조건10초보다 길어 기존 실패 진단을 우선할 수 있음 | 전체 프로젝트 최악값이라는 뜻은 아님. 대상 메서드의 단계 수와 cleanup을 확인하고 사용자 지정 후 적용 |
| native catalog/다중 씬 단계 메서드 outer60,000ms 후보 | 단계별10초 조건과 native setup/cleanup을 여러 번 거치는 메서드에 추가 여유 | 현재 3필터 측정으로 모든 native 케이스의 정상 최대치를 증명하지 못함. 적용 범위 확정 시 실제 대상 측정 필요 |
| 기존 helper100프레임·조건1/3/10초·rebind0.5/2초 유지 | 이미 검증 의미가 있는 제한 | 임의로 완화·대체하지 않음. 이번 단계에서 값 수정 없음 |

30/60초는 framework 기본180초를 줄이는 runner 테스트 보호 후보이며 IMP-03의 production Shutdown timeout60초 계약과 관계없다. helper wait를 `await task.Timeout(...)`로 치환하면 fault/cancel이 기존 assertion 전에 전파되어 의미가 달라질 수 있으므로 같은 변경으로 간주하지 않는다. 값 결정은 현재 관찰과 미측정 범위를 함께 보고 사용자가 한다.

## UI 제외 범위 참고

생성식만 집계: `Assets/TPLab/UI/Tests` **12개/9파일**, `UI/InputSystem/Tests` 0개. runtime 생성 횟수/게이트 테스트 메서드 수/실행 수가 아니다. UI 내용 감사·성능 측정·수정은 하지 않았다. 아래 목록과 별도 조건 timeout 위치만 참고로 남긴다.

| UI 파일 | 생성 행 |
|---|---|
| `Assets/TPLab/UI/Tests/EditMode/UIContextInputContractTests.cs` | 72, 79 |
| `Assets/TPLab/UI/Tests/EditMode/UIContextLifecycleTests.cs` | 57 |
| `Assets/TPLab/UI/Tests/EditMode/UIContextLoadingTests.cs` | 67 |
| `Assets/TPLab/UI/Tests/EditMode/UIContextPresentationTests.cs` | 72 |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextInputPresentationTests.cs` | 113 |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextPlayModeTests.cs` | 59 |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextPresentationTests.cs` | 124, 131 |
| `Assets/TPLab/UI/Tests/PlayMode/UIContextReuseTests.cs` | 66, 73 |
| `Assets/TPLab/UI/Tests/PlayMode/VirtualScrollRectPlayModeTests.cs` | 235 |

`UI/InputSystem/Tests/PlayMode/UIContextInstallerInputSystemPlayModeTests.cs:69`에는 별도 UniTask10초 초기화 한도가 있다. 이 값을 이번 Core/Input 정책의 근거로 일반화하거나 UI 적용을 승인한 것으로 해석하지 않는다.

## 검증·사용자 결정·다음 단계

- 실험2개(의도한 failure1), 현재 기존46/46, 실패/skip0. 원본 테스트/fixture158개와 기존 dirty6 raw hash는 [보존 증거](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/preserved-inputs.json)로 대조했다. 영구 코드/테스트·패키지·설정 변경 없음. 임시 source/meta 제거·소스 diff0을 확인했다.
- 최종 Editor/Console 확인은 [종료 상태 증거](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-04-survey/post-editor-state.json)에 기록한다. ResourceManager의 의도한 provider 실패 로그 2개와 컴파일 오류를 구분하며 Console 전체가 0이라고 보고하지 않는다.
- 현지 전체 EditMode/PlayMode 회귀·EditMode 실험·case별 새 duration·Player·최종 제품 UX·IL2CPP/다른 기기는 미실행이다. 선택46개 결과를 전체 회귀로 확대하지 않는다.
- 최종 문서 HEAD 규칙 검사/Actions와 PR 링크는 PR 본문·완료 보고에 기록한다. source 기준은 위 main이며 문서 자체 최종 SHA를 실행 source로 혼동하지 않는다.
- 필요한 사용자 결정: (a) explicit62개/추가 provider·continuation·native·Input 중 적용할 정확한 메서드 범위, (b) helper/개별 속성/혼합 방식, (c) 그룹별 timeout 값. 권장 혼합·30초/필요한 native60초는 제안이다. UI는 제외한다.
- 백로그 IMP-04는 1단계 조사·실험을 완료한 진행 상태다. 2026-10-12 사용자가 [PR #5](https://github.com/KangSungKyu/TPLab/pull/5)의 조사 문서 병합을 승인했다. 기존 테스트 적용·값 설정은 별도 사용자 범위 지정/확인 후이며 IMP-01 등 다음 큐 항목은 시작하지 않는다.
