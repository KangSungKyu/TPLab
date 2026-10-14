# 2026-10-14 · 04 · validation 소규모 영역 원시 자료와 링크 정리

T0 검증·결정 기록이다. 사용자 요청 A(PR #17 통합)를 완료한 뒤 B를 최신 main에서 시작했다. 삭제 전 기준은 `4704dc0387d5e22ad8eae745386dcc8a03e78680`, 브랜치는 `codex/validation-prune-group1`이다. B는 PR 제공까지만 진행하며 병합 승인을 기다린다. 이 회고 한 곳에 증거 요약을 남긴다.

## A · PR #17 통합

HEAD `2438eeaae6a3acf4bda2e354e3fdf63305b4d241`, base/main `db05c252f0e8bce7c9fc67b2faf944bbd00d601d`가 지정값과 같고 그 HEAD의 [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38501858880)가 completed/success이며 exact-SHA·조회 완전성 조건을 만족하는지 새로 확인했다. 일반 merge 결과와 로컬/원격 main은 모두 `4704dc0387d5e22ad8eae745386dcc8a03e78680`이다.

로컬/원격 `codex/validation-prune-imp03-04-05-14`를 정리했다. 삭제 전 tip은 위 HEAD다. main 도달성, 로컬/원격 tip 일치, 다른 worktree 미사용을 확인하고 원격 예상 tip을 조건으로 삭제한 뒤 로컬 `git branch -d`를 사용했다. dirty 6개와 A 범위 밖 추적 파일 2,108개의 원시 바이트를 보존했다. 원본 커밋은 main 이력에 남는다.

## B · 확정 대상과 보존 경계

허용 원시 확장자만 401파일·4,460,731 Git blob 바이트를 삭제한다(.json 298, .log 22, .png 3, .txt 67, .xml 11). 범위 내 보존은 163파일·1,122,402바이트다. 이는 verifier 직접 입력 130개 + 추가 출력 2개 + Markdown 25개 + 다른 확장자 6개이며 서로 겹치지 않는다.

전체 verifier 직접 입력은 generic 158개 + Bootstrap 7개 = 165개이고 모두 원시 SHA-256을 보존한다(선택 영역 밖 UI 직접 입력 35개 포함). 체크 결과 선언, 필수 5개 및 hash payload 선언 경로도 보존했다. hash payload 선언 경로 836개와 삭제 대상의 교집합은 0이다. 과거 payload가 누락됐거나 hash가 달라 기존 verifier가 실패하는 현상은 수정하지 않는다.

처음 허용 확장자/직접 입력만 분류한 후보는 403파일·4,461,919바이트였다. 사용자 후속 결정 “고정 출력 파일도 보존·조사 계속”에 따라 아래 833·355바이트를 추가 보존해 최종 수치를 확정했다.

| 고정 출력 | 바이트 | 보존 근거 |
|---|---:|---|
| `data-table-importer/native-editor.json` | 833 | `Assets/TPLab/Validation/Editor/DataTableImportEditorCheck.cs:246-247`의 현재 고정 출력 |
| `game-scene-entry/native-editor.json` | 355 | `doc/validation/game-scene-entry/README.md:38`의 명시 실행 가능한 과거 고정 출력 인자; 보수적으로 보존 |
| `bootstrap-system/native-editor.json` | 294 | 기존 Bootstrap 직접 입력 7개에 이미 포함; `BootstrapEditorCheck.cs:19,146` |

아래 경로는 모두 `doc/validation/` 아래다. Bootstrap 직접 입력 7개는 `full-EditMode.json`, `full-PlayMode.json`, `native-editor.json`, `final-console.json`, `final-status.txt`, `test-inputs.json`, `preserved-user-inputs.json`이다. `ui-system`, `distribution-*`, `imp-*`는 삭제 대상 밖이며 기존 내용을 보존했다.

| 영역 | 삭제 파일 | 삭제 바이트 | 보존 파일 | 보존 바이트 | 직접 입력 |
|---|---:|---:|---:|---:|---:|
| scene-integration | 44 | 1,557,334 | 11 | 96,938 | 10 |
| scene-loading | 55 | 1,513,008 | 10 | 106,270 | 8 |
| scene-transition-editor | 5 | 13,723 | 18 | 164,404 | 17 |
| scene-definitions | 2 | 12,389 | 14 | 94,174 | 13 |
| scene-loaders | 4 | 12,230 | 17 | 92,536 | 16 |
| scene-areas | 3 | 9,040 | 12 | 80,691 | 11 |
| scene-replacement | 2 | 8,549 | 10 | 71,064 | 9 |
| scene-transition-contracts | 3 | 9,382 | 10 | 44,171 | 9 |
| scene-root | 9 | 26,654 | 0 | 0 | 0 |
| async-scene | 10 | 31,204 | 0 | 0 | 0 |
| game-scene-entry | 7 | 24,657 | 16 | 57,929 | 14 |
| input-system | 59 | 224,589 | 11 | 102,430 | 9 |
| data-tables | 24 | 283,828 | 1 | 5,236 | 0 |
| data-table-importer | 31 | 116,804 | 2 | 7,424 | 0 |
| generic-data-tables | 25 | 90,333 | 1 | 6,118 | 0 |
| data-table-templates | 14 | 69,006 | 1 | 4,307 | 0 |
| resource-manager | 20 | 168,966 | 1 | 5,221 | 0 |
| generic-pool | 8 | 11,969 | 2 | 1,790 | 0 |
| object-pool | 6 | 6,991 | 0 | 0 | 0 |
| singleton | 8 | 19,290 | 0 | 0 | 0 |
| bootstrap-system | 10 | 30,384 | 9 | 53,569 | 7 |
| tplab-naming | 7 | 77,332 | 8 | 101,280 | 7 |
| tplab-name-cleanup | 11 | 5,395 | 1 | 3,965 | 0 |
| ci-review-p1 | 32 | 135,744 | 8 | 22,885 | 0 |
| documentation | 1 | 725 | 0 | 0 | 0 |
| bootstrap-additive-design.json | 1 | 1,205 | 0 | 0 | 0 |
| 합계 | 401 | 4,460,731 | 163 | 1,122,402 | 130 |

## 참조 전수 분류

고정 SHA의 `git ls-tree -rlz`, `git cat-file --batch`, `git grep -I -n -E` 및 UTF-8 텍스트 분석으로 조사했다. slash/backslash·중복 backslash·상대 경로·URL·basename 문맥을 구별했다. 현행 입력 의존과 출력 경로를 분리했다.

| 분류 | 측정 | 처리 |
|---|---|---|
| (a) Markdown 링크 | 파일 링크 329 + 디렉터리 링크 6 = 335표현, 56문서 | 표시 텍스트·문장·수치·anchor 유지, 대상만 고정 SHA URL |
| (b) 링크 밖 Markdown 경로 | 2곳 | 원문 그대로 보존; 삭제 후 경로 문자열만 남는 점을 아래 기록 |
| (c) raw 내부 출처 | 135 source-line/target 쌍, source 18개 | 원시 문자열은 수정하지 않음; source 18개도 삭제돼 잔존 raw 출처의 끊김은 0개 |
| (d) 보존 경계 밖 현재 읽기 의존 | 0개 | 추가 입력 제외/중단 대상 없음; 직접 입력·고정 출력은 보존 |

(b) `ci-review-p1/fix-p1/README.md:5`의 `../baseline.json` 및 `scene-loading/integration/README.md:9`의 `phase-cleanup.json`은 링크 밖 역사적 경로 표기라 수정하지 않았다. 이 두 문자열의 대상은 삭제돼 현재 트리에서 열리지 않는다. source와 target은 삭제 전 SHA에서 확인할 수 있다. 다른 기존 broken 경로나 문장은 이 작업에서 고치지 않았다.

UPM 배포 사본의 `Documentation~/api/Pooling.md:102`와 `Documentation~/ai/api/Pooling.md:9`는 이미 `v0.0.1` 이력을 가리키는 URL 2개다. main 삭제는 해당 릴리스 이력을 바꾸지 않으며 AGENTS의 배포 사본 직접 수정 금지에 따라 그대로 보존했다. Assets·테스트·UPM·실행 도구·workflow 수정은 0이다. `tools/README.md`는 Markdown 링크 대상 1개만 전환했다.

## permalink와 문서별 전환 표

기준 SHA는 `4704dc0387d5e22ad8eae745386dcc8a03e78680`다. 파일 대상 형식은 `https://github.com/KangSungKyu/TPLab/blob/<SHA>/<repository-path>`, 디렉터리는 `tree`다. 삭제 파일 401개가 그 SHA의 blob이며 링크 대상 디렉터리 3개가 tree임을 `git cat-file`로 전수 확인했다. 파일 링크 329개는 서로 다른 파일 287개를 가리킨다. HTTP HEAD 표본은 영역별 파일 26개와 디렉터리 3개 = 29/29 HTTP 200이다. 나머지 모든 파일의 HTTP 검사를 실행했다고 주장하지 않는다.

초기 파일 링크 계획은 329표현/53문서였다. 첫 삭제 후 링크 검사에서 비는 디렉터리 링크 6개를 발견했다. `scene-loading/p1`, `scene-loading/p2`, `scene-integration/CoreConsumer-P6-20261006-02`를 같은 SHA의 tree permalink로 보완하여 새 오류를 제거했다. 아래는 삭제 전 기준 줄 번호다. 같은 줄의 여러 링크는 반복 줄 번호로 표시해 전환 수와 일치시킨다.

| 문서 | 기준 줄(표현별) | 전환 수 |
|---|---|---:|
| `doc/ASYNC_SCENE_LIFECYCLE.md` | 58, 58, 59, 59, 60, 61 | 6 |
| `doc/GENERIC_POOL.md` | 93, 94, 95, 96, 96, 97, 98, 99 | 8 |
| `doc/INDEX.md` | 80, 81, 81, 82, 82, 83, 83, 84, 84, 85, 85, 85, 86, 86, 86, 87, 87, 87 | 18 |
| `doc/INPUT_SYSTEM_TRACK.md` | 37, 41, 47 | 3 |
| `doc/OBJECT_POOL.md` | 72, 72, 73, 73, 74, 75 | 6 |
| `doc/SCENE_LOADING_PRESENTATION_DRAFT.md` | 95 | 1 |
| `doc/SCENE_LOADING_TRACK.md` | 9, 10, 31 | 3 |
| `doc/SCENE_ROOT.md` | 53, 53, 54, 54, 56, 57, 57, 58 | 8 |
| `doc/SCENE_TRANSITION_ACCEPTANCE.md` | 3 | 1 |
| `doc/SCENE_TRANSITION_TRACK.md` | 31, 34, 35, 36, 37, 39, 90 | 7 |
| `doc/SINGLETON.md` | 45, 46, 47, 48, 49, 49, 50, 50 | 8 |
| `doc/ai/api/Pooling.md` | 11 | 1 |
| `doc/api/Pooling.md` | 136 | 1 |
| `doc/retrospectives/2026-10-06-03-idx-codec.md` | 7, 7 | 2 |
| `doc/retrospectives/2026-10-06-04-standard-data-query.md` | 7, 7 | 2 |
| `doc/retrospectives/2026-10-06-05-data-table-binding-fk.md` | 7, 7 | 2 |
| `doc/retrospectives/2026-10-06-11-bootstrap-additive-design.md` | 20 | 1 |
| `doc/retrospectives/2026-10-06-17-scene-loaders.md` | 11 | 1 |
| `doc/retrospectives/2026-10-06-18-scene-replacement.md` | 10 | 1 |
| `doc/retrospectives/2026-10-06-19-scene-areas.md` | 10 | 1 |
| `doc/retrospectives/2026-10-06-20-scene-definitions.md` | 10 | 1 |
| `doc/retrospectives/2026-10-07-01-scene-track-integration.md` | 4 | 1 |
| `doc/retrospectives/2026-10-07-04-input-layers.md` | 6, 6, 6 | 3 |
| `doc/retrospectives/2026-10-07-05-input-rebinding.md` | 7, 7, 7, 7 | 4 |
| `doc/retrospectives/2026-10-07-06-input-integration.md` | 7, 7, 7, 7 | 4 |
| `doc/retrospectives/2026-10-07-09-public-api-docs-and-input-acceptance.md` | 17 | 1 |
| `doc/retrospectives/2026-10-07-10-scene-loading-progress.md` | 5 | 1 |
| `doc/retrospectives/2026-10-07-11-scene-loading-flow-and-proceed.md` | 9 | 1 |
| `doc/retrospectives/2026-10-07-12-scene-loading-sample-validation.md` | 22 | 1 |
| `doc/retrospectives/2026-10-12-04-ci-review-automation.md` | 40 | 1 |
| `doc/retrospectives/2026-10-12-05-ci-review-corrections.md` | 36 | 1 |
| `doc/validation/bootstrap-system/README.md` | 7, 8, 9, 10, 14, 16, 18, 18, 31 | 9 |
| `doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` | 20 | 1 |
| `doc/validation/ci-review-p1/README.md` | 7, 11, 12, 13, 13, 14, 14, 15, 19, 21, 23, 23, 31, 31, 46 | 15 |
| `doc/validation/ci-review-p1/fix-p1/README.md` | 5, 11, 11, 12, 12, 13, 13, 15, 27, 27 | 10 |
| `doc/validation/ci-review-p1/merge-acceptance/README.md` | 5, 7, 7 | 3 |
| `doc/validation/data-table-importer/README.md` | 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 28, 30, 36, 37, 38, 39, 39, 39, 39 | 25 |
| `doc/validation/data-table-templates/README.md` | 9, 9, 10, 10, 11, 12, 23, 23, 23, 23, 24, 25 | 12 |
| `doc/validation/data-tables/README.md` | 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 38, 38, 38, 38, 40, 40 | 22 |
| `doc/validation/game-scene-entry/README.md` | 21, 25, 25, 25, 27, 27 | 6 |
| `doc/validation/generic-data-tables/README.md` | 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 41, 41, 41, 41, 51, 51 | 16 |
| `doc/validation/input-system/p4/README.md` | 11, 12, 12, 12, 12, 13, 14, 24, 24, 24, 24, 34, 38, 42 | 14 |
| `doc/validation/input-system/p4/final-integration/README.md` | 3, 6, 8, 9 | 4 |
| `doc/validation/resource-manager/README.md` | 9, 10, 11, 12, 13, 14, 14, 15, 15, 16, 16, 37, 38, 39, 39, 39, 40, 42 | 18 |
| `doc/validation/scene-areas/README.md` | 18, 20 | 2 |
| `doc/validation/scene-definitions/README.md` | 20 | 1 |
| `doc/validation/scene-integration/README.md` | 7, 9, 9, 13, 15, 15, 23, 25, 31, 31, 31, 31, 33, 33, 35, 35, 35, 37, 37, 37, 39, 39, 39, 43, 47, 47, 49, 49 | 28 |
| `doc/validation/scene-loaders/README.md` | 21, 21, 23 | 3 |
| `doc/validation/scene-loading/integration/README.md` | 7, 15 | 2 |
| `doc/validation/scene-loading/p4/README.md` | 10, 11, 12, 13, 14, 15, 17, 17, 21, 21, 21, 21, 21, 21, 23, 23, 25 | 17 |
| `doc/validation/scene-replacement/README.md` | 16 | 1 |
| `doc/validation/scene-transition-contracts/README.md` | 14, 16, 26 | 3 |
| `doc/validation/scene-transition-editor/README.md` | 22, 22 | 2 |
| `doc/validation/tplab-name-cleanup/README.md` | 6, 7, 8, 9, 16, 16, 16, 18 | 8 |
| `doc/validation/tplab-naming/README.md` | 11, 15, 15, 16, 16, 17, 17, 18, 19, 19, 21, 31 | 12 |
| `tools/README.md` | 27 | 1 |

문서마다 원본 Git blob에 위 링크 대상 교체만 적용한 예상 바이트와 실제 결과를 비교한다. 표시 텍스트·문장·수치 변경은 0이다. 원시 자료 안의 출처 문자열은 아래 이력 목록으로만 기록하며 바꾸지 않는다.

## (c) 원시 출처 목록

아래 source와 target은 모두 함께 삭제됐다. source는 `doc/validation/` 아래이며 `행=상대 target`이다. `../`는 source 디렉터리 기준이다. 18개 source/135쌍 전부 삭제 목록에 포함됨을 확인했다. 잔존 raw 자료에서 새로 끊기는 해당 출처 문자열은 0개다.

| source | 행=target |
|---|---|
| `async-scene/execution.json` | 70=test-input-sha256.json |
| `scene-root/execution.json` | 53=test-input-sha256.json |
| `scene-integration/sample-authoring-failure.txt` | 7=settings-new-scene-observed.json |
| `ci-review-p1/fix-p1/pr-rules.json` | 26=../baseline.json; 27=../combined-tests.txt; 28=../draft-rename-probe.json; 29=../draft-tests-utf8.txt; 30=../draft-tests.txt; 33=combined-windows-cp949.log; 34=combined-windows.json; 35=combined-windows.txt; 36=fix-policy.json; 37=green-windows-cp949.log; 38=green-windows.json; 39=green-windows.txt; 40=red-windows-cp949.log; 41=red-windows.json; 42=red-windows.txt; 45=reviewer/green.txt; 46=reviewer/red.txt; 47=../gate-green.json; 48=../gate-green.txt; 49=../gate-policy.json; 50=../gate-red.json; 51=../gate-red.txt; 52=../github-annotations-9383e4d.json; 53=../github-ci-9383e4d.json; 54=../pr-rules.json; 55=../pr-rules.txt |
| `data-table-importer/static-checks.json` | 41=compile-final.txt; 42=console-after.json; 43=console-before-clear.json; 44=console-native-after.json; 45=format-check.json; 46=full-EditMode.json; 47=full-EditMode.txt; 48=full-PlayMode.json; 49=full-PlayMode.txt; 50=git-remote-before.json; 51=green-existing-fk.json; 52=green-existing-fk.txt; 53=green-final-targeted.json; 54=green-final-targeted.txt; 55=green-generator.json; 56=green-generator.txt; 57=green-service.json; 58=green-service.txt; 59=green-thread-profile.json; 60=green-thread-profile.txt; 61=green-writer.json; 62=green-writer.txt; 63=native-editor-failed.json; 65=red-existing-fk.json; 66=red-generator.json; 67=red-service.json; 68=red-thread-profile.json; 69=red-writer.json; 71=status-after.txt; 72=test-inputs.json |
| `data-table-templates/static-checks.json` | 31=compile-after.txt; 32=console-after.json; 33=console-before-clear.json; 34=full-EditMode.json; 35=full-EditMode.txt; 36=full-PlayMode.json; 37=full-PlayMode.txt; 38=green.json; 39=green.txt; 40=red.json; 41=red.txt; 43=status-after.txt; 44=test-inputs.json |
| `data-tables/static-checks.json` | 25=async-green.json; 26=async-green.xml; 27=compile-after.txt; 28=console-after.json; 29=expected-test-errors.json; 30=full-EditMode-cli.txt; 31=full-EditMode.json; 32=full-EditMode.xml; 33=full-PlayMode-cli.txt; 34=full-PlayMode.json; 35=full-PlayMode.xml; 36=green-edit.json; 37=green-edit.xml; 38=red-edit.json; 39=red-edit.xml; 40=resource-integration-green.json; 41=resource-integration-green.xml; 43=status-after.txt; 44=test-inputs.json; 45=unused-column-green.json; 46=unused-column-green.xml; 47=unused-column-red.json; 48=unused-column-red.xml |
| `generic-data-tables/static-checks.json` | 44=async-green.json; 45=async-green.txt; 46=binding-green.json; 47=binding-green.txt; 48=binding-red.json; 49=binding-red.txt; 50=codec-green.json; 51=codec-green.txt; 52=codec-red.json; 53=compile-after.txt; 54=console-after.json; 55=console-before-clear.json; 56=full-EditMode.json; 57=full-EditMode.txt; 58=full-PlayMode.json; 59=full-PlayMode.txt; 60=resource-root-green.json; 61=resource-root-green.txt; 62=standard-green.json; 63=standard-green.txt; 64=standard-red.json; 65=standard-red.txt; 67=status-after.txt; 68=test-inputs.json |
| `input-system/p4/scene-player-additive-20261007T033441Z-25664.json` | 58=scene-player-additive-20261007T033441Z-25664.log |
| `input-system/p4/scene-player-single-20261007T033537Z-30492.json` | 58=scene-player-single-20261007T033537Z-30492.log |
| `scene-integration/scene-player-additive-20261006T134438Z-49780.json` | 58=scene-player-additive-20261006T134438Z-49780.log |
| `scene-integration/scene-player-single-20261006T134658Z-49992.json` | 58=scene-player-single-20261006T134658Z-49992.log |
| `scene-loading/p4/player/scene-player-additive-20261007T054303Z-36760.json` | 69=scene-player-additive-20261007T054303Z-36760.log |
| `scene-loading/p4/player/scene-player-single-20261007T054350Z-21564.json` | 69=scene-player-single-20261007T054350Z-21564.log |
| `scene-loading/tplab-naming-consumer-core/preserved-logs.json` | 2=editor-processLog.txt; 3=editor-stdoutLog.txt; 4=player-processLog.txt; 5=player-stdoutLog.txt |
| `scene-loading/tplab-naming-consumer-input/preserved-logs.json` | 2=editor-processLog.txt; 3=editor-stdoutLog.txt; 4=player-processLog.txt; 5=player-stdoutLog.txt |
| `scene-loading/tplab-naming-player-additive/scene-player-additive-20261007T064834Z-31168.json` | 69=scene-player-additive-20261007T064834Z-31168.log |
| `scene-loading/tplab-naming-player-single/scene-player-single-20261007T064926Z-26268.json` | 69=scene-player-single-20261007T064926Z-26268.log |

## 검증 요약

환경은 Windows 로컬 Git checkout·Python 3.14, Unity/Player/IL2CPP 실행은 0회다. 문서·원시 자료 정리이므로 실제 Unity 회귀나 제품 Console 확인을 통과로 주장하지 않는다.

삭제 전 `4704dc03`에서 inline Python 읽기 전용 Markdown/anchor 검사와 `C:\Python314\python.exe tools/verify_validation.py --evidence <각 checks.json 폴더>` 16회 + `C:\Python314\python.exe doc/validation/bootstrap-system/verify.py` 1회를 실행했다. 같은 스크립트로 변경 후 비교한다.

| 검사 | 전 | 후 | 판정 |
|---|---:|---:|---|
| 기존 local 링크 오류 | 6 | 6 | 새 오류 0 |
| anchor 수/새 anchor 오류 | 445/0 | 445/0 | 새 오류 0 |
| verifier 실행·실패 | 17/17 | 17/17 | 각 exit code·stdout·stderr 동일, 새 실패 0 |
| verifier 직접 입력 | 165개 | 165개 | 원시 SHA-256 동일 |
| hash payload와 삭제 교집합 | 0 | 0 | 선언/해당 파일 보존 |
| dirty 변경 | 6개 | 6개 | 경로·원시 SHA-256 동일 |
| 범위 밖 추적 파일 | 기존 원시 SHA-256 | 동일 | 무관한 변경 0 |

기존 링크 오류 6건은 `ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md`의 과거 상대 링크다. verifier의 기존 첫 실패는 구 `Assets/MyLab/Core/DataTables/CsvDataTable.cs` 누락 3건, `Assets/MyLab/Core.meta` 누락 8건, `Assets/TPLab/Core/Lifecycle/ISceneRoot.cs` hash 불일치 6건이다. 기존 17 실패를 통과로 바꾸지 않았다. verifier가 첫 오류에서 중단하므로 과거 검증 전체의 재실행 성공을 증명하지 않으며, 입력 보존과 실패 원문 동일성만 비교했다.

`git diff --check <base> <HEAD>`와 `C:\Python314\python.exe tools/check_pr_rules.py --base <base> --head <HEAD>`, 최종 exact-SHA Actions 결과는 아래 완료 상태에 기록한다. 사용자 dirty의 기존 공백 문제를 전체 diff 검사에 섞지 않는다.

완료 상태: 변경 source commit `3102f417f526578f6ebc2e5884b8e73c882c258c`에서 `check_pr_rules` 오류 0·경고 0, scoped 공백 오류 0, exact-SHA [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38505462682) run 1/check 1 completed/success, 조회 collection 전부 complete·queryIssues/blockingReasons 0을 확인했다. Claude 수동 리뷰 워크플로는 미실행이다. 독립 읽기 전용 diff 검토에서도 핵심 누락·잘못된 주장을 발견하지 못했다. 이 완료 문장 갱신 뒤 최종 tip의 규칙·링크·exact-SHA gate를 다시 확인한다. 최종 commit/검사 metadata는 [PR #18 Checks](https://github.com/KangSungKyu/TPLab/pull/18/checks)에 대응하며 위 검증 source와 구별한다. PR은 open으로 유지하고 병합하지 않는다. 실행 증거 요약은 이 회고에 두고 PR/색인은 상태와 이 문서 링크만 남긴다.

## 삭제 파일 전체 목록

Git blob 바이트로 측정했다. `git rm -- <명시 파일>`만 실행했고 폴더 와일드카드·reset·clean·stash·이력 재작성은 사용하지 않았다. 파일 이력은 위 SHA에서 복원 가능하다.

| 파일 | 바이트 |
|---|---:|
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/editor-build.json` | 330 |
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/editor.log` | 347,491 |
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/manifest.json` | 437 |
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/packages-lock.json` | 2,761 |
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/player-smoke.json` | 372 |
| `doc/validation/scene-integration/CoreConsumer-P6-20261006-02/player.log` | 1,589 |
| `doc/validation/scene-integration/additive-build-native.log` | 906,864 |
| `doc/validation/scene-integration/additive-failure-held-after-shutdown.png` | 81,349 |
| `doc/validation/scene-integration/additive-failure-held-final.png` | 38,664 |
| `doc/validation/scene-integration/additive-ready.png` | 78,169 |
| `doc/validation/scene-integration/automatic-restore.json` | 122 |
| `doc/validation/scene-integration/build-additive.json` | 431 |
| `doc/validation/scene-integration/build-settings-refresh-drift.txt` | 401 |
| `doc/validation/scene-integration/build-side-effect-restoration.json` | 1,910 |
| `doc/validation/scene-integration/build-single.json` | 427 |
| `doc/validation/scene-integration/ci-policy.json` | 359 |
| `doc/validation/scene-integration/core-consumer-20261006T123529Z-10048.json` | 23,307 |
| `doc/validation/scene-integration/core-consumer-20261006T123935Z-47452.json` | 24,728 |
| `doc/validation/scene-integration/editor-additive-final-smoke.json` | 1,162 |
| `doc/validation/scene-integration/editor-additive-smoke.json` | 1,162 |
| `doc/validation/scene-integration/editor-single-smoke.json` | 1,159 |
| `doc/validation/scene-integration/expected-regression-console.json` | 12,030 |
| `doc/validation/scene-integration/final-integration/acceptance-commit-ci-policy.json` | 359 |
| `doc/validation/scene-integration/final-integration/acceptance.json` | 616 |
| `doc/validation/scene-integration/final-integration/branch-tips-before-final-record.json` | 914 |
| `doc/validation/scene-integration/final-integration/build-guid-before-restoration.txt` | 401 |
| `doc/validation/scene-integration/final-integration/console.json` | 3 |
| `doc/validation/scene-integration/final-integration/source-ci-policy.json` | 359 |
| `doc/validation/scene-integration/final-integration/status.txt` | 110 |
| `doc/validation/scene-integration/fixture-failure-reload.json` | 663 |
| `doc/validation/scene-integration/identity-fixture-failure-reload.json` | 4,025 |
| `doc/validation/scene-integration/initial-authoring-asset-guids.json` | 495 |
| `doc/validation/scene-integration/native-checks.json` | 450 |
| `doc/validation/scene-integration/reload-observed.json` | 6,941 |
| `doc/validation/scene-integration/sample-asset-guids.json` | 814 |
| `doc/validation/scene-integration/sample-authoring-failure.txt` | 878 |
| `doc/validation/scene-integration/sample-discovery-zero.json` | 771 |
| `doc/validation/scene-integration/sample-meta-import-diagnostics.txt` | 2,445 |
| `doc/validation/scene-integration/scene-player-additive-20261006T134438Z-49780.json` | 2,134 |
| `doc/validation/scene-integration/scene-player-additive-20261006T134438Z-49780.log` | 2,500 |
| `doc/validation/scene-integration/scene-player-single-20261006T134658Z-49992.json` | 2,121 |
| `doc/validation/scene-integration/scene-player-single-20261006T134658Z-49992.log` | 4,195 |
| `doc/validation/scene-integration/settings-new-scene-observed.json` | 158 |
| `doc/validation/scene-integration/temporary-cleanup.json` | 758 |
| `doc/validation/scene-loading/integration/current-console.json` | 3 |
| `doc/validation/scene-loading/integration/document-checks.json` | 820 |
| `doc/validation/scene-loading/integration/phase-cleanup.json` | 728 |
| `doc/validation/scene-loading/integration/phase-tips.json` | 421 |
| `doc/validation/scene-loading/integration/postmerge-github-ci.json` | 359 |
| `doc/validation/scene-loading/integration/premerge-github-ci.json` | 359 |
| `doc/validation/scene-loading/p1/green-observer.json` | 1,723 |
| `doc/validation/scene-loading/p1/green-play.json` | 21,276 |
| `doc/validation/scene-loading/p1/preserved-inputs.json` | 787 |
| `doc/validation/scene-loading/p1/red-observer.json` | 2,417 |
| `doc/validation/scene-loading/p1/red-play.json` | 21,795 |
| `doc/validation/scene-loading/p2/green-core.json` | 22,732 |
| `doc/validation/scene-loading/p2/green-edit.json` | 25,657 |
| `doc/validation/scene-loading/p2/green-final-flow.json` | 4,284 |
| `doc/validation/scene-loading/p2/green-flow.json` | 3,449 |
| `doc/validation/scene-loading/p2/red-flow.json` | 3,826 |
| `doc/validation/scene-loading/p4/build-additive.json` | 441 |
| `doc/validation/scene-loading/p4/build-generated-restoration.json` | 327 |
| `doc/validation/scene-loading/p4/build-single.json` | 437 |
| `doc/validation/scene-loading/p4/consumer/core-consumer-20261007T054058Z-35276.json` | 29,322 |
| `doc/validation/scene-loading/p4/consumer/logs/editor-stdout.log` | 0 |
| `doc/validation/scene-loading/p4/consumer/logs/editor.log` | 448,475 |
| `doc/validation/scene-loading/p4/consumer/logs/player-stdout.log` | 1,741 |
| `doc/validation/scene-loading/p4/consumer/logs/player.log` | 1,547 |
| `doc/validation/scene-loading/p4/final-state.txt` | 81 |
| `doc/validation/scene-loading/p4/full-edit.json` | 27,501 |
| `doc/validation/scene-loading/p4/green-ui-bound-module.json` | 1,772 |
| `doc/validation/scene-loading/p4/green-ui-fixed-target.json` | 2,916 |
| `doc/validation/scene-loading/p4/green-ui.json` | 2,697 |
| `doc/validation/scene-loading/p4/player/scene-player-additive-20261007T054303Z-36760.json` | 2,609 |
| `doc/validation/scene-loading/p4/player/scene-player-additive-20261007T054303Z-36760.log` | 2,530 |
| `doc/validation/scene-loading/p4/player/scene-player-single-20261007T054350Z-21564.json` | 2,596 |
| `doc/validation/scene-loading/p4/player/scene-player-single-20261007T054350Z-21564.log` | 4,224 |
| `doc/validation/scene-loading/p4/preclear-test-console.txt` | 3 |
| `doc/validation/scene-loading/p4/red-ui.json` | 2,262 |
| `doc/validation/scene-loading/p4/review-red-ready-ui.json` | 770 |
| `doc/validation/scene-loading/p4/source-github-ci.json` | 359 |
| `doc/validation/scene-loading/tplab-naming-build-additive.json` | 438 |
| `doc/validation/scene-loading/tplab-naming-build-single.json` | 434 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/core-consumer-20261007T064913Z-19632.json` | 26,219 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/editor-processLog.txt` | 347,038 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/editor-stdoutLog.txt` | 0 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/player-processLog.txt` | 1,532 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/player-stdoutLog.txt` | 1,741 |
| `doc/validation/scene-loading/tplab-naming-consumer-core/preserved-logs.json` | 415 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/core-consumer-20261007T064538Z-36104.json` | 29,304 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/editor-processLog.txt` | 446,947 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/editor-stdoutLog.txt` | 0 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/player-processLog.txt` | 1,535 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/player-stdoutLog.txt` | 1,741 |
| `doc/validation/scene-loading/tplab-naming-consumer-input/preserved-logs.json` | 419 |
| `doc/validation/scene-loading/tplab-naming-player-additive/scene-player-additive-20261007T064834Z-31168.json` | 2,633 |
| `doc/validation/scene-loading/tplab-naming-player-additive/scene-player-additive-20261007T064834Z-31168.log` | 2,521 |
| `doc/validation/scene-loading/tplab-naming-player-single/scene-player-single-20261007T064926Z-26268.json` | 2,619 |
| `doc/validation/scene-loading/tplab-naming-player-single/scene-player-single-20261007T064926Z-26268.log` | 4,226 |
| `doc/validation/scene-transition-editor/ci-policy.json` | 359 |
| `doc/validation/scene-transition-editor/expected-native-console.json` | 3,624 |
| `doc/validation/scene-transition-editor/expected-test-console.json` | 8,698 |
| `doc/validation/scene-transition-editor/native-editor.json` | 432 |
| `doc/validation/scene-transition-editor/native-execution.json` | 610 |
| `doc/validation/scene-definitions/ci-policy.json` | 359 |
| `doc/validation/scene-definitions/expected-test-console.json` | 12,030 |
| `doc/validation/scene-loaders/ci-policy.json` | 359 |
| `doc/validation/scene-loaders/expected-test-console.json` | 8,190 |
| `doc/validation/scene-loaders/native-editor.json` | 355 |
| `doc/validation/scene-loaders/native-expected-console.json` | 3,326 |
| `doc/validation/scene-areas/ci-policy.json` | 359 |
| `doc/validation/scene-areas/driver-checks.json` | 611 |
| `doc/validation/scene-areas/expected-test-console.json` | 8,070 |
| `doc/validation/scene-replacement/ci-policy.json` | 359 |
| `doc/validation/scene-replacement/expected-test-console.json` | 8,190 |
| `doc/validation/scene-transition-contracts/ci-baseline.json` | 359 |
| `doc/validation/scene-transition-contracts/execution.json` | 953 |
| `doc/validation/scene-transition-contracts/expected-test-console.json` | 8,070 |
| `doc/validation/scene-root/console-errors.json` | 3 |
| `doc/validation/scene-root/execution.json` | 1,621 |
| `doc/validation/scene-root/expected-test-exceptions.json` | 7,207 |
| `doc/validation/scene-root/green-edit.json` | 3,729 |
| `doc/validation/scene-root/green-play.json` | 3,803 |
| `doc/validation/scene-root/red-editor.json` | 544 |
| `doc/validation/scene-root/red-play.json` | 2,473 |
| `doc/validation/scene-root/reload-check.json` | 1,897 |
| `doc/validation/scene-root/test-input-sha256.json` | 5,377 |
| `doc/validation/async-scene/console-errors.json` | 3 |
| `doc/validation/async-scene/execution.json` | 2,344 |
| `doc/validation/async-scene/expected-test-exceptions.json` | 7,209 |
| `doc/validation/async-scene/green-edit.json` | 3,729 |
| `doc/validation/async-scene/green-play.json` | 5,190 |
| `doc/validation/async-scene/green-targeted.json` | 1,488 |
| `doc/validation/async-scene/red-play.json` | 1,773 |
| `doc/validation/async-scene/red-reveal.json` | 1,562 |
| `doc/validation/async-scene/reload-check.json` | 2,207 |
| `doc/validation/async-scene/test-input-sha256.json` | 5,699 |
| `doc/validation/game-scene-entry/ci-policy.json` | 359 |
| `doc/validation/game-scene-entry/execution.json` | 1,632 |
| `doc/validation/game-scene-entry/expected-test-console.json` | 11,800 |
| `doc/validation/game-scene-entry/iteration-bootstrap.json` | 1,979 |
| `doc/validation/game-scene-entry/iteration-entry.json` | 2,323 |
| `doc/validation/game-scene-entry/iteration-notes.json` | 852 |
| `doc/validation/game-scene-entry/native-expected-console.json` | 5,712 |
| `doc/validation/input-system/p1/green-edit-2.json` | 1,991 |
| `doc/validation/input-system/p1/green-edit-final.json` | 1,691 |
| `doc/validation/input-system/p1/green-edit.json` | 1,842 |
| `doc/validation/input-system/p1/green-play.json` | 602 |
| `doc/validation/input-system/p1/red-edit.json` | 3,209 |
| `doc/validation/input-system/p2/green-edit-corrected.json` | 2,279 |
| `doc/validation/input-system/p2/green-edit-final.json` | 2,279 |
| `doc/validation/input-system/p2/green-edit.json` | 1,102 |
| `doc/validation/input-system/p2/green-play-corrected.json` | 1,442 |
| `doc/validation/input-system/p2/green-play-final.json` | 1,469 |
| `doc/validation/input-system/p2/green-play.json` | 1,472 |
| `doc/validation/input-system/p2/red-edit.json` | 1,915 |
| `doc/validation/input-system/p2/red-play.json` | 1,355 |
| `doc/validation/input-system/p3/green-edit.json` | 2,279 |
| `doc/validation/input-system/p3/green-play-final.json` | 1,860 |
| `doc/validation/input-system/p3/green-play.json` | 1,862 |
| `doc/validation/input-system/p3/red-play.json` | 1,196 |
| `doc/validation/input-system/p3/sample-regression-play.json` | 721 |
| `doc/validation/input-system/p4/before-final-reload-state.txt` | 176 |
| `doc/validation/input-system/p4/before-review-EditMode.json` | 26,276 |
| `doc/validation/input-system/p4/before-review-PlayMode.json` | 21,655 |
| `doc/validation/input-system/p4/before-review-reload.json` | 8,757 |
| `doc/validation/input-system/p4/build-additive.json` | 433 |
| `doc/validation/input-system/p4/build-side-effect-restoration.json` | 2,078 |
| `doc/validation/input-system/p4/build-single.json` | 429 |
| `doc/validation/input-system/p4/consumer-excluded/core-consumer-20261007T031416Z-28236.json` | 24,753 |
| `doc/validation/input-system/p4/consumer-excluded/diagnostics.json` | 794 |
| `doc/validation/input-system/p4/consumer-excluded/editor-result.json` | 330 |
| `doc/validation/input-system/p4/consumer-excluded/manifest.json` | 437 |
| `doc/validation/input-system/p4/consumer-excluded/packages-lock.json` | 2,761 |
| `doc/validation/input-system/p4/consumer-excluded/player-result.json` | 372 |
| `doc/validation/input-system/p4/consumer-excluded/player.log` | 1,589 |
| `doc/validation/input-system/p4/consumer-included-final/core-consumer-20261007T033305Z-23128.json` | 27,863 |
| `doc/validation/input-system/p4/consumer-included-final/diagnostics.json` | 794 |
| `doc/validation/input-system/p4/consumer-included-final/editor-result.json` | 336 |
| `doc/validation/input-system/p4/consumer-included-final/manifest.json` | 521 |
| `doc/validation/input-system/p4/consumer-included-final/packages-lock.json` | 3,792 |
| `doc/validation/input-system/p4/consumer-included-final/player-result.json` | 404 |
| `doc/validation/input-system/p4/consumer-included-final/player.log` | 1,607 |
| `doc/validation/input-system/p4/consumer-included/core-consumer-20261007T031939Z-19668.json` | 27,827 |
| `doc/validation/input-system/p4/consumer-included/diagnostics.json` | 794 |
| `doc/validation/input-system/p4/consumer-included/editor-result.json` | 330 |
| `doc/validation/input-system/p4/consumer-included/manifest.json` | 521 |
| `doc/validation/input-system/p4/consumer-included/packages-lock.json` | 3,792 |
| `doc/validation/input-system/p4/consumer-included/player-result.json` | 404 |
| `doc/validation/input-system/p4/consumer-included/player.log` | 1,589 |
| `doc/validation/input-system/p4/expected-regression-console.json` | 8,190 |
| `doc/validation/input-system/p4/final-editor-state.txt` | 144 |
| `doc/validation/input-system/p4/final-integration/acceptance.json` | 650 |
| `doc/validation/input-system/p4/final-integration/documentation-ci-policy.json` | 359 |
| `doc/validation/input-system/p4/final-integration/phase-branch-cleanup.json` | 2,295 |
| `doc/validation/input-system/p4/final-integration/source-ci-policy.json` | 359 |
| `doc/validation/input-system/p4/reload-observed.json` | 8,758 |
| `doc/validation/input-system/p4/scene-player-additive-20261007T033441Z-25664.json` | 2,116 |
| `doc/validation/input-system/p4/scene-player-additive-20261007T033441Z-25664.log` | 2,506 |
| `doc/validation/input-system/p4/scene-player-single-20261007T033537Z-30492.json` | 2,104 |
| `doc/validation/input-system/p4/scene-player-single-20261007T033537Z-30492.log` | 4,201 |
| `doc/validation/input-system/p4/source-ci-policy.json` | 359 |
| `doc/validation/input-system/p4/temporary-cleanup.json` | 568 |
| `doc/validation/data-tables/async-green.json` | 176 |
| `doc/validation/data-tables/async-green.xml` | 9,018 |
| `doc/validation/data-tables/compile-after.txt` | 79 |
| `doc/validation/data-tables/console-after.json` | 3 |
| `doc/validation/data-tables/expected-test-errors.json` | 12,030 |
| `doc/validation/data-tables/full-EditMode-cli.txt` | 5,814 |
| `doc/validation/data-tables/full-EditMode.json` | 178 |
| `doc/validation/data-tables/full-EditMode.xml` | 53,584 |
| `doc/validation/data-tables/full-PlayMode-cli.txt` | 7,684 |
| `doc/validation/data-tables/full-PlayMode.json` | 178 |
| `doc/validation/data-tables/full-PlayMode.xml` | 71,573 |
| `doc/validation/data-tables/green-edit.json` | 178 |
| `doc/validation/data-tables/green-edit.xml` | 16,722 |
| `doc/validation/data-tables/red-edit.json` | 4,418 |
| `doc/validation/data-tables/red-edit.xml` | 39,103 |
| `doc/validation/data-tables/resource-integration-green.json` | 178 |
| `doc/validation/data-tables/resource-integration-green.xml` | 16,441 |
| `doc/validation/data-tables/static-checks.json` | 2,685 |
| `doc/validation/data-tables/status-after.txt` | 110 |
| `doc/validation/data-tables/test-inputs.json` | 6,770 |
| `doc/validation/data-tables/unused-column-green.json` | 178 |
| `doc/validation/data-tables/unused-column-green.xml` | 17,491 |
| `doc/validation/data-tables/unused-column-red.json` | 378 |
| `doc/validation/data-tables/unused-column-red.xml` | 18,859 |
| `doc/validation/data-table-importer/compile-final.txt` | 79 |
| `doc/validation/data-table-importer/console-after.json` | 3 |
| `doc/validation/data-table-importer/console-before-clear.json` | 8,190 |
| `doc/validation/data-table-importer/console-native-after.json` | 3 |
| `doc/validation/data-table-importer/format-check.json` | 636 |
| `doc/validation/data-table-importer/full-EditMode.json` | 15,745 |
| `doc/validation/data-table-importer/full-EditMode.txt` | 15,745 |
| `doc/validation/data-table-importer/full-PlayMode.json` | 8,169 |
| `doc/validation/data-table-importer/full-PlayMode.txt` | 8,169 |
| `doc/validation/data-table-importer/git-remote-before.json` | 435 |
| `doc/validation/data-table-importer/green-existing-fk.json` | 4,234 |
| `doc/validation/data-table-importer/green-existing-fk.txt` | 4,234 |
| `doc/validation/data-table-importer/green-final-targeted.json` | 4,530 |
| `doc/validation/data-table-importer/green-final-targeted.txt` | 4,530 |
| `doc/validation/data-table-importer/green-generator.json` | 2,326 |
| `doc/validation/data-table-importer/green-generator.txt` | 2,326 |
| `doc/validation/data-table-importer/green-service.json` | 3,615 |
| `doc/validation/data-table-importer/green-service.txt` | 3,615 |
| `doc/validation/data-table-importer/green-thread-profile.json` | 4,423 |
| `doc/validation/data-table-importer/green-thread-profile.txt` | 4,423 |
| `doc/validation/data-table-importer/green-writer.json` | 3,091 |
| `doc/validation/data-table-importer/green-writer.txt` | 3,091 |
| `doc/validation/data-table-importer/native-editor-failed.json` | 539 |
| `doc/validation/data-table-importer/red-existing-fk.json` | 293 |
| `doc/validation/data-table-importer/red-generator.json` | 270 |
| `doc/validation/data-table-importer/red-service.json` | 257 |
| `doc/validation/data-table-importer/red-thread-profile.json` | 325 |
| `doc/validation/data-table-importer/red-writer.json` | 260 |
| `doc/validation/data-table-importer/static-checks.json` | 3,850 |
| `doc/validation/data-table-importer/status-after.txt` | 110 |
| `doc/validation/data-table-importer/test-inputs.json` | 9,288 |
| `doc/validation/generic-data-tables/async-green.json` | 442 |
| `doc/validation/generic-data-tables/async-green.txt` | 489 |
| `doc/validation/generic-data-tables/binding-green.json` | 2,106 |
| `doc/validation/generic-data-tables/binding-green.txt` | 2,106 |
| `doc/validation/generic-data-tables/binding-red.json` | 4,448 |
| `doc/validation/generic-data-tables/binding-red.txt` | 4,362 |
| `doc/validation/generic-data-tables/codec-green.json` | 1,140 |
| `doc/validation/generic-data-tables/codec-green.txt` | 1,140 |
| `doc/validation/generic-data-tables/codec-red.json` | 245 |
| `doc/validation/generic-data-tables/compile-after.txt` | 359 |
| `doc/validation/generic-data-tables/console-after.json` | 3 |
| `doc/validation/generic-data-tables/console-before-clear.json` | 8,190 |
| `doc/validation/generic-data-tables/full-EditMode.json` | 10,914 |
| `doc/validation/generic-data-tables/full-EditMode.txt` | 10,914 |
| `doc/validation/generic-data-tables/full-PlayMode.json` | 8,169 |
| `doc/validation/generic-data-tables/full-PlayMode.txt` | 8,216 |
| `doc/validation/generic-data-tables/resource-root-green.json` | 1,940 |
| `doc/validation/generic-data-tables/resource-root-green.txt` | 1,987 |
| `doc/validation/generic-data-tables/standard-green.json` | 2,043 |
| `doc/validation/generic-data-tables/standard-green.txt` | 2,043 |
| `doc/validation/generic-data-tables/standard-red.json` | 3,902 |
| `doc/validation/generic-data-tables/standard-red.txt` | 3,824 |
| `doc/validation/generic-data-tables/static-checks.json` | 3,398 |
| `doc/validation/generic-data-tables/status-after.txt` | 110 |
| `doc/validation/generic-data-tables/test-inputs.json` | 7,843 |
| `doc/validation/data-table-templates/compile-after.txt` | 359 |
| `doc/validation/data-table-templates/console-after.json` | 3 |
| `doc/validation/data-table-templates/console-before-clear.json` | 8,190 |
| `doc/validation/data-table-templates/full-EditMode.json` | 11,316 |
| `doc/validation/data-table-templates/full-EditMode.txt` | 11,316 |
| `doc/validation/data-table-templates/full-PlayMode.json` | 8,169 |
| `doc/validation/data-table-templates/full-PlayMode.txt` | 8,216 |
| `doc/validation/data-table-templates/green.json` | 2,557 |
| `doc/validation/data-table-templates/green.txt` | 2,557 |
| `doc/validation/data-table-templates/red.json` | 3,169 |
| `doc/validation/data-table-templates/red.txt` | 3,062 |
| `doc/validation/data-table-templates/static-checks.json` | 1,984 |
| `doc/validation/data-table-templates/status-after.txt` | 110 |
| `doc/validation/data-table-templates/test-inputs.json` | 7,998 |
| `doc/validation/resource-manager/asset-green.json` | 781 |
| `doc/validation/resource-manager/compile-after.txt` | 79 |
| `doc/validation/resource-manager/console-after.json` | 3 |
| `doc/validation/resource-manager/expected-test-errors.json` | 4,824 |
| `doc/validation/resource-manager/full-EditMode-cli.txt` | 3,729 |
| `doc/validation/resource-manager/full-EditMode.json` | 3,956 |
| `doc/validation/resource-manager/full-EditMode.xml` | 38,635 |
| `doc/validation/resource-manager/full-PlayMode-cli.txt` | 6,681 |
| `doc/validation/resource-manager/full-PlayMode.json` | 6,923 |
| `doc/validation/resource-manager/full-PlayMode.xml` | 63,550 |
| `doc/validation/resource-manager/full-expected-test-errors.json` | 12,030 |
| `doc/validation/resource-manager/installer-green.json` | 1,227 |
| `doc/validation/resource-manager/installer-red.json` | 1,435 |
| `doc/validation/resource-manager/red.json` | 1,923 |
| `doc/validation/resource-manager/static-checks.json` | 201 |
| `doc/validation/resource-manager/status-after.txt` | 110 |
| `doc/validation/resource-manager/targeted-green.json` | 1,663 |
| `doc/validation/resource-manager/targeted-green.xml` | 14,904 |
| `doc/validation/resource-manager/test-inputs.json` | 5,982 |
| `doc/validation/resource-manager/thread-red.json` | 330 |
| `doc/validation/generic-pool/console-errors.json` | 3 |
| `doc/validation/generic-pool/dotnet-probe.txt` | 66 |
| `doc/validation/generic-pool/green-edit.json` | 3,493 |
| `doc/validation/generic-pool/green-play.json` | 1,520 |
| `doc/validation/generic-pool/play-recovery.json` | 369 |
| `doc/validation/generic-pool/red-generic.json` | 3,438 |
| `doc/validation/generic-pool/test-input-sha256.json` | 2,271 |
| `doc/validation/generic-pool/unity-contract.json` | 809 |
| `doc/validation/object-pool/console-errors.json` | 3 |
| `doc/validation/object-pool/green-edit.json` | 689 |
| `doc/validation/object-pool/green-play.json` | 1,520 |
| `doc/validation/object-pool/red-edit.json` | 956 |
| `doc/validation/object-pool/red-play.json` | 2,334 |
| `doc/validation/object-pool/test-input-sha256.json` | 1,489 |
| `doc/validation/singleton/console-errors.json` | 2 |
| `doc/validation/singleton/execution.json` | 1,247 |
| `doc/validation/singleton/expected-test-exceptions.json` | 5,204 |
| `doc/validation/singleton/green-edit.json` | 3,492 |
| `doc/validation/singleton/green-play.json` | 2,842 |
| `doc/validation/singleton/red-play.json` | 2,406 |
| `doc/validation/singleton/reload-check.json` | 1,287 |
| `doc/validation/singleton/test-input-sha256.json` | 2,810 |
| `doc/validation/bootstrap-system/ci-policy.json` | 161 |
| `doc/validation/bootstrap-system/editmode-undo-warnings.json` | 3,789 |
| `doc/validation/bootstrap-system/green-play-recovered.json` | 656 |
| `doc/validation/bootstrap-system/iteration-notes.json` | 992 |
| `doc/validation/bootstrap-system/late-cancel-failure.json` | 9,487 |
| `doc/validation/bootstrap-system/native-expected-console.json` | 3,722 |
| `doc/validation/bootstrap-system/red-config.json` | 8,044 |
| `doc/validation/bootstrap-system/red-play.json` | 1,142 |
| `doc/validation/bootstrap-system/red-shutdown-cover.json` | 241 |
| `doc/validation/bootstrap-system/red-singleton.json` | 2,150 |
| `doc/validation/tplab-naming/baseline.json` | 24,261 |
| `doc/validation/tplab-naming/build-settings-restoration.json` | 1,107 |
| `doc/validation/tplab-naming/integration-ci.json` | 359 |
| `doc/validation/tplab-naming/native-scenes.json` | 1,166 |
| `doc/validation/tplab-naming/red-edit.json` | 27,785 |
| `doc/validation/tplab-naming/rename-static.json` | 22,295 |
| `doc/validation/tplab-naming/source-ci.json` | 359 |
| `doc/validation/tplab-name-cleanup/changed-documents.json` | 252 |
| `doc/validation/tplab-name-cleanup/final-console.json` | 3 |
| `doc/validation/tplab-name-cleanup/final-status.txt` | 110 |
| `doc/validation/tplab-name-cleanup/integrated-commit-ci.json` | 359 |
| `doc/validation/tplab-name-cleanup/integration-ci.json` | 359 |
| `doc/validation/tplab-name-cleanup/local-project-setting.json` | 458 |
| `doc/validation/tplab-name-cleanup/native-name-check.json` | 243 |
| `doc/validation/tplab-name-cleanup/observed-build-settings.json` | 260 |
| `doc/validation/tplab-name-cleanup/preserved-inputs.json` | 559 |
| `doc/validation/tplab-name-cleanup/removed-generated-files.json` | 2,233 |
| `doc/validation/tplab-name-cleanup/static-checks.json` | 559 |
| `doc/validation/ci-review-p1/baseline.json` | 1,665 |
| `doc/validation/ci-review-p1/combined-tests.txt` | 6,384 |
| `doc/validation/ci-review-p1/draft-rename-probe.json` | 329 |
| `doc/validation/ci-review-p1/draft-tests-utf8.txt` | 2,594 |
| `doc/validation/ci-review-p1/draft-tests.txt` | 2,400 |
| `doc/validation/ci-review-p1/fix-p1/combined-windows-cp949.log` | 6,769 |
| `doc/validation/ci-review-p1/fix-p1/combined-windows.json` | 712 |
| `doc/validation/ci-review-p1/fix-p1/combined-windows.txt` | 6,909 |
| `doc/validation/ci-review-p1/fix-p1/fix-policy.json` | 1,184 |
| `doc/validation/ci-review-p1/fix-p1/green-windows-cp949.log` | 2,979 |
| `doc/validation/ci-review-p1/fix-p1/green-windows.json` | 691 |
| `doc/validation/ci-review-p1/fix-p1/green-windows.txt` | 3,119 |
| `doc/validation/ci-review-p1/fix-p1/pr-rules.json` | 2,938 |
| `doc/validation/ci-review-p1/fix-p1/pr-rules.txt` | 1,929 |
| `doc/validation/ci-review-p1/fix-p1/red-windows-cp949.log` | 4,981 |
| `doc/validation/ci-review-p1/fix-p1/red-windows.json` | 1,089 |
| `doc/validation/ci-review-p1/fix-p1/red-windows.txt` | 4,981 |
| `doc/validation/ci-review-p1/fix-p1/reviewer/green.txt` | 978 |
| `doc/validation/ci-review-p1/fix-p1/reviewer/red.txt` | 5,119 |
| `doc/validation/ci-review-p1/gate-green.json` | 483 |
| `doc/validation/ci-review-p1/gate-green.txt` | 3,894 |
| `doc/validation/ci-review-p1/gate-policy.json` | 2,379 |
| `doc/validation/ci-review-p1/gate-red.json` | 483 |
| `doc/validation/ci-review-p1/gate-red.txt` | 25,736 |
| `doc/validation/ci-review-p1/github-annotations-9383e4d.json` | 4,091 |
| `doc/validation/ci-review-p1/github-ci-9383e4d.json` | 22,900 |
| `doc/validation/ci-review-p1/merge-acceptance/approved-pr.json` | 336 |
| `doc/validation/ci-review-p1/merge-acceptance/merge-tests-cp949.log` | 6,769 |
| `doc/validation/ci-review-p1/merge-acceptance/merge-tests.json` | 789 |
| `doc/validation/ci-review-p1/merge-acceptance/merge-tests.txt` | 6,909 |
| `doc/validation/ci-review-p1/pr-rules.json` | 1,305 |
| `doc/validation/ci-review-p1/pr-rules.txt` | 1,920 |
| `doc/validation/documentation/public-api-checks.json` | 725 |
| `doc/validation/bootstrap-additive-design.json` | 1,205 |

## 보존 파일 전체 목록

현재 영역의 163개다. 직접 입력 165개 전체 원시 hash와 영역 밖 파일의 원시 hash도 별도로 비교했다. `ci-review-p1/CI_GATE_PROPOSAL.md`와 해당 영역의 모든 Markdown을 보존했다(요청한 링크 대상만 변경).

| 파일 | 바이트 | 이유 |
|---|---:|---|
| `doc/validation/scene-integration/README.md` | 10,702 | Markdown 보존 |
| `doc/validation/scene-integration/checks.json` | 1,507 | verifier 직접 입력 |
| `doc/validation/scene-integration/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-integration/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-integration/full-EditMode.json` | 24,478 | verifier 직접 입력 |
| `doc/validation/scene-integration/full-PlayMode.json` | 20,420 | verifier 직접 입력 |
| `doc/validation/scene-integration/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-integration/sample-final-green-play.json` | 757 | verifier 직접 입력 |
| `doc/validation/scene-integration/sample-green-play.json` | 723 | verifier 직접 입력 |
| `doc/validation/scene-integration/sample-red-play.json` | 843 | verifier 직접 입력 |
| `doc/validation/scene-integration/test-inputs.json` | 36,832 | verifier 직접 입력 |
| `doc/validation/scene-loading/integration/README.md` | 2,479 | Markdown 보존 |
| `doc/validation/scene-loading/p4/README.md` | 5,196 | Markdown 보존 |
| `doc/validation/scene-loading/p4/checks.json` | 1,155 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/final-edit.json` | 27,501 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/full-play.json` | 25,772 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/preserved-inputs.json` | 787 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/reload-ui.json` | 1,928 | verifier 직접 입력 |
| `doc/validation/scene-loading/p4/test-inputs.json` | 41,339 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/README.md` | 4,377 | Markdown 보존 |
| `doc/validation/scene-transition-editor/checks.json` | 1,926 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/default-corrected-red-edit.json` | 918 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/fixture-failure-edit.json` | 2,644 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/full-EditMode.json` | 24,478 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/full-PlayMode.json` | 20,051 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/green-edit.json` | 1,834 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/initial-full-EditMode.json` | 24,238 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/initial-green-edit.json` | 1,594 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/pre-cleanup-full-EditMode.json` | 24,478 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/pre-cleanup-full-PlayMode.json` | 20,085 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/red-edit.json` | 4,376 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/supplemental-green-failure-edit.json` | 2,051 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/supplemental-red-edit.json` | 2,094 | verifier 직접 입력 |
| `doc/validation/scene-transition-editor/test-inputs.json` | 28,584 | verifier 직접 입력 |
| `doc/validation/scene-definitions/README.md` | 3,094 | Markdown 보존 |
| `doc/validation/scene-definitions/checks.json` | 1,747 | verifier 직접 입력 |
| `doc/validation/scene-definitions/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-definitions/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-definitions/full-EditMode.json` | 23,146 | verifier 직접 입력 |
| `doc/validation/scene-definitions/full-PlayMode.json` | 20,051 | verifier 직접 입력 |
| `doc/validation/scene-definitions/getters-red-edit.json` | 961 | verifier 직접 입력 |
| `doc/validation/scene-definitions/green-edit.json` | 1,982 | verifier 직접 입력 |
| `doc/validation/scene-definitions/green-play.json` | 3,297 | verifier 직접 입력 |
| `doc/validation/scene-definitions/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-definitions/red-edit.json` | 4,747 | verifier 직접 입력 |
| `doc/validation/scene-definitions/red-play.json` | 5,907 | verifier 직접 입력 |
| `doc/validation/scene-definitions/reveal-red-play.json` | 793 | verifier 직접 입력 |
| `doc/validation/scene-definitions/test-inputs.json` | 27,773 | verifier 직접 입력 |
| `doc/validation/scene-loaders/README.md` | 4,828 | Markdown 보존 |
| `doc/validation/scene-loaders/checks.json` | 1,887 | verifier 직접 입력 |
| `doc/validation/scene-loaders/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-loaders/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-loaders/full-EditMode.json` | 21,506 | verifier 직접 입력 |
| `doc/validation/scene-loaders/full-PlayMode.json` | 12,465 | verifier 직접 입력 |
| `doc/validation/scene-loaders/green-edit-initial.json` | 2,005 | verifier 직접 입력 |
| `doc/validation/scene-loaders/green-editor.json` | 1,641 | verifier 직접 입력 |
| `doc/validation/scene-loaders/green-play-initial.json` | 1,470 | verifier 직접 입력 |
| `doc/validation/scene-loaders/green-play.json` | 1,684 | verifier 직접 입력 |
| `doc/validation/scene-loaders/iteration-dependency-release.json` | 1,724 | verifier 직접 입력 |
| `doc/validation/scene-loaders/iteration-full-PlayMode.json` | 13,107 | verifier 직접 입력 |
| `doc/validation/scene-loaders/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-loaders/red-edit.json` | 2,878 | verifier 직접 입력 |
| `doc/validation/scene-loaders/red-editor-initial.json` | 1,384 | verifier 직접 입력 |
| `doc/validation/scene-loaders/red-editor.json` | 1,529 | verifier 직접 입력 |
| `doc/validation/scene-loaders/test-inputs.json` | 23,752 | verifier 직접 입력 |
| `doc/validation/scene-areas/README.md` | 3,476 | Markdown 보존 |
| `doc/validation/scene-areas/checks.json` | 1,220 | verifier 직접 입력 |
| `doc/validation/scene-areas/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-areas/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-areas/full-EditMode.json` | 21,668 | verifier 직접 입력 |
| `doc/validation/scene-areas/full-PlayMode.json` | 17,290 | verifier 직접 입력 |
| `doc/validation/scene-areas/green-play.json` | 3,256 | verifier 직접 입력 |
| `doc/validation/scene-areas/initial-green-play.json` | 2,993 | verifier 직접 입력 |
| `doc/validation/scene-areas/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-areas/red-play.json` | 4,065 | verifier 직접 입력 |
| `doc/validation/scene-areas/reveal-red-play.json` | 759 | verifier 직접 입력 |
| `doc/validation/scene-areas/test-inputs.json` | 25,288 | verifier 직접 입력 |
| `doc/validation/scene-replacement/README.md` | 3,246 | Markdown 보존 |
| `doc/validation/scene-replacement/checks.json` | 1,053 | verifier 직접 입력 |
| `doc/validation/scene-replacement/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-replacement/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-replacement/full-EditMode.json` | 21,507 | verifier 직접 입력 |
| `doc/validation/scene-replacement/full-PlayMode.json` | 14,333 | verifier 직접 입력 |
| `doc/validation/scene-replacement/green-play.json` | 2,276 | verifier 직접 입력 |
| `doc/validation/scene-replacement/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-replacement/red-play.json` | 3,697 | verifier 직접 입력 |
| `doc/validation/scene-replacement/test-inputs.json` | 24,276 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/README.md` | 3,685 | Markdown 보존 |
| `doc/validation/scene-transition-contracts/checks.json` | 1,099 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/full-EditMode.json` | 17,947 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/full-PlayMode.json` | 9,140 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/green-play.json` | 1,072 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/preserved-inputs.json` | 563 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/red-play.json` | 370 | verifier 직접 입력 |
| `doc/validation/scene-transition-contracts/test-inputs.json` | 10,182 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/README.md` | 6,166 | Markdown 보존 |
| `doc/validation/game-scene-entry/checks.json` | 1,762 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/full-EditMode.json` | 18,532 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/full-PlayMode.json` | 11,118 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/green-bootstrap.json` | 2,433 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/green-edit.json` | 2,691 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/green-entry.json` | 785 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/native-editor.json` | 355 | 사용자 결정: 고정 출력 보존 |
| `doc/validation/game-scene-entry/preserved-inputs.json` | 562 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/red-async-cover.json` | 564 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/red-edit.json` | 1,291 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/red-moved-root.json` | 596 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/red-play.json` | 303 | verifier 직접 입력 |
| `doc/validation/game-scene-entry/test-inputs.json` | 10,658 | verifier 직접 입력 |
| `doc/validation/input-system/p4/README.md` | 7,631 | Markdown 보존 |
| `doc/validation/input-system/p4/checks.json` | 1,632 | verifier 직접 입력 |
| `doc/validation/input-system/p4/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/input-system/p4/final-integration/README.md` | 2,314 | Markdown 보존 |
| `doc/validation/input-system/p4/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/input-system/p4/full-EditMode.json` | 26,276 | verifier 직접 입력 |
| `doc/validation/input-system/p4/full-PlayMode.json` | 21,849 | verifier 직접 입력 |
| `doc/validation/input-system/p4/preserved-inputs.json` | 787 | verifier 직접 입력 |
| `doc/validation/input-system/p4/review-green-play.json` | 1,551 | verifier 직접 입력 |
| `doc/validation/input-system/p4/review-red-play.json` | 742 | verifier 직접 입력 |
| `doc/validation/input-system/p4/test-inputs.json` | 39,535 | verifier 직접 입력 |
| `doc/validation/data-tables/README.md` | 5,236 | Markdown 보존 |
| `doc/validation/data-table-importer/README.md` | 6,591 | Markdown 보존 |
| `doc/validation/data-table-importer/native-editor.json` | 833 | 사용자 결정: 고정 출력 보존 |
| `doc/validation/generic-data-tables/README.md` | 6,118 | Markdown 보존 |
| `doc/validation/data-table-templates/README.md` | 4,307 | Markdown 보존 |
| `doc/validation/resource-manager/README.md` | 5,221 | Markdown 보존 |
| `doc/validation/generic-pool/dotnet-probe/PoolProbe.csproj` | 290 | 허용 확장자 밖/도구 보존 |
| `doc/validation/generic-pool/dotnet-probe/Program.cs` | 1,500 | 허용 확장자 밖/도구 보존 |
| `doc/validation/bootstrap-system/README.md` | 4,211 | Markdown 보존 |
| `doc/validation/bootstrap-system/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/full-EditMode.json` | 17,740 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/full-PlayMode.json` | 9,038 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/native-editor.json` | 294 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/preserved-user-inputs.json` | 448 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/test-inputs.json` | 18,530 | verifier 직접 입력 |
| `doc/validation/bootstrap-system/verify.py` | 3,195 | 허용 확장자 밖/도구 보존 |
| `doc/validation/tplab-naming/README.md` | 5,113 | Markdown 보존 |
| `doc/validation/tplab-naming/checks.json` | 689 | verifier 직접 입력 |
| `doc/validation/tplab-naming/final-console.json` | 3 | verifier 직접 입력 |
| `doc/validation/tplab-naming/final-status.txt` | 110 | verifier 직접 입력 |
| `doc/validation/tplab-naming/full-edit.json` | 27,501 | verifier 직접 입력 |
| `doc/validation/tplab-naming/full-play.json` | 25,738 | verifier 직접 입력 |
| `doc/validation/tplab-naming/preserved-inputs.json` | 787 | verifier 직접 입력 |
| `doc/validation/tplab-naming/test-inputs.json` | 41,339 | verifier 직접 입력 |
| `doc/validation/tplab-name-cleanup/README.md` | 3,965 | Markdown 보존 |
| `doc/validation/ci-review-p1/.gitattributes` | 85 | 허용 확장자 밖/도구 보존 |
| `doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` | 3,627 | Markdown 보존 |
| `doc/validation/ci-review-p1/README.md` | 6,625 | Markdown 보존 |
| `doc/validation/ci-review-p1/fix-p1/.gitattributes` | 125 | 허용 확장자 밖/도구 보존 |
| `doc/validation/ci-review-p1/fix-p1/README.md` | 4,642 | Markdown 보존 |
| `doc/validation/ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md` | 3,171 | Markdown 보존 |
| `doc/validation/ci-review-p1/fix-p1/reviewer/check_pr_rules.diff` | 2,477 | 허용 확장자 밖/도구 보존 |
| `doc/validation/ci-review-p1/merge-acceptance/README.md` | 2,133 | Markdown 보존 |

허용 원시 확장자 밖 6개는 `generic-pool/dotnet-probe/PoolProbe.csproj`, `generic-pool/dotnet-probe/Program.cs`, `bootstrap-system/verify.py`, `ci-review-p1/.gitattributes`, `ci-review-p1/fix-p1/.gitattributes`, `ci-review-p1/fix-p1/reviewer/check_pr_rules.diff`다. 종류는 .csproj/.cs/.py/.diff 각각 1개, .gitattributes 2개다. 도구 및 승인 원문도 그대로 보존했다.

## 회고와 한계

고정 출력은 현재 코드의 default뿐 아니라 보존 Markdown의 실행 가능한 명시 출력 인자까지 보수적으로 보존했다. 파일 링크만 센 초기 조사에서는 디렉터리 링크 6곳을 빠뜨렸고 변경 후 링크 검사로 확인해 보완했다. 이후 검사 수치와 원시 hash로 삭제 영향과 사용자 변경 보존을 확인한다. HTTP 검사는 표본이며 모든 Git blob 존재 확인과 구별한다. 이 작업은 기존 6/17 오류 수정, runtime 리팩터링, 패키지·재직렬화·배포 사본 재생성 또는 새 릴리스가 아니다.

작업용 생성 출력은 `Temp/validation-prune-group1-20261014`에만 두며 저장소에 추가하지 않는다. PR 제공·최종 비교 후 이 작업이 만든 명시 파일만 정리한다. 현재 사용자 자산·dirty·기존 Temp는 건드리지 않는다.
