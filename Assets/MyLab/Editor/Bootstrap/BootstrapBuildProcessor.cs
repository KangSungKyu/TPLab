using UnityEditor.Build;

namespace MyLab.Core.Editor.Bootstrap
{
    /// <summary>Validates the actual build plan before Player content is generated, including command-line builds.</summary>
    public sealed class BootstrapBuildProcessor : BuildPlayerProcessor
    {
        public override int callbackOrder => -1000;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var errors = BootstrapValidator.ValidateBuildScenes(buildPlayerContext.BuildPlayerOptions.scenes);
            if (errors.Count != 0)
                throw new BuildFailedException("Bootstrap validation failed:\n" + string.Join("\n", errors));
        }
    }
}
