# Commandify

A command-line interface for Unity Editor operations that allows you to control Unity through a TCP server.

## Installation

1. Open Unity Package Manager
2. Click the + button in the top-left corner
3. Select "Add package from disk"
4. Navigate to and select the `package.json` file in this folder

## Usage

1. Open the Commandify Server window from Window > Commandify > Server Settings
2. Configure the port if needed (default: 13999)
3. Click "Start Server" to begin listening for commands
4. Connect to the server using either:
   - The included interactive client: `./commandify-client.sh [--port PORT] [--host HOST]`
   - Any TCP client (e.g., netcat, telnet)

Example using the interactive client:
```bash
./commandify-client.sh -p 13999 -h localhost
```

Example using netcat:
```bash
echo "scene list --all" | nc localhost 13999
```

## Available Commands

### Scene Management
- `scene list [--opened | --all | --active]` - List scenes
- `scene open [--additive] <path>` - Open a scene
- `scene new <path> [<scene-template-name>]` - Create a new scene
- `scene save` - Save opened scenes
- `scene unload <scene-specifier>...` - Unload specified scenes
- `scene activate <scene-specifier>` - Set active scene

### GameObject Creation and Instantiation
- `create [--parent path/to/parent] [--with Component1,Component2,...] [--prefab prefab-selector] <name>` - Create a new GameObject
- `create [--parent path/to/parent] [--with Component1,Component2,...] [--prefab prefab-selector] <source-selector> <name>` - Create by duplicating an existing GameObject or instantiating a prefab
  - `name`: Name for the new GameObject (optional, defaults to source object name or "GameObject")
  - `source-selector`: Reference to source GameObject or prefab to duplicate/instantiate (optional)
  - `--parent`: Optional hierarchy path to parent the new object under
  - `--with`: Optional comma-separated list of components to add
  - `--prefab`: Optional prefab selector to specify which prefab to edit

Examples:
```bash
# Create basic GameObjects
create MyObject                                      # Create empty GameObject named "MyObject"
create Player --parent World                         # Create as child of "World" object
create Camera --with Camera,AudioListener            # Create with components
create Enemy --parent World/Enemies --with Rigidbody,BoxCollider,MeshRenderer # Create with parent and components

# Duplicate or instantiate from existing objects
create @&1234 EnemyClone                             # Duplicate GameObject with ID 1234
create Prefabs/Enemy.prefab NewEnemy                # Instantiate prefab
create Prefabs/UI/Button.prefab CustomButton --parent Canvas  # Instantiate prefab under Canvas

# Create inside prefabs
create Button --prefab UI/MenuPrefab                # Create GameObject at root of MenuPrefab
create Icon --prefab UI/ButtonPrefab --parent Panel --with Image  # Create GameObject under Panel in ButtonPrefab
create Prefabs/Icon.prefab NewIcon --prefab UI/ButtonPrefab --parent Panel  # Instantiate prefab inside another prefab
```

### Asset Operations
- `asset search [--folder folders] [--format format] <query>` - Search for assets using FindAssets
  - `--folder`: Single folder or array of folders to search in (e.g., `--folder [Assets/Prefabs, Assets/Materials]`)
  - `--format`: Output format (`path` for full paths, `instance-id` for instance IDs)
- `asset create <type> <path>` - Create a new asset
- `asset move <path> <new-path>` - Move/rename asset
- `asset create-types` - List available asset types
- `asset thumbnail <selector>` - Get base64 PNG thumbnails for selected assets

### Prefab Operations
- `prefab create [--variant] <selector> <path>` - Create prefab or prefab variant

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
  - `list <selector>` - List components on objects
  - `add <selector> <type>` - Add component to objects
  - `search <pattern> [--base <type>]` - Search for component types by name pattern
    - `pattern`: Wildcard pattern matching component type names (including namespace)
    - `--base`: Optional base type to filter results (e.g., Collider, UI.Selectable)
- `transform <command> <selector> [<args>]` - Transform operations

Examples:
```bash
# Search for components
component search "*"                      # List all components
component search "UnityEngine.UI.*"       # List all UI components
component search "*Collider*"             # List all collider-related components
component search "*" --base Collider      # List all components inheriting from Collider
```

### Transform Operations
- `transform translate <selector> [<x> <y> <z>]` - Translate objects or show current positions
- `transform rotate <selector> [<x> <y> <z>]` - Rotate objects or show current rotations
- `transform scale <selector> [<x> <y> <z>]` - Scale objects or show current scales
- `transform parent <parent-selector> <child-selector>...` - Parent objects
- `transform show <selector>` - Show complete transform information including position, rotation, scale, and parent

Examples:
```bash
# Show current transform values
transform show ^**             # Show complete transform info of everything in the scene
transform translate ^Cube         # Show positions
transform rotate ^Cube              # Show rotations
transform scale ^Cube           # Show scales

# Apply transformations
transform translate ^Cube 0 1 0   # Move up by 1 unit
transform rotate ^Cube 0 90 0     # Rotate 90 degrees around Y
transform scale ^Cube 2 2 2      # Double the size
```

### Variables
- `set [--add | --sub] $<varname> <selector>` - Set variables
- Built-in variable `$~` stores last command result (alternative output form)

### Editor Operations
- `package list` - List installed packages
- `package install <package-name>` - Install package
- `exec <menu-path>` - Execute menu item
- `exec --search [pattern]` - Search menu items by pattern
- `run <script-path> [<options>]` - Execute a commandify script file

Examples:
```bash
# Execute menu items
exec "Window/Commandify/Server Settings"  # Open server settings window
exec "Assets/Create/Material"             # Create new material

# Search menu items
exec --search window     # Search menu items containing "window"
exec --search "Assets/"  # Search menu items under Assets menu

# Run commandify scripts
run scripts/create-cube.sh MyCube     # Create a cube primitive
run scripts/create-sphere.sh Ball     # Create a sphere primitive
```

## Selector Grammar

A selector is used to identify and select objects in the Unity scene or project. The grammar is structured as follows:

### Basic Structure
```
selector = <base-selector>[::<type-specifier>][#<range-specifier>]
```

### Base Selectors
- `path` - Direct asset path (e.g., `Assets/Prefabs/Player`)
- `^path` - Hierarchy path starting from root (e.g., `^Canvas/Panel/Button`)
- `$varname` - Variable reference (e.g., `$selected`)
- `@#tag` - Select by tag (e.g., `@#Player`)
- `@@search` - QuickSearch query (e.g., `@@t:material`)
- `@&instance-id` - Select by instance ID
- `base-selector:component-type` - Filter by component (e.g., `^Player:Rigidbody`)

### Type Specifiers
Type specifiers allow filtering results by type and loading all assets at a path:
```
Assets/MyMaterial.mat::Material  # Load all assets at path and filter for Material type
Assets/Textures/*::Texture      # Load all textures from directory
```

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
Resources/unity_builtin_extra::Material # Load all built-in assets at path and filter for Material type
^UI/MainMenu/PlayButton   # Select object in hierarchy
@#Enemy                   # Select all with "Enemy" tag
@@o:asteroid             # QuickSearch for asteroid objects

# Component filtering
^Player:Rigidbody        # Select Player's Rigidbody
^World/*:MeshRenderer    # Filter selected objects by MeshRenderer
$~:Transform            # Filter last command result by Transform

# Range selection
^Enemies#0              # Select first enemy
^Enemies#0..3          # Select first four enemies
^Items#0,2,4          # Select items at index 0, 2, and 4

# Combined selections
^Player:Rigidbody & @@t:physics  # Intersection of selections
@#Enemy | @#Boss               # Select all enemies and bosses
