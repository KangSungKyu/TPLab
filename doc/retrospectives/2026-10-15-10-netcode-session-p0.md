# Netcode 세션 wrapper P0 문서화

위험 등급: **T0 · 문서 전용**. 목적은 채팅의 확정 범위와 검토 보완 A–F를 저장소 계약 초안 한 곳에 옮기는 것이다. 구현·설치·package 입력 반영·테스트 실행·새 사용자 결정은 포함하지 않는다. [P0 초안](../NETCODE_SESSION_DRAFT.md)이 제안/미확인/기존 결정을 구분한다.

## 선행 병합과 작업 기준

사용자가 승인한 PR #33의 HEAD `80e02f0a6656a36e7a22cc35d5c735a3153b972e`, exact-SHA Actions completed/success, 당시 main `d14ca4c68b95bd5491ca7cc51504e3ce7932fe3f`와 원격 tip을 다시 확인한 뒤 일반 merge했다. 병합 SHA는 `6b1e638ef65c073e292b68d83a222ad31db37d09`이며 local/remote main도 같다. 승인된 tip이 main에 포함되고 다른 worktree 사용이 없음을 확인한 뒤 `codex/docs-current-headers`의 원격을 SHA lease 조건으로 삭제하고 로컬은 `git branch -d`로 정리했다. 사용자 dirty10개는 raw SHA256 동일하다. squash/rebase·이력 재작성은 없다.

GitHub REST의 `pulls?state=open&per_page=100`에서 결과0·추가 페이지 없음으로 열린 PR0을 확인했다. 같은 main에서 새 `codex/netcode-session-p0` 브랜치를 만들었다. 이 작업의 main 병합은 승인하지 않았으므로 PR 제공 뒤 멈춘다.

## 변경 파일과 책임

| 파일 | 변경 |
|---|---|
| `doc/NETCODE_SESSION_DRAFT.md` | 신규 P0 레퍼런스1개. 5줄 머리말, 0절 설치 사실, 범위/구성/식별자/정상·실패 흐름, A–F 독립 절, P0–P5, 미확인·사용자 결정 목록 |
| `doc/INDEX.md` | 이 계약 초안을 가리키는 표 행1개 |
| 이 회고 | 문서화의 source·정적 조사·검사·미실행·후속 |
| `doc/retrospectives/INDEX.md` | 기존 순번 뒤10번 행1개 |

원문에 없는 확정을 만들지 않았다. 서버 endpoint 시작과 게임 시작 승인을 구분했고 고정 명단의 미접속 멤버 입장은 허용하되 명단 밖 입장은 거부하는 제안으로 구체화했다. 기존 GameSceneManager·ISceneRoot/RootInstallation·Input/UI 선택 어댑터 경계를 읽고 근거 파일/장 링크를 붙였다. 다른 계약 문서·가이드는 바꾸지 않는다.

## 0절 사실 확인과 dirty 보호

읽은 경로는 `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt`, 실제 `Library/PackageCache` package.json과 관련 runtime/테스트 helper 소스다. `git show main:Packages/manifest.json`에서는 multiplayer.center1.0.1만 있고 NGO/UTP는 없다. 작업 폴더의 NGO는 **2.13.3**, UTP 실제 해석은 **2.7.3**이다. NGO의 UTP 선언2.6.0과 Mono Cecil 선언1.11.4를 실제 lock의2.7.3·1.11.6과 구분했다. UTP가 선언한 Collections2.2.1/Burst1.8.24/Mathematics1.3.2의 실제 lock은2.6.6/1.8.29/1.3.3이다. 간접 test-framework/performance/builtin 선언도 끝까지 확인했으며 이들을 모두 NGO의 직접 runtime 참조라고 표현하지 않았다.

종전 `@3.1` NGO 웹 문서는 버전 불일치로 미사용이다. 이번 API 근거는 설치 버전 소스만이며 웹 문서·최신 버전의 계약을 덧붙이지 않았다. PackageCache의 hash suffix를 의미 버전으로 읽지 않고 package.json의 version과 lock을 대조했다. Unity6000.3.18f1은 ProjectVersion 파일의 값이다. package.json의 최소 Unity 메타데이터는 실제 컴파일/Windows Mono·Player 지원 증거가 아니다.

