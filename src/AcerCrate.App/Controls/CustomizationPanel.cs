using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using AcerCrate.App.Helpers;
using AcerCrate.App.Services;
using AcerCrate.App.ViewModels;
using AcerCrate.Core.Settings;
using Windows.UI;

namespace AcerCrate.App.Controls;

public sealed partial class CustomizationPanel : UserControl
{
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly TextBlock message = new() { FontSize = 12, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap };
    private readonly TextBox model = new() { Header = T("Поиск изображения по модели"), PlaceholderText = "AN515-58", MaxLength = 80 };

    public CustomizationPanel()
    {
        var panel = new StackPanel { Spacing = 14 };
        panel.Children.Add(new TextBlock { Text = T("Оформление Acer Crate"), FontSize = 22 });
        model.Text = settings.Current.Ui.ArtworkModelQuery ?? session.DeviceName ?? "";
        panel.Children.Add(model);
        var catalog = new ComboBox { Header = CrateUi.L("Offline model photos", "Встроенные фото моделей"), HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = ModelArtwork.Entries.Select(entry => entry.Title + " · " + entry.Models.FirstOrDefault()).ToArray(), PlaceholderText = CrateUi.L("Choose a model / series", "Выбрать модель / серию") };
        catalog.SelectionChanged += (_, _) =>
        {
            if (catalog.SelectedIndex < 0) return;
            var entry = ModelArtwork.Entries[catalog.SelectedIndex];
            model.Text = entry.Models.FirstOrDefault() ?? "";
            Update(ui => ui with { CustomLaptopImage = null, ArtworkModelQuery = model.Text });
            message.Text = CrateUi.L("Photo selected. Use Automatic to return to your detected model.", "Фото выбрано. Кнопка «Автоматически» вернёт фото обнаруженной модели.");
        };
        panel.Children.Add(catalog);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var search = new Button { Content = T("Найти модель") };
        search.Click += async (_, _) => await FindModelAsync();
        var custom = new Button { Content = T("Своё изображение…") };
        custom.Click += async (_, _) => await PickImageAsync();
        var reset = new Button { Content = T("Автоматически") };
        reset.Click += (_, _) =>
        {
            Update(ui => ui with { CustomLaptopImage = null, ArtworkModelQuery = null });
            model.Text = session.DeviceName ?? "";
            message.Text = T("Изображение выбрано по обнаруженной модели.");
        };
        actions.Children.Add(search);
        actions.Children.Add(custom);
        actions.Children.Add(reset);
        panel.Children.Add(actions);
        var photos = new ToggleSwitch { Header = CrateUi.L("Download matching model photo from Acer", "Загружать фото модели с сайта Acer"), IsOn = settings.Current.Ui.DownloadModelArtwork };
        photos.Toggled += (_, _) => Update(ui => ui with { DownloadModelArtwork = photos.IsOn });
        panel.Children.Add(photos);
        panel.Children.Add(new TextBlock { Text = CrateUi.L("Only the model code is used. Bundled or cached images work offline.", "Используется только код модели. Встроенные и сохранённые фото работают без интернета."), FontSize = 12, Opacity = .6, TextWrapping = TextWrapping.Wrap });

        panel.Children.Add(ColorEditor(T("Цвет акцента"), settings.Current.Ui.AccentHex, "#12E1D2",
            hex => Update(ui => ui with { AccentHex = hex }), () => Update(ui => ui with { AccentHex = "#12E1D2" })));
        panel.Children.Add(ColorEditor(T("Цвет текста"), settings.Current.Ui.TextHex, settings.Current.Ui.Theme == 1 ? "#1B1B1B" : "#FFFFFF",
            hex => Update(ui => ui with { TextHex = hex }), () => Update(ui => ui with { TextHex = null })));
        panel.Children.Add(ColorEditor(T("Фон боковой панели"), settings.Current.Ui.SidebarHex, settings.Current.Ui.Theme == 1 ? "#E9ECEB" : "#0C0F0F",
            hex => Update(ui => ui with { SidebarHex = hex }), () => Update(ui => ui with { SidebarHex = null })));
        var animation = new ToggleSwitch { Header = T("Анимации интерфейса"), IsOn = settings.Current.Ui.AnimateInterface };
        animation.Toggled += (_, _) => Update(ui => ui with { AnimateInterface = animation.IsOn });
        panel.Children.Add(animation);
        var modeOsd = new ToggleSwitch { Header = T("Сообщение о режиме при скрытом окне"), IsOn = settings.Current.Ui.ModeOsdWhenHidden };
        modeOsd.Toggled += (_, _) => Update(ui => ui with { ModeOsdWhenHidden = modeOsd.IsOn });
        panel.Children.Add(modeOsd);
        var modeCycle = new ToggleSwitch { Header = T("Ctrl+Alt+F: переключать режимы по кругу"), IsOn = settings.Current.Ui.ModeCycleHotkey };
        modeCycle.Toggled += (_, _) => Update(ui => ui with { ModeCycleHotkey = modeCycle.IsOn });
        panel.Children.Add(modeCycle);
        var refresh = new ComboBox
        {
            Header = T("Обновление главного экрана"), ItemsSource = new[] { T("Каждую секунду"), T("Каждые 2 секунды"), T("Каждые 3 секунды") },
            SelectedIndex = Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3) - 1,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
        };
        refresh.SelectionChanged += (_, _) =>
        {
            if (refresh.SelectedIndex >= 0) Update(ui => ui with { UiRefreshSeconds = refresh.SelectedIndex + 1 });
        };
        panel.Children.Add(refresh);
        panel.Children.Add(message);
        Content = new Border { Style = (Microsoft.UI.Xaml.Style)Microsoft.UI.Xaml.Application.Current.Resources["CardStyle"], Child = panel };
    }

    private async Task FindModelAsync()
    {
        var ui = settings.Current.Ui with { ArtworkModelQuery = model.Text.Trim(), CustomLaptopImage = null };
        message.Text = CrateUi.L("Looking for model photo…", "Поиск фото модели…");
        if (await ModelArtwork.ResolveAsync(ui, model.Text) is not {} path || !File.Exists(path))
        {
            message.Text = T("Для этой модели изображения в каталоге пока нет. Выберите своё изображение.");
            return;
        }
        Update(ui => ui with { ArtworkModelQuery = model.Text.Trim(), CustomLaptopImage = null });
        message.Text = CrateUi.L("Model photo is ready on Overview.", "Фото модели доступно на главной.");
    }

    private static Expander ColorEditor(string title, string? value, string fallback, Action<string> save, Action reset)
    {
        var initial = CrateAppearance.Parse(value, CrateAppearance.Parse(fallback, Color.FromArgb(255, 255, 255, 255)));
        var swatch = new SolidColorBrush(initial);
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        heading.Children.Add(new Border { Width = 22, Height = 22, Background = swatch, BorderBrush = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3) });
        heading.Children.Add(new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center });
        var picker = new ColorPicker { Color = initial, IsAlphaEnabled = false, IsColorSliderVisible = true, IsColorChannelTextInputVisible = true, IsHexInputVisible = false, IsMoreButtonVisible = false, MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Left };
        var hex = new TextBox { Header = "HEX", Text = $"#{initial.R:X2}{initial.G:X2}{initial.B:X2}", PlaceholderText = "#RRGGBB", MaxLength = 7, MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Stretch };
        var error = new TextBlock { Text = T("Введите цвет в формате #RRGGBB."), Visibility = Visibility.Collapsed, TextWrapping = TextWrapping.Wrap };
        var syncing = false;
        void ApplyHex(bool normalize)
        {
            if (syncing) return;
            if (!UiColors.TryNormalize(hex.Text, out var normalized)) { error.Visibility = Visibility.Visible; return; }
            syncing = true;
            picker.Color = CrateAppearance.Parse(normalized, initial);
            swatch.Color = picker.Color;
            if (normalize) hex.Text = normalized;
            syncing = false;
            error.Visibility = Visibility.Collapsed;
            save(normalized);
        }
        picker.ColorChanged += (_, args) =>
        {
            if (syncing) return;
            syncing = true;
            hex.Text = $"#{args.NewColor.R:X2}{args.NewColor.G:X2}{args.NewColor.B:X2}";
            swatch.Color = args.NewColor;
            syncing = false;
            error.Visibility = Visibility.Collapsed;
            save(hex.Text);
        };
        hex.TextChanged += (_, _) => { if (hex.Text.Trim().Length == 7) ApplyHex(false); };
        hex.LostFocus += (_, _) => ApplyHex(true);
        hex.KeyDown += (_, args) => { if (args.Key == Windows.System.VirtualKey.Enter) { ApplyHex(true); args.Handled = true; } };
        var restore = new Button { Content = T("Сбросить цвет") };
        restore.Click += (_, _) =>
        {
            syncing = true;
            picker.Color = CrateAppearance.Parse(fallback, initial);
            swatch.Color = picker.Color;
            hex.Text = fallback;
            syncing = false;
            error.Visibility = Visibility.Collapsed;
            reset();
        };
        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(picker); body.Children.Add(hex); body.Children.Add(error); body.Children.Add(restore);
        return new Expander { Header = heading, Content = body, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
    }

    private async Task PickImageAsync()
    {
        var window = App.Current.Services.GetRequiredService<MainWindow>();
        var picker = new FileOpenPicker(window.AppWindow.Id) { ViewMode = PickerViewMode.Thumbnail, SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp" }) picker.FileTypeFilter.Add(extension);
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        try
        {
            var info = new FileInfo(file.Path);
            if (info.Length > 12 * 1024 * 1024) throw new IOException(T("Выберите изображение размером до 12 МБ."));
            var bytes = await File.ReadAllBytesAsync(file.Path);
            if (await Pictures.FromBytesAsync(bytes, 520) is null) throw new IOException(T("Этот файл не удалось прочитать как изображение."));
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AcerCrate", "Artwork");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, Guid.NewGuid().ToString("N") + info.Extension);
            await File.WriteAllBytesAsync(path, bytes);
            settings.SaveNow(s => s with { Ui = s.Ui with { CustomLaptopImage = path } });
            message.Text = T("Своё изображение сохранено.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            message.Text = T(ex.Message);
        }
    }

    private void Update(Func<UiSettings, UiSettings> change) => settings.Update(s => s with { Ui = change(s.Ui) });
}
