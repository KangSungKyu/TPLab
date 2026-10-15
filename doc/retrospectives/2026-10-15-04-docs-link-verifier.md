# 2026-10-15 · 04 · 문서 링크 정정과 verifier 원인 분류

**중단 항목: verifier17건의 해결.** 기존 도구는 현재 checkout을 과거 source·보호 입력 hash와 비교한다. 오래된 경로만 현재 이름으로 바꿔도 hash 불일치가 남아 문서·checks 데이터 정정만으로 유효한 통과를 만들 수 없다. 사용자 규칙에 따라 도구·판정 로직 수정과 과거 기대 hash 덮어쓰기를 하지 않고 결정 대기로 멈춘다.

T0 문서 전용이다. 기준 main/source는 `e661a79e3e487c46fb185190d5bc9e0ad8391375`, branch는 `codex/docs-link-verifier`다. 링크6건 수정과 읽기 전용 원인 조사·회고·PR까지만 수행한다. 작업1·2와 별도 PR이며 병합하지 않는다.

## 링크6건 전후

대상은 [AGENTS_CHANGE.md](../validation/ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md) 한 문서다. 저장소 루트용 경로가 깊은 문서의 상대 링크로 그대로 쓰여 생긴 오류다. 대상 두 문서는 현재 존재하므로 삭제 증거 permalink로 바꿀 경우가 아니다. 표시 텍스트·문장·수치는 그대로 두고 아래 target만 바꿨다.

| 기존 줄 | 표시 텍스트 | 기존 target | 새 target |
|---|---|---|---|
| 9 | CI_SETUP_GUIDE.md | `doc/CI_SETUP_GUIDE.md` | `../../../../../doc/CI_SETUP_GUIDE.md` |
| 13 | CI_SETUP_GUIDE.md | `doc/CI_SETUP_GUIDE.md` | `../../../../../doc/CI_SETUP_GUIDE.md` |
| 21 첫째 | GitHub 리뷰 자동화 초안 | `doc/CI_SETUP_GUIDE.md` | `../../../../../doc/CI_SETUP_GUIDE.md` |
| 21 둘째 | 현재 변경안 | `doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` | `../../../../../doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` |
| 25 첫째 | GitHub 리뷰 자동화 가이드 | `doc/CI_SETUP_GUIDE.md` | `../../../../../doc/CI_SETUP_GUIDE.md` |
| 25 둘째 | CI 판정 기준 | `doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` | `../../../../../doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` |

target를 역변환하면 기준 main의 원문 bytes와 같음을 확인했다. 보호 파일 AGENTS.md나 CI_SETUP_GUIDE 본문·CI_GATE_PROPOSAL 판정은 이 PR에서 변경하지 않는다.

## verifier 원인 분류

분모는 `tools/verify_validation.py --evidence <각 checks.json 폴더>`16회와 `doc/validation/bootstrap-system/verify.py`1회다. 실제 실행17회 모두 exit1이었다. 각 도구는 첫 실패에서 중단하므로 다음 표는 **현재 관측한 첫 실패 기준**이며 이후 검사가 모두 정상이라는 뜻이 아니다.

| 요청 분류 | 첫 실패 수 | 확인한 원인 |
|---|---:|---|
| 1. raw 증거 삭제로 인한 입력 누락 | 0 | 현재 필수 기계 입력165개가 모두 존재한다. manifest의 누락 경로 중 doc/validation 원시 자료는 없었다. 이후 미도달 검사 전체 성공은 주장하지 않는다. |
| 2. checks와 연결 manifest의 오래된 경로 | 11 | 당시 Assets/MyLab 경로를 현재 checkout에서 읽어 FileNotFoundError 발생 |
| 3. 도구 버그 | 0 | 현재 관측한 최초 실패에서 도구 자체의 잘못된 판정은 입증되지 않음 |
| 4. 기타 | 6 | 당시 source hash와 현재 ISceneRoot.cs hash 불일치. 이후 source가 변경된 경우이며 현재 runtime 결함을 뜻하지 않음 |

