# 단위 작업 회고 색인

작성·읽기 규칙은 [AGENTS.md의 단위 작업 회고](../../AGENTS.md#단위-작업-회고)를 따른다. 작업과 관련된 최신 기록 및 필요한 선행 기록만 읽는다. 기록 당시의 상태를 현재 checkout의 완료·검증 증거로 대신하지 않는다.

| 날짜 | 단위 | 상태 | 기록 |
|---|---|---|---|
| 2026-10-02 | 회고 작성·후속 문맥 확인 지침 추가 | 문서 완료 | [01-unit-retrospectives](2026-10-02-01-unit-retrospectives.md) |
| 2026-10-02 | uint idx·생성/추출·PK/FK 계약 | 초안 | [02-data-table-idx-draft](2026-10-02-02-data-table-idx-draft.md) |
| 2026-10-06 | Parts·Stride·선택적 localType 계약 보완 | 초안 | [01-idx-parts-stride](2026-10-06-01-idx-parts-stride.md) |
| 2026-10-06 | 제네릭 데이터 조회 구현 준비 | 준비 완료·미구현 | [02-generic-data-table-ready](2026-10-06-02-generic-data-table-ready.md) |
| 2026-10-06 | idx codec 구현 | 집중 검증 완료 | [03-idx-codec](2026-10-06-03-idx-codec.md) |
| 2026-10-06 | 표준 데이터 등록·generic 조회 | 집중 검증 완료 | [04-standard-data-query](2026-10-06-04-standard-data-query.md) |
| 2026-10-06 | 테이블 binding·FK | 집중 검증 완료 | [05-data-table-binding-fk](2026-10-06-05-data-table-binding-fk.md) |
| 2026-10-06 | generic 데이터 테이블 통합 검증 | MyLab 내부 구현·검증 완료 | [06-generic-data-table-validation](2026-10-06-06-generic-data-table-validation.md) |
| 2026-10-06 | Text·Resource 예시 템플릿 분리 | MyLab 내부 분리·검증 완료 | [07-data-table-templates](2026-10-06-07-data-table-templates.md) |
| 2026-10-06 | 임시 파일 지침·Editor importer 검토 | 지침 완료·설계 초안·미구현 | [08-data-table-importer-draft](2026-10-06-08-data-table-importer-draft.md) |
| 2026-10-06 | Importer 설정 자산·공용 validator 보완 | 설계 초안 보완·미구현 | [09-importer-settings-validation](2026-10-06-09-importer-settings-validation.md) |
| 2026-10-06 | CSV Editor importer·설정·공용 검증 구현 | MyLab 내부 구현·검증 완료 | [10-data-table-importer](2026-10-06-10-data-table-importer.md) |
| 2026-10-06 | Bootstrap 먼저·게임 씬 Additive 권장안 | 권장 문서 완료·전환 설계 초안·runtime 미구현 | [11-bootstrap-additive-design](2026-10-06-11-bootstrap-additive-design.md) |
| 2026-10-06 | BootstrapSystem 최초 진입·Editor/Play/build 사전 검증 | MyLab 내부 구현·검증 완료 | [12-bootstrap-system](2026-10-06-12-bootstrap-system.md) |
| 2026-10-06 | SceneTransition Phase 1 계약·callback 공용화 | MyLab 내부 구현·검증 완료, manager 후속 | [13-scene-transition-contracts](2026-10-06-13-scene-transition-contracts.md) |
| 2026-10-06 | GameSceneManager Phase 2 최초 진입·Bootstrap 위임 | MyLab 내부 구현·검증 완료, 연속 교체 후속 | [14-game-scene-entry](2026-10-06-14-game-scene-entry.md) |
| 2026-10-06 | 최종 병합 후 작업 브랜치 정리 지침 | 지침 반영·검증 완료 | [15-post-merge-branch-cleanup](2026-10-06-15-post-merge-branch-cleanup.md) |

## 작성 형식

작업 규모에 맞게 짧게 작성한다. 해당 없는 항목은 해당 없음으로 표시하며 명세·검증 설명을 반복 복제하지 않는다.

```markdown
# 날짜 · 단위 작업명

## 작업과 기준

- 목표·요청 범위:
- 기준 branch/HEAD와 작업 branch:
- 상태: 완료 / 초안 / 중단 / 차단
- 관련 명세·선행 회고:

## 결정과 변경

- 변경 파일·결과:
- 핵심 결정·이유:
- 문제·원인·해결과 배운 점:
- Git 수행·남은 통합:

## 검증과 한계

- 실제 실행 수 / 실패 / skip, 증거 링크:
- 컴파일·Console·실행·Player 등 확인/미실행 범위:

## 다음 작업

- 미완료·위험·미확정:
- 다음 행동·선행 조건:
```
