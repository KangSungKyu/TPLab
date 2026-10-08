using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests
{
    public sealed class UIContextReuseTests
    {
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();
        private readonly List<UniTaskCompletionSource<GameObject>> _loads = new List<UniTaskCompletionSource<GameObject>>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UIReuseOwner");
            _source = new GameObject("UIReuseSource");
            _source.SetActive(false);
            _source.AddComponent<UIContextLifecycleProbe>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var load in _loads)
            {
                load.TrySetResult(_source);
            }
            _loads.Clear();
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
                // Expected completion errors belong to the test; preserve native fixture teardown.
            }
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_source);
            _context = null;
            yield return null;
        }

        private UIContext CreateReuse()
        {
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("popup", _source, retention: UIRetention.Reuse));
            return _context;
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
            Assert.That(task.IsCompleted, Is.True, "The public completion did not settle within 100 frames.");
        }

        private static async UniTask AssertCancellation(Task task)
        {
            bool cancelled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True, "Await must propagate cancellation.");
        }

        [UnityTest]
        public IEnumerator RuntimeLazyOpenUsesInactivePreparationBeforeActivation()
            => CheckRuntimeLazyPreparation().ToCoroutine();

        private async UniTask CheckRuntimeLazyPreparation()
        {
            var load = LoadGate();
            var preparationGate = Gate();
            int providerCalls = 0;
            int cleanupCount = 0;
            GameObject view = null;
            _source.SetActive(true);
            _context = new UIContext(_root, loadPrefab: (key, _) =>
            {
                ++providerCalls;
                Assert.That(key, Is.EqualTo("ui/lazy"));
                return load.Task;
            });
            _context.Register(new UIDefinition("popup", assetKey: "ui/lazy"));
            var hooks = new UIHooks
            {
                PrepareAsync = async (handle, token) =>
                {
                    view = handle.ViewObject;
                    Assert.That(view.activeInHierarchy, Is.False);
                    Assert.That(view.GetComponent<UIContextLifecycleProbe>().EnableCount, Is.Zero);
                    handle.RegisterCleanup(() => ++cleanupCount);
                    await preparationGate.Task.AttachExternalCancellation(token);
                    view.GetComponent<UIContextLifecycleProbe>().Prepared = true;
                }
            };
            var handle = _context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            Assert.That(handle.State, Is.EqualTo(UIState.Opening));
            Assert.That(handle.ViewObject == null, Is.True);
            Assert.That(opened.IsCompleted, Is.False);
            Assert.That(providerCalls, Is.EqualTo(1), "No separate Prepare call is required.");
            load.TrySetResult(_source);
            for (int frame = 0; frame < 100 && view == null; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(view != null, Is.True);
            Assert.That(view.activeInHierarchy, Is.False);
            Assert.That(opened.IsCompleted, Is.False);
            preparationGate.TrySetResult();
            await Wait(opened);
            await opened;
            var probe = view.GetComponent<UIContextLifecycleProbe>();
            Assert.That(handle.State, Is.EqualTo(UIState.Visible));
            Assert.That(probe.EnableCount, Is.EqualTo(1));
            Assert.That(probe.FirstEnableWasPrepared, Is.True);
            Assert.That(providerCalls, Is.EqualTo(1));
            await handle.CloseAsync();
            Assert.That(view == null, Is.True);
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(_source != null && _source.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator ReuseReopensSameCloneWithFreshGenerationAndCleanup()
            => CheckFreshReuseGeneration().ToCoroutine();

        private async UniTask CheckFreshReuseGeneration()
        {
            var context = CreateReuse();
            int prepareCount = 0;
            var cleanedGenerations = new List<long>();
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, _) =>
                {
                    ++prepareCount;
                    GameObject boundView = handle.ViewObject;
                    Assert.That(boundView.activeInHierarchy, Is.False);
                    var probe = boundView.GetComponent<UIContextLifecycleProbe>();
                    probe.Prepared = true;
                    long generation = handle.Id;
                    handle.RegisterCleanup(() =>
                    {
                        probe.Prepared = false;
                        cleanedGenerations.Add(generation);
                    });
                    return UniTask.CompletedTask;
                }
            };
            var first = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            var reusedView = first.ViewObject;
            var oldToken = first.LifetimeToken;
            await first.CloseAsync();
            Assert.That(reusedView != null, Is.True, "Successful Reuse close retains the owned native clone.");
            Assert.That(reusedView.activeSelf, Is.False);
            Assert.That(ReferenceEquals(first.ViewObject, null), Is.True);
            Assert.That(first.State, Is.EqualTo(UIState.Closed));
            Assert.That(oldToken.IsCancellationRequested, Is.True);
            CollectionAssert.AreEqual(new[] { first.Id }, cleanedGenerations);
            using (var caller = new CancellationTokenSource())
            {
                var second = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks), caller.Token);
                Assert.That(second.ViewObject, Is.SameAs(reusedView));
                Assert.That(second.Id, Is.Not.EqualTo(first.Id));
                Assert.That(second.LifetimeToken, Is.Not.EqualTo(oldToken));
                Assert.That(second.LifetimeToken.IsCancellationRequested, Is.False);
                caller.Cancel();
                await first.CloseAsync();
                await first.Closed;
                Assert.That(second.State, Is.EqualTo(UIState.Visible));
                Assert.That(second.ViewObject.activeInHierarchy, Is.True);
                Assert.That(second.LifetimeToken.IsCancellationRequested, Is.False);
                Assert.That(ReferenceEquals(first.ViewObject, null), Is.True);
                CollectionAssert.AreEqual(new[] { first.Id }, cleanedGenerations);
                Assert.That(prepareCount, Is.EqualTo(2));
                Assert.That(reusedView.GetComponent<UIContextLifecycleProbe>().EnableCount, Is.EqualTo(2));
                await second.CloseAsync();
                CollectionAssert.AreEqual(new[] { first.Id, second.Id }, cleanedGenerations);
            }
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_root.GetComponentsInChildren<UIContextLifecycleProbe>(true).Length, Is.EqualTo(1));
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator PartialOpeningOrCleanupFailureDiscardsReusableClone()
            => CheckFailedReuseDiscard().ToCoroutine();

        private async UniTask CheckFailedReuseDiscard()
        {
            var context = CreateReuse();
            for (int failureMode = 0; failureMode < 4; ++failureMode)
            {
                int mode = failureMode;
                var gate = Gate();
                GameObject failedView = null;
                var trace = new List<string>();
                var hooks = new UIHooks
                {
                    PrepareAsync = (handle, token) =>
                    {
                        failedView = handle.ViewObject;
                        handle.RegisterCleanup(() => trace.Add("first"));
                        handle.RegisterCleanup(() =>
                        {
                            trace.Add("last");
                            if (mode == 3)
                            {
                                throw new InvalidOperationException("expected-reuse-cleanup-failure");
                            }
                        });
                        if (mode == 0)
                        {
                            return gate.Task.AttachExternalCancellation(token);
                        }
                        if (mode == 1)
                        {
                            throw new InvalidOperationException("expected-reuse-prepare-failure");
                        }
                        return UniTask.CompletedTask;
                    },
                    OpenAsync = (_, __) =>
                    {
                        if (mode == 2)
                        {
                            throw new InvalidOperationException("expected-reuse-open-failure");
                        }
                        return UniTask.CompletedTask;
                    }
                };
                using (var caller = new CancellationTokenSource())
                {
                    var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks), caller.Token);
                    var opened = handle.Opened.AsTask();
                    var closed = handle.Closed.AsTask();
                    if (mode == 0)
                    {
                        for (int frame = 0; frame < 100 && failedView == null; ++frame)
                        {
                            await UniTask.NextFrame();
                        }
                        Assert.That(failedView != null, Is.True, "Cancel only after preparation observes a partial clone.");
                        caller.Cancel();
                    }
                    await Wait(opened);
                    if (mode == 0)
                    {
                        await AssertCancellation(opened);
                        Assert.That(handle.Opened.Status, Is.EqualTo(UniTaskStatus.Canceled));
                    }
                    else if (mode < 3)
                    {
                        Assert.That(opened.IsFaulted, Is.True);
                        Assert.That(opened.Exception.ToString(),
                            Does.Contain(mode == 1 ? "expected-reuse-prepare-failure" : "expected-reuse-open-failure"));
                    }
                    else
                    {
                        await opened;
                        var close = handle.CloseAsync().AsTask();
                        await Wait(close);
                        Assert.That(close.IsFaulted, Is.True);
                        Assert.That(close.Exception.ToString(), Does.Contain("expected-reuse-cleanup-failure"));
                    }
                    await Wait(closed);
                    if (mode == 3)
                    {
                        Assert.That(closed.IsFaulted, Is.True);
                        Assert.That(closed.Exception.ToString(), Does.Contain("expected-reuse-cleanup-failure"));
                    }
                    else
                    {
                        await closed;
                    }
                    CollectionAssert.AreEqual(new[] { "last", "first" }, trace);
                    Assert.That(failedView == null, Is.True, "A partial or failed generation must not enter the reuse cache.");
                    Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
                    Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                    Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
                    Assert.That(context.Displays, Is.Empty);
                    Assert.That(_root.GetComponentsInChildren<UIContextLifecycleProbe>(true).Length, Is.Zero);
                    Assert.That(context.IsDisposed, Is.False);
                }
            }
            var next = await context.OpenAsync(new UIOpenRequest("popup"));
            Assert.That(next.State, Is.EqualTo(UIState.Visible));
            await next.CloseAsync();
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator ClosedObserverFailureDiscardsRetiredCloneWithoutAffectingNewDisplay()
            => CheckObserverFailureAndNewGeneration().ToCoroutine();

        private async UniTask CheckObserverFailureAndNewGeneration()
        {
            var context = CreateReuse();
            UIHandle next = null;
            int observerCount = 0;
            bool observerSawTermination = false;
            int cleanupCount = 0;
            var hooks = new UIHooks
            {
                Closed = handle =>
                {
                    ++observerCount;
                    observerSawTermination = handle.State == UIState.Closed
                        && ReferenceEquals(handle.ViewObject, null);
                    next = context.BeginOpen(new UIOpenRequest("popup"));
                    throw new InvalidOperationException("expected-reuse-observer-failure");
                }
            };
            var first = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            var retiredView = first.ViewObject;
            first.RegisterCleanup(() => ++cleanupCount);
            var firstClose = first.CloseAsync().AsTask();
            await Wait(firstClose);
            Assert.That(firstClose.IsFaulted, Is.True);
            Assert.That(firstClose.Exception.ToString(), Does.Contain("expected-reuse-observer-failure"));
            Assert.That(next, Is.Not.Null);
            var nextOpened = next.Opened.AsTask();
            await Wait(nextOpened);
            await nextOpened;
            Assert.That(observerSawTermination, Is.True);
            Assert.That(observerCount, Is.EqualTo(1));
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(retiredView == null, Is.True, "Observer failure discards only its retired clone.");
            Assert.That(next.ViewObject != null && next.ViewObject.activeInHierarchy, Is.True);
            Assert.That(next.ViewObject, Is.Not.SameAs(retiredView));
            Assert.That(next.Id, Is.Not.EqualTo(first.Id));
            Assert.That(next.LifetimeToken.IsCancellationRequested, Is.False);
            var oldClose = first.CloseAsync().AsTask();
            await Wait(oldClose);
            Assert.That(oldClose.IsFaulted, Is.True);
            Assert.That(oldClose.Exception.ToString(), Does.Contain("expected-reuse-observer-failure"));
            Assert.That(next.State, Is.EqualTo(UIState.Visible));
            CollectionAssert.AreEqual(new[] { next }, context.Displays);
            await next.CloseAsync();
            Assert.That(observerCount, Is.EqualTo(1));
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator ReuseCacheKeepsOnlyOneInactiveCloneAfterNestedCloseCallbacks()
            => CheckBoundedReuseCache().ToCoroutine();

        private async UniTask CheckBoundedReuseCache()
        {
            var context = CreateReuse();
            UIHandle nested = null;
            Task nestedClose = null;
            GameObject nestedView = null;
            int observerCount = 0;
            var hooks = new UIHooks
            {
                Closed = _ =>
                {
                    ++observerCount;
                    var nestedHooks = new UIHooks
                    {
                        PrepareAsync = (handle, __) =>
                        {
                            nestedView = handle.ViewObject;
                            return UniTask.CompletedTask;
                        }
                    };
                    nested = context.BeginOpen(new UIOpenRequest("popup", hooks: nestedHooks));
                    nestedClose = CloseAfterOpened(nested).AsTask();
                }
            };
            var first = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            var firstView = first.ViewObject;
            var firstClose = first.CloseAsync().AsTask();
            await Wait(firstClose);
            await firstClose;
            Assert.That(nestedClose, Is.Not.Null);
            await Wait(nestedClose);
            await nestedClose;
            Assert.That(first.State, Is.EqualTo(UIState.Closed));
            Assert.That(nested.State, Is.EqualTo(UIState.Closed));
            Assert.That(nested.Id, Is.Not.EqualTo(first.Id));
            Assert.That(ReferenceEquals(first.ViewObject, null), Is.True);
            Assert.That(ReferenceEquals(nested.ViewObject, null), Is.True);
            Assert.That(observerCount, Is.EqualTo(1));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_root.GetComponentsInChildren<UIContextLifecycleProbe>(true).Length, Is.EqualTo(1),
                "One definition may retain at most one inactive clone after nested generations close.");
            var cachedView = firstView != null ? firstView : nestedView;
            Assert.That(cachedView != null, Is.True);
            Assert.That(cachedView.activeSelf, Is.False);
            Assert.That(cachedView.activeInHierarchy, Is.False);
            await context.ShutdownAsync();
            Assert.That(firstView == null && nestedView == null, Is.True);
            Assert.That(_source != null, Is.True);
        }

        private static async UniTask CloseAfterOpened(UIHandle handle)
        {
            await handle.Opened;
            await handle.CloseAsync();
        }

        [UnityTest]
        public IEnumerator ShutdownDestroysCachedCloneAndPreservesBorrowedSource()
            => CheckCachedGracefulShutdown().ToCoroutine();

        private async UniTask CheckCachedGracefulShutdown()
        {
            var context = CreateReuse();
            var handle = await context.OpenAsync(new UIOpenRequest("popup"));
            var cachedView = handle.ViewObject;
            await handle.CloseAsync();
            Assert.That(cachedView != null && !cachedView.activeSelf, Is.True);
            var ownerToken = context.LifetimeToken;
            var shutdown = context.ShutdownAsync().AsTask();
            await Wait(shutdown);
            await shutdown;
            await context.ShutdownAsync();
            await handle.CloseAsync();
            Assert.That(cachedView == null, Is.True);
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
            Assert.That(_source != null && _root != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RootDestructionDestroysCachedCloneAndPreservesBorrowedSource()
            => CheckCachedNativeOwnerDestruction().ToCoroutine();

        private async UniTask CheckCachedNativeOwnerDestruction()
        {
            var context = CreateReuse();
            var handle = await context.OpenAsync(new UIOpenRequest("popup"));
            var cachedView = handle.ViewObject;
            await handle.CloseAsync();
            Assert.That(cachedView != null && !cachedView.activeSelf, Is.True);
            var ownerToken = context.LifetimeToken;
            UnityEngine.Object.Destroy(_root);
            for (int frame = 0; frame < 100 && !context.IsDisposed; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(context.IsDisposed, Is.True, "Native owner destruction must end the cache owner.");
            await context.ShutdownAsync();
            await handle.CloseAsync();
            Assert.That(cachedView == null, Is.True);
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.That(context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_source != null, Is.True);
        }
    }
}