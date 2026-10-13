# IMP-05 · 씬 폴링 할당 제거와 호출자 버퍼

2026-10-13. **Implemented / Windows Mono Editor Verified / 사용자 수락·병합 승인 완료**. branch `codex/imp-05-scene-polling`, base main `3ee358f94187a9b09ef9a34a5e14f08e9748ac84`, 검증한 source `0cc919b6c497f348deae3e6ae4dffb33f21c8482`. 최종 문서 HEAD·그 Actions는 작업 PR/완료 보고에서 확인한다.

## 선행 승인·공개 계약

사용자 PR #9 병합/완료후 재개 지시를 따랐다. [receipt](pr9-merge-receipt.json): 승인후 수락문서5개만 추가한 최종tip90df125의 Actions 성공 확인 → 일반merge main3ee358f → 로컬/origin/server 일치·원본tip 도달성 확인 → 해당helper branch를 exact remote SHA lease/로컬-d로 삭제. old 조사branch는 삭제하지 않았다. 신규meta5의 Git checkout CRLF→LF 정규화만 있었고 source13 canonical 내용/GUID·dirty6는 보존했다. 이번 IMP-05는 최신 main에서 새 브랜치를 만들었다.

버퍼 API의 사전 확인 절차를 요청했고 사용자가 다음3개를 그대로 확정했다: `int OwnedSceneCount { get; }`, `void GetOwnedScenes(List<Scene> buffer)`, `void GetRegisteredScenes(List<SceneRegistration> buffer)`. 이름·형식 변경 없이 구현했다. [runtime/XML](../../../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs), [사람 API](../../../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot), [AI API](../../../ai/api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot).

null은 ArgumentNullException, thread 위반은 InvalidOperationException이며 이 검증은 Clear 이전이다. 이후 Clear/fill 실패는 partial contents를 Clear하고 원래 예외를 전파한다. 최종 buffer는 empty, capacity 확장은 복원하지 않는다. metadata 읽기 예외를 실제 root probe로 주입해 primary1개가 채워진 뒤 실패·최종empty·원래exception object를 확인했다. worker는 정상 출력과 다른2개sentinel 전체가 그대로인지 확인해 늦은 thread 검사를 막는다. GetOwnedScenes의 Add OOM을 강제로 발생시키지는 않았으며 catch/clear 경로를 정적으로 확인했다.

Owned count/buffer는 valid&&loaded primary→derived→candidate, registered는 primary→derived(candidate 제외·추가loaded filter 없음)이다. snapshot membership/복사 flags는 다음 호출 전 그대로고 Scene handle의 native 조회는 후속 상태를 반영할 수 있다. caller capacity를 충분히 확보하면 bookkeeping은0events. 기존 두 속성은 allocating snapshot을 계속 제공한다. caller root의 IsPrepared 자체 할당/예외를0으로 숨기지 않는다.

## 현재 위치 대조·구현 경계

시작 main3ee358f의 GameSceneManager69/72/78–83/985–1007/1120/1200은 백로그와 일치했다. CanProceed를 호출자 buffer 방식으로 우회하지 않고 All/iterator/메서드 그룹을 index root 순회로, 기대 Concat/ToArray와 HashSet inventory를 Scene instance 양방향 membership으로 교체했다. 동적 expected는 retained+primary+derived+candidate를 **필터 없이** 포함하고 모든 실제loaded→expected / 모든expected→actual loaded를 확인한다. duplicate Set 의미와 missing/unloaded expected 거부를 보존한다. 기존 short-circuit·root 읽기 순서, CanProceed의 종료중등록준비검사 포함, P1 ValidatePreparedOwnership의 종료중 owner 제외는 유지했다.

ExpectedGameScenes는 CanProceed/ValidatePreparedOwnership 두 caller에만 있었고 제거했다. array inventory3caller는 유지하며 같은 membership 의미를 직접 순회한다. GetLoadedScenes는 저빈도 초기 snapshot/common validation/마지막씬 보호에서 그대로다. 기존 snapshot getter 본문·async command 본문·root/scene 수명·P1 다른LINQ·패키지·asmdef는 변경하지 않았다. class 분리/다른 IMP로 확장하지 않았다. LINQ 판단: P0는 직접 루프로 교체, 진단 snapshot과 이 작업 밖 P1/P2 순회는 원래대로 유지한다.

