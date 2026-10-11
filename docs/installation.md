# Installation

## Requirements

| Requirement | Notes |
|-------------|-------|
| Windows 10 or later | x64 or ARM64; use the .NET 10 **Desktop** Runtime |
| Linux | x64 or ARM64; libfuse3 (e.g. `sudo apt install libfuse3-3 fuse3`) and `/dev/fuse` |
| macOS | Intel or Apple silicon; [macFUSE](https://macfuse.io) installed |
| [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) | Required: the download bundles are framework-dependent and do **not** embed the runtime |

## Choose a mount core

A single CHDMounter executable supports every core and the one to use is selected in
**File → Settings → Mount core**:

- **Dokan** (default, Windows) — uses [Dokany](https://github.com/dokan-dev/dokany/releases)
  (`Dokan.sys`).
- **WinFsp** (Windows) — uses [WinFsp](https://github.com/winfsp/winfsp/releases).
- **FUSE** (Linux/macOS) — uses libfuse3 on Linux and macFUSE on macOS.

On Windows you only need to install the driver you actually use; Dokan is the default. On Linux
and macOS the FUSE core is always used. See [Mount Cores](dokan-vs-winfsp.md) for help choosing.

> **Important:** always install the **latest** driver release. Outdated driver versions can be
> missing functions that the application bindings require.

## Download

Grab the latest bundle from the [Releases page](https://github.com/purelogiccode/CHDMounter/releases).
Each bundle is a framework-dependent, single-file executable:

| Bundle | Platform | Architecture |
|--------|----------|--------------|
| `release_<version>_win-x64.zip` | Windows | x64 |
| `release_<version>_win-arm64.zip` | Windows | ARM64 |
| `release_<version>_linux-x64.zip` | Linux | x64 |
| `release_<version>_linux-arm64.zip` | Linux | ARM64 |
| `release_<version>_osx-x64.zip` | macOS | Intel |
| `release_<version>_osx-arm64.zip` | macOS | Apple silicon |

Every bundle contains:

- `CHDMounter` (`CHDMounter.exe` on Windows)
- `LICENSE.txt`
- `README.md`
- `WhatsNew.md`

Extract the bundle anywhere and run `CHDMounter` (`./CHDMounter` or `open CHDMounter` on
macOS). No installer is required. If the executable permission bit was lost during extraction,
run `chmod +x CHDMounter` once.

> The bundles are portable: settings, logs and screenshots are written to the application data
> folder (see below), not next to the executable.

## Installing Dokan (Windows)

1. Download the latest Dokany release from <https://github.com/dokan-dev/dokany/releases>.
2. Run the installer (accept the driver installation prompts).
3. Reboot if the installer asks you to.

## Installing WinFsp (Windows)

1. Download the latest WinFsp release from <https://github.com/winfsp/winfsp/releases>.
2. Run the installer and choose a standard installation.
3. Reboot if the installer asks you to.

CHDMounter checks the installed native DLL (`winfsp-a64.dll`, `winfsp-x64.dll` or
`winfsp-x86.dll`), resolves the entry points the .NET bindings need and verifies the version
match before mounting. If the driver is missing or outdated, CHDMounter shows a dialog with a
link to the WinFsp download page.

## Installing FUSE (Linux / macOS)

- **Linux**: install FUSE 3 with your package manager, for example
  `sudo apt install libfuse3-3 fuse3` (Debian/Ubuntu), `sudo dnf install fuse3 fuse3-libs`
  (Fedora) or `sudo pacman -S fuse3` (Arch). Make sure the `fuse` kernel module is available
  (`/dev/fuse` exists).
- **macOS**: install [macFUSE](https://macfuse.io). On macOS 15.4 or later choose the FSKit
  backend when prompted (no kernel extension required).

If the library is missing, CHDMounter shows a dialog with the download/install page.

## Verifying the installation

1. Start CHDMounter. The main window opens without an error dialog.
2. The title bar shows the selected mount core, for example `CHDMounter (Dokan)`.
3. Mount any `.chd` file. The status bar shows **Mounted** and the drive letter/folder.
4. If the driver is missing, a dialog offers to open the driver download page; the log pane
   explains the exact reason.

## Where are settings, logs and screenshots stored?

| Data | Location |
|------|----------|
| Settings and logs | Windows: `%LOCALAPPDATA%\CHDMounter` · Linux: `~/.local/share/CHDMounter` · macOS: `~/Library/Application Support/CHDMounter` |
| Screenshots (F8) | Primary: `Screenshot` folder next to the executable · Fallback: the application data folder above |

Use **Help → Open AppData Folder** in the application to open the application data folder. On
Windows the settings file is DPAPI-encrypted; on Linux/macOS it is plain JSON. See
[Settings](settings.md) and [Screenshots](screenshots.md) for details.

## Uninstalling

CHDMounter is portable — delete the extracted files to remove the application, and delete the
application data folder to remove settings, logs and screenshots. Driver packages (Dokan,
WinFsp, macFUSE) are installed separately and are removed through your system's software
manager.

## Next steps

- [Getting Started](getting-started.md) — mount your first image
- [Troubleshooting](troubleshooting.md) — driver and mount problems
- [Building from Source](building.md) — build your own binary
