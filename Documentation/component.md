# Component Management Commands

## SYNOPSIS
```bash
component <command> <selector> [<args>]
```

## DESCRIPTION
Manage components on Unity GameObjects. Supports listing, adding, and searching for components.

### Commands

#### list
```bash
component list <selector> [--format <format>]
```
Lists components attached to selected objects.

Options:
- `--format <format>` - Output format (path, name)

#### add
```bash
component add <selector> <type>
```
Adds a component of the specified type to selected objects.

#### search
```bash
component search <pattern> [--base <type>]
```
Search for component types by name pattern.

Options:
- `pattern` - Wildcard pattern matching component type names (including namespace)
- `--base <type>` - Optional base type to filter results (e.g., Collider, UI.Selectable)

#### remove
```bash
component remove <selector> <component-name>[,component-name,...]
```
Removes specified components from selected objects.

Options:
- `selector` - Object selector to find target objects
- `component-name` - Name of component(s) to remove, comma-separated for multiple

## EXAMPLES
```bash
# List components
component list ^Player
component list ^UI/* --format path

# Add components
component add ^Player BoxCollider
component add ^Enemy* NavMeshAgent

# Search for components
component search "UI.*"
component search "*" --base Collider

# Remove components
component remove ^Player BoxCollider
component remove ^UI/* Image,Button
component remove ^Enemy* NavMeshAgent,AudioSource
```

## SEE ALSO
- `selectors` - Selector syntax reference
- `property` - Property management commands
- `transform` - Transform operations
