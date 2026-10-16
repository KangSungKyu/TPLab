using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Input;
using TPLab.Core.Lifecycle;
using TPLab.Core.SceneManagement;
using TPLab.UI;
using TPLab.UI.InputSystem;
using TPLab.UI.Installation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TPLab.Samples.UI
{
    /// <summary>Project-owned walkthrough borrowing explicit installers and Bootstrap.Manager; no global lookup.</summary>
    public sealed class UIContextSampleController : SceneTransitionCallbacks
    {
        [SerializeField] private BootstrapSystem _bootstrap;
        [SerializeField] private UIContextInstaller _uiInstaller;
        [SerializeField] private InputManagerInstaller _inputInstaller;
        [SerializeField] private UIContextSettings _settings;
        [SerializeField] private InputActionAsset _sourceActions;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private InputSystemUIInputModule _module;
        [SerializeField] private UIInputSystemAdapter _adapter;
        [SerializeField] private Transform _host;
        [SerializeField] private RectTransform _cellPrefab;
        [SerializeField] private Text _status;
        [SerializeField] private CanvasGroup _cover;
        [SerializeField] private CanvasGroup _loading;
        [SerializeField] private Text _loadingStatus;
        [SerializeField] private Button _continue;
        [SerializeField] private Button[] _commands = Array.Empty<Button>();
        [SerializeField] private string[] _commandIds = Array.Empty<string>();
        private UnityEngine.Events.UnityAction[] _commandListeners = Array.Empty<UnityEngine.Events.UnityAction>();
        private readonly List<InputActionReference> _references = new List<InputActionReference>();
        private readonly Dictionary<int, long> _visibleRows = new Dictionary<int, long>();
        private readonly string[] _rows = new string[10000];
        private readonly Dictionary<int, UIContext> _sceneContexts = new Dictionary<int, UIContext>();
        private readonly List<int> _closedSceneIds = new List<int>();
        private int _sceneRetiredCount;
        private IDisposable _gameLease, _uiLease, _transitionLease, _shutdownBlock;
        private InputActionMap _playerMap, _uiMap;
        private ISceneRoot _commonRoot;
        private UIContext _sceneContext, _retiredSceneContext;
        private UIHandle _a, _b, _c, _child, _inventoryHandle, _transitionShield;
        private VirtualScrollRect _inventory;
        private ScrollRect _scroll;
        private int _gameInputCount;
        private bool _veto = true, _useLoading = true, _manualContinue = true;
        private bool _bound, _released, _busy, _loadingActive;
        private Guid _loadingId;
        private float _nextStatus;
        private string _message = "Bootstrap pending";

        /// <summary>Builder wiring before activation; every supplied asset/service remains borrowed.</summary>
        public void ConfigureSample(BootstrapSystem bootstrap, UIContextInstaller uiInstaller,
            InputManagerInstaller inputInstaller, UIContextSettings settings,
            InputActionAsset sourceActions, EventSystem eventSystem, InputSystemUIInputModule module,
            UIInputSystemAdapter adapter, Transform host, RectTransform cellPrefab, Text status,
            CanvasGroup cover, CanvasGroup loading, Text loadingStatus, Button proceed,
            Button[] commands, string[] commandIds)
        {
            _bootstrap = bootstrap; _uiInstaller = uiInstaller; _inputInstaller = inputInstaller;
            _settings = settings; _sourceActions = sourceActions;
            _eventSystem = eventSystem; _module = module; _adapter = adapter; _host = host;
            _cellPrefab = cellPrefab; _status = status; _cover = cover; _loading = loading;
            _loadingStatus = loadingStatus; _continue = proceed; _commands = commands; _commandIds = commandIds;
        }

        private void PrepareProjectControls()
        {
            for (int i = 0; i < _rows.Length; ++i) _rows[i] = "Item " + i.ToString("D5");
            if (_commands.Length != _commandIds.Length) throw new InvalidOperationException("Command wiring mismatch.");
            _commandListeners = new UnityEngine.Events.UnityAction[_commands.Length];
            for (int i = 0; i < _commands.Length; ++i)
            {
                string command = _commandIds[i];
                _commandListeners[i] = () => ExecuteCommandAsync(command);
                _commands[i].onClick.AddListener(_commandListeners[i]);
            }
            Visible(_cover, true); Visible(_loading, false); _continue.interactable = false;
        }
        private async void Start()
        {
            try { await _bootstrap.BootstrapAsync(this.GetCancellationTokenOnDestroy()); }
            catch (OperationCanceledException) when (this == null) { }
            catch (Exception error) { OnFailure(error); }
        }

        /// <summary>Called after Input.Install; owns only sample leases, subscriptions and runtime action references.</summary>
        public void InstallScope(ISceneRoot root)
        {
            _commonRoot = root;
            _released = false; _bound = false; _busy = false; _loadingActive = false;
            _a = _b = _c = _child = _inventoryHandle = _transitionShield = null;
            _inventory = null; _scroll = null; _sceneContext = _retiredSceneContext = null;
            _visibleRows.Clear(); _sceneContexts.Clear(); _closedSceneIds.Clear();
            _sceneRetiredCount = 0; _gameInputCount = 0;
            PrepareProjectControls();
            var input = _inputInstaller.Input;
            if (_sourceActions == null || input.Actions == _sourceActions)
                throw new InvalidOperationException("Sample input must borrow a source and use the installer's owned clone.");
            // Configure the next installer before UI.Install; root restoration does not depend on Controller.Awake order.
            _uiInstaller.Configure(_settings, new[] { new UIContextInstaller.HostBinding("sample", _host) },
                eventSystem: _eventSystem,
                acquireModalBlock: () => _adapter.AcquireModalBlock("modal"), firstHudHooks: ViewHooks());
            _playerMap = input.Actions.FindActionMap("Player", true);
            _uiMap = input.Actions.FindActionMap("UI", true);
            input.Layers.RegisterLayer("game", new[] { _playerMap.id }, 0, InputLayerMode.Overlay);
            input.Layers.RegisterLayer("ui", new[] { _uiMap.id }, 2000, InputLayerMode.Overlay);
            input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
            input.Layers.RegisterLayer("transition", Array.Empty<Guid>(), 1000, InputLayerMode.BlockLower);
            _gameLease = input.Layers.AcquireLayer("game"); _uiLease = input.Layers.AcquireLayer("ui");
            _module.enabled = false; _module.actionsAsset = input.Actions;
            _module.move = Reference("Navigate"); _module.submit = Reference("Submit");
            _module.cancel = Reference("Cancel"); _module.point = Reference("Point");
            _module.leftClick = Reference("Click"); _module.rightClick = Reference("RightClick");
            _module.middleClick = Reference("MiddleClick"); _module.scrollWheel = Reference("ScrollWheel");
            _module.trackedDevicePosition = Reference("TrackedDevicePosition");
            _module.trackedDeviceOrientation = Reference("TrackedDeviceOrientation");
            _playerMap.FindAction("Fire", true).performed += OnGameplayInput;
            input.Layers.Refresh();
        }

        /// <summary>All root Install calls have completed; binds the created UIContext before first UI.Prepare/modal display.</summary>
        public void BindScope()
        {
            if (_bound) return;
            _adapter.Bind(_uiInstaller.Context, _inputInstaller.Input, _module, _uiMap.id);
            _bound = true;
        }

        private InputActionReference Reference(string name)
        {
            var reference = InputActionReference.Create(_uiMap.FindAction(name, true));
            _references.Add(reference);
            return reference;
        }

        private void PublishPreparedInput()
        {
            if (_commonRoot == null || !_commonRoot.IsPrepared) throw new InvalidOperationException("Common root is not prepared.");
            _inputInstaller.CompletePreparation();
        }

        private void OnGameplayInput(InputAction.CallbackContext action)
        {
            if (_bootstrap.Manager != null && _bootstrap.Manager.CanProceed) ++_gameInputCount;
        }

        private UIHooks ViewHooks()
        {
            return new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    Button close = handle.ViewObject.GetComponentInChildren<Button>(true);
                    if (close != null)
                    {
                        UnityEngine.Events.UnityAction listener = () => ObserveCloseAsync(handle, UIUserCloseReason.Button);
                        close.onClick.AddListener(listener);
                        handle.RegisterCleanup(() => { if (close != null) close.onClick.RemoveListener(listener); });
                        handle.SetFocus(close.gameObject);
                    }
                    return UniTask.CompletedTask;
                },
                CanCloseAsync = (handle, reason, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    _message = handle.DefinitionId + " " + reason + (_veto ? " vetoed" : " accepted");
                    return UniTask.FromResult(!_veto);
                }
            };
        }

        private async UniTaskVoid ObserveCloseAsync(UIHandle handle, UIUserCloseReason reason)
        {
            try { await handle.RequestCloseAsync(reason, this.GetCancellationTokenOnDestroy()); }
            catch (Exception error) { RecordCommandFailure(error); }
        }

        private async UniTaskVoid ExecuteCommandAsync(string command)
        {
            // Project harness buttons can intentionally address a middle/blocked handle.
            if (_busy || _released) return;
            _busy = true;
            try
            {
                var context = _uiInstaller.Context;
                if (context == null || context.IsDisposed) throw new InvalidOperationException("UI owner unavailable.");
                switch (command)
                {
                    case "hud-a": case "hud-b":
                        await context.SelectHudAsync(new UIOpenRequest(command, hooks: ViewHooks())); break;
                    case "abc":
                        await CloseIfLiveAsync(_c); await CloseIfLiveAsync(_b); await CloseIfLiveAsync(_a);
                        _a = await context.OpenAsync(new UIOpenRequest("a", hooks: ViewHooks()));
                        _b = await context.OpenAsync(new UIOpenRequest("b", hooks: ViewHooks()));
                        _c = await context.OpenAsync(new UIOpenRequest("c", hooks: ViewHooks())); break;
                    case "close-b":
                        await CloseIfLiveAsync(_b);
                        _message = "B closed; C=" + (_c == null ? "absent" : _c.State + "/eligible=" + _c.CanReceiveInput); break;
                    case "child":
                        RequireVisible(_a);
                        _child = await context.OpenAsync(new UIOpenRequest("c", parent: _a, hooks: ViewHooks())); break;
                    case "close-child": await CloseIfLiveAsync(_child); break;
                    case "close-parent": await CloseIfLiveAsync(_a); break;
                    case "front-a": RequireVisible(_a); _a.BringToFront(); break;
                    case "mode-b":
                        RequireVisible(_b);
                        _b.SetInputMode(_b.InputMode == UIInputMode.Modal ? UIInputMode.Modeless : UIInputMode.Modal); break;
                    case "veto": _veto = !_veto; _message = "Veto=" + _veto; break;
                    case "user-c": RequireVisible(_c); await _c.RequestCloseAsync(UIUserCloseReason.Button); break;
                    case "outside-c": RequireVisible(_c); await _c.RequestCloseAsync(UIUserCloseReason.OutsidePointer); break;
                    case "force-c": await CloseIfLiveAsync(_c); break;
                    case "1000": RequireInventory(); _inventory.SetCount(1000); break;
                    case "10000": RequireInventory(); _inventory.SetCount(10000); break;
                    case "0": RequireInventory(); _inventory.SetCount(0); break;
                    case "last": RequireInventory(); if (_inventory.Count > 0) _inventory.ScrollToIndex(_inventory.Count - 1); break;
                    case "resize":
                        RequireInventory(); var size = _scroll.viewport.sizeDelta;
                        size.y = size.y < 300 ? 360 : 240; _scroll.viewport.sizeDelta = size; break;
                    case "refresh":
                        RequireInventory();
                        for (int i = 0; i < _rows.Length; ++i) _rows[i] = "Updated item " + i.ToString("D5");
                        _inventory.Refresh(); break;
                    case "loading": _useLoading = !_useLoading; _message = "Loading presentation=" + _useLoading; break;
                    case "manual": _manualContinue = !_manualContinue; _message = "Manual Continue=" + _manualContinue; break;
                    case "area": case "nested": case "remove-nested": case "remove-area":
                        if (_bootstrap.Manager == null || !_bootstrap.Manager.CanProceed) throw new InvalidOperationException("Transition not ready.");
                        bool hub = _bootstrap.Manager.GameScene.path.EndsWith("/UIContextHub.unity", StringComparison.Ordinal);
                        string transition = command == "area" ? (hub ? "area-hub" : "area-main") :
                            command == "remove-area" ? (hub ? "remove-area-hub" : "remove-area-main") : command;
                        bool changed = await _bootstrap.Manager.TryTransitionAsync(transition);
                        _message = transition + " accepted=" + changed; break;
                    case "replace":
                        if (_bootstrap.Manager == null || !_bootstrap.Manager.CanProceed) throw new InvalidOperationException("Transition not ready.");
                        bool fromHub = _bootstrap.Manager.GameScene.path.EndsWith("/UIContextHub.unity", StringComparison.Ordinal);
                        bool accepted = await _bootstrap.Manager.TryTransitionAsync(fromHub ? "to-main" : "to-hub");
                        _message = "Replace accepted=" + accepted; break;
                    default: throw new ArgumentException("Unknown sample command: " + command);
                }
            }
            catch (Exception error) { RecordCommandFailure(error); }
            finally { _busy = false; }
        }

        private void RecordCommandFailure(Exception error)
        {
            _message = error.GetType().Name + ": " + error.Message;
            if (!(error is OperationCanceledException)) Debug.LogException(error, this);
        }

        private static void RequireVisible(UIHandle handle)
        {
            if (handle == null || handle.State != UIState.Visible) throw new InvalidOperationException("Open A/B/C first; target must be Visible.");
        }

        private static UniTask CloseIfLiveAsync(UIHandle handle) => handle == null ? UniTask.CompletedTask : handle.CloseAsync();
        private void RequireInventory()
        {
            if (_inventory == null || _inventoryHandle == null || _inventoryHandle.State != UIState.Visible)
                throw new InvalidOperationException("Inventory is unavailable.");
        }

        private async UniTask OpenInventoryAsync(CancellationToken token)
        {
            if (_inventoryHandle != null && _inventoryHandle.State == UIState.Visible) return;
            _inventoryHandle = await _uiInstaller.Context.OpenAsync(new UIOpenRequest("inventory", hooks: new UIHooks
            {
                PrepareAsync = (handle, lifetime) =>
                {
                    lifetime.ThrowIfCancellationRequested();
                    _inventory = handle.ViewObject.GetComponent<VirtualScrollRect>();
                    _scroll = handle.ViewObject.GetComponentInChildren<ScrollRect>(true);
                    _inventory.Configure(_scroll, _cellPrefab, 24, BindRow, UnbindRow, overscan: 2);
                    _inventory.SetCount(1000);
                    return UniTask.CompletedTask;
                },
                Closed = handle => { _inventory = null; _scroll = null; _visibleRows.Clear(); }
            }), token);
        }

        private void BindRow(VirtualCellBinding binding)
        {
            binding.View.GetComponentInChildren<Text>(true).text = _rows[binding.Index];
            _visibleRows[binding.Index] = binding.Generation;
        }

        private void UnbindRow(VirtualCellBinding binding)
        {
            if (_visibleRows.TryGetValue(binding.Index, out long generation) && generation == binding.Generation)
                _visibleRows.Remove(binding.Index);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextStatus || _status == null) return;
            _nextStatus = Time.unscaledTime + .1f;
            _closedSceneIds.Clear();
            foreach (var pair in _sceneContexts) if (pair.Value.IsDisposed) _closedSceneIds.Add(pair.Key);
            foreach (int id in _closedSceneIds) { _sceneContexts.Remove(id); ++_sceneRetiredCount; }
            string handles = "";
            if (_uiInstaller.Context != null && !_uiInstaller.Context.IsDisposed)
                foreach (var handle in _uiInstaller.Context.Displays)
                    handles += handle.DefinitionId + "#" + handle.Id + " " + handle.State + "/" + handle.InputMode + "/input=" + handle.CanReceiveInput + "  ";
            int first = int.MaxValue, last = -1;
            foreach (int index in _visibleRows.Keys) { first = Math.Min(first, index); last = Math.Max(last, index); }
            string counts = _inventory == null ? "No inventory" :
                "N=" + _inventory.Count + " active/inactive/owned=" + _inventory.CountActive + "/" + _inventory.CountInactive + "/" + _inventory.CountOwned +
                " created/destroyed=" + _inventory.TotalCreated + "/" + _inventory.TotalDestroyed + " range=" + (last < 0 ? "empty" : first + ".." + last);
            _status.text = _message + "\n" + handles + "\n" + counts + "\n" +
                "Fire=" + _gameInputCount + " Player/UI maps=" + (_playerMap != null && _playerMap.enabled) + "/" + (_uiMap != null && _uiMap.enabled) +
                " selected=" + (_eventSystem.currentSelectedGameObject == null ? "none" : _eventSystem.currentSelectedGameObject.name) +
                " transition=" + (_bootstrap.Manager == null ? "none" : _bootstrap.Manager.State.ToString()) +
                " scene UI live/retired=" + _sceneContexts.Count + "/" + _sceneRetiredCount +
                " last scene UI disposed=" + (_retiredSceneContext != null && _retiredSceneContext.IsDisposed);
        }

        /// <inheritdoc />
        public override async UniTask ShowCoverAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _transitionLease ??= _inputInstaller.Input.Layers.AcquireLayer("transition");
            Visible(_cover, true);
            // All Install calls finished before Bootstrap starts; first cover binds before its explicit modal open.
            BindScope();
            if (_transitionShield == null || _transitionShield.State == UIState.Closed)
                _transitionShield = await _uiInstaller.Context.OpenAsync(new UIOpenRequest("transition-shield", hooks: new UIHooks
                {
                    PrepareAsync = (handle, lifetime) =>
                    {
                        lifetime.ThrowIfCancellationRequested();
                        foreach (var button in handle.ViewObject.GetComponentsInChildren<Button>(true)) button.interactable = false;
                        return UniTask.CompletedTask;
                    },
                    CanCloseAsync = (handle, reason, lifetime) =>
                    {
                        lifetime.ThrowIfCancellationRequested();
                        return UniTask.FromResult(false);
                    }
                }), token);
            else _transitionShield.BringToFront();
        }

        /// <inheritdoc />
        public override UniTask ConfigureSceneAsync(Scene scene, ISceneRoot root, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); PublishPreparedInput();
            _retiredSceneContext = _sceneContext;
            // The callback supplies the exact scene root; no scene-wide/global service lookup.
            var installer = root.RootObject.GetComponent<UIContextInstaller>();
            if (installer == null || installer.Context == null) throw new InvalidOperationException("Owned game scene requires its UI installer.");
            _sceneContext = installer.Context;
            _sceneContexts[scene.handle] = _sceneContext;
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public override async UniTask PreparePresentationAsync(Scene scene, ISceneRoot root, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await OpenInventoryAsync(token);
            _transitionShield.BringToFront();
            token.ThrowIfCancellationRequested();
            _sceneContext.CurrentHud.ViewObject.GetComponentInChildren<Text>(true).text = "Scene-owned HUD: " + scene.name;
            _message = "Prepared " + scene.name + "; scene HUD=" + _sceneContext.CurrentHud.DefinitionId;
        }

        /// <inheritdoc />
        public override async UniTask HideCoverAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_loadingActive) { Visible(_cover, false); return; }
            await CloseIfLiveAsync(_transitionShield);
            _transitionShield = null;
            token.ThrowIfCancellationRequested();
            Visible(_cover, false);
            var lease = _transitionLease; _transitionLease = null; lease?.Dispose();
        }

        /// <inheritdoc />
        public override bool UsesLoadingPresentation(SceneLoadingContext context) => _useLoading;
        /// <inheritdoc />
        public override UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_loadingActive) throw new InvalidOperationException("Previous loading UI is still owned.");
            PublishPreparedInput();
            _loadingId = context.OperationId; _loadingActive = true;
            _loadingStatus.text = "Preparing " + context.Target.ScenePath;
            _continue.interactable = false; Visible(_loading, true);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public override UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken token)
        {
            RequireLoading(context); token.ThrowIfCancellationRequested(); Visible(_cover, false);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public override void ReportLoadingProgress(SceneTransitionProgress progress)
        {
            if (_loadingActive && progress.OperationId == _loadingId && _loadingStatus != null)
                _loadingStatus.text = progress.Stage + (progress.StageRatio.HasValue ? " " + Mathf.RoundToInt(progress.StageRatio.Value * 100) + "%" : " (no ratio)");
        }

        /// <inheritdoc />
        public override async UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken token)
        {
            RequireLoading(context); token.ThrowIfCancellationRequested();
            if (!_manualContinue) return;
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, this.GetCancellationTokenOnDestroy(),
                _transitionShield.LifetimeToken, _loading.gameObject.GetCancellationTokenOnDestroy());
            var waitToken = lifetime.Token;
            var completion = new UniTaskCompletionSource();
            bool armed = false;
            UnityEngine.Events.UnityAction listener = () =>
            {
                if (armed && _loadingActive && context.OperationId == _loadingId && !waitToken.IsCancellationRequested)
                { armed = false; _continue.interactable = false; completion.TrySetResult(); }
            };
            var target = _transitionShield.ViewObject;
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();
            var submit = new EventTrigger.Entry { eventID = EventTriggerType.Submit };
            submit.callback.AddListener(data => listener());
            try
            {
                trigger.triggers.Add(submit);
                _continue.onClick.AddListener(listener);
                int releaseFrame = -1;
                while (true)
                {
                    await UniTask.NextFrame(cancellationToken: waitToken);
                    RequireLoading(context);
                    if (!RawControlsReleased()) { releaseFrame = -1; continue; }
                    if (releaseFrame < 0) { releaseFrame = Time.frameCount; continue; }
                    if (Time.frameCount > releaseFrame && _module.enabled && _uiMap.enabled) break;
                }
                armed = true; _continue.interactable = true;
                _loadingStatus.text = "Prepared. Continue with fresh input.";
                // Adapter restores owned focus each frame. Owned native Submit and visible external pointer button share this wait.
                _transitionShield.SetFocus(target);
                await completion.Task.AttachExternalCancellation(waitToken);
            }
            finally
            {
                armed = false;
                if (trigger != null) trigger.triggers.Remove(submit);
                if (waitToken.IsCancellationRequested) completion.TrySetCanceled(waitToken);
                if (_continue != null) { _continue.onClick.RemoveListener(listener); _continue.interactable = false; }
            }
        }

        private bool RawControlsReleased()
        {
            foreach (var action in _uiMap.actions)
            {
                if (action.name == "Point" || action.name == "ScrollWheel" || action.name.StartsWith("TrackedDevice", StringComparison.Ordinal)) continue;
                foreach (var control in action.controls)
                    if (control is ButtonControl button ? button.isPressed : control.EvaluateMagnitude() > 0) return false;
            }
            return true;
        }

        private void RequireLoading(SceneLoadingContext context)
        {
            if (_released || !_loadingActive || _loadingId != context.OperationId || _loading == null || _inputInstaller.Input == null)
                throw new InvalidOperationException("Loading operation owner is unavailable.");
        }

        /// <inheritdoc />
        public override UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context)
        {
            if (_loadingActive && _loadingId == context.OperationId)
            {
                _loadingActive = false;
                if (_continue != null)
                {
                    _continue.interactable = false;
                }
                if (_loading != null)
                {
                    Visible(_loading, false);
                }
            }
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public override void OnFailure(Exception error)
        {
            _message = "Covered failure: " + error.Message;
            if (_cover != null)
            {
                _cover.GetComponentInChildren<Text>(true).text = _message; Visible(_cover, true);
            }
            Debug.LogException(error, this);
        }

        /// <summary>After normal UI.Shutdown, attempts all sample cleanup without disposing borrowed service owners.</summary>
        public void ReleaseScope()
        {
            if (_released) return;
            _released = true;
            var errors = new List<Exception>();
            void Clean(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            var input = _inputInstaller.Input;
            if (input != null && !input.IsDisposed) Clean(() => _shutdownBlock = input.Layers.BlockAll());
            if (_adapter != null) Clean(_adapter.Unbind);
            _bound = false;
            if (_module != null) Clean(() => _module.enabled = false);
            if (_playerMap != null) Clean(() => _playerMap.FindAction("Fire", true).performed -= OnGameplayInput);
            Clean(() => { var lease = _transitionLease; _transitionLease = null; lease?.Dispose(); });
            Clean(() => { var lease = _gameLease; _gameLease = null; lease?.Dispose(); });
            Clean(() => { var lease = _uiLease; _uiLease = null; lease?.Dispose(); });
            if (_module != null) Clean(() =>
            {
                _module.actionsAsset = null;
                _module.move = null; _module.submit = null; _module.cancel = null; _module.point = null;
                _module.leftClick = null; _module.rightClick = null; _module.middleClick = null;
                _module.scrollWheel = null; _module.trackedDevicePosition = null; _module.trackedDeviceOrientation = null;
            });
            foreach (var reference in _references) if (reference != null) Clean(() => Destroy(reference));
            _references.Clear();
            for (int i = 0; i < _commandListeners.Length; ++i)
            {
                int index = i;
                if (_commands[index] != null && _commandListeners[index] != null)
                    Clean(() => _commands[index].onClick.RemoveListener(_commandListeners[index]));
            }
            _commandListeners = Array.Empty<UnityEngine.Events.UnityAction>();
            // Keep this owned block until Input.Release has drained; Uninstall then harmlessly retires it.
            if (errors.Count != 0) throw new AggregateException("Sample cleanup failed after remaining cleanup was attempted.", errors);
        }

        /// <summary>Idempotent partial-install fallback before borrowed input's Uninstall.</summary>
        public void UninstallScope()
        {
            var errors = new List<Exception>();
            try
            {
                ReleaseScope();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            var block = _shutdownBlock;
            _shutdownBlock = null;
            try
            {
                block?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            finally
            {
                _commonRoot = null;
                _loadingActive = false;
            }
            if (errors.Count != 0)
            {
                throw new AggregateException("Sample fallback cleanup failed after remaining cleanup was attempted.", errors);
            }
        }

        private static void Visible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1 : 0; group.blocksRaycasts = visible; group.interactable = visible;
        }
    }
}