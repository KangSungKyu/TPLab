# IMP-04b — 테스트 assembly 기본 30초 적용

## 요청·기준·범위

2026-10-16 사용자는 PR #41 병합과 **테스트 assembly 기본 30초, 더 긴 시간이 필요한 테스트만 method별 조정**, 기존 취소·정리·대기 상한 보존을 승인했다. 위험등급은 **T2**다. 제품 API 변경은 없지만 테스트 실행의 제한 정책을 바꾸므로 임시 실제 Red → 동결 → 설정 적용 → Green → 전체 회귀로 확인했다. XML·사람/AI 제품 API는 변경 대상이 아니며 테스트 가이드·README·체크리스트의 D-I2만 함께 갱신한다.

PR #41은 tip `23d042711299dc88d3e99f4653b3f1fc501d8cd9`의 exact-SHA CI PASS를 확인한 후 일반 merge했다([규칙 검사](https://github.com/KangSungKyu/TPLab/actions/runs/38867096147)). 병합 main은 `6ec859a2112a3cdffc238caf248235aeade3b2e9`이며 local/remote main 일치를 확인했다. tip이 main에 포함되고 다른 worktree가 없음을 확인한 뒤 `codex/imp-04b-timeout-experiment` 원격 ref를 확인한 tip 조건으로 삭제하고 로컬을 `git branch -d`로 정리했다. 새 브랜치는 `codex/imp-04b-timeout-default`다. 이 후속 PR의 병합은 승인하지 않은 상태로 남긴다.

기존 사용자 dirty **12개(추적9·미추적3)**를 포함한 기존 **1,300파일**의 SHA-256을 시작 전에 보존했다. 원시 JSON/XML·실패·실행 명령·시간·임시 probe 소스 사본은 `Temp/imp-04b-apply-20261016/`에 보존하고 Git에는 넣지 않는다. 검토·병합 대기 중 재개에 필요하므로 유지하며, 검토 완료 후 이 요약을 유지한 채 해당 작업 소유 Temp만 정리할 수 있다.

## 적용과 보존

각 아래 폴더에 `AssemblyInfo.cs` 한 줄 `[assembly: NUnit.Framework.Timeout(30000)]`과 고유 `.meta`만 추가했다. asmdef/참조·package·runtime·기존 테스트 본문·SetUp/TearDown은 변경하지 않는다.

| assembly | 설정 파일 | 기준선 실행 case |
|---|---|---:|
| `TPLab.Input.EditModeTests` | [Assets/TPLab/Input/Tests/EditMode/AssemblyInfo.cs](../../Assets/TPLab/Input/Tests/EditMode/AssemblyInfo.cs) | 19 |
| `TPLab.Input.PlayModeTests` | [Assets/TPLab/Input/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/Input/Tests/PlayMode/AssemblyInfo.cs) | 18 |
| `TPLab.Netcode.EditModeTests` | [Assets/TPLab/Netcode/Tests/EditMode/AssemblyInfo.cs](../../Assets/TPLab/Netcode/Tests/EditMode/AssemblyInfo.cs) | 136 |
| `TPLab.Netcode.PlayModeTests` | [Assets/TPLab/Netcode/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/Netcode/Tests/PlayMode/AssemblyInfo.cs) | 34 |
| `TPLab.Netcode.Samples.PlayModeTests` | [Assets/TPLab/Samples/Netcode/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/Samples/Netcode/Tests/PlayMode/AssemblyInfo.cs) | 14 |
| `TPLab.Core.EditModeTests` | [Assets/TPLab/Tests/EditMode/AssemblyInfo.cs](../../Assets/TPLab/Tests/EditMode/AssemblyInfo.cs) | 326 |
| `TPLab.Core.PlayModeTests` | [Assets/TPLab/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/Tests/PlayMode/AssemblyInfo.cs) | 305 |
| `TPLab.UI.InputSystem.PlayModeTests` | [Assets/TPLab/UI/InputSystem/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/UI/InputSystem/Tests/PlayMode/AssemblyInfo.cs) | 6 |
| `TPLab.UI.EditModeTests` | [Assets/TPLab/UI/Tests/EditMode/AssemblyInfo.cs](../../Assets/TPLab/UI/Tests/EditMode/AssemblyInfo.cs) | 52 |
| `TPLab.UI.PlayModeTests` | [Assets/TPLab/UI/Tests/PlayMode/AssemblyInfo.cs](../../Assets/TPLab/UI/Tests/PlayMode/AssemblyInfo.cs) | 50 |

