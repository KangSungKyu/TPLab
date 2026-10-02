using System;
using UnityEngine;

namespace MyLab.Core.Tests
{
    public class PoolLifecycleProbe : MonoBehaviour
    {
        public static int EnableCount;
        public static Action OnEnabled;

        private void OnEnable()
        {
            EnableCount++;
            OnEnabled?.Invoke();
        }
    }
}
