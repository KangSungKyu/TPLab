# Netcode P5 — 고정 씬 예제와 사용자 수락 대기


## 요청·기준·범위

2026-10-16 사용자가 P5 고정 씬 예제 구현과 최종 행렬·회귀·문서·PR 준비를 승인했다. **T3**다. 새 sample public API와 비동기 준비·중복 종료·caller 취소·파괴 fallback·native 종료·복원/입력 보호를 함께 다루기 때문이다. Core/NGO wrapper runtime API를 바꾸지 않는 sample 변경이라는 사실이 이 수명 위험을 없애지는 않는다. 사용자 수락과 main 병합 승인은 아직 없으며 이 작업은 PR 검토까지다.

기준 branch는 `codex/netcode-p5-sample`, wrapper main은 PR #39 병합 `3a4216b90e6f6b21a26c186661bf3dbce4221e6e`다. **P5 예제 source: `67b4612becaec08c91dfd541070b5ca4f7e039a1`**. 회귀와 Player 검증 뒤 실행 source 변경 없이 커밋했다. 신규 Unity 생성 YAML/meta 5개는 행 끝 공백만 정리했고 필드 값·GUID는 동일하다. `source-inputs-final.json`과 `whitespace-only.json`으로 실행 입력과 commit의 차이를 구분한다. P4의 별도 프로세스 true Single/Additive·false 검증은 [P4 회고](2026-10-16-04-netcode-p4-multiprocess.md)의 당시 조건으로 보존한다. 이를 P5 예제의 모든 모드 검증으로 확대하지 않는다.

개발 source `Assets/TPLab/Samples/Netcode/`에 Runtime controller/View, Editor builder, sample 소유 fixed scene/inputactions, PlayMode 테스트, README와 새 assembly 경계를 추가했다. 기존 Core/UI/Input assembly, `Assets/TPLab/Netcode/Runtime`, package/UPM source는 이번 구현에서 변경하지 않는다. [예제 사용법](../../Assets/TPLab/Samples/Netcode/README.md), [사람 API](../api/Netcode.md), [AI API](../ai/api/Netcode.md), [세션 계약](../NETCODE_SESSION_DRAFT.md)이 계약 본문이다. 이 회고가 실행 증거 요약을 소유한다.

## 구현·소유권과 결정

- 모든 peer가 같은 고정 sample scene에서 시작한다. Host+Client1+Client2, 기본 roster `host,c1,c2`, 논리 scene key `TPLab.Netcode.P5.Fixed`와 revision/version 1을 사용한다. 실제 sample Player 검증은 native scene management=true와 초기 Additive다. Single sample 전환·Bootstrap factory·공유 씬 전환을 추가하지 않았다.
- `NetcodeSessionSample`은 public `NetworkSessionContext`와 callback 계약만 사용한다. `Configure`는 활성 top-level idle NetworkManager를 빌리고 주소·native 정책의 소유권은 프로젝트에 둔다. View는 loopback `127.0.0.1`만 설정한다. 보호·Prepare·Release·Restore callback에서 빌린 manager를 파괴하지 않는다.
- StartHostAsync/StartClientAsync는 새 Context와 component 수명 동안 재사용하지 않은 Session/Attempt key로 시작한다. CompletePreparation/FailPreparation은 로컬 callback gate를 제어하며 서버의 전원 준비 승인을 대체하지 않는다. RecordGameplayInput은 Running·보호 해제·native 연결 건전성을 다시 확인한다. Interact는 동기화된 게임 상태가 아닌 로컬 카운터다.
- caller cancellation은 해당 대기만 취소한다. owned completion monitor는 실제 작업을 계속 관찰한다. ShutdownAsync는 owner 종료이며 미시작 때 null, 준비 중에는 원래 실패 정리 결과, 정상 Running 종료에는 Closed를 반환한다. sample이 Closed 뒤 로비 UI를 표시하는 것은 wrapper의 자동 RestoreLobby 호출이 아니다.
- 첫 Restore 실패는 보호와 최초 실패를 보존하고, `CanRetryLobbyRestore`가 참일 때만 같은 key의 명시적 복원을 재시도한다. Running 뒤 연결 손실에서는 입력을 닫고 명시적인 Stop을 안내한다. OnDestroy는 기다릴 수 없는 owner-shutdown fallback이며 정상 복원이나 manager 파괴를 보장하지 않는다. 파괴 뒤 cleanup callback은 managed 필드만 사용한다.
- Host와 remote admission 모두 같은 demo verifier를 사용한다. `demo-host`, `demo-c1`, `demo-c2` 고정 UTF-8 proof는 인증·발급·만료·replay 방어가 아니다. 실제 인증/Relay/로비/팀 매칭·GameSceneManager 통합은 후속이다.
- View는 Start/Stop/Retry/Quit에서 native 명령 전에 보호막을 동기 적용하고, 매 frame 보호 상태와 10Hz 상태 문구를 갱신한다. gameplay Canvas0, 보호 Canvas50, 세션 조작 Canvas100으로 Ready/Stop 등은 보호막 위에서 유지한다. 새 public ProtectionChanged event를 추가하지 않았다.
- 명시 sample 값은 전체 admission envelope4096bytes, preparation120초, shutdown/restore 각10초다. 공용 기본값이나 운영 권장값이 아니다. 단위 테스트의 2048/5/3/2와도 구분한다.

