using Microsoft.UI.Dispatching;
using OpenSense.App.Views;
using OpenSense.Core.Control;
using OpenSense.Core.Monitoring;

namespace OpenSense.App.Services;

public sealed class ModeOsdService(DispatcherQueue dispatcher, DeviceSession session, SettingsService settings) : IDisposable
{
    private readonly ModeChangeTracker tracker = new();
    private readonly object gate = new();
    private ModeOsdWindow? window;
    private bool started, disposed;

    public void Initialize()
    {
        if (started) return;
        started = true;
        session.TelemetryUpdated += OnTelemetry;
        App.Current.WindowVisibilityChanged += OnVisibilityChanged;
    }

    private void OnTelemetry(Telemetry sample)
    {
        if (disposed || !LiveTelemetry.IsFresh(sample, DateTime.UtcNow)) return;
        bool show;
        lock (gate)
            show = tracker.Observe(sample.OperatingMode, sample.ControlConfirmed, sample.Failsafe,
                App.Current.IsWindowVisible || !settings.Current.Ui.ModeOsdWhenHidden);
        if (!show || sample.OperatingMode is not { } mode) return;
        dispatcher.TryEnqueue(() =>
        {
            if (disposed || App.Current.IsExiting || App.Current.IsWindowVisible || !settings.Current.Ui.ModeOsdWhenHidden) return;
            (window ??= new ModeOsdWindow()).ShowMode(mode);
        });
    }

    private void OnVisibilityChanged() { if (App.Current.IsWindowVisible) window?.Hide(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        session.TelemetryUpdated -= OnTelemetry;
        App.Current.WindowVisibilityChanged -= OnVisibilityChanged;
        window?.Stop(); window = null;
    }
}
