# Development Guide

## Getting started

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (the repository
   pins `10.0.301` in `global.json`).
2. Clone the repository and build:

   ```bash
   dotnet build CSharp_CHDMounter.sln
   dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj
   ```

3. Run the application:

   ```bash
   dotnet run --project CHDMounter/CHDMounter.csproj
   ```

See [Building from Source](building.md) for publishing and release packaging.

## Repository layout

| Path | Contents |
|------|----------|
| `CHDMounter/` | Cross-platform Avalonia application |
| `CHDMounter/Interfaces/` | Service contracts |
| `CHDMounter/Models/` | Settings, log entry, mount backend, update result |
| `CHDMounter/Services/` | Logging, settings, mounting, screenshots, stats, updates, bug reports |
| `CHDMounter/Mounting/` | Mount router, Dokan/WinFsp/FUSE backends, shared path helper |
| `CHDMounter/Views/` | Windows, dialogs and code-behind |
| `CHDMounter.Tests/` | xUnit unit and integration tests |
| `CHDMounter_Tester/` | WPF batch-parsing/benchmarking tool (developer only) |
| `build/` | Release packaging script |
| `docs/` | This documentation (docfx + GitHub wiki source) |

## Code conventions

- **Nullable reference types** are enabled everywhere; avoid `!` unless the invariant is obvious
  and commented.
- **Implicit usings** are enabled; shared global usings live in `GlobalUsings.cs`.
- **XML documentation** is enforced for public API (`GenerateDocumentationFile`); the solution
  must build with zero warnings. Document internal helpers too.
- **Async naming** — every asynchronous method ends with `Async`, including event handlers.
- **Logging** goes through Serilog. Never use `Console.WriteLine`/`Debug.WriteLine` for
  diagnostics; use `ILoggingService` (UI + Serilog) or `Serilog.Log` directly.
- **User/environment errors** are logged with `LoggingService.LogUserError` (Information level)
  so they never file bug reports; genuine failures use `LogError` with the exception.
- **Analyzers** — `Meziantou.Analyzer` runs on all projects; keep the build warning-free.
- **Visibility** — implementation classes are `internal`; only contracts and types needed by the
  Tester or tests are `public` (tests and the Tester use `InternalsVisibleTo`).
- **Catch blocks** always log; silent catches are only allowed for documented last-resort paths.

## Testing

The test project uses **xUnit** and contains both unit tests and integration tests:

```bash
# All tests
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj

# A single class
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj --filter "FullyQualifiedName~MountPathHelperTests"

# By namespace
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj --filter "FullyQualifiedName~CHDMounter.Tests.Services"
```

- **Integration tests** (`CHDMounter.Tests/Parsers`) parse real CHD files. They read paths from
  `ChdPathCatalog`; directories that do not exist on the machine are skipped, so the suite passes
  on machines without the game library.
- **Unit tests** must not send real bug reports — `TestInitializer` disables bug reporting for
  the whole test process, and `BugReportClient` refuses to send from test hosts.
- Prefer testing pure logic through internal statics (for example `MountPathHelper`,
  `ScreenshotPaths`, `FileNameMatcher`, `DriveHelper`, `FuseFileSystem.BuildMountArguments`).

## Debugging tips

- The UI log pane shows the same messages that are written to the daily log file; the newest log
  is the first thing to inspect.
- Press **F8** to capture the current window to the `Screenshot` folder (see
  [Screenshots](screenshots.md)).
- Startup failures are logged before the UI exists; run the executable from a terminal to see
  console output.
- `BugReportSink` only forwards Warning+ events. To reproduce a bug report locally, enable
  sending with `BugReportClient.IsSendingEnabled = true` (production entry points do this) and
  watch the log file for `BugReportClient:` messages.

## The Tester tool

`CHDMounter_Tester` is a Windows-only WPF tool for batch-parsing folders of CHD files:

1. Select a folder containing `.chd` files.
2. Choose a console type.
3. Click **Run Tests** to parse every file and collect statistics.
4. Click **Export PDF** to save a report.

It is intentionally not published in releases; build it from source with
`dotnet run --project CHDMounter_Tester/CHDMounter_Tester.csproj`.

## Adding support for new console formats

Console file system parsers and the alias registry live in the external
[VideoGameFileSystemParser](https://github.com/drpetersonfernandes/VideoGameFileSystemParser)
package — not in this repository. To add a format, contribute the parser there, then bump the
`VideoGameFileSystemParser` package reference in `CHDMounter/CHDMounter.csproj`. The alias list
shown in the UI and accepted by `/s:<alias>` updates automatically because it is read from the
library's `ParserFactory`.

## Documentation

This documentation lives in `docs/`. Add a page, link it from `docs/index.md`, `docs/toc.yml`
and `docs/_Sidebar.md`, then open a pull request. The docs workflow builds the site with docfx
and publishes it to GitHub Pages and the wiki.

## See also

- [Architecture](architecture.md)
- [Building from Source](building.md)
- [Privacy and Telemetry](privacy.md)
