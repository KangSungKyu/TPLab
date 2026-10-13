# 07 · 검증 증거 초안

[현행] SourceRevision은 main35508bc이고 새 확인은 코드 읽기·Git 조회다. 아래 Mono Editor 수치는 기존 source의 실행 기록이며 이번 Unity/Player 실행은 0회다. 근거: [공통 범위](README.md#근거와-검증-범위), [문서 전용 검증:74](../DOCUMENTATION_GUIDE.md#L74).

## 무엇을 확인했는지 구분하는 현행

- [현행] 코드 읽기는 실행 완료 증거가 아니며 source·환경·명령·test 이름·total/pass/fail/skip와 한계를 구분해 보고하는 현행 규칙이 있다. 근거: [AGENTS:38,39](../../AGENTS.md#L38), [백로그 보고:323](../IMPROVEMENT_BACKLOG.md#L323).
- [현행] IMP-03은 오라클·fixture 오류와 별도 승인 정정·재동결을 기록한 뒤44/44·전체Edit375/Play345를 남겼다; 처음의 잘못된 실패를 의도한 Red로 바꾸어 쓰지 않았다. 근거: [요약:7,17,25,27,65](../validation/imp-03/README.md#L7).
- [현행] exact-SHA 판정은 대상 SHA·조회 완전성·완료/success를 검사하며 CI 존재와 Unity 통과는 별개다. 근거: [CI 도구:1,72](../../tools/check_github_ci.py#L1), [AGENTS:125](../../AGENTS.md#L125).
- [현행] 현재 IMP-14/05/03은 Windows Mono Editor 중심이고 Player/IL2CPP는 미검증이다. 과거0.0.1 Mono Player 실행은 다른 source의 최신 계약 검증을 대신하지 않는다. 근거: [IMP-05:75](../validation/imp-05/implementation/README.md#L75), [IMP-03:74](../validation/imp-03/README.md#L74), [과거 Release:5,11](../validation/distribution-release/README.md#L5).

## 증거 소유와 최소 기계 입력

- [결정됨] **D-E — 회고 단일 소유.** 변경 하나의 증거 요약은 회고 한 곳에 두고 PR 본문은 그 회고를 링크한다. 새 작업의 validation README를 만들지 않으며 백로그·INDEX에는 상태 한 줄과 링크만 두고 수치·SHA를 복제하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-E, [06장의 분류 관계](06-documentation.md#문서-4분류와-증거-소유).
- [결정됨] 검토했으나 채택하지 않은 대안: validation README·회고·PR·색인에 같은 요약을 각각 유지하는 안. 근거: 사용자 2026-10-13 Phase1b D-E, [중복 측정](08-change-procedure.md#15단계-측정-근거).
- [결정됨] **D-A·D-H10 — 원시와 예외.** 원시 산출물 비보관 원칙에서 현행 도구가 요구하는 최소 기계 입력만 예외로 보존한다. 아래 실제 입력 목록이 기준이며 “나중에 필요할 수 있음”을 이유로 모든 JSON·로그를 예외로 묶지 않는다. 도구는 이번에 수정하지 않는다. 근거: 사용자 2026-10-13 일괄 승인(D-H10), [필수 입력 읽기:31,37,42,60,61](../../tools/verify_validation.py#L31).
- [결정됨] 검토했으나 채택하지 않은 대안: 필수 기계 입력 예외 없이 요약만 남기도록 도구를 이번에 변경하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H10).
- [현행] verify_validation.py는 checks.json, checks.results가 이름을 정한 결과 JSON, test-inputs.json, preserved-inputs.json, final-console.json, final-status.txt를 읽는다. exact-SHA 도구의 --output JSON은 조회 출력이고 커밋된 validation JSON을 필수 입력으로 읽지는 않는다. 근거: [verify_validation:31](../../tools/verify_validation.py#L31), [CI 조회 출력:163,178](../../tools/check_github_ci.py#L163).
- [제안] 필수 입력이 없는 과거 실행의 재현을 같은 source·환경의 새 실행으로 남길지, 원본을 별도 제공 위치에서 확보할지는 사용자 결정 대상이다. 원본 해시의 기록과 bytes의 재검사 가능은 다르다. 근거: [bytes 확인:23,37](../../tools/verify_validation.py#L23), [환경 한계:22](../DOCUMENTATION_GUIDE.md#L22).

## CI 판정 문서의 라벨 정정

- [현행] CI_GATE_PROPOSAL.md는 AGENTS.md:125가 판정 기준으로 링크한다. 이전 보존 제약은 조사 요청에서 온 것이며 사용자 확정 결정으로 올린 라벨이 잘못됐다; D-A·D-H10은 이 파일의 위치를 확정하지 않는다. 근거: [실제 참조:125](../../AGENTS.md#L125), 사용자 2026-10-13 Phase1b의 정정 요청.
- [제안] [CI_GATE_PROPOSAL.md](../validation/ci-review-p1/CI_GATE_PROPOSAL.md)의 보존·현재 위치 유지와 doc/ 아래 이동·참조 갱신 중 선택은 사용자 결정 대상이다. 이번에 이동·삭제하지 않는다. 근거: 위 참조와 Phase1b의 미결정 목록.

## 삭제와 현행 도구의 충돌

| 상태 | 현재 요구/위치 | 새 결정과의 관계 |
|---|---|---|
| [현행] | [AGENTS:60](../../AGENTS.md#L60) | 증거 보존 문구는 D-E의 회고 소유·최소 입력 예외와 대조할 후속 정리 대상이다. 근거: [사용자 결정](README.md#사용자-결정). |
| [현행] | [AGENTS:70](../../AGENTS.md#L70) | 과거 원문 보존 문구와 D-A 삭제 방향은 충돌한다; 이번에 AGENTS를 수정하지 않는다. 근거: 같은 결정·이번 제외 범위. |
| [현행] | [verify_validation:23,31](../../tools/verify_validation.py#L23) | 삭제된 과거 원본은 같은 로컬 입력 검사를 다시 수행할 수 없다. |
| [현행] | [consumer 입력:222,230](../../tools/run_core_consumer.py#L222) | 독립 tools 템플릿 입력이므로 동일 validation blob만으로 대체 필요성이 성립하지 않는다. 근거: [조회 정정](README.md#05단계-사실-확인). |
| [현행] | [Editor check:246](../../Assets/TPLab/Validation/Editor/DataTableImportEditorCheck.cs#L246) | 향후 실행은 기존 validation 폴더·JSON을 다시 만든다; 삭제만으로 새 출력 경로·정책이 구현되지는 않는다. |

- [결정됨] **D-H11 — 2단계 삭제 순서.** 직접 참조가 없는 leaf 폴더14개를 먼저 검토하고 이후 실행 단위별 링크·판정·도구 입력을 매핑한다. 실제 삭제는 목록 제시와 사용자 승인 뒤 별도 PR에서 수행한다. “참조 없음”은 폐기 안전의 증명이 아니다. 근거: 사용자 2026-10-13 일괄 승인, [0.5단계 조회](README.md#05단계-사실-확인).
- [결정됨] 검토했으나 채택하지 않은 대안: leaf 검토를 건너뛰고 오래된 실행 단위부터 바로 정리하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H11).
- [결정됨] **D-H10 — 고정 permalink.** 삭제된 과거 증거 링크는 자료가 존재하는 commit SHA에 고정한 permalink로 대체한다. 이는 읽기용 링크이며 로컬 원본 복원·재검증 능력이 아니다. 실제 링크 수정도2단계에서 수행한다. 근거: 사용자 2026-10-13 일괄 승인, [D-A](README.md#사용자-결정), [현재 입력 검사](../../tools/verify_validation.py#L23).
- [결정됨] 검토했으나 채택하지 않은 대안: permalink를 두지 않고 원시 링크를 제거하거나 별도 호스팅을 기본 보존 경로로 정하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H10).
