# Installation

## Requirements

| Requirement | Notes |
|-------------|-------|
| Windows 10 or later | x64 or ARM64 |
| [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) | Required: the download bundles are framework-dependent and do **not** embed the runtime |
| [Dokan](https://github.com/dokan-dev/dokany/releases) **or** [WinFsp](https://github.com/winfsp/winfsp/releases) | Only one driver is needed — see below |

## Choose a driver

CHDMounter ships two variants:

- **Dokan build** — uses [Dokany](https://github.com/dokan-dev/dokany/releases) (`Dokan.sys`).
- **WinFsp build** — uses [WinFsp](https://github.com/winfsp/winfsp/releases).

Both variants provide the same features and UI. Install the driver that matches the bundle you
downloaded. You do not need to install both.

> **Important:** always install the **latest** driver release. Outdated driver versions can be
> missing functions that the application bindings require.

## Download

Grab the latest bundle from the [Releases page](https://github.com/purelogiccode/CHDMounter/releases).
Each bundle is a framework-dependent, single-file executable:

| Bundle | Architecture | Driver |
|--------|--------------|--------|
| `release_<version>_Dokan_win-x64.zip` | x64 | Dokan |
| `release_<version>_Dokan_win-arm64.zip` | ARM64 | Dokan |
| `release_<version>_WinFsp_win-x64.zip` | x64 | WinFsp |
| `release_<version>_WinFsp_win-arm64.zip` | ARM64 | WinFsp |

Every bundle contains:

- `CHDMounter.exe`
- `LICENSE.txt`
- `README.md`
- `WhatsNew.md`

Extract the bundle anywhere and run `CHDMounter.exe`. No installer is required.

## Installing Dokan

1. Download the latest Dokany release from <https://github.com/dokan-dev/dokany/releases>.
2. Run the installer (accept the driver installation prompts).
3. Reboot if the installer asks you to.

## Installing WinFsp

1. Download the latest WinFsp release from <https://github.com/winfsp/winfsp/releases>.
2. Run the installer and choose a standard installation.
3. Reboot if the installer asks you to.

The WinFsp build checks the installed native DLL (`winfsp-a64.dll`, `winfsp-x64.dll` or
`winfsp-x86.dll`), resolves the entry points the .NET bindings need and verifies the version
match before mounting. If the driver is missing or outdated, CHDMounter shows a dialog with a
link to the WinFsp download page.

## Where are settings and logs stored?

Settings, diagnostic logs and screenshots are stored under `%LOCALAPPDATA%\CHDMounter`. Use
**Open AppData Folder** in the application to open that directory.
