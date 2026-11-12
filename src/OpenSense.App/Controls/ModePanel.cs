using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using OpenSense.App.Helpers;
using OpenSense.App.Services;
using OpenSense.App.ViewModels;
using OpenSense.Core.Control;
using OpenSense.Core.Hardware;
using Windows.UI;

namespace OpenSense.App.Controls;

public sealed class ModePanel : StackPanel
{
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly PersonalModeService personal = App.Current.Services.GetRequiredService<PersonalModeService>();
    private readonly TextBlock status = Label();
    private readonly TextBlock selected = Label();
    private readonly TextBlock actual = Label();
    private readonly TextBlock windows = Label();
    private readonly List<Button> buttons = [];
    private readonly List<Border> selectionLayers = [];
    private TextBlock? personalLabel;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;
    private readonly Dictionary<Button, bool> highlights = [];
    private bool applying;
    private string? error;
    private Color lastAccent;

    public ModePanel()
    {
        MaxWidth = 660;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Spacing = 7;
        Children.Add(new TextBlock { Text = T("Режим работы"), FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var selector = new Grid { MinWidth = 460, MaxWidth = 660 };
        string[] titles = [T("Тихий"), T("Баланс"), T("Производительность"), ModeTitle(settings.Current.PersonalMode.Name)];
        string[] glyphs = ["\uE708", "\uE945", "\uE7C1", "\uE713"];
        for (var i = 0; i < titles.Length; i++)
        {
            selector.ColumnDefinitions.Add(new ColumnDefinition());
            var content = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 7, 12, 7) };
            content.Children.Add(new FontIcon { Glyph = glyphs[i], FontSize = 20 });
            var label = new TextBlock { Text = titles[i], FontSize = 12, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis };
            content.Children.Add(label);
            if (i == 3) personalLabel = label;
            var highlight = new Border { IsHitTestVisible = false, BorderThickness = new Thickness(0, 0, 0, 2) };
            ElementCompositionPreview.GetElementVisual(highlight).Opacity = 0;
            selectionLayers.Add(highlight);
            var layers = new Grid();
            layers.Children.Add(highlight);
            layers.Children.Add(content);
            var button = new Button
            {
                Content = layers, Tag = i, MinHeight = 64, Padding = new Thickness(0),
                Style = (Style)Application.Current.Resources["ModeButtonStyle"],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 128, 128, 128)),
                CornerRadius = new CornerRadius(2), BorderThickness = new Thickness(0, 0, 0, 2)
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, titles[i]);
            button.Click += Choose;
            Grid.SetColumn(button, i);
            selector.Children.Add(button);
            buttons.Add(button);
        }
        Children.Add(selector);
        Children.Add(status);
        var details = new StackPanel { Spacing = 5 };
        details.Children.Add(selected);
        details.Children.Add(actual);
        details.Children.Add(windows);
        Children.Add(new Expander
        {
            Header = T("Состояние устройства"), Content = details, IsExpanded = false,
            HorizontalAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(0)
        });
        timer = DispatcherQueue.CreateTimer();
        timer.Tick += (_, _) => Refresh();
        UiActivity.Track(this, timer, Refresh);
        Loaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged += StopHiddenAnimations;
            settings.Changed += OnSettingsChanged;
        };
        Unloaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged -= StopHiddenAnimations;
            settings.Changed -= OnSettingsChanged;
            StopSelectionAnimations();
        };
    }

    private void OnSettingsChanged(OpenSense.Core.Settings.UserSettings _) => DispatcherQueue.TryEnqueue(() =>
    {
        if (IsLoaded && App.Current.IsWindowVisible) Refresh();
    });

    private void StopHiddenAnimations()
    {
        if (!App.Current.IsWindowVisible) StopSelectionAnimations();
    }

    private void StopSelectionAnimations()
    {
        for (var i = 0; i < buttons.Count; i++)
        {
            var visual = ElementCompositionPreview.GetElementVisual(selectionLayers[i]);
            visual.StopAnimation("Opacity");
            visual.Opacity = highlights.GetValueOrDefault(buttons[i]) ? 1 : 0;
        }
    }

    private static TextBlock Label() => new() { FontSize = 12, Opacity = .75, TextWrapping = TextWrapping.Wrap };

    private async void Choose(object sender, RoutedEventArgs e)
    {
        if (App.Current.Options.Preview || applying) return;
        var index = (int)((Button)sender).Tag;
        if (index == 3 && settings.Current.PersonalMode.Profile is null)
        {
            App.Current.Services.GetRequiredService<NavigationService>().Navigate("presets"); return;
        }
        applying = true;
        error = null;
        Refresh();
        try
        {
            if (index == 3) await personal.ApplyAsync();
            else await personal.SelectNativeAsync((LaptopMode)index);
        }
        catch (Exception ex) { error = T(ex.Message); }
        finally { applying = false; Refresh(); }
    }

    private void Refresh()
    {
        timer.Interval = TimeSpan.FromSeconds(Math.Clamp(settings.Current.Ui.UiRefreshSeconds, 1, 3));
        var profile = session.Settings.Profile;
        var telemetry = session.Latest;
        string[] names = [T("Тихий"), T("Сбалансированный"), T("Производительность")];
        var personalSelected = personal.IsSelected;
        if (personalLabel is not null) personalLabel.Text = ModeTitle(settings.Current.PersonalMode.Name);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(buttons[3], ModeTitle(settings.Current.PersonalMode.Name));
        selected.Text = personalSelected ? T("Выбран «") + ModeTitle(settings.Current.PersonalMode.Name) + T("» · основа ") + PersonalModeService.ModeName(profile.LaptopMode)
            : T("Выбран ") + (profile.LaptopMode is {} mode ? names[(int)mode] : T("исходный профиль"));
        actual.Text = T("Прошивка · ") + (telemetry?.OperatingMode is {} native ? OpenSense.App.Localization.Names.OperatingMode(native) : T("ожидание данных"));
        var activeScheme = telemetry?.ActivePowerScheme;
        var scheme = Enumerable.Range(0, 3).FirstOrDefault(i => ModePowerSchemes.Id((LaptopMode)i) == activeScheme, -1);
        windows.Text = "Windows · " + (scheme >= 0 ? "Acer Crate — " + names[scheme] : T("другая схема"));
        var fresh = telemetry is not null && DateTime.UtcNow - telemetry.Timestamp.ToUniversalTime() < TimeSpan.FromSeconds(7);
        status.Text = error ?? (App.Current.Options.Preview ? T("Просмотр интерфейса") :
            applying ? T("Применение…") :
            !fresh ? T("Ожидание связи с устройством") :
            telemetry!.PowerSchemeError is {} schemeError ? T(schemeError) :
            telemetry.Failsafe ? T("Охлаждением управляет прошивка") :
            !telemetry.ControlConfirmed || telemetry.AppliedProfile is not {} applied || !OpenSense.Core.Settings.PersonalMode.Equivalent(applied, profile) ? T("Параметры пока не подтверждены") :
            profile.OperatingMode is null ? T("Программный профиль") :
            telemetry.OperatingMode != profile.OperatingMode ? T("Режим ограничен текущим питанием") :
            profile.LaptopMode == LaptopMode.Quiet ? T("Тихий · автоматическое охлаждение") : T("Параметры применены"));

        var accent = CrateAppearance.AccentColor;
        if (accent != lastAccent) { highlights.Clear(); lastAccent = accent; }
        for (var i = 0; i < buttons.Count; i++)
        {
            var active = i == 3 ? personalSelected : !personalSelected && profile.LaptopMode == (LaptopMode)i;
            if (!highlights.TryGetValue(buttons[i], out var previous) || previous != active)
            {
                var initialized = highlights.ContainsKey(buttons[i]);
                highlights[buttons[i]] = active;
                var layer = selectionLayers[i];
                layer.BorderBrush = new SolidColorBrush(accent);
                layer.Background = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1),
                    GradientStops =
                    {
                        new GradientStop { Offset = 0, Color = Microsoft.UI.Colors.Transparent },
                        new GradientStop { Offset = 1, Color = Color.FromArgb(42, accent.R, accent.G, accent.B) }
                    }
                };
                var visual = ElementCompositionPreview.GetElementVisual(layer);
                if (initialized && IsLoaded && App.Current.IsWindowVisible && CrateAppearance.AnimationsEnabled)
                {
                    var fade = visual.Compositor.CreateScalarKeyFrameAnimation();
                    fade.InsertExpressionKeyFrame(0, "this.StartingValue");
                    fade.InsertKeyFrame(1, active ? 1 : 0, visual.Compositor.CreateCubicBezierEasingFunction(new System.Numerics.Vector2(.2f, 0), new System.Numerics.Vector2(.2f, 1)));
                    fade.Duration = TimeSpan.FromMilliseconds(240);
                    visual.StartAnimation("Opacity", fade);
                }
                else { visual.StopAnimation("Opacity"); visual.Opacity = active ? 1 : 0; }
            }
            buttons[i].IsEnabled = !applying && !personal.IsApplying && !App.Current.Options.Preview && session.State == SessionState.Ready && fresh;
        }
    }
}
