# FAQ

### Do I need both Dokan and WinFsp?

No. Install the driver that matches the bundle you downloaded. The two bundles are separate
executables because they use different driver APIs.

### Does CHDMounter modify my CHD files?

No. The image is opened read-only and the mounted volume is read-only.

### Can I mount a CHD as a regular folder instead of a drive letter?

Yes. Pass a folder path as the mount point, e.g. `CHDMounter.exe game.chd C:\Mounts\game`. When
running elevated with WinFsp, folder mounts use a permissive DACL so standard processes can access
them.

### Which console type should I choose if I am unsure?

Try `psauto` for PlayStation discs, `iso9660` for generic data discs, or `isoraw2352` when an
emulator (xemu, RPCS3) wants a plain `image.iso`.

### Why is the download bundle only a few megabytes?

The bundles are framework-dependent single-file executables: the .NET runtime is intentionally
not embedded. Install the .NET 10 Desktop Runtime once and all applications can use it.

### Is the Tester application still released?

No. Starting with version 1.2.1 the `CHDMounter_Tester` batch-parsing/benchmarking tool is no
longer published in release bundles. It remains available in the source tree for developers.

### Where does the update checker get downloads from?

CHDMounter checks the latest GitHub release of
[purelogiccode/CHDMounter](https://github.com/purelogiccode/CHDMounter/releases) and, when a
newer version exists, links to the bundle matching your variant (Dokan/WinFsp) and architecture
(x64/ARM64).

### Do you collect diagnostics?

The application can send crash/bug reports to help diagnose problems. Known user errors
(missing driver, wrong console type, busy mount point) are never filed as bugs, and test
harnesses can never file reports. Logs always stay local under `%LOCALAPPDATA%\CHDMounter`.
