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

$referenceAssemblyCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'),
    (Join-Path $env:ProgramFiles 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8')
)
$referenceAssemblies = $referenceAssemblyCandidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
    Select-Object -First 1
if (-not $referenceAssemblies) {
    throw 'Exact .NET Framework v4.8 reference assemblies are required. Install the Microsoft .NET Framework 4.8 Developer Pack.'
}
$serializationReference = Join-Path $referenceAssemblies 'System.Runtime.Serialization.dll'
if (-not (Test-Path -LiteralPath $serializationReference -PathType Leaf)) {
    throw "The .NET Framework v4.8 reference assembly is missing: $serializationReference"
}
$testReferences = @('mscorlib.dll', 'System.dll', 'System.Runtime.Serialization.dll', 'System.Xml.dll') |
    ForEach-Object {
        $path = Join-Path $referenceAssemblies $_
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "The .NET Framework v4.8 reference assembly is missing: $path"
        }
        "/r:$path"
    }

$output = Join-Path $env:TEMP 'BunnyStateMachineTests.exe'
& $compiler /noconfig /nostdlib /nologo /target:exe /out:$output `
    (Join-Path $root 'native\StateMachineTests.cs') `
    (Join-Path $root 'native\BunnyStateMachine.cs') `
    (Join-Path $root 'native\AppSettings.cs') `
    $testReferences
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
$projectXml = Get-Content -LiteralPath $project -Raw
$appSource = Get-Content -LiteralPath (Join-Path $root 'native\App.xaml.cs') -Raw
if ($projectXml -match 'GlobalInputWatcher\.cs' -or $appSource -match '\.Kill\s*\(') {
    throw 'Store build must not install a global keyboard hook or terminate other processes.'
}
$targetFrameworkVersion = 'v4.8'
$targetFrameworkRoot = Split-Path -Parent (Split-Path -Parent $referenceAssemblies)
& $msbuild $project /t:Rebuild /p:Configuration=Release /p:Platform=x64 "/p:TargetFrameworkVersion=$targetFrameworkVersion" "/p:TargetFrameworkRootPath=$targetFrameworkRoot" /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $root 'native\bin\x64\Release\BunnyPet.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Build succeeded but expected output was missing: $exe" }

try {
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($exe)
    $attribute = [Reflection.CustomAttributeData]::GetCustomAttributes($assembly) |
        Where-Object { $_.AttributeType.FullName -eq 'System.Runtime.Versioning.TargetFrameworkAttribute' } |
        Select-Object -First 1
    $actualTargetFramework = if ($attribute) { [string]$attribute.ConstructorArguments[0].Value } else { $null }
}
catch {
    throw "Could not inspect TargetFrameworkAttribute in $exe. $($_.Exception.Message)"
}
if ($actualTargetFramework -ne '.NETFramework,Version=v4.8') {
    throw "Native executable TargetFrameworkAttribute must be .NETFramework,Version=v4.8 but was '$actualTargetFramework'."
}
Write-Host "TargetFrameworkAttribute: $actualTargetFramework"
