using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;

namespace TPLab.Core.Tests
{
    public sealed class SceneReadinessFaultProbe : MonoBehaviour, ISceneRoot
    {
        public ISceneRoot Wrapped;
        public Exception Failure;
        public Action Reading;
        public GameObject RootObject => gameObject;
        public bool IsReady => Wrapped.IsReady;
        public bool IsPrepared
        {
            get
            {
                Reading?.Invoke();
                if (Failure != null) throw Failure;
                return Wrapped.IsPrepared;
            }
        }
        public UniTask PrepareAsync(CancellationToken cancellationToken = default) => Wrapped.PrepareAsync(cancellationToken);
        public UniTask ShutdownAsync() => Wrapped.ShutdownAsync();
    }
}
