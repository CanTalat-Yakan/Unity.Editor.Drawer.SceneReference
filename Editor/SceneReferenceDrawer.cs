#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a custom property drawer for the <see cref="SceneReference"/> type in the Unity Editor.
    /// </summary>
    /// <remarks>This drawer allows users to assign a scene asset to a <see cref="SceneReference"/> field in
    /// the Inspector. It ensures that the selected scene asset is properly serialized by updating the associated scene
    /// path.</remarks>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the custom GUI for a serialized property representing a scene asset.
        /// </summary>
        /// <remarks>This method displays an object field for selecting a <see cref="SceneAsset"/>. If the
        /// selected scene asset is cleared,  the associated scene path is also reset to an empty string.</remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sceneAsset = property.FindPropertyRelative("_sceneAsset");
            var scenePath = property.FindPropertyRelative("_scenePath");

            EditorGUI.BeginChangeCheck();
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(
                position, label, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

            if (EditorGUI.EndChangeCheck())
                if (sceneAsset.objectReferenceValue == null)
                    scenePath.stringValue = "";
        }
    }
}
#endif