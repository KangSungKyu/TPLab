# IMP-05 · 측정 수단 검증 조사

2026-10-13. **조사 보고 완료·후속 지시 대기**. branch `codex/imp-05-scene-polling-gc`, 조사 기준 HEAD `32b052b43aac243499dac974b8660a4a9b8f26e7`, main `9285805bd234d1988019c890e3670945d88bce9d`.

사용자가 허용한 범위는 측정 수단 검증뿐이다. 양성 대조를 static 필드에 유지하고 반복했으며, 기존 GC 0 테스트의 양성 대조 여부는 목록만 조사했다. **기존 테스트·runtime 수정0, API 확정0, 새 패키지0**. 결과를 보고하고 멈춘다. 기존 예상 Red를 바꾸거나 GameSceneManager 구현을 시작하지 않았다.

## 실행 조건·대조

- 같은 Windows Mono Unity Editor `6000.3.18f1`에서 EditMode·실제 PlayMode 각1회 프로브를 실행했다. 각 프로브 내부의 독립 반복 round2개 × 각 round10회로 후보마다 양성20·음성20이다. 후보4개 × 대조2종 ×20 × mode2 = **관측320개**. 별도 warmup32개는 표의 n에서 제외했다. NUnit 실행은 이번0건이다.
- 양성은 `new byte[4096]`의 참조를 **static byte[][] RetainedPayloads**에 저장하고 payload[0]을0x5A로 기록한다. 단순 지역 변수 할당이나 GC.KeepAlive에만 의존하지 않았다. 각 mode에서 warmup 양성8+측정 양성80=배열88개, 요청 payload합360,448B의 길이·내용·고유 참조를 검증했다. 초기 WarmPayload·저장 배열·관측 배열은 이88개에 포함하지 않는다.
- 음성은 미리 할당한 static WarmPayload를 static RetainedReference에 대입한다. 양성/음성을 번갈아 실행했다. 결과도 static Observation[]에 저장했다. JSON·관측 결과 저장·고유 참조 검증은 측정 구간 밖이다. getter/JIT·storage·recorder 관련 호출은 양성/음성 각2회 warmup했다.
- 실험 source는 **Assets 밖**의 Temp 폴더에 놓고, 설치된 CLI0.4.1의 CompileAndExecute 내부 함수를 일회성 reflection으로 호출해 memory assembly로 실행했다. 이 내부 함수 사용은 정식 public API나 새 공용 도구 계약이 아니다. [실제 프로브 원문](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/MeasurementProbe.cs.txt)을 보존한다.
- PlayMode는 저장할 dirty scene이 없는 것을 확인한 뒤, 저장하지 않는 **root0개의 빈 씬**에서 실행했다. 기존 InitScene 게임 흐름은 실행하지 않았다. 원래 scene setup·Profiler.enabled=false·Enter PlayMode 설정을 복원했다. profiler 전역 기록을 켜거나 설정/자산 파일을 수정하지 않았다.

## 후보별 실제 결과

각 셀의 값은 **min=max**, 해당 mode/대조20회 모두 동일했다. 한 번의 측정 window는 동기·같은 스레드이며 await/로그/assertion/JSON이 없다. [원본 EditMode](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/edit-observations.json), [원본 PlayMode](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/play-observations.json), [집계](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/control-summary.json).

| 후보 | 관측 단위 | Edit 양성 / 음성 (각20회) | Play 양성 / 음성 (각20회) | 조사 판정 |
|---|---|---:|---:|---|
| GC.GetAllocatedBytesForCurrentThread 전후 차이 | B | 0 / 0 | 0 / 0 | **양성 검출 실패**. 이 환경의 할당0 증거로 사용 불가 |
| ProfilerRecorder GC.Alloc, 현재 스레드·Start/Stop 구간의 Sum(sample.Count) | 할당 이벤트 수 | 1 / 0 | 1 / 0 | 양성20/20 감지·음성20/20 무검출, 사용 가능 후보 |
| ProfilerRecorder GC Allocated In Frame, CurrentValue 전후 차이 | B, 전역 counter | 4,128 / 0 | 4,128 / 0 | 이번 대조 감지 성공. 현재 스레드 전용이 아니며 보조 후보 |
| Profiler.GetMonoUsedSizeLong 전후 차이 | 관리 heap 사용량 B | 8,192 / 0 | 8,192 / 0 | 이번 대조 감지 성공. 누적 할당량/현재 스레드 counter가 아님 |

