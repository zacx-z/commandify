# Select Command

## SYNOPSIS
```bash
select <selector> [--add] [--children]
```

## DESCRIPTION
Selects objects matching the given selector in the Unity Editor. The selected objects can be referenced later using the `$selected` variable.

### Options
- `--add` - Add to current selection instead of replacing it
- `--children` - Include children of selected objects in the selection

## EXAMPLES
```bash
# Basic selection
select Assets/Prefabs/Player     # Select player prefab
select ^UI/MainMenu             # Select main menu in hierarchy
select @#Enemy                  # Select all objects with Enemy tag

# Add to selection
select ^Enemy --add            # Add enemies to current selection
select ^Item* --add           # Add all items to selection

# Select with children
select ^Parent --children     # Select parent and all children
select ^Level/* --children   # Select level objects and children

# Combined options
select ^Boss --add --children  # Add boss and children to selection

# Using with variables
select $~           # Select last created object
```

## SEE ALSO
- `list` - List objects and their properties
- `selectors` - Selector syntax reference
- `property` - Modify properties of selected objects
