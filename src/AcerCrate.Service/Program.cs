using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using AcerCrate.Core.Engine;
using AcerCrate.Core.Ipc;
using AcerCrate.Core.Settings;
using AcerCrate.Service;
using Serilog;

// The AcerCrate service: owns the laptop's firmware (as LocalSystem) and serves the app over a named
// pipe, so the app needs no administrator rights. Registered by the installer.

SettingsPaths.SecureMachineDirectory();

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    DisableDefaults = true,
});
builder.Services.AddWindowsService(options => options.ServiceName = CratePipe.ServiceName);
if (WindowsServiceHelpers.IsWindowsService())
    builder.Services.AddSingleton<IHostLifetime, PowerAwareServiceLifetime>(); // also forwards sleep/resume and AC events

builder.Services.AddSerilog(log => log
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "service-.log"), formatProvider: CultureInfo.InvariantCulture,
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5, fileSizeLimitBytes: 2 * 1024 * 1024, rollOnFileSizeLimit: true));

builder.Services.AddSingleton(services => new CrateEngine(
    new WindowsMachine(), SettingsPaths.Machine, services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CrateEngine>>()));
builder.Services.AddSingleton(services => new PipeServer(
    services.GetRequiredService<CrateEngine>(),
    services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PipeServer>>()));
builder.Services.AddHostedService<EngineHost>();

try
{
    await builder.Build().RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
