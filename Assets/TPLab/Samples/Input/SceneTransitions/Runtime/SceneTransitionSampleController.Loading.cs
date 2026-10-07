using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace TPLab.Samples.SceneTransitions
{
    public sealed partial class SceneTransitionSampleController
    {
        [SerializeField] private bool _useLoadingPresentation;
        [SerializeField] private bool _manualProceed;
        [SerializeField] private string[] _loadingTips = { "Scenes are prepared before gameplay resumes.", "A system modal can keep gameplay blocked after loading." };
        private CanvasGroup _loadingPanel;
        private Button _continueButton;
        private Text _loadingStage;
        private Text _loadingTip;
        private Image _loadingBar;
        private LoadingOperation _loadingOperation;
        private bool _loadingLeaseObserved = true;

        private sealed class LoadingOperation
        {
            public Guid Id;
            public bool Manual;
            public bool Waiting;
            public bool Armed;
            public bool Completed;
            public int FirstCoverCount;
            public CancellationToken WaitToken;
            public UnityAction Listener;
            public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();
        }

        /// <summary>Borrowed operation UI, created and released by this project-owned controller.</summary>
        public CanvasGroup LoadingPanel => _loadingPanel;
        /// <summary>Borrowed Continue button; disabled until preparation and the input release boundary finish.</summary>
        public Button ContinueButton => _continueButton;
        /// <summary>Number of operation-specific continuation completions.</summary>
        public int ProceedCount
        {
            get;
            private set;
        }
        /// <summary>Observed operation preparation count, including later failed scene preparation.</summary>
        public int LoadingPrepareCount
        {
            get;
            private set;
        }
        /// <summary>Observed loading-only reveals; these retain the transition lease.</summary>
        public int LoadingRevealCount
        {
            get;
            private set;
        }
        /// <summary>Progress snapshots accepted for the currently displayed operation.</summary>
        public int LoadingProgressCount
        {
            get;
            private set;
        }
        /// <summary>Operations whose loading UI/listeners have been released exactly once.</summary>
        public int LoadingReleaseCount
        {
            get;
            private set;
        }
        /// <summary>Completed continuations released beneath their second cover.</summary>
        public int LoadingTwoCoverCount
        {
            get;
            private set;
        }

        /// <summary>Configures optional project presentation. Automatic continuation is the default.</summary>
        public void ConfigureLoadingPresentation(bool enabled, bool manualProceed = false)
        {
            if (_loadingOperation != null)
            {
                throw new InvalidOperationException("Loading presentation policy cannot change during an operation.");
            }
            _useLoadingPresentation = enabled;
            _manualProceed = manualProceed;
        }

        /// <inheritdoc />
        public override bool UsesLoadingPresentation(SceneLoadingContext context) => _useLoadingPresentation;
        /// <inheritdoc />
        public override UniTask PrepareLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_loadingOperation != null)
            {
                throw new InvalidOperationException("Release the previous loading presentation before preparing another.");
            }
            if (_input == null || _input.IsDisposed)
            {
                throw new InvalidOperationException("Loading UI requires the live sample input owner.");
            }
            RequireLoadingPermission();
            var operation = new LoadingOperation
            {
                Id = context.OperationId,
                Manual = _manualProceed,
                FirstCoverCount = CoverCount
            };
            _loadingOperation = operation;
            LoadingPrepareCount++;
            try
            {
                CreateLoadingUi();
                _loadingPanel.gameObject.SetActive(true);
                SetVisible(_loadingPanel, true);
                _continueButton.interactable = false;
                SetLoadingRatio(0);
                _loadingStage.text = "Preparing loading presentation...";
                _loadingTip.text = _loadingTips != null && _loadingTips.Length > 0
                    ? _loadingTips[(LoadingPrepareCount - 1) % _loadingTips.Length] : "Gameplay resumes after preparation and final reveal.";
                operation.Listener = () => CompleteProceed(operation);
                _continueButton.onClick.AddListener(operation.Listener);
                ObserveLoadingLease();
            }
            catch
            {
                CleanupLoading();
                throw;
            }
            return UniTask.CompletedTask;
        }
        /// <inheritdoc />
        public override UniTask RevealLoadingPresentationAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireLoadingOperation(context);
            SetVisible(_cover, false);
            LoadingRevealCount++;
            ObserveLoadingLease();
            return UniTask.CompletedTask;
        }
        /// <inheritdoc />
        public override void ReportLoadingProgress(SceneTransitionProgress progress)
        {
            if (_loadingOperation == null || progress.OperationId != _loadingOperation.Id)
            {
                return;
            }
            RequireLoadingUi();
            LoadingProgressCount++;
            _loadingStage.text = progress.StageRatio.HasValue
                ? progress.Stage + ": " + Mathf.RoundToInt(progress.StageRatio.Value * 100) + "%"
                : progress.Stage + ": working...";
            SetLoadingRatio(progress.StageRatio ?? 0);
            ObserveLoadingLease();
        }
        /// <inheritdoc />
        public override async UniTask WaitForProceedAsync(SceneLoadingContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var operation = RequireLoadingOperation(context);
            if (operation.Waiting)
            {
                throw new InvalidOperationException("One loading operation has only one continuation wait.");
            }
            operation.Waiting = true;
            _loadingStage.text = operation.Manual ? "Ready. Release input, then Continue." : "Ready. Opening game...";
            SetLoadingRatio(1f);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            var token = linked.Token;
            operation.WaitToken = token;
            try
            {
                RequireWaitingOperation(operation, token);
                if (!operation.Manual)
                {
                    operation.Armed = true;
                    CompleteProceed(operation);
                    await operation.Completion.Task;
                    return;
                }
                // The enabled native module consumes pointer-up before this button can accept a new click.
                do
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    RequireWaitingOperation(operation, token);
                    if (!LoadingButtonsReleased())
                    {
                        continue;
                    }
                    await UniTask.NextFrame(cancellationToken: token);
                    RequireWaitingOperation(operation, token);
                    if (LoadingButtonsReleased())
                    {
                        break;
                    }
                }
                while (true);
                if (!_module.enabled)
                {
                    throw new InvalidOperationException("The sample UI module was not restored after its input release boundary.");
                }
                operation.Armed = true;
                _continueButton.interactable = true;
                _module.GetComponent<EventSystem>().SetSelectedGameObject(_continueButton.gameObject);
                while (operation.Completion.Task.Status == UniTaskStatus.Pending)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    RequireWaitingOperation(operation, token);
                }
                token.ThrowIfCancellationRequested();
                await operation.Completion.Task;
            }
            finally
            {
                operation.Armed = false;
                RemoveProceedListener(operation);
                if (ReferenceEquals(_loadingOperation, operation) && _continueButton != null)
                {
                    _continueButton.interactable = false;
                }
            }
        }
        /// <inheritdoc />
        public override UniTask ReleaseLoadingPresentationAsync(SceneLoadingContext context)
        {
            var operation = _loadingOperation;
            if (operation == null || operation.Id != context.OperationId)
            {
                return UniTask.CompletedTask;
            }
            try
            {
                ObserveLoadingLease();
                if (operation.Completed && CoverCount > operation.FirstCoverCount && _cover != null && _cover.blocksRaycasts)
                {
                    LoadingTwoCoverCount++;
                }
            }
            finally
            {
                CleanupLoading();
            }
            return UniTask.CompletedTask;
        }

        private LoadingOperation RequireLoadingOperation(SceneLoadingContext context)
        {
            if (_loadingOperation == null || _loadingOperation.Id != context.OperationId)
            {
                throw new InvalidOperationException("The loading presentation belongs to another operation.");
            }
            RequireLoadingUi();
            return _loadingOperation;
        }

        private void RequireWaitingOperation(LoadingOperation operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_loadingOperation, operation))
            {
                throw new OperationCanceledException("Loading presentation was released.", token);
            }
            RequireLoadingUi();
        }

        private void RequireLoadingUi()
        {
            if (_loadingPanel == null || !_loadingPanel.gameObject.activeInHierarchy || _continueButton == null ||
                _loadingStage == null || _loadingBar == null || _input == null || _input.IsDisposed || _module == null)
            {
                throw new InvalidOperationException("The project-owned loading UI or input scope is no longer available.");
            }
            RequireLoadingPermission();
        }

        private void RequireLoadingPermission()
        {
            var snapshot = _input.Layers.Snapshot;
            if (snapshot.AllInputBlocked || !snapshot.ActiveMapIds.Contains(_ui.id))
            {
                throw new InvalidOperationException("Loading UI cannot proceed while its input map is blocked.");
            }
        }

        private void CompleteProceed(LoadingOperation operation)
        {
            if (!ReferenceEquals(_loadingOperation, operation) || !operation.Armed || operation.Completed || operation.WaitToken.IsCancellationRequested ||
                _loadingPanel == null || !_loadingPanel.gameObject.activeInHierarchy || _continueButton == null)
            {
                return;
            }
            operation.Armed = false;
            operation.Completed = true;
            _continueButton.interactable = false;
            ProceedCount++;
            ObserveLoadingLease();
            operation.Completion.TrySetResult();
        }

        private bool LoadingButtonsReleased() => LoadingReleased(_module.submit) && LoadingReleased(_module.leftClick)
            && LoadingReleased(_module.rightClick) && LoadingReleased(_module.middleClick);

        private static bool LoadingReleased(InputActionReference reference)
        {
            if (reference == null)
            {
                return true;
            }
            foreach (var control in reference.action.controls)
            {
                if (control is ButtonControl button && button.isPressed)
                {
                    return false;
                }
            }
            return true;
        }

        private void ObserveLoadingLease()
        {
            var snapshot = _input.Layers.Snapshot;
            _loadingLeaseObserved &= _transitionLease != null && TransitionBlocked && !PlayerInputEnabled &&
                !snapshot.AllInputBlocked && snapshot.ActiveMapIds.Contains(_ui.id);
        }

        private void SetLoadingRatio(float ratio)
        {
            // A sprite-free Image uses its rectangle; changing anchors avoids a generated sprite/texture owner.
            _loadingBar.rectTransform.anchorMax = new Vector2(ratio, 1);
        }

        private void RemoveProceedListener(LoadingOperation operation)
        {
            if (_continueButton != null && operation.Listener != null)
            {
                _continueButton.onClick.RemoveListener(operation.Listener);
            }
            operation.Listener = null;
        }

        private void CleanupLoading()
        {
            var operation = _loadingOperation;
            _loadingOperation = null;
            if (operation != null)
            {
                operation.Armed = false;
                RemoveProceedListener(operation);
                operation.Completion.TrySetCanceled();
                LoadingReleaseCount++;
            }
            if (_continueButton != null)
            {
                _continueButton.interactable = false;
            }
            if (_loadingPanel != null)
            {
                SetVisible(_loadingPanel, false);
                _loadingPanel.gameObject.SetActive(false);
            }
        }

        private void CreateLoadingUi()
        {
            if (_loadingPanel != null)
            {
                if (_continueButton == null || _loadingStage == null || _loadingTip == null || _loadingBar == null)
                {
                    throw new InvalidOperationException("The previously created loading UI was destroyed.");
                }
                return;
            }
            var host = new GameObject("LoadingPresentation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            host.transform.SetParent(transform, false);
            var canvas = host.GetComponent<Canvas>();
            var coverCanvas = _cover.GetComponentInParent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = coverCanvas.sortingLayerID;
            canvas.targetDisplay = coverCanvas.targetDisplay;
            canvas.sortingOrder = coverCanvas.sortingOrder - 1;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            _loadingPanel = host.GetComponent<CanvasGroup>();
            var background = LoadingImage(host.transform, "RaycastBlocker", new Color(.04f, .065f, .1f, 1), Vector2.zero, Vector2.one);
            _loadingStage = LoadingLabel(background.transform, "Stage", "Preparing...", new Vector2(.2f, .6f), new Vector2(.8f, .75f));
            _loadingTip = LoadingLabel(background.transform, "Tip", "", new Vector2(.2f, .35f), new Vector2(.8f, .48f));
            var track = LoadingImage(background.transform, "ProgressTrack", new Color(.1f, .15f, .2f), new Vector2(.2f, .52f), new Vector2(.8f, .56f));
            _loadingBar = LoadingImage(track.transform, "StageProgress", new Color(.25f, .65f, .9f), Vector2.zero, Vector2.one);
            var buttonImage = LoadingImage(background.transform, "Continue", new Color(.18f, .32f, .46f), new Vector2(.35f, .2f), new Vector2(.65f, .3f));
            _continueButton = buttonImage.gameObject.AddComponent<Button>();
            _continueButton.targetGraphic = buttonImage;
            LoadingLabel(buttonImage.transform, "Label", "Continue", Vector2.zero, Vector2.one);
        }

        private static Image LoadingImage(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Image));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = host.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text LoadingLabel(Transform parent, string name, string value, Vector2 min, Vector2 max)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Text));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = host.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }
    }
}
