; AcerCrate's own installer text in Romanian, next to the app's Strings\ro-RO.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Romanian"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Pornire AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Nu trebuie să acceptați această licență pentru a utiliza AcerCrate. Ea vă oferă dreptul de a-l distribui și modifica și stabilește condițiile pentru aceasta."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (obligatoriu)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "Driver PawnIO (obligatoriu)"
; Component name.
${LangFileString} Inst_SecStartMenu "Comandă rapidă în meniul Start"
; Component name.
${LangFileString} Inst_SecDesktop "Comandă rapidă pe desktop"
; Component name.
${LangFileString} Inst_SecAutostart "Pornire odată cu Windows"
; Component description.
${LangFileString} Inst_DescCore "Aplicația AcerCrate și serviciul său de fundal, cu tot ce le trebuie pentru a rula."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Driver semnat care permite AcerCrate să citească senzorul de temperatură al procesorului, precum ThrottleStop și HWiNFO. Omis dacă PawnIO este deja instalat."
; Component description.
${LangFileString} Inst_DescStartMenu "Adaugă AcerCrate în meniul Start."
; Component description.
${LangFileString} Inst_DescDesktop "Adaugă o comandă rapidă AcerCrate pe desktop."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Deschide AcerCrate în zona de notificare când vă conectați. Controlul ventilatoarelor funcționează oricum de la pornire."
; Progress log line.
${LangFileString} Inst_Closing "Se închide AcerCrate care rulează..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Se elimină versiunea instalată din $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Versiunea instalată a AcerCrate nu a putut fi eliminată (eroarea $1). Continuați oricum?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 este deja instalat."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Se instalează PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Driverul PawnIO nu a putut fi instalat (eroarea $1). Până atunci, AcerCrate va afișa temperatura mai puțin exactă a procesorului furnizată de firmware."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Se înregistrează serviciul AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Serviciul AcerCrate nu a putut fi înregistrat (eroarea $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Se pornește serviciul AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "Serviciul AcerCrate nu a pornit (eroarea $0). Jurnalul său se află în %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Controlul ventilatoarelor, performanței și iluminării pentru laptopurile Acer Nitro și Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Controlul ventilatoarelor, performanței și iluminării"
${LangFileString} Inst_Needs64Bit "AcerCrate necesită Windows pe 64 de biți."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate necesită Windows 10 versiunea 2004 (versiunea de compilare ${MIN_WINDOWS_BUILD}) sau o versiune ulterioară."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Se oprește serviciul AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Eliminați și setările și jurnalele AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Unele fișiere AcerCrate sunt în uz și vor fi eliminate când reporniți Windows."
