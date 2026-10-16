# Netcode P3 — adapter/callback 구현과 승인된 오라클 정정

## 요청·상태·병합

2026-10-16 사용자는 PR #37 병합 후 P3 구현을 승인했다. PR #37 tip `1329e7b816b4b565100cbe1142ae7f253d32d761`의 규칙 검사 성공과 Claude review run `38845663561`의 skipped를 확인했다. 사용자는 이 skipped 실행 한 건만 병합 gate 예외로 승인했다. 정책·workflow는 변경하지 않았다. 일반 merge 결과와 local/origin main은 `d6be3922ca2db1cbfad29c96d2425837db2a5b34`다. P2 tip 도달성과 다른 worktree 사용 여부, 원격 tip을 확인하고 `codex/netcode-p2-protocol` 원격 ref는 SHA 조건부 삭제, 로컬은 `branch -d`로 정리했다.

P3 브랜치는 `codex/netcode-p3-adapter`, 기준 HEAD는 위 merge다. 위험등급 T3: admission, native 연결/종료, 비협조 callback, 취소·복원 수명과 소유권 경계다. 승인된 기존 UniTask/Unity.Collections Runtime 참조 및 Netcode PlayMode 테스트 assembly1개를 추가했다. S3는 전체 admission envelope 상한을 프로젝트가 필수 지정하며 기본값이 없다. 새 패키지·manifest·기존 Core/UI/Input·UPM·tools·workflow 변경은 없다.

**중단 이력:** 첫 EditMode Green에서 codec 입력 오라클 오류1건을 발견했다. 사용자는 처음에 “정정 보류, 현재 결과 보고”로 답해 입력·기대값을 바꾸지 않고 미커밋 상태로 중단했다. 당시 진행 중이던 전체 PlayMode만 회수했으며 최종 EditMode·문서·CI gate는 수행하지 않았다.

**재개 승인 2026-10-16:** 사용자가 case7 입력만 `double.NegativeInfinity`로 정정하고 최초 실패·이유 보존, Red 재확인·재동결, P3 검증·문서 갱신과 PR 생성을 승인했다. 아래와 같이 입력 한 곳만 고쳤고 동결 테스트의 다른 입력·단언은 바꾸지 않았다. P3 병합은 승인되지 않았으며 PR을 열어 검토 대기로 둔다.

## 구현과 담당

- `NetworkSessionContracts` / `NetworkSessionContext`: public request/limits/verifier/callback/result, single-attempt ownership, caller wait cancellation, owner shutdown, original failure retention, bounded cleanup/restore observation and explicit retry.
- `SessionWireCodec`: strict UTF-8 framing, caller envelope cap, exact identity/generation/control validation. Project proof format is opaque.
- `SessionStartCoordinator`: explicit borrowed NGO manager, Host pre-verification once, actual sender/generation mapping, native true/false readiness, P2 barrier, normalized rejection/abort, stop events plus role flags, own handler/data restoration.
- Runtime source and new tests are only in `Assets/TPLab/Netcode`. No previous P2 tests or other runtime/tests were edited.

부모가 공용 계약·Context·계약/수명 테스트·실제 Unity 실행·Git과 이 기록을 소유했다. 기존 `/root/netcode_boundary_review`(gpt-6.1-sol/high)는 NativeAdapter test candidate와 coordinator만 담당했다. `/root/netcode_p2_review`(gpt-6.1-sol/high)는 codec/test candidate와 부모 Context 독립 읽기 검토를 담당했다. 상태·수명 위험 때문에 high를 사용했다. source/test 범위를 분리했고 Unity 실행 중 source를 동결했다. 두 agent는 Unity/Git을 조작하지 않았다. 재개 후 boundary reviewer는 사람/AI Netcode API 두 문서를 담당했고 coordinator의 구현/문서 정합을 읽기 전용으로 재확인했다. 부모가 전체 diff와 결과를 확인했다.

## 실행·동결

환경은 기존 TPLab Editor PID3428, Unity6000.3.18f1, Windows Editor Mono, NGO2.13.3/UTP2.7.3, CLI0.4.1이다. 새 Editor나 강제 종료를 사용하지 않았다. 명령은 `unity-cli editor refresh --compile --project C:/Users/PC/Projects/TPLab` 및 다음 runner 형식이다.