### 단위·판정의 한계

- 설치된 **GC.Alloc의 recorder UnitType은 TimeNanoseconds**였다. sample.Value를 할당 byte로 해석하지 않았다. Sum(sample.Count)로 event1/0만 판정했다. SumAllSamplesInFrame을 쓰지 않고 capacity128의 recorder를 Reset→Start→대조→Stop한 뒤 sample을 읽었다. 모든 양성 sample수1·Count합1, 음성 sample수0이었다. 현재 스레드 필터를 설정했지만 다른 스레드를 일부러 할당시키는 격리 실험은 미실행이다. [설치 지표 조회](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/metrics-discovery.json), [Unity sample.Count](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderSample.Count.html), [현재 스레드 수집](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html).
- GC Allocated In Frame은 bytes인 전역 counter다. LastValue나 이전 frame 값을 읽지 않고 같은 동기 구간의 CurrentValue 차이를 읽었다. 원래4,096B payload 요청에 관측4,128B였다는 사실만 기록한다. 이 차이를 모든 객체 크기나 backend의 표준 overhead로 일반화하지 않는다. 다른 스레드의 동시 할당·frame 경계 영향은 이번에 부하를 주어 검증하지 않았다. [CurrentValue 정의](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.CurrentValue.html).
- MonoUsed는 살아 있는 객체와 **아직 회수되지 않은 객체**를 포함한 heap 사용량이다. 프로브의 Unit 문자열 `Live managed heap bytes`는 좁은 설명 문구이며, 표/판정은 공식 API의 범위로 해석했다. 이번8,192B 증가는 요청 payload길이/정확한 누적 할당량으로 해석할 수 없다. GC 회수/다른 스레드 영향도 있다. 관측 중 GC.Collect를 강제해 계측을 바꾸지 않았다. [Unity MonoUsed 정의](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler.GetMonoUsedSizeLong.html).
- .NET counter는 이 Editor의 Edit/Play 양성40/40을 모두0으로 보고했다. static 보존을 검증하고 두 ProfilerRecorder 후보는 같은 payload를 감지했으므로 단순 미사용 지역 변수 제거를 원인으로 볼 근거가 없다. 현재 메서드는 mscorlib의 **InternalCall, IL 없음**으로 확인했다. native 구현 원인·stub 여부·모든Unity/Mono버전의 동작은 확정하지 않는다. [구현 metadata](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/gc-counter-implementation.json), [.NET counter의 원래 계약](https://learn.microsoft.com/en-us/dotnet/api/system.gc.getallocatedbytesforcurrentthread).

GC.Alloc 이벤트 수는 이번 양성/음성 대조를 통과한 **후보**다. 이번 요청에서 이를 테스트에 채택하거나 byte 기반 단언을 event 단언으로 변경하지 않았다. byte 측정과 할당 여부 측정은 별도 계약이다.

## 기존 GC 0 테스트 목록만 조사

현재 Assets/TPLab 전체(Core/Input/UI)에서 strict GC0은4개/3파일이다. Core Edit3·Play1이며 Input/UI0이다. 임계값을 허용하는 할당 테스트나 별도 ProfilerRecorder/GC 계측 공용 helper는 발견하지 못했다. 아래는 **소스 읽기만 수행**한 목록이며 이번에 재실행·수정하지 않았다.

| 기존 메서드 | 현재 선언 / 계측 / 단언 행 | 횟수·warmup | 내장 양성 대조 |
|---|---|---|---|
| [ObjectPoolLivenessTests.WarmedDeadLoanRecoveryAddsNoPoolAllocation](../../../../Assets/TPLab/Tests/EditMode/ObjectPoolLivenessTests.cs) | 257 / 269·277 / 278 | dead-loan 회복1,000회. 객체2개 미리 생성·대여/회복1회 warmup | 없음 |
| [ObjectPoolLivenessTests.WarmedReuseCapacitySweepAndCountGettersAllocateZero](../../../../Assets/TPLab/Tests/EditMode/ObjectPoolLivenessTests.cs) | 286 / 299·300·308 / 309 | 대여·정원거절·Count3종·반납1,000회. 재사용/거절/storage와 counter warmup | 없음 |
| [ObjectPoolTryReturnTests.WarmedTryReturnReuseAllocatesZero](../../../../Assets/TPLab/Tests/EditMode/ObjectPoolTryReturnTests.cs) | 149 / 157·163 / 164 | TryRent/typed TryReturn1,000회, delegate 바인딩·대여/반납 warmup | **없음** |
| [GameSceneEntryTests.CanProceedDoesNotAllocateInReadyState](../../../../Assets/TPLab/Tests/PlayMode/GameSceneEntryTests.cs) | 75 / 82·87 / 89 | Ready, getter1회 warmup 후100회 | 없음 |

모두 GC.GetAllocatedBytesForCurrentThread 전후 차이를 Is.Zero로 단언한다. TryReturnTestApi.Bind(:9–17)은 signature 확인·typed delegate 바인딩용이며 측정 API의 양성 대조가 아니다. 문서의 GC 가이드/과거0바이트 설명과 이번 대조 JSON은 이 테스트의 내장 양성 대조로 세지 않는다. 기존 기능/계약 테스트 결과를 실패로 다시 표시하지 않으며, 여기서는 **GC0 단언만으로 이 환경의 무할당을 입증할 수 없다는 측정 한계**를 보고한다. 기존 tests·과거 evidence·사람/AI API 문서는 목록 조사 때문에 수정하지 않았다.

## 보존·검증·중단

[초기 Editor 구성](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/original-editor-setup.json)과 [원래 씬 복원](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/restoration.json): InitScene 활성/loaded, Play/compile/import false, dirty scene0, Profiler false. 전후 raw hash547개·사용자 dirty6개 모두 동일하다. 고정된 Red 파일 hash도 동일하며 runtime/test/package/settings/tools 변경0이다. [입력 보존](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/source-preservation.json). 제품 Console error0을 확인했고 로그를 비우지 않았다. 첫 복원용 일회성 snippet은 JToken extension 이름 사용으로 compile 단계에서1회 실패했으며 실행되지 않았다. 명시적 cast로 수정한 snippet이 실제 복원을 마쳤다. 제품 컴파일/테스트 오류와 구분한다.

이번은 **측정 수단 조사만 완료**했다. NUnit0·전체 회귀/Green/Player/IL2CPP/외부 소비/멀티스레드 부하 미실행이다. 조사 문서에 대한 diff·공백·링크·규칙 검사만 수행한다. PR/Actions는 아직 이 조사 branch에 없고 main병합·branch삭제는 하지 않는다. 선택한 측정 도구 없음, API 확정 없음. 사용자 지시에 따라 결과 보고 뒤 멈춘다.

[메모리 정리](https://github.com/KangSungKyu/TPLab/blob/db05c252f0e8bce7c9fc67b2faf944bbd00d601d/doc/validation/imp-05/measurement-survey/probe-cleanup.json): native recorder는 이미 Dispose됐고, 잔여 임시 assembly1개의 static 양성 배열88개 참조와 결과 buffer를 Clear했다. 임시 assembly type·초기 warmup/storage 배열 틀은 다음 정상 domain reload/Editor 종료까지 남으며, 강제 reload로 사용자 Editor를 건드리지 않았다. 실제 프로브·관측·복원 증거를 이 폴더에 보존한 뒤 이번 Temp/imp-05-measurement 소유 파일8개만 정리했다. 이전 Temp/imp-05는 고정 테스트/이전재현 재개 자료라 이번 정리에 포함하지 않는다. Unity/PC 종료 지시는 재사용하지 않는다. [회고](../../../retrospectives/2026-10-13-02-gc-measurement-survey.md).
