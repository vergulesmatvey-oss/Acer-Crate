using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Services;

public sealed class GameLibraryService(SettingsService settings, ILogger<GameLibraryService> log)
{
    private Task<List<GameEntry>>? scan;
    public IReadOnlyList<GameEntry> Games => settings.Current.Games;
    public async Task DiscoverAtStartupAsync()
    {
        if (!settings.Current.Ui.AutoDiscoverGames) return;
        try { await DiscoverAsync(); }
        catch (Exception error) { log.LogWarning(error, "Automatic game discovery failed"); }
    }
    public void Add(GameEntry game)
    {
        if (!game.CanLaunch) throw new InvalidDataException("Choose an executable or a supported launcher game.");
        settings.Update(s => s with { Games = GameLibrary.Merge(s.Games, [game]).ToList() });
    }
    public void Remove(string id) => settings.Update(s => s with { Games = s.Games.Where(g => g.Id != id).ToList() });
    public void Favorite(string id) => settings.Update(s => s with { Games = s.Games.Select(g => g.Id == id ? g with { Favorite = !g.Favorite } : g).ToList() });
    public void Launch(GameEntry game)
    {
        if (!game.CanLaunch) throw new InvalidDataException("Invalid game target.");
        if (game.LaunchUri is { } uri && GameLibrary.IsLauncherUri(uri))
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true })?.Dispose();
        else
        {
            if (!File.Exists(game.Executable)) throw new FileNotFoundException("Game executable was not found.", game.Executable);
            Process.Start(new ProcessStartInfo(game.Executable)
            {
                UseShellExecute = true, Arguments = game.Arguments,
                WorkingDirectory = Path.GetDirectoryName(game.Executable)!
            })?.Dispose();
        }
        settings.Update(s => s with { Games = s.Games.Select(g => g.Id == game.Id ? g with { LastPlayed = DateTimeOffset.UtcNow } : g).ToList() });
        log.LogInformation("Game launched: {Name} ({Source})", game.Name, game.Source);
    }

    public async Task<int> DiscoverAsync()
    {
        var currentScan = scan ??= Task.Run(Discover);
        List<GameEntry> discovered;
        try { discovered = await currentScan; }
        finally { if (scan == currentScan) scan = null; }
        var before = settings.Current.Games.Count;
        settings.Update(s => s with { Games = GameLibrary.Merge(s.Games, discovered).ToList() });
        log.LogInformation("Library scan found {Count} entries", discovered.Count);
        return settings.Current.Games.Count - before;
    }

    private List<GameEntry> Discover()
    {
        var games = new List<GameEntry>();
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            if (key?.GetValue("SteamPath") is string steam) roots.Add(steam);
        foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var key = root.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("InstallPath") is string steam) roots.Add(steam);
        }
        foreach (var root in roots.ToArray())
        {
            var libraries = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            TryRead(libraries, text => { foreach (var library in GameLibrary.SteamLibraries(text)) roots.Add(library); });
        }
        foreach (var root in roots)
        {
            var folder = Path.Combine(root, "steamapps");
            if (!Directory.Exists(folder)) continue;
            try { foreach (var file in Directory.EnumerateFiles(folder, "appmanifest_*.acf")) TryRead(file, text => { if (GameLibrary.SteamManifest(text) is { } game) games.Add(game); }); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log.LogDebug(e, "Steam directory unavailable"); }
        }
        var epic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
        if (Directory.Exists(epic))
        {
            try { foreach (var file in Directory.EnumerateFiles(epic, "*.item")) TryRead(file, text => { if (GameLibrary.EpicManifest(text) is { } game) games.Add(game); }); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log.LogDebug(e, "Epic directory unavailable"); }
        }
        return games;
    }

    private void TryRead(string path, Action<string> consume)
    {
        try { if (File.Exists(path) && new FileInfo(path).Length <= 2 * 1024 * 1024) consume(File.ReadAllText(path)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { log.LogDebug(e, "Skipped game manifest"); }
    }
}
