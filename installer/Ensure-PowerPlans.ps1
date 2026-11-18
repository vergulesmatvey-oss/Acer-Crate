param([switch]$Remove)

$ErrorActionPreference='Stop'
$powercfg=Join-Path $env:WINDIR 'System32\powercfg.exe'
$plans=@(
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000001';Template='SCHEME_MAX';Name='Acer Crate — Тихий'},
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000002';Template='SCHEME_BALANCED';Name='Acer Crate — Сбалансированный'},
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000003';Template='SCHEME_MIN';Name='Acer Crate — Производительность'}
)
function Get-PowerSchemes {
 $pattern = '(?im)^\s*[^:\r\n]+:\s*(?<id>[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})\s*\((?<name>.*)\)\s*\*?\s*$'
 $list = & $powercfg /list | Out-String
 if ($LASTEXITCODE -ne 0) { throw 'Could not list Windows power schemes.' }
 foreach ($entry in [regex]::Matches($list, $pattern)) {
  [pscustomobject]@{Id=$entry.Groups['id'].Value; Name=$entry.Groups['name'].Value}
 }
}

function Get-ActivePowerSchemeId {
 $output = & $powercfg /getactivescheme | Out-String
 if ($LASTEXITCODE -ne 0 -or $output -notmatch '(?i):\s*([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})\s*\(') {
  throw 'Could not determine the active Windows power scheme.'
 }
 $Matches[1]
}

$schemes = @(Get-PowerSchemes)
$existing = @($schemes | ForEach-Object Id)
if ($Remove) {
 $service = Get-Service -Name AcerCrate -ErrorAction SilentlyContinue
 if ($service -and $service.Status -ne 'Stopped') { throw 'Stop the Acer Crate service before removing power schemes.' }
 # These GUIDs are reserved by Acer Crate, including the recoverable temporary CPU plan.
 $managedIds = @($plans.Id) + '6eae192d-02a0-4d44-91ee-cc0000000004'
 $owned = @($existing | Where-Object { $_ -in $managedIds })
 if ($owned.Count -eq 0) { return }

 $active = Get-ActivePowerSchemeId
 if ($active -in $owned) {
  $balanced = '381b4222-f694-41f0-9685-ff5bb260df2e'
  $other = @($existing | Where-Object { $_ -notin $owned })
  if ($balanced -in $other) { $fallback = $balanced }
  elseif ($other.Count -gt 0) { $fallback = $other[0] }
  else { throw 'No Windows power scheme is available to replace the active Acer Crate scheme.' }
  & $powercfg /setactive $fallback
  if ($LASTEXITCODE -ne 0 -or (Get-ActivePowerSchemeId) -ne $fallback) {
   throw 'Could not switch Windows away from the Acer Crate power scheme.'
  }
 }

 foreach ($id in $owned) {
  if ((Get-ActivePowerSchemeId) -eq $id) { throw "The Acer Crate power scheme $id is still active." }
  & $powercfg /delete $id
  if ($LASTEXITCODE -ne 0) { throw "Could not remove the Acer Crate power scheme $id." }
 }
 return
}

foreach($plan in $plans){
 if($plan.Id -notin $existing){
  & $powercfg /duplicatescheme $plan.Template $plan.Id
  if($LASTEXITCODE -ne 0){throw ('Power scheme creation failed: '+$plan.Name)}
 }
 & $powercfg /changename $plan.Id $plan.Name
 if($LASTEXITCODE -ne 0){throw ('Power scheme naming failed: '+$plan.Name)}
}
