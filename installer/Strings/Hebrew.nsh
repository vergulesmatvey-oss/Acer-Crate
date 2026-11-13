; AcerCrate's own installer text in Hebrew, next to the app's Strings\he-IL.
; $0, $1 and ${MIN_WINDOWS_BUILD} are filled in by the installer: keep them as they are.

!insertmacro LANGFILE_EXT "Hebrew"

; Check box on the last page: open the app when setup closes.
${LangFileString} Inst_Run "הפעל את AcerCrate"
; Under the GPL text on the license page.
${LangFileString} Inst_LicenseNote "אין צורך לקבל את הרישיון הזה כדי להשתמש ב-AcerCrate. הוא מעניק לך את הזכות לשתף ולשנות אותו, וקובע את התנאים לכך."
; Component name.
${LangFileString} Inst_SecCore "AcerCrate (חובה)"
; Component name. PawnIO is a driver's name: keep it.
${LangFileString} Inst_SecPawnIO "מנהל ההתקן PawnIO (חובה)"
; Component name.
${LangFileString} Inst_SecStartMenu "קיצור דרך בתפריט 'התחל'"
; Component name.
${LangFileString} Inst_SecDesktop "קיצור דרך בשולחן העבודה"
; Component name.
${LangFileString} Inst_SecAutostart "הפעל עם Windows"
; Component description.
${LangFileString} Inst_DescCore "אפליקציית AcerCrate ושירות הרקע שלה, עם כל מה שהם צריכים כדי לפעול."
; Component description. ThrottleStop and HWiNFO are app names.
${LangFileString} Inst_DescPawnIO "מנהל התקן חתום שמאפשר ל-AcerCrate לקרוא את חיישן הטמפרטורה של ה-CPU עצמו, כמו ThrottleStop ו-HWiNFO. המערכת מדלגת עליו אם PawnIO כבר מותקן."
; Component description.
${LangFileString} Inst_DescStartMenu "מוסיף את AcerCrate לתפריט 'התחל'."
; Component description.
${LangFileString} Inst_DescDesktop "מוסיף קיצור דרך ל-AcerCrate בשולחן העבודה."
; Component description. Notification area = the Windows system tray.
${LangFileString} Inst_DescAutostart "פותח את AcerCrate באזור ההודעות בעת הכניסה. בקרת המאווררים פועלת מרגע ההפעלה בכל מקרה."
; Progress log line.
${LangFileString} Inst_Closing "סוגר את AcerCrate הפועל..."
; Progress log line. $0 = a folder.
${LangFileString} Inst_RemovingOld "מסיר את הגרסה המותקנת מ-$0..."
; Yes/No question. $1 = an error code.
${LangFileString} Inst_RemoveFailed "לא ניתן היה להסיר את הגרסה המותקנת של AcerCrate (שגיאה $1). להמשיך בכל זאת?"
; Progress log line. $0 = a version number.
${LangFileString} Inst_PawnIOPresent "PawnIO $0 כבר מותקן."
; Progress log line.
${LangFileString} Inst_PawnIOInstalling "מתקין את PawnIO..."
; $1 = an error code.
${LangFileString} Inst_PawnIOFailed "לא ניתן היה להתקין את מנהל ההתקן PawnIO (שגיאה $1). עד שהוא יותקן, AcerCrate יציג את טמפרטורת ה-CPU הפחות מדויקת של הקושחה."
; Progress log line.
${LangFileString} Inst_ServiceRegistering "רושם את השירות של AcerCrate..."
; $0 = an error code.
${LangFileString} Inst_ServiceRegisterFailed "לא ניתן היה לרשום את השירות של AcerCrate (שגיאה $0)."
; Progress log line.
${LangFileString} Inst_ServiceStarting "מפעיל את השירות של AcerCrate..."
; $0 = an error code. Keep the folder path.
${LangFileString} Inst_ServiceStartFailed "השירות של AcerCrate לא הופעל (שגיאה $0). היומן שלו נמצא ב-%ProgramData%\AcerCrate\logs."
; The service's description in the Windows Services list.
${LangFileString} Inst_ServiceDescription "בקרת מאווררים, ביצועים ותאורה למחשבים הניידים Acer Nitro ו-Predator."
; The Start menu shortcut's tooltip.
${LangFileString} Inst_ShortcutDescription "בקרת מאווררים, ביצועים ותאורה"
${LangFileString} Inst_Needs64Bit "AcerCrate דורש גרסת 64 סיביות של Windows."
; ${MIN_WINDOWS_BUILD} = a Windows build number.
${LangFileString} Inst_NeedsWindows "AcerCrate דורש Windows 10 גרסה 2004 (Build ${MIN_WINDOWS_BUILD}) ואילך."
; Progress log line (uninstall).
${LangFileString} Inst_ServiceStopping "עוצר את השירות של AcerCrate..."
; Yes/No question when uninstalling.
${LangFileString} Inst_RemoveUserData "להסיר גם את ההגדרות והיומנים של AcerCrate?"
; Uninstall message.
${LangFileString} Inst_RebootToFinish "חלק מהקבצים של AcerCrate נמצאים בשימוש ויוסרו כש-Windows יופעל מחדש."