부모가 Unity·Player/프로세스 수명·Asset import·문서·Git/최종 리뷰를 소유했다. 기존 `/root/netcode_p2_review`는 gpt-6.1-sol/high로 controller/test 후보·공유 종료 결함 검토·문서 updater/회고 초안을 맡았고, `/root/netcode_boundary_review`는 같은 모델/high로 builder/probe·독립 source/실행 검토를 맡았다. 비동기 소유권 위험에 맞춰 high를 사용했다. 후보 작성은 Temp에서 분리했으며 같은 파일 동시 수정과 agent의 Unity/Git 실행은 하지 않았다. 최종 source 변경·동결·실제 Red/Green 실행은 부모가 소유했다.

## 실행 환경·명령·빌드

환경: Unity6000.3.18f1, Windows Standalone64 Development Player/Mono2x, NGO2.13.3, Unity Transport2.7.3, UniTask2.5.11, uGUI/Input System, CLI/Connector0.4.1, Python3.14. 기존 TPLab Editor PID3428을 사용했다. 모든 테스트 실행은 해당 project를 명시한다. 다른 Editor·동일 프로세스 NGO helper로 실제 세 peer 검증을 대체하지 않았다.

```text
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --filter TPLab.Samples.Netcode.Tests --output Temp/netcode-p5-20261016/<target-output>.json
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/netcode-p5-20261016/<final-edit>.json
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/netcode-p5-20261016/<final-play>.json
Get-Content Temp/netcode-p5-20261016/build-probe-v3.cs -Raw -Encoding utf8 | unity-cli exec --project C:/Users/PC/Projects/TPLab --allow-async
C:/Python314/python.exe -B Temp/netcode-p5-20261016/run_sample.py --scenario <normal|fail|leave|retry|restart> --size <1280x720|1024x768>
```

위 테스트 명령은 저장한 실행의 재현 형식이다. red-shutdown.json은 connector-file 실행 정보를 갖고 나머지 저장된 실행별 실제 mode/filter/source/PID/return code는 각 JSON의 execution에 남아 있다. runner는 실행마다 새 출력 폴더·loopback 임시 port와 자신이 시작한 Host/c1/c2 Popen을 사용한다. 외부 command 대기는 기본40초, Host listening60초, 전원 Preparing80초 등 유한 상한을 둔다. 이는 동기 hook/native 작업의 강제 중단 보장이 아니다. 종료는 sample Quit Safely/public ShutdownAsync 경로를 사용했고 OS kill을 정상 정리로 세지 않았다.

