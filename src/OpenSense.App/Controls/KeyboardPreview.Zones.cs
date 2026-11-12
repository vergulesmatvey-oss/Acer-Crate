using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpenSense.App.ViewModels;
using OpenSense.Core.Lighting;
using Windows.Foundation;
using Windows.UI;

namespace OpenSense.App.Controls;

public sealed partial class KeyboardPreview
{
    public static readonly DependencyProperty InteractiveProperty = DependencyProperty.Register(
        nameof(Interactive), typeof(bool), typeof(KeyboardPreview), new PropertyMetadata(false, (d, _) => ((KeyboardPreview)d).UpdateZoneEditor()));

    public bool Interactive { get => (bool)GetValue(InteractiveProperty); set => SetValue(InteractiveProperty, value); }
    private readonly Microsoft.UI.Xaml.Controls.Canvas zoneLayer = new();
    private readonly List<Button> zoneButtons = [];

    private void InitializeZoneEditor()
    {
        Root.Children.Add(zoneLayer);
        SizeChanged += (_, _) => UpdateZoneEditor();
    }

    private void UpdateZoneEditor()
    {
        var vm = Source;
        zoneLayer.Visibility = Interactive && vm is { IsKeyboard: true, Zones.Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        if (zoneLayer.Visibility != Visibility.Visible || vm is null) return;
        var count = Math.Clamp(vm.Zones.Count, 1, KeyboardAnimation.ZoneCount);
        if (zoneButtons.Count != count || zoneButtons.Where((button, i) => !ReferenceEquals(button.Tag, vm.Zones[i])).Any())
        {
            zoneLayer.Children.Clear();
            zoneButtons.Clear();
            foreach (var zone in vm.Zones.Take(count))
            {
                var button = CreateZoneButton(vm, zone);
                zoneButtons.Add(button);
                zoneLayer.Children.Add(button);
            }
        }
        var keys = KeyboardLayouts.Get(vm.Layout).Keys.Where(k => count == 4 || k.Bounds.Left < PadLeft).ToList();
        if (keys.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;
        var bounds = keys.Select(k => k.Bounds).Aggregate((a, b) => { a.Union(b); return a; });
        var unit = Math.Min(ActualWidth / bounds.Width, ActualHeight / bounds.Height);
        var originX = (ActualWidth - bounds.Width * unit) / 2 - bounds.Left * unit;
        var originY = (ActualHeight - bounds.Height * unit) / 2 - bounds.Top * unit;
        var edges = count == 4 ? FourZoneEdges : Enumerable.Range(1, count - 1).Select(i => i * MainWidth / count).ToArray();
        for (var i = 0; i < count; i++)
        {
            var left = i == 0 ? bounds.Left : edges[i - 1];
            var right = i == count - 1 ? bounds.Right : edges[i];
            var button = zoneButtons[i];
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(button, originX + left * unit);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(button, originY + bounds.Top * unit);
            button.Width = Math.Max(1, (right - left) * unit);
            button.Height = bounds.Height * unit;
            button.IsEnabled = vm.Available && !App.Current.Options.Preview;
        }
    }

    private static Button CreateZoneButton(LightingViewModel vm, ZoneViewModel zone)
    {
        var button = new Button { Tag = zone, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(0) };
        button.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
        button.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
        AutomationProperties.SetName(button, zone.Name + T(": цвет и включение"));
        ToolTipService.SetToolTip(button, zone.Name + T(" · нажмите для настройки"));
        var panel = new StackPanel { Spacing = 10, Width = 280 };
        panel.Children.Add(new TextBlock { Text = zone.Name, FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = T("Выбор своего цвета включает статическую подсветку."), FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = .65 });
        var enabled = new ToggleSwitch { Header = T("Подсветка зоны"), IsOn = zone.On, Visibility = zone.Switchable ? Visibility.Visible : Visibility.Collapsed };
        enabled.Toggled += (_, _) => { if (zone.On == enabled.IsOn) return; vm.ModeIndex = 0; zone.On = enabled.IsOn; };
        panel.Children.Add(enabled);
        var picker = new ColorPicker { Color = zone.Color, IsAlphaEnabled = false, ColorSpectrumShape = ColorSpectrumShape.Ring, IsHexInputVisible = true, IsColorChannelTextInputVisible = true, IsMoreButtonVisible = false };
        picker.ColorChanged += (_, args) =>
        {
            if (zone.Color == args.NewColor) return;
            vm.ModeIndex = 0;
            zone.On = true;
            enabled.IsOn = true;
            zone.Color = args.NewColor;
        };
        panel.Children.Add(picker);
        button.Flyout = new Flyout { Content = panel };
        return button;
    }
}
