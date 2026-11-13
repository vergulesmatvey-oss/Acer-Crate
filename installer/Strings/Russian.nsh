; AcerCrate's own installer text in Russian, next to the app's Strings\ru-RU.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Russian"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "Запустить AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "Для использования AcerCrate не нужно принимать эту лицензию. Она даёт вам право распространять и изменять программу и определяет условия, на которых это можно делать."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (обязательно)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "Драйвер PawnIO (обязательно)"
; Component name.
${LangFileString} Inst_SecStartMenu "Ярлык в меню «Пуск»"
; Component name.
${LangFileString} Inst_SecDesktop "Ярлык на рабочем столе"
; Component name.
${LangFileString} Inst_SecAutostart "Запуск вместе с Windows"
; Component description.
${LangFileString} Inst_DescCore "Приложение AcerCrate и его фоновая служба со всем необходимым для работы."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "Подписанный драйвер, с помощью которого AcerCrate считывает собственный датчик температуры процессора, как ThrottleStop и HWiNFO. Пропускается, если PawnIO уже установлен."
; Component description.
${LangFileString} Inst_DescStartMenu "Добавляет AcerCrate в меню «Пуск»."
; Component description.
${LangFileString} Inst_DescDesktop "Добавляет ярлык AcerCrate на рабочий стол."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Открывает AcerCrate в области уведомлений при входе в систему. Управление вентиляторами в любом случае работает с момента загрузки."
; Progress log line.
${LangFileString} Inst_Closing "Закрытие запущенного AcerCrate..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Удаление установленной версии из $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "Не удалось удалить установленную версию AcerCrate (ошибка $1). Продолжить всё равно?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 уже установлен."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "Установка PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "Не удалось установить драйвер PawnIO (ошибка $1). До его установки AcerCrate будет показывать менее точную температуру ЦП от встроенного ПО."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "Регистрация службы AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "Не удалось зарегистрировать службу AcerCrate (ошибка $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "Запуск службы AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "Служба AcerCrate не запустилась (ошибка $0). Её журнал находится в папке %ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Управление вентиляторами, производительностью и подсветкой ноутбуков Acer Nitro и Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Управление вентиляторами, производительностью и подсветкой"
${LangFileString} Inst_Needs64Bit "Для AcerCrate требуется 64-разрядная версия Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "Для AcerCrate требуется Windows 10 версии 2004 (сборка ${MIN_WINDOWS_BUILD}) или более поздней."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "Остановка службы AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "Удалить также ваши параметры и журналы AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Некоторые файлы AcerCrate используются и будут удалены после перезапуска Windows."
