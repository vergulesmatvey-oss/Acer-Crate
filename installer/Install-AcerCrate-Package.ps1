param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$ManifestHash,
    [Parameter(Mandatory=$true)][string]$ResultPath,
    [Parameter(Mandatory=$true)][string]$InstallDirectory
)
$ErrorActionPreference='Stop'
$destination=[IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
$serviceName='AcerCrate'
$legacyName='OpenSense' # Compatibility with Acer Crate 1.0.x installations.
$manifestPath=Join-Path $PackagePath 'AcerCrate.manifest.json'
$backup=$null; $existing=$null; $legacy=$null
$wasRunning=$false; $legacyWasRunning=$false; $changed=$false; $copied=$false; $transcript=$false
$settingsMoves=@(); $settingsCopies=@(); $retiredSettings=@()
try {
    if((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $ManifestHash){throw 'Package manifest integrity check failed.'}
    $manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packageRoot=[IO.Path]::GetFullPath($PackagePath).TrimEnd('\')+'\'
    foreach($file in $manifest.Files){
        $path=[IO.Path]::GetFullPath((Join-Path $PackagePath $file.Path))
        if(-not $path.StartsWith($packageRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid package path.'}
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256){throw ('Package file integrity check failed: '+$file.Path)}
    }
    foreach($relative in @('AcerCrate.runtimeconfig.json','Service\AcerCrate.Service.runtimeconfig.json')){
        $runtime=Get-Content -LiteralPath (Join-Path $PackagePath $relative) -Raw | ConvertFrom-Json
        if(-not $runtime.runtimeOptions.includedFrameworks -or $runtime.runtimeOptions.frameworks){throw 'Self-contained runtimes are missing.'}
    }
    $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
    if(-not (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Administrator privileges are required.'}
    $registration=Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AcerCrate' -ErrorAction SilentlyContinue
    $ownedRoots=@($destination)
    if($registration.InstallLocation){$ownedRoots+=[IO.Path]::GetFullPath($registration.InstallLocation).TrimEnd('\')}
    $possibleLegacy=Get-CimInstance Win32_Service -Filter "Name='$legacyName'"
    if($possibleLegacy){
        foreach($root in $ownedRoots){
            $expected=Join-Path $root 'Service\OpenSense.Service.exe'
            if($possibleLegacy.PathName.Trim('"') -eq $expected){$legacy=$possibleLegacy}
        }
        if(-not $legacy -and $possibleLegacy.State -eq 'Running'){throw 'An independent OpenSense service is running. Stop that application before installing Acer Crate.'}
    }
    $existing=Get-CimInstance Win32_Service -Filter "Name='$serviceName'"
    $wasRunning=$existing -and $existing.State -eq 'Running'
    $legacyWasRunning=$legacy -and $legacy.State -eq 'Running'
    $backup=Join-Path ([IO.Path]::GetDirectoryName($destination)) ('AcerCrate Backups\backup-setup-'+(Get-Date -Format yyyyMMdd-HHmmss))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Start-Transcript -Path (Join-Path $backup 'installation.log') -Force | Out-Null; $transcript=$true
    foreach($root in $ownedRoots | Select-Object -Unique){
        $client=Join-Path $root 'AcerCrate.exe'
        if(Test-Path -LiteralPath $client){$exit=Start-Process -FilePath $client -ArgumentList '--exit' -WindowStyle Hidden -Wait -PassThru;if($exit.ExitCode -ne 0){throw 'Close Acer Crate before updating.'}}
    }
    $exit=Start-Process -FilePath (Join-Path $PackagePath 'AcerCrate.exe') -ArgumentList '--exit' -WindowStyle Hidden -Wait -PassThru
    if($exit.ExitCode -ne 0){throw 'Close the running Acer Crate instance.'}
    foreach($service in @($existing,$legacy)){
        if(-not $service){continue}
        $service | Select-Object Name,PathName,StartMode,State | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup ($service.Name+'-service.json')) -Encoding UTF8
        Stop-Service -Name $service.Name
        (Get-Service $service.Name).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
        if($service.ProcessId -gt 0){Wait-Process -Id $service.ProcessId -Timeout 15 -ErrorAction SilentlyContinue}
    }
    if(Test-Path -LiteralPath $destination){Copy-Item -LiteralPath $destination -Destination (Join-Path $backup 'Application') -Recurse -Force}
    foreach($parent in @($env:APPDATA,$env:ProgramData)){
        $old=Join-Path $parent $legacyName; $new=Join-Path $parent 'AcerCrate'
        foreach($folder in @($old,$new)){if(Test-Path -LiteralPath $folder){Copy-Item -LiteralPath $folder -Destination (Join-Path $backup ((Split-Path $parent -Leaf)+'-'+(Split-Path $folder -Leaf))) -Recurse -Force}}
        if($legacy -and (Test-Path -LiteralPath $old)){
            $parentFull=[IO.Path]::GetFullPath($parent).TrimEnd('\')+'\'
            $oldFull=[IO.Path]::GetFullPath($old); $newFull=[IO.Path]::GetFullPath($new)
            if(-not $oldFull.StartsWith($parentFull,[StringComparison]::OrdinalIgnoreCase) -or -not $newFull.StartsWith($parentFull,[StringComparison]::OrdinalIgnoreCase)){throw 'Settings migration escaped its parent directory.'}
            if(-not (Test-Path -LiteralPath $newFull)){
                Move-Item -LiteralPath $oldFull -Destination $newFull
                $settingsMoves+=@{Old=$oldFull;New=$newFull;Parent=$parentFull}
            }elseif(-not (Test-Path -LiteralPath (Join-Path $newFull 'settings.json')) -and (Test-Path -LiteralPath (Join-Path $oldFull 'settings.json'))){
                $targetSettings=Join-Path $newFull 'settings.json'
                Copy-Item -LiteralPath (Join-Path $oldFull 'settings.json') -Destination $targetSettings
                $settingsCopies+=$targetSettings
            }
            if(Test-Path -LiteralPath $oldFull){
                $retired=[IO.Path]::GetFullPath((Join-Path $backup ('Retired-'+(Split-Path $parent -Leaf))))
                if(-not $retired.StartsWith([IO.Path]::GetFullPath($backup).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid retired settings path.'}
                Move-Item -LiteralPath $oldFull -Destination $retired
                $retiredSettings+=@{Old=$oldFull;Retired=$retired;Parent=$parentFull}
            }
        }
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $security=New-Object Security.AccessControl.DirectorySecurity
    $inherit=[Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'
    $none=[Security.AccessControl.PropagationFlags]::None; $allow=[Security.AccessControl.AccessControlType]::Allow
    $security.SetAccessRuleProtection($true,$false)
    $security.SetOwner((New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')))
    foreach($sid in @('S-1-5-18','S-1-5-32-544')){$security.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($sid)),[Security.AccessControl.FileSystemRights]::FullControl,$inherit,$none,$allow)))}
    $security.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier('S-1-5-32-545')),[Security.AccessControl.FileSystemRights]::ReadAndExecute,$inherit,$none,$allow)))
    Set-Acl -LiteralPath $destination -AclObject $security
    $copied=$true
    foreach($file in $manifest.Files){
        $target=Join-Path $destination $file.Path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $PackagePath $file.Path) -Destination $target -Force
        if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.Sha256){throw ('Installed file integrity check failed: '+$file.Path)}
    }
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $destination 'AcerCrate.manifest.json') -Force
    # Only the current user's log directory is writable; binaries and service logs stay protected.
    $userLogs=Join-Path $destination ('Logs\'+$identity.User.Value)
    New-Item -ItemType Directory -Path $userLogs -Force | Out-Null
    $userAcl=Get-Acl -LiteralPath $userLogs
    $userAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($identity.User,[Security.AccessControl.FileSystemRights]::Modify,$inherit,$none,$allow)))
    Set-Acl -LiteralPath $userLogs -AclObject $userAcl
    & (Join-Path $PSScriptRoot 'Ensure-PowerPlans.ps1')
    $serviceExe=Join-Path $destination 'Service\AcerCrate.Service.exe'
    if($existing){$change=Invoke-CimMethod -InputObject $existing -MethodName Change -Arguments @{PathName=('"'+$serviceExe+'"');StartMode='Automatic';DisplayName='Acer Crate Control'};if($change.ReturnValue -ne 0){throw 'Could not update the service.'}}
    else{New-Service -Name $serviceName -BinaryPathName ('"'+$serviceExe+'"') -DisplayName 'Acer Crate Control' -StartupType Automatic -DependsOn Winmgmt | Out-Null}
    $changed=$true
    & sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/10000/restart/60000 | Out-Null
    Start-Service -Name $serviceName
    (Get-Service $serviceName).WaitForStatus('Running',[TimeSpan]::FromSeconds(30))
    $runKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    $run=Get-ItemProperty -LiteralPath $runKey -ErrorAction SilentlyContinue
    $oldRun=$run.$legacyName
    if($run.AcerCrate -or ($legacy -and $oldRun)){
        Set-ItemProperty -LiteralPath $runKey -Name AcerCrate -Value ('"'+(Join-Path $destination 'AcerCrate.exe')+'" --autostart')
        if($legacy -and $oldRun){Remove-ItemProperty -LiteralPath $runKey -Name $legacyName -ErrorAction SilentlyContinue}
    }
    if($legacy){
        $oldTaskName=$legacyName+' autostart ('+$env:USERNAME+')'
        $oldTask=Get-ScheduledTask -TaskName $oldTaskName -ErrorAction SilentlyContinue
        if($oldTask){
            $taskTarget=[IO.Path]::GetFullPath($oldTask.Actions[0].Execute.Trim('"'))
            if(@($ownedRoots | ForEach-Object {Join-Path $_ 'AcerCrate.exe'}) -contains $taskTarget){
                Export-ScheduledTask -TaskName $oldTaskName | Set-Content -LiteralPath (Join-Path $backup 'legacy-autostart.xml') -Encoding Unicode
                if($oldTask.Settings.Enabled){New-Item -Path $runKey -Force | Out-Null; Set-ItemProperty -LiteralPath $runKey -Name AcerCrate -Value ('"'+(Join-Path $destination 'AcerCrate.exe')+'" --autostart')}
                Unregister-ScheduledTask -TaskName $oldTaskName -Confirm:$false
            }
        }
    }
    if($legacy){& sc.exe delete $legacyName | Out-Null}
    $obsolete=@('OpenSense.Core.dll','OpenSense.Core.pdb','Assets\OpenSense.ico','Assets\OpenSense.png','Assets\OpenSense-512.png','Service\OpenSense.Service.exe','Service\OpenSense.Service.dll','Service\OpenSense.Service.pdb','Service\OpenSense.Service.deps.json','Service\OpenSense.Service.runtimeconfig.json','Service\OpenSense.Core.dll','Service\OpenSense.Core.pdb')
    foreach($relative in $obsolete){$oldFile=[IO.Path]::GetFullPath((Join-Path $destination $relative));if($oldFile.StartsWith($destination+'\',[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $oldFile)){Remove-Item -LiteralPath $oldFile -Force -ErrorAction Continue}}
    @{Success=$true;Time=(Get-Date -Format o);Backup=$backup;Version=$manifest.Version;ServiceName=$serviceName;MigratedLegacyService=[bool]$legacy} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    exit 0
} catch {
    $failure=$_.Exception.Message
    try{
        if($changed){Stop-Service -Name $serviceName -ErrorAction SilentlyContinue}
        if($copied -and $backup -and (Test-Path -LiteralPath (Join-Path $backup 'Application'))){Get-ChildItem -LiteralPath (Join-Path $backup 'Application') | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force}}
        if($existing){$restore=Get-CimInstance Win32_Service -Filter "Name='$serviceName'";Invoke-CimMethod -InputObject $restore -MethodName Change -Arguments @{PathName=$existing.PathName;StartMode='Automatic'} | Out-Null;if($wasRunning){Start-Service -Name $serviceName}}
        elseif($changed){& sc.exe delete $serviceName | Out-Null}
        foreach($move in $settingsMoves){
            if(-not $move.Old.StartsWith($move.Parent,[StringComparison]::OrdinalIgnoreCase) -or -not $move.New.StartsWith($move.Parent,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid settings rollback path.'}
            if((Test-Path -LiteralPath $move.New) -and -not (Test-Path -LiteralPath $move.Old)){Move-Item -LiteralPath $move.New -Destination $move.Old}
        }
        foreach($copy in $settingsCopies){if(Test-Path -LiteralPath $copy){Remove-Item -LiteralPath $copy -Force}}
        foreach($move in $retiredSettings){
            if(-not $move.Old.StartsWith($move.Parent,[StringComparison]::OrdinalIgnoreCase) -or -not $move.Retired.StartsWith([IO.Path]::GetFullPath($backup).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid retired settings rollback path.'}
            if((Test-Path -LiteralPath $move.Retired) -and -not (Test-Path -LiteralPath $move.Old)){Move-Item -LiteralPath $move.Retired -Destination $move.Old}
        }
        if($legacyWasRunning){Start-Service -Name $legacyName -ErrorAction Continue}
    }catch{$failure+=' Rollback: '+$_.Exception.Message}
    @{Success=$false;Time=(Get-Date -Format o);Backup=$backup;Error=$failure} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    Write-Error $failure -ErrorAction Continue
    exit 1
}finally{if($transcript){Stop-Transcript | Out-Null}}
