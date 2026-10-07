using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Loads real Addressables SceneInstances without sharing scene results through the asset cache.</summary>
    public sealed class AddressableSceneLoader : ISceneProgressLoader
    {
        /// <summary>Rejects malformed or non-Addressable targets; catalog/key resolution occurs during load.</summary>
        public void Validate(SceneTarget target)
        {
            LoadedScene.EnsureMainThread();
            target.Validate();
            if (target.Source != SceneSource.Addressable)
            {
                throw new ArgumentException("Addressables loader requires an Addressable target.", nameof(target));
            }
        }

        /// <summary>Resolves a unique scene location, loads/activates it, and transfers private handle ownership to its result.</summary>
        public UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)
        {
            return LoadAsync(target, mode, null);
        }

        /// <summary>Loads with optional synchronous PercentComplete observation, preserving private handle ownership.</summary>
        public async UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode, SceneLoadProgressObserver observer)
        {
            Validate(target);
            NativeSceneLoader.ValidateMode(mode);
            if (SceneManager.GetSceneByPath(target.ScenePath).isLoaded)
            {
                throw new InvalidOperationException("The scene is already loaded; concurrent copies are not supported.");
            }
            var locations = Addressables.LoadResourceLocationsAsync(target.AddressableKey, typeof(SceneInstance));
            AsyncOperationHandle<SceneInstance> handle = default;
            try
            {
                observer?.Report(new SceneLoadProgress(SceneLoadStage.ResolvingTarget, 0f));
                // UniTask 2.5.11 reports download-byte ratio here; sample operation PercentComplete instead.
                var resolvingProgress = observer == null ? null : Cysharp.Threading.Tasks.Progress.Create<float>(_ =>
                    observer.Report(new SceneLoadProgress(SceneLoadStage.ResolvingTarget, locations.PercentComplete)));
                var resolved = await locations.ToUniTask(progress: resolvingProgress);
                if (resolved == null || resolved.Count != 1)
                {
                    throw new InvalidOperationException("An Addressables scene key must resolve to exactly one scene location.");
                }
                observer?.Report(new SceneLoadProgress(SceneLoadStage.ResolvingTarget, 1f));
                handle = Addressables.LoadSceneAsync(resolved[0], mode, activateOnLoad: true);
                observer?.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0f));
                var loadingProgress = observer == null ? null : Cysharp.Threading.Tasks.Progress.Create<float>(_ =>
                    observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, handle.PercentComplete)));
                var instance = await handle.ToUniTask(progress: loadingProgress);
                // The manager retains the result before checking the actual path, including a last Single scene it cannot unload.
                var result = new LoadedScene(target, instance.Scene, () => UnloadAsync(handle, instance.Scene));
                observer?.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 1f));
                return result;
            }
            catch
            {
                // No result was transferred: also release successful-but-invalid provider results rejected by LoadedScene.
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
            finally
            {
                if (locations.IsValid())
                {
                    Addressables.Release(locations);
                }
            }
        }

        private static async UniTask UnloadAsync(AsyncOperationHandle<SceneInstance> handle, Scene scene)
        {
            // An external Single/unload may already have released Addressables' scene handle.
            if (!handle.IsValid())
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    throw new InvalidOperationException("The loaded scene lost its Addressables ownership handle.");
                }
                return;
            }
            if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount <= 1)
            {
                throw new InvalidOperationException("Unity cannot unload the last normal scene.");
            }
            var operation = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: false);
            try
            {
                await operation.ToUniTask();
                // SceneProvider publishes completion before releasing its load handle and dependencies.
                // Let that callback return before publishing our shared unload completion.
                await UniTask.Yield();
            }
            finally
            {
                if (operation.IsValid())
                {
                    Addressables.Release(operation);
                }
            }
        }
    }
}
