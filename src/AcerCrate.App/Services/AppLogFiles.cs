using System.Security.Principal;

namespace AcerCrate.App.Services;

public static class AppLogFiles
{
    public static string ClientDirectory { get; } = ChooseClientDirectory();
    public static string ServiceDirectory => Path.Combine(AppContext.BaseDirectory, "Service", "Logs");
    private static string ChooseClientDirectory()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var local = Path.Combine(AppContext.BaseDirectory, "Logs", identity.User?.Value ?? Environment.UserName);
        try { Directory.CreateDirectory(local); var probe = Path.Combine(local, Guid.NewGuid().ToString("N")); File.WriteAllText(probe, ""); File.Delete(probe); return local; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AcerCrate", "Logs"); }
    }
}
