using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.UI;
using TPLab.UI.Benchmark;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if TPLAB_UI_INPUT
using TPLab.Core.Input;
using TPLab.UI.InputSystem;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
#endif

namespace UIConsumer
{
    /// <summary>Consumer-owned runtime proof; no global lookup or original-project editor mutation.</summary>
    public sealed class UIConsumerSmoke : MonoBehaviour
    {
        private readonly List<string> _checks = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private UIContext _context;
        private SceneOwnedRoot _root;
        private GameObject _owner, _canvas, _source, _events;
        private BenchmarkCapture _capture;
#if TPLAB_UI_INPUT
        private InputManager _input;
        private InputActionAsset _actions;
        private UIInputSystemAdapter _adapter;
        private InputSystemUIInputModule _module;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private readonly List<InputActionReference> _references = new List<InputActionReference>();
        private IDisposable _gameLease, _uiLease, _transition;
        private InputActionMap _uiMap, _gameMap;
#endif
        private void Start() => RunAsync().Forget(Debug.LogException);
        private void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) _errors.Add(condition + "\n" + stack);
        }
        private void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("Consumer check failed: " + name);
            _checks.Add(name);
        }
        private async UniTask RunAsync()
        {
            var result = new Result
            {
                runId = Environment.GetEnvironmentVariable("TPLAB_UI_RUN_ID"),
                sourceRevision = Environment.GetEnvironmentVariable("TPLAB_UI_SOURCE_REVISION"),
                projectPath = Environment.GetEnvironmentVariable("TPLAB_UI_PROJECT"),
                startedUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                unityVersion = Application.unityVersion, backend = "Mono2x", target = "StandaloneWindows64"
            };
            Application.logMessageReceived += OnLog;
            Exception failure = null;
            try
            {
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                await UniTask.NextFrame();
                await UniTask.NextFrame();
#if TPLAB_UI_INPUT
                result.includeInputCompiled = true;
#else
                result.includeInputCompiled = false;
#endif
                Check(result.includeInputCompiled == (Environment.GetEnvironmentVariable("TPLAB_UI_INCLUDE_INPUT") == "1"), "Actual compiled source profile matches selection");
                string mode = Environment.GetEnvironmentVariable("TPLAB_UI_MODE");
                Check(!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer && IntPtr.Size == 8, "Windows64 graphics Player");
#if !ENABLE_MONO
                throw new InvalidOperationException("Actual Mono runtime required.");
#endif
                Check(Path.GetFullPath(".") == result.projectPath, "Actual isolated consumer process directory");
                Check(Application.unityVersion == "6000.3.18f1", "Exact Unity version");
                Check(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null && Screen.width == 1280 && Screen.height == 720, "Actual device and resolution");
                if (mode == "scroll")
                {
                    var benchmark = gameObject.AddComponent<UIVirtualScrollBenchmark>();
                    await benchmark.RunAllAsync(this.GetCancellationTokenOnDestroy());
                }
                else if (mode == "canvas")
                {
                    var benchmark = gameObject.AddComponent<UICanvasBenchmark>();
                    await benchmark.RunAllAsync(this.GetCancellationTokenOnDestroy());
                }
                else if (mode == "smoke")
                {
                    await VerifyLifecycleAsync();
                    await VerifyVirtualAsync();
#if TPLAB_UI_INPUT
                    await VerifyNativeAsync();
#endif
                    await MeasureAsync(result);
                }
                else throw new InvalidOperationException("Explicit smoke/scroll/canvas mode required.");
                Check(_errors.Count == 0, "No product error/exception logs");
            }
            catch (Exception error) { failure = error; }
            finally
            {
                _capture?.Stop();
                try { if (_context != null) await _context.ShutdownAsync(); }
                catch (Exception error) { failure = Combine(failure, error); }
#if TPLAB_UI_INPUT
                try
                {
                    _transition?.Dispose(); _gameLease?.Dispose(); _uiLease?.Dispose();
                    if (_input != null) await _input.ShutdownAsync();
                }
                catch (Exception error) { failure = Combine(failure, error); }
                foreach (InputActionReference reference in _references) if (reference != null) Destroy(reference);
                if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
                if (_mouse != null) InputSystem.RemoveDevice(_mouse);
                if (_actions != null) Destroy(_actions);
#endif
                try { if (_root != null) await _root.ShutdownAsync(); }
                catch (Exception error) { failure = Combine(failure, error); }
                if (failure == null && _owner != null && (_root == null || _root.IsReady)) failure = new InvalidOperationException("Core root shutdown was not observed.");
                foreach (GameObject owned in new[] { _events, _source, _canvas, _owner }) if (owned != null) Destroy(owned);
                await UniTask.NextFrame();
                Application.logMessageReceived -= OnLog;
                result.success = failure == null && _errors.Count == 0;
                result.error = failure?.ToString(); result.checks = _checks.ToArray(); result.errors = _errors.ToArray();
                result.completedUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
                result.graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
                result.width = Screen.width; result.height = Screen.height;
                result.environment = BenchmarkCapture.Environment(result.sourceRevision, result.backend);
                string path = Environment.GetEnvironmentVariable("TPLAB_UI_PLAYER_RESULT");
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || File.Exists(path)) throw new InvalidOperationException("Fresh absolute Player result required.");
                File.WriteAllText(path, JsonUtility.ToJson(result, true));
                if (failure != null) Debug.LogException(failure);
                Application.Quit(result.success ? 0 : 2);
            }
        }
        private static Exception Combine(Exception first, Exception next) => first == null ? next : new AggregateException(first, next);

        private async UniTask VerifyLifecycleAsync()
        {
            _owner = new GameObject("Consumer SceneRoot"); _owner.SetActive(false);
            _root = _owner.AddComponent<SceneOwnedRoot>();
            _root.Configure(Array.Empty<SceneRootInstaller>());
            _owner.SetActive(true); await _root.PrepareAsync();
            Check(_root.IsReady && _root.IsPrepared, "SceneRoot installed and prepared");
            _canvas = new GameObject("Borrowed UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _source = new GameObject("Borrowed view source", typeof(RectTransform), typeof(Image), typeof(ConsumerNativeProbe));
            var rect = (RectTransform)_source.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            _source.GetComponent<Image>().color = new Color(.12f, .28f, .4f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(rect, false);
            var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = "Source-only UI consumer"; text.fontSize = 32; text.color = Color.white;
            _source.SetActive(false);
            _events = new GameObject("Explicit borrowed EventSystem"); _events.SetActive(false);
            var system = _events.AddComponent<EventSystem>();
#if TPLAB_UI_INPUT
            ConfigureNative(system);
#endif
            _events.SetActive(true);
            int loads = 0;
            _context = new UIContext(_owner, (key, token) => { ++loads; return UniTask.FromResult(_source); },
#if TPLAB_UI_INPUT
                () => _adapter.AcquireModalBlock("modal"),
#else
                null,
#endif
                system);
            _context.RegisterHost("consumer", _canvas.transform);
            _context.Register(new UIDefinition("hud-a", _source, role: UIRole.Hud, hostId: "consumer"));
            _context.Register(new UIDefinition("hud-b", assetKey: "borrowed-source", role: UIRole.Hud, hostId: "consumer"));
            _context.Register(new UIDefinition("popup", _source, hostId: "consumer", retention: UIRetention.Reuse));
            _context.Register(new UIDefinition("modal", _source, hostId: "consumer", inputMode: UIInputMode.Modal));
#if TPLAB_UI_INPUT
            _adapter.Bind(_context, _input, _module, _uiMap.id);
#endif
            var first = await _context.SelectHudAsync(new UIOpenRequest("hud-a"));
            var popup = await _context.OpenAsync(new UIOpenRequest("popup", first));
            bool cleanup = false; popup.RegisterCleanup(() => cleanup = true);
            await _context.SelectHudAsync(new UIOpenRequest("hud-b"));
            Check(first.State == UIState.Closed && popup.State == UIState.Closed && cleanup && loads == 1, "HUD replacement closes child; keyed borrowed provider");
            bool veto = true;
            var hooks = new UIHooks { CanCloseAsync = (handle, reason, token) => UniTask.FromResult(!veto) };
            popup = await _context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            Check(!await popup.RequestCloseAsync(UIUserCloseReason.Button) && popup.State == UIState.Visible, "User veto preserves display");
            var clone = popup.ViewObject; await popup.CloseAsync();
            popup = await _context.OpenAsync(new UIOpenRequest("popup"));
            Check(popup.ViewObject == clone && !popup.LifetimeToken.IsCancellationRequested, "Reuse retains clone with a new generation");
            popup.BringToFront(); popup.SetInputMode(UIInputMode.Modal); popup.SetInputMode(UIInputMode.Modeless);
            await popup.CloseAsync();
            Check(_source != null && _canvas != null && _events != null && _root.IsReady, "UI borrows native sources and services");
        }

        private async UniTask VerifyVirtualAsync()
        {
            var holder = new GameObject("Consumer inventory", typeof(RectTransform)); holder.transform.SetParent(_canvas.transform, false);
            var viewport = holder.AddComponent<RectMask2D>(); var scroll = holder.AddComponent<ScrollRect>();
            var rect = (RectTransform)holder.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(rect, false);
            var contentRect = (RectTransform)content.transform; Top(contentRect);
            var prefab = new GameObject("Cell source", typeof(RectTransform), typeof(Image)); prefab.SetActive(false); Top((RectTransform)prefab.transform);
            scroll.viewport = rect; scroll.content = contentRect; scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var virtualScroll = holder.AddComponent<VirtualScrollRect>();
            int binds = 0, unbinds = 0;
            virtualScroll.Configure(scroll, (RectTransform)prefab.transform, 20, binding => { ++binds; if (!binding.IsCurrent) throw new InvalidOperationException("Stale binding."); }, binding => ++unbinds);
            try
            {
                Canvas.ForceUpdateCanvases(); virtualScroll.SetCount(1000); await UniTask.NextFrame();
                Check(virtualScroll.Count == 1000 && virtualScroll.CountOwned > 0 && virtualScroll.CountOwned <= 42, "Virtual1000 bounded ownership");
                virtualScroll.SetCount(10000); virtualScroll.ScrollToIndex(9999); await UniTask.NextFrame();
                Check(virtualScroll.Count == 10000 && virtualScroll.CountOwned <= 42, "Virtual10000 native scrolling with bounded cells");
                virtualScroll.SetCount(0); await UniTask.NextFrame();
                Check(virtualScroll.Count == 0 && virtualScroll.CountActive == 0 && virtualScroll.CountInactive == virtualScroll.CountOwned && virtualScroll.CountOwned <= 42 && unbinds == binds, "Count zero releases bindings and retains only budgeted inactive cells");
            }
            finally { Destroy(holder); Destroy(prefab); }
            await UniTask.NextFrame();
        }
        private static void Top(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
        }
        private async UniTask MeasureAsync(Result result)
        {
            _capture = new BenchmarkCapture(); _capture.Start(800);
            try
            {
                for (int frame = 0; frame < 720; ++frame)
                {
                    await UniTask.NextFrame(); await UniTask.WaitForEndOfFrame();
                    _capture.Sample(frame < 120 ? 0 : 1);
                }
                _capture.Stop();
                result.actualFrames = 600;
                result.counters = _capture.BuildReports(new[] { "warmup", "measure" }, new[] { 120, 600 });
                foreach (var counter in result.counters)
                    if (counter.name == "Draw Calls Count")
                    {
                        var phase = counter.phases[1];
                        Check(phase.status == "Measured" && phase.freshSamples == 600 && phase.graphicsEligibleFrames == 600, "Fresh complete draw recorder coverage");
                        long minimum = long.MaxValue; foreach (long value in phase.rawValues) minimum = Math.Min(minimum, value);
                        result.drawCalls = minimum; Check(minimum > 0, "600 frames actually rendered nonzero draws");
                    }
                Check(result.drawCalls > 0, "Draw marker exists");
            }
            finally { _capture.Stop(); }
        }
#if TPLAB_UI_INPUT
        private void ConfigureNative(EventSystem system)
        {
            _keyboard = InputSystem.AddDevice<Keyboard>("UIConsumerKeyboard");
            _mouse = InputSystem.AddDevice<Mouse>("UIConsumerMouse");
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var game = new InputActionMap("Game"); game.AddAction("Fire", InputActionType.Button, "<Keyboard>/space"); _actions.AddActionMap(game);
            var ui = new InputActionMap("UI");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
            ui.AddAction("Right", InputActionType.PassThrough, "<Mouse>/rightButton", expectedControlLayout: "Button");
            ui.AddAction("Middle", InputActionType.PassThrough, "<Mouse>/middleButton", expectedControlLayout: "Button");
            ui.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
            ui.AddAction("Move", InputActionType.PassThrough, "<Gamepad>/leftStick", expectedControlLayout: "Vector2");
            ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter"); ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            ui.AddAction("Position", InputActionType.PassThrough, expectedControlLayout: "Vector3");
            ui.AddAction("Orientation", InputActionType.PassThrough, expectedControlLayout: "Quaternion");
            _actions.AddActionMap(ui); _input = new InputManager(_actions);
            _input.Actions.devices = new InputDevice[] { _keyboard, _mouse };
            _gameMap = _input.Actions.FindActionMap("Game"); _uiMap = _input.Actions.FindActionMap("UI");
            _input.Layers.RegisterLayer("game", new[] { _gameMap.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("ui", new[] { _uiMap.id }, 2000, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
            _input.Layers.RegisterLayer("transition", Array.Empty<Guid>(), 1000, InputLayerMode.BlockLower);
            _gameLease = _input.Layers.AcquireLayer("game"); _uiLease = _input.Layers.AcquireLayer("ui");
            _module = system.gameObject.AddComponent<InputSystemUIInputModule>(); _module.enabled = false;
            _module.actionsAsset = _input.Actions;
            _module.point = Ref("Point"); _module.leftClick = Ref("Click"); _module.rightClick = Ref("Right"); _module.middleClick = Ref("Middle");
            _module.scrollWheel = Ref("Scroll"); _module.move = Ref("Move"); _module.submit = Ref("Submit"); _module.cancel = Ref("Cancel");
            _module.trackedDevicePosition = Ref("Position"); _module.trackedDeviceOrientation = Ref("Orientation");
            _adapter = gameObject.AddComponent<UIInputSystemAdapter>();
        }
        private InputActionReference Ref(string name)
        {
            var reference = InputActionReference.Create(_uiMap.FindAction(name)); _references.Add(reference); return reference;
        }
        private async UniTask ReleaseControlsAsync()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            await UniTask.NextFrame(); await UniTask.NextFrame(); await UniTask.NextFrame();
        }
        private async UniTask VerifyNativeAsync()
        {
            await ReleaseControlsAsync();
            Check(_gameMap.enabled && _uiMap.enabled && _module.enabled, "Optional native module uses runtime clone policy");
            int cancelRequests = 0;
            var popup = await _context.OpenAsync(new UIOpenRequest("modal", hooks: new UIHooks
            {
                PrepareAsync = (handle, token) => { handle.SetFocus(handle.ViewObject); return UniTask.CompletedTask; },
                CanCloseAsync = (handle, reason, token) => { ++cancelRequests; return UniTask.FromResult(true); }
            }));
            Check(!_gameMap.enabled && _uiMap.enabled, "Native modal blocks gameplay and retains UI map");
            _transition = _input.Layers.AcquireLayer("transition");
            await popup.CloseAsync(); await ReleaseControlsAsync();
            Check(!_gameMap.enabled && _uiMap.enabled, "Independent transition lease survives modal close");
            _transition.Dispose(); _transition = null; await ReleaseControlsAsync();
            Check(_gameMap.enabled, "Only project transition release restores gameplay");
            popup = await _context.OpenAsync(new UIOpenRequest("modal", hooks: new UIHooks
            {
                PrepareAsync = (handle, token) => { handle.SetFocus(handle.ViewObject); return UniTask.CompletedTask; },
                CanCloseAsync = (handle, reason, token) => { ++cancelRequests; return UniTask.FromResult(true); }
            }));
            await ReleaseControlsAsync();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            for (int i = 0; i < 6; ++i) await UniTask.NextFrame();
            Check(cancelRequests == 1 && popup.State == UIState.Closed && !_gameMap.enabled, "Real native Cancel closes once; held press retains quarantine");
            await ReleaseControlsAsync(); Check(_gameMap.enabled, "Raw release plus subsequent EventSystem frame restores game");
            UIHandle lower = _context.CurrentHud;
            lower.SetFocus(lower.ViewObject);
            var lowerProbe = lower.ViewObject.GetComponent<ConsumerNativeProbe>();
            int previousSubmits = lowerProbe.SubmitCount;
            using (var block = _input.Layers.BlockAll())
            {
                _module.enabled = true;
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Enter));
                await UniTask.NextFrame(); await UniTask.NextFrame();
                Check(!_module.enabled && !_uiMap.enabled && !_gameMap.enabled && lowerProbe.SubmitCount == previousSubmits, "External native enable cannot bypass BlockAll or leak Submit");
            }
            await ReleaseControlsAsync();
            await _context.ShutdownAsync();
            Check(!_input.IsDisposed && _events != null && _actions != null && !_module.enabled, "UI owner shutdown leaves borrowed Input/EventSystem intact");
            // A new modeless context keeps a visible graphic for the separate 600-frame render proof.
            _context = new UIContext(_owner); _context.RegisterHost("consumer", _canvas.transform);
            _context.Register(new UIDefinition("final", _source, hostId: "consumer"));
            await _context.OpenAsync(new UIOpenRequest("final"));
        }
#endif
        [Serializable] private sealed class Result
        {
            public bool success, includeInputCompiled;
            public string runId, sourceRevision, projectPath, unityVersion, backend, target, graphicsDeviceType, error;
            public double startedUnix, completedUnix;
            public int actualFrames, width, height;
            public long drawCalls;
            public string[] checks, errors;
            public BenchmarkCounterReport[] counters;
            public BenchmarkEnvironment environment;
        }
    }
    public sealed class ConsumerNativeProbe : MonoBehaviour, ISubmitHandler
    {
        public int SubmitCount { get; private set; }
        public void OnSubmit(BaseEventData eventData) => ++SubmitCount;
    }
}
