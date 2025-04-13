# Asset Management Commands

## SYNOPSIS

```bash
asset <command> [<args>]
```

## DESCRIPTION

Manage Unity assets in the project. Supports searching and generating thumbnails for assets.

### Commands

#### create

```bash
asset create [--overwrite] [<type>] <path> [--uri <uri>]
```

Creates a new asset in the project. If no type is specified, it will be inferred from the file extension or URI content.

Options:
- `<type>` - Optional type of asset to create (e.g., TextAsset, Texture2D, ScriptableObject)
- `<path>` - Path where the asset should be created
- `--uri <uri>` - URI to fetch content from. Supports:
  - HTTP(S) URLs (e.g., `https://example.com/file.txt`)
  - Data URIs (e.g., `data:text/plain,hello%20world`)
  - File URIs (e.g., `file:///Users/nickname/Downloads/logo.png`)
- `--overwrite` - Allow overwriting existing assets

#### search

```bash
asset search [--format <format>] <query>
```

Searches for assets in the project.

Options:

- `--format <format>` - Output format (path, name)
- `<query>` - Search term to find matching assets

#### mkdir

```bash
asset mkdir <path>
```

Creates a new directory in the project.

Options:

- `<path>` - Path where the directory should be created

#### move

```bash
asset move <path> <dest-path>
```

Moves an asset or directory to a new location. Can also be used to rename assets.

Options:

- `<path>` - Source path of the asset or directory to move
- `<dest-path>` - Destination path. For directories, if this ends with a slash, the original directory name will be used

#### delete/rm

```bash
asset delete <path> [<path2> ...]
asset rm <path> [<path2> ...]
```

Deletes one or more assets or directories from the project.

Options:

- `<path>` - Path to the asset or directory to delete. Multiple paths can be specified.

#### duplicate/cp

```bash
asset duplicate <path> <target-path> [<path2> <target-path2> ...]
asset cp <path> <target-path> [<path2> <target-path2> ...]
```

Duplicates one or more assets or directories to new locations.

Options:

- `<path>` - Source path of the asset or directory to duplicate
- `<target-path>` - Destination path. For directories, if this ends with a slash, the original directory name will be used
- Additional source-target pairs can be specified to duplicate multiple items at once

#### thumbnail

```bash
asset thumbnail <selector>
```

Generates thumbnail previews for selected assets.

Options:

- `<selector>` - Asset selector (see selectors.md for syntax)

#### read

```bash
asset read [--encode <encoding>] [--truncate <max-length>] <path>
```

Reads and outputs the contents of an asset file.

Options:

- `--encode <encoding>` - Output encoding (utf-8, base64, hex). Defaults to utf-8
- `--truncate <max-length>` - Maximum number of characters to output
- `<path>` - Path to the asset file to read

## EXAMPLES

```bash
# Search for materials
asset search --format path t:Material

# Create assets from URLs
asset create Assets/Content/readme.md --uri https://raw.githubusercontent.com/example/readme.md
asset create Assets/Textures/logo.png --uri https://example.com/logo.png --overwrite

# Create assets with inline data
asset create Assets/data.txt --uri "data:text/plain,hello world"

# Create typed assets
asset create TextAsset Assets/empty.txt
asset create GameSettings Assets/settings.asset

# Create a new directory
asset mkdir Assets/NewFolder

# Delete multiple items
asset delete Assets/OldFolder Assets/unused.mat

# Duplicate assets
asset cp Assets/Textures/logo.png Assets/Images/logo2.png
asset duplicate Assets/Models/ Assets/Models_Backup/

# Move a texture to a new location
asset move Assets/Textures/logo.png Assets/Images/logo.png

# Move and rename a directory
asset move Assets/Textures Assets/Images/NewTextures

# Get thumbnails for selected textures
asset thumbnail Assets/Textures/*

# Read a text file
asset read Assets/Content/config.txt

# Read a binary file as base64
asset read --encode base64 Assets/Textures/icon.png

# Read first 100 bytes of a file as hex
asset read --encode hex --truncate 100 Assets/Data/binary.dat
```

## SEE ALSO

- `selectors` - Selector syntax reference
- `unity-builtin-assets` - Unity built-in asset reference
- `prefab` - Prefab management commands
- `create` - Asset creation commands
