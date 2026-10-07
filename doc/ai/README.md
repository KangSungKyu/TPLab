# TPLab AI 통합 안내

2026-10-07 이름 통일: namespace/assembly와 소스 경로는 `TPLab` / `Assets/TPLab`을 사용한다. [변경 안내](../TPLAB_NAMING.md)에서 현재 이름·경로 규칙과 검증 기록을 확인한다.

SourceRevision: `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`. 목적은 Unity 재사용 코어의 정확한 사용/수정이다. API 사실은 실제 public 선언/XML·구현·테스트로 대조한다. [사람용 README](../../README.md), [사람용 API](../api/README.md), [AI API](api/README.md)를 함께 유지한다. 아래의 승인은 소비 프로젝트 작업 권한을 부여하지 않는다. 소비 프로젝트 자체의 사용자 지시/지침을 따른다.

| Module | Namespace / Assembly | Source | Human / AI |
|---|---|---|---|
| Pooling | TPLab.Core.Pooling / TPLab.Core | [Pooling](../../Assets/TPLab/Core/Pooling) | [Human](../api/Pooling.md) / [AI](api/Pooling.md) |
| Lifecycle | TPLab.Core.Lifecycle / TPLab.Core | [Lifecycle](../../Assets/TPLab/Core/Lifecycle) | [Human](../api/Lifecycle.md) / [AI](api/Lifecycle.md) |
| Resources | TPLab.Core.ResourceManagement / TPLab.Core | [Resources](../../Assets/TPLab/Core/ResourceManagement) | [Human](../api/Resources.md) / [AI](api/Resources.md) |
| DataTables | TPLab.Core.DataTables / TPLab.Core | [DataTables](../../Assets/TPLab/Core/DataTables) | [Human](../api/DataTables.md) / [AI](api/DataTables.md) |
| SceneManagement | TPLab.Core.SceneManagement / TPLab.Core | [Scenes](../../Assets/TPLab/Core/SceneManagement) | [Human](../api/SceneManagement.md) / [AI](api/SceneManagement.md) |
| Input | TPLab.Core.Input / TPLab.Core.Input | [Input](../../Assets/TPLab/Input/Runtime) | [Human](../api/Input.md) / [AI](api/Input.md) |
| Editor | TPLab.Core.Editor.DataTables / TPLab.Core.Editor.Bootstrap / TPLab.Core.Editor | [Editor](../../Assets/TPLab/Editor) | [Human](../api/Editor.md) / [AI](api/Editor.md) |

RequiredSequence:

1. 필요한 모듈의 AI 계약 → HumanContract → SourcePath/XML → 관련 실제 테스트/호출자를 읽는다. 전체 회고를 읽는 것으로 현재 코드를 대신하지 않는다.
2. Core와 CsvHelper/UniTask/Addressables를 가져온다. Input은 선택 assembly + Unity.InputSystem1.19.0; Editor는 Editor 전용; Samples는 프로젝트 UI 예제. 설치 버전/라이선스는 사람 README를 따른다.
3. root host 또는 명시적 owner를 선택한다. singleton 접근을 모든 manager의 필수 기반으로 만들지 않는다.
4. 프로젝트 DTO/codec/FK/validator·source delegates·scene targets/root conditions·action assets/layers/UI callbacks를 설정한다.
5. 준비 완료를 await한 뒤 사용/입력을 공개한다. leases/구독을 소비자가 해제하고 시스템/native handles는 소유자가 정리한다.
6. 계약 변경 시 XML/양쪽 API 문서를 함께 갱신한다. source/tag와 실제 결과 입력을 비교하고 해당 프로젝트의 compile/runtime/사용자 gate를 수행한다.

ImplementationStatus: listed modules Implemented. SceneManagement progress snapshot, opt-in `SceneLoadingContext` flow and project-owned sample loading UI are implemented; UI remains outside Core runtime.
ValidationStatus: Partial; current P4 Edit271/271 and Play253/253, failed0/skip0; Windows Mono Additive/Single builds and Players passed (12/12 each), and the Input-included consumer completed one Editor build and one Player run. See [P4 evidence](../validation/scene-loading/p4/README.md). UserAcceptance: Confirmed by user PlayMode feedback on 2026-10-07; individual mode/resolution/device results were not supplied. Physical device coverage, remote Addressables content/download, IL2CPP and other platforms remain unverified. Historical P2 counts are not current-source evidence. CI is not configured.
ForbiddenUsage: Core에 게임 schema/UI/저장 정책 추가; runtime UnityEditor 참조; 공유 자산 cache에 씬 handle 혼합; borrowed manager/action을 임의 Dispose/Destroy; 로컬 경로/개인 Git 승인/세션/PC 종료 지시를 소비 계약으로 복사; 미검증 예제를 실행 성공으로 표시.
Limitations: 소스 복사 방식이며 UPM 배포본/자동 API generator는 없다. TPLab original code/docs: MIT; third-party code retains its own license/notices. See [License](../../LICENSE) and [Third-party notices](../../THIRD_PARTY_NOTICES.md). 실제 제공 전 source/tag 기준 설치·예제·상대 링크를 확인해야 한다.

DistributionPlan: [0.0.1 package/dev-build contract](../DISTRIBUTION_PIPELINE.md). Status: Proposed; package builder, artifact installation validation, tag and Release are NotImplemented/NotRun. Current installation remains source copy. Generated `tplab/` is a build-worktree output, not the development source or a supported Git UPM path. Internal operational permissions do not transfer to consumer projects.
