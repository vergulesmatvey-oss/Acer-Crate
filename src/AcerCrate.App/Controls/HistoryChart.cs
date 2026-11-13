using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using AcerCrate.App.Helpers;
using Windows.Foundation;

namespace AcerCrate.App.Controls;

/// <summary>
/// Area chart of recent temperatures (newest on the right), coloured by height on the temperature scale,
/// with an optional faint second series.
/// </summary>
public sealed partial class HistoryChart : CanvasElement
{
    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(HistoryBuffer), typeof(HistoryChart), new PropertyMetadata(null, (d, _) => ((HistoryChart)d).Invalidate()));

    public static readonly DependencyProperty SecondaryProperty = DependencyProperty.Register(
        nameof(Secondary), typeof(HistoryBuffer), typeof(HistoryChart), new PropertyMetadata(null, (d, _) => ((HistoryChart)d).Invalidate()));

    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(HistoryChart), new PropertyMetadata(0, (d, _) => ((HistoryChart)d).Invalidate()));

    public HistoryBuffer? Series
    {
        get => (HistoryBuffer?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    /// <summary>Drawn as a thin line on a 0–100 scale (e.g. load %).</summary>
    public HistoryBuffer? Secondary
    {
        get => (HistoryBuffer?)GetValue(SecondaryProperty);
        set => SetValue(SecondaryProperty, value);
    }

    public int Revision
    {
        get => (int)GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public double Minimum { get; set; } = 25;

    public double Maximum { get; set; } = 100;
    public bool ShowAxes { get; set; }
    public string AxisUnit { get; set; } = "";
    public bool UseTemperatureColors { get; set; } = true;
    public double TimeWindowSeconds { get; set; } = 300;

    protected override void OnDraw(CanvasDrawingSession session, Size size)
    {
        var w = (float)size.Width;
        var h = (float)size.Height;
        if (w < 4 || h < 4)
            return;

        var left = ShowAxes ? AxisUnit == T("об/мин") ? 78f : 48f : 0f;
        var top = ShowAxes ? 9f : 0f;
        var bottom = ShowAxes ? 23f : 0f;
        var plotW = w - left - (ShowAxes ? 8 : 0);
        var plotH = h - top - bottom;
        if (plotW < 10 || plotH < 10) return;
        if (ShowAxes)
        {
            using var format = new CanvasTextFormat { FontSize = 10, HorizontalAlignment = CanvasHorizontalAlignment.Right, VerticalAlignment = CanvasVerticalAlignment.Center };
            for (var i = 0; i <= 4; i++)
            {
                var value = Maximum - (Maximum - Minimum) * i / 4;
                session.DrawText($"{value:0} {AxisUnit}", new Rect(0, top + plotH * i / 4 - 8, left - 8, 16), TextSecondary, format);
            }
            format.HorizontalAlignment = CanvasHorizontalAlignment.Left;
            var end = Series?.LatestTimestamp;
            var startLabel = end?.AddSeconds(-TimeWindowSeconds).ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture) ?? T("−5 мин");
            var endLabel = end?.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture) ?? T("Сейчас");
            session.DrawText(startLabel, new Rect(left, h - 19, 90, 18), TextTertiary, format);
            format.HorizontalAlignment = CanvasHorizontalAlignment.Right;
            session.DrawText(endLabel, new Rect(w - 98, h - 19, 90, 18), TextTertiary, format);
        }
        var transform = session.Transform;
        session.Transform = Matrix3x2.CreateTranslation(left, top) * transform;
        DrawPlot(session, plotW, plotH);
        session.Transform = transform;
    }

    private void DrawPlot(CanvasDrawingSession session, float w, float h)
    {
        for (var i = 0; i <= 4; i++)
        {
            var y = h * i / 4f;
            session.DrawLine(0, y, w, y, GridLine, 1);
        }

        if (Secondary is { Count: > 1 } secondary)
        {
            using var line = Path(session, secondary, w, h, 0, 100, close: false, ShowAxes ? TimeWindowSeconds : null);
            if (line is not null)
                session.DrawGeometry(line, TextTertiary, 1.2f);
        }

        if (Series is not { Count: > 1 } series || series.Range() is null)
        {
            if (ShowAxes)
            {
                using var format = new CanvasTextFormat { FontSize = 12, HorizontalAlignment = CanvasHorizontalAlignment.Center, VerticalAlignment = CanvasVerticalAlignment.Center };
                session.DrawText(T("Нет показаний"), new Rect(0, 0, w, h), TextTertiary, format);
            }
            return;
        }

        using (var area = Path(session, series, w, h, Minimum, Maximum, close: true, ShowAxes ? TimeWindowSeconds : null))
        {
            if (area is not null)
            {
                using var fill = Gradient(session, h, 0x55, fade: true);
                session.FillGeometry(area, fill);
            }
        }

        using (var line = Path(session, series, w, h, Minimum, Maximum, close: false, ShowAxes ? TimeWindowSeconds : null))
        {
            if (line is not null)
            {
                using var style = new CanvasStrokeStyle { LineJoin = CanvasLineJoin.Round };
                using var stroke = Gradient(session, h, 0xFF, fade: false);
                session.DrawGeometry(line, stroke, 2f, style);
            }
        }
    }

    /// <summary>
    /// Top-to-bottom brush following the temperature scale by height, so the line shifts smoothly from
    /// cool to hot colours as it rises. With <paramref name="fade"/> it also fades out towards the bottom
    /// (for the area under the line).
    /// </summary>
    private CanvasLinearGradientBrush Gradient(ICanvasResourceCreator creator, float h, byte alpha, bool fade)
    {
        const int Samples = 12;
        var stops = new CanvasGradientStop[Samples + 1];
        for (var i = 0; i <= Samples; i++)
        {
            var position = i / (float)Samples;
            var color = UseTemperatureColors ? TemperatureScale.ColorFor(Maximum - position * (Maximum - Minimum)) : AcerCrate.App.Services.CrateAppearance.AccentColor;
            stops[i] = new CanvasGradientStop
            {
                Position = position,
                Color = TemperatureScale.WithAlpha(color, (byte)(fade ? alpha * (1 - position) : alpha)),
            };
        }
        return new CanvasLinearGradientBrush(creator, stops) { StartPoint = new Vector2(0, 0), EndPoint = new Vector2(0, h) };
    }

    /// <summary>Builds the series path; gaps (nulls) break the line.</summary>
    private static CanvasGeometry? Path(ICanvasResourceCreator creator, HistoryBuffer buffer, float w, float h, double min, double max, bool close, double? seconds = null)
    {
        using var builder = new CanvasPathBuilder(creator);
        var step = w / (buffer.Capacity - 1);
        var offset = (buffer.Capacity - buffer.Count) * step;
        var open = false;
        float firstX = 0, lastX = 0;
        var any = false;
        var end = buffer.LatestTimestamp;

        for (var i = 0; i < buffer.Count; i++)
        {
            var position = seconds is {} window && end is {} last && buffer.TimestampAt(i) is {} time ? 1 - (last - time).TotalSeconds / window : (offset + i * step) / w;
            if (position < 0) continue;
            if (buffer[i] is not { } value)
            {
                if (open)
                {
                    Close(builder, close, firstX, lastX, h);
                    open = false;
                }
                continue;
            }
            var x = (float)position * w;
            var y = h - (float)Math.Clamp((value - min) / (max - min), 0, 1) * (h - 2) - 1;
            if (!open)
            {
                builder.BeginFigure(close ? new Vector2(x, h) : new Vector2(x, y));
                if (close)
                    builder.AddLine(x, y);
                firstX = x;
                open = true;
            }
            else
            {
                builder.AddLine(x, y);
            }
            lastX = x;
            any = true;
        }
        if (open)
            Close(builder, close, firstX, lastX, h);

        return any ? CanvasGeometry.CreatePath(builder) : null;
    }

    private static void Close(CanvasPathBuilder builder, bool close, float firstX, float lastX, float h)
    {
        if (close)
        {
            builder.AddLine(lastX, h);
            builder.AddLine(firstX, h);
            builder.EndFigure(CanvasFigureLoop.Closed);
        }
        else
        {
            builder.EndFigure(CanvasFigureLoop.Open);
        }
    }
}