보존한 dirty 목록(발생 원인은 추정하지 않음):

| 종류 | 경로 |
|---|---|
| 추적 수정 | `Assets/Scenes/InitScene.unity` |
| 추적 수정 | `Assets/Settings/DefaultVolumeProfile.asset` |
| 추적 수정 | `Assets/Settings/Mobile_RPAsset.asset` |
| 추적 수정 | `Assets/TPLab/Samples/Input/SceneTransitions/Settings/SampleInput.inputactions` |
| 추적 수정 | `Packages/manifest.json` |
| 추적 수정 | `Packages/packages-lock.json` |
| 추적 수정 | `ProjectSettings/EditorBuildSettings.asset` |
| 미추적 | `Assets/DefaultNetworkPrefabs.asset` |
| 미추적 | `Assets/DefaultNetworkPrefabs.asset.meta` |
| 미추적 | `ProjectSettings/SceneTemplateSettings.json` |

설치본은 사용자 dirty 상태이며 이 PR의 설치/manifest 승인으로 해석하지 않는다. 읽기 시작/완료 raw hash를 대조하고 위 파일을 stage하지 않는다.

## 정적 근거의 주요 관찰

1. `NetworkManager.ConnectionApprovalCallback`은 단일 callback이다. Payload와 실제 SenderId 기반 ClientNetworkId를 제공하지만 로비 ID 인증은 하지 않는다. 원격 approval Pending/Reason과 Host의 로컬 승인 처리 차이를 구분했다.
2. `HostServerInitialize`은 Approved=false라도 Host를 자동 승인한다. Host 검증은 StartHost 이전에 해야 한다는 제안을 넣었다. client의 자기 주장 ID나 이전 시도/중복 proof를 Ready 인원으로 세지 않는다.
3. 설치 테스트의 `NetcodeIntegrationTestHelpers.Create/StartInternal`은 동일 프로세스에 여러 manager를 만든다. Singleton만으로 이를 불가라고 쓰지 않았다. TPLab의 실제 격리/실행은 미확인으로 두었다.
4. `NetworkManager.Shutdown`은 flag를 설정하고 후속 NetworkUpdate/ProcessServerShutdown/ShutdownInternal이 정리한다. stop bool은 Host 역할 정보이며 성공값이 아니다. 로컬 역할/상태와 해당 세대 이벤트 관찰 제안을 P1에 연결했다. UTP driver 정리가 원격 로비 복원 완료를 뜻하지 않는다.
5. `NetworkConfig.EnableSceneManagement` 기본 true와 false의 scene object soft sync 제약을 확인했다. 고정 씬 설정은 P1 선택/검증 항목이며 공유 씬 전환이 구현됐다고 쓰지 않았다.
6. 업무 모듈 의존 Core+NGO 경계와 UniTask 직접 참조 필요를 구분했다. exact asmdef/UTP 직접 참조 여부는 미확정이며 현재 파일을 만들지 않았다.

읽은 설치 소스의 raw SHA256(영속 본문은 이 표와 버전/심볼을 근거로 재조사하며 cache 경로의 영구 존재를 보장하지 않음):

