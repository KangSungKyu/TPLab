using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MyLab.Core.Tests
{
    public sealed class DataTableManagerAsyncTests
    {
        private DataTableManager _tables;

        [SetUp]
        public void SetUp() => _tables = new DataTableManager();

        [TearDown]
        public void TearDown()
        {
            _tables.Dispose();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ConcurrentRequestsPublishOnlyAfterAllSourcesAndValidation() => CheckPublication().ToCoroutine();

        private async UniTask CheckPublication()
        {
            var gate = new UniTaskCompletionSource<string>();
            int calls = 0;
            _tables.Register("first", new[] { "Id" }, token => UniTask.FromResult("Id\n1"), csv => csv.GetField<int>("Id"), id => id);
            _tables.Register("last", new[] { "Id" }, token => { ++calls; return gate.Task; }, csv => csv.GetField<int>("Id"), id => id);
            _tables.AddValidator(candidate =>
            {
                Assert.That(_tables.Snapshot, Is.Null);
                Assert.That(candidate.GetTable<int, int>("first")[1], Is.EqualTo(1));
                Assert.That(candidate.GetTable<int, int>("last")[2], Is.EqualTo(2));
            });
            var first = _tables.LoadAsync().AsTask();
            var second = _tables.LoadAsync().AsTask();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(_tables.Snapshot, Is.Null);
            Assert.That(first.IsCompleted, Is.False);
            Assert.Throws<InvalidOperationException>(() => _tables.AddValidator(snapshot => { }));
            gate.TrySetResult("Id\n2");
            await Settle(first);
            await Settle(second);
            Assert.That(first.Result, Is.SameAs(second.Result).And.SameAs(_tables.Snapshot));
            Assert.That(_tables.IsLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator CallerCancellationLeavesSharedWork() => CheckCancellation(false).ToCoroutine();

        [UnityTest]
        public IEnumerator AllWaitersCancelledStillPublishOwnerSnapshot() => CheckCancellation(true).ToCoroutine();

        private async UniTask CheckCancellation(bool cancelAll)
        {
            var gate = new UniTaskCompletionSource<string>();
            CancellationToken ownerToken = default;
            _tables.Register("rows", new[] { "Id" }, token => { ownerToken = token; return gate.Task; }, csv => csv.GetField<int>("Id"), id => id);
            using (var caller = new CancellationTokenSource())
            {
                var cancelled = _tables.LoadAsync(caller.Token).AsTask();
                var survivor = cancelAll ? null : _tables.LoadAsync().AsTask();
                caller.Cancel();
                await Settle(cancelled);
                Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
                Assert.That(ownerToken.IsCancellationRequested, Is.False);
                Assert.That(_tables.IsLoading, Is.True);
                gate.TrySetResult("Id\n1");
                if (survivor != null) await Settle(survivor);
                for (int frame = 0; frame < 100 && _tables.IsLoading; ++frame) await UniTask.NextFrame();
                Assert.That(_tables.Snapshot.GetTable<int, int>("rows")[1], Is.EqualTo(1));
                if (survivor != null) Assert.That(survivor.Result, Is.SameAs(_tables.Snapshot));
            }
        }

        [UnityTest]
        public IEnumerator PreCancelledRequestDoesNotDispatchOrFreezeRegistration() => CheckPreCancellation().ToCoroutine();

        private async UniTask CheckPreCancellation()
        {
            int calls = 0;
            _tables.Register("first", new[] { "Id" }, token => { ++calls; return UniTask.FromResult("Id\n1"); }, csv => csv.GetField<int>("Id"), id => id);
            using (var caller = new CancellationTokenSource())
            {
                caller.Cancel();
                Assert.Throws<OperationCanceledException>(() => _tables.LoadAsync(caller.Token).GetAwaiter().GetResult());
                Assert.That(calls, Is.Zero);
                Assert.That(_tables.IsLoading, Is.False);
                _tables.Register("second", new[] { "Id" }, token => UniTask.FromResult("Id\n2"), csv => csv.GetField<int>("Id"), id => id);
                Assert.That((await _tables.LoadAsync()).Count, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator SourceFailureAndCancellationPreserveSnapshotAndAllowRetry() => CheckRetry().ToCoroutine();

        private async UniTask CheckRetry()
        {
            var gate = new UniTaskCompletionSource<string>();
            _tables.Register("rows", new[] { "Id" }, token => gate.Task, csv => csv.GetField<int>("Id"), id => id);
            var first = _tables.LoadAsync().AsTask();
            gate.TrySetResult("Id\n1");
            await Settle(first);
            var previous = first.Result;
            gate = new UniTaskCompletionSource<string>();
            var failure = _tables.LoadAsync().AsTask();
            Assert.That(_tables.Snapshot, Is.SameAs(previous));
            gate.TrySetException(new IOException("source unavailable"));
            await Settle(failure);
            Assert.That(Assert.Throws<InvalidDataException>(() => failure.GetAwaiter().GetResult()).InnerException, Is.TypeOf<IOException>());
            Assert.That(_tables.Snapshot, Is.SameAs(previous));
            gate = new UniTaskCompletionSource<string>();
            var cancelled = _tables.LoadAsync().AsTask();
            gate.TrySetCanceled();
            await Settle(cancelled);
            Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
            Assert.That(_tables.Snapshot, Is.SameAs(previous));
            gate = new UniTaskCompletionSource<string>();
            var retry = _tables.LoadAsync().AsTask();
            gate.TrySetResult("Id\n2");
            await Settle(retry);
            Assert.That(retry.Result.GetTable<int, int>("rows")[2], Is.EqualTo(2));
            Assert.That(previous.GetTable<int, int>("rows")[1], Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisposalCancelsWaitersAndRejectsLatePublication() => CheckDisposal().ToCoroutine();

        private async UniTask CheckDisposal()
        {
            var gate = new UniTaskCompletionSource<string>();
            CancellationToken ownerToken = default;
            _tables.Register("rows", new[] { "Id" }, token => { ownerToken = token; return gate.Task; }, csv => csv.GetField<int>("Id"), id => id);
            var first = _tables.LoadAsync().AsTask();
            gate.TrySetResult("Id\n1");
            await Settle(first);
            var previous = first.Result;
            gate = new UniTaskCompletionSource<string>();
            var pending = _tables.LoadAsync().AsTask();
            var shared = _tables.LoadAsync().AsTask();
            _tables.Dispose();
            _tables.Dispose();
            await Settle(pending);
            await Settle(shared);
            Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
            Assert.Throws<OperationCanceledException>(() => shared.GetAwaiter().GetResult());
            Assert.That(ownerToken.IsCancellationRequested, Is.True);
            gate.TrySetResult("Id\n2");
            await UniTask.NextFrame();
            Assert.That(_tables.Snapshot, Is.Null);
            Assert.That(_tables.IsLoading, Is.False);
            Assert.That(previous.GetTable<int, int>("rows")[1], Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => _tables.LoadAsync().GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator WorkerSourceReturnsToMainThreadBeforeProjectCallbacks() => CheckWorkerSource().ToCoroutine();

        private async UniTask CheckWorkerSource()
        {
            var gate = new UniTaskCompletionSource<string>();
            _tables.Register("rows", new[] { "Id" }, token => gate.Task, csv =>
            {
                Assert.That(PlayerLoopHelper.IsMainThread, Is.True);
                return csv.GetField<int>("Id");
            }, id => id);
            _tables.AddValidator(candidate => Assert.That(PlayerLoopHelper.IsMainThread, Is.True));
            var load = _tables.LoadAsync().AsTask();
            var worker = Task.Run(() => gate.TrySetResult("Id\n1"));
            await Settle(worker);
            worker.GetAwaiter().GetResult();
            await Settle(load);
            Assert.That(load.Result, Is.SameAs(_tables.Snapshot));
        }

        [UnityTest]
        public IEnumerator BackgroundApiCallsCannotMutateOwner() => CheckThreadBoundary().ToCoroutine();

        private async UniTask CheckThreadBoundary()
        {
            var register = Task.Run(() => _tables.Register("rows", new[] { "Id" }, token => UniTask.FromResult("Id\n1"), csv => csv.GetField<int>("Id"), id => id));
            await Settle(register);
            Assert.Throws<InvalidOperationException>(() => register.GetAwaiter().GetResult());
            var load = Task.Run(() => _tables.LoadAsync().GetAwaiter().GetResult());
            await Settle(load);
            Assert.Throws<InvalidOperationException>(() => load.GetAwaiter().GetResult());
            var dispose = Task.Run(() => _tables.Dispose());
            await Settle(dispose);
            Assert.Throws<InvalidOperationException>(() => dispose.GetAwaiter().GetResult());
            Assert.That(_tables.IsDisposed, Is.False);
            Assert.That(_tables.Snapshot, Is.Null);
        }

        private static async UniTask Settle(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame) await UniTask.NextFrame();
            Assert.That(task.IsCompleted, Is.True, "Operation did not settle within 100 frames.");
        }
    }
}
