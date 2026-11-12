# Keep SDK state, dependencies and temporary build files next to the source tree.
$taskDevelopmentRoot = Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) '.acercrate-dev'
foreach ($taskDirectory in @('temp','dotnet-home','cache\nuget','cache\http','cache\plugins')) {
    New-Item -ItemType Directory -Path (Join-Path $taskDevelopmentRoot $taskDirectory) -Force | Out-Null
}
$env:DOTNET_CLI_HOME = Join-Path $taskDevelopmentRoot 'dotnet-home'
$env:APPDATA = Join-Path $env:DOTNET_CLI_HOME 'AppData\Roaming'
$env:LOCALAPPDATA = Join-Path $env:DOTNET_CLI_HOME 'AppData\Local'
New-Item -ItemType Directory -Path $env:APPDATA,$env:LOCALAPPDATA -Force | Out-Null
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:NUGET_PACKAGES = Join-Path $taskDevelopmentRoot 'cache\nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $taskDevelopmentRoot 'cache\http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $taskDevelopmentRoot 'cache\plugins'
$env:TEMP = Join-Path $taskDevelopmentRoot 'temp'
$env:TMP = $env:TEMP
$taskSdkDirectory = Join-Path $taskDevelopmentRoot 'tools\dotnet'
if (Test-Path -LiteralPath (Join-Path $taskSdkDirectory 'dotnet.exe')) {
    $env:DOTNET_ROOT = $taskSdkDirectory
    $env:PATH = $taskSdkDirectory + ';' + $env:PATH
}
