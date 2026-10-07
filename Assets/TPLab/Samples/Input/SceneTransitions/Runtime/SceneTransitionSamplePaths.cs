using System;

namespace TPLab.Samples.SceneTransitions
{
    /// <summary>Owned sample paths. Editor authoring resolves its script; Players resolve their saved Bootstrap scene.</summary>
    public static class SceneTransitionSamplePaths
    {
        public const string DevelopmentRoot = "Assets/TPLab/Samples/Input/SceneTransitions";
        public static string Root { get; private set; } = DevelopmentRoot;
        public static string Hub => Root + "/Scenes/Hub.unity";
        public static string Main => Root + "/Scenes/Main.unity";
        public static string Area => Root + "/Scenes/Area.unity";
        public static string Nested => Root + "/Scenes/Nested.unity";
        public static string BootstrapAdditive => Root + "/Scenes/BootstrapAdditive.unity";
        public static string BootstrapSingle => Root + "/Scenes/BootstrapSingle.unity";
        public static string Bootstrap(bool single) => single ? BootstrapSingle : BootstrapAdditive;
        public static string[] BuildScenes(bool single) => new[] { Bootstrap(single), Hub, Main, Area, Nested };

        /// <summary>Accepts only an unambiguous project-owned Assets path, excluding package read-only storage.</summary>
        public static string ValidateRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !root.StartsWith("Assets/", StringComparison.Ordinal) ||
                root.IndexOfAny(new[] { '\\', ':', '*', '?', '"', '<', '>', '|', '\0' }) >= 0)
                throw new ArgumentException("Import the sample into a project-owned Assets folder before authoring it.", nameof(root));
            foreach (string segment in root.Split('/'))
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == ".." || segment != segment.Trim())
                    throw new ArgumentException("Sample root contains an ambiguous path segment.", nameof(root));
            return root;
        }

        /// <summary>Sets the validated root before sample paths are used. Does not touch files or Editor settings.</summary>
        public static void ConfigureRoot(string root) => Root = ValidateRoot(root);

        /// <summary>Derives the root from the actual builder MonoScript path found through AssetDatabase.</summary>
        public static string RootFromScript(string scriptPath) => RootFromSuffix(scriptPath, "/Editor/SceneTransitionSampleBuilder.cs");

        /// <summary>Derives the root from the controller's saved owning scene before it can become persistent.</summary>
        public static string RootFromBootstrap(string scenePath)
        {
            const string single = "/Scenes/BootstrapSingle.unity";
            const string additive = "/Scenes/BootstrapAdditive.unity";
            return RootFromSuffix(scenePath, scenePath != null && scenePath.EndsWith(single, StringComparison.Ordinal) ? single : additive);
        }

        private static string RootFromSuffix(string path, string suffix)
        {
            if (path == null || !path.EndsWith(suffix, StringComparison.Ordinal))
                throw new ArgumentException("Unexpected sample asset path.", nameof(path));
            return ValidateRoot(path.Substring(0, path.Length - suffix.Length));
        }
    }
}
