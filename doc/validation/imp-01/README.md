# IMP-01 · 대여 중 외부 파괴의 지연 슬롯 회수

2026-10-12. **Implemented / 사용자 수락 완료·통합 준비**. 작업 브랜치 `codex/imp-01-pool-liveness`, 기준 main `a10c3022587a46b0cc3d479a2273c82ec649611f`, 초기 검증 소스 `d7f319c6f18713bb9fba77dd9505fc9dbe26f088`. 최종 문서 commit과 정확한 HEAD Actions 결과는 이 작업 PR과 완료 보고에서 확인한다.

**현재 후보:** 후속 소스 `901d56c582691d55f4aa9698e91be57231328635`, 전체 Edit335/335·Play309/309, 실패·skip0. 초기 실행 자료는 그대로 보존하고, 아래 후속 요청 절에서 최신 경계 비교·회귀·백로그 상태 갱신을 구분한다.

## 요청과 변경 범위

[백로그](../../IMPROVEMENT_BACKLOG.md)의 D-01=A1만 구현했다. PR #6(IMP-04)의 사용자 병합 승인 후 진행 큐에 따라 시작했다. PR #6은 main `a10c3022587a46b0cc3d479a2273c82ec649611f`에 일반 merge되고 `codex/imp-04-apply`의 로컬·원격 ref가 삭제됐다. IMP-01 승인 전에는 main 병합·작업 브랜치 삭제·IMP-02 시작을 하지 않는다.

`ObjectPool<T>` 생성자 마지막에 선택 `Func<T, bool> isAlive = null`을 추가했다. null은 기존 판정 없는 동작을 유지한다. `PrefabPool`은 Unity fake-null 판정을 넘겨 소비자가 Destroy한 대여 clone의 정원이 계속 막히던 문제를 회복한다. 추가 컴포넌트·패키지·프로젝트 설정·배포 사본 변경은 없다.

- 대기 Stack은 Peek 판정 후 죽은 항목을 제거한다. 살아 있는 재사용 항목의 LIFO 순서를 보존한다.
- 재사용 항목이 없고 정원이 찼을 때만 대여 Dictionary를 순회한다. 생성자에서 준비한 capacity 크기 scratch에 수집한 뒤 제거하며, 정상 재사용에서는 대여 목록을 순회하지 않는다.
- predicate는 기존 재진입 가드 안에서 실행한다. 대기 predicate 실패는 현재 후보를 보존하고, 순회 predicate 실패는 순회 소유권을 보존한다. 이미 제거한 대기 항목은 복구하지 않는다.
- 검출·제거한 죽은 객체의 onDestroy를 한 번씩 모두 시도한다. 실패는 집계하고 슬롯은 회복한다. 실패 cleanup을 Dispose에서 다시 호출하지 않는다. 새 factory 결과가 죽었거나 판정이 실패하면 소유 등록 뒤 기존 실패 정리 경로로 폐기하며 onRent를 호출하지 않는다.
- 기존 Return/Dispose 본문·검증·예외 우선순위는 그대로다. 파괴된 clone의 Return은 계속 ArgumentNullException이다. Count getter는 판정·변경·할당을 하지 않아 관련 TryRent 확인 전까지 죽은 객체가 추적 수에 남을 수 있다.

공개 Prune, getter 자동 정리, TryReturn은 이번 범위에 없다. 마지막 선택 인자는 기존 소스 호출과 호환되지만 CLR 생성자 시그니처가 달라져 미리 컴파일한 소비 어셈블리는 재빌드해야 한다.

## 실제 Red → Green 및 회귀

기존 TPLab Editor PID24376, Unity6000.3.18f1 / Test Framework1.6.0 / CLI·Connector0.4.1 / Windows Mono Editor에서 실행했다. 소스 변경 후 AssetDatabase.Refresh와 reflection으로 새 생성자·보충 테스트가 실제 로드됐음을 확인했다. 테스트 실행 중 소스는 동결했다.

