# IMP-03 · root 종료 단계별 타임아웃

2026-10-13. **Implemented / Windows Mono Editor Verified / 사용자 승인 대기**. branch `codex/imp-03-shutdown-timeout`, base main `5725e90b133aa6739c3ef098455815336de00e20`. PR #10 통합 후 사용자가 재개했다. 검증 source `5fe5bf5a41386409775d836e5b3a50a2f516a787`. 최종 문서 HEAD·Actions는 PR/완료 보고에서 확인한다.

## 원문 Red와 별도 승인 정정

기존 Unity6000.3.18f1 / UTF1.6.0 / UniTask2.5.11 / CLI·Connector0.4.1 Windows Mono Editor에서 mandatory `tools/run_unity_tests.py`로 실행했다. [이름·수·실패 원문](red-runs.json).

| 실행 | total | pass | fail | skip | 의미 |
|---|---:|---:|---:|---:|---|
| 원래 root 기준 | 15 | 15 | 0 | 0 | 기존 계약/Editor 응답 확인 |
| 최초 Play Red | 28 | 18 | 10 | 0 | 종료 미완료9 + 잘못된 취소 상태 단언1 |
| 취소 정정 후 Play Red | 28 | 19 | 9 | 0 | 모두 의도한 종료 미완료, driver23.068초 |
| 최초 설정 Red | 16 | 0 | 16 | 0 | 모두 SetUp 오류; API Red로 쓰지 않음 |
| fixture 정정 후 설정 Red | 16 | 0 | 16 | 0 | API/serialized field 부재·YAML field수0, driver19.443초 |

첫 정정: 제가 새 helper3곳에 IsCanceled=true를 잘못 기대했다. 설치 UniTask.AsTask는 취소도 Task.FromException/SetException으로 변환한다. 사용자가 **단언3곳만 GetResult의 OperationCanceledException 검사로 정정**하도록 승인했다. 기존 정리·상태·순서·대기는 유지했다. [기록](oracle-correction.json), [diff](proposed-test-oracle-fix.diff), [실제 Editor 변환](as-task-cancellation-probe.json), [probe source](AsTaskCancellationProbe.cs.txt).

```text
Status=Faulted|IsCanceled=False|Exception=OperationCanceledException
```

Task 키워드로 CLI가 사전 차단한 첫 요청은 실행 증거가 아니다. allow-async 요청에서 위 변환을 확인했으며 프로젝트 작업을 시작하지 않았다.

둘째 정정: 설정16건 수만 확인했고 뒤늦게 원문이 SetUp 오류임을 발견했다. runtime 구현을 중단했으며 당시 runtime/XML 변경0이었다. UTF1.6.0 TaskList/CreateBootstrapSceneTask는 원래 scene setup을 저장한 뒤 EditMode에 미저장 DefaultGameObjects Single bootstrap을 만든다. 여기서 NewScene(Additive)가 거부됐다. 사용자가 **씬 생성·재로딩·정리만 정정, 단언 유지**를 별도로 승인했다. fixture는 runner context 검사 후 Single 임시 씬을 사용하며 자기 파일만 저장·재로딩·삭제한다. UTF RestoreSceneSetupTask가 원래 씬을 복원한다. 기존16case·모든 계약 단언은 그대로다. [기록](fixture-correction.json), [diff](proposed-settings-fixture-fix.diff). 정정 후 Setup/TearDown 실패0·Imp03LegacyRoot 씬/prefab/meta 잔여0이었다.

각 승인을 받은 뒤에만 정정했고 최종 test/fixture5개 hash를 **구현 전에 재동결**했다. 원래 잘못된 실패를 유효 Red로 덮어쓰지 않는다.

실제 정정 후 실패:

```text
Shutdown did not settle within 1 second with a configured 0.1-second timeout.
  Expected: True
  But was:  False
```

```text
Attach must expose the confirmed optional shutdownTimeoutSeconds argument.
  Expected: not null
  But was:  null
```

```text
The newly saved host must contain exactly one timeout field before creating the legacy fixture.
  Expected: 1
  But was:  0
```

**NUnit Timeout(30000)으로 중단된 case0개.** 종료 Red9건은 realtime1초 관찰 helper의 assertion이다. framework timeout 발생 시각/그로 인한 복구는 해당 없음이다. CLI는 개별 실패 timestamp를 제공하지 않으므로 정확한 시각으로 꾸미지 않고 driver 전체 시간과 구분한다. 같은 Editor에서 후속 설정16건을 실행했고 프로세스/과거 결과를 대체하지 않았다.

## 확정 계약과 남은 검증

D-03a~c: 기본60초, Inspector와 Configure/Attach 마지막 optional float, finite0이하는 기존 무제한. 양수는 TimeSpan으로 표현 가능해야 하며 NaN/Infinity/양수 범위 초과는 mutation 전 ArgumentOutOfRangeException이다. Prepare 취소 대기와 역순 Release 각각의 독립 unscaled deadline이며 총 shutdown60초 보장이 아니다. 원래 preparation 실패는 awaiter 소유, 원래 Release 오류는 보존한다. 자체 초과는 installer 타입·단계 TimeoutException을 집계하고 남은 Release·Uninstall을 시도한다.

