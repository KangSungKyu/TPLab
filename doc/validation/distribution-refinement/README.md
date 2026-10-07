# 배포 명세 보완 검증

기준 main `36e8ffbb0833293474da43396481895e5d8108d0`, 작업 `codex/distribution-refinement`. Git URL·tarball 설치, `upm/` 추적 사본·예제 분류와 다음 버전 계획을 문서에 반영했다. 실행 코드·Unity asset·manifest·실행 도구는 수정하지 않았다.

[보호 입력](preserved-inputs.json)은 기존 사용자 변경5개와 ProjectSettings의 bytes를 확인한다. [정적 결과](static-checks.json)는 현재 source 입력317개·보호 파일6개 불변, 문서 링크·공백·허용 변경을 확인한 결과다. Unity 테스트·compile·build·실제 설치 실행은 모두 **0건**이며 문서 작업에 실행 테스트 대상이 없다. 패키지 폴더 생성·예제 이동·GameUISystem 구현·tag/Release 발행은 미실행이다.

[GitHub gate](design-ci.json)는 문서 commit `d6f41320dbd775db497306851b9f68f3f0659691`의 보호·check/workflow 상태를 읽었다. main 비보호·workflow/check/status/run0으로 CI 미구성을 확인했다. CI 미구성은 Unity 검증 성공을 의미하지 않는다. 정적 검사와 exact commit gate 뒤 기존 문서 병합 정책에 따라 main에 반영하며 작업 tip이 main에 보존됐을 때만 작업 브랜치를 정리한다.
