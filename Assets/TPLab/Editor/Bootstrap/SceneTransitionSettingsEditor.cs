using UnityEditor;
using UnityEngine;

namespace TPLab.Core.Editor.Bootstrap
{
    [CustomEditor(typeof(TPLab.Core.SceneManagement.SceneTransitionSettings))]
    internal sealed class SceneTransitionSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_definitions"), true);
            if (serializedObject.ApplyModifiedProperties()) BootstrapEditorValidation.Queue();
            try
            {
                ((TPLab.Core.SceneManagement.SceneTransitionSettings)target).CreateSnapshot();
            }
            catch (System.Exception exception)
            {
                EditorGUILayout.HelpBox("Invalid transition settings: " + exception.Message, MessageType.Error);
            }
        }
    }

    [CustomPropertyDrawer(typeof(TPLab.Core.SceneManagement.SceneTransitionDefinition))]
    internal sealed class SceneTransitionDefinitionDrawer : PropertyDrawer
    {
        private const float Gap = 2f;
        private const float Line = 18f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int kind = property.FindPropertyRelative("_kind").enumValueIndex;
            int backend = property.FindPropertyRelative("_sceneSource").enumValueIndex;
            int rows = 4 + (kind == 0 ? 0 : 1) + (backend == (int)TPLab.Core.ResourceManagement.SceneSource.Addressable ? 1 : 0) +
                (kind <= 1 ? 1 : kind == 2 ? 2 : 0);
            return Line * rows + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_requiredConditionIds"), true) + Gap * (rows + 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var id = property.FindPropertyRelative("_id");
            var kind = property.FindPropertyRelative("_kind");
            var sourcePath = property.FindPropertyRelative("_sourceScenePath");
            var backend = property.FindPropertyRelative("_sceneSource");
            var targetPath = property.FindPropertyRelative("_scenePath");
            var key = property.FindPropertyRelative("_addressableKey");
            var mode = property.FindPropertyRelative("_mode");
            var activate = property.FindPropertyRelative("_activate");
            var priority = property.FindPropertyRelative("_priority");
            var required = property.FindPropertyRelative("_requiredConditionIds");
            float y = position.y;
            Rect Row()
            {
                var rect = new Rect(position.x, y, position.width, Line);
                y += Line + Gap;
                return rect;
            }
            EditorGUI.PropertyField(Row(), id, new GUIContent("ID"));
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(Row(), kind, new GUIContent("Kind"));
            if (EditorGUI.EndChangeCheck())
            {
                if (kind.enumValueIndex == 0) sourcePath.stringValue = "";
                else if (kind.enumValueIndex == 2 || kind.enumValueIndex == 3) mode.enumValueIndex = (int)UnityEngine.SceneManagement.LoadSceneMode.Additive;
            }
            if (kind.enumValueIndex != 0)
            {
                var source = AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath.stringValue);
                EditorGUI.BeginChangeCheck();
                var selected = (SceneAsset)EditorGUI.ObjectField(Row(), "Source Scene", source, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck()) sourcePath.stringValue = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(Row(), backend, new GUIContent("Destination Source"));
            bool backendChanged = EditorGUI.EndChangeCheck();
            var destination = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedTarget = (SceneAsset)EditorGUI.ObjectField(Row(), "Destination Scene", destination, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) targetPath.stringValue = selectedTarget == null ? "" : AssetDatabase.GetAssetPath(selectedTarget);
            if ((TPLab.Core.ResourceManagement.SceneSource)backend.enumValueIndex == TPLab.Core.ResourceManagement.SceneSource.Addressable)
                EditorGUI.PropertyField(Row(), key, new GUIContent("Addressables Key"));
            else if (backendChanged) key.stringValue = "";
            if (kind.enumValueIndex <= 1) EditorGUI.PropertyField(Row(), mode, new GUIContent("Load Mode"));
            if (kind.enumValueIndex == 2)
            {
                EditorGUI.PropertyField(Row(), activate);
                EditorGUI.PropertyField(Row(), priority);
            }
            var requiredRect = new Rect(position.x, y, position.width,
                EditorGUI.GetPropertyHeight(required, true));
            EditorGUI.PropertyField(requiredRect, required, new GUIContent("Required Condition IDs"), true);
            EditorGUI.EndProperty();
        }
    }
}
