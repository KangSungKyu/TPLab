using System;
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
        public int FailureCount;
        public Exception Failure;
        public Action Configuring;
        public Action Revealing;
        public Action Presenting;
        public bool FailFailureCallback;
        public bool ConfigurationWasBeforePreparation;
        public bool PresentationReceivedPreparedRoot;
        public ISceneRoot PresentationRoot;
        public UniTaskCompletionSource PresentationGate;
        public UniTaskCompletionSource CoverGate;

        public override async UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            ++CoverCount;
            if (CoverGate != null) await CoverGate.Task.AttachExternalCancellation(cancellationToken);
        }

        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            ConfigurationWasBeforePreparation = root.IsReady && !root.IsPrepared && root.RootObject.scene == scene;
            Configuring?.Invoke();
            return UniTask.CompletedTask;
        }

        public override UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            PresentationRoot = root;
            PresentationReceivedPreparedRoot = root.IsPrepared && root.RootObject.scene == scene;
            Presenting?.Invoke();
            return PresentationGate == null ? UniTask.CompletedTask : PresentationGate.Task.AttachExternalCancellation(cancellationToken);
        }

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            ++RevealCount;
            Revealing?.Invoke();
            return UniTask.CompletedTask;
        }

        public override void OnFailure(Exception exception)
        {
            ++FailureCount;
            Failure = exception;
            if (FailFailureCallback) throw new InvalidOperationException("failure-callback");
        }
    }
}
