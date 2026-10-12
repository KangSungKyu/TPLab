using System;
using UnityEngine;

namespace TPLab.Core.Tests
{
    public class PoolReturnLifecycleProbe : MonoBehaviour
    {
        public Action<GameObject> Disabled;
        public Action<GameObject> Destroyed;

        private void OnDisable()
        {
            Disabled?.Invoke(gameObject);
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke(gameObject);
        }
    }
}
