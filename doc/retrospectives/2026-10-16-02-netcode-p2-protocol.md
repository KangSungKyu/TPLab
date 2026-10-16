# Netcode P2 — 내부 프로토콜·상태기계

## 요청·기준·범위

2026-10-16 사용자는 PR #36 병합 후 P2 구현을 승인했다. PR #36의 HEAD `bb3f33dc138c6cacb6afb0b7a44a3f91c96dabb4`, exact-SHA Actions 성공과 변경 없는 main을 확인하고 일반 merge했다. 병합/main은 `67fe237edfc82afb7b45d5580d6551ab3b325c20`이며 local/origin/server 일치를 확인했다. 같은 tip이 main에서 도달하고 다른 worktree가 사용하지 않음을 확인한 뒤 `codex/netcode-p1-verification`의 원격 ref는 확인 SHA 조건으로, 로컬은 `branch -d`로 삭제했다. 이력 재작성·dirty 이동/삭제는 없었다.

P2는 최신 main에서 만든 `codex/netcode-p2-protocol`이다. 시작 dirty8개는 P1의 승인된 package 입력2개가 이미 병합된 이후 남은 사용자 자산/설정이다. 원본 bytes와 상태를 Temp에서 보존한다. 위험등급 T3: 내부 전이이지만 서버 권위·세대·deadline·중단/시작 순서가 후속 비동기 수명의 기준이므로 T2와 독립 검토·양성 경로 확인을 적용한다.

사용자는 별도 질문에 개발 assembly2개(Runtime의 Core+NGO 참조, EditMode 테스트 참조) 추가를 승인했다. `TPLab.Netcode` 하나에 순수 internal 상태기계를 두며 향후 NGO 어댑터도 같은 assembly를 사용할 수 있다. NGO 미설치용 별도 순수 assembly·기존 assembly 참조 변경·UPM 구성은 만들지 않았다. 제품 source 범위는 새 `Assets/TPLab/Netcode`뿐이다. NetworkManager·GameSceneManager·UI/Input·기존 테스트·package 입력·tools는 변경하지 않는다.

## 구현 경계

프로토콜은 NGO 객체나 Unity 시간 없이 명시 입력으로 동작한다. 확정 명단·Session/Attempt/RosterRevision/protocol/scene 맥락, 검증된 참가자↔실제 sender+접속 세대 mapping, 연결 완료와 Ready의 분리, 서버 자신의 준비, 단일 시작 승인, 최초 실패와 terminal 상태를 다룬다. 잘못된 입장 요청은 해당 요청만 거부하고 기존 mapping·Ready를 보존한다. clock은 호출자가 제공하는 단조 표본이며 start+명시 budget의 정각부터 만료다. 숫자 기본값은 정하지 않는다.

P2의 입력은 adapter가 전달할 값의 계약이다. 입장 증명 검증·바이트 포맷·Named message 등록/해지·NGO 접속·Prepare/Shutdown await·로비 복원·원격 실제 전달은 P3/P4 범위다. caller 대기 취소는 상태기계 사건이 아니며 owner Cancel만 별도 입력이다. terminal Aborted는 종료 완료를 뜻하지 않고, StartAuthorized는 실제 gameplay Running을 뜻하지 않는다. 첫 실패 객체/원인은 유지하지만 cleanup/restore 추가 원인 집계는 P3에서 연결해야 한다.

## 기준선·Red 동결

환경: Unity6000.3.18f1, Windows Editor Mono, NGO2.13.3/UTP2.7.3, CLI0.4.1. 기존 TPLab Editor PID3428을 사용한다. 첫 기준선 명령은 테스트 시작 전 console 조회에서 `RuntimeError: Resolve compilation errors before testing; old Connector results are not evidence: Error: no Unity instances running`로 끝났다. 실제 C# 오류가 확인된 것이 아니라 CLI 탐색 실패였다. 같은 PID의 응답을 확인한 다음 시작되지 않은 요청만 다시 실행해 Edit398/398·Play379/379, 실패0·skip0을 얻었다. 과거 Connector 결과를 재사용하지 않았다.

컴파일 가능한 DTO/미구현 동작 뼈대와 신규 테스트를 작성했다. 처음의 테스트 컴파일에서 CS0051 3건이 발생했다: public 테스트 메서드 인자가 internal SessionAdmissionResult/SessionFailureCode였다. runtime 타입은 internal을 유지하고 테스트 파라미터를 int+명시 enum cast로 바꿨다. 실행 전 정정이며 실패한 Red/기대값을 변경한 것이 아니다.

