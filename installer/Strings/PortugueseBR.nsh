; AcerCrate's own installer text in PortugueseBR, next to the app's Strings\pt-BR.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "PortugueseBR"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Iniciar o AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Você não precisa aceitar esta licença para usar o AcerCrate. Ela lhe dá o direito de compartilhá-lo e modificá-lo e define as condições para isso."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (obrigatório)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "Driver PawnIO (obrigatório)"
; Component name.
${LangFileString} Inst_SecStartMenu "Atalho no menu Iniciar"
; Component name.
${LangFileString} Inst_SecDesktop "Atalho na área de trabalho"
; Component name.
${LangFileString} Inst_SecAutostart "Iniciar com o Windows"
; Component description.
${LangFileString} Inst_DescCore "O aplicativo AcerCrate e seu serviço em segundo plano, com tudo o que precisam para funcionar."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Driver assinado que permite ao AcerCrate ler o sensor de temperatura da própria CPU, como o ThrottleStop e o HWiNFO. Ignorado se o PawnIO já estiver instalado."
; Component description.
${LangFileString} Inst_DescStartMenu "Adiciona o AcerCrate ao menu Iniciar."
; Component description.
${LangFileString} Inst_DescDesktop "Adiciona um atalho do AcerCrate à área de trabalho."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Abre o AcerCrate na área de notificação quando você entra. O controle das ventoinhas funciona desde a inicialização de qualquer forma."
; Progress log line.
${LangFileString} Inst_Closing "Fechando o AcerCrate em execução..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Removendo a versão instalada de $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Não foi possível remover a versão instalada do AcerCrate (erro $1). Continuar mesmo assim?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "O PawnIO $0 já está instalado."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Instalando o PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Não foi possível instalar o driver PawnIO (erro $1). Até que ele seja instalado, o AcerCrate mostrará a temperatura da CPU do firmware, que é menos precisa."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Registrando o serviço do AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Não foi possível registrar o serviço do AcerCrate (erro $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Iniciando o serviço do AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "O serviço do AcerCrate não foi iniciado (erro $0). O log está em %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Controle de ventoinhas, desempenho e iluminação para notebooks Acer Nitro e Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Controle de ventoinhas, desempenho e iluminação"
${LangFileString} Inst_Needs64Bit "O AcerCrate requer o Windows de 64 bits."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "O AcerCrate requer o Windows 10 versão 2004 (build ${MIN_WINDOWS_BUILD}) ou posterior."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Parando o serviço do AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Remover também suas configurações e logs do AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Alguns arquivos do AcerCrate estão em uso e serão removidos quando o Windows reiniciar."
