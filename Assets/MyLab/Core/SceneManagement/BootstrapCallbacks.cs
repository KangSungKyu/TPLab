using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Project-owned presentation and explicit consumer injection; keep this component in Bootstrap.</summary>
    public abstract class BootstrapCallbacks : MonoBehaviour
    {
        /// <summary>Completes after covering the display and blocking gameplay input.</summary>
        public virtual UniTask ShowCoverAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        /// <summary>Connects borrowed shared services to consumers before asynchronous game root preparation.</summary>
        public virtual UniTask ConfigureGameAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;
        /// <summary>Completes after content is ready; gameplay must respect this explicit gate.</summary>
        public virtual UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;
        /// <summary>Completes after revealing the prepared game and permitting input.</summary>
        public virtual UniTask HideCoverAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        /// <summary>Receives failure or cancellation; the caller still receives the error.</summary>
        public virtual void OnFailure(Exception exception) { }
    }
}
