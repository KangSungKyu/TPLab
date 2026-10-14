# 2026-10-14 · 06 · 배포 원시 자료 삭제와 permalink 전환

T0 검증·결정 기록이다. 사용자 2026-10-14 13:15 요청 A를 완료한 뒤 B를 최신 main에서 시작했다. 기준 main은 `73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67`, 브랜치는 `codex/validation-prune-distribution`이다. B는 PR 제공까지만 진행하며 사용자 병합 승인을 기다린다. UIContext 특성화 테스트는 별도 브랜치·PR에서 진행하고 이 PR에는 추가하지 않는다.

## A · PR #19 통합

지정 HEAD `99723eb467adf0a1032b3ef29a0abb7504687881`, main `5b08d90e3976d6f9faa16d68895ab663ad6a0133`와 exact-SHA [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38509028601) success를 새로 확인했다. targetSha/inspectedCommit, collection 전부 complete·queryIssues/blockingReasons0, run/check completed/success 확인 후 일반 merge했다. 병합 SHA·로컬/원격 main은 `73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67`이다.

로컬/원격 `codex/validation-prune-ui-system`을 정리했다. 삭제 전 tip은 위 HEAD다. main 도달성·tip 일치·다른 worktree 미사용을 확인하고 예상 원격 tip을 조건으로 삭제한 뒤 로컬 `git branch -d`를 사용했다. 기존 dirty6개와 추적 파일1,316개 원시 바이트를 보존했다. 원본 커밋은 main 이력에서 조회 가능하다.

## B · 대상과 보존

측정함: `git ls-tree -rlz <base>`의 Git blob 크기다. 지정6영역 전체145파일·3,714,475바이트로 이전145파일·약3.7MB와 일치한다. 허용 확장자 원시139파일·3,693,660바이트를 삭제하고 Markdown6파일·20,815바이트를 보존한다. 종류별 삭제 파일 수는 .json 96, .txt 27, .xml 16다. 허용 확장자 밖 파일은 이6영역에0개다.

| 영역 | 삭제 파일 | 삭제 바이트 | 보존 파일 | 보존 바이트 |
|---|---:|---:|---:|---:|
| distribution-candidate | 56 | 3,007,710 | 1 | 5,459 |
| distribution-consumer | 52 | 546,209 | 1 | 6,233 |
| distribution-packaging | 12 | 101,188 | 1 | 2,853 |
| distribution-release | 11 | 32,703 | 1 | 2,501 |
| distribution-design | 5 | 3,528 | 1 | 2,496 |
| distribution-refinement | 3 | 2,322 | 1 | 1,273 |
| 합계 | 139 | 3,693,660 | 6 | 20,815 |

generic158+Bootstrap7=전체 직접 입력165개와 선언 hash payload836경로·실존490파일을 보존한다. 후보139개와 direct/hash payload/documents/assets 선언 교집합은 모두0이다. 이 배포 영역의 직접 입력은0개다. standalone `preserved-inputs.json`3개는 해당 폴더의 checks.json 또는 현행 도구가 읽는 입력으로 연결돼 있지 않아 원시 후보에 포함한다. 입력 manifests의 내용과 hash는 수정하지 않는다.

각6영역의 README만 보존하고 `upm/`·소스·테스트·tools·workflow는 수정하지 않는다. 기존 링크 오류·verifier 실패·범위 밖 자료도 수정하지 않는다.

## 배포 파이프라인 의존·고정 출력 조사

고정 SHA의 source·consumer templates·문서·UPM 인용을 읽고 입력과 생성 출력을 구분했다. **현재 raw 입력 의존(d)0, 같은 기존 파일 고정 출력0, 추가 보존 raw0**이다.

