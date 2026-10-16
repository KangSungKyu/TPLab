using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TPLab.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using NativeInputSystem = UnityEngine.InputSystem.InputSystem;

namespace TPLab.UI.InputSystem
{
    /// <summary>Optional native Input System boundary borrowing an explicit UI context, input scope, and EventSystem module.</summary>
    /// <remarks>
    /// The configured runtime-clone UI map is reserved for this adapter/native module. Arbitrary raw action subscribers
    /// bypass native UI isolation and are outside the supported replay boundary. Only subscriptions and modal recovery
    /// leases are owned here. Emergency blocking transfers to the context until owner shutdown. The input scope,
    /// EventSystem, module, and action references remain borrowed.
    /// </remarks>
    [DefaultExecutionOrder(-10000)]
    public sealed class UIInputSystemAdapter : MonoBehaviour
    {
        private static readonly Dictionary<InputSystemUIInputModule, UIInputSystemAdapter> ModuleOwners =
            new Dictionary<InputSystemUIInputModule, UIInputSystemAdapter>();
        private readonly List<ModalLease> _leases = new List<ModalLease>();
        private UIContext _context;
        private InputManager _input;
        private InputSystemUIInputModule _module;
        private InputActionMap _uiMap;
        private bool _reconciling;
        private bool _faulted;
        private bool _quarantined;
        private bool _cancelHeld;
        private int _releasedFrame = -1;

        /// <summary>Connects one explicit borrowed context and runtime UI map to its configured native module on Unity's main thread.</summary>
        /// <param name="context">Borrowed live UI owner whose explicit EventSystem matches this module's EventSystem.</param>
        /// <param name="input">Borrowed live runtime action-clone owner; layer registration must already be complete.</param>
        /// <param name="module">Borrowed module with all action references configured to the runtime UI map.</param>
        /// <param name="uiMapId">Stable ID of the configured UI map in the borrowed runtime clone.</param>
        /// <remarks>
        /// No global EventSystem or input owner is resolved. One adapter/context may bind a native module at a time.
        /// Native application failures fault the context, which owns safe blocking until shutdown even after Unbind.
        /// Ordinary project DisplayChanged listener failures remain separate from native failures.
        /// </remarks>
        /// <exception cref="ArgumentException">Map, action references, or explicit EventSystem do not match the scope.</exception>
        /// <exception cref="InvalidOperationException">Thread violation, competing binding, or unavailable owners.</exception>
        public void Bind(UIContext context, InputManager input, InputSystemUIInputModule module, Guid uiMapId)
        {
            UIContext.EnsureMainThread();
            if (_context != null)
            {
                throw new InvalidOperationException("This adapter already has an input binding.");
            }
            if (context == null || context.IsDisposed || context.Fault != null || context.RootObject == null
                || input == null || input.IsDisposed || input.Actions == null || input.Layers.IsFaulted || module == null)
            {
                throw new InvalidOperationException("Binding requires live, healthy borrowed owners.");
            }
            if (uiMapId == Guid.Empty)
            {
                throw new ArgumentException("An explicit runtime UI map ID is required.", nameof(uiMapId));
            }
            InputActionMap map = input.Actions.FindActionMap(uiMapId);
            if (map == null || context.EventSystem == null || module.GetComponent<EventSystem>() != context.EventSystem)
            {
                throw new ArgumentException("The runtime UI map and explicit EventSystem must match the borrowed scope.");
            }
            if (ModuleOwners.TryGetValue(module, out UIInputSystemAdapter owner))
            {
                if (owner != null)
                {
                    throw new InvalidOperationException("This native module already has an interactive binding.");
                }
                ModuleOwners.Remove(module);
            }
            foreach (BaseInputModule other in context.EventSystem.GetComponents<BaseInputModule>())
            {
                if (other != module && other.isActiveAndEnabled)
                {
                    throw new InvalidOperationException("The explicit EventSystem has another active input module.");
                }
            }
            ValidateConfiguration(input, module, map);
            // Attach before publishing native ownership; a competing context binding must not mutate this module.
            context.AttachInputBridge(this, Apply, EmergencyBlock, Unbind);
            _context = context;
            _input = input;
            _module = module;
            _uiMap = map;
            ModuleOwners.Add(module, this);
            input.Layers.Changed += OnLayersChanged;
            NativeInputSystem.onAfterUpdate += OnInputUpdate;
            try
            {
                Apply();
            }
            catch (Exception error)
            {
                ReportFailure(error);
                Unbind();
                throw;
            }
        }

