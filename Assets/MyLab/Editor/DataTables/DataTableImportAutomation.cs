using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MyLab.Core.Editor.DataTables
{
    /// <summary>Coalesces asset changes and runs outside import callbacks. Never creates settings automatically.</summary>
    [InitializeOnLoad]
    public static class DataTableImportAutomation
    {
        internal const string PendingKey = "MyLab.DataTables.PendingSettings";
        internal const string PendingAutomaticKey = "MyLab.DataTables.PendingAutomatic";
        private const string Key = "MyLab.DataTables.";
        private static bool _requested = true;
        private static bool _busy;
        private static int _revision;
        private static CancellationTokenSource _operation;
        private static DataTableImportSettings _manualSettings;
        private static bool _manualGenerate;
        private static double _nextCheck;

        /// <summary>Most recent outcome, persisted across domain reload for Inspector diagnostics.</summary>
        public static DataTableImportStatus LastStatus => (DataTableImportStatus)SessionState.GetInt(Key + "Status", (int)DataTableImportStatus.Disabled);
        /// <summary>Current diagnostic; no runtime data is retained.</summary>
        public static string LastDiagnostic => SessionState.GetString(Key + "Diagnostic", "Automation is disabled.");
        internal static bool CompilationFailed => SessionState.GetBool(Key + "CompileFailed", false);

        static DataTableImportAutomation()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += () => _operation?.Cancel();
            CompilationPipeline.compilationStarted += _ => SessionState.SetBool(Key + "CompileFailed", false);
            CompilationPipeline.assemblyCompilationFinished += (_, messages) =>
            {
                if (messages.Any(m => m.type == CompilerMessageType.Error))
                    SessionState.SetBool(Key + "CompileFailed", true);
            };
            CompilationPipeline.compilationFinished += _ => Queue();
        }

        /// <summary>Queues a manual operation in an idle Editor. Still performs mandatory shared validation.</summary>
        public static void RequestManual(DataTableImportSettings settings, bool generate)
        {
            _manualSettings = settings;
            _manualGenerate = generate;
            Queue();
        }

        /// <summary>Receives imported/deleted/moved paths. Generated outputs are ignored; reloads revalidate typed code.</summary>
        public static void Notify(string[] paths, bool didDomainReload = false)
        {
            var settings = AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath);
            if (paths.Any(p => p == DataTableImportSettings.ActivePath) || didDomainReload || settings != null && paths.Any(p =>
                !string.IsNullOrEmpty(settings.InputFolder) && DataTableImporter.Under(p, settings.InputFolder) ||
                !string.IsNullOrEmpty(settings.SchemaFolder) && DataTableImporter.Under(p, settings.SchemaFolder)))
                Queue();
        }

        private static void Queue()
        {
            ++_revision;
            _requested = true;
            _operation?.Cancel();
            SessionState.SetInt(Key + "Status", (int)DataTableImportStatus.AwaitingConfiguration);
            SessionState.SetString(Key + "Diagnostic", "Changes detected. Validation is pending.");
        }

        internal static void Invalidate() => Queue();

        private static void Update()
        {
            if (!_requested || _busy || EditorApplication.timeSinceStartup < _nextCheck)
                return;
            _nextCheck = EditorApplication.timeSinceStartup + 0.2;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            _requested = false;
            RunQueuedAsync().Forget();
        }

        private static async UniTask RunQueuedAsync()
        {
            _busy = true;
            int revision = _revision;
            _operation = new CancellationTokenSource();
            try
            {
                string pending = SessionState.GetString(PendingKey, "");
                bool manual = _manualSettings != null;
                var settings = manual ? _manualSettings : AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(string.IsNullOrEmpty(pending) ? DataTableImportSettings.ActivePath : pending);
                bool automatic = manual ? false : string.IsNullOrEmpty(pending) || SessionState.GetBool(PendingAutomaticKey, false);
                bool generate = manual ? _manualGenerate : string.IsNullOrEmpty(pending) && settings != null && settings.AutomationMode == DataTableAutomationMode.GenerateValidated;
                _manualSettings = null;
                SessionState.EraseString(PendingKey);
                var result = await DataTableImporter.RunAsync(settings, generate, automatic, _operation.Token);
                if (revision == _revision)
                {
                    SessionState.SetInt(Key + "Status", (int)result.Status);
                    SessionState.SetString(Key + "Diagnostic", result.Diagnostic ?? "");
                }
            }
            finally
            {
                _operation.Dispose();
                _operation = null;
                _busy = false;
            }
        }
    }

    internal sealed class DataTableAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom, bool didDomainReload)
            => DataTableImportAutomation.Notify(imported.Concat(deleted).Concat(moved).Concat(movedFrom).ToArray(), didDomainReload);
    }

    [CustomEditor(typeof(DataTableImportSettings))]
    internal sealed class DataTableImportSettingsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var settings = (DataTableImportSettings)target;
            EditorGUILayout.HelpBox(DataTableImportAutomation.LastStatus + ": " + DataTableImportAutomation.LastDiagnostic, MessageType.Info);
            if (AssetDatabase.GetAssetPath(settings) != DataTableImportSettings.ActivePath)
                EditorGUILayout.HelpBox("Automatic work uses only " + DataTableImportSettings.ActivePath + ". This asset supports manual commands.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Validate All Tables"))
                    DataTableImportAutomation.RequestManual(settings, false);
                if (GUILayout.Button("Generate and Validate"))
                    DataTableImportAutomation.RequestManual(settings, true);
            }
        }

        [MenuItem("Tools/MyLab/Data Tables/Create or Select Active Settings")]
        private static void CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DataTableImportSettings>(DataTableImportSettings.ActivePath);
            if (settings == null)
            {
                if (System.IO.File.Exists(DataTableImportSettings.ActivePath))
                    throw new System.IO.InvalidDataException("An unrelated asset occupies the settings path.");
                string parent = "Assets";
                foreach (string part in new[] { "Editor", "MyLab" })
                {
                    if (!AssetDatabase.IsValidFolder(parent + "/" + part))
                        AssetDatabase.CreateFolder(parent, part);
                    parent += "/" + part;
                }
                settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
                AssetDatabase.CreateAsset(settings, DataTableImportSettings.ActivePath);
            }
            Selection.activeObject = settings;
        }
    }
}
