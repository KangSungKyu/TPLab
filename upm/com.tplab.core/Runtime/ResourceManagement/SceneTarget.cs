using System;
using UnityEngine.AddressableAssets;

namespace TPLab.Core.ResourceManagement
{
    /// <summary>Explicit scene backend; targets never fall back to another backend.</summary>
    public enum SceneSource
    {
        BuildScene,
        Addressable
    }

    /// <summary>Identifies one scene asset and its explicit loading backend, without owning a loaded instance.</summary>
    public readonly struct SceneTarget
    {
        /// <summary>Backend selected by the caller.</summary>
        public SceneSource Source { get; }
        /// <summary>Normalized full Assets scene path, also used to detect unsupported duplicate instances.</summary>
        public string ScenePath { get; }
        /// <summary>Unique Addressables address or reference runtime key; null for a build scene.</summary>
        public string AddressableKey { get; }

        /// <summary>Creates an explicit target. Rejects invalid source, path, and source/key combinations.</summary>
        public SceneTarget(SceneSource source, string scenePath, string addressableKey = null)
        {
            Source = source;
            ScenePath = scenePath;
            AddressableKey = addressableKey;
            Validate();
        }

        /// <summary>Creates a scene target resolved only through the Player build scene list.</summary>
        public static SceneTarget BuildScene(string scenePath) => new SceneTarget(SceneSource.BuildScene, scenePath);

        /// <summary>Creates an Addressables target using a unique address and its explicit scene asset path.</summary>
        public static SceneTarget Addressable(string key, string scenePath) => new SceneTarget(SceneSource.Addressable, scenePath, key);

        /// <summary>Creates an Addressables target from a valid reference runtime key and its explicit scene asset path.</summary>
        public static SceneTarget Addressable(AssetReference reference, string scenePath)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (!reference.RuntimeKeyIsValid()) throw new ArgumentException("A valid scene reference is required.", nameof(reference));
            return Addressable(reference.RuntimeKey.ToString(), scenePath);
        }

        /// <summary>Rejects malformed or default targets without starting a native operation.</summary>
        public void Validate()
        {
            if (Source != SceneSource.BuildScene && Source != SceneSource.Addressable)
                throw new ArgumentOutOfRangeException(nameof(Source));
            ValidateScenePath(ScenePath);
            if (Source == SceneSource.BuildScene && AddressableKey != null)
                throw new ArgumentException("A Build Scene target cannot contain an Addressables key.", nameof(AddressableKey));
            if (Source == SceneSource.Addressable && string.IsNullOrWhiteSpace(AddressableKey))
                throw new ArgumentException("An explicit Addressables scene key is required.", nameof(AddressableKey));
        }

        /// <summary>Rejects missing, non-Assets, non-scene, or non-normalized paths without checking asset existence.</summary>
        public static void ValidateScenePath(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !scenePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !scenePath.EndsWith(".unity", StringComparison.Ordinal) || scenePath.Contains("//") ||
                scenePath.IndexOfAny(new[] { '\\', ':', '\r', '\n', '\t' }) >= 0)
                throw new InvalidOperationException("Select a full Assets/.../*.unity scene path.");
            foreach (var part in scenePath.Split('/'))
            {
                if (part == "." || part == "..")
                    throw new InvalidOperationException("Relative path segments are not allowed.");
            }
        }
    }
}
