using UnityEngine;

namespace TPLab.Core.SceneManagement
{
    /// <summary>Attached root policy, including disabled components. IDs/configuration stay fixed throughout an accepted operation.</summary>
    public abstract class SceneTransitionCondition : MonoBehaviour
    {
        /// <summary>Stable nonempty policy ID, unique within this root; other affected roots may use the same ID.</summary>
        public abstract string ConditionId { get; }
        /// <summary>Synchronously reads project state. False rejects; exceptions remain visible. Never start, stop, cancel or await transitions here.</summary>
        public abstract bool Evaluate(SceneTransitionContext context);
    }
}
