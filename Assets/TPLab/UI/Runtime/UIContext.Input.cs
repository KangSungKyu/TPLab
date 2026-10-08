using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI
{
    public sealed partial class UIContext
    {
        private Func<IDisposable> _acquireModalBlock;
        private long _nextOrder;
        private long _nextFocusSequence;
        private int _notificationDepth;
        private bool _nativeInputAvailable = true;
        private object _inputBridgeOwner;
        private Action _inputApply;
        private Func<IDisposable> _inputFault;
        private IDisposable _inputFaultBlock;
        private Action _inputShutdown;

        private void NotifyDisplayChanged(UIHandle handle)
        {
            Action<UIHandle> listeners = DisplayChanged;
            if (listeners == null)
            {
                return;
            }
            ++_notificationDepth;
            try
            {
                foreach (Action<UIHandle> listener in listeners.GetInvocationList())
                {
                    try
                    {
                        listener(handle);
                    }
                    catch (Exception error)
                    {
                        handle.Errors.Add(error);
                    }
                }
            }
            finally
            {
                --_notificationDepth;
            }
        }

        private void EnsureVisibleCommand(UIHandle handle)
        {
            EnsureCommand();
            if (handle.Context != this || handle.State != UIState.Visible || _dispatching == handle)
            {
                throw new InvalidOperationException("This command requires a live Visible generation outside its own callback.");
            }
        }

        internal void SetInputMode(UIHandle handle, UIInputMode inputMode)
        {
            EnsureVisibleCommand(handle);
            if (!Enum.IsDefined(typeof(UIInputMode), inputMode))
            {
                throw new ArgumentException("Invalid input mode.", nameof(inputMode));
            }
            if (handle.CurrentInputMode == inputMode)
            {
                return;
            }
            ApplyInputBridge(handle);
            if (inputMode == UIInputMode.Modal)
            {
                AcquireModalLease(handle);
            }
            handle.CurrentInputMode = inputMode;
            if (inputMode == UIInputMode.Modeless)
            {
                RetireModalLease(handle);
            }
            RefreshInput(handle);
            int errorsBeforeNotification = handle.Errors.Count;
            NotifyDisplayChanged(handle);
            ThrowCommandNotificationErrors(handle, errorsBeforeNotification);
        }

        internal void BringToFront(UIHandle handle)
        {
            EnsureVisibleCommand(handle);
            var subtree = new List<UIHandle>();
            foreach (UIHandle current in _displays)
            {
                if (current.State != UIState.Closed && IsWithin(current, handle))
                {
                    subtree.Add(current);
                }
            }
            subtree.Sort(CompareDisplayOrder);
            var proposed = new Dictionary<UIHandle, long>();
            long order = _nextOrder;
            foreach (UIHandle current in subtree)
            {
                proposed.Add(current, ++order);
            }
            // Check every fixed plane before changing native roots or the mutable logical order.
            foreach (UIHandle current in subtree)
            {
                if (!current.IsPresented)
                {
                    continue;
                }
                UIPresentation.Plane plane = UIPresentation.Validate(current.View,
                    GetHost(current.Definition.HostId), current.Definition.HideStrategy);
                if (!plane.HasGraphics)
                {
                    continue;
                }
                foreach (UIHandle other in _displays)
                {
                    if (other == current || !other.IsPresented || other.State == UIState.Closed)
                    {
                        continue;
                    }
                    UIPresentation.Plane otherPlane = UIPresentation.Validate(other.View,
                        GetHost(other.Definition.HostId), other.Definition.HideStrategy);
                    if (!otherPlane.HasGraphics)
                    {
                        continue;
                    }
                    int fixedOrder = UIPresentation.CompareFixedOrder(plane, otherPlane);
                    long otherOrder = proposed.TryGetValue(other, out long next) ? next : other.Order;
                    int roles = current.Definition.Role.CompareTo(other.Definition.Role);
                    int logical = roles != 0 ? roles : proposed[current].CompareTo(otherOrder);
                    if (fixedOrder != 0 && Math.Sign(fixedOrder) != Math.Sign(logical))
                    {
                        throw new InvalidOperationException("Fixed hosts cannot express the requested front order.");
                    }
                }
            }
            ApplyInputBridge(handle);
            foreach (UIHandle current in subtree)
            {
                current.Order = proposed[current];
            }
            _nextOrder = order;
            ++_nativeDepth;
            try
            {
                foreach (Transform host in _hosts.Values)
                {
                    ApplySiblingOrder(host);
                }
            }
            catch (Exception error)
            {
                RecordNativeFailure(handle, error);
                throw;
            }
            finally
            {
                --_nativeDepth;
            }
            RefreshInput(handle);
            int errorsBeforeNotification = handle.Errors.Count;
            NotifyDisplayChanged(handle);
            ThrowCommandNotificationErrors(handle, errorsBeforeNotification);
        }

        internal void SetFocus(UIHandle handle, GameObject target)
        {
            EnsureCommand();
            if (handle.Context != this || _dispatching == handle && handle.State != UIState.Opening
                || handle.State != UIState.Opening && handle.State != UIState.Visible
                || handle.View == null || handle.State == UIState.Visible && !handle.CanReceiveInput)
            {
                throw new InvalidOperationException("Focus requires a prepared Opening or eligible Visible generation.");
            }
            if (EventSystem == null)
            {
                throw new InvalidOperationException("Focus requires an explicitly borrowed EventSystem.");
            }
            if (!IsOwnedTarget(handle, target))
            {
                throw new ArgumentException("The live focus target must belong to this generation's view.", nameof(target));
            }
            if (handle.State == UIState.Visible && !IsFocusTargetValid(handle, target))
            {
                throw new ArgumentException("The focus target must be active and interactable.", nameof(target));
            }
            handle.FocusTarget = target;
            handle.FocusSequence = ++_nextFocusSequence;
            handle.FocusPending = handle.State == UIState.Opening || !_nativeInputAvailable;
            if (handle.State == UIState.Visible && _nativeInputAvailable)
            {
                SelectNative(target);
            }
            int errorsBeforeNotification = handle.Errors.Count;
            NotifyDisplayChanged(handle);
            ThrowCommandNotificationErrors(handle, errorsBeforeNotification);
        }

        private static bool IsOwnedTarget(UIHandle handle, GameObject target)
        {
            return target != null && handle.View != null
                && (target == handle.View || target.transform.IsChildOf(handle.View.transform));
        }

        private static bool IsFocusTargetValid(UIHandle handle, GameObject target)
        {
            if (!handle.CanReceiveInput || !IsOwnedTarget(handle, target) || !target.activeInHierarchy)
            {
                return false;
            }
            Selectable selectable = target.GetComponent<Selectable>();
            return selectable == null || selectable.isActiveAndEnabled && selectable.IsInteractable();
        }

        private void RememberCurrentFocus()
        {
            if (EventSystem == null || EventSystem.currentSelectedGameObject == null)
            {
                return;
            }
            GameObject selected = EventSystem.currentSelectedGameObject;
            foreach (UIHandle current in _displays)
            {
                if (current.State == UIState.Visible && IsOwnedTarget(current, selected))
                {
                    if (current.FocusTarget != selected)
                    {
                        current.FocusTarget = selected;
                        current.FocusSequence = ++_nextFocusSequence;
                    }
                    return;
                }
            }
        }

        private void SelectNative(GameObject target)
        {
            if (EventSystem == null || EventSystem.currentSelectedGameObject == target)
            {
                return;
            }
            // Selection synchronously invokes project Select/Deselect handlers.
            ++_nativeDepth;
            try
            {
                EventSystem.SetSelectedGameObject(target);
            }
            finally
            {
                --_nativeDepth;
            }
        }

        private void ReconcileFocus()
        {
            if (EventSystem == null)
            {
                return;
            }
            if (!_nativeInputAvailable || IsDisposed || Fault != null)
            {
                SelectNative(null);
                return;
            }
            UIHandle pending = null;
            foreach (UIHandle current in _displays)
            {
                if (current.FocusPending && IsFocusTargetValid(current, current.FocusTarget)
                    && (pending == null || current.FocusSequence > pending.FocusSequence))
                {
                    pending = current;
                }
            }
            if (pending != null)
            {
                pending.FocusPending = false;
                SelectNative(pending.FocusTarget);
                return;
            }
            GameObject selected = EventSystem.currentSelectedGameObject;
            foreach (UIHandle current in _displays)
            {
                if (IsFocusTargetValid(current, selected))
                {
                    return;
                }
            }
            UIHandle remembered = null;
            UIHandle front = null;
            foreach (UIHandle current in _displays)
            {
                if (!current.CanReceiveInput)
                {
                    continue;
                }
                if (front == null || CompareDisplayOrder(current, front) > 0)
                {
                    front = current;
                }
                if (IsFocusTargetValid(current, current.FocusTarget)
                    && (remembered == null || current.FocusSequence > remembered.FocusSequence))
                {
                    remembered = current;
                }
            }
            if (remembered != null)
            {
                SelectNative(remembered.FocusTarget);
                return;
            }
            if (front != null && front.View != null)
            {
                foreach (Selectable selectable in front.View.GetComponentsInChildren<Selectable>())
                {
                    if (IsFocusTargetValid(front, selectable.gameObject))
                    {
                        front.FocusTarget = selectable.gameObject;
                        front.FocusSequence = ++_nextFocusSequence;
                        SelectNative(selectable.gameObject);
                        return;
                    }
                }
            }
            SelectNative(null);
        }

        private void RefreshInput(UIHandle affected)
        {
            RememberCurrentFocus();
            UIHandle barrier = null;
            foreach (UIHandle current in _displays)
            {
                if (current.IsPresented && current.InputPublished && current.CurrentInputMode == UIInputMode.Modal
                    && (current.State == UIState.Visible || current.State == UIState.Closing)
                    && (barrier == null || CompareDisplayOrder(current, barrier) > 0))
                {
                    barrier = current;
                }
            }
            ++_nativeDepth;
            try
            {
                foreach (UIHandle current in _displays)
                {
                    current.InputEligible = !IsDisposed && Fault == null && current.State == UIState.Visible
                        && (barrier == null || CompareDisplayOrder(current, barrier) >= 0);
                    if (current.Presentation != null && current.Presentation.Root != null)
                    {
                        current.Presentation.SetInputEnabled(current.InputEligible);
                    }
                }
                ReconcileFocus();
            }
            catch (Exception error)
            {
                RecordNativeFailure(affected, error);
                throw;
            }
            finally
            {
                --_nativeDepth;
            }
        }

        private void ApplyInputBridge(UIHandle handle)
        {
            ++_nativeDepth;
            try
            {
                _inputApply?.Invoke();
            }
            catch (Exception error)
            {
                RecordNativeFailure(handle, error);
                throw;
            }
            finally
            {
                --_nativeDepth;
            }
        }

        private void AcquireModalLease(UIHandle handle)
        {
            if (handle.ModalLease != null || _acquireModalBlock == null)
            {
                return;
            }
            ++_nativeDepth;
            try
            {
                handle.ModalLease = _acquireModalBlock()
                    ?? throw new InvalidOperationException("The modal factory returned no owned lease.");
            }
            catch (Exception error)
            {
                RecordNativeFailure(handle, error);
                throw;
            }
            finally
            {
                --_nativeDepth;
            }
        }

        private void RetireModalLease(UIHandle handle)
        {
            IDisposable lease = handle.ModalLease;
            handle.ModalLease = null;
            if (lease == null)
            {
                return;
            }
            ++_nativeDepth;
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                RecordNativeFailure(handle, error);
            }
            finally
            {
                --_nativeDepth;
            }
        }

        private static void ThrowCommandNotificationErrors(UIHandle handle, int start)
        {
            int count = handle.Errors.Count - start;
            if (count == 0)
            {
                return;
            }
            var errors = handle.Errors.GetRange(start, count);
            handle.Errors.RemoveRange(start, count);
            throw new AggregateException(errors);
        }

        private static void ThrowRecordedErrors(UIHandle handle)
        {
            if (handle.Errors.Count != 0)
            {
                throw new AggregateException(handle.Errors);
            }
        }

        internal UniTask<bool> RequestCloseAsync(UIHandle handle, UIUserCloseReason reason, CancellationToken token)
        {
            EnsureVisibleCommand(handle);
            if (!Enum.IsDefined(typeof(UIUserCloseReason), reason))
            {
                throw new ArgumentException("Invalid user close reason.", nameof(reason));
            }
            token.ThrowIfCancellationRequested();
            if (handle.UserRequestPending)
            {
                throw new InvalidOperationException("This generation already has a user-close request.");
            }
            if (handle.Hooks.CanCloseAsync == null)
            {
                return UniTask.FromResult(false);
            }
            handle.UserRequestPending = true;
            return RunUserCloseAsync(handle, reason, token);
        }

        private async UniTask<bool> RunUserCloseAsync(UIHandle handle, UIUserCloseReason reason, CancellationToken caller)
        {
            CancellationTokenSource approval = CancellationTokenSource.CreateLinkedTokenSource(caller, handle.LifetimeToken);
            handle.UserApproval = approval;
            try
            {
                UIHandle previous = _dispatching;
                _dispatching = handle;
                UniTask<bool> decision;
                try
                {
                    decision = handle.Hooks.CanCloseAsync(handle, reason, approval.Token);
                }
                finally
                {
                    _dispatching = previous;
                }
                bool allowed = await decision.AttachExternalCancellation(approval.Token);
                await UniTask.SwitchToMainThread();
                approval.Token.ThrowIfCancellationRequested();
                if (!allowed)
                {
                    return false;
                }
                // Accepted close cancels its display token; the approval token no longer owns this wait.
                handle.UserApproval = null;
                approval.Dispose();
                approval = null;
                Close(handle);
                await handle.CloseCompletion.Task.AttachExternalCancellation(caller);
                return true;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(handle.UserApproval, approval))
                {
                    handle.UserApproval = null;
                }
                approval?.Dispose();
                handle.UserRequestPending = false;
            }
        }

        internal void AttachInputBridge(object owner, Action apply, Func<IDisposable> fault, Action shutdown)
        {
            EnsureCommand();
            if (_inputBridgeOwner != null)
            {
                throw new InvalidOperationException("The context already has an input binding.");
            }
            _inputBridgeOwner = owner;
            _inputApply = apply;
            _inputFault = fault;
            _inputShutdown = shutdown;
        }

        internal void DetachInputBridge(object owner)
        {
            EnsureMainThread();
            if (!ReferenceEquals(_inputBridgeOwner, owner))
            {
                return;
            }
            _inputBridgeOwner = null;
            _inputApply = null;
            _inputFault = null;
            _inputShutdown = null;
            _nativeInputAvailable = true;
        }

        internal void SetNativeInputAvailable(object owner, bool available)
        {
            EnsureMainThread();
            if (!ReferenceEquals(_inputBridgeOwner, owner))
            {
                return;
            }
            RememberCurrentFocus();
            _nativeInputAvailable = available;
            ReconcileFocus();
        }

        internal UIHandle GetCancelTarget(object owner)
        {
            EnsureMainThread();
            if (!ReferenceEquals(_inputBridgeOwner, owner) || IsDisposed || Fault != null)
            {
                return null;
            }
            UIHandle front = null;
            foreach (UIHandle current in _displays)
            {
                if (current.CanReceiveInput && current.Hooks.CanCloseAsync != null
                    && (front == null || CompareDisplayOrder(current, front) > 0))
                {
                    front = current;
                }
            }
            return front;
        }

        internal void ReportInputFailure(object owner, Exception error)
        {
            EnsureMainThread();
            if (ReferenceEquals(_inputBridgeOwner, owner))
            {
                ProtectNativeFault(error);
            }
        }

        private void ProtectNativeFault(Exception error)
        {
            Fault = Fault ?? error;
            ++_nativeDepth;
            try
            {
                foreach (UIHandle current in _displays)
                {
                    current.InputEligible = false;
                    if (current.Presentation != null && current.Presentation.Root != null)
                    {
                        current.Presentation.SetInputEnabled(false);
                    }
                }
                SelectNative(null);
                if (_inputFaultBlock == null && _inputFault != null)
                {
                    _inputFaultBlock = _inputFault();
                }
            }
            catch (Exception protectionError)
            {
                Fault = new AggregateException(Fault, protectionError);
            }
            finally
            {
                --_nativeDepth;
            }
        }
    }
}