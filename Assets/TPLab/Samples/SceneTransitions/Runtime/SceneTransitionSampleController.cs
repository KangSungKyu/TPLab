using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.Input;
using TPLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TPLab.Samples.SceneTransitions
{
    /// <summary>Project-owned presentation borrowing Bootstrap.Manager; owns only cloned input and independent UI blocking.</summary>
    public sealed partial class SceneTransitionSampleController : SceneTransitionCallbacks
    {
        [SerializeField] private BootstrapSystem _bootstrap;
        [SerializeField] private InputActionAsset _inputSource;
        [SerializeField] private CanvasGroup _cover;
        [SerializeField] private CanvasGroup _modal;
        [SerializeField] private InputSystemUIInputModule _module;
        [SerializeField] private Text _status;
        [SerializeField] private Text _modalMessage;
        [SerializeField] private Button[] _buttons = Array.Empty<Button>();
        [SerializeField] private Button _closeModal;
        private InputManager _input;
        private InputSystemUiScope _uiScope;
        private IDisposable _gameplayLease;
        private IDisposable _uiLease;
        private IDisposable _modalLease;
        private IDisposable _transitionLease;
        private string _savedOverrides;
        private InputActionMap _player;
        private InputActionMap _ui;
        private bool _gameplayReady;
        private bool _failNextPrepare;
        private bool _smokeRunning;
        private readonly List<InputActionReference> _ownedReferences = new List<InputActionReference>();

        public bool TransitionBlocked
        {
            get;
            private set;
        }
        public bool ModalBlocked
        {
            get;
            private set;
        }
        public bool PolicyAllowed
        {
            get;
            private set;
        } = true;
        public bool PlayerInputEnabled => _player != null && _player.enabled;
        public bool UiInputEnabled => _ui != null && _ui.enabled;
        /// <summary>Borrowed sample scope for a project settings view; this controller owns its lifetime.</summary>
        public InputManager Input => _input;
        public GameSceneManager Manager => _bootstrap != null ? _bootstrap.Manager : null;
        public int CoverCount
        {
            get;
            private set;
        }
        public int RevealCount
        {
            get;
            private set;
        }
        public int FailureCount
        {
            get;
            private set;
        }
        public int PreparationCount
        {
            get;
            private set;
        }
        public int PresentationCount
        {
            get;
            private set;
        }
        public int ReleaseCount
        {
            get;
            private set;
        }
        public int GameplayInputCount
        {
            get;
            private set;
        }

        /// <summary>Explicit saved sample wiring; never modifies the borrowed core manager.</summary>
        public void ConfigureSample(BootstrapSystem bootstrap, InputActionAsset source, CanvasGroup cover, CanvasGroup modal,
            InputSystemUIInputModule module, Text status, Text modalMessage, Button[] buttons, Button closeModal)
        {
            _bootstrap = bootstrap;
            _inputSource = source;
            _cover = cover;
            _modal = modal;
            _module = module;
            _status = status;
            _modalMessage = modalMessage;
            _buttons = buttons;
            _closeModal = closeModal;
        }

        private void Awake()
        {
            if (_inputSource == null) return;
            // Input contract tests wire the view explicitly before activation.
            ConfigureView(_inputSource, _cover, _modal, _module);
            string[] commands = { "replace", "area", "nested", "nested-self", "area-self", "area-parent", "modal", "policy", "fail" };
            for (int i = 0; i < _buttons.Length; i++)
            {
                string command = commands[i];
                _buttons[i].onClick.AddListener(() => RunCommandAsync(command).Forget());
            }
            _closeModal.onClick.AddListener(() => SetModalOpen(false));
            _player.FindAction("Attack", true).performed += OnGameplayInput;
        }

        private void Start()
        {
            if (_bootstrap != null) StartSessionAsync().Forget();
        }

        private async UniTask StartSessionAsync()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool smoke = args.Contains("-tplab-scene-smoke");
            try
            {
                if (args.Contains("-tplab-loading-presentation"))
                {
                    ConfigureLoadingPresentation(true);
                }
                if (smoke)
                {
                    int index = Array.IndexOf(args, "-tplab-scene-result");
                    if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Smoke requires -tplab-scene-result <absolute path>.");
                    await RunSmokeAsync(args[index + 1], true);
                }
                else
                {
                    await _bootstrap.BootstrapAsync();
                    Status("Ready. Player Attack increments the gameplay input counter.");
                }
            }
            catch (Exception exception)
            {
                Status(exception.Message);
                if (smoke) Application.Quit(1);
            }
        }

        private void Update()
        {
            if (_bootstrap == null || _input == null) return;
            RefreshGameplayPermission(Manager != null && Manager.CanProceed);
            bool ready = Manager != null && Manager.CanProceed && !TransitionBlocked && !ModalBlocked;
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool valid = ready;
                if (i == 1) valid &= !HasScene(SceneTransitionSamplePaths.Area);
                if (i == 2) valid &= HasScene(SceneTransitionSamplePaths.Area) && !HasScene(SceneTransitionSamplePaths.Nested);
                if (i == 3) valid &= HasScene(SceneTransitionSamplePaths.Nested);
                if (i == 4 || i == 5) valid &= HasScene(SceneTransitionSamplePaths.Area);
                _buttons[i].interactable = valid;
            }
        }

        private bool HasScene(string path) => Manager != null && Manager.RegisteredScenes.Any(item => item.Scene.path == path);
        private bool InMain => Manager.GameScene.path == SceneTransitionSamplePaths.Main;
        private void OnGameplayInput(InputAction.CallbackContext context)
        {
            GameplayInputCount++;
            Status("Gameplay input: " + GameplayInputCount);
        }
        private void Status(string message)
        {
            if (_status != null) _status.text = message;
        }

        /// <summary>Clones project actions, binds the real UI module and borrows explicit views once.</summary>
        public void ConfigureView(InputActionAsset source, CanvasGroup cover, CanvasGroup modal, InputSystemUIInputModule module)
        {
            if (_input != null) throw new InvalidOperationException("Configure the sample view once.");
            if (source == null || cover == null || modal == null || module == null) throw new ArgumentException("Explicit sample input and views are required.");
            _cover = cover;
            _modal = modal;
            _module = module;
            module.enabled = false;
            _input = new InputManager(source);
            _player = _input.Actions.FindActionMap("Player", true);
            _ui = _input.Actions.FindActionMap("UI", true);
            _input.Layers.RegisterLayer("gameplay", new[] { _player.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("ui", new[] { _ui.id }, 2000, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
            _input.Layers.RegisterLayer("transition", Array.Empty<Guid>(), 1000, InputLayerMode.BlockLower);
            module.actionsAsset = _input.Actions;
            module.point = Reference("Point");
            module.leftClick = Reference("Click");
            module.rightClick = Reference("RightClick");
            module.middleClick = Reference("MiddleClick");
            module.scrollWheel = Reference("ScrollWheel");
            module.move = Reference("Navigate");
            module.submit = Reference("Submit");
            module.cancel = Reference("Cancel");
            _uiScope = module.gameObject.AddComponent<InputSystemUiScope>();
            _uiScope.Bind(_input, module, _ui.id);
            _uiLease = _input.Layers.AcquireLayer("ui");
            SetVisible(_cover, false);
            SetVisible(_modal, false);
        }

        /// <summary>Combines current manager permission with independent transition/modal ownership; UI stays enabled.</summary>
        public void RefreshGameplayPermission(bool canProceed)
        {
            _gameplayReady = canProceed;
            if (_input == null || _input.IsDisposed)
            {
                return;
            }
            if (_gameplayReady)
            {
                _gameplayLease ??= _input.Layers.AcquireLayer("gameplay");
            }
            else
            {
                _gameplayLease?.Dispose();
                _gameplayLease = null;
            }
        }
        /// <summary>Owns only modal blocking and focus; closing never releases transition-owned blocking.</summary>
        public void SetModalOpen(bool open)
        {
            ModalBlocked = open;
            if (open)
            {
                _modalLease ??= _input.Layers.AcquireLayer("modal");
            }
            else
            {
                _modalLease?.Dispose();
                _modalLease = null;
            }
            SetVisible(_modal, open);
            RefreshGameplayPermission(_gameplayReady);
            if (_closeModal != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(open ? _closeModal.gameObject : _buttons[0].gameObject);
        }
        /// <summary>Changes the sample business flag, leaving attached condition configuration fixed.</summary>
        public void SetPolicyAllowed(bool allowed)
        {
            PolicyAllowed = allowed;
            Status("Policy allowed: " + allowed);
        }
        /// <summary>Arms one candidate installer failure; does not start a transition or authorize recovery.</summary>
        public void FailNextPreparation()
        {
            _failNextPrepare = true;
            Status("Next candidate preparation will fail; explicit shutdown follows failure.");
        }
        public bool ConsumePrepareFailure()
        {
            bool fail = _failNextPrepare;
            _failNextPrepare = false;
            return fail;
        }
        public void RecordPreparation() => PreparationCount++;
        public void RecordPresentation() => PresentationCount++;
        public void RecordRelease() => ReleaseCount++;

        public override UniTask ShowCoverAsync(CancellationToken cancellationToken)
        {
            CoverCount++;
            TransitionBlocked = true;
            _transitionLease ??= _input.Layers.AcquireLayer("transition");
            SetVisible(_cover, true);
            RefreshGameplayPermission(_gameplayReady);
            return UniTask.CompletedTask;
        }
        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken)
        {
            var consumer = root.RootObject.GetComponentInChildren<SampleSceneConsumer>(true);
            if (consumer == null) throw new InvalidOperationException("Sample scene has no installed consumer.");
            consumer.Bind(this);
            return UniTask.CompletedTask;
        }
        public override UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken cancellationToken) =>
            root.RootObject.GetComponentInChildren<SampleSceneConsumer>(true).WaitForPresentationAsync(cancellationToken);
        public override async UniTask HideCoverAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(120, ignoreTimeScale: true, cancellationToken: cancellationToken);
            RevealCount++;
            TransitionBlocked = false;
            SetVisible(_cover, false);
            _transitionLease?.Dispose();
            _transitionLease = null;
            RefreshGameplayPermission(_gameplayReady);
        }
        public override void OnFailure(Exception exception)
        {
            FailureCount++;
            RefreshGameplayPermission(false);
            if (_modalMessage != null) _modalMessage.text = "Transition stopped\n" + exception.Message + "\nCover remains held. Close only dismisses this message.";
            SetModalOpen(true);
            Status(exception.Message);
        }

        private async UniTask RunCommandAsync(string command)
        {
            try
            {
                if (command == "modal")
                {
                    if (_modalMessage != null) _modalMessage.text = "Independent system modal\nGameplay remains blocked after any reveal.";
                    SetModalOpen(true);
                    return;
                }
                if (command == "policy")
                {
                    SetPolicyAllowed(!PolicyAllowed);
                    return;
                }
                if (command == "fail")
                {
                    FailNextPreparation();
                    return;
                }
                string id = command;
                if (command == "replace") id = InMain ? "to-hub" : "to-main";
                if (command == "area") id = InMain ? "area-main" : "area-hub";
                if (command == "area-parent") id = InMain ? "area-parent-main" : "area-parent-hub";
                bool accepted = await Manager.TryTransitionAsync(id);
                Status(accepted ? "Completed " + id : "Policy rejected " + id + "; no transition started.");
            }
            catch (Exception exception)
            {
                Status(exception.Message);
            }
        }

        /// <summary>Runs actual core operations in Play/Player; writes a fresh result and optionally exits a smoke Player.</summary>
        public async UniTask<SceneSampleSmokeResult> RunSmokeAsync(string resultPath, bool quitPlayer = false)
        {
            if (_smokeRunning) throw new InvalidOperationException("A sample smoke is already running.");
            string output = Path.GetFullPath(resultPath);
            if (!Path.IsPathRooted(resultPath) || File.Exists(output)) throw new ArgumentException("Use a fresh absolute result path.");
            _smokeRunning = true;
            var result = new SceneSampleSmokeResult
            {
                mode = _bootstrap.LoadMode.ToString(),
                unityVersion = Application.unityVersion
            };
            try
            {
                await _bootstrap.BootstrapAsync().Timeout(TimeSpan.FromSeconds(20));
                await UniTask.Yield();
                RefreshGameplayPermission(Manager.CanProceed);
                Require(Manager.CanProceed && PlayerInputEnabled, "entry");
                result.Entry = true;
                result.completedChecks++;
                result.observations.Add("entry prepared and Player enabled");
                if (_useLoadingPresentation)
                {
                    Require(LoadingPrepareCount == 1 && LoadingRevealCount == 1 && LoadingProgressCount > 0 && ProceedCount == 1 &&
                        LoadingReleaseCount == 1 && LoadingTwoCoverCount == 1 && _loadingLeaseObserved &&
                        _loadingPanel != null && !_loadingPanel.gameObject.activeInHierarchy, "loading presentation entry ownership");
                    result.LoadingFlow = true;
                    result.completedChecks++;
                    result.observations.Add("loading entry: native progress, automatic continuation, two covers, retained lease and released UI");
                }
                Require(await Manager.TryTransitionAsync("to-main"), "Hub to Main");
                Require(Manager.GameScene.path == SceneTransitionSamplePaths.Main, "Main actual scene");
                result.completedChecks++;
                result.observations.Add("Hub to Main actual destination");
                Require(await Manager.TryTransitionAsync("to-hub"), "Main to Hub");
                result.RoundTrip = true;
                result.completedChecks++;
                result.observations.Add("Hub to Main to Hub actual primary scenes");
                Require(await Manager.TryTransitionAsync("area-hub"), "area add");
                Require(await Manager.TryTransitionAsync("nested"), "nested add");
                Require(Manager.RegisteredScenes.Count == 3, "nested tree");
                result.NestedAdd = true;
                result.completedChecks++;
                result.observations.Add("primary/area/nested registrations = 3");
                Require(await Manager.TryTransitionAsync("nested-self"), "nested self remove");
                Require(!HasScene(SceneTransitionSamplePaths.Nested) && HasScene(SceneTransitionSamplePaths.Area), "self removal preserves parent");
                result.SelfRemoval = true;
                result.completedChecks++;
                result.observations.Add("nested self removal preserved area");
                Require(await Manager.TryTransitionAsync("nested"), "nested re-add");
                Require(await Manager.TryTransitionAsync("area-parent-hub"), "ancestor subtree remove");
                Require(Manager.RegisteredScenes.Count == 1, "ancestor removed subtree");
                result.AncestorRemoval = true;
                result.completedChecks++;
                result.observations.Add("ancestor removal drained subtree");
                SetPolicyAllowed(false);
                int covers = CoverCount, failures = FailureCount, releases = ReleaseCount;
                int preparations = PreparationCount, presentations = PresentationCount;
                var registrations = Manager.RegisteredScenes.ToArray();
                Scene old = Manager.GameScene;
                Require(!await Manager.TryTransitionAsync("to-main"), "condition false");
                Require(old == Manager.GameScene && CoverCount == covers && FailureCount == failures && ReleaseCount == releases &&
                    PreparationCount == preparations && PresentationCount == presentations && registrations.SequenceEqual(Manager.RegisteredScenes) &&
                    Manager.CanProceed, "rejection has no effects");
                result.ConditionNoEffects = true;
                result.completedChecks++;
                result.observations.Add("condition false: no cover, failure, release, preparation, presentation or registration change");
                SetPolicyAllowed(true);
                SetModalOpen(true);
                Require(await Manager.TryTransitionAsync("to-main"), "transition under modal");
                await UniTask.Yield();
                RefreshGameplayPermission(Manager.CanProceed);
                Require(ModalBlocked && !TransitionBlocked && !PlayerInputEnabled && UiInputEnabled, "independent modal after reveal");
                result.ModalHeldAfterReveal = true;
                result.completedChecks++;
                result.observations.Add("modal survived reveal; Player off/UI on");
                SetModalOpen(false);
                FailNextPreparation();
                bool failed = false;
                try
                {
                    await Manager.TryTransitionAsync("to-hub");
                }
                catch (Exception)
                {
                    failed = true;
                }
                Require(failed && !_failNextPrepare && Manager.LastFailure.ToString().Contains("Sample requested preparation failure.") &&
                    Manager.State == SceneTransitionState.Faulted && TransitionBlocked && _cover.blocksRaycasts && _cover.alpha == 1 &&
                    !PlayerInputEnabled && UiInputEnabled && FailureCount == failures + 1, "execution failure held");
                result.ExecutionFailureHeld = true;
                result.completedChecks++;
                result.observations.Add("execution failure: Faulted/cover held/Player off/UI on");
                if (_useLoadingPresentation)
                {
                    Require(_loadingOperation == null && _loadingPanel != null && !_loadingPanel.gameObject.activeInHierarchy &&
                        LoadingReleaseCount == LoadingPrepareCount && LoadingTwoCoverCount == ProceedCount &&
                        _loadingLeaseObserved && _cover.alpha == 1 && TransitionBlocked, "loading presentation failure cleanup");
                    result.LoadingFailureCleanup = true;
                    result.completedChecks++;
                    result.observations.Add("loading failure: operation UI/listeners released once, cover/transition lease retained");
                }
            }
            catch (Exception exception)
            {
                result.error = exception.ToString();
            }
            finally
            {
                try
                {
                    // This harness owns a normal recovery scene solely to satisfy Unity's last-scene unload rule.
                    if (_bootstrap.LoadMode == LoadSceneMode.Single)
                    {
                        SceneManager.CreateScene("SampleSmokeOwnedRecovery");
                        result.OwnedRecoveryCreated = true;
                    }
                    await _bootstrap.ShutdownAsync().Timeout(TimeSpan.FromSeconds(20));
                    var common = (ISceneRoot)_bootstrap.SceneRoot;
                    await common.ShutdownAsync().Timeout(TimeSpan.FromSeconds(20));
                    result.GracefulShutdown = Manager != null && Manager.State == SceneTransitionState.Stopped && !common.IsReady && !common.IsPrepared && Manager.OwnedScenes.Count == 0;
                    Require(result.GracefulShutdown, "graceful shutdown");
                    result.completedChecks++;
                    result.observations.Add("manager owned scenes = 0; common not ready or prepared; graceful Stopped");
                }
                catch (Exception exception)
                {
                    result.error += "\nShutdown: " + exception;
                }
                result.Preparations = PreparationCount;
                result.Presentations = PresentationCount;
                result.Releases = ReleaseCount;
                result.Covers = CoverCount;
                result.Reveals = RevealCount;
                result.Failures = FailureCount;
                result.LoadingPreparations = LoadingPrepareCount;
                result.LoadingReveals = LoadingRevealCount;
                result.LoadingProgressReports = LoadingProgressCount;
                result.Proceeds = ProceedCount;
                result.LoadingReleases = LoadingReleaseCount;
                result.LoadingTwoCovers = LoadingTwoCoverCount;
                result.success = string.IsNullOrEmpty(result.error) && result.completedChecks == (_useLoadingPresentation ? 12 : 10) && PreparationCount > 0 && PresentationCount > 0 && ReleaseCount > 0;
                result.exitCode = result.success ? 0 : 1;
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(result, true));
                _smokeRunning = false;
                if (quitPlayer) Application.Quit(result.exitCode);
            }
            return result;
        }
        private static void Require(bool valid, string step)
        {
            if (!valid) throw new InvalidOperationException("Smoke observation failed: " + step);
        }
        private InputActionReference Reference(string name)
        {
            var reference = InputActionReference.Create(_ui.FindAction(name, true));
            _ownedReferences.Add(reference);
            return reference;
        }

        [ContextMenu("Input/Rebind Attack Keyboard")]
        private void RebindAttackKeyboard() => RebindAttackKeyboardAsync().Forget(exception => Status(exception.Message));

        private async UniTask RebindAttackKeyboardAsync()
        {
            var attack = _player.FindAction("Attack", true);
            int bindingIndex = -1;
            for (int i = 0; i < attack.bindings.Count; i++)
            {
                if (attack.bindings[i].path.StartsWith("<Keyboard>/", StringComparison.Ordinal))
                {
                    bindingIndex = i;
                    break;
                }
            }
            if (bindingIndex < 0)
            {
                throw new InvalidOperationException("The sample needs a keyboard Attack binding.");
            }
            Status("Press and release a keyboard key. Escape cancels; timeout is 15 seconds.");
            var result = await _input.Rebinding.RebindAsync(new RebindRequest(attack.id, attack.bindings[bindingIndex].id)
            {
                ControlPath = "<Keyboard>", BindingGroup = "Keyboard&Mouse", TimeoutSeconds = 15
            }, this.GetCancellationTokenOnDestroy());
            Status(result.Status + ": " + attack.GetBindingDisplayString(bindingIndex));
        }

        [ContextMenu("Input/Save Overrides In Memory")]
        private void SaveInputOverrides()
        {
            _savedOverrides = _input.Rebinding.ExportOverridesJson();
            Status("Input overrides saved in sample memory.");
        }

        [ContextMenu("Input/Restore Overrides From Memory")]
        private void RestoreInputOverrides()
        {
            if (_savedOverrides == null)
            {
                throw new InvalidOperationException("Save overrides in this Play session first.");
            }
            _input.Rebinding.ImportOverridesJson(_savedOverrides);
            Status("Saved input overrides restored.");
        }

        [ContextMenu("Input/Reset Overrides")]
        private void ResetInputOverrides()
        {
            _input.Rebinding.ResetAll();
            Status("Input overrides reset to the source defaults.");
        }
        private static void SetVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1 : 0;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
        private void OnDestroy()
        {
            CleanupLoading();
            if (_loadingPanel != null)
            {
                Destroy(_loadingPanel.gameObject);
            }
            if (_uiScope != null)
            {
                _uiScope.Unbind();
            }
            if (_player != null)
            {
                _player.FindAction("Attack", true).performed -= OnGameplayInput;
            }
            _input?.Dispose();
            foreach (var reference in _ownedReferences)
            {
                if (reference != null)
                {
                    Destroy(reference);
                }
            }
        }
    }

    /// <summary>Observed smoke evidence; booleans are set only after the corresponding real operation/assertion.</summary>
    [Serializable]
    public sealed class SceneSampleSmokeResult
    {
        public bool success;
        public int exitCode;
        public string mode;
        public int completedChecks;
        public bool Entry, RoundTrip, NestedAdd, SelfRemoval, AncestorRemoval, ConditionNoEffects, ModalHeldAfterReveal, ExecutionFailureHeld, GracefulShutdown, OwnedRecoveryCreated;
        public int Preparations, Presentations, Releases, Covers, Reveals, Failures;
        public bool LoadingFlow, LoadingFailureCleanup;
        public int LoadingPreparations, LoadingReveals, LoadingProgressReports, Proceeds, LoadingReleases, LoadingTwoCovers;
        public string error = "";
        public string unityVersion;
        public List<string> observations = new List<string>();
    }
}
