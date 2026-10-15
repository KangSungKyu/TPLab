# 07 · 검증 증거

[현행] SourceRevision: main `af084e0d0027c4098fad08c336ccb0753e1c7862`. 갱신일: 2026-10-15. 현재 지침·인용을 읽기/Git 조회로 대조했으며 Unity·테스트·Player 실행은 0회다. 기존 실행·집계 수치는 각 기록의 당시 source 기준이며 이번 재실행·재집계로 보지 않는다. [공통 근거와 검증 범위](README.md#근거와-검증-범위).

## 무엇을 확인했는지 구분하는 현행

- [현행] 코드 읽기는 실행 완료 증거가 아니며 source·환경·명령·test 이름·total/pass/fail/skip와 한계를 구분해 보고하는 현행 규칙이 있다. 근거: [AGENTS:42,43](../../AGENTS.md#L42), [백로그 보고:323](../IMPROVEMENT_BACKLOG.md#L323).
- [현행] IMP-03은 오라클·fixture 오류와 별도 승인 정정·재동결을 기록한 뒤44/44·전체Edit375/Play345를 남겼다; 처음의 잘못된 실패를 의도한 Red로 바꾸어 쓰지 않았다. 근거: [요약:7,17,25,27,65](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md#L7).
- [현행] exact-SHA 판정은 대상 SHA·조회 완전성·완료/success를 검사하며 CI 존재와 Unity 통과는 별개다. 근거: [CI 도구:1,72](../../tools/check_github_ci.py#L1), [AGENTS:118](../../AGENTS.md#L118).
- [현행] 현재 IMP-14/05/03은 Windows Mono Editor 중심이고 Player/IL2CPP는 미검증이다. 과거0.0.1 Mono Player 실행은 다른 source의 최신 계약 검증을 대신하지 않는다. 근거: [IMP-05:75](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L75), [IMP-03:74](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md#L74), [과거 Release:5,11](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/distribution-release/README.md#L5).

## 증거 소유와 최소 기계 입력

- [결정됨] **D-E — 회고 단일 소유.** 변경 하나의 증거 요약은 회고 한 곳에 두고 PR 본문은 그 회고를 링크한다. 새 작업의 validation README를 만들지 않으며 백로그·INDEX에는 상태 한 줄과 링크만 두고 수치·SHA를 복제하지 않는다. 근거: 사용자 2026-10-13 Phase1b D-E, [06장의 분류 관계](06-documentation.md#문서-4분류와-증거-소유).
- [결정됨] 검토했으나 채택하지 않은 대안: validation README·회고·PR·색인에 같은 요약을 각각 유지하는 안. 근거: 사용자 2026-10-13 Phase1b D-E, [중복 측정](08-change-procedure.md#15단계-측정-근거).
- [결정됨] **D-A·D-H10 — 원시와 예외.** 원시 산출물 비보관 원칙에서 현행 도구가 요구하는 최소 기계 입력만 예외로 보존한다. 아래 실제 입력 목록이 기준이며 “나중에 필요할 수 있음”을 이유로 모든 JSON·로그를 예외로 묶지 않는다. 도구는 이번에 수정하지 않는다. 근거: 사용자 2026-10-13 일괄 승인(D-H10), [필수 입력 읽기:31,37,42,60,61](../../tools/verify_validation.py#L31).
- [결정됨] 검토했으나 채택하지 않은 대안: 필수 기계 입력 예외 없이 요약만 남기도록 도구를 이번에 변경하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H10).
- [현행] verify_validation.py는 checks.json, checks.results가 이름을 정한 결과 JSON, test-inputs.json, preserved-inputs.json, final-console.json, final-status.txt를 읽는다. exact-SHA 도구의 --output JSON은 조회 출력이고 커밋된 validation JSON을 필수 입력으로 읽지는 않는다. 근거: [verify_validation:31](../../tools/verify_validation.py#L31), [CI 조회 출력:163,178](../../tools/check_github_ci.py#L163).
- [결정됨] **D-I5 — 입력 없는 과거 실행.** 필수 입력이 없는 과거 실행은 새 실행 없이 기록만 유지하며 현재 소스로 재검증하지 않는다. 검증기 17건의 역사적 기준선 유지 결정과 같다. 원본 해시 기록과 bytes 재검사 가능은 구분한다. 근거: 사용자 결정 2026-10-15(D-I5), [17건 역사적 기준선 결정](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/retrospectives/2026-10-15-04-docs-link-verifier.md).

## CI 판정 문서의 라벨 정정

- [현행] CI_GATE_PROPOSAL.md는 [AGENTS:118](../../AGENTS.md#L118)이 판정 기준으로 링크한다. 2026-10-13 보존 라벨 정정은 당시 기록이며, 현 위치는 아래 D-I4의 사용자 결정으로 확정한다. 근거: 실제 AGENTS 링크와 사용자 결정 2026-10-15(D-I4).
- [결정됨] **D-I4 — 위치 확정.** [CI_GATE_PROPOSAL.md](../CI_GATE_PROPOSAL.md)를 doc/로 이동한다. AGENTS의 승인된 링크 한 곳과 문서 링크 대상·이동 파일 자신의 상대 링크만 갱신하며 판정 내용·도구·workflow는 바꾸지 않는다. 근거: 사용자 결정 2026-10-15(D-I4), [06 위치 결정](06-documentation.md#기존-자료와-배포-사본).

## 삭제와 현행 도구의 충돌

| 상태 | 현재 요구/위치 | 새 결정과의 관계 |
|---|---|---|
| [현행] | [AGENTS:61](../../AGENTS.md#L61) | 증거 요약은 회고 한 곳과 최소 기계 입력 예외를 따르도록 이관 완료됐다. 근거: 실제 임시 파일·증거 지침과 [증거 상세](#증거-분류와-원문-보존-상세--2026-10-13-이관). |
| [현행] | [AGENTS:71](../../AGENTS.md#L71) | 기존 기록은 삭제 단계 별도 승인 전까지 보존하는 문구로 정렬됐다. 이번 요청은 원시 자료 삭제를 포함하지 않는다. 근거: 실제 이름·경로 유지 지침. |
| [현행] | [verify_validation:23,31](../../tools/verify_validation.py#L23) | 삭제된 과거 원본은 같은 로컬 입력 검사를 다시 수행할 수 없다. |
| [현행] | [consumer 입력:222,230](../../tools/run_core_consumer.py#L222) | 독립 tools 템플릿 입력이므로 동일 validation blob만으로 대체 필요성이 성립하지 않는다. 근거: [조회 정정](README.md#05단계-사실-확인). |
| [현행] | [Editor check:246](../../Assets/TPLab/Validation/Editor/DataTableImportEditorCheck.cs#L246) | 향후 실행은 기존 validation 폴더·JSON을 다시 만든다; 삭제만으로 새 출력 경로·정책이 구현되지는 않는다. |

- [결정됨] **D-H11 — 2단계 삭제 순서.** 직접 참조가 없는 leaf 폴더14개를 먼저 검토하고 이후 실행 단위별 링크·판정·도구 입력을 매핑한다. 실제 삭제는 목록 제시와 사용자 승인 뒤 별도 PR에서 수행한다. “참조 없음”은 폐기 안전의 증명이 아니다. 근거: 사용자 2026-10-13 일괄 승인, [0.5단계 조회](README.md#05단계-사실-확인).
- [결정됨] 검토했으나 채택하지 않은 대안: leaf 검토를 건너뛰고 오래된 실행 단위부터 바로 정리하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H11).
- [결정됨] **D-H10 — 고정 permalink.** 삭제된 과거 증거 링크는 자료가 존재하는 commit SHA에 고정한 permalink로 대체한다. 이는 읽기용 링크이며 로컬 원본 복원·재검증 능력이 아니다. 실제 링크 수정도2단계에서 수행한다. 근거: 사용자 2026-10-13 일괄 승인, [D-A](README.md#사용자-결정), [현재 입력 검사](../../tools/verify_validation.py#L23).
- [결정됨] 검토했으나 채택하지 않은 대안: permalink를 두지 않고 원시 링크를 제거하거나 별도 호스팅을 기본 보존 경로로 정하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H10).

## 증거 분류와 원문 보존 상세 — 2026-10-13 이관

[결정됨] 사용자 A2 승인에 따라 AGENTS 54·60·70행의 상세를 이관한다. 이관 당시의 후속 정리 예고는 역사적 기록이다. 현재 적용은 D-E와 아래 보존 경계를 함께 따른다. [회고 상세](06-documentation.md#단위-작업-회고-상세--2026-10-13-이관)가 형식·순번·인계를 소유한다.

- [결정됨] 확정·제안·미확정, 구현 완료·초안·검증 대기를 구분한다. 실행한 테스트의 수·실패·skip와 실행하지 않은 검사를 구분하고, 미실행은 통과로 기록하지 않는다. 컴파일·제품 Console·실행·Player의 확인 범위를 분리하며 source·환경·명령과 한계를 회고에 남긴다.
- [결정됨] 기존 명세·코드·검증 자료는 링크로 참조한다. 대화 전문·전체 로그·비밀값을 복제하지 않으며 영속 API·작업 규칙의 본문 소유권을 바꾸지 않는다. 과거 통과·승인·완료는 현재 검증이나 새로운 권한을 대신하지 않는다.
- [결정됨] 원시 산출물과 증거 요약을 구분한다. 이번 작업이 만든 일회성 출력은 작업별 임시 폴더에 두고, 필요한 요약은 회고 한 곳에 보존한다(D-E). 현행 도구가 요구하는 최소 기계 입력만 예외로 보존한다(D-A·D-H10). 진행 중 실행·참조가 없음을 확인한 뒤 그 작업의 임시 파일만 제거하고, 중단·차단 시에는 재개 파일의 위치·용도·제거 조건을 회고에 남긴다. 현재 요청이 삭제를 금지하면 제거하지 않는다.
- [결정됨] 기존 validation·회고 기록은 삭제 단계의 별도 승인 전까지 실행 당시 원문으로 보존한다. 현재 이름 검사에서는 과거 기록 영역을 구분하며 과거 명령의 절대 경로를 현재 사용 예제로 복사하지 않는다. 이번 이관은 기존 파일 삭제·이동·원문 교정을 승인하지 않는다.

### 이관 전 증거 문장 원문

아래는 기준 [AGENTS 60행](https://github.com/KangSungKyu/TPLab/blob/1ddc87d4116e9151f1b82836d664655d83270ba1/AGENTS.md#L60)과 [70행](https://github.com/KangSungKyu/TPLab/blob/1ddc87d4116e9151f1b82836d664655d83270ba1/AGENTS.md#L70)의 역사적 원문이다. 60행의 validation/회고 선택은 현재 D-E의 회고 단일 요약·최소 입력 예외로 조정됐고, 70행의 무기한 보존은 삭제 단계 승인 전까지의 보존으로 조정됐다. 나머지 요건을 생략하거나 기존 기록을 삭제하지 않는다.

```markdown
- 에이전트의 일회성 스크립트·중간 JSON·로그·빌드 출력은 작업별 임시 폴더에 둔다. 작업 완료 후 필요한 검증 증거를 `doc/validation/` 또는 해당 회고에 보존하고, 진행 중인 실행·참조가 없음을 확인한 뒤 이번 작업이 만든 임시 파일을 제거한다. 중단·차단되어 재개에 필요한 파일은 위치·용도·제거 조건을 회고에 남긴다.
- 과거 `doc/validation/` 로그·JSON·해시와 `doc/retrospectives/` 기록은 실행 당시 원문을 보존한다. 현재 이름 검사에서는 이 기록 영역을 구분하며, 과거 명령의 절대 경로를 현재 사용 예제로 복사하지 않는다.
```
