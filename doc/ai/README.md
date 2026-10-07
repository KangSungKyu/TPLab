# TPLab AI 통합 안내

SourceRevision: `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`. 목적은 Unity 재사용 코어의 정확한 사용/수정이다. API 사실은 실제 public 선언/XML·구현·테스트로 대조한다. [사람용 README](../../README.md), [사람용 API](../api/README.md), [AI API](api/README.md)를 함께 유지한다. 아래의 승인은 소비 프로젝트 작업 권한을 부여하지 않는다. 소비 프로젝트 자체의 사용자 지시/지침을 따른다.

| Module | Namespace / Assembly | Source | Human / AI |
|---|---|---|---|
| Pooling | MyLab.Core.Pooling / MyLab.Core | [Pooling](../../Assets/MyLab/Core/Pooling) | [Human](../api/Pooling.md) / [AI](api/Pooling.md) |
| Lifecycle | MyLab.Core.Lifecycle / MyLab.Core | [Lifecycle](../../Assets/MyLab/Core/Lifecycle) | [Human](../api/Lifecycle.md) / [AI](api/Lifecycle.md) |
| Resources | MyLab.Core.ResourceManagement / MyLab.Core | [Resources](../../Assets/MyLab/Core/ResourceManagement) | [Human](../api/Resources.md) / [AI](api/Resources.md) |
| DataTables | MyLab.Core.DataTables / MyLab.Core | [DataTables](../../Assets/MyLab/Core/DataTables) | [Human](../api/DataTables.md) / [AI](api/DataTables.md) |
| SceneManagement | MyLab.Core.SceneManagement / MyLab.Core | [Scenes](../../Assets/MyLab/Core/SceneManagement) | [Human](../api/SceneManagement.md) / [AI](api/SceneManagement.md) |
| Input | MyLab.Core.Input / MyLab.Core.Input | [Input](../../Assets/MyLab/Input/Runtime) | [Human](../api/Input.md) / [AI](api/Input.md) |
| Editor | MyLab.Core.Editor.DataTables / MyLab.Core.Editor.Bootstrap / MyLab.Core.Editor | [Editor](../../Assets/MyLab/Editor) | [Human](../api/Editor.md) / [AI](api/Editor.md) |

RequiredSequence:

1. 필요한 모듈의 AI 계약 → HumanContract → SourcePath/XML → 관련 실제 테스트/호출자를 읽는다. 전체 회고를 읽는 것으로 현재 코드를 대신하지 않는다.
2. Core와 CsvHelper/UniTask/Addressables를 가져온다. Input은 선택 assembly + Unity.InputSystem1.19.0; Editor는 Editor 전용; Samples는 프로젝트 UI 예제. 설치 버전/라이선스는 사람 README를 따른다.
3. root host 또는 명시적 owner를 선택한다. singleton 접근을 모든 manager의 필수 기반으로 만들지 않는다.
4. 프로젝트 DTO/codec/FK/validator·source delegates·scene targets/root conditions·action assets/layers/UI callbacks를 설정한다.
5. 준비 완료를 await한 뒤 사용/입력을 공개한다. leases/구독을 소비자가 해제하고 시스템/native handles는 소유자가 정리한다.
6. 계약 변경 시 XML/양쪽 API 문서를 함께 갱신한다. source/tag와 실제 결과 입력을 비교하고 해당 프로젝트의 compile/runtime/사용자 gate를 수행한다.

ImplementationStatus: listed modules Implemented. 새 로딩 UI progress/tips/자동·버튼 proceed 단계는 [Proposed](../SCENE_LOADING_PRESENTATION_DRAFT.md), NotRun이며 API를 추측해 호출하지 않는다.
ValidationStatus: Partial; [source9305 결과](../validation/input-system/p4/README.md) Edit258/Play217, reload8, Windows Mono/consumer 범위. 물리 gamepad/touch·IL2CPP·다른 버전 미검증. 사용자 입력 확인은 개별 장치 실행 증거를 대신하지 않는다. CI 미구성.
ForbiddenUsage: Core에 게임 schema/UI/저장 정책 추가; runtime UnityEditor 참조; 공유 자산 cache에 씬 handle 혼합; borrowed manager/action을 임의 Dispose/Destroy; 로컬 경로/개인 Git 승인/세션/PC 종료 지시를 소비 계약으로 복사; 미검증 예제를 실행 성공으로 표시.
Limitations: 소스 복사 방식이며 UPM 배포본/자동 API generator는 없다. TPLab 외부 라이선스 정책 미정. 실제 제공 전 source/tag 기준 설치·예제·상대 링크를 확인해야 한다.
