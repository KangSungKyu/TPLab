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

        /// <summary>Borrowed runtime clone. Activation, overrides and destruction belong to this manager.</summary>
        public InputActionAsset Actions { get; }

        /// <summary>Controls map activation through independent project-owned leases.</summary>
        public InputLayerController Layers { get; }

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
            Dispose();
            return UniTask.CompletedTask;
        }

        /// <summary>Immediately stops the scope; repeated teardown and late lease disposal are harmless.</summary>
        public void Dispose()
        {
            EnsureThread();
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                Layers.Stop();
            }
            finally
            {
                DestroyActions();
            }
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
            if (Actions == null)
            {
                return;
            }

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
