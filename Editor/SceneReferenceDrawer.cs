#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
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