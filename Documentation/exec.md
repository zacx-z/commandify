# Execute Menu Commands

## SYNOPSIS
```bash
exec <menu-path>
exec --search [pattern]
exec --help
```

## DESCRIPTION
Execute Unity Editor menu items or search available menu items.

### Commands

#### exec <menu-path>
Execute a specific menu item by its path.

#### exec --search [pattern]
Search available menu items. If pattern is provided, only shows matching items.

#### exec --help
Show help information about the exec command.

## EXAMPLES
```bash
# Execute menu items
exec Edit/Play                   # Toggle play mode
exec Window/General/Game         # Show game view
exec Assets/Create/Material      # Create new material

# Search menu items
exec --search                   # List all menu items
exec --search "build"           # Search items containing "build"
exec --search "window/layout"   # Search layout menu items
```

## SEE ALSO
- `settings` - Manage Unity settings
- `list` - List objects and their properties
