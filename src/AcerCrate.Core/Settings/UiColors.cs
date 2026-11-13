namespace AcerCrate.Core.Settings;

/// <summary>Opaque interface colours, shared by the picker and saved settings.</summary>
public static class UiColors
{
    public static bool TryNormalize(string? value, out string hex)
    {
        hex = "";
        if (value is null) return false;
        var digits = value.Trim();
        if (digits.StartsWith('#')) digits = digits[1..];
        if (digits.Length == 3)
            digits = string.Concat(digits.Select(c => new string(c, 2)));
        if (digits.Length != 6 || !digits.All(Uri.IsHexDigit)) return false;
        hex = "#" + digits.ToUpperInvariant();
        return true;
    }
}
