using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using OpenSense.App.Localization;
using OpenSense.App.Services;
using OpenSense.Core.Hardware;
using Windows.Graphics;

namespace OpenSense.App.Views;

public sealed partial class ModeOsdWindow : Window
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;
    private (int Width, int Height) regionSize;
    private readonly SolidColorBrush accentBrush = new();

    public ModeOsdWindow()
    {
        InitializeComponent();
        Pill.BorderBrush = ModeIcon.Foreground = accentBrush;
        ExtendsContentIntoTitleBar = true;
        AppWindow.IsShownInSwitchers = false;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false; presenter.IsMaximizable = false; presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        // Passive and click-through: the desktop/game keeps its focus and keyboard input.
        SetWindowLongPtr(handle, -20, GetWindowLongPtr(handle, -20) | new nint(0x08000020));
        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(1700);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => Hide();
    }

    public void ShowMode(OperatingMode mode)
    {
        ModeLabel.Text = Names.OperatingMode(mode);
        accentBrush.Color = CrateAppearance.AccentColor;
        ModeIcon.Glyph = mode switch
        {
            OperatingMode.Quiet or OperatingMode.Eco => "\uE708",
            OperatingMode.Performance or OperatingMode.Turbo => "\uE7C1",
            _ => "\uE945"
        };
        var nativeHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var handle = new Windows.Win32.Foundation.HWND(nativeHandle);
        var scale = Windows.Win32.PInvoke.GetDpiForWindow(handle) / 96.0;
        var area = DisplayArea.Primary.WorkArea;
        var width = (int)(410 * scale); var height = (int)(78 * scale);
        AppWindow.MoveAndResize(new RectInt32(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height));
        ApplyPillRegion(nativeHandle, width, height);
        AppWindow.Show(false);
        timer.Stop(); timer.Start();
    }

    public void Hide() { timer.Stop(); AppWindow.Hide(); }
    public void Stop() { timer.Stop(); Close(); }

    private void ApplyPillRegion(nint handle, int width, int height)
    {
        if (regionSize == (width, height)) return;
        var region = CreateRoundRectRgn(0, 0, width + 1, height + 1, height, height);
        if (region == 0) return;
        // Windows takes ownership on success and frees the previous region itself.
        if (SetWindowRgn(handle, region, true) != 0) regionSize = (width, height);
        else DeleteObject(region);
    }

    [DllImport("gdi32.dll")]
    private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);
    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(nint window, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);
}
