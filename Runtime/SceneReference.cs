using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

namespace UnityEssentials
{
    /// <summary>
    /// Represents the state of a scene reference, indicating how the scene is managed or accessed.
    /// </summary>
    /// <remarks>This enumeration is used to classify scene references based on their safety and
    /// accessibility: <list type="bullet"> <item> <description><see cref="Unsafe"/> indicates that the scene reference
    /// is not safe to use, potentially due to missing or invalid data.</description> </item> <item> <description><see
    /// cref="Regular"/> indicates that the scene reference is a standard reference, typically managed
    /// locally.</description> </item> <item> <description><see cref="Addressable"/> indicates that the scene reference
    /// is managed through an addressable asset system, allowing for dynamic loading.</description> </item>
    /// </list></remarks>
    public enum SceneReferenceState
    {
        Unsafe = 0,
        Regular = 1,
        Addressable = 2
    }

    /// <summary>
    /// Represents a reference to a Unity scene, supporting both regular and addressable scenes.
    /// </summary>
    /// <remarks>This class provides functionality to reference, load, and unload Unity scenes by their path,
    /// GUID, or addressable asset reference. It supports both regular scenes (loaded via <see cref="SceneManager"/>)
    /// and addressable scenes (loaded via Unity Addressables).</remarks>
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

        /// <summary>
        /// Loads the scene using the specified loading mode.
        /// </summary>
        /// <remarks>The method determines the loading mechanism based on the scene's state. If the scene 
        /// is addressable, it uses the Addressables system to load the scene asynchronously.  Otherwise, it uses the
        /// <see cref="SceneManager"/> to load the scene synchronously.</remarks>
        /// <param name="mode">The mode in which to load the scene. The default is <see cref="LoadSceneMode.Single"/>,  which loads the
        /// scene and closes all others. Use <see cref="LoadSceneMode.Additive"/>  to load the scene alongside others.</param>
        public void Load(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (State == SceneReferenceState.Addressable)
                Addressables.LoadSceneAsync(_addressableReference, mode);
            else if (State == SceneReferenceState.Regular)
                SceneManager.LoadScene(Path, new LoadSceneParameters(mode));
        }

        /// <summary>
        /// Asynchronously loads the scene specified by this scene reference.
        /// </summary>
        /// <remarks>This method should only be called when the scene reference is in a valid state  (<see
        /// cref="SceneReferenceState.Regular"/>). If the state is not regular, the method  will return <see
        /// langword="null"/> and no loading operation will be initiated.</remarks>
        /// <param name="mode">The mode in which to load the scene. The default is <see cref="LoadSceneMode.Single"/>,  which loads the
        /// scene by itself, replacing any currently loaded scenes.  Use <see cref="LoadSceneMode.Additive"/> to load
        /// the scene alongside others.</param>
        /// <returns>An <see cref="AsyncOperation"/> representing the asynchronous operation of loading the scene,  or <see
        /// langword="null"/> if the scene reference is not in a regular state.</returns>
        public AsyncOperation LoadAsync(LoadSceneMode mode = LoadSceneMode.Single) =>
            State == SceneReferenceState.Regular
                ? SceneManager.LoadSceneAsync(Path, mode)
                : null;

        /// <summary>
        /// Loads a scene asynchronously using the addressable asset system.
        /// </summary>
        /// <remarks>This method only loads the scene if the current state is <see
        /// cref="SceneReferenceState.Addressable"/>. If the state is not addressable, the method returns a default
        /// handle.</remarks>
        /// <param name="mode">The mode in which to load the scene. Use <see cref="LoadSceneMode.Single"/> to load the scene exclusively,
        /// or <see cref="LoadSceneMode.Additive"/> to load it additively alongside other scenes.</param>
        /// <returns>An <see cref="AsyncOperationHandle{T}"/> representing the asynchronous operation. The result contains a <see
        /// cref="SceneInstance"/> if the operation is successful, or a default handle if the scene is not addressable.</returns>
        public AsyncOperationHandle<SceneInstance> LoadAsyncAddressable(LoadSceneMode mode = LoadSceneMode.Single) =>
            State == SceneReferenceState.Addressable
                ? Addressables.LoadSceneAsync(_addressableReference, mode)
                : default;

        /// <summary>
        /// Unloads the currently loaded scene using the specified loading mode.
        /// </summary>
        /// <remarks>This method handles both addressable and regular scenes based on the current state of
        /// the scene reference. If the scene is not valid, the method will return without performing any
        /// action.</remarks>
        /// <param name="mode">The scene loading mode to use when unloading the scene. The default is <see cref="LoadSceneMode.Single"/>.</param>
        public void Unload(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!LoadedScene.IsValid())
                return;

            if (State == SceneReferenceState.Addressable)
                Addressables.LoadSceneAsync(_addressableReference, mode);
            else if (State == SceneReferenceState.Regular)
                SceneManager.LoadScene(Path, mode);
        }

        /// <summary>
        /// Asynchronously unloads the currently loaded scene.
        /// </summary>
        /// <remarks>This method checks whether the scene is valid and the state is <see
        /// cref="SceneReferenceState.Regular"/> before initiating the unload operation. If these conditions are not
        /// met, the method returns <see langword="null"/>.</remarks>
        /// <param name="mode">The mode in which the scene should be unloaded. The default is <see cref="LoadSceneMode.Single"/>.</param>
        /// <returns>An <see cref="AsyncOperation"/> representing the asynchronous unload operation, or <see langword="null"/> if
        /// the scene is not valid or the state is not <see cref="SceneReferenceState.Regular"/>.</returns>
        public AsyncOperation UnloadAsync(LoadSceneMode mode = LoadSceneMode.Single) =>
            LoadedScene.IsValid() && State == SceneReferenceState.Regular
                ? SceneManager.LoadSceneAsync(Path, mode)
                : null;

        /// <summary>
        /// Unloads a scene asynchronously if it was loaded as an addressable asset.
        /// </summary>
        /// <remarks>This method checks if the scene is valid and was loaded as an addressable asset
        /// before attempting to unload it. If these conditions are not met, the method returns a default handle without
        /// performing any operation.</remarks>
        /// <param name="mode">The mode in which the scene should be unloaded. Defaults to <see cref="LoadSceneMode.Single"/>.</param>
        /// <returns>An <see cref="AsyncOperationHandle{T}"/> representing the asynchronous operation to unload the scene. If the
        /// scene is not valid or not loaded as an addressable, returns the default handle.</returns>
        public AsyncOperationHandle<SceneInstance> UnloadAsyncAddressable(LoadSceneMode mode = LoadSceneMode.Single) =>
            LoadedScene.IsValid() && State == SceneReferenceState.Addressable
                ? Addressables.LoadSceneAsync(_addressableReference, mode)
                : default;

        /// <summary>
        /// Retrieves the currently loaded scene associated with the specified path.
        /// </summary>
        /// <remarks>This method returns a cached scene if it has been previously loaded. If the
        /// application is not  in play mode, the method will return the default <see cref="Scene"/> value.</remarks>
        /// <returns>The loaded <see cref="Scene"/> if the application is playing and the scene is available;  otherwise, the
        /// default <see cref="Scene"/> value.</returns>
        private Scene GetLoadedScene() =>
            Application.isPlaying
                ? _loadedScene ??= SceneManager.GetSceneByPath(Path)
                : default;
    }
}