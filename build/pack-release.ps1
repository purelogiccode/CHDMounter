#Requires -Version 7
<#
.SYNOPSIS
    Builds the CHDMounter release bundles.

.DESCRIPTION
    Publishes the Dokan and WinFsp variants for win-x64 and win-arm64 as
    framework-dependent single-file executables and packs each one into a zip
    bundle named release_<version>_<variant>_<rid>.zip.

    Every bundle contains CHDMounter.exe, LICENSE.txt, README.md and WhatsNew.md.
    Existing files in the output directory are preserved.

.PARAMETER Version
    Version to stamp and name the bundles. Defaults to the AssemblyVersion of
    CHDMounter/CHDMounter.csproj.

.PARAMETER OutputDir
    Directory that receives the zip bundles. Defaults to CHDMounter/bin/Release.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    ./build/pack-release.ps1
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDir,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'CHDMounter/CHDMounter.csproj'
$winFspProject = Join-Path $repoRoot 'CHDMounter_WinFsp/CHDMounter_WinFsp.csproj'

if (-not $Version) {
    $match = Select-String -Path $appProject -Pattern '<AssemblyVersion>([^<]+)</AssemblyVersion>'
    if (-not $match) { throw "Could not determine version from $appProject" }
    $Version = $match.Matches[0].Groups[1].Value
}

if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'CHDMounter/bin/Release' }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$variants = @(
    @{ Name = 'Dokan'; Project = $appProject; Exe = 'CHDMounter.exe' }
    @{ Name = 'WinFsp'; Project = $winFspProject; Exe = 'CHDMounter_WinFsp.exe' }
)
$runtimes = @('win-x64', 'win-arm64')

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "chdmounter-pack-$Version"
if (Test-Path $stagingRoot) { Remove-Item $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$created = [System.Collections.Generic.List[string]]::new()

foreach ($runtime in $runtimes) {
    foreach ($variant in $variants) {
        $publishDir = Join-Path $stagingRoot "$($variant.Name)-$runtime"
        $bundleDir = Join-Path $stagingRoot "bundle-$($variant.Name)-$runtime"
        New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null

        Write-Host "Publishing $($variant.Name) ($runtime)..."

        # Framework-dependent single file: the .NET runtime is not embedded.
        dotnet publish $variant.Project `
            -c $Configuration `
            -r $runtime `
            --self-contained false `
            -p:Version=$Version `
            -p:AssemblyVersion=$Version `
            -p:FileVersion=$Version `
            --nologo `
            -o $publishDir | Out-Host

        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $($variant.Name) $runtime" }

        $exePath = Join-Path $publishDir $variant.Exe
        if (-not (Test-Path $exePath)) { throw "Published executable not found: $exePath" }
        Copy-Item $exePath (Join-Path $bundleDir 'CHDMounter.exe') -Force

        Copy-Item (Join-Path $repoRoot 'LICENSE.txt') $bundleDir -Force
        Copy-Item (Join-Path $repoRoot 'README.md') $bundleDir -Force
        Copy-Item (Join-Path $repoRoot 'WhatsNew.md') $bundleDir -Force

        $zipName = "release_${Version}_$($variant.Name)_$runtime.zip"
        $zipPath = Join-Path $OutputDir $zipName
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path (Join-Path $bundleDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

        $created.Add($zipPath)
    }
}

Remove-Item $stagingRoot -Recurse -Force

Write-Host ''
Write-Host 'Created bundles:'
foreach ($zip in $created) {
    $size = [math]::Round((Get-Item $zip).Length / 1MB, 2)
    Write-Host "  $zip ($size MB)"
}
