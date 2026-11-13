using System.Text.RegularExpressions;

namespace AcerCrate.Core.Hardware;

public sealed record ModelArtworkEntry
{
    public string File { get; init; } = "";
    public string Title { get; init; } = "";
    public string[] Models { get; init; } = [];
    public string[] Prefixes { get; init; } = [];
    public string SourcePage { get; init; } = "";
    public string ImageUrl { get; init; } = "";
}

public sealed record ModelArtworkMatch(ModelArtworkEntry Entry, bool IsExact);

public static partial class ModelArtworkCatalog
{
    public static string? Code(string? model)
    {
        var match = ModelCode().Match(model ?? "");
        return match.Success ? (match.Groups[1].Value + "-" + match.Groups[2].Value).ToUpperInvariant() : null;
    }

    public static ModelArtworkMatch? Find(string? model, IReadOnlyList<ModelArtworkEntry> entries)
    {
        if (Code(model) is not { } code) return null;
        var exact = entries.FirstOrDefault(entry => entry.Models.Contains(code, StringComparer.OrdinalIgnoreCase));
        if (exact is not null) return new(exact, true);
        var family = entries.SelectMany(entry => entry.Prefixes.Select(prefix => (Entry: entry, Prefix: prefix)))
            .Where(item => code.StartsWith(item.Prefix + "-", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Prefix.Length).FirstOrDefault();
        return family.Entry is not null ? new(family.Entry, false) : null;
    }

    [GeneratedRegex(@"\b((?:AN|PH|PT|NL)[A-Z0-9]*?\d{2,3}S?)[-\s]?(\d{2}[A-Z]?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ModelCode();
}