## 실제 Red → Green

Unity6000.3.18f1 / UTF1.6.0 / CLI·Connector0.4.1 / Windows Mono Editor의 기존 동일인스턴스. 모두 mandatory `tools/run_unity_tests.py --project <절대 checkout> --mode <mode> [--filter <exact fixture/method>] --output <JSON>` 실행이다. compile idle와 actual type load 뒤 실행했다. 공개 JSON은 개인 path/PID/runId를 생략하고 failure checkout prefix만 정리했으며 case 이름·수·원문 assertion·결과source/CLI코드는 유지했다.

| 단계 | 대상 | total | pass | fail | skip | 증거 |
|---|---|---:|---:|---:|---:|---|
| Red | 새 GameScenePollingTests | 13 | 6 | 7 | 0 | [Red](red-runs.json) |
| Red | 기존 CanProceed allocation | 1 | 0 | 1 | 0 | [Red](red-runs.json) |
| Green | 새 API/의미·allocation | 13 | 13 | 0 | 0 | [Green](green-runs.json) |
| Green | 기존 entry(CanProceed 포함) | 8 | 8 | 0 | 0 | [Green](green-runs.json) |
| Green | 기존 replacement | 36 | 36 | 0 | 0 | [Green](green-runs.json) |
| Green | 기존 derived areas | 27 | 27 | 0 | 0 | [Green](green-runs.json) |
| 회귀 | 전체 EditMode | 359 | 359 | 0 | 0 | [회귀](regression-runs.json) |
| 회귀 | 전체 PlayMode | 332 | 332 | 0 | 0 | [회귀](regression-runs.json) |

```text
Required public API: void GameSceneManager.GetOwnedScenes(List<Scene>)
  Expected: not null
  But was:  null
```

```text
CanProceedDoesNotAllocateInReadyState:   Expected: 0
  But was:  2300
```

API부재7개는 typed reflection binding으로 존재/void signature를 검사한 실제 NUnit 실패다. 새 API 없는 상태의 compile error를 Red라고 보지 않으며 모든 경계를 각각 행동 Red로 실행했다고 주장하지 않는다. 기존 의미6개는 baseline pass를 그대로 유지했다. compile 초기 requester 누락은 Red 동결 전에 기존 RemoveDerivedAsync(scene,requester) 계약으로 수정했고 검증한 fixture를 동결했다. 구현 단계에서 이 fixture/메타·기존CanProceed·GChelper6개를 수정하지 않았다. worker sentinel 보완도 리뷰 후 Red 이전에 끝냈다.

신규 테스트는 primary/derived/nested의 결과·순서·Clear·stale snapshot/flags, capacity8/1000회Count+두fill/합계9000·event0, 원래속성alloc, null/thread, partialfail empty, 준비중candidate owned2/registered1, external derived unload filter차이, 외부추가/retained삭제/active변경/derived준비상실/중복expected를 검증한다. 기존Single/additive/잔존실패/조건/loading도 전체 회귀에서 보존됐다.

## 구성별 기여도 실측

[이전](before-components.json), [이후](after-components.json), [probe 실행](probe-runs.json), [before source](ComponentProbe.cs.txt), [after source](OptimizedProbe.cs.txt), [drivers](ComponentProbeTests.cs.txt)/[after driver](OptimizedProbeTests.cs.txt). 독립 Ready primary-only Graph에서 각100회, binding/Emit/기대array생성/warm-up/assert/serialization은 window밖. 결과·payload는static에 보존하고 helper 각호출 양성1·음성0을 확인했다. 같은 기존 Editor PlayMode에서 temporary fixture를 mandatory runner로 실행했으며 실행 후 owned source/meta4개씩 제거했다. 임시코드는 Assets나병합source에 남지 않고 증거 .cs.txt만 보존한다.

| 경로(100회) | 전 events | 후 events | 전/후 전역 frame bytes(보조) |
|---|---:|---:|---:|
| 등록준비: original All-bearing IL proxy → 실제 index helper | 200 | 0 | 19200 / 0 |
| ExpectedGameScenes Concat/ToArray | 700 | 제거·새동적membership에 통합 | 37600 / 분리후측정 없음 |
| array InventoryMatches(prebuilt expected) | 1400 | 0 | 69200 / 0 |
| 새 동적 expected union+inventory | 별도경로없음 | 0 | 분리전측정 없음 / 0 |
| 실제 CanProceed 전체 | 2300 | 0 | 126000 / 0 |

