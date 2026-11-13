using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Services;
using AcerCrate.App.Helpers;
using AcerCrate.Core.Hardware;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Controls;

public sealed partial class CurrentProfileSummary : UserControl
{
    public CurrentProfileSummary()
    {
        var session = App.Current.Services.GetRequiredService<DeviceSession>();
        var settings = App.Current.Services.GetRequiredService<SettingsService>();
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = T("Текущее состояние"), FontSize = 21 });
        var chosen = new TextBlock { FontSize = 15, TextWrapping = TextWrapping.Wrap };
        var device = new TextBlock { FontSize = 13, Opacity = .75, TextWrapping = TextWrapping.Wrap };
        var cooling = new TextBlock { FontSize = 13, Opacity = .75 };
        var fans = new TextBlock { FontSize = 13, Opacity = .75, TextWrapping = TextWrapping.Wrap };
        var status = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = .65 };
        panel.Children.Add(chosen); panel.Children.Add(device); panel.Children.Add(cooling); panel.Children.Add(fans); panel.Children.Add(status);
        var timer = DispatcherQueue.CreateTimer();
        void Refresh()
        {
            timer.Interval = TimeSpan.FromSeconds(Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3));
            var profile = session.Settings.Profile; var telemetry = session.Latest;
            var own = settings.Current.Ui.PersonalModeSelected && settings.Current.PersonalMode.Matches(profile);
            chosen.Text = T("Выбран: ") + (own ? ModeTitle(settings.Current.PersonalMode.Name) : PersonalModeService.ModeName(profile.LaptopMode));
            device.Text = T("Прошивка: ") + (telemetry?.OperatingMode is {} mode ? AcerCrate.App.Localization.Names.OperatingMode(mode) : T("ожидание данных"));
            cooling.Text = T("Охлаждение: ") + AcerCrate.App.Localization.Names.FanMode(profile.Mode);
            fans.Text = "CPU: " + Units.Rpm(telemetry?.Fan(FanId.Cpu)?.Rpm) + " · GPU: " + Units.Rpm(telemetry?.Fan(FanId.Gpu)?.Rpm);
            status.Text = telemetry is null || DateTime.UtcNow - telemetry.Timestamp.ToUniversalTime() > TimeSpan.FromSeconds(6) ? T("Ожидание свежих показаний") :
                telemetry.ControlConfirmed && telemetry.AppliedProfile is {} applied && PersonalMode.Equivalent(profile, applied) ? T("Режим и охлаждение подтверждены устройством") : T("Проверка применения параметров");
        }
        timer.Tick += (_, _) => Refresh();
        UiActivity.Track(this, timer, Refresh);
        Content = new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = panel };
    }
}
