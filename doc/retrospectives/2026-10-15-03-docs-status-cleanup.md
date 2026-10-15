# 2026-10-15 · 03 · 문서 상태 정정과 과거 기록 표시

T0 문서 전용이다. 사용자 작업2의 다섯 상태 문장과 이전 non-validation 서베이의 과거 기록12개만 정리한다. 기준 main/source는 `e661a79e3e487c46fb185190d5bc9e0ad8391375`, branch는 `codex/docs-status-cleanup`이다. 작업1·3과 별도 PR이며 병합하지 않는다.

## 다섯 상태의 직접 확인

모든 근거 commit은 `git merge-base --is-ancestor <commit> <source>`로 현재 main 포함을 확인했다. 파일 존재와 source 선언·workflow를 읽었으며 과거 실행을 새 실행으로 바꾸지 않는다.

| 대상·변경 전 위치 | 기존 표현 | 정정 내용 | 직접 확인 근거 |
|---|---|---|---|
| [GAME_SCENE_MANAGER_DRAFT](../GAME_SCENE_MANAGER_DRAFT.md) ·162 | IMP-05 사용자 승인 대기 | 사용자 승인·PR #10 main 병합 완료, 실행 수치는 당시 기록; 새 Player/IL2CPP NotRun 유지 | [PR #10](https://github.com/KangSungKyu/TPLab/pull/10) closed·merged=true; merge `5725e90b133aa6739c3ef098455815336de00e20`; [실제 buffer API](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L101) |
| [CI_SETUP_GUIDE](../CI_SETUP_GUIDE.md) ·3 | 초안·저장소 미반영·ci-draft 파일 | workflow·규칙/CI 도구 main 반영 완료; 초기 도입·충돌 설명은 당시 기록; Unity CI 포함 안 됨 | 도입 commit `b194ebbf090108f4fae1ea0f23b2e64d1ee1d91d`; [rule workflow](../../.github/workflows/rule-check.yml), [Claude workflow](../../.github/workflows/claude-review.yml), [exact-SHA 도구](../../tools/check_github_ci.py) |
| [UI_ACCEPTANCE](../UI_ACCEPTANCE.md) ·3 | Main integration is in progress and is not complete. | main 통합 완료, 기존 수락은 후속 source의 새 검증이 아님 | [UI 통합 기록](../validation/ui-system/integration-20261012/README.md)의 `7b606586038883d4a38841e76e4c969d116306b1` main 포함; [실제 UIContext](../../Assets/TPLab/UI/Runtime/UIContext.cs) |
| [INPUT_SYSTEM_TRACK](../INPUT_SYSTEM_TRACK.md) ·47 | 수락 전 main 병합·삭제 보류를 현재 지시처럼 표현 | 입력·문서 main 통합 완료를 명시하고 뒤 문단은 2026-10-07 통합 전 운영 조건으로 구분 | [통합 기록](../validation/input-system/p4/final-integration/README.md)의 `9648ff5feb48219931cdd8b24f12115b58e9805d` main 포함; [실제 InputManager](../../Assets/TPLab/Input/Runtime/InputManager.cs) |
| [SCENE_TRANSITION_ACCEPTANCE](../SCENE_TRANSITION_ACCEPTANCE.md) ·33 이후 | 통합 대기·절전·종료를 현재 지시처럼 표현 | main 통합 완료를 명시하고 기존 절은 2026-10-07의 과거 조건·현재 권한 아님으로 구분 | `7fae48093253b7710d9447b91322c22de2a1e1e1` 및 완료 기록 `8cf3f226ae5e57f037c199fc49f48a14fd299835` main 포함; [track](../SCENE_TRANSITION_TRACK.md) |

CI workflow 존재·내용은 확인했지만 Claude 인증 시크릿 설정이나 새 독립 리뷰 성공은 확인하지 않았다. rule workflow는 diff 규칙 검사이며 Unity 실행이 아니다. CI_SETUP_GUIDE의 다른 본문을 재작성하거나 정책을 바꾸지 않는다.

## 과거 기록 표시12개

이전 non-validation 서베이는 main a3c4e00d 기준으로 다음12개를 역사 기록 보존군으로 분류했다. 사용자가 같은 목록을 지정했으므로 현재 파일의 첫 제목·빈 줄 다음에 표시 한 줄만 추가했다. 문서별 기존 헤더 배치를 유지하고 본문을 옮기거나 삭제하지 않는다.

