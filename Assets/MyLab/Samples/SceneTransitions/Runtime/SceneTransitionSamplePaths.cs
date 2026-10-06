namespace MyLab.Samples.SceneTransitions
{
    /// <summary>Paths owned exclusively by the explicit sample asset builder.</summary>
    public static class SceneTransitionSamplePaths
    {
        public const string Root = "Assets/MyLab/Samples/SceneTransitions";
        public const string Hub = Root + "/Scenes/Hub.unity";
        public const string Main = Root + "/Scenes/Main.unity";
        public const string Area = Root + "/Scenes/Area.unity";
        public const string Nested = Root + "/Scenes/Nested.unity";
        public const string BootstrapAdditive = Root + "/Scenes/BootstrapAdditive.unity";
        public const string BootstrapSingle = Root + "/Scenes/BootstrapSingle.unity";
        public static string Bootstrap(bool single) => single ? BootstrapSingle : BootstrapAdditive;
        public static string[] BuildScenes(bool single) => new[] { Bootstrap(single), Hub, Main, Area, Nested };
    }
}