| evidence 폴더 | 분류 | 첫 실패 원문 요약 |
|---|---|---|
| doc/validation/game-scene-entry | 2 | FileNotFoundError: Assets/MyLab/Core/DataTables/CsvDataTable.cs |
| doc/validation/input-system/p4 | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-areas | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-definitions | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-integration | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-loaders | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-loading/p4 | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-replacement | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/scene-transition-contracts | 2 | FileNotFoundError: Assets/MyLab/Core/DataTables/CsvDataTable.cs |
| doc/validation/scene-transition-editor | 2 | FileNotFoundError: Assets/MyLab/Core.meta |
| doc/validation/tplab-naming | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/ui-system/p3 | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/ui-system/p4 | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/ui-system/p5 | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/ui-system/p6 | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/ui-system/p7 | 4 | AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs |
| doc/validation/bootstrap-system | 2 | FileNotFoundError: Assets/MyLab/Core/DataTables/CsvDataTable.cs |

필수 입력165개는16개 checks 폴더의 checks.json·test-inputs.json·preserved-inputs.json·final-console.json·final-status.txt·선언 results와 Bootstrap 입력7개다. 목록과 raw hash를 전수 비교해 이 PR에서 변경0임을 확인한다. Markdown 정정과 당시 실행 입력을 구분한다.

### 데이터 경로 정정만으로 해결되지 않는 근거

