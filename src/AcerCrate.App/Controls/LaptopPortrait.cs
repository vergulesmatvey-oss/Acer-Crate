using System.Numerics;
using AcerCrate.Core.Hardware;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace AcerCrate.App.Controls;

/// <summary>Frames a single laptop from a catalog photograph without changing the original image.</summary>
public sealed class LaptopPortrait : Grid
{
    private readonly Canvas frame = new();
    private readonly Image photo = new() { Stretch = Stretch.Fill };
    private CanvasGeometry? outline;

    public LaptopPortrait()
    {
        frame.Children.Add(photo);
        Children.Add(new Viewbox { Child = frame, Stretch = Stretch.Uniform });
    }

    public bool HasPhoto => photo.Source is not null;

    public void Show(BitmapImage? bitmap, ArtworkFraming? framing, string? name)
    {
        var visual = ElementCompositionPreview.GetElementVisual(photo);
        visual.Clip = null;
        outline?.Dispose();
        outline = null;
        photo.Source = bitmap;
        Visibility = bitmap is not null ? Visibility.Visible : Visibility.Collapsed;
        if (bitmap is null) return;

        var width = (double)bitmap.PixelWidth;
        var height = (double)bitmap.PixelHeight;
        var bounds = framing?.Bounds is [var x, var y, var w, var h] && x >= 0 && y >= 0 && w > 0 && h > 0 && x + w <= 1 && y + h <= 1
            ? new Rect(x * width, y * height, w * width, h * height)
            : new Rect(0, 0, width, height);
        frame.Width = bounds.Width;
        frame.Height = bounds.Height;
        frame.Clip = new RectangleGeometry { Rect = new Rect(0, 0, bounds.Width, bounds.Height) };
        photo.Width = width;
        photo.Height = height;
        Canvas.SetLeft(photo, -bounds.X);
        Canvas.SetTop(photo, -bounds.Y);
        if (name is not null) AutomationProperties.SetName(photo, name);

        var points = framing?.Outline;
        if (points is not { Length: >= 6 } || points.Length % 2 != 0) return;
        var vertices = new Vector2[points.Length / 2];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector2((float)(points[i * 2] * width), (float)(points[i * 2 + 1] * height));

        outline = CanvasGeometry.CreatePolygon(CanvasDevice.GetSharedDevice(), vertices);
        var path = visual.Compositor.CreatePathGeometry(new CompositionPath(outline));
        visual.Clip = visual.Compositor.CreateGeometricClip(path);
    }
}
