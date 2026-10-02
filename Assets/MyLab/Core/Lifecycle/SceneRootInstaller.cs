using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Adapts project-specific manager creation and reference injection to a selected root.</summary>
    public abstract class SceneRootInstaller : MonoBehaviour
    {
        /// <summary>Creates owned services and injects explicit references. Root readiness is false here.</summary>
        public abstract void Install(ISceneRoot root);
        /// <summary>Clears injected references and releases owned resources, including partial installation.</summary>
        public abstract void Uninstall(ISceneRoot root);
    }
}
