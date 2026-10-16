# Netcode 세션 모듈
유형: 레퍼런스
현재 상태: P3 public 세션 API·NGO 어댑터 구현; 검증·수락 범위는 회고 참조
기준 source: ea8123c0079cb1ddeb05ff5e8d526f6cf6588d98
갱신일: 2026-10-16
계약 링크: [Netcode 세션 계약](../NETCODE_SESSION_DRAFT.md)

`NetworkSessionContext`는 프로젝트가 구성한 `NetworkManager`를 빌려 고정 명단의 입장·로컬 준비·전원 Ready·서버 시작 승인을 연결한다. 프로젝트 callback과 정리·복원 수명도 context가 관리한다. Host와 원격 client를 시작하며 전용 서버 시작 API와 분산 권한 모드는 현재 범위 밖이다.

| 구분 | 책임·근거 |
|---|---|
| Namespace / assembly | `TPLab.Netcode` / `TPLab.Netcode`; 직접 참조는 TPLab.Core·Unity.Netcode.Runtime·UniTask·Unity.Collections |
| Public 계약 | [NetworkSessionContracts.cs](../../Assets/TPLab/Netcode/Runtime/NetworkSessionContracts.cs), [NetworkSessionContext.cs](../../Assets/TPLab/Netcode/Runtime/NetworkSessionContext.cs) |
| 내부 어댑터 | [SessionStartCoordinator.cs](../../Assets/TPLab/Netcode/Runtime/SessionStartCoordinator.cs); 소비자가 직접 호출하지 않음 |
| 프로젝트 소유 | manager/GameObject·transport·주소·prefab·인증/증명·실제 씬/root·UI/input·gameplay 동기화 |
| 검증 상태 | Partial. 환경·실행 범위·미실행은 [P3 회고](../retrospectives/2026-10-16-03-netcode-p3-adapter.md)에만 기록 |

## 생성과 필수 입력

```csharp
NetworkSessionIdentity(string sessionId, string attemptId, ulong rosterRevision,
    uint protocolVersion, string sceneKey)
NetworkSessionOptions(int maxAdmissionPayloadBytes, double preparationTimeoutSeconds,
    double shutdownTimeoutSeconds, double restoreTimeoutSeconds)
NetworkSessionContext(NetworkManager manager, INetworkSessionCallbacks callbacks,
    NetworkSessionOptions options, IAdmissionVerifier verifier = null)

// NetworkSessionRequest
static NetworkSessionRequest Host(NetworkSessionIdentity identity,
    string[] participants, byte[] proof, LoadSceneMode mode)
static NetworkSessionRequest Client(NetworkSessionIdentity identity,
    byte[] proof)
```

Identity 문자열은 공백일 수 없고 Ordinal로 비교한다. `ProtocolVersion`은 프로젝트 버전이며 내부 wire 형식 버전과 별개다. `SceneKey`는 논리 식별자이며 실제 씬의 적합성은 프로젝트 Prepare callback이 확인한다.

Host 명단은 로컬 Host 참가자를 포함한 비어 있지 않은 고정 명단이다. 참가자 ID는 공백·중복을 허용하지 않는다. 요청은 명단과 proof를 복사한다. proof는 null을 허용하지 않지만 빈 배열은 허용하며 진위·만료·재사용 방지는 프로젝트 검증기가 결정한다. Client 요청의 명단은 비어 있다. Host의 초기 동기화 `mode`는 Single 또는 Additive를 명시한다. Client 요청에 저장된 모드 값은 동기화 정책 선택으로 사용하지 않는다.

**payload 상한과 세 시간 budget은 필수이며 기본값이 없다.** 양수 `MaxAdmissionPayloadBytes`는 identity와 proof를 포함한 전체 입장 envelope를 제한한다. NGO가 먼저 수행하는 역직렬화/할당까지 제한하지 않는다. 시간값은 유한 양수 초다. 준비 budget은 StartAsync에서 보호·접속·준비에 걸린 시간을 포함하며, 종료 관찰·로비 복원에는 각각 별도 budget을 사용한다. timeout은 동기 검증기나 비협조 hook을 강제로 중단하지 않는다.

manager는 활성화된 최상위 GameObject에 있고 정지 상태여야 한다. 프로젝트가 client/server transport와 `ConnectionApproval=true`를 설정해야 하며 기존 approval callback은 없어야 한다. Host에는 검증기가 필수다. 설정 검사는 보호 callback의 효과 전에 수행한다. Context 생성자는 native 시작이나 설정 변경을 하지 않는다.

## 호출·취소·소유권

```csharp
UniTask<NetworkSessionResult> StartAsync(NetworkSessionRequest request,
    CancellationToken cancellationToken = default)
UniTask<NetworkSessionResult> ShutdownAsync(CancellationToken cancellationToken = default)
UniTask<NetworkSessionResult> RetryLobbyRestoreAsync(CancellationToken cancellationToken = default)

NetworkSessionState State { get; }
NetworkSessionResult LastResult { get; }
AdmissionRejection LastAdmissionRejection { get; }
bool CanRetryLobbyRestore { get; }
```

