# 2026-10-14 · 01 · 검증 원시 자료 1차 삭제

유형은 검증·결정 기록, 등급은 T0다. 기준은 main `d9c2f8896a21081063179f791aabc78d5399e96c`, 작업 브랜치는 `codex/validation-prune-batch1`다. 앞선 11폴더 조사에서 소비 프로젝트 진단의 출처 문자열을 발견해 수정 없이 중단했다. 이번 사용자 승인은 14폴더 전체 삭제와 알려진 출처 4건의 원문 보존으로 범위를 바꿨다. 새 외부 참조 발견 시 중단하며, 아래 기대 수치·삭제로 인한 새 링크/verifier 실패 0·최종 HEAD Actions 성공을 모두 확인한 뒤 채팅에 먼저 보고해야 병합할 수 있다.

## 삭제 목록과 참조 재확인

경로는 `doc/validation/` 기준이다. Git 저장본의 51파일·4,102,323바이트와 일치했다. Windows `core.autocrlf=true` checkout의 합계 4,109,999바이트와 구분한다. 범위 밖 파일을 삭제하지 않았고, 14폴더에는 미추적 파일이나 줄끝 변환 외 변경이 없었다.

| 폴더 | 파일 | Git bytes | 허용 참조 | 신규 참조 | 파일 목록 SHA-256 |
|---|---:|---:|---:|---:|---|
| `scene-integration/CoreConsumer-P6-20261006-01` | 4 | 350,108 | 0 | 0 | `8e533da51b5bc65b38bee556ba930d922e43d14fdb23770f451f0657e2f69e6b` |
| `ui-system/p4/final-inputs` | 2 | 52,613 | 0 | 0 | `09cbcf8db13d7ba1db4a25000b231ff66c16630b6e6e7471267846229cbbf643` |
| `ui-system/p5/final-inputs` | 2 | 53,675 | 0 | 0 | `83061c918f7a0f87c35242ce228ff760f820e41cb08eefef69266b1701998be9` |
| `ui-system/p5/first-green-inputs` | 2 | 53,675 | 0 | 0 | `f7bc62d6fa4821c4e2fe7209afa6ec03d73337443e5b8cb6e53a0b62f3d5f5a0` |
| `ui-system/p5/red-inputs` | 2 | 53,675 | 1 | 0 | `be1197f2e6f32aaf313e108ad5c565206c4e1d01f15fb13b7913fec4953e8645` |
| `ui-system/p6/final-inputs` | 2 | 62,149 | 0 | 0 | `745420560cc8c0f1e51ae08e22f17a91b1e8e84b924615997a52f1eee5d58f07` |
| `ui-system/p6/first-green-inputs` | 2 | 55,072 | 0 | 0 | `654862baf61eeafd1ee4528395a1cb741149c6f93bc8a82d42177eae87ca9b51` |
| `ui-system/p6/red-inputs` | 2 | 55,072 | 0 | 0 | `fef0c9aabb26820837b323c930ba076d65723bbf7bd43986571fe7c8477c42fa` |
| `ui-system/p6/red-scaffold-inputs` | 2 | 55,072 | 0 | 0 | `7ee111403202b29f6f55f37acf5fbabbb6b1680bcc464a078b9fa81b8131a18c` |
| `ui-system/p7/consumer-base-a` | 8 | 518,644 | 1 | 0 | `9c1d5914ab2fa1c7947e61de34dab502ba51937dbe652aac30f8435aa8a7e1b0` |
| `ui-system/p7/consumer-base-b` | 8 | 956,181 | 0 | 0 | `63771c63cad176fa4fdc635d4aa82aba9e4b472426bc4116151ba7d7e7d62f82` |
| `ui-system/p7/consumer-input-additive-b` | 5 | 910,088 | 1 | 0 | `296013e69552498ff580c002ae80506f6874eea42524c1776b4773e395c8389f` |
| `ui-system/p7/consumer-input-single-a` | 5 | 908,907 | 1 | 0 | `a5d9654a7d01f0026d22d5a349884eafafc1079c7f184983ec9343cfe21f755a` |
| `ui-system/p7/sample-additive-b` | 5 | 17,392 | 0 | 0 | `c949c6c41c7b7b8aa94eee18ebe0a87ee1f1ef63f141109c17920f859378f9f4` |
| 합계 | 51 | 4,102,323 | 4 | 0 | — |

목록 해시는 폴더 안 상대 파일명을 정렬하고 각 파일의 `relativePath + NUL + Git bytes + NUL + fileSHA256 + LF`를 UTF-8로 연결해 SHA-256으로 계산했다. 개별 bytes·내용 해시는 삭제 전 commit의 파일로 다시 계산할 수 있다.

