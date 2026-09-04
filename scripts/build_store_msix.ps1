param(
    [string]$IdentityName = "UHDH.473259539542C",

    [string]$Publisher = "CN=9FE15EFB-BC7B-4D79-9643-4458FCDB174F",

    [string]$PublisherDisplayName = "UHDH",

    [string]$DisplayName = "내 토끼 데스크톱 펫"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$TemplateRoot = Join-Path $ProjectRoot "store-package"
$StagingRoot = Join-Path $ProjectRoot "store-staging"
$AppSource = Join-Path $ProjectRoot "dist\My Bunny Desktop Pet-win32-x64"
$AppDestination = Join-Path $StagingRoot "app"
$TemplatePath = Join-Path $TemplateRoot "Package.appxmanifest.template"
$ManifestPath = Join-Path $StagingRoot "AppxManifest.xml"
$OutputPath = Join-Path $ProjectRoot "store-output"

if ($env:OS -ne "Windows_NT") {
    throw "This build script must run on Windows."
}

if (-not (Test-Path (Join-Path $AppSource "My Bunny Desktop Pet.exe"))) {
    throw "Windows package not found. Run npm run package:win first."
}

function Escape-Xml([string]$Value) {
    return [System.Security.SecurityElement]::Escape($Value)
}

if (Test-Path $StagingRoot) {
    Remove-Item $StagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $AppDestination -Force | Out-Null
Copy-Item (Join-Path $TemplateRoot "Assets") $StagingRoot -Recurse -Force
Copy-Item (Join-Path $AppSource "*") $AppDestination -Recurse -Force

$template = Get-Content $TemplatePath -Raw -Encoding UTF8
$manifest = $template.Replace("{{IDENTITY_NAME}}", (Escape-Xml $IdentityName))
$manifest = $manifest.Replace("{{PUBLISHER}}", (Escape-Xml $Publisher))
$manifest = $manifest.Replace("{{PUBLISHER_DISPLAY_NAME}}", (Escape-Xml $PublisherDisplayName))
$manifest = $manifest.Replace("{{DISPLAY_NAME}}", (Escape-Xml $DisplayName))
[System.IO.File]::WriteAllText($ManifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))

New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
Get-ChildItem $OutputPath -Filter "*.msix" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Creating unsigned Store submission MSIX..."
$winApp = Get-Command winapp -ErrorAction SilentlyContinue
if (-not $winApp -and (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Microsoft's WinApp CLI with WinGet..."
    & winget install --id Microsoft.winappcli --source winget --accept-package-agreements --accept-source-agreements --silent
    $env:Path += ";$env:LOCALAPPDATA\Microsoft\WinGet\Links"
    $winApp = Get-Command winapp -ErrorAction SilentlyContinue
}

if ($winApp) {
    & winapp pack $StagingRoot --output $OutputPath --manifest $ManifestPath
}
elseif (Get-Command npx -ErrorAction SilentlyContinue) {
    & npx --yes @microsoft/winappcli pack $StagingRoot --output $OutputPath --manifest $ManifestPath
}
else {
    throw "WinApp CLI could not be installed. Install it with: winget install Microsoft.winappcli --source winget"
}
if ($LASTEXITCODE -ne 0) {
    throw "winapp MSIX packaging failed with exit code $LASTEXITCODE"
}

$package = Get-ChildItem $OutputPath -Filter "*.msix" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $package) {
    throw "MSIX output was not created."
}

Write-Host "Store package created: $($package.FullName)"
Write-Host "This package is intentionally unsigned; Microsoft Store signs it after certification."
