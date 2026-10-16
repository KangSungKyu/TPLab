# Netcode P4 — 다중 프로세스 wrapper 검증

## 요청·기준·통합

2026-10-16 사용자가 PR #38 병합과 P4 다중 프로세스 검증을 승인했다. PR #38의 추가 검토에서 확인된 원격 Abort 코드 보존, verifier 동기 재진입 차단, Host 검증기 원인 예외 정체성 문제는 [P3 후속 기록](2026-10-16-03-netcode-p3-adapter.md)에 Red/Green과 전체 회귀를 남기고 같은 PR에서 수정했다. 수정 source는 `ea8123c0079cb1ddeb05ff5e8d526f6cf6588d98`, 최종 PR tip은 `78197bd91047e39bd4801dede442fdc92c1fc5bc`이며 그 tip의 [Actions 38853422301](https://github.com/KangSungKyu/TPLab/actions/runs/38853422301) 성공·완전한 조회와 main 불변을 확인한 뒤 일반 merge했다. 이전 tip의 skipped/cancelled를 성공 예외로 재사용하지 않았다.

병합 결과와 local/origin main은 `9e85cbb89802231a20424c4b73af4609875da670`이다. P3 tip의 main 도달성과 worktree 미사용·원격 tip을 확인하고 `codex/netcode-p3-adapter` 원격 ref를 정확한 tip 조건으로 삭제, 로컬은 `branch -d`로 정리했다. 기존 dirty8개는 보존했다. P4 브랜치 `codex/netcode-p4-multiprocess`는 이 main에서 시작했다. 이번 P4의 영속 변경은 문서8개뿐인 **T0**이며 검증 대상 런타임은 위 source 그대로다. 실제 실험은 native 연결·종료·실패 수명을 다루므로 단순 문서 검사를 P4 증거로 대신하지 않는다. P4 PR은 사용자 검토용이며 병합하지 않는다.

## 실행 구성과 소유권

환경: Unity6000.3.18f1, Windows Standalone64 Development Player/Mono2x, NGO2.13.3, Unity Transport2.7.3, UniTask2.5.11, CLI/Connector0.4.1, Python3.14. 기존 TPLab Editor PID3428만 빌드·컴파일에 사용했다. 다른 Editor로 대체하거나 기존 Editor를 강제 종료하지 않았다. `NetworkSessionContext` public API와 명시적 NetworkManager를 사용한 임시 하네스이며 internal reflection이나 프로토콜 handler 대체는 없다. 동일 프로세스 다중 manager 실험으로 대체하지 않았다.

부모가 Unity·프로세스 수명·최종 문서·Git을 소유했다. 기존 `/root/netcode_p2_review`와 `/root/netcode_boundary_review`는 gpt-6.1-sol/high로 Temp 하네스 후보·독립 결과 검토를 분담했다. 비동기 소유권 위험에 맞춰 high를 사용했으며 같은 파일 동시 수정·agent의 Unity/Git 조작은 없었다. boundary reviewer의 최종 완료 report 감사는 `final-evidence-audit.json`에 보존했다.

- 한 실행은 Host+2 clients의 독립 프로세스3개다. 중복/명단 밖 입장만 추가 probe client1개를 사용했다. 주소는 loopback이고 실행마다 별도 포트·SessionId 맥락·출력 폴더/PID를 사용했다. AttemptId 문자열은 `attempt-1`을 사용하며 SessionId와 함께 맥락을 구분한다. 모든 시작은 숨김 프로세스다.
- true는 Host가 P4Arena를 미리 열고 시작한다. client의 initial Single/Additive 동기화 뒤 Prepare가 native 동기화·씬·spawned in-scene `Sentinel`을 확인한다. 각 프로세스의 NetworkObjectId와 초기 x=47을 대조했다. Single은 client Bootstrap 해제, Additive는 유지 여부를 확인했다. false는 각자 준비한 P4Plain 고정 씬이며 in-scene 객체 동기화 보장을 주장하지 않는다.
- 보호·Prepare·StartGameplay·Release·Restore callback, 상태, native 시작/종료 이벤트·flag, active hook, 실패 code/원격 관찰 여부, manager 파괴를 각 프로세스에서 기록했다. 정상 시작은 모든 peer의 gameplay1회, 준비 실패는0회다. 이것은 적용 횟수 관찰이며 **raw Start 송신 횟수/sequence 직접 계측은 아니다**.
- 역할별 stop 이벤트와 `!IsListening && !ShutdownInProgress && !IsServer && !IsClient`, active hook0을 확인하고 public ShutdownAsync 완료 뒤 프로젝트 소유 manager를 파괴한다. Host 손실 시에는 해당 실행이 시작한 Host PID/실행 파일을 확인한 뒤 그 프로세스만 의도적으로 종료했다. 강제 종료 Host는 정상 cleanup/최종 report 대상이 아니며 살아남은 clients의 정리·복원을 확인했다.
- 실제 인증 서비스 대신 합성 verifier와 증명, 테스트 callback/gate를 썼다. 입장 보안·증명 replay 방지·실제 root/UI 수락 증거가 아니다.

