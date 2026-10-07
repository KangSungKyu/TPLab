# P1 패키징 검증

기준 main `af9958ba402be2bc7f31d453afb2b14294a47ac8`, phase `codex/dev-build-v0.0.1-p1-packaging`, track `codex/dev-build-v0.0.1`. 부모가 구현·검증·Git을 담당했고 `/root/distribution_docs_review` (gpt-6-luna / low)는 기존 API와 builder를 읽기 전용 검토했다.

[Red](red.txt)는 미구현 builder의 18개 contract test를 실행해 실패를 확인했다. subtest를 포함해 failures21/errors3이다. [첫 구현](green-first.txt)은 failures3/errors4, [HTTPS 경로 수정 후](green-second.txt)는 errors1, [Git blob/줄바꿈 보정 후](green-third.txt)는18/18 통과했다. [최종 Green](green-final.txt)은 GUID·provenance·도구 버전 기록을 반영한 18/18(실패0·skip0)이다. 같은 입력 두 archive bytes 일치, 사본 커밋 후 재생성 일치와 drift 거부, clean/revision/version/run/output/문서 링크·license/GUID/symlink 거부를 포함한다.

[보호 입력](preserved-inputs.json)은 원본 개발 checkout의 사용자 변경5개와 ProjectSettings bytes다. 이 isolated worktree의 깨끗한 main 소스에는 해당 사용자 변경을 복사하지 않는다. 과거 source 입력317개를 대조하고 실행 Runtime/Editor·Assets·Packages·ProjectSettings가 불변인지 확인한다. P1은 Python 도구와 문서/배포 사본만 작업한다.

[실제 생성 결과](generation-checks.json)는 prepare source `90ba7d152ea27d902d6eedaa97fe73db71f2de20`, 사본 포함 source `ec1f786d248207a8c9245b6d5263f95a1985e093`를 구분한다. 171개 사본 파일의 원본 asset bytes·GUID를 보존했고 기본 mode의 사본 일치와 두 run의 archive bytes/manifest 일치를 확인했다. [배포 manifest](distribution-manifest.json)와 [SHA256](SHA256SUMS.txt)에 source 입력147개·매핑·도구 버전·각 package hash를 기록한다. [사본 커밋 후 테스트](green-committed.txt)는 정확한 해당 코드에서18/18 통과했다. [source CI 조회](source-ci.json)는 main 비보호·CI 미구성이며 CI 성공을 의미하지 않는다. Unity 테스트·compile·UPM 실제 설치·새 Player 실행은 **0건**이다. P1 계약 테스트 통과를 UPM 설치 통과로 확대하지 않는다. Samples는 P2에서 경로 이동·import 동작을 검증하며 P1 패키지에 포함하지 않는다. `v0.0.1` 태그와 Release는 발행하지 않는다.

원본 Unity meta와 raw unittest stderr의 직렬화/진행 출력 공백을 보존했다. 그 경로의 blank-at-eol만 제외하고 새 코드·문서·manifest의 공백 검사를 수행했다. 과거 실패 기록은 삭제하지 않았다. 현재 P1 후보는 worktree의 `tplab/verify-a/artifacts`에 보존하며 중복 prepare/verify-b는 영속 증거 보존 후 소유 경로만 정리한다. worktree/phase는 P2 이후 최종 통합에 필요해 유지한다.
