; AcerCrate's own installer text in Farsi, next to the app's Strings\fa-IR.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Farsi"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "شروع AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "برای استفاده از AcerCrate نیازی به پذیرفتن این مجوز نیست. این مجوز به شما حق اشتراک‌گذاری و تغییر آن را می‌دهد و شرایط انجام این کار را تعیین می‌کند."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (الزامی)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "درایور PawnIO (الزامی)"
; Component name.
${LangFileString} Inst_SecStartMenu "میان‌بر منوی شروع"
; Component name.
${LangFileString} Inst_SecDesktop "میان‌بر میزکار"
; Component name.
${LangFileString} Inst_SecAutostart "شروع همراه با Windows"
; Component description.
${LangFileString} Inst_DescCore "برنامهٔ AcerCrate و سرویس پس‌زمینهٔ آن، با هر آنچه برای اجرا لازم دارند."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "درایور امضاشده‌ای که به AcerCrate امکان می‌دهد حسگر دمای خود CPU را بخواند، مانند ThrottleStop و HWiNFO. اگر PawnIO از قبل نصب باشد، رد می‌شود."
; Component description.
${LangFileString} Inst_DescStartMenu "AcerCrate را به منوی شروع اضافه می‌کند."
; Component description.
${LangFileString} Inst_DescDesktop "یک میان‌بر AcerCrate به میزکار اضافه می‌کند."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "هنگام ورود به سیستم، AcerCrate را در ناحیهٔ اعلان باز می‌کند. کنترل فن در هر صورت از زمان راه‌اندازی اجرا می‌شود."
; Progress log line.
${LangFileString} Inst_Closing "در حال بستن AcerCrate در حال اجرا..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "در حال حذف نسخهٔ نصب‌شده از $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "نسخهٔ نصب‌شدهٔ AcerCrate حذف نشد (خطای $1). با این حال ادامه داده شود؟"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 از قبل نصب است."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "در حال نصب PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "درایور PawnIO نصب نشد (خطای $1). تا زمانی که نصب شود، AcerCrate دمای کم‌دقت‌تر CPU را از سفت‌افزار نشان می‌دهد."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "در حال ثبت سرویس AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "سرویس AcerCrate ثبت نشد (خطای $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "در حال شروع سرویس AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "سرویس AcerCrate شروع نشد (خطای $0). گزارش آن در %ProgramData%\AcerCrate\logs است."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "کنترل فن، عملکرد و نورپردازی برای لپ‌تاپ‌های Acer Nitro و Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "کنترل فن، عملکرد و نورپردازی"
${LangFileString} Inst_Needs64Bit "AcerCrate به Windows ۶۴ بیتی نیاز دارد."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate به Windows 10 نسخهٔ 2004 (ساخت ${MIN_WINDOWS_BUILD}) یا جدیدتر نیاز دارد."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "در حال توقف سرویس AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "تنظیمات و گزارش‌های AcerCrate شما هم حذف شوند؟"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "برخی از فایل‌های AcerCrate در حال استفاده هستند و هنگام راه‌اندازی مجدد Windows حذف می‌شوند."
