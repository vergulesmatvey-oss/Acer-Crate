; AcerCrate's own installer text in Thai, next to the app's Strings\th-TH.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Thai"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "เริ่ม AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "คุณไม่จำเป็นต้องยอมรับสิทธิ์การใช้งานนี้เพื่อใช้ AcerCrate สิทธิ์การใช้งานนี้ให้สิทธิ์คุณในการแบ่งปันและแก้ไขโปรแกรม และกำหนดเงื่อนไขในการทำเช่นนั้น"
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (จำเป็น)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "โปรแกรมควบคุม PawnIO (จำเป็น)"
; Component name.
${LangFileString} Inst_SecStartMenu "ทางลัดในเมนูเริ่ม"
; Component name.
${LangFileString} Inst_SecDesktop "ทางลัดบนเดสก์ท็อป"
; Component name.
${LangFileString} Inst_SecAutostart "เริ่มพร้อมกับ Windows"
; Component description.
${LangFileString} Inst_DescCore "แอป AcerCrate และบริการเบื้องหลัง พร้อมทุกอย่างที่จำเป็นต่อการทำงาน"
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "โปรแกรมควบคุมที่ลงนามแล้ว ซึ่งช่วยให้ AcerCrate อ่านเซ็นเซอร์อุณหภูมิของ CPU เองได้ เช่นเดียวกับ ThrottleStop และ HWiNFO จะข้ามไปหากติดตั้ง PawnIO ไว้แล้ว"
; Component description.
${LangFileString} Inst_DescStartMenu "เพิ่ม AcerCrate ลงในเมนูเริ่ม"
; Component description.
${LangFileString} Inst_DescDesktop "เพิ่มทางลัด AcerCrate บนเดสก์ท็อป"
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "เปิด AcerCrate ในพื้นที่แจ้งเตือนเมื่อคุณลงชื่อเข้าใช้ การควบคุมพัดลมทำงานตั้งแต่เริ่มระบบไม่ว่าในกรณีใด"
; Progress log line.
${LangFileString} Inst_Closing "กำลังปิด AcerCrate ที่ทำงานอยู่..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "กำลังเอาเวอร์ชันที่ติดตั้งไว้ออกจาก $0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "ไม่สามารถเอา AcerCrate เวอร์ชันที่ติดตั้งไว้ออกได้ (ข้อผิดพลาด $1) ต้องการดำเนินการต่อหรือไม่"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "ติดตั้ง PawnIO $0 ไว้แล้ว"
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "กำลังติดตั้ง PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "ไม่สามารถติดตั้งโปรแกรมควบคุม PawnIO ได้ (ข้อผิดพลาด $1) จนกว่าจะติดตั้งได้ AcerCrate จะแสดงอุณหภูมิ CPU จากเฟิร์มแวร์ซึ่งแม่นยำน้อยกว่า"
; Progress log line.
${LangFileString} Inst_ServiceRegistering "กำลังลงทะเบียนบริการ AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "ไม่สามารถลงทะเบียนบริการ AcerCrate ได้ (ข้อผิดพลาด $0)"
; Progress log line.
${LangFileString} Inst_ServiceStarting "กำลังเริ่มบริการ AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "บริการ AcerCrate ไม่เริ่มทำงาน (ข้อผิดพลาด $0) บันทึกของบริการอยู่ที่ %ProgramData%\AcerCrate\logs"
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "การควบคุมพัดลม ประสิทธิภาพ และไฟสำหรับแล็ปท็อป Acer Nitro และ Predator"
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "การควบคุมพัดลม ประสิทธิภาพ และไฟ"
${LangFileString} Inst_Needs64Bit "AcerCrate ต้องใช้ Windows แบบ 64 บิต"
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate ต้องใช้ Windows 10 เวอร์ชัน 2004 (บิลด์ ${MIN_WINDOWS_BUILD}) หรือใหม่กว่า"
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "กำลังหยุดบริการ AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "ต้องการเอาการตั้งค่าและบันทึกของ AcerCrate ออกด้วยหรือไม่"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "ไฟล์ AcerCrate บางไฟล์กำลังถูกใช้งานอยู่ และจะถูกเอาออกเมื่อเริ่ม Windows ใหม่"
