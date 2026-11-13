; AcerCrate's own installer text in German, next to the app's Strings\de-DE.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "German"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "AcerCrate starten"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Sie müssen diese Lizenz nicht akzeptieren, um AcerCrate zu verwenden. Sie gibt Ihnen das Recht, das Programm weiterzugeben und zu ändern, und legt die Bedingungen dafür fest."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (erforderlich)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO-Treiber (erforderlich)"
; Component name.
${LangFileString} Inst_SecStartMenu "Verknüpfung im Startmenü"
; Component name.
${LangFileString} Inst_SecDesktop "Desktopverknüpfung"
; Component name.
${LangFileString} Inst_SecAutostart "Mit Windows starten"
; Component description.
${LangFileString} Inst_DescCore "Die AcerCrate-App und ihr Hintergrunddienst mit allem, was sie zum Ausführen benötigen."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Signierter Treiber, mit dem AcerCrate den eigenen Temperatursensor der CPU liest, wie ThrottleStop und HWiNFO. Wird übersprungen, wenn PawnIO bereits installiert ist."
; Component description.
${LangFileString} Inst_DescStartMenu "Fügt AcerCrate zum Startmenü hinzu."
; Component description.
${LangFileString} Inst_DescDesktop "Fügt dem Desktop eine Verknüpfung zu AcerCrate hinzu."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Öffnet AcerCrate bei der Anmeldung im Infobereich. Die Lüftersteuerung läuft in jedem Fall ab dem Systemstart."
; Progress log line.
${LangFileString} Inst_Closing "Das laufende AcerCrate wird geschlossen..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Die installierte Version wird aus $0 entfernt..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Die installierte Version von AcerCrate konnte nicht entfernt werden (Fehler $1). Trotzdem fortfahren?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 ist bereits installiert."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "PawnIO wird installiert..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Der PawnIO-Treiber konnte nicht installiert werden (Fehler $1). Bis dahin zeigt AcerCrate die weniger genaue CPU-Temperatur der Firmware an."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Der AcerCrate-Dienst wird registriert..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Der AcerCrate-Dienst konnte nicht registriert werden (Fehler $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Der AcerCrate-Dienst wird gestartet..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "Der AcerCrate-Dienst wurde nicht gestartet (Fehler $0). Sein Protokoll befindet sich unter %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Steuerung für Lüfter, Leistung und Beleuchtung von Acer Nitro- und Predator-Laptops."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Steuerung für Lüfter, Leistung und Beleuchtung"
${LangFileString} Inst_Needs64Bit "AcerCrate erfordert eine 64-Bit-Version von Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate erfordert Windows 10 Version 2004 (Build ${MIN_WINDOWS_BUILD}) oder höher."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Der AcerCrate-Dienst wird beendet..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Auch Ihre AcerCrate-Einstellungen und -Protokolle entfernen?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Einige AcerCrate-Dateien werden verwendet und beim nächsten Neustart von Windows entfernt."
