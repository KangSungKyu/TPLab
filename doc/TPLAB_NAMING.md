# TPLab 이름 통일

2026-10-07. 프로젝트 내부 이름을 TPLab으로 통일한다. 패키지 구조 전환이나 Release 발행은 이번 변경 범위에 포함하지 않는다. 첫 배포 목표 버전은 `0.0.1`이며 태그 `v0.0.1`은 실제 배포 검증을 마친 뒤 생성한다.

| 기존 | 현재 |
|---|---|
| `Assets/MyLab` | `Assets/TPLab` |
| `MyLab.Core.*`, `MyLab.Samples.*` | `TPLab.Core.*`, `TPLab.Samples.*` |
| `MyLab.*` assembly와 asmdef 이름 | `TPLab.*` |
| `MyLab` Editor 메뉴 | `TPLab` |
| `Assets/Editor/MyLab/setting.asset` importer 설정 위치 | `Assets/Editor/TPLab/setting.asset` |
| 예제 Player의 `-mylab-*` 인수 | `-tplab-*` |
| 소비 검증의 `MYLAB_*` 환경 변수·define | `TPLAB_*` |

## 기존 사용 코드와 자산

소비 프로젝트는 using·asmdef 참조·경로·메뉴 호출을 위 이름으로 변경한다. 새 namespace는 `TPLab.Core`와 기능 하위 namespace다. 이전 이름의 별도 호환 assembly는 제공하지 않는다. 이미 가져온 importer 설정이 있다면 폴더와 `.meta`를 함께 새 위치로 이동한다.

이번 프로젝트 이동은 모든 `.meta`와 GUID를 보존한다. 예제의 SceneTarget 경로와 `m_EditorClassIdentifier`도 새 이름을 사용한다. 사용자 작성 `InitScene`은 기존 오브젝트·직렬화 값을 보존하고 코어 타입 식별 문자열만 변경한다. 프로젝트 이름 관련 설정 네 필드만 TPLab으로 바꾼다.

로컬 checkout은 실행 중인 Editor와 Codex 작업의 경로인 `C:\Users\PC\Projects\MyLab`을 유지한다. 폴더의 물리 이름은 namespace·assembly·배포 이름과 독립적이다. 다른 PC에서는 원하는 폴더에 clone하고 도구의 `--project`에 해당 절대 경로를 전달한다.

## 기록과 검증

과거 검증 JSON·로그·회고의 MyLab 표기는 당시 실행 기록이다. 이번 이름 변경의 통과 증거로 사용하지 않는다. 과거 Markdown의 소스 링크만 새 위치로 갱신하고 현재 사람/AI README·API와 개발 문서의 이름·SourceRevision은 새 소스를 가리킨다.

현재 실행 결과와 미검증 영역은 [이름 변경 검증](validation/tplab-naming/README.md)에 기록한다. 이름 변경으로 새 게임 UI나 입력 정책을 추가하지 않으므로 이번 작업의 추가 시각 수락 항목은 없다. 외부 배포 패키지 설치 검증과 다른 Unity·플랫폼 지원은 별도 단계다.
