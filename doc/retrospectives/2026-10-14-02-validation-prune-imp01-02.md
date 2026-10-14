# 2026-10-14 · 02 · IMP-01·02 원시 자료 삭제와 permalink 전환

T0 검증·결정 기록이다. 기준 main은 `a3c4e00d4af950e4e482c2774d912b70b512dba7`, 작업 브랜치는 `codex/validation-prune-imp01-02`다. 첨부의 두 요청 중 이 단위만 저장소를 수정한다. 별도 nonvalidation 조사는 같은 main을 고정한 읽기 전용 채팅 보고이며 이 PR에 조사 파일을 추가하지 않는다. 현재 승인 범위는 삭제·링크 전환·검증·PR 생성까지이며 PR은 open으로 두고 병합 승인을 기다린다. PR #15의 조건부 승인을 이 작업에 재사용하지 않는다.

## 대상과 측정

`git ls-tree -rlz a3c4e00d4af950e4e482c2774d912b70b512dba7 -- doc/validation/imp-01 doc/validation/imp-02`의 Git blob bytes 기준이다. Windows checkout 줄끝 크기나 Git pack 회수량과 구분한다.

| 단위 | 삭제 파일 | Git bytes | 보존 파일 |
|---|---:|---:|---|
| IMP-01 | 12 | 258,656 | README.md |
| IMP-02 | 6 | 170,748 | README.md |
| 합계 | 18 | 429,404 | Markdown 요약 2개 |

| 삭제 경로 | Git bytes | 종류 |
|---|---:|---|
| `doc/validation/imp-01/baseline-boundary-runs.json` | 2,237 | JSON |
| `doc/validation/imp-01/baseline-clone-provenance.json` | 2,995 | JSON |
| `doc/validation/imp-01/boundary-green-runs.json` | 4,797 | JSON |
| `doc/validation/imp-01/environment.json` | 10,040 | JSON |
| `doc/validation/imp-01/followup-environment.json` | 9,867 | JSON |
| `doc/validation/imp-01/followup-preservation.json` | 8,512 | JSON |
| `doc/validation/imp-01/followup-regression-runs.json` | 69,923 | JSON |
| `doc/validation/imp-01/green-runs.json` | 8,127 | JSON |
| `doc/validation/imp-01/red-results.json` | 2,852 | JSON |
| `doc/validation/imp-01/regression-runs.json` | 69,577 | JSON |
| `doc/validation/imp-01/source-preservation.json` | 68,409 | JSON |
| `doc/validation/imp-01/zero-selection.json` | 1,320 | JSON |
| `doc/validation/imp-02/environment.json` | 9,991 | JSON |
| `doc/validation/imp-02/green-runs.json` | 9,631 | JSON |
| `doc/validation/imp-02/pr7-merge-receipt.json` | 383 | JSON |
| `doc/validation/imp-02/red-runs.json` | 6,113 | JSON |
| `doc/validation/imp-02/regression-runs.json` | 71,970 | JSON |
| `doc/validation/imp-02/source-preservation.json` | 72,660 | JSON |

현행 generic 직접 입력 158 + Bootstrap 7 = 165파일은 두 단위와 교집합 0이며 전부 보존한다. 추가 보존 예외나 대상에서 제외한 원시 파일은 없다. `git rm`은 위 18파일만 대상으로 했고, 두 폴더는 README가 있어 남는다. 소스·테스트·tools·upm·workflow·다른 IMP 자료는 변경하지 않는다.

## 참조 분류와 원문 보존

측정은 기준 main 추적 2,200파일 중 텍스트 2,186파일의 `git cat-file --batch` 조회와 `rg` 파일명/경로 확인이다. NUL을 포함한 PNG/DLL 14개는 텍스트 검사에서 제외했다. 전체·suffix·부모 상대 경로 및 GitHub URL, 단일/이중 역슬래시, basename 문맥을 구분했다. basename이 같지만 다른 단위인 파일을 삭제 대상의 참조로 세지 않았다.

| 분류 | IMP-01 | IMP-02 | 의미 |
|---|---:|---:|---|
| 단위 참조 Markdown | 10 | 9 | 보존하는 README에 대한 참조도 포함. 이전 조사와 일치. |
| (a) 원시 Markdown 링크 표현 | 15 | 8 | 문서 3개 합계 23개, 대상 12/6파일. 이전 조사와 일치. |
| (b) 링크 밖 원시 파일 경로 | 0 | 0 | 추가 본문/코드 경로 전환 없음. |
| (c) 원시 파일을 가리키는 출처 문자열 | 0 | 0 | 삭제 뒤 남는 끊긴 원시 출처 문자열 없음. |
| (d) tools/workflow/테스트/소스/UPM 참조 | 0 | 0 | 실행 입력 의존 없음. |

