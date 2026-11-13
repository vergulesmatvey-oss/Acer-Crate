; AcerCrate's own installer text in Turkish, next to the app's Strings\tr-TR.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Turkish"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "AcerCrate'i başlat"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "AcerCrate'i kullanmak için bu lisansı kabul etmeniz gerekmez. Lisans size programı paylaşma ve değiştirme hakkı verir ve bunun koşullarını belirler."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (gerekli)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "PawnIO sürücüsü (gerekli)"
; Component name.
${LangFileString} Inst_SecStartMenu "Başlat menüsü kısayolu"
; Component name.
${LangFileString} Inst_SecDesktop "Masaüstü kısayolu"
; Component name.
${LangFileString} Inst_SecAutostart "Windows ile başlat"
; Component description.
${LangFileString} Inst_DescCore "AcerCrate uygulaması ve arka plan hizmeti, çalışmaları için gereken her şeyle birlikte."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "ThrottleStop ve HWiNFO gibi, AcerCrate'in CPU'nun kendi sıcaklık sensörünü okumasını sağlayan imzalı sürücü. PawnIO zaten yüklüyse atlanır."
; Component description.
${LangFileString} Inst_DescStartMenu "AcerCrate'i Başlat menüsüne ekler."
; Component description.
${LangFileString} Inst_DescDesktop "Masaüstüne bir AcerCrate kısayolu ekler."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "Oturum açtığınızda AcerCrate'i bildirim alanında açar. Fan denetimi her durumda başlangıçtan itibaren çalışır."
; Progress log line.
${LangFileString} Inst_Closing "Çalışan AcerCrate kapatılıyor..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "Yüklü sürüm $0 konumundan kaldırılıyor..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "AcerCrate'in yüklü sürümü kaldırılamadı (hata $1). Yine de devam edilsin mi?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 zaten yüklü."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "PawnIO yükleniyor..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "PawnIO sürücüsü yüklenemedi (hata $1). Yüklenene kadar AcerCrate, üretici yazılımının daha az doğru CPU sıcaklığını gösterir."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "AcerCrate hizmeti kaydediliyor..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "AcerCrate hizmeti kaydedilemedi (hata $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "AcerCrate hizmeti başlatılıyor..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "AcerCrate hizmeti başlatılamadı (hata $0). Günlüğü %ProgramData%\AcerCrate\logs konumunda."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "Acer Nitro ve Predator dizüstü bilgisayarlar için fan, performans ve aydınlatma denetimi."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "Fan, performans ve aydınlatma denetimi"
${LangFileString} Inst_Needs64Bit "AcerCrate için 64 bit Windows gerekir."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate için Windows 10 sürüm 2004 (derleme ${MIN_WINDOWS_BUILD}) veya üzeri gerekir."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "AcerCrate hizmeti durduruluyor..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "AcerCrate ayarlarınız ve günlükleriniz de kaldırılsın mı?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "Bazı AcerCrate dosyaları kullanımda ve Windows yeniden başlatıldığında kaldırılacak."
