using Microsoft.Graphics.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Services;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace AcerCrate.App.Controls;

/// <summary>
/// Base for controls drawn with Win2D. Creates the CanvasControl while loaded and releases it on
/// unload (Win2D's documented requirement to avoid leaking device resources).
/// </summary>
public abstract partial class CanvasElement : UserControl
{
    // Windows' own colours; kept alive so its change event keeps firing.
    private static readonly UISettings UiSettings = new();

    private readonly Grid _root = new();
    private readonly SettingsService _settings = App.Current.Services.GetRequiredService<SettingsService>();
    private CanvasControl? _canvas;

    protected CanvasElement()
    {
        Content = _root;
        // Graphs, fans and the keyboard are pictures: right-to-left languages mustn't mirror them (or the text drawn on them).
        FlowDirection = FlowDirection.LeftToRight;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += (_, _) => Invalidate();
    }

    /// <summary>Put XAML overlays (text etc.) here; the canvas sits underneath.</summary>
    protected Grid Root => _root;

    protected CanvasControl? Canvas => _canvas;

    public void Invalidate()
    {
        if (App.Current.IsWindowVisible) _canvas?.Invalidate();
    }

    protected abstract void OnDraw(CanvasDrawingSession session, Size size);

    /// <summary>Hook pointer input here; called each time a canvas is created.</summary>
    protected virtual void AttachInput(CanvasControl canvas)
    {
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_canvas is not null)
            return;
        UiSettings.ColorValuesChanged += OnColorValuesChanged;
        _settings.Changed += OnAppearanceChanged;
        App.Current.WindowVisibilityChanged += Invalidate;
        _canvas = new CanvasControl { ClearColor = Colors.Transparent };
        _canvas.Draw += (s, args) => { if (App.Current.IsWindowVisible) OnDraw(args.DrawingSession, s.Size); };
        AttachInput(_canvas);
        _root.Children.Insert(0, _canvas);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_canvas is null)
            return;
        UiSettings.ColorValuesChanged -= OnColorValuesChanged;
        _settings.Changed -= OnAppearanceChanged;
        App.Current.WindowVisibilityChanged -= Invalidate;
        _root.Children.Remove(_canvas);
        _canvas.RemoveFromVisualTree();
        _canvas = null;
    }

    // A new accent colour in Windows; the event comes on a background thread.
    private void OnColorValuesChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(Invalidate);
    private void OnAppearanceChanged(AcerCrate.Core.Settings.UserSettings _) => DispatcherQueue.TryEnqueue(Invalidate);

    // Theme-aware colours matching WinUI's text and stroke brushes.
    protected bool IsLight => ActualTheme == ElementTheme.Light;

    protected Color TextPrimary => CrateAppearance.TextColor(IsLight);

    protected Color TextSecondary => CrateAppearance.TextColor(IsLight, 200);

    protected Color TextTertiary => CrateAppearance.TextColor(IsLight, 160);

    protected Color Track => IsLight ? Color.FromArgb(0x18, 0, 0, 0) : Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF);

    protected Color GridLine => IsLight ? Color.FromArgb(0x14, 0, 0, 0) : Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF);

    /// <summary>Windows' accent colour in the shade WinUI's accent fill uses: lighter on dark, darker on light.</summary>
    protected static Color Accent => CrateAppearance.AccentColor;

    protected Color Surface => IsLight ? Color.FromArgb(0xFF, 0xFB, 0xFB, 0xFB) : Colors.Black;
}
