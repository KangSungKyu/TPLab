using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputLayerTests
    {
        private InputActionAsset _source;
        private InputManager _input;
        private InputAction _fire;
        private InputActionMap _game;
        private InputActionMap _menu;
        private InputActionMap _popup;
        private Keyboard _keyboard;

        [SetUp]
        public void SetUp()
        {
            _source = ScriptableObject.CreateInstance<InputActionAsset>();
            _game = _source.AddActionMap("Game");
            _fire = _game.AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
            _menu = _source.AddActionMap("Menu");
            _menu.AddAction("Select", InputActionType.Button, "<Keyboard>/enter");
            _popup = _source.AddActionMap("Popup");
            _popup.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            _input = new InputManager(_source);
        }

        [TearDown]
        public void TearDown()
        {
            _input?.Dispose();
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_source != null) Object.DestroyImmediate(_source);
        }

        private void Register(int menuPriority = 100, int popupPriority = 200)
        {
            _input.Layers.RegisterLayer("game", new[] { _game.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("menu", new[] { _menu.id }, menuPriority, InputLayerMode.BlockLower);
            _input.Layers.RegisterLayer("popup", new[] { _popup.id }, popupPriority, InputLayerMode.BlockLower);
        }

        private bool Enabled(Guid id) => _input.Actions.FindActionMap(id.ToString(), true).enabled;

        [Test]
        public void CloneKeepsSourceDisabledAndResolvesReferenceById()
        {
            var reference = InputActionReference.Create(_fire);
            try
            {
                Assert.That(_input.Actions, Is.Not.Null.And.Not.SameAs(_source));
                Assert.That(_input.GetAction(reference).id, Is.EqualTo(_fire.id));
                Assert.That(_input.GetAction(_fire.id), Is.Not.SameAs(_fire));
                Register();
                using (_input.Layers.AcquireLayer("game"))
                {
                    Assert.That(Enabled(_game.id), Is.True);
                    Assert.That(_fire.enabled, Is.False);
                }
                Assert.That(Enabled(_game.id), Is.False);
            }
            finally { Object.DestroyImmediate(reference); }
        }

        [Test]
        public void NestedLayersAndOutOfOrderDisposalRecalculateRemainingOwnership()
        {
            Register();
            using var game = _input.Layers.AcquireLayer("game");
            var menu = _input.Layers.AcquireLayer("menu");
            var popup = _input.Layers.AcquireLayer("popup");
            Assert.That(Enabled(_popup.id), Is.True);
            Assert.That(Enabled(_menu.id) || Enabled(_game.id), Is.False);
            menu.Dispose();
            Assert.That(Enabled(_popup.id), Is.True);
            popup.Dispose();
            Assert.That(Enabled(_game.id), Is.True);
        }

        [Test]
        public void SamePriorityUsesLatestRemainingLeaseAndDuplicateDisposalIsSafe()
        {
            Register(100, 100);
            using var game = _input.Layers.AcquireLayer("game");
            var menu = _input.Layers.AcquireLayer("menu");
            var popup = _input.Layers.AcquireLayer("popup");
            var laterMenu = _input.Layers.AcquireLayer("menu");
            Assert.That(Enabled(_menu.id), Is.True);
            laterMenu.Dispose();
            laterMenu.Dispose();
            Assert.That(Enabled(_popup.id), Is.True);
            popup.Dispose();
            Assert.That(Enabled(_menu.id), Is.True);
            menu.Dispose();
        }

        [Test]
        public void OverlaysShareInputAndWholeScopeBlocksRemainIndependent()
        {
            _input.Layers.RegisterLayer("game", new[] { _game.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("menu", new[] { _menu.id }, 100, InputLayerMode.Overlay);
            using var game = _input.Layers.AcquireLayer("game");
            using var menu = _input.Layers.AcquireLayer("menu");
            Assert.That(Enabled(_game.id) && Enabled(_menu.id), Is.True);
            var first = _input.Layers.BlockAll();
            var second = _input.Layers.BlockAll();
            first.Dispose();
            Assert.That(Enabled(_game.id) || Enabled(_menu.id), Is.False);
            second.Dispose();
            Assert.That(Enabled(_game.id) && Enabled(_menu.id), Is.True);
        }

        [Test]
        public void EmptyBlockingLayerSuppressesLowerMap()
        {
            _input.Layers.RegisterLayer("game", new[] { _game.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("transition", Array.Empty<Guid>(), 1000, InputLayerMode.BlockLower);
            using var game = _input.Layers.AcquireLayer("game");
            var transition = _input.Layers.AcquireLayer("transition");
            Assert.That(Enabled(_game.id), Is.False);
            transition.Dispose();
            Assert.That(Enabled(_game.id), Is.True);
        }

        [Test]
        public void InvalidAndOverlappingRegistrationHasNoActivationSideEffects()
        {
            Assert.Throws<ArgumentException>(() => _input.Layers.RegisterLayer("bad", new[] { Guid.NewGuid() }, 0, InputLayerMode.Overlay));
            _input.Layers.RegisterLayer("game", new[] { _game.id }, 0, InputLayerMode.Overlay);
            Assert.Throws<ArgumentException>(() => _input.Layers.RegisterLayer("other", new[] { _game.id }, 1, InputLayerMode.Overlay));
            Assert.Throws<ArgumentException>(() => _input.Layers.RegisterLayer("game", Array.Empty<Guid>(), 0, InputLayerMode.Overlay));
            Assert.That(Enabled(_game.id), Is.False);
            using var game = _input.Layers.AcquireLayer("game");
            Assert.Throws<InvalidOperationException>(() => _input.Layers.RegisterLayer("late", new[] { _menu.id }, 1, InputLayerMode.Overlay));
        }

        [Test]
        public void ShutdownBlocksNewWorkAndLateLeasesAreHarmless()
        {
            Register();
            var lease = _input.Layers.AcquireLayer("game");
            _input.Dispose();
            _input.Dispose();
            Assert.DoesNotThrow(lease.Dispose);
            Assert.Throws<ObjectDisposedException>(() => _input.Layers.AcquireLayer("game"));
            Assert.Throws<ObjectDisposedException>(() => _input.GetAction(_fire.id));
            Assert.That(_source, Is.Not.Null);
        }

        [Test]
        public void ForeignAssetReferenceAndMissingActionAreRejected()
        {
            var foreign = ScriptableObject.CreateInstance<InputActionAsset>();
            var reference = InputActionReference.Create(foreign.AddActionMap("Other").AddAction("Action"));
            try
            {
                Assert.Throws<ArgumentException>(() => _input.GetAction(reference));
                Assert.Throws<ArgumentException>(() => _input.GetAction(Guid.NewGuid()));
            }
            finally
            {
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(foreign);
            }
        }

        [Test]
        public void PreparationBlockDoesNotFreezeRegistrationAndSnapshotsAreDetached()
        {
            var block = _input.Layers.BlockAll();
            Register();
            using var game = _input.Layers.AcquireLayer("game");
            var blocked = _input.Layers.Snapshot;
            Assert.That(blocked.AllInputBlocked, Is.True);
            block.Dispose();
            Assert.That(_input.Layers.Snapshot.ActiveMapIds, Is.EqualTo(new[] { _game.id }));
            Assert.That(blocked.ActiveMapIds, Is.Empty);
        }

        [Test]
        public void ObserverRefreshConvergesAndNativeChangesAreReconciled()
        {
            Register();
            int notifications = 0;
            _input.Layers.Changed += _ =>
            {
                notifications++;
                _input.Layers.Refresh();
            };
            using var game = _input.Layers.AcquireLayer("game");
            _input.Actions.FindActionMap(_menu.id.ToString(), true).Enable();
            _input.Layers.Refresh();
            Assert.That(Enabled(_menu.id), Is.False);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void ObserverFailureStopsAllMapsAndInvalidatesExistingLeases()
        {
            Register();
            var game = _input.Layers.AcquireLayer("game");
            var error = new InvalidOperationException("observer failed");
            _input.Layers.Changed += _ => throw error;
            Assert.That(Assert.Throws<InvalidOperationException>(() => _input.Layers.AcquireLayer("menu")), Is.SameAs(error));
            Assert.That(_input.Layers.IsFaulted, Is.True);
            Assert.That(Enabled(_game.id) || Enabled(_menu.id), Is.False);
            Assert.DoesNotThrow(game.Dispose);
            Assert.Throws<InvalidOperationException>(() => _input.GetAction(_fire.id));
        }

        [Test]
        public void WorkerThreadCannotMutateScopeOrConsumeALease()
        {
            Register();
            var game = _input.Layers.AcquireLayer("game");
            Exception error = null;
            var worker = new System.Threading.Thread(() =>
            {
                try
                {
                    game.Dispose();
                }
                catch (Exception failure)
                {
                    error = failure;
                }
            });
            worker.Start();
            Assert.That(worker.Join(3000), Is.True);
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(Enabled(_game.id), Is.True);
            game.Dispose();
            Assert.That(Enabled(_game.id), Is.False);
        }
    }
}
