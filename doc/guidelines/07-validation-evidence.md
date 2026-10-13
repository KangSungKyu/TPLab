# 07 · 검증 증거 초안

[현행] SourceRevision은 main35508bc이며 이 장의 새 확인은 코드 읽기·Git 조회다; 아래 Mono Editor 실행 수치는 해당 요약 문서의 기존 source 기록이고 이번 Unity/Player 실행은0회다. 근거: [공통 범위](README.md#근거와-검증-범위), [문서 전용 검증:74](../DOCUMENTATION_GUIDE.md#L74).

## 무엇을 확인했는지 구분하는 현행

- [현행] 코드 읽기는 구현/예외 경로 관찰이고 실행 완료 증거는 아니다; 실행 보고는 source·환경·명령·test 이름·total/pass/fail/skip와 한계를 구분한다. 근거: [AGENTS:38,39](../../AGENTS.md#L38), [백로그 보고:323](../IMPROVEMENT_BACKLOG.md#L323).
- [현행] IMP-03은 오라클 오류와 fixture Setup 오류의 원문·별도 승인 정정·재동결을 보존한 뒤44/44·전체Edit375/Play345를 기록했다; 최초 잘못된 실패를 의도한 Red로 덮어쓰지 않는다. 근거: [요약:7,17,25,27,65](../validation/imp-03/README.md#L7).
- [현행] 최종 HEAD의 exact-SHA Actions 판정은 대상 SHA·조회 완전성·완료/success를 검사하며, workflow 존재/CI 미구성은 Unity 테스트 통과가 아니다. 근거: [CI 도구:1,72](../../tools/check_github_ci.py#L1), [AGENTS:125](../../AGENTS.md#L125).
- [현행] 현재 IMP-14/05/03의 새 계약은 Windows Mono Editor 중심이고 Player/IL2CPP·다른 플랫폼은 미검증이다; 과거0.0.1의 Windows Mono Player 검증은 존재하나 다른 source의 최신 계약 검증으로 대체하지 않는다. 근거: [IMP-05 NotRun:75](../validation/imp-05/implementation/README.md#L75), [IMP-03:74](../validation/imp-03/README.md#L74), [과거 Release:5,11](../validation/distribution-release/README.md#L5).

## 사용자 결정과 새 증거 보존 제안

- [결정됨] D-A에 따라 원시 실행 JSON/로그/XML 등의 저장소 보관을 중단하고 요약·판정 근거는 남긴다; 기존 자료의 삭제 목록을 사용자에게 먼저 보여주는2단계 별도 PR이며 이 단계에서 삭제나 이력 재작성을 하지 않는다. 근거: [D-A 결정 기록](README.md#사용자-결정).
- [결정됨] CI 판정 기준으로 링크되는 CI_GATE_PROPOSAL.md는 보존 대상이다; 이 파일을 원시 로그와 같이 삭제하지 않는다. 근거: 사용자 D-A 추가 제약과 [AGENTS:125](../../AGENTS.md#L125), [해당 문서](../validation/ci-review-p1/CI_GATE_PROPOSAL.md).
- [제안] 새 저장소 증거는 요약 README에 source/HEAD·명령·환경·선택 test·수치·원문 실패 요약·해시 대상/정규화·판정·NotRun·exact-SHA Actions 링크를 남기고 원시 산출물은 작업별 임시 출력으로 두는 안이다; 대안은 아래 현행 도구의 최소 기계 입력만 예외 보존한다. 근거: [현행 보고 항목:323](../IMPROVEMENT_BACKLOG.md#L323), [입력 검증:31](../../tools/verify_validation.py#L31), [D-A](README.md#사용자-결정).
- [제안] 원시를 커밋하지 않는 기본안은 사후 해시의 “기록”과 원본 bytes의 “재검사 가능”을 구분하고, 재현하려면 동일 source/환경에서 새 실행으로 새 요약을 만드는 안이다; 대안은 원본 입력을 별도 제공 위치에 보관하는 안을 사용자 선택으로 정한다. 근거: [bytes 재검사:23,37](../../tools/verify_validation.py#L23), [환경 한계:22](../DOCUMENTATION_GUIDE.md#L22).

## 삭제와 현행 도구의 충돌

| 상태 | 현재 요구/위치 | 새 정책과의 관계 |
|---|---|---|
| [현행] | [AGENTS:60](../../AGENTS.md#L60) | 필요한 증거를 validation/회고에 보존한다는 현행 문구를 D-A의 요약 중심 기준과 대조해야 한다. 근거: [D-A](README.md#사용자-결정). |
| [현행] | [AGENTS:70](../../AGENTS.md#L70) | 과거 로그·JSON·해시 원문 보존 문구는 기존 원시 삭제 방향과 충돌한다; 이번 PR에서 AGENTS를 바꾸지 않는다. 근거: 같은 결정·이번 제외 범위. |
| [현행] | [verify_validation.py:31,37,42,60,61](../../tools/verify_validation.py#L31) | checks.json, checks.results가 이름을 정한 결과JSON, test-inputs.json, preserved-inputs.json, final-console.json, final-status.txt를 읽는다; 삭제된 과거 원본을 대상으로 같은 검증을 다시 수행할 수 없다. |
| [현행] | [check_github_ci.py:3,163,178](../../tools/check_github_ci.py#L163) | --output의 조회 JSON은 caller가 경로를 정하는 실행 출력이며, 커밋된 validation JSON을 필수 입력으로 읽는 도구가 아니다. |
| [현행] | [consumer 템플릿 입력:222,230](../../tools/run_core_consumer.py#L222) | 독립 tools 템플릿을 읽는다; validation과 동일 blob이라는 사실만으로 대체 입력이 필요하지는 않다. 근거: [조회 정정](README.md#05단계-사실-확인). |
| [현행] | [Editor check:246](../../Assets/TPLab/Validation/Editor/DataTableImportEditorCheck.cs#L246) | 향후 실행은 validation 폴더·JSON을 다시 만든다; 삭제만으로 향후 출력 정책이나 도구 경로가 바뀌지는 않는다. |

- [제안] 최소 기계 입력을 원시 보관 예외로 둘지, summary-only에 맞게 도구를 별도 변경할지 선택하는 안이다; exact-SHA 보고 JSON은 임시로 조회·확인하고 요약/PR에 SHA·조회 완전성·결과 링크를 남기는 대안도 비교한다. 이 PR은 도구·CI를 변경하지 않는다. 근거: 위 파일 입력 목록, [exact-SHA 구현](../../tools/check_github_ci.py#L72), [D-A](README.md#사용자-결정).
- [제안] Phase2는 원시 후보1,216파일/52.5MB와 요약51파일/0.3MB를 분리하고, 직접 참조 없는 leaf14개/4.1MB를 상대적으로 낮은 참조 비용의 첫 검토 후보로 삼는 안이다; 대안은 오래된 실행단위별로 정리하되 링크/판정/도구 입력을 먼저 매핑한다. “참조 없음”은 폐기 안전의 증명이 아니다. 근거: [0.5단계 조회·범위](README.md#05단계-사실-확인).
- [제안] 이력에 남은 원시의 commit SHA permalink를 과거 읽기용 링크로 사용하는 안이며, raw를 다시 로컬 입력으로 확보해 검증하는 절차나 별도 제공 위치는 후속 사용자 선택이다. 근거: [D-A 이력 보존 결정](README.md#사용자-결정), [현재 파일 입력](../../tools/verify_validation.py#L23).
