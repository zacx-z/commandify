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
```

## SEE ALSO
- `selectors` - Selector syntax reference
- `property` - Property management commands
- `transform` - Transform operations
