using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI
{
    /// <summary>Owns bounded fixed-height, vertical, one-column cells over a borrowed native ScrollRect.</summary>
    /// <remarks>
    /// The ScrollRect, viewport/content, prefab, and project data remain borrowed. Only cell clones, binding
    /// tokens, staging storage, and this component's native listener are owned. Native drag, inertia,
    /// elasticity, and scrollbar processing remain ScrollRect responsibilities. No input adapter is required.
    /// Commands require Unity's main thread. Metrics describe settled state outside project callbacks.
    /// </remarks>
    public sealed class VirtualScrollRect : MonoBehaviour
    {
        private readonly Dictionary<int, Cell> _active = new Dictionary<int, Cell>();
        private readonly Stack<Cell> _inactive = new Stack<Cell>();
        private readonly List<Cell> _owned = new List<Cell>();
        private readonly List<Cell> _retiring = new List<Cell>();
        private ScrollRect _scroll;
        private RectTransform _viewport;
        private RectTransform _content;
        private RectTransform _prefab;
        private GameObject _staging;
        private Action<VirtualCellBinding> _bind;
        private Action<VirtualCellBinding> _unbind;
        private float _height;
        private float _spacing;
        private float _stride;
        private int _overscan;
        private int _count;
        private int _totalCreated;
        private int _totalDestroyed;
        private int _callbackDepth;
        private int _version;
        private bool _configured;
        private bool _subscribed;
        private bool _updating;
        private bool _clearing;
        private bool _destroyed;
        private bool _failed;
        private bool _dirty;
        private float _lastViewportHeight;
        private float _lastOffset;

        /// <summary>Gets logical data rows independently of cell ownership, including while disabled.</summary>
        public int Count => _count;
        /// <summary>Gets current bound cells; binding callbacks run before their native activation.</summary>
        public int CountActive => _active.Count;
        /// <summary>Gets unbound cells held for reuse.</summary>
        public int CountInactive => _inactive.Count;
        /// <summary>Gets all owned cells, active plus retained, independently of logical Count.</summary>
        public int CountOwned => _owned.Count;
        /// <summary>Gets cumulative cell clone creation during this component lifetime.</summary>
        public int TotalCreated => _totalCreated;
        /// <summary>Gets cumulative cell retirement; PlayMode native destruction completes at frame end.</summary>
        public int TotalDestroyed => _totalDestroyed;

        /// <summary>Configures a borrowed vertical ScrollRect, cell prefab, and project binding callbacks.</summary>
        /// <param name="scrollRect">Live native ScrollRect with explicit viewport and direct-child content.</param>
        /// <param name="cellPrefab">Live borrowed RectTransform, preserved during disable, reconfiguration, and destruction.</param>
        /// <param name="cellHeight">Finite positive row height, applied only to owned clones.</param>
        /// <param name="bind">Required data/subscription callback for each binding generation, before activation.</param>
        /// <param name="unbind">Optional subscription/reference removal, after token cancellation and deactivation.</param>
        /// <param name="overscan">Non-negative rows before and after the visible window.</param>
        /// <param name="spacing">Finite non-negative gap; height plus spacing must also be finite.</param>
        /// <remarks>
        /// Content/cell roots use top-stretch anchors, pivot (0.5,1), identity local rotation/scale, and no
        /// LayoutGroup/ContentSizeFitter on those roots. Content has no horizontal padding or offset.
        /// This component writes borrowed content extent/position; project prefab and ScrollRect settings are preserved.
        /// Invalid configuration is rejected before working state changes. Successful replacement retires old bindings
        /// and cells and starts at Count zero; call SetCount for the new project data.
        /// Bind/unbind must not reenter Configure/SetCount/Refresh/ScrollToIndex on this component. Other UI is independent.
        /// Native notifications caused by reconciliation are coalesced. Native disable/destruction still performs cleanup;
        /// project callbacks must not directly destroy or alter ownership of supplied native cells.
        /// </remarks>
        /// <exception cref="ArgumentException">Borrowed inputs, callbacks, or native layout are invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Height, spacing, or overscan is invalid.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread or synchronous mutation reentry.</exception>
        /// <exception cref="AggregateException">Previous owned cleanup failed after remaining cleanup was attempted.</exception>
        public void Configure(ScrollRect scrollRect, RectTransform cellPrefab, float cellHeight,
            Action<VirtualCellBinding> bind, Action<VirtualCellBinding> unbind = null,
            int overscan = 2, float spacing = 0)
        {
            EnsureCommand(false);
            if (!IsFinite(cellHeight) || cellHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cellHeight));
            }
            if (!IsFinite(spacing) || spacing < 0 || (double)cellHeight + spacing > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(spacing));
            }
            if (overscan < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(overscan));
            }
            if (bind == null)
            {
                throw new ArgumentNullException(nameof(bind));
            }
            ValidateLayout(scrollRect, cellPrefab);
            Unsubscribe();
            var errors = new List<Exception>();
            _updating = true;
            try
            {
                ClearBindings(errors, false);
                DestroyCells(errors);
                DestroyStaging(errors);
            }
            finally
            {
                _updating = false;
            }
            if (errors.Count != 0)
            {
                _failed = true;
                Subscribe();
                throw new AggregateException(errors);
            }
            if (_destroyed)
            {
                throw new ObjectDisposedException(nameof(VirtualScrollRect));
            }
            _scroll = scrollRect;
            _viewport = scrollRect.viewport;
            _content = scrollRect.content;
            _prefab = cellPrefab;
            _height = cellHeight;
            _spacing = spacing;
            _stride = cellHeight + spacing;
            _overscan = overscan;
            _bind = bind;
            _unbind = unbind;
            _count = 0;
            _configured = true;
            _failed = false;
            ++_version;
            Subscribe();
            Reconcile(false, true);
        }

        /// <summary>Sets a non-negative logical count, clamps native position, and reconciles cell range.</summary>
        /// <remarks>
        /// Unchanged indexes retain their bindings. Use Refresh after replacing project data at existing indexes.
        /// Failure cancels/unbinds every partial binding and stops automatic retries until another explicit command.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Count is negative or its native extent is not finite.</exception>
        /// <exception cref="InvalidOperationException">Unconfigured, wrong thread, or synchronous mutation reentry.</exception>
        /// <exception cref="AggregateException">Binding/native work failed after remaining cleanup was attempted.</exception>
        public void SetCount(int count)
        {
            EnsureCommand();
            GetExtent(count);
            _count = count;
            _failed = false;
            Reconcile(false, true);
        }

        /// <summary>Recomputes layout and explicitly replaces visible binding generations for project data changes.</summary>
        /// <remarks>
        /// Automatic scroll/viewport reconciliation keeps unchanged indexes and tokens; idle frames do not rebind.
        /// Owned cells are limited by current viewport demand plus overscan, and viewport shrink trims excess cells.
        /// A failed operation can be retried explicitly here. Source data and async work remain project-owned.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Unconfigured, wrong thread, or synchronous mutation reentry.</exception>
        /// <exception cref="AggregateException">Binding/native work failed after remaining cleanup was attempted.</exception>
        public void Refresh()
        {
            EnsureCommand();
            _failed = false;
            Reconcile(true, true);
        }

        /// <summary>Stops native inertia and jumps to an indexed row, clamped at the end of the content.</summary>
        /// <remarks>Index zero on an empty list resets to the top; other indexes must belong to Count.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Index does not belong to the list.</exception>
        /// <exception cref="InvalidOperationException">Unconfigured, wrong thread, or synchronous mutation reentry.</exception>
        /// <exception cref="AggregateException">Binding/native work failed after remaining cleanup was attempted.</exception>
        public void ScrollToIndex(int index)
        {
            EnsureCommand();
            if (index < 0 || (_count == 0 ? index != 0 : index >= _count))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            ValidateLiveInputs();
            float extent = GetExtent(_count);
            float maximum = Mathf.Max(0, extent - _viewport.rect.height);
            _scroll.StopMovement();
            Vector2 position = _content.anchoredPosition;
            position.y = (float)Math.Min((double)index * _stride, maximum);
            _content.anchoredPosition = position;
            _failed = false;
            Reconcile(false, true);
        }

        private void EnsureCommand(bool requireConfiguration = true)
        {
            UIContext.EnsureMainThread();
            if (_callbackDepth != 0 || _updating || _clearing)
            {
                throw new InvalidOperationException("Binding or native reconciliation cannot reenter this virtual list.");
            }
            if (_destroyed)
            {
                throw new ObjectDisposedException(nameof(VirtualScrollRect));
            }
            if (requireConfiguration && !_configured)
            {
                throw new InvalidOperationException("Configure the virtual list before changing data or position.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void ValidateLayout(ScrollRect scroll, RectTransform prefab)
        {
            if (scroll == null || prefab == null || scroll.viewport == null || scroll.content == null)
            {
                throw new ArgumentException("A live ScrollRect, explicit viewport/content, and cell prefab are required.");
            }
            if (!scroll.vertical || scroll.horizontal || scroll.content.parent != scroll.viewport)
            {
                throw new ArgumentException("Only vertical scrolling with direct viewport content is supported.");
            }
            ValidateRect(scroll.content);
            ValidateRect(prefab);
            if (!Mathf.Approximately(scroll.content.sizeDelta.x, 0)
                || !Mathf.Approximately(scroll.content.anchoredPosition.x, 0)
                || !IsFinite(scroll.viewport.rect.height) || scroll.viewport.rect.height < 0)
            {
                throw new ArgumentException("Content cannot have horizontal padding/offset and viewport height must be finite.");
            }
        }

        private static void ValidateRect(RectTransform rect)
        {
            if (rect.anchorMin != new Vector2(0, 1) || rect.anchorMax != new Vector2(1, 1)
                || rect.pivot != new Vector2(0.5f, 1) || rect.localScale != Vector3.one
                || Quaternion.Angle(rect.localRotation, Quaternion.identity) > 0.01f
                || rect.GetComponent<LayoutGroup>() != null || rect.GetComponent<ContentSizeFitter>() != null)
            {
                throw new ArgumentException("Content/cell roots require a fixed top-stretch rect without layout drivers.");
            }
        }

        private void ValidateLiveInputs()
        {
            if (_scroll == null || _viewport == null || _content == null || _prefab == null
                || _scroll.viewport != _viewport || _scroll.content != _content)
            {
                throw new InvalidOperationException("A borrowed virtual-list input was destroyed or replaced.");
            }
            ValidateLayout(_scroll, _prefab);
            if (!IsFinite(_content.anchoredPosition.y))
            {
                throw new InvalidOperationException("The native content position must be finite.");
            }
        }

        private float GetExtent(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            double extent = count == 0 ? 0 : (double)count * _height + (double)(count - 1) * _spacing;
            if (extent > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The content extent is not representable by RectTransform.");
            }
            return (float)extent;
        }

        private int GetBudget(float viewportHeight)
        {
            double budget = Math.Ceiling(viewportHeight / (double)_stride) + 1 + 2.0 * _overscan;
            return budget >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)budget);
        }

        private void GetRange(float viewportHeight, float extent, out int first, out int last)
        {
            first = 0;
            last = -1;
            if (_count == 0 || viewportHeight <= 0)
            {
                return;
            }
            // Clamp only the range calculation during native elastic overscroll, never its physical position.
            double offset = Mathf.Clamp(_content.anchoredPosition.y, 0, Mathf.Max(0, extent - viewportHeight));
            double visibleFirst = Math.Max(0, Math.Floor((offset - _height) / _stride) + 1);
            double visibleLast = Math.Min(_count - 1, Math.Ceiling((offset + viewportHeight) / _stride) - 1);
            first = (int)Math.Max(0, visibleFirst - _overscan);
            last = (int)Math.Min(_count - 1, visibleLast + _overscan);
        }

        private void Reconcile(bool forceRebind, bool clampPosition)
        {
            if (_updating)
            {
                _dirty = true;
                return;
            }
            _updating = true;
            _dirty = false;
            int version = _version;
            List<Exception> errors = null;
            try
            {
                ValidateLiveInputs();
                float viewportHeight = _viewport.rect.height;
                float extent = GetExtent(_count);
                if (!Mathf.Approximately(_content.rect.height, extent))
                {
                    _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, extent);
                }
                if (clampPosition || !Mathf.Approximately(viewportHeight, _lastViewportHeight))
                {
                    Vector2 position = _content.anchoredPosition;
                    float clamped = Mathf.Clamp(position.y, 0, Mathf.Max(0, extent - viewportHeight));
                    if (!Mathf.Approximately(position.y, clamped))
                    {
                        _scroll.StopMovement();
                        position.y = clamped;
                        _content.anchoredPosition = position;
                    }
                }
                int budget = GetBudget(viewportHeight);
                GetRange(viewportHeight, extent, out int first, out int last);
                _retiring.Clear();
                foreach (Cell cell in _active.Values)
                {
                    if (forceRebind || !isActiveAndEnabled || cell.Index < first || cell.Index > last)
                    {
                        _retiring.Add(cell);
                    }
                }
                foreach (Cell cell in _retiring)
                {
                    ReleaseCell(cell, ref errors, true);
                }
                _retiring.Clear();
                if (errors == null && CanContinue(version))
                {
                    // Fill only the viewport budget so fractional native scrolling needs no later allocation.
                    int capacity = Math.Min(_count, budget);
                    while (_owned.Count < capacity && CanContinue(version))
                    {
                        _inactive.Push(CreateCell());
                    }
                    for (int index = first; index <= last && CanContinue(version); ++index)
                    {
                        if (_active.ContainsKey(index))
                        {
                            continue;
                        }
                        Cell cell = RentCell();
                        BindCell(cell, index, version);
                    }
                }
                TrimInactive(budget, ref errors);
                if (errors != null && errors.Count != 0)
                {
                    _failed = true;
                    ClearBindings(errors, true);
                }
            }
            catch (Exception error)
            {
                _failed = true;
                AddError(ref errors, error);
                ClearBindings(EnsureErrors(ref errors), true);
            }
            finally
            {
                _retiring.Clear();
                _lastViewportHeight = _viewport != null ? _viewport.rect.height : 0;
                _lastOffset = _content != null ? _content.anchoredPosition.y : 0;
                _updating = false;
            }
            if (errors != null && errors.Count != 0)
            {
                throw new AggregateException(errors);
            }
        }

        private bool CanContinue(int version)
        {
            return !_destroyed && version == _version && isActiveAndEnabled
                && _scroll != null && _viewport != null && _content != null;
        }

        private Cell RentCell()
        {
            while (_inactive.Count != 0)
            {
                Cell retained = _inactive.Pop();
                if (retained.View != null)
                {
                    return retained;
                }
                if (_owned.Remove(retained))
                {
                    ++_totalDestroyed;
                }
            }
            return CreateCell();
        }

        private Cell CreateCell()
        {
            if (_staging == null)
            {
                _staging = new GameObject("VirtualScrollRect Inactive Staging");
                _staging.SetActive(false);
                _staging.transform.SetParent(transform, false);
            }
            RectTransform clone = UnityEngine.Object.Instantiate(_prefab, _staging.transform, false);
            var cell = new Cell(this, clone);
            _owned.Add(cell);
            ++_totalCreated;
            try
            {
                clone.gameObject.SetActive(false);
                clone.SetParent(_content, false);
                return cell;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                DestroyCell(cell, errors);
                throw new AggregateException(errors);
            }
        }

        private void BindCell(Cell cell, int index, int version)
        {
            try
            {
                cell.View.anchorMin = new Vector2(0, 1);
                cell.View.anchorMax = new Vector2(1, 1);
                cell.View.pivot = new Vector2(0.5f, 1);
                cell.View.localRotation = Quaternion.identity;
                cell.View.localScale = Vector3.one;
                cell.View.sizeDelta = new Vector2(_prefab.sizeDelta.x, _height);
                cell.View.anchoredPosition3D = new Vector3(_prefab.anchoredPosition3D.x,
                    -(float)((double)index * _stride), _prefab.anchoredPosition3D.z);
                cell.Index = index;
                ++cell.Generation;
                cell.Lifetime = new CancellationTokenSource();
                cell.Binding = new VirtualCellBinding(cell, index, cell.Generation, cell.View, cell.Lifetime.Token);
                cell.Bound = true;
                _active.Add(index, cell);
                ++_callbackDepth;
                try
                {
                    _bind(cell.Binding);
                }
                finally
                {
                    --_callbackDepth;
                }
                if (!CanContinue(version) || !cell.Bound)
                {
                    return;
                }
                if (cell.View == null || cell.View.parent != _content)
                {
                    throw new InvalidOperationException("A binding callback changed owned cell lifetime or parent.");
                }
                cell.View.gameObject.SetActive(true);
                if (CanContinue(version) && cell.Bound && (cell.View == null || cell.View.parent != _content))
                {
                    throw new InvalidOperationException("Native activation changed owned cell lifetime or parent.");
                }
            }
            catch (Exception error)
            {
                // A failed binding cannot become a reusable cell before project cleanup has run.
                var errors = new List<Exception> { error };
                DestroyCell(cell, errors);
                throw new AggregateException(errors);
            }
        }

        private void ReleaseCell(Cell cell, ref List<Exception> errors, bool retain)
        {
            if (!cell.Bound)
            {
                return;
            }
            int before = errors?.Count ?? 0;
            UnbindCell(cell, EnsureErrors(ref errors));
            if (errors.Count == before && retain && !_destroyed && cell.View != null && _owned.Contains(cell))
            {
                _inactive.Push(cell);
            }
            else
            {
                DestroyCell(cell, errors);
            }
            if (errors.Count == 0)
            {
                errors = null;
            }
        }

        private void UnbindCell(Cell cell, List<Exception> errors)
        {
            if (!cell.Bound)
            {
                return;
            }
            cell.Bound = false;
            _active.Remove(cell.Index);
            CancellationTokenSource lifetime = cell.Lifetime;
            Action<VirtualCellBinding> unbind = _unbind;
            cell.Lifetime = null;
            TryCleanup(() => lifetime?.Cancel(), errors);
            TryCleanup(() =>
            {
                if (cell.View != null)
                {
                    cell.View.gameObject.SetActive(false);
                }
            }, errors);
            ++_callbackDepth;
            try
            {
                TryCleanup(() => unbind?.Invoke(cell.Binding), errors);
            }
            finally
            {
                --_callbackDepth;
                lifetime?.Dispose();
            }
        }

        private void ClearBindings(List<Exception> errors, bool retain)
        {
            if (_clearing)
            {
                return;
            }
            _clearing = true;
            try
            {
                var cells = new List<Cell>(_active.Values);
                foreach (Cell cell in cells)
                {
                    List<Exception> collected = errors;
                    ReleaseCell(cell, ref collected, retain);
                }
            }
            finally
            {
                _clearing = false;
            }
        }

        private void TrimInactive(int budget, ref List<Exception> errors)
        {
            while (_owned.Count > budget && _inactive.Count != 0)
            {
                DestroyCell(_inactive.Pop(), EnsureErrors(ref errors));
            }
            if (errors != null && errors.Count == 0)
            {
                errors = null;
            }
        }

        private void DestroyCell(Cell cell, List<Exception> errors)
        {
            UnbindCell(cell, errors);
            if (!_owned.Remove(cell))
            {
                return;
            }
            ++_totalDestroyed;
            RectTransform view = cell.View;
            cell.View = null;
            TryCleanup(() => DestroyNative(view != null ? view.gameObject : null), errors);
        }

        private void DestroyCells(List<Exception> errors)
        {
            _inactive.Clear();
            var cells = new List<Cell>(_owned);
            foreach (Cell cell in cells)
            {
                DestroyCell(cell, errors);
            }
        }

        private void DestroyStaging(List<Exception> errors)
        {
            GameObject staging = _staging;
            _staging = null;
            TryCleanup(() => DestroyNative(staging), errors);
        }

        private static void DestroyNative(GameObject owned)
        {
            if (owned == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(owned);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(owned);
            }
        }

        private static List<Exception> EnsureErrors(ref List<Exception> errors)
        {
            return errors ?? (errors = new List<Exception>());
        }

        private static void AddError(ref List<Exception> errors, Exception error)
        {
            EnsureErrors(ref errors).Add(error);
        }

        private static void TryCleanup(Action action, List<Exception> errors)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        private void Subscribe()
        {
            if (_configured && !_subscribed && isActiveAndEnabled && _scroll != null)
            {
                _scroll.onValueChanged.AddListener(OnScrollChanged);
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed && _scroll != null)
            {
                _scroll.onValueChanged.RemoveListener(OnScrollChanged);
            }
            _subscribed = false;
        }

        private void OnScrollChanged(Vector2 position)
        {
            _dirty = true;
            if (!_updating && _callbackDepth == 0)
            {
                ReconcileAutomatic();
            }
        }

        private void LateUpdate()
        {
            if (!_configured || _destroyed || _failed || !isActiveAndEnabled)
            {
                return;
            }
            if (_viewport == null || _content == null || _dirty
                || !Mathf.Approximately(_viewport.rect.height, _lastViewportHeight)
                || !Mathf.Approximately(_content.anchoredPosition.y, _lastOffset))
            {
                ReconcileAutomatic();
            }
        }

        private void ReconcileAutomatic()
        {
            if (!_configured || _destroyed || _failed || !isActiveAndEnabled)
            {
                return;
            }
            try
            {
                Reconcile(false, false);
            }
            catch (Exception error)
            {
                // Native event/lifecycle boundaries cannot return a command result; preserve the completed cleanup error in Unity's log.
                Debug.LogException(error, this);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            _dirty = true;
        }

        private void OnDisable()
        {
            ++_version;
            Unsubscribe();
            var errors = new List<Exception>();
            ClearBindings(errors, true);
            if (errors.Count != 0)
            {
                Debug.LogException(new AggregateException(errors), this);
            }
        }

        private void OnDestroy()
        {
            _destroyed = true;
            ++_version;
            Unsubscribe();
            var errors = new List<Exception>();
            ClearBindings(errors, false);
            DestroyCells(errors);
            DestroyStaging(errors);
            _bind = null;
            _unbind = null;
            _scroll = null;
            _viewport = null;
            _content = null;
            _prefab = null;
            if (errors.Count != 0)
            {
                Debug.LogException(new AggregateException(errors), this);
            }
        }

        internal bool IsBindingCurrent(Cell cell, long generation)
        {
            UIContext.EnsureMainThread();
            return !_destroyed && isActiveAndEnabled && cell.Bound && cell.Generation == generation && cell.View != null;
        }

        internal sealed class Cell
        {
            internal readonly VirtualScrollRect Owner;
            internal RectTransform View;
            internal int Index;
            internal long Generation;
            internal bool Bound;
            internal CancellationTokenSource Lifetime;
            internal VirtualCellBinding Binding;

            internal Cell(VirtualScrollRect owner, RectTransform view)
            {
                Owner = owner;
                View = view;
            }
        }
    }
}