using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPLab.UI.Installation
{
    /// <summary>Owns one UIContext through an explicitly ordered SceneRoot installation.</summary>
    /// <remarks>
    /// Settings, hosts, prefab assets, ResourceManager, EventSystem, providers, and project hooks remain borrowed.
    /// Install creates a context and registers metadata only. Prepare optionally preloads sources then selects the
    /// first HUD. Place this installer after borrowed resource/input installers so reverse Release awaits UI cleanup
    /// before those owners terminate. No input service, global lookup, or automatic input publication is added.
    /// Configuration and lifecycle calls require Unity's main thread.
    /// </remarks>
    public sealed class UIContextInstaller : SceneRootInstaller
    {
        /// <summary>Names a borrowed native container without changing its ownership or Canvas settings.</summary>
        [Serializable]
        public struct HostBinding
        {
            [SerializeField] private string _id;
            [SerializeField] private Transform _container;

            /// <summary>Stores a host reference; Configure and Install validate it before registering a context.</summary>
            /// <param name="id">Unique nonblank ID, excluding the implicit default owner-root host.</param>
            /// <param name="container">Live scene Transform whose lifetime the project preserves.</param>
            public HostBinding(string id, Transform container)
            {
                _id = id;
                _container = container;
            }

            /// <summary>Gets the physical host ID.</summary>
            public string Id => _id;
            /// <summary>Gets the borrowed native container.</summary>
            public Transform Container => _container;
        }

        [SerializeField] private UIContextSettings _settings;
        [SerializeField] private HostBinding[] _hosts = Array.Empty<HostBinding>();
        [SerializeField] private ResourceManagerInstaller _resources;
        [SerializeField] private EventSystem _eventSystem;
        private Func<string, CancellationToken, UniTask<GameObject>> _loadPrefab;
        private Func<IDisposable> _acquireModalBlock;
        private UIHooks _firstHudHooks;
        private ISceneRoot _owner;
        private string _firstHudDefinitionId;
        private string[] _preloadDefinitionIds = Array.Empty<string>();
        private UniTaskCompletionSource _preparation;
        private bool _stopping;

        /// <summary>Gets this installed owner's context; null before Install and after Uninstall.</summary>
        /// <remarks>Release leaves its terminating context observable until Uninstall clears the reference.</remarks>
        public UIContext Context { get; private set; }

        /// <summary>Validates script-side configuration and copies host bindings before installation.</summary>
        /// <param name="settings">Borrowed registration metadata, also validated when installed from the Inspector.</param>
        /// <param name="hosts">Additional borrowed scene containers; null means only the owner-root default host.</param>
        /// <param name="resources">Optional borrowed resource installer, installed earlier than this consumer.</param>
        /// <param name="loadPrefab">Optional borrowed provider, mutually exclusive with resources.</param>
        /// <param name="eventSystem">Optional explicitly borrowed EventSystem for focus; no global fallback is resolved.</param>
        /// <param name="acquireModalBlock">Optional project factory whose returned modal leases are owned by the context.</param>
        /// <param name="firstHudHooks">Optional project hooks for the configured first HUD, snapshotted by UIContext on acceptance.</param>
        /// <remarks>Invalid input preserves prior configuration. No context, asset load, or clone is created.</remarks>
        /// <exception cref="ArgumentException">Settings, host/source bindings, or mutually exclusive providers are invalid.</exception>
        /// <exception cref="InvalidOperationException">Already installed or called outside Unity's main thread.</exception>
        public void Configure(UIContextSettings settings, HostBinding[] hosts = null,
            ResourceManagerInstaller resources = null,
            Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
            EventSystem eventSystem = null, Func<IDisposable> acquireModalBlock = null,
            UIHooks firstHudHooks = null)
        {
            UIContext.EnsureMainThread();
            if (_owner != null || Context != null)
            {
                throw new InvalidOperationException("Configure the UI installer before installation.");
            }
            var hostCopy = hosts == null ? Array.Empty<HostBinding>() : (HostBinding[])hosts.Clone();
            ValidateConfiguration(settings, hostCopy, resources, loadPrefab, eventSystem);
            _settings = settings;
            _hosts = hostCopy;
            _resources = resources;
            _loadPrefab = loadPrefab;
            _eventSystem = eventSystem;
            _acquireModalBlock = acquireModalBlock;
            _firstHudHooks = firstHudHooks;
        }

        /// <summary>Snapshots settings and creates/registers one root-owned context without loading or cloning UI.</summary>
        /// <param name="root">Live scene root containing this installer; owns the context lifetime.</param>
        /// <remarks>
        /// A referenced resource installer must already expose a non-disposed ResourceManager; initialization belongs
        /// to its later Prepare. The provider captures that specific borrowed manager, never a replacement installation.
        /// Partial context setup is cleaned before an installation failure propagates. Borrowed objects survive.
        /// </remarks>
        /// <exception cref="ArgumentException">Root ownership or settings/host/source metadata is invalid.</exception>
        /// <exception cref="InvalidOperationException">Already installed, missing resource installation, or wrong thread.</exception>
        /// <exception cref="AggregateException">Installation and partial cleanup both fail.</exception>
        public override void Install(ISceneRoot root)
        {
            UIContext.EnsureMainThread();
            if (_owner != null || Context != null)
            {
                throw new InvalidOperationException("The UI context is already installed.");
            }
            if (ReferenceEquals(root, null) || root is UnityEngine.Object nativeRoot && nativeRoot == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            GameObject rootObject = root.RootObject;
            if (rootObject == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (!rootObject.scene.IsValid() || rootObject.transform.parent != null
                || !transform.IsChildOf(rootObject.transform))
            {
                throw new ArgumentException("The installer must belong to the supplied scene root.", nameof(root));
            }
            var hosts = _hosts == null ? Array.Empty<HostBinding>() : (HostBinding[])_hosts.Clone();
            IReadOnlyList<UIDefinition> definitions = ValidateConfiguration(_settings, hosts, _resources, _loadPrefab, _eventSystem);
            ResourceManager resourceScope = _resources != null ? _resources.Resources : null;
            if (_resources != null && (resourceScope == null || resourceScope.IsDisposed))
            {
                throw new InvalidOperationException("Install the borrowed resource owner before the UI consumer.");
            }
            Func<string, CancellationToken, UniTask<GameObject>> provider = _loadPrefab;
            if (resourceScope != null)
            {
                provider = resourceScope.LoadAssetAsync<GameObject>;
            }
            IReadOnlyList<string> preloads = _settings.PreloadDefinitionIds;
            var preloadCopy = new string[preloads.Count];
            for (int index = 0; index < preloads.Count; ++index)
            {
                preloadCopy[index] = preloads[index];
            }
            _owner = root;
            _firstHudDefinitionId = _settings.FirstHudDefinitionId;
            _preloadDefinitionIds = preloadCopy;
            _preparation = null;
            _stopping = false;
            try
            {
                Context = new UIContext(rootObject, provider, _acquireModalBlock, _eventSystem);
                foreach (HostBinding host in hosts)
                {
                    Context.RegisterHost(host.Id, host.Container);
                }
                foreach (UIDefinition definition in definitions)
                {
                    Context.Register(definition);
                }
            }
            catch (Exception error)
            {
                try
                {
                    Uninstall(root);
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(error, cleanupError);
                }
                throw;
            }
        }

        /// <summary>Shares optional source preloading followed by exactly one configured first-HUD selection.</summary>
        /// <param name="root">The same installed root identity.</param>
        /// <param name="cancellationToken">Root preparation lifetime for the first attempt; repeated calls cancel only their wait.</param>
        /// <returns>Shared preparation success, original asset/hook failure, or cancellation.</returns>
        /// <remarks>
        /// Source preloads create no display or warm clone. First-HUD display uses the normal UIContext selection path.
        /// This installer never releases InputManager's independent preparation block; the project completes it explicitly.
        /// A failed attempt remains observable until Uninstall; root shutdown must still run Release/Uninstall.
        /// </remarks>
        /// <exception cref="ArgumentException">The supplied root is not this installation's owner.</exception>
        /// <exception cref="InvalidOperationException">Uninstalled, terminating, or called outside Unity's main thread.</exception>
        /// <exception cref="OperationCanceledException">The root preparation or context lifetime ends.</exception>
        public override UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            EnsureOwner(root, true);
            cancellationToken.ThrowIfCancellationRequested();
            Context.LifetimeToken.ThrowIfCancellationRequested();
            UniTaskCompletionSource preparation = _preparation;
            if (preparation == null)
            {
                preparation = new UniTaskCompletionSource();
                _preparation = preparation;
                PrepareOwnedAsync(Context, preparation, cancellationToken).Forget();
            }
            return preparation.Task.AttachExternalCancellation(cancellationToken);
        }

        private async UniTask PrepareOwnedAsync(UIContext context, UniTaskCompletionSource completion, CancellationToken token)
        {
            try
            {
                foreach (string id in _preloadDefinitionIds)
                {
                    EnsurePreparing(context, token);
                    await context.PrepareAsync(id, token);
                    await UniTask.SwitchToMainThread();
                }
                EnsurePreparing(context, token);
                if (!string.IsNullOrEmpty(_firstHudDefinitionId))
                {
                    await context.SelectHudAsync(new UIOpenRequest(_firstHudDefinitionId, hooks: _firstHudHooks), token);
                    await UniTask.SwitchToMainThread();
                }
                EnsurePreparing(context, token);
                completion.TrySetResult();
            }
            catch (OperationCanceledException error)
            {
                await UniTask.SwitchToMainThread();
                completion.TrySetCanceled(error.CancellationToken);
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                completion.TrySetException(error);
            }
        }

        private void EnsurePreparing(UIContext context, CancellationToken token)
        {
            UIContext.EnsureMainThread();
            token.ThrowIfCancellationRequested();
            context.LifetimeToken.ThrowIfCancellationRequested();
            if (_stopping || !ReferenceEquals(Context, context))
            {
                throw new OperationCanceledException(context.LifetimeToken);
            }
        }

        /// <summary>Shares uncancelled UI shutdown before earlier borrowed resource/input installers release their services.</summary>
        /// <param name="root">The same installed root; native destruction may already have made it Unity-null.</param>
        /// <returns>Owned display/cache destruction and cleanup completion; completed when no context was created.</returns>
        /// <exception cref="ArgumentException">The supplied root identity does not match the installed owner.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread or forbidden context shutdown reentry.</exception>
        /// <exception cref="AggregateException">UI cleanup failed after all remaining cleanup was attempted.</exception>
        public override UniTask ReleaseAsync(ISceneRoot root)
        {
            EnsureOwner(root);
            UniTask shutdown = Context?.ShutdownAsync() ?? UniTask.CompletedTask;
            _stopping = true;
            return shutdown;
        }

        /// <summary>Clears the installed reference and starts idempotent fallback UI cleanup, including partial installation.</summary>
        /// <param name="root">The same owner identity; fake-null Unity roots are allowed for destruction fallback.</param>
        /// <remarks>
        /// Normal roots await Release first. Immediate fallback cannot await native destruction or project work.
        /// State is cleared even if accepted cleanup reports an error; borrowed root, assets, services, and hosts survive.
        /// </remarks>
        /// <exception cref="ArgumentException">The supplied root identity does not match the installed owner.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread or forbidden context shutdown reentry, rejected before clearing.</exception>
        /// <exception cref="AggregateException">Already-completed synchronous UI cleanup failed.</exception>
        public override void Uninstall(ISceneRoot root)
        {
            EnsureOwner(root);
            UIContext context = Context;
            context?.EnsureNoReentry();
            Context = null;
            _owner = null;
            _preparation = null;
            _preloadDefinitionIds = Array.Empty<string>();
            _firstHudDefinitionId = null;
            _stopping = true;
            context?.Dispose();
        }

        private void EnsureOwner(ISceneRoot root, bool requireInstalled = false)
        {
            UIContext.EnsureMainThread();
            if (ReferenceEquals(root, null))
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (_owner != null && !ReferenceEquals(_owner, root))
            {
                throw new ArgumentException("The root does not own this UI installation.", nameof(root));
            }
            if (requireInstalled && (_owner == null || Context == null || _stopping))
            {
                throw new InvalidOperationException("Install the UI context before preparation and stop using it after release.");
            }
            if (root is UnityEngine.Object nativeRoot && nativeRoot == null)
            {
                if (requireInstalled)
                {
                    throw new ObjectDisposedException(nameof(root));
                }
                return;
            }
            if (Context != null && Context.RootObject != null && root.RootObject != Context.RootObject)
            {
                throw new ArgumentException("The installed root object cannot be replaced.", nameof(root));
            }
        }

        private static IReadOnlyList<UIDefinition> ValidateConfiguration(UIContextSettings settings, HostBinding[] hosts,
            ResourceManagerInstaller resources, Func<string, CancellationToken, UniTask<GameObject>> provider,
            EventSystem eventSystem)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if ((!ReferenceEquals(resources, null) && resources == null)
                || (resources != null && provider != null))
            {
                throw new ArgumentException("Choose one live resource installer or one custom provider.", nameof(resources));
            }
            if (!ReferenceEquals(eventSystem, null) && eventSystem == null)
            {
                throw new ArgumentException("The supplied EventSystem has been destroyed.", nameof(eventSystem));
            }
            IReadOnlyList<UIDefinition> definitions = settings.CreateSnapshot();
            var hostIds = new HashSet<string>(StringComparer.Ordinal) { "default" };
            foreach (HostBinding host in hosts)
            {
                if (string.IsNullOrWhiteSpace(host.Id) || !hostIds.Add(host.Id)
                    || host.Container == null || !host.Container.gameObject.scene.IsValid())
                {
                    throw new ArgumentException("Additional hosts must have unique IDs and live scene containers.", nameof(hosts));
                }
            }
            foreach (UIDefinition definition in definitions)
            {
                if (!hostIds.Contains(definition.HostId))
                {
                    throw new ArgumentException("A definition refers to an unregistered host.", nameof(settings));
                }
                if (!string.IsNullOrWhiteSpace(definition.AssetKey) && resources == null && provider == null)
                {
                    throw new ArgumentException("Key definitions require an explicit prefab provider.", nameof(settings));
                }
            }
            return definitions;
        }
    }
}