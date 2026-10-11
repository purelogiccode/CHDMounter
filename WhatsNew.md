# What's New

## Unreleased

### Changed

- **One application instead of two** — the Dokan, WinFsp and Core projects are merged into a
  single `CHDMounter` application. There is now one download per platform/architecture
  (`release_<version>_<rid>.zip`) instead of separate Dokan and WinFsp bundles.
- **Cross-platform UI** — the WPF interface was ported to Avalonia with the same dark theme and
  layout, and the application now runs on Windows, Linux and macOS (x64 and ARM64).
- **Selectable mount core** — choose Dokan (default) or WinFsp on Windows in
  **Settings → Mount core**. Linux and macOS mount through FUSE 3 (libfuse3 / macFUSE) using the
  bundled `FuseSharp` library.
- **Per-platform settings** — settings stay DPAPI-encrypted on Windows and are stored as plain
  JSON on Linux/macOS.

## 1.2.1

### Fixed

- **WinFsp variant could never mount** — the single-file release did not extract its content, so
  `Fsp.Interop.Api`'s static initializer failed with
  `The type initializer for 'Fsp.Interop.Api' threw an exception` even when WinFsp was installed
  (reported as bugs 67653–67656 and 67777/67778). The WinFsp bundle now extracts its content to
  disk so the WinFsp.NET bindings can locate `winfsp-msil.dll` and initialize correctly.
- **WinFsp detection is now a full compatibility check** — the app loads the native WinFsp DLL,
  resolves every entry point the bindings need, and verifies the native/managed version match
  *before* attempting a mount. Missing or outdated WinFsp now shows an actionable
  "install or repair WinFsp" dialog instead of failing mid-mount.
- **ARM64 WinFsp** — the native DLL probe now selects `winfsp-a64.dll` on ARM64 instead of
  looking for the x64 DLL and reporting WinFsp as missing.
- **Mount-point folder creation is retried** — a folder mount point that cannot be created no
  longer aborts the remaining drive-letter/folder candidates.
- **ISO 9660 / PC-FX multi-extent matching** — continuation records must match byte-for-byte
  (ordinal). Case-insensitive matching could merge two distinct Joliet names into one file.
- **HFS/HFS+ catalog deduplication** — deduplication is now ordinal, so HFSX volumes with
  names that differ only by case keep all entries.
- **VideoGameFileSystemParser public API** — `FileEntry.Extents` and `SectorReader.Tracks`
  are `List<T>` again (they had been changed to `IList<T>`, breaking external consumers).
- **Bug-report noise** — known test hosts (`testhost`, `vstest`, `ReSharperTestRunner`,
  `xunit.console`, NUnit, MSTest) can never file production bug reports, while the
  `CHDMounter_Tester` application keeps its diagnostics.
- **User-error handling** — missing drivers, wrong console type, busy mount points and similar
  environment problems are logged without filing a bug report; genuinely unexpected mount and
  unmount failures are still reported with full stack traces.

### Changed

- **Release bundles are framework-dependent** — the .NET runtime is no longer embedded.
  Each bundle contains a single `CHDMounter.exe` plus `LICENSE.txt`, `README.md` and
  `WhatsNew.md`, and requires the **.NET 10 Desktop Runtime**.
- **Update checker** now points at the canonical repository `purelogiccode/CHDMounter`.
- **The Tester application is no longer published** in releases.

### Bundles

| Bundle | Architecture | Driver |
|--------|--------------|--------|
| `release_1.2.1_Dokan_win-x64.zip` | x64 | [Dokan](https://github.com/dokan-dev/dokany/releases) |
| `release_1.2.1_Dokan_win-arm64.zip` | ARM64 | [Dokan](https://github.com/dokan-dev/dokany/releases) |
| `release_1.2.1_WinFsp_win-x64.zip` | x64 | [WinFsp](https://github.com/winfsp/winfsp/releases) |
| `release_1.2.1_WinFsp_win-arm64.zip` | ARM64 | [WinFsp](https://github.com/winfsp/winfsp/releases) |

## 1.2.0

- Added the WinFsp variant alongside the Dokan variant.
- 37 console/format modes, virtual CUE/BIN/ISO/WAV export, SingleFile ISO passthrough.
- Console selection is alias-only (e.g. `ps2`, `cuebin2352`).
- Vendored `VideoGameFileSystemParser` as a project reference.
- Added the CHDMounter Tester batch-parsing/benchmarking application.
- Bug-report API, logging, settings and mount diagnostics improvements.

## 1.1.0

- Added diagnostics logging, update checker and statistics reporting.
- Wildcard enumeration fixes on mounted virtual drives.
- Removed the CHDSharp dependency from the application (now via VideoGameFileSystemParser).
- README and release process improvements.

## 1.0.0

- First public release: mount CHD images as virtual read-only drives with Dokan.
- ISO 9660, UDF, XDVDFS, OperaFS, CD-i Green Book and HFS/HFS+ support.
