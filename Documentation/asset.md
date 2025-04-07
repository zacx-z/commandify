# Asset Management Commands

## SYNOPSIS
```bash
asset <command> [<args>]
```

## DESCRIPTION
Manage Unity assets in the project. Supports searching and generating thumbnails for assets.

### Commands

#### search
```bash
asset search [--format <format>] <query>
```
Searches for assets in the project.

Options:
- `--format <format>` - Output format (path, name)
- `<query>` - Search term to find matching assets

#### thumbnail
```bash
asset thumbnail <selector>
```
Generates thumbnail previews for selected assets.

Options:
- `<selector>` - Asset selector (see selectors.md for syntax)

## EXAMPLES
```bash
# Search for materials
asset search --format path t:Material

# Get thumbnails for selected textures
asset thumbnail Assets/Textures/*
```

## SEE ALSO
- `selectors` - Selector syntax reference
- `unity-builtin-assets` - Unity built-in asset reference
- `prefab` - Prefab management commands
- `create` - Asset creation commands
