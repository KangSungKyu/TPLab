# 05. 테스트 작성법 — 현행 관행 초안

- [현행] 기준은 main `35508bc1fb8ed7749a292d1f9ae983b45387afd4`이며 이번에는 코드·기존 요약을 읽었다(컴파일·Unity·테스트 실행 0건); 아래 과거 실행 확인은 당시 source의 증거다. 근거: [공통 검증 범위](README.md#근거와-검증-범위), [IMP-03 README:3·65–68·86](../validation/imp-03/README.md#green회귀원본-보존).

## mode와 시간 제한의 분모

- [현행] main의 `Assets/TPLab/**/Tests/**/*.cs` 전체에서 EditMode30파일·PlayMode32파일·공유 Fixtures/기타16파일을 읽었다; 아래는 attribute가 붙은 **선언 메서드 수**이며 TestCase/Repeat로 확장된 실행 case 수와 다르다. 근거: [Core Tests](../../Assets/TPLab/Tests), [Input Tests](../../Assets/TPLab/Input/Tests), [UI Tests](../../Assets/TPLab/UI/Tests), [UI InputSystem Tests](../../Assets/TPLab/UI/InputSystem/Tests); 비교할 실제 실행은 [IMP-03 README:65–68](../validation/imp-03/README.md#green회귀원본-보존).

| 상태·main 전체 범위 | 테스트 메서드 | 그중 UnityTest | Timeout이 붙은 테스트 메서드 | 대표 근거 |
|---|---:|---:|---:|---|
| [현행] EditMode | 256 | 25 | 2 (UnityTest에는 0) | 위 전체 소스; `GcAllocationMeasurementTests.cs:29·81`([파일](../../Assets/TPLab/Tests/EditMode/GcAllocationMeasurementTests.cs#L29)) |
| [현행] PlayMode | 345 | 318 | 58 (모두 UnityTest) | 위 전체 소스; `AsyncSceneRootTests.cs:133–135·413–426`([파일](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L133)) |
| [현행] 합계 | 601 | 343 | 60 | 위 전체 소스의 Test/TestCase/UnityTest 선언 합집합; parameterized 선언을 중복 합산하지 않음 |

- [현행] Timeout **속성 선언 전체는62개**다: 위 테스트60개 외 `GameScenePollingTests.SetUp/TearDown`2개가 있으므로 “UnityTest343개 중 Timeout62개”로 나누지 않는다; UI를 포함한 현재 전체 분모는 원조사 표본의232/45와 다르다. 근거: [GameScenePollingTests.cs:33–35·50–52](../../Assets/TPLab/Tests/PlayMode/GameScenePollingTests.cs#L33), [0.5단계 조사](README.md#05단계-사실-확인).
- [현행] AGENTS는 순수 계약을 EditMode, 실제 GameObject·씬·Addressables 수명을 필요한 PlayMode로 나누지만 EditMode에도 UnityTest가 있다; mode와 Test/UnityTest 속성은 같은 분류가 아니다. 근거: [AGENTS.md의 TDD와 완료 기준](../../AGENTS.md#tdd와-완료-기준), [SceneLoaderTests.cs](../../Assets/TPLab/Tests/EditMode/SceneLoaderTests.cs), 위 전체 선언 집계.
- [현행] 설치 UTF1.6.0의 속성 미지정 기본180초는 과거 설치 소스 조사로 확인됐고, 제한250ms로 bounded IEnumerator를 멈춘 실험은 PlayMode cooperative coroutine에 한정됐다; **UniTask.ToCoroutine의 hang에 Timeout이 실제 적용되는지는 미검증(IMP-04b)**이며 blocking 강제 중단·underlying 작업 취소·teardown 완료를 보장하지 않는다. 근거: [IMP-04 조사 README:226·234–248](../validation/imp-04-survey/README.md#2-대기-helper와-현재-보호), [IMP-04 적용 README:79–101](../validation/imp-04-apply/README.md#timeout-시-기존-정리-경로--정적-검토), 테스트 `CoverAndAllPreparationAndScenePresentationAreAwaited`([AsyncSceneRootTests.cs:135](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L135)).

## 게이트와 실제 시간 관찰

- [현행] 순서·진입 금지는 TCS gate를 닫아 부정형 단언을 확인한 뒤 직접 열고 최종 trace/완료를 확인한다; `CoverAndAllPreparationAndScenePresentationAreAwaited`는 cover→installer2개→scene gate를 순서대로 해제한다. 근거: [AsyncSceneRootTests.cs:137–161](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L137), [IMP-04 조사 README:10–28](../validation/imp-04-survey/README.md#1-게이트-대기-목록과-집계).
- [현행] **시간 계약 자체**는 실제 시간을 관찰한다: IMP-03은 configured0.1초에 realtime1초 완료를 단언하고, 무제한0/음수는0.2초 뒤 아직 pending인지 본다; 이 assertion 실패는 NUnit30초 만료 실패가 아니다. 근거: [AsyncSceneRootTests.cs:386–394·629–657](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L386), [IMP-03 README:49](../validation/imp-03/README.md#원문-red와-별도-승인-정정).
- [현행] 전체 지정 테스트 소스의 `Thread.Sleep`은 `DataTableImporterTests.TransientManifestReadLockIsRetriedWithoutRewritingSources`1곳(75ms)이며 **background thread**가 파일 잠금을 풀기 위해 사용한다; 메인 스레드 Sleep이나 일반 gate 대기 선례로 확대하지 않는다. 근거: [DataTableImporterTests.cs:164–190](../../Assets/TPLab/Tests/EditMode/DataTableImporterTests.cs#L164); 이 위치는 [백로그의 Delay/Sleep 제한:24](../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완)과 비교할 현행 예외다.

## 실패 오라클과 측정 대조

- [현행] IMP-14 helper는 같은 측정 호출에 음성0·보존4KB 양성>0을 확인하고 대조가 실패하면 workload 전에 실패한다; GC.Alloc **event Count**를 byte로 해석하지 않으며 예전 counter0B는 양성 대조 없는 역사적 관측으로 남아 있다. 근거: [GcAllocationMeasurement.cs:75–114](../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs#L75), [IMP-14 README:11–15·23–42·67](../validation/imp-14/README.md#범위와-계약).
- [현행] IMP-03 최초 Play Red에는 오라클 오류1건, 최초 설정 Red16건에는 SetUp 오류가 있었다; 둘은 유효한 요구사항 Red로 재서술하지 않았다: 승인한 단언/fixture 정정 뒤 원래 실패와 정정 실패를 별도로 남기고 구현 전에 test/fixture5개 hash를 재동결했다. 근거: [IMP-03 README:9–27](../validation/imp-03/README.md#원문-red와-별도-승인-정정), [백로그:23](../IMPROVEMENT_BACKLOG.md#추가-규칙-agentsmd-보완).
- [현행] 해당 설치 UniTask의 `AsTask` 취소는 **Task.Faulted + OperationCanceledException**, IsCanceled=false였다; 이 변환 경로를 사용하는 현재 root 테스트는 GetResult의 OCE를 검사한다(모든 Task 구현으로 일반화하지 않음). 근거: [IMP-03 README:17–23](../validation/imp-03/README.md#원문-red와-별도-승인-정정), 테스트 `ShutdownWithUncooperativePreparationNeverCompletesWithoutTimeout`와 [AsyncSceneRootTests.cs:629–663](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L629).
- [현행] 최신 관련 과거 회귀는 IMP-03의 Edit375/375·Play345/345·실패0·skip0, 합720/720이며 단위44개를 중복 합산하지 않는다; 이 기록은 Windows Mono Editor/당시 source의 증거로 Player·IL2CPP·제품 Console0·이번 Unity 실행을 뜻하지 않는다. 근거: [IMP-03 README:63–78·86](../validation/imp-03/README.md#green회귀원본-보존), 기대 Console 로그10개는 같은 문서74행.

## 양성 대조 범위와 미확정 사항

- [결정됨] **D-H8 — 위험이 확인된 오라클의 양성 대조.** 계측·순서 등 오라클 감도 위험이 확인된 테스트에만 양성 대조를 요구한다; GC는 현행 호출별 대조 helper를 유지하고 순서 테스트는 통제된 gate 해제 후 완료를 확인하는 방식 등으로 해당 위험을 검증한다. 기존 양성 대조 없는 모든 부정형 단언을 일괄 갱신하는 규칙으로 확대하지 않는다. 근거: **사용자 2026-10-13 일괄 승인**, [사용자 결정](README.md#사용자-결정), [IMP-14 README:23–42](../validation/imp-14/README.md#교체-전--실제-red--교체-후), [AsyncSceneRootTests.cs:146–161](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L146).
- [결정됨] 검토했으나 채택하지 않은 대안: 모든 부정형 단언에 통제된 양성 경로를 일괄 요구하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H8).
- [제안] 실패 수뿐 아니라 실패 이유·SetUp/TearDown 구분을 검토한 뒤 테스트를 고정하는 안과, 기존 항목별 동결 절차를 유지하는 안을 비교한다; 오라클/fixture 변경은 구현 편의로 실패를 덮지 않고 별도 근거를 남기는 후보 규칙이다. 근거: [IMP-03 README:12–27](../validation/imp-03/README.md#원문-red와-별도-승인-정정), [제안 목록](README.md#제안-목록).
- [제안] 상태·순서는 gate/유한 조건 대기를 우선하고 시간 자체의 계약만 경과 관찰로 검증하는 안을 후보로 둔다; Timeout 속성은 위험 메서드별 적용과 fixture/assembly 기본 적용의 대안이 있으며 ToCoroutine hang/cleanup은 IMP-04b의 별도 검증 후 결정한다. 근거: [IMP-04 조사 README:268–277](../validation/imp-04-survey/README.md#검증사용자-결정다음-단계), [IMP-04 적용 README:79–101](../validation/imp-04-apply/README.md#timeout-시-기존-정리-경로--정적-검토), [제안 목록](README.md#제안-목록).
