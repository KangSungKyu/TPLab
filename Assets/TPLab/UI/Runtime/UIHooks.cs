using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TPLab.UI
{
    /// <summary>
    /// Project-owned callbacks for one display. UIContext snapshots callbacks when accepting a request.
    /// Callbacks execute on Unity's main thread and must observe their supplied cancellation token.
    /// They must not await or reenter their own display operation. Synchronous self-reentry is detected;
    /// self-await after yielding remains forbidden usage, outside the synchronous dispatch guard.
    /// Cancelled callbacks must stop accessing the clone; owner fallback does not await project work indefinitely.
    /// </summary>
    public sealed class UIHooks
    {
        /// <summary>
        /// Prepares this display's data and subscriptions, separately from source-only UIContext.PrepareAsync.
        /// Fresh and Deactivate clones are inactive. Renderer-only Reuse clones remain active with owned rendering,
        /// raycasters, and the visibility mask blocked. The supplied token is this display's lifetime.
        /// Register precise subscription removals on the handle; borrowed services remain project-owned.
        /// </summary>
        public Func<UIHandle, CancellationToken, UniTask> PrepareAsync { get; set; }

        /// <summary>
        /// Awaits project opening work after native activation. The display lifetime cancels this work on close.
        /// Returning completes preparation for the handle's Opened observation.
        /// </summary>
        public Func<UIHandle, CancellationToken, UniTask> OpenAsync { get; set; }

        /// <summary>
        /// Awaits graceful closing work. Its internal token can be cancelled by owner termination and is
        /// distinct from the already-cancelled display token. Cleanup continues if this callback fails.
        /// </summary>
        public Func<UIHandle, CancellationToken, UniTask> CloseAsync { get; set; }

        /// <summary>
        /// Observes termination once, including partial opening and owner fallback. Errors remain observable
        /// through Closed; callbacks must not request or await this same display's termination again.
        /// </summary>
        public Action<UIHandle> Closed { get; set; }
    }
}