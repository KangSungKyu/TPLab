using System.Collections;
using TPLab.Core.Editor;
using TPLab.Core.Lifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.Core.Tests
{
    public class SceneRootEditorTests
    {
        private GameObject _root;
        private Object _previousSelection;
        private int _undoGroup;

        [SetUp]
        public void SetUp()
        {
            _previousSelection = Selection.activeObject;
            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            _root = new GameObject("SceneRootEditorTest");
            Undo.RegisterCreatedObjectUndo(_root, "Create Test Root");
            Selection.activeGameObject = _root;
            Undo.IncrementCurrentGroup();
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = _previousSelection;
            Undo.RevertAllDownToGroup(_undoGroup);
        }

        [UnityTest]
        public IEnumerator SceneOwnedMenuSupportsUndoRedo() => CheckMenu(SceneRootMode.SceneOwned, "Scene Owned");

        [UnityTest]
        public IEnumerator SingletonMenuSupportsUndoRedo() => CheckMenu(SceneRootMode.Singleton, "Singleton");

        private IEnumerator CheckMenu(SceneRootMode mode, string menu)
        {
            var light = _root.AddComponent<Light>();
            var first = _root.AddComponent<SceneRootInstallerProbe>();
            var second = _root.AddComponent<SceneRootInstallerProbe>();
            Assert.That(SceneRootMenu.CanAttachSelected(), Is.True);
            Assert.That(EditorApplication.ExecuteMenuItem("GameObject/TPLab/Scene Root/" + menu), Is.True);
            var hostType = mode == SceneRootMode.SceneOwned ? typeof(SceneOwnedRoot) : typeof(SingletonSceneRoot);
            var host = _root.GetComponent(hostType);
            Assert.That(host, Is.Not.Null);
            Assert.That(((ISceneRoot)host).IsReady, Is.False);
            Assert.That(SceneRootMenu.CanAttachSelected(), Is.False);
            Assert.That(_root.GetComponent<Light>(), Is.SameAs(light));
            AssertInstallers(host, first, second);
            Undo.PerformUndo();
            Assert.That(_root.GetComponent(hostType), Is.Null);
            Undo.PerformRedo();
            Assert.That(_root.GetComponent(hostType), Is.Not.Null);
            AssertInstallers(_root.GetComponent(hostType), first, second);
            yield return null;
        }

        private static void AssertInstallers(Component host, Object first, Object second)
        {
            using (var serialized = new SerializedObject(host))
            {
                var installers = serialized.FindProperty("_installers");
                Assert.That(installers.arraySize, Is.EqualTo(2));
                Assert.That(installers.GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(first));
                Assert.That(installers.GetArrayElementAtIndex(1).objectReferenceValue, Is.SameAs(second));
                Assert.That(serialized.FindProperty("_persistAcrossScenes").boolValue, Is.False);
            }
        }

        [Test]
        public void MenuRejectsNoSelectionAndChildSelection()
        {
            Selection.activeGameObject = null;
            Assert.That(SceneRootMenu.CanAttachSelected(), Is.False);
            var child = new GameObject("Child");
            Undo.RegisterCreatedObjectUndo(child, "Create Test Child");
            child.transform.SetParent(_root.transform);
            Selection.activeGameObject = child;
            Assert.That(SceneRootMenu.CanAttachSelected(), Is.False);
            Selection.activeGameObject = _root;
            Assert.That(SceneRootMenu.CanAttachSelected(), Is.True);
        }
    }
}
