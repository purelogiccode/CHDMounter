#Requires -Version 7
<#
.SYNOPSIS
    Builds the CHDMounter release bundles.

.DESCRIPTION
    Publishes the merged CHDMounter application as a framework-dependent single-file
    executable for each requested runtime identifier and packs each one into a zip
    bundle named release_<version>_<rid>.zip.

    Every bundle contains CHDMounter (CHDMounter.exe on Windows), LICENSE.txt,
    README.md and WhatsNew.md. Existing files in the output directory are preserved.

    Cross-publishing from a Windows host works for all runtimes; for native permission
    handling (executable bit) run the script on the matching platform.

.PARAMETER Version
    Version to stamp and name the bundles. Defaults to the AssemblyVersion of
    CHDMounter/CHDMounter.csproj.

.PARAMETER OutputDir
    Directory that receives the zip bundles. Defaults to CHDMounter/bin/Release.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER Runtime
    Comma-separated runtime identifiers to publish. Defaults to all supported
    runtimes: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64.

.EXAMPLE
    ./build/pack-release.ps1

.EXAMPLE
    ./build/pack-release.ps1 -Runtime win-x64,win-arm64
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDir,
    [string]$Configuration = 'Release',
    [string]$Runtime
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'CHDMounter/CHDMounter.csproj'

if (-not $Version) {
    $match = Select-String -Path $appProject -Pattern '<AssemblyVersion>([^<]+)</AssemblyVersion>'
    if (-not $match) { throw "Could not determine version from $appProject" }
    $Version = $match.Matches[0].Groups[1].Value
}

if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'CHDMounter/bin/Release' }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$runtimes = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')
if ($Runtime) {
    $runtimes = $Runtime.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "chdmounter-pack-$Version"
if (Test-Path $stagingRoot) { Remove-Item $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$created = [System.Collections.Generic.List[string]]::new()

foreach ($runtime in $runtimes) {
    $publishDir = Join-Path $stagingRoot "publish-$runtime"
    $bundleDir = Join-Path $stagingRoot "bundle-$runtime"
    New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null

    Write-Host "Publishing CHDMounter ($runtime)..."

    # Framework-dependent single file: the .NET runtime is not embedded.
    dotnet publish $appProject `
        -c $Configuration `
        -r $runtime `
        --self-contained false `
        -p:Version=$Version `
        -p:AssemblyVersion=$Version `
        -p:FileVersion=$Version `
        --nologo `
        -o $publishDir | Out-Host

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $runtime" }

    $exeName = if ($runtime.StartsWith('win-')) { 'CHDMounter.exe' } else { 'CHDMounter' }
    $exePath = Join-Path $publishDir $exeName
    if (-not (Test-Path $exePath)) { throw "Published executable not found: $exePath" }

    $bundledExe = Join-Path $bundleDir $exeName
    Copy-Item $exePath $bundledExe -Force

    if (-not $IsWindows) {
        # Preserve the executable bit for Unix bundles.
        $mode = [System.IO.File]::GetUnixFileMode($bundledExe)
        $mode = $mode -bor [System.IO.UnixFileMode]::UserExecute -bor [System.IO.UnixFileMode]::GroupExecute -bor [System.IO.UnixFileMode]::OtherExecute
        [System.IO.File]::SetUnixFileMode($bundledExe, $mode)
    }

    Copy-Item (Join-Path $repoRoot 'LICENSE.txt') $bundleDir -Force
    Copy-Item (Join-Path $repoRoot 'README.md') $bundleDir -Force
    Copy-Item (Join-Path $repoRoot 'WhatsNew.md') $bundleDir -Force

    $zipName = "release_${Version}_$runtime.zip"
    $zipPath = Join-Path $OutputDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    if ($IsWindows) {
        Compress-Archive -Path (Join-Path $bundleDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    }
    else {
        # Info-ZIP stores Unix permissions, which keeps the executable bit on extraction.
        Push-Location $bundleDir
        try {
            & zip -r -q "$zipPath" .
            if ($LASTEXITCODE -ne 0) { throw "zip failed for $runtime" }
        }
        finally {
            Pop-Location
        }
    }

    $created.Add($zipPath)
}

Remove-Item $stagingRoot -Recurse -Force

Write-Host ''
Write-Host 'Created bundles:'
foreach ($zip in $created) {
    $size = [math]::Round((Get-Item $zip).Length / 1MB, 2)
    Write-Host "  $zip ($size MB)"
}
