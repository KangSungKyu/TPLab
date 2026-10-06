# 2026-10-06 · 테이블 binding·FK

- 목표: 명시적 interface binding, 후보 snapshot의 FK 검증. 기준 main/7cd8955, 작업 codex/generic-data-tables. [표준 조회 선행](2026-10-06-04-standard-data-query.md).
- 상태: 구현·집중 검증 완료. [작업 순서](../DATA_TABLE_GENERIC_IMPLEMENTATION.md) 단계 3.
- 변경: manager BindTable/RegisterForeignKey, snapshot GetTable 계약 조회, DataTableBindingTests 및 meta.
- 결정: 기본·명시적 계약은 같은 테이블 인스턴스, legacy dictionary는 같은 Rows를 반환한다. FK를 기존 전체 후보 validator 목록에 연결해 전역 중복 색인·별도 FK 엔진을 만들지 않았다.
- 검증: 실제 Red 18/2/16/0, Green 18/18/0/0. [Red](../validation/generic-data-tables/binding-red.txt), [Green](../validation/generic-data-tables/binding-green.txt). 필수/선택/null/0/잘못된 종류/없는 행, 자기·순환 관계, 후보 대상 제거 후 기존 binding 유지, null/예외/이전 성공·실패 후보 factory 재사용 거부 확인. 이미 동작하는 factory·resource 규칙 2건은 Red에서도 통과했다.
- MyLab 컴파일 완료. 아직 전체 회귀·root/native 연결·Player·소비 프로젝트 미실행.
- Git: 미커밋·미통합, 사용자 scene/settings 변경을 제외한다.
- 다음: standard native CSV/root 준비 및 비동기 수명 집중 검사 후 전체 회귀·문서·통합.
