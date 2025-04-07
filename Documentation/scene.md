# Scene Management Commands

## SYNOPSIS
```bash
scene <command> [<args>]
```

## DESCRIPTION
Manage Unity scenes. Supports listing, opening, creating, saving, unloading and activating scenes.

### Commands

#### list
```bash
scene list [--opened | --all | --active]
```
Lists scenes based on the specified flag.

Options:
- `--opened` - Lists currently opened scenes (default)
- `--all` - Lists all scenes in the project
- `--active` - Shows the currently active scene

#### open
```bash
scene open [--additive] <path>
```
Opens a scene at the specified path.

Options:
- `--additive` - Opens the scene additively (without closing current scenes)
- `<path>` - Path to the scene file (relative to Assets folder)

#### new
```bash
scene new [<scene-template-name>]
```
Creates a new scene.

Options:
- `<scene-template-name>` - Optional template to use for the new scene

#### save
```bash
scene save
```
Saves all currently opened scenes.

#### unload
```bash
scene unload <scene-specifier>...
```
Unloads one or more scenes.

Options:
- `<scene-specifier>` - Name, path, or index of scene(s) to unload

#### activate
```bash
scene activate <scene-specifier>
```
Sets the specified scene as the active scene.

Options:
- `<scene-specifier>` - Name, path, or index of the scene to activate

## EXAMPLES
```bash
# List all scenes in project
scene list --all

# Open a scene additively
scene open --additive Assets/Scenes/Level1.unity

# Create new scene
scene new

# Save current scenes
scene save

# Unload multiple scenes
scene unload Level1 Level2

# Activate a specific scene
scene activate Level1
```

## SEE ALSO
- `create` - Scene creation commands
- `property` - Property management commands
- `transform` - Transform operations
