using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Cysharp.Threading.Tasks;
using TPLab.Core.DataTables;
using TPLab.Examples.DataTables;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public sealed class StandardDataTableAsyncTests
    {
        [UnityTest]
        public IEnumerator ReloadQueriesOldGenerationAndCallerCancellationLeavesSharedWork() => CheckReload().ToCoroutine();

        private static async UniTask CheckReload()
        {
            using (var manager = new DataTableManager())
            using (var caller = new CancellationTokenSource())
            {
                var gate = new UniTaskCompletionSource<string>();
                int calls = 0;
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                manager.RegisterTable<TextRow, TextDataTable>(1, "text", _ => { ++calls; return gate.Task; }, () => new TextDataTable());
                var first = manager.LoadAsync().AsTask();
                gate.TrySetResult("idx,text\n1001,old");
                await Settle(first);
                var previous = first.Result;
                gate = new UniTaskCompletionSource<string>();
                var canceled = manager.LoadAsync(caller.Token).AsTask();
                var survivor = manager.LoadAsync().AsTask();
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(manager.Get<TextRow>(1001).Text, Is.EqualTo("old"));
                Assert.Throws<InvalidOperationException>(() => manager.BindTable<IDisposable>("text"));
                caller.Cancel();
                await Settle(canceled);
                Assert.Throws<OperationCanceledException>(() => canceled.GetAwaiter().GetResult());
                Assert.That(survivor.IsCompleted, Is.False);
                gate.TrySetResult("idx,text\n1001,new");
                await Settle(survivor);
                Assert.That(manager.Get<TextRow>(1001).Text, Is.EqualTo("new"));
                Assert.That(previous.Get<TextRow>(1001).Text, Is.EqualTo("old"));
            }
        }

        public sealed class MainThreadTable : CsvDataTable<TextRow>
        {
            protected override void ConfigureMapping(CsvContext context) => Assert.That(PlayerLoopHelper.IsMainThread, Is.True);
            protected override void ValidateRow(TextRow row) => Assert.That(PlayerLoopHelper.IsMainThread, Is.True);
        }

        [UnityTest]
        public IEnumerator WorkerSourceParsesOnMainAndWorkerQueriesAreRejected() => CheckWorker().ToCoroutine();

        private static async UniTask CheckWorker()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                var gate = new UniTaskCompletionSource<string>();
                manager.RegisterTable<TextRow, MainThreadTable>(1, "text", _ => gate.Task, () =>
                {
                    Assert.That(PlayerLoopHelper.IsMainThread, Is.True);
                    return new MainThreadTable();
                });
                manager.AddValidator(snapshot => Assert.That(PlayerLoopHelper.IsMainThread, Is.True));
                var load = manager.LoadAsync().AsTask();
                var source = Task.Run(() => gate.TrySetResult("idx,text\n1001,ready"));
                await Settle(source);
                source.GetAwaiter().GetResult();
                await Settle(load);
                Assert.That(load.Result.Get<TextRow>(1001).Text, Is.EqualTo("ready"));
                var query = Task.Run(() => manager.Get<TextRow>(1001));
                await Settle(query);
                Assert.Throws<InvalidOperationException>(() => query.GetAwaiter().GetResult());
                var tryQuery = Task.Run(() => manager.TryGet(1001, out TextRow _));
                await Settle(tryQuery);
                Assert.Throws<InvalidOperationException>(() => tryQuery.GetAwaiter().GetResult());
            }
        }

        [UnityTest]
        public IEnumerator DisposalRejectsLateStandardPublicationAndRetainedSnapshotStaysReadable() => CheckDisposal().ToCoroutine();

        private static async UniTask CheckDisposal()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                var gate = new UniTaskCompletionSource<string>();
                manager.RegisterTable<TextRow, TextDataTable>(1, "text", _ => gate.Task, () => new TextDataTable());
                var first = manager.LoadAsync().AsTask();
                gate.TrySetResult("idx,text\n1001,old");
                await Settle(first);
                var previous = first.Result;
                gate = new UniTaskCompletionSource<string>();
                var pending = manager.LoadAsync().AsTask();
                manager.Dispose();
                await Settle(pending);
                Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
                gate.TrySetResult("idx,text\n1001,late");
                await UniTask.NextFrame();
                Assert.That(manager.Snapshot, Is.Null);
                Assert.That(previous.Get<TextRow>(1001).Text, Is.EqualTo("old"));
                Assert.Throws<ObjectDisposedException>(() => manager.TryGet(1001, out TextRow _));
            }
        }

        private static async UniTask Settle(Task task)
        {
            for (int frame = 0; frame < 100 && !task.IsCompleted; ++frame) await UniTask.NextFrame();
            Assert.That(task.IsCompleted, Is.True, "Operation did not settle within 100 frames.");
        }
    }
}
