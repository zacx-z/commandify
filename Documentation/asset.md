# Asset Commands

Commands for managing Unity assets.

## Commands

### `asset search <query>`
Searches for assets in the project.
- `<query>`: Search term to find matching assets

### `asset thumbnail <selector>`
Generates thumbnail previews for selected assets.
- `<selector>`: Asset selector (see selectors.md for syntax)

## Examples

```bash
# Search for materials
asset search --format path t:Material

# Get thumbnails for selected textures
asset thumbnail Assets/Textures/*
```

## See Also
- [Selectors Documentation](selectors.md)
- [Unity Built-in Assets](unity-builtin-assets.md)
- [Prefab Commands](prefab.md)
- [Create Commands](create.md)
