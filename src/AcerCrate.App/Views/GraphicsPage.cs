using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Controls;
using AcerCrate.App.Services;
using AcerCrate.App.ViewModels;
using static AcerCrate.App.Controls.CrateUi;

namespace AcerCrate.App.Views;

public sealed partial class GraphicsPage : Page
{
    private readonly SystemViewModel system = App.Current.Services.GetRequiredService<SystemViewModel>();
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly TextBlock status = Text("", 14);
    private readonly TextBlock hardware = Text("", 14);
    private readonly TextBlock capability = Text("", 13, .65);
    private readonly List<Button> choices = [];
    private readonly Button restart;

    public GraphicsPage()
    {
        InitializeComponent();
        var body = Body();
        body.Children.Add(new PageHeading { Title = L("Graphics", "Графика"), Description = L("Display routing, hybrid graphics and GPU selection", "Гибридная графика и выбор видеокарты"), Glyph = "\uE7F4" });
        body.Children.Add(Card(L("Detected adapters", "Обнаруженные адаптеры"), session.DeviceName, hardware));
        var modes = new StackPanel { Spacing = 14 };
        modes.Children.Add(status); modes.Children.Add(capability);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        string[] names = [L("Hybrid / iGPU + dGPU", "Гибридный / iGPU + dGPU"), L("Discrete GPU only", "Только дискретная"), L("Automatic", "Автоматически")];
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var button = Button(names[i], (_, _) => system.GpuModeIndex = index);
            choices.Add(button); buttons.Children.Add(button);
        }
        modes.Children.Add(buttons);
        modes.Children.Add(Text(L("Hybrid keeps integrated graphics available and lets applications use the discrete GPU. It is not an integrated-only mode. Firmware mode changes can require a restart.", "Гибридный режим оставляет встроенную графику доступной и позволяет приложениям использовать дискретную. Это не режим «только iGPU». Для смены режима прошивки может потребоваться перезагрузка."), 13, .65));
        restart = Button(L("Restart now", "Перезагрузить сейчас"), (_, _) => system.RestartNowCommand.Execute(null));
        modes.Children.Add(restart);
        body.Children.Add(Card(L("GPU mode / MUX", "Режим GPU / MUX"), null, modes));
        var preferences = new StackPanel { Spacing = 12 };
        preferences.Children.Add(Text(L("For individual games, Windows can select a power-saving or high-performance GPU even when a firmware MUX switch is not exposed.", "Для отдельных игр Windows позволяет выбрать энергосберегающий или производительный GPU, даже если переключатель MUX недоступен."), 13, .65));
        preferences.Children.Add(Button(L("Open Windows graphics settings", "Открыть настройки графики Windows"), (_, _) => Process.Start(new ProcessStartInfo("ms-settings:display-advancedgraphics") { UseShellExecute = true })?.Dispose()));
        body.Children.Add(Card(L("Application GPU preference", "GPU для приложений"), null, preferences));
        body.Children.Add(Card(L("Model compatibility", "Совместимость модели"), L("Nitro / Nitro V / Predator, 2021 and newer", "Nitro / Nitro V / Predator, 2021 года и новее"), Text(L("Controls follow the capabilities reported by this laptop. Model year alone does not enable unsupported firmware methods. Not all configurations have a MUX switch.", "Функции определяются возможностями этого ноутбука. Год модели не включает отсутствующие методы прошивки. Переключатель MUX есть не во всех комплектациях."), 13, .65)));
        Content = new ScrollView { Content = body };
        Loaded += async (_, _) => { system.PropertyChanged += Changed; system.Attach(); Refresh(); await ReadAdaptersAsync(); };
        Unloaded += (_, _) => system.PropertyChanged -= Changed;
    }

    private void Changed(object? sender, PropertyChangedEventArgs e) => Refresh();
    private void Refresh()
    {
        string[] names = [L("Hybrid", "Гибридный"), L("Discrete only", "Только дискретная"), L("Automatic", "Автоматически")];
        status.Text = L("Firmware setting: ", "Настройка прошивки: ") + names[Math.Clamp(system.GpuModeIndex, 0, 2)];
        capability.Text = system.GpuSwitchAvailable
            ? L("Firmware GPU switching is available. Changing mode asks for confirmation.", "Прошивка поддерживает переключение GPU. Изменение потребует подтверждения.")
            : L("This firmware/configuration does not expose a supported MUX switch. Device disabling is not used.", "Эта прошивка или комплектация не предоставляет поддерживаемый MUX. Отключение устройств не используется.");
        for (var i = 0; i < choices.Count; i++)
        {
            choices[i].IsEnabled = !App.Current.Options.Preview && system.GpuSwitchAvailable && !system.GpuSwitchBusy && (i != 2 || system.GpuAutomaticAvailable);
            choices[i].Opacity = i == system.GpuModeIndex ? 1 : .65;
            choices[i].Visibility = i == 2 && !system.GpuAutomaticAvailable ? Visibility.Collapsed : Visibility.Visible;
        }
        restart.Visibility = system.RestartPending ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ReadAdaptersAsync()
    {
        try
        {
            hardware.Text = await Task.Run(() =>
            {
                using var query = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                using var results = query.Get();
                var names = new List<string>();
                foreach (ManagementObject adapter in results) using (adapter) if (adapter["Name"] is string name) names.Add(name);
                return string.Join("\n", names.Distinct());
            });
        }
        catch (Exception e) when (e is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException) { hardware.Text = session.GpuName ?? e.Message; }
    }
}