```text
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode|PlayMode [--filter TPLab.Netcode.Tests|TPLab.Netcode.Tests.SessionRecoveryExitTests] --output Temp/netcode-p3-20261016/<result>.json
```

| 실제 실행 | 전체 | 통과 | 실패 | skip | 근거 파일(작업 Temp) |
|---|---:|---:|---:|---:|---|
| 기존 전체 EditMode 기준선 | 460 | 460 | 0 | 0 | baseline-edit.json |
| 기존 전체 PlayMode 기준선 | 379 | 379 | 0 | 0 | baseline-play.json |
| 최초 EditMode Red: 기존 P2 62 + 신규74 | 136 | 63 | 73 | 0 | red-edit.json |
| 최초 P3 PlayMode Red | 23 | 0 | 23 | 0 | red-play.json |
| 첫 EditMode Green 시도 | 136 | 135 | 1 | 0 | green-edit-1.json |
| 첫 P3 PlayMode Green | 23 | 23 | 0 | 0 | green-play-1.json |
| 독립 검토 경계 추가 Red | 5 | 0 | 5 | 0 | red-recovery.json |
| 추가 경계 수정 후 P3 PlayMode | 28 | 28 | 0 | 0 | green-play-2.json |
| 중단 요청 당시 실행 중이던 전체 PlayMode | 407 | 407 | 0 | 0 | full-play-1.json |
| 승인된 case7 정정 후 codec Red 재확인 | 59 | 0 | 59 | 0 | red-codec-corrected.json |
| 재동결·원래 Green runtime 복원 후 codec | 59 | 59 | 0 | 0 | green-codec-corrected.json |
| 최종 전체 EditMode | 534 | 534 | 0 | 0 | final-edit.json |
| 최종 전체 PlayMode | 407 | 407 | 0 | 0 | final-play.json |
| 같은 source 전체 PlayMode 반복 | 407 | 407 | 0 | 0 | final-play-repeat.json |

Edit 신규74는 contract15 + codec59다. 초기 Red에서 request copied-snapshot 계약1건은 기존 scaffold 구현으로 이미 통과했고 나머지73건은 guard/codec 미구현으로 실패했다. 기존 P2 62건은 통과했다. Play 최초23건은 native11 + callback12이며 native 미실행/미구현 예외가 test body에서 발생했다. 추가 Red5도 모두 test body의 의도한 실패였다. SetUp/TearDown/Timeout 실패는 없다. 초기 freeze16파일 및 추가 recovery test의 hash를 보존했다. 재개 시 승인된 codec 입력 한 곳의 hash만 갱신해 재동결했으며 이후 테스트 입력·단언·실행문 변경0이다. staged 공백 검사에서 codec/recovery 신규 파일의 마지막 빈 줄1개씩만 제거했다. 정정 전/후 텍스트는 끝 공백 정규화 시 동일하며 별도 hash 기록으로 구분한다. 커밋 공백 검사에서 새 PlayMode 폴더 .meta의 Unity 생성 빈 값 뒤 공백3곳만 제거했으며 GUID·설정값은 동일하다(정정 전/후 hash 별도 보존). 전체 PlayMode의 기존379 이름은 모두 유지되며 신규28만 증가했다(`play-name-parity.json`).

Play 기준선은 CLI return1 뒤 runner가 검증한 fresh matching Connector 결과다: runId `34216-1792122027352880000`, Editor3428. 단순 CLI 직접 성공으로 기록하지 않는다. 이후 위 P3 실행은 각 result.execution에 기록된 fresh CLI 결과다. full Play 이후 Console에는 기존 singleton/provider/root 실패 테스트의 예외 로그가 남아 있으므로 제품 Console 빈 상태를 확인했다고 주장하지 않는다. 컴파일 refresh는 성공했으며 신규 P3 targeted TearDown의 마지막 LogAssert도 통과했다.

## 최초 실패와 승인된 codec 오라클 정정

[SessionWireCodecTests.InvalidControlValuesAreRejectedByBothDirections](../../Assets/TPLab/Netcode/Tests/EditMode/SessionWireCodecTests.cs)의 case7은 `kind=Welcome`, `generation=7`, `sequence=0`, `failureCode=0`, `remaining=1`을 구성한다. [SessionWireCodec.IsValidControl](../../Assets/TPLab/Netcode/Runtime/SessionWireCodec.cs)의 Welcome 계약은 finite positive remaining이며 이 입력은 유효하다. 런타임이 이를 수락하는 것이 계약과 일치한다. 테스트의 invalid 입력 선택이 잘못됐다.

