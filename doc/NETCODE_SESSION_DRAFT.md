# Netcode 세션 wrapper 계약과 개발 상태
유형: 레퍼런스
현재 상태: P3 NGO adapter/callback 개발 구현; P4 다중 프로세스·P5 예제/수락 대기
기준 source: de8328039ca0c4f137c43bf09464137fa85a10af; NGO 2.13.3 / Unity Transport 2.7.3
갱신일: 2026-10-16
계약 링크: 본 문서

## 0. 사실 확인과 근거 범위

[현행] PR #34의 P0 초안과 PR #35의 D-N1~D-N8 기록에 2026-10-16 사용자 결정 S1·S2·S4 및 S3 결정 시점, D-N8의 true 기본 권장·false 프로젝트 선택 방향을 반영했다. 2026-10-16에 PR #36 병합과 P2 프로토콜 구현, 개발 assembly2개 추가가 승인됐다. 이후 PR #37 병합과 P3 NGO 어댑터·callback 구현이 승인됐다. 현재 구현은 아래 P3 절에 두며 P4/P5와 UPM 배포는 후속이다. 아래 0절의 설치·dirty 조사는 PR #35 당시 기록이다. P1 실행 상태와 한계는 [P1 회고](retrospectives/2026-10-16-01-netcode-p1-verification.md)에 구분한다. P3에서 구현한 public API와 프로젝트 필수 입력은 [현행]으로 구분하고 미구현 대안·운영 값은 [제안]/미확인으로 남긴다.

| 상태 | 확인 대상 | 읽어서 확인한 사실·한계 |
|---|---|---|
| [현행] | main의 `Packages/manifest.json` | `com.unity.multiplayer.center: 1.0.1`은 있지만 NGO/UTP 항목은 없다. PR #35 작성 당시 main의 Git blob 조사이며 현재 manifest 상태가 아니다. NGO/Transport 의존성 입력은 이후 PR #36으로 반영됐다. |
| [현행] | 작업 폴더 manifest·lock·PackageCache | NGO는 manifest/lock/cache 모두 **2.13.3**. UTP는 manifest 직접 항목 없이 lock/cache의 **2.7.3**이다. NGO package.json의 UTP 의존 선언 **2.6.0**과 실제 해석 버전을 구분한다. |
| [현행] | NGO 직접 의존 선언 | `com.unity.transport: 2.6.0`, `com.unity.nuget.mono-cecil: 1.11.4`. lock의 실제 버전은 각각2.7.3·1.11.6이다. |
| [현행] | UTP 직접 의존 선언 | Collections2.2.1·Burst1.8.24·Mathematics1.3.2. lock의 실제 버전은 Collections2.6.6·Burst1.8.29·Mathematics1.3.3이다. |
| [현행] | 간접 의존 선언을 끝까지 따라간 lock | Collections는 Test Framework/Performance와 Mono Cecil도 선언한다. 전체 선언 그래프의 나머지 실제 버전은 Test Framework1.6.0, Performance3.5.0, ext.nunit2.0.5, builtin jsonserialize/imgui1.0.0이다. 이 목록을 NGO runtime의 직접 참조 목록으로 해석하지 않는다. |
| [현행] | Unity 버전 파일·패키지 메타데이터 | 프로젝트 `ProjectVersion.txt`는6000.3.18f1. NGO package.json의 `unity`는6000.0, UTP는2022.3이다. 최소 버전 메타데이터는 이 프로젝트의 컴파일·지원 확인을 대신하지 않는다. |
| 미확인 | Unity6000.3.18f1 / Windows Mono 컴파일·Player 지원 | **미실행**. P1의 설치·컴파일·실제 종료 관찰 선행 gate다. IL2CPP·다른 플랫폼도 미검증이다. |
| [현행] | 문서 버전 | 종전 채팅의 NGO `@3.1` 문서는 **버전 불일치, 미사용**이다. 이 초안은 NGO/UTP 웹 문서를 API 근거로 인용하지 않고 설치 소스만 사용한다. |

