using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.Lifecycle;
using TPLab.Core.Pooling;
using TPLab.Core.ResourceManagement;
using UnityEngine;

namespace TPLab.Core.Tests
{
    public sealed class ResourceConsumerProbe : SceneRootInstaller
    {
        public ResourceManagerInstaller Source;
        public string Key;
        public ResourceManager Injected;
        public TextAsset Text;
        public PrefabPool Pool;
        public GameObject Clone;
        public bool LoadPrefab;

        public override void Install(ISceneRoot root) => Injected = Source.Resources;

        public override async UniTask PrepareAsync(ISceneRoot root, CancellationToken cancellationToken)
        {
            if (LoadPrefab)
            {
                var prefab = await Injected.LoadAssetAsync<GameObject>(Key, cancellationToken);
                Pool = new PrefabPool(prefab, 1, transform);
                Pool.TryRent(out Clone);
            }
            else
            {
                Text = await Injected.LoadAssetAsync<TextAsset>(Key, cancellationToken);
            }
        }

        public override async UniTask ReleaseAsync(ISceneRoot root)
        {
            Pool?.Dispose();
            // Unity destroys clones at frame end, before the earlier installer releases prefab handles.
            await UniTask.NextFrame();
        }

        public override void Uninstall(ISceneRoot root)
        {
            Pool?.Dispose();
            Pool = null;
            Text = null;
            Injected = null;
        }
    }
}
