using System;
using System.Collections.Generic;
using MyLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Explicit owner command; derived loads are Additive and removals cover one subtree.</summary>
    public enum SceneTransitionKind { FirstEntry, ReplacePrimary, AddDerived, RemoveDerived }

    /// <summary>Serializable project definition. The settings owner creates a detached runtime snapshot before execution.</summary>
    [Serializable]
    public sealed class SceneTransitionDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private SceneTransitionKind _kind;
        [SerializeField] private string _sourceScenePath;
        [SerializeField] private SceneSource _sceneSource;
        [SerializeField] private string _scenePath;
        [SerializeField] private string _addressableKey;
        [SerializeField] private LoadSceneMode _mode;
        [SerializeField] private bool _activate;
        [SerializeField] private int _priority;
        [SerializeField] private string[] _requiredConditionIds = Array.Empty<string>();

        /// <summary>Unique ID within one settings asset.</summary>
        public string Id => _id;
        /// <summary>Requested lifecycle operation.</summary>
        public SceneTransitionKind Kind => _kind;
        /// <summary>Asset path used to resolve the current primary, parent or requester; first entry starts from common.</summary>
        public string SourceScenePath => _sourceScenePath;
        /// <summary>Explicit destination backend/path/key; removal resolves the existing destination instance by path.</summary>
        public SceneTarget Target
        {
            get
            {
                if (_sceneSource == SceneSource.BuildScene && string.IsNullOrEmpty(_addressableKey))
                    return SceneTarget.BuildScene(_scenePath);
                return new SceneTarget(_sceneSource, _scenePath, _addressableKey);
            }
        }
        /// <summary>First/primary load mode; derived operations require Additive.</summary>
        public LoadSceneMode Mode => _mode;
        /// <summary>Whether a newly added derived instance becomes active.</summary>
        public bool Activate => _activate;
        /// <summary>Project metadata without implicit focus or ordering.</summary>
        public int Priority => _priority;
        /// <summary>Required policy IDs, each supplied by at least one affected root.</summary>
        public IReadOnlyList<string> RequiredConditionIds => Array.AsReadOnly(_requiredConditionIds ?? Array.Empty<string>());

        /// <summary>Copies definition data; configuration validation is performed when settings create their runtime snapshot.</summary>
        public SceneTransitionDefinition(string id, SceneTransitionKind kind, string sourceScenePath, SceneTarget target,
            LoadSceneMode mode = LoadSceneMode.Additive, bool activate = false, int priority = 0, string[] requiredConditionIds = null)
        {
            _id = id;
            _kind = kind;
            _sourceScenePath = sourceScenePath;
            _sceneSource = target.Source;
            _scenePath = target.ScenePath;
            _addressableKey = target.AddressableKey;
            _mode = mode;
            _activate = activate;
            _priority = priority;
            _requiredConditionIds = requiredConditionIds == null ? Array.Empty<string>() : (string[])requiredConditionIds.Clone();
        }
    }
}
