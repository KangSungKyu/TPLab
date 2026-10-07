# TPLab

개발·참여하는 Unity 프로젝트에서 재사용할 공용 코어 시스템을 개발하고 검증한다.
GitHub 저장소는 `KangSungKyu/TPLab`이며, 로컬 Unity 프로젝트 폴더는 `MyLab`이다.

## 1차 목표

- ObjectPool
- Singleton
- ResourceManager
- DataTableManager
- GameSceneManager

일반 C# 클래스용 [ObjectPool<T>](doc/GENERIC_POOL.md), Unity [PrefabPool 어댑터](doc/OBJECT_POOL.md), 씬/영속 수명의 [MonoSingleton<T>](doc/SINGLETON.md)를 구현했다. 씬 루트의 소유 방식과 주입을 선택하는 [SceneRoot](doc/SCENE_ROOT.md)도 제공한다. 모든 시스템 준비 후 씬 진행과 가림막 callback을 기다리는 [비동기 씬 수명](doc/ASYNC_SCENE_LIFECYCLE.md)을 지원한다. [ResourceManager](doc/RESOURCE_MANAGER.md)는 Addressables 공유 로드와 소유자 종료를 관리하며 두 root 방식에 주입할 수 있다. [DataTableManager](doc/DATA_TABLE_MANAGER.md)는 프로젝트 CSV 스키마·교차 검증을 등록하고 전체 snapshot을 검증 후 공개한다. 표준 uint idx의 Get<T>/TryGet, 프로젝트 router·테이블 interface binding·FK 검증을 지원하며 임의 PK 수동 테이블도 함께 등록할 수 있다. [표준 사용법과 검증](doc/DATA_TABLE_GENERIC_IMPLEMENTATION.md)을 확인한다. [CSV Editor importer](doc/DATA_TABLE_IMPORTER_DRAFT.md)는 JSON 스키마와 setting.asset에 따라 DTO/table 생성과 공용·프로젝트 검증을 수행한다. 자동화는 기본 Disabled이며 Tools/MyLab/Data Tables에서 설정을 시작한다. [BootstrapSystem](doc/BOOTSTRAP_SYSTEM.md)은 root·목적지 설정, GameSceneManager에 위임하는 최초 Single/Additive 진입과 공용 root 영속 수명 선택, 공용 SceneTransitionCallbacks·기존 callback 호환과 Editor/Play/build 사전 검증을 제공한다. GameSceneManager의 Single/Additive 교체, 파생 씬 수명 tree, 정의 asset·root 조건·Editor 사전 검사를 구현했다. `MyLab > Scene Transitions > Open Additive / Open Single`에서 실행하는 프로젝트 소유 예제도 제공한다. [최종 사용자 확인](doc/SCENE_TRANSITION_ACCEPTANCE.md)은 2026-10-07 완료했고, main 통합·브랜치 정리는 [트랙 기록](doc/SCENE_TRANSITION_TRACK.md)을 따른다. 게임별 코드·데이터·UI를 코어에 포함하지 않는다.

## 개발 환경

| 항목 | 버전 |
|---|---|
| Unity | 6000.3.18f1 |
| Addressables | 2.9.1 |
| UniTask | 2.5.11 |
| CsvHelper | 33.1.0, netstandard2.1 DLL |
| Unity CLI / Connector | 0.4.1 |
| Unity Test Framework | 1.6.0 |

UniTask·Unity CLI Connector는 고정된 UPM Git 의존성으로 복원한다. CsvHelper DLL과 라이선스는 `Assets/Plugins/CsvHelper/`에 포함한다. CLI 실행 파일은 개발 환경에 별도 설치한다.

## 작업 기준

[AGENTS.md](AGENTS.md)의 C#·Unity 컨벤션, SOLID·TDD·검증 규칙을 따른다.
작업별 읽을 명세·검증·라이선스 자료는 [문서 색인](doc/INDEX.md)에서 선택한다.
시스템 경계, Cashier 참조와 의존성 검증 기록은 [CORE_PLAN.md](doc/CORE_PLAN.md)에 있다.

Unity Editor에서 프로젝트를 연 뒤 `unity-cli --project <프로젝트 절대 경로> status`로 연결을 확인한다.
`Library`, `Temp`, `Logs`, `UserSettings`와 IDE 생성 파일은 버전 관리에서 제외한다.
기능 구현 검증은 EditMode·PlayMode·Console·Player 빌드의 실행 범위를 구분해 기록한다.

현재 테스트는 `unity-cli --project <프로젝트 절대 경로> test --mode EditMode`와 `test --mode PlayMode`로 실행한다. 두 실행은 같은 Editor에서 순차적으로 수행한다.