모든 실행은 `tools/run_unity_tests.py --project <TPLab 절대 checkout> --mode <EditMode|PlayMode> [--filter <fixture>] --output <작업별 JSON>`으로 수행했다. 전체 회귀는 filter 없이 실행했다. 실행별 UTC 시각·실행 수·전체 이름·runId·PID·CLI 반환값은 아래 JSON에 보존했다. PlayMode 일부 CLI가 reload 때문에 1을 반환했지만 도구가 동일 실행의 fresh connector-file 결과를 복구했다. 실제 test 실패로 바꾸거나 다른 Editor에서 재실행한 결과가 아니다. 모든 실행은 단일 attempt다.

| 단계 | 대상 | total | pass | fail | skip | 증거 |
|---|---|---:|---:|---:|---:|---|
| Red | PrefabPoolTests | 18 | 17 | 1 | 0 | [Red](red-results.json) |
| Green | PrefabPoolTests | 18 | 18 | 0 | 0 | [집중 실행](green-runs.json) |
| Green | 기존 ObjectPoolTests | 24 | 24 | 0 | 0 | [집중 실행](green-runs.json) |
| Green | ObjectPoolLivenessTests | 15 | 15 | 0 | 0 | [집중 실행](green-runs.json) |
| 회귀 | 전체 EditMode | 334 | 334 | 0 | 0 | [회귀](regression-runs.json) |
| 회귀 | 전체 PlayMode | 307 | 307 | 0 | 0 | [회귀](regression-runs.json) |

실제 Red는 `TPLab.Core.Tests.PrefabPoolTests.RentedCloneDestroyedByConsumerFreesItsSlot` 한 건만 실패했고, 원문 메시지는 다음과 같아 예상된 슬롯 고정과 일치했다.

```text
TPLab.Core.Tests.PrefabPoolTests.RentedCloneDestroyedByConsumerFreesItsSlot:   Expected: True
  But was:  False
```

기존 CreatePool(1)로 대여 → UnityEngine.Object.Destroy(clone) → yield return null → 다시 TryRent가 true인지 검증한다. 테스트에 Timeout30000ms를 먼저 지정했고 Red 후 파일 SHA256 `790262dc7e188fdafebef68fee31edfeae6dc44e2d12285f34cc509b77e2786b`를 고정했다. 구현 단계에서 이 파일을 수정하지 않았다. 기존 17개 PlayMode 테스트와 기존 24개 ObjectPool 테스트의 기대값·Ignore·timeout은 바꾸지 않았다. 새 보충 fixture는 구현 뒤 경계 계약을 검증한 11개 메서드·15개 case이며 모두 Red 실행했다고 주장하지 않는다.

보충 테스트는 죽은 대여 슬롯/살아 있는 대여 보존, 대기 survivor LIFO, 모든 죽은 대기 정리, null 정책, predicate 예외·재진입에서 소유 보존, 모든 cleanup 시도·실패 집계와 회복, 죽은 factory/predicate 실패의 정리·onRent 미호출, 할당을 확인한다. 전체 회귀 수는 기준319/306에서 새15/1을 더한334/307이다. 기존625개 통과 이름을 이전 기준 결과와 집합 비교해 모두 보존됐고 신규16개가 해당 fixture에만 속함을 확인했다.

## 할당·컴파일·Console·한계

`GC.GetAllocatedBytesForCurrentThread()`로 워밍업 뒤 각각1,000회 반복을 측정한 `WarmedReuseCapacitySweepAndCountGettersAllocateZero`와 `WarmedDeadLoanRecoveryAddsNoPoolAllocation`은 정확히 **0바이트** assertion으로 통과했다. 임계값을 완화하지 않았다. 회복 측정은 미리 만든 객체를 교체하여 factory 생성 할당을 제외한다. 증거는 집중·전체 EditMode에 기록된 두 실제 테스트 이름/통과 결과다.

이 수치는 측정한 풀 내부 bookkeeping 범위다. Unity clone 생성, 소비자 predicate·callback·factory 할당, 초기 scratch 메모리, CPU 비용까지 0이라는 뜻은 아니다. 정상 재사용에도 생존 predicate 호출 비용은 있다. Player/IL2CPP/다른 플랫폼에서의 GC·성능 수치는 이번에 측정하지 않았다.

