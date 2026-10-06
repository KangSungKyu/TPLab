# 2026-10-06 · generic 데이터 테이블 통합 검증

## 작업과 기준

- 목표: 확정된 generic 기준 구현, 임의 PK 수동 경로 보존, 전체 검증 후 승인된 통합.
- 기준 main/7cd8955, 작업 codex/generic-data-tables. [구현 순서](../DATA_TABLE_GENERIC_IMPLEMENTATION.md), [binding/FK 선행](2026-10-06-05-data-table-binding-fk.md).
- 상태: MyLab 내부 구현·검증 완료. 소비 프로젝트/Player 배포 검증과 구분한다.

## 결정과 변경

- Core/DataTables의 기본 codec·DTO·테이블, manager/snapshot 표준 generic API·binding·FK를 추가했다. 기존 CSV pipeline·후보의 단일 공개·비동기 owner 수명을 재사용했다.
- fixture에서 표준 경로를 선택하게 하고 Singleton root/native TextAsset 연결 2건과 표준 async 3건을 검증했다. 기존 manual fixture 경로는 유지했다.
- 테스트 스캔/전역 PK 중복 index/DI/dynamic·패키지·게임별 DTO·scene/settings 변경을 추가하지 않았다. 이전 snapshot은 불변 router와 DTO/테이블을 유지하며 DTO setter는 소비자가 수정하지 않는 계약이다.
- 문제와 배운 점: CLI filter는 완전한 이름이 필요했다. 0건을 실패 실행으로 오인하지 않고 다시 정확한 filter로 Red를 확보했다. doc 갱신 script의 문자열 불일치는 남은 파일부터 보완했다. Git 공백 검사에서 나온 기존 InitScene 경고는 사용자 변경이며 이번 allowlist 검사에서 제외했다.
- Git: 작업 branch 생성 및 원격 fetch로 기준 main과 일치 확인. 최종 allowlist commit/push·원격 커밋 검사 후 main fast-forward 통합을 진행한다. 이 기록 이후의 정확한 commit과 통합 결과는 Git 이력·최종 보고에서 확인한다. 삭제/이력 재작성 없음.

## 검증과 한계

- codec Red 13/1/12/0→Green 13/13/0/0; 표준 Red 17/0/17/0→Green 17/17/0/0; binding/FK Red 18/2/16/0→Green 18/18/0/0.
- 표준 async 3/3/0/0, 자산/root·Resource 회귀 20/20/0/0, 전체 EditMode 110/110/0/0(외부 Addressables 예제 1 포함), PlayMode 87/87/0/0. [증거·입력 hash·재현](../validation/generic-data-tables/README.md).
- 정확한 MyLab 기존 Editor PID 23176의 컴파일 완료, 정상 Console 오류/경고 0. 예상 실패 경로 로그는 별도 보관했다.
- 기존 InitScene·SceneTemplateSettings hash 보존. 문서·meta/GUID·allowlist 정적 검사를 수행한다.
- 소비 프로젝트·Player/IL2CPP·최종 시각 UX·실제 원격 bundle·대용량 프레임 예산 미실행. 새 수동 확인이 필요한 UI/자산 변경은 없다.

## 다음 작업

- GameSceneManager 요구사항·전환 책임·준비/해제 실패 계약을 확정한다.
- 공용 코어 배포 완료 전에 별도 소비 Unity 프로젝트·최소 예제·Player/IL2CPP 매핑을 검증한다. MyLab 테스트만으로 AOT/stripping 호환성을 단정하지 않는다.