실제 sample scene와 View를 실행한 Windows graphical Player에 Temp observer DLL을 import해 command/result·상태·raycast를 관찰했다. probe는 `--p5-output` 지정 때만 활성화한다. view 공개 명령을 호출하며 core internal/reflection·예약 protocol handler 대체는 사용하지 않는다. probe의 파일 command와 frame cap60은 개발 검증 구성이고 배포 sample API/요구사항이 아니다. screenshot이 없는 현재 runner를 최종 시각 수락 증거로 쓰지 않는다.

| 빌드 보고 | 결과 | BuildReport 초 | bytes | 오류/경고 |
|---|---|---:|---:|---|
| player-build-probe-v1.json | Succeeded | 48.1458623 | 178725850 | 0/2 |
| player-build-probe-v2.json | Succeeded | 21.6192656 | 178726706 | 0/0 |
| player-build-probe-v3.json | Succeeded | 38.287986 | 178727218 | 0/2 |
| player-build-acceptance.json | Succeeded, observer DLL 제외 | 47.5055316 | 178682421 | 0/2 |

v1/v3의 두 경고는 기존 `UIContextSampleController.cs` 85/168행 CS4014다. v2 보고의 경고0을 다른 빌드 전체의 경고0으로 확대하지 않는다. 빌드는 sample scene만 명시한 BuildPlayerOptions.scenes를 사용했다. authoring.json은 기존 scene setup/BuildSettings 보존과 NGO GenerateDefaultNetworkPrefabs true→true, 기존 target/meta 충돌 거부를 기록한다.

빌드 후 알려진4개 설정 변경은 `build-effects-restored.json`에 원래/빌드 hash를 기록했고 CHECKPOINT 시점에는 원본 복원을 확인했다. 대상은 PC_RPAsset, UniversalRenderPipelineGlobalSettings, ProjectSettings, UnityConnectSettings다. 마지막 수락용 빌드 뒤에도 같은 4개만 직전 bytes로 복원했다(`acceptance-build-restored.json`). 문서 편집 전 모든 기존 추적 파일과 dirty8개가 시작 hash와 같았다. 빌드가 잠시 만든 Resources는 자체 정리됐고 별도 자산 삭제를 하지 않았다.

## TDD·동결 테스트

| 단계·증거 | 통과/실패/skip | 판정 |
|---|---|---|
| baseline-edit.json | 534/0/0 | 변경 전 전체 기준 |
| baseline-play.json | 413/0/0 | 변경 전 전체 기준 |
| red-sample.json + red-sample.xml | 1/9/0, total10 | Configure wiring만 기존 통과; 나머지는 test body의 NotImplemented 또는 예상 예외 불일치. SetUp/TearDown 원인 실패0 |
| green-sample.json + green-sample.xml | 10/0/0 | 최초 controller 구현의 targeted Green |
| red-shutdown.json + red-shutdown.xml | 0/2/0 | 추가 두 test body에서 공유 pending continuation 실패. SetUp/TearDown 원인 실패0 |
| green-all-sample.json + green-all-sample.xml | 12/0/0 | UCS monitor 수정 뒤 targeted Green |
| final-edit.json | 534/0/0 | 체크포인트 이전 중간 실행; 마지막 private View 수정 뒤 전체 회귀를 대체하지 않음 |
| resume-final-edit.json | 534/0/0 | 기존 이름 집합과 동일, CLI 직접 결과/PID49056 |
| resume-final-play.json | 425/0/0 | 기존413 이름 유지 + 신규12만 추가, CLI 직접 결과/PID15656 |
| resume-repeat-play.json | 425/0/0 | 전체 반복, 같은 이름 집합; CLI 직접 결과/PID34340 |

동결된 테스트는 초기10개 `NetcodeSessionSampleTests.cs` SHA256 `38304a50e977f44b8d1d87dd371706fe0c6c248439906299942fe89cb6db2214`, 추가2개 `NetcodeSessionSampleShutdownTests.cs` SHA256 `6a19c5c85a47cced1594d67d0958b3b6ce3ccf74217a604e1a8dfec1181e0a65`다. 독립 재검토에서 actual Assets hash와 CHECKPOINT의 동결값 일치를 확인했다. 기존10개를 고쳐 새 결함 테스트를 숨기지 않았으며 두 회귀는 별도 파일로 추가했다.

