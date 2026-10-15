# Netcode 세션 wrapper P0 계약 초안
유형: 레퍼런스
현재 상태: P0 계약 초안, 구현 없음
기준 source: TPLab main 6b1e638ef65c073e292b68d83a222ad31db37d09; NGO 2.13.3 / Unity Transport 2.7.3 설치 소스(사용자 dirty)
갱신일: 2026-10-15
계약 링크: 본 문서

## 0. 사실 확인과 근거 범위

[현행] 이 문서는 채팅에서 합의한 1차 범위를 저장소에 처음 기록한다. 파일·심볼을 읽은 정적 사실과 신규 설계 제안을 구분한다. **P0 계약 초안, 구현 없음**이며 문서 작성이나 PR 승인만으로 아래 미선택 설계가 사용자 결정으로 바뀌지 않는다. 아래의 [결정됨]은 기존 사용자 결정만 뜻한다. 이번에는 설치·컴파일·Unity 테스트·Player·다중 프로세스를 실행하지 않는다.

| 상태 | 확인 대상 | 읽어서 확인한 사실·한계 |
|---|---|---|
| [현행] | main의 `Packages/manifest.json` | `com.unity.multiplayer.center: 1.0.1`은 있지만 NGO/UTP 항목은 없다. 기준은 위 main SHA의 Git blob이다. |
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
| [제안] `NetworkSessionContext` | 명시적으로 주입된 NetworkManager의 세션 사용권, 자신의 구독·프로토콜 handler·연결/종료 작업·연결 mapping | 전역 Singleton 탐색, 프로젝트가 만든 GameObject의 암묵적 파괴, 로비 인증 구현 |
| [제안] `SessionStartCoordinator` | 확정 명단·시도 세대·참가자 상태, 전원 준비 barrier, 서버 승인/중단의 직렬화와 실패 결과 | 씬을 직접 로드/해제, 플레이어별 비즈니스 준비 구현 |
| [제안] 프로젝트 callback | 로컬 준비·보호 화면·게임play gate·실패 표시·로비 복원과 필요한 resource/input 수명 관리 | 공용 코드의 UI/로비 서비스 직접 의존 |
| [제안] 프로젝트 로비 | 인증된 명단과 입장 증명 발급/검증, 세션 이전 복원 정보, 복원 후 사용자 동작 | NGO ClientId를 영속 계정 ID처럼 저장 |

