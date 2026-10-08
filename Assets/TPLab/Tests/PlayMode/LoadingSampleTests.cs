using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TPLab.Samples.SceneTransitions.Tests
{
    public sealed class LoadingSampleTests : InputTestFixture
    {
        private GameObject _host;
        private InputActionAsset _source;
        private SceneTransitionSampleController _controller;
        private CanvasGroup _cover;
        private CanvasGroup _modal;
        private SceneLoadingContext _context;
        private readonly List<InputDevice> _devices = new List<InputDevice>();

        [SetUp]
        public void SetUp()
        {
            _source = InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
            _host = new GameObject("Loading sample test", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _host.SetActive(false);
            _host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _host.AddComponent<EventSystem>();
            var module = _host.AddComponent<InputSystemUIInputModule>();
            _cover = Group("Cover");
            _modal = Group("Modal");
            _controller = _host.AddComponent<SceneTransitionSampleController>();
            _controller.ConfigureView(_source, _cover, _modal, module);
            _controller.ConfigureLoadingPresentation(true, true);
            _host.SetActive(true);
            _controller.RefreshGameplayPermission(true);
        }

        [UnityTearDown]
        public IEnumerator CleanupAsync() => UniTask.ToCoroutine(async () =>
        {
            if (_host != null)
            {
                Object.Destroy(_host);
            }
            if (_source != null)
            {
                Object.Destroy(_source);
            }
            foreach (var device in _devices)
            {
                if (device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }
            _devices.Clear();
            await UniTask.NextFrame();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator LoadingRevealKeepsTransitionAndIndependentModalLeases() => UniTask.ToCoroutine(async () =>
        {
            _controller.SetModalOpen(true);
            await ShowLoadingAsync();
            Assert.That(_controller.UsesLoadingPresentation(_context), Is.True);
            Assert.That(_controller.LoadingPanel.transform.IsChildOf(_cover.transform), Is.False);
            Assert.That(_controller.LoadingPanel.alpha, Is.EqualTo(1));
            Assert.That(_controller.LoadingPanel.blocksRaycasts, Is.True);
            Assert.That(_controller.LoadingPanel.GetComponentsInChildren<Image>().Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(_controller.LoadingPanel.GetComponentsInChildren<Text>().Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            Assert.That(_cover.alpha, Is.Zero);
            Assert.That(_controller.TransitionBlocked && _controller.ModalBlocked, Is.True);
            Assert.That(_controller.PlayerInputEnabled, Is.False);
            Assert.That(_controller.UiInputEnabled, Is.True);
            await _controller.ReleaseLoadingPresentationAsync(_context);
            await _controller.ReleaseLoadingPresentationAsync(_context);
            Assert.That(_controller.LoadingPanel.gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.TransitionBlocked && _controller.ModalBlocked, Is.True);
            await _controller.HideCoverAsync(default);
            Assert.That(_controller.TransitionBlocked, Is.False);
            Assert.That(_controller.ModalBlocked, Is.True);
            Assert.That(_controller.PlayerInputEnabled, Is.False);
        });

        [UnityTest]
        public IEnumerator HeldKeyboardSubmitRequiresReleaseThenFreshSubmit() => UniTask.ToCoroutine(async () =>
        {
            var keyboard = Device<Keyboard>();
            await ShowLoadingAsync();
            SelectContinue();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            await FramesAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.True);
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            SelectContinue();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            await wait;
            Assert.That(_controller.ProceedCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator HeldGamepadSubmitRequiresReleaseThenFreshSubmit() => UniTask.ToCoroutine(async () =>
        {
            var gamepad = Device<Gamepad>();
            await ShowLoadingAsync();
            SelectContinue();
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            await FramesAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.True);
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            SelectContinue();
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            await wait;
        });

        [UnityTest]
        public IEnumerator HeldPointerReleaseDoesNotProceedAndFreshClickDoes() => UniTask.ToCoroutine(async () =>
        {
            var mouse = Device<Mouse>();
            await ShowLoadingAsync();
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)_controller.ContinueButton.transform;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            await FramesAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_controller.ContinueButton.interactable, Is.True);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            await FramesAsync();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            await wait;
        });

        [UnityTest]
        public IEnumerator EarlyAndDuplicateClicksDoNotCompleteLaterOperation() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            var previous = _context;
            _controller.ContinueButton.onClick.Invoke();
            var first = _controller.WaitForProceedAsync(_context, default).Preserve();
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Pending));
            await FramesAsync();
            _controller.ContinueButton.onClick.Invoke();
            _controller.ContinueButton.onClick.Invoke();
            await first;
            Assert.That(_controller.ProceedCount, Is.EqualTo(1));
            await _controller.ReleaseLoadingPresentationAsync(_context);
            _controller.ContinueButton.onClick.Invoke();
            await ShowLoadingAsync();
            Assert.That(_context.OperationId, Is.Not.EqualTo(previous.OperationId));
            await _controller.ReleaseLoadingPresentationAsync(previous);
            int reports = _controller.LoadingProgressCount;
            _controller.ReportLoadingProgress(Progress(previous, .9f));
            Assert.That(_controller.LoadingProgressCount, Is.EqualTo(reports));
            Assert.That(_controller.LoadingPanel.gameObject.activeInHierarchy, Is.True);
            try
            {
                await _controller.WaitForProceedAsync(previous, default);
                Assert.Fail("A stale context must not wait on the current operation.");
            }
            catch (InvalidOperationException)
            {
            }
            _controller.ContinueButton.onClick.Invoke();
            var second = _controller.WaitForProceedAsync(_context, default).Preserve();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
            await FramesAsync();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
            _controller.ContinueButton.onClick.Invoke();
            await second;
            Assert.That(_controller.ProceedCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator CancelledWaitReleasesListenersWithoutReleasingTransition() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            using var cancellation = new CancellationTokenSource();
            var wait = _controller.WaitForProceedAsync(_context, cancellation.Token).Preserve();
            await FramesAsync();
            cancellation.Cancel();
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Canceled));
            try
            {
                await wait;
                Assert.Fail("Owner cancellation must not become continuation.");
            }
            catch (OperationCanceledException)
            {
            }
            await _controller.ReleaseLoadingPresentationAsync(_context);
            _controller.ContinueButton.onClick.Invoke();
            Assert.That(_controller.ProceedCount, Is.Zero);
            Assert.That(_controller.LoadingPanel.gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.TransitionBlocked, Is.True);
        });

        [UnityTest]
        public IEnumerator DestroyedContinueButtonFailsPendingWait() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Object.Destroy(_controller.ContinueButton.gameObject);
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Faulted));
            try
            {
                await wait;
                Assert.Fail("Destroyed loading UI must fail its operation.");
            }
            catch (InvalidOperationException)
            {
            }
            await _controller.ReleaseLoadingPresentationAsync(_context);
            Assert.That(_controller.TransitionBlocked, Is.True);
        });

        [UnityTest]
        public IEnumerator DestroyedUiOwnerCancelsPendingWait() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Object.Destroy(_host);
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Canceled));
            try
            {
                await wait;
                Assert.Fail("Destroyed project owner must cancel its wait.");
            }
            catch (OperationCanceledException)
            {
            }
        });

        [UnityTest]
        public IEnumerator SecondaryPointerButtonsAlsoRequireNeutralBeforeArming() => UniTask.ToCoroutine(async () =>
        {
            var mouse = Device<Mouse>();
            await ShowLoadingAsync();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Middle));
            await FramesAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle));
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            await FramesAsync();
            Assert.That(_controller.ContinueButton.interactable, Is.True);
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            _controller.ContinueButton.onClick.Invoke();
            await wait;
        });

        [UnityTest]
        public IEnumerator ProgressShowsStageRatioAndUnknownWorkWithoutChangingActivePolicy() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            Assert.Throws<InvalidOperationException>(() => _controller.ConfigureLoadingPresentation(true, false));
            _controller.ReportLoadingProgress(Progress(_context, .4f));
            var bar = _controller.LoadingPanel.transform.Find("RaycastBlocker/ProgressTrack/StageProgress").GetComponent<Image>();
            var text = _controller.LoadingPanel.transform.Find("RaycastBlocker/Stage").GetComponent<Text>();
            Assert.That(bar.rectTransform.anchorMax.x, Is.EqualTo(.4f));
            Assert.That(text.text, Does.Contain("40%"));
            _controller.ReportLoadingProgress(Progress(_context, null));
            Assert.That(bar.rectTransform.anchorMax.x, Is.Zero);
            Assert.That(text.text, Does.Contain("working"));
            Assert.That(_controller.ContinueButton.interactable, Is.False);
            Assert.That(_controller.ProceedCount, Is.Zero);
        });

        [UnityTest]
        public IEnumerator AutomaticContinuationDoesNotWaitForHeldSubmitOrPointerRelease() => UniTask.ToCoroutine(async () =>
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            _devices.Add(keyboard);
            _devices.Add(mouse);
            _controller.Input.Actions.devices = new InputDevice[] { keyboard, mouse };
            _controller.ConfigureLoadingPresentation(true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            await FramesAsync();
            await ShowLoadingAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            await wait;
            Assert.That(_controller.ProceedCount, Is.EqualTo(1));
            Assert.That(_controller.ContinueButton.interactable, Is.False);
        });

        [UnityTest]
        public IEnumerator PreparedManualUiShowsReadyAndFullStageBarWhileStillWaiting() => UniTask.ToCoroutine(async () =>
        {
            Device<Keyboard>();
            await ShowLoadingAsync();
            var wait = _controller.WaitForProceedAsync(_context, default).Preserve();
            await FramesAsync();
            Assert.That(wait.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_controller.LoadingPanel.GetComponentsInChildren<Text>().Any(label => label.text.StartsWith("Ready")), Is.True);
            var bar = _controller.LoadingPanel.GetComponentsInChildren<Image>().Single(image => image.name == "StageProgress");
            Assert.That(bar.rectTransform.anchorMax.x, Is.EqualTo(1));
            _controller.ContinueButton.onClick.Invoke();
            await wait;
        });

        private async UniTask ShowLoadingAsync()
        {
            // Core alone creates accepted identities; reflection supplies distinct snapshots without expanding its API.
            var request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, SceneTarget.BuildScene("Assets/TPLab/Tests/Fixtures/BootstrapHub.unity"));
            _context = (SceneLoadingContext)Activator.CreateInstance(typeof(SceneLoadingContext),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { request }, null);
            await _controller.ShowCoverAsync(default);
            await _controller.PrepareLoadingPresentationAsync(_context, default);
            Assert.That(_controller.LoadingPanel, Is.Not.Null, "Opt-in preparation must create project-owned loading UI.");
            Assert.That(_controller.ContinueButton, Is.Not.Null);
            await _controller.RevealLoadingPresentationAsync(_context, default);
        }

        private static SceneTransitionProgress Progress(SceneLoadingContext context, float? ratio) =>
            (SceneTransitionProgress)Activator.CreateInstance(typeof(SceneTransitionProgress), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { context, SceneTransitionState.Loading, ratio, false }, null);

        private T Device<T>() where T : InputDevice, new()
        {
            var device = InputSystem.AddDevice<T>();
            _devices.Add(device);
            _controller.Input.Actions.devices = new InputDevice[] { device };
            return device;
        }

        private void SelectContinue() => _host.GetComponent<EventSystem>().SetSelectedGameObject(_controller.ContinueButton.gameObject);

        private static async UniTask FramesAsync()
        {
            for (int i = 0; i < 6; i++)
            {
                await UniTask.NextFrame();
            }
        }

        private CanvasGroup Group(string name)
        {
            var group = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            group.transform.SetParent(_host.transform, false);
            var rect = (RectTransform)group.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return group.GetComponent<CanvasGroup>();
        }
    }
}
