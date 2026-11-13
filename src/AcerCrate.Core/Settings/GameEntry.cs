using System.Text.Json;
using System.Text.RegularExpressions;

namespace AcerCrate.Core.Settings;

public sealed record GameEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "";
    public string Executable { get; init; } = "";
    public string? LaunchUri { get; init; }
    public string Arguments { get; init; } = "";
    public string Source { get; init; } = "Manual";
    public bool Favorite { get; init; }
    public DateTimeOffset? LastPlayed { get; init; }

    public bool CanLaunch => GameLibrary.IsLauncherUri(LaunchUri) ||
        (Path.IsPathFullyQualified(Executable) && Path.GetExtension(Executable).ToLowerInvariant() is ".exe" or ".lnk");
}

public static partial class GameLibrary
{
    public static bool IsLauncherUri(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "steam" or "com.epicgames.launcher" && !value.Any(char.IsControl);

    public static IReadOnlyList<GameEntry> Merge(IEnumerable<GameEntry> existing, IEnumerable<GameEntry> discovered)
    {
        var result = existing.Where(game => game.CanLaunch).ToList();
        var known = new HashSet<string>(result.Select(Identity), StringComparer.OrdinalIgnoreCase);
        foreach (var game in discovered)
            if (game.CanLaunch && known.Add(Identity(game))) result.Add(game);
        return result;
    }

    private static string Identity(GameEntry game) => game.LaunchUri ?? Path.GetFullPath(game.Executable);

    public static GameEntry? SteamManifest(string text)
    {
        var id = Value(text, "appid");
        var name = Value(text, "name");
        return uint.TryParse(id, out var appId) && appId > 0 && !string.IsNullOrWhiteSpace(name)
            ? new GameEntry { Id = "steam-" + appId, Name = name, Source = "Steam", LaunchUri = "steam://rungameid/" + appId }
            : null;
    }

    public static IEnumerable<string> SteamLibraries(string text) => LibraryPath().Matches(text)
        .Select(match => Unescape(match.Groups[1].Value)).Distinct(StringComparer.OrdinalIgnoreCase);

    public static GameEntry? EpicManifest(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        var app = root.TryGetProperty("AppName", out var a) ? a.GetString() : null;
        var name = root.TryGetProperty("DisplayName", out var n) ? n.GetString() : null;
        var installed = root.TryGetProperty("InstallLocation", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installed)) return null;
        var executable = root.TryGetProperty("LaunchExecutable", out var e) ? e.GetString() : null;
        return new GameEntry
        {
            Id = "epic-" + app, Name = name, Source = "Epic Games",
            Executable = executable is not null ? Path.Combine(installed, executable) : "",
            LaunchUri = "com.epicgames.launcher://apps/" + Uri.EscapeDataString(app) + "?action=launch&silent=true"
        };
    }

    private static string? Value(string text, string key) => Regex.Match(text,
        "\"" + Regex.Escape(key) + "\"\\s+\"((?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) is { Success: true } match
        ? Unescape(match.Groups[1].Value) : null;
    private static string Unescape(string value) => value.Replace("\\\\", "\\", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);
    [GeneratedRegex("\"path\"\\s+\"((?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LibraryPath();
}
