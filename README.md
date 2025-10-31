# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Scene Reference Drawer

> Quick overview: Strongly-typed scene picker + runtime helper. Serialize a scene reference with path, GUID, build index, and optional Addressables link, then load/unload by code. The Inspector shows a single field to pick a SceneAsset.

A tiny type + drawer that makes scene fields safer and easier to use. At edit time you pick a SceneAsset; at runtime you get a `SceneReference` that knows its path, GUID, build index, and whether it’s a regular build scene or an Addressable scene.

![screenshot](Documentation/Screenshot.png)

## Features
- Scene picker in the Inspector via a single field
- Stores and exposes:
  - Path, GUID, Build Index, Name
  - State: Regular, Addressable, or Unsafe
  - Addressables link (address GUID and `AssetReference`)
- Loading helpers:
  - Regular scenes: `Load`, `LoadAsync`, `Unload`, `UnloadAsync`
  - Addressables: `LoadAsyncAddressable`, `UnloadAsyncAddressable`
- Implicit cast to string returns the scene path
- Build Settings awareness: warns if the scene is not added or unchecked
- Addressables awareness: detects when the scene is in Addressables (if package present)

## Requirements
- Unity Editor 6000.0+ (Inspector drawer is Editor-only)
- Optional for Addressables support: `com.unity.addressables`

Notes
- The Addressables detection uses the default Addressables settings (`AddressableAssetSettingsDefaultObject`). If Addressables isn’t installed or set up, the scene is treated as non‑addressable.

## Usage
Declare a field and pick a scene in the Inspector

```csharp
using UnityEngine;
using UnityEssentials; // SceneReference

public class SceneLoader : MonoBehaviour
{
    public SceneReference mainMenu;

    public void LoadMenu()
    {
        // Regular (Build Settings) scene
        if (mainMenu.State == SceneReferenceState.Regular)
            mainMenu.Load();

        // Or async
        // mainMenu.LoadAsync();

        // Addressable scene (if configured in Addressables)
        if (mainMenu.State == SceneReferenceState.Addressable)
            mainMenu.LoadAsyncAddressable();
    }
}
```

Working with paths and names

```csharp
// Implicit conversion to string returns the path
string path = mainMenu;    // e.g., "Assets/Scenes/MainMenu.unity"
string name = mainMenu.Name; // e.g., "MainMenu"
int index = mainMenu.BuildIndex; // -1 if not in Build Settings
```

Unload examples

```csharp
// Regular scene unload variants
mainMenu.Unload();
// or
mainMenu.UnloadAsync();

// Addressables unload variant
mainMenu.UnloadAsyncAddressable();
```

Addressables reference

```csharp
// Access the AssetReference for manual control
var aref = mainMenu.AddressableReference; // AssetReference
```

## How It Works
- Type: `SceneReference` (Serializable)
  - Serialized backing: `_sceneAsset`, `_scenePath`, `_guid`, `_buildIndex`, state, and address string
  - Properties expose Path, Guid, BuildIndex, Name, LoadedScene, State, Address, and `AssetReference`
  - Implicit `string` cast returns `Path`
- Editor integration
  - `SceneReferenceDrawer` renders a single `ObjectField` for a `SceneAsset`
  - Clearing the object resets the stored path
  - `SceneReference` implements `ISerializationCallbackReceiver` to update path/ID/index and validate state
- Build Settings + Addressables
  - Build: `BuildUtilities.GetBuildScene(SceneAsset)` resolves build index and info
  - Addressables: checks default Addressables settings; if the GUID is an Addressable scene, creates an `AssetReference`
- State
  - `Regular`: included and enabled in Build Settings
  - `Addressable`: registered as an Addressable scene
  - `Unsafe`: empty/invalid path or disabled/not in Build Settings

## Notes and Limitations
- Include regular scenes in Build Settings (File → Build Settings) so `BuildIndex` is valid and `Regular` state applies
- For Addressables, add the scene to an Addressables group; the drawer does not manage Addressables settings
- Editor‑only validation logs warnings for missing/disabled scenes
- Unload/load helpers are thin wrappers around `SceneManager` or Addressables APIs; choose the async variant when needed
- Multi‑object editing: the picker works per‑object in the Inspector

## Files in This Package
- `Runtime/SceneReference.cs` – Serializable type with load/unload helpers and state
- `Runtime/SceneReferenceEditor.cs` – Editor‑only partial (serialization callbacks, state validation, Addressables/build checks)
- `Runtime/BuildUtilities.cs` – Editor‑only helper to query Build Settings
- `Editor/SceneReferenceDrawer.cs` – PropertyDrawer (SceneAsset picker)
- `Runtime/UnityEssentials.SceneReference.asmdef` – Runtime assembly definition

## Tags
unity, unity-editor, drawer, scene, scenereference, addressables, build-settings, inspector, propertydrawer, tools, workflow
