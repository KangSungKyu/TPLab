# 배포 후보 P3

2026-10-08. 상태: 자동 검증 완료 / 최종 사용자 확인 대기. 기준 track은 `5ffe2a9deece93ccc216cdff728f0f8da30661d0`, 작업 branch는 `codex/dev-build-v0.0.1-p3-candidate`, 검증한 최종 source는 `de879b9396ddae0523bd3ab86939b679b383dd92`다. main/tag/Release 발행은 후속 P4다.

목표는 고정한 clean source와 실제 UPM/tarball에서 회귀·소비 실행·정책·문서를 검증하는 것이다. 원본 Editor/main과 사용자 씬·설정 6개를 보존하고 detached `tplab/worktrees/p3-source`에서 후보를 검증했다. 원본 Editor는 별도 EditMode 271/271, PlayMode 253/253을 확인했다.

최초 manifest 교체 실패를 조사하고 외부 reader 잠금을 실제 Red로 재현했다. Windows 오류 32/33/1175에만 동일 원자적 File.Replace를 최대 5회/파일로 시도하며, 소진·다른 오류에는 복원과 예외 전파를 유지한다. 원래 첫 실패의 reader 원인은 확정하지 않았다.

전역 native input을 사용한 batch PlayMode 10개 실패는 graphics 설정만으로 해결되지 않았다. 공식 InputTestFixture로 가상 입력을 격리하고, loading 비동기 owner 정리 뒤 mock runtime을 복원하는 순서로 보정했다. 제품 Input runtime은 바꾸지 않았다. 실제 Git-full에서는 정상 226자 최종 파일에 임시 suffix를 더해 263자로 실패했다. 긴 경로 Red/Green을 확인하고 같은 폴더의 GUID.tmp로 이름만 줄였다. 소유권·meta·원자적 교체·복원 계약은 유지하며 임의의 긴 경로 지원을 보장하지 않는다. 최초 실패와 수정 근거는 [증거](../validation/distribution-candidate/README.md)에 보존했다.

최종 후보 결과: EditMode 286/286·PlayMode 253/253, 실패/skip 0. Git/tarball 8/8 구성, 소비 Editor 18회·기본 Player 8회·sample Player 4회로 총 30process. 빌드 12개 오류 0, sample 48checks. Python 33 methods 중 32pass/1skip(OS symlink 권한). 두 생성의 archive 3개 bytes/manifest가 일치한다. Core/Input hash는 P2와 같고 Editor만 갱신됐다. 변경한 package 입력은 Editor 구현과 사람/AI Editor API의 3개 파일뿐이다. 원본 main af9958·사용자 파일 6개 hash·Editor PID28008을 보존했다. CI 미구성/unprotected 확인은 CI 실행 성공이 아니다.

부모가 구현·Unity·검증·문서·Git 통합을 담당했다. 기존 distribution_editor_probe(gpt-6.1-sol/medium)는 diff·정리 순서·문서의 읽기 전용 리뷰를 맡았다. 정적 리뷰와 실제 실행을 구분한다.

P3 commit/push와 track fast-forward 통합 후 사용자 확인을 기다린다. 사용자는 **확인할 예정이야**라고 답했으며 최종 수락은 Pending이다. [최종 확인](../DISTRIBUTION_ACCEPTANCE.md)에서 Package Manager/sample UI·실제 입력·화면비·복원을 확인한다. 과거 PlayMode 피드백은 새 UPM workflow의 수락을 대신하지 않는다. 이후 P4에서 main·source 태그·첨부물 다운로드/tag URL 검증·브랜치 정리·Unity/PC 종료를 진행한다.

보존: `tplab/worktrees/p3-source/tplab/p3-final2-a` archive와 `p3-final2-b` 재현본, `p3-source/Temp/DistributionConsumer/git-full-p3-b` 및 `tarball-full-p3-b` 확인 프로젝트. 실패/회귀 raw log는 p3-source/tplab/p3-*에, 기타 소비 실행은 p3-source/Temp/DistributionConsumer의 이번 고유 run에 보존한다. Release 안내 초안은 p1/tplab/p3-release-notes-draft에 있다. 중복 evidence와 사용이 끝난 일회성 스크립트는 증거 보존 후 소유 목록만 정리한다. P4 완료 뒤 나머지 생성본도 필요한 진단을 보존하고 정리한다. 원본 Temp 전체·다른 작업·사용자 변경은 삭제하지 않는다. 타PC/IL2CPP/다른 Unity·플랫폼/remote Addressables/물리 장치·시각 수락을 자동 통과로 확대하지 않는다.
