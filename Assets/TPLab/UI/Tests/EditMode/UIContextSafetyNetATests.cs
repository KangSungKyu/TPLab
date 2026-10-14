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
    public sealed class UIContextSafetyNetATests
    {
        public enum WorkerMember
        {
            Constructor, Displays, CurrentHud, RegisterHost, Register, PrepareAsync,
            BeginOpen, OpenAsync, SelectHudAsync, ShutdownAsync, Dispose
        }

        public enum PriorityPair
        {
            WorkerDisposed, WorkerFault, WorkerMutation,
            MutationDisposed, MutationFault, DisposedFault
        }

        private static readonly WorkerMember[] WorkerMembers =
            (WorkerMember[])Enum.GetValues(typeof(WorkerMember));
        private static readonly PriorityPair[] PriorityPairs =
            (PriorityPair[])Enum.GetValues(typeof(PriorityPair));
        private const string ThreadMessage = "UI operations require Unity's main thread.";
        private const string MutationMessage =
            "Cleanup, native application, or read-only notification cannot reenter lifecycle commands.";
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;
        private readonly List<UniTaskCompletionSource> _gates = new List<UniTaskCompletionSource>();
        private readonly List<UIContext> _unexpectedContexts = new List<UIContext>();

        private sealed class WorkerObservation
        {
            public Exception Error;
            public bool Joined;
            public bool Pending;
            public UIContext CreatedContext;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UISafetyNetOwner");
            _source = new GameObject("UISafetyNetSource");
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
            foreach (var context in _unexpectedContexts)
            {
                DisposeForCleanup(context);
            }
            _unexpectedContexts.Clear();
            DisposeForCleanup(_context);
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_source);
            _context = null;
        }

        private static void DisposeForCleanup(UIContext context)
        {
            try
            {
                context?.Dispose();
            }
            catch (Exception)
            {
                // Completion assertions own failures; borrowed fixture objects still need cleanup.
            }
        }

        private UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        private static async UniTask Wait(Task task)
        {
            var clock = Stopwatch.StartNew();
            for (int frame = 0; frame < 100 && !task.IsCompleted && clock.ElapsedMilliseconds < 10000; ++frame)
            {
                await UniTask.Yield();
            }
            Assert.That(task.IsCompleted, Is.True, "Completion exceeded 100 yields or 10 seconds.");
        }

        private static async UniTask Complete(Task task)
        {
            await Wait(task);
            task.GetAwaiter().GetResult();
        }

        private static WorkerObservation OnWorker(Action<WorkerObservation> action)
        {
            var observation = new WorkerObservation();
            var thread = new Thread(() =>
            {
                try
                {
                    action(observation);
                }
                catch (Exception error)
                {
                    observation.Error = error;
                }
            }) { IsBackground = true };
            thread.Start();
            observation.Joined = thread.Join(3000);
            return observation;
        }

        private static void Observe(UniTask operation, WorkerObservation observation)
        {
            var awaiter = operation.GetAwaiter();
            observation.Pending = !awaiter.IsCompleted;
            if (!observation.Pending)
            {
                awaiter.GetResult();
            }
        }

        private static void Observe(UniTask<UIHandle> operation, WorkerObservation observation)
        {
            var awaiter = operation.GetAwaiter();
            observation.Pending = !awaiter.IsCompleted;
            if (!observation.Pending)
            {
                awaiter.GetResult();
            }
        }

        private static void AssertWorkerRejected(WorkerObservation observation)
        {
            Assert.That(observation, Is.Not.Null);
            Assert.That(observation.Joined, Is.True, "Worker did not join within 3000ms.");
            Assert.That(observation.Pending, Is.False, "A rejected worker command must not leave pending work.");
            Assert.That(observation.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(observation.Error.Message, Is.EqualTo(ThreadMessage));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator WorkerBoundaryRejectsBeforeNativeOrOwnerMutation(
            [ValueSource(nameof(WorkerMembers))] WorkerMember member)
        {
            return CheckWorkerMember(member).ToCoroutine();
        }

        private async UniTask CheckWorkerMember(WorkerMember member)
        {
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("popup", _source));
            _context.Register(new UIDefinition("hud", _source, role: UIRole.Hud));
            var registration = new UIDefinition("worker-registration", _source);
            var popup = new UIOpenRequest("popup");
            var hud = new UIOpenRequest("hud");
            Transform host = _root.transform;
            int childrenBefore = host.childCount;
            var observation = OnWorker(result =>
            {
                switch (member)
                {
                    case WorkerMember.Constructor: result.CreatedContext = new UIContext(_root); break;
                    case WorkerMember.Displays: _ = _context.Displays; break;
                    case WorkerMember.CurrentHud: _ = _context.CurrentHud; break;
                    case WorkerMember.RegisterHost: _context.RegisterHost("worker-host", host); break;
                    case WorkerMember.Register: _context.Register(registration); break;
                    case WorkerMember.PrepareAsync: Observe(_context.PrepareAsync("popup"), result); break;
                    case WorkerMember.BeginOpen: _context.BeginOpen(popup); break;
                    case WorkerMember.OpenAsync: Observe(_context.OpenAsync(popup), result); break;
                    case WorkerMember.SelectHudAsync: Observe(_context.SelectHudAsync(hud), result); break;
                    case WorkerMember.ShutdownAsync: Observe(_context.ShutdownAsync(), result); break;
                    case WorkerMember.Dispose: _context.Dispose(); break;
                    default: throw new ArgumentOutOfRangeException(nameof(member));
                }
            });
            if (observation.CreatedContext != null)
            {
                _unexpectedContexts.Add(observation.CreatedContext);
            }
            AssertWorkerRejected(observation);
            Assert.That(_context.IsDisposed, Is.False);
            Assert.That(_context.Fault, Is.Null);
            Assert.That(_context.LifetimeToken.IsCancellationRequested, Is.False);
            Assert.That(_context.Displays, Is.Empty);
            Assert.That(_context.CurrentHud, Is.Null);
            Assert.That(host.childCount, Is.EqualTo(childrenBefore));
            Assert.That(_source.activeSelf, Is.False);
            Assert.That(_root != null, Is.True);
            // Reusing the rejected IDs also checks that registration never committed.
            _context.RegisterHost("worker-host", host);
            _context.Register(registration);
            await Complete(_context.PrepareAsync("popup").AsTask());
            var handle = _context.BeginOpen(popup);
            await Complete(handle.Opened.AsTask());
            Assert.That(handle.State, Is.EqualTo(UIState.Visible));
            await Complete(handle.CloseAsync().AsTask());
            await Complete(_context.ShutdownAsync().AsTask());
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ValidationPriorityMatchesCurrentContract(
            [ValueSource(nameof(PriorityPairs))] PriorityPair pair)
        {
            return CheckPriority(pair).ToCoroutine();
        }

        private async UniTask CheckPriority(PriorityPair pair)
        {
            bool faultCase = pair == PriorityPair.WorkerFault || pair == PriorityPair.MutationFault
                || pair == PriorityPair.DisposedFault;
            var sentinel = new InvalidOperationException("SafetyNet modal factory sentinel");
            _context = faultCase
                ? new UIContext(_root, acquireModalBlock: () => throw sentinel)
                : new UIContext(_root);
            _context.Register(new UIDefinition("popup", _source));
            var probe = new UIDefinition("priority-probe", _source);
            Exception callbackError = null;
            WorkerObservation callbackWorker = null;
            int callbackCount = 0;
            bool disposedAtCallback = false;
            Exception faultAtCallback = null;
            UIState stateBefore = default;
            UIState stateAfter = default;
            int countBefore = -1;
            int countAfter = -1;
            Action<UIHandle> listener = handle =>
            {
                if (handle.State != UIState.Closing || callbackCount != 0)
                {
                    return;
                }
                ++callbackCount;
                disposedAtCallback = _context.IsDisposed;
                faultAtCallback = _context.Fault;
                stateBefore = handle.State;
                countBefore = _context.Displays.Count;
                if (pair == PriorityPair.WorkerMutation)
                {
                    callbackWorker = OnWorker(_ => _context.Register(probe));
                }
                else
                {
                    try
                    {
                        _context.Register(probe);
                    }
                    catch (Exception error)
                    {
                        callbackError = error;
                    }
                }
                stateAfter = handle.State;
                countAfter = _context.Displays.Count;
            };
            bool mutationCase = pair == PriorityPair.WorkerMutation || pair == PriorityPair.MutationDisposed
                || pair == PriorityPair.MutationFault;
            if (mutationCase)
            {
                _context.DisplayChanged += listener;
            }
            try
            {
                if (faultCase)
                {
                    var failed = _context.BeginOpen(new UIOpenRequest("popup", inputMode: UIInputMode.Modal));
                    Task opening = failed.Opened.AsTask();
                    await Wait(opening);
                    Exception openingError = null;
                    try
                    {
                        opening.GetAwaiter().GetResult();
                    }
                    catch (Exception error)
                    {
                        openingError = error;
                    }
                    Assert.That(ContainsIdentity(openingError, sentinel), Is.True);
                    Assert.That(_context.Fault, Is.SameAs(sentinel));
                    // Closing may carry the same native fault; observe that result without replacing it.
                    Task closed = failed.Closed.AsTask();
                    await Wait(closed);
                    try
                    {
                        closed.GetAwaiter().GetResult();
                    }
                    catch (Exception error)
                    {
                        Assert.That(ContainsIdentity(error, sentinel), Is.True);
                    }
                    Assert.That(failed.State, Is.EqualTo(UIState.Closed));
                    Assert.That(_context.Displays, Is.Empty);
                    Assert.That(_context.IsDisposed, Is.False);
                }
                else if (mutationCase)
                {
                    var handle = _context.BeginOpen(new UIOpenRequest("popup"));
                    await Complete(handle.Opened.AsTask());
                    Task termination = pair == PriorityPair.MutationDisposed
                        ? _context.ShutdownAsync().AsTask() : handle.CloseAsync().AsTask();
                    await Complete(termination);
                }
                if (pair == PriorityPair.WorkerDisposed || pair == PriorityPair.DisposedFault)
                {
                    await Complete(_context.ShutdownAsync().AsTask());
                    Assert.That(_context.IsDisposed, Is.True);
                }
                if (pair == PriorityPair.WorkerDisposed || pair == PriorityPair.WorkerFault)
                {
                    bool disposedBefore = _context.IsDisposed;
                    Exception faultBefore = _context.Fault;
                    AssertWorkerRejected(OnWorker(_ => _context.Register(probe)));
                    Assert.That(_context.IsDisposed, Is.EqualTo(disposedBefore));
                    Assert.That(_context.Fault, Is.SameAs(faultBefore));
                    Assert.That(_context.Displays, Is.Empty);
                }
                else if (pair == PriorityPair.DisposedFault)
                {
                    var error = Assert.Throws<ObjectDisposedException>(() => _context.Register(probe));
                    Assert.That(error.ObjectName, Is.EqualTo(nameof(UIContext)));
                    Assert.That(_context.Fault, Is.SameAs(sentinel));
                    Assert.That(_context.IsDisposed, Is.True);
                    Assert.That(_context.Displays, Is.Empty);
                }
                else
                {
                    Assert.That(callbackCount, Is.EqualTo(1));
                    Assert.That(stateBefore, Is.EqualTo(UIState.Closing));
                    Assert.That(stateAfter, Is.EqualTo(stateBefore));
                    Assert.That(countBefore, Is.EqualTo(1));
                    Assert.That(countAfter, Is.EqualTo(countBefore));
                    Assert.That(disposedAtCallback, Is.EqualTo(pair == PriorityPair.MutationDisposed));
                    if (pair == PriorityPair.MutationFault)
                    {
                        Assert.That(faultAtCallback, Is.SameAs(sentinel));
                        Assert.That(_context.Fault, Is.SameAs(sentinel));
                    }
                    else
                    {
                        Assert.That(faultAtCallback, Is.Null);
                        Assert.That(_context.Fault, Is.Null);
                    }
                    Assert.That(_context.IsDisposed, Is.EqualTo(pair == PriorityPair.MutationDisposed));
                    if (pair == PriorityPair.WorkerMutation)
                    {
                        AssertWorkerRejected(callbackWorker);
                    }
                    else
                    {
                        Assert.That(callbackError, Is.TypeOf<InvalidOperationException>());
                        Assert.That(callbackError.Message, Is.EqualTo(MutationMessage));
                    }
                    if (pair == PriorityPair.WorkerMutation)
                    {
                        _context.Register(probe);
                    }
                }
                Assert.That(_source.activeSelf, Is.False);
                Assert.That(_root != null, Is.True);
            }
            finally
            {
                _context.DisplayChanged -= listener;
            }
        }

        private static bool ContainsIdentity(Exception error, Exception expected)
        {
            if (ReferenceEquals(error, expected))
            {
                return true;
            }
            if (error is AggregateException aggregate)
            {
                foreach (Exception inner in aggregate.InnerExceptions)
                {
                    if (ContainsIdentity(inner, expected))
                    {
                        return true;
                    }
                }
            }
            return error?.InnerException != null && ContainsIdentity(error.InnerException, expected);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DisplaysIsDetachedAcceptanceOrderSnapshot()
        {
            return CheckSnapshot().ToCoroutine();
        }

        private async UniTask CheckSnapshot()
        {
            _context = new UIContext(_root);
            foreach (string id in new[] { "a", "b", "c" })
            {
                _context.Register(new UIDefinition(id, _source));
            }
            var prepare = Gate();
            var close = Gate();
            try
            {
                var a = _context.BeginOpen(new UIOpenRequest("a", hooks: new UIHooks
                {
                    PrepareAsync = (_, __) => prepare.Task
                }));
                var b = _context.BeginOpen(new UIOpenRequest("b"));
                await Complete(b.Opened.AsTask());
                var c = _context.BeginOpen(new UIOpenRequest("c", hooks: new UIHooks
                {
                    CloseAsync = (_, __) => close.Task
                }));
                await Complete(c.Opened.AsTask());
                Task cClosed = c.CloseAsync().AsTask();
                var snapshot = _context.Displays;
                Assert.That(snapshot, Is.EqualTo(new[] { a, b, c }));
                Assert.That(a.State, Is.EqualTo(UIState.Opening));
                Assert.That(b.State, Is.EqualTo(UIState.Visible));
                Assert.That(c.State, Is.EqualTo(UIState.Closing));
                Assert.That(cClosed.IsCompleted, Is.False);
                Assert.Throws<NotSupportedException>(() => ((IList<UIHandle>)snapshot).Add(a));
                prepare.TrySetResult();
                await Complete(a.Opened.AsTask());
                b.BringToFront();
                Assert.That(b.ViewObject.transform.parent.GetSiblingIndex(),
                    Is.GreaterThan(a.ViewObject.transform.parent.GetSiblingIndex()));
                Assert.That(_context.Displays, Is.EqualTo(new[] { a, b, c }));
                close.TrySetResult();
                await Complete(cClosed);
                Assert.That(_context.Displays, Is.EqualTo(new[] { a, b }));
                Assert.That(snapshot, Is.EqualTo(new[] { a, b, c }));
                Assert.That(snapshot[2].State, Is.EqualTo(UIState.Closed));
                await Complete(_context.ShutdownAsync().AsTask());
                Assert.That(_context.Displays, Is.Empty);
                Assert.That(snapshot, Is.EqualTo(new[] { a, b, c }));
                Assert.That(_source.activeSelf, Is.False);
                Assert.That(_root != null, Is.True);
            }
            finally
            {
                prepare.TrySetResult();
                close.TrySetResult();
            }
        }
    }
}
