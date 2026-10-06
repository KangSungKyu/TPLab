using System;
using System.Collections.Generic;
using System.Linq;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Editor.Bootstrap
{
    /// <summary>Reads saved scenes in isolated previews without saving or replacing user scenes.</summary>
    public static class BootstrapValidator
    {
        /// <summary>Checks the actual ordered build scene paths. Projects without Bootstrap are unaffected.</summary>
        public static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths)
        {
            var errors = new List<string>();
            var previews = new List<Scene>();
            try
            {
                foreach (string path in scenePaths ?? Array.Empty<string>())
                {
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    {
                        errors.Add("Missing build scene: " + path);
                        continue;
                    }
                    previews.Add(EditorSceneManager.OpenPreviewScene(path));
                }
                var bootstraps = previews.SelectMany(GetBootstraps).ToArray();
                if (bootstraps.Length == 0) return Array.Empty<string>();
                if (bootstraps.Length != 1) errors.Add("The build must contain exactly one BootstrapSystem.");
                foreach (var bootstrap in bootstraps)
                {
                    string path = bootstrap.gameObject.scene.path;
                    if (scenePaths.Count == 0 || path != scenePaths[0])
                        errors.Add("Bootstrap must be the first build scene: " + path);
                    errors.AddRange(ValidateBootstrap(bootstrap));
                    if (!scenePaths.Contains(bootstrap.FirstScenePath))
                        errors.Add("First game scene must be included in the build: " + bootstrap.FirstScenePath);
                    else
                    {
                        var game = previews.FirstOrDefault(s => s.path == bootstrap.FirstScenePath);
                        if (game.IsValid())
                        {
                            errors.AddRange(ValidateGameScene(game));
                            if (bootstrap.SceneRoot is SingletonSceneRoot && game.GetRootGameObjects()
                                .Any(go => go.GetComponentInChildren<SingletonSceneRoot>(true) != null))
                                errors.Add("Bootstrap and game scenes cannot both own SingletonSceneRoot.Instance; use SceneOwnedRoot for the game.");
                        }
                    }
                }
                if (scenePaths.Distinct().Count() != scenePaths.Count)
                    errors.Add("Duplicate build scene paths are not allowed with Bootstrap.");
                return errors;
            }
            catch (Exception exception)
            {
                errors.Add("Bootstrap inspection failed: " + exception.Message);
                return errors;
            }
            finally
            {
                foreach (var scene in previews) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>Checks live serialized references, including unsaved Inspector changes.</summary>
        public static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap)
        {
            var errors = new List<string>();
            try
            {
                bootstrap.ValidateConfiguration();
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
            if (bootstrap.SceneRoot != null) errors.AddRange(ValidateInstallers(bootstrap.SceneRoot));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(bootstrap.FirstScenePath) == null)
                errors.Add("First game scene does not exist: " + bootstrap.FirstScenePath);
            return errors;
        }

        /// <summary>Checks the unique game root and serialized installer ownership.</summary>
        public static IReadOnlyList<string> ValidateGameScene(Scene scene)
        {
            var errors = new List<string>();
            var roots = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(component => component is ISceneRoot).ToArray();
            if (roots.Length != 1) errors.Add("Game scene must contain exactly one lifecycle root: " + scene.path);
            foreach (var root in roots)
            {
                try
                {
                    BootstrapSystem.ValidateSceneRoot(root, scene);
                }
                catch (Exception exception)
                {
                    errors.Add(scene.path + ": " + exception.Message);
                }
                errors.AddRange(ValidateInstallers(root));
            }
            return errors;
        }

        internal static IEnumerable<BootstrapSystem> GetBootstraps(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<BootstrapSystem>(true));

        private static IEnumerable<string> ValidateInstallers(MonoBehaviour root)
        {
            var property = new SerializedObject(root).FindProperty("_installers");
            if (property == null) yield break;
            var seen = new HashSet<SceneRootInstaller>();
            for (int index = 0; index < property.arraySize; ++index)
            {
                var installer = property.GetArrayElementAtIndex(index).objectReferenceValue as SceneRootInstaller;
                if (installer == null || !installer.transform.IsChildOf(root.transform) || !seen.Add(installer))
                    yield return root.name + ": installers must be non-null, unique components on the root or its children.";
            }
        }
    }
}
