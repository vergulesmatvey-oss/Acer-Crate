; AcerCrate's own installer text in Slovak, next to the app's Strings\sk-SK.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Slovak"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Spustiť AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Na používanie AcerCrate nemusíte túto licenciu prijať. Dáva vám právo program šíriť a upravovať a stanovuje podmienky, za ktorých to môžete robiť."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (povinné)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "Ovládač PawnIO (povinné)"
; Component name.
${LangFileString} Inst_SecStartMenu "Odkaz v ponuke Štart"
; Component name.
${LangFileString} Inst_SecDesktop "Odkaz na pracovnej ploche"
; Component name.
${LangFileString} Inst_SecAutostart "Spúšťať so systémom Windows"
; Component description.
${LangFileString} Inst_DescCore "Aplikácia AcerCrate a jej služba na pozadí so všetkým, čo potrebujú na spustenie."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Podpísaný ovládač, vďaka ktorému AcerCrate číta vlastný teplotný senzor procesora rovnako ako ThrottleStop a HWiNFO. Ak je PawnIO už nainštalovaný, preskočí sa."
; Component description.
${LangFileString} Inst_DescStartMenu "Pridá AcerCrate do ponuky Štart."
; Component description.
${LangFileString} Inst_DescDesktop "Pridá odkaz na AcerCrate na pracovnú plochu."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Po prihlásení otvorí AcerCrate v oblasti oznámení. Ovládanie ventilátorov beží od spustenia v každom prípade."
; Progress log line.
${LangFileString} Inst_Closing "Ukončuje sa spustený AcerCrate..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Odstraňuje sa nainštalovaná verzia z priečinka $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Nainštalovanú verziu AcerCrate sa nepodarilo odstrániť (chyba $1). Chcete napriek tomu pokračovať?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 je už nainštalovaný."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Inštaluje sa PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Ovládač PawnIO sa nepodarilo nainštalovať (chyba $1). Dovtedy bude AcerCrate zobrazovať menej presnú teplotu procesora z firmvéru."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Registruje sa služba AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Službu AcerCrate sa nepodarilo zaregistrovať (chyba $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Spúšťa sa služba AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "Služba AcerCrate sa nespustila (chyba $0). Jej denník je v priečinku %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Ovládanie ventilátorov, výkonu a podsvietenia pre notebooky Acer Nitro a Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Ovládanie ventilátorov, výkonu a podsvietenia"
${LangFileString} Inst_Needs64Bit "AcerCrate vyžaduje 64-bitový systém Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate vyžaduje Windows 10 verzie 2004 (zostava ${MIN_WINDOWS_BUILD}) alebo novší."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Zastavuje sa služba AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Odstrániť aj nastavenia a denníky AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Niektoré súbory AcerCrate sa používajú a budú odstránené po reštartovaní Windows."
