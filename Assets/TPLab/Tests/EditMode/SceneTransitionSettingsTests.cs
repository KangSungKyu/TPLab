using System;
using System.Linq;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPLab.Core.Tests
{
    public sealed class SceneTransitionSettingsTests
    {
        private const string Hub = "Assets/TPLab/Tests/Fixtures/BootstrapHub.unity";
        private const string Main = "Assets/TPLab/Tests/Fixtures/ReplacementMain.unity";
        private SceneTransitionSettings _settings;
        [SetUp] public void SetUp() => _settings = ScriptableObject.CreateInstance<SceneTransitionSettings>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_settings);

        [Test]
        public void ValidSnapshotCopiesDefinitionAndRequiredIds()
        {
            var ids = new[] { "permission" };
            var definition = Definition("entry", required: ids);
            _settings.Configure(definition);
            var snapshot = _settings.CreateSnapshot();
            ids[0] = "mutated";
            JsonUtility.FromJsonOverwrite("{\"_id\":\"changed\",\"_requiredConditionIds\":[\"other\"]}", definition);
            Assert.That(snapshot.Single().Id, Is.EqualTo("entry"));
            Assert.That(snapshot.Single().RequiredConditionIds, Is.EqualTo(new[] { "permission" }));
            Assert.That(snapshot.Single().Target, Is.EqualTo(SceneTarget.BuildScene(Hub)));
        }

        [Test]
        public void ConfigureDefensivelyCopiesCallerList()
        {
            var definitions = new[] { Definition("entry") };
            _settings.Configure(definitions);
            definitions[0] = Definition("replacement");
            Assert.That(_settings.CreateSnapshot().Single().Id, Is.EqualTo("entry"));
        }

        [Test]
        public void ConfigureRejectsDuplicateDefinitionIds()
            => Assert.Throws<ArgumentException>(() => _settings.Configure(Definition("same"), Definition("same")));

        [TestCase("")]
        [TestCase(" ")]
        public void ConfigureRejectsEmptyDefinitionId(string id)
            => Assert.Throws<ArgumentException>(() => _settings.Configure(Definition(id)));

        [Test]
        public void ConfigureRejectsNullDefinition()
            => Assert.Throws<ArgumentException>(() => _settings.Configure(new SceneTransitionDefinition[] { null }));

        [Test]
        public void SnapshotRejectsUnknownKind()
        {
            _settings.Configure(new SceneTransitionDefinition("entry", (SceneTransitionKind)99, "", SceneTarget.BuildScene(Hub)));
            Assert.Catch<ArgumentException>(() => _settings.CreateSnapshot());
        }

        [Test]
        public void SnapshotRejectsMalformedSourcePath()
        {
            _settings.Configure(new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, "BootstrapHub", SceneTarget.BuildScene(Hub)));
            Assert.Throws<InvalidOperationException>(() => _settings.CreateSnapshot());
        }

        [Test]
        public void SnapshotRejectsFirstEntrySourcePath()
        {
            _settings.Configure(new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, Hub, SceneTarget.BuildScene(Hub)));
            Assert.Throws<ArgumentException>(() => _settings.CreateSnapshot());
        }

        [Test]
        public void SnapshotRejectsSingleDerivedDefinition()
        {
            _settings.Configure(new SceneTransitionDefinition("add", SceneTransitionKind.AddDerived, Hub,
                SceneTarget.BuildScene("Assets/TPLab/Tests/Fixtures/DerivedArea.unity"), LoadSceneMode.Single));
            Assert.Throws<ArgumentException>(() => _settings.CreateSnapshot());
        }

        [TestCase("", "ok")]
        [TestCase("permission", "permission")]
        public void SnapshotRejectsEmptyOrDuplicateRequiredId(string first, string second)
        {
            _settings.Configure(Definition("entry", new[] { first, second }));
            Assert.Throws<ArgumentException>(() => _settings.CreateSnapshot());
        }

        [Test]
        public void BuildDefinitionJsonRoundTripAcceptsEmptySerializedAddressableKey()
        {
            var definition = Definition("entry");
            JsonUtility.FromJsonOverwrite("{\"_addressableKey\":\"\"}", definition);
            var restored = JsonUtility.FromJson<SceneTransitionDefinition>(JsonUtility.ToJson(definition));
            Assert.That(restored.Target, Is.EqualTo(SceneTarget.BuildScene(Hub)));
            Assert.That(restored.Target.AddressableKey, Is.Null);
        }

        [Test]
        public void BootstrapDefinitionGettersExposeSelectedEntryAndPreserveLegacySerialization()
        {
            _settings.Configure(new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, "",
                SceneTarget.BuildScene(Hub), LoadSceneMode.Single));
            var host = new GameObject("BootstrapDefinitionGetterTest");
            try
            {
                var bootstrap = host.AddComponent<BootstrapSystem>();
                bootstrap.Configure(null, Main, false);
                bootstrap.Configure(null, _settings, "entry", false);
                Assert.That(bootstrap.FirstScenePath, Is.EqualTo(Hub));
                Assert.That(bootstrap.LoadMode, Is.EqualTo(LoadSceneMode.Single));
                Assert.That(bootstrap.Source, Is.EqualTo(SceneSource.BuildScene));
                Assert.That(bootstrap.AddressableKey, Is.Null);
                Assert.That(bootstrap.SceneReference, Is.Null);
                Assert.That(bootstrap.FirstSceneTarget, Is.EqualTo(SceneTarget.BuildScene(Hub)));
                Assert.That(JsonUtility.ToJson(bootstrap), Does.Contain("\"_firstScenePath\":\"" + Main + "\""));
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void RequestAndContextListsAreDetachedAndReadOnly()
        {
            var required = new[] { "permission" };
            var request = new SceneTransitionRequest(SceneTransitionKind.FirstEntry, SceneTarget.BuildScene(Hub), requiredConditionIds: required);
            required[0] = "changed";
            Assert.That(request.RequiredConditionIds.Single(), Is.EqualTo("permission"));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<string>)request.RequiredConditionIds)[0] = "changed");
            var affected = new System.Collections.Generic.List<Scene> { default };
            var context = new SceneTransitionContext(request, default, default, affected);
            affected.Clear();
            Assert.That(context.AffectedScenes.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<Scene>)context.AffectedScenes).Clear());
        }

        private static SceneTransitionDefinition Definition(string id, string[] required = null)
            => new SceneTransitionDefinition(id, SceneTransitionKind.FirstEntry, "", SceneTarget.BuildScene(Hub), requiredConditionIds: required);
    }
}