무제한 관찰은0초 경계로 유지했다. finite 두 host, 공유 종료, timeScale0, 단계별 예산, 원래 TimeoutException identity, 늦은 결과, API/default/input/legacy asset을 검사한다. LateReleaseFailureAfterTimeoutIsObserved는 late fault 뒤 cleanup/state 보존만 실행으로 보호한다. 예외 소비 추적 IUniTaskSource·GC 시점 검사는 없으며 실제 await/catch는 정적 리뷰로 구분한다.

이번 Player/IL2CPP·외부 소비·시각 UX는 미실행이다. timeout은 main thread 동기 blocking 코드나 비협조 hook 자체를 중단하지 못한다. 늦은 프로젝트 작업은 해제된 자원을 건드리지 않도록 수명을 지켜야 한다.

## Green·회귀·원본 보존

| 실행 | total | pass | fail | skip | 증거 |
|---|---:|---:|---:|---:|---|
| 동결 AsyncSceneRootTests | 28 | 28 | 0 | 0 | [Green](green-runs.json) |
| 동결 설정/legacy asset | 16 | 16 | 0 | 0 | [Green](green-runs.json) |
| 전체 EditMode | 375 | 375 | 0 | 0 | [회귀](regression-runs.json) |
| 전체 PlayMode | 345 | 345 | 0 | 0 | [회귀](regression-runs.json) |

총720/720·skip0이며 단위44개는 전체 회귀와 중복 집계하지 않는다. Green root driver7.518초·settings4.534초, 전체Edit20.152초·Play13.988초다. Green root CLI는 domain reload로1을 반환했지만 동일 invocation의 fresh Connector 결과28/28을 driver가 복구했다. 실제 실패 테스트를 재실행하거나 다른 Editor·과거 suite로 대체하지 않았다.

두 host×scene/prefab에서 실제 YAML 필드 제거·재import 뒤 누락값60초, 명시적0은0초를 확인했다(8case). 사용자 씬/prefab을 재직렬화하지 않고 테스트가 생성한 fixture만 썼다. 신규 timeout field는 기존 DrawDefaultInspector로 표시되므로 Editor UI 소스는 변경하지 않았다. 극단 양수 float.MaxValue는 두 host의 Attach/Configure에서 mutation 전 AORE를 확인한 [추가 Green4검사](max-value-probe.json)이며 독립 Red 실행으로 주장하지 않는다. [probe source](MaxValueProbe.cs.txt); 실행 후 original active scene/count를 복원했다.

[보존](source-preservation.json): base561 tracked source/assets/settings/tools 중 승인된 runtime/XML6 + 기존 async test1만 변경, 나머지554 raw hash 동일. dirty6·최종 frozen5·검증 source11 hash 일치. 새 Editor test/meta2는 고유 GUID이며 임시 legacy scene/prefab/meta0이다. 사용자 승인 전 동결 예외2건과 구현 중 test 변경0을 구분한다.

[환경](environment.json): InitScene active, loaded1, dirty=false, playing/compiling/updating=false, timeScale1, profiler=false. [Console](console.json)10건은 기존 singleton6/provider2/root2 LogAssert와 오류 첫줄별로 일치한다. Console을 지우거나 product 오류0이라고 표시하지 않았다.

정적 리뷰는 실제 준비종료 flag/CTS 지연 해제, original failure capture, 독립 timer/원래예외 identity, 역순 해제/공유 shutdown, host mutation 전 validation과 XML signature를 확인했다. 전체 배열·게임 씬/입력/UI 소스·패키지·asmdef·프로젝트 설정·기존 fixture는 변경하지 않았다. RootInstallation 생성자와 concrete host Configure/Attach optional 설정, private serialized field 외 ISceneRoot 시그니처는 유지했다.

**NotRun:** 이번 Player/IL2CPP·다른 Unity/플랫폼·새 소비 설치/배포·최종 시각 UX·CPU/GC benchmark·late exception IUniTaskSource 소비/GC 시점 추적·동기 blocking preemption. 영원히 끝나지 않는 비협조 Prepare의 CTS는 실제 owner 종료까지 유지된다. 초과 뒤 프로젝트 hook이 자기 자원을 만지는 문제를 코어가 강제 중단해 해결하지 않는다. 준비 자체의 정상 대기에는 timeout을 추가하지 않았다.

[사람 API](../../api/Lifecycle.md#종료-시간-제한)·[AI API](../../ai/api/Lifecycle.md#shutdowntimeout--compatibility)·XML·기능명세·백로그·회고/색인을 같은 단위에서 갱신했다. 이번 IMP-03 사용자 승인 전 main 병합/branch 삭제를 하지 않는다. 큐 밖 IMP는 자동 진행하지 않는다. Git 최종HEAD/Actions/PR은 완료보고를 따른다.

승인 diff 증거는 unified diff의 빈 문맥 행(prefix 공백1개)을 원문 그대로 보존했다. 일반 코드/문서는 git diff --check를 통과했고 이 두 raw diff에만 형식상 blank-at-eol 예외로 따로 확인했다.
