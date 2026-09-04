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
