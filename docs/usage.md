# Usage

## GUI

The main window exposes:

| Control | Purpose |
|---------|---------|
| **Browse** | Select a `.chd` file |
| **Console type** | Choose the filesystem/format parser |
| **Mount point** | Optional drive letter (Windows) or folder (Linux/macOS). Leave empty to auto-pick |
| **Mount** | Mount the image as a read-only virtual drive |
| **Unmount** | Remove the virtual drive |
| **Settings** | Auto-open the mounted drive and select the mount core (Dokan/WinFsp/FUSE) |
| **About** | Version information and third-party credits |
| **Open AppData Folder** | Opens `%LOCALAPPDATA%\CHDMounter` (logs, settings, screenshots) |
| **Log** | Real-time mount/parse log output |

### Mount options

- **Auto-open drive** — open the mounted drive in your file manager when the mount succeeds.
- **Mount core** — choose **Dokan** (default) or **WinFsp** on Windows; Linux and macOS always
  use **FUSE** (libfuse3 / macFUSE).

On Windows settings are encrypted with DPAPI; on Linux/macOS they are stored as plain JSON.
Settings persist between sessions.

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

Structured Serilog logs are written to the `logs` folder inside the application data folder
(`%LOCALAPPDATA%\CHDMounter` on Windows, `~/.local/share/CHDMounter` on Linux,
`~/Library/Application Support/CHDMounter` on macOS). The log pane in the UI shows the same
messages. When reporting an issue, include the newest log file.
