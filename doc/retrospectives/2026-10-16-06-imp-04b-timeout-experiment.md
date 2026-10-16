# IMP-04b — Timeout 기본값·우선순위·ToCoroutine 실험

## 요청·기준·범위

2026-10-16 사용자는 IMP-04b 실험과 결과·권장안 보고를 요청했다. **타임아웃 값과 적용 범위는 결과 확인 후 사용자가 결정한다.** 기존 테스트·runtime·package·asmdef·runner·가이드 정책은 변경하지 않는다. 최종 산출물의 위험등급은 **T0(실험 결과 회고와 색인만 변경)**다. 실험 중 임시 테스트의 의도된 실패를 제품 Red/Green이나 기존 테스트 실패로 해석하지 않는다.

기준 source는 PR #40 병합 main `22f6905744516d21bb10704bc51979ba7b490845`, 작업 브랜치는 `codex/imp-04b-timeout-experiment`다. 시작 시 `git ls-remote origin refs/heads/main`과 local/remote main이 일치했다. 사용자 dirty 12개(추적 9, 미추적 3)를 포함한 기존 파일 1,299개의 SHA-256을 저장해 실행 후 대조했다. 원시 결과는 `Temp/imp-04b-20261016/`에 두고 Git에 추가하지 않는다. 이번 결과 확인과 값·범위 선택이 대기 중이므로 JSON/XML·관찰·최초 실패·실험 소스 사본을 보존한다. 사용자 검토가 끝난 뒤 이 회고의 요약을 유지하고 작업 소유 Temp만 정리할 수 있다.

