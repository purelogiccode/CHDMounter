# FAQ

### Do I need both Dokan and WinFsp?

No. CHDMounter is a single executable; choose the mount core in **Settings → Mount core**. On
Windows, Dokan is the default but you can switch to WinFsp at any time. Linux and macOS always
use FUSE (libfuse3 / macFUSE).

### Does CHDMounter run on Linux and macOS?

Yes. The application is cross-platform (Windows, Linux and macOS, x64 and ARM64). On Linux it
needs libfuse3 and `/dev/fuse`; on macOS it needs macFUSE.

### Does CHDMounter modify my CHD files?

No. The image is opened read-only and the mounted volume is read-only.

### Can I mount a CHD as a regular folder instead of a drive letter?

Yes. Pass a folder path as the mount point, e.g. `CHDMounter game.chd C:\Mounts\game` on Windows
or `CHDMounter game.chd ~/mounts/game` on Linux/macOS. When running elevated with WinFsp, folder
mounts use a permissive DACL so standard processes can access them. On Linux/macOS folder mounts
are always used.

### Which console type should I choose if I am unsure?

Try `psauto` for PlayStation discs, `iso9660` for generic data discs, or `isoraw2352` when an
emulator (xemu, RPCS3) wants a plain `image.iso`. See [Console Types](console-types.md) for the
complete guide.

### Why is the download bundle only a few megabytes?

The bundles are framework-dependent single-file executables: the .NET runtime is intentionally
not embedded. Install the .NET 10 runtime once and all applications can use it.

### How do I take a screenshot?

Press **F8** while a CHDMounter window is focused. Images are saved locally — see
[Screenshots](screenshots.md).

### Where are my settings, logs and screenshots?

In the application data folder (`%LOCALAPPDATA%\CHDMounter` on Windows,
`~/.local/share/CHDMounter` on Linux, `~/Library/Application Support/CHDMounter` on macOS),
except screenshots, which prefer a `Screenshot` folder next to the executable. Use
**Help → Open AppData Folder** to open the folder.

### How do I reset the application to defaults?

Close CHDMounter and delete `settings.dat` (or the whole application data folder). The
application recreates everything with defaults on the next start. See [Settings](settings.md).

### Is the Tester application still released?

No. The `CHDMounter_Tester` batch-parsing/benchmarking tool is a Windows-only developer tool and
is not published in release bundles. It remains available in the source tree — see the
[Development Guide](development.md).

### Where does the update checker get downloads from?

CHDMounter checks the latest GitHub release of
[purelogiccode/CHDMounter](https://github.com/purelogiccode/CHDMounter/releases) and, when a
newer version exists, links to the bundle matching your platform and architecture
(`win-x64`, `linux-arm64`, `osx-x64`, and so on).

### Do you collect diagnostics?

The application sends anonymous usage statistics once per launch and can send crash/bug reports
when warnings or errors are logged. Known user errors (missing driver, wrong console type, busy
mount point) are never filed as bugs, and test harnesses can never file reports. Logs and
screenshots always stay local. See [Privacy and Telemetry](privacy.md) for the exact payloads
and how to disable network access.

### Can I use CHDMounter commercially?

CHDMounter is licensed under **GPLv3**. See [License](license.md) for details and third-party
component licenses.
