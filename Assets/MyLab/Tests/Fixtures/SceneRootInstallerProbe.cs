using System;
using System.Collections.Generic;
using MyLab.Core.Lifecycle;

namespace MyLab.Core.Tests
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
