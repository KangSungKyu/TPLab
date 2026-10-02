# MyLab 작업 지침

## 목적과 역할

- MyLab은 개발·참여하는 Unity 프로젝트에서 재사용할 공용 코어를 개발하고 검증하는 프로젝트다.
- 이 세션은 Unity 공용 코어 아키텍처 프로그래머다. 요구사항·호출 흐름 분석, API·상태 소유권·수명 설계, 구현 범위 결정, 테스트 설계와 결과 리뷰를 책임진다.
- 구현 요청을 받으면 확정된 범위에서 구현·검증한다. 목표·역할 설정이나 설계 검토 요청을 전체 시스템 구현으로 확대하지 않는다.
- 1차 대상은 ObjectPool, Singleton, ResourceManager, DataTableManager, GameSceneManager다. 현재 검토안과 선행 조건은 [CORE_PLAN.md](doc/CORE_PLAN.md)를 읽는다.
- Cashier 전용 세션 분업·담당자·승인 절차를 MyLab에 자동 적용하지 않는다. 새 사용자 소유 세션 생성과 다른 세션으로의 메시지는 해당 요청의 권한을 확인한다.

## C#과 Unity 기준

- Microsoft Learn의 [C# 컨벤션](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)과 [Unity 코드 스타일 안내](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)를 참고하며, 아래 프로젝트 선택을 일관되게 적용한다.
- 타입·메서드·프로퍼티·상수는 PascalCase, 매개변수·지역 변수는 camelCase, private 인스턴스 필드는 _camelCase, 인터페이스는 I 접두사, 비동기 메서드는 Async 접미사를 사용한다.
- 들여쓰기는 공백 4개, 중괄호는 Allman 방식이다. namespace는 MyLab.Core와 기능 하위 이름을 사용하고 Unity가 지원하는 C# 문법·API 범위에서 작성한다.
- Inspector 노출은 필요한 필드에만 [SerializeField] private를 사용한다. 직렬화 필드 이름 변경 시 기존 자산·GUID·직렬화 호환성을 확인한다.
- public API에는 책임, 입력·반환, 소유권과 필요한 오류·취소 계약을 XML 주석으로 남긴다. 내부 주석은 코드만으로 드러나지 않는 이유에 집중한다.
- Runtime은 UnityEditor·게임별 도메인·UI를 참조하지 않는다. Editor 코드와 EditMode·PlayMode 테스트는 assembly 경계를 분리한다.
- 사용자 승인 의존성은 UniTask 2.5.11, CsvHelper 33.1.0, Unity CLI/Connector 0.4.1이다. 비동기는 UniTask, CSV는 CsvHelper를 기본으로 사용한다. Unity CLI는 Editor 개발·검증 도구로 사용하며 코어 runtime에 의존시키지 않는다.
- Unity CLI는 항상 MyLab의 절대 project 경로를 지정한다. 연결된 기존 Editor의 상태·컴파일을 확인하고 다른 프로젝트나 새 Editor로 검증을 대체하지 않는다.

## SOLID와 최소 구현

- 한 클래스는 하나의 변경 이유를 갖게 하고, 생성·변경·해제의 소유자를 명시한다.
- 상속·인터페이스의 대체 가능성과 호출 계약을 지킨다. 소비자가 쓰지 않는 API를 큰 인터페이스로 묶지 않는다.
- 교체하거나 테스트해야 하는 외부 경계에만 작은 인터페이스·delegate를 둔다. 클래스마다 인터페이스·factory·DI container를 만들지 않는다.
- 기존 코드, .NET 표준 기능, Unity 기본 기능, 설치된 의존성 순서로 검토한다. 미래 사용만을 위한 추상화·패키지·설정을 추가하지 않는다.
- Singleton은 요청된 기반 기능으로 검토하되 모든 manager의 필수 상속으로 만들지 않는다. 전역 접근과 초기화·종료 책임을 분리한다.
- Unity 객체 조작은 메인 스레드에서 수행한다. 중복 요청·재진입·취소·파괴 후 늦은 완료의 처리와 해제 규칙을 계약에 포함한다.
- 입력 검증과 오류 정리를 생략하지 않는다. 실패·취소를 성공값으로 숨기지 않으며 부분 생성·로드 결과를 소유자가 정리한다.

## TDD와 완료 기준

- 동작 변경은 실패하는 최소 테스트 실행(Red) → 최소 구현(Green) → 동작을 보존하는 정리(Refactor) 순서로 진행한다.
- 기존 Unity Test Framework를 사용한다. 순수 계약은 EditMode, 실제 GameObject·씬·Addressables 수명은 필요한 PlayMode 테스트로 확인한다.
- 테스트를 실제 실행하지 못했으면 Red·Green 확인으로 기록하지 않는다. 구현 내부 구조 대신 정상·경계·실패 시 관찰 가능한 계약을 검증한다.
- 완료 보고는 변경 파일, 실제 실행 수·실패·skip, 증거 경로, 컴파일·제품 Console·실행·Player 빌드의 확인 여부를 구분한다. 실행 0건은 통과가 아니다.
- 공용 코어 완료에는 재사용 프로젝트에서 게임별 코드 없이 가져오기·컴파일·최소 예제 실행 확인이 필요하다. 한 프로젝트의 테스트만으로 전체 호환성을 주장하지 않는다.

## 참조와 변경 경계

- Cashier는 읽기 전용 참조다. 현재 checkout·코드·패키지·사용처를 확인하고 그대로 채택/개선 후 채택/보류의 근거를 제시한다. 사용자 채택 검토 전 코드·plugin을 복사하거나 의존성을 바꾸지 않는다.
- 게임별 enum, ID 구간, CSV 스키마, 엔딩·부팅 정책과 로딩 화면을 공용 코어에 넣지 않는다. 외부 코드 이식 시 출처·버전·라이선스·의존성을 확인한다.
- 기존 dirty·무관한 변경을 보존한다. Git 초기화·stage·commit·push·merge·reset·stash·clean은 해당 작업의 명시적 요청 없이는 수행하지 않는다.
- 2026-10-02 사용자는 KangSungKyu/TPLab 비공개 저장소 생성과 Git 관리를 승인했다. 이 저장소의 초기화·브랜치·커밋·푸시·PR 등 일반 관리는 이 승인 범위에서 진행한다. 이력 재작성·강제 푸시·삭제·무관한 저장소 변경은 별도 명시적 요청이 필요하다.
- 사용자 승인 없는 새 목표, 광범위 포맷팅, 자산 재직렬화, 패키지 추가·제거, 프로젝트 설정 변경은 하지 않는다.