[환경·제품 Console](environment.json): reflection으로 새 코드 로드를 확인했고, 실행 후 Play/compile/import/TestRun은 모두 false다. Console의 예외10건은 기존 LogAssert 기대값(싱글턴6·provider2·root2)과 대조했다. compiler CS 오류·추가 미예상 오류는 관찰되지 않았다. Console을 비우지 않았으며 'Console 로그0'으로 보고하지 않는다.

[입력 보존](source-preservation.json): 원본 추적 입력504개 중 수정한 기존 source3개를 제외한501개 및 기존 dirty6의 raw SHA256을 확인했다. 새 meta GUID는 유일하다. 고정한 Red와 Green 이후 핵심5파일의 raw hash도 동일하다. Git의 LF 정규화로 raw hash가 blob hash와 다른 파일은 두 hash를 별도 보존한다. 기존 Return/Dispose 본문 동일성과 ObjectPool의 Unity 비의존을 확인했다. Unity test 임시 씬 잔여는 없다. 기존 사용자 씬의 trailing whitespace는 이번 stage/검사 범위 밖이며 원문을 보존했다.

초기 후보 `6e16dc491814c255a037dfca759a7a543a696076`에서는 백로그 원문32772바이트를 보존했다. 이후 2026-10-12의 명시 요청에 따라 상태6곳을 갱신했다. 현재 파일 전체가 원문 바이트와 동일하다고 주장하지 않는다. 설계 D-01과 다른 항목의 본문은 유지하며 원문은 위 초기 commit에서 확인할 수 있다.

새 소비 프로젝트 가져오기·패키지 설치·Player 빌드·IL2CPP·다른 Unity 버전·시각 UX는 **이번 단위 미실행**이다. 과거 소비 검증을 이번 변경 검증으로 대신하지 않는다. 사람/AI API의 설명 발췌 예제도 실행하지 않았다. 별도 시각 UI 동작은 추가하지 않았으며, 최종 사용자 확인은 코드·API·회복 정책 검토다.

## 문서와 다음 gate

핵심 변경은 ObjectPool.cs, PrefabPool.cs, 고정 Red가 추가된 PrefabPoolTests.cs, 새 ObjectPoolLivenessTests.cs/.meta다. XML과 [GENERIC_POOL](../../GENERIC_POOL.md)·[OBJECT_POOL](../../OBJECT_POOL.md)·[사람 API](../../api/Pooling.md)·[AI API](../../ai/api/Pooling.md)를 같은 단위에서 갱신했다. [회고](../../retrospectives/2026-10-12-10-imp-01-pool-liveness.md)·두 색인·위 JSON만 문서 증거로 추가한다.

정확한 최종 branch HEAD의 로컬 PR 규칙 검사와 Actions는 PR 본문·완료 보고에 결과/URL을 남긴다. Actions는 Unity 테스트 실행 CI가 아니므로 위 Editor 실행 증거와 구분한다. 사용자 승인 이후에만 이 PR을 병합·브랜치 정리하고 IMP-02를 시작한다.

## 2026-10-12 후속 요청 · 현재 경계 검증과 상태 갱신

같은 IMP-01의 추가 실행 지시다. PR #6 merged=true/main `a10c302`를 원격에서 다시 확인했고 최신 main이 동일하므로, 그 main에서 이미 만든 `codex/imp-01-pool-liveness`와 PR #7을 재사용했다. source `901d56c582691d55f4aa9698e91be57231328635`는 추가 경계 테스트와 Count XML 설명을 포함한다. 이 요청의 병합·강제 푸시·브랜치 삭제 금지를 따른다. IMP-02는 시작하지 않는다.

### main 증거 대조

| 근거 | 최신 main a10c302 | 현재 작업 소스 | 결과 |
|---|---:|---:|---|
| PrefabPool.Return | 92–100 | 92–100 | 백로그와 일치 |
| ObjectPool._owned | 19 | 21 | main은 백로그와 일치; 구현 필드 추가 뒤 위치 이동 |
| ObjectPool.CountRented | 29 | 31 | main은 백로그와 일치; getter 동작 유지 |
| ObjectPool 정원 검사 | 86 | 92 | main은 백로그와 일치; 지연 점검 추가 |

