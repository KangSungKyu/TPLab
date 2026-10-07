# 2026-10-07 · 외부 제공용 사람·AI 문서 지침

- 요청: 외부 프로젝트 제공을 고려한 사람용 README/API와 AI용 README/API의 문서화 지침 준비. 기존 기능의 전체 매뉴얼 작성이나 배포는 이번 범위가 아니다.
- 기준: `codex/input-system-track` / `f632c1d871372928be60e0b5b1a6bf8b776fb9b9`, 작업 `codex/documentation-guidelines`. 입력 작업의 사용자 수락 대기를 유지하며 사용자 dirty4와 기존 source를 보존한다.
- 변경: DOCUMENTATION_GUIDE에 문서별 경로·독자·필수 내용, source/XML/명세의 공통 근거, 구현/검증 상태, 사용·해제·취소 계약, 예제와 링크 검증·동시 갱신·제공본 경계를 정의했다. AGENTS의 필수 적용 절과 문서 색인/README 연결을 추가했다.
- 결정: AI용 문서는 별도 backend 규격이 아닌 짧고 일정한 Markdown 참조로 제공한다. 기존 사람용 계약 문서는 색인으로 재사용할 수 있어 장문을 중복 생성하지 않는다. 문서 생성기·package·배포 도구는 추가하지 않는다.
- 검증: 최초 문서6개/상대 링크139개, track 마무리 포함 문서7개/링크150개·공백·변경 범위·보호7 raw bytes 검사 통과. 순수 문서 변경으로 Unity 테스트 실행0건, compile/제품 Console/Play/Player 미실행. 기존 입력 테스트 결과를 이번 지침의 테스트 통과로 재사용하지 않는다.
- Git: `439cd5f93836a3b5d60a9a73afbebf607a38684b`를 문서 브랜치에 commit/push하고 입력 track에 fast-forward 통합·push했다. source304/보호7과 로컬·원격 main `ce290878da51c653eaa219aecc45e6bf61ac755a` 보존을 확인했다. 입력 구현을 조상으로 포함하므로 main 병합·작업 브랜치 삭제는 기존 사용자 수락 gate를 기다린다. 이 마무리 기록과 track 안내는 같은 문서 단위의 보완이다.
- 다음: 요청을 받으면 확정된 기능별로 실제 사람/AI 문서를 작성하고 제공할 source/tag·설치/예제 검증과 연결한다. 현재 예약 경로는 생성 완료가 아니다.
