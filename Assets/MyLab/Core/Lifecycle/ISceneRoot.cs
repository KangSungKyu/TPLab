using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Provides root ownership, synchronous installation, and explicit asynchronous preparation.</summary>
    public interface ISceneRoot
    {
        /// <summary>Gets the root GameObject that owns this installation.</summary>
        GameObject RootObject { get; }
        /// <summary>Gets whether every installer completed. Does not imply asynchronous assets are ready.</summary>
        bool IsReady { get; }
        /// <summary>Gets whether every installer completed PrepareAsync and shutdown has not started.</summary>
        bool IsPrepared { get; }
        /// <summary>
        /// Shares one ordered preparation attempt after installation. Caller cancellation only cancels that wait.
        /// Fails for an inactive/uninstalled or terminal root. Failures remain visible; retry requires a new root.
        /// </summary>
        UniTask PrepareAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Stops preparation, awaits its cooperative completion, releases installers in reverse order,
        /// then uninstalls. Shares shutdown completion and aggregates cleanup failures. This ends the root lifetime.
        /// Call before destroying the root to permit asynchronous release.
        /// </summary>
        UniTask ShutdownAsync();
    }
}
