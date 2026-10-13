# IMP-05 · 씬 폴링 할당 재현 · 계측 확인 대기

2026-10-13. **ImplementationStatus: Proposed / ValidationStatus: Partial**. 기준 main `9285805bd234d1988019c890e3670945d88bce9d`, branch `codex/imp-05-scene-polling-gc`. runtime·public API 변경 없음. 예상 Red가 통과해 [백로그 중단 규칙](../../IMPROVEMENT_BACKLOG.md)에 따라 자동 구현을 멈췄다.

## 선행 승인·PR #8 통합

사용자가 IMP-02 PR #8 병합을 승인했다. 승인 후보039d132에서 수락 문서9개만 갱신했고 최종tip `e7fa70eda058ac023725ad2f1e8fe87e1e3a3bca`의 규칙25개/오류0/경고0·상대링크247개/누락0·[exact Actions 성공](https://github.com/KangSungKyu/TPLab/actions/runs/38328195499)을 확인했다. 일반 merge main9285805에 원본tip 도달성을 확인하고 작업 branch를 로컬/원격에서 삭제했다. local/origin/server main 일치·사용자 dirty6 raw hash를 확인했다. [통합 영수증](pr8-merge-receipt.json), [CI 조회 요약](pr8-final-tip-ci-summary.json).

main checkout 때 tested10 파일의 줄바꿈만 CRLF→LF로 바뀌었다. Git blob/정규화 내용과 기존 검증 소스는 동일하다. raw hash 보존과 동일하게 표현하지 않는다. 최종 작업 트리는 기존 dirty6만 남겼고 이 normalization은 통합 영수증에 구분했다. PR 본문의 상세 실행 정보 갱신은 자동 승인 검토가 공개 위험으로 거부해, 로컬 경로·프로세스 정보가 없는 변경/검증 요약으로 갱신했다.

## 현재 main의 근거 대조

| 근거 | main9285805 위치 | 결과 |
|---|---|---|
| OwnedScenes / RegisteredScenes | GameSceneManager.cs:69–74 | LINQ/배열 스냅샷 그대로 |
| CanProceed | :78–83 | 등록 iterator/All, ExpectedGameScenes·InventoryMatches 그대로 |
| 기대 씬 / owner iterator | :996–1007 | Concat/Select/ToArray와 yield 경로 그대로 |
| InventoryMatches / GetLoadedScenes | :1120, :1200–1201 | HashSet/SetEquals 및 LINQ 그대로 |
| 기존 direct manager 준비 | 기존 GameSceneEntryTests.cs:79 | 백로그78행과 한 줄 차이, 계약 차이 없음 |

행 번호는 새 테스트 삽입 전 main을 기준으로 한다. Core의 CanProceed 소비는 :373의 요청 경로다. 소비자의 프레임 폴링 비용만 이번 P0 대상이며 IMP-11 전환 경로 전체나 클래스 분리를 진행하지 않았다.

## 실제 실행과 예상 밖 결과

기존 CreateBootstrap(Additive)·direct manager·EnterFirstSceneAsync(Hub)·기존 TearDown을 재사용했다. `[UnityTest]`, `[Timeout(30000)]`인 CanProceedDoesNotAllocateInReadyState 하나를 추가했다. Ready 확인 후 getter1회 워밍업, 같은 스레드에서100회 호출 전후 GC.GetAllocatedBytesForCurrentThread 차이를 strict0으로 확인한다. 측정 구간에는 await/로그/assertion이 없다. 이후 canProceed==true와 allocated==0을 단언한다.

`tools/run_unity_tests.py`로 정확한 새 메서드를 실행했다. **total1/pass1/fail0/skip0**이며 실제 통과 이름도 일치한다. 이는 **예상 Red 미확립**이고 Green·최적화 완료가 아니다. [실행 결과](unexpected-red-result.json). 테스트 hash를 추가 직후 고정했고 실행 뒤에도 동일하다.

