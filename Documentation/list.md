# List Command

## SYNOPSIS
```bash
list <selector> [--components] [--format <format>] [--filter <pattern>]
```

## DESCRIPTION
Lists objects matching the given selector. Can show components and format output in different ways.

### Options
- `--components` - Show components attached to GameObjects
- `--format <format>` - Output format:
  - `path` - Show full path to object
  - `name` - Show only object name (default)
- `--filter <pattern>` - Filter results by name pattern (supports wildcards)

## EXAMPLES
```bash
# Basic listing
list ^Canvas/Panel/*          # List all objects under Panel
list Assets/Prefabs/*        # List all prefabs in directory

# Show components
list ^Cube --components      # List Cube with its components
list ^Enemy* --components   # List all enemies and their components

# Format output
list Assets/* --format path  # List assets with full paths
list ^UI/* --format name    # List UI objects by name only

# Filter results
list ^Enemy* --filter "*Boss*"    # List enemies with "Boss" in name
list Assets/* --filter "*.prefab"  # List only prefab assets
```

## SEE ALSO
- `select` - Select objects in the editor
- `selectors` - Selector syntax reference
- `component list` - List components on objects
