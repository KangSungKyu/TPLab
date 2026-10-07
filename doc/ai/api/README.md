# AI API 색인

SourceRevision: `18d479bf07fe8479a22187ec7357024e9979d096`. [AI README](../README.md)의 설치·읽기 순서를 따른다. signature 블록은 public 선언 발췌이며 private/internal 구현 API는 소비자 사용 대상이 아니다.

| Module | AI contract | Human contract |
|---|---|---|
| Pooling | [Pooling](Pooling.md) | [Human](../../api/Pooling.md) |
| Lifecycle | [Lifecycle](Lifecycle.md) | [Human](../../api/Lifecycle.md) |
| Resources | [Resources](Resources.md) | [Human](../../api/Resources.md) |
| DataTables | [DataTables](DataTables.md) | [Human](../../api/DataTables.md) |
| SceneManagement | [SceneManagement](SceneManagement.md) | [Human](../../api/SceneManagement.md) |
| Input | [Input](Input.md) | [Human](../../api/Input.md) |
| Editor | [Editor](Editor.md) | [Human](../../api/Editor.md) |

항목 이름: Module/Namespace/Assembly, SourceRevision/SourcePath/HumanContract, ImplementationStatus/ValidationStatus/Evidence, Symbol/Signature/Constraints, Inputs/Outputs/Errors, Ownership/Lifecycle/Threading, Concurrency/Cancellation/FailureCleanup, Configuration/ExtensionPoints, RequiredSequence/ForbiddenUsage, Example/Compatibility/Limitations.

상태는 서로 대체하지 않는다. Implemented는 소스가 존재함, Verified/Partial/NotRun은 증거 범위다. 로딩 progress는 Resources optional backend 경계, 표시/진행 대기 callback은 SceneManagement 계약에 포함한다. 프로젝트 UI는 Core 소유가 아니며 [최종 사용자 확인](../../SCENE_LOADING_ACCEPTANCE.md)은 대기 중이다. 세부 XML 계약과 변경이 생기면 해당 모듈의 양쪽 문서를 동시에 갱신한다.