[제안] NetworkManager의 start/stop·승인 callback·프로토콜 handler를 이 context가 독점 사용하도록 사전에 합의한다. 주입 객체의 수명 소유자는 프로젝트이며 context는 자기 구독과 세션 종료를 책임진다. 다른 시스템과 callback을 무조건 +=/교체하지 않는다(N1). 수명/명시적 주입의 기존 예는 [Input 계약](INPUT_SYSTEM_DRAFT.md#책임과-최소-구성), [Input assembly](../Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef), [SceneRoot 계약](SCENE_ROOT.md)이다.

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
- [결정됨] [GameSceneManager](GAME_SCENE_MANAGER_DRAFT.md)는 1차에서 확장하지 않는다. 공유 씬 로드·활성화·승인·unload의 네트워크 합의는 후속이다. 프로젝트 복원 callback이 기존 로컬 전환 API를 사용할 수 있지만 두 manager가 같은 native 씬을 동시에 소유·전환하지 않는다. 근거: 사용자 1차 범위 지정.

## A. 테스트 구조와 NGO 어댑터 경계

[현행] 기존 Input은 native Input System을 별도 [TPLab.Core.Input](../Assets/TPLab/Input/Runtime/TPLab.Core.Input.asmdef)으로 분리한다. UI 연결도 선택 [TPLab.UI.InputSystem](../Assets/TPLab/UI/InputSystem/Runtime/TPLab.UI.InputSystem.asmdef)/[UIInputSystemAdapter.Bind](../Assets/TPLab/UI/InputSystem/Runtime/UIInputSystemAdapter.cs)와 `UIContext.Input.cs::AttachInputBridge`의 좁은 delegate 접점으로 연결한다. Input 전체가 순수 상태기계라는 뜻은 아니다.

| 상태·안 | 장점 | 비용·검증의 한계 |
|---|---|---|
| [제안] A1 순수 상태기계 + 얇은 NGO 어댑터 | 명단·attempt·Ready·중복·실패 전이를 NGO 객체 없이 EditMode에서 검사. NGO handler는 sender/사건을 상태 입력으로 번역하고 실행 효과를 전달 | 상태기계 입력/효과의 최소 경계가 필요. 같은 assembly 안 internal 클래스로 시작하면 컴파일 시 NGO 참조는 여전히 필요하다. NGO 미설치 상태에서도 컴파일되는 별도 순수 assembly는 추가 승인 대상이며 이번 추천 범위가 아니다. |
| [제안] A0 분리하지 않음 | 처음의 클래스/전달 코드 수가 적다 | 프로토콜 조합마다 NetworkManager/PlayerLoop fixture가 필요해 통합 테스트 비용·전역 상태 의존이 커진다. 순수 규칙만 따로 검사하기 어렵다. |

[제안] **A1 추천**: 명단·시도·준비 전이 한 단위만 internal 순수 로직으로 두고 어댑터는 start/승인/메시지/종료 관찰에 한정한다. 일반 네트워크 framework, 모든 메서드의 interface, NGO package 내부 helper 복사·reflection·private API 의존은 만들지 않는다. package 테스트 helper를 TPLab의 public 지원 API로 간주하지 않는다.

[현행] N6는 단일 프로세스에서 여러 NetworkManager를 생성·시작하는 소스 근거다. `NetworkManager.OnEnable`은 Singleton이 없을 때만 설정하며 인스턴스별 참조도 존재한다. 따라서 “NGO는 무조건 한 프로세스 한 instance”는 이 소스로 뒷받침되지 않는다. **미확인:** 현재 Editor·어셈블리에서의 multi-instance fixture 격리·실제 네트워크 동작. P1에서 확인하고 실패하면 임의 package helper/새 패키지를 가져오지 않는다. 그 경우 실제 승인/transport/종료의 peer 간 검증은 P4 다중 프로세스로 넘기고, P2 EditMode 프로토콜 검증을 그 대체물로 보고하지 않는다. 단일 프로세스가 가능해도 프로세스 종료·실제 단절·개별 로그/복원은 P4에서 별도 확인한다.

## B. 입장 검증 접점과 신뢰 경계

[현행] N1–N3의 `NetworkConfig.ConnectionApproval=true` + `ConnectionData` + `ConnectionApprovalCallback`이 입장 검증 접점이다. callback의 `ClientNetworkId`는 수신 context의 SenderId에서 오지만 Payload는 client가 보낸 바이트다. config hash 비교나 transport가 부여한 ClientId 자체는 로비 계정 소유권 증명이 아니다. `ConnectionApprovalCallback`을 등록만 하고 ConnectionApproval을 켜지 않으면 승인 기능이 활성화되지 않는다(N2/NetworkManager.CanStart).

[제안] 프로젝트가 신뢰할 수 있는 로비/인증 경로에서 Session/Attempt/RosterRevision·참가자·만료·재사용 방지에 결합한 입장 증명을 검증하고, coordinator는 검증 결과와 실제 NGO sender를 일대일로 연결한다. 단순 LobbyParticipantId 문자열을 맞췄다는 이유만으로 승인하지 않는다. 서명 ticket 또는 서버 보관 일회용 opaque ticket 중 선택은 아래 사용자 결정2다. 별도 인증 서비스를 공용 코드에 내장하지 않는다.

| 상태·입력/시점 | 처리 제안 | 기존 세션에 미치는 영향 |
|---|---|---|
| [제안] malformed·과대 payload | 복사·문자열 해석 전에 wrapper byte 길이/포맷/버전 상한 검사, 정규화된 거부 사유 | 해당 접속만 거부; 참가자 mapping·Ready 무변경 |
| [제안] 명단 밖·위조·만료·이전 Attempt | 검증 실패/틀린 세대를 거부하고 Ready/승인 시작 조건에서 배제 | 공격자의 요청만으로 기존 확정 명단을 축소하거나 전체 시도를 중단하지 않음 |
| [제안] 중복 참가자·사용한 ticket | 살아 있는 승인 mapping/보류 reservation과 충돌하면 새 접속 거부 | 기존 정상 접속을 바꾸지 않음; 실패한 새 접속의 예약만 회수 |
| [제안] 비동기 프로젝트 인증 | remote만 Pending을 쓸 수 있는 후보. 완료 시 Attempt·sender 접속·소유 수명을 재검사하고 응답 필드를 설정한 뒤 Pending=false | 옛 시도/끊어진 접속의 늦은 인증 결과를 채택하지 않음. 동기 검증 우선 추천 |
| [제안] Host 로컬 플레이어 | StartHost 전에 신뢰된 로컬 참가자·명단·proof를 검증하고 서버 mapping에 예약 | N4상 Approved=false로 Host를 거부할 수 없음. 잘못된 Host면 start 자체 거부 또는 시작된 전체 endpoint 정리; Host를 누락한 채 게임 시작 금지 |
| [제안] Ready·Start 메시지 | N7의 transport/RPC sender로 인증 mapping 조회; 본문이 주장한 participant/ClientId만으로 선택하지 않음 | 중복·이전 세대·위조 응답은 준비 인원에 반영하지 않음 |

[현행] N1은 `Approved=false`와 문자열 `Reason`을 제공하고 N3는 reason 메시지를 보낸 뒤 disconnect를 예약한다. 프로젝트용 typed 오류 enum은 이 API에서 제공하지 않는다. **[제안]** wrapper는 `InvalidProof`, `NotInRoster`, `DuplicateParticipant`, `StaleAttempt`, `AdmissionClosed`, `ProtocolMismatch`, `PayloadRejected` 같은 코드와 단계·관측 여부를 가진 결과를 별도로 정의한다(이름/시그니처 미확정). Reason에는 제한된 코드만 넣고 ticket·비밀·서버 예외 원문을 넣지 않는다. 이유를 수신하지 못하면 `Disconnected/UnknownRemoteReason`을 쓰고 성공으로 처리하지 않는다. 거부된 접속은 Ready 멤버가 아니다.

[제안] payload 상한 후보는 **1KiB**(NGO의 보장값이 아님)이며 실제 proof 길이·인코딩·전송 방식을 P1에서 확인한 후 정한다. **미확인:** NGO/UTP 조합의 유효 최대 접속 payload, 이 프로젝트의 암호화·인증 채널 구성과 재전송/거부 사유 수신 보장. N3는 callback 이전에 byte[]를 역직렬화하므로 wrapper 상한만으로 NGO 내부 선행 할당/모든 transport 입력을 제한한다고 주장하지 않는다. 장기 비밀을 평문 payload에 넣는 설계는 채택하지 않으며 secure transport 여부를 승인 callback의 존재로 추정하지 않는다.

## C. 패키지·배포 연결과 의존성 반영

[현행] [build_distribution.py::PACKAGES](../tools/build_distribution.py)는 `com.tplab.core/input/editor` 3개만 정의하며 `upm/`의 실제 디렉터리도 같다. UI source는 존재하지만 배포 패키지 경계는 아직 포함되지 않았다. [UI 소스 마감](retrospectives/2026-10-15-07-ui-source-close.md)은 배포를 후속으로 남겼다. 이 초안이 패키지/버전/CHANGELOG를 만들지 않는다.

- [제안] `com.tplab.netcode`와 `TPLab.Netcode`는 선택 모듈 후보다. 업무 모듈 의존은 Core와 NGO에 한정하고 UI/Input에 NGO 의존을 넣지 않는다. 프로젝트가 설정한 NetworkManager/NetworkTransport를 받아 UTP 설정을 공용 wrapper가 반드시 소유하도록 강제하지 않는다.
- [제안] 기존 공용 async 규칙대로 UniTask를 공개/내부 async에서 직접 쓰면 `UniTask` assembly 참조도 명시적으로 필요하다. “Core와 NGO만”은 업무 모듈 경계이며 실제 asmdef 항목이 딱2개라는 확정이 아니다. **미확인:** 정확한 최종 참조/패키지 의존 표. P1 직전 승인·컴파일 검증에 올린다. UnityTransport 타입을 직접 사용하게 되면 UTP 참조가 추가로 필요하므로 프로젝트 설정 경계 유지와 비교해 다시 승인받는다.
- [제안] 릴리스 시 package.json의 Core·NGO·필요한 UniTask/직접 UTP 버전, 제3자 고지, 문서 projection, 소비 프로젝트 manifest와 lock을 확인해야 한다. 개발 package 설치 검증이 artifact 설치 검증을 대신하지 않는다.
- [제안] 순서는 **P0 결정 → 의존성 반영만 별도 승인·커밋 → P1 컴파일/종료 검증 → P2/P3 source → P4 peer 검증 → P5 예제·사용자 수락 → 후속 배포 구조 결정/packager 대상 추가·재생성·설치 검증**이다. UI 배포 구조 결정과 Netcode 배포 추가를 같은 릴리스 계획에서 조정할 수 있으나 UI 패키징 완료를 순수 Netcode source 개발의 숨은 선행 조건으로 만들지 않는다. `build_distribution.py`/tests/upm 변경은 그 릴리스 작업의 별도 범위다.
- [제안] 사용자 dirty manifest·lock의 통째 커밋은 추천하지 않는다. 설치된 전체 transitive diff를 검토한 뒤 승인된 package 입력만 별도 단위로 반영하는 안을 추천한다. 이 문서 PR에서 dirty를 복사·되돌리기·stage하지 않는다.

## D. 로비 복원 실패 상태의 출구

[제안] 최초 실패·종료 실패·복원 실패를 따로 보존한다. 보호 화면 소유자는 프로젝트 callback이며 context는 표시에 필요한 단계/사유를 제공한다. native 종료를 확인하지 못했거나 프로젝트 복원이 실패하면 gameplay 입력을 다시 열지 않는다.

| 상태 | 사건·조건 | 다음 상태·허용 동작 |
|---|---|---|
| [제안] Connecting/Preparing/ReadyBarrier | 취소·실패·확정 멤버 이탈·기한 초과 | Aborting: attempt 무효화, 준비 취소, 사유 통지 시도, 세션 종료 관찰 |
| [제안] Aborting | 소유 정리·NGO 로컬 종료 완료 | RestoringLobby: 같은 failure record로 프로젝트 복원 callback 1회 |
| [제안] Aborting | 종료 관찰 실패/상한 초과·미완료 hook 잔존 | RecoveryFault: 보호 화면 유지, endpoint 재시작·새 attempt 금지, 상태 관찰·진단·앱 종료만 허용 |
| [제안] RestoringLobby | 복원 callback 성공 | Lobby: 실패 사유 표시, 새 명단·새 attempt·필요 시 새 root로 시작 가능 |
| [제안] RestoringLobby | 복원 callback 실패 | RecoveryFault: 최초 실패/복원 실패 둘 다 반환·표시, 자동 성공/무한 재시도 없음 |
| [제안] RecoveryFault | native 종료·이전 hook 종료가 확인되고 명시적 복원 재시도 요청 | RestoringLobby: 복원만 재시도; 옛 세션/Ready를 되살리지 않음 |
| [제안] RecoveryFault | callback이 비협조적으로 아직 실행 중 | 병렬 복원 재호출 금지; 보호 화면·프로젝트의 대체 안전 화면/앱 종료 안내 유지 |
| [제안] 모든 오류 상태 | 앱 종료/재시작 선택 | 프로세스 종료는 프로젝트/사용자 책임; wrapper가 임의 종료하지 않음 |

[제안] **명시적 로비 복원 재시도 + 안전한 종료 출구**를 추천한다. 후보 API 이름 `RetryLobbyRestoreAsync`는 미확정이며 복원 callback의 멱등 key·취소·부분 복원 책임도 P0 선택 대상이다. 대안은 (1) 재시도 없이 앱 재시작만 허용: 계약은 작지만 UX 비용 큼, (2) 자동 재시도: 일시 장애에 편하나 중복 자원·무한 대기 위험. 재시도가 구현되어 있다고 표현하지 않는다. 같은 root의 Prepare 재시도 금지와 “프로젝트 복원 callback의 재호출”을 혼동하지 않는다.

## E. 대기 상한과 취소의 서로 다른 수명

- [제안] 전체 전원 준비 deadline은 서버가 RosterFrozen을 수락한 시점부터 StartAuthorized 직전까지의 **접속·인증·로컬 준비를 포함한 한 budget**으로 둔다. 서버의 단조 증가/unscaled clock으로 판정하고 client 시계나 Time.timeScale로 결정하지 않는다. 지연된 한 명 때문에 budget을 자동 갱신하거나 명단을 줄이지 않는다. 만료하면 전체 attempt 중단·복원이다.
- [제안] 초기 실험 후보는 **60초**이며 기본값 확정이 아니다. 대상 게임/네트워크에서의 적합성은 **미확인**이고 P1/P4 측정 후 정한다. NGO의 연결 승인10초(N2), transport timeout, root shutdown의 단계별60초는 서로 다른 정책이다.
- [현행] [03 작업과 대기자의 수명](guidelines/03-async-lifecycle.md#작업과-대기자의-수명)과 [D-H6/D-H7](guidelines/03-async-lifecycle.md#확정된-수명-규칙)은 caller 대기 취소와 owner 종료, hook의 취소/해제 책임, root 단계별 deadline의 범위를 구분한다. root 정책을 NGO 종료 완료 보장으로 자동 확대하지 않는다.
- [제안] 관측 caller의 cancellation은 그 호출의 대기만 취소한다. 소유자의 중단 요청은 별도 명시적 동작으로 서버가 수락하고 attempt를 끝낸다. client가 로컬 대기를 취소했다는 이유만으로 전역 세션을 몰래 종료하지 않는다. owner Shutdown/실제 이탈은 별도 세션 사건이다.
- [제안] 종료/복원 관찰에도 별도 유한 budget을 두되 구체 값은 미확정이다. Timeout은 대기 실패와 보호 상태 진입을 뜻하고 NGO native/hook/메인 스레드 blocking의 강제 중단을 뜻하지 않는다. 늦은 결과는 attempt/owner 세대가 맞는지 확인해 상태·gameplay를 되살리지 않는다. hook 작성자가 자신이 시작한 작업을 취소·release하고 종료된 서비스에 접근하지 않게 관리한다.
- [현행] [05 Timeout 방침](guidelines/05-testing.md#양성-대조-범위와-미확정-사항)의 D-I2/IMP-04b는 테스트 timeout 적용 범위/기본값 실험이다. 세션의 runtime deadline과 별개이며 실험 전 hang 중단 보장을 쓰지 않는다. P2/P3 테스트는 gate·유한 조건 대기·양성 경로로 상태를 검증하고 timeout 자체를 성공 오라클로 삼지 않는다.

## F. NGO 종료 완료 관찰

[현행] N5의 `NetworkManager.Shutdown(bool)` 구현은 `IsServer || IsClient`일 때 `m_ShuttingDown=true`와 메시지 처리 flag를 설정하고 반환한다. `discardMessageQueue=true`의 주석을 호출 즉시 모든 정리 완료라는 보장으로 쓰지 않는다. 후속 `NetworkUpdate`가 shutdown을 처리하며 server/Host는 `ProcessServerShutdown`에서 원격 연결 정리 단계를 거친다. 이 설치 소스의 해당 단계에는5초 내부 대기도 있지만 wrapper의 전체 종료 SLA가 아니다.

[현행] `ShutdownInternal`은 connection/transport 정리(N8), `IsListening=false`, shutdown flag 정리·local role 초기화 뒤 역할별 `OnClientStopped(bool)`/`OnServerStopped(bool)`를 호출한다. Host는 둘 다 호출되는 경로가 있다. 이 bool은 Host 역할 정보이며 성공/실패 결과가 아니다. `ShutdownInProgress=false`만으로는 아직 시작하지 않은 객체도 참이므로 종료 성공으로 세면 안 된다. `OnStopped`는 internal이므로 wrapper의 완료 관찰 API로 쓰지 않는다.

[제안] P1 어댑터는 **요청 전에** 역할별 공개 stop 이벤트를 구독하고, 해당 start 세대의 예상 이벤트와 로컬 `!IsListening && !ShutdownInProgress && !IsServer && !IsClient`를 함께 확인한다. start 실패/이미 정지 상태는 별도 경로로 판정한다. 성공 시 자기 handler를 해제하며 중복 종료는 같은 작업 결과를 공유하도록 한다. 이벤트 중 throw·재진입 start를 하지 않는 관찰 handler를 쓴다. 외부 소유자의 같은 NetworkManager 재시작/파괴는 독점 사용 계약 위반 또는 별도 긴급 종료 경로로 보고한다.

미확인: Unity6000.3.18f1 / Windows Mono에서 이벤트 순서·실제 필요 프레임·Host/Client/start 실패/연속 start-stop/파괴의 관찰과 누락 없는 구독 해제. P1에서 실제로 검증하기 전에는 위 조건이 모든 비정상 종료까지 포괄한다고 단정하지 않는다. 이 관찰은 로컬 NGO 종료이며 모든 원격 프로세스가 종료·로비 복원을 완료했다는 확인이 아니다. 원격 통지/복원 완료 보장은 P4의 별도 프로토콜·통신 가용성 범위다.

## P0–P5 진행 단계와 gate

| 상태·phase | 목적·예정 범위 | 완료 조건·미실행 경계 |
|---|---|---|
| [제안] P0 계약 | 이 초안·책임·실패/복원·identity·미확인/선택지 정리 | 사용자 결정 항목 선택 후 계약 확정. 현재는 문서 작성만이며 구현 승인 아님 |
| [제안] P1 NGO 기반 확인 | 승인된 의존성 반영 후 컴파일, 명시 주입·Host/client 시작·승인 callback·실제 종료 완료 관찰, payload/고정 씬 설정·다중 instance feasibility | Unity6000.3.18f1 / Windows Mono compile/Play/필요 Player 확인. 미확인 API를 실제 확인 또는 제한으로 고정 |
| [제안] P2 프로토콜 | 선택된 최소 상태기계, 명단·attempt·Ready·start/abort 경합·deadline/실패 결과 | NGO 객체 없는 EditMode Red→Green·민감도 확인, 잘못된 ID/중복/옛 세대/지연 응답을 검사 |
| [제안] P3 어댑터·callback | NetworkSessionContext/SessionStartCoordinator, NGO sender mapping·준비/복원 callback·owner 수명·stop 관찰 | 설치 소스와 실제 NGO 경로 targeted 테스트, 전체 회귀·정리 완료/로그 관찰, 정상 코드 실패 시 중단/원인 분리 |
| [제안] P4 다중 peer | 별도 프로세스 Host+clients, 전원 준비, 위조·중복·중도 이탈·느린 peer·server 손실·복원 실패 | 각 프로세스 결과와 서버 승인 횟수/세대/실패 전달을 대조. 단일 프로세스 통과로 대체하지 않음 |
| [제안] P5 예제·수락 | 고정 씬·프로젝트 로비/보호 화면 callback 예제, 사용자 검토, 사람/AI API·XML 확정 | 예제와 사용자 수락. 공유 씬 전환/Relay·인증 provider/팀 매칭/UPM 발행은 별도 후속 |

[현행] 테스트 실행·동결·fixture/SetUp/TearDown 실패 구분은 [05 테스트](guidelines/05-testing.md#특성화-안전망과-fixture-정리의-현행-교훈), API 변경/절차는 [08 위험 등급](guidelines/08-change-procedure.md#위험-등급과-공통-항목)을 따른다. 이 T0 문서 PR에서 P1–P5 테스트를 실행한 것으로 기록하지 않는다.

## 미확인 항목과 사용자 결정

미확인 목록: (U1) 설치 조합의 컴파일·Windows Mono/Player/IL2CPP 지원, (U2) TPLab 단일 프로세스 다중 manager fixture, (U3) 프로젝트 로비 인증/별도 사유 전달 채널, (U4) 실제 접속 payload/거부 사유 전달 상한·보안 구성, (U5) 최종 asmdef/package 의존성, (U6) 복원 callback의 멱등/부분 실패/완료 관찰, (U7) 전체 deadline/종료·복원 budget 적합성, (U8) 역할별 종료 이벤트·프레임·파괴 경로, (U9) 고정 씬의 NGO 초기 동기화 설정이다. 읽기로 확인한 API 존재와 이 미실행 동작을 구분한다.

| 번호·상태 | 선택지 | 추천·선행 확인 |
|---|---|---|
| 1 [제안] A 분리 | A1 내부 순수 상태기계+얇은 NGO 어댑터 / A0 통합 클래스 | A1. 같은 assembly의 좁은 경계부터; NGO 미설치 컴파일까지 위한 추가 assembly는 별도 승인 |
| 2 [제안] B 입장 검증 | 서버 보관 일회용 opaque ticket / 서명 ticket+프로젝트 verifier | 기존 로비가 발급·검증 가능한 쪽 우선. 그런 기반이 없으면 서버 보관 일회용 proof의 작은 callback부터; 구현 전 신뢰된 발급/전달 경로 확인. 자기 주장 ID만으로 승인은 선택지 아님 |
| 3 [제안] C 패키지 시점 | source 단계 선택 assembly 후 배포는 별도 / P1부터 UPM+packager까지 | source 경계 먼저, 배포 구조는 UI 후속과 함께 별도 결정. UniTask 직접 참조·UTP 직접 사용 여부도 P1 전 확인 |
| 4 [제안] D 복원 출구 | 명시적 복원만 재시도+안전한 종료 / 앱 재시작만 / 자동 재시도 | 명시적 복원 재시도. 이전 native/hook 정리가 확인돼야 허용하며 API/멱등 계약을 먼저 확정 |
| 5 [제안] E deadline | 전체 budget+전원 중단 / 멤버별 연장·일부 제외 | 전체 budget+명단 유지. 60초는 미검증 실험 후보, 구체 값·상한 초과 시 결과·cleanup budget은 P1/P4 근거 후 확정 |
| 6 [제안] 의존성 저장소 반영 | 검토한 package 입력만 별도 승인 단위 / 현재 dirty 전체 반영 | 전자. 현재 dirty는 모두 보존하며 package 관련 diff만 검토한 별도 승인 없이는 커밋하지 않음 |
| 7 [제안] 준비 메시지 경로 | NGO CustomMessagingManager / 프로젝트 NetworkBehaviour RPC | 작은 Ready/Start/Abort는 N7의 named message 후보 추천. 실제 sender 검증·등록/해지·호환성은 P1/P3 확인; public API 미확정 |
| 8 [제안] 고정 씬 초기 동기화 | EnableSceneManagement=false+명시적인 고정 씬/prefab 구성 / true의 초기 자동 scene 동기화 허용 | 첫 안을 P1 후보로 검증. N2는 false일 때 scene object soft sync 제약을 명시하므로 프로젝트의 NetworkObject 구성을 먼저 확인. true를 선택하면 연결 중 자동 씬 보정을 로컬 GameSceneManager와 어떻게 분리할지 추가 합의. 공유 씬 전환 지원을 묵시적으로 추가하지 않음 |

[제안] 위 선택과 미확인이 해소되기 전 구현 API 이름/시그니처·패키지 설치·GameSceneManager 확장은 확정하지 않는다. 결정 결과는 이 계약 문서에 반영하고 단계별 실행 증거는 작업 회고 한 곳에서 관리한다.
