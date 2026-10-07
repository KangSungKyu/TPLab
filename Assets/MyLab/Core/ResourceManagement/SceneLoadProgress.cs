using System;

namespace MyLab.Core.ResourceManagement
{
    /// <summary>Identifies backend target resolution or native scene loading, independently of root preparation.</summary>
    public enum SceneLoadStage
    {
        ResolvingTarget,
        LoadingScene
    }

    /// <summary>Immutable backend progress. Ratio is finite and between zero and one; it does not authorize gameplay.</summary>
    public readonly struct SceneLoadProgress
    {
        /// <summary>The backend stage being observed.</summary>
        public SceneLoadStage Stage { get; }

        /// <summary>Stage progress from zero to one, distinct from download byte progress.</summary>
        public float Ratio { get; }

        /// <summary>Creates one valid stage snapshot; invalid stages or ratios throw ArgumentOutOfRangeException.</summary>
        public SceneLoadProgress(SceneLoadStage stage, float ratio)
        {
            if (stage != SceneLoadStage.ResolvingTarget && stage != SceneLoadStage.LoadingScene)
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }
            if (float.IsNaN(ratio) || float.IsInfinity(ratio) || ratio < 0f || ratio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(ratio));
            }
            Stage = stage;
            Ratio = ratio;
        }
    }

    /// <summary>Synchronous main-thread observation that captures UI failure without abandoning backend ownership.</summary>
    public sealed class SceneLoadProgressObserver : IDisposable
    {
        private Action<SceneLoadProgress> _onProgress;

        /// <summary>The first callback exception, or null; the load owner inspects it only after retaining its result.</summary>
        public Exception Failure { get; private set; }

        /// <summary>Creates an observer with a nonnull callback; null throws ArgumentNullException.</summary>
        public SceneLoadProgressObserver(Action<SceneLoadProgress> onProgress)
        {
            _onProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));
        }

        /// <summary>Reports synchronously on Unity's main thread; captures the first callback failure and suppresses later reports.</summary>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public void Report(SceneLoadProgress progress)
        {
            LoadedScene.EnsureMainThread();
            if (_onProgress == null || Failure != null)
            {
                return;
            }
            try
            {
                _onProgress(progress);
            }
            catch (Exception error)
            {
                if (Failure == null)
                {
                    Failure = error;
                }
            }
        }

        /// <summary>Idempotently suppresses future notifications without cancelling native work or clearing the first failure.</summary>
        public void Dispose()
        {
            _onProgress = null;
        }
    }
}
