#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a reference to a Unity scene, providing functionality to manage and validate the scene's state.
    /// </summary>
    /// <remarks>This class is used to reference Unity scenes in a serialized manner, ensuring that the
    /// scene's state is kept up-to-date and valid. It supports scenarios such as detecting whether the scene is
    /// included in the build settings, whether it is addressable, and whether it is enabled. Additionally, it handles
    /// serialization and deserialization to maintain the integrity of the scene reference.  Note: This class is
    /// intended for use in the Unity Editor and relies on UnityEditor APIs.</remarks>
    public partial class SceneReference : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector] private SceneAsset _sceneAsset;

        private bool IsValidSceneAsset => _sceneAsset != null;
        private string GetScenePath() => _sceneAsset ? AssetDatabase.GetAssetPath(_sceneAsset) : _scenePath;
        private SceneAsset GetSceneAssetFromPath() => AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);

        /// <summary>
        /// Updates the state of the scene reference based on its current path and configuration.
        /// </summary>
        /// <remarks>This method evaluates the scene reference's path to determine its validity and type. 
        /// If the path is empty or invalid, the state is set to <see cref="SceneReferenceState.Unsafe"/>.  If the scene
        /// is addressable, the state is set to <see cref="SceneReferenceState.Addressable"/>.  Otherwise, the state is
        /// set to <see cref="SceneReferenceState.Regular"/> if the scene is enabled,  or <see
        /// cref="SceneReferenceState.Unsafe"/> if it is not.</remarks>
        public void UpdateState()
        {
            if (string.IsNullOrEmpty(Path))
            {
                if (_state != SceneReferenceState.Unsafe)
                    Debug.LogWarning("SceneReference is empty. It is not referencing anything!");
                _state = SceneReferenceState.Unsafe;
            }
            else if (IsAddressableScene())
                _state = SceneReferenceState.Addressable;
            else _ = IsSceneEnabled() ?
                _state = SceneReferenceState.Regular :
                _state = SceneReferenceState.Unsafe;
        }

        /// <summary>
        /// Prepares the object for serialization by updating its scene information and state.
        /// </summary>
        /// <remarks>This method should be called before serializing the object to ensure that all
        /// relevant  data is up-to-date. It updates internal scene-related information and the object's state  to
        /// reflect the current context.</remarks>
        public void OnBeforeSerialize()
        {
            UpdateSceneInfo();
            UpdateState();
        }

        /// <summary>
        /// Registers a callback to handle actions that need to occur after deserialization.
        /// </summary>
        /// <remarks>This method subscribes to the <see cref="EditorApplication.update"/> event to execute
        /// post-deserialization logic. Ensure that the callback is properly unsubscribed when no  longer needed to
        /// avoid potential memory leaks or unintended behavior.</remarks>
        public void OnAfterDeserialize() =>
            EditorApplication.update += HandleAfterDeserialize;

        /// <summary>
        /// Unsubscribes the <see cref="HandleAfterDeserialize"/> method from the <see cref="EditorApplication.update"/>
        /// event.
        /// </summary>
        /// <remarks>This method is typically called when the object is being destroyed to ensure that the
        /// event subscription is properly removed, preventing potential memory leaks or unintended behavior.</remarks>
        public void OnDestroy() =>
            EditorApplication.update -= HandleAfterDeserialize;

        /// <summary>
        /// Handles post-deserialization logic for the object.
        /// </summary>
        /// <remarks>This method is invoked after the object has been deserialized. It ensures that       
        /// the scene information is updated and marks all scenes as dirty in the editor          if the application is
        /// not in play mode. The method also unregisters itself          from the <see
        /// cref="EditorApplication.update"/> event to prevent repeated execution.</remarks>
        private void HandleAfterDeserialize()
        {
            EditorApplication.update -= HandleAfterDeserialize;
            UpdateSceneInfo();
            if (!Application.isPlaying)
                EditorSceneManager.MarkAllScenesDirty();
        }

        /// <summary>
        /// Updates the scene information, including the scene asset, path, GUID, and build index.
        /// </summary>
        /// <remarks>This method ensures that the scene information is consistent by validating the scene
        /// asset and updating related properties such as the scene path, GUID, and build index. If the scene asset is
        /// invalid, the scene path is cleared, and default values are assigned to the GUID and build index.</remarks>
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

        /// <summary>
        /// Determines whether the current asset is an addressable scene in the Addressable Asset system.
        /// </summary>
        /// <remarks>This method checks the Addressable Asset settings to determine if the asset,
        /// identified by its GUID, is registered as an addressable scene. If the asset is an addressable scene, an <see
        /// cref="AssetReference"/> is created for it; otherwise, the reference is set to <see
        /// langword="null"/>.</remarks>
        /// <returns><see langword="true"/> if the asset is an addressable scene; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Determines whether the scene associated with this instance is enabled in the build settings.
        /// </summary>
        /// <remarks>If the scene is included in the build settings but is disabled (unchecked), a warning
        /// is logged.  If the scene is not included in the build settings, a warning is also logged.</remarks>
        /// <returns><see langword="true"/> if the scene is included in the build settings and is enabled;  otherwise, <see
        /// langword="false"/>.</returns>
        private bool IsSceneEnabled()
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
    }
}
#endif