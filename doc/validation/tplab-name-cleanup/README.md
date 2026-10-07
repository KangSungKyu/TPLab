# TPLab 이름·경로 후속 정리

2026-10-07. 기준 main `6828547da628d70f49e374bde138041de9b4c476`, 작업 브랜치 `codex/tplab-name-cleanup`.

- 현재 README·사람/AI API 안내·작업 지침·도구 예제·씬 실행 안내·이름 안내 11개 문서를 현재 이름과 실제 checkout 경로로 정리했다. 영속 지침에 이름과 Cloud 동기화 확인, 과거 기록 구분을 추가했다.
- 이미 새 이름으로 생성된 대체 파일이 있는 Git ignored `.csproj` 11개와 동일한 `.slnx` 1개를 제거했다. [제거 목록](removed-generated-files.json)의 명시한 root 파일만 대상이다. 소스·자산·캐시 전체를 삭제하지 않았다.
- [로컬 프로젝트 이름](local-project-setting.json)은 단일 `projectName` 필드만 현재 이름으로 복원했다. 연결된 Cloud 프로젝트 ID와 다른 설정은 유지한다. 변경 전 live Editor에서 Cloud 이름이 이전 이름임을 확인했고 사용자가 Dashboard에서 같은 프로젝트 이름을 변경했다. 이후 live Editor에서 이름과 ID를 재확인했다.
- 초기 연결 재확인 시 Editor가 종료되어 있었으나 사용자가 Cloud 이름을 변경하고 Editor를 다시 열었다. 이후 동일 checkout의 PID28008·Unity6000.3.18f1·Connector0.4.1에서 ready, 컴파일 idle, 제품 Console 오류·경고0을 확인했다. [실제 이름 확인](native-name-check.json)은 Cloud 이름·Product Name이 현재 이름이며 연결 ID 유지, 이전 assembly0, 현재 Core 로드를 확인한다.
- Runtime·Editor 코드·asmdef·패키지·테스트 동작 변경이 없다. 이번 단위 테스트 실행은 0건이며 통과로 기록하지 않는다. 현재 사용 영역416개 파일의 이전 이름0건, 소스 입력317개 불변, 문서106개·로컬 링크1099개 유효, 보호 파일5개의 예상 변경 외 byte 보존을 확인했다. 공백 검사도 통과했다. [정적 결과](static-checks.json)를 따른다. 이전 전체 테스트 결과를 이번 실행으로 표현하지 않는다.
- `doc/validation/`의 이전 실행 로그·JSON·해시와 `doc/retrospectives/` 원문은 변경하지 않는다. 이전 물리 경로와 타입 이름은 당시 실행 사실이다. 실제 현재 명령은 [도구 안내](../../../tools/README.md)를 사용한다.

## Cloud 확인과 통합

[공식 절차](https://docs.unity.com/en-us/cloud/projects/edit-project)에 따라 Cloud 프로젝트 `53eb3a3f-eeea-4548-8909-d737dae4a02c`의 표시 이름 변경 후 실제 checkout의 Editor에서 `CloudProjectSettings.projectName`, 로컬 `projectName`, Product Name을 모두 현재 이름으로 확인했다. 현재 세션에는 Dashboard 조작 runtime 도구가 없어 사용자가 직접 이름을 변경했고, 에이전트가 이후 live Editor에서 동기화 결과를 확인했다. 계정·프로젝트 연결 해제나 새 Cloud 프로젝트 생성으로 대체하지 않는다.

사용자 씬·Volume·Render Pipeline·SceneTemplate 변경은 보존한다. 조사 중 별도 Build Settings 변경도 발견했으며 이 작업의 Git stage와 통합 대상에서 제외한다. 로컬 `projectName`의 단일 변경은 명시한 이름 정리 범위다. 원본 보호 해시와 예상 변경은 [보호 입력](preserved-inputs.json), [설정 변경](local-project-setting.json), [정적 결과](static-checks.json)에 기록한다.

Cloud 이름 변경과 Editor 동기화 확인을 완료했다. 현재 범위에 추가 사용자 수락 항목은 없으며 정적 검사·exact commit 원격 검사를 마친 뒤 기존 승인에 따라 main 통합·브랜치 정리를 진행한다.
