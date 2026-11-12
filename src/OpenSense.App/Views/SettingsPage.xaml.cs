using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenSense.App.Localization;
using OpenSense.App.ViewModels;
using Windows.System;
using Windows.Win32;
using System.Diagnostics;
using System.ComponentModel;

namespace OpenSense.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
        Updates = App.Current.Services.GetRequiredService<UpdateViewModel>();
        InitializeComponent();
        // Recording takes keys at the page, before any control reacts to them (Tab, Space, arrows).
        PreviewKeyDown += OnShortcutKeyDown;
    }

    private void OnCenterPreferences(object sender, RoutedEventArgs e) => App.Current.Services.GetRequiredService<OpenSense.App.Services.NavigationService>().Navigate("preferences");
    public SettingsViewModel ViewModel { get; }

    public UpdateViewModel Updates { get; }

    public static string VersionText(string version) => Strings.Format("Settings_Version", version);
    public static string CrateVersion => $"Acer Crate {typeof(App).Assembly.GetName().Version?.ToString(3)}";

    private async void OnUninstall(object sender, RoutedEventArgs e)
    {
        var uninstaller = Path.Combine(AppContext.BaseDirectory, "Uninstall.exe");
        if (!File.Exists(uninstaller))
        {
            UninstallError.Message = T("Для этой копии установщик удаления не найден. Установленную версию можно удалить в параметрах Windows.");
            UninstallError.IsOpen = true;
            return;
        }
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot, RequestedTheme = ActualTheme,
            Title = T("Удалить Acer Crate?"),
            Content = T("Откроется мастер удаления приложения. Пользовательские настройки сохранятся."),
            PrimaryButtonText = T("Удалить"), CloseButtonText = T("Отмена"), DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            App.Current.Services.GetRequiredService<OpenSense.App.Services.SettingsService>().Flush();
            Process.Start(new ProcessStartInfo(uninstaller) { UseShellExecute = true, Verb = "runas" })?.Dispose();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) { }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            UninstallError.Message = ex.Message;
            UninstallError.IsOpen = true;
        }
    }

    /// <summary>The language a language picker entry is written in (the "use system setting" entry is in the app's).</summary>
    public static string TextLanguage(string tag) => tag.Length > 0 ? tag : AppLanguage.Current;

    private void OnShortcutKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!ViewModel.RecordingShortcut)
            return;
        // While recording, keys are for the shortcut, not for moving focus or pressing the button.
        e.Handled = true;
        ViewModel.TryRecordShortcut(e.Key, IsDown(VirtualKey.Control), IsDown(VirtualKey.Menu), IsDown(VirtualKey.Shift),
            IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows));
    }

    private void OnShortcutLostFocus(object sender, RoutedEventArgs e) => ViewModel.CancelShortcutRecording();

    /// <summary>The physical key state; WinUI's per-thread key state reported held modifiers as up.</summary>
    private static bool IsDown(VirtualKey key) => (PInvoke.GetAsyncKeyState((int)key) & 0x8000) != 0;
}