초기 Red9개는 모두 다음 message를 직접 받거나 예상 타입과 이 message가 달라 실패했다. test 이름과 구분은 다음과 같다.

```text
System.NotImplementedException : The method or operation is not implemented.
```

| test 이름(공통 NetcodeSessionSampleTests prefix 생략) | Red 원인 |
|---|---|
| ANewAttemptUsesANewContextAndCannotReuseAnOldAttemptKey | NotImplementedException |
| CallerCancellationOnlyStopsThatWait | NotImplementedException |
| DestroyFallbackRequestsOwnerShutdownAndDoesNotDestroyTheBorrowedManager | NotImplementedException |
| FailedRestoreKeepsProtectionUntilExplicitRetry | NotImplementedException |
| InvalidClientAndHostInputsLeaveTheBorrowedManagerIdle | Expected System.ArgumentException; But was System.NotImplementedException |
| InvalidOrConcurrentCommandsDoNotReplaceTheOwnedAttempt | Expected System.InvalidOperationException; But was System.NotImplementedException |
| LocalPreparationGateBlocksInputUntilServerAuthorization | NotImplementedException |
| OwnerShutdownCooperativelyDrainsPendingPreparation | NotImplementedException |
| PreparationFailureRestoresLobbyAndKeepsGameplayClosed | NotImplementedException |

공유 종료 Red는 `ConcurrentPendingShutdownCallersShareTheExactResult`와 `CancelledFirstShutdownWaiterDoesNotPreventASecondFromJoiningCleanup` 모두 다음 원문이다.

```text
System.InvalidOperationException : Already continuation registered, can not await twice or get Status after await.
```

원인은 pending async UniTask를 Preserve()만으로 공유한 것이다. 설치된 MemoizeSource는 pending continuation을 원 source로 전달하므로 동시에 기다리는 Stop/Stop·Stop/Quit·첫 wait 취소 뒤 Stop이 실패했다. 최소 수정은 하나의 owned stop monitor와 다중 대기 가능한 UniTaskCompletionSource 결과 공유다. 결과/예외를 보존하고 `_stopping`을 false로 만든 뒤 결과를 publish하며 caller token은 각 wait에만 적용한다. 두 caller는 yield 전에 등록해 frame 경합에 기대지 않는 Red/Green을 만들었다. fixture는 sample adapter 결함과 별개로 public Context shutdown을 await하고 native 정지 뒤 객체를 파괴하며 LogAssert를 마지막에 둔다.

## 실제 sample Player 성공 행렬

아래는 Unity Test Framework case 수가 아닌 runner assertion이다. 모두 true/Additive·같은 fixed sample scene·Host+2 clients다. summary의 각 report는 passed=true/errors=[]이고 세 프로세스 exit0, native stopped=true, IsPreparing=false를 확인했다. 빌린 manager가 report에 exists=true인 것은 sample이 임의 파괴하지 않는 소유권과 일치한다. 프로세스 종료를 callback manager-destroy assertion으로 바꾸어 쓰지 않는다.

| 실행 폴더(runs/ 아래) | scenario·화면 | 통과/전체 | runner wall 초 |
|---|---|---:|---:|
| 20261016T145735-normal-04d1719d | normal1280×720 | 27/27 | 8.508 |
| 20261016T150211-fail-07294508 | fail1280×720 | 16/16 | 6.772 |
| 20261016T150218-leave-77019f04 | leave1280×720 | 16/16 | 6.143 |
| 20261016T150224-retry-43d89140 | retry1280×720 | 19/19 | 6.247 |
| 20261016T150231-restart-e90982d5 | restart1280×720 | 57/57 | 10.809 |
| 20261016T150242-normal-c6d6273d | normal1024×768(4:3) | 27/27 | 8.263 |

