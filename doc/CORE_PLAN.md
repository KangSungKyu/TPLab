# 공용 코어 1차 검토안

작성일: 2026-10-02. 사용자 지정 목표·작업 원칙은 확정이다. ObjectPool은 [제네릭 계약](GENERIC_POOL.md)과 [프리팹 어댑터 계약](OBJECT_POOL.md), Singleton은 [수명 계약](SINGLETON.md), 씬 루트의 소유 방식·참조 주입은 [SceneRoot 계약](SCENE_ROOT.md)으로 확정했다. 나머지 시스템의 구현·이식 방향은 검토 제안이다.

## 초기 확인 기준 (의존성 반영 전)

- MyLab: Unity 6000.3.18f1, Addressables 2.9.1, Test Framework 1.6.0, URP 17.3.0. Assets에 자체 C#·asmdef·DLL은 아직 없다. Git 저장소가 초기화되어 있지 않다.
- Cashier: 같은 Unity·Addressables·Test Framework 버전. 읽은 checkout은 total_merge, HEAD 8b093946a5ffa85864bea734ce6eeab6ccea41a0다. 폰트·URP·ProjectSettings 수정과 ProfilerCaptures 미추적 폴더가 존재한다. 읽기 전용으로 보존했다.
- 현재 환경의 버전 확인은 다른 Unity 버전·플랫폼 지원 증거가 아니다. 최소 지원 Unity 버전, 대상 플랫폼·IL2CPP/WebGL 지원은 구현 계약을 정할 때 확인한다.

근거: 각 프로젝트의 Packages/manifest.json, ProjectSettings/ProjectVersion.txt 및 Cashier의 현재 코드·Git 상태. 과거 테스트 결과를 이번 검증 결과로 사용하지 않았다.

## 시스템별 검토

| 시스템 | Cashier 참조 | 공용 코어 방향 제안 | 첫 완료 조건 |
|---|---|---|---|
| ObjectPool | SimplePool.cs, SimplePoolManager.cs, RESOURCE_POOL_CONTRACT.md | 일반 C# 클래스용 ObjectPool<T>가 정원·참조 소유권·실패 정리를 관리하고 PrefabPool은 Unity 객체 수명 어댑터로 유지. Unity 기본 API의 경계 동작을 실행 검증한 뒤 System 컬렉션으로 전환 | 정상 재사용, 소진, 중복·비소유 반환, hook 실패, 대여 중 종료 확인 |
| Singleton | Singleton.cs | 중복 인스턴스 처리와 정리 hook을 참고. 전역 접근을 제한하고 영속/씬 수명, 초기화 실패, 반복 Play의 static 초기화 계약 보완 | 중복 생성, 소유자 파괴, 재생성, Domain Reload 설정에 따른 반복 실행 확인 |
| ResourceManager | ResourceManager.cs, RESOURCE_POOL_CONTRACT.md | 진행 중 로드 공유, 요청 타입 검증, 호출자 대기 취소와 소유자 종료 구분, 늦은 결과 해제를 참고. Datas label·catalog 갱신·atlas 자동 구독은 게임 정책으로 분리 | 동시 요청, 개별 취소, 실패 후 재요청, 로드 중 해제, 객체 생성·해제 확인 |
| DataTableManager | DataTableManager.cs, Util.cs, 각 DataTable | 등록·읽기·검증·공개 책임을 분리. 검증 후 공개 원칙을 참고하고 게임별 테이블·FK 규칙은 소비 프로젝트가 제공 | 중복 키, 필수값·변환 오류, 참조 검증 실패 시 기존 정상 데이터 보존 확인 |
| GameSceneManager | GameSceneManager.cs, InitScene.cs | 재진입 차단과 준비→로드→활성화 흐름을 참고. SceneName, 엔딩, 세션 재시작, 가게·손님 준비, 로딩 UI와 개인 씬 설정은 분리 | 전환 성공, 중복 요청, 잘못된 목적지, 준비 실패, 취소·파괴 후 정리 확인 |

### 그대로 복사하기 전에 해결할 차이

