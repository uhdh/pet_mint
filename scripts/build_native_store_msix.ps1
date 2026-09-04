param(
    [string]$IdentityName = "UHDH.473259539542C",
    [string]$Publisher = "CN=9FE15EFB-BC7B-4D79-9643-4458FCDB174F",
    [string]$PublisherDisplayName = "UHDH",
    [string]$DisplayName = [string]::Concat([char]0xB0B4, ' ', [char]0xD1A0, [char]0xB07C, ' ', [char]0xB370, [char]0xC2A4, [char]0xD06C, [char]0xD1B1, ' ', [char]0xD3AB)
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot)).TrimEnd('\')
$TemplateRoot = Join-Path $ProjectRoot "store-package"
$StagingRoot = Join-Path $ProjectRoot "native-store-staging"
$AppDestination = Join-Path $StagingRoot "app"
$OutputRoot = Join-Path $ProjectRoot "store-output"
$PackagePath = Join-Path $OutputRoot "BunnyPet-native_1.0.1.0_x64.msix"
$TemplatePath = Join-Path $TemplateRoot "Package.appxmanifest.template"
$ManifestPath = Join-Path $StagingRoot "AppxManifest.xml"

function Assert-InProject([string]$Path, [string]$Label) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $prefix = "$ProjectRoot\"
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label resolved outside project root: $resolved"
    }
    return $resolved
}

function Assert-StagedPackage([string]$Path) {
    $forbidden = @('electron.exe', 'resources.pak', 'icudtl.dat', 'node.dll')
    $files = @(Get-ChildItem -LiteralPath $Path -File -Recurse)
    foreach ($file in $files) {
        if ($file.Length -gt 10MB) { throw "Staged file exceeds 10 MB: $($file.FullName)" }
        $name = $file.Name.ToLowerInvariant()
        if (($forbidden -contains $name) -or ($name -like 'chrome_*.pak')) {
            throw "Forbidden Electron runtime file in staging: $($file.FullName)"
        }
    }
    Write-Host "Staged package validation passed: $($files.Count) files."
}

function Assert-Msix([string]$Path) {
    $package = Get-Item -LiteralPath $Path -ErrorAction Stop
    if ($package.Length -gt 5MB) { throw "MSIX exceeds 5 MB: $($package.Length) bytes" }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $forbidden = @('electron.exe', 'resources.pak', 'icudtl.dat', 'node.dll')
        $entries = @($archive.Entries)
        foreach ($entry in $entries) {
            $name = $entry.Name.ToLowerInvariant()
            if (($forbidden -contains $name) -or ($name -like 'chrome_*.pak')) {
                throw "Forbidden Electron runtime file in MSIX: $($entry.FullName)"
            }
        }
    }
    finally { $archive.Dispose() }
    Write-Host "MSIX size: $($package.Length) bytes."
    Write-Host "Archive validation passed: $($entries.Count) entries; no forbidden Electron runtime names."
}

if ($env:OS -ne "Windows_NT") { throw "This build script must run on Windows." }

$testScript = Join-Path $PSScriptRoot "test-native.ps1"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testScript
if ($LASTEXITCODE -ne 0) { throw "Native tests/build failed with exit code $LASTEXITCODE" }
$nativeExe = Join-Path $ProjectRoot "native\bin\x64\Release\BunnyPet.exe"
if (-not (Test-Path -LiteralPath $nativeExe -PathType Leaf)) { throw "Native build output is missing: $nativeExe" }
Write-Host "Native tests and x64 Release build passed."

$stagingPath = Assert-InProject $StagingRoot "Staging path"
if (Test-Path -LiteralPath $stagingPath) { Remove-Item -LiteralPath $stagingPath -Recurse -Force }
New-Item -ItemType Directory -Path $AppDestination -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $TemplateRoot "Assets") -Destination $StagingRoot -Recurse -Force
Copy-Item -LiteralPath $nativeExe -Destination (Join-Path $AppDestination "BunnyPet.exe") -Force

$template = Get-Content -LiteralPath $TemplatePath -Raw -Encoding UTF8
function Escape-Xml([string]$Value) { [Security.SecurityElement]::Escape($Value) }
$manifest = $template.Replace("{{IDENTITY_NAME}}", (Escape-Xml $IdentityName))
$manifest = $manifest.Replace("{{PUBLISHER}}", (Escape-Xml $Publisher))
$manifest = $manifest.Replace("{{PUBLISHER_DISPLAY_NAME}}", (Escape-Xml $PublisherDisplayName))
$manifest = $manifest.Replace("{{DISPLAY_NAME}}", (Escape-Xml $DisplayName))
$manifest = $manifest.Replace("app\My Bunny Desktop Pet.exe", "app\BunnyPet.exe")
if ($manifest.Contains('{{')) { throw "Unresolved manifest token remains." }
[xml]$manifest | Out-Null
[IO.File]::WriteAllText($ManifestPath, $manifest, [Text.UTF8Encoding]::new($false))

Assert-StagedPackage $stagingPath
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$winApp = Get-Command winapp.exe -ErrorAction SilentlyContinue
if (-not $winApp) { throw "WinApp CLI is required: install winapp before packaging." }
if (Test-Path -LiteralPath $PackagePath) { Remove-Item -LiteralPath $PackagePath -Force }
Write-Host "Creating unsigned Store submission MSIX..."
& $winApp.Source pack $stagingPath --output $PackagePath --manifest $ManifestPath
if ($LASTEXITCODE -ne 0) { throw "winapp pack failed with exit code $LASTEXITCODE" }
if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) { throw "MSIX output was not created: $PackagePath" }

Assert-Msix $PackagePath
Write-Host "Store package created: $([IO.Path]::GetFullPath($PackagePath))"
Write-Host "This package is intentionally unsigned; Microsoft Store signs it after certification."
