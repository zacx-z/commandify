# Transform Operations

## SYNOPSIS
```bash
transform <command> <selector> [<args>]
```

## DESCRIPTION
Perform transform operations on Unity GameObjects. Supports translation, rotation, scaling, and parenting.

### Commands

#### translate
```bash
transform translate <selector> [(<x>,<y>,<z>) | <x> <y> <z>]
```
Translate objects or show current positions. Without coordinates, shows current positions. Supports both vector format `(x,y,z)` and separate arguments.

#### rotate
```bash
transform rotate <selector> [(<x>,<y>,<z>) | <x> <y> <z>]
```
Rotate objects or show current rotations. Without angles, shows current rotations. Supports both vector format `(x,y,z)` and separate arguments.

#### scale
```bash
transform scale <selector> [(<x>,<y>,<z>) | <x> <y> <z>]
```
Scale objects or show current scales. Without values, shows current scales. Supports both vector format `(x,y,z)` and separate arguments.

#### parent
```bash
transform parent <parent-selector> <child-selector>...
```
Parent objects to a target parent transform.

## EXAMPLES
```bash
# View transform information
transform translate ^Player          # Show player position
transform rotate ^Camera            # Show camera rotation
transform scale ^UI/Panel          # Show panel scale

# Modify transforms
transform translate ^Cube (0,2,0)    # Move cube up by 2 units using vector format
transform translate ^Cube 0 2 0      # Move cube up by 2 units using separate arguments
transform rotate ^Player (0,90,0)   # Rotate player 90 degrees Y using vector format
transform scale ^UI/Panel (2,2,1)   # Scale panel x2 on X and Y using vector format

# Parent objects
transform parent ^Level/Section1 ^Enemy* # Parent all enemies to Section1
```

## SEE ALSO
- `selectors` - Selector syntax reference
- `list` - List objects and their properties
- `select` - Select objects in the editor
