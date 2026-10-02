using MyLab.Core.Lifecycle;
using UnityEditor;
using UnityEngine;

namespace MyLab.Core.Editor
{
    /// <summary>Offers root ownership selection on the existing scene GameObject with Unity Undo.</summary>
    public static class SceneRootMenu
    {
        [MenuItem("GameObject/MyLab/Scene Root/Scene Owned", false, 10)]
        private static void AttachSceneOwned() => AttachSelected(SceneRootMode.SceneOwned);

        [MenuItem("GameObject/MyLab/Scene Root/Singleton", false, 11)]
        private static void AttachSingleton() => AttachSelected(SceneRootMode.Singleton);

        /// <summary>Returns whether the current selection is an editable scene root without a host.</summary>
        [MenuItem("GameObject/MyLab/Scene Root/Scene Owned", true)]
        [MenuItem("GameObject/MyLab/Scene Root/Singleton", true)]
        public static bool CanAttachSelected()
        {
            var root = Selection.activeGameObject;
            return !EditorApplication.isPlayingOrWillChangePlaymode && root != null && root.scene.IsValid() &&
                root.transform.parent == null && root.GetComponent<SceneOwnedRoot>() == null &&
                root.GetComponent<SingletonSceneRoot>() == null;
        }

        private static void AttachSelected(SceneRootMode mode)
        {
            if (!CanAttachSelected())
            {
                return;
            }
            var root = Selection.activeGameObject;
            var installers = root.GetComponents<SceneRootInstaller>();
            Component host;
            if (mode == SceneRootMode.SceneOwned)
            {
                var owner = Undo.AddComponent<SceneOwnedRoot>(root);
                Undo.RecordObject(owner, "Configure Scene Root");
                owner.Configure(installers);
                host = owner;
            }
            else
            {
                var owner = Undo.AddComponent<SingletonSceneRoot>(root);
                Undo.RecordObject(owner, "Configure Scene Root");
                owner.Configure(installers);
                host = owner;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(host);
            EditorUtility.SetDirty(host);
            Undo.FlushUndoRecordObjects();
        }
    }
}
