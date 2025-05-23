using UnityEditor;

namespace UnityEssentials
{
    /// <summary>
    /// Provides utility methods for working with build scenes in Unity's Editor.
    /// </summary>
    /// <remarks>This class includes methods for retrieving information about scenes configured in the
    /// Unity Editor's build settings. It is intended to simplify operations related to build scenes.</remarks>
    public static class BuildUtilities
    {
        /// <summary>
        /// Represents a scene included in the build process, containing its build index, asset path, and associated
        /// editor settings.
        /// </summary>
        /// <remarks>This struct is used to encapsulate information about a scene that is part of
        /// the build configuration. It includes the scene's build index, its asset path in the project, and its
        /// settings as defined in the editor.</remarks>
        public struct BuildScene
        {
            public int BuildIndex;
            public string AssetPath;
            public EditorBuildSettingsScene Scene;
        }

        /// <summary>
        /// Retrieves the build settings information for a given scene asset.
        /// </summary>
        /// <remarks>This method searches the Unity Editor's build settings for the specified
        /// scene asset and returns its associated build settings information. If the scene is not part of the build
        /// settings, the returned <see cref="BuildScene"/> object will indicate this with a build index of
        /// -1.</remarks>
        /// <param name="scene">The scene asset to locate in the build settings.</param>
        /// <returns>A <see cref="BuildScene"/> object containing the build index, asset path, and build settings information
        /// for the specified scene. If the scene is not included in the build settings, the <see
        /// cref="BuildScene.BuildIndex"/> will be set to -1.</returns>
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
