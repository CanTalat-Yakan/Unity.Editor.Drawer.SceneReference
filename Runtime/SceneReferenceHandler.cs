using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

using UnityEngine.AddressableAssets;

namespace UnityEssentials
{
    public enum SceneReferenceState
    {
        Unsafe = 0,
        Regular = 1,
        Addressable = 2
    }

    public partial class SceneReference
    {
        [SerializeField, HideInInspector] private Scene? _loadedScene = null;
        [SerializeField, HideInInspector] private SceneReferenceState? _state = null;
        [SerializeField, HideInInspector] private string _address = null;

        public Scene LoadedScene => GetLoadedScene();
        public SceneReferenceState State => UpdateState();
        public string Address => GetAddress();

        public AssetReference AddressableReference => _addressableReference ??= new AssetReference(_address);
        private AssetReference _addressableReference;

        #region Loading
        public void Load(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (State == SceneReferenceState.Addressable)
                Addressables.LoadSceneAsync(_addressableReference, mode);
            else if (State == SceneReferenceState.Regular)
                SceneManager.LoadScene(Path, new LoadSceneParameters(mode));
        }

        public AsyncOperation LoadAsync(LoadSceneMode mode = LoadSceneMode.Single) =>
            State == SceneReferenceState.Regular
                ? SceneManager.LoadSceneAsync(Path, mode)
                : null;

        public AsyncOperationHandle<SceneInstance> LoadAsyncAddressable(LoadSceneMode mode = LoadSceneMode.Single) =>
            State == SceneReferenceState.Addressable
                ? Addressables.LoadSceneAsync(_addressableReference, mode)
                : default;
        #endregion

        #region Unloading
        public void Unload(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!LoadedScene.IsValid())
                return;

            if (State == SceneReferenceState.Addressable)
                Addressables.LoadSceneAsync(_addressableReference, mode);
            else if (State == SceneReferenceState.Regular)
                SceneManager.LoadScene(Path, mode);
        }

        public AsyncOperation UnloadAsync(LoadSceneMode mode = LoadSceneMode.Single) =>
            LoadedScene.IsValid() && State == SceneReferenceState.Regular
                ? SceneManager.LoadSceneAsync(Path, mode)
                : null;

        public AsyncOperationHandle<SceneInstance> UnloadAsyncAddressable(LoadSceneMode mode = LoadSceneMode.Single) =>
            LoadedScene.IsValid() && State == SceneReferenceState.Addressable
                ? Addressables.LoadSceneAsync(_addressableReference, mode)
                : default;
        #endregion

        #region Utilities
        private Scene GetLoadedScene() =>
            Application.isPlaying
                ? _loadedScene ??= SceneManager.GetSceneByPath(Path)
                : default;

        private SceneReferenceState UpdateState()
        {
            if (_state != null)
                return _state.Value;

            if (string.IsNullOrEmpty(Path))
            {
                Debug.LogWarning($"SceneReference is empty. It is not referencing anything!");
                return _state ??= SceneReferenceState.Unsafe;
            }

#if UNITY_EDITOR
            if (IsAddressableScene())
                return _state ??= SceneReferenceState.Addressable;

            return _state ??= IsSceneEnabled()
                ? SceneReferenceState.Regular
                : SceneReferenceState.Unsafe;
#else
            return _state.Value;
#endif
        }

        private string GetAddress() =>
            _address ??= _addressableReference?.AssetGUID ?? string.Empty;
        #endregion

        #region Helper
#if UNITY_EDITOR
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
#endif

#if UNITY_EDITOR
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
#endif
        #endregion
    }
}