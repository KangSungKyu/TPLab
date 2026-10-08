using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests
{
    public sealed class UIContextPlayModeTests
    {
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UIPlayOwner");
            _source = new GameObject("UIPlaySource");
            _source.SetActive(false);
            _source.AddComponent<UIContextLifecycleProbe>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            _gates.Clear();
            try
            {
                _context?.Dispose();
            }
            catch (Exception)
            {
                // Completion assertions own expected failures; preserve native fixture teardown.
            }
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_source);
            _context = null;
            yield return null;
        }

        private UIContext Create()
        {
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("popup", _source));
            return _context;
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
            Assert.That(task.IsCompleted, Is.True, "The public completion did not settle within 100 frames.");
        }

        private static async UniTask WaitForView(Func<GameObject> readView)
        {
            for (int frame = 0; frame < 100 && readView() == null; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(readView() != null, Is.True, "Preparation did not receive a native clone.");
        }

        private static async UniTask AssertCancellation(Task task)
        {
            bool observedCancellation = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                observedCancellation = true;
            }
            Assert.That(observedCancellation, Is.True, "Await must propagate cancellation.");
        }

        [UnityTest]
        public IEnumerator CloneIsBorrowedSourceCopyAndStaysInactiveDuringPreparation()
            => CheckInactiveClone().ToCoroutine();

        private async UniTask CheckInactiveClone()
        {
            var context = Create();
            var gate = Gate();
            GameObject view = null;
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    view = handle.ViewObject;
                    return gate.Task.AttachExternalCancellation(token);
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            await WaitForView(() => view);
            Assert.That(handle.State, Is.EqualTo(UIState.Opening));
            Assert.That(opened.IsCompleted, Is.False);
            Assert.That(view, Is.Not.SameAs(_source));
            Assert.That(view.activeInHierarchy, Is.False);
            Assert.That(view.GetComponent<UIContextLifecycleProbe>().EnableCount, Is.Zero);
            Assert.That(_source.activeSelf, Is.False);
            var close = handle.CloseAsync().AsTask();
            await Wait(close);
            await close;
            await Wait(opened);
            await AssertCancellation(opened);
            Assert.That(handle.Opened.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(view == null, Is.True);
            Assert.That(_source != null && _root != null, Is.True);
        }

        [UnityTest]
        public IEnumerator PreparedCloneActivatesOnlyAfterPrepareHookCompletes()
            => CheckPreparedActivation().ToCoroutine();

        private async UniTask CheckPreparedActivation()
        {
            var context = Create();
            var gate = Gate();
            GameObject view = null;
            var hooks = new UIHooks
            {
                PrepareAsync = async (handle, token) =>
                {
                    view = handle.ViewObject;
                    await gate.Task.AttachExternalCancellation(token);
                    view.GetComponent<UIContextLifecycleProbe>().Prepared = true;
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            await WaitForView(() => view);
            Assert.That(view.GetComponent<UIContextLifecycleProbe>().EnableCount, Is.Zero);
            gate.TrySetResult();
            await Wait(opened);
            await opened;
            var probe = view.GetComponent<UIContextLifecycleProbe>();
            Assert.That(handle.State, Is.EqualTo(UIState.Visible));
            Assert.That(view.activeInHierarchy, Is.True);
            Assert.That(probe.EnableCount, Is.EqualTo(1));
            Assert.That(probe.FirstEnableWasPrepared, Is.True);
            await handle.CloseAsync();
        }

        [UnityTest]
        public IEnumerator CloseWaitsForNativeCloneDestructionAndPreservesBorrowedObjects()
            => CheckNativeClose().ToCoroutine();

        private async UniTask CheckNativeClose()
        {
            var context = Create();
            var handle = await context.OpenAsync(new UIOpenRequest("popup"));
            var view = handle.ViewObject;
            var lifetime = handle.LifetimeToken;
            int cleanupCount = 0;
            var probe = view.GetComponent<UIContextLifecycleProbe>();
            handle.RegisterCleanup(() => ++cleanupCount);
            var close = handle.CloseAsync().AsTask();
            var observation = handle.Closed.AsTask();
            await Wait(Task.WhenAll(close, observation));
            await close;
            await observation;
            await handle.CloseAsync();
            Assert.That(view == null, Is.True, "Close completed while a clone still existed natively.");
            Assert.That(handle.ViewObject == null, Is.True);
            Assert.That(lifetime.IsCancellationRequested, Is.True);
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(probe.DisableCount, Is.EqualTo(1));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_source != null && _root != null, Is.True);
            Assert.That(_source.GetComponent<UIContextLifecycleProbe>().EnableCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ClosedObserverSeesClosedStateAndNoView()
            => CheckClosedObserver().ToCoroutine();

        private async UniTask CheckClosedObserver()
        {
            var context = Create();
            var trace = new List<string>();
            int observerCount = 0;
            UIState observedState = UIState.Opening;
            bool observedNoView = false;
            bool observedNativeDestruction = false;
            GameObject createdView = null;
            var hooks = new UIHooks
            {
                Closed = handle =>
                {
                    ++observerCount;
                    observedState = handle.State;
                    observedNoView = ReferenceEquals(handle.ViewObject, null);
                    observedNativeDestruction = createdView == null;
                    trace.Add("closed");
                }
            };
            var handle = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            createdView = handle.ViewObject;
            handle.RegisterCleanup(() => trace.Add("first"));
            handle.RegisterCleanup(() => trace.Add("last"));
            var firstClose = handle.CloseAsync().AsTask();
            var repeatedClose = handle.CloseAsync().AsTask();
            await Wait(Task.WhenAll(firstClose, repeatedClose));
            await firstClose;
            await repeatedClose;
            await handle.Closed;
            await handle.CloseAsync();
            Assert.That(observedState, Is.EqualTo(UIState.Closed));
            Assert.That(observedNoView, Is.True, "The observer must receive a handle whose view reference was cleared.");
            Assert.That(observedNativeDestruction, Is.True, "The observer must run after native clone destruction.");
            CollectionAssert.AreEqual(new[] { "last", "first", "closed" }, trace);
            Assert.That(observerCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator IndependentPopupCanOpenAndCloseFromAnotherPopupHook()
            => CheckIndependentPopupComposition().ToCoroutine();

        private async UniTask CheckIndependentPopupComposition()
        {
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("a", _source));
            _context.Register(new UIDefinition("b", _source));
            UIHandle secondary = null;
            var hooks = new UIHooks
            {
                OpenAsync = async (_, token) =>
                {
                    secondary = await _context.OpenAsync(new UIOpenRequest("b"), token);
                    await secondary.Opened;
                    await secondary.CloseAsync();
                }
            };
            var primary = _context.BeginOpen(new UIOpenRequest("a", hooks: hooks));
            var opened = primary.Opened.AsTask();
            await Wait(opened);
            await opened;
            Assert.That(primary.State, Is.EqualTo(UIState.Visible));
            Assert.That(secondary, Is.Not.Null);
            Assert.That(secondary.State, Is.EqualTo(UIState.Closed));
            Assert.That(ReferenceEquals(secondary.ViewObject, null), Is.True);
            CollectionAssert.AreEqual(new[] { primary }, _context.Displays);
            await primary.CloseAsync();
        }

        [UnityTest]
        public IEnumerator OpeningHookFailureStillDestroysCloneAndRunsAllCleanup()
            => CheckOpeningFailure().ToCoroutine();

        private async UniTask CheckOpeningFailure()
        {
            var context = Create();
            GameObject view = null;
            var trace = new List<string>();
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    view = handle.ViewObject;
                    handle.RegisterCleanup(() => trace.Add("first"));
                    handle.RegisterCleanup(() => trace.Add("last"));
                    throw new InvalidOperationException("expected-opening-failure");
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            var closed = handle.Closed.AsTask();
            await Wait(opened);
            await Wait(closed);
            Assert.That(opened.IsFaulted, Is.True);
            Assert.That(opened.Exception.ToString(), Does.Contain("expected-opening-failure"));
            await closed;
            CollectionAssert.AreEqual(new[] { "last", "first" }, trace);
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(view == null, Is.True);
            Assert.That(context.IsDisposed, Is.False);
            Assert.That(_source != null && _root != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RootDestructionCancelsPendingOpenAndDisposesDisplayOnce()
            => CheckPendingOwnerDestruction().ToCoroutine();

        private async UniTask CheckPendingOwnerDestruction()
        {
            var context = Create();
            var gate = Gate();
            int cleanupCount = 0;
            GameObject view = null;
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    view = handle.ViewObject;
                    handle.RegisterCleanup(() => ++cleanupCount);
                    return gate.Task.AttachExternalCancellation(token);
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            var closed = handle.Closed.AsTask();
            var ownerToken = context.LifetimeToken;
            await WaitForView(() => view);
            UnityEngine.Object.Destroy(_root);
            await Wait(opened);
            await Wait(closed);
            await AssertCancellation(opened);
            Assert.That(handle.Opened.Status, Is.EqualTo(UniTaskStatus.Canceled));
            await closed;
            await context.ShutdownAsync();
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(view == null, Is.True);
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator NeverActiveOwnerDestructionCancelsContextLifetime()
            => CheckNeverActiveOwnerDestruction().ToCoroutine();

        private async UniTask CheckNeverActiveOwnerDestruction()
        {
            _root.SetActive(false);
            var context = Create();
            var ownerToken = context.LifetimeToken;
            UnityEngine.Object.Destroy(_root);
            for (int frame = 0; frame < 100 && !context.IsDisposed; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(context.IsDisposed, Is.True, "A never-active owner must still end its context.");
            await context.ShutdownAsync();
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RootDestructionClosesVisibleDisplayAndRepeatedShutdownIsSafe()
            => CheckVisibleOwnerDestruction().ToCoroutine();

        private async UniTask CheckVisibleOwnerDestruction()
        {
            var context = Create();
            int cleanupCount = 0;
            int closedCount = 0;
            var hooks = new UIHooks { Closed = _ => ++closedCount };
            var handle = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            var view = handle.ViewObject;
            var closed = handle.Closed.AsTask();
            var ownerToken = context.LifetimeToken;
            handle.RegisterCleanup(() => ++cleanupCount);
            UnityEngine.Object.Destroy(_root);
            await Wait(closed);
            await closed;
            var firstShutdown = context.ShutdownAsync().AsTask();
            var secondShutdown = context.ShutdownAsync().AsTask();
            await Wait(Task.WhenAll(firstShutdown, secondShutdown));
            await firstShutdown;
            await secondShutdown;
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(closedCount, Is.EqualTo(1));
            Assert.That(view == null, Is.True);
            Assert.That(_source != null, Is.True);
        }
    }
}