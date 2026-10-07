using System;
using TPLab.Core.ResourceManagement;
using UnityEngine.SceneManagement;

namespace TPLab.Core.SceneManagement
{
    /// <summary>Immutable presentation identity; grants no scene or root ownership.</summary>
    public readonly struct SceneLoadingContext
    {
        /// <summary>Unique identity of one accepted transition, including repeat loads of the same asset.</summary>
        public Guid OperationId { get; }
        /// <summary>Requested operation. Removal never opts into loading presentation.</summary>
        public SceneTransitionKind Kind { get; }
        /// <summary>Explicit destination backend/path; unused for removal.</summary>
        public SceneTarget Target { get; }
        /// <summary>Actual native load mode, unchanged by presentation policy.</summary>
        public LoadSceneMode Mode { get; }
        internal SceneLoadingContext(SceneTransitionRequest request)
        {
            OperationId = Guid.NewGuid();
            Kind = request.Kind;
            Target = request.Target;
            Mode = request.Mode;
        }
    }

    /// <summary>Immutable stage observation; a native ratio of one does not grant gameplay permission.</summary>
    public readonly struct SceneTransitionProgress
    {
        /// <summary>Identity used to discard stale UI events.</summary>
        public Guid OperationId { get; }
        /// <summary>Current observable transition phase.</summary>
        public SceneTransitionState Stage { get; }
        /// <summary>Native stage ratio, or null when no workload metric is available. Not an overall percentage.</summary>
        public float? StageRatio { get; }
        /// <summary>Destination root and presentation are prepared; final release/reveal may still be pending.</summary>
        public bool IsPrepared { get; }
        internal SceneTransitionProgress(SceneLoadingContext context, SceneTransitionState stage, float? ratio, bool prepared)
        {
            OperationId = context.OperationId;
            Stage = stage;
            StageRatio = ratio;
            IsPrepared = prepared;
        }
    }
}
