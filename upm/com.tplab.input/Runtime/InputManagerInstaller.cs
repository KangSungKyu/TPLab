using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPLab.Core.Input
{
    /// <summary>Owns an input scope on either scene root; project code explicitly publishes prepared input.</summary>
    public sealed class InputManagerInstaller : SceneRootInstaller
    {
        [SerializeField] private InputActionAsset _source;
        private ISceneRoot _root;
        private IDisposable _preparation;

        /// <summary>The root-owned scope, borrowed by consumers; null before installation and after uninstall.</summary>
        public InputManager Input { get; private set; }

        /// <summary>Assigns the borrowed source before installation; null or late configuration is rejected.</summary>
        public void Configure(InputActionAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (Input != null)
            {
                throw new InvalidOperationException("Configure the input source before installation.");
            }
            _source = source;
        }

        /// <summary>Releases only this installer's preparation block after its live root is prepared.</summary>
        public void CompletePreparation()
        {
            if (Input == null || _root == null || (_root as Component) == null || _root.RootObject == null)
            {
                throw new InvalidOperationException("The input scope is not installed on a live root.");
            }
            _ = Input.Layers.Snapshot;
            if (!_root.IsPrepared)
            {
                throw new InvalidOperationException("Prepare the owning root before publishing input.");
            }
            var preparation = _preparation;
            _preparation = null;
            preparation?.Dispose();
        }

        /// <summary>Creates a disabled clone and an independent preparation block on its owning scene root.</summary>
        public override void Install(ISceneRoot root)
        {
            ValidateRootArgument(root);
            if (Input != null)
            {
                throw new InvalidOperationException("The input scope is already installed.");
            }
            if ((root as Component) == null)
            {
                throw new ArgumentException("The scene root must be alive.", nameof(root));
            }
            var rootObject = root.RootObject;
            if (rootObject == null || !rootObject.scene.IsValid() || rootObject.transform.parent != null
                || (transform != rootObject.transform && !transform.IsChildOf(rootObject.transform)))
            {
                throw new ArgumentException("The installer must belong to the same root object or its children.", nameof(root));
            }
            if (_source == null)
            {
                throw new InvalidOperationException("Assign an input source before installation.");
            }

            var input = new InputManager(_source);
            try
            {
                var preparation = input.Layers.BlockAll();
                _root = root;
                _preparation = preparation;
                Input = input;
            }
            catch (Exception failure)
            {
                try
                {
                    input.Dispose();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }
        }

        /// <summary>Checks the root and cancellation only; preparation never implicitly publishes gameplay.</summary>
        public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            ValidateInstalledRoot(root);
            cancellationToken.ThrowIfCancellationRequested();
            _ = Input.Layers.Snapshot;
            return UniTask.CompletedTask;
        }

        /// <summary>Drains the installed scope before root uninstall; cleanup is not caller-cancellable.</summary>
        public override UniTask ReleaseAsync(ISceneRoot root)
        {
            ValidateRootArgument(root);
            if (_root != null && !ReferenceEquals(root, _root))
            {
                throw new ArgumentException("Only the owning root may release this scope.", nameof(root));
            }
            return Input?.ShutdownAsync() ?? UniTask.CompletedTask;
        }

        /// <summary>Clears borrowed references, immediately disposes the owner and invalidates late preparation cleanup.</summary>
        public override void Uninstall(ISceneRoot root)
        {
            ValidateRootArgument(root);
            if (_root != null && !ReferenceEquals(root, _root))
            {
                throw new ArgumentException("Only the owning root may uninstall this scope.", nameof(root));
            }
            var input = Input;
            var preparation = _preparation;
            Input = null;
            _root = null;
            _preparation = null;
            try
            {
                input?.Dispose();
            }
            finally
            {
                preparation?.Dispose();
            }
        }

        private void ValidateInstalledRoot(ISceneRoot root)
        {
            ValidateRootArgument(root);
            if (Input == null || _root == null)
            {
                throw new InvalidOperationException("Install the input scope before preparation.");
            }
            if (!ReferenceEquals(root, _root))
            {
                throw new ArgumentException("The root does not own this input scope.", nameof(root));
            }
        }

        private static void ValidateRootArgument(ISceneRoot root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (!(root is SceneOwnedRoot) && !(root is SingletonSceneRoot))
            {
                throw new ArgumentException("Use a SceneOwnedRoot or SingletonSceneRoot host.", nameof(root));
            }
        }

        private void OnDestroy()
        {
            if (Input != null)
            {
                Uninstall(_root);
            }
        }
    }
}
