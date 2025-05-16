#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

namespace UnityEssentials
{
    public partial class SceneReference : ISerializationCallbackReceiver
    {
        [InitializeOnLoadMethod]
        public static void SubscribeToEvents()
        {
            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;
            OnModificationGlobal += OnAddressablesChanged;
        }

        private static void OnBuildSettingsChanged() =>
            RevalidateAllSceneReferences();

        private static void OnAddressablesChanged(AddressableAssetSettings settings, ModificationEvent modEvent, object obj) =>
            RevalidateAllAddressablesChanges(settings, modEvent, obj);

        private static void RevalidateAllAddressablesChanges(AddressableAssetSettings settings, ModificationEvent modEvent, object obj)
        {
            if (modEvent == ModificationEvent.EntryAdded || modEvent == ModificationEvent.EntryRemoved || modEvent == ModificationEvent.EntryModified)
                RevalidateAllSceneReferences();
        }

        private static void RevalidateAllSceneReferences()
        {
            // Find and update all SceneReference instances in the project
            var guids = AssetDatabase.FindAssets("t:ScriptableObject t:GameObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var objs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is MonoBehaviour mono && mono.TryGetComponent<SceneReference>(out var sr))
                    {
                        sr.UpdateState();
                        EditorUtility.SetDirty(mono);
                    }
                }
            }
        }
    }
}
#endif