- [game-scene-entry manifest:5](../validation/game-scene-entry/test-inputs.json#L5)의 옛 CsvDataTable 경로를 이름만 Assets/TPLab으로 치환해 읽어도 expected `cef6591e94fb2cd62789d8ad6ab2322ce662196751e24c73e018fbb85b3bc586`와 실제 normalized SHA256 `5105b572e60b2e084cdfb18862d91986af00ac08f867c01601117e4dafba1b50`가 다르다.
- [tplab-naming manifest:25](../validation/tplab-naming/test-inputs.json#L25)의 ISceneRoot expected는 `2b7ea39c5ae91771d8c956cb0e762d060aa862ea2ea1b18d5521bdddbcb3d6dd`, 현재 normalized SHA256은 `763a2da7610f71d7c252e05abdd1d0276ca17bf508ad1ec4344d61278571eac7`다.
- 첫 실패 뒤에도11개 옛 경로 실행 모두 이름만 치환한 실제 파일의 hash 차이가 존재했다. 나머지6개에도 현재 존재하는 파일의 hash 차이가 있다. 경로·기대 hash를 현재값으로 바꾸면 과거 실행이 현재 source를 검증한 것처럼 오인하게 하므로 수정하지 않는다.
- 판정은 source manifest·사용자 보호 입력·native GUID·문서까지 포함한다. source SHA만 정해도 당시 dirty/비추적 bytes까지 Git에서 모두 확보된다는 보장은 없다.

## 도구 변경이 필요한 위치와 영향

아래는 설계 선택지 설명이며 구현하지 않았다. 기존 도구·checks JSON·workflow·검사 강도 변경0이다.

| 위치 | 필요한 변경 요지 | 영향·승인 경계 |
|---|---|---|
| [verify_validation.py:17](../../tools/verify_validation.py#L17) ·17/25/37~43 | 현재 checkout 고정 대신 evidence별 확정 source를 명시적으로 선택해 원본 bytes와 비교할지 설계 | 과거 source와 현재 검증을 분리해야 함. CI 판정 도구 여부와 무관하게 이번 요청의 도구 변경 금지 대상이므로 별도 승인 필요 |
| [같은 파일:44](../../tools/verify_validation.py#L44) ·44~60 | document·asset·GUID 검사도 선택한 source와 현재 checkout 중 어느 범위를 검증하는지 일관되게 정의 | source hash만 과거로 바꾸고 native 검사만 현재로 두는 혼합 판정을 피해야 함 |
| [Bootstrap verify.py:7](../validation/bootstrap-system/verify.py#L7) ·7/29~33/40~47 | 현재 ROOT 기준 hash와 Assets/MyLab 하드코딩을 역사 검증 범위에 맞게 처리 | 독립 verifier에도 같은 정책이 필요. 원시 입력·기대값의 임의 정정은 대체안이 아님 |

도구 버그를 확인했다는 뜻은 아니다. 과거 입력을 현재 checkout과 일치시켜 검사하는 기존 목적과 역사적 증거 읽기라는 새 목적이 다르다. 이번 문서에서 기능 추가·새 판정 기준을 확정하지 않는다.

## 사용자 결정 대기

| 선택지 | 장점 | 한계 |
|---|---|---|
| 과거 source별 검증을 별도 도구 작업으로 설계 | 당시 source와 기대 hash 의미를 보존하면서 검증 가능한 부분을 확인 | 도구·테스트·판정 범위 변경 승인이 필요하며 당시 dirty/비추적 입력 미확보는 별도 미검증으로 남음 |
| 기존17건을 과거 기록의 기준선 실패로 유지 | 도구·과거 데이터·검사 강도를 바꾸지 않음 | 현재 checkout에서 기존 verifier는 계속 실패. 최신 Unity 실행이나 source 검증과 분리해 보고해야 함 |

삭제 raw를 요구하는 첫 실패는0이므로 D-A 충돌을 이유로 검사 제거·요약 검증 대체를 임의로 수행하지 않는다. 향후 실제 삭제 raw 의존이 발견되면 그때 검사 제거·요약 검증·기준선 처리의 범위를 별도로 결정한다.

## 검증·보존·미실행

환경: Windows PowerShell·Python3.14. 실행은 작업 소유 `Temp/docs-close-20261015/check_docs.py --label <전후> --audit`, 기존 verifier17회, source·manifest 검색·SHA256 비교, `git diff --check <source> HEAD`와 `tools/check_pr_rules.py --base <source> --head HEAD`다. 원인 조사에서는 원래 verifier를 변경하거나 과거 입력을 덮어쓰지 않았다.

변경 전 상대 링크 오류6·anchor 오류0이다. 변경 후 링크·anchor 오류0을 요구하고, verifier17건의 exit/stdout/stderr를 기준선과 비교해 새 실패0·기존 실패17 유지 여부를 확인한다. 최초 실패 원문과 입력 검사는 일회성 Temp 출력에서 확인한 뒤 이 회고에 필요한 요약을 남겼다.

기존 추적1203파일과 dirty6 hash를 저장했다. 허용 문서 밖 bytes·필수 입력165개가 동일하고 runtime·테스트·asmdef·UPM·tools·workflow·checks JSON diff0임을 확인한다. 중단한 것은 verifier 해결 정책이며 링크 정정·회고·PR 제공·exact-SHA Actions 확인은 허용 범위에서 마무리한다. 실제 원시 자료 삭제와 source 재실행은 하지 않는다.

미실행: Unity·compile·제품 Console·Player/IL2CPP·GC 계측·과거 source 환경 재현·도구 변경·main 병합. verifier17건을 해결했다고 보고하지 않는다. 일회성 조사·검사·출력은 요약 보존 후 이번 작업 소유 Temp의 명시 목록만 정리하며, 다음 작업에는 이 회고의 명령과 중단 원인으로 재개할 수 있다.

## 문서 검사 확정

실제 변경 후 Markdown247개·상대 링크1953개·anchor481개를 검사했다. 링크 오류6→0·anchor 오류0→0이며 새 오류0이다. verifier17회의 exit/stdout/stderr는 기준선과 완전히 같아 기존17실패 유지·새 실패0이다. 허용 문서 밖 기존1201파일과 dirty6, 필수 기계 입력165개의 raw hash가 동일했다. 도구·CI·checks 데이터와 runtime·테스트·asmdef·UPM은 변경하지 않았다. verifier 해결은 위 정책 결정 대기로 중단한다.
