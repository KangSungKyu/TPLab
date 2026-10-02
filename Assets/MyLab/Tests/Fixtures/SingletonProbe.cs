using System;
using MyLab.Core.Lifecycle;
using UnityEngine;

namespace MyLab.Core.Tests
{
    public sealed class SingletonProbe : MonoSingleton<SingletonProbe>
    {
        public bool Persistent;
        public bool FailInitialize;
        public bool FailShutdown;
        public bool SpawnDuplicate;
        public bool DestroyDuringInitialize;
        [NonSerialized] public int InitializeCount;
        [NonSerialized] public int ShutdownCount;
        [NonSerialized] public SingletonProbe InstanceDuringInitialize;
        public static int TotalInitializeCount;
        public static int TotalShutdownCount;

        protected override bool PersistAcrossScenes => Persistent;

        protected override void OnSingletonInitialize()
        {
            InitializeCount++;
            TotalInitializeCount++;
            InstanceDuringInitialize = Instance;
            if (SpawnDuplicate)
            {
                new GameObject("SingletonTestReentrantDuplicate").AddComponent<SingletonProbe>();
            }
            if (FailInitialize)
            {
                throw new InvalidOperationException("singleton-init");
            }
            if (DestroyDuringInitialize)
            {
                UnityEngine.Object.DestroyImmediate(this);
            }
        }

        protected override void OnSingletonShutdown()
        {
            ShutdownCount++;
            TotalShutdownCount++;
            if (FailShutdown)
            {
                throw new InvalidOperationException("singleton-shutdown");
            }
        }
    }
}