| 대상 | 코드로 확인한 입력/출력 경계 |
|---|---|
| `tools/build_distribution.py:66` | 소스/API Markdown/라이선스/버전 allowlist. validation raw는 입력에 없음 |
| `build_distribution.py:233-239` | 검증하는 입력은 upm 배포 사본. 이를 수정하거나 재생성하지 않음 |
| `build_distribution.py:215-216,255-256` | 출력은 source/tplab/고유run-id, 기존 output 거부; manifest/checksum은 그 artifacts 아래 |
| `tools/run_core_consumer.py:96-100` | artifact 입력은 project/tplab/**/artifacts. 동명 validation manifest는 자동 입력으로 선택하지 않음 |
| `run_core_consumer.py:329-333` | evidence 디렉터리는 기존 distribution-consumer를 사용할 수 있지만 파일은 timestamp+PID이며 동일 파일 존재를 거부 |
| `tools/test_build_distribution.py:25-34` | 임시 fixture 복사 범위가 한정돼 validation raw를 읽지 않음 |
| `tools/test_distribution_consumer.py:11-23,66,104` | 임시 artifacts 생성. validation 디렉터리는 경로 거부 검사 인자이며 기존 raw 읽기 아님 |
| `doc/DISTRIBUTION_PIPELINE.md:118`·`tools/README.md:83` | 새 output/artifacts·evidence 디렉터리 명령. 기존 raw의 고정 출력 없음 |
| consumer templates·upm | 후보 raw의 literal 입력·링크0. UPM 직접 수정0 |

고정 basename인 `distribution-manifest.json`과 같은 기존 validation 경로를 혼동하지 않았다. pipeline에 필요하다면 해당 파일만 제외하고 나머지를 진행하라는 이번 사용자 조건에 따라 조사했으며 실제 제외할 의존 파일은0개였다.

## 참조 분류·permalink

측정함: 고정 SHA의 `git cat-file --batch`·`git grep`와 slash/backslash·상대/절대·URL 정규화로 전수 분류했다.

| 분류 | 결과 | 처리 |
|---|---|---|
| (a) Markdown raw 링크 | 88표현·10문서·83대상 파일 | 표시 텍스트·문장·수치 유지, target만 SHA permalink |
| 디렉터리 링크 | 삭제로 비는 tree 링크0 | 6개 README가 남아 상위 디렉터리 유지 |
| (b) 링크 밖 Markdown의 후보 파일 경로 | 0 | 변경0 |
| (c) raw 출처 | 교차36쌍+자기1=37쌍·source9개 | 원시 문자열 변경0, 모든source도함께삭제 |
| (d) 현재 raw 입력 의존 | 0 | 추가 제외0 |

독립 최초 교차 출처35집계에 부모 전수검사가 `distribution-release/release-validation.json:3 → public-release.json`1개를 추가했다. 원문으로 대조해 교차36을 확정한다. `owned-cleanup.json`의 source는 과거 worktree distribution-consumer/p3-final2 경로이며 현재 candidate 파일과 혼동하지 않았다. 같은 행의 preserved 경로만 실제 후보로 집계한다. 잔존 raw source의 새 끊긴 출처 문자열은0개다.

permalink 기준 SHA는 `73a12f6ac4c4bd9f45db6ca9d14f94cbc8fc4e67`다. 파일은 `https://github.com/KangSungKyu/TPLab/blob/<SHA>/<repository-path>`, 디렉터리는 tree를 사용한다. 후보139개가 이 SHA에 blob으로 존재함을 전수 확인했다. HTTP HEAD는6개 영역 표본6/6 HTTP200이며 나머지83대상 전부 HTTP검사했다고 주장하지 않는다.

