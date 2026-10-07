# TPLab 이름과 경로

2026-10-07. 현재 개발·사용 문서는 다음 이름을 기준으로 한다. 첫 배포 목표 버전은 `0.0.1`이며 `v0.0.1` 태그는 실제 배포 검증 뒤 생성한다.

| 대상 | 이름·경로 |
|---|---|
| 저장소·Unity Product Name | `TPLab` |
| 코어 소스 | `Assets/TPLab/Core` |
| namespace·assembly | `TPLab.Core.*`, `TPLab.Samples.*` |
| Editor 메뉴 | `TPLab` |
| importer 설정 | `Assets/Editor/TPLab/setting.asset` |
| 예제 Player 인수 | `-tplab-*` |
| 소비 검증 환경 변수·define | `TPLAB_*` |
| 현재 PC checkout | `C:\Users\PC\Projects\TPLab` |

소비 프로젝트의 using·asmdef 참조·경로·메뉴 호출은 이 이름을 사용한다. 소스 이동 시 `.meta`와 GUID를 보존한다. 이전 이름의 호환 assembly는 제공하지 않는다. 다른 PC에서는 원하는 폴더에 clone하고 도구의 `--project`에 실제 절대 경로를 전달한다.

## Unity Cloud 이름

Unity Editor의 `CloudProjectSettings.projectName`은 연결된 Cloud 프로젝트 이름이다. 로컬 `ProjectSettings.asset`만 바꾸면 Cloud의 이전 이름으로 다시 동기화될 수 있다. 동일한 Cloud 프로젝트의 표시 이름을 `TPLab`으로 바꾸고 Editor에서 확인한다. 프로젝트 ID·조직·서비스 연결은 유지한다.

현재 연결 프로젝트 ID는 `53eb3a3f-eeea-4548-8909-d737dae4a02c`다. [Unity 공식 변경 절차](https://docs.unity.com/en-us/cloud/projects/edit-project)는 Owner 또는 Manager 권한으로 Dashboard → 해당 프로젝트 → Settings → Project Details → 이름 Edit → Save다. Cloud 이름 변경 후 기존 연결 ID를 유지한 채 Editor의 Cloud 이름·Product Name·로컬 설정을 모두 현재 이름으로 확인했다. [정리 상태](validation/tplab-name-cleanup/README.md)를 확인한다.

## 기록과 검증

`doc/validation/`의 과거 로그·JSON과 `doc/retrospectives/`의 회고에는 당시 이름·절대 경로·해시가 남을 수 있다. 실행 사실과 검증 무결성을 위해 원문을 보존한다. 해당 기록의 명령을 현재 사용 지침으로 복사하지 말고 [현재 도구 안내](../tools/README.md)를 따른다. 현재 문서와 소스에는 위 이름을 사용한다.

[소스 이름 변경 검증](validation/tplab-naming/README.md)은 폴더 이동 전의 실제 실행 기록이다. 폴더 이동 이후 변경 범위와 검증은 [이름·경로 정리](validation/tplab-name-cleanup/README.md)에 구분한다. 패키지 구조 전환·Release 발행·다른 Unity 및 플랫폼 검증은 별도 단계다.
