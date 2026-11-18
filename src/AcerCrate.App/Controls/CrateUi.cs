using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
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
        var button = new Button { Content = text, MinHeight = 36, Padding = new Thickness(16, 8, 16, 8), CornerRadius = new CornerRadius(6) };
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
    public static StackPanel Body() => new()
    {
        Spacing = 22,
        Padding = (Thickness)Application.Current.Resources["PagePadding"],
        MaxWidth = (double)Application.Current.Resources["PageMaxWidth"]
    };
}

public sealed class PageHeading : Grid
{
    private readonly TextBlock title = new()
    {
        FontSize = 29, FontFamily = new FontFamily("Segoe UI Variable Display"),
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBlock description = new() { FontSize = 13, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
    private readonly FontIcon icon = new() { FontSize = 19 };
    private readonly Rectangle rule = new() { Height = 1 };
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeading), new PropertyMetadata("", (o, e) => ((PageHeading)o).title.Text = (string)e.NewValue));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(PageHeading),
        new PropertyMetadata("", (o, e) => ((PageHeading)o).SetDescription(e.NewValue as string)));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(PageHeading), new PropertyMetadata("\uE713", (o, e) => ((PageHeading)o).icon.Glyph = (string)e.NewValue));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => Title; set => Title = value; }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string? RussianDescription { get; set; }
    public bool ShowRule
    {
        get => rule.Visibility == Visibility.Visible;
        set
        {
            rule.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            RowSpacing = value ? 15 : 0;
        }
    }
    public PageHeading()
    {
        var resources = Application.Current.Resources;
        ColumnSpacing = 13;
        RowSpacing = 15;
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition());
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        icon.Glyph = "\uE713";
        icon.Foreground = (Brush)resources["CrateAccent"];
        Children.Add(new Border
        {
            Width = 42, Height = 42, CornerRadius = new CornerRadius(7),
            Background = (Brush)resources["CrateCard"], BorderBrush = (Brush)resources["CrateCardStroke"],
            BorderThickness = new Thickness(1), Child = icon, VerticalAlignment = VerticalAlignment.Center
        });
        description.Foreground = (Brush)resources["TextFillColorSecondaryBrush"];
        var labels = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(title);
        labels.Children.Add(description);
        SetColumn(labels, 1);
        Children.Add(labels);

        rule.Fill = (Brush)resources["CrateRule"];
        SetRow(rule, 1);
        SetColumnSpan(rule, 2);
        Children.Add(rule);
        Loaded += (_, _) => { if (AppLanguage.Current == "ru-RU" && RussianDescription is { } ru) Description = ru; };
    }

    private void SetDescription(string? value)
    {
        description.Text = value ?? "";
        description.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    }
}
