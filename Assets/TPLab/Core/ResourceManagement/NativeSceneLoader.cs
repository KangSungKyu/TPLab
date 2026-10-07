using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Loads enabled Player build scenes and transfers ownership of the actual native Scene instance.</summary>
    public sealed class NativeSceneLoader : ISceneProgressLoader
    {
        /// <summary>Rejects malformed/non-build targets and scenes absent from the enabled Player build list.</summary>
        public void Validate(SceneTarget target)
        {
            LoadedScene.EnsureMainThread();
            target.Validate();
            if (target.Source != SceneSource.BuildScene)
            {
                throw new ArgumentException("Native loader requires a Build Scene target.", nameof(target));
            }
            if (!Application.CanStreamedLevelBeLoaded(target.ScenePath))
            {
                throw new InvalidOperationException("The game scene must be enabled in the Player build scene list.");
            }
        }

        /// <summary>Loads/activates one real instance without cancellation; each result exclusively owns its native unload.</summary>
        public UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode)
        {
            return LoadAsync(target, mode, null);
        }

        /// <summary>Loads with optional synchronous progress while preserving exclusive native result ownership.</summary>
        public async UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode, SceneLoadProgressObserver observer)
        {
            Validate(target);
            ValidateMode(mode);
            if (SceneManager.GetSceneByPath(target.ScenePath).isLoaded)
            {
                throw new InvalidOperationException("The scene is already loaded; concurrent copies are not supported.");
            }
            Scene loaded = default;
            void OnLoaded(Scene scene, LoadSceneMode loadedMode)
            {
                if (scene.path == target.ScenePath && loadedMode == mode && !loaded.IsValid())
                {
                    loaded = scene;
                }
            }
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                var operation = SceneManager.LoadSceneAsync(target.ScenePath, mode);
                if (operation == null)
                {
                    throw new InvalidOperationException("Unity did not start the scene load.");
                }
                observer?.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 0f));
                var progress = observer == null ? null : Cysharp.Threading.Tasks.Progress.Create<float>(ratio =>
                    observer.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, ratio)));
                await operation.ToUniTask(progress: progress);
            }
            finally
            {
                SceneManager.sceneLoaded -= OnLoaded;
            }
            var result = new LoadedScene(target, loaded, () => UnloadAsync(loaded));
            observer?.Report(new SceneLoadProgress(SceneLoadStage.LoadingScene, 1f));
            return result;
        }

        private static async UniTask UnloadAsync(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }
            if (SceneManager.sceneCount <= 1)
            {
                throw new InvalidOperationException("Unity cannot unload the last normal scene.");
            }
            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                throw new InvalidOperationException("Unity did not start the owned scene unload.");
            }
            await operation.ToUniTask();
        }

        internal static void ValidateMode(LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single && mode != LoadSceneMode.Additive)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}
