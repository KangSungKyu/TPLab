# 2026-10-12 · GitHub review automation P1

## 작업과 기준

- 범위: 사용자가 Claude 추가 프롬프트의 파일 반영·PR 생성까지 승인. main 병합·다른 branch push/삭제·시크릿·App·저장소 설정 변경은 제외.
- 기준: main `0641fb57e4d6fae03fe17b88801c6762d78de84f`; 작업 `codex/github-review-p1`.
- 상태: 로컬 구현·검증 완료, PR 생성 후 사용자 검토 대기. 원문 초안 7개는 내용 수정 없이 반영.
- 선행: [CI 설정 초안](../CI_SETUP_GUIDE.md), [검증·보존 기준](../validation/ci-review-p1/README.md).

## 결정과 변경

- 원문 파일 7개와 문서 안내를 첫 커밋 `b194ebbf090108f4fae1ea0f23b2e64d1ee1d91d`에 반영. 기존 CI 조회 helper 변경은 별도 커밋으로 분리.
- helper는 워크플로 존재 자체 대신 정확 SHA의 조회 완전성·완료/성공 결과로 판단. protected/unknown main, 조회 누락·실패·진행·취소·skipped/neutral은 차단. CI 없음은 UNCONFIGURED이며 통과 테스트를 뜻하지 않는다.
- 기존 AGENTS 규칙 문구와 사용자 dirty 6개를 보존. Unity/public C# API 변경이 없어 XML·사람/AI SDK API 문서 생성은 해당 없음. 운영 지침·판정표·검증·회고를 갱신.
- CP949에서 초안 report의 em dash 출력 오류를 실제 확인. 초안 변경 없이 PYTHONUTF8=1 환경으로 재실행. 이는 새 기능 TDD Red로 계산하지 않는다.
- 원문 rename 검사에서 이전 .meta 잔존 누락을 fixture로 재현해 보고만 함. 원문 초안 보존 요청이 우선.

| 담당 | 모델/추론 | 이유·허용 경로 | 상태 |
|---|---|---|---|
| /root/ui_lifecycle_review 재사용 | 부모 모델·추론 상속 | 병합 안전장치 위험: tools/check_github_ci.py, tools/test_check_github_ci.py, gate-* 증거만; Git/Unity/실제 API 제외 | 구현·Red/Green·동결 완료 |
| /root/ui_dependency_audit 재사용 | gpt-6-luna / low | 독립 읽기 전용 초안·정책 대조; 모든 변경/Git/Unity/API 제외 | 초안 문제 보고 완료·최종 정책 리뷰 |
| 부모 | 현재 세션 | 공용 정책 검토·docs·Git/PR·실제 원격 상태 확인 | 최종 PR 진행 |

## 검증과 한계

- 원문 검사13: 최초 오류1/실패0/skip0 → UTF-8 13/13. CI helper Red28: 실패27/오류1/skip0 → 같은 test 파일 Green28/28. 부모 결합41/41, 실패·오류·skip0. [증거](../validation/ci-review-p1/README.md).
- 단위 테스트는 오프라인 모의 API이며 실제 GitHub Actions·Claude 실행을 증명하지 않는다. 정확 PR diff 규칙 결과와 실제 GitHub 상태는 PR 생성 단계에서 별도 기록.
- Unity 컴파일·Console·EditMode·PlayMode·Player·최종 시각 확인은 이번 범위에서 미실행.

## 다음 작업

- PR 본문의 [판정 변경 표](../validation/ci-review-p1/CI_GATE_PROPOSAL.md)를 사용자 검토 후에만 main 반영. 이번 작업에서는 병합·branch 삭제 금지.
- 원문 가이드는 초안 상태 문구와 advisory 정책을 유지. local helper가 실패를 차단하는 기준과의 차이, 재시도 이전 실패 run 처리, optional/skipped 체크 정책은 후속 결정 대상.
- 사용자가 Claude App·OAuth secret을 직접 설정한 후 가이드 S1–S5를 별도 실행. read-only·사용량을 검증 전 보장하지 않는다. 토큰은 채팅·문서에 남기지 않는다.
- 작업별 Temp helper/PR body는 필요한 증거 보존 후 소유 경로만 정리. 사용자 첨부/zip과 기존 Temp는 보존.
