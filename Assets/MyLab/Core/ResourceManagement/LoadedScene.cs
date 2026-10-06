using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyLab.Core.ResourceManagement
{
    /// <summary>Exclusive main-thread owner of one loaded scene instance and its private backend release operation.</summary>
    public sealed class LoadedScene
    {
        private readonly Func<UniTask> _unloadAsync;
        private UniTaskCompletionSource _unload;
        /// <summary>Asset and backend that produced this instance.</summary>
        public SceneTarget Target { get; }
        /// <summary>The actual loaded scene, never a path-based lookup of another instance.</summary>
        public Scene Scene { get; }
        /// <summary>True only after the shared unload succeeds and the scene is no longer loaded.</summary>
        public bool IsUnloaded { get; private set; }

        /// <summary>
        /// Takes exclusive ownership of a valid, loaded Scene and its uncancelled unload delegate.
        /// Rejects invalid targets, invalid/unloaded scenes, and null unload delegates.
        /// The delegate must unload this exact instance and release only its own backend resources; handles stay private.
        /// </summary>
        public LoadedScene(SceneTarget target, Scene scene, Func<UniTask> unloadAsync)
        {
            EnsureMainThread();
            target.Validate();
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A real loaded scene instance is required.", nameof(scene));
            _unloadAsync = unloadAsync ?? throw new ArgumentNullException(nameof(unloadAsync));
            Target = target;
            Scene = scene;
        }

        /// <summary>
        /// Starts uncancelled unload once; concurrent and later callers observe the same completion or failure.
        /// A failure remains observable and does not mark the instance unloaded; no implicit retry is performed.
        /// </summary>
        public UniTask UnloadAsync()
        {
            EnsureMainThread();
            if (_unload == null)
            {
                _unload = new UniTaskCompletionSource();
                RunUnloadAsync().Forget();
            }
            return _unload.Task;
        }

        private async UniTask RunUnloadAsync()
        {
            try
            {
                await _unloadAsync();
                EnsureMainThread();
                if (Scene.IsValid() && Scene.isLoaded)
                    throw new InvalidOperationException("The backend completed unload while the owned scene remains loaded.");
                IsUnloaded = true;
                _unload.TrySetResult();
            }
            catch (Exception failure)
            {
                _unload.TrySetException(failure);
            }
        }

        internal static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("Scene resources must be used on Unity's main thread.");
        }
    }
}
