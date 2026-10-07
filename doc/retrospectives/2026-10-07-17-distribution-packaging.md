# P1 패키징 구현

- 요청: 사람/AI README·API를 포함해 첫 배포의 P1 패키징을 구현한다. 기준 main `af9958ba402be2bc7f31d453afb2b14294a47ac8`; track `codex/dev-build-v0.0.1`, phase `codex/dev-build-v0.0.1-p1-packaging`을 별도 Git worktree에서 작업한다. [선행 회고](2026-10-07-16-distribution-refinement.md)를 확인했다.
- 변경: 표준 Python [builder](../../tools/build_distribution.py)와 [contract tests](../../tools/test_build_distribution.py), `/tplab/` ignore, 기존 사람/AI 문서 입구·운영 계약을 갱신했다. Runtime/Editor 코드·project settings·sample 이동은 수정하지 않는다.
- 결정: Git blob bytes로 source를 읽어 checkout 줄바꿈/filters의 차이를 없앤다. package source/API/라이선스를 선별하고 link projection·GUID/DLL 검사를 한 뒤 새 output에 생성한다. prepare 이후 사본은 명시적으로 검토·커밋하고 기본 mode에서 파일/bytes 일치를 요구한다. generator는 Git/설치/publish를 수행하지 않는다.
- 문제/해결: 최초 구현의 절대 경로 검사가 HTTPS를 잘못 탐지해 보정했다. fixture Git 설정에 따른 asmdef CRLF/LF 차이는 정규화된 fixture와 Git blob 판독으로 재현·수정했다. 사본 읽기는 batch로 처리한다. 과거 API SourceRevision과 오래된 evidence SHA 차이는 의도된 역사 구분이므로 원문을 바꾸지 않았다.
- 배분: `/root/distribution_docs_review` / gpt-6-luna low, API link·builder 경계의 읽기 전용 검토. 파일/Git/Unity 변경·새 테스트 실행 없이 완료했다. 부모가 구현·최종 검증·Git 통합을 소유한다.
- 검증: 실제 Red18 methods 실패, 최종 Green18/18·실패0·skip0. 중간 실패를 숨기지 않고 [증거](../validation/distribution-packaging/README.md)에 남긴다. 기존 사용자 변경과 source317개를 보존하며 Unity 테스트/compile/UPM 실제 설치/새 Player는0건이다.
- Git: 검증한 P1을 track으로 순차 통합한다. 최종 main/tag/Release와 작업 브랜치 정리는 P2/P3의 필요한 자동·사용자 gate 뒤다. prepare `90ba7d1`, 사본 포함 `ec1f786`에서 실제3개 archive 생성·사본 일치·동일 입력 재현을 확인했다. 해당 커밋의 GitHub gate는 CI 미구성·main 비보호다.
- 다음: P2의 Git URL/tarball 소비 설치·Core/Input 예제 이동 및 UPM import·Editor importer 연결 검증. 현재 P1 산출물은 development candidate, publishable false이며 정식0.0.1 배포 완료가 아니다.

- 완료: 원본 asset bytes·source317·parent 보호6개 불변, 커밋된 사본171 files, actual3 archives 및 두 run bytes/manifest 일치. package installation/Player 미실행을 manifest에 유지했다. 마감 문서는 source 후보 ec1f786의 역사 증거이며 이후 문서 커밋을 해당 archive source로 바꾸지 않는다. 자체 중복 생성물만 정리하고 현재 후보·build worktree는 P2까지 보존한다.
