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
emulator (xemu, RPCS3) wants a plain `image.iso`.

### Why is the download bundle only a few megabytes?

The bundles are framework-dependent single-file executables: the .NET runtime is intentionally
not embedded. Install the .NET 10 runtime once and all applications can use it.

### Is the Tester application still released?

No. Starting with version 1.2.1 the `CHDMounter_Tester` batch-parsing/benchmarking tool is no
longer published in release bundles. It remains available in the source tree for developers
(Windows/WPF only).

### Where does the update checker get downloads from?

CHDMounter checks the latest GitHub release of
[purelogiccode/CHDMounter](https://github.com/purelogiccode/CHDMounter/releases) and, when a
newer version exists, links to the bundle matching your platform and architecture
(`win-x64`, `linux-arm64`, `osx-x64`, and so on).

### Do you collect diagnostics?

The application can send crash/bug reports to help diagnose problems. Known user errors
(missing driver, wrong console type, busy mount point) are never filed as bugs, and test
harnesses can never file reports. Logs always stay local in the application data folder.
