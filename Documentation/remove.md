# Remove Command

## SYNOPSIS
```bash
remove <selector>
```

## DESCRIPTION
Removes GameObjects from the scene or from a prefab. Can remove individual objects, objects matching a pattern, or objects by tag.

### Arguments
- `selector` - A selector to find objects to remove. See [selector syntax](selectors.md) for details.
  - Name pattern: `^MyObject`, `^Temp*`
  - Tag: `@#Enemy`, `@#Pickup`
  - Path: `^Parent/Child/*`

### Notes
- Objects are removed immediately from the scene/prefab
- Only GameObjects can be removed
- The operation can be undone using the `undo` command
- If no objects match the selector, a message will be displayed

## EXAMPLES
```bash
# Remove by name
remove ^Player                  # Remove specific object
remove ^Enemy*                  # Remove objects matching pattern

# Remove by tag
remove @#Pickup                 # Remove all pickups
remove @#Enemy                  # Remove all enemies

# Remove by path
remove ^Level/Enemies/*         # Remove all under path
remove ^UI/HUD/*               # Remove all HUD elements
```

## SEE ALSO
- `component remove` - Remove specific components
- `selectors` - Selector syntax reference
- `undo` - Undo operations
- `list` - List objects
