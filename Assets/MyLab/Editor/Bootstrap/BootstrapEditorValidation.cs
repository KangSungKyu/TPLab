using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Editor.Bootstrap
{
    /// <summary>Deferred post-compilation diagnostics and a synchronous pre-Play gate.</summary>
    [InitializeOnLoad]
    public static class BootstrapEditorValidation
    {
        private static bool _queued;
        private static bool _checking;
        private static string _lastDiagnostics;

        static BootstrapEditorValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorBuildSettings.sceneListChanged += Queue;
            EditorSceneManager.sceneSaved += scene => Queue();
            EditorSceneManager.sceneOpened += (scene, mode) => Queue();
            EditorApplication.hierarchyChanged += Queue;
            Queue();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded() => Queue();

        internal static void Queue()
        {
            if (_queued || _checking) return;
            _queued = true;
            EditorApplication.delayCall += CheckDeferred;
        }

        private static void CheckDeferred()
        {
            _queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Queue();
                return;
            }
            Report(ValidateEditorSetup(false));
        }

        /// <summary>Checks enabled build scenes and loaded unsaved Bootstrap objects without changing scenes.</summary>
        public static IReadOnlyList<string> ValidateEditorSetup(bool enteringPlay)
        {
            if (_checking) return Array.Empty<string>();
            _checking = true;
            try
            {
                var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                var errors = new List<string>(BootstrapValidator.ValidateBuildScenes(paths));
                var live = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                    .Where(scene => scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene))
                    .SelectMany(BootstrapValidator.GetBootstraps).ToArray();
                foreach (var bootstrap in live) errors.AddRange(BootstrapValidator.ValidateBootstrap(bootstrap));
                if (live.Length > 1) errors.Add("Only one BootstrapSystem may be loaded.");
                if (enteringPlay)
                {
                    if (live.Length == 1)
                    {
                        if (paths.Length == 0 || live[0].gameObject.scene.path != paths[0])
                            errors.Add("Play must start with the first build scene containing Bootstrap.");
                        if (SceneManager.GetSceneByPath(live[0].FirstScenePath).isLoaded)
                            errors.Add("Unload the first game scene before playing Bootstrap; the manager must own its load.");
                        if (EditorSceneManager.playModeStartScene != null &&
                            AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) != live[0].gameObject.scene.path)
                            errors.Add("Play Mode Start Scene must use the configured Bootstrap scene.");
                    }
                    else if (paths.Length > 0 && AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[0]) != null)
                    {
                        var first = EditorSceneManager.OpenPreviewScene(paths[0]);
                        try
                        {
                            if (BootstrapValidator.GetBootstraps(first).Any())
                                errors.Add("Open the Bootstrap scene before entering Play Mode.");
                        }
                        finally
                        {
                            EditorSceneManager.ClosePreviewScene(first);
                        }
                    }
                }
                return errors.Distinct().ToArray();
            }
            finally
            {
                _checking = false;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            var errors = ValidateEditorSetup(true);
            if (errors.Count == 0) return;
            Report(errors);
            EditorApplication.isPlaying = false;
        }

        private static void Report(IReadOnlyList<string> errors)
        {
            string diagnostics = string.Join("\n", errors);
            if (diagnostics.Length != 0 && diagnostics != _lastDiagnostics)
                Debug.LogError("Bootstrap validation:\n" + diagnostics);
            _lastDiagnostics = diagnostics;
        }
    }

    internal sealed class BootstrapScenePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(path => path.EndsWith(".unity", StringComparison.Ordinal)))
                BootstrapEditorValidation.Queue();
        }
    }
}
