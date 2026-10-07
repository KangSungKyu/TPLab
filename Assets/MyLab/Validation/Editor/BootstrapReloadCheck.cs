using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using MyLab.Core.Input;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    /// <summary>Runs eight real Bootstrap entries in the current Editor and restores its original saved setup.</summary>
    [InitializeOnLoad]
    public static class BootstrapReloadCheck
    {
        private const string Key = "MyLab.BootstrapReloadCheck.";
        private const string Hub = "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity";
        private const string BuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
        private static readonly EnterPlayModeOptions[] Options =
        {
            EnterPlayModeOptions.None,
            EnterPlayModeOptions.DisableDomainReload,
            EnterPlayModeOptions.DisableSceneReload,
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload
        };
        private static GameSceneManager _previousManager;
        private static BootstrapSystem _previousBootstrap;
        private static int _previousBootstrapInstanceId;
        private static InputManager _previousInput;

        [Serializable]
        private sealed class BuildSceneState
        {
            public string Path;
            public bool Enabled;
        }

        [Serializable]
        private sealed class Baseline
        {
            public SceneSetup[] Scenes;
            public BuildSceneState[] BuildScenes;
            public bool OptionsEnabled;
            public EnterPlayModeOptions Options;
            public string StartScene;
        }

        [Serializable]
        private sealed class Result
        {
            public bool Success;
            public bool InputIncluded;
            public int AttemptedEntries;
            public int CompletedEntries;
            public string ErrorStage;
            public string Error;
            public string[] Observations;
            public bool SettingsRestored;
            public bool StartSceneRestored;
            public bool SceneSetupRestored;
            public bool BuildSettingsRestored;
            public bool BuildSettingsBytesRestored;
            public bool FixtureRemoved;
        }

        static BootstrapReloadCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += CheckEntryDeadline;
        }

        /// <summary>
        /// Dispatches four reload combinations twice each. Requires saved clean scenes and an unused evidence path
        /// under doc/validation/scene-integration or input-system. Configures the owned saved fixture once;
        /// never resets Bootstrap in Play. includeInput also checks fresh input scopes and explicit preparation publication.
        /// </summary>
        public static void Run(string evidencePath, bool includeInput = false)
        {
            if (SessionState.GetBool(Key + "Active", false)) throw new InvalidOperationException("A Bootstrap reload check already owns the Editor.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Requires an idle compiled Editor.");
            var scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (var scene in scenes)
                if (string.IsNullOrEmpty(scene.path) || (scene.isLoaded && SceneManager.GetSceneByPath(scene.path).isDirty))
                    throw new InvalidOperationException("Save scene edits before running this check.");
            string evidence = Path.GetFullPath(evidencePath);
            string evidenceRoot = Path.GetFullPath("doc/validation/scene-integration") + Path.DirectorySeparatorChar;
            string inputEvidenceRoot = Path.GetFullPath("doc/validation/input-system") + Path.DirectorySeparatorChar;
            if ((!evidence.StartsWith(evidenceRoot, StringComparison.OrdinalIgnoreCase) &&
                !evidence.StartsWith(inputEvidenceRoot, StringComparison.OrdinalIgnoreCase)) ||
                !Directory.Exists(Path.GetDirectoryName(evidence)) || File.Exists(evidence))
                throw new ArgumentException("Choose an unused evidence file in the existing validation directory.", nameof(evidencePath));
            string id = Guid.NewGuid().ToString("N");
            string workspace = Path.GetFullPath("Temp/GameScenesTrack/Reload6/" + id);
            string fixture = "Assets/MyLab/Tests/Fixtures/BootstrapReload-" + id + ".unity";
            if (Directory.Exists(workspace) || File.Exists(fixture) || File.Exists(fixture + ".meta"))
                throw new InvalidOperationException("Owned harness path collision.");
            var baseline = new Baseline
            {
                Scenes = scenes,
                BuildScenes = EditorBuildSettings.scenes.Select(scene => new BuildSceneState { Path = scene.path, Enabled = scene.enabled }).ToArray(),
                OptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled,
                Options = EditorSettings.enterPlayModeOptions,
                StartScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
            };
            Directory.CreateDirectory(workspace);
            File.WriteAllText(Path.Combine(workspace, "baseline.json"), JsonUtility.ToJson(baseline, true));
            File.Copy(BuildSettingsPath, Path.Combine(workspace, "EditorBuildSettings.asset"));
            SessionState.SetString(Key + "Workspace", workspace);
            SessionState.SetString(Key + "Evidence", evidence);
            SessionState.SetString(Key + "Fixture", fixture);
            SessionState.SetBool(Key + "IncludeInput", includeInput);
            SessionState.SetInt(Key + "Step", 0);
            SessionState.SetInt(Key + "Attempted", 0);
            SessionState.SetInt(Key + "Completed", 0);
            SessionState.SetString(Key + "Error", "");
            SessionState.SetString(Key + "ErrorStage", "");
            SessionState.SetString(Key + "Observations", "");
            SessionState.SetBool(Key + "Active", true);
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var host = new GameObject("BootstrapReloadCommon");
                host.SetActive(false);
                var installer = host.AddComponent<SceneRootInstallerProbe>();
                installer.Id = "reload-common";
                var root = host.AddComponent<SceneOwnedRoot>();
                if (includeInput)
                {
                    var inputInstaller = host.AddComponent<InputManagerInstaller>();
                    inputInstaller.Configure(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
                    root.Configure(new SceneRootInstaller[] { inputInstaller, installer });
                }
                else root.Configure(new SceneRootInstaller[] { installer });
                var callbacks = host.AddComponent<BootstrapReloadCallbacksProbe>();
                host.AddComponent<BootstrapSystem>().Configure(root, Hub, false, callbacks);
                host.SetActive(true);
                if (!EditorSceneManager.SaveScene(scene, fixture)) throw new InvalidOperationException("Could not save owned Bootstrap fixture.");
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(fixture, true), new EditorBuildSettingsScene(Hub, true) };
                EditorSceneManager.playModeStartScene = null;
                ScheduleEntry();
            }
            catch (Exception exception)
            {
                RecordError("setup", exception);
                Finish();
            }
        }

        private static void ScheduleEntry()
        {
            SessionState.SetString(Key + "Phase", "scheduled");
            EditorApplication.update += BeginOnNextUpdate;
        }

        private static void BeginOnNextUpdate()
        {
            EditorApplication.update -= BeginOnNextUpdate;
            if (!SessionState.GetBool(Key + "Active", false)) return;
            try
            {
                int step = SessionState.GetInt(Key + "Step", 0);
                // A fresh baseline belongs to the new combination only. Its second entry keeps the same native scene.
                if (step % 2 == 0)
                {
                    _previousManager = null;
                    _previousBootstrap = null;
                    _previousBootstrapInstanceId = 0;
                    _previousInput = null;
                    EditorSceneManager.OpenScene(SessionState.GetString(Key + "Fixture", ""), OpenSceneMode.Single);
                }
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = Options[step / 2];
                SessionState.SetBool(Key + "Passed", false);
                SessionState.SetString(Key + "Phase", "entering");
                SessionState.SetString(Key + "Deadline", (EditorApplication.timeSinceStartup + 45).ToString("R", CultureInfo.InvariantCulture));
                SessionState.SetInt(Key + "Attempted", SessionState.GetInt(Key + "Attempted", 0) + 1);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordError("entering", exception);
                Finish();
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Key + "Active", false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetString(Key + "Phase", "entry");
                CheckEntryAsync(SessionState.GetInt(Key + "Step", 0)).Forget();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                int step = SessionState.GetInt(Key + "Step", 0);
                if (SessionState.GetString(Key + "Phase", "") != "exiting")
                    RecordError("exit", new InvalidOperationException("Play ended before the harness completed graceful shutdown, or its pre-Play gate rejected entry."));
                if (SceneManager.GetSceneByPath(Hub).isLoaded)
                    RecordError("exit", new InvalidOperationException("Owned game scene remained loaded after exit."));
                Observe("exit " + step + ": entered Edit Mode, gameLoaded=" + SceneManager.GetSceneByPath(Hub).isLoaded);
                if (SessionState.GetBool(Key + "Passed", false) && SessionState.GetString(Key + "Error", "").Length == 0)
                    SessionState.SetInt(Key + "Completed", SessionState.GetInt(Key + "Completed", 0) + 1);
                SessionState.SetInt(Key + "Step", step + 1);
                if (SessionState.GetString(Key + "Error", "").Length != 0 || step == 7) Finish();
                else ScheduleEntry();
            }
        }

        private static async UniTask CheckEntryAsync(int step)
        {
            BootstrapSystem bootstrap = null;
            SceneOwnedRoot common = null;
            SceneRootInstallerProbe installer = null;
            InputManagerInstaller inputInstaller = null;
            InputManager input = null;
            InputActionAsset inputClone = null;
            IDisposable gameplay = null;
            int releaseBefore = 0;
            int uninstallBefore = 0;
            bool entered = false;
            try
            {
                await UniTask.Yield();
                var scene = SceneManager.GetSceneByPath(SessionState.GetString(Key + "Fixture", ""));
                bootstrap = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<BootstrapSystem>(true)).Single();
                common = (SceneOwnedRoot)bootstrap.SceneRoot;
                installer = common.GetComponent<SceneRootInstallerProbe>();
                releaseBefore = installer.ReleaseCount;
                uninstallBefore = installer.UninstallCount;
                int prepareBefore = installer.PrepareCount;
                var callbacks = common.GetComponent<BootstrapReloadCallbacksProbe>();
                int coverBefore = callbacks.CoverCount;
                int configureBefore = callbacks.ConfigurationCount;
                int presentationBefore = callbacks.PresentationCount;
                int revealBefore = callbacks.RevealCount;
                Observe(step + ": observedReloadOptions=" + EditorSettings.enterPlayModeOptions +
                    ", observedOptionsEnabled=" + EditorSettings.enterPlayModeOptionsEnabled);
                Require(EditorSettings.enterPlayModeOptions == Options[step / 2] &&
                    (Options[step / 2] == EnterPlayModeOptions.None || EditorSettings.enterPlayModeOptionsEnabled), "Reload options changed.");
                bool sameBootstrap = ReferenceEquals(_previousBootstrap, bootstrap);
                Observe(step + ": " + Options[step / 2] + ", bootstrapId=" + bootstrap.GetInstanceID() +
                    ", rootId=" + common.GetInstanceID() + ", sameBootstrapReference=" + sameBootstrap);
                if (step % 2 == 1 && Options[step / 2] == (EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload))
                    Require(_previousBootstrapInstanceId != 0 && _previousBootstrapInstanceId == bootstrap.GetInstanceID(),
                        "Both-disabled second entry replaced its native Bootstrap instance; reload behavior was not exercised.");
                var managerBefore = bootstrap.Manager;
                // Test-only diagnostics: native object identity does not imply Unity retained its CLR wrapper or fields.
                bool stoppingBefore = (bool)typeof(BootstrapSystem).GetField("_stopping", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(bootstrap);
                Observe(step + ": beforeEntry managerPresent=" + (managerBefore != null) +
                    ", managerState=" + (managerBefore == null ? "<none>" : managerBefore.State.ToString()) +
                    ", managerReferenceId=" + (managerBefore == null ? 0 : RuntimeHelpers.GetHashCode(managerBefore)) +
                    ", stopping=" + stoppingBefore + ", previousManagerPresent=" + (_previousManager != null) +
                    ", previousManagerState=" + (_previousManager == null ? "<none>" : _previousManager.State.ToString()) +
                    ", previousManagerReferenceId=" + (_previousManager == null ? 0 : RuntimeHelpers.GetHashCode(_previousManager)) +
                    ", sameManagerReference=" + ReferenceEquals(_previousManager, managerBefore) +
                    ", previousBootstrapNativeValid=" + (_previousBootstrap != null) +
                    ", previousBootstrapId=" + _previousBootstrapInstanceId +
                    ", install=" + installer.InstallCount + ", prepare=" + prepareBefore +
                    ", release=" + releaseBefore + ", uninstall=" + uninstallBefore +
                    ", cover=" + coverBefore + ", configure=" + configureBefore +
                    ", presentation=" + presentationBefore + ", reveal=" + revealBefore);
                Require(common.IsReady && ReferenceEquals(installer.InjectedRoot, common), "Common installation was not restored.");
                InputActionMap sourceMap = null;
                bool sourceEnabled = false;
                if (SessionState.GetBool(Key + "IncludeInput", false))
                {
                    inputInstaller = common.GetComponent<InputManagerInstaller>();
                    input = inputInstaller.Input;
                    Require(input != null && !input.IsDisposed && !ReferenceEquals(_previousInput, input), "Input scope was not fresh.");
                    var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                    sourceMap = source.FindActionMap("Player", true);
                    sourceEnabled = sourceMap.enabled;
                    inputClone = input.Actions;
                    Require(inputClone != source && input.Layers.Snapshot.AllInputBlocked, "Input clone or preparation block was missing.");
                    input.Layers.RegisterLayer("reload-game", new[] { sourceMap.id }, 0, InputLayerMode.Overlay);
                    gameplay = input.Layers.AcquireLayer("reload-game");
                    Require(!inputClone.FindActionMap(sourceMap.id).enabled, "Input escaped its preparation block.");
                    _previousInput = input;
                    Observe(step + ": inputFresh=true, preparationBlocked=true, cloned=true");
                }
                await bootstrap.BootstrapAsync().Timeout(TimeSpan.FromSeconds(20), DelayType.Realtime);
                Require(bootstrap.Manager != null && bootstrap.GameScene.path == Hub && bootstrap.Manager.CanProceed && common.IsPrepared,
                    "Bootstrap did not publish a prepared fresh game.");
                Require(installer.PrepareCount == prepareBefore + 1 && callbacks.CoverCount == coverBefore + 1 &&
                    callbacks.ConfigurationCount == configureBefore + 1 && callbacks.PresentationCount == presentationBefore + 1 &&
                    callbacks.RevealCount == revealBefore + 1,
                    "Entry reused historical completion instead of real preparation/load/reveal.");
                if (input != null)
                {
                    inputInstaller.CompletePreparation();
                    Require(inputClone.FindActionMap(sourceMap.id).enabled && sourceMap.enabled == sourceEnabled,
                        "Prepared input did not publish independently of the source asset.");
                    using (input.Layers.BlockAll())
                        Require(!inputClone.FindActionMap(sourceMap.id).enabled, "Input block failed after reentry.");
                    Require(inputClone.FindActionMap(sourceMap.id).enabled, "Input lease was not restored after reentry.");
                    Observe(step + ": inputPrepared=true, leaseRestored=true, sourceUnchanged=true");
                }
                if (step % 2 == 1 && (Options[step / 2] & EnterPlayModeOptions.DisableDomainReload) != 0)
                    Require(_previousManager != null && !ReferenceEquals(_previousManager, bootstrap.Manager), "Second entry reused the previous manager.");
                _previousManager = bootstrap.Manager;
                _previousBootstrap = bootstrap;
                _previousBootstrapInstanceId = bootstrap.GetInstanceID();
                entered = true;
                Observe(step + ": " + Options[step / 2] + ", prepared=true, freshManager=true, game=" + bootstrap.GameScene.path);
            }
            catch (Exception exception) { RecordError("entry " + step, exception); }
            finally
            {
                SessionState.SetString(Key + "Phase", "shutdown");
                try
                {
                    if (bootstrap != null) await bootstrap.ShutdownAsync().Timeout(TimeSpan.FromSeconds(20), DelayType.Realtime);
                }
                catch (Exception exception) { RecordError("manager shutdown " + step, exception); }
                try
                {
                    if (common != null) await common.ShutdownAsync().Timeout(TimeSpan.FromSeconds(20), DelayType.Realtime);
                    if (input != null)
                    {
                        gameplay?.Dispose();
                        await UniTask.NextFrame();
                        Require(input.IsDisposed && inputInstaller.Input == null && inputClone == null,
                            "Input shutdown retained its scope or native clone.");
                        Observe("shutdown " + step + ": inputReleased=true, cloneDestroyed=true, lateLeaseSafe=true");
                    }
                    if (entered)
                    {
                        Require(bootstrap.Manager.State == SceneTransitionState.Stopped && bootstrap.Manager.OwnedScenes.Count == 0 &&
                            !SceneManager.GetSceneByPath(Hub).isLoaded && !common.IsReady && !common.IsPrepared,
                            "Graceful scene/root shutdown did not finish.");
                        Require(installer.ReleaseCount == releaseBefore + 1 && installer.UninstallCount == uninstallBefore + 1,
                            "Common release/uninstall did not finish exactly once.");
                        Observe("shutdown " + step + ": managerStopped=true, gameUnloaded=true, commonReleased=true");
                    }
                }
                catch (Exception exception) { RecordError("common shutdown " + step, exception); }
                if (SessionState.GetBool(Key + "Active", false) && SessionState.GetInt(Key + "Step", -1) == step)
                {
                    SessionState.SetBool(Key + "Passed", entered && SessionState.GetString(Key + "Error", "").Length == 0);
                    SessionState.SetString(Key + "Phase", "exiting");
                    EditorApplication.update += StopOnNextUpdate;
                }
            }
        }

        private static void StopOnNextUpdate()
        {
            EditorApplication.update -= StopOnNextUpdate;
            if (SessionState.GetBool(Key + "Active", false)) EditorApplication.isPlaying = false;
        }

        private static void CheckEntryDeadline()
        {
            if (!SessionState.GetBool(Key + "Active", false) || SessionState.GetString(Key + "Phase", "") != "entering") return;
            if (double.TryParse(SessionState.GetString(Key + "Deadline", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double deadline) &&
                EditorApplication.timeSinceStartup > deadline)
            {
                RecordError("entering", new TimeoutException("Editor did not enter Play Mode before the deadline."));
                if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
                else Finish();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Observe(string message) => SessionState.SetString(Key + "Observations",
            SessionState.GetString(Key + "Observations", "") + message + "\n");

        private static void RecordError(string stage, Exception exception)
        {
            if (SessionState.GetString(Key + "Error", "").Length == 0) SessionState.SetString(Key + "ErrorStage", stage);
            SessionState.SetString(Key + "Error", SessionState.GetString(Key + "Error", "") + stage + ": " + exception + "\n");
        }

        private static void Finish()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Restore the original Editor setup only after entering Edit Mode.");
            SessionState.SetBool(Key + "Active", false);
            EditorApplication.update -= BeginOnNextUpdate;
            EditorApplication.update -= StopOnNextUpdate;
            string workspace = SessionState.GetString(Key + "Workspace", "");
            string fixture = SessionState.GetString(Key + "Fixture", "");
            var baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(workspace, "baseline.json")));
            var result = new Result
            {
                InputIncluded = SessionState.GetBool(Key + "IncludeInput", false),
                AttemptedEntries = SessionState.GetInt(Key + "Attempted", 0),
                CompletedEntries = SessionState.GetInt(Key + "Completed", 0),
                ErrorStage = SessionState.GetString(Key + "ErrorStage", ""),
                Error = SessionState.GetString(Key + "Error", ""),
                Observations = SessionState.GetString(Key + "Observations", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            };
            try
            {
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(baseline.StartScene) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(baseline.StartScene);
                result.StartSceneRestored = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) == baseline.StartScene;
                EditorBuildSettings.scenes = baseline.BuildScenes.Select(item => new EditorBuildSettingsScene(item.Path, item.Enabled)).ToArray();
                result.BuildSettingsRestored = EditorBuildSettings.scenes.Select(item => item.path + "|" + item.enabled)
                    .SequenceEqual(baseline.BuildScenes.Select(item => item.Path + "|" + item.Enabled));
                EditorSceneManager.RestoreSceneManagerSetup(baseline.Scenes);
                result.SceneSetupRestored = JsonUtility.ToJson(new Baseline { Scenes = EditorSceneManager.GetSceneManagerSetup() }) ==
                    JsonUtility.ToJson(new Baseline { Scenes = baseline.Scenes });
                AssetDatabase.DeleteAsset(fixture);
                result.FixtureRemoved = !File.Exists(fixture) && !File.Exists(fixture + ".meta");
            }
            catch (Exception exception) { result.Error += "restore: " + exception + "\n"; }
            finally
            {
                // Unity's toggle setter changes flags too; restore flags after the toggle.
                EditorSettings.enterPlayModeOptionsEnabled = baseline.OptionsEnabled;
                EditorSettings.enterPlayModeOptions = baseline.Options;
                result.SettingsRestored = EditorSettings.enterPlayModeOptionsEnabled == baseline.OptionsEnabled && EditorSettings.enterPlayModeOptions == baseline.Options;
                var bytes = File.ReadAllBytes(Path.Combine(workspace, "EditorBuildSettings.asset"));
                File.WriteAllBytes(BuildSettingsPath, bytes);
                result.BuildSettingsBytesRestored = File.ReadAllBytes(BuildSettingsPath).SequenceEqual(bytes);
                _previousManager = null;
                _previousBootstrap = null;
                _previousBootstrapInstanceId = 0;
                _previousInput = null;
            }
            result.Success = result.Error.Length == 0 && result.AttemptedEntries == 8 && result.CompletedEntries == 8 &&
                result.SettingsRestored && result.StartSceneRestored && result.SceneSetupRestored && result.BuildSettingsRestored &&
                result.BuildSettingsBytesRestored && result.FixtureRemoved;
            string json = JsonUtility.ToJson(result, true);
            File.WriteAllText(Path.Combine(workspace, "result.json"), json);
            File.WriteAllText(SessionState.GetString(Key + "Evidence", ""), json);
        }
    }
}
