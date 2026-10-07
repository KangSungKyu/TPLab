using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.DataTables;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Editor.DataTables
{
    /// <summary>Preflight, compilation and full validation are distinct outcomes.</summary>
    public enum DataTableImportStatus
    {
        Disabled, AwaitingConfiguration, AwaitingCompilation, Validated, Failed, Cancelled
    }

    /// <summary>Editor diagnostics only. Never contains or publishes a runtime snapshot.</summary>
    public sealed class DataTableImportResult
    {
        /// <summary>Distinguishes pending/failed work from full typed validation.</summary>
        public DataTableImportStatus Status
        {
            get; internal set;
        }
        /// <summary>Current failure or next action; contains no runtime snapshot.</summary>
        public string Diagnostic
        {
            get; internal set;
        }
        /// <summary>Input/configuration/code identity of a fully validated attempt; absent for other outcomes.</summary>
        public string Fingerprint
        {
            get; internal set;
        }
    }

    /// <summary>Fixed input set for a fresh temporary manager. Project code registers typed factories explicitly.</summary>
    public sealed class DataTableImportContext
    {
        internal Dictionary<string, DataTableImporter.Input> Inputs;
        private readonly HashSet<string> _registered = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Only the temporary validation manager; project code may add FK/interface/whole-candidate validators.</summary>
        public DataTableManager Manager
        {
            get; internal set;
        }
        /// <summary>Registers a schema's exact compiled DTO/table using captured local CSV and a fresh factory.</summary>
        public void RegisterTable<TRow, TTable>(string tableId, Func<TTable> factory)
            where TRow : class, IDataRow where TTable : CsvDataTable<TRow>
        {
            if (!Inputs.TryGetValue(tableId, out var input))
                throw new InvalidDataException("Unknown schema: " + tableId);
            var schema = input.Schema;
            if (typeof(TRow).FullName != schema.Namespace + "." + schema.RowType || typeof(TTable).FullName != schema.Namespace + "." + schema.TableType)
                throw new InvalidDataException("Registered types do not match schema: " + tableId);
            foreach (var column in schema.Columns)
            {
                var property = typeof(TRow).GetProperty(column.Member);
                if (property == null || property.PropertyType != DataTableGenerator.FieldType(column.Type))
                    throw new InvalidDataException("Compiled member differs from schema: " + tableId + "." + column.Member);
            }
            Manager.RegisterTable<TRow, TTable>(schema.DataType, tableId, _ => UniTask.FromResult(input.Csv), factory);
            _registered.Add(tableId);
        }

        internal bool IsComplete => _registered.SetEquals(Inputs.Keys);
    }

    /// <summary>Explicit project Editor connections. Register from InitializeOnLoadMethod; no assembly discovery or activation.</summary>
    public static class DataTableImportProfiles
    {
        internal sealed class Profile
        {
            internal Func<IIdxRouter> CreateRouter; internal Action<DataTableImportContext> Configure;
        }
        internal static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>(StringComparer.Ordinal);
        internal static int Revision
        {
            get; private set;
        }
        /// <summary>Registers one immutable-router factory and the same typed table/validator rules used by the project.</summary>
        public static void Register(string id, Func<IIdxRouter> createRouter, Action<DataTableImportContext> configure)
        {
            if (string.IsNullOrWhiteSpace(id) || createRouter == null || configure == null)
                throw new ArgumentException("A named router and configuration are required.");
            Profiles.Add(id, new Profile { CreateRouter = createRouter, Configure = configure });
            ++Revision;
            DataTableImportAutomation.Invalidate();
        }
        /// <summary>Removes an owned registration. Mainly useful for project teardown and isolated tests.</summary>
        public static void Unregister(string id)
        {
            if (!Profiles.Remove(id))
                return;
            ++Revision;
            DataTableImportAutomation.Invalidate();
        }
    }

    /// <summary>Runs preflight and optional owned generation, then uses the existing manager for actual typed validation.</summary>
    public static class DataTableImporter
    {
        internal sealed class Input
        {
            internal string SchemaPath, SchemaText, CsvPath, Csv, Owner, InputGuid, Contract;
            internal DataTableSchema Schema;
            internal IReadOnlyDictionary<string, string> Generated;
        }

        /// <summary>Inspects all configured schemas. Manual calls work in Disabled mode; automatic calls obey settings.</summary>
        /// <param name="settings">Project settings. Null disables automatic work.</param>
        /// <param name="generate">Allows writes to generated-mode owned files after preflight.</param>
        /// <param name="automatic">Enforces the selected automation mode and protects contract changes.</param>
        /// <param name="cancellationToken">Cancels this attempt; owned temporary managers are disposed.</param>
        /// <returns>Diagnostic outcome. Source generation alone never returns Validated.</returns>
        public static async UniTask<DataTableImportResult> RunAsync(DataTableImportSettings settings, bool generate = false, bool automatic = false, CancellationToken cancellationToken = default)
        {
            if (!PlayerLoopHelper.IsMainThread)
                return Result(DataTableImportStatus.Failed, "Run the importer on the Editor main thread.");
            if (settings == null || automatic && settings.AutomationMode == DataTableAutomationMode.Disabled)
                return Result(DataTableImportStatus.Disabled, "Automation is disabled.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return Result(DataTableImportStatus.AwaitingCompilation, "Run in an idle Editor.");
            if (DataTableImportAutomation.CompilationFailed)
                return Result(DataTableImportStatus.Failed, "Resolve script compilation errors before typed validation.");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateSettings(settings);
                if (!DataTableImportProfiles.Profiles.TryGetValue(settings.ValidationProfileId, out var profile))
                    return Result(DataTableImportStatus.AwaitingConfiguration, "Register the selected project validation profile.");
                IIdxRouter router = profile.CreateRouter();
                if (router == null)
                    return Result(DataTableImportStatus.AwaitingConfiguration, "The profile must provide an immutable idx router.");
                var inputs = ReadInputs(settings);
                if (inputs.Count == 0)
                    return Result(DataTableImportStatus.AwaitingConfiguration, "No table schemas were found.");
                foreach (var input in inputs.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DataTableGenerator.ValidateCsv(input.Schema, input.Csv, router);
                    if (input.Schema.Mode == "generated")
                    {
                        input.Generated = DataTableGenerator.Generate(input.Schema, settings.DefaultNamespace);
                        DataTableGeneratedFiles.Check(settings.OutputFolder, input.Owner, input.Contract, input.Generated, automatic);
                        if (DataTableGeneratedFiles.ReadRecord(settings.OutputFolder, input.Owner) == null)
                            foreach (string fullName in new[] { input.Schema.Namespace + "." + input.Schema.RowType, input.Schema.Namespace + "." + input.Schema.TableType })
                                if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(fullName, false) != null))
                                    throw new InvalidDataException("Unowned compiled type already exists: " + fullName);
                    }
                }
                string before = Stamp(settings, inputs);
                bool mayWrite = generate && (!automatic || settings.AutomationMode == DataTableAutomationMode.GenerateValidated);
                bool changed = false;
                if (mayWrite)
                    foreach (var input in inputs.Values.Where(i => i.Generated != null))
                    {
                        changed |= DataTableGeneratedFiles.Apply(settings.OutputFolder, input.Owner, input.Contract, input.Generated, automatic);
                        var record = DataTableGeneratedFiles.ReadRecord(settings.OutputFolder, input.Owner);
                        if (record.InputGuid != input.InputGuid)
                        {
                            record.InputGuid = input.InputGuid;
                            DataTableGeneratedFiles.SaveRecord(settings.OutputFolder, record);
                        }
                    }
                if (changed)
                {
                    string asset = AssetDatabase.GetAssetPath(settings);
                    if (!string.IsNullOrEmpty(asset))
                    {
                        SessionState.SetString(DataTableImportAutomation.PendingKey, asset);
                        SessionState.SetBool(DataTableImportAutomation.PendingAutomaticKey, automatic);
                    }
                    AssetDatabase.Refresh();
                    return Result(DataTableImportStatus.AwaitingCompilation, "Sources were provisionally generated. Compile, connect typed registrations, then validate again.");
                }
                // Unwritten or stale generated code cannot be reported as the schema's compiled contract.
                foreach (var input in inputs.Values.Where(i => i.Generated != null))
                    foreach (var pair in input.Generated)
                    {
                        string path = DataTableGeneratedFiles.AssetPath(settings.OutputFolder + "/" + pair.Key, true);
                        if (!File.Exists(path) || File.ReadAllText(path) != pair.Value)
                            return Result(DataTableImportStatus.AwaitingCompilation, "Apply the generated schema sources before typed validation.");
                    }
                using (var manager = new DataTableManager())
                {
                    manager.RegisterIdxRouter(router);
                    var context = new DataTableImportContext { Manager = manager, Inputs = inputs };
                    profile.Configure(context);
                    if (!context.IsComplete)
                        return Result(DataTableImportStatus.AwaitingConfiguration, "Connect every schema using context.RegisterTable with its exact compiled types.");
                    DataTableSnapshot snapshot;
                    try
                    {
                        snapshot = await manager.LoadAsync(cancellationToken);
                    }
                    finally { await UniTask.SwitchToMainThread(); }
                    if (snapshot.Count != inputs.Count)
                        throw new InvalidDataException("Validation profile registered tables outside the captured input set.");
                    cancellationToken.ThrowIfCancellationRequested();
                    if (before != Stamp(settings, inputs))
                        return Result(DataTableImportStatus.Cancelled, "Settings, inputs or source code changed during validation.");
                    foreach (var input in inputs.Values.Where(i => i.Generated != null))
                    {
                        var record = DataTableGeneratedFiles.ReadRecord(settings.OutputFolder, input.Owner);
                        record.LastValidated = before;
                        record.InputGuid = input.InputGuid;
                        foreach (var file in record.Files)
                            file.LastGoodContent = File.ReadAllText(DataTableGeneratedFiles.AssetPath(settings.OutputFolder + "/" + file.Name, true));
                        DataTableGeneratedFiles.SaveRecord(settings.OutputFolder, record);
                    }
                    return new DataTableImportResult { Status = DataTableImportStatus.Validated, Diagnostic = "All " + inputs.Count + " tables passed shared and project validation.", Fingerprint = before };
                }
            }
            catch (OperationCanceledException) { return Result(DataTableImportStatus.Cancelled, "Validation cancelled."); }
            catch (Exception error) { return Result(DataTableImportStatus.Failed, error.Message); }
        }

        internal static void ValidateSettings(DataTableImportSettings settings)
        {
            if (!Enum.IsDefined(typeof(DataTableAutomationMode), settings.AutomationMode))
                throw new InvalidDataException("Unknown automation mode.");
            DataTableGeneratedFiles.AssetPath(settings.InputFolder);
            DataTableGeneratedFiles.AssetPath(settings.SchemaFolder);
            DataTableGeneratedFiles.AssetPath(settings.OutputFolder, true);
            DataTableGenerator.CheckNamespace(settings.DefaultNamespace);
            if (Under(settings.OutputFolder, settings.InputFolder) || Under(settings.OutputFolder, settings.SchemaFolder) ||
                Under(settings.InputFolder, settings.OutputFolder) || Under(settings.SchemaFolder, settings.OutputFolder))
                throw new InvalidDataException("Input/schema and generated folders must be separate.");
            if (!Directory.Exists(settings.InputFolder) || !Directory.Exists(settings.SchemaFolder))
                throw new InvalidDataException("Input and schema folders must exist.");
        }

        internal static bool Under(string path, string folder) => path.Equals(folder, StringComparison.OrdinalIgnoreCase) || path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, Input> ReadInputs(DataTableImportSettings settings)
        {
            var inputs = new Dictionary<string, Input>(StringComparer.Ordinal);
            var kinds = new HashSet<uint>();
            var generatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.GetFiles(settings.SchemaFolder, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                string path = file.Replace('\\', '/');
                string json = File.ReadAllText(DataTableGeneratedFiles.AssetPath(path));
                var schema = DataTableGenerator.ReadSchema(json);
                if (string.IsNullOrEmpty(schema.Namespace))
                    schema.Namespace = settings.DefaultNamespace;
                string guid = AssetDatabase.AssetPathToGUID(path);
                string owner = "schema_" + (string.IsNullOrEmpty(guid) ? DataTableGeneratedFiles.Hash(path) : guid);
                string inputPath = schema.Input;
                var record = schema.Mode == "generated" ? DataTableGeneratedFiles.ReadRecord(settings.OutputFolder, owner) : null;
                if (!string.IsNullOrEmpty(record?.InputGuid))
                {
                    string moved = AssetDatabase.GUIDToAssetPath(record.InputGuid);
                    if (!string.IsNullOrEmpty(moved) && File.Exists(DataTableGeneratedFiles.AssetPath(moved)) && AssetDatabase.AssetPathToGUID(moved) == record.InputGuid)
                        inputPath = moved;
                }
                if (!Under(inputPath, settings.InputFolder) || !inputPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Table input must be CSV inside the input folder: " + schema.TableId);
                string csv = File.ReadAllText(DataTableGeneratedFiles.AssetPath(inputPath));
                if (!kinds.Add(schema.DataType) || inputs.ContainsKey(schema.TableId))
                    throw new InvalidDataException("Duplicate table name or kind.");
                if (schema.Mode == "generated")
                    foreach (string name in new[] { schema.RowType, schema.TableType })
                        if (!generatedNames.Add(name))
                            throw new InvalidDataException("More than one schema owns the same generated file: " + name);
                string contract = JsonConvert.SerializeObject(new
                {
                    schema.SchemaVersion,
                    schema.TableId,
                    schema.DataType,
                    schema.Namespace,
                    schema.RowType,
                    schema.TableType,
                    Columns = schema.Columns.OrderBy(c => c.Member, StringComparer.Ordinal).ToArray()
                });
                inputs.Add(schema.TableId, new Input
                {
                    Schema = schema,
                    SchemaPath = path,
                    SchemaText = json,
                    CsvPath = inputPath,
                    Csv = csv,
                    Owner = owner,
                    InputGuid = AssetDatabase.AssetPathToGUID(inputPath),
                    Contract = contract
                });
            }
            if (Directory.Exists(settings.OutputFolder))
                foreach (string manifest in Directory.GetFiles(settings.OutputFolder, "*.tableimport.json"))
                {
                    var record = JsonConvert.DeserializeObject<DataTableGeneratedFiles.Record>(File.ReadAllText(DataTableGeneratedFiles.AssetPath(manifest.Replace('\\', '/'), true)));
                    if (record == null || !inputs.Values.Any(i => i.Owner == record.Owner))
                        throw new InvalidDataException("Missing source schema for owned output. Generated files were retained: " + manifest);
                }
            return inputs;
        }

        private static string Stamp(DataTableImportSettings settings, Dictionary<string, Input> inputs)
        {
            var profile = DataTableImportProfiles.Profiles[settings.ValidationProfileId];
            string text = JsonUtility.ToJson(settings) + DataTableImportProfiles.Revision +
                typeof(DataTableManager).Module.ModuleVersionId + typeof(DataTableImporter).Module.ModuleVersionId +
                profile.CreateRouter.Method.Module.ModuleVersionId + profile.Configure.Method.Module.ModuleVersionId;
            foreach (var input in inputs.Values)
            {
                if (File.ReadAllText(DataTableGeneratedFiles.AssetPath(input.SchemaPath)) != input.SchemaText || File.ReadAllText(DataTableGeneratedFiles.AssetPath(input.CsvPath)) != input.Csv)
                    throw new OperationCanceledException("Input changed.");
                text += input.SchemaPath + input.SchemaText + input.CsvPath + input.Csv;
                // Compiled identities invalidate prior success when project DTO/table code changes.
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName, StringComparer.Ordinal))
                    if (assembly.GetType(input.Schema.Namespace + "." + input.Schema.RowType, false) != null ||
                        assembly.GetType(input.Schema.Namespace + "." + input.Schema.TableType, false) != null)
                        text += assembly.ManifestModule.ModuleVersionId;
            }
            if (Directory.Exists(settings.OutputFolder))
                foreach (string path in Directory.GetFiles(settings.OutputFolder, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                    text += path + File.ReadAllText(DataTableGeneratedFiles.AssetPath(path.Replace('\\', '/'), true));
            return DataTableGeneratedFiles.Hash(text);
        }

        private static DataTableImportResult Result(DataTableImportStatus status, string message) => new DataTableImportResult { Status = status, Diagnostic = message };
    }
}
