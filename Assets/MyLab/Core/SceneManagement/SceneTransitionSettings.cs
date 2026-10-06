using System;
using System.Collections.Generic;
using System.Linq;
using MyLab.Core.ResourceManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLab.Core.SceneManagement
{
    /// <summary>Explicit project asset owning definitions; no global discovery, queues or automatic evaluation.</summary>
    [CreateAssetMenu(menuName = "MyLab/Scene Transition Settings")]
    public sealed class SceneTransitionSettings : ScriptableObject
    {
        [SerializeField] private SceneTransitionDefinition[] _definitions = Array.Empty<SceneTransitionDefinition>();

        /// <summary>Copies the list and rejects null definitions or empty/duplicate IDs; existing runtime snapshots remain independent.</summary>
        public void Configure(params SceneTransitionDefinition[] definitions)
        {
            var copied = definitions == null ? Array.Empty<SceneTransitionDefinition>() : (SceneTransitionDefinition[])definitions.Clone();
            ValidateDefinitionIds(copied);
            _definitions = copied;
        }

        /// <summary>Validates unique IDs, source/target/mode and required IDs, returning a detached read-only snapshot; malformed data throws.</summary>
        public IReadOnlyList<SceneTransitionDefinition> CreateSnapshot()
        {
            var definitions = _definitions ?? Array.Empty<SceneTransitionDefinition>();
            ValidateDefinitionIds(definitions);
            var copied = new List<SceneTransitionDefinition>();
            foreach (var definition in definitions)
            {
                ValidateKindAndMode(definition.Kind, definition.Mode);
                if (definition.Kind == SceneTransitionKind.FirstEntry && !string.IsNullOrEmpty(definition.SourceScenePath))
                    throw new ArgumentException("First entry uses the injected common root; leave its source path empty.");
                if (definition.Kind != SceneTransitionKind.FirstEntry)
                    SceneTarget.ValidateScenePath(definition.SourceScenePath);
                var target = definition.Target;
                target.Validate();
                ValidateRequiredIds(definition.RequiredConditionIds);
                copied.Add(new SceneTransitionDefinition(definition.Id, definition.Kind, definition.SourceScenePath, target,
                    definition.Mode, definition.Activate, definition.Priority, definition.RequiredConditionIds.ToArray()));
            }
            return copied.AsReadOnly();
        }

        private static void ValidateDefinitionIds(IEnumerable<SceneTransitionDefinition> definitions)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || !ids.Add(definition.Id))
                    throw new ArgumentException("Definitions require unique nonempty IDs and nonnull entries.");
        }

        internal static void ValidateRequiredIds(IReadOnlyList<string> requiredIds)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in requiredIds)
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    throw new ArgumentException("Required condition IDs must be nonempty and unique.");
        }

        internal static void ValidateKindAndMode(SceneTransitionKind kind, LoadSceneMode mode)
        {
            if ((int)kind < (int)SceneTransitionKind.FirstEntry || (int)kind > (int)SceneTransitionKind.RemoveDerived)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (mode != LoadSceneMode.Single && mode != LoadSceneMode.Additive ||
                (kind == SceneTransitionKind.AddDerived || kind == SceneTransitionKind.RemoveDerived) && mode != LoadSceneMode.Additive)
                throw new ArgumentException("Select a valid load mode; derived commands require Additive.", nameof(mode));
        }
    }
}
