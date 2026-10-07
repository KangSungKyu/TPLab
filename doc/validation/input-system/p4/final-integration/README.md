# 입력·공개 API 문서 최종 통합

2026-10-07. [사용자 수락](acceptance.json)은 입력 확인과 문서화 요청을 기록한다. 새 loading presentation 구현은 범위 밖이다. 입력 source는 `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`, 문서 branch 기준은 track `502ab4c4b53e1dc8740fc105f6d2e1e1532365dd`다.

- 기존 실제 Edit258/Play217와 reload/consumer/Player 범위는 [p4](../README.md)가 소유한다. 이번 문서화의 새 Unity 테스트 실행은0건이다.
- source304개·보호7개 hash와 문서 링크/공백/계약을 정적으로 대조한다. [문서 검사](../../../documentation/public-api-checks.json)는 테스트 실행 결과가 아니다.
- 최신 원격 main·정확한 통합 commit의 CI 구성·작업 tip의 포함·로컬/원격 일치를 확인한 뒤 일반 Git 통합과 승인된 브랜치 삭제를 수행한다. 기존 unrelated branch/사용자 변경은 제외한다.
- 입력·문서 main 통합/push commit: `9648ff5feb48219931cdd8b24f12115b58e9805d`. local main/origin main/server main 일치를 확인했다. [정확한 문서 commit CI](documentation-ci-policy.json)는 unprotected·workflow/check/status/run0, CI 미구성이다.
- Phase4개와 문서 지침 branch의 local/remote 삭제를 확인했고 [삭제 tip 관찰](phase-branch-cleanup.json)에 보존했다. 해당 원본 tip은 위 pushed main의 ancestor다. native Player raw log의 원래 후행 공백은 증거 보존을 위해 수정하지 않았고 C#/Markdown/Python/JSON/asmdef의 diff 공백 검사는 통과했다.
- 이 후속 기록을 포함하는 commit도 기존 문서 branch→track→main으로 fast-forward/push한다. 기록 branch `codex/public-api-documentation`과 `codex/input-system-track`은 이 기록 commit을 main에 보존한 뒤 동일 SHA 조건·worktree·ancestor 검사로 삭제한다. 두 최종 tip과 최종 main SHA는 마지막 작업 보고에 남긴다. 이 파일의 source/main 통합 SHA는 스스로를 포함하는 commit SHA를 미리 적는 방식으로 갱신하지 않는다.
- SHA에서 조사/복원할 수 있으며 main에서 도달 가능한 원본 이력을 보존한다. 새로운 Unity 실행0건, source304/보호7개 대조 통과. UI progress/팁/자동·버튼 proceed는 여전히 미구현 초안이다.
