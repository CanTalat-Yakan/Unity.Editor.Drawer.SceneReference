#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEssentials
{
    public partial class SceneReference : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector] private SceneAsset _sceneAsset;

        private bool IsValidSceneAsset => _sceneAsset != null;
        private string GetScenePath() => _sceneAsset ? AssetDatabase.GetAssetPath(_sceneAsset) : _scenePath;
        private SceneAsset GetSceneAssetFromPath() => AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);

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
                _guid = AssetDatabase.AssetPathToGUID(_scenePath);
                _buildIndex = BuildUtilities.GetBuildScene(_sceneAsset).BuildIndex;
            }
            else
            {
                _guid = default;
                _buildIndex = -1;
            }
        }

        public void OnBeforeSerialize()
        {
            UpdateSceneInfo();
            UpdateState();
        }

        public void OnAfterDeserialize()
        {
            EditorApplication.update += HandleAfterDeserialize;
        }

        private void HandleAfterDeserialize()
        {
            EditorApplication.update -= HandleAfterDeserialize;
            UpdateSceneInfo();
            if (!Application.isPlaying)
                EditorSceneManager.MarkAllScenesDirty();
        }

        private void UpdateState()
        {
            if (string.IsNullOrEmpty(Path))
            {
                Debug.LogWarning("SceneReference is empty. It is not referencing anything!");
                _state = SceneReferenceState.Unsafe;
                return;
            }

            if (IsAddressableScene())
                _state = SceneReferenceState.Addressable;
            else
            {
                _state = IsSceneEnabled()
                    ? SceneReferenceState.Regular
                    : SceneReferenceState.Unsafe;
            }
        }

        private bool IsAddressableScene()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            var guid = Guid.ToString();
            var entry = settings.FindAssetEntry(guid);
            if (entry != null && entry.IsScene)
            {
                _addressableReference = new AssetReference(guid);
                return true;
            }

            _addressableReference = null;
            return false;
        }

        public bool IsSceneEnabled()
        {
            if (_buildIndex >= 0)
            {
                var buildScene = BuildUtilities.GetBuildScene(_sceneAsset).Scene;
                if (buildScene.enabled)
                    return true;
                else Debug.LogWarning($"Scene {Name} is in the build settings, but is disabled (unchecked)!");
            }
            else Debug.LogWarning($"Scene {Name} is not included in the build settings!");
            return false;
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
    }

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