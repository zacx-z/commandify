# Commandify Selectors

## SYNOPSIS
Selectors are used to identify and filter Unity objects in various commands. The general syntax is:
```
selector = <base-selector>[::type-specifier][#range-specifier]
```

## DESCRIPTION
Selectors are a powerful way to identify and filter Unity objects in the Editor. They support various formats for matching objects by path, type, hierarchy, tags, and more.

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
::type - Filter objects by Unity type (e.g., ::Material, ::Mesh)
```

### Range Specifiers
Range specifiers allow selecting specific items from the results:
```
#range = single_number | range_expression | multiple_ranges
- single_number: "0" selects first item
- range_expression: "0..5" selects items 0 through 5
- multiple_ranges: "0,2..5,7" combines individual selections
```

## EXAMPLES
```bash
# Basic selections
Assets/Prefabs/Player      # Select asset by path
^Canvas/Panel/Button      # Select object in hierarchy
$selected                # Reference last selected objects
@#Enemy                  # Select all objects with "Enemy" tag
@@t:material            # Search for materials using QuickSearch
^Player:Rigidbody       # Select Rigidbody component on Player object

# Type specifiers
Resources/unity_builtin_extra::Material  # Load all built-in materials
Assets/Models/*.fbx::Mesh               # Load meshes from all FBX files

# Range specifiers
^Enemy#0                # Select first enemy
^Cube#0..3             # Select first four cubes
^Item#0,2,4            # Select items at indices 0, 2, and 4
```

## SEE ALSO
- `list` - List objects matching a selector
- `select` - Select objects matching a selector
- `create` - Create objects using selectors for source/parent
- `component` - Manage components on selected objects
- `property` - Manage properties on selected objects
- `transform` - Transform operations on selected objects
