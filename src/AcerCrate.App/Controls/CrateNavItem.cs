using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AcerCrate.App.Services;
using AcerCrate.App.Localization;

namespace AcerCrate.App.Controls;

public sealed class CrateNavItem : NavigationViewItem
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(CrateNavItem), new PropertyMetadata("", (d, _) => ((CrateNavItem)d).RefreshAppearance()));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    private readonly TextBlock label = new() { FontSize = 13, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private bool compact;

    public CrateNavItem()
    {
        Height = 43; Margin = new Thickness(0, 1, 0, 1); CornerRadius = new CornerRadius(3);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Content = new Border { Height = 43, Child = label };
        Loaded += (_, _) =>
        {
            Title = (Tag as string) switch
            {
                "dashboard" => Strings.Get("Nav_Dashboard/Content"),
                "fans" => Strings.Get("Nav_Fans/Content"),
                "lighting" => Strings.Get("Nav_Lighting/Content"),
                "system" => Strings.Get("Nav_System/Content"),
                _ => Title
            };
            RefreshAppearance();
        };
        RegisterPropertyChangedCallback(IsSelectedProperty, (_, _) => RefreshAppearance());
    }

    public void SetCompact(bool value)
    {
        compact = value;
        RefreshAppearance();
    }

    public void RefreshAppearance()
    {
        label.Text = Title;
        label.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        ToolTipService.SetToolTip(this, compact ? Title : null);
        label.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        AutomationProperties.SetName(this, Title);
        label.FontWeight = IsSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }
}