정확한 파일·행과 hash는 [후속 보존](followup-preservation.json)에 남겼다. 첫 rented-clone Red는 위의 실제 사전 실행 증거를 사용한다. 실패 메시지 `Expected: True / But was: False`, CreatePool(1)·UnityTest·Timeout30000과 이후 파일 SHA 고정은 그대로다.

### 경계별 기준 구현 → 현재 구현

추가 경계3개를 먼저 작성하고 `.cs`·`.meta` hash를 고정했다. 기존 Red 파일의 private CreatePool을 공유하려면 그 frozen 파일을 바꿔야 하므로, 별도 작은 fixture에서 source 생성/종료만 준비했다. 기대값·Ignore·timeout은 baseline 비교 뒤 수정하지 않았다.

이미 구현한 뒤 보완하는 작업이므로 **사후 baseline 비교**다. 같은 Editor에서 `git show main`의 ObjectPool/PrefabPool을 namespace만 변경한 임시 복제본으로 실행했다. 임시 테스트는 고정한 경계 테스트의 namespace와 using만 바꿨다. 원문 body·hash·변경 범위는 [복제 근거](baseline-clone-provenance.json)에 보존한다. 새 패키지나 다른 Editor를 사용하지 않았다. 원래 checkout을 main으로 rollback하거나 source·dirty·Red 파일을 바꾸지 않았다.

| 경계 | baseline 결과 | 현재 Green | 의미 |
|---|---|---|---|
| (a) LiveLoanIsPreservedWhenCapacityIsExhausted | PASS | PASS | alive 대여·false/null·소유/개수 보존 및 정상 반납 재사용 |
| (b) DestroyedInactiveCloneIsSkippedAndReplacedInSameRent | FAIL: 아래 원문 | PASS | 같은 TryRent에서 dead inactive 건너뛰고 replacement 성공, stale count 확인 |
| (c) OmittedLivenessPolicyPreservesUncheckedLoansAndReuse | PASS (EditMode) | PASS (EditMode) | isAlive **생략** 시 dead 표시 plain class의 기존 대여 정원·원참조 재사용·종료 cleanup 유지 |

(b)의 실제 비교 실패 원문:

```text
TPLab.Core.Tests.Imp01Baseline.PrefabPoolLivenessBoundaryTests.DestroyedInactiveCloneIsSkippedAndReplacedInSameRent: System.InvalidOperationException : A pooled clone has been destroyed.
```

(a)/(c)는 기존부터 맞는 보존 계약이므로 인위적인 Red 실패를 만들지 않았다. 각각 baseline PASS → 적용 PASS로 보고하며, (b)는 실행된 baseline FAIL → 적용 PASS다. 세 case를 모두 구현 전 Red라고 기록하지 않는다. 임시 baseline `.cs`/`.meta`8개를 명시 경로로 제거하고 refresh·reflection으로 실제 assembly에서도 사라진 것을 확인한 뒤 현재 Green을 실행했다.

[baseline 실행](baseline-boundary-runs.json): Play2(pass1/fail1/skip0)·Edit1(pass1/fail0/skip0). [현재 Green](boundary-green-runs.json): Play경계2/2·Edit생략1/1·기존 Prefab18/18, 실패·skip0.

요청한 기존5개는 Prefab18 실제 pass 이름에서 각각 확인했다: RentHookDestroyingInstanceIsReportedAndCapacityRecovered, ReturnHookDestroyingInstanceIsReportedAndCapacityRecovered, DestroyedParentRejectsOperationsButStillAllowsDisposal, ReturnRejectsNull, DisposedPoolRejectsRentAndReturn. 각 이름은 위 JSON의 passes에 있고 원래 파일은 변경하지 않았다.

### 미실행 호출·최종 회귀·상태

