# Settings Management Commands

## SYNOPSIS
```bash
settings <command> [<args>]
```

## DESCRIPTION
Manage Unity Editor and Player settings.

### Commands

#### list
```bash
settings list
```
Lists all available settings and their types.

#### get
```bash
settings get <setting-name>
```
Gets the value of a specific setting.

#### set
```bash
settings set <setting-name> <value>
```
Sets the value of a specific setting.

### Available Settings

EditorPrefs:
- `autoRefresh` (bool)
- `companyName` (string)
- `productName` (string)
- `scriptingRuntimeVersion` (string)
- `selectedColorSpace` (string)

PlayerSettings:
- `bundleIdentifier` (string)
- `bundleVersion` (string)
- `defaultScreenWidth` (int)
- `defaultScreenHeight` (int)
- `fullScreenMode` (bool)
- `runInBackground` (bool)
- `defaultIsFullScreen` (bool)
- `captureSingleScreen` (bool)
- `usePlayerLog` (bool)
- `resizableWindow` (bool)
- `allowFullscreenSwitch` (bool)
- `visibleInBackground` (bool)
- `macRetinaSupport` (bool)
- `defaultWebScreenWidth` (int)
- `defaultWebScreenHeight` (int)
- `scriptingBackend` (string)
- `apiCompatibilityLevel` (string)

## EXAMPLES
```bash
# List all settings
settings list

# Get setting values
settings get productName
settings get scriptingBackend

# Set setting values
settings set companyName "My Company"
settings set defaultScreenWidth 1920
settings set runInBackground true
```

## SEE ALSO
- `exec` - Execute Unity menu items
- `property` - Property management commands