Context 생성·명령·상태 조회는 Unity 메인 스레드에서 한다. 한 context는 한 attempt만 시작한다. 새 attempt에는 새 AttemptId와 새 context를 사용한다. StartAsync에서 manager 사용권을 획득하고 Lobby 또는 Closed 결과에서 반환한다. RecoveryFault에서는 정리가 끝났더라도 사용권을 유지한다. 다른 context의 같은 manager 시작, 반복 Start, 동기 callback 재진입은 거부한다.

각 메서드의 token은 **그 호출자의 기다림만 취소**한다. 이미 취소된 token은 명령 시작 전에 거부한다. 기다림이 취소돼도 소유 작업은 계속되므로 context 참조를 보존한다. StartAsync 재호출은 재대기 API가 아니다. LastResult로 결과를 확인하거나 명시적인 소유자 명령인 ShutdownAsync를 호출한다. 중복 Shutdown은 진행 중 정리를 공유한다.

준비 중 Shutdown은 owner 취소로 준비를 중단하고 정리 후 조건이 충족되면 로비를 복원한다. Running의 정상 Shutdown은 보호·Release·native 종료 후 Closed로 끝나며 로비를 자동 복원하지 않는다. manager/GameObject는 프로젝트 소유이며 wrapper가 파괴하지 않는다. Shutdown 결과가 RecoveryFault라면 미종료 hook/native 작업을 파괴하거나 manager를 재사용해도 안전하다는 뜻이 아니다.

## 프로젝트 callback과 입장 검증

```csharp
// INetworkSessionCallbacks
UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
void StartGameplay(NetworkSessionAttempt attempt)
UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken cancellationToken)

// IAdmissionVerifier
AdmissionDecision Verify(AdmissionRequest request)
// AdmissionDecision
static AdmissionDecision Accept(string participantId)
static AdmissionDecision Reject(AdmissionRejectionCode code)
```

정상 시작은 Protect 완료 → native 연결/필요한 초기 동기화 → Prepare 완료 → 로컬 Ready 기록/전송 → 확정 명단 전원 준비·서버 승인 → 동기 StartGameplay 순서다. 모든 화면의 같은 프레임 시작을 보장하지 않는다. 실패할 수 있는 장시간 자산/root/업무 준비는 Prepare에서 끝낸다. installer가 자기 세션이나 같은 root의 준비를 다시 기다리는 순환을 만들지 않는다.

Protect는 표시·입력을 차단한다. Release는 진행 중이던 준비 hook이 실제로 끝난 뒤 프로젝트 자원을 정리한다. RestoreLobby는 native 종료와 필요한 hook 정리가 확인된 뒤 호출한다. 복원 입력의 SessionId/AttemptId 쌍은 멱등 key다. hook은 자기 늦은 작업·취소·자원 수명을 관리하고 같은 context의 명령으로 재진입하지 않는다.

검증기는 Unity 메인 스레드에서 동기·비차단으로 끝나야 한다. Host 사전 검증과 원격 승인 검증 모두 같은 context의 명령 재진입을 효과 전에 거부한다. 신뢰된 서버 identity, 불투명 proof, 실제 ClientId/ConnectionGeneration, 인증용 ServerUtcNow, 남은 준비 초를 받는다. UTC 인증 만료와 단조 시각의 세션 budget을 혼동하지 않는다. Host도 StartHost 전에 같은 검증기를 한 번 사용하고 해당 시작 시도의 결과를 재사용한다. 반환 ID는 명단·중복 mapping 검사까지 통과해야 입장한다. 검증 예외·잘못된 결과·중복 요청은 새 입장을 거부하며 기존 정상 멤버를 대체하지 않는다. 이미 소비된 proof의 재발급은 프로젝트 책임이다.

## 결과와 실패 복원

| 결과·진단 | 읽을 값 |
|---|---|
| NetworkSessionResult | State, Started, 최초 Failure, 분리된 RecoveryErrors snapshot. Started는 State가 Running일 때만 true |
| NetworkSessionFailure | Reason, 로컬 Cause, 확인된 ParticipantId, RemoteReasonObserved |
| AdmissionRejection | 마지막 서버 로컬 ClientId, 정규화 Code, 로컬 Cause |
| NetworkSessionAttempt | Identity, IsHost |
| NetworkSessionRecovery | 원래 Attempt, 원래 Failure, 해당 시점 RecoveryErrors snapshot |

사전 조건·잘못된 구성은 ArgumentException 계열 또는 InvalidOperationException으로 거부한다. hook/native 실행 실패는 결과의 Failure와 RecoveryErrors로 구분한다. 호출자 기다림 취소는 OperationCanceledException이다. 로컬 Cause에는 민감한 정보가 있을 수 있으므로 원격에 전달하지 않는다. Host 검증기가 던진 원본 예외 객체는 결과 Failure.Cause와 LastAdmissionRejection.Cause에 동일하게 보존한다. 원격 실패는 실제 sender·세대·attempt를 확인한 뒤 정규화 코드를 보존하고 RemoteReasonObserved=true로 표시한다. 원격 사유를 못 받으면 로컬 단절·만료와 구분한다.

