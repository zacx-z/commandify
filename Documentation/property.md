# Property Management Commands

## SYNOPSIS
```bash
property <command> <selector> [<args>]
```

## DESCRIPTION
Manage properties on Unity objects. Supports listing, getting, and setting property values.

### Commands

#### list
```bash
property list <selector>
```
Lists all available properties on the selected objects.

#### get
```bash
property get <selector> <property-name>
```
Gets the value of a specific property on selected objects.

#### set
```bash
property set <selector> <property-name> <value>
```
Sets the value of a specific property on selected objects.

## EXAMPLES
```bash
# List properties
property list ^DirectionalLight:Light                # List light properties
property list Assets/*.mat          # List material properties

# Get property values
property get ^MainCamera:Camera "field of view"    # Get camera FOV
property get ^DirectionalLight:Light m_Intensity      # Get light intensity

# Set property values
property set ^MainCamera:Camera "field of view" 90    # Set camera FOV to 90
property set ^DirectionalLight:Light m_Intensity 2        # Set light intensity to 2
property set ^Cube m_TagString Player        # Set cube's tag to "Player"
```

## SEE ALSO
- `selectors` - Selector syntax reference
- `component` - Component management commands
- `transform` - Transform operations
