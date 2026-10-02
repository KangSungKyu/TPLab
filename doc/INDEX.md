# MyLab 문서 색인

## 읽는 순서

1. 모든 작업은 [AGENTS.md](../AGENTS.md)에서 역할·승인·보존·Git·검증 규칙을 확인한다. 코드 작업에는 같은 문서의 C#·Unity 기준, SOLID·TDD 기준을 함께 적용한다.
2. 아래 표에서 이번 작업과 연결되는 문서만 읽는다. 새 시스템 시작·의존성 변경·Cashier 참조 검토에는 [CORE_PLAN.md](CORE_PLAN.md)를 먼저 확인한다.
3. 기능 계약을 읽은 뒤 해당 코드·사용처·검증 입력을 현재 checkout에서 대조한다. 영향 범위가 늘어나면 관련 문서를 추가로 확인한다.
4. 완료 판단에는 아래 검증 자료를 사용한다. 기록의 날짜·대상 소스·실행 범위를 확인하고, 변경된 동작에 필요한 검증을 실제 실행한다.

이 색인은 모든 문서를 매번 읽으라는 지시가 아니다. 기능 계약은 해당 명세 한 곳에서 갱신하며 색인에 본문을 복제하지 않는다. 문서와 구현이 충돌하면 차이와 영향을 확인한다. 과거 통과 기록을 현재 checkout의 완료 증거로 대신하지 않는다.

## 문서와 읽기 조건

코드·기능 기준: 2026-10-02, `main 059e6a4`의 ResourceManager 단계까지. 저장소 이름은 TPLab, 로컬 Unity 프로젝트 이름은 MyLab이다. 아래에는 실제 존재하는 문서만 연결한다.

| 문서 | 읽는 조건 | 내용 |
|---|---|---|
| [AGENTS.md](../AGENTS.md) | 모든 작업에서 필수 | 공용 코어 역할·C#·SOLID·TDD·참조 경계·브랜치 판단·검증 후 병합 |
| [README.md](../README.md) | 프로젝트 진입·환경 복원 | 목표·구현 기능·설치 의존성·검증 명령 |
| [INDEX.md](INDEX.md) | 작업 시작·범위 변경 | 작업별 명세와 검증 자료 선택 |
| [CORE_PLAN.md](CORE_PLAN.md) | 새 단계·설계·패키지·Cashier 참조 검토 | 공용 코어 범위·채택 판단·의존성·개발 순서·단계 진행 |
| [GENERIC_POOL.md](GENERIC_POOL.md) | 일반 C# pooling·소유권·정원·반환·종료 변경 | ObjectPool<T> 계약, Unity 기본 풀 검토와 제네릭 전환 근거 |
| [OBJECT_POOL.md](OBJECT_POOL.md) | GameObject prefab pooling·활성화·Transform·파괴 변경 | PrefabPool 어댑터 계약과 최초 구현 기록; 공통 풀은 GENERIC_POOL 참조 |
| [SINGLETON.md](SINGLETON.md) | 전역 접근·중복 객체·씬/영속 수명·반복 Play 변경 | MonoSingleton 계약·초기화/정리·Domain/Scene Reload 검증 |
| [SCENE_ROOT.md](SCENE_ROOT.md) | InitScene 같은 root의 소유 방식·installer·참조 주입·Editor 설정 | SceneOwned/Singleton 선택, Inspector·script 연결과 동기 주입 |
| [ASYNC_SCENE_LIFECYCLE.md](ASYNC_SCENE_LIFECYCLE.md) | async 준비·해제·취소·씬 진행·가림막 callback 변경 | 준비/종료 순서·실패 rollback·표시 보호·전환 소유권 |
| [RESOURCE_MANAGER.md](RESOURCE_MANAGER.md) | Addressables 로드·캐시·타입·취소·handle·pool 자산 수명 변경 | ResourceManager 소유권·종료·root 주입과 소비자 준비/해제 |
| [ResourceManager 검증](validation/resource-manager/README.md) | ResourceManager 검증 계획·결과 해석·회귀 확인 | 실제 Red/Green·native fixture·컴파일/Console·증거·미검증 경계 |

## 검증 자료

계약 문서가 검증 결과의 설명을 소유한다. 아래는 실행 자료를 찾는 입구이며, 테스트 수를 현재 통과 상태로 고정하는 표가 아니다. `green` 파일도 생성 당시 소스에 대한 증거다.

