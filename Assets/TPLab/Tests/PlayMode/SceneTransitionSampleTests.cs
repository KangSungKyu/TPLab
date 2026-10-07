using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using TPLab.Samples.SceneTransitions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TPLab.Core.Tests
{
    public sealed class SceneTransitionSampleTests
    {
        private GameObject _host;
        private InputActionAsset _source;
        private SceneTransitionSampleController _controller;
        private CanvasGroup _cover;
        private CanvasGroup _modal;
        private Image _coverImage;
        private InputSystemUIInputModule _module;

        [SetUp]
        public void SetUp()
        {
            _source = InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
            _host = new GameObject("SceneTransitionSampleInputTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _host.SetActive(false);
            _host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _host.AddComponent<EventSystem>();
            _module = _host.AddComponent<InputSystemUIInputModule>();
            _cover = Group("Cover", out _coverImage);
            _modal = Group("Modal", out _);
            _controller = _host.AddComponent<SceneTransitionSampleController>();
            _controller.ConfigureView(_source, _cover, _modal, _module);
            _host.SetActive(true);
            _controller.RefreshGameplayPermission(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            if (_host != null) Object.Destroy(_host);
            if (_source != null) Object.Destroy(_source);
            await UniTask.Yield();
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest]
        public IEnumerator TransitionCoverBlocksPlayerAndPointerButKeepsRealUiMapEnabled() => UniTask.ToCoroutine(async () =>
        {
            await _controller.ShowCoverAsync(default);
            Assert.That(_controller.PlayerInputEnabled, Is.False);
            Assert.That(_controller.UiInputEnabled, Is.True);
            Assert.That(_controller.TransitionBlocked, Is.True);
            Assert.That(_cover.alpha, Is.EqualTo(1));
            Assert.That(_cover.blocksRaycasts && _coverImage.raycastTarget, Is.True);
            Assert.That(_module.point.action.enabled && _module.leftClick.action.enabled, Is.True);
            Assert.That(_module.actionsAsset, Is.Not.SameAs(_source));
            Assert.That(_source.FindActionMap("Player", true).enabled, Is.False);
        });

        [UnityTest]
        public IEnumerator RevealDoesNotReleasePlayerWhileIndependentModalRemainsOpen() => UniTask.ToCoroutine(async () =>
        {
            _controller.SetModalOpen(true);
            await _controller.ShowCoverAsync(default);
            await _controller.HideCoverAsync(default);
            _controller.RefreshGameplayPermission(true);
            Assert.That(_controller.PlayerInputEnabled, Is.False);
            Assert.That(_controller.UiInputEnabled, Is.True);
            Assert.That(_controller.ModalBlocked, Is.True);
            Assert.That(_modal.alpha, Is.EqualTo(1));
            Assert.That(_modal.blocksRaycasts, Is.True);
            Assert.That(_controller.TransitionBlocked, Is.False);
            Assert.That(_cover.blocksRaycasts, Is.False);
            _controller.SetModalOpen(false);
            Assert.That(_controller.PlayerInputEnabled, Is.True);
        });

        private CanvasGroup Group(string name, out Image image)
        {
            var group = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            group.transform.SetParent(_host.transform, false);
            image = group.GetComponent<Image>();
            image.raycastTarget = true;
            var rect = (RectTransform)group.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return group.GetComponent<CanvasGroup>();
        }
    }
}
