# Netcode P1 기반 검증과 승인 의존성 반영

위험 등급: **T2 · 개발 의존성/지원 범위 추가**. 제품 wrapper나 public API 구현은 없다. 수명 실험에는 유한 조건 대기·역할별 종료 관찰·실험 소유 정리를 적용했다. 구현이 없어 Red/Green을 만들지 않았으며 실제 NGO 동작을 확인하는 P1 관찰 실험과 기존 회귀를 구분한다.

## 기준과 승인

PR #35의 승인 tip은 `ef407c3858f65c96fbe28234f8d43f2b39f29f6c`다. exact-SHA Actions·규칙/범위·dirty 보존을 재확인한 뒤 일반 merge했으며, main은 `0e2d3ce00928e033f121e6ed457a3528f690d874`로 로컬/원격이 일치했다. tip의 main 도달성과 단일 worktree·원격 tip을 확인해 `codex/netcode-admission-decisions`의 원격/로컬 ref를 정리했다. 새 브랜치는 최신 main의 `codex/netcode-p1-verification`이다.

사용자 2026-10-16 승인: S1 짧은 동기 검증 우선, S2 프로젝트 증명 replay와 wrapper 동일 Attempt 중복 제한 분담, S4 같은 검증기로 Host 시작 전 검증·해당 시도 결과 재사용. S3 상한은 P1 측정 뒤 결정하며 1KiB를 임의 확정하지 않았다. 승인 범위는 검토한 의존성과 P1까지다. P2/P3 wrapper 구현·영속 assembly·UPM/버전·배포를 시작하지 않았다. 결정은 [계약 B절](../NETCODE_SESSION_DRAFT.md#b-입장-검증-접점과-신뢰-경계)에 반영했다.

이미 사용자 작업 폴더에 설치된 package 입력을 사용했다. manifest에 NGO2.13.3 한 항목, lock에 NGO2.13.3·UTP2.7.3 추가와 기존 Mono Cecil1.11.6의 depth3→1만 반영한다. 기존 패키지 버전 변경/삭제나 UTP 직접 manifest 항목 추가는 없다. package cache 버전과 lock, 선언 의존성의 존재를 확인했다. 설치 원문의 NGO/UTP UCL 고지를 THIRD_PARTY_NOTICES에 추가하며 제3자 구현을 복사하지 않았다. 새 패키지 다운로드/업그레이드는 실행하지 않았다.

시작 dirty는 추적7+미추적3=10개다. 승인된 manifest/lock2개의 bytes는 그대로 반영하며 다른8개의 bytes/추적 상태를 보존한다. 기준선도 이미 이 설치본과 dirty를 포함한 실제 작업 폴더에서 실행한 것이며, NGO 미설치 clean main과의 비교로 주장하지 않는다.

## 환경과 실행

Windows, Unity6000.3.18f1, Unity CLI/Connector0.4.1, Python3.14, NGO2.13.3/UTP2.7.3. 기존 TPLab Editor PID3428만 사용했다. 처음 실제 CLI 호출이 시간 초과했고 runner가 `Error: no Unity instances running`으로 테스트 시작 전에 실패했다. 그때 status의 ready 표시만으로 실행을 인정하지 않았다. 사용자 복구 답변 후 같은 PID의 실제 exec·컴파일/Play 유휴 상태를 확인하고 실행했다. 최초 시작 실패 transcript는 Temp에 별도 보존했다.

P1의 정식 소스는 추가하지 않았다. 설치 API를 직접 호출하는 일회성 C# 하네스를 `Temp/netcode-p1-20261016/`에서 Unity 번들 Roslyn으로 컴파일하고 Editor Play에서 로드했다. NGO private API/패키지 테스트 helper를 복사하지 않았다. 본문 source 근거는 계약의 N1~N8(설치 파일·심볼)이다. 이 동적 하네스의 컴파일 성공을 제품 wrapper 컴파일 성공으로 표현하지 않는다.

원래 열린 InitScene은 미저장 변경0임을 확인하고 씬 구성을 기록한 뒤 빈 씬으로 격리했다. Player 빌드 때만 새 `Assets/TPLabP1NativeProbe-20261016`에 자체 DLL·빈 씬을 생성했다. 종료 후 실제 절대 경로/파일4개+폴더 meta의 소유권을 확인하고 원래 씬 구성을 복원한 뒤 이 자산만 제거했다. 영속 C#/asmdef·기존 테스트·upm·tools·workflow 변경은 없다.

하네스 컴파일에서 `Action<string,string,LogType>`를 `Application.LogCallback`으로 바꾸는 delegate 타입 정정이 있었다. 씬 복원 CLI 표현식의 `JToken.Value<bool>()`도 `ToObject<bool>()`로 정정했다. 둘 다 컴파일 단계 실패이며 해당 명령은 실행되지 않았다. NGO 실행 결과나 단언을 바꿔 이를 통과로 만들지 않았다.

## 실제 결과

아래 NUnit 수와 P1 관찰 단언 수는 서로 다른 분모다. 기존 이름 집합은 전체 배열을 정렬 비교했다.

| 구분 | 실험 전 기준선 | 실험/임시 자산 제거 뒤 | 추가 자동 설정 복원 뒤 최종 | 실패/skip |
|---|---:|---:|---:|---:|
| 전체 EditMode | 398/398 | 398/398 | 398/398 | 모두0/0 |
| 전체 PlayMode | 379/379 | 379/379 | 379/379 | 모두0/0 |

전체 suite를 같은 Editor에서 총6회(각 mode3회) 실행했으며 신규 테스트 case는 없다. 최초 prestart 실패는 이6회에 포함하지 않는다. 같은 이름 집합/통과 수를 확인했으며 테스트를 삭제·수정하지 않았다.

| P1 실행 | 관찰 단언 | 별도 측정/관찰 | 결과·범위 |
|---|---:|---:|---|
| 최초 Editor, 무효 Transport 포함 | 앞선39개 통과 | payload2·무효 시작1 | 무효 시작 뒤 cleanup 실패, 잔존1. 정상 통과로 기록하지 않음 |
| 격리한 Editor 정상 경로 | 40/40 | payload2·객체 파괴1 | error null, 실험 manager 잔존0, 약0.513초 |
| Windows Mono Player 정상 경로 | 40/40 | payload2·객체 파괴1 | error null, 잔존0, 프로세스 exit0, 하네스 약0.357초 |

Player는 동일 프로세스 안의 native Host+client이며 P4의 별도 프로세스 peer 검증이 아니다. 위 하네스 시간은 접속/관찰 코드 시간으로 Player 시작·assembly 로드·빌드 비용을 포함하지 않고 성능 기준으로 삼지 않는다. Development Windows64 Mono 빌드는 Succeeded·오류0·경고2·약115.192초·178,536,730bytes였다. 경고는 미수정 `UIContextSampleController.cs`의 CS4014(85/168행)2건이다. 이전 Player build의 경고 수와 비교하지 않았으며 범위 밖 코드 수정은 하지 않는다.

| 관찰 대상 | 확인한 내용 | 한계 |
|---|---|---|
| 명시 manager 참조·multi-instance | 직접 생성한 Host/client가 같은 프로세스에서 연결. 서버 approval의 실제 ClientNetworkId와 client.LocalClientId가 일치 | 전역 Singleton 탐색/실제 wrapper 주입 API의 구현 검증 아님 |
| 합성 ConnectionData | 총길이0·256·1,024·4,096·8,192 bytes에서 수신 배열의 exact equality 관찰 | 증명/서버 발급 시스템·최종 envelope가 없음. WAN/보안/최대치 탐색 아님. S3 기본값/상한 미확정 |
| callback thread | 관찰한 승인·Named message·stop callback은 각 프로세스의 main thread ID와 일치 | 모든 transport/callback 경로의 thread 보장으로 확대하지 않음 |
| Named message | 실제 sender·int payload·정확한1회 호출. 해지 후 같은 reliable channel의 marker가 도착해도 기존 handler 재호출0 | Ready 프로토콜/Attempt·명단 replay 구현은 P2/P3 |
| 원격 거부 | 고정 코드 AdmissionDenied 수신·client 정지·Host 기존 멤버 유지 | 실제 검증기 예외/비밀 비노출 규격·네트워크 단절 중 사유 전달 보장 아님 |
| Host | native Approved=false가 무시되어 승인됨. 정상 하네스는 Host 시작 전에 synthetic verifier1회 호출 후 같은 시작 시도의 callback에서 결과 재사용 | S4 설계 가능성 관찰이며 실제 인증·증명 소비/수명 구현 아님 |
| 정상 stop | 요청 직후 listening/role이 남고 ShutdownInProgress=true. 관찰한14개 종료는 다음1프레임에 역할별 stop 이벤트와 모든 정지 flag가 일치. Host bool은 Host 여부 | 즉시 정리/모든 플랫폼1프레임 SLA/원격 로비 복원 완료 보장 아님 |
| 재시작·파괴 | 같은 manager의 재시작2회/각 stop 관찰. running Host 파괴 시 client/server stop 각각1회·객체 제거 | 파괴는 정규 shutdown을 대신하지 않음. abrupt destroy에서 UTP shutdown 경고 관찰 |
| 고정 씬 후보 | EnableSceneManagement=false인 빈 씬에서 Editor/Player 접속 가능 | 최종 설정 선택 미확정. in-scene NetworkObject·prefab 동기화·공유 씬 전환 미검증 |

격리한 정상 하네스의 Error/Exception 로그는0이다. 의도적으로 native Host 승인을 false로 준 경고와 running 객체 파괴 시 UTP shutdown 경고는 각 실행에서 보존했다. 전체 제품 Console/실제 프로젝트의 게임 UX를 검증한 결과로 표현하지 않는다. Player 종료의 engine memory report도 GC/누수 계측으로 해석하지 않는다.

## 최초 실패 원문과 분리

Transport가 없는 NetworkConfig에서 StartHost를 시도한 최초 결과는 반환 bool이 아니라 다음 예외였다.

```text
System.Exception: There is no NetworkManager assigned to this instance!
[Netcode] [Initialize] No transport has been selected!
NullReferenceException: Object reference not set to an instance of an object
IsListening=false; ShutdownInProgress=false; IsClient=true; IsServer=true
System.InvalidOperationException: P1 cleanup did not finish: TPLab-P1-owned-invalid-transport
```

cleanup의8초 유한 조건 대기는 정상 종료를 확인하지 못했고 finally에서도 같은 실패를 보존했다(최초 실행 약16.503초·잔존1). 그 결과를 지우지 않고 최초 하네스 source/DLL/JSON을 별도 보존했다. 실험 소유 객체만 Destroy한 뒤 Play를 종료했고 다음 진입 전에 native manager0을 확인했다. 이후 **무효 설정 실험을 분리**한 하네스에서 나머지 기존 단언·정리 조건을 그대로 재실행했다. 실패 경로를 정상 종료로 인정하거나 관찰 상한을 늘리지 않았다. NGO/런타임 코드를 수정하지 않았다. P3의 필수 설정 사전 검증·비정상 정리의 오류 상태는 제안으로 보고하며 모든 start 실패 복구를 보장하지 않는다.

정상 경로의 조건 대기는6초, 정리 관찰은8초 상한이며 각 frame에서 실제 상태/이벤트를 검사한다. Sleep/Delay로 성공 시점을 맞추지 않았다. 이는 일회성 실험 보호 상한이고 runtime deadline이나 신규 [Timeout] 기본값 결정이 아니다. 프로젝트 callback·입력/EventSystem 전역 상태는 생성하지 않았다.

## 작업 중 자동 설정 차이와 보존

빌드/Editor 작업 후 시작 시 clean이던 `PC_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings.asset`, `UnityConnectSettings.asset`4개에 새 직렬화 차이를 발견했다. 원문 diff와 시작 상태를 보존하고, 기존 dirty10개의 SHA256이 그대로임을 확인한 다음 이4개에만 HEAD 복원을 적용했다. 다른 dirty에는 restore/reset/stash/clean을 사용하지 않았다. 새 차이는 렌더링 prefilter/list·Standalone batching 항목·Unity Connect enabled 값이었다. 각각의 자동 기록 주체를 별도 추적하지 않았으므로 전부 package 변경이라고 단정하지 않는다. 복원 뒤 전체 회귀를 다시 실행했다. 최종에는 이4개 diff0과 초기 dirty10 bytes 보존을 재확인한다.

## 명령과 증거

```text
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/netcode-p1-20261016/<run>-edit.json
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/netcode-p1-20261016/<run>-play.json
unity-cli exec <Temp 일회성 명령> --project C:/Users/PC/Projects/TPLab
unity-cli editor play --wait / editor stop / editor refresh --compile --project C:/Users/PC/Projects/TPLab
Unity 번들 dotnet exec csc.dll @Temp/netcode-p1-20261016/probe.rsp
TPLabP1.exe -batchmode -nographics -tplabP1Output <task Temp> -logFile <task Temp>/player-runtime.log
python tools/check_pr_rules.py --base 0e2d3ce00928e033f121e6ed457a3528f690d874 --head HEAD
python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <exact40SHA> --output <task Temp>/ci-final.json
```

실행 JSON, 최초 실패, 하네스 source/DLL, Player 출력과 작업 소유 검사기는 `Temp/netcode-p1-20261016/`에 둔다. P1 결과/S3·씬 설정 검토와 P3 실패 경로 재조사 용도로 유지하며, 수락 후 참조가 끝나면 이 작업 소유 목록만 제거한다. 원시 증거를 Git/새 validation README에 추가하지 않는다. 영속 API/가이드가 Temp 파일에 의존하지 않는다. 최초 시작 실패와 후속 정상 결과를 합쳐 성공으로 덮지 않는다.

## 문서 검사와 다음 단계

검증 수치는 이 회고 한 곳에 둔다. 링크/anchor·공백·규칙 검사, 과거 verifier17건의 exit/stdout/stderr 동일 여부, 보호836경로의 존재/해시 보존, 허용7파일·기존 dirty 보존을 검사한다. 최종 커밋·Actions·PR은 Git 이력/PR 검사와 최종 보고로 식별한다. 이 새 PR은 사용자 병합 승인 전 open으로 유지한다.

미실행: 실제 검증기/로비·암호학적 replay, wrapper 상태기계/adapter·씬 root 통합, P4 다중 프로세스/원격 네트워크·단절·복원, in-scene NetworkObject 동기화, IL2CPP/다른 플랫폼, GC/실제 성능 계측, UPM 설치·배포. P1의 native 확인을 구현/서비스 보안/전체 호환성 검증으로 확대하지 않는다. 다음 결정은 S3 상한 값과 고정 씬 설정의 적용 조건이며, P2 이후 구현은 별도 승인이다.

## 최종 정적 검사

검증 source 커밋은 `35f63b58c52a0bac655ecd9d2ec10229f216b3e0`이다. 전체 테스트·하네스 실행 당시의 C#/의존성 bytes와 같은 상태를 커밋했으며, 이후 변경은 이 회고의 검사 기록뿐이다. 이 source의 [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38831375040)는 exact-SHA/조회 완전성 gate 통과로 확인했다. 새 최종 문서 tip도 별도 exact-SHA 조회하며 결과는 PR 검사와 최종 보고에서 식별한다.

실측 링크/anchor 검사는 Markdown259개·상대 링크2,117개·anchor492개·오류0이다(HTTP는 미조회). `check_pr_rules` 오류0·경고0, 공백 오류0이며 보호 파일 변경은 없다. 원래 verifier17건은17→17 실패로 exit/stdout/stderr 전체가 동일하고 보호836경로의 존재/해시가 같다. 허용7파일만 커밋했으며 제품 C#/기존 테스트/asmdef/tools/workflow/upm diff0이다. 원래 dirty10개의 bytes는 모두 유지됐고, 그중 승인된 package 입력2개만 커밋해 나머지8개는 그대로 남겼다.

문서 줄바꿈 일괄 정규화 시도는 자동 승인 검토가 범위 밖 변경 위험으로 실행 전에 거부했다. 정규화는 생략하고 읽기 전용 감사를 수행했으며 Git의 CRLF→LF 안내를 검사 오류와 구분했다. 불필요한 정규화 승인을 새로 요구하거나 우회 실행하지 않았다.


## 2026-10-16 후속 — true 권장과 native 씬 검증

### 승인·변경 경계

사용자는 `EnableSceneManagement=true`를 기본 권장으로, `false`를 프로젝트 선택으로 정하고 GameSceneManager 역할 분리와 true P1 실행을 승인했다. 같은 단위의 후속이므로 열린 PR #36과 `codex/netcode-p1-verification`을 재사용했다. 시작 local/remote branch HEAD는 `a0497fb9e89f4f8068323ed4279e100dbf23dfef`, local/remote main은 `0e2d3ce00928e033f121e6ed457a3528f690d874`로 일치했다. 이 추가 단위에서 병합·새 패키지·제품 wrapper·GSM·공개 API를 구현하지 않았다. PR 전체는 앞선 승인 의존성 단위와 같은 T2이며, 이번 영속 diff는 문서4개뿐이다.

D-N8과 역할 표를 갱신했다. 프로젝트는 주입 NetworkManager 객체·설정 수명을, wrapper는 세션 제어·입장·Ready/시작 절차를, NGO는 native 공유 씬 동기화를 맡는 방향이다. 세부 독점 사용권 획득/충돌 검사·공개 API는 제안이다. true/false를 실행 중 묵시 전환하지 않으며, 초기 동기화 Single/Additive의 기본값도 새로 결정하지 않는다. 현행 GSM의 inventory/active-scene 검사와 NGO 변경 사이의 소유권 이관은 후속 통합으로 남긴다. 이전 절의 false 실험·미확인 문장은 당시 기록이며 아래 결과가 true 관찰 범위를 보완한다.

하위 에이전트 `/root/netcode_boundary_review`(gpt-6.1-sol / high)는 비동기 수명·소유권 검토와 `NETCODE_SESSION_DRAFT.md`만 수정했다. 추가 하네스 검토는 읽기 전용이었다. 부모가 문서·native 결과·diff를 리뷰하고 모든 Unity/Git 작업을 수행했다. 동일 파일의 동시 수정과 Unity 병렬 조작은 없었다. 하네스 검토에서 지적한 callback 실패 row·잔존 manager·양쪽 객체 ID는 외부 판정기에 모두 포함해 exit0만으로 통과시키지 않았다.

### 실행·관찰 범위

Unity6000.3.18f1 / Windows / NGO2.13.3 / UTP2.7.3 / Mono / CLI0.4.1, 기존 Editor PID3428을 유지했다. 설치 package의 공개 API만 쓰는 자체 C# 하네스를 Temp에서 컴파일했다. NetworkBehaviour·private API·package 테스트 helper·새 assembly 정의는 사용하지 않았다. 씬에는 native NetworkObject만 배치했다. 최초 컴파일의 IsSceneObject obsolete 경고는 실행 전에 공개 InScenePlaced로 바꿔 제거했으며, 실행 단언이나 제품 코드는 변경하지 않았다.

미저장 Editor 씬0을 확인한 뒤 작업 소유 폴더 `Assets/TPLabP1SceneProbe-20261016`에 DLL과 Bootstrap/Arena/Extra/Final 씬을 준비했다. BuildPlayerOptions.scenes로 해당4개만 전달했고 기존 dirty EditorBuildSettings는 바꾸지 않았다. 광역 SaveAssets는 호출하지 않았다. 빌드 명령은 CLI 전송 대기 중 `Error: timed out sending command to Unity: cannot connect to Unity`를 반환했지만 Editor의 빌드는 계속됐다. 중복 실행·강제 종료 없이 결과 파일과 Editor 복구를 확인했다. 실제 build는 Succeeded·오류0·경고2·137.792초·178,538,493bytes다. 경고2개는 앞선 P1과 같은 미수정 UIContextSampleController의 CS4014(85/168행)다.

초기 동기화 모드마다 별도 Host/Client Player 프로세스2개를 loopback으로 실행했다(총2쌍/4프로세스). 각 실행은 Host가 Arena를 로드하고 장면 객체의 x를47로 바꾼 다음 Client가 접속한다. 초기 sync 뒤 Extra Additive 로드→해제→Final Single 전환→Host 종료와 Client 종료를 관찰했다. 이는 P1 native 연동의 작은 별도 프로세스 실험이며 P4의 실제 wrapper·전원 Ready·위조/이탈/서버 손실·로비 복원 시나리오 완료가 아니다.

| 초기 동기화 모드 | Host 관찰 단언 | Client 관찰 단언 | 외부 보고서 판정 | 결과·잔존·오류 |
|---|---:|---:|---:|---|
| Single | 17/17 | 10/10 | 15/15 | 양쪽 exit0·error null·manager 잔존0·Error/Exception 로그0 |
| Additive | 17/17 | 10/10 | 15/15 | 양쪽 exit0·error null·manager 잔존0·Error/Exception 로그0 |

위 수치는 일회성 관찰 단언과 보고서 검사이며 NUnit case 수에 더하지 않는다. 각 모드1회 실행했다. 외부 판정은 모든 row.passed·error·오류 로그·종료 코드·잔존 manager·관찰 callback thread를 확인하며, Arena/Extra/Final의 NetworkObjectId·spawned·sceneObject·씬 이름을 양쪽 보고서에서 대조한다. 두 실행 모두 객체 ID는 각각1/2/3으로 일치했고 초기 server x47이 client에서도47이었다. callback에서 실패 row가 생기거나 남은 manager가 있어도 exit0을 근거로 통과시키지 않는다.

| 관찰 | 확인한 경계 | 확대하지 않는 것 |
|---|---|---|
| 초기 Single/Additive | Single의 Client Bootstrap 미유지, Additive의 Bootstrap 유지; server 설정이 client에 반영 | 프로젝트 Bootstrap/root 수명·GSM inventory 수용 보장 아님 |
| 초기 in-scene 객체 | 같은 Arena 객체 ID와 server에서 바꾼 초기 transform 동기화 | NetworkVariable/RPC/gameplay 준비 구현이나 지속 Transform 동기화 시험 아님 |
| Additive 로드·해제 | Arena 유지, Extra 객체 생성/제거, 서버 완료 명단 [0,1], timeout 명단 비어 있음 | 다수 Client·WAN·실패 복구 시나리오 아님 |
| Single 전환 | Arena/Bootstrap 제거, Final 객체 생성과 양쪽 ID 일치 | NGO/GSM 소유권 이관 구현 아님 |
| Client 권한 | client LoadScene 요청이 ServerOnlyAction, 즉시 씬 수·이벤트 수 불변 | 다른 모든 명령의 권한 검증 아님 |
| 정상 종료 | Host의 client/server stop 각1회, Client stop1회·server stop0, 각 프로세스 manager 제거 | 로비 callback·비협조 작업·모든 비정상 종료 보장 아님 |

Client의 무권한 Load 실험에서 `[ServerOnlyAction][Load] Clients cannot invoke the LoadScene method!` 경고가 실행당1건 발생했다. 의도한 경고를 원문으로 보존하며 오류0과 구분한다. 실행 하네스 시간은 Single Host3.163초/Client0.309초, Additive Host1.524초/Client0.273초이며 프로세스·Mono assembly 초기 로드와 빌드 비용을 포함하지 않는다. 성능/GC 결과가 아니다.

정상 상태 관찰 상한15초, peer 접속/초기 sync30초, finally 정리10초이며 매 frame 실제 상태/이벤트를 검사한다. 외부 실행기는 Host 준비 파일60초, 각 프로세스 종료100초 관찰 상한을 가지며 초과 시 프로세스/Editor를 강제로 끊지 않는다. 이 숫자는 실험 보호값이며 S3나 runtime deadline 정책을 결정하지 않는다. Input/EventSystem 구성 객체를 만들지 않았다.

### 정리·회귀·정적 검증

종료한 Player4개를 확인하고 원래 InitScene 구성·미저장 씬0·Play/컴파일 유휴를 확인했다. 소유 목록과 해시가 일치한 임시 Assets11개(DLL/meta·씬4쌍·폴더meta)만 제거했다. 빌드/Editor 후 시작 시 clean이던 PC_RPAsset·UniversalRenderPipelineGlobalSettings·ProjectSettings·UnityConnectSettings4개에 생긴 렌더링/배칭/preloadedAssets/Connect 차이는 Temp에 보존하고 이4개만 HEAD로 복원했다. 각 자동 기록 주체를 개별 추적한 것은 아니다. 원래 dirty8개와 package 입력은 수정하지 않았다.

검증 source는 후속 시작 HEAD `a0497fb9e89f4f8068323ed4279e100dbf23dfef`이며 제품 source/의존성 bytes는 그 상태로 유지했다. 전체 테스트 결과는 다음과 같다. 기존 이름 집합이 전후 동일하고 신규 NUnit 테스트는 없다. 각 suite는 기존 Editor PID3428에서 전후 각1회 실행했으며 실패·skip은 모두0이다.

| 기존 전체 회귀 | 실험 전 | 정리·복원 후 | 이름 집합 |
|---|---:|---:|---|
| EditMode | 398/398 | 398/398 | 동일 |
| PlayMode | 379/379 | 379/379 | 동일 |

읽기 전용 감사에서 원래 dirty8개 raw SHA256, 이 후속 단위의 허용 문서4개 이외 모든 추적 파일 bytes, 보호 선언836경로의 존재/해시가 모두 일치했다. 임시 Assets 폴더와 meta는 없다. 역사적 verifier17건은17→17 실패로 exit/stdout/stderr 전체가 동일했다. 새 실패0이라는 뜻이며 verifier 자체 통과로 표현하지 않는다. 제품 C#/기존 테스트/asmdef/tools/workflow/upm/package 입력의 이번 추가 diff는0이다.

독립 문서 검토는 현재 문서4개와 native 보고서를 읽어서 확인했다(Git diff는 해당 하위 도구의 work-tree 오류로 조회하지 못함). 이전 P0의 “이 T0 문서 PR” 문장이 현재 T2/P1 관찰과 혼동될 수 있어 PR #34 당시 기록으로 명시했다. 부모가 실제 diff를 직접 확인했다. 최종 문서 tip의 규칙·링크·공백 검사와 exact-SHA Actions 결과는 PR 검사 및 최종 보고로 식별한다.

```text
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/netcode-p1-scene-true-20261016/<baseline|final>-edit.json
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/netcode-p1-scene-true-20261016/<baseline|final>-play.json
python Temp/netcode-p1-scene-true-20261016/compile_probe.py
Get-Content Temp/netcode-p1-scene-true-20261016/build-player.cs -Raw | unity-cli exec --project C:/Users/PC/Projects/TPLab
python Temp/netcode-p1-scene-true-20261016/run_pair.py Single 27931
python Temp/netcode-p1-scene-true-20261016/run_pair.py Additive 27932
python tools/check_pr_rules.py --base 0e2d3ce00928e033f121e6ed457a3528f690d874 --head HEAD
python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <exact40SHA> --output Temp/netcode-p1-scene-true-20261016/ci-final.json
```

Temp의 `P1SceneProbe.cs`/DLL·컴파일/build 명령·Player·single-a/additive-a JSON/로그·소유 목록·전후 검사 자료는 PR36 수락과 P3 연동 검토가 끝날 때까지 보존한 뒤 그 작업 소유 목록만 정리한다. Git에 원시 증거·validation README를 추가하지 않는다. 시작 해시 수집 스크립트에서 porcelain 행두 공백을 strip한 경로 해석 오류는 저장 전에 실패했고, rstrip으로 정정 후 원래 dirty 해시를 확인했다. 이는 Unity 실행/제품 실패가 아니다.

미실행: 실제 프로젝트 검증기/인증·Ready/전원 시작·로비 복원, GameSceneManager 통합, Editor 내 true peer 실험(추가 native 실험은 별도 Mono Player), P4 실패 조합·WAN·다수 Client, IL2CPP/다른 플랫폼, GC/성능 계측, UPM 설치·배포. S3 payload 상한과 초기 Single/Additive 기본 모드는 여전히 선택 대상이며 true 권장 결정을 이 값의 결정으로 확대하지 않는다.

후속 링크/anchor 검사는 Markdown259개·상대 링크2,118개·anchor493개·오류0이다(HTTP 미조회). 전체 working tree의 `git diff --check`는 원래 dirty `Assets/Scenes/InitScene.unity`의 trailing whitespace3곳(164/276/398행)을 보고했다. 그 파일은 시작 raw SHA256과 동일하며 사용자 변경이므로 수정하지 않았다. 이 후속 문서4개와 PR commit 범위의 공백 검사를 별도로 적용한다.
