# Editor

Editor 도구는 Bootstrap/씬 전환 설정을 사전 검사하고 CSV와 명시적 JSON schema로 프로젝트 DTO·테이블 C# 소스를 생성/검증한다. runtime 데이터를 Editor에서 자동 등록하거나 공개하지 않는다.

- Assembly: `TPLab.Core.Editor` (Editor 전용); Namespace: `TPLab.Core.Editor.DataTables`, `TPLab.Core.Editor.Bootstrap`
- SourceRevision: `896bcbeeb8e627e193bb1eba5032455b7f8e3f32`; [소스](../../Assets/TPLab/Editor)
- ImplementationStatus: Implemented; ValidationStatus: Partial. [importer 검증](../validation/data-table-importer/README.md), [씬 gate 검증](../validation/scene-transition-editor/README.md), [최종 회귀](../validation/input-system/p4/README.md). P2에서 실제 Git/tarball importer 소비 실행을 확인했다. 이번 Windows 교체 보정의 새 package 검증은 배포 후보 gate에서 별도로 기록한다.
- 의존성: Core, UniTask, Addressables Editor 및 설치된 Unity Newtonsoft.Json **3.2.2**. runtime Player에 이 assembly를 포함하지 않는다.

`Create > TPLab > Data Table Import Settings`로 설정을 만들고 자동 사용 시 `Assets/Editor/TPLab/setting.asset`에 저장한다. 없으면 자동화는 꺼져 있다. Inspector에서 명시적 검증/생성도 요청할 수 있다. [schema 및 설정 상세](../DATA_TABLE_IMPORTER_DRAFT.md)를 따른다.

| DataTableImportSettings 항목 | 기본값·용도 |
|---|---|
| `AutomationMode` | Disabled; enum은 Disabled / ValidateOnly / GenerateValidated |
| `InputFolder` | Assets/Game/Data; CSV 행 입력 |
| `SchemaFolder` | Assets/Game/DataSchemas; 명시적 schema JSON |
| `OutputFolder` | Assets/Game/Generated/Data; 프로젝트 runtime 소스 |
| `DefaultNamespace` | Game.Data |
| `ValidationProfileId` | 빈 문자열; 프로젝트 Editor 등록 ID |

설정은 모두 public get/set이고 `ActivePath`는 public const string이다. profile은 프로젝트 `[InitializeOnLoadMethod]`에서 등록한다. `createRouter`는 immutable router를 만들고 configure는 `context.RegisterTable<TRow,TTable>`로 모든 schema의 **정확한 컴파일 타입**을 연결한다. 필요 시 `context.Manager`에 동일 FK/binding/validator를 등록한다. context는 importer가 제공하며 임의 생성해서 사용하는 데이터 저장소가 아니다.

```csharp
// DataTableImportProfiles
static void Register(string id, Func<IIdxRouter> createRouter,
    Action<DataTableImportContext> configure);
static void Unregister(string id);
// DataTableImportContext
DataTableManager Manager { get; } // internal setter
void RegisterTable<TRow, TTable>(string tableId, Func<TTable> factory)
    where TRow : class, IDataRow where TTable : CsvDataTable<TRow>;
// DataTableImporter
static UniTask<DataTableImportResult> RunAsync(DataTableImportSettings settings,
    bool generate = false, bool automatic = false, CancellationToken cancellationToken = default);
// DataTableImportAutomation
static DataTableImportStatus LastStatus { get; }
static string LastDiagnostic { get; }
static void RequestManual(DataTableImportSettings settings, bool generate);
static void Notify(string[] paths, bool didDomainReload = false);
```

등록 ID는 nonempty/unique이고 factory·configure 필수다. 동일 ID 재등록은 거부한다. 모든 작업은 idle Editor 메인 스레드에서 한다. 자동화는 변경을 모아 import callback 밖에서 실행하고 source 생성 후 domain reload에 이어 검증한다. 직접 RunAsync를 겹쳐 호출하지 말고 큐 API를 사용한다. `Notify`의 paths는 null이 아닌 경로 배열이다.

`DataTableImportResult`의 `Status`, `Diagnostic`, `Fingerprint`는 public getter/internal setter다. enum: Disabled, AwaitingConfiguration, AwaitingCompilation, Validated, Failed, Cancelled. 생성·사전 검사만 성공하면 아직 Validated가 아니다. 컴파일된 전체 테이블의 공용/프로젝트 검증까지 성공해야 fingerprint가 생긴다. importer는 오류/취소를 Failed/Cancelled 진단 결과로 돌려준다. 검증 중 입력·설정·코드가 달라지면 Cancelled이고 runtime snapshot은 공개하지 않는다. 생성된 소스가 provisional이어도 남을 수 있으므로 상태와 manifest를 확인한다.

