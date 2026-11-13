; AcerCrate's own installer text in English, next to the app's Strings\en-US.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "English"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Start AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "You don't need to accept this license to use AcerCrate. It gives you the right to share and change it, and sets the conditions for doing so."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (required)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO driver (required)"
; Component name.
${LangFileString} Inst_SecStartMenu "Start menu shortcut"
; Component name.
${LangFileString} Inst_SecDesktop "Desktop shortcut"
; Component name.
${LangFileString} Inst_SecAutostart "Start with Windows"
; Component description.
${LangFileString} Inst_DescCore "The AcerCrate app and its background service, with everything they need to run."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Signed driver that lets AcerCrate read the CPU's own temperature sensor, like ThrottleStop and HWiNFO. Skipped if PawnIO is already installed."
; Component description.
${LangFileString} Inst_DescStartMenu "Adds AcerCrate to the Start menu."
; Component description.
${LangFileString} Inst_DescDesktop "Adds an AcerCrate shortcut to the desktop."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Opens AcerCrate in the notification area when you sign in. Fan control runs from startup either way."
; Progress log line.
${LangFileString} Inst_Closing "Closing the running copy of AcerCrate..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Removing the installed version from $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "The installed version of AcerCrate could not be removed (error $1). Continue anyway?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 is already installed."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Installing PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "The PawnIO driver could not be installed (error $1). AcerCrate will show the firmware's less accurate CPU temperature until it is."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Registering the AcerCrate service..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "The AcerCrate service could not be registered (error $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Starting the AcerCrate service..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "The AcerCrate service did not start (error $0). Its log is in %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Fan, performance and lighting control for Acer Nitro and Predator laptops."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Fan, performance and lighting control"
${LangFileString} Inst_Needs64Bit "AcerCrate requires 64-bit Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate requires Windows 10 version 2004 (build ${MIN_WINDOWS_BUILD}) or later."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Stopping the AcerCrate service..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Also remove your AcerCrate settings and logs?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Some AcerCrate files are in use and will be removed when Windows restarts."
