using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TPLab.UI.Tests
{
    /// <summary>Draft integration tests using a real ScrollRect and LegacyRuntime Text Graphic; not executed. Drag uses synthetic PointerEventData dispatched through ExecuteEvents, not physical/Input System coverage.</summary>
    public sealed class VirtualScrollRectPlayModeTests
    {
        private Fixture _fixture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_fixture?.Root != null)
            {
                UnityEngine.Object.Destroy(_fixture.Root);
            }
            if (_fixture?.Prefab != null)
            {
                UnityEngine.Object.Destroy(_fixture.Prefab.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmallAndLargeCountsKeepVisibleGraphicIndicesCorrect()
            => CheckCounts();

        private IEnumerator CheckCounts()
        {
            _fixture = CreateFixture(200);
            int[] counts = { 0, 1, 100, 1000, 10000 };
            foreach (int count in counts)
            {
                _fixture.Virtual.SetCount(count);
                _fixture.Virtual.ScrollToIndex(0);
                _fixture.Virtual.Refresh();
                yield return null;
                yield return null;
                Assert.That(_fixture.Virtual.Count, Is.EqualTo(count));
                if (count == 0)
                {
                    Assert.That(_fixture.Virtual.CountActive, Is.Zero);
                }
                else
                {
                    Assert.That(_fixture.Virtual.CountActive, Is.GreaterThan(0));
                }
                Assert.That(_fixture.Virtual.CountActive, Is.LessThanOrEqualTo(15));
                Assert.That(_fixture.Virtual.CountOwned, Is.EqualTo(_fixture.Virtual.CountActive + _fixture.Virtual.CountInactive));
                Assert.That(_fixture.Virtual.CountOwned, Is.LessThanOrEqualTo(15));
                AssertVisibleWindow(count);
                var idleBindings = _fixture.Bindings.Where(binding => binding.IsCurrent).ToArray();
                int bindCount = _fixture.Bindings.Count;
                int unbindCount = _fixture.Unbindings.Count;
                yield return null;
                yield return null;
                Assert.That(_fixture.Bindings.Count, Is.EqualTo(bindCount), "Native idle reconciliation does not rebind unchanged rows.");
                Assert.That(_fixture.Unbindings.Count, Is.EqualTo(unbindCount));
                Assert.That(idleBindings.All(binding => binding.IsCurrent && !binding.LifetimeToken.IsCancellationRequested), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator LogicalJumpScrollbarAndEventSystemDragInertiaKeepTextRowsInRange()
            => CheckNativeScrollPaths();

        private IEnumerator CheckNativeScrollPaths()
        {
            _fixture = CreateFixture(200, withScrollbar: true);
            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            yield return null;

            foreach (int index in new[] { 0, 499, 999 })
            {
                _fixture.Virtual.ScrollToIndex(index);
                yield return null;
                Assert.That(VisibleIndexes().Contains(index), Is.True, "Requested row should be bound and visible.");
                AssertVisibleWindow(1000);
            }

            _fixture.Scroll.verticalScrollbar.value = 1;
            yield return null;
            Assert.That(VisibleIndexes().Contains(0), Is.True);
            AssertVisibleWindow(1000);
            _fixture.Scroll.verticalScrollbar.value = 0;
            yield return null;
            Assert.That(VisibleIndexes().Contains(999), Is.True);
            AssertVisibleWindow(1000);

            _fixture.Virtual.ScrollToIndex(499);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, _fixture.Viewport.position);
            var pointer = new PointerEventData(_fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pressPosition = center,
                position = center
            };
            ExecuteEvents.Execute(_fixture.Scroll.gameObject, pointer, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(_fixture.Scroll.gameObject, pointer, ExecuteEvents.beginDragHandler);
            float beforeDrag = _fixture.Content.anchoredPosition.y;
            pointer.position = center + Vector2.up * 60;
            pointer.delta = Vector2.up * 60;
            ExecuteEvents.Execute(_fixture.Scroll.gameObject, pointer, ExecuteEvents.dragHandler);
            yield return null;
            Assert.That(_fixture.Content.anchoredPosition.y, Is.GreaterThan(beforeDrag));
            Assert.That(_fixture.Scroll.velocity.y, Is.GreaterThan(0.001f), "Real ScrollRect LateUpdate must produce drag velocity.");
            ExecuteEvents.Execute(_fixture.Scroll.gameObject, pointer, ExecuteEvents.endDragHandler);
            float afterDrag = _fixture.Content.anchoredPosition.y;
            yield return null;
            Assert.That(_fixture.Content.anchoredPosition.y, Is.GreaterThan(afterDrag), "Native inertia advances away from both clamped boundaries.");
            Assert.That(VisibleIndexes().All(index => index >= 0 && index < 1000), Is.True);
            AssertVisibleWindow(1000);
        }

        [UnityTest]
        public IEnumerator ShrinkingAndRestoringCountClampsTheNativeContentAndBindings()
            => CheckCountChanges();

        private IEnumerator CheckCountChanges()
        {
            _fixture = CreateFixture(200);
            _fixture.Virtual.SetCount(10000);
            _fixture.Virtual.Refresh();
            _fixture.Virtual.ScrollToIndex(9999);
            yield return null;
            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            yield return null;
            Assert.That(_fixture.Virtual.Count, Is.EqualTo(1000));
            Assert.That(VisibleIndexes().All(index => index >= 0 && index < 1000), Is.True);
            AssertVisibleWindow(1000);

            _fixture.Virtual.SetCount(0);
            _fixture.Virtual.Refresh();
            yield return null;
            Assert.That(_fixture.Virtual.CountActive, Is.Zero);
            Assert.That(_fixture.Virtual.Count, Is.Zero);
            AssertVisibleWindow(0);

            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            _fixture.Virtual.ScrollToIndex(0);
            yield return null;
            Assert.That(VisibleIndexes().Contains(0), Is.True);
            AssertVisibleWindow(1000);
        }

        [UnityTest]
        public IEnumerator ResizeDisableEnableAndDestroyReleaseBindingsButPreserveBorrowedScrollRectAndPrefab()
            => CheckLifetimeAndResize();

        private IEnumerator CheckLifetimeAndResize()
        {
            _fixture = CreateFixture(200);
            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            yield return null;
            _fixture.Scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 400);
            _fixture.Virtual.Refresh();
            yield return null;
            Assert.That(_fixture.Virtual.CountActive, Is.LessThanOrEqualTo(25));
            Assert.That(_fixture.Virtual.CountOwned, Is.LessThanOrEqualTo(25));
            _fixture.Scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
            _fixture.Virtual.Refresh();
            yield return null;
            Assert.That(_fixture.Virtual.CountActive, Is.LessThanOrEqualTo(9));
            Assert.That(_fixture.Virtual.CountOwned, Is.LessThanOrEqualTo(9), "Resize trims excess retained cells to the current viewport budget.");

            var current = _fixture.Bindings.Where(binding => binding.IsCurrent).ToArray();
            _fixture.Virtual.enabled = false;
            Assert.That(current.All(binding => binding.LifetimeToken.IsCancellationRequested && !binding.IsCurrent), Is.True);
            Assert.That(_fixture.Virtual.CountActive, Is.Zero);
            _fixture.Virtual.enabled = true;
            _fixture.Virtual.Refresh();
            yield return null;
            var atDestruction = _fixture.Bindings.Where(binding => binding.IsCurrent).ToArray();
            UnityEngine.Object.Destroy(_fixture.Virtual);
            yield return null;
            Assert.That(atDestruction.All(binding => binding.LifetimeToken.IsCancellationRequested && !binding.IsCurrent), Is.True);
            Assert.That(_fixture.Content.GetComponentsInChildren<Text>(true), Is.Empty, "Destroy releases every owned native cell.");
            Assert.That(_fixture.Scroll != null && _fixture.Prefab != null, Is.True, "The configured ScrollRect and prefab are borrowed.");
        }

        [UnityTest]
        public IEnumerator ExtentAndLastIndexJumpStayWithinNativeViewportBounds()
            => CheckExtentAndLastJump();

        private IEnumerator CheckExtentAndLastJump()
        {
            _fixture = CreateFixture(200, spacing: 2.5f);
            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            Assert.That(_fixture.Content.rect.height, Is.EqualTo(20000 + 999 * 2.5f).Within(0.1f));
            _fixture.Virtual.ScrollToIndex(999);
            yield return null;
            float maxOffset = _fixture.Content.rect.height - _fixture.Viewport.rect.height;
            Assert.That(_fixture.Content.anchoredPosition.y, Is.InRange(0, maxOffset + 0.1f));
            Assert.That(VisibleIndexes().Contains(999), Is.True);
            AssertVisibleWindow(1000, spacing: 2.5f);

            _fixture.Virtual.SetCount(0);
            _fixture.Virtual.Refresh();
            yield return null;
            Assert.That(_fixture.Content.rect.height, Is.Zero.Within(0.1f));
        }

        [UnityTest]
        public IEnumerator LateBindingGenerationCannotOverwriteTheRecycledRows()
            => CheckStaleAndFailurePaths();

        private IEnumerator CheckStaleAndFailurePaths()
        {
            _fixture = CreateFixture(200);
            _fixture.Virtual.SetCount(1000);
            _fixture.Virtual.Refresh();
            yield return null;
            VirtualCellBinding stale = _fixture.Bindings.First(binding => binding.Index == 0 && binding.IsCurrent);
            Assert.That(stale.LifetimeToken.IsCancellationRequested, Is.False);
            RectTransform staleView = stale.View;
            long staleGeneration = stale.Generation;
            var staleToken = stale.LifetimeToken;
            var gate = new UniTaskCompletionSource();
            UniTask lateResult = ApplyAfterGate(stale, gate.Task, "late-stale-result");
            Task canceledWait = gate.Task.AttachExternalCancellation(staleToken).AsTask();
            _fixture.Virtual.ScrollToIndex(500);
            yield return null;
            VirtualCellBinding current = _fixture.Bindings.Last(binding => binding.View == staleView && binding.IsCurrent);
            Assert.That(staleToken.IsCancellationRequested, Is.True);
            Assert.That(stale.IsCurrent, Is.False);
            Assert.That(current.Generation, Is.GreaterThan(staleGeneration));
            Assert.That(stale.LifetimeToken.IsCancellationRequested, Is.True, "A cached token remains readable after its source is disposed.");
            gate.TrySetResult();
            yield return lateResult.ToCoroutine();
            bool cancellationObserved = false;
            yield return ObserveCancellation(canceledWait, () => cancellationObserved = true).ToCoroutine();
            Assert.That(cancellationObserved, Is.True, "Observe cancellation by awaiting the task and catching OperationCanceledException.");
            Assert.That(staleToken.IsCancellationRequested, Is.True);
            Assert.That(stale.IsCurrent, Is.False);
            Assert.That(staleView.GetComponent<Text>().text, Is.EqualTo(current.Index.ToString()));
            Assert.That(VisibleIndexes().All(index => index >= 0 && index < 1000), Is.True);

        }

        [UnityTest]
        public IEnumerator BindFailureCancelsAndUnbindsPartialRentals()
            => CheckBindFailure();

        private IEnumerator CheckBindFailure()
        {
            _fixture = CreateFixture(200, failAtIndex: 5);
            Exception observed = null;
            try
            {
                _fixture.Virtual.SetCount(100);
                _fixture.Virtual.Refresh();
            }
            catch (Exception error)
            {
                observed = error;
            }
            yield return null;
            Assert.That(observed, Is.Not.Null, "Bind failure must remain observable.");
            Assert.That(_fixture.Virtual.CountActive, Is.Zero);
            Assert.That(_fixture.Virtual.CountOwned, Is.LessThanOrEqualTo(15));
            Assert.That(_fixture.Bindings.All(binding => binding.LifetimeToken.IsCancellationRequested), Is.True);
            Assert.That(_fixture.Unbindings.Count, Is.EqualTo(_fixture.Bindings.Count));
        }

        private int[] VisibleIndexes()
            => _fixture.Content.GetComponentsInChildren<Text>(false).Select(text => int.Parse(text.text)).ToArray();

        private void AssertVisibleWindow(int count, float spacing = 0)
        {
            const float height = 20;
            float stride = height + spacing;
            float offset = _fixture.Content.anchoredPosition.y;
            float viewportHeight = _fixture.Viewport.rect.height;
            var current = _fixture.Bindings.Where(binding => binding.IsCurrent).ToArray();
            int[] indexes = current.Select(binding => binding.Index).ToArray();

            Assert.That(indexes.Distinct().Count(), Is.EqualTo(indexes.Length), "A logical row must have one active binding.");
            Assert.That(current.Length, Is.EqualTo(_fixture.Virtual.CountActive));
            Assert.That(_fixture.Content.GetComponentsInChildren<Text>(false).Length, Is.EqualTo(current.Length));
            foreach (VirtualCellBinding binding in current)
            {
                Assert.That(binding.Index, Is.InRange(0, count - 1));
                Assert.That(binding.View.rect.height, Is.EqualTo(height).Within(0.01f));
                Assert.That(binding.View.anchoredPosition.y, Is.EqualTo(-binding.Index * stride).Within(0.1f));
                Assert.That(binding.View.GetComponent<Text>().text, Is.EqualTo(binding.Index.ToString()));
            }

            int firstVisible = count == 0 ? 0 : Mathf.Max(0, Mathf.FloorToInt((offset - height) / stride) + 1);
            int lastVisible = count == 0 ? -1 : Mathf.Min(count - 1, Mathf.CeilToInt((offset + viewportHeight) / stride) - 1);
            for (int index = firstVisible; index <= lastVisible; index++)
            {
                Assert.That(indexes, Does.Contain(index), $"Visible row {index} must be bound at offset {offset}.");
            }
        }

        private static async UniTask ApplyAfterGate(VirtualCellBinding binding, UniTask gate, string result)
        {
            await gate;
            await UniTask.SwitchToMainThread();
            if (binding.LifetimeToken.IsCancellationRequested || !binding.IsCurrent)
            {
                return;
            }
            binding.View.GetComponent<Text>().text = result;
        }

        private static async UniTask ObserveCancellation(Task task, Action onCanceled)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                onCanceled();
            }
        }

        private Fixture CreateFixture(int viewportHeight, bool withScrollbar = false, int failAtIndex = -1, float spacing = 0)
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(EventSystem));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scrollObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(root.transform, false);
            scrollObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200, viewportHeight);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            Scrollbar scrollbar = null;
            if (withScrollbar)
            {
                var barObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Scrollbar));
                barObject.transform.SetParent(scrollObject.transform, false);
                scrollbar = barObject.GetComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = scrollbar;
            }
            var prefabObject = new GameObject("CellPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var prefabText = prefabObject.GetComponent<Text>();
            prefabText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prefabText.raycastTarget = false;
            prefabObject.SetActive(false);
            var prefabRect = prefabObject.GetComponent<RectTransform>();
            prefabRect.anchorMin = new Vector2(0, 1);
            prefabRect.anchorMax = new Vector2(1, 1);
            prefabRect.pivot = new Vector2(0.5f, 1);
            prefabRect.sizeDelta = new Vector2(0, 20);
            var fixture = new Fixture(root, prefabObject.GetComponent<RectTransform>(), scroll, viewport, content, root.GetComponent<EventSystem>());
            // Publish fixture ownership before the signature stub can throw during Configure.
            _fixture = fixture;
            fixture.Virtual = scrollObject.AddComponent<VirtualScrollRect>();
            fixture.Virtual.Configure(scroll, fixture.Prefab, 20,
                binding =>
                {
                    fixture.Bindings.Add(binding);
                    if (binding.Index == failAtIndex)
                    {
                        throw new InvalidOperationException("draft bind failure");
                    }
                    binding.View.GetComponent<Text>().text = binding.Index.ToString();
                }, binding => fixture.Unbindings.Add(binding), overscan: 2, spacing: spacing);
            return fixture;
        }

        private sealed class Fixture
        {
            internal readonly GameObject Root;
            internal readonly RectTransform Prefab;
            internal readonly ScrollRect Scroll;
            internal readonly RectTransform Viewport;
            internal readonly RectTransform Content;
            internal readonly EventSystem EventSystem;
            internal readonly List<VirtualCellBinding> Bindings = new List<VirtualCellBinding>();
            internal readonly List<VirtualCellBinding> Unbindings = new List<VirtualCellBinding>();
            internal VirtualScrollRect Virtual;

            internal Fixture(GameObject root, RectTransform prefab, ScrollRect scroll, RectTransform viewport,
                RectTransform content, EventSystem eventSystem)
            {
                Root = root;
                Prefab = prefab;
                Scroll = scroll;
                Viewport = viewport;
                Content = content;
                EventSystem = eventSystem;
            }
        }
    }
}
