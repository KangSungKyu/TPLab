using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.SceneManagement;
using UnityEngine.SceneManagement;

namespace MyLab.Core.Tests
{
    public sealed class BootstrapCallbacksProbe : BootstrapCallbacks
    {
        public int CoverCount;
        public int RevealCount;
        public int FailureCount;
        public bool FailConfigure;
        public bool FailGamePrepare;
        public bool FailReveal;
        public UniTaskCompletionSource GamePreparation;
        public SceneRootInstallerProbe GameInstaller;

        public override UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            ++CoverCount;
            return UniTask.CompletedTask;
        }

        public override UniTask ConfigureGameAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            if (FailConfigure) throw new InvalidOperationException("bootstrap-configure");
            GameInstaller = root.RootObject.GetComponent<SceneRootInstallerProbe>();
            GameInstaller.PrepareGate = GamePreparation;
            GameInstaller.FailPrepare = FailGamePrepare;
            return UniTask.CompletedTask;
        }

        public override UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            ++RevealCount;
            if (FailReveal) throw new InvalidOperationException("bootstrap-reveal");
            return UniTask.CompletedTask;
        }

        public override void OnFailure(Exception exception) => ++FailureCount;
    }
}
