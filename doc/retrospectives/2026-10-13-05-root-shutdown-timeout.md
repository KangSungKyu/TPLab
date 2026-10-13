# IMP-03 · root 종료 타임아웃

2026-10-13. 구현·자동 검증 완료, 사용자 수락·PR #11 병합 승인 완료. branch `codex/imp-03-shutdown-timeout`, main `5725e90` 기준. PR #10 통합 후 명시적 재개 지시.

- 부모: 테스트·Editor·문서·Git·최종 검증. 기존 imp04_survey_verification(6.1-sol/high): 수명·예외 구분 검토와 runtime6개 구현. imp01_pool_impl(6.1-sol/medium): 문서/API caller 읽기 전용 검토. 새 사용자 작업·동시 Editor 조작 없음.
- 목표: 단계별 unscaled deadline, 기본60/finite0이하무제한, 원래 오류 소유권·예외 identity·역순 해제/Dispose 유지. ISceneRoot signature·패키지·게임 정책 유지.
- 첫 실수: AsTask의 Faulted/OCE 변환을 놓쳤다. 원래Red28/18/10·실제 변환 보존, 사용자 승인 뒤3곳만 기존 방식의 취소 예외 검사로 정정. 이후28/19/9는 모두 미완료 실패.
- 둘째 실수: 설정16건 수만 먼저 봐 SetUp 실패를 API Red로 오인. runtime변경0에서 중단·원문16건 보존. 별도 사용자 승인 후 UTF 소유 임시씬 절차만 수정. 기존 계약·16case 유지, 이후 API/직렬화Red16·Setup/TearDown0·자산잔여0.
- 교훈: 수가 맞아도 모든 실패 원문을 대조해야 한다. framework bootstrap/사용자 씬 복원 소유자를 구분하고, 취소 Task 상태와 get-result 예외를 같은 것으로 보지 않는다.
- 정정 후 구현 전 frozen5 재동결. NUnitTimeout 만료0, local1초 assertion과 driver23.068초/19.443초를 구분. [원문과 정정 기록](../validation/imp-03/README.md).
- source `5fe5bf5a41386409775d836e5b3a50a2f516a787` 커밋. 동결root28/28·설정16/16·전체375/345 allpass·skip0, 기존fixture/user6·원본554/검증11 보존. legacy누락/명시0 8case와MaxValue Green4검사·Editor원래씬/시간배율/상태복원 확인. Console10은기존LogAssert. Player/IL2CPP·CPU/GC·외부소비·시각UX 미실행. 문서 최종HEAD의규칙/Actions·PR은완료보고/Git이력 참조. 2026-10-13 PR #11 사용자 승인으로 통합 gate를 충족했다. 이번 요청에 Unity/PC종료 지시없음.

- 구현 검토: tagged capture로 프로젝트 TimeoutException과 own deadline을 구분하고 각Release의실패identity를보존했다. 실제 preparation running flag로promise취소와owner종료를구분해CTS를지연해제한다. 영원한비협조 작업의retainedCTS와lateResource접근은project계약위반의한계다. LateRelease 테스트는cleanup/state보존, 실제failure소비는staticawait/catch 검토로구분하며추적source/GC 검증은미실행이다.

## 2026-10-13 사용자 수락·통합

- 승인 tip `2cf57533b4773248b04a53a212979d70ca98c3c1`, [PR #11](https://github.com/KangSungKyu/TPLab/pull/11). 해당 tip Actions workflow1/check1 success·조회누락0을 재확인했다.
- 같은 작업 브랜치를 재사용해 승인 문서7개만 갱신한다. runtime/test source와 기존 dirty6는 보존하며 Unity 회귀를 다시 실행하지 않는다. 최종 문서 tip의 규칙/Actions를 확인한 뒤 원본 커밋을 보존하는 일반 merge, main 동기화 및 해당 로컬·원격 브랜치 정리를 진행한다. 실제 최종 HEAD·merge SHA·정리 결과는 PR/Git 이력 및 완료보고를 따른다.
- 자동 큐 IMP-04/01/02/14/05/03은 모두 승인·완료다. 큐 밖 항목은 자동 시작하지 않는다.
