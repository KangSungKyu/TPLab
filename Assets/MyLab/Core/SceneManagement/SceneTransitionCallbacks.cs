using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>
    /// Project-owned scene injection and presentation hooks shared by first entry and later transitions.
    /// The component and its UI dependencies must outlive every scene released by the transition owner.
    /// Hooks do not own scene loading, root shutdown, or automatic gameplay activation.
    /// </summary>
    public abstract class SceneTransitionCallbacks : MonoBehaviour
    {
        /// <summary>Completes after the cover is displayed and gameplay input is blocked.</summary>
        public virtual UniTask ShowCoverAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// Connects borrowed services to installed consumers before root preparation.
        /// Awake/Install may already have run. Do not reconfigure an active root or release borrowed services.
        /// </summary>
        public virtual UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// Receives a prepared root and completes after project presentation is ready.
        /// Root readiness is distinct from permission to run gameplay and from successful reveal.
        /// </summary>
        public virtual UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// Completes after revealing the prepared scene. Release only transition-owned input blocking;
        /// another modal may still block gameplay. A failure must remain visible to the transition owner.
        /// </summary>
        public virtual UniTask HideCoverAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// Reports execution failure or owner cancellation without converting it into success.
        /// Acknowledging a message does not authorize reveal. Do not await or restart the current transition here.
        /// </summary>
        public virtual void OnFailure(Exception exception) { }
    }
}
