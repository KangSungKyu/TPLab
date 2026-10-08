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
    public sealed class UIContextLoadingTests
    {
        private GameObject _root;
        private GameObject _source;
        private readonly List<UIContext> _contexts = new List<UIContext>();
        private readonly List<UniTaskCompletionSource<GameObject>> _loads = new List<UniTaskCompletionSource<GameObject>>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UILoadingOwner");
            _source = new GameObject("UILoadingSource");
            _source.SetActive(false);
            _source.AddComponent<CanvasGroup>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var load in _loads)
            {
                load.TrySetResult(_source);
            }
            _loads.Clear();
            foreach (var context in _contexts)
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception)
                {
                    // Expected completion errors belong to the test; native fixture cleanup remains obligatory.
                }
            }
            _contexts.Clear();
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_source);
        }

        private UIContext Create(Func<string, CancellationToken, UniTask<GameObject>> provider = null)
        {
            var context = new UIContext(_root, loadPrefab: provider);
            _contexts.Add(context);
            return context;
        }

        private int CountNativeViews()
        {
            return _root.GetComponentsInChildren<CanvasGroup>(true).Length;
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

        [Test]
        public void ProviderDefinitionRequiresConfiguredLoaderAndExplicitSource()
        {
            var provider = new BorrowedProvider((_, __) => UniTask.FromResult(_source));
            var context = Create(provider.Load);
            context.Register(new UIDefinition("provider", assetKey: "ui/provider"));
            Assert.That(provider.CallCount, Is.Zero, "Register must not preload or instantiate.");
            Assert.That(context.Displays, Is.Empty);
            Assert.That(CountNativeViews(), Is.Zero);
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition("blank", assetKey: " ")));
            Assert.Throws<ArgumentException>(() => context.Register(new UIDefinition("both", _source, "ui/provider")));
            var missingProvider = Create();
            Assert.Throws<InvalidOperationException>(() =>
                missingProvider.Register(new UIDefinition("provider", assetKey: "ui/provider")));
            Assert.That(provider.IsDisposed, Is.False);
        }

        [UnityTest]
        public IEnumerator PrepareAndRuntimeOpenShareOneLoadPerKey()
            => CheckSharedPreparation().ToCoroutine();

        private async UniTask CheckSharedPreparation()
        {
            var gate = LoadGate();
            var provider = new BorrowedProvider((_, __) => gate.Task);
            var context = Create(provider.Load);
            context.Register(new UIDefinition("a", assetKey: "ui/shared"));
            context.Register(new UIDefinition("b", assetKey: "ui/shared"));
            var prepareA = context.PrepareAsync("a").AsTask();
            var prepareB = context.PrepareAsync("b").AsTask();
            Assert.That(provider.CallCount, Is.EqualTo(1));
            Assert.That(provider.LastKey, Is.EqualTo("ui/shared"));
            Assert.That(context.Displays, Is.Empty);
            Assert.That(CountNativeViews(), Is.Zero, "Asset preparation must not warm instances.");
            var first = context.BeginOpen(new UIOpenRequest("a"));
            var second = context.BeginOpen(new UIOpenRequest("b"));
            var firstOpened = first.Opened.AsTask();
            var secondOpened = second.Opened.AsTask();
            Assert.That(first.State, Is.EqualTo(UIState.Opening));
            Assert.That(second.State, Is.EqualTo(UIState.Opening));
            Assert.That(first.ViewObject == null && second.ViewObject == null, Is.True);
            Assert.That(provider.CallCount, Is.EqualTo(1));
            gate.TrySetResult(_source);
            await Wait(Task.WhenAll(prepareA, prepareB, firstOpened, secondOpened));
            await prepareA;
            await prepareB;
            await firstOpened;
            await secondOpened;
            Assert.That(first.State, Is.EqualTo(UIState.Visible));
            Assert.That(second.State, Is.EqualTo(UIState.Visible));
            Assert.That(first.ViewObject, Is.Not.SameAs(second.ViewObject));
            await first.CloseAsync();
            await second.CloseAsync();
            await context.PrepareAsync("a");
            Assert.That(provider.CallCount, Is.EqualTo(1), "Successful key preparation is shared for the owner lifetime.");
            Assert.That(provider.IsDisposed, Is.False);
            Assert.That(_source != null && _root != null, Is.True);
        }

        [UnityTest]
        public IEnumerator PrepareWaitCancellationKeepsSharedLoadAlive()
            => CheckPreparationWaitCancellation().ToCoroutine();

        private async UniTask CheckPreparationWaitCancellation()
        {
            var gate = LoadGate();
            var provider = new BorrowedProvider((_, __) => gate.Task);
            var context = Create(provider.Load);
            context.Register(new UIDefinition("popup", assetKey: "ui/shared"));
            using (var caller = new CancellationTokenSource())
            {
                var cancelledWait = context.PrepareAsync("popup", caller.Token).AsTask();
                var survivingWait = context.PrepareAsync("popup").AsTask();
                var handle = context.BeginOpen(new UIOpenRequest("popup"));
                var opened = handle.Opened.AsTask();
                caller.Cancel();
                await Wait(cancelledWait);
                await AssertCancellation(cancelledWait);
                Assert.That(survivingWait.IsCompleted, Is.False);
                Assert.That(opened.IsCompleted, Is.False);
                Assert.That(provider.CallCount, Is.EqualTo(1));
                Assert.That(provider.ReceivedTokens[0].IsCancellationRequested, Is.False);
                Assert.That(context.IsDisposed, Is.False);
                gate.TrySetResult(_source);
                await Wait(Task.WhenAll(survivingWait, opened));
                await survivingWait;
                await opened;
                Assert.That(handle.State, Is.EqualTo(UIState.Visible));
                await handle.CloseAsync();
            }
            Assert.That(provider.IsDisposed, Is.False);
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator FailedOrInvalidProviderResultRetriesOnlyOnNextRequest()
            => CheckExplicitRetry().ToCoroutine();

        private async UniTask CheckExplicitRetry()
        {
            var destroyed = new GameObject("DestroyedBorrowedAsset");
            UnityEngine.Object.DestroyImmediate(destroyed);
            int attempt = 0;
            var provider = new BorrowedProvider((_, __) =>
            {
                ++attempt;
                if (attempt == 1)
                {
                    return UniTask.FromException<GameObject>(new InvalidOperationException("expected-load-failure"));
                }
                if (attempt == 2)
                {
                    return UniTask.FromResult<GameObject>(null);
                }
                if (attempt == 3)
                {
                    return UniTask.FromResult(destroyed);
                }
                return UniTask.FromResult(_source);
            });
            var context = Create(provider.Load);
            context.Register(new UIDefinition("popup", assetKey: "ui/retry"));
            for (int failure = 1; failure <= 3; ++failure)
            {
                var preparation = context.PrepareAsync("popup").AsTask();
                await Wait(preparation);
                Assert.That(preparation.IsFaulted, Is.True);
                Assert.That(preparation.Exception.Flatten().InnerExceptions[0],
                    Is.InstanceOf<InvalidOperationException>());
                if (failure == 1)
                {
                    Assert.That(preparation.Exception.ToString(), Does.Contain("expected-load-failure"));
                }
                await UniTask.NextFrame();
                Assert.That(provider.CallCount, Is.EqualTo(failure), "Failure must not start an automatic retry.");
                Assert.That(context.Displays, Is.Empty);
                Assert.That(CountNativeViews(), Is.Zero);
            }
            await context.PrepareAsync("popup");
            Assert.That(provider.CallCount, Is.EqualTo(4));
            var handle = await context.OpenAsync(new UIOpenRequest("popup"));
            Assert.That(handle.State, Is.EqualTo(UIState.Visible));
            Assert.That(provider.CallCount, Is.EqualTo(4));
            await handle.CloseAsync();
            Assert.That(provider.IsDisposed, Is.False);
            Assert.That(_source != null, Is.True);
        }

        [UnityTest]
        public IEnumerator OwnerShutdownIgnoresLateProviderResultWithoutReleasingSource()
            => CheckLateProviderResult().ToCoroutine();

        private async UniTask CheckLateProviderResult()
        {
            var gate = LoadGate();
            // The fake native provider deliberately ignores its wait token and completes after UI owner shutdown.
            var provider = new BorrowedProvider((_, __) => gate.Task);
            var context = Create(provider.Load);
            context.Register(new UIDefinition("popup", assetKey: "ui/late"));
            var preparation = context.PrepareAsync("popup").AsTask();
            int projectPreparationCount = 0;
            var hooks = new UIHooks
            {
                PrepareAsync = (_, __) =>
                {
                    ++projectPreparationCount;
                    return UniTask.CompletedTask;
                }
            };
            var handle = context.BeginOpen(new UIOpenRequest("popup", hooks: hooks));
            var opened = handle.Opened.AsTask();
            var closed = handle.Closed.AsTask();
            var shutdown = context.ShutdownAsync().AsTask();
            await Wait(shutdown);
            await shutdown;
            await Wait(preparation);
            await Wait(opened);
            await Wait(closed);
            await AssertCancellation(preparation);
            await AssertCancellation(opened);
            await closed;
            Assert.That(context.IsDisposed, Is.True);
            Assert.That(context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(provider.ReceivedTokens[0].IsCancellationRequested, Is.True);
            Assert.That(projectPreparationCount, Is.Zero);
            Assert.That(context.Displays, Is.Empty);
            Assert.That(CountNativeViews(), Is.Zero);
            gate.TrySetResult(_source);
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(projectPreparationCount, Is.Zero, "Late asset readiness must not republish a display.");
            Assert.That(context.Displays, Is.Empty);
            Assert.That(CountNativeViews(), Is.Zero);
            Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
            Assert.That(provider.IsDisposed, Is.False);
            Assert.That(_source != null && _root != null, Is.True);
            Assert.Throws<ObjectDisposedException>(() => context.BeginOpen(new UIOpenRequest("popup")));
        }

        private sealed class BorrowedProvider : IDisposable
        {
            private readonly Func<string, CancellationToken, UniTask<GameObject>> _load;

            internal BorrowedProvider(Func<string, CancellationToken, UniTask<GameObject>> load)
            {
                _load = load;
            }

            internal int CallCount { get; private set; }
            internal string LastKey { get; private set; }
            internal bool IsDisposed { get; private set; }
            internal readonly List<CancellationToken> ReceivedTokens = new List<CancellationToken>();

            internal UniTask<GameObject> Load(string key, CancellationToken cancellationToken)
            {
                ++CallCount;
                LastKey = key;
                ReceivedTokens.Add(cancellationToken);
                return _load(key, cancellationToken);
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}