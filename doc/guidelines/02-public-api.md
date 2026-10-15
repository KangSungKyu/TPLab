# 02 · 공개 API 설계

[현행] SourceRevision: main `af084e0d0027c4098fad08c336ccb0753e1c7862`. 갱신일: 2026-10-15. 현재 지침·인용을 읽기/Git 조회로 대조했으며 Unity·테스트·Player 실행은 0회다. 기존 실행·집계 수치는 각 기록의 당시 source 기준이며 이번 재실행·재집계로 보지 않는다. [공통 근거와 검증 범위](README.md#근거와-검증-범위).

## 호환성: 호출 가능과 기존 바이너리 결합

- [현행] IMP-01은 ObjectPool 생성자 마지막에 optional `isAlive`를 더했다: 기존 소스 호출은 호환되지만 CLR 생성자 시그니처가 바뀌어 미리 컴파일된 소비 어셈블리는 재빌드가 필요하다. [생성자:49](../../Assets/TPLab/Core/Pooling/ObjectPool.cs#L49), [IMP-01:25](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-01/README.md#L25), [사람 API:51](../api/Pooling.md#objectpoolt).
- [현행] IMP-03은 concrete root의 Configure와 Attach에 optional `shutdownTimeoutSeconds = 60f`를 더했다; 기존 소스에서 생략하면 새60초 정책을 사용하며 reflection/delegate의 정확한 시그니처와 기존 소비 어셈블리는 재바인딩/재빌드가 필요하다. 따라서 이번 현행 두 사례는 아래 D-B를 이미 만족한 사례가 아니다. [Configure:46](../../Assets/TPLab/Core/Lifecycle/SceneOwnedRoot.cs#L46), [Attach:28](../../Assets/TPLab/Core/Lifecycle/SceneRootSetup.cs#L28), [Lifecycle API:100](../api/Lifecycle.md#종료-시간-제한).
- [결정됨] D-B: 앞으로 옵션을 추가할 때 기존 공개 CLR 시그니처를 유지하고 옵션 객체 또는 새 오버로드로 확장한다; IMP-01/03의 기존 API를 이번 문서 PR에서 바꾸라는 결정은 아니다. [사용자 결정 D-B](README.md#사용자-결정).
- [결정됨] D-H3: 옵션을 받는 새 오버로드는 기존 오버로드를 통해 같은 실행 경로로 위임한다. 근거: 사용자 2026-10-13 일괄 승인, [사용자 결정](README.md#사용자-결정), [IMP-01 호환성 사례:25](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-01/README.md#L25), [Lifecycle:100](../api/Lifecycle.md#종료-시간-제한).
- [결정됨] 검토했으나 채택하지 않은 대안: 새 이름의 API로 구분하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H3), [사용자 결정](README.md#사용자-결정).

## 옵션 객체와 default 검증: 형태를 계약에 맞게 선택

- [현행] 값형 선례는 `readonly struct SceneTransitionRequest`다: 생성자가 ID 배열을 복사하지만 실행을 시작하지 않고 manager가 실제 instance/settings/조건을 검증한다; `default(struct)`는 이 생성자를 통과하지 않으므로 생성자 기본값과 같다고 취급할 근거가 없다. [선언:9](../../Assets/TPLab/Core/SceneManagement/SceneTransitionRequest.cs#L9), [생성자/복사:29](../../Assets/TPLab/Core/SceneManagement/SceneTransitionRequest.cs#L29), [manager 검증:310](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L310).
- [현행] 참조형 선례 `RebindRequest`는 생성자의 필수 GUID와 mutable 옵션(default timeout10초)을 갖고, RebindAsync 진입 시 옵션을 복사하며 null/NaN/Infinity/음수 timeout을 거부한다. 이 선례는 모든 객체에 불변 class나 생성자 검증만을 사용한다는 규칙을 지지하지 않는다. [Request:34](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L34), [기본값:53](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L53), [진입 검증:109](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L109).
- [현행] root timeout 검증은 별도 ValidateShutdownTimeout을 Attach/Configure mutation 전에 호출하며 finite0이하는 무제한이다; RebindRequest의 음수 거부와 다른 계약이다. [Attach:45](../../Assets/TPLab/Core/Lifecycle/SceneRootSetup.cs#L45), [Configure:55](../../Assets/TPLab/Core/Lifecycle/SceneOwnedRoot.cs#L55), [검증:223](../../Assets/TPLab/Core/Lifecycle/RootInstallation.cs#L223), [IMP-03:53](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-03/README.md#L53).
- [결정됨] D-H2: 옵션은 단일 형태를 강제하지 않는다. 복사 가능한 작은 값 snapshot은 readonly struct, null 의미나 mutable authoring이 필요하면 class를 사용한다. **default 의미와 검증 위치는 옵션마다 XML에 명시한다.** 근거: 사용자 2026-10-13 일괄 승인, [사용자 결정](README.md#사용자-결정), [request 복사:41](../../Assets/TPLab/Core/SceneManagement/SceneTransitionRequest.cs#L41), [rebind 복사/검증:116](../../Assets/TPLab/Input/Runtime/InputRebindingController.cs#L116).
- [결정됨] 검토했으나 채택하지 않은 대안: 모든 옵션에 단일 형태를 일괄 강제하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H2), [사용자 결정](README.md#사용자-결정).

## snapshot과 호출자 버퍼

- [현행] IMP-05는 기존 `OwnedScenes`/`RegisteredScenes` 할당 snapshot 속성을 유지하고 `OwnedSceneCount`, `GetOwnedScenes(List<Scene>)`, `GetRegisteredScenes(List<SceneRegistration>)`를 추가했다; 매번 새 목록을 요구하는 진단과 반복 관찰의 버퍼 재사용을 구분한다. [기존 속성:69](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L69), [추가 API:78](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L78), [API 정리:350](../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot).
- [현행] 버퍼는 caller 소유이고 manager가 저장하지 않는다: null→thread 검증 후 Clear/fill, fill 실패는 부분 내용을 Clear하고 예외를 전파하며 thread 검증 실패는 기존 내용을 보존한다. 복사된 membership/registration 값은 다음 호출까지 유지되지만 Scene native 수명은 변할 수 있다; 소유권 이전이 아니다. [구현:104](../../Assets/TPLab/Core/SceneManagement/GameSceneManager.cs#L104), [계약:360](../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot), [수명:362](../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot).
- [현행] 충분한 capacity와 warm-up 조건에서1000회 workload0events가 과거 검증에 기록되어 있다; List 확장/프로젝트 getter 비용·CPU·다른 backend까지 무할당/성능 보장이 확장되지는 않는다. [측정 조건:364](../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot), [관찰 한계:368](../api/SceneManagement.md#imp-05-폴링과-호출자-버퍼-snapshot).

## Obsolete와 XML

- [현행] 위 개발 Runtime 범위의 Obsolete 선언은 BootstrapCallbacks 한 타입이며, warning형 속성과 ConfigureGameAsync→ConfigureSceneAsync 호환 adapter를 유지한다; 제거 일정이나 버전별 유예기간을 이 코드에서 확인할 수는 없다. [속성·adapter:9](../../Assets/TPLab/Core/SceneManagement/BootstrapCallbacks.cs#L9).
- [결정됨] D-H4: 공개 API를 제거하기 전에 대체 API·이행 예·소비 호출 확인·제거 버전 결정을 기록한다. 기존 호환 adapter(BootstrapCallbacks 등)는 제거 버전이 정해질 때까지 유지하고 임의로 제거 날짜를 부여하지 않는다. 근거: 사용자 2026-10-13 일괄 승인, [사용자 결정](README.md#사용자-결정), [보존된 호환 hook:17](../../Assets/TPLab/Core/SceneManagement/BootstrapCallbacks.cs#L17).
- [결정됨] 검토했으나 채택하지 않은 대안: 제거 버전 결정과 무관하게 기존 adapter를 계속 유지하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H4), [사용자 결정](README.md#사용자-결정).
- [현행] AGENTS는 공개 계약의 XML 책임/입출력/소유권/오류를 요구하고 DOCUMENTATION_GUIDE는 public 선언/XML을 구현·테스트와 대조하도록 한다; 실제 표현은 ObjectPool의 param/exception과 Request의 summary처럼 서로 다르므로 모든 공개 멤버에 같은 태그가 완비됐다고 주장하지 않는다. [AGENTS:19](../../AGENTS.md#c과-unity-기준), [문서 규칙:18](../DOCUMENTATION_GUIDE.md#공통-사실과-상태), [ObjectPool:45](../../Assets/TPLab/Core/Pooling/ObjectPool.cs#L45), [Request:29](../../Assets/TPLab/Core/SceneManagement/SceneTransitionRequest.cs#L29).
- [현행] public API·default·소유권·취소·오류가 바뀌면 XML과 관련 사람/AI 문서를 같은 단위에서 갱신한다는 문서 규칙이 있다; 이 초안은 그 규칙을 대체하지 않는다. [DOCUMENTATION_GUIDE:73](../DOCUMENTATION_GUIDE.md#갱신과-검증).