성공6회는 독립 프로세스18개·assertion162/162다. 이 wall time은 프로세스 시작·command·준비·quit 관찰 시간이며 네트워크 지연/성능 지표가 아니다. 실패4회는 아래에 별도로 남기며 성공6회에 포함하지 않는다.

- normal은 마지막 Ready 전 보호/raycast/입력 차단과 이후 각 로컬 input1회, 정상 Stop 후 Closed·Release1·Restore0을 확인했다. 4:3도 같은 범위이며 가독성·실제 pointer/focus 수락을 대신하지 않는다.
- fail과 leave는 모두 gameplay input0, 각 Release1·Restore1·Lobby 복원을 확인했다. source c2의 로컬 실패/OwnerCancelled와 다른 peer의 정규화 사유 관찰을 구분한다.
- retry는 Host RecoveryFault에서 보호/재시작 차단·명시적 Retry를 거쳐 Lobby로 돌아왔다. Host Restore2, 다른 peer Restore1, 전원 gameplay input0·Release1이다.
- restart는 같은 프로세스/빌린 manager로 새 공통 Attempt를 사용해 두 번 시작·종료했다. 최종 각 Prepare2·Release2·Restore0·input2와 native start/stop2를 확인했다. 종료한 Context/옛 key를 재사용한 것이 아니다.

## 최초 UI Red와 하네스 실패 보존

| 실행 폴더 | 결과·원인 | 정리 관찰 |
|---|---|---|
| 20261016T144826-normal-9ca59b50 | 실제 UI 보호 순서 Red. 총2 check 중 Host 보호 check1 실패 | 세 exit0·native idle. 아직 연결하지 않은 clients를 native stop-event 성공으로 세지 않음 |
| 20261016T145112-normal-94d218f8 | 하네스 command 파일 sharing violation→screenshot command timeout. 9 check가 통과해도 실행 실패 | 세 exit0·native idle, Host/c2 observer error report 보존 |
| 20261016T145254-normal-4f51a8b1 | 같은 파일 공유 경계의 두 번째 실패. 9 check 통과와 실행 실패를 구분 | 세 exit0·native idle, Host observer error report 보존 |
| 20261016T145616-normal-965887e3 | hidden graphical Player의 캡처 실패. 10 check 중 screenshot dispatch1 실패 | 세 exit0·native idle, Host 캡처 error report 보존 |

실제 UI Red 원문 요약은 다음과 같다. command가 native 작업을 시작한 직후 coveralpha0·raycastfalse였고 Interact가 top raycast였다.

```text
AssertionError: Host command protects before returning:
{'alpha': 0.0, 'blocksRaycasts': False, 'interactable': False,
 'topRaycast': 'interact', 'topIsCover': False, 'topIsInteract': True, ...}
```

View의 status10Hz refresh에 보호막 적용도 묶였던 것이 원인이다. 기존 API 안에서 native command dispatch 전에 private ApplyProtection(true), 매 frame 보호 적용, command finally의 model 상태 refresh로 고쳤다. 새 public event·wrapper 변경 없이 후속 normal Player에서 같은 순서/raycast assertion27개가 통과했다. 최초 Red를 삭제하거나 통과로 재분류하지 않았다.

하네스 실패 두 실행의 공통 원문은 다음과 같다. 경로는 줄여 표시하며 전체 stack은 해당 summary/role report에 보존한다.

```text
System.IO.IOException: Sharing violation on path .../host-commands/004.json
at TPLab.Samples.Netcode.P5Experiment.P5SampleProbe.Update ()
TimeoutError: command host screenshot: ...
```

Temp observer reader를 FileShare.ReadWrite|Delete로 바꾸고 성공적으로 읽기/parse한 command만 seen으로 처리했다. runner command publication도 임시 파일→rename의 atomic 경로로 고쳤다. 잃은 Quit은 원자를 지켜 재발행해 해당 child들이 실제 종료되도록 했다. 제품 API·실패 기대값을 느슨하게 하여 observer 오류를 숨기지 않았다. 이전 probe DLL/runner 변형을 보존했다.

