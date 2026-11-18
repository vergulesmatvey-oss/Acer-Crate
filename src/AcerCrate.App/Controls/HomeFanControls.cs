using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AcerCrate.App.Services;
using AcerCrate.App.ViewModels;
using AcerCrate.Core.Hardware;
using AcerCrate.Core.Monitoring;

namespace AcerCrate.App.Controls;

public sealed class HomeFanControls : StackPanel
{
    private readonly MonitorViewModel monitor = App.Current.Services.GetRequiredService<MonitorViewModel>();
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly List<(FanId Id, TextBlock Reading, CrateMeter Gauge)> rows = [];
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;

    public HomeFanControls()
    {
        Spacing = 10;
        timer = DispatcherQueue.CreateTimer();
        timer.Tick += (_, _) => Refresh();
        timer.Interval = TimeSpan.FromSeconds(1);
        Loaded += (_, _) => { Build(); monitor.Fans.CollectionChanged += OnCollectionChanged; };
        Unloaded += (_, _) => { monitor.Fans.CollectionChanged -= OnCollectionChanged; timer.Stop(); };
        AcerCrate.App.Helpers.UiActivity.Track(this, timer, Refresh);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Build();

    private void Build()
    {
        Children.Clear(); rows.Clear();
        foreach (var fan in monitor.Fans)
        {
            var group = new StackPanel { Spacing = 5 };
            var header = new Grid { ColumnSpacing = 8 };
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock
            {
                Text = fan.Name, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            var reading = new TextBlock { FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            Grid.SetColumn(reading, 1); header.Children.Add(reading); group.Children.Add(header);
            var gauge = new CrateMeter { Maximum = 8000 };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(gauge, fan.Name);
            group.Children.Add(gauge);
            rows.Add((fan.Id, reading, gauge)); Children.Add(group);
        }
        Refresh();
    }

    private void Refresh()
    {
        timer.Interval = TimeSpan.FromSeconds(Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3));
        var sample = session.Latest;
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var rpm = LiveTelemetry.FanRpm(sample, row.Id, now);
            row.Reading.Text = AcerCrate.App.Helpers.Units.Rpm(rpm);
            row.Gauge.Value = rpm ?? 0;
        }
    }
}
