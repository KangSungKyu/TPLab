# Netcode protocol integration reference
유형: 레퍼런스
현재 상태: Internal P2 implementation; public wrapper API unavailable
기준 source: P2 작업 트리; 기준 main 67fe237edfc82afb7b45d5580d6551ab3b325c20
갱신일: 2026-10-16
계약 링크: [Netcode session contract](../../NETCODE_SESSION_DRAFT.md)

- Module / Namespace / Assembly: Netcode / TPLab.Netcode / TPLab.Netcode.
- SourcePath: [Runtime](../../../Assets/TPLab/Netcode/Runtime). HumanContract: [Human API status](../../api/Netcode.md), [P2 contract](../../NETCODE_SESSION_DRAFT.md#p2-내부-프로토콜-구현).
- ImplementationStatus: internal state machine only. No public symbols, NetworkSessionContext, SessionStartCoordinator, adapter, wire codec, installer, or consumer example.
- ValidationStatus / Evidence: [P2 retrospective](../../retrospectives/2026-10-16-02-netcode-p2-protocol.md). Native P1 observations are separate from protocol validation.

| Internal symbol | Rule |
|---|---|
| SessionProtocolContext | Immutable session/attempt/revision/protocol/scene identity; Ordinal string equality |
| ServerSessionProtocol | Frozen copied roster; verified participant to actual sender/generation mapping; admitted, connected and Ready are independent |
| TryAuthorizeStart | Local server preparation plus every frozen member ready and connected; one signal with sequence1 |
| ClientSessionProtocol | Pinned server connection and exact context; local preparation before acceptance; positive sequence accepted once |
| SessionAttemptState | Preparing to StartAuthorized or Aborted; terminal inputs ignored; first failure preserved |

Inputs / Outputs / Errors: typed internal inputs, bool/SessionAdmissionResult acceptance, immutable failure. Invalid identity/roster/clock configuration throws ArgumentException subclasses. Invalid network identity/mapping inputs are rejected without replacing valid members. Time expiry may abort the pending attempt before any input is processed. Remote abort carries only a known enum code, never an Exception or arbitrary diagnostic text.

Ownership / Lifecycle / Threading: one protocol object per attempt; the owner serializes all calls. No Unity thread guard, locking, or native effects. Aborted is not native cleanup completion; StartAuthorized is not gameplay Running. Diagnostic counts may remain after terminal state; they cannot authorize another start.

Concurrency / Cancellation / FailureCleanup: owner Cancel is an explicit attempt event; caller wait cancellation is not. Admission rejection does not abort a healthy attempt. Confirmed-member disconnect before authorization aborts; disconnect after authorization belongs to gameplay policy. Cleanup/restore callbacks and additional failure aggregation belong to P3.

Configuration / RequiredSequence: caller supplies finite start time and positive finite budget using the same local monotonic seconds as subsequent samples. Reject backward/nonfinite samples before mutation; expiry is inclusive at deadline. No runtime default, clock synchronization, or payload-size policy is defined. P3 must carry the server's remaining budget into the client-local observation policy without assuming shared clock origin. Mark local readiness before sending Ready; early start signals are rejected, not queued.

ForbiddenUsage / Compatibility / Limitations: do not invoke internal types through reflection, claim NGO-free assembly compilation, bypass admission verification, infer real sender authentication from these pure tests, or expose guessed wrapper signatures. Core/NGO are development assembly references; UPM/version/packager work remains after P5. Player/IL2CPP, GC, transport delivery, actual verifier and recovery remain unverified for P2.
