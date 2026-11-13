using Microsoft.UI.Dispatching;
using AcerCrate.App.Views;
using AcerCrate.Core.Control;
using AcerCrate.Core.Monitoring;
using AcerCrate.Core.Hardware;
using AcerCrate.App.Localization;

namespace AcerCrate.App.Services;

public sealed class ModeOsdService(DispatcherQueue dispatcher, DeviceSession session, SettingsService settings) : IDisposable
{
    private readonly ModeChangeTracker tracker = new();
    private readonly object gate = new();
    private ModeOsdWindow? window;
    private bool started, disposed;
    private readonly ModeRequestState request = new();
    private DispatcherQueueTimer? requestTimeout;

    public void Initialize()
    {
        if (started) return;
        started = true;
        requestTimeout = dispatcher.CreateTimer();
        requestTimeout.Interval = TimeSpan.FromSeconds(8); requestTimeout.IsRepeating = false;
        requestTimeout.Tick += (_, _) => ShowFailure();
        session.TelemetryUpdated += OnTelemetry;
        App.Current.WindowVisibilityChanged += OnVisibilityChanged;
    }

    public void ShowRequested(LaptopMode mode)
    {
        var native = mode switch { LaptopMode.Quiet => OperatingMode.Quiet, LaptopMode.Performance => OperatingMode.Performance, _ => OperatingMode.Balanced };
        lock (gate) request.Request(native);
        requestTimeout?.Stop(); requestTimeout?.Start();
        Show(native, AppLanguage.Current == "ru-RU" ? "Применяется…" : "Applying…", true);
    }

    public void ShowPreview()
    {
        if (App.Current.Options.Preview)
            Show(OperatingMode.Quiet, AppLanguage.Current == "ru-RU" ? "Предпросмотр" : "Preview", true);
    }

    public void ShowFailure()
    {
        OperatingMode? requested;
        lock (gate) { requested = request.Pending; request.Fail(); }
        requestTimeout?.Stop();
        if (requested is { } mode) Show(mode, AppLanguage.Current == "ru-RU" ? "Не подтверждено" : "Not confirmed");
    }

    public void CommandAccepted(LaptopMode mode)
    {
        var native = mode switch { LaptopMode.Quiet => OperatingMode.Quiet, LaptopMode.Performance => OperatingMode.Performance, _ => OperatingMode.Balanced };
        lock (gate) request.Accepted(native, DateTime.UtcNow);
    }

    private void Show(OperatingMode mode, string? status = null, bool applying = false)
    {
        if (disposed || App.Current.IsExiting || App.Current.IsWindowVisible || !settings.Current.Ui.ModeOsdWhenHidden) return;
        try { (window ??= new ModeOsdWindow()).ShowMode(mode, status, applying); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        { Serilog.Log.Warning(error, "Could not display the mode notification"); }
    }

    private void OnTelemetry(Telemetry sample)
    {
        if (disposed || !LiveTelemetry.IsFresh(sample, DateTime.UtcNow)) return;
        bool show, completed = false;
        int revision;
        OperatingMode? notificationMode = sample.OperatingMode;
        lock (gate)
        {
            revision = request.Revision;
            if (request.Pending.HasValue)
            {
                notificationMode = request.Pending;
                completed = request.Confirm(sample);
                tracker.Observe(sample.OperatingMode, sample.ControlConfirmed, sample.Failsafe, true);
                if (!completed) return;
                show = true;
            }
            else show = tracker.Observe(sample.OperatingMode, sample.ControlConfirmed, sample.Failsafe,
                App.Current.IsWindowVisible || !settings.Current.Ui.ModeOsdWhenHidden);
        }
        if (!show || notificationMode is not { } mode) return;
        dispatcher.TryEnqueue(() =>
        {
            lock (gate) if (revision != request.Revision) return;
            if (completed) requestTimeout?.Stop();
            Show(mode);
        });
    }

    private void OnVisibilityChanged() { if (App.Current.IsWindowVisible) window?.Hide(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        requestTimeout?.Stop();
        session.TelemetryUpdated -= OnTelemetry;
        App.Current.WindowVisibilityChanged -= OnVisibilityChanged;
        window?.Stop(); window = null;
    }
}
