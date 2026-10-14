# 2026-10-14 · 05 · UI 원시 자료 삭제와 permalink 전환

T0 검증·결정 기록이다. 사용자 2026-10-14 12:29 요청 A를 완료한 뒤 B를 최신 main에서 시작했다. 기준 main은 `5b08d90e3976d6f9faa16d68895ab663ad6a0133`, 브랜치는 `codex/validation-prune-ui-system`이다. PR 제공까지만 진행하고 main 병합은 사용자 승인을 기다린다. 책임·테스트 계획 조사는 별도 읽기 전용으로 수행해 채팅에만 보고하며 runtime·테스트 변경은 없다.

## A · PR #18 통합

사용자 지정 HEAD `8c06f4394eb627de64e00c70ebd538e99257a0ed`, main `4704dc0387d5e22ad8eae745386dcc8a03e78680`와 exact-SHA [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38505627969) success를 새로 조회했다. targetSha/inspectedCommit, 모든 조회 collection complete·queryIssues/blockingReasons 0, run/check completed/success를 확인하고 일반 merge했다.

병합 SHA·로컬 main·origin/main·서버 main은 `5b08d90e3976d6f9faa16d68895ab663ad6a0133`으로 일치한다. 로컬/원격 `codex/validation-prune-group1`의 삭제 전 tip은 위 HEAD다. main 도달성·tip 일치·다른 worktree 미사용을 확인하고 원격 예상 tip을 조건으로 삭제한 뒤 로컬 `git branch -d`로 정리했다. 기존 dirty6개와 추적 파일1,719개 원시 바이트를 보존했다.

## B · 대상과 보존 목록 확정

측정함: `git ls-tree -rlz <base>`의 Git blob 크기로 전체 UI456파일·39,152,841바이트를 재측정했다. 이전456파일·약39MB 집계와 설명 불가한 차이는 없다. 삭제는 JSON·LOG·XML·TXT·PNG만404파일·38,717,532바이트다. 종류별 파일 수는 .json 243, .log 62, .png 9, .txt 90. 보존은52파일·435,309바이트다: verifier 직접35 + Markdown11 + .gitattributes6. `distribution-*`·`imp-*`는 이번 삭제 밖이다.

| 하위 영역 | 삭제 파일 | 삭제 바이트 | 보존 파일 | 보존 바이트 | 직접 입력 |
|---|---:|---:|---:|---:|---:|
| compile-registry-20261012 | 30 | 190,110 | 2 | 4,111 | 0 |
| integration-20261012 | 9 | 14,262 | 2 | 3,093 | 0 |
| p0 | 2 | 1,168 | 1 | 1,471 | 0 |
| p1 | 19 | 117,701 | 1 | 4,234 | 0 |
| p2 | 11 | 106,016 | 1 | 3,248 | 0 |
| p3 | 19 | 170,240 | 8 | 59,421 | 7 |
| p4 | 17 | 127,638 | 8 | 66,327 | 7 |
| p5 | 23 | 26,729 | 8 | 68,400 | 7 |
| p6 | 49 | 125,148 | 8 | 76,663 | 7 |
| p7 | 189 | 36,795,119 | 11 | 144,845 | 7 |
| sample-clear-20261012 | 36 | 1,043,401 | 2 | 3,496 | 0 |
| 합계 | 404 | 38,717,532 | 52 | 435,309 | 35 |

전체 generic 직접 입력158 + Bootstrap7 =165개의 원시 SHA-256을 전후 비교한다. UI p3~p7의 직접 입력35개를 아래처럼 보존했다. 각 `checks.results` 선언 결과 파일을 포함한다.

| checks 폴더 | 직접 파일 수 | 보존 파일 |
|---|---:|---|
| `doc/validation/ui-system/p3/checks.json` | 7 | `checks.json`, `final-console.json`, `final-edit.json`, `final-play.json`, `final-status.txt`, `preserved-inputs.json`, `test-inputs.json` |
| `doc/validation/ui-system/p4/checks.json` | 7 | `checks.json`, `final-console.json`, `final-edit.json`, `final-play.json`, `final-status.txt`, `preserved-inputs.json`, `test-inputs.json` |
| `doc/validation/ui-system/p5/checks.json` | 7 | `checks.json`, `final-console.json`, `final-edit.json`, `final-play.json`, `final-status.txt`, `preserved-inputs.json`, `test-inputs.json` |
| `doc/validation/ui-system/p6/checks.json` | 7 | `checks.json`, `final-console.json`, `final-edit.json`, `final-play.json`, `final-status.txt`, `preserved-inputs.json`, `test-inputs.json` |
| `doc/validation/ui-system/p7/checks.json` | 7 | `checks.json`, `final-console.json`, `final-edit.json`, `final-regression/play.json`, `final-status.txt`, `preserved-inputs.json`, `test-inputs.json` |

UI `checks.documents`의 고유 문서 27개도 모두 실존하며 삭제 대상 밖이다. 링크 변경은 텍스트·문장·수치가 아니라 대상만 바꾼다.

| 선언 문서 | 상태 |
|---|---|
| `Assets/TPLab/Samples/UI/README.md` | 보존 |
| `README.md` | 보존 |
| `doc/CORE_PLAN.md` | 보존 |
| `doc/GAME_UI_SYSTEM.md` | 보존 |
| `doc/GAME_UI_SYSTEM_DRAFT.md` | 보존 |
| `doc/GAME_UI_SYSTEM_TRACK.md` | 보존 |
| `doc/INDEX.md` | 보존 |
| `doc/UI_ACCEPTANCE.md` | 보존 |
| `doc/UI_PERFORMANCE.md` | 보존 |
| `doc/ai/README.md` | 보존 |
| `doc/ai/api/README.md` | 보존 |
| `doc/ai/api/UI.md` | 보존 |
| `doc/api/README.md` | 보존 |
| `doc/api/UI.md` | 보존 |
| `doc/retrospectives/2026-10-08-07-ui-presentation.md` | 보존 |
| `doc/retrospectives/2026-10-08-08-ui-input.md` | 보존 |
| `doc/retrospectives/2026-10-08-09-ui-virtual-scroll.md` | 보존 |
| `doc/retrospectives/2026-10-08-10-ui-installation.md` | 보존 |
| `doc/retrospectives/2026-10-08-11-ui-validation.md` | 보존 |
| `doc/retrospectives/INDEX.md` | 보존 |
| `doc/validation/ui-system/p3/README.md` | 보존 |
| `doc/validation/ui-system/p4/README.md` | 보존 |
| `doc/validation/ui-system/p5/README.md` | 보존 |
| `doc/validation/ui-system/p6/README.md` | 보존 |
| `doc/validation/ui-system/p7/README.md` | 보존 |
| `tools/README.md` | 보존 |
| `tools/ui-consumer/README.md` | 보존 |

전체 `checks.documents`는69경로이며 실존68개를 보존한다. 기존 `Assets/MyLab/Samples/SceneTransitions/README.md` 누락1개는 이번 작업으로 생긴 오류가 아니므로 고치지 않았다. 전체 hash payload 선언836경로 중 실존490개를 보존하며 삭제 대상과의 교집합은0이다. 입력 manifests의 내용·hash 값은 수정하지 않는다. 이전 hash 값과 현재 source의 불일치는 기존 verifier 실패로 남는다.

## 고정 출력·현재 의존 검사

추가로 보존할 동일 기존 고정 출력은0개다. 독립 읽기 전용 감사에서도 같은 결론이었다. `tools/run_ui_source_consumer.py:83-99`는 새로운 직속child만 허용하고 existing 경로를 :96-97에서 거부한다. :370에서 검사한 뒤 :384-385에 생성하므로 :467/:500/:509/:556 읽기는 그 fresh evidence의 이번 실행 결과다. 기존 tracked raw 파일을 입력으로 사용하거나 같은 기존 경로를 재사용하지 않는다.

