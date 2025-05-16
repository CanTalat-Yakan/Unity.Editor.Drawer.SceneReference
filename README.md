# Unity Essentials

**Unity Essentials** is a lightweight, modular utility namespace designed to streamline development in Unity. 
It provides a collection of foundational tools, extensions, and helpers to enhance productivity and maintain clean code architecture.

## 📦 This Package

This package is part of the **Unity Essentials** ecosystem.  
It integrates seamlessly with other Unity Essentials modules and follows the same lightweight, dependency-free philosophy.

## 🌐 Namespace

All utilities are under the `UnityEssentials` namespace. This keeps your project clean, consistent, and conflict-free.

```csharp
using UnityEssentials;
```

# SceneReference

A serializable class for referencing Unity scenes through path, GUID, build index, or addressable address. Supports both synchronous and asynchronous loading/unloading for regular and addressable scenes. Compatible with Unity Addressables and Editor scene references.



## Features

- Abstracts both regular and addressable scene referencing
- Stores scene path, GUID, build index, and address
- Lazy-loads scene instance in play mode
- Implicit conversion to `string` returns scene path
- Unified interface for:
  - Load / LoadAsync (regular)
  - LoadAsyncAddressable
  - Unload / UnloadAsync (regular)
  - UnloadAsyncAddressable



## Scene States

```csharp
public enum SceneReferenceState
{
    Unsafe = 0,      // Invalid or unrecognized state
    Regular = 1,     // Scene loaded by path (non-addressable)
    Addressable = 2  // Scene loaded via Addressables system
}
```