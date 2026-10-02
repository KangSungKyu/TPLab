using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MyLab.Core.Lifecycle
{
    /// <summary>Awaits presentation callbacks and root preparation/shutdown before advancing a scene.</summary>
    public sealed class SceneRootFlow
    {
        private readonly ISceneRoot _root;
        private readonly Func<CancellationToken, UniTask> _coverAsync;
        private readonly Func<CancellationToken, UniTask> _revealAsync;
        private readonly Action<Exception> _onFailure;

        /// <summary>Gets whether this flow is running. Use one flow per transition owner to reject overlaps.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Creates a main-thread flow. Optional cover/reveal callbacks finish when their animations finish.
        /// The failure callback receives failure or cancellation; exceptions remain visible to the caller.
        /// The presentation owner must outlive scene teardown. No UI, scene loading, or input policy is implicit.
        /// </summary>
        public SceneRootFlow(ISceneRoot root, Func<CancellationToken, UniTask> coverAsync = null,
            Func<CancellationToken, UniTask> revealAsync = null, Action<Exception> onFailure = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _coverAsync = coverAsync;
            _revealAsync = revealAsync;
            _onFailure = onFailure;
        }

        /// <summary>
        /// Covers, prepares installers in configured order, awaits scene/content readiness, then reveals.
        /// Preparation failure shuts down the root. Caller cancellation only stops this flow's wait.
        /// Failures and cancellation skip reveal; overlapping calls fail without invoking callbacks.
        /// </summary>
        public UniTask PrepareAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
            CancellationToken cancellationToken = default) => RunAsync(proceedAsync, false, cancellationToken);

        /// <summary>
        /// Covers, waits for reverse shutdown, awaits the next scene/content readiness, then reveals.
        /// Once shutdown starts, cancellation does not interrupt cleanup; it prevents further scene progress.
        /// The root becomes terminal. Destroy its object after shutdown when appropriate.
        /// </summary>
        public UniTask ReleaseAndProceedAsync(Func<CancellationToken, UniTask> proceedAsync,
            CancellationToken cancellationToken = default) => RunAsync(proceedAsync, true, cancellationToken);

        private async UniTask RunAsync(Func<CancellationToken, UniTask> proceedAsync, bool release,
            CancellationToken cancellationToken)
        {
            if (proceedAsync == null)
            {
                throw new ArgumentNullException(nameof(proceedAsync));
            }
            if (IsTransitioning)
            {
                throw new InvalidOperationException("A scene flow is already running.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            IsTransitioning = true;
            bool revealing = false;
            try
            {
                if (_coverAsync != null)
                {
                    await _coverAsync(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (release)
                {
                    await _root.ShutdownAsync();
                }
                else
                {
                    try
                    {
                        await _root.PrepareAsync(cancellationToken);
                    }
                    catch (Exception preparationFailure) when (!(preparationFailure is OperationCanceledException))
                    {
                        try
                        {
                            await _root.ShutdownAsync();
                        }
                        catch (Exception cleanupFailure)
                        {
                            throw new AggregateException(preparationFailure, cleanupFailure);
                        }
                        throw;
                    }
                    if (!_root.IsPrepared)
                    {
                        throw new InvalidOperationException("The root no longer owns prepared systems.");
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                await proceedAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (_revealAsync != null)
                {
                    revealing = true;
                    await _revealAsync(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception failure)
            {
                Exception reported = failure;
                if (revealing && _coverAsync != null)
                {
                    try
                    {
                        // Restore protection even when the transition token is already cancelled.
                        await _coverAsync(CancellationToken.None);
                    }
                    catch (Exception coverFailure)
                    {
                        reported = new AggregateException(failure, coverFailure);
                    }
                }
                try
                {
                    _onFailure?.Invoke(reported);
                }
                catch (Exception callbackFailure)
                {
                    throw new AggregateException(reported, callbackFailure);
                }
                if (!ReferenceEquals(reported, failure))
                {
                    throw reported;
                }
                throw;
            }
            finally
            {
                IsTransitioning = false;
            }
        }
    }
}
