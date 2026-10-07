# 사람용 API

2026-10-07 이름 통일: namespace/assembly와 소스 경로는 `TPLab` / `Assets/TPLab`을 사용한다. [변경 안내](../TPLAB_NAMING.md)에서 현재 이름·경로 규칙과 검증 기록을 확인한다.

SourceRevision: `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`. 아래 문서는 현재 구현된 소비 API다. 긴 설계·정책 본문은 모듈 문서의 HumanContract/상세 계약 링크가 소유한다. 문서 블록은 선언 또는 설명용 발췌이며 별도 실행 여부를 각 문서에 표시한다.

| 모듈 | 책임 | 소스 / AI 참조 |
|---|---|---|
| [Pooling](Pooling.md) | C# 객체·prefab 대여/반환 | [Core/Pooling](../../Assets/TPLab/Core/Pooling) / [AI](../ai/api/Pooling.md) |
| [Lifecycle](Lifecycle.md) | root·Singleton·준비·해제 | [Core/Lifecycle](../../Assets/TPLab/Core/Lifecycle) / [AI](../ai/api/Lifecycle.md) |
| [Resources](Resources.md) | Addressables 자산 cache·씬 backend/progress | [Core/ResourceManagement](../../Assets/TPLab/Core/ResourceManagement) / [AI](../ai/api/Resources.md) |
| [DataTables](DataTables.md) | CSV·idx·PK/FK·snapshot | [Core/DataTables](../../Assets/TPLab/Core/DataTables) / [AI](../ai/api/DataTables.md) |
| [SceneManagement](SceneManagement.md) | 씬 로드/전환·수명 tree·조건·로딩 표시/진행 대기 | [Core/SceneManagement](../../Assets/TPLab/Core/SceneManagement) / [AI](../ai/api/SceneManagement.md) |
| [Input](Input.md) | layer·rebind·override | [Input/Runtime](../../Assets/TPLab/Input/Runtime) / [AI](../ai/api/Input.md) |
| [Editor](Editor.md) | importer·설정·사전 gate | [Editor](../../Assets/TPLab/Editor) / [AI](../ai/api/Editor.md) |

설치와 선택 의존성은 [README](../../README.md)를 먼저 읽는다. 시스템 주입은 Lifecycle → 필요한 service → SceneManagement/Input 순서다. 프로젝트가 DTO/codec/validator, action asset/layer, 씬 root와 UI callback을 제공한다. runtime API와 Samples/테스트 fixture/Editor 도구를 구분한다.

구현 상태와 검증 상태는 별개다. 현재 모듈은 Implemented, 검증은 Unity6000.3.18f1/Windows Mono 범위의 Partial이다. [현재 결과](../validation/scene-loading/p4/README.md)는 다른 버전·플랫폼 지원을 보장하지 않는다. [로딩 progress/팁/진행 대기](../SCENE_LOADING_PRESENTATION_DRAFT.md)는 구현됐으며 실제 UI는 프로젝트 callback이 소유한다. [최종 사용자 확인](../SCENE_LOADING_ACCEPTANCE.md)은 2026-10-07 PlayMode 확인으로 완료됐다.
