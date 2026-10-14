using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests
{
    public sealed class UIContextSafetyNetBPlayModeTests
    {
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();
        private readonly List<Task> _hooks = new List<Task>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UISafetyNetBOwner");
            _source = new GameObject("UISafetyNetBSource");
            _source.SetActive(false);
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("first", _source));
            _context.Register(new UIDefinition("second", _source));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return CleanupAsync().ToCoroutine();
            LogAssert.NoUnexpectedReceived();
        }

        private async UniTask CleanupAsync()
        {
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            try
            {
                foreach (var hook in _hooks)
                {
                    await Wait(hook);
                    await hook;
                }
                if (_context != null)
                {
                    var shutdown = _context.ShutdownAsync().AsTask();
                    await Wait(shutdown);
                    try
                    {
                        await shutdown;
                    }
                    catch (AggregateException)
                    {
                        // The test observes intentional callback errors; fixture retirement must still finish.
                    }
                }
            }
            finally
            {
                var root = _root;
                var source = _source;
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(source);
                _root = null;
                _source = null;
                _context = null;
                _gates.Clear();
                _hooks.Clear();
                await UniTask.NextFrame();
                Assert.That(root == null && source == null, Is.True, "Fixture objects must retire before the next case.");
            }
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private Task HoldHook(UniTaskCompletionSource gate)
        {
            var task = gate.Task.AsTask();
            _hooks.Add(task);
            return task;
        }

        private static async UniTask Wait(Task task)
        {
            var timer = Stopwatch.StartNew();
            for (int frame = 0; frame < 100 && !task.IsCompleted && timer.ElapsedMilliseconds < 10000; ++frame)
            {
                await UniTask.NextFrame();
            }
            Assert.That(task.IsCompleted, Is.True, "Completion must settle within 100 yields and 10 seconds.");
        }

        private static Exception Error(Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return null;
            }
            catch (Exception error)
            {
                return error;
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator NonCooperativeOpeningCancelsAndRetiresBeforeHookCompletes()
        {
            return CheckOpening().ToCoroutine();
        }

        private async UniTask CheckOpening()
        {
            var gate = Gate();
            Task hook = null;
            CancellationToken hookToken = default;
            int cleanupCount = 0;
            int closedCount = 0;
            using (var caller = new CancellationTokenSource())
            {
                try
                {
                    var handle = _context.BeginOpen(new UIOpenRequest("first", hooks: new UIHooks
                    {
                        OpenAsync = (current, token) =>
                        {
                            hookToken = token;
                            current.RegisterCleanup(() => ++cleanupCount);
                            hook = HoldHook(gate);
                            return hook.AsUniTask();
                        },
                        Closed = _ => ++closedCount
                    }), caller.Token);
                    var opened = handle.Opened.AsTask();
                    var view = handle.ViewObject;
                    Assert.That(hook, Is.Not.Null, "The direct-source open hook must have started.");
                    Assert.That(handle.State, Is.EqualTo(UIState.Opening));
                    Assert.That(opened.IsCompleted || hook.IsCompleted, Is.False);
                    Assert.That(view != null, Is.True);
                    caller.Cancel();
                    var closed = handle.Closed.AsTask();
                    await Wait(opened);
                    Assert.That(closed.IsCompleted, Is.True, "Opened cancellation is published after display cleanup completes.");
                    Assert.That(view == null, Is.True, "Opened cancellation must observe native retirement.");
                    await Wait(closed);
                    Assert.That(Error(opened), Is.InstanceOf<OperationCanceledException>());
                    Assert.That(Error(closed), Is.Null);
                    Assert.That(hookToken.IsCancellationRequested, Is.True);
                    Assert.That(hook.IsCompleted, Is.False, "Core cancellation must not depend on project gate release.");
                    Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                    Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
                    Assert.That(view == null, Is.True);
                    Assert.That(cleanupCount, Is.EqualTo(1));
                    Assert.That(closedCount, Is.EqualTo(1));
                    Assert.That(_context.IsDisposed || _context.LifetimeToken.IsCancellationRequested, Is.False);
                    Assert.That(_context.Fault, Is.Null);
                    Assert.That(_context.Displays, Is.Empty);
                    var next = await _context.OpenAsync(new UIOpenRequest("second"));
                    Assert.That(next.State, Is.EqualTo(UIState.Visible));
                    var nextClose = next.CloseAsync().AsTask();
                    await Wait(nextClose);
                    await nextClose;
                }
                finally
                {
                    gate.TrySetResult();
                    if (hook != null)
                    {
                        await Wait(hook);
                        await hook;
                    }
                }
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator OwnerShutdownRetiresWhileNonCooperativeCloseHookRemainsPending()
        {
            return CheckOwnerShutdown().ToCoroutine();
        }

        private async UniTask CheckOwnerShutdown()
        {
            var gate = Gate();
            Task hook = null;
            CancellationToken hookToken = default;
            int cleanupCount = 0;
            int closedCount = 0;
            try
            {
                var handle = await _context.OpenAsync(new UIOpenRequest("first", hooks: new UIHooks
                {
                    CloseAsync = (_, token) =>
                    {
                        hookToken = token;
                        hook = HoldHook(gate);
                        return hook.AsUniTask();
                    },
                    Closed = _ => ++closedCount
                }));
                var view = handle.ViewObject;
                handle.RegisterCleanup(() => ++cleanupCount);
                var closed = handle.CloseAsync().AsTask();
                Assert.That(hook, Is.Not.Null);
                Assert.That(handle.State, Is.EqualTo(UIState.Closing));
                Assert.That(closed.IsCompleted || hook.IsCompleted || hookToken.IsCancellationRequested, Is.False);
                var shutdown = _context.ShutdownAsync().AsTask();
                var repeatedShutdown = _context.ShutdownAsync().AsTask();
                await Wait(shutdown);
                await Wait(repeatedShutdown);
                await Wait(closed);
                Assert.That(Error(shutdown), Is.Null);
                Assert.That(Error(repeatedShutdown), Is.Null);
                Assert.That(Error(closed), Is.Null);
                Assert.That(hook.IsCompleted, Is.False, "Owner cancellation detaches the core wait without completing the hook.");
                Assert.That(hookToken.IsCancellationRequested, Is.True);
                Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
                Assert.That(_context.IsDisposed && _context.LifetimeToken.IsCancellationRequested, Is.True);
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
                Assert.That(view == null, Is.True);
                Assert.That(cleanupCount, Is.EqualTo(1));
                Assert.That(closedCount, Is.EqualTo(1));
                Assert.That(_context.Displays, Is.Empty);
                Assert.That(_context.Fault, Is.Null);
                Assert.That(_root != null && _source != null, Is.True);
            }
            finally
            {
                gate.TrySetResult();
                if (hook != null)
                {
                    await Wait(hook);
                    await hook;
                }
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ShutdownAggregatesEachDisplayFailureInAcceptanceOrderAfterRetirement()
        {
            return CheckMultipleFailures().ToCoroutine();
        }

        private async UniTask CheckMultipleFailures()
        {
            var firstCleanup = new InvalidOperationException("first-cleanup");
            var firstClosed = new ApplicationException("first-closed");
            var secondCleanup = new InvalidOperationException("second-cleanup");
            var secondClosed = new ApplicationException("second-closed");
            int cleanupCount = 0;
            int observerCount = 0;
            var first = await _context.OpenAsync(new UIOpenRequest("first", hooks: new UIHooks
            {
                Closed = _ => { ++observerCount; throw firstClosed; }
            }));
            var second = await _context.OpenAsync(new UIOpenRequest("second", hooks: new UIHooks
            {
                Closed = _ => { ++observerCount; throw secondClosed; }
            }));
            first.RegisterCleanup(() => { ++cleanupCount; throw firstCleanup; });
            second.RegisterCleanup(() => { ++cleanupCount; throw secondCleanup; });
            var firstView = first.ViewObject;
            var secondView = second.ViewObject;
            var firstCompletion = first.Closed.AsTask();
            var secondCompletion = second.Closed.AsTask();
            var shutdown = _context.ShutdownAsync().AsTask();
            var repeatedShutdown = _context.ShutdownAsync().AsTask();
            await Wait(shutdown);
            await Wait(repeatedShutdown);
            await Wait(firstCompletion);
            await Wait(secondCompletion);
            Assert.That(Error(firstCompletion), Is.TypeOf<AggregateException>());
            Assert.That(Error(secondCompletion), Is.TypeOf<AggregateException>());
            Assert.That(Error(shutdown), Is.TypeOf<AggregateException>());
            var firstError = (AggregateException)Error(firstCompletion);
            var secondError = (AggregateException)Error(secondCompletion);
            var ownerError = (AggregateException)Error(shutdown);
            Assert.That(ownerError.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(ownerError.InnerExceptions[0], Is.SameAs(firstError));
            Assert.That(ownerError.InnerExceptions[1], Is.SameAs(secondError));
            Assert.That(firstError.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(firstError.InnerExceptions[0], Is.SameAs(firstCleanup));
            Assert.That(firstError.InnerExceptions[1], Is.SameAs(firstClosed));
            Assert.That(secondError.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(secondError.InnerExceptions[0], Is.SameAs(secondCleanup));
            Assert.That(secondError.InnerExceptions[1], Is.SameAs(secondClosed));
            Assert.That(Error(repeatedShutdown), Is.SameAs(ownerError));
            Assert.That(Error(first.CloseAsync().AsTask()), Is.SameAs(firstError));
            Assert.That(Error(second.CloseAsync().AsTask()), Is.SameAs(secondError));
            Assert.That(cleanupCount, Is.EqualTo(2));
            Assert.That(observerCount, Is.EqualTo(2));
            foreach (var handle in new[] { first, second })
            {
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
                Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
            }
            Assert.That(firstView == null && secondView == null, Is.True);
            Assert.That(_context.IsDisposed && _context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(_context.Displays, Is.Empty);
            Assert.That(_context.CurrentHud, Is.Null);
            Assert.That(_context.Fault, Is.Null);
            Assert.That(_root != null && _source != null, Is.True);
        }
    }
}
