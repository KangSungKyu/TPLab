using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Main-thread scene backend boundary. Owns native work until an exclusive result is returned.</summary>
    public interface ISceneLoader
    {
        /// <summary>Rejects unsupported or invalid targets synchronously before caller side effects.</summary>
        void Validate(SceneTarget target);

        /// <summary>
        /// Loads and activates one real scene instance in Single or Additive mode, transferring exclusive unload ownership.
        /// Native completion cannot be cancelled; the caller must retain and clean up late results.
        /// A failed load throws and releases its partial backend resources; it never returns an invalid scene as success.
        /// The manager must retain the result before validating its actual Scene path against the requested asset.
        /// </summary>
        UniTask<LoadedScene> LoadAsync(SceneTarget target, LoadSceneMode mode);
    }
}
