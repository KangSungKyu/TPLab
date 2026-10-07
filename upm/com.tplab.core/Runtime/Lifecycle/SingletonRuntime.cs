using System;
using UnityEngine;

namespace TPLab.Core.Lifecycle
{
    // Unity invokes the non-generic entry points even with Domain Reload disabled.
    internal static class SingletonRuntime
    {
        private static Action _reset;
        private static Action _restore;

        internal static bool IsQuitting { get; private set; }

        internal static void Register(Action reset, Action restore)
        {
            _reset += reset;
            _restore += restore;
        }

        internal static void MarkQuitting() => IsQuitting = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
            _reset?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Restore() => _restore?.Invoke();
    }
}
