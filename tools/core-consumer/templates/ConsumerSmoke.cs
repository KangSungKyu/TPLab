using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using MyLab.Core.Lifecycle;
using MyLab.Core.Pooling;
using MyLab.Core.ResourceManagement;
using MyLab.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if MYLAB_INPUT_CONSUMER
using MyLab.Core.Input;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace MyLabConsumer
{
    public sealed class ConsumerSmoke : MonoBehaviour
    {
        private const string DerivedScenePath = "Assets/MyLabConsumer/Scenes/Derived.unity";
        private BootstrapSystem _bootstrap;
        private SceneOwnedRoot _root;

        public void ConfigureForBuild(string path)
        {
            _derivedScenePath = path;
        }

        [SerializeField] private string _derivedScenePath = DerivedScenePath;

        private void Start()
        {
            _bootstrap = GetComponent<BootstrapSystem>();
            _root = GetComponent<SceneOwnedRoot>();
            RunAsync().Forget(Debug.LogException);
        }

        private async UniTask RunAsync()
        {
            var report = new SmokeReport();
            var pool = new ObjectPool<PoolItem>(() => new PoolItem(), 1);
            var resources = new ResourceManager();
            var tables = new DataTableManager();
            Exception failure = null;
            try
            {
                report.commonRootReady = _root != null && _root.IsReady;
#if MYLAB_INPUT_CONSUMER
                report.inputScopeVerified = await VerifyInputScopeAsync();
#endif
                var codec = new DecimalIdxCodec(1000);
                tables.RegisterIdxRouter(codec);
                tables.RegisterTable<SmokeRow, SmokeTable>(1, "smoke",
                    token => UniTask.FromResult("idx,Name\n1001,consumer\n"), () => new SmokeTable());
                await tables.LoadAsync();
                var row = tables.Get<SmokeRow>(1001);
                report.csvTypedLookup = row != null && row.Id == 1001 && row.Name == "consumer";

                pool.TryRent(out var first);
                pool.Return(first);
                pool.TryRent(out var second);
                report.poolReused = ReferenceEquals(first, second) && pool.CountOwned == 1;
                pool.Return(second);
                pool.Dispose();

                await resources.ShutdownAsync();
                report.emptyResourceManagerShutdown = resources.IsDisposed;

                await _bootstrap.BootstrapAsync();
                Scene game = _bootstrap.GameScene;
                report.gameSceneLoaded = game.IsValid() && game.isLoaded;
                report.commonRootPrepared = _root.IsPrepared;
                report.activeSceneOwned = SceneManager.GetActiveScene() == game;
                report.canProceedAfterEntry = _bootstrap.Manager.CanProceed;

                Scene derived = await _bootstrap.Manager.AddDerivedAsync(_derivedScenePath, game);
                report.derivedAdded = derived.IsValid() && derived.isLoaded && _bootstrap.Manager.CanProceed;
                await _bootstrap.Manager.RemoveDerivedAsync(derived, game);
                report.derivedRemoved = !derived.isLoaded && _bootstrap.Manager.CanProceed;

                await _bootstrap.ShutdownAsync();
                await _root.ShutdownAsync();
                report.gracefulShutdown = _bootstrap.Manager != null && _bootstrap.Manager.OwnedScenes.Count == 0 && !_root.IsReady;
            }
            catch (Exception exception)
            {
                failure = exception;
                Debug.LogException(exception);
            }
            finally
            {
                try
                {
                    if (_bootstrap != null) await _bootstrap.ShutdownAsync();
                }
                catch (Exception exception) { failure = failure ?? exception; Debug.LogException(exception); }
                try
                {
                    if (_root != null) await _root.ShutdownAsync();
                }
                catch (Exception exception) { failure = failure ?? exception; Debug.LogException(exception); }
                try { tables.Dispose(); }
                catch (Exception exception) { failure = failure ?? exception; Debug.LogException(exception); }
                try { pool.Dispose(); }
                catch (Exception exception) { failure = failure ?? exception; Debug.LogException(exception); }
                report.success = failure == null && report.commonRootReady && report.gameSceneLoaded &&
                    report.commonRootPrepared && report.activeSceneOwned && report.canProceedAfterEntry &&
                    report.derivedAdded && report.derivedRemoved && report.poolReused && report.csvTypedLookup &&
                    report.emptyResourceManagerShutdown && report.gracefulShutdown;
#if MYLAB_INPUT_CONSUMER
                report.success &= report.inputScopeVerified;
#endif
                report.error = failure == null ? "" : failure.ToString();
                WriteResult(report);
                Application.Quit(report.success ? 0 : 1);
            }
        }

#if MYLAB_INPUT_CONSUMER
        private static async UniTask<bool> VerifyInputScopeAsync()
        {
            var source = ScriptableObject.CreateInstance<InputActionAsset>();
            Keyboard keyboard = null;
            InputManager input = null;
            IDisposable gameplay = null;
            IDisposable modal = null;
            try
            {
                var gameMap = source.AddActionMap("Game");
                var fire = gameMap.AddAction("Fire", InputActionType.Button, "<Keyboard>/space");
                keyboard = InputSystem.AddDevice<Keyboard>();
                input = new InputManager(source);
                input.Actions.devices = new InputDevice[] { keyboard };
                input.Layers.RegisterLayer("game", new[] { gameMap.id }, 0, InputLayerMode.Overlay);
                input.Layers.RegisterLayer("modal", Array.Empty<Guid>(), 100, InputLayerMode.BlockLower);
                gameplay = input.Layers.AcquireLayer("game");
                if (!input.GetAction(fire.id).enabled || source.FindAction(fire.id).enabled)
                {
                    return false;
                }

                bool nativeCandidateSelected = false;
                var pending = input.Rebinding.RebindAsync(new RebindRequest(fire.id, fire.bindings[0].id)
                {
                    ControlPath = "<Keyboard>",
                    TimeoutSeconds = 5,
                    Validator = candidate =>
                    {
                        nativeCandidateSelected = candidate.Control != null && candidate.Control.device == keyboard &&
                            candidate.Path == "<Keyboard>/k";
                        return nativeCandidateSelected;
                    }
                }).Preserve();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                await UniTask.WaitUntil(() => nativeCandidateSelected || pending.Status != UniTaskStatus.Pending)
                    .Timeout(TimeSpan.FromSeconds(2), DelayType.Realtime);
                bool waitsForRelease = nativeCandidateSelected && pending.Status == UniTaskStatus.Pending &&
                    input.GetAction(fire.id).bindings[0].overridePath == null;
                modal = input.Layers.AcquireLayer("modal");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                RebindResult result = await pending;
                bool applied = result.Status == RebindStatus.Applied && result.Path == "<Keyboard>/k" &&
                    input.GetAction(fire.id).bindings[0].effectivePath == "<Keyboard>/k" &&
                    !input.GetAction(fire.id).enabled && input.Layers.Snapshot.ActiveMapIds.Count == 0 &&
                    input.Layers.Snapshot.ActiveLayerIds.Count == 1 &&
                    input.Layers.Snapshot.ActiveLayerIds[0] == "modal" &&
                    fire.bindings[0].overridePath == null;
                modal.Dispose();
                modal = null;
                bool layerRestored = input.GetAction(fire.id).enabled &&
                    input.Layers.Snapshot.ActiveLayerIds.Count == 1 &&
                    input.Layers.Snapshot.ActiveLayerIds[0] == "game";

                string overrides = input.Rebinding.ExportOverridesJson();
                input.Rebinding.ResetBinding(fire.id, fire.bindings[0].id);
                bool resetBinding = input.GetAction(fire.id).bindings[0].effectivePath == "<Keyboard>/space";
                input.Rebinding.ImportOverridesJson(overrides);
                bool imported = input.GetAction(fire.id).bindings[0].effectivePath == "<Keyboard>/k";
                input.Rebinding.ResetAll();
                bool resetAll = input.Rebinding.ExportOverridesJson() == "" &&
                    input.GetAction(fire.id).bindings[0].effectivePath == "<Keyboard>/space" &&
                    fire.bindings[0].overridePath == null;
                gameplay.Dispose();
                gameplay = null;
                var clone = input.Actions;
                await input.ShutdownAsync();
                await UniTask.NextFrame();
                bool shutdown = clone == null;
                return waitsForRelease && applied && layerRestored && resetBinding && imported && resetAll && shutdown;
            }
            finally
            {
                try
                {
                    modal?.Dispose();
                    gameplay?.Dispose();
                    if (input != null) await input.ShutdownAsync();
                }
                finally
                {
                    try
                    {
                        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                    }
                    finally
                    {
                        if (source != null) Destroy(source);
                    }
                }
            }
        }
#endif

        private static void WriteResult(SmokeReport report)
        {
            string path = Environment.GetEnvironmentVariable("MYLAB_CONSUMER_PLAYER_RESULT");
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError("MYLAB_CONSUMER_PLAYER_RESULT is required.");
                Application.Quit(2);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        [Serializable]
        private sealed class SmokeReport
        {
            public bool success;
            public bool commonRootReady;
            public bool gameSceneLoaded;
            public bool commonRootPrepared;
            public bool activeSceneOwned;
            public bool canProceedAfterEntry;
            public bool derivedAdded;
            public bool derivedRemoved;
            public bool poolReused;
            public bool csvTypedLookup;
            public bool emptyResourceManagerShutdown;
            public bool gracefulShutdown;
#if MYLAB_INPUT_CONSUMER
            public bool inputScopeVerified;
#endif
            public string error;
        }

        private sealed class PoolItem { }

        public sealed class SmokeRow : DataRow
        {
            public string Name { get; set; }
        }

        public sealed class SmokeTable : CsvDataTable<SmokeRow> { }
    }
}
