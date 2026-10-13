# IMP-02 · 폐기 뒤 반납을 허용하는 TryReturn

## Editor 계측 한계에 의한 정정 (2026-10-13)

기존 `GC.GetAllocatedBytesForCurrentThread()`의 0바이트/pass 기록은 실행 당시 관측으로 보존한다. 후속 static4KB 양성 대조가 Edit/Play에서0을 보고했으므로, 당시 결과는 이 Editor의 무할당 증거로 사용할 수 없다. 아래의 기존 0바이트 서술은 이 정정의 제한을 받는다. 기존 JSON·소스 hash·로직/기능 검증 결론은 변경하지 않았다.

[IMP-14 측정 교체 검증](../imp-14/README.md)에서 기존 pool GC3개를 호출마다 양성·음성 대조를 검증하는 ProfilerRecorder 현재 스레드 이벤트 계측으로 교체했다. 재사용·정원/Count·죽은 대여 회복·TryReturn 반복을 포함한 각1000회 범위에서 workload **0 events**를 확인했다. 이는 byte 단위 보장이 아니며 factory/callback/Unity 생성·초기확장·Dispose 전체 비용은 포함하지 않는다. D-01=A1 지연 회수와 D-02 TryReturn 계약·로직 결론은 유지한다. Player/IL2CPP는 미검증이다.

2026-10-12 자동 검증, 2026-10-13 사용자 수락·병합 승인. **Implemented / 승인·완료**. 브랜치 `codex/imp-02-try-return`, 기준 main `592c43515fa67e2ab48f64254726ca210cbdfe2f`, 검증 소스 `2c8c0768c53878dcc9c2d4ed090582092e9dc5e0`. 최종 문서 HEAD·PR·정확한 SHA Actions 결과는 완료 보고와 PR 본문에서 확인한다.

## 범위와 선행 승인

[백로그](../../IMPROVEMENT_BACKLOG.md)의 D-02=(b)만 구현했다. 사용자 PR #7 병합 승인과 큐의 승인 뒤 자동 진행 규칙(33/55행)에 따라 새 main에서 시작했다. PR #7은 최종 문서 tip `71b3b0416d125e31300a3d90da9ab181e7ebaeea`의 exact Actions 성공 후 일반 merge로 main `592c43515fa67e2ab48f64254726ca210cbdfe2f`에 통합됐고 로컬·원격 branch가 삭제됐다. [통합 증거](pr7-merge-receipt.json)에 승인 후보·최종tip·main·Actions URL을 구분했다.

ObjectPool/PrefabPool에 다음 API만 추가했다. 기존 Return/Dispose 시그니처·본문과 기존 테스트는 변경하지 않았다. 새 패키지·설정·배포 사본·Prune·IMP-03/05 등 다른 기능은 추가하지 않았다.

```csharp
// ObjectPool<T> (클래스의 T : class 제약 유지)
public bool TryReturn(T instance)
// PrefabPool
public bool TryReturn(GameObject instance)
```

- 정확한 구현: `if (IsDisposed) return false; Return(instance); return true;`.
- **disposed가 입력보다 우선**한다. 이미 폐기됐다면 null/foreign/destroyed 입력도 false이며 callback·파괴·상태 변경·정리 재시도는 없다. Dispose가 소유한 객체의 정리를 시도할 책임자다.
- 열린 풀은 기존 Return을 그대로 수행해 성공하면 true다. null·foreign·duplicate·context·재진입·callback 실패는 기존 예외/검증 순서/실패 정리를 유지하며 false로 숨기는 catch가 없다. 기존 Return은 종료 뒤 계속 ObjectDisposedException이다.
- Dispose가 callback 전에 IsDisposed를 확정하므로 Dispose cleanup 중 TryReturn은 false다. 활성 rent/predicate/return/실패 cleanup 중 변경 재진입은 기존 오류다. 두 상황을 혼동하지 않는다.
- API는 동기·단일 스레드 순차 사용이다. Prefab은 Unity main-thread **사용 전제**다. 기존 runtime에는 명시적인 thread guard가 없으므로 이 단위에서 새 guard·lock·동시 Dispose 동기화를 추가하지 않았다.

