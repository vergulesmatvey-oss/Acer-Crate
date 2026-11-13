$ErrorActionPreference = 'Stop'
$taskSourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Use-BuildEnvironment.ps1')
$taskOutput = Join-Path $taskSourceRoot '.acercrate-dev\outputs'
$taskStatus = Join-Path $taskOutput 'update-status.json'
try {
    # The already-published app and service are reused. No SDK download or compilation is needed here.
    & (Join-Path $PSScriptRoot 'Build-AcerCrate.ps1') -SkipPublish
    $taskSetup = Join-Path $taskOutput 'AcerCrate-Setup-x64.exe'
    if (-not (Test-Path -LiteralPath $taskSetup)) { throw 'Installer was not created.' }
    Write-Host 'Acer Crate 1.0.1 is ready. The installer will open; choose a folder on D:.'
    $taskInstall = Start-Process -FilePath $taskSetup -ArgumentList '/Relaunch' -Verb RunAs -Wait -PassThru
    if ($taskInstall.ExitCode -ne 0) { throw ('Installer exit code: ' + $taskInstall.ExitCode) }
    $taskRegistration = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AcerCrate'
    $taskExpected = ([xml](Get-Content -LiteralPath (Join-Path $taskSourceRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
    if ($taskRegistration.DisplayVersion -ne $taskExpected) { throw 'The new version was not installed, or installation was cancelled.' }
    if (-not $taskRegistration.InstallLocation.StartsWith('D:\',[StringComparison]::OrdinalIgnoreCase)) { throw 'The selected installation folder is not on D:.' }
    $taskService = Get-CimInstance Win32_Service -Filter "Name='AcerCrate'"
    if ($taskService.State -ne 'Running') { throw 'Installation completed, but the control service is not running.' }
    @{Success=$true;Version=$taskRegistration.DisplayVersion;InstallLocation=$taskRegistration.InstallLocation;ServiceState=$taskService.State;Time=(Get-Date -Format o)} | ConvertTo-Json | Set-Content -LiteralPath $taskStatus -Encoding UTF8
    Write-Host ('Installed: Acer Crate ' + $taskRegistration.DisplayVersion + ' in ' + $taskRegistration.InstallLocation)
} catch {
    @{Success=$false;Error=$_.Exception.Message;Time=(Get-Date -Format o)} | ConvertTo-Json | Set-Content -LiteralPath $taskStatus -Encoding UTF8
    Write-Error $_ -ErrorAction Continue
    exit 1
}
