using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;

namespace TPLab.Samples.SceneTransitions
{
    /// <summary>Owns asynchronous sample content preparation and its separate presentation signal.</summary>
    public sealed class SampleSceneInstaller : SceneRootInstaller
    {
        [SerializeField] private SampleSceneConsumer _consumer;
        private CancellationTokenSource _presentationLifetime;
        private UniTask _presentationTask;
        public void Configure(SampleSceneConsumer consumer) => _consumer = consumer;
        public override void Install(ISceneRoot root) => _consumer.Install(root);
        public override async UniTask PrepareAsync(ISceneRoot root, CancellationToken token)
        {
            if (_consumer.Controller == null) throw new InvalidOperationException("Scene callback injection is required.");
            await UniTask.Delay(220, ignoreTimeScale: true, cancellationToken: token);
            if (_consumer.Controller.ConsumePrepareFailure()) throw new InvalidOperationException("Sample requested preparation failure.");
            _consumer.Controller.RecordPreparation();
            _presentationLifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            _presentationTask = PresentAsync(_presentationLifetime.Token).Preserve();
        }
        private async UniTask PresentAsync(CancellationToken token)
        {
            try
            {
                await _consumer.PresentAsync(token);
            }
            catch (Exception exception)
            {
                _consumer.FailPresentation(exception);
            }
        }
        public override async UniTask ReleaseAsync(ISceneRoot root)
        {
            _presentationLifetime?.Cancel();
            await _presentationTask;
            await UniTask.Delay(60, ignoreTimeScale: true);
            _consumer.Controller?.RecordRelease();
        }
        public override void Uninstall(ISceneRoot root)
        {
            _presentationLifetime?.Cancel();
            _presentationLifetime?.Dispose();
            _presentationLifetime = null;
            _consumer.Uninstall();
        }
    }
}

