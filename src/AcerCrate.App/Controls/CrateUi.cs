using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AcerCrate.App.Localization;

namespace AcerCrate.App.Controls;

internal static class CrateUi
{
    public static string L(string en, string ru) => AppLanguage.Current == "ru-RU" ? ru : en;
    public static TextBlock Text(string value, double size = 14, double opacity = 1) => new()
    {
        Text = value, FontSize = size, Opacity = opacity, TextWrapping = TextWrapping.Wrap
    };
    public static Button Button(string text, RoutedEventHandler click, bool accent = false)
    {
        var button = new Button { Content = text, MinHeight = 36, Padding = new Thickness(16, 8, 16, 8), CornerRadius = new CornerRadius(8) };
        if (accent)
        {
            var brush = (Brush)Application.Current.Resources["CrateAccent"];
            button.Background = brush;
            button.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
            foreach (var key in new[] { "ButtonBackgroundPointerOver", "ButtonBackgroundPressed" }) button.Resources[key] = brush;
        }
        button.Click += click; return button;
    }
    public static Border Card(string title, string? description, UIElement content)
    {
        var panel = new StackPanel { Spacing = 14 };
        var heading = Text(title, 19); heading.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        panel.Children.Add(heading);
        if (!string.IsNullOrEmpty(description)) panel.Children.Add(Text(description, 13, .65));
        panel.Children.Add(content);
        return new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = panel };
    }
    public static StackPanel Body() => new() { Spacing = 22, Padding = new Thickness(30, 24, 30, 32), HorizontalAlignment = HorizontalAlignment.Stretch };
}

public sealed class PageHeading : Grid
{
    private readonly TextBlock title = new() { FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock description = new() { FontSize = 13, Opacity = .65, TextWrapping = TextWrapping.Wrap };
    private readonly FontIcon icon = new() { FontSize = 24 };
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeading), new PropertyMetadata("", (o, e) => ((PageHeading)o).title.Text = (string)e.NewValue));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(PageHeading), new PropertyMetadata("", (o, e) => ((PageHeading)o).description.Text = (string)e.NewValue));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(PageHeading), new PropertyMetadata("\uE713", (o, e) => ((PageHeading)o).icon.Glyph = (string)e.NewValue));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => Title; set => Title = value; }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string? RussianDescription { get; set; }
    public PageHeading()
    {
        ColumnSpacing = 16; Margin = new Thickness(0, 0, 0, 4);
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); ColumnDefinitions.Add(new ColumnDefinition());
        icon.Glyph = "\uE713"; icon.Foreground = (Brush)Application.Current.Resources["CrateAccent"];
        Children.Add(new Border { Width = 54, Height = 54, CornerRadius = new CornerRadius(14), Background = (Brush)Application.Current.Resources["CrateCard"], Child = icon, VerticalAlignment = VerticalAlignment.Top });
        var labels = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(title); labels.Children.Add(description); SetColumn(labels, 1); Children.Add(labels);
        Loaded += (_, _) => { if (AppLanguage.Current == "ru-RU" && RussianDescription is { } ru) Description = ru; };
    }
}
