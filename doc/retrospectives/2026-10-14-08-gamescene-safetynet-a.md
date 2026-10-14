# 2026-10-14 · 08 · GameSceneManager 특성화 안전망 A

T2 테스트 전용 단위다. 사용자 요청대로 최신 main `f54554a6c7ff1b06e73dcc3258d8b4170d26ddfa`에서 `codex/gamescene-safetynet-a`를 만들고, **전체 기준선 → 정상 코드 신규17 case 통과 → 임시 단일 변형 실패/원복 재통과 → 동결 후 전체 회귀**를 수행했다. runtime·기존 GameSceneManager 테스트·package·UPM·사용자 자산은 바꾸지 않았다. PR 제공 뒤 main 병합은 사용자의 명시적 승인을 기다린다.

## 변경 범위와 새 테스트

[GameSceneManagerSafetyNetATests.cs](../../Assets/TPLab/Tests/PlayMode/GameSceneManagerSafetyNetATests.cs)와 .meta만 새 테스트 소스다. 기존 `TPLab.Core.PlayModeTests.asmdef`와 `BootstrapSceneTestSetup`를 사용했고 새 assembly·fixture 파일은 추가하지 않았다. test-only callback recorder는 같은 새 파일의 `TPLab.Core.Tests.GameSceneSafetyNetCallbacks`에 둔다. runtime assembly/public API가 아니며 loader wrapper·observer는 새 테스트 내부 private 타입이다.

GUID `06ff1704a69c4d8ba850676c441b5df1`는 `rg -l -F <GUID> Assets upm -g '*.meta'`에서 새 .meta 한 곳뿐이다. 회고와 [회고 INDEX](INDEX.md)만 문서로 추가/수정한다. production 계약이 변하지 않아 XML·doc/api·doc/ai/api 갱신은 비적용이다. 기존 등록 조회 실패 테스트의 capacity 단언 보강은 사용자 지정대로 **Scene-B**에 남기며 이번에 기존 테스트를 수정하지 않았다.

| 신규 테스트 | 새 cs 위치 | 실행 case |
|---|---:|---:|
| CommandOverloads_WorkerRejectsBeforeEffects | 382 | 13 |
| Constructor_WorkerRejectsBeforeHostValidation | 448 | 1 |
| NullBuffers_WorkerUsesArgumentNullPriority | 469 | 2 |
| WaitWithoutHistory_PreservesExactRejectionAndIdleState | 504 | 1 |

UTF1.6.0의 UnityTest+ValueSource가 명령13·buffer2를 실제 발견·실행했음을 targeted 결과의 case 이름으로 확인했다. 선언4메서드와 실행17case를 구분한다.

## 명령 overload13 확정

[GameSceneManager.cs](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs)의 실제 public 명령 경로를 읽었다. 줄 번호는 위 source main 기준이며 runtime byte 불변을 확인했다.

| case | 실제 공개 overload | 선언 줄 | 최초 manager 검증 |
|---|---|---:|---|
| EnterPath | EnterFirstSceneAsync(string, mode, token) | 219 | thread → hook → caller cancellation |
| EnterTarget | EnterFirstSceneAsync(SceneTarget, mode, token) | 229 | passive request 생성 → BeginRequest thread |
| WaitEntry | WaitForEntryAsync(token) | 241 | thread → hook → entry 이력 |
| ReplacePath | ReplacePrimaryAsync(string, mode, token) | 263 | thread → caller cancellation |
| ReplaceTarget | ReplacePrimaryAsync(SceneTarget, mode, token) | 254 | request 생성 → BeginRequest thread |
| AddPath | AddDerivedAsync(string, parent, activate, priority, token) | 285 | thread → caller cancellation |
| AddTarget | AddDerivedAsync(SceneTarget, parent, activate, priority, token) | 276 | request 생성 → BeginRequest thread |
| Remove | RemoveDerivedAsync(scene, requester, token) | 298 | request 생성 → BeginRequest thread |
| TryId | TryTransitionAsync(string definitionId, token) | 485 | thread → hook → caller cancellation → ID 조회 |
| TryRequest | TryTransitionAsync(SceneTransitionRequest, token) | 499 | BeginRequest thread |
| WaitTransition | WaitForTransitionAsync(token) | 521 | thread → hook → transition 이력 |
| Cancel | CancelTransition() | 530 | thread → condition 재진입 → 실제 pending owner 취소 |
| Shutdown | ShutdownAsync() | 542 | thread → hook → 기존 shutdown 반환/종료 시작 |

