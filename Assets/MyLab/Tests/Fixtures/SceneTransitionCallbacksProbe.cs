using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    public sealed class SceneTransitionCallbacksProbe : SceneTransitionCallbacks
    {
        public int CoverCount;
        public int RevealCount;
        public bool ConfigurationWasBeforePreparation;
        public bool PresentationReceivedPreparedRoot;
        public ISceneRoot PresentationRoot;
        public UniTaskCompletionSource PresentationGate;

        public override UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            ++CoverCount;
            return UniTask.CompletedTask;
        }

        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            ConfigurationWasBeforePreparation = root.IsReady && !root.IsPrepared && root.RootObject.scene == scene;
            return UniTask.CompletedTask;
        }

        public override UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            PresentationRoot = root;
            PresentationReceivedPreparedRoot = root.IsPrepared && root.RootObject.scene == scene;
            return PresentationGate == null ? UniTask.CompletedTask : PresentationGate.Task.AttachExternalCancellation(cancellationToken);
        }

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            ++RevealCount;
            return UniTask.CompletedTask;
        }
    }
}
