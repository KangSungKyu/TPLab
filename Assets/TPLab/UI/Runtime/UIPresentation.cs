using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.UI
{
    // Native state belongs to the clone, so a reused generation retains its own visibility mask and baselines.
    internal sealed class UIPresentation
    {
        internal readonly GameObject View;
        internal readonly GameObject Root;
        internal readonly UIHideStrategy HideStrategy;
        private readonly Canvas _canvas;
        private readonly CanvasGroup _visibility;
        private readonly Dictionary<GraphicRaycaster, bool> _raycasters = new Dictionary<GraphicRaycaster, bool>();

        internal UIPresentation(GameObject view, UIHideStrategy hideStrategy)
        {
            View = view;
            Root = view;
            HideStrategy = hideStrategy;
            _canvas = view.GetComponent<Canvas>();
            // CanvasGroup permits one component per GameObject. Keep every project component intact.
            Root = new GameObject("UIContext Visibility", typeof(RectTransform));
            try
            {
                Root.transform.SetParent(view.transform.parent, false);
                var wrapper = (RectTransform)Root.transform;
                wrapper.anchorMin = Vector2.zero;
                wrapper.anchorMax = Vector2.one;
                wrapper.pivot = new Vector2(0.5f, 0.5f);
                wrapper.sizeDelta = Vector2.zero;
                wrapper.anchoredPosition3D = Vector3.zero;
                var rect = view.transform as RectTransform;
                Vector3 anchoredPosition = rect != null ? rect.anchoredPosition3D : Vector3.zero;
                view.transform.SetParent(wrapper, false);
                if (rect != null)
                {
                    rect.anchoredPosition3D = anchoredPosition;
                }
                _visibility = Root.AddComponent<CanvasGroup>();
                _visibility.ignoreParentGroups = false;
                CaptureRaycasters();
            }
            catch
            {
                // Until construction returns, the handle only tracks the clone. Retire this partial wrapper too.
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(Root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
                throw;
            }
        }

        internal void RestoreLayout(GameObject prefab)
        {
            View.transform.localPosition = prefab.transform.localPosition;
            View.transform.localRotation = prefab.transform.localRotation;
            View.transform.localScale = prefab.transform.localScale;
            if (View.transform is RectTransform rect && prefab.transform is RectTransform source)
            {
                rect.anchorMin = source.anchorMin;
                rect.anchorMax = source.anchorMax;
                rect.pivot = source.pivot;
                rect.sizeDelta = source.sizeDelta;
                // Anchor and pivot changes affect the position; restore the authored position last.
                rect.anchoredPosition3D = source.anchoredPosition3D;
            }
        }

        internal void MoveTo(Transform container)
        {
            if (Root.transform.parent == container)
            {
                return;
            }
            var rect = Root.transform as RectTransform;
            Vector3 anchoredPosition = rect != null ? rect.anchoredPosition3D : Vector3.zero;
            Root.transform.SetParent(container, false);
            if (rect != null)
            {
                rect.anchoredPosition3D = anchoredPosition;
            }
        }

        internal void Show()
        {
            if (HideStrategy == UIHideStrategy.DisableCanvasRendering)
            {
                CaptureRaycasters();
                _canvas.enabled = true;
                foreach (var entry in _raycasters)
                {
                    if (entry.Key != null)
                    {
                        entry.Key.enabled = entry.Value;
                    }
                }
            }
            _visibility.alpha = 1f;
            SetInputEnabled(false);
            View.SetActive(true);
        }

        internal void Hide()
        {
            SetInputEnabled(false);
            if (HideStrategy == UIHideStrategy.DeactivateView)
            {
                View.SetActive(false);
                return;
            }
            // Disabled child Canvases can send Graphics to an enabled ancestor Canvas.
            // This additional owned group masks that fallback without altering project groups or the host.
            _visibility.alpha = 0f;
            _visibility.blocksRaycasts = false;
            _visibility.interactable = false;
            CaptureRaycasters();
            _canvas.enabled = false;
            foreach (var entry in _raycasters)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = false;
                }
            }
        }

        internal void SetInputEnabled(bool enabled)
        {
            _visibility.blocksRaycasts = enabled;
            _visibility.interactable = enabled;
        }

        private void CaptureRaycasters()
        {
            foreach (var raycaster in View.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                if (!_raycasters.ContainsKey(raycaster))
                {
                    _raycasters.Add(raycaster, raycaster.enabled);
                }
            }
        }

        internal readonly struct Plane
        {
            internal readonly Canvas Canvas;
            internal readonly Canvas Root;
            internal readonly Transform Host;
            internal readonly bool HasGraphics;

            internal Plane(Canvas canvas, Canvas root, Transform host, bool hasGraphics)
            {
                Canvas = canvas;
                Root = root;
                Host = host;
                HasGraphics = hasGraphics;
            }
        }

        internal static Plane Validate(GameObject view, Transform host, UIHideStrategy hideStrategy)
        {
            if (view == null || host == null)
            {
                throw new InvalidOperationException("A live view source and physical host are required.");
            }
            Canvas ownedCanvas = view.GetComponent<Canvas>();
            foreach (var canvas in view.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.overrideSorting)
                {
                    throw new InvalidOperationException("Unmanaged prefab overrideSorting can bypass the managed order and visibility mask.");
                }
            }
            if (hideStrategy == UIHideStrategy.DisableCanvasRendering)
            {
                if (ownedCanvas == null)
                {
                    throw new InvalidOperationException("Renderer-only hiding requires a dedicated Canvas on the owned clone root.");
                }
            }
            foreach (var group in view.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (group.ignoreParentGroups)
                {
                    throw new InvalidOperationException("ignoreParentGroups can bypass the owned input and rendering mask.");
                }
            }
            bool hasGraphics = view.GetComponentsInChildren<Graphic>(true).Length != 0;
            Canvas closest = FindEnabledCanvas(host);
            if (closest == null)
            {
                closest = ownedCanvas;
            }
            Canvas effective = closest;
            while (effective != null && !effective.overrideSorting)
            {
                Canvas parent = FindEnabledCanvas(effective.transform.parent);
                if (parent == null)
                {
                    break;
                }
                effective = parent;
            }
            Canvas root = closest;
            while (root != null)
            {
                Canvas parent = FindEnabledCanvas(root.transform.parent);
                if (parent == null)
                {
                    break;
                }
                root = parent;
            }
            if (hasGraphics)
            {
                if (!host.gameObject.activeInHierarchy || effective == null || root == null)
                {
                    throw new InvalidOperationException("UI Graphics require an active physical host and an effective Canvas.");
                }
                if (root.renderMode == RenderMode.WorldSpace
                    || root.renderMode == RenderMode.ScreenSpaceCamera && root.worldCamera == null)
                {
                    throw new InvalidOperationException("This layout cannot express a managed screen order with an explicit camera.");
                }
            }
            return new Plane(effective, root, host, hasGraphics);
        }

        private static Canvas FindEnabledCanvas(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && canvas.enabled && current.gameObject.activeInHierarchy)
                {
                    return canvas;
                }
            }
            return null;
        }

        internal static int CompareFixedOrder(Plane left, Plane right)
        {
            if (left.Root.renderMode != right.Root.renderMode
                || left.Root.targetDisplay != right.Root.targetDisplay
                || left.Root.renderMode != RenderMode.ScreenSpaceOverlay && left.Root.worldCamera != right.Root.worldCamera)
            {
                throw new InvalidOperationException("Managed UI planes must share render mode, camera, and target display.");
            }
            if (left.Canvas != right.Canvas)
            {
                int layers = SortingLayer.GetLayerValueFromID(left.Canvas.sortingLayerID)
                    .CompareTo(SortingLayer.GetLayerValueFromID(right.Canvas.sortingLayerID));
                int order = layers != 0 ? layers : left.Canvas.sortingOrder.CompareTo(right.Canvas.sortingOrder);
                if (order == 0)
                {
                    throw new InvalidOperationException("Separate managed Canvas planes need an unambiguous sorting layer/order.");
                }
                return order;
            }
            if (left.Host == right.Host)
            {
                return 0;
            }
            if (left.Host.IsChildOf(right.Host) || right.Host.IsChildOf(left.Host))
            {
                throw new InvalidOperationException("Overlapping fixed host containers cannot express an independent managed order.");
            }
            Transform leftBranch = left.Host;
            Transform rightBranch = right.Host;
            for (Transform ancestor = left.Host.parent; ancestor != null; ancestor = ancestor.parent)
            {
                if (!right.Host.IsChildOf(ancestor))
                {
                    continue;
                }
                while (leftBranch.parent != ancestor)
                {
                    leftBranch = leftBranch.parent;
                }
                while (rightBranch.parent != ancestor)
                {
                    rightBranch = rightBranch.parent;
                }
                return leftBranch.GetSiblingIndex().CompareTo(rightBranch.GetSiblingIndex());
            }
            throw new InvalidOperationException("Fixed hosts do not share a comparable Canvas hierarchy.");
        }
    }
}
