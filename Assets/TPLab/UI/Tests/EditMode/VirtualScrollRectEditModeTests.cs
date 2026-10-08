using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI.Tests
{
    /// <summary>Draft contract tests. These should fail against the signature-only P5 stub.</summary>
    public sealed class VirtualScrollRectEditModeTests
    {
        private GameObject _root;
        private RectTransform _prefab;
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private VirtualScrollRect _virtual;
        private readonly List<VirtualCellBinding> _bound = new List<VirtualCellBinding>();
        private readonly List<VirtualCellBinding> _unbound = new List<VirtualCellBinding>();

        [SetUp]
        public void SetUp()
        {
            _root = CreateScrollRect(200, out _scrollRect, out _content);
            _prefab = CreateCellPrefab();
            _virtual = _scrollRect.gameObject.AddComponent<VirtualScrollRect>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab.gameObject);
            }
        }

        [Test]
        public void TenThousandRowsKeepLogicalCountSeparateFromRealGraphicCells()
        {
            ConfigureDefault();
            _virtual.SetCount(10000);
            _virtual.Refresh();
            Canvas.ForceUpdateCanvases();

            Assert.That(_virtual.Count, Is.EqualTo(10000));
            Assert.That(_virtual.CountActive, Is.GreaterThan(0));
            Assert.That(_virtual.CountActive, Is.LessThanOrEqualTo(15));
            Assert.That(_virtual.CountOwned, Is.EqualTo(_virtual.CountActive + _virtual.CountInactive));
            Assert.That(_virtual.CountOwned, Is.LessThanOrEqualTo(15));
            AssertVisibleWindow(10000);
        }

        [Test]
        public void EmptyingRowsUnbindsEveryActiveGenerationAndKeepsBorrowedInputsAlive()
        {
            ConfigureDefault();
            _virtual.SetCount(100);
            _virtual.Refresh();
            int activeBefore = _virtual.CountActive;
            int unboundBefore = _unbound.Count;

            _virtual.SetCount(0);
            _virtual.Refresh();

            Assert.That(_virtual.Count, Is.Zero);
            Assert.That(_virtual.CountActive, Is.Zero);
            Assert.That(_unbound.Count - unboundBefore, Is.EqualTo(activeBefore));
            Assert.That(_scrollRect != null && _prefab != null, Is.True);
        }

        [Test]
        public void ContentExtentUsesFixedRowsAndGapsAndBecomesZeroForNoRows()
        {
            ConfigureDefault();
            _virtual.SetCount(3);
            _virtual.Refresh();
            Assert.That(_content.rect.height, Is.EqualTo(65).Within(0.01f));

            _virtual.SetCount(0);
            _virtual.Refresh();
            Assert.That(_content.rect.height, Is.Zero.Within(0.01f));
        }

        [Test]
        public void WarmFixedViewportRefreshDoesNotContinueCreatingOrDestroyingCells()
        {
            ConfigureDefault();
            _virtual.SetCount(1000);
            _virtual.Refresh();
            _virtual.ScrollToIndex(500);
            _virtual.Refresh();
            Canvas.ForceUpdateCanvases();
            Assert.That(_virtual.CountActive, Is.GreaterThan(0));
            Assert.That(_virtual.TotalCreated, Is.GreaterThan(0));
            AssertVisibleWindow(1000);
            int created = _virtual.TotalCreated;
            int destroyed = _virtual.TotalDestroyed;

            foreach (int index in new[] { 0, 999, 10, 900, 500 })
            {
                _virtual.ScrollToIndex(index);
                _virtual.Refresh();
            }

            Assert.That(_virtual.TotalCreated, Is.EqualTo(created));
            Assert.That(_virtual.TotalDestroyed, Is.EqualTo(destroyed));
            Assert.That(_virtual.CountOwned, Is.LessThanOrEqualTo(15));
            Assert.That(_virtual.CountActive, Is.GreaterThan(0));
            AssertVisibleWindow(1000);
        }

        [Test]
        public void InvalidCountDoesNotReplaceValidLogicalState()
        {
            ConfigureDefault();
            _virtual.SetCount(100);
            _virtual.Refresh();
            int owned = _virtual.CountOwned;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _virtual.SetCount(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _virtual.ScrollToIndex(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _virtual.ScrollToIndex(100));
            Assert.That(_virtual.Count, Is.EqualTo(100));
            Assert.That(_virtual.CountOwned, Is.EqualTo(owned));
        }

        [Test]
        public void InvalidConfigurationIsRejectedBeforeReplacingWorkingState()
        {
            ConfigureDefault(verifyReentry: true);
            _virtual.SetCount(100);
            _virtual.Refresh();
            int owned = _virtual.CountOwned;

            foreach (float height in new[] { 0, -1, float.NaN, float.PositiveInfinity })
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                    _virtual.Configure(_scrollRect, _prefab, height, _ => { }));
            }
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }, overscan: -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }, spacing: float.NaN));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }, spacing: -1));
            Vector2 anchorMin = _content.anchorMin;
            _content.anchorMin = new Vector2(0, 0.5f);
            Assert.Catch<System.ArgumentException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }));
            _content.anchorMin = anchorMin;
            Vector2 pivot = _prefab.pivot;
            _prefab.pivot = new Vector2(0.5f, 0.5f);
            Assert.Catch<System.ArgumentException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }));
            _prefab.pivot = pivot;
            _scrollRect.horizontal = true;
            Assert.Catch<System.ArgumentException>(() =>
                _virtual.Configure(_scrollRect, _prefab, 20, _ => { }));
            _scrollRect.horizontal = false;

            Assert.That(_virtual.Count, Is.EqualTo(100));
            Assert.That(_virtual.CountOwned, Is.EqualTo(owned));
        }

        [Test]
        public void LayoutGroupOnContentIsRejectedBeforeReplacingWorkingState()
        {
            ConfigureDefault();
            _virtual.SetCount(100);
            _virtual.Refresh();
            int owned = _virtual.CountOwned;
            _content.gameObject.AddComponent<VerticalLayoutGroup>();

            Assert.Throws<System.ArgumentException>(() => _virtual.Configure(_scrollRect, _prefab, 20,
                binding => { }, overscan: 2));

            Assert.That(_virtual.Count, Is.EqualTo(100));
            Assert.That(_virtual.CountOwned, Is.EqualTo(owned));
        }

        private void ConfigureDefault(bool verifyReentry = false)
        {
            bool checkedReentry = false;
            bool checkedUnbindReentry = false;
            _virtual.Configure(_scrollRect, _prefab, 20,
                binding =>
                {
                    _bound.Add(binding);
                    if (verifyReentry && !checkedReentry)
                    {
                        checkedReentry = true;
                        Assert.Throws<System.InvalidOperationException>(_virtual.Refresh);
                        Assert.Throws<System.InvalidOperationException>(() => _virtual.SetCount(0));
                        Assert.Throws<System.InvalidOperationException>(() => _virtual.ScrollToIndex(0));
                        Assert.Throws<System.InvalidOperationException>(() =>
                            _virtual.Configure(_scrollRect, _prefab, 20, _ => { }));
                    }
                    binding.View.GetComponent<Text>().text = binding.Index.ToString();
                },
                binding =>
                {
                    Assert.That(binding.LifetimeToken.IsCancellationRequested, Is.True);
                    Assert.That(binding.IsCurrent, Is.False);
                    if (verifyReentry && !checkedUnbindReentry)
                    {
                        checkedUnbindReentry = true;
                        Assert.Throws<System.InvalidOperationException>(_virtual.Refresh);
                        Assert.Throws<System.InvalidOperationException>(() => _virtual.SetCount(0));
                        Assert.Throws<System.InvalidOperationException>(() => _virtual.ScrollToIndex(0));
                        Assert.Throws<System.InvalidOperationException>(() =>
                            _virtual.Configure(_scrollRect, _prefab, 20, _ => { }));
                    }
                    _unbound.Add(binding);
                }, overscan: 2, spacing: 2.5f);
        }
        private void AssertVisibleWindow(int count)
        {
            const float height = 20;
            const float spacing = 2.5f;
            const float stride = height + spacing;
            float offset = _content.anchoredPosition.y;
            float viewportHeight = _scrollRect.viewport.rect.height;
            var current = _bound.Where(binding => binding.IsCurrent).ToArray();
            var indexes = current.Select(binding => binding.Index).ToArray();

            Assert.That(indexes.Distinct().Count(), Is.EqualTo(indexes.Length), "A logical row must have one active binding.");
            Assert.That(current.Length, Is.EqualTo(_virtual.CountActive));
            Assert.That(_content.GetComponentsInChildren<Text>(false).Length, Is.EqualTo(current.Length));
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

        private static GameObject CreateScrollRect(int viewportHeight, out ScrollRect scrollRect, out RectTransform content)
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scrollObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(root.transform, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = new Vector2(200, viewportHeight);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            return root;
        }

        private static RectTransform CreateCellPrefab()
        {
            var prefab = new GameObject("CellPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var text = prefab.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            prefab.SetActive(false);
            var rect = prefab.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 20);
            return prefab.GetComponent<RectTransform>();
        }
    }
}