실제 Red는 `SessionProtocolTests`62/62 실패, 통과0·skip0이었다. 모두 test body의 assertion 실패이며 SetUp/TearDown·컴파일·Timeout 실패는0이다. 대표 원문:

```text
AdmissionConnectionAndReadyAreIndependentFacts:
  Expected: Accepted
  But was:  InactiveAttempt
BothRolesRejectInvalidBudgets(0.0d):
  Expected: <System.ArgumentOutOfRangeException>
  But was:  null
ContextComparesEveryDimensionByValue(0):
  Expected: True
  But was:  False
RosterIsAnImmutableOrderedSnapshot:
  Values differ at index [0]
  Expected: "a"
  But was:  "intruder"
```

전체 원문은 작업 Temp의 red.json에 보존했다. 구현 전에 테스트/asmdef/meta의 SHA256을 동결했다. 뼈대의 공통 정상 진입에서 막힌 case도 있으므로 최초 Red만으로 각 부정 경계의 독립 민감도를 입증했다고 표현하지 않는다. 테스트에는 거부 뒤의 정상 수락, 중복 뒤 남은 멤버 수락, 시간 정각 직전/정각, 시작과 이탈의 두 순서를 포함했다. async/worker/global state/Input/EventSystem fixture가 없고 유한 동기 호출뿐이라 대기 상한이나 InputTestFixture는 비적용이다.

## 담당과 검증 기록

부모가 테스트·Red 동결·Unity/Git·문서와 최종 리뷰를 맡았다. 기존 하위 에이전트 `/root/netcode_boundary_review`(gpt-6.1-sol / high)는 계약 읽기 검토 뒤 허용된 새 Runtime 내부 구현만 맡았다. 상태·세대·수명 경계 때문에 high를 유지했다. tests/asmdef/meta(허용 새 runtime helper의 meta 제외)·기존 source·문서는 수정하지 못하게 범위를 지정했다. Unity 실행은 부모만 수행하며 실행 중 source를 동결한다.

구현 후 최초 targeted Green은62/62, 실패0·skip0이었다. frozen test/fixture5파일의 bytes가 Red 동결과 일치했다. 원래 전체 이름 집합을 유지하며 아래 전체 회귀를 통과했다.

| 실행 | EditMode | PlayMode | 실패·skip |
|---|---:|---:|---|
| 구현 전 baseline | 398/398 | 379/379 | 0·0 |
| 구현 후 전체 | 460/460 | 379/379 | 0·0 |

핵심 조건 3개를 함수 범위로 한 번에 하나씩 임시 변형했다. 변형 전 단일 일치를 확인하고, 각 실행 뒤 원본 bytes로 원복·재컴파일·targeted 재통과를 확인했다. 최초 preflight는 CRLF 때문에 일치0으로 **쓰기 전에** 중단했다. 작업 소유 Temp 도구의 읽기 시 줄바꿈 처리를 정정했으며 제품/test source는 바꾸지 않았다. 아래 결과는 그 이후 실제 실행이다.

| 임시 변형 | 의도한 실패 case | 실패 수 | 원복 후 |
|---|---|---:|---|
| SessionProtocolContext.Matches의 AttemptId 비교 제거 | AdmissionRejection(1,2), ClientRejectsEveryMismatchedStartAndAbortContext(1), ContextComparesEveryDimensionByValue(1), NewAttemptCannotAcceptTheOldAttemptMessages, ReadyAndFailureRejectEveryMismatchedContext(1) | 5 | 62/62 |
| ServerSessionProtocol.TryAuthorizeStart의 LocalReady 검사 제거 | FourFrozenParticipantsAndServerPreparationAreRequiredForOneAuthorization, ParticipantIdsDoNotImplyHostOrDedicatedServerReadiness의 Host0·서버25 두 case | 3 | 62/62 |
| SessionAttemptState.TryContinue의 기한 비교를 >=에서 >로 변경 | AdmissionAndPartialReadinessDoNotExtendTheFrozenBudget, ClientFailureKeepsGameplayClosedAndPreservesFirstReason(2,4), DeadlineIsRecheckedAtTheAuthorizationBoundary(120.0d,False) | 3 | 62/62 |

실패 원문(같은 메시지의 중복 case는 위 표로 대응):

```text
Attempt 비교 제거:
  Expected: ContextMismatch
  But was:  Accepted
  Expected: False
  But was:  True
LocalReady 검사 제거:
  Expected: False
  But was:  True
deadline 정각 비교 변경:
  Expected: Aborted
  But was:  Preparing
  Expected: not null
  But was:  null
  Expected: False
  But was:  True
```

