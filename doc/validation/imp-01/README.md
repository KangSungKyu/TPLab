# IMP-01 · 대여 중 외부 파괴의 지연 슬롯 회수

2026-10-12. **Implemented / 자동 검증 완료·사용자 확인 대기**. 작업 브랜치 `codex/imp-01-pool-liveness`, 기준 main `a10c3022587a46b0cc3d479a2273c82ec649611f`, 검증 소스 `d7f319c6f18713bb9fba77dd9505fc9dbe26f088`. 최종 문서 commit과 정확한 HEAD Actions 결과는 이 작업 PR과 완료 보고에서 확인한다.

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

`doc/IMPROVEMENT_BACKLOG.md`는 사용자가 제공한 원문32772바이트를 보존했다. 원문 큐의 과거 상태 표기는 현재 실행 증거가 아니며, 실제 IMP-04 승인 완료와 IMP-01 확인 대기는 이 기록·색인·회고·PR에서 구분한다.

새 소비 프로젝트 가져오기·패키지 설치·Player 빌드·IL2CPP·다른 Unity 버전·시각 UX는 **이번 단위 미실행**이다. 과거 소비 검증을 이번 변경 검증으로 대신하지 않는다. 사람/AI API의 설명 발췌 예제도 실행하지 않았다. 별도 시각 UI 동작은 추가하지 않았으며, 최종 사용자 확인은 코드·API·회복 정책 검토다.

## 문서와 다음 gate

핵심 변경은 ObjectPool.cs, PrefabPool.cs, 고정 Red가 추가된 PrefabPoolTests.cs, 새 ObjectPoolLivenessTests.cs/.meta다. XML과 [GENERIC_POOL](../../GENERIC_POOL.md)·[OBJECT_POOL](../../OBJECT_POOL.md)·[사람 API](../../api/Pooling.md)·[AI API](../../ai/api/Pooling.md)를 같은 단위에서 갱신했다. [회고](../../retrospectives/2026-10-12-10-imp-01-pool-liveness.md)·두 색인·위 JSON만 문서 증거로 추가한다.

정확한 최종 branch HEAD의 로컬 PR 규칙 검사와 Actions는 PR 본문·완료 보고에 결과/URL을 남긴다. Actions는 Unity 테스트 실행 CI가 아니므로 위 Editor 실행 증거와 구분한다. 사용자 승인 이후에만 이 PR을 병합·브랜치 정리하고 IMP-02를 시작한다.
