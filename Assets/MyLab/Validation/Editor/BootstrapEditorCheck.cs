using System;
using System.IO;
using System.Linq;
using MyLab.Core.Editor.Bootstrap;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    /// <summary>Checks native build and Play rejection in the current Editor; owns only its temporary scene/output.</summary>
    public static class BootstrapEditorCheck
    {
        private const string Path = "Assets/MyLabBootstrapValidationProbe.unity";
        private const string Evidence = "doc/validation/bootstrap-system/native-editor.json";
        private static Scene _probe;
        private static Scene _previous;
        private static Result _result;
        private static string _evidence;
        private static SceneTransitionSettings _transitionSettings;
        private const string SettingsPath = "Assets/MyLabBootstrapValidationProbe.asset";

        [Serializable]
        private sealed class Result
        {
            public bool BuildRejected;
            public bool PlayRejected;
            public bool LiveInvalidRootDetected;
            public bool LiveInvalidModeDetected;
            public bool LiveInvalidDefinitionDetected;
            public bool FixtureRemoved;
            public string Error;
            public string BuildDiagnostic;
            public string BuildResult;
        }

        /// <summary>Runs an actual BuildPipeline failure and a pre-Play gate. Does not save user scenes or edit build settings.</summary>
        public static void Run(string evidencePath = Evidence, bool invalidSingle = false, bool invalidDefinition = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
                File.Exists(Path) || File.Exists(Path + ".meta") || File.Exists(SettingsPath) || File.Exists(SettingsPath + ".meta"))
                throw new InvalidOperationException("Requires idle Editor and unused probe path.");
            _evidence = System.IO.Path.GetFullPath(evidencePath);
            if (!_evidence.StartsWith(System.IO.Path.GetFullPath("doc/validation") + System.IO.Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) || !Directory.Exists(System.IO.Path.GetDirectoryName(_evidence)))
                throw new ArgumentException("Select an existing evidence directory under doc/validation.", nameof(evidencePath));
            _result = new Result();
            _previous = SceneManager.GetActiveScene();
            try
            {
                File.WriteAllText(Path, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
                AssetDatabase.ImportAsset(Path);
                _probe = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
                var go = new GameObject("InvalidBootstrapProbe");
                SceneManager.MoveGameObjectToScene(go, _probe);
                var root = invalidSingle || invalidDefinition ? go.AddComponent<SceneOwnedRoot>() : null;
                var bootstrap = go.AddComponent<BootstrapSystem>();
                bootstrap.Configure(root, "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity", false,
                    loadMode: invalidSingle ? LoadSceneMode.Single : LoadSceneMode.Additive);
                if (invalidDefinition)
                {
                    _transitionSettings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
                    _transitionSettings.Configure(new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, "",
                        MyLab.Core.ResourceManagement.SceneTarget.BuildScene("Assets/MyLab/Tests/Fixtures/BootstrapHub.unity"),
                        requiredConditionIds: new[] { "missing-required-policy" }));
                    AssetDatabase.CreateAsset(_transitionSettings, SettingsPath);
                    bootstrap.Configure(root, _transitionSettings, "entry", false);
                    EditorUtility.SetDirty(_transitionSettings);
                    AssetDatabase.SaveAssetIfDirty(_transitionSettings);
                }
                if (!EditorSceneManager.SaveScene(_probe, Path)) throw new Exception("Probe save failed.");
                _result.LiveInvalidRootDetected = BootstrapEditorValidation.ValidateEditorSetup(true)
                    .Any(error => error.Contains("SceneOwnedRoot"));
                _result.LiveInvalidModeDetected = BootstrapEditorValidation.ValidateEditorSetup(true)
                    .Any(error => error.Contains("Single requires"));
                _result.LiveInvalidDefinitionDetected = invalidDefinition &&
                    BootstrapEditorValidation.ValidateEditorSetup(true).Any(error => error.Contains("missing-required-policy"));
                string output = System.IO.Path.GetFullPath("Temp/MyLabBootstrapInvalidBuild/Probe.exe");
                if (Directory.Exists(System.IO.Path.GetDirectoryName(output)))
                    throw new Exception("Build output collision.");
                Application.LogCallback capture = (message, stack, type) =>
                {
                    if (message.Contains("Bootstrap validation failed")) _result.BuildDiagnostic += message + "\n";
                };
                Application.logMessageReceived += capture;
                try
                {
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { Path, "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity" },
                        locationPathName = output,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.Development
                    });
                    _result.BuildResult = report == null ? "No report" : report.summary.result.ToString();
                    _result.BuildDiagnostic += report == null ? "No report" : string.Join("\n", report.steps
                        .SelectMany(step => step.messages).Select(message => message.content));
                    _result.BuildRejected = _result.BuildDiagnostic.Contains("Bootstrap validation failed") &&
                        (report == null || report.summary.result != BuildResult.Succeeded) && !File.Exists(output);
                }
                catch (Exception exception)
                {
                    _result.BuildDiagnostic = exception.Message;
                    _result.BuildResult = "Thrown";
                    _result.BuildRejected = exception.Message.Contains("Bootstrap validation failed");
                }
                finally
                {
                    Application.logMessageReceived -= capture;
                    string directory = System.IO.Path.GetDirectoryName(output);
                    if (!directory.StartsWith(System.IO.Path.GetFullPath("Temp") + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Unsafe probe cleanup path.");
                    if (Directory.Exists(directory)) Directory.Delete(directory, true);
                }
                EditorApplication.isPlaying = true;
                EditorApplication.delayCall += FinishAfterPlayGate;
            }
            catch (Exception exception)
            {
                _result.Error = exception.ToString();
                Finish();
            }
        }

        private static void FinishAfterPlayGate()
        {
            _result.PlayRejected = !EditorApplication.isPlayingOrWillChangePlaymode;
            if (!_result.PlayRejected) EditorApplication.isPlaying = false;
            Finish();
        }

        private static void Finish()
        {
            EditorApplication.delayCall -= FinishAfterPlayGate;
            if (_probe.IsValid()) EditorSceneManager.CloseScene(_probe, true);
            if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
            AssetDatabase.DeleteAsset(Path);
            if (_transitionSettings != null) AssetDatabase.DeleteAsset(SettingsPath);
            _transitionSettings = null;
            _result.FixtureRemoved = !File.Exists(Path) && !File.Exists(Path + ".meta") &&
                !File.Exists(SettingsPath) && !File.Exists(SettingsPath + ".meta");
            File.WriteAllText(_evidence, JsonUtility.ToJson(_result, true));
            _probe = default;
        }
    }
}
