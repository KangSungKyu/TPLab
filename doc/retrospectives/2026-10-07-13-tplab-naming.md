# TPLab 이름 통일

2026-10-07. 상태: 구현·자동 검증·리뷰·main 반영 완료. 아래 Git 마감 문서를 반영한 뒤 이번 작업 브랜치를 정리한다.

- 요청: 현재 Assets/MyLab 개발 구조를 유지하면서 프로젝트·namespace·assembly·경로·문서를 TPLab으로 통일한다. UPM 전환이나 Release 발행으로 확장하지 않는다.
- 기준: main `11d62236edfac6d3c85f02daa5a3da7ad7d98d8f`, 새 `codex/tplab-naming`. 기존 dirty4를 확인하고 보존했다. 읽기 전용 조사 뒤 이름 변경만 별도 브랜치에 기록했다.
- 변경: Assets/TPLab로 이동, namespace·assembly11개·메뉴·importer 생성 코드/설정 경로·예제 SceneTarget·Player 인수·consumer 환경 변수/define과 도구를 함께 변경했다. metadata180개 raw bytes와 GUID를 유지했다. ProjectSettings 이름4필드만 변경하며 물리 checkout 경로는 기존 Editor/Codex 연결을 위해 유지했다.
- 문서: 사람/AI README와 API·현재 계약을 새 source `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`에 맞췄다. 과거 검증 JSON·로그·회고는 당시 증거로 보존하고 Markdown 소스 링크만 복구했다. [이름 변경 안내](../TPLAB_NAMING.md)가 이전 이름의 migration을 소유한다.
- 문제: 첫 Edit271 중2개가 기존 테스트의 Build Settings 원복 File.WriteAllBytes에서 Win32 IO1224로 실패했다. 테스트·예제 빌드의 두 동일 복원 지점에서 Unity의 AssetDatabase.ReleaseCachedFileHandles를 호출하고 테스트에 bytes 일치 검사를 추가했다. 잘못된 Player output 경로1회는 실행 전 거부였으며 실행0건으로 기록했다.
- 검증: 최초 Edit269pass/2fail/skip0, 수정 후 Edit271/271·Play253/253(실패0·skip0). Additive/Single Windows Mono build 각1회(errors0/warnings0), Player 각12/12. Core만/Core+Input 독립 소비 프로젝트 각build1/Player1(actualRuns2) 성공. 원래 Editor PID23120·Console 오류/경고0, preview7씬 root각1/누락스크립트0. 도구 self-check3개 각1회 통과. [전체 증거](../validation/tplab-naming/README.md)를 따른다.
- 보호: 사용자 InitScene 오브젝트·값을 보존하며 namespace 식별 문자열만 변경했고 이 씬을 commit에서 제외했다. 다른 dirty3은 raw bytes 유지했다. Unity 빌드가 자동 수정한 URP/settings7개는 명시 목록의 snapshot/기준 Git 원본으로 복원했다. source317/보호7·문서104개·GUID 검사 통과. 사용자 씬의 기존 trailing whitespace는 그대로 두며 이번 staged 변경의 공백 검사는 별도로 수행한다.
- Git: 이름 변경 소스 `6d0139efe3a217d33b6c3e2f811e03476895e4da`와 파일 핸들 복원 수정 `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`을 별도 commit으로 보존·push했다. 정확한 소스 GitHub 검사에서 CI 미구성 확인(CI 성공 아님). 마지막 문서/evidence commit은 실제 Git 이력에서 확인한다. 필요한 검사와 사용자 시각 gate가 남아 있지 않아 기존 승인 조건에 따라 main 반영·이번 브랜치만 SHA 조건부 삭제한다.
- 한계/다음: 이번 결과는 source 이름 변경의 기존 동일 Unity/Windows Mono 회귀다. 다른 PC·Unity·IL2CPP·배포 패키지 설치는 미실행이다. 사용자 후속 질문 후 배포 규격/파이프라인을 결정한다. 목표 버전0.0.1의 실제 태그·Release는 아직 생성하지 않는다. PC 종료/절전은 이번 요청 범위 밖이다.
- 임시 파일: 영속 결과와 consumer/Player 로그를 doc/validation에 보존한 뒤 이번 Temp/TPLabNaming, ScenePlayers/TPLab-Naming-Additive·Single, PlayerRuns/TPLab-Naming-Additive·Single만 경로·프로세스 사용 여부를 확인해 제거한다. 다른 Temp 파일과 사용자 자산은 제외한다.

2026-10-07 Git 마감: 문서·증거 commit `9ae5c15355132b80384541e9371b53e109439805`의 exact GitHub gate와 원격 main/작업 tip을 확인했다. main을 `11d6223`에서 `9ae5c15`로 강제 옵션 없는 로컬 fetch로 fast-forward하고 서버 main push/일치를 확인했다. 이 방식은 현재 검증 checkout을 중간 구버전으로 교체하지 않는다. source317/보호7·문서/GUID 검사 통과, 작업 diff의 코드·문서 공백 검사 통과. 원본 Unity 로그의 trailing whitespace는 증거 보존을 위해 검사 대상에서 제외했다. 추가 테스트 실행0건이며 기존 결과의 source 입력이 그대로다. 이 마감 commit까지 main에 보존·push한 뒤 `codex/tplab-naming` 하나만 서버 tip SHA 조건으로 삭제하며 최종 SHA는 Git 이력과 완료 보고에 남긴다.
