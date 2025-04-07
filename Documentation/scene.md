# Scene Commands

Commands for managing Unity scenes.

## Commands

### `scene list [--opened | --all | --active]`
Lists scenes based on the specified flag:
- `--opened`: Lists currently opened scenes
- `--all`: Lists all scenes in the project
- `--active`: Shows the currently active scene
If no flag is specified, lists opened scenes.

### `scene open [--additive] <path>`
Opens a scene at the specified path.
- `--additive`: Opens the scene additively (without closing current scenes)
- `<path>`: Path to the scene file (relative to Assets folder)

### `scene new [<scene-template-name>]`
Creates a new scene.
- `<scene-template-name>`: Optional template to use for the new scene

### `scene save`
Saves all currently opened scenes.

### `scene unload <scene-specifier>...`
Unloads one or more scenes.
- `<scene-specifier>`: Name, path, or index of scene(s) to unload

### `scene activate <scene-specifier>`
Sets the specified scene as the active scene.
- `<scene-specifier>`: Name, path, or index of the scene to activate

## Examples

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