[BeginRequest:308](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L308)는 thread → hook → stopping → caller cancellation → kind/mode → 권한/상태/target/loader/policy 검증 → operation 시작 순서다. 공통 worker 오류는 **정확한 InvalidOperationException 타입·`Scene management must be used on Unity's main thread.` 메시지**다.

제외 사유: constructor는 명령13에서 제외해 별도1 case로 검사한다. `GetOwnedScenes/GetRegisteredScenes`는 caller-buffer polling이며 별도 null+worker2 case로 검사한다. public 상태/snapshot/count/permission 프로퍼티11개는 명령 overload가 아니므로 이13개 목록에서 제외한다. nonnull worker buffer/count의 기존 테스트는 유지하며 capacity 보강은 포함하지 않았다.

### 효과 없음과 후속 사용

12명령은 설치된 Idle manager, Cancel은 실제 entry의 **Covering/pending** 상태에서 검사한다. Cancel은 CoverGate로 정지해 worker 취소가 owner의 token·pending completion을 바꾸지 않는지 확인하고 main에서 gate 해제 후 완료한다. 다른 command의 parent/default scene은 main에서 미리 읽으며 worker에서 native 조회는 하지 않는다. string/target/request 객체도 main에서 준비한다.

main에서 worker 호출 전·직후·다음 yield의 EffectSnapshot을 대조한다: loader Validate/Load/Unload 횟수, 모든11 callback과 progress/failure 기록, native sceneLoaded/sceneUnloaded/activeSceneChanged 이벤트, scene inventory/active/common scene, manager State/FailurePhase/LastFailure/Progress/GameScene/LoadedScene/owned/registered/permission, common root readiness/preparation, installer/condition 횟수·trace, entry/transition history를 비교한다. 콜백에는 단언을 두지 않으며 worker는 오류/완료 여부만 기록한다.

각 사례 후 main의 실제 Additive entry·Wait 두 개·Shutdown을 수행한다. Ready/CanProceed와 actual Hub path, owned1, Load1/Unload1, common Prepare1, 정상 cover/configure/presentation/reveal callback·progress, loaded/unloaded native event를 확인한다. 따라서 0으로 고정된 계측 stub을 성공 증거로 삼지 않는다. Shutdown 뒤 owned/registered가 비고 **borrowed common은 prepared**로 남는다. fixture teardown이 common을 별도로 해제하며 native 구독과 자신이 만든 host/settings만 정리한다.

### 추가 경계4 case

constructor1은 worker에서 null host로 생성해 [constructor:196](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L196)의 thread 거부가 host 검증보다 먼저임을 확인한다. main fixture manager는 정상 host로 별도 설치한다.

null-buffer2는 worker에서도 **ArgumentNullException·ParamName=buffer**가 먼저다([Owned:104](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L104), [Registered:133](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L133)). Message는 현재 환경의 `new ArgumentNullException("buffer").Message`와 정확히 비교해 플랫폼 문구를 새 계약으로 임의 고정하지 않는다. 상태/기록 불변과 후속 main 사용도 확인한다.

Wait1 case는 두 API의 exact InvalidOperationException/message를 함께 검사한다: `Start first entry before waiting for it.` / `Start a transition before waiting for it.`. default token과 이미 취소된 token 모두 이력 거부가 먼저이며 Idle·나머지 snapshot을 보존한다. 이1 case를 두 실행 case로 부풀리지 않는다.

## 실행 환경·명령·기준선/최종

측정함: Windows, 기존 **Unity6000.3.18f1·Connector0.4.1·Editor PID27224**, UTF1.6.0, Python3.14. 같은 Editor를 사용했고 새 Editor·다른 프로젝트로 대체하지 않았다. 실행 중 소스는 동결하고 부모만 Unity를 조작했다.

```text
unity-cli status --project C:/Users/PC/Projects/TPLab
unity-cli editor refresh --compile --project C:/Users/PC/Projects/TPLab
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/gamescene-safetynet-a-20261014/baseline-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/gamescene-safetynet-a-20261014/baseline-play.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --filter TPLab.Core.Tests.GameSceneManagerSafetyNetATests --output Temp/gamescene-safetynet-a-20261014/normal-targeted.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/gamescene-safetynet-a-20261014/final-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/gamescene-safetynet-a-20261014/final-play.json
```

