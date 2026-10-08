using System;
using System.IO;
using UIConsumer;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIConsumer.Editor
{
    public static class UIConsumerBuild
    {
        public static void Perform()
        {
            var report = new Report
            {
                runId = Environment.GetEnvironmentVariable("TPLAB_UI_RUN_ID"),
                sourceRevision = Environment.GetEnvironmentVariable("TPLAB_UI_SOURCE_REVISION"),
                projectPath = Environment.GetEnvironmentVariable("TPLAB_UI_PROJECT"),
                playerPath = Environment.GetEnvironmentVariable("TPLAB_UI_PLAYER_PATH"),
                unityVersion = Application.unityVersion, target = "StandaloneWindows64", backend = "Mono2x"
            };
            int exit = 1;
            try
            {
                if (Application.unityVersion != "6000.3.18f1" || Path.GetFullPath(".") != report.projectPath)
                    throw new InvalidOperationException("Exact isolated consumer project/Unity version required.");
                Directory.CreateDirectory("Assets/UIConsumer/Scenes");
                const string scenePath = "Assets/UIConsumer/Scenes/Consumer.unity";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("UI Consumer Driver").AddComponent<UIConsumerSmoke>();
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Consumer scene save failed.");
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                report.backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString();
                PlayerSettings.runInBackground = true;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath }, locationPathName = report.playerPath,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                report.target = build.summary.platform.ToString();
                report.errors = (int)build.summary.totalErrors;
                report.warnings = (int)build.summary.totalWarnings;
                report.success = build.summary.result == BuildResult.Succeeded && report.errors == 0;
                report.result = build.summary.result.ToString();
                exit = report.success ? 0 : 2;
                string entry = Environment.GetEnvironmentVariable("TPLAB_UI_SAMPLE_ENTRY");
                if (report.success && !string.IsNullOrWhiteSpace(entry))
                {
                    // Only this fresh consumer's build list is changed. Samples are copied from the exact allowlist.
                    const string folder = "Assets/TPLab/Samples/UI/Scenes/";
                    string[] paths = { folder + entry + ".unity", folder + "UIContextHub.unity", folder + "UIContextMain.unity", folder + "UIContextArea.unity", folder + "UIContextNested.unity" };
                    foreach (string path in paths) if (!File.Exists(path)) throw new FileNotFoundException(path);
                    var settings = new EditorBuildSettingsScene[paths.Length];
                    for (int i = 0; i < paths.Length; ++i) settings[i] = new EditorBuildSettingsScene(paths[i], true);
                    EditorBuildSettings.scenes = settings;
                    string samplePath = Environment.GetEnvironmentVariable("TPLAB_UI_SAMPLE_PLAYER_PATH");
                    var sampleBuild = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = paths, locationPathName = samplePath, target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.Development
                    });
                    report.samplePlayerPath = samplePath;
                    report.sampleSuccess = sampleBuild.summary.result == BuildResult.Succeeded && sampleBuild.summary.totalErrors == 0;
                    report.sampleErrors = (int)sampleBuild.summary.totalErrors;
                    if (!report.sampleSuccess) { report.success = false; exit = 3; }
                }
            }
            catch (Exception error) { report.error = error.ToString(); report.success = false; Debug.LogException(error); }
            finally
            {
                string path = Environment.GetEnvironmentVariable("TPLAB_UI_EDITOR_RESULT");
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || File.Exists(path))
                    throw new InvalidOperationException("Fresh absolute build evidence path required.");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                EditorApplication.Exit(exit);
            }
        }
        [Serializable] private sealed class Report
        {
            public bool success, sampleSuccess;
            public int errors, warnings, sampleErrors;
            public string runId, sourceRevision, projectPath, unityVersion, backend, target, playerPath, result, error, samplePlayerPath;
        }
    }
}
