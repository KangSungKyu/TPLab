# TPLab

개발·참여하는 Unity 프로젝트에서 재사용할 공용 코어 시스템을 개발하고 검증한다.
GitHub 저장소는 `KangSungKyu/TPLab`이며, 로컬 Unity 프로젝트 폴더는 `MyLab`이다.

## 1차 목표

- ObjectPool
- Singleton
- ResourceManager
- DataTableManager
- GameSceneManager

첫 단계인 로컬 [PrefabPool](doc/OBJECT_POOL.md)을 구현했다. 나머지 시스템은 순서대로 계약·테스트·구현을 진행한다. 게임별 코드·데이터·UI를 코어에 포함하지 않는다.

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
시스템 경계, Cashier 참조와 의존성 검증 기록은 [CORE_PLAN.md](doc/CORE_PLAN.md)에 있다.

Unity Editor에서 프로젝트를 연 뒤 `unity-cli --project <프로젝트 절대 경로> status`로 연결을 확인한다.
`Library`, `Temp`, `Logs`, `UserSettings`와 IDE 생성 파일은 버전 관리에서 제외한다.
기능 구현 검증은 EditMode·PlayMode·Console·Player 빌드의 실행 범위를 구분해 기록한다.

현재 테스트는 `unity-cli --project <프로젝트 절대 경로> test --mode EditMode`와 `test --mode PlayMode`로 실행한다. 두 실행은 같은 Editor에서 순차적으로 수행한다.
