# Commandify

A command-line interface for Unity Editor operations that allows you to control Unity through a TCP server.

## Installation

1. Open Unity Package Manager
2. Click the + button in the top-left corner
3. Select "Add package from disk"
4. Navigate to and select the `package.json` file in this folder

## Usage

1. Open the Commandify Server window from Window > Commandify > Server Settings
2. Configure the port if needed (default: 12345)
3. Click "Start Server" to begin listening for commands
4. Connect to the server using either:
   - The included interactive client: `./commandify-client.sh [--port PORT] [--host HOST]`
   - Any TCP client (e.g., netcat, telnet)

Example using the interactive client:
```bash
./commandify-client.sh -p 12345 -h localhost
```

Example using netcat:
```bash
echo "scene list --all" | nc localhost 12345
```

## Available Commands

### Scene Management
- `scene list [--opened | --all | --active]` - List scenes
- `scene open [--additive] <path>` - Open a scene
- `scene new [<scene-template-name>]` - Create a new scene
- `scene save` - Save opened scenes
- `scene unload <scene-specifier>...` - Unload specified scenes
- `scene activate <scene-specifier>` - Set active scene

### Asset Operations
- `asset list [--filter <filterspec> | --recursive] <path>` - List assets
- `asset create <type> <path>` - Create a new asset
- `asset move <path> <new-path>` - Move/rename asset
- `asset create-types` - List available asset types

### Prefab Operations
- `prefab instantiate <hierarchy-path>` - Instantiate prefab
- `prefab create [--variant] <selector> <path>` - Create prefab

### View Operations
- `list [--filter <filterspec>] [--path] <selector>` - List selected objects

### Edit Operations
- `select [--add] [--children] <selector>` - Select objects
- `property <command> <selector> [<args>]` - Manage object properties
- `component <command> <selector> [<args>]` - Manage components
- `transform <command> <selector> [<args>]` - Transform operations

### Transform Operations
- `transform translate <selector> [<x> <y> <z>]` - Translate objects or show current positions
- `transform rotate <selector> [<x> <y> <z>]` - Rotate objects or show current rotations
- `transform scale <selector> [<x> <y> <z>]` - Scale objects or show current scales
- `transform parent <parent-selector> <child-selector>...` - Parent objects

Examples:
```bash
# Show current transform values
transform translate selected         # Show positions
transform rotate ^Cube              # Show rotations
transform scale selected:Renderer   # Show scales

# Apply transformations
transform translate selected 0 1 0   # Move up by 1 unit
transform rotate selected 0 90 0     # Rotate 90 degrees around Y
transform scale selected 2 2 2      # Double the size
```

### Variables
- `set [--add | --sub] $<varname> <selector>` - Set variables
- Built-in variable `$?` stores last command result

### Editor Operations
- `package list` - List installed packages
- `package install <package-name>` - Install package
- `settings set/get <setting-path> <value>` - Manage settings
- `exec <menu-path>` - Execute menu item
- `exec --search [pattern]` - Search menu items by pattern

Examples:
```bash
# Execute menu items
exec "Window/Commandify/Server Settings"  # Open server settings window
exec "Assets/Create/Material"             # Create new material

# Search menu items
exec --search window     # Search menu items containing "window"
exec --search "Assets/"  # Search menu items under Assets menu
```

## Selector Grammar
```
selector:
    primary-selector#range-specifier

primary-selector:
    path-notation
    ^path-notation
    $varname
    @#tag
    @@search
    primary-selector:component-type
    selector & selector
    selector | selector

path-notation:
    path-notation/name-literal
    path-notation/*
    name-literal

range-specifier:
    integer
    integer..integer
    range-specifier,range-specifier
```

## Examples
- Find asset: `Assets/path/to/asset`
- Find in hierarchy: `^root/parent/child`
- Find by tag: `@#MainCamera`
- QuickSearch: `@@o: astroid`
- First element: `<selector>#0`
- Range selection: `<selector>#0..10`
- Component filter: `<selector>:SpriteRenderer`
- Intersection: `<selector>&<selector>`
- Union: `<selector>|<selector>`
