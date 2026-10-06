using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Compatibility adapter for existing Bootstrap callbacks. New code uses SceneTransitionCallbacks.</summary>
    [Obsolete("Use SceneTransitionCallbacks and override ConfigureSceneAsync. Existing ConfigureGameAsync overrides remain supported.")]
    public abstract class BootstrapCallbacks : SceneTransitionCallbacks
    {
        /// <inheritdoc />
        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) =>
            ConfigureGameAsync(scene, root, cancellationToken);

        /// <summary>Legacy injection hook invoked by ConfigureSceneAsync for existing subclasses.</summary>
        public virtual UniTask ConfigureGameAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) => UniTask.CompletedTask;
    }
}