## 명령·빌드·시간 상한

임시 자료의 공통 루트는 `Temp/netcode-p4-20261016/`다. 원문 JSON/JSONL/log와 임시 소스·runner·Player는 P4 검토/재현을 위해 로컬에 보존하고 Git에 추가하지 않는다. 검토 완료 후 현재 실행/참조가 없음을 확인한 별도 작업에서 이 작업 폴더만 정리한다. 영속 계약은 Temp 하네스에 의존하지 않는다.

```text
C:/Python314/python.exe -B Temp/netcode-p4-20261016/compile_probe_v2.py
Get-Content Temp/netcode-p4-20261016/build-player-v2.cs -Raw -Encoding utf8 | unity-cli exec --project C:/Users/PC/Projects/TPLab --allow-async
C:/Python314/python.exe -B Temp/netcode-p4-20261016/run_scenarios.py --scenario <scenario> --mode <false|Single|Additive> --label <unique-label> --exe C:/Users/PC/Projects/TPLab/Temp/netcode-p4-20261016/Player-v2/TPLabP4.exe
```

하네스 DLL은 Unity 동봉 Roslyn과 현재 컴파일 assembly로 만들었다. 실제 v2 source SHA256은 `bba0ff59c57dee99aad07f605083c56b6b6682cb621dcbf7bf1bab8879a7f495`다. 기존 source에 probe C#이나 asmdef를 추가하지 않고 작업 소유 DLL/씬을 잠시 Assets에 import해 빌드했다. P4Bootstrap/P4Arena/P4Plain만 명시적인 BuildPlayerOptions.scenes에 전달했으며 사용자 EditorBuildSettings를 대체하지 않았다.

| 빌드 | 결과 | 시간(초) | bytes | 오류/경고 |
|---|---|---:|---:|---|
| v1 | Succeeded | 50.1977531 | 178664848 | 0/2 |
| v2 | Succeeded | 38.980453 | 178677136 | 0/2 |

두 경고는 기존 `UIContextSampleController.cs`의85/168행 CS4014다. 수정하지 않았으며 빌드의 오류0과 경고0을 혼동하지 않는다. 위 시간은 BuildReport의 빌드 시간이다.

하네스 값은 프로젝트 운영 기본값이 아니다. 전체 envelope4096bytes, Prepare45초(deadline 사례12초), shutdown/restore 각각10초를 명시했다. Player cold-start 설정 대기120초, runner 관찰300초와 종료 정리120초를 별도로 뒀다. 각 Player가 configured에 도달한 뒤 외부 gate로 Host→clients 시작을 열어 Mono 시작 비용을 세션 budget과 분리했다. 대기는 유한 gate/poll이며 native 작업의 강제 중단 수단으로 timeout을 쓰지 않았다. 비협조 gate는 종료 경로에서 해제한다. Player `targetFrameRate=60`은 headless 하네스 설정이다.

## 결과 행렬

아래는 **통과한16회**이며 Unity Test Framework case 수가 아닌 실행/프로세스 내부 assertion 집계다. normal Single은2회 반복했다. 시간은 runner의 프로세스 시작·gate·종료 관찰 wall time이므로 네트워크 지연이나 성능 지표로 해석하지 않는다. 전체18회 중 아래16회 통과와 뒤의 하네스 실패2회를 구분한다.

