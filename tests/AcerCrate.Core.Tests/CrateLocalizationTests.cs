using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AcerCrate.Core.Tests;

public class CrateLocalizationTests
{
    [Fact]
    public void Every_custom_text_has_complete_translations_and_matching_format_arguments()
    {
        var path = Path.Combine(SourceTree.Root, "src", "AcerCrate.App", "Localization", "CrateText.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var ids = new HashSet<string>(); var originals = new HashSet<string>();
        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            Assert.True(ids.Add(entry.GetProperty("Id").GetString()!));
            var ru = entry.GetProperty("Ru").GetString()!;
            Assert.True(originals.Add(ru));
            foreach (var language in new[] { "En", "Uk" })
            {
                var value = entry.GetProperty(language).GetString();
                Assert.False(string.IsNullOrWhiteSpace(value), $"Missing {language}: {ru}");
                string Args(string text) => string.Join(',', Regex.Matches(text, @"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})").Select(m => m.Groups[1].Value).Order());
                Assert.Equal(Args(ru), Args(value!));
            }
        }
    }

    [Fact]
    public void Custom_xaml_labels_use_existing_translation_keys()
    {
        var root = Path.Combine(SourceTree.Root, "src", "AcerCrate.App");
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Localization", "CrateText.json")));
        var keys = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("Id").GetString()).ToHashSet();
        foreach (var file in Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories).Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        foreach (var attribute in XDocument.Load(file).Descendants().Attributes())
        {
            var match = Regex.Match(attribute.Value, @"^\{l:Text Key=(Crate\d+)\}$");
            if (match.Success) Assert.Contains(match.Groups[1].Value, keys);
        }
    }
}
