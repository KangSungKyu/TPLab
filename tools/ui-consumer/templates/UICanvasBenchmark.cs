using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI.Benchmark
{
    /// <summary>Controlled Canvas comparison using real UIContext lifecycles and borrowed fixed hosts.</summary>
    /// <remarks>All three display scopes are Modeless. This fixture makes no inter-context modal/focus/input claim.</remarks>
    public sealed class UICanvasBenchmark : MonoBehaviour
    {
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 600;
        private const int StaticRows = 48;
        private const int DynamicRows = 16;
        private const int PopupRows = 12;
        private const int PopupCount = 3;
        private static readonly string[] PopupIds = { "Popup0", "Popup1", "Popup2" };
        [SerializeField] private bool runWhenEnabled;
        [SerializeField] private string outputPath;
        [SerializeField] private string sourceRevision;
        [SerializeField] private string playerBackend;
        private CancellationTokenSource _run;
        private readonly BenchmarkCapture _capture = new BenchmarkCapture();
        private readonly ArmReport[] _reports = new ArmReport[3];
        private readonly UIHandle[] _popups = new UIHandle[PopupCount];
        private readonly UniTask[] _closeTasks = new UniTask[PopupCount];
        private readonly bool[] _pendingClose = new bool[PopupCount];
        private readonly GameObject[] _popupSources = new GameObject[PopupCount];
        private readonly GameObject[] _cachedPopupViews = new GameObject[PopupCount];
        private readonly Text[][] _popupTexts = new Text[PopupCount][];
        private readonly Image[] _popupPanels = new Image[PopupCount];
        private readonly UIOpenRequest[] _popupRequests = new UIOpenRequest[PopupCount];
        private GameObject _staticSource, _dynamicSource, _fixtureRoot;
        private UIContext _staticContext, _dynamicContext, _popupContext;
        private Text[] _staticTexts, _dynamicTexts;
        private string[] _captions;
        private Color[] _colors;
        private int _canvasCount;

        private void Start()
        {
            if (runWhenEnabled)
            {
                _run = new CancellationTokenSource();
                RunAllAsync(_run.Token).Forget(Debug.LogException);
            }
        }

        private void OnDisable()
        {
            _run?.Cancel();
        }

        private async UniTask RunAllAsync(CancellationToken token)
        {
            try
            {
                PrepareSourcesAndData();
                for (int arm = 0; arm < 3; ++arm) await RunArmAsync(arm, token);
                var report = new CanvasReport
                {
                    status = "EXPLORATORY_MEASUREMENT",
                    environment = BenchmarkCapture.Environment(sourceRevision, playerBackend),
                    warmupFrames = WarmupFrames, measuredFrames = MeasuredFrames,
                    hudGraphicCount = 2 * (StaticRows + DynamicRows), popupGraphicCountEach = 1 + 2 * PopupRows,
                    contextScope = "Same three Modeless display scopes in all arms; inter-context modal/focus/input is untested.",
                    schedule = "120-frame cycle: open P0@0,P1@20,P2@40; close P2@80,P1@90,P0@100. Dynamic HUD every frame; static HUD every60; visible popup text/color every15.",
                    operationCostScope = "Startup/close endpoints are settling observations; complete setup/resize/close costs are unmeasured.",
                    arms = _reports
                };
                BenchmarkCapture.Write(outputPath, "TPLAB_UI_BENCHMARK_OUTPUT", report);
            }
            finally
            {
                _capture.Stop();
                await ShutdownFixtureAsync();
                if (_staticSource != null) Destroy(_staticSource);
                if (_dynamicSource != null) Destroy(_dynamicSource);
                foreach (GameObject source in _popupSources) if (source != null) Destroy(source);
                _run?.Dispose();
                _run = null;
            }
        }

        private void PrepareSourcesAndData()
        {
            _captions = new string[120];
            _colors = new Color[120];
            for (int frame = 0; frame < _captions.Length; ++frame)
            {
                _captions[frame] = "Prepared UI " + frame.ToString("D3");
                _colors[frame] = new Color(0.1f + frame / 1200f, 0.2f, 0.3f, 0.35f);
            }
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font.RequestCharactersInTexture("Prepared UI0123456789", 14, FontStyle.Normal);
            _staticSource = CreateSource("Static HUD Source", StaticRows, 0, false, font);
            _dynamicSource = CreateSource("Dynamic HUD Source", DynamicRows, 420, false, font);
            for (int i = 0; i < PopupCount; ++i)
            {
                _popupSources[i] = CreateSource(PopupIds[i] + " Source", PopupRows, 180 + 20 * i, true, font);
                _popupRequests[i] = new UIOpenRequest(PopupIds[i], inputMode: UIInputMode.Modeless);
            }
        }

        private async UniTask RunArmAsync(int arm, CancellationToken token)
        {
            var visiblePopups = new int[MeasuredFrames];
            var visibleGraphics = new int[MeasuredFrames];
            int scheduleMismatches = 0;
            int setupStartedFrame = Time.frameCount;
            try
            {
                BuildHosts(arm);
                _staticContext.Register(new UIDefinition("StaticHud", _staticSource, role: UIRole.Hud,
                    hostId: "Static", inputMode: UIInputMode.Modeless, retention: UIRetention.Reuse));
                _dynamicContext.Register(new UIDefinition("DynamicHud", _dynamicSource, role: UIRole.Hud,
                    hostId: "Dynamic", inputMode: UIInputMode.Modeless, retention: UIRetention.Reuse));
                UIHandle staticHud = await _staticContext.SelectHudAsync(new UIOpenRequest("StaticHud"), token);
                UIHandle dynamicHud = await _dynamicContext.SelectHudAsync(new UIOpenRequest("DynamicHud"), token);
                _staticTexts = staticHud.ViewObject.GetComponentsInChildren<Text>(true);
                _dynamicTexts = dynamicHud.ViewObject.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < PopupCount; ++i)
                {
                    _popupContext.Register(new UIDefinition(PopupIds[i], _popupSources[i], hostId: PopupIds[i],
                        inputMode: UIInputMode.Modeless, retention: UIRetention.Reuse));
                    UIHandle popup = await _popupContext.OpenAsync(_popupRequests[i], token);
                    _popups[i] = popup;
                    _cachedPopupViews[i] = popup.ViewObject;
                    _popupTexts[i] = popup.ViewObject.GetComponentsInChildren<Text>(true);
                    _popupPanels[i] = popup.ViewObject.GetComponent<Image>();
                }
                for (int i = PopupCount - 1; i >= 0; --i) { await _popups[i].CloseAsync(); _popups[i] = null; }
                Canvas.ForceUpdateCanvases();
                int setupObservedFrames = Time.frameCount - setupStartedFrame;
                _capture.Start(800);
                await UniTask.NextFrame(cancellationToken: token);
                await UniTask.WaitForEndOfFrame(token);
                _capture.Sample(0);
                int measuredStart = 0;
                for (int frame = 0; frame < WarmupFrames + MeasuredFrames; ++frame)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    int cycle = frame % 120;
                    PollCloseTasks();
                    ApplySchedule(cycle);
                    await UniTask.WaitForEndOfFrame(token);
                    _capture.Sample(frame < WarmupFrames ? 1 : 2);
                    if (frame >= WarmupFrames)
                    {
                        if (frame == WarmupFrames) measuredStart = Time.frameCount;
                        int sample = frame - WarmupFrames;
                        int visible = 0;
                        for (int i = 0; i < PopupCount; ++i)
                            if (_popups[i] != null && _popups[i].State == UIState.Visible
                                && _popups[i].ViewObject != null && _popups[i].ViewObject.activeInHierarchy) ++visible;
                        visiblePopups[sample] = visible;
                        visibleGraphics[sample] = 2 * (StaticRows + DynamicRows) + visible * (1 + 2 * PopupRows);
                        int expected = (cycle < 100 ? 1 : 0) + (cycle >= 20 && cycle < 90 ? 1 : 0)
                            + (cycle >= 40 && cycle < 80 ? 1 : 0);
                        if (visible != expected) ++scheduleMismatches;
                    }
                }
                int measuredSpan = Time.frameCount - measuredStart + 1;
                PollCloseTasks();
                // Actual graph inspection is outside the steady measurement loop.
                int actualEndpointGraphics = _fixtureRoot.GetComponentsInChildren<Graphic>().Length;
                await ShutdownContextsAsync(); // Await actual UI ownership cleanup before destroying borrowed hosts.
                await UniTask.NextFrame(cancellationToken: token);
                await UniTask.WaitForEndOfFrame(token);
                _capture.Sample(3);
                _capture.Stop();
                BenchmarkCounterReport[] counters = _capture.BuildReports(
                    new[] { "StartupEndpoint", "Warmup", "MeasuredSchedule", "CloseEndpoint" },
                    new[] { 1, WarmupFrames, MeasuredFrames, 1 });
                _reports[arm] = new ArmReport
                {
                    name = arm == 0 ? "SharedCanvas" : arm == 1 ? "FrequencySeparatedCanvases" : "PopupPerCanvas",
                    canvasCount = _canvasCount, setupObservedFrames = setupObservedFrames,
                    measuredFrameSpan = measuredSpan, scheduleMismatchFrames = scheduleMismatches,
                    scheduleGate = scheduleMismatches == 0 && measuredSpan == MeasuredFrames ? "Controlled schedule matched" : "Partial: frame or visibility schedule mismatch",
                    actualEndpointGraphicCount = actualEndpointGraphics,
                    endpointScope = "After measured cycle while popups are closed; this is not all-frame Graphic coverage proof",
                    visiblePopupCounts = visiblePopups, expectedGraphicCountsFromVisibleDisplays = visibleGraphics,
                    counters = counters, rendering = BenchmarkCapture.RenderEvidence(counters, 2)
                };
            }
            finally
            {
                _capture.Stop();
                await ShutdownFixtureAsync();
            }
            await UniTask.NextFrame(cancellationToken: token);
        }

        private void BuildHosts(int arm)
        {
            _fixtureRoot = new GameObject("P7 Canvas arrangement fixture");
            _canvasCount = 0;
            _staticContext = new UIContext(CreateOwner("Static HUD owner"));
            _dynamicContext = new UIContext(CreateOwner("Dynamic HUD owner"));
            _popupContext = new UIContext(CreateOwner("Popup owner"));
            Canvas hudCanvas = CreateCanvas("Static/shared HUD Canvas", 0);
            Canvas dynamicCanvas = arm == 1 ? CreateCanvas("Frequently updated HUD Canvas", 10) : hudCanvas;
            _staticContext.RegisterHost("Static", CreateHost(hudCanvas, "Static host"));
            _dynamicContext.RegisterHost("Dynamic", CreateHost(dynamicCanvas, "Dynamic host"));
            Canvas sharedPopupCanvas = arm == 1 ? CreateCanvas("Popup update Canvas", 20) : hudCanvas;
            for (int i = 0; i < PopupCount; ++i)
            {
                Canvas canvas = arm == 2 ? CreateCanvas(PopupIds[i] + " Canvas", 100 + i) : sharedPopupCanvas;
                _popupContext.RegisterHost(PopupIds[i], CreateHost(canvas, PopupIds[i] + " host"));
                _popups[i] = null;
                _pendingClose[i] = false;
                _closeTasks[i] = default;
            }
            // Fixed borrowed hosts express the same P0 < P1 < P2 order. Owned clone/wrapper ordering stays in UIContext.
        }

        private GameObject CreateOwner(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_fixtureRoot.transform, false);
            return root;
        }

        private Canvas CreateCanvas(string name, int order)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(_fixtureRoot.transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            ++_canvasCount;
            return canvas;
        }

        private static RectTransform CreateHost(Canvas canvas, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            RectTransform host = (RectTransform)root.transform;
            host.SetParent(canvas.transform, false);
            UIVirtualScrollBenchmark.Stretch(host);
            return host;
        }

        private GameObject CreateSource(string name, int rows, float top, bool popup, Font font)
        {
            var source = new GameObject(name, typeof(RectTransform));
            UIVirtualScrollBenchmark.Stretch((RectTransform)source.transform);
            if (popup)
            {
                Image panel = source.AddComponent<Image>();
                panel.color = _colors[0];
                panel.material = Graphic.defaultGraphicMaterial;
                panel.raycastTarget = false;
            }
            for (int i = 0; i < rows; ++i)
            {
                var row = new GameObject("Prepared row", typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)row.transform;
                rect.SetParent(source.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(140, 20);
                rect.anchoredPosition = new Vector2(20 + i % 8 * 150, -top - i / 8 * 25);
                Image image = row.GetComponent<Image>();
                image.color = new Color(0.18f, 0.18f, 0.18f, 1);
                image.material = Graphic.defaultGraphicMaterial;
                image.raycastTarget = false;
                var label = new GameObject("Prepared label", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(row.transform, false);
                UIVirtualScrollBenchmark.Stretch((RectTransform)label.transform);
                Text text = label.GetComponent<Text>();
                text.font = font;
                text.fontSize = 14;
                text.text = _captions[i % _captions.Length];
                text.material = Graphic.defaultGraphicMaterial;
                text.raycastTarget = false;
            }
            // Sources contain no Canvas/overrideSorting. Managed wrappers are created by UIContext, never the fixture.
            source.SetActive(false);
            return source;
        }

        private void ApplySchedule(int cycle)
        {
            for (int i = 0; i < _dynamicTexts.Length; ++i) _dynamicTexts[i].text = _captions[cycle];
            if (cycle % 60 == 0) for (int i = 0; i < _staticTexts.Length; ++i) _staticTexts[i].text = _captions[cycle];
            if (cycle == 0) Open(0);
            if (cycle == 20) Open(1);
            if (cycle == 40) Open(2);
            if (cycle == 80) Close(2);
            if (cycle == 90) Close(1);
            if (cycle == 100) Close(0);
            if (cycle % 15 == 0)
            {
                for (int popup = 0; popup < PopupCount; ++popup)
                {
                    if (_popups[popup] == null || _popups[popup].State != UIState.Visible) continue;
                    _popupPanels[popup].color = _colors[cycle];
                    for (int i = 0; i < _popupTexts[popup].Length; ++i) _popupTexts[popup][i].text = _captions[cycle];
                }
            }
        }

        private void Open(int index)
        {
            if (_pendingClose[index]) throw new InvalidOperationException("Draft schedule reached a still-closing popup.");
            UIHandle popup = _popupContext.BeginOpen(_popupRequests[index]);
            _popups[index] = popup;
            if (popup.ViewObject != _cachedPopupViews[index])
                throw new InvalidOperationException("Reuse fixture unexpectedly replaced its prewarmed clone.");
        }

        private void Close(int index)
        {
            _closeTasks[index] = _popups[index].CloseAsync();
            _pendingClose[index] = true;
            _popups[index] = null;
        }

        private void PollCloseTasks()
        {
            for (int i = 0; i < PopupCount; ++i)
            {
                if (!_pendingClose[i] || _closeTasks[i].Status == UniTaskStatus.Pending) continue;
                _closeTasks[i].GetAwaiter().GetResult(); // Observe errors; no unobserved fire-and-forget close operation.
                _pendingClose[i] = false;
            }
        }

        private async UniTask ShutdownContextsAsync()
        {
            var errors = new List<Exception>();
            foreach (UIContext context in new[] { _popupContext, _dynamicContext, _staticContext })
            {
                if (context == null) continue;
                try { await context.ShutdownAsync(); }
                catch (Exception error) { errors.Add(error); }
            }
            _popupContext = _dynamicContext = _staticContext = null;
            if (errors.Count != 0) throw new AggregateException(errors);
        }

        private async UniTask ShutdownFixtureAsync()
        {
            try { await ShutdownContextsAsync(); }
            finally
            {
                if (_fixtureRoot != null) Destroy(_fixtureRoot);
                _fixtureRoot = null;
            }
        }

        [Serializable] private sealed class ArmReport
        {
            public string name, scheduleGate, endpointScope, rendering;
            public int canvasCount, setupObservedFrames, measuredFrameSpan, scheduleMismatchFrames, actualEndpointGraphicCount;
            public int[] visiblePopupCounts, expectedGraphicCountsFromVisibleDisplays;
            public BenchmarkCounterReport[] counters;
        }
        [Serializable] private sealed class CanvasReport
        {
            public string status, contextScope, schedule, operationCostScope;
            public BenchmarkEnvironment environment;
            public int warmupFrames, measuredFrames, hudGraphicCount, popupGraphicCountEach;
            public ArmReport[] arms;
        }
    }
}