전체 EditMode 534건에는 외부 패키지 `Unity.Addressables.DocExampleCode.Editor.Tests.dll`의 1건이 포함된다. 위 TPLab EditMode 합계는 533건이며 외부 패키지 소스/캐시는 수정하지 않았다. PlayMode는 위 6개 assembly의 합427건이다.

테스트 선언이 없는 `TPLab.Core.TestFixtures`는 공유 helper이므로 제외했다. 그 assembly에만 설정해도 참조하는 테스트 assembly로 속성이 전파되지 않는다. runtime·Editor 도구·sample runtime·UPM도 제외한다. 기존 **141개 Timeout(테스트 메서드129·hook12), 모두30000ms**는 그대로 유지한다. 짧은 `.Timeout(...)`, worker Join, 유한 yield 대기, cancellation/CTS, gate finally 해제, ShutdownAsync 대기와 LogAssert 순서도 그대로다. Input/EventSystem fixture를 새로 만들거나 수정하지 않았다.

기준선 단일 test-case 최대값은 Edit0.971522초·Play0.690931초, 적용 후 두 회차 최대는 Edit0.960837초·Play0.744673초였다. 이번 환경에서는 30초보다 긴 method 예외가 필요하다는 근거가 없어 **추가 예외0**이다. 이는 다른 환경의 실행 시간 보장이 아니다.

## 환경·실행·Red 동결

Unity6000.3.18f1 / Windows Mono Editor(PID3428), UTF1.6.0 / NUnit3.5.0.0 / UniTask2.5.11 / CLI0.4.1 / Python3.14. 기존 Editor의 idle·compile/update=false를 실제 exec로 확인했다. 다른 Editor·Player로 대체하지 않았으며 별도 outer timeout으로 실행을 끊지 않았다. 기존 runner와 같은 callback의 XML recorder만 임시 추가했다.

```text
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/imp-04b-apply-20261016/<run>.json
python -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/imp-04b-apply-20261016/<run>.json
# 임시 probe의 대상 실행에는 --filter TPLab.TimeoutDefaultProbe 추가
```

각 assembly에 임시 `EffectiveTimeoutProbe.UnannotatedTestUsesThirtySecondAssemblyDefault` 1개씩 두고 실행 context의 TestCaseTimeout을 읽었다. 기본값이 없는 정상본에서 아래 **10건(Edit4·Play6) 모두 같은 이유로 실패**했다. test body의 값 불일치이며 SetUp/TearDown 실패가 아님을 확인하고 probe10개 hash를 동결했다. 이후 probe 변경 없이 assembly 설정만 추가해 동일10건 모두 통과했다. 임시 테스트는 최종 변경이나 영구 case 수에 포함하지 않는다.

```text
Unannotated tests must inherit the approved 30-second assembly default.
Expected: 30000
But was:  180000
```

| 단계 | EditMode | PlayMode | 판정 |
|---|---:|---:|---|
| 최초 필터 `TimeoutDefaultProbe` | total0 | 미실행 | namespace가 아닌 필터로0건 선택. runner가 `No nonzero result for this invocation`으로 거부. Red·통과로 세지 않음 |
| 전체 namespace로 실제 Red | 0/4, fail4 | 0/6, fail6 | 위 기대값30000/실제180000, 예상 밖 실패0 |
| 설정 적용 후 Green | 4/4 | 6/6 | 실패0·skip0, 동결된 probe 그대로 |

