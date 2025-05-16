using System;
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

    [Serializable]
    public partial class SceneReference
    {
        [SerializeField, HideInInspector] private string _scenePath = string.Empty;
        [SerializeField, HideInInspector] private string _guid = default;
        [SerializeField, HideInInspector] private int _buildIndex = -1;

        [SerializeField, HideInInspector] private Scene? _loadedScene = null;
        [SerializeField, HideInInspector] private SceneReferenceState _state = default;
        [SerializeField, HideInInspector] private string _address = null;

        public string Guid => _guid;
        public int BuildIndex => _buildIndex;
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
        public string Path => _scenePath;
        public Scene LoadedScene => GetLoadedScene();
        public SceneReferenceState State => _state;
        public string Address => _address ?? string.Empty;
        public AssetReference AddressableReference => _addressableReference ??= new AssetReference(_address);
        private AssetReference _addressableReference;

        public static implicit operator string(SceneReference sceneReference) => sceneReference.Path;

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

        private Scene GetLoadedScene() =>
            Application.isPlaying
                ? _loadedScene ??= SceneManager.GetSceneByPath(Path)
                : default;
    }
}