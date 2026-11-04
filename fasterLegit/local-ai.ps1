param(
    [ValidateSet('install', 'serve', 'pull', 'status')][string]$Task = 'status',
    [string]$Model = 'qwen3:4b'
)
$ErrorActionPreference = 'Stop'
$veloraAiRoot = Join-Path $PSScriptRoot '.tools\ollama'
$veloraRuntime = Join-Path $veloraAiRoot 'runtime\ollama.exe'
$veloraModels = Join-Path $veloraAiRoot 'models'
$veloraProfile = Join-Path $veloraAiRoot 'profile'
$veloraTemp = Join-Path $veloraAiRoot 'temp'
foreach ($veloraDirectory in @($veloraAiRoot, $veloraModels, $veloraProfile, $veloraTemp)) {
    New-Item -ItemType Directory -Force -Path $veloraDirectory | Out-Null
}
if ($Task -eq 'install') {
    if (Test-Path -LiteralPath $veloraRuntime) { Write-Output 'Portable Ollama is already installed beside the project.'; return }
    $veloraArchive = Join-Path $veloraAiRoot 'ollama-windows-amd64.zip'
    $veloraExpectedHash = '65d68cade6cf6375f398b518224f8c8a15e062387f38cf477abfc848de7e7e29'
    $ProgressPreference = 'SilentlyContinue'
    if (-not (Test-Path -LiteralPath $veloraArchive)) {
        Invoke-WebRequest 'https://github.com/ollama/ollama/releases/download/v0.40.14/ollama-windows-amd64.zip' -OutFile $veloraArchive
    }
    if ((Get-FileHash -LiteralPath $veloraArchive -Algorithm SHA256).Hash -ne $veloraExpectedHash) { throw 'Ollama SHA-256 mismatch.' }
    Expand-Archive -LiteralPath $veloraArchive -DestinationPath (Join-Path $veloraAiRoot 'runtime')
    Write-Output 'Ollama v0.40.14 installed inside the project. License: https://github.com/ollama/ollama/blob/main/LICENSE'
    return
}
if ($Task -eq 'status') {
    Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 5 | ConvertTo-Json -Depth 5
    return
}
if (-not (Test-Path -LiteralPath $veloraRuntime)) { throw 'Run local-ai.ps1 install first.' }
if ($Model -notmatch '^[A-Za-z0-9._:/-]{1,120}$' -or $Model -match '(:cloud|-cloud)$') { throw 'Choose a valid local model name.' }
$veloraStartInfo = [Diagnostics.ProcessStartInfo]::new()
$veloraStartInfo.FileName = $veloraRuntime
$veloraStartInfo.WorkingDirectory = $veloraAiRoot
$veloraStartInfo.UseShellExecute = $false
$veloraStartInfo.CreateNoWindow = $true
$veloraStartInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$veloraStartInfo.Environment['OLLAMA_MODELS'] = $veloraModels
$veloraStartInfo.Environment['OLLAMA_HOST'] = '127.0.0.1:11434'
$veloraStartInfo.Environment['OLLAMA_NO_CLOUD'] = '1'
$veloraStartInfo.Environment['OLLAMA_CONTEXT_LENGTH'] = '8192'
$veloraStartInfo.Environment['OLLAMA_MAX_LOADED_MODELS'] = '1'
$veloraStartInfo.Environment['OLLAMA_KEEP_ALIVE'] = '0'
$veloraStartInfo.Environment['OLLAMA_TMPDIR'] = $veloraTemp
$veloraStartInfo.Environment['TEMP'] = $veloraTemp
$veloraStartInfo.Environment['TMP'] = $veloraTemp
$veloraStartInfo.Environment['USERPROFILE'] = $veloraProfile
$veloraStartInfo.Environment['LOCALAPPDATA'] = Join-Path $veloraProfile 'Local'
$veloraStartInfo.Environment['APPDATA'] = Join-Path $veloraProfile 'Roaming'
if ($Task -eq 'serve') {
    $veloraExisting = $null
    try { $veloraExisting = Invoke-RestMethod 'http://127.0.0.1:11434/api/version' -TimeoutSec 2 } catch { }
    if ($null -ne $veloraExisting) { throw 'An Ollama server already uses port 11434. Keep it or stop it yourself before starting the project-owned server.' }
    $veloraStartInfo.ArgumentList.Add('serve')
    $veloraServer = [Diagnostics.Process]::Start($veloraStartInfo)
    $veloraServer.Id | Set-Content -LiteralPath (Join-Path $veloraAiRoot 'server.pid')
    Write-Output "Ollama server started (PID $($veloraServer.Id)); models: $veloraModels"
} else {
    $veloraServerIdPath = Join-Path $veloraAiRoot 'server.pid'
    if (-not (Test-Path -LiteralPath $veloraServerIdPath)) { throw 'Start the project-owned server with local-ai.ps1 serve before pulling a model.' }
    $veloraServerProcess = Get-Process -Id ([int](Get-Content -LiteralPath $veloraServerIdPath)) -ErrorAction Stop
    if ($veloraServerProcess.Path -ne $veloraRuntime) { throw 'The saved server PID does not belong to this portable runtime.' }
    $veloraStartInfo.ArgumentList.Add('pull')
    $veloraStartInfo.ArgumentList.Add($Model)
    $veloraPull = [Diagnostics.Process]::Start($veloraStartInfo)
    $veloraPull.WaitForExit()
    if ($veloraPull.ExitCode -ne 0) { throw 'Model download failed.' }
    Write-Output "Local model ready: $Model"
}