실패한 attempt는 먼저 무효화한다. 보호를 유지하고 준비 취소·실제 hook 완료·Release·native 종료를 관찰한 뒤 복원을 시도한다. 첫 실패를 정리/복원 오류로 덮지 않는다. 승인 후 송신 실패나 StartGameplay 실패는 일부 peer가 시작했을 수 있으므로 자동 로비 복원을 보장하지 않는다. 보호 실패도 자동 복원 성공으로 숨기지 않는다.

비협조 hook이나 native 종료가 budget 안에 끝나지 않으면 RecoveryFault를 반환하면서 소유 정리를 계속 관찰한다. 늦은 완료가 gameplay나 자동 복원을 되살리지 않는다. CanRetryLobbyRestore가 true일 때만 RetryLobbyRestoreAsync로 복원만 명시적으로 재시도한다. 이전 restore가 실행 중이면 중복 재시도할 수 없다. Release 등 정리가 실패한 상태는 재시도가 가능하다고 보장하지 않는다. 과거 결과 snapshot은 이후 오류 때문에 바뀌지 않는다.

## 씬 경계와 사용 발췌

`EnableSceneManagement=true`를 기본 권장하고 false는 프로젝트의 명시적 선택이다. wrapper는 이 값을 묵시적으로 바꾸지 않는다. true의 원격 client는 native 연결과 자기 초기 동기화가 모두 끝나야 Prepare로 진행한다. Host는 자기 SynchronizeComplete를 기다리지 않는다. false에서는 필요한 씬 구성·동기화와 in-scene 객체 처리를 프로젝트가 책임진다. 초기 Single/Additive 기본 모드는 정하지 않으며 프로젝트가 명시한다.

GameSceneManager는 확장하지 않았다. 로컬 씬 소유권·전환과 NGO의 네트워크 씬 동기화를 같은 native 씬에 중복 적용하지 않는다. GSM inventory/소유권 이관과 실제 프로젝트 root 통합은 후속이다. gameplay RPC·NetworkVariable·NetworkObject는 프로젝트가 NGO를 직접 사용한다.

아래는 프로젝트 입력 변수로 호출 순서만 보여주는 발췌다. 실행 가능한 소비 예제 검증은 P5에서 수행하며 이 발췌는 미실행이다.

```csharp
// 프로젝트 입력: NetworkManager manager; INetworkSessionCallbacks callbacks;
// IAdmissionVerifier verifier; NetworkSessionIdentity identity;
// string[] frozenParticipants; byte[] proof; LoadSceneMode initialSynchronizationMode;
// int maxAdmissionPayloadBytes; double preparationTimeoutSeconds;
// double shutdownTimeoutSeconds; double restoreTimeoutSeconds;
// CancellationToken callerWaitCancellation; CancellationToken shutdownWaitCancellation;
var options = new NetworkSessionOptions(maxAdmissionPayloadBytes,
    preparationTimeoutSeconds, shutdownTimeoutSeconds, restoreTimeoutSeconds);
var context = new NetworkSessionContext(manager, callbacks, options, verifier);
var request = NetworkSessionRequest.Host(identity, frozenParticipants, proof,
    initialSynchronizationMode);
NetworkSessionResult started = await context.StartAsync(request, callerWaitCancellation);

// 프로젝트가 종료를 결정한 별도 시점에 명시적으로 호출한다.
NetworkSessionResult stopped = await context.ShutdownAsync(shutdownWaitCancellation);
// RecoveryFault라면 manager 파괴/재사용 전에 실제 정리와 복원 가능 상태를 확인한다.
```

Client는 같은 identity와 프로젝트 proof로 NetworkSessionRequest.Client(identity, proof)를 만든다. Host verifier는 해당 참가자의 진위를 확인해야 하며 Client 자기 주장 문자열을 인증 ID로 사용하지 않는다.

Named handler 이름 `TPLab.Netcode.Session.v1`은 예약 영역이다. NGO는 공개 등록 조회 API 없이 같은 이름을 덮어쓸 수 있으므로 외부 코드의 충돌 부재까지 자동 검증하지 않는다. 사용 중 직접 Start/Shutdown·approval 교체·예약 handler 등록·ConnectionData/씬 정책 변경을 하지 않는다. wrapper는 native stop 확인 후 자기 handler와 아직 자기 값인 차용 필드만 정리한다.

P3 true Additive의 같은 프로세스 검증은 안전하게 격리한 smoke 범위이며 실제 씬 객체·GSM 통합 증거가 아니다. true Single의 wrapper 별도 프로세스 검증은 P4 미실행으로 남는다. P1 native Player 관찰은 P3 wrapper 검증을 대신하지 않는다. IL2CPP/다른 플랫폼·실제 인증/로비·WAN/다수 client·실패 조합·GC/성능·UPM 소비 설치/배포는 회고의 미실행 경계를 따른다. [AI API](../ai/api/Netcode.md)와 [전체 계약](../NETCODE_SESSION_DRAFT.md)을 함께 확인한다.
