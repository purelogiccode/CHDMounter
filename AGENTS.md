# AGENTS.md

Guidance for AI coding agents and automation working in this repository.

## Project overview

CHDMounter is a .NET 10 / Avalonia desktop application that mounts CHD (Compressed Hunks of
Data) CD/DVD images as read-only virtual drives. It runs on Windows, Linux and macOS, supports
37 console/format modes, and can use Dokan or WinFsp on Windows and FUSE on Linux/macOS as the
mount core.

- `CHDMounter/` - cross-platform Avalonia application.
- `CHDMounter.Tests/` - xUnit unit and integration tests (Windows only, `net10.0-windows`).
- `CHDMounter_Tester/` - Windows-only WPF batch-parsing/benchmarking developer tool.
- `build/` - release packaging script.
- `docs/` - docfx documentation site and GitHub wiki source.

The solution targets **.NET 10** and pins SDK `10.0.301` in `global.json`. The application is
framework-dependent and is published as a single file per runtime.

## Before you start

1. Read `README.md`, `docs/development.md` and `docs/building.md`.
2. Check the current branch and working tree with `git status --short`.
3. Do not read or modify other repositories. Work only inside this checkout.
4. Never commit `.repofleet-task.md`; it is a temporary harness file.
5. Never commit build output (`bin/`, `obj/`, `TestResults/`, `artifacts/`, `docs/_site/`).

## Build, test and run

```bash
# Restore the whole solution (Windows)
dotnet restore CSharp_CHDMounter.sln

# Build the whole solution (Windows; tests and Tester are Windows-only)
dotnet build CSharp_CHDMounter.sln -c Release

# Build only the cross-platform application (Linux/macOS)
dotnet build CHDMounter/CHDMounter.csproj -c Release

# Run the test suite (Windows)
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj -c Release

# Run one test class
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj --filter "FullyQualifiedName~MountPathHelperTests"

# Run the application
dotnet run --project CHDMounter/CHDMounter.csproj

# Pack release bundles (PowerShell 7+)
pwsh ./build/pack-release.ps1 -Version 2.0.0 -Runtime win-x64
```

The build must stay warning-free. `Meziantou.Analyzer` runs on every project, nullable reference
types are enabled, and public members require XML documentation.

## Repository conventions

- Use **file-scoped namespaces** and the existing formatting/style of the surrounding code.
- Keep implementation classes `internal`; only expose contracts and types needed by the Tester or
  tests. Tests and the Tester use `InternalsVisibleTo`.
- Async methods end in `Async`, including event handlers.
- Log through Serilog (`ILoggingService` or `Serilog.Log`). Do not use `Console.WriteLine` or
  `Debug.WriteLine` for diagnostics.
- User/environment errors use `LoggingService.LogUserError` (Information level); genuine failures
  use `LogError` with the exception.
- Catch blocks must log. Silent catches are only allowed for documented last-resort paths.
- Do not modify `.chd` files or the bundled `chdman.exe`/`chdman_arm64.exe` binaries unless the
  task explicitly requires it.
- Update `README.md`, `docs/` and `WhatsNew.md` when behavior, CLI options, dependencies or
  release steps change.

## Architecture notes

- `CHDMounter/Program.cs` and `CHDMounter/App.axaml.cs` are the application entry points.
- `CHDMounter/Services/` contains logging, settings (DPAPI on Windows, JSON elsewhere), update
  checking, bug reporting, screenshots and statistics.
- `CHDMounter/Mounting/` contains the mount router and the Dokan, WinFsp and FUSE backends.
- CHD reading is provided by the `CHDSharp` package; filesystem parsing is provided by
  `VideoGameFileSystemParser`. The parser alias registry lives in that external package.
- `ChdContainer` bridges the parsed `FsNode` tree to the driver-specific VFS layer and also
  generates virtual CUE/BIN/ISO/WAV exports.
- WinFsp requires `IncludeAllContentForSelfExtract=true` so `winfsp-msil.dll` is available on
  disk; do not remove that setting from `CHDMounter.csproj`.

## Testing notes

- The suite contains both unit and integration tests. Integration tests read real CHD files from
  `ChdPathCatalog`; missing directories are skipped, so the suite passes without a game library.
- `TestInitializer` disables bug reporting for the whole test process. Do not send real bug
  reports from tests.
- The test project and `CHDMounter_Tester` target `net10.0-windows`; they cannot run on Linux or
  macOS.
- Run the full test suite before finishing whenever the change can affect runtime behavior.

## Continuous integration

Workflows live in `.github/workflows/`:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push to `master`, pull requests, manual | Builds and tests on Windows, builds the app on Linux/macOS, packages all six runtime bundles, and builds the documentation site |
| `codeql.yml` | Push to `master`, pull requests, weekly schedule, manual | CodeQL security analysis for C# |
| `dependency-review.yml` | Pull requests | Blocks new high-severity vulnerable dependencies |
| `docs.yml` | Docs changes, release, manual | Deploys the docfx site to GitHub Pages and syncs the wiki |
| `release.yml` | Tag `release_*` | Tests, packages all runtime bundles and publishes a GitHub release |

Keep workflow permissions least-privilege, pin actions to the major versions already used, and do
not add secrets to workflows unless the task requires them.

## Release process

1. Bump `<AssemblyVersion>` and `<FileVersion>` in `CHDMounter/CHDMounter.csproj`,
   `CHDMounter_Tester/CHDMounter_Tester.csproj` and `CHDMounter.Tests/CHDMounter.Tests.csproj`.
2. Update `WhatsNew.md` (and the generated `docs/changelog.md` if needed).
3. Commit and push a tag named `release_<version>` (for example `release_1.3.2`).
4. `release.yml` packages `release_<version>_<rid>.zip` for all six runtime identifiers and
   creates the GitHub release.

## Security

- Follow `SECURITY.md`. Report vulnerabilities privately through GitHub Security Advisories; do
  not open a public issue with exploit details.
- Dependabot alerts, Dependabot security updates, secret scanning, push protection, private
  vulnerability reporting and CodeQL are enabled for this repository.
- Never commit credentials, tokens, signing keys, SSH keys or personal data. Settings files use
  DPAPI on Windows; do not weaken that protection.
- Do not log file contents, user paths or other sensitive data unnecessarily.

## Documentation

The documentation source is `docs/`. When adding a page, link it from `docs/index.md`,
`docs/toc.yml` and `docs/_Sidebar.md`. The docs workflow builds with docfx and publishes to
GitHub Pages and the wiki. Keep `README.md` and `WhatsNew.md` consistent with user-facing changes.
