# Command Line

```text
CHDMounter.exe [/l] [/a] [/s:<alias>] <chd_file> [mount_point]
```

| Argument | Description |
|----------|-------------|
| `/l` | Open the mounted drive in the file manager (Explorer/Finder/xdg-open) |
| `/a` | Auto-select a drive letter |
| `/s:<alias>` | Select the console/format by alias (e.g. `ps2`, `cuebin2352`) |
| `<chd_file>` | Path to the `.chd` file (required) |
| `[mount_point]` | Drive letter or folder (optional; auto-picks if omitted) |

**Notes**

- Arguments can be in any order.
- Flags are prefixed with `/`.
- A console alias may also be passed as a positional argument: `CHDMounter.exe game.chd ps2`.
- On Linux and macOS the executable is named `CHDMounter` (no `.exe` suffix).
- If no console type is given, a dialog appears asking you to choose.
- Numeric console indexes are not supported — use aliases (see [Console Types](console-types.md)).

## Examples

```bat
:: Mount a PS2 game as drive M:
CHDMounter.exe /s:ps2 game.chd M:

:: Mount an Xbox 360 game, auto-select drive letter
CHDMounter.exe game.chd xbox360

:: Mount a Dreamcast game and open Explorer
CHDMounter.exe /l /s:segadreamcast game.chd

:: Mount as a virtual CUE/BIN image (2352-byte sectors)
CHDMounter.exe /s:cuebin2352 disc.chd

:: Mount with the generic ISO 9660 parser
CHDMounter.exe /s:iso9660 data.chd N:

:: Mount a PS3 image as a single image.iso file (for RPCS3)
CHDMounter.exe /s:isoraw2352 game.chd

:: Console alias as a positional argument
CHDMounter.exe game.chd ps3
```

## Auto-open after mount

With `/l`, CHDMounter opens the file manager at the mount point as soon as the drive is ready.
The same behaviour can be enabled permanently in **Settings** (*Auto-open mounted drive*).