| 경로/도구 | 판정 | 근거 |
|---|---|---|
| `doc/validation/ui-system/p7/<fresh-child>/consumer-report.json` 및 build/smoke/scroll/Canvas/sample 결과·로그 | 새 호출의 동적 출력; 기존404개에 대한 추가 고정 출력 보존0 | 현재 runner의 fresh 경로 gate와 전달 인자 |
| `tools/ui-consumer/README.md:14-21` 명령 | `consumer-base-<fresh-id>` placeholder | 기존 tracked 경로를 지정하지 않음 |
| consumer template 출력 | 환경변수/serialized 인자 | 기존404개를 가리키는 literal 입력 없음 |
| p3~p7 final 결과·console·status | verifier 직접35에 포함 | 추가 출력 보존과 중복 집계하지 않음 |

현재 source/tool/workflow/테스트/UPM의 새 후보 입력 의존(d)은0개이며, 체크의 asset 선언과도 후보 교집합0이다. 새 패키지·도구·workflow·runtime·테스트·UPM 사본 수정은 없다. `Assets/TPLab/Samples/UI/README.md`와 `tools/README.md`는 요청한 Markdown 링크 대상만 바꾼다.

## 참조 분류와 permalink

측정함: 고정 SHA의 `git cat-file --batch`와 `git grep` 및 경로 토큰 분석으로 slash/backslash·상대/절대·URL·원시 basename 문맥을 구분했다. 동명 다른 폴더의 파일을 교차 참조로 세지 않았다.

| 분류 | 결과 | 처리 |
|---|---|---|
| (a) Markdown 파일 링크 | 237표현·26문서·대상162파일 | 표시 텍스트·문장·수치·anchor 유지, target만 permalink |
| 디렉터리 링크 | 이번 삭제로 비는 tree 링크0 | 추가 tree 전환0 |
| (b) 링크 밖 Markdown 경로 | 1곳 | 원문 유지 |
| (c) raw 내부 경로 | 자기6 포함177쌍·source35개; 자기 제외171쌍·source29개 | 원문 수정0; source도 삭제되는30개/보존되는5개를 구분 |
| (d) 새 현재 읽기 의존 | 0 | 추가 입력 제외/중단 대상 없음 |

(b)는 `tools/ui-consumer/README.md:54`의 `doc/validation/ui-system/p7/promoted-helper-green.txt`다. 링크 밖 역사적 경로 표기이므로 그대로 둔다. 삭제 후 현재 트리에서 열리지 않는 문자열이 남는 점을 기록한다.

고정 permalink 기준은 `5b08d90e3976d6f9faa16d68895ab663ad6a0133`다. 파일 URL은 `https://github.com/KangSungKyu/TPLab/blob/<SHA>/<repository-path>`이며 디렉터리는 `tree`를 사용한다. 이번 대상404개가 해당 SHA의 blob으로 존재하는지 전수 확인했다. HTTP HEAD는11개 하위 영역 표본11/11 HTTP200이다. 모든162개 링크 대상의 HTTP 검사를 실행했다고 주장하지 않는다.

기존 1차 삭제 회고의 역사 URL3개는 old SHA와 이번 base에서 target blob OID가 동일하고 #L4/#L9/#L16 범위가 유효한지 확인한 뒤 target SHA만 통일했다. 표시 텍스트와 원문은 바꾸지 않는다.

| 문서 | 삭제 전 기준 줄(표현별) | 전환 수 |
|---|---|---:|
| `Assets/TPLab/Samples/UI/README.md` | 66, 66, 66, 66, 66, 66, 66, 66 | 8 |
| `README.md` | 90, 90, 90 | 3 |
| `doc/GAME_UI_SYSTEM.md` | 122, 122, 124 | 3 |
| `doc/GAME_UI_SYSTEM_TRACK.md` | 8, 38, 42 | 3 |
| `doc/UI_PERFORMANCE.md` | 3, 5, 5, 5, 5, 5, 5 | 7 |
| `doc/ai/README.md` | 26, 26, 26 | 3 |
| `doc/ai/api/README.md` | 20, 20, 20, 20 | 4 |
| `doc/ai/api/UI.md` | 90, 112, 112, 128, 128, 128, 128, 128, 128, 128, 128 | 11 |
| `doc/api/README.md` | 20, 20, 20, 20, 20, 20, 20 | 7 |
| `doc/api/UI.md` | 5, 5, 5, 5, 5, 11, 11, 11, 11, 11, 11, 11, 11, 133 | 14 |
| `doc/retrospectives/2026-10-08-08-ui-input.md` | 9, 9 | 2 |
| `doc/retrospectives/2026-10-08-09-ui-virtual-scroll.md` | 20, 20, 21, 21, 23 | 5 |
| `doc/retrospectives/2026-10-12-03-ui-main-integration.md` | 8 | 1 |
| `doc/retrospectives/2026-10-14-01-validation-prune-batch1.md` | 37, 38, 40 | 3 |
| `doc/validation/ui-system/compile-registry-20261012/README.md` | 7, 7, 7, 19, 19, 19, 21, 21, 21, 21, 25, 25, 26, 26, 27, 27, 27 | 17 |
| `doc/validation/ui-system/integration-20261012/README.md` | 5, 5, 5, 7, 7, 7, 7, 7, 9, 11, 11, 11 | 12 |
| `doc/validation/ui-system/p0/README.md` | 5, 6 | 2 |
| `doc/validation/ui-system/p1/README.md` | 11, 12, 14, 14, 28, 29, 30, 32, 32, 34, 34, 34, 34, 36, 36, 36, 40, 40 | 18 |
| `doc/validation/ui-system/p2/README.md` | 15, 15, 15, 15, 23, 24, 26, 30 | 8 |
| `doc/validation/ui-system/p3/README.md` | 5, 7, 7, 7, 9, 9, 9, 9, 11, 11, 11, 11, 13, 13, 13 | 15 |
| `doc/validation/ui-system/p4/README.md` | 7, 7, 7, 7, 8, 8, 8, 9, 16, 16, 16, 20, 24 | 13 |
| `doc/validation/ui-system/p5/README.md` | 14, 14, 14, 14, 18, 18, 22, 22, 22, 22, 24 | 11 |
| `doc/validation/ui-system/p6/README.md` | 5, 5, 5, 7, 7, 7, 11, 17, 17, 19 | 10 |
| `doc/validation/ui-system/p7/README.md` | 7, 8, 16, 16, 16, 17, 19, 19, 19, 20, 21, 21, 22, 22, 23, 23, 24, 24, 25, 25, 25, 27, 27, 38, 38, 38, 38, 38, 38, 42, 42, 42, 42, 44, 45, 46, 47, 47, 48, 60, 60 | 41 |
| `doc/validation/ui-system/sample-clear-20261012/README.md` | 3, 5, 5, 5, 7, 7, 11, 11, 11, 12, 12, 12, 13, 13 | 14 |
| `tools/README.md` | 110, 110 | 2 |

각 문서에서 원본 Git blob에 이 링크 대상 교체만 적용한 예상 바이트와 실제 결과를 비교한다. 보존 Markdown의 본문·수치·표시 텍스트 변경0을 확인한다. 더 오래된 상태 문구는 이번에 수정하지 않는다.

## 삭제 후 보존 raw에 남는 출처 문자열

아래5건은 현재 verifier가 따라 읽는 입력이 아니다. 결과 counts를 읽는 `tools/verify_validation.py:32`는 native provenance 필드를 열지 않는다. 원문을 바꾸지 않고 삭제 후 끊긴 출처 문자열이 남는 한계를 기록한다. 삭제 전 base에서 target을 조회할 수 있다.

