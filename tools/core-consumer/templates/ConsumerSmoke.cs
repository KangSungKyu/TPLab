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
                report.error = failure == null ? "" : failure.ToString();
                WriteResult(report);
                Application.Quit(report.success ? 0 : 1);
            }
        }

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
