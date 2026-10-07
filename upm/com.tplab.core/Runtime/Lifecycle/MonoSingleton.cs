using System;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    /// <summary>
    /// Registers one explicitly created component of T on Unity's main thread.
    /// Override the hooks instead of hiding Awake, OnEnable, OnDestroy or OnApplicationQuit.
    /// Duplicate components are removed without destroying their GameObjects.
    /// </summary>
    /// <typeparam name="T">The concrete singleton component type.</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isBusy;
        [NonSerialized] private bool _lifecycleClaimed;
        [NonSerialized] private bool _initialized;
        [NonSerialized] private bool _rejected;

        static MonoSingleton()
        {
            SingletonRuntime.Register(ResetStatics, RestoreSceneInstances);
        }

        /// <summary>
        /// Gets the successfully initialized owner, or null outside Play, during shutdown,
        /// or when no owner exists. Never searches for or creates a component.
        /// A disabled owner remains registered until destroyed.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (!Application.isPlaying || SingletonRuntime.IsQuitting)
                {
                    return null;
                }
                if (_instance == null)
                {
                    _instance = null;
                }
                return _instance;
            }
        }

        /// <summary>Gets whether this component completed initialization and owns its lifecycle.</summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Chooses scene persistence at initialization. Defaults to scene lifetime.
        /// Persistent owners must be root GameObjects; their children also persist.
        /// </summary>
        protected virtual bool PersistAcrossScenes => false;

        /// <summary>
        /// Initializes before Instance is published. An exception is logged, invokes cleanup
        /// once for partial state, and removes this component. Keep initialization synchronous.
        /// </summary>
        protected virtual void OnSingletonInitialize() { }

        /// <summary>
        /// Releases owned resources after Instance is cleared, including partial initialization.
        /// Runs once per initialization attempt on destruction or reset between Play sessions.
        /// Exceptions are logged without leaving the owner registered.
        /// </summary>
        protected virtual void OnSingletonShutdown() { }

        /// <summary>Registers the component when it first becomes active.</summary>
        protected void Awake() => InitializeIfNeeded();

        /// <summary>Restores registration when entering Play without a scene reload.</summary>
        protected void OnEnable() => InitializeIfNeeded();

        /// <summary>Clears registration and releases owned resources.</summary>
        protected void OnDestroy() => ReleaseOwnership();

        /// <summary>Prevents registration and lookup while the application exits Play.</summary>
        protected void OnApplicationQuit() => SingletonRuntime.MarkQuitting();

        private void InitializeIfNeeded()
        {
            if (!Application.isPlaying || _lifecycleClaimed || _rejected)
            {
                return;
            }
            if (SingletonRuntime.IsQuitting || _isBusy || _instance != null)
            {
                RejectComponent();
                return;
            }

            _isBusy = true;
            try
            {
                if (!(this is T owner))
                {
                    throw new InvalidOperationException("Singleton type must match its concrete component.");
                }
                bool persistent = PersistAcrossScenes;
                ValidateRoot(persistent);
                _lifecycleClaimed = true;
                OnSingletonInitialize();
                if (this == null || !_lifecycleClaimed)
                {
                    throw new InvalidOperationException("Singleton was destroyed during initialization.");
                }
                ValidateRoot(persistent);
                if (persistent)
                {
                    DontDestroyOnLoad(gameObject);
                }
                _initialized = true;
                _instance = owner;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ReleaseOwnership();
                RejectComponent();
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void ValidateRoot(bool persistent)
        {
            if (persistent && transform.parent != null)
            {
                throw new InvalidOperationException("Persistent singletons require a root GameObject.");
            }
        }

        private void RejectComponent()
        {
            _rejected = true;
            if (this != null)
            {
                enabled = false;
                Destroy(this);
            }
        }

        private void ReleaseOwnership()
        {
            if (!_lifecycleClaimed)
            {
                return;
            }
            _lifecycleClaimed = false;
            _initialized = false;
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
            bool wasBusy = _isBusy;
            _isBusy = true;
            try
            {
                OnSingletonShutdown();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isBusy = wasBusy;
            }
        }

        private static void ResetStatics()
        {
            var previous = _instance;
            _instance = null;
            _isBusy = false;
            if (previous != null)
            {
                previous.ReleaseOwnership();
            }
        }

        private static void RestoreSceneInstances()
        {
            // Awake can be skipped when Scene Reload is disabled. Only known singleton types
            // are scanned once at Play entry; lookups never scan scenes.
            foreach (var owner in FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (owner.gameObject.scene.IsValid() && owner.gameObject.activeInHierarchy)
                {
                    owner.InitializeIfNeeded();
                }
            }
        }
    }
}