| 실행 label | 시나리오 | 모드 | 프로세스 수 | assertion 통과/전체 | wall 초 |
|---|---|---|---:|---:|---:|
| `normal-single-v2-a` | normal | Single | 3 | 41/41 | 33.558 |
| `matrix-01-normal-Additive` | normal | Additive | 3 | 41/41 | 3.710 |
| `matrix-02-normal-false` | normal | false | 3 | 35/35 | 2.462 |
| `matrix-03-slow-false` | slow | false | 3 | 35/35 | 4.303 |
| `matrix-04-prepare-fail-false` | prepare-fail | false | 3 | 39/39 | 2.365 |
| `leave-shared-read-b` | leave | false | 3 | 40/40 | 2.637 |
| `matrix-06-restore-fail-false-b` | restore-fail | false | 3 | 42/42 | 3.019 |
| `matrix-07-duplicate-false-b` | duplicate | false | 4 | 46/46 | 2.618 |
| `matrix-08-reject-false-b` | reject | false | 4 | 46/46 | 2.714 |
| `matrix-09-deadline-false-b` | deadline | false | 3 | 39/39 | 13.730 |
| `matrix-10-control-negative-false-b` | control-negative | false | 3 | 57/57 | 4.266 |
| `matrix-11-host-loss-false-b` | host-loss | false | 3 | 31/31 | 31.943 |
| `matrix-12-normal-Single-b` | normal | Single | 3 | 41/41 | 2.349 |
| `matrix-13-prepare-fail-Single-b` | prepare-fail | Single | 3 | 45/45 | 2.866 |
| `matrix-14-leave-Additive-b` | leave | Additive | 3 | 46/46 | 2.485 |
| `matrix-15-host-loss-Single-b` | host-loss | Single | 3 | 37/37 | 32.115 |

통과16회는 프로세스50개, assertion **661/661**, 정상 종료·정리48개와 의도 Host 종료2개다. 정상 종료48개는 report pass·exit0·native stop·hook0·manager 파괴를 확인했다. 실패2회까지 포함한 전체 최종 증거는 프로세스54개, assertion712개 중711통과/1실패, 정상 cleanup52개·의도 종료2개다. v1 summary는 Host 최종 report보다 먼저 작성돼 당시 snapshot707/707과 최종712개가 다르다. `host-report.json`의 늦은 실패1개를 누락하거나 실패 실행을 통과로 재분류하지 않는다.

| 보호 경계 | 실제 관찰 |
|---|---|
| 정상 시작·느린 peer | Host/c1이 준비돼도 c2 gate 전에는 gameplay0. slow에서 prepare-complete→gameplay는 Host2.0673168초/c1 2.0677192초; gate 뒤 각1회 |
| 준비 실패 | false/Single에서 c2의 LocalPreparationFailed가 Host/다른 client에 정규화 코드로 전달. source c2의 원인 예외 객체 정체성과 remote reason 관찰을 구분 |
| owner 이탈 | false/Additive에서 c2 Shutdown의 OwnerCancelled 코드 보존·전원 gameplay0·살아 있는 peer 복원 |
| 복원 callback 실패 | Host RecoveryFault 유지, native/hook 정리 완료 후 명시적 retry에서 Restore2회; 다른 peer1회. 원래 실패와 복원 오류를 보존 |
| 중복/명단 밖 입장 | dup/outside 접속은 거부되고 기존3명의 mapping·준비·시작은 유지. 실제 인증/발급 보장 아님 |
| 전체 deadline | c2 gate를 열지 않아 Host DeadlineExceeded. 실제 clients는 같은 코드를 remote=true로 관찰; 로컬 추론과 혼동하지 않음 |
| 잘못된 제어 입력 | 실제 mapped c2→Host의 stale Attempt/wrong generation Ready·Abort와 client-origin Start5개, Host→c1의 stale/wrong generation Start·Abort4개. 별도 ReliableFragmentedSequenced fence/ack 뒤2초 동안 무효과 확인 |
| 제어 입력 양성 대조 | 같은 실제 c2 sender의 유효 Abort를 이어 보냈고 Host OwnerCancelled·ParticipantId=c2·RemoteReasonObserved=true·원격 cause null 확인. 예약 handler를 대체하지 않음 |
| Host 손실 | false/Single의 owned Host를 준비 중 종료. 두 client는 ParticipantDisconnected·RemoteReasonObserved=false로 로컬 단절을 추론하고 각각 복원·정상 종료. Host가 보내지 못한 실패 사유 수신을 주장하지 않음 |

