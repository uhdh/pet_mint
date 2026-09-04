param(
    [string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "dist\My Bunny Desktop Pet-win32-x64")
)

$LocalePath = Join-Path $PackagePath "locales"
if (-not (Test-Path $LocalePath)) {
    throw "Electron locale directory not found: $LocalePath"
}

Get-ChildItem $LocalePath -File -Filter "*.pak" |
    Where-Object BaseName -NotIn "ko", "en-US" |
    Remove-Item -Force
