using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using AcerCrate.Core.Control;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Services;

public sealed class ProfileShortcuts(DispatcherQueue dispatcher, ILogger<OpenShortcut> log, ILogger<ProfileShortcuts> activityLog, SettingsService settings,
    DeviceSession session, PersonalModeService modes, NotificationService notifications, ModeOsdService osd) : IDisposable
{
    private readonly List<OpenShortcut> keys = [];
    private OpenShortcut? cycle;
    private bool enabled, switching, disposed, conflictReported;
    private LaptopMode? pending, cursor;
    private DispatcherQueueTimer? retry;

    public void Initialize()
    {
        settings.Changed += Changed;
        retry = dispatcher.CreateTimer(); retry.Interval = TimeSpan.FromSeconds(10);
        retry.Tick += (_, _) => RefreshCycle();
        Refresh();
    }
    private void Changed(UserSettings _) => dispatcher.TryEnqueue(Refresh);

    private void Refresh()
    {
        if (disposed) return;
        RefreshCycle();
        var wanted = settings.Current.Ui.ProfileHotkeys;
        if (wanted == enabled) return;
        Clear(); enabled = wanted;
        if (!wanted) return;
        for (var index = 0; index < 4; index++)
        {
            var selected = index;
            var key = new OpenShortcut(log, "AcerCrate.Profile" + index);
            key.Pressed += async () =>
            {
                if (session.State != SessionState.Ready || App.Current.IsExiting) return;
                try
                {
                    if (selected == 3) await modes.ApplyAsync();
                    else { osd.ShowRequested((LaptopMode)selected); await modes.SelectNativeAsync((LaptopMode)selected); osd.CommandAccepted((LaptopMode)selected); }
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
        if (disposed) return;
        var wanted = settings.Current.Ui.ModeCycleHotkey;
        if (!wanted) { cycle?.Dispose(); cycle = null; retry?.Stop(); pending = cursor = null; conflictReported = false; return; }
        if (cycle?.IsSet == true) { retry?.Stop(); return; }
        var key = new OpenShortcut(log, "AcerCrate.ModeCycle");
        key.Pressed += async () =>
        {
            if (disposed || !settings.Current.Ui.ModeCycleHotkey || session.State != SessionState.Ready || App.Current.IsExiting) return;
            var sample = session.Latest;
            var next = ModeCyclePolicy.NextRequested(cursor ?? session.Settings.Profile.LaptopMode, sample?.OperatingMode,
                session.Capabilities.OperatingModes, sample?.PowerLimit ?? PowerLimit.None);
            if (next is not { } requested) return;
            cursor = pending = requested;
            osd.ShowRequested(requested);
            if (switching) return;
            switching = true;
            try
            {
                while (pending is { } mode && !disposed && !App.Current.IsExiting && settings.Current.Ui.ModeCycleHotkey)
                {
                    pending = null;
                    activityLog.LogInformation("Mode shortcut {Previous} -> {Requested}", session.Settings.Profile.LaptopMode, mode);
                    await modes.SelectNativeAsync(mode);
                    osd.CommandAccepted(mode);
                }
            }
            catch (Exception error) { pending = null; osd.ShowFailure(); activityLog.LogWarning(error, "Mode cycle failed"); notifications.Show(T("Горячая клавиша профиля"), T(error.Message)); }
            finally { switching = false; cursor = null; }
        };
        if (!key.Set(new KeyShortcut(0x46, Control: true, Alt: true)))
        {
            key.Dispose();
            retry?.Start();
            if (!conflictReported)
            {
                conflictReported = true;
                notifications.Show(T("Горячие клавиши"), T("Ctrl+Alt+F занято другим приложением. Переключение режимов не зарегистрировано."));
            }
            return;
        }
        cycle = key; conflictReported = false; retry?.Stop();
        activityLog.LogInformation("Ctrl+Alt+F registered");
    }

    private void Clear() { foreach (var key in keys) key.Dispose(); keys.Clear(); }
    public void Dispose() { disposed = true; pending = cursor = null; retry?.Stop(); settings.Changed -= Changed; Clear(); cycle?.Dispose(); cycle = null; }
}