All은 private 타입 때문에 실제 getter의 GetRegisteredOwners→ldnull→ldftn IsOwnerPrepared→new Func→Enumerable.All IL을 확인하고 그 순서만 DynamicMethod로 재현했다(`allSequenceVerified=true`, raw193B IL 보존). 원래 compiled block 자체를 분리 실행했다고 주장하지 않는다. Expected/Inventory/전체와 이후 helpers는 실제 bound 메서드다. 사전 구간은 각각독립window이며 이번 환경에서는200+700+1400=2300, bytes합계126000이 전체와 일치했다. 일반적인 가산성·모든scene graph별비중·Root getter 비용을 일반화하지 않는다. sample.Count 이벤트와 sample.Value/byte는 다른 단위이고 frame값은 전역보조다.

이후 API 독립1000회: Count/GetOwned/GetRegistered 각0events(보조0B). 이 probe는primary1개이며 permanent test는nested3개에서세API합산1000회0events로검증했다. legacy snapshot100회는 Owned600events/30800B, Registered400events/30000B로계속할당했다. CPU 시간 측정은하지 않았으며 membership복잡도 O(loaded×expected)인tradeoff를문서화했다.

## 보존·Console·문서·남은 gate

[보존](source-preservation.json): main557 source/assets/settings/tools 중Runtime1개만변경, 나머지556 rawhash동일. 사용자dirty6·frozen6·testedsource7동일, 신규meta2GUID유일, 임시probeAssets잔여0. [환경](environment.json): 원래InitScene active/loaded/dirty=false, Play/compile/import/TestRun=false, Profiler.enabled=false. [제품Console](console.json)10개는singleton6/provider2/root2의기존LogAssert와대조했고추가미예상/CS오류는관찰하지않았다. 처음stack문자열로집계하면서root 예외의MonoSingleton base stack을중복분류했으므로오류첫줄별로다시검증했다;Console을비우거나오류0으로표시하지않는다.

Green polling·entry와afterprobe CLI가 domainreload로1을반환했지만동일PID의fresh Connector 결과를복구했고exact nonzero case는모두pass였다. 실제실패case를retry하거나다른Editor/과거suite로대체하지않았다. 전체회귀691/691·skip0은 이번 source의결과다.

runtime/XML·사람/AI API·기능명세·백로그·회고/색인을같은단위에서갱신한다. 최종HEAD 규칙/Actions와PR은완료보고에연결하며GitHub Actions는Unity CI가아니다. 새source와기존pending변경을섞지않았다. 2026-10-13 사용자가 PR #10 병합을 승인했다. 실제 통합 뒤 다음 큐 IMP-03은 기존 gate와 실행 불가 시 중단 규칙을 따른다.

**NotRun:** 이번 Player/IL2CPP·다른Unity/플랫폼·새소비설치/Release·제품시각/UX·CPUbenchmark·buffer Add OOM주입·root getter에서재진입하는비정상사용. 과거Player결과로이번변경을입증하지않는다. 프로젝트Root getter자체의할당과현재Editor 계측 한계는 [helper검증](../../imp-14/README.md)을따른다.

## 사용자 수락 · 2026-10-13

사용자가 PR #10 병합을 승인했다. 승인 대상 tip은 `56c101fb282e7f2723ad6a06a253a741ccf47c4e`이며 [그 Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38335940767)의 완료·success를 다시 확인했다. 이 후속 변경은 승인 상태·회고·색인 문서5개뿐이다. 검증 source `0cc919b6c497f348deae3e6ae4dffb33f21c8482`, runtime·테스트·기존 dirty6는 유지하며 문서 최종 HEAD의 규칙/Actions 뒤 일반 merge로 원본 이력을 보존한다. 실제 merge SHA와 branch 정리는 PR/Git 이력 및 완료 보고에서 확인한다. 이 수락으로 Player/IL2CPP·CPU 등 NotRun이 통과로 바뀌지는 않는다.
