using System;
using System.Reflection;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public sealed class SceneTransitionSamplePathTests
    {
        private static Type Paths => Type.GetType("TPLab.Samples.SceneTransitions.SceneTransitionSamplePaths, TPLab.SceneTransitionSamples", true);
        private static string Invoke(string method, string path)
        {
            var member = Paths.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.That(member, Is.Not.Null, "Missing portable sample path contract: " + method);
            try { return (string)member.Invoke(null, new object[] { path }); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }

        [TestCase("Assets/TPLab/Samples/Input/SceneTransitions")]
        [TestCase("Assets/Samples/com.tplab.input/0.0.1/Scene Transitions")]
        public void BootstrapAndSourceScriptResolveTheirActualOwningRoot(string root)
        {
            Assert.That(Invoke("RootFromBootstrap", root + "/Scenes/BootstrapSingle.unity"), Is.EqualTo(root));
            Assert.That(Invoke("RootFromBootstrap", root + "/Scenes/BootstrapAdditive.unity"), Is.EqualTo(root));
            Assert.That(Invoke("RootFromScript", root + "/Editor/SceneTransitionSampleBuilder.cs"), Is.EqualTo(root));
        }

        [TestCase("")]
        [TestCase("Packages/com.tplab.input/Samples~/SceneTransitions")]
        [TestCase("Assets/Samples/../foreign")]
        [TestCase("Assets/Samples/./foreign")]
        [TestCase("Assets\\Samples\\foreign")]
        [TestCase("/Assets/Samples/foreign")]
        [TestCase("Assets/Samples//foreign")]
        public void RootRejectsNonAssetsOrAmbiguousPaths(string root)
        {
            Assert.Throws<ArgumentException>(() => Invoke("ValidateRoot", root));
        }

        [Test]
        public void NewSessionRootReplacesStaleRootAndInvalidRootLeavesItUntouched()
        {
            var root = Paths.GetProperty("Root", BindingFlags.Public | BindingFlags.Static);
            string before = (string)root.GetValue(null);
            try
            {
                Invoke("ConfigureRoot", "Assets/Samples/com.tplab.input/0.0.1/First import");
                Invoke("ConfigureRoot", Invoke("RootFromBootstrap", "Assets/Samples/com.tplab.input/0.0.2/Second import/Scenes/BootstrapSingle.unity"));
                string expected = "Assets/Samples/com.tplab.input/0.0.2/Second import";
                Assert.That(root.GetValue(null), Is.EqualTo(expected));
                Assert.That(Paths.GetProperty("Main").GetValue(null), Is.EqualTo(expected + "/Scenes/Main.unity"));
                Assert.Throws<ArgumentException>(() => Invoke("ConfigureRoot", "Packages/com.tplab.input"));
                Assert.That(root.GetValue(null), Is.EqualTo(expected));
            }
            finally { Invoke("ConfigureRoot", before); }
        }
        [TestCase("Assets/Foreign/Scenes/Main.unity")]
        [TestCase("Assets/Samples/Scenes/BootstrapSingle.unity/extra")]
        public void BootstrapRejectsOtherSceneNamesAndExtraSuffix(string path)
        {
            Assert.Throws<ArgumentException>(() => Invoke("RootFromBootstrap", path));
        }
    }
}
