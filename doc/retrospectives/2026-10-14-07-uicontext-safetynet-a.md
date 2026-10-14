# 2026-10-14 · 07 · UIContext 특성화 안전망 A

사용자 2026-10-14 13:15 요청의 별도 테스트 PR이다. PR #19는 지정 HEAD·main·exact-SHA CI 확인 후 일반 merge했고, 이후 최신 main `73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67`에서 `codex/uicontext-safetynet-a`를 만들었다. 배포 원시 정리(PR #20)와 별도 브랜치이며 서로 통합하지 않았다. T2 특성화 절차는 사용자 지시대로 **정상본 통과 → 임시 변형 실패 → 원복 재통과 → 테스트 동결 → 전체 회귀**를 수행했다. 새 production 동작/API·런타임·기존 테스트·UPM 변경은 없다. 사용자 병합 승인 대기다.

## 계약과 수정 범위

추가 파일은 [UIContextSafetyNetATests.cs](../../Assets/TPLab/UI/Tests/EditMode/UIContextSafetyNetATests.cs)와 해당 .meta다. 기존 `TPLab.UI.EditModeTests.asmdef`를 사용하며 새 assembly·패키지는 없다. .meta GUID는 `f4bf0a14cda44b37a598516679d2e93b`이며 저장소 검색 결과 이 파일 한 곳뿐이다. 문서는 이 회고와 [회고 INDEX](INDEX.md)만 바꿨다. 런타임 계약을 변경하지 않았으므로 XML·doc/api·doc/ai/api를 재작성하지 않았다.

3개 UnityTest 선언을 UTF1.6.0의 ValueSource로 매개변수화했다. 설치 소스 `UnityTestAttribute.cs:64/69/96`의 CombiningStrategyAttribute·ParameterDataSourceProvider·IEnumerator 지원을 대조했고, 실제 discovery/execution이 아래18 case임을 확인했다.

| 새 테스트 | 위치 | 실행 사례 |
|---|---|---:|
| WorkerBoundaryRejectsBeforeNativeOrOwnerMutation | cs:160 | 11 |
| ValidationPriorityMatchesCurrentContract | cs:219 | 6 |
| DisplaysIsDetachedAcceptanceOrderSnapshot | cs:404 | 1 |

### Worker 대상 확정

[UIContext.cs](../../Assets/TPLab/UI/Runtime/UIContext.cs)의 constructor1·guarded 조회2·명령8을 선택했다.

| 대상11개 | 확인 |
|---|---|
| constructor `UIContext` | worker에서 native 생성·owner 등록 전에 거부, 반환 context 없음 |
| `Displays`, `CurrentHud` | guarded 조회2개, owner snapshot/HUD 상태 유지 |
| `RegisterHost`, `Register` | 등록 미반영, 같은 ID를 main에서 다시 등록 가능 |
| `PrepareAsync`, `BeginOpen`, `OpenAsync` | 표시·준비 상태 미변경, 즉시 오류를 관찰하며 pending을 허용하지 않음 |
| `SelectHudAsync` | CurrentHud 상태 유지, 이후 main 표시 명령 정상 사용 |
| `ShutdownAsync`, `Dispose` | owner disposed·cancellation token 변경 없이 거부 |

공통 단언은 **정확한 InvalidOperationException 타입과 `UI operations require Unity's main thread.` 메시지**, pending 없음·bounded Join 성공, IsDisposed/Fault/LifetimeToken/Displays/CurrentHud 보존, root child count와 source activeSelf 보존이다. 거부 이후 main 등록·Prepare·Open·Close·Shutdown이 정상 동작한다. worker에서는 Unity native 상태를 읽지 않고 결과만 수집하며 native/owner 단언은 main에서 한다.

제외: `RootObject`, `EventSystem`, `LifetimeToken`, `IsDisposed`, `Fault`는 guard 없는 저장된 참조/상태 조회이며 `DisplayChanged`는 event 구독이다. 현재 구현의 worker 거부 계약이 없는 항목을 거부하도록 테스트로 고정하지 않았다. 제외 멤버의 thread-safe 보장은 이 작업에서 검증하지 않았다.

### 가능한 검사 우선순위6조합

