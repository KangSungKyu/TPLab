using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests
{
    public sealed class UIContextSafetyNetBEditModeTests
    {
        private GameObject _root;
        private GameObject _source;
        private UIContext _context;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UISafetyNetBEditOwner");
            _source = new GameObject("UISafetyNetBEditSource");
            _source.SetActive(false);
            _context = new UIContext(_root);
            _context.Register(new UIDefinition("popup", _source));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return CleanupAsync().ToCoroutine();
            LogAssert.NoUnexpectedReceived();
        }

        private async UniTask CleanupAsync()
        {
            try
            {
                if (_context != null)
                {
                    var shutdown = _context.ShutdownAsync().AsTask();
                    await Wait(shutdown);
                    await shutdown;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(_root);
                UnityEngine.Object.DestroyImmediate(_source);
                _root = null;
                _source = null;
                _context = null;
            }
        }

        private static async UniTask Wait(Task task)
        {
            var timer = Stopwatch.StartNew();
            for (int frame = 0; frame < 100 && !task.IsCompleted && timer.ElapsedMilliseconds < 10000; ++frame)
            {
                await UniTask.Yield();
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
        public IEnumerator CleanupFailureSharesOriginalAggregateAndLeavesOwnerUsable()
        {
            return CheckCleanupFailure().ToCoroutine();
        }

        private async UniTask CheckCleanupFailure()
        {
            var closingError = new ApplicationException("closing-identity");
            var cleanupError = new InvalidOperationException("cleanup-identity");
            var observerError = new NotSupportedException("closed-identity");
            int cleanupCount = 0;
            var handle = await _context.OpenAsync(new UIOpenRequest("popup", hooks: new UIHooks
            {
                CloseAsync = (_, __) => throw closingError,
                Closed = _ => throw observerError
            }));
            var view = handle.ViewObject;
            handle.RegisterCleanup(() => { ++cleanupCount; throw cleanupError; });
            var observedClosed = handle.Closed.AsTask();
            var firstClose = handle.CloseAsync().AsTask();
            var repeatedClose = handle.CloseAsync().AsTask();
            await Wait(firstClose);
            await Wait(repeatedClose);
            await Wait(observedClosed);
            Assert.That(Error(firstClose), Is.TypeOf<AggregateException>());
            var failure = (AggregateException)Error(firstClose);
            Assert.That(failure.InnerExceptions.Count, Is.EqualTo(3));
            Assert.That(failure.InnerExceptions[0], Is.SameAs(closingError));
            Assert.That(failure.InnerExceptions[1], Is.SameAs(cleanupError));
            Assert.That(failure.InnerExceptions[2], Is.SameAs(observerError));
            Assert.That(failure.InnerException, Is.SameAs(closingError));
            Assert.That(Error(repeatedClose), Is.SameAs(failure));
            Assert.That(Error(observedClosed), Is.SameAs(failure));
            Assert.That(Error(handle.CloseAsync().AsTask()), Is.SameAs(failure));
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(handle.State, Is.EqualTo(UIState.Closed));
            Assert.That(ReferenceEquals(handle.ViewObject, null), Is.True);
            Assert.That(view == null, Is.True);
            Assert.That(handle.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(_context.Displays, Is.Empty);
            Assert.That(_context.Fault, Is.Null, "Ordinary callback errors do not become a native context fault.");
            Assert.That(_context.IsDisposed || _context.LifetimeToken.IsCancellationRequested, Is.False);
            var next = await _context.OpenAsync(new UIOpenRequest("popup"));
            Assert.That(next.State, Is.EqualTo(UIState.Visible));
            var nextClose = next.CloseAsync().AsTask();
            await Wait(nextClose);
            await nextClose;
            var shutdown = _context.ShutdownAsync().AsTask();
            await Wait(shutdown);
            Assert.That(Error(shutdown), Is.Null, "A previously retired failure is not carried into owner shutdown.");
            Assert.That(_context.IsDisposed && _context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(_context.Fault, Is.Null);
            Assert.That(_root != null && _source != null, Is.True);
        }
    }
}