Green 첫 호출에서 부모가 클래스명 일부 `TPLab.Core.Tests.PrefabPool`을 filter로 지정하여 **total0**이 선택됐다. CLI 연결 종료 뒤 driver가 nonzero-result 검증에서 실패했다. [0건 선택 기록](zero-selection.json)에 CLI34204의 해당 결과와 transcript를 보존했다. 실행0건은 통과·case 실패로 세지 않는다. 설치된 Connector의 testNames/groupNames 필터를 확인하고 정확한 fixture 이름으로 별도 실행했다. 실행된 실패 case를 다시 시도하거나 다른 suite의 결과로 성공을 대신하지 않았다. 기존 보충15개를 구현 전 Red로 실행했다는 주장도 하지 않는다.

[최종 전체 회귀](followup-regression-runs.json)는 코드/테스트9개를 고정하고 mandatory runner로 수행한 Edit335/335·Play309/309, fail/skip0이다. 이전641개 pass 이름을 모두 보존하고 신규 경계3개만 추가됐음을 대조했다. 임시 baseline 이름은 최종 실행에 없다. 기존 generic24개·liveness15개와 GC 두 테스트의 strict0바이트·각1000회 측정도 최종 EditMode pass에 포함된다. 임계값을 바꾸지 않았다. CPU 증가까지0 또는 factory/Unity Instantiate 비용0이라고 주장하지 않는다.

[최종 환경](followup-environment.json): 동일 Editor24376, Unity6000.3.18f1/UTF1.6.0/CLI0.4.1, Windows Mono. source reload·optional ctor 확인, Play/compile/import/TestRun false. Console 예상예외10개를 기존 기대값과 대조했고 추가 미예상/CS 오류는 관찰되지 않았다.

런타임 후속 변경은 ObjectPool.CountOwned·PrefabPool.CountRented XML 두 줄뿐이다. 기존 메서드 body·공개 API·할당 정책은 변하지 않았다. 사람/AI API와 기능 문서의 current revision·검증 상태를 갱신했다. source9 및 frozen Red/경계 파일·기존dirty6은 hash로 확인했다. 새metaGUID유일·기존meta/GUID보존·임시 test scene 잔여0을 확인했다.

백로그는 사용자 지시대로 IMP-04를 승인·완료(2026-10-12, PR #6), IMP-01을 진행으로 상태6곳만 갱신했다. D-01 선택과 나머지302행은 유지했다. 현재 진행의 세부 상태는 자동 검증 완료·사용자 확인 대기다. 기존 원문 byte 보존 증거는 초기 시점의 사실이며 현재 file 전체 동일로 확대하지 않는다.

**사용자 결정:** CountOwned/CountRented는 관련 TryRent 정리 전까지 실제보다 클 수 있다. getter 정리는 도입하지 않았다. 공개 Prune()은 **보류**하며 즉시 수동 정리가 필요한 소비 용도가 있을 때 추가 여부·호출 책임·실패 계약을 사용자가 정한다. 현재 A1을 위해 필수인 API는 아니다.

Player/소비 패키지 설치/IL2CPP/다른 Unity버전·플랫폼/시각UX/문서발췌 실행은 이번 미실행이다. 최종 PR HEAD의 로컬 규칙 검사와 Actions 결과는 PR 본문·완료 보고에서 정확한 SHA/URL로 연결하며 Unity 실행 CI로 취급하지 않는다.

## 2026-10-12 PR #7 최종 수락

사용자의 `pr#7 병합 승인`으로 후보 `b9f25bf2c9454153002c5a9f57df9651413762b8`를 수락했다. core/test/meta9개와 dirty6의 해시를 다시 대조했고 최종335/309 실행 증거와 동일하다. 이번 승인 기록은 문서7개만 수정하며 코드·테스트·설정 변경이 없어 Unity 회귀를 반복하지 않는다. 최종 branch tip에 규칙 검사와 정확한 HEAD Actions를 다시 확인한 뒤 원본 commit을 보존하는 일반 merge를 진행한다. 실제 main SHA·브랜치 삭제 결과는 PR #7/최종 보고가 소유한다. 대기/병합 금지 표기는 당시 후속 요청 단계의 기록이며 이번 새 승인이 통합을 허용한다. 백로그 큐에 따라 승인된 IMP-01 통합·정리 후 IMP-02만 별도 브랜치로 시작한다. 공개 Prune은 계속 보류다.
