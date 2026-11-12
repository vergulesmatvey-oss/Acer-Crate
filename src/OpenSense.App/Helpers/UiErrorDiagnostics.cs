using System.Runtime.ExceptionServices;

namespace OpenSense.App.Helpers;

/// <summary>Keep the original managed stack when WinRT later reports only a stowed HRESULT.</summary>
internal static class UiErrorDiagnostics
{
    private static readonly object Gate = new();
    private static readonly HashSet<string> Recorded = [];
    [ThreadStatic] private static bool writing;

    public static void Initialize()
    {
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception) Record(exception, "Unhandled");
        };
    }

    private static void OnFirstChance(object? sender, FirstChanceExceptionEventArgs args)
    {
        if (args.Exception is InvalidOperationException) Record(args.Exception, "First chance (may be handled)");
    }

    public static void Record(Exception exception, string context)
    {
        if (writing) return;
        writing = true;
        try
        {
            var detail = exception.ToString();
            lock (Gate)
            {
                if (Recorded.Count >= 64 || !Recorded.Add(context + detail)) return;
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AcerCrate", "Diagnostics");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "ui-errors.log"), $"{DateTimeOffset.Now:o} · {context} · 0x{exception.HResult:X8}\n{detail}\n\n");
            }
        }
        catch (Exception) { /* Diagnostic I/O must never terminate the app. */ }
        finally { writing = false; }
    }
}
