using System;
using System.IO;
using System.Linq;
using MyLab.Core.Lifecycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    /// <summary>Runs repeated Play checks in the current Editor without nesting Test Framework runs.</summary>
    [InitializeOnLoad]
    public static class SingletonReloadCheck
    {
        private const string Key = "MyLab.SingletonReloadCheck.";
        private const string ResultPath = "Temp/Singleton/reload-check.json";
        private static readonly EnterPlayModeOptions[] Options =
        {
            EnterPlayModeOptions.None,
            EnterPlayModeOptions.DisableDomainReload,
            EnterPlayModeOptions.DisableSceneReload,
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload,
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload
        };

        [Serializable]
        private class SceneState { public SceneSetup[] Scenes; }

        [Serializable]
        private class Result
        {
            public bool Success;
            public int CompletedPlayChecks;
            public bool SettingsRestored;
            public string Error;
            public string[] Observations;
        }

        static SingletonReloadCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>Starts ten Play checks; writes results and restores scene setup and Editor options.</summary>
        public static void Run()
        {
            if (SessionState.GetBool(Key + "Active", false))
            {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Run only in an idle Editor.");
            }
            foreach (var scene in EditorSceneManager.GetSceneManagerSetup())
            {
                if (string.IsNullOrEmpty(scene.path) || SceneManager.GetSceneByPath(scene.path).isDirty)
                {
                    throw new InvalidOperationException("Save scene edits before running this check.");
                }
            }
            Directory.CreateDirectory("Temp/Singleton");
            SessionState.SetString(Key + "Scenes", JsonUtility.ToJson(new SceneState { Scenes = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + "Enabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Key + "Options", (int)EditorSettings.enterPlayModeOptions);
            SessionState.SetInt(Key + "Step", 0);
            SessionState.SetString(Key + "Observations", "");
            SessionState.SetString(Key + "Error", "");
            SessionState.SetBool(Key + "Active", true);
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var source = new GameObject("SingletonReloadOwner");
                SceneManager.MoveGameObjectToScene(source, scene);
                source.AddComponent<SingletonProbe>();
                AddSceneRoot(scene, SceneRootMode.SceneOwned, "owned");
                AddSceneRoot(scene, SceneRootMode.Singleton, "global");
                string path = "Assets/MyLab/Tests/EditMode/SingletonReload-" + Guid.NewGuid().ToString("N") + ".unity";
                SessionState.SetString(Key + "Path", path);
                if (!EditorSceneManager.SaveScene(scene, path))
                {
                    throw new InvalidOperationException("Could not save the temporary check scene.");
                }
                EditorApplication.update += BeginOnNextUpdate;
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
            }
        }

        private static void BeginPlay()
        {
            int step = SessionState.GetInt(Key + "Step", 0);
            if (step % 2 == 0)
            {
                SingletonProbe.TotalInitializeCount = 0;
                SingletonProbe.TotalShutdownCount = 0;
                SceneRootInstallerProbe.Trace.Clear();
            }
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = Options[step / 2];
            EditorApplication.isPlaying = true;
        }

        private static void AddSceneRoot(Scene scene, SceneRootMode mode, string id)
        {
            var root = new GameObject("SingletonReloadRoot-" + id);
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            var installer = root.AddComponent<SceneRootInstallerProbe>();
            installer.Id = id;
            SceneRootSetup.Attach(root, mode, new[] { installer });
            root.SetActive(true);
        }

        private static void BeginOnNextUpdate()
        {
            EditorApplication.update -= BeginOnNextUpdate;
            BeginPlay();
        }

        private static void StopOnNextUpdate()
        {
            EditorApplication.update -= StopOnNextUpdate;
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key + "Active", false))
            {
                return;
            }
            int step = SessionState.GetInt(Key + "Step", 0);
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    var options = Options[step / 2];
                    var owner = SingletonProbe.Instance;
                    int expected = (options & EnterPlayModeOptions.DisableDomainReload) == 0 ? 1 : step % 2 + 1;
                    Require(EditorSettings.enterPlayModeOptions == options
                        && (options == EnterPlayModeOptions.None || EditorSettings.enterPlayModeOptionsEnabled), "Editor options changed.");
                    Require(owner != null && owner.IsInitialized, "Owner was not initialized.");
                    Require(owner.InstanceDuringInitialize == null, "Owner was published before initialization completed.");
                    Require(SingletonProbe.TotalInitializeCount == expected, "Unexpected initialization count.");
                    foreach (string id in new[] { "owned", "global" })
                    {
                        var rootObject = GameObject.Find("SingletonReloadRoot-" + id);
                        var installer = rootObject.GetComponent<SceneRootInstallerProbe>();
                        var root = id == "owned" ? (ISceneRoot)rootObject.GetComponent<SceneOwnedRoot>()
                            : rootObject.GetComponent<SingletonSceneRoot>();
                        Require(root.IsReady && ReferenceEquals(installer.InjectedRoot, root), "Scene root injection was not restored: " + id);
                        Require(SceneRootInstallerProbe.Trace.Count(entry => entry == "install:" + id) == expected,
                            "Unexpected root installation count: " + id);
                    }
                    if (step == 8)
                    {
                        owner.SendMessage("OnApplicationQuit");
                        Require(SingletonProbe.Instance == null, "Quit did not hide the owner.");
                        var lateObject = new GameObject("SingletonReloadLateOwner");
                        var late = lateObject.AddComponent<SingletonProbe>();
                        Require(late.InitializeCount == 0 && !late.enabled, "Late registration succeeded during quit.");
                        UnityEngine.Object.Destroy(lateObject);
                    }
                    string line = step + ": " + options + ", totalInit=" + SingletonProbe.TotalInitializeCount
                        + ", ownerInit=" + owner.InitializeCount + ", ownerId=" + owner.GetInstanceID()
                        + ", owned/globalInstall=" + expected + ", injected=true";
                    SessionState.SetString(Key + "Observations", SessionState.GetString(Key + "Observations", "") + line + "\n");
                }
                catch (Exception exception)
                {
                    SessionState.SetString(Key + "Error", exception.ToString());
                }
                EditorApplication.update += StopOnNextUpdate;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                string error = SessionState.GetString(Key + "Error", "");
                if (SingletonProbe.Instance != null)
                {
                    error = "Instance remained available outside Play.";
                }
                int expected = (Options[step / 2] & EnterPlayModeOptions.DisableDomainReload) == 0 ? 1 : step % 2 + 1;
                if (SingletonProbe.TotalShutdownCount != expected)
                {
                    error += "Unexpected shutdown count: " + SingletonProbe.TotalShutdownCount + ".";
                }
                foreach (string id in new[] { "owned", "global" })
                {
                    if (SceneRootInstallerProbe.Trace.Count(entry => entry == "uninstall:" + id) != expected)
                    {
                        error += "Unexpected root cleanup count: " + id + ".";
                    }
                }
                SessionState.SetString(Key + "Observations", SessionState.GetString(Key + "Observations", "")
                    + "exit " + step + ": totalShutdown=" + SingletonProbe.TotalShutdownCount
                    + ", owned/globalCleanup=" + expected + "\n");
                if (error != "")
                {
                    Finish(error);
                    return;
                }
                SessionState.SetInt(Key + "Step", step + 1);
                if (step == 9)
                {
                    Finish("");
                }
                else
                {
                    EditorApplication.update += BeginOnNextUpdate;
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "Active", false);
            var originalOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "Options", 0);
            bool originalEnabled = SessionState.GetBool(Key + "Enabled", true);
            try
            {
                EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<SceneState>(SessionState.GetString(Key + "Scenes", "")).Scenes);
                AssetDatabase.DeleteAsset(SessionState.GetString(Key + "Path", ""));
            }
            catch (Exception exception)
            {
                error += "\n" + exception;
            }
            finally
            {
                // In 6000.3.18f1 the legacy toggle setter also changes the reload flags.
                EditorSettings.enterPlayModeOptionsEnabled = originalEnabled;
                EditorSettings.enterPlayModeOptions = originalOptions;
            }
            var result = new Result
            {
                Success = error == "",
                CompletedPlayChecks = SessionState.GetInt(Key + "Step", 0),
                SettingsRestored = EditorSettings.enterPlayModeOptions == originalOptions && EditorSettings.enterPlayModeOptionsEnabled == originalEnabled,
                Error = error,
                Observations = SessionState.GetString(Key + "Observations", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            };
            File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
        }
    }
}
