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
- `list [--filter <filterspec>] [--format <format>] [--components] <selector>` - List selected objects
  - `--format`: Output format (default: name only)
    - `instance-id`: Output as `@&instance-id` format
    - `path`: Output full hierarchy paths
    - `full`: Output paths with components
  - `--components`: Include component types in output
  - `--filter`: Filter objects by name pattern

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

A selector is used to identify and select objects in the Unity scene or project. The grammar is structured as follows:

### Basic Structure
```
selector = <base-selector> [#<range-specifier>]
```

### Base Selectors
- `path` - Direct asset path (e.g., `Assets/Prefabs/Player`)
- `^path` - Hierarchy path starting from root (e.g., `^Canvas/Panel/Button`)
- `$varname` - Variable reference (e.g., `$selected`)
- `@#tag` - Select by tag (e.g., `@#Player`)
- `@@search` - QuickSearch query (e.g., `@@t:material`)
- `@&instance-id` - Select by instance ID
- `base-selector:component-type` - Filter by component (e.g., `^Player:Rigidbody`)

### Combining Selectors
- `selector & selector` - Intersection (objects matching both selectors)
- `selector | selector` - Union (objects matching either selector)

### Range Specifiers
Range specifiers allow selecting specific items from the results:
```
range = single_number | range_expression | multiple_ranges
- single_number: "0" selects first item
- range_expression: "0..5" selects items 0 through 5
- multiple_ranges: "0,2..5,7" combines individual selections
```

### Examples
```bash
# Basic selections
Assets/Prefabs/Player      # Select asset by path
^UI/MainMenu/PlayButton   # Select object in hierarchy
@#Enemy                   # Select all with "Enemy" tag
@@o:asteroid             # QuickSearch for asteroid objects

# Component filtering
^Player:Rigidbody        # Select Player's Rigidbody
selected:MeshRenderer    # Filter selected objects by MeshRenderer

# Range selection
^Enemies#0              # Select first enemy
^Enemies#0..3          # Select first four enemies
^Items#0,2,4          # Select items at index 0, 2, and 4

# Combined selections
^Player:Rigidbody & @@t:physics  # Intersection of selections
@#Enemy | @#Boss               # Select all enemies and bosses
