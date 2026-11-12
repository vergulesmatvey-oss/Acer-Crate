param([switch]$SkipPublish,[string]$MakeNsis,[switch]$PrepareOnly)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Use-BuildEnvironment.ps1')
$output = Join-Path $root '.acercrate-dev\outputs'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$publish = Join-Path $output 'AcerCrate'
$build = Join-Path $root '.acercrate-dev\packaging'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$version = ([xml](Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
if (-not $SkipPublish) {
    & dotnet publish (Join-Path $root 'src\OpenSense.App\OpenSense.App.csproj') -c Release -r win-x64 -p:RuntimeIdentifiers=win-x64 -p:Platform=x64 -p:UseSharedCompilation=false --self-contained true -m:1 -nr:false -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'Не собрано приложение.' }
    & dotnet publish (Join-Path $root 'src\OpenSense.Service\OpenSense.Service.csproj') -c Release -r win-x64 -p:UseSharedCompilation=false --self-contained true -m:1 -nr:false -o (Join-Path $publish 'Service')
    if ($LASTEXITCODE -ne 0) { throw 'Не собрана служба.' }
}
foreach ($item in @('AcerCrate.exe','coreclr.dll','hostfxr.dll','Service\OpenSense.Service.exe','Service\coreclr.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publish $item))) { throw ('Не хватает файла поставки: ' + $item) }
}
if (-not $MakeNsis) {
    $command = Get-Command makensis.exe -ErrorAction SilentlyContinue
    if ($command) { $MakeNsis = $command.Source }
}
if (-not $MakeNsis) {
    $tools = Join-Path $build 'tools'
    $MakeNsis = Join-Path $tools 'nsis-3.13\makensis.exe'
    if (-not (Test-Path -LiteralPath $MakeNsis)) {
        New-Item -ItemType Directory -Path $tools -Force | Out-Null
        $archive = Join-Path $tools 'nsis-3.13.zip'
        Invoke-WebRequest -Uri 'https://github.com/tauri-apps/binary-releases/releases/download/nsis-3.13/nsis-3.13.zip' -OutFile $archive
        if ((Get-FileHash -LiteralPath $archive -Algorithm SHA1).Hash -ne 'DB14F8AF2EA346B786C6AE19343F61AAF5E09B8E') { throw 'Не прошла проверка компилятора NSIS.' }
        Expand-Archive -LiteralPath $archive -DestinationPath $tools -Force
    }
}
if (-not (Test-Path -LiteralPath $MakeNsis)) { throw 'Компилятор NSIS не найден.' }
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $publish 'LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'ACER-CRATE.md') -Destination (Join-Path $publish 'README.ru.md') -Force

# Corresponding source accompanies the binary in the same installer.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceDir = Join-Path $publish 'Source'
New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
$sourceZip = Join-Path $sourceDir 'AcerCrate-source.zip'
$stream = [IO.File]::Open($sourceZip,[IO.FileMode]::Create)
$archive = New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try {
    Get-ChildItem -LiteralPath $root -Force | Where-Object Name -ne '.acercrate-dev' | ForEach-Object {
        if ($_.PSIsContainer) { Get-ChildItem -LiteralPath $_.FullName -Recurse -File } else { $_ }
    } | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length+1)
        if ($relative -notmatch '(^|\\)(bin|obj|\.git|\.vs|\.idea|TestResults|artifacts)(\\|$)') {
            $entry = $archive.CreateEntry($relative.Replace('\','/'),[IO.Compression.CompressionLevel]::Optimal)
            $target = $entry.Open(); $source = [IO.File]::OpenRead($_.FullName)
            try { $source.CopyTo($target) } finally { $source.Dispose(); $target.Dispose() }
        }
    }
} finally { $archive.Dispose(); $stream.Dispose() }

$excluded = @('AcerCrate.manifest.json','Install-AcerCrate.ps1','Rollback-Service.ps1','Ensure-PowerPlans.ps1','Uninstall.exe')
$files = @(Get-ChildItem -LiteralPath $publish -Recurse -File | Where-Object {
    $_.FullName.Substring($publish.Length+1) -notin $excluded
} | Sort-Object FullName)
$entries = @($files | ForEach-Object { @{Path=$_.FullName.Substring($publish.Length+1);Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;Length=$_.Length} })
$manifestPath = Join-Path $publish 'AcerCrate.manifest.json'
@{Version=[string]$version;Architecture='x64';Files=$entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
function Escape-Nsis([string]$Value) { $Value.Replace('$','$$').Replace('"','$\"') }
$payload = New-Object 'System.Collections.Generic.List[string]'
$uninstall = New-Object 'System.Collections.Generic.List[string]'
$directories = New-Object 'System.Collections.Generic.HashSet[string]'
$previousDirectory = $null
foreach ($file in @($files) + @(Get-Item -LiteralPath $manifestPath)) {
    $relative = $file.FullName.Substring($publish.Length+1)
    if ($relative.Split('\') -contains '..' -or [IO.Path]::IsPathRooted($relative)) { throw 'Некорректный путь пакета.' }
    $directory = [IO.Path]::GetDirectoryName($relative)
    if ($directory -ne $previousDirectory) {
        $suffix = if ($directory) { '\' + (Escape-Nsis $directory) } else { '' }
        $payload.Add('SetOutPath "$PLUGINSDIR\payload' + $suffix + '"')
        $previousDirectory = $directory
    }
    $payload.Add('File "' + (Escape-Nsis $file.FullName) + '"')
    if ($relative -ne 'AcerCrate.manifest.json') { $uninstall.Add('Delete /REBOOTOK "$INSTDIR\' + (Escape-Nsis $relative) + '"') }
    while ($directory) { [void]$directories.Add($directory); $directory = [IO.Path]::GetDirectoryName($directory) }
}
foreach ($directory in ($directories | Sort-Object Length -Descending)) { $uninstall.Add('RMDir /REBOOTOK "$INSTDIR\' + (Escape-Nsis $directory) + '"') }
$payloadPath = Join-Path $build 'Payload.nsh'
$uninstallPath = Join-Path $build 'UninstallFiles.nsh'
$payload | Set-Content -LiteralPath $payloadPath -Encoding UTF8
$uninstall | Set-Content -LiteralPath $uninstallPath -Encoding UTF8
if ($PrepareOnly) {
    Write-Host ('Пакет и исходники подготовлены: ' + $publish)
    return
}
$setup = Join-Path $output 'AcerCrate-Setup-x64.exe'
& $MakeNsis "/INPUTCHARSET" "UTF8" "/DVERSION=$version" "/DOUTPUT=$setup" "/DPAYLOAD_FILES=$payloadPath" "/DUNINSTALL_FILES=$uninstallPath" "/DMANIFEST_HASH=$manifestHash" (Join-Path $PSScriptRoot 'AcerCrate.nsi')
if ($LASTEXITCODE -ne 0) { throw 'Не собран установщик.' }
$setupHash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash

# One shareable archive: editable corresponding source and the offline installer.
$bundle = Join-Path $output 'AcerCrate.zip'
$bundleTemp = Join-Path $build 'AcerCrate.zip.tmp'
$bundleStream = [IO.File]::Open($bundleTemp,[IO.FileMode]::Create)
$bundleArchive = New-Object IO.Compression.ZipArchive($bundleStream,[IO.Compression.ZipArchiveMode]::Create,$false)
$sourceArchive = [IO.Compression.ZipFile]::OpenRead($sourceZip)
try {
    foreach ($sourceEntry in $sourceArchive.Entries) {
        $entry = $bundleArchive.CreateEntry(('AcerCrate/Source/' + $sourceEntry.FullName),[IO.Compression.CompressionLevel]::Optimal)
        $source = $sourceEntry.Open(); $target = $entry.Open()
        try { $source.CopyTo($target) } finally { $source.Dispose(); $target.Dispose() }
    }
    foreach ($item in @(
        @{Path=$setup;Name='AcerCrate/AcerCrate-Setup-x64.exe'},
        @{Path=(Join-Path $root 'ACER-CRATE.md');Name='AcerCrate/README.ru.md'}
    )) {
        $entry = $bundleArchive.CreateEntry($item.Name,[IO.Compression.CompressionLevel]::NoCompression)
        $source = [IO.File]::OpenRead($item.Path); $target = $entry.Open()
        try { $source.CopyTo($target) } finally { $source.Dispose(); $target.Dispose() }
    }
    $checksumEntry = $bundleArchive.CreateEntry('AcerCrate/SHA256SUMS.txt')
    $writer = New-Object IO.StreamWriter($checksumEntry.Open(),(New-Object Text.UTF8Encoding($false)))
    try { $writer.WriteLine($setupHash + '  AcerCrate-Setup-x64.exe') } finally { $writer.Dispose() }
} finally { $sourceArchive.Dispose(); $bundleArchive.Dispose(); $bundleStream.Dispose() }
Move-Item -LiteralPath $bundleTemp -Destination $bundle -Force
@{Installer=$setup;Sha256=$setupHash;Bytes=(Get-Item -LiteralPath $setup).Length;PayloadFiles=$entries.Count;ManifestHash=$manifestHash;Bundle=$bundle;BundleSha256=(Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $build 'build-result.json') -Encoding UTF8
Write-Host ('Готово: ' + $setup)
Write-Host ('Архив с исходниками: ' + $bundle)

