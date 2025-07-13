#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

namespace UnityEssentials
{
    /// <summary>
    /// Provides functionality to manage and validate scene references in the Unity Editor.
    /// </summary>
    /// <remarks>This class subscribes to Unity Editor events to automatically revalidate scene references
    /// when changes occur in the build settings or Addressable Asset settings. It ensures that all <see
    /// cref="SceneReference"/> instances in the project remain up-to-date.</remarks>
    public class SceneReferenceHandler
    {
        /// <summary>
        /// Subscribes to global and editor-specific events to handle changes in build settings and addressable assets.
        /// </summary>
        /// <remarks>This method registers event handlers for the <see
        /// cref="EditorBuildSettings.sceneListChanged"/> event  and the <c>OnModificationGlobal</c> event. It ensures
        /// that changes to the build settings or addressable  assets are appropriately handled. This method is
        /// automatically invoked when the editor loads.</remarks>
        [InitializeOnLoadMethod]
        public static void SubscribeToEvents()
        {
            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;
            OnModificationGlobal += OnAddressablesChanged;
        }

        /// <summary>
        /// Handles changes to the build settings by revalidating all scene references.
        /// </summary>
        /// <remarks>This method is triggered when the build settings are modified. It ensures that all
        /// scene references remain valid and up-to-date after the changes.</remarks>
        private static void OnBuildSettingsChanged() =>
            RevalidateAllSceneReferences();

        /// <summary>
        /// Handles changes to Addressable Asset settings and triggers revalidation of all addressable changes.
        /// </summary>
        /// <param name="settings">The <see cref="AddressableAssetSettings"/> instance that was modified.</param>
        /// <param name="modEvent">The type of modification event that occurred.</param>
        /// <param name="obj">An object containing additional data related to the modification event.</param>
        private static void OnAddressablesChanged(AddressableAssetSettings settings, ModificationEvent modEvent, object obj) =>
            RevalidateAllAddressablesChanges(settings, modEvent, obj);

        /// <summary>
        /// Revalidates all scene references when specific addressable asset modifications occur.
        /// </summary>
        /// <param name="settings">The addressable asset settings associated with the modification event.</param>
        /// <param name="modEvent">The type of modification event that occurred. Must be <see cref="ModificationEvent.EntryAdded"/>, <see
        /// cref="ModificationEvent.EntryRemoved"/>, or <see cref="ModificationEvent.EntryModified"/> to trigger
        /// revalidation.</param>
        /// <param name="obj">An object associated with the modification event. This parameter is not used in the revalidation process.</param>
        private static void RevalidateAllAddressablesChanges(AddressableAssetSettings settings, ModificationEvent modEvent, object obj)
        {
            if (modEvent == ModificationEvent.EntryAdded || modEvent == ModificationEvent.EntryRemoved || modEvent == ModificationEvent.EntryModified)
                RevalidateAllSceneReferences();
        }

        /// <summary>
        /// Revalidates all <see cref="SceneReference"/> instances in the project.
        /// </summary>
        /// <remarks>This method scans all assets in the project for <see cref="MonoBehaviour"/>
        /// components  containing a <see cref="SceneReference"/> and updates their state. It marks the modified 
        /// objects as dirty to ensure changes are saved.</remarks>
        private static void RevalidateAllSceneReferences()
        {
            // Find and update all SceneReference instances in the project
            var guids = AssetDatabase.FindAssets("t:ScriptableObject t:GameObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var loadedAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in loadedAssets)
                    if (asset.GetType().IsAssignableFrom(typeof(MonoBehaviour)))
                        if (asset is MonoBehaviour mono && mono.TryGetComponent<SceneReference>(out var sceneReference))
                        {
                            sceneReference?.UpdateState();
                            EditorUtility.SetDirty(mono);
                        }
            }
        }
    }
}
#endif