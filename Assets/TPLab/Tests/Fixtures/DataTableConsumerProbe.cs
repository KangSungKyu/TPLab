using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.DataTables;
using TPLab.Examples.DataTables;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using UnityEngine;

namespace TPLab.Core.Tests
{
    public sealed class DataTableConsumerProbe : SceneRootInstaller
    {
        public ResourceManagerInstaller Source;
        public string Key;
        public bool UseStandardIdx;
        public DataTableManager Tables;

        public override void Install(ISceneRoot root)
        {
            Tables = new DataTableManager();
            if (UseStandardIdx)
            {
                Tables.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Tables.RegisterTable<TextRow, TextDataTable>(1, "rows", async token =>
                    (await Source.Resources.LoadAssetAsync<TextAsset>(Key, token)).text, () => new TextDataTable());
                return;
            }
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
