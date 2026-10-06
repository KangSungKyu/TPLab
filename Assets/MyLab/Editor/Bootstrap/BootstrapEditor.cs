using MyLab.Core.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace MyLab.Core.Editor.Bootstrap
{
    [CustomEditor(typeof(BootstrapSystem))]
    internal sealed class BootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sceneRoot"));
                var path = serializedObject.FindProperty("_firstScenePath");
                var previous = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
                EditorGUI.BeginChangeCheck();
                var selected = (SceneAsset)EditorGUILayout.ObjectField("First Game Scene", previous, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck()) path.stringValue = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
                if (selected == null && !string.IsNullOrEmpty(path.stringValue))
                    EditorGUILayout.HelpBox("Missing scene: " + path.stringValue, MessageType.Error);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoStart"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_loadMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_callbacks"));
            }
            if (serializedObject.ApplyModifiedProperties()) BootstrapEditorValidation.Queue();
            if (!EditorApplication.isPlaying)
            {
                foreach (string error in BootstrapValidator.ValidateBootstrap((BootstrapSystem)target))
                    EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }
    }
}
