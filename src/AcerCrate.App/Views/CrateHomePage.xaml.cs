using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Services;
using AcerCrate.App.Helpers;
using AcerCrate.App.ViewModels;
using AcerCrate.App.Controls;
using AcerCrate.Core.Hardware;

namespace AcerCrate.App.Views;

public sealed partial class CrateHomePage : Page
{
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly MonitorViewModel monitor = App.Current.Services.GetRequiredService<MonitorViewModel>();
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;
    private DateTime nextDiskRead;
    private bool readingDisks;
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private string? lastArtwork = "";
    private (string? Custom, string? Query, string? Device, bool Download)? artworkKey;
    private readonly List<(string Name, StackPanel Row, TextBlock Caption, PinnedProgressBar Bar)> driveRows = [];
    private int artworkRequest;
    public LightingViewModel Lighting { get; } = App.Current.Services.GetRequiredService<LightingViewModel>();
    public MonitorViewModel Monitor => monitor;

    public CrateHomePage()
    {
        InitializeComponent();
        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) =>
        {
            settings.Changed += OnSettingsChanged;
            AnimateEntrance();
        };
        Unloaded += (_, _) => { settings.Changed -= OnSettingsChanged; artworkRequest++; artworkKey = null; lastArtwork = ""; };
        UiActivity.Track(this, timer, Refresh);
    }

    private void AnimateEntrance()
    {
        if (!CrateAppearance.AnimationsEnabled) return;
        var motion = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        foreach (var panel in new[] { Hero, MonitorArea })
        {
            var fade = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(260)),
                BeginTime = panel == Hero ? TimeSpan.Zero : TimeSpan.FromMilliseconds(65)
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fade, panel);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fade, "Opacity");
            motion.Children.Add(fade);
        }
        motion.Begin();
    }

    private void Refresh()
    {
        var preview = App.Current.Options.Preview;
        Model.Text = preview ? "Acer Crate" : session.DeviceName ?? "Acer Nitro";
        CpuName.Text = session.CpuName ?? "";
        GpuName.Text = session.GpuName ?? "";
        timer.Interval = TimeSpan.FromSeconds(Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3));
        _ = UpdateArtworkAsync();
        var sample = session.Latest;
        bool fresh = sample is not null && DateTime.UtcNow - sample.Timestamp.ToUniversalTime() < TimeSpan.FromSeconds(6);
        CpuLoad.Text = fresh ? monitor.CpuLoadText : "—";
        GpuLoad.Text = fresh ? monitor.GpuLoadText : "—";
        CpuTemp.Text = fresh ? monitor.CpuTemperatureText : "—";
        GpuTemp.Text = fresh ? monitor.GpuTemperatureText : "—";
        CpuLoadBar.Value = fresh ? sample!.CpuLoad ?? 0 : 0;
        GpuLoadBar.Value = fresh ? sample!.GpuLoad ?? 0 : 0;
        Supply.Text = fresh ? sample!.OnAcPower ? T("Сеть") : T("Батарея") : "—";
        GpuState.Text = fresh ? sample!.GpuAsleep ? T("Простой") : T("Активна") : "—";
        if (preview) return;
        var memory = new MemoryStatus { Size = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (GlobalMemoryStatusEx(ref memory) && memory.Total > 0)
        {
            Ram.Text = Capacity(memory.Total - memory.Free, memory.Total) + $" · {memory.Load}%";
            RamBar.Value = memory.Load;
        }
        else
        {
            Ram.Text = "—";
            RamBar.Value = 0;
        }
        if (DateTime.UtcNow >= nextDiskRead && !readingDisks) _ = RefreshDisksAsync();
    }

    private async Task RefreshDisksAsync()
    {
        readingDisks = true;
        nextDiskRead = DateTime.UtcNow.AddSeconds(15);
        try
        {
            var totals = await Task.Run(() =>
            {
                ulong total = 0, free = 0;
                var names = new List<string>();
                var items = new List<(string Name, ulong Used, ulong Total)>();
                foreach (var disk in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (disk.DriveType != DriveType.Fixed || !disk.IsReady) continue;
                        var size = disk.TotalSize;
                        var available = disk.TotalFreeSpace;
                        if (size <= 0) continue;
                        total += (ulong)size;
                        free += (ulong)Math.Clamp(available, 0, size);
                        names.Add(disk.Name.TrimEnd('\\'));
                        items.Add((disk.Name.TrimEnd('\\'), (ulong)(size - available), (ulong)size));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
                return (Total: total, Free: free, Names: string.Join(" + ", names), Items: items);
            });
            Disk.Text = totals.Total > 0 ? Capacity(totals.Total - totals.Free, totals.Total) + $" · {100.0 * (totals.Total - totals.Free) / totals.Total:0}%" : "—";
            DiskBar.Value = totals.Total > 0 ? 100.0 * (totals.Total - totals.Free) / totals.Total : 0;
            DiskLabel.Text = totals.Total > 0 ? T("Диски · ") + totals.Names : T("Локальные диски");
            if (!driveRows.Select(row => row.Name).SequenceEqual(totals.Items.Select(drive => drive.Name)))
            {
                DriveDetails.Children.Clear();
                driveRows.Clear();
                foreach (var drive in totals.Items)
                {
                    var row = new StackPanel { Spacing = 6 };
                    var caption = new TextBlock { FontSize = 12 };
                    var bar = new PinnedProgressBar
                    {
                        Minimum = 0, Maximum = 100, Height = 3, CornerRadius = new CornerRadius(0),
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CrateAccent"]
                    };
                    row.Children.Add(caption);
                    row.Children.Add(bar);
                    driveRows.Add((drive.Name, row, caption, bar));
                    DriveDetails.Children.Add(row);
                }
            }
            for (var i = 0; i < totals.Items.Count; i++)
            {
                var drive = totals.Items[i];
                driveRows[i].Caption.Text = drive.Name + " · " + Capacity(drive.Used, drive.Total) + $" · {100.0 * drive.Used / drive.Total:0}%";
                driveRows[i].Bar.Value = 100.0 * drive.Used / drive.Total;
            }
        }
        catch (IOException) { Disk.Text = "—"; DiskBar.Value = 0; }
        finally { readingDisks = false; }
    }

    private void OnSettingsChanged(AcerCrate.Core.Settings.UserSettings _) =>
        DispatcherQueue.TryEnqueue(() => { if (IsLoaded && App.Current.IsWindowVisible) Refresh(); });

    private async Task UpdateArtworkAsync()
    {
        var ui = settings.Current.Ui;
        var key = (ui.CustomLaptopImage, ui.ArtworkModelQuery, session.DeviceName, ui.DownloadModelArtwork);
        if (artworkKey == key) return;
        artworkKey = key;
        var request = ++artworkRequest;
        var path = await ModelArtwork.ResolveAsync(ui, session.DeviceName);
        if (request != artworkRequest || !IsLoaded) return;
        if (path == lastArtwork)
        {
            if (LaptopImage.Source is not null)
                ArtworkStatus.Text = ModelArtwork.Match(ui.ArtworkModelQuery ?? session.DeviceName) is { IsExact: false } && string.IsNullOrWhiteSpace(ui.CustomLaptopImage)
                    ? AcerCrate.App.Controls.CrateUi.L("Series illustration · configuration may differ", "Изображение серии · комплектация может отличаться") : "";
            return;
        }
        lastArtwork = path;
        try
        {
            var image = path is not null && File.Exists(path)
                ? await Pictures.FromBytesAsync(await File.ReadAllBytesAsync(path), 520)
                : null;
            if (request != artworkRequest) return;
            LaptopImage.Source = image;
            LaptopImage.Visibility = image is not null ? Visibility.Visible : Visibility.Collapsed;
            var match = string.IsNullOrWhiteSpace(ui.CustomLaptopImage) ? ModelArtwork.Match(ui.ArtworkModelQuery ?? session.DeviceName) : null;
            ArtworkStatus.Text = image is null ? T("Изображение можно выбрать в настройках")
                : match is { IsExact: false } ? AcerCrate.App.Controls.CrateUi.L("Series illustration · configuration may differ", "Изображение серии · комплектация может отличаться") : "";
        }
        catch (IOException)
        {
            if (request != artworkRequest) return;
            LaptopImage.Visibility = Visibility.Collapsed;
            ArtworkStatus.Text = T("Выберите другое изображение в настройках");
        }
    }

    private static string Rpm(int? rpm) => rpm is {} speed ? F("{0:N0} об/мин", speed) : "—";
    private static string Capacity(ulong used, ulong total) => F("{0:0.#} / {1:0.#} ГиБ", used / 1073741824.0, total / 1073741824.0);
    private void OpenLighting(object sender, RoutedEventArgs e) => Navigate("lighting");
    private void OpenCooling(object sender, RoutedEventArgs e) => Navigate("fans");
    private void OpenMonitoring(object sender, RoutedEventArgs e) => Navigate("monitoring");
    private static void Navigate(string page) => App.Current.Services.GetRequiredService<NavigationService>().Navigate(page);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Size, Load;
        public ulong Total, Free, TotalPage, FreePage, TotalVirtual, FreeVirtual, FreeExtended;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}

