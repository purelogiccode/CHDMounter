# Usage

## GUI

The main window exposes:

| Control | Purpose |
|---------|---------|
| **Browse** | Select a `.chd` file |
| **Console type** | Choose the filesystem/format parser |
| **Mount** | Mount the image as a read-only virtual drive |
| **Unmount** | Remove the virtual drive |
| **Log pane** | Real-time mount/parse log output |
| **Status bar** | Current state and the mounted drive letter/folder |

### Menus

| Menu | Item | Purpose |
|------|------|---------|
| **File** | Open CHD… | Select a `.chd` file |
| **File** | Settings | Auto-open the mounted drive and select the mount core |
| **File** | Exit | Close the application (unmounts first) |
| **Help** | Open AppData Folder | Opens the folder with settings, logs and screenshots |
| **Help** | About | Version information, update banner and third-party credits |

### Keyboard

| Key | Action |
|-----|--------|
| **F8** | Save a screenshot of the window (see [Screenshots](screenshots.md)) |

### Mount options

- **Auto-open drive** — open the mounted drive in your file manager when the mount succeeds.
  The same behaviour can be requested per run with the `/l` command-line flag.
- **Mount core** — choose **Dokan** (default) or **WinFsp** on Windows; Linux and macOS always
  use **FUSE** (libfuse3 / macFUSE).

On Windows settings are encrypted with DPAPI; on Linux/macOS they are stored as plain JSON.
Settings persist between sessions — see [Settings](settings.md) for all options and storage
locations.

## Update checker

On startup CHDMounter checks the latest release of
[purelogiccode/CHDMounter](https://github.com/purelogiccode/CHDMounter/releases). When a newer
version exists:

- the main window asks whether you want to open the matching download page for your platform and
  architecture (`win-x64`, `linux-arm64`, `osx-x64`, …);
- the **About** window shows a banner with a direct download link.

The check is best-effort: network failures are logged locally and never interrupt the
application. See [Privacy](privacy.md) if you want to disable network access.

## CLI via the GUI build

The GUI executable is also a CLI application:

```text
CHDMounter.exe [/l] [/a] [/s:<alias>] <chd_file> [mount_point]
```

See [Command Line](command-line.md) for all flags and examples.

## Read-only by design

CHDMounter never writes to your CHD files. The mounted volume reports a read-only filesystem and
the image is opened in read-only mode. Windows shows the volume as **CHDFS**.

## Closing and signals

- Closing the window (or **File → Exit**) unmounts the drive before the application exits.
- On Linux and macOS, `SIGTERM`, `SIGHUP` and Ctrl+C (`SIGINT`) also shut the application down
  cleanly: the drive is unmounted and the log is flushed before the process exits.

## Logs

Structured Serilog logs are written to the `logs` folder inside the application data folder
(`%LOCALAPPDATA%\CHDMounter` on Windows, `~/.local/share/CHDMounter` on Linux,
`~/Library/Application Support/CHDMounter` on macOS). Log files roll daily and files older than
7 days are removed at startup. The log pane in the UI shows the same messages.

When reporting an issue, include the newest log file — see [Troubleshooting](troubleshooting.md).

## See also

- [Settings](settings.md)
- [Screenshots](screenshots.md)
- [Privacy and Telemetry](privacy.md)
