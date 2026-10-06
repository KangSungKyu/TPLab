# 2026-10-06 · Text·Resource 예시 템플릿 분리

## 작업과 기준

- 목표: 사용자 요청에 따라 Text/Resource 구체 DTO·테이블을 core에서 제거하고 프로젝트용 예시 템플릿으로 남긴다.
- 기준 main/8e44803, 작업 codex/data-table-templates. [선행 회고](2026-10-06-06-generic-data-table-validation.md), [매핑 계약](../DATA_TABLE_MAPPING_DRAFT.md).
- 상태: MyLab 내부 분리·자동 검증 완료.

## 결정과 변경

- DataRow.cs에서 TextRow/ResourceKeyRow/TextDataTable/ResourceKeyDataTable을 제거하고 기존 meta/GUID를 보존했다. 공용 IDataRow/DataRow·CsvDataTable·manager/snapshot·codec은 유지했다.
- [예시 소스](../../Assets/MyLab/Tests/Fixtures/DataTableTemplates.cs)는 별도 namespace MyLab.Examples.DataTables와 기존 TestFixtures assembly에 둔다. 새 package/assembly·복제된 예시 코드를 만들지 않고 실제 예시를 기존 테스트에서 컴파일·사용한다. [프로젝트 복사·수정 방법](../templates/data-tables/README.md)을 추가했다.
- 테스트 5파일의 예시 참조를 변경하고 core에 구체 4타입이 없음을 확인하는 검사 4건을 추가했다. 예시 검증 정책은 유지하며 runtime 기본 정책으로 강제하지 않는다.
- CLI의 일시적 instance discovery 오류 후 기존 assembly 18건만 실행된 결과를 제외했다. refresh 완료 후 새로운 4검사가 포함된 실제 Red 22건을 확보했다. 실행 수/검사 이름과 최신 코드 입력을 같이 확인해야 한다.
- Git: 승인된 작업 branch 생성. 이번 allowlist만 commit/push하고 해당 커밋의 원격 검사·입력 hash를 확인한 뒤 main fast-forward 통합을 진행한다. 최종 commit과 실제 통합은 Git 이력·최종 보고에서 확인한다. 기존 사용자 scene/settings는 제외하며 이력 재작성·삭제는 하지 않는다.

## 검증과 한계

- Red 22/18/4/0 → Green 22/22/0/0. 최종 EditMode 114/114/0/0(외부 Addressables 예제 1 포함), PlayMode 87/87/0/0. [실행 증거·입력 hash](../validation/data-table-templates/README.md).
- 기존 MyLab Editor PID 23176에서 컴파일 완료, 정상 Console 오류/경고 0. 예상 실패 경로 로그는 별도로 보관했다.
- 코드·meta/GUID·문서 링크/색인·변경 범위·공백과 사용자 InitScene/SceneTemplateSettings hash 보존을 확인한다.
- 소비 프로젝트 복사·Player/IL2CPP·최종 시각 UX 미실행. 기존 Core의 구체 타입을 쓰는 소비자는 프로젝트 예시/자체 타입으로 namespace/등록을 변경해야 한다. 이번 프로젝트 내 사용처는 모두 전환·검증했다.

## 다음 작업

- GameSceneManager의 요구사항·전환 소유권·async 준비/해제 실패 계약을 확정한다.
- 공용 코어 배포 검증 시 소비 프로젝트에서 템플릿 복사·스키마 수정·Player 매핑을 확인한다.
