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
    private readonly TextBlock label = new() { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border line;

    public CrateNavItem()
    {
        Height = 49; Margin = new Thickness(0, 2, 0, 2); CornerRadius = new CornerRadius(2);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        line = new Border { Height = 47, MinWidth = 145, Child = label };
        Content = line;
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

    public void RefreshAppearance()
    {
        label.Text = Title;
        label.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        AutomationProperties.SetName(this, Title);
        label.FontWeight = IsSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }
}