기준 main에서 백로그 위치를 다시 대조했다. ObjectPool.BeginOperation은 현재306행(과거201–212), Return은139행(과거124), Prefab.EnsureOpenContext179행·Return92행이다. 기존 ObjectPoolTests의 폐기 후 예외 assertion274/275는 그대로고 PrefabPoolTests.DisposedPoolRejectsRentAndReturn은 IMP-01 Red 추가로253행(과거242)이다. 행동/계약 차이는 없고 줄 위치만 달라졌다.

## 실제 TDD·Green·전체 회귀

기존 Editor PID24376, Unity6000.3.18f1 / UTF1.6.0 / CLI·Connector0.4.1 / Windows Mono에서 mandatory `tools/run_unity_tests.py --project <TPLab 절대 checkout> --mode <mode> [--filter <정확한 fixture>] --output <작업 JSON>`을 사용했다. 전체 회귀는 filter 없이 실행했다. 소스/테스트/meta를 실행 중 동결하고 refresh 뒤 실제 loaded method를 확인했다.

새 API가 없는 상태에서도 컴파일 가능한 test-only reflection binder로 **public bool signature 존재**를 assertion하고, 존재하면 typed Func delegate를 한 번 바인딩한다. Red는 API 부재의 실제 NUnit 실행 증거다. 생성하지 않은 API의 컴파일 실패나 아직 구현되지 않은 각 분기의 동작 실패라고 주장하지 않는다. delegate는 실제 원래 예외를 그대로 전달하며 GC 측정 중 reflection/boxing/인수 배열은 생성하지 않는다.

| 단계 | fixture/범위 | total | pass | fail | skip |
|---|---|---:|---:|---:|---:|
| Red | ObjectPoolTryReturnTests | 13 | 0 | 13 | 0 |
| Red | PrefabPoolTryReturnTests | 7 | 0 | 7 | 0 |
| Green | ObjectPoolTryReturnTests | 13 | 13 | 0 | 0 |
| Green | PrefabPoolTryReturnTests | 7 | 7 | 0 | 0 |
| Green | 기존 ObjectPoolTests | 24 | 24 | 0 | 0 |
| Green | 기존 PrefabPoolTests | 18 | 18 | 0 | 0 |
| 회귀 | 전체 EditMode | 348 | 348 | 0 | 0 |
| 회귀 | 전체 PlayMode | 316 | 316 | 0 | 0 |

[실제 Red](red-runs.json)의 실패20개는 모두 Required public API assertion에 해당하며 원문 예시는 다음과 같다.

```text
Required public API: bool ObjectPool`1.TryReturn(Item)
  Expected: not null
  But was:  null
```

```text
Required public API: bool PrefabPool.TryReturn(GameObject)
  Expected: not null
  But was:  null
