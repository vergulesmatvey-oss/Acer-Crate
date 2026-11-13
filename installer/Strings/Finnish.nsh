; AcerCrate's own installer text in Finnish, next to the app's Strings\fi-FI.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Finnish"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Käynnistä AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Sinun ei tarvitse hyväksyä tätä käyttöoikeutta käyttääksesi AcerCrateä. Se antaa sinulle oikeuden jakaa ja muuttaa ohjelmaa ja määrittää ehdot sille."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (pakollinen)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO-ohjain (pakollinen)"
; Component name.
${LangFileString} Inst_SecStartMenu "Pikakuvake Käynnistä-valikkoon"
; Component name.
${LangFileString} Inst_SecDesktop "Pikakuvake työpöydälle"
; Component name.
${LangFileString} Inst_SecAutostart "Käynnistä Windowsin mukana"
; Component description.
${LangFileString} Inst_DescCore "AcerCrate-sovellus ja sen taustapalvelu sekä kaikki, mitä ne tarvitsevat toimiakseen."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Allekirjoitettu ohjain, jonka avulla AcerCrate lukee suorittimen omaa lämpötila-anturia kuten ThrottleStop ja HWiNFO. Ohitetaan, jos PawnIO on jo asennettu."
; Component description.
${LangFileString} Inst_DescStartMenu "Lisää AcerCraten Käynnistä-valikkoon."
; Component description.
${LangFileString} Inst_DescDesktop "Lisää AcerCraten pikakuvakkeen työpöydälle."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Avaa AcerCraten ilmaisinalueelle, kun kirjaudut sisään. Tuuletinohjaus toimii käynnistyksestä lähtien joka tapauksessa."
; Progress log line.
${LangFileString} Inst_Closing "Suljetaan käynnissä oleva AcerCrate..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Poistetaan asennettu versio kansiosta $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "AcerCraten asennetun version poistaminen epäonnistui (virhe $1). Jatketaanko silti?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 on jo asennettu."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Asennetaan PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "PawnIO-ohjaimen asentaminen epäonnistui (virhe $1). Siihen asti AcerCrate näyttää laiteohjelmiston epätarkemman suoritinlämpötilan."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Rekisteröidään AcerCrate-palvelu..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "AcerCrate-palvelun rekisteröiminen epäonnistui (virhe $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Käynnistetään AcerCrate-palvelu..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "AcerCrate-palvelu ei käynnistynyt (virhe $0). Sen loki on kansiossa %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Tuuletin-, suorituskyky- ja valaistusohjaus Acer Nitro- ja Predator-kannettaville."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Tuuletin-, suorituskyky- ja valaistusohjaus"
${LangFileString} Inst_Needs64Bit "AcerCrate edellyttää 64-bittistä Windowsia."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate edellyttää Windows 10 -versiota 2004 (koontiversio ${MIN_WINDOWS_BUILD}) tai uudempaa."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Pysäytetään AcerCrate-palvelu..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Poistetaanko myös AcerCraten asetukset ja lokit?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Jotkin AcerCraten tiedostot ovat käytössä, ja ne poistetaan, kun Windows käynnistetään uudelleen."
