; AcerCrate's own installer text in Spanish, next to the app's Strings\es-ES.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Spanish"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Iniciar AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "No necesitas aceptar esta licencia para usar AcerCrate. Te da derecho a compartirlo y modificarlo, y establece las condiciones para hacerlo."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (necesario)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "Controlador PawnIO (necesario)"
; Component name.
${LangFileString} Inst_SecStartMenu "Acceso directo en el menú Inicio"
; Component name.
${LangFileString} Inst_SecDesktop "Acceso directo en el escritorio"
; Component name.
${LangFileString} Inst_SecAutostart "Iniciar con Windows"
; Component description.
${LangFileString} Inst_DescCore "La aplicación AcerCrate y su servicio en segundo plano, con todo lo que necesitan para funcionar."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Controlador firmado que permite a AcerCrate leer el propio sensor de temperatura de la CPU, como ThrottleStop y HWiNFO. Se omite si PawnIO ya está instalado."
; Component description.
${LangFileString} Inst_DescStartMenu "Añade AcerCrate al menú Inicio."
; Component description.
${LangFileString} Inst_DescDesktop "Añade un acceso directo a AcerCrate en el escritorio."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Abre AcerCrate en el área de notificación al iniciar sesión. El control de los ventiladores funciona desde el inicio en cualquier caso."
; Progress log line.
${LangFileString} Inst_Closing "Cerrando la copia de AcerCrate en ejecución..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Quitando la versión instalada de $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "No se pudo quitar la versión instalada de AcerCrate (error $1). ¿Quieres continuar de todos modos?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 ya está instalado."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Instalando PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "No se pudo instalar el controlador PawnIO (error $1). Hasta que se instale, AcerCrate mostrará la temperatura de la CPU del firmware, que es menos precisa."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Registrando el servicio AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "No se pudo registrar el servicio AcerCrate (error $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Iniciando el servicio AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "El servicio AcerCrate no se inició (error $0). Su registro está en %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Control de ventiladores, rendimiento e iluminación para portátiles Acer Nitro y Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Control de ventiladores, rendimiento e iluminación"
${LangFileString} Inst_Needs64Bit "AcerCrate requiere Windows de 64 bits."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate requiere Windows 10 versión 2004 (compilación ${MIN_WINDOWS_BUILD}) o posterior."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Deteniendo el servicio AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "¿Quitar también tu configuración y tus registros de AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Algunos archivos de AcerCrate están en uso y se quitarán cuando Windows se reinicie."
