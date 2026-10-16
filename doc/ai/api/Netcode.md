# Netcode protocol integration reference
유형: 레퍼런스
현재 상태: P3 public wrapper and NGO adapter implemented; validation and acceptance remain scoped
기준 source: ea8123c0079cb1ddeb05ff5e8d526f6cf6588d98
갱신일: 2026-10-16
계약 링크: [Netcode session contract](../../NETCODE_SESSION_DRAFT.md)

- Module / Namespace / Assembly: Netcode / `TPLab.Netcode` / `TPLab.Netcode`.
- SourceRevision: ea8123c0079cb1ddeb05ff5e8d526f6cf6588d98; runtime/test content matches the validated P3 source.
- SourcePath: [contracts](../../../Assets/TPLab/Netcode/Runtime/NetworkSessionContracts.cs), [context](../../../Assets/TPLab/Netcode/Runtime/NetworkSessionContext.cs), [internal adapter](../../../Assets/TPLab/Netcode/Runtime/SessionStartCoordinator.cs).
- HumanContract: [Human API](../../api/Netcode.md), [session contract](../../NETCODE_SESSION_DRAFT.md).
- ImplementationStatus: public Host/client wrapper, project callback lifecycle, NGO adapter and internal protocol. Dedicated-server startup, distributed authority, GameSceneManager integration and UPM distribution are excluded.
- ValidationStatus: Partial. Evidence: [P3 retrospective](../../retrospectives/2026-10-16-03-netcode-p3-adapter.md). Do not copy test counts here or substitute P1 native observations for P3 wrapper validation.

## Symbol / Signature / Constraints

Constructors and factories below are public. Context operations use Unity's main thread; immutable value contracts do not create or start a native manager.

```csharp
NetworkSessionIdentity(string sessionId, string attemptId, ulong rosterRevision,
    uint protocolVersion, string sceneKey)
NetworkSessionOptions(int maxAdmissionPayloadBytes, double preparationTimeoutSeconds,
    double shutdownTimeoutSeconds, double restoreTimeoutSeconds)
NetworkSessionContext(NetworkManager manager, INetworkSessionCallbacks callbacks,
    NetworkSessionOptions options, IAdmissionVerifier verifier = null)

// NetworkSessionRequest
public static NetworkSessionRequest Host(NetworkSessionIdentity identity,
    string[] participants, byte[] proof, LoadSceneMode mode)
public static NetworkSessionRequest Client(NetworkSessionIdentity identity, byte[] proof)

// NetworkSessionContext
public UniTask<NetworkSessionResult> StartAsync(NetworkSessionRequest request,
    CancellationToken cancellationToken = default)
public UniTask<NetworkSessionResult> ShutdownAsync(CancellationToken cancellationToken = default)
public UniTask<NetworkSessionResult> RetryLobbyRestoreAsync(CancellationToken cancellationToken = default)
public NetworkSessionState State { get; }
public NetworkSessionResult LastResult { get; }
public AdmissionRejection LastAdmissionRejection { get; }
public bool CanRetryLobbyRestore { get; }

// IAdmissionVerifier
AdmissionDecision Verify(AdmissionRequest request)
// AdmissionDecision
public static AdmissionDecision Accept(string participantId)
public static AdmissionDecision Reject(AdmissionRejectionCode code)

// INetworkSessionCallbacks
UniTask ProtectAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
UniTask PrepareAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
void StartGameplay(NetworkSessionAttempt attempt)
UniTask ReleaseAsync(NetworkSessionAttempt attempt, CancellationToken cancellationToken)
UniTask RestoreLobbyAsync(NetworkSessionRecovery recovery, CancellationToken cancellationToken)
```

| Public contract | Read-only properties |
|---|---|
| NetworkSessionIdentity | string SessionId, string AttemptId, ulong RosterRevision, uint ProtocolVersion, string SceneKey |
| NetworkSessionOptions | int MaxAdmissionPayloadBytes; double PreparationTimeoutSeconds, ShutdownTimeoutSeconds, RestoreTimeoutSeconds |
| NetworkSessionRequest | NetworkSessionIdentity Identity; bool IsHost; IReadOnlyList<string> Participants; ReadOnlyMemory<byte> Proof; LoadSceneMode InitialSynchronizationMode |
| AdmissionRequest | NetworkSessionIdentity Identity; ReadOnlyMemory<byte> Proof; ulong ClientId, ConnectionGeneration; DateTimeOffset ServerUtcNow; double RemainingSeconds |
| AdmissionDecision | string ParticipantId; AdmissionRejectionCode Code |
| AdmissionRejection | ulong ClientId; AdmissionRejectionCode Code; Exception Cause |
| NetworkSessionFailure | NetworkSessionFailureReason Reason; Exception Cause; string ParticipantId; bool RemoteReasonObserved |
| NetworkSessionResult | NetworkSessionState State; bool Started; NetworkSessionFailure Failure; IReadOnlyList<Exception> RecoveryErrors |
| NetworkSessionAttempt | NetworkSessionIdentity Identity; bool IsHost |
| NetworkSessionRecovery | NetworkSessionAttempt Attempt; NetworkSessionFailure Failure; IReadOnlyList<Exception> RecoveryErrors |

