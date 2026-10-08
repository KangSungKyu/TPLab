# 사람용 API

2026-10-07 이름 통일: namespace/assembly와 소스 경로는 `TPLab` / `Assets/TPLab`을 사용한다. [변경 안내](../TPLAB_NAMING.md)에서 현재 이름·경로 규칙과 검증 기록을 확인한다.

SourceRevision: 3062716f2d494bc61bf515f3fa30b1ee8aada9f0 remains the reference for the original seven modules. UIContext P4 source revision is deb222cf6fda752d3e0dd6d22bda67f9d60e9e16; focused P4 Edit24/24 and Play41/41 evidence is in [UI](UI.md). P3 source and execution records remain historical. Long design/policy details live in each module's HumanContract.

| 모듈 | 책임 | 소스 / AI 참조 |
|---|---|---|
| [Pooling](Pooling.md) | C# 객체·prefab 대여/반환 | [Core/Pooling](../../Assets/TPLab/Core/Pooling) / [AI](../ai/api/Pooling.md) |
| [Lifecycle](Lifecycle.md) | root·Singleton·준비·해제 | [Core/Lifecycle](../../Assets/TPLab/Core/Lifecycle) / [AI](../ai/api/Lifecycle.md) |
| [Resources](Resources.md) | Addressables 자산 cache·씬 backend/progress | [Core/ResourceManagement](../../Assets/TPLab/Core/ResourceManagement) / [AI](../ai/api/Resources.md) |
| [DataTables](DataTables.md) | CSV·idx·PK/FK·snapshot | [Core/DataTables](../../Assets/TPLab/Core/DataTables) / [AI](../ai/api/DataTables.md) |
| [SceneManagement](SceneManagement.md) | 씬 로드/전환·수명 tree·조건·로딩 표시/진행 대기 | [Core/SceneManagement](../../Assets/TPLab/Core/SceneManagement) / [AI](../ai/api/SceneManagement.md) |
| [Input](Input.md) | layer·rebind·override | [Input/Runtime](../../Assets/TPLab/Input/Runtime) / [AI](../ai/api/Input.md) |
| [Editor](Editor.md) | importer·설정·사전 gate | [Editor](../../Assets/TPLab/Editor) / [AI](../ai/api/Editor.md) |
| [UIContext](UI.md) | P1-P4 HUD/Popup/Canvas, modal input eligibility and optional Input System adapter (P4 focused Edit24/24, Play41/41) | [UI Runtime](../../Assets/TPLab/UI/Runtime) / [AI](../ai/api/UI.md) |

설치와 선택 의존성은 [README](../../README.md)를 먼저 읽는다. 시스템 주입은 Lifecycle → 필요한 service → SceneManagement/Input 순서다. 프로젝트가 DTO/codec/validator, action asset/layer, 씬 root와 UI callback을 제공한다. runtime API와 Samples/테스트 fixture/Editor 도구를 구분한다.

구현 상태와 검증 상태는 별개다. 기존 7개 모듈은 Implemented이며 기존 검증은 Unity6000.3.18f1/Windows Mono 범위의 Partial이다. UIContext P3 historical source는 `18666acae25b04c1c63ca49d8c00ca2ee67da308`다. P3 focused Edit18/18·Play30/30, failed/skip0, compile/product Console errors0이며 [P3 record](../validation/ui-system/p3/README.md)에 근거가 있다. P2+P1 historical results와 범위는 [UI API](UI.md) 및 그 안의 P2 Edit/Play 증거를 따른다. Consumer installation·Player·Profiler·UX·user acceptance remain P7 NotRun. 0.0.1 release packages/tag는 바뀌지 않았다. [SceneManagement 결과](../validation/scene-loading/p4/README.md)는 다른 버전·플랫폼 지원을 보장하지 않는다. [로딩 progress/팁/진행 대기](../SCENE_LOADING_PRESENTATION_DRAFT.md)는 구현됐으며 실제 UI는 프로젝트 callback이 소유한다. [최종 로딩 사용자 확인](../SCENE_LOADING_ACCEPTANCE.md)은 2026-10-07 PlayMode 확인으로 완료됐다.