근거 규칙은 [05 테스트의 D-I2](../guidelines/05-testing.md#양성-대조-범위와-미확정-사항), [D-E 증거 소유](../guidelines/07-validation-evidence.md#증거-소유와-최소-기계-입력), [이전 IMP-04 조사](../validation/imp-04-survey/README.md#3-utf160-작은-실행-실험)다. 기존 가이드의 실험 전 미확인 기록과 실제 적용 정책은 이번에 바꾸지 않는다.

## 환경·실행 방법

- Unity 6000.3.18f1, Windows Mono Editor, 기존 Editor PID 3428. 실제 exec 응답에서 idle·compile/update=false를 확인했다. 다른 Editor나 Player로 대체하지 않았다.
- Unity Test Framework 1.6.0, `com.unity.ext.nunit` 2.0.5의 NUnit assembly 3.5.0.0, UniTask 2.5.11, CLI/Connector 0.4.1, Python 3.14.
- 기존 `TPLab.Core.EditModeTests`와 `TPLab.Core.PlayModeTests`에 새 임시 `.cs/.meta`만 넣었다. asmdef나 기존 파일은 바꾸지 않았다. assembly Timeout은 해당 assembly에 영향을 주므로 이 설정이 존재하는 동안 **임시 namespace만 targeted 실행**했다. 전체 회귀 전에 임시 테스트와 assembly 속성을 제거했다.
- 임시 Editor recorder는 동일 TestRunner callback의 결과 XML을 저장했다. 전체 회귀 후 recorder도 제거하고 최종 컴파일을 확인했다. 새 영구 테스트·실험 assembly·패키지는 없다.
- 부모가 모든 Unity 실행·파일 변경·Git을 소유했다. 재사용 agent `/root/netcode_p2_review`(gpt-6.1-sol/high)는 설치 소스/IL과 실험 오라클을 읽기 전용으로 검토했다. 비동기 종료·fixture 오염 위험을 이유로 high를 사용했고 같은 파일 동시 수정은 없었다.

모든 실제 테스트 진입은 기존 runner를 사용했다. Temp `run.py`는 아래 명령의 stdout·wall time을 보존하고 XML 파일명만 지정하는 작업용 실행 기록기다. 별도 outer timeout, Editor 중단·강제 종료는 사용하지 않았다.

```text
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --filter TPLab.Core.Tests.Imp04b --output Temp/imp-04b-20261016/<run>.json
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --filter TPLab.Core.Tests.Imp04b --output Temp/imp-04b-20261016/<run>.json
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/imp-04b-20261016/recovery-full-edit.json
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/imp-04b-20261016/recovery-full-play.json
```

기존 runner는 CLI 응답에 180초 상한을 가지고 있으므로 framework 기본 180초 만료와 경합할 수 있다. 이번에는 **기본 180초 실제 만료를 기다리지 않았고**, 실행 context의 값과 짧은 대조를 관찰했다. runner 수정도 하지 않았다. 모든 유효 실험 run은 30초 미만에 결과를 받았다. PlayMode 반복 2회차는 CLI domain reload 단절 뒤 같은 CLI PID의 Connector 결과 파일로 회수했다(다른 실행 결과 대체 아님).

## 설치 소스·IL로 확인한 범위

아래는 설치 캐시 source/metadata 확인이며 실제 실행 결과와 구분한다. 캐시 이름을 외부 계약이나 저장소 추적 파일로 취급하지 않는다.

| 확인 | 설치 파일·심볼 | 읽기 결과 |
|---|---|---|
| 기본값 | UTF `UnityEngine.TestRunner/NUnitExtensions/Runner/UnityWorkItem.cs::InitializeContext` (18,63–65행), `DefaultTestWorkItem.cs` (46–48행) | context 값 0이면 180,000ms |
| 선언 위치 | NUnit `TimeoutAttribute` metadata, `ApplyToContext` IL token `0600020A` | Assembly/Class/Method 허용, AllowMultiple=false, Inherited=false; 지정 값 대입 |
| scope 전파 | NUnit `CommandBuilder.MakeOneTimeSetUpCommand` IL `060005F7`; UTF `CompositeWorkItem::RunChildren` (200–201행), `UnityTestExecutionContext` copy ctor (100행) | assembly/fixture context를 자식에게 전달 |
| method 적용 | UTF `TestCommandBuilder::BuildTestCommand` (113–116행), `EnumerableApplyChangesToContextCommand::ExecuteEnumerable` (18행) | method 값이 test 실행 전에 적용 |
| cooperative timeout | UTF `EnumerableTestMethodCommand::ExecuteEnumerableAndRecordExceptions` (79–95행) | MoveNext 반환 뒤 HasTimedOut 검사 |
| 원 UniTask 수명 | UniTask `Runtime/UniTaskExtensions.cs::ToCoroutineEnumerator` (788–847행) | pending 동안 Current=null/MoveNext=true; IDisposable·owner 취소 연결 없음 |

AttributeUsage의 `Inherited=false`와 실행 context 값의 자식 전파는 별개다. 테스트 클래스 상속 규칙까지 이번 실험으로 확정하지 않는다. 동기 body blocking은 UTF `UnityTestMethodCommand::Execute`의 body 반환 뒤 검사 경로이며 강제 중단을 보장하지 않는다.

## 최초 관측 코드 실패와 정정

첫 `default-edit` 실행은 2건 모두 다음 원문으로 실패했다.

```text
SetUp : System.NullReferenceException : Object reference not set to an instance of an object
TearDown : System.NullReferenceException : Object reference not set to an instance of an object
```

원인은 실험의 `ProbeFixture.EffectiveTimeout`이 설치 NUnit의 `CurrentTestExecutionContext` **필드**를 `GetProperty`로 읽은 것이다. 실제 metadata 조회로 필드임을 확인해 `GetField`로 정정했다. Setup 실패 뒤 record/watch가 없어 발생한 TearDown 연쇄 실패도 초기화 순서를 보완했다. 기존 runtime의 Timeout 동작이나 유효한 실험 실패로 세지 않는다. 최초 JSON/XML·소스 v1을 별도 보존했다.

독립 검토에서 NUnit fixture instance가 case 간 재사용됨을 고려해 `gate/owner/failsafe/finished/usedFailsafe`를 매 SetUp에서 초기화하도록 **matrix 첫 실행 전에** 정정했다. 이 정정은 초기 default 2건의 실패 원인과 다르다. 정정 후의 실험 코드로 기본값 및 모든 matrix 실행을 수행했고, 정정 전 matrix 결과를 섞지 않았다.

## 실험 구성과 관찰 결과

실험값은 **운영 기본값 권장 수치가 아니다**. `yield null` 기반 유한 대기와 순수 managed gate만 사용했다. 첫 PlayMode 프레임 지연을 실험값 차이로 오인하지 않도록 matrix에 5초 상한/300ms warmup 대조를 포함했다.

| 질문·case | 임시 설정과 workload | EditMode / PlayMode 실제 결과 |
|---|---|---|
| 무지정 기본값 | Test + UnityTest, 별도 속성 없음; 100ms 유한 대기 | 두 mode 각각 2/2 통과, 실행 context 180,000ms. 실제 180초 만료는 미실행 |
| assembly 기본 적용 | assembly 1,200ms, method 없음, workload 1,800ms | 두 mode·반복 모두 1,200ms Timeout 실패, 본문 정상 완료 전 종료 |
| fixture가 assembly 재정의 | assembly 1,200ms + fixture 600ms, method 없음, workload 1,100ms | 모두 context 600ms, 600ms Timeout 실패 |
| 더 짧은 method | assembly 1,200ms → method 300ms / fixture 600ms → method 250ms; workload 700ms | 모두 각각 300ms / 250ms Timeout 실패 |
| 더 긴 method | assembly 1,200ms → method 3,000ms, workload 1,600ms / fixture 600ms → method 2,500ms, workload 1,400ms | 모두 정상 완료. 부모 scope 시간을 실제로 넘어도 method 값 적용 |
| pending ToCoroutine | 반환형·중첩 yield·factory형 3개, method 400ms; 자발적으로 열리지 않는 gate | 두 mode·반복 모두 400ms Timeout 실패. 5초 failsafe 사용 0 |
| 후속 정상 실행 | 각 fixture 말미 유한 대조 + 다음 case SetUp의 pendingOperations=0 단언 | 모두 통과; 이전 owned 작업 유출 없음 |

정상 완료 대조 8건과 **의도된 Timeout 실패 7건**이 mode별 matrix 15건이다. 실패를 성공으로 바꾸어 세지 않는다. 15건 행렬을 각 mode에서 2회 수행했고 결과·실패 이유가 동일했다. 유효한 실험은 기본값 4건 + 행렬 60건 = **64회**, 통과 36회·의도된 실패 28회·예상 밖 실패 0·skip 0이다. 최초 관측 코드 오류 2건은 별도다.

| run | total | passed | expected failed | skipped | XML suite초 | 명령 wall초 |
|---|---:|---:|---:|---:|---:|---:|
| `default-edit-corrected` | 2 | 2 | 0 | 0 | 0.326 | 2.563 |
| `default-play` | 2 | 2 | 0 | 0 | 0.836 | 11.970 |
| `matrix-edit-1` | 15 | 8 | 7 | 0 | 11.221 | 19.595 |
| `matrix-play-1` | 15 | 8 | 7 | 0 | 9.502 | 27.037 |
| `matrix-edit-2` | 15 | 8 | 7 | 0 | 10.751 | 18.324 |
| `matrix-play-2` | 15 | 8 | 7 | 0 | 9.273 | 25.609 |

XML suite 시간과 CLI 준비·Play 진입/복귀를 포함한 wall time을 구분한다. matrix 실행은 전체 기존 테스트 실행이 아니다.

### ToCoroutine timeout과 원 작업의 수명

각 case는 독립 `UniTaskCompletionSource`와 owner CTS를 만들었다. owned async body는 gate를 await하고 finally 완료 여부를 기록한다. NUnit timeout 후 동기 TearDown에서 상태를 먼저 읽은 뒤 gate를 열고 owned continuation/finally 완료를 확인했다. 테스트가 자동으로 원 작업을 취소해 준다고 가정하지 않았다. 5초 Timer는 timeout이 작동하지 않아도 Editor를 영구 대기시키지 않는 **실험 자체의 escape hatch**이며, TearDown에서 dispose하고 callback 종료를 1초 상한으로 확인했다.

12/12 관찰은 동일했다: `gatePendingAtTeardown=true`, `finallyBeforeRelease=false`, `tokenCancelledBeforeRelease=false`, `finallyAfterRelease=true`, `failsafeUsed=false`, `teardownCompleted=true`. bodyCompleted 값은 명시 gate 해제 후 기록이므로 timeout 이전 성공 의미가 아니다. 원 작업의 finally는 timeout이 아니라 **정리 코드의 명시 gate 해제**로 실행됐다.

- EditMode: gate 해제 전 관찰 494.3–645.0ms, XML test-case 전체 0.595–0.746초.
- PlayMode: gate 해제 전 관찰 401.8–884.4ms, XML test-case 전체 0.402–0.892초.

위 값은 실패 순간 자체의 timestamp가 아니라 **TearDown 진입 시 관찰한 경과시간**이다. 지정 400ms보다 늦게 관찰됐다는 사실도 숨기지 않는다. frame scheduling·runner 단계가 포함되므로 hard real-time 400ms 중단을 보장하지 않는다. timeout 메시지만으로 성공 판정하지 않고 5초 failsafe 미사용·gate pending·해제 전/후 finally·후속 대조를 함께 확인했다.

실패 원문은 설정값만 달랐고 형식은 다음과 같았다. SetUp/TearDown 실패가 섞이지 않았는지 JSON/XML을 전수 대조했다.

```text
Timeout value of 400 ms was exceeded. If this is intended, increase the timeout value using a TimeoutAttribute.
```

## 실험 제거 후 회귀·보존

임시 테스트 2개와 meta를 제거해 assembly Timeout을 완전히 없앤 뒤 전체 기존 테스트를 1회씩 실행했다. XML recorder만 결과 수집에 남겨 두었다가 두 실행 후 제거했다. PR #40 최종 source의 결과 이름 집합과 비교해 Edit534/Play427 이름이 모두 동일했다.

| mode | 통과/전체 | 실패 / skip | XML suite초 | 명령 wall초 |
|---|---:|---:|---:|---:|
| edit | 534/534 | 0 / 0 | 27.208 | 29.603 |
| play | 427/427 | 0 / 0 | 14.061 | 25.064 |

이번 전체 실행에서 개별 case 최대 시간은 Edit 0.944661초(`SceneTransitionEditorTests.NestedDerivedConditionUnionIsVisitedSafelyAndNeverEvaluated`), Play 0.681389초(`UIContextPresentationTests.SeparateCanvasOrderMatchesLogicalOrderAndRejectsUnsupportedLayouts`)였다. 단일 기기의 **한 번 실행 측정**이며 느린 기기·cold CI·Player의 상한 증명이 아니다.

기존 파일 1,299개 해시와 dirty 12개는 실행 전후 동일했다. runtime·기존 테스트·asmdef·package·tools·upm 변경 0이며 임시 `.cs/.meta`는 최종 diff에 없다. 최종 컴파일 완료와 기존 Editor PID3428의 idle 응답(playing/compiling/updating=false)을 확인했다. 전체 회귀 후 Console에는 기존 LogAssert.Expect가 요구하는 Singleton6·Resource2·SceneRoot2 예외/오류 로그 10건이 남았다. 해당 기대문을 source와 대조했으며 Console0 또는 제품 화면 실행 검증으로 보고하지 않는다.

## 권장안과 사용자 결정 대기

아래는 **권장 후보이며 적용하지 않았다**.

1. 기본값은 테스트 assembly 수준에 한 번 두고, 실제로 더 오래 걸려야 하는 method만 명시 override하는 방식을 우선 추천한다. fixture 수준도 기술적으로 동작하지만 3단계 override를 상시 혼용하면 적용값 추적 비용이 커지므로 별도 목적이 있는 fixture에만 쓰는 편이 명확하다. 구체 적용 assembly 범위는 사용자 결정 후 각 기존 속성과 장기 작업을 확인한다.
2. 기본값 **30초**를 검토 후보로 제안한다. 이번 최대 단일 case 약0.945초의 약31.8배이며, 기존 framework180초보다 실패 발견이 빠르다. 이는 이번 Windows Mono 관찰만의 여유 배수다. 느린 I/O·Player/다중 프로세스 검증은 정상 소요 측정과 업무 budget을 근거로 60초 이상 등 별도 method값을 검토하며 일괄60초로 결정하지 않는다.
3. `[Timeout]`은 frame을 양보하는 테스트의 마지막 실패 감시다. 업무 상태 대기는 gate/유한 조건, worker는 Join/Wait 상한, owned 작업은 명시 취소/release 후 drain을 유지한다. **timeout이 취소·Dispose·finally·UnityTearDown 완료를 대신하지 않는다.**
4. 180초 framework 만료 자체를 검증하려면 기존 runner/CLI 상한과 분리된 별도 실험 절차가 필요하다. 이번 범위에서 tools/외부 timeout을 바꾸거나 Editor를 끊지 않았다.

사용자가 결정할 것은 (a) 기본값 숫자, (b) 대상 assembly/fixture/method 적용 범위, (c) method 예외값을 둘 장기 테스트 범위다. 실험으로 확인한 우선순위는 **method > fixture > assembly > 무지정 framework fallback**이다. 기존 D-I2의 정책 방향을 바꾸지 않으며 이번 브랜치는 값·scope 적용 구현이 아니다.

## 한계·미실행·다음 단계

- Player/IL2CPP·다른 플랫폼·실제 GC 계측 미실행. CPU 무한 루프, main-thread blocking, native AsyncOperation 무한 대기 강제 중단은 미검증이며 이번 결과를 확대하지 않는다.
- 설치 source상 UnitySetUp/UnityTearDown의 별도 열거 경로에는 같은 Timeout 검사가 없는 부분이 있다. teardown 자체의 hang을 이번 실험으로 중단시켰다고 주장하지 않는다. 이번 cleanup은 동기 managed gate release와 bounded Timer drain이었다.
- 기본180초 실제 만료, Timeout0/음수 의미, attribute 상속, 임시 실험을 넣지 않은 다른 assembly의 새 정책 적용은 미실행이다.
- 임시 실험의 시간 조건은 timeout 동작을 측정하려고 의도적으로 사용했다. 기존 상태·순서 테스트에 임의 Delay/Sleep을 추가한 선례가 아니다.
- 결과를 확인한 사용자 결정 뒤에만 정책 적용 단위를 시작한다. 이번에는 병합하지 않는다.

## 문서 검증·Git

최종 변경은 이 회고와 회고 INDEX 한 항목뿐이다. 링크/anchor·공백·PR 규칙 검사 결과와 확인한 Actions 증거는 아래에 보존한다. 이 회고를 포함하는 최종 문서 commit 자체의 ID는 Git 이력과 최종 보고에서 확인한다. 영구 코드·테스트 변경이 없어 실험용 테스트를 repository의 새 안전망 개수에 포함하지 않는다.

독립 검토는 JSON/XML/관찰 기록 64건과 최초 실패를 대조해 수치 일치·예상 밖 실패 0·복구 회귀·보존을 확인했다. 링크/anchor 검사는 267문서·local links2,197개·anchors499개에서 오류0이다. 전체 checkout의 공백 검사에서는 기존 사용자 `Assets/Scenes/InitScene.unity` 변경에 trailing whitespace3곳이 있었으며 이번 작업 소유가 아니므로 보존했다. PR 대상 문서만의 공백 검사와 규칙 검사는 별도로 판정한다.

### 2026-10-16 병합 전 CI 증거 보완

- 최초 확인한 정확한 SHA는 `89d1bb555aca7f68ee0a818ec15383b63220cf93`이며 [PR 규칙 검사 run 38866512474](https://github.com/KangSungKyu/TPLab/actions/runs/38866512474)는 `completed/success`였다. 당시 exact-SHA 조회는 PASS였고 Unity 실행을 의미하지 않는다.
- 이후 같은 SHA의 자동 리뷰 댓글 이벤트로 [Claude run 38866785328](https://github.com/KangSungKyu/TPLab/actions/runs/38866785328)이 생성되어 `completed/skipped`가 됐다. `@claude` 조건 불충족이며 리뷰가 실행된 것은 아니다. 병합 전 재조회는 이 결과 때문에 BLOCKED였고 해당 상태로 병합하지 않았다. 이를 성공이나 승인된 예외로 바꾸지 않는다.
- 자동 리뷰가 지적한 회고의 CI 링크 누락을 이 추가 기록으로 보완한다. 실험 결과·권장안·코드·테스트는 바꾸지 않는다. 보완 commit의 CI는 이전 SHA 결과로 대체하지 않고 다시 조회하며, 이 회고의 최종 commit 식별은 [PR #41 commit 이력](https://github.com/KangSungKyu/TPLab/pull/41/commits)에서 확인한다.
