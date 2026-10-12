# Architecture

This page describes how CHDMounter is put together for developers who want to understand, debug
or extend the codebase.

## Solution layout

| Project | Target | Purpose |
|---------|--------|---------|
| `CHDMounter` | `net10.0` (Windows/Linux/macOS) | The cross-platform Avalonia application |
| `CHDMounter.Tests` | `net10.0-windows` | xUnit unit and integration tests |
| `CHDMounter_Tester` | `net10.0-windows` | WPF batch-parsing/benchmarking tool (developer only) |

The application consumes two external libraries: [CHDSharp](https://github.com/drpetersonfernandes/CHDSharp)
(CHD decoding, pulled in by the parser package) and
[VideoGameFileSystemParser](https://github.com/drpetersonfernandes/VideoGameFileSystemParser) 1.4.0
(console file system parsing and the console alias registry).

## Component overview

```text
┌────────────────────────────────────────────────────────────────────┐
│ Avalonia UI (CHDMounter/Views)                                     │
│  MainWindow · SettingsWindow · ConsoleSelectionWindow · AboutWindow│
│  MessageDialog · Dialogs                                           │
└───────────────┬────────────────────────────────────────────────────┘
                │ service locator (ServiceProvider)
┌───────────────▼────────────────────────────────────────────────────┐
│ Services (CHDMounter/Services)                                     │
│  ILoggingService/LoggingService      ISettingsService/Settings     │
│  IMountService/MountService ──► Dokan · WinFsp · FUSE backends     │
│  IScreenshotService/ScreenshotService                              │
│  StatsClient · UpdateChecker · BugReportClient                     │
└───────────────┬────────────────────────────────────────────────────┘
                │ parsed volume (ChdContainer from VideoGameFileSystemParser)
┌───────────────▼────────────────────────────────────────────────────┐
│ Mount backends (CHDMounter/Mounting)                               │
│  Dokan  → ChdFs : IDokanOperations                                 │
│  WinFsp → ChdFs : FileSystemBase                                   │
│  FUSE   → ChdFuseVolume : IFuseVolume + FuseSharp interop          │
└────────────────────────────────────────────────────────────────────┘
```

## Startup sequence

1. `Program.Main` builds the Avalonia application (`AppBuilder.Configure<App>()`).
2. `App.OnFrameworkInitializationCompleted` calls `InitializeApplication`:
   - `DiagnosticLogger.Initialize()` configures Serilog (file + debug sinks) and enables the
     bug report sink.
   - `ServiceProvider` registers the logging, settings, mount and screenshot services.
   - `Console.Out`/`Console.Error` are redirected through `LogTextWriter` so console output is
     mirrored into the UI log and Serilog.
   - `ErrorLogger` installs global handlers for unhandled and unobserved exceptions.
   - `StatsClient.SendStats()` and `UpdateChecker.CheckForUpdates()` run in the background.
3. `MainWindow` opens; `Opened` handles command-line arguments and starts polling the update
   result to prompt the user when a newer release exists.

## Services

| Service | Implementation | Responsibility |
|---------|----------------|----------------|
| `ILoggingService` | `LoggingService` | Thread-safe UI log collection (bounded at 5000 entries) + Serilog writes |
| `ISettingsService` | `SettingsService` | Load/save `AppSettings` (DPAPI on Windows, JSON elsewhere) |
| `IMountService` | `MountService` | Routes mount/unmount to the selected backend and owns backend lifetime |
| `IScreenshotService` | `ScreenshotService` | F8 capture of the active window (Avalonia) |

`ServiceProvider` is a small static service locator used as the composition root. Interfaces live
in `CHDMounter/Interfaces`, models in `CHDMounter/Models`.

## Mount pipeline

1. `MountService` resolves the backend from settings (`Dokan`, `WinFsp` or `FUSE`; FUSE is forced
   on Linux/macOS).
2. The backend opens the CHD file with `ChdContainer`, parses the selected console/format and
   computes mount-point candidates (drive letters or folders, with suffixed fallbacks).
3. The backend adapts the parsed container to its driver interface:
   - **Dokan** — `ChdFs` implements `IDokanOperations` (path lookup, file reads, directory
     enumeration, read-only volume information).
   - **WinFsp** — `ChdFs` extends `FileSystemBase` (stable file indexes, permissive DACL for
     cross-integrity folder mounts when elevated).
   - **FUSE** — `ChdFuseVolume` implements `IFuseVolume`; `FuseMountService` runs the blocking
     FUSE session on a dedicated background thread and stops it through `FuseFileSystem.Exit()`.
4. `MountPathHelper` shares folder-name sanitization, candidate generation and volume creation
   time across the folder-based backends.

Guidance dialogs ("driver not installed") are posted to the UI thread **without blocking** the
mount worker (`Dialogs.ConfirmLater`) so mount locks are never held while waiting for the UI.

## Logging pipeline

```text
application code ──► Serilog.Log ──► file sink (debug_*.log)
                                 ├─► debug sink (IDE output)
                                 └─► BugReportSink ──► BugReportClient ──► bug report API
console output ────► LogTextWriter ──► ILoggingService (UI pane) + Serilog
unhandled errors ──► ErrorLogger ──► Serilog
```

- Log files roll daily and are pruned after 7 days on startup.
- `BugReportSink` forwards Warning+ events; user/environment errors are logged below Warning so
  they never become bug reports (see [Privacy](privacy.md)).

## Networking clients

| Client | Endpoint | Behaviour |
|--------|----------|-----------|
| `StatsClient` | `ApplicationStats/stats` | Once per process; fire-and-forget; failures logged at Information |
| `UpdateChecker` | GitHub Releases API | Runs once per process; result polled by the UI; failures logged at Information |
| `BugReportClient` | `bugreport/api/send-bug-report` | Queued (one at a time, ~6 s spacing); fail-closed in tests; test hosts excluded |

## Threading model

- UI work runs on the Avalonia dispatcher; dialogs are marshaled to it.
- Mount/unmount operations run on background tasks (`Task.Run`) so the UI stays responsive.
- The FUSE session blocks by design and therefore owns a dedicated background thread; the mount
  service waits for the FUSE `init` callback (30 s timeout) before reporting success.
- `LoggingService` posts collection updates to the UI thread and bounds the log at 5000 entries.

## Source layout

```text
CHDMounter/
  App.axaml(.cs)        Application lifecycle and composition root
  Program.cs            Entry point
  Interfaces/           Service contracts (FUSE contracts come from the LibFuseSharp NuGet package)
  Logging/              Serilog setup, sinks, console writer, error handlers
  Models/               Settings, log entry, mount backend, update result
  Mounting/             Mount router + Dokan/WinFsp/FUSE backends, shared path helper
  Services/             Logging, settings, mounting, screenshots, stats, updates, bug reports
  Themes/               Dark theme resources
  Views/                Windows, dialogs and code-behind
```

## See also

- [Building from Source](building.md)
- [Development Guide](development.md)
- [How It Works](how-it-works.md)
