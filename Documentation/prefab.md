# Prefab Command

## SYNOPSIS
```bash
prefab create [--variant] <selector> <path>
```

## DESCRIPTION
Create prefab assets from GameObjects in the scene. Supports creating both standard prefabs and prefab variants.

### Commands

#### create
Creates a new prefab asset from a GameObject.

Options:
- `--variant` - Create as a prefab variant instead of a standard prefab

## EXAMPLES
```bash
# Create standard prefabs
prefab create ^Player Assets/Prefabs/Player           # Create from player object
prefab create ^UI/Panel Assets/Prefabs/Panel          # Create from UI panel
prefab create ^Level/Boss Assets/Prefabs/Enemies/Boss # Create from boss object

# Create prefab variants
prefab create --variant ^CustomEnemy Assets/Prefabs/EnemyVariant    # Create enemy variant
prefab create --variant ^ModifiedUI Assets/Prefabs/CustomPanel      # Create UI variant
```

## SEE ALSO
- `create` - Create GameObjects
- `selectors` - Selector syntax reference
- `list` - List objects and their properties
