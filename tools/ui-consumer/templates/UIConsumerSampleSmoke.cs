using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Input;
using TPLab.Core.Lifecycle;
using TPLab.Core.SceneManagement;
using TPLab.Samples.UI;
using TPLab.UI;
using TPLab.UI.Installation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIConsumer
{
    /// <summary>Consumer-only concrete sample proof activated by the explicit sample environment mode.</summary>
    /// <remarks>
    /// The consumer assembly must reference TPLab.UISamples.OptionalInput. Reflection accesses only this
    /// known fixture. Button invocation and native raycast/focus checks do not prove physical input or UX.
    /// No product API or product lookup is introduced. Run only in the isolated Windows Mono Player.
    /// </remarks>
    public sealed class UIConsumerSampleSmoke : MonoBehaviour
    {
        private const double StepTimeout = 45;
        private static bool _started;
        private readonly List<string> _checks = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private readonly List<InventorySnapshot> _inventorySnapshots = new List<InventorySnapshot>();
        private readonly HashSet<Guid> _continued = new HashSet<Guid>();
        private UIContextSampleController _sample;
        private ISceneRoot _root;
        private BootstrapSystem _bootstrap;
        private UIContextInstaller _uiInstaller;
        private UIContext _context;
        private InputManager _input;
        private EventSystem _events;
        private InputSystemUIInputModule _module;
        private InputActionMap _playerMap;
        private InputActionMap _uiMap;
        private CancellationTokenSource _deadline;
        private Exception _discoveryFailure;
        private string _entry;
        private string _step = "startup";
        private int _shutdownCleanupCount;
        private int _manualContinueCount;
        private bool _shutdownCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToExplicitSample()
        {
            string mode = Environment.GetEnvironmentVariable("TPLAB_UI_RUN_MODE");
            string legacyMode = Environment.GetEnvironmentVariable("TPLAB_UI_MODE");
            if (_started || (mode != "sample" && legacyMode != "sample"))
            {
                return;
            }
            _started = true;
            string entry = SceneManager.GetActiveScene().name;
            UIContextSampleController controller = null;
            Exception failure = null;
            try
            {
                if (entry != "UIContextBootstrapSingle" && entry != "UIContextBootstrapAdditive")
                {
                    throw new InvalidOperationException("One known Bootstrap entry scene is required.");
                }
                foreach (UIContextSampleController candidate in UnityEngine.Object.FindObjectsByType<UIContextSampleController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (controller != null)
                    {
                        throw new InvalidOperationException("Multiple active sample controllers.");
                    }
                    controller = candidate;
                }
                if (controller == null)
                {
                    throw new InvalidOperationException("Explicit active sample controller is absent.");
                }
            }
            catch (Exception error)
            {
                failure = error;
            }
            GameObject owner = controller != null ? controller.gameObject : new GameObject("Sample validation failure");
            var driver = owner.AddComponent<UIConsumerSampleSmoke>();
            driver._sample = controller;
            driver._entry = entry;
            driver._discoveryFailure = failure;
            Application.logMessageReceived += driver.OnLog;
        }

        private void Start()
        {
            RunAsync().Forget(error =>
            {
                Debug.LogException(error);
                Application.Quit(2);
            });
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _errors.Add(message + "\n" + stack);
            }
        }

        private async UniTask RunAsync()
        {
            var report = new SampleReport
            {
                schema = "tplab-ui-sample-player-v1", mode = "sample",
                runId = Environment.GetEnvironmentVariable("TPLAB_UI_RUN_ID"),
                sourceRevision = Environment.GetEnvironmentVariable("TPLAB_UI_SOURCE_REVISION"),
                projectPath = Environment.GetEnvironmentVariable("TPLAB_UI_PROJECT"),
                startedUnix = UnixTime(), unityVersion = Application.unityVersion,
                backend = "Mono2x", target = "StandaloneWindows64", sampleEntry = _entry
            };
            Exception failure = null;
            string failedStep = null;
            string failureState = null;
            _deadline = new CancellationTokenSource();
            using var timer = _deadline.CancelAfterSlim(TimeSpan.FromSeconds(300), DelayType.Realtime);
            try
            {
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                await FramesAsync(2);
                ValidateIdentity(report);
                if (_discoveryFailure != null)
                {
                    throw _discoveryFailure;
                }
                ConnectFixture();
                await UntilAsync(() => _root.IsPrepared && _bootstrap.Manager != null, "common root prepared");
                await WaitReadyAsync("initial Bootstrap", null, null);
                Check(_context.CurrentHud != null && _context.CurrentHud.DefinitionId == "hud-a",
                    "Initial configured HUD is Visible");
                Check(_input.Actions != Read<InputActionAsset>("_sourceActions"), "Input owns a distinct action clone");
                await VerifyDisplaysAsync();
                await VerifyInventoryAsync();
                await VerifyScenesAsync();
                await MeasureGraphicsAsync(report);
                await ShutdownAsync();
                Check(_errors.Count == 0, "No observed product Error Exception Assert logs");
            }
            catch (Exception error)
            {
                failure = error;
                failedStep = _step;
                failureState = SnapshotState();
                Debug.LogException(error, this);
            }
            finally
            {
                if (!_shutdownCompleted && _root != null)
                {
                    try
                    {
                        await ShutdownAsync();
                    }
                    catch (Exception error)
                    {
                        failure = Combine(failure, error);
                        Debug.LogException(error, this);
                    }
                }
                await UniTask.NextFrame();
                Application.logMessageReceived -= OnLog;
                report.success = failure == null && _errors.Count == 0 && _shutdownCompleted;
                report.error = failure?.ToString();
                report.failedStep = report.success ? null : (failedStep ?? _step);
                report.failureState = failureState;
                report.lastState = SnapshotState();
                report.checks = _checks.ToArray();
                report.errors = _errors.ToArray();
                report.inventory = _inventorySnapshots.ToArray();
                report.manualContinueCount = _manualContinueCount;
                report.shutdownCleanupCount = _shutdownCleanupCount;
                report.completedUnix = UnixTime();
                report.graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
                report.width = Screen.width;
                report.height = Screen.height;
                timer.Dispose();
                _deadline.Dispose();
                WriteFreshResult(report);
                Application.Quit(report.success ? 0 : 2);
            }
        }

        private void ValidateIdentity(SampleReport report)
        {
            string mode = Environment.GetEnvironmentVariable("TPLAB_UI_RUN_MODE");
            string legacyMode = Environment.GetEnvironmentVariable("TPLAB_UI_MODE");
            Check((mode == null || mode == "sample") && (legacyMode == null || legacyMode == "sample"),
                "Explicit sample mode without conflicting values");
            Check(!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer && IntPtr.Size == 8,
                "Actual Windows64 Player");
#if !ENABLE_MONO
            throw new InvalidOperationException("Actual Mono runtime is required.");
#endif
            Check(Application.unityVersion == "6000.3.18f1", "Exact Unity version");
            Check(!string.IsNullOrWhiteSpace(report.runId) && IsRevision(report.sourceRevision),
                "Run identity and exact forty-character source revision");
            Check(!string.IsNullOrWhiteSpace(report.projectPath) && Path.IsPathRooted(report.projectPath) &&
                string.Equals(Path.GetFullPath("."), Path.GetFullPath(report.projectPath), StringComparison.OrdinalIgnoreCase),
                "Actual isolated consumer process directory");
            Check(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null &&
                Screen.width == 1280 && Screen.height == 720, "Actual graphics device and 1280x720");
        }

        private void ConnectFixture()
        {
            foreach (MonoBehaviour component in _sample.GetComponents<MonoBehaviour>())
            {
                if (component is ISceneRoot candidate)
                {
                    if (_root != null)
                    {
                        throw new InvalidOperationException("Multiple roots on sample owner.");
                    }
                    _root = candidate;
                }
            }
            Check(_root != null && _root.RootObject == _sample.gameObject && _root.IsReady,
                "Explicit common root is installed");
            Check(_entry == "UIContextBootstrapSingle" ? _root is SingletonSceneRoot : _root is SceneOwnedRoot,
                "Chosen Single or Additive root type");
            _bootstrap = Read<BootstrapSystem>("_bootstrap");
            _uiInstaller = Read<UIContextInstaller>("_uiInstaller");
            _context = _uiInstaller.Context;
            _input = Read<InputManagerInstaller>("_inputInstaller").Input;
            _events = Read<EventSystem>("_eventSystem");
            _module = Read<InputSystemUIInputModule>("_module");
            _playerMap = Read<InputActionMap>("_playerMap");
            _uiMap = Read<InputActionMap>("_uiMap");
            Check(_context != null && _input != null && _events != null && _module != null &&
                _context.RootObject == _root.RootObject && _context.EventSystem == _events,
                "Context connects explicit borrowed native and input owners");
        }

        private async UniTask VerifyDisplaysAsync()
        {
            UIHandle first = _context.CurrentHud;
            await CommandAsync("hud-b");
            UIHandle second = _context.CurrentHud;
            Check(second != first && second.DefinitionId == "hud-b" && second.State == UIState.Visible &&
                first.State == UIState.Closed && first.ViewObject == null, "HUD replacement retires old generation");
            await CommandAsync("hud-a");
            Check(_context.CurrentHud.DefinitionId == "hud-a" && second.State == UIState.Closed,
                "Reusable HUD opens a fresh display generation");
            await CommandAsync("abc");
            UIHandle a = Read<UIHandle>("_a");
            UIHandle b = Read<UIHandle>("_b");
            UIHandle c = Read<UIHandle>("_c");
            await FramesAsync(3);
            GameObject selected = _events.currentSelectedGameObject;
            Check(a.State == UIState.Visible && b.State == UIState.Visible && c.State == UIState.Visible &&
                !a.CanReceiveInput && b.CanReceiveInput && c.CanReceiveInput &&
                !_playerMap.enabled && _uiMap.enabled, "A B C modal boundary blocks lower UI and gameplay");
            Check(a.ViewObject.transform.parent.parent == b.ViewObject.transform.parent.parent &&
                b.ViewObject.transform.parent.parent == c.ViewObject.transform.parent.parent &&
                a.ViewObject.transform.parent.GetSiblingIndex() < b.ViewObject.transform.parent.GetSiblingIndex() &&
                b.ViewObject.transform.parent.GetSiblingIndex() < c.ViewObject.transform.parent.GetSiblingIndex(),
                "Actual owned roots follow A B C front order");
            Check(IsOwnedTarget(c, selected) && HasRaycast(c, new Vector2(.5f, .28f)) &&
                !HasRaycast(a, new Vector2(.1f, .5f)), "Native focus and raycast obey modal eligibility");
            await CommandAsync("close-b");
            await UntilAsync(() => _playerMap.enabled && _module.enabled, "modal retirement");
            Check(b.State == UIState.Closed && b.ViewObject == null && a.CanReceiveInput && c.CanReceiveInput &&
                c.State == UIState.Visible && _events.currentSelectedGameObject == selected &&
                HasRaycast(a, new Vector2(.1f, .5f)), "Middle B close preserves C focus and restores lower input");
            await CommandAsync("child");
            UIHandle child = Read<UIHandle>("_child");
            Check(child.Parent == a && child.State == UIState.Visible, "C child has explicit logical parent");
            await CommandAsync("close-parent");
            Check(a.State == UIState.Closed && child.State == UIState.Closed &&
                a.ViewObject == null && child.ViewObject == null && c.State == UIState.Visible && c.CanReceiveInput,
                "Parent cascade closes child and preserves independent C");
            Check(Read<bool>("_veto"), "Approval starts vetoed");
            await CommandAsync("user-c");
            Check(c.State == UIState.Visible && Read<string>("_message").Contains("vetoed"), "Veto keeps C Visible");
            await CommandAsync("veto");
            await CommandAsync("user-c");
            Check(c.State == UIState.Closed && c.ViewObject == null &&
                Read<string>("_message").Contains("accepted"), "Accepted user close retires C");
            await CommandAsync("veto");
        }

        private async UniTask VerifyInventoryAsync()
        {
            await CommandAsync("1000");
            CaptureInventory("1000", 1000);
            VirtualScrollRect inventory = Read<VirtualScrollRect>("_inventory");
            int created = inventory.TotalCreated;
            await CommandAsync("10000");
            CaptureInventory("10000", 10000);
            Check(inventory.TotalCreated == created, "Ten thousand rows do not create ten thousand cells");
            await CommandAsync("last");
            await FramesAsync(2);
            Dictionary<int, long> rows = Read<Dictionary<int, long>>("_visibleRows");
            Check(rows.ContainsKey(9999), "Last-index jump binds final data row");
            CaptureInventory("last", 10000);
            await CommandAsync("0");
            Check(inventory.CountActive == 0 && rows.Count == 0, "Count zero unbinds visible rows");
            CaptureInventory("0", 0);
            await CommandAsync("1000");
            CaptureInventory("restore1000", 1000);
            await CommandAsync("resize");
            CaptureInventory("resize360", 1000);
            await CommandAsync("resize");
            CaptureInventory("resize240", 1000);
            await CommandAsync("refresh");
            Check(rows.Count == inventory.CountActive && rows.Count > 0, "Refresh republishes prepared data rows");
        }

        private void CaptureInventory(string label, int count)
        {
            VirtualScrollRect inventory = Read<VirtualScrollRect>("_inventory");
            ScrollRect scroll = Read<ScrollRect>("_scroll");
            Dictionary<int, long> rows = Read<Dictionary<int, long>>("_visibleRows");
            int budget = Mathf.CeilToInt(scroll.viewport.rect.height / 24f) + 1 + 2 * 2;
            int first = int.MaxValue;
            int last = -1;
            foreach (int index in rows.Keys)
            {
                first = Math.Min(first, index);
                last = Math.Max(last, index);
                if (index < 0 || index >= count)
                {
                    throw new InvalidOperationException("Binding index outside current Count.");
                }
            }
            Check(inventory.Count == count && inventory.CountOwned <= budget &&
                inventory.CountOwned == inventory.CountActive + inventory.CountInactive &&
                rows.Count == inventory.CountActive && scroll.content.childCount == inventory.CountActive &&
                (count == 0 || inventory.CountActive > 0),
                "Bounded actual cell ownership " + label);
            _inventorySnapshots.Add(new InventorySnapshot
            {
                phase = label, logicalCount = count, active = inventory.CountActive, inactive = inventory.CountInactive,
                owned = inventory.CountOwned, budget = budget, created = inventory.TotalCreated,
                destroyed = inventory.TotalDestroyed, viewportHeight = scroll.viewport.rect.height,
                first = last < 0 ? -1 : first, last = last
            });
        }

        private async UniTask VerifyScenesAsync()
        {
            GameObject common = _root.RootObject;
            UIHandle inventory = Read<UIHandle>("_inventoryHandle");
            await TransitionAsync("area", true, true);
            Scene area = RegisteredScene("UIContextArea");
            UIContext areaContext = ContextFor(area);
            UIHandle areaHud = areaContext.CurrentHud;
            await TransitionAsync("nested", true, true);
            Scene nested = RegisteredScene("UIContextNested");
            UIContext nestedContext = ContextFor(nested);
            UIHandle nestedHud = nestedContext.CurrentHud;
            Check(!areaContext.IsDisposed && !nestedContext.IsDisposed && areaContext != nestedContext &&
                areaHud.State == UIState.Visible && nestedHud.State == UIState.Visible,
                "Area Nested own distinct live scene UI contexts");
            await TransitionAsync("remove-area", false, false);
            Check(areaContext.IsDisposed && nestedContext.IsDisposed &&
                areaHud.State == UIState.Closed && nestedHud.State == UIState.Closed &&
                !area.isLoaded && !nested.isLoaded && _context == _uiInstaller.Context && !_context.IsDisposed &&
                _root.RootObject == common && inventory.State == UIState.Visible,
                "Area removal retires Nested subtree and preserves common UI");
            Scene previous = _bootstrap.Manager.GameScene;
            UIContext previousContext = ContextFor(previous);
            UIHandle previousHud = previousContext.CurrentHud;
            await TransitionAsync("replace", true, true);
            Check(_bootstrap.Manager.GameScene.handle != previous.handle && previousContext.IsDisposed &&
                previousHud.State == UIState.Closed && _context == _uiInstaller.Context &&
                _root.RootObject == common && inventory.State == UIState.Visible && _root.IsPrepared,
                "Primary replacement retires scene UI and retains same common root");
            await CommandAsync("loading");
            Check(!Read<bool>("_useLoading"), "Cover-only project policy selected");
            await TransitionAsync("replace", false, false);
            await CommandAsync("loading");
            await CommandAsync("manual");
            Check(Read<bool>("_useLoading") && !Read<bool>("_manualContinue"), "Automatic loading policy selected");
            await TransitionAsync("replace", true, false);
            Check(_playerMap.enabled && _uiMap.enabled && _module.enabled && _bootstrap.Manager.CanProceed,
                "Completed transitions release their own blocks");
        }

        private async UniTask TransitionAsync(string command, bool expectLoading, bool expectManual)
        {
            _step = "transition " + command;
            InvokeCommand(command);
            await WaitReadyAsync(_step, expectLoading, expectManual);
            CheckCommandOutcome(command);
            Check(Read<string>("_message").Contains("accepted=True"), "Scene command accepted " + command);
        }

        private async UniTask WaitReadyAsync(string label, bool? expectLoading, bool? expectManual)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + StepTimeout;
            bool loadingSeen = false;
            bool coverSeen = false;
            int continuedBefore = _manualContinueCount;
            while (Read<bool>("_busy") || _bootstrap.Manager == null || !_bootstrap.Manager.CanProceed)
            {
                CheckWait(deadline, label);
                CheckManagerFailure();
                bool loadingActive = Read<bool>("_loadingActive");
                CanvasGroup cover = Read<CanvasGroup>("_cover");
                CanvasGroup loading = Read<CanvasGroup>("_loading");
                coverSeen |= cover != null && cover.alpha > 0;
                loadingSeen |= loadingActive && loading != null && loading.alpha > 0;
                Button proceed = Read<Button>("_continue");
                if (loadingActive && proceed != null && proceed.interactable)
                {
                    Guid operation = Read<Guid>("_loadingId");
                    if (_continued.Contains(operation))
                    {
                        throw new InvalidOperationException("Same operation stayed armed after Continue.");
                    }
                    UIHandle shield = Read<UIHandle>("_transitionShield");
                    Check(operation != Guid.Empty && loading != null && loading.alpha > 0 && loading.blocksRaycasts &&
                        !_playerMap.enabled && _uiMap.enabled && _module.enabled && shield != null &&
                        shield.State == UIState.Visible && shield.InputMode == UIInputMode.Modal &&
                        Read<IDisposable>("_transitionLease") != null,
                        "Manual Continue current operation keeps gameplay blocked " + label);
                    _continued.Add(operation);
                    ++_manualContinueCount;
                    proceed.onClick.Invoke();
                }
                await FramesAsync(1);
            }
            await UntilAsync(() => _playerMap.enabled && _uiMap.enabled && _module.enabled, label + " input recovery");
            Check(!_context.IsDisposed && _context.Fault == null && _root.IsPrepared, "Common owner Ready " + label);
            if (expectLoading.HasValue && expectManual.HasValue)
            {
                Check(loadingSeen == expectLoading.Value &&
                    (_manualContinueCount > continuedBefore) == expectManual.Value &&
                    (expectLoading.Value || coverSeen), "Observed route " + label +
                    " loading=" + loadingSeen + " manual=" + (_manualContinueCount > continuedBefore) + " cover=" + coverSeen);
            }
        }

        private Scene RegisteredScene(string name)
        {
            foreach (SceneRegistration registration in _bootstrap.Manager.RegisteredScenes)
            {
                if (registration.Scene.name == name)
                {
                    return registration.Scene;
                }
            }
            throw new InvalidOperationException("Registered scene absent: " + name);
        }

        private UIContext ContextFor(Scene scene)
        {
            if (!Read<Dictionary<int, UIContext>>("_sceneContexts").TryGetValue(scene.handle, out UIContext context))
            {
                throw new InvalidOperationException("Exact scene-instance context absent: " + scene.name);
            }
            return context;
        }

        private async UniTask CommandAsync(string command)
        {
            _step = "command " + command;
            InvokeCommand(command);
            await UntilAsync(() => !Read<bool>("_busy"), _step);
            await FramesAsync(2);
            CheckCommandOutcome(command);
        }

        private void InvokeCommand(string command)
        {
            if (Read<bool>("_busy") || Read<bool>("_released"))
            {
                throw new InvalidOperationException("Sample command busy/released: " + command);
            }
            string[] ids = Read<string[]>("_commandIds");
            Button[] buttons = Read<Button[]>("_commands");
            if (ids.Length != buttons.Length)
            {
                throw new InvalidOperationException("Command arrays differ.");
            }
            for (int index = 0; index < ids.Length; ++index)
            {
                if (ids[index] == command)
                {
                    if (buttons[index] == null || !buttons[index].isActiveAndEnabled || !buttons[index].interactable)
                    {
                        throw new InvalidOperationException("Project command button unavailable: " + command);
                    }
                    buttons[index].onClick.Invoke();
                    return;
                }
            }
            throw new InvalidOperationException("Project command not found: " + command);
        }

        private void CheckCommandOutcome(string command)
        {
            CheckManagerFailure();
            string message = Read<string>("_message") ?? "";
            if (_errors.Count != 0 || _context.Fault != null ||
                message.StartsWith("OperationCanceledException:", StringComparison.Ordinal) ||
                message.StartsWith("Covered failure:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Command failed " + command + ": " + SnapshotState());
            }
        }

        private void CheckManagerFailure()
        {
            if (_bootstrap.Manager != null && _bootstrap.Manager.LastFailure != null)
            {
                throw new InvalidOperationException("Manager failed: " + SnapshotState(), _bootstrap.Manager.LastFailure);
            }
        }

        private bool HasRaycast(UIHandle handle, Vector2 relative)
        {
            RectTransform rect = (RectTransform)handle.ViewObject.transform;
            Vector3 local = new Vector3(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, relative.x),
                Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, relative.y), 0);
            var pointer = new PointerEventData(_events)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local))
            };
            var hits = new List<RaycastResult>();
            _events.RaycastAll(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (IsOwnedTarget(handle, hit.gameObject))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsOwnedTarget(UIHandle handle, GameObject target)
        {
            return target != null && handle.ViewObject != null &&
                (target == handle.ViewObject || target.transform.IsChildOf(handle.ViewObject.transform));
        }

        private async UniTask MeasureGraphicsAsync(SampleReport report)
        {
            _step = "rendered frames";
            string value = Environment.GetEnvironmentVariable("TPLAB_UI_SAMPLE_FRAMES");
            int frames = string.IsNullOrEmpty(value) ? 30 : int.Parse(value);
            Check(frames == 30 || frames == 600, "Explicit graphics window is 30 or 600 frames");
            var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", frames + 12,
                ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
            try
            {
                Check(recorder.Valid, "Native Draw Calls Count recorder available");
                await FramesAsync(3);
                await UniTask.WaitForEndOfFrame(cancellationToken: _deadline.Token);
                int previousCount = recorder.Count;
                int previousFrame = Time.frameCount;
                report.unityFrames = new int[frames];
                report.recorderCounts = new int[frames];
                report.rawDrawCalls = new long[frames];
                long minimum = long.MaxValue;
                for (int index = 0; index < frames; ++index)
                {
                    await FramesAsync(1);
                    await UniTask.WaitForEndOfFrame(cancellationToken: _deadline.Token);
                    int count = recorder.Count;
                    int frame = Time.frameCount;
                    long draws = recorder.LastValue;
                    if (!recorder.Valid || count != previousCount + 1 || count >= recorder.Capacity - 1 ||
                        frame != previousFrame + 1 || draws <= 0)
                    {
                        throw new InvalidOperationException("Missing/stale/discontinuous/nonpositive draw at " + index +
                            ": count " + previousCount + "->" + count + ", frame " + previousFrame + "->" + frame + ", draws=" + draws);
                    }
                    report.unityFrames[index] = frame;
                    report.recorderCounts[index] = count;
                    report.rawDrawCalls[index] = draws;
                    minimum = Math.Min(minimum, draws);
                    previousCount = count;
                    previousFrame = frame;
                    ++report.actualFrames;
                }
                report.drawCalls = minimum;
                Check(report.actualFrames == frames && minimum > 0, "Every measured frame has fresh positive draws");
            }
            finally
            {
                recorder.Dispose();
            }
        }

        private async UniTask ShutdownAsync()
        {
            _step = "ordered owner shutdown";
            if (_shutdownCompleted)
            {
                return;
            }
            Exception failure = null;
            var sources = new List<GameObject>();
            InputActionAsset inputSource = null;
            UIHandle[] displays = Array.Empty<UIHandle>();
            if (_sample != null && _context != null && !_context.IsDisposed)
            {
                inputSource = Read<InputActionAsset>("_sourceActions");
                foreach (UIDefinition definition in Read<UIContextSettings>("_settings").CreateSnapshot())
                {
                    sources.Add(definition.Prefab);
                }
                displays = new List<UIHandle>(_context.Displays).ToArray();
                foreach (UIHandle handle in displays)
                {
                    handle.RegisterCleanup(() =>
                    {
                        ++_shutdownCleanupCount;
                        if (_input.IsDisposed)
                        {
                            throw new InvalidOperationException("Input ended before common UI cleanup.");
                        }
                    });
                }
            }
            try
            {
                if (_bootstrap != null)
                {
                    await AwaitOwnedAsync(_bootstrap.ShutdownAsync(), "game scene shutdown");
                }
            }
            catch (Exception error)
            {
                failure = error;
            }
            try
            {
                await AwaitOwnedAsync(_root.ShutdownAsync(), "common root shutdown");
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            await UniTask.NextFrame();
            if (failure != null)
            {
                throw failure;
            }
            Check(_context != null && _context.IsDisposed && _input != null && _input.IsDisposed &&
                !_root.IsReady && !_root.IsPrepared && _root.RootObject != null && _shutdownCleanupCount > 0,
                "Same common root retires UI before Input and preserves borrowed root");
            foreach (UIHandle handle in displays)
            {
                Check(handle.State == UIState.Closed && handle.ViewObject == null &&
                    handle.LifetimeToken.IsCancellationRequested, "Shutdown retires display " + handle.DefinitionId);
            }
            Check(inputSource != null && inputSource.FindActionMap("Player", true).FindAction("Fire", true) != null,
                "Borrowed imported input source survives shutdown");
            foreach (GameObject source in sources)
            {
                Check(source != null && !source.activeSelf,
                    "Borrowed prefab remains alive inactive " + (source == null ? "missing" : source.name));
            }
            _shutdownCompleted = true;
        }

        private async UniTask AwaitOwnedAsync(UniTask operation, string name)
        {
            using var timeout = new CancellationTokenSource();
            using var timer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(StepTimeout), DelayType.Realtime);
            try
            {
                await operation.AttachExternalCancellation(timeout.Token);
            }
            catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException("Timeout awaiting " + name + ": " + SnapshotState(), error);
            }
        }

        private async UniTask UntilAsync(Func<bool> ready, string name)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + StepTimeout;
            while (!ready())
            {
                CheckWait(deadline, name);
                await FramesAsync(1);
            }
        }

        private void CheckWait(double deadline, string name)
        {
            _deadline.Token.ThrowIfCancellationRequested();
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                throw new TimeoutException("Timeout at " + name + ": " + SnapshotState());
            }
            if (_errors.Count != 0)
            {
                throw new InvalidOperationException("Product error at " + name + ": " + SnapshotState());
            }
        }

        private async UniTask FramesAsync(int count)
        {
            for (int index = 0; index < count; ++index)
            {
                await UniTask.NextFrame(cancellationToken: _deadline.Token);
            }
        }

        private T Read<T>(string name)
        {
            FieldInfo field = typeof(UIContextSampleController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || _sample == null)
            {
                throw new InvalidOperationException("Known fixture field unavailable: " + name);
            }
            return (T)field.GetValue(_sample);
        }

        private string SnapshotState()
        {
            if (_sample == null)
            {
                return "No sample controller; entry=" + _entry;
            }
            try
            {
                return "entry=" + _entry + " step=" + _step + " message=" + Read<string>("_message") +
                    " busy=" + Read<bool>("_busy") + " released=" + Read<bool>("_released") +
                    " rootPrepared=" + (_root != null && _root.IsPrepared) +
                    " manager=" + (_bootstrap?.Manager == null ? "none" : _bootstrap.Manager.State.ToString()) +
                    " loading=" + Read<bool>("_loadingActive") + "/" + Read<Guid>("_loadingId") +
                    " Player/UI=" + (_playerMap != null && _playerMap.enabled) + "/" + (_uiMap != null && _uiMap.enabled) +
                    " ContextDisposed=" + (_context != null && _context.IsDisposed);
            }
            catch (Exception error)
            {
                return "State snapshot failed: " + error;
            }
        }

        private void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Sample check failed: " + name + "; " + SnapshotState());
            }
            _checks.Add(name);
        }

        private static bool IsRevision(string value)
        {
            if (value == null || value.Length != 40)
            {
                return false;
            }
            foreach (char character in value)
            {
                if (!Uri.IsHexDigit(character))
                {
                    return false;
                }
            }
            return true;
        }

        private static double UnixTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        }

        private static Exception Combine(Exception first, Exception next)
        {
            return first == null ? next : new AggregateException(first, next);
        }

        private static void WriteFreshResult(SampleReport report)
        {
            string path = Environment.GetEnvironmentVariable("TPLAB_UI_PLAYER_RESULT");
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                !Directory.Exists(Path.GetDirectoryName(path)))
            {
                throw new InvalidOperationException("Fresh absolute result path with existing parent required.");
            }
            byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(JsonUtility.ToJson(report, true));
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            stream.Write(bytes, 0, bytes.Length);
        }

        [Serializable]
        private sealed class InventorySnapshot
        {
            public string phase;
            public int logicalCount, active, inactive, owned, budget, created, destroyed, first, last;
            public float viewportHeight;
        }

        [Serializable]
        private sealed class SampleReport
        {
            public string schema, mode, runId, sourceRevision, projectPath, unityVersion, backend, target;
            public string sampleEntry, graphicsDeviceType, error, failedStep, failureState, lastState;
            public double startedUnix, completedUnix;
            public bool success, physicalInputVerified, visualAcceptanceVerified;
            public int width, height, actualFrames, manualContinueCount, shutdownCleanupCount;
            public long drawCalls;
            public int[] unityFrames, recorderCounts;
            public long[] rawDrawCalls;
            public string[] checks, errors;
            public InventorySnapshot[] inventory;
        }
    }
}
