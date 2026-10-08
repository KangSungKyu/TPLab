PackageVersion: 0.0.1. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.

# Editor

Module: Editor
Namespace: TPLab.Core.Editor.DataTables; TPLab.Core.Editor.Bootstrap
Assembly: TPLab.Core.Editor (includePlatforms: Editor)
SourceRevision: 896bcbeeb8e627e193bb1eba5032455b7f8e3f32
SourcePath: [Editor](../../../Editor)
HumanContract: [Editor](../../api/Editor.md), [importer](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/DATA_TABLE_IMPORTER_DRAFT.md), [Bootstrap](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/BOOTSTRAP_SYSTEM.md)
ImplementationStatus: Implemented
ValidationStatus: Partial
Evidence: [importer](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/data-table-importer/README.md), [scene gate](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/scene-transition-editor/README.md), [final regression](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/validation/input-system/p4/README.md)

Symbol / Signature / Constraints:

```csharp
// DataTableImportSettings : ScriptableObject
const string ActivePath = "Assets/Editor/TPLab/setting.asset";
DataTableAutomationMode AutomationMode { get; set; }
string InputFolder { get; set; } // Assets/Game/Data
string SchemaFolder { get; set; } // Assets/Game/DataSchemas
string OutputFolder { get; set; } // Assets/Game/Generated/Data
string DefaultNamespace { get; set; } // Game.Data
string ValidationProfileId { get; set; } // ""
// enum DataTableAutomationMode: Disabled, ValidateOnly, GenerateValidated
// enum DataTableImportStatus:
// Disabled, AwaitingConfiguration, AwaitingCompilation, Validated, Failed, Cancelled
// DataTableImportResult (get; internal set;)
DataTableImportStatus Status { get; }
string Diagnostic { get; }
string Fingerprint { get; }
// DataTableImportContext
DataTableManager Manager { get; } // internal setter
void RegisterTable<TRow, TTable>(string tableId, Func<TTable> factory)
    where TRow : class, IDataRow where TTable : CsvDataTable<TRow>;
// DataTableImportProfiles
static void Register(string id, Func<IIdxRouter> createRouter, Action<DataTableImportContext> configure);
static void Unregister(string id);
// DataTableImporter
static UniTask<DataTableImportResult> RunAsync(DataTableImportSettings settings,
    bool generate = false, bool automatic = false, CancellationToken cancellationToken = default);
// DataTableImportAutomation
static DataTableImportStatus LastStatus { get; }
static string LastDiagnostic { get; }
static void RequestManual(DataTableImportSettings settings, bool generate);
static void Notify(string[] paths, bool didDomainReload = false);
// DataTableGenerator
static DataTableSchema ReadSchema(string json);
static void ValidateCsv(DataTableSchema schema, string csv, IIdxRouter router);
static IReadOnlyDictionary<string, string> Generate(DataTableSchema schema, string defaultNamespace);
// DataTableGeneratedFiles
static string AssetPath(string path, bool output = false);
static void Check(string outputFolder, string ownerId, string contract,
    IReadOnlyDictionary<string, string> files, bool automatic = false);
static bool Apply(string outputFolder, string ownerId, string contract,
    IReadOnlyDictionary<string, string> files, bool automatic = false);
// BootstrapValidator
static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths);
static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths, AddressableAssetSettings settings);
static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap);
static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap, AddressableAssetSettings settings);
static IReadOnlyList<string> ValidateGameScene(Scene scene);
// BootstrapEditorValidation
static IReadOnlyList<string> ValidateEditorSetup(bool enteringPlay);
```

Inputs: explicit version1 CSV schema: generated/existing, nonzero kind, required uint idx→Id. DataTableSchema public fields SchemaVersion:int; TableId/Input/Format/Mode/Namespace/RowType/TableType:string; DataType:uint; Columns:DataTableColumn[]. Column fields Name/Member/Type:string, Required:bool. actual JsonProperty names are source authority. primitive supported aliases and nullable value types listed in human contract. profile unique ID and nonnull factories/configure. Notify paths nonnull.
Outputs: two partial C# sources, ownership manifest, Editor result diagnostic/fingerprint; no runtime snapshot. Bootstrap methods return error list, empty means preflight success only.
Errors: RunAsync captures failures/cancellation as Failed/Cancelled; direct low-level APIs propagate InvalidDataException/argument/I/O errors. provisional generation is not Validated. unknown type/field/duplicate JSON/path/user edit/automatic contract change rejected.
Ownership: project owns CSV/schema/settings/generated files/profile rules; importer owns temporary manager and preview scenes; no automatic runtime registration. manifest controls writes, .meta preserved. generated sources are persistent project source, not Temp.
Lifecycle: settings/profile → schema+shared CSV/idx preflight → optional generation → compilation → exact typed full-table/FK validation → Validated fingerprint. missing profile/types=AwaitingConfiguration/Compilation, not success.
Threading: idle Editor main thread; assembly excluded from Player.
Concurrency: queue outside asset callbacks; shared automation cancels obsolete request and resumes after compilation/reload. direct RunAsync overlap unsupported; use RequestManual/Notify. compilation/Play busy returns pending outcome.
Cancellation: RunAsync token/changed input stamp→Cancelled; before assembly reload cancels queued attempt.
FailureCleanup: temporary manager disposed; preview scenes closed; low-level Apply rolls back staged file writes. Windows HRESULT sharing/lock/unable-to-remove-replaced codes32/33/1175 permit only the same atomic File.Replace, maximum5 attempts with50ms intervals (at most200ms requested synchronous waits per replaced file; I/O and OS scheduling add time). Other errors, missing stage/destination, or exhaustion propagate the original IOException and rollback; no delete/move or direct-overwrite fallback. Final IOException.Data includes ReplacementDestination and ReplacementAttempts. does not promise full runtime/source rollback after every later compilation error.
Configuration: absent ActivePath asset disables automation; Disabled permits manual. normalized Assets paths only, no traversal/links/core/test/Editor output. automatic schema contract changes require manual review/apply.
ExtensionPoints: InitializeOnLoadMethod registers profile; context.RegisterTable exact DTO/table names/properties; context.Manager binds FK/interface/whole-validator identical project rules; generated partial files for custom rules.
RequiredSequence: use human setup above; explicit backend scene target/root/actual build scenes must pass compile/Play/build gate before runtime use.
ForbiddenUsage: JSON row loader; infer C# schema from CSV automatically; overwrite hand-edited .g.cs/unowned files; delete sources on input deletion; rename types automatically; publish validation context manager as runtime manager; include Editor assembly in Player; auto-select loader by scene registration.
Example: [human declarations](../../api/Editor.md) are NotRun excerpts; [importer template](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/DATA_TABLE_IMPORTER_DRAFT.md) provides project configuration.
Compatibility: actual Unity scene properties/Inspector drawer types are Editor integration, not separate runtime consumers. project schema changes may need migration/manual apply.
Limitations: minimal Git/tarball importer consumers passed in the P2 validation source; this does not validate project-specific DTO/profile rules. JSON rows/rename migration/generic PK generation not provided.
