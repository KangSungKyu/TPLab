using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.UI;

namespace MyLab.Samples.SceneTransitions
{
    /// <summary>Installed game consumer borrowing the session controller; owns its presentation completion.</summary>
    public sealed class SampleSceneConsumer : MonoBehaviour
    {
        [SerializeField] private Text _label;
        private ISceneRoot _root;
        private UniTaskCompletionSource _presentation;
        public SceneTransitionSampleController Controller
        {
            get;
            private set;
        }
        public void ConfigureLabel(Text label) => _label = label;
        public void Install(ISceneRoot root)
        {
            _root = root;
            _presentation = new UniTaskCompletionSource();
        }
        public void Bind(SceneTransitionSampleController controller)
        {
            if (_root == null || !_root.IsReady) throw new InvalidOperationException("Inject after installation.");
            Controller = controller;
        }
        public async UniTask PresentAsync(CancellationToken token)
        {
            await UniTask.Delay(180, ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (_label != null) _label.text = gameObject.scene.name + "\nContent prepared; presentation ready";
            Controller.RecordPresentation();
            _presentation.TrySetResult();
        }
        public void FailPresentation(Exception exception)
        {
            if (exception is OperationCanceledException) _presentation.TrySetCanceled();
            else _presentation.TrySetException(exception);
        }
        public UniTask WaitForPresentationAsync(CancellationToken token) => _presentation.Task.AttachExternalCancellation(token);
        public void Uninstall()
        {
            _presentation?.TrySetCanceled();
            _root = null;
            Controller = null;
        }
    }
}

