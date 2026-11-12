using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using OpenSense.Core.Control;
using OpenSense.Core.Settings;

namespace OpenSense.App.Services;

public sealed class ProfileShortcuts(DispatcherQueue dispatcher, ILogger<OpenShortcut> log, SettingsService settings,
    DeviceSession session, PersonalModeService modes, NotificationService notifications) : IDisposable
{
    private readonly List<OpenShortcut> keys = [];
    private OpenShortcut? cycle;
    private bool enabled, cycleEnabled, switching;

    public void Initialize() { settings.Changed += Changed; Refresh(); }
    private void Changed(UserSettings _) => dispatcher.TryEnqueue(Refresh);

    private void Refresh()
    {
        RefreshCycle();
        var wanted = settings.Current.Ui.ProfileHotkeys;
        if (wanted == enabled) return;
        Clear(); enabled = wanted;
        if (!wanted) return;
        for (var index = 0; index < 4; index++)
        {
            var selected = index;
            var key = new OpenShortcut(log, "Nivo.Profile" + index);
            key.Pressed += async () =>
            {
                if (session.State != SessionState.Ready || App.Current.IsExiting) return;
                try
                {
                    if (selected == 3) await modes.ApplyAsync();
                    else await modes.SelectNativeAsync((LaptopMode)selected);
                }
                catch (Exception error) { notifications.Show(T("Горячая клавиша профиля"), T(error.Message)); }
            };
            if (!key.Set(new KeyShortcut(0x31 + index, Control: true, Alt: true)))
            {
                key.Dispose(); Clear(); enabled = false;
                notifications.Show(T("Горячие клавиши"), T("Ctrl+Alt+1/2/3/4 заняты. Сочетания не зарегистрированы."));
                return;
            }
            keys.Add(key);
        }
    }

    private void RefreshCycle()
    {
        var wanted = settings.Current.Ui.ModeCycleHotkey;
        if (wanted == cycleEnabled) return;
        cycle?.Dispose(); cycle = null; cycleEnabled = wanted;
        if (!wanted) return;
        var key = new OpenShortcut(log, "AcerCrate.ModeCycle");
        key.Pressed += async () =>
        {
            if (switching || session.State != SessionState.Ready || App.Current.IsExiting) return;
            switching = true;
            try
            {
                var sample = session.Latest;
                var next = ModeCyclePolicy.Next(sample?.OperatingMode ?? session.Settings.Profile.OperatingMode,
                    session.Capabilities.OperatingModes, sample?.PowerLimit ?? PowerLimit.None);
                if (next is { } mode) await modes.SelectNativeAsync(mode);
            }
            catch (Exception error) { notifications.Show(T("Горячая клавиша профиля"), T(error.Message)); }
            finally { switching = false; }
        };
        if (!key.Set(new KeyShortcut(0x46, Control: true, Alt: true)))
        {
            key.Dispose();
            notifications.Show(T("Горячие клавиши"), T("Ctrl+Alt+F занято другим приложением. Переключение режимов не зарегистрировано."));
            return;
        }
        cycle = key;
    }

    private void Clear() { foreach (var key in keys) key.Dispose(); keys.Clear(); }
    public void Dispose() { settings.Changed -= Changed; Clear(); cycle?.Dispose(); cycle = null; }
}