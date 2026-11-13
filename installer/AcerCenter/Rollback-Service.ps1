param([Parameter(Mandatory=$true)][string]$BackupDirectory)
$ErrorActionPreference='Stop'
$record=Get-Content -LiteralPath (Join-Path $BackupDirectory 'service.json') -Raw | ConvertFrom-Json
Stop-Service AcerCrate
(Get-Service AcerCrate).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(20))
$applicationBackup=Join-Path $BackupDirectory 'Application'
if(Test-Path -LiteralPath $applicationBackup){ Get-ChildItem -LiteralPath $applicationBackup | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $env:ProgramFiles 'Acer Center') -Recurse -Force } }
$service = Get-CimInstance Win32_Service -Filter "Name='AcerCrate'"
$changed = Invoke-CimMethod -InputObject $service -MethodName Change -Arguments @{PathName=$record.PathName}
if ($changed.ReturnValue -ne 0) { throw 'Service rollback failed' }
if ($record.State -eq 'Running') { Start-Service AcerCrate }
Write-Host 'Путь исходной службы восстановлен. Конфигурации сохранены в резервной копии.'
