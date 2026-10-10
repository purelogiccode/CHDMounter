# Usage

## GUI

The main window exposes:

| Control | Purpose |
|---------|---------|
| **Browse** | Select a `.chd` file |
| **Console type** | Choose the filesystem/format parser |
| **Mount point** | Optional drive letter, e.g. `M:`. Leave empty to auto-pick |
| **Mount** | Mount the image as a read-only virtual drive |
| **Unmount** | Remove the virtual drive |
| **Settings** | Auto-open drive, drive-letter preference and more |
| **About** | Version information and third-party credits |
| **Open AppData Folder** | Opens `%LOCALAPPDATA%\CHDMounter` (logs, settings, screenshots) |
| **Log** | Real-time mount/parse log output |

### Mount options

- **Auto-open drive** — launch File Explorer at the mounted drive when the mount succeeds.
- **Drive letter** — a preferred drive letter for automatic mounts.

Settings are encrypted with Windows DPAPI and persisted between sessions.

## CLI via the GUI build

The GUI executable is also a CLI application:

```text
CHDMounter.exe [/l] [/a] [/s:<alias>] <chd_file> [mount_point]
```

See [Command Line](command-line.md) for all flags and examples.

## Read-only by design

CHDMounter never writes to your CHD files. The mounted volume reports a read-only filesystem and
the image is opened in read-only mode.

## Logs

Structured Serilog logs are written to `%LOCALAPPDATA%\CHDMounter\Logs`. The log pane in the UI
shows the same messages. When reporting an issue, include the newest log file.
