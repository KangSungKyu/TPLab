// P4 Red native UI tests. Assertions use public state and actual native UI observations.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TPLab.UI.Tests
{
    public sealed class UIContextInputPresentationTests
    {
        private GameObject _owner;
        private GameObject _host;
        private GameObject _source;
        private EventSystem _events;
        private UIContext _context;
        private int _leases;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();

        [SetUp]
        public void SetUp()
        {
            _owner = Own("P4 native owner");
            _host = Host("P4 borrowed canvas", 10);
            _source = Own("P4 panel source", typeof(RectTransform));
            _source.SetActive(false);
            Stretch((RectTransform)_source.transform);
            var group = _source.AddComponent<CanvasGroup>();
            group.alpha = 0.6f;
            _source.AddComponent<Image>();
            AddButton("first", -90f);
            AddButton("second", 90f);
            _events = Own("P4 native EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            // The native EventSystem is borrowed explicitly; focus never resolves EventSystem.current.
            _context = new UIContext(_owner, acquireModalBlock: () =>
            {
                ++_leases;
                return new Lease(() => --_leases);
            }, eventSystem: _events);
            Assert.That(_context.EventSystem, Is.SameAs(_events));
            _context.RegisterHost("shared", _host.transform);
            foreach (string id in new[] { "a", "b", "c", "child" })
            {
                _context.Register(new UIDefinition(id, _source, hostId: "shared", retention: UIRetention.Reuse));
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            try
            {
                _context?.Dispose();
            }
            catch (Exception)
            {
                // Expected errors are asserted before native fixture teardown.
            }
            for (int index = _objects.Count - 1; index >= 0; --index)
            {
                UnityEngine.Object.Destroy(_objects[index]);
            }
            _gates.Clear();
            _objects.Clear();
            yield return null;
        }

        private GameObject Own(string name, params Type[] components)
        {
            var value = new GameObject(name, components);
            _objects.Add(value);
            return value;
        }

        private GameObject Host(string name, int order)
        {
            var value = Own(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            value.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            value.GetComponent<Canvas>().sortingOrder = order;
            return value;
        }

        private void AddButton(string name, float x)
        {
            var button = Own(name, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(_source.transform, false);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100f, 50f);
            rect.anchoredPosition = new Vector2(x, 0f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private static async UniTask Wait(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True);
        }

        private static async UniTask Complete(UniTask operation)
        {
            var task = operation.AsTask();
            await Wait(task);
            await task;
        }

        private async UniTask<UIHandle> Open(string id, UIInputMode mode = UIInputMode.Modeless,
            UIHandle parent = null, UIHooks hooks = null)
        {
            var task = _context.OpenAsync(new UIOpenRequest(id, parent, mode, hooks)).AsTask();
            await Wait(task);
            return await task;
        }

        private static async UniTask SettleGraphic(UIHandle handle)
        {
            var image = handle.ViewObject.GetComponent<Image>();
            for (int frame = 0; frame < 10; ++frame)
            {
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                if (image.depth >= 0)
                {
                    break;
                }
            }
            Assert.That(image.depth, Is.GreaterThanOrEqualTo(0), "The native graphic must be registered before hit assertions.");
            Assert.That(image.rectTransform.rect.width, Is.GreaterThan(0f));
        }

        private bool Hits(UIHandle handle)
        {
            Canvas.ForceUpdateCanvases();
            var results = new List<RaycastResult>();
            _events.RaycastAll(new PointerEventData(_events)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            }, results);
            foreach (var result in results)
            {
                if (result.gameObject == handle.ViewObject || result.gameObject.transform.IsChildOf(handle.ViewObject.transform))
                {
                    return true;
                }
            }
            return false;
        }

        private static GameObject First(UIHandle handle) => handle.ViewObject.transform.Find("first").gameObject;
        private static Transform NativeRoot(UIHandle handle) => handle.ViewObject.transform.parent;

        [UnityTest]
        public IEnumerator MiddleModalBlocksAThroughClosingAndPreservesIndependentCFocus()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var a = await Open("a");
                var closeGate = Gate();
                var b = await Open("b", UIInputMode.Modal, hooks: new UIHooks
                {
                    CloseAsync = (_, token) => closeGate.Task.AttachExternalCancellation(token)
                });
                var c = await Open("c");
                c.SetFocus(First(c));
                await SettleGraphic(c);
                Assert.That(Hits(a), Is.False);
                Assert.That(Hits(b) && Hits(c), Is.True);
                Assert.That(a.CanReceiveInput, Is.False);
                Assert.That(b.CanReceiveInput && c.CanReceiveInput, Is.True);
                var closing = b.CloseAsync().AsTask();
                Assert.That(b.State, Is.EqualTo(UIState.Closing));
                Assert.That(_leases, Is.EqualTo(1));
                Assert.That(Hits(a), Is.False);
                Assert.That(_events.currentSelectedGameObject, Is.SameAs(First(c)));
                Assert.That(First(a).GetComponent<Button>().IsInteractable(), Is.False);
                Assert.That(First(c).GetComponent<Button>().FindSelectableOnRight(),
                    Is.SameAs(c.ViewObject.transform.Find("second").GetComponent<Button>()));
                closeGate.TrySetResult();
                await Wait(closing);
                await closing;
                Assert.That(_leases, Is.Zero);
                Assert.That(Hits(a) && Hits(c), Is.True);
                Assert.That(_events.currentSelectedGameObject, Is.SameAs(First(c)));
                Assert.That(a.CanReceiveInput && c.CanReceiveInput, Is.True);
                Assert.That(b.CanReceiveInput, Is.False);
            });
        }

        [UnityTest]
        public IEnumerator OpeningModalRendersWithoutEnteringInputBoundaryUntilVisible()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var a = await Open("a");
                var openGate = Gate();
                var b = _context.BeginOpen(new UIOpenRequest("b", inputMode: UIInputMode.Modal, hooks: new UIHooks
                {
                    OpenAsync = (_, token) => openGate.Task.AttachExternalCancellation(token)
                }));
                await SettleGraphic(b);
                Assert.That(b.State, Is.EqualTo(UIState.Opening));
                Assert.That(b.ViewObject.activeInHierarchy, Is.True);
                Assert.That(b.CanReceiveInput, Is.False);
                Assert.That(Hits(b), Is.False);
                Assert.That(Hits(a), Is.True);
                Assert.That(_leases, Is.Zero);
                openGate.TrySetResult();
                await Complete(b.Opened);
                Assert.That(b.CanReceiveInput, Is.True);
                Assert.That(Hits(b), Is.True);
                Assert.That(Hits(a), Is.False);
                Assert.That(_leases, Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator DynamicModeAndBringToFrontMoveWholeSubtreeWithoutChangingGeneration()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var a = await Open("a");
                var child = await Open("child", parent: a);
                var b = await Open("b", UIInputMode.Modal);
                var c = await Open("c");
                long aId = a.Id;
                long childId = child.Id;
                Assert.That(a.CanReceiveInput || child.CanReceiveInput, Is.False);
                a.BringToFront();
                Assert.That(NativeRoot(b).GetSiblingIndex(), Is.LessThan(NativeRoot(c).GetSiblingIndex()));
                Assert.That(NativeRoot(c).GetSiblingIndex(), Is.LessThan(NativeRoot(a).GetSiblingIndex()));
                Assert.That(NativeRoot(a).GetSiblingIndex(), Is.LessThan(NativeRoot(child).GetSiblingIndex()));
                Assert.That(a.Id == aId && child.Id == childId, Is.True);
                Assert.That(a.CanReceiveInput && child.CanReceiveInput, Is.True);
                child.SetInputMode(UIInputMode.Modal);
                Assert.That(_leases, Is.EqualTo(2));
                Assert.That(c.CanReceiveInput || a.CanReceiveInput, Is.False);
                Assert.That(child.CanReceiveInput, Is.True);
                child.SetInputMode(UIInputMode.Modeless);
                Assert.That(child.InputMode, Is.EqualTo(UIInputMode.Modeless));
                Assert.That(_leases, Is.EqualTo(1));
                Assert.That(a.CanReceiveInput && c.CanReceiveInput, Is.True);
                Assert.That(_source.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.6f));
                Assert.That(a.ViewObject.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.6f));
                Assert.That(a.ViewObject.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(a.ViewObject.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(a.ViewObject.GetComponent<Image>().enabled, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator RetiredReusableHandleCannotChangeLaterRentalOrRestoreStaleFocus()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var old = await Open("a");
                var reusedView = old.ViewObject;
                var oldTarget = First(old);
                old.SetFocus(oldTarget);
                var b = await Open("b", UIInputMode.Modal);
                var c = await Open("c");
                c.SetFocus(First(c));
                await Complete(old.CloseAsync());
                var next = await Open("a");
                Assert.That(next.ViewObject, Is.SameAs(reusedView));
                Assert.That(next.Id, Is.Not.EqualTo(old.Id));
                c.SetFocus(First(c));
                Assert.Throws<InvalidOperationException>(() => old.SetFocus(oldTarget));
                Assert.Throws<InvalidOperationException>(old.BringToFront);
                Assert.Throws<InvalidOperationException>(() => old.SetInputMode(UIInputMode.Modal));
                await Complete(old.CloseAsync());
                Assert.That(next.State, Is.EqualTo(UIState.Visible));
                await Complete(b.CloseAsync());
                Assert.That(_events.currentSelectedGameObject, Is.SameAs(First(c)),
                    "A valid current selection wins over a stale generation bookmark.");
                Assert.That(next.InputMode, Is.EqualTo(UIInputMode.Modeless));
                var destroyedTarget = First(c);
                UnityEngine.Object.Destroy(destroyedTarget);
                await UniTask.NextFrame();
                Assert.That(destroyedTarget == null, Is.True);
                Assert.Throws<ArgumentException>(() => c.SetFocus(destroyedTarget));
                c.SetFocus(c.ViewObject.transform.Find("second").gameObject);
                await Complete(next.CloseAsync());
                Assert.That(_events.currentSelectedGameObject,
                    Is.SameAs(c.ViewObject.transform.Find("second").gameObject),
                    "A destroyed focus target cannot survive a later eligibility restoration.");
            });
        }

        [UnityTest]
        public IEnumerator UnsafeNativeGateAndUnrepresentableFrontRejectBeforeBorrowedMutation()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var a = await Open("a");
                var unsafeSource = Own("P4 bypass source", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                unsafeSource.SetActive(false);
                unsafeSource.GetComponent<CanvasGroup>().ignoreParentGroups = true;
                _context.Register(new UIDefinition("unsafe", unsafeSource, hostId: "shared"));
                int sibling = NativeRoot(a).GetSiblingIndex();
                var invalid = _context.OpenAsync(new UIOpenRequest("unsafe")).AsTask();
                await Wait(invalid);
                Exception invalidError = null;
                try
                {
                    await invalid;
                }
                catch (Exception error)
                {
                    invalidError = error;
                }
                Assert.That(invalidError, Is.InstanceOf<InvalidOperationException>());
                Assert.That(NativeRoot(a).GetSiblingIndex(), Is.EqualTo(sibling));
                var higher = Host("P4 borrowed higher canvas", 20);
                _context.RegisterHost("higher", higher.transform);
                _context.Register(new UIDefinition("higher", _source, hostId: "higher"));
                var upper = await _context.OpenAsync(new UIOpenRequest("higher"));
                Assert.Throws<InvalidOperationException>(a.BringToFront);
                Assert.That(NativeRoot(a).GetSiblingIndex(), Is.EqualTo(sibling));
                Assert.That(upper.State, Is.EqualTo(UIState.Visible));
                Assert.That(_host.GetComponent<Canvas>().sortingOrder, Is.EqualTo(10));
                Assert.That(higher.GetComponent<Canvas>().sortingOrder, Is.EqualTo(20));
                Assert.That(_context.Fault, Is.Null);
            });
        }

        [UnityTest]
        public IEnumerator ClosingAnimationAndCleanupFailureStillRetireLeaseAndPreserveCFocus()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var a = await Open("a");
                var gate = Gate();
                var trace = new List<string>();
                var b = await Open("b", UIInputMode.Modal, hooks: new UIHooks
                {
                    CloseAsync = (_, token) => gate.Task.AttachExternalCancellation(token),
                    Closed = handle =>
                    {
                        Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                        Assert.That(handle.ViewObject, Is.Null);
                        trace.Add("closed");
                    }
                });
                var c = await Open("c");
                c.SetFocus(First(c));
                b.RegisterCleanup(() => trace.Add("cleanup-1"));
                b.RegisterCleanup(() =>
                {
                    trace.Add("cleanup-2");
                    throw new InvalidOperationException("expected-P4-cleanup-failure");
                });
                var first = b.CloseAsync().AsTask();
                var second = b.CloseAsync().AsTask();
                Assert.That(_leases, Is.EqualTo(1));
                Assert.That(a.CanReceiveInput, Is.False);
                gate.TrySetResult();
                foreach (var task in new[] { first, second })
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
                    Assert.That(failure.ToString(), Does.Contain("expected-P4-cleanup-failure"));
                }
                CollectionAssert.AreEqual(new[] { "cleanup-2", "cleanup-1", "closed" }, trace);
                Assert.That(_leases, Is.Zero);
                Assert.That(a.CanReceiveInput && c.CanReceiveInput, Is.True);
                Assert.That(_events.currentSelectedGameObject, Is.SameAs(First(c)));
                Assert.That(_context.Fault, Is.Null);
            });
        }

        private sealed class Lease : IDisposable
        {
            private Action _release;

            internal Lease(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                Action release = _release;
                _release = null;
                release?.Invoke();
            }
        }
    }
}