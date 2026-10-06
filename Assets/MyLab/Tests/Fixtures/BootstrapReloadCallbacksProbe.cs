using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    /// <summary>Observes actual preparation and reveal on each reload-harness entry without owning scene lifetime.</summary>
    public sealed class BootstrapReloadCallbacksProbe : SceneTransitionCallbacks
    {
        public int CoverCount;
        public int ConfigurationCount;
        public int PresentationCount;
        public int RevealCount;
        public int FailureCount;

        public override UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            ++CoverCount;
            return UniTask.CompletedTask;
        }

        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            if (!root.IsReady || root.IsPrepared || root.RootObject.scene != scene)
                throw new InvalidOperationException("Reload candidate injection did not precede preparation.");
            ++ConfigurationCount;
            return UniTask.CompletedTask;
        }

        public override UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            if (!root.IsPrepared || root.RootObject.scene != scene)
                throw new InvalidOperationException("Reload candidate presentation was not prepared.");
            ++PresentationCount;
            return UniTask.CompletedTask;
        }

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            ++RevealCount;
            return UniTask.CompletedTask;
        }

        public override void OnFailure(Exception exception) => ++FailureCount;
    }
}
