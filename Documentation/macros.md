# Unity Primitive Creation Macros

This documentation describes how to use macros in the commandify system to create basic 3D primitives in Unity. Macros replace the previous script-based approach and are fully compatible with the C# server implementation.

## Available Macros

- `create-cube` - Creates a cube primitive
- `create-sphere` - Creates a sphere primitive
- `create-capsule` - Creates a capsule primitive
- `create-cylinder` - Creates a cylinder primitive
- `create-plane` - Creates a plane primitive
- `create-quad` - Creates a quad primitive

## Usage

All macros follow the same usage pattern:

```bash
create-<primitive> <object-name> [parameter=value ...]
```

For example:
```bash
create-cube MyCube
create-sphere GameBall
create-plane Ground pos=(0,-1,0) size=(10,1,10)
```

## Getting Help for Macros

You can get detailed help for any macro by using the `help` command:

```bash
help create-cube
```

This will display the documentation comments from the top of the macro file.

## Parameters

Macros support both positional and named parameters:

- Positional: The first parameter is typically the object name
- Named: Additional parameters can be specified with name=value syntax

Common parameters include:
- `pos` or `position` - The position of the object (Vector3)
- `rotation` - The rotation of the object (Vector3 in Euler angles)
- `size` or `scale` - The scale of the object (Vector3)

## Creating Custom Macros

You can create your own macros by adding `.macro` files to the `Packages/com.nelasystem.commandify/Macros` directory. Macro files contain a series of commandify commands that will be executed in sequence.

### Adding Documentation to Macros

To add documentation to your macro, include comment lines at the top of the file that start with `#`. These comments will be displayed when users run the `help` command for your macro.

Example macro file (create-cube.macro):
```
# Creates a cube primitive with the specified name
# 
# Usage: create-cube <name> [pos=(x,y,z)] [rotation=(x,y,z)] [size=(x,y,z)]
#
# Parameters:
#   name - The name of the cube object (required)
#   pos - Position vector (optional, default: current position)
#   rotation - Rotation in Euler angles (optional, default: no rotation)
#   size - Scale vector (optional, default: (1,1,1))
#
create $1 --with MeshFilter,MeshRenderer
set obj $~
property set $obj:MeshFilter m_Mesh BuiltinResources/Cube.fbx::Mesh
property set $obj:MeshRenderer m_Materials [BuiltinExtra/Default-Diffuse.mat::Material]
transform translate $obj $pos
transform rotate $obj $rotation
transform scale $obj $size
```

### Variables in Macros

Macros can use variables:
- `$1`, `$2`, etc. - Positional arguments
- `$name` - Named arguments (when using name=value syntax)
- `$~` - The result of the previous command

## SEE ALSO
- `create` - Object creation commands
- `property` - Property management commands
- `transform` - Transform manipulation commands
