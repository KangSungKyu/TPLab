using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Owns a ResourceManager on either scene-root host. Place before installers that consume its borrowed assets.</summary>
    [AddComponentMenu("TPLab/Resource Manager Installer")]
    public sealed class ResourceManagerInstaller : SceneRootInstaller
    {
        /// <summary>The installed owner for explicit reference injection; null before installation and after uninstall.</summary>
        public ResourceManager Resources { get; private set; }

        /// <summary>Creates this installer's scope. Consumers receive Resources through their own explicit references.</summary>
        /// <exception cref="InvalidOperationException">Already installed.</exception>
        public override void Install(ISceneRoot root)
        {
            if (Resources != null)
            {
                throw new InvalidOperationException("The resource scope is already installed.");
            }
            Resources = new ResourceManager();
        }

        /// <summary>Awaits Addressables readiness only. Later consumer installers must await their required assets.</summary>
        /// <param name="root">Owning host.</param>
        /// <param name="cancellationToken">Root preparation wait cancellation.</param>
        /// <returns>Initialization completion; resource errors and cancellation propagate to the root.</returns>
        public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
            => Resources.InitializeAsync(cancellationToken);

        /// <summary>Drains native work after later consumer installers have released their pools and other borrowed assets.</summary>
        /// <returns>Uncancelled cleanup completion, or an already completed task after partial installation.</returns>
        public override UniTask ReleaseAsync(ISceneRoot root) => Resources?.ShutdownAsync() ?? UniTask.CompletedTask;

        /// <summary>Clears the injected owner and performs immediate idempotent fallback cleanup, including on Unity destruction.</summary>
        public override void Uninstall(ISceneRoot root)
        {
            var resources = Resources;
            Resources = null;
            resources?.Dispose();
        }
    }
}
