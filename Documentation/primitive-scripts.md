# Unity Primitive Creation Scripts

This directory contains scripts to create basic 3D primitives in Unity using the commandify system. Each script creates a GameObject with the appropriate mesh and default material.

## Available Scripts

- `create-cube.sh` - Creates a cube primitive
- `create-sphere.sh` - Creates a sphere primitive
- `create-capsule.sh` - Creates a capsule primitive
- `create-cylinder.sh` - Creates a cylinder primitive
- `create-plane.sh` - Creates a plane primitive
- `create-quad.sh` - Creates a quad primitive

## Usage

All scripts follow the same usage pattern:

```bash
commandify> run scripts/create-<primitive>.sh <object-name>
```

For example:
```bash
commandify> run scripts/create-cube.sh MyCube
commandify> run scripts/create-sphere.sh GameBall
commandify> run scripts/create-plane.sh Ground
```

## What the Scripts Do

Each script:
1. Creates a new GameObject with the specified name
2. Adds MeshFilter and MeshRenderer components
3. Sets the appropriate primitive mesh from Unity's built-in resources
4. Assigns the default diffuse material