| 보존 source | 줄 | 삭제되는 target |
|---|---:|---|
| `doc/validation/ui-system/p3/final-edit.json` | 34 | `doc/validation/ui-system/p3/final-edit.native.json` |
| `doc/validation/ui-system/p4/final-edit.json` | 40 | `doc/validation/ui-system/p4/final-edit.native.json` |
| `doc/validation/ui-system/p5/final-edit.json` | 47 | `doc/validation/ui-system/p5/final-edit.native.json` |
| `doc/validation/ui-system/p6/final-edit.json` | 49 | `doc/validation/ui-system/p6/final-edit.native.json` |
| `doc/validation/ui-system/p7/final-edit.json` | 336 | `doc/validation/ui-system/p7/final-regression/edit.json` |

## 원시 출처 전수 목록

source/target 경로·줄은 삭제 전 base 기준이다. 자기 참조6건은 같은 editor.log에 출력된 자기 logFile 인자이며 교차 출처171과 구별한다. source35개 중30개도 함께 삭제,5개는 직접 입력이라 보존한다. 총177쌍 목록의 원시 문자열은 변경하지 않았다.

| source | 줄 | target | source 상태 |
|---|---:|---|---|
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 16 | `doc/validation/ui-system/integration-20261012/branch-tips-before.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 17 | `doc/validation/ui-system/integration-20261012/console-original.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 18 | `doc/validation/ui-system/integration-20261012/editor-restored.txt` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 19 | `doc/validation/ui-system/integration-20261012/latest-consumer-summary.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 20 | `doc/validation/ui-system/integration-20261012/source-and-acceptance.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 22 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-benchmark.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 23 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 24 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 25 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 26 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 27 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 28 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 29 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 30 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 31 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 32 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 33 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 34 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 35 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-benchmark.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 36 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 37 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 38 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 39 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 40 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 41 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 42 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 43 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 44 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/source-manifest.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 46 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-benchmark.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 47 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 48 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 49 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 50 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 51 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 52 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 53 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 54 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 55 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 56 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 57 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 58 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 59 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-benchmark.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 60 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 61 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 62 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 63 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 64 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 65 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-result.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 66 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 67 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-validated.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 68 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/source-manifest.json` | 함께 삭제 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 70 | `doc/validation/ui-system/sample-clear-20261012/summary.json` | 함께 삭제 |
| `doc/validation/ui-system/p3/final-edit.json` | 34 | `doc/validation/ui-system/p3/final-edit.native.json` | 보존 source |
| `doc/validation/ui-system/p4/file-reader-recovery.json` | 3 | `doc/validation/ui-system/p4/first-green-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p4/final-edit.json` | 40 | `doc/validation/ui-system/p4/final-edit.native.json` | 보존 source |
| `doc/validation/ui-system/p4/first-green-edit.json` | 40 | `doc/validation/ui-system/p4/first-green-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p4/red-edit.json` | 22 | `doc/validation/ui-system/p4/red-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p5/final-edit.json` | 47 | `doc/validation/ui-system/p5/final-edit.native.json` | 보존 source |
| `doc/validation/ui-system/p5/first-green-edit.json` | 47 | `doc/validation/ui-system/p5/first-green-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p5/red-edit.json` | 23 | `doc/validation/ui-system/p5/red-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p6/final-edit.json` | 49 | `doc/validation/ui-system/p6/final-edit.native.json` | 보존 source |
| `doc/validation/ui-system/p6/first-green-edit.json` | 50 | `doc/validation/ui-system/p6/first-green-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p6/red-edit.json` | 18 | `doc/validation/ui-system/p6/red-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p6/second-green-edit.json` | 49 | `doc/validation/ui-system/p6/second-green-edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 425 | `doc/validation/ui-system/p7/consumer-base-c/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 426 | `doc/validation/ui-system/p7/consumer-base-c/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 439 | `doc/validation/ui-system/p7/consumer-base-c/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 440 | `doc/validation/ui-system/p7/consumer-base-c/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 453 | `doc/validation/ui-system/p7/consumer-base-c/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 454 | `doc/validation/ui-system/p7/consumer-base-c/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 467 | `doc/validation/ui-system/p7/consumer-base-c/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 468 | `doc/validation/ui-system/p7/consumer-base-c/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-base-c/editor.log` | 39 | `doc/validation/ui-system/p7/consumer-base-c/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 593 | `doc/validation/ui-system/p7/consumer-input-additive-a/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 594 | `doc/validation/ui-system/p7/consumer-input-additive-a/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 607 | `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 608 | `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 621 | `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 622 | `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 635 | `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 636 | `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 649 | `doc/validation/ui-system/p7/consumer-input-additive-a/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 650 | `doc/validation/ui-system/p7/consumer-input-additive-a/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/editor.log` | 39 | `doc/validation/ui-system/p7/consumer-input-additive-a/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 593 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 594 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 607 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 608 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 621 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 622 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 635 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 636 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 649 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 650 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor.log` | 39 | `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 593 | `doc/validation/ui-system/p7/consumer-input-additive-final/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 594 | `doc/validation/ui-system/p7/consumer-input-additive-final/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 607 | `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 608 | `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 621 | `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 622 | `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 635 | `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 636 | `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 649 | `doc/validation/ui-system/p7/consumer-input-additive-final/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 650 | `doc/validation/ui-system/p7/consumer-input-additive-final/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/editor.log` | 39 | `doc/validation/ui-system/p7/consumer-input-additive-final/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 593 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 594 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 607 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 608 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 621 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 622 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 635 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 636 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 649 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 650 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor.log` | 21 | `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 593 | `doc/validation/ui-system/p7/consumer-input-single-final/editor.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 594 | `doc/validation/ui-system/p7/consumer-input-single-final/editor-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 607 | `doc/validation/ui-system/p7/consumer-input-single-final/smoke-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 608 | `doc/validation/ui-system/p7/consumer-input-single-final/smoke-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 621 | `doc/validation/ui-system/p7/consumer-input-single-final/scroll-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 622 | `doc/validation/ui-system/p7/consumer-input-single-final/scroll-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 635 | `doc/validation/ui-system/p7/consumer-input-single-final/canvas-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 636 | `doc/validation/ui-system/p7/consumer-input-single-final/canvas-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 649 | `doc/validation/ui-system/p7/consumer-input-single-final/sample-player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 650 | `doc/validation/ui-system/p7/consumer-input-single-final/sample-stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/consumer-input-single-final/editor.log` | 39 | `doc/validation/ui-system/p7/consumer-input-single-final/editor.log` | 함께 삭제·자기 참조 |
| `doc/validation/ui-system/p7/final-edit.json` | 336 | `doc/validation/ui-system/p7/final-regression/edit.json` | 보존 source |
| `doc/validation/ui-system/p7/final-regression/edit-runner.txt` | 1 | `doc/validation/ui-system/p7/final-regression/edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/final-regression/edit.json` | 336 | `doc/validation/ui-system/p7/final-regression/edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/original-regression/edit-runner-corrected-path.txt` | 1 | `doc/validation/ui-system/p7/original-regression/edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/original-regression/edit.json` | 336 | `doc/validation/ui-system/p7/original-regression/edit.native.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/sample-first-diagnosis.json` | 12 | `doc/validation/ui-system/p7/consumer-input-additive-a/sample-result.json` | 함께 삭제 |
| `doc/validation/ui-system/p7/sample-single-a/process.json` | 9 | `doc/validation/ui-system/p7/sample-single-a/player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/sample-single-a/process.json` | 10 | `doc/validation/ui-system/p7/sample-single-a/stdout.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/sample-single-a/process.json` | 27 | `doc/validation/ui-system/p7/sample-single-a/player.log` | 함께 삭제 |
| `doc/validation/ui-system/p7/tool-promotion.json` | 41 | `doc/validation/ui-system/p6/p7-consumer-red.txt` | 함께 삭제 |
| `doc/validation/ui-system/p7/tool-promotion.json` | 42 | `doc/validation/ui-system/p6/p7-consumer-first-green.txt` | 함께 삭제 |
| `doc/validation/ui-system/p7/tool-promotion.json` | 43 | `doc/validation/ui-system/p6/p7-newline-red.txt` | 함께 삭제 |
| `doc/validation/ui-system/p7/tool-promotion.json` | 44 | `doc/validation/ui-system/p6/p7-newline-green.txt` | 함께 삭제 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/process.json` | 20 | `doc/validation/ui-system/p7/visible-smoke-diagnostic/player.log` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 14 | `doc/validation/ui-system/sample-clear-20261012/additive-abc.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 15 | `doc/validation/ui-system/sample-clear-20261012/additive-abc.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 16 | `doc/validation/ui-system/sample-clear-20261012/additive-close.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 17 | `doc/validation/ui-system/sample-clear-20261012/additive-close.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 18 | `doc/validation/ui-system/sample-clear-20261012/additive-continue.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 19 | `doc/validation/ui-system/sample-clear-20261012/additive-final.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 20 | `doc/validation/ui-system/sample-clear-20261012/additive-final.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 21 | `doc/validation/ui-system/sample-clear-20261012/additive-native-frames.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 22 | `doc/validation/ui-system/sample-clear-20261012/additive-replace.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 23 | `doc/validation/ui-system/sample-clear-20261012/additive-start.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 24 | `doc/validation/ui-system/sample-clear-20261012/baseline.json` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 25 | `doc/validation/ui-system/sample-clear-20261012/compile-final.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 26 | `doc/validation/ui-system/sample-clear-20261012/console-after-authoring.json` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 27 | `doc/validation/ui-system/sample-clear-20261012/console-final.json` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 28 | `doc/validation/ui-system/sample-clear-20261012/edit-idle.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 29 | `doc/validation/ui-system/sample-clear-20261012/overlap-before.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 30 | `doc/validation/ui-system/sample-clear-20261012/overlap-camera-after.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 31 | `doc/validation/ui-system/sample-clear-20261012/overlap-camera-after.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 32 | `doc/validation/ui-system/sample-clear-20261012/overlap-state.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 34 | `doc/validation/ui-system/sample-clear-20261012/repair.cs.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 35 | `doc/validation/ui-system/sample-clear-20261012/repair.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 36 | `doc/validation/ui-system/sample-clear-20261012/review-state-final.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 37 | `doc/validation/ui-system/sample-clear-20261012/single-abc.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 38 | `doc/validation/ui-system/sample-clear-20261012/single-abc.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 39 | `doc/validation/ui-system/sample-clear-20261012/single-close.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 40 | `doc/validation/ui-system/sample-clear-20261012/single-close.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 41 | `doc/validation/ui-system/sample-clear-20261012/single-continue.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 42 | `doc/validation/ui-system/sample-clear-20261012/single-final.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 43 | `doc/validation/ui-system/sample-clear-20261012/single-final.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 44 | `doc/validation/ui-system/sample-clear-20261012/single-native-frames.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 45 | `doc/validation/ui-system/sample-clear-20261012/single-replace.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 46 | `doc/validation/ui-system/sample-clear-20261012/single-wait.png` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 47 | `doc/validation/ui-system/sample-clear-20261012/single-wait.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 48 | `doc/validation/ui-system/sample-clear-20261012/stop-play.txt` | 함께 삭제 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 49 | `doc/validation/ui-system/sample-clear-20261012/summary.json` | 함께 삭제 |

## 검증 요약과 한계

환경은 Windows 로컬 Git checkout/Python3.14다. Unity·Player·IL2CPP 실행은0회이며 새 실행 통과로 보고하지 않는다. 순수 문서/원시 정리 T0로 링크·입력 보존·기존 verifier 결과를 비교했다.

삭제 전 base에서 inline Python 읽기 전용 Markdown/anchor 검사와 `C:\Python314\python.exe tools/verify_validation.py --evidence <각 checks.json 폴더>`16회 + `C:\Python314\python.exe doc/validation/bootstrap-system/verify.py`1회를 실행했다. 같은 검사로 전후 비교한다.

| 검사 | 전 | 후 | 결과 |
|---|---:|---:|---|
| 기존 링크 오류 | 6 | 6 | 새 오류0 |
| anchor | 445 | 445 | 새 오류0 |
| verifier 실행/실패 | 17/17 | 17/17 | 각 exit code·stdout·stderr 동일, 새 실패0 |
| 직접 입력 원시 hash | 165개 | 165개 | 동일 |
| hash payload와 삭제 교집합 | 0 | 0 | 실존 파일 보존 |
| dirty 원시 hash | 6개 | 6개 | 동일 |
| 범위 밖 추적 파일 | 기존 raw hash | 동일 | 무관한 변경0 |

기존 링크 오류6개는 `ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md`의 과거 상대 링크다. verifier의 기존 첫 실패는 구 `Assets/MyLab/Core/DataTables/CsvDataTable.cs` 누락3, `Assets/MyLab/Core.meta` 누락8, `Assets/TPLab/Core/Lifecycle/ISceneRoot.cs` hash 불일치6이다. 기존17 실패를 통과로 고치지 않았다. verifier는 첫 오류에서 중단하므로 원시 hash 보존과 동일한 첫 실패 원문을 확인한 범위이며, 과거 결과 전체 재실행 성공을 뜻하지 않는다.

`git diff --check <base> <HEAD>`·`C:\Python314\python.exe tools/check_pr_rules.py --base <base> --head <HEAD>`·최종 HEAD exact-SHA Actions 판정은 아래 완료 상태에 남긴다. 사용자 dirty의 기존 공백 문제를 승인 diff 검사에 섞지 않는다.

완료 상태: 최종 커밋·규칙·Actions 확인 대기. PR은 open으로 유지한다. 증거 요약은 이 회고 한 곳에 두고 PR/색인은 상태와 링크만 남긴다.

## 삭제 파일 전체 목록

Git blob bytes다. `git rm -- <명시 목록>`만 실행했다. source·Markdown·허용 확장자 밖 파일·범위 밖 파일을 삭제하지 않았다. 삭제된 파일 이력은 위 base SHA에서 복원할 수 있다.

| 파일 | 바이트 |
|---|---:|
| `doc/validation/ui-system/compile-registry-20261012/TPLab.UI.EditModeTests.clean-compiler.rsp.txt` | 37,826 |
| `doc/validation/ui-system/compile-registry-20261012/TPLab.UI.PlayModeTests.clean-compiler.rsp.txt` | 36,327 |
| `doc/validation/ui-system/compile-registry-20261012/TPLab.UI.clean-compiler.rsp.txt` | 38,561 |
| `doc/validation/ui-system/compile-registry-20261012/console-before.json` | 981 |
| `doc/validation/ui-system/compile-registry-20261012/console-final.json` | 3 |
| `doc/validation/ui-system/compile-registry-20261012/owned-after-move.json` | 385 |
| `doc/validation/ui-system/compile-registry-20261012/owned-before.json` | 736 |
| `doc/validation/ui-system/compile-registry-20261012/recompile.cs.txt` | 551 |
| `doc/validation/ui-system/compile-registry-20261012/recompile.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/recompile.txt` | 71 |
| `doc/validation/ui-system/compile-registry-20261012/registry-after-clean.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/registry-after-clean.txt` | 3,873 |
| `doc/validation/ui-system/compile-registry-20261012/registry-after-move.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/registry-after-move.txt` | 3,873 |
| `doc/validation/ui-system/compile-registry-20261012/registry-before.cs.txt` | 2,253 |
| `doc/validation/ui-system/compile-registry-20261012/registry-before.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/registry-before.txt` | 3,706 |
| `doc/validation/ui-system/compile-registry-20261012/registry-final.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/registry-final.txt` | 3,873 |
| `doc/validation/ui-system/compile-registry-20261012/repair-move.cs.txt` | 1,996 |
| `doc/validation/ui-system/compile-registry-20261012/repair-move.stderr.txt` | 0 |
| `doc/validation/ui-system/compile-registry-20261012/repair-move.txt` | 491 |
| `doc/validation/ui-system/compile-registry-20261012/status-before.txt` | 110 |
| `doc/validation/ui-system/compile-registry-20261012/status-final.txt` | 110 |
| `doc/validation/ui-system/compile-registry-20261012/summary.json` | 1,092 |
| `doc/validation/ui-system/compile-registry-20261012/ui-compiler-before.rsp.txt` | 38,517 |
| `doc/validation/ui-system/compile-registry-20261012/ui-edit.json` | 3,964 |
| `doc/validation/ui-system/compile-registry-20261012/ui-edit.native.json` | 4,038 |
| `doc/validation/ui-system/compile-registry-20261012/ui-play-runner.txt` | 336 |
| `doc/validation/ui-system/compile-registry-20261012/ui-play.json` | 6,437 |
| `doc/validation/ui-system/integration-20261012/branch-tips-before.json` | 1,486 |
| `doc/validation/ui-system/integration-20261012/console-original.json` | 3 |
| `doc/validation/ui-system/integration-20261012/editor-restored.txt` | 225 |
| `doc/validation/ui-system/integration-20261012/github-before-main.json` | 373 |
| `doc/validation/ui-system/integration-20261012/latest-consumer-summary.json` | 1,854 |
| `doc/validation/ui-system/integration-20261012/main-integration.json` | 1,612 |
| `doc/validation/ui-system/integration-20261012/owned-files.json` | 6,021 |
| `doc/validation/ui-system/integration-20261012/source-and-acceptance.json` | 1,690 |
| `doc/validation/ui-system/integration-20261012/temporary-cleanup.json` | 998 |
| `doc/validation/ui-system/p0/editor-state.json` | 454 |
| `doc/validation/ui-system/p0/protected-inputs.json` | 714 |
| `doc/validation/ui-system/p1/console-expected-errors.json` | 2,439 |
| `doc/validation/ui-system/p1/corrected-edit.json` | 1,455 |
| `doc/validation/ui-system/p1/corrected-play.json` | 1,204 |
| `doc/validation/ui-system/p1/editor-state.json` | 329 |
| `doc/validation/ui-system/p1/final-edit.json` | 1,457 |
| `doc/validation/ui-system/p1/final-play.json` | 1,383 |
| `doc/validation/ui-system/p1/green-edit.json` | 1,656 |
| `doc/validation/ui-system/p1/green-play.json` | 1,290 |
| `doc/validation/ui-system/p1/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p1/red-edit.json` | 2,638 |
| `doc/validation/ui-system/p1/red-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p1/red-inputs/test-inputs.json` | 47,398 |
| `doc/validation/ui-system/p1/red-play.json` | 1,533 |
| `doc/validation/ui-system/p1/refactor-edit.json` | 1,457 |
| `doc/validation/ui-system/p1/refactor-play.json` | 1,383 |
| `doc/validation/ui-system/p1/review-red-play.json` | 1,529 |
| `doc/validation/ui-system/p1/root-regression-play.json` | 1,450 |
| `doc/validation/ui-system/p1/static-checks.json` | 274 |
| `doc/validation/ui-system/p1/test-inputs.json` | 47,398 |
| `doc/validation/ui-system/p2/final-edit.json` | 1,914 |
| `doc/validation/ui-system/p2/final-play.json` | 2,060 |
| `doc/validation/ui-system/p2/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p2/red-edit.json` | 1,320 |
| `doc/validation/ui-system/p2/red-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p2/red-inputs/test-inputs.json` | 47,895 |
| `doc/validation/ui-system/p2/red-play.json` | 1,675 |
| `doc/validation/ui-system/p2/static-checks.json` | 535 |
| `doc/validation/ui-system/p2/test-inputs.json` | 47,895 |
| `doc/validation/ui-system/p2/transport-rejected.json` | 685 |
| `doc/validation/ui-system/p2/validation-state.json` | 609 |
| `doc/validation/ui-system/p3/final-edit.native.json` | 2,266 |
| `doc/validation/ui-system/p3/first-green-edit.json` | 2,357 |
| `doc/validation/ui-system/p3/first-green-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p3/first-green-inputs/test-inputs.json` | 48,721 |
| `doc/validation/ui-system/p3/first-green-play.json` | 3,925 |
| `doc/validation/ui-system/p3/fixture-corrected-play.json` | 2,132 |
| `doc/validation/ui-system/p3/frame-dependent-edit.native.json` | 2,502 |
| `doc/validation/ui-system/p3/frame-dependent-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p3/frame-dependent-inputs/test-inputs.json` | 48,826 |
| `doc/validation/ui-system/p3/native-canvas-observation.json` | 627 |
| `doc/validation/ui-system/p3/native-group-observation.json` | 792 |
| `doc/validation/ui-system/p3/native-override-observation.json` | 1,015 |
| `doc/validation/ui-system/p3/red-edit.json` | 1,975 |
| `doc/validation/ui-system/p3/red-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p3/red-inputs/test-inputs.json` | 48,482 |
| `doc/validation/ui-system/p3/red-play.json` | 2,642 |
| `doc/validation/ui-system/p3/static-checks.json` | 360 |
| `doc/validation/ui-system/p3/transport-rejected-edit-final.json` | 659 |
| `doc/validation/ui-system/p3/transport-rejected-edit.json` | 817 |
| `doc/validation/ui-system/p4/editor-registration-recovery.json` | 1,064 |
| `doc/validation/ui-system/p4/file-reader-recovery.json` | 490 |
| `doc/validation/ui-system/p4/final-edit.native.json` | 2,945 |
| `doc/validation/ui-system/p4/final-registration-observation.json` | 344 |
| `doc/validation/ui-system/p4/first-green-edit.json` | 3,023 |
| `doc/validation/ui-system/p4/first-green-edit.native.json` | 2,945 |
| `doc/validation/ui-system/p4/first-green-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p4/first-green-inputs/test-inputs.json` | 51,899 |
| `doc/validation/ui-system/p4/first-green-play.json` | 4,938 |
| `doc/validation/ui-system/p4/red-core-play.json` | 1,816 |
| `doc/validation/ui-system/p4/red-edit.json` | 1,569 |
| `doc/validation/ui-system/p4/red-edit.native.json` | 1,542 |
| `doc/validation/ui-system/p4/red-input-play.json` | 1,463 |
| `doc/validation/ui-system/p4/red-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p4/red-inputs/test-inputs.json` | 51,423 |
| `doc/validation/ui-system/p4/source-commit-match.json` | 312 |
| `doc/validation/ui-system/p4/static-checks.json` | 437 |
| `doc/validation/ui-system/p5/compiler-input-paths.json` | 826 |
| `doc/validation/ui-system/p5/final-compile-registration.json` | 312 |
| `doc/validation/ui-system/p5/final-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p5/final-compile-registration.stdout.txt` | 273 |
| `doc/validation/ui-system/p5/final-edit-prestart.json` | 257 |
| `doc/validation/ui-system/p5/final-edit.native.json` | 3,740 |
| `doc/validation/ui-system/p5/final-registration-observation.json` | 464 |
| `doc/validation/ui-system/p5/first-green-compile-registration.json` | 312 |
| `doc/validation/ui-system/p5/first-green-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p5/first-green-compile-registration.stdout.txt` | 273 |
| `doc/validation/ui-system/p5/first-green-edit.json` | 3,725 |
| `doc/validation/ui-system/p5/first-green-edit.native.json` | 3,740 |
| `doc/validation/ui-system/p5/first-green-play.json` | 5,717 |
| `doc/validation/ui-system/p5/red-compile-registration-attempt1.json` | 268 |
| `doc/validation/ui-system/p5/red-compile-registration-attempt1.stdout.txt` | 0 |
| `doc/validation/ui-system/p5/red-compile-registration.json` | 273 |
| `doc/validation/ui-system/p5/red-edit-prestart.json` | 356 |
| `doc/validation/ui-system/p5/red-edit.json` | 1,741 |
| `doc/validation/ui-system/p5/red-edit.native.json` | 1,716 |
| `doc/validation/ui-system/p5/red-play.json` | 1,861 |
| `doc/validation/ui-system/p5/source-commit-match.json` | 358 |
| `doc/validation/ui-system/p5/source-whitespace-fix.json` | 263 |
| `doc/validation/ui-system/p5/static-validation.json` | 254 |
| `doc/validation/ui-system/p6/final-compile-registration.cs.txt` | 2,009 |
| `doc/validation/ui-system/p6/final-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/final-compile-registration.stdout.txt` | 300 |
| `doc/validation/ui-system/p6/final-edit.native.json` | 3,995 |
| `doc/validation/ui-system/p6/final-observation.json` | 433 |
| `doc/validation/ui-system/p6/first-green-cli-trace.json` | 1,309 |
| `doc/validation/ui-system/p6/first-green-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/first-green-compile-registration.stdout.txt` | 273 |
| `doc/validation/ui-system/p6/first-green-diagnosis.json` | 534 |
| `doc/validation/ui-system/p6/first-green-edit-prestart.json` | 182 |
| `doc/validation/ui-system/p6/first-green-edit.json` | 4,054 |
| `doc/validation/ui-system/p6/first-green-edit.native.json` | 4,075 |
| `doc/validation/ui-system/p6/first-green-play.json` | 6,676 |
| `doc/validation/ui-system/p6/first-green-ready-observation.json` | 275 |
| `doc/validation/ui-system/p6/p7-consumer-first-green.txt` | 978 |
| `doc/validation/ui-system/p6/p7-consumer-red.txt` | 4,965 |
| `doc/validation/ui-system/p6/p7-newline-green.txt` | 1,219 |
| `doc/validation/ui-system/p6/p7-newline-red.txt` | 2,236 |
| `doc/validation/ui-system/p6/prior-phase-temp-cleanup.json` | 1,380 |
| `doc/validation/ui-system/p6/red-compile-attempt1-errors.json` | 1,515 |
| `doc/validation/ui-system/p6/red-compile-registration-attempt2.json` | 571 |
| `doc/validation/ui-system/p6/red-compile-registration-attempt2.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/red-compile-registration-attempt2.stdout.txt` | 510 |
| `doc/validation/ui-system/p6/red-compile-registration.json` | 312 |
| `doc/validation/ui-system/p6/red-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/red-compile-registration.stdout.txt` | 273 |
| `doc/validation/ui-system/p6/red-edit.json` | 1,393 |
| `doc/validation/ui-system/p6/red-edit.native.json` | 1,358 |
| `doc/validation/ui-system/p6/red-fixture-integration.json` | 619 |
| `doc/validation/ui-system/p6/red-play.json` | 1,591 |
| `doc/validation/ui-system/p6/red-scaffold.json` | 631 |
| `doc/validation/ui-system/p6/sample-authoring.json` | 2,679 |
| `doc/validation/ui-system/p6/sample-compile-first-errors.json` | 1,360 |
| `doc/validation/ui-system/p6/sample-compile-registration-2.cs.txt` | 2,009 |
| `doc/validation/ui-system/p6/sample-compile-registration-2.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/sample-compile-registration-2.stdout.txt` | 137 |
| `doc/validation/ui-system/p6/sample-compile-registration.cs.txt` | 2,009 |
| `doc/validation/ui-system/p6/sample-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/sample-compile-registration.stdout.txt` | 300 |
| `doc/validation/ui-system/p6/sample-line-endings.json` | 1,177 |
| `doc/validation/ui-system/p6/sample-promotion.json` | 1,761 |
| `doc/validation/ui-system/p6/second-green-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p6/second-green-compile-registration.stdout.txt` | 250 |
| `doc/validation/ui-system/p6/second-green-edit.json` | 3,977 |
| `doc/validation/ui-system/p6/second-green-edit.native.json` | 3,995 |
| `doc/validation/ui-system/p6/second-green-inputs/preserved-inputs.json` | 714 |
| `doc/validation/ui-system/p6/second-green-inputs/test-inputs.json` | 54,358 |
| `doc/validation/ui-system/p6/second-green-play.json` | 6,364 |
| `doc/validation/ui-system/p6/source-commit-match.json` | 392 |
| `doc/validation/ui-system/p7/candidate-source-check/preserved-inputs.json` | 722 |
| `doc/validation/ui-system/p7/candidate-source-check/source-commit-match.json` | 849 |
| `doc/validation/ui-system/p7/candidate-source-check/test-inputs.json` | 62,533 |
| `doc/validation/ui-system/p7/compile-registration-driver.py.txt` | 2,596 |
| `doc/validation/ui-system/p7/consumer-base-c/canvas-benchmark.json` | 1,828,145 |
| `doc/validation/ui-system/p7/consumer-base-c/canvas-player.log` | 2,855 |
| `doc/validation/ui-system/p7/consumer-base-c/canvas-result.json` | 1,912 |
| `doc/validation/ui-system/p7/consumer-base-c/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-base-c/canvas-validated.json` | 332,207 |
| `doc/validation/ui-system/p7/consumer-base-c/consumer-report.json` | 48,674 |
| `doc/validation/ui-system/p7/consumer-base-c/editor-result.json` | 587 |
| `doc/validation/ui-system/p7/consumer-base-c/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-base-c/editor.log` | 422,290 |
| `doc/validation/ui-system/p7/consumer-base-c/scroll-benchmark.json` | 2,015,528 |
| `doc/validation/ui-system/p7/consumer-base-c/scroll-player.log` | 2,855 |
| `doc/validation/ui-system/p7/consumer-base-c/scroll-result.json` | 1,912 |
| `doc/validation/ui-system/p7/consumer-base-c/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-base-c/scroll-validated.json` | 333,802 |
| `doc/validation/ui-system/p7/consumer-base-c/smoke-player.log` | 2,855 |
| `doc/validation/ui-system/p7/consumer-base-c/smoke-result.json` | 440,203 |
| `doc/validation/ui-system/p7/consumer-base-c/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-base-c/smoke-validated.json` | 105,695 |
| `doc/validation/ui-system/p7/consumer-base-c/source-manifest.json` | 40,737 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-benchmark.json` | 1,827,967 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-player.log` | 2,848 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-result.json` | 1,921 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/canvas-validated.json` | 332,055 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/consumer-report.json` | 71,707 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/editor-result.json` | 708 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/editor.log` | 779,851 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/sample-player.log` | 8,612 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/sample-result.json` | 10,071 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/sample-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-benchmark.json` | 2,016,056 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-player.log` | 2,848 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-result.json` | 1,921 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/scroll-validated.json` | 334,238 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-player.log` | 2,848 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-result.json` | 441,185 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/smoke-validated.json` | 106,336 |
| `doc/validation/ui-system/p7/consumer-input-additive-a/source-manifest.json` | 61,202 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-benchmark.json` | 1,828,440 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-player.log` | 3,092 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-result.json` | 1,933 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/canvas-validated.json` | 332,512 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/consumer-report.json` | 71,978 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor-result.json` | 747 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/editor.log` | 788,814 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-player.log` | 3,121 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-result.json` | 11,322 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/sample-validated.json` | 634 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-benchmark.json` | 2,016,261 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-player.log` | 3,093 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-result.json` | 1,934 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/scroll-validated.json` | 334,481 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-player.log` | 3,183 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-result.json` | 440,822 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/smoke-validated.json` | 106,128 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/source-manifest.json` | 61,215 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-benchmark.json` | 1,828,062 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-player.log` | 2,858 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-result.json` | 1,925 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/canvas-validated.json` | 332,127 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/consumer-report.json` | 71,844 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/editor-result.json` | 720 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/editor.log` | 783,608 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/sample-player.log` | 2,887 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/sample-result.json` | 11,313 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/sample-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/sample-validated.json` | 654 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-benchmark.json` | 2,016,190 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-player.log` | 2,859 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-result.json` | 1,925 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/scroll-validated.json` | 334,294 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-player.log` | 2,859 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-result.json` | 440,962 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/smoke-validated.json` | 106,148 |
| `doc/validation/ui-system/p7/consumer-input-additive-final/source-manifest.json` | 61,206 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-benchmark.json` | 1,828,610 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-player.log` | 2,880 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-result.json` | 1,932 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/canvas-validated.json` | 332,673 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/consumer-report.json` | 71,954 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor-result.json` | 741 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/editor.log` | 787,905 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-player.log` | 4,500 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-result.json` | 11,533 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/sample-validated.json` | 630 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-benchmark.json` | 2,015,579 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-player.log` | 3,086 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-result.json` | 1,932 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/scroll-validated.json` | 333,974 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-player.log` | 3,086 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-result.json` | 441,358 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/smoke-validated.json` | 106,551 |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/source-manifest.json` | 61,211 |
| `doc/validation/ui-system/p7/consumer-input-single-final/canvas-benchmark.json` | 1,828,005 |
| `doc/validation/ui-system/p7/consumer-input-single-final/canvas-player.log` | 2,851 |
| `doc/validation/ui-system/p7/consumer-input-single-final/canvas-result.json` | 1,923 |
| `doc/validation/ui-system/p7/consumer-input-single-final/canvas-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-final/canvas-validated.json` | 332,036 |
| `doc/validation/ui-system/p7/consumer-input-single-final/consumer-report.json` | 71,816 |
| `doc/validation/ui-system/p7/consumer-input-single-final/editor-result.json` | 714 |
| `doc/validation/ui-system/p7/consumer-input-single-final/editor-stdout.log` | 0 |
| `doc/validation/ui-system/p7/consumer-input-single-final/editor.log` | 782,427 |
| `doc/validation/ui-system/p7/consumer-input-single-final/sample-player.log` | 4,272 |
| `doc/validation/ui-system/p7/consumer-input-single-final/sample-result.json` | 11,524 |
| `doc/validation/ui-system/p7/consumer-input-single-final/sample-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-final/sample-validated.json` | 650 |
| `doc/validation/ui-system/p7/consumer-input-single-final/scroll-benchmark.json` | 2,015,903 |
| `doc/validation/ui-system/p7/consumer-input-single-final/scroll-player.log` | 2,854 |
| `doc/validation/ui-system/p7/consumer-input-single-final/scroll-result.json` | 1,923 |
| `doc/validation/ui-system/p7/consumer-input-single-final/scroll-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-final/scroll-validated.json` | 334,200 |
| `doc/validation/ui-system/p7/consumer-input-single-final/smoke-player.log` | 2,853 |
| `doc/validation/ui-system/p7/consumer-input-single-final/smoke-result.json` | 440,958 |
| `doc/validation/ui-system/p7/consumer-input-single-final/smoke-stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/consumer-input-single-final/smoke-validated.json` | 106,153 |
| `doc/validation/ui-system/p7/consumer-input-single-final/source-manifest.json` | 61,202 |
| `doc/validation/ui-system/p7/corrected-helper-green-direct.txt` | 1,099 |
| `doc/validation/ui-system/p7/corrected-helper-green.txt` | 919 |
| `doc/validation/ui-system/p7/failed-sample-count-red.txt` | 162 |
| `doc/validation/ui-system/p7/final-console-clear.txt` | 17 |
| `doc/validation/ui-system/p7/final-console.txt` | 3 |
| `doc/validation/ui-system/p7/final-editor-observation.json` | 579 |
| `doc/validation/ui-system/p7/final-fixture-compile-registration.cs.txt` | 2,017 |
| `doc/validation/ui-system/p7/final-fixture-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p7/final-fixture-compile-registration.stdout.txt` | 301 |
| `doc/validation/ui-system/p7/final-helper-green.txt` | 1,109 |
| `doc/validation/ui-system/p7/final-original-compile-registration.cs.txt` | 2,017 |
| `doc/validation/ui-system/p7/final-original-compile-registration.stderr.txt` | 0 |
| `doc/validation/ui-system/p7/final-original-compile-registration.stdout.txt` | 301 |
| `doc/validation/ui-system/p7/final-product-console.json` | 3 |
| `doc/validation/ui-system/p7/final-regression/edit-runner.txt` | 31,719 |
| `doc/validation/ui-system/p7/final-regression/edit.json` | 33,516 |
| `doc/validation/ui-system/p7/final-regression/edit.native.json` | 33,788 |
| `doc/validation/ui-system/p7/final-regression/play-runner.txt` | 368 |
| `doc/validation/ui-system/p7/final-regression/preserved-inputs.json` | 722 |
| `doc/validation/ui-system/p7/final-regression/test-inputs.json` | 62,533 |
| `doc/validation/ui-system/p7/final-source-check/preserved-inputs.json` | 722 |
| `doc/validation/ui-system/p7/final-source-check/source-commit-match.json` | 385 |
| `doc/validation/ui-system/p7/final-source-check/test-inputs.json` | 62,533 |
| `doc/validation/ui-system/p7/final-static-check.json` | 539 |
| `doc/validation/ui-system/p7/final-test-expected-console.json` | 12,030 |
| `doc/validation/ui-system/p7/first-consumer-diagnosis.json` | 620 |
| `doc/validation/ui-system/p7/full-regression-first-diagnosis.json` | 882 |
| `doc/validation/ui-system/p7/original-regression/edit-runner-corrected-path.txt` | 31,722 |
| `doc/validation/ui-system/p7/original-regression/edit-runner.txt` | 631 |
| `doc/validation/ui-system/p7/original-regression/edit.json` | 33,516 |
| `doc/validation/ui-system/p7/original-regression/edit.native.json` | 33,788 |
| `doc/validation/ui-system/p7/original-regression/play-runner.txt` | 334 |
| `doc/validation/ui-system/p7/original-regression/play.json` | 32,127 |
| `doc/validation/ui-system/p7/original-regression/preserved-inputs.json` | 722 |
| `doc/validation/ui-system/p7/original-regression/test-inputs.json` | 62,533 |
| `doc/validation/ui-system/p7/promoted-helper-green.txt` | 1,229 |
| `doc/validation/ui-system/p7/sample-first-diagnosis.json` | 868 |
| `doc/validation/ui-system/p7/sample-harness-promotion.json` | 1,048 |
| `doc/validation/ui-system/p7/sample-single-a/player.log` | 27,592 |
| `doc/validation/ui-system/p7/sample-single-a/process.json` | 1,123 |
| `doc/validation/ui-system/p7/sample-single-a/result.json` | 56,444 |
| `doc/validation/ui-system/p7/sample-single-a/stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/sample-single-first-diagnosis.json` | 801 |
| `doc/validation/ui-system/p7/source-commit-match.json` | 966 |
| `doc/validation/ui-system/p7/temporary-cleanup.json` | 1,884 |
| `doc/validation/ui-system/p7/temporary-drafts-manifest.json` | 7,576 |
| `doc/validation/ui-system/p7/tool-promotion.json` | 2,343 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/player.log` | 2,854 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/process.json` | 792 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/result.json` | 440,101 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/stdout.log` | 1,771 |
| `doc/validation/ui-system/p7/visible-smoke-diagnostic/validated.json` | 87,085 |
| `doc/validation/ui-system/sample-clear-20261012/additive-abc.png` | 126,664 |
| `doc/validation/ui-system/sample-clear-20261012/additive-abc.txt` | 38 |
| `doc/validation/ui-system/sample-clear-20261012/additive-close.png` | 115,736 |
| `doc/validation/ui-system/sample-clear-20261012/additive-close.txt` | 48 |
| `doc/validation/ui-system/sample-clear-20261012/additive-continue.txt` | 26 |
| `doc/validation/ui-system/sample-clear-20261012/additive-final.png` | 114,051 |
| `doc/validation/ui-system/sample-clear-20261012/additive-final.txt` | 476 |
| `doc/validation/ui-system/sample-clear-20261012/additive-native-frames.txt` | 47 |
| `doc/validation/ui-system/sample-clear-20261012/additive-replace.txt` | 60 |
| `doc/validation/ui-system/sample-clear-20261012/additive-start.txt` | 31 |
| `doc/validation/ui-system/sample-clear-20261012/baseline.json` | 1,406 |
| `doc/validation/ui-system/sample-clear-20261012/compile-final.txt` | 1,069 |
| `doc/validation/ui-system/sample-clear-20261012/console-after-authoring.json` | 3 |
| `doc/validation/ui-system/sample-clear-20261012/console-final.json` | 3 |
| `doc/validation/ui-system/sample-clear-20261012/edit-idle.txt` | 0 |
| `doc/validation/ui-system/sample-clear-20261012/overlap-before.png` | 151,567 |
| `doc/validation/ui-system/sample-clear-20261012/overlap-camera-after.png` | 124,663 |
| `doc/validation/ui-system/sample-clear-20261012/overlap-camera-after.txt` | 159 |
| `doc/validation/ui-system/sample-clear-20261012/overlap-state.txt` | 11,800 |
| `doc/validation/ui-system/sample-clear-20261012/repair.cs.txt` | 1,696 |
| `doc/validation/ui-system/sample-clear-20261012/repair.txt` | 348 |
| `doc/validation/ui-system/sample-clear-20261012/review-state-final.txt` | 137 |
| `doc/validation/ui-system/sample-clear-20261012/reviewed-files.json` | 3,220 |
| `doc/validation/ui-system/sample-clear-20261012/single-abc.png` | 126,664 |
| `doc/validation/ui-system/sample-clear-20261012/single-abc.txt` | 38 |
| `doc/validation/ui-system/sample-clear-20261012/single-close.png` | 115,736 |
| `doc/validation/ui-system/sample-clear-20261012/single-close.txt` | 48 |
| `doc/validation/ui-system/sample-clear-20261012/single-continue.txt` | 431 |
| `doc/validation/ui-system/sample-clear-20261012/single-final.png` | 114,051 |
| `doc/validation/ui-system/sample-clear-20261012/single-final.txt` | 467 |
| `doc/validation/ui-system/sample-clear-20261012/single-native-frames.txt` | 66 |
| `doc/validation/ui-system/sample-clear-20261012/single-replace.txt` | 58 |
| `doc/validation/ui-system/sample-clear-20261012/single-wait.png` | 29,696 |
| `doc/validation/ui-system/sample-clear-20261012/single-wait.txt` | 478 |
| `doc/validation/ui-system/sample-clear-20261012/stop-play.txt` | 55 |
| `doc/validation/ui-system/sample-clear-20261012/summary.json` | 2,365 |

