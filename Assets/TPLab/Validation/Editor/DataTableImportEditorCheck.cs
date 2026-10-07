using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TPLab.Core.DataTables;
using TPLab.Core.Editor.DataTables;
using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Tests
{
    /// <summary>Exercises actual asset callbacks and generated runtime compilation in the current Editor; restores owned fixtures.</summary>
    [InitializeOnLoad]
    public static class DataTableImportEditorCheck
    {
        private const string Key = "TPLab.ImportProbe.";
        private const string Root = "Assets/TPLabImporterProbe";
        private const string Csv = Root + "/Input/texts.csv";
        private const string Moved = Root + "/Input/moved.csv";
        private const string Row = Root + "/Generated/ProbeRow.g.cs";
        private const string Schema = Root + "/Schemas/texts.json";
        private const string Profile = "TPLab.ImporterNativeProbe";

        [Serializable]
        private sealed class Result
        {
            public bool Success; public int Checks; public int TypedValidations; public bool FixturesRemoved; public string Error; public string[] Observations;
        }

        static DataTableImportEditorCheck()
        {
            EditorApplication.update += Update;
            if (SessionState.GetBool(Key + "Active", false))
                RegisterProfile();
        }

        /// <summary>Starts a bounded native Editor check. Refuses preexisting fixture/settings paths and performs no scene operations.</summary>
        public static void Run()
        {
            if (SessionState.GetBool(Key + "Active", false) || EditorApplication.isPlayingOrWillChangePlaymode ||
                Directory.Exists(Root) || File.Exists(Root + ".meta") || File.Exists(DataTableImportSettings.ActivePath) || File.Exists(DataTableImportSettings.ActivePath + ".meta"))
                throw new InvalidOperationException("Probe requires an idle Editor and unused fixture/settings paths.");
            SessionState.SetBool(Key + "Active", true);
            SessionState.SetInt(Key + "Phase", 0);
            SessionState.SetInt(Key + "Count", 0);
            SessionState.SetString(Key + "Observations", "");
            SessionState.SetString(Key + "Start", EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
            SessionState.SetBool(Key + "MadeEditor", !AssetDatabase.IsValidFolder("Assets/Editor"));
            SessionState.SetBool(Key + "MadeTPLab", !AssetDatabase.IsValidFolder("Assets/Editor/TPLab"));
            try
            {
                Directory.CreateDirectory(Root + "/Input");
                Directory.CreateDirectory(Root + "/Schemas");
                File.WriteAllText(Csv, "idx,text\n1001,initial");
                File.WriteAllText(Schema, "{\"schemaVersion\":1,\"tableId\":\"texts\",\"input\":\"" + Csv + "\",\"format\":\"csv\",\"mode\":\"generated\",\"namespace\":\"TPLab.ImportProbe\",\"rowType\":\"ProbeRow\",\"tableType\":\"ProbeTable\",\"dataType\":1,\"columns\":[{\"name\":\"idx\",\"member\":\"Id\",\"type\":\"uint\",\"required\":true},{\"name\":\"text\",\"member\":\"Text\",\"type\":\"string\",\"required\":true}]}");
                AssetDatabase.Refresh();
                SessionState.SetString(Key + "RootGuid", AssetDatabase.AssetPathToGUID(Root));
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                    AssetDatabase.CreateFolder("Assets", "Editor");
                if (!AssetDatabase.IsValidFolder("Assets/Editor/TPLab"))
                    AssetDatabase.CreateFolder("Assets/Editor", "TPLab");
                var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
                settings.InputFolder = Root + "/Input";
                settings.SchemaFolder = Root + "/Schemas";
                settings.OutputFolder = Root + "/Generated";
                settings.DefaultNamespace = "TPLab.ImportProbe";
                settings.ValidationProfileId = Profile;
                settings.AutomationMode = DataTableAutomationMode.GenerateValidated;
                RegisterProfile();
                AssetDatabase.CreateAsset(settings, DataTableImportSettings.ActivePath);
                SessionState.SetString(Key + "SettingsGuid", AssetDatabase.AssetPathToGUID(DataTableImportSettings.ActivePath));
                AssetDatabase.ImportAsset(DataTableImportSettings.ActivePath);
            }
            catch (Exception error) { Finish(error.Message); }
        }

        private static void RegisterProfile()
        {
            DataTableImportProfiles.Unregister(Profile);
            DataTableImportProfiles.Register(Profile, () => new DecimalIdxCodec(1000), context =>
            {
                // Test-only reflection bridges a type that does not exist until the generated source compiles.
                Type row = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TPLab.ImportProbe.ProbeRow", false)).FirstOrDefault(t => t != null);
                Type table = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TPLab.ImportProbe.ProbeTable", false)).FirstOrDefault(t => t != null);
                if (row != null && table != null)
                    typeof(DataTableImportEditorCheck).GetMethod(nameof(RegisterTyped), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(row, table).Invoke(null, new object[] { context });
            });
        }

        private static void RegisterTyped<TRow, TTable>(DataTableImportContext context)
            where TRow : class, IDataRow where TTable : CsvDataTable<TRow>, new()
        {
            context.RegisterTable<TRow, TTable>("texts", () => new TTable());
            context.Manager.AddValidator(snapshot =>
            {
                var row = snapshot.Get<TRow>(1001);
                Check(row != null && typeof(TRow).GetProperty("Text").GetValue(row) != null, "Generated typed fields and generic lookup");
                SessionState.SetInt(Key + "Count", SessionState.GetInt(Key + "Count", 0) + 1);
            });
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Key + "Active", false) || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            try
            {
                if (EditorApplication.timeSinceStartup - double.Parse(SessionState.GetString(Key + "Start", "0"), CultureInfo.InvariantCulture) > 240)
                    throw new TimeoutException("Native importer check timed out: " + DataTableImportAutomation.LastDiagnostic);
                var status = DataTableImportAutomation.LastStatus;
                int count = SessionState.GetInt(Key + "Count", 0);
                int phase = SessionState.GetInt(Key + "Phase", 0);
                if (phase == 0 && status == DataTableImportStatus.Failed)
                    throw new InvalidOperationException(DataTableImportAutomation.LastDiagnostic);
                if (phase == 0 && status == DataTableImportStatus.Validated)
                {
                    Check(File.Exists(Row), "Automatic generation, domain reload and full typed validation");
                    SessionState.SetString(Key + "RowTime", File.GetLastWriteTimeUtc(Row).Ticks.ToString());
                    SessionState.SetString(Key + "RowGuid", AssetDatabase.AssetPathToGUID(Row));
                    SessionState.SetString(Key + "CsvGuid", AssetDatabase.AssetPathToGUID(Csv));
                    Advance(1, count);
                    Change(Csv, "text,idx\nchanged,1001");
                }
                else if (phase == 1 && status == DataTableImportStatus.Validated && count > SessionState.GetInt(Key + "Before", 0))
                {
                    Check(File.GetLastWriteTimeUtc(Row).Ticks.ToString() == SessionState.GetString(Key + "RowTime", ""), "Data-only/header-order update does not rewrite C#");
                    Advance(2, count);
                    Change(Csv, "idx,text\n1001,a\n1001,b");
                }
                else if (phase == 2 && status == DataTableImportStatus.Failed)
                {
                    Check(File.Exists(Row), "Duplicate PK is rejected before source mutation");
                    Advance(3, count);
                    Check(string.IsNullOrEmpty(AssetDatabase.MoveAsset(Csv, Moved)), "CSV asset moved");
                    Change(Moved, "idx,text\n1001,moved");
                }
                else if (phase == 3 && status == DataTableImportStatus.Validated && count > SessionState.GetInt(Key + "Before", 0))
                {
                    Check(AssetDatabase.AssetPathToGUID(Moved) == SessionState.GetString(Key + "CsvGuid", ""), "Moved input resolves by retained GUID");
                    Advance(4, count);
                    AssetDatabase.DeleteAsset(Moved);
                }
                else if (phase == 4 && status == DataTableImportStatus.Failed)
                {
                    Check(File.Exists(Row), "Deleted input is diagnosed and generated sources retained");
                    Advance(5, count);
                    Change(Csv, "idx,text\n1001,restored");
                    var settings = AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath);
                    settings.AutomationMode = DataTableAutomationMode.ValidateOnly;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssetIfDirty(settings);
                }
                else if (phase == 5 && status == DataTableImportStatus.Validated && count > SessionState.GetInt(Key + "Before", 0))
                {
                    Check(AssetDatabase.AssetPathToGUID(Row) == SessionState.GetString(Key + "RowGuid", ""), "ValidateOnly and reimport preserve generated GUID");
                    Advance(6, count);
                    var settings = AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath);
                    settings.AutomationMode = DataTableAutomationMode.Disabled;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssetIfDirty(settings);
                }
                else if (phase == 6 && status == DataTableImportStatus.Disabled)
                {
                    Advance(7, count);
                    SessionState.SetString(Key + "OffTime", EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
                    Change(Csv, "idx,text\n1001,disabled");
                }
                else if (phase == 7 && EditorApplication.timeSinceStartup - double.Parse(SessionState.GetString(Key + "OffTime", "0"), CultureInfo.InvariantCulture) > 1)
                {
                    Check(count == SessionState.GetInt(Key + "Before", 0) && status == DataTableImportStatus.Disabled, "Disabled mode performs no typed validation on CSV changes");
                    Advance(8, count);
                    DataTableImportAutomation.RequestManual(AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath), false);
                }
                else if (phase == 8 && status == DataTableImportStatus.Validated && count > SessionState.GetInt(Key + "Before", 0))
                {
                    Check(count < 25, "Manual validation still works while automation is Disabled; callbacks remain bounded");
                    Advance(9, count);
                    Change(Root + "/Generated/ExpectedCompileFailure.cs", "#error TPLabImporterExpectedCompileFailure\n");
                }
                else if (phase == 9 && !EditorApplication.isCompiling && File.Exists(Root + "/Generated/ExpectedCompileFailure.cs"))
                {
                    if (status != DataTableImportStatus.Failed)
                    {
                        DataTableImportAutomation.RequestManual(AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath), false);
                        return;
                    }
                    Check(DataTableImportAutomation.LastDiagnostic.Contains("compilation") && count == SessionState.GetInt(Key + "Before", 0), "Compilation errors block stale typed validation");
                    Advance(10, count);
                    AssetDatabase.DeleteAsset(Root + "/Generated/ExpectedCompileFailure.cs");
                    AssetDatabase.Refresh();
                }
                else if (phase == 10 && status == DataTableImportStatus.Disabled)
                {
                    Advance(11, count);
                    DataTableImportAutomation.RequestManual(AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath), false);
                }
                else if (phase == 11 && status == DataTableImportStatus.Validated && count > SessionState.GetInt(Key + "Before", 0))
                {
                    Check(true, "Typed validation resumes after fixing compilation errors");
                    Finish(null);
                }
            }
            catch (Exception error) { Finish(error.Message); }
        }

        private static void Advance(int phase, int count)
        {
            SessionState.SetInt(Key + "Phase", phase);
            SessionState.SetInt(Key + "Before", count);
        }
        private static void Change(string path, string text)
        {
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
        }
        private static void Check(bool condition, string observation)
        {
            if (!condition)
                throw new InvalidOperationException(observation);
            if (!observation.StartsWith("Generated typed", StringComparison.Ordinal))
                SessionState.SetString(Key + "Observations", SessionState.GetString(Key + "Observations", "") + observation + "\n");
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "Active", false);
            DataTableImportProfiles.Unregister(Profile);
            bool removed = false;
            try
            {
                if (AssetDatabase.AssetPathToGUID(DataTableImportSettings.ActivePath) == SessionState.GetString(Key + "SettingsGuid", "missing"))
                    AssetDatabase.DeleteAsset(DataTableImportSettings.ActivePath);
                if (AssetDatabase.AssetPathToGUID(Root) == SessionState.GetString(Key + "RootGuid", "missing"))
                    AssetDatabase.DeleteAsset(Root);
                foreach (string folder in new[] { "Assets/Editor/TPLab", "Assets/Editor" })
                {
                    bool created = SessionState.GetBool(Key + (folder.EndsWith("TPLab", StringComparison.Ordinal) ? "MadeTPLab" : "MadeEditor"), false);
                    if (created && Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0)
                        AssetDatabase.DeleteAsset(folder);
                }
                removed = !Directory.Exists(Root) && !File.Exists(DataTableImportSettings.ActivePath);
            }
            catch (Exception cleanup) { error = (error ?? "") + " Cleanup: " + cleanup.Message; }
            var observations = SessionState.GetString(Key + "Observations", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Directory.CreateDirectory("doc/validation/data-table-importer");
            File.WriteAllText("doc/validation/data-table-importer/native-editor.json", JsonUtility.ToJson(new Result
            {
                Success = error == null && removed,
                Checks = observations.Length,
                TypedValidations = SessionState.GetInt(Key + "Count", 0),
                FixturesRemoved = removed,
                Error = error,
                Observations = observations
            }, true));
        }
    }
}
