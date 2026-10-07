using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputRuntimeTests
    {
        [UnityTest]
        public IEnumerator CancelledActionCanAcquireAnotherLayerDuringRecalculation()
        {
            var source = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = source.AddActionMap("Game");
            var fire = map.AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
            var popupMap = source.AddActionMap("Popup");
            popupMap.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            using var input = new InputManager(source);
            try
            {
                input.Actions.devices = new InputDevice[] { keyboard };
                input.Layers.RegisterLayer("game", new[] { map.id }, 0, InputLayerMode.Overlay);
                input.Layers.RegisterLayer("popup", new[] { popupMap.id }, 100, InputLayerMode.BlockLower);
                using var game = input.Layers.AcquireLayer("game");
                var action = input.GetAction(fire.id);
                IDisposable popup = null;
                action.canceled += _ => popup ??= input.Layers.AcquireLayer("popup");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null;
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                var block = input.Layers.BlockAll();
                Assert.That(popup, Is.Not.Null);
                Assert.That(input.Actions.enabled, Is.False);
                block.Dispose();
                Assert.That(input.Actions.FindActionMap(popupMap.id.ToString(), true).enabled, Is.True);
                popup.Dispose();
            }
            finally
            {
                input.Dispose();
                InputSystem.RemoveDevice(keyboard);
                Object.Destroy(source);
            }
        }
    }
}
