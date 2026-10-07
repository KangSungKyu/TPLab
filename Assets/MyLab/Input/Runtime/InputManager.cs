using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace MyLab.Core.Input
{
    /// <summary>Owns one runtime action clone. Consumers borrow actions and own their subscriptions.</summary>
    public sealed class InputManager : IDisposable
    {
        private readonly InputActionAsset _source;
        private readonly int _threadId;
        private bool _disposed;
        private bool _actionsDestroyed;
        private bool _shutdownStarted;
        private UniTask _shutdownTask;

        /// <summary>Borrowed runtime clone. Activation, overrides and destruction belong to this manager.</summary>
        public InputActionAsset Actions { get; }

        /// <summary>Controls map activation through independent project-owned leases.</summary>
        public InputLayerController Layers { get; }

        /// <summary>Owns native rebinding and binding override transactions for this scope.</summary>
        public InputRebindingController Rebinding { get; }

        /// <summary>Whether graceful or immediate shutdown has begun; no new work is accepted afterward.</summary>
        public bool IsDisposed => _disposed;

        /// <summary>Creates a disabled scope from a borrowed source asset without modifying the source.</summary>
        public InputManager(InputActionAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _threadId = Thread.CurrentThread.ManagedThreadId;
            _source = source;
            Actions = Object.Instantiate(source);
            try
            {
                Actions.name = source.name + " (Input Scope)";
                Actions.Disable();
                Layers = new InputLayerController(Actions, EnsureActive);
                Rebinding = new InputRebindingController(this);
            }
            catch
            {
                DestroyActions();
                throw;
            }
        }

        /// <summary>Resolves a stable action ID in this scope; the returned native action is borrowed.</summary>
        public InputAction GetAction(Guid actionId)
        {
            EnsureActive();
            return Actions.FindAction(actionId) ?? throw new ArgumentException("Action ID does not belong to this scope.", nameof(actionId));
        }

        /// <summary>Resolves a source/runtime reference into this scope and rejects foreign assets.</summary>
        public InputAction GetAction(InputActionReference reference)
        {
            EnsureActive();
            InputAction action = reference == null ? null : reference.action;
            if (action == null || (action.actionMap.asset != _source && action.actionMap.asset != Actions))
            {
                throw new ArgumentException("Reference must belong to the source or runtime asset.", nameof(reference));
            }

            return GetAction(action.id);
        }

        /// <summary>Stops this scope and shares graceful completion; no native input request is abandoned.</summary>
        public UniTask ShutdownAsync()
        {
            EnsureThread();
            if (!_shutdownStarted)
            {
                _shutdownStarted = true;
                _shutdownTask = ShutdownCoreAsync().Preserve();
            }
            return _shutdownTask;
        }

        private async UniTask ShutdownCoreAsync()
        {
            _disposed = true;
            Exception failure = null;
            try
            {
                StopOwnedInput();
            }
            catch (Exception error)
            {
                failure = error;
            }
            try
            {
                await Rebinding.WaitForCompletionAsync();
            }
            finally
            {
                DestroyActions();
            }
            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>Immediately stops the scope; repeated teardown and late lease disposal are harmless.</summary>
        public void Dispose()
        {
            EnsureThread();
            if (_disposed)
            {
                DestroyActions();
                return;
            }

            _disposed = true;
            try
            {
                StopOwnedInput();
            }
            finally
            {
                DestroyActions();
            }
        }

        private void StopOwnedInput()
        {
            try
            {
                Rebinding.Stop();
            }
            catch (Exception rebindingFailure)
            {
                try
                {
                    Layers.Stop();
                }
                catch (Exception layerFailure)
                {
                    throw new AggregateException(rebindingFailure, layerFailure);
                }
                throw;
            }
            Layers.Stop();
        }

        internal void EnsureActive()
        {
            EnsureThread();
            if (_disposed || Actions == null)
            {
                throw new ObjectDisposedException(nameof(InputManager));
            }

            if (Layers != null && Layers.IsFaulted)
            {
                throw new InvalidOperationException("The input scope is faulted.", Layers.Fault);
            }
        }

        internal void EnsureThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId)
            {
                throw new InvalidOperationException("Input scope operations require the creating Unity thread.");
            }
        }

        private void DestroyActions()
        {
            if (_actionsDestroyed || Actions == null)
            {
                return;
            }

            _actionsDestroyed = true;

            if (Application.isPlaying)
            {
                Object.Destroy(Actions);
            }
            else
            {
                Object.DestroyImmediate(Actions);
            }
        }
    }
}