원시 JSON에서 발견한 6행은 `imp-01/followup-preservation.json:93/94/96`과 `imp-02/source-preservation.json:578/579/580`의 **보존하는 README 링크**다. 삭제 대상 원시 파일의 출처가 아니며 이 문자열을 고치지 않고 파일과 함께 이력에 남겼다. JSON·LOG 등 원시 내용의 수정이나 출처 복원은 하지 않았다.

## 링크 전환

아래는 삭제 전 main의 줄이다. 링크 텍스트·기존 본문·수치·문장은 그대로 두고 (a)의 대상만 고정 permalink로 바꿨다. 예외는 사용자 요청에 따른 과거 열람/로컬 재검증 구분 안내 한 문단을 IMP-01 README 맨 끝에 추가한 것이다. IMP-02는 그 공통 안내의 적용 범위에 포함한다.

| 변경 문서 | 원래 줄 | 유지한 링크 텍스트 | 대상 전환 |
|---|---:|---|---|
| `doc/retrospectives/2026-10-12-11-imp-02-try-return.md` | 5 | `[PR7 통합]` | `../validation/imp-02/pr7-merge-receipt.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/pr7-merge-receipt.json` |
| `doc/validation/imp-01/README.md` | 35 | `[Red]` | `red-results.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/red-results.json` |
| `doc/validation/imp-01/README.md` | 36 | `[집중 실행]` | `green-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/green-runs.json` |
| `doc/validation/imp-01/README.md` | 37 | `[집중 실행]` | `green-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/green-runs.json` |
| `doc/validation/imp-01/README.md` | 38 | `[집중 실행]` | `green-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/green-runs.json` |
| `doc/validation/imp-01/README.md` | 39 | `[회귀]` | `regression-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/regression-runs.json` |
| `doc/validation/imp-01/README.md` | 40 | `[회귀]` | `regression-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/regression-runs.json` |
| `doc/validation/imp-01/README.md` | 59 | `[환경·제품 Console]` | `environment.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/environment.json` |
| `doc/validation/imp-01/README.md` | 61 | `[입력 보존]` | `source-preservation.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/source-preservation.json` |
| `doc/validation/imp-01/README.md` | 86 | `[후속 보존]` | `followup-preservation.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/followup-preservation.json` |
| `doc/validation/imp-01/README.md` | 92 | `[복제 근거]` | `baseline-clone-provenance.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/baseline-clone-provenance.json` |
| `doc/validation/imp-01/README.md` | 108 | `[baseline 실행]` | `baseline-boundary-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/baseline-boundary-runs.json` |
| `doc/validation/imp-01/README.md` | 108 | `[현재 Green]` | `boundary-green-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/boundary-green-runs.json` |
| `doc/validation/imp-01/README.md` | 114 | `[0건 선택 기록]` | `zero-selection.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/zero-selection.json` |
| `doc/validation/imp-01/README.md` | 116 | `[최종 전체 회귀]` | `followup-regression-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/followup-regression-runs.json` |
| `doc/validation/imp-01/README.md` | 118 | `[최종 환경]` | `followup-environment.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01/followup-environment.json` |
| `doc/validation/imp-02/README.md` | 13 | `[통합 증거]` | `pr7-merge-receipt.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/pr7-merge-receipt.json` |
| `doc/validation/imp-02/README.md` | 49 | `[실제 Red]` | `red-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/red-runs.json` |
| `doc/validation/imp-02/README.md` | 63 | `[Red]` | `red-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/red-runs.json` |
| `doc/validation/imp-02/README.md` | 63 | `[Green]` | `green-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/green-runs.json` |
| `doc/validation/imp-02/README.md` | 63 | `[전체 회귀]` | `regression-runs.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/regression-runs.json` |
| `doc/validation/imp-02/README.md` | 75 | `[환경/Console]` | `environment.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/environment.json` |
| `doc/validation/imp-02/README.md` | 77 | `[입력 보존]` | `source-preservation.json` → `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02/source-preservation.json` |

