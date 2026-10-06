using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Configures first entry and delegates scene ownership to an explicit GameSceneManager.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyLab/Bootstrap System")]
    public sealed class BootstrapSystem : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _sceneRoot;
        [SerializeField] private string _firstScenePath = "";
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private SceneTransitionCallbacks _callbacks;
        [SerializeField] private LoadSceneMode _loadMode = LoadSceneMode.Additive;
        [SerializeField] private SceneSource _sceneSource = SceneSource.BuildScene;
        [SerializeField] private string _addressableKey = "";
        [SerializeField] private AssetReference _sceneReference;
        [SerializeField] private SceneTransitionSettings _transitionSettings;
        [SerializeField] private string _firstTransitionId = "";
        private bool _stopping;
        /// <summary>Explicit manager created before common preparation; null before the first entry command.</summary>
        public GameSceneManager Manager { get; private set; }
        /// <summary>Successfully prepared/revealed game scene, or invalid before completion and during shutdown.</summary>
        public Scene GameScene => Manager?.GameScene ?? default;
        /// <summary>First game load mode. Single requires a persistent common root.</summary>
        public LoadSceneMode LoadMode => UsesDefinition ? GetFirstDefinition(_transitionSettings, _firstTransitionId).Mode : _loadMode;

        /// <summary>Explicit lifecycle host; Bootstrap never searches for global services.</summary>
        public MonoBehaviour SceneRoot => _sceneRoot;
        /// <summary>Full Assets scene path selected by the project.</summary>
        public string FirstScenePath => UsesDefinition ? FirstSceneTarget.ScenePath : _firstScenePath;
        /// <summary>Explicit first-scene loader source; legacy serialized settings default to Build Scene.</summary>
        public SceneSource Source => UsesDefinition ? FirstSceneTarget.Source : _sceneSource;
        /// <summary>Unique scene address, mutually exclusive with a configured scene reference.</summary>
        public string AddressableKey => UsesDefinition ? FirstSceneTarget.AddressableKey : _addressableKey;
        /// <summary>Optional explicit scene reference, mutually exclusive with a string scene address.</summary>
        public AssetReference SceneReference => UsesDefinition ? null : _sceneReference;
        /// <summary>Validated first target; an Addressable requires exactly one key/reference and an explicit scene path.</summary>
        public SceneTarget FirstSceneTarget
        {
            get
            {
                if (UsesDefinition) return GetFirstDefinition(_transitionSettings, _firstTransitionId).Target;
                // Unity can deserialize an empty inline AssetReference even when Configure assigned null.
                if (_sceneSource == SceneSource.Addressable && _sceneReference != null &&
                    !string.IsNullOrEmpty(_sceneReference.AssetGUID))
                {
                    if (!string.IsNullOrWhiteSpace(_addressableKey))
                        throw new InvalidOperationException("Select one Addressables key or scene reference, not both.");
                    return SceneTarget.Addressable(_sceneReference, _firstScenePath);
                }
                return new SceneTarget(_sceneSource, _firstScenePath,
                    _sceneSource == SceneSource.BuildScene ? null : _addressableKey);
            }
        }
        /// <summary>Starts preparation from Start when enabled; false permits explicit execution.</summary>
        public bool AutoStart => _autoStart;
        /// <summary>Optional explicit definition asset; existing path/source entry remains available when absent.</summary>
        public SceneTransitionSettings Settings => _transitionSettings;
        /// <summary>Optional FirstEntry definition ID within Settings.</summary>
        public string FirstTransitionId => _firstTransitionId;

        /// <summary>Configures one FirstEntry definition before execution, preserving existing serialized fields and entry ownership.</summary>
        public void Configure(MonoBehaviour sceneRoot, SceneTransitionSettings settings, string firstTransitionId,
            bool autoStart = true, SceneTransitionCallbacks callbacks = null)
        {
            if (Manager != null || _stopping) throw new InvalidOperationException("Configure Bootstrap before its first entry attempt.");
            GetFirstDefinition(settings, firstTransitionId);
            _sceneRoot = sceneRoot;
            _autoStart = autoStart;
            _callbacks = callbacks;
            _transitionSettings = settings;
            _firstTransitionId = firstTransitionId;
        }

        /// <summary>Configures entry before execution. Inspector and code use the same validation.</summary>
        public void Configure(MonoBehaviour sceneRoot, string firstScenePath, bool autoStart = true, SceneTransitionCallbacks callbacks = null,
            LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            if (Manager != null || _stopping)
            {
                throw new InvalidOperationException("Configure Bootstrap before its first entry attempt.");
            }
            _sceneRoot = sceneRoot;
            _firstScenePath = firstScenePath;
            _autoStart = autoStart;
            _callbacks = callbacks;
            _loadMode = loadMode;
            _sceneSource = SceneSource.BuildScene;
            _addressableKey = "";
            _sceneReference = null;
            _transitionSettings = null;
            _firstTransitionId = "";
        }

        /// <summary>Configures an explicit Build/Addressable target before entry, preserving the existing lifecycle and mode contract.</summary>
        public void Configure(MonoBehaviour sceneRoot, SceneTarget target, bool autoStart = true, SceneTransitionCallbacks callbacks = null,
            LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            target.Validate();
            Configure(sceneRoot, target.ScenePath, autoStart, callbacks, loadMode);
            _sceneSource = target.Source;
            _addressableKey = target.AddressableKey ?? "";
        }

        /// <summary>Rejects invalid root ownership or entry settings before initialization.</summary>
        public void ValidateConfiguration()
        {
            ValidateSceneRoot(_sceneRoot, gameObject.scene, true);
            var definition = UsesDefinition ? GetFirstDefinition(_transitionSettings, _firstTransitionId) : null;
            var mode = definition != null ? definition.Mode : _loadMode;
            var target = definition != null ? definition.Target : FirstSceneTarget;
            if (mode != LoadSceneMode.Additive && mode != LoadSceneMode.Single)
                throw new InvalidOperationException("Select Single or Additive as the first load mode.");
            if (mode == LoadSceneMode.Single && !IsPersistent(_sceneRoot))
                throw new InvalidOperationException("Single requires a persistent common root; it cannot retain Bootstrap.");
            if (IsPersistent(_sceneRoot) && (!transform.IsChildOf(_sceneRoot.transform) ||
                (_callbacks != null && !_callbacks.transform.IsChildOf(_sceneRoot.transform))))
                throw new InvalidOperationException("Bootstrap and callbacks must survive under the persistent common root.");
            target.Validate();
            if (target.ScenePath == gameObject.scene.path)
                throw new InvalidOperationException("Bootstrap cannot load itself as the game scene.");
            if (_callbacks != null && _callbacks.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Callbacks must belong to the retained Bootstrap scene.");
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("Bootstrap must be active and enabled.");
        }

        /// <summary>Checks a standard, active, nonpersistent, top-level lifecycle host in its owning scene; common hosts may opt into persistence.</summary>
        public static void ValidateSceneRoot(MonoBehaviour host, Scene scene, bool allowPersistence = false)
        {
            if (!(host is SceneOwnedRoot) && !(host is SingletonSceneRoot))
                throw new InvalidOperationException("Select a SceneOwnedRoot or SingletonSceneRoot.");
            if (host == null || !scene.IsValid() || host.gameObject.scene != scene ||
                host.transform.parent != null || !host.isActiveAndEnabled)
                throw new InvalidOperationException("The lifecycle host must be an active root in its owning scene.");
            if (host.GetComponents<SceneOwnedRoot>().Length + host.GetComponents<SingletonSceneRoot>().Length != 1)
                throw new InvalidOperationException("A root must have exactly one lifecycle host.");
            if (!allowPersistence && IsPersistent(host))
                throw new InvalidOperationException("Additive scenes own their roots; disable DontDestroyOnLoad.");
        }

        /// <summary>Checks a normalized Assets scene path; existence and build inclusion are checked separately.</summary>
        public static void ValidateScenePath(string path) => SceneTarget.ValidateScenePath(path);

        /// <summary>Shares one entry attempt; caller cancellation stops only its wait. Failure keeps the cover.</summary>
        public UniTask BootstrapAsync(CancellationToken cancellationToken = default)
        {
            GameSceneManager.EnsureMainThread();
            if (_stopping) throw new ObjectDisposedException(nameof(BootstrapSystem));
            cancellationToken.ThrowIfCancellationRequested();
            if (Manager != null) return Manager.WaitForEntryAsync(cancellationToken);
            ValidateConfiguration();
            Manager = new GameSceneManager(_sceneRoot, _callbacks, settings: UsesDefinition ? _transitionSettings : null);
            try
            {
                return UsesDefinition ? AwaitDefinitionEntryAsync(Manager, Manager.TryTransitionAsync(_firstTransitionId, cancellationToken)) :
                    Manager.EnterFirstSceneAsync(FirstSceneTarget, _loadMode, cancellationToken);
            }
            catch
            {
                Manager = null; throw;
            }
        }

        private bool UsesDefinition => _transitionSettings != null || !string.IsNullOrEmpty(_firstTransitionId);

        private static SceneTransitionDefinition GetFirstDefinition(SceneTransitionSettings settings, string id)
        {
            if (settings == null || string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Select transition settings and a FirstEntry definition ID together.");
            var definition = settings.CreateSnapshot().SingleOrDefault(entry => entry.Id == id);
            if (definition == null || definition.Kind != SceneTransitionKind.FirstEntry)
                throw new InvalidOperationException("Bootstrap requires an existing FirstEntry definition ID.");
            return definition;
        }

        private async UniTask AwaitDefinitionEntryAsync(GameSceneManager owner, UniTask<bool> attempt)
        {
            if (await attempt) return;
            if (ReferenceEquals(Manager, owner)) Manager = null;
            throw new SceneTransitionRejectedException("The selected first entry definition was rejected before execution.");
        }

        /// <summary>Delegates game cleanup to the manager; the external root owner releases common systems separately.</summary>
        public UniTask ShutdownAsync()
        {
            GameSceneManager.EnsureMainThread();
            var shutdown = Manager?.ShutdownAsync() ?? UniTask.CompletedTask;
            _stopping = true;
            return shutdown;
        }

        private void Start()
        {
            if (_autoStart)
            {
                try
                {
                    BootstrapAsync().Forget(Debug.LogException);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void OnDestroy()
        {
            // Graceful release requires awaiting ShutdownAsync before destroying this component.
            if (_stopping) return;
            try
            {
                ShutdownAsync().Forget(Debug.LogException);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Reset()
        {
            _sceneRoot = (MonoBehaviour)GetComponent<SceneOwnedRoot>() ?? GetComponent<SingletonSceneRoot>();
        }

        internal static bool IsPersistent(MonoBehaviour host) =>
            (host is SceneOwnedRoot owned && owned.PersistsAcrossScenes) ||
            (host is SingletonSceneRoot singleton && singleton.PersistsAcrossScenes);
    }
}
