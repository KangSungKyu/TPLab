# 입력·공개 API 문서 최종 통합

2026-10-07. [사용자 수락](acceptance.json)은 입력 확인과 문서화 요청을 기록한다. 새 loading presentation 구현은 범위 밖이다. 입력 source는 `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`, 문서 branch 기준은 track `502ab4c4b53e1dc8740fc105f6d2e1e1532365dd`다.

- 기존 실제 Edit258/Play217와 reload/consumer/Player 범위는 [p4](../README.md)가 소유한다. 이번 문서화의 새 Unity 테스트 실행은0건이다.
- source304개·보호7개 hash와 문서 링크/공백/계약을 정적으로 대조한다. [문서 검사](../../../documentation/public-api-checks.json)는 테스트 실행 결과가 아니다.
- 최신 원격 main·정확한 통합 commit의 CI 구성·작업 tip의 포함·로컬/원격 일치를 확인한 뒤 일반 Git 통합과 승인된 브랜치 삭제를 수행한다. 기존 unrelated branch/사용자 변경은 제외한다.
- main 최종 SHA와 브랜치 tip/삭제 관찰은 통합 후 이 폴더에 기록한다. SHA에서 조사/복원할 수 있고 원본 commit은 main에서 도달 가능하게 보존한다.
