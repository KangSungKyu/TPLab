# 프로그래머 가이드라인 · 현행 관행 초안

2026-10-13 Phase1 최초 작성 당시 기록: 문서 초안·사용자 검토 단계이며 규범 채택·main 통합 전이었다. 후속1b 결정은 아래 날짜 기록을 따른다. 기준 main35508bc(PR #11 병합 확인), 작업 branch codex/docs-guidelines-draft. [초안 색인](../guidelines/README.md).

- 목적: 사용자 설계와 D-A/B/C를 구분해7개 장의 현행·결정·제안에 근거를 붙인다. 코드·테스트·원시 증거·UPM·기존 지침·모든 기존 INDEX는 변경하지 않는다.
- 부모는 README/GC/문서/증거 장과 최종 검토·Git을 담당했다. /root/factcheck_upm(gpt-6.1-sol/medium)은 코드/API 장과 부모 문서의 독립 읽기 검토, /root/factcheck_forget(gpt-6.1-sol/high)은 비동기/테스트 장·자신의 최종 링크 검토를 담당했다. 동일 파일의 동시 수정·Unity 조작은 없다.
- 이번 Unity/테스트/Player 실행0회. 단위의 완성 검사는 실제 source 근거·상태 라벨·상대 링크/anchor/줄 범위·공백·변경 allowlist·원래2239파일/dirty6 해시 보존·규칙 검사·정확한 최종 HEAD Actions이다. 최종 결과·PR/HEAD는 Git 이력과 완료 보고/PR에 기록한다.
- 수정한 조사 예: InputRebindingController의 공개 타입은4가 아닌5, 직접 Debug.LogException은 조사 Runtime56파일에12곳이다. Runtime line 중앙값75.5를 근거로500/1000행은 검토 신호 제안이며 확정하지 않는다.
- 동일 manifest blob은 동일 내용 공유다. 실제 consumer는 tools 템플릿 경로를 읽으므로 “validation 삭제 전 템플릿 대체 필수”라는 인과관계는 성립하지 않는다. D-A 삭제 정책은 유지하고 후속 선행 조건 정정을 제안했다.
- GC 기존 §8 counter/frame 우선순위와 §10 해결 전 목록의 충돌, allocating snapshot 유지, 과거 Player/현재 IMP의 NotRun을 분리했다. 최소 기계 입력 예외와 summary-only·permalink의 차이도 후속 선택으로 남겼다.
- 기존 INDEX 수정 금지를 회고 색인에도 적용했다. 이번 새 회고는 초안/PR에서 안내하고 색인 반영은 사용자 승인 뒤 별도 문서 정리로 남긴다.

정적 검사 결과: 새 문서9개의 근거 링크311개(상대 경로·Markdown anchor·행 범위) 오류0, trailing whitespace0. 원래 추적2239파일의 SHA-256 집계와 dirty6의 개별 SHA-256은 작업 전과 일치했다. Unity 실행은 없고 규칙 검사와 최종 HEAD Actions 결과는 완료 보고/PR에 기록한다.

## AGENTS 이관 제안: 적용하지 않음

- [제안] 80~88(GameSceneManager),90~97(UIContext)의 완료 track·배정·단계별 통합·당시 PC 종료 운영은 기존 track의 역사적 기록으로 모으고 AGENTS에는 해당 단위의 현재 지침 링크만 두는 안이다. 개인 종료 지시는 다른 요청에 자동 승계하지 않는다. 근거: [AGENTS](../../AGENTS.md#참조와-변경-경계), [Scene track](../SCENE_TRANSITION_TRACK.md), [UI track](../GAME_UI_SYSTEM_TRACK.md).
- [제안] 49~56의 회고 세부 형식은 문서 장,60~64와70의 증거 분류·원문 보존 충돌은 증거 장으로 옮길 수 있는 상세 후보다. 기존 임시 파일/GUID/사용자 자산 보호 자체는 AGENTS에 남긴다. 근거: [AGENTS:49](../../AGENTS.md#사람ai용-외부-제공-문서), [06](../guidelines/06-documentation.md), [07](../guidelines/07-validation-evidence.md).
- [제안] AGENTS에 남길 영속 규칙은 사용자 권한/역할·범위와 dirty 보호, C#/assembly/소유권의 짧은 핵심, SOLID·TDD/실행0과 미검증 구분, 승인 없는 패키지·재직렬화·이력 변경 금지, Git 시작 전 판단·exact-SHA gate·사용자 수락·승인 후 해당 branch 정리다. 상세 공개 API/async/GC 기준의 채택 여부는 사용자 검토 전이므로 이 제안으로 규범을 교체하지 않는다. 근거: [AGENTS:5,14,23,33,74,109,117,125,127](../../AGENTS.md#목적과-역할), [초안 결정/제안](../guidelines/README.md#사용자-결정).

## Phase1 당시 다음 선택 · 2026-10-13

원시 삭제·링크 정리/기존 문서 이관은2단계 별도 PR, 실제 클래스/수명/로깅 개선은3단계 설계 대상이다. 이 PR의 모든 추가 제안과 템플릿 선행 조건 정정은 사용자 검토를 기다리며 기존 문서를 자동 변경하거나 다음 구현을 시작하지 않는다.


## Phase1b 결정 반영 · 2026-10-13

- 범위/등급: D-E~D-G와 사용자 2026-10-13 15:57 일괄 승인 D-H1~D-H12의 문서 반영이다. T0(문서만); PR #12가 open·merged=false여서 같은 branch codex/docs-guidelines-draft에 추가한다. 기존 문서 source와 승인된 설계만 서술하며 코드·XML·도구·CI·UPM은 구현하지 않는다. [PR #12](https://github.com/KangSungKyu/TPLab/pull/12).
- 검증 source SHA: 코드/기존 규칙 main `35508bc1fb8ed7749a292d1f9ae983b45387afd4`, 이번 문서 변경 전 HEAD `79cd5045c3adbd720101a57e6b7cc1af4acfa80e`. 환경은 Windows·Python3.14·기존 TPLab checkout이며 Unity/테스트/Player 실행0회다.
- 핵심 변경: [README](../guidelines/README.md)의 결정 표·라벨 집계, [08](../guidelines/08-change-procedure.md)의 T0~T3·공통 승인/CI·승급·회고 소유·14묶음 제안표. 01~07은 선택한 정책만 확정하고 미채택 대안을 날짜 승인 근거와 함께 보존했다.
- 라벨 정정: 06/07의 CI_GATE_PROPOSAL 보존을 확정 정책에서 빼고 “AGENTS:125의 실제 참조”와 “현 위치 보존/doc 이동 선택”으로 분리했다. 출처는 조사 요청이며 사용자 확정 결정이 아니었다.
- 증거 소유: 새 validation README는 만들지 않고 이 회고가 이번 요약을 소유한다. PR 본문은 T0·근거와 이 회고 링크만 둔다. 모든 기존 INDEX 수정은 이번 범위 밖이므로 색인 갱신과 AGENTS 이관3개 제안은 후속 사용자 검토를 기다린다.
- 분담: 기존 /root/factcheck_upm(gpt-6.1-sol/medium)은01/02, /root/factcheck_forget(gpt-6.1-sol/high)은03/05를 수정하고 부모는04/06/07/08/README/이 회고를 담당했다. 두 에이전트는 부모 문서의 독립 읽기 검토를 추가 담당한다. 같은 파일 동시 수정·Unity 조작은 없다.
- 교훈: 승인된 등급 정의와14묶음의 적용 칸은 별개다. [제안]인56칸으로 D-F 공통 항목을 완화하지 않는다. D-H8은 기존 계측/테스트 의미를 완화하지 않으며 새 helper·소량 대조를 구현한 것도 아니다.
- 실행 명령: 내부 링크/Markdown anchor/행 범위/공백과 문장 라벨의 인메모리 Python 검사, `git diff --check`, `python tools/check_pr_rules.py --base 35508bc1fb8ed7749a292d1f9ae983b45387afd4 --head <검사HEAD>`, `python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <최종HEAD> --output <작업별임시출력>`. 원시 조회 출력은 Git에 추가하지 않는다.
- Actions: [PR #12 checks](https://github.com/KangSungKyu/TPLab/pull/12/checks)는 최종HEAD와 연결되는 기계 증거다. 정확한 HEAD/완료·success/조회 완전성을 도구로 확인해 최종 채팅에 보고한다; workflow 존재를 성공으로 기록하지 않는다. 회고를 포함하는 최종commit 식별은 [AGENTS:53](../../AGENTS.md#사람ai용-외부-제공-문서)의 Git 이력/완료 보고 방식과 구분한다.
- 미실행/후속: Unity·Player·IL2CPP·예제 실행·실제 리팩터링·Unity CI는 미실행. 근거를 검사하기 전 완료로 쓰지 않고 실제 검사 수치·한계는 아래 결과에 기록한다. 병합·강제 푸시·branch 삭제·기존 증거 삭제는 하지 않는다.

<!-- PHASE1B_VALIDATION_START -->
- 정적 결과: 문서10개의 근거 링크403개(상대 경로·Markdown anchor·행 범위) 오류0, 공백 오류0. 기존 지침·INDEX·코드·테스트·UPM 등 제외 범위2239파일 SHA-256 집계와 기존 dirty6 개별 해시는 변경 전과 일치한다. 8개 장의 남은 제안22항목, README3·AGENTS 이관3까지28항목이며 적용 칸56개는 별도 제안이다. 미채택 대안은20문장(표식 한 줄 기준)으로 남겼다.
- 독립 검토/정정: 두 검토자가 README의 리터럴 개행·끊긴 표와06장의 대안 의미 차이를 발견했다. 실제 개행·연속 표로 고치고 원안의 짧은 AGENTS 규칙/가이드 링크·짧은 API 변경 안내·별도 track 보관 대안을 보존했다. 최종 전체 링크 검사 오류0이다.
- 증거 한계: PR checks 링크는 현재tip을 따르므로 단독으로 과거 exact-SHA 판정·조회 완전성을 고정 입증하지 않는다. 최종HEAD는 Git/PR metadata에서, 완료·success·조회 완전성은 실제 CI 도구 조회와 최종 채팅에서 확인한다. 회고에 미래 성공을 미리 기록하거나 원시 조회 JSON을 커밋하지 않는다.
- 다음: 08의14묶음 적용·장기 인용 방식,05의 Timeout/Red 교훈 범위,06의 머리말,07의 과거 재현/CI 판정 문서 위치, 템플릿 선행 조건·AGENTS 이관은 제안 상태다. EnsureMainThread 시범은 채택됐지만 코드 작업·원시 삭제·main 병합은 별도 요청/승인 전에 시작하지 않는다.
<!-- PHASE1B_VALIDATION_END -->
