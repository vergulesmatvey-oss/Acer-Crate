Unicode true
ManifestDPIAware true
ManifestSupportedOS all
RequestExecutionLevel admin
SetCompressor /SOLID zlib
!include MUI2.nsh
!include x64.nsh
!include LogicLib.nsh
!include FileFunc.nsh

!define APP_NAME "Acer Crate"
!define APP_EXE "AcerCrate.exe"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AcerCrate"
!ifndef VERSION
  !define VERSION "1.1.0"
!endif
Name "${APP_NAME}"
OutFile "${OUTPUT}"
InstallDir "D:\Acer Crate"
BrandingText "$(CrateBranding)"
VIProductVersion "${VERSION}.0"
VIAddVersionKey /LANG=1049 "ProductName" "Acer Crate"
VIAddVersionKey /LANG=1049 "FileDescription" "Установка Acer Crate для Windows x64"
VIAddVersionKey /LANG=1049 "FileVersion" "${VERSION}.0"
VIAddVersionKey /LANG=1049 "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=1049 "LegalCopyright" "OpenSense contributors · GPL-3.0"

!define MUI_ICON "..\src\AcerCrate.App\Assets\AcerCrate.ico"
!define MUI_UNICON "..\src\AcerCrate.App\Assets\AcerCrate.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "$(CrateWelcomeTitle)"
!define MUI_WELCOMEPAGE_TEXT "$(CrateWelcomeText)"
!define MUI_LICENSEPAGE_TEXT_BOTTOM "$(CrateLicense)"
!define MUI_LICENSEPAGE_BUTTON "$(^NextBtn)"
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "$(CrateLaunch)"
!define MUI_FINISHPAGE_RUN_FUNCTION LaunchApp
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "Ukrainian"
!include "AcerCrateLanguages.nsh"

Var PowerShell
Var Result

Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
  ${IfNot} ${RunningX64}
    MessageBox MB_OK|MB_ICONSTOP "$(CrateNeed64)" /SD IDOK
    Abort
  ${EndIf}
  SetRegView 64
  StrCpy $PowerShell "$WINDIR\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "CurrentBuildNumber"
  ${If} $0 < 19041
    MessageBox MB_OK|MB_ICONSTOP "$(CrateNeedWindows)" /SD IDOK
    Abort
  ${EndIf}
FunctionEnd

Function LaunchApp
  Exec '"$WINDIR\explorer.exe" "$INSTDIR\${APP_EXE}"'
FunctionEnd

Section "Acer Crate"
  SectionIn RO
  InitPluginsDir
  DetailPrint "$(CrateUnpack)"
  !include "${PAYLOAD_FILES}"
  SetOutPath "$PLUGINSDIR"
  File "Install-AcerCrate-Package.ps1"
  File "Ensure-PowerPlans.ps1"
  DetailPrint "$(CrateInstall)"
  nsExec::ExecToLog '"$PowerShell" -NoProfile -ExecutionPolicy Bypass -File "$PLUGINSDIR\Install-AcerCrate-Package.ps1" -PackagePath "$PLUGINSDIR\payload" -ManifestHash "${MANIFEST_HASH}" -ResultPath "$PLUGINSDIR\result.json" -InstallDirectory "$INSTDIR"'
  Pop $Result
  ${If} $Result != 0
    CreateDirectory "$LOCALAPPDATA\AcerCrate"
    CopyFiles /SILENT "$PLUGINSDIR\result.json" "$LOCALAPPDATA\AcerCrate\setup-result.json"
    MessageBox MB_OK|MB_ICONSTOP "$(CrateFailed)" /SD IDOK
    SetErrorLevel 1
    Abort
  ${EndIf}
  SetOutPath "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  SetShellVarContext current
  CreateShortcut "$SMPROGRAMS\Acer Crate.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\Assets\AcerCrate-clear.ico" 0 SW_SHOWNORMAL
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "Acer Crate"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "Acer Crate"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE},0"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKLM "${UNINSTALL_KEY}" "QuietUninstallString" '$\"$INSTDIR\Uninstall.exe$\" /S'
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "EstimatedSize" $0
  CreateDirectory "$LOCALAPPDATA\AcerCrate"
  CopyFiles /SILENT "$PLUGINSDIR\result.json" "$LOCALAPPDATA\AcerCrate\setup-result.json"
  DetailPrint "$(CrateInstalled)"
SectionEnd

Function .onInstSuccess
  ${GetParameters} $0
  ClearErrors
  ${GetOptions} $0 "/Relaunch" $1
  ${IfNot} ${Errors}
    Call LaunchApp
  ${EndIf}
FunctionEnd

Function un.onInit
  SetRegView 64
FunctionEnd

Section "Uninstall"
  IfFileExists "$INSTDIR\${APP_EXE}" 0 +2
    ExecWait '"$INSTDIR\${APP_EXE}" --exit'
  IfFileExists "$INSTDIR\${APP_EXE}" 0 +2
    ExecWait '"$INSTDIR\${APP_EXE}" --disable-autostart'
  ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Services\AcerCrate" "ImagePath"
  ${If} $0 == '$\"$INSTDIR\Service\AcerCrate.Service.exe$\"'
    nsExec::ExecToLog '"$SYSDIR\net.exe" stop AcerCrate'
    Pop $0
    nsExec::ExecToLog '"$SYSDIR\sc.exe" delete AcerCrate'
    Pop $0
  ${EndIf}
  SetShellVarContext current
  Delete "$SMPROGRAMS\Acer Crate.lnk"
  !include "${UNINSTALL_FILES}"
  Delete /REBOOTOK "$INSTDIR\AcerCrate.manifest.json"
  Delete /REBOOTOK "$INSTDIR\Uninstall.exe"
  RMDir /REBOOTOK "$INSTDIR"
  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  DetailPrint "$(CrateRemoved)"
SectionEnd
