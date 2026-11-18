using System.Diagnostics;
using AcerCrate.App.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using MeterPath = Microsoft.UI.Xaml.Shapes.Path;

namespace AcerCrate.App.Controls;

/// <summary>A telemetry gauge with a fixed baseline and a straight bevel at the end of its fill.</summary>
public sealed class CrateMeter : ProgressBar
{
    private MeterPath? fill;
    private readonly LineSegment shoulder = new();
    private readonly LineSegment tip = new();
    private readonly LineSegment bottom = new();
    private readonly PathGeometry geometry = new();
    private const double AnimationMilliseconds = 230;
    private DispatcherQueueTimer? animation;
    private long animationStart;
    private double startFraction;
    private double displayedFraction;
    private double targetFraction;

    public CrateMeter()
    {
        var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
        figure.Segments.Add(shoulder);
        figure.Segments.Add(tip);
        figure.Segments.Add(bottom);
        geometry.Figures.Add(figure);

        Style = (Style)Application.Current.Resources["CrateMeterStyle"];
        SizeChanged += (_, _) => Draw();
        ValueChanged += (_, _) => UpdateValue(animate: true);
        RegisterPropertyChangedCallback(MinimumProperty, (_, _) => UpdateValue(animate: false));
        RegisterPropertyChangedCallback(MaximumProperty, (_, _) => UpdateValue(animate: false));
        Loaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged += OnVisibilityChanged;
            UpdateValue(animate: false);
        };
        Unloaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged -= OnVisibilityChanged;
            animation?.Stop();
        };
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        fill = GetTemplateChild("MeterFill") as MeterPath;
        if (fill is null) return;

        fill.Data = geometry;
        UpdateValue(animate: false);
    }

    private void OnVisibilityChanged() => UpdateValue(animate: false);

    private void UpdateValue(bool animate)
    {
        var span = Maximum - Minimum;
        targetFraction = span > 0 ? Math.Clamp((Value - Minimum) / span, 0, 1) : 0;
        if (animate && fill is not null && IsLoaded && ActualWidth > 0 &&
            displayedFraction != targetFraction && App.Current.IsWindowVisible && CrateAppearance.AnimationsEnabled)
        {
            if (animation is null)
            {
                animation = DispatcherQueue.CreateTimer();
                animation.Interval = TimeSpan.FromMilliseconds(16);
                animation.Tick += Animate;
            }
            startFraction = displayedFraction;
            animationStart = Stopwatch.GetTimestamp();
            animation.Start();
            return;
        }

        animation?.Stop();
        displayedFraction = targetFraction;
        Draw();
    }

    private void Animate(DispatcherQueueTimer timer, object args)
    {
        var progress = Math.Min(Stopwatch.GetElapsedTime(animationStart).TotalMilliseconds / AnimationMilliseconds, 1);
        var remaining = 1 - progress;
        displayedFraction = targetFraction + (startFraction - targetFraction) * remaining * remaining * remaining;
        Draw();
        if (progress >= 1) timer.Stop();
    }

    private void Draw()
    {
        if (fill is null) return;

        var width = ActualWidth * displayedFraction;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || !double.IsFinite(width))
        {
            fill.Visibility = Visibility.Collapsed;
            return;
        }

        var bevel = Math.Min(height * .8, width / 2);
        shoulder.Point = new Point(width - bevel, 0);
        tip.Point = new Point(width, height);
        bottom.Point = new Point(0, height);
        fill.Visibility = Visibility.Visible;
    }
}
