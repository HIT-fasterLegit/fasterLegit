param([ValidateSet('restore', 'build', 'test', 'run', 'publish')][string]$Task = 'build')
$ErrorActionPreference = 'Stop'
$env:DOTNET_ROOT = Join-Path $PSScriptRoot '.tools\dotnet'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-cache\cli'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.build-cache\nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $PSScriptRoot '.build-cache\http'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:TEMP = Join-Path $PSScriptRoot '.build-cache\temp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force $env:TEMP | Out-Null
$veloraDotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
if (-not (Test-Path -LiteralPath $veloraDotnet)) {
    throw 'Install .NET 10 SDK into fasterLegit/.tools/dotnet. See README.md.'
}
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    switch ($Task) {
        'restore' { & $veloraDotnet restore fasterLegit.slnx }
        'build' { & $veloraDotnet build fasterLegit.slnx --configuration Release --no-restore }
        'test' { & $veloraDotnet run --project fasterLegit.Test/fasterLegit.Test.csproj --configuration Release --no-build -- -longRunning 30 }
        'run' { & $veloraDotnet run --project fasterLegit/fasterLegit.csproj --configuration Release --no-build }
        'publish' { & $veloraDotnet publish fasterLegit/fasterLegit.csproj --configuration Release --runtime win-x64 --self-contained true --output (Join-Path $PSScriptRoot 'Release\VELORA') }
    }
    if ($LASTEXITCODE -ne 0) { throw "VELORA $Task failed (exit $LASTEXITCODE)." }
} finally { Pop-Location }
