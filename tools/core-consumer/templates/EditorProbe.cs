using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CsvHelper.Configuration.Attributes;
using Cysharp.Threading.Tasks;
using TPLab.Core.DataTables;
using TPLab.Core.Editor.DataTables;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace TPLabConsumer
{
    /// <summary>Exercises the installed Editor importer in two explicitly separate Editor processes.</summary>
    public static class EditorProbe
    {
        private const string FolderPrefix = "Assets/TPLabConsumerImporterProbe_";
        private const string Marker = "/probe-owner.txt";
        private const string Rejection = "consumer project validator rejection";

        [Serializable]
        public sealed class ProbeResult
        {
            public bool defaultSettingsDisabled;
            public bool missingSettingsDisabled;
            public bool defaultAutomaticDisabled;
            public bool automaticNoMutation;
            public bool manualTypedValidated;
            public bool manualValidatorObserved;
            public bool generatedUnwrittenPending;
            public bool generatedAwaitingCompilation;
            public bool requiresGeneratedValidationPhase;
            public bool generatedValidated;
            public bool generatedValidatorObserved;
            public bool validatorFailurePreservesOwnedSources;
            public bool packagesOutputDenied;
            public bool packageBytesUnchanged;
            public bool invalidSchemaDiagnostics;
            public bool profileUnregistered;
            public bool ownedAssetsCleaned;
            public bool baselineAssetsUnchanged;
            public bool buildSettingsUnchanged;
            public string diagnostic;
            public string manualDiagnostic;
            public string generatedDiagnostic;
            public string rejectedDiagnostic;
            public string invalidSchemaDiagnostic;
            public string packagesDiagnostic;
        }

        public sealed class ProbeException : Exception
        {
            public ProbeResult Result { get; }

            public ProbeException(ProbeResult result, Exception inner) : base("Installed Editor importer probe failed: " + inner.Message, inner)
            {
                Result = result;
            }
        }

        [Serializable]
        private sealed class FileStamp
        {
            public string path;
            public string hash;
        }

        [Serializable]
        private sealed class State
        {
            public string project;
            public string folder;
            public string id;
            public string generatedNamespace;
            public FileStamp[] assets;
            public FileStamp[] package;
            public string buildSettings;
            public ProbeResult result;
        }

        /// <summary>Validates existing types and negative paths, then retains owned generated sources for the next launch.</summary>
        /// <returns>Preparation observations. AwaitingCompilation is explicitly pending, never a completed validation.</returns>
        public static async UniTask<ProbeResult> RunAsync()
        {
            string statePath = StatePath();
            Require(!File.Exists(statePath), "Importer state already exists; use a fresh consumer run.");
            string token = Guid.NewGuid().ToString("N");
            var state = new State
            {
                project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                folder = FolderPrefix + token,
                id = "consumer_" + token,
                generatedNamespace = "TPLabConsumer.Generated_" + token,
                result = new ProbeResult()
            };
            var result = state.result;
            var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
            try
            {
                var package = PackageInfo.FindForAssembly(typeof(DataTableImporter).Assembly);
                Require(package != null && package.name == "com.tplab.editor", "Importer must come from installed com.tplab.editor.");
                state.package = Capture(package.resolvedPath);
                state.assets = Capture(Application.dataPath);
                state.buildSettings = HashOrMissing("ProjectSettings/EditorBuildSettings.asset");
                Require(!File.Exists(DataTableImportSettings.ActivePath), "A fresh consumer must have no active importer settings.");
                result.defaultSettingsDisabled = settings.AutomationMode == DataTableAutomationMode.Disabled && settings.ValidationProfileId == "";
                var missing = await DataTableImporter.RunAsync(null, true, true);
                result.missingSettingsDisabled = missing.Status == DataTableImportStatus.Disabled;
                var disabled = await DataTableImporter.RunAsync(settings, true, true);
                result.defaultAutomaticDisabled = disabled.Status == DataTableImportStatus.Disabled;
                result.automaticNoMutation = Matches(state.assets) && !File.Exists(DataTableImportSettings.ActivePath);
                Require(result.defaultSettingsDisabled && result.missingSettingsDisabled && result.defaultAutomaticDisabled && result.automaticNoMutation, "Default/missing settings must disable automatic work without creating assets.");

                Directory.CreateDirectory(state.folder);
                File.WriteAllText(state.folder + Marker, state.id);
                Configure(settings, state);
                Directory.CreateDirectory(settings.InputFolder);
                Directory.CreateDirectory(settings.SchemaFolder);
                File.WriteAllText(settings.InputFolder + "/texts.csv", "idx,text\n1001,hello");
                WriteSchema(settings, "existing", "TPLabConsumer", "EditorProbeRow", "EditorProbeTable");
                // Establish schema/input GUIDs before ownership is recorded; a new GUID would change the owner on the next launch.
                AssetDatabase.ImportAsset(settings.InputFolder + "/texts.csv", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(settings.SchemaFolder + "/texts.json", ImportAssetOptions.ForceSynchronousImport);
                Require(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(settings.InputFolder + "/texts.csv")) && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(settings.SchemaFolder + "/texts.json")), "Owned input/schema GUIDs were not imported.");
                bool manualObserved = false;
                DataTableImportProfiles.Register(state.id, () => new DecimalIdxCodec(1000), context =>
                {
                    context.RegisterTable<EditorProbeRow, EditorProbeTable>("texts", () => new EditorProbeTable());
                    context.Manager.AddValidator(snapshot =>
                    {
                        Require(snapshot.Get<EditorProbeRow>(1001).Text == "hello", "Typed validator received incorrect CSV data.");
                        manualObserved = true;
                    });
                });
                var manual = await DataTableImporter.RunAsync(settings);
                result.manualDiagnostic = manual.Status + ": " + manual.Diagnostic;
                result.manualTypedValidated = manual.Status == DataTableImportStatus.Validated && !string.IsNullOrEmpty(manual.Fingerprint) && !Directory.Exists(settings.OutputFolder);
                result.manualValidatorObserved = manualObserved;
                Require(result.manualTypedValidated && manualObserved, "Manual typed validation: " + manual.Status + " " + manual.Diagnostic);

                string output = settings.OutputFolder;
                settings.OutputFolder = "Packages/TPLabConsumerImporterProbe_" + token;
                var denied = await DataTableImporter.RunAsync(settings, true);
                result.packagesDiagnostic = denied.Status + ": " + denied.Diagnostic;
                result.packagesOutputDenied = denied.Status == DataTableImportStatus.Failed && !string.IsNullOrEmpty(denied.Diagnostic) && !Directory.Exists(settings.OutputFolder);
                settings.OutputFolder = output;
                Require(result.packagesOutputDenied, "Packages output must be rejected before any write.");

                File.WriteAllText(settings.SchemaFolder + "/texts.json", "{\"schemaVersion\":2}");
                var invalid = await DataTableImporter.RunAsync(settings, true);
                result.invalidSchemaDiagnostic = invalid.Status + ": " + invalid.Diagnostic;
                result.invalidSchemaDiagnostics = invalid.Status == DataTableImportStatus.Failed && !string.IsNullOrWhiteSpace(invalid.Diagnostic) && !Directory.Exists(output);
                Require(result.invalidSchemaDiagnostics, "Invalid schemas require a failure diagnostic and no output.");

                DataTableImportProfiles.Unregister(state.id);
                DataTableImportProfiles.Register(state.id, () => new DecimalIdxCodec(1000), context => { });
                WriteSchema(settings, "generated", state.generatedNamespace, "ConsumerGeneratedRow", "ConsumerGeneratedTable");
                var pending = await DataTableImporter.RunAsync(settings);
                result.generatedUnwrittenPending = pending.Status == DataTableImportStatus.AwaitingCompilation && string.IsNullOrEmpty(pending.Fingerprint) && !Directory.Exists(output);
                Require(result.generatedUnwrittenPending, "Unwritten generated DTOs must remain pending.");

                // Keep the executeMethod continuation alive long enough to persist the explicit phase boundary.
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    var generated = await DataTableImporter.RunAsync(settings, true);
                    result.generatedDiagnostic = generated.Status + ": " + generated.Diagnostic;
                    result.generatedAwaitingCompilation = generated.Status == DataTableImportStatus.AwaitingCompilation && string.IsNullOrEmpty(generated.Fingerprint)
                        && File.Exists(output + "/ConsumerGeneratedRow.g.cs") && File.Exists(output + "/ConsumerGeneratedTable.g.cs");
                    Require(result.generatedAwaitingCompilation, "Generation must be reported as pending compilation: " + generated.Diagnostic);
                    result.requiresGeneratedValidationPhase = true;
                    result.diagnostic = "Owned generated sources retained. Start a second Editor process to compile and validate them.";
                    CheckProtection(state);
                    Directory.CreateDirectory(Path.GetDirectoryName(statePath));
                    File.WriteAllText(statePath, JsonUtility.ToJson(state, true));
                }
                finally
                {
                    AssetDatabase.AllowAutoRefresh();
                    EditorApplication.UnlockReloadAssemblies();
                }
                return result;
            }
            catch (Exception error)
            {
                Cleanup(state);
                throw new ProbeException(result, error);
            }
            finally
            {
                DataTableImportProfiles.Unregister(state.id);
                result.profileUnregistered = VerifyUnregistered(state.id);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        /// <summary>Validates the prior launch's actual compiled generated DTOs, checks rejecting-validator preservation, and cleans owned assets.</summary>
        public static async UniTask<ProbeResult> ValidateGeneratedAsync()
        {
            string statePath = StatePath();
            Require(File.Exists(statePath), "Importer preparation state is missing.");
            var state = JsonUtility.FromJson<State>(File.ReadAllText(statePath));
            ValidateOwner(state);
            var result = state.result;
            var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
            try
            {
                Configure(settings, state);
                Type row = FindType(state.generatedNamespace + ".ConsumerGeneratedRow");
                Type table = FindType(state.generatedNamespace + ".ConsumerGeneratedTable");
                Require(row != null && table != null, "Generated DTO/table were not compiled by the second Editor launch.");
                MethodInfo register = typeof(EditorProbe).GetMethod(nameof(RegisterGenerated), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(row, table);
                bool observed = false;
                DataTableImportProfiles.Register(state.id, () => new DecimalIdxCodec(1000), context =>
                {
                    register.Invoke(null, new object[] { context, (Action)(() => observed = true), false });
                });
                var validated = await DataTableImporter.RunAsync(settings);
                result.generatedDiagnostic = validated.Status + ": " + validated.Diagnostic;
                result.generatedValidated = validated.Status == DataTableImportStatus.Validated && !string.IsNullOrEmpty(validated.Fingerprint);
                result.generatedValidatorObserved = observed;
                Require(result.generatedValidated && observed, "Compiled generated validation: " + validated.Status + " " + validated.Diagnostic);

                FileStamp[] sources = Directory.GetFiles(settings.OutputFolder, "*.cs").Select(Stamp).ToArray();
                Require(sources.Length == 2, "Expected exactly the two owned generated C# sources.");
                DataTableImportProfiles.Unregister(state.id);
                DataTableImportProfiles.Register(state.id, () => new DecimalIdxCodec(1000), context =>
                {
                    register.Invoke(null, new object[] { context, (Action)(() => { }), true });
                });
                var rejected = await DataTableImporter.RunAsync(settings, true);
                result.rejectedDiagnostic = rejected.Status + ": " + rejected.Diagnostic;
                result.validatorFailurePreservesOwnedSources = rejected.Status == DataTableImportStatus.Failed && rejected.Diagnostic.Contains(Rejection) && Matches(sources);
                Require(result.validatorFailurePreservesOwnedSources, "A typed validator rejection must retain the prior owned source bytes: " + rejected.Diagnostic);
                result.requiresGeneratedValidationPhase = false;
                result.diagnostic = "Installed Editor importer preparation and compiled generated validation completed.";
                return result;
            }
            catch (Exception error)
            {
                throw new ProbeException(result, error);
            }
            finally
            {
                DataTableImportProfiles.Unregister(state.id);
                result.profileUnregistered = VerifyUnregistered(state.id);
                UnityEngine.Object.DestroyImmediate(settings);
                Cleanup(state);
                CheckProtection(state);
                Require(result.ownedAssetsCleaned && result.profileUnregistered, "Owned probe assets/profile were not cleaned.");
            }
        }

        private static void RegisterGenerated<TRow, TTable>(DataTableImportContext context, Action observed, bool reject)
            where TRow : class, IDataRow where TTable : CsvDataTable<TRow>
        {
            context.RegisterTable<TRow, TTable>("texts", () => (TTable)Activator.CreateInstance(typeof(TTable)));
            context.Manager.AddValidator(snapshot =>
            {
                TRow row = snapshot.Get<TRow>(1001);
                Require((string)typeof(TRow).GetProperty("Text").GetValue(row) == "hello", "Generated typed validator received incorrect data.");
                observed();
                if (reject)
                {
                    throw new InvalidDataException(Rejection);
                }
            });
        }

        private static void Configure(DataTableImportSettings settings, State state)
        {
            settings.InputFolder = state.folder + "/Input";
            settings.SchemaFolder = state.folder + "/Schemas";
            settings.OutputFolder = state.folder + "/Generated";
            settings.ValidationProfileId = state.id;
        }

        private static void WriteSchema(DataTableImportSettings settings, string mode, string ns, string row, string table)
        {
            string json = "{\"schemaVersion\":1,\"tableId\":\"texts\",\"input\":\"" + settings.InputFolder + "/texts.csv\",\"format\":\"csv\",\"mode\":\"" + mode
                + "\",\"namespace\":\"" + ns + "\",\"rowType\":\"" + row + "\",\"tableType\":\"" + table
                + "\",\"dataType\":1,\"columns\":[{\"name\":\"idx\",\"member\":\"Id\",\"type\":\"uint\",\"required\":true},{\"name\":\"text\",\"member\":\"Text\",\"type\":\"string\",\"required\":true}]}";
            File.WriteAllText(settings.SchemaFolder + "/texts.json", json);
        }

        private static bool VerifyUnregistered(string id)
        {
            // Public registration semantics reject duplicate IDs; reuse proves teardown removed our registration.
            DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context => { });
            DataTableImportProfiles.Unregister(id);
            return true;
        }

        private static Type FindType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
        }

        private static string StatePath()
        {
            string path = Environment.GetEnvironmentVariable("TPLAB_CONSUMER_IMPORTER_STATE");
            Require(!string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path), "TPLAB_CONSUMER_IMPORTER_STATE must be an absolute fresh result path.");
            return path;
        }

        private static FileStamp[] Capture(string folder)
        {
            return Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).Select(Stamp).ToArray();
        }

        private static FileStamp Stamp(string path)
        {
            return new FileStamp { path = Path.GetFullPath(path), hash = HashOrMissing(path) };
        }

        private static string HashOrMissing(string path)
        {
            if (!File.Exists(path))
            {
                return "missing";
            }
            using (var sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
            }
        }

        private static bool Matches(IEnumerable<FileStamp> files)
        {
            return files.All(file => HashOrMissing(file.path) == file.hash);
        }

        private static void CheckProtection(State state)
        {
            state.result.packageBytesUnchanged = Matches(state.package);
            state.result.baselineAssetsUnchanged = Matches(state.assets) && !File.Exists(DataTableImportSettings.ActivePath);
            state.result.buildSettingsUnchanged = HashOrMissing("ProjectSettings/EditorBuildSettings.asset") == state.buildSettings;
            Require(state.result.packageBytesUnchanged && state.result.baselineAssetsUnchanged && state.result.buildSettingsUnchanged, "Importer modified baseline assets, installed package bytes, or Build Settings.");
        }

        private static void ValidateOwner(State state)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Require(state != null && state.project == project && state.folder != null && state.folder.StartsWith(FolderPrefix, StringComparison.Ordinal)
                && state.folder.Length == FolderPrefix.Length + 32 && state.folder.Substring(FolderPrefix.Length).All(Uri.IsHexDigit), "Importer state does not own a folder in this consumer.");
            DataTableGeneratedFiles.AssetPath(state.folder, true);
            Require(File.Exists(state.folder + Marker) && File.ReadAllText(state.folder + Marker) == state.id, "Probe ownership marker is missing or changed.");
        }

        private static void Cleanup(State state)
        {
            if (!Directory.Exists(state.folder))
            {
                state.result.ownedAssetsCleaned = true;
                return;
            }
            ValidateOwner(state);
            Directory.Delete(DataTableGeneratedFiles.AssetPath(state.folder, true), true);
            string meta = DataTableGeneratedFiles.AssetPath(state.folder + ".meta", true);
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
            state.result.ownedAssetsCleaned = !Directory.Exists(state.folder) && !File.Exists(meta);
        }

        private static void Require(bool condition, string diagnostic)
        {
            if (!condition)
            {
                throw new InvalidDataException(diagnostic);
            }
        }
    }

    public sealed class EditorProbeRow : DataRow
    {
        [Name("text")]
        public string Text { get; set; }
    }

    public sealed class EditorProbeTable : CsvDataTable<EditorProbeRow>
    {
    }
}