```

실제 전체 테스트명·원문·실행시각·PID·runId·CLI 반환값은 [Red](red-runs.json), [Green](green-runs.json), [전체 회귀](regression-runs.json)에 보존한다. source/meta8개를 Red 이전에 hash 고정했고 구현 단계에서 수정하지 않았다. 기존 Return 테스트 파일도 원문 hash가 동일하다. 이후 core2와 합친 tested10개를 Green/전체 실행 동안 동결했다.

Generic13case는 정상 반환/reset1회·Count/재사용, disposed loan/null/foreign false와 기존 Return 예외, active null/foreign/duplicate, Dispose의 모든 cleanup 계속, reset+cleanup AggregateException/정원 회복, return/rent/predicate 재진입, GC를 검증한다. Prefab7case는 활성 정상 반환/비활성/reset/재사용, 오용 검증, 다음 프레임의 destroyed late-loan false, 실제 **OnDisable/OnDestroy**의 TryReturn false·다른 cleanup 계속, rent 재진입, reset 실패 폐기, destroyed parent의 context-before-null 우선순위를 검증한다.

기존 `PrefabPoolTests.DisposedPoolRejectsRentAndReturn`와 ObjectPoolTests274/275의 계약이 해당 fixture 및 전체 회귀에서 통과했다. 이전644개 pass 이름을 모두 보존하고 신규13/7만 추가된 것을 비교했다. 활성 상태의 destroyed clone을 입력하는 독립 새 case는 없으며 이 경계는 unchanged Return 위임·검증 순서의 소스 리뷰 범위로 구분한다.

## 실행 복구·GC·제품 Console

Green Edit13 실행 전 CLI가 instance를 찾지 못한 시도가1회 있었다. 도구가 **아직 test가 시작되지 않은 경우**로 분류하고 같은 Editor를 재탐색한 뒤 실행했다(첫 cli46892/prestartDiscoveryRejected=true, 실제 cli47568/pass13). 실행한 실패 case의 재실행0회이며 모든 실행이 CLI 단일 attempt라고 주장하지 않는다. 일부 PlayMode는 reload로 CLI1을 반환했지만 동일 CLI PID의 fresh connector 결과로 driver0·정확한 nonzero 이름/수를 확인했다. 다른 Editor·suite·과거 결과로 대체하지 않았다.

`WarmedTryReturnReuseAllocatesZero`는 delegate 바인딩과 storage warmup 뒤 **1000회 TryRent/TryReturn에서 GC.GetAllocatedBytesForCurrentThread 차이0바이트** assertion을 집중·전체 EditMode에서 통과했다. 임계값 변경 없음. pool bookkeeping 범위이며 최초 storage 확장·caller callback/factory·Unity Instantiate·Dispose 전체 비용은 측정하지 않았다. CPU 비용 불변이나 다른 backend의 GC0을 입증하지 않는다.

[환경/Console](environment.json): 새 두 method 로드와 compile/import 종료를 확인했다. 실행 뒤 Play/compile/import/TestRun false. 제품 Console의 예상예외10건은 기존 LogAssert(싱글턴6/provider2/root2)와 대조했고 추가 미예상/CS 오류는 관찰되지 않았다. Console을 비우거나 로그0으로 표시하지 않는다.

[입력 보존](source-preservation.json): original510개 중 core2를 제외한508개·dirty6·고정Red8·tested10 raw hash가 동일하다. 기존 Return/Dispose body4개 동일, 신규meta4개 GUID유일, 기존GUID/사용자settings/자산보존·임시test scene잔여0을 확인했다. canonical Git blob hash와 raw 파일 hash를 구분한다.

## 문서·사용자 gate와 미실행

XML·[GENERIC_POOL](../../GENERIC_POOL.md)·[OBJECT_POOL](../../OBJECT_POOL.md)·[사람 API](../../api/Pooling.md)·[AI API](../../ai/api/Pooling.md)에 bool 반환 의미·검증 순서·cleanup 책임·호환성·제약을 함께 반영했다. 기존 사람/AI 예제와 새 발췌는 **NotRun**이다. 새 소비 프로젝트 설치·Player/IL2CPP/다른 Unity버전/플랫폼·시각UX·경합을 허용하는 멀티스레드 테스트는 이번 미실행이다. 기존 테스트 성공으로 범위를 확대하지 않는다.

[회고](../../retrospectives/2026-10-12-11-imp-02-try-return.md)와 두 색인, 백로그의 IMP-02 상태3곳을 갱신했다. D02 설계나 다른 항목 본문을 바꾸지 않았다. source commit과 문서 commit을 같은 PR로 제공하며 source-only 상태를 최종 제공본으로 표시하지 않는다. 최종 HEAD의 로컬 규칙/Actions는 완료 보고·PR에서 확인하고 Actions를 Unity CI로 오인하지 않는다.

2026-10-13 사용자가 PR #8 병합을 승인했다. 승인 후보는 `039d1323176758ff4297459e1b13e9a9e561877f`이며 source/test 변경 없이 승인 기록만 갱신한다. 최종 문서 tip의 규칙/Actions를 확인한 뒤 일반 merge와 해당 branch 정리를 진행한다. 실제 merge SHA·정리는 PR/Git 이력과 최종 보고를 따른다. 이후 백로그 승인 게이트에 따라 IMP-05를 시작하고, 버퍼 API 이름·시그니처 확인 단계에서는 구현을 멈춘다. 이전 단위의 Unity/PC 종료 지시를 이번 요청에 재사용하지 않는다.