직후 같은 Editor의 **유휴 EditMode**에서 `new byte[4096]`·GC.KeepAlive를 양성 대조로 실행했지만 before0/after0/delta0이었다. [실험과 코드](idle-counter-positive-control.json). 이 계측은 의도한 배열 할당을 감지하지 못했으므로 getter의 실제 할당0 증거로 해석할 수 없다. PlayMode 내 양성 대조와 ProfilerRecorder 등 대체 계측은 아직 실행하지 않았다. 모든 backend에서 해당 API가 지원되지 않는다고 확정하지 않는다.

도메인 reload 중 첫 driver는 Console 연결 확인에서 실행 전 중단했다(테스트 시작0건). 같은 Editor의 ready/compile/import/play/testRun 상태와 실제 메서드 로드를 확인한 뒤 실제 테스트1회를 실행했다. 실패 case 재실행0건. 일회성 상태 조회의 잘못된 UTF 메서드 이름과 미지원 CLI refresh 명령도 각각 도구 단계에서 실패했고 테스트 실패로 세지 않았다. 설치된 UTF의 internal IsRunActive를 reflection으로 조회하고 AssetDatabase.Refresh로 반영했다.

[입력 보존](source-preservation.json): 기존 tracked518개 중 신규 테스트를 추가한 기존 파일1개 외517개 raw hash 동일, dirty6 보존, Core/Input runtime 변경0. 실행 뒤 play/compile/import/testRun false·dirty scene0, 제품 Console error0을 관찰했다. Console을 비우지 않았다.

## 구현 전 사용자 결정

1. 예상 Red가 통과했으므로 계측 정상성을 먼저 조사할지, IMP-05를 보류할지 결정이 필요하다. 조사 승인 뒤에도 기존 고정 테스트의 계측 변경은 별도 검토하고 runtime 구현을 시작하지 않는다.
2. 계측/Red를 해소한 뒤 백로그가 정한 버퍼 API 이름·시그니처 확인을 받는다. 다음은 **Proposed, 현재 호출할 수 없는 API**다.

```csharp
public int OwnedSceneCount { get; }
public void GetOwnedScenes(List<Scene> buffer);
public void GetRegisteredScenes(List<SceneRegistration> buffer);
```

OwnedSceneCount/GetOwnedScenes는 기존 OwnedScenes와 같은 primary→derived→candidate 순서·valid && loaded 필터, 실패 후 후보/잔존 소유 씬을 보존한다. common/retained 씬은 제외한다. Registered는 primary→derived의 성공 등록만, candidate 제외·기존처럼 추가 loaded 필터 없이 Parent/Role/Priority/준비/종료 상태를 그 시점에 복사한다. 기존 할당 스냅샷 프로퍼티는 유지한다. 캐시하지 않는다.

버퍼는 호출자가 소유하고 메서드가 Clear 후 채우며 null은 ArgumentNullException 후보 계약이다. Unity 메인스레드 전제와 충분한 Capacity 이후에만 추가 storage 할당0을 기대한다는 제한을 제안한다. CanProceed 최적화 시 short-circuit 순서 및 SetEquals의 양방향 포함/중복 무시 의미도 보존해야 한다. 이 계약은 아직 구현·사용자 확인되지 않았다.

## 미실행·재개

Green, 전체 EditMode/PlayMode 회귀, Player/IL2CPP/외부 소비 설치·대체 계측은 미실행이다. 의도한 Red를 확립하기 전 전체 통과나 최적화 완료를 주장하지 않는다. 이번 branch의 PR은 최종 검증 단계 전이라 아직 만들지 않았다. PR #8 최종 Actions와 이 branch의 로컬 규칙 검사는 구분한다.

Temp/imp-05에는 원래 CLI 결과·고정 hash/입력·dirty 보존 자료를 재개용으로 남긴다. 사용자 조사 결정과 필요한 증거 보존 뒤 해당 소유 파일만 정리한다. 이전 요청의 Unity/PC 종료 권한을 재사용하지 않는다. [회고](../../retrospectives/2026-10-13-01-imp-05-polling-measurement.md).
