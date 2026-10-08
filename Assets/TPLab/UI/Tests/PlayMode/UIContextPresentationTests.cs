using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TPLab.UI.Tests.PlayMode
{
    public sealed class UIContextPresentationTests
    {
        private GameObject _root;
        private GameObject _source;
        private GameObject _host;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<UIContext> _contexts = new List<UIContext>();
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();
        private readonly List<UniTaskCompletionSource<GameObject>> _loads = new List<UniTaskCompletionSource<GameObject>>();

        [SetUp]
        public void SetUp()
        {
            _root = Own("UIPresentationPlayOwner");
            _host = CanvasHost("BorrowedPresentationCanvas", 10);
            _source = Source("UIPresentationSource");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var load in _loads)
            {
                load.TrySetResult(_source);
            }
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            foreach (var context in _contexts)
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception)
                {
                    // Expected callback failures are observed by tests; native fixture cleanup remains required.
                }
            }
            for (int index = _objects.Count - 1; index >= 0; --index)
            {
                UnityEngine.Object.Destroy(_objects[index]);
            }
            _loads.Clear();
            _gates.Clear();
            _contexts.Clear();
            _objects.Clear();
            yield return null;
        }

        private GameObject Own(string name, params Type[] components)
        {
            var value = new GameObject(name, components);
            _objects.Add(value);
            return value;
        }

        private GameObject CanvasHost(string name, int order)
        {
            var value = Own(name, typeof(RectTransform));
            var canvas = value.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            value.AddComponent<GraphicRaycaster>();
            return value;
        }

        private GameObject Source(string name, bool dedicatedCanvas = false)
        {
            var value = Own(name, typeof(RectTransform));
            value.SetActive(false);
            var rect = (RectTransform)value.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
            value.AddComponent<UIContextLifecycleProbe>();
            value.AddComponent<CanvasGroup>();
            if (dedicatedCanvas)
            {
                value.AddComponent<Canvas>();
                value.AddComponent<GraphicRaycaster>();
            }
            value.AddComponent<Image>();
            return value;
        }

        private UIContext Create(Func<string, CancellationToken, UniTask<GameObject>> provider = null,
            bool registerSharedHost = true)
        {
            var context = new UIContext(_root, loadPrefab: provider);
            _contexts.Add(context);
            if (registerSharedHost)
            {
                context.RegisterHost("shared", _host.transform);
            }
            return context;
        }

        private void Register(UIContext context, string id, UIRole role = UIRole.Popup,
            UIRetention retention = UIRetention.DestroyOnClose)
        {
            context.Register(new UIDefinition(id, _source, role: role, hostId: "shared", retention: retention));
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private UniTaskCompletionSource<GameObject> LoadGate()
        {
            var gate = new UniTaskCompletionSource<GameObject>();
            _loads.Add(gate);
            return gate;
        }

        private static async UniTask Wait(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True, "Public completion did not settle within 100 frames.");
        }

        private static async UniTask Complete(UniTask operation)
        {
            var task = operation.AsTask();
            await Wait(task);
            await task;
        }

        private static async UniTask<UIHandle> Open(UIContext context, UIOpenRequest request)
        {
            var task = context.OpenAsync(request).AsTask();
            await Wait(task);
            return await task;
        }

        private static async UniTask<UIHandle> Select(UIContext context, UIOpenRequest request)
        {
            var task = context.SelectHudAsync(request).AsTask();
            await Wait(task);
            return await task;
        }

        private static async UniTask ExpectError(Task task, string message)
        {
            await Wait(task);
            Exception failure = null;
            try
            {
                await task;
            }
            catch (Exception error)
            {
                failure = error;
            }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain(message));
        }

        private static async UniTask ExpectCancellation(Task task)
        {
            await Wait(task);
            bool cancelled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True, "Await must propagate cancellation rather than successful completion.");
        }

        private static async UniTask ExpectInvalid(Func<UniTask> operation)
        {
            Exception failure = null;
            try
            {
                await Complete(operation());
            }
            catch (Exception error)
            {
                failure = error;
            }
            Assert.That(failure, Is.InstanceOf<InvalidOperationException>());
        }

        private static bool HitsView(EventSystem eventSystem, GameObject view)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventSystem)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            foreach (var result in results)
            {
                if (result.gameObject != null && (result.gameObject == view
                    || result.gameObject.transform.IsChildOf(view.transform)))
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator ParentCloseCancelsPendingChildAndCompletesChildFirst()
        {
            return CheckPendingChildClose().ToCoroutine();
        }

        private async UniTask CheckPendingChildClose()
        {
            var load = LoadGate();
            var context = Create((_, __) => load.Task);
            Register(context, "parent");
            context.Register(new UIDefinition("child", assetKey: "ui/child", hostId: "shared"));
            var trace = new List<string>();
            UIHandle child = null;
            var parent = await Open(context, new UIOpenRequest("parent", hooks: new UIHooks
            {
                CloseAsync = (_, __) =>
                {
                    Assert.That(child.State, Is.EqualTo(UIState.Closed));
                    Assert.That(ReferenceEquals(child.ViewObject, null), Is.True);
                    trace.Add("parent-close");
                    return UniTask.CompletedTask;
                },
                Closed = _ => trace.Add("parent-closed")
            }));
            parent.RegisterCleanup(() => trace.Add("parent-cleanup"));
            child = context.BeginOpen(new UIOpenRequest("child", parent: parent, hooks: new UIHooks
            {
                Closed = _ => trace.Add("child-closed")
            }));
            child.RegisterCleanup(() => trace.Add("child-cleanup"));
            var childOpened = child.Opened.AsTask();
            Assert.That(child.State, Is.EqualTo(UIState.Opening));
            await Complete(parent.CloseAsync());
            await ExpectCancellation(childOpened);
            CollectionAssert.AreEqual(new[]
            {
                "child-cleanup", "child-closed", "parent-close", "parent-cleanup", "parent-closed"
            }, trace);
            Assert.That(child.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(parent.State, Is.EqualTo(UIState.Closed));
            load.TrySetResult(_source);
            await UniTask.NextFrame();
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_host.GetComponentsInChildren<Image>(true), Is.Empty);
            Assert.That(_source != null && _host != null, Is.True);
        }

        [UnityTest]
        public IEnumerator MiddleClosePreservesIndependentPopupAndClosesOwnedChild()
        {
            return CheckMiddleClose().ToCoroutine();
        }

        private async UniTask CheckMiddleClose()
        {
            var context = Create();
            foreach (string id in new[] { "a", "b", "c" })
            {
                Register(context, id);
            }
            for (int ownedChild = 0; ownedChild < 2; ++ownedChild)
            {
                var a = await Open(context, new UIOpenRequest("a"));
                var b = await Open(context, new UIOpenRequest("b"));
                var c = await Open(context, new UIOpenRequest("c", parent: ownedChild == 1 ? b : null));
                var cView = c.ViewObject;
                Assert.That(a.ViewObject.transform.GetSiblingIndex(), Is.LessThan(b.ViewObject.transform.GetSiblingIndex()));
                Assert.That(b.ViewObject.transform.GetSiblingIndex(), Is.LessThan(cView.transform.GetSiblingIndex()));
                await Complete(b.CloseAsync());
                Assert.That(a.State, Is.EqualTo(UIState.Visible));
                Assert.That(a.ViewObject.activeInHierarchy, Is.True);
                Assert.That(b.State, Is.EqualTo(UIState.Closed));
                if (ownedChild == 0)
                {
                    Assert.That(c.State, Is.EqualTo(UIState.Visible));
                    Assert.That(cView != null && cView.activeInHierarchy, Is.True);
                    Assert.That(a.ViewObject.transform.GetSiblingIndex(), Is.LessThan(cView.transform.GetSiblingIndex()));
                    await Complete(c.CloseAsync());
                }
                else
                {
                    Assert.That(c.State, Is.EqualTo(UIState.Closed));
                    Assert.That(cView == null, Is.True);
                }
                await Complete(a.CloseAsync());
            }
            await Complete(context.ShutdownAsync());
            Assert.That(_host != null && _host.GetComponent<Canvas>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator AcceptedOrderSurvivesReverseLoadCompletionOnSharedCanvas()
        {
            return CheckAcceptedOrder().ToCoroutine();
        }

        private async UniTask CheckAcceptedOrder()
        {
            var loadA = LoadGate();
            var loadB = LoadGate();
            var context = Create((key, _) => key == "ui/a" ? loadA.Task : loadB.Task);
            var unmanaged = Own("BorrowedUnmanagedSibling", typeof(RectTransform));
            unmanaged.transform.SetParent(_host.transform, false);
            int unmanagedIndex = unmanaged.transform.GetSiblingIndex();
            int hostOrder = _host.GetComponent<Canvas>().sortingOrder;
            context.Register(new UIDefinition("a", assetKey: "ui/a", hostId: "shared"));
            context.Register(new UIDefinition("b", assetKey: "ui/b", hostId: "shared"));
            var a = context.BeginOpen(new UIOpenRequest("a"));
            var b = context.BeginOpen(new UIOpenRequest("b"));
            var aOpened = a.Opened.AsTask();
            var bOpened = b.Opened.AsTask();
            Assert.That(a.Id, Is.LessThan(b.Id));
            loadB.TrySetResult(_source);
            await Wait(bOpened);
            await bOpened;
            Assert.That(a.State, Is.EqualTo(UIState.Opening));
            loadA.TrySetResult(_source);
            await Wait(aOpened);
            await aOpened;
            Assert.That(a.ViewObject.transform.parent, Is.SameAs(_host.transform));
            Assert.That(b.ViewObject.transform.parent, Is.SameAs(_host.transform));
            int aIndex = a.ViewObject.transform.GetSiblingIndex();
            int bIndex = b.ViewObject.transform.GetSiblingIndex();
            Assert.That(aIndex, Is.LessThan(bIndex), "Load completion may not reverse accepted depth.");
            Assert.That(unmanaged.transform.GetSiblingIndex(), Is.EqualTo(unmanagedIndex));
            for (int frame = 0; frame < 3; ++frame)
            {
                await UniTask.NextFrame();
                Assert.That(a.ViewObject.transform.GetSiblingIndex(), Is.EqualTo(aIndex));
                Assert.That(b.ViewObject.transform.GetSiblingIndex(), Is.EqualTo(bIndex));
            }
            Assert.That(_host.GetComponent<Canvas>().sortingOrder, Is.EqualTo(hostOrder));
            await Complete(context.ShutdownAsync());
            Assert.That(unmanaged != null && _host != null && _source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator ThreeAcceptedRootsKeepInterleavedBorrowedSiblingSlots()
        {
            return CheckThreeRootOrder().ToCoroutine();
        }

        private async UniTask CheckThreeRootOrder()
        {
            var loads = new[] { LoadGate(), LoadGate(), LoadGate() };
            var context = Create((key, _) => loads[int.Parse(key)].Task);
            var handles = new UIHandle[3];
            for (int index = 0; index < handles.Length; ++index)
            {
                context.Register(new UIDefinition("ordered-" + index, assetKey: index.ToString(), hostId: "shared"));
                handles[index] = context.BeginOpen(new UIOpenRequest("ordered-" + index));
            }
            var borrowed = new Transform[3];
            for (int index = 2; index >= 0; --index)
            {
                borrowed[2 - index] = Own("InterleavedBorrowed-" + index, typeof(RectTransform)).transform;
                borrowed[2 - index].SetParent(_host.transform, false);
                loads[index].TrySetResult(_source);
                await Complete(handles[index].Opened);
            }
            for (int index = 0; index < 3; ++index)
            {
                Assert.That(borrowed[index].GetSiblingIndex(), Is.EqualTo(index * 2));
                Assert.That(handles[index].ViewObject.transform.GetSiblingIndex(), Is.EqualTo(index * 2 + 1));
            }
            // A project native change may disorder managed roots. The next managed open reapplies
            // its complete order without exchanging a borrowed sibling's final slot.
            handles[2].ViewObject.transform.SetSiblingIndex(1);
            borrowed[1].SetSiblingIndex(2);
            handles[0].ViewObject.transform.SetSiblingIndex(3);
            borrowed[2].SetSiblingIndex(4);
            handles[1].ViewObject.transform.SetSiblingIndex(5);
            Register(context, "fourth");
            var fourth = await Open(context, new UIOpenRequest("fourth"));
            for (int index = 0; index < 3; ++index)
            {
                Assert.That(borrowed[index].GetSiblingIndex(), Is.EqualTo(index * 2));
                Assert.That(handles[index].ViewObject.transform.GetSiblingIndex(), Is.EqualTo(index * 2 + 1));
            }
            Assert.That(fourth.ViewObject.transform.GetSiblingIndex(), Is.EqualTo(6));
            await Complete(context.ShutdownAsync());
            Assert.That(borrowed[0] != null && borrowed[1] != null && borrowed[2] != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RendererOnlyRejectsVisibilityBypassBeforeMutatingExistingView()
        {
            return CheckVisibilityBypass().ToCoroutine();
        }

        private async UniTask CheckVisibilityBypass()
        {
            var context = Create();
            Register(context, "existing");
            var existing = await Open(context, new UIOpenRequest("existing"));
            int sibling = existing.ViewObject.transform.GetSiblingIndex();
            int childCount = _host.transform.childCount;
            var unsafeGroup = Source("IgnoreParentGroupSource", dedicatedCanvas: true);
            unsafeGroup.GetComponent<CanvasGroup>().ignoreParentGroups = true;
            var unsafeCanvas = Source("OverrideSortingSource", dedicatedCanvas: true);
            var sourceStage = CanvasHost("VisibilityBypassFixtureStage", 0);
            unsafeCanvas.transform.SetParent(sourceStage.transform, false);
            unsafeCanvas.SetActive(true);
            unsafeCanvas.GetComponent<Canvas>().overrideSorting = true;
            Assert.That(unsafeCanvas.GetComponent<Canvas>().overrideSorting, Is.True,
                "Root Canvas ignores the setter; this fixture must author a real nested override.");
            unsafeCanvas.SetActive(false);
            var sources = new[] { unsafeGroup, unsafeCanvas };
            for (int index = 0; index < sources.Length; ++index)
            {
                string id = "unsafe-" + index;
                context.Register(new UIDefinition(id, sources[index], hostId: "shared",
                    hideStrategy: UIHideStrategy.DisableCanvasRendering));
                await ExpectInvalid(async () => { await context.OpenAsync(new UIOpenRequest(id)); });
                Assert.That(existing.State, Is.EqualTo(UIState.Visible));
                Assert.That(existing.ViewObject.activeInHierarchy, Is.True);
                Assert.That(existing.ViewObject.transform.GetSiblingIndex(), Is.EqualTo(sibling));
                Assert.That(_host.transform.childCount, Is.EqualTo(childCount));
                Assert.That(context.Fault, Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator SeparateCanvasOrderMatchesLogicalOrderAndRejectsUnsupportedLayouts()
        {
            return CheckCanvasLayouts().ToCoroutine();
        }

        private async UniTask CheckCanvasLayouts()
        {
            var context = Create(registerSharedHost: false);
            var lower = CanvasHost("BorrowedLowerCanvas", 10);
            var higher = CanvasHost("BorrowedHigherCanvas", 20);
            var lowerCanvas = lower.GetComponent<Canvas>();
            var higherCanvas = higher.GetComponent<Canvas>();
            var layers = SortingLayer.layers;
            lowerCanvas.sortingLayerID = layers[0].id;
            higherCanvas.sortingLayerID = layers[layers.Length - 1].id;
            int lowerLayer = lowerCanvas.sortingLayerID;
            int higherLayer = higherCanvas.sortingLayerID;
            context.RegisterHost("lower", lower.transform);
            context.RegisterHost("higher", higher.transform);
            context.Register(new UIDefinition("a", _source, hostId: "lower"));
            context.Register(new UIDefinition("b", _source, hostId: "higher"));
            var a = await Open(context, new UIOpenRequest("a"));
            var b = await Open(context, new UIOpenRequest("b", parent: a));
            int lowerValue = SortingLayer.GetLayerValueFromID(lowerLayer);
            int higherValue = SortingLayer.GetLayerValueFromID(higherLayer);
            Assert.That(lowerValue < higherValue || lowerValue == higherValue
                && lowerCanvas.sortingOrder < higherCanvas.sortingOrder, Is.True);
            await ExpectInvalid(async () =>
            {
                context.Register(new UIDefinition("backward-child", _source, hostId: "lower"));
                await context.OpenAsync(new UIOpenRequest("backward-child", parent: b));
            });
            await ExpectInvalid(async () =>
            {
                context.Register(new UIDefinition("backward-independent", _source, hostId: "lower"));
                await context.OpenAsync(new UIOpenRequest("backward-independent"));
            });
            var cameraObject = Own("BorrowedOtherCamera", typeof(Camera));
            var wrongMode = CanvasHost("BorrowedCameraCanvas", 30);
            wrongMode.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
            wrongMode.GetComponent<Canvas>().worldCamera = cameraObject.GetComponent<Camera>();
            await ExpectInvalid(async () =>
            {
                context.RegisterHost("wrong-mode", wrongMode.transform);
                context.Register(new UIDefinition("wrong-mode-view", _source, hostId: "wrong-mode"));
                await context.OpenAsync(new UIOpenRequest("wrong-mode-view"));
            });
            var unsafeSource = Source("UnmanagedOverrideSource");
            var overrideChild = Own("UnmanagedOverrideChild", typeof(RectTransform));
            overrideChild.transform.SetParent(unsafeSource.transform, false);
            var overrideCanvas = overrideChild.AddComponent<Canvas>();
            var overrideStage = CanvasHost("OverrideFixtureStage", 0);
            unsafeSource.transform.SetParent(overrideStage.transform, false);
            unsafeSource.SetActive(true);
            overrideCanvas.overrideSorting = true;
            Assert.That(overrideCanvas.overrideSorting, Is.True);
            overrideCanvas.sortingOrder = 999;
            overrideChild.AddComponent<GraphicRaycaster>();
            overrideChild.AddComponent<Image>();
            unsafeSource.SetActive(false);
            Assert.That(overrideCanvas.overrideSorting, Is.True);
            await ExpectInvalid(async () =>
            {
                context.Register(new UIDefinition("unmanaged-override", unsafeSource, hostId: "higher"));
                await context.OpenAsync(new UIOpenRequest("unmanaged-override"));
            });
            Assert.That(a.State, Is.EqualTo(UIState.Visible));
            Assert.That(b.State, Is.EqualTo(UIState.Visible));
            Assert.That(a.ViewObject.activeInHierarchy && b.ViewObject.activeInHierarchy, Is.True);
            Assert.That(lowerCanvas.sortingLayerID, Is.EqualTo(lowerLayer));
            Assert.That(higherCanvas.sortingLayerID, Is.EqualTo(higherLayer));
            Assert.That(lowerCanvas.sortingOrder, Is.EqualTo(10));
            Assert.That(higherCanvas.sortingOrder, Is.EqualTo(20));
            Assert.That(wrongMode.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(overrideCanvas.sortingOrder, Is.EqualTo(999));
            Assert.That(context.Fault, Is.Null, "Configuration rejection is not a native state-application fault.");
            var fixedHosts = Create(registerSharedHost: false);
            var left = Own("BorrowedFixedLowerHost", typeof(RectTransform));
            var right = Own("BorrowedFixedUpperHost", typeof(RectTransform));
            left.transform.SetParent(_host.transform, false);
            right.transform.SetParent(_host.transform, false);
            int leftIndex = left.transform.GetSiblingIndex();
            int rightIndex = right.transform.GetSiblingIndex();
            fixedHosts.RegisterHost("left", left.transform);
            fixedHosts.RegisterHost("right", right.transform);
            fixedHosts.Register(new UIDefinition("left-a", _source, hostId: "left"));
            fixedHosts.Register(new UIDefinition("right-b", _source, hostId: "right"));
            var first = await Open(fixedHosts, new UIOpenRequest("left-a"));
            var second = await Open(fixedHosts, new UIOpenRequest("right-b"));
            await ExpectInvalid(async () =>
            {
                fixedHosts.Register(new UIDefinition("left-c", _source, hostId: "left"));
                await fixedHosts.OpenAsync(new UIOpenRequest("left-c"));
            });
            Assert.That(first.State, Is.EqualTo(UIState.Visible));
            Assert.That(second.State, Is.EqualTo(UIState.Visible));
            Assert.That(left.transform.GetSiblingIndex(), Is.EqualTo(leftIndex));
            Assert.That(right.transform.GetSiblingIndex(), Is.EqualTo(rightIndex));
            Assert.That(first.ViewObject.transform.parent, Is.SameAs(left.transform));
            Assert.That(second.ViewObject.transform.parent, Is.SameAs(right.transform));
            Assert.That(fixedHosts.Fault, Is.Null);
            await Complete(context.ShutdownAsync());
            await Complete(fixedHosts.ShutdownAsync());
            Assert.That(lower != null && higher != null && left != null && right != null, Is.True);
        }

        [UnityTest]
        public IEnumerator HudPrepareFailurePreservesCurrentHudAndChildren()
        {
            return CheckHudPreparationFailure().ToCoroutine();
        }

        private async UniTask CheckHudPreparationFailure()
        {
            var load = LoadGate();
            int providerCalls = 0;
            var context = Create((_, __) => ++providerCalls == 1 ? load.Task : UniTask.FromResult(_source));
            Register(context, "old", UIRole.Hud);
            Register(context, "child");
            context.Register(new UIDefinition("next", assetKey: "ui/next-hud", role: UIRole.Hud, hostId: "shared"));
            var old = await Select(context, new UIOpenRequest("old"));
            var child = await Open(context, new UIOpenRequest("child", parent: old));
            var oldView = old.ViewObject;
            var childView = child.ViewObject;
            var failedLoad = context.SelectHudAsync(new UIOpenRequest("next")).AsTask();
            Assert.That(failedLoad.IsCompleted, Is.False);
            Assert.That(context.CurrentHud, Is.SameAs(old));
            load.TrySetException(new InvalidOperationException("expected-hud-asset-failure"));
            await ExpectError(failedLoad, "expected-hud-asset-failure");
            UIHandle candidate = null;
            GameObject candidateView = null;
            int cleanupCount = 0;
            var failedPrepare = context.SelectHudAsync(new UIOpenRequest("next", hooks: new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    candidate = handle;
                    candidateView = handle.ViewObject;
                    Assert.That(candidateView.activeInHierarchy, Is.False);
                    handle.RegisterCleanup(() => ++cleanupCount);
                    throw new InvalidOperationException("expected-hud-prepare-failure");
                }
            })).AsTask();
            await ExpectError(failedPrepare, "expected-hud-prepare-failure");
            Assert.That(providerCalls, Is.EqualTo(2), "Only the explicit retry requests the failed asset again.");
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.State, Is.EqualTo(UIState.Closed));
            Assert.That(candidateView == null, Is.True);
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(context.CurrentHud, Is.SameAs(old));
            Assert.That(old.State, Is.EqualTo(UIState.Visible));
            Assert.That(child.State, Is.EqualTo(UIState.Visible));
            Assert.That(old.ViewObject, Is.SameAs(oldView));
            Assert.That(child.ViewObject, Is.SameAs(childView));
            Assert.That(oldView.activeInHierarchy && childView.activeInHierarchy, Is.True);
            Assert.That(context.Fault, Is.Null);
        }

        [UnityTest]
        public IEnumerator HudSwitchClosesOwnedChildrenAndPreservesIndependentPopup()
        {
            return CheckHudReplacement().ToCoroutine();
        }

        private async UniTask CheckHudReplacement()
        {
            var context = Create();
            Register(context, "old", UIRole.Hud);
            Register(context, "next", UIRole.Hud);
            Register(context, "child");
            Register(context, "independent");
            var closeGate = Gate();
            var prepareGate = Gate();
            var trace = new List<string>();
            UIHandle child = null;
            var old = await Select(context, new UIOpenRequest("old", hooks: new UIHooks
            {
                CloseAsync = (_, __) =>
                {
                    Assert.That(child.State, Is.EqualTo(UIState.Closed));
                    trace.Add("old-close");
                    return UniTask.CompletedTask;
                },
                Closed = _ => trace.Add("old-closed")
            }));
            child = await Open(context, new UIOpenRequest("child", parent: old, hooks: new UIHooks
            {
                CloseAsync = (_, token) => closeGate.Task.AttachExternalCancellation(token),
                Closed = _ => trace.Add("child-closed")
            }));
            var independent = await Open(context, new UIOpenRequest("independent"));
            UIHandle candidate = null;
            var selection = context.SelectHudAsync(new UIOpenRequest("next", hooks: new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    candidate = handle;
                    Assert.That(old.State, Is.EqualTo(UIState.Visible));
                    Assert.That(child.State, Is.EqualTo(UIState.Visible));
                    trace.Add("next-prepare");
                    return prepareGate.Task.AttachExternalCancellation(token);
                },
                OpenAsync = (_, __) =>
                {
                    Assert.That(old.State, Is.EqualTo(UIState.Closed));
                    Assert.That(child.State, Is.EqualTo(UIState.Closed));
                    trace.Add("next-open");
                    return UniTask.CompletedTask;
                }
            })).AsTask();
            for (int frame = 0; frame < 100 && candidate == null && !selection.IsCompleted; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.ViewObject.activeInHierarchy, Is.False);
            Assert.That(context.CurrentHud, Is.SameAs(old));
            prepareGate.TrySetResult();
            for (int frame = 0; frame < 100 && child.State != UIState.Closing; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(child.State, Is.EqualTo(UIState.Closing));
            Assert.That(selection.IsCompleted, Is.False);
            Assert.That(candidate.ViewObject.activeInHierarchy, Is.False);
            Assert.That(independent.State, Is.EqualTo(UIState.Visible));
            closeGate.TrySetResult();
            await Wait(selection);
            var selected = await selection;
            Assert.That(context.CurrentHud, Is.SameAs(selected));
            Assert.That(selected, Is.SameAs(candidate));
            Assert.That(selected.State, Is.EqualTo(UIState.Visible));
            Assert.That(independent.State, Is.EqualTo(UIState.Visible));
            Assert.That(selected.ViewObject.transform.GetSiblingIndex(),
                Is.LessThan(independent.ViewObject.transform.GetSiblingIndex()), "A later HUD stays behind Popup.");
            CollectionAssert.AreEqual(new[]
            {
                "next-prepare", "child-closed", "old-close", "old-closed", "next-open"
            }, trace);
            await Complete(selected.CloseAsync());
            Assert.That(context.CurrentHud, Is.Null);
            Assert.That(independent.State, Is.EqualTo(UIState.Visible));
            Assert.That(context.Fault, Is.Null);
        }

        [UnityTest]
        public IEnumerator HudCloseFailureDiscardsCandidateAndPreservesErrors()
        {
            return CheckHudTerminationFailure().ToCoroutine();
        }

        private async UniTask CheckHudTerminationFailure()
        {
            var context = Create();
            Register(context, "old", UIRole.Hud);
            Register(context, "next", UIRole.Hud, UIRetention.Reuse);
            Register(context, "child");
            int oldCleanupCount = 0;
            var old = await Select(context, new UIOpenRequest("old", hooks: new UIHooks
            {
                CloseAsync = (_, __) => throw new InvalidOperationException("expected-old-hud-close-failure")
            }));
            old.RegisterCleanup(() => ++oldCleanupCount);
            var child = await Open(context, new UIOpenRequest("child", parent: old));
            UIHandle candidate = null;
            GameObject candidateView = null;
            int candidateCleanupCount = 0;
            int candidateOpenCount = 0;
            var selection = context.SelectHudAsync(new UIOpenRequest("next", hooks: new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    candidate = handle;
                    candidateView = handle.ViewObject;
                    handle.RegisterCleanup(() =>
                    {
                        ++candidateCleanupCount;
                        throw new InvalidOperationException("expected-candidate-cleanup-failure");
                    });
                    return UniTask.CompletedTask;
                },
                OpenAsync = (_, __) =>
                {
                    ++candidateOpenCount;
                    return UniTask.CompletedTask;
                }
            })).AsTask();
            await ExpectError(selection, "expected-old-hud-close-failure");
            Assert.That(selection.Exception.ToString(), Does.Contain("expected-candidate-cleanup-failure"));
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.State, Is.EqualTo(UIState.Closed));
            Assert.That(candidateView == null, Is.True);
            Assert.That(candidateCleanupCount, Is.EqualTo(1));
            Assert.That(candidateOpenCount, Is.Zero);
            Assert.That(oldCleanupCount, Is.EqualTo(1));
            Assert.That(old.State, Is.EqualTo(UIState.Closed));
            Assert.That(child.State, Is.EqualTo(UIState.Closed));
            Assert.That(context.CurrentHud, Is.Null);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_host.GetComponentsInChildren<Image>(true), Is.Empty, "A failed candidate is not retained.");
            Assert.That(context.Fault, Is.Null, "Project close/cleanup errors do not fault native state application.");
        }

        [UnityTest]
        public IEnumerator OwnerCancellationPreventsLateHudPublication()
        {
            return CheckOwnerDuringSelection().ToCoroutine();
        }

        private async UniTask CheckOwnerDuringSelection()
        {
            var load = LoadGate();
            var context = Create((_, __) => load.Task);
            Register(context, "old", UIRole.Hud);
            context.Register(new UIDefinition("next", assetKey: "ui/late-hud", role: UIRole.Hud, hostId: "shared"));
            var old = await Select(context, new UIOpenRequest("old"));
            var selection = context.SelectHudAsync(new UIOpenRequest("next")).AsTask();
            Assert.That(selection.IsCompleted, Is.False);
            Assert.That(context.CurrentHud, Is.SameAs(old));
            var ownerToken = context.LifetimeToken;
            await Complete(context.ShutdownAsync());
            await ExpectCancellation(selection);
            load.TrySetResult(_source);
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(context.CurrentHud, Is.Null);
            Assert.That(old.State, Is.EqualTo(UIState.Closed));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_host.GetComponentsInChildren<Image>(true), Is.Empty);
            Assert.That(_host != null && _host.GetComponent<Canvas>().enabled && _source != null && _root != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RendererOnlyReuseKeepsActivationAndDisablesOwnedRaycasts()
        {
            return CheckRenderingOnlyReuse().ToCoroutine();
        }

        private async UniTask CheckRenderingOnlyReuse()
        {
            var context = Create();
            var source = Source("DedicatedCanvasSource", dedicatedCanvas: true);
            var sourceGroup = source.GetComponent<CanvasGroup>();
            sourceGroup.alpha = 0.65f;
            sourceGroup.blocksRaycasts = true;
            sourceGroup.interactable = true;
            context.Register(new UIDefinition("render-only", source, hostId: "shared", retention: UIRetention.Reuse,
                hideStrategy: UIHideStrategy.DisableCanvasRendering));
            var eventSystem = Own("PresentationRaycastEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            int prepareCount = 0;
            int cleanupCount = 0;
            GameObject view = null;
            CanvasGroup projectGroup = null;
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    ++prepareCount;
                    view = handle.ViewObject;
                    projectGroup = view.GetComponent<CanvasGroup>();
                    Assert.That(projectGroup.alpha, Is.EqualTo(0.65f));
                    Assert.That(projectGroup.blocksRaycasts && projectGroup.interactable, Is.True);
                    if (prepareCount == 1)
                    {
                        Assert.That(view.activeInHierarchy, Is.False, "Fresh clones still prepare inactive.");
                    }
                    else
                    {
                        Assert.That(view.activeSelf && view.activeInHierarchy, Is.True);
                        Assert.That(view.GetComponent<Canvas>().enabled, Is.False);
                        Assert.That(view.GetComponent<GraphicRaycaster>().enabled, Is.False);
                        Canvas.ForceUpdateCanvases();
                        Assert.That(view.GetComponent<Image>().canvasRenderer.GetInheritedAlpha(), Is.EqualTo(0f));
                    }
                    handle.RegisterCleanup(() => ++cleanupCount);
                    return UniTask.CompletedTask;
                }
            };
            var first = await Open(context, new UIOpenRequest("render-only", hooks: hooks));
            await UniTask.NextFrame();
            var retained = first.ViewObject;
            var canvas = retained.GetComponent<Canvas>();
            var raycaster = retained.GetComponent<GraphicRaycaster>();
            var image = retained.GetComponent<Image>();
            var probe = retained.GetComponent<UIContextLifecycleProbe>();
            Assert.That(canvas.enabled && raycaster.enabled && retained.activeInHierarchy, Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(image.depth, Is.GreaterThanOrEqualTo(0), "The fixture needs a rendered native Graphic depth.");
            Assert.That(((RectTransform)retained.transform).rect.width, Is.GreaterThan(0));
            Assert.That(((RectTransform)retained.transform).rect.height, Is.GreaterThan(0));
            Assert.That(HitsView(eventSystem, retained), Is.True, "The visible fixture must receive an actual native UI hit.");
            Assert.That(image.canvasRenderer.GetInheritedAlpha(), Is.GreaterThan(0f));
            int enableCount = probe.EnableCount;
            int disableCount = probe.DisableCount;
            await Complete(first.CloseAsync());
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            Assert.That(retained != null && retained.activeSelf && retained.activeInHierarchy, Is.True);
            Assert.That(canvas.enabled, Is.False);
            Assert.That(raycaster.enabled, Is.False);
            Assert.That(_host.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(_host.GetComponent<GraphicRaycaster>().enabled, Is.True);
            Assert.That(image.isActiveAndEnabled, Is.True);
            Assert.That(image.canvasRenderer.GetInheritedAlpha(), Is.EqualTo(0f),
                "Canvas disabling may move the Graphic to an enabled ancestor; owned visibility must still hide it.");
            Assert.That(HitsView(eventSystem, retained), Is.False, "The ancestor raycaster must not hit cached UI.");
            Assert.That(projectGroup.alpha, Is.EqualTo(0.65f));
            Assert.That(projectGroup.blocksRaycasts && projectGroup.interactable, Is.True);
            Assert.That(probe.EnableCount, Is.EqualTo(enableCount));
            Assert.That(probe.DisableCount, Is.EqualTo(disableCount));
            Assert.That(first.State, Is.EqualTo(UIState.Closed));
            Assert.That(ReferenceEquals(first.ViewObject, null), Is.True);
            Assert.That(first.LifetimeToken.IsCancellationRequested, Is.True);
            var second = await Open(context, new UIOpenRequest("render-only", hooks: hooks));
            await UniTask.NextFrame();
            Assert.That(second.ViewObject, Is.SameAs(retained));
            Assert.That(second.Id, Is.Not.EqualTo(first.Id));
            Assert.That(canvas.enabled && raycaster.enabled, Is.True);
            Assert.That(HitsView(eventSystem, retained), Is.True);
            Assert.That(image.canvasRenderer.GetInheritedAlpha(), Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(probe.EnableCount, Is.EqualTo(enableCount));
            Assert.That(probe.DisableCount, Is.EqualTo(disableCount));
            await Complete(first.CloseAsync());
            Assert.That(second.State, Is.EqualTo(UIState.Visible));
            Assert.That(second.LifetimeToken.IsCancellationRequested, Is.False);
            await Complete(second.CloseAsync());
            Assert.That(cleanupCount, Is.EqualTo(2));
            Assert.That(prepareCount, Is.EqualTo(2));
            Assert.That(sourceGroup.alpha, Is.EqualTo(0.65f));
            Assert.That(sourceGroup.blocksRaycasts && sourceGroup.interactable, Is.True);
            await Complete(context.ShutdownAsync());
            Assert.That(retained == null, Is.True);
            Assert.That(source != null && _host != null && _host.GetComponent<Canvas>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PersistentOwnerKeepsRendererOnlyRetentionInItsOwnScene()
        {
            return CheckPersistentRetention().ToCoroutine();
        }

        private async UniTask CheckPersistentRetention()
        {
            Scene originalScene = SceneManager.GetActiveScene();
            UnityEngine.Object.DontDestroyOnLoad(_root);
            Assert.That(_root.scene, Is.Not.EqualTo(originalScene));
            var context = Create();
            var source = Source("PersistentDedicatedSource", dedicatedCanvas: true);
            context.Register(new UIDefinition("persistent-retained", source, hostId: "shared",
                retention: UIRetention.Reuse, hideStrategy: UIHideStrategy.DisableCanvasRendering));
            var first = await Open(context, new UIOpenRequest("persistent-retained"));
            var retained = first.ViewObject;
            await Complete(first.CloseAsync());
            Assert.That(retained != null && retained.activeInHierarchy, Is.True);
            Assert.That(retained.scene, Is.EqualTo(_root.scene),
                "A persistent owner's cache must not be destroyed with the current game scene.");
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene));
            var second = await Open(context, new UIOpenRequest("persistent-retained"));
            Assert.That(second.ViewObject, Is.SameAs(retained));
            await Complete(context.ShutdownAsync());
            Assert.That(retained == null, Is.True);
            Assert.That(_root != null && _host != null && source != null, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene));
        }

        [UnityTest]
        public IEnumerator RendererOnlyHideRejectsSharedCanvasWithoutAffectingSibling()
        {
            return CheckSharedCanvasHideRejection().ToCoroutine();
        }

        private async UniTask CheckSharedCanvasHideRejection()
        {
            var context = Create();
            Register(context, "existing");
            var existing = await Open(context, new UIOpenRequest("existing"));
            var view = existing.ViewObject;
            int siblingIndex = view.transform.GetSiblingIndex();
            int hostChildren = _host.transform.childCount;
            await ExpectInvalid(async () =>
            {
                context.Register(new UIDefinition("invalid-render-only", _source, hostId: "shared",
                    hideStrategy: UIHideStrategy.DisableCanvasRendering));
                await context.OpenAsync(new UIOpenRequest("invalid-render-only"));
            });
            Assert.That(existing.State, Is.EqualTo(UIState.Visible));
            Assert.That(existing.ViewObject, Is.SameAs(view));
            Assert.That(view.activeInHierarchy, Is.True);
            Assert.That(view.transform.GetSiblingIndex(), Is.EqualTo(siblingIndex));
            Assert.That(_host.transform.childCount, Is.EqualTo(hostChildren));
            Assert.That(_host.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(_host.GetComponent<GraphicRaycaster>().enabled, Is.True);
            Assert.That(context.Fault, Is.Null);
        }

        [UnityTest]
        public IEnumerator ReusedRectTransformRestoresPrefabLayoutBeforePreparation()
        {
            return CheckReusedLayout().ToCoroutine();
        }

        private async UniTask CheckReusedLayout()
        {
            var sourceRect = (RectTransform)_source.transform;
            sourceRect.anchorMin = new Vector2(0.1f, 0.2f);
            sourceRect.anchorMax = new Vector2(0.8f, 0.9f);
            sourceRect.pivot = new Vector2(0.3f, 0.7f);
            sourceRect.sizeDelta = new Vector2(40f, -20f);
            sourceRect.anchoredPosition3D = new Vector3(12f, 34f, 5f);
            var context = Create();
            Register(context, "layout", retention: UIRetention.Reuse);
            int preparationCount = 0;
            GameObject firstView = null;
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    ++preparationCount;
                    var rect = (RectTransform)handle.ViewObject.transform;
                    Assert.That(handle.ViewObject.activeInHierarchy, Is.False);
                    Assert.That(rect.anchorMin, Is.EqualTo(sourceRect.anchorMin));
                    Assert.That(rect.anchorMax, Is.EqualTo(sourceRect.anchorMax));
                    Assert.That(rect.pivot, Is.EqualTo(sourceRect.pivot));
                    Assert.That(rect.sizeDelta, Is.EqualTo(sourceRect.sizeDelta));
                    Assert.That((rect.anchoredPosition3D - sourceRect.anchoredPosition3D).sqrMagnitude, Is.LessThan(0.0001f),
                        "The source layout must be restored before project preparation observes a reused clone.");
                    return UniTask.CompletedTask;
                }
            };
            var first = await Open(context, new UIOpenRequest("layout", hooks: hooks));
            firstView = first.ViewObject;
            var changed = (RectTransform)firstView.transform;
            changed.anchorMin = Vector2.zero;
            changed.anchorMax = Vector2.zero;
            changed.pivot = Vector2.one;
            changed.sizeDelta = new Vector2(777f, 888f);
            changed.anchoredPosition3D = new Vector3(-100f, 200f, 30f);
            await Complete(first.CloseAsync());
            var second = await Open(context, new UIOpenRequest("layout", hooks: hooks));
            Assert.That(second.ViewObject, Is.SameAs(firstView));
            Assert.That(preparationCount, Is.EqualTo(2));
            Assert.That(sourceRect.anchorMin, Is.EqualTo(new Vector2(0.1f, 0.2f)));
            Assert.That(sourceRect.sizeDelta, Is.EqualTo(new Vector2(40f, -20f)));
            await Complete(context.ShutdownAsync());
            Assert.That(firstView == null, Is.True);
            Assert.That(_source != null && _host != null, Is.True);
        }
    }
}
