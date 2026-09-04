$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    $compiler = (Get-Command csc.exe -ErrorAction SilentlyContinue).Source
}
if (-not $compiler) { throw 'Could not locate the .NET Framework C# compiler (csc.exe).' }

$output = Join-Path $env:TEMP 'BunnyStateMachineTests.exe'
& $compiler /nologo /target:exe /out:$output `
    (Join-Path $root 'native\StateMachineTests.cs') `
    (Join-Path $root 'native\BunnyStateMachine.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$msbuildCandidates = @(
    (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source,
    'D:\VisualStudio\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
)
$msbuild = $msbuildCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if (-not $msbuild) { throw 'Could not locate MSBuild.exe.' }

$project = Join-Path $root 'native\BunnyPet.csproj'
$targetFrameworkVersion = 'v4.8'
if (-not (Test-Path -LiteralPath 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8')) {
    $targetFrameworkVersion = 'v4.8.1'
}
& $msbuild $project /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworkVersion=$targetFrameworkVersion /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $root 'native\bin\x64\Release\BunnyPet.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Build succeeded but expected output was missing: $exe" }
