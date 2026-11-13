using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Services;
using AcerCrate.Core.Control;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Controls;

public sealed partial class PersonalModeEditor : UserControl
{
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly StackPanel panel = new() { Spacing = 14 };
    private readonly TextBox name = new() { Header = T("Название вашего режима"), MaxLength = 32 };
    private readonly ComboBox basis = new() { Header = T("Штатная основа"), ItemsSource = new[] { T("Тихий"), T("Баланс"), T("Производительность") }, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox cooling = new() { Header = T("Охлаждение"), ItemsSource = new[] { T("Автоматика"), T("Максимум"), T("Ручное") }, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ToggleSwitch autoBoost = new() { Header = T("Усиление автоматики при нагреве") };
    private readonly ToggleSwitch coolBoost = new() { Header = "CoolBoost" };
    private readonly ToggleSwitch includeLighting = new() { Header = T("Сохранить текущую подсветку") };
    private readonly ToggleSwitch includePower = new() { Header = T("Сохранить настройки зарядки и USB-питания") };
    private readonly TextBlock message = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly List<(AcerCrate.Core.Hardware.FanId Id, Slider Percent, ToggleSwitch Curve, StackPanel Row)> rows = [];
    private ControlProfile draft;

    public PersonalModeEditor()
    {
        var saved = settings.Current.PersonalMode;
        draft = saved.Profile ?? session.Settings.Profile;
        panel.Children.Add(new TextBlock { Text = T("Мой режим"), FontSize = 22 });
        panel.Children.Add(new TextBlock { Text = T("Сохраните свою комбинацию параметров и включайте её четвёртой кнопкой на главной. Сначала можно взять текущие настройки устройства."), TextWrapping = TextWrapping.Wrap, Opacity = .65, FontSize = 13 });
        name.Text = saved.Name; panel.Children.Add(name);
        basis.SelectedIndex = (int)(saved.Profile?.LaptopMode ?? LaptopMode.Balanced); panel.Children.Add(basis);
        cooling.SelectedIndex = (int)draft.Mode; panel.Children.Add(cooling);
        autoBoost.IsOn = draft.AutoBoost; panel.Children.Add(autoBoost);
        coolBoost.IsOn = draft.CoolBoost == true; coolBoost.Visibility = session.Capabilities.CoolBoost ? Visibility.Visible : Visibility.Collapsed; panel.Children.Add(coolBoost);
        foreach (var fan in session.Capabilities.ControllableFans)
        {
            var value = draft.ManualFor(fan.Id);
            var row = new StackPanel { Spacing = 6 };
            row.Children.Add(new TextBlock { Text = AcerCrate.App.Localization.Names.Fan(fan.Id, session.Capabilities.Fans), FontSize = 15 });
            var curve = new ToggleSwitch { Header = T("Использовать сохранённую кривую"), IsOn = value.UseCurve };
            var slider = new Slider { Header = T("Добавка к автоматике, %"), Minimum = 0, Maximum = 100, StepFrequency = 1, Value = value.Percent };
            curve.Toggled += (_, _) => slider.IsEnabled = !curve.IsOn;
            row.Children.Add(curve); row.Children.Add(slider); panel.Children.Add(row); rows.Add((fan.Id, slider, curve, row));
        }
        includeLighting.IsOn = saved.Profile is null || saved.Lighting is not null;
        includeLighting.Visibility = session.Capabilities.Lights.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        includePower.IsOn = saved.Profile is null || saved.Power is not null;
        includePower.Visibility = session.Capabilities.Battery.ChargeLimit || session.Capabilities.UsbCharging ? Visibility.Visible : Visibility.Collapsed;
        panel.Children.Add(includeLighting); panel.Children.Add(includePower);
        var links = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var (caption, page) in new[] { (T("Кривые вентиляторов"), "fans"), (T("Питание и GPU"), "system"), (T("Редактор подсветки"), "lighting") })
        {
            var link = new Button { Content = caption, BorderThickness = new Thickness(0, 0, 0, 1), CornerRadius = new CornerRadius(0) };
            link.Click += (_, _) => App.Current.Services.GetRequiredService<NavigationService>().Navigate(page); links.Children.Add(link);
        }
        panel.Children.Add(links);
        panel.Children.Add(new TextBlock { Text = T("Дополнительные параметры меняются в соответствующих разделах. После этого нажмите «Взять текущие», затем сохраните режим. Поддержка BIOS, GPU и подсветки определяется устройством; яркость экрана в режим не входит."), FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var capture = new Button { Content = T("Взять текущие") };
        capture.Click += (_, _) =>
        {
            draft = session.Settings.Profile;
            basis.SelectedIndex = (int)(draft.LaptopMode ?? LaptopMode.Balanced); cooling.SelectedIndex = (int)draft.Mode;
            autoBoost.IsOn = draft.AutoBoost; coolBoost.IsOn = draft.CoolBoost == true;
            foreach (var row in rows) { row.Percent.Value = draft.ManualFor(row.Id).Percent; row.Curve.IsOn = draft.ManualFor(row.Id).UseCurve; }
            Refresh(); message.Text = T("Текущие параметры скопированы в редактор.");
        };
        var save = new Button { Content = T("Сохранить мой режим") }; save.Click += (_, _) => Save();
        var apply = new Button { Content = T("Включить") }; apply.Click += async (_, _) =>
        {
            if (!Save()) return;
            IsEnabled = false;
            try { await App.Current.Services.GetRequiredService<PersonalModeService>().ApplyAsync(); message.Text = T("Режим передан контроллеру. Подтверждённое состояние — на главной."); }
            catch (Exception ex) { message.Text = T(ex.Message); }
            finally { IsEnabled = true; }
        };
        actions.Children.Add(capture); actions.Children.Add(save); actions.Children.Add(apply); panel.Children.Add(actions); panel.Children.Add(message);
        basis.SelectionChanged += (_, _) => Refresh(); cooling.SelectionChanged += (_, _) => Refresh();
        Content = new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = panel };
        IsEnabled = !App.Current.Options.Preview && session.State == SessionState.Ready;
        Refresh();
    }

    private void Refresh()
    {
        var quiet = basis.SelectedIndex == (int)LaptopMode.Quiet;
        if (quiet) { cooling.SelectedIndex = 0; autoBoost.IsOn = false; }
        cooling.IsEnabled = !quiet;
        autoBoost.Visibility = !quiet && cooling.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        coolBoost.Visibility = !quiet && session.Capabilities.CoolBoost ? Visibility.Visible : Visibility.Collapsed;
        foreach (var row in rows) { row.Row.Visibility = !quiet && cooling.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed; row.Percent.IsEnabled = !row.Curve.IsOn; }
    }

    private bool Save()
    {
        try
        {
            var title = name.Text.Trim();
            if (title.Length == 0) throw new InvalidOperationException(T("Введите название режима."));
            var profile = PersonalMode.Prepare(draft with
            {
                Mode = (FanControlMode)Math.Clamp(cooling.SelectedIndex, 0, 2), AutoBoost = autoBoost.IsOn,
                CoolBoost = session.Capabilities.CoolBoost ? coolBoost.IsOn : null,
                Manual = rows.ToDictionary(r => r.Id, r => new ManualFanSetting((int)r.Percent.Value, r.Curve.IsOn))
            }, (LaptopMode)Math.Clamp(basis.SelectedIndex, 0, 2), session.Capabilities);
            var lights = session.Settings.Lighting;
            var lighting = App.Current.Services.GetRequiredService<AcerCrate.App.ViewModels.LightingViewModel>();
            if (includeLighting.IsOn && lighting.Device is {} device) lights = lights.With(device.Id, lighting.CapturePreset());
            var preset = new PersonalMode { Name = title, Profile = profile, Lighting = includeLighting.IsOn && includeLighting.Visibility == Visibility.Visible ? lights : null, Power = includePower.IsOn && includePower.Visibility == Visibility.Visible ? session.Settings.Power : null };
            settings.SaveNow(s => s with { PersonalMode = preset, Ui = s.Ui with { PersonalModeSelected = s.Ui.PersonalModeSelected && preset.Matches(session.Settings.Profile) } });
            message.Text = F("Режим «{0}» сохранён. Он доступен на главной.", title);
            return true;
        }
        catch (Exception ex) { message.Text = T(ex.Message); return false; }
    }
}