        /// <summary>Acquires one independent lease from an explicitly registered project modal layer.</summary>
        /// <param name="layerId">Existing project layer ID. The caller must supply a compatible mapless BlockLower layer.</param>
        /// <returns>An owned lease whose retirement, while bound, waits for raw input release and a subsequent EventSystem frame.</returns>
        /// <remarks>
        /// Compatible layer configuration is a caller precondition; this adapter does not inspect private layer metadata
        /// or register layers. Each call first resets/quarantines native processing, owns exactly one lease, and never
        /// releases another modal/transition/external lease. Unbind detaches an active lease for direct release by its UI owner.
        /// Pass this factory explicitly to UIContext. Raw controls are inspected even when the UI map is disabled.
        /// </remarks>
        /// <exception cref="ArgumentException">The layer ID is empty or not registered.</exception>
        /// <exception cref="InvalidOperationException">Thread violation or no usable binding.</exception>
        public IDisposable AcquireModalBlock(string layerId)
        {
            UIContext.EnsureMainThread();
            EnsureBinding();
            if (string.IsNullOrWhiteSpace(layerId))
            {
                throw new ArgumentException("A registered modal layer ID is required.", nameof(layerId));
            }
            ValidateConfiguration(_input, _module, _uiMap);
            IDisposable acquired = _input.Layers.AcquireLayer(layerId);
            if (_faulted)
            {
                acquired.Dispose();
                throw new InvalidOperationException("Native input binding has faulted.", _context.Fault);
            }
            var lease = new ModalLease(this, acquired);
            _leases.Add(lease);
            try
            {
                // Already-held native input must not become the incoming modal's first event.
                Quarantine();
                return lease;
            }
            catch (Exception error)
            {
                ReportFailure(error);
                _leases.Remove(lease);
                try
                {
                    lease.ForceRelease();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(error, cleanupError);
                }
                throw;
            }
        }

