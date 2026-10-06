using System;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Convenience command's preflight policy refusal. No operation, cover, load or failure notification has started.</summary>
    public sealed class SceneTransitionRejectedException : InvalidOperationException
    {
        /// <summary>Creates an observable refusal without converting configuration or execution failures into rejection.</summary>
        public SceneTransitionRejectedException(string message) : base(message) { }
    }
}
