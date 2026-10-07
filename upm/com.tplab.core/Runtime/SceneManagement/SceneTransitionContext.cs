using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace TPLab.Core.SceneManagement
{
    /// <summary>Read-only condition input; reading this context grants no ownership or transition command authority.</summary>
    public readonly struct SceneTransitionContext
    {
        /// <summary>Accepted instance request being evaluated.</summary>
        public SceneTransitionRequest Request { get; }
        /// <summary>Actual common/current primary/parent/requester source.</summary>
        public Scene SourceScene { get; }
        /// <summary>Actual removal or acquired load destination, default before a new scene exists.</summary>
        public Scene DestinationScene { get; }
        /// <summary>Distinct affected instance snapshot.</summary>
        public IReadOnlyList<Scene> AffectedScenes { get; }

        /// <summary>Copies actual condition context without evaluating or changing project state.</summary>
        public SceneTransitionContext(SceneTransitionRequest request, Scene sourceScene, Scene destinationScene,
            IEnumerable<Scene> affectedScenes)
        {
            Request = request;
            SourceScene = sourceScene;
            DestinationScene = destinationScene;
            AffectedScenes = System.Array.AsReadOnly(affectedScenes == null ? System.Array.Empty<Scene>() : affectedScenes.Distinct().ToArray());
        }
    }
}
