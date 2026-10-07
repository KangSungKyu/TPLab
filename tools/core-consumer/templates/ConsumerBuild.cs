using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPLabConsumer
{
    public static class ConsumerBuild
    {
        private const string ScenesFolder = "Assets/TPLabConsumer/Scenes";
        private const string BootstrapPath = ScenesFolder + "/Bootstrap.unity";
        private const string GamePath = ScenesFolder + "/Game.unity";
        private const string DerivedPath = ScenesFolder + "/Derived.unity";

        public static void Perform() => PerformAsync().Forget();

        private static async UniTaskVoid PerformAsync()
        {
            var report = new BuildReportFile();
            int exitCode = 1;
            try
            {
#if TPLAB_EDITOR_CONSUMER
                report.importer = await EditorProbe.ValidateGeneratedAsync();
#endif
#if TPLAB_ARTIFACT_CONSUMER
                report.installedPackages = PackageInfo.GetAllRegisteredPackages().Where(p => p.name.StartsWith("com.tplab.", StringComparison.Ordinal))
                    .Select(p => new InstalledPackage { name = p.name, version = p.version, resolvedPath = p.resolvedPath, source = p.source.ToString() }).ToArray();
#endif
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

#if TPLAB_ARTIFACT_CONSUMER
                if (Environment.GetEnvironmentVariable("TPLAB_CONSUMER_SAMPLES") == "1")
                {
                    VerifySamples();
                    report.samplesVerified = true;
                }
#endif
                string playerPath = Environment.GetEnvironmentVariable("TPLAB_CONSUMER_PLAYER_PATH");
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { BootstrapPath, GamePath, DerivedPath },
                    locationPathName = playerPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                var build = BuildPipeline.BuildPlayer(options);
                report.success = build.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && build.summary.totalErrors == 0;
                report.unityVersion = Application.unityVersion;
                report.target = build.summary.platform.ToString();
                report.backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString();
                report.playerPath = playerPath ?? "";
                report.result = build.summary.result.ToString();
                report.errors = (int)build.summary.totalErrors;
                report.warnings = (int)build.summary.totalWarnings;
#if TPLAB_ARTIFACT_CONSUMER
                if (report.success && report.samplesVerified) report.sampleBuilds = BuildSamples();
#endif
                WriteResult(report);
                exitCode = report.success ? 0 : 2;
            }
            catch (Exception exception)
            {
                report.success = false;
                report.unityVersion = Application.unityVersion;
                report.error = exception.ToString();
#if TPLAB_EDITOR_CONSUMER
                if (exception is EditorProbe.ProbeException probe) report.importer = probe.Result;
#endif
                WriteResult(report);
                Debug.LogException(exception);
            }
            EditorApplication.Exit(exitCode);
        }

#if TPLAB_ARTIFACT_CONSUMER
        private static Type SampleBuilder => Type.GetType("TPLab.Samples.SceneTransitions.Editor.SceneTransitionSampleBuilder, TPLab.SceneTransitionSamples.Editor", true);

        private static void VerifySamples()
        {
            Type.GetType("TPLab.Samples.CorePooling.CorePoolingSample, TPLab.CorePoolingSample", true).GetMethod("Run").Invoke(null, null);
            string file = "ProjectSettings/EditorBuildSettings.asset";
            byte[] baseline = File.ReadAllBytes(file);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            string builderPath = AssetDatabase.GUIDToAssetPath("3da5004585d94731a0f004f6211ad0e6");
            string root = builderPath.Substring(0, builderPath.Length - "/Editor/SceneTransitionSampleBuilder.cs".Length);
            string marker = root + "/sample-owner.txt";
            byte[] originalMarker = File.ReadAllBytes(marker);
            bool refused = false;
            try
            {
                File.WriteAllText(marker, "foreign owner collision");
                try { SampleBuilder.GetMethod("BuildSampleAssets").Invoke(null, null); }
                catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { refused = true; }
            }
            finally { File.WriteAllBytes(marker, originalMarker); }
            if (!refused || !baseline.SequenceEqual(File.ReadAllBytes(file)) || !SameSetup(setup, EditorSceneManager.GetSceneManagerSetup()))
                throw new InvalidOperationException("Sample ownership collision was not refused without changing the baseline.");
            SampleBuilder.GetMethod("BuildSampleAssets").Invoke(null, null);
            if (!baseline.SequenceEqual(File.ReadAllBytes(file)) || !SameSetup(setup, EditorSceneManager.GetSceneManagerSetup()))
                throw new InvalidOperationException("Explicit imported sample generation did not restore the baseline.");
            SampleBuilder.GetMethod("OpenSingle").Invoke(null, null);
            SampleBuilder.GetMethod("RestoreOriginal").Invoke(null, null);
            if (!baseline.SequenceEqual(File.ReadAllBytes(file)) || !SameSetup(setup, EditorSceneManager.GetSceneManagerSetup()))
                throw new InvalidOperationException("Imported sample open/restore changed the original setup.");
        }

        private static bool SameSetup(SceneSetup[] before, SceneSetup[] after) => before.Length == after.Length && before.Zip(after,
            (a, b) => a.path == b.path && a.isLoaded == b.isLoaded && a.isActive == b.isActive).All(value => value);

        private static SampleBuild[] BuildSamples()
        {
            return new[] { BuildSample(false), BuildSample(true) };
        }

        private static SampleBuild BuildSample(bool single)
        {
            string mode = single ? "single" : "additive";
            string folder = "Temp/GameScenesTrack/ScenePlayers/consumer-" + mode;
            string evidence = "doc/validation/scene-integration/consumer-" + mode + ".json";
            SampleBuilder.GetMethod("BuildWindowsMono").Invoke(null, new object[] { single, folder, evidence });
            return new SampleBuild { mode = mode, playerPath = Path.GetFullPath(folder + "/SceneTransitions.exe"), evidencePath = Path.GetFullPath(evidence) };
        }
#endif

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
            string path = Environment.GetEnvironmentVariable("TPLAB_CONSUMER_EDITOR_RESULT");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("TPLAB_CONSUMER_EDITOR_RESULT is required.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        [Serializable]
        private sealed class InstalledPackage
        {
            public string name, version, resolvedPath, source;
        }

        [Serializable]
        private sealed class SampleBuild
        {
            public string mode, playerPath, evidencePath;
        }

        [Serializable]
        private sealed class BuildReportFile
        {
            public bool success;
#if TPLAB_EDITOR_CONSUMER
            public EditorProbe.ProbeResult importer;
#endif
            public InstalledPackage[] installedPackages;
            public bool samplesVerified;
            public SampleBuild[] sampleBuilds;
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
