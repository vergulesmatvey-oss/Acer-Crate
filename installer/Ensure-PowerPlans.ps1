$ErrorActionPreference='Stop'
$powercfg=Join-Path $env:WINDIR 'System32\powercfg.exe'
$plans=@(
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000001';Template='SCHEME_MAX';Name='Acer Crate — Тихий'},
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000002';Template='SCHEME_BALANCED';Name='Acer Crate — Сбалансированный'},
 @{Id='6eae192d-02a0-4d44-91ee-cc0000000003';Template='SCHEME_MIN';Name='Acer Crate — Производительность'}
)
$existing=& $powercfg /list | Out-String
foreach($plan in $plans){
 if($existing -notmatch [regex]::Escape($plan.Id)){
  & $powercfg /duplicatescheme $plan.Template $plan.Id
  if($LASTEXITCODE -ne 0){throw ('Power scheme creation failed: '+$plan.Name)}
 }
 & $powercfg /changename $plan.Id $plan.Name
 if($LASTEXITCODE -ne 0){throw ('Power scheme naming failed: '+$plan.Name)}
}
