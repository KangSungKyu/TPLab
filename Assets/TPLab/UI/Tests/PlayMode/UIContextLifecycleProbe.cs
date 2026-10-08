using UnityEngine;

namespace TPLab.UI.Tests
{
    /// <summary>Native activation evidence from a cloned test view, independent of UIContext internals.</summary>
    public sealed class UIContextLifecycleProbe : MonoBehaviour
    {
        public bool Prepared;
        public int EnableCount { get; private set; }
        public int DisableCount { get; private set; }
        public bool FirstEnableWasPrepared { get; private set; }

        private void OnEnable()
        {
            if (EnableCount == 0)
            {
                FirstEnableWasPrepared = Prepared;
            }
            ++EnableCount;
        }

        private void OnDisable() => ++DisableCount;
    }
}