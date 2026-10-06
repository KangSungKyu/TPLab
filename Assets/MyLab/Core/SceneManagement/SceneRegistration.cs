using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Successful registration role; common systems belong to the external session owner.</summary>
    public enum SceneRegistrationRole
    {
        Primary,
        Derived
    }

    /// <summary>Passive snapshot of one registered instance. Reading it grants no load, release or reparent ownership.</summary>
    public readonly struct SceneRegistration
    {
        /// <summary>Actual registered Scene instance, not an asset path or key.</summary>
        public Scene Scene { get; }
        /// <summary>Registered parent instance; default for the primary's external session boundary.</summary>
        public Scene Parent { get; }
        /// <summary>Primary or derived role assigned by the manager.</summary>
        public SceneRegistrationRole Role { get; }
        /// <summary>Project metadata only; does not imply activation, sorting or release order.</summary>
        public int Priority { get; }
        /// <summary>Root readiness observed when this snapshot was created.</summary>
        public bool IsPrepared { get; }
        /// <summary>Whether graceful shutdown had started when this snapshot was created.</summary>
        public bool IsShuttingDown { get; }

        /// <summary>Copies registration values without acquiring ownership or validating current Unity lifetime.</summary>
        public SceneRegistration(Scene scene, Scene parent, SceneRegistrationRole role, int priority,
            bool isPrepared, bool isShuttingDown)
        {
            Scene = scene;
            Parent = parent;
            Role = role;
            Priority = priority;
            IsPrepared = isPrepared;
            IsShuttingDown = isShuttingDown;
        }
    }
}
