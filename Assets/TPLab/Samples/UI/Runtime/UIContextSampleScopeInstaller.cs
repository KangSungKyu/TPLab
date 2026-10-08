using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;

namespace TPLab.Samples.UI
{
    /// <summary>Project sample wiring between Input.Install and UI.Prepare; root retains installer ownership/order.</summary>
    public sealed class UIContextSampleScopeInstaller : SceneRootInstaller
    {
        [SerializeField] private UIContextSampleController _controller;

        /// <summary>Assigns the borrowed project controller before root activation.</summary>
        public void Configure(UIContextSampleController controller) => _controller = controller;
        /// <inheritdoc />
        public override void Install(ISceneRoot root) => _controller.InstallScope(root);
        /// <inheritdoc />
        public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _controller.BindScope();
            return UniTask.CompletedTask;
        }
        /// <inheritdoc />
        public override UniTask ReleaseAsync(ISceneRoot root)
        {
            _controller.ReleaseScope();
            return UniTask.CompletedTask;
        }
        /// <inheritdoc />
        public override void Uninstall(ISceneRoot root) => _controller.UninstallScope();
    }
}