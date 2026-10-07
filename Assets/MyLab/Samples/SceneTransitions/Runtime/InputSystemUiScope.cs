using System;
using System.Threading;
using MyLab.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

namespace MyLab.Samples.SceneTransitions
{
    /// <summary>Project-owned UI module adapter; borrows clone references and owns focus reset at block boundaries.</summary>
    public sealed class InputSystemUiScope : MonoBehaviour
    {
        private InputManager _input;
        private InputSystemUIInputModule _module;
        private InputActionMap _uiMap;
        private InputLayerSnapshot _pendingSnapshot;
        private int _threadId;
        private int _restoreAfterFrame;
        private bool _wanted;
        private bool _waitingForRestore;
        private bool _reconciling;

        /// <summary>
        /// Borrows an active scope and a module configured with references from the clone's one UI map.
        /// Inactive configuration is allowed. The caller owns references and rebinds after adapter disable.
        /// Binding twice or mixing assets/maps is rejected.
        /// </summary>
        public void Bind(InputManager input, InputSystemUIInputModule module, Guid uiMapId)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            if (_input != null)
            {
                throw new InvalidOperationException("The UI module is already bound.");
            }
            var snapshot = input.Layers.Snapshot;
            var map = input.Actions.FindActionMap(uiMapId.ToString());
            if (uiMapId == Guid.Empty || map == null)
            {
                throw new ArgumentException("The UI map must belong to the borrowed input scope.", nameof(uiMapId));
            }
            ValidateModule(input, module, map);
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _input = input;
            _module = module;
            _uiMap = map;
            _wanted = false;
            input.Layers.Changed += OnLayersChanged;
            Reconcile(snapshot);
        }

        /// <summary>Reapplies module permission on the Unity thread after external native module lifecycle changes.</summary>
        public void Refresh()
        {
            if (_input == null)
            {
                return;
            }
            EnsureThread();
            if (OwnerUnavailable() || _module == null)
            {
                Unbind();
                return;
            }
            Reconcile(_input.Layers.Snapshot);
        }

        /// <summary>Unsubscribes and disables the borrowed module without disposing input or destroying caller references.</summary>
        public void Unbind()
        {
            if (_input == null)
            {
                return;
            }
            EnsureThread();
            var input = _input;
            var module = _module;
            _input = null;
            _module = null;
            _uiMap = null;
            _pendingSnapshot = null;
            _wanted = false;
            _waitingForRestore = false;
            input.Layers.Changed -= OnLayersChanged;
            DisableModule(module);
        }

        private void OnLayersChanged(InputLayerSnapshot snapshot)
        {
            Reconcile(snapshot);
        }

        private void Reconcile(InputLayerSnapshot snapshot)
        {
            if (_reconciling)
            {
                _pendingSnapshot = snapshot;
                return;
            }
            _reconciling = true;
            try
            {
                do
                {
                    _pendingSnapshot = null;
                    if (_input == null)
                    {
                        return;
                    }
                    if (OwnerUnavailable() || _module == null)
                    {
                        Unbind();
                        return;
                    }
                    ValidateModule(_input, _module, _uiMap);
                    ApplyPermission(snapshot);
                    snapshot = _pendingSnapshot;
                }
                while (snapshot != null);
            }
            catch (Exception failure)
            {
                try
                {
                    Unbind();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }
            finally
            {
                _reconciling = false;
            }
        }

        private void ApplyPermission(InputLayerSnapshot snapshot)
        {
            bool wanted = !snapshot.AllInputBlocked;
            bool mapActive = false;
            foreach (var id in snapshot.ActiveMapIds)
            {
                if (id == _uiMap.id)
                {
                    mapActive = true;
                    break;
                }
            }
            wanted &= mapActive;
            bool blockedBoundary = !wanted && _wanted;
            if (wanted && !_wanted)
            {
                _waitingForRestore = true;
                _restoreAfterFrame = Time.frameCount + 1;
            }
            _wanted = wanted;
            bool permitModule = isActiveAndEnabled && wanted;
            if (_waitingForRestore)
            {
                permitModule = isActiveAndEnabled && wanted && Time.frameCount >= _restoreAfterFrame && ButtonsReleased();
            }
            bool moduleChanged = _module.enabled != permitModule;
            if (!permitModule)
            {
                DisableModule(_module, moduleChanged || blockedBoundary);
            }
            else if (!_module.enabled)
            {
                _module.enabled = true;
            }
            if (permitModule)
            {
                _waitingForRestore = false;
            }
            if (OwnerUnavailable())
            {
                Unbind();
                return;
            }
            // Native module Enable/Disable manages individual actions; the layer owner restores its current policy.
            if (moduleChanged || _uiMap.enabled != wanted)
            {
                _input.Layers.Refresh();
            }
        }

        private bool ButtonsReleased()
        {
            return Released(_module.leftClick) && Released(_module.rightClick)
                && Released(_module.middleClick) && Released(_module.submit);
        }

        private static bool Released(InputActionReference reference)
        {
            if (reference == null)
            {
                return true;
            }
            foreach (var control in reference.action.controls)
            {
                if (control is ButtonControl button && button.isPressed)
                {
                    return false;
                }
            }
            return true;
        }

        private static void DisableModule(InputSystemUIInputModule module, bool clearFocus = true)
        {
            if (module == null)
            {
                return;
            }
            module.enabled = false;
            // Native OnDisable clears pointer state; selection belongs to this project's focus boundary.
            var eventSystem = module.GetComponent<EventSystem>();
            if (clearFocus && eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private static void ValidateModule(InputManager input, InputSystemUIInputModule module, InputActionMap map)
        {
            if (module.actionsAsset != input.Actions || module.GetComponent<EventSystem>() == null)
            {
                throw new ArgumentException("The UI module must use the runtime clone and its own EventSystem.", nameof(module));
            }
            ValidateReference(module.point, map);
            ValidateReference(module.move, map);
            ValidateReference(module.leftClick, map);
            ValidateReference(module.rightClick, map);
            ValidateReference(module.middleClick, map);
            ValidateReference(module.scrollWheel, map);
            ValidateReference(module.submit, map);
            ValidateReference(module.cancel, map);
            ValidateReference(module.trackedDevicePosition, map);
            ValidateReference(module.trackedDeviceOrientation, map);
        }

        private static void ValidateReference(InputActionReference reference, InputActionMap map)
        {
            if (reference != null && (reference.action == null || reference.action.actionMap != map))
            {
                throw new ArgumentException("Configured UI references must belong to the runtime clone's UI map.");
            }
        }

        private bool OwnerUnavailable()
        {
            return _input == null || _input.IsDisposed || _input.Actions == null || _input.Layers.IsFaulted;
        }

        private void EnsureThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId)
            {
                throw new InvalidOperationException("UI scope operations require the binding Unity thread.");
            }
        }

        private void LateUpdate() => Refresh();
        private void OnEnable()
        {
            if (_input != null)
            {
                _waitingForRestore = true;
                _restoreAfterFrame = Time.frameCount + 1;
                Refresh();
            }
        }
        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
