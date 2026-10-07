using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputOverrideTests
    {
        private InputActionAsset _source;
        private InputManager _input;
        private InputAction _action;

        [SetUp]
        public void SetUp()
        {
            _source = ScriptableObject.CreateInstance<InputActionAsset>();
            _action = _source.AddActionMap("Game").AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
            _input = new InputManager(_source);
            _input.Layers.RegisterLayer("game", new[] { _action.actionMap.id }, 0, InputLayerMode.Overlay);
        }

        [TearDown]
        public void TearDown()
        {
            _input.Dispose();
            Object.DestroyImmediate(_source);
        }

        private string Overrides(string path)
        {
            var candidate = Object.Instantiate(_source);
            try
            {
                candidate.FindAction(_action.id).ApplyBindingOverride(0, path);
                return candidate.SaveBindingOverridesAsJson();
            }
            finally
            {
                Object.DestroyImmediate(candidate);
            }
        }

        [Test]
        public void ImportExportResetPreserveSourceAndCurrentLayers()
        {
            using var game = _input.Layers.AcquireLayer("game");
            _input.Rebinding.ImportOverridesJson(Overrides("<Keyboard>/k"));
            var runtime = _input.GetAction(_action.id);
            Assert.That(runtime.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/k"));
            Assert.That(runtime.enabled, Is.True);
            Assert.That(_input.Rebinding.ExportOverridesJson(), Does.Contain("<Keyboard>/k"));
            Assert.That(_action.bindings[0].overridePath, Is.Null);
            _input.Rebinding.ResetBinding(_action.id, _action.bindings[0].id);
            Assert.That(runtime.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
            _input.Rebinding.ImportOverridesJson(Overrides("<Keyboard>/k"));
            _input.Rebinding.ResetAll();
            Assert.That(_input.Rebinding.ExportOverridesJson(), Is.EqualTo(""));
        }

        [TestCase("{}")]
        [TestCase("[]")]
        [TestCase("not json")]
        public void MalformedJsonCannotEraseExistingOverrides(string invalid)
        {
            _input.Rebinding.ImportOverridesJson(Overrides("<Keyboard>/k"));
            Assert.Throws<ArgumentException>(() => _input.Rebinding.ImportOverridesJson(invalid));
            Assert.That(_input.GetAction(_action.id).bindings[0].effectivePath, Is.EqualTo("<Keyboard>/k"));
        }

        [Test]
        public void UnknownAndDuplicateBindingIdsCannotPartiallyApply()
        {
            string valid = Overrides("<Keyboard>/k");
            string unknown = valid.Replace(_action.bindings[0].id.ToString(), Guid.NewGuid().ToString());
            Assert.Throws<ArgumentException>(() => _input.Rebinding.ImportOverridesJson(unknown));
            Assert.That(_input.GetAction(_action.id).bindings[0].overridePath, Is.Null);
            int begin = valid.IndexOf('[') + 1;
            string entry = valid.Substring(begin, valid.LastIndexOf(']') - begin);
            string duplicate = valid.Replace(entry, entry + "," + entry);
            Assert.Throws<ArgumentException>(() => _input.Rebinding.ImportOverridesJson(duplicate));
            Assert.Throws<ArgumentException>(() => _input.Rebinding.ImportOverridesJson(valid.Replace("Game/Fire", "Missing/Action")));
            Assert.Throws<ArgumentException>(() => _input.Rebinding.ResetBinding(_action.id, Guid.NewGuid()));
        }

        [Test]
        public void EmptyNativeJsonRoundTripsAndIndependentBlockRemainsHeld()
        {
            using var game = _input.Layers.AcquireLayer("game");
            using var block = _input.Layers.BlockAll();
            _input.Rebinding.ImportOverridesJson(Overrides("<Keyboard>/k"));
            _input.Rebinding.ImportOverridesJson("");
            Assert.That(_input.Rebinding.ExportOverridesJson(), Is.EqualTo(""));
            Assert.That(_input.Actions.enabled, Is.False);
            Assert.Throws<ArgumentNullException>(() => _input.Rebinding.ImportOverridesJson(null));
        }
    }
}
