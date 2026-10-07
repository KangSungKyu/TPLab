using System;
using System.Collections.Generic;
using TPLab.Core.ResourceManagement;
using UnityEngine.SceneManagement;

namespace TPLab.Core.SceneManagement
{
    /// <summary>Immutable explicit instance request. Caller provides registered instances; paths never grant removal authority.</summary>
    public readonly struct SceneTransitionRequest
    {
        /// <summary>Owner operation requested.</summary>
        public SceneTransitionKind Kind { get; }
        /// <summary>Explicit load destination for first/replace/add; unused for direct removal.</summary>
        public SceneTarget Target { get; }
        /// <summary>Current primary, derived parent, or removal requester; first entry starts from common.</summary>
        public Scene SourceScene { get; }
        /// <summary>Exact derived instance to remove; load requests acquire their destination from the loader.</summary>
        public Scene DestinationScene { get; }
        /// <summary>First/primary load mode; derived commands require Additive.</summary>
        public LoadSceneMode Mode { get; }
        /// <summary>Explicit derived activation policy.</summary>
        public bool Activate { get; }
        /// <summary>Project metadata only.</summary>
        public int Priority { get; }
        private readonly string[] _requiredConditionIds;
        /// <summary>Required IDs do not bypass any other attached root condition.</summary>
        public IReadOnlyList<string> RequiredConditionIds => Array.AsReadOnly(_requiredConditionIds ?? Array.Empty<string>());

        /// <summary>Copies request data without starting work; the manager validates instances, settings and conditions before side effects.</summary>
        public SceneTransitionRequest(SceneTransitionKind kind, SceneTarget target = default, Scene sourceScene = default,
            Scene destinationScene = default, LoadSceneMode mode = LoadSceneMode.Additive, bool activate = false, int priority = 0,
            string[] requiredConditionIds = null)
        {
            Kind = kind;
            Target = target;
            SourceScene = sourceScene;
            DestinationScene = destinationScene;
            Mode = mode;
            Activate = activate;
            Priority = priority;
            _requiredConditionIds = requiredConditionIds == null ? Array.Empty<string>() : (string[])requiredConditionIds.Clone();
        }
    }
}
