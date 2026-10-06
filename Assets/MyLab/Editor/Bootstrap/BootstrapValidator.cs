using System;
using System.Collections.Generic;
using System.Linq;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace MyLab.Core.Editor.Bootstrap
{
    /// <summary>Reads saved scenes in isolated previews without saving or replacing user scenes.</summary>
    public static class BootstrapValidator
    {
        /// <summary>Validates the build scene plan against the project's current Addressables settings when needed.</summary>
        public static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths,
            AddressableAssetSettings settings)
        {
            return ValidateBuildScenesCore(scenePaths, settings, true);
        }

        /// <summary>Validates one Bootstrap against supplied project settings for the current preflight operation.</summary>
        public static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap, AddressableAssetSettings settings)
        {
            return ValidateBootstrap(bootstrap, settings, true);
        }

        /// <summary>Checks the actual ordered build scene paths. Projects without Bootstrap are unaffected.</summary>
        public static IReadOnlyList<string> ValidateBuildScenes(IReadOnlyList<string> scenePaths)
            => ValidateBuildScenesCore(scenePaths, null, false);

        private static IReadOnlyList<string> ValidateBuildScenesCore(IReadOnlyList<string> scenePaths,
            AddressableAssetSettings suppliedSettings, bool settingsWereSupplied)
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
                bool hasAddressable = bootstraps.Any(b => b.Source == SceneSource.Addressable);
                AddressableAssetSettings settings = suppliedSettings;
                if (hasAddressable && !settingsWereSupplied) settings = ResolveCurrentSettings();
                foreach (var bootstrap in bootstraps)
                {
                    string path = bootstrap.gameObject.scene.path;
                    if (scenePaths.Count == 0 || path != scenePaths[0])
                        errors.Add("Bootstrap must be the first build scene: " + path);
                    errors.AddRange(ValidateBootstrap(bootstrap, settings, settingsWereSupplied || hasAddressable));
                    if (bootstrap.Source == SceneSource.BuildScene && !scenePaths.Contains(bootstrap.FirstScenePath))
                        errors.Add("First game scene must be included in the build: " + bootstrap.FirstScenePath);
                    else
                    {
                        var game = previews.FirstOrDefault(s => s.path == bootstrap.FirstScenePath);
                        if (!game.IsValid() && bootstrap.Source == SceneSource.Addressable &&
                            AssetDatabase.LoadAssetAtPath<SceneAsset>(bootstrap.FirstScenePath) != null)
                        {
                            game = EditorSceneManager.OpenPreviewScene(bootstrap.FirstScenePath);
                            previews.Add(game);
                        }
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
            => ValidateBootstrap(bootstrap, null, false);

        private static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap,
            AddressableAssetSettings settings, bool settingsWereSupplied)
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
            else if (bootstrap.Source == SceneSource.Addressable)
            {
                if (!settingsWereSupplied) settings = ResolveCurrentSettings();
                errors.AddRange(ValidateAddressableTarget(bootstrap, settings));
            }
            return errors;
        }

        private static IReadOnlyList<string> ValidateAddressableTarget(BootstrapSystem bootstrap, AddressableAssetSettings settings)
        {
            var errors = new List<string>();
            if (settings == null) { errors.Add("Addressables settings are missing; create/configure project settings explicitly."); return errors; }
            string path = bootstrap.FirstScenePath;
            var reference = bootstrap.SceneReference;
            bool hasReference = reference != null && !string.IsNullOrEmpty(reference.AssetGUID);
            if (hasReference && !string.IsNullOrWhiteSpace(bootstrap.AddressableKey))
            { errors.Add("Select an Addressables key or scene reference, not both."); return errors; }
            if (hasReference && (!string.IsNullOrEmpty(reference.SubObjectName) ||
                !string.Equals(reference.RuntimeKey.ToString(), reference.AssetGUID, StringComparison.Ordinal)))
            { errors.Add("Scene AssetReference cannot target a sub-object; select the scene asset itself."); return errors; }
            string guid = hasReference ? reference.AssetGUID : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || AssetDatabase.GUIDToAssetPath(guid) != path)
            { errors.Add("Addressables target GUID must identify the selected scene: " + path); return errors; }
            var entry = settings.FindAssetEntry(guid, false);
            if (entry == null || !entry.IsScene || entry.AssetPath != path)
            { errors.Add("Selected game scene must have an explicit Addressables scene entry: " + path); return errors; }
            if (hasReference)
            {
                var schema = entry.parentGroup == null ? null : entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
                if (schema == null || !schema.IncludeGUIDInCatalog)
                    errors.Add("Addressables scene reference requires Include GUID in Catalog on its Bundled Asset Group schema: " + path);
                var entries = new List<AddressableAssetEntry>();
                settings.GetAllAssets(entries, false);
                if (entries.Any(candidate => candidate.address == guid && candidate.guid != guid))
                    errors.Add("Addressables reference GUID collides with another asset address: " + guid);
            }
            else
            {
                var matches = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
                settings.GetAllAssets(matches, false);
                var addressMatches = matches.Where(candidate => candidate.address == bootstrap.AddressableKey).ToArray();
                bool selectedGuid = string.Equals(bootstrap.AddressableKey, guid, StringComparison.Ordinal);
                var schema = entry.parentGroup == null ? null : entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
                bool guidAvailable = schema != null && schema.IncludeGUIDInCatalog;
                bool addressAvailable = schema != null && schema.IncludeAddressInCatalog &&
                    addressMatches.Length == 1 && addressMatches[0].guid == guid;
                if (addressMatches.Any(candidate => candidate.guid != guid) || addressMatches.Length > 1 ||
                    (selectedGuid ? !guidAvailable && !addressAvailable : !addressAvailable))
                    errors.Add("Addressables key must uniquely identify the selected scene: " + bootstrap.AddressableKey);
            }
            return errors;
        }

        private static AddressableAssetSettings ResolveCurrentSettings()
        {
            // Read only the current config object; Settings may migrate legacy config as a side effect.
            return EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                out AddressableAssetSettingsDefaultObject _) ? AddressableAssetSettingsDefaultObject.Settings : null;
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
