using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using AcerCrate.Core.Settings;
using Windows.UI;

namespace AcerCrate.App.Services;

public static class CrateAppearance
{
    private static readonly Windows.UI.ViewManagement.UISettings systemUi = new();
    private static readonly Color DefaultAccent = Color.FromArgb(255, 18, 225, 210);
    private static volatile Palette? palette;
    private sealed record Palette(UiSettings Settings, Color Accent, Color LightText, Color DarkText);

    private static Palette CurrentPalette()
    {
        var ui = App.Current.Services.GetRequiredService<SettingsService>().Current.Ui;
        var cached = palette;
        if (cached is not null && ReferenceEquals(cached.Settings, ui)) return cached;
        // Canvas controls ask for colours many times per frame. Parse only when settings change.
        var updated = new Palette(ui, Parse(ui.AccentHex, DefaultAccent),
            Parse(ui.TextHex, Color.FromArgb(255, 27, 27, 27)),
            Parse(ui.TextHex, Color.FromArgb(255, 255, 255, 255)));
        palette = updated;
        return updated;
    }
    public static Color AccentColor
    {
        get
        {
            return App.Current.IsExiting ? DefaultAccent : CurrentPalette().Accent;
        }
    }

    public static Color Parse(string? value, Color fallback)
    {
        if (!UiColors.TryNormalize(value, out var hex)) return fallback;
        var bytes = Convert.FromHexString(hex[1..]);
        return Color.FromArgb(255, bytes[0], bytes[1], bytes[2]);
    }

    public static Color TextColor(bool light, byte alpha = 255)
    {
        var colors = App.Current.IsExiting ? null : CurrentPalette();
        var color = light ? colors?.LightText ?? Color.FromArgb(255, 27, 27, 27)
            : colors?.DarkText ?? Color.FromArgb(255, 255, 255, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>Mutate the brushes already used by XAML so loaded pages update immediately.</summary>
    public static void Apply(UiSettings settings)
    {
        var theme = Application.Current.Resources.MergedDictionaries.Last();
        foreach (var name in new[] { "Default", "Light" })
        {
            var dictionary = (ResourceDictionary)theme.ThemeDictionaries[name];
            var light = name == "Light";
            var text = Parse(settings.TextHex, light ? Color.FromArgb(255, 27, 27, 27) : Color.FromArgb(255, 255, 255, 255));
            var accent = Parse(settings.AccentHex, Color.FromArgb(255, 18, 225, 210));
            var sidebar = Parse(settings.SidebarHex, light ? Color.FromArgb(255, 233, 236, 235) : Color.FromArgb(255, 12, 15, 15));
            foreach (var key in AccentBrushes) Set(dictionary, key, accent);
            foreach (var key in PrimaryTextBrushes) Set(dictionary, key, text);
            Set(dictionary, "TextFillColorSecondaryBrush", WithAlpha(text, 200));
            Set(dictionary, "TextFillColorTertiaryBrush", WithAlpha(text, 160));
            Set(dictionary, "TextFillColorDisabledBrush", WithAlpha(text, 96));
            foreach (var key in SidebarBrushes) Set(dictionary, key, sidebar);
        }
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static void Set(ResourceDictionary resources, string key, Color color)
    {
        if (resources.TryGetValue(key, out var value) && value is SolidColorBrush brush) brush.Color = color;
    }

    private static readonly string[] AccentBrushes =
    [
        "CrateAccent", "NavigationViewSelectionIndicatorForeground", "AccentFillColorDefaultBrush",
        "AccentFillColorSecondaryBrush", "AccentFillColorTertiaryBrush", "AccentTextFillColorPrimaryBrush",
        "AccentTextFillColorSecondaryBrush", "AccentTextFillColorTertiaryBrush", "SliderThumbBackground",
        "SliderThumbBackgroundPointerOver", "SliderThumbBackgroundPressed", "SliderTrackValueFill",
        "SliderTrackValueFillPointerOver", "SliderTrackValueFillPressed", "ProgressBarForeground",
        "ToggleSwitchFillOn", "ToggleSwitchFillOnPointerOver", "ToggleSwitchFillOnPressed"
    ];

    private static readonly string[] PrimaryTextBrushes =
    [
        "TextFillColorPrimaryBrush", "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused",
        "NavigationViewItemForeground", "NavigationViewItemForegroundPointerOver", "NavigationViewItemForegroundPressed",
        "NavigationViewItemForegroundSelected", "NavigationViewItemForegroundSelectedPointerOver", "NavigationViewItemForegroundSelectedPressed"
    ];

    private static readonly string[] SidebarBrushes =
    [
        "CrateRail", "NavigationViewDefaultPaneBackground", "NavigationViewExpandedPaneBackground", "NavigationViewTopPaneBackground"
    ];

    public static bool AnimationsEnabled =>
        !App.Current.IsExiting &&
        App.Current.Services.GetRequiredService<SettingsService>().Current.Ui.AnimateInterface &&
        systemUi.AnimationsEnabled;
}