[EnsureCommand](../../Assets/TPLab/UI/Runtime/UIContext.cs#L550)의 현재 순서는 thread → 재진입 → disposed → Fault → root 파괴다. public lifecycle 경로로 실제 도달하는 상태만 사용한다.

| case | 동시에 성립하는 조건을 만드는 경로 | 먼저 발생하는 결과 |
|---|---|---|
| WorkerDisposed | main Shutdown 완료 뒤 worker Register | thread InvalidOperationException 정확한 메시지 |
| WorkerFault | acquireModalBlock이 sentinel을 throw한 뒤 worker Register | thread 오류; Fault identity 보존 |
| WorkerMutation | Closing DisplayChanged 안에서 worker Register+Join | thread 오류가 알림 재진입 오류보다 먼저 |
| MutationDisposed | Shutdown이 disposed를 설정한 뒤 Closing 알림에서 Register | 재진입 InvalidOperationException |
| MutationFault | modal factory Fault 뒤 Closing 알림에서 Register | 재진입 오류가 Fault보다 먼저 |
| DisposedFault | native Fault 보유 상태에서 Shutdown 뒤 Register | 정확한 ObjectDisposedException·ObjectName=`UIContext` |

재진입 메시지는 `Cleanup, native application, or read-only notification cannot reenter lifecycle commands.`이다. 알림 내부에는 NUnit 단언을 두지 않고 상태/오류/worker Join 결과를 기록한 뒤 테스트 본문에서 검증한다. Fault sentinel identity, Closing 상태·Displays 수, disposed 상태가 rejection 전후 유지됨을 확인한다.

root 파괴 + !disposed 조합은 [OnRootDestroyed → BeginShutdown](../../Assets/TPLab/UI/Runtime/UIContext.cs#L1167) 경로가 disposed를 먼저 설정하므로 일반 public 흐름에서 독립 판별 상태를 만들지 않았다. disposed+root 파괴는 두 분기가 같은 ObjectDisposedException이어서 순서에 민감한 오라클이 되지 않는다. private reflection으로 불가능한 상태를 강제하지 않았다. 인자 검증은 이번 EnsureCommand 조건 쌍6개 범위 밖이다.

### Snapshot 계약

Opening A·Visible B·Closing C를 acceptance 순서로 수락해 `[A,B,C]`를 캡처한다. read-only IList의 Add는 NotSupportedException이다. A 준비 완료·B BringToFront의 native sibling 순서 변경 뒤에도 acceptance 순서는 유지된다. C 완료·owner Shutdown 뒤 새 목록은 `[A,B]`→빈 목록이 되지만 기존 snapshot의 membership은 `[A,B,C]` 그대로다. **UIHandle 자체의 State는 mutable**하여 기존 snapshot의 C.State가 Closed가 되는 것도 검증한다. deep immutable snapshot으로 의미를 바꾸지 않았다.

## 환경·실행 명령·기준선

측정함: Windows의 기존 TPLab Editor **Unity6000.3.18f1·Connector0.4.1·PID27224**, Test Framework1.6.0, Python3.14. 새 Editor/다른 프로젝트를 대체하지 않았다. 테스트 실행은 부모만 소유하고 실행 중 소스는 동결했다.

```text
unity-cli status --project C:/Users/PC/Projects/TPLab
unity-cli editor refresh --compile --project C:/Users/PC/Projects/TPLab
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/uicontext-safetynet-a-20261014/baseline-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/uicontext-safetynet-a-20261014/baseline-play.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --filter TPLab.UI.Tests.UIContextSafetyNetATests --output Temp/uicontext-safetynet-a-20261014/normal-targeted.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/uicontext-safetynet-a-20261014/final-edit.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/uicontext-safetynet-a-20261014/final-play.json
```

| 실행 | total | passed | failed | skipped | CLI PID / Editor PID |
|---|---:|---:|---:|---:|---|
| 신규 파일 작성 전 전체 EditMode | 375 | 375 | 0 | 0 | 18696 / 27224 |
| 신규 파일 작성 전 전체 PlayMode | 345 | 345 | 0 | 0 | 38592 / 27224 |
| 정상본 신규 targeted | 18 | 18 | 0 | 0 | 29432 / 27224 |
| 동결 후 전체 EditMode | 393 | 393 | 0 | 0 | 38300 / 27224 |
| 동결 후 전체 PlayMode | 345 | 345 | 0 | 0 | 19116 / 27224 |

기준선 Play 최초 CLI35064는 실제 실행 전 discovery 거절(prestartDiscoveryRejected=true, return1)이었다. 공식 runner가 같은 Editor의 discovery를 재확인한 뒤 CLI38592에서345개를 실행·통과했다. 이를 실패 테스트0건/첫 시도 성공으로 혼동하지 않는다. 최종 두 전체 실행에는 재시도 없었다. 신규 import 시 refresh 한 번은 connector 미발견이었지만 기존 Editor27224의 ready/프로세스를 확인한 뒤 재연결 컴파일했고 새 인스턴스는 만들지 않았다.

결과의 pass 이름 집합을 대조했다. 기존 Edit375개는 전부 보존되고 추가 집합은 정상 targeted의18개와 정확히 같다. Play345개 이름 집합도 기준선/최종 동일하다.

제품 Console의 error 조회에는 기준선/최종 모두 기존 테스트가 `LogAssert.Expect`로 선언한10개 로그가 있었다(싱글턴6·provider2·root2). 첫 줄 메시지별 multiset이 동일하고 새 컴파일/미예상 오류는 없었다. 근거 단언: MonoSingletonTests:119/135/147/148/169/207, ResourceManagerTests:212/499, SceneRootTests:116/137. 테스트 예상 로그가 남아 있으므로 Console를 비어 있음으로 보고하지 않는다. 컴파일 완료를 확인했으며 별도 실제 제품 UI 수동 실행은 하지 않았다.

## 임시 변형 민감도와 원복

한 번에 UIContext.cs 한 변형만 적용하고 컴파일·같은 targeted18 실행 → 원본 bytes 복원 → 재컴파일·targeted18 재통과 순서로 진행했다. 새 테스트는 최초 정상본 통과 뒤 변형/최종 회귀 동안 동일 SHA256 `45fe122bb1c70f8303ba4807dc259a1c9445eda0f2739c0b159d2688d1d2c71c`를 유지했다. 원본 UIContext.cs SHA256은 `c0caf7871b1ad30a8aeac3e3f97b3d2b78f3d527cbd3afd8f3e121f3ea26d753`다. 변형 코드·임시 도구는 커밋하지 않았다.

| 변형 | 단일 변경 | 변형 결과 | 실패 사례 | 의도한 실패 이유 | 원복 후 |
|---|---|---|---|---|---|
| M1 | main-thread guard body removed | 4/18 통과·14 실패 | worker 11개 전부; priority WorkerDisposed / WorkerFault / WorkerMutation | Constructor는 UnityException, guard 없는 조회·명령은 예외 없음, async Open/HUD는 pending. worker 우선순위는 disposed/Fault/mutation 예외가 먼저 나옴. | 18/18·실패/skip0 |
| M2 | EnsureCommand thread/mutation order swapped | 17/18 통과·1 실패 | ValidationPriorityMatchesCurrentContract(WorkerMutation) | WorkerMutation: thread 메시지 대신 mutation 메시지. | 18/18·실패/skip0 |
| M3 | mutation guard moved below disposed check | 17/18 통과·1 실패 | ValidationPriorityMatchesCurrentContract(MutationDisposed) | MutationDisposed: InvalidOperationException 대신 ObjectDisposedException. | 18/18·실패/skip0 |
| M4 | disposed/Fault checks swapped | 17/18 통과·1 실패 | ValidationPriorityMatchesCurrentContract(DisposedFault) | DisposedFault: ObjectDisposedException 대신 native Fault InvalidOperationException. | 18/18·실패/skip0 |
| M5 | Fault check moved ahead of mutation guard | 16/18 통과·2 실패 | ValidationPriorityMatchesCurrentContract(MutationFault); ValidationPriorityMatchesCurrentContract(DisposedFault) | MutationFault: mutation 메시지 대신 native Fault 메시지. DisposedFault도 InvalidOperationException. | 18/18·실패/skip0 |
| M6 | Displays returns live readonly collection | 17/18 통과·1 실패 | DisplaysIsDetachedAcceptanceOrderSnapshot | snapshot의 기존 3개 항목이 live 목록의 2개 항목으로 변함. | 18/18·실패/skip0 |

모든18 case가 적어도 한 변형에서 실패했다: worker11과 worker 관련 priority3은M1, MutationDisposed는M3, MutationFault는M5, DisposedFault는M4/M5, snapshot은M6. 실패는 모두 기대 타입/메시지/pending/membership 단언이며 Timeout으로 종료된 실패는 없었다. 변형6회의 전체108 case 중 의도된 실패20·통과88·skip0이다.

M2 임시 도구의 최초 preflight는 thread/mutation 문자열이 EnsureCommand 외 Dispose에도 있어 중복2를 확인하고 **runtime 쓰기 전에 중단**했다. EnsureCommand 함수 범위로 좁혀 실행했다. 그 예방 중단은 변형 성공으로 세지 않았고 원본에서18/18 재통과도 확인했다. 테스트 기대값·fixture는 수정하지 않았다.

## 대기 상한과 한계

각 UnityTest는 `Timeout(30000)`을 선언하고, 실제 worker 대기는 **background Thread.Join(3000)**으로 제한한다. async completion은 **100 yield 또는 Stopwatch10초 중 먼저 도달하는 상한** 뒤 IsCompleted를 단언하고, 완료한 작업에만 GetResult한다. gate는 finally/TearDown에서 해제하고 이번 fixture가 만든 root/source/context만 정리한다.

상한 선택 근거: worker는 짧은 guard를 관찰하므로3초는 OS scheduling 여유를 주면서 hang을 유한 실패로 만든다. local lifecycle에는 external IO가 없고 최초 targeted18 driver wall은2.1673초였으므로10초 helper·30초 outer는 현재 정상 실행보다 충분히 크며 정상 타이밍을 계약으로 만들지 않는다. [기존 Timeout 조사 한계](../guidelines/05-testing.md)를 확인했다. Timeout/100 yield는 Editor 메인 스레드 자체 blocking을 강제 중단하거나 underlying 작업의 취소·정리를 보장하지 않는다. EditMode ToCoroutine hang에 대한 Timeout 적용 실험은 이번에 하지 않았다. worker Join과 completion 완료 단언을 실질 오라클로 삼는다.

## 보호·검토·확정 범위

부모가 배정·Unity·최종 검증·회고·Git을 소유했다. 기존 하위 에이전트 `/root/imp01_pool_impl`를 신규 cs/meta만 작성하도록 재사용했고, `/root/factcheck_upm`는 runtime와 새 테스트의 순서·async 결과·callback 단언·cleanup을 읽기 전용 독립 검토했다. 둘 다 상속된 기존 모델/추론을 변경하지 않았고 실제 모델 설정을 새로 추측하지 않았다. 동시성/수명 위험은 high로 취급했다. 하위 에이전트 Unity/Git/기존 파일 수정0, 새 사용자 세션 생성0이다.

회고 작성 전 원시 SHA256 대조에서 기준선 추적1316파일·runtime84 .cs·dirty6가 모두 동일했다. 회고 INDEX 한 곳만 의도된 변경에 추가한다. 최종 Git diff에서 runtime·기존 테스트·asmdef·packages·UPM 변화0, 신규 테스트2파일+회고1+회고 INDEX1로 한정한다. dirty6는 stage/commit에 포함하지 않는다.

의심 동작/발견 사항: 정상본18개에서 버그로 판단할 결과는 발견되지 않았다. root 파괴 독립 우선순위와 guard 없는 조회/구독, provider 호출 횟수·GC 계측은 이번18개로 보호했다고 주장하지 않는다. 배포 정리 PR #20도 같은 회고 INDEX에 행을 추가하므로 두 PR을 향후 통합할 때 색인 충돌이 생기면 각 행을 보존해야 한다(이번에는 통합하지 않음).

미실행: Player/IL2CPP, GC 별도 계측, 실제 제품 시각·입력 UX 수동 확인, artifact 재빌드/설치/배포, Unity CI 테스트, main 병합·새 브랜치 삭제. Actions는 규칙 검사이며 위 로컬 Editor 실행을 대체하지 않는다. 일회성 runner 결과·변형 도구는 이번 작업 소유 Temp에만 두고 완료 요약 이후 진행 중 실행·참조가 없음을 확인해 명시 목록으로 제거한다. 영속 문서는 그 임시 도구를 요구하지 않는다.

## 최종 검사 기록

공백·링크·규칙·exact-SHA Actions는 PR 게시 단계에서 결과를 보완한다. source(위 기준 runtime+동결 test)와 최종 commit metadata를 구분하며 이후 runtime/test 변경이 있으면 해당 검증을 다시 실행한다.
