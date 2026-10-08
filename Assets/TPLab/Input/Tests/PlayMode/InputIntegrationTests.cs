using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Samples.SceneTransitions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input.Tests
{
    public sealed class InputIntegrationTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator SceneOwnedScopeWaitsForRootAndPublishesExplicitly() => RootScopeAsync(false).ToCoroutine();

        [UnityTest]
        public IEnumerator SingletonScopeUsesSameExplicitPublicationContract() => RootScopeAsync(true).ToCoroutine();

        private static async UniTask RootScopeAsync(bool singleton)
        {
            var source = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = source.AddActionMap("Game");
            map.AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
            var host = new GameObject("Input root");
            host.SetActive(false);
            try
            {
                var installer = host.AddComponent<InputManagerInstaller>();
                installer.Configure(source);
                ISceneRoot root;
                if (singleton)
                {
                    var singletonRoot = host.AddComponent<SingletonSceneRoot>();
                    singletonRoot.Configure(new SceneRootInstaller[] { installer });
                    root = singletonRoot;
                }
                else
                {
                    var sceneRoot = host.AddComponent<SceneOwnedRoot>();
                    sceneRoot.Configure(new SceneRootInstaller[] { installer });
                    root = sceneRoot;
                }
                host.SetActive(true);
                Assert.That(root.IsReady, Is.True);
                var input = installer.Input;
                input.Layers.RegisterLayer("game", new[] { map.id }, 0, InputLayerMode.Overlay);
                var lease = input.Layers.AcquireLayer("game");
                Assert.That(input.Actions.enabled, Is.False);
                Assert.Throws<InvalidOperationException>(installer.CompletePreparation);
                await root.PrepareAsync();
                Assert.That(input.Actions.enabled, Is.False);
                installer.CompletePreparation();
                Assert.That(input.Actions.enabled, Is.True);
                var clone = input.Actions;
                await root.ShutdownAsync();
                Assert.That(installer.Input, Is.Null);
                Assert.DoesNotThrow(lease.Dispose);
                await UniTask.NextFrame();
                Assert.That(clone == null && source != null, Is.True);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(source);
            }
        }

        [UnityTest]
        public IEnumerator WholeScopeBlockDisablesUiModuleAndReactivationCannotBypassIt() => UniTask.ToCoroutine(async () =>
        {
            var source = InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
            var host = new GameObject("UI scope", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = host.GetComponent<InputSystemUIInputModule>();
            var references = new System.Collections.Generic.List<InputActionReference>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            using var input = new InputManager(source);
            try
            {
                input.Actions.devices = new InputDevice[] { keyboard };
                module.enabled = false;
                var ui = input.Actions.FindActionMap("UI", true);
                module.actionsAsset = input.Actions;
                module.point = Ref("Point");
                module.leftClick = Ref("Click");
                module.move = Ref("Navigate");
                module.submit = Ref("Submit");
                module.cancel = Ref("Cancel");
                input.Layers.RegisterLayer("ui", new[] { ui.id }, 100, InputLayerMode.Overlay);
                var adapter = host.AddComponent<InputSystemUiScope>();
                adapter.Bind(input, module, ui.id);
                using var uiLease = input.Layers.AcquireLayer("ui");
                await UniTask.NextFrame();
                adapter.Refresh();
                Assert.That(module.enabled && ui.enabled, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                var block = input.Layers.BlockAll();
                Assert.That(module.enabled || input.Actions.enabled, Is.False);
                module.enabled = true;
                adapter.Refresh();
                Assert.That(module.enabled || input.Actions.enabled, Is.False);
                block.Dispose();
                await UniTask.NextFrame();
                adapter.Refresh();
                Assert.That(module.enabled, Is.False, "Held Submit must not replay through a newly activated UI module.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                adapter.Refresh();
                Assert.That(module.enabled && ui.enabled, Is.True);
                Assert.That(module.actionsAsset, Is.SameAs(input.Actions));
                adapter.Unbind();

                InputActionReference Ref(string name)
                {
                    var reference = InputActionReference.Create(ui.FindAction(name, true));
                    references.Add(reference);
                    return reference;
                }
            }
            finally
            {
                module.enabled = false;
                module.actionsAsset = null;
                input.Dispose();
                InputSystem.RemoveDevice(keyboard);
                foreach (var reference in references)
                {
                    Object.Destroy(reference);
                }
                Object.Destroy(host);
                Object.Destroy(source);
            }
        });

        [UnityTest]
        public IEnumerator SubmitOpeningModalDoesNotSubmitNewFocusInSameFrame() => UniTask.ToCoroutine(async () =>
        {
            var source = InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
            var host = new GameObject("Submit boundary", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = host.GetComponent<InputSystemUIInputModule>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var reference = InputActionReference.Create(source.FindAction("UI/Submit", true));
            var first = new GameObject("Open modal", typeof(RectTransform), typeof(Button));
            var second = new GameObject("Confirm modal", typeof(RectTransform), typeof(Button));
            first.transform.SetParent(host.transform);
            second.transform.SetParent(host.transform);
            using var input = new InputManager(source);
            InputActionReference runtimeReference = null;
            IDisposable modal = null;
            try
            {
                module.enabled = false;
                input.Actions.devices = new InputDevice[] { keyboard };
                var ui = input.Actions.FindActionMap("UI", true);
                module.actionsAsset = input.Actions;
                runtimeReference = InputActionReference.Create(input.GetAction(reference));
                module.submit = runtimeReference;
                input.Layers.RegisterLayer("ui", new[] { ui.id }, 2000, InputLayerMode.Overlay);
                input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
                var adapter = host.AddComponent<InputSystemUiScope>();
                adapter.Bind(input, module, ui.id);
                using var uiLease = input.Layers.AcquireLayer("ui");
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                int opened = 0;
                int confirmed = 0;
                first.GetComponent<Button>().onClick.AddListener(() =>
                {
                    opened++;
                    modal = input.Layers.AcquireLayer("modal");
                    host.GetComponent<EventSystem>().SetSelectedGameObject(second);
                });
                second.GetComponent<Button>().onClick.AddListener(() => confirmed++);
                host.GetComponent<EventSystem>().SetSelectedGameObject(first);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(opened, Is.EqualTo(1));
                Assert.That(confirmed, Is.Zero);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(confirmed, Is.EqualTo(1));
                adapter.Unbind();
            }
            finally
            {
                modal?.Dispose();
                module.enabled = false;
                module.actionsAsset = null;
                input.Dispose();
                InputSystem.RemoveDevice(keyboard);
                Object.Destroy(runtimeReference);
                Object.Destroy(reference);
                Object.Destroy(host);
                Object.Destroy(source);
            }
        });
    }
}