캡처 실패는 다음 두 원문이며 screenshot 성공으로 취급하지 않는다.

```text
Error: CaptureScreenshotAsTexture() failed to generate texture! Was method called before the 'end of frame' state was reached?
System.InvalidOperationException: ScreenCapture returned no texture.
```

현재 runner에서 screenshot 명령을 제외하고 실패 이력을 보존했다. 기존 Editor의 별도 Host 단독 false 시각 확인은 `visual-lobby/preparing/recovery-fault/restored/running.png`로 보존했다. 1600×900 GameView에서 상태 문구·복원 오류·보호·Ready/Retry 표시를 확인했으며 자동 raycast/상태 검증과 구분한다. 사람의 마우스/키보드·focus 이동·크기/화면비·가독성·실제 세 창 workflow 수락은 대기다.

## 문서·보호 검사·남은 gate

최종 결과: 위 전체 회귀와 반복에서 실패/skip0, 기존 테스트 이름 소실0. 동결한12개 테스트의 hash도 동일하다. probe 없는 수락용 빌드와 Editor 시각 확인을 구분했다. 마지막 compile 완료, Console 오류0·기존 UI CS4014 경고2이며 task Player 잔존0을 확인한다. 역사적 verifier17건·선언836경로의 전후 원문/hash, 상대 링크·공백·PR 규칙은 아래 최종 검사 표에 기록한다.

`update_docs.py`는 5개 현행 문서의 정확한 old sentence22개를 모두 확인한 뒤에만 갱신하며 기본 실행은 read-only다. source 전체40hex·validated flag·README/P5retro 존재가 apply의 필수 입력이다. 실행 수치는 이 회고만 소유하고 INDEX/API에는 상태와 링크를 둔다. sample README와 회고 색인을 같은 단위에서 갱신했다. 루트 README의 P5 후속 문장도 현재 수락 대기 상태와 일치시켰다. **USER ACCEPTANCE PENDING**, UPM 구성·설치·배포와 main merge는 후속 gate다.

## 임시 자료 소유와 후속

원문 공통 루트 `Temp/netcode-p5-20261016/`에 Red/Green JSON/XML, baseline/중간/최종 결과, runs별 summary·role report·Unity log·command, probe source/DLL/rsp·runner 변형·Player-probe, authoring/build 보고·설정 백업·보호 hash와 checkpoint를 보존한다. 작성에 사용한 초안은 `Temp/p5-retrospective-draft.md`에 보존한다. Temp 자료를 Git/배포 sample 계약에 포함하지 않는다. 진행 중 검증·리뷰·재현이 끝나고 사용 중인 프로세스/참조가 없음을 확인한 별도 정리에서 이 작업이 소유한 명시 경로만 삭제한다. 임시 Assets probe 폴더와 meta는 이 단위 소유 확인 후 제거했다. 수락용 Player의 Managed에는 P5SampleProbe DLL이 없으며 자동행렬용 Player-probe와 구분한다.

후속 순서는 PR/CI 검토 → 사용자 실제 수락 → 명시적 main 병합 승인이다. 현재 사용자 요청은 PR까지이며 과거 병합/절전/종료 승인이나 무응답을 이 단계의 승인으로 쓰지 않는다.

## 최종 시각 확인·정리와 검증 한계

GameView의 첫 복원 화면은 임시 스크립트가 Dropdown template의 Item Toggle을 선택해 일반 실패 복원으로 끝났다(`visual-recovery.png`). 정확한 이름 FailRestore로 다시 지정한 새 Attempt에서 RecoveryFault와 Retry를 확인했다. 제품 실패를 고친 것이 아니라 관찰 스크립트의 대상 선택을 정정한 것이다.

