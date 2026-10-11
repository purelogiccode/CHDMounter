# Settings

Open the settings dialog with **File → Settings** (or the **Settings** menu item). Changes take
effect when you click **Save**.

## Options

| Setting | Default | Description |
|---------|---------|-------------|
| **Auto open the mounted drive in Explorer** | On | Opens the mounted drive in the platform file manager (Explorer, Finder, `xdg-open`) as soon as the mount succeeds. The same behaviour can be requested per run with the [`/l`](command-line.md) flag. |
| **Mount core** | Dokan (Windows) | The file system driver used to expose CHD images as virtual drives: **Dokan** (default) or **WinFsp** on Windows. On Linux and macOS the FUSE core is always used. |

Only the mount cores supported by the current operating system are shown in the dialog. See
[Mount Cores](dokan-vs-winfsp.md) for help choosing between Dokan and WinFsp.

## Where settings are stored

Settings are persisted next to the application logs:

| Platform | Location |
|----------|----------|
| Windows | `%LOCALAPPDATA%\CHDMounter\settings.dat` |
| Linux | `~/.local/share/CHDMounter/settings.dat` |
| macOS | `~/Library/Application Support/CHDMounter/settings.dat` |

- On **Windows** the file is encrypted with the **Data Protection API (DPAPI)** for the current
  user account.
- On **Linux and macOS** the file is plain JSON (it contains no sensitive data).

Use **Help → Open AppData Folder** to open the folder in your file manager.

## Resetting settings

Close CHDMounter and delete `settings.dat` (or the whole `CHDMounter` application data folder).
The application recreates the file with defaults on the next start.

If the settings file becomes unreadable or corrupt, CHDMounter logs the problem and resets to
defaults automatically — the corrupt file is deleted so the failure does not recur on every
launch.

## See also

- [Usage](usage.md) — the GUI reference
- [Mount Cores](dokan-vs-winfsp.md) — Dokan, WinFsp and FUSE compared
- [Troubleshooting](troubleshooting.md) — mount and driver problems
