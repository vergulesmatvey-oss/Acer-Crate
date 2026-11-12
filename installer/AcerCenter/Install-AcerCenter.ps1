# Acer Center — установка совместимого управляющего компонента.
# Запускать из распакованной поставки с правами администратора.
$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Запустите установку с правами администратора.' }
$destination = Join-Path $env:ProgramFiles 'Acer Center'
$serviceSource = Join-Path $PSScriptRoot 'Service\OpenSense.Service.exe'
if (-not (Test-Path -LiteralPath $serviceSource)) { throw 'Распакуйте всю папку AcerCenter вместе с Service.' }
$existing = Get-CimInstance Win32_Service -Filter "Name='OpenSense'"
$backup = Join-Path $env:ProgramData ('AcerCenter\backup-' + (Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
if ($existing) { $existing | Select-Object Name,PathName,State,StartMode | ConvertTo-Json | Set-Content (Join-Path $backup 'service.json') -Encoding UTF8 }
$settings = Join-Path $env:ProgramData 'OpenSense'
if (Test-Path -LiteralPath $settings) { Copy-Item -LiteralPath $settings -Destination (Join-Path $backup 'OpenSense') -Recurse }
if (Test-Path -LiteralPath $destination) { Copy-Item -LiteralPath $destination -Destination (Join-Path $backup 'Application') -Recurse }
$activeScheme = (& powercfg.exe /getactivescheme | Out-String)
$activeScheme | Set-Content (Join-Path $backup 'active-power-scheme.txt') -Encoding UTF8
$schemeMatch = [regex]::Match($activeScheme,'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}')
if ($schemeMatch.Success) { & powercfg.exe /export (Join-Path $backup 'active-power-scheme.pow') $schemeMatch.Value }
$wasRunning = $existing -and $existing.State -eq 'Running'
try {
    & (Join-Path $PSScriptRoot 'Ensure-PowerPlans.ps1')
    if ($existing) { Stop-Service -Name OpenSense; (Get-Service OpenSense).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(20)) }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $PSScriptRoot | Where-Object { $_.Name -notin @('Install-AcerCenter.ps1','Rollback-Service.ps1') } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force }
    $serviceExe = Join-Path $destination 'Service\OpenSense.Service.exe'
    if ($existing) {
        $changed = Invoke-CimMethod -InputObject $existing -MethodName Change -Arguments @{PathName=('"'+$serviceExe+'"');StartMode='Automatic'}
        if ($changed.ReturnValue -ne 0) { throw ('Service configuration failed: '+$changed.ReturnValue) }
    } else {
        New-Service -Name OpenSense -BinaryPathName ('"'+$serviceExe+'"') -DisplayName 'Acer Center Control' -StartupType Automatic | Out-Null
    }
    Start-Service OpenSense
    (Get-Service OpenSense).WaitForStatus('Running',[TimeSpan]::FromSeconds(20))
    Write-Host ('Установлено: ' + (Join-Path $destination 'AcerCenter.exe'))
    Write-Host ('Резервная копия: ' + $backup)
} catch {
    if ($existing) {
        $restoreService = Get-CimInstance Win32_Service -Filter "Name='OpenSense'"
        Invoke-CimMethod -InputObject $restoreService -MethodName Change -Arguments @{PathName=$existing.PathName} | Out-Null
        if ($wasRunning) { Start-Service OpenSense -ErrorAction SilentlyContinue }
    }
    throw
}