| 실제 실행 | total | passed | failed | skipped | CLI PID / Editor PID |
|---|---:|---:|---:|---:|---|
| 신규 작성 전 EditMode | 393 | 393 | 0 | 0 | 33432 / 27224 |
| 신규 작성 전 PlayMode | 345 | 345 | 0 | 0 | 33696 / 27224 |
| 정상본 신규 targeted | 17 | 17 | 0 | 0 | 30900 / 27224 |
| 동결 후 전체 EditMode | 393 | 393 | 0 | 0 | 24996 / 27224 |
| 동결 후 전체 PlayMode | 362 | 362 | 0 | 0 | 40456 / 27224 |

모든 위 실행은 official runner의 execution에서 실제 PID·return0·nonzero 결과를 확인했고 discovery 재시도는 없었다. pass 이름 집합 비교에서 기존 Edit393·Play345가 전부 유지되고 **Play 추가 집합이 targeted17과 정확히 동일**하다. 정상본 실패·기대값 약화·fixture 정정은 없었다.

컴파일 완료를 확인했다. Console error 조회는 기준선/최종 모두 기존 테스트의 예상10로그(싱글턴6·provider2·root2)가 남았고 첫 줄 메시지 multiset이 동일했다. 새 compile/미예상 오류는 없었다. 이는 테스트가 `LogAssert.Expect`로 처리한 로그이므로 제품 Console가 비어 있다고 표현하지 않는다. 실제 게임 UX의 수동 실행은 이번 범위 밖이다.

## 임시 변형과 원복 증거

runtime 원본 GameSceneManager.cs SHA256은 `8526e10477d54b7de9a9891fde0b538b4681349674e84b1999eb360b52801745`, 동결 새 test는 `691ba4c025d30bd4214693ed4ac7aeb61c075db2f97e957651791368d57bd008`다. 모든 변형은 한 번에 **runtime 함수1곳**만 바꿨고 테스트의 hash는 최초 정상 targeted 이후 그대로 유지했다. 아래는 각 mutant의 실제 실패를 읽어 의도한 타입/메시지/상태/native-effect 오류인지 판정한 결과이며 단순 failed>0만으로 의미를 추정하지 않았다.

