using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TPLab.Samples.SceneTransitions.Editor
{
    /// <summary>Explicit sample authoring and temporary Editor/build ownership; never saves existing user scenes.</summary>
    [InitializeOnLoad]
    public static class SceneTransitionSampleBuilder
    {
        private static string Root => SceneTransitionSamplePaths.Root;
        private static string Marker => Root + "/sample-owner.txt";
        private const string Ownership = "TPLab SceneTransitions sample v1";
        private const string BuildFile = "ProjectSettings/EditorBuildSettings.asset";
        private const string SessionKey = "TPLab.SceneTransitionSample.Baseline";
        private static readonly string[] Games = { "Hub", "Main", "Area", "Nested" };
        private static readonly string[] Commands = { "Hub / Main", "Add Area", "Add Nested", "Nested self exit", "Area self exit", "Parent removes Area", "System modal", "Toggle policy", "Fail next prepare" };

        [Serializable]
        private sealed class BuildSceneState
        {
            public string path;
            public bool enabled;
        }
        [Serializable]
        private sealed class Baseline
        {
            public SceneSetup[] scenes;
            public BuildSceneState[] build;
            public string startScene;
        }
        [Serializable]
        private sealed class BuildEvidence
        {
            public bool success;
            public string unityVersion;
            public string mode;
            public string output;
            public string result;
            public int errors, warnings;
            public bool backendRestored, sceneSetupRestored, buildSettingsRestored, buildSettingsBytesRestored, startSceneRestored;
            public string error = "";
        }

        static SceneTransitionSampleBuilder()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(SessionState.GetString(SessionKey, "")))
                {
                    EditorApplication.update -= RestoreOnNextUpdate;
                    EditorApplication.update += RestoreOnNextUpdate;
                }
            };
        }

        private static void RestoreOnNextUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) return;
            EditorApplication.update -= RestoreOnNextUpdate;
            RestoreOriginal();
        }

        /// <summary>Explicitly creates/replaces owned sample assets only; requires an idle Editor with clean saved scenes and restores the original setup.</summary>
        [MenuItem("TPLab/Scene Transitions/Build Sample Assets")]
        public static void BuildSampleAssets()
        {
            RequireIdleAndClean();
            string[] planned = SceneTransitionSamplePaths.BuildScenes(false).Concat(new[] { SceneTransitionSamplePaths.BootstrapSingle, SettingsPath(true), SettingsPath(false) }).ToArray();
            bool owned = File.Exists(Marker) && File.ReadAllText(Marker).Trim() == Ownership;
            if (!owned && (File.Exists(Marker) || planned.Any(path => File.Exists(path) || File.Exists(path + ".meta")))) throw new InvalidOperationException("Sample asset collision: only the owned builder may replace these paths.");
            foreach (string path in planned)
            {
                if (Directory.Exists(path)) throw new InvalidOperationException("Sample path is a directory: " + path);
                if (!File.Exists(path)) continue;
                Type expectedType = path.EndsWith(".unity", StringComparison.Ordinal) ? typeof(SceneAsset) : typeof(SceneTransitionSettings);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null || asset.GetType() != expectedType) throw new InvalidOperationException("Sample asset type collision: " + path);
            }
            var baseline = Capture();
            byte[] raw = File.ReadAllBytes(BuildFile);
            try
            {
                Folder(Root + "/Scenes");
                Folder(Root + "/Settings");
                if (!owned) File.WriteAllText(Marker, Ownership);
                foreach (string name in Games) CreateGame(name);
                CreateSettings(false);
                CreateBootstrap(false);
                CreateSettings(true);
                CreateBootstrap(true);
                AssetDatabase.ImportAsset(Marker, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                Restore(baseline, raw);
            }
        }

        /// <summary>Temporarily opens the saved Additive sample and owns restoration after Play stops; rejects dirty scenes or missing sample assets.</summary>
        [MenuItem("TPLab/Scene Transitions/Open Additive")]
        public static void OpenAdditive() => Open(false);
        /// <summary>Temporarily opens the saved Single sample, selecting only its Bootstrap in Build settings; rejects dirty scenes or missing sample assets.</summary>
        [MenuItem("TPLab/Scene Transitions/Open Single")]
        public static void OpenSingle() => Open(true);
        private static void Open(bool single)
        {
            RequireIdleAndClean();
            RequireAssets(single);
            if (string.IsNullOrEmpty(SessionState.GetString(SessionKey, "")))
            {
                string directory = Path.GetFullPath("Temp/GameScenesTrack/Sample6/" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "baseline.json"), JsonUtility.ToJson(Capture(), true));
                File.Copy(BuildFile, Path.Combine(directory, "EditorBuildSettings.asset"));
                SessionState.SetString(SessionKey, directory);
            }
            try
            {
                EditorSceneManager.playModeStartScene = null;
                EditorBuildSettings.scenes = SceneTransitionSamplePaths.BuildScenes(single).Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
                EditorSceneManager.OpenScene(SceneTransitionSamplePaths.Bootstrap(single), OpenSceneMode.Single);
            }
            catch
            {
                RestoreOriginal();
                throw;
            }
        }

        /// <summary>Restores the session-owned baseline and exact original Build bytes after Play; leaves the backup for verification.</summary>
        [MenuItem("TPLab/Scene Transitions/Restore Original Setup")]
        public static void RestoreOriginal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before restoring the original setup.");
            EditorApplication.update -= RestoreOnNextUpdate;
            string directory = SessionState.GetString(SessionKey, "");
            if (string.IsNullOrEmpty(directory)) return;
            var baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(directory, "baseline.json")));
            Restore(baseline, File.ReadAllBytes(Path.Combine(directory, "EditorBuildSettings.asset")));
            SessionState.EraseString(SessionKey);
            // Keep this owned backup as evidence/recovery material; parent cleans it after byte verification.
        }

        /// <summary>Runs the loaded sample's real smoke, preserving the Play result until the caller stops Play.</summary>
        public static async UniTask<SceneSampleSmokeResult> RunSmokeAsync(string absoluteResultPath)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play using Open Additive/Open Single first.");
            var controller = UnityEngine.Object.FindFirstObjectByType<SceneTransitionSampleController>();
            if (controller == null) throw new InvalidOperationException("The active Play session is not the scene transition sample.");
            return await controller.RunSmokeAsync(absoluteResultPath);
        }

        /// <summary>Builds actual selected sample scenes as Windows x64 Mono into a fresh owned Temp directory; never launches it.</summary>
        public static void BuildWindowsMono(bool single, string outputDirectory, string evidencePath)
        {
            RequireIdleAndClean();
            RequireAssets(single);
            string output = Path.GetFullPath(outputDirectory);
            string allowed = Path.GetFullPath("Temp/GameScenesTrack/ScenePlayers") + Path.DirectorySeparatorChar;
            if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output))
                throw new ArgumentException("Use an unused output directory below Temp/GameScenesTrack/ScenePlayers.");
            string evidence = Path.GetFullPath(evidencePath);
            string evidenceRoot = Path.GetFullPath("doc/validation/scene-integration") + Path.DirectorySeparatorChar;
            string inputEvidenceRoot = Path.GetFullPath("doc/validation/input-system") + Path.DirectorySeparatorChar;
            string loadingEvidenceRoot = Path.GetFullPath("doc/validation/scene-loading") + Path.DirectorySeparatorChar;
            if ((!evidence.StartsWith(evidenceRoot, StringComparison.OrdinalIgnoreCase) &&
                !evidence.StartsWith(inputEvidenceRoot, StringComparison.OrdinalIgnoreCase) &&
                !evidence.StartsWith(loadingEvidenceRoot, StringComparison.OrdinalIgnoreCase)) ||
                !evidence.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || File.Exists(evidence) || Directory.Exists(evidence))
                throw new ArgumentException("Use a fresh JSON evidence path below scene-integration, input-system or scene-loading validation.");
            var baseline = Capture();
            byte[] raw = File.ReadAllBytes(BuildFile);
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var result = new BuildEvidence
            {
                mode = single ? "Single" : "Additive",
                unityVersion = Application.unityVersion,
                output = output
            };
            try
            {
                Directory.CreateDirectory(output);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = SceneTransitionSamplePaths.BuildScenes(single),
                    locationPathName = Path.Combine(output, "SceneTransitions.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                result.result = report.summary.result.ToString();
                result.errors = (int)report.summary.totalErrors;
                result.warnings = (int)report.summary.totalWarnings;
                result.success = report.summary.result == BuildResult.Succeeded && report.summary.totalErrors == 0;
            }
            catch (Exception exception)
            {
                result.error = exception.ToString();
            }
            finally
            {
                try
                {
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                    Restore(baseline, raw);
                    result.backendRestored = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == backend;
                    result.sceneSetupRestored = SameSetup(baseline.scenes, EditorSceneManager.GetSceneManagerSetup());
                    result.buildSettingsRestored = baseline.build.Length == EditorBuildSettings.scenes.Length && baseline.build.Zip(EditorBuildSettings.scenes,
                        (before, after) => before.path == after.path && before.enabled == after.enabled).All(value => value);
                    result.buildSettingsBytesRestored = raw.SequenceEqual(File.ReadAllBytes(BuildFile));
                    result.startSceneRestored = baseline.startScene == CurrentStartScene();
                    result.success &= result.backendRestored && result.sceneSetupRestored && result.buildSettingsRestored && result.buildSettingsBytesRestored && result.startSceneRestored;
                }
                catch (Exception exception)
                {
                    result.success = false;
                    result.error += "\nRestore: " + exception;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(evidence));
                File.WriteAllText(evidence, JsonUtility.ToJson(result, true));
            }
            if (!result.success) throw new InvalidOperationException("Windows Mono sample build failed; see " + evidence);
        }

        private static SceneTransitionSettings CreateSettings(bool single)
        {
            string path = SettingsPath(single);
            var settings = AssetDatabase.LoadAssetAtPath<SceneTransitionSettings>(path);
            if (settings == null)
            {
                if (File.Exists(path)) throw new InvalidOperationException("Settings type collision at " + path);
                settings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            LoadSceneMode mode = single ? LoadSceneMode.Single : LoadSceneMode.Additive;
            string hub = SceneTransitionSamplePaths.Hub, main = SceneTransitionSamplePaths.Main, area = SceneTransitionSamplePaths.Area, nested = SceneTransitionSamplePaths.Nested;
            settings.Configure(
                Definition("entry", SceneTransitionKind.FirstEntry, "", hub, mode),
                Definition("to-main", SceneTransitionKind.ReplacePrimary, hub, main, mode),
                Definition("to-hub", SceneTransitionKind.ReplacePrimary, main, hub, mode),
                Definition("area-hub", SceneTransitionKind.AddDerived, hub, area),
                Definition("area-main", SceneTransitionKind.AddDerived, main, area),
                Definition("nested", SceneTransitionKind.AddDerived, area, nested),
                Definition("nested-self", SceneTransitionKind.RemoveDerived, nested, nested),
                Definition("area-self", SceneTransitionKind.RemoveDerived, area, area),
                Definition("area-parent-hub", SceneTransitionKind.RemoveDerived, hub, area),
                Definition("area-parent-main", SceneTransitionKind.RemoveDerived, main, area));
            settings.CreateSnapshot();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            return settings;
        }
        private static SceneTransitionDefinition Definition(string id, SceneTransitionKind kind, string source, string target, LoadSceneMode mode = LoadSceneMode.Additive) =>
            new SceneTransitionDefinition(id, kind, source, SceneTarget.BuildScene(target), mode, priority: kind == SceneTransitionKind.AddDerived ? 10 : 0, requiredConditionIds: new[] { "sample-policy" });
        private static string SettingsPath(bool single) => Root + "/Settings/" + (single ? "Single" : "Additive") + ".asset";

        private static void CreateGame(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject(name + "Root");
            host.SetActive(false);
            var consumer = host.AddComponent<SampleSceneConsumer>();
            var installer = host.AddComponent<SampleSceneInstaller>();
            installer.Configure(consumer);
            var root = host.AddComponent<SceneOwnedRoot>();
            root.Configure(new SceneRootInstaller[] { installer });
            host.AddComponent<SampleTransitionCondition>().Configure(null, consumer);
            var canvas = Canvas(host.transform, name + "Screen", 0);
            Color color = name == "Main" ? new Color(.12f, .2f, .27f) : new Color(.12f, .16f, .21f);
            var panel = Image(canvas.transform, "Background", color);
            Stretch(panel.rectTransform);
            var label = Label(panel.transform, "SceneLabel", name, 32);
            Rect(label.rectTransform, new Vector2(.3f, .15f), new Vector2(.9f, .8f));
            consumer.ConfigureLabel(label);
            host.SetActive(true);
            if (!EditorSceneManager.SaveScene(scene, Root + "/Scenes/" + name + ".unity")) throw new IOException("Could not save owned game scene.");
        }

        private static void CreateBootstrap(bool single)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the previous asset wrapper; resolve the saved settings after this boundary.
            var settings = AssetDatabase.LoadAssetAtPath<SceneTransitionSettings>(SettingsPath(single));
            if (settings == null) throw new InvalidOperationException("Saved sample settings are required after creating the Bootstrap scene.");
            var host = new GameObject("CommonSceneRoot");
            host.SetActive(false);
            MonoBehaviour root;
            if (single)
            {
                var singleton = host.AddComponent<SingletonSceneRoot>();
                singleton.Configure(Array.Empty<SceneRootInstaller>(), true);
                root = singleton;
            }
            else
            {
                var owned = host.AddComponent<SceneOwnedRoot>();
                owned.Configure(Array.Empty<SceneRootInstaller>(), false);
                root = owned;
            }
            var bootstrap = host.AddComponent<BootstrapSystem>();
            var controller = host.AddComponent<SceneTransitionSampleController>();
            host.AddComponent<SampleTransitionCondition>().Configure(controller, null);
            var eventHost = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventHost.transform.SetParent(host.transform, false);
            var module = eventHost.GetComponent<InputSystemUIInputModule>();
            var hud = Canvas(host.transform, "GameHUD", 100);
            var bar = Image(hud.transform, "Commands", new Color(.07f, .09f, .12f, .98f));
            Rect(bar.rectTransform, Vector2.zero, new Vector2(.27f, 1));
            var heading = Label(bar.transform, "Title", (single ? "SINGLE" : "ADDITIVE") + "\nScene transitions", 24);
            Rect(heading.rectTransform, new Vector2(.05f, .86f), new Vector2(.95f, .99f));
            var buttons = new Button[Commands.Length];
            for (int i = 0; i < Commands.Length; i++)
            {
                buttons[i] = Button(bar.transform, Commands[i]);
                float top = .83f - i * .072f;
                Rect((RectTransform)buttons[i].transform, new Vector2(.05f, top - .061f), new Vector2(.95f, top));
            }
            var status = Label(hud.transform, "Status", "Preparing session...", 18);
            Rect(status.rectTransform, new Vector2(.29f, .03f), new Vector2(.98f, .15f));
            var cover = Group(host.transform, "TransitionCover", 200, new Color(.025f, .035f, .055f, 1f), out var coverPanel);
            var loading = Label(coverPanel.transform, "Loading", "Preparing content and presentation...", 28);
            Rect(loading.rectTransform, new Vector2(.25f, .35f), new Vector2(.8f, .65f));
            var modal = Group(host.transform, "SystemModal", 300, new Color(0, 0, 0, 1f), out var modalPanel);
            var message = Label(modalPanel.transform, "Message", "Independent system modal", 26);
            Rect(message.rectTransform, new Vector2(.25f, .4f), new Vector2(.8f, .72f));
            var close = Button(modalPanel.transform, "Dismiss modal");
            Rect((RectTransform)close.transform, new Vector2(.35f, .25f), new Vector2(.65f, .33f));
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Settings/SampleInput.inputactions");
            if (source == null) throw new InvalidOperationException("Installed sample input source is required.");
            controller.ConfigureSample(bootstrap, source, cover, modal, module, status, message, buttons, close);
            bootstrap.Configure(root, settings, "entry", false, controller);
            eventHost.GetComponent<EventSystem>().firstSelectedGameObject = buttons[0].gameObject;
            host.SetActive(true);
            if (!EditorSceneManager.SaveScene(scene, SceneTransitionSamplePaths.Bootstrap(single))) throw new IOException("Could not save owned Bootstrap scene.");
        }

        private static Canvas Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            return canvas;
        }
        private static CanvasGroup Group(Transform parent, string name, int order, Color color, out Image panel)
        {
            var canvas = Canvas(parent, name, order);
            panel = Image(canvas.transform, "FullScreen", color);
            Stretch(panel.rectTransform);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }
        private static Image Image(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }
        private static Text Label(Transform parent, string name, string value, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string title)
        {
            var image = Image(parent, title, new Color(.2f, .29f, .4f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(image.transform, "Label", title, 17);
            Stretch(label.rectTransform);
            return button;
        }
        private static void Stretch(RectTransform rect) => Rect(rect, Vector2.zero, Vector2.one);
        private static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void RequireAssets(bool single)
        {
            if (!File.Exists(Marker) || File.ReadAllText(Marker).Trim() != Ownership || SceneTransitionSamplePaths.BuildScenes(single).Any(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null))
                throw new InvalidOperationException("Run Build Sample Assets explicitly first.");
            string settingsPath = SettingsPath(single);
            var settings = AssetDatabase.LoadAssetAtPath<SceneTransitionSettings>(settingsPath);
            if (settings == null) throw new InvalidOperationException("The saved sample settings asset is missing or has the wrong type.");
            settings.CreateSnapshot();
            var preview = EditorSceneManager.OpenPreviewScene(SceneTransitionSamplePaths.Bootstrap(single));
            try
            {
                var bootstraps = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BootstrapSystem>(true)).ToArray();
                if (bootstraps.Length != 1) throw new InvalidOperationException("The saved sample requires exactly one Bootstrap.");
                var bootstrap = bootstraps[0];
                if (bootstrap.Settings == null || AssetDatabase.GetAssetPath(bootstrap.Settings) != settingsPath || bootstrap.FirstTransitionId != "entry")
                    throw new InvalidOperationException("The saved Bootstrap must reference its mode settings and entry definition.");
                bootstrap.Settings.CreateSnapshot();
                if (bootstrap.FirstScenePath != SceneTransitionSamplePaths.Hub || bootstrap.LoadMode != (single ? LoadSceneMode.Single : LoadSceneMode.Additive))
                    throw new InvalidOperationException("The saved Bootstrap entry must select Hub with the sample's explicit load mode.");
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        private static void ResolveRoot()
        {
            // Preserved MonoScript GUID follows canonical relocation and UPM Sample.Import.
            const string builderGuid = "3da5004585d94731a0f004f6211ad0e6";
            string path = AssetDatabase.GUIDToAssetPath(builderGuid);
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(path) == null)
                throw new InvalidOperationException("The sample builder script could not be resolved. Import the sample first.");
            SceneTransitionSamplePaths.ConfigureRoot(SceneTransitionSamplePaths.RootFromScript(path));
        }

        private static void RequireIdleAndClean()
        {
            ResolveRoot();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) throw new InvalidOperationException("Requires the idle compiled original Editor.");
            foreach (var scene in EditorSceneManager.GetSceneManagerSetup())
                if (string.IsNullOrEmpty(scene.path) || scene.isLoaded && SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save existing scene edits before using the sample builder.");
        }
        private static string CurrentStartScene() => EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "";
        private static Baseline Capture() => new Baseline
        {
            scenes = EditorSceneManager.GetSceneManagerSetup(), startScene = CurrentStartScene(),
            build = EditorBuildSettings.scenes.Select(scene => new BuildSceneState { path = scene.path, enabled = scene.enabled }).ToArray()
        };
        private static void Restore(Baseline baseline, byte[] raw)
        {
            EditorBuildSettings.scenes = baseline.build.Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled)).ToArray();
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(baseline.startScene) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(baseline.startScene);
            EditorSceneManager.RestoreSceneManagerSetup(baseline.scenes);
            AssetDatabase.ReleaseCachedFileHandles();
            File.WriteAllBytes(BuildFile, raw);
            // Restore missing original GUID bytes after Unity's native list normalization.
        }
        private static bool SameSetup(SceneSetup[] left, SceneSetup[] right) => left.Length == right.Length && left.Zip(right, (a, b) => a.path == b.path && a.isLoaded == b.isLoaded && a.isActive == b.isActive).All(value => value);
    }
}
