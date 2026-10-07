using System;
using System.Collections.Generic;
using System.Linq;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace TPLab.Core.Editor.Bootstrap
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
                bool hasAddressable = bootstraps.Any(UsesAddressables);
                AddressableAssetSettings settings = suppliedSettings;
                if (hasAddressable && !settingsWereSupplied) settings = ResolveCurrentSettings();
                foreach (var bootstrap in bootstraps)
                {
                    string path = bootstrap.gameObject.scene.path;
                    if (scenePaths.Count == 0 || path != scenePaths[0])
                        errors.Add("Bootstrap must be the first build scene: " + path);
                    if (!TryResolveBootstrapTarget(bootstrap, out var target, out var mode, out var definitions,
                            out string configurationError))
                    {
                        errors.Add(configurationError);
                        continue;
                    }
                    errors.AddRange(ValidateBootstrap(bootstrap, scenePaths, settings, settingsWereSupplied || hasAddressable,
                        target, mode, definitions));
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
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            return ValidateBootstrap(bootstrap, paths, null, false);
        }

        public static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap, AddressableAssetSettings settings)
        {
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            return ValidateBootstrap(bootstrap, paths, settings, true);
        }

        private static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap, IReadOnlyList<string> buildScenePaths,
            AddressableAssetSettings settings, bool settingsWereSupplied)
        {
            if (!TryResolveBootstrapTarget(bootstrap, out var target, out var mode, out var definitions,
                    out string configurationError))
                return new[] { configurationError };
            return ValidateBootstrap(bootstrap, buildScenePaths, settings, settingsWereSupplied, target, mode, definitions);
        }

        private static IReadOnlyList<string> ValidateBootstrap(BootstrapSystem bootstrap, IReadOnlyList<string> buildScenePaths,
            AddressableAssetSettings settings, bool settingsWereSupplied, SceneTarget target, LoadSceneMode mode,
            IReadOnlyList<SceneTransitionDefinition> definitions)
        {
            var errors = new List<string>();
            try
            {
                bootstrap.ValidateConfiguration();
                if (bootstrap.SceneRoot != null) errors.AddRange(ValidateInstallers(bootstrap.SceneRoot));
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
            if (target.Source == SceneSource.BuildScene && !buildScenePaths.Contains(target.ScenePath))
                errors.Add("First game scene must be included in the build: " + target.ScenePath);
            if (target.Source == SceneSource.Addressable || definitions.Any(item => item.Target.Source == SceneSource.Addressable))
            {
                if (!settingsWereSupplied) settings = ResolveCurrentSettings();
            }
            if (target.Source == SceneSource.Addressable)
                errors.AddRange(bootstrap.Settings == null
                    ? ValidateAddressableTarget(bootstrap, settings)
                    : ValidateAddressableTarget(target, settings));
            var previews = new List<Scene>();
            try
            {
                errors.AddRange(ValidateTransitionDefinitions(definitions, buildScenePaths, settings,
                    bootstrap.gameObject.scene.path, bootstrap.SceneRoot, previews));
                var game = GetOrOpenPreview(target.ScenePath, previews, errors);
                if (game.IsValid())
                {
                    errors.AddRange(ValidateGameScene(game));
                    if (bootstrap.SceneRoot is SingletonSceneRoot && game.GetRootGameObjects()
                        .Any(go => go.GetComponentInChildren<SingletonSceneRoot>(true) != null))
                        errors.Add("Bootstrap and game scenes cannot both own SingletonSceneRoot.Instance; use SceneOwnedRoot for the game.");
                }
            }
            finally
            {
                foreach (var preview in previews) if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
            return errors;
        }

        private static bool TryResolveBootstrapTarget(BootstrapSystem bootstrap, out SceneTarget target,
            out LoadSceneMode mode, out IReadOnlyList<SceneTransitionDefinition> definitions, out string error)
        {
            target = default;
            mode = LoadSceneMode.Additive;
            definitions = Array.Empty<SceneTransitionDefinition>();
            error = null;
            try
            {
                var settings = bootstrap.Settings;
                string firstId = bootstrap.FirstTransitionId;
                if (settings == null && string.IsNullOrWhiteSpace(firstId))
                {
                    target = bootstrap.FirstSceneTarget;
                    mode = bootstrap.LoadMode;
                    return true;
                }
                if (settings == null || string.IsNullOrWhiteSpace(firstId))
                {
                    error = "Select transition settings and a FirstEntry definition ID together.";
                    return false;
                }
                definitions = settings.CreateSnapshot();
                var first = definitions.SingleOrDefault(definition => definition.Id == firstId);
                if (first == null || first.Kind != SceneTransitionKind.FirstEntry)
                {
                    error = "Bootstrap requires an existing FirstEntry definition ID.";
                    return false;
                }
                target = first.Target;
                mode = first.Mode;
                return true;
            }
            catch (Exception exception)
            {
                error = "Invalid Bootstrap transition settings: " + exception.Message;
                return false;
            }
        }

        private static bool UsesAddressables(BootstrapSystem bootstrap)
        {
            try
            {
                if (bootstrap.Settings != null)
                    return bootstrap.Settings.CreateSnapshot().Any(definition => definition.Target.Source == SceneSource.Addressable);
                return string.IsNullOrWhiteSpace(bootstrap.FirstTransitionId) && bootstrap.Source == SceneSource.Addressable;
            }
            catch
            {
                return false;
            }
        }

        private static IReadOnlyList<string> ValidateTransitionDefinitions(IReadOnlyList<SceneTransitionDefinition> definitions,
            IReadOnlyList<string> buildScenePaths, AddressableAssetSettings addressableSettings, string bootstrapScenePath,
            MonoBehaviour liveCommonRoot, List<Scene> previews)
        {
            var errors = new List<string>();
            if (definitions == null) return errors;
            var conditionIdsByPath = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var inspected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                SceneTarget target;
                try
                {
                    target = definition.Target;
                    target.Validate();
                }
                catch (Exception exception)
                {
                    errors.Add("Definition " + definition.Id + " has an invalid destination: " + exception.Message);
                    continue;
                }
                if (target.Source == SceneSource.BuildScene && !buildScenePaths.Contains(target.ScenePath))
                    errors.Add("Build Scene destination must be included in the actual Player scene list: " + target.ScenePath);
                if (target.Source == SceneSource.Addressable)
                    errors.AddRange(ValidateAddressableTarget(target, addressableSettings));
                if (definition.Kind != SceneTransitionKind.FirstEntry)
                    ValidateDefinitionScene(definition.SourceScenePath, false, previews, inspected, conditionIdsByPath, errors);
                ValidateDefinitionScene(target.ScenePath, false, previews, inspected, conditionIdsByPath, errors);
            }

            if (liveCommonRoot == null && !string.IsNullOrEmpty(bootstrapScenePath))
                ValidateDefinitionScene(bootstrapScenePath, true, previews, inspected, conditionIdsByPath, errors);
            if (liveCommonRoot != null)
                conditionIdsByPath[bootstrapScenePath] = ReadRootConditionIds(liveCommonRoot, bootstrapScenePath, errors);

            var childPaths = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var definition in definitions.Where(item => item.Kind == SceneTransitionKind.AddDerived))
            {
                if (!childPaths.TryGetValue(definition.SourceScenePath, out var children))
                    childPaths.Add(definition.SourceScenePath, children = new List<string>());
                children.Add(definition.Target.ScenePath);
            }
            foreach (var definition in definitions)
            {
                var possibleRoots = new HashSet<string>(StringComparer.Ordinal);
                if (definition.Kind == SceneTransitionKind.FirstEntry)
                    possibleRoots.Add(bootstrapScenePath);
                else if (definition.Kind == SceneTransitionKind.AddDerived)
                    possibleRoots.Add(definition.SourceScenePath);
                else if (definition.Kind == SceneTransitionKind.ReplacePrimary && !string.IsNullOrEmpty(definition.SourceScenePath))
                {
                    possibleRoots.Add(definition.SourceScenePath);
                    AddDescendants(definition.SourceScenePath, childPaths, possibleRoots);
                }
                else if (definition.Kind == SceneTransitionKind.RemoveDerived)
                {
                    possibleRoots.Add(definition.SourceScenePath);
                    possibleRoots.Add(definition.Target.ScenePath);
                    AddDescendants(definition.Target.ScenePath, childPaths, possibleRoots);
                    foreach (var add in definitions.Where(item => item.Kind == SceneTransitionKind.AddDerived && item.Target.ScenePath == definition.Target.ScenePath))
                        possibleRoots.Add(add.SourceScenePath);
                }
                foreach (string requiredId in definition.RequiredConditionIds)
                    if (!possibleRoots.Any(path => conditionIdsByPath.TryGetValue(path, out var ids) && ids.Contains(requiredId)))
                        errors.Add("Required condition ID is not declared on a potentially affected root: " + requiredId + " (" + definition.Id + ").");
            }
            return errors;
        }

        private static void ValidateDefinitionScene(string path, bool bootstrap, List<Scene> previews,
            HashSet<string> inspected, Dictionary<string, HashSet<string>> conditionIdsByPath, List<string> errors)
        {
            if (string.IsNullOrEmpty(path) || !inspected.Add(path)) return;
            var scene = GetOrOpenPreview(path, previews, errors);
            if (!scene.IsValid()) return;
            if (!bootstrap) errors.AddRange(ValidateGameScene(scene));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var roots = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(component => component is ISceneRoot).ToArray();
            foreach (var root in roots)
                ids.UnionWith(ReadRootConditionIds(root, path, errors));
            conditionIdsByPath[path] = ids;
        }

        private static void AddDescendants(string start, Dictionary<string, List<string>> childPaths, HashSet<string> visited)
        {
            var pending = new Stack<string>();
            pending.Push(start);
            var expanded = new HashSet<string>(StringComparer.Ordinal);
            while (pending.Count != 0)
            {
                string current = pending.Pop();
                visited.Add(current);
                if (!expanded.Add(current) || !childPaths.TryGetValue(current, out var children)) continue;
                foreach (string child in children) pending.Push(child);
            }
        }

        private static Scene GetOrOpenPreview(string path, List<Scene> previews, List<string> errors)
        {
            var scene = previews.FirstOrDefault(candidate => candidate.path == path);
            if (scene.IsValid()) return scene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                errors.Add("Missing transition scene asset: " + path);
                return default;
            }
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(path);
                previews.Add(scene);
                return scene;
            }
            catch (Exception exception)
            {
                errors.Add("Could not inspect transition scene " + path + ": " + exception.Message);
                return default;
            }
        }

        private static HashSet<string> ReadRootConditionIds(MonoBehaviour root, string path, List<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var condition in root.GetComponents<SceneTransitionCondition>())
            {
                try
                {
                    string id = condition.ConditionId;
                    if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                        errors.Add(path + ": duplicate root condition ID or empty condition ID.");
                }
                catch (Exception exception)
                {
                    errors.Add(path + ": reading ConditionId failed: " + exception.Message);
                }
            }
            return ids;
        }

        private static IReadOnlyList<string> ValidateAddressableTarget(BootstrapSystem bootstrap, AddressableAssetSettings settings)
        {
            var errors = new List<string>();
            if (settings == null)
            {
                errors.Add("Addressables settings are missing; create/configure project settings explicitly.");
                return errors;
            }
            var reference = bootstrap.SceneReference;
            return ValidateAddressableTarget(bootstrap.FirstSceneTarget, settings, reference, bootstrap.AddressableKey);
        }

        private static IReadOnlyList<string> ValidateAddressableTarget(SceneTarget target, AddressableAssetSettings settings)
            => ValidateAddressableTarget(target, settings, null, target.AddressableKey);

        private static IReadOnlyList<string> ValidateAddressableTarget(SceneTarget target, AddressableAssetSettings settings,
            UnityEngine.AddressableAssets.AssetReference reference, string addressableKey)
        {
            var errors = new List<string>();
            if (settings == null)
            {
                errors.Add("Addressables settings are missing; create/configure project settings explicitly.");
                return errors;
            }
            string path = target.ScenePath;
            bool hasReference = reference != null && !string.IsNullOrEmpty(reference.AssetGUID);
            if (hasReference && !string.IsNullOrWhiteSpace(addressableKey))
            {
                errors.Add("Select an Addressables key or scene reference, not both.");
                return errors;
            }
            if (hasReference && (!string.IsNullOrEmpty(reference.SubObjectName) ||
                !string.Equals(reference.RuntimeKey.ToString(), reference.AssetGUID, StringComparison.Ordinal)))
            {
                errors.Add("Scene AssetReference cannot target a sub-object; select the scene asset itself.");
                return errors;
            }
            string guid = hasReference ? reference.AssetGUID : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || AssetDatabase.GUIDToAssetPath(guid) != path)
            {
                errors.Add("Addressables target GUID must identify the selected scene: " + path);
                return errors;
            }
            var entry = settings.FindAssetEntry(guid, false);
            if (entry == null || !entry.IsScene || entry.AssetPath != path)
            {
                errors.Add("Selected game scene must have an explicit Addressables scene entry: " + path);
                return errors;
            }
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
                var addressMatches = matches.Where(candidate => candidate.address == addressableKey).ToArray();
                bool selectedGuid = string.Equals(addressableKey, guid, StringComparison.Ordinal);
                var schema = entry.parentGroup == null ? null : entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
                bool guidAvailable = schema != null && schema.IncludeGUIDInCatalog;
                bool addressAvailable = schema != null && schema.IncludeAddressInCatalog &&
                    addressMatches.Length == 1 && addressMatches[0].guid == guid;
                if (addressMatches.Any(candidate => candidate.guid != guid) || addressMatches.Length > 1 ||
                    (selectedGuid ? !guidAvailable && !addressAvailable : !addressAvailable))
                    errors.Add("Addressables key must uniquely identify the selected scene: " + addressableKey);
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
