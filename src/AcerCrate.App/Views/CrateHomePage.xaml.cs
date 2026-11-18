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
    private int artworkRequest;
    private bool stacked;
    public MonitorViewModel Monitor => monitor;

    public CrateHomePage()
    {
        InitializeComponent();
        HomeModes.PlaceFeedbackIn(ModeFeedback);
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

    private void OnOverviewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 920;
        if (stacked == narrow) return;

        stacked = narrow;
        Grid.SetColumnSpan(Hero, narrow ? 2 : 1);
        Grid.SetColumn(MonitorArea, narrow ? 0 : 1);
        Grid.SetColumnSpan(MonitorArea, narrow ? 2 : 1);
        Grid.SetRow(MonitorArea, narrow ? 1 : 0);
        Grid.SetColumn(ModeFeedback, narrow ? 0 : 1);
        Grid.SetColumnSpan(ModeFeedback, narrow ? 2 : 1);
        Grid.SetRow(ModeFeedback, narrow ? 2 : 1);
        OverviewColumns.ColumnSpacing = narrow ? 0 : 44;
        MonitorArea.Margin = new Thickness(0, narrow ? 30 : 0, 0, 0);
        LaptopImage.Height = narrow ? 198 : 214;
    }

    private void AnimateEntrance()
    {
        if (!CrateAppearance.AnimationsEnabled) return;
        var motion = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        foreach (var panel in new[] { Hero, MonitorArea, ModeFeedback })
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
        var device = session.DeviceName ?? (preview ? "Acer Nitro AN515-58" : null);
        Model.Text = (device ?? "Acer Nitro").ToUpperInvariant();
        CpuName.Text = session.CpuName ?? "";
        GpuName.Text = session.GpuName ?? "";
        CpuName.Visibility = CpuName.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        GpuName.Visibility = GpuName.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        timer.Interval = TimeSpan.FromSeconds(Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3));
        _ = UpdateArtworkAsync(device);
        var sample = session.Latest;
        bool fresh = sample is not null && DateTime.UtcNow - sample.Timestamp.ToUniversalTime() < TimeSpan.FromSeconds(6);
        CpuLoad.Text = fresh ? monitor.CpuLoadText : "—";
        GpuLoad.Text = fresh ? monitor.GpuLoadText : "—";
        CpuTemp.Text = fresh ? monitor.CpuTemperatureText : "—";
        GpuTemp.Text = fresh ? monitor.GpuTemperatureText : "—";
        CpuLoadBar.Value = fresh ? sample!.CpuLoad ?? 0 : 0;
        GpuLoadBar.Value = fresh ? sample!.GpuLoad ?? 0 : 0;
        CpuTemperatureBar.Value = fresh ? sample!.CpuTemperature ?? 0 : 0;
        GpuTemperatureBar.Value = fresh ? sample!.GpuTemperature ?? 0 : 0;
        Supply.Text = fresh ? sample!.OnAcPower ? T("Сеть") : T("Батарея") : "—";
        GpuState.Text = fresh ? sample!.GpuAsleep ? T("Простой") : T("Активна") : "—";
        GpuVoltage.Text = fresh && sample!.GpuVoltage is { } volts ? $"{volts:0.000} {T("В")}" : "—";
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
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
                return (Total: total, Free: free, Names: string.Join(" + ", names));
            });
            Disk.Text = totals.Total > 0 ? Capacity(totals.Total - totals.Free, totals.Total) + $" · {100.0 * (totals.Total - totals.Free) / totals.Total:0}%" : "—";
            DiskBar.Value = totals.Total > 0 ? 100.0 * (totals.Total - totals.Free) / totals.Total : 0;
            DiskLabel.Text = totals.Total > 0 ? T("Диски · ") + totals.Names : T("Локальные диски");

        }
        catch (IOException) { Disk.Text = "—"; DiskBar.Value = 0; }
        finally { readingDisks = false; }
    }

    private void OnSettingsChanged(AcerCrate.Core.Settings.UserSettings _) =>
        DispatcherQueue.TryEnqueue(() => { if (IsLoaded && App.Current.IsWindowVisible) Refresh(); });

    private async Task UpdateArtworkAsync(string? device)
    {
        var ui = settings.Current.Ui;
        var key = (ui.CustomLaptopImage, ui.ArtworkModelQuery, device, ui.DownloadModelArtwork);
        if (artworkKey == key) return;
        artworkKey = key;
        var request = ++artworkRequest;
        var path = await ModelArtwork.ResolveAsync(ui, device);
        if (request != artworkRequest || !IsLoaded) return;
        var match = string.IsNullOrWhiteSpace(ui.CustomLaptopImage) ? ModelArtwork.Match(ui.ArtworkModelQuery ?? device) : null;
        var familyCaption = match is { IsExact: false }
            ? CrateUi.L("Series illustration · configuration may differ", "Изображение серии · комплектация может отличаться") : "";
        if (path == lastArtwork)
        {
            SetArtworkCaption(!LaptopImage.HasPhoto ? T("Изображение можно выбрать в настройках") : familyCaption);
            return;
        }
        lastArtwork = path;
        try
        {
            var image = path is not null && File.Exists(path)
                ? await Pictures.FromBytesAsync(await File.ReadAllBytesAsync(path), 520)
                : null;
            if (request != artworkRequest) return;
            LaptopImage.Show(image, match?.Entry.Framing, match?.Entry.Title ?? device);
            SetArtworkCaption(image is null ? T("Изображение можно выбрать в настройках") : familyCaption);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (request != artworkRequest) return;
            LaptopImage.Show(null, null, null);
            SetArtworkCaption(T("Выберите другое изображение в настройках"));
        }
    }

    private void SetArtworkCaption(string caption)
    {
        ArtworkStatus.Text = caption;
        ArtworkStatus.Visibility = caption.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string Capacity(ulong used, ulong total) => F("{0:0.#} / {1:0.#} ГиБ", used / 1073741824.0, total / 1073741824.0);
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

