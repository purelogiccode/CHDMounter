# Building from Source

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | The repository pins SDK `10.0.301` via `global.json` (`rollForward: latestPatch`) |
| Windows 10+ | Required to build the tests and the `CHDMounter_Tester` WPF tool (`net10.0-windows`) |
| Linux / macOS | The application project builds and runs; the tests and Tester require Windows |
| PowerShell 7+ | Required for the release packaging script (`build/pack-release.ps1`) |

## Build and test

```bash
# Restore and build the whole solution (Windows)
dotnet restore CSharp_CHDMounter.sln
dotnet build CSharp_CHDMounter.sln

# Build only the cross-platform application
dotnet build CHDMounter/CHDMounter.csproj

# Run the test suite
dotnet test CHDMounter.Tests/CHDMounter.Tests.csproj
```

The build treats missing XML documentation on public members as a warning
(`GenerateDocumentationFile`), so the application and Tester projects build with zero warnings.

## Publish a single-file executable

The application publishes as a framework-dependent single file (the .NET runtime is **not**
embedded):

```bash
dotnet publish CHDMounter/CHDMounter.csproj -c Release -r win-x64
```

Supported runtime identifiers: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`,
`osx-arm64`.

Two MSBuild settings in `CHDMounter.csproj` are important:

- `PublishSingleFile` + `SelfContained=false` — one executable per bundle.
- `IncludeAllContentForSelfExtract=true` — required by the WinFsp.NET bindings, which read their
  own assembly location to load the native DLL.

## Package release bundles

`build/pack-release.ps1` publishes each runtime and packs it into
`release_<version>_<rid>.zip`:

```powershell
# All supported runtimes (default output: CHDMounter/bin/Release)
pwsh ./build/pack-release.ps1

# A specific version and runtime
pwsh ./build/pack-release.ps1 -Version 2.0.0 -Runtime win-x64

# Multiple runtimes
pwsh ./build/pack-release.ps1 -Runtime win-x64,win-arm64
```

Every bundle contains:

- `CHDMounter` (`CHDMounter.exe` on Windows)
- `LICENSE.txt`
- `README.md`
- `WhatsNew.md`

The version defaults to the `AssemblyVersion` in `CHDMounter/CHDMounter.csproj`. For correct
Unix executable permissions, run the script on the matching platform.

## Continuous integration

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `.github/workflows/ci.yml` | Push to `master`, pull requests | Builds and tests on Windows; verifies the application builds on Linux and macOS |
| `.github/workflows/release.yml` | Tag `release_*` | Tests, packages all six runtime bundles across Windows/Linux/macOS runners and publishes a GitHub release |
| `.github/workflows/docs.yml` | Changes under `docs/` (and on release) | Builds the docfx site to GitHub Pages and syncs `docs/` to the GitHub wiki |

## Versioning and releases

1. Update `<AssemblyVersion>` / `<FileVersion>` in `CHDMounter/CHDMounter.csproj`,
   `CHDMounter_Tester/CHDMounter_Tester.csproj` and `CHDMounter.Tests/CHDMounter.Tests.csproj`.
2. Update `WhatsNew.md`.
3. Commit and push a tag named `release_<version>` (for example `release_1.2.2`).
4. The release workflow packages the bundles and creates the GitHub release; the update checker
   in running clients picks up the new release automatically.

## See also

- [Architecture](architecture.md)
- [Development Guide](development.md)
- [Installation](installation.md) — end-user installation
