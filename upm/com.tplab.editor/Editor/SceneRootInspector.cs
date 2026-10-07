using TPLab.Core.Lifecycle;
using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Editor
{
    [CustomEditor(typeof(SceneOwnedRoot))]
    internal class SceneRootInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                DrawDefaultInspector();
            }
        }
    }

    [CustomEditor(typeof(SingletonSceneRoot))]
    internal sealed class SingletonSceneRootInspector : SceneRootInspector
    {
    }
}
