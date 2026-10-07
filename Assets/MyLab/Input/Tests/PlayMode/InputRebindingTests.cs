using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MyLab.Core.Input.Tests
{
    public sealed class InputRebindingTests
    {
        private InputActionAsset _source;
        private InputManager _input;
        private InputAction _fire;
        private Keyboard _keyboard;

        [SetUp]
        public void SetUp()
        {
            _source = ScriptableObject.CreateInstance<InputActionAsset>();
            var game = _source.AddActionMap("Game");
            _fire = game.AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
            game.AddAction("Jump", InputActionType.Button, "<Keyboard>/j");
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _input = new InputManager(_source);
            _input.Actions.devices = new InputDevice[] { _keyboard };
            _input.Layers.RegisterLayer("game", new[] { game.id }, 0, InputLayerMode.Overlay);
            _input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
        }

        [TearDown]
        public void TearDown()
        {
            _input.Dispose();
            if (_keyboard.added)
            {
                InputSystem.RemoveDevice(_keyboard);
            }
            Object.Destroy(_source);
        }

        private RebindRequest Request(float timeout = 2) => new RebindRequest(_fire.id, _fire.bindings[0].id)
        {
            ControlPath = "<Keyboard>", TimeoutSeconds = timeout
        };

        private async UniTask KeyAsync(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            await UniTask.NextFrame();
            await UniTask.NextFrame();
        }

        [UnityTest]
        public IEnumerator SelectedButtonWaitsForReleaseAndRestoresCurrentModal() => UniTask.ToCoroutine(async () =>
        {
            using var game = _input.Layers.AcquireLayer("game");
            bool selected = false;
            var request = Request();
            request.Validator = _ => { selected = true; return true; };
            var pending = _input.Rebinding.RebindAsync(request).Preserve();
            Assert.That(_input.Actions.enabled, Is.False);
            await KeyAsync(Key.K);
            await UniTask.WaitUntil(() => selected).Timeout(TimeSpan.FromSeconds(1));
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_input.GetAction(_fire.id).bindings[0].overridePath, Is.Null);
            using var modal = _input.Layers.AcquireLayer("modal");
            await KeyAsync();
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Applied));
            Assert.That(_input.GetAction(_fire.id).bindings[0].effectivePath, Is.EqualTo("<Keyboard>/k"));
            Assert.That(_input.Actions.enabled, Is.False);
            Assert.That(_fire.bindings[0].overridePath, Is.Null);
            modal.Dispose();
            int performed = 0;
            _input.GetAction(_fire.id).performed += _ => performed++;
            await KeyAsync(Key.Space);
            await KeyAsync();
            Assert.That(performed, Is.Zero);
            await KeyAsync(Key.K);
            Assert.That(performed, Is.EqualTo(1));
            await KeyAsync();
        });

        [UnityTest]
        public IEnumerator SameMapConflictIsRejectedWithoutMutation() => UniTask.ToCoroutine(async () =>
        {
            var pending = _input.Rebinding.RebindAsync(Request());
            await KeyAsync(Key.J);
            await KeyAsync();
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Rejected));
            Assert.That(_input.GetAction(_fire.id).bindings[0].overridePath, Is.Null);
        });

        [UnityTest]
        public IEnumerator TimeoutIncludesReleaseWait() => UniTask.ToCoroutine(async () =>
        {
            bool selected = false;
            var request = Request(0.5f);
            request.Validator = _ => { selected = true; return true; };
            var pending = _input.Rebinding.RebindAsync(request);
            await KeyAsync(Key.K);
            await UniTask.WaitUntil(() => selected).Timeout(TimeSpan.FromSeconds(1));
            Assert.That(_keyboard.kKey.isPressed, Is.True);
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.TimedOut));
            Assert.That(_input.GetAction(_fire.id).bindings[0].overridePath, Is.Null);
        });

        [UnityTest]
        public IEnumerator CancelKeyAndOwnerCancellationAreDistinct() => UniTask.ToCoroutine(async () =>
        {
            var pending = _input.Rebinding.RebindAsync(Request());
            await KeyAsync(Key.Escape);
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Cancelled));
            await KeyAsync();
            pending = _input.Rebinding.RebindAsync(Request());
            _input.Dispose();
            bool cancelled = false;
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
        });

        [UnityTest]
        public IEnumerator OwnerDisposalWhileAcquiringBlockIsCancellation() => UniTask.ToCoroutine(async () =>
        {
            using var game = _input.Layers.AcquireLayer("game");
            var action = _input.GetAction(_fire.id);
            await KeyAsync(Key.Space);
            Assert.That(action.IsPressed(), Is.True);
            action.canceled += _ => _input.Dispose();
            bool cancelled = false;
            try
            {
                await _input.Rebinding.RebindAsync(Request());
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            Assert.That(_input.IsDisposed, Is.True);
            Assert.That(_input.Rebinding.IsRebinding, Is.False);
            Assert.That(_fire.bindings[0].overridePath, Is.Null);
            await UniTask.NextFrame();
        });

        [UnityTest]
        public IEnumerator OwnerShutdownWhileAcquiringBlockIsCancellation() => UniTask.ToCoroutine(async () =>
        {
            using var game = _input.Layers.AcquireLayer("game");
            var action = _input.GetAction(_fire.id);
            await KeyAsync(Key.Space);
            Assert.That(action.IsPressed(), Is.True);
            UniTask shutdown = default;
            action.canceled += _ => shutdown = _input.ShutdownAsync().Preserve();
            bool cancelled = false;
            try
            {
                await _input.Rebinding.RebindAsync(Request());
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            await shutdown;
            Assert.That(cancelled, Is.True);
            Assert.That(_input.Rebinding.IsRebinding, Is.False);
            Assert.That(_fire.bindings[0].overridePath, Is.Null);
            await UniTask.NextFrame();
        });

        [UnityTest]
        public IEnumerator ConcurrentMutationAndInvalidRequestsHaveNoSideEffects() => UniTask.ToCoroutine(async () =>
        {
            using var cancellation = new CancellationTokenSource();
            var pending = _input.Rebinding.RebindAsync(Request(), cancellation.Token);
            Assert.Throws<InvalidOperationException>(() => _input.Rebinding.ResetAll());
            bool duplicate = false;
            try
            {
                await _input.Rebinding.RebindAsync(Request());
            }
            catch (InvalidOperationException)
            {
                duplicate = true;
            }
            Assert.That(duplicate, Is.True);
            cancellation.Cancel();
            bool cancelled = false;
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            Assert.That(_input.Rebinding.IsRebinding, Is.False);
        });

        [UnityTest]
        public IEnumerator RemovedCandidateDeviceCancelsWithoutApplying() => UniTask.ToCoroutine(async () =>
        {
            var pending = _input.Rebinding.RebindAsync(Request());
            await KeyAsync(Key.K);
            InputSystem.RemoveDevice(_keyboard);
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Cancelled));
            Assert.That(_input.GetAction(_fire.id).bindings[0].overridePath, Is.Null);
        });

        [UnityTest]
        public IEnumerator ValidatorCanReplaceConflictPolicyAndExceptionsLeaveNoOverride() => UniTask.ToCoroutine(async () =>
        {
            var request = Request();
            request.Validator = _ => true;
            var pending = _input.Rebinding.RebindAsync(request);
            await KeyAsync(Key.J);
            await KeyAsync();
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Applied));
            _input.Rebinding.ResetAll();
            request.Validator = _ => throw new InvalidOperationException("candidate rejected with an error");
            pending = _input.Rebinding.RebindAsync(request);
            await KeyAsync(Key.K);
            await KeyAsync();
            bool failed = false;
            try
            {
                await pending;
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }
            Assert.That(failed, Is.True);
            Assert.That(_input.GetAction(_fire.id).bindings[0].overridePath, Is.Null);
            Assert.That(_input.Rebinding.IsRebinding, Is.False);
        });

        [UnityTest]
        public IEnumerator ShutdownSharesDrainAndRejectsLateCommit() => UniTask.ToCoroutine(async () =>
        {
            var pending = _input.Rebinding.RebindAsync(Request());
            await KeyAsync(Key.K);
            var first = _input.ShutdownAsync();
            var second = _input.ShutdownAsync();
            await first;
            await second;
            Assert.That(_input.Rebinding.IsRebinding, Is.False);
            bool cancelled = false;
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            Assert.That(_fire.bindings[0].overridePath, Is.Null);
        });

        [UnityTest]
        public IEnumerator CompositeRootRejectsWhilePartKeepsStableId() => UniTask.ToCoroutine(async () =>
        {
            _input.Dispose();
            var move = _fire.actionMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Axis");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            _input = new InputManager(_source);
            _input.Actions.devices = new InputDevice[] { _keyboard };
            _input.Layers.RegisterLayer("game", new[] { move.actionMap.id }, 0, InputLayerMode.Overlay);
            using var game = _input.Layers.AcquireLayer("game");
            bool rejected = false;
            try
            {
                await _input.Rebinding.RebindAsync(new RebindRequest(move.id, move.bindings[0].id));
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Assert.That(rejected && _input.Actions.enabled, Is.True);
            Guid partId = move.bindings[1].id;
            var pending = _input.Rebinding.RebindAsync(new RebindRequest(move.id, partId)
            {
                ControlPath = "<Keyboard>", TimeoutSeconds = 2
            });
            await KeyAsync(Key.K);
            await KeyAsync();
            Assert.That((await pending).Status, Is.EqualTo(RebindStatus.Applied));
            var runtime = _input.GetAction(move.id);
            Assert.That(runtime.bindings[1].id, Is.EqualTo(partId));
            Assert.That(runtime.bindings[1].effectivePath, Is.EqualTo("<Keyboard>/k"));
            Assert.That(runtime.bindings[0].overridePath, Is.Null);
            Assert.That(runtime.bindings[2].effectivePath, Is.EqualTo("<Keyboard>/d"));
        });
    }
}
