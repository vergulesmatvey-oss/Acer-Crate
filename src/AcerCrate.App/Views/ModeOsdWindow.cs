using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using AcerCrate.App.Localization;
using AcerCrate.App.Services;
using AcerCrate.Core.Hardware;
using Windows.Foundation;
using Windows.UI;

namespace AcerCrate.App.Views;

/// <summary>A passive per-pixel-alpha popup. DirectWrite/Direct2D smooth the pill, icons and text.</summary>
public sealed class ModeOsdWindow : IDisposable
{
    private static readonly WindowProcedure procedure = WindowProc;
    private static readonly string className = "AcerCrate.ModeOsd." + Environment.ProcessId;
    private static bool registered;
    private readonly DispatcherQueueTimer timer;
    private readonly nint window;
    private CanvasRenderTarget? surface;
    private nint memoryDc, bitmap, previousBitmap, bits;
    private int width, height;
    private bool stopped;

    public ModeOsdWindow()
    {
        var instance = GetModuleHandle(null);
        if (!registered)
        {
            var cls = new WindowClass { Size = (uint)Marshal.SizeOf<WindowClass>(), Instance = instance,
                Procedure = Marshal.GetFunctionPointerForDelegate(procedure), Name = className };
            if (RegisterClassEx(ref cls) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            registered = true;
        }
        // Layered, tool window, click-through and no activation. No native frame or hard-edged window region.
        window = CreateWindowEx(0x080800A0, className, "Acer Crate · mode", 0x80000000,
            0, 0, 1, 1, 0, 0, instance, 0);
        if (window == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        timer.IsRepeating = false;
        timer.Tick += (_, _) => Hide();
    }

    public void ShowMode(OperatingMode mode, string? status = null, bool applying = false)
    {
        if (stopped) return;
        var label = Names.OperatingMode(mode);
        var detail = status ?? (AppLanguage.Current == "ru-RU" ? "Режим включён" : "Mode active");
        SetWindowText(window, "Acer Crate · " + label + " · " + detail);
        var scale = Math.Max(1, GetDpiForWindow(window)) / 96f;
        var targetWidth = (int)Math.Round(350 * scale);
        var targetHeight = (int)Math.Round(76 * scale);
        EnsureSurface(targetWidth, targetHeight);
        var accent = CrateAppearance.AccentColor;
        var icon = mode switch { OperatingMode.Quiet or OperatingMode.Eco => "\uE708",
            OperatingMode.Performance or OperatingMode.Turbo => "\uE7C1", _ => "\uE945" };
        using (var drawing = surface!.CreateDrawingSession())
        {
            drawing.Clear(Color.FromArgb(0, 0, 0, 0));
            drawing.Antialiasing = CanvasAntialiasing.Antialiased;
            drawing.TextAntialiasing = CanvasTextAntialiasing.Grayscale;
            drawing.FillRoundedRectangle(0, 0, width, height, height / 2f, height / 2f, Color.FromArgb(255, 11, 14, 14));
            drawing.DrawRoundedRectangle(.5f, .5f, width - 1, height - 1, (height - 1) / 2f, (height - 1) / 2f,
                Color.FromArgb(65, accent.R, accent.G, accent.B), 1);
            drawing.FillCircle(40 * scale, 38 * scale, 24 * scale, Color.FromArgb(24, accent.R, accent.G, accent.B));
            using var glyph = new CanvasTextFormat { FontFamily = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) ? "Segoe Fluent Icons" : "Segoe MDL2 Assets", FontSize = 24 * scale,
                HorizontalAlignment = CanvasHorizontalAlignment.Center, VerticalAlignment = CanvasVerticalAlignment.Center };
            drawing.DrawText(icon, new Rect(16 * scale, 14 * scale, 48 * scale, 48 * scale), accent, glyph);
            using var title = new CanvasTextFormat { FontFamily = "Segoe UI Semibold", FontSize = 19 * scale,
                WordWrapping = CanvasWordWrapping.NoWrap };
            using var subtitle = new CanvasTextFormat { FontFamily = "Segoe UI", FontSize = 11 * scale,
                WordWrapping = CanvasWordWrapping.NoWrap };
            drawing.DrawText(label, new Rect(78 * scale, 16 * scale, 224 * scale, 27 * scale), Color.FromArgb(255, 255, 255, 255), title);
            drawing.DrawText(detail, new Rect(78 * scale, 44 * scale, 224 * scale, 18 * scale), Color.FromArgb(168, 255, 255, 255), subtitle);
            if (applying)
            {
                drawing.DrawCircle(322 * scale, 38 * scale, 7 * scale, Color.FromArgb(65, accent.R, accent.G, accent.B), 1.5f * scale);
                drawing.FillCircle(322 * scale, 31 * scale, 2 * scale, accent);
            }
            else
            {
                glyph.FontSize = 15 * scale;
                drawing.DrawText(status is null ? "\uE73E" : "\uE7BA", new Rect(311 * scale, 27 * scale, 22 * scale, 22 * scale), accent, glyph);
            }
        }
        var pixels = surface.GetPixelBytes(); // BGRA8, premultiplied alpha; zero-alpha corners remain transparent.
        Marshal.Copy(pixels, 0, bits, pixels.Length);
        var work = DisplayArea.Primary.WorkArea;
        var destination = new PointInt(work.X + (work.Width - width) / 2, work.Y + (work.Height - height) / 2);
        var size = new SizeInt(width, height);
        var source = new PointInt(0, 0);
        var blend = new Blend { ConstantAlpha = 255, AlphaFormat = 1 };
        if (!UpdateLayeredWindow(window, 0, ref destination, ref size, memoryDc, ref source, 0, ref blend, 2))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (!SetWindowPos(window, new nint(-1), destination.X, destination.Y, width, height, 0x50))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        timer.Stop(); timer.Interval = TimeSpan.FromMilliseconds(applying ? 10000 : 1700); timer.Start();
        if (App.Current.Options.Preview)
        {
            var edgePixels = 0;
            for (var i = 3; i < pixels.Length; i += 4) if (pixels[i] is > 0 and < 255) edgePixels++;
            Serilog.Log.Information("OSD alpha surface {Width}x{Height}: {Edges} smoothed pixels, corner alpha {Corner}", width, height, edgePixels, pixels[3]);
            _ = SavePreviewAsync(surface);
        }
    }

    private static async Task SavePreviewAsync(CanvasRenderTarget target)
    {
        try
        {
            var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(AppLogFiles.ClientDirectory);
            var file = await folder.CreateFileAsync("osd-smooth-preview.png", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            await target.SaveAsync(stream, CanvasBitmapFileFormat.Png);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ExternalException)
        { Serilog.Log.Debug(error, "Could not save OSD visual preview"); }
    }

    private void EnsureSurface(int newWidth, int newHeight)
    {
        if (surface is not null && width == newWidth && height == newHeight) return;
        FreeSurface(); width = newWidth; height = newHeight;
        surface = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
        memoryDc = CreateCompatibleDC(0);
        var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
        bitmap = CreateDIBSection(memoryDc, ref info, 0, out bits, 0, 0);
        if (memoryDc == 0 || bitmap == 0) { FreeSurface(); throw new Win32Exception(Marshal.GetLastWin32Error()); }
        previousBitmap = SelectObject(memoryDc, bitmap);
    }

    public void Hide() { if (stopped) return; timer.Stop(); ShowWindow(window, 0); }
    public void Stop()
    {
        if (stopped) return;
        stopped = true; timer.Stop(); DestroyWindow(window); FreeSurface();
    }
    public void Dispose() => Stop();
    private void FreeSurface()
    {
        surface?.Dispose(); surface = null;
        if (previousBitmap != 0 && memoryDc != 0) SelectObject(memoryDc, previousBitmap);
        if (bitmap != 0) DeleteObject(bitmap);
        if (memoryDc != 0) DeleteDC(memoryDc);
        memoryDc = bitmap = previousBitmap = bits = 0;
    }
    private static nint WindowProc(nint handle, uint message, nuint wParam, nint lParam) => message switch
    {
        0x0084 => new nint(-1), // HTTRANSPARENT
        0x0021 => new nint(3), // MA_NOACTIVATE
        0x0014 => new nint(1),
        _ => DefWindowProc(handle, message, wParam, lParam)
    };

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate nint WindowProcedure(nint handle, uint message, nuint wParam, nint lParam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct WindowClass
    {
        public uint Size, Style; public nint Procedure; public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background; public string? Menu; public string Name; public nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct PointInt(int x, int y) { public int X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct SizeInt(int width, int height) { public int Width = width, Height = height; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { public byte Operation, Flags, ConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, ImageSize;
        public int XPixelsPerMeter, YPixelsPerMeter; public uint ColorsUsed, ColorsImportant, Color;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassEx(ref WindowClass windowClass);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint CreateWindowEx(uint extendedStyle, string className, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(nint window, string text);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint value);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(nint window, nint destinationDc, ref PointInt destination, ref SizeInt size, nint sourceDc, ref PointInt source, uint colorKey, ref Blend blend, uint flags);
}
