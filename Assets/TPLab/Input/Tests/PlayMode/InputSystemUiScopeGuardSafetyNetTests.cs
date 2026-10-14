using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Samples.SceneTransitions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputSystemUiScopeGuardSafetyNetTests
    {
        private readonly InputTestFixture _inputFixture = new InputTestFixture();
        private GameObject _host;
        private InputActionAsset _source;
        private InputManager _input;
        private InputActionMap _uiMap;
        private InputSystemUIInputModule _module;
        private InputSystemUiScope _adapter;
        private IDisposable _lease;
        private int _changedCount;
        private readonly List<InputActionReference> _references = new List<InputActionReference>();

        [SetUp]
        public void SetUp()
        {
            _inputFixture.Setup();
            _changedCount = 0;
            _source = ScriptableObject.CreateInstance<InputActionAsset>();
            var sourceMap = _source.AddActionMap("UI");
            sourceMap.AddAction("Point", InputActionType.Value, expectedControlLayout: "Vector2");
            sourceMap.AddAction("Click", InputActionType.Button);
            sourceMap.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            sourceMap.AddAction("Submit", InputActionType.Button);
            sourceMap.AddAction("Cancel", InputActionType.Button);
            _input = new InputManager(_source);
            _uiMap = _input.Actions.FindActionMap(sourceMap.id.ToString(), true);
            _input.Layers.RegisterLayer("ui", new[] { _uiMap.id }, 100, InputLayerMode.Overlay);
            _input.Layers.Changed += _ => ++_changedCount;

            // Keep the whole host inactive: native OnEnable/LateUpdate cannot alter the snapshots.
            _host = new GameObject("InputUiGuardOwner");
            _host.SetActive(false);
            _host.AddComponent<EventSystem>();
            _module = _host.AddComponent<InputSystemUIInputModule>();
            _module.enabled = false;
            _module.actionsAsset = _input.Actions;
            _module.point = Reference("Point");
            _module.leftClick = Reference("Click");
            _module.move = Reference("Navigate");
            _module.submit = Reference("Submit");
            _module.cancel = Reference("Cancel");
            _module.rightClick = null;
            _module.middleClick = null;
            _module.scrollWheel = null;
            _module.trackedDevicePosition = null;
            _module.trackedDeviceOrientation = null;
            _adapter = _host.AddComponent<InputSystemUiScope>();
        }

        private InputActionReference Reference(string name)
        {
            var reference = InputActionReference.Create(_uiMap.FindAction(name, true));
            _references.Add(reference);
            return reference;
        }

        [UnityTearDown, Timeout(30000)]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                if (_adapter != null) _adapter.Unbind();
                _lease?.Dispose();
                _lease = null;
                if (_module != null)
                {
                    _module.enabled = false;
                    _module.actionsAsset = null;
                }
                _input?.Dispose();
                foreach (InputActionReference reference in _references) Object.Destroy(reference);
                _references.Clear();
                if (_host != null) Object.Destroy(_host);
                if (_source != null) Object.Destroy(_source);
                _input = null;
                _adapter = null;
                _module = null;
                _host = null;
                _source = null;
                await UniTask.Yield();
            }
            finally
            {
                _inputFixture.TearDown();
            }
            LogAssert.NoUnexpectedReceived();
        });

        private static Exception[] OnWorker(Action first, Action second)
        {
            var errors = new Exception[2];
            var worker = new Thread(() =>
            {
                try { first(); } catch (Exception error) { errors[0] = error; }
                try { second(); } catch (Exception error) { errors[1] = error; }
            }) { IsBackground = true };
            worker.Start();
            Assert.That(worker.Join(3000), Is.True, "Worker did not join within 3000ms.");
            return errors;
        }

        private void AssertBorrowedObjectsUnchanged(InputActionAsset clone, InputActionReference point,
            InputLayerSnapshot snapshot, int changes, bool mapEnabled)
        {
            Assert.That(_input.IsDisposed, Is.False);
            Assert.That(_input.Actions, Is.SameAs(clone));
            Assert.That(_module.actionsAsset, Is.SameAs(clone));
            Assert.That(_module.point, Is.SameAs(point));
            Assert.That(point.action.actionMap, Is.SameAs(_uiMap));
            Assert.That(_module.enabled, Is.False);
            Assert.That(_host.activeSelf, Is.False);
            Assert.That(_uiMap.enabled, Is.EqualTo(mapEnabled));
            Assert.That(_input.Layers.Snapshot, Is.SameAs(snapshot));
            Assert.That(_changedCount, Is.EqualTo(changes));
            Assert.That(_source.enabled, Is.False);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator BoundWorkerCallsPreserveBindingThreadMessageAndLease() => UniTask.ToCoroutine(async () =>
        {
            _adapter.Bind(_input, _module, _uiMap.id);
            _lease = _input.Layers.AcquireLayer("ui");
            var clone = _input.Actions;
            var point = _module.point;
            InputLayerSnapshot snapshot = _input.Layers.Snapshot;
            int changes = _changedCount;
            Assert.That(_uiMap.enabled, Is.True);
            Assert.That(snapshot.ActiveLayerIds, Is.EqualTo(new[] { "ui" }));
            Assert.That(snapshot.ActiveMapIds, Is.EqualTo(new[] { _uiMap.id }));

            Exception[] errors = OnWorker(_adapter.Refresh, _adapter.Unbind);
            foreach (Exception error in errors)
            {
                Assert.That(error, Is.TypeOf<InvalidOperationException>());
                Assert.That(error.Message,
                    Is.EqualTo("UI scope operations require the binding Unity thread."));
            }
            AssertBorrowedObjectsUnchanged(clone, point, snapshot, changes, true);
            await UniTask.Yield();
            AssertBorrowedObjectsUnchanged(clone, point, snapshot, changes, true);

            // Repeated Bind must still reject: the worker Unbind did not consume this binding.
            var alreadyBound = Assert.Throws<InvalidOperationException>(
                () => _adapter.Bind(_input, _module, _uiMap.id));
            Assert.That(alreadyBound.Message, Is.EqualTo("The UI module is already bound."));
            _adapter.Refresh();
            _adapter.Unbind();
            _adapter.Bind(_input, _module, _uiMap.id);
            AssertBorrowedObjectsUnchanged(clone, point, snapshot, changes, true);
            _lease.Dispose();
            Assert.That(_uiMap.enabled, Is.False);
            Assert.That(_input.Layers.Snapshot.ActiveLayerIds, Is.Empty);
            Assert.That(_input.Layers.Snapshot.ActiveMapIds, Is.Empty);
            Assert.That(_changedCount, Is.EqualTo(changes + 1));
        });

        [UnityTest, Timeout(30000)]
        public IEnumerator UnboundWorkerCallsReturnBeforeBindingThreadValidation() => UniTask.ToCoroutine(async () =>
        {
            var clone = _input.Actions;
            var point = _module.point;
            InputLayerSnapshot snapshot = _input.Layers.Snapshot;
            int changes = _changedCount;
            Exception[] errors = OnWorker(_adapter.Refresh, _adapter.Unbind);
            Assert.That(errors, Is.EqualTo(new Exception[] { null, null }));
            AssertBorrowedObjectsUnchanged(clone, point, snapshot, changes, false);
            await UniTask.Yield();
            AssertBorrowedObjectsUnchanged(clone, point, snapshot, changes, false);

            _adapter.Bind(_input, _module, _uiMap.id);
            _lease = _input.Layers.AcquireLayer("ui");
            Assert.That(_uiMap.enabled, Is.True);
            Assert.That(_input.Layers.Snapshot.ActiveLayerIds, Is.EqualTo(new[] { "ui" }));
            _adapter.Refresh();
            _adapter.Unbind();
            Assert.That(_input.IsDisposed, Is.False);
            Assert.That(_module.actionsAsset, Is.SameAs(clone));
            _lease.Dispose();
            Assert.That(_uiMap.enabled, Is.False);
            Assert.That(_input.Layers.Snapshot.ActiveLayerIds, Is.Empty);
            Assert.That(_changedCount, Is.EqualTo(changes + 2));
        });
    }
}
