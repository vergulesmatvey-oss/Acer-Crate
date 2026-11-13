; AcerCrate's own installer text in Serbian, next to the app's Strings\sr-Cyrl-RS.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Serbian"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Покрени AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Не морате да прихватите ову лиценцу да бисте користили AcerCrate. Она вам даје право да га делите и мењате и одређује услове под којима то можете да радите."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (обавезно)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO управљачки програм (обавезно)"
; Component name.
${LangFileString} Inst_SecStartMenu "Пречица у менију „Старт“"
; Component name.
${LangFileString} Inst_SecDesktop "Пречица на радној површини"
; Component name.
${LangFileString} Inst_SecAutostart "Покрени са системом Windows"
; Component description.
${LangFileString} Inst_DescCore "Апликација AcerCrate и њена услуга у позадини, са свиме што им је потребно за рад."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Потписани управљачки програм помоћу ког AcerCrate чита сопствени сензор температуре процесора, као ThrottleStop и HWiNFO. Прескаче се ако је PawnIO већ инсталиран."
; Component description.
${LangFileString} Inst_DescStartMenu "Додаје AcerCrate у мени „Старт“."
; Component description.
${LangFileString} Inst_DescDesktop "Додаје пречицу за AcerCrate на радну површину."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Отвара AcerCrate у области са обавештењима када се пријавите. Управљање вентилаторима у сваком случају ради од покретања."
; Progress log line.
${LangFileString} Inst_Closing "Затварање покренутог програма AcerCrate..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Уклањање инсталиране верзије из $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Није могуће уклонити инсталирану верзију програма AcerCrate (грешка $1). Желите ли ипак да наставите?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 је већ инсталиран."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Инсталирање PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Није могуће инсталирати PawnIO управљачки програм (грешка $1). До тада ће AcerCrate приказивати мање тачну температуру процесора из фирмвера."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Регистровање услуге AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Није могуће регистровати услугу AcerCrate (грешка $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Покретање услуге AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "Услуга AcerCrate се није покренула (грешка $0). Њена евиденција се налази у фасцикли %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Управљање вентилаторима, перформансама и осветљењем за лаптопове Acer Nitro и Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Управљање вентилаторима, перформансама и осветљењем"
${LangFileString} Inst_Needs64Bit "AcerCrate захтева 64-битни Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate захтева Windows 10 верзије 2004 (верзија ${MIN_WINDOWS_BUILD}) или новији."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Заустављање услуге AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Желите ли да уклоните и своје AcerCrate поставке и евиденције?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Неке AcerCrate датотеке се користе и биће уклоњене када се Windows поново покрене."