각 실행은 `runs/<UTC>-<label>-<uuid>/`에 summary, 시작 명령/PID, role별 report/assertions/events/state, Unity log를 남겼다. `matrix-progress*.json`은 실행 순서 색인이다. `final-evidence-audit.json`은 최종 report와 killed Host의 live assertion을 별도로 재집계한다.

## 최초 실패와 하네스 정정 이력

### v1 cold-start 관찰 실패

첫 `normal-single-a`에서 외부25초 Host-listening 대기가 먼저 만료됐다. 원문은 `TimeoutError: P4 external observation timed out: Host listening`이다. `host-unity.log`의 Mono assembly load는30.640초였고 client는 시작되지 않았다. Host가 늦게 진입한 뒤 참가자 없는45초 budget을 만료해 `expected-start-outcome` assertion1개가 실패했다. 최종 Host report는 nativeVerifiedStopped=true, managerDestroyed=true와 exit2를 남겼다. `late-report-observation.json`으로 지연 결과를 회수했으며 최초 실패를 삭제하지 않았다.

정정은 하네스의 configured/start gate와 cold-start120초 관찰 분리다. 제품 timeout·코드·예상 정상 시작 조건을 완화하지 않았다. v1 소스/runner/DLL과 실패 실행 `20261016T050352-normal-single-a-36ab64719807400bb079dfa6a597d79b`를 보존했다. v2 normal Single 재실행과 마지막 반복이 모두 통과했다.

### v2 observer 파일 공유 실패

첫 `matrix-05-leave-false`에서 실제 실패 코드·cleanup은 맞았지만 c1 observer가 상태 파일을 교체하는 동안 Python reader가 Windows delete-share 없이 열어 다음 원문이 발생했다.

```text
System.InvalidOperationException: Probe observer failed. ---> System.IO.IOException:
The process cannot access the file '.../20261016T051101-matrix-05-leave-false-88294c6a0a014f468e9adb2cc65723f5/c1-state.json' because it is being used by another process.
at System.IO.File.Delete (...)
at TPLab.Netcode.P4Experiment.P4SessionProbe.Write (...)
at TPLab.Netcode.P4Experiment.P4SessionProbe.Observe (...)
```

위 인용은 경로 앞부분/stack을 줄였고 전체 원문은 c1-report/summary에 보존했다. Temp runner의 state JSON reader만 CreateFileW share READ|WRITE|DELETE로 고쳤다. 제품 런타임·Player DLL·단언·기대값은 그대로다. 별도 `leave-shared-read-b`는3개 프로세스 모두 통과했고 이후 행렬도 통과했다. 이전 reader는 `run_scenarios.v2-before-share-delete.py`로 보존했다. 최초 leave 실행의 assertion40/40만 보고 실행 성공으로 취급하지 않는다(observerError 때문에 실행 실패).

### 강제 종료 Host native 로그 한계

두 killed Host의 managed `host-logs.jsonl`은 생성되지 않았다(Warning/Error 시에 생성하는 recorder이며 빈 파일을 읽은 것이 아님). live assertion은 false5/5, Single7/7 통과다. false Host의 `host-unity.log:39`에는 다음1줄이 있다.

```text
Curl error 23: Failure writing output to destination, passed 293 returned 0
```

위치: `runs/20261016T051236-matrix-11-host-loss-false-b-f03cfc1cdcdf4cc491fd3a5b1f1e91a3/host-unity.log`. 원인과 의도한 강제 종료와의 연관은 **미확인**이다. native Curl 로그를 NGO 계약 결함으로 단정하지 않고 “모든 Player 로그 오류0”도 주장하지 않는다. Single killed Host native log의 error/exception/assert 단어 검색은0이었다. 살아 있는 peer의 결과·cleanup과 killed Host 정상 종료는 별개다.

