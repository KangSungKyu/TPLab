using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Provides the selected root and synchronous installation readiness to installers.</summary>
    public interface ISceneRoot
    {
        /// <summary>Gets the root GameObject that owns this installation.</summary>
        GameObject RootObject { get; }
        /// <summary>Gets whether every installer completed. Does not imply asynchronous assets are ready.</summary>
        bool IsReady { get; }
    }
}
