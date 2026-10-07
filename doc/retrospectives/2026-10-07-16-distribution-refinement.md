# Git URL·예제 분류와 다음 버전 계획

- 요청: 첫 배포의 Git URL/`.tgz` 병행과 Core/Input 예제 분류를 채택하고, 다음 버전의 Core 외부 의존성 분리 검토·GameUISystem을 기록한다.
- 기준: main `36e8ffbb0833293474da43396481895e5d8108d0`; 작업 `codex/distribution-refinement`. 선행 [배포 설계 회고](2026-10-07-15-distribution-design.md)를 확인하고 기존 사용자 파일5개를 보존한다.
- 변경: [배포 규격](../DISTRIBUTION_PIPELINE.md)에 추적할 `upm/` 사본과 생성 전용 `tplab/`, 두 설치 방식의 검증, 사본 커밋 이후 후보 SHA 고정, 예제 분류·GUID 보존을 반영했다. 사람/AI README·색인·운영 지침을 맞추고 [다음 버전 계획](../CORE_PLAN.md#다음-버전-계획-2026-10-07)을 추가했다.
- 결정: Git URL은 태그의 실제 package.json을 읽으므로 ignored 생성 공간을 설치 경로로 안내하지 않는다. 원본→배포 사본을 검토·커밋한 뒤 후보 SHA를 고정하고 사본/tarball 내용 일치를 검증한다. 사본 내부에 미래 커밋 SHA를 넣지 않는다.
- 의존성: Pool 소스 자체에는 외부 plugin 의존성이 없으나 Core assembly에 포함되어 전체 설치 의존성을 공유한다. 다음 버전 검토는 이를 줄일 최소 경계를 찾는 작업이다. GameUISystem은 별도 설계 후 구현하며 현재 프로젝트 소유 가림막·로딩 callback 계약을 임의로 바꾸지 않는다.
- 검증: 문서만 변경해 Unity 테스트·compile·build·설치 실행0건. source 입력317개·보호 파일6개 hash 불변, 문서113개·로컬 링크1159개·diff·허용 범위·공백 검사를 통과했다. 실제 결과는 [문서 검증](../validation/distribution-refinement/README.md)을 따른다. 과거 Unity 결과를 이번 실행으로 사용하지 않는다.
- Git: 문서 commit `d6f41320dbd775db497306851b9f68f3f0659691`을 푸시하고 exact commit gate에서 CI 미구성·main 비보호를 확인했다. 마감 문서도 exact gate 뒤 기존 승인에 따라 main에 반영하고 해당 작업 브랜치만 정리한다. 회고를 포함하는 최종 SHA·통합 결과는 Git 이력과 최종 보고에서 확인한다.
- 다음: P1 패키징 구현. 예제 실제 이동·UPM 설치·tag/Release 발행은 후속 Phase며 아직 수행하지 않았다. 차기 버전 번호·의존성 분리 API·GameUISystem 계약은 첫 배포 이후 별도 설계로 확정한다.
