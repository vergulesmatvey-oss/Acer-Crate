using System.Text.RegularExpressions;
using OpenSense.Core.Settings;

namespace OpenSense.App.Services;

public static partial class ModelArtwork
{
    public static string? Resolve(UiSettings settings, string? detectedModel)
    {
        if (!string.IsNullOrWhiteSpace(settings.CustomLaptopImage))
            return settings.CustomLaptopImage;
        return Catalog(settings.ArtworkModelQuery ?? detectedModel);
    }

    public static string? Catalog(string? model)
    {
        if (model is null || !Nitro58().IsMatch(model)) return null;
        return Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "AN515-58.png");
    }

    [GeneratedRegex(@"\bAN515[-\s]?58\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Nitro58();
}