표시: **과거 기록(2026-10-15 표시): 당시 설계·운영·수락·검증의 기록이며, 현재 계약·작업 권한·최신 검증을 대신하지 않는다.**

| 대상 | 대상 |
|---|---|
| [DATA_TABLE_GENERIC_IMPLEMENTATION](../DATA_TABLE_GENERIC_IMPLEMENTATION.md) | [GAME_UI_SYSTEM_DRAFT](../GAME_UI_SYSTEM_DRAFT.md) |
| [GAME_UI_SYSTEM_TRACK](../GAME_UI_SYSTEM_TRACK.md) | [INPUT_SYSTEM_TRACK](../INPUT_SYSTEM_TRACK.md) |
| [SCENE_LOADING_TRACK](../SCENE_LOADING_TRACK.md) | [SCENE_TRANSITION_TRACK](../SCENE_TRANSITION_TRACK.md) |
| [DISTRIBUTION_ACCEPTANCE](../DISTRIBUTION_ACCEPTANCE.md) | [INPUT_SYSTEM_ACCEPTANCE](../INPUT_SYSTEM_ACCEPTANCE.md) |
| [SCENE_LOADING_ACCEPTANCE](../SCENE_LOADING_ACCEPTANCE.md) | [SCENE_TRANSITION_ACCEPTANCE](../SCENE_TRANSITION_ACCEPTANCE.md) |
| [UI_ACCEPTANCE](../UI_ACCEPTANCE.md) | [UI_PERFORMANCE](../UI_PERFORMANCE.md) |

## 검증·보존·미실행

표시 한 줄과 승인된 다섯 상태 수정만 역변환하면 대상14문서가 기준 main Git 원문과 같음을 전수 비교했다. 표시 대상12개 중 Input track·Scene acceptance·UI acceptance3개만 별도 승인한 상태 수정이 겹친다. 다른9개는 표시 한 줄 외 내용 변경0이다.

환경은 Windows PowerShell·Python3.14다. 실행은 `git show/log/merge-base`, source·workflow 검색, `Temp/docs-close-20261015/check_docs.py --label <전후>`, `git diff --check <source> HEAD`, `tools/check_pr_rules.py --base <source> --head HEAD`다. 전후 링크·anchor와 verifier17회의 원문을 비교하며 기존 링크6건 외 새 오류0이어야 한다. 규칙 검사와 최종 HEAD exact-SHA Actions는 제공 기록과 최종 채팅에서 구분한다.

사용자 dirty6과 허용 문서 밖 기존 추적파일 raw hash, runtime·기존/신규 테스트·asmdef·UPM·도구·workflow 변경0을 확인한다. 임시 검사·출력은 작업 소유 Temp에서만 사용하고 요약 보존·참조 종료 뒤 명시 목록으로 정리한다.

미실행: Unity 테스트·compile·제품 Console·Player/IL2CPP·새 GC 계측·외부 소비·Claude 새 리뷰. 과거 수락을 최신 source의 새 검증으로 확대하지 않는다. 범위 밖 GAME_SCENE_MANAGER_DRAFT3/83행과 CI 가이드의 상세 도입 계획은 재작성하지 않았다.

## 문서 검사 확정

변경 후 Markdown247개·상대 링크1979개·anchor477개를 검사했다. 전후 기존 링크 오류6건·anchor 오류0건이 같고 새 오류0이다. verifier17회의 exit/stdout/stderr가 기준선과 같아 새 실패0이다. 허용 문서 밖 기존1188파일과 사용자 dirty6의 raw hash가 동일했다. 표시는12개에 각1줄이며 승인한 상태5곳 외 본문 정정0을 원문 역비교로 확인했다.

[PR #27](https://github.com/KangSungKyu/TPLab/pull/27)을 non-draft/open으로 제공했다. 최초 문서 commit 91524bb3412e27526996265d6fceb753350de472의 [Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38675998339)는 completed/success, check1·run1이며 모든 collection complete=true/errors=[], queryIssues/blockingReasons=[]였다. 로컬 규칙 검사16파일 오류0·경고0과 commit diff 공백0을 확인했다. 이 제공 기록만 추가한 최종 HEAD도 exact-SHA로 다시 확인해 최종 보고한다. main 병합은 하지 않는다.