| 변형 | 단일 함수 변경 | mutant 결과 | 실패 사례 | 실제 실패 이유 | 원복 후 |
|---|---|---|---|---|---|
| M8 | Shutdown worker rejection message changed without native effects (public UniTask ShutdownAsync()         {) | 16/17 통과·1 실패 | CommandOverloads_WorkerRejectsBeforeEffects(Shutdown) | Shutdown: thread 메시지 대신 Mutated shutdown worker rejection. native 호출·로그 없이 직접 메시지 단언에서 실패. | 17/17·실패/skip0 |

모든17case가 적어도 한 변형에서 실제 실패했다. 8회×17=136 mutant 실행은 통과96·의도된 실패40·skip0이다. 각 원복 뒤 targeted17/17를 확인했다. M1 Shutdown은 worker에서 native 접근이 진행되어 `IsValidInternal can only be called from the main thread.` unhandled log로 실패했으므로, 이를 순수 메시지 단언의 증거로 과장하지 않았다. M8은 native를 호출하지 않고 Shutdown case의 직접 메시지 단언을 실패시켜 별도 민감도를 확인했다. Timeout으로 끊긴 실패는 없으며 Editor·후속 targeted·전체 회귀 복구를 실제 확인했다.

변형 도구는 signature와 old text를 **함수 내부 각1 match**로 좁히고 모든 preflight를 runtime 쓰기 전에 수행했다. in-memory 중복2 음성 대조도 쓰기 없이 거부했고 원본 hash가 동일했다. 추가 M8의 처음 광역 `public UniTask ShutdownAsync(` signature는 내부 wrapper까지2 match라 preflight가 **해당 변형 쓰기 전에 중단**했다. 실제 함수의 parameter/body 시작까지 좁힌 뒤8개 preflight를 통과했다. 이 예방 중단을 테스트 실패·정상본 실패로 세지 않았다.

독립 검토에서 runner 외부75초 timeout 뒤 Editor 시험 종료가 불명확한데 원복할 수 있는 위험을 발견해 **임시 도구만** 보완했다. 공식 runner의 자체 상한은 유지하고 추가 외부 timeout은 제거했다. source 쓰기 전·원복 전에 설치 UTF의 `TestRunnerApi.IsRunActive`(:217)를 read-only reflection으로 조회하여 PID27224·active=false·compiling=false·updating=false·playing=false를 확인했다. active/불명확이면 소스 동결 상태를 유지하고 restorePending으로 보고하도록 했다. 첫 쓰기 전에 idle 확인이 실패하고 원본 상태라면 재쓰기·refresh·복구 테스트도 하지 않는다. 이 오류 경로의 실제 timeout/Editor hang 실험은 하지 않았으며 정상 실행의 idle 확인만 실행했다. 결과 path의 기존 존재를 거부하고 mtime≥launch·새 execution PID를 확인해 오래된 결과를 재사용하지 않았다.

## 대기 상한·보호·한계

worker는 **background Thread.Join(3000)**, async completion은 **100 yield 또는 Stopwatch10초 중 먼저 도달하는 상한**, UnityTest와 cleanup은 `Timeout(30000)`이다. 완료 여부를 먼저 단언한 뒤 완료된 작업만 GetResult한다. gate는 finally/teardown에서 해제하고 native 이벤트는 finally에서 해지한다.

선택 근거: worker는 짧은 guard이므로3초는 OS scheduling 여유를 두면서 무한 Join을 막는다. 최초 targeted17의 driver wall은 **7.2115초**였다. UI-A와 같은 local lifecycle 보호를 사용하며30초 outer는 현재 정상 전체 targeted보다 충분히 크다. native load/unload에도100 yield 한도를 적용하므로 cold/slow 환경에서는10초보다 먼저 프레임 상한에 도달할 수 있다. 이 단위를 타 플랫폼 최대시간 보장으로 일반화하지 않는다. [기존 Timeout 한계](../guidelines/05-testing.md)에 따라 Editor main thread 자체 blocking 강제 중단·underlying 작업 취소를 보장하지 않는다. 실제 무한 hang/강제 종료 실험은 미실행이다.

부모가 배정·Unity·실제 mutant 실패 판정·원복·회고·Git을 소유했다. 기존 `/root/imp01_pool_impl`는 새 cs/meta2개만 작성했고, `/root/factcheck_upm`는 새 fixture/검사 순서/비동기 관찰/변형 도구 실패 경로를 읽기 전용 독립 검토했다. 둘 다 기존 상속 모델/추론을 변경하지 않았고 실제 설정을 추측하지 않았다. 동시성/수명 위험은 high로 취급했다. 하위 에이전트 Unity/Git/기존 소스 수정0·사용자 소유 세션 생성0이다.

회고 작성 전 기존 추적1181파일과 dirty6 raw SHA256이 동일했다. 이후 의도된 기존 파일 변경은 회고 INDEX1개뿐이다. 최종 runtime84 .cs·기존 tests·asmdef·packages·UPM diff0이며 신규 test/meta+회고+INDEX4파일만 커밋한다. 기존 dirty는 stage에 넣지 않는다. 일회성 결과·변형 도구는 작업별 Temp에만 두고 요약 이후 실행·참조가 없음을 확인해 이번 작업 소유 명시 목록만 제거한다.

의심 동작: 정상본17case에서 버그로 판정할 결과는 없었다. 반복되는 string overload의 앞 guard만 제거하면 BeginRequest guard가 남아 public 동작이 같을 수 있어, M2는 실제 공통 entry 경계를 검사했다. Ready 상태의 모든 replace/add/remove 작업 조합, 추가 validation priority, late completion/종료 검증, polling capacity, GC는 이번17case 보호 범위로 주장하지 않는다.

미실행: Player/IL2CPP·별도 GC 계측·제품 시각/입력 UX 수동 확인·artifact 빌드/설치/배포·Unity CI 테스트·main 병합/새 브랜치 삭제. 기존 전체 회귀에 포함된 GC 테스트 통과를 새 경계의 GC 계측으로 확대하지 않는다.

## PR/최종 검사

공백·링크·규칙·source Actions는 PR 게시 단계에서 보완한다. source runtime+동결 test와 최종 commit metadata를 구분한다. 최종 tip의 exact-SHA Actions는 별도 새 조회하며 PR 본문·INDEX에 위 증거 수치를 중복하지 않는다.
