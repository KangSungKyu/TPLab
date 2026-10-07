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
        public Action<SceneTransitionProgress> ProgressReported;
        public bool LoadingEnabled;
        public bool Covered;
        public int LoadingPrepareCount, LoadingRevealCount, LoadingReleaseCount, ProceedCount;
        public UniTaskCompletionSource ProceedGate;
        public Action LoadingPreparing, LoadingRevealing, Proceeding, LoadingReleasing;
        public SceneLoadingContext LoadingContext;
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
            Covered = true;
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
            Covered = false;
            Revealing?.Invoke();
            return UniTask.CompletedTask;
        }

        public override bool UsesLoadingPresentation(SceneLoadingContext context) => LoadingEnabled;
        public override UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            LoadingContext = context;
            LoadingPrepareCount++;
            LoadingPreparing?.Invoke();
            return UniTask.CompletedTask;
        }
        public override UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            LoadingRevealCount++;
            Covered = false;
            LoadingRevealing?.Invoke();
            return UniTask.CompletedTask;
        }
        public override UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            ProceedCount++;
            Proceeding?.Invoke();
            return ProceedGate == null ? UniTask.CompletedTask : ProceedGate.Task.AttachExternalCancellation(cancellationToken);
        }
        public override UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context)
        {
            LoadingReleaseCount++;
            LoadingReleasing?.Invoke();
            return UniTask.CompletedTask;
        }

        public override void ReportLoadingProgress(SceneTransitionProgress progress) => ProgressReported?.Invoke(progress);

        public override void OnFailure(Exception exception)
        {
            ++FailureCount;
            Failure = exception;
            if (FailFailureCallback) throw new InvalidOperationException("failure-callback");
        }
    }
}