최초 실패 원문:

```text
TPLab.Netcode.Tests.SessionWireCodecTests.InvalidControlValuesAreRejectedByBothDirections(7):
  Expected: <System.ArgumentException>
  But was:  null
```

원문은 `green-edit-1.json` 및 `oracle-case7-original.txt`에 보존했다. 첫 보류 당시 정정0이었고, 이후 사용자가 case7 입력만 승인했다. Welcome의 finite positive remaining 계약에 따라 `1`은 유효하고 `double.NegativeInfinity`는 무효다. 유효한1초를 runtime에서 거부하도록 변경하지 않았다.

정정 전/후(다른 case10·13은 여전히1, 다른 입력·단언 변경0):

```csharp
// Before
else if (invalid == 7 || invalid == 10 || invalid == 13) remaining = 1;
// After: user approval, case7 input only
else if (invalid == 7 || invalid == 10 || invalid == 13) remaining = invalid == 7 ? double.NegativeInfinity : 1;
```

정정 후 기존 Green runtime을 Temp에 보존하고 최초 미구현 codec scaffold만 일시 복원해 codec59건을 실제 Red로 재실행했다. case7의 의도한 Red 원문은 다음과 같다. fixture 실패나 시간 초과가 아니다.

```text
TPLab.Netcode.Tests.SessionWireCodecTests.InvalidControlValuesAreRejectedByBothDirections(7):
  Expected: <System.ArgumentException>
  But was:  null
```

미구현 scaffold가 잘못된 입력을 거부하지 않는 실패다. `frozen-tests-approved.json`으로 재동결한 뒤 원래 Green codec을 그대로 복원했다(SHA256 `f549be2dd45848fcefa9f8c6feda68423941a1a3d1f29124230db7522924b6ea`, 전후 동일). 그 결과 codec59건이 통과했다. 최초 오류·정정 전 테스트·승인된 diff·Red/Green 결과·이전/새 freeze를 보존했다. 이 정정을 통과시키기 위한 runtime 변경은0이다.

최종 전체 이름 집합은 EditMode 기존460 유지+신규74, PlayMode 기존379 유지+신규28이다. PlayMode 반복의 이름 집합과 결과도 동일하며 실패·skip 변화0이다.

## 독립 검토로 추가한 경계

| 테스트 | 실제 Red 원문 핵심 | 수정과 Green |
|---|---|---|
| AManagerCannotBeLeasedAgainUntilPendingRecoveryHasSafelyClosed | Expected: InvalidOperationException / But was: null | pending recovery 동안 lease 유지; terminal Lobby/Closed에서만 반환 |
| ShutdownCannotClaimClosedWhileAnOldRestoreHookIsPending | Expected: RecoveryFault / But was: Closed | pending restore는 bounded observation 뒤 Fault, 실제 완료 뒤 재호출로 Closed |
| ShutdownProtectsGameplayBeforeNativeShutdownOrRelease | Native shutdown must wait for the project's protection gate. Expected: True / But was: False | 보호 완료 뒤 native shutdown/release |
| SynchronousReleaseOverBudgetMustRecordCleanupTimeout | Expected: some item TimeoutException / But was: empty | callback 실행 전 cleanup deadline 고정·완료 뒤 초과 검사 |
| SynchronousRestoreOverBudgetCannotReportLobbySuccess | Expected: RecoveryFault / But was: Lobby | callback 실행 전 restore deadline 고정·완료 뒤 초과 검사 |

이5건은 현재 구현에서 실제 실패를 본 뒤 테스트를 별도 동결하고 runtime을 수정했다. 전체 원문은 `red-recovery.json`이다. 추가5와 기존23의 Green28 통과 후, 같은 source로 전체 Play407 통과했다. 독립 검토자는 pending restore 안전 종료·lease·예산·보호 선행 순서를 다시 읽었고 추가 치명적 결함을 발견하지 못했다고 보고했다. 이는 실행 검증이나 보안 보장을 대신하지 않는다.

## T3 오라클 대조

