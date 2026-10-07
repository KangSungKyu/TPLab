using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;

namespace TPLab.Core.Tests
{
    public sealed class SceneRootInstallerProbe : SceneRootInstaller
    {
        public static readonly List<string> Trace = new List<string>();
        public string Id;
        public bool FailInstall;
        public bool FailUninstall;
        public int InstallCount;
        public int UninstallCount;
        public ISceneRoot InjectedRoot;
        public bool WasReadyDuringInstall;
        [NonSerialized] public UniTaskCompletionSource PrepareGate;
        [NonSerialized] public UniTaskCompletionSource ReleaseGate;
        public bool FailPrepare;
        public bool FailRelease;
        public bool IgnorePrepareCancellation;
        public int PrepareCount;
        public int ReleaseCount;
        [NonSerialized] public Action Preparing;
        [NonSerialized] public Action Releasing;

        public override async UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            PrepareCount++;
            Preparing?.Invoke();
            Trace.Add("prepare:" + Id);
            if (PrepareGate != null)
            {
                if (IgnorePrepareCancellation)
                {
                    await PrepareGate.Task;
                }
                else
                {
                    await PrepareGate.Task.AttachExternalCancellation(cancellationToken);
                }
            }
            if (FailPrepare)
            {
                throw new InvalidOperationException("root-prepare:" + Id);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        public override async UniTask ReleaseAsync(ISceneRoot root)
        {
            ReleaseCount++;
            Releasing?.Invoke();
            Trace.Add("release:" + Id);
            if (ReleaseGate != null)
            {
                await ReleaseGate.Task;
            }
            if (FailRelease)
            {
                throw new InvalidOperationException("root-release:" + Id);
            }
        }

        public override void Install(ISceneRoot root)
        {
            InstallCount++;
            InjectedRoot = root;
            WasReadyDuringInstall = root.IsReady;
            Trace.Add("install:" + Id);
            if (FailInstall)
            {
                throw new InvalidOperationException("root-install:" + Id);
            }
        }

        public override void Uninstall(ISceneRoot root)
        {
            UninstallCount++;
            InjectedRoot = null;
            Trace.Add("uninstall:" + Id);
            if (FailUninstall)
            {
                throw new InvalidOperationException("root-uninstall:" + Id);
            }
        }
    }
}
