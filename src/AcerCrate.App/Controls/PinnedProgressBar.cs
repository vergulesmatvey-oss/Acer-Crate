using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using AcerCrate.App.Services;

namespace AcerCrate.App.Controls;

/// <summary>A measured value grows from its fixed start; the WinUI reposition transition must not move the whole fill.</summary>
public sealed class PinnedProgressBar : ProgressBar
{
    private Rectangle? fill;
    private Panel? host;
    private Microsoft.UI.Composition.Visual? fillVisual;

    public PinnedProgressBar()
    {
        ValueChanged += (_, _) => UpdateFill(true);
        RegisterPropertyChangedCallback(MinimumProperty, (_, _) => UpdateFill(true));
        RegisterPropertyChangedCallback(MaximumProperty, (_, _) => UpdateFill(true));
        RegisterPropertyChangedCallback(IsIndeterminateProperty, (_, _) => UpdateFill(false));
        Loaded += (_, _) => { App.Current.WindowVisibilityChanged += OnVisibilityChanged; UpdateFill(false); };
        Unloaded += (_, _) => { App.Current.WindowVisibilityChanged -= OnVisibilityChanged; fillVisual?.StopAnimation("Scale.X"); };
    }

    private void OnVisibilityChanged() => UpdateFill(false);

    protected override void OnApplyTemplate()
    {
        if (fill is not null) host?.Children.Remove(fill);
        fillVisual?.StopAnimation("Scale.X");
        fill = null; host = null; fillVisual = null;
        base.OnApplyTemplate();
        if (GetTemplateChild("LayoutRoot") is not FrameworkElement root) return;
        foreach (var group in VisualStateManager.GetVisualStateGroups(root))
        {
            for (var i = group.Transitions.Count - 1; i >= 0; i--)
            {
                if (group.Transitions[i].Storyboard?.Children.Any(animation => animation is RepositionThemeAnimation) == true)
                    group.Transitions.RemoveAt(i);
            }
        }
        if (GetTemplateChild("DeterminateProgressBarIndicator") is not Rectangle indicator || VisualTreeHelper.GetParent(indicator) is not Panel panel) return;
        indicator.Opacity = 0;
        host = panel;
        fill = new Rectangle { Name = "PinnedDeterminateFill", IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        fill.SetBinding(Shape.FillProperty, new Binding { Source = indicator, Path = new PropertyPath("Fill"), Mode = BindingMode.OneWay });
        fill.SetBinding(Rectangle.RadiusXProperty, new Binding { Source = indicator, Path = new PropertyPath("RadiusX"), Mode = BindingMode.OneWay });
        fill.SetBinding(Rectangle.RadiusYProperty, new Binding { Source = indicator, Path = new PropertyPath("RadiusY"), Mode = BindingMode.OneWay });
        panel.Children.Add(fill);
        fillVisual = ElementCompositionPreview.GetElementVisual(fill);
        fillVisual.CenterPoint = Vector3.Zero;
        UpdateFill(false);
    }

    private void UpdateFill(bool animate)
    {
        if (fill is null || fillVisual is null) return;
        fill.Visibility = IsIndeterminate ? Visibility.Collapsed : Visibility.Visible;
        var fraction = Maximum > Minimum ? (float)Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) : 0;
        if (animate && IsLoaded && App.Current.IsWindowVisible && !IsIndeterminate && CrateAppearance.AnimationsEnabled)
        {
            var animation = fillVisual.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertExpressionKeyFrame(0, "this.StartingValue");
            animation.InsertKeyFrame(1, fraction, fillVisual.Compositor.CreateCubicBezierEasingFunction(new Vector2(.2f, 0), new Vector2(.2f, 1)));
            animation.Duration = TimeSpan.FromMilliseconds(220);
            fillVisual.StartAnimation("Scale.X", animation);
        }
        else
        {
            fillVisual.StopAnimation("Scale.X");
            fillVisual.Scale = new Vector3(fraction, 1, 1);
        }
    }
}
