; AcerCrate's own installer text in Norwegian, next to the app's Strings\nb-NO.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Norwegian"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Start AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Du trenger ikke å godta denne lisensen for å bruke AcerCrate. Den gir deg rett til å dele og endre programmet og fastsetter vilkårene for å gjøre det."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (påkrevd)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO-driver (påkrevd)"
; Component name.
${LangFileString} Inst_SecStartMenu "Snarvei på Start-menyen"
; Component name.
${LangFileString} Inst_SecDesktop "Snarvei på skrivebordet"
; Component name.
${LangFileString} Inst_SecAutostart "Start med Windows"
; Component description.
${LangFileString} Inst_DescCore "AcerCrate-appen og bakgrunnstjenesten dens, med alt de trenger for å kjøre."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Signert driver som lar AcerCrate lese CPU-ens egen temperatursensor, slik ThrottleStop og HWiNFO gjør. Hoppes over hvis PawnIO allerede er installert."
; Component description.
${LangFileString} Inst_DescStartMenu "Legger til AcerCrate på Start-menyen."
; Component description.
${LangFileString} Inst_DescDesktop "Legger til en snarvei til AcerCrate på skrivebordet."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Åpner AcerCrate i systemstatusfeltet når du logger på. Viftestyringen kjører fra oppstart uansett."
; Progress log line.
${LangFileString} Inst_Closing "Lukker AcerCrate-kopien som kjører..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Fjerner den installerte versjonen fra $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Kan ikke fjerne den installerte versjonen av AcerCrate (feil $1). Vil du fortsette likevel?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 er allerede installert."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Installerer PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Kan ikke installere PawnIO-driveren (feil $1). Inntil den er installert, viser AcerCrate fastvarens mindre nøyaktige CPU-temperatur."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Registrerer AcerCrate-tjenesten..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Kan ikke registrere AcerCrate-tjenesten (feil $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Starter AcerCrate-tjenesten..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "AcerCrate-tjenesten startet ikke (feil $0). Loggen ligger i %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Styring av vifter, ytelse og belysning for bærbare Acer Nitro- og Predator-PC-er."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Styring av vifter, ytelse og belysning"
${LangFileString} Inst_Needs64Bit "AcerCrate krever 64-biters Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate krever Windows 10 versjon 2004 (build ${MIN_WINDOWS_BUILD}) eller nyere."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Stopper AcerCrate-tjenesten..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Vil du også fjerne AcerCrate-innstillingene og -loggene dine?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Noen AcerCrate-filer er i bruk og fjernes når Windows startes på nytt."