## 보존 파일 전체 목록

| 파일 | 바이트 | 이유 |
|---|---:|---|
| `doc/validation/ui-system/compile-registry-20261012/.gitattributes` | 37 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/compile-registry-20261012/README.md` | 4,074 | Markdown 보존 |
| `doc/validation/ui-system/integration-20261012/.gitattributes` | 100 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/integration-20261012/README.md` | 2,993 | Markdown 보존 |
| `doc/validation/ui-system/p0/README.md` | 1,471 | Markdown 보존 |
| `doc/validation/ui-system/p1/README.md` | 4,234 | Markdown 보존 |
| `doc/validation/ui-system/p2/README.md` | 3,248 | Markdown 보존 |
| `doc/validation/ui-system/p3/README.md` | 2,655 | Markdown 보존 |
| `doc/validation/ui-system/p3/checks.json` | 1,197 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/final-edit.json` | 2,267 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/final-play.json` | 3,649 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/preserved-inputs.json` | 714 | verifier 직접 입력 |
| `doc/validation/ui-system/p3/test-inputs.json` | 48,826 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/README.md` | 4,117 | Markdown 보존 |
| `doc/validation/ui-system/p4/checks.json` | 1,584 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/final-edit.json` | 2,962 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/final-play.json` | 4,938 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/preserved-inputs.json` | 714 | verifier 직접 입력 |
| `doc/validation/ui-system/p4/test-inputs.json` | 51,899 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/README.md` | 4,204 | Markdown 보존 |
| `doc/validation/ui-system/p5/checks.json` | 938 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/final-edit.json` | 3,719 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/final-play.json` | 5,751 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/preserved-inputs.json` | 714 | verifier 직접 입력 |
| `doc/validation/ui-system/p5/test-inputs.json` | 52,961 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/README.md` | 3,040 | Markdown 보존 |
| `doc/validation/ui-system/p6/checks.json` | 996 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/final-edit.json` | 3,970 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/final-play.json` | 6,395 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/preserved-inputs.json` | 714 | verifier 직접 입력 |
| `doc/validation/ui-system/p6/test-inputs.json` | 61,435 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/.gitattributes` | 113 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/p7/README.md` | 11,338 | Markdown 보존 |
| `doc/validation/ui-system/p7/checks.json` | 4,551 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/.gitattributes` | 85 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/.gitattributes` | 85 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/p7/final-console.json` | 4 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/final-edit.json` | 33,304 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/final-regression/play.json` | 32,000 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/preserved-inputs.json` | 722 | verifier 직접 입력 |
| `doc/validation/ui-system/p7/test-inputs.json` | 62,533 | verifier 직접 입력 |
| `doc/validation/ui-system/sample-clear-20261012/.gitattributes` | 67 | 허용 확장자 밖 .gitattributes |
| `doc/validation/ui-system/sample-clear-20261012/README.md` | 3,429 | Markdown 보존 |

허용 확장자 밖 파일은 .gitattributes6개뿐이다. .cs/.csproj/.py/.diff 등의 파일은 이 UI validation 트리에 없었다.

| 보존 .gitattributes |
|---|
| `doc/validation/ui-system/compile-registry-20261012/.gitattributes` |
| `doc/validation/ui-system/integration-20261012/.gitattributes` |
| `doc/validation/ui-system/p7/.gitattributes` |
| `doc/validation/ui-system/p7/consumer-input-additive-accepted-20261012/.gitattributes` |
| `doc/validation/ui-system/p7/consumer-input-single-accepted-20261012/.gitattributes` |
| `doc/validation/ui-system/sample-clear-20261012/.gitattributes` |

## 회고·협업·미실행

이전 UI456파일 집계와 실제 트리는 같았다. 소비 runner의 고정 basename을 기존 고정 경로로 오인하지 않도록 fresh-path gate와 문서 인자를 확인했다. 잔존 raw 출처5개와 비링크 경로1개는 원문 보존 정책의 한계로 명시했다. 기존 누락 doc1개·링크6·verifier17은 수정하지 않는다. HTTP는 표본이며 Git 존재 확인과 구별한다.

기존 하위 에이전트 `/root/imp01_pool_impl`(UI 책임·테스트 조사)와 `/root/factcheck_upm`(GameScene 조사·B 독립 감사)를 재사용했다. 모델/추론 설정 변경 없이 기존 설정을 유지했으며 도구가 모델명/수치를 노출하지 않아 추정하지 않는다. 수명·소유권 분석 위험은 high로 기록했다. 허용 범위는 관련 Git/source/test/docs 읽기만, 파일/Temp/Git/Unity 변경 금지이며 두 작업 완료다. 부모가 B 파일 변경·Git·검증·회고를 소유한다. 책임·테스트 조사 내용은 별도 채팅 보고이며 코드 분리·새 테스트·임시 변형은 하지 않았다.

작업용 생성 출력은 `Temp/validation-prune-ui-system-20261014`에만 두고 Git에 추가하지 않는다. PR 제공·최종 비교 후 이번 작업이 만든 명시 파일만 정리한다. 기존 Temp·사용자 자산·dirty 변경은 건드리지 않는다.