기존 읽기 감사 담당 `/root/factcheck_upm`을 재사용했다(기존 gpt-6.1-sol/medium 설정 유지). 독립 텍스트 감사에 한정했고, 부모만 파일·Git 작업을 수행했다. main 추적 2,250파일 중 텍스트 2,236파일을 전수 확인했으며 PNG/DLL 14개는 텍스트 검색에서 제외했다. 전체 경로·suffix·부모 상대 경로, 슬래시·단일/이중 역슬래시 및 정확한 폴더 토큰 69행의 문맥을 확인했다. 자기 폴더 참조를 제외하고, 삭제 대상 간 교차 참조도 포함했다. Markdown, `tools/`, `.github/`, `Assets/` 문자열을 포함한 결과 허용 4건 외 참조는 0건이었다. CoreConsumer report의 Temp 소비 프로젝트, `temporary-cleanup.json`의 `removedOwnedDirectories` 기록, 다른 Phase의 동명 입력 폴더와 `consumer-input-single-accepted-*`는 실제 삭제 경로와 구분했다.

## 알려진 출처 문자열과 이력

아래 원문은 수정하지 않았다. 링크는 모두 삭제 전 SHA에 고정했다.

| 삭제 전 출처 | 원문 값 | 삭제 후 상태 |
|---|---|---|
| [p5/red-edit-prestart.json:4](https://github.com/KangSungKyu/TPLab/blob/d9c2f8896a21081063179f791aabc78d5399e96c/doc/validation/ui-system/p5/red-edit-prestart.json#L4) | `sourceSnapshot = "red-inputs/test-inputs.json"` | 원문 파일을 유지하며 끊긴 출처 문자열로 남는다. |
| [p7/first-consumer-diagnosis.json:9](https://github.com/KangSungKyu/TPLab/blob/d9c2f8896a21081063179f791aabc78d5399e96c/doc/validation/ui-system/p7/first-consumer-diagnosis.json#L9) | `preservedFailure = "consumer-base-a/smoke-result.json"` | 원문 파일을 유지하며 끊긴 출처 문자열로 남는다. |
| [p7/sample-additive-b/process.json:16](https://github.com/KangSungKyu/TPLab/blob/d9c2f8896a21081063179f791aabc78d5399e96c/doc/validation/ui-system/p7/sample-additive-b/process.json#L16) | `buildEvidence = "doc\\validation\\ui-system\\p7\\consumer-input-additive-b"` | 출처 파일 자체도 승인된 51파일에 포함돼 함께 삭제됐다. 문자열 수정 없이 Git 이력에 남는다. |
| [p7/sample-single-a/process.json:16](https://github.com/KangSungKyu/TPLab/blob/d9c2f8896a21081063179f791aabc78d5399e96c/doc/validation/ui-system/p7/sample-single-a/process.json#L16) | `buildEvidence = "doc\\validation\\ui-system\\p7\\consumer-input-single-a"` | 원문 파일을 유지하며 끊긴 출처 문자열로 남는다. |

즉 알려진 4건 중 현 checkout에 남는 끊긴 문자열은 3건이다. 삭제된 JSON 안의 1건도 원문 이력으로 열람할 수 있다. 이것은 사용자가 명시적으로 허용한 과거 출처 정보이며 작동하는 현재 로컬 입력으로 표시하지 않는다.

Git 이력은 다시 쓰지 않았다. [삭제 전 validation tree](https://github.com/KangSungKyu/TPLab/tree/d9c2f8896a21081063179f791aabc78d5399e96c/doc/validation)에서 14폴더를 열람하거나 아래처럼 파일을 읽을 수 있다. 이 permalink는 과거 열람 수단이며 해당 source의 Unity 재검증을 수행했다는 뜻이 아니다.

```text
git show d9c2f8896a21081063179f791aabc78d5399e96c:doc/validation/<folder>/<file>
```

## 삭제 전후 검사와 보존

[generic verifier](../../tools/verify_validation.py#L1)의 16 evidence root와 [Bootstrap verifier](../validation/bootstrap-system/verify.py#L1)를 읽기 전용으로 실제 실행했다. 실행 명령은 `C:\Python314\python.exe tools/verify_validation.py --evidence doc/validation/<root>`(checks.json이 있는 16개 root 각각)와 `C:\Python314\python.exe doc/validation/bootstrap-system/verify.py`다. 두 실행 모두 Windows/Python 3.14, 동일 checkout, `PYTHONDONTWRITEBYTECODE=1`이었다. 기존 도구·검증 코드는 수정하지 않았다.

| 검사 | 삭제 전 main | 삭제 후 | 삭제로 생긴 새 실패 |
|---|---:|---:|---:|
| 추적 Markdown 상대 링크 | 2,647개·기존 오류 6 | 삭제 직후 2,647개·동일 오류 6 | 0 |
| line/heading anchor | 443개·오류 0 | 삭제 직후 443개·오류 0 | 0 |
| generic verifier | 16회·기존 실패 16 | 16회·동일 실패 원문 16 | 0 |
| Bootstrap verifier | 1회·기존 실패 1 | 1회·동일 실패 원문 1 | 0 |

링크/anchor는 추적 Markdown의 fenced/inline code를 제외하고 상대 링크·참조형 링크, 파일/디렉터리 존재, GFM heading·HTML id·`#L` 범위를 검사하는 일회성 stdlib 조사로 확인했다. 새 회고와 색인을 포함한 최종 변경도 같은 검사에 포함했고, 상대 링크 2,651개·anchor 445개에서 기존 오류 6건·새 오류 0건이었다. 기존 6개는 `doc/validation/ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md`의 9·13·21(2건)·25(2건)행에서 잘못된 상대 경로를 사용하는 항목이며 수정하지 않았다. remote HTTP 응답 검사는 미실행이다.

verifier 실패를 통과로 기록하지 않는다. 17개 exit code는 모두 1이며 stdout/stderr가 전후 동일했다. 구형 `Assets/MyLab/Core/DataTables/CsvDataTable.cs` 경로 부재 3개, `Assets/MyLab/Core.meta` 부재 8개, 현재 `Assets/TPLab/Core/Lifecycle/ISceneRoot.cs`의 과거 hash 불일치 6개였다. 실패 원문 요약은 각각 `FileNotFoundError: [Errno 2] No such file or directory`(위 두 경로)와 `AssertionError: Assets/TPLab/Core/Lifecycle/ISceneRoot.cs`다. 각 verifier는 첫 기존 실패에서 멈추므로 이후 모든 과거 단언을 재검증했다는 뜻이 아니다. 별도로 실제 직접 입력 generic 158 + Bootstrap 7 = 165파일의 raw hash 보존, manifest payload·checks.documents와 삭제 51파일의 교집합 0을 확인했다.

범위 밖 추적 2,198파일과 기존 dirty 6개는 삭제 직후 raw hash가 같았다. 살아남은 출처 파일 3개의 bytes도 그대로다. 14개 대상 디렉터리는 `git rm`으로 사라졌으며 상위 `scene-integration`/UI Phase 디렉터리는 보존할 다른 자료가 있어 남아 있다. 요약 Markdown·현행 verifier 최소 입력·소스·테스트·UPM·보호 지침은 삭제하지 않았다. 승인된 삭제 외 변경은 이 회고와 [회고 색인](INDEX.md)의 새 행뿐이다.

## Git gate와 미실행

공백·변경 범위·원문/hash 보존을 확인하고 `tools/check_pr_rules.py --base d9c2f8896a21081063179f791aabc78d5399e96c --head <최종 tip>`을 실행한다. 최종 Actions는 `tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <최종 tip> --output Temp/validation-prune-batch1-20261014/final-ci.json`로 exact-SHA·조회 완전성·completed/success를 판정한다. [브랜치 Actions 실행](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Fvalidation-prune-batch1)의 최종 tip 결과와 완료 보고를 함께 대조한다. 회고 자신을 포함하는 최종 SHA 및 실제 병합 SHA는 Git·PR 이력/완료 보고에서 확인한다. PR 본문에는 삭제 목록·이력 보존과 이 회고 링크를 둔다.

사용자의 조건부 승인으로 모든 gate가 맞을 때만 병합 전 채팅 보고 후 원본 commit을 보존하는 일반 merge를 진행한다. 강제 푸시·이력 재작성·브랜치 삭제는 하지 않는다. 작업 전의 dirty 6개는 통합 후에도 재확인한다.

미실행: Unity Edit/Play 테스트·Editor 컴파일·제품 Console·Player/IL2CPP·외부 링크 HTTP 조회·독립 Claude workflow. 런타임/테스트/API를 바꾸지 않은 T0 자료 삭제로 Unity 실행을 주장하지 않는다. 알려진 4출처를 자동 복원하거나 verifier의 구형 경로/hash 및 기존 링크 오류를 정정하지 않았다. 일회성 조사 JSON·로그는 이 작업의 Temp 폴더에만 두고 필요한 요약을 여기 보존한 뒤 해당 작업 파일만 정리한다; 영속 문서는 Temp에 의존하지 않는다.
