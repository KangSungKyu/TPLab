# 프로그래머 가이드라인 · 현행 관행 초안

2026-10-13. 문서 초안 작성·사용자 검토 단계이며 규범 채택·main 통합은 완료하지 않았다. 기준 main35508bc(PR #11 병합 확인), 작업 branch codex/docs-guidelines-draft. [초안 색인](../guidelines/README.md).

- 목적: 사용자 설계와 D-A/B/C를 구분해7개 장의 현행·결정·제안에 근거를 붙인다. 코드·테스트·원시 증거·UPM·기존 지침·모든 기존 INDEX는 변경하지 않는다.
- 부모는 README/GC/문서/증거 장과 최종 검토·Git을 담당했다. /root/factcheck_upm(gpt-6.1-sol/medium)은 코드/API 장과 부모 문서의 독립 읽기 검토, /root/factcheck_forget(gpt-6.1-sol/high)은 비동기/테스트 장·자신의 최종 링크 검토를 담당했다. 동일 파일의 동시 수정·Unity 조작은 없다.
- 이번 Unity/테스트/Player 실행0회. 단위의 완성 검사는 실제 source 근거·상태 라벨·상대 링크/anchor/줄 범위·공백·변경 allowlist·원래2239파일/dirty6 해시 보존·규칙 검사·정확한 최종 HEAD Actions이다. 최종 결과·PR/HEAD는 Git 이력과 완료 보고/PR에 기록한다.
- 수정한 조사 예: InputRebindingController의 공개 타입은4가 아닌5, 직접 Debug.LogException은 조사 Runtime56파일에12곳이다. Runtime line 중앙값75.5를 근거로500/1000행은 검토 신호 제안이며 확정하지 않는다.
- 동일 manifest blob은 동일 내용 공유다. 실제 consumer는 tools 템플릿 경로를 읽으므로 “validation 삭제 전 템플릿 대체 필수”라는 인과관계는 성립하지 않는다. D-A 삭제 정책은 유지하고 후속 선행 조건 정정을 제안했다.
- GC 기존 §8 counter/frame 우선순위와 §10 해결 전 목록의 충돌, allocating snapshot 유지, 과거 Player/현재 IMP의 NotRun을 분리했다. 최소 기계 입력 예외와 summary-only·permalink의 차이도 후속 선택으로 남겼다.
- 기존 INDEX 수정 금지를 회고 색인에도 적용했다. 이번 새 회고는 초안/PR에서 안내하고 색인 반영은 사용자 승인 뒤 별도 문서 정리로 남긴다.

정적 검사 결과: 새 문서9개의 근거 링크311개(상대 경로·Markdown anchor·행 범위) 오류0, trailing whitespace0. 원래 추적2239파일의 SHA-256 집계와 dirty6의 개별 SHA-256은 작업 전과 일치했다. Unity 실행은 없고 규칙 검사와 최종 HEAD Actions 결과는 완료 보고/PR에 기록한다.

## AGENTS 이관 제안: 적용하지 않음

- [제안] 80~88(GameSceneManager),90~97(UIContext)의 완료 track·배정·단계별 통합·당시 PC 종료 운영은 기존 track의 역사적 기록으로 모으고 AGENTS에는 해당 단위의 현재 지침 링크만 두는 안이다. 개인 종료 지시는 다른 요청에 자동 승계하지 않는다. 근거: [AGENTS](../../AGENTS.md#L80), [Scene track](../SCENE_TRANSITION_TRACK.md), [UI track](../GAME_UI_SYSTEM_TRACK.md).
- [제안] 49~56의 회고 세부 형식은 문서 장,60~64와70의 증거 분류·원문 보존 충돌은 증거 장으로 옮길 수 있는 상세 후보다. 기존 임시 파일/GUID/사용자 자산 보호 자체는 AGENTS에 남긴다. 근거: [AGENTS:49](../../AGENTS.md#L49), [06](../guidelines/06-documentation.md), [07](../guidelines/07-validation-evidence.md).
- [제안] AGENTS에 남길 영속 규칙은 사용자 권한/역할·범위와 dirty 보호, C#/assembly/소유권의 짧은 핵심, SOLID·TDD/실행0과 미검증 구분, 승인 없는 패키지·재직렬화·이력 변경 금지, Git 시작 전 판단·exact-SHA gate·사용자 수락·승인 후 해당 branch 정리다. 상세 공개 API/async/GC 기준의 채택 여부는 사용자 검토 전이므로 이 제안으로 규범을 교체하지 않는다. 근거: [AGENTS:5,14,23,33,74,109,117,125,127](../../AGENTS.md#L5), [초안 결정/제안](../guidelines/README.md#사용자-결정).

## 다음 선택

원시 삭제·링크 정리/기존 문서 이관은2단계 별도 PR, 실제 클래스/수명/로깅 개선은3단계 설계 대상이다. 이 PR의 모든 추가 제안과 템플릿 선행 조건 정정은 사용자 검토를 기다리며 기존 문서를 자동 변경하거나 다음 구현을 시작하지 않는다.
