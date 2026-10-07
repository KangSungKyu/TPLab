using TPLab.Core.SceneManagement;
using TPLab.Core.ResourceManagement;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Editor.Bootstrap
{
    [CustomEditor(typeof(BootstrapSystem))]
    internal sealed class BootstrapEditor : UnityEditor.Editor
    {
        private int _addressMode;
        private string[] _firstEntryIds = Array.Empty<string>();

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
                var settings = serializedObject.FindProperty("_transitionSettings");
                var firstId = serializedObject.FindProperty("_firstTransitionId");
                EditorGUILayout.PropertyField(settings, new GUIContent("Transition Settings"));
                if (settings.objectReferenceValue is SceneTransitionSettings transitionSettings)
                {
                    try
                    {
                        _firstEntryIds = transitionSettings.CreateSnapshot()
                            .Where(definition => definition.Kind == SceneTransitionKind.FirstEntry)
                            .Select(definition => definition.Id).ToArray();
                        int selectedIndex = Array.IndexOf(_firstEntryIds, firstId.stringValue);
                        var options = new[] { "<Select FirstEntry ID>" }.Concat(_firstEntryIds).ToArray();
                        int next = EditorGUILayout.Popup("FirstEntry ID", selectedIndex < 0 ? 0 : selectedIndex + 1, options);
                        if (next > 0) firstId.stringValue = _firstEntryIds[next - 1];
                        else if (selectedIndex >= 0) firstId.stringValue = "";
                    }
                    catch (Exception exception) { EditorGUILayout.HelpBox("Invalid transition settings: " + exception.Message, MessageType.Error); }
                }
                else if (!string.IsNullOrEmpty(firstId.stringValue))
                {
                    EditorGUILayout.HelpBox("A FirstEntry ID requires Transition Settings.", MessageType.Error);
                    EditorGUILayout.PropertyField(firstId, new GUIContent("FirstEntry ID (clear to use legacy fields)"));
                }
                else
                {
                    var source = serializedObject.FindProperty("_sceneSource");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(source, new GUIContent("Scene Source"));
                    bool sourceChanged = EditorGUI.EndChangeCheck();
                    var path = serializedObject.FindProperty("_firstScenePath");
                    var key = serializedObject.FindProperty("_addressableKey");
                    var reference = serializedObject.FindProperty("_sceneReference");
                    var guid = reference?.FindPropertyRelative("m_AssetGUID");
                    var subObject = reference?.FindPropertyRelative("m_SubObjectName");
                    if (sourceChanged && (SceneSource)source.enumValueIndex == SceneSource.BuildScene)
                    {
                        key.stringValue = "";
                        if (guid != null) guid.stringValue = "";
                        if (subObject != null) subObject.stringValue = "";
                        _addressMode = 0;
                    }
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
                            if (_addressMode == 0)
                            {
                                if (guid != null) guid.stringValue = "";
                                if (subObject != null) subObject.stringValue = "";
                            }
                            else
                            {
                                key.stringValue = "";
                                if (guid != null) guid.stringValue = selected == null ? "" : AssetDatabase.AssetPathToGUID(path.stringValue);
                                if (subObject != null) subObject.stringValue = "";
                            }
                        }
                        if (_addressMode == 0)
                        {
                            EditorGUI.BeginChangeCheck();
                            string address = EditorGUILayout.TextField("Scene Address", key.stringValue);
                            if (EditorGUI.EndChangeCheck()) key.stringValue = address;
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Reference GUID", guid == null ? "AssetReference unavailable" : guid.stringValue);
                            string selectedGuid = selected == null ? "" : AssetDatabase.AssetPathToGUID(path.stringValue);
                            if (guid != null && (!string.Equals(guid.stringValue, selectedGuid, StringComparison.Ordinal) ||
                                (subObject != null && !string.IsNullOrEmpty(subObject.stringValue))))
                                EditorGUILayout.HelpBox("The serialized scene reference does not match this scene asset or targets a sub-object. Its value is preserved; select the scene asset or change Key Type to repair it.", MessageType.Error);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(key.stringValue) || (guid != null && !string.IsNullOrEmpty(guid.stringValue)) ||
                            (subObject != null && !string.IsNullOrEmpty(subObject.stringValue)))
                            EditorGUILayout.HelpBox("Serialized Addressables values are retained while Build Scene is selected. Change Source to Addressable and back to Build Scene to explicitly clear them.", MessageType.Warning);
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_loadMode"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoStart"));
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
