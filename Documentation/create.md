# Create Command

## SYNOPSIS
```bash
create <name> [--parent <path>] [--with <components>] [--prefab <selector>]
create <source-selector> <name> [--parent <path>] [--with <components>] [--prefab <selector>]
```

## DESCRIPTION
Creates new GameObjects in the scene or in a prefab. Can create empty objects, duplicate existing objects, or instantiate from prefabs.

### Options
- `--parent <path>` - Parent path for the new object
- `--with <components>` - Comma-separated list of components to add
- `--prefab <selector>` - Create in prefab mode using the selected prefab

### Primitive Creation Scripts
The following scripts are available in `commandify/scripts/` for creating Unity primitive objects:
- `create-cube.sh` - Create a cube
- `create-sphere.sh` - Create a sphere
- `create-capsule.sh` - Create a capsule
- `create-cylinder.sh` - Create a cylinder
- `create-plane.sh` - Create a plane
- `create-quad.sh` - Create a quad

These scripts automatically configure the objects with appropriate MeshFilter and MeshRenderer components. They use the variable system (`$~` and `$obj`) for object referencing.

## EXAMPLES
```bash
# Create basic objects
create Player                                    # Create empty GameObject
create Enemy --parent ^Level/Section1            # Create under parent
create Item --with Rigidbody,BoxCollider        # Create with components

# Create from source
create ^Cube NewCube                            # Duplicate cube
create Assets/Prefabs/Enemy.prefab Boss         # Instantiate from prefab

# Create in prefab
create UI --parent ^Canvas --prefab $uiPrefab   # Create in UI prefab

# Using macros
create-cube MyCube                             # Create a cube
create-sphere MySphere                         # Create a sphere
```

## SEE ALSO
- `prefab` - Prefab management commands
- `selectors` - Selector syntax reference
- `component` - Component management commands
- `Documentation/macros.md` - Detailed macro documentation