AdmissionRequest, AdmissionRejection, NetworkSessionFailure, NetworkSessionResult, NetworkSessionAttempt and NetworkSessionRecovery are produced by the module; their constructors are internal. AdmissionDecision uses its static factories.

- `NetworkSessionState`: Created, Starting, Preparing, Running, Aborting, RestoringLobby, Lobby, RecoveryFault, Closed.
- `NetworkSessionFailureReason`: LocalPreparationFailed, ParticipantPreparationFailed, ParticipantDisconnected, OwnerCancelled, DeadlineExceeded, EndpointFailed, AdmissionRejected, ProtectionFailed, GameplayGateFailed.
- `AdmissionRejectionCode`: None, InvalidPayload, ContextMismatch, VerifierRejected, VerifierFailed, VerificationDeadlineExceeded, InvalidVerifierResult, UnknownParticipant, DuplicateParticipant, DuplicateClient, AttemptClosed.

## Inputs / Outputs / Errors

Identity strings must be nonblank and compare Ordinally. SceneKey is a logical token; the project verifies actual scene/root readiness. ProtocolVersion is the application's version, separate from the internal wire format version. Host roster must be nonempty, unique and nonblank, including the local Host participant. Requests copy roster and proof. Null proof is rejected; empty proof is permitted and its validity belongs to the verifier. Host mode must be Single or Additive. Client requests have an empty roster and do not select the server's initial synchronization mode.

MaxAdmissionPayloadBytes is mandatory, positive and covers the entire identity/proof envelope. All three durations are mandatory finite positive seconds. There are no numeric runtime defaults. This admission limit does not bound allocation already performed inside NGO. A positive limit can still be too small for the chosen identity/proof; configuration/encoding must succeed before native effects.

Null required context objects, request, request identity, roster or proof throw ArgumentNullException. Null/blank identity strings and invalid roster members throw ArgumentException; invalid sizes, durations or Host mode throw ArgumentOutOfRangeException. Invalid endpoint state, missing Host verifier, reused context, unavailable lease, wrong thread or synchronous callback reentry throws InvalidOperationException. Caller wait cancellation throws OperationCanceledException. Execution failures are returned as NetworkSessionResult.Failure plus separate RecoveryErrors; do not translate RecoveryFault into successful restoration.

NetworkSessionResult.Started means State == Running, including successful local StartGameplay. It is not a distributed atomic-start acknowledgement. LastResult and recovery error lists are detached snapshots. Local Cause can contain sensitive data; do not forward it. A Host verifier exception is preserved as the same object in result Failure.Cause and LastAdmissionRejection.Cause. Remote Abort retains its normalized code after sender/generation/context checks, with RemoteReasonObserved=true; never expose remote exception text. RemoteReasonObserved distinguishes an observed normalized remote reason from a locally inferred disconnect/deadline.

## Ownership / Lifecycle / Threading

The project creates/configures/destroys NetworkManager and its GameObject, transport, addresses and prefabs. A manager must be active, top-level, idle, client/server topology, with a nonnull transport, ConnectionApproval enabled and no existing approval callback. Context construction has no native effects; Start performs endpoint preflight before Protect effects and acquires the manager lease. Do not call native Start/Shutdown, replace approval, modify ConnectionData/scene policy or use the reserved protocol handler while leased.

One context owns one attempt. Use a fresh AttemptId and fresh context for a new attempt. The lease is released only on a Lobby or Closed outcome; RecoveryFault retains it even when native cleanup has finished. Shutdown does not destroy the borrowed manager. Await/inspect actual cleanup before project destruction or reuse; there is no fire-and-forget IDisposable contract on NetworkSessionContext.

Context construction, commands and state properties are main-thread-only. The owner serializes native events and P2 state updates. Project hooks are invoked on Unity's thread; asynchronous hook completion is published back on the main thread. Hooks and verifiers must not synchronously reenter context commands, await their own session completion, or create root preparation cycles. This API does not claim thread safety or forcibly terminate work that ignores cancellation.

## Concurrency / Cancellation / FailureCleanup

The CancellationToken on each public operation cancels only that caller's wait. A pre-cancelled token is rejected before initiating the command. Later caller cancellation does not cancel preparation, shutdown or restoration. Retain the context and inspect LastResult or issue the explicit owner command ShutdownAsync. Calling StartAsync again is not a reattach mechanism. Concurrent Shutdown callers share owned cleanup; repeated Start is rejected. Parallel restore retries are rejected.

