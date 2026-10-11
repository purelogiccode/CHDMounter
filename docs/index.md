# CHDMounter

Mount CHD (Compressed Hunks of Data) CD/DVD images as virtual read-only drives on Windows, Linux
and macOS. CHDMounter supports 37 console/format modes and lets you choose the mount core in
Settings: **Dokan** (default) or **WinFsp** on Windows, **FUSE** (libfuse3 / macFUSE) on Linux
and macOS.

![CHDMounter main window](images/screenshot.png)

## Highlights

- Mount `.chd` images as read-only virtual drives
- 37 console/format modes (PlayStation, Sega, Xbox, CD-i, 3DO, PC Engine, and generic CUE/ISO/BIN modes)
- Virtual CUE/BIN/ISO/WAV export for emulators and burning tools
- SingleFile ISO passthrough for xemu and RPCS3
- Command-line interface for scripting and frontend integration
- Avalonia cross-platform dark-theme UI with settings persistence and real-time logs
- Selectable mount core: Dokan (default) or WinFsp on Windows; FUSE on Linux/macOS
- x64 and ARM64 builds for Windows, Linux and macOS

## Documentation

| Page | Description |
|------|-------------|
| [Installation](installation.md) | Drivers, .NET runtime and release bundles |
| [Getting Started](getting-started.md) | Your first mount in a few clicks |
| [Usage](usage.md) | GUI walkthrough |
| [Command Line](command-line.md) | Flags, aliases and examples |
| [Console Types](console-types.md) | Full alias reference |
| [How It Works](how-it-works.md) | CHD reading, sector mapping and filesystem parsing |
| [Dokan vs WinFsp](dokan-vs-winfsp.md) | Choosing a mount core |
| [Troubleshooting](troubleshooting.md) | Common problems and fixes |
| [FAQ](faq.md) | Frequently asked questions |
| [Changelog](changelog.md) | Release notes |
| [License](license.md) | GPLv3 |

## Requirements at a glance

- Windows 10+ (x64/ARM64), Linux (x64/ARM64) or macOS (Intel/Apple silicon)
- [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows: [Dokan](https://github.com/dokan-dev/dokany/releases) (default) or [WinFsp](https://github.com/winfsp/winfsp/releases)
- Linux: libfuse3 and `/dev/fuse`
- macOS: [macFUSE](https://macfuse.io)

See [Installation](installation.md) for details.