schema version1, format `csv`, mode `generated` 또는 `existing`. 표준 PK는 required `uint` 컬럼 `idx` → `Id`. `DataTableSchema`의 public fields는 `SchemaVersion:int`, `TableId/Input/Format/Mode/Namespace/RowType/TableType:string`, `DataType:uint`, `Columns:DataTableColumn[]`. column fields는 `Name/Member/Type:string`, `Required:bool`. required는 **컬럼 존재**이며 값의 nonempty 규칙은 project validator다. JSON field는 소스의 JsonProperty 이름을 따른다. JSON은 schema 용도이고 JSON 행 로더가 아니다.

```csharp
// DataTableGenerator: 사전검사/문자열 생성, 파일 쓰기 없음
static DataTableSchema ReadSchema(string json);
static void ValidateCsv(DataTableSchema schema, string csv, IIdxRouter router);
static IReadOnlyDictionary<string, string> Generate(DataTableSchema schema, string defaultNamespace);
// DataTableGeneratedFiles: manifest 기반 파일 소유권
static string AssetPath(string path, bool output = false);
static void Check(string outputFolder, string ownerId, string contract,
    IReadOnlyDictionary<string, string> files, bool automatic = false);
static bool Apply(string outputFolder, string ownerId, string contract,
    IReadOnlyDictionary<string, string> files, bool automatic = false);
```

지원 type은 string, bool, int, uint, long, ulong, float, double, decimal 및 value type의 `?`이다. namespace/identifier·unknown/duplicate JSON field·CSV 변환·idx를 사전 검사한다. 생성 결과는 `<Row>.g.cs`, `<Table>.g.cs` 두 partial class다. 별도 partial에 프로젝트 검사/interface를 작성한다. core namespace에 프로젝트 타입을 생성하지 않는다. low-level generator/file APIs는 오류를 예외로 전달하며 invalid schema/소유권/path는 주로 `InvalidDataException`, router null은 `ArgumentNullException`이다. 파일 Apply는 쓰기 실패 시 rollback한다. Windows의 sharing/lock/교체 대상 제거 오류(32/33/1175)는 같은 원자적 `File.Replace`를 최대 5회, 50ms 간격으로만 시도한다. 다른 오류·사라진 파일·소진은 기존 예외/rollback으로 전파하며 삭제 후 Move나 직접 덮어쓰기로 교체를 우회하지 않는다. 교체 파일 하나당 동기 대기 요청 합계는 최대 200ms이며 파일 I/O와 OS 스케줄링 시간은 별도다. 마지막 IOException의 Data에 ReplacementDestination/ReplacementAttempts를 남긴다.

경로는 forward-slash Assets 상대 경로이며 traversal·linked path·보호된 core/test/Editor output은 거부한다. 기존 생성물의 해시와 manifest가 맞아야 수정할 수 있다. 사용자 편집·알 수 없는 파일은 덮어쓰지 않고 `.meta`를 보존한다. 자동 schema 계약 변경은 거부하므로 검토 후 수동 적용한다. 입력 삭제가 generated source 자동 삭제나 rename migration을 뜻하지 않는다. 생성 소스/manifest/설정은 버전 관리할 프로젝트 소스다.

Bootstrap 검사 API:

```csharp
// BootstrapValidator
static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths);
static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths,
    AddressableAssetSettings settings);
static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap);
static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap,
    AddressableAssetSettings settings);
static IReadOnlyList<string> ValidateGameScene(Scene scene);
// BootstrapEditorValidation
static IReadOnlyList<string> ValidateEditorSetup(bool enteringPlay);
```

빈 오류 목록은 해당 사전 검사 통과이며 씬 실행 완료 증거가 아니다. 저장된 씬은 격리 preview로 검사하고 사용자 씬을 저장/대체하지 않는다. 첫 build scene/root/installer/Single 유지 가능성/정의/condition metadata 및 명시적으로 선택한 BuildScene·Addressables 등록을 검사한다. Inspector·컴파일 후·Play 진입·build gate가 연결되어 있다. unsaved live root와 실제 Player build scene 목록도 검사한다. `SceneAsset`의 등록 여부로 backend를 자동 선택하지 않는다. [Bootstrap 사용](../BOOTSTRAP_SYSTEM.md), [씬 API](SceneManagement.md)를 따른다.

최소 importer 예제는 [설정/profile 템플릿](../DATA_TABLE_IMPORTER_DRAFT.md)이다. 문서의 선언 블록은 발췌(NotRun); 설정을 만들고 profile 등록 → CSV/schema 사전검사 → 명시적 생성 → compile → typed 전체 검증의 순서를 지킨다. JSON 행·자동 타입 추론·자동 이름 변경·runtime 설정 UI는 제공하지 않는다.

StagingPath: 임시 파일은 출력 폴더 안의 GUID.tmp로 만들어 최종 파일명에 37자를 덧붙이지 않는다. 최종/임시 절대 경로에는 Unity·OS 파일시스템 제한이 여전히 적용되며 임의의 긴 경로 지원을 보장하지 않는다.
