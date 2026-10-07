using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyLab.Core.ResourceManagement
{
    /// <summary>Optional scene backend progress; existing ISceneLoader implementations remain valid without this extension.</summary>
    public interface ISceneProgressLoader : ISceneLoader
    {
        /// <summary>
        /// Loads under the existing ownership contract while reporting backend stages synchronously.
        /// A null or disposed observer disables notifications. Callback failure is retained in observer.Failure;
        /// the caller must first retain the successful result, then inspect failure and clean up that result.
        /// Native failures still propagate and release untransferred backend resources.
        /// </summary>
        UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode, SceneLoadProgressObserver observer);
    }
}