        /// <summary>Unsubscribes and force-retires only outgoing/pending modal recovery leases on Unity's main thread.</summary>
        /// <remarks>
        /// Idempotent and also used on disable, destruction, and UI owner shutdown. Disables the borrowed module and
        /// refreshes usable input policy. Borrowed services/action references and independent external leases survive.
        /// Active modal leases remain owned by their UI generations; their detached wrappers release directly on Dispose.
        /// Already-retired wrappers remain harmless. Context-owned emergency blocking remains until owner shutdown.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public void Unbind()
        {
            UIContext.EnsureMainThread();
            if (_context == null)
            {
                return;
            }
            UIContext context = _context;
            InputManager input = _input;
            InputSystemUIInputModule module = _module;
            _context = null;
            input.Layers.Changed -= OnLayersChanged;
            NativeInputSystem.onAfterUpdate -= OnInputUpdate;
            ModuleOwners.Remove(module);
            var errors = new List<Exception>();
            TryCleanup(() => DisableModule(module), errors);
            foreach (ModalLease lease in _leases)
            {
                if (lease.Pending)
                {
                    TryCleanup(lease.ForceRelease, errors);
                }
                else
                {
                    lease.Detach();
                }
            }
            _leases.Clear();
            if (CanRefresh(input))
            {
                TryCleanup(input.Layers.Refresh, errors);
            }
            TryCleanup(() => context.SetNativeInputAvailable(this, false), errors);
            context.DetachInputBridge(this);
            _input = null;
            _module = null;
            _uiMap = null;
            _quarantined = false;
            _faulted = false;
            _cancelHeld = false;
            _releasedFrame = -1;
            if (errors.Count != 0)
            {
                throw new AggregateException(errors);
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            OnInputUpdate();
        }

        private void LateUpdate()
        {
            if (_context == null || _faulted || !_quarantined || _releasedFrame < 0
                || Time.frameCount <= _releasedFrame || _context.EventSystem == null
                || !_context.EventSystem.isActiveAndEnabled)
            {
                return;
            }
            try
            {
                // EventSystem.Update has now crossed a later frame with the native module still disabled.
                if (HasHeldControls())
                {
                    _releasedFrame = -1;
                    return;
                }
                for (int index = _leases.Count - 1; index >= 0; --index)
                {
                    if (_leases[index].Pending)
                    {
                        _leases[index].ForceRelease();
                        _leases.RemoveAt(index);
                    }
                }
                _quarantined = false;
                _releasedFrame = -1;
                Apply();
            }
            catch (Exception error)
            {
                ReportFailure(error);
            }
        }

        private void OnInputUpdate()
        {
            if (_context == null)
            {
                return;
            }
            try
            {
                if (_faulted)
                {
                    DisableModule(_module);
                    if (CanRefresh(_input))
                    {
                        _input.Layers.Refresh();
                    }
                    return;
                }
                Apply();
                bool heldCancel = IsHeld(_module.cancel.action);
                bool freshCancel = heldCancel && !_cancelHeld;
                _cancelHeld = heldCancel;
                if (freshCancel && UiMapAllowed() && !_quarantined)
                {
                    UIHandle target = _context.GetCancelTarget(this);
                    if (target != null)
                    {
                        Quarantine();
                        RequestCancelAsync(target).Forget();
                    }
                }
                if (_quarantined)
                {
                    if (HasHeldControls())
                    {
                        _releasedFrame = -1;
                    }
                    else if (_releasedFrame < 0)
                    {
                        _releasedFrame = Time.frameCount;
                    }
                }
            }
            catch (Exception error)
            {
                ReportFailure(error);
            }
        }

        private async UniTaskVoid RequestCancelAsync(UIHandle target)
        {
            try
            {
                await target.RequestCloseAsync(UIUserCloseReason.Cancel);
            }
            catch (OperationCanceledException) when (target.LifetimeToken.IsCancellationRequested || target.Context.IsDisposed)
            {
                // Forced owner/display termination cancels pending approval; its owner observes that cleanup.
            }
            catch (Exception error)
            {
                // Observe project approval/cleanup failures without converting ordinary observer errors into native faults.
                Debug.LogException(error, this);
            }
        }

        private void OnLayersChanged(InputLayerSnapshot snapshot)
        {
            if (_context == null || _reconciling || _faulted)
            {
                return;
            }
            try
            {
                Apply();
            }
            catch (Exception error)
            {
                // Never throw into Core's Changed dispatch: its layer policy must remain healthy and independently owned.
                ReportFailure(error);
            }
        }

        private void Apply()
        {
            if (_reconciling)
            {
                return;
            }
            EnsureBinding();
            ValidateConfiguration(_input, _module, _uiMap);
            _reconciling = true;
            try
            {
                // A borrowed module may have been enabled externally since our last callback.
                _input.Layers.Refresh();
                bool allowed = UiMapAllowed();
                bool enable = allowed && !_quarantined;
                if (_module.enabled != enable)
                {
                    if (enable)
                    {
                        _module.enabled = true;
                    }
                    else
                    {
                        DisableModule(_module);
                    }
                    _input.Layers.Refresh();
                }
                _context.SetNativeInputAvailable(this, allowed);
            }
            finally
            {
                _reconciling = false;
            }
        }

        private void Quarantine()
        {
            _quarantined = true;
            _releasedFrame = -1;
            DisableModule(_module);
            _input.Layers.Refresh();
            // Native processing is paused, but valid generation focus remains until the UI layer itself is blocked.
            _context.SetNativeInputAvailable(this, UiMapAllowed());
        }

        private void Retire(ModalLease lease)
        {
            UIContext.EnsureMainThread();
            if (lease.Pending || lease.Released)
            {
                return;
            }
            lease.Pending = true;
            try
            {
                EnsureBinding();
                Quarantine();
            }
            catch (Exception error)
            {
                ReportFailure(error);
                throw;
            }
        }

        private IDisposable EmergencyBlock()
        {
            // Context invokes this factory once and owns the returned lease through final owner shutdown.
            _faulted = true;
            _quarantined = true;
            DisableModule(_module);
            _input.Layers.Refresh();
            _context.SetNativeInputAvailable(this, false);
            return _input.Layers.BlockAll();
        }

        private void ReportFailure(Exception error)
        {
            try
            {
                _context?.ReportInputFailure(this, error);
            }
            catch (Exception reportingError)
            {
                Debug.LogException(new AggregateException(error, reportingError), this);
            }
        }

        private bool UiMapAllowed()
        {
            foreach (Guid mapId in _input.Layers.Snapshot.ActiveMapIds)
            {
                if (mapId == _uiMap.id)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasHeldControls()
        {
            return IsHeld(_module.leftClick.action) || IsHeld(_module.rightClick.action)
                || IsHeld(_module.middleClick.action) || IsHeld(_module.submit.action)
                || IsHeld(_module.cancel.action) || IsHeld(_module.move.action);
        }

        private static bool IsHeld(InputAction action)
        {
            // Native controls keep device state independently of action/map enablement, including touch press controls.
            foreach (InputControl control in action.controls)
            {
                if (control.device.added && (control is ButtonControl button
                    ? button.isPressed : control.EvaluateMagnitude() > 0f))
                {
                    return true;
                }
            }
            return false;
        }

        private void EnsureBinding()
        {
            if (_context == null || _context.IsDisposed || _context.Fault != null || _context.RootObject == null
                || _input == null || _input.IsDisposed || _input.Actions == null || _input.Layers.IsFaulted
                || _module == null || _context.EventSystem == null
                || _module.GetComponent<EventSystem>() != _context.EventSystem || _faulted)
            {
                throw new InvalidOperationException("There is no usable native input binding.");
            }
        }

        private static void ValidateConfiguration(InputManager input, InputSystemUIInputModule module, InputActionMap map)
        {
            if (module.actionsAsset != input.Actions)
            {
                throw new ArgumentException("The native module must use the borrowed runtime action clone.");
            }
            ValidateAction(module.point, map);
            ValidateAction(module.leftClick, map);
            ValidateAction(module.rightClick, map);
            ValidateAction(module.middleClick, map);
            ValidateAction(module.move, map);
            ValidateAction(module.submit, map);
            ValidateAction(module.cancel, map);
            ValidateAction(module.scrollWheel, map);
            ValidateAction(module.trackedDevicePosition, map);
            ValidateAction(module.trackedDeviceOrientation, map);
        }

        private static void ValidateAction(InputActionReference reference, InputActionMap map)
        {
            if (reference == null || reference.action == null || reference.action.actionMap != map)
            {
                throw new ArgumentException("Every native action reference must belong to the configured runtime UI map.");
            }
        }

        private static bool CanRefresh(InputManager input)
        {
            return input != null && !input.IsDisposed && input.Actions != null && !input.Layers.IsFaulted;
        }

        private static void DisableModule(InputSystemUIInputModule module)
        {
            if (module != null && module.enabled)
            {
                // Cancel while native callbacks are still hooked so cached navigation becomes zero, then reset pointers.
                module.move?.action?.Disable();
                module.enabled = false;
            }
        }

        private static void TryCleanup(Action cleanup, List<Exception> errors)
        {
            try
            {
                cleanup();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        private sealed class ModalLease : IDisposable
        {
            private UIInputSystemAdapter _owner;
            private IDisposable _lease;
            internal bool Pending;
            internal bool Released => _lease == null;

            internal ModalLease(UIInputSystemAdapter owner, IDisposable lease)
            {
                _owner = owner;
                _lease = lease;
            }

            public void Dispose()
            {
                UIContext.EnsureMainThread();
                if (_owner != null)
                {
                    _owner.Retire(this);
                }
                else
                {
                    ForceRelease();
                }
            }

            internal void Detach()
            {
                _owner = null;
            }

            internal void ForceRelease()
            {
                IDisposable lease = _lease;
                _lease = null;
                _owner = null;
                lease?.Dispose();
            }
        }
    }
}