18개 고유 대상 모두 `git cat-file -e a3c4e00d4af950e4e482c2774d912b70b512dba7:<path>`로 존재를 확인했고, 각 `https://github.com/KangSungKyu/TPLab/blob/a3c4e00d4af950e4e482c2774d912b70b512dba7/<path>`에 HTTP HEAD를 보내 **18/18 status 200**을 확인했다. permalink는 과거 읽기용이며 로컬 입력 복원·재검증 능력을 뜻하지 않는다. [삭제 전 IMP-01](https://github.com/KangSungKyu/TPLab/tree/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-01)과 [삭제 전 IMP-02](https://github.com/KangSungKyu/TPLab/tree/a3c4e00d4af950e4e482c2774d912b70b512dba7/doc/validation/imp-02)에서 원문을 열람할 수 있다. Git 이력 재작성·강제 푸시·브랜치 삭제는 하지 않는다.

## 검사와 한계

삭제 전 기준: 추적 Markdown 236개, 상대 링크 2,651개, anchor 445개, 기존 상대 링크 오류 6건·anchor 오류 0건이다. 같은 stdlib 조사로 fenced/inline code를 제외하고 상대·참조형 링크, 존재 여부, GFM heading/HTML id/line anchor를 검사한다. 삭제 후 회고/색인을 포함해 Markdown 237개·상대 링크 2,630개·anchor 445개, 동일 기존 오류 6건·새 오류 0건이었다. verifier 17회도 exit/stdout/stderr가 전후 모두 동일해 새 실패 0건이다. 범위 밖 추적 2,178파일·dirty 6개·직접 입력 165개의 raw hash를 보존했고, 링크를 역변환하면 기존 3문서의 문장·수치·라벨이 삭제 전 Git 원문과 일치했다.

`C:\Python314\python.exe tools/verify_validation.py --evidence doc/validation/<root>`를 checks.json이 있는 16개 root에서, `C:\Python314\python.exe doc/validation/bootstrap-system/verify.py`를 Bootstrap에서 실제 읽기 전용으로 실행한다. 기존 도구를 수정하지 않는다. 삭제 전 17회는 기존 실패 17개였으며 구형 `Assets/MyLab/Core/DataTables/CsvDataTable.cs` 부재 3개, `Assets/MyLab/Core.meta` 부재 8개, `AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs` 과거 hash 불일치 6개다. 부재 실패 원문은 `FileNotFoundError: [Errno 2] No such file or directory`다. 전후 exit/stdout/stderr를 비교하고 새 실패를 분리한다. 첫 기존 실패 뒤의 모든 과거 단언까지 재실행했다고 주장하지 않는다.

`tools/check_pr_rules.py --base a3c4e00d4af950e4e482c2774d912b70b512dba7 --head <최종 tip>`, scoped `git diff --check`, 변경 허용 목록·라벨/문장 보존·기존 INDEX prefix 및 dirty 6개·범위 밖 파일 raw hash와 verifier 직접 입력 165개를 검사한다. 최종 HEAD Actions는 `tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <최종 tip> --output Temp/validation-prune-imp01-02-20261014/final-ci.json`로 exact-SHA·조회 완전성·completed/success를 판정한다. [브랜치 Actions](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Fvalidation-prune-imp01-02)의 최종 tip과 완료 보고를 대조한다. 회고 자신을 포함한 최종 SHA는 Git/PR 이력과 완료 보고에서 확인한다.

기존 읽기 감사 담당 `/root/factcheck_upm`을 gpt-6.1-sol/medium 기존 설정으로 재사용해 독립 nonvalidation 조사를 맡겼다. 해당 담당은 고정 main의 Git/rg 조회만 수행하며 저장소를 수정하지 않는다. 부모는 이 PR의 범위/링크/검증·Git 작업을 소유한다. 추가 파일 생성은 이 회고와 [회고 색인](INDEX.md)의 한 행뿐이다. 일회성 조회 출력은 이번 작업의 Temp 폴더에 두고 필요한 요약을 여기 남긴 뒤 작업 파일만 정리한다.

미실행: Unity Edit/Play·컴파일·제품 Console·Player/IL2CPP·독립 Claude workflow·main 병합·브랜치 삭제. 런타임·테스트·API 변경 없는 T0 원시 자료 정리다. 다른 문서의 낡은 상태·이전 실행/GC 수치·verifier 경로/hash·기존 6개 링크 오류는 발견 사항으로만 남기고 고치지 않았다. 다음 삭제 단위는 사용자가 정한다.

검증 커밋 `6bbeccbd06032e18259344015c1819298382c87c`에서 PR 규칙 검사 오류 0·경고 0·exit 0을 확인했다. [Actions 실행](https://github.com/KangSungKyu/TPLab/actions/runs/38499038816) 1개/check 1개는 completed/success였고 모든 collection complete, queryIssues/blockingReasons 0이었다. 결과를 보완한 최종 문서 tip도 같은 exact-SHA gate로 다시 확인하며 그 SHA는 완료 보고/PR에서 대조한다. PR #16은 open이며 main 병합은 하지 않았다.