무효 wire 입력 검사는 유효 envelope의 encode/decode 성공 및 필드 roundtrip과 짝을 이룬다. 준비/보호 gate는 닫힌 동안 완료·gameplay/native shutdown이 진행하지 않음을 검사하고, gate 해제 뒤 실제 완료·상태 전이를 확인한다. remote 입장 거부는 기존 Host/mapping이 유지되고 정상 참가자로 barrier를 완료할 수 있는 경로와 함께 검사한다. 종료는 stop 이벤트와 역할/listening/shutdown 상태를 함께 관찰한다. 전체 실행·반복은 실제 비동기 정리가 다음 case로 새는지를 관찰하는 범위이며 GC 계측 양성 대조나 모든 장애 조합을 실행한 것이 아니다.

## 대기·소유권과 한계

모든 새 async Play 테스트는 Timeout30000과 유한 조건 대기를 사용한다. worker1건은 background Thread.Join2000으로 관찰한다. gate는 finally/teardown에서 해제하고 정리 완료를 기다린 뒤 IEnumerator 마지막에서 LogAssert를 호출한다. 입력/EventSystem 전역 상태를 사용하지 않아 InputTestFixture는 비적용이다. 동기 release/restore 예산 테스트2건은 시간 자체가 계약이어서 .01초 budget과100ms 동기 지연을 썼다. 강제 중단을 검증한 것이 아니다.

false Host/client 실제 loopback과 true Host·safe Additive pair를 실행했다. true Additive fixture는 transient TestRunner 씬을 공개 validation callback으로 제외하므로 실제 게임 씬 객체 동기화/true Single 다중 프로세스 증거가 아니다. P4의 별도 프로세스 wrapper 준비/실패/이탈 전파, 실제 인증·Relay, Player/IL2CPP, GC, UPM 설치·예제·사용자 UX는 미실행이다. P1의 native Player 실험을 P3 wrapper Player 검증으로 대체하지 않는다.

보호 화면 자체와 hook의 취소/늦은 작업 책임은 프로젝트다. timeout은 native/hook 강제 종료가 아니며 실제 완료 전 성공 복원이나 manager 재사용을 허용하지 않는다. 최초 실패와 추가 cleanup/restore 오류는 분리한다. 시작 승인은 분산 원자성이 아니므로 승인 뒤 송신/게임 gate 실패를 자동 로비 복원 성공으로 처리하지 않는다. 개발 public API는 XML·사람/AI API·세션 계약에 같은 단위로 갱신했다. P4/P5 실제 프로젝트 통합·수락과 마지막 배포는 남아 있다.

## 문서·범위·최종 검사

public API 추가에 맞춰 Runtime XML, 사람/AI Netcode API, 각 README/색인, NETCODE_SESSION_DRAFT의 현행 구현을 갱신했다. 새 패키지·버전·UPM 사본은 생성하지 않았다. Runtime assembly의 기존 UniTask/Unity.Collections 참조와 PlayMode assembly 추가는 이번 P3 사용자 승인 범위다. 검증 source는 `de8328039ca0c4f137c43bf09464137fa85a10af`다. 테스트 실행 뒤 EOF 공백만 정리한 것이며 실행문은 동일하다. 이후 문서 commit은 runtime/test를 바꾸지 않는다. 작업 branch의 main 대비 범위는33파일이다.

작업 시작 dirty8파일의 bytes/hash와 상태를 보존했다. 기존 tracked P2/runtime/test, Core/UI/Input, Packages, ProjectSettings, tools/workflow/UPM/AGENTS/CLAUDE 변경0을 작업 전 hash와 대조했다(원래 사용자 dirty는 비교 기준으로만 보존). 테스트 동결hash, 전체 이름 집합, 직접 보호 입력836경로와 verifier17건 역사적 실패 원문의 전후 동일성을 확인했다. 최종 링크·공백·규칙/Actions 결과는 아래 gate에 기록한다.

