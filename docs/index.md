# CHDMounter

Mount CHD (Compressed Hunks of Data) CD/DVD images as virtual read-only drives on Windows, Linux
and macOS. CHDMounter supports 37 console/format modes and lets you choose the mount core in
Settings: **Dokan** (default) or **WinFsp** on Windows, **FUSE** (libfuse3 / macFUSE) on Linux
and macOS.

![CHDMounter main window](images/screenshot.png)

## Highlights

- Mount `.chd` images as read-only virtual drives — your files are never modified
- 37 console/format modes (PlayStation, Sega, Xbox, CD-i, 3DO, PC Engine, and generic
  CUE/ISO/BIN modes)
- Virtual CUE/BIN/ISO/WAV export for emulators and burning tools
- SingleFile ISO passthrough for xemu and RPCS3
- Command-line interface for scripting and frontend integration
- Avalonia cross-platform dark-theme UI with settings persistence and real-time logs
- Selectable mount core: Dokan (default) or WinFsp on Windows; FUSE on Linux/macOS
- **F8** screenshot capture and an update checker for new GitHub releases
- x64 and ARM64 builds for Windows, Linux and macOS

## Requirements at a glance

- Windows 10+ (x64/ARM64), Linux (x64/ARM64) or macOS (Intel/Apple silicon)
- [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows: [Dokan](https://github.com/dokan-dev/dokany/releases) (default) or
  [WinFsp](https://github.com/winfsp/winfsp/releases)
- Linux: libfuse3 and `/dev/fuse`
- macOS: [macFUSE](https://macfuse.io)

See [Installation](installation.md) for details.

## Documentation

### User guide

| Page | Description |
|------|-------------|
| [Installation](installation.md) | Drivers, .NET runtime and release bundles |
| [Getting Started](getting-started.md) | Your first mount in a few clicks |
| [Usage](usage.md) | GUI walkthrough, update checker and logs |
| [Settings](settings.md) | Auto-open drive, mount core and settings storage |
| [Screenshots](screenshots.md) | F8 capture and where images are saved |
| [Command Line](command-line.md) | Flags, aliases and examples |
| [Console Types](console-types.md) | Full alias reference and choosing guide |
| [Mount Cores](dokan-vs-winfsp.md) | Dokan, WinFsp and FUSE compared |
| [Troubleshooting](troubleshooting.md) | Common problems and fixes |
| [FAQ](faq.md) | Frequently asked questions |
| [Privacy and Telemetry](privacy.md) | What is sent and how to disable it |
| [License](license.md) | GPLv3 and third-party components |

### Developer guide

| Page | Description |
|------|-------------|
| [Architecture](architecture.md) | Solution layout, services, mount and logging pipelines |
| [Building from Source](building.md) | Build, publish and release packaging |
| [Development Guide](development.md) | Conventions, tests and the Tester tool |
| [Changelog](changelog.md) | Release notes |

## Project links

- [Releases](https://github.com/purelogiccode/CHDMounter/releases)
- [Issue tracker](https://github.com/purelogiccode/CHDMounter/issues)
- [Source code](https://github.com/purelogiccode/CHDMounter)
- [Documentation site](https://purelogiccode.github.io/CHDMounter/)
- [Wiki](https://github.com/purelogiccode/CHDMounter/wiki)
