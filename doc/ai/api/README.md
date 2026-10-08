# AI API 색인

2026-10-07 이름 통일: namespace/assembly와 소스 경로는 `TPLab` / `Assets/TPLab`을 사용한다. [변경 안내](../../TPLAB_NAMING.md)에서 현재 이름·경로 규칙과 검증 기록을 확인한다.

SourceRevision: 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 remains the reference for the original seven modules. UI runtime remains P6 source `21f89e2580f08be3903724efa0a42e5ad0567c83`; final regression snapshot is `99df25dd6b89fd7f3e322c8033b61eb1364f495e`. Current validation is in the UI API. P3-P6 validation records are historical.

| Module | AI contract | Human contract |
|---|---|---|
| Pooling | [Pooling](Pooling.md) | [Human](../../api/Pooling.md) |
| Lifecycle | [Lifecycle](Lifecycle.md) | [Human](../../api/Lifecycle.md) |
| Resources | [Resources](Resources.md) | [Human](../../api/Resources.md) |
| DataTables | [DataTables](DataTables.md) | [Human](../../api/DataTables.md) |
| SceneManagement | [SceneManagement](SceneManagement.md) | [Human](../../api/SceneManagement.md) |
| Input | [Input](Input.md) | [Human](../../api/Input.md) |
| Editor | [Editor](Editor.md) | [Human](../../api/Editor.md) |
| UIContext (P1-P6 implemented; focused validation partial) | [UI](UI.md) | [Human](../../api/UI.md) |

항목 이름: Module/Namespace/Assembly, SourceRevision/SourcePath/HumanContract, ImplementationStatus/ValidationStatus/Evidence, Symbol/Signature/Constraints, Inputs/Outputs/Errors, Ownership/Lifecycle/Threading, Concurrency/Cancellation/FailureCleanup, Configuration/ExtensionPoints, RequiredSequence/ForbiddenUsage, Example/Compatibility/Limitations.

상태는 서로 대체하지 않는다. Implemented는 명시된 범위의 소스가 존재함, Partial/NotRun은 검증 범위다. UIContext is Implemented through P6 with Partial validation. P3 focused Edit18/18 and Play30/30, failed0/skip0, compile/product Console errors0; 48 tests include 18 P3 additions, not all separately Red-tested. See [P3 record](../../validation/ui-system/p3/README.md). P5 final UI tests passed Edit31/31 and Play48/48, failed0/skip0, with product Console empty ([Edit](../../validation/ui-system/p5/final-edit.json), [Play](../../validation/ui-system/p5/final-play.json)); 405 non-protected Git matches and six protected files were verified ([match record](../../validation/ui-system/p5/source-commit-match.json)). P6 final focused validation passed Edit33/33 and Play53/53, failed0/skip0 ([P6 record](../../validation/ui-system/p6/README.md), [Edit](../../validation/ui-system/p6/final-edit.json), [Play](../../validation/ui-system/p6/final-play.json)); P7 original regression passed Edit318/318 and Play306/306, failed0/skip0; base consumer and optional-Input Single/Additive automation passed their recorded scopes. Physical/visual acceptance remains pending. See [P7 evidence](../../validation/ui-system/p7/final-source-check/source-commit-match.json) and [current UI status](UI.md). Historical P2+P1 source/test evidence is [UI contract](UI.md), [Edit](../../validation/ui-system/p2/final-edit.json), [Play](../../validation/ui-system/p2/final-play.json)에 있다. P7 consumer/Player automation passed its recorded scopes; performance evidence is exploratory on one device. Physical/visual acceptance and main approval remain pending. UI source는 개발 Assets에만 있고 0.0.1 release packages/tag는 바뀌지 않았다. 로딩 progress는 Resources optional backend 경계, loading presentation callback은 SceneManagement 계약에 포함한다. [최종 loading 사용자 확인](../../SCENE_LOADING_ACCEPTANCE.md)은 2026-10-07에 완료됐으며 UIContext 수락을 뜻하지 않는다. 세부 XML 계약과 변경이 생기면 해당 모듈의 양쪽 문서를 동시에 갱신한다.