[현행] 기존 dirty는 추적7개와 미추적3개다. `Assets/Scenes/InitScene.unity`, `Assets/Settings/DefaultVolumeProfile.asset`, `Assets/Settings/Mobile_RPAsset.asset`, `Assets/TPLab/Samples/Input/SceneTransitions/Settings/SampleInput.inputactions`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/EditorBuildSettings.asset`이 추적 수정이다. `Assets/DefaultNetworkPrefabs.asset`, 그 `.meta`, `ProjectSettings/SceneTemplateSettings.json`은 미추적이다. **확인된 의존성 선언은 사용자 dirty의 설치 상태이며 이 문서 PR에 반영하지 않는다.** 나머지 dirty의 발생 원인을 Netcode 설치라고 추정하지 않는다.

### 설치 소스 경로와 심볼 표기

[현행] 아래 `NGO/`는 로컬 `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/`, `UTP/`는 `Library/PackageCache/com.unity.transport@56cfdfbf9d44/`의 약칭이다. 각 루트의 `package.json::version`으로 버전을 확인했다. PackageCache는 Git의 영속 계약 파일이 아니므로 이를 저장소 permalink처럼 만들지 않는다. 아래 파일·심볼은 설치 소스에서 읽은 근거이며 실행 증거가 아니다. 재조사 때 버전과 package fingerprint를 다시 확인한다.

| 근거 | 파일·심볼 | 정적 확인 |
|---|---|---|
| N1 | `NGO/Runtime/Core/NetworkManager.cs::ConnectionApprovalCallback`, `ConnectionApprovalRequest`, `ConnectionApprovalResponse` | 서버의 단일 승인 callback; 요청 Payload/ClientNetworkId, 응답 Approved/Pending/Reason/CreatePlayerObject를 제공한다. 다중 invocation callback은 setter가 거부한다. |
| N2 | `NGO/Runtime/Configuration/NetworkConfig.cs::ConnectionApproval`, `ConnectionData`, `ClientConnectionBufferTimeout`, `EnableSceneManagement` | 승인 기본 false, 연결 payload byte[], 연결 승인 timeout 기본10초, scene management 기본 true다. 이 기존 값은 wrapper의 정책 기본값이 아니다. |
| N3 | `NGO/Runtime/Messaging/Messages/ConnectionRequestMessage.cs::Serialize/Deserialize`; `NGO/Runtime/Connection/NetworkConnectionManager.cs::ApproveConnection/ProcessPendingApprovals/HandleConnectionDisconnect` | payload를 전송·역직렬화하고 transport sender를 ClientNetworkId로 callback에 연결한다. 승인 거부는 reason 전송과 disconnect 경로로 간다. 이 경로는 프로젝트 로비 ID의 진위를 인증하지 않는다. |
| N4 | `NGO/Runtime/Core/NetworkManager.cs::StartHost/HostServerInitialize` | 로컬 Host 승인 callback을 호출하나 Approved=false여도 자동 승인한다. Host 검증은 원격 거부와 별도여야 한다. |
| N5 | `NGO/Runtime/Core/NetworkManager.cs::Shutdown/NetworkUpdate/ProcessServerShutdown/ShutdownInternal`, `ShutdownInProgress`, `OnClientStopped`, `OnServerStopped` | 종료 요청과 프레임 기반 후속 정리가 분리된다. 공개 상태·이벤트가 존재하며 자세한 관찰 범위는 F절에 둔다. |
| N6 | `NGO/Tests/Runtime/TestHelpers/NetcodeIntegrationTestHelpers.cs::Create/CreateServer/CreateNewClient/StartInternal`; `NetworkManager.cs::OnEnable/Singleton` | 패키지 테스트 소스가 동일 프로세스의 server+clients를 명시적으로 만든다. Singleton 존재만으로 다중 instance 불가라고 판단할 수 없다. TPLab에서의 실행은 미확인이다. |
| N7 | `NGO/Runtime/Messaging/RpcParams.cs::RpcReceiveParams.SenderClientId/ServerRpcReceiveParams.SenderClientId`; `NGO/Runtime/Messaging/CustomMessageManager.cs::CustomMessagingManager.HandleNamedMessageDelegate/RegisterNamedMessageHandler` | RPC/이름 있는 메시지 양쪽에 실제 sender ID를 받는 접점이 있다. 전송 방식 선택과 실제 전달 검증은 아직 하지 않았다. |
| N8 | `NGO/Runtime/Connection/NetworkConnectionManager.cs::Shutdown`; `NGO/Runtime/Transports/UTP/UnityTransport.cs::Shutdown/DisposeInternals`; `UTP/Runtime/NetworkDriver.cs::Dispose` | connection manager가 transport.Shutdown을 호출한다. UnityTransport는 driver update 완료 후 내부 dispose를 호출하며 직접 종료 대신 NetworkManager 경로 사용을 안내한다. 이것은 원격 전체의 종료 확인이 아니다. |

## 1. 1차 범위와 역할

- [결정됨] 세션 wrapper와 **고정된 게임 씬에서 확정 참가자 전원 준비 후 서버 시작 승인**까지가 1차 범위다. 공유 씬 전환은 후속이다. 근거: 사용자 대화의 1차 범위 지정과 이번 P0 문서 요청.
- [결정됨] 프로젝트 로비에서 참가자 명단을 확정한 뒤 세션을 만든다. 준비 중 확정 참가자 실패·이탈이 있으면 이전 로비로 돌아가 서버/클라이언트 양측에 사유를 전달한다. 근거: 사용자의 명단 확정·실패 복원 답변.
- [제안] 인원수는 확정 명단 N명으로 표현하고 Host 자신은 플레이어일 때 한 명으로 포함한다. 전용 서버는 참가자 수에 더하지 않는다. 2vs2 팀·좌석·밸런스는 프로젝트 명단 메타데이터이며 NGO ClientId와 팀을 동일시하지 않는다. 팀 배정·매칭은 wrapper가 구현하지 않는다.
- [제안] 로비 서비스·인증·Relay 할당·주소 설정·UI·업무 로직·게임 객체 동기화는 프로젝트가 소유한다. wrapper는 NGO의 transport/RPC/NetworkVariable을 다시 구현하지 않는다. Host와 client의 고정 씬 경로부터 검증하고 전용 서버의 실제 지원은 별도 검증 상태로 기록한다.

| 상태·구성 | 소유하는 책임 | 빌리거나 맡지 않는 것 |
|---|---|---|
| [제안] 프로젝트 구성 소유자 | NetworkManager/GameObject 생성·설정·최종 파괴, NetworkTransport·주소·prefab 설정, 세션을 사용하는 프로젝트 scope의 수명 | wrapper가 사용하는 동안 같은 manager의 세션 명령·승인 callback을 임의 변경 |
| [제안] `NetworkSessionContext` | 명시적으로 주입된 NetworkManager의 세션 사용권, 자신의 구독·프로토콜 handler·연결/종료 작업·연결 mapping | 전역 Singleton 탐색, 프로젝트가 만든 GameObject의 암묵적 파괴, 로비 인증 구현 |
| [제안] `SessionStartCoordinator` | 확정 명단·시도 세대·참가자 상태, 전원 준비 barrier, 서버 승인/중단의 직렬화와 실패 결과 | 씬을 직접 로드/해제, 플레이어별 비즈니스 준비 구현 |
| [제안] 프로젝트 callback | 로컬 준비·보호 화면·게임play gate·실패 표시·로비 복원과 필요한 resource/input 수명 관리 | 공용 코드의 UI/로비 서비스 직접 의존 |
| [제안] 프로젝트 로비 | 인증된 명단과 입장 증명 발급/검증, 세션 이전 복원 정보, 복원 후 사용자 동작 | NGO ClientId를 영속 계정 ID처럼 저장 |

[제안] NetworkManager의 start/stop·승인 callback·프로토콜 handler를 이 context가 독점 사용하도록 사전에 합의한다. 주입 객체의 수명 소유자는 프로젝트이며 context는 자기 구독과 세션 종료를 책임진다. 다른 시스템과 callback을 무조건 +=/교체하지 않는다(N1). 수명/명시적 주입의 기존 예는 [Input 계약](INPUT_SYSTEM_DRAFT.md#책임과-최소-구성), [Input assembly](../Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef), [SceneRoot 계약](SCENE_ROOT.md)이다.

[현행] 독점 범위는 세션 명령·입장 승인·wrapper의 준비/시작 protocol handler다. 프로젝트 gameplay는 NGO의 NetworkObject/RPC/NetworkVariable을 직접 사용한다. StartAsync가 설정을 검증하고 manager 사용권을 획득하며 Lobby/Closed에서 반환한다. 예약 handler `TPLab.Netcode.Session.v1`은 NGO 공개 조회 수단이 없어 외부 충돌 부재까지 검증하지 않는다. RecoveryFault에서는 manager 사용권을 유지한다.

## 2. 식별자와 정상 흐름

| 상태·식별자 | 범위·의미 | 혼동하지 않는 것 |
|---|---|---|
| [제안] LobbyParticipantId | 인증된 프로젝트 계정/좌석 ID; 서버 명단의 key | 클라이언트 자기 주장 문자열 |
| [제안] SessionId | 로비 확정 후 만드는 논리 세션; endpoint 재생성과 구분 | NGO ClientId, 씬 path, 로비 이름 |
| [제안] AttemptId | 준비·시작 시도의 고유 세대; 재시도 시 새 값 | 같은 root PrepareAsync의 재호출 |
| [제안] ConnectionGeneration + NGO ClientId | 이 endpoint 실행에서 관찰한 접속과 참가자 mapping | 영속 ID 또는 다음 접속에도 유지되는 key |
| [제안] RosterRevision / MemberSet | 서버가 고정한 명단의 revision/immutable snapshot | 현재 접속자 수만 센 가변 quorum |

[제안] `StartHost/StartServer`의 **NGO endpoint 시작**과 coordinator의 **게임 시작 승인**을 구분한다. 접속을 받으려면 endpoint가 먼저 시작되어야 하며 `StartHost()==true` 또는 접속 이벤트만으로 전원 준비/게임 시작을 보고하지 않는다(N4).

| 상태 | 정상 흐름 | 다음 단계의 조건 |
|---|---|---|
| [제안] Lobby → RosterFrozen | 서버가 인증된 명단·복원 정보·프로토콜/고정 씬 식별을 고정 | 새 모집은 닫되 아직 접속하지 않은 확정 멤버는 입장 허용 |
| [제안] RosterFrozen → Connecting | Host 사전 검증, 승인 handler 설정, endpoint 시작·확정 멤버 연결 | 인증 mapping과 접속 완료를 별도로 확인 |
| [제안] Connecting → Preparing | 각 peer가 같은 고정 씬·프로젝트 준비 callback 수행 | 네트워크 연결만으로 Ready를 발행하지 않음 |
| [제안] Preparing → ReadyBarrier | 로컬 root/업무 준비 완료 후 현재 세대 Ready를 서버에 전달 | 서버의 검증된 sender mapping과 해당 명단 상태에 반영 |
| [제안] ReadyBarrier → StartAuthorized | 정확히 확정 명단 전체가 접속·인증·Ready인지를 서버가 한 직렬화 지점에서 재검사 | 중단/종료/기한 만료가 먼저 수락되지 않았을 것 |
| [제안] StartAuthorized → Running | 서버 권위의 동일 Session/Attempt/승인 sequence를 각 peer가 수락, 프로젝트가 gameplay gate를 연다 | 중복 승인은 무효과; 이전 시도 승인은 버림 |

- [제안] 시작 승인 조건은 명단 일치·고정 씬/프로토콜 일치·검증 mapping의 일대일성·로컬 준비 성공·전원 Ready·미취소/미실패·유효 기한을 모두 만족하는 것이다. 서버 권한 검사를 통과한 승인만 수락하며 client의 Start 요청이 직접 gameplay를 열지 않는다.
- [제안] 접속자가 명단보다 많거나 Ready packet이 중복되어도 준비 인원을 늘리지 않는다. 동일 Ready는 멱등 처리하고, 이전 Attempt/끊어진 ConnectionGeneration의 늦은 Ready는 무시한다. disconnect와 Start의 경합은 서버가 수락한 이벤트 순서로 결정한다. 승인 전 이탈은 중단, 승인 후 일반 게임 중 이탈은 프로젝트의 후속 gameplay 정책이다.
- [제안] 전원 준비 barrier는 모든 화면이 같은 프레임에 시작한다는 분산 원자성 보장이 아니다. 시작 승인 유실/지연 시 client는 gameplay를 닫은 채 연결 실패/기한 경로를 따른다. 공통 시작 tick 동기화는 이번 범위에 넣지 않는다.
- [제안] 승인 뒤 callback에서 새 장시간 준비를 시작하지 않는다. 실패할 수 있는 자산·업무 준비는 Ready 전에 끝낸다. gameplay gate callback의 실패 처리는 원인을 보고하고 보호 화면으로 들어가며, 승인 전 복원 계약을 승인 후 gameplay 복구 보장으로 확대하지 않는다.

## 3. 실패 흐름과 SceneRoot 정합

[제안] 준비 실패·확정 멤버 이탈·endpoint 시작 실패·세션 소유자 중단·기한 만료는 해당 Attempt를 먼저 무효화한다. 신규 Ready/Start를 거부하고 보호 화면/입력 차단 callback → 남은 peer에 중단 사유 전달 시도 → 소유 준비 취소·협조적 종료/NGO 종료 관찰 → 프로젝트 로비 복원 → 실패 결과 표시의 순서로 진행한다. 복원 실패·종료 관찰 실패는 성공적인 로비 복원으로 숨기지 않는다. 상세 출구는 D절이다.

[제안] 결과에는 Session/Attempt, 실패 단계, 최초 원인, 참가자 식별(허용 범위), 종료·복원 중 추가 실패와 원격 사유 관측 여부를 따로 남긴다. 뒤의 cleanup 예외로 최초 원인을 덮지 않는다. 통신이 끊긴 peer에게 정확한 서버 사유를 전달했다고 보장하지 않는다. 남아 있는 경로로 best effort 통지하고, 수신 불가 peer는 로컬 `Disconnected`/`DeadlineExceeded`와 “원격 사유 미확인”으로 복원한다. 프로젝트가 로비의 별도 통지 경로를 가진다면 callback으로 연결하되 그 가용성은 미확인이다.

- [현행] [ISceneRoot.PrepareAsync](../Assets/TPLab/Core/Lifecycle/ISceneRoot.cs)와 [RootInstallation.PrepareAsync](../Assets/TPLab/Core/Lifecycle/RootInstallation.cs)는 한 준비 시도를 공유한다. 호출자 token은 그 대기만 취소하고 실패는 재호출로 초기화되지 않는다. [비동기 준비 계약](ASYNC_SCENE_LIFECYCLE.md#준비와-실행-순서)도 같은 규칙이다.
- [제안] 네트워크 Ready는 root 준비 완료 뒤에만 보낸다. coordinator를 준비하는 installer가 다시 같은 root.PrepareAsync를 기다리는 순환은 만들지 않는다. root 준비 후 외부 소유자가 coordinator를 시작하거나 별도 session scope를 구성한다.
- [현행] root.ShutdownAsync는 그 root의 수명을 끝낸다. 실패/종료한 root를 재사용하지 않는다. [SceneRoot](SCENE_ROOT.md), [비동기 종료](ASYNC_SCENE_LIFECYCLE.md#해제-순서와-수명) 참조.
- [제안] 실패 시 세션용 root는 종료 후 새로 만들고, 미종료 상태의 프로젝트 로비 root는 별도 소유권에 따라 재사용 가능성을 판단한다. 무조건 Bootstrap 공용 root를 종료하지 않는다. NetworkManager native 종료와 root 종료는 각각 관찰하고 어느 한쪽을 다른 쪽 완료로 대체하지 않는다.
- [결정됨] [GameSceneManager](GAME_SCENE_MANAGER_DRAFT.md)는 로컬 씬 수명/전환, NGO는 네트워크 씬 동기화, 세션 wrapper는 입장·준비·시작 승인의 역할로 구분하며 GameSceneManager는 1차에서 확장하지 않는다. 같은 native 씬을 두 manager가 동시에 소유·전환하지 않는다. 공유 씬 전환과 기존 GameSceneManager의 inventory 등록·소유권 이관은 후속 통합 과제다. 프로젝트 복원 callback의 기존 로컬 전환 API 사용도 이 소유권 경계를 지켜야 하며, NGO가 로드한 씬을 현행 GameSceneManager가 자동 수용한다고 보장하지 않는다. 근거: 사용자 1차 범위 지정 및 2026-10-16 역할 분리 결정.
- [결정됨] `EnableSceneManagement=true`를 기본 권장하고 `false`는 프로젝트의 명시적 선택으로 둔다. true에서는 NGO의 native 씬 동기화를 사용하고, false에서는 필요한 씬 동기화·in-scene NetworkObject 처리를 프로젝트가 책임진다. wrapper가 실행 중 이 값을 묵시적으로 전환하지 않는다. 이 정책은 기존 GameSceneManager와의 공유 씬 전환 통합 완료를 뜻하지 않는다. 근거: 사용자 결정 2026-10-16; native true 동작의 P1 관찰 범위와 통합 미검증은 아래 P1 표와 회고를 따른다.
- [제안] true 경로의 준비 완료는 native 동기화 완료와 프로젝트 root/업무 준비 완료를 모두 요구한다. 원격 client의 초기 `OnSynchronizeComplete`와 Host 자신의 로드 완료는 서로 다른 관찰 경로이며, Host가 자기 SynchronizeComplete를 기다리는 계약으로 만들지 않는다. 후속 씬 이벤트는 완료 명단과 timeout 명단을 함께 확인한다. false 경로는 프로젝트가 로컬 씬 구성·준비 결과를 제공한다. Single/Additive 초기 동기화 모드는 프로젝트 구성으로 별도 선택·검증하며 여기서 기본 모드를 임의 확정하지 않는다.
- [제안] 프로젝트의 gameplay gate는 로컬 준비/씬 진행 가능 조건과 서버 시작 승인을 함께 요구한다. GameSceneManager의 준비/reveal만으로 네트워크 게임 입력을 열지 않는다. 복원을 실행하는 scope는 NGO 종료 관찰과 프로젝트 로비 복원 동안 유지해야 한다.


## A. 테스트 구조와 NGO 어댑터 경계

[현행] 기존 Input은 native Input System을 별도 [TPLab.Core.Input](../Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef)으로 분리한다. UI 연결도 선택 [TPLab.UI.InputSystem](../Assets/TPLab/UI/InputSystem/Runtime/TPLab.UI.InputSystem.asmdef)/[UIInputSystemAdapter.Bind](../Assets/TPLab/UI/InputSystem/Runtime/UIInputSystemAdapter.cs)와 `UIContext.Input.cs::AttachInputBridge`의 좁은 delegate 접점으로 연결한다. Input 전체가 순수 상태기계라는 뜻은 아니다.

| 상태·안 | 장점 | 비용·검증의 한계 |
|---|---|---|
| [제안] D-N1의 A1 세부 경계: 순수 상태기계 + 얇은 NGO 어댑터 | 명단·attempt·Ready·중복·실패 전이를 NGO 객체 없이 EditMode에서 검사. NGO handler는 sender/사건을 상태 입력으로 번역하고 실행 효과를 전달 | 상태기계 입력/효과의 최소 경계가 필요. 같은 assembly 안 internal 클래스로 시작하면 컴파일 시 NGO 참조는 여전히 필요하다. NGO 미설치 상태에서도 컴파일되는 별도 순수 assembly는 추가 승인 대상이며 이번 추천 범위가 아니다. |
| [제안] A0 분리하지 않음 | 처음의 클래스/전달 코드 수가 적다 | 프로토콜 조합마다 NetworkManager/PlayerLoop fixture가 필요해 통합 테스트 비용·전역 상태 의존이 커진다. 순수 규칙만 따로 검사하기 어렵다. |

[제안] D-N1로 A1 방향을 채택한 뒤의 구체화 후보: 명단·시도·준비 전이 한 단위만 internal 순수 로직으로 두고 어댑터는 start/승인/메시지/종료 관찰에 한정한다. A0는 비교용 대안으로 남기며 이번 채택안이 아니다. 일반 네트워크 framework, 모든 메서드의 interface, NGO package 내부 helper 복사·reflection·private API 의존은 만들지 않는다. package 테스트 helper를 TPLab의 public 지원 API로 간주하지 않는다.

[현행] N6는 단일 프로세스에서 여러 NetworkManager를 생성·시작하는 소스 근거다. `NetworkManager.OnEnable`은 Singleton이 없을 때만 설정하며 인스턴스별 참조도 존재한다. 따라서 “NGO는 무조건 한 프로세스 한 instance”는 이 소스로 뒷받침되지 않는다. **미확인:** 향후 wrapper 테스트 fixture의 격리. P1에서는 일회성 하네스로 같은 프로세스의 여러 manager를 Editor/Windows Mono Player에서 관찰했다. 실패해도 임의 package helper/새 패키지를 가져오지 않는다. 그 경우 실제 승인/transport/종료의 peer 간 검증은 P4 다중 프로세스로 넘기고, P2 EditMode 프로토콜 검증을 그 대체물로 보고하지 않는다. 단일 프로세스가 가능해도 프로세스 종료·실제 단절·개별 로그/복원은 P4에서 별도 확인한다.

### P2 내부 프로토콜 구현

[현행] 2026-10-16 사용자 구현 승인과 별도 assembly 승인에 따라 [Runtime](../Assets/TPLab/Netcode/Runtime)에 내부 순수 상태기계를 구현했다. `TPLab.Netcode`는 Core·Unity.Netcode.Runtime을 참조하는 하나의 개발 assembly다. 순수 타입도 이 assembly 안에 있으며 NGO 미설치용 별도 assembly가 아니다. P2 당시에는 `TPLab.Netcode.EditModeTests`만 friend로 열었고 runtime 타입은 모두 internal이었다. P3가 public wrapper와 PlayMode friend를 추가했으며 아래 순수 프로토콜 타입은 internal을 유지한다. 실제 검증은 [P2 회고](retrospectives/2026-10-16-02-netcode-p2-protocol.md), 외부 사용 상태는 [사람](api/Netcode.md)/[AI](ai/api/Netcode.md) 안내를 따른다.

| 내부 구성 | 소유하는 상태·입력 | 책임의 끝 |
|---|---|---|
| `SessionProtocolContext` | SessionId·AttemptId·RosterRevision·ProtocolVersion·SceneKey 불변 값; 문자열 Ordinal 비교 | 인증 증명·wire 포맷·씬 로딩 아님 |
| `ServerSessionProtocol` | 복사한 고정 명단, 검증된 참가자 ID와 실제 ClientId/ConnectionGeneration mapping, connected/Ready·서버 준비 | 승인된 값을 입력받는 순수 규칙; 검증기 실행·NGO 콜백 조작 없음 |
| `ClientSessionProtocol` | 신뢰된 adapter가 지정한 서버 연결과 현재 맥락, 로컬 준비·단일 승인 수락 | sender 자체의 네트워크 진위 확인은 NGO adapter가 제공 |
| `SessionAttemptState` | Preparing→StartAuthorized 또는 Aborted, 고정 deadline·최초 실패·승인 sequence | native 종료·로비 복원·실제 Running 관찰 아님 |

[현행] 서버는 각 멤버의 인증 mapping, 실제 연결, Ready를 별도로 기록한다. Host의 ClientId0도 일반 멤버로 처리하고 서버 자신의 준비는 별도 사실이다. 명단 밖·중복 참가자/ClientId·옛 맥락·접속 세대0 요청은 현재 mapping/Ready를 바꾸지 않고 거부한다. 같은 attempt에서 승인된 멤버는 교체하지 않는다. 현재 연결 세대와 일치하는 승인 멤버의 이탈/준비 실패, 서버 로컬 실패·owner 취소·endpoint 실패·deadline만 attempt를 중단한다. 최초 실패 객체와 로컬 Exception 원인은 보존하며 원격 client에는 enum 코드만 수락한다.

[현행] `TryAuthorizeStart`는 서버 로컬 준비와 명단 전원의 admitted/connected/Ready를 한 직렬화 지점에서 확인한 뒤 sequence1을 한 번 발행한다. Client는 지정 서버 ID+세대, 전체 맥락, localReady와 양수 sequence를 확인해 한 번 수락한다. 동일/다른 sequence의 후속 승인과 terminal 이후 늦은 입력은 모두 무효과다. 승인 뒤 이탈은 준비 실패로 되돌리지 않으며 이후 gameplay 정책은 후속이다. 준비 이전에 도착한 시작 신호는 보관하지 않는다. adapter는 localReady 기록 후 Ready를 송신해야 한다.

[현행] `startedAt`, `budget`, `now`는 같은 로컬 단조 clock의 표본(초)이다. budget은 호출자가 반드시 지정하며 유한 양수·표현 가능한 미래 deadline만 허용한다. pending 입력에서 NaN/무한/역행 시각은 상태 변경 전에 거부하고, `now >= startedAt + budget`이면 그 입력보다 만료가 우선한다. 새 입장/부분 Ready로 budget을 늘리지 않는다. terminal 입력은 시각을 포함해 무시한다. 서버·클라이언트의 clock을 같은 원점으로 가정하지 않는다. P3는 Welcome의 서버 잔여 budget으로 client의 기존 로컬 deadline을 줄이며 P2가 분산 clock 동기화를 구현하지 않는다.

[현행] 역할별 호출은 adapter/소유자가 직렬화해야 한다. 순수 상태기계에 Unity 메인 스레드 검사·lock·callback·비동기 대기는 없다. caller의 대기 취소는 이 상태의 Cancel 입력으로 전달하지 않는다. 상태 Aborted는 무효화 결과이며 정리 완료가 아니다. 종료·복원 오류의 추가 집계와 안전한 재시도 출구는 아래 P3/D절과 public Context가 담당한다. 새 세션 시작은 새로운 Attempt 맥락과 새 protocol 객체로 시작한다.

### P3 NGO 어댑터와 callback 구현

[현행] [NetworkSessionContext](../Assets/TPLab/Netcode/Runtime/NetworkSessionContext.cs)와 [공개 계약](../Assets/TPLab/Netcode/Runtime/NetworkSessionContracts.cs)을 개발 source에 제공한다. `SessionStartCoordinator`와 `SessionWireCodec`, P2 프로토콜은 internal이다. `TPLab.Netcode`는 승인된 Core·NGO·UniTask·Unity.Collections를 참조하며 Netcode PlayMode 테스트 assembly1개가 추가됐다. 기존 Core/UI/Input/UPM 참조나 package 입력은 바꾸지 않았다. [사람 API](api/Netcode.md), [AI API](ai/api/Netcode.md), 실행·미실행은 [P3 회고](retrospectives/2026-10-16-03-netcode-p3-adapter.md)를 따른다.

| 경계 | 구현 계약 |
|---|---|
| 공개 진입 | 명시적으로 빌린 manager + 프로젝트 callback + 필수 options + Host verifier. `StartAsync`는 한 context에서 한 attempt만 허용 |
| 시작 전 | 활성 top-level idle manager, transport, client/server 모드, ConnectionApproval=true, 비어 있는 approval callback 검사. Singleton 탐색/프로젝트 GameObject 파괴 없음 |
| 프로젝트 설정 | EnableSceneManagement는 프로젝트 값 그대로. true 권장/false 선택. Host는 초기 Single/Additive 모드를 request에 명시. 전용 서버 StartServer API 없음 |
| 입장 | 프로젝트의 동기 검증기, 전체 envelope 상한·구조·맥락 검사 후 실제 sender와 검증된 ID/세대 연결. Host는 StartHost 전 검증1회 후 로컬 callback에 같은 결과 재사용 |
| 준비 | Protect 완료 → endpoint 시작 → native 연결/초기 동기화 완료 → Prepare 완료 → localReady 기록 → Ready 송신/전원 barrier → 서버 승인 → 동기 StartGameplay |
| native 준비 | Host는 자기 connected 사실을 사용하며 자기 SynchronizeComplete를 기다리지 않음. true client는 connected + local synchronization + Welcome을 모두 요구. false client는 connected + Welcome 이후 프로젝트 Prepare가 씬 준비를 책임짐 |
| 대기 | 전체 envelope 상한과 준비/정리/복원 budget을 프로젝트가 모두 지정. 숫자 기본값 없음. 동기 hook도 호출 전 deadline을 잡고 완료 후 초과 확인; 강제 중단 아님 |
| 명령 취소 | caller token은 그 대기만 취소. owner Shutdown이 소유 attempt를 중단. Start 재호출은 재대기 API가 아님 |
| 실패 정리 | 첫 실패 보존 → 무효화/취소 → 보호 완료 → best-effort abort와 native shutdown → 실제 prepare 종료 후 release → 역할별 stop events + role/listening/shutdown flags 관찰. 실패 전파는 분산 원자성 보장이 아님 |
| 복원 | 정리 성공 후 RestoreLobbyAsync. 최초 실패와 추가 복원/정리 오류를 분리. 동일 SessionId/AttemptId key로 명시 RetryLobbyRestoreAsync. 이전 hook이 남아 있으면 병렬 복원·새 owner 금지 |
| 안전 종료 | Running의 정상 Shutdown은 자동 로비 복원 없이 Closed. pending restore는 RecoveryFault를 유지하고 실제 완료 뒤 Shutdown 재호출로 Closed 가능. lease는 Lobby/Closed에서만 반환 |
| 시작 승인 이후 실패 | 일부 peer가 이미 시작했을 수 있으므로 송신/StartGameplay 실패는 자동 복원을 주장하지 않고 보호·정리 후 RecoveryFault. 프로젝트의 명시적 복원/안전 출구 필요 |
| 스레드·재진입 | public Context는 Unity main thread. callback의 동기 실행 중 명령 재진입 거부. hook이 첫 await 뒤 자기 session 완료를 기다리는 순환은 지원하지 않음 |

[현행] `TPLab.Netcode.Session.v1` Named message 이름은 wrapper 전용으로 예약한다. NGO 공개 API에는 기존 이름 등록 조회가 없으므로 다른 소유자의 충돌을 모두 사전 검출한다고 보장하지 않는다. 사용하는 동안 승인 callback, connection data, wrapper channel과 native 세션 명령을 다른 코드가 교체하지 않아야 한다. 종료 후 자신의 handler/approval과 차용 connection data를 정리·복원한다. 프로젝트는 일반 gameplay RPC/NetworkVariable/NetworkObject를 직접 사용한다.

[현행] P3의 false peer 및 true Host/safe Additive peer 테스트는 동일 Editor 프로세스 관찰이다. safe Additive fixture는 TestRunner 씬을 제외하므로 실제 게임 씬 객체 동기화·true Single 별도 프로세스 wrapper 증거가 아니다. 인증 서비스·실제 증명 재사용 방지·Relay·P4 다중 프로세스·P5 예제·Player/IL2CPP·GC·UPM 설치는 별도 미검증이다. GameSceneManager와 같은 씬 이중 소유를 허용하거나 공유 씬 통합을 완료한 구현이 아니다.

## B. 입장 검증 접점과 신뢰 경계

[현행] B1~B8은 D-N2의 프로젝트 검증기 주입과 S1의 동기 우선·S2의 replay 책임 분담·S4의 Host 사전 검증 방향을 구체화한다. wrapper는 증명 형식이나 인증 발급 서비스를 선택하지 않는다. P3의 `IAdmissionVerifier`/`AdmissionRequest`/`AdmissionDecision`과 정규화 코드가 현행 개발 API다. 전체 payload 상한은 S3에 따라 프로젝트 필수 입력이며 증명 내용·운영 값은 프로젝트가 정한다. 비동기 Pending 대안은 미구현이다.

[현행] N1~N3의 `NetworkConfig.ConnectionApproval=true`, `ConnectionData`, `ConnectionApprovalCallback`은 기존 NGO 접점이다. Payload는 client가 보낸 바이트이고, callback의 ClientNetworkId는 N3의 `NetworkContext.SenderId`에서 전달된다. config hash/ClientId 자체는 로비 계정 소유권 증명이 아니다. N1의 callback은 단일 등록이며 등록만 하고 ConnectionApproval을 켜지 않으면 승인 기능이 활성화되지 않는다. N7의 named message/RPC sender를 Ready 등의 실제 발신자 mapping에 사용할 접점도 존재한다. 아래의 wrapper 동작은 이 NGO API가 이미 제공하는 기능이라는 뜻이 아니다.

### B1. 책임 분담

| 상태·책임 | 프로젝트 검증기·인증 기반 | wrapper |
|---|---|---|
| [현행] 발급·진위·계정/좌석 | 증명 발급·진위/만료 검증, 인증된 계정과 로비 좌석의 연결 확인 | 증명 내부를 해석하지 않고 검증된 LobbyParticipantId를 받음 |
| [현행] 입력 외곽 검사 | wrapper 검사를 통과한 불투명 증명을 자기 규격으로 검증 | 검증기 호출 전에 전체 payload 바이트 상한·wrapper 외곽 프로토콜 버전·구조 검사. 증명 형식을 지정하는 검사가 아님 |
| [현행] 세션·명단 정합 | 전달받은 신뢰된 Session/Attempt/RosterRevision 맥락에 증명 binding을 확인 | 서버의 현재 Attempt/RosterRevision과 일치하는 요청인지 검사하고 검증기가 돌려준 ID를 확정 명단과 대조 |
| [현행] replay·중복 | 증명 nonce/일회성 소비·재사용 방지·재발급 정책 | 같은 Attempt에서 같은 ID의 살아 있는 접속1개 제한과 보류 예약 충돌 검사. 증명 replay 검증을 대체하지 않음 |
| [현행] 실제 발신자 mapping | NGO 객체를 직접 변경하지 않고 검증 결과만 반환 | 실제 NGO ClientId+접속 세대와 검증된 ID를 일대일로 연결. payload의 자기 주장 ClientId로 연결하지 않음 |
| [현행] 응답·진단 | 거부 결과 또는 서버 관찰용 실패 원인 전달 | 승인/거부 NGO 응답 구성, 코드 정규화, 원격에는 정규화 코드만 전달하고 서버 결과/진단에 원인 보존 |

[현행] wrapper의 외곽 payload는 버전·세대 맥락과 프로젝트 증명 바이트를 구분한다. 서명/ticket 포맷을 검사기에서 강제하지 않는다. N3는 NGO callback 전에 byte[]를 역직렬화하므로 wrapper 상한이 그 이전의 NGO 할당·모든 transport 입력까지 제한한다는 보장은 하지 않는다. 입력 상한과 검증기 호출 순서는 P3 계약이며 S3 상한은 프로젝트 필수 입력이다.

[현행] 검증 전 보류 작업은 실제 sender/ConnectionGeneration으로 식별한다. 검증되지 않은 자기 주장 참가자 ID만으로 좌석을 선점하지 않는다. 검증된 ID를 받은 뒤 같은 Attempt의 살아 있는 mapping/검증된 예약과 충돌하는지 확인하고, 중복 검사와 mapping 공개를 한 직렬화 지점에서 처리한다.

### B2. 검증기 입출력과 A1 연결

[현행] `IAdmissionVerifier.Verify(AdmissionRequest)`는 `AdmissionDecision`을 반환하는 동기 public 계약이다. 입출력은 NGO 객체 타입을 노출하지 않는 값/증명 snapshot이다. NGO ClientId는 원시 값으로 받고 NetworkManager·ConnectionApprovalResponse·FastBufferReader를 검증기에 넘기지 않는다. 이 방향이 NGO 미설치 컴파일을 위한 별도 assembly 생성을 뜻하지 않는다(D-N1).

| 상태·방향 | 최소 값 | 출처·소유권·확인 |
|---|---|---|
| [현행] 입력 | 읽기 전용 불투명 증명 바이트 | wrapper가 외곽 검사 뒤 제공. ReadOnlyMemory<byte>로 전달하며 envelope decode/request 생성에서 복사; 검증기는 동기 계약 |
| [현행] 입력 | SessionId, AttemptId, RosterRevision | 서버의 현재 맥락. client가 주장한 값을 신뢰 값으로 대체하지 않음 |
| [현행] 입력 | 실제 NGO ClientId의 원시 값, ConnectionGeneration | 접속 상관관계용이며 그 자체로 인증 증명이 아님 |
| [현행] 입력 | 검증 기준 시각의 서버 제공 값, 남은 검증 budget | 클라이언트 시계 사용 금지. ServerUtcNow는 DateTimeOffset, RemainingSeconds는 로컬 단조 deadline 잔여 초; 인증 만료와 session deadline을 혼동하지 않음 |
| [현행] 승인 출력 | 검증된 LobbyParticipantId 개념의 string ParticipantId | 빈 ID는 InvalidVerifierResult, 명단 밖 ID는 UnknownParticipant로 거부. 이 출력만으로 Ready가 되지 않음 |
| [현행] 거부 출력 | AdmissionRejectionCode의 정규화 거부 코드 | 임의 원격 표시 문자열을 전달하지 않음. AdmissionDecision/AdmissionRejectionCode이며 원문 예외는 서버 로컬 AdmissionRejection.Cause에만 보존 |

### B3. 동기와 비동기 Pending

| 상태·안 | 이점 | 제약·추천 |
|---|---|---|
| [결정됨] 동기 검증 우선(S1) | callback 안에서 짧게 판정, 늦은 완료/예약 수명 관리가 작음 | 캐시된 검증 자료 등으로 blocking 없이 끝나는 경로부터 지원. 네트워크 I/O를 동기로 막는 구현을 허용하는 결정 아님. 근거: 사용자 결정 2026-10-16 |
| [제안] 비동기 검증 + NGO Pending | 프로젝트 인증 기반의 비동기 조회 연결 가능 | 요청·접속·owner 수명/취소·기한·늦은 결과 관리 필요. S1에 따라 현재 지원 범위 밖이며 실제 요구 확인 뒤 별도 결정 |

[현행] N1 응답은 Pending을 제공하고, N3의 `ProcessPendingApprovals`는 Pending=false인 응답만 승인/거부 처리한다. N1 주석은 나머지 필드를 먼저 설정한 뒤 Pending=false로 완료를 알리도록 설명한다. N4의 Host 경로는 Pending 대기 없이 로컬 응답을 사용하므로 remote의 Pending과 같은 방식으로 Host 사전 검증 완료를 기다릴 수 있다고 해석하지 않는다.

[제안] 비동기를 허용하면 완료 시 서버의 Attempt·RosterRevision·ClientId+ConnectionGeneration·해당 보류 예약·owner 수명·유효 기한을 모두 다시 검사한다. 정상이면 승인/거부 필드 확정 뒤 Pending=false를 반영한다. 만료/단절/이전 시도의 늦은 결과는 승인·mapping·Ready를 복원하지 못하고, 해당 요청의 예약/관측 자원만 정리한다. 동명이인 새 접속의 예약을 옛 작업이 해제하지 않게 세대를 대조한다. owner의 취소 요청과 실제 검증 작업 종료도 구분한다.

[현행] 동기 검증기는 남은 전체 준비 budget 안에서 끝나야 하며 호출 전·후 시각을 검사한다. NGO 자체 승인 한계는 별도다. blocking 검증기를 강제로 중단하지 못하므로 초과 검출/거부와 실행 중단 보장을 구분한다. [제안] 향후 비동기 검증을 추가한다면 취소·늦은 결과·실제 작업 해제를 별도로 설계한다.

### B4. 검증기 실패와 호출 맥락

| 상태·실패 | 해당 접속 처리 | 세션·진단 |
|---|---|---|
| [현행] 검증기 예외 | VerifierFailed 정규화 코드로 거부 | 예외 객체/원인을 서버 결과·진단에 보존; 원격 Reason에 원문·stack·증명 바이트를 넣지 않음 |
| [현행] 기한 초과·지나친 지연 | VerificationDeadlineExceeded 코드로 거부 | 늦은 성공은 미채택. 동기 hook 강제 중단은 보장하지 않음 |
| [현행] 잘못된 결과 | 빈 ID/null decision/알 수 없는 코드는 InvalidVerifierResult, 명단 밖 ID는 UnknownParticipant로 거부 | 정규화 실패 사유와 서버 원인을 보존하고 상태를 승인으로 공개하지 않음 |
| [현행] 중복 접속·이미 사용 중인 좌석 | 새 접속만 거부, 기존 살아 있는 mapping/Ready 무변경 | 새 요청의 예약만 정리하며 기존 정상 멤버를 교체하지 않음 |

[현행] 코드 이름은 AdmissionRejectionCode의 현행 이름이다. 검증기 오류·지연·잘못된 결과만으로 전체 세션을 즉시 중단하지 않는다. 승인 전 해당 접속을 거부하고 확정 명단은 유지한다. 필요한 멤버가 끝내 입장하지 못해 **전체 budget이 만료**되면 D-N5에 따라 세션을 중단하는 별도 사건이다. 이미 승인된 확정 멤버가 이후 이탈하는 기존 실패/복원 흐름도 별개다. Host 사전 검증 실패는 StartHost 요청을 거부하는 경로이며 아직 시작하지 않은 세션을 실행 중인 세션의 중단으로 보고하지 않는다.

[현행] 읽은 N3의 `ApproveConnection`과 N4의 `HostServerInitialize`는 각각 현재 호출 스택에서 callback을 직접 호출한다. 이 함수들 자체에는 Unity 메인 스레드 검사가 없다. **미확인:** 가능한 모든 transport/접속 경로의 callback이 항상 메인 스레드라는 보장과 현재 프로젝트에서의 실제 호출 스레드. 이 정적 근거만으로 확정하지 않는다.

[현행] wrapper의 명령·NGO 응답/mapping은 Unity 메인 스레드로 제한하고 검증기를 동기·비차단으로 호출한다. project hook의 비동기 완료는 주 스레드로 돌아와 owner 상태에 반영한다. 검증기는 native 조작·context 재진입을 하지 않는다. 실제 프로젝트 인증 서비스와 모든 transport 경로의 호출 맥락 보장은 이 검증으로 확대하지 않는다.

### B5. replay 책임의 경계

| 상태·대상 | 보장 책임 제안 | wrapper의 보조·보장하지 않는 것 |
|---|---|---|
| [결정됨] 증명의 진위·만료·계정/좌석 소유 (S2) | 프로젝트 검증기 | 검증 결과 ID가 서버 명단에 있는지만 추가 대조. 암호학적 진위를 대신 판정하지 않음 |
| [결정됨] 증명 nonce의 일회성 소비·재사용 방지 (S2) | 프로젝트 검증기 | 살아 있는 같은 참가자의 동시 접속을 막는 것이 nonce 소비/원자성 보장을 대신하지 않음 |
| [결정됨] Attempt/session/server 재생성 경계의 replay (S2) | 프로젝트 검증기 | wrapper가 현재 Attempt/RosterRevision과 메시지 세대를 대조해도 증명 재발급/교차 시도 소비 정책은 구현하지 않음 |
| [결정됨] 같은 Attempt의 같은 참가자 중복 접속 (S2) | wrapper | 살아 있는 승인 mapping1개와 미완료 예약 충돌을 제한. 재접속 허용 시 새 증명이 필요한지는 프로젝트 검증기 정책 |
| [제안] 늦은 승인·이전 접속의 응답 | wrapper | 실제 sender·접속 세대·owner를 대조해 상태 반영을 폐기. 증명 자체의 전역 replay 검증은 아님 |

[결정됨] S2는 프로젝트의 증명 replay 검증과 wrapper의 같은 Attempt 내 살아 있는 mapping/예약 중복 제한으로 분담한다. 근거: 사용자 결정 2026-10-16. [현행] 검증기 입출력은 P3 API이며 재발급 방식은 프로젝트 계약이다. 프로젝트는 검증 성공 후 wrapper의 중복 검사에서 거부될 때 소비된 증명의 재발급/실패 정책도 정의한다. wrapper는 증명 소비를 암묵적으로 되돌리거나 같은 증명을 재사용 가능하다고 보고하지 않는다. 원자적 검증/소비 보장은 검증기 구현이 제공한다.

### B6. Host 로컬 플레이어 경로

[현행] N4는 Host callback의 Approved=false를 자동 승인으로 처리한다. 원격 거부 경로와 같다고 가정하지 않는다.

| 상태·안 | 시작 전 검증 | 한계·추천 |
|---|---|---|
| [결정됨] 같은 프로젝트 검증기 사전 호출(S4) | StartHost 전에 같은 Session/Attempt/Roster 맥락으로 검증기를 호출하고 로컬 ID를 확인 | 성공 결과는 해당 Host 시작 시도에만 재사용해 증명을 이중 소비하지 않음. P3 AdmissionDecision을 해당 시작 시도에만 캐시. 근거: 사용자 결정 2026-10-16 |
| [제안] 비채택 대안: 별도 로컬 신뢰 경로 | 프로젝트가 인증한 로컬 참가자를 별도 callback으로 제공하고 wrapper가 명단/중복/세대를 검사 | 원격과 정책이 달라질 수 있어 신뢰 근거·실패 규칙을 따로 정의해야 함. Host라는 이유만으로 무검증 승인하는 안은 아님 |

[현행] 잘못된 Host ID·검증 실패·만료는 endpoint 시작 전에 거부한다. Host 사전 검증 성공 결과는 같은 시작 시도에서 NGO 로컬 승인 callback에 재사용하고 새 Attempt에 넘기지 않는다. P3 동일 Editor 검증 범위와 실제 프로젝트 인증·P4 미실행은 회고에서 구분한다.

### B7. 프로젝트 제공 요건

- [제안] 프로젝트 검증기 구현을 필수로 받고 미주입 시 해당 입장/시작 요청을 명시적으로 거부한다. 모든 접속을 통과시키는 운영 fallback을 제공하지 않는다.
- [제안] 증명 발급·전달 경로의 보안, 계정↔좌석 진위, 재사용 방지·만료·재발급·관측 가능성은 프로젝트 책임이다. wrapper의 NGO 승인 callback이 이를 자동 제공하지 않는다.
- [제안] server 로그/결과의 원인 보존과 원격 정규화 코드 매핑을 정의하고 proof/비밀을 원격 거부 문자열에 노출하지 않는다. 실제 원격 Reason 수신이 불가능하면 로컬 단절/원격 사유 미확인으로 구분한다.
- [제안] 로비가 없는 초기 개발용 단순 verifier 예제는 P5 후보로 둔다. 운영 인증 예제나 실제 발급 시스템으로 표시하지 않으며 이번 PR은 예제를 구현하지 않는다.
- 미확인: 프로젝트의 실제 검증기·인증/발급 서비스·별도 로비 사유 채널, 실제 증명과 envelope의 상한·보안 구성, 실제 인증 서비스와 모든 native callback 경로. P1은 로컬 loopback의 native 접속·합성 payload·handler·종료 경로를 관찰했으며 실제 인증 서비스나 wrapper 구현을 검증하지 않았다.

### B8. P0 이후 하위 결정과 남은 값

| 항목·상태 | 선택지 | 추천·확인할 것 |
|---|---|---|
| S1 [결정됨] 동기/비동기 | 짧은 동기 검증 우선 | 네트워크 I/O를 동기로 막지 않음. 비동기 지원은 실제 요구 확인 뒤 별도 결정. 근거: 사용자 결정 2026-10-16 |
| S2 [결정됨] replay 경계 | 프로젝트가 증명 replay 검증, wrapper가 같은 Attempt의 살아 있는 mapping/예약 중복 제한 | 증명 소비·만료·재발급은 프로젝트 책임. wrapper의 중복 제한은 인증/교차 시도 replay 검증을 대신하지 않음. 근거: 사용자 결정 2026-10-16 |
| S3 [결정됨] 프로젝트 필수 지정 | 전체 admission envelope 바이트 상한을 프로젝트가 필수 지정, 기본값 없음 | P1 합성 payload 관찰만으로 운영 상한을 정하지 않는다. NetworkSessionOptions.MaxAdmissionPayloadBytes에 반영. 근거: 사용자 결정 2026-10-16 |
| S4 [결정됨] Host 경로 | StartHost 전 같은 프로젝트 검증기 호출 | 성공 결과는 해당 시도에만 재사용해 이중 소비 방지. P3에서 같은 시도 캐시와 로컬 승인 경로를 구현. 근거: 사용자 결정 2026-10-16 |

[현행] 2026-10-16 사용자가 검토한 의존성 입력 반영과 P1 검증을 승인했다. manifest의 NGO2.13.3 한 항목, lock의 NGO2.13.3·UTP2.7.3 추가 및 기존 Mono Cecil1.11.6의 depth3→1만 이 단위에 포함한다. 기존 패키지 버전 변경·별도 UTP 직접 의존 추가·다른 dirty 자산 반영은 없다. P1의 temporary 하네스는 제품 wrapper·새 영속 assembly가 아니다.

## C. 패키지·배포 연결과 의존성 반영

[현행] [build_distribution.py::PACKAGES](../tools/build_distribution.py)는 `com.tplab.core/input/editor` 3개만 정의하며 `upm/`의 실제 디렉터리도 같다. UI source는 존재하지만 배포 패키지 경계는 아직 포함되지 않았다. [UI 소스 마감](retrospectives/2026-10-15-07-ui-source-close.md)은 배포를 후속으로 남겼다. 이 초안이 패키지/버전/CHANGELOG를 만들지 않는다.

- [현행] 개발 assembly는 `TPLab.Netcode`다. [제안] `com.tplab.netcode` UPM 분리는 후속 배포 후보다. 업무 모듈 의존은 Core와 NGO에 한정하고 UI/Input에 NGO 의존을 넣지 않는다. 프로젝트가 설정한 NetworkManager/NetworkTransport를 받아 UTP 설정을 공용 wrapper가 반드시 소유하도록 강제하지 않는다.
- [현행] P3 Runtime asmdef는 TPLab.Core·Unity.Netcode.Runtime·UniTask·Unity.Collections를 직접 참조한다. UniTask/Collections와 PlayMode 테스트 assembly는 사용자 별도 승인 후 추가했다. UI/Input 역참조는 없다. UnityTransport 클래스 자체는 NGO Runtime 소속이며 Unity.Networking.Transport API 직접 참조와 구분한다. **미확인:** 최종 UPM 패키지 경계·배포 의존성 표. D-N3에 따라 P5 이후 결정한다.
- [제안] 릴리스 시 package.json의 Core·NGO·필요한 UniTask/직접 UTP 버전, 제3자 고지, 문서 projection, 소비 프로젝트 manifest와 lock을 확인해야 한다. 개발 package 설치 검증이 artifact 설치 검증을 대신하지 않는다.
- [제안] D-N3/D-N6을 적용한 진행 세부: **P0 결정 반영 → 검토한 NGO 개발 의존성 입력의 별도 승인·반영 → P1 → P2 → P3 → P4 → P5 → 맨 마지막에 UI와 함께 UPM 패키지·배포**다. 순서의 방향은 D-N3로 정해졌으며 세부 gate는 아래 표의 제안이다. P1~P5를 끝내기 전 TPLab 배포 패키지 구성·버전·packager 작업은 하지 않는다. 개발용 NGO 의존성 반영과 소스 assembly 검증을 UPM 배포물 구성으로 혼동하지 않는다. `build_distribution.py`/tests/upm 변경은 마지막 릴리스 작업의 별도 범위다.
- [현행] D-N6의 개발 의존성 단위는 사용자 2026-10-16 승인에 따라 검토한 manifest/lock 항목만 반영한다. PR #35에서는 이 입력을 변경하지 않았다. 설치본의 transitive 선언·실제 해석 버전과 package 원문 고지는 P1 회고에서 구분하며, 나머지 dirty를 커밋하지 않는다.

## D. 로비 복원 실패 상태의 출구

[제안] 최초 실패·종료 실패·복원 실패를 따로 보존한다. 보호 화면 소유자는 프로젝트 callback이며 context는 표시에 필요한 단계/사유를 제공한다. native 종료를 확인하지 못했거나 프로젝트 복원이 실패하면 gameplay 입력을 다시 열지 않는다.

| 상태 | 사건·조건 | 다음 상태·허용 동작 |
|---|---|---|
| [제안] Connecting/Preparing/ReadyBarrier | 취소·실패·확정 멤버 이탈·기한 초과 | Aborting: attempt 무효화, 준비 취소, 사유 통지 시도, 세션 종료 관찰 |
| [현행] Aborting | 정리 성공·시작 승인 전 실패이며 보호/게임 gate 실패가 아님 | RestoringLobby: 같은 failure record로 프로젝트 복원 callback 1회. 정상 Running 종료는 Closed, 승인 후 실패·보호/게임 gate 실패는 자동 복원하지 않음 |
| [제안] Aborting | 종료 관찰 실패/상한 초과·미완료 hook 잔존 | RecoveryFault: 보호 화면 유지, endpoint 재시작·새 attempt 금지, 상태 관찰·진단·앱 종료만 허용 |
| [제안] RestoringLobby | 복원 callback 성공 | Lobby: 실패 사유 표시, 새 명단·새 attempt·필요 시 새 root로 시작 가능 |
| [제안] RestoringLobby | 복원 callback 실패 | RecoveryFault: 최초 실패/복원 실패 둘 다 반환·표시, 자동 성공/무한 재시도 없음 |
| [현행] RecoveryFault | 정리 성공, 미완료 restore/진행 중 retry 없음, 명시적 복원 재시도 요청 | RestoringLobby: 복원만 재시도; 옛 세션/Ready를 되살리지 않음 |
| [제안] RecoveryFault | callback이 비협조적으로 아직 실행 중 | 병렬 복원 재호출 금지; 보호 화면·프로젝트의 대체 안전 화면/앱 종료 안내 유지 |
| [제안] 모든 오류 상태 | 앱 종료/재시작 선택 | 프로세스 종료는 프로젝트/사용자 책임; wrapper가 임의 종료하지 않음 |

[현행] D-N4의 **정리 완료 후 명시적 로비 복원 재시도 + 안전한 종료 출구**를 `RetryLobbyRestoreAsync`와 `ShutdownAsync`로 제공한다. 복원 callback의 멱등 key는 같은 SessionId/AttemptId이고 부분 복원 자원은 프로젝트가 소유한다. 대안은 (1) 재시도 없이 앱 재시작만 허용: 계약은 작지만 UX 비용 큼, (2) 자동 재시도: 일시 장애에 편하나 중복 자원·무한 대기 위험. 비협조 hook의 실제 종료 전에는 재시도/안전 종료를 성공으로 보고하지 않는다. 같은 root의 Prepare 재시도 금지와 “프로젝트 복원 callback의 재호출”을 혼동하지 않는다.

## E. 대기 상한과 취소의 서로 다른 수명

- [제안] D-N5로 정한 전체 budget·확정 명단 유지의 구체화: 전체 전원 준비 deadline은 StartAsync가 요청을 수락한 시점부터 시작 승인 관찰까지의 **보호·접속·인증·로컬 준비를 포함한 한 budget**으로 둔다. 서버의 단조 증가/unscaled clock으로 판정하고 client 시계나 Time.timeScale로 결정하지 않는다. 지연된 한 명 때문에 budget을 자동 갱신하거나 명단을 줄이지 않는다. 만료하면 전체 attempt 중단·복원이다.
- [제안] 초기 실험 후보는 **60초**이며 기본값 확정이 아니다. 대상 게임/네트워크에서의 적합성은 **미확인**이고 P1/P4 측정 후 정한다. NGO의 연결 승인10초(N2), transport timeout, root shutdown의 단계별60초는 서로 다른 정책이다.
- [현행] [03 작업과 대기자의 수명](guidelines/03-async-lifecycle.md#작업과-대기자의-수명)과 [D-H6/D-H7](guidelines/03-async-lifecycle.md#확정된-수명-규칙)은 caller 대기 취소와 owner 종료, hook의 취소/해제 책임, root 단계별 deadline의 범위를 구분한다. root 정책을 NGO 종료 완료 보장으로 자동 확대하지 않는다.
- [현행] 각 public 메서드의 cancellation은 그 호출의 대기만 취소한다. ShutdownAsync는 로컬 owner 작업의 명시적 취소·정리이며 서버 승인 응답을 기다리지 않는다. 원격 사유 통지는 best effort이고 별도 P4 검증 대상이다. caller 대기 취소·owner Shutdown·실제 단절은 서로 다른 사건이다.

## F. NGO 종료 완료 관찰

[현행] N5의 `NetworkManager.Shutdown(bool)` 구현은 `IsServer || IsClient`일 때 `m_ShuttingDown=true`와 메시지 처리 flag를 설정하고 반환한다. `discardMessageQueue=true`의 주석을 호출 즉시 모든 정리 완료라는 보장으로 쓰지 않는다. 후속 `NetworkUpdate`가 shutdown을 처리하며 server/Host는 `ProcessServerShutdown`에서 원격 연결 정리 단계를 거친다. 이 설치 소스의 해당 단계에는5초 내부 대기도 있지만 wrapper의 전체 종료 SLA가 아니다.

[현행] `ShutdownInternal`은 connection/transport 정리(N8), `IsListening=false`, shutdown flag 정리·local role 초기화 뒤 역할별 `OnClientStopped(bool)`/`OnServerStopped(bool)`를 호출한다. Host는 둘 다 호출되는 경로가 있다. 이 bool은 Host 역할 정보이며 성공/실패 결과가 아니다. `ShutdownInProgress=false`만으로는 아직 시작하지 않은 객체도 참이므로 종료 성공으로 세면 안 된다. `OnStopped`는 internal이므로 wrapper의 완료 관찰 API로 쓰지 않는다.

[현행] P3 어댑터는 **요청 전에** 역할별 공개 stop 이벤트를 구독하고, 해당 start 세대의 예상 이벤트와 로컬 `!IsListening && !ShutdownInProgress && !IsServer && !IsClient`를 함께 확인한다. start 실패/이미 정지 상태는 별도 경로로 판정한다. 성공 시 자기 handler를 해제하며 중복 종료는 같은 작업 결과를 공유하도록 한다. 이벤트 중 throw·재진입 start를 하지 않는 관찰 handler를 쓴다. 외부 소유자의 같은 NetworkManager 재시작/파괴는 독점 사용 계약 위반 또는 별도 긴급 종료 경로로 보고한다.

[현행] P1은 명시적으로 생성·참조한 NGO/UTP의 정상 Host/client 종료, 같은 manager의 재시작, stop 이벤트와 역할 상태, 객체 파괴를 Editor/Windows Mono Player에서 관찰했다. 실행 조건·수치·원문은 P1 회고에 둔다. Transport가 없는 시작 실패에서는 역할 상태가 남고 정상 shutdown 관찰을 완료하지 못했으므로, 단순 role flag나 start 반환값만으로 모든 비정상 정리를 보장하지 않는다. [현행] P3 어댑터는 native 시작 전에 필수 설정을 검증하고, 정리 관찰 실패를 오류 상태로 보존한다. 이 제한은 NGO 수정이나 모든 native 실패의 복구 보장을 승인하지 않는다. P1은 로컬 NGO 종료 관찰이며 모든 원격 프로세스의 종료·로비 복원 완료가 아니다. 원격 통지/복원은 P4에서 별도로 검증한다.

## P0–P5 진행 단계와 gate

| 상태·phase | 목적·예정 범위 | 완료 조건·미실행 경계 |
|---|---|---|
| [현행] P0 계약 기록 | D-N1~D-N8 반영·검증기 주입 세부 후보 정리 | 당시 방향·S1/S2/S4 결정 반영이며 그 자체는 구현 승인이 아님. 이후 별도 P3 승인으로 public API·S3 필수 입력을 구현 |
| [현행] P1 기반 확인(사용자 실행 승인 2026-10-16) | 승인 의존성·컴파일, 명시 참조 Host/client·승인·Named message·종료·합성 payload·종전 false 후보·동일 프로세스 다중 instance 관찰 | Editor/Windows Mono Player 기본 경로 확인과 무효 Transport 시작 제한을 회고에 기록. false 관찰은 당시 빈 고정 씬 조건의 증거로 보존한다. 당시 미확인이던 별도 프로세스 native 관찰은 다음 행에서 보완한다. 실제 인증·IL2CPP·wrapper 구현과 S3 값은 여전히 미확인 |
| [현행] P1 true 추가 검증(2026-10-16) | 별도 Windows Mono Player Host/client의 초기 Single/Additive 동기화·in-scene NetworkObject, Additive 로드/해제·Single 전환·client 권한 거부·정상 종료 | native 관찰 완료. 실행 수·객체 ID 대조·정리·한계는 [추가 회고](retrospectives/2026-10-16-01-netcode-p1-verification.md#2026-10-16-후속--true-권장과-native-씬-검증)에 보존한다. GameSceneManager 통합·wrapper Ready/복원·P4 실패 시나리오 검증은 아님 |
| [현행] P2 프로토콜 | internal 순수 server/client 전이, 명단·attempt·Ready·단일 start/abort·deadline/최초 실패 | NGO 객체 없는 EditMode Red→Green; [P2 계약](#p2-내부-프로토콜-구현)과 회고의 검증/미실행 참조. wire encoding·실제 adapter/복원은 P3 |
| [현행] P3 어댑터·callback | NetworkSessionContext/SessionStartCoordinator, NGO sender mapping·준비/복원 callback·owner 수명·stop 관찰 | [P3 계약](#p3-ngo-어댑터와-callback-구현)·[회고](retrospectives/2026-10-16-03-netcode-p3-adapter.md)의 실행 범위 참조. 같은 Editor 관찰과 P4/P5 미실행을 구분 |
| [제안] P4 다중 peer | 별도 프로세스 Host+clients, 전원 준비, 위조·중복·중도 이탈·느린 peer·server 손실·복원 실패 | 각 프로세스 결과와 서버 승인 횟수/세대/실패 전달을 대조. 단일 프로세스 통과로 대체하지 않음 |
| [제안] P5 예제·수락 | 고정 씬·프로젝트 로비/보호 화면 callback 예제, 사용자 검토, 사람/AI API·XML 확정 | 예제와 사용자 수락. 공유 씬 전환/Relay·인증 provider/팀 매칭은 별도 후속; UPM 패키지·배포는 P1~P5 뒤 UI와 함께 맨 마지막(D-N3) |

[현행] 테스트 실행·동결·fixture/SetUp/TearDown 실패 구분은 [05 테스트](guidelines/05-testing.md#특성화-안전망과-fixture-정리의-현행-교훈), API 변경/절차는 [08 위험 등급](guidelines/08-change-procedure.md#위험-등급과-공통-항목)을 따른다. PR #34의 P0 문서 작성 당시에는 P1–P5 테스트를 실행하지 않았으며, 이후 P1 실행 결과는 위 표와 P1 회고에 별도로 기록한다.

## 미확인 항목과 사용자 결정

미확인 목록: (U1) P3 wrapper Player/IL2CPP·다른 플랫폼, (U2) P4 별도 프로세스 wrapper 실패/위조/이탈·복원 전파, (U3) 프로젝트의 실제 검증기·발급/인증 기반·별도 사유 채널, (U4) 프로젝트별 실제 envelope 상한과 보안 구성(S3 필수 입력), (U5) 최종 UPM 경계·의존성·소비 설치, (U6) 실제 프로젝트 복원 callback의 멱등성/부분 실패, (U7) 프로젝트 준비/종료/복원 budget 값, (U8) 모든 비정상 native 시작/종료의 복구 범위, (U9) 실제 프로젝트 초기 Single/Additive 선택과 GameSceneManager inventory/소유권 통합이다. P1 native Player 관찰과 P3 동일 Editor 개발 검증은 다른 증거다. public API와 SessionId/AttemptId 복원 key는 P3에 구현됐지만 프로젝트 동작까지 검증한 것은 아니다.

### 사용자 결정 D-N1~D-N8

| ID·상태 | 반영한 결정 | 단서·근거 |
|---|---|---|
| D-N1 [결정됨] 상태기계 분리 | A1 = 내부 순수 상태기계 + 얇은 NGO 어댑터 | NGO 미설치 컴파일을 위한 별도 assembly는 이번 결정 밖이며 별도 승인. 근거: 사용자 결정 2026-10-15 |
| D-N2 [결정됨] 입장 검증 | 프로젝트가 검증기를 주입한다. wrapper는 서명/일회용 ticket 등 증명 형식을 정하지 않는다. | B절의 입출력은 P3 개발 API로 구체화했고 S1/S2/S4 방향은 2026-10-16 별도 결정. 근거: 사용자 결정 2026-10-15 |
| D-N3 [결정됨] 개발·배포 순서 | Netcode wrapper P1~P5 개발을 먼저 마치고 UPM 패키지·배포는 맨 마지막에 UI와 함께 진행 | 그 전에 TPLab 배포 패키지 구성·버전·packager 작업을 하지 않음. 근거: 사용자 결정 2026-10-15 |
| D-N4 [결정됨] 복원 출구 | 정리 완료 후 명시적 복원 재시도 + 안전한 종료 출구 | P3 개발 API는 RetryLobbyRestoreAsync, 복원 멱등 key는 SessionId/AttemptId다. 근거: 사용자 결정 2026-10-15 |
| D-N5 [결정됨] deadline 방향 | 전체 budget + 확정 명단 유지; 개별 연장·일부 제외 안 함 | 60초는 실험 후보이며 종료·복원 budget 값도 미확정. 근거: 사용자 결정 2026-10-15 |
| D-N6 [결정됨] 의존성 반영 | 검토한 package 입력만 별도 승인 단위로 반영; dirty 전체 커밋 안 함 | PR #35에서 manifest/lock 변경0; 2026-10-16에 검토한 NGO/Transport 의존성과 P1 검증 승인. 근거: 사용자 결정 2026-10-15 |
| D-N7 [결정됨] 준비 메시지 방향 | Named message 후보로 진행 | P3에서 실제 sender 검증·등록/해지와 public wrapper를 구현. 다중 프로세스 실패 전파는 P4. 근거: 사용자 결정 2026-10-15 |
| D-N8 [결정됨] 초기 동기화 정책·검증 방향 | `EnableSceneManagement=true` 기본 권장, `false`는 프로젝트 선택과 씬 동기화 책임 아래 허용; true P1 추가 검증 | 2026-10-15의 false 후보 검증 방향을 2026-10-16 사용자 결정으로 갱신. 종전 false 관찰은 역사적 조건으로 보존한다. GameSceneManager 역할 분리·1차 미확장, 같은 씬 이중 소유 금지; inventory/소유권 이관과 공유 씬 통합은 후속. native true의 별도 Windows Mono Player 관찰 완료; 실제 프로젝트·wrapper 통합은 후속 |

[현행] [S1~S4](#b8-p0-이후-하위-결정과-남은-값)를 반영한 P3 개발 API가 구현됐다. S3는 프로젝트 필수 입력/기본값 없음이며 준비·정리·복원 시간 값도 프로젝트가 명시한다. P4/P5와 마지막 UPM 배포는 후속이다. 단계별 실행 결과·중단/정정 이력·미실행은 각 P1/P2/P3 회고에 둔다.