| 자료 | 읽는 조건 | 해석 기준 |
|---|---|---|
| [최초 prefab pool 실행](validation/object-pool/green-play.json) | Unity 기본 풀을 사용했던 최초 단계 조사 | 과거 구현 증거; 현재 제네릭 풀 계약과 구분 |
| [제네릭 pool 실행](validation/generic-pool/green-edit.json) · [Unity 기본 풀 재현](validation/generic-pool/unity-contract.json) · [.NET 검사 코드](validation/generic-pool/dotnet-probe/Program.cs) | ObjectPool<T> 전환·Unity API 차이·순수 C# 재사용 확인 | GENERIC_POOL의 검증 절과 입력 hash 대조; .NET 검사는 Unity 소비 프로젝트/Player 증거가 아님 |
| [Singleton 실행 기록](validation/singleton/execution.json) · [반복 Play](validation/singleton/reload-check.json) | static·씬 수명·Reload 옵션 확인 | SINGLETON의 검증 절과 대상 입력 대조 |
| [SceneRoot 실행 기록](validation/scene-root/execution.json) · [반복 Play](validation/scene-root/reload-check.json) | root 선택·Editor 메뉴·주입/정리 확인 | SCENE_ROOT의 검증 절과 대상 입력 대조 |
| [비동기 씬 수명 실행 기록](validation/async-scene/execution.json) · [반복 Play](validation/async-scene/reload-check.json) | 준비·해제·가림막 callback·취소 확인 | ASYNC_SCENE_LIFECYCLE의 검증 절 참조; 실제 가림막 시각 UX와 구분 |
| [ResourceManager 검증 설명](validation/resource-manager/README.md) · [EditMode](validation/resource-manager/full-EditMode.json) · [PlayMode](validation/resource-manager/full-PlayMode.json) · [입력 hash](validation/resource-manager/test-inputs.json) | 자산 수명·root/pool 연결·전체 회귀 확인 | native catalog/provider 실행 범위와 원격 bundle·Player 미검증 경계 확인 |

## 의존성과 라이선스 자료

패키지 추가·교체·외부 코드 채택 검토 시 CORE_PLAN의 판단과 실제 설치 파일을 함께 확인한다.

| 자료 | 용도 |
|---|---|
| [manifest.json](../Packages/manifest.json) · [packages-lock.json](../Packages/packages-lock.json) | 현재 UPM 의존성과 고정 버전·commit 확인 |
| [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt) | Unity 버전 확인; 다른 버전 호환성 증거와 구분 |
| [UniTask 라이선스](licenses/UniTask-LICENSE.txt) | 승인된 UniTask의 출처·배포 조건 |
| [Unity CLI Connector 라이선스](licenses/UnityCli-LICENSE.txt) | 개발·검증 도구의 출처·배포 조건 |
| [CsvHelper 라이선스](../Assets/Plugins/CsvHelper/LICENSE.txt) | 포함 DLL의 배포 조건; 설치 버전·hash는 CORE_PLAN 참조 |

## 다음 단계와 완료 경계

- DataTableManager와 GameSceneManager는 구현 전 단계다. 별도 확정 계약 문서를 만들기 전에는 [CORE_PLAN의 단계 진행](CORE_PLAN.md#단계-진행)을 기준으로 요구사항·완료 조건부터 정한다.
- 소비 프로젝트 가져오기·최소 예제 실행·Player 검증은 후속 단계다. MyLab 내부 테스트의 통과를 전체 배포 호환성 완료로 확대하지 않는다.
- Cashier는 읽기 전용 참조다. 이 색인은 Cashier `doc/INDEX.md`의 조건별 문서 선택·단일 본문·과거 증거 구분을 개선 후 적용했다. Cashier의 게임별 규칙·팀 분업·통합 승인 절차는 MyLab에 적용하지 않는다.

## 입구와 유지 규칙

- 작업 규칙 입구는 [AGENTS.md](../AGENTS.md), 프로젝트 안내 입구는 [README.md](../README.md)다. 두 입구에서 이 색인을 통해 관련 명세를 선택한다. 다른 도구의 전용 입구나 자동 규칙 적용은 이번 작업에서 설정·검증하지 않았다.
- 기능 문서 생성·이동·폐기 시 해당 읽기 조건과 링크를 이 색인에서 갱신한다. 상세 API·규칙·실행 결과의 본문은 원래 문서에 둔다.
- 문서 이동이 발생하면 이전 경로와 새 단일 본문의 대응을 기록한다. 이번 적용은 기존 문서를 이동하거나 규칙 본문을 분리하지 않았다.
- 검증 자료 추가 시 기능 문서에서 그 실행의 대상·시점·범위·한계를 설명한다. 색인은 필요한 자료의 위치만 연결한다.
- 문서만 변경할 때는 상대 링크·문서 등록 누락·diff/공백·변경 범위를 확인한다. runtime·자산·설정이 바뀌지 않았으면 Unity 테스트 재실행 대상이 없다고 기록하며 미실행을 통과로 세지 않는다.
