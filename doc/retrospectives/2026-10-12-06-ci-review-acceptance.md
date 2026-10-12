# 2026-10-12 · GitHub 리뷰 자동화 최종 수락

## 작업과 기준

- 사용자 "승인, 병합"을 확인. 앞선 PR-only 범위는 이번 main 통합 승인으로 갱신되고, 기존 병합 후 해당 작업 branch 정리 규칙을 적용한다. App/secret/저장소 설정·Claude 호출은 제외.
- main `0641fb57e4d6fae03fe17b88801c6762d78de84f`; 승인받은 후보 `0162f7eb39b5d8fc505d05323206bbe4c75ecfc8`; 작업 `codex/github-review-p1` 재사용. [PR #1](https://github.com/KangSungKyu/TPLab/pull/1), [수정 회고](2026-10-12-05-ci-review-corrections.md).
- 상태: 사용자 수락·병합 전 최종 검증 완료. 이 기록은 acceptance 단위의 사실이며, 실제 통합 SHA·branch 정리 결과는 PR 본문/Git 이력/최종 보고에 기록한다.

## 결정과 검증

- GitHub API의 open/non-draft, mergeable/clean, main unprotected와 원격 기준 불변 확인. 기존 main은 후보의 조상이며 원본 source/test hash와 사용자 dirty6는 유지됐다.
- 현재 Windows 회귀45/45, 실패·오류·skip0, UTF8 env 없음. [새 실제 증거](../validation/ci-review-p1/merge-acceptance/README.md). 이후 evidence/회고만 추가하며 runtime/SDK/설정·검사기/test source는 바꾸지 않는다.
- `/root/ui_dependency_audit` 재사용(gpt-6-luna/low)은 diff의 Unity 제외·조상·hash 보존 정적 확인. 변경/테스트/API0. child status 조회는 실패해 그 결과는 전체 dirty 증거로 쓰지 않았고 부모가 실제 status를 소유한다.
- 최종 candidate의 PR 검사 완료·성공을 실제로 확인한 뒤 GitHub 일반 merge로 원본 commit을 보존한다. 병합 tree/ancestry·local/remote main·dirty를 확인한 뒤 source tip을 조건으로 remote branch 삭제하고 local은 -d만 사용한다.

## 남은 범위

- 사용자 설정 후 Claude/S1–S5 실동작은 별도 확인. 과거 체크를 새 코드의 증거로 대체하지 않으며 GitHub PR 규칙 성공은 Unity 런타임 검증이 아니다.
- 최종 Actions·merge SHA·정리 사실은 발생 후 PR 본문에 갱신한다. 이번 임시 증거/PR body는 필요한 데이터 보존 후 해당 소유 파일만 정리한다. Unity/PC 종료 지시 없음.
