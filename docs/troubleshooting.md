# Troubleshooting

Start with the log pane in the main window and the newest file in the `logs` folder — most
problems are explained there. See [Usage](usage.md#logs) for the log locations.

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
link. The single-file release extracts its content correctly, which is required for the
WinFsp.NET bindings to load.

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
box or let CHDMounter choose automatically. On Linux/macOS use a folder path instead.

## Mount fails with an NTSTATUS error (WinFsp)

`NTSTATUS 0xC0000035` (`STATUS_OBJECT_NAME_COLLISION`) means the folder mount point is already in
use. CHDMounter retries with suffixed folders automatically; if the problem persists, unmount any
stale CHDMounter mount or restart the application.

## The mount point folder cannot be created

CHDMounter retries the remaining mount candidates when a folder cannot be created, and reports
the reason in the log. Check the folder permissions or choose a different mount point.

## Parse failure / empty directory listing

- Verify you selected the correct console type. Try `iso9660` or `psauto`, or the alias
  recommended in [Console Types](console-types.md).
- Some discs ship with unusual layouts; try a generic mode (`iso9660`, `isoraw2352`).
- Check the log pane for the parser's error message.

## F8 does not create a screenshot

- Make sure a CHDMounter window is focused when you press **F8**.
- Screenshots are written to the `Screenshot` folder next to the executable first; if that folder
  is not writable the application falls back to the application data folder. The saved path is
  written to the log — see [Screenshots](screenshots.md).
- A failure to save is logged but never interrupts the application.

## The update notification does not appear

The update check is best-effort. If GitHub cannot be reached (offline, firewall, rate limit) no
notification is shown and the failure is logged at Information level. You can always check the
[Releases page](https://github.com/purelogiccode/CHDMounter/releases) manually.

## Settings are not saved or keep resetting

- On Windows, settings are DPAPI-encrypted for the current user. Copying `settings.dat` between
  user accounts or machines makes it unreadable; CHDMounter then resets to defaults.
- A corrupt settings file is reset automatically (and removed) on the next launch.
- To reset manually, close the application and delete `settings.dat` from the application data
  folder — see [Settings](settings.md).

## The virtual drive appears as "CHDFS" and is read-only

That is expected. CHDMounter mounts images read-only and never modifies the CHD file.

## Reporting an issue

1. Reproduce the problem once.
2. Open **Help → Open AppData Folder** and pick the newest file in `logs`.
3. Open an issue on the
   [issue tracker](https://github.com/purelogiccode/CHDMounter/issues) and attach the log plus a
   description of the disc and console type used.

See [Privacy and Telemetry](privacy.md) for what the application may send automatically.
