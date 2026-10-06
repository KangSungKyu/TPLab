# 2026-10-06 · 표준 데이터 등록·generic 조회

- 목표: 표준 DTO 매핑·종류 registry·Get/TryGet. 기준 main/7cd8955, 작업 codex/generic-data-tables. [codec 선행](2026-10-06-03-idx-codec.md).
- 상태: 구현·집중 검증 완료. [작업 순서](../DATA_TABLE_GENERIC_IMPLEMENTATION.md) 단계 2.
- 변경: CsvDataTable 기반, Text/ResourceKey DTO·기본 테이블, manager/snapshot 표준 등록·조회, StandardDataTableTests 및 meta.
- 결정: 두 CSV 경로가 같은 ReadTable 파이프라인을 사용한다. 표준만 CsvHelper ValidateHeader/GetRecord를 적용하며 manual 임의 키·0 key를 유지한다. factory table의 사용 플래그로 실패한 후보의 재사용도 막는다.
- 검증: 실제 Red 17/0/17/0, Green 17/17/0/0. [Red](../validation/generic-data-tables/standard-red.txt), [Green](../validation/generic-data-tables/standard-green.txt). 정확한 DTO, 같은 DTO의 다른 종류, 3요소 routing, header-only·ClassMap·Optional·타입 변환·검증 hook, 재로드 실패 보존·이전 세대 유지 확인.
- 컴파일 완료·제품 Console 오류 0건 확인. 전체 회귀·root/native·Player·소비 프로젝트는 이 단계에서 미실행.
- Git: 아직 미커밋·미통합; 기존 사용자 asset/settings 변경은 제외.
- 다음: 명시적 계약 binding·FK helper 및 전체 후보의 참조 검증.
