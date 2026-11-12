using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using OpenSense.App.Services;
using OpenSense.App.Startup;
using OpenSense.App.ViewModels;
using OpenSense.Core.Settings;
using OpenSense.Core.Updates;
using Serilog;

namespace OpenSense.App;

public partial class App : Application
{
    private readonly LaunchOptions _options;
    private readonly SingleInstance _instance;
    private IHost? _host;
    private MainWindow? _window;
    private bool _exiting;
    private volatile bool _windowVisible;

    public App(LaunchOptions options, SingleInstance instance)
    {
        _options = options;
        _instance = instance;
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            Helpers.UiErrorDiagnostics.Record(e.Exception, "Unhandled UI");
            Log.Error(e.Exception, "Unhandled UI exception");
        };
    }

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("The app has not started yet.");

    public LaunchOptions Options => _options;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = BuildHost();
        await _host.StartAsync();
        if (!_options.Preview) AutostartService.Refresh(); // a portable copy's sign-in task follows it when it was moved

        _window = Services.GetRequiredService<MainWindow>();
        _window.AppWindow.Changed += (_, _) => RefreshWindowVisibility();
        _window.Activated += (_, _) => RefreshWindowVisibility();
        _window.Closed += (_, _) =>
        {
            _window = null;
            _windowVisible = false;
            if (!_exiting) Quit();
        };
        var dispatcher = _window.DispatcherQueue;
        Services.GetRequiredService<TrayService>().Initialize();
        Services.GetRequiredService<ToastService>().Initialize();
        _instance.Activated += () => dispatcher.TryEnqueue(ShowMainWindow);
        _instance.ExitRequested += () => dispatcher.TryEnqueue(Quit);

        var settings = Services.GetRequiredService<SettingsService>().Current;
        if (!_options.Autostart || !settings.Ui.StartMinimized)
        {
            ShowMainWindow();
        }

        if (_options.Preview) { await Services.GetRequiredService<ShellViewModel>().InitializeAsync(); return; }
        Services.GetRequiredService<ModeOsdService>().Initialize();
        Services.GetRequiredService<ProfileShortcuts>().Initialize();
        // Upstream OpenSense releases must not overwrite this custom application.
        var nitroSenseKey = Services.GetRequiredService<NitroSenseKey>();
        nitroSenseKey.Pressed += () => dispatcher.TryEnqueue(ShowMainWindow);
        if (settings.Ui.OpenWithNitroSenseKey)
            nitroSenseKey.Start();
        var shortcut = Services.GetRequiredService<OpenShortcut>();
        shortcut.Pressed += () => dispatcher.TryEnqueue(ShowMainWindow);
        shortcut.Set(settings.Ui.OpenShortcut);
        await Services.GetRequiredService<ShellViewModel>().InitializeAsync();
    }

    private IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenSense", "logs");
        builder.Services.AddSerilog(log => log
            .MinimumLevel.Warning()
            .WriteTo.File(Path.Combine(logDirectory, "opensense-.log"), formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

        builder.Services.AddSingleton(_options);
        builder.Services.AddSingleton(DispatcherQueue.GetForCurrentThread());
        builder.Services.AddSingleton(new SettingsStore<UserSettings>(_options.Preview ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenSensePreview", "settings.json") : SettingsPaths.User));
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<DeviceSession>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<TrayService>();
        builder.Services.AddSingleton<ToastService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton<NitroSenseKey>();
        builder.Services.AddSingleton<OpenShortcut>();
        builder.Services.AddSingleton<ProfileShortcuts>();
        builder.Services.AddSingleton<PersonalModeService>();
        builder.Services.AddSingleton<ModeOsdService>();
        builder.Services.AddSingleton(UpdaterOptions());
        builder.Services.AddHttpClient<GitHubUpdater>(client =>
        {
            // GitHub's REST API requires a User-Agent; pin the API version it answers with.
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OpenSense", UpdaterOptions().CurrentVersion.ToString(3)));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            client.Timeout = TimeSpan.FromMinutes(10); // the installer is ~80 MB
        });

        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.AddSingleton<MonitorViewModel>();
        builder.Services.AddSingleton<FanControlViewModel>();
        builder.Services.AddSingleton<LightingViewModel>();
        builder.Services.AddSingleton<SystemViewModel>();
        builder.Services.AddSingleton<BatteryViewModel>();
        builder.Services.AddSingleton<StartupViewModel>();
        builder.Services.AddSingleton<GpuClocksViewModel>();
        builder.Services.AddSingleton<BiosSettingsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<UpdateViewModel>();

        builder.Services.AddSingleton<MainWindow>();
        return builder.Build();
    }

    public void ShowMainWindow()
    {
        if (_exiting || _window is not { } window || window.AppWindow is not { } appWindow)
            return;
        appWindow.Show();
        if (appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        window.Activate();
        appWindow.MoveInZOrderAtTop();
        Windows.Win32.PInvoke.SetForegroundWindow(new Windows.Win32.Foundation.HWND(WinRT.Interop.WindowNative.GetWindowHandle(window)));
        RefreshWindowVisibility();
    }

    /// <summary>Retry elevation from the "administrator rights needed" screen (portable copy).</summary>
    public void RelaunchElevated()
    {
        _instance.Dispose(); // let the elevated copy become the running instance
        if (Program.TryRelaunchElevated(_options))
            Quit();
    }

    /// <summary>Quits and opens a new copy on the Settings page, e.g. to show a new language.</summary>
    public void Restart()
    {
        _instance.Dispose(); // let the new copy become the running instance once this one has exited
        var arguments = new LaunchOptions { Page = "settings", WaitForProcess = Environment.ProcessId }.ToArguments();
        // Started directly, not through the shell, so it keeps this copy's rights (a portable copy runs elevated).
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!, arguments) { UseShellExecute = false })?.Dispose();
        Quit();
    }

    public void Quit()
    {
        if (_exiting)
            return;
        _exiting = true;
        // Stop UI timers and compositor work while the services they use are still alive.
        if (_windowVisible)
        {
            _windowVisible = false;
            WindowVisibilityChanged?.Invoke();
        }
        if (_host is { } host)
        {
            // Each part closes whatever the one before did: a hosted engine must still hand the fans back.
            Close(Services.GetRequiredService<NitroSenseKey>);
            Close(Services.GetRequiredService<OpenShortcut>);
            Close(Services.GetRequiredService<ProfileShortcuts>);
            Close(Services.GetRequiredService<ToastService>);
            Close(Services.GetRequiredService<TrayService>);
            Close(Services.GetRequiredService<ModeOsdService>);
            Close(Services.GetRequiredService<DeviceSession>); // the service keeps control; a hosted engine hands the fans back
            Close(Services.GetRequiredService<SettingsService>);
            Close(() => host);
        }
        Log.CloseAndFlush();
        Exit();
    }

    private static void Close<T>(Func<T> part) where T : IDisposable
    {
        try
        {
            part().Dispose();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Log.Error(ex, "Closing {Part} failed", typeof(T).Name);
        }
    }

    public bool IsExiting => _exiting;

    /// <summary>Cached on the UI thread; safe for telemetry callbacks to read.</summary>
    public bool IsWindowVisible => _windowVisible;

    internal void RefreshWindowVisibility()
    {
        var appWindow = _exiting ? null : _window?.AppWindow;
        var visible = appWindow?.IsVisible == true &&
            appWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Minimized };
        if (_windowVisible == visible) return;
        _windowVisible = visible;
        WindowVisibilityChanged?.Invoke();
    }

    /// <summary>The main window was shown or hidden (<see cref="IsWindowVisible"/>).</summary>
    public event Action? WindowVisibilityChanged;

    /// <summary>This build's version, and the GitHub repository (from RepositoryUrl in Directory.Build.props) to update from.</summary>
    private static UpdaterOptions UpdaterOptions()
    {
        var assembly = typeof(App).Assembly;
        var version = assembly.GetName().Version ?? new Version(0, 0, 0);
        var repository = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RepositoryUrl").Value!;
        return new UpdaterOptions(new Uri(repository).AbsolutePath.Trim('/'), new Version(version.Major, version.Minor, version.Build));
    }
}

