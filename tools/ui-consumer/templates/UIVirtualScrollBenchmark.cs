using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI.Benchmark
{
    /// <summary>Controlled graphics Player fixture; actual results are evidence, not a generalized performance claim.</summary>
    public sealed class UIVirtualScrollBenchmark : MonoBehaviour
    {
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 600;
        private const float Width = 1280;
        private const float Height = 720;
        private const float RowHeight = 20;
        private const int Overscan = 2;
        [SerializeField] private bool runWhenEnabled;
        [SerializeField] private bool reverseComparisonOrder;
        [SerializeField] private string outputPath;
        [SerializeField] private string sourceRevision;
        [SerializeField] private string playerBackend;
        private CancellationTokenSource _run;
        private readonly BenchmarkCapture _capture = new BenchmarkCapture();
        private readonly ScenarioReport[] _reports = new ScenarioReport[3];
        private string[] _data;
        private RectTransform _prefab;
        private GameObject _root;
        private ScrollRect _scroll;
        private VirtualScrollRect _virtual;
        private RectTransform[] _nativeViews;
        private readonly RectTransform[] _boundViews = new RectTransform[128];
        private readonly int[] _boundIndices = new int[128];
        private int _nativeCreated;
        private int _nativeDestroyed;
        private int _count;

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
                _data = new string[10000];
                for (int i = 0; i < _data.Length; ++i) _data[i] = i.ToString("D5");
                _prefab = CreateCellPrefab();
                for (int slot = 0; slot < 3; ++slot)
                {
                    int arm = slot < 2 && reverseComparisonOrder ? 1 - slot : slot;
                    await RunScenarioAsync(arm, token);
                }
                var report = new ScrollReport
                {
                    status = "EXPLORATORY_MEASUREMENT",
                    environment = BenchmarkCapture.Environment(sourceRevision, playerBackend),
                    viewportWidth = Width, viewportHeight = Height, rowHeight = RowHeight,
                    spacing = 0, overscan = Overscan, warmupFrames = WarmupFrames, measuredFrames = MeasuredFrames,
                    comparisonOrder = reverseComparisonOrder ? "Virtual1000,Native1000,Virtual10000" : "Native1000,Virtual1000,Virtual10000",
                    operationCostScope = "Startup/resize/close are endpoint settle samples; total operation cost is unmeasured.",
                    scenarios = _reports
                };
                BenchmarkCapture.Write(outputPath, "TPLAB_UI_BENCHMARK_OUTPUT", report);
            }
            finally
            {
                _capture.Stop();
                DestroyFixture();
                if (_prefab != null) Destroy(_prefab.gameObject);
                _run?.Dispose();
                _run = null;
            }
        }

        private async UniTask RunScenarioAsync(int arm, CancellationToken token)
        {
            int count = arm == 2 ? 10000 : 1000;
            var cells = new CellSeries
            {
                active = new int[MeasuredFrames], inactive = new int[MeasuredFrames], owned = new int[MeasuredFrames],
                created = new int[MeasuredFrames], destroyed = new int[MeasuredFrames]
            };
            _nativeCreated = 0;
            _nativeDestroyed = 0;
            for (int i = 0; i < _boundIndices.Length; ++i) { _boundIndices[i] = -1; _boundViews[i] = null; }
            try
            {
                _capture.Start(800);
                BuildFixture(arm, count);
                await SettleAsync(0, token);
                SetCount(count / 2);
                await SettleAsync(1, token);
                SetCount(count);
                await SettleAsync(1, token);
                for (int frame = 0; frame < WarmupFrames + MeasuredFrames; ++frame)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    SetScrollPath(frame % WarmupFrames);
                    await UniTask.WaitForEndOfFrame(token);
                    _capture.Sample(frame < WarmupFrames ? 2 : 3);
                    if (frame >= WarmupFrames)
                    {
                        int sample = frame - WarmupFrames;
                        cells.active[sample] = _virtual == null ? _count : _virtual.CountActive;
                        cells.inactive[sample] = _virtual == null ? 0 : _virtual.CountInactive;
                        cells.owned[sample] = _virtual == null ? _count : _virtual.CountOwned;
                        cells.created[sample] = _virtual == null ? _nativeCreated : _virtual.TotalCreated;
                        cells.destroyed[sample] = _virtual == null ? _nativeDestroyed : _virtual.TotalDestroyed;
                    }
                }
                // Geometry inspection is outside the measured loop and is explicitly one endpoint snapshot.
                GeometrySnapshot geometry = InspectGeometry();
                DestroyFixture();
                await SettleAsync(4, token);
                _capture.Stop();
                BenchmarkCounterReport[] counters = _capture.BuildReports(
                    new[] { "StartupEndpoint", "ResizeEndpoint", "Warmup", "MeasuredScroll", "CloseEndpoint" },
                    new[] { 1, 2, WarmupFrames, MeasuredFrames, 1 });
                _reports[arm] = new ScenarioReport
                {
                    name = arm == 0 ? "Native1000" : arm == 1 ? "Virtual1000" : "Virtual10000",
                    comparisonGroup = arm == 2 ? "Separate scalability result" : "Identical N=1000 fixture comparison",
                    logicalCount = count, cells = cells, geometry = geometry, counters = counters,
                    rendering = BenchmarkCapture.RenderEvidence(counters, 3)
                };
            }
            finally
            {
                _capture.Stop();
                DestroyFixture();
            }
            await UniTask.NextFrame(cancellationToken: token);
        }

        private async UniTask SettleAsync(int phase, CancellationToken token)
        {
            await UniTask.NextFrame(cancellationToken: token);
            await UniTask.WaitForEndOfFrame(token);
            _capture.Sample(phase);
        }

        private void BuildFixture(int arm, int count)
        {
            _root = new GameObject("P7 Scroll Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            var scrollObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            RectTransform scrollRect = (RectTransform)scrollObject.transform;
            scrollRect.SetParent(_root.transform, false);
            scrollRect.sizeDelta = new Vector2(Width, Height);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(scrollRect, false);
            Stretch(viewport);
            var contentObject = new GameObject("Content", typeof(RectTransform));
            RectTransform content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one;
            content.localRotation = Quaternion.identity;
            _scroll = scrollObject.GetComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.inertia = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            if (arm != 0)
            {
                _virtual = scrollObject.AddComponent<VirtualScrollRect>();
                _virtual.Configure(_scroll, _prefab, RowHeight, Bind, Unbind, overscan: Overscan, spacing: 0);
            }
            SetCount(count);
        }

        private static RectTransform CreateCellPrefab()
        {
            var cell = new GameObject("Shared Text Image Row Source", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)cell.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, RowHeight);
            rect.anchoredPosition = Vector2.zero;
            cell.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1);
            cell.GetComponent<Image>().material = Graphic.defaultGraphicMaterial;
            cell.GetComponent<Image>().raycastTarget = false;
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(cell.transform, false);
            Stretch((RectTransform)textObject.transform);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font.RequestCharactersInTexture("0123456789", 14, FontStyle.Normal);
            text.fontSize = 14;
            text.material = Graphic.defaultGraphicMaterial;
            text.raycastTarget = false;
            cell.SetActive(false);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void SetCount(int count)
        {
            _count = count;
            if (_virtual != null)
            {
                _virtual.SetCount(count); // SetCount reconciles; Refresh is reserved for explicit data changes.
                return;
            }
            if (_nativeViews != null)
            {
                foreach (RectTransform view in _nativeViews) { Destroy(view.gameObject); ++_nativeDestroyed; }
            }
            _nativeViews = new RectTransform[count];
            _scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, count * RowHeight);
            for (int i = 0; i < count; ++i)
            {
                RectTransform view = Instantiate(_prefab, _scroll.content, false);
                view.anchoredPosition = new Vector2(0, -i * RowHeight);
                view.GetComponentInChildren<Text>(true).text = _data[i];
                view.gameObject.SetActive(true);
                _nativeViews[i] = view;
                ++_nativeCreated;
            }
        }

        private void Bind(VirtualCellBinding binding)
        {
            binding.View.GetComponentInChildren<Text>(true).text = _data[binding.Index];
            for (int slot = 0; slot < _boundViews.Length; ++slot)
            {
                if (_boundViews[slot] == binding.View || _boundViews[slot] == null)
                {
                    _boundViews[slot] = binding.View;
                    _boundIndices[slot] = binding.Index;
                    return;
                }
            }
            throw new InvalidOperationException("Draft binding bookkeeping exceeded the fixed fixture's 128 slots.");
        }

        private void Unbind(VirtualCellBinding binding)
        {
            for (int slot = 0; slot < _boundViews.Length; ++slot)
                if (_boundViews[slot] == binding.View) { _boundIndices[slot] = -1; return; }
        }

        private void SetScrollPath(int frame)
        {
            float fraction = frame / 119f;
            _scroll.verticalNormalizedPosition = fraction <= 0.5f ? 1 - 2 * fraction : 2 * fraction - 1;
            // Native onValueChanged/LateUpdate reconciles P5. Calling Refresh here would replace unchanged generations.
        }

        private GeometrySnapshot InspectGeometry()
        {
            var unique = new HashSet<int>();
            int duplicates = 0;
            if (_virtual == null)
            {
                for (int i = 0; i < _nativeViews.Length; ++i)
                    if (_nativeViews[i] != null && _nativeViews[i].gameObject.activeInHierarchy) unique.Add(i);
            }
            else
            {
                for (int slot = 0; slot < _boundViews.Length; ++slot)
                    if (_boundIndices[slot] >= 0 && _boundViews[slot] != null && _boundViews[slot].gameObject.activeInHierarchy
                        && !unique.Add(_boundIndices[slot])) ++duplicates;
            }
            float top = Mathf.Clamp(_scroll.content.anchoredPosition.y, 0, Mathf.Max(0, _count * RowHeight - Height));
            int first = Mathf.Clamp(Mathf.FloorToInt(top / RowHeight), 0, _count - 1);
            int last = Mathf.Clamp(Mathf.CeilToInt((top + Height) / RowHeight) - 1, first, _count - 1);
            int missing = 0;
            for (int i = first; i <= last; ++i) if (!unique.Contains(i)) ++missing;
            return new GeometrySnapshot
            {
                scope = "One after-scroll/before-close endpoint; not per-frame traversal proof",
                firstVisible = first, lastVisible = last, uniqueBoundIndices = unique.Count,
                duplicateIndices = duplicates, missingVisibleRows = missing,
                currentViewportDemandCeiling = Mathf.CeilToInt(Height / RowHeight) + 2 * Overscan + 2
            };
        }

        private void DestroyFixture()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            _scroll = null;
            _virtual = null;
            _nativeViews = null;
        }

        [Serializable] private sealed class CellSeries
        {
            public int[] active, inactive, owned, created, destroyed;
        }
        [Serializable] private struct GeometrySnapshot
        {
            public string scope;
            public int firstVisible, lastVisible, uniqueBoundIndices, duplicateIndices, missingVisibleRows, currentViewportDemandCeiling;
        }
        [Serializable] private sealed class ScenarioReport
        {
            public string name, comparisonGroup, rendering;
            public int logicalCount;
            public CellSeries cells;
            public GeometrySnapshot geometry;
            public BenchmarkCounterReport[] counters;
        }
        [Serializable] private sealed class ScrollReport
        {
            public string status, comparisonOrder, operationCostScope;
            public BenchmarkEnvironment environment;
            public float viewportWidth, viewportHeight, rowHeight, spacing;
            public int overscan, warmupFrames, measuredFrames;
            public ScenarioReport[] scenarios;
        }
    }

    // Draft-only shared collector: the two controlled fixtures need the same recorder/freshness rules.
    internal sealed class BenchmarkCapture
    {
        private static readonly string[] Names =
        {
            "Main Thread", "GC Allocated In Frame", "Total Used Memory", "GC Used Memory", "Layout",
            "Canvas.BuildBatch", "UGUI.Rendering.UpdateBatches", "Batches Count", "Draw Calls Count", "Vertices Count"
        };
        private ProfilerRecorder[] _recorders;
        private Buffer[] _buffers;
        private int[] _phases;
        private int[] _unityFrames;
        private bool[] _graphicsEligible;
        private int _frames;

        internal void Start(int capacity)
        {
            Stop();
            _recorders = new ProfilerRecorder[Names.Length];
            _buffers = new Buffer[Names.Length];
            _phases = new int[capacity];
            _unityFrames = new int[capacity];
            _graphicsEligible = new bool[capacity];
            _frames = 0;
            for (int i = 0; i < Names.Length; ++i) _buffers[i] = new Buffer(Names[i], capacity);
            var handles = new List<ProfilerRecorderHandle>(256);
            try { ProfilerRecorderHandle.GetAvailable(handles); }
            catch (Exception error)
            {
                foreach (Buffer buffer in _buffers) buffer.reason = "Recorder discovery failed; N/A: " + error.GetType().Name;
                return;
            }
            for (int i = 0; i < Names.Length; ++i)
            {
                Buffer buffer = _buffers[i];
                ProfilerRecorderDescription? match = null;
                foreach (ProfilerRecorderHandle handle in handles)
                {
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Name != Names[i]) continue;
                    if (match.HasValue) { buffer.reason = "Ambiguous marker name; N/A."; match = null; break; }
                    match = description;
                }
                if (!match.HasValue) { buffer.reason = buffer.reason ?? "Marker unavailable; N/A."; continue; }
                buffer.category = match.Value.Category.ToString();
                buffer.unit = match.Value.UnitType.ToString();
                try
                {
                    ProfilerRecorder recorder = ProfilerRecorder.StartNew(match.Value.Category, match.Value.Name, capacity,
                        ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                    if (!recorder.Valid) { recorder.Dispose(); buffer.reason = "Recorder invalid; N/A."; continue; }
                    _recorders[i] = recorder;
                    buffer.lastCount = recorder.Count;
                    buffer.available = true;
                }
                catch (Exception error) { buffer.reason = "Recorder start failed: " + error.GetType().Name; }
            }
        }

        internal void Sample(int phase)
        {
            if (_frames >= _phases.Length) throw new InvalidOperationException("Draft sample buffer capacity exceeded.");
            int frame = _frames++;
            _phases[frame] = phase;
            _unityFrames[frame] = Time.frameCount;
#if ENABLE_MONO
            _graphicsEligible[frame] = !Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer
                && IntPtr.Size == 8 && Screen.width == 1280 && Screen.height == 720
                && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
#else
            _graphicsEligible[frame] = false;
#endif
            for (int i = 0; i < _buffers.Length; ++i)
            {
                Buffer buffer = _buffers[i];
                if (!buffer.available) continue;
                ProfilerRecorder recorder = _recorders[i];
                if (!recorder.Valid) { buffer.reason = "Recorder became invalid; missing values are N/A."; continue; }
                int count = recorder.Count;
                int previous = buffer.lastCount;
                buffer.lastCount = count;
                if (count >= recorder.Capacity - 1) { buffer.reason = "Recorder capacity saturated; frame attribution is N/A."; continue; }
                if (count <= previous) { buffer.reason = "Count did not advance or regressed; stale values are N/A."; continue; }
                if (count != previous + 1) { buffer.reason = "Recorder Count discontinuity; skipped values are N/A."; continue; }
                if (frame > 0 && _unityFrames[frame] != _unityFrames[frame - 1] + 1)
                { buffer.reason = "Player frame discontinuity; this sample is N/A."; continue; }
                buffer.values[frame] = recorder.LastValue;
                buffer.sampled[frame] = true;
            }
        }

        internal BenchmarkCounterReport[] BuildReports(string[] phaseNames, int[] expectedFrames)
        {
            var reports = new BenchmarkCounterReport[_buffers.Length];
            for (int i = 0; i < _buffers.Length; ++i)
            {
                Buffer buffer = _buffers[i];
                var report = reports[i] = new BenchmarkCounterReport
                {
                    name = buffer.name, category = buffer.category, rawUnit = buffer.unit,
                    reason = buffer.reason, phases = new BenchmarkPhaseReport[phaseNames.Length]
                };
                for (int phase = 0; phase < phaseNames.Length; ++phase)
                {
                    var values = new List<long>();
                    var frames = new List<int>();
                    int graphicsFrames = 0;
                    for (int f = 0; f < _frames; ++f) if (_phases[f] == phase && _graphicsEligible[f]) ++graphicsFrames;
                    for (int f = 0; f < _frames; ++f)
                        if (_phases[f] == phase && buffer.sampled[f]) { values.Add(buffer.values[f]); frames.Add(_unityFrames[f]); }
                    long[] raw = values.ToArray();
                    long[] sorted = (long[])raw.Clone();
                    Array.Sort(sorted);
                    double[] summary = Array.Empty<double>();
                    double[] milliseconds = Array.Empty<double>();
                    if (sorted.Length > 0)
                    {
                        int middle = sorted.Length / 2;
                        double median = sorted.Length % 2 == 0 ? sorted[middle - 1] / 2.0 + sorted[middle] / 2.0 : sorted[middle];
                        summary = new[] { median, sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1)], sorted[sorted.Length - 1] };
                        if (buffer.unit == ProfilerMarkerDataUnit.TimeNanoseconds.ToString())
                            milliseconds = new[] { summary[0] / 1e6, summary[1] / 1e6, summary[2] / 1e6 };
                    }
                    report.phases[phase] = new BenchmarkPhaseReport
                    {
                        name = phaseNames[phase], expectedFrames = expectedFrames[phase], freshSamples = raw.Length,
                        status = raw.Length == 0 ? "N/A" : raw.Length == expectedFrames[phase] ? "Measured" : "Partial",
                        reason = raw.Length == expectedFrames[phase] ? null : buffer.reason ?? "Incomplete fresh marker coverage; missing values are N/A.",
                        graphicsEligibleFrames = graphicsFrames, unityFrames = frames.ToArray(), rawValues = raw, summaryOrder = "median,p95,max",
                        summaryRaw = summary, summaryMilliseconds = milliseconds
                    };
                }
            }
            return reports;
        }

        internal void Stop()
        {
            if (_recorders == null) return;
            for (int i = 0; i < _recorders.Length; ++i) if (_buffers[i]?.available == true) _recorders[i].Dispose();
            _recorders = null;
        }

        internal static string RenderEvidence(BenchmarkCounterReport[] reports, int measuredPhase)
        {
            foreach (BenchmarkCounterReport report in reports)
            {
                if (report.name != "Draw Calls Count") continue;
                BenchmarkPhaseReport phase = report.phases[measuredPhase];
                if (phase.graphicsEligibleFrames != phase.expectedFrames)
                    return "Failed/N/A: measured interval lacked a graphics Windows64 Player or fixed 1280x720 resolution.";
                if (phase.status != "Measured") return "N/A: draw-call marker is unavailable/stale/partial; render success is unproven.";
                foreach (long value in phase.rawValues) if (value <= 0) return "Failed: a fresh measured draw-call value was zero.";
                return "Fresh nonzero draws measured; graphics device/backend/identity gates are separate.";
            }
            return "N/A: no draw-call marker.";
        }

        internal static BenchmarkEnvironment Environment(string revision, string suppliedBackend)
        {
            if (string.IsNullOrWhiteSpace(revision)) revision = System.Environment.GetEnvironmentVariable("TPLAB_UI_BENCHMARK_REVISION");
            if (string.IsNullOrWhiteSpace(suppliedBackend)) suppliedBackend = System.Environment.GetEnvironmentVariable("TPLAB_UI_BENCHMARK_BACKEND");
#if ENABLE_MONO
            const string actualBackend = "Mono2x";
#elif ENABLE_IL2CPP
            const string actualBackend = "IL2CPP";
#else
            const string actualBackend = "N/A";
#endif
            bool exactRevision = revision != null && revision.Length == 40;
            if (exactRevision) foreach (char character in revision)
                if (!(character >= '0' && character <= '9' || character >= 'a' && character <= 'f')) { exactRevision = false; break; }
            bool eligible = !Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer && IntPtr.Size == 8
                && actualBackend == "Mono2x" && suppliedBackend == actualBackend && exactRevision
                && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null
                && Screen.width == 1280 && Screen.height == 720;
            return new BenchmarkEnvironment
            {
                identityGate = eligible ? "Eligible graphics Windows64 Mono identity; actual copied-source hashes still need runner verification." : "Failed/N/A: graphics Player/backend/resolution/exact source identity gate unmet.",
                sourceRevision = revision, suppliedBackend = suppliedBackend, actualBackend = actualBackend,
                unityVersion = Application.unityVersion, isEditor = Application.isEditor, platform = Application.platform.ToString(),
                graphicsDevice = SystemInfo.graphicsDeviceName, graphicsBackend = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDriver = SystemInfo.graphicsDeviceVersion, processor = SystemInfo.processorType,
                operatingSystem = SystemInfo.operatingSystem, memoryMiB = SystemInfo.systemMemorySize,
                screenWidth = Screen.width, screenHeight = Screen.height,
                targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount,
                batchBreakingReason = "N/A: no independent UI Profiler observation captured by this draft.",
                overdraw = "N/A: no independent overdraw capture; never inferred from draws/batches."
            };
        }

        internal static void Write(string requested, string environmentKey, object report)
        {
            string path = string.IsNullOrWhiteSpace(requested) ? System.Environment.GetEnvironmentVariable(environmentKey) : requested;
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || File.Exists(path))
                throw new InvalidOperationException("Draft requires an explicit fresh absolute evidence filename.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        private sealed class Buffer
        {
            internal readonly string name;
            internal readonly long[] values;
            internal readonly bool[] sampled;
            internal string category, unit, reason;
            internal bool available;
            internal int lastCount;
            internal Buffer(string counter, int capacity)
            { name = counter; values = new long[capacity]; sampled = new bool[capacity]; }
        }
    }

    [Serializable] internal sealed class BenchmarkCounterReport
    {
        public string name, category, rawUnit, reason;
        public BenchmarkPhaseReport[] phases;
    }
    [Serializable] internal sealed class BenchmarkPhaseReport
    {
        public string name, status, reason, summaryOrder;
        public int expectedFrames, freshSamples, graphicsEligibleFrames;
        public int[] unityFrames;
        public long[] rawValues;
        public double[] summaryRaw;
        public double[] summaryMilliseconds;
    }
    [Serializable] internal sealed class BenchmarkEnvironment
    {
        public string identityGate, sourceRevision, suppliedBackend, actualBackend, unityVersion, platform;
        public string graphicsDevice, graphicsBackend, graphicsDriver, processor, operatingSystem, batchBreakingReason, overdraw;
        public bool isEditor;
        public int memoryMiB, screenWidth, screenHeight, targetFrameRate, vSyncCount;
    }
}