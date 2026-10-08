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
        /// Opening remains input-ineligible until this callback succeeds. Returning publishes native modal/focus state before Opened completes.
        /// </summary>
        public Func<UIHandle, CancellationToken, UniTask> OpenAsync { get; set; }

        /// <summary>
        /// Awaits graceful closing work. Its internal token can be cancelled by owner termination and is
        /// distinct from the already-cancelled display token. Cleanup continues if this callback fails.
        /// </summary>
        public Func<UIHandle, CancellationToken, UniTask> CloseAsync { get; set; }

        /// <summary>Optionally approves an explicit user close request for this display generation.</summary>
        /// <remarks>
        /// Null means user close is not opted in; false vetoes it. Forced close and owner shutdown never consult this callback.
        /// The token is scoped to approval and caller/forced termination, separately from graceful close animation.
        /// Project communication remains project-owned. Stop accessing the view after cancellation and register subscription cleanup.
        /// The callback runs on Unity's main thread and must not request or await this same lifecycle operation.
        /// </remarks>
        public Func<UIHandle, UIUserCloseReason, CancellationToken, UniTask<bool>> CanCloseAsync { get; set; }
        /// <summary>
        /// Observes termination once, including partial opening and owner fallback. Errors remain observable
        /// through Closed; callbacks must not request or await this same display's termination again.
        /// </summary>
        public Action<UIHandle> Closed { get; set; }
    }
}