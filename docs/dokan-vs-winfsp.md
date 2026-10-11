# Mount Cores: Dokan, WinFsp and FUSE

CHDMounter is a single executable that supports three mount cores. Pick the one to use in
**File → Settings → Mount core**; all cores share the same functionality and UI.

| Aspect | Dokan (Windows, default) | WinFsp (Windows) | FUSE (Linux/macOS) |
|--------|--------------------------|------------------|--------------------|
| Driver | Kernel-mode (`Dokan.sys`) | User-mode (WinFsp) | libfuse3 / macFUSE |
| Bindings | `DokanNet` 2.3.0.3 | `winfsp.net` 2.1.25156 | bundled `FuseSharp` |
| File system base | `IDokanOperations` | `FileSystemBase` | `IFuseVolume` |
| Elevation | Standard mounts | Cross-integrity folder mounts with permissive DACL when elevated | Standard (mount points are folders) |
| Vendor | <https://github.com/dokan-dev/dokany/releases> | <https://github.com/winfsp/winfsp/releases> | <https://github.com/libfuse/libfuse> / <https://macfuse.io> |

## Which one should I use?

- All cores mount CHD images as read-only virtual drives and support all 37 console/format modes.
- **Windows:** Pick **Dokan** (the default) if you already use Dokany for other tools, or
  **WinFsp** if you prefer a user-mode driver, need folder mounts while running elevated, or
  already use WinFsp.
- **Linux/macOS:** FUSE is used automatically; install libfuse3 or macFUSE.

You only need the driver for the core you select.

## Important

Install the **latest** driver version. CHDMounter verifies that the installed driver exposes the
functions its bindings require; an outdated version will be reported as incompatible and the
mount will be blocked with a dialog linking to the driver download page.