| 문서 | 삭제 전 기준 줄(표현별) | 전환 수 |
|---|---|---:|
| `doc/DISTRIBUTION_PIPELINE.md` | 3 | 1 |
| `doc/retrospectives/2026-10-07-15-distribution-design.md` | 9 | 1 |
| `doc/retrospectives/2026-10-08-02-distribution-release.md` | 13, 15 | 2 |
| `doc/validation/distribution-candidate/README.md` | 3, 3, 3, 3, 5, 5, 5, 9, 9, 10, 10, 11, 11, 11, 15, 15, 15, 25, 26, 27, 28, 29, 30, 31, 32, 36, 36 | 27 |
| `doc/validation/distribution-consumer/README.md` | 3, 3, 3, 3, 3, 7, 8, 9, 10, 11, 12, 13, 14, 16, 20, 24, 24, 24, 24, 24, 24, 24, 24, 26, 26, 26, 26, 28 | 28 |
| `doc/validation/distribution-design/README.md` | 5, 7, 9, 11, 11 | 5 |
| `doc/validation/distribution-packaging/README.md` | 5, 5, 5, 5, 5, 7, 9, 9, 9, 9, 9 | 11 |
| `doc/validation/distribution-refinement/README.md` | 5, 5, 7 | 3 |
| `doc/validation/distribution-release/README.md` | 3, 3, 3, 7, 9, 9, 9, 13 | 8 |
| `doc/validation/imp-04-survey/README.md` | 263, 263 | 2 |

각 문서의 원본 Git blob에 위 target 교체만 적용한 예상 바이트와 실제 결과를 비교해 표시 텍스트·본문·수치 변경0을 확인한다.

## 원시 출처 전수 목록

source9개도 모두삭제된다. 원문 문자열을 변경하지 않고 삭제 전 base에서 조회할 위치를 기록한다.

