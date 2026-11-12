param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$ManifestHash,
    [Parameter(Mandatory=$true)][string]$ResultPath,
    [Parameter(Mandatory=$true)][string]$InstallDirectory
)
$ErrorActionPreference = 'Stop'
$destination = [IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
$manifestPath = Join-Path $PackagePath 'AcerCrate.manifest.json'
$backup = $null
$existing = $null
$wasRunning = $false
$serviceChanged = $false
$copied = $false
$transcriptStarted = $false
try {
    if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $ManifestHash) { throw 'Нарушена целостность комплекта установки.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packageRoot = [IO.Path]::GetFullPath($PackagePath).TrimEnd('\') + '\'
    foreach ($file in $manifest.Files) {
        $path = [IO.Path]::GetFullPath((Join-Path $PackagePath $file.Path))
        if (-not $path.StartsWith($packageRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Некорректный путь в комплекте.' }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256) { throw ('Повреждён файл: ' + $file.Path) }
    }
    foreach ($relative in @('AcerCrate.runtimeconfig.json','Service\OpenSense.Service.runtimeconfig.json')) {
        $runtime = Get-Content -LiteralPath (Join-Path $PackagePath $relative) -Raw | ConvertFrom-Json
        if (-not $runtime.runtimeOptions.includedFrameworks -or $runtime.runtimeOptions.frameworks) { throw 'В комплекте отсутствует встроенная среда .NET.' }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $PackagePath 'coreclr.dll')) -or -not (Test-Path -LiteralPath (Join-Path $PackagePath 'Service\coreclr.dll'))) { throw 'Комплект среды запуска неполон.' }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Для установки нужны права администратора.' }
    $backup = Join-Path ([IO.Path]::GetDirectoryName($destination)) ('AcerCrate Backups\backup-setup-' + (Get-Date -Format yyyyMMdd-HHmmss))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Start-Transcript -Path (Join-Path $backup 'installation.log') -Force | Out-Null
    $transcriptStarted = $true
    $existing = Get-CimInstance Win32_Service -Filter "Name='OpenSense'"
    $wasRunning = $existing -and $existing.State -eq 'Running'
    if ($existing) { $existing | Select-Object Name,PathName,StartMode,State | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'service.json') -Encoding UTF8 }
    foreach ($item in @(
        @{Path=(Join-Path $env:APPDATA 'OpenSense');Name='UserSettings'},
        @{Path=(Join-Path $env:ProgramData 'OpenSense');Name='MachineSettings'})) {
        if (Test-Path -LiteralPath $item.Path) { Copy-Item -LiteralPath $item.Path -Destination (Join-Path $backup $item.Name) -Recurse -Force }
    }
    if (Test-Path -LiteralPath $destination) { Copy-Item -LiteralPath $destination -Destination (Join-Path $backup 'Application') -Recurse -Force }
    $exit = Start-Process -FilePath (Join-Path $PackagePath 'AcerCrate.exe') -ArgumentList '--exit' -WindowStyle Hidden -Wait -PassThru
    if ($exit.ExitCode -ne 0) { throw 'Закройте Acer Crate и повторите установку.' }
    $deadline = (Get-Date).AddSeconds(15)
    do {
        $open = @(Get-Process -Name AcerCrate -ErrorAction SilentlyContinue | Where-Object { $_.Path -and [IO.Path]::GetDirectoryName($_.Path) -eq $destination })
        if ($open.Count -eq 0) { break }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    if ($open.Count -gt 0) { throw 'Acer Crate ещё открыт. Закройте его во всех сеансах Windows.' }
    if ($existing) {
        Stop-Service -Name OpenSense
        (Get-Service OpenSense).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
        if ($existing.ProcessId -gt 0) { Wait-Process -Id $existing.ProcessId -Timeout 15 -ErrorAction SilentlyContinue }
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    # A service installed outside Program Files still needs an administrator-owned executable directory.
    $security = New-Object Security.AccessControl.DirectorySecurity
    $security.SetAccessRuleProtection($true,$false)
    $security.SetOwner((New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')))
    foreach ($sid in @('S-1-5-18','S-1-5-32-544','S-1-5-32-545')) {
        $rights = if ($sid -eq 'S-1-5-32-545') { [Security.AccessControl.FileSystemRights]::ReadAndExecute } else { [Security.AccessControl.FileSystemRights]::FullControl }
        $rule = New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($sid)),$rights,([Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'),[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow)
        $security.AddAccessRule($rule)
    }
    Set-Acl -LiteralPath $destination -AclObject $security
    $copied = $true
    foreach ($file in $manifest.Files) {
        $target = Join-Path $destination $file.Path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $PackagePath $file.Path) -Destination $target -Force
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.Sha256) { throw ('Не прошла проверка установленного файла: ' + $file.Path) }
    }
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $destination 'AcerCrate.manifest.json') -Force
    & (Join-Path $PSScriptRoot 'Ensure-PowerPlans.ps1')
    $serviceExe = Join-Path $destination 'Service\OpenSense.Service.exe'
    if ($existing) {
        $change = Invoke-CimMethod -InputObject $existing -MethodName Change -Arguments @{PathName=('"'+$serviceExe+'"');StartMode='Automatic';DisplayName='Acer Crate Control'}
        if ($change.ReturnValue -ne 0) { throw ('Не удалось обновить службу: ' + $change.ReturnValue) }
    } else {
        New-Service -Name OpenSense -BinaryPathName ('"'+$serviceExe+'"') -DisplayName 'Acer Crate Control' -StartupType Automatic -DependsOn Winmgmt | Out-Null
    }
    $serviceChanged = $true
    & sc.exe failure OpenSense reset= 86400 actions= restart/5000/restart/10000/restart/60000 | Out-Null
    Start-Service -Name OpenSense
    (Get-Service OpenSense).WaitForStatus('Running',[TimeSpan]::FromSeconds(30))
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    $run = Get-ItemProperty -LiteralPath $runKey -Name OpenSense -ErrorAction SilentlyContinue
    if ($run -and $run.OpenSense) {
        Set-ItemProperty -LiteralPath $runKey -Name OpenSense -Value ('"'+(Join-Path $destination 'AcerCrate.exe')+'" --autostart')
    }
    @{Success=$true;Time=(Get-Date -Format o);Backup=$backup;Version=$manifest.Version} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    exit 0
} catch {
    $failure = $_.Exception.Message
    try {
        if ($serviceChanged) { Stop-Service -Name OpenSense -ErrorAction SilentlyContinue }
        if ($copied -and $backup -and (Test-Path -LiteralPath (Join-Path $backup 'Application'))) {
            Get-ChildItem -LiteralPath (Join-Path $backup 'Application') | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force }
        }
        if ($existing) {
            $restore = Get-CimInstance Win32_Service -Filter "Name='OpenSense'"
            if ($restore) {
                $restoreStart = if ($existing.StartMode -eq 'Auto') { 'Automatic' } else { $existing.StartMode }
                $restored = Invoke-CimMethod -InputObject $restore -MethodName Change -Arguments @{PathName=$existing.PathName;StartMode=$restoreStart}
                if ($restored.ReturnValue -ne 0) { throw ('Не восстановлена конфигурация службы: ' + $restored.ReturnValue) }
            }
            if ($wasRunning) { Start-Service -Name OpenSense }
        } elseif ($serviceChanged) { & sc.exe delete OpenSense | Out-Null }
    } catch { $failure += ' Возврат предыдущей версии: ' + $_.Exception.Message }
    @{Success=$false;Time=(Get-Date -Format o);Backup=$backup;Error=$failure} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    if ($backup) { $failure | Set-Content -LiteralPath (Join-Path $backup 'error.txt') -Encoding UTF8 }
    Write-Error $failure -ErrorAction Continue
    exit 1
} finally { if ($transcriptStarted) { Stop-Transcript | Out-Null } }