임시 종료 스크립트는 `UniTask<NetworkSessionResult>.AsTask` extension using/async wrapper가 없어 CLI compile error를 냈다. 해당 Play 종료는 파괴 fallback으로 구분하고 정상 await 증거로 쓰지 않는다. 별도 재진입에서 공개 Stop 버튼을 사용하고 `visual-shutdown.json`의 Lobby/IsPreparing=false/IsListening=false/ShutdownInProgress=false를 확인한 뒤 종료했다. 이미 복원된 뒤 중복 관찰 명령의 NullReferenceException도 CLI 임시 명령 오류이며 제품 Console은 오류0이었다. `visual-before.json`/`visual-after.json`의 InitScene setup과 기존 playModeStartScene을 복원했다.

단위 테스트는 모든 case에 Timeout(30000), 7초 유한 조건 대기를 두고 Release/Shutdown 완료를 기다린 뒤 IEnumerator 마지막에서 LogAssert를 검사한다. Input/EventSystem을 생성하지 않는 controller fixture이며 실제 uGUI/InputSystem은 독립 Player와 별도 Editor 화면에서 검증했다. 전역 입력 fixture에 대한 전체 PlayMode 누출은 두 전체 회귀에서 관찰되지 않았다.

GC 경로: editor authoring/초기 wiring은 P2, Start/Stop/Retry의 collection/LINQ는 P1이다. View의 매 frame 보호 토글은 직접 필드 대입이며 상태 텍스트는 10Hz 문자열 생성이 있는 예제 표시 경로다. 이 예제에 할당0이나 성능 수치를 주장하지 않는다. GC 계측·IL2CPP 호환성·WAN/Relay/실제 인증·다른 플랫폼·UPM 설치는 미실행이다. Windows Mono Player는 실행했다.

사용자 수락에는 실제 키보드/마우스·Dropdown·창 포커스/크기 변경·세 창 workflow가 남는다. 자동 Button.onClick 호출과 EventSystem raycast 관찰은 물리 입력 수락을 대체하지 않는다. 실행 방법과 기대 결과는 예제 README의 사용자 수락 표를 따른다.

## 최종 저장소 검사

| 검사 | 결과 |
|---|---|
| 전체 회귀·반복 | Edit534/534, Play425/425, Play 반복425/425; 모든 실패/skip0. 기존413개 Play 이름 유지+신규12, Edit 이름 동일 |
| 링크/anchor | Markdown266개, 상대 링크2193개, anchor496개; 오류0. 외부 HTTP1105개는 조회하지 않음 |
| 역사적 verifier | 17→17, stdout/stderr/returncode 원문 동일; 선언836경로 hash/존재 상태 동일 |
| 사용자 변경·범위 | 기존 dirty8개 hash 동일. 기존 Core/Netcode/UI/Input runtime·기존 테스트·기존 asmdef·Packages·upm·tools·workflow·AGENTS·CLAUDE 변경0 |
| 공백 | sample staged diff와 작업 문서 diff 오류0. 신규 Unity 생성 자산5개 행 끝 공백만 정리; 값/GUID 보존. 기존 dirty InitScene 공백은 손대지 않음 |
| PR 규칙 | 오류0, LINQ 경고2: builder authoring(P2), View 초기 wiring(P2)/Start 입력 가공(P1). 프레임 폴링에 LINQ를 추가하지 않음 |
| 컴파일·Console·빌드 | 기존 Editor compile 완료, 오류0·기존 UI CS4014 경고2. 수락용 Windows Mono 빌드 성공. observer DLL 없음 |
| exact-SHA CI | 문서까지 포함한 최종 PR tip으로 조회한다. 성공 여부와 SHA는 PR Checks 및 완료 보고, 로컬 ci-final.json에서 대응한다. CI 미완료를 성공으로 쓰지 않음 |

샘플 소스 commit과 문서 commit을 분리해 실제 검증 코드의 SHA를 문서에 명시했다. 로컬 브랜치 커밋·푸시·PR 생성까지 수행하며 main 병합·브랜치 삭제·PC 종료/절전은 수행하지 않는다.
