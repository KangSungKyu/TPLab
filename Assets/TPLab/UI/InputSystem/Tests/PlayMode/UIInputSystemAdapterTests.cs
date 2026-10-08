// P4 Red optional InputSystem adapter tests. New adapter APIs remain stubs before actual Red.
// Reuses InputIntegrationTests' runtime action clone + explicit native module references + QueueStateEvent.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UIAdapter = TPLab.UI.InputSystem.UIInputSystemAdapter;
using Object = UnityEngine.Object;

namespace TPLab.UI.Tests
{
    public sealed class UIInputSystemAdapterTests : InputTestFixture
    {
        private enum HeldControl
        {
            LeftPointer,
            RightPointer,
            MiddlePointer,
            Submit,
            Cancel,
            Move,
            Touch
        }

        private static async UniTask Frames(int count = 2)
        {
            for (int frame = 0; frame < count; ++frame)
            {
                await UniTask.NextFrame();
            }
        }

        [UnityTest]
        public IEnumerator HeldControlsRequireRawReleaseAndNextEventSystemFrameWithoutReplay()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var scope = new Scope();
                var a = await scope.Open("a");
                var c = await scope.Open("c");
                foreach (HeldControl control in Enum.GetValues(typeof(HeldControl)))
                {
                    await scope.WaitEnabled();
                    var b = await scope.Open("b", UIInputMode.Modal);
                    c.BringToFront();
                    c.SetFocus(c.ViewObject);
                    await Frames();
                    scope.QueueHeld(control);
                    await Frames();
                    await b.CloseAsync();
                    Assert.That(scope.Module.enabled, Is.False, control.ToString());
                    Assert.That(scope.Game.enabled, Is.False, "Outgoing modal still owns a deferred game-input block.");
                    Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("modal"));
                    Assert.That(scope.Events.currentSelectedGameObject, Is.SameAs(c.ViewObject));
                    var probe = c.ViewObject.GetComponent<UIP4NativeEventProbe>();
                    int eventsBeforeRecovery = probe.EventCount;
                    using (var suppressedUi = scope.Input.Layers.BlockAll())
                    {
                        scope.Module.enabled = true;
                        await Frames();
                        Assert.That(scope.Module.enabled || scope.Input.Actions.enabled, Is.False,
                            "Native re-enable must not bypass borrowed layer policy while the UI map is blocked.");
                        Assert.That(scope.Events.currentSelectedGameObject, Is.Null);
                        Assert.That(probe.EventCount, Is.EqualTo(eventsBeforeRecovery));
                    }
                    await Frames(3);
                    Assert.That(scope.Module.enabled, Is.False, "A disabled/re-enabled UI map must still inspect held raw controls.");
                    Assert.That(probe.EventCount, Is.EqualTo(eventsBeforeRecovery));
                    scope.QueueRelease();
                    // Process only the input event: no EventSystem frame has crossed this boundary yet.
                    UnityEngine.InputSystem.InputSystem.Update();
                    Assert.That(scope.Module.enabled, Is.False, "Release alone is insufficient before the next EventSystem frame.");
                    await scope.WaitEnabled();
                    Assert.That(probe.EventCount, Is.EqualTo(eventsBeforeRecovery), "Retired press/submit/cancel/move/touch must not replay.");
                    Assert.That(scope.Game.enabled, Is.True);
                    Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Not.Contain("modal"));
                    Assert.That(scope.Events.currentSelectedGameObject, Is.SameAs(c.ViewObject));
                }
                int submissions = c.ViewObject.GetComponent<UIP4NativeEventProbe>().Submits;
                scope.QueueHeld(HeldControl.Submit);
                await Frames();
                Assert.That(c.ViewObject.GetComponent<UIP4NativeEventProbe>().Submits, Is.EqualTo(submissions + 1),
                    "Fresh input after recovery must be usable.");
                scope.QueueRelease();
                await Frames();
                Assert.That(a.State, Is.EqualTo(UIState.Visible));
            });
        }

        [UnityTest]
        public IEnumerator IndependentModalAndTransitionLeasesSurviveMiddleRetirement()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var scope = new Scope();
                var a = await scope.Open("a");
                var b = await scope.Open("b", UIInputMode.Modal);
                var c = await scope.Open("c", UIInputMode.Modal);
                c.SetFocus(c.ViewObject);
                using var transition = scope.Input.Layers.AcquireLayer("transition");
                scope.QueueHeld(HeldControl.LeftPointer);
                await Frames();
                await b.CloseAsync();
                Assert.That(c.State, Is.EqualTo(UIState.Visible));
                Assert.That(scope.Events.currentSelectedGameObject, Is.SameAs(c.ViewObject));
                scope.QueueRelease();
                await scope.WaitEnabled();
                Assert.That(scope.Game.enabled, Is.False);
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("transition"));
                // Snapshot contains applied layers only, not every outstanding lease. Expose the lower modal explicitly.
                transition.Dispose();
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("modal"));
                Assert.That(scope.Game.enabled, Is.False, "Middle close must not release C's independent modal lease.");
                using var secondTransition = scope.Input.Layers.AcquireLayer("transition");
                await c.CloseAsync();
                await scope.WaitEnabled();
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Not.Contain("modal"));
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("transition"));
                Assert.That(scope.Game.enabled, Is.False);
                secondTransition.Dispose();
                await Frames();
                Assert.That(scope.Game.enabled, Is.True);
                Assert.That(a.State, Is.EqualTo(UIState.Visible));
                Assert.That(scope.Source != null && scope.Input.Actions != null && scope.Events != null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator CancelIsOptInRequestsOnceAndNeverReachesLowerUiAfterAcceptedClose()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var scope = new Scope();
                var a = await scope.Open("a");
                bool allowClose = false;
                int requested = 0;
                var b = await scope.Open("b", UIInputMode.Modal, new UIHooks
                {
                    CanCloseAsync = (_, reason, __) =>
                    {
                        Assert.That(reason, Is.EqualTo(UIUserCloseReason.Cancel));
                        ++requested;
                        return UniTask.FromResult(allowClose);
                    }
                });
                b.SetFocus(b.ViewObject);
                await scope.WaitEnabled();
                scope.QueueHeld(HeldControl.Cancel);
                await Frames(3);
                Assert.That(requested, Is.EqualTo(1));
                Assert.That(b.State, Is.EqualTo(UIState.Visible));
                Assert.That(a.ViewObject.GetComponent<UIP4NativeEventProbe>().Cancels, Is.Zero);
                scope.QueueRelease();
                await scope.WaitEnabled();
                allowClose = true;
                scope.QueueHeld(HeldControl.Cancel);
                for (int frame = 0; frame < 100 && b.State != UIState.Closed; ++frame)
                {
                    await UniTask.NextFrame();
                }
                await b.Closed;
                Assert.That(requested, Is.EqualTo(2));
                Assert.That(scope.Module.enabled, Is.False);
                await Frames(3);
                Assert.That(a.ViewObject.GetComponent<UIP4NativeEventProbe>().Cancels, Is.Zero);
                scope.QueueRelease();
                await scope.WaitEnabled();
                Assert.That(a.ViewObject.GetComponent<UIP4NativeEventProbe>().Cancels, Is.Zero);
                Assert.That(requested, Is.EqualTo(2));
            });
        }

        [UnityTest]
        public IEnumerator UnbindForceReleasesOnlyAdapterPendingLeasesAndPreservesBorrowedOwners()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var scope = new Scope();
                var a = await scope.Open("a");
                var b = await scope.Open("b", UIInputMode.Modal);
                var active = await scope.Open("c", UIInputMode.Modal);
                var external = scope.Input.Layers.AcquireLayer("modal");
                scope.QueueHeld(HeldControl.Submit);
                await Frames();
                await b.CloseAsync();
                Assert.That(scope.Module.enabled, Is.False);
                scope.Adapter.Unbind();
                Assert.That(scope.Module.enabled, Is.False);
                Assert.That(scope.Input.IsDisposed, Is.False);
                Assert.That(scope.Source != null && scope.Input.Actions != null && scope.Events != null, Is.True);
                Assert.That(scope.Module.actionsAsset, Is.SameAs(scope.Input.Actions));
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("modal"));
                external.Dispose();
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Contain("modal"),
                    "Unbind releases pending B only; active C keeps its independent context-owned lease.");
                Assert.That(scope.Game.enabled, Is.False);
                await active.CloseAsync();
                Assert.That(scope.Input.Layers.Snapshot.ActiveLayerIds, Does.Not.Contain("modal"));
                Assert.That(scope.Game.enabled, Is.True);
                scope.QueueRelease();
                await Frames();
                // The detached factory cannot acquire new Modal leases; compose modeless work to check unsubscription.
                a.BringToFront();
                await a.CloseAsync();
                await scope.Open("c");
                await Frames();
                Assert.That(scope.Module.enabled, Is.False, "Detached subscriptions must not reactivate the borrowed module.");
                Assert.That(scope.Input.IsDisposed, Is.False);
                Assert.That(scope.Root != null && scope.Module != null && scope.Events != null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator NativeAdapterApplicationFailureFaultsUiAndKeepsSafeBlockUntilOwnerShutdown()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var scope = new Scope();
                await scope.Open("a");
                await scope.WaitEnabled();
                // Actual borrowed native configuration is corrupted; no test-only public registration API is introduced.
                // Confirm at P4 integration that this validates through the internal friend-assembly application bridge.
                scope.Module.actionsAsset = scope.Source;
                Exception failure = null;
                try
                {
                    await scope.Open("b", UIInputMode.Modal);
                }
                catch (Exception error)
                {
                    failure = error;
                }
                Assert.That(failure, Is.Not.Null);
                Assert.That(scope.Context.Fault, Is.Not.Null);
                Assert.That(scope.Module.enabled, Is.False);
                Assert.That(scope.Game.enabled, Is.False);
                Assert.That(scope.Input.IsDisposed, Is.False);
                scope.Adapter.Unbind();
                Assert.That(scope.Game.enabled, Is.False, "Public Unbind cannot release the owner emergency block.");
                try
                {
                    await scope.Context.ShutdownAsync();
                }
                catch (Exception cleanupFailure)
                {
                    Assert.That(cleanupFailure.ToString(), Does.Contain(scope.Context.Fault.Message));
                }
                Assert.That(scope.Context.IsDisposed, Is.True);
                Assert.That(scope.Game.enabled, Is.True, "Owner shutdown releases its emergency block after owned UI cleanup.");
                Assert.That(scope.Source != null && scope.Input.Actions != null && scope.Root != null, Is.True);
                Assert.That(scope.Events != null && scope.Module != null, Is.True);
            });
        }

        private sealed class Scope : IDisposable
        {
            internal readonly InputActionAsset Source;
            internal readonly InputManager Input;
            internal readonly InputActionMap Game;
            internal readonly GameObject Root;
            internal readonly GameObject Host;
            internal readonly GameObject Prefab;
            internal readonly GameObject ModuleHost;
            internal readonly EventSystem Events;
            internal readonly InputSystemUIInputModule Module;
            internal readonly UIAdapter Adapter;
            internal readonly UIContext Context;
            private readonly Keyboard _keyboard;
            private readonly Mouse _mouse;
            private readonly Touchscreen _touch;
            private readonly List<InputActionReference> _references = new List<InputActionReference>();
            private readonly IDisposable _gameLease;
            private readonly IDisposable _uiLease;

            internal Scope()
            {
                try
                {
                    Source = InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
                    Input = new InputManager(Source);
                    _keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<Keyboard>();
                    _mouse = UnityEngine.InputSystem.InputSystem.AddDevice<Mouse>();
                    _touch = UnityEngine.InputSystem.InputSystem.AddDevice<Touchscreen>();
                    Input.Actions.devices = new InputDevice[] { _keyboard, _mouse, _touch };
                    Game = Input.Actions.FindActionMap("Player", true);
                    var ui = Input.Actions.FindActionMap("UI", true);
                    Input.Layers.RegisterLayer("game", new[] { Game.id }, 0, InputLayerMode.Overlay);
                    Input.Layers.RegisterLayer("ui", new[] { ui.id }, 2000, InputLayerMode.Overlay);
                    Input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
                    Input.Layers.RegisterLayer("transition", Array.Empty<Guid>(), 150, InputLayerMode.BlockLower);
                    _gameLease = Input.Layers.AcquireLayer("game");
                    _uiLease = Input.Layers.AcquireLayer("ui");
                    Root = new GameObject("P4 adapter UI owner");
                    Host = new GameObject("P4 adapter borrowed canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                    Host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    Prefab = new GameObject("P4 adapter source", typeof(RectTransform));
                    Prefab.SetActive(false);
                    var rect = (RectTransform)Prefab.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition3D = Vector3.zero;
                    Prefab.AddComponent<CanvasGroup>();
                    Prefab.AddComponent<Image>();
                    Prefab.AddComponent<Button>();
                    Prefab.AddComponent<UIP4NativeEventProbe>();
                    ModuleHost = new GameObject("P4 adapter native scope");
                    ModuleHost.SetActive(false);
                    Events = ModuleHost.AddComponent<EventSystem>();
                    Module = ModuleHost.AddComponent<InputSystemUIInputModule>();
                    Module.enabled = false;
                    Module.actionsAsset = Input.Actions;
                    Module.point = Ref("Point");
                    Module.leftClick = Ref("Click");
                    Module.rightClick = Ref("RightClick");
                    Module.middleClick = Ref("MiddleClick");
                    Module.move = Ref("Navigate");
                    Module.submit = Ref("Submit");
                    Module.cancel = Ref("Cancel");
                    Module.scrollWheel = Ref("ScrollWheel");
                    Module.trackedDevicePosition = Ref("TrackedDevicePosition");
                    Module.trackedDeviceOrientation = Ref("TrackedDeviceOrientation");
                    Adapter = ModuleHost.AddComponent<UIAdapter>();
                    Context = new UIContext(Root, acquireModalBlock: () => Adapter.AcquireModalBlock("modal"), eventSystem: Events);
                    Assert.That(Context.EventSystem, Is.SameAs(Events));
                    Context.RegisterHost("native", Host.transform);
                    foreach (string id in new[] { "a", "b", "c" })
                    {
                        Context.Register(new UIDefinition(id, Prefab, hostId: "native"));
                    }
                    Adapter.Bind(Context, Input, Module, ui.id);
                    ModuleHost.SetActive(true);
                    QueueRelease();

                    InputActionReference Ref(string name)
                    {
                        var reference = InputActionReference.Create(ui.FindAction(name, true));
                        _references.Add(reference);
                        return reference;
                    }
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal async UniTask<UIHandle> Open(string id, UIInputMode mode = UIInputMode.Modeless, UIHooks hooks = null)
            {
                var handle = await Context.OpenAsync(new UIOpenRequest(id, inputMode: mode, hooks: hooks));
                for (int frame = 0; frame < 10; ++frame)
                {
                    await UniTask.NextFrame();
                    Canvas.ForceUpdateCanvases();
                    if (handle.ViewObject.GetComponent<Image>().depth >= 0)
                    {
                        break;
                    }
                }
                Assert.That(handle.ViewObject.GetComponent<Image>().depth, Is.GreaterThanOrEqualTo(0));
                return handle;
            }

            internal async UniTask WaitEnabled()
            {
                for (int frame = 0; frame < 100 && !Module.enabled; ++frame)
                {
                    await UniTask.NextFrame();
                }
                Assert.That(Module.enabled, Is.True, "Native UI module did not recover after all raw controls were released.");
                Assert.That(Input.Actions.FindActionMap("UI", true).enabled, Is.True);
            }

            internal void QueueHeld(HeldControl control)
            {
                var position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (control == HeldControl.Submit || control == HeldControl.Cancel || control == HeldControl.Move)
                {
                    Key key = control == HeldControl.Submit ? Key.Enter : control == HeldControl.Cancel ? Key.Escape : Key.RightArrow;
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
                }
                else if (control == HeldControl.Touch)
                {
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(_touch, new TouchState
                    {
                        touchId = 1,
                        phase = UnityEngine.InputSystem.TouchPhase.Began,
                        position = position
                    });
                }
                else
                {
                    MouseButton button = control == HeldControl.LeftPointer ? MouseButton.Left
                        : control == HeldControl.RightPointer ? MouseButton.Right : MouseButton.Middle;
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(_mouse, new MouseState { position = position }.WithButton(button));
                }
            }

            internal void QueueRelease()
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(_mouse, new MouseState
                {
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                });
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(_touch, new TouchState
                {
                    touchId = 1,
                    phase = UnityEngine.InputSystem.TouchPhase.Ended,
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                });
            }

            public void Dispose()
            {
                try
                {
                    Adapter?.Unbind();
                }
                catch (NotImplementedException)
                {
                    // Before actual Red, Unbind is a declared stub; fixture ownership still requires native cleanup.
                }
                try
                {
                    Context?.Dispose();
                }
                catch (Exception) when (Context != null && Context.Fault != null)
                {
                    // The native-failure case already observes the context fault and shared shutdown result.
                }
                finally
                {
                    if (Module != null)
                    {
                        Module.enabled = false;
                        Module.actionsAsset = null;
                    }
                    _uiLease?.Dispose();
                    _gameLease?.Dispose();
                    Input?.Dispose();
                    foreach (var device in new InputDevice[] { _keyboard, _mouse, _touch })
                    {
                        if (device != null)
                        {
                            UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
                        }
                    }
                    foreach (var reference in _references)
                    {
                        Object.Destroy(reference);
                    }
                    Object.Destroy(ModuleHost);
                    Object.Destroy(Prefab);
                    Object.Destroy(Host);
                    Object.Destroy(Root);
                    Object.Destroy(Source);
                }
            }
        }
    }

    public sealed class UIP4NativeEventProbe : MonoBehaviour, ISubmitHandler, ICancelHandler, IMoveHandler, IPointerClickHandler
    {
        public int Submits { get; private set; }
        public int Cancels { get; private set; }
        public int Moves { get; private set; }
        public int Clicks { get; private set; }
        public int EventCount => Submits + Cancels + Moves + Clicks;

        public void OnSubmit(BaseEventData eventData)
        {
            ++Submits;
        }

        public void OnCancel(BaseEventData eventData)
        {
            ++Cancels;
        }

        public void OnMove(AxisEventData eventData)
        {
            ++Moves;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ++Clicks;
        }
    }
}