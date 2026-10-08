// P4 input contracts; actual Red evidence is retained independently of this fixture refinement.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests
{
    public sealed class UIContextInputContractTests
    {
        private GameObject _root;
        private GameObject _source;
        private EventSystem _events;
        private readonly List<UIContext> _contexts = new List<UIContext>();
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();
        private readonly List<UniTaskCompletionSource<bool>> _decisions = new List<UniTaskCompletionSource<bool>>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("P4 Edit owner", typeof(EventSystem));
            _events = _root.GetComponent<EventSystem>();
            _source = new GameObject("P4 Edit source");
            _source.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var gate in _gates)
            {
                gate.TrySetResult();
            }
            foreach (var decision in _decisions)
            {
                decision.TrySetResult(false);
            }
            foreach (var context in _contexts)
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception)
                {
                    // The case observes expected listener/hook errors before fixture retirement.
                }
            }
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_source);
            _contexts.Clear();
            _gates.Clear();
            _decisions.Clear();
        }

        private UIContext Create(Func<IDisposable> acquireModalBlock = null)
        {
            var context = new UIContext(_root, acquireModalBlock: acquireModalBlock, eventSystem: _events);
            context.Register(new UIDefinition("view", _source));
            _contexts.Add(context);
            return context;
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private UniTaskCompletionSource<bool> Decision()
        {
            var decision = new UniTaskCompletionSource<bool>();
            _decisions.Add(decision);
            return decision;
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

        private static async UniTask ExpectCancellation(Task task)
        {
            await Wait(task);
            Exception error = null;
            try
            {
                await task;
            }
            catch (Exception failure)
            {
                error = failure;
            }
            Assert.That(error, Is.InstanceOf<OperationCanceledException>());
        }

        private static async UniTask ExpectInvalid(Func<UniTask<bool>> operation)
        {
            Exception error = null;
            try
            {
                var task = operation().AsTask();
                await Wait(task);
                await task;
            }
            catch (Exception failure)
            {
                error = failure;
            }
            Assert.That(error, Is.InstanceOf<InvalidOperationException>());
        }

        [UnityTest]
        public IEnumerator OpeningAndRetiredGenerationRejectInputCommands()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var context = Create();
                var gate = Gate();
                var handle = context.BeginOpen(new UIOpenRequest("view", hooks: new UIHooks
                {
                    PrepareAsync = (preparing, token) =>
                    {
                        preparing.SetFocus(preparing.ViewObject);
                        Assert.That(_events.currentSelectedGameObject, Is.Null, "Opening remembers focus without native selection.");
                        return gate.Task.AttachExternalCancellation(token);
                    }
                }));
                Assert.That(handle.CanReceiveInput, Is.False);
                Assert.Throws<InvalidOperationException>(() => handle.SetInputMode(UIInputMode.Modal));
                Assert.Throws<InvalidOperationException>(handle.BringToFront);
                Assert.Throws<ArgumentException>(() => handle.SetFocus(_root));
                Assert.That(_events.currentSelectedGameObject, Is.Null);
                await ExpectInvalid(() => handle.RequestCloseAsync(UIUserCloseReason.Button));
                gate.TrySetResult();
                await Complete(handle.Opened);
                Assert.That(_events.currentSelectedGameObject, Is.SameAs(handle.ViewObject));
                Assert.That(handle.InputMode, Is.EqualTo(UIInputMode.Modeless));
                Assert.That(handle.CanReceiveInput, Is.True);
                Assert.Catch<ArgumentException>(() => handle.SetInputMode((UIInputMode)999));
                Assert.Catch<ArgumentException>(() => handle.SetFocus(_root));
                await Complete(handle.CloseAsync());
                Assert.That(handle.CanReceiveInput, Is.False);
                Assert.Throws<InvalidOperationException>(() => handle.SetInputMode(UIInputMode.Modal));
                Assert.Throws<InvalidOperationException>(handle.BringToFront);
                await ExpectInvalid(() => handle.RequestCloseAsync(UIUserCloseReason.Cancel));
                var withoutEvents = new UIContext(_root);
                _contexts.Add(withoutEvents);
                withoutEvents.Register(new UIDefinition("unconnected", _source));
                var disconnected = await Open(withoutEvents, new UIOpenRequest("unconnected"));
                Assert.Throws<InvalidOperationException>(() => disconnected.SetFocus(disconnected.ViewObject),
                    "SetFocus requires an explicit borrowed EventSystem, even when an unrelated current system exists.");
                disconnected.SetInputMode(UIInputMode.Modal);
                Assert.That(disconnected.CanReceiveInput, Is.True, "Native UI-only eligibility does not require a game-input adapter.");
                await Complete(disconnected.CloseAsync());
            });
        }

        [UnityTest]
        public IEnumerator UserCloseReasonsVetoWithoutChangingDisplayAndForceCloseIgnoresVeto()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var context = Create();
                var noOptIn = await Open(context, new UIOpenRequest("view"));
                Assert.That(await noOptIn.RequestCloseAsync(UIUserCloseReason.Button), Is.False);
                Assert.That(noOptIn.State, Is.EqualTo(UIState.Visible));
                await Complete(noOptIn.CloseAsync());
                var reasons = new List<UIUserCloseReason>();
                int cleanup = 0;
                var handle = await Open(context, new UIOpenRequest("view", hooks: new UIHooks
                {
                    CanCloseAsync = (_, reason, __) =>
                    {
                        reasons.Add(reason);
                        return UniTask.FromResult(false);
                    }
                }));
                handle.RegisterCleanup(() => ++cleanup);
                foreach (var reason in new[] { UIUserCloseReason.Cancel, UIUserCloseReason.OutsidePointer, UIUserCloseReason.Button })
                {
                    Assert.That(await handle.RequestCloseAsync(reason), Is.False);
                    Assert.That(handle.State, Is.EqualTo(UIState.Visible));
                    Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.False);
                }
                Assert.That(reasons.Count, Is.EqualTo(3));
                await Complete(handle.CloseAsync());
                Assert.That(reasons.Count, Is.EqualTo(3), "Force Close must not consult CanClose again.");
                Assert.That(cleanup, Is.EqualTo(1));
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            });
        }

        [UnityTest]
        public IEnumerator AcceptedUserCloseWaitsForSharedNativeCloseCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var context = Create();
                var closeGate = Gate();
                var handle = await Open(context, new UIOpenRequest("view", hooks: new UIHooks
                {
                    CanCloseAsync = (_, __, ___) => UniTask.FromResult(true),
                    CloseAsync = (_, token) => closeGate.Task.AttachExternalCancellation(token)
                }));
                var request = handle.RequestCloseAsync(UIUserCloseReason.Button).AsTask();
                Assert.That(handle.State, Is.EqualTo(UIState.Closing));
                Assert.That(request.IsCompleted, Is.False);
                var repeatedClose = handle.CloseAsync().AsTask();
                closeGate.TrySetResult();
                await Wait(request);
                Assert.That(await request, Is.True);
                await repeatedClose;
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                Assert.That(handle.ViewObject, Is.Null);
            });
        }

        [UnityTest]
        public IEnumerator PendingUserRequestRejectsConcurrentRequestAndPropagatesCallerCancellation()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var context = Create();
                var decision = Decision();
                using var caller = new CancellationTokenSource();
                CancellationToken vetoToken = default;
                var handle = await Open(context, new UIOpenRequest("view", hooks: new UIHooks
                {
                    CanCloseAsync = (_, __, token) =>
                    {
                        vetoToken = token;
                        return decision.Task.AttachExternalCancellation(token);
                    }
                }));
                var pending = handle.RequestCloseAsync(UIUserCloseReason.Cancel, caller.Token).AsTask();
                await ExpectInvalid(() => handle.RequestCloseAsync(UIUserCloseReason.Button));
                caller.Cancel();
                await ExpectCancellation(pending);
                Assert.That(handle.State, Is.EqualTo(UIState.Visible));
                Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.False);
                // Resolve the project decision as veto; this case does not invent late-approval semantics.
                decision.TrySetResult(false);
                await Complete(handle.CloseAsync());
                Assert.That(vetoToken.IsCancellationRequested, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator ForceCloseAndOwnerShutdownCancelOutstandingProjectVeto()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var context = Create();
                for (int ownerShutdown = 0; ownerShutdown < 2; ++ownerShutdown)
                {
                    var decision = Decision();
                    CancellationToken vetoToken = default;
                    var handle = await Open(context, new UIOpenRequest("view", hooks: new UIHooks
                    {
                        CanCloseAsync = (_, __, token) =>
                        {
                            vetoToken = token;
                            return decision.Task.AttachExternalCancellation(token);
                        }
                    }));
                    var pending = handle.RequestCloseAsync(UIUserCloseReason.OutsidePointer).AsTask();
                    await Complete(ownerShutdown == 0 ? handle.CloseAsync() : context.ShutdownAsync());
                    await ExpectCancellation(pending);
                    Assert.That(vetoToken.IsCancellationRequested, Is.True);
                    Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                    decision.TrySetResult(true);
                    Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                }
                Assert.That(_root != null && _source != null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator OrdinaryListenerFailureContinuesNotificationsCleanupAndLeaseRetirement()
        {
            return UniTask.ToCoroutine(async () =>
            {
                int leases = 0;
                var context = Create(() =>
                {
                    ++leases;
                    return new Lease(() => --leases);
                });
                int nextListener = 0;
                int cleanup = 0;
                context.DisplayChanged += handle =>
                {
                    if (handle.State == UIState.Closing)
                    {
                        Assert.Throws<InvalidOperationException>(() => context.BeginOpen(new UIOpenRequest("view")));
                        throw new InvalidOperationException("expected-project-listener-failure");
                    }
                };
                context.DisplayChanged += handle =>
                {
                    if (handle.State == UIState.Closing)
                    {
                        ++nextListener;
                    }
                };
                var handle = await Open(context, new UIOpenRequest("view", inputMode: UIInputMode.Modal));
                handle.RegisterCleanup(() => ++cleanup);
                Exception error = null;
                try
                {
                    await Complete(handle.CloseAsync());
                }
                catch (Exception failure)
                {
                    error = failure;
                }
                Assert.That(error, Is.Not.Null);
                Assert.That(error.ToString(), Does.Contain("expected-project-listener-failure"));
                Assert.That(nextListener, Is.EqualTo(1));
                Assert.That(cleanup, Is.EqualTo(1));
                Assert.That(leases, Is.Zero);
                Assert.That(handle.State, Is.EqualTo(UIState.Closed));
                Assert.That(context.Fault, Is.Null);
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