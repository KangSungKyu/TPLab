using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputManagerGuardSafetyNetTests
    {
        [Test, Timeout(30000)]
        public void WorkerLeaseDisposePreservesCreatingThreadMessageAndLease()
        {
            var source = ScriptableObject.CreateInstance<InputActionAsset>();
            InputManager input = null;
            IDisposable lease = null;
            try
            {
                var sourceMap = source.AddActionMap("Game");
                sourceMap.AddAction("Fire", InputActionType.Button);
                input = new InputManager(source);
                var map = input.Actions.FindActionMap(sourceMap.id.ToString(), true);
                input.Layers.RegisterLayer("game", new[] { map.id }, 0, InputLayerMode.Overlay);
                int changedCount = 0;
                input.Layers.Changed += _ => ++changedCount;
                lease = input.Layers.AcquireLayer("game");
                InputLayerSnapshot snapshot = input.Layers.Snapshot;
                int changesBefore = changedCount;
                InputActionAsset clone = input.Actions;
                Assert.That(map.enabled, Is.True);
                Assert.That(snapshot.ActiveLayerIds, Is.EqualTo(new[] { "game" }));
                Assert.That(snapshot.ActiveMapIds, Is.EqualTo(new[] { map.id }));

                Exception workerError = null;
                var worker = new Thread(() =>
                {
                    try
                    {
                        lease.Dispose();
                    }
                    catch (Exception error)
                    {
                        workerError = error;
                    }
                }) { IsBackground = true };
                worker.Start();
                Assert.That(worker.Join(3000), Is.True, "Worker did not join within 3000ms.");
                Assert.That(workerError, Is.TypeOf<InvalidOperationException>());
                Assert.That(workerError.Message,
                    Is.EqualTo("Input scope operations require the creating Unity thread."));
                Assert.That(input.IsDisposed, Is.False);
                Assert.That(input.Actions, Is.SameAs(clone));
                Assert.That(input.Layers.Snapshot, Is.SameAs(snapshot));
                Assert.That(changedCount, Is.EqualTo(changesBefore));
                Assert.That(map.enabled, Is.True);
                Assert.That(sourceMap.enabled, Is.False);

                // The rejected worker call must leave this exact lease usable by its creating thread.
                lease.Dispose();
                Assert.That(map.enabled, Is.False);
                Assert.That(input.Layers.Snapshot.ActiveLayerIds, Is.Empty);
                Assert.That(input.Layers.Snapshot.ActiveMapIds, Is.Empty);
                Assert.That(changedCount, Is.EqualTo(changesBefore + 1));
                lease.Dispose();
                Assert.That(changedCount, Is.EqualTo(changesBefore + 1));
                Assert.That(input.IsDisposed, Is.False);
                Assert.That(sourceMap.enabled, Is.False);
            }
            finally
            {
                lease?.Dispose();
                input?.Dispose();
                Object.DestroyImmediate(source);
            }
        }
    }
}
