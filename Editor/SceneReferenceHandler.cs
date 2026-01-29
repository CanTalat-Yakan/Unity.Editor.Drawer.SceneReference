#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;
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
        private static bool _subscribed;
        private static bool _revalidateQueued;
        private static bool _isRevalidating;

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
            if (_subscribed)
                return;
            _subscribed = true;

            EditorBuildSettings.sceneListChanged -= OnBuildSettingsChanged;
            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;

            OnModificationGlobal -= OnAddressablesChanged;
            OnModificationGlobal += OnAddressablesChanged;
        }

        /// <summary>
        /// Handles changes to the build settings by revalidating all scene references.
        /// </summary>
        /// <remarks>This method is triggered when the build settings are modified. It ensures that all
        /// scene references remain valid and up-to-date after the changes.</remarks>
        private static void OnBuildSettingsChanged() =>
            QueueRevalidateAllSceneReferences();

        /// <summary>
        /// Handles changes to Addressable Asset settings and triggers revalidation of all addressable changes.
        /// </summary>
        /// <param name="_">The addressable settings instance (unused).</param>
        /// <param name="modEvent">The type of modification event that occurred.</param>
        /// <param name="__">Additional event data (unused).</param>
        private static void OnAddressablesChanged(AddressableAssetSettings _, ModificationEvent modEvent, object __) =>
            RevalidateAllAddressablesChanges(modEvent);

        /// <summary>
        /// Revalidates all scene references when specific addressable asset modifications occur.
        /// </summary>
        /// <param name="modEvent">The type of modification event that occurred. Must be <see cref="ModificationEvent.EntryAdded"/>, <see
        /// cref="ModificationEvent.EntryRemoved"/>, or <see cref="ModificationEvent.EntryModified"/> to trigger
        /// revalidation.</param>
        private static void RevalidateAllAddressablesChanges(ModificationEvent modEvent)
        {
            if (modEvent == ModificationEvent.EntryAdded || modEvent == ModificationEvent.EntryRemoved || modEvent == ModificationEvent.EntryModified)
                QueueRevalidateAllSceneReferences();
        }

        private static void QueueRevalidateAllSceneReferences()
        {
            if (_revalidateQueued)
                return;

            _revalidateQueued = true;
            EditorApplication.delayCall += () =>
            {
                _revalidateQueued = false;
                RevalidateAllSceneReferences();
            };
        }

        /// <summary>
        /// Revalidates all <see cref="SceneReference"/> instances in the project.
        /// </summary>
        /// <remarks>This method scans all assets in the project for <see cref="MonoBehaviour"/>
        /// components  containing a <see cref="SceneReference"/> and updates their state. It marks the modified
        /// objects as dirty to ensure changes are saved.</remarks>
        private static void RevalidateAllSceneReferences()
        {
            if (_isRevalidating)
                return;

            _isRevalidating = true;
            try
            {
                // Scan serialized objects for fields of type SceneReference and call UpdateState.
                // We keep this Editor-only and conservative to avoid impacting build settings UI.
                var guids = AssetDatabase.FindAssets("t:ScriptableObject t:GameObject");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                    if (mainAssetType == typeof(GameObject))
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go != null)
                            RevalidateUnityObject(go);
                    }
                    else
                    {
                        var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                        if (so != null)
                            RevalidateUnityObject(so);
                    }
                }
            }
            finally
            {
                _isRevalidating = false;
            }
        }

        private static void RevalidateUnityObject(Object obj)
        {
            // Prefabs: walk all components.
            if (obj is GameObject go)
            {
                var components = go.GetComponentsInChildren<Component>(true);
                foreach (var c in components)
                {
                    if (c == null)
                        continue;
                    RevalidateSerializedObject(c);
                }
                return;
            }

            // ScriptableObjects.
            RevalidateSerializedObject(obj);
        }

        private static void RevalidateSerializedObject(Object target)
        {
            var so = new SerializedObject(target);
            var changed = false;

            SerializedProperty iterator = so.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Look for SceneReference's backing field pattern: a generic-serialized blob that contains _scenePath etc.
                if (iterator.propertyType != SerializedPropertyType.Generic)
                    continue;

                // Fast check: do we have at least the _scenePath child?
                var scenePathProp = iterator.FindPropertyRelative("_scenePath");
                if (scenePathProp == null)
                    continue;

                // Heuristic: ensure this generic property looks like SceneReference by checking for expected children.
                var guidProp = iterator.FindPropertyRelative("_guid");
                var buildIndexProp = iterator.FindPropertyRelative("_buildIndex");
                if (guidProp == null || buildIndexProp == null)
                    continue;

                // Create a managed SceneReference instance and sync the key serialized data into it.
                // Then call UpdateState and write back the updated state fields.
                var sr = new SceneReference();

                // Write into the private backing fields via SerializedObject on a temp ScriptableObject is overkill;
                // instead use reflection for these specific fields.
                // NOTE: This is Editor-only; reflection cost is acceptable since we're debounced.
                var t = typeof(SceneReference);
                SetPrivateField(t, sr, "_scenePath", scenePathProp.stringValue);
                SetPrivateField(t, sr, "_guid", guidProp.stringValue);
                SetPrivateField(t, sr, "_buildIndex", buildIndexProp.intValue);

                // Call UpdateState (Editor partial).
                sr.UpdateState();

                // Write back state + address (UpdateState/IsAddressableScene can update these).
                var stateProp = iterator.FindPropertyRelative("_state");
                if (stateProp != null)
                {
                    var newState = (int)sr.State;
                    if (stateProp.enumValueIndex != newState)
                    {
                        stateProp.enumValueIndex = newState;
                        changed = true;
                    }
                }

                var addressProp = iterator.FindPropertyRelative("_address");
                if (addressProp != null)
                {
                    var newAddress = sr.Address;
                    if (addressProp.stringValue != newAddress)
                    {
                        addressProp.stringValue = newAddress;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(instance, value);
        }
    }
}
#endif