using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSense.App.Services;
using OpenSense.App.ViewModels;
using OpenSense.Core.Control;
using OpenSense.Core.Lighting;

namespace OpenSense.App.Controls;

public sealed partial class PresetEditor : UserControl
{
    public bool ForLighting { get; set; }
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly LightingViewModel lighting = App.Current.Services.GetRequiredService<LightingViewModel>();
    private readonly FanControlViewModel fans = App.Current.Services.GetRequiredService<FanControlViewModel>();
    private readonly ComboBox list = new() { PlaceholderText = T("Выберите пресет"), MinWidth = 165 };
    private readonly TextBox name = new() { PlaceholderText = T("Название пресета"), MaxLength = 48 };
    private readonly TextBlock message = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Button apply = new() { Content = T("Применить") };
    private readonly Button remove = new() { Content = T("Удалить") };

    public PresetEditor()
    {
        var editor = new StackPanel { Spacing = 10 };
        editor.Children.Add(new TextBlock { Text = T("Мои пресеты"), FontSize = 19, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var selectRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        selectRow.Children.Add(list);
        selectRow.Children.Add(apply);
        selectRow.Children.Add(remove);
        editor.Children.Add(selectRow);
        var saveRow = new Grid { ColumnSpacing = 8 };
        saveRow.ColumnDefinitions.Add(new ColumnDefinition());
        saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        saveRow.Children.Add(name);
        var save = new Button { Content = T("Сохранить текущие") };
        Grid.SetColumn(save, 1);
        saveRow.Children.Add(save);
        editor.Children.Add(saveRow);
        editor.Children.Add(message);
        Content = editor;
        save.Click += Save;
        apply.Click += Apply;
        remove.Click += Remove;
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is string selected) name.Text = selected;
            apply.IsEnabled = list.SelectedItem is string && (ForLighting || fans.ManualAllowed);
            remove.IsEnabled = list.SelectedItem is string;
        };
        Loaded += (_, _) =>
        {
            IsEnabled = !App.Current.Options.Preview && (ForLighting ? lighting.Available : fans.HasFans);
            Refresh(null);
            fans.PropertyChanged += OnFanAvailabilityChanged;
        };
        Unloaded += (_, _) => fans.PropertyChanged -= OnFanAvailabilityChanged;
    }

    private void OnFanAvailabilityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!ForLighting && e.PropertyName == nameof(FanControlViewModel.ManualAllowed))
            apply.IsEnabled = list.SelectedItem is string && fans.ManualAllowed;
    }

    private void Refresh(string? selected)
    {
        IEnumerable<string> names = ForLighting ? settings.Current.LightingPresets.Keys : settings.Current.CoolingPresets.Keys;
        list.ItemsSource = names.Order(StringComparer.CurrentCultureIgnoreCase).ToList();
        list.SelectedItem = selected;
        apply.IsEnabled = list.SelectedItem is string && (ForLighting || fans.ManualAllowed);
        remove.IsEnabled = list.SelectedItem is string;
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        var key = name.Text.Trim();
        if (key.Length == 0) { message.Text = T("Введите название пресета."); return; }
        try
        {
            if (ForLighting)
            {
                var value = lighting.CapturePreset();
                settings.SaveNow(s => s with { LightingPresets = Put(s.LightingPresets, key, value) });
            }
            else
            {
                var value = fans.CapturePreset();
                settings.SaveNow(s => s with { CoolingPresets = Put(s.CoolingPresets, key, value) });
            }
            Refresh(key);
            message.Text = F("Сохранён пресет «{0}».", key);
        }
        catch (Exception ex) { message.Text = T(ex.Message); }
    }

    private async void Apply(object sender, RoutedEventArgs e)
    {
        if (list.SelectedItem is not string key) return;
        IsEnabled = false;
        try
        {
            if (ForLighting) await lighting.RestorePresetAsync(settings.Current.LightingPresets[key]);
            else await fans.RestorePresetAsync(settings.Current.CoolingPresets[key]);
            message.Text = F("Настройки «{0}» переданы контроллеру.", key);
        }
        catch (Exception ex) { message.Text = T(ex.Message); }
        finally { IsEnabled = true; }
    }

    private void Remove(object sender, RoutedEventArgs e)
    {
        if (list.SelectedItem is not string key) return;
        try
        {
            if (ForLighting) settings.SaveNow(s => s with { LightingPresets = Without(s.LightingPresets, key) });
            else settings.SaveNow(s => s with { CoolingPresets = Without(s.CoolingPresets, key) });
            name.Text = "";
            Refresh(null);
            message.Text = F("Удалён пресет «{0}».", key);
        }
        catch (Exception ex) { message.Text = T(ex.Message); }
    }

    private static Dictionary<string,T> Put<T>(Dictionary<string,T> original, string name, T value) =>
        new(original, StringComparer.OrdinalIgnoreCase) { [name] = value };

    private static Dictionary<string,T> Without<T>(Dictionary<string,T> original, string name)
    {
        var result = new Dictionary<string,T>(original, StringComparer.OrdinalIgnoreCase);
        result.Remove(name);
        return result;
    }
}
