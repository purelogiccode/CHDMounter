# Troubleshooting

## "The application requires the .NET runtime"

The download bundles are framework-dependent: they do not embed the runtime. Install the
[.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (on Windows the Desktop
Runtime; x64 or ARM64 to match your bundle) and start CHDMounter again.

## "The type initializer for 'Fsp.Interop.Api' threw an exception" (WinFsp)

This message appears when the WinFsp build cannot initialize the WinFsp.NET bindings. Causes:

1. **WinFsp is not installed** — install the latest release.
2. **An old WinFsp version is installed** — the bindings require the native DLL to expose every
   function they use and to match the expected version. Install the latest release and reboot.
3. **A mismatched `winfsp-*.dll` is next to the executable** — remove stray `winfsp-a64.dll`,
   `winfsp-x64.dll` or `winfsp-x86.dll` files from the application folder and let CHDMounter use
   the registered installation.

CHDMounter detects these situations before mounting and shows a dialog with the WinFsp download
link. Starting with version 1.2.1 the WinFsp bundle also extracts its content correctly in
single-file mode, which is required for the WinFsp.NET bindings to load.

## "Dokan driver not found" / "Driver not found"

The driver for the selected mount core is missing or cannot be loaded. Install the latest
[Dokany](https://github.com/dokan-dev/dokany/releases) or
[WinFsp](https://github.com/winfsp/winfsp/releases) release and restart the application, or
switch cores in **Settings → Mount core**.

## "FUSE Library Not Found" (Linux / macOS)

The FUSE 3 library is missing. On Linux install it with your package manager (for example
`sudo apt install libfuse3-3 fuse3`) and make sure `/dev/fuse` exists (`sudo modprobe fuse`).
On macOS install [macFUSE](https://macfuse.io).

## Mount fails with "No available drive letter"

All drive letters are in use, or the chosen letter is reserved by another process (drives mounted
in another session can be invisible to `DriveInfo`). Pick a free letter in the **Mount point**
box or let CHDMounter choose automatically.

## Mount fails with an NTSTATUS error (WinFsp)

`NTSTATUS 0xC0000035` (`STATUS_OBJECT_NAME_COLLISION`) means the folder mount point is already in
use. CHDMounter retries with suffixed folders automatically; if the problem persists, unmount any
stale CHDMounter mount or restart the application.

## Parse failure / empty directory listing

- Verify you selected the correct console type. Try `iso9660` or `psauto`, or the alias
  recommended in [Console Types](console-types.md).
- Some discs ship with unusual layouts; try a generic mode (`iso9660`, `isoraw2352`).
- Check the log pane for the parser's error message.

## Where are the logs?

Open **Open AppData Folder** in the application, or browse to the `logs` folder inside the
application data folder (`%LOCALAPPDATA%\CHDMounter` on Windows, `~/.local/share/CHDMounter` on
Linux, `~/Library/Application Support/CHDMounter` on macOS). Attach the newest log file when
reporting an issue.

## The virtual drive appears as "CHDFS" and is read-only

That is expected. CHDMounter mounts images read-only and never modifies the CHD file.