- Cashier SimplePool.capacity는 대여 중 객체를 포함한 총 소유 정원이다. Unity 기본 풀의 maxSize는 반환 시 보관 한도이며 빈 풀의 Get은 새 객체를 생성한다. Clear·중복 반환의 의미도 다르므로 이름만 맞춰 대체하지 않는다. 대여 중 전체 폐기·영구 종료가 필요하면 그 정책을 별도로 유지한다. [Unity ObjectPool](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html)
- Cashier Singleton은 Awake에서 Instance를 등록하고 DontDestroyOnLoad를 적용하며 OnDestroy에서 정리한다. 공용판에서는 반복 Play·초기화 예외·중복 객체의 파괴 범위를 명시하고 검증한다. 일반적인 static 초기화 안내를 generic 기반 클래스에 그대로 붙여 동작을 보장했다고 판단하지 않는다. [Domain Reload](https://docs.unity3d.com/6000.3/Documentation/Manual/domain-reloading.html)
- ResourceManager의 공유 자산은 manager가 Addressables 참조를 소유한다. 개별 소비자가 Release(key)로 다른 소비자의 자산을 해제하지 않게 소유 범위를 API에 드러낸다. 소비자별 lease/reference count 도입 여부는 실제 사용 사례에 맞춰 결정한다. [Addressables 메모리 관리](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/MemoryManagement.html)
- Cashier DataTableManager는 구체 테이블과 검증을 직접 열거하고 Util은 idx / 1000으로 테이블을 구분한다. 공용판에는 명시적 테이블 등록과 소비 프로젝트의 스키마·검증을 전달한다. Cashier에서 확인한 PendingRows/Validate/Commit 흐름을 전체 로드의 원자성 보장으로 확대하지 않는다.
- Scene 전환의 취소 요청과 네이티브 로드의 중단 가능성은 구분한다. 취소 허용 시점, 취소 이후 활성화·해제, 이전 씬 보존이 가능한 구간을 먼저 정한다.

## 패키지·plugin 판단

| 항목 | 현재 근거 | 제안 |
|---|---|---|
| Addressables 2.9.1 | 두 프로젝트 manifest에 존재 | 설치된 버전 활용. 의존 경계와 handle 소유권을 명시 |
| Test Framework 1.6.0 | 두 프로젝트 manifest에 존재 | 기존 NUnit/EditMode/PlayMode로 TDD 수행 |
| UniTask 2.5.11 | 공식 UPM Git package, tag 2.5.11, MIT | 사용자 채택 확정. 비동기 기본 도구로 사용 |
| CsvHelper 33.1.0 | 공식 NuGet의 netstandard2.1 DLL, MS-PL OR Apache-2.0 | 사용자 채택 확정. CSV 기본 도구로 사용 |
| DOTween | Cashier Assets/Plugins/Demigiant/DOTween | 1차 코어의 필수 의존성으로 넣을 근거가 현재 없음. 연출이 필요한 소비 프로젝트에서 사용 |
| Unity CLI/Connector 0.4.1 | 기존 CLI 실행 파일과 공식 connector 고정 커밋 | 사용자 채택 확정. Editor 개발·검증 도구로 사용 |
| 2D Animation/Aseprite/PSD/SpriteShape/Tilemap 도구 | Cashier manifest의 MyLab 대비 추가 항목 | 다섯 공용 시스템에 필요한 근거가 생길 때 검토 |

비동기 기본 도구는 UniTask로 확정했다. 개별 UniTask를 임의로 여러 번 await하지 않고, 공유 완료에는 동시 대기가 가능한 완료 소스를 사용한다. 플랫폼·취소·메인 스레드 계약은 기능별로 검증한다. [UniTask 공식 안내](https://github.com/Cysharp/UniTask/tree/2.5.11)

## 개발 순서와 경계

1. 각 기능의 입력·결과·소유자·수명·오류 계약과 최소 테스트를 먼저 정한다. ObjectPool은 로컬 prefab 경로부터 시작할 후보이며 Singleton을 선행 의존성으로 강제하지 않는다.
2. Singleton과 로컬 ObjectPool을 각각 검증한다. 전역 manager 생성·검색을 서비스 조회에 숨기지 않는다.
3. ResourceManager의 Addressables 경로를 검증하고 필요할 때 pool의 자산 준비 경로를 연결한다.
4. DataTableManager의 등록·검증·공개를 검증한다. 게임별 DTO·CSV ID·FK 규칙은 공용 runtime 밖에 둔다.
5. GameSceneManager에 명시적인 사전 준비 작업을 연결하고 실제 씬 수명을 검증한다. 엔딩·게임 세션·로딩 화면은 소비 프로젝트가 소유한다.
6. 별도 소비 프로젝트에 가져와 컴파일·최소 사용 예제를 실행한다. Cashier에 적용하는 변경은 별도 요청 범위에서 진행한다.

첫 로컬 구현은 Assets/MyLab/Core와 .meta를 함께 가져오는 방식, MyLab.Core assembly와 MyLab.Core.Pooling namespace, Unity 6000.3 검증 기준을 사용한다. EditMode·PlayMode assembly를 runtime과 분리한다. 여러 프로젝트 배포의 UPM package ID·버전 호환 정책은 소비 프로젝트 검증 단계에서 결정한다. URP·Input System·uGUI·DOTween 등 특정 프로젝트 도구를 코어의 필수 의존성으로 확장하지 않는다.

## 세션 역할 보완

이 세션이 아키텍처·공용 API·수명 계약과 구현 리뷰를 맡는다. 추가 세션을 미리 만들 필요는 없다. 구현 규모가 커지면 확정 계약의 구현 담당과 독립 검증 담당을 분리한다.

보완할 책임은 API 호환성·패키지 의존성 관리와 검증 증거 관리다. 매 단계에서 설계 검토, 실제 diff 리뷰, 컴파일, 제품 Console, 테스트 실행, 소비 프로젝트 통합, Player 빌드의 확인 범위를 구분한다.

## 초기 역할 설정의 검증 범위

- 프로젝트·패키지·관련 코드를 읽고 역할·검토 기준을 문서화했다. 이식·개선안 채택은 미확정이다.
- Cashier ResourcePoolTests의 공유 로드·취소·해제·실패/retry·pool 소유권 검사를 참고했다. 이번 Unity 테스트 실행은 0건이다.
- C# 구현, Unity 조작·컴파일·제품 Console 확인, plugin 복사, 패키지·프로젝트 설정 변경 및 Git 변경 작업은 수행하지 않았다.

## 승인 의존성 반영 (2026-10-02)

- UniTask: Packages/manifest.json의 공식 UPM URL에 #2.5.11을 지정했다. lock의 commit은 2e993ff18f28c931602a07292df0b0804eebef99다. [출처·설치 안내](https://github.com/Cysharp/UniTask/tree/2.5.11), [MIT 원문](licenses/UniTask-LICENSE.txt).
- Unity CLI Connector: Packages/manifest.json의 공식 UPM URL에 Cashier와 같은 commit 07c62fd29f1e6d29d8f9a504b968bedbbd47ddc8을 지정했다. 커넥터와 설치된 CLI 실행 파일은 모두 0.4.1이다. [출처](https://github.com/youngwoocho02/unity-cli), [MIT 원문](licenses/UnityCli-LICENSE.txt).
- CsvHelper: [공식 NuGet 33.1.0](https://www.nuget.org/packages/CsvHelper/33.1.0)의 lib/netstandard2.1/CsvHelper.dll을 Assets/Plugins/CsvHelper/에 배치했다. NuGet의 명시된 의존성 Microsoft.CSharp은 추가 DLL 없이 현재 Unity Editor에서 CSV 실행에 필요한 의존성을 충족했다. 추가 NuGet 관리 plugin은 설치하지 않았다. 다른 플랫폼 빌드 호환성은 미검증이다.
- CsvHelper DLL의 SHA-256은 20101C398654A14BFD42BD78D7281F43197D19B7B3CF41C7AAE93F1EABA65A61이며 Cashier DLL과 일치한다. 기존 DLL GUID 0737863bb2f229c40911f9a88cc5b5aa를 보존했다. [라이선스 원문](../Assets/Plugins/CsvHelper/LICENSE.txt)을 함께 배치했다.
- 두 UPM package의 resolve·lock 반영, Unity .meta 짝, Editor 컴파일 완료를 확인했다. 정확한 MyLab Editor PID 42616, Unity 6000.3.18f1, Connector 0.4.1에서 CLI 상태 ready와 Console 오류 0건을 확인했다.
- Editor CLI smoke 실행 성공 1건: CSV 따옴표 내부 쉼표·줄바꿈 및 UniTask 완료 결과 확인 3개를 통과했다. CsvHelper 패키지는 33.1.0이며 런타임 assembly version은 33.0.0.0이다. 비동기 PlayerLoop·취소·PlayMode·Player 빌드 및 NUnit 테스트는 미실행이다. TDD Red/Green 증거로 사용하지 않는다.
- 재실행 코드: Temp/DependencySetup/smoke.cs. PowerShell에서 내용을 -Raw로 읽어 변수에 담고 `unity-cli --project C:\Users\PC\Projects\MyLab exec $smokeSource --allow-async`로 실행한다. --allow-async는 완료된 UniTask 값의 검사에 필요하며 백그라운드 작업을 예약하지 않는다.
- 증거: Temp/DependencySetup/dependency-smoke.txt, status-after.txt, console-errors.json. 기본 stdin 전달에서는 CLI가 instance를 찾지 못했고 일반 exec에서는 UniTask 키워드 정책으로 실행 전에 거부되어, project 옵션을 명시한 직접 인수와 --allow-async로 확인했다.
- 코어 기능 구현·Git 초기화·저장소 생성·commit·push는 수행하지 않았다. Cashier의 초기 dirty 파일 목록은 유지됐다.

## 저장소 설정 (2026-10-02)

- 사용자 승인: GitHub KangSungKyu/TPLab, private, 저장소 생성·Git 관리 허용.
- 로컬 프로젝트 경로 C:\Users\PC\Projects\MyLab은 유지한다. 기본 브랜치는 main이며, Unity 원본 Assets·Packages·ProjectSettings와 문서·외부 라이선스를 초기 커밋에 포함한다.
- .gitignore로 Unity·IDE 생성물과 로컬 인증 파일을 제외한다. CsvHelper.dll은 버전 관리에 포함하고 Unity .meta를 보존한다. .gitattributes는 텍스트 줄바꿈과 DLL binary 취급을 지정한다.
- Git author는 현재 설정된 KangSungKyu를 사용하며, 기존 Git 인증의 GitHub 계정도 KangSungKyu로 확인했다. [TPLab](https://github.com/KangSungKyu/TPLab)의 private 생성과 push/admin 권한을 확인했다. 원격 origin은 https://github.com/KangSungKyu/TPLab.git을 사용한다.
- 초기 커밋 대상은 60개 파일이다. 생성물 제외·자산과 .meta 짝·GUID 중복을 검사했다. 새 문서·설정의 cached whitespace 검사는 통과했고, 전체 초기 diff에는 Unity 직렬화 원본 32개 파일의 기존 후행 공백 경고가 있다. YAML·meta의 내용을 정리하지 않고 원본을 보존했다.

## 단계 진행

1. ObjectPool: 일반 C# 클래스용 ObjectPool<T>와 PrefabPool 어댑터의 구현·자동 검증 완료. Unity 기본 보관 기능을 제거하고 System 컬렉션으로 전환했다. Cashier 코드·의존성은 복사하지 않았다. 검증 범위와 증거는 [GENERIC_POOL.md](GENERIC_POOL.md)를 따른다.
2. Singleton: MonoSingleton<T>의 중복 컴포넌트 제거, 씬/영속 수명, 초기화·정리 실패 및 반복 Play 자동 검증 완료. 모든 manager의 필수 기반으로 확장하지 않는다. 검증 범위와 증거는 [SINGLETON.md](SINGLETON.md)를 따른다.
3. SceneRoot: InitScene 같은 기존 씬 root에 SceneOwned/Singleton host를 선택하고 프로젝트 installer를 연결한다. Editor 메뉴·Inspector와 스크립트 Attach/Configure를 제공한다. 동기 주입과 비동기 서비스 준비는 구분한다. 모든 installer 준비 뒤 씬을 진행하고 가림막의 표시·해제 완료를 callback으로 기다리는 [비동기 계약](ASYNC_SCENE_LIFECYCLE.md)을 제공한다. 검증 범위는 [SCENE_ROOT.md](SCENE_ROOT.md)를 따른다.
4. ResourceManager: Addressables 공유 로드·정확한 타입 검증·개별 대기 취소·실패 재요청·소유자 종료와 늦은 결과 해제를 구현했다. ResourceManagerInstaller를 양쪽 root host에 연결하며 소비자 installer가 필수 자산 준비와 prefab pool 정리를 기다린다. [계약·범위](RESOURCE_MANAGER.md)와 [현재 검증](validation/resource-manager/README.md)을 따른다.
5. DataTableManager: 2026-10-06 표준 uint idx DTO·프로젝트 router·Get/TryGet·테이블 binding·FK를 추가했다. 임의 PK 수동 경로는 유지한다. [표준 계약](DATA_TABLE_GENERIC_IMPLEMENTATION.md)과 [현재 검증](validation/generic-data-tables/README.md)을 따른다. 명시적 테이블·CSV 스키마·키·행 검증·교차 검증 등록과 전체 snapshot의 검증 후 공개를 구현했다. 공유 비동기 로드·개별 대기 취소·재로드 실패 시 이전 데이터 보존·소유자 종료를 확인하고 ResourceManager와 SceneRoot 준비/해제 흐름을 연결했다. [계약](DATA_TABLE_MANAGER.md)과 [현재 검증](validation/data-tables/README.md)을 따른다.
Text/Resource 구체 DTO·테이블은 [예시 템플릿](templates/data-tables/README.md)으로 분리했다. 코어는 최소 공용 기반만 제공하고 스키마는 사용 프로젝트가 소유한다. [현재 분리 검증](validation/data-table-templates/README.md)을 확인한다.

6. GameSceneManager 후속 작업은 [기본 사양·Bootstrap 권장안](GAME_SCENE_MANAGER_DRAFT.md)과 [Phase 통합 track](SCENE_TRANSITION_TRACK.md)을 따른다. Bootstrap을 첫 씬으로 실행하고 공용 준비 후 게임 씬을 Additive로 로드하며 Bootstrap은 앱 수명 동안 유지하는 구조를 권장한다. [BootstrapSystem](BOOTSTRAP_SYSTEM.md)의 최초 진입·설정·사전 검사는 구현했다. Phase 1 callback 공용화와 Phase 2 GameSceneManager 최초 Single/Additive 진입·Bootstrap 위임을 구현했다. 연속 교체·구역·조건 runtime은 후속 단계이며, 소비 프로젝트 가져오기·Player 검증을 포함한 최종 gate까지 내부 단계의 통과를 전체 코어 배포 호환성으로 확대하지 않는다.

2026-10-06 Editor importer 구현: [현재 계약](DATA_TABLE_IMPORTER_DRAFT.md)의 CSV + 명시적 JSON 스키마, setting.asset 3모드, 생성/기존 타입 검증, 공용 CSV/idx 사전검사와 컴파일 후 typed 전체 검증을 제공한다. runtime에 구체 테이블·자동 등록을 추가하지 않았다. [이번 검증](validation/data-table-importer/README.md)을 확인한다. JSON 행 로더·rename migration·소비 프로젝트/Player 검증은 후속 범위다. 다음 시스템은 GameSceneManager다.

2026-10-06 Bootstrap 구현: 기존 root/SceneRootFlow를 재사용해 root·목적지 설정, 최초 Additive 로드/active/preparation, callback, 늦은 완료 정리와 graceful 게임 종료를 제공한다. Inspector·컴파일 후 Editor·Play 진입·실제 build scene 목록 검사를 연결했다. [계약](BOOTSTRAP_SYSTEM.md)과 [검증](validation/bootstrap-system/README.md)을 확인한다. GameSceneManager 일반 전환·성공 Player/소비 프로젝트 실행은 후속 범위다.

2026-10-06 Phase 1: SceneTransitionCallbacks/ConfigureSceneAsync로 최초 진입 callback을 공용화하고 기존 BootstrapCallbacks를 호환 어댑터로 유지한다. Single/Additive·수명 tree·주/파생 구역·root 조건·UI·취소/실패 계약과 단계별 완료 기준은 GAME_SCENE_MANAGER_DRAFT.md가 소유한다. [이번 검증](validation/scene-transition-contracts/README.md)을 확인한다.

2026-10-06 Phase 2: GameSceneManager 최초 진입 소유권, 공용 영속 수명/첫 로드 모드, 공유 await/owner 취소, 실패 단계·잔여 씬 상태, 명시적 종료와 기존 Editor gate 연결을 제공한다. [계약](BOOTSTRAP_SYSTEM.md)과 [검증](validation/game-scene-entry/README.md)을 확인한다.

2026-10-06 P0 loaders: 명시적 BuildScene/Addressable SceneTarget, 독점 LoadedScene과 두 loader를 제공하고 기존 최초 진입·Bootstrap Inspector 및 사전 gate에 연결했다. ResourceManager 자산 cache에는 씬 handle을 공유하지 않는다. [계약](SCENE_LOADING.md)과 [검증](validation/scene-loaders/README.md)을 확인한다. 다음은 Phase 3A 주 흐름 교체다.

2026-10-06 Phase 3A: Additive/Single primary 교체, 작업별 취소·공유 완료와 실제 잔여 소유 씬을 제공한다. [검증](validation/scene-replacement/README.md)을 확인한다. 다음은 Phase 3B 파생 구역 수명 tree다.

2026-10-06 Phase 3B: 실제 Scene 부모/자식 등록, 구역 추가/자기·ancestor 제거, active 선택과 자식 우선 정리·공유 제거를 제공한다. [검증](validation/scene-areas/README.md)을 확인한다. 다음은 Phase 4 정의 asset과 root 조건이다.

2026-10-06 Phase 4: 설정 정의 snapshot, ID/직접 요청과 편의 API의 공통 root 조건 정책, 해제 전 및 first/add reveal 전후 재검사를 제공한다. [검증](validation/scene-definitions/README.md)을 확인한다. 다음은 Phase 5 Inspector와 정의·조건 사전 검사다.