## 정리·변경 범위·검증

빌드가 자동 수정한 `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/UnityConnectSettings.asset`4개는 직전/직후 hash와 작업별 byte backup을 대조한 뒤 이 작업분만 복구했다. 사용자 원래 dirty8개의 hash는 동일하다. `Assets/TPLabP4Probe-20261016/`와 그 folder.meta만 절대 경로·소유권 확인 뒤 제거했다. 임시DLL/씬을 개발 source나 소비 예제로 남기지 않았다. 기존 InitScene setup은 복원하고 Editor refresh compile을 완료했다. 강제 종료 Host2개 외 모든 Player의 정상 종료를 확인했으며 잔존 P4 Player0이다.

영속 변경은 README, Netcode 계약, 사람/AI Netcode API, AI README, doc INDEX, 이번 회고와 회고 INDEX다. public API·XML·런타임·기존/신규 영속 테스트·asmdef·package·upm·tools·workflow·AGENTS·CLAUDE 변경0이다. API 문서는 동작 계약 변경 없이 검증 상태와 경계만 갱신했다. 과거 P3 회고 원문을 현재 결과로 덮어쓰지 않았다.

| 검사 | 결과·구분 |
|---|---|
| Player compile/build | 위2회 Succeeded, errors0/warnings2; 임시 자산 제거 후 기존 Editor compile 완료 |
| 전체 Editor 회귀 | P4에서는 새로 실행하지 않음. 이 요청의 PR38 병합 직전 P3 최종 source에서 Edit534/534·Play413/413 및 Play반복413/413, 실패/skip0이었다. 해당 source와 P4 제품 코드/테스트 byte 동일. Player assertion을 이 수에 더하지 않음 |
| 링크/anchor | 변경 전/후 오류0. 변경 후 Markdown264개·상대 링크2167개·anchor496개 검사; HTTP1105개는 fetch하지 않음 |
| 역사적 verifier | 17→17건 역사적 실패, stdout/stderr 원문 동일. 선언836경로의 현재 hash/존재 상태 동일. 과거 실패를 통과로 고치지 않음 |
| 공백·PR 규칙 | 작업 문서와 staged diff의 공백 오류0; `tools/check_pr_rules.py --base 9e85cbb89802231a20424c4b73af4609875da670 --head HEAD`로 commit diff 검사. 전체 working-tree diff에는 기존 dirty InitScene의 trailing whitespace3건이 있으며 수정하지 않음 |
| 최종 CI | 이 회고를 포함한 PR tip의 exact-SHA Actions를 조회한다. 최종 SHA/결과는 PR Checks와 완료 보고, 로컬 `ci-final.json`에서 대응하며 성공 전에 완료로 취급하지 않음 |

## 한계·다음 단계

P4는 이 Windows Mono loopback 행렬의 검증이다. 하나의 기본 세션 기준15가지 scenario/mode 조합과 normal Single 반복을 수행했으며 모든 실패×모드 조합을 검증하지 않았다. 실제 transport sender ID 자체 위조·유효한 참가자의 거짓 Ready 방지·wire Start 송신 횟수·대규모 인원·WAN/Relay·실제 인증/로비·모든 native 오류 복구는 미확인이다. 원격 실패 사유 전달은 best effort이며 프로세스 손실은 RemoteReasonObserved=false로 남는다. 운영 payload/budget 값을 이번 실험 수치로 확정하지 않는다.

**미실행:** IL2CPP/다른 플랫폼, GC·Profiler 성능 계측, 실제 프로젝트 root·UI/입력·GameSceneManager 소유권 통합, P5 소비 예제/사용자 수락, UPM 재생성·설치·배포. P5는 고정 씬·프로젝트 로비/보호 화면 callback 사용 예제와 사용자 확인을 다루는 후속이다. 공유 씬 전환과 배포는 기존 합의의 후속 범위를 유지한다. 이 P4 문서 PR은 병합하지 않고 검토를 기다린다.