| 설치 소스 경로 | SHA256 |
|---|---|
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/package.json` | `e4afd935404f4061ca8a240cb8ec8089b5cc10b1f25bd87c747b441824fceb00` |
| `Library/PackageCache/com.unity.transport@56cfdfbf9d44/package.json` | `82d043314cfe2c6f054c89316b057871c53ccd9d1465425fabce19eda3085818` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Core/NetworkManager.cs` | `46bed269dd8feee471de4f40735ede2854797823ed94eca4c2b68f0d7c3ab94c` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Connection/NetworkConnectionManager.cs` | `f0bd3a1c4b47b3aeee9130cd834cb66245e98f1202ff2f49b5d3b361457c5d62` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Configuration/NetworkConfig.cs` | `112a63a2992cf73ac3c71a44dab707ddf4c663c3dedd6820d7c3dad223f19a94` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Messaging/Messages/ConnectionRequestMessage.cs` | `b9a848b1cd40f7099d995fcbf61211a97fed7fa7b43fb74bc22941dafd635b23` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Messaging/RpcParams.cs` | `9eb14662568bb160a67e71161136cb741818476dbbf9afaaa39c23a546d73025` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Messaging/CustomMessageManager.cs` | `4748019575ed01efaeb237760cf15505a9ab43559bebbb9ce9269b2a7d43c578` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Transports/UTP/UnityTransport.cs` | `4925659bb1d7b7d4b0a5ffeb7b5331ca74f762a5f87e96cdc4a912769b81ff53` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Tests/Runtime/TestHelpers/NetcodeIntegrationTestHelpers.cs` | `51e09c4f3d0516db37513dc40c5822a051c13a25fb603bef716bf5cf96184446` |
| `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/Unity.Netcode.Runtime.asmdef` | `782004aa13f1585acff72be0349e1645141239a9200d2a095e840c94a647f2c9` |
| `Library/PackageCache/com.unity.transport@56cfdfbf9d44/Runtime/NetworkDriver.cs` | `91201d4e3b1c46d0cd4da75df8c8233560079efd439908abc3fda5c2941bcb60` |

## 미확인과 사용자 결정

P0의 U1–U9는 다음 미확인을 뜻한다: 설치 조합 컴파일·Player/IL2CPP, TPLab 다중 instance fixture, 인증/별도 로비 사유 채널, payload·Reason 한계/보안 구성, 최종 assembly/패키지 의존, callback 멱등/부분 실패, deadline·종료/복원 budget, 실제 역할별 stop 이벤트·파괴 순서, 고정 씬 초기 동기화. 설치·컴파일·테스트·다중 프로세스는 이번에 실행하지 않았다.

사용자 선택은 계약의 번호1–8에 모았다. 추천은 내부 순수 상태기계+얇은 NGO 경계, 프로젝트의 신뢰된 proof 검증, source assembly 먼저·배포 후속, 정리가 끝난 뒤 명시적 로비 복원 재시도, 전원 하나의 deadline·느린 멤버 자동 제외 없음, 승인된 package 입력만 별도 반영, 작은 named message 후보, 고정 씬 sync 설정의 P1 검증이다. 숫자60초·payload1KiB는 미검증 제안이며 기본값으로 확정하지 않았다. 이번 문서 PR이 이 선택을 대신하지 않는다.

## 검사·명령·환경

기준 source는 `6b1e638ef65c073e292b68d83a222ad31db37d09`와 위 사용자 dirty의 설치 소스다. Windows PowerShell·Git·Python3.14로 검색/읽기와 문서 정적 검사를 수행했다. 대표 명령:

```text
git status --short
git show main:Packages/manifest.json
rg -n <심볼> Library/PackageCache/<확인한 package>/Runtime
C:/Python314/python.exe -B Temp/netcode-session-p0-20261015/verify_doc_links.py --label before/after
C:/Python314/python.exe -B tools/verify_validation.py --evidence <16개 checks 폴더>
C:/Python314/python.exe -B doc/validation/bootstrap-system/verify.py
C:/Python314/python.exe -B tools/check_pr_rules.py --base 6b1e638ef65c073e292b68d83a222ad31db37d09 --head HEAD
git diff --check 6b1e638ef65c073e292b68d83a222ad31db37d09 HEAD
C:/Python314/python.exe -B tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <40자리 HEAD> --output Temp/netcode-session-p0-20261015/ci-final.json
```

문서 머리말5필드·유형·상태·source·날짜·본 문서 계약 분류를 검사하고 머리말의 줄 번호 링크는0이어야 한다. 변경은 Markdown4개만 허용하며 doc/INDEX는 행1개, 회고 INDEX는 순번10 행1개다. 실행 코드·테스트·asmdef·tools·workflow·upm·AGENTS·CLAUDE·manifest의 이 작업 diff는0이어야 한다.

fresh 링크 기준선은 Markdown255개·상대 링크2082·anchor476·오류0이다. 초안의 최초 링크 검사에서 신규 앵커2개가 실제 장 제목과 달랐고, 대상 문서의 “준비와 실행 순서”·“해제 순서와 수명”을 직접 열어 신규 문서의 링크 대상만 보정했다. 기존 문서는 변경하지 않았다. 최종 결과는 실제 커밋 뒤 아래에 확정한다.

verifier 기준선은16개 checks 폴더와 Bootstrap1개의17실패다. 전/후 종료 코드·stdout·stderr를 비교하며 기존 역사적 실패17건을 성공으로 쓰지 않는다. 선언 hash 경로836개·설치 소스12개·dirty10개의 raw hash/존재 상태도 보존한다. 새 실패가 있으면 문서/도구를 임의 수정해 숨기지 않는다.

## 실행하지 않은 것과 후속

이 작업에서 package 설치·컴파일·EditMode/PlayMode·제품 Console·Player/IL2CPP·GC·다중 프로세스·네트워크 통신·인증 서버·복원 callback·UPM/배포는 모두 미실행이다. 문서 작성/정적 소스 읽기를 해당 동작의 성공으로 기록하지 않는다. 새 PR의 main 병합·브랜치 삭제·PC 제어도 하지 않는다.

작업 소유 Temp의 baseline/hash/검사기는 PR 검토·보완 재개의 근거로만 유지한다. 사용자 수락 후 더 이상 실행/조사 참조가 없으면 이번 작업의 명시 파일만 제거하고 다른 Temp와 사용자 파일은 보존한다. 다음 실행 단계는 사용자 설계 선택과 의존성 반영 승인을 받은 뒤 P1의 실제 컴파일/종료 관찰부터 진행한다.

## 최종 검증

실측 문서/anchor 검사는 Markdown257개·상대 링크2104개·anchor484개·오류0이다(기준선255/2082/476/0). 새 링크 오류0이다. 머리말5필드·줄 번호 링크0·계약 분류와 source를 확인했다. verifier는17실패→17실패이며 전체 exit/stdout/stderr 원문 동일, 새 실패0이다. 선언 경로836개·설치 소스12개·사용자 dirty10개의 hash/존재 상태가 같다. 문서4개 밖 Git 변경0, 기존 색인 내용 삭제/교체0을 확인했다.

임시 범위 감사의 최초 raw 비교는 branch checkout 시 CRLF→LF가 된 clean 문서2개(Bootstrap 계약·이전09회고)를 발견했다. 이전 raw hash를 CRLF 재구성으로 대조하고 Git blob 내용/diff0을 확인해 줄바꿈 정규화로 분류했다. 사용자 dirty10개는 raw bytes가 그대로다. 색인 감사도 추가 행 앞의 빈 줄을 기존 문장 변경으로 잘못 세던 검사식만 보정했다. 전역 `git diff --check`가 지적한 사용자 dirty InitScene의 기존 공백3곳은 수정하지 않고 이 PR의4문서/커밋 diff로 검사 범위를 한정한다. 이 일들은 Unity/테스트 실행 실패가 아니다.

공백·규칙 검사와 Actions는 실제 커밋 후 exact-SHA로 아래에 확정한다. PR 본문에는 이 회고 링크만 두며 main 병합은 보류한다.

문서 source commit은 `22edd6777e89609c1bb92074de1eecd2a5aa45aa`다. 커밋된4파일의 규칙 검사 **오류0·경고0**, 공백0, 링크/anchor0을 확인했다. [source exact-SHA Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38706501057)는 completed/success이며 check1/run1·대상 SHA 일치와 전체 조회 완전성을 확인했다. 이 결과 기록만 추가한 최종 HEAD도 exact-SHA로 다시 확인해 PR/채팅 보고에 연결한다. [PR #34](https://github.com/KangSungKyu/TPLab/pull/34)은 open으로 유지하며 이번 요청에서는 병합하지 않는다.
