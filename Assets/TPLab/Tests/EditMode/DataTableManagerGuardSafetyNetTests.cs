using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using TPLab.Core.DataTables;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public sealed class DataTableManagerGuardSafetyNetTests
    {
        public enum OwnerState
        {
            Open, Disposed
        }
        private static readonly OwnerState[] States = { OwnerState.Open, OwnerState.Disposed };

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadAsync_WorkerThreadPrecedesOwnerState(
            [ValueSource(nameof(States))] OwnerState state)
        {
            var manager = new DataTableManager();
            int validatorCalls = 0;
            try
            {
                if (state == OwnerState.Disposed)
                {
                    manager.Dispose();
                }
                bool disposedBefore = manager.IsDisposed;
                var snapshotBefore = manager.Snapshot;
                bool loadingBefore = manager.IsLoading;
                Exception failure = null;
                var worker = new Thread(() =>
                {
                    try
                    {
                        manager.LoadAsync();
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                }) { IsBackground = true };
                worker.Start();
                Assert.That(worker.Join(3000), Is.True, "Worker did not join within 3000ms.");
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                Assert.That(failure.Message, Is.EqualTo("DataTableManager requires Unity's main thread."));
                Assert.That(manager.IsDisposed, Is.EqualTo(disposedBefore));
                Assert.That(manager.IsLoading, Is.EqualTo(loadingBefore));
                Assert.That(manager.Snapshot, Is.SameAs(snapshotBefore));
                yield return null;
                Assert.That(manager.IsDisposed, Is.EqualTo(disposedBefore));
                Assert.That(manager.IsLoading, Is.EqualTo(loadingBefore));
                Assert.That(manager.Snapshot, Is.SameAs(snapshotBefore));
                if (state == OwnerState.Open)
                {
                    var mainFailure = Assert.Throws<InvalidOperationException>(() => manager.LoadAsync());
                    Assert.That(mainFailure.Message, Is.EqualTo("Register at least one table before loading."));
                    manager.AddValidator(_ => ++validatorCalls);
                    Assert.That(manager.IsDisposed, Is.False);
                }
                else
                {
                    var mainFailure = Assert.Throws<ObjectDisposedException>(() => manager.LoadAsync());
                    Assert.That(mainFailure.ObjectName, Is.EqualTo(nameof(DataTableManager)));
                }
                Assert.That(validatorCalls, Is.Zero);
                manager.Dispose();
                manager.Dispose();
                Assert.That(manager.IsDisposed, Is.True);
                Assert.That(manager.IsLoading, Is.False);
                Assert.That(manager.Snapshot, Is.Null);
            }
            finally
            {
                manager.Dispose();
            }
        }
    }
}