Shutdown during pending start requests owner cancellation, cancels preparation and shares its failure cleanup. Normal Running Shutdown protects, releases project resources, observes native stop and returns Closed without automatic lobby restoration. Failure before authorization can restore only after actual native/hook cleanup. Protection failure, gameplay gate failure and failure after authorization do not claim automatic lobby recovery.

The first failure remains stable. Additional protection/release/shutdown/restore failures accumulate separately. Cleanup waits for previous hooks to actually finish before overlapping protection/release. Timeout can return RecoveryFault while owned cleanup or restoration is still running. Late completion never reopens gameplay or silently restores success. CanRetryLobbyRestore must be true before RetryLobbyRestoreAsync; it requires successful native/hook cleanup, no pending restore and no active retry. Retry restores only the lobby with the original attempt/failure and stable SessionId/AttemptId idempotency key. A failed Release does not imply a retry is available.

## Configuration / ExtensionPoints

Runtime asmdef references TPLab.Core, Unity.Netcode.Runtime, UniTask and Unity.Collections. Project gameplay can use NGO NetworkObject/RPC/NetworkVariable directly. No UI/Input dependency or dependency on a concrete authentication, lobby or Relay provider is introduced.

IAdmissionVerifier is mandatory for Host startup, synchronous and nonblocking. Host pre-verification and remote admission both reject synchronous commands reentering the same context before owner/native effects. AdmissionRequest contains trusted server identity, copied opaque proof, actual native ClientId/generation, UTC authentication sample and remaining monotonic preparation seconds. Host uses the same verifier once before StartHost and reuses that attempt's result for native local approval. Accepted participant identity still needs frozen-roster and one-to-one mapping checks. Reject(None), unknown rejection codes, null decisions and blank accepted IDs are invalid results. Rejected remote/duplicate requests do not replace healthy mappings or abort the existing healthy Host. Proof consumption/reissue and authentication are project responsibilities; duplicate rejection does not roll consumption back.

EnableSceneManagement=true is recommended; false is an explicit project choice. The wrapper never silently toggles it. Host explicitly selects Single/Additive initial synchronization; no default mode is chosen by this API contract. A true client needs native connection and local synchronization; Host does not wait for its own SynchronizeComplete. For false the project owns necessary scene synchronization/in-scene object handling through preparation. Client deadline is limited by its initial local deadline and welcome-receipt time plus server remaining duration; no common clock origin or exact cross-peer expiry instant is promised.

Named handler `TPLab.Netcode.Session.v1` is reserved. NGO provides no public registration-lookup API and registration can overwrite another callback; the wrapper cannot prove absence of external collisions. Native stop requires role-specific stop events and stopped role/listening/shutdown flags. Only owned handlers and still-owned borrowed field values are restored; never destroy the project's manager as routine cleanup.

## RequiredSequence / ForbiddenUsage

1. Configure the explicit idle manager and project policy values. Freeze identity/roster and provide proof, callbacks and Host verifier.
2. Create NetworkSessionOptions, NetworkSessionRequest and a fresh NetworkSessionContext on Unity's thread.
3. StartAsync: preflight/lease → Protect → native startup/synchronization → Prepare → local-ready record before Ready send → full server barrier/authorization → synchronous StartGameplay → Running.
4. On failure, retain protection and observe cleanup. Restore only under the conditions above; inspect RecoveryFault and CanRetryLobbyRestore rather than treating timeout as completed cleanup.
5. Explicitly ShutdownAsync when the owner exits. Return/destroy project resources only after their actual owned work has stopped. Use a fresh context for another attempt.

Do not expose/call internal SessionStartCoordinator, codec or P2 types through reflection, treat native StartHost success as gameplay authorization, bypass verifier mapping, reuse a terminated root, or infer permission to destroy pending hooks from a returned timeout result.

## Example / Compatibility / Limitations

[Human usage excerpt](../../api/Netcode.md#씬-경계와-사용-발췌) uses explicitly supplied project variables, without numeric defaults. It is a non-runnable documentation excerpt; a runnable consumer sample is P5 and NotRun.

GameSceneManager is not extended. Its local scene inventory/ownership and NGO network scene synchronization must not both own/transition the same native scene. Ownership transfer and actual project root integration remain future work. P3 same-process true Additive is a safely isolated smoke, not full scene-object integration. Separate-process wrapper true Single remains P4 NotRun; earlier native Player observations do not close this gate. P4 failure scenarios, actual verifier/lobby/WAN/many clients, IL2CPP/other platforms, GC/performance and UPM installation/distribution remain bounded by the retrospective. The module is development source, not an added released UPM artifact.
