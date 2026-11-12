using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml.Markup;

namespace OpenSense.App.Localization;

/// <summary>Texts added by Acer Crate, shared by code and XAML. User names and device readings stay untouched.</summary>
public static class CrateText
{
    private sealed record Entry(string Ru, string En, string Uk);
    private static readonly Dictionary<string, Entry> Texts = Load();

    private static Dictionary<string, Entry> Load()
    {
        using var stream = typeof(CrateText).Assembly.GetManifestResourceStream("OpenSense.App.Localization.CrateText.json")!;
        using var document = JsonDocument.Parse(stream);
        var values = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var entry = new Entry(item.GetProperty("Ru").GetString()!, item.GetProperty("En").GetString()!, item.GetProperty("Uk").GetString()!);
            values[entry.Ru] = entry;
            values[item.GetProperty("Id").GetString()!] = entry;
        }
        return values;
    }

    public static string T(string text) => Texts.TryGetValue(text, out var entry)
        ? AppLanguage.Current switch { "ru-RU" => entry.Ru, "uk-UA" => entry.Uk, _ => entry.En }
        : text;

    public static string F(string text, params object?[] args) => string.Format(CultureInfo.CurrentCulture, T(text), args);
    public static string ModeTitle(string name) => name == "Мой" ? T(name) : name;

    public static string ConnectionStatus(Services.SessionState state) => T(state switch
    {
        Services.SessionState.Ready => "Подключено",
        Services.SessionState.Starting => "Подключение",
        Services.SessionState.NeedsElevation => "Нужны права администратора",
        Services.SessionState.ServiceUnavailable => "Служба недоступна",
        Services.SessionState.Unsupported => "Устройство не поддерживается",
        _ => "Ошибка подключения"
    });
}

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class TextExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    protected override object ProvideValue() => CrateText.T(Key);
}
