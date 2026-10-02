using System.Threading;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using MyLab.Core.Lifecycle;
using MyLab.Core.ResourceManagement;
using UnityEngine;

namespace MyLab.Core.Tests
{
    public sealed class DataTableConsumerProbe : SceneRootInstaller
    {
        public ResourceManagerInstaller Source;
        public string Key;
        public DataTableManager Tables;

        public override void Install(ISceneRoot root)
        {
            Tables = new DataTableManager();
            Tables.Register("rows", new[] { "Id", "Name" }, async token =>
                (await Source.Resources.LoadAssetAsync<TextAsset>(Key, token)).text,
                csv => (Id: csv.GetField<int>("Id"), Name: csv.GetField("Name")), row => row.Id);
        }

        public override async UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            await Tables.LoadAsync(cancellationToken);
        }

        public override UniTask ReleaseAsync(ISceneRoot root)
        {
            Tables?.Dispose();
            return UniTask.CompletedTask;
        }

        public override void Uninstall(ISceneRoot root)
        {
            Tables?.Dispose();
            Tables = null;
        }
    }
}