원복 후 Runtime/test19파일은 전체 회귀 때의 bytes와 같고 Red에서 동결한 테스트 관련5파일도 동일하다. 변형 source는 커밋하지 않는다. 이 3개 실험은 해당 경계의 민감도 증거이며 모든62case에 독립 변형을 했다는 의미가 아니다.

별도 독립 검토자 `/root/netcode_p2_review`(gpt-6.1-sol / high)는 Runtime/test/설계의 읽기 전용 대조를 수행해 actionable 결함0을 보고했다. 이 검토는 독립 Unity 실행이 아니며, 실제 Green/전체 실행은 부모가 수행했다. 검토에서 P3 연결 조건을 재확인했다: 검증된 ID와 실제 sender·접속 세대 제공, 맥락 인자가 없는 connection callback의 attempt 소유자 확인, 직렬 호출과 native 정리/추가 오류 집계.

GC 경로는 읽기 기준으로 생성/명단 복사/실패 객체는 P2(초기화·저빈도), 입장·Ready·승인은 P1(사건), Poll/상태 getter는 P0 후보로 분류한다. 원격 abort의 Enum.IsDefined boxing은 저빈도 오류 경로이며 이번에 GC 계측이나 zero-events 보장을 하지 않았다. 초기화 할당을 없애기 위한 pooling이나 캐시를 새로 만들지 않았다.

## 최종 정적·보존 확인

실제 로드된 TPLab.Netcode assembly의 GetExportedTypes 결과는0이다. Editor PID3428은 컴파일/Play가 종료됐고 활성 씬은 InitScene, 미저장 씬0, 최종 error Console은 빈 배열이었다. 제품 사용/다중 peer 시나리오 실행을 주장하지 않는다.

승인된 새 Assets20파일(폴더 meta 포함 meta12개)과 문서8개만 이번 변경이다. 기존 runtime/test/assembly/package 입력/tools/workflow/upm/AGENTS/CLAUDE 변경0, dirty8개 원본 bytes 일치, meta 누락0·새 GUID 중복0을 확인했다. 전체 이름 집합은 Edit 기존398개+신규62개, Play 기존379개 그대로다.

문서 링크/anchor 검사는262개 Markdown·로컬 링크2142개·anchor496개에서 오류0이다(외부 HTTP는 검사 범위 밖). 신규/변경 계약 문서의 공통 머리말5필드와 줄 번호 링크 미사용을 확인했다.

역사적 verifier는17→17이며 실행별 stdout/stderr/종료 코드가 모두 같다. 보호 입력 선언836경로의 존재/bytes hash가 전부 같다. 이 과거 실패는 이번 테스트 통과 수에 포함하지 않는다. 저장소 전체 공백 검사에는 기존 사용자 InitScene의 trailing whitespace3줄이 있으므로 작업/PR diff만 검사하고 사용자 파일을 고치지 않는다.

## 명령·증거 보존

```text
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/netcode-p2-20261016/baseline-edit.json
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/netcode-p2-20261016/baseline-play.json
python tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --filter TPLab.Netcode.Tests.SessionProtocolTests --output Temp/netcode-p2-20261016/<red|green>.json
unity-cli editor refresh --compile --project C:/Users/PC/Projects/TPLab
python tools/check_pr_rules.py --base 67fe237edfc82afb7b45d5580d6551ab3b325c20 --head HEAD
python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <exact40SHA> --output Temp/netcode-p2-20261016/ci-final.json
```

작업 소유 `Temp/netcode-p2-20261016`에 Red·Green·전체 실행 JSON, 동결 hash, 최초 탐색/컴파일 원문, 내부 검증 도구·감사 결과를 둔다. 사용자 수락과 P3 인계에서 참조가 끝나면 이 목록만 정리하며 새 validation README나 원시 증거를 Git에 쌓지 않는다.

미실행/후속: P2 Player/IL2CPP·GC 계측·외부 UPM 설치, 실제 verifier/입장 증명·wire encoding·NGO adapter·전원 Ready 전송·복원 callback·GSM 통합·P4 다중 peer 실패 조합. 스레드 직렬화와 메인 스레드 검사는 P3 adapter의 책임이며 순수 상태기계 자체를 thread-safe라고 주장하지 않는다. S3 payload 상한·초기 Single/Additive 기본 모드·runtime budget 값·공개 wrapper API는 아직 미확정이다.