| Gate | 결과 |
|---|---|
| 컴파일 / Unity 회귀 | refresh 성공; 위 표의 최종 전체 EditMode/PlayMode·반복 통과 |
| API/XML/문서 | 실제 public 시그니처·취소/복원/소유권·필수 입력과 미실행 정합 검토 |
| 링크 / 공백 / PR 규칙 | 링크/anchor 오류0, scoped 공백 오류0, check_pr_rules 오류0·경고2(동기 callback budget 검사 Thread.Sleep). 보호 파일 경고0 |
| 역사적 verifier / 보호 입력 | 17→17 실패 원문 동일, 836경로 hash 동일, 신규 실패0 |
| exact-SHA Actions | source `de8328039ca0c4f137c43bf09464137fa85a10af`의 [규칙 검사 run38849894747](https://github.com/KangSungKyu/TPLab/actions/runs/38849894747) success, exact-SHA helper passed·조회 누락0. 문서 metadata 후속 HEAD는 PR 최종 보고에서 별도 확인 |
| 병합 | [PR #38](https://github.com/KangSungKyu/TPLab/pull/38) open·미병합. 사용자 승인 대기 |

독립 검토의 fixture 주의: NativeAdapterTests.Cleanup은 native stop을 유한 관찰한 뒤 시험 소유 manager를 정리한다. stop 관찰 실패 경로에서도 시험 소유 객체를 파괴한 뒤 실패 단언하는 fallback이 있다. 이는 stop 확인 전 파괴가 가능하므로 모든 실패 경로에서 native 정리 완료가 보장된다는 근거로 쓰지 않는다. 이번 Green/반복에서는 stop 단언을 모두 통과했다. frozen fixture를 이번 case7 승인 범위 밖으로 정정하지 않았고, 향후 실패 시 로그/잔여 상태 격리 조사 대상으로 남긴다. 제품 context의 manager 파괴 동작은 없다.

## 보존·미실행

사용자가 요청한 최초 실패 보존과 PR 검토·P4 인계를 위해 원시 결과·정정 전 테스트·freeze·보존 검사·명령 기록을 `Temp/netcode-p3-20261016`에 유지한다. 영속 요약은 이 회고 한 곳이며 API는 Temp에 의존하지 않는다. 해당 원문 검토와 인계가 끝나면 작업 소유 목록만 정리한다. 다른 Temp/사용자 자산은 정리하지 않는다.

미실행: P3 wrapper Player/IL2CPP, 별도 프로세스 P4 실패/이탈/위조·true Single 경로, 실제 인증/로비/Relay, GC/성능, 외부 소비 설치, P5 예제/UX 수락, UPM 재생성·배포. 이 단위는 P3 개발 소스 검증이며 Netcode 전체 또는 배포 완료가 아니다.

## PR #38 병합 전 리뷰 보완 — 2026-10-16

위 최초 P3 검증·중단·승인 기록은 당시 결과다. 이후 사용자는 PR #38 병합과 완료 후 P4를 승인했다. 병합 직전 tip `e7adb6b60564a420c496e92affdce6bf586589e1`에서 새 리뷰 3건과 Claude workflow skipped2/cancelled1을 확인해 병합을 보류했다. 규칙 run38849988483 성공만으로 전체 gate 성공으로 처리하지 않았다. 기존 PR #37의 skipped 예외는 재사용하지 않았다. 리뷰는 기존 public 계약의 결함임을 읽어 확인한 뒤 같은 PR에서 보완했다.

원인: `SessionStartCoordinator.OnMessage`가 검증한 원격 Abort 코드를 버리고 `ReportParticipantFailure`를 호출했다. `VerifyAdmission`의 프로젝트 verifier는 Context의 동기 callback guard 밖에서 실행됐다. `NetworkSessionContext.RunStartAsync`는 Host verifier의 원본 예외를 감싼 native-start 거부 예외를 public Cause로 내보냈다.

신규 [SessionReviewRegressionTests](../../Assets/TPLab/Netcode/Tests/PlayMode/SessionReviewRegressionTests.cs) 6건만 추가했고 기존 테스트는 수정하지 않았다. Red는 test body에서 아래 이유로 모두 실패했으며 SetUp/TearDown/Timeout 실패0이다. 원문 `review-red-play.json`을 작업 Temp에 보존하고 SHA256 `97cbc564fd6d5173ab856422e5df022326af3ce49bd5740dd503a556201bd2b1`로 동결했다. 동결 hash는 검증 기록에서 다시 대조한다.

| 신규 테스트 | Red 원문 핵심 | 보완 |
|---|---|---|
| ClientDeadlineExceededAbortPreservesRemoteReasonOnHost | Expected: DeadlineExceeded / But was: ParticipantPreparationFailed | sender·generation·context·connected 확인 후 정규화 code와 원격 관측 여부 보존 |
| ClientEndpointFailedAbortPreservesRemoteReasonOnHost | Expected: EndpointFailed / But was: ParticipantPreparationFailed | 위와 같음 |
| ClientOwnerCancelledAbortPreservesRemoteReasonOnHost | Expected: OwnerCancelled / But was: ParticipantPreparationFailed | 위와 같음 |
| HostVerifierCannotReenterOwnerShutdownAndAdmissionCanStillStart | Expected: System.InvalidOperationException / But was: null | private verifier adapter로 동기 재진입 guard 적용·finally에서 이전 값 복원 |
| RemoteVerifierCannotReenterOwnerShutdownAndBothPeersCanStart | Expected: System.InvalidOperationException / But was: null | 원격 승인도 같은 guard 사용, 거부 후 정상 입장·시작 유지 |
| HostVerifierExceptionPreservesOriginalResultAndAdmissionCause | Expected: same as InvalidOperationException: local admission regression sentinel / But was: InvalidOperationException: Host admission was rejected before native start. ---> local admission regression sentinel | LastAdmissionRejection.Cause 원본 우선; 없으면 기존 시작 오류 보존 |

신규 internal `ReceiveParticipantAbort`는 기존 P2 `ReportParticipantFailure`의 의미를 바꾸지 않는다. public 시그니처·assembly·패키지 변경0이다. verifier guard는 생성 시 private adapter1개를 소유하며 매 검증마다 wrapper 할당을 추가하지 않는다. GC 계측은 미실행이다. XML·사람/AI API와 P3 계약 표에 같은 경계를 명시했다.

신규 6건은 false 모드 실제 loopback으로 검증한다. 원격 Abort는 연결된 실제 sender가 raw named message를 보내고 Host가 수신할 때까지 연결을 유지해 단절과 경쟁하지 않게 했다. 모든 gate는 finally/teardown에서 해제하며 Timeout30000·8초 유한 관찰을 사용한다. 정상 임대/사용 유지와 원격 거부 뒤 정상 시작을 함께 확인한다. Input/EventSystem 비사용이다. 포트 예약을 해제한 뒤 native transport가 바인딩하기까지의 작은 경쟁 가능성은 남는다.

부모가 테스트 검토·복사·Unity·수정·문서를 수행했다. boundary reviewer(gpt-6.1-sol/high)는 Temp 신규 후보와 최종 runtime3파일 읽기 검토를 담당했으며 추가 결함을 발견하지 못했다. 기존 테스트 파일·사용자 dirty8은 보존한다. P4용 Temp probe 준비는 병렬이지만 실제 P4 실행은 병합 뒤에 한다.

| 병합 전 보완 실제 실행 | 통과/전체 | 실패/skip | 작업 Temp 결과 |
|---|---:|---:|---|
| 수정 전 전체 EditMode | 534/534 | 0/0 | review-baseline-edit.json |
| 수정 전 전체 PlayMode | 407/407 | 0/0 | review-baseline-play.json |
| 신규 Red | 0/6 | 6/0 | review-red-play.json |
| 동결 후 Green | 6/6 | 0/0 | review-green-play.json |
| 보완 후 전체 EditMode | 534/534 | 0/0 | review-final-edit.json |
| 보완 후 전체 PlayMode | 413/413 | 0/0 | review-final-play.json |
| 같은 source 전체 PlayMode 반복 | 413/413 | 0/0 | review-final-play-repeat.json |

기존534/407 이름 집합 모두 유지, Play 신규6만 증가, 반복 이름·결과 동일. 실행 환경·명령 형식은 위와 같고 신규 filter는 `TPLab.Netcode.Tests.SessionReviewRegressionTests`다. Green targeted의 첫 CLI는 실행 전 discovery 거부였고 runner의 다음 fresh 실행에서6건이 통과했다(실제 실패 재시도로 감추지 않음). compile refresh 성공; runtime 실행문 수정 뒤 전체 회귀를 했으며 마지막 XML 주석 정정 뒤 refresh·Play 반복도 통과했다. 보완 검증 source는 `ea8123c0079cb1ddeb05ff5e8d526f6cf6588d98`이며 이후 metadata commit은 runtime/test를 바꾸지 않는다. 직접 보호836경로·역사적 verifier17원문·dirty8 hash·신규 동결hash 동일, 링크 오류0. 실제 GC/Player/다중 프로세스는 여전히 이 P3 증거 범위 밖이다.