| source | 줄 | target | 분류 |
|---|---:|---|---|
| `doc/validation/distribution-candidate/final-matrix.json` | 10 | `doc/validation/distribution-candidate/core-consumer-20261008T010952Z-2044.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 10 | `doc/validation/distribution-candidate/core-consumer-20261008T011939Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 18 | `doc/validation/distribution-candidate/core-consumer-20261008T012343Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 26 | `doc/validation/distribution-candidate/core-consumer-20261008T012450Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 34 | `doc/validation/distribution-candidate/core-consumer-20261008T012613Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 42 | `doc/validation/distribution-candidate/core-consumer-20261008T012727Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 50 | `doc/validation/distribution-candidate/core-consumer-20261008T013037Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 58 | `doc/validation/distribution-candidate/core-consumer-20261008T013143Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 66 | `doc/validation/distribution-candidate/core-consumer-20261008T013454Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 5 | `doc/validation/distribution-candidate/core-consumer-20261008T011939Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 10 | `doc/validation/distribution-candidate/core-consumer-20261008T012343Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 15 | `doc/validation/distribution-candidate/core-consumer-20261008T012450Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 20 | `doc/validation/distribution-candidate/core-consumer-20261008T012613Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 25 | `doc/validation/distribution-candidate/core-consumer-20261008T012727Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 30 | `doc/validation/distribution-candidate/core-consumer-20261008T013037Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 35 | `doc/validation/distribution-candidate/core-consumer-20261008T013143Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 40 | `doc/validation/distribution-candidate/core-consumer-20261008T013454Z-29272.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-c.json` | 12 | `doc/validation/distribution-consumer/core-consumer-20261007T092003Z-28540.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-d.json` | 12 | `doc/validation/distribution-consumer/core-consumer-20261007T092606Z-6576.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-d.json` | 20 | `doc/validation/distribution-consumer/core-consumer-20261007T092946Z-6576.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-d.json` | 28 | `doc/validation/distribution-consumer/core-consumer-20261007T093107Z-6576.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-d.json` | 36 | `doc/validation/distribution-consumer/core-consumer-20261007T093240Z-6576.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-d.json` | 44 | `doc/validation/distribution-consumer/core-consumer-20261007T093407Z-6576.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 12 | `doc/validation/distribution-consumer/core-consumer-20261007T093824Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 20 | `doc/validation/distribution-consumer/core-consumer-20261007T094205Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 28 | `doc/validation/distribution-consumer/core-consumer-20261007T094328Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 36 | `doc/validation/distribution-consumer/core-consumer-20261007T094503Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 44 | `doc/validation/distribution-consumer/core-consumer-20261007T094629Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 52 | `doc/validation/distribution-consumer/core-consumer-20261007T095005Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 60 | `doc/validation/distribution-consumer/core-consumer-20261007T095127Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-consumer/matrix-e.json` | 68 | `doc/validation/distribution-consumer/core-consumer-20261007T095301Z-30376.json` | 교차 출처 |
| `doc/validation/distribution-refinement/static-checks.json` | 24 | `doc/validation/distribution-refinement/preserved-inputs.json` | 교차 출처 |
| `doc/validation/distribution-refinement/static-checks.json` | 25 | `doc/validation/distribution-refinement/static-checks.json` | 자기 참조 |
| `doc/validation/distribution-refinement/static-checks.json` | 26 | `doc/validation/distribution-refinement/design-ci.json` | 교차 출처 |
| `doc/validation/distribution-release/cleanup-preparation.json` | 7 | `doc/validation/distribution-release/core-consumer-20261008T014425Z-37332.json` | 교차 출처 |
| `doc/validation/distribution-release/release-validation.json` | 3 | `doc/validation/distribution-release/public-release.json` | 교차 출처 |
| `doc/validation/distribution-release/release-validation.json` | 21 | `doc/validation/distribution-release/core-consumer-20261008T014425Z-37332.json` | 교차 출처 |

## 검증 요약

환경은 Windows 로컬 Git checkout/Python3.14다. 이 B는 T0이므로 Unity/Player/IL2CPP 실행은 미실행이며 별도 UI 테스트 작업의 baseline을 이 정리의 Unity 검증으로 사용하지 않는다.

삭제 전/후에 inline Python 읽기 전용 Markdown/anchor 검사, `tools/verify_validation.py --evidence <각 checks.json 폴더>`16회 + `doc/validation/bootstrap-system/verify.py`1회를 실행한다. 직접입력165와실존payload490의원시hash도비교한다.

| 검사 | 전 | 후 | 판정 |
|---|---:|---:|---|
| 기존 링크 오류 | 6 | 6 | 새 오류0 |
| anchor | 445 | 445 | 새 오류0 |
| verifier 실행/실패 | 17/17 | 17/17 | 각 exit code·stdout·stderr 동일, 새 실패0 |
| 직접 입력 hash | 165개 | 165개 | 동일 |
| hash 선언과 삭제 교집합 | 0 | 0 | 실존payload490보존 |
| dirty | 6개 | 6개 | 경로·원시hash동일 |
| 범위 밖 추적 파일 | 기존rawhash | 동일 | 무관한변경0 |

기존링크6은 `ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md`의 과거상대링크다. verifier 첫실패는 구 `Assets/MyLab/Core/DataTables/CsvDataTable.cs`누락3, `Assets/MyLab/Core.meta`누락8, `Assets/TPLab/Core/Lifecycle/ISceneRoot.cs`hash불일치6이다. 기존17실패는고치지않는다. 첫오류에서중단하므로 역사검증전체재실행성공이아니라 입력보존과 동일첫실패원문을확인한범위다.

배포 도구는 아래 명령을 삭제 전/후 같은 환경에서 실행한다. 임시 fixture만 사용하며 실제 archive 배포·새 소비 Unity 프로젝트·Release를 만들지 않는다.

| 명령 | 삭제 전 | 삭제 후 |
|---|---|---|
| `C:\Python314\python.exe tools/test_build_distribution.py -v` | 19통과·실패0·skip0 | 19통과·실패0·skip0 |
| `C:\Python314\python.exe tools/test_distribution_consumer.py -v` | 13통과·실패0·skip1(total14) | 13통과·실패0·skip1(total14) |
| `C:\Python314\python.exe tools/run_core_consumer.py --self-check` | PASS | PASS |

skip은 `test_symlink_output_rejected`의 `OS does not permit directory symlinks`다. skip을통과로표현하지않는다. 전후 비교는 각 case이름/결과·skip이유와 self-check 결과를 비교하며 실행시간은달라질수있으므로원문전체동일로주장하지않는다.

`git diff --check <base> <HEAD>`·`tools/check_pr_rules.py --base <base> --head <HEAD>`·최종HEAD exact-SHA Actions는아래완료상태에기록한다. 사용자dirty의기존공백문제는승인diff검사에섞지않는다.

완료 상태: 변경source commit `72e7324c9a01f689bcde286ddb3247120b638940`에서 배포19통과·소비13통과/1skip·self-check PASS의전후case결과동일, 규칙오류0·경고0·scoped공백오류0, exact-SHA [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38512164363) run1/check1 completed/success·모든collectioncomplete·queryIssues/blockingReasons0을확인했다. 독립diff감사에서도핵심누락·잘못된주장0이었다. 이완료문장갱신뒤최종tip의링크·규칙·exact-SHA gate를다시확인한다. 최종metadata는 [PR #20 Checks](https://github.com/KangSungKyu/TPLab/pull/20/checks)에대응하며위검증source와구별한다. PR은open으로유지하고병합하지않는다. 증거요약은회고한곳에두고PR/색인은상태와링크만남긴다.

## 삭제 파일 전체 목록

Git blob 바이트다. `git rm -- <명시파일>`만 사용했다. 폴더와일드카드·reset·clean·stash·이력재작성은하지않았다. source이력은위base에서복원가능하다.

| 파일 | 바이트 |
|---|---:|
| `doc/validation/distribution-candidate/SHA256SUMS.txt` | 276 |
| `doc/validation/distribution-candidate/candidate-EditMode.xml` | 234,696 |
| `doc/validation/distribution-candidate/candidate-PlayMode-fixture-cleanup.json` | 1,163 |
| `doc/validation/distribution-candidate/candidate-PlayMode-fixture-cleanup.xml` | 211,874 |
| `doc/validation/distribution-candidate/candidate-PlayMode-graphics.json` | 3,046 |
| `doc/validation/distribution-candidate/candidate-PlayMode-graphics.xml` | 226,117 |
| `doc/validation/distribution-candidate/candidate-fixture-EditMode.xml` | 233,458 |
| `doc/validation/distribution-candidate/candidate-fixture-PlayMode.xml` | 240,898 |
| `doc/validation/distribution-candidate/candidate-full-regression-fixture.json` | 5,783 |
| `doc/validation/distribution-candidate/candidate-full-regression-green.json` | 4,617 |
| `doc/validation/distribution-candidate/candidate-full-regression.json` | 1,625 |
| `doc/validation/distribution-candidate/candidate-green-EditMode.xml` | 233,431 |
| `doc/validation/distribution-candidate/candidate-green-PlayMode.xml` | 226,107 |
| `doc/validation/distribution-candidate/core-consumer-20261008T010952Z-2044.json` | 11,571 |
| `doc/validation/distribution-candidate/core-consumer-20261008T011939Z-29272.json` | 23,877 |
| `doc/validation/distribution-candidate/core-consumer-20261008T012343Z-29272.json` | 8,459 |
| `doc/validation/distribution-candidate/core-consumer-20261008T012450Z-29272.json` | 9,984 |
| `doc/validation/distribution-candidate/core-consumer-20261008T012613Z-29272.json` | 12,929 |
| `doc/validation/distribution-candidate/core-consumer-20261008T012727Z-29272.json` | 23,920 |
| `doc/validation/distribution-candidate/core-consumer-20261008T013037Z-29272.json` | 8,479 |
| `doc/validation/distribution-candidate/core-consumer-20261008T013143Z-29272.json` | 9,988 |
| `doc/validation/distribution-candidate/core-consumer-20261008T013454Z-29272.json` | 12,934 |
| `doc/validation/distribution-candidate/distribution-manifest.json` | 84,036 |
| `doc/validation/distribution-candidate/documentation-check.json` | 240 |
| `doc/validation/distribution-candidate/engine-normalization-final2.json` | 2,994 |
| `doc/validation/distribution-candidate/engine-normalization.json` | 2,994 |
| `doc/validation/distribution-candidate/final-EditMode.xml` | 233,445 |
| `doc/validation/distribution-candidate/final-PlayMode.xml` | 211,872 |
| `doc/validation/distribution-candidate/final-SHA256SUMS.txt` | 276 |
| `doc/validation/distribution-candidate/final-distribution-manifest.json` | 84,036 |
| `doc/validation/distribution-candidate/final-full-regression.json` | 2,498 |
| `doc/validation/distribution-candidate/final-matrix.json` | 429 |
| `doc/validation/distribution-candidate/final-source-ci.json` | 359 |
| `doc/validation/distribution-candidate/final2-EditMode.xml` | 234,135 |
| `doc/validation/distribution-candidate/final2-PlayMode.xml` | 211,882 |
| `doc/validation/distribution-candidate/final2-SHA256SUMS.txt` | 276 |
| `doc/validation/distribution-candidate/final2-distribution-manifest.json` | 84,036 |
| `doc/validation/distribution-candidate/final2-full-regression.json` | 2,504 |
| `doc/validation/distribution-candidate/final2-matrix.json` | 1,678 |
| `doc/validation/distribution-candidate/final2-reproduction-protection.json` | 1,999 |
| `doc/validation/distribution-candidate/final2-source-ci.json` | 359 |
| `doc/validation/distribution-candidate/long-path-green.json` | 1,014 |
| `doc/validation/distribution-candidate/long-path-green.xml` | 3,717 |
| `doc/validation/distribution-candidate/long-path-red.json` | 1,533 |
| `doc/validation/distribution-candidate/long-path-red.xml` | 7,322 |
| `doc/validation/distribution-candidate/manifest-lock-red.json` | 1,270 |
| `doc/validation/distribution-candidate/manifest-lock-red.xml` | 6,415 |
| `doc/validation/distribution-candidate/original-console.json` | 9,814 |
| `doc/validation/distribution-candidate/original-edit.json` | 27,501 |
| `doc/validation/distribution-candidate/original-play.json` | 25,738 |
| `doc/validation/distribution-candidate/owned-cleanup.json` | 3,204 |
| `doc/validation/distribution-candidate/python-contracts.json` | 275 |
| `doc/validation/distribution-candidate/python-final2.txt` | 4,320 |
| `doc/validation/distribution-candidate/snapshot-reproduction.json` | 1,119 |
| `doc/validation/distribution-candidate/source-ci.json` | 359 |
| `doc/validation/distribution-candidate/temporary-files-cleanup.json` | 8,829 |
| `doc/validation/distribution-consumer/SHA256SUMS-d.txt` | 276 |
| `doc/validation/distribution-consumer/SHA256SUMS-e.txt` | 276 |
| `doc/validation/distribution-consumer/archive-reproduction-d.json` | 883 |
| `doc/validation/distribution-consumer/archive-reproduction-e.json` | 808 |
| `doc/validation/distribution-consumer/canonical-samples-e.json` | 11,594 |
| `doc/validation/distribution-consumer/cleanup-log-inventory.json` | 63,031 |
| `doc/validation/distribution-consumer/core-consumer-20261007T090853Z-33284.json` | 4,567 |
| `doc/validation/distribution-consumer/core-consumer-20261007T091444Z-22920.json` | 13,707 |
| `doc/validation/distribution-consumer/core-consumer-20261007T092003Z-28540.json` | 17,700 |
| `doc/validation/distribution-consumer/core-consumer-20261007T092606Z-6576.json` | 23,603 |
| `doc/validation/distribution-consumer/core-consumer-20261007T092946Z-6576.json` | 8,359 |
| `doc/validation/distribution-consumer/core-consumer-20261007T093107Z-6576.json` | 9,842 |
| `doc/validation/distribution-consumer/core-consumer-20261007T093240Z-6576.json` | 12,784 |
| `doc/validation/distribution-consumer/core-consumer-20261007T093407Z-6576.json` | 6,161 |
| `doc/validation/distribution-consumer/core-consumer-20261007T093824Z-30376.json` | 23,583 |
| `doc/validation/distribution-consumer/core-consumer-20261007T094205Z-30376.json` | 8,349 |
| `doc/validation/distribution-consumer/core-consumer-20261007T094328Z-30376.json` | 9,853 |
| `doc/validation/distribution-consumer/core-consumer-20261007T094503Z-30376.json` | 12,800 |
| `doc/validation/distribution-consumer/core-consumer-20261007T094629Z-30376.json` | 23,602 |
| `doc/validation/distribution-consumer/core-consumer-20261007T095005Z-30376.json` | 8,359 |
| `doc/validation/distribution-consumer/core-consumer-20261007T095127Z-30376.json` | 9,841 |
| `doc/validation/distribution-consumer/core-consumer-20261007T095301Z-30376.json` | 12,786 |
| `doc/validation/distribution-consumer/distribution-manifest-d.json` | 84,036 |
| `doc/validation/distribution-consumer/distribution-manifest-e.json` | 84,036 |
| `doc/validation/distribution-consumer/dll-gate-green.txt` | 1,808 |
| `doc/validation/distribution-consumer/dll-gate-red.txt` | 2,450 |
| `doc/validation/distribution-consumer/first-consumer-failure.txt` | 371 |
| `doc/validation/distribution-consumer/git-metadata-green.txt` | 2,134 |
| `doc/validation/distribution-consumer/git-metadata-observation.json` | 296 |
| `doc/validation/distribution-consumer/git-metadata-red.txt` | 2,924 |
| `doc/validation/distribution-consumer/matrix-c.json` | 522 |
| `doc/validation/distribution-consumer/matrix-d.json` | 1,224 |
| `doc/validation/distribution-consumer/matrix-e.json` | 1,720 |
| `doc/validation/distribution-consumer/path-green-process.json` | 543 |
| `doc/validation/distribution-consumer/path-green.xml` | 13,959 |
| `doc/validation/distribution-consumer/path-red-process.json` | 533 |
| `doc/validation/distribution-consumer/path-red.xml` | 30,075 |
| `doc/validation/distribution-consumer/python-final-committed.txt` | 4,320 |
| `doc/validation/distribution-consumer/python-green-final.txt` | 1,668 |
| `doc/validation/distribution-consumer/python-green-first.txt` | 1,271 |
| `doc/validation/distribution-consumer/python-green-phases.txt` | 1,808 |
| `doc/validation/distribution-consumer/python-red.txt` | 5,520 |
| `doc/validation/distribution-consumer/sample-guid-preservation.json` | 7,010 |
| `doc/validation/distribution-consumer/sample-temp-observation.json` | 418 |
| `doc/validation/distribution-consumer/samples-packaging-green.txt` | 2,285 |
| `doc/validation/distribution-consumer/samples-packaging-red.txt` | 2,937 |
| `doc/validation/distribution-consumer/source-ci-e.json` | 359 |
| `doc/validation/distribution-consumer/static-c.json` | 1,585 |
| `doc/validation/distribution-consumer/summary-e.json` | 2,429 |
| `doc/validation/distribution-consumer/upm-metadata-green.txt` | 1,980 |
| `doc/validation/distribution-consumer/upm-metadata-observation.json` | 492 |
| `doc/validation/distribution-consumer/upm-metadata-red.txt` | 2,732 |
| `doc/validation/distribution-packaging/SHA256SUMS.txt` | 276 |
| `doc/validation/distribution-packaging/distribution-manifest.json` | 59,246 |
| `doc/validation/distribution-packaging/generation-checks.json` | 2,152 |
| `doc/validation/distribution-packaging/green-committed.txt` | 1,864 |
| `doc/validation/distribution-packaging/green-final.txt` | 1,864 |
| `doc/validation/distribution-packaging/green-first.txt` | 10,496 |
| `doc/validation/distribution-packaging/green-second.txt` | 3,127 |
| `doc/validation/distribution-packaging/green-third.txt` | 1,864 |
| `doc/validation/distribution-packaging/preserved-inputs.json` | 674 |
| `doc/validation/distribution-packaging/red.txt` | 18,889 |
| `doc/validation/distribution-packaging/source-ci.json` | 359 |
| `doc/validation/distribution-packaging/static-checks.json` | 377 |
| `doc/validation/distribution-release/before-integration.json` | 1,294 |
| `doc/validation/distribution-release/branch-cleanup.json` | 1,360 |
| `doc/validation/distribution-release/cleanup-preparation.json` | 604 |
| `doc/validation/distribution-release/core-consumer-20261008T014425Z-37332.json` | 23,670 |
| `doc/validation/distribution-release/documentation-check.json` | 204 |
| `doc/validation/distribution-release/draft-assets.json` | 1,097 |
| `doc/validation/distribution-release/integration.json` | 250 |
| `doc/validation/distribution-release/original-editor-compile.json` | 313 |
| `doc/validation/distribution-release/public-release.json` | 2,607 |
| `doc/validation/distribution-release/release-validation.json` | 945 |
| `doc/validation/distribution-release/source-ci.json` | 359 |
| `doc/validation/distribution-design/design-ci.json` | 359 |
| `doc/validation/distribution-design/preserved-inputs.json` | 674 |
| `doc/validation/distribution-design/public-preflight.json` | 598 |
| `doc/validation/distribution-design/public-status.json` | 540 |
| `doc/validation/distribution-design/static-checks.json` | 1,357 |
| `doc/validation/distribution-refinement/design-ci.json` | 359 |
| `doc/validation/distribution-refinement/preserved-inputs.json` | 674 |
| `doc/validation/distribution-refinement/static-checks.json` | 1,289 |

## 보존 파일 전체 목록

| 파일 | 바이트 | 이유 |
|---|---:|---|
| `doc/validation/distribution-candidate/README.md` | 5,459 | Markdown 보존 |
| `doc/validation/distribution-consumer/README.md` | 6,233 | Markdown 보존 |
| `doc/validation/distribution-packaging/README.md` | 2,853 | Markdown 보존 |
| `doc/validation/distribution-release/README.md` | 2,501 | Markdown 보존 |
| `doc/validation/distribution-design/README.md` | 2,496 | Markdown 보존 |
| `doc/validation/distribution-refinement/README.md` | 1,273 | Markdown 보존 |

허용확장자밖파일과추가고정출력파일은0개이며 코드·tools·UPM은전체원시hash로보존한다.

## 회고·협업·미실행

동명manifest의validation사본과현재artifact입력을구별하고consumer의고정evidence디렉터리와timestamp파일명을구별했다. 독립출처집계와부모전수검사의1건차이는원문으로대조했다. read-only 하위agent `/root/factcheck_upm`을 기존설정변경없이재사용해의존/출력/참조를독립감사했다. 허용범위는source/test/docs/Git읽기만이며Unity/파일/Git변경금지다. 부모가B변경·Git·검증·회고를담당한다.

기존문서누락·링크6·verifier17은수정하지않는다. 배포실제build/install/Player/Release및Claude수동리뷰는미실행이다. 작업용출력은 `Temp/validation-prune-distribution-20261014`에만두며Git에추가하지않고최종비교후이번작업이만든명시목록만정리한다. 다른작업Temp·사용자자산·dirty는보존한다.
