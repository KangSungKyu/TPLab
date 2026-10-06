using MyLab.Core.SceneManagement;
using MyLab.Core.ResourceManagement;
using UnityEditor;
using UnityEngine;

namespace MyLab.Core.Editor.Bootstrap
{
    [CustomEditor(typeof(BootstrapSystem))]
    internal sealed class BootstrapEditor : UnityEditor.Editor
    {
        private int _addressMode;

        private void OnEnable()
        {
            serializedObject.Update();
            var reference = serializedObject.FindProperty("_sceneReference");
            var guid = reference?.FindPropertyRelative("m_AssetGUID");
            _addressMode = guid != null && !string.IsNullOrEmpty(guid.stringValue) ? 1 : 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sceneRoot"));
                var source = serializedObject.FindProperty("_sceneSource");
                EditorGUILayout.PropertyField(source, new GUIContent("Scene Source"));
                var path = serializedObject.FindProperty("_firstScenePath");
                var key = serializedObject.FindProperty("_addressableKey");
                var reference = serializedObject.FindProperty("_sceneReference");
                var guid = reference?.FindPropertyRelative("m_AssetGUID");
                var subObject = reference?.FindPropertyRelative("m_SubObjectName");
                var previous = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
                EditorGUI.BeginChangeCheck();
                var selected = (SceneAsset)EditorGUILayout.ObjectField("First Game Scene", previous, typeof(SceneAsset), false);
                bool sceneChanged = EditorGUI.EndChangeCheck();
                if (sceneChanged)
                {
                    path.stringValue = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
                    if ((SceneSource)source.enumValueIndex == SceneSource.Addressable && _addressMode == 1 && guid != null)
                    {
                        guid.stringValue = selected == null ? "" : AssetDatabase.AssetPathToGUID(path.stringValue);
                        if (subObject != null) subObject.stringValue = "";
                    }
                }
                if (selected == null && !string.IsNullOrEmpty(path.stringValue))
                    EditorGUILayout.HelpBox("Missing scene: " + path.stringValue, MessageType.Error);
                if ((SceneSource)source.enumValueIndex == SceneSource.Addressable)
                {
                    EditorGUILayout.LabelField("Addressables Target", EditorStyles.boldLabel);
                    int mode = EditorGUILayout.Popup("Key Type", _addressMode, new[] { "Address", "Scene GUID Reference" });
                    if (mode != _addressMode)
                    {
                        _addressMode = mode;
                        key.stringValue = "";
                        if (guid != null) guid.stringValue = "";
                        if (subObject != null) subObject.stringValue = "";
                    }
                    if (_addressMode == 0)
                    {
                        key.stringValue = EditorGUILayout.TextField("Scene Address", key.stringValue);
                        if (guid != null) guid.stringValue = "";
                    }
                    else
                    {
                        key.stringValue = "";
                        if (guid != null) guid.stringValue = selected == null ? "" : AssetDatabase.AssetPathToGUID(path.stringValue);
                        if (subObject != null) subObject.stringValue = "";
                        EditorGUILayout.LabelField("Reference GUID", guid == null ? "AssetReference unavailable" : guid.stringValue);
                    }
                }
                else
                {
                    key.stringValue = "";
                    if (guid != null) guid.stringValue = "";
                    if (subObject != null) subObject.stringValue = "";
                    _addressMode = 0;
                }
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
