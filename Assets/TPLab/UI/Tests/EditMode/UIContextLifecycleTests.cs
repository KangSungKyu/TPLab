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
    public sealed class UIContextLifecycleTests
    {
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UIEditOwner");
            _source = new GameObject("UIEditSource");
            _source.SetActive(false);
        }

        [TearDown]
        public void TearDown()
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
                // The tested completion owns expected errors; native fixture cleanup must still run.
            }
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_source);
            _context = null;
        }

        private UIContext Create()
        {
            _context = new UIContext(_root);
            return _context;
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private void Register(string id = "popup")
            => _context.Register(new UIDefinition(id, _source));

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

        [Test]
        public void ConstructorRejectsNullAndDestroyedOwner()
        {
            Assert.Throws<ArgumentNullException>(() => new UIContext(null));
            UnityEngine.Object.DestroyImmediate(_root);
            Assert.Throws<ArgumentNullException>(() => new UIContext(_root));
        }

        [Test]
        public void RegisterRejectsInvalidAndDuplicateDefinitionsWithoutCreatingDisplays()
        {
            var context = Create();
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition(" ", _source)));
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition("missing-source")));
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition("two-sources", _source, "asset-key")));
            Register();
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition("popup", _source)));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(_source.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator BeginOpenRejectsUnknownAndDuplicateOwnerDefinition() => CheckRequestValidation().ToCoroutine();

        private async UniTask CheckRequestValidation()
        {
            var context = Create();
            Register();
            Assert.Throws<ArgumentException>(() => context.BeginOpen(new UIOpenRequest("unknown")));
            var gate = Gate();
            var hooks = new UIHooks { PrepareAsync = (_, token) => gate.Task.AttachExternalCancellation(token) };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            Assert.Throws<InvalidOperationException>(() => context.BeginOpen(new UIOpenRequest("popup")));
            Assert.That(context.Displays.Count, Is.EqualTo(1));
            gate.TrySetResult();
            await Wait(opened);
            await opened;
            await handle.CloseAsync();
        }

        [UnityTest]
        public IEnumerator BeginOpenPublishesHandleAndSupportsRepeatedOpenedAwaitAndConveniencePath()
            => CheckOpeningObservation().ToCoroutine();

        private async UniTask CheckOpeningObservation()
        {
            var context = Create();
            Register();
            int preparationCount = 0;
            var firstGate = Gate();
            var firstHooks = new UIHooks
            {
                PrepareAsync = (_, token) =>
                {
                    ++preparationCount;
                    return firstGate.Task.AttachExternalCancellation(token);
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: firstHooks));
            var firstWait = handle.Opened.AsTask();
            var secondWait = handle.Opened.AsTask();
            Assert.That(handle.State, Is.EqualTo(UIState.Opening));
            Assert.That(context.Displays, Does.Contain(handle));
            Assert.That(firstWait.IsCompleted, Is.False);
            Assert.That(secondWait.IsCompleted, Is.False);
            firstGate.TrySetResult();
            await Wait(Task.WhenAll(firstWait, secondWait));
            await firstWait;
            await secondWait;
            Assert.That(handle.State, Is.EqualTo(UIState.Visible));
            await handle.CloseAsync();

            var secondGate = Gate();
            var secondHooks = new UIHooks
            {
                PrepareAsync = (_, token) =>
                {
                    ++preparationCount;
                    return secondGate.Task.AttachExternalCancellation(token);
                }
            };
            var convenience = context.OpenAsync(new UIOpenRequest("popup", hooks: secondHooks)).AsTask();
            Assert.That(convenience.IsCompleted, Is.False);
            secondGate.TrySetResult();
            await Wait(convenience);
            var secondHandle = await convenience;
            Assert.That(secondHandle.State, Is.EqualTo(UIState.Visible));
            Assert.That(secondHandle.Id, Is.Not.EqualTo(handle.Id));
            Assert.That(preparationCount, Is.EqualTo(2));
            Assert.That(secondHandle.ViewObject != null, Is.True);
            await secondHandle.CloseAsync();
        }

        [UnityTest]
        public IEnumerator CleanupIsReverseOrderAndExactlyOnceIncludingEarlyRegistrationDispose()
            => CheckCleanupOrder().ToCoroutine();

        private async UniTask CheckCleanupOrder()
        {
            Create();
            Register();
            int closedCount = 0;
            var handle = await _context.OpenAsync(new UIOpenRequest("popup",
                hooks: new UIHooks { Closed = _ => ++closedCount }));
            var trace = new List<string>();
            handle.RegisterCleanup(() => trace.Add("first"));
            var early = handle.RegisterCleanup(() => trace.Add("early"));
            handle.RegisterCleanup(() => trace.Add("last"));
            early.Dispose();
            early.Dispose();
            CollectionAssert.AreEqual(new[] { "early" }, trace);

            var firstClose = handle.CloseAsync().AsTask();
            var secondClose = handle.CloseAsync().AsTask();
            var observation = handle.Closed.AsTask();
            await Wait(Task.WhenAll(firstClose, secondClose, observation));
            await firstClose;
            await secondClose;
            await observation;
            await handle.CloseAsync();
            CollectionAssert.AreEqual(new[] { "early", "last", "first" }, trace);
            Assert.That(closedCount, Is.EqualTo(1));
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.Throws<ObjectDisposedException>(() => handle.RegisterCleanup(() => trace.Add("late")));
            CollectionAssert.AreEqual(new[] { "early", "last", "first" }, trace);
        }

        [UnityTest]
        public IEnumerator ClosingHookAndCleanupFailuresDoNotSkipRemainingCleanupAndShareFailure()
            => CheckCleanupFailure().ToCoroutine();

        private async UniTask CheckCleanupFailure()
        {
            Create();
            Register();
            var hooks = new UIHooks
            {
                CloseAsync = (_, __) => throw new InvalidOperationException("expected-close-hook"),
                Closed = _ => throw new InvalidOperationException("expected-closed-hook")
            };
            var handle = await _context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            var trace = new List<string>();
            handle.RegisterCleanup(() => trace.Add("first"));
            handle.RegisterCleanup(() =>
            {
                trace.Add("broken");
                throw new InvalidOperationException("expected-cleanup");
            });
            handle.RegisterCleanup(() => trace.Add("last"));

            var firstClose = handle.CloseAsync().AsTask();
            var secondClose = handle.CloseAsync().AsTask();
            await Wait(firstClose);
            await Wait(secondClose);
            Assert.That(firstClose.IsFaulted, Is.True);
            Assert.That(secondClose.IsFaulted, Is.True);
            var message = firstClose.Exception.ToString();
            Assert.That(message, Does.Contain("expected-close-hook").And.Contain("expected-cleanup")
                .And.Contain("expected-closed-hook"));
            Assert.That(secondClose.Exception.ToString(), Does.Contain("expected-cleanup"));
            CollectionAssert.AreEqual(new[] { "last", "broken", "first" }, trace);
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(handle.ViewObject == null, Is.True);
        }

        [UnityTest]
        public IEnumerator OpeningCallerCancellationCleansDisplayWithoutEndingOwner()
            => CheckOpeningCancellation().ToCoroutine();

        private async UniTask CheckOpeningCancellation()
        {
            var context = Create();
            Register();
            var gate = Gate();
            int cleanupCount = 0;
            var hooks = new UIHooks
            {
                PrepareAsync = (handle, token) =>
                {
                    handle.RegisterCleanup(() => ++cleanupCount);
                    return gate.Task.AttachExternalCancellation(token);
                }
            };
            using (var cancellation = new CancellationTokenSource())
            {
                var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks), cancellation.Token);
                var opened = handle.Opened.AsTask();
                var closed = handle.Closed.AsTask();
                Assert.That(handle.State, Is.EqualTo(UIState.Opening));
                cancellation.Cancel();
                await Wait(opened);
                await Wait(closed);
                await AssertCancellation(opened);
                Assert.That(handle.Opened.Status, Is.EqualTo(UniTaskStatus.Canceled));
                await closed;
                Assert.That(cleanupCount, Is.EqualTo(1));
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
                Assert.That(context.IsDisposed, Is.False);
                Assert.That(context.LifetimeToken.IsCancellationRequested, Is.False);
                Assert.That(_root != null && _source != null, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator CloseWaitCancellationDoesNotStopCleanupAndShutdownSharesCompletion()
            => CheckWaitCancellationAndShutdown().ToCoroutine();

        private async UniTask CheckWaitCancellationAndShutdown()
        {
            var context = Create();
            Register();
            var gate = Gate();
            int closingCount = 0;
            int cleanupCount = 0;
            var hooks = new UIHooks
            {
                CloseAsync = (_, token) =>
                {
                    ++closingCount;
                    return gate.Task.AttachExternalCancellation(token);
                }
            };
            var handle = await context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            handle.RegisterCleanup(() => ++cleanupCount);
            using (var cancellation = new CancellationTokenSource())
            {
                var cancelledWait = handle.CloseAsync(cancellation.Token).AsTask();
                var actualClose = handle.Closed.AsTask();
                var repeatedClose = handle.CloseAsync().AsTask();
                cancellation.Cancel();
                await Wait(cancelledWait);
                await AssertCancellation(cancelledWait);
                Assert.That(actualClose.IsCompleted, Is.False);
                Assert.That(handle.State, Is.EqualTo(UIState.Closing));
                Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
                gate.TrySetResult();
                await Wait(Task.WhenAll(actualClose, repeatedClose));
                await actualClose;
                await repeatedClose;
            }
            Assert.That(closingCount, Is.EqualTo(1));
            Assert.That(cleanupCount, Is.EqualTo(1));

            var ownerToken = context.LifetimeToken;
            var shutdown = context.ShutdownAsync().AsTask();
            var secondShutdown = context.ShutdownAsync().AsTask();
            await Wait(Task.WhenAll(shutdown, secondShutdown));
            await shutdown;
            await secondShutdown;
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            Assert.Throws<ObjectDisposedException>(() => context.BeginOpen(new UIOpenRequest("popup")));
            Assert.That(_root != null && _source != null, Is.True);
            Assert.That(context.Displays, Is.Empty);
        }

        [UnityTest]
        public IEnumerator SelfClosingHookReentryIsRejectedAndStillRunsCleanup()
            => CheckSelfReentry().ToCoroutine();

        private async UniTask CheckSelfReentry()
        {
            Create();
            Register();
            int closingCount = 0;
            int cleanupCount = 0;
            var hooks = new UIHooks
            {
                CloseAsync = (handle, _) =>
                {
                    ++closingCount;
                    return handle.CloseAsync();
                }
            };
            var handle = await _context.OpenAsync(new UIOpenRequest("popup", hooks: hooks));
            handle.RegisterCleanup(() => ++cleanupCount);
            var closed = handle.CloseAsync().AsTask();
            await Wait(closed);
            Assert.That(closed.IsFaulted, Is.True);
            bool foundReentry = false;
            foreach (var error in closed.Exception.Flatten().InnerExceptions)
            {
                foundReentry |= error is InvalidOperationException;
            }
            Assert.That(foundReentry, Is.True, closed.Exception.ToString());
            Assert.That(closingCount, Is.EqualTo(1));
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
        }
    }
}