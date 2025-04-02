# Finding Unity Builtin Assets with Commandify

Commandify provides three different ways to access Unity's built-in assets through its selector system. This guide explains the syntax for each method.

## 1. List All Assets of a Type from Built-in Extra Resources

To get a list of all assets of a specific type from Unity's built-in extra resources, use:

```
Resources/unity_builtin_extra::<AssetType>
```

This will return all resources of AssetType in the built-in extra resources. You can use `Material`, `Sprite`, or `Shader` as the AssetType.

## 2. Get Specific Built-in Extra Resource

To get a specific built-in extra resource by its name (requires correct type specification). For example:

```
BuiltinExtra/Sprites-Default.mat::Material
```

Make sure to specify both the correct file name and the correct type after the double colon.

## 3. Get Built-in Mesh Resources

For accessing Unity's default primitive meshes, use these selectors:

```
BuiltinResources/New-Sphere.fbx::Mesh
BuiltinResources/New-Capsule.fbx::Mesh
BuiltinResources/New-Cylinder.fbx::Mesh
BuiltinResources/Cube.fbx::Mesh
BuiltinResources/New-Plane.fbx::Mesh
BuiltinResources/Quad.fbx::Mesh
```

## Usage Notes

- All assets are read-only and cannot be modified
- These assets are always available, regardless of project settings
