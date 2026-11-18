using AcerCrate.App.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AcerCrate.App.Controls;

public sealed class FanNoiseMeter : StackPanel
{
    private readonly DispatcherQueueTimer timer;
    private readonly Button toggle = new() { FontSize = 12, Padding = new Thickness(9, 4, 9, 4) };
    private readonly Button calibrate = new() { FontSize = 12, Padding = new Thickness(9, 4, 9, 4), Visibility = Visibility.Collapsed };
    private readonly TextBlock reading = new() { Text = "—", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
    private readonly TextBlock status = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
    private MicrophoneLevel? capture;
    private double? dbfs;
    private double? calibration;
    private int request;
    private long lastSample;

    public FanNoiseMeter()
    {
        Spacing = 6;
        var header = new Grid { ColumnSpacing = 8 };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var label = new TextBlock
        {
            Text = T("Шум у микрофона"),
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        };
        header.Children.Add(label);
        Grid.SetColumn(reading, 1);
        header.Children.Add(reading);
        ToolTipService.SetToolTip(header, T("Микрофон измеряет весь звук рядом с ноутбуком. Для шкалы дБ SPL нужен эталонный замер."));
        Children.Add(header);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        toggle.Content = T("Измерить шум");
        toggle.IsEnabled = !App.Current.Options.Preview;
        toggle.Click += ToggleCapture;
        calibrate.Content = T("Калибровать");
        calibrate.Flyout = BuildCalibration();
        actions.Children.Add(toggle);
        actions.Children.Add(calibrate);
        Children.Add(actions);
        Children.Add(status);

        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(250);
        timer.Tick += (_, _) => ReadLevel();
        Loaded += (_, _) => App.Current.WindowVisibilityChanged += OnVisibilityChanged;
        Unloaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged -= OnVisibilityChanged;
            StopCapture();
        };
    }

    private void OnVisibilityChanged()
    {
        if (!App.Current.IsWindowVisible) StopCapture();
    }

    private async void ToggleCapture(object sender, RoutedEventArgs args)
    {
        if (capture is not null)
        {
            StopCapture();
            return;
        }

        var current = ++request;
        toggle.IsEnabled = false;
        ShowStatus(null);
        MicrophoneLevel? pending = null;
        try
        {
            pending = await MicrophoneLevel.OpenAsync();
            if (current != request || !IsLoaded || !App.Current.IsWindowVisible) return;
            if (pending is null)
            {
                ShowStatus(T("Микрофон недоступен. Проверьте разрешение Windows."));
                return;
            }
            pending.Start();
            capture = pending;
            pending = null;
            toggle.Content = T("Остановить замер");
            lastSample = Environment.TickCount64;
            timer.Start();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (current == request) ShowStatus(T("Микрофон недоступен. Проверьте разрешение Windows."));
        }
        finally
        {
            try { pending?.Dispose(); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { }
            if (current == request) toggle.IsEnabled = true;
        }
    }

    private void ReadLevel()
    {
        try
        {
            dbfs = capture?.ReadDbfs();
            if (dbfs is not null)
                lastSample = Environment.TickCount64;
            else if (Environment.TickCount64 - lastSample > 2000)
            {
                StopCapture(T("Микрофон перестал передавать данные."));
                return;
            }
            UpdateReading();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            StopCapture(T("Микрофон перестал передавать данные."));
        }
    }

    private void UpdateReading()
    {
        reading.Text = dbfs is not { } level ? "—" : calibration is { } offset
            ? F("≈ {0:0.0} дБ SPL", level + offset)
            : F("{0:0.0} дБFS", level);
        calibrate.Visibility = dbfs is > -100 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Flyout BuildCalibration()
    {
        var panel = new StackPanel { Spacing = 8, MaxWidth = 260 };
        var reference = new NumberBox { Header = T("Показание шумомера, дБ SPL"), Minimum = 20, Maximum = 120 };
        var apply = new Button { Content = T("Калибровать"), IsEnabled = false };
        var flyout = new Flyout { Content = panel };
        reference.ValueChanged += (_, _) => apply.IsEnabled = reference.Value is >= 20 and <= 120;
        apply.Click += (_, _) =>
        {
            if (dbfs is not { } level || level <= -100 || reference.Value is not (>= 20 and <= 120)) return;
            calibration = reference.Value - level;
            UpdateReading();
            flyout.Hide();
        };
        panel.Children.Add(CrateUi.Text(T("Введите показание шумомера, расположенного рядом с микрофоном."), 12));
        panel.Children.Add(reference);
        panel.Children.Add(apply);
        return flyout;
    }

    private void StopCapture(string? error = null)
    {
        request++;
        timer.Stop();
        var session = capture;
        capture = null;
        try { session?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        calibration = null;
        dbfs = null;
        UpdateReading();
        toggle.Content = T("Измерить шум");
        toggle.IsEnabled = !App.Current.Options.Preview;
        ShowStatus(error);
    }

    private void ShowStatus(string? message)
    {
        status.Text = message ?? "";
        status.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
    }
}
