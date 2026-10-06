using System;
using System.IO;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLabConsumer
{
    public static class ConsumerBuild
    {
        private const string ScenesFolder = "Assets/MyLabConsumer/Scenes";
        private const string BootstrapPath = ScenesFolder + "/Bootstrap.unity";
        private const string GamePath = ScenesFolder + "/Game.unity";
        private const string DerivedPath = ScenesFolder + "/Derived.unity";

        public static void Perform()
        {
            int exitCode = 1;
            try
            {
                Directory.CreateDirectory(ScenesFolder);
                CreateRootScene(GamePath);
                CreateRootScene(DerivedPath);
                CreateBootstrapScene();
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(BootstrapPath, true),
                    new EditorBuildSettingsScene(GamePath, true),
                    new EditorBuildSettingsScene(DerivedPath, true)
                };

                string playerPath = Environment.GetEnvironmentVariable("MYLAB_CONSUMER_PLAYER_PATH");
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { BootstrapPath, GamePath, DerivedPath },
                    locationPathName = playerPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                var build = BuildPipeline.BuildPlayer(options);
                var report = new BuildReportFile
                {
                    success = build.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && build.summary.totalErrors == 0,
                    unityVersion = Application.unityVersion,
                    target = build.summary.platform.ToString(),
                    backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString(),
                    playerPath = playerPath ?? "",
                    result = build.summary.result.ToString(),
                    errors = (int)build.summary.totalErrors,
                    warnings = (int)build.summary.totalWarnings
                };
                WriteResult(report);
                exitCode = report.success ? 0 : 2;
            }
            catch (Exception exception)
            {
                WriteResult(new BuildReportFile { success = false, unityVersion = Application.unityVersion, error = exception.ToString() });
                Debug.LogException(exception);
            }
            EditorApplication.Exit(exitCode);
        }

        private static void CreateRootScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("ConsumerSceneRoot");
            var root = host.AddComponent<SceneOwnedRoot>();
            root.Configure(Array.Empty<SceneRootInstaller>(), false);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save consumer scene: " + path);
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("ConsumerBootstrapRoot");
            var root = host.AddComponent<SceneOwnedRoot>();
            root.Configure(Array.Empty<SceneRootInstaller>(), false);
            var bootstrap = host.AddComponent<BootstrapSystem>();
            bootstrap.Configure(root, SceneTarget.BuildScene(GamePath), false);
            host.AddComponent<ConsumerSmoke>().ConfigureForBuild(DerivedPath);
            if (!EditorSceneManager.SaveScene(scene, BootstrapPath)) throw new InvalidOperationException("Could not save consumer Bootstrap scene.");
        }

        private static void WriteResult(BuildReportFile report)
        {
            string path = Environment.GetEnvironmentVariable("MYLAB_CONSUMER_EDITOR_RESULT");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("MYLAB_CONSUMER_EDITOR_RESULT is required.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        [Serializable]
        private sealed class BuildReportFile
        {
            public bool success;
            public string unityVersion;
            public string target;
            public string backend;
            public string playerPath;
            public string result;
            public int errors;
            public int warnings;
            public string error;
        }
    }
}
