# DataTableManager 계약

2026-10-02. `MyLab.Core.DataTables`의 일반 C# 소유자다. Singleton 상속과 게임별 enum·ID 구간·CSV DTO·검색 규칙을 강제하지 않는다. [검증 기록](validation/data-tables/README.md)에서 현재 실행 범위와 증거를 확인한다.

## 등록과 공개

1. `Register<TKey, TRow>`로 테이블 이름, 필수 열 이름, CSV 문자열 공급자, 현재 행의 파서, 키 선택자와 선택적 행 검증을 등록한다.
2. `AddValidator`로 전체 후보 snapshot의 교차 테이블 검증을 등록한다. FK·게임 규칙은 소비 프로젝트가 소유한다.
3. `LoadAsync`는 등록 순서로 모든 테이블을 읽는다. 전체 후보와 모든 검증이 성공하면 `Snapshot`을 한 번 교체한다.
4. 소비자는 반환된 `DataTableSnapshot.GetTable<TKey, TRow>(name)`으로 읽는다. 여러 테이블을 함께 사용하는 작업은 같은 snapshot을 보관하여 세대를 맞춘다.

`Snapshot`은 최초 성공 전과 Dispose 후 null이다. 재로드 중에는 이전 정상 snapshot을 유지한다. 어느 소스·행·키·FK 검증이라도 실패하면 후보 전체를 버리고 이전 snapshot을 그대로 유지한다. 이후 LoadAsync로 재시도할 수 있다. 저장·외부 상태 갱신 등의 부수 효과까지 rollback하는 계약은 아니므로 파서와 검증 callback은 외부 상태를 변경하지 않아야 한다.

이름은 대소문자를 구분하는 ordinal 문자열이다. 같은 이름은 한 번만 등록하며 첫 유효한 로드 시작부터 등록·검증 구성이 동결된다. 테이블이 없거나 호출자가 사전 취소한 요청은 로드를 시작하지 않고 동결하지 않는다. 중첩된 LoadAsync는 진행 중 요청을 공유하며, 완료 뒤 LoadAsync는 명시적인 전체 재로드다. 검증 callback 안에서 이 소유자의 로드 완료를 기다리지 않는다.

```csharp
var tables = new DataTableManager();
tables.Register("items", new[] { "Id", "Name" },
    async token => (await resources.LoadAssetAsync<TextAsset>(csvKey, token)).text,
    csv => (Id: csv.GetField<int>("Id"), Name: csv.GetField("Name")),
    row => row.Id);
var snapshot = await tables.LoadAsync(cancellationToken);
var item = snapshot.GetTable<int, (int Id, string Name)>("items")[1];
```

`resources`와 `csvKey`는 소비 프로젝트에서 주입한다. CSV 공급자가 실제 파일·네트워크·Addressables handle을 소유·정리하며 DataTableManager는 관리 데이터만 보관한다. ResourceManager는 scope 안에서 자산을 캐시하므로 위 예제의 재로드가 원격 content를 다시 다운로드하거나 catalog를 갱신한다는 뜻은 아니다.

## CSV와 데이터 계약

