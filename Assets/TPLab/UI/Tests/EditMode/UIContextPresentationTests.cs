using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests.EditMode
{
    public sealed class UIContextPresentationTests
    {
        private GameObject _root;
        private GameObject _source;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<UIContext> _contexts = new List<UIContext>();
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();

        [SetUp]
        public void SetUp()
        {
            _root = Own("UIPresentationEditOwner");
            _source = Own("UIPresentationEditSource");
            _source.SetActive(false);
            _source.AddComponent<CanvasGroup>();
        }

        [TearDown]
        public void TearDown()
        {
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
                    // The test observes callback failures; fixture ownership must still be released.
                }
            }
            for (int index = _objects.Count - 1; index >= 0; --index)
            {
                UnityEngine.Object.DestroyImmediate(_objects[index]);
            }
            _gates.Clear();
            _contexts.Clear();
            _objects.Clear();
        }

        private GameObject Own(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private UIContext Create(GameObject root = null)
        {
            var context = new UIContext(root != null ? root : _root);
            _contexts.Add(context);
            return context;
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
                await UniTask.Yield();
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

        [Test]
        public void RegisterHostRejectsInvalidAndDuplicateRegistration()
        {
            var context = Create();
            var host = Own("BorrowedEditHost");
            var unmanaged = Own("BorrowedUnmanagedSibling");
            unmanaged.transform.SetParent(host.transform, false);
            var hostParent = host.transform.parent;
            int unmanagedIndex = unmanaged.transform.GetSiblingIndex();
            context.RegisterHost("popup", host.transform);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(host.transform.childCount, Is.EqualTo(1), "Host registration creates no view or helper.");
            Assert.Throws<ArgumentException>(() => context.RegisterHost("popup", host.transform));
            Assert.Throws<ArgumentException>(() => context.RegisterHost("default", host.transform));
            Assert.Throws<ArgumentException>(() => context.RegisterHost(" ", host.transform));
            Assert.Throws<ArgumentNullException>(() => context.RegisterHost("null", null));
            var destroyed = Own("DestroyedHost");
            var destroyedTransform = destroyed.transform;
            UnityEngine.Object.DestroyImmediate(destroyed);
            Assert.Throws<ArgumentNullException>(() => context.RegisterHost("destroyed", destroyedTransform));
            context.Dispose();
            Assert.That(host != null && unmanaged != null && _root != null && _source != null, Is.True);
            Assert.That(host.transform.parent, Is.SameAs(hostParent));
            Assert.That(unmanaged.transform.GetSiblingIndex(), Is.EqualTo(unmanagedIndex));
            Assert.That(context.CurrentHud, Is.Null);
            Assert.Throws<ObjectDisposedException>(() => context.RegisterHost("late", host.transform));
        }

        [UnityTest]
        public IEnumerator ParentValidationAndDuplicateScopeUseLogicalOwner()
        {
            return CheckParentsAndDuplicates().ToCoroutine();
        }

        private async UniTask CheckParentsAndDuplicates()
        {
            var context = Create();
            foreach (string id in new[] { "a", "b", "child", "opening" })
            {
                context.Register(new UIDefinition(id, _source));
            }
            var foreign = Create(Own("ForeignUIOwner"));
            foreign.Register(new UIDefinition("foreign", _source));
            var foreignParent = await Open(foreign, new UIOpenRequest("foreign"));
            Assert.Throws<InvalidOperationException>(() =>
                context.BeginOpen(new UIOpenRequest("child", parent: foreignParent)));
            var prepareGate = Gate();
            var opening = context.BeginOpen(new UIOpenRequest("opening", hooks: new UIHooks
            {
                PrepareAsync = (_, token) => prepareGate.Task.AttachExternalCancellation(token)
            }));
            var opened = opening.Opened.AsTask();
            Assert.That(opening.State, Is.EqualTo(UIState.Opening));
            Assert.Throws<InvalidOperationException>(() =>
                context.BeginOpen(new UIOpenRequest("child", parent: opening)));
            await Complete(opening.CloseAsync());
            await Wait(opened);
            bool cancelled = false;
            try
            {
                await opened;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            var closeGate = Gate();
            var a = await Open(context, new UIOpenRequest("a", hooks: new UIHooks
            {
                CloseAsync = (_, token) => closeGate.Task.AttachExternalCancellation(token)
            }));
            var b = await Open(context, new UIOpenRequest("b"));
            var childA = await Open(context, new UIOpenRequest("child", parent: a));
            var childB = await Open(context, new UIOpenRequest("child", parent: b));
            var contextChild = await Open(context, new UIOpenRequest("child"));
            Assert.That(childA.Parent, Is.SameAs(a));
            Assert.That(childB.Parent, Is.SameAs(b));
            Assert.That(contextChild.Parent, Is.Null);
            Assert.Throws<InvalidOperationException>(() =>
                context.BeginOpen(new UIOpenRequest("child", parent: a)));
            Assert.Throws<InvalidOperationException>(() => context.BeginOpen(new UIOpenRequest("child")));
            var aClose = a.CloseAsync().AsTask();
            Assert.That(a.State, Is.EqualTo(UIState.Closing));
            Assert.Throws<InvalidOperationException>(() =>
                context.BeginOpen(new UIOpenRequest("child", parent: a)));
            closeGate.TrySetResult();
            await Wait(aClose);
            await aClose;
            Assert.That(childA.State, Is.EqualTo(UIState.Closed));
            Assert.Throws<InvalidOperationException>(() =>
                context.BeginOpen(new UIOpenRequest("child", parent: a)));
            Assert.That(childB.State, Is.EqualTo(UIState.Visible));
            Assert.That(contextChild.State, Is.EqualTo(UIState.Visible));
            Assert.That(context.Fault, Is.Null);
        }

        [UnityTest]
        public IEnumerator HudSelectionRejectsWrongRoleParentAndConcurrentRequests()
        {
            return CheckSelectionValidation().ToCoroutine();
        }

        private async UniTask CheckSelectionValidation()
        {
            var context = Create();
            context.Register(new UIDefinition("popup", _source));
            context.Register(new UIDefinition("hud", _source, role: UIRole.Hud));
            context.Register(new UIDefinition("next", _source, role: UIRole.Hud));
            Assert.That(context.CurrentHud, Is.Null);
            await ExpectInvalid(async () =>
            {
                await context.SelectHudAsync(new UIOpenRequest("popup"));
            });
            var popup = await Open(context, new UIOpenRequest("popup"));
            await ExpectInvalid(async () =>
            {
                await context.SelectHudAsync(new UIOpenRequest("hud", parent: popup));
            });
            Assert.Throws<InvalidOperationException>(() => context.BeginOpen(new UIOpenRequest("hud")));
            UIHandle candidate = null;
            var gate = Gate();
            var selection = context.SelectHudAsync(new UIOpenRequest("hud", hooks: new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    candidate = handle;
                    return gate.Task.AttachExternalCancellation(token);
                }
            })).AsTask();
            for (int frame = 0; frame < 100 && candidate == null && !selection.IsCompleted; ++frame)
            {
                await UniTask.Yield();
            }
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.State, Is.EqualTo(UIState.Opening));
            Assert.That(context.CurrentHud, Is.Null);
            Assert.That(selection.IsCompleted, Is.False);
            await ExpectInvalid(async () =>
            {
                await context.SelectHudAsync(new UIOpenRequest("next"));
            });
            gate.TrySetResult();
            await Wait(selection);
            var selected = await selection;
            Assert.That(selected, Is.SameAs(candidate));
            Assert.That(context.CurrentHud, Is.SameAs(selected));
            await ExpectInvalid(async () =>
            {
                await context.SelectHudAsync(new UIOpenRequest("hud"));
            });
            Assert.That(context.CurrentHud, Is.SameAs(selected));
            Assert.That(popup.State, Is.EqualTo(UIState.Visible));
            Assert.That(context.Fault, Is.Null);
        }

        [UnityTest]
        public IEnumerator HookCannotCloseItsAncestorButCanOperateIndependentTree()
        {
            return CheckSubtreeReentry().ToCoroutine();
        }

        private async UniTask CheckSubtreeReentry()
        {
            var context = Create();
            foreach (string id in new[] { "parent", "child", "independent" })
            {
                context.Register(new UIDefinition(id, _source));
            }
            var parent = await Open(context, new UIOpenRequest("parent"));
            var independent = await Open(context, new UIOpenRequest("independent"));
            bool rejectedAncestor = false;
            bool rejectedSelf = false;
            var child = await Open(context, new UIOpenRequest("child", parent: parent, hooks: new UIHooks
            {
                OpenAsync = async (handle, _) =>
                {
                    rejectedAncestor = Assert.Throws<InvalidOperationException>(() => parent.CloseAsync()) != null;
                    rejectedSelf = Assert.Throws<InvalidOperationException>(() => handle.CloseAsync()) != null;
                    Assert.Throws<InvalidOperationException>(() => context.ShutdownAsync());
                    await independent.CloseAsync();
                }
            }));
            Assert.That(rejectedAncestor && rejectedSelf, Is.True);
            Assert.That(independent.State, Is.EqualTo(UIState.Closed));
            Assert.That(parent.State, Is.EqualTo(UIState.Visible));
            Assert.That(child.State, Is.EqualTo(UIState.Visible));
            await Complete(parent.CloseAsync());
            Assert.That(child.State, Is.EqualTo(UIState.Closed));
            Assert.That(parent.State, Is.EqualTo(UIState.Closed));
            Assert.That(context.Fault, Is.Null);
        }
    }
}
