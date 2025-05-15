using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityEssentials
{
    [Serializable]
    public partial class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private SceneAsset _sceneAsset;
        private bool IsValidSceneAsset => _sceneAsset != null;
#endif

        [SerializeField, HideInInspector] private string _scenePath = string.Empty;
        [SerializeField, HideInInspector] private GUID _guid = default;
        [SerializeField, HideInInspector] private int _buildIndex = -1;

        public string Path
        {
            get
            {
#if UNITY_EDITOR
                return GetScenePath();
#else
                return _scenePath;
#endif
            }
            set
            {
                _scenePath = value;
#if UNITY_EDITOR
                _sceneAsset = GetSceneAssetFromPath();
                UpdateSceneInfo();
#endif
            }
        }

        public GUID Guid => GetSceneGuid();
        public int BuildIndex => _buildIndex;
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public static implicit operator string(SceneReference sceneReference) => sceneReference.Path;

#if UNITY_EDITOR
        public void OnBeforeSerialize() =>
            UpdateSceneInfo();

        public void OnAfterDeserialize() =>
            EditorApplication.update += HandleAfterDeserialize;
#endif

        #region Utilities
#if UNITY_EDITOR
        private string GetScenePath() =>
            _sceneAsset ? AssetDatabase.GetAssetPath(_sceneAsset) : _scenePath;

        private SceneAsset GetSceneAssetFromPath() =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);

        private GUID GetSceneGuid()
        {
            if (_guid == default)
                UpdateSceneInfo();

            return _guid;
        }

        private void UpdateSceneInfo()
        {
            if (!IsValidSceneAsset && !string.IsNullOrEmpty(_scenePath))
            {
                _sceneAsset = GetSceneAssetFromPath();
                if (_sceneAsset == null)
                    _scenePath = string.Empty;
            }

            if (IsValidSceneAsset)
            {
                _scenePath = GetScenePath();
                _guid = AssetDatabase.GUIDFromAssetPath(_scenePath);
                _buildIndex = BuildUtilities.GetBuildScene(_sceneAsset).BuildIndex;
            }
            else
            {
                _guid = default;
                _buildIndex = -1;
            }
        }

        private void HandleAfterDeserialize()
        {
            EditorApplication.update -= HandleAfterDeserialize;

            UpdateSceneInfo();

            if (!Application.isPlaying)
                EditorSceneManager.MarkAllScenesDirty();
        }

        public static class BuildUtilities
        {
            public struct BuildScene
            {
                public int BuildIndex;
                public string AssetPath;
                public EditorBuildSettingsScene Scene;
            }

            public static BuildScene GetBuildScene(SceneAsset scene)
            {
                var path = AssetDatabase.GetAssetPath(scene);
                var scenes = EditorBuildSettings.scenes;

                for (int i = 0; i < scenes.Length; i++)
                    if (scenes[i].path == path)
                        return new BuildScene
                        {
                            BuildIndex = i,
                            AssetPath = path,
                            Scene = scenes[i]
                        };

                return new BuildScene { BuildIndex = -1 };
            }
        }
#endif
        #endregion
    }

#if UNITY_EDITOR
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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }
}
#endif