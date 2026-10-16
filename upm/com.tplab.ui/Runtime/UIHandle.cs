using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TPLab.UI
{
    /// <summary>
    /// One context-owned display generation, never a reusable native object's identity.
    /// Commands require Unity's main thread. Completed state, tasks, and cached tokens remain observable after owner disposal.
    /// </summary>
    public sealed class UIHandle
    {
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly CancellationToken _lifetimeToken;
        private readonly List<CleanupRegistration> _cleanup = new List<CleanupRegistration>();
        private CancellationTokenRegistration _callerRegistration;
        private CancellationToken _callerToken;
        internal readonly UniTaskCompletionSource OpenCompletion = new UniTaskCompletionSource();
        internal readonly UniTaskCompletionSource CloseCompletion = new UniTaskCompletionSource();
        internal readonly UniTaskCompletionSource OpeningDone = new UniTaskCompletionSource();
        internal readonly List<Exception> Errors = new List<Exception>();
        internal UIState CurrentState = UIState.Opening;
        internal UIDefinition Definition;
        internal UIHooks Hooks;
        internal GameObject View;
        internal bool CloseStarted;
        internal bool IsPresented;
        internal bool IsHudSelection;
        internal UIPresentation Presentation;
        internal UIInputMode CurrentInputMode;
        internal bool InputEligible;
        internal bool InputPublished;
        internal long Order;
        internal IDisposable ModalLease;
        internal CancellationTokenSource UserApproval;
        internal bool UserRequestPending;
        internal GameObject FocusTarget;
        internal long FocusSequence;
        internal bool FocusPending;

        internal UIHandle(UIContext context, long id, UIOpenRequest request, UIDefinition definition)
        {
            Context = context;
            Id = id;
            DefinitionId = request.DefinitionId;
            Parent = request.Parent;
            CurrentInputMode = request.InputMode ?? definition.InputMode;
            Definition = definition;
            _lifetimeToken = _lifetime.Token;
            UIHooks requested = request.Hooks;
            Hooks = new UIHooks
            {
                PrepareAsync = requested?.PrepareAsync,
                OpenAsync = requested?.OpenAsync,
                CloseAsync = requested?.CloseAsync,
                CanCloseAsync = requested?.CanCloseAsync,
                Closed = requested?.Closed
            };
        }

        /// <summary>Gets the owner, without authorizing disposal of its borrowed services.</summary>
        public UIContext Context
        {
            get;
        }
        /// <summary>Gets the monotonically allocated generation within this context.</summary>
        public long Id
        {
            get;
        }
        /// <summary>Gets the registered definition ID.</summary>
        public string DefinitionId
        {
            get;
        }
        /// <summary>Gets the immutable logical parent, independent of physical Canvas/Transform placement.</summary>
        public UIHandle Parent
        {
            get;
        }
        /// <summary>Gets this generation's state, including after termination.</summary>
        public UIState State => CurrentState;
        /// <summary>Gets the context-owned prefab clone while this generation exists; never destroy it directly.</summary>
        /// <remarks>Every display places this original clone under an owned input/visibility wrapper; that wrapper is the managed host child.</remarks>
        public GameObject ViewObject => View;
        /// <summary>Gets the cached token cancelled at the start of this display's termination.</summary>
        public CancellationToken LifetimeToken => _lifetimeToken;

        /// <summary>Gets this display generation's current input mode, separately from its native visibility.</summary>
        /// <remarks>The cached generation value remains observable after retirement. The value belongs to this generation.</remarks>
        public UIInputMode InputMode => CurrentInputMode;

        /// <summary>Gets whether this Visible generation is eligible within the context's modal boundary.</summary>
        /// <remarks>
        /// This is logical UI eligibility; it does not promise a borrowed input map or adapter recovery gate is enabled.
        /// Opening, Closing, and retired generations are ineligible. The value belongs to this generation.
        /// </remarks>
        public bool CanReceiveInput => CurrentState == UIState.Visible && InputEligible && !Context.IsDisposed && Context.Fault == null;

        /// <summary>Changes a Visible generation's input mode and independent modal lease on Unity's main thread.</summary>
        /// <exception cref="InvalidOperationException">Not Visible, reentrant, or native state cannot be applied safely.</exception>
        /// <exception cref="ArgumentException">The mode is invalid.</exception>
        public void SetInputMode(UIInputMode inputMode)
        {
            Context.SetInputMode(this, inputMode);
        }

        /// <summary>Moves this Visible generation and its logical subtree forward, preserving role and parent-before-child order.</summary>
        /// <remarks>The immutable generation Id does not change. Borrowed hosts and sorting settings are not reordered.</remarks>
        /// <exception cref="InvalidOperationException">Not Visible, reentrant, or fixed native layout cannot express the requested order.</exception>
        public void BringToFront()
        {
            Context.BringToFront(this);
        }

        /// <summary>Requests focus for a live owned-view target on Unity's main thread.</summary>
        /// <param name="target">Borrowed live target within this generation's owned prefab clone.</param>
        /// <remarks>A prepared Opening generation remembers the target without native selection; eligible Visible applies it. No global EventSystem is resolved.</remarks>
        /// <exception cref="InvalidOperationException">No explicit EventSystem connection, ineligible/state invalid, or reentry.</exception>
        /// <exception cref="ArgumentException">The target is destroyed, foreign, or outside this generation's view.</exception>
        public void SetFocus(GameObject target)
        {
            Context.SetFocus(this, target);
        }

        /// <summary>Requests opted-in project approval before terminating this Visible generation.</summary>
        /// <param name="reason">Explicit user action; no outside pointer detector is installed by this API.</param>
        /// <param name="cancellationToken">Cancels this user request with OperationCanceledException; it never replaces owner cleanup.</param>
        /// <returns>True after this accepted close reaches Closed; false when no handler is registered or project approval vetoes it.</returns>
        /// <remarks>
        /// Concurrent user requests are rejected. Forced CloseAsync and owner shutdown ignore approval and cancel outstanding veto work.
        /// Approval callbacks must not await this same display operation; synchronous lifecycle/cleanup/native reentry is rejected.
        /// Cleanup/listener/native errors remain failed results rather than successful true. The value belongs to this generation.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Stale/state invalid, concurrent request, thread violation, or forbidden reentry.</exception>
        /// <exception cref="ArgumentException">The reason is invalid.</exception>
        /// <exception cref="OperationCanceledException">The caller or forced display/owner termination cancelled outstanding approval.</exception>
        public UniTask<bool> RequestCloseAsync(UIUserCloseReason reason, CancellationToken cancellationToken = default)
        {
            return Context.RequestCloseAsync(this, reason, cancellationToken);
        }
        /// <summary>Shares the opening outcome, published after partial cleanup if opening fails or is cancelled.</summary>
        /// <remarks>
        /// Synchronous callback self-await is rejected. Self-await after a callback has yielded is forbidden
        /// usage, but cannot be detected by the synchronous dispatch guard.
        /// </remarks>
        public UniTask Opened
        {
            get
            {
                Context.EnsureObservation(this);
                return OpenCompletion.Task;
            }
        }

        /// <summary>
        /// Shares cleanup completion after native retirement, State Closed, cleared ViewObject, and the Closed observer.
        /// Successful Reuse retains a hidden clone only after its observer succeeds; renderer-only clones stay active.
        /// Observer/cleanup errors fault this result after discard. Opening errors alone do not fault Closed.
        /// </summary>
        public UniTask Closed
        {
            get
            {
                Context.EnsureObservation(this, true);
                return CloseCompletion.Task;
            }
        }

        /// <summary>Registers synchronous owned cleanup, executed once in reverse order at termination.</summary>
        /// <param name="cleanup">Subscription/resource removal; never dispose a borrowed service.</param>
        /// <returns>A registration whose Dispose executes its action immediately and exactly once, including from a display hook.</returns>
        /// <exception cref="ObjectDisposedException">Termination began; clean the unregistered resource at the call site.</exception>
        /// <exception cref="InvalidOperationException">Thread violation or cleanup reentry.</exception>
        public IDisposable RegisterCleanup(Action cleanup)
        {
            Context.EnsureCleanupRegistration();
            if (cleanup == null)
            {
                throw new ArgumentNullException(nameof(cleanup));
            }
            if (CurrentState == UIState.Closing || CurrentState == UIState.Closed || Context.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(UIHandle));
            }
            CleanupRegistration registration = new CleanupRegistration(this, cleanup);
            _cleanup.Add(registration);
            return registration;
        }

        /// <summary>Starts non-vetoable termination and shares this generation's Closed result.</summary>
        /// <remarks>
        /// A successful Reuse generation retains its hidden clone and native wrapper; failures and partial opening discard them.
        /// This handle releases its view and display token regardless of retention, and never mutates a later generation.
        /// </remarks>
        /// <param name="cancellationToken">Cancels only this caller's wait; requested cleanup always continues.</param>
        /// <exception cref="InvalidOperationException">Thread violation, a callback inside this subtree, or cleanup/native reentry; independent hooks may close it.</exception>
        /// <exception cref="AggregateException">Cleanup failed after remaining cleanup was attempted.</exception>
        public UniTask CloseAsync(CancellationToken cancellationToken = default)
        {
            Context.Close(this);
            return CloseCompletion.Task.AttachExternalCancellation(cancellationToken);
        }

        internal void ConnectCallerCancellation(CancellationToken callerToken)
        {
            _callerToken = callerToken;
            if (!callerToken.CanBeCanceled)
            {
                return;
            }
            _callerRegistration = callerToken.Register(() => Context.CallerCancelled(this));
            if (CurrentState != UIState.Opening)
            {
                _callerRegistration.Dispose();
            }
        }

        internal void DetachCaller()
        {
            _callerRegistration.Dispose();
            _callerRegistration = default;
            _callerToken = default;
        }

        internal void ThrowIfOpeningCancelled()
        {
            UIContext.EnsureMainThread();
            _callerToken.ThrowIfCancellationRequested();
            _lifetimeToken.ThrowIfCancellationRequested();
            Context.LifetimeToken.ThrowIfCancellationRequested();
            if (Context.IsDisposed || Context.RootObject == null)
            {
                throw new OperationCanceledException(Context.LifetimeToken);
            }
            if (Context.Fault != null)
            {
                throw new InvalidOperationException("The context has a native application fault.", Context.Fault);
            }
            if (CurrentState != UIState.Opening)
            {
                throw new OperationCanceledException(_lifetimeToken);
            }
        }

        internal void CancelLifetime()
        {
            try
            {
                _lifetime.Cancel();
            }
            catch (Exception error)
            {
                Errors.Add(error);
            }
        }

        internal void RunCleanup()
        {
            for (int index = _cleanup.Count - 1; index >= 0; --index)
            {
                try
                {
                    _cleanup[index].Invoke();
                }
                catch (Exception)
                {
                    // Invoke records the error; continue every remaining registration.
                }
            }
            _cleanup.Clear();
        }

        internal void Finish()
        {
            DetachCaller();
            _lifetime.Dispose();
            View = null;
            Presentation = null;
            IsPresented = false;
            InputEligible = false;
            InputPublished = false;
            FocusTarget = null;
            FocusPending = false;
            Hooks = null;
            Definition = null;
            CurrentState = UIState.Closed;
        }

        private sealed class CleanupRegistration : IDisposable
        {
            private UIHandle _handle;
            private Action _action;

            internal CleanupRegistration(UIHandle handle, Action action)
            {
                _handle = handle;
                _action = action;
            }

            public void Dispose()
            {
                UIContext.EnsureMainThread();
                if (_action == null)
                {
                    return;
                }
                _handle.Context.EnsureMutationAllowed();
                Invoke();
            }

            internal void Invoke()
            {
                Action action = _action;
                if (action == null)
                {
                    return;
                }
                UIHandle handle = _handle;
                _action = null;
                _handle = null;
                try
                {
                    handle.Context.DispatchCleanup(action);
                }
                catch (Exception error)
                {
                    handle.Errors.Add(error);
                    throw;
                }
            }
        }
    }
}