최초0건 실행의 XML·driver 원문을 별도 보존했고 기존 테스트/fixture 정정은 없다. 실행 context 관찰은 새10개 assembly 설정 적용의 검증이다. 30초 실제 만료나 우선순위 행렬을 이번에 다시 실행한 것은 아니며, cooperative timeout·method override 동작의 선행 근거는 [PR #41 실험](https://github.com/KangSungKyu/TPLab/blob/23d042711299dc88d3e99f4653b3f1fc501d8cd9/doc/retrospectives/2026-10-16-06-imp-04b-timeout-experiment.md#실험-구성과-관찰-결과)이다.

## 전체 회귀·반복

임시 probe를 모두 제거하고 설정을 남긴 상태로 전체 회귀를 두 번 수행했다. baseline·1회차·2회차의 **test fullname 집합이 mode별 완전히 동일**하며 신규/삭제/skip0이다. 임시 XML recorder는 최종 반복 후 제거하고 다시 컴파일했다.

| run | pass/total | fail/skip | XML suite초 | CLI wall초 |
|---|---:|---:|---:|---:|
| baseline-edit | 534/534 | 0/0 | 27.536 | 29.999 |
| full-edit-1 | 534/534 | 0/0 | 27.504 | 29.986 |
| full-edit-2 | 534/534 | 0/0 | 26.988 | 28.470 |
| baseline-play | 427/427 | 0/0 | 14.158 | 25.499 |
| full-play-1 | 427/427 | 0/0 | 14.392 | 26.483 |
| full-play-2 | 427/427 | 0/0 | 14.528 | 27.609 |

wall은 CLI 준비·Play 진입/복귀를 포함하며 suite시간과 구분한다. `full-play-1`은 CLI domain reload 단절 뒤 runner가 **동일 CLI PID55688**의 Connector 결과를 회수했다. 다른 실행·재시도로 실패를 덮지 않았다. 전후 테스트 결과는 모두 정상이고 새로운 기능 실패는 없었다. 이후 코드/테스트 변경 없이 문서만 정리한다.

## 검토·한계·미실행

재사용 agent `/root/netcode_p2_review`(gpt-6.1-sol/high)가 읽기 전용으로 assembly10개 일치·중복0, metaGUID, 기존1,300파일 hash와 Red/Green 원문을 대조했다. timeout/cleanup 경계의 검토라 high를 유지했고 부모만 파일/Git/Unity를 조작했다.

Timeout은 underlying 작업 취소·finally·정리 완료를 보장하지 않는다. 기존 owner 취소·gate 해제·정리 완료 대기를 대체하지 않았으며 30초는 suite 전체가 아닌 개별 테스트 제한이다. 프레임 기반 검사라 정확히30초에 강제 중단한다고 주장하지 않는다. CPU blocking/native yield/UnityTearDown 자체 hang 중단, Player/IL2CPP·다른 플랫폼·새 GC 측정·소비 설치는 미실행이다. 기존 GC 테스트가 전체 회귀에 포함된 것과 새 GC 계측 실험은 구분한다.

## 문서·보존·CI 검증

D-I2 사용자 결정과 적용 사실은 [05](../guidelines/05-testing.md#양성-대조-범위와-미확정-사항)·[README](../guidelines/README.md#사용자-결정)·[CHECKLIST](../guidelines/CHECKLIST.md)에 반영한다. 기존 가이드의 역사적 source·선언 집계는 유지한다. 라벨 집계의 fence·집계표를 제외하고 각 문장/표 행의 첫 라벨만 세어 README 표를 갱신했다.

최종 검사·Actions는 확인한 source와 실제 결과를 이 절에 추가한다. 이 회고 자체를 포함하는 최종 commit ID는 PR commit 이력과 완료 보고에서 확인하며 이전 SHA의 CI를 최종 SHA 검사로 대체하지 않는다.

링크/anchor 검사: 변경 전267문서·local2197/anchor499 → 변경 후268문서·local2217/anchor502, 오류0→0. HTTP 전체 검사는 미실행이다. 일반 verifier16개와 Bootstrap verifier1개는 전후 **17→17 역사적 실패**이며 exit code·stdout·stderr 원문이 각각 동일했다. 최초 verifier 호출의 누락된 `--evidence` 인자 오류는 실행 대상 판정으로 세지 않고 올바른16개 경로와 별도Bootstrap으로 확인했다. 도구·checks·기대 해시는 변경하지 않는다.

가이드 라벨 집계(현행/결정됨/제안)는05가19/4/1→21/4/0, README가25/28/1→27/28/0이며 다른 장은 동일하다. 작업 소유 임시11개 `.cs/.meta`와 UTF 임시 씬은 최종 Assets에 남지 않았고, 기존 baseline 파일 중 변경은 승인된 가이드3개·회고INDEX뿐이다. 나머지1,296파일과 사용자dirty12개는 byte동일, runtime·기존 테스트·asmdef·package·UPM·tools·workflow 변경0이다. 최종refresh compile완료, Editor3428 idle, error Console조회 `[]`를 확인했으나 별도의 제품씬 UX검증은 미실행이다. 문서작성 임시 스크립트의 기본인코딩(cp949) 읽기 오류는 UTF-8로 정정했으며 테스트 결과·기존 파일은 바꾸지 않았다.
