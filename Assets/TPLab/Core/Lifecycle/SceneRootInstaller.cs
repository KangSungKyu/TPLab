using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    /// <summary>Adapts project-specific manager creation and reference injection to a selected root.</summary>
    public abstract class SceneRootInstaller : MonoBehaviour
    {
        /// <summary>Creates owned services and injects explicit references. Root readiness is false here.</summary>
        public abstract void Install(ISceneRoot root);
        /// <summary>Clears injected references and releases owned resources, including partial installation.</summary>
        public abstract void Uninstall(ISceneRoot root);
        /// <summary>
        /// Awaits owned systems, assets, and content readiness after injection. Defaults to completed.
        /// Honour the root lifetime token and prevent late work from touching released services.
        /// A host may stop waiting after its shutdown deadline and still release/uninstall services;
        /// the deadline cannot stop this method's non-cooperative work.
        /// </summary>
        public virtual UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;
        /// <summary>
        /// Awaits graceful service shutdown before Uninstall. Defaults to completed and must handle partial preparation.
        /// Cleanup cannot be cancelled by a scene caller. Never await this root's ShutdownAsync from this hook.
        /// Uninstall must still support immediate teardown when Unity destruction skips this hook.
        /// Complete within the host's independent unscaled release deadline; expiry does not stop this hook.
        /// Remaining releases and Uninstall still run, so late work must not access already released resources.
        /// Late completion failures are observed but cannot change the shutdown result already published.
        /// </summary>
        public virtual UniTask ReleaseAsync(ISceneRoot root) => UniTask.CompletedTask;
    }
}