- 설치된 CsvHelper 33.1.0을 사용한다. `ReadHeader`와 소비자의 `GetField` 행 파싱은 [공식 수동 읽기 방식](https://joshclose.github.io/CsvHelper/examples/reading/reading-by-hand/)을 따른다. 구분자는 쉼표, 문화권은 InvariantCulture다. BOM·따옴표 안 쉼표·줄바꿈·이스케이프된 따옴표를 지원한다.
- 필수 열은 비어 있지 않고 중복되지 않아야 하며 등록 시 복사한다. 실제 CSV header도 열 이름이 비어 있거나 중복되면 거부한다. 필수 열의 누락은 행이 없는 CSV에서도 거부한다. 추가로 이름이 있는 열은 허용한다.
- 빈 문자열·null CSV, 열 수 불일치·잘못된 인용·타입 변환 오류, null 행·null 키·중복 키는 거부한다. 키 비교는 `EqualityComparer<TKey>.Default`다. 키 0·특정 ID 구간은 공용판의 오류가 아니다.
- header만 있는 빈 테이블은 허용한다. 빈 값·최소 행 수·범위·FK 정책은 행 검증과 전체 후보 검증에서 지정한다. CsvHelper의 기본 빈 줄 건너뛰기 정책을 사용한다.
- 행 파서는 현재 record만 읽으며 reader를 진행·해제·보관하지 않는다. Source/CSV/행/키 오류는 테이블·행 문맥을 가진 InvalidDataException과 원인 예외로 전달한다. 교차 검증에서 던진 예외와 취소는 그대로 전달한다. 실패를 빈 성공 테이블로 대체하지 않는다.

Snapshot과 테이블 dictionary는 읽기 전용 container다. 행 객체 자체를 깊게 복제하지 않으므로 불변 DTO를 사용하거나 읽기 전용으로 취급해야 한다. 이전 snapshot을 보관한 소비자는 재로드·소유자 종료 후에도 그 관리 데이터를 읽을 수 있다. 알려지지 않은 이름은 KeyNotFoundException, 정확하지 않은 key/row 제네릭 타입은 InvalidOperationException이다.

## 비동기 수명과 root 주입

공용 메서드는 Unity 메인 스레드에서 호출한다. CSV 공급자가 worker에서 완료해도 파싱·게임 검증·snapshot 공개는 메인 스레드로 돌아온 뒤 실행한다. 순차 파싱은 준비 단계의 작업이며 큰 CSV의 프레임 예산·백그라운드 파싱은 이번 계약에 포함하지 않는다.

각 LoadAsync의 CancellationToken은 해당 호출자의 대기만 취소한다. 다른 대기자와 소유자의 작업은 계속되며 모든 호출자가 대기를 취소해도 완료된 snapshot은 소유자에게 공개할 수 있다. Dispose는 영구 종료하며 소유자 작업·공유 대기를 취소하고 Snapshot을 비운다. 늦은 공급자 완료는 공개하지 않는다. 반복 Dispose는 무시한다. 공급자의 네이티브 I/O 완료·자원 해제 대기는 해당 자원 소유자가 책임진다.

프로젝트 installer는 다음 순서로 [SceneRoot](SCENE_ROOT.md)에 연결한다. 별도의 필수 DataTableManager 전역 host는 만들지 않는다.

1. ResourceManagerInstaller 뒤에 프로젝트의 데이터 installer를 등록한다. Install에서 DataTableManager를 생성하고 주입된 소스·스키마를 등록한다.
2. PrepareAsync에서 LoadAsync를 기다린다. 마지막 installer까지 준비된 뒤 [SceneRootFlow](ASYNC_SCENE_LIFECYCLE.md)가 씬 진행 callback을 실행한다.
3. ReleaseAsync와 Uninstall에서 데이터 소유자를 Dispose하고 참조를 비운다. 역순 정리로 ResourceManager의 ShutdownAsync보다 먼저 데이터를 종료한다.

실행 가능한 연결은 [DataTableConsumerProbe](../Assets/MyLab/Tests/Fixtures/DataTableConsumerProbe.cs)와 [ResourceManagerTests](../Assets/MyLab/Tests/PlayMode/ResourceManagerTests.cs)의 두 데이터 root 테스트다. CSV 검증 실패는 root 준비 실패로 전달되어 가림막을 유지하고 씬 진행을 중단하며 자원을 정리한다. 호출자 취소만으로 root 소유자가 종료되지는 않으므로 전환 소유자가 명시적 ShutdownAsync를 수행하는 기존 정책을 따른다.

## Cashier 참조 판단

읽기 전용 기준은 `total_merge / 8b093946a5ffa85864bea734ce6eeab6ccea41a0`의 `Assets/Scripts/Manager/DataTableManager.cs`와 각 테이블이다. PendingRows·Validate·Commit의 검증 후 공개 원칙을 개선 후 채택했다. Cashier의 테이블별 Commit과 선행 공개를 전체 테이블의 원자성 증거로 사용하지 않고 MyLab은 완전한 후보 snapshot 한 개를 교체한다.

Cashier의 Singleton 상속, 구체 테이블 목록, idx / 1000, Datas label 자동 탐색, Resources fallback, 게임별 FK는 이식하지 않았다. 설치된 CsvHelper만 사용하고 Cashier 코드·plugin·프로젝트 설정은 변경하거나 복사하지 않았다.
