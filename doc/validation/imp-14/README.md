# IMP-14 · 대조 검증을 내장한 GC 측정 helper

2026-10-13. **Implemented / helper·pool Verified / 사용자 수락·main 통합 완료**. 전체 회귀는 **Partial: CanProceed Red 1건 유지**다. IMP-05 runtime 구현은 재개하지 않았다. 브랜치 `codex/imp-14-gc-measurement-helper`, 기준 main `9285805bd234d1988019c890e3670945d88bce9d`, 선행 조사 tip `dd8e48742b8e297d199b869f9cc135e0daf9a072`, 검증한 helper/test source revision `1b123d6a77f62e3bc7f75d96a2cb607b403d291a`. 문서 최종 HEAD와 그 Actions는 PR·완료 보고에서 확인한다.

## 범위와 계약

사용자가 지정한 선행 단위를 [IMP-14](../../IMPROVEMENT_BACKLOG.md#imp-14-대조-검증을-내장한-테스트-gc-측정-helper)로 추가했다. 기존 [측정 수단 조사](../imp-05/measurement-survey/README.md)를 의존 기준으로 같은 checkout에 별도 브랜치를 만들었다. PR에는 main 이후 선행 조사·CanProceed 테스트와 이번 helper 단위가 포함된다. 테스트 fixture만 변경했고 pool·GameSceneManager runtime/API, package·asmdef·프로젝트 설정은 변경하지 않았다. 기존 사용자 변경은 보존했다.

테스트용 `GcAllocationMeasurement.Measure(Action workload)`는 `TPLab.Core.Tests` / `TPLab.Core.TestFixtures`에 있다. [helper](../../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs)는 소비용 runtime API가 아니다. 동기·비중첩·비동시 호출을 전제로 하며 caller가 warm-up·정리를 소유한다. 내부 markerName/capacity overload는 미지원·미검출·용량 소진 테스트용 seam이다.

- GC.Alloc recorder에 `CollectOnlyOnCurrentThread | WrapAroundWhenCapacityReached`를 명시하고 `sample.Count`를 합산한다. 기본 capacity4096. 설치 지표 UnitType은 TimeNanoseconds이므로 sample.Value를 byte로 읽지 않는다. frame 합산 옵션은 사용하지 않는다.
- 한 Measure 호출에서 계측·control 경로만 워밍업하고, 같은 recorder의 별도 Reset→Start→Stop 구간으로 음성0·static 보존 4KB 양성>0을 확인한다. 양성 미검출·음성 오염·미지원이면 workload를 실행하기 전에 실패한다. 결과에서 대조 이벤트와 workload 이벤트를 분리한다.
- workload는 자동 warm-up 없이 정확히1회 실행한다. delegate/closure·recorder 생성·NUnit 단언·JSON·정리는 측정 구간 밖이다. Count가 capacity에 닿거나 WrappedAround가 true면 버린 표본을0으로 숨기지 않고 실패한다.
- `GC Allocated In Frame.CurrentValue` 전후 차이는 지원 여부와 함께 보조로 반환한다. 전역값이라 다른 스레드/frame 영향을 받으며 GC0 판정을 대체하지 않는다. 미지원 시 availability=false, delta0은 **미측정 표시**이지 무할당 증거가 아니다.
- finally에서 Stop/Dispose와 static 양성 참조 해제를 시도하고 workload 원래 예외를 보존한다. 등록한 다른 profiler marker에 GC 양성 이벤트가 들어오지 않는 경우, 없는 marker, capacity2/할당16회, callback 예외 후 다음 측정, 정확히1회 호출을 테스트했다.

공식 정의: [sample.Count](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderSample.Count.html), [현재 스레드 옵션](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html), [WrappedAround](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.WrappedAround.html), [CurrentValue](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.CurrentValue.html).

## 교체 전 → 실제 Red → 교체 후

Unity6000.3.18f1 / UTF1.6.0 / CLI·Connector0.4.1 / Windows Mono Editor의 기존 인스턴스에서 mandatory `tools/run_unity_tests.py --project <절대 checkout> --mode <mode> --filter <정확한 fixture/메서드> --output <JSON>`으로 실행했다. 전체 회귀는 filter 없이 실행했다. 사전 상태·실제 nonzero named 결과를 확인하고 실패한 테스트를 과거 결과로 대체하지 않았다. 공개 JSON에서는 로컬 경로/PID/runId를 제외하고 오류 stack의 checkout prefix만 `[checkout]`으로 치환했다. 기존 IMP-01·02 원본 증거는 수정하지 않았다.

[교체 전](before-runs.json)에는 기존 counter/양성 대조 없는4개를 각각 실행해 pass1이었다. [Red baseline source](red-helper.cs.txt)는 기존 counter를 helper 계약으로 감싼 구현이다. static4KB 양성도0을 보고하여 실패했으며 ProfilerRecorder 채택 전에 실행했다. helper13개는 pass2/fail11, 교체된4개는 fail4, 합계 total17/pass2/fail15/skip0. missing-positive·null 검증2개는 처음부터 통과했으므로 모든 경계가 각각 Red→Green이었다고 주장하지 않는다. [실제 Red](red-runs.json).

```text
Positive control allocation was not detected.
  Expected: greater than 0
  But was:  0
```

| 기존 메서드 | 반복 | 교체 전 | 교체 helper Red | 교체 후 |
|---|---:|---|---|---|
| WarmedDeadLoanRecoveryAddsNoPoolAllocation | 1000 | pass, counter0B | 양성 미검출 fail | pass, workload0 events |
| WarmedReuseCapacitySweepAndCountGettersAllocateZero | 1000 | pass, counter0B | 양성 미검출 fail | pass, workload0 events |
| WarmedTryReturnReuseAllocatesZero | 1000 | pass, counter0B | 양성 미검출 fail | pass, workload0 events |
| CanProceedDoesNotAllocateInReadyState | 100 | pass, counter0B | 양성 미검출 fail | **fail, workload2300 events** |

기존 warm-up/loop/반환·Count·Ready 의미 단언은 유지하고 측정 수단만 교체했다. 교체 테스트와 새 계약 테스트를 Red 전에 고정하고 helper 구현 단계에서 수정하지 않았다. 보조 계측/한 번 호출 보호는 별도 diagnostic 파일2개를 추가하고 별도 Red 후 고정했다. 이 파일 추가는 초기 Red 묶음 실행 중 발생했지만 실행된 기존 파일·helper baseline은 수정하지 않았고, 추가 테스트의 실제 Red는 refresh 뒤 따로 실행했다. 이후 Green·전체 회귀는 전체 테스트 소스를 동결했다.

첫 Green 요청은 테스트 시작 전 `ProfilerCategory.Any`가 설치 버전에 없어 CS0117로 중단됐다(실행0건). helper만 실제 지원되는 Memory로 수정하고 compile error 해소·새 assembly 로드를 확인했다. 오류 Console을 비우지 않았으며 다음 실행에서는 CS 오류가 없었다. frame/worker API를 최신 upstream 존재만으로 설치 환경에서 검증했다고 보지 않는다.

[교체 후](after-runs.json): helper Edit9+diagnostic2, Play2 모두 pass13/13. pool liveness15/15·TryReturn13/13(그중 GC3개 포함). CanProceed1fail은 대조가 유효한 상태에서 실제 이벤트2300을 검출했다. IMP-05 수정 Red로 남겨 Ignore/단언 완화/runtime 변경을 하지 않았다.

## 반복 안정성과 thread 격리

[집계](stability-summary.json), [Edit 직접 snapshot](edit-stability-snapshot.json), [고정 반복 코드](../../../Assets/TPLab/Tests/Fixtures/GcAllocationControls.cs), [실행 결과](after-runs.json). Play는 종료 domain reload 후 static0을 결과로 삼지 않고, Repeat(100)의 정확한 집계 단언이 통과한 결과를 근거로 한다. 전체 회귀에서도 같은 반복·격리 테스트가 다시 통과했다.

| 환경 | 양성 workload | 음성 workload 검출 | 호출별 대조 검증 | worker4KB 할당의 현재 스레드 이벤트 |
|---|---:|---:|---:|---:|
| EditMode | 100/100 감지(100%) | 0/100 | 200/200 유효 | 0, static payload 보존 확인 |
| PlayMode | 100/100 감지(100%) | 0/100 | 200/200 유효 | 0, static payload 보존 확인 |

각 helper 호출에도 양성·음성 대조가 들어간다. worker는 미리 생성한 thread/동기화 객체로 워밍업하고 측정 중 byte[4096]을 static에 저장한다. bounded wait와 Join으로 정리한다. 양성은 버려진 지역 변수가 아니다. 다른 스레드 할당의 제외는 두 mode에서 실제 테스트했으며 전역 frame bytes의 thread 격리를 주장하지 않는다.

[Editor 진단](environment.json)의 추가 양성/음성 probe는 이벤트1/0, 보조 frame bytes4128/0B, 내부 양성1·음성0, 지원=true였다. 이벤트0을 정확한 thread allocation byte0으로 바꾸지 않는다.

## 전체 회귀·보존·Console

[전체 회귀](regression-runs.json): **Edit359/359, Play318/319, 실패1·skip0**. 유일한 실패는 위 CanProceed2300 이벤트이고 helper/pool 테스트는 모두 통과했다. 전체 회귀를 Green으로 표기하지 않는다. 새로운 helper 테스트13개 외 기존 의미 단언은 유지했다.

[보존](source-preservation.json): baseline547 paths 중 교체3개 외544개 raw hash 동일. runtime·packages·settings·tools와 기존 dirty6, Red 고정12개, 신규meta GUID5개의 유일성을 확인했다. 테스트 대상 source/meta13개 SHA256을 함께 고정했다. [환경](environment.json)에서 원래 InitScene loaded/active·dirty=false, Play/compile/import/TestRun=false, Profiler.enabled=false를 확인했다.

[제품 Console](console.json) 예상 예외10개는 singleton6/provider2/root2의 현재 LogAssert와 대조했고 추가 미예상/CS 오류는 관찰되지 않았다. 테스트 assertion 실패1건과 Console 상태는 별도 증거다. Red Play helper 실행 전 discovery가1회 거부됐고 tool은 test 미시작을 확인한 뒤 같은 Editor를 재탐색했다. 실제 test 실패 retry나 Editor/suite 대체는 하지 않았다.

## Editor 계측 한계에 의한 정정과 승인 gate

[IMP-01](../imp-01/README.md), [IMP-02](../imp-02/README.md), [사람 Pooling](../../api/Pooling.md), [AI Pooling](../../ai/api/Pooling.md), [제네릭 풀](../../GENERIC_POOL.md), [PrefabPool](../../OBJECT_POOL.md)에 정정을 반영했다. 과거 counter0B/pass 결과는 역사적 관측으로 유지하지만 양성 대조가 없어 해당 Editor의 무할당 입증으로 사용할 수 없다. 현재 pool3개는 유효한 호출별 대조 아래 이벤트0을 통과했다. D-01=A1 지연 회수·D-02 TryReturn 검증순서/로직·반환/정리 결론은 변경하지 않았다. runtime 공개 API/XML을 수정하지 않았고 새 helper의 test-only 계약만 XML로 문서화했다.

이 단위의 승인 전 IMP-05 구현/API 확정·main 병합·브랜치 삭제를 하지 않는다. 공개 Prune 등 다른 백로그 결정은 변경하지 않았다. 로컬 규칙·정확한 최종 HEAD Actions는 PR와 완료 보고에 연결한다. PR 규칙 Actions는 Unity 테스트 CI가 아니다.

**NotRun:** Player/IL2CPP, 다른 Unity/플랫폼, 새 소비 프로젝트·배포, 제품 UX/시각 확인, 비동기/중첩/동시 helper 호출. 이벤트 유실이 생기는 환경·현재 스레드 필터를 지원하지 않는 환경은 helper 대조/용량 실패가 gate다. native 구현 원인·모든 Mono 버전의 counter 장애를 일반화하지 않는다.

## 사용자 수락·통합 (2026-10-13)

사용자가 PR #9 병합과 완료 후 IMP-05 재개를 승인했다. 승인 후보는 `e8f45411c5efc9227d7047e564e6976e6db145ef`다. CanProceed2300 events의 알려진 실패1을 유지한 상태의 명시적 승인으로, 전체 회귀가 Green으로 바뀐 것은 아니다. 수락 기록만 추가하고 helper/runtime/test 소스는 그대로 유지했다. 최종 문서 tip의 규칙/Actions를 확인하고 원본 커밋을 보존하는 일반 merge와 해당 브랜치 정리를 진행한다. 실제 merge SHA·정리는 PR/Git 이력과 완료 보고를 따른다. IMP-05의 버퍼 API 이름·시그니처 확인 단계에서 재개한다.

2026-10-13 통합완료: 최종tip90df125의Actions성공후일반merge main3ee358f. 소스/GUID/dirty6와원본tip도달성확인, 해당branch로컬/원격삭제. 실제receipt는 [IMP05 선행통합](../imp-05/implementation/pr9-merge-receipt.json)에보존한다. 이후API확정·IMP05가원래CanProceed Red를0events로해결했지만위IMP14실행원문/통과수는수정하지않는다.
