# 2026-10-06 · Phase 4 전환 정의와 root 조건

- 목표/기준: `codex/game-scenes-p4-definitions`, track `54107893f9da91e86f44be5766c5501e8de8e0df`. 설정 asset/ID와 직접 요청을 같은 root 조건 정책으로 연결한다.
- 변경/결정: detached 설정 snapshot, 명시적 요청/context, root-local 조건 계약과 preflight 거절을 제공했다. 편의 API도 공통 경로를 사용한다. 조건 false는 preflight에서만 false/거절이며 실행 중 false는 실제 취소와 cover 유지·후보 정리로 전달한다. first/add는 비동기 reveal 전후 검사 후에만 성공을 공개한다. Bootstrap getter는 선택 정의와 실행을 일치시키고 legacy 직렬화 값을 유지한다.
- 배정/리뷰: scene_runtime(6.1-sol/high)이 제한 Core/테스트를 담당했다. 부모가 Unity·문서·Git·최종 검증을 맡고 getter/reveal 경계의 추가 Red를 요청했다. readonly 최종 리뷰에서 추가 결함을 찾지 못했다.
- 검증: 최초 Red Edit 13/1/12/0·Play 25/0/25/0, getter Red 1/0/1/0·reveal Red 1/0/1/0, Green Edit 15/15/0/0·Play 26/26/0/0, 전체 Edit 228/228/0/0·Play 199/199/0/0. [증거](../validation/scene-definitions/README.md). FirstEntry source 경계 1건/일부 assertion의 별도 Red는 미실행이다. compile 완료·예상 로그 10건 보존 후 Console 오류/경고 0·원래 Editor ready, 보호 5개 raw bytes 유지.
- 문제/도구: reveal Red의 실행 전 discovery 실패는 결과/통과로 세지 않았다. runner project 인자를 절대 forward-slash로 통일했고 이후 실제 Red를 확인했다. path가 원인이라고 단정하지 않는다. 실제 실패의 재시도 정책은 바꾸지 않았다.
- Git/다음: 이번 소스와 증거를 commit/push 후 track에 원본 commit을 보존해 통합한다. main·브랜치 삭제는 사용자 확인까지 보류한다. Phase 5에서 실제 씬 asset·정의·조건 ID를 Inspector/compile/Play/build 검사에 연결한다. Evaluate는 Editor 검증에서 실행하지 않는다. 성공 Player·소비 프로젝트·실제 UX는 Phase 6이다.
