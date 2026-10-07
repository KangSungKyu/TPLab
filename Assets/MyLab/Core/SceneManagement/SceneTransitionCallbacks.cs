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
        /// <summary>Completes after the cover is displayed and gameplay input is blocked. Loading presentation calls this twice; retain one transition lease.</summary>
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

        /// <summary>Opt-in policy sampled once at execution start; removal always uses the existing cover flow.</summary>
        public virtual bool UsesLoadingPresentation(SceneLoadingContext context) => false;

        /// <summary>Prepares project-owned loading UI while covered; token is the transition owner's lifetime.</summary>
        public virtual UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>Shows prepared loading UI and hides only the cover; retain gameplay blocking.</summary>
        public virtual UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>Receives synchronous main-thread snapshots; never starts or waits for its own transition.</summary>
        public virtual void ReportLoadingProgress(SceneTransitionProgress progress) { }

        /// <summary>
        /// Automatic continuation by default. Manual UI must await fresh operation-specific input after preparation,
        /// bind its own destruction to failure/cancellation, and remove listeners in cleanup. Caller cancellation
        /// affects only caller observation; the supplied token belongs to the transition owner.
        /// </summary>
        public virtual UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>Releases only this operation's UI/listeners under cover, without abandoning begun cleanup on cancellation.</summary>
        public virtual UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context) => UniTask.CompletedTask;

        /// <summary>
        /// Reports execution failure or owner cancellation without converting it into success.
        /// Acknowledging a message does not authorize reveal. Do not await or restart the current transition here.
        /// </summary>
        public virtual void OnFailure(Exception exception) { }
    }